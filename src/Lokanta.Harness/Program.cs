using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Lokanta.Content;
using Lokanta.Core.Content;
using Lokanta.Core.Economy;
using Lokanta.Core.Sim;

namespace Lokanta.Harness
{
    /// <summary>
    /// Headless balance harness.
    ///
    /// docs/04-architecture.md: the product of Phase 0. It simulates campaigns
    /// with different player strategies and answers the questions in
    /// docs/12-economy.md 8 with numbers.
    ///
    /// Running it:
    ///   dotnet run --project src/Lokanta.Harness
    ///   dotnet run --project src/Lokanta.Harness -- --seeds 20 --csv out.csv
    /// </summary>
    public static class Program
    {
        /// <summary>
        /// The default cuisine. Changed with --cuisine turk.
        ///
        /// The whole balance solution was done with fast food and the second
        /// cuisine was never measured. Its slot durations differ
        /// (576/2304/1200/720), so the Turkish restaurant's lunch peak is far
        /// sharper; there is no reason for it to give the same result with the
        /// same crew and the same equipment.
        /// </summary>
        private const string DefaultCuisine = "fastfood";

        public static int Main(string[] args)
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

            int seeds = ArgInt(args, "--seeds", 8);

            // For measuring the cold storage ladder one step at a time.
            // Unlimited by default, so it does not affect the normal run.
            Equipment.StorageCap = ArgInt(args, "--storage-cap", int.MaxValue);
            int days = ArgInt(args, "--days", 60);
            string csv = ArgStr(args, "--csv", null);
            string root = FindRoot();

            EconomyConfig economy = ContentLoader.LoadEconomy(Path.Combine(root, "content"));
            string cuisine = ArgStr(args, "--cuisine", DefaultCuisine);

            // AN UNKNOWN FLAG IS AN ERROR.
            //
            // It used to be swallowed silently and that was dangerous: someone
            // typing "--cuisine turk" got a second fastfood run and believed
            // they had compared two cuisines. A measuring tool that silently
            // measures the wrong thing is worse than one that measures nothing.
            if (!CheckArgs(args)) return 2;
            ContentSet content = ContentSetLoader.Load(Path.Combine(root, "content"), cuisine);
            // Slot durations come from the cuisine (docs/28 Decision G).
            TimingConfig timing = content.SlotDurationsBp != null
                ? TimingConfig.Default().WithSlotDurations(content.SlotDurationsBp).WithEatMs(content.EatMs)
                : TimingConfig.Default();

            Console.WriteLine("=== Lokanta balance harness ===");
            Console.WriteLine($"cuisine     : {cuisine}");
            Console.WriteLine($"dishes      : {content.Dishes.Length}");
            Console.WriteLine($"archetypes  : {content.Archetypes.Length}");
            Console.WriteLine($"days        : {days}");
            Console.WriteLine($"seeds       : {seeds}");
            Console.WriteLine($"service day : {timing.ServiceDayMs / 1000} s sim, {timing.ServiceTicks} ticks");
            Console.WriteLine($"slot ticks  : {timing.SlotTicks(0)} / {timing.SlotTicks(1)} / " +
                              $"{timing.SlotTicks(2)} / {timing.SlotTicks(3)}");
            Console.WriteLine();

            if (Array.IndexOf(args, "--solve") >= 0)
            {
                RentSolver.Run(economy, content, timing, Math.Min(seeds, 4), days);
                return 0;
            }

            List<StrategyResult> results = new List<StrategyResult>();
            StringBuilder rows = new StringBuilder();
            rows.AppendLine("strategy,seed,day,tables,cooks,hall,planned,served,angry_at_table,people,revenue,satisfaction,reputation,cash");

            // "--strategy" IS ACTUALLY READ NOW. The flag sat in the list but
            // was read nowhere; someone who wanted to measure a single bot ran
            // all twenty of them and believed they had filtered.
            string only = ArgStr(args, "--strategy", null);
            int matches = 0;

