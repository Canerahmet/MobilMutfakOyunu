using Lokanta.Content;
using Lokanta.Core.Content;
using Lokanta.Core.Economy;
using Lokanta.Core.Sim;
using Xunit;
using Xunit.Abstractions;

namespace Lokanta.Core.Tests
{
    /// <summary>
    /// THE TWO LEVERS docs/14 NAMED AND THE CODE NEVER HAD.
    ///
    /// A raise: morale up by fifteen, the wage up by 15% for good - and it
    /// shows in the next payroll. A day off: morale up by ten, and tomorrow
    /// the person is not on the floor, though they are paid. The only cook
    /// cannot take one: the owner is not the cook. Each is asserted as the
    /// relationship the old code could not produce, and each was proved red
    /// by removing the effect.
    /// </summary>
    public sealed class MoraleLeverTests
    {
        private readonly ITestOutputHelper _out;
        public MoraleLeverTests(ITestOutputHelper o) { _out = o; }

        private const ulong Seed = 20260919UL;

        private static Simulation Fresh()
        {
            EconomyConfig eco = ContentLoader.LoadEconomy(Paths.Content);
            ContentSet c = ContentSetLoader.Load(Paths.Content, "turk");
            TimingConfig timing = c.SlotDurationsBp != null
                ? TimingConfig.Default().WithSlotDurations(c.SlotDurationsBp).WithEatMs(c.EatMs)
                : TimingConfig.Default();
            return new Simulation(eco, c, timing, Seed);
        }

        [Fact]
        public void A_raise_lifts_morale_and_costs_every_week_after()
        {
            Simulation sim = Fresh();
            sim.Apply(new Command(sim.TickIndex, CommandKind.Hire, 1, 0));
            int morale = sim.StaffMorale(1, 0);
            long billBefore = sim.WeeklyBill;

            sim.Apply(new Command(sim.TickIndex, CommandKind.GiveRaise, 1, 0));

            _out.WriteLine($"morale {morale} -> {sim.StaffMorale(1, 0)}; weekly bill {billBefore} -> {sim.WeeklyBill}; raise {sim.StaffRaiseBp(1, 0)} bp");
            Assert.Equal(morale + Simulation.RaiseMoraleDelta, sim.StaffMorale(1, 0));
            Assert.Equal(Simulation.RaiseWageBp, sim.StaffRaiseBp(1, 0));
            Assert.True(sim.WeeklyBill > billBefore, "a raise that costs nothing is not a raise");
        }

        [Fact]
        public void A_day_off_lifts_morale_and_takes_the_person_off_the_floor()
        {
            Simulation sim = Fresh();
            sim.Apply(new Command(sim.TickIndex, CommandKind.Hire, 1, 0));
            sim.Apply(new Command(sim.TickIndex, CommandKind.Hire, 1, 0));
            int morale = sim.StaffMorale(1, 0);
            int hall = sim.HallStaff;

            // Given in the morning, it is today: the person sits out this service.
            sim.Apply(new Command(sim.TickIndex, CommandKind.DayOff, 1, 0));
            Assert.Equal(morale + Simulation.DayOffMoraleDelta, sim.StaffMorale(1, 0));
            Assert.True(sim.StaffRestingNext(1, 0), "the day off was not recorded");

            sim.Apply(new Command(sim.TickIndex, CommandKind.OpenService));
            _out.WriteLine($"hall {hall} on the roster, {sim.HallStaff} on the floor, {sim.HallResting} resting");
            Assert.Equal(hall - 1, sim.HallStaff);
            Assert.Equal(1, sim.HallResting);

            sim.Apply(new Command(sim.TickIndex, CommandKind.CloseDay));
            Assert.Equal(hall, sim.HallStaff);
            Assert.Equal(0, sim.HallResting);
        }

        [Fact]
        public void The_only_cook_cannot_take_a_day_off()
        {
            Simulation sim = Fresh();
            Assert.Equal(1, sim.Cooks);
            sim.Apply(new Command(sim.TickIndex, CommandKind.DayOff, 0, 0));
            Assert.False(sim.StaffRestingNext(0, 0), "the only cook was given the day off: nobody is cooking tomorrow");
        }
    }
}
