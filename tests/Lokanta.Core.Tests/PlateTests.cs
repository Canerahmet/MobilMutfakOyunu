using Lokanta.Content;
using Lokanta.Core.Content;
using Lokanta.Core.Economy;
using Lokanta.Core.Sim;
using Xunit;
using Xunit.Abstractions;

namespace Lokanta.Core.Tests
{
    /// <summary>
    /// THE PLATE CYCLE.
    ///
    /// The restaurant has a counted number of plates and they go round:
    ///
    ///   clean -> (the cook plates up) -> in use
    ///   in use -> (the waiter clears the table) -> dirty
    ///   dirty -> (washed at the sink) -> clean
    ///
    /// docs/14 describes the dishwasher as a BOTTLENECK: "if the plates run out
    /// the service stops - invisible, but noticed the moment it blocks". Until
    /// now that bottleneck was buried inside the hall capacity.
    /// </summary>
    public sealed class PlateTests
    {
        private readonly ITestOutputHelper _out;
        public PlateTests(ITestOutputHelper o) { _out = o; }

        private const ulong Seed = 20260912UL;

        private static EconomyConfig Economy() => ContentLoader.LoadEconomy(Paths.Content);
        private static ContentSet Content() => ContentSetLoader.Load(Paths.Content, "fastfood");

        /// <summary>
        /// A TABLE SERVICE cuisine. Where the hall really is busy.
        ///
        /// Fast food became SELF SERVICE (docs/51): no waiter comes to the table,
        /// the hall is a cashier + a dishwasher and the work per customer is less
        /// than half. So no plate bottleneck forms there and the question "does a
        /// dishwasher rescue the hall" CANNOT BE ASKED - the two arms come out
        /// identical (32/32, zero ticks waiting for a plate).
        ///
        /// The question is meaningful in a table service cuisine: the waiter runs
        /// both to the table and to the sink, so the two really do COMPETE.
        /// </summary>
        private static ContentSet TableServiceContent() =>
            ContentSetLoader.Load(Paths.Content, "turk");

        private static TimingConfig Timing()
        {
            ContentSet c = Content();
            return c.SlotDurationsBp != null
                ? TimingConfig.Default().WithSlotDurations(c.SlotDurationsBp).WithEatMs(c.EatMs)
                : TimingConfig.Default();
        }

        private static Simulation NewSim(int hall = 1, int dishwashers = 0)
        {
            Simulation sim = new Simulation(Economy(), Content(), Timing(), Seed);
            for (int i = 1; i < hall; i++)
                sim.Apply(new Command(0, CommandKind.Hire, 1));
            if (dishwashers > 0)
                sim.Apply(new Command(0, CommandKind.SetDishwashers, dishwashers));
            return sim;
        }

        /// <summary>
        /// A morning that GROWS SANELY.
        ///
        /// The old helper said "if the till is over 400,000 try THREE tiers at
        /// once, then hire three people", and the place went under in five days:
        /// ZERO parties were served between days 6 and 40. So the measurement
        /// called "the busy days of a growing restaurant" was measuring a dead
        /// restaurant.
        ///
        /// The rule is now a real player's: one tier, and only if THREE TIMES the
        /// cost is in the till; the crew follows tomorrow's demand.
        /// </summary>
        private static void GrowSanely(Simulation sim)
        {
            for (int tier = 1; tier < 8; tier++)
            {
                long cost = sim.UpgradeCostFor(tier);
                if (cost <= 0) continue;
                if (sim.Cash < cost * 3) break;
                sim.Apply(new Command(sim.TickIndex, CommandKind.Expand, tier));
                break;
            }

            Crew need = sim.RequiredCrewTomorrow();
            while (sim.Cooks < need.Cooks && sim.Cooks + sim.HallStaff < sim.StaffCap)
                sim.Apply(new Command(sim.TickIndex, CommandKind.Hire, 0));
            while (sim.HallStaff < need.Hall && sim.Cooks + sim.HallStaff < sim.StaffCap)
                sim.Apply(new Command(sim.TickIndex, CommandKind.Hire, 1));
        }

        private static DayReport RunOneDay(Simulation sim)
        {
            sim.Apply(new Command(sim.TickIndex, CommandKind.OpenService));
            int limit = Timing().ServiceTicks + 4000;
            for (int t = 0; t < limit; t++)
            {
                sim.Tick();
                if (sim.ServiceComplete) break;
            }
            sim.Apply(new Command(sim.TickIndex, CommandKind.CloseDay));
            return sim.BuildDayReport();
        }

