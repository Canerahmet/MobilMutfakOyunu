using Lokanta.Content;
using Lokanta.Core.Content;
using Lokanta.Core.Economy;
using Lokanta.Core.Sim;
using Xunit;
using Xunit.Abstractions;

namespace Lokanta.Core.Tests
{
    /// <summary>
    /// THE DEDICATED DISHWASHER: putting one hall worker on the sink.
    ///
    /// The mechanic existed in the interface and in the core, but no balance bot
    /// used it (docs/49). An attempt was made to measure it in the harness and
    /// the measurement came out DIRTY: the arm that assigned a dishwasher
    /// finished with a smaller restaurant, so it was impossible to tell whether
    /// the difference in the till came from the dishwashing or from the
    /// difference in growth.
    ///
    /// This file removes that confusion: THE TWO ARMS RUN EXACTLY THE SAME
    /// player, the only difference being `SetDishwashers`. The same seed, the
    /// same decisions, the same days - the only thing that varies is the thing
    /// being measured.
    ///
    /// (It is also a necessity: Smart App Control blocks the harness binary on
    /// this machine and switching it off is not allowed.)
    /// </summary>
    public sealed class DishwasherTests
    {
        private readonly ITestOutputHelper _out;
        public DishwasherTests(ITestOutputHelper o) { _out = o; }

        private const ulong Seed = 20260915UL;

        private static EconomyConfig Economy() => ContentLoader.LoadEconomy(Paths.Content);
        private static ContentSet Content() => ContentSetLoader.Load(Paths.Content, "turk");

        private static TimingConfig Timing()
        {
            ContentSet c = Content();
            return c.SlotDurationsBp != null
                ? TimingConfig.Default().WithSlotDurations(c.SlotDurationsBp).WithEatMs(c.EatMs)
                : TimingConfig.Default();
        }

        /// <summary>The outcome of one campaign.</summary>
        private readonly struct Outcome
        {
            public readonly long Cash;
            public readonly int Served;
            public readonly int StallTicks;     // ticks where the clean plates are ZERO
            public readonly int MaxDirty;
            public readonly int Tables;

            public Outcome(long cash, int served, int stall, int maxDirty, int tables)
            {
                Cash = cash; Served = served; StallTicks = stall;
                MaxDirty = maxDirty; Tables = tables;
            }
        }

