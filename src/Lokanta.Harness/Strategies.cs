using Lokanta.Core.Economy;
using Lokanta.Core.Sim;

namespace Lokanta.Harness
{
    /// <summary>
    /// The player strategies the balance harness plays.
    /// docs/12-economy.md 8: the questions the tool has to answer are measured
    /// with these.
    /// </summary>
    public interface IStrategy
    {
        string Name { get; }
        string Question { get; }
        void OnMorning(Simulation sim);
        void OnEvening(Simulation sim, in DayReport report);

        /// <summary>
        /// During service, once every fifty ticks. The signature mechanics run
        /// here: the tab opens at the moment of payment, the combo at the moment
        /// of ordering.
        ///
        /// ON THE INTERFACE, with an EMPTY default body. It was not like this
        /// before: Program.cs dispatched through a chain of type checks
        /// ("strategy is SignaturePlayer sp") and a new strategy that was not in
        /// the chain SILENTLY did nothing - the bot looked like it was running
        /// and returned an empty measurement. That is exactly what happened when
        /// `erken_tahsilat` was added: its result came out BYTE FOR BYTE
        /// identical to `makul`.
        ///
        /// Thanks to the default body a new strategy now takes part simply by
        /// writing the method; there is no registration point left to forget.
        /// </summary>
        void DuringService(Simulation sim) { }
    }

    /// <summary>
    /// The player who never intervenes. Does not go to the market, does not
    /// hire, does not expand.
    /// Question 1: on which day do they go under.
    /// </summary>
    public sealed class PassivePlayer : IStrategy
    {
        public string Name => "pasif";
        public string Question => "On which day does a player who never intervenes go under";
        public void OnMorning(Simulation sim) { }
        public void OnEvening(Simulation sim, in DayReport report) { }
    }

    /// <summary>The player who goes to the market every morning and does nothing else.</summary>
    /// <summary>
    /// The equipment buying rule. It looks at CAPACITY, not at angry customers:
    /// if a station lags behind the tier the table count demands and the till
    /// carries twice the price, it is bought.
    ///
    /// A note on the same mistake being made in the crew decision, which the
    /// balance harness caught: hiring off the wrong signal dropped wages from
    /// 19,647 to 10,446.
    /// </summary>
    /// <summary>
    /// The hiring choice. docs/14: three candidates are shown, the player picks.
    ///
    /// This class's reason to exist was measured: WITHOUT a candidate pool a
    /// trait was a lottery, and drawing an expensive crew took the good player's
    /// reputation from 96.5 down to 87. The choice was the missing half of the
    /// mechanic.
    ///
    /// The bot's choice is simple and defensible: SPEED MINUS WAGE. A real
    /// player looks at those two numbers too; which of them they weight is up
    /// to them.
    /// </summary>
    public static class Hiring
    {
        public static int Pick(Simulation sim, int pool)
        {
            int best = 0, bestScore = int.MinValue;
            for (int i = 0; i < Simulation.CandidateSlots; i++)
            {
                int score = sim.CandidateScore(pool, i);
                if (score > bestScore) { bestScore = score; best = i; }
            }
            return best;
        }

        /// <summary>
        /// Replaces a bad member of staff with a clearly better candidate.
        /// Returns true if it did so once.
        ///
        /// This was the missing half of the trait system. The trait arrived at
        /// random and the bot COULD NOT REACT TO IT: a surly waiter wrote -6
        /// points on every table for sixty days, satisfaction leaked from 9,000
        /// to 5,000, the place stayed at four tables, and the run was lost to a
        /// single roll of the dice.
        ///
        /// The threshold is WIDE (3,000 points) and the swap COSTS something:
        /// the departing person's experience is reset. So it is not "look for
        /// the best every day" but "replace them if they really are bad".
        /// </summary>
        public static bool ReplaceWorst(Simulation sim, int pool)
        {
            int count = pool == 0 ? sim.Cooks : sim.HallStaff;
            if (count == 0) return false;

            int worst = -1, worstScore = int.MaxValue;
            for (int i = 0; i < count; i++)
            {
                int score = sim.StaffTraitScore(pool, i);
                if (score < worstScore) { worstScore = score; worst = i; }
            }
            if (worst < 0) return false;

            int cand = Pick(sim, pool);
            int candScore = sim.CandidateScore(pool, cand);
            if (candScore == int.MinValue) return false;
            // The threshold is 1,200. The first value was 3,000 and the total
            // spread of the trait scores is ~4,700; so the replacement almost
            // never fired and the mechanic remained a dice roll rather than a
            // decision. 1,200 catches the difference between "a surly waiter"
            // and "a waiter you get on with".
            if (candScore - worstScore < 1200) return false;

            // It removes the WORST person. The command now takes an index; sent
            // without one it always removed the last person, and the "replace
            // the worst" strategy was really "replace the last one".
            sim.Apply(new Command(sim.TickIndex, CommandKind.Fire, pool, worst));
            sim.Apply(new Command(sim.TickIndex, CommandKind.Hire, pool, cand));
            return true;
        }
    }

    public static class Equipment
    {
        /// <summary>
        /// The HIGHEST cold storage tier the bot may climb to. For measurement.
        ///
        /// Whether each rung of the ladder pays for itself can only be measured
        /// by closing the rungs ONE BY ONE. This used to be done by writing
        /// unreachable prices into content/equipment.json, and twice it left the
        /// content patched: if the measuring run is killed the undo does not
        /// happen, and the next calibration runs with a 99,000,000-coin storage.
        ///
        /// A flag measures the same thing without touching the content at all.
        /// </summary>
        public static int StorageCap = int.MaxValue;

