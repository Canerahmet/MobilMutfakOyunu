using Lokanta.Content;
using Lokanta.Core.Content;
using Lokanta.Core.Economy;
using Lokanta.Core.Sim;
using Xunit;
using Xunit.Abstractions;

namespace Lokanta.Core.Tests
{
    /// <summary>
    /// A SERVICE DAY IS LONGER THAN THE WINDOW THE DOCUMENTS SELL.
    ///
    /// docs/23 7 writes the contract as "one service day | 4,800 | 8 minutes
    /// at 1x speed". That is the ARRIVAL window. `ServiceComplete` needs
    /// `_serviceTick >= ServiceTicks` AND `_partyCount == 0`, so the day runs
    /// on until the room empties, and the people who arrived in the last
    /// minute eat, pay and leave inside that tail.
    ///
    /// It is not a rounding error. The tail was measured at 27% of a fast
    /// food day's revenue ([62](62-service-agency.md)), which is why
    /// `CloseDay` - which ejects the room - was always the wrong button, and
    /// why docs/27's session arithmetic was optimistic.
    ///
    /// THIS TEST EXISTS BECAUSE THE NUMBER WILL BE EDITED BY SOMEBODY WHO
    /// THINKS THEY ARE TIDYING UP. `ServiceComplete` reads like a bug: the
    /// obvious "fix" is to drop `_partyCount == 0` and end the day on the
    /// clock. That deletes the tail, and with it a quarter of the day's
    /// money, and every check in this repository would still be green. So the
    /// tail is asserted, in both cuisines, as a share rather than a constant -
    /// "assert the relationship, not the number" (docs/57).
    /// </summary>
    public sealed class ServiceDayLengthTests
    {
        private readonly ITestOutputHelper _out;
        public ServiceDayLengthTests(ITestOutputHelper o) { _out = o; }

        private const ulong Seed = 20260918UL;

