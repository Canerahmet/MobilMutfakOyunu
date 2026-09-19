using System;
using Lokanta.Core.Content;
using Lokanta.Core.Economy;

namespace Lokanta.Core.Sim
{
    public enum DayPhase
    {
        Closed = 0,
        Morning = 1,
        Service = 2,
        Evening = 3
    }

    public enum CustomerStage
    {
        None = 0,
        WaitingForTable = 1,
        WaitingToOrder = 2,
        WaitingForFood = 3,
        Eating = 4,
        WaitingToPay = 5,
        Done = 6,
        LeftAngry = 7
    }

    internal enum TaskKind
    {
        None = 0,
        SeatOrder = 1,
        Cook = 2,
        Serve = 3,
        Pay = 4,
        Clear = 5,

        /// <summary>
        /// Washing up. One head from the hall pool moves to the sink.
        ///
        /// docs/14 describes the dishwasher as a BOTTLENECK: "if the plates
        /// run out, service stops - invisible, but noticed the moment it
        /// clogs". Until now that bottleneck was buried inside the hall
        /// capacity, that is, it was never visible at all.
        /// </summary>
        Wash = 6
    }

    /// <summary>
    /// The fixed-step restaurant simulation.
    ///
    /// The contract of docs/23-core-contract.md holds:
    ///   - Tick() takes no parameters, advances 100 ms, and never sees real time
    ///   - all state is integer
    ///   - randomness comes from a separate stream per subsystem
    ///   - the same seed and the same command log give the same state
    ///
    /// Staff are not modelled as agents. docs/14: "no complex pathfinding".
    /// A member of staff is a SERVER in a work pool; they work one task at a
    /// time. Daily capacity falls out naturally from these task durations.
    ///
    /// The unit of a customer: one slot is a PARTY occupying a TABLE. The
    /// demand formula produces PEOPLE; parties are formed until the people
    /// run out. Work and the bill scale with the party size. This is the
    /// decision behind the review's finding that "people vs. parties must be
    /// made clear".
    /// </summary>
    public sealed partial class Simulation
    {
        public const int MaxParties = 256;
        public const int MaxTables = 16;
        public const int MaxServers = 16;

        /// <summary>The opening stock, in grams per ingredient.</summary>
        public const int OpeningStockGrams = 6000;

        /// <summary>
        /// At least this many portions' worth of ingredients is held for
        /// every dish on the menu. It matches the largest party size.
        /// </summary>
        public const int MinPartyBuffer = 4;

        /// <summary>
        /// The most commands per day. docs/23 7.3: the touch budget is 60,
        /// and four times that was left as headroom. Beyond it a command is
        /// rejected.
        /// </summary>
        public const int MaxCommandsPerDay = 256;

        // The loan terms come FROM THE CONTENT (economy.json). They used to
        // be hard coded here and it was a coincidence that they matched the
        // values in the content; tools/audit_content.py caught it.

        /// <summary>A gram's cost from the price per kilo: price x grams / 1000.</summary>
        private const int GramsPerKilo = 1000;

        // ---- immutables -----------------------------------------------------
        private readonly EconomyConfig _economy;
        private readonly ContentSet _content;
        private readonly TimingConfig _timing;
        private readonly ulong _masterSeed;

        // ---- randomness -----------------------------------------------------
        private Rng _rngArrival;
        private Rng _rngArchetype;
        private Rng _rngOrder;
        private Rng _rngStaffError;

        /// <summary>The daily movement of market prices. RngStream.Market.</summary>
        private Rng _rngMarket;
        private Rng _rngCredit;
        private Rng _rngRegular;
        private Rng _rngHiring;
        private Rng _rngEvent;

        /// <summary>The staff name. It does not enter the play, only the presentation.</summary>
        private Rng _rngName;

        // ---- state ----------------------------------------------------------
        private long _tickIndex;
        private int _day;
        private DayPhase _phase;
        private int _serviceTick;

        private int _tableCount;
        private int _reputationCenti;
        private long _cash;

        private int _cooks;
        private int _hall;

        // --- The tab, docs/07 the Turkish signature mechanic ------------------
        // Open accounts. One account = one party's unpaid bill.
        // The ORDER of operations matters: no gap is left in the array, the
        // last account slides into the place of the one removed, so that a
        // scan ends at _tabCount.
        public const int MaxTabs = 24;
        private readonly long[] _tabAmount = new long[MaxTabs];
        private readonly int[] _tabDueDay = new int[MaxTabs];
        private readonly int[] _tabTea = new int[MaxTabs];

        /// <summary>
        /// WHO this account is written against (the regular's index, or -1).
        ///
        /// The book WAS NOT KEEPING whose debt it was, which is why the
        /// collection chance was the same for everybody and no question of
        /// "who shall I write it against" could arise. Trust is read from
        /// this field.
        /// </summary>
        private readonly int[] _tabRegular = new int[MaxTabs];
        private int _tabCount;
        /// <summary>Is this party's bill going onto the tab.</summary>
        private readonly bool[] _pCredit = new bool[MaxParties];
        /// <summary>Was this party offered tea; it raises the collection chance.</summary>
        private readonly bool[] _pTea = new bool[MaxParties];
        /// <summary>Is this party ASKING for a tab. Turning the request down has a price.</summary>
        private readonly bool[] _pAsksCredit = new bool[MaxParties];
        /// <summary>A party taking the combo: the bill is worked out from the combo.</summary>
        private readonly bool[] _pCombo = new bool[MaxParties];
        /// <summary>True when the combo is on the menu and switched on. The player's decision.</summary>
        private bool _comboOn;
        /// <summary>The loyalty the tab has built up: a permanent addition to demand, in bp.</summary>
        private int _creditLoyaltyBp;

        // --- Named regulars, docs/11 ------------------------------------------
        // An archetype produces thousands of customers; a regular is ONE
        // SINGLE PERSON and is always the same person. That is why their
        // state is kept per person: how many times they have come, how
        // satisfied they left on average, which beat of their story has
        // opened, and, if they were upset, how many days they will stay away.
        public const int MaxRegulars = 16;
        private readonly int[] _regVisits = new int[MaxRegulars];
        private readonly long[] _regSatSum = new long[MaxRegulars];
        private readonly int[] _regBeat = new int[MaxRegulars];
        private readonly int[] _regAwayDays = new int[MaxRegulars];
        private readonly bool[] _regComing = new bool[MaxRegulars];
        /// <summary>Which regular this party is; -1 means the nameless crowd.</summary>
        private readonly int[] _pRegular = new int[MaxParties];
        /// <summary>Did they fail to find their favourite dish on the menu.</summary>
        private readonly bool[] _pMissedFavourite = new bool[MaxParties];
        private readonly int[] _arrRegular = new int[MaxParties];

        // docs/14 "Experience and level": DAYS WORKED per head. The sim keeps
        // staff as a count, but experience cannot be a count - how long each
        // one has been here belongs to that person. The first _cooks /
        // _hall elements of the array are the valid ones. Hiring appends at
        // the end and firing takes from the end: the newest goes. Otherwise a
        // decision of the form "sack the most experienced one" would arise,
        // which makes no sense.
        private readonly int[] _cookXpDays = new int[MaxServers];

        /// <summary>
        /// TENURE: how many days they have been here. SEPARATE from experience.
        ///
        /// The screen read "Level 0 (0 days)" and that "days" was actually
        /// _cookXpDays - that is, EXPERIENCE. Experience depends on the trait
        /// (the `tecrubeli` trait has XpBp 0, while the
        /// `cirak` one has 2x), and therefore:
        ///   - somebody with `tecrubeli` who had worked sixty days
        ///     showed "0 days",
        ///   - somebody with `cirak` who had worked thirty days
        ///     showed "60 days".
        /// Printing on screen a number the simulation contradicts is the
        /// same mistake this session fixed in the dialogue lines.
        ///
        /// Tenure is independent of the trait: +1 for every day worked.
        /// </summary>
        private readonly int[] _cookTenure = new int[MaxServers];
        private readonly int[] _hallTenure = new int[MaxServers];

        /// <summary>
        /// How many days of tenure ARE RECOGNISED.
        ///
        /// 30 = half the campaign. It was not invented; it came out of two
        /// bounds:
        ///   - Too small (7, say) and it fires for somebody every week and
        ///     stops being recognition, turning into noise - the badges
        ///     firing three times in fifteen days and then going quiet for
        ///     fifty-two was lived through once already on this project
        ///     (docs/47).
        ///   - Too large (50, say) and it only ever fires for a crew hired on
        ///     day one and never changed, so it has nothing to do with the
        ///     player's DECISION.
        /// Thirty days means someone still with you in their second month.
        ///
        /// THE THRESHOLD IS EXACT: "== TenureDays", not ">=". Otherwise the
        /// event would fire again every day and the notification strip would
        /// fill up with a single sentence - the same mistake was made once
        /// with the plate notification.
        /// </summary>
        public const int TenureDays = 30;
        private readonly int[] _hallXpDays = new int[MaxServers];

        // --- Traits and morale, docs/14 ---------------------------------------
        // Each member of staff draws TWO traits from the pool; a conflicting
        // pair never comes up. Morale is 0-100 and a new hire starts at 70.
        private readonly int[] _cookTraitA = new int[MaxServers];
        private readonly int[] _cookTraitB = new int[MaxServers];
        private readonly int[] _hallTraitA = new int[MaxServers];
        private readonly int[] _hallTraitB = new int[MaxServers];
        /// <summary>
        /// The member of staff's position in the name pool.
        ///
        /// The name does not enter THE GAME'S RULES; it only builds the
        /// player's attachment. It is still kept in the state and saved: if
        /// "Nurten Abla" turns out to be somebody else on the second launch,
        /// the attachment goes with her.
        /// </summary>
        private readonly int[] _cookName = new int[MaxServers];
        private readonly int[] _hallName = new int[MaxServers];

        private readonly int[] _cookMorale = new int[MaxServers];
        private readonly int[] _hallMorale = new int[MaxServers];
        /// <summary>Which waiter served this party last; for the trait's effect on satisfaction.</summary>
        private readonly int[] _pServer = new int[MaxParties];
        /// <summary>Which cook last cooked this party's food.</summary>
        private readonly int[] _pCook = new int[MaxParties];
        /// <summary>The run of busy days in a row. docs/14: -3 morale a day.</summary>
        private int _busyStreak;

        // --- Hiring candidates, docs/14 ---------------------------------------
        // "Three candidates are visible at once on the hiring screen. A
        //  candidate is generated: role, two traits, appearance, name. The
        //  candidate pool refreshes every three days."
        //
        // Without this pool a trait is A LOTTERY: the player does not know
        // who they are taking on. Measurement showed it - blind hiring took
        // a good player's reputation on fast food from 96.5 down to 87,
        // because drawing an expensive crew tightens the budget and a tight
        // budget stopped them hiring the person who would have fixed the
        // service. The CHOICE in docs/14 is precisely the answer to that.
        public const int CandidateSlots = 3;
        private readonly int[] _candTraitA = new int[CandidateSlots * 2];
        private readonly int[] _candTraitB = new int[CandidateSlots * 2];
        private int _candDay = -1;

        private readonly long[] _dishPrice;      // the price the player set
        private readonly bool[] _dishOnMenu;

        /// <summary>
        /// Was this dish open YESTERDAY. Only for announcing the unlock; it
        /// does not enter the game's rules.
        /// </summary>
        private readonly bool[] _dishWasUnlocked;

        /// <summary>
        /// Is there any dish that uses this station. Worked out once while
        /// the cuisine is loading.
        /// </summary>
        private readonly bool[] _stationUsed;

        // ---- stock (the market stage) ----------------------------------------
        // The day cycle of docs/02: ingredients are bought for cash AT THE
        // MARKET in the morning. Without this the restaurant was running
        // itself and a player who never intervened at all closed the sixty
        // days in profit. This is the price of neglect.
        private readonly int[] _stockGrams;

        /// <summary>
        /// The ingredient's AGE, in days. Meaningless without a cold store:
        /// everything perishable goes off overnight. Once a cold-store tier
        /// arrives, an ingredient can live for part of its own shelf life.
        ///
        /// The age is kept per INGREDIENT rather than per batch; on a
        /// purchase a WEIGHTED MEAN is taken. The simple "reset on purchase"
        /// rule opened an exploit: you could buy one gram a day and keep the
        /// clock at zero forever.
        /// </summary>
        private readonly int[] _stockAgeDays;

        /// <summary>The cold-store tier owned. 0 = none.</summary>
        private int _storageTier;

        /// <summary>
        /// TODAY'S market price multiplier, per ingredient, in basis points.
        ///
        /// docs/12 3: "there is no advantage in buying early, stock goes off.
        /// Catching the cheap day is not luck, it is a matter of paying
        /// attention." priceVolatilityBp was written in the content and was
        /// read nowhere; the market gave the same price every day, so there
        /// was nothing to pay attention to.
        ///
        /// It is set at the start of the day and fixed for the day: the
        /// player sees the prices in the morning and decides.
        /// </summary>
        private readonly int[] _marketBp;

        /// <summary>
        /// The quality tier of what is bought at the market: 0 low, 1
        /// standard, 2 high. ONE global setting, with no choice per
        /// ingredient.
        ///
        /// The reason is in the content: the six most sensitive ingredients
        /// are all meat, while salt and black pepper are all but
        /// insensitive. So even a single setting gives a different result by
        /// dish, and the player is not loaded with 77 decisions.
        /// </summary>
        private int _quality = 1;

        /// <summary>
        /// The MEAN quality effect of the stock in hand, in centi-points.
        /// A weighted mean is taken on a purchase; somebody who buys cheap
        /// and then buys dear is not rid of the cheap goods on their hands
        /// straight away.
        /// </summary>
        private readonly int[] _stockQualityCenti;
        private int _stockOutEvents;
        private int _turnedAwayParties;

        /// <summary>
        /// The satisfaction of a customer turned back at the door. The
        /// neutral threshold is 6000; this is below it, but nowhere near
        /// zero. "They had nothing" is a bad experience, but not as bad as
        /// "I waited forty minutes".
        /// </summary>
        private const int TurnAwaySatisfactionCenti = 4000;

        // ---- tables ----------------------------------------------------------
        private readonly int[] _tableParty;      // -1 empty
        private readonly bool[] _tableDirty;

        // ---- THE PLATE CYCLE -------------------------------------------------
        //
        // The restaurant has a COUNTED number of plates and they circulate:
        //
        //   clean   -> (the cook plates the food)     -> in use
        //   in use  -> (the waiter clears the table)  -> dirty
        //   dirty   -> (washed at the sink)           -> clean
        //
        // The total does not change: clean + in use + dirty = the tier's
        // plates. This is an INVARIANT and the test checks it as one - a
        // leaking plate would be a bug that slowly brings service to a halt
        // with no visible cause.
        //
        // Why counted: docs/14's justification for the dishwasher. When the
        // clean plates run out the cook cannot put the cooked food on a plate
        // and service STOPS. This is the thing that teaches the player "do
        // not neglect what looks unimportant".
        private int _platesClean;
        private int _platesDirty;
        private int _platesInUse;

        /// <summary>The plates sitting on the table; they go to the sink when it is cleared.</summary>
        private readonly int[] _tablePlates;

        /// <summary>The plates the party has in hand.</summary>
        private readonly int[] _pPlates;

        /// <summary>Their food HAS COOKED but it is waiting for a plate.</summary>
        private readonly bool[] _pCooked;

        /// <summary>The ticks spent waiting because there is no clean plate (for the day).</summary>
        private int _plateBlockedTicks;

        // ---- parties ---------------------------------------------------------
        private readonly bool[] _pActive;
        private readonly int[] _pArchetype;
        private readonly int[] _pDishMain;
        private readonly int[] _pDishSide;    // -1 none
        private readonly int[] _pDishDrink;   // -1 none

        /// <summary>
        /// The dessert. Until this field was written the dessert group was
        /// DEAD content: fast food had 6 dessert dishes and the Turkish
        /// restaurant 3, and not one of them could be ordered. The peak
        /// table of docs/27 3.3 gives the dessert 0.08 concurrent plates,
        /// and the equipment ladder has an upgrade for the dessert station;
        /// both were running for nothing.
        /// </summary>
        private readonly int[] _pDishDessert; // -1 none

        /// <summary>
        /// The dish the customer ASKED FOR but which the restaurant cannot
        /// make. -1 for none.
        ///
        /// Once the unlock system was written it was measured that a locked
        /// dish rewards PASSIVITY: a dish that cannot be made costs nothing
        /// in stock either, so a player who invested nothing was profiting
        /// for free. This field removes that free ride: a customer comes and
        /// asks for the dish whose day and reputation have come but whose
        /// EQUIPMENT has not been bought, and when they cannot have it their
        /// satisfaction drops.
        /// </summary>
        private readonly int[] _pAskedDish;
        private readonly int[] _pSize;
        private readonly CustomerStage[] _pStage;

        /// <summary>
        /// Did the owner see to this table IN PERSON - for the next piece of
        /// hall work. It is cleared once the work is done: attention is a
        /// SINGLE STEP, not a standing condition.
        /// </summary>
        private readonly bool[] _pAttended;
        private readonly int[] _pTable;
        private readonly int[] _pPatienceLeftMs;
        private readonly int[] _pPatienceTotalMs;
        private readonly int[] _pWaitedMs;
        private readonly int[] _pEatLeftMs;
        private readonly int[] _pSatisfactionCenti;
        private readonly int[] _pBonusCenti;     // an apology, a treat, the owner's attention

        /// <summary>
        /// The owner's intervention allowance left today. docs/02 59: 3-5 a
        /// day. Without the limit every angry customer could be saved for
        /// free.
        /// </summary>
        private int _interventionsLeft;
        private readonly bool[] _pInTask;
        private readonly bool[] _pWarned;
        private int _partyCount;

        // ---- stations and equipment ------------------------------------------
        // docs/27 Decision D: prepMs is the dish's WALL CLOCK time, and the
        // cook is occupied for prepMs x attendBp / 10000. An equipment
        // upgrade either adds a slot or lowers attendBp; it does not touch
        // prepMs.
        //
        // Until this was written the simulation kept the cook busy for the
        // whole of the wall clock, which meant there was no difference at all
        // between the oven and the stove and no story for the equipment to
        // tell.
        private readonly int[] _stationTier;    // the equipment tier owned
        private readonly int[] _stationBusy;    // the slots currently occupied

        /// <summary>The most station jobs one party can have: main, side, drink.</summary>
        /// <summary>Main, side, drink, dessert: all four may be separate stations.</summary>
        private const int MaxJobsPerParty = 4;

        private readonly int[] _jobStation;     // MaxParties x 3, -1 empty
        private readonly int[] _jobMs;          // the wall clock remaining
        private readonly int[] _jobState;       // 0 waiting, 1 cooking, 2 done
        private readonly int[] _jobPlates;      // how many plates
        private readonly int[] _jobSlots;       // how many slots it took when it started
        private readonly int[] _pJobsLeft;

        // ---- the work pools --------------------------------------------------
        private readonly TaskKind[] _hallTaskKind;
        private readonly int[] _hallTaskTarget;
        private readonly int[] _hallTaskLeftMs;
        private readonly TaskKind[] _kitchenTaskKind;
        private readonly int[] _kitchenTaskTarget;
        private readonly int[] _kitchenTaskLeftMs;

        // ---- the arrival plan ------------------------------------------------
        private readonly int[] _arrTick;
        private readonly int[] _arrArchetype;
        private readonly int[] _arrSize;

        // Which slot is busiest today, and the day it was worked out for.
        // Derived from the arrival plan, so it is never saved.
        private int _peakSlot = -1;
        private int _peakSlotDay = -1;
        private int _arrCount;
        private int _arrNext;

        /// <summary>
        /// How many items of each role were ordered during the day: 0 main,
        /// 1 side, 2 drink, 3 dessert. For measurement only; this is what the
        /// test uses to guard against the dessert group quietly turning back
        /// into dead content.
        /// </summary>
        private readonly int[] _orderedRole = new int[4];

        // ---- the day's counters ----------------------------------------------
        private int _servedParties;
        private int _servedPeople;
        private int _angryParties;

        /// <summary>
        /// How many parties left A TABLE angry today.
        ///
        /// _angryParties counts those turned back at the door as well - a
        /// party that found no table never sat down, and so NEVER APPEARED
        /// in the crisis strip. Without separating the two, the inference
        /// "if there is an angry customer a warning must have gone out" is
        /// wrong: a warning can only be given for a party that IS SEATED.
        /// </summary>
        private int _angrySeated;
        private int _hallRushWashes;
        private long _revenue;
        private long _ingredientCost;
        private long _satisfactionSum;      // centi, weighted by people
        private long _revenueAll;           // across the campaign, never zeroed
        private long _reputationDeltaMicro; // cumulative, 1e-6 points
        private long _weeklyWagesPaid;
        private long _weeklyRentPaid;
        private int _firstDebtDay;          // 0 = never fell into debt

        /// <summary>
        /// How many times the ladder down was climbed. The RESILIENCE axis
        /// of the year-end evaluation reads this (docs/08): the punishment is
        /// cumulative rather than momentary.
        /// </summary>
        private int _debtRungs;

        /// <summary>
        /// The wages and rent paid TODAY. Full on one day a week and zero on
        /// the others; the day report shows these.
        /// </summary>
        private long _dayWages, _dayRent;

        /// <summary>
        /// The value of the stock thrown out TODAY. Daily rather than
        /// cumulative: the evening report is about today.
        /// </summary>
        private long _daySpoiled;

        /// <summary>
        /// The value of the stock THROWN OUT across the season, in centi.
        /// The missing fourth line of the income statement.
        /// </summary>
        private long _spoiledValue;

        /// <summary>The total paid for equipment and expansion, in centi.</summary>
        private long _equipmentSpend, _expansionSpend;

        /// <summary>
        /// The cash SPENT on ingredients, in centi. Different from
        /// IngredientCost: that is the cost OF GOODS SOLD (for the day
        /// report), this is the money leaving the till. Confusing the two
        /// had torn the balance tool's income statement away from the real
        /// movement of cash.
        /// </summary>
        private long _ingredientSpend;

        /// <summary>
        /// The money the ladder down PUTS INTO the till, in centi: half of
        /// the equipment sold, the refund from downsizing, and debt written
        /// off.
        ///
        /// It has to be its own line in the income statement: without it the
        /// "net" and the real movement of cash do not agree, and the
        /// difference cannot be explained.
        /// </summary>
        private long _rescueValue;

        // ---- the year-end evaluation (docs/08) -------------------------------
        //
        // These do not enter the game's RULES; they are only read at the end
        // of the sixtieth day. Keeping a counter with no effect on the rules
        // in the state is cheap, and the justification is this: the year-end
        // score looks at the past, and the past only exists if it is
        // accumulated.

        /// <summary>
        /// How many parties ordered A MAIN DISH across the season, and how
        /// many of those turned into a combo.
        ///
        /// The denominator and the numerator of the signature axis. The
        /// denominator is not "all parties" but THOSE WITH A MAIN DISH ON
        /// THE ORDER: a combo only attaches to a main, so a party taking a
        /// dessert or a drink is not measuring the player's decision - put
        /// into the denominator, it would let the menu mix muddy the axis.
        /// </summary>
        private int _mainOrders, _comboOrders;

        /// <summary>
        /// The total cost of the tea offered when a tab is opened, in centi.
        ///
        /// IT HAS TO BE COUNTED: the money was leaving the till and was
        /// written into NO expense line, so the income statement of a player
        /// who opened tabs could never be reconciled. The balance tool's
        /// "difference" column showed 134 coins for the signature bot on
        /// Turkish cuisine, and that number was exactly this.
        ///
        /// It has to be visible too: a tab IS NOT FREE, and the absence of
        /// its cost from every screen made the mechanic look cheaper than it
        /// is.
        /// </summary>
        private long _teaSpend;

        /// <summary>The total written into the book across the season, in centi.</summary>
        private long _creditIssued;

        /// <summary>The total collected across the season, in centi.</summary>
        private long _creditCollected;

        // ---- the loan (docs/12 4) --------------------------------------------
        // One loan at a time. The instalment is weekly, on the same day as
        // the rent and the wages.
        private long _loanInstallment;      // the weekly instalment, centi-coins

        /// <summary>The total principal of the loans taken, in centi.</summary>
        private long _loanTaken;

        /// <summary>
        /// The total instalments paid on ALL loans, in centi.
        ///
        /// Different from _loanTotalRepaid: that one IS ZEROED when a new
        /// loan is taken (that field's job is the question "how much of THIS
        /// loan has been paid"). When the balance tool's reconciliation used
        /// it, a shortfall of 6,750 coins appeared on the strategies that
        /// took a second loan, and the shortfall was exactly the forgotten
        /// repayments.
        /// </summary>
        private long _loanRepaidAll;
        private int _loanWeeksLeft;
        private long _loanTotalRepaid;

        private readonly EventBuffer _events = new EventBuffer();

        // ---- the command log (docs/23 7) -------------------------------------
        // A save is: a snapshot at the start of the day + the commands
        // applied since then. Loading replays the log at top speed. Because
        // it is deterministic the result is byte for byte the same as an
        // uninterrupted game.
        private readonly Command[] _commandLog = new Command[MaxCommandsPerDay];
        private int _commandCount;

        // ====================================================================
        public Simulation(EconomyConfig economy, ContentSet content,
                          TimingConfig timing, ulong masterSeed)
        {
            _economy = economy ?? throw new ArgumentNullException(nameof(economy));

            // THE CUISINE'S HALL POOL IS APPLIED TO THE ECONOMY.
            //
            // If the content gave its own role list (cuisines/*.json:
            // salonRoles), the hall workload and wage come from it. If it did
            // not, WithHallPool changes nothing, so the old behaviour is kept
            // exactly.
            if (content != null && content.HallWorkPerCustomerMicro > 0)
                _economy = _economy.WithHallPool(
                    content.HallWorkPerCustomerMicro, content.HallWageNumerator);

            // THE CUISINE'S RENT.
            //
            // The justification is realistic: chains sit in expensive,
            // high-traffic places - rent is the price of volume.
            //
            // BUT THE DIRECTION WAS NOT INTUITED, IT WAS MEASURED (docs/52):
            //
            //   rent x1.15  reasonable 22,263  planner 25,094  signature 22,433
            //   rent x1.25  reasonable 23,493  planner 26,548  signature 22,578
            //
            // RAISING the rent RAISES the bot's till. The reason is the same
            // one seen with the volume multiplier (docs/51 §5): the bot
            // answers a cost by not expanding, and not expanding is more
            // profitable anyway. So THE CLOSING TILL IS NOT A MEASURE OF
            // DIFFICULTY FOR THIS BOT; the measure is the DIFFERENCE BETWEEN
            // the two cuisines.
            //
            // 11500 was chosen because it is the value that narrows that
            // difference:
            //   no multiplier  29.5%  |  x1.15  28.3%  |  x1.25  35.4%
            // (against Turkish reasonable at 17,351.)
            if (content != null && content.RentMultiplierBp > 0
                && content.RentMultiplierBp != Fx.One)
            {
                TierConfig[] k = new TierConfig[_economy.TierCount];
                for (int i = 0; i < k.Length; i++)
                {
                    TierConfig t = _economy.TierAt(i);
                    k[i] = new TierConfig(
                        t.Tables,
                        Fx.Bp(t.Rent, content.RentMultiplierBp),
                        t.Upgrade, t.StaffCap, t.ReputationCapCenti, t.Plates);
                }
                _economy = _economy.WithTiers(k);
            }
            _content = content ?? throw new ArgumentNullException(nameof(content));
            _timing = timing ?? throw new ArgumentNullException(nameof(timing));
            _masterSeed = masterSeed;

            _tableParty = new int[MaxTables];
            _tableDirty = new bool[MaxTables];
            _tablePlates = new int[MaxTables];
            _pPlates = new int[MaxParties];
            _pCooked = new bool[MaxParties];

            _pActive = new bool[MaxParties];
            _pArchetype = new int[MaxParties];
            _pDishMain = new int[MaxParties];
            _pDishSide = new int[MaxParties];
            _pDishDrink = new int[MaxParties];
            _pDishDessert = new int[MaxParties];
            _pAskedDish = new int[MaxParties];
            _pSize = new int[MaxParties];
            _pStage = new CustomerStage[MaxParties];
            _pAttended = new bool[MaxParties];
            _pTable = new int[MaxParties];
            _pPatienceLeftMs = new int[MaxParties];
            _pPatienceTotalMs = new int[MaxParties];
            _pWaitedMs = new int[MaxParties];
            _pEatLeftMs = new int[MaxParties];
            _pSatisfactionCenti = new int[MaxParties];
            _pBonusCenti = new int[MaxParties];
            _pInTask = new bool[MaxParties];
            _pWarned = new bool[MaxParties];

            _hallTaskKind = new TaskKind[MaxServers];
            _hallTaskTarget = new int[MaxServers];
            _hallTaskLeftMs = new int[MaxServers];
            _stationTier = new int[content.Stations.Length];
            _stationBusy = new int[content.Stations.Length];
            _jobStation = new int[MaxParties * MaxJobsPerParty];
            _jobMs = new int[MaxParties * MaxJobsPerParty];
            _jobState = new int[MaxParties * MaxJobsPerParty];
            _jobPlates = new int[MaxParties * MaxJobsPerParty];
            _jobSlots = new int[MaxParties * MaxJobsPerParty];
            _pJobsLeft = new int[MaxParties];

            _kitchenTaskKind = new TaskKind[MaxServers];
            _kitchenTaskTarget = new int[MaxServers];
            _kitchenTaskLeftMs = new int[MaxServers];

            _arrTick = new int[MaxParties];
            _arrArchetype = new int[MaxParties];
            _arrSize = new int[MaxParties];

            _stockGrams = new int[content.Ingredients.Length];
            _stockAgeDays = new int[content.Ingredients.Length];
            _stockQualityCenti = new int[content.Ingredients.Length];
            _marketBp = new int[content.Ingredients.Length];
            for (int i = 0; i < _marketBp.Length; i++) _marketBp[i] = Fx.One;
            _dishPrice = new long[content.Dishes.Length];
            _dishOnMenu = new bool[content.Dishes.Length];
            _dishWasUnlocked = new bool[content.Dishes.Length];

            _stationUsed = new bool[content.Stations.Length];
            for (int i = 0; i < content.Dishes.Length; i++)
            {
                int st = content.Dishes[i].StationIndex;
                if (st >= 0 && st < _stationUsed.Length) _stationUsed[st] = true;
            }
            for (int i = 0; i < content.Dishes.Length; i++)
            {
                _dishPrice[i] = content.Dishes[i].Price;
                _dishOnMenu[i] = true;
            }

            ResetStreams();

            _day = 1;
            _phase = DayPhase.Morning;
            _tableCount = economy.TierAt(0).Tables;
            _platesClean = economy.TierAt(0).Plates;
            _reputationCenti = economy.StartingReputationCenti;
            _cash = economy.StartingCash;
            _cooks = 1;
            // The starting cook begins WITHOUT TRAITS but with morale.
            //
            // Two separate things. Morale: without this line the first
            // cook's morale started at 0 - BELOW the resignation threshold -
            // and the restaurant was left without a cook on the second day.
            //
            // Traits: the player DOES NOT CHOOSE the starting cook, the game
            // gives them one. Rolling random traits for them means rolling
            // an invisible die on the campaign's first day - and it was
            // measured: on a run that drew a badly-trait'd starting cook,
            // satisfaction leaks from 9,000 down to 5,000, the shop stays at
            // four tables and never recovers across the sixty days. The cook
            // you inherit is ORDINARY; character comes with the people you
            // CHOOSE.
            _cookMorale[0] = _economy.StartingMorale;
            _cookTraitA[0] = -1;
            _cookTraitB[0] = -1;

            // The cook you inherit has a name too - even without traits.
            // A person without a name is a number the player does not care
            // about.
            for (int i = 0; i < MaxServers; i++) { _cookName[i] = -1; _hallName[i] = -1; }
            if (content.StaffNames.Length > 0)
            {
                _cookName[0] = _rngName.NextInt(content.StaffNames.Length);
                _hallName[0] = _rngName.NextInt(content.StaffNames.Length);
            }

            // The candidate pool is full ON THE FIRST DAY too.
            //
            // It used to be built only at the opening of a day, and there is
            // no opening on the first day - the game already starts on the
            // morning of day one. The result: all six candidates showed the
            // arrays' zero default, that is, they were all the same person
            // carrying the same trait TWICE. All three looked exactly alike
            // on screen and there was no choice at all.
            for (int i = 0; i < CandidateSlots * 2; i++)
            {
                _candTraitA[i] = -1;
                _candTraitB[i] = -1;
            }
            RefreshCandidates();
            // The first day starts ALREADY OPEN, so OpenDay is never called.
            // Setting the allowance only there left day one short-changed.
            // THE BOOK'S DEBTORS START AT -1.
            //
            // The default for an int array is 0, and 0 is a valid regular
            // index: an empty account would be drawing on the trust of
            // somebody it had never met. It would be silent.
            for (int i = 0; i < MaxTabs; i++) _tabRegular[i] = -1;

            _interventionsLeft = economy.InterventionStart;   // OpenService sets it properly

            // THE CREW YOU INHERIT: ONE COOK, ONE WAITER.
            //
            // The hall used to be EMPTY and on the first day the owner did
            // the whole service alone. That is wrong for two reasons:
            //
            //   1. The game says "you are the owner, not the chef", but what
            //      the player sees at the opening is a one-person shop -
            //      themselves. A restaurant you take over has a waiter.
            //   2. As teaching: without seeing what a waiter does, the
            //      decision "I hired a waiter" cannot be understood. What is
            //      seen on the first day should be the thing that is
            //      multiplied later.
            //
            // As with the cook: morale is THE STARTING MORALE (otherwise
            // they are born below the resignation threshold and the hall
            // empties on the second day), there are NO traits (inherited
            // staff are ordinary; character comes with the people you
            // CHOOSE), but they do have a name.
            _hall = 1;
            _hallMorale[0] = _economy.StartingMorale;
            _hallTraitA[0] = -1;
            _hallTraitB[0] = -1;

            // The opening stock: enough to get through the first day; for
            // the second the player has to go to the market.
            //
            // ONE DAY'S WORTH, BY THE MENU. Six kilos used to be put in for
            // EVERY ONE of the seventy-seven ingredients - the ingredients
            // of dishes not on the menu, the ingredients of dishes not yet
            // unlocked, all of them. Since forty-four are perishable, the
            // lot went into the bin on the first night: 14,713 coins, more
            // than three times the player's starting till.
            //
            // As long as it was invisible it was taken for harmless. It came
            // out when the "thrown out" line was added to the evening
            // report: at the end of the very first day, having bought
            // nothing, the player would see a large spoilage figure taken
            // out of their till.
            RestockForOneDay();

            // The dishes open on the first day ARE NOT ANNOUNCED: the player
            // can already see them on the menu. Filling the flag now stops
            // the screen filling up with seventeen "new dish" notifications
            // on day one.
            for (int i = 0; i < _content.Dishes.Length; i++)
                _dishWasUnlocked[i] = Unlocked(i);

            // Let the first day's market prices MOVE as well.
            //
            // RollMarket used to be called only at the opening of a day, and
            // there is no opening on the first day - so on the first economy
            // screen the player saw, "today's price" and "the yearly
            // average" were identical on every line. There was no telling
            // why the two columns existed.
            RollMarket();

            for (int i = 0; i < MaxTables; i++) _tableParty[i] = -1;

            // THE PHOTOGRAPH OF WEEK ZERO.
            //
            // Without this line the first report's difference would be the
            // axes THEMSELVES: last week would count as zero and on day
            // seven the player would see a jump such as "Place +33" that
            // they had not made. The score of the shop they inherited is not
            // something they earned.
            SeasonScore opening = Score();
            for (int i = 0; i < SeasonScore.AxisCount; i++)
            {
                _weekAxis[i] = opening.AxisAt(i);
                _weekAxisPrev[i] = _weekAxis[i];
            }
        }