        public static void Upgrade(Simulation sim, long cashMultiple = 2)
        {
            // THE WEEKLY PAYMENT IS PROTECTED. Buying equipment must not eat the
            // rent and the wages.
            //
            // This line was added after a measurement: when named equipment
            // arrived, the planner strategy dropped from 19,649 to 330 on day
            // 56, could not buy stock the next morning, its service fell to zero
            // and its reputation collapsed from 100 to 31 in three days. A real
            // player does not buy a waffle iron with payday coming up.
            long safety = sim.WeeklyFixedCost();
            // First the MANDATORY thing: the slot the table count demands.
            for (int st = 0; st < sim.StationCount; st++)
            {
                if (sim.StationTier(st) >= sim.RequiredStationTier(st)) continue;
                long price = sim.NextEquipmentPrice(st);
                if (price < 0 || sim.Cash < price * cashMultiple) continue;
                if (sim.Cash - price < safety) continue;
                sim.Apply(new Command(sim.TickIndex, CommandKind.BuyEquipment, st));
                return;      // one piece of equipment a day; do not empty the till in one morning
            }

            // Then the OPTIONAL things: upgrades that free the cook earlier, and
            // cold storage. Only while the till is comfortable, because these are
            // not mandatory. The game's goal of "even in the eighth week you are
            // saving up for something" (docs/12 7) is fed from here.
            // No optional upgrade while there is a loan - with ONE EXCEPTION:
            // named equipment opens up the MENU and customers actively ASK for
            // those dishes (docs/34 6). Buying it once the loan is paid off means
            // spending half of the sixty days answering "we haven't got any".
            //
            // Measured: when doner and pide arrived in the Turkish cuisine the
            // non-expanding strategy's reputation fell from 85.1 to 50.7, because
            // it had a loan and could NEVER buy the grill; every day it said no
            // to a dish people asked for. It was the bot that was wrong, not the
            // economy.
            bool tightBudget = sim.HasLoan;

            // Cold storage first: it opens up menu width, so it feeds both the
            // average ticket and satisfaction. Wider in effect than any single
            // station slot.
            // Cold storage can be bought with a loan outstanding too, but keeping
            // two weeks of costs in hand. Banning it outright was measured and it
            // was expensive: when the trait wages arrived the budget wobbled, the
            // loan was taken earlier, and with the loan locking out cold storage
            // the menu narrowed and satisfaction fell. Cold storage is an
            // investment that pays for itself - with a loan outstanding it should
            // not be BANNED, it should be bought CAREFULLY.
            long cold = sim.StorageTier >= StorageCap ? -1 : sim.NextStoragePrice();
            long coldSafety = tightBudget ? safety * 2 : safety;
            if (cold >= 0 && sim.Cash >= cold * (cashMultiple + 2)
                && sim.Cash - cold >= coldSafety)
            {
                sim.Apply(new Command(sim.TickIndex, CommandKind.BuyStorage));
                return;
            }

            for (int st = 0; st < sim.StationCount; st++)
            {
                long price = sim.NextEquipmentPrice(st);
                if (price < 0 || sim.Cash < price * (cashMultiple + 2)) continue;
                if (sim.Cash - price < safety) continue;
                // With a loan outstanding, ONLY equipment that opens up the menu,
                // and keeping two weeks of fixed costs in hand. One week is not
                // enough: the planner strategy was missing its expansion schedule
                // (12.5 tables against a target of 13) because it put the money
                // into a grill and delayed the tier.
                if (tightBudget && !sim.IsCuisineStation(st)) continue;
                if (tightBudget && sim.Cash - price < safety * 2) continue;

                // Named equipment (stone oven, doner grill, pide oven) opens up
                // the MENU, not capacity. A restaurant sitting at four tables has
                // nowhere to open up to: widening the menu splits the stock and
                // turns the money from an investment into an EXPENSE.
                //
                // Measured: when doner and pide arrived in the Turkish cuisine
                // the non-expanding strategy put 10,200 coins into three grills,
                // its end cash fell to 8,748 and the growth multiplier rose to
                // 4.14 - which made it look as though "expanding is rewarded too
                // much". It was the BOT that was wrong, not the economy: a real
                // player does not buy a third oven for four tables.
                if (sim.IsCuisineStation(st) && sim.TableCount <= 4
                    && price * 6 > sim.Cash) continue;


                sim.Apply(new Command(sim.TickIndex, CommandKind.BuyEquipment, st));
                return;
            }
        }
    }

    public sealed class RestockOnly : IStrategy
    {
        public string Name => "sadece_hal";
        public string Question => "What does a player who only buys stock and does nothing else earn";

        public void OnMorning(Simulation sim) { Restock(sim); }
        public void OnEvening(Simulation sim, in DayReport report) { }

        /// <summary>Orders the recommended stock. If the till is short it stays partial.</summary>
        public static void Restock(Simulation sim)
        {
            Restock(sim, false);
        }

        /// <summary>
        /// Buys stock at the market in the morning.
        ///
        /// stockAhead: if there is cold storage and the stock is cheap TODAY, it
        /// buys extra. Cheapness comes from two sources: the season (slow,
        /// predictable) and the market's daily swing (fast, unpredictable).
        /// Neither is a decision on its own, only a swing in an expense; for it
        /// to be a decision you have to be able to buy cheap and keep it. The
        /// cold storage ladder sells exactly that - docs/12 3.
        /// </summary>
        public static void Restock(Simulation sim, bool stockAhead)
        {
            for (int i = 0; i < sim.IngredientCount; i++)
            {
                int need = sim.RecommendedRestock(i);
                if (need <= 0) continue;

                if (stockAhead && sim.CanKeep(i))
                {
                    // If today's price is below the year's average, stock up.
                    // The average does not include the market's swing (the mean
                    // of the swing is 1.0), so the comparison measures the sum
                    // of season + market.
                    long today = sim.IngredientPriceToday(i);
                    long mean = sim.IngredientPriceMean(i);
                    if (mean > 0 && today * 100 < mean * 92)
                    {
                        // The cap comes from MaxUsefulDays, not from a
                        // hand-written number: with the interface about to tell
                        // the player "three days at most", a bot buying four
                        // days' worth would mean the tool was measuring a
                        // strategy the player cannot see.
                        int days = sim.KeepDays(i);
                        int cap = sim.MaxUsefulDays(i);
                        if (days > cap) days = cap;
                        if (days > 1) need *= days;
                    }
                }

                sim.Apply(new Command(sim.TickIndex, CommandKind.OrderIngredient, i, need));
            }
        }
    }

    /// <summary>
    /// The reasonable player: hires when they see angry customers, expands if
    /// the till allows and the reputation carries it. Question 2: what net worth
    /// do they end the year with.
    /// </summary>
    public sealed class ReasonablePlayer : IStrategy
    {
        private readonly bool _expand;
        private int _lastServiceRateBp = 10_000;

        /// <summary>How many days the hall crew has been over strength. Lets one go on the third.</summary>
        private int _surplusDays;

        /// <summary>
        /// Whether taking a loan is allowed. For MEASUREMENT only: the no-loan
        /// strategy switches this off and runs the same player, so that the loan
        /// is the single variable.
        /// </summary>
        public static bool AllowLoan = true;

        /// <summary>
        /// HOW MANY PEOPLE short of what the capacity model asks for the hall
        /// crew will be kept. For MEASUREMENT only.
        ///
        /// Why it exists: the value of an intervention cannot be measured in a
        /// comfortable restaurant. Measured - the interventionist bot got 1440
        /// of 1440 interventions through and served the SAME number of people as
        /// the reasonable player. The reason is the ceiling: in a well run place
        /// about 0.3 parties a day are lost, so there is nothing to save.
        ///
        /// This arm runs the same player one waiter short; then the number of
        /// parties lost becomes meaningful and the question "does intervening
        /// work" can really be asked.
        /// </summary>
        public static int HallShort = 0;

        public ReasonablePlayer(bool expand = true) { _expand = expand; }

        public string Name => _expand ? "makul" : "genislemeyen";
        public string Question => _expand
            ? "What net worth does a player who plays well end the year with"
            : "How much does a player who never expands earn";