            foreach (IStrategy proto in AllStrategies())
            {
                if (only != null
                    && !proto.Name.Equals(only, StringComparison.OrdinalIgnoreCase))
                    continue;
                matches++;

                StrategyResult agg = new StrategyResult(proto.Name, proto.Question)
                {
                    // For the reconciliation: cash movement is measured from here.
                    StartCash = economy.StartingCash,
                };

                // THE MEASUREMENT ARM DEPENDS ON THE STRATEGY: the pressured
                // pair runs the hall one person short. Because the arm is
                // STATIC it is rewritten at the start of every strategy -
                // otherwise the next strategy inherits the short crew and the
                // measurement silently measures something else.
                ReasonablePlayer.HallShort = proto is PressuredPlayer ? 1 : 0;
                Interventionist.ResetCounters();

                // STATIC FOR THE SAME REASON AS EVERY COUNTER ABOVE IT.
                // Left unreset, the planner's refusals would be added to
                // whatever strategy ran after it and the number would slowly
                // stop meaning anything.
                PlannerSchedule.Refused = 0;

                // THE PATIENT MODE IS STATIC TOO: if it is not reset the next
                // strategy inherits it and the measurement silently measures
                // something else - the same trap as HallShort above.
                Interventionist.OnlyWhenUrgent = proto is PatientInterventionist;

                // The picky arm only puts customers with 5+ visits on the tab.
                SignaturePlayer.MinVisits = proto is PickyCreditor ? 5 : 0;

                // The arm that closes at the peak: the combo closes once the
                // hall is HALF full.
                //
                // My first attempt was 75% and it NEVER FIRED ONCE: occupancy
                // does not reach that level in practice (8-10 of 14 tables full
                // = 57-71%). The arm gave exactly the same result as the
                // signature bot, which means the measurement measured nothing -
                // and the only thing that said so was the two rows coming out
                // identical.
                SignaturePlayer.CloseAtOccupancyBp = proto is PeakCloser ? 5000 : 0;

                for (int s = 0; s < seeds; s++)
                {
                    ulong seed = 20260910UL + (ulong)s * 7919UL;
                    IStrategy strategy = NewLike(proto);
                    RunOne(economy, content, timing, seed, days, strategy, agg, rows);
                }
                ReasonablePlayer.HallShort = 0;
                agg.InterventionsTried = Interventionist.Tried;
                agg.InterventionsApplied = Interventionist.Applied;
                agg.ExpansionsRefused = PlannerSchedule.Refused;

                agg.Finish(seeds);
                results.Add(agg);
            }

            // NO SILENT EMPTY TABLE. Someone who makes a typo would see an
            // empty report and conclude "so this bot does nothing".
            if (only != null && matches == 0)
            {
                Console.WriteLine($"ERROR: there is no strategy called '{only}'.");
                return 2;
            }

            Report(results);
            PlateReport(results);

            if (csv != null)
            {
                File.WriteAllText(csv, rows.ToString());
                Console.WriteLine($"\nCSV written: {csv}");
            }
            return 0;
        }

        private static IEnumerable<IStrategy> AllStrategies()
        {
            yield return new PassivePlayer();
            yield return new RestockOnly();
            yield return new ReasonablePlayer();
            yield return new ReasonablePlayer(expand: false);
            yield return new Expansionist();
            yield return new PlannerSchedule();
            yield return new GreedyPricer();
            yield return new GreedyPricer(11000, "orta_fiyat");
            yield return new ExtrasGouger();
            yield return new OverStaffer();
            yield return new CheapIngredients();
            yield return new Interventionist();
            yield return new PatientInterventionist();
            yield return new SignaturePlayer();
            yield return new PickyCreditor();
            yield return new EagerCollector();
            yield return new DishDuty();
            yield return new PeakCloser();
            yield return new ComboGouger();
            yield return new OneDishPlayer();
            yield return new WideMenuPlayer();
            yield return new NoLoanPlayer();
            yield return new CheapPricer();

            // THE PRESSURED PAIR: the ONLY difference between them is the
            // intervention.
            //
            // It had already been measured that intervention cannot be detected
            // in a comfortable restaurant (1440 of 1440 interventions went
            // through and the result did not change). This pair separates the
            // question "is the mechanic weak" from "is there nothing to save".
            yield return new PressuredPlayer(intervene: false);
            yield return new PressuredPlayer(intervene: true);
        }

        private static IStrategy NewLike(IStrategy proto)
        {
            switch (proto.Name)
            {
                case "pasif": return new PassivePlayer();
                case "sadece_hal": return new RestockOnly();
                case "makul": return new ReasonablePlayer();
                case "genislemeyen": return new ReasonablePlayer(expand: false);
                case "atilgan": return new Expansionist();
                case "planci": return new PlannerSchedule();
                case "yuksek_fiyat": return new GreedyPricer();
                case "orta_fiyat": return new GreedyPricer(11000, "orta_fiyat");
                case "pahali_ekstra": return new ExtrasGouger();
                case "fazla_kadro": return new OverStaffer();
                case "ucuz_malzeme": return new CheapIngredients();
                case "mudahaleci": return new Interventionist();
                case "sabirli_mudahale": return new PatientInterventionist();
                case "imzaci": return new SignaturePlayer();
                case "secici_veresiye": return new PickyCreditor();
                case "erken_tahsilat": return new EagerCollector();
                case "bulasikci": return new DishDuty();
                case "zirvede_kapat": return new PeakCloser();
                case "kombo_sismesi": return new ComboGouger();
                case "tek_yemek": return new OneDishPlayer();
                case "genis_menu": return new WideMenuPlayer();
                case "kredisiz": return new NoLoanPlayer();
                case "ucuz_fiyat": return new CheapPricer();
                case "baskili": return new PressuredPlayer(intervene: false);
                case "baskili_mudahale": return new PressuredPlayer(intervene: true);
                default: throw new InvalidOperationException(proto.Name);
            }
        }