        private void ResetStreams()
        {
            _rngArrival = RngSeeder.Create(_masterSeed, RngStream.Arrival);
            _rngArchetype = RngSeeder.Create(_masterSeed, RngStream.Archetype);
            _rngOrder = RngSeeder.Create(_masterSeed, RngStream.Order);
            _rngStaffError = RngSeeder.Create(_masterSeed, RngStream.StaffError);
            _rngMarket = RngSeeder.Create(_masterSeed, RngStream.Market);
            _rngCredit = RngSeeder.Create(_masterSeed, RngStream.Credit);
            _rngRegular = RngSeeder.Create(_masterSeed, RngStream.Regular);
            _rngHiring = RngSeeder.Create(_masterSeed, RngStream.Hiring);
            _rngEvent = RngSeeder.Create(_masterSeed, RngStream.Event);
            _rngName = RngSeeder.Create(_masterSeed, RngStream.Name);
        }

        // ---- the read surface ------------------------------------------------
        public long TickIndex { get { return _tickIndex; } }
        public int Day { get { return _day; } }
        public DayPhase Phase { get { return _phase; } }
        public int ServiceTick { get { return _serviceTick; } }

        /// <summary>
        /// The progress of service, IN TEN-THOUSANDTHS (0-10000).
        ///
        /// AN INTEGER: the core neither takes nor returns floating point
        /// (docs/23) and a test checks that mechanically - the first draft
        /// returned a float and the test went red, RIGHTLY. Turning the
        /// ratio into floating point is the view's job.
        ///
        /// The view turns this into the time of day (DayLight): the
        /// direction of the shadows, the colour of the light and the street
        /// lamps are all read from this one number.
        /// </summary>
        public int ServiceProgressBp
        {
            get
            {
                int n = _timing.ServiceTicks;
                if (n <= 0) return 0;
                long t = (long)_serviceTick * 10000L / n;
                return t < 0L ? 0 : (t > 10000L ? 10000 : (int)t);
            }
        }
        public int TableCount { get { return _tableCount; } }

        /// <summary>How many tables are occupied right now. For the view and the music.</summary>
        public int OccupiedTables
        {
            get
            {
                int n = 0;
                for (int t = 0; t < _tableCount; t++) if (_tableParty[t] >= 0) n++;
                return n;
            }
        }

        /// <summary>Is there a party sitting at this table. For the view layer.</summary>
        public bool TableOccupied(int table)
        {
            return table >= 0 && table < _tableCount && _tableParty[table] >= 0;
        }

        /// <summary>
        /// How many people are sitting at this table. The view layer uses it
        /// to put figures on the chairs: putting a single figure at every
        /// occupied table made a party of four look like one person.
        /// </summary>
        public int TableGuests(int table)
        {
            if (table < 0 || table >= _tableCount) return 0;
            int p = _tableParty[table];
            return p >= 0 ? _pSize[p] : 0;
        }

        /// <summary>
        /// The STAGE of the party at this table. None at an empty table.
        ///
        /// The view layer draws each table's state above it. Without this
        /// the stage of service could not be followed: for eight minutes
        /// nothing on screen moved and no information flowed at all beyond
        /// the two numbers in the top strip.
        /// </summary>
        /// <summary>
        /// The index of the PARTY sitting at this table. -1 if the table is
        /// empty.
        ///
        /// THERE ARE TWO SEPARATE INDEX SPACES and mixing them up produces a
        /// silent bug: a table is 0..15 (MaxTables), a party is 0..255
        /// (MaxParties). The UI touches a table, whereas Intervene expects a
        /// PARTY. Without the translation the command "offer tea to table 3"
        /// goes to PARTY NUMBER 3 - as the day goes on, an entirely
        /// different table, or a party that left long ago; and then the
        /// command is silently rejected and the player still reads "tea was
        /// offered".
        /// </summary>
        public int PartyAtTable(int table)
        {
            if (table < 0 || table >= _tableCount) return -1;
            return _tableParty[table];
        }

        public CustomerStage TableStage(int table)
        {
            if (table < 0 || table >= _tableCount) return CustomerStage.None;
            int p = _tableParty[table];
            return p >= 0 ? _pStage[p] : CustomerStage.None;
        }

        /// <summary>
        /// The PATIENCE LEFT in the party at this table, in basis points
        /// (10000 = full).
        ///
        /// The patience warning was already being raised as an event and was
        /// visible nowhere; and yet it is the one piece of information the
        /// player's intervention decision rests on.
        /// </summary>
        /// <summary>
        /// The threshold at which patience counts as "critical", in basis
        /// points.
        ///
        /// The view asks for it to build the crisis strip: which table's
        /// patience is about to run out. Writing the threshold a second time
        /// into the UI would have let the two drift apart silently whenever
        /// the balance changed.
        /// </summary>
        public int PatienceWarnBp { get { return _timing.PatienceWarnBp; } }

        /// <summary>
        /// Is there any party whose patience has now fallen BELOW THE
        /// WARNING THRESHOLD.
        ///
        /// For the balance tool: the answer to "does intervening pay" turns
        /// on WHEN the allowance is spent. While the bot was burning it in
        /// the first eighty seconds of the day, what was being measured was
        /// not the mechanic but the bot playing badly.
        /// </summary>
        public bool AnyPartyCritical
        {
            get
            {
                for (int i = 0; i < MaxParties; i++)
                {
                    if (!_pActive[i] || _pPatienceTotalMs[i] <= 0) continue;
                    if (DrainRateBp(i) <= 0) continue;
                    long leftBp = Fx.MulDiv(_pPatienceLeftMs[i], Fx.One,
                                            _pPatienceTotalMs[i]);
                    if (leftBp <= _timing.PatienceWarnBp) return true;
                }
                return false;
            }
        }

        public int TablePatienceBp(int table)
        {
            if (table < 0 || table >= _tableCount) return 0;
            int p = _tableParty[table];
            if (p < 0 || _pPatienceTotalMs[p] <= 0) return 0;

            long bp = (long)_pPatienceLeftMs[p] * Fx.One / _pPatienceTotalMs[p];
            if (bp < 0) bp = 0;
            if (bp > Fx.One) bp = Fx.One;
            return (int)bp;
        }

        /// <summary>Is this table waiting to be cleared.</summary>
        public bool TableDirty(int table)
        {
            return table >= 0 && table < _tableCount && _tableDirty[table];
        }
        public int ReputationCenti { get { return _reputationCenti; } }

        /// <summary>
        /// The highest reputation reachable at this tier. The UI has to show
        /// it: a player pressed against the ceiling should know why it no
        /// longer goes up.
        /// </summary>
        public int ReputationCapCenti
        {
            get
            {
                int cap = _economy.TierForTables(_tableCount).ReputationCapCenti;
                return cap <= 0 || cap > 10000 ? 10000 : cap;
            }
        }
        /// <summary>
        /// The reputation LOST at the ceiling accumulates here, in
        /// centi-points.
        ///
        /// The ceiling is the right idea - a four-table shop cannot be the
        /// restaurant the whole neighbourhood talks about - but DELETING the
        /// overflow did one more thing: for a player at the ceiling there
        /// was no measurable difference left between a PERFECT day and a day
        /// that merely GOT BY. Measured: a good player presses against 75 at
        /// seven tables and stands there for thirty-two days - more than
        /// half the campaign, unrequited.
        ///
        /// The same rule had been measured in the price gap: IF AN AXIS IS
        /// CLAMPED, EVERY PRICE PAID INTO THAT AXIS IS FREE ABOVE THE
        /// CEILING. This is its mirror image - what was EARNED above the
        /// ceiling was being given away free too.
        /// </summary>
        private int _reputationOverflowCenti;

        /// <summary>The reputation banked at the ceiling. For the UI and the tour.</summary>
        public int ReputationOverflowCenti { get { return _reputationOverflowCenti; } }

        // =====================================================================
        // BADGES AND THE WEEKLY REPORT
        //
        // The two close the same gap: the game scored on seven axes and the
        // player saw them EXACTLY ONCE - on the sixtieth day. You cannot
        // feel progress in something you cannot see. The weekly report takes
        // that single moment of achievement up to nine; and the badges name
        // what was achieved on the days in between.
        //
        // Why WEEKLY rather than daily: daily would be noise (the axes do
        // not move in a day) and the game's own rhythm is weekly already -
        // wages and rent are paid weekly, and the peak falls two days a week.
        // =====================================================================

        /// <summary>The bit mask of badges earned.</summary>
        private int _badges;

        /// <summary>Those earned TODAY. The evening screen highlights these.</summary>
        private int _badgesToday;

        /// <summary>
        /// Has the book ever been opened. Without this the "book closed"
        /// badge would be SILENTLY wrong: a player who has never given
        /// credit also has an outstanding tab of zero, so the badge would be
        /// handed out on the first day of its own accord.
        /// </summary>
        private bool _creditEverOpened;

        /// <summary>This week's axes; filled at the end of the week.</summary>
        private readonly int[] _weekAxis = new int[SeasonScore.AxisCount];

        /// <summary>Last week's axes. The difference comes out of these two.</summary>
        private readonly int[] _weekAxisPrev = new int[SeasonScore.AxisCount];

        /// <summary>The day the report was issued; 0 means it never was.</summary>
        private int _weekReportDay;

        public int BadgeCount { get { return Badges.Count; } }

        /// <summary>Has the badge been earned.</summary>
        public bool HasBadge(int i)
        {
            return i >= 0 && i < Badges.Count && (_badges & (1 << i)) != 0;
        }

        /// <summary>Was the badge earned TODAY.</summary>
        public bool BadgeEarnedToday(int i)
        {
            return i >= 0 && i < Badges.Count && (_badgesToday & (1 << i)) != 0;
        }

        /// <summary>How many badges have been earned.</summary>
        public int BadgesEarned
        {
            get
            {
                int n = 0;
                for (int i = 0; i < Badges.Count; i++) if (HasBadge(i)) n++;
                return n;
            }
        }

        /// <summary>
        /// Did the weekly report come out on this day. The evening screen
        /// asks this; asking "Day % 7 == 0" would be WRONG, because the
        /// report comes out at the CLOSE of the day and the player sees it
        /// on the evening screen.
        /// </summary>
        public bool WeekReportReady { get { return _weekReportDay > 0 && _weekReportDay == _day; } }

        /// <summary>Which week the report is for.</summary>
        public int WeekNumber { get { return _weekReportDay / 7; } }

        public int WeekAxis(int i)
        {
            return i >= 0 && i < SeasonScore.AxisCount ? _weekAxis[i] : 0;
        }

        /// <summary>The difference against last week. This is the whole point of the report.</summary>
        public int WeekAxisDelta(int i)
        {
            return i >= 0 && i < SeasonScore.AxisCount
                ? _weekAxis[i] - _weekAxisPrev[i] : 0;
        }

        public long Cash { get { return _cash; } }
        public int Cooks { get { return _cooks; } }
        public int HallStaff { get { return _hall; } }

        /// <summary>How many hall staff are dedicated to the sink.</summary>
        public int Dishwashers { get { return _dishwashers; } }

        /// <summary>The number of clean plates.</summary>
        public int PlatesClean { get { return _platesClean; } }

        /// <summary>The number of dirty plates waiting at the sink.</summary>
        public int PlatesDirty { get { return _platesDirty; } }

        /// <summary>The number of plates on tables and in service.</summary>
        public int PlatesInUse { get { return _platesInUse; } }

        /// <summary>The restaurant's total plates.</summary>
        public int PlatesTotal
        {
            get { return _economy.TierForTables(_tableCount).Plates; }
        }

        /// <summary>
        /// The ticks the kitchen spent waiting because there was no clean
        /// plate.
        ///
        /// THE MEASURE of the sentence "the washing-up was neglected". If it
        /// is above zero, service really did stop.
        /// </summary>
        public int PlateBlockedTicks { get { return _plateBlockedTicks; } }

        /// <summary>The plates washed today.</summary>
        public int PlatesWashedToday { get { return _washedToday; } }

        /// <summary>
        /// How many times TODAY the hall staff dropped their work and ran to
        /// the sink. The dishwasher's measurable effect is in this number.
        /// </summary>
        public int HallRushWashes { get { return _hallRushWashes; } }

        /// <summary>How many trips to the sink were made because the kitchen had stopped.</summary>
        public int HallCrisisWashes { get { return _hallCrisisWashes; } }

        /// <summary>The plates dirtied today (cumulative).</summary>
        public int PlatesDirtiedToday { get { return _dirtiedToday; } }
        public int ActiveParties { get { return _partyCount; } }
        public int ServedParties { get { return _servedParties; } }
        public int ServedPeople { get { return _servedPeople; } }
        /// <summary>
        /// How many parties currently have PATIENCE RUNNING - that is, are
        /// waiting for something.
        ///
        /// This set is the tea's target: the tea goes out into the hall, and
        /// in a hall with nobody waiting there is nobody to send it to. The
        /// UI reads this and disables the button, or the player would be
        /// trying to burn an intervention in an empty hall.
        /// </summary>
        public int WaitingParties
        {
            get
            {
                int n = 0;
                for (int i = 0; i < MaxParties; i++)
                    if (_pActive[i] && DrainRateBp(i) > 0) n++;
                return n;
            }
        }

        public int AngryParties { get { return _angryParties; } }

        /// <summary>How many parties left a table angry today.</summary>
        public int AngrySeatedParties { get { return _angrySeated; } }

        /// <summary>
        /// Parties TURNED BACK AT THE DOOR because there was nothing on the
        /// menu that could be made for them.
        ///
        /// It was in the day report but COULD NOT BE ASKED FOR during
        /// service - so the commonest way of dying in the first week was
        /// only visible to the player once the day was over, not in the
        /// hours when they could still fix it.
        /// </summary>
        public int TurnedAwayParties { get { return _turnedAwayParties; } }
        public long Revenue { get { return _revenue; } }

        /// <summary>
        /// Today's mean satisfaction (in centi). Weighted by people.
        ///
        /// The day report was already working this out, but ONLY at the end
        /// of the day: the player could read nowhere how it was going while
        /// it went. The same division is meaningful during service too - 0
        /// if nobody has been served.
        /// </summary>
        public int AverageSatisfactionCenti
        {
            get
            {
                return _servedPeople > 0
                    ? (int)(_satisfactionSum / _servedPeople) : 0;
            }
        }
        public int PlannedArrivals { get { return _arrCount; } }
        public EventBuffer Events { get { return _events; } }
        public long TotalWagesPaid { get { return _weeklyWagesPaid; } }

        /// <summary>
        /// The total revenue ACROSS THE CAMPAIGN. _revenue is zeroed every
        /// day.
        ///
        /// The balance tool was SUMMING revenue from the day reports, and
        /// that produced a boundary leftover: AdvanceToNextDay() collects the
        /// tabs that have fallen due, but that call runs AFTER the day's
        /// report has been taken - so the last day's collection went into no
        /// report at all and the income statement could not be reconciled
        /// for a player using the signature mechanic. Wages and rent were
        /// already being read cumulatively; revenue should be read the same
        /// way.
        /// </summary>
        public long TotalRevenue { get { return _revenueAll; } }
        public long TotalRentPaid { get { return _weeklyRentPaid; } }
        /// <summary>The day the till first went negative; 0 means it never did.</summary>
        public int FirstDebtDay { get { return _firstDebtDay; } }

        /// <summary>
        /// How many days are left until the rent-and-wages day. 0 if it falls
        /// today.
        ///
        /// docs/02 declares the weekly rent "the metronome of the pressure":
        /// "from the fourth day on, the player starts thinking about Friday."
        /// In the game there WAS no such Friday - the money left the till and
        /// the player only noticed once the number had dropped. The metronome
        /// was silent.
        /// </summary>
        public int DaysToRent
        {
            get
            {
                int n = _economy.RentDayInterval;
                if (n <= 0) return 0;
                int r = _day % n;
                return r == 0 ? 0 : n - r;
            }
        }

        /// <summary>The rent and wages due to be paid this week, together.</summary>
        public long WeeklyBill
        {
            get
            {
                Crew crew = new Crew(_cooks, _hall);
                long wages = StaffingModel.WeeklyWageBill(crew, _day / 7, _economy);
                wages = Fx.MulDiv(wages, TraitWageMultiplierBp(), Fx.One);
                return wages + _economy.TierForTables(_tableCount).Rent + _loanInstallment;
            }
        }

        /// <summary>How many times the ladder down was climbed.</summary>
        public int DebtRungs { get { return _debtRungs; } }

        /// <summary>The total value of the stock thrown out, in centi.</summary>
        public long SpoiledValue { get { return _spoiledValue; } }

        /// <summary>The total cash spent on ingredients, in centi.</summary>
        public long IngredientSpend { get { return _ingredientSpend; } }

        /// <summary>The total the ladder down put into the till, in centi.</summary>
        public long RescueValue { get { return _rescueValue; } }

        /// <summary>The principal of the loans taken, in centi.</summary>
        public long LoanTaken { get { return _loanTaken; } }

        /// <summary>The total instalments paid on all loans, in centi.</summary>
        public long LoanRepaid { get { return _loanRepaidAll; } }

        /// <summary>The total paid for equipment, in centi.</summary>
        public long EquipmentSpend { get { return _equipmentSpend; } }

        /// <summary>The total paid for expansion, in centi.</summary>
        public long ExpansionSpend { get { return _expansionSpend; } }

        /// <summary>The highest number of people served in a single day.</summary>
        /// <summary>
        /// What share of main-dish orders turned into a combo, in basis
        /// points.
        ///
        /// The raw form of the signature axis. The balance tool prints this
        /// so that the axis's TARGET comes out of a measurement - an invented
        /// target makes the axis either saturated or unreachable ("a reward
        /// paid into a saturated axis is invisible").
        /// </summary>
        /// <summary>The total spent on the tab's tea, in centi.</summary>
        public long TeaSpend { get { return _teaSpend; } }

        public int ComboShareBp
        {
            get
            {
                if (_mainOrders <= 0) return 0;
                return (int)((long)_comboOrders * Fx.One / _mainOrders);
            }
        }

        /// <summary>The length of the campaign, in days.</summary>
        public int CampaignDays { get { return _economy.CampaignDays; } }

        /// <summary>
        /// The campaign is up and the evaluation HAS NOT YET BEEN SHOWN.
        ///
        /// The view layer sees this, opens the screen and calls
        /// MarkSeasonScored. The flag goes into the save, so the same screen
        /// does not come up again on the second launch.
        /// </summary>
        public bool SeasonJustEnded
        {
            get { return !_seasonScored && _day > _economy.CampaignDays; }
        }

        public void MarkSeasonScored() { _seasonScored = true; }

        /// <summary>
        /// Is the campaign over. Unlike SeasonJustEnded, this one NEVER
        /// CLOSES again, so the evaluation screen can always be reopened
        /// from the menu.
        /// </summary>
        public bool SeasonOver { get { return _day > _economy.CampaignDays; } }

        /// <summary>
        /// The year-end evaluation. The seven axes of docs/08-endgame.md.
        ///
        /// Each axis is 0-100 and none stands in for another: it should be
        /// possible to score well by growing and equally by running a small
        /// but well-loved shop.
        ///
        /// The measures are THE GAME'S OWN NUMBERS, not a separate score
        /// economy. Inventing a separate scale would have pulled apart the
        /// thing the player watches while playing and the thing that is
        /// rewarded at the end of the year.
        /// </summary>
        /// <summary>How many beats are written per regular (docs/13).</summary>
        private const int StoryBeatsPerRegular = 3;

        public SeasonScore Score()
        {
            // --- wealth: the till + the book + WHAT IS OWNED
            //
            // INVESTMENT IS NO LONGER PUNISHED. Only the till and the book
            // used to be counted: as equipment was bought the "Wealth" bar
            // GOT SHORTER, so the thing the game encourages came back as a
            // penalty on the report. What the player saw was this - their
            // score falls as they play well, and no screen says why.
            //
            // The value of what is owned is found by subtracting WHAT IS
            // LEFT from the whole catalogue. I deliberately did not write a
            // separate summation: two separate calculations drift apart one
            // day and there is no telling which is right (the same mistake
            // was lived through once in this file - RemainingPurchaseCost
            // counted the cold store and the other one did not).
            long yardstick = RemainingPurchaseCostTotal();
            long owned = yardstick - RemainingPurchaseCost();
            if (owned < 0) owned = 0;
            long worth = _cash + OpenCredit + owned;
            int wealth = yardstick > 0 ? (int)(worth * 100 / yardstick) : 100;

            // --- reputation: straight through
            int reputation = _reputationCenti / 100;

            // --- regulars: THE DEPTH OF THE RELATIONSHIP
            //
            // Not "did they drop in" but "how many of their beats did you
            // open". The first measure looked only at visits, and because
            // everyone drops in at least once in sixty days the axis came out
            // at 100 FOR EVERYBODY - that is, it measured nothing. Opening a
            // beat, by contrast, takes work: they have to come often and
            // leave satisfied.
            int beats = 0;
            for (int i = 0; i < RegularCount; i++)
            {
                int b = _regBeat[i];
                beats += b > StoryBeatsPerRegular ? StoryBeatsPerRegular : b;
            }
            int regulars = RegularCount > 0
                ? beats * 100 / (RegularCount * StoryBeatsPerRegular) : 0;

            // --- crew: how full the roster is and its morale, half and half
            int cap = StaffCap;
            int head = _cooks + _hall;
            int fill = cap > 0 ? head * 100 / cap : 0;
            int crew = head > 0 ? (fill + AverageMorale()) / 2 : 0;

            // --- place: tables, against the top tier
            int topTables = _economy.TierAt(_economy.TierCount - 1).Tables;
            int place = topTables > 0 ? _tableCount * 100 / topTables : 0;

            // --- resilience: never climbing down the ladder is full marks
            //
            // The most valuable side effect of docs/08: climbing down the
            // ladder is now a CUMULATIVE price rather than a momentary one.
            // Each rung is 30 points.
            int resilience = 100 - _debtRungs * 30;
            if (_firstDebtDay > 0 && _debtRungs == 0) resilience -= 15;

            return new SeasonScore(wealth, reputation, regulars, crew,
                                   place, resilience, SignatureAxis());
        }

        /// <summary>What the whole campaign would come to if it were all up for sale.</summary>
        private long RemainingPurchaseCostTotal()
        {
            long total = 0;
            for (int t = 1; t < _economy.TierCount; t++)
                total += _economy.TierAt(t).Upgrade;

            // A STATION NO DISH USES IS NOT SOMETHING YOU CAN BUY.
            //
            // This counted every station in the content. In the Turkish
            // restaurant no dish uses the oven, so its ladder went into the
            // denominator of the WEALTH axis - and since `owned` is the
            // yardstick minus what is left, buying the useless oven RAISED
            // the score. Six thousand coins for a better mark and nothing
            // else. RequiredStationTier already skipped unused stations; the
            // two answers to "what is there to buy" simply disagreed.
            for (int i = 0; i < _content.Stations.Length; i++)
            {
                if (!IsStationUsed(i)) continue;
                StationDef def = _content.Stations[i];
                for (int t = 1; t < def.Tiers.Length; t++) total += def.Tiers[t].Price;
            }

            // THE COLD STORE IS SOMETHING YOU CAN BUY TOO.
            //
            // It had been skipped here, while RemainingPurchaseCost() - the
            // other function asking the same question - WAS counting it. Two
            // functions gave two different answers to "what is left to buy",
            // and the denominator of the year-end WEALTH axis was using the
            // smaller one: the player's wealth was being measured not
            // against everything they could buy but against part of it.
            if (_content.Storage != null)
                for (int t = 1; t < _content.Storage.Tiers.Length; t++)
                    total += _content.Storage.Tiers[t].Price;

            return total;
        }

        private int AverageMorale()
        {
            int sum = 0, n = 0;
            for (int i = 0; i < _cooks && i < MaxServers; i++) { sum += _cookMorale[i]; n++; }
            for (int i = 0; i < _hall && i < MaxServers; i++) { sum += _hallMorale[i]; n++; }
            return n > 0 ? sum / n : 0;
        }

        /// <summary>
        /// The cuisine-specific axis. The measure ITSELF is in the code, its
        /// TARGET is in the content (content/cuisines/*.json scoreAxis) -
        /// docs/23 8.2.
        /// </summary>
        private int SignatureAxis()
        {
            ScoreAxisDef axis = _content.ScoreAxis;
            if (axis == null || axis.Target <= 0) return 100;

            switch (axis.Kind)
            {
                case "comboShare":
                    // WHAT SHARE OF MAIN-DISH ORDERS BECAME A COMBO.
                    //
                    // Fast food's axis was `peakCovers` for a while, and it
                    // WAS MEASURED that it was tracking EXPANSION rather than
                    // the signature: a bot that switched the combo on every
                    // morning and a bot that never switched it on scored the
                    // same (38 / 38), while the highest scores belonged to
                    // whoever opened the most tables - that is, the axis was
                    // a copy of the "Place" axis. docs/08 says of that axis
                    // that it "rewards the signature mechanic DIRECTLY", and
                    // this was the thing it was not saying.
                    //
                    // This measure measures a DECISION: the combo lifts the
                    // average ticket but raises the kitchen load too, so
                    // switching it off at the peak is a legitimate play.
                    // Never switching it on is not zero, but it is not full
                    // marks either.
                    //
                    // If the denominator is zero (no main dish sold at all)
                    // the score is 0: even if no OPPORTUNITY to use the
                    // mechanic arose, a year in which no food was sold to
                    // anybody does not deserve a signature score.
                    if (_mainOrders <= 0) return 0;
                    int comboBp = (int)((long)_comboOrders * Fx.One / _mainOrders);
                    return comboBp * 100 / axis.Target;

                case "creditCollected":
                    // Never opening a tab is NOT full marks: never using the
                    // mechanic cannot count the same as using it well.
                    if (_creditIssued <= 0) return 0;
                    int rateBp = (int)(_creditCollected * Fx.One / _creditIssued);
                    return rateBp * 100 / axis.Target;

                default:
                    return 100;
            }
        }

        private bool _seasonScored;
        public int StaffCap { get { return _economy.TierForTables(_tableCount).StaffCap; } }
        public long UpgradeCostFor(int tierIndex) { return _economy.TierAt(tierIndex).Upgrade; }
        public int TablesAtTier(int tierIndex) { return _economy.TierAt(tierIndex).Tables; }
        public int TierCount { get { return _economy.TierCount; } }
        public int DishCount { get { return _content.Dishes.Length; } }

        /// <summary>The market price from the content. Not the price the player set.</summary>
        public long BasePriceOf(int dishIndex)
        {
            if (dishIndex < 0 || dishIndex >= _content.Dishes.Length) return 0;
            return _content.Dishes[dishIndex].Price;
        }

        public CustomerStage StageOf(int party) { return _pStage[party]; }

        // =====================================================================
        // READ-ONLY QUESTIONS FOR THE VIEW.
        //
        // They say not WHERE anybody is STANDING in the hall but what each
        // one is BUSY WITH. Position is the view layer's job; the simulation
        // does not know metres and must not (docs/23: no Unity in the core,
        // no floating point). These accessors DO NOT CHANGE state.

        /// <summary>Which table the party is at; -1 if they have not sat down yet.</summary>
        public int TableOfParty(int party)
        {
            if (party < 0 || party >= MaxParties) return -1;
            for (int t = 0; t < _tableCount && t < MaxTables; t++)
                if (_tableParty[t] == party) return t;
            return -1;
        }

        /// <summary>
        /// Which table the hall worker is seeing to right now; -1 if idle.
        ///
        /// Worker number 0 is THE OWNER themselves (DispatchHall: "the owner
        /// works the hall too"), so they walk about on screen as well.
        /// </summary>
        public int HallTaskTable(int server)
        {
            if (server < 0 || server >= MaxServers) return -1;
            TaskKind k = _hallTaskKind[server];
            if (k == TaskKind.None) return -1;
            // The target of a clearing task is already a TABLE; for the
            // rest the target is a PARTY.
            if (k == TaskKind.Clear) return _hallTaskTarget[server];
            return TableOfParty(_hallTaskTarget[server]);
        }

        /// <summary>
        /// HOW MANY PLATES are cooking at this station right now. 0 means the
        /// station is idle.
        ///
        /// The view turns this into the flame on the hob and the lamp in the
        /// oven: a working hob should be lit, an idle one should not. The
        /// number itself is information too - a hob with two plates on it and
        /// a hob with six should not look the same.
        /// </summary>
        public int StationLoad(int station)
        {
            if (station < 0 || station >= _stationBusy.Length) return 0;
            return _stationBusy[station];
        }

        /// <summary>
        /// How many plates this station can work on at once, at its current
        /// equipment tier.
        ///
        /// The load on its own is a number without a scale: three plates on a
        /// one-slot hob is a jam, three on a four-slot range is a quiet
        /// morning. The view needs both to draw a meter rather than a lamp.
        /// </summary>
        public int StationSlotCount(int station)
        {
            if (station < 0 || station >= _stationTier.Length) return 0;
            return StationSlots(station);
        }

        /// <summary>
        /// Is the hall worker CARRYING FOOD right now.
        ///
        /// The view turns this into a plate in the waiter's hand: serving is
        /// a management game's main verb, and without seeing it the player
        /// cannot follow what is going on. Taking a payment, taking an order
        /// and clearing a table need nothing in the hands.
        /// </summary>
        /// <summary>
        /// Is the hall worker AT THE SINK right now.
        ///
        /// A separate question for the view: a washing task's target is not a
        /// table (HallTaskTable returns -1) and -1 is the same number as
        /// "idle". Without telling the two apart, a waiter who is washing is
        /// sent home and the player never sees the washing-up being done.
        /// </summary>
        public bool HallWashing(int server)
        {
            if (server < 0 || server >= MaxServers) return false;
            return _hallTaskKind[server] == TaskKind.Wash;
        }

        public bool HallCarrying(int server)
        {
            if (server < 0 || server >= MaxServers) return false;
            return _hallTaskKind[server] == TaskKind.Serve;
        }

        /// <summary>Which station the cook is working at right now; -1 if idle.</summary>
        public int CookTaskStation(int cook)
        {
            if (cook < 0 || cook >= MaxServers) return -1;
            if (_kitchenTaskKind[cook] != TaskKind.Cook) return -1;
            int job = _kitchenTaskTarget[cook];
            if (job < 0 || job >= _jobStation.Length) return -1;
            return _jobStation[job];
        }
        public bool PartyActive(int party) { return _pActive[party]; }
        public int PartyArchetype(int party) { return _pArchetype[party]; }
        public long DishPrice(int dish) { return _dishPrice[dish]; }

        /// <summary>The service window is up and nobody is left in the hall.</summary>
        public bool ServiceComplete
        {
            get { return _serviceTick >= _timing.ServiceTicks && _partyCount == 0; }
        }

        // ====================================================================
        // Commands
        // ====================================================================
        public void Apply(in Command c)
        {
            // Every command applied since the start of the day is written
            // into the log. Speed, pause and camera are NOT commands: those
            // are view state.
            // THE STAGE COMMANDS ARE EXEMPT FROM THE LIMIT.
            //
            // Opening service and closing the day are not the player's
            // "actions" but the progress of the day. Rejecting them means a
            // soft lock: the day never closes, the next day cannot be
            // reached, and the tabs falling due are never settled. That is
            // exactly what happened, and two signature mechanic tests caught
            // it.
            bool phase = c.Kind == CommandKind.OpenService || c.Kind == CommandKind.CloseDay;

            if (_commandCount < MaxCommandsPerDay)
            {
                _commandLog[_commandCount++] = c;
            }
            else if (!phase)
            {
                // THE RETURN IS ESSENTIAL. It used to only raise the event
                // while the command was processed ANYWAY: the state changed
                // but it did not go into the log. That silently broke the
                // contract "the same seed + the same command log = the same
                // state" (docs/23 7) - a replay from the save diverged.
                // 12 = THE DAY'S COMMAND ALLOWANCE IS SPENT. The UI looks at
                // this number and says why (Notices.cs), so the number is a
                // UI CONTRACT - it must not be given to another reason.
                Emit(SimEventKind.CommandRejected, (int)c.Kind, 12);
                return;
            }

            switch (c.Kind)
            {
                case CommandKind.OpenService: OpenService(); break;
                case CommandKind.CloseDay: CloseDay(); break;
                case CommandKind.SetPrice: SetPrice(c.A, c.B); break;
                case CommandKind.SetMenuSlot: SetMenuSlot(c.A, c.B != 0); break;
                case CommandKind.BuyEquipment: BuyEquipment(c.A); break;
                case CommandKind.BuyStorage: BuyStorage(); break;
                case CommandKind.SetQuality: SetQuality(c.A); break;
                case CommandKind.ExtendCredit: ExtendCredit(c.A); break;
                case CommandKind.CollectCredit: CollectCredit(c.A); break;
                case CommandKind.SetCombo: SetCombo(c.A != 0); break;
                case CommandKind.Hire: Hire(c.A, c.B); break;
                case CommandKind.SetDishwashers: SetDishwashers(c.A); break;
                case CommandKind.Fire: Fire(c.A, c.B); break;
                case CommandKind.OrderIngredient: OrderIngredient(c.A, c.B); break;
                case CommandKind.OrderRecommended: OrderRecommended(); break;
                case CommandKind.TakeLoan: TakeLoan(c.A); break;
                case CommandKind.Intervene: Intervene(c.A, (InterventionKind)c.B); break;
                case CommandKind.Expand: Expand(c.A); break;
                case CommandKind.LastOrders: LastOrders(); break;
                default:
                    Emit(SimEventKind.CommandRejected, (int)c.Kind, 1);
                    break;
            }
        }

        private void OpenService()
        {
            if (_phase != DayPhase.Morning)
            {
                Emit(SimEventKind.CommandRejected, (int)CommandKind.OpenService, 2);
                return;
            }
            _phase = DayPhase.Service;
            _serviceTick = 0;
            // THE POOL IS SET WHERE SERVICE BEGINS, not where the day turns.
            //
            // The day-turn is the wrong place and the first version used it:
            // on day one nothing has turned over yet, so the pool still held
            // whatever the constructor left and the opening day ran on the
            // old day-budget number. The tour would not have caught it - it
            // plays day one like any other - and the harness would have
            // measured the new mechanic with the old one's first day in it.
            _interventionsLeft = _economy.InterventionStart;
            _interventionMs = 0;
            _doorsClosed = false;
            BuildArrivalPlan();
            Emit(SimEventKind.ServiceOpened, _day);
        }

        /// <summary>
        /// Shuts the door. Nobody inside is touched and the day ends when the
        /// room empties, as it always did.
        /// </summary>
        private void LastOrders()
        {
            if (_phase != DayPhase.Service)
            {
                Emit(SimEventKind.CommandRejected, (int)CommandKind.LastOrders, 2);
                return;
            }
            if (_doorsClosed)
            {
                // Pressing it twice is not a second decision, and it must not
                // read as one.
                Emit(SimEventKind.CommandRejected, (int)CommandKind.LastOrders, 10);
                return;
            }
            _doorsClosed = true;
            Emit(SimEventKind.LastOrders, _arrCount - _arrNext, _serviceTick);
        }