        public void OnMorning(Simulation sim)
        {
            // Narrow the menu to the demand FIRST, THEN go to the market.
            //
            // Carrying a wide menu is expensive: for every dish on the menu you
            // have to stock enough for one party, and the perishables reset every
            // day. A twelve main course menu bankrupts a place with thirteen
            // customers a day. That is exactly where the tension of the market
            // phase comes from.
            NarrowMenu(sim);

            // If the till cannot cover two weeks of fixed costs, take a loan.
            // docs/12 4 exists for this situation. A player without a loan, once
            // emptied out, cannot buy stock and cannot recover.
            if (AllowLoan && !sim.HasLoan && sim.Cash < sim.WeeklyFixedCost() * 2)
                sim.Apply(new Command(sim.TickIndex, CommandKind.TakeLoan, 0));

            RestockOnly.Restock(sim, stockAhead: true);

            // The equipment comes BEFORE the expansion. A restaurant whose tables
            // do not fill up should buy equipment for a kitchen that cannot keep
            // up, not new tables.
            Equipment.Upgrade(sim);

            if (!_expand) return;

            // If they can afford the next tier and the reputation is good enough,
            // expand. Do not empty the till completely.
            for (int t = 0; t < sim.TierCount; t++)
            {
                if (sim.TablesAtTier(t) <= sim.TableCount) continue;
                long cost = sim.UpgradeCostFor(t);

                // Three conditions for expanding: there is money, there is
                // reputation, and the current place can already meet the demand.
                // Without the third the player starts paying rent on tables they
                // do not fill.
                bool canServe = _lastServiceRateBp > 8500;

                // IT DOES NOT EXPAND WHILE IT HAS A DEBT - and this rule was
                // MEASURED, found faulty, but NOT FIXED. The reason is below.
                //
                // The rule cripples the reasonable player by its own hand: the
                // same player that never touches a loan (the no-loan strategy)
                // climbs to 14 tables and 100 reputation and finishes on 27,853,
                // while the reasonable one stays at 7.4 tables and finishes on
                // 25,424. So taking a loan shuts down the growth curve for EIGHT
                // WEEKS - and it is the bot's own rule that does it, not the
                // economy.
                //
                // The gate was turned into "can it afford it" and measured:
                //
                //   share = 4 instalments   reasonable 14 tables, 43,746 - BEATS the planner
                //   share = the whole
                //           remaining debt  reasonable 14 tables, 39,877 - still beats it
                //   calibration penalty     9 -> 32, four more targets break:
                //                           imzaci/makul 0.84 and 0.82 (floor 0.90),
                //                           Turkish growth multiplier 4.20 (ceiling 4.0),
                //                           money becomes trivial for makul too
                //
                // So the fix is right but it CANNOT BE MADE ON ITS OWN: all the
                // calibration targets are tuned against this crippled reference,
                // and changing the reference means re-deriving the fixed point,
                // the rents and the target bands. A half-tuned balance is worse
                // than a documented flaw.
                //
                // The full record and the order in which to close it: docs/12 8d.
                if (sim.Cash > cost * 2 && sim.ReputationCenti > 4500 && canServe
                    && !sim.HasLoan)
                    sim.Apply(new Command(sim.TickIndex, CommandKind.Expand, t));
                break;
            }
        }

        /// <summary>
        /// Keeps as many main dishes open as the demand will bear and closes the
        /// rest. Rough rule: every main course needs at least four people a day.
        /// </summary>
        private static void NarrowMenu(Simulation sim)
        {
            int people = sim.ExpectedPeopleToday();

            // COLD STORAGE decides the menu width. Without cold storage
            // everything perishable goes off overnight, so every dish on the
            // menu has to be restocked daily and the leftovers go in the bin;
            // that is why we want four customers per main course. Cold storage
            // keeps the leftovers alive, so this threshold falls.
            //
            // This is why a content inventory of thirty-two dishes exists: the
            // upgrade buys menu width.
            // T2 AND T3 ARE NOT THE SAME.
            //
            // It used to read { 4, 3, 2, 2 }: the top tier gave nothing more
            // than the one before it. Naturally the bot never bought it - the
            // measurement came out as "t3 is never bought" and that was taken
            // for a CONTENT finding, when it was really the BOT's doing. The
            // simulation does allow the menu to widen further at t3 (the Awaited
            // penalty is proportional), so the bot has to see that opportunity.
            int[] perMain = { 4, 3, 2, 1 };
            int need = perMain[sim.StorageTier < perMain.Length ? sim.StorageTier : perMain.Length - 1];
            int allowedMains = people / need;
            if (allowedMains < 2) allowedMains = 2;

            // THE NAMED CUSTOMER'S FAVOURITE DISH COMES FIRST.
            //
            // If a regular cannot find their favourite dish on the menu their
            // satisfaction drops (docs/11). Narrowing the menu in a blind order
            // means paying that mechanic's penalty every single day - and the
            // measurement caught it: the good player's reputation fell from 96.5
            // to 87 and in some runs the place emptied out. It was the BOT that
            // was wrong, not the economy; a real player does not take Hasan
            // Usta's kuru fasulye off the menu.
            //
            // This is the DECISION the mechanic creates: menu width is limited
            // and the named customer's favourite takes one of those places.
            // THE RULE APPLIES TO EVERY DISH, AND UNTIL 19 SEPTEMBER IT WAS
            // APPLIED TO THE MAINS ALONE.
            //
            // The sentence above says why the menu is narrowed: every dish on
            // it is restocked daily and the leftovers go in the bin. Then the
            // loop switched every side, drink and dessert ON, every morning,
            // unconditionally. In Turkish those are the fresh ones - the
            // salads, the milk puddings - and at four tables the market
            // recommendation was buying four portions of each for dishes
            // that sell one a day. Measured (docs/63 10): the non-expander
            // threw away 41% of what it bought and earned 305 coins in sixty
            // days, and two levers in the GAME were tried against that
            // number before this line was read. The bot was not playing a
            // lokanta; a lokanta has a short menu.
            //
            // So the same arithmetic runs per role: a dish stays open while
            // the role's expected portions, spread over the dishes kept,
            // still reach `need` a day. Favourites of regulars first, as for
            // the mains, and at least one dish per role so nothing is ever
            // unorderable.
            int[] allowed = new int[4];
            int[] kept = new int[4];
            for (int r = 0; r < 4; r++)
            {
                long portions = Lokanta.Core.Fx.MulDiv(people, sim.RoleChanceBp(r), Lokanta.Core.Fx.One);
                allowed[r] = (int)(portions / need);
                if (allowed[r] < 1) allowed[r] = 1;
            }
            allowed[0] = allowedMains;   // the mains keep their floor of two

            for (int pass = 0; pass < 2; pass++)
            {
                for (int i = 0; i < sim.DishCount; i++)
                {
                    if (!sim.IsUnlocked(i)) continue;
                    int role = sim.DishRole(i);
                    if (role < 0)
                    {
                        if (pass == 0)
                            sim.Apply(new Command(sim.TickIndex, CommandKind.SetMenuSlot, i, 1));
                        continue;
                    }

                    bool favourite = sim.IsFavouriteOfArrivedRegular(i);
                    if (pass == 0 && !favourite) continue;      // favourites first
                    if (pass == 1 && favourite) continue;       // then the rest

                    bool on = kept[role] < allowed[role];
                    if (on) kept[role]++;
                    sim.Apply(new Command(sim.TickIndex, CommandKind.SetMenuSlot, i, on ? 1 : 0));
                }
            }
        }

