using System;

namespace Lokanta.Core.Economy
{
    /// <summary>
    /// One staff trait. docs/14: each member of staff draws TWO from the pool.
    ///
    /// The design intent is written down in docs/14: "no trait is purely
    /// good or purely bad." The two that look free of charge (gets on well
    /// with customers, lifts the crew's morale) pay their price in the pool:
    /// each has an EVIL TWIN and the two conflict, so a good trait is not an
    /// advantage you choose but a piece of luck.
    /// </summary>
    public sealed class TraitDef
    {
        public string Id { get; }
        public string NameKey { get; }
        /// <summary>The effect on task speed, in basis points. +1800 = 18% faster.</summary>
        public int SpeedBp { get; }
        /// <summary>The satisfaction difference at the table they serve, in centi-points.</summary>
        public int SatisfactionCenti { get; }
        /// <summary>The wage difference, in basis points.</summary>
        public int WageBp { get; }
        /// <summary>The experience gain multiplier, in basis points. 20000 = double, 0 = none.</summary>
        public int XpBp { get; }
        /// <summary>Extra slowdown in a busy slot, in basis points.</summary>
        public int PeakPenaltyBp { get; }
        /// <summary>Extra slowdown in the last quarter of the day, in basis points.</summary>
        public int FatiguePenaltyBp { get; }
        /// <summary>The effect on the other staff's morale.</summary>
        public int MoraleAura { get; }
        /// <summary>The effect on the satisfaction of the food they cook, in basis points.</summary>
        public int QualityBp { get; }
        /// <summary>The effect on the speed of clearing tables, in basis points.</summary>
        public int CleanlinessBp { get; }
        public bool PeakImmune { get; }
        public bool FatigueImmune { get; }
        /// <summary>The indices of the traits it cannot be held alongside.</summary>
        public int[] ConflictsWith { get; private set; }

        /// <summary>
        /// The conflict list is bound at load time: trait names have to be
        /// turned into indices first, and that can only be done once every
        /// trait has been read.
        /// </summary>
        public void BindConflicts(int[] indices)
        {
            ConflictsWith = indices ?? new int[0];
        }

        /// <summary>Can this trait be held alongside the given one.</summary>
        public bool ConflictsWithIndex(int other)
        {
            for (int i = 0; i < ConflictsWith.Length; i++)
                if (ConflictsWith[i] == other) return true;
            return false;
        }

        public TraitDef(string id, string nameKey, int speedBp, int satisfactionCenti,
                        int wageBp, int xpBp, int peakPenaltyBp, int fatiguePenaltyBp,
                        int moraleAura, bool peakImmune, bool fatigueImmune,
                        int qualityBp = 0, int cleanlinessBp = 0)
        {
            QualityBp = qualityBp;
            CleanlinessBp = cleanlinessBp;
            Id = id; NameKey = nameKey;
            SpeedBp = speedBp; SatisfactionCenti = satisfactionCenti;
            WageBp = wageBp; XpBp = xpBp;
            PeakPenaltyBp = peakPenaltyBp; FatiguePenaltyBp = fatiguePenaltyBp;
            MoraleAura = moraleAura;
            PeakImmune = peakImmune; FatigueImmune = fatigueImmune;
            ConflictsWith = new int[0];
        }
    }