        private void CloseDay()
        {
            if (_phase != DayPhase.Service)
            {
                Emit(SimEventKind.CommandRejected, (int)CommandKind.CloseDay, 2);
                return;
            }

            // Whoever is left in the hall leaves angry.
            for (int i = 0; i < MaxParties; i++)
                if (_pActive[i]) LeaveAngry(i);

            ApplyReputation();
            SpoilPerishables();
            GainExperience();
            PayWeeklyCostsIfDue();
            UpdateMorale();

            // THE BADGES ARE CHECKED AFTER EVERYTHING HAS RUN.
            //
            // The order matters: had "the first ten thousand in the till"
            // been checked before the wages and rent were paid, the player
            // would look rich for a moment before the week's bill and would
            // take the badge WITHOUT EARNING IT.
            EvaluateBadges();
            WeeklyReportIfDue();

            _phase = DayPhase.Evening;
            Emit(SimEventKind.DayClosed, _day);
        }

        /// <summary>
        /// Which badges were earned today.
        ///
        /// All of them look BACKWARDS: what is checked is what the player
        /// did, not what they were asked to do. That is why none of them can
        /// upset the player's plan - a player deliberately running
        /// short-handed would have missed a quest; here they take a badge.
        /// </summary>
        private void EvaluateBadges()
        {
            // If the book has ever been opened we remember it PERMANENTLY:
            // the badge's condition is not "the outstanding tab is zero" but
            // "the book was opened and closed". Without the flag the badge
            // would be handed out on the first day.
            if (OpenCredit > 0) _creditEverOpened = true;

            bool peak = IsWeekend(_day);
            bool noneAngry = _angrySeated == 0;

            // 1. Nobody went hungry at the peak.
            if (peak && noneAngry && _turnedAwayParties == 0 && _servedParties > 0)
                Earn(Badges.EverybodyFed);

            // 2. Got through the peak short-handed.
            //
            // "Short" = BELOW the crew required. It counts if even one of the
            // two pools is short: in the game's trade-off, working one head
            // short is working one head short.
            if (peak && noneAngry && _servedParties > 0)
            {
                Crew required = RequiredCrewToday();
                if (_cooks < required.Cooks || _hall < required.Hall)
                    Earn(Badges.PeakShortHanded);
            }

            // 3. The book was closed.
            if (_creditEverOpened && OpenCredit == 0) Earn(Badges.TabBookClosed);

            // 4. The first story beat.
            for (int i = 0; i < RegularCount; i++)
                if (_regBeat[i] > 0) { Earn(Badges.FirstStoryBeat); break; }

            // 5. The first ten thousand in the till.
            if (_cash >= Badges.CashMilestone) Earn(Badges.FirstTenThousand);

            // 6. The shop grew.
            if (_tableCount > _economy.TierAt(0).Tables) Earn(Badges.FirstExpansion);

            // 7. Reputation 90. It requires expanding first - reputation is
            // clamped to the ceiling of the table tier.
            if (_reputationCenti >= Badges.ReputationMilestoneCenti)
                Earn(Badges.TalkOfTheNeighbourhood);
        }

        /// <summary>
        /// Award the badge - ONLY the first time. Marking it "earned today" a
        /// second time would be showing the player the same thing as new
        /// every week, and it would reduce a badge's worth to nothing.
        /// </summary>
        private void Earn(int badge)
        {
            int bit = 1 << badge;
            if ((_badges & bit) != 0) return;
            _badges |= bit;
            _badgesToday |= bit;
            Emit(SimEventKind.BadgeEarned, badge);
        }

        /// <summary>
        /// Photographs the seven axes if the week is up.
        ///
        /// Score() is a PURE function of the current state - it runs on any
        /// day of the campaign, so no new calculation had to be written for
        /// the report. Two separate calculations would drift apart one day
        /// and there would be no telling which was right.
        /// </summary>
        private void WeeklyReportIfDue()
        {
            if (_day % 7 != 0) return;

            SeasonScore s = Score();
            for (int i = 0; i < SeasonScore.AxisCount; i++)
            {
                _weekAxisPrev[i] = _weekAxis[i];
                _weekAxis[i] = s.AxisAt(i);
            }
            _weekReportDay = _day;
        }

        /// <summary>
        /// docs/14: 1 point for every day worked. It is awarded at the CLOSE
        /// of the day, so somebody hired today spends today's service as a
        /// novice.
        /// </summary>
        private void GainExperience()
        {
            int before, after;
            for (int i = 0; i < _cooks && i < MaxServers; i++)
            {
                before = _economy.XpLevelOf(_cookXpDays[i]);
                _cookXpDays[i] += XpGainOf(0, i);
                _cookTenure[i]++;
                // B = THE INDEX, not the day count: the day is the constant
                // TenureDays already. If it did not carry the index the
                // notification would have to GO LOOKING for it, and if two
                // people crossed the threshold on the same day it would
                // write the first one's name against both - a wrong name
                // stops recognition being recognition.
                if (_cookTenure[i] == TenureDays)
                    Emit(SimEventKind.StaffTenure, 0, i);
                after = _economy.XpLevelOf(_cookXpDays[i]);
                if (after > before) Emit(SimEventKind.StaffLeveledUp, 0, after);
            }
            for (int i = 0; i < _hall && i < MaxServers; i++)
            {
                before = _economy.XpLevelOf(_hallXpDays[i]);
                _hallXpDays[i] += XpGainOf(1, i);
                _hallTenure[i]++;
                if (_hallTenure[i] == TenureDays)
                    Emit(SimEventKind.StaffTenure, 1, i);
                after = _economy.XpLevelOf(_hallXpDays[i]);
                if (after > before) Emit(SimEventKind.StaffLeveledUp, 1, after);
            }
        }

        // =====================================================================
        // Morale. The "Morale" table and thresholds of docs/14.
        //
        // The reason this system exists is written down in docs/14: "The link
        // with the ladder down: a late wage is the ladder's third rung. A
        // collapse in morale, and a resignation, become the concrete face of
        // going under. LOSING A NUMBER IS ABSTRACT; A MEMBER OF STAFF WHOSE
        // NAME YOU KNOW HANDING IN THEIR NOTICE IS CONCRETE."
        // =====================================================================

        /// <summary>
        /// Morale at the end of the day: how busy it was, the aura of the
        /// traits, and the risk of resignation. The effect of wages runs
        /// inside PayWeeklyCostsIfDue, at the moment of payment.
        /// </summary>
        private void UpdateMorale()
        {
            if (_cooks + _hall == 0) return;

            // A busy day = the kitchen worked above its DESIGN CAPACITY.
            //
            // The first definition was "three parties per table" and
            // measurement rejected it: in a well-run shop every day counted
            // as busy, morale fell in one direction only, and the whole crew
            // resigned within a month. Busyness is not an absolute threshold
            // but a threshold RELATIVE TO THE CREW: the same number of
            // customers is calm with two cooks and gruelling with one.
            long capacity = (long)_cooks * _economy.CookCapacityPerDay;
            bool busy = capacity > 0 && _servedPeople > capacity;
            _busyStreak = busy ? _busyStreak + 1 : 0;

            int busyDelta = _busyStreak >= 3 ? _economy.MoraleBusyDelta : 0;
            if (!busy) busyDelta = _economy.MoraleRecoveryDelta;

            // The trait aura: whoever lifts the crew's morale gives +10, a
            // surly one -8. Their own aura does not apply to themselves -
            // otherwise the surly one beats themselves up and falls far
            // below the morale floor.
            int aura = 0;
            for (int i = 0; i < _cooks && i < MaxServers; i++)
                aura += TraitSum(0, i, t => t.MoraleAura);
            for (int i = 0; i < _hall && i < MaxServers; i++)
                aura += TraitSum(1, i, t => t.MoraleAura);

            for (int pool = 0; pool < 2; pool++)
            {
                int count = pool == 0 ? _cooks : _hall;
                int[] morale = pool == 0 ? _cookMorale : _hallMorale;
                for (int i = 0; i < count && i < MaxServers; i++)
                {
                    int own = TraitSum(pool, i, t => t.MoraleAura);
                    // The aura is GRADUAL rather than daily: a surly one
                    // does not give -8 every day, they PULL the crew's mood
                    // down by that much. Applying a tenth of it settles the
                    // balance onto a weekly rather than a yearly scale.
                    morale[i] += busyDelta + (aura - own) / 10;

                    // Recovery DOES NOT GO PAST the starting morale: quiet
                    // days do not make somebody happy, they only bring them
                    // back to normal. Going above it requires the player to
                    // DO something (a rise, a day off - the event list in
                    // docs/14).
                    if (busyDelta > 0 && morale[i] > _economy.StartingMorale)
                        morale[i] = _economy.StartingMorale;
                    ClampMorale(pool, i);
                }
            }

            RollResignations();
        }

        /// <summary>
        /// The wage multiplier coming out of the crew's traits, in basis
        /// points. An apprentice is cheap and an experienced hand dear; with
        /// both on the books the bill lands somewhere in between.
        /// </summary>
        public int TraitWageMultiplierBp()
        {
            int people = _cooks + _hall;
            if (people == 0) return Fx.One;

            long sum = 0;
            for (int i = 0; i < _cooks && i < MaxServers; i++)
                sum += Fx.One + TraitSum(0, i, t => t.WageBp);
            for (int i = 0; i < _hall && i < MaxServers; i++)
                sum += Fx.One + TraitSum(1, i, t => t.WageBp);

            int bp = (int)(sum / people);
            return bp < 1000 ? 1000 : bp;      // a floor: a wage cannot be zeroed
        }

        /// <summary>Applies the same amount to the morale of the whole crew.</summary>
        private void MoraleEvent(int delta)
        {
            for (int i = 0; i < _cooks && i < MaxServers; i++)
            { _cookMorale[i] += delta; ClampMorale(0, i); }
            for (int i = 0; i < _hall && i < MaxServers; i++)
            { _hallMorale[i] += delta; ClampMorale(1, i); }
        }

        private void ClampMorale(int pool, int index)
        {
            int[] m = pool == 0 ? _cookMorale : _hallMorale;
            if (m[index] > 100) m[index] = 100;
            if (m[index] < 0) m[index] = 0;
        }

        /// <summary>
        /// docs/14: with morale below 15, a 10% risk of resignation every
        /// day. Whoever resigns goes from their own position rather than from
        /// the end; the rest slide along. So you can lose your most
        /// experienced hand too.
        /// </summary>
        private void RollResignations()
        {
            for (int pool = 0; pool < 2; pool++)
            {
                int[] morale = pool == 0 ? _cookMorale : _hallMorale;
                for (int i = (pool == 0 ? _cooks : _hall) - 1; i >= 0; i--)
                {
                    if (i >= MaxServers) continue;
                    if (morale[i] >= _economy.MoraleQuitThreshold) continue;
                    if (!_rngHiring.Chance(_economy.MoraleQuitChanceBp)) continue;

                    RemoveStaff(pool, i);
                    Emit(SimEventKind.StaffResigned, pool, i);
                }
            }
        }

        /// <summary>
        /// Removes a member of staff from the list; those after them slide
        /// along.
        ///
        /// THE NAME SLIDES TOO. The name array was not being shifted, and
        /// this function's only caller is A RESIGNATION: when somebody with
        /// low morale left, the name of EVERYONE below them in the list
        /// shifted by one, and because the name is written into the save the
        /// error was permanent.
        ///
        /// That is precisely why this mechanic exists: "losing a number is
        /// abstract; a member of staff WHOSE NAME YOU KNOW handing in their
        /// notice is concrete." Once the names started lying, the mechanic
        /// turned on its head.
        /// </summary>
        /// <summary>
        /// Does the party have work IN THE KITCHEN.
        ///
        /// SEPARATE from _pInTask. Both had been written as "this party is
        /// being seen to", but their meanings differ: _pInTask means "a
        /// waiter is at the table" and it FREEZES patience; whereas a cook
        /// being at the stove is not attending to the customer - the
        /// customer is waiting at exactly that moment.
        ///
        /// Measured: in 87% of "waiting for food" ticks, patience WAS
        /// FROZEN, so DrainWaitingFoodBp = 3500 was in practice behaving as
        /// ~465 (7.5 times weaker). The consequence: prepMs, the equipment
        /// tier and the combo's kitchen load were almost invisible on the
        /// customer's side - the tension of "the kitchen is backed up"
        /// existed, but it had no price.
        ///
        /// Task dispatch reads both flags (so as not to put a party into two
        /// tasks at once); only PATIENCE was separated out.
        /// </summary>
        private readonly bool[] _pKitchenTask = new bool[MaxParties];

        private void RemoveStaff(int pool, int index)
        {
            int[] xp = pool == 0 ? _cookXpDays : _hallXpDays;
            int[] ta = pool == 0 ? _cookTraitA : _hallTraitA;
            int[] tb = pool == 0 ? _cookTraitB : _hallTraitB;
            int[] mo = pool == 0 ? _cookMorale : _hallMorale;
            int[] nm = pool == 0 ? _cookName : _hallName;
            int count = pool == 0 ? _cooks : _hall;

            for (int k = index; k < count - 1 && k + 1 < MaxServers; k++)
            {
                xp[k] = xp[k + 1];
                ta[k] = ta[k + 1];
                tb[k] = tb[k + 1];
                mo[k] = mo[k + 1];
                nm[k] = nm[k + 1];
            }
            int last = count - 1;
            if (last >= 0 && last < MaxServers)
            {
                xp[last] = 0; ta[last] = -1; tb[last] = -1; mo[last] = 0;
                nm[last] = -1;
            }
            if (pool == 0) _cooks--; else _hall--;
        }

        /// <summary>
        /// The day's experience points. docs/14: normally 1. It is 2 with
        /// the `cirak` trait, and 0 with the
        /// `tecrubeli` trait, since "an experienced hand gains no
        /// experience."
        /// </summary>
        private int XpGainOf(int pool, int index)
        {
            int mult = Fx.One;
            for (int slot = 0; slot < 2; slot++)
            {
                int t = StaffTrait(pool, index, slot);
                if (t < 0) continue;
                int xp = _economy.TraitAt(t).XpBp;
                if (xp != Fx.One) mult = xp;      // the most decisive trait
            }
            return (int)Fx.MulDiv(1, mult, Fx.One);
        }

        /// <summary>
        /// A perishable ingredient loses all of its value when the day
        /// closes. docs/12 3: this is what separates the cuisines' risk
        /// profiles, and it is the rule that holds WITHOUT a cold store.
        ///
        /// A cold-store tier buys back part of an ingredient's OWN shelf
        /// life. That is how the spoilDays field in the content comes alive:
        /// the field had been written but the simulation never read it, so
        /// an onion that keeps for twenty days and mince that keeps for one
        /// went into the bin on the same night.
        /// </summary>
        private void SpoilPerishables()
        {
            int keepBp = StorageKeepBp();
            for (int i = 0; i < _stockGrams.Length; i++)
            {
                if (_stockGrams[i] <= 0) { _stockAgeDays[i] = 0; continue; }
                if (!_content.Ingredients[i].Perishable) continue;

                _stockAgeDays[i]++;

                // The life = HOW MANY DAYS it can be used. The age goes up
                // by one at the end of every night, so a life of 1 means
                // "only on the day it was bought" and goes into the bin on
                // the same night as a life of 0.
                //
                // There used to be a floor here reading "at least one day if
                // there is a cold store"; it did nothing, because a life of 1
                // and a life of 0 are the same. An attempt was made to make
                // it "real" by raising the floor and A TEST REFUTED IT: the
                // spoilDays in the content is the shelf life UNDER FULL
                // REFRIGERATION. Mince has a spoilDays of 1, that is, one day
                // even at the top tier - and so it should be. Had the floor
                // been raised, mince would outlive beef in the fridge.
                //
                // So a short-lived ingredient dies every night, at every
                // tier. The cold store does not save those; it saves the
                // LONG-lived ones, and that is where it buys menu breadth.
                int life = keepBp > 0
                    ? (int)Fx.MulDiv(_content.Ingredients[i].SpoilDays, keepBp, Fx.One)
                    : 0;

                if (_stockAgeDays[i] >= life)
                {
                    // THE VALUE OF THE SPOILAGE IS COUNTED.
                    //
                    // The balance tool was reporting a number in the "net"
                    // column 2-20 times the real movement of cash, and the
                    // whole of the difference was this: the stock going into
                    // the bin was not being totalled anywhere. Measured - on
                    // a bot that buys nothing but goes to the market every
                    // day, 43 out of every 100 coins' worth of ingredients
                    // bought goes into the bin. That is why the answers to
                    // three design questions CARRIED THE WRONG SIGN.
                    long lost = Fx.MulDiv(
                        _content.Ingredients[i].BasePrice, _stockGrams[i], 1000);
                    _spoiledValue += lost;
                    _daySpoiled += lost;

                    _stockGrams[i] = 0;
                    _stockAgeDays[i] = 0;
                }
            }
        }

        /// <summary>
        /// The share of shelf life the cold-store tier buys back, in basis
        /// points. 0 = no cold store, the ingredient dies overnight.
        /// </summary>
        private int StorageKeepBp()
        {
            if (_content.Storage == null || _storageTier <= 0) return 0;
            return _content.Storage.Tiers[_storageTier].KeepBp;
        }

        /// <summary>
        /// Rent and wages are WEEKLY, paid in one go at the end of the
        /// seventh day. docs/12 2: this is the metronome of the pressure.
        /// </summary>
        private void PayWeeklyCostsIfDue()
        {
            if (_day % _economy.RentDayInterval != 0) return;

            int week = _day / 7;
            Crew crew = new Crew(_cooks, _hall);
            long wages = StaffingModel.WeeklyWageBill(crew, week, _economy);

            // The trait's effect on the wage. docs/14: `cirak` -25%,
            // `tecrubeli` +30%. The crew multiplier is taken as a MEAN
            // because WeeklyWageBill works per pool rather than per head;
            // that is why the shape of the wage model (the capacity model of
            // docs/14) is not broken.
            wages = Fx.MulDiv(wages, TraitWageMultiplierBp(), Fx.One);

            long rent = _economy.TierForTables(_tableCount).Rent;

            long installment = 0;
            if (_loanWeeksLeft > 0)
            {
                installment = _loanInstallment;
                _loanTotalRepaid += installment;
                _loanRepaidAll += installment;
                _loanWeeksLeft--;
                if (_loanWeeksLeft == 0) _loanInstallment = 0;
            }

            // docs/14: "the wage was paid on time +5, the wage was late
            // -25", and this is the third rung of the ladder down.
            //
            // The test is THE WHOLE BILL: is the till in credit after the
            // rent, the wages and the instalment have been paid. The first
            // draft looked only at the wages and counted a SOUND shop that
            // had bought ingredients that morning as "late" too; one
            // resignation dropped service, which dropped revenue, and a good
            // player came down on their own. Being late means falling into
            // debt.
            bool onTime = _cash - (wages + rent + installment) >= 0;
            MoraleEvent(onTime ? _economy.MoralePaidDelta : _economy.MoraleLateDelta);
            if (!onTime) Emit(SimEventKind.WagesLate, _day, (int)(wages - _cash));

            bool wasPositive = _cash >= 0;
            _cash -= wages + rent + installment;
            _weeklyWagesPaid += wages;
            _weeklyRentPaid += rent;
            _dayWages = wages;
            _dayRent = rent + installment;

            Emit(SimEventKind.WeeklyCostsPaid, (int)rent, (int)wages,
                 _cash > int.MaxValue ? int.MaxValue : (int)_cash);

            if (wasPositive && _cash < 0)
            {
                _firstDebtDay = _day;
                Emit(SimEventKind.CashWentNegative, _day, (int)(-_cash));
            }

            if (_cash < 0) ClimbDownDebtLadder();
        }

        /// <summary>
        /// The ladder down. docs/02, "soft but toothed failure".
        ///
        /// Until this was written, NOTHING happened when the till went
        /// negative, and the result was a soft lock: with a negative till no
        /// ingredients can be bought (OrderIngredient rejects on "cost >
        /// _cash"), so revenue falls to zero; but the rent and the wages go
        /// on being taken unconditionally. Measured: the bold bot falls into
        /// debt on day 7, spends the remaining 53 days without a customer and
        /// finishes on -48,370. It was neither instructive nor enjoyable -
        /// the game did not end, it simply froze.
        ///
        /// The ladder has three rungs, and EVERY RUNG PUTS THE TILL BACK
        /// TOGETHER:
        ///
        ///   1. Selling equipment - starting from the top tier, at half the
        ///      purchase price. Capacity falls but the shop opens.
        ///   2. Downsizing - one table tier down, half the cost of the move
        ///      back. The rent falls too, so this is the real rescue.
        ///   3. The remaining debt IS WRITTEN OFF and the price is taken out
        ///      of reputation.
        ///
        /// The save is not deleted and the game does not end (docs/08). The
        /// price is cumulative: climbing down the ladder lowers the
        /// resilience axis in the year-end evaluation.
        /// </summary>
        private void ClimbDownDebtLadder()
        {
            _debtRungs++;

            // --- 1. sell equipment ----------------------------------------
            while (_cash < 0 && SellBestEquipment()) { }

            // --- 2. downsize ----------------------------------------------
            while (_cash < 0 && Downsize()) { }

            // --- 3. write off the debt, pay in reputation ------------------
            if (_cash < 0)
            {
                _rescueValue += -_cash;
                _cash = 0;
                _reputationCenti -= _economy.DebtWriteOffRepCenti;
                if (_reputationCenti < 0) _reputationCenti = 0;

                // A ZERO TILL WAS A LOCK TOO.
                //
                // The ladder was written precisely to break the soft lock,
                // but the third rung left the till at ZERO, and Buy rejects
                // on "cost > _cash" - with zero, no ingredient can be bought
                // either. Measured: the diagnostic output of the plate test
                // printed "0 parties, 4 tables" EVERY DAY from day 10 to day
                // 40. Thirty-five days running without a single customer,
                // the rent and the wages still being taken, and the ladder
                // being climbed down again every week. So the ladder was not
                // breaking the lock, it was REPEATING IT FOREVER.
                //
                // The rung now leaves the shop IN WORKING ORDER: a rescue
                // sum equal to the cost of one day's recommended stock. The
                // number is not invented - RecommendedRestock already knows
                // the menu, the demand and the safety margin, so it means
                // "enough to open tomorrow morning".
                //
                // It has a price: the rescue value is written into the
                // year-end resilience axis, so climbing down the ladder is
                // always expensive.
                long floorCash = RecommendedRestockCost();
                if (floorCash > 0)
                {
                    _cash = floorCash;
                    _rescueValue += floorCash;
                }
            }
        }

        /// <summary>
        /// The cost of one day's recommended stock at the current seasonal
        /// price.
        ///
        /// The ladder's rescue sum comes from this. It uses THE SAME price
        /// path as Buy (the seasonal movement + the market multiplier);
        /// otherwise money worked out as "I gave them enough" would not have
        /// been enough.
        /// </summary>
        private long RecommendedRestockCost()
        {
            long total = 0;
            for (int i = 0; i < _stockGrams.Length; i++)
            {
                int need = RecommendedRestock(i);
                if (need <= 0) continue;
                long perKilo = _content.Ingredients[i].PriceAt(Season, _quality);
                perKilo = Fx.MulDiv(perKilo, _marketBp[i], Fx.One);
                total += Fx.MulDiv(perKilo, need, GramsPerKilo);
            }
            return total;
        }

        /// <summary>
        /// Sells the highest-tier station down one rung. Half the price comes
        /// back; a sale is always a loss, deliberately.
        /// </summary>
        private bool SellBestEquipment()
        {
            int best = -1, bestTier = 0;
            long bestPrice = 0;

            for (int i = 0; i < _stationTier.Length; i++)
            {
                int t = _stationTier[i];
                if (t <= 0) continue;

                // It does not go below the tier the table count makes
                // COMPULSORY: a sale must not leave the shop unable to work.
                if (t <= RequiredStationTier(i)) continue;

                long price = _content.Stations[i].Tiers[t].Price;
                if (price > bestPrice) { best = i; bestTier = t; bestPrice = price; }
            }
            if (best < 0) return false;

            _stationTier[best] = bestTier - 1;
            _cash += bestPrice / 2;
            _rescueValue += bestPrice / 2;
            Emit(SimEventKind.EquipmentSold, best, bestTier - 1);
            return true;
        }

        /// <summary>Goes down one table tier. The rent falls with it.</summary>
        private bool Downsize()
        {
            int tier = -1;
            for (int t = 0; t < _economy.TierCount; t++)
                if (_economy.TierAt(t).Tables == _tableCount) { tier = t; break; }
            if (tier <= 0) return false;

            TierConfig lower = _economy.TierAt(tier - 1);
            _cash += _economy.TierAt(tier).Upgrade / 2;
            _rescueValue += _economy.TierAt(tier).Upgrade / 2;

            // A SHRINKING SHOP LOSES PLATES TOO.
            //
            // Expand was adding CLEAN plates for the difference while
            // Downsize removed none: on the day of the downsizing
            // "clean + in use + dirty > PlatesTotal" held, and because
            // WashNeeded's thresholds are worked out against the shrunken
            // total, the washing-up shift was triggered at the wrong time.
            // Because AdvanceToNextDay rewrites the plates to the tier
            // overnight, the bug WAS HIDING ITSELF - the invariant was only
            // broken on that one day.
            //
            // It comes off the clean plates first and off the dirty ones if
            // that is not enough. The plates on the tables (in use) are not
            // touched: a guest with a plate in front of them does not vanish.
            int extraPlates = _economy.TierAt(tier).Plates - lower.Plates;
            if (extraPlates > 0)
            {
                int fromClean = extraPlates < _platesClean ? extraPlates : _platesClean;
                _platesClean -= fromClean;
                extraPlates -= fromClean;
                if (extraPlates > 0)
                {
                    int fromDirty = extraPlates < _platesDirty ? extraPlates : _platesDirty;
                    _platesDirty -= fromDirty;
                }
            }

            _tableCount = lower.Tables;

            // The staff cap has fallen too; whoever is over it goes.
            int cap = lower.StaffCap;
            while (_cooks + _hall > cap && _hall > 0) Fire(1, _hall - 1);
            while (_cooks + _hall > cap && _cooks > 1) Fire(0, _cooks - 1);

            Emit(SimEventKind.Downsized, _tableCount, tier - 1);
            return true;
        }

        /// <summary>Moves from the evening stage to the next morning.</summary>
        public void AdvanceToNextDay()
        {
            if (_phase != DayPhase.Evening) return;

            // Update the peak BEFORE the day's covers are zeroed.

            _day++;
            _phase = DayPhase.Morning;
            _serviceTick = 0;
            _dayWages = 0;
            _dayRent = 0;
            _daySpoiled = 0;
            _servedParties = 0;
            _servedPeople = 0;
            _angryParties = 0;
            _angrySeated = 0;
            _hallRushWashes = 0;
            _hallCrisisWashes = 0;
            _revenue = 0;
            _ingredientCost = 0;
            _satisfactionSum = 0;
            _reputationDeltaMicro = 0;
            _arrCount = 0;
            _arrNext = 0;
            _turnedAwayParties = 0;
            _commandCount = 0;
            // What was earned TODAY is zeroed; the badges already earned
            // (_badges) of course stay.
            _badgesToday = 0;
            for (int i = 0; i < MaxTables; i++)
            {
                _tableParty[i] = -1;
                _tableDirty[i] = false;
                _tablePlates[i] = 0;
            }

            // THE WASHING-UP IS FINISHED OVERNIGHT.
            //
            // The closing shift empties the sink; the next morning every
            // plate is clean. The bottleneck is a bottleneck WITHIN THE DAY -
            // carrying yesterday's neglect into today would be a punishment
            // arriving from somewhere the player cannot see.
            _platesClean = _economy.TierForTables(_tableCount).Plates;
            _platesDirty = 0;
            _platesInUse = 0;
            _plateBlockedTicks = 0;
            _plateStalled = false;
            _washedToday = 0;
            _dirtiedToday = 0;
            _washing = false;
            _plateWarned = false;
            for (int i = 0; i < MaxParties; i++) { _pPlates[i] = 0; _pCooked[i] = false; }
            for (int i = 0; i < MaxServers; i++)
            {
                _hallTaskKind[i] = TaskKind.None;
                _kitchenTaskKind[i] = TaskKind.None;
            }
            // No slot should be occupied at the start of the day. A counter
            // left over from the previous day would narrow the kitchen
            // permanently.
            for (int i = 0; i < _stationBusy.Length; i++) _stationBusy[i] = 0;
            for (int j = 0; j < _jobStation.Length; j++)
            {
                _jobStation[j] = -1;
                _jobMs[j] = 0;
                _jobPlates[j] = 0;
                _jobSlots[j] = 0;
                _jobState[j] = 0;
            }
            for (int i = 0; i < MaxParties; i++) _pJobsLeft[i] = 0;
            for (int i = 0; i < MaxParties; i++) _pAskedDish[i] = -1;
            for (int i = 0; i < MaxParties; i++)
            {
                _pCredit[i] = false;
                _pTea[i] = false;
                _pCombo[i] = false;
                _pAsksCredit[i] = false;
                _pRegular[i] = -1;
                _pMissedFavourite[i] = false;
            }
            SettleDueTabs();
            AnnounceUnlocks();
            RollMarket();
            PlanRegularVisits();
            RefreshCandidates();
            for (int i = 0; i < _orderedRole.Length; i++) _orderedRole[i] = 0;

            Emit(SimEventKind.DayOpened, _day);
        }

        /// <summary>
        /// Announces the dishes unlocked today.
        ///
        /// Why an event: the unlock was SILENT. The dishes open one by one
        /// from day 3 to day 57, and the player only saw it if they opened
        /// the Menu screen and scrolled down. Everything that opens is a
        /// reward; a reward that is not announced is not a reward.
        ///
        /// It scans for what was closed yesterday and is open today, so the
        /// condition need not be the dish's DAY - a dish opened by reputation
        /// or by equipment is announced too.
        /// </summary>
        private void AnnounceUnlocks()
        {
            for (int i = 0; i < _content.Dishes.Length; i++)
            {
                bool open = Unlocked(i);
                if (open == _dishWasUnlocked[i]) continue;

                _dishWasUnlocked[i] = open;
                if (open) Emit(SimEventKind.DishUnlocked, i);
            }
        }

        /// <summary>
        /// Sets a dish's price.
        ///
        /// THERE IS A CEILING. Satisfaction is clamped to [0, 10000] and
        /// demand never sees the price; so past a certain point the
        /// punishment SATURATES and every zero after that is free. Measured:
        /// a bot that made the side items 2000 times dearer piled up 3.4
        /// million coins while reputation and satisfaction did not change at
        /// all. With the combo on, 24.8 million.
        ///
        /// The floor (UnderpriceFloorBp) was already there; the absence of a
        /// ceiling was an error of symmetry. The ceiling is relative to THE
        /// MARKET price: when the content changes, so does the limit.
        /// </summary>
        private void SetPrice(int dishIndex, int price)
        {
            if (dishIndex < 0 || dishIndex >= _dishPrice.Length || price <= 0)
            {
                Emit(SimEventKind.CommandRejected, (int)CommandKind.SetPrice, 3);
                return;
            }

            long market = _content.Dishes[dishIndex].Price;
            if (market > 0)
            {
                long ceiling = Fx.MulDiv(market, _economy.OverpriceCeilingBp, Fx.One);
                if (price > ceiling)
                {
                    // Reason 13: the price ceiling. The UI shows this as
                    // "nobody will come at that price".
                    Emit(SimEventKind.CommandRejected, (int)CommandKind.SetPrice, 13);
                    return;
                }
            }

            _dishPrice[dishIndex] = price;
        }

        private void SetMenuSlot(int dishIndex, bool on)
        {
            if (dishIndex < 0 || dishIndex >= _dishOnMenu.Length)
            {
                Emit(SimEventKind.CommandRejected, (int)CommandKind.SetMenuSlot, 3);
                return;
            }
            _dishOnMenu[dishIndex] = on;
        }

        /// <summary>
        /// Buys the next equipment tier up for a station. docs/27 Decision D:
        /// a new tier either adds a slot or lowers attendBp; it does not
        /// touch the cooking time.
        ///
        /// The price is paid in cash out of the till; it cannot be bought on
        /// credit.
        /// </summary>
        private void BuyEquipment(int station)
        {
            if (station < 0 || station >= _stationTier.Length)
            {
                Emit(SimEventKind.CommandRejected, (int)CommandKind.BuyEquipment, 3);
                return;
            }

            // NOTHING COOKS HERE, SO THERE IS NOTHING TO UPGRADE.
            //
            // A cuisine does not use every station in the content - no
            // Turkish dish uses the oven - and this let the player pay for
            // its ladder anyway. Six thousand coins, the priciest single
            // step in the game, for a machine no order will ever reach.
            if (!IsStationUsed(station))
            {
                Emit(SimEventKind.CommandRejected, (int)CommandKind.BuyEquipment, 4);
                return;
            }

            StationDef def = _content.Stations[station];
            int next = _stationTier[station] + 1;
            if (next > def.MaxTier)
            {
                Emit(SimEventKind.CommandRejected, (int)CommandKind.BuyEquipment, 4);
                return;
            }

            long price = def.Tiers[next].Price;
            if (_cash < price)
            {
                Emit(SimEventKind.CommandRejected, (int)CommandKind.BuyEquipment, 5);
                return;
            }

            _cash -= price;
            _equipmentSpend += price;
            _stationTier[station] = next;
            Emit(SimEventKind.EquipmentBought, station, next);
        }

        /// <summary>
        /// Buys the next cold-store tier up.
        ///
        /// What this buys in the game is MENU BREADTH. Without a cold store
        /// everything perishable dies overnight, so a narrow menu is
        /// unambiguously the right strategy; in the balance tool what keeps
        /// the reasonable player standing is narrowing the menu to three main
        /// dishes. The cold store loosens that constraint, and so becomes the
        /// reason the thirty-two-dish content inventory exists at all.
        /// </summary>
        private void BuyStorage()
        {
            StorageDef def = _content.Storage;
            if (def == null)
            {
                Emit(SimEventKind.CommandRejected, (int)CommandKind.BuyStorage, 3);
                return;
            }

            int next = _storageTier + 1;
            if (next > def.MaxTier)
            {
                Emit(SimEventKind.CommandRejected, (int)CommandKind.BuyStorage, 4);
                return;
            }

            long price = def.Tiers[next].Price;
            if (_cash < price)
            {
                Emit(SimEventKind.CommandRejected, (int)CommandKind.BuyStorage, 5);
                return;
            }

            _cash -= price;
            _equipmentSpend += price;
            _storageTier = next;
            Emit(SimEventKind.StorageBought, next, def.Tiers[next].KeepBp);
        }

        /// <summary>
        /// Chooses the quality tier of what is bought at the market.
        /// 0 low, 1 standard, 2 high. It DOES NOT CHANGE the stock in hand;
        /// it affects only purchases from here on.
        /// </summary>
        /// <summary>
        /// The number of quality tiers: low, standard, high.
        ///
        /// It lives in one place because it is read in two - command
        /// validation and save validation. A number written separately in
        /// the two means a value one of them accepts and the other rejects.
        /// </summary>
        public const int QualityCount = 3;

        private void SetQuality(int level)
        {
            if (level < 0 || level >= QualityCount)
            {
                Emit(SimEventKind.CommandRejected, (int)CommandKind.SetQuality, 3);
                return;
            }
            _quality = level;
        }

        /// <summary>
        /// Which dish the given archetype would pick from the given role. For
        /// measurement only; it does not change the simulation's state.
        /// </summary>
        public int WouldPick(int archetype, int role, int servings = 1)
        {
            if (archetype < 0 || archetype >= _content.Archetypes.Length) return -1;
            string[] set = role == 0 ? _content.MainGroups
                         : role == 1 ? _content.SideGroups
                         : role == 2 ? _content.DrinkGroups
                                     : _content.DessertGroups;
            return PickFromRole(set, int.MaxValue, true, servings,
                                _content.Archetypes[archetype]);
        }

        /// <summary>The quality tier selected.</summary>
        public int Quality { get { return _quality; } }