        public void OnEvening(Simulation sim, in DayReport report)
        {
            if (report.PlannedParties > 0)
                _lastServiceRateBp = (int)Lokanta.Core.Fx.MulDiv(
                    report.ServedParties, Lokanta.Core.Fx.One, report.PlannedParties);

            // The crew is read FROM THE CAPACITY MODEL, not from the number of
            // angry customers.
            //
            // The first version said "hire if there were angry customers two days
            // running". But an angry customer is not always a crew signal: an
            // archetype with a short fuse may wait through the peak and walk out,
            // or the stock may have run out. The balance harness showed the
            // result: two hall staff on four tables, 2,408 coins of wages a week
            // against a rent of 1,950. At that volume the capacity model asks for
            // zero hall staff; the owner alone is enough.
            // THE CREW FOLLOWS TODAY, THE SACKING FOLLOWS PERSISTENCE.
            //
            // RequiredCrewToday now really measures TODAY (it used to return the
            // weekend peak every day). That is right, but it pushes the bot into
            // hiring and firing every week: hire on Friday, sack on Monday. Firing
            // resets EXPERIENCE, so churn is not free.
            //
            // The rule: if short, hire IMMEDIATELY; if over strength, wait for
            // THREE DAYS IN A ROW. A real player does not sack the waiter they
            // hired for the weekend on the Monday either.
            //
            // THE DECISION IS TAKEN IN THE EVENING BUT IT AFFECTS TOMORROW.
            //
            // This is OnEvening: AdvanceToNextDay comes AFTER this call, so a crew
            // set up for "today's" day type walks out onto the floor tomorrow. The
            // decision on the evening of day 5 (a weekday) was being taken against
            // the weekday crew, and day 6 (the weekend) was entered short-handed.
            // The method's name was right, its CALLER was asking about the wrong
            // day.
            Crew need = sim.RequiredCrewTomorrow();

            if (sim.Cooks < need.Cooks && sim.Cooks + sim.HallStaff < sim.StaffCap)
            {
                sim.Apply(new Command(sim.TickIndex, CommandKind.Hire, 0, Hiring.Pick(sim, 0)));
                _surplusDays = 0;
                return;
            }
            int hallTarget = need.Hall - HallShort;
            if (hallTarget < 0) hallTarget = 0;

            _surplusDays = sim.HallStaff > hallTarget ? _surplusDays + 1 : 0;

            if (sim.HallStaff < hallTarget && sim.Cooks + sim.HallStaff < sim.StaffCap)
            {
                sim.Apply(new Command(sim.TickIndex, CommandKind.Hire, 1, Hiring.Pick(sim, 1)));
                return;
            }

            // If the crew size is right, look at its QUALITY: if someone on the
            // books is clearly worse than the candidate at the door, replace
            // them. The hall first, because a hall trait is written directly onto
            // every table.
            // The condition is ">=", not "==". The first version looked for exact
            // equality, and the capacity model does not give exact equality on
            // most days; the replacement almost never ran. Spending sixty days
            // with a bad waiter meant taking the mechanic's penalty without ever
            // taking its decision.
            if (sim.HallStaff >= hallTarget && sim.HallStaff > 0
                && Hiring.ReplaceWorst(sim, 1)) return;
            if (sim.Cooks >= need.Cooks && Hiring.ReplaceWorst(sim, 0)) return;

            // Surplus crew is a direct loss: the wage is paid whether the
            // customers come or not.
            if (sim.HallStaff > hallTarget && _surplusDays >= 3)
            {
                sim.Apply(new Command(sim.TickIndex, CommandKind.Fire, 1,
                                      sim.HallStaff - 1));
                _surplusDays = 0;
            }
            else if (sim.Cooks > need.Cooks && sim.Cooks > 1)
                sim.Apply(new Command(sim.TickIndex, CommandKind.Fire, 0,
                                      sim.Cooks - 1));
        }
    }

    /// <summary>
    /// The player who keeps their prices permanently above the market.
    /// Question 4: does this strategy pay.
    /// </summary>
    /// <summary>
    /// The player who keeps their prices BELOW the market.
    ///
    /// The opposite of yuksek_fiyat, and the tool was ASYMMETRIC for a long
    /// time: it measured overpricing but not undercutting. And yet a real
    /// player's first reflex is usually "let me cut the price and pull a crowd".
    ///
    /// Price has no DIRECT effect on demand (DemandModel does not take a price);
    /// the only route is reputation, through satisfaction. So this strategy also
    /// tests whether that indirect route works at all.
    /// </summary>
    public sealed class CheapPricer : IStrategy
    {
        private readonly ReasonablePlayer _inner = new ReasonablePlayer();
        private bool _applied;

        public string Name => "ucuz_fiyat";
        public string Question => "Does keeping prices below the market pay";

        public void OnMorning(Simulation sim)
        {
            if (!_applied)
            {
                // 15% under: the content's underpriceFloorBp floor sits exactly
                // here, so anything below it should be pure lost revenue.
                for (int i = 0; i < 64; i++)
                {
                    long baseline = sim.BasePriceOf(i);
                    if (baseline <= 0) break;
                    int target = (int)Lokanta.Core.Fx.Bp(baseline, 8500);
                    sim.Apply(new Command(sim.TickIndex, CommandKind.SetPrice, i, target));
                }
                _applied = true;
            }
            _inner.OnMorning(sim);
        }

        public void OnEvening(Simulation sim, in DayReport report)
        {
            _inner.OnEvening(sim, report);
        }
    }

    /// <summary>
    /// The player who triples the sides, the drinks and the desserts WITHOUT
    /// TOUCHING the main course. The permanent sentry over one particular hole.
    ///
    /// Before the fix: satisfaction looked only at the main course's price
    /// (ComputeSatisfaction), while the bill charged for all four items
    /// (OrderPrice). So 20 of the 32 dishes could be made arbitrarily expensive
    /// and the customer never reacted at all. Measured: in the Turkish cuisine,
    /// taking a single drink from 16 to 160 was worth +109,000 centi - SIX TIMES
    /// the entire profit of the campaign.
    ///
    /// The expected result: it MUST NOT BEAT `makul`. If it does, the penalty is
    /// once again looking at a single item.
    /// </summary>
    public sealed class ExtrasGouger : IStrategy
    {
        private readonly ReasonablePlayer _inner = new ReasonablePlayer();
        private bool _applied;

        public string Name => "pahali_ekstra";
        public string Question => "Does making the extras expensive without touching the main course pay";

        public void OnMorning(Simulation sim)
        {
            if (!_applied)
            {
                for (int i = 0; i < sim.DishCount; i++)
                {
                    if (sim.IsMainDish(i)) continue;          // the main course IS NOT TOUCHED
                    long baseline = sim.BasePriceOf(i);
                    if (baseline <= 0) continue;
                    // BELOW THE CEILING: 2.3 times. Writing three times is now
                    // REJECTED (overpriceCeilingBp 25000), and a command that is
                    // rejected tests nothing - the bot would silently have turned
                    // into the "reasonable player".
                    sim.Apply(new Command(sim.TickIndex, CommandKind.SetPrice,
                                          i, (int)(baseline * 23 / 10)));
                }
                _applied = true;
            }
            _inner.OnMorning(sim);
        }

        public void OnEvening(Simulation sim, in DayReport report)
        {
            _inner.OnEvening(sim, report);
        }
    }

    /// <summary>
    /// THE BOT THAT OPENS THE COMBO AND INFLATES THE EXTRAS.
    ///
    /// Why it exists: the hole sat exactly at the INTERSECTION of two bots.
    /// "pahali_ekstra" makes the extras expensive but does NOT OPEN the combo;
    /// "imzaci" opens the combo but does NOT TOUCH the prices. There was no bot
    /// that combined the two, and that is why the balance harness stayed green
    /// for so long.
    ///
    /// The exploit measured: with the combo open, the price of the side and the
    /// drink entered satisfaction NOT AT ALL (only the main course was measured)
    /// and there was no price ceiling - the bot piled up 24.8 MILLION coins,
    /// 1470 times the baseline run.
    ///
    /// This bot is now a GUARD: if its till gets to a few times the reasonable
    /// player's, the hole is back.
    /// </summary>
    public sealed class ComboGouger : IStrategy
    {
        private readonly ReasonablePlayer _inner = new ReasonablePlayer();
        private bool _applied;

        public string Name => "kombo_sismesi";
        public string Question => "Does opening the combo and inflating the extras pay";

        public void OnMorning(Simulation sim)
        {
            _inner.OnMorning(sim);

            if (sim.HasCombo && !sim.ComboEnabled)
                sim.Apply(new Command(sim.TickIndex, CommandKind.SetCombo, 1));

            if (_applied) return;
            _applied = true;

            // Make the extras as expensive as possible WITHOUT TOUCHING the main
            // course. If there is a ceiling the command is rejected and the price
            // stays at the market; if there is not, twenty times gets written.
            for (int i = 0; i < sim.DishCount; i++)
            {
                if (sim.IsMainDish(i)) continue;
                long baseline = sim.BasePriceOf(i);
                if (baseline <= 0) continue;
                // Just under the ceiling: 2.4 times. More than that is rejected
                // and the bot would test nothing.
                sim.Apply(new Command(sim.TickIndex, CommandKind.SetPrice,
                                      i, (int)(baseline * 24 / 10)));
            }
        }