        // -------------------------------------------------------------------
        private static void RunOne(EconomyConfig economy, ContentSet content,
                                   TimingConfig timing, ulong seed, int days,
                                   IStrategy strategy, StrategyResult agg,
                                   StringBuilder rows)
        {
            Simulation sim = new Simulation(economy, content, timing, seed);
            int limit = timing.ServiceTicks + 6000;

            long partiesLost = 0, peopleServed = 0, totalRevenue = 0, totalIngredients = 0;
            long plannedParties = 0, servedParties = 0, turnedAway = 0;
            int trivialWeek = 0;

            // The "the restaurant visibly emptied out" day: the day reputation
            // drops below the breaking point of the demand curve.
            //
            // docs/08 rejects a game over ("the save is not deleted, the game
            // does not end"), so the price of neglect is not the end of the
            // game. But the player must SEE that it is over. This column
            // measures that: however long the money in the till delays the
            // funeral, when did the place empty out.
            const int CollapseCenti = 2000;
            int collapseDay = 0;

            // PLATE PRESSURE: does the bottleneck really bite.
            //
            // docs/14 describes the dishwasher as a bottleneck, but once the
            // mechanic was added it had to be measured directly: "existing" and
            // "being felt" are different things, and in this project a
            // decorative mechanic is the worst possible outcome.
            int plateBlocked = 0, plateMinClean = int.MaxValue, plateMaxDirty = 0;

            for (int day = 1; day <= days; day++)
            {
                strategy.OnMorning(sim);

                sim.Apply(new Command(sim.TickIndex, CommandKind.OpenService));
                for (int t = 0; t < limit; t++)
                {
                    sim.Tick();

                    // Intervention DURING service. docs/02 core loop: "during
                    // service you only intervene in crises". Open only to that
                    // strategy; the others never use it, so that the difference
                    // can be measured.
                    // THE PATIENT ARM IS ASKED ON EVERY TICK.
                    //
                    // A 200-tick interval (20 sim-seconds) was enough for the
                    // interventionist because it spends at the first
                    // opportunity anyway. The patient arm WAITS FOR THE CRISIS,
                    // and the crisis window is 3 seconds: a bot that looks once
                    // every twenty seconds misses that window most of the time,
                    // and the measurement would have said "holding back does
                    // not pay" - when what it actually measured was its own
                    // blinking.
                    if (strategy is PatientInterventionist)
                        Interventionist.DuringService(sim);
                    else if ((t % 200) == 0
                        && (strategy is Interventionist
                            || (strategy is PressuredPlayer pp && pp.Intervenes)))
                        Interventionist.DuringService(sim);

                    // The signature mechanics also run during service: the tab
                    // opens at the moment of payment, the combo at the moment
                    // of ordering.
                    if ((t % 50) == 0)
                    {
                        // NO MORE CHAIN OF TYPE CHECKS.
                        //
                        // A strategy that was not in the chain silently did
                        // nothing and nothing said so. Thanks to the default
                        // empty body on the interface every strategy is called;
                        // writing the method is enough to take part.
                        strategy.DuringService(sim);
                    }

                    if (sim.PlatesClean < plateMinClean) plateMinClean = sim.PlatesClean;
                    if (sim.PlatesDirty > plateMaxDirty) plateMaxDirty = sim.PlatesDirty;
                    if (sim.ServiceComplete) break;
                }
                plateBlocked += sim.PlateBlockedTicks;
                sim.Apply(new Command(sim.TickIndex, CommandKind.CloseDay));

                DayReport r = sim.BuildDayReport();
                strategy.OnEvening(sim, r);

                peopleServed += r.ServedPeople;
                plannedParties += r.PlannedParties;
                servedParties += r.ServedParties;
                turnedAway += r.TurnedAwayParties;
                totalRevenue += r.Revenue;
                totalIngredients += r.IngredientCost;
                // PARTIES are counted, not people - and the field is named
                // accordingly. It used to be called "peopleLost" and Warnings()
                // compared it against the PEOPLE count: for the reasonable
                // player that is 2,042 people / 16 parties, threshold 408 - the
                // guard could never fire, mathematically.
                //
                // Also, those turned away at the door are NOW SEPARATE: a party
                // that leaves the table angry is a service problem, a party
                // turned away at the door is a capacity problem. Folded into
                // one number, you cannot read which trouble is growing.
                partiesLost += r.AngrySeatedParties;

                // "Money has stopped being a problem": if the till can pay for
                // ALL the remaining purchasables in one go, then the player has
                // nothing left to save up for.
                //
                // Both of the old definitions were wrong too. First it was
                // "three times the most expensive expansion"; that counted no
                // equipment at all. Then equipment was added to the same
                // threshold but the "three times" remained arbitrary: with
                // 30,000 in hand and two 20,000 pieces of equipment still on
                // the shelf, money still matters.
                long remaining = sim.RemainingPurchaseCost();
                if (trivialWeek == 0 && remaining > 0 && sim.Cash > remaining)
                    trivialWeek = (day + 6) / 7;

                if (collapseDay == 0 && sim.ReputationCenti < CollapseCenti)
                    collapseDay = day;

                rows.Append(strategy.Name).Append(',').Append(seed).Append(',')
                    .Append(day).Append(',').Append(sim.TableCount).Append(',')
                    .Append(sim.Cooks).Append(',').Append(sim.HallStaff).Append(',')
                    .Append(r.PlannedParties).Append(',').Append(r.ServedParties).Append(',')
                    .Append(r.AngrySeatedParties).Append(',').Append(r.ServedPeople).Append(',')
                    .Append(r.Revenue).Append(',').Append(r.AverageSatisfactionCenti).Append(',')
                    .Append(r.ReputationCenti).Append(',').Append(r.Cash).AppendLine();

                sim.AdvanceToNextDay();
            }

            // REVENUE IS READ CUMULATIVELY, not summed from the day reports.
            //
            // Summing produced a boundary remainder: AdvanceToNextDay() collects
            // the tab entries that have come due, but the day's report is taken
            // before that, so the last day's collection entered no report at all
            // and the reconciliation would not close. Adding the tail by hand
            // did not match exactly either; the right answer is to read the
            // simulation's own cumulative counter, as is done for wages and
            // rent. That way there is no boundary at all.
            totalRevenue = sim.TotalRevenue;

            agg.Add(sim, peopleServed, partiesLost, trivialWeek, totalRevenue,
                    totalIngredients, collapseDay);
            agg.AddFlow(plannedParties, servedParties, turnedAway);
            agg.AddPlates(plateBlocked,
                          plateMinClean == int.MaxValue ? 0 : plateMinClean,
                          plateMaxDirty);
        }