        /// <summary>The dish's quality effect today, in centi-points. For measurement.</summary>
        public int DishQualityCentiOf(int dish)
        {
            if (dish < 0 || dish >= _content.Dishes.Length) return 0;
            return DishQualityCenti(dish);
        }

        /// <summary>The mean quality effect of the ingredient in stock, in centi-points.</summary>
        public int StockQualityOf(int ingredient)
        {
            if (ingredient < 0 || ingredient >= _stockQualityCenti.Length) return 0;
            return _stockQualityCenti[ingredient];
        }

        /// <summary>The cold-store tier owned.</summary>
        public int StorageTier { get { return _storageTier; } }

        /// <summary>The price of the next cold-store tier up; -1 at the top.</summary>
        public long NextStoragePrice()
        {
            StorageDef def = _content.Storage;
            if (def == null) return -1;
            int next = _storageTier + 1;
            return next > def.MaxTier ? -1 : def.Tiers[next].Price;
        }

        /// <summary>
        /// The station's content id: "ocak", "izgara", "doner_ocagi"...
        ///
        /// WHY THE VIEW NEEDS IT. Until now the kitchen drew three copies of
        /// one stove prefab and spread sixteen possible stations over them by
        /// `station % 3`, so buying a grill changed nothing on screen. The
        /// view builds one object per station now, and it has to know WHICH
        /// station each one is to build the right thing - a fryer is not an
        /// oven.
        ///
        /// Returns the empty string out of range rather than throwing: the
        /// view runs against a preview simulation as well as a live one.
        /// </summary>
        public string StationId(int station)
        {
            if (station < 0 || station >= _content.Stations.Length) return string.Empty;
            return _content.Stations[station].Id;
        }

        /// <summary>The equipment tier owned for this station.</summary>
        public int StationTier(int station)
        {
            if (station < 0 || station >= _stationTier.Length) return 0;
            return _stationTier[station];
        }

        /// <summary>
        /// Today's season: 0 spring, 1 summer, 2 autumn, 3 winter.
        /// Spring on day 1; it turns over every SeasonDays days.
        /// </summary>
        public int Season
        {
            get
            {
                int len = _economy.SeasonDays;
                if (len <= 0) return 0;
                int d = _day > 0 ? _day - 1 : 0;
                return (d / len) % 4;
            }
        }

        /// <summary>
        /// The ingredient's price per kilo TODAY: the season, the quality and
        /// the daily market movement together.
        /// </summary>
        public long IngredientPriceToday(int ingredient)
        {
            if (ingredient < 0 || ingredient >= _content.Ingredients.Length) return 0;
            long p = _content.Ingredients[ingredient].PriceAt(Season, _quality);
            return Fx.MulDiv(p, _marketBp[ingredient], Fx.One);
        }

        /// <summary>Today's market multiplier, in basis points. 10000 = a normal day.</summary>
        public int MarketBpOf(int ingredient)
        {
            if (ingredient < 0 || ingredient >= _marketBp.Length) return Fx.One;
            return _marketBp[ingredient];
        }

        /// <summary>
        /// Rolls the day's market prices. Every ingredient moves
        /// independently: on one day tomatoes can be cheap and meat dear.
        /// Moving them with a single multiplier would mean "everything is
        /// dear today" and would leave nothing to pay attention to.
        /// </summary>
        private void RollMarket()
        {
            int vol = _economy.PriceVolatilityBp;
            if (vol <= 0)
            {
                for (int i = 0; i < _marketBp.Length; i++) _marketBp[i] = Fx.One;
                return;
            }
            for (int i = 0; i < _marketBp.Length; i++)
                _marketBp[i] = Fx.One - vol + _rngMarket.NextInt(2 * vol + 1);
        }

        /// <summary>The ingredient's MEAN price per kilo across the four seasons.</summary>
        public long IngredientPriceMean(int ingredient)
        {
            if (ingredient < 0 || ingredient >= _content.Ingredients.Length) return 0;
            IngredientDef d = _content.Ingredients[ingredient];
            if (d.SeasonPriceBp == null) return d.BasePrice;
            long sum = 0;
            for (int i = 0; i < d.SeasonPriceBp.Length; i++) sum += d.PriceInSeason(i);
            return sum / d.SeasonPriceBp.Length;
        }

        /// <summary>How many days the ingredient keeps with today's cold store.</summary>
        public int KeepDays(int ingredient)
        {
            if (ingredient < 0 || ingredient >= _content.Ingredients.Length) return 0;
            IngredientDef d = _content.Ingredients[ingredient];
            if (!d.Perishable) return int.MaxValue;
            int keepBp = StorageKeepBp();
            if (keepBp <= 0) return 1;
            int life = (int)Fx.MulDiv(d.SpoilDays, keepBp, Fx.One);
            // A floor of 1 for the UI: there is no such thing as "it keeps
            // for 0 days", it can be used today. In the simulation a life of
            // 0 and a life of 1 are the same thing.
            return life < 1 ? 1 : life;
        }

        /// <summary>
        /// Does this ingredient go off at all. Salt, flour and oil do not; for
        /// those KeepDays returns int.MaxValue, and if the UI prints that
        /// number AS IT IS the player sees "2147483647 days" - which is
        /// exactly what happened in the first desktop build.
        /// </summary>
        public bool IsPerishable(int ingredient)
        {
            return ingredient >= 0 && ingredient < _content.Ingredients.Length
                && _content.Ingredients[ingredient].Perishable;
        }

        /// <summary>Can the ingredient be kept for more than a day.</summary>
        public bool CanKeep(int ingredient)
        {
            return KeepDays(ingredient) > 1;
        }

        public int StationCount { get { return _stationTier.Length; } }

        /// <summary>
        /// Is this station a piece of equipment named SPECIFICALLY for the
        /// cuisine (tas_firin, doner_ocagi, ...) or one of the seven shared
        /// stations. A named one never becomes COMPULSORY because of the
        /// table count; it only opens menu.
        /// </summary>
        public bool IsCuisineStation(int station)
        {
            return station >= 0 && station < _content.Stations.Length
                && !_content.Stations[station].Shared;
        }

        /// <summary>How many items of this role were ordered today. 0 main, 3 dessert.</summary>
        public int OrderedInRole(int role)
        {
            return role >= 0 && role < _orderedRole.Length ? _orderedRole[role] : 0;
        }

        /// <summary>
        /// The LOWEST tier the station requires at this table count.
        /// It is read from the equipment ladder's neededAtTables field; that
        /// field is derived inside tools/balance from the peak table of
        /// docs/27 3.3.
        ///
        /// Reading it from CAPACITY rather than from the number of angry
        /// customers is deliberate: the same mistake had been made in the
        /// crew decision and the balance tool caught it.
        /// </summary>
        public int RequiredStationTier(int station)
        {
            if (station < 0 || station >= _stationTier.Length) return 0;

            // A station THIS CUISINE DOES NOT USE cannot be compulsory.
            //
            // equipment.json is shared by every cuisine and "neededAtTables"
            // lives there. The oven is marked compulsory at fourteen tables;
            // but NOT ONE of the thirty-two Turkish dishes uses the oven. So
            // when the Turkish restaurant grew it was forced to pay 4,800
            // coins for an oven that was of no use to it whatsoever.
            if (!_stationUsed[station]) return 0;

            StationDef def = _content.Stations[station];
            int want = 0;
            for (int t = 1; t < def.Tiers.Length; t++)
                if (def.Tiers[t].NeededAtTables > 0
                    && def.Tiers[t].NeededAtTables <= _tableCount)
                    want = t;
            return want;
        }

        /// <summary>
        /// The member of staff's name. With no name pool it returns null and
        /// the UI falls back to a numbered name (Cook 1).
        /// </summary>
        public string StaffName(int pool, int index)
        {
            int count = pool == 0 ? _cooks : _hall;
            if (index < 0 || index >= count || index >= MaxServers) return null;

            int[] names = pool == 0 ? _cookName : _hallName;
            int n = names[index];
            return n >= 0 && n < _content.StaffNames.Length
                ? _content.StaffNames[n] : null;
        }

        /// <summary>
        /// WHICH name, rather than the name itself.
        ///
        /// A staff member's identity is an INDEX into the content's name list
        /// and the save carries that index, so the string is PRESENTATION and
        /// belongs to the view: `Loc.StaffName` turns this into a name in the
        /// player's own language. The core has no business knowing which
        /// language is on.
        ///
        /// `StaffName` above is kept because the balance harness and the tests
        /// run without a Loc table and only need to tell two colleagues apart.
        /// </summary>
        public int StaffNameIndex(int pool, int index)
        {
            int count = pool == 0 ? _cooks : _hall;
            if (index < 0 || index >= count || index >= MaxServers) return -1;
            return (pool == 0 ? _cookName : _hallName)[index];
        }

        /// <summary>Is there at least one dish that uses this station.</summary>
        public bool IsStationUsed(int station)
        {
            return station >= 0 && station < _stationUsed.Length && _stationUsed[station];
        }

        /// <summary>
        /// The total of everything the player CAN STILL buy: the expansion
        /// tiers left plus the equipment rungs left.
        ///
        /// The measure "money has stopped being a problem" uses this. The
        /// previous measure said "is the till above three times the dearest
        /// expansion", and it was wrong in two ways: it counted no equipment
        /// at all, and three times was an arbitrary number. The right
        /// question is "is there anything left to save up for": if the till
        /// buys everything that is left in one go, then there really is
        /// nothing left.
        /// </summary>
        public long RemainingPurchaseCost()
        {
            long total = 0;
            for (int t = 0; t < _economy.TierCount; t++)
                if (_economy.TierAt(t).Tables > _tableCount)
                    total += _economy.TierAt(t).Upgrade;

            // The same filter as RemainingPurchaseCostTotal: the two have to
            // answer the same question or `owned` comes out wrong.
            for (int st = 0; st < _stationTier.Length; st++)
            {
                if (!IsStationUsed(st)) continue;
                StationDef def = _content.Stations[st];
                for (int t = _stationTier[st] + 1; t < def.Tiers.Length; t++)
                    total += def.Tiers[t].Price;
            }

            if (_content.Storage != null)
                for (int t = _storageTier + 1; t < _content.Storage.Tiers.Length; t++)
                    total += _content.Storage.Tiers[t].Price;

            return total;
        }

        /// <summary>The price of the next tier up; -1 at the top.</summary>
        public long NextEquipmentPrice(int station)
        {
            if (station < 0 || station >= _stationTier.Length) return -1;

            // A STATION THIS CUISINE DOES NOT COOK ON IS NOT FOR SALE, and
            // -1 is how this method already says "there is nothing to buy".
            //
            // BuyEquipment learned to refuse it (reason 4, the six thousand
            // coin oven no Turkish order would ever reach) and this method
            // was left quoting a price for the same purchase. Everything
            // that asks "what does the next tier cost" therefore believed
            // there was one: the equipment screen drew a live Upgrade button
            // for it, and the harness bot spent its one purchase a day on a
            // command the simulation threw away.
            //
            // MEASURED, and it is not small. Inserting `fritoz` made `ocak`
            // dead for fast food and put it at index 0 - the first thing the
            // bot's optional loop tries - so from that commit the fast-food
            // bots never bought another optional upgrade. The campaign report
            // moved on 252 lines and docs/12's growth multiplier fell from
            // 1.60 to 1.14. The content change really was neutral; this was
            // the leak, and it had been open since the oven.
            if (!IsStationUsed(station)) return -1;

            StationDef def = _content.Stations[station];
            int next = _stationTier[station] + 1;
            return next > def.MaxTier ? -1 : def.Tiers[next].Price;
        }

        /// <summary>
        /// How many hall staff are dedicated to the sink.
        ///
        /// THE CEILING IS _hall. The owner is not counted: the player is the
        /// owner, not the dishwasher - DispatchHall never puts server zero
        /// (the owner) at the sink (the `sinkFrom < 1` guard).
        ///
        /// THE COMMENT ONCE SAID "the WHOLE of the hall crew cannot be put on
        /// the sink, or the game locks itself up", and two lines beneath it
        /// were IMITATING that floor:
        ///
        ///     int enFazla = _hall > 0 ? _hall - 0 : 0;   // == _hall
        ///     if (enFazla > _hall) enFazla = _hall;      // can never be true
        ///
        /// An audit rightly found both to be no-ops; what was wrong was not
        /// THE CODE but THE COMMENT. There is no such lock: even with every
        /// hired hand at the sink the owner stays on the floor and service
        /// carries on. What is more, on the first day, with a single hall
        /// worker, putting them on the sink is a LEGITIMATE decision - with a
        /// floor in place the plate bottleneck could not be tried at all in
        /// the campaign's early days.
        ///
        /// So the rule is "no floor, ceiling _hall", and ClampDishwashers
        /// applies that same ceiling already.
        /// </summary>
        private void SetDishwashers(int n)
        {
            if (n < 0) n = 0;
            if (n > _hall) n = _hall;
            _dishwashers = n;
        }

        private void Hire(int pool, int candidate)
        {
            int cap = _economy.TierForTables(_tableCount).StaffCap;
            if (_cooks + _hall >= cap)
            {
                Emit(SimEventKind.CommandRejected, (int)CommandKind.Hire, 4);
                return;
            }
            if (pool == 0)
            {
                if (_cooks < MaxServers) { _cookXpDays[_cooks] = 0; _cookTenure[_cooks] = 0; RollTraits(0, _cooks, candidate); }
                _cooks++;
            }
            else
            {
                if (_hall < MaxServers) { _hallXpDays[_hall] = 0; _hallTenure[_hall] = 0; RollTraits(1, _hall, candidate); }
                _hall++;
            }
        }

        /// <summary>
        /// Keeps the sink crew inside the hall crew.
        ///
        /// When a resignation or a dismissal drops the hall count, the number
        /// dedicated to the sink can be left larger than it - and then
        /// DispatchHall counts people who do not exist as being at the sink,
        /// and the hall silently shrinks by that many heads.
        /// </summary>
        private void ClampDishwashers()
        {
            if (_dishwashers > _hall) _dishwashers = _hall;
            if (_dishwashers < 0) _dishwashers = 0;
        }

        /// <summary>
        /// A new hire's two traits. docs/14: two from the pool, with no
        /// conflicting pair. Conflicts are FILTERED OUT while the second
        /// trait is being drawn - drawing first and rejecting afterwards
        /// would mean a different number of random calls on the same seed,
        /// and it would break replay.
        /// </summary>
        private void RollTraits(int pool, int index, int candidate = 0)
        {
            int[] a = pool == 0 ? _cookTraitA : _hallTraitA;
            int[] b = pool == 0 ? _cookTraitB : _hallTraitB;
            int[] morale = pool == 0 ? _cookMorale : _hallMorale;

            morale[index] = _economy.StartingMorale;
            a[index] = -1;
            b[index] = -1;

            // The name is chosen now too, from the same stream: hiring is a
            // single event, and leaving the name to a separate roll of the
            // die would complicate replay for no reason.
            // THE NAME COMES FROM ITS OWN STREAM.
            //
            // It used to be drawn from the hiring stream, and that shifted
            // the trait die: a test running on the same seed suddenly saw
            // different traits and could not find a busy station. The
            // independent streams of docs/23 exist precisely for this -
            // presentation must not touch the die the play runs on.
            int[] names = pool == 0 ? _cookName : _hallName;
            names[index] = _content.StaffNames.Length > 0
                ? _rngName.NextInt(_content.StaffNames.Length) : -1;
            if (_economy.TraitCount == 0) return;

            // If the pool has not been built yet (the constructor, the first
            // cook), build it now.
            if (_candDay < 0) RefreshCandidates();

            if (candidate < 0) candidate = 0;
            if (candidate >= CandidateSlots) candidate = CandidateSlots - 1;

            a[index] = CandidateTrait(pool, candidate, 0);
            b[index] = CandidateTrait(pool, candidate, 1);

            // The candidate taken leaves the pool and NO NEW ONE ARRIVES in
            // their place: docs/14, "you can turn down a candidate you do not
            // like, but a new one does not arrive straight away." Their slot
            // stays empty until the pool refreshes.
            int slot = pool * CandidateSlots + candidate;
            _candTraitA[slot] = -1;
            _candTraitB[slot] = -1;
        }

        /// <summary>
        /// Refreshes the candidate pool every three days. docs/14: "you can
        /// turn down a candidate you do not like, but a new one does not
        /// arrive straight away."
        /// </summary>
        private void RefreshCandidates()
        {
            if (_economy.TraitCount == 0) return;
            if (_candDay >= 0 && _day - _candDay < _economy.CandidateRefreshDays) return;

            _candDay = _day;
            for (int i = 0; i < CandidateSlots * 2; i++)
                RollCandidate(i);
        }

        private void RollCandidate(int slot)
        {
            int n = _economy.TraitCount;
            _candTraitA[slot] = _rngHiring.NextInt(n);
            TraitDef first = _economy.TraitAt(_candTraitA[slot]);

            int free = 0;
            for (int i = 0; i < n; i++)
                if (i != _candTraitA[slot] && !first.ConflictsWithIndex(i)) free++;
            _candTraitB[slot] = -1;
            if (free == 0) return;

            int pick = _rngHiring.NextInt(free);
            for (int i = 0; i < n; i++)
            {
                if (i == _candTraitA[slot] || first.ConflictsWithIndex(i)) continue;
                if (pick == 0) { _candTraitB[slot] = i; return; }
                pick--;
            }
        }

        /// <summary>The candidate's trait. pool 0 kitchen, 1 hall; slot 0..2.</summary>
        public int CandidateTrait(int pool, int slot, int which)
        {
            if (slot < 0 || slot >= CandidateSlots) return -1;
            int i = pool * CandidateSlots + slot;
            return which == 0 ? _candTraitA[i] : _candTraitB[i];
        }

        /// <summary>The candidate's wage difference, in basis points. The UI will show it.</summary>
        public int CandidateWageBp(int pool, int slot)
        {
            int total = 0;
            for (int which = 0; which < 2; which++)
            {
                int t = CandidateTrait(pool, slot, which);
                if (t >= 0) total += _economy.TraitAt(t).WageBp;
            }
            return total;
        }

        /// <summary>
        /// A trait pair's rough worth: speed + satisfaction - wage.
        ///
        /// Three different units are added together and the crudeness is
        /// deliberate. The aim is not a balance calculation but a COMPARISON:
        /// is the person you have better, or the candidate at the door. The
        /// UI will show that ordering too.
        /// </summary>
        private int TraitScore(int a, int b)
        {
            int score = 0;
            for (int k = 0; k < 2; k++)
            {
                int t = k == 0 ? a : b;
                if (t < 0) continue;
                TraitDef d = _economy.TraitAt(t);
                score += d.SpeedBp + d.SatisfactionCenti - d.WageBp;
            }
            return score;
        }

        /// <summary>The trait score of a member of staff on the books.</summary>
        public int StaffTraitScore(int pool, int index)
        {
            return TraitScore(StaffTrait(pool, index, 0), StaffTrait(pool, index, 1));
        }

        /// <summary>A candidate's trait score. int.MinValue if the candidate has been taken.</summary>
        public int CandidateScore(int pool, int slot)
        {
            int a = CandidateTrait(pool, slot, 0);
            if (a < 0) return int.MinValue;
            return TraitScore(a, CandidateTrait(pool, slot, 1));
        }

        /// <summary>The candidate's speed difference, in basis points.</summary>
        public int CandidateSpeedBp(int pool, int slot)
        {
            int total = 0;
            for (int which = 0; which < 2; which++)
            {
                int t = CandidateTrait(pool, slot, which);
                if (t >= 0) total += _economy.TraitAt(t).SpeedBp;
            }
            return total;
        }

        /// <summary>A member of staff's trait; slot 0 or 1. -1 if there is none.</summary>
        public int StaffTrait(int pool, int index, int slot)
        {
            int count = pool == 0 ? _cooks : _hall;
            if (index < 0 || index >= count || index >= MaxServers) return -1;
            if (slot == 0) return pool == 0 ? _cookTraitA[index] : _hallTraitA[index];
            return pool == 0 ? _cookTraitB[index] : _hallTraitB[index];
        }

        /// <summary>A member of staff's morale, 0-100.</summary>
        public int StaffMorale(int pool, int index)
        {
            int count = pool == 0 ? _cooks : _hall;
            if (index < 0 || index >= count || index >= MaxServers) return 0;
            return pool == 0 ? _cookMorale[index] : _hallMorale[index];
        }

        /// <summary>The combined effect of a member of staff's traits.</summary>
        private int TraitSum(int pool, int index, System.Func<TraitDef, int> pick)
        {
            int total = 0;
            for (int slot = 0; slot < 2; slot++)
            {
                int t = StaffTrait(pool, index, slot);
                if (t >= 0) total += pick(_economy.TraitAt(t));
            }
            return total;
        }

        private bool TraitAny(int pool, int index, System.Func<TraitDef, bool> pick)
        {
            for (int slot = 0; slot < 2; slot++)
            {
                int t = StaffTrait(pool, index, slot);
                if (t >= 0 && pick(_economy.TraitAt(t))) return true;
            }
            return false;
        }

        /// <summary>
        /// Dismisses a particular person.
        ///
        /// THE INDEX IS ESSENTIAL. Only the pool used to be taken and it was
        /// always the LAST person who went: the player pressed the button on
        /// the "Cook 2" card and the game removed Cook 1. A surly cook drags
        /// the whole crew's morale down (the aura mechanic) and the player
        /// could not remove precisely that one - the staff system's single
        /// painful decision did not work.
        ///
        /// The LAST person slides into the place of whoever is removed;
        /// leaving a gap in the array would litter every loop with an "is it
        /// empty" check.
        /// </summary>
        private void Fire(int pool, int index)
        {
            int[] xp = pool == 0 ? _cookXpDays : _hallXpDays;
            int[] tenure = pool == 0 ? _cookTenure : _hallTenure;
            int[] a = pool == 0 ? _cookTraitA : _hallTraitA;
            int[] b = pool == 0 ? _cookTraitB : _hallTraitB;
            int[] morale = pool == 0 ? _cookMorale : _hallMorale;
            int count = pool == 0 ? _cooks : _hall;

            if (count <= 0 || index < 0 || index >= count || index >= MaxServers)
            {
                Emit(SimEventKind.CommandRejected, (int)CommandKind.Fire, 5);
                return;
            }

            int last = count - 1;
            if (index != last && last < MaxServers)
            {
                xp[index] = xp[last];
                tenure[index] = tenure[last];
                a[index] = a[last];
                b[index] = b[last];
                morale[index] = morale[last];
            }

            if (last < MaxServers)
            {
                xp[last] = 0;
                tenure[last] = 0;
                a[last] = -1;
                b[last] = -1;
                morale[last] = 0;
            }

            int[] names = pool == 0 ? _cookName : _hallName;
            if (index != last && last < MaxServers) names[index] = names[last];
            if (last < MaxServers) names[last] = -1;

            if (pool == 0) _cooks--; else _hall--;
            Emit(SimEventKind.StaffResigned, pool, index);
        }

        // =====================================================================
        // The signature mechanics. docs/07: "the most important line - this
        // is the thing that shows a purchase is not a repaint but ANOTHER
        // GAME." The mechanic is here, the numbers are in cuisines/*.json
        // (docs/23 8.2).
        // =====================================================================

        // =====================================================================
        // Named regulars. docs/11: "a named customer is one single person,
        // written by hand, with a story, and always the same person."
        // =====================================================================

        public int RegularCount { get { return _content.Regulars.Length; } }
        public int RegularVisits(int i)
        {
            return i >= 0 && i < RegularCount ? _regVisits[i] : 0;
        }
        public int RegularBeat(int i)
        {
            return i >= 0 && i < RegularCount ? _regBeat[i] : 0;
        }
        public bool RegularAway(int i)
        {
            return i >= 0 && i < RegularCount && _regAwayDays[i] > 0;
        }
        /// <summary>Their mean satisfaction, in centi-points. 0 if they have never come.</summary>
        public int RegularSatisfactionCenti(int i)
        {
            if (i < 0 || i >= RegularCount || _regVisits[i] == 0) return 0;
            return (int)(_regSatSum[i] / _regVisits[i]);
        }
        /// <summary>
        /// Is this dish the favourite of a regular who has entered the
        /// campaign. The UI will mark it with a star on the menu; the balance
        /// tool protects these while narrowing the menu.
        /// </summary>
        public bool IsFavouriteOfArrivedRegular(int dish)
        {
            return FavouriteRegularOf(dish) >= 0;
        }

        /// <summary>
        /// The index of the regular who favours this dish and HAS ARRIVED;
        /// -1 if there is none.
        ///
        /// Only "does somebody favour it" could be asked before, and the menu
        /// screen showed that with a nameless dot. Without knowing whose
        /// favourite it is, that dot is decoration rather than information.
        /// </summary>
        public int FavouriteRegularOf(int dish)
        {
            RegularDef[] regs = _content.Regulars;
            for (int i = 0; i < regs.Length && i < MaxRegulars; i++)
                if (regs[i].FavouriteDish == dish && regs[i].ArrivesFromDay <= _day)
                    return i;
            return -1;
        }

        /// <summary>Which regular this party is; -1 means the nameless crowd.</summary>
        public int PartyRegular(int party)
        {
            return party >= 0 && party < MaxParties ? _pRegular[party] : -1;
        }

        /// <summary>
        /// Who will drop in today. At the opening of the day, BEFORE the
        /// arrival plan.
        ///
        /// A regular is NOT ADDED to demand but TAKEN OUT of it: otherwise
        /// writing a named customer would inflate the economy, and the
        /// calibration would drift with every new name.
        /// </summary>
        private void PlanRegularVisits()
        {
            RegularDef[] regs = _content.Regulars;
            for (int i = 0; i < regs.Length && i < MaxRegulars; i++)
            {
                if (_regAwayDays[i] > 0) { _regAwayDays[i]--; _regComing[i] = false; continue; }
                _regComing[i] = regs[i].ArrivesFromDay <= _day
                                && _rngRegular.Chance(_economy.RegularVisitChanceBp);
            }
        }

        /// <summary>
        /// Binds the regulars coming today to suitable rows in the arrival
        /// plan. A row of THEIR OWN archetype is looked for first; failing
        /// that, the earliest free row is written in their name.
        /// </summary>
        private void BindRegularsToPlan()
        {
            RegularDef[] regs = _content.Regulars;
            for (int i = 0; i < regs.Length && i < MaxRegulars; i++)
            {
                if (!_regComing[i]) continue;

                int pick = -1;
                for (int k = 0; k < _arrCount; k++)
                {
                    if (_arrRegular[k] >= 0) continue;
                    if (_arrArchetype[k] != regs[i].ArchetypeIndex) continue;
                    pick = k;
                    break;
                }
                if (pick < 0)
                {
                    for (int k = 0; k < _arrCount; k++)
                    {
                        if (_arrRegular[k] >= 0) continue;
                        pick = k;
                        _arrArchetype[k] = regs[i].ArchetypeIndex;
                        break;
                    }
                }
                if (pick < 0) { _regComing[i] = false; continue; }   // no room today
                _arrRegular[pick] = i;
            }
        }

        /// <summary>
        /// Records the visit and checks whether a story beat has opened.
        /// Called at the moment of payment.
        /// </summary>
        private void RecordRegularVisit(int party, int satisfaction)
        {
            int i = _pRegular[party];
            if (i < 0 || i >= RegularCount) return;

            _regVisits[i]++;
            _regSatSum[i] += satisfaction;
            Emit(SimEventKind.RegularVisited, i, satisfaction);

            // A regular who is badly served STAYS AWAY FOR A WHILE. The
            // punishment is not reputation: somebody whose name you know not
            // knocking on the door says more than a number does.
            if (satisfaction < _economy.RegularUpsetCenti)
            {
                _regAwayDays[i] = _economy.RegularAwayDays;
                Emit(SimEventKind.RegularUpset, i, _economy.RegularAwayDays);
                return;
            }

            StoryBeat[] story = _content.Regulars[i].Story;
            int avg = RegularSatisfactionCenti(i);
            while (_regBeat[i] < story.Length)
            {
                StoryBeat b = story[_regBeat[i]];
                if (_regVisits[i] < b.RequiresVisits) break;
                if (avg < b.RequiresSatisfactionCenti) break;
                _regBeat[i]++;
                Emit(SimEventKind.RegularStoryBeat, i, b.Beat);
            }
        }

        /// <summary>The total outstanding tab, in centi-coins.</summary>
        // ---- THE BOOK, FOR THE UI -------------------------------------------
        //
        // The tab stopped being a "bonus button" and started producing
        // decisions (the collection chance now depends on the customer's
        // trust), but the player COULD NOT SEE the book: who owes how much,
        // when it falls due, what their trust is - none of it was on screen,
        // and the mechanic's second decision (collecting early) stood there
        // with no interface at all.
        //
        // The chance is exposed as well, deliberately: no decision can be
        // made on a hidden probability. The number the player sees has to be
        // THE VERY NUMBER the simulation uses - with two separate
        // calculations, the screen would be lying.

        /// <summary>The number of open accounts in the book.</summary>
        public int TabCount { get { return _tabCount; } }

        /// <summary>The account's amount, in centi.</summary>
        public long TabAmount(int tab)
        {
            return tab >= 0 && tab < _tabCount ? _tabAmount[tab] : 0;
        }

        /// <summary>The days left until it falls due. Negative means overdue.</summary>
        public int TabDaysLeft(int tab)
        {
            return tab >= 0 && tab < _tabCount ? _tabDueDay[tab] - _day : 0;
        }

        /// <summary>The account's owner (the regular's index), or -1.</summary>
        public int TabRegular(int tab)
        {
            return tab >= 0 && tab < _tabCount ? _tabRegular[tab] : -1;
        }

        /// <summary>Was tea offered when this account was opened.</summary>
        public bool TabHadTea(int tab)
        {
            return tab >= 0 && tab < _tabCount && _tabTea[tab] != 0;
        }

        /// <summary>
        /// The collection chance if it is left UNTIL IT FALLS DUE, in basis
        /// points.
        ///
        /// THE SAME calculation as SettleTab; it is not written out a second
        /// time there, because two copies drift apart one day and the screen
        /// would show the player a number the simulation does not use.
        /// </summary>
        public int TabCollectChanceBp(int tab)
        {
            if (tab < 0 || tab >= _tabCount) return 0;
            return TabChanceBp(tab, false);
        }

        /// <summary>The collection chance if it is CHASED EARLY, in basis points.</summary>
        public int TabEarlyChanceBp(int tab)
        {
            if (tab < 0 || tab >= _tabCount) return 0;
            return TabChanceBp(tab, true);
        }

        public long OpenCredit
        {
            get
            {
                long t = 0;
                for (int i = 0; i < _tabCount; i++) t += _tabAmount[i];
                return t;
            }
        }

        public int OpenCreditCount { get { return _tabCount; } }
        /// <summary>The demand bonus the tab has built up, in basis points.</summary>
        public int CreditLoyaltyBp { get { return _creditLoyaltyBp; } }
        public bool ComboEnabled { get { return _comboOn; } }
        /// <summary>
        /// Has the signature mechanic opened. docs/09: the first day of the
        /// second season. In the first season the teaching load is already
        /// full with the menu and the price.
        /// </summary>
        public bool SignatureOpen
        {
            get
            {
                return _content.Signature.Kind != SignatureKind.None
                    && _day >= _content.Signature.FromDay;
            }
        }

        public int SignatureFromDay { get { return _content.Signature.FromDay; } }

        /// <summary>Is this cuisine's signature mechanic the tab, AND has it opened.</summary>
        public bool HasCredit
        {
            get { return _content.Signature.Kind == SignatureKind.Credit && SignatureOpen; }
        }
        public bool HasCombo
        {
            get { return _content.Signature.Kind == SignatureKind.Combo && SignatureOpen; }
        }

        /// <summary>
        /// Self service: no waiter, a cleaner who clears the tables
        /// (docs/51). The simulation already branches on the content's own
        /// flag in two places; the VIEW had no way to ask, so it dressed the
        /// cleaner as a waiter.
        /// </summary>
        public bool SelfService { get { return _content.SelfService; } }

        /// <summary>
        /// Can a tab be opened for this party. The rule is DERIVED: only the
        /// FREQUENT archetypes (TierIndex 0). docs/13 put the veresiyeEligible
        /// field in the "regulars" file, but that content does not exist yet;
        /// and a frequent customer is the neighbourhood's regular anyway.
        /// Writing out one more list by hand would mean writing the same
        /// character a second time.
        /// </summary>
        public bool CreditEligible(int party)
        {
            if (!HasCredit) return false;
            if (party < 0 || party >= MaxParties || !_pActive[party]) return false;
            // A tab IS NOT OFFERED, IT IS ASKED FOR. Opening one for
            // somebody who has not asked would mean tying up cash to solve a
            // problem that does not exist.
            return _pAsksCredit[party];
        }

        /// <summary>
        /// Can a tab be opened for this party - the IDENTITY condition.
        ///
        /// docs/13 put the veresiyeEligible field in the regulars file, and
        /// that is the right place: a tab is opened for somebody whose name
        /// you know. With no regulars content the rule was derived from the
        /// FREQUENT archetype; now it comes from the content when there is
        /// any, and the old derivation stands as a fallback when there is not
        /// (the unit tests run without a regulars file).
        /// </summary>
        private bool CreditIdentityOk(int party)
        {
            if (_content.Regulars.Length == 0)
                return _content.Archetypes[_pArchetype[party]].TierIndex == 0;

            int i = _pRegular[party];
            return i >= 0 && i < RegularCount && _content.Regulars[i].TabEligible;
        }

        /// <summary>Is this party asking for a tab. The UI will mark it.</summary>
        /// <summary>
        /// The FIRST party currently asking for a tab, or -1.
        ///
        /// The UI was writing the same loop itself; the tour would have
        /// written it too. Three copies means three separate definitions of
        /// "is it eligible" - and one day one of them contradicts another.
        /// </summary>
        public int FirstCreditAsker()
        {
            for (int p = 0; p < MaxParties; p++)
                if (CreditEligible(p)) return p;
            return -1;
        }

        /// <summary>
        /// Has a tab been OPENED for this party.
        ///
        /// ExtendCredit does not write into the book STRAIGHT AWAY: it only
        /// marks the party, and the book entry is created WHEN THE BILL IS
        /// SETTLED. Anything wanting to measure that the command was accepted
        /// (the tour, the tests) measures THE WRONG MOMENT if it looks at
        /// OpenCredit, and takes the command for rejected - I did exactly
        /// that once.
        /// </summary>
        public bool PartyHasCredit(int party)
        {
            return party >= 0 && party < MaxParties && _pActive[party] && _pCredit[party];
        }

        public bool AsksForCredit(int party)
        {
            return party >= 0 && party < MaxParties && _pActive[party] && _pAsksCredit[party];
        }

        private void ExtendCredit(int party)
        {
            SignatureDef sig = _content.Signature;
            if (!HasCredit)
            {
                Emit(SimEventKind.CommandRejected, (int)CommandKind.ExtendCredit, 10);
                return;
            }
            if (!CreditEligible(party) || _pCredit[party])
            {
                Emit(SimEventKind.CommandRejected, (int)CommandKind.ExtendCredit, 11);
                return;
            }
            if (_tabCount >= MaxTabs)
            {
                // 16, NOT 12. Code 12 means the day's command allowance is
                // spent, and the UI looks at that number and writes "that is
                // enough work for today" - which is the wrong sentence for a
                // player whose book is full. The rejection reasons SPEAK TO
                // THE UI; giving the same number to two reasons means telling
                // the player the wrong thing.
                Emit(SimEventKind.CommandRejected, (int)CommandKind.ExtendCredit, 16);
                return;
            }
            long bill = OrderPrice(party) * _pSize[party];
            if (bill > sig.CreditMaxPerRegular)
            {
                Emit(SimEventKind.CommandRejected, (int)CommandKind.ExtendCredit, 13);
                return;
            }
            // A man written into the book gets a glass of tea. Not a custom
            // but a MECHANIC: the tea raises the collection chance (docs/12
            // 3), and its cost is teaCostCenti from the content - a field
            // that until now was not being read.
            long tea = (long)sig.CreditTeaCostCenti * _pSize[party];
            if (_cash >= tea)
            {
                _cash -= tea;
                _teaSpend += tea;
                _pTea[party] = true;
            }

            _pCredit[party] = true;
            Emit(SimEventKind.CreditExtended, party, (int)bill);
        }

