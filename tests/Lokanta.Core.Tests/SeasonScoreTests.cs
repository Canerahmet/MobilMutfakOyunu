using Lokanta.Content;
using Lokanta.Core.Content;
using Lokanta.Core.Economy;
using Lokanta.Core.Sim;
using Xunit;
using Xunit.Abstractions;

namespace Lokanta.Core.Tests
{
    /// <summary>
    /// The year-end evaluation. docs/08-endgame.md.
    ///
    /// This feature had once been DESIGNED but NEVER WIRED UP: the screen was
    /// written, CampaignDays sat in the content, and the sixtieth day came and
    /// went. The tests did not see that gap because none of them played to the
    /// end of the campaign.
    /// </summary>
    public sealed class SeasonScoreTests
    {
        private readonly ITestOutputHelper _out;
        public SeasonScoreTests(ITestOutputHelper o) { _out = o; }

        private static Simulation NewSim(string cuisine = "fastfood")
        {
            EconomyConfig economy = ContentLoader.LoadEconomy(Paths.Content);
            ContentSet content = ContentSetLoader.Load(Paths.Content, cuisine);
            TimingConfig timing = content.SlotDurationsBp != null
                ? TimingConfig.Default()
                    .WithSlotDurations(content.SlotDurationsBp)
                    .WithEatMs(content.EatMs)
                : TimingConfig.Default();
            return new Simulation(economy, content, timing, 20260911UL);
        }

        [Fact]
        public void The_evaluation_does_not_open_before_the_campaign_ends()
        {
            Simulation sim = NewSim();
            Assert.False(sim.SeasonJustEnded);
            Assert.True(sim.CampaignDays >= 30, "the campaign length is not coming from the content");
        }

        [Fact]
        public void The_evaluation_opens_a_single_time()
        {
            Simulation sim = NewSim();
            RunTo(sim, sim.CampaignDays + 1);

            Assert.True(sim.SeasonJustEnded, "the campaign is over but the evaluation does not open");

            sim.MarkSeasonScored();
            Assert.False(sim.SeasonJustEnded, "the evaluation opens a second time");
        }

        [Fact]
        public void All_seven_axes_stay_within_their_bounds()
        {
            Simulation sim = NewSim();
            RunTo(sim, sim.CampaignDays + 1);

            SeasonScore s = sim.Score();
            for (int i = 0; i < SeasonScore.AxisCount; i++)
            {
                int v = s.AxisAt(i);
                Assert.True(v >= 0 && v <= 100,
                            "the " + SeasonScore.AxisKey(i) + " axis is out of range: " + v);
            }
            Assert.True(s.Total >= 0 && s.Total <= 100);
            Assert.True(s.Plaque >= 0 && s.Plaque <= 3);

            _out.WriteLine($"total {s.Total}, plaque {s.Plaque}");
            for (int i = 0; i < SeasonScore.AxisCount; i++)
                _out.WriteLine($"  {SeasonScore.AxisKey(i)} = {s.AxisAt(i)}");
        }

        [Fact]
        public void A_player_who_does_nothing_does_not_get_full_marks()
        {
            // A passive run: no stock is bought, nobody is hired, there is no
            // expansion. The score MUST be low - otherwise the evaluation is
            // measuring nothing at all.
            Simulation sim = NewSim();
            RunTo(sim, sim.CampaignDays + 1);

            SeasonScore s = sim.Score();
            Assert.True(s.Total < 60,
                        "a player who does nothing scores " + s.Total);
            Assert.True(s.Place < 50, "a player who never expands has a high place score");
        }

        [Fact]
        public void Going_down_the_ladder_lowers_resilience()
        {
            Simulation sim = NewSim();
            RunTo(sim, sim.CampaignDays + 1);

            SeasonScore s = sim.Score();
            if (sim.DebtRungs > 0)
                Assert.True(s.Resilience < 100,
                            "the ladder was used and yet resilience is full marks");
            else
                Assert.True(s.Resilience >= 85,
                            "there was never any debt and yet resilience is low");
        }

        /// <summary>Advances day by day without doing anything.</summary>
        private static void RunTo(Simulation sim, int day)
        {
            while (sim.Day < day)
            {
                sim.Apply(new Command(sim.TickIndex, CommandKind.OpenService));
                for (int t = 0; t < 5000; t++)
                {
                    sim.Tick();
                    if (sim.ServiceComplete) break;
                }
                sim.Apply(new Command(sim.TickIndex, CommandKind.CloseDay));
                sim.AdvanceToNextDay();
            }
        }
    }
}
