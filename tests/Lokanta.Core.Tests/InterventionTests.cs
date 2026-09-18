using Lokanta.Content;
using Lokanta.Core.Content;
using Lokanta.Core.Economy;
using Lokanta.Core.Sim;
using Xunit;
using Xunit.Abstractions;

namespace Lokanta.Core.Tests
{
    /// <summary>
    /// THE OWNER'S ATTENTION REGENERATES, AND THE DOOR CAN BE SHUT.
    ///
    /// Both of these came out of a measurement rather than a preference.
    ///
    /// The allowance used to be a day budget, and an eager player's whole
    /// budget was gone at 0:45 on average (24 seeds x 60 days, fast food one
    /// waiter short) - before the lunch crest at 2:00 and two crests before
    /// the evening one at 6:14. The rest of an eight-minute day had nothing
    /// in it. Spreading the same resource beat enlarging it: a six-charge
    /// budget spent 5.18 charges and lost 59 parties, a three-charge
    /// regenerating pool spent 3.08 and lost 53.
    ///
    /// CloseDay sends everybody seated away angry, and the tail after the
    /// arrival window carries 27% of a fast food day's revenue - so closing
    /// early was never right on any day in either cuisine. A button that is
    /// always wrong to press is not a decision. LastOrders is the decision it
    /// was pretending to be.
    /// </summary>
    public sealed class InterventionTests
    {
        private readonly ITestOutputHelper _out;
        public InterventionTests(ITestOutputHelper o) { _out = o; }

        private const ulong Seed = 20260918UL;

        private static EconomyConfig Economy() => ContentLoader.LoadEconomy(Paths.Content);
        private static ContentSet Content() => ContentSetLoader.Load(Paths.Content, "turk");

        private static TimingConfig Timing()
        {
            ContentSet c = Content();
            return c.SlotDurationsBp != null
                ? TimingConfig.Default().WithSlotDurations(c.SlotDurationsBp).WithEatMs(c.EatMs)
                : TimingConfig.Default();
        }

        private static Simulation Open()
        {
            Simulation sim = new Simulation(Economy(), Content(), Timing(), Seed);
            sim.Apply(new Command(0, CommandKind.OpenService));
            return sim;
        }

        /// <summary>
        /// THE COUNT GOES UP WHILE THE SERVICE RUNS.
        ///
        /// This is the assertion the old allowance could never make: a check
        /// that only ever watched the count go DOWN would pass on a pool that
        /// never refills. Spend one, run a regeneration period, and it has to
        /// come back.
        /// </summary>
        [Fact]
        public void The_attention_pool_refills_during_service()
        {
            Simulation sim = Open();
            EconomyConfig eco = Economy();

            Assert.Equal(eco.InterventionStart, sim.InterventionsLeft);

            // Spend one on the whole hall. Tea is the verb that never needs a
            // target to exist, so the test does not depend on somebody being
            // seated yet.
            sim.Apply(new Command(sim.TickIndex, CommandKind.Intervene,
                                  0, (int)InterventionKind.FreeTea));
            int after = sim.InterventionsLeft;

            int ticks = eco.InterventionRegenMs / TimingConfig.TickMs + 2;
            for (int i = 0; i < ticks; i++) sim.Tick();

            _out.WriteLine($"start {eco.InterventionStart}, after spending {after}, "
                           + $"after {ticks} ticks {sim.InterventionsLeft}");
            Assert.True(sim.InterventionsLeft > after,
                "the attention pool did not refill: it went from " + after + " to "
                + sim.InterventionsLeft + " over " + ticks + " ticks, and the "
                + "regeneration period is " + eco.InterventionRegenMs + " ms");
        }

        /// <summary>
        /// AND IT STOPS AT THE CAP, because the cap is the cost.
        ///
        /// Without a ceiling the decision disappears: a player could hold
        /// every charge the day produces and spend them all at the crest. The
        /// measurement says that is not even the better play, but a mechanic
        /// must not merely be unattractive - it must be unavailable, or it is
        /// the only thing anybody does.
        /// </summary>
        [Fact]
        public void The_pool_stops_at_the_cap()
        {
            Simulation sim = Open();
            for (int i = 0; i < 4800; i++) sim.Tick();

            _out.WriteLine($"after a whole service: {sim.InterventionsLeft} "
                           + $"of a cap of {sim.InterventionCapToday}");
            Assert.True(sim.InterventionsLeft <= sim.InterventionCapToday,
                "the pool ran past its cap: " + sim.InterventionsLeft
                + " > " + sim.InterventionCapToday);
        }

        /// <summary>
        /// LAST ORDERS STOPS THE DOOR AND TOUCHES NOBODY.
        ///
        /// The two halves are asserted separately because they fail
        /// separately: a version that ended the day would also stop the
        /// arrivals, and a check that only counted arrivals would call it
        /// correct.
        /// </summary>
        [Fact]
        public void Last_orders_stops_arrivals_and_ejects_nobody()
        {
            Simulation sim = Open();

            // Run into the Turkish lunch crest, which starts at 0:58.
            for (int i = 0; i < 1200; i++) sim.Tick();
            int seated = sim.ActiveParties;
            Assert.True(seated > 0,
                "nobody had arrived by tick 1200, so this test cannot tell "
                + "whether last orders ejects anybody");

            sim.Apply(new Command(sim.TickIndex, CommandKind.LastOrders));
            Assert.True(sim.DoorsClosed);
            Assert.Equal(seated, sim.ActiveParties);

            // Nobody new comes in, however long it runs.
            for (int i = 0; i < 1200; i++) sim.Tick();
            _out.WriteLine($"seated at last orders {seated}, "
                           + $"active 120 s later {sim.ActiveParties}");
            Assert.True(sim.ActiveParties <= seated,
                "somebody arrived after last orders: " + sim.ActiveParties
                + " active against " + seated + " at the moment the door shut");

            // And the day is still running - the whole point is that it does
            // not end until the room empties.
            Assert.Equal(DayPhase.Service, sim.Phase);
        }
    }
}