        /// <summary>
        /// CHASES an account EARLY, before it falls due. There is a price:
        /// the chance halves, and if it does not come off the account closes
        /// there and then. A real trade-off between "take it now but on worse
        /// odds" and "wait".
        /// </summary>
        private void CollectCredit(int tab)
        {
            if (_content.Signature.Kind != SignatureKind.Credit
                || tab < 0 || tab >= _tabCount)
            {
                Emit(SimEventKind.CommandRejected, (int)CommandKind.CollectCredit, 14);
                return;
            }
            SettleTab(tab, halfChance: true);
        }

        /// <summary>
        /// An account's collection chance, in basis points.
        ///
        /// IN ONE PLACE: both SettleTab and the UI read it from here. The UI
        /// SHOWS the chance to the player (no decision can be made on a
        /// hidden probability), and the number the screen shows must be the
        /// very number the simulation uses.
        ///
        /// THE CHANCE DEPENDS ON WHO IT IS WRITTEN AGAINST. It was fixed,
        /// and together with the repayment bonus it made the tab MORE
        /// PROFITABLE than a cash sale (0.95 x 1.12 = 1.064 x the ticket), so
        /// there was never a day to refuse. Now the customer's visit count is
        /// added as trust: somebody you have just met is a bad bet, somebody
        /// who has come for years is a good one.
        /// </summary>
        private int TabChanceBp(int tab, bool halfChance)
        {
            SignatureDef sig = _content.Signature;
            int chance = sig.CreditCollectChanceBp + TabTrustBp(tab);
            if (_tabTea[tab] != 0) chance += sig.CreditTeaCollectBonusBp;

            // The ceiling is NOT COMPLETE CERTAINTY: a riskless book would
            // once again be a bonus button that produces no decision.
            if (chance > sig.CreditChanceCapBp) chance = sig.CreditChanceCapBp;
            return halfChance ? chance / 2 : chance;
        }

        /// <summary>
        /// Closes an account: it is either collected or it goes bad. When it
        /// goes bad reputation falls - having to chase somebody sours the
        /// mood of the place.
        /// </summary>
        private void SettleTab(int tab, bool halfChance)
        {
            SignatureDef sig = _content.Signature;
            long amount = _tabAmount[tab];

            int chance = TabChanceBp(tab, halfChance);

            bool paid = _rngCredit.Chance(chance);
            RemoveTab(tab);

            if (paid)
            {
                // Whoever settles their account puts something on top. The
                // mechanic's earning side.
                long settled = amount + Fx.Bp(amount, sig.CreditRepayBonusBp);
                _cash += settled;
                _revenue += settled;
                _revenueAll += settled;
                _creditCollected += amount;

                // Every account paid leaves LOYALTY behind: that customer
                // comes back. Demand rather than reputation - reputation
                // stops once it reaches the ceiling, but the neighbourhood's
                // trust in you does not.
                _creditLoyaltyBp += sig.CreditLoyaltyDemandBp;
                if (_creditLoyaltyBp > sig.CreditLoyaltyCapBp)
                    _creditLoyaltyBp = sig.CreditLoyaltyCapBp;

                Emit(SimEventKind.CreditCollected, (int)settled, (int)OpenCredit);
            }
            else
            {
                _reputationCenti -= sig.CreditDefaultRepPenaltyCenti;
                if (_reputationCenti < 0) _reputationCenti = 0;

                // A bad account takes the loyalty with it: a customer you
                // have chased does not come back.
                _creditLoyaltyBp -= sig.CreditLoyaltyDemandBp;
                if (_creditLoyaltyBp < 0) _creditLoyaltyBp = 0;

                Emit(SimEventKind.CreditDefaulted, (int)amount,
                     sig.CreditDefaultRepPenaltyCenti);
            }
        }

        /// <summary>
        /// The trust in this account's owner, in basis points.
        ///
        /// It comes from the visit count and is bounded by the ceiling in the
        /// content. There is no trust in a customer whose name is not known
        /// (one who is not a regular) - a tab is only opened for somebody
        /// whose name is known anyway (CreditIdentityOk), but the value can
        /// be -1 if it comes from an old save.
        /// </summary>
        private int TabTrustBp(int tab)
        {
            int r = _tabRegular[tab];
            if (r < 0 || r >= RegularCount) return 0;

            long bp = (long)_regVisits[r] * _content.Signature.CreditTrustPerVisitBp;
            int cap = _content.Signature.CreditTrustCapBp;
            return bp > cap ? cap : (int)bp;
        }

        private void RemoveTab(int tab)
        {
            int last = _tabCount - 1;
            _tabAmount[tab] = _tabAmount[last];
            _tabDueDay[tab] = _tabDueDay[last];
            _tabTea[tab] = _tabTea[last];
            _tabRegular[tab] = _tabRegular[last];
            _tabAmount[last] = 0;
            _tabDueDay[last] = 0;
            _tabTea[last] = 0;
            _tabRegular[last] = -1;
            _tabCount--;
        }

        /// <summary>Closes the accounts that have fallen due. At the opening of the day.</summary>
        private void SettleDueTabs()
        {
            // We deliberately do not look at HasCredit: an account that has
            // been opened has to close, whatever the mechanic is doing.
            if (_content.Signature.Kind != SignatureKind.Credit) return;
            for (int i = _tabCount - 1; i >= 0; i--)
                if (_tabDueDay[i] <= _day) SettleTab(i, halfChance: false);
        }

        /// <summary>
        /// Switches the combo on or off. While it is on, a party that picks
        /// the combo's main dish CERTAINLY takes its side and its drink too,
        /// pays a discounted price for all three, and the kitchen stays busy
        /// for longer.
        /// </summary>
        private void SetCombo(bool on)
        {
            if (!HasCombo)
            {
                Emit(SimEventKind.CommandRejected, (int)CommandKind.SetCombo, 15);
                return;
            }
            _comboOn = on;
        }

        /// <summary>Can we sell the combo: it is on AND all three items are on the menu.</summary>
        /// <summary>
        /// Can the combo be sold right now.
        ///
        /// THE STOCK IS ASKED ABOUT TOO. Only "is it on the menu" and "is it
        /// switched on" used to be checked; the path that picks dishes one by
        /// one (PickFromRole) checks CanMake for every candidate, while the
        /// combo block SKIPPED it. The consequence was measured: even when no
        /// ingredients at all were bought for the side and the drink, 325
        /// sides + 318 drinks were sold over 24 days - at full price, with
        /// ZERO ingredient cost (Consume clamps a negative to zero). So the
        /// combo was a door to free production without stock.
        /// </summary>
        private bool ComboSellable()
        {
            if (!_comboOn || !HasCombo) return false;
            int[] d = _content.Signature.ComboDishes;
            if (d == null) return false;
            for (int i = 0; i < d.Length; i++)
            {
                if (!_dishOnMenu[d[i]] || !Unlocked(d[i])) return false;
                if (!CanMake(d[i], 1)) return false;
            }
            return true;
        }

        /// <summary>A person's experience level. pool 0 kitchen, 1 hall.</summary>
        public int StaffLevel(int pool, int index)
        {
            int[] xp = pool == 0 ? _cookXpDays : _hallXpDays;
            int count = pool == 0 ? _cooks : _hall;
            if (index < 0 || index >= count || index >= MaxServers) return 0;
            return _economy.XpLevelOf(xp[index]);
        }

        /// <summary>
        /// The number of days a person has ACTUALLY worked.
        ///
        /// It used to return `_cookXpDays`, and that is EXPERIENCE - which
        /// depends on the trait. The screen read "Level 0 (0 days)" and for a
        /// `tecrubeli` who had worked for sixty days that was a flat lie.
        /// </summary>
        public int StaffDaysWorked(int pool, int index)
        {
            int[] tenure = pool == 0 ? _cookTenure : _hallTenure;
            int count = pool == 0 ? _cooks : _hall;
            if (index < 0 || index >= count || index >= MaxServers) return 0;
            return tenure[index];
        }

        /// <summary>A person's days of EXPERIENCE. The level comes out of this.</summary>
        public int StaffXpDays(int pool, int index)
        {
            int[] xp = pool == 0 ? _cookXpDays : _hallXpDays;
            int count = pool == 0 ? _cooks : _hall;
            if (index < 0 || index >= count || index >= MaxServers) return 0;
            return xp[index];
        }

        /// <summary>
        /// How experience shortens the work: duration / speed. It is not the
        /// dish's OWN cooking time that shortens but the time the person is
        /// TIED UP by that job - whatever docs/27 Decision D says for
        /// equipment holds for experience too.
        /// </summary>
        private int XpAdjusted(int pool, int index, int ms)
        {
            int[] xp = pool == 0 ? _cookXpDays : _hallXpDays;
            if (index < 0 || index >= MaxServers) return ms;

            // The member of staff's total speed: experience + traits -
            // morale - busyness - fatigue. They all add into the same
            // multiplier, because they are all saying the same thing: how
            // quickly does this person get this job done.
            int speedBp = _economy.XpSpeedBp(_economy.XpLevelOf(xp[index]), pool == 0);
            speedBp += TraitSum(pool, index, t => t.SpeedBp);

            // The morale thresholds of docs/14: below 30, speed -20%.
            if (StaffMorale(pool, index) < _economy.MoraleLowThreshold)
                speedBp -= _economy.MoraleSlowPenaltyBp;

            // Busyness and fatigue ARE NOT ADDED; the larger of the two is
            // taken.
            //
            // Adding them was the first draft and measurement rejected it: a
            // cook who panics in a crowd AND tires quickly fell to -45% in the
            // last quarter of the peak; with low morale's -20% on top of
            // that, the person all but stopped. On fast food a good player's
            // reputation fell from 96.5 to 87, and on some runs the shop
            // emptied on day 50.
            //
            // docs/14 writes these two traits for SEPARATE SITUATIONS ("in
            // busy slots", "in the last quarter of the day"); paying for both
            // when they overlap is not what the design says. Being bad at a
            // bad moment is enough; being doubly bad is not required.
            int situational = 0;
            if (InPeakSlot() && !TraitAny(pool, index, t => t.PeakImmune))
                situational = TraitSum(pool, index, t => t.PeakPenaltyBp);
            if (InLastQuarter() && !TraitAny(pool, index, t => t.FatigueImmune))
            {
                int f = TraitSum(pool, index, t => t.FatiguePenaltyBp);
                if (f > situational) situational = f;
            }
            speedBp -= situational;

            // The floor is 50%. It was 20% in the first draft - that is, a
            // task could take five times as long. A pit that deep stops a
            // trait being a personal difference and makes it the thing that
            // decides the run.
            if (speedBp < 5000) speedBp = 5000;
            if (speedBp == Fx.One) return ms;

            int adjusted = (int)Fx.MulDiv(ms, Fx.One, speedBp);
            return adjusted < 1 ? 1 : adjusted;
        }

        /// <summary>Are we in the busy slot of the service day.</summary>
        private bool InPeakSlot()
        {
            int peak = PeakSlotToday();
            if (peak < 0) return false;
            int start = _timing.SlotStartTick(peak);
            return _serviceTick >= start && _serviceTick < start + _timing.SlotTicks(peak);
        }

        /// <summary>
        /// The busiest slot of TODAY, by density - guests per tick.
        /// </summary>
        /// <remarks>
        /// THIS USED TO RETURN THE LONGEST SLOT, AND THAT WAS EXACTLY
        /// BACKWARDS.
        ///
        /// The reasoning was sound when it was written: docs/28 Decision G
        /// shaped the day by changing slot DURATION rather than arrival
        /// share, so the peak really was the longest slot. Then docs/48
        /// sharpened the day by making the busy slot SHORT - the same
        /// arrivals pressed into less time. The comment kept the old
        /// conclusion and nothing re-measured it.
        ///
        /// Measured on the shipped content, arrivals over duration:
        ///   fast food  0.54 / 1.93 / 0.47 / 1.61   longest = slot 2
        ///   Turkish    0.98 / 1.90 / 0.45 / 0.97   longest = slot 2
        /// So "the busy slot" was firing during the EMPTIEST part of the
        /// day, in both cuisines. Two of the twelve traits hang off it and
        /// both were inverted: `kalabalikta_panikleyen` (-25% speed in a
        /// rush) cost nothing during the rush and slowed the quiet
        /// afternoon, and `sakin` - the trait you hire FOR the rush -
        /// bought nothing at all. The staff screen told the player the
        /// opposite of what the game did.
        ///
        /// Density is read off TODAY'S arrival plan rather than the
        /// content, so it follows the day that is actually being played
        /// and needs no second copy of the shape of the day. The answer is
        /// cached per day; the plan does not change within one.
        /// </remarks>
        /// <summary>
        /// Today's busiest slot, or -1 before service is planned. Public so
        /// a test can hold it to the density rule instead of the duration
        /// rule it silently had for weeks.
        /// </summary>
        public int PeakSlotIndex { get { return PeakSlotToday(); } }

        private int PeakSlotToday()
        {
            // AN EMPTY PLAN IS NOT AN ANSWER, AND IT MUST NOT BE CACHED.
            //
            // The plan is built by OpenService and cleared by the day
            // advance, so before service `_arrCount` is 0 - every slot then
            // holds zero guests, the loop below picks the FIRST one, and the
            // per-day cache would hold that for the whole day. The traits
            // would then treat slot 0 as the rush whatever the day looked
            // like.
            //
            // Nothing reads it that early today; `PeakSlotIndex` is public
            // now, which is exactly how a caller like that turns up.
            if (_arrCount <= 0) return -1;

            if (_peakSlotDay == _day) return _peakSlot;

            int slots = _timing.SlotCount;
            if (slots <= 0) { _peakSlotDay = _day; _peakSlot = -1; return -1; }

            // Guests per slot, from the plan.
            int best = -1;
            long bestNum = -1, bestDen = 1;
            for (int i = 0; i < slots; i++)
            {
                int ticks = _timing.SlotTicks(i);
                if (ticks <= 0) continue;
                int start = _timing.SlotStartTick(i);
                long people = 0;
                for (int a = 0; a < _arrCount; a++)
                    if (_arrTick[a] >= start && _arrTick[a] < start + ticks)
                        people += _arrSize[a];

                // people/ticks compared as a fraction - no float in the core.
                if (best < 0 || people * bestDen > bestNum * ticks)
                {
                    best = i; bestNum = people; bestDen = ticks;
                }
            }

            _peakSlotDay = _day;
            _peakSlot = best;
            return best;
        }

        /// <summary>The last quarter of the day. docs/14, "tires quickly".</summary>
        private bool InLastQuarter()
        {
            return _serviceTick * 4 >= _timing.ServiceTicks * 3;
        }

        /// <summary>
        /// Buys an ingredient at the market. Paid in cash; rejected if the
        /// till cannot cover it. docs/02: the morning stage.
        /// </summary>
        private void OrderIngredient(int ingredient, int grams)
        {
            if (ingredient < 0 || ingredient >= _stockGrams.Length || grams <= 0)
            {
                Emit(SimEventKind.CommandRejected, (int)CommandKind.OrderIngredient, 3);
                return;
            }
            if (!Buy(ingredient, grams))
                Emit(SimEventKind.CommandRejected, (int)CommandKind.OrderIngredient, 9);
        }

        /// <summary>
        /// Makes the purchase. Returns false and changes NOTHING AT ALL if
        /// there is not enough money.
        ///
        /// It stands apart because it has two callers: the command that buys
        /// a single item, and the command that buys the whole recommended
        /// stock. The second tries fifty items and silently skips the ones it
        /// cannot afford; raising a rejection event for each of them would do
        /// nothing but fill up the notification area.
        /// </summary>
        private bool Buy(int ingredient, int grams)
        {
            // THE SEASONAL PRICE. docs/09 divides the campaign into four
            // seasons, and 35 of the 77 ingredients in the content have real
            // movement: tomatoes 14% cheaper in summer, 20% dearer in winter.
            // Until this line the simulation always paid the base price.
            long perKilo = _content.Ingredients[ingredient].PriceAt(Season, _quality);
            perKilo = Fx.MulDiv(perKilo, _marketBp[ingredient], Fx.One);
            long cost = Fx.MulDiv(perKilo, grams, GramsPerKilo);
            if (cost > _cash) return false;

            _cash -= cost;
            _ingredientSpend += cost;

            // New goods mix with the old: the age is a WEIGHTED MEAN.
            // "Reset on purchase" meant buying one gram a day and stopping
            // the clock forever.
            int had = _stockGrams[ingredient];
            if (had > 0 && _stockAgeDays[ingredient] > 0)
                _stockAgeDays[ingredient] =
                    (int)Fx.MulDiv(_stockAgeDays[ingredient], had, had + grams);
            else
                _stockAgeDays[ingredient] = 0;

            // The quality mixes too: somebody who buys cheap and then buys
            // dear is not rid of the cheap goods on their hands straight
            // away.
            int bought = _content.Ingredients[ingredient].QualityDelta(_quality);
            _stockQualityCenti[ingredient] = had > 0
                ? (int)((( long)_stockQualityCenti[ingredient] * had + (long)bought * grams)
                        / (had + grams))
                : bought;

            _stockGrams[ingredient] += grams;
            return true;
        }

        /// <summary>
        /// Takes a loan. Only one loan can be carried at a time.
        /// The total repayment is 1.35 times the principal, in eight equal
        /// weekly instalments.
        /// </summary>
        private void TakeLoan(int option)
        {
            if (option < 0 || option >= _economy.LoanOptions.Length)
            {
                Emit(SimEventKind.CommandRejected, (int)CommandKind.TakeLoan, 10);
                return;
            }
            if (_loanWeeksLeft > 0)
            {
                Emit(SimEventKind.CommandRejected, (int)CommandKind.TakeLoan, 11);
                return;
            }

            long principal = _economy.LoanOptions[option];
            long repay = Fx.Bp(principal, _economy.LoanMultiplierBp);

            _cash += principal;
            _loanTaken += principal;
            _loanInstallment = Fx.CeilDivL(repay, _economy.LoanWeeks);
            _loanWeeksLeft = _economy.LoanWeeks;
            _loanTotalRepaid = 0;
        }

        /// <summary>
        /// The crew required against today's PEAK day. The capacity model of
        /// docs/14. The strategies and the UI use this; the number of angry
        /// customers is not a staffing signal.
        /// </summary>
        /// <summary>
        /// The crew required TODAY. The name is now true.
        ///
        /// Its old form used the WEEKEND multiplier every day, so despite
        /// saying "today" it was always measuring THE PEAK. The balance tool
        /// showed what that cost: a player hiring as many waiters as the
        /// capacity model told them to earned 5,300 coins less than a player
        /// working ONE WAITER SHORT (18,712 against 24,022). Wages are paid
        /// every day; the peak falls two days a week.
        ///
        /// The name was lying and the lie was expensive: the UI wrote "you
        /// need 3 today", the player hired, and they lost money.
        ///
        /// The peak is a separate question with a separate method
        /// (RequiredCrewPeak): a player wanting to prepare for the weekend
        /// needs to see that too.
        /// </summary>
        public Crew RequiredCrewToday()
        {
            int today = ExpectedCustomers(
                IsWeekend(_day) ? _economy.WeekendMultiplierBp
                                : _economy.WeekdayMultiplierBp);
            return StaffingModel.Required(today, _economy);
        }

        /// <summary>
        /// The crew required TOMORROW.
        ///
        /// Crew decisions are taken IN THE EVENING but they affect the next
        /// day: a player who sets a small crew because today is a weekday
        /// walks into the peak short-handed if tomorrow is the weekend.
        /// RequiredCrewToday's name is right; its CALLER was asking at the
        /// wrong moment.
        /// </summary>
        public Crew RequiredCrewTomorrow()
        {
            int tomorrow = ExpectedCustomers(
                IsWeekend(_day + 1) ? _economy.WeekendMultiplierBp
                                    : _economy.WeekdayMultiplierBp);
            return StaffingModel.Required(tomorrow, _economy);
        }

        /// <summary>The crew required at the weekend peak.</summary>
        public Crew RequiredCrewPeak()
        {
            int peak = ExpectedCustomers(_economy.WeekendMultiplierBp);
            return StaffingModel.Required(peak, _economy);
        }

        public bool HasLoan { get { return _loanWeeksLeft > 0; } }
        public int LoanWeeksLeft { get { return _loanWeeksLeft; } }
        public long LoanInstallment { get { return _loanInstallment; } }

        /// <summary>The weekly fixed costs: rent, wages and the loan instalment if there is one.</summary>
        public long WeeklyFixedCost()
        {
            Crew crew = new Crew(_cooks, _hall);
            int week = _day / 7 + 1;

            // The trait multiplier has to be HERE TOO. Without it the
            // player's (and the balance tool's) budget guard showed the real
            // bill as smaller than it was: one experienced cook raises the
            // wage by 30%, the guard did not see it, and crew and equipment
            // were bought against that wrong number. Measured: the fast food
            // good player's reputation fell from 96.5 to 86.9 and the shop
            // emptied on day 36 - that was the only cause.
            long wages = Fx.MulDiv(StaffingModel.WeeklyWageBill(crew, week, _economy),
                                   TraitWageMultiplierBp(), Fx.One);
            return wages
                   + _economy.TierForTables(_tableCount).Rent
                   + (_loanWeeksLeft > 0 ? _loanInstallment : 0);
        }

        /// <summary>
        /// The opening stock: as much as the menu asks for on day one.
        ///
        /// RecommendedRestock already knows the menu, the expected demand,
        /// the safety margin and the per-dish floor - and that is exactly
        /// what is in the store of the shop you inherit. It returns zero for
        /// an ingredient used in no dish, so the store is NOW SELECTIVE.
        /// </summary>
        private void RestockForOneDay()
        {
            for (int i = 0; i < _stockGrams.Length; i++)
            {
                int need = RecommendedRestock(i);
                // A little extra of the non-perishables (salt, flour, oil):
                // they do not go into the bin anyway, and spending the first
                // morning buying twenty items turns the game's first minute
                // into a spreadsheet.
                if (need > 0 && !_content.Ingredients[i].Perishable)
                    need *= 2;
                _stockGrams[i] = need;
            }
        }

        /// <summary>Can this dish be made from the current stock, at the given number of portions.</summary>
        public bool CanMake(int dishIndex, int servings)
        {
            if (dishIndex < 0 || dishIndex >= _content.Dishes.Length) return false;
            DishIngredient[] parts = _content.Dishes[dishIndex].Ingredients;
            for (int i = 0; i < parts.Length; i++)
                if (_stockGrams[parts[i].IngredientIndex] < parts[i].Grams * servings)
                    return false;
            return true;
        }

        private void Consume(int dishIndex, int servings)
        {
            if (dishIndex < 0) return;
            DishIngredient[] parts = _content.Dishes[dishIndex].Ingredients;
            for (int i = 0; i < parts.Length; i++)
            {
                int idx = parts[i].IngredientIndex;
                _stockGrams[idx] -= parts[i].Grams * servings;
                if (_stockGrams[idx] < 0) _stockGrams[idx] = 0;
            }
        }

        public int StockOf(int ingredient)
        {
            return (ingredient >= 0 && ingredient < _stockGrams.Length)
                ? _stockGrams[ingredient] : 0;
        }

        /// <summary>
        /// The stock recommended for today, in grams. Worked out from the
        /// menu: every open dish's expected portions multiplied by the
        /// grammage in that dish's recipe.
        ///
        /// This is the core side of the "ingredient ordering becomes
        /// automatic" feature of docs/02 principle 2. The player can order it
        /// with a single tap; anyone who wants to pick by hand still can.
        /// </summary>
        public int RecommendedRestock(int ingredient)
        {
            return RecommendedRestock(ingredient, ignoreStock: false);
        }

        /// <summary>
        /// ignoreStock: ignores whatever is in hand, that is, it gives ONE
        /// DAY'S requirement itself.
        /// </summary>
        private int RecommendedRestock(int ingredient, bool ignoreStock)
        {
            if (ingredient < 0 || ingredient >= _stockGrams.Length) return 0;

            int dayFactorBp = IsWeekend(_day)
                ? _economy.WeekendMultiplierBp : _economy.WeekdayMultiplierBp;
            // The tab's loyalty is inside ExpectedCustomers now, with every
            // other demand term, so the stock and the arrivals agree.
            int people = ExpectedCustomers(dayFactorBp);
            if (people <= 0) return 0;

            // How many OPEN dishes there are in each role; orders spread
            // evenly within a role.
            //
            // A fixed "/4" used to be written for the side and the drink -
            // meaning "there are about four options". With a single drink
            // open on the menu, that division stocked a quarter of what was
            // needed and the customer was turned back at the door. The count
            // is now real.
            int mains = 0, sides = 0, drinks = 0, desserts = 0;
            for (int i = 0; i < _content.Dishes.Length; i++)
            {
                if (!_dishOnMenu[i] || !Unlocked(i)) continue;
                string g = _content.Dishes[i].Group;
                if (_content.IsInRole(g, _content.MainGroups)) mains++;
                else if (_content.IsInRole(g, _content.SideGroups)) sides++;
                else if (_content.IsInRole(g, _content.DrinkGroups)) drinks++;
                else if (_content.IsInRole(g, _content.DessertGroups)) desserts++;
            }
            if (mains == 0) mains = 1;
            if (sides == 0) sides = 1;
            if (drinks == 0) drinks = 1;
            if (desserts == 0) desserts = 1;

            long grams = 0;
            for (int i = 0; i < _content.Dishes.Length; i++)
            {
                DishDef d = _content.Dishes[i];
                if (!_dishOnMenu[i] || !Unlocked(i)) continue;

                // A main for everybody; the side, the drink and THE DESSERT
                // by chance.
                //
                // The dessert arrived here later and it had to: the ordering
                // model WANTS a dessert (DessertChanceBp, line 3271) while
                // the market model was BUYING NONE of its ingredients. So the
                // desserts of both cuisines became unreachable once the
                // opening stock ran out - CanMake returned false, the order
                // silently fell to -1, and the player saw nothing at all.
                //
                // It stayed covered up as long as the opening stock put six
                // kilos of every one of the seventy-seven ingredients in;
                // once the stock was tied to the menu, the tests broke with
                // "no DESSERT was ordered at all".
                int shareBp;
                if (_content.IsInRole(d.Group, _content.MainGroups))
                    shareBp = Fx.One / mains;
                else if (_content.IsInRole(d.Group, _content.SideGroups))
                    shareBp = _economy.SideChanceBp / sides;
                else if (_content.IsInRole(d.Group, _content.DrinkGroups))
                    shareBp = _economy.DrinkChanceBp / drinks;
                else if (_content.IsInRole(d.Group, _content.DessertGroups))
                    shareBp = _economy.DessertChanceBp / desserts;
                else
                    continue;

                for (int k = 0; k < d.Ingredients.Length; k++)
                {
                    if (d.Ingredients[k].IngredientIndex != ingredient) continue;

                    long expected = Fx.MulDiv((long)people * d.Ingredients[k].Grams,
                                              shareBp, Fx.One);

                    // A floor: every dish on the menu must be able to serve
                    // AT LEAST one party. The stock check is done per party;
                    // the daily average may look sufficient, and yet when a
                    // single party of four asks for that dish the stock is
                    // short and the customer is turned back at the door.
                    long floorGrams = (long)d.Ingredients[k].Grams * MinPartyBuffer;

                    // THE SAFETY MARGIN GOES ON THE FORECAST, NOT ON THE FLOOR.
                    //
                    // The 20% below exists because demand fluctuates around
                    // the forecast. The floor is not a forecast: it is already
                    // a buffer - a full party's worth held for a dish that
                    // may sell one portion today. Multiplying a buffer by a
                    // safety margin bought 4.8 portions of every perishable
                    // for every low-selling dish, every day, and at four
                    // tables without a cold store all of it died that night.
                    // Measured on the Turkish non-expander (docs/63 10) as
                    // part of a 41% spoilage rate. So the margin applies to
                    // the expected demand, and the floor stands as written.
                    long withMargin = Fx.Bp(expected, 12000);
                    grams += withMargin > floorGrams ? withMargin : floorGrams;
                }
            }

            long missing = grams - (ignoreStock ? 0 : _stockGrams[ingredient]);
            return missing > 0 ? (int)missing : 0;
        }

        /// <summary>
        /// Buys the whole of the recommended stock. Items it cannot afford
        /// are silently skipped - fifty separate rejection events would do
        /// nothing but fill up the day's notification area.
        /// </summary>
        private void OrderRecommended()
        {
            if (_phase != DayPhase.Morning)
            {
                Emit(SimEventKind.CommandRejected,
                     (int)CommandKind.OrderRecommended, 2);
                return;
            }

            int bought = 0;
            for (int i = 0; i < _stockGrams.Length; i++)
            {
                int need = RecommendedRestock(i);
                if (need <= 0) continue;
                if (Buy(i, need)) bought++;
            }
            // If nothing at all could be bought, the player has to be told:
            // the button was pressed and the till did not move.
            if (bought == 0)
                Emit(SimEventKind.CommandRejected,
                     (int)CommandKind.OrderRecommended, 9);
        }

        /// <summary>
        /// ONE DAY'S requirement, independent of whatever is in hand.
        ///
        /// RecommendedRestock gives "the shortfall", so it returns zero when
        /// the stock is full. For the UI to be able to offer buying extra,
        /// the requirement ITSELF is needed: buying three days' worth on a
        /// cheap day is the very thing the cold store buys you - and that
        /// decision cannot be offered without knowing the requirement.
        /// </summary>
        public int DailyNeed(int ingredient)
        {
            if (ingredient < 0 || ingredient >= _stockGrams.Length) return 0;

            // It works this out WITHOUT ZEROING THE STOCK IN HAND.
            //
            // It used to zero the stock temporarily, call RecommendedRestock,
            // and then write it back. This is a READ function and the view
            // layer calls it per ingredient every time the market screen is
            // built - a write right in the middle of the rule "the view reads
            // the simulation, it does not write to it".
            //
            // It looked harmless because the write-back came immediately
            // after; but had there been an overflow in the calculation in
            // between (Fx.MulDiv throws on overflow) the stock would have
            // been left at zero PERMANENTLY.
            return RecommendedRestock(ingredient, ignoreStock: true);
        }

        /// <summary>
        /// The most days' worth of this ingredient it makes sense to buy.
        ///
        /// For a non-perishable there is no limit (three days is enough, more
        /// than that ties up cash). For a perishable, as long as the cold
        /// store holds it: without a cold store, one day, because all of it
        /// goes overnight.
        /// </summary>
        public int MaxUsefulDays(int ingredient)
        {
            if (!IsPerishable(ingredient)) return 3;
            int keep = KeepDays(ingredient);
            if (keep < 1) keep = 1;
            return keep > 3 ? 3 : keep;
        }

        /// <summary>The customers expected today. For the strategies and the UI.</summary>
        /// <summary>
        /// How many of the dishes on the menu CAN BE MADE from the current
        /// stock.
        ///
        /// For the morning's preparation summary: before opening service the
        /// player should see the answer to "will the stock get through
        /// today". If one dish can be made the day limps; if none can, the
        /// day is lost from the start, and learning that from the evening
        /// report is too late.
        ///
        /// A crude but honest measure: the number of makeable dishes on the
        /// menu. A real answer to "how many days will it last" needs a demand
        /// forecast, and that is a number too uncertain for the morning
        /// screen to carry.
        /// </summary>
        /// <summary>
        /// HOW MANY DISHES on the menu can be made. NOT days, a dish count.
        ///
        /// Its name was once StockDaysLeft and it promised days; its body
        /// counted "dishes of which at least one portion can be made". The UI
        /// looked at it and gave a green tick: a player with ONE portion each
        /// of six dishes looked "ready", opened service, and ran out of goods
        /// in the first ten minutes.
        ///
        /// The name now says what it does; the answer to "is it enough for
        /// the day" is in StockCoverageBp.
        /// </summary>
        public int MakeableDishCount()
        {
            int makeable = 0;
            for (int i = 0; i < _dishOnMenu.Length; i++)
            {
                if (!_dishOnMenu[i] || !Unlocked(i)) continue;
                if (CanMake(i, 1)) makeable++;
            }
            return makeable;
        }

        /// <summary>
        /// What share of today's expected demand is in hand. 10000 = all of it.
        ///
        /// THE MEASURE IS THE SCARCEST INGREDIENT, not the total. Looking at
        /// the total stock misleads: a kitchen with twenty ingredients in
        /// plenty and one of them out looks "full" in total, while every
        /// order wanting that one ingredient is turned back at the door. What
        /// decides the day is the scarcest.
        ///
        /// The requirement comes from the market's own calculation
        /// (RecommendedRestock, ignoreStock: true) - so the number the screen
        /// promises and the quantity the market screen recommends come out of
        /// THE SAME source. With two separate calculations one would
        /// contradict the other.
        /// </summary>
        public int StockCoverageBp()
        {
            int lowest = int.MaxValue;
            for (int i = 0; i < _stockGrams.Length; i++)
            {
                int needed = RecommendedRestock(i, true);
                if (needed <= 0) continue;

                int have = _stockGrams[i];
                int bp = have >= needed ? Fx.One : (int)Fx.MulDiv(have, Fx.One, needed);
                if (bp < lowest) lowest = bp;
            }

            // If no ingredient is needed at all the menu is empty; that is a
            // separate warning.
            return lowest == int.MaxValue ? 0 : lowest;
        }

        public int ExpectedPeopleToday()
        {
            int dayFactorBp = IsWeekend(_day)
                ? _economy.WeekendMultiplierBp : _economy.WeekdayMultiplierBp;
            return ExpectedCustomers(dayFactorBp);
        }

        /// <summary>
        /// Is the dish unlocked. Three conditions at once:
        ///   1. has the day come     (the tempo floor, docs/09)
        ///   2. is reputation enough (the thing you earn)
        ///   3. is the equipment there (the thing you buy)
        ///
        /// Without the second and third conditions the unlock was A
        /// CALENDAR: the dishes opened of their own accord with the player
        /// doing nothing. Now buying equipment opens menu, so the equipment
        /// ladder buys not only speed but CONTENT.
        /// </summary>
        private bool Unlocked(int dish)
        {
            DishDef d = _content.Dishes[dish];
            if (d.UnlockDay > _day) return false;
            if (_reputationCenti < d.UnlockReputationCenti) return false;
            if (d.RequiresStationTier > 0
                && _stationTier[d.StationIndex] < d.RequiresStationTier) return false;
            return true;
        }

        public bool IsUnlocked(int dish)
        {
            return dish >= 0 && dish < _content.Dishes.Length
                && Unlocked(dish);
        }

        /// <summary>Is this dish on the menu right now. For the UI.</summary>
        /// <summary>
        /// Is this dish a MAIN. The role comes from the content; the cuisines
        /// use their own group names (docs/33) and a hard-coded list had
        /// produced zero customers on the second cuisine.
        /// </summary>
        public bool IsMainDish(int dish)
        {
            if (dish < 0 || dish >= _content.Dishes.Length) return false;
            return _content.IsInRole(_content.Dishes[dish].Group, _content.MainGroups);
        }

        public bool IsOnMenu(int dish)
        {
            return dish >= 0 && dish < _dishOnMenu.Length && _dishOnMenu[dish];
        }