    /// <summary>One expansion tier. Money is in centi-coins.</summary>
    public readonly struct TierConfig
    {
        public readonly int Tables;
        public readonly long Rent;        // weekly
        public readonly long Upgrade;     // the cost of moving up to this tier
        public readonly int StaffCap;

        /// <summary>
        /// The HIGHEST value reputation can reach at this tier, in
        /// centi-points.
        ///
        /// A four-table shop cannot be the restaurant the whole
        /// neighbourhood talks about. The ceiling stops reputation being a
        /// saturated axis: instead of topping out on day 25 and spending the
        /// remaining 35 days on a plateau, you are forced to grow.
        /// </summary>
        public readonly int ReputationCapCenti;

        /// <summary>
        /// The number of plates the restaurant OWNS at this tier.
        ///
        /// The plates are counted and they circulate: clean -> in use ->
        /// dirty -> clean. When the clean plates run out the cook cannot
        /// send out what has been cooked and service stops - this number is
        /// what makes docs/14's justification for the dishwasher ("if the
        /// plates run out, service stops") a real bottleneck.
        ///
        /// It grows with the tier: a shop that grows buys plates too. Had it
        /// not grown, a fourteen-table restaurant would be working with a
        /// four-table kitchen's plates and the bottleneck would be a wall
        /// rather than a mechanic.
        /// </summary>
        public readonly int Plates;

        public TierConfig(int tables, long rent, long upgrade, int staffCap,
                          int reputationCapCenti = 10000, int plates = 0)
        {
            Tables = tables; Rent = rent; Upgrade = upgrade; StaffCap = staffCap;
            ReputationCapCenti = reputationCapCenti;
            // Six per table when the content does not say: so that old
            // saves and tests do not lock up with zero plates.
            Plates = plates > 0 ? plates : tables * 6;
        }
    }

    /// <summary>
    /// Every economy constant the core needs.
    /// Lokanta.Content builds this from JSON; the core knows nothing of JSON.
    ///
    /// The staff are two pools:
    ///   kitchen  the cooks; the owner cannot enter (the player is the
    ///            owner, not the chef)
    ///   hall     waiter + dishwasher + cashier, a single work pool
    /// </summary>
    public sealed class EconomyConfig
    {
        public long StartingCash { get; private set; }
        public int StartingReputationCenti { get; private set; }
        public int CampaignDays { get; private set; }

        /// <summary>
        /// How many days a season is. docs/09: the sixty-day campaign is
        /// four seasons, so fifteen days each. Ingredient prices move with
        /// the season.
        /// </summary>
        public int SeasonDays { get; private set; }

        // The fields below were written down IN THE CONTENT but were HARD
        // CODED in the code. Check number three of tools/audit_content.py
        // found them: the DTO binds them and they never reach the core. The
        // values are the same today, so behaviour does not change; but when
        // a number is changed in the content, it now really does change.

        /// <summary>
        /// The length of the service day, in milliseconds. docs/23 1.3:
        /// 480,000 ms, that is 4,800 ticks. TimingConfig uses this.
        /// </summary>
        public int ServiceMs { get; private set; }

        /// <summary>
        /// The owner's allowance of interventions per day. docs/02 59: "you
        /// have a limited number of owner interventions (3-5 a day)".
        ///
        /// It was written in the content and nothing was enforcing it: the
        /// player could intervene without limit, so every angry customer
        /// could be saved for free.
        /// </summary>
        public int InterventionsPerDay { get; private set; }

        /// <summary>
        /// The cost of the complimentary tea PER HEAD, in centi-coins.
        /// docs/12 3: "2 in cost per portion, given away free".
        /// </summary>
        public long TreatCost { get; private set; }

        /// <summary>
        /// The daily swing of market prices, in basis points. 2500 = 25%.
        /// docs/12 3: "catching the cheap day is not luck, it is a matter of
        /// paying attention".
        /// </summary>
        public int PriceVolatilityBp { get; private set; }

        /// <summary>How many days apart rent and wages are paid. docs/12 2.</summary>
        public int RentDayInterval { get; private set; }

        /// <summary>
        /// Satisfaction's neutral threshold, in centi-points. Above it earns
        /// reputation, below it loses reputation. docs/12 5.5.
        /// </summary>
        public int SatisfactionNeutralCenti { get; private set; }

        /// <summary>
        /// There is no benefit in cutting the price below this ratio of the
        /// market, in basis points. 8500 = going more than 15% below gains
        /// nothing.
        /// </summary>
        public int UnderpriceFloorBp { get; private set; }