        // -------------------------------------------------------------------
        /// <summary>
        /// THE PLATE PRESSURE TABLE.
        ///
        /// docs/14 describes the dishwasher as a bottleneck. It is not enough
        /// for a bottleneck to "exist", it has to be FELT somewhere - a mechanic
        /// nobody feels is decoration, and in this project that is the worst
        /// possible outcome. This table asks exactly that question.
        /// </summary>
        private static void PlateReport(List<StrategyResult> results)
        {
            Console.WriteLine();
            Console.WriteLine("=== plate pressure (60 days, average) ===");
            Console.WriteLine("| strategy      | waited, no plate | min clean   | max dirty    |");
            Console.WriteLine("|---------------|-----------------:|------------:|-------------:|");
            foreach (StrategyResult r in results)
                Console.WriteLine($"| {r.Name,-13} | {r.AvgPlateBlocked,16:0} | "
                                  + $"{r.AvgPlateMinClean,11:0.0} | {r.AvgPlateMaxDirty,12:0.0} |");
        }

        private static void Report(List<StrategyResult> results)
        {
            Console.WriteLine("| strategy      | end cash |    tab | rep    | tabl | crew  | served | lost  | emptied | debt day | trivial |");
            Console.WriteLine("|---------------|---------:|-------:|-------:|-----:|------:|-------:|------:|--------:|---------:|--------:|");
            foreach (StrategyResult r in results)
            {
                Console.WriteLine(
                    $"| {r.Name,-13} | {Coin(r.AvgFinalCash),8} | " +
                    $"{(r.AvgOpenCredit > 0 ? Coin(r.AvgOpenCredit) : "-"),6} | " +
                    $"{r.AvgReputation / 100,6:0.0} | " +
                    $"{r.AvgTables,4:0.0} | {r.AvgStaff,5:0.0} | {r.AvgServed,6:0} | " +
                    $"{r.AvgLost,5:0} | {(r.AvgCollapseDay > 0 ? r.AvgCollapseDay.ToString("0") : "-"),7} | " +
                    $"{(r.AvgFirstDebtDay > 0 ? r.AvgFirstDebtDay.ToString("0") : "-"),8} | " +
                    $"{(r.AvgTrivialWeek > 0 ? r.AvgTrivialWeek.ToString("0.0") : "-"),7} |");
            }

            Console.WriteLine();
            Console.WriteLine("=== the 60-day income statement (average, coins) ===");
            // THERE IS A RECONCILIATION COLUMN: the gap between the net and the
            // real cash movement. It must be zero.
            //
            // The spoilage, equipment and expansion lines USED TO BE MISSING and
            // the "net" came out 2-20 times the real cash movement. The answers
            // to three design questions had the wrong sign: the tool said "the
            // high price still pays", when in reality it lost money. Every
            // balance decision taken by looking at these tables was suspect.
            // "tabl" AND "crew" ARE DAY-60 FIGURES, AND THE HEADER DID NOT SAY.
            //
            // Both are accumulated once per run at the end, beside the table
            // count, while every column around them is a campaign total. On
            // 19 September a Turkish non-expander read "crew 1.0" and
            // "wages 13,947": 232 a day is a cook AND a hall worker most of
            // the campaign, let go before day 60. It took three readings of
            // the table to see it. The row regex in calibrate.py keys on
            // lines that begin with "|", so this line is invisible to it.
            Console.WriteLine("  tabl and crew are the figures on day 60; "
                              + "every other column is the whole campaign.");
            Console.WriteLine("| strategy      | revenue |   rescue |   stock | spoiled |"
                              + "  tea |   wages |    rent |  invest |     net |   gap |");
            Console.WriteLine("|---------------|--------:|---------:|--------:|--------:|"
                              + "-----:|--------:|--------:|--------:|--------:|------:|");
            foreach (StrategyResult r in results)
            {
                // The reconciliation uses INGREDIENT SPEND, not the cost of
                // goods sold: what leaves the till is the purchase. Spoilage is
                // a separate column and is NOT DEDUCTED FROM THE NET - that
                // money already left when it was bought; spoilage is an
                // INFORMATION line showing how much of what was bought went to
                // waste.
                double ing = r.AvgIngredientSpend;
                double invest = r.AvgEquipment + r.AvgExpansion;

                // Rescue and the loan are CASH MOVEMENTS too: the bankruptcy
                // ladder sells equipment, the place shrinks and the remaining
                // debt is written off - all of it puts money into the till. If
                // it is invisible the reconciliation does not close and the gap
                // cannot be explained.
                double inflow = r.AvgRescue + r.AvgLoan;

                // Loan instalments are NOT in the rent column: TotalRentPaid
                // counts only the rent. If they are not deducted as a separate
                // outflow the reconciliation drifts by exactly the loan
                // repayment - 6,117 coins for the reasonable player.
                // TEA IS AN EXPENSE TOO.
                //
                // The tea offered when a tab is opened costs money out of the
                // till and was counted in NO column: in the Turkish cuisine
                // that was exactly the size of the signature bot's difference
                // (134 coins). An invisible expense does not merely break the
                // reconciliation - it makes the mechanic look cheaper than it is.
                double net = r.AvgRevenue + inflow - ing - r.AvgWages
                             - r.AvgRent - invest - r.AvgLoanRepaid - r.AvgTea;

                // Cash movement: from the starting till to today.
                //
                // THE OPEN TAB DOES NOT GO IN HERE. It was added once and it
                // BROKE the reconciliation: a bill written on the tab never
                // enters _revenue (it only enters when collected), so there is
                // no counterpart on the revenue side. In the Turkish cuisine
                // the signature player's gap came out at -2,441 and the column
                // said "must be zero".
                //
                // The open tab is not lost, but it IS NOT INCOME YET: neither in
                // the till nor in the revenue. The reconciliation closes once
                // it counts neither of them.
                double moved = r.AvgFinalCash - r.StartCash;
                double gap = net - moved;

                // The gap MUST BE ZERO and something must say when it is not:
                // the column read -2,441 for a long time and no warning came
                // out, because Warnings() never looked at the gap. A measuring
                // tool that does not say when it has broken is worse than
                // believing a wrong measurement is right.
                if (System.Math.Abs(gap) > 100)
                    r.Reconciliation = gap;

                Console.WriteLine(
                    $"| {r.Name,-13} | {Coin(r.AvgRevenue),7} | {Coin(inflow),8} | " +
                    $"{Coin(ing),7} | {Coin(r.AvgSpoiled),7} | {Coin(r.AvgTea),5} | " +
                    $"{Coin(r.AvgWages),7} | " +
                    $"{Coin(r.AvgRent),7} | {Coin(invest),7} | {Coin(net),7} | " +
                    $"{Coin(gap),5} |");
            }

            // THE YEAR-END SCORE: an answer OTHER than the till.
            //
            // docs/08 scores the campaign on seven axes and that is the result
            // the player sees. "End cash" on its own is misleading: a player who
            // runs short-staffed and hoards the money may be losing on the
            // reputation, crew and regulars axes - or may not be. The table
            // answers that question.
            Console.WriteLine();
            Console.WriteLine("=== year-end score (docs/08, 0-100) ===");
            Console.WriteLine("| strategy      | score | wealth | rep    | regulars "
                              + "| crew | place | resil  | sig  | combo% |");
            Console.WriteLine("|---------------|------:|-------:|-------:|---------:"
                              + "|-----:|------:|-------:|-----:|-------:|");
            foreach (StrategyResult r in results)
                Console.WriteLine(
                    $"| {r.Name,-13} | {r.AvgScore,5:0} | {r.AvgScoreWealth,6:0} | "
                    + $"{r.AvgScoreRep,6:0} | {r.AvgScoreRegulars,8:0} | "
                    + $"{r.AvgScoreCrew,4:0} | {r.AvgScorePlace,5:0} | "
                    + $"{r.AvgScoreResilience,6:0} | {r.AvgScoreSignature,4:0} | "
                    + $"{r.AvgComboShareBp / 100,6:0.0} |");

            Console.WriteLine();
            Console.WriteLine("=== docs/12 8: the questions the tool answers ===");
            foreach (StrategyResult r in results)
            {
                Console.WriteLine($"\n{r.Question}");
                Console.WriteLine($"  -> {r.Verdict()}");
            }

            // DID THE INTERVENTION ACTUALLY HAPPEN.
            //
            // "A bot that gets refused is not a bot": once the price ceiling
            // arrived the high-price bot's commands were being rejected and the
            // bot had silently become a copy of the reasonable player. Before
            // asking whether the intervention PAYS, you have to ask whether it
            // happened at all.
            // PER ARM. It used to be printed on a single line as the sum of the
            // two arms, and you could not read how many of which arm's
            // interventions went through - which was exactly why the counters
            // were written in the first place.
            Console.WriteLine();
            Console.WriteLine("=== interventions (per strategy) ===");
            bool anyIntervention = false;
            foreach (StrategyResult r in results)
            {
                if (r.InterventionsTried == 0) continue;
                anyIntervention = true;
                Console.WriteLine($"  {r.Name,-20} {r.InterventionsApplied,6} applied / "
                                  + $"{r.InterventionsTried,6} tried");
            }
            if (!anyIntervention) Console.WriteLine("  none");

            foreach (StrategyResult r in results)
            {
                if (r.ExpansionsRefused == 0) continue;
                Console.WriteLine($"  {r.Name,-20} {r.ExpansionsRefused,6} scheduled "
                                  + "expansions refused before they went through");
            }

            Console.WriteLine();
            Console.WriteLine("=== Warnings ===");
            int warnings = 0;
            foreach (StrategyResult r in results)
            {
                foreach (string w in r.Warnings())
                {
                    Console.WriteLine("  ! " + w);
                    warnings++;
                }
            }
            if (warnings == 0) Console.WriteLine("  none");
        }