        // ====================================================================
        /// <summary>
        /// NO PLATE IS LOST OR GAINED.
        ///
        /// This invariant is the foundation of the whole mechanic: a leaking plate
        /// becomes a bug that slows the service down day by day with its cause
        /// visible nowhere. It is tested on every tick, not at the end of the day
        /// - a counter that breaks in an intermediate state and recovers by the
        /// end would pass an end-of-day check.
        /// </summary>
        [Fact]
        public void The_plate_count_is_conserved()
        {
            Simulation sim = NewSim(hall: 2);
            int total = sim.PlatesTotal;
            Assert.True(total > 0, "the tier has no plates");

            sim.Apply(new Command(sim.TickIndex, CommandKind.OpenService));
            int limit = Timing().ServiceTicks + 4000;
            int minClean = int.MaxValue, maxDirty = 0;

            for (int t = 0; t < limit; t++)
            {
                sim.Tick();

                int sum = sim.PlatesClean + sim.PlatesInUse + sim.PlatesDirty;
                Assert.True(sum == total,
                    $"tick {t}: clean {sim.PlatesClean} + in use {sim.PlatesInUse}"
                    + $" + dirty {sim.PlatesDirty} = {sum}, expected {total}");
                Assert.True(sim.PlatesClean >= 0 && sim.PlatesInUse >= 0
                            && sim.PlatesDirty >= 0, $"tick {t}: negative plates");

                if (sim.PlatesClean < minClean) minClean = sim.PlatesClean;
                if (sim.PlatesDirty > maxDirty) maxDirty = sim.PlatesDirty;
                if (sim.ServiceComplete) break;
            }

            _out.WriteLine($"{total} plates in total | min clean {minClean}"
                           + $" | max dirty {maxDirty}"
                           + $" | washed {sim.PlatesWashedToday}"
                           + $" | waited with no plate {sim.PlateBlockedTicks} ticks");

            // The cycle REALLY turns: something got dirty and something got washed.
            Assert.True(maxDirty > 0, "no plate ever got dirty - the cycle is not running");
            Assert.True(sim.PlatesWashedToday > 0, "no plate was ever washed");
        }

        /// <summary>
        /// WITH A DISHWASHER, EVERYONE DOES THEIR OWN JOB.
        ///
        /// That is the user's sentence. Its measurable form: with someone
        /// dedicated to the sink, the hall staff DO NOT DROP their work and run to
        /// the washing up, so clearing tables and serving do not suffer.
        ///
        /// THE OLD VERSION MEASURED NOTHING. In a quiet four-table restaurant the
        /// two runs were IDENTICAL (waited with no plate 0/0, washed 15/15, served
        /// 7/7) and the only claim was "0 &lt;= 0": if the dishwasher mechanic had
        /// been deleted outright the test would still have been green.
        ///
        /// Now the PRESSURE is built up first - a restaurant growing over
        /// twenty-five days - and the pressure really forming is asserted as a
        /// PRECONDITION. If there is no pressure the test fails rather than
        /// quietly passing as "could not be measured".
        ///
        /// The crew is EQUAL: the same number of hall workers in both runs.
        /// Otherwise what would be measured is not the dishwasher but one extra
        /// person.
        /// </summary>
        [Fact]
        public void A_dishwasher_rescues_the_hall_from_the_sink()
        {
            const int Days = 25;
            Simulation plain = new Simulation(Economy(), TableServiceContent(),
                                              Timing(), Seed);
            Simulation dedicated = new Simulation(Economy(), TableServiceContent(),
                                                  Timing(), Seed);

            int plainWashes = 0, dedicatedWashes = 0;
            int plainBlocked = 0, dedicatedBlocked = 0;
            int plainServed = 0, dedicatedServed = 0;
            int plainDirtyPeak = 0, dedicatedDirtyPeak = 0;

            for (int day = 0; day < Days; day++)
            {
                GrowSanely(plain);
                GrowSanely(dedicated);

                // THE DISHWASHER IS SET AGAIN EVERY MORNING: as the crew changes
                // the cap changes, and SetDishwashers clamps to the cap.
                if (dedicated.HallStaff >= 2)
                    dedicated.Apply(new Command(dedicated.TickIndex,
                                                CommandKind.SetDishwashers, 1));

                // THE PLACE CANNOT GROW WITHOUT RESTOCKING: RunOneDay only opens
                // the service, it does not do the morning shopping.
                plain.Apply(new Command(plain.TickIndex, CommandKind.OrderRecommended));
                dedicated.Apply(new Command(dedicated.TickIndex, CommandKind.OrderRecommended));

                Assert.True(plain.HallStaff == dedicated.HallStaff,
                    $"day {day}: the crews diverged ({plain.HallStaff} / {dedicated.HallStaff}) - "
                    + "what is measured would be one extra person, not the dishwasher");

                DayReport a = RunOneDay(plain);
                DayReport b = RunOneDay(dedicated);

                plainWashes += plain.HallRushWashes;
                dedicatedWashes += dedicated.HallRushWashes;
                plainBlocked += plain.PlateBlockedTicks;
                dedicatedBlocked += dedicated.PlateBlockedTicks;
                plainServed += a.ServedParties;
                dedicatedServed += b.ServedParties;
                if (plain.PlatesDirty > plainDirtyPeak) plainDirtyPeak = plain.PlatesDirty;
                if (dedicated.PlatesDirty > dedicatedDirtyPeak) dedicatedDirtyPeak = dedicated.PlatesDirty;

                plain.AdvanceToNextDay();
                dedicated.AdvanceToNextDay();
            }

            _out.WriteLine($"no dishwasher: hall rushed to the sink {plainWashes} times, "
                           + $"waited with no plate {plainBlocked}, served {plainServed} parties, "
                           + $"{plain.TableCount} tables");
            _out.WriteLine($"with dishwasher: hall rushed to the sink {dedicatedWashes} times, "
                           + $"waited with no plate {dedicatedBlocked}, served {dedicatedServed} parties, "
                           + $"{dedicated.TableCount} tables");

            // PRECONDITION: did the pressure really form. Without this line the
            // test would stay green in the "nothing happened" case too.
            Assert.True(plainDirtyPeak > 0,
                "no plate ever got dirty - no pressure was built, the comparison is meaningless");
            Assert.True(plainWashes > 0,
                $"in the run without a dishwasher the hall never ran to the sink ({plainWashes} times) - "
                + "no pressure was built, the dishwasher's difference cannot be measured");

            // THE REAL CLAIM: with a dishwasher the hall STOPS making the
            // emergency run to the sink. Not "zero" but less than half, because in
            // the early days no dishwasher can be assigned until the hall crew
            // reaches two people - SetDishwashers keeps at least one person on the
            // floor. Measured: 3 runs against 31.
            Assert.True(dedicatedWashes * 2 < plainWashes,
                $"the dishwasher did not rescue the hall from the sink: {dedicatedWashes} times / "
                + $"{plainWashes} times");

            // And this is not free: the plates still get washed and the service
            // does not suffer.
            Assert.True(dedicatedBlocked <= plainBlocked,
                $"the dishwasher increased the plateless waiting: {dedicatedBlocked} > {plainBlocked}");
        }

