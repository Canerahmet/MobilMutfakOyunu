using Lokanta.Content;
using Lokanta.Core.Content;
using Lokanta.Core.Economy;
using Lokanta.Core.Sim;
using Xunit;
using Xunit.Abstractions;

namespace Lokanta.Core.Tests
{
    /// <summary>
    /// ONE SCHEDULED THING HAPPENS TO THE RESTAURANT.
    ///
    /// docs/64 4: from day 22 the reference player's campaign was the same
    /// day repeated, and nothing on the calendar ever arrived - the critic
    /// docs/09 promised was a random rare walk-in. Decided 19 September: one
    /// event per season that happens TO the shop, the third season's being
    /// the critic. Asserted: the morning before, the game says so; on the
    /// day, exactly one party of the critic's archetype is in the plan; and
    /// when they have gone, a verdict is on the books and announced. Proved
    /// red by not adding the critic to the plan.
    /// </summary>
    public sealed class CriticTests
    {
        private readonly ITestOutputHelper _out;
        public CriticTests(ITestOutputHelper o) { _out = o; }

        private const ulong Seed = 20260919UL;

        [Theory]
        [InlineData("turk")]
        [InlineData("fastfood")]
        public void The_critic_is_announced_arrives_and_leaves_a_verdict(string cuisine)
        {
            EconomyConfig eco = ContentLoader.LoadEconomy(Paths.Content);
            ContentSet c = ContentSetLoader.Load(Paths.Content, cuisine);
            TimingConfig timing = c.SlotDurationsBp != null
                ? TimingConfig.Default().WithSlotDurations(c.SlotDurationsBp).WithEatMs(c.EatMs)
                : TimingConfig.Default();
            Simulation sim = new Simulation(eco, c, timing, Seed);

            int day = sim.CriticDay;
            _out.WriteLine($"{cuisine}: the critic's day is {day} of {eco.CampaignDays}");
            Assert.True(day > 1, "the campaign has no critic's day");

            SimEvent[] buffer = new SimEvent[4096];
            bool expected = false, verdict = false;
            int verdictPoints = -1;

            while (sim.Day <= day)
            {
                for (int i = 0; i < sim.IngredientCount; i++)
                {
                    int need = sim.RecommendedRestock(i);
                    if (need > 0) sim.Apply(new Command(sim.TickIndex, CommandKind.OrderIngredient, i, need));
                }
                sim.Apply(new Command(sim.TickIndex, CommandKind.OpenService));
                int limit = timing.ServiceTicks + 4000;
                for (int t = 0; t < limit && !sim.ServiceComplete; t++) sim.Tick();
                sim.Apply(new Command(sim.TickIndex, CommandKind.CloseDay));
                int n = sim.Events.Drain(buffer);
                for (int i = 0; i < n; i++)
                {
                    if (buffer[i].Kind == SimEventKind.CriticExpected && sim.Day == day - 1) expected = true;
                    if (buffer[i].Kind == SimEventKind.CriticVerdict) { verdict = true; verdictPoints = buffer[i].A; }
                }
                if (sim.Day == day) break;
                sim.AdvanceToNextDay();
            }

            _out.WriteLine($"  announced the morning before: {expected}; verdict on the day: {verdict} ({verdictPoints}/100); "
                           + $"CriticVisited {sim.CriticVisited}, verdict {sim.CriticVerdictCenti}");
            Assert.True(expected, "the critic was not announced the morning before");
            Assert.True(sim.CriticVisited && verdict,
                "the critic's day came and went and no verdict was recorded: the critic never came");
            Assert.InRange(verdictPoints, 0, 100);
        }
    }
}