        private static string Coin(double centi)
        {
            return (centi / 100.0).ToString("N0", CultureInfo.InvariantCulture);
        }

        // -------------------------------------------------------------------
        private static string FindRoot()
        {
            DirectoryInfo d = new DirectoryInfo(AppContext.BaseDirectory);
            while (d != null)
            {
                if (File.Exists(Path.Combine(d.FullName, "Lokanta.slnx"))
                    || File.Exists(Path.Combine(d.FullName, "Lokanta.sln")))
                    return d.FullName;
                d = d.Parent;
            }
            throw new InvalidOperationException("Repository root not found");
        }

        /// <summary>
        /// Every flag we recognise. Anything else is an error.
        ///
        /// "--nologo" is not ours: "dotnet run --nologo" does not recognise it
        /// and so PASSES IT ON to the application. The whole calibration tool
        /// stopped on the check's first run because of this; the flag is on the
        /// list so that our own guard tools do not block one another.
        /// </summary>
        private static readonly string[] KnownFlags =
        {
            "--seeds", "--days", "--csv", "--cuisine", "--strategy",
            "--storage-cap", "--solve",
            "--nologo",
        };

        // RentSolver WAS UNREACHABLE BECAUSE "--solve" WAS NOT ON THE LIST:
        // CheckArgs did not recognise the flag, so the tool said "Unknown flag"
        // and exited with 2. The rent solver was written and could never be run.
        //
        // "--tohum" CAME OFF THE LIST: it was accepted but there was not a
        // single read of it in the code. Someone typing "--tohum 3" got no
        // error, got the full run, and believed they had filtered - the very
        // danger CheckArgs was written to prevent, INSIDE the list itself.