        public void OnEvening(Simulation sim, in DayReport report)
        {
            _inner.OnEvening(sim, report);
        }
    }

    public sealed class GreedyPricer : IStrategy
    {
        private readonly int _markupBp;
        private readonly ReasonablePlayer _inner = new ReasonablePlayer();
        private bool _applied;

        private readonly string _name;

        /// <summary>
        /// THE MARKUP BAND IS NOW MEASURED FROM TWO POINTS.
        //
        /// The price bots sat at 8500 (cheap), 13000, 23000 and 24000 bp; there
        /// was no measurement at all BETWEEN 10000 and 13000. That mattered,
        /// because price has no direct channel to demand - the only route is
        /// satisfaction -> reputation, and reputation is clamped to the tier's
        /// ceiling. For a player at that ceiling a loss of satisfaction may buy
        /// nothing at all, which means a small markup may be FREE. Only a bot
        /// from inside the band can show that.
        /// </summary>
        public GreedyPricer(int markupBp = 13000, string name = "yuksek_fiyat")
        {
            _markupBp = markupBp;
            _name = name;
        }

        public string Name => _name;
        public string Question => "Does a strategy that keeps prices permanently above the market pay";

        public void OnMorning(Simulation sim)
        {
            if (!_applied)
            {
                // The prices are set once; SetPrice takes an absolute value.
                for (int i = 0; i < 64; i++)
                {
                    long baseline = sim.BasePriceOf(i);
                    if (baseline <= 0) break;
                    int target = (int)Lokanta.Core.Fx.Bp(baseline, _markupBp);
                    sim.Apply(new Command(sim.TickIndex, CommandKind.SetPrice, i, target));
                }
                _applied = true;
            }
            _inner.OnMorning(sim);
        }

        public void OnEvening(Simulation sim, in DayReport report)
        {
            _inner.OnEvening(sim, report);
        }
    }

    /// <summary>
    /// The player who expands the moment they see money. Does not look for the
    /// service rate condition.
    /// What it measures: is the reasonable player's 85% service rate gate too
    /// tight.
    /// </summary>
    public sealed class Expansionist : IStrategy
    {
        public string Name => "atilgan";
        public string Question => "Are the expansion gates too tight";

        public void OnMorning(Simulation sim)
        {
            RestockOnly.Restock(sim);

            if (!sim.HasLoan && sim.Cash < sim.WeeklyFixedCost() * 2)
                sim.Apply(new Command(sim.TickIndex, CommandKind.TakeLoan, 0));

            for (int t = 0; t < sim.TierCount; t++)
            {
                if (sim.TablesAtTier(t) <= sim.TableCount) continue;
                // One condition only: can they afford it. No reputation and no
                // service rate are required.
                if (sim.Cash > sim.UpgradeCostFor(t))
                    sim.Apply(new Command(sim.TickIndex, CommandKind.Expand, t));
                break;
            }
        }

        public void OnEvening(Simulation sim, in DayReport report)
        {
            Crew need = sim.RequiredCrewToday();
            if (sim.Cooks < need.Cooks && sim.Cooks + sim.HallStaff < sim.StaffCap)
                sim.Apply(new Command(sim.TickIndex, CommandKind.Hire, 0, Hiring.Pick(sim, 0)));
            else if (sim.HallStaff < need.Hall && sim.Cooks + sim.HallStaff < sim.StaffCap)
                sim.Apply(new Command(sim.TickIndex, CommandKind.Hire, 1, Hiring.Pick(sim, 1)));
            else if (sim.HallStaff > need.Hall)
                sim.Apply(new Command(sim.TickIndex, CommandKind.Fire, 1));
        }
    }

    /// <summary>
    /// The player who expands on the closed-form model's schedule:
    /// 7 tables on day 15, 10 tables on day 29, 14 tables on day 43.
    /// What it measures: can that schedule be met in the simulation.
    /// </summary>
    public sealed class PlannerSchedule : IStrategy
    {
        private static readonly int[] Days = { 15, 29, 43 };
        private int _next;

        public string Name => "planci";
        public string Question => "Can the closed-form model's expansion schedule be met";

        public void OnMorning(Simulation sim)
        {
            RestockOnly.Restock(sim, stockAhead: true);

            if (!sim.HasLoan && sim.Cash < sim.WeeklyFixedCost() * 2)
                sim.Apply(new Command(sim.TickIndex, CommandKind.TakeLoan, 0));

            // The schedule watches the tables; the kitchen has to keep up with
            // those tables too.
            Equipment.Upgrade(sim);

            if (_next >= Days.Length || sim.Day < Days[_next]) return;

            // A REFUSED EXPANSION USED TO BURN THE SLOT FOREVER.
            //
            // _next++ ran whether or not the Expand went through, so a
            // planner that was a few hundred coins short on day 43 never
            // tried again - it spent the remaining seventeen days earning
            // money it had no way to spend. Turkish ended the campaign at
            // 11.0 tables holding 23,622 coins, when the step it had missed
            // costs 8,000.
            //
            // "A bot that gets refused is not a bot" - the note beside
            // Interventionist.Tried records the same lesson from the price
            // ceiling, where yuksek_fiyat silently became a copy of the
            // reasonable player once its prices started bouncing. Here it
            // made the calibration read "planci cannot keep to the calendar"
            // on almost every candidate, at every realisation rate, which is
            // a complaint the calibration has no lever to answer.
            //
            // So the slot is only consumed when the table count ACTUALLY
            // moves, and until then the planner retries every morning. The
            // question it asks is unchanged - can the model's schedule be
            // afforded - but a schedule met on day 45 now reads as met,
            // where it used to read as never.
            int before = sim.TableCount;
            for (int t = 0; t < sim.TierCount; t++)
            {
                if (sim.TablesAtTier(t) <= sim.TableCount) continue;
                sim.Apply(new Command(sim.TickIndex, CommandKind.Expand, t));
                break;
            }
            if (sim.TableCount == before)
            {
                Refused++;
                return;
            }
            _next++;
        }

        /// <summary>
        /// How many scheduled expansions were turned down. It is the
        /// difference between "the schedule is unaffordable" and "the bot
        /// asked once at the wrong moment", and before this counter existed
        /// the two were indistinguishable in the output.
        /// </summary>
        public static int Refused;

        public void OnEvening(Simulation sim, in DayReport report)
        {
            Crew need = sim.RequiredCrewToday();
            if (sim.Cooks < need.Cooks && sim.Cooks + sim.HallStaff < sim.StaffCap)
                sim.Apply(new Command(sim.TickIndex, CommandKind.Hire, 0, Hiring.Pick(sim, 0)));
            else if (sim.HallStaff < need.Hall && sim.Cooks + sim.HallStaff < sim.StaffCap)
                sim.Apply(new Command(sim.TickIndex, CommandKind.Hire, 1, Hiring.Pick(sim, 1)));
            else if (sim.HallStaff > need.Hall)
                sim.Apply(new Command(sim.TickIndex, CommandKind.Fire, 1));
        }
    }

    /// <summary>
    /// The player who skimps on ingredients: buys everything at the CHEAPEST
    /// quality, everything else just like the reasonable player.
    ///
    /// The question it measures: is skimping on ingredients a valid strategy or
    /// a trap. The content has made the six most sensitive ingredients MEAT, so
    /// the answer is expected to vary with the menu.
    /// </summary>
    public sealed class CheapIngredients : IStrategy
    {
        private readonly ReasonablePlayer _inner = new ReasonablePlayer();
        private bool _set;

        public string Name => "ucuz_malzeme";
        public string Question => "Does skimping on ingredients pay";

