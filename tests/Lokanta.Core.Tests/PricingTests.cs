using System.Collections.Generic;
using System.IO;
using Lokanta.Content;
using Lokanta.Core.Content;
using Lokanta.Core.Economy;
using Lokanta.Core.Sim;
using Xunit;
using Xunit.Abstractions;

namespace Lokanta.Core.Tests
{
    /// <summary>
    /// THE PRICE'S DIRECT EFFECT ON DEMAND.
    ///
    /// This channel once DID NOT EXIST AT ALL and it was the game's biggest design
    /// hole. The price's only route was satisfaction -> reputation; and reputation
    /// is CLAMPED to the table tier's ceiling. So for a player sitting at the
    /// ceiling a loss of satisfaction bought nothing at all, and a small markup
    /// was FREE.
    ///
    /// The balance harness measured it (24 seeds, 60 days, fast food): the bot
    /// pricing 10% above the market finished on 27,849 coins; the game's most
    /// developed strategy on 25,092 and the baseline strategy on 18,670. A button
    /// pressed once in the morning beat everything with FEWER tables and a SMALLER
    /// crew. The penalty existed only outside the band (at a 30% markup the
    /// reputation is wiped out and the place goes under); in between there was
    /// nothing.
    ///
    /// This file tests that the channel RUNS. If the channel is deleted, or if a
    /// call site goes back to DemandModel.CustomersPerDay directly, one of these
    /// tests breaks.
    /// </summary>
    public sealed class PricingTests
    {
        private readonly ITestOutputHelper _out;
        public PricingTests(ITestOutputHelper o) { _out = o; }

        private const ulong Seed = 20260913UL;

        private static EconomyConfig Economy() => ContentLoader.LoadEconomy(Paths.Content);
        private static ContentSet Content() => ContentSetLoader.Load(Paths.Content, "fastfood");

        private static TimingConfig Timing()
        {
            ContentSet c = Content();
            return c.SlotDurationsBp != null
                ? TimingConfig.Default().WithSlotDurations(c.SlotDurationsBp).WithEatMs(c.EatMs)
                : TimingConfig.Default();
        }

        private static Simulation NewSim() =>
            new Simulation(Economy(), Content(), Timing(), Seed);

        /// <summary>Puts every unlocked dish on the menu.</summary>
        private static void OpenTheMenu(Simulation sim)
        {
            for (int i = 0; i < sim.DishCount; i++)
                if (sim.IsUnlocked(i))
                    sim.Apply(new Command(sim.TickIndex, CommandKind.SetMenuSlot, i, 1));
        }

        /// <summary>Pulls every dish on the menu to the given ratio of the market price.</summary>
        private static void PriceAll(Simulation sim, int markupBp)
        {
            for (int i = 0; i < sim.DishCount; i++)
            {
                long market = sim.BasePriceOf(i);
                if (market <= 0) continue;
                sim.Apply(new Command(sim.TickIndex, CommandKind.SetPrice,
                                      i, (int)Fx.Bp(market, markupBp)));
            }
        }

        [Fact]
        public void A_markup_lowers_demand()
        {
            Simulation baseline = NewSim();
            int atMarket = baseline.ExpectedPeopleToday();

            Simulation dearer = NewSim();
            PriceAll(dearer, 11000);                // 10% above the market
            int dearerDemand = dearer.ExpectedPeopleToday();

            _out.WriteLine($"at market {atMarket} people, 10% dearer {dearerDemand} people");

            // LIVENESS FIRST: if the baseline demand were zero the comparison
            // below would stay green in a vacuum.
            Assert.True(atMarket > 0, "the baseline demand is zero - the measurement did not run");
            Assert.True(dearerDemand < atMarket,
                $"a markup does not lower demand ({atMarket} -> {dearerDemand})");
        }

        [Fact]
        public void A_discount_raises_demand()
        {
            Simulation baseline = NewSim();
            int atMarket = baseline.ExpectedPeopleToday();

            Simulation cheaper = NewSim();
            PriceAll(cheaper, 9000);                // 10% below the market
            int cheaperDemand = cheaper.ExpectedPeopleToday();

            _out.WriteLine($"at market {atMarket} people, 10% cheaper {cheaperDemand} people");

            Assert.True(atMarket > 0, "the baseline demand is zero - the measurement did not run");
            Assert.True(cheaperDemand > atMarket,
                $"a discount does not raise demand ({atMarket} -> {cheaperDemand})");
        }