        /// <summary>
        /// The effect of price deviation on demand. 10000 = unit elasticity
        /// (a 10% rise -> 10% fewer customers).
        ///
        /// This channel once DID NOT EXIST and it was the game's biggest
        /// hole: price's only route was satisfaction -> reputation, and
        /// reputation was clamped to the tier ceiling, so for a player at
        /// that ceiling a small rise was free. Measured: a bot raising
        /// prices by 10% finished on 27,849 coins, the baseline strategy on
        /// 18,670.
        ///
        /// It comes FROM THE CONTENT so that the balance tool can search
        /// over it; had it been a hand-written constant, calibrate.py could
        /// never have seen this axis at all.
        /// </summary>
        public int PriceElasticityBp { get; private set; }

        /// <summary>
        /// How far the day's demand may stray FROM THE EXPECTATION, in basis
        /// points. 1000 = between -10% and +10% on the day.
        ///
        /// Demand was once COMPLETELY deterministic: every Tuesday at the
        /// same reputation and table count brought in exactly the same
        /// number of customers. The consequence was that the morning's stock
        /// decision was a button rather than a JUDGEMENT - the market's
        /// recommendation was always exactly right and the line "Stock is
        /// enough for 8/13 people" never turned red.
        ///
        /// THE VARIANCE IS ONLY IN THE REALISED NUMBER. The forecasts (the
        /// crew recommendation, the market recommendation, the expected
        /// covers) go on showing the expectation - otherwise the player
        /// would once again hold certain knowledge and the volatility would
        /// stay decoration.
        /// </summary>
        public int DemandVarianceBp { get; private set; }

        /// <summary>
        /// When the owner sees to a table, how much that table's NEXT piece
        /// of hall work is shortened, in basis points. 5000 = by half.
        ///
        /// When the intervention was measured it came out NEUTRAL: in a
        /// properly staffed restaurant a crisis hardly ever happens (0.8
        /// interventions a day), so the mechanic was a safety net - whereas
        /// the store text sells it as a core mechanic.
        ///
        /// What was missing was the hall side: attention lengthened patience
        /// and sped up THE KITCHEN, but the bottleneck is usually in the
        /// hall. "The owner is seeing to it personally" means exactly that
        /// they take the order or the payment - the table turns over faster,
        /// so more customers on the same day. That way the intervention
        /// produces value without waiting for a crisis.
        /// </summary>
        public int AttendWorkCutBp { get; private set; }

        /// <summary>
        /// The CEILING on price relative to the market, in basis points.
        /// 25000 = nothing may be sold for more than 2.5 times the market.
        ///
        /// WHY IT EXISTS: satisfaction is clamped to [0, 10000] and demand
        /// never sees the price at all. So past the point where an item's
        /// price is enough to drive the satisfaction of whoever buys it down
        /// to zero, EVERY FURTHER ZERO IS FREE. Measured: a bot that made
        /// the side items 2000 times dearer piled up 3.4 million coins while
        /// reputation, satisfaction and the number of people served did NOT
        /// change at all - past that point the simulation was not seeing the
        /// price.
        ///
        /// The floor was already there (UnderpriceFloorBp); the absence of a
        /// ceiling was an error of symmetry.
        /// </summary>
        public int OverpriceCeilingBp { get; private set; }

        /// <summary>What multiple of the principal is repaid, in basis points.</summary>
        public int LoanMultiplierBp { get; private set; }

        /// <summary>The number of loan instalments, in weeks.</summary>
        public int LoanWeeks { get; private set; }

        /// <summary>The loan options, in centi-coins.</summary>
        public long[] LoanOptions { get; private set; }
        public int WeekendDaysPerWeek { get; private set; }

        public int CustomerBasePerTable { get; private set; }
        public int WeekdayMultiplierBp { get; private set; }
        public int WeekendMultiplierBp { get; private set; }