        public void OnMorning(Simulation sim)
        {
            if (!_set)
            {
                sim.Apply(new Command(sim.TickIndex, CommandKind.SetQuality, 0));
                _set = true;
            }
            _inner.OnMorning(sim);
        }

        public void OnEvening(Simulation sim, in DayReport report)
        {
            _inner.OnEvening(sim, report);
        }
    }

    /// <summary>
    /// The player who INTERVENES during service: plays like the reasonable
    /// player, and on top of that spends the owner's attention on the tables
    /// with the least patience left.
    ///
    /// The question it measures: does docs/02's core loop -- "during service you
    /// only intervene in crises" -- actually pay. Interventions are limited per
    /// day and the free tea costs money.
    /// </summary>
    /// <summary>
    /// The player who uses the signature mechanic. docs/07: the mechanic is the
    /// one thing that separates one cuisine from another, so USING IT has to
    /// pay - without becoming compulsory. This strategy measures that band.
    ///
    /// Fast food -> opens the combo in the second season and keeps it open.
    /// Turkish   -> opens the tab SELECTIVELY: only for the party it offered tea
    ///              to, at most two a day, and only while the open balance stays
    ///              under one week of fixed costs.
    /// </summary>
    public sealed class SignaturePlayer : IStrategy
    {
        /// <summary>
        /// The minimum number of visits for a tab. 0 = everyone.
        /// STATIC: rewritten at the start of every strategy, otherwise the next
        /// arm inherits the selectiveness and the measurement silently measures
        /// something else.
        /// </summary>
        public static int MinVisits;

        private readonly ReasonablePlayer _inner = new ReasonablePlayer();
        private int _grantedToday;

        public string Name => "imzaci";
        public string Question => "Does using the signature mechanic pay";

        public void OnMorning(Simulation sim)
        {
            _grantedToday = 0;
            _inner.OnMorning(sim);

            // The combo is not free (it tires the kitchen out) but the three
            // items it has to keep on the menu are already in the opening menu.
            if (sim.HasCombo && !sim.ComboEnabled)
                sim.Apply(new Command(sim.TickIndex, CommandKind.SetCombo, 1));
        }

        public void OnEvening(Simulation sim, in DayReport report)
        {
            _inner.OnEvening(sim, report);
        }

        /// <summary>
        /// THE ARM THAT CLOSES THE COMBO AT THE PEAK. 0 = never close.
        ///
        /// The design's written intention: "because the combo also increases the
        /// kitchen load, closing it at the peak is a LEGITIMATE play and the axis
        /// must not punish it". But there was no bot playing that way, so the
        /// axis's target could only have been INVENTED. This arm measures it: the
        /// combo closes once hall occupancy passes a threshold and reopens when
        /// it falls back.
        ///
        /// STATIC: rewritten at the start of every strategy.
        /// </summary>
        public static int CloseAtOccupancyBp;

        /// <summary>Grants a tab to WHOEVER ASKS. Called during service.</summary>
        public void DuringService(Simulation sim)
        {
            // The combo closes at the peak: to give the kitchen some relief.
            if (CloseAtOccupancyBp > 0 && sim.HasCombo && sim.TableCount > 0)
            {
                int occupancyBp = sim.OccupiedTables * 10000 / sim.TableCount;
                bool shouldBeOpen = occupancyBp < CloseAtOccupancyBp;
                if (sim.ComboEnabled != shouldBeOpen)
                    sim.Apply(new Command(sim.TickIndex, CommandKind.SetCombo, shouldBeOpen ? 1 : 0));
            }

            if (!sim.HasCredit || _grantedToday >= 4) return;

            // Stop if the open balance exceeds one week of fixed costs: a tab
            // disturbs the cash flow, and a disturbed cash flow cannot pay the
            // rent.
            if (sim.OpenCredit > sim.WeeklyFixedCost()) return;

            for (int p = 0; p < Simulation.MaxParties; p++)
            {
                if (!sim.CreditEligible(p)) continue;

                // THE TRUST THRESHOLD: does it matter who you write it for?
                //
                // The chance of collecting now depends on the customer's visit
                // count. This arm writes for everyone with a threshold of zero;
                // the picky arm writes only for the people it knows. The
                // difference between the two tells us whether "who should I
                // write it for" is REALLY a decision - one arm, one answer.
                if (MinVisits > 0)
                {
                    int r = sim.PartyRegular(p);
                    if (r < 0 || sim.RegularVisits(r) < MinVisits) continue;
                }


                // The tea is PART of the tab: ExtendCredit pays for it itself and
                // does not eat into the daily intervention budget. In the first
                // version the tea was an intervention, and because the budget ran
                // out most tabs were opened without one - so the collection bonus
                // was never exercised at all.
                sim.Apply(new Command(sim.TickIndex, CommandKind.ExtendCredit, p));
                if (++_grantedToday >= 4) return;
            }
        }
    }

    /// <summary>
    /// THE PATIENT INTERVENTIONIST: it SAVES its interventions.
    ///
    /// Its only difference from "mudahaleci" is the timing - both use the same
    /// actions in the same order. The difference is that this arm spends NOTHING
    /// until a table drops below the warning threshold.
    ///
    /// Why it is a separate arm: while the value of intervening was being
    /// measured, the bot was burning its budget in the first eighty seconds of
    /// the day, while the peak is in the second slot. So the question "does
    /// intervening pay" was being measured with the player's ONLY real decision
    /// ("now, or at the peak") held fixed - and held at its worst value at that.
    ///
    /// If this arm BEATS the interventionist the mechanic is sound and the
    /// problem is failing to teach the player to hold back. If it does not, the
    /// mechanic itself is weak. One arm, one answer.
    /// </summary>
    /// <summary>
    /// THE PICKY CREDITOR: writes a tab only for PEOPLE IT KNOWS.
    ///
    /// That is its only difference from "imzaci"; both can open the same number
    /// of tabs on the same days. The difference is that this arm looks at whether
    /// it recognises the customer.
    ///
    /// Why it is a separate arm: the chance of collecting now depends on the
    /// visit count, but whether that produces a DECISION can only be known by
    /// measuring the selectiveness. If this arm beats the signature player the
    /// question is real; if it does not, the trust dimension is just a number.
    /// </summary>
    public sealed class PickyCreditor : IStrategy
    {
        private readonly SignaturePlayer _inner = new SignaturePlayer();

        public string Name => "secici_veresiye";
        public string Question => "Does opening a tab only for PEOPLE YOU KNOW pay";

        public void OnMorning(Simulation sim) { _inner.OnMorning(sim); }
        public void OnEvening(Simulation sim, in DayReport report)
        {
            _inner.OnEvening(sim, report);
        }
        public void DuringService(Simulation sim) { _inner.DuringService(sim); }
    }

    /// <summary>
    /// THE ONE WHO ASSIGNS A DISHWASHER: puts one hall worker on the sink.
    ///
    /// Why it is needed: `CommandKind.SetDishwashers` existed in the interface
    /// and in the tests but NO BOT used it - so the plate bottleneck's decision
    /// had never been measured. The tool's own output says the bottleneck is
    /// REAL ("waited, no plate" is 206 times for makul), but nothing could answer
    /// the question "does putting one person on the sink pay".
    ///
    /// The trade-off is clean: the person on the sink is NOT in the hall. So
    /// opening up the plate bottleneck narrows the service bottleneck.
    /// </summary>
    public sealed class DishDuty : IStrategy
    {
        // BUILT ON TOP OF THE PLANNER, not on the reasonable player.
        //
        // Its first version wrapped ReasonablePlayer, and that player NEVER
        // reaches four hall workers (its crew stops at 4, its hall at ~3). The
        // result came out BYTE FOR BYTE identical to `makul` - the threshold
        // never fired, and the only thing that said so was the two rows being
        // identical. A dedicated dishwasher is a BIG restaurant's question
        // anyway.
        private readonly PlannerSchedule _inner = new PlannerSchedule();