        /// <summary>
        /// DEMAND PASSES THROUGH A SINGLE GATE.
        ///
        /// The daily customer count is asked for in SIX separate places: today's
        /// crew, tomorrow's crew, the peak crew, the recommended stock, the
        /// expected people and the arrival plan. Missing one of them while adding
        /// the price channel would mean that screen giving advice based on
        /// customers WHO WILL NOT ACTUALLY COME - e.g. the market would still
        /// recommend stock for a busy day to a restaurant that had raised its
        /// prices, and the player's money would go in the bin. Silent, and visible
        /// on no screen.
        ///
        /// I TRIED MEASURING THE CONDITION THROUGH BEHAVIOUR AND IT DID NOT WORK:
        /// the crew and the stock are integers and on day 1 they are already at
        /// the floor (peak crew 1+0, the opening stock covers even a wide menu),
        /// so the difference rounds away and the test stayed green IN A VACUUM.
        /// The invariant itself is structural: "inside Simulation, only
        /// ExpectedCustomers calls CustomersPerDay". Measuring that directly in
        /// the source is both exact and breakable.
        /// </summary>
        [Fact]
        public void Demand_passes_through_a_single_gate()
        {
            string path = Path.Combine(Paths.Root, "unity", "Assets", "Lokanta",
                                       "Core", "Sim", "Simulation.cs");
            Assert.True(File.Exists(path), "Simulation.cs not found: " + path);

            string[] lines = File.ReadAllLines(path);

            // DID THE SCAN ACTUALLY HAPPEN: if the file read empty the test would
            // stay green saying "no violations".
            Assert.True(lines.Length > 1000,
                $"only {lines.Length} lines of Simulation.cs were read - the path may be wrong");

            List<string> violations = new List<string>();
            bool insideHelper = false;

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];

                if (line.Contains("private int ExpectedCustomers(")) insideHelper = true;
                else if (insideHelper && line.StartsWith("        }")) insideHelper = false;

                // Comment lines do not count: the name appears in the reasoning.
                string trimmed = line.TrimStart();
                if (trimmed.StartsWith("//") || trimmed.StartsWith("///")) continue;

                if (line.Contains("DemandModel.CustomersPerDay") && !insideHelper)
                    violations.Add($"line {i + 1}: {trimmed}");
            }