        public int IngredientRateBp { get; private set; }

        public int CookCapacityPerDay { get; private set; }
        public long CookDailyWage { get; private set; }

        /// <summary>The work one customer loads onto the hall, in micro person-days.</summary>
        public int HallWorkPerCustomerMicro { get; private set; }

        /// <summary>
        /// Sum(workMicro_r * dailyWage_r) over the hall roles.
        /// The numerator is kept apart so the per-head wage is not rounded
        /// in advance; the rounding happens once, when the wage bill is
        /// worked out.
        /// </summary>
        public long HallWageNumerator { get; private set; }

        public int OwnerWorkMicro { get; private set; }
        public int WeeklyXpWageGrowthBp { get; private set; }

        // --- Experience, docs/14 "Experience and level" -----------------------
        // 1 point for every day worked, a level at 30 points, 3 levels at
        // most, +10% speed per level. The speed ladder comes from the content.
        /// <summary>The days worked that one level requires. docs/14: 30.</summary>
        // --- Staff traits and morale, docs/14 ---------------------------------
        private TraitDef[] _traits;

        public int TraitCount { get { return _traits == null ? 0 : _traits.Length; } }
        public TraitDef TraitAt(int i) { return _traits[i]; }

        /// <summary>A new hire's morale. docs/14: it starts at 70.</summary>
        public int StartingMorale { get; private set; }
        /// <summary>Below this, speed drops and the chance of a mistake rises.</summary>
        public int MoraleLowThreshold { get; private set; }
        /// <summary>Below this, there is a risk of resignation every day.</summary>
        public int MoraleQuitThreshold { get; private set; }
        /// <summary>The risk of resignation, in basis points. docs/14: 10% a day.</summary>
        public int MoraleQuitChanceBp { get; private set; }
        /// <summary>The speed penalty of low morale, in basis points. docs/14: 20%.</summary>
        public int MoraleSlowPenaltyBp { get; private set; }
        /// <summary>The wage was paid on time.</summary>
        public int MoralePaidDelta { get; private set; }
        /// <summary>The wage was late.</summary>
        public int MoraleLateDelta { get; private set; }
        /// <summary>A busy day on top of another.</summary>
        public int MoraleBusyDelta { get; private set; }
        /// <summary>A quiet day's recovery; back towards the starting morale.</summary>
        public int MoraleRecoveryDelta { get; private set; }
        /// <summary>How many days apart the candidate pool refreshes. docs/14: three.</summary>
        public int CandidateRefreshDays { get; private set; }

        /// <summary>
        /// The reputation cost of written-off debt, in centi-points. The last
        /// rung of the ladder: once both the equipment and the tables have
        /// been sold, the remaining debt is written off and the price is
        /// taken out of reputation.
        /// </summary>
        public int DebtWriteOffRepCenti { get; private set; }

        /// <summary>
        /// The intervention's contribution to satisfaction, in centi-points.
        /// docs/12 5.4.
        ///
        /// The real effect is now the extension of PATIENCE; these two
        /// numbers are only a small touch on top. Because it used to be the
        /// other way round, an intervention was a measurable loss.
        /// </summary>
        public int AttentionSatisfactionCenti { get; private set; }
        public int TreatSatisfactionCenti { get; private set; }

        /// <summary>The share wiped off the remaining time of a job that is hurried along, in basis points.</summary>
        public int RushCutBp { get; private set; }

        /// <summary>
        /// The patience extension, as a multiple of the seat-and-order time.
        ///
        /// It sets up a TRADE-OFF: the extension holds on to a party that
        /// was about to leave, but it also occupies the table for longer, so
        /// somebody else cannot be served. It is not a free favour.
        /// </summary>
        public int AttentionPatienceMult { get; private set; }
        public int TreatPatienceMult { get; private set; }

