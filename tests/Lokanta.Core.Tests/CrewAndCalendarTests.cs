using Lokanta.Content;
using Lokanta.Core.Content;
using Lokanta.Core.Economy;
using Lokanta.Core.Sim;
using Xunit;
using Xunit.Abstractions;

namespace Lokanta.Core.Tests
{
    /// <summary>
    /// THREE THINGS docs/64 FOUND THE GAME SAYING AND NOT DOING.
    ///
    /// The crew axis gave its best mark to the bot that hires to the cap and
    /// loses money. Firing was an array shuffle while docs/14 promised a
    /// week's severance and a morale hit. The tab, the combo and the seasons
    /// arrived with no string. Each is asserted as a relationship the old
    /// code breaks: hiring past the peak's need must not raise the crew
    /// mark; a firing must cost cash and morale; the calendar must emit what
    /// it does on the day it does it.
    /// </summary>
    public sealed class CrewAndCalendarTests
    {
        private readonly ITestOutputHelper _out;
        public CrewAndCalendarTests(ITestOutputHelper o) { _out = o; }

        private const ulong Seed = 20260919UL;

        private static Simulation Fresh(string cuisine, out ContentSet content)
        {
            EconomyConfig eco = ContentLoader.LoadEconomy(Paths.Content);
            content = ContentSetLoader.Load(Paths.Content, cuisine);
            TimingConfig timing = content.SlotDurationsBp != null
                ? TimingConfig.Default().WithSlotDurations(content.SlotDurationsBp)
                                        .WithEatMs(content.EatMs)
                : TimingConfig.Default();
            return new Simulation(eco, content, timing, Seed);
        }

        private static void Hire(Simulation sim, int pool)
        {
            sim.Apply(new Command(sim.TickIndex, CommandKind.Hire, pool, 0));
        }

        [Fact]
        public void Hiring_past_what_the_peak_needs_lowers_the_crew_mark()
        {
            Simulation sim = Fresh("turk", out _);
            Crew need = sim.RequiredCrewPeak();
            int needed = need.Cooks + need.Hall;
            _out.WriteLine($"the peak needs {need.Cooks} cooks and {need.Hall} hall; cap {sim.StaffCap}");

            // Bring the roster to at least the need, read the mark, then hire
            // one more and read it again. Morale is the same fresh value for
            // everybody, so the difference is the roster half alone.
            while (sim.Cooks < need.Cooks) Hire(sim, 0);
            while (sim.HallStaff < need.Hall) Hire(sim, 1);
            int before = sim.Cooks + sim.HallStaff;
            int right = sim.Score().Crew;

            Hire(sim, 1);
            Assert.True(sim.Cooks + sim.HallStaff == before + 1, "the extra hire was refused, nothing to compare");
            int over = sim.Score().Crew;
            _out.WriteLine($"crew mark with {before} on the roster {right}, with {sim.Cooks + sim.HallStaff} on it {over}");

            Assert.True(sim.Cooks + sim.HallStaff > needed, "could not hire past the need, nothing to compare");
            // Decided 19 September: a hand past the need costs the mark, as a
            // missing one does. Not merely "no higher" - LOWER.
            Assert.True(over < right,
                $"hiring past the peak's need left the crew mark at {over} against {right}: an idle extra hand is free");
        }

        [Fact]
        public void Letting_somebody_go_costs_a_weeks_wage_and_the_rest_take_it_badly()
        {
            Simulation sim = Fresh("turk", out _);
            Hire(sim, 1);
            Hire(sim, 1);
            Assert.True(sim.HallStaff >= 2, "two hall staff were needed for the morale of the one who stays");

            long cashBefore = sim.Cash;
            int moraleOfStayer = sim.StaffMorale(1, 1);
            sim.Apply(new Command(sim.TickIndex, CommandKind.Fire, 1, 0));

            _out.WriteLine($"cash {cashBefore} -> {sim.Cash}; the colleague's morale {moraleOfStayer} -> {sim.StaffMorale(1, 0)}");
            Assert.True(sim.Cash < cashBefore, "the firing cost nothing: docs/14 promises a week's wage in severance");
            Assert.True(sim.StaffMorale(1, 0) < moraleOfStayer,
                "the colleague who stayed was untouched: docs/14 promises a morale hit on the rest of the crew");
        }

        [Theory]
        [InlineData("turk")]
        [InlineData("fastfood")]
        public void The_calendar_announces_the_signature_and_the_seasons(string cuisine)
        {
            Simulation sim = Fresh(cuisine, out ContentSet c);
            int openDay = sim.SignatureFromDay;
            int seasonLen = ContentLoader.LoadEconomy(Paths.Content).SeasonDays;
            Assert.True(openDay > 1 && seasonLen > 1, "the content has no calendar to announce");

            int signatureOn = -1, seasonOn = -1, seasonTo = -1;
            SimEvent[] buffer = new SimEvent[4096];
            for (int day = 1; day <= seasonLen + 1 && day <= 62; day++)
            {
                sim.Apply(new Command(sim.TickIndex, CommandKind.OpenService));
                sim.Apply(new Command(sim.TickIndex, CommandKind.CloseDay));
                sim.AdvanceToNextDay();
                int n = sim.Events.Drain(buffer);
                for (int i = 0; i < n; i++)
                {
                    SimEvent e = buffer[i];
                    if (e.Kind == SimEventKind.SignatureOpened && signatureOn < 0) signatureOn = sim.Day;
                    if (e.Kind == SimEventKind.SeasonChanged && seasonOn < 0) { seasonOn = sim.Day; seasonTo = e.A; }
                }
                if (signatureOn > 0 && seasonOn > 0) break;
            }
            _out.WriteLine($"{cuisine}: signature announced on day {signatureOn} (opens {openDay}); "
                           + $"season announced on day {seasonOn} as season {seasonTo} (length {seasonLen})");
            Assert.Equal(openDay, signatureOn);
            Assert.Equal(seasonLen + 1, seasonOn);
            Assert.Equal(1, seasonTo);
        }
    }
}