        /// <summary>
        /// THE TAIL IS A PROPERTY OF THE ARRIVAL CURVE, NOT OF THE GAME.
        ///
        /// Measured on the opening day: fast food overruns by 40.1 s carrying
        /// 7.3% of the day, and Turkish overruns by NOTHING - its room is
        /// empty on the stroke of the window. That is not a bug in either. The
        /// last slot's intensity is 1.505 in fast food and 0.823 in Turkish,
        /// so fast food is still filling tables when the door closes and
        /// Turkish emptied an hour earlier. The expectation is passed in per
        /// cuisine
        /// and the slot intensity is printed beside it, so if the curves are
        /// ever re-balanced the two readings disagree loudly instead of one of
        /// them quietly becoming wrong.
        ///
        /// AND ONE OF THOSE TWO NUMBERS DOES NOT AGREE WITH THE DOCUMENT.
        /// docs/62 quotes the fast food figure as 1.60 and this derivation
        /// gives 1.505; Turkish agrees to three places (0.823 against 0.83).
        /// They are computed differently - this one aggregates the archetype
        /// weights straight off the content, while the figure in docs/62 came
        /// from the arrival PLAN, which the simulation builds after the day of
        /// the week, the season and the regulars have had their say. A 6%
        /// difference in one cuisine and none in the other is consistent with
        /// that, and is not consistent with either number being a typo. It is
        /// left standing and written down rather than reconciled by picking
        /// one: this derivation is only used in a failure message, so nothing
        /// rests on it, and guessing which of the two is right is how a wrong
        /// number gets laundered into a correct-looking one.
        /// </summary>
        [Theory]
        [InlineData("fastfood", true)]
        [InlineData("turk", false)]
        public void A_service_day_runs_on_past_its_arrival_window(string cuisine, bool mustOverrun)
        {
            EconomyConfig eco = ContentLoader.LoadEconomy(Paths.Content);
            ContentSet content = ContentSetLoader.Load(Paths.Content, cuisine);
            TimingConfig timing = content.SlotDurationsBp != null
                ? TimingConfig.Default().WithSlotDurations(content.SlotDurationsBp)
                                        .WithEatMs(content.EatMs)
                : TimingConfig.Default();

            Simulation sim = new Simulation(eco, content, timing, Seed);
            sim.Apply(new Command(0, CommandKind.OpenService));

            int window = timing.ServiceTicks;
            long revenueAtWindow = -1;

            // A ceiling, so a day that never completes fails as a hang rather
            // than as a test run that never returns.
            int ceiling = window * 3;
            int t = 0;
            while (!sim.ServiceComplete && t < ceiling)
            {
                sim.Tick();
                t++;
                if (sim.ServiceTick == window) revenueAtWindow = sim.Revenue;
            }

            Assert.True(sim.ServiceComplete,
                "the day never completed: it ran " + t + " ticks against a window of "
                + window);
            Assert.True(revenueAtWindow >= 0,
                "the run never passed the arrival window, so there is no tail to measure");

            long tail = sim.Revenue - revenueAtWindow;
            double tailShare = sim.Revenue > 0 ? (double)tail / sim.Revenue : 0.0;
            double overrunSeconds = (t - window) * TimingConfig.TickMs / 1000.0;

            _out.WriteLine($"{cuisine}: window {window} ticks, day {t} ticks "
                           + $"(+{overrunSeconds:0.0} s at 1x), revenue {sim.Revenue}, "
                           + $"of which {tail} ({tailShare * 100:0.0}%) arrived after the window");

            int lastSlot = LastSlotIntensityBp(content);
            _out.WriteLine($"{cuisine}: last slot intensity {lastSlot} bp");

            // THE FLOOR IS FIVE PER CENT, AND THE FIRST VERSION SAID TEN.
            //
            // docs/62 records the tail as 27% of a fast food day, and I set
            // the floor from that number without asking what it had been
            // measured on. It went red at 7.3%: that 27% comes from 60-day
            // CAMPAIGNS, where the restaurant has grown to fourteen tables and
            // a late party overlaps three others. This test opens the game and
            // plays day one at the opening tier.
            //
            // Which is on purpose, and is why the floor stays low rather than
            // the test being taught to grow a restaurant. The opening day is
            // the campaign's WEAKEST case - fewest tables, least overlap, the
            // shortest drain there is - so a floor proved here is a floor that
            // holds on every later day. Measured: fast food 7.3% over 40.1 s.
            if (mustOverrun)
            {
                Assert.True(t > window,
                    "the day ended exactly on the clock, so either the drain has been "
                    + "removed - ServiceComplete must wait for the room to empty - or "
                    + "the last slot has been quietened: it is " + lastSlot + " bp");

                // See the floor note above: the opening day is the campaign's
                // weakest case, so a floor proved here holds on every later day.
                Assert.True(tailShare >= 0.05,
                    "the tail after the arrival window is only " + (tailShare * 100).ToString("0.0")
                    + "% of the opening day's revenue, and the opening day is the weakest "
                    + "case there is; the whole argument for LastOrders rests on the tail "
                    + "being material");
            }
            else
            {
                // NOT "no tail is fine" - "no tail is what this curve means".
                // If Turkish ever starts overrunning, its evening has been made
                // busier and the design note in docs/62 needs re-reading.
                Assert.True(t == window,
                    "this cuisine emptied its room by the window when the curve was "
                    + "written, and now runs " + (t - window) + " ticks past it; its "
                    + "last slot is " + lastSlot + " bp");
            }

        }
        /// <summary>
        /// The intensity of the day's LAST slot, in basis points, where 10000
        /// is "this slot gets exactly its share of the day".
        ///
        /// The arrival weights live on the ARCHETYPE, not on the cuisine -
        /// the intensities docs/62 quotes are derived, not stored. Intensity
        /// is the slot's share of the arrivals divided by its share of the
        /// clock: a slot that takes 10% of the day and brings 16% of the
        /// people runs at 1.6x.
        ///
        /// IT VERIFIES ITSELF. Every measurement tool in this repository is
        /// supposed to carry a line that proves it is measuring what it says,
        /// because one of them was quietly reading a different number for a
        /// week. Here that line is the sum: the four intensities weighted by
        /// the four durations must come back to 10000, and if the weights or
        /// the durations stop summing to 10000 that is the assertion which
        /// fails, rather than the day-length check failing for a reason
        /// nobody can read.
        /// </summary>
        private static int LastSlotIntensityBp(ContentSet content)
        {
            int[] durations = content.SlotDurationsBp;
            Assert.NotNull(durations);
            int slots = durations.Length;

            long[] arrivals = new long[slots];
            long total = 0;
            foreach (ArchetypeDef a in content.Archetypes)
            {
                for (int i = 0; i < slots && i < a.ArrivalWeightsBp.Length; i++)
                {
                    long w = (long)a.Weight * a.ArrivalWeightsBp[i];
                    arrivals[i] += w;
                    total += w;
                }
            }
            Assert.True(total > 0, "the content carries no arrival weight at all");

            long check = 0;
            for (int i = 0; i < slots; i++)
            {
                long intensity = arrivals[i] * 10000L / total * 10000L / durations[i];
                check += intensity * durations[i] / 10000L;
            }
            Assert.InRange(check, 9900L, 10100L);

            int last = slots - 1;
            return (int)(arrivals[last] * 10000L / total * 10000L / durations[last]);
        }
    }
}