        /// <summary>
        /// The intervention numbers. They come from the content (docs/23 8.2).
        ///
        /// Any field that arrives as zero falls back to its value in the old
        /// code: so that an old save or a missing file does not silently
        /// render the intervention ineffective.
        /// </summary>
        public EconomyConfig WithIntervention(int attentionCenti, int treatCenti,
                                              int rushCutBp, int attentionMult,
                                              int treatMult)
        {
            EconomyConfig c = (EconomyConfig)MemberwiseClone();
            c.AttentionSatisfactionCenti = attentionCenti > 0 ? attentionCenti : 1200;
            c.TreatSatisfactionCenti = treatCenti > 0 ? treatCenti : 900;
            c.RushCutBp = rushCutBp > 0 ? rushCutBp : 4000;
            c.AttentionPatienceMult = attentionMult > 0 ? attentionMult : 3;
            c.TreatPatienceMult = treatMult > 0 ? treatMult : 1;
            return c;
        }

        /// <summary>
        /// A copy with its tiers replaced.
        ///
        /// FOR THE TESTS: testing how the reputation ceiling behaves needs a
        /// shop that is PRESSED UP against that ceiling. In the real content
        /// the ceiling at four tables is 55 and a small shop's natural
        /// equilibrium is ~34.6 - so the ceiling binds nothing there. To
        /// reach the ceiling the test would have to play as well as the
        /// harness bot, that is, to write a bot inside the test.
        ///
        /// Lowering the ceiling FROM THE CONTENT is the right answer: the
        /// rule is the same rule, only the threshold at which it becomes
        /// visible is brought closer.
        /// </summary>
        /// <summary>
        /// CHANGES THE HALL POOL ACCORDING TO THE CUISINE.
        ///
        /// The hall workload and wage used to be the sum of ALL the hall
        /// roles in economy.json - waiter + dishwasher + cashier. But fast
        /// food is SELF SERVICE: no waiter comes to the table, so the player
        /// was paying the wage of a waiter who did no work, and the staffing
        /// model was asking for heads on that basis.
        ///
        /// The cuisine's own role list (cuisines/*.json: salonRoles) is
        /// applied from here. If there is no list, nothing changes.
        /// </summary>
        public EconomyConfig WithHallPool(int workPerCustomerMicro,
                                           long wageNumerator)
        {
            if (workPerCustomerMicro <= 0) return this;
            EconomyConfig c = (EconomyConfig)MemberwiseClone();
            c.HallWorkPerCustomerMicro = workPerCustomerMicro;
            c.HallWageNumerator = wageNumerator;
            return c;
        }

        public EconomyConfig WithTiers(TierConfig[] tiers)
        {
            if (tiers == null || tiers.Length == 0) return this;
            EconomyConfig c = (EconomyConfig)MemberwiseClone();
            c._tiers = tiers;
            return c;
        }

        public EconomyConfig WithTraits(TraitDef[] traits)
        {
            EconomyConfig c = (EconomyConfig)MemberwiseClone();
            c._traits = traits;
            return c;
        }

        public EconomyConfig WithMorale(int starting, int low, int quit, int quitChanceBp,
                                        int slowPenaltyBp, int paid, int late, int busy,
                                        int recovery)
        {
            EconomyConfig c = (EconomyConfig)MemberwiseClone();
            c.StartingMorale = starting;
            c.MoraleLowThreshold = low;
            c.MoraleQuitThreshold = quit;
            c.MoraleQuitChanceBp = quitChanceBp;
            c.MoraleSlowPenaltyBp = slowPenaltyBp;
            c.MoralePaidDelta = paid;
            c.MoraleLateDelta = late;
            c.MoraleBusyDelta = busy;
            c.MoraleRecoveryDelta = recovery > 0 ? recovery : 2;
            c.CandidateRefreshDays = 3;
            c.DebtWriteOffRepCenti = 2000;
            return c;
        }