        public bool IsMain(int dish)
        {
            return dish >= 0 && dish < _content.Dishes.Length
                && _content.IsInRole(_content.Dishes[dish].Group, _content.MainGroups);
        }

        /// <summary>
        /// The dish's role on the menu: 0 main, 1 side, 2 drink, 3 dessert,
        /// -1 none of those. Read-only, for whoever narrows a menu - the
        /// balance bots first, and the same question a menu screen asks.
        /// </summary>
        public int DishRole(int dish)
        {
            if (dish < 0 || dish >= _content.Dishes.Length) return -1;
            string g = _content.Dishes[dish].Group;
            if (_content.IsInRole(g, _content.MainGroups)) return 0;
            if (_content.IsInRole(g, _content.SideGroups)) return 1;
            if (_content.IsInRole(g, _content.DrinkGroups)) return 2;
            if (_content.IsInRole(g, _content.DessertGroups)) return 3;
            return -1;
        }

        /// <summary>
        /// How likely one guest is to order from a role, in basis points:
        /// a main for everybody, the rest by chance - the same numbers the
        /// market recommendation buys against.
        /// </summary>
        public int RoleChanceBp(int role)
        {
            switch (role)
            {
                case 0: return Fx.One;
                case 1: return _economy.SideChanceBp;
                case 2: return _economy.DrinkChanceBp;
                case 3: return _economy.DessertChanceBp;
                default: return 0;
            }
        }

        public int IngredientCount { get { return _content.Ingredients.Length; } }
        public long IngredientPrice(int i) { return _content.Ingredients[i].BasePrice; }
        public int StockOutEvents { get { return _stockOutEvents; } }

        private void Intervene(int party, InterventionKind kind)
        {
            // Hurrying a station along is done to A STATION rather than to a
            // table; in that case the A field is a station index, not a
            // table.
            if (kind == InterventionKind.RushStation)
            {
                RushStation(party);
                return;
            }

            // AN UNKNOWN KIND NO LONGER FALLS SILENTLY THROUGH TO THE TEA.
            //
            // The branches below ask "is it OwnerAttention?" and, if not,
            // behaved like THE TEA. So an undefined kind (for instance
            // default(InterventionKind)) took the tea's effect WITHOUT PAYING
            // FOR IT. It is now rejected outright.
            if (kind != InterventionKind.FreeTea
                && kind != InterventionKind.OwnerAttention)
            {
                Emit(SimEventKind.CommandRejected, (int)CommandKind.Intervene, 10);
                return;
            }

            if (_interventionsLeft <= 0)
            {
                Emit(SimEventKind.CommandRejected, (int)CommandKind.Intervene, 8);
                return;
            }

            // THE TEA NOW GOES TO THE WHOLE HALL, NOT TO ONE TABLE.
            //
            // Its old form LEFT ONE OF THE THREE VERBS DEAD. The tea was
            // below the owner's attention on every axis: 900 satisfaction
            // against 2400, x1 patience against x2, and it did not speed the
            // kitchen up - and on top of that it TAKES MONEY OUT OF THE TILL,
            // while attention is free. Because they burn the same
            // intervention allowance, there was never a day to press the tea.
            // A player who understood never touched that button across the
            // sixty days; one who did not paid money and got half as much. It
            // was a trap taking up space on screen.
            //
            // Now the two answer DIFFERENT QUESTIONS:
            //   attention -> a deep intervention on ONE table (x2 patience +
            //                moving its kitchen job up). For the single table
            //                in crisis.
            //   tea       -> a shallow intervention for EVERYONE WAITING. At
            //                the peak, with six tables growing impatient at
            //                once.
            //
            // Its cost comes from the same place: the tea now costs as much
            // as the head count of EVERYONE waiting in the hall. So in a
            // crowd it is both the most valuable and the most expensive.
            //
            // IT WANTS NO TARGET, which is why the party's validity is
            // checked AFTER this branch. In the first draft it was below the
            // check, and the -1 the UI sends when nothing is selected was
            // silently rejected: the button did nothing. A test caught it.
            if (kind == InterventionKind.FreeTea)
            {
                int people = 0;
                for (int i = 0; i < MaxParties; i++)
                    if (_pActive[i] && DrainRateBp(i) > 0) people += _pSize[i];

                // In a hall with nobody waiting there is nobody to send it
                // to; it must not burn an allowance for nothing.
                if (people <= 0)
                {
                    Emit(SimEventKind.CommandRejected, (int)CommandKind.Intervene, 11);
                    return;
                }

                long cost = _economy.TreatCost * people;
                if (_cash < cost)
                {
                    Emit(SimEventKind.CommandRejected, (int)CommandKind.Intervene, 9);
                    return;
                }
                _cash -= cost;
                _teaSpend += cost;
                _interventionsLeft--;

                int extra = _timing.SeatOrderMs * _economy.TreatPatienceMult;
                for (int i = 0; i < MaxParties; i++)
                {
                    if (!_pActive[i] || DrainRateBp(i) <= 0) continue;
                    _pTea[i] = true;
                    _pBonusCenti[i] += _economy.TreatSatisfactionCenti;
                    _pPatienceLeftMs[i] += extra;
                }
                return;
            }

            if (party < 0 || party >= MaxParties || !_pActive[party])
            {
                Emit(SimEventKind.CommandRejected, (int)CommandKind.Intervene, 6);
                return;
            }

            _interventionsLeft--;

            // THE REWARD IS PAID IN TIME, NOT IN SATISFACTION.
            //
            // Measured: the bot that intervened earned LESS than the one that
            // never intervened at all (26,526 against 26,969). The reason was
            // arithmetic - four interventions a day x twenty parties, +20
            // points to 20% of the parties, so +4 points on the mean; and the
            // mean satisfaction is already 78 while reputation's only
            // threshold is 62. The reward was being paid into a SATURATED
            // axis and could change nothing.
            //
            // Adding patience, on the other hand, does not saturate: the
            // waiting table does not leave, the table turnover rate rises,
            // and that means revenue directly. The owner's attention SAVES a
            // day rather than prettifying it.
            //
            // Only OwnerAttention reaches here now: the tea finishes its own
            // path in the branch above and returns.
            _pBonusCenti[party] += _economy.AttentionSatisfactionCenti;

            _pPatienceLeftMs[party] += _timing.SeatOrderMs
                                     * _economy.AttentionPatienceMult;

            // Paying attention to a table waiting for its food moves its
            // kitchen job up as well: the owner clears the path, they do not
            // cook.
            HurryPartyJob(party);

            // AND THE OWNER TAKES ON THE HALL WORK TOO.
            //
            // Attention used to lengthen patience and move THE KITCHEN up
            // only; it did not touch the hall side at all, and yet that is
            // usually where the bottleneck is. Measured: the bot that
            // intervened finished in the same place as the one that did not
            // (18,869 / 18,670), because the mechanic only helped IN A
            // CRISIS, and a crisis hardly ever happens.
            //
            // The next piece of hall work is shortened: the table turns over
            // faster, so more customers on the same day.
            _pAttended[party] = true;
        }

        /// <summary>
        /// Shortens this party's job in the kitchen.
        ///
        /// The owner DOES NOT COOK (docs/14 forbids it); they change the
        /// priority. The effect is a third of the job's remaining wall clock.
        /// </summary>
        /// <summary>
        /// The station a party's food is cooking on, or -1 if nothing of
        /// theirs is in the kitchen.
        ///
        /// IT EXISTS SO THAT "HURRY THE KITCHEN" CAN HAVE A TARGET. The
        /// button picked BusiestStation() for the player - the only one of
        /// the three verbs where the game still answered the WHO - and the
        /// note beside it records why the fix could not be a label: the
        /// station's name made that one button 319 dp and the strip 1,103 dp
        /// on an 873 dp screen.
        ///
        /// So the target comes from the selection the player has already
        /// made. Tapping a table already aims the tea and the owner's
        /// attention; now it aims the kitchen too, at the station THAT table
        /// is waiting on. With nothing selected the old behaviour stands, so
        /// a player who never zooms in loses nothing.
        /// </summary>
        public int StationOfParty(int party)
        {
            if (party < 0) return -1;
            for (int j = 0; j < _jobStation.Length; j++)
            {
                if (_jobState[j] == 0) continue;
                if (j / MaxJobsPerParty != party) continue;
                return _jobStation[j];
            }
            return -1;
        }

        private void HurryPartyJob(int party)
        {
            // THERE WERE TWO BUGS AT ONCE.
            //
            // (1) THE INDEX SPACE. _kitchenTaskTarget holds not a PARTY but a
            //     JOB: DispatchKitchen writes "job" and job =
            //     party * MaxJobsPerParty + k. A raw comparison was being
            //     made here (target != party), so party number 7's job was
            //     never hurried; instead the job with JOB INDEX 7 - party
            //     number 1's fourth item - was hurried. For parties 0-3 it
            //     all went to party 0's job. CancelTasksFor, one function
            //     below, does the division CORRECTLY; here it had been
            //     forgotten.
            //
            // (2) THE WRONG DURATION. What was being shortened was
            //     _kitchenTaskLeftMs: the time the cook IS TIED UP by the
            //     job. The dish's wall clock is _jobMs, and that is what the
            //     customer is waiting on. RushStation does it right.
            //
            // Together the two produced this: the player spends their
            // allowance, reads the "attention given" bubble, and nothing in
            // the kitchen changes.
            for (int j = 0; j < _jobStation.Length; j++)
            {
                if (_jobState[j] == 0) continue;
                if (j / MaxJobsPerParty != party) continue;

                int cut = _jobMs[j] / 3;
                _jobMs[j] -= cut;
                if (_jobMs[j] < TimingConfig.TickMs) _jobMs[j] = TimingConfig.TickMs;
            }
        }

        /// <summary>
        /// The party with the least patience left that has not yet had an
        /// intervention. -1 if there is none.
        ///
        /// The "not yet had one" condition matters: intervening at the same
        /// table over and over would mean spending the day's allowance on one
        /// table.
        /// </summary>
        public int MostImpatientParty()
        {
            int best = -1;
            int least = int.MaxValue;
            for (int i = 0; i < MaxParties; i++)
            {
                if (!_pActive[i] || _pBonusCenti[i] != 0) continue;
                if (_pStage[i] == CustomerStage.Eating) continue;
                if (_pPatienceLeftMs[i] >= least) continue;
                least = _pPatienceLeftMs[i];
                best = i;
            }
            return best;
        }

        /// <summary>
        /// Hurries a station along: a share is wiped off the remaining wall
        /// clock of every job cooking at that station.
        ///
        /// The owner DOES NOT COOK (docs/14); they clear the path. So what
        /// shortens is the job's remaining time, not the time the cook is
        /// tied up.
        /// </summary>
        private void RushStation(int station)
        {
            if (station < 0 || station >= _stationBusy.Length)
            {
                Emit(SimEventKind.CommandRejected, (int)CommandKind.Intervene, 6);
                return;
            }
            if (_interventionsLeft <= 0)
            {
                Emit(SimEventKind.CommandRejected, (int)CommandKind.Intervene, 8);
                return;
            }

            int touched = 0;
            for (int j = 0; j < _jobStation.Length; j++)
            {
                if (_jobState[j] != 1 || _jobStation[j] != station) continue;
                int cut = (int)Fx.MulDiv(_jobMs[j], _economy.RushCutBp, Fx.One);
                _jobMs[j] -= cut;
                if (_jobMs[j] < TimingConfig.TickMs) _jobMs[j] = TimingConfig.TickMs;
                touched++;
            }

            if (touched == 0)
            {
                // Hurrying an empty station along must not burn an allowance.
                Emit(SimEventKind.CommandRejected, (int)CommandKind.Intervene, 10);
                return;
            }

            _interventionsLeft--;
            Emit(SimEventKind.StationRushed, station, touched);
        }

        /// <summary>
        /// The most congested station: the one with the longest queue. -1 if
        /// none is busy. For picking an intervention target.
        /// </summary>
        public int BusiestStation()
        {
            int best = -1, most = 0;
            for (int st = 0; st < _stationBusy.Length; st++)
            {
                int waiting = 0;
                for (int j = 0; j < _jobStation.Length; j++)
                    if (_jobStation[j] == st && _jobState[j] == 1) waiting++;
                if (waiting > most) { most = waiting; best = st; }
            }
            return best;
        }

        /// <summary>The owner's intervention allowance left today.</summary>
        /// <summary>
        /// Today's intervention allowance. THE CEILING DEPENDS ON THE TABLE
        /// COUNT.
        ///
        /// It was fixed at four, and the mechanic faded out exactly where it
        /// was most needed: in a four-table shop four allowances cover most
        /// of the day's crises, at a fourteen-table weekend peak a small part
        /// of them. So in the game's LATE section the player was making FEWER
        /// decisions than in the early one - growing was not increasing their
        /// agency but dissolving it.
        ///
        /// The floor at four tables is kept (the game's opening must not
        /// change), and one allowance is added per four tables: 4 tables 4,
        /// 8 tables 5, 12 tables 6, 14 tables 6.
        /// </summary>
        public int InterventionsToday
        {
            get
            {
                // The base table count comes FROM THE CONTENT: the first
                // tier. Hard-coding 4 would be silently wrong as soon as the
                // tiers changed.
                int extra = (_tableCount - _economy.TierAt(0).Tables) / 4;
                if (extra < 0) extra = 0;
                return _economy.InterventionsPerDay + extra;
            }
        }

        private int _plannedPeople;

        /// <summary>
        /// The number of people ACTUALLY planned to arrive today.
        ///
        /// ExpectedPeopleToday gives THE EXPECTATION; the difference between
        /// the two is the day's variance. The player cannot see this (and
        /// must not - if they could, the volatility would be decoration
        /// again); it exists for the tests and the tour.
        /// </summary>
        public int PlannedPeopleToday { get { return _plannedPeople; } }

        public int InterventionsLeft { get { return _interventionsLeft; } }

        /// <summary>Has the player called last orders? No new parties arrive.</summary>
        public bool DoorsClosed { get { return _doorsClosed; } }

        private bool _doorsClosed;

        /// <summary>
        /// How many parties currently in the hall have been offered tea.
        ///
        /// It exists to test that the tea goes TO THE HALL: one round of tea
        /// should touch more than one table. Without this number, "it went to
        /// all of them" and "it went to one of them" could not be told apart.
        /// </summary>
        public int PartiesWithTea
        {
            get
            {
                int n = 0;
                for (int i = 0; i < MaxParties; i++)
                    if (_pActive[i] && _pTea[i]) n++;
                return n;
            }
        }

        /// <summary>
        /// The daily intervention allowance. So that the UI's hint can read
        /// the number FROM THE CONTENT: a number typed by hand into the text
        /// starts lying silently the moment the balance tool changes the
        /// value.
        /// </summary>
        public int InterventionsPerDay { get { return _economy.InterventionsPerDay; } }

        private void Expand(int tierIndex)
        {
            if (tierIndex < 0 || tierIndex >= _economy.TierCount)
            {
                Emit(SimEventKind.CommandRejected, (int)CommandKind.Expand, 7);
                return;
            }
            TierConfig t = _economy.TierAt(tierIndex);
            if (t.Tables <= _tableCount || _cash < t.Upgrade)
            {
                Emit(SimEventKind.CommandRejected, (int)CommandKind.Expand, 8);
                return;
            }
            _cash -= t.Upgrade;
            _expansionSpend += t.Upgrade;

            // A GROWING SHOP BRINGS IN NEW PLATES.
            //
            // As many as the difference, and CLEAN. Adding the difference
            // rather than tying the total to the tier is essential: the dirty
            // pile and the plates on the tables are still there, and
            // rewriting the total would destroy them and break the invariant.
            int newPlates = t.Plates - _economy.TierForTables(_tableCount).Plates;
            if (newPlates > 0) _platesClean += newPlates;

            _tableCount = t.Tables;

            // THE REPUTATION BANKED AT THE CEILING IS PAID OUT HERE.
            //
            // An expansion does not only buy tables: the return on the good
            // service given under a low ceiling arrives on that day too. So
            // the days at the ceiling do not pass unrequited, and an
            // expansion reads not as "new tables" but as "the release of a
            // reputation that had been building up".
            if (_reputationOverflowCenti > 0)
            {
                _reputationCenti += _reputationOverflowCenti;
                _reputationOverflowCenti = 0;

                int newCap = _economy.TierForTables(_tableCount).ReputationCapCenti;
                if (newCap <= 0 || newCap > 10000) newCap = 10000;
                if (_reputationCenti > newCap) _reputationCenti = newCap;
            }
        }

        // ====================================================================
        // Tick
        // ====================================================================
        public void Tick()
        {
            if (_phase == DayPhase.Service) TickService();
            _tickIndex++;
        }

        private void TickService()
        {
            RegenerateInterventions();
            SpawnArrivals();
            AdvancePatience();
            SeatWaitingParties();
            DispatchKitchen();
            PlateUp();
            DispatchHall();
            AdvanceTasks();
            AdvanceEating();
            _serviceTick++;
        }

        /// <summary>
        /// THE OWNER'S ATTENTION COMES BACK, IT IS NOT HANDED OUT AT THE DOOR.
        ///
        /// Measured, 24 seeds x 60 days, fast food one waiter short: an eager
        /// player's whole day budget was spent by 0:45 - before the lunch
        /// crest at 2:00 and two crests before the evening one. The rest of
        /// an eight-minute day had nothing in it, which is the complaint this
        /// change answers.
        ///
        /// And spreading beats enlarging, which is the part that is not
        /// obvious: a six-charge day budget spends 5.18 charges and loses 59
        /// parties, a three-charge regenerating pool spends 3.08 and loses
        /// 53. FEWER CHARGES, BETTER DAY.
        ///
        /// The cap is the cost. Hoarding through a quiet stretch throws away
        /// everything that would have regenerated; entering a 2.1x crest
        /// empty is the other way to lose. So "now or at the crest" survives
        /// as a question - measured, it still separates eager from patient
        /// play by 11 parties.
        ///
        /// INTEGER MILLISECONDS OFF THE TICK, never a wall clock: the
        /// campaign has to replay identically from a seed (CLAUDE.md rule 3).
        /// </summary>
        private void RegenerateInterventions()
        {
            if (_interventionsLeft >= InterventionCapToday) return;

            _interventionMs += TimingConfig.TickMs;
            if (_interventionMs < _economy.InterventionRegenMs) return;

            _interventionMs -= _economy.InterventionRegenMs;
            _interventionsLeft++;
            Emit(SimEventKind.InterventionRegained, _interventionsLeft, 0);
        }

        /// <summary>
        /// How many charges may be held at once.
        ///
        /// It grows with the table count for the reason InterventionsToday
        /// used to: "growing was not increasing the player's agency but
        /// dissolving it". The CAP is what grows now rather than a budget -
        /// a bigger purse measured worse than a better-spread one.
        /// </summary>
        public int InterventionCapToday
        {
            get
            {
                int extra = (_tableCount - _economy.TierAt(0).Tables) / 8;
                if (extra < 0) extra = 0;
                return _economy.InterventionCap + extra;
            }
        }

        /// <summary>Milliseconds banked toward the next charge. For the meter.</summary>
        public int InterventionRegenMs { get { return _interventionMs; } }

        /// <summary>The period, so the view can draw a fraction.</summary>
        public int InterventionRegenPeriodMs
        {
            get { return _economy.InterventionRegenMs; }
        }

        private int _interventionMs;

        // ---- 1. arrivals -----------------------------------------------------
        private void SpawnArrivals()
        {
            // THE DOOR CAN BE SHUT WITHOUT THROWING ANYBODY OUT.
            //
            // CloseDay ends the day by sending every seated party away angry,
            // so closing early was never right on any day in either cuisine -
            // measured, the tail after the arrival window carries 27% of a
            // fast food day's revenue. A button that is never right to press
            // is not a decision, it is a hazard with a guard rail on it.
            //
            // Last orders is the decision that button was pretending to be:
            // the arrivals stop, everybody already inside is served, and the
            // day ends when the room empties. Plates gone, one hand short,
            // ninety seconds of arrivals left - give up the revenue, keep the
            // reputation.
            if (_doorsClosed) return;

            while (_arrNext < _arrCount && _arrTick[_arrNext] <= _serviceTick)
            {
                int slot = FindFreeParty();
                if (slot < 0) { _arrNext++; continue; }   // the pool is full, a customer is lost

                int arch = _arrArchetype[_arrNext];
                ArchetypeDef a = _content.Archetypes[arch];

                _pActive[slot] = true;
                _pArchetype[slot] = arch;
                _pSize[slot] = _arrSize[_arrNext];
                _pStage[slot] = CustomerStage.WaitingForTable;
                _pTable[slot] = -1;
                _pPatienceTotalMs[slot] = a.PatienceMs;
                _pPatienceLeftMs[slot] = a.PatienceMs;
                _pWaitedMs[slot] = 0;
                _pEatLeftMs[slot] = 0;
                _pSatisfactionCenti[slot] = 0;
                _pBonusCenti[slot] = 0;
                _pInTask[slot] = false;
                _pKitchenTask[slot] = false;
                _pWarned[slot] = false;
                _pRegular[slot] = _arrRegular[_arrNext];
                _pServer[slot] = -1;
                _pCook[slot] = -1;
                PickOrder(slot, a.PatienceMs);

                // If there is no main dish on the menu that can be made for
                // them, the customer TURNS BACK AT THE DOOR. They do not sit
                // down and wait until their patience runs out: that would be
                // both unrealistic and would charge a stock mistake the
                // reputation penalty of a customer kept waiting forty
                // minutes.
                if (_pDishMain[slot] < 0)
                {
                    _pActive[slot] = false;
                    _stockOutEvents++;
                    _turnedAwayParties++;
                    AccumulateReputation(slot, TurnAwaySatisfactionCenti);
                    Emit(SimEventKind.TurnedAway, slot, arch, _pSize[slot]);
                    _arrNext++;
                    continue;
                }

                _partyCount++;
                Emit(SimEventKind.CustomerArrived, slot, arch, _pSize[slot]);
                _arrNext++;
            }
        }

        private int FindFreeParty()
        {
            for (int i = 0; i < MaxParties; i++)
                if (!_pActive[i]) return i;
            return -1;
        }


        /// <summary>
        /// One person's order: the MAIN dish is certain, the side and the
        /// drink are by chance.
        ///
        /// Why not a single item: on the balance tool's first run the passive
        /// player did not go under while the good player did. The cause was
        /// that the average ticket was far too low; picking a single item
        /// from the menu with equal probability meant most customers took
        /// only a drink. The base of the combo mechanic of docs/07.
        ///
        /// A customer in a hurry does not order a heavy dish: the candidates
        /// are the dishes whose prepMs &lt;= patience x the factor. Without
        /// this, the courier with 8 seconds of patience could never have been
        /// served.
        /// </summary>
        private void PickOrder(int slot, int patienceMs)
        {
            long limit = Fx.MulDiv(patienceMs, _timing.OrderPatienceFactorBp, Fx.One);

            int servings = _pSize[slot];
            _pAskedDish[slot] = AskForMissingDish();

            ArchetypeDef arch = _content.Archetypes[_pArchetype[slot]];

            // The roles come FROM THE CONTENT: ana/yan/icecek on fast food,
            // sulu+izgara / corba+pilav+meze / icecek in the Turkish
            // restaurant.
            _pDishMain[slot] = PickFromRole(_content.MainGroups, limit, true, servings, arch);

            // A regular asks for the dish THEY FAVOUR. Not on the menu means
            // disappointment: it was the one thing they came for and it was
            // not there.
            _pMissedFavourite[slot] = false;
            int reg = _pRegular[slot];
            if (reg >= 0 && reg < RegularCount)
            {
                int fav = _content.Regulars[reg].FavouriteDish;
                if (_dishOnMenu[fav] && Unlocked(fav) && CanMake(fav, servings))
                    _pDishMain[slot] = fav;
                else if (_pDishMain[slot] >= 0)
                    _pMissedFavourite[slot] = true;
            }
            _pDishSide[slot] = _rngOrder.Chance(_economy.SideChanceBp)
                ? PickFromRole(_content.SideGroups, limit, false, servings, arch) : -1;
            _pDishDrink[slot] = _rngOrder.Chance(_economy.DrinkChanceBp)
                ? PickFromRole(_content.DrinkGroups, limit, false, servings, arch) : -1;
            _pDishDessert[slot] = _rngOrder.Chance(ExtrasChanceBp(arch, _economy.DessertChanceBp))
                ? PickFromRole(_content.DessertGroups, limit, false, servings, arch) : -1;

            // The customer who asks for a tab. The decision will be taken at
            // the moment of payment, but WHO asks is rolled here, with the
            // same determinism as the arrival plan - we do not call for
            // randomness during service.
            _pAsksCredit[slot] = HasCredit
                && CreditIdentityOk(slot)
                && _rngCredit.Chance(_content.Signature.CreditAskChanceBp);

            // The combo: WHICHEVER main dish it is, the side and the drink
            // come with it FOR CERTAIN. The ticket grows (a certainty rather
            // than a chance) and so does the kitchen load - docs/07: "a
            // well-built combo raises the average ticket BUT increases the
            // kitchen load."
            //
            // It used to be tied to ONE SINGLE main dish (the combo's own
            // main, the hamburger). Measured: it fired on only 6% of orders,
            // because five main dishes stay open on the menu and a regular's
            // favourite overrides the main too. Its contribution to the
            // average ticket was +0.7 coins, that is 1.3% - whereas docs/12
            // promises 44%. Tying it to the group makes the signature
            // mechanic genuinely felt.
            _pCombo[slot] = false;
            // THE DENOMINATOR ONLY COUNTS WHILE THE MECHANIC IS OPEN.
            //
            // It counted unconditionally, whereas the combo opens on day 16:
            // the denominator also took in the fifteen days on which the
            // numerator was STRUCTURALLY ZERO, and the axis understated real
            // usage by about a third. The measure should say what it is:
            // "what share of the orders that COULD have become a combo did".
            if (HasCombo && IsMain(_pDishMain[slot])) _mainOrders++;
            if (ComboSellable() && IsMain(_pDishMain[slot]))
            {
                _comboOrders++;
                int[] cd = _content.Signature.ComboDishes;
                _pDishSide[slot] = cd[1];
                _pDishDrink[slot] = cd[2];
                _pCombo[slot] = true;
                Emit(SimEventKind.ComboOrdered, slot, (int)ComboPrice(slot));
            }

            // If no main dish could be found, the caller (SpawnArrivals)
            // turns the party back at the door; here we only leave a -1.
        }

        /// <summary>
        /// Does the customer ask for a dish they HAVE HEARD OF but which
        /// cannot be made.
        ///
        /// A candidate: a main dish whose day and reputation have come but
        /// whose equipment has not been bought. That is, a shortfall IN THE
        /// PLAYER'S HANDS; a dish the calendar has not yet brought is never
        /// asked for, as that would be unfair.
        /// </summary>
        private int AskForMissingDish()
        {
            int n = 0, total = 0;
            for (int i = 0; i < _content.Dishes.Length; i++)
            {
                if (!UnlockedMain(i)) continue;
                total++;
                if (Awaited(i)) n++;
            }
            if (n == 0 || total == 0) return -1;

            // The chance of being asked grows with the PROPORTION missing,
            // not with the count.
            //
            // It used to be an absolute count and it saturated at four: "four
            // dishes missing" and "twelve dishes missing" took the same
            // penalty. As long as the shortfall was limited to a few dishes'
            // worth of equipment that was no problem, but once dishes taken
            // off the menu started counting too, the penalty hit the ceiling
            // FOR EVERYBODY: a player who narrowed their menu sensibly took
            // the same penalty as one keeping a single dish. Measured - the
            // single-dish strategy fell from 27,361 to 116 and the shop
            // emptied on the ninth day. A narrow menu went from "free" to
            // "fatal"; both are wrong.
            //
            // A proportion is fair: if half the open main dishes are off the
            // menu the penalty is half, and if none of them is on it, full.
            // Menu breadth becomes a CONTINUOUS axis rather than a trap with
            // a threshold.
            int chance = (int)Fx.MulDiv(_economy.AskChanceBp * 4, n, total);
            if (chance > Fx.One) chance = Fx.One;
            if (!_rngOrder.Chance(chance)) return -1;

            int pick = _rngOrder.NextInt(n);
            for (int i = 0; i < _content.Dishes.Length; i++)
            {
                if (!Awaited(i)) continue;
                if (pick == 0) { Emit(SimEventKind.DishRequested, i); return i; }
                pick--;
            }
            return -1;
        }

        /// <summary>
        /// A main dish whose day and reputation have come but which CANNOT BE
        /// MADE TODAY. The thing the customer asks for and does not find.
        ///
        /// There are two reasons and FROM THE CUSTOMER'S POINT OF VIEW THEY
        /// ARE THE SAME:
        ///   1. the equipment has not been bought - a shortfall the player
        ///      can close
        ///   2. it is not on the menu - a decision the player took TODAY
        ///
        /// The second one WAS ABSENT for a long time, and it was the game's
        /// deepest balance error: a dish taken off the menu was never "asked
        /// for", so narrowing the menu had ZERO cost on the demand side.
        /// Measured - a player keeping a single main dish on the menu beat
        /// the reasonable player by 12% on fast food and by 38% on Turkish
        /// cuisine. A narrow menu was strictly the dominant strategy.
        ///
        /// The consequence was this: the cold store's second reward (being
        /// able to carry menu breadth) was worthless, so the ladder's upper
        /// tiers were never bought; and the reason the thirty-two-dish
        /// content inventory existed vanished. docs/32 200 says "the cold
        /// store makes you buy menu breadth" - now it really does.
        ///
        /// A dish the calendar has not yet brought is never asked for; that
        /// would be unfair.
        /// </summary>
        /// <summary>
        /// A MAIN dish whose calendar and reputation have come. Awaited's
        /// denominator: the answer to "how many dishes could there have
        /// been".
        /// </summary>
        private bool UnlockedMain(int dish)
        {
            DishDef d = _content.Dishes[dish];
            if (d.UnlockDay > _day) return false;
            if (_reputationCenti < d.UnlockReputationCenti) return false;
            return _content.IsInRole(d.Group, _content.MainGroups);
        }

        private bool Awaited(int dish)
        {
            DishDef d = _content.Dishes[dish];
            if (d.UnlockDay > _day) return false;
            if (_reputationCenti < d.UnlockReputationCenti) return false;
            if (!_content.IsInRole(d.Group, _content.MainGroups)) return false;

            // The equipment is missing: a shortfall the player can close.
            if (d.RequiresStationTier > 0
                && _stationTier[d.StationIndex] < d.RequiresStationTier)
                return true;

            // The equipment is there but it is not on the menu: today's
            // decision.
            return !_dishOnMenu[dish];
        }

        /// <summary>
        /// Picks with equal probability from the dishes in the group that fit
        /// within the patience. If none fits: the fastest in the group for a
        /// required role, otherwise -1.
        /// </summary>
        /// <summary>
        /// The chance of EXTRA items such as a dessert varies by archetype.
        /// The tipping tendency is a proxy for willingness to spend: whoever
        /// leaves a big tip takes a dessert too, whoever leaves none does not.
        ///
        /// docs/13 had designed an orderPreference per archetype. Rather than
        /// writing twenty-four weight tables by hand, it is derived from the
        /// character fields ALREADY LOADED; so there is no invented number
        /// and TipChanceBp and PriceSensitivityBp do double duty.
        /// </summary>
        private static int ExtrasChanceBp(ArchetypeDef a, int baseBp)
        {
            // A tip of 0 -> half, 3200 -> double.
            int scale = Fx.One / 2 + a.TipChanceBp * 3;
            if (scale > 2 * Fx.One) scale = 2 * Fx.One;
            return (int)Fx.MulDiv(baseBp, scale, Fx.One);
        }

        /// <summary>
        /// Picks a dish from a role. The choice IS NOT EQUALLY PROBABLE: the
        /// archetype's price sensitivity leans towards the cheap or the dear
        /// end.
        ///
        /// A sensitivity of 10000 is neutral. The haggler at 25000 leans
        /// towards the cheapest, the inspector at 5000 towards the dearest.
        /// This field was already loaded and was used only in the
        /// satisfaction penalty; it played no part at all in the CHOICE of
        /// dish, so every customer ordered from the same distribution.
        /// </summary>
        private int PickFromRole(string[] role, long limit, bool required, int servings,
                                 ArchetypeDef arch)
        {
            int fastest = -1;
            int fastestMs = int.MaxValue;
            long lo = long.MaxValue, hi = long.MinValue;
            int n = 0;

            for (int i = 0; i < _content.Dishes.Length; i++)
            {
                DishDef d = _content.Dishes[i];
                if (!_dishOnMenu[i] || !Unlocked(i)) continue;
                if (!_content.IsInRole(d.Group, role)) continue;
                if (!CanMake(i, servings)) continue;      // not in stock, it cannot be ordered

                if (d.PrepMs < fastestMs) { fastestMs = d.PrepMs; fastest = i; }
                if (d.PrepMs > limit) continue;
                n++;
                if (_dishPrice[i] < lo) lo = _dishPrice[i];
                if (_dishPrice[i] > hi) hi = _dishPrice[i];
            }

            if (n == 0) return required ? fastest : -1;

            // The weights. Equal if there is a single candidate, or if they
            // are all the same price.
            int lean = arch != null ? arch.PriceSensitivityBp - Fx.One : 0;
            long total = 0;
            for (int i = 0; i < _content.Dishes.Length; i++)
            {
                int w = RoleWeight(i, role, limit, servings, lo, hi, lean);
                if (w > 0) total += w;
            }
            if (total <= 0) return fastest;

            long pick = _rngOrder.NextInt((int)(total > int.MaxValue ? int.MaxValue : total));
            for (int i = 0; i < _content.Dishes.Length; i++)
            {
                int w = RoleWeight(i, role, limit, servings, lo, hi, lean);
                if (w <= 0) continue;
                if (pick < w) return i;
                pick -= w;
            }
            return fastest;
        }

        /// <summary>The candidate's weight; zero if it is not a candidate.</summary>
        private int RoleWeight(int dish, string[] role, long limit, int servings,
                               long lo, long hi, int lean)
        {
            DishDef d = _content.Dishes[dish];
            if (!_dishOnMenu[dish] || !Unlocked(dish)) return 0;
            if (!_content.IsInRole(d.Group, role)) return 0;
            if (!CanMake(dish, servings)) return 0;
            if (d.PrepMs > limit) return 0;
            if (hi <= lo || lean == 0) return Fx.One;

            // 0 = the cheapest, 10000 = the dearest
            int rel = (int)Fx.MulDiv(_dishPrice[dish] - lo, Fx.One, hi - lo);
            int w = Fx.One + (int)Fx.MulDiv(Fx.One - 2 * rel, lean, Fx.One);
            return w < 1000 ? 1000 : w;      // no dish is shut out completely
        }

        /// <summary>The total cooking time of one person's order.</summary>
        private int OrderPrepMs(int party)
        {
            int ms = 0;
            if (_pDishMain[party] >= 0) ms += _content.Dishes[_pDishMain[party]].PrepMs;
            if (_pDishSide[party] >= 0) ms += _content.Dishes[_pDishSide[party]].PrepMs;
            if (_pDishDrink[party] >= 0) ms += _content.Dishes[_pDishDrink[party]].PrepMs;
            if (_pDishDessert[party] >= 0) ms += _content.Dishes[_pDishDessert[party]].PrepMs;
            return ms;
        }