        private static bool CheckArgs(string[] a)
        {
            bool ok = true;
            for (int i = 0; i < a.Length; i++)
            {
                if (!a[i].StartsWith("--", StringComparison.Ordinal)) continue;

                bool known = false;
                for (int k = 0; k < KnownFlags.Length; k++)
                    if (a[i] == KnownFlags[k]) { known = true; break; }

                if (!known)
                {
                    Console.Error.WriteLine("Unknown flag: " + a[i]);
                    ok = false;
                }
            }
            if (!ok)
                Console.Error.WriteLine("Recognised flags: " + string.Join(" ", KnownFlags));
            return ok;
        }

        private static int ArgInt(string[] a, string name, int fallback)
        {
            for (int i = 0; i + 1 < a.Length; i++)
                if (a[i] == name && int.TryParse(a[i + 1], NumberStyles.Integer,
                                                 CultureInfo.InvariantCulture, out int v))
                    return v;
            return fallback;
        }

        private static string ArgStr(string[] a, string name, string fallback)
        {
            for (int i = 0; i + 1 < a.Length; i++)
                if (a[i] == name) return a[i + 1];
            return fallback;
        }
    }

    // ======================================================================
    public sealed class StrategyResult
    {
        public string Name { get; }
        public string Question { get; }

        private long _cash, _reputation, _tables, _staff, _served, _lost, _openCredit;
        private long _spoiled, _equipment, _expansion, _ingredientSpend;
        private long _rescue, _loan, _loanRepaid;
        private int _debtDays, _debtCount, _trivialWeeks, _trivialCount, _runs;
        private int _collapseDays, _collapseCount;

        public double AvgFinalCash, AvgReputation, AvgTables, AvgStaff, AvgServed, AvgLost;
        /// <summary>PARTIES served per run; the same unit as AvgLost.</summary>
        public double AvgServedParties;

        /// <summary>What share of the main dishes became combos (basis points).</summary>
        public double AvgComboShareBp;
        private long _comboShareBp;

        /// <summary>This strategy's own intervention counters.</summary>
        public int InterventionsTried, InterventionsApplied;

        /// <summary>
        /// Mornings on which a scheduled expansion was turned down. It
        /// separates "the model's calendar is unaffordable" from "the bot
        /// asked on the wrong day", which the table could not tell apart
        /// while a refusal silently burnt the slot.
        /// </summary>
        public int ExpansionsRefused;
        /// <summary>
        /// Money still ON THE TAB on the sixtieth day. Not in the till, but not
        /// lost either - the docs/08 year-end evaluation measures net worth, and
        /// an uncollected tab is part of net worth. A separate column, because
        /// the "end cash" targets were not calibrated with it included.
        /// </summary>
        public double AvgOpenCredit;
        public double AvgFirstDebtDay, AvgTrivialWeek;
        /// <summary>The day reputation fell below 20; the moment the place emptied out.</summary>
        public double AvgCollapseDay;
        public double AvgRevenue, AvgWages, AvgRent, AvgTicket, AvgIngredients;
        public double AvgSpoiled, AvgEquipment, AvgExpansion, AvgIngredientSpend;

        /// <summary>The cost of the tab's tea - an INVISIBLE expense.</summary>
        public double AvgTea;
        private long _teaSpend;

        /// <summary>The income statement's reconciliation gap. Non-zero = the tool is broken.</summary>
        public double Reconciliation;
        public double AvgRescue, AvgLoan, AvgLoanRepaid;
        public double StartCash;
        public double DebtShare;

        public StrategyResult(string name, string question)
        {
            Name = name; Question = question;
        }

        private long _revenue, _wages, _rent, _ingredients;
        private long _planned, _servedP, _turned;

        public double ServiceRate => _planned > 0 ? (double)_servedP / _planned : 0;
        public double TurnAwayRate => _planned > 0 ? (double)_turned / _planned : 0;

        /// <summary>How much of the demand was served. The closed-form model assumes 100%.</summary>
        public void AddFlow(long planned, long served, long turned)
        {
            _planned += planned; _servedP += served; _turned += turned;
        }

        private long _score, _scoreWealth, _scoreRep, _scoreRegulars;
        private long _scoreCrew, _scorePlace, _scoreResilience, _scoreSignature;

        /// <summary>The year-end score (0-100) and its seven axes.</summary>
        public double AvgScore { get { return _runs == 0 ? 0 : (double)_score / _runs; } }
        public double AvgScoreWealth { get { return _runs == 0 ? 0 : (double)_scoreWealth / _runs; } }
        public double AvgScoreRep { get { return _runs == 0 ? 0 : (double)_scoreRep / _runs; } }
        public double AvgScoreRegulars { get { return _runs == 0 ? 0 : (double)_scoreRegulars / _runs; } }
        public double AvgScoreCrew { get { return _runs == 0 ? 0 : (double)_scoreCrew / _runs; } }
        public double AvgScorePlace { get { return _runs == 0 ? 0 : (double)_scorePlace / _runs; } }
        public double AvgScoreResilience { get { return _runs == 0 ? 0 : (double)_scoreResilience / _runs; } }
        public double AvgScoreSignature { get { return _runs == 0 ? 0 : (double)_scoreSignature / _runs; } }

        private long _plateBlocked, _plateMinClean, _plateMaxDirty;
        private int _plateRuns;

        /// <summary>Plate pressure: does the bottleneck bite.</summary>
        public void AddPlates(int blocked, int minClean, int maxDirty)
        {
            _plateBlocked += blocked;
            _plateMinClean += minClean;
            _plateMaxDirty += maxDirty;
            _plateRuns++;
        }

        public double AvgPlateBlocked
        {
            get { return _plateRuns == 0 ? 0 : (double)_plateBlocked / _plateRuns; }
        }
        public double AvgPlateMinClean
        {
            get { return _plateRuns == 0 ? 0 : (double)_plateMinClean / _plateRuns; }
        }
        public double AvgPlateMaxDirty
        {
            get { return _plateRuns == 0 ? 0 : (double)_plateMaxDirty / _plateRuns; }
        }

        public void Add(Simulation sim, long served, long lost, int trivialWeek,
                        long revenue, long ingredients, int collapseDay)
        {
            _runs++;
            if (collapseDay > 0) { _collapseDays += collapseDay; _collapseCount++; }
            _revenue += revenue;
            _ingredients += ingredients;
            _wages += sim.TotalWagesPaid;
            _rent += sim.TotalRentPaid;
            _cash += sim.Cash;
            _spoiled += sim.SpoiledValue;
            _teaSpend += sim.TeaSpend;
            _ingredientSpend += sim.IngredientSpend;
            _rescue += sim.RescueValue;
            _loan += sim.LoanTaken;
            _loanRepaid += sim.LoanRepaid;
            _equipment += sim.EquipmentSpend;
            _expansion += sim.ExpansionSpend;
            _openCredit += sim.OpenCredit;
            // THE YEAR-END SCORE: what docs/08 is really about.
            //
            // This column was added to measure a CLAIM. In docs/42 I had written
            // "short-staffing wins the money and loses the SCORE" - and that
            // sentence had never been measured. The rule of this project is
            // plain: an unmeasured sentence is a guess sitting in a document.
            SeasonScore score = sim.Score();
            _score += score.Total;
            _scoreWealth += score.Wealth;
            _scoreRep += score.Reputation;
            _scoreRegulars += score.Regulars;
            _scoreCrew += score.Crew;
            _scorePlace += score.Place;
            _scoreResilience += score.Resilience;
            _scoreSignature += score.Signature;

            // THE RAW COMBO SHARE. The signature axis's TARGET will come from
            // this measurement; an invented target makes the axis either
            // saturated or unreachable.
            _comboShareBp += sim.ComboShareBp;

            _reputation += sim.ReputationCenti;
            _tables += sim.TableCount;
            _staff += sim.Cooks + sim.HallStaff;
            _served += served;
            _lost += lost;
            if (sim.FirstDebtDay > 0) { _debtDays += sim.FirstDebtDay; _debtCount++; }
            if (trivialWeek > 0) { _trivialWeeks += trivialWeek; _trivialCount++; }
        }

        public void Finish(int seeds)
        {
            if (_runs == 0) return;
            AvgFinalCash = (double)_cash / _runs;
            AvgOpenCredit = (double)_openCredit / _runs;
            AvgReputation = (double)_reputation / _runs;
            AvgTables = (double)_tables / _runs;
            AvgStaff = (double)_staff / _runs;
            AvgServed = (double)_served / _runs;
            AvgLost = (double)_lost / _runs;
            AvgServedParties = (double)_servedP / _runs;
            AvgComboShareBp = (double)_comboShareBp / _runs;
            AvgRevenue = (double)_revenue / _runs;
            AvgIngredients = (double)_ingredients / _runs;
            AvgSpoiled = (double)_spoiled / _runs;
            AvgTea = (double)_teaSpend / _runs;
            AvgIngredientSpend = (double)_ingredientSpend / _runs;
            AvgRescue = (double)_rescue / _runs;
            AvgLoan = (double)_loan / _runs;
            AvgLoanRepaid = (double)_loanRepaid / _runs;
            AvgEquipment = (double)_equipment / _runs;
            AvgExpansion = (double)_expansion / _runs;
            AvgWages = (double)_wages / _runs;
            AvgRent = (double)_rent / _runs;
            AvgTicket = _served > 0 ? (double)_revenue / _served : 0;
            AvgFirstDebtDay = _debtCount > 0 ? (double)_debtDays / _debtCount : 0;
            AvgTrivialWeek = _trivialCount > 0 ? (double)_trivialWeeks / _trivialCount : 0;
            AvgCollapseDay = _collapseCount > 0 ? (double)_collapseDays / _collapseCount : 0;
            DebtShare = (double)_debtCount / _runs;
        }

        public string Verdict()
        {
            string cash = (AvgFinalCash / 100.0).ToString("N0", CultureInfo.InvariantCulture);
            if (DebtShare > 0.5)
                return $"{DebtShare * 100:0}% of the runs fell into debt, on average on day {AvgFirstDebtDay:0}; " +
                       $"end cash {cash}";
            return $"end cash {cash}, reputation {AvgReputation / 100:0.0}, " +
                   $"{AvgServed:0} people served, {AvgLost:0} parties left the table angry";
        }

        public IEnumerable<string> Warnings()
        {
            // docs/12 8.3: in which week does the economy stop mattering
            if (AvgTrivialWeek > 0 && AvgTrivialWeek < 8)
                yield return $"{Name}: money stops being a problem in week {AvgTrivialWeek:0.0} " +
                             "(target: not before week 8)";

            if (Name == "makul" && DebtShare > 0.2)
                yield return $"the reasonable player falls into debt in {DebtShare * 100:0}% of runs; " +
                             "the economy is too harsh";

            // THE THRESHOLD MUST BE IN THE SAME UNIT.
            //
            // AvgLost counts PARTIES, AvgServed counts PEOPLE. The old line
            // compared the two directly: in the measured run that is 2,042
            // people / 16 parties, so the threshold was 408 - the guard could
            // never fire. On top of that the text said "a quarter" while the
            // arithmetic did "a fifth".
            //
            // Now parties are compared with parties and the threshold matches
            // the text.
            if (Name == "makul" && AvgServedParties > 0
                && AvgLost > AvgServedParties / 4)
                yield return $"the reasonable player sends more than a quarter of the "
                             + $"seated parties away angry ({AvgLost:0} / {AvgServedParties:0})";

            // SPOILAGE is a visible warning.
            //
            // Until it was added to the income statement it was counted nowhere
            // and the "net" column was a multiple of the real cash movement. Now
            // it is counted, and what comes out is this: even a player who plays
            // well throws away half the stock they buy. That is a balance
            // question and it must at least be visible.
            // FOR EVERY STRATEGY. It used to be checked only for "makul" and the
            // real offenders were silent: measured - in the Turkish cuisine
            // sadece_hal spoils 50.6%, fazla_kadro 47.7%, yuksek_fiyat 65.7%,
            // and the tool printed "Warnings: none", because makul was under the
            // threshold at 29%.
            if (AvgIngredientSpend > 0 && AvgSpoiled > AvgIngredientSpend * 0.4)
                yield return $"{Name} throws away "
                             + $"{AvgSpoiled * 100 / AvgIngredientSpend:0}% of the stock it buys";

            // The tool MUST SAY when it has broken. The gap column read -2,441
            // for a long time and no warning came out.
            if (Reconciliation != 0)
                yield return $"{Name}'s income statement DOES NOT RECONCILE: gap "
                             + $"{Reconciliation / 100.0:N0} coins";

            if (Name == "pasif" && DebtShare < 0.5)
                yield return "the passive player never falls into debt; "
                             + "there is no price for not intervening";
        }
    }
}