        // --- Named regulars, docs/11 ------------------------------------------
        /// <summary>The chance of dropping in on a given day once you have met, in basis points.</summary>
        public int RegularVisitChanceBp { get; private set; }
        /// <summary>The penalty when a regular cannot find their favourite dish, in centi.</summary>
        public int RegularMissedFavouriteCenti { get; private set; }
        /// <summary>If they leave below this they stay away for a while, in centi-points.</summary>
        public int RegularUpsetCenti { get; private set; }
        /// <summary>How many days they stay away once they are upset.</summary>
        public int RegularAwayDays { get; private set; }

        /// <summary>Fits the regulars' settings. They come from the content.</summary>
        public EconomyConfig WithRegulars(int visitChanceBp, int missedFavouriteCenti,
                                          int upsetCenti, int awayDays)
        {
            EconomyConfig c = (EconomyConfig)MemberwiseClone();
            c.RegularVisitChanceBp = visitChanceBp > 0 ? visitChanceBp : 4500;
            c.RegularMissedFavouriteCenti = missedFavouriteCenti;
            c.RegularUpsetCenti = upsetCenti;
            c.RegularAwayDays = awayDays > 0 ? awayDays : 3;
            return c;
        }

        public int XpDaysPerLevel { get; private set; }
        /// <summary>The highest level. docs/14: 3.</summary>
        public int MaxXpLevel { get; private set; }

        private int[] _cookXpSpeedBp;
        private int[] _hallXpSpeedBp;

        /// <summary>
        /// The level's speed multiplier, in basis points. If the ladder is
        /// short the last rung repeats; the content should always be
        /// MaxXpLevel+1 long, but we do not want to run off the end of the
        /// array because of a ladder with a rung missing.
        /// </summary>
        public int XpSpeedBp(int level, bool kitchen)
        {
            int[] ladder = kitchen ? _cookXpSpeedBp : _hallXpSpeedBp;
            if (ladder == null || ladder.Length == 0) return 10_000;
            if (level < 0) level = 0;
            if (level >= ladder.Length) level = ladder.Length - 1;
            return ladder[level];
        }

        /// <summary>The level from the number of days worked. docs/14: a cap of 3.</summary>
        public int XpLevelOf(int daysWorked)
        {
            if (XpDaysPerLevel <= 0) return 0;
            int level = daysWorked / XpDaysPerLevel;
            return level > MaxXpLevel ? MaxXpLevel : level;
        }

        /// <summary>
        /// Fits the experience ladders. Rather than adding two more arguments
        /// to constructors that already take forty, we return a copy: getting
        /// the order wrong in that list is a kind of mistake the compiler
        /// cannot catch.
        /// </summary>
        public EconomyConfig WithXpSpeed(int[] cookLadder, int[] hallLadder,
                                         int daysPerLevel, int maxLevel)
        {
            EconomyConfig c = (EconomyConfig)MemberwiseClone();
            c._cookXpSpeedBp = cookLadder;
            c._hallXpSpeedBp = hallLadder;
            c.XpDaysPerLevel = daysPerLevel > 0 ? daysPerLevel : 30;
            c.MaxXpLevel = maxLevel > 0 ? maxLevel : 3;
            return c;
        }

        /// <summary>Neglected, reputation erodes. docs/12 5.5: 0.3 points a day.</summary>
        public int ReputationDecayPerDayCenti { get; private set; }

        // --- The ordering model -----------------------------------------------
        // One MAIN dish per head is certain; a side and a drink are by
        // chance. Without these the balance tool was computing the average
        // ticket far too low: every customer picked a single item from the
        // menu with equal probability, and most of them took a cola. This is
        // the base state of the combo mechanic of docs/07.
        /// <summary>The chance of a side dish per head, in basis points.</summary>
        public int SideChanceBp { get; private set; }
        /// <summary>The chance of a drink per head, in basis points.</summary>
        public int DrinkChanceBp { get; private set; }
        /// <summary>
        /// The chance of a dessert per head, in basis points. Lower than the
        /// drink: dessert comes at the end and not everybody takes one.
        /// </summary>
        public int DessertChanceBp { get; private set; }