        /// <summary>
        /// From how many hall workers on someone is dedicated to the sink.
        ///
        /// The threshold was chosen BY MEASUREMENT. The first attempt was 2 and
        /// it assigned someone in a small restaurant too; what was being measured
        /// was not "does a dishwasher pay" but "does assigning one too early
        /// lose". The same trap had been fallen into on the previous round with
        /// PeakCloser.
        /// </summary>
        private const int DedicateFrom = 4;

        public string Name => "bulasikci";
        public string Question => "Does putting one person on the sink pay";

        public void OnMorning(Simulation sim)
        {
            _inner.OnMorning(sim);

            // Once the hall reaches two people, one goes to the sink. Assigning
            // one while there is a single waiter EMPTIES the hall - and then what
            // would be measured is not the dishwashing decision but being left
            // without a waiter.
            int target = sim.HallStaff >= DedicateFrom ? 1 : 0;
            if (sim.Dishwashers != target)
                sim.Apply(new Command(sim.TickIndex, CommandKind.SetDishwashers, target));
        }

        public void OnEvening(Simulation sim, in DayReport report)
        {
            _inner.OnEvening(sim, report);
        }
    }

    /// <summary>
    /// THE EAGER COLLECTOR: chases the tab before it falls due.
    ///
    /// Why it is needed: `CommandKind.CollectCredit` was written out in full in
    /// the simulation and it even has an interface ("Chase it now") - but NO BOT
    /// ever pressed it. So the mechanic's second decision, the thing the design
    /// sells as "collection depends on trust", had NEVER BEEN MEASURED.
    ///
    /// The trade-off is clean: chasing HALVES the chance and if it fails the tab
    /// closes there and then - but you get the money without waiting seven days.
    /// The question is therefore "patience or cash".
    /// </summary>
    public sealed class EagerCollector : IStrategy
    {
        private readonly SignaturePlayer _inner = new SignaturePlayer();

        public string Name => "erken_tahsilat";
        public string Question => "Does chasing the tab before it falls due pay";

        public void OnMorning(Simulation sim) { _inner.OnMorning(sim); }

        public void OnEvening(Simulation sim, in DayReport report)
        {
            _inner.OnEvening(sim, report);

            // BACK TO FRONT: chasing closes the tab and the rest shift down. If
            // we went front to back, one tab would be skipped on every close and
            // the bot would look like it was chasing while having chased only
            // half of them.
            for (int i = sim.TabCount - 1; i >= 0; i--)
                sim.Apply(new Command(sim.TickIndex, CommandKind.CollectCredit, i));
        }

        public void DuringService(Simulation sim) { _inner.DuringService(sim); }
    }

    /// <summary>
    /// THE SIGNATURE PLAYER WHO CLOSES AT THE PEAK.
    ///
    /// Its only difference from "imzaci": it closes the combo once hall occupancy
    /// passes 75% and opens it again when it falls back. This is the play the
    /// design calls LEGITIMATE.
    ///
    /// Why it is needed: the signature axis's target has to be set against the
    /// share this play reaches, otherwise the target is invented - and the code's
    /// own warning says "an invented target makes the axis either saturated or
    /// unreachable".
    /// </summary>
    public sealed class PeakCloser : IStrategy
    {
        private readonly SignaturePlayer _inner = new SignaturePlayer();

        public string Name => "zirvede_kapat";
        public string Question => "Does closing the combo at the peak pay";

        public void OnMorning(Simulation sim) { _inner.OnMorning(sim); }
        public void OnEvening(Simulation sim, in DayReport report)
        {
            _inner.OnEvening(sim, report);
        }
        public void DuringService(Simulation sim) { _inner.DuringService(sim); }
    }

    public sealed class PatientInterventionist : IStrategy
    {
        private readonly ReasonablePlayer _inner = new ReasonablePlayer();

        public string Name => "sabirli_mudahale";
        public string Question => "Does saving the intervention FOR THE PEAK pay";

        public void OnMorning(Simulation sim) { _inner.OnMorning(sim); }
        public void OnEvening(Simulation sim, in DayReport report)
        {
            _inner.OnEvening(sim, report);
        }
    }

    public sealed class Interventionist : IStrategy
    {
        private readonly ReasonablePlayer _inner = new ReasonablePlayer();

        public string Name => "mudahaleci";
        public string Question => "Does intervening during service pay";

        public void OnMorning(Simulation sim) { _inner.OnMorning(sim); }

        public void OnEvening(Simulation sim, in DayReport report)
        {
            _inner.OnEvening(sim, report);
        }

        /// <summary>
        /// Called during service. Shows the owner's attention to the table with
        /// the least patience left that has not been intervened on yet.
        /// </summary>
        /// <summary>
        /// How many interventions were TRIED and how many WENT THROUGH.
        ///
        /// "A bot that gets refused is not a bot" (this project learnt that on
        /// the price ceiling: once the ceiling arrived the yuksek_fiyat bot's
        /// prices were being rejected and the bot had silently become a copy of
        /// the reasonable player). Before measuring the effect of an intervention
        /// you have to measure whether the intervention REALLY happened.
        /// </summary>
        public static int Tried, Applied;

        /// <summary>
        /// Resets the counters. Called at the start of every strategy.
        ///
        /// REQUIRED BECAUSE THEY ARE STATIC: DuringService is called by both
        /// "mudahale" and "baskili_mudahale". Without a reset the sum of the two
        /// arms was printed on a single line and the counters' reason to exist -
        /// "did the intervention really happen", PER ARM - became unreadable.
        /// </summary>
        public static void ResetCounters() { Tried = 0; Applied = 0; }

        /// <summary>
        /// TEA OR ATTENTION - NOW A QUESTION.
        ///
        /// The tea used to be below attention on every axis and this bot never
        /// pressed it; so one of the pieces of evidence for "one of the three
        /// actions is dead" was the bot's own behaviour. The tea now goes to
        /// EVERYONE WAITING, so in a crowd its value may exceed attention's.
        ///
        /// The rule: how many tables are waiting. At the threshold and above, tea
        /// for the hall; below it, attention for a single table. Whether the
        /// threshold is in the right place can only be known by measuring - which
        /// is why the threshold is a PARAMETER.
        /// </summary>
        public static int TeaThreshold = 3;

        /// <summary>
        /// PATIENT MODE: the budget is spent only when there is REAL pressure.
        ///
        /// The bot was burning its budget every 20 sim-seconds, so four
        /// interventions a day ran out in roughly the first 80 seconds of a
        /// 480-second day - while the peak is in the second slot. So the answer to
        /// "does intervening pay" may have been measuring not the mechanic but
        /// THE BOT PLAYING BADLY. The player's only real decision - "now, or at
        /// the peak" - had been held fixed, and at its worst value at that.
        ///
        /// This mode sets it free: no budget is spent until a table drops below
        /// the warning threshold.
        /// </summary>
        public static bool OnlyWhenUrgent;