        /// <summary>The value of one person's order, at the prices the player set.</summary>
        private long OrderPrice(int party)
        {
            long p = 0;
            if (_pCombo[party])
            {
                // Three items at one price; a dessert, if there is one, goes
                // on top at full price.
                p = ComboPrice(party);
                if (_pDishDessert[party] >= 0) p += _dishPrice[_pDishDessert[party]];
                return p;
            }
            if (_pDishMain[party] >= 0) p += _dishPrice[_pDishMain[party]];
            if (_pDishSide[party] >= 0) p += _dishPrice[_pDishSide[party]];
            if (_pDishDrink[party] >= 0) p += _dishPrice[_pDishDrink[party]];
            if (_pDishDessert[party] >= 0) p += _dishPrice[_pDishDessert[party]];
            return p;
        }

        /// <summary>
        /// The combo's price per head: the CHOSEN main dish + the combo's
        /// side and drink, all discounted by priceBp.
        ///
        /// The main dish is no longer fixed (see PickOrder): the combo is
        /// built on whichever main the player keeps on the menu. That is why
        /// the price is worked out from the chosen dish - using a fixed
        /// dish's price would mean selling an expensive main cheaply.
        /// </summary>
        public long ComboPrice(int party)
        {
            int[] d = _content.Signature.ComboDishes;
            if (d == null) return 0;

            int main = party >= 0 && party < MaxParties && _pDishMain[party] >= 0
                ? _pDishMain[party] : d[0];

            long sum = _dishPrice[main] + _dishPrice[d[1]] + _dishPrice[d[2]];
            return Fx.MulDiv(sum, _content.Signature.ComboPriceBp, Fx.One);
        }

        /// <summary>For the UI: the combo's sample price (with the combo's own main).</summary>
        public long ComboPrice() { return ComboPrice(-1); }

        /// <summary>The ingredient cost of one person's order.</summary>
        private long OrderCost(int party)
        {
            long c = 0;
            if (_pDishMain[party] >= 0) c += _content.Dishes[_pDishMain[party]].IngredientCost;
            if (_pDishSide[party] >= 0) c += _content.Dishes[_pDishSide[party]].IngredientCost;
            if (_pDishDrink[party] >= 0) c += _content.Dishes[_pDishDrink[party]].IngredientCost;
            if (_pDishDessert[party] >= 0)
                c += _content.Dishes[_pDishDessert[party]].IngredientCost;
            return c;
        }

        // ---- 2. patience -----------------------------------------------------
        private void AdvancePatience()
        {
            for (int i = 0; i < MaxParties; i++)
            {
                if (!_pActive[i]) continue;

                int rateBp = DrainRateBp(i);
                if (rateBp <= 0) continue;

                int drained = (int)Fx.MulDiv(TimingConfig.TickMs, rateBp, Fx.One);
                _pPatienceLeftMs[i] -= drained;

                // THE WAITING IS COUNTED WITH THE SAME WEIGHTING.
                //
                // Every tick used to count IN FULL, and that was harmless as
                // long as patience was frozen. It came out once patience ran
                // during cooking too: a minute of cooking saturated the
                // satisfaction penalty (waited/patience x 6000) and every
                // customer left with zero satisfaction - all the reference
                // bots went bankrupt.
                //
                // The right answer is the same measure: waiting counts in
                // proportion to how much it IRKS the customer. Waiting for a
                // table counts in full, waiting for food counts little - both
                // in the same unit.
                _pWaitedMs[i] += drained;

                if (!_pWarned[i] && _pPatienceTotalMs[i] > 0)
                {
                    int leftBp = (int)Fx.MulDiv(_pPatienceLeftMs[i], Fx.One, _pPatienceTotalMs[i]);
                    if (leftBp <= _timing.PatienceWarnBp)
                    {
                        _pWarned[i] = true;
                        Emit(SimEventKind.PatienceWarning, i, leftBp);
                    }
                }

                if (_pPatienceLeftMs[i] <= 0) LeaveAngry(i);
            }
        }

        /// <summary>
        /// The rate at which patience drains. While a waiter is at the table
        /// (_pInTask) it does not drain at all: a customer being seen to does
        /// not count as waiting.
        /// </summary>
        private int DrainRateBp(int i)
        {
            if (_pInTask[i]) return 0;
            switch (_pStage[i])
            {
                case CustomerStage.WaitingForTable: return _timing.DrainWaitingTableBp;
                case CustomerStage.WaitingToOrder: return _timing.DrainWaitingOrderBp;
                case CustomerStage.WaitingForFood: return _timing.DrainWaitingFoodBp;
                case CustomerStage.WaitingToPay: return _timing.DrainWaitingPayBp;
                default: return 0;
            }
        }

        private void LeaveAngry(int i)
        {
            // A customer whose patience runs out leaves and drops reputation
            // sharply.
            int stage = (int)_pStage[i];
            _pSatisfactionCenti[i] = 0;
            AccumulateReputation(i, 0);
            _angryParties++;
            if (_pTable[i] >= 0) _angrySeated++;

            FreeTableOf(i, dirty: _pTable[i] >= 0);
            CancelTasksFor(i);
            _pStage[i] = CustomerStage.LeftAngry;
            _pActive[i] = false;
            _partyCount--;

            Emit(SimEventKind.CustomerLeftAngry, i, stage, _pWaitedMs[i]);
        }

        private void FreeTableOf(int party, bool dirty)
        {
            int t = _pTable[party];
            if (t < 0) return;
            _tableParty[t] = -1;
            _tableDirty[t] = dirty;

            // THE PLATES STAY ON THE TABLE. Until a waiter clears it they
            // count as "in use" - they only go to the sink once it is
            // cleared. A party that leaves without eating has no plates, and
            // zero is added here.
            _tablePlates[t] += _pPlates[party];
            _pPlates[party] = 0;

            _pTable[party] = -1;
        }

        private void CancelTasksFor(int party)
        {
            for (int s = 0; s < MaxServers; s++)
            {
                if (_hallTaskKind[s] != TaskKind.None && _hallTaskKind[s] != TaskKind.Clear
                    && _hallTaskTarget[s] == party)
                    _hallTaskKind[s] = TaskKind.None;
                if (_kitchenTaskKind[s] != TaskKind.None
                    && _kitchenTaskTarget[s] / MaxJobsPerParty == party)
                    _kitchenTaskKind[s] = TaskKind.None;
            }
            ReleaseJobs(party);
            _pInTask[party] = false;
            _pKitchenTask[party] = false;
        }

        /// <summary>
        /// Cancels the party's station jobs and FREES THE SLOTS.
        /// Without this, every party that left in anger locked a slot for
        /// good and the kitchen silently ground to a halt as the day went on.
        /// </summary>
        private void ReleaseJobs(int party)
        {
            int b = party * MaxJobsPerParty;
            for (int k = 0; k < MaxJobsPerParty; k++)
            {
                if (_jobState[b + k] == 1) _stationBusy[_jobStation[b + k]] -= _jobSlots[b + k];
                _jobStation[b + k] = -1;
                _jobMs[b + k] = 0;
                _jobPlates[b + k] = 0;
                _jobSlots[b + k] = 0;
                _jobState[b + k] = 0;
            }
            _pJobsLeft[party] = 0;
        }

        // ---- 3. seating ------------------------------------------------------
        private void SeatWaitingParties()
        {
            for (int t = 0; t < _tableCount; t++)
            {
                if (_tableParty[t] >= 0 || _tableDirty[t]) continue;

                int best = MostUrgent(CustomerStage.WaitingForTable);
                if (best < 0) return;

                // WITH NO CLEAN PLATE, NOBODY IS SEATED.
                //
                // This line prevents a DEATH SPIRAL, and the spiral was seen
                // on the automated tour: in a four-table shop with twelve
                // plates, two parties of four used up the supply; the third
                // party sat down, ordered, the kitchen could find no plate,
                // their patience ran out, they left angry - and so the day
                // closed with nobody served at all.
                //
                // This is the right behaviour in a real restaurant too: with
                // no plate you DO NOT SEAT anybody, you do not seat them and
                // leave them hungry. The waiting party waits at the door and
                // comes in when a clean plate appears - the pressure is
                // visible and the shop does not lock up.
                //
                // Not a reservation but a THRESHOLD: plates are deducted at
                // the moment of plating. Holding them here would mean a party
                // that has not ordered keeping plates in hand.
                if (_platesClean < _pSize[best]) return;

                _tableParty[t] = best;
                _pTable[best] = t;
                _pStage[best] = CustomerStage.WaitingToOrder;
                Emit(SimEventKind.CustomerSeated, best, t);
            }
        }

        /// <summary>
        /// The party at the given stage, not tied to a task, with the LEAST
        /// patience left. On a tie the lower index wins: the rule is
        /// deterministic. The rule "see to whoever is closest to walking out
        /// first" means the right priority does not have to be coded
        /// separately.
        /// </summary>
        private int MostUrgent(CustomerStage stage)
        {
            int best = -1;
            int bestPatience = int.MaxValue;
            for (int i = 0; i < MaxParties; i++)
            {
                if (!_pActive[i] || _pStage[i] != stage) continue;
                if (_pInTask[i] || _pKitchenTask[i]) continue;
                if (_pPatienceLeftMs[i] < bestPatience)
                {
                    bestPatience = _pPatienceLeftMs[i];
                    best = i;
                }
            }
            return best;
        }

        // ---- 4. task dispatch ------------------------------------------------
        /// <summary>
        /// A party waiting to be cooked for. _pEatLeftMs == 0 means "cooking",
        /// -1 means "cooked". Without that distinction the kitchen started
        /// cooking again food that was ready and waiting to be served.
        /// </summary>
        /// <summary>
        /// Splits the order into STATION JOBS. Items going to the same station
        /// merge into one job. A job keeps the duration of ONE PLATE and the
        /// NUMBER OF PLATES separately; how many plates cook at once is
        /// decided by the free slots when the job starts (docs/27 3.3).
        ///
        /// In the first draft all of a party's plates cooked one after
        /// another in a SINGLE slot. In that model buying equipment changed
        /// nothing, because the slot count did not affect the duration.
        /// </summary>
        private void BuildJobs(int party)
        {
            int b = party * MaxJobsPerParty;
            for (int k = 0; k < MaxJobsPerParty; k++)
            {
                _jobStation[b + k] = -1;
                _jobMs[b + k] = 0;
                _jobPlates[b + k] = 0;
                _jobSlots[b + k] = 0;
                _jobState[b + k] = 0;
            }
            _pJobsLeft[party] = 0;

            AddJob(party, _pDishMain[party]);
            AddJob(party, _pDishSide[party]);
            AddJob(party, _pDishDrink[party]);
            AddJob(party, _pDishDessert[party]);
        }

        private void AddJob(int party, int dish)
        {
            if (dish < 0) return;
            int station = _content.Dishes[dish].StationIndex;
            int b = party * MaxJobsPerParty;

            for (int k = 0; k < MaxJobsPerParty; k++)
            {
                // A second item going to the same station: the single-plate
                // duration adds up, the plate count stays the same.
                if (_jobStation[b + k] == station)
                {
                    _jobMs[b + k] += _content.Dishes[dish].PrepMs;
                    return;
                }
                if (_jobStation[b + k] < 0)
                {
                    _jobStation[b + k] = station;
                    _jobMs[b + k] = _content.Dishes[dish].PrepMs;
                    _jobPlates[b + k] = _pSize[party];
                    _jobState[b + k] = 0;
                    _pJobsLeft[party]++;
                    return;
                }
            }
        }

        /// <summary>The station's slot count, per its equipment tier.</summary>
        private int StationSlots(int station)
        {
            return _content.Stations[station].Tiers[_stationTier[station]].Slots;
        }

        /// <summary>The station's attendBp value, per its equipment tier.</summary>
        private int StationAttendBp(int station)
        {
            return _content.Stations[station].Tiers[_stationTier[station]].AttendBp;
        }

        /// <summary>
        /// The most urgent of the jobs that can be started. The party with the
        /// least patience left comes first; the same party may have more than
        /// one job, which is why it looks even while the party is in a task.
        /// </summary>
        private int MostUrgentJob()
        {
            int best = -1;
            int bestPatience = int.MaxValue;
            for (int i = 0; i < MaxParties; i++)
            {
                if (!_pActive[i]) continue;
                if (_pStage[i] != CustomerStage.WaitingForFood) continue;
                if (_pEatLeftMs[i] != 0) continue;      // 0 = not yet cooked
                if (_pPatienceLeftMs[i] >= bestPatience) continue;

                int b = i * MaxJobsPerParty;
                for (int k = 0; k < MaxJobsPerParty; k++)
                {
                    if (_jobStation[b + k] < 0 || _jobState[b + k] != 0) continue;
                    if (_stationBusy[_jobStation[b + k]] >= StationSlots(_jobStation[b + k]))
                        continue;
                    bestPatience = _pPatienceLeftMs[i];
                    best = b + k;
                    break;
                }
            }
            return best;
        }

        private void DispatchKitchen()
        {
            for (int s = 0; s < _cooks && s < MaxServers; s++)
            {
                if (_kitchenTaskKind[s] != TaskKind.None) continue;

                int job = MostUrgentJob();
                if (job < 0) return;

                int station = _jobStation[job];
                int plates = _jobPlates[job];
                int attendBp = StationAttendBp(station);
                int free = StationSlots(station) - _stationBusy[station];

                // How many plates cook at once. Three limits at once:
                //   - the number of free slots
                //   - the party's plate count
                //   - the number of plates ONE COOK can see to at a time
                //
                // The third is essential. Without it a party of four held all
                // four slots at once while the cook saw to them one after
                // another, so the slots sat there looking occupied for
                // nothing. A measurement caught it: buying equipment was
                // DROPPING kitchen satisfaction from 87 to 81.
                //
                // docs/27 3.3 gives the same number: at the tier 4 peak, 8.66
                // concurrent plates with 4 cooks, that is 2.2 per cook.
                int perCook = Fx.CeilDiv(Fx.One, attendBp);
                int take = plates;
                if (free < take) take = free;
                if (perCook < take) take = perCook;
                if (take < 1) take = 1;
                int rounds = Fx.CeilDiv(plates, take);

                // The cook's hands-on effort is per plate; parallelism does
                // not reduce it. Experience works here: AN EXPERIENCED COOK IS
                // FREED SOONER, the food does not cook faster. wallMs is
                // worked out separately below and is left untouched.
                int attendMs = (int)Fx.MulDiv((long)_jobMs[job] * plates,
                                              attendBp, Fx.One);
                attendMs = XpAdjusted(0, s, attendMs);

                // The combo's price is paid IN THE KITCHEN: the same plate
                // ties the cook up for longer. The discount is in the hall,
                // the load in the kitchen - that is the signature mechanic's
                // trade-off (docs/07).
                if (_pCombo[job / MaxJobsPerParty] && _content.Signature.ComboKitchenLoadBp > 0)
                    attendMs = (int)Fx.MulDiv(attendMs,
                                              _content.Signature.ComboKitchenLoadBp, Fx.One);

                if (attendMs < TimingConfig.TickMs) attendMs = TimingConfig.TickMs;

                int wallMs = _jobMs[job] * rounds;
                if (wallMs < attendMs) wallMs = attendMs;

                _jobState[job] = 1;
                _jobSlots[job] = take;
                _jobMs[job] = wallMs;
                _stationBusy[station] += take;

                _kitchenTaskKind[s] = TaskKind.Cook;
                _kitchenTaskTarget[s] = job;
                _pCook[job / MaxJobsPerParty] = s;
                _kitchenTaskLeftMs[s] = attendMs;
                _pKitchenTask[job / MaxJobsPerParty] = true;
            }
        }

        private void DispatchHall()
        {
            ClampDishwashers();

            int servers = _hall + 1;   // the owner works the hall too
            if (servers > MaxServers) servers = MaxServers;

            // THOSE DEDICATED TO THE SINK: counted FROM THE END of the hall
            // array. Number zero is the owner, and the owner does not go to
            // the sink.
            int sinkFrom = servers - _dishwashers;
            if (sinkFrom < 1) sinkFrom = 1;

            // HOW MANY WENT TO THE SINK FOR THE CRISIS ON THIS TICK.
            int crisisWashers = 0;

            for (int s = 0; s < servers; s++)
            {
                if (_hallTaskKind[s] != TaskKind.None) continue;

                // THE SINK IS NOT THE OWNER'S JOB - as long as there are
                // staff. It is worked out in one place: two copies, one of
                // them unjustified, would drift apart without whoever changed
                // the other one knowing.
                bool owner = s == 0 && _hall > 0;

                if (s >= sinkFrom)
                {
                    // THE DEDICATED DISHWASHER: they only wash, they do not
                    // go to the tables. The user's own words: "once you take
                    // on a dishwasher everybody does their own job" - this
                    // line is exactly that.
                    //
                    // AN ATTEMPT WAS MADE TWICE TO WEAKEN THIS LINE, AND
                    // MEASUREMENT REJECTED BOTH (docs/53):
                    //
                    //   let them go back to the hall when there is nothing to
                    //   wash            197 -> 216   (32 seeds, against HEAD)
                    //   do not let them wash until the pile passes a threshold
                    //                   229 -> 293   (12 seeds - NOT ON THE
                    //                   SAME SCALE, it only shows the
                    //                   direction)
                    //
                    // Both sounded reasonable and both break the same thing:
                    // the specialist's whole value is in washing WITHOUT
                    // BREAK and AT ONCE. A dishwasher who goes back to the
                    // hall is tied to a customer's job when a plate gets
                    // dirty, and gets back to the sink LATE.
                    if (_platesDirty > 0)
                    {
                        _hallTaskKind[s] = TaskKind.Wash;
                        _hallTaskTarget[s] = -1;
                        // THE SPECIALIST'S TIME: a dedicated dishwasher
                        // washes faster. The difference was already written
                        // in the role table (dishwasher 48 / waiter 26 daily
                        // capacity) and the simulation was making no use of
                        // it.
                        _hallTaskLeftMs[s] =
                            XpAdjusted(1, s - 1, _timing.DishwasherWashMs);
                    }
                    continue;
                }

                // ============================================================
                // THE CRISIS: THE KITCHEN HAS STOPPED. This branch comes
                // BEFORE the customer work.
                //
                // Why the exception is legitimate: when the clean plates run
                // out the plating cycle stops COMPLETELY (see
                // `_plateStalled`), so cooked food sits on the pass. At that
                // moment a waiter taking a new order is worthless work -
                // nothing is coming out to be served anyway. Going to the
                // sink OPENS the shop.
                //
                // WHY THE PREVIOUS ATTEMPT DID NOT WORK (docs/49 §6, 351 ->
                // 351): the exception had been written AFTER the customer
                // work and it looked for a "free person"; at the peak the
                // hall is already full, so it never fired. Looking for a free
                // person was the wrong question - the right question is "is
                // the work being done right now worth anything".
                //
                // IT FIRES EVEN WITH A DISHWASHER ON - AND DELIBERATELY.
                //
                // My first comment said "with a dishwasher the crisis does not
                // arise anyway", and that was an UNMEASURED claim;
                // measurement says otherwise (the dishwasher arm moves 198 ->
                // 197 against HEAD, so the branch does fire). A single
                // dishwasher may not keep up with a large enough hall.
                //
                // The user's rule is about ROUTINE washing, and it stands
                // there untouched: `WashNeeded` gives the hall no routine
                // washing when there is a dishwasher. This is not routine -
                // the kitchen HAS STOPPED. In a stopped kitchen the
                // dishwasher is already behind; keeping the hall away from
                // the sink at that moment would not be upholding the rule but
                // locking the shop up.
                //
                // Research (docs/53): in no shipped game does taking on a
                // specialist silently shut the general pool off. RimWorld's
                // FIRE behaviour is exactly this pattern - a rare, severe,
                // all-hands condition overrides the normal priority.
                if (!owner && _plateStalled && _platesDirty > crisisWashers)
                {
                    _hallTaskKind[s] = TaskKind.Wash;
                    _hallTaskTarget[s] = -1;
                    _hallTaskLeftMs[s] =
                        OwnerAdjusted(s, XpAdjusted(1, s - 1, _timing.WashMs));
                    _washing = true;

                    // A SEPARATE COUNTER. `_hallRushWashes` measures whether
                    // the dishwasher saves the hall from the sink
                    // (PlateTests), and this branch fires INDEPENDENTLY OF
                    // THE DISHWASHER - folded into the same counter, both
                    // arms of the test would count the same crisis and the
                    // difference would become unmeasurable. Exactly what that
                    // counter's own comment forbids.
                    _hallCrisisWashes++;

                    // ONE DIRTY PLATE MUST NOT PULL IN THE WHOLE HALL.
                    //
                    // `_platesDirty` drops when the task FINISHES, not when it
                    // is assigned. Without a threshold, five servers went to
                    // the sink at once for a single dirty plate and four came
                    // back empty (inflating `_washedToday` as they went).
                    crisisWashers++;
                    continue;
                }

                // The work that touches a customer first, starting with
                // whoever has the least patience left.
                int party = MostUrgentHall(out TaskKind kind, out int ms);
                if (party >= 0)
                {
                    _hallTaskKind[s] = kind;
                    _hallTaskTarget[s] = party;
                    _pServer[party] = s;
                    _hallTaskLeftMs[s] = OwnerAdjusted(s, XpAdjusted(1, s - 1, ms));
                    _pInTask[party] = true;
                    continue;
                }

                // WHEN THE WASHING-UP PILES UP, THE HALL MOVES TO THE SINK.
                //
                // The user's own words: "when the washing-up piles up too
                // much, let the waiter go and wash it". AFTER the work that
                // touches a customer, BEFORE clearing tables: a pile of
                // washing-up is more urgent than a dirty table, because when
                // the clean plates run out THE KITCHEN stops.
                //
                // Two thresholds (hysteresis) are essential: with a single
                // threshold the waiter washes one plate, goes back to
                // service, and returns on the next frame - it read not as
                // "washing" but as "going back and forth".
                if (!owner && WashNeeded())
                {
                    _hallTaskKind[s] = TaskKind.Wash;
                    _hallTaskTarget[s] = -1;
                    _hallTaskLeftMs[s] = OwnerAdjusted(s, XpAdjusted(1, s - 1, _timing.WashMs));
                    _washing = true;

                    // THIS LINE IS FOR MEASUREMENT.
                    //
                    // The hall has TWO routes to the washing-up: this one
                    // (urgent - DROPPING its work and running to the sink)
                    // and the idle-time branch below. The dishwasher's whole
                    // value lies in preventing the first; the second is
                    // harmless anyway. Folded into a single counter, the
                    // dishwasher's difference cannot be measured - that is
                    // exactly why the "did the dishwasher cut the waiting"
                    // test was comparing zero with zero.
                    _hallRushWashes++;
                    continue;
                }

                // If nobody is waiting, clear a dirty table.
                int table = FirstDirtyTable();
                if (table < 0)
                {
                    // With no table either, the idle time goes on the
                    // washing-up: a real waiter does the same, and it is what
                    // lets the shop go into the peak with clean plates.
                    if (!owner && _platesDirty > 0)
                    {
                        _hallTaskKind[s] = TaskKind.Wash;
                        _hallTaskTarget[s] = -1;
                        _hallTaskLeftMs[s] = OwnerAdjusted(s, XpAdjusted(1, s - 1, _timing.WashMs));
                        _washing = true;
                        continue;
                    }
                    return;
                }
                // THE FLAG DOES NOT DROP HERE, IT DROPS WHEN THE TASK
                // FINISHES.
                //
                // It used to drop as the clearing BEGAN, and the table opened
                // to a new customer in that same second: SeatWaitingParties
                // looks only at _tableDirty. The result was that ClearMs
                // constrained capacity not at all - it kept the waiter busy
                // but did not hold the table - and a new party could be seen
                // sitting down at a table still being cleared.
                //
                // FirstDirtyTable is what stops the same table being picked
                // by two waiters.
                _hallTaskKind[s] = TaskKind.Clear;
                _hallTaskTarget[s] = table;
                // The cleanliness trait applies ONLY to clearing tables: the
                // "fast but messy" one is fast while serving and slow while
                // clearing. Spreading the effect across every task would turn
                // the trait into a second speed multiplier and make the
                // messiness meaningless.
                int clearMs = _timing.ClearMs;
                if (s > 0)
                {
                    int clean = TraitSum(1, s - 1, t => t.CleanlinessBp);
                    if (clean != 0)
                    {
                        int bp = Fx.One + clean;
                        if (bp < 2000) bp = 2000;
                        clearMs = (int)Fx.MulDiv(clearMs, Fx.One, bp);
                    }
                }
                _hallTaskLeftMs[s] = OwnerAdjusted(s, XpAdjusted(1, s - 1, clearMs));
            }
        }

        private int MostUrgentHall(out TaskKind kind, out int ms)
        {
            int best = -1;
            int bestPatience = int.MaxValue;
            CustomerStage bestStage = CustomerStage.None;

            for (int i = 0; i < MaxParties; i++)
            {
                if (!_pActive[i] || _pInTask[i] || _pKitchenTask[i]) continue;
                CustomerStage st = _pStage[i];
                bool needsHall = st == CustomerStage.WaitingToOrder
                              || st == CustomerStage.WaitingToPay;
                if (!needsHall) continue;
                if (_pPatienceLeftMs[i] < bestPatience)
                {
                    bestPatience = _pPatienceLeftMs[i];
                    best = i;
                    bestStage = st;
                }
            }

            // Those whose food is ready are waiting to be served; they are
            // marked separately.
            for (int i = 0; i < MaxParties; i++)
            {
                if (!_pActive[i] || _pInTask[i] || _pKitchenTask[i]) continue;
                if (_pStage[i] != CustomerStage.WaitingForFood) continue;
                if (_pEatLeftMs[i] != -1) continue;    // -1 = cooked, waiting to be served
                if (_pPatienceLeftMs[i] < bestPatience)
                {
                    bestPatience = _pPatienceLeftMs[i];
                    best = i;
                    bestStage = CustomerStage.WaitingForFood;
                }
            }

            if (best < 0) { kind = TaskKind.None; ms = 0; return -1; }

            int size = _pSize[best];
            switch (bestStage)
            {
                case CustomerStage.WaitingToOrder:
                    kind = TaskKind.SeatOrder; ms = _timing.SeatOrderMs * size; break;
                case CustomerStage.WaitingForFood:
                    kind = TaskKind.Serve; ms = _timing.ServeMs * size; break;
                default:
                    kind = TaskKind.Pay; ms = _timing.PayMs * size; break;
            }

            // A TABLE THE OWNER HAS SEEN TO: the work shortens and the mark
            // IS SPENT. Attention is a SINGLE STEP, not a standing condition -
            // otherwise a table seen to once would stay privileged all day.
            if (_pAttended[best] && _economy.AttendWorkCutBp > 0)
            {
                ms = (int)Fx.Bp(ms, Fx.One - _economy.AttendWorkCutBp);
                if (ms < 1) ms = 1;
                _pAttended[best] = false;
            }
            return best;
        }

        /// <summary>
        /// The hall's server number zero is THE OWNER. The capacity model
        /// counts the owner as 1.4 person-days (docs/14), that is, faster
        /// than a member of staff. We apply the same factor to the task
        /// duration here; otherwise the simulation would produce lower output
        /// than the capacity model.
        /// </summary>
        private int OwnerAdjusted(int serverIndex, int ms)
        {
            if (serverIndex != 0) return ms;
            return (int)Fx.MulDiv(ms, Fx.One, _economy.OwnerWorkMicro / 100);
        }

        /// <summary>
        /// Is the washing-up URGENT - that is, should the hall work be
        /// dropped for the sink.
        ///
        /// Either reason is enough:
        ///   - the clean plates have run out or are about to (the kitchen is
        ///     stopping),
        ///   - the dirty pile has passed the threshold.
        ///
        /// Hysteresis: once it has begun it carries on until the pile falls
        /// to the LOWER threshold. With a single threshold the waiter went
        /// back and forth on every frame.
        /// </summary>
        private bool WashNeeded()
        {
            if (_platesDirty <= 0) return false;

            // WITH A DISHWASHER ON, THE HALL DOES NOT GET INVOLVED.
            //
            // The rule's justification is the user's own words: "once you
            // take on a dishwasher everybody does their own job".
            //
            // I LOOSENED THIS ONCE AND PUT IT BACK. The dedicated dishwasher
            // was not opening the plate bottleneck but making it worse
            // (waiting without plates 263 -> 351), and the exception "let the
            // hall come to the rescue when the clean plates are about to run
            // out" changed nothing at all: 351 -> 351. The reason was that the
            // exception could find no FREE PERSON to fire for - washing is
            // looked at AFTER the customer work and at the peak the hall is
            // already full.
            //
            // Weakening the user's design rule on an unmeasured justification
            // would have been wrong; the rule stands. The real cause and the
            // explicit decision are in docs/49 §5.
            if (_dishwashers > 0) return false;

            int total = _economy.TierForTables(_tableCount).Plates;
            if (_platesClean * 4 <= total) return true;        // a quarter of the clean ones left
            if (_washing && _platesDirty * 4 > total) return true;   // not down to the lower threshold
            return _platesDirty * 2 >= total;                  // half of them dirty
        }

        /// <summary>Is somebody from the hall at the sink right now (for the hysteresis).</summary>
        private bool _washing;

        /// <summary>How many hall staff are dedicated to the sink.</summary>
        private int _dishwashers;

        /// <summary>The plates washed today. For the report and for diagnostics.</summary>
        private int _washedToday;

        /// <summary>
        /// The plates DIRTIED today - cumulative.
        ///
        /// The instantaneous dirty count ("how many plates are at the sink
        /// right now") DOES NOT SAY whether the cycle is turning: at the
        /// start of the day nobody has finished eating and the number is
        /// zero. A cumulative counter is the right measure of "did the cycle
        /// turn today".
        /// </summary>
        private int _dirtiedToday;

        /// <summary>
        /// Has the "out of plates" notification been given.
        ///
        /// Announcing it on every tick would fill the notification strip with
        /// a single sentence; the flag drops when a plate appears, so each
        /// NEW blockage is said once.
        /// </summary>
        private bool _plateWarned;

        /// <summary>
        /// IS THE KITCHEN STOPPED RIGHT NOW FOR WANT OF PLATES.
        ///
        /// It lives the same life as `_plateWarned` but its job is different:
        /// that one is a NOTIFICATION flag, this one is a DECISION input.
        /// Folding the two into a single field would mean a change that
        /// silenced the notification silently switching off the hall's crisis
        /// behaviour as well.
        /// </summary>
        private bool _plateStalled;

        /// <summary>
        /// The hall tasks that ran to the sink because the kitchen had
        /// stopped.
        ///
        /// SEPARATE from `_hallRushWashes`: that one measures whether the
        /// hall gets free of the sink when a dishwasher is on, and the crisis
        /// branch fires independently of the dishwasher.
        /// </summary>
        private int _hallCrisisWashes;

        private int FirstDirtyTable()
        {
            for (int t = 0; t < _tableCount; t++)
            {
                if (!_tableDirty[t] || _tableParty[t] >= 0) continue;
                if (BeingCleared(t)) continue;
                return t;
            }
            return -1;
        }

        /// <summary>
        /// Is there a waiter clearing this table.
        ///
        /// The dirty flag now drops when the task FINISHES, which means the
        /// same table could be picked by two waiters. Rather than keeping a
        /// separate "being cleared" array, the task list is consulted: a new
        /// state field would have to go into the save, the validation and the
        /// replay as well.
        /// </summary>
        private bool BeingCleared(int table)
        {
            for (int s = 0; s < MaxServers; s++)
                if (_hallTaskKind[s] == TaskKind.Clear && _hallTaskTarget[s] == table)
                    return true;
            return false;
        }

        // ---- 5. advance the tasks --------------------------------------------
        private void AdvanceTasks()
        {
            // The time the cook is tied up. When it ends the cook is free,
            // but the food goes on cooking AT THE STATION.
            for (int s = 0; s < MaxServers; s++)
            {
                if (_kitchenTaskKind[s] == TaskKind.None) continue;
                _kitchenTaskLeftMs[s] -= TimingConfig.TickMs;
                if (_kitchenTaskLeftMs[s] > 0) continue;
                _kitchenTaskKind[s] = TaskKind.None;
            }

            // The jobs at the stations. A slot frees when its job finishes.
            //
            // PARTIES are scanned, NOT THE ARRAY. _jobStation.Length =
            // MaxParties * MaxJobsPerParty = 1024, and this loop used to run
            // UNCONDITIONALLY on every tick; whereas the number of active
            // jobs can never exceed 14 tables x 4 = 56. A single check per
            // party brings 1024 iterations down to 256, and for most parties
            // the inner loop of four never opens at all.
            //
            // Tick runs ten times a second (160 times at the highest speed),
            // so every iteration here eats into the frame budget.
            for (int p = 0; p < MaxParties; p++)
            {
                if (_pJobsLeft[p] <= 0) continue;

                int b = p * MaxJobsPerParty;
                for (int k = 0; k < MaxJobsPerParty; k++)
                {
                    int j = b + k;
                    if (_jobState[j] != 1) continue;

                    _jobMs[j] -= TimingConfig.TickMs;
                    if (_jobMs[j] > 0) continue;

                    _jobState[j] = 2;
                    _stationBusy[_jobStation[j]] -= _jobSlots[j];

                    if (!_pActive[p]) continue;
                    if (--_pJobsLeft[p] > 0) continue;

                    _pKitchenTask[p] = false;

                    // COOKED, BUT NOT YET ON A PLATE.
                    //
                    // This used to mark them as waiting to be served
                    // directly. There is now a step in between: the cook
                    // cannot send the food out without taking a clean plate
                    // (PlateUp). With no clean plate the food waits on the
                    // pass - this is the plate bottleneck in the form the
                    // player sees.
                    _pCooked[p] = true;
                }
            }

            for (int s = 0; s < MaxServers; s++)
            {
                if (_hallTaskKind[s] == TaskKind.None) continue;
                _hallTaskLeftMs[s] -= TimingConfig.TickMs;
                if (_hallTaskLeftMs[s] > 0) continue;

                TaskKind kind = _hallTaskKind[s];
                int target = _hallTaskTarget[s];
                _hallTaskKind[s] = TaskKind.None;

                if (kind == TaskKind.Clear)
                {
                    // The table only frees up NOW.
                    if (target >= 0 && target < MaxTables)
                    {
                        _tableDirty[target] = false;

                        // The dirty plates go to the sink with the waiter.
                        int carried = _tablePlates[target];
                        _tablePlates[target] = 0;
                        _platesInUse -= carried;
                        _platesDirty += carried;
                        _dirtiedToday += carried;
                    }
                    Emit(SimEventKind.TableCleared, target);
                    continue;
                }

                if (kind == TaskKind.Wash)
                {
                    // One plate per go. When the wash finishes it joins the
                    // clean pile.
                    if (_platesDirty > 0)
                    {
                        _platesDirty--;
                        _platesClean++;
                    }
                    _washedToday++;
                    if (_platesDirty == 0) _washing = false;
                    continue;
                }

                if (!_pActive[target]) continue;
                _pInTask[target] = false;

                switch (kind)
                {
                    case TaskKind.SeatOrder:
                        // The stock is deducted WHEN THE ORDER IS TAKEN. Had
                        // it run out later, the customer would have been kept
                        // waiting at the table; the pressure of the market
                        // stage should be felt at the moment of ordering.
                        Consume(_pDishMain[target], _pSize[target]);
                        Consume(_pDishSide[target], _pSize[target]);
                        Consume(_pDishDrink[target], _pSize[target]);
                        Consume(_pDishDessert[target], _pSize[target]);
                        _pStage[target] = CustomerStage.WaitingForFood;
                        _pEatLeftMs[target] = 0;      // 0 = cooking
                        BuildJobs(target);
                        if (_pDishMain[target] >= 0) _orderedRole[0] += _pSize[target];
                        if (_pDishSide[target] >= 0) _orderedRole[1] += _pSize[target];
                        if (_pDishDrink[target] >= 0) _orderedRole[2] += _pSize[target];
                        if (_pDishDessert[target] >= 0) _orderedRole[3] += _pSize[target];
                        Emit(SimEventKind.OrderTaken, target, _pDishMain[target]);
                        break;

                    case TaskKind.Serve:
                        _pStage[target] = CustomerStage.Eating;
                        _pEatLeftMs[target] = _timing.EatMs;
                        Emit(SimEventKind.FoodServed, target, _pTable[target]);
                        break;

                    case TaskKind.Pay:
                        CompletePayment(target);
                        break;
                }
            }
        }