        /// <summary>
        /// A SHRINKING RESTAURANT LOSES PLATES TOO.
        ///
        /// Expand added as many CLEAN plates as the difference, Downsize removed
        /// nothing at all: on the day it shrank, "clean + in use + dirty >
        /// PlatesTotal" held, and because WashNeeded's thresholds are computed
        /// against the shrunken total the washing-up duty fired at the wrong time.
        ///
        /// The bug HID ITSELF: overnight AdvanceToNextDay rewrites the plates from
        /// the tier, so the invariant was broken ONLY ON THAT DAY - and
        /// "The_plate_count_is_conserved" never saw it because it never ran a
        /// shrink.
        /// </summary>
        [Fact]
        public void A_shrinking_restaurant_loses_plates_too()
        {
            Simulation sim = new Simulation(Economy(), Content(), Timing(), Seed);

            // GROW FIRST: there has to be a tier to shrink from.
            for (int tier = 1; tier < 8; tier++)
            {
                long cost = sim.UpgradeCostFor(tier);
                if (cost <= 0 || sim.Cash < cost) break;
                sim.Apply(new Command(sim.TickIndex, CommandKind.Expand, tier));
            }
            Assert.True(sim.TableCount > 4, $"the place did not grow ({sim.TableCount} tables) - "
                                            + "the shrink cannot be tested");

            int bigTotal = sim.PlatesTotal;
            Assert.Equal(bigTotal,
                         sim.PlatesClean + sim.PlatesInUse + sim.PlatesDirty);

            // SHRINK: we drive the till negative and call up the bankruptcy ladder.
            // The ladder sells equipment first, then shrinks.
            int guard = 0;
            int tablesBefore = sim.TableCount;
            while (sim.TableCount == tablesBefore && guard++ < 60)
            {
                sim.Apply(new Command(sim.TickIndex, CommandKind.OrderRecommended));
                sim.Apply(new Command(sim.TickIndex, CommandKind.OpenService));
                sim.Apply(new Command(sim.TickIndex, CommandKind.CloseDay));
                sim.AdvanceToNextDay();
            }
            Assert.True(sim.TableCount < tablesBefore,
                $"the place did not shrink in {guard} days ({sim.TableCount} tables)");

            // THE REAL CLAIM: the invariant holds IMMEDIATELY AFTER the shrink too.
            int sum = sim.PlatesClean + sim.PlatesInUse + sim.PlatesDirty;
            _out.WriteLine($"{tablesBefore} -> {sim.TableCount} tables | total {sim.PlatesTotal}"
                           + $" | clean {sim.PlatesClean} in use {sim.PlatesInUse}"
                           + $" dirty {sim.PlatesDirty}");
            Assert.True(sum <= sim.PlatesTotal,
                $"too many plates after the shrink: {sum} > {sim.PlatesTotal}");
        }