            Assert.True(violations.Count == 0,
                "Demand is computed outside ExpectedCustomers - the price channel "
                + "DOES NOT RUN at that call site: " + string.Join("; ", violations));
        }

        /// <summary>
        /// AT THE MARKET PRICE THE CHANNEL IS SILENT.
        ///
        /// With no deviation the multiplier must be exactly 1. If it is not, the
        /// channel shifts the whole balance and nothing says so: every
        /// market-priced strategy in the harness would drift at the same time, in
        /// the same direction.
        /// </summary>
        [Fact]
        public void At_the_market_price_demand_does_not_change()
        {
            Simulation baseline = NewSim();
            int atMarket = baseline.ExpectedPeopleToday();

            Simulation same = NewSim();
            PriceAll(same, 10000);                  // exactly the market
            int sameDemand = same.ExpectedPeopleToday();

            Assert.True(atMarket > 0, "the baseline demand is zero - the measurement did not run");
            Assert.Equal(atMarket, sameDemand);
        }

        /// <summary>
        /// THE TEA GOES TO THE WHOLE HALL, NOT TO A SINGLE TABLE.
        ///
        /// The old version left one of the three actions DEAD: the tea was below
        /// the owner's attention on every axis (900 satisfaction against 2400,
        /// patience x1 against x2, it does not speed the kitchen up) and on top of
        /// that it took money out of the till - attention is free. Because they
        /// burn the same budget there was no day at all on which to press the tea.
        ///
        /// This test measures its new job: ONE tea must extend the patience of
        /// SEVERAL waiting tables. If the tea goes back to a single table the
        /// number stays at 1 and the test breaks.
        /// </summary>
        [Fact]
        public void The_tea_goes_to_everyone_waiting()
        {
            Simulation sim = NewSim();
            OpenTheMenu(sim);
            sim.Apply(new Command(sim.TickIndex, CommandKind.OpenService));

            // Advance until the hall fills: at least two tables must be WAITING,
            // otherwise "it went to all of them" cannot be told apart from "it went
            // to one of them" and the measurement stays green IN A VACUUM.
            int waiting = 0;
            for (int t = 0; t < 4000 && waiting < 2; t++)
            {
                sim.Tick();
                waiting = sim.WaitingParties;
            }

            Assert.True(waiting >= 2,
                $"two waiting tables never formed ({waiting}) - the measurement did not run");

            int budgetBefore = sim.InterventionsLeft;
            int extendedBefore = sim.PartiesWithTea;
            sim.Apply(new Command(sim.TickIndex, CommandKind.Intervene,
                                  -1, (int)InterventionKind.FreeTea));

            Assert.True(sim.InterventionsLeft < budgetBefore,
                "the tea burnt no budget - the command was rejected");
            _out.WriteLine($"{waiting} waiting tables, {sim.PartiesWithTea - extendedBefore} got tea");
            Assert.True(sim.PartiesWithTea - extendedBefore >= 2,
                $"the tea went to only {sim.PartiesWithTea - extendedBefore} tables, {waiting} were waiting");
        }

        /// <summary>
        /// REPUTATION EARNED AT THE CEILING IS NOT DELETED, IT BANKS UP.
        ///
        /// A ceiling is the right idea, but deleting the overflow made a PERFECT
        /// day and a day that merely COPED identical for a player at the ceiling -
        /// and someone playing well spends more than half the campaign there.
        ///
        /// The test FORCES the ceiling, measures that the bank forms, and then that
        /// it is PAID OUT on expansion. It breaks if the bank is deleted, or if it
        /// is not paid out on expansion.
        /// </summary>
        [Fact]
        public void Reputation_earned_at_the_ceiling_is_paid_out_on_expansion()
        {
            // THE CEILING IS LOWERED FROM THE CONTENT.
            //
            // In the real content the ceiling at four tables is 55 and a small
            // restaurant's natural equilibrium is ~34.6: the ceiling is NOT binding
            // there at all, so no overflow ever forms. I first tried to get past
            // that with crew and expansion - the reputation stalled at 3730 over
            // 120 days against a ceiling of 7500. Reaching the ceiling would have
            // meant the test playing as well as the harness bot, that is, writing a
            // bot inside the test.
            //
            // The rule is the same rule; only the threshold at which it becomes
            // visible is brought closer. The ceiling is pulled down to 32 points
            // (BELOW the ~34.6 equilibrium) and the second tier to 60 - so that the
            // payout can be seen.
            EconomyConfig baseEconomy = Economy();
            TierConfig[] tiers = new TierConfig[baseEconomy.TierCount];
            for (int i = 0; i < tiers.Length; i++)
            {
                TierConfig t = baseEconomy.TierAt(i);
                tiers[i] = new TierConfig(t.Tables, t.Rent, t.Upgrade, t.StaffCap,
                                          i == 0 ? 3200 : 6000, t.Plates);
            }

            Simulation sim = new Simulation(baseEconomy.WithTiers(tiers), Content(), Timing(), Seed);
            OpenTheMenu(sim);
            sim.Apply(new Command(sim.TickIndex, CommandKind.Hire, 1, 0));

            int day = 0;
            while (day < 40 && sim.ReputationOverflowCenti <= 0)
            {
                sim.Apply(new Command(sim.TickIndex, CommandKind.OrderRecommended));
                sim.Apply(new Command(sim.TickIndex, CommandKind.OpenService));
                while (!sim.ServiceComplete) sim.Tick();
                sim.Apply(new Command(sim.TickIndex, CommandKind.CloseDay));
                day++;
            }

            _out.WriteLine($"day {day}: reputation {sim.ReputationCenti}, "
                           + $"ceiling {sim.ReputationCapCenti}, "
                           + $"banked {sim.ReputationOverflowCenti}");

            // LIVENESS: if no bank ever formed, everything below would stay green in
            // a vacuum.
            Assert.True(sim.ReputationOverflowCenti > 0,
                $"no bank formed at the ceiling in {day} days - the measurement did not run "
                + $"(reputation {sim.ReputationCenti}, ceiling {sim.ReputationCapCenti})");
            Assert.Equal(sim.ReputationCapCenti, sim.ReputationCenti);

            int banked = sim.ReputationOverflowCenti;
            int ceilingBefore = sim.ReputationCapCenti;

            sim.Apply(new Command(sim.TickIndex, CommandKind.Expand, 1));

            Assert.True(sim.ReputationCapCenti > ceilingBefore,
                $"the expansion did not raise the ceiling - the measurement did not run (till {sim.Cash})");
            Assert.Equal(0, sim.ReputationOverflowCenti);
            Assert.True(sim.ReputationCenti > ceilingBefore,
                $"the banked {banked} centi were not paid out (reputation {sim.ReputationCenti})");
        }

        /// <summary>
        /// THE DAILY DEMAND DEVIATES FROM THE EXPECTATION - but the forecast does not.
        ///
        /// The demand used to be entirely deterministic: every Tuesday with the
        /// same reputation and table count brought exactly the same customers. The
        /// result was that the morning stock decision was a button rather than a
        /// JUDGEMENT - the market's recommendation was always exactly right.
        ///
        /// What the test measures is the DISTINCTION: what happens deviates, the
        /// forecast does not. If the deviation showed up in the forecast too the
        /// player would again have certain knowledge and the volatility would stay
        /// decoration.
        /// </summary>
        [Fact]
        public void The_daily_demand_deviates_from_the_expectation()
        {
            Simulation sim = NewSim();
            OpenTheMenu(sim);

            int deviatingDays = 0, measuredDays = 0;
            long totalGap = 0;

            for (int g = 0; g < 20; g++)
            {
                int expected = sim.ExpectedPeopleToday();
                sim.Apply(new Command(sim.TickIndex, CommandKind.OrderRecommended));
                sim.Apply(new Command(sim.TickIndex, CommandKind.OpenService));

                int actual = sim.PlannedPeopleToday;
                if (expected > 0)
                {
                    measuredDays++;
                    if (actual != expected) deviatingDays++;
                    totalGap += actual - expected;
                }

                while (!sim.ServiceComplete) sim.Tick();
                sim.Apply(new Command(sim.TickIndex, CommandKind.CloseDay));
            }

            _out.WriteLine($"{measuredDays} days measured, {deviatingDays} days deviated, "
                           + $"total gap {totalGap} people");

            // LIVENESS: if no day was measured, everything below would stay green in
            // a vacuum.
            Assert.True(measuredDays >= 15, $"only {measuredDays} days were measured");

            // Most of the days should deviate; not all of them (zero deviation is a
            // valid draw too).
            Assert.True(deviatingDays >= measuredDays / 2,
                $"only {deviatingDays} of {measuredDays} days deviate - "
                + "the volatility may not be running");
        }

        /// <summary>
        /// THE VOLATILITY DOES NOT BREAK DETERMINISM.
        ///
        /// The same seed must give the same result; otherwise replay and the golden
        /// data break (docs/23 2.5). The draw comes from the _rngEvent stream and
        /// that stream goes into the save.
        /// </summary>
        [Fact]
        public void The_volatility_is_the_same_for_the_same_seed()
        {
            int[] Run()
            {
                Simulation sim = NewSim();
                OpenTheMenu(sim);
                int[] days = new int[10];
                for (int g = 0; g < days.Length; g++)
                {
                    sim.Apply(new Command(sim.TickIndex, CommandKind.OrderRecommended));
                    sim.Apply(new Command(sim.TickIndex, CommandKind.OpenService));
                    days[g] = sim.PlannedPeopleToday;
                    while (!sim.ServiceComplete) sim.Tick();
                    sim.Apply(new Command(sim.TickIndex, CommandKind.CloseDay));
                }
                return days;
            }

            int[] a = Run();
            int[] b = Run();

            Assert.True(a[0] > 0, "zero people on day one - the measurement did not run");
            Assert.Equal(a, b);
        }
    }
}