        /// <summary>
        /// PUTS THE COOKED FOOD ON A CLEAN PLATE.
        ///
        /// When the kitchen finishes its job the food is ready but not
        /// servable: the cook has to take a clean plate. With no plate the
        /// food waits on the pass and a counter runs - that counter is THE
        /// MEASURE of the sentence "the washing-up was neglected".
        ///
        /// It starts with whoever has the least patience: when plates are
        /// scarce, whose food comes out must not be random.
        /// </summary>
        private void PlateUp()
        {
            while (true)
            {
                int best = -1;
                int bestPatience = int.MaxValue;
                for (int i = 0; i < MaxParties; i++)
                {
                    if (!_pActive[i] || !_pCooked[i]) continue;
                    if (_pPatienceLeftMs[i] >= bestPatience) continue;
                    bestPatience = _pPatienceLeftMs[i];
                    best = i;
                }
                if (best < 0)
                {
                    // A LATCH: THE FLAG HAS TO DROP HERE TOO.
                    //
                    // There used to be only a `return` here and
                    // `_plateStalled` WAS STICKING: once the blocked party
                    // lost patience and left (LeaveAngry does not clear
                    // `_pCooked`), this exit was taken silently on every
                    // tick, the kitchen was not stopped and yet the hall
                    // stayed in the CRISIS branch - so nobody took an order,
                    // no newly cooked party appeared, and the flag fed
                    // itself.
                    //
                    // Dropping the flag here too makes it DERIVED: its value
                    // is recomputed on every tick in PlateUp (PlateUp runs
                    // BEFORE DispatchHall). That is also why it NEED NOT be
                    // written into the save - the first tick after a load
                    // establishes the right value.
                    _plateStalled = false;
                    return;
                }

                int need = _pSize[best];
                if (_platesClean < need)
                {
                    // NO PLATES. The counter runs and the loop stops here -
                    // slipping in a smaller party that needs fewer plates
                    // would mean leaving whoever has the least patience
                    // waiting.
                    _plateBlockedTicks++;
                    _plateStalled = true;

                    // THE PLAYER IS TOLD ONCE.
                    //
                    // Announcing it on every tick would fill the notification
                    // strip with a single sentence; saying nothing means
                    // service slows for no reason and the player reads that
                    // not as a mechanic but as a BUG. The flag drops when a
                    // plate appears, so each NEW blockage is said once.
                    if (!_plateWarned)
                    {
                        _plateWarned = true;
                        Emit(SimEventKind.PlatesOut, _platesDirty, _dishwashers);
                    }
                    return;
                }

                _plateWarned = false;
                _plateStalled = false;
                _platesClean -= need;
                _platesInUse += need;
                _pPlates[best] += need;
                _pCooked[best] = false;
                // UNDER SELF SERVICE THERE IS NO SERVING STEP.
                //
                // A customer who orders at the counter takes their own tray;
                // nobody brings it to the table. This is the largest
                // structural difference between the two cuisines (docs/51),
                // and this step carried half the hall's workload.
                //
                // The accounting is untouched: satisfaction, reputation and
                // leaving the table dirty are still in CompletePayment.
                if (_content.SelfService)
                {
                    _pStage[best] = CustomerStage.Eating;
                    _pEatLeftMs[best] = _timing.EatMs;
                    Emit(SimEventKind.FoodServed, best, _pTable[best]);
                }
                else
                {
                    _pEatLeftMs[best] = -1;           // plated, waiting to be served
                }
                Emit(SimEventKind.FoodReady, best, _pDishMain[best]);
            }
        }

        private void AdvanceEating()
        {
            for (int i = 0; i < MaxParties; i++)
            {
                if (!_pActive[i] || _pStage[i] != CustomerStage.Eating) continue;
                _pEatLeftMs[i] -= TimingConfig.TickMs;
                if (_pEatLeftMs[i] > 0) continue;
                // UNDER SELF SERVICE THERE IS NO WAITING TO PAY.
                //
                // The money was paid at the counter, at the moment of
                // ordering. The customer gets up and goes - their tray is
                // left on the table and THE CLEANER clears it
                // (CompletePayment leaves the table dirty).
                //
                // CompletePayment is still called: satisfaction, reputation,
                // the regular's record and the revenue are in there. The only
                // thing that changes is that the player NEED NOT send a
                // waiter to the table.
                if (_content.SelfService)
                {
                    _pStage[i] = CustomerStage.WaitingToPay;
                    CompletePayment(i);
                    continue;
                }

                _pStage[i] = CustomerStage.WaitingToPay;
                // Patience runs again while they wait to pay, but refreshed:
                // a customer who has eaten does not start from full patience
                // but from half of it.
                _pPatienceLeftMs[i] = _pPatienceTotalMs[i] / 2;
                _pWarned[i] = false;
            }
        }

        // ---- payment and satisfaction ----------------------------------------
        private void CompletePayment(int party)
        {
            int size = _pSize[party];
            long bill = OrderPrice(party) * size;
            long cost = OrderCost(party) * size;

            int satisfaction = ComputeSatisfaction(party, _pDishMain[party]);

            // The trait of the waiter seeing to the table. docs/14: the one
            // who gets on well with customers +8 points, the surly one -6.
            // The owner (server zero) has no traits.
            int server = _pServer[party];
            if (server > 0)
                satisfaction += TraitSum(1, server - 1, t => t.SatisfactionCenti);

            // The trait of the cook who made it: "slow but meticulous"
            // raises the food's quality (docs/14). The effect is applied to
            // the REMAINING share of satisfaction - it cannot improve a dish
            // already at the ceiling.
            int cook = _pCook[party];
            if (cook >= 0)
            {
                int q = TraitSum(0, cook, t => t.QualityBp);
                if (q > 0 && satisfaction < Fx.One)
                    satisfaction += (int)Fx.MulDiv(Fx.One - satisfaction, q, Fx.One);
                else if (q < 0)
                    satisfaction += (int)Fx.MulDiv(satisfaction, q, Fx.One);
            }

            // A regular who could not find their favourite dish leaves
            // short-changed, however well they were otherwise served.
            if (_pMissedFavourite[party])
            {
                satisfaction -= _economy.RegularMissedFavouriteCenti;
                if (satisfaction < 0) satisfaction = 0;
            }
            _pSatisfactionCenti[party] = satisfaction;

            // The tip: a satisfied customer, per the archetype's tipping
            // tendency.
            ArchetypeDef a = _content.Archetypes[_pArchetype[party]];
            if (satisfaction > 8000 && _rngStaffError.Chance(a.TipChanceBp))
                bill += Fx.Bp(bill, 1000);   // a 10% tip

            // The ingredients were paid for in cash AT THE MARKET IN THE
            // MORNING (OrderIngredient); they are not deducted a second time
            // here. Before the market stage existed they were deducted here,
            // and because that temporary line was not removed the ingredients
            // were being paid for twice. On the daily trace the gross profit
            // was 476 coins while the till rose by 116; the 360 in between was
            // exactly the second payment.
            SignatureDef sig = _content.Signature;

            // They asked for a tab and did not get one: they leave the table
            // embarrassed.
            if (_pAsksCredit[party] && !_pCredit[party] && HasCredit)
            {
                satisfaction -= sig.CreditRefusedPenaltyCenti;
                if (satisfaction < 0) satisfaction = 0;
                _pSatisfactionCenti[party] = satisfaction;
            }

            if (_pCredit[party] && HasCredit && _tabCount < MaxTabs)
            {
                // The bill DOES NOT GO INTO the till: it is written into the
                // tab book. Nor does it count as revenue today; it will count
                // when it is collected.
                // docs/07: "it upsets the cash flow but raises loyalty."
                _tabAmount[_tabCount] = bill;
                _tabDueDay[_tabCount] = _day + sig.CreditDueDays;
                _tabTea[_tabCount] = _pTea[party] ? 1 : 0;
                _tabRegular[_tabCount] = _pRegular[party];
                _tabCount++;
                _creditIssued += bill;      // for the year-end collection rate
                satisfaction += sig.CreditLoyaltyBonusCenti;
                if (satisfaction > Fx.One) satisfaction = Fx.One;
                _pSatisfactionCenti[party] = satisfaction;
            }
            else
            {
                _cash += bill;
                _revenue += bill;
                _revenueAll += bill;
            }
            _ingredientCost += cost;
            _servedParties++;
            _servedPeople += size;
            _satisfactionSum += (long)satisfaction * size;

            AccumulateReputation(party, satisfaction);
            RecordRegularVisit(party, satisfaction);

            FreeTableOf(party, dirty: true);
            _pStage[party] = CustomerStage.Done;
            _pActive[party] = false;
            _partyCount--;

            Emit(SimEventKind.CustomerPaid, party, (int)bill, satisfaction);
        }

        /// <summary>
        /// A dish's quality effect: whatever THE MOST DECISIVE ingredient
        /// says.
        ///
        /// A mean was taken first and measurement rejected it: on Turkish
        /// cuisine a player buying cheap ingredients BEAT good play, 35,200
        /// against 26,211. The cause was the mean itself: the cheap onion
        /// thrown into the pot hid the cheap meat. A six-ingredient stew
        /// divides the penalty by six, a three-ingredient hamburger by three.
        ///
        /// The customer does not think that way. They say "the meat is
        /// cheap"; they do not count how many onions came with it. So the
        /// ingredient with the largest absolute value decides.
        /// </summary>
        private int DishQualityCenti(int dish)
        {
            DishIngredient[] parts = _content.Dishes[dish].Ingredients;
            if (parts == null || parts.Length == 0) return 0;

            int strongest = 0;
            for (int i = 0; i < parts.Length; i++)
            {
                int q = _stockQualityCenti[parts[i].IngredientIndex];
                if (q < 0 ? -q > (strongest < 0 ? -strongest : strongest)
                          : q > (strongest < 0 ? -strongest : strongest))
                    strongest = q;
            }
            return strongest;
        }

        /// <summary>
        /// The deviation of every item on the bill from the market, weighted
        /// by that item's SHARE of the bill, in basis points.
        ///
        /// Why weighted: a 10% rise on a 46-centi main dish and a 10% rise on
        /// a 16-centi ayran are not the same thing. What the player feels is
        /// not the mean of the percentages but how much THE BILL has swollen.
        ///
        /// A combo bill already carries its own discount (ComboPrice) and the
        /// pricing decision there is a separate mechanic; with a combo, only
        /// the main dish is measured - otherwise the player would be punished
        /// for switching on the signature mechanic.
        /// </summary>
        private long WeightedPriceDiffBp(int party, int main)
        {
            // WITH A COMBO, THE WHOLE BILL IS MEASURED TOO.
            //
            // Only the main dish used to be looked at, and the justification
            // was "the combo carries its own discount, the player must not be
            // punished for switching on the signature mechanic". The
            // justification was right and the implementation wrong: a combo
            // bill sums the price of THREE items, and the combo is imposed on
            // every party. So leaving the main dish at the market price and
            // making the side and the drink as dear as you liked REACHED THE
            // CUSTOMER NOT AT ALL.
            //
            // Measured: with the combo on, a bot that made the extras 2000
            // times dearer piled up 24.8 MILLION coins, and satisfaction fell
            // only from 68.9 to 66.8.
            //
            // The right answer: the combo's OWN market equivalent (the market
            // sum of the three items x the combo discount) is compared with
            // the player's combo price. The discount is not punished; the
            // swelling is measured.
            if (_pCombo[party])
            {
                int[] cd = _content.Signature.ComboDishes;
                if (cd == null || cd.Length < 3) return DishPriceDiffBp(main);

                long market = _content.Dishes[main].Price
                            + _content.Dishes[cd[1]].Price
                            + _content.Dishes[cd[2]].Price;
                market = Fx.MulDiv(market, _content.Signature.ComboPriceBp, Fx.One);
                if (market <= 0) return 0;

                long deviation = Fx.MulDiv(ComboPrice(party) - market, Fx.One, market);
                long floor = _economy.UnderpriceFloorBp - Fx.One;
                return deviation < floor ? floor : deviation;
            }

            long total = 0, weight = 0;
            Add(ref total, ref weight, main);
            Add(ref total, ref weight, _pDishSide[party]);
            Add(ref total, ref weight, _pDishDrink[party]);
            Add(ref total, ref weight, _pDishDessert[party]);

            return weight > 0 ? total / weight : 0;
        }

        private void Add(ref long total, ref long weight, int dish)
        {
            if (dish < 0) return;
            long market = _content.Dishes[dish].Price;
            if (market <= 0) return;
            // The weight is THE MARKET price, not the player's: otherwise
            // selling dearly would enlarge the item's weight as well and the
            // penalty would feed itself.
            total += DishPriceDiffBp(dish) * market;
            weight += market;
        }

        /// <summary>A single dish's deviation from the market, with a floor.</summary>
        /// <summary>
        /// The menu's MEAN price deviation, in basis points. For the demand
        /// channel.
        ///
        /// The weights are the same as the ordering probabilities: everybody
        /// takes the main, the side/drink/dessert come by chance.
        /// RecommendedRestock uses the same weights - with two separate sets
        /// of weights in two places, the market screen and demand would
        /// contradict each other.
        ///
        /// Making a single dish dearer affects demand little, making the
        /// whole menu dearer affects it greatly: what is measured is the
        /// player's PRICING POLICY, not one item.
        /// </summary>
        private long MenuPriceDiffBp()
        {
            int mains = 0, sides = 0, drinks = 0, desserts = 0;
            for (int i = 0; i < _content.Dishes.Length; i++)
            {
                if (!_dishOnMenu[i] || !Unlocked(i)) continue;
                string g = _content.Dishes[i].Group;
                if (_content.IsInRole(g, _content.MainGroups)) mains++;
                else if (_content.IsInRole(g, _content.SideGroups)) sides++;
                else if (_content.IsInRole(g, _content.DrinkGroups)) drinks++;
                else if (_content.IsInRole(g, _content.DessertGroups)) desserts++;
            }
            if (mains == 0) return 0;          // the menu is empty: no deviation either
            if (sides == 0) sides = 1;
            if (drinks == 0) drinks = 1;
            if (desserts == 0) desserts = 1;

            long total = 0, weight = 0;
            for (int i = 0; i < _content.Dishes.Length; i++)
            {
                if (!_dishOnMenu[i] || !Unlocked(i)) continue;
                string g = _content.Dishes[i].Group;

                long w;
                if (_content.IsInRole(g, _content.MainGroups))
                    w = Fx.One / mains;
                else if (_content.IsInRole(g, _content.SideGroups))
                    w = _economy.SideChanceBp / sides;
                else if (_content.IsInRole(g, _content.DrinkGroups))
                    w = _economy.DrinkChanceBp / drinks;
                else if (_content.IsInRole(g, _content.DessertGroups))
                    w = _economy.DessertChanceBp / desserts;
                else continue;

                if (w <= 0) continue;
                total += DishPriceDiffBp(i) * w;
                weight += w;
            }
            return weight > 0 ? total / weight : 0;
        }

        /// <summary>
        /// A day's expected customer count, PRICE INCLUDED.
        ///
        /// ALL SIX CALL SITES GO THROUGH HERE. Each of them used to call
        /// DemandModel.CustomersPerDay directly; missing one while adding the
        /// price channel would mean the market screen recommending stock for
        /// customers who would not actually arrive - a silent inconsistency
        /// that would throw the player's money in the bin.
        /// </summary>
        private int ExpectedCustomers(int dayFactorBp)
        {
            int people = DemandModel.CustomersPerDay(
                _tableCount, _reputationCenti, _economy.CustomerBasePerTable, dayFactorBp);

            // THE CUISINE'S VOLUME. Fast food brings more people to the same
            // table - the numeric form of the promise "crowded, low ticket".
            // The multiplier is applied at A SINGLE GATE, so the crew
            // recommendation, the market recommendation and the arrival plan
            // all see the same number.
            int multiplier = _content.CustomerMultiplierBp;
            if (multiplier > 0 && multiplier != Fx.One)
                people = (int)Fx.Bp(people, multiplier);

            // THE TAB'S LOYALTY - THE CUSTOMER WHO COMES BACK - LIVES HERE,
            // AT THE GATE, AND UNTIL 19 SEPTEMBER IT DID NOT.
            //
            // It was applied inside RecommendedRestock alone, after this
            // method had returned. So every settled account raised the
            // number of people the MARKET screen bought stock for, and not
            // the number who arrived: the neighbourhood's trust bought
            // ingredients for customers who never came. Measured, Turkish, 8
            // seeds: the bot that runs tabs served FEWER people than the one
            // that does not (1,829 against 1,846) and spoiled 2,700 coins
            // more. The sentence at the top of this method warned about
            // exactly this - "recommending stock for customers who would not
            // actually arrive" - one mechanic before it happened.
            if (_creditLoyaltyBp > 0)
                people = (int)Fx.MulDiv(people, Fx.One + _creditLoyaltyBp, Fx.One);

            return DemandModel.ApplyPrice(people, MenuPriceDiffBp(),
                                          _economy.PriceElasticityBp);
        }

        /// <summary>
        /// The number of customers who will ACTUALLY come today.
        ///
        /// ExpectedCustomers is THE EXPECTATION; this puts the day's variance
        /// on top of it. THE DISTINCTION IS DELIBERATE and the whole mechanic
        /// lies in it:
        ///
        ///   the forecast -> the crew recommendation, the market
        ///                   recommendation, the expected covers
        ///   the actual   -> the arrival plan alone
        ///
        /// Had the variance shown up in the forecast too, the player would
        /// once again hold certain knowledge and the volatility would stay
        /// decoration. The real gain is here: the morning's stock decision is
        /// now a JUDGEMENT - buy too much and it goes in the bin, buy too
        /// little and customers turn back at the door.
        ///
        /// The draw happens ONCE A DAY and comes from the _rngEvent stream:
        /// that stream already existed, went into the save, and was never
        /// used. Replay stays byte for byte the same.
        /// </summary>
        private int ActualCustomers(int dayFactorBp)
        {
            int expected = ExpectedCustomers(dayFactorBp);
            int variance = _economy.DemandVarianceBp;
            if (variance <= 0 || expected <= 0) return expected;

            // A single draw in the range [-variance, +variance]. Integer:
            // floating point is banned in the core (docs/23 2.5).
            int range = 2 * variance + 1;
            int deviation = (int)(_rngEvent.Next() % (uint)range) - variance;

            int actual = (int)Fx.MulDiv(expected, Fx.One + deviation, Fx.One);
            return actual < 0 ? 0 : actual;
        }

        private long DishPriceDiffBp(int dish)
        {
            if (dish < 0) return 0;
            long market = _content.Dishes[dish].Price;
            if (market <= 0) return 0;

            long diffBp = Fx.MulDiv(_dishPrice[dish] - market, Fx.One, market);
            // There is a floor on going below the market; it is written in
            // the content as underpriceFloorBp (8500 = 15%).
            long floorBp = _economy.UnderpriceFloorBp - Fx.One;
            if (diffBp < floorBp) diffBp = floorBp;
            return diffBp;
        }

        /// <summary>The satisfaction formula of docs/12 5.4, in centi-points.</summary>
        private int ComputeSatisfaction(int party, int dish)
        {
            int sat = 10000;

            // The waiting penalty: (waited / patience) x 60 points
            if (_pPatienceTotalMs[party] > 0)
            {
                long penalty = Fx.MulDiv(_pWaitedMs[party], 6000, _pPatienceTotalMs[party]);
                sat -= (int)penalty;
            }

            // THE PRICE PENALTY APPLIES TO THE WHOLE BILL, not to the main
            // dish alone.
            //
            // Only THE MAIN DISH's price used to be compared with the market;
            // and yet the bill writes all four items (OrderPrice) and SetPrice
            // has no upper limit. So on either cuisine 20 of the 32 dishes -
            // the sides, the drinks, the desserts - could be sold as dearly as
            // you liked and the customer saw NOTHING of it.
            //
            // Measured: Turkish cuisine has a single drink (ayran, 16). Over
            // sixty days, 1896 people, with a 40% chance of a drink. Raising
            // the ayran to 160 brings in +109,000 centi - six times the whole
            // campaign's profit, at zero risk.
            //
            // The penalty is now each item's deviation weighted by ITS SHARE
            // OF THE BILL. With prices at their content values every deviation
            // is zero, so this change DOES NOT ALTER BEHAVIOUR in an untouched
            // game - the calibration's reference bots do not touch prices.
            long deviation = WeightedPriceDiffBp(party, dish);
            if (deviation != 0)
            {
                int sens = _content.Archetypes[_pArchetype[party]].PriceSensitivityBp;
                sat -= (int)Fx.MulDiv(deviation, sens, Fx.One);
            }

            sat += _pBonusCenti[party];

            // INGREDIENT QUALITY. The mean quality in stock of the
            // ingredients in the dish's recipe. Because the content makes the
            // six most sensitive ingredients MEAT, the same global setting
            // gives a heavy result on a meat dish and a light one on pasta:
            // cutting corners is free on salt and a disaster on meat.
            if (dish >= 0) sat += DishQualityCenti(dish);

            // A customer who could not get the dish they asked for. docs/02,
            // "visible growth": the price of missing equipment is not an
            // abstract loss of speed but disappointment at the table.
            if (_pAskedDish[party] >= 0)
            {
                // The magnifier is a PROPORTION too. The reason is the same
                // as in AskForMissingDish: an absolute count put a player who
                // narrowed their menu sensibly in the same scale as one
                // keeping a single dish.
                int waiting = 0, total = 0;
                for (int i = 0; i < _content.Dishes.Length; i++)
                {
                    if (!UnlockedMain(i)) continue;
                    total++;
                    if (Awaited(i)) waiting++;
                }
                int scale = total > 0
                    ? Fx.One + (int)Fx.MulDiv(Fx.One, waiting, total)
                    : Fx.One;
                sat -= (int)Fx.MulDiv(_economy.AskMissCenti, scale, Fx.One);
            }

            // COMPLEXITY IS ONLY RISK, never a reward.
            //
            // In the first draft the deviation was magnified in both
            // directions and measurement caught it: because 17 of the Turkish
            // menu are complexity 3, the do-nothing "market only" player
            // jumped from 5,094 to 33,488. Since most customers are satisfied
            // anyway, the magnifier worked in one direction in practice and
            // inflated reputation.
            //
            // The reward is already IN THE PRICE: a complexity 3 dish is
            // priced at twice a complexity 1 one. What belongs here is its
            // price: take a dish that takes real skill to the table late and
            // the customer is angrier.
            // The scale is RELATIVE, not ABSOLUTE: a dish's complexity against
            // the mean of its own cuisine. On an absolute scale 17 of the
            // Turkish restaurant's dishes count as "hard" and a good player's
            // reputation stayed at 51.6, while on fast food it was 99.0. On a
            // relative scale fast food's few hard dishes are GENUINELY hard,
            // while the Turkish restaurant's stew is ordinary for it.
            int neutral = _economy.SatisfactionNeutralCenti;
            if (dish >= 0 && sat < neutral)
            {
                int relBp = _content.RelativeComplexityBp(dish);
                if (relBp > Fx.One)
                {
                    int lossBp = Fx.One + (int)Fx.MulDiv(relBp - Fx.One, 4000, Fx.One);
                    sat = neutral - (int)Fx.MulDiv(neutral - sat, lossBp, Fx.One);
                }
                else if (relBp < Fx.One)
                {
                    // A dish below the mean is more forgiving.
                    int easeBp = Fx.One - (int)Fx.MulDiv(Fx.One - relBp, 4000, Fx.One);
                    sat = neutral - (int)Fx.MulDiv(neutral - sat, easeBp, Fx.One);
                }
            }

            if (sat < 0) sat = 0;
            if (sat > 10000) sat = 10000;
            return sat;
        }

        /// <summary>
        /// docs/12 5.5: daily_change = Sum (satisfaction - 60) x weight / 100
        /// In centi-points and basis points: (sat - 6000) x weightBp / 1,000,000
        /// The running total is kept in micro-points and rounded once at the
        /// end of the day.
        /// </summary>
        private void AccumulateReputation(int party, int satisfactionCenti)
        {
            ArchetypeDef a = _content.Archetypes[_pArchetype[party]];
            long delta = (long)(satisfactionCenti - _economy.SatisfactionNeutralCenti)
                         * a.ReputationWeightBp * _pSize[party];
            _reputationDeltaMicro += delta;   // on the 1e6 scale
        }

        /// <summary>
        /// The overflow vessel's ceiling: the gap up to the NEXT tier's
        /// ceiling. Zero at the top tier - there the ceiling is already 100.
        /// </summary>
        private int OverflowCapCenti()
        {
            int current = _economy.TierForTables(_tableCount).ReputationCapCenti;
            if (current <= 0 || current > 10000) current = 10000;

            int above = current;
            for (int i = 0; i < _economy.TierCount; i++)
            {
                int c = _economy.TierAt(i).ReputationCapCenti;
                if (c > current && (above == current || c < above)) above = c;
            }
            return above > current ? above - current : 0;
        }

        private void ApplyReputation()
        {
            // The natural erosion: 0.3 points a day = 30 centi
            long deltaCenti = _reputationDeltaMicro / 1_000_000L
                              - _economy.ReputationDecayPerDayCenti;

            // Diminishing returns. The balance tool's finding: with the
            // undamped formula +12 points were gained a day and reputation
            // went from 30 to 100 in nine days; so the main progression axis
            // of a sixty-day campaign was used up in the first week.
            //
            // A gain is scaled by the headroom left, A LOSS IS NOT: reputation
            // is hard to earn and easy to lose. That is also true of running a
            // restaurant, and it matches the player comments in the research.
            if (deltaCenti > 0)
            {
                int headroom = 10000 - _reputationCenti;
                if (headroom < 0) headroom = 0;

                // A FLOOR. The damping was put in to stop reputation hitting
                // the ceiling in nine days, and it does that job. But near the
                // top it was too harsh: measurement showed a 3.5-point
                // difference in SATISFACTION between the two cuisines turning
                // into a 20-point difference in REPUTATION (fast food 94.2,
                // Turkish 74.5).
                //
                // The cause is that the damping is linear: once reputation
                // reaches 90 the gain drops to a tenth and the daily erosion
                // beats it. The floor stops the region at the top being a
                // knife edge; the early damping stands exactly as it was.
                // The floor is 1500. It was 3000 before and softened the
                // region at the top too much: even at a reputation of 100 the
                // daily gain stayed positive, so STAYING at the top took no
                // effort.
                if (headroom < 1500) headroom = 1500;
                deltaCenti = Fx.MulDiv(deltaCenti, headroom, 10000);
            }

            int before = _reputationCenti;
            _reputationCenti += (int)deltaCenti;
            if (_reputationCenti < 0) _reputationCenti = 0;

            // THE CEILING COMES FROM THE TIER. A four-table shop cannot be
            // the restaurant the whole neighbourhood talks about; reputation
            // only opens upwards as you grow.
            int cap = _economy.TierForTables(_tableCount).ReputationCapCenti;
            if (cap <= 0 || cap > 10000) cap = 10000;
            if (_reputationCenti > cap)
            {
                // THE OVERFLOW IS NOT DELETED, IT IS BANKED.
                _reputationOverflowCenti += _reputationCenti - cap;
                _reputationCenti = cap;

                // THE VESSEL HOLDS ONE TIER'S WORTH: unbounded accumulation
                // would fling reputation straight to the ceiling on the day of
                // an expansion and make the new tier's own work meaningless.
                int vessel = OverflowCapCenti();
                if (_reputationOverflowCenti > vessel) _reputationOverflowCenti = vessel;
            }

            Emit(SimEventKind.ReputationChanged, _reputationCenti, _reputationCenti - before);
        }

        // ---- the arrival plan ------------------------------------------------
        /// <summary>
        /// At the start of the day every arrival is worked out in advance and
        /// sorted by tick. That way no randomness is called for during
        /// service, and replay becomes cheap.
        /// </summary>
        private void BuildArrivalPlan()
        {
            int dayFactorBp = IsWeekend(_day)
                ? _economy.WeekendMultiplierBp
                : _economy.WeekdayMultiplierBp;

            // The ACTUAL number, not the forecast. The difference is what
            // the player's morning stock decision is worth.
            int people = ActualCustomers(dayFactorBp);
            _plannedPeople = people;

            _arrCount = 0;
            _arrNext = 0;
            int assigned = 0;

            while (assigned < people && _arrCount < MaxParties)
            {
                int arch = PickArchetype();
                ArchetypeDef a = _content.Archetypes[arch];

                int size = a.GroupSizeMin;
                if (a.GroupSizeMax > a.GroupSizeMin)
                    size = _rngArrival.NextInt(a.GroupSizeMin, a.GroupSizeMax + 1);
                if (assigned + size > people) size = people - assigned;
                if (size < 1) size = 1;

                // The slots are NOT EQUAL; each cuisine has its own shape of
                // day. docs/28-peak-decision.md Decision G.
                int slot = PickSlot(a);
                int tick = _timing.SlotStartTick(slot)
                           + _rngArrival.NextInt(_timing.SlotTicks(slot));

                _arrTick[_arrCount] = tick;
                _arrArchetype[_arrCount] = arch;
                _arrSize[_arrCount] = size;
                _arrRegular[_arrCount] = -1;
                _arrCount++;
                assigned += size;
            }

            SortArrivals();

            // The regulars are bound after the plan has been built AND
            // SORTED: they are not added to demand, they take a place from
            // within the plan. It has to happen after the sort - SortArrivals
            // does not carry the _arrRegular array along.
            BindRegularsToPlan();
        }

        private bool IsWeekend(int day)
        {
            int dayOfWeek = ((day - 1) % 7) + 1;      // 1..7
            return dayOfWeek > 7 - _economy.WeekendDaysPerWeek;
        }

        private int PickArchetype()
        {
            int total = 0;
            for (int i = 0; i < _content.Archetypes.Length; i++)
                total += _content.Archetypes[i].Weight;
            if (total <= 0) return 0;

            int pick = _rngArchetype.NextInt(total);
            for (int i = 0; i < _content.Archetypes.Length; i++)
            {
                pick -= _content.Archetypes[i].Weight;
                if (pick < 0) return i;
            }
            return _content.Archetypes.Length - 1;
        }

        private int PickSlot(ArchetypeDef a)
        {
            int pick = _rngArrival.NextInt(Fx.One);
            int acc = 0;
            for (int s = 0; s < a.ArrivalWeightsBp.Length; s++)
            {
                acc += a.ArrivalWeightsBp[s];
                if (pick < acc) return s;
            }
            return a.ArrivalWeightsBp.Length - 1;
        }

        /// <summary>
        /// Insertion sort. Stable and deterministic; Array.Sort with a
        /// comparison delegate is not guaranteed to be stable.
        /// </summary>
        private void SortArrivals()
        {
            for (int i = 1; i < _arrCount; i++)
            {
                int t = _arrTick[i], a = _arrArchetype[i], s = _arrSize[i];
                int j = i - 1;
                while (j >= 0 && _arrTick[j] > t)
                {
                    _arrTick[j + 1] = _arrTick[j];
                    _arrArchetype[j + 1] = _arrArchetype[j];
                    _arrSize[j + 1] = _arrSize[j];
                    j--;
                }
                _arrTick[j + 1] = t;
                _arrArchetype[j + 1] = a;
                _arrSize[j + 1] = s;
            }
        }

        // ---- the day report --------------------------------------------------
        public DayReport BuildDayReport()
        {
            int avgSat = _servedPeople > 0
                ? (int)(_satisfactionSum / _servedPeople)
                : 0;
            return new DayReport(_day, _servedParties, _servedPeople, _angryParties,
                                 _revenue, _ingredientCost, avgSat, _reputationCenti,
                                 _arrCount, _cash, _turnedAwayParties,
                                 _dayWages, _dayRent, _daySpoiled, _angrySeated);
        }

        private void Emit(SimEventKind kind, int a = 0, int b = 0, int c = 0, int d = 0)
        {
            _events.Push(new SimEvent(_tickIndex, kind, a, b, c, d));
        }
    }

    /// <summary>The summary of one service day.</summary>
    public readonly struct DayReport
    {
        public readonly int Day;
        public readonly int ServedParties;
        public readonly int ServedPeople;
        public readonly int AngryParties;

        /// <summary>
        /// The parties that left angry AFTER SITTING DOWN AT A TABLE.
        ///
        /// AngryParties counts both: those turned back at the door for want of
        /// a table, and those who sat, waited and ran out of patience. These
        /// are NOT THE SAME THING - one is a capacity problem, the other a
        /// service problem - and what the player can do about them differs
        /// too. As long as the screens, the balance tool's CSV and its
        /// warnings did not carry the distinction, the "angry customers"
        /// figure folded two different troubles into one number.
        /// </summary>
        public readonly int AngrySeatedParties;
        public readonly long Revenue;
        public readonly long IngredientCost;
        public readonly int AverageSatisfactionCenti;
        public readonly int ReputationCenti;
        public readonly int PlannedParties;
        public readonly long Cash;
        /// <summary>The parties turned back at the door for finding nothing on the menu.</summary>
        public readonly int TurnedAwayParties;

        /// <summary>
        /// The wages and rent paid TODAY. It may be zero - they are paid on
        /// one day a week.
        ///
        /// Added to the report because the number shown as "Profit" DID NOT
        /// INCLUDE them: the player hired staff, saw revenue rise, saw
        /// "profit" rise too, and then the till emptied and no screen
        /// explained why. The game's central tension - crew means capacity,
        /// but crew also means money - was visible nowhere.
        /// </summary>
        public readonly long WageCost;
        public readonly long RentCost;

        /// <summary>
        /// The value of the stock going into the bin tonight.
        ///
        /// Added to the report because it was THE LARGEST INVISIBLE OUTGOING:
        /// a reasonable player throws away fifty-seven per cent of the
        /// ingredients they buy across sixty days - 21,912 coins, more than
        /// the year's net profit - and that number appeared on no screen in
        /// the game. The player restocked every morning, saw a "profit" in the
        /// evening, and could not understand why the till was not filling up.
        ///
        /// IT DOES NOT GO INTO NET PROFIT, and it must not: the money left
        /// when the stock was bought, and deducting it again here would count
        /// it twice. This is a LOSS line, not a payment.
        /// </summary>
        public readonly long SpoiledValue;

        /// <summary>The day's NET profit: revenue - ingredients - wages - rent.</summary>
        public long NetProfit
        {
            get { return Revenue - IngredientCost - WageCost - RentCost; }
        }

        public DayReport(int day, int servedParties, int servedPeople, int angryParties,
                         long revenue, long ingredientCost, int averageSatisfactionCenti,
                         int reputationCenti, int plannedParties, long cash,
                         int turnedAwayParties = 0,
                         long wageCost = 0, long rentCost = 0, long spoiledValue = 0,
                         int angrySeatedParties = 0)
        {
            AngrySeatedParties = angrySeatedParties;
            TurnedAwayParties = turnedAwayParties;
            WageCost = wageCost;
            RentCost = rentCost;
            SpoiledValue = spoiledValue;
            Day = day; ServedParties = servedParties; ServedPeople = servedPeople;
            AngryParties = angryParties; Revenue = revenue; IngredientCost = ingredientCost;
            AverageSatisfactionCenti = averageSatisfactionCenti;
            ReputationCenti = reputationCenti; PlannedParties = plannedParties; Cash = cash;
        }

        public long GrossProfit { get { return Revenue - IngredientCost; } }
    }
}