        /// <summary>
        /// A sixty-day campaign. If `dishwashers` is greater than zero, that many
        /// people are dedicated to the sink every morning.
        /// </summary>
        private Outcome Run(int dishwashers)
        {
            Simulation sim = new Simulation(Economy(), Content(), Timing(), Seed);
            int stall = 0, maxDirty = 0, served = 0;

            // THE MENU IS NARROWED - without this the measurement does not run
            // at all.
            //
            // The game starts with ALL the dishes open, and a thirty-two dish
            // menu bankrupts a place with thirteen customers a day (stock is held
            // for every dish on the menu and the perishables go overnight). I
            // skipped this on both attempts: the place finished the sixtieth day
            // on 1 coin and 252 parties, the plate bottleneck NEVER formed, and
            // the two arms came out byte for byte identical.
            //
            // Which dishes stay is NOT INVENTED: the simulation's own `WouldPick`
            // measure is asked - "which dish would this archetype choose from
            // this role". So what stays on the menu really is what gets ordered.
            bool[] keep = new bool[sim.DishCount];
            for (int a = 0; a < 5; a++)
                for (int role = 0; role < 4; role++)
                {
                    int d = sim.WouldPick(a, role);
                    if (d >= 0) keep[d] = true;
                }
            for (int d = 0; d < sim.DishCount; d++)
                if (!keep[d] && sim.IsOnMenu(d))
                    sim.Apply(new Command(sim.TickIndex, CommandKind.SetMenuSlot, d, 0));

            for (int day = 1; day <= sim.CampaignDays; day++)
            {
                // --- morning: stock, crew, expansion ---
                for (int i = 0; i < sim.IngredientCount; i++)
                {
                    int need = sim.RecommendedRestock(i);
                    if (need > 0)
                        sim.Apply(new Command(sim.TickIndex,
                                              CommandKind.OrderIngredient, i, need));
                }

                // NO EXPANSION - deliberately.
                //
                // The first version had one, and this measuring player (which
                // does not know how to narrow the menu) went under once it
                // expanded: four tables and 537 in the till on the sixtieth day.
                // Because both arms went under the same way the result came out
                // BYTE FOR BYTE identical and the threshold never fired.
                //
                // It is not needed either: at tier 0 the crew cap is 3, so one
                // cook + TWO hall staff is possible. That is enough to dedicate
                // one of them to the sink, and the place stays on its feet.

                Crew needed = sim.RequiredCrewTomorrow();
                for (int guard = 0; guard < 12 && sim.Cooks < needed.Cooks
                                    && sim.Cooks + sim.HallStaff < sim.StaffCap; guard++)
                {
                    int before = sim.Cooks;
                    sim.Apply(new Command(sim.TickIndex, CommandKind.Hire, 0, 0));
                    if (sim.Cooks == before) break;
                }
                for (int guard = 0; guard < 12 && sim.HallStaff < needed.Hall
                                    && sim.Cooks + sim.HallStaff < sim.StaffCap; guard++)
                {
                    int before = sim.HallStaff;
                    sim.Apply(new Command(sim.TickIndex, CommandKind.Hire, 1, 0));
                    if (sim.HallStaff == before) break;
                }

                // THIS IS THE ONLY DIFFERENCE.
                int target = dishwashers > 0 && sim.HallStaff >= 2 ? dishwashers : 0;
                if (sim.Dishwashers != target)
                    sim.Apply(new Command(sim.TickIndex, CommandKind.SetDishwashers, target));

                // --- service ---
                sim.Apply(new Command(sim.TickIndex, CommandKind.OpenService));
                for (int t = 0; t < 5000; t++)
                {
                    sim.Tick();
                    if (sim.PlatesClean == 0) stall++;
                    if (sim.PlatesDirty > maxDirty) maxDirty = sim.PlatesDirty;
                    if (sim.ServiceComplete) break;
                }
                sim.Apply(new Command(sim.TickIndex, CommandKind.CloseDay));
                served += sim.BuildDayReport().ServedParties;
                sim.AdvanceToNextDay();
            }

            return new Outcome(sim.Cash, served, stall, maxDirty, sim.TableCount);
        }

        /// <summary>
        /// THE SPECIALIST REALLY DOES WASH FASTER.
        ///
        /// The user's sentence: "the dishwasher's washing speed has to be much
        /// higher than everyone else's, because they are the person who does that
        /// job." The content already said so too - in staff-roles.json the
        /// dishwasher role's daily capacity is 48 against the waiter's 26 - but
        /// the simulation used the difference NOT AT ALL: the dedicated
        /// dishwasher and the waiter running to help both washed with the same
        /// WashMs.
        ///
        /// This test holds that connection. If the multiplier goes back to 10000
        /// (no difference) it breaks.
        /// </summary>
        [Fact]
        public void The_specialist_washes_faster()
        {
            TimingConfig t = Timing();
            _out.WriteLine($"waiter {t.WashMs} ms, dishwasher {t.DishwasherWashMs} ms "
                           + $"(multiplier {t.DishwasherSpeedBp} bp)");

            Assert.True(t.WashMs > 0, "the washing time is zero - the measurement did not run");
            Assert.True(t.DishwasherWashMs < t.WashMs,
                $"the dedicated dishwasher is not faster ({t.DishwasherWashMs} >= {t.WashMs})");
        }

        // THE CAMPAIGN COMPARISON DOES NOT BELONG HERE, IT BELONGS IN THE HARNESS.
        //
        // I tried it and it DID NOT RUN: the plate bottleneck is a BIG RESTAURANT
        // phenomenon (in the harness the planner gives 263 ticks at 12 tables).
        // Building a viable big player inside the test project means rewriting
        // the harness's strategies from scratch - and on all three of my attempts
        // the place stayed at four tables and went under, so the two arms came
        // out BYTE FOR BYTE identical and the plateless ticks were zero.
        //
        // The liveness line I wrote ("the clean plates never ran out - the
        // measurement did not run") caught all three; without that line the test
        // would have shown green and measured nothing.
        //
        // The balance comparison belongs in the harness, the wiring belongs here.
    }
}