        /// <summary>
        /// The chance that a customer ASKS for a dish they have heard of but
        /// which cannot be made.
        /// </summary>
        public int AskChanceBp { get; private set; }

        /// <summary>
        /// The satisfaction a customer loses when the dish they asked for is
        /// not to be had, in centi-points.
        /// </summary>
        public int AskMissCenti { get; private set; }

        /// <summary>
        /// How much of demand ACTUALLY turns into revenue, in basis points.
        ///
        /// The closed-form model used to assume all of demand was served.
        /// On the same expansion timetable the simulation produces 65% of
        /// the model's revenue: customers who run out of patience, stock
        /// that runs out, tables that fill up. Because the rents were solved
        /// from the model's revenue, they were 35% too high.
        ///
        /// It is a MEASURED value, not a chosen one. It must be measured
        /// again whenever the simulation changes.
        /// </summary>
        public int RealisationBp { get; private set; }

        private TierConfig[] _tiers;

        public EconomyConfig(
            long startingCash, int startingReputationCenti, int campaignDays,
            int weekendDaysPerWeek, int customerBasePerTable,
            int weekdayMultiplierBp, int weekendMultiplierBp, int ingredientRateBp,
            int cookCapacityPerDay, long cookDailyWage,
            int hallWorkPerCustomerMicro, long hallWageNumerator,
            int ownerWorkMicro, int weeklyXpWageGrowthBp, TierConfig[] tiers,
            int reputationDecayPerDayCenti = 30,
            int seasonDays = 15, int serviceMs = 480_000, int rentDayInterval = 7,
            int interventionsPerDay = 4, long treatCost = 200,
            int priceVolatilityBp = 2500,
            int satisfactionNeutralCenti = 6000, int underpriceFloorBp = 8500,
            int loanMultiplierBp = 13500, int loanWeeks = 8,
            long[] loanOptions = null, int sideChanceBp = 3000, int drinkChanceBp = 4000,
            int dessertChanceBp = 1800, int askChanceBp = 2500,
            int askMissCenti = 1500, int realisationBp = 10000,
            int overpriceCeilingBp = 25000,
            int priceElasticityBp = 9000,
            int demandVarianceBp = 0,
            int attendWorkCutBp = 0)
        {
            if (tiers == null || tiers.Length == 0)
                throw new ArgumentException("At least one tier is required", nameof(tiers));
            if (customerBasePerTable <= 0)
                throw new ArgumentOutOfRangeException(nameof(customerBasePerTable));
            if (cookCapacityPerDay <= 0)
                throw new ArgumentOutOfRangeException(nameof(cookCapacityPerDay));
            if (hallWorkPerCustomerMicro <= 0)
                throw new ArgumentOutOfRangeException(nameof(hallWorkPerCustomerMicro));

            StartingCash = startingCash;
            StartingReputationCenti = startingReputationCenti;
            CampaignDays = campaignDays;
            SeasonDays = seasonDays > 0 ? seasonDays : 15;
            ServiceMs = serviceMs > 0 ? serviceMs : 480_000;
            InterventionsPerDay = interventionsPerDay > 0 ? interventionsPerDay : 4;
            TreatCost = treatCost > 0 ? treatCost : 200;
            PriceVolatilityBp = priceVolatilityBp >= 0 ? priceVolatilityBp : 2500;
            RentDayInterval = rentDayInterval > 0 ? rentDayInterval : 7;
            SatisfactionNeutralCenti =
                satisfactionNeutralCenti > 0 ? satisfactionNeutralCenti : 6000;
            UnderpriceFloorBp = underpriceFloorBp > 0 ? underpriceFloorBp : 8500;
            PriceElasticityBp = priceElasticityBp > 0 ? priceElasticityBp : 9000;
            DemandVarianceBp = demandVarianceBp < 0 ? 0 : demandVarianceBp;
            AttendWorkCutBp = attendWorkCutBp < 0 ? 0 : attendWorkCutBp;
            OverpriceCeilingBp =
                overpriceCeilingBp > Fx.One ? overpriceCeilingBp : 25000;
            LoanMultiplierBp = loanMultiplierBp > 0 ? loanMultiplierBp : 13500;
            LoanWeeks = loanWeeks > 0 ? loanWeeks : 8;
            LoanOptions = loanOptions != null && loanOptions.Length > 0
                ? loanOptions
                : new long[] { 500_000, 1_000_000, 2_000_000 };
            WeekendDaysPerWeek = weekendDaysPerWeek;
            CustomerBasePerTable = customerBasePerTable;
            WeekdayMultiplierBp = weekdayMultiplierBp;
            WeekendMultiplierBp = weekendMultiplierBp;
            IngredientRateBp = ingredientRateBp;
            CookCapacityPerDay = cookCapacityPerDay;
            CookDailyWage = cookDailyWage;
            HallWorkPerCustomerMicro = hallWorkPerCustomerMicro;
            HallWageNumerator = hallWageNumerator;
            OwnerWorkMicro = ownerWorkMicro;
            WeeklyXpWageGrowthBp = weeklyXpWageGrowthBp;
            // The default: no experience. The content fits it with WithXpSpeed.
            XpDaysPerLevel = 30;
            MaxXpLevel = 3;
            StartingMorale = 70;
            MoraleLowThreshold = 30;
            MoraleQuitThreshold = 15;
            MoraleQuitChanceBp = 1000;
            MoraleSlowPenaltyBp = 2000;
            MoralePaidDelta = 5;
            MoraleLateDelta = -25;
            MoraleBusyDelta = -3;
            MoraleRecoveryDelta = 2;
            CandidateRefreshDays = 3;
            DebtWriteOffRepCenti = 2000;
            AttentionSatisfactionCenti = 1200;
            TreatSatisfactionCenti = 900;
            RegularVisitChanceBp = 4500;
            RegularMissedFavouriteCenti = 900;
            RegularUpsetCenti = 5000;
            RegularAwayDays = 3;
            _cookXpSpeedBp = null;
            _hallXpSpeedBp = null;
            ReputationDecayPerDayCenti = reputationDecayPerDayCenti;
            SideChanceBp = sideChanceBp;
            DrinkChanceBp = drinkChanceBp;
            DessertChanceBp = dessertChanceBp;
            AskChanceBp = askChanceBp;
            AskMissCenti = askMissCenti;
            RealisationBp = realisationBp;
            _tiers = tiers;
        }

        public int TierCount { get { return _tiers.Length; } }

        public TierConfig TierAt(int index) { return _tiers[index]; }

        /// <summary>Is there a tier that matches this table count.</summary>
        public bool HasTierForTables(int tables)
        {
            for (int i = 0; i < _tiers.Length; i++)
                if (_tiers[i].Tables == tables) return true;
            return false;
        }

        /// <summary>
        /// The tier for a table count. With no exact match, the NEAREST TIER
        /// BELOW.
        ///
        /// It used to throw, and that was in the wrong place: a reader
        /// should never bring things down. An invalid table count coming
        /// from a corrupt save killed the game the moment the player opened
        /// the Staff screen, and made the save unusable. A save's validity
        /// is checked AT LOAD (Simulation.Validate); this place is only
        /// obliged to give a sensible answer.
        /// </summary>
        public TierConfig TierForTables(int tables)
        {
            TierConfig best = _tiers[0];
            for (int i = 0; i < _tiers.Length; i++)
            {
                if (_tiers[i].Tables == tables) return _tiers[i];
                if (_tiers[i].Tables <= tables) best = _tiers[i];
            }
            return best;
        }
    }
}