        /// <summary>
        /// DOES THE BOTTLENECK REALLY BITE AT THE PEAK - A DIAGNOSTIC.
        ///
        /// Nothing happens on day one (seven parties, twenty-four plates). It is
        /// not enough for a mechanic to "exist", it has to be FELT somewhere; this
        /// test runs the busy days of a grown restaurant and prints the plate
        /// pressure.
        ///
        /// It does not claim, it MEASURES: the numbers are there to tune the plate
        /// count.
        /// </summary>
        [Fact]
        public void The_plate_pressure_is_measured_at_the_peak()
        {
            Simulation sim = new Simulation(Economy(), Content(), Timing(), Seed);

            int worstBlocked = 0, maxDirty = 0, minClean = int.MaxValue;
            int totalBlocked = 0;
            int maxOccupied = 0;
            int lastTenDays = 0;

            for (int day = 0; day < 40; day++)
            {
                // A GROWING RESTAURANT: the bottleneck only means anything at an
                // OCCUPANCY peak, and a quiet four-table place has no such peak.
                // Every morning we grow and hire as far as we can afford - a crude
                // version of the "planci" bot.
                // Grow while the money is comfortable. My first version tried every
                // tier every morning and the place went bankrupt on the tenth day -
                // what was measured was not the peak but the desert itself.
                GrowSanely(sim);

                sim.Apply(new Command(sim.TickIndex, CommandKind.OrderRecommended));
                sim.Apply(new Command(sim.TickIndex, CommandKind.OpenService));
                int limit = Timing().ServiceTicks + 4000;
                for (int t = 0; t < limit; t++)
                {
                    sim.Tick();
                    if (sim.PlatesDirty > maxDirty) maxDirty = sim.PlatesDirty;
                    if (sim.PlatesClean < minClean) minClean = sim.PlatesClean;
                    if (sim.OccupiedTables > maxOccupied) maxOccupied = sim.OccupiedTables;
                    if (sim.ServiceComplete) break;
                }
                sim.Apply(new Command(sim.TickIndex, CommandKind.CloseDay));
                DayReport r = sim.BuildDayReport();
                if (day >= 30) lastTenDays += r.ServedParties;
                if (sim.PlateBlockedTicks > worstBlocked) worstBlocked = sim.PlateBlockedTicks;
                totalBlocked += sim.PlateBlockedTicks;
                if (day % 5 == 4 || sim.PlateBlockedTicks > 0)
                    _out.WriteLine($"day {sim.Day}: {r.ServedParties} parties, {sim.TableCount} tables, "
                                   + $"clean-low {sim.PlatesClean}, dirty {sim.PlatesDirty}, "
                                   + $"waited with no plate {sim.PlateBlockedTicks} ticks");
                sim.AdvanceToNextDay();
            }

            _out.WriteLine($"DIAGNOSTIC {sim.PlatesTotal} plates in total | min clean {minClean}"
                           + $" | max dirty {maxDirty}"
                           + $" | most tables occupied {maxOccupied}/{sim.TableCount}"
                           + $" | worst day {worstBlocked} ticks"
                           + $" | 40-day total {totalBlocked} ticks");

            // THE LIVENESS PRECONDITION.
            //
            // Days 1-5 satisfied the "maxDirty > 0" claim on their own; the test
            // stayed green even if the place died after day 6. This line tests
            // WHAT THE MEASUREMENT IS MEASURING: are customers still being served
            // in the last ten days.
            Assert.True(lastTenDays > 0,
                $"not a single party was served in the last ten days - what is being "
                + $"measured is not the peak but a dead restaurant (the growth logic "
                + $"or the bankruptcy ladder is broken)");
            Assert.True(maxOccupied > 4,
                $"the place never grew (most tables occupied {maxOccupied}) - "
                + "four tables are not enough for a peak measurement");
            Assert.True(maxDirty > 0, "no plate ever got dirty");
        }
    }
}