        public static void DuringService(Simulation sim)
        {
            if (sim.InterventionsLeft <= 0) return;

            // In patient mode: if nobody is critical, do nothing.
            if (OnlyWhenUrgent && !sim.AnyPartyCritical) return;

            // THE KITCHEN first. Calming a customer whose patience has run out
            // covers the symptom; clearing the blocked station solves the cause
            // and rescues EVERYONE waiting at that station at once.
            int station = sim.BusiestStation();
            if (station >= 0)
            {
                int before = sim.InterventionsLeft;
                Tried++;
                sim.Apply(new Command(sim.TickIndex, CommandKind.Intervene,
                                      station, (int)InterventionKind.RushStation));
                if (sim.InterventionsLeft < before) Applied++;
                if (sim.InterventionsLeft <= 0) return;
            }

            // IF MANY TABLES ARE WAITING, TEA FOR THE HALL.
            if (TeaThreshold > 0 && sim.WaitingParties >= TeaThreshold)
            {
                int beforeTea = sim.InterventionsLeft;
                Tried++;
                sim.Apply(new Command(sim.TickIndex, CommandKind.Intervene,
                                      -1, (int)InterventionKind.FreeTea));
                if (sim.InterventionsLeft < beforeTea) Applied++;
                if (sim.InterventionsLeft <= 0) return;
            }

            int worst = sim.MostImpatientParty();
            if (worst < 0) return;

            int beforeAttention = sim.InterventionsLeft;
            Tried++;
            sim.Apply(new Command(sim.TickIndex, CommandKind.Intervene,
                                  worst, (int)InterventionKind.OwnerAttention));
            if (sim.InterventionsLeft < beforeAttention) Applied++;
        }
    }

    /// <summary>
    /// THE PLAYER UNDER PRESSURE: one waiter short.
    ///
    /// Two copies of it run - one intervenes, one does not - and that is the ONLY
    /// difference between them. It had already been measured that intervening
    /// cannot be detected in a comfortable restaurant (1440/1440 went through and
    /// the result did not change); this pair separates whether the problem is "is
    /// the mechanic weak" or "is there nothing to save in the first place".
    ///
    /// The short crew is a deliberate choice: a real player does this too, to save
    /// on wages, and the game's promise kicks in at exactly that moment - "you
    /// are the owner, when they cannot keep up, you step in".
    /// </summary>
    public sealed class PressuredPlayer : IStrategy
    {
        private readonly ReasonablePlayer _inner = new ReasonablePlayer();
        private readonly bool _intervene;

        public PressuredPlayer(bool intervene) { _intervene = intervene; }

        public bool Intervenes => _intervene;

        public string Name => _intervene ? "baskili_mudahale" : "baskili";

        public string Question => _intervene
            ? "Does intervening rescue things when the crew cannot keep up"
            : "What does working one waiter short cost";

        public void OnMorning(Simulation sim) { _inner.OnMorning(sim); }

        public void OnEvening(Simulation sim, in DayReport report)
        {
            _inner.OnEvening(sim, report);
        }
    }

    /// <summary>
    /// The player who never touches a loan.
    ///
    /// docs/12 8's SIXTH QUESTION: "does taking a loan work, or is it a trap".
    /// The tool asks that question and no strategy could answer it - makul,
    /// planci and atilgan all take the loan opportunistically, and there was no
    /// LOAN-FREE control. So the answer to a design question was being treated as
    /// "known" without ever being measured.
    ///
    /// The question is meaningful now because the loan has an INTERFACE: the
    /// mechanic was written out in full in the core and had a button on no screen
    /// at all.
    /// </summary>
    public sealed class NoLoanPlayer : IStrategy
    {
        private readonly ReasonablePlayer _inner = new ReasonablePlayer();

        public string Name => "kredisiz";
        public string Question => "Does taking a loan work, or is it a trap";

        public void OnMorning(Simulation sim)
        {
            // The reasonable player itself, with ONE difference: no loan.
            // ReasonablePlayer decides on the loan itself, so a flag is needed to
            // block it here.
            ReasonablePlayer.AllowLoan = false;
            try { _inner.OnMorning(sim); }
            finally { ReasonablePlayer.AllowLoan = true; }
        }

        public void OnEvening(Simulation sim, in DayReport report)
        {
            _inner.OnEvening(sim, report);
        }
    }

    /// <summary>
    /// The player who keeps a SINGLE main course on the menu. An ACCEPTANCE TEST.
    ///
    /// A narrow menu was once the decisively dominant strategy and the tool could
    /// not see it, because no strategy was trying it: a dish taken off the menu
    /// was not counted as "asked for", so narrowing cost nothing at all on the
    /// demand side. Measured - this strategy beat the reasonable player by 12% in
    /// fast food and by 38% in the Turkish cuisine.
    ///
    /// These lines are that bug's REGRESSION GUARD: if tek_yemek beats makul
    /// again, Awaited() is failing to see the menu once more.
    /// </summary>
    public sealed class OneDishPlayer : IStrategy
    {
        private readonly ReasonablePlayer _inner = new ReasonablePlayer();

        public string Name => "tek_yemek";
        public string Question => "Does narrowing the menu down to a single dish pay";

        public void OnMorning(Simulation sim)
        {
            _inner.OnMorning(sim);

            // The reasonable player has already narrowed the menu to the demand;
            // this player closes everything but ONE main course. The sides and
            // drinks stay: what is measured is menu WIDTH, not the existence of a
            // menu.
            int kept = -1;
            for (int i = 0; i < sim.DishCount; i++)
            {
                if (!sim.IsOnMenu(i)) continue;
                if (!sim.IsMainDish(i)) continue;
                if (kept < 0) { kept = i; continue; }
                sim.Apply(new Command(sim.TickIndex, CommandKind.SetMenuSlot, i, 0));
            }
        }

        public void OnEvening(Simulation sim, in DayReport report)
        {
            _inner.OnEvening(sim, report);
        }
    }

    /// <summary>
    /// The player who NEVER narrows the menu: every main course that unlocks
    /// stays on it.
    ///
    /// The OPPOSITE of tek_yemek and the other end of the same question. docs/32
    /// says "cold storage makes you buy menu width"; that sentence is only true
    /// if a wide menu earns SOMETHING. The cost of a narrow menu had been
    /// measured (tek_yemek goes bankrupt), the reward of a wide one had not.
    ///
    /// The ladder cannot be priced until both are measured: the storage makes a
    /// wide menu AFFORDABLE, so its reward is the wide menu's reward.
    /// </summary>
    public sealed class WideMenuPlayer : IStrategy
    {
        private readonly ReasonablePlayer _inner = new ReasonablePlayer();

        public string Name => "genis_menu";
        public string Question => "Does never narrowing the menu pay";

        public void OnMorning(Simulation sim)
        {
            _inner.OnMorning(sim);

            // The reasonable player narrowed the menu; this player opens it all
            // back up.
            for (int i = 0; i < sim.DishCount; i++)
            {
                if (sim.IsOnMenu(i) || !sim.IsUnlocked(i)) continue;
                sim.Apply(new Command(sim.TickIndex, CommandKind.SetMenuSlot, i, 1));
            }
        }

        public void OnEvening(Simulation sim, in DayReport report)
        {
            _inner.OnEvening(sim, report);
        }
    }

    /// <summary>
    /// The player who inflates the crew right up to the cap. Measures the penalty
    /// for being overstaffed.
    /// </summary>
    public sealed class OverStaffer : IStrategy
    {
        public string Name => "fazla_kadro";
        public string Question => "Does running the crew up to the cap pay";

        public void OnMorning(Simulation sim)
        {
            RestockOnly.Restock(sim);
            while (sim.Cooks + sim.HallStaff < sim.StaffCap)
            {
                int pool = sim.HallStaff <= sim.Cooks ? 1 : 0;
                int before = sim.Cooks + sim.HallStaff;
                sim.Apply(new Command(sim.TickIndex, CommandKind.Hire, pool, Hiring.Pick(sim, pool)));
                if (sim.Cooks + sim.HallStaff == before) break;
            }
        }

        public void OnEvening(Simulation sim, in DayReport report) { }
    }
}
