using System.Collections.Generic;
using System.IO;
using Lokanta.Content;
using Lokanta.Core.Content;
using Lokanta.Core.Economy;
using Lokanta.Core.Sim;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;

namespace Lokanta.Core.Tests
{
    /// <summary>
    /// docs/14-staff-system.md "Experience and level":
    ///   - 1 point for every day worked
    ///   - a level every 30 points, 3 levels at most
    ///   - +10% speed per level
    ///
    /// The ladder had been written in the content (staff-roles.json xpSpeedBp)
    /// for months and was read nowhere at all - one of the last items in the
    /// auditor's queue.
    ///
    /// An important boundary: experience does NOT shorten A DISH'S COOKING TIME.
    /// What docs/27 Decision D says for equipment holds for experience too - what
    /// shortens is the time the person is TIED UP by that job.
    /// </summary>
    public class ExperienceTests
    {
        private readonly ITestOutputHelper _out;
        public ExperienceTests(ITestOutputHelper output) { _out = output; }

        private const ulong Seed = 20260910UL;

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

        private static void Restock(Simulation sim)
        {
            for (int i = 0; i < sim.IngredientCount; i++)
            {
                int need = sim.RecommendedRestock(i);
                if (need > 0)
                    sim.Apply(new Command(sim.TickIndex, CommandKind.OrderIngredient, i, need));
            }
        }

        private static DayReport RunOneDay(Simulation sim)
        {
            Restock(sim);
            sim.Apply(new Command(sim.TickIndex, CommandKind.OpenService));
            int limit = Timing().ServiceTicks + 4000;
            for (int t = 0; t < limit; t++)
            {
                sim.Tick();
                if (sim.ServiceComplete) break;
            }
            sim.Apply(new Command(sim.TickIndex, CommandKind.CloseDay));
            DayReport rep = sim.BuildDayReport();
            sim.AdvanceToNextDay();
            return rep;
        }

        // ====================================================================
        [Fact]
        public void The_content_loads_a_four_rung_ladder()
        {
            EconomyConfig e = Economy();
            Assert.Equal(30, e.XpDaysPerLevel);
            Assert.Equal(3, e.MaxXpLevel);

            // Level 0 gives no speed, every level after that +10%.
            Assert.Equal(10_000, e.XpSpeedBp(0, true));
            Assert.Equal(11_000, e.XpSpeedBp(1, true));
            Assert.Equal(12_000, e.XpSpeedBp(2, true));
            Assert.Equal(13_000, e.XpSpeedBp(3, true));

            // Above the ceiling it clamps to the ceiling; it does not run off the end of the array.
            Assert.Equal(13_000, e.XpSpeedBp(9, true));
        }

        [Fact]
        public void A_level_is_gained_every_thirty_days()
        {
            Simulation sim = NewSim();
            Assert.Equal(0, sim.StaffLevel(0, 0));

            for (int d = 0; d < 29; d++) RunOneDay(sim);
            Assert.Equal(29, sim.StaffDaysWorked(0, 0));
            Assert.Equal(0, sim.StaffLevel(0, 0));

            RunOneDay(sim);
            Assert.Equal(30, sim.StaffDaysWorked(0, 0));
            Assert.Equal(1, sim.StaffLevel(0, 0));
        }

        [Fact]
        public void The_level_stops_at_three()
        {
            // In a 60-day campaign the third level falls on day 90: the ceiling
            // is UNREACHABLE within the campaign. docs/29 says free play carries
            // on, so the ceiling is tested all the same.
            EconomyConfig e = Economy();
            Assert.Equal(0, e.XpLevelOf(0));
            Assert.Equal(0, e.XpLevelOf(29));
            Assert.Equal(1, e.XpLevelOf(30));
            Assert.Equal(2, e.XpLevelOf(60));
            Assert.Equal(3, e.XpLevelOf(90));
            Assert.Equal(3, e.XpLevelOf(300));
        }

        [Fact]
        public void Someone_hired_later_starts_from_zero()
        {
            Simulation sim = NewSim();
            for (int d = 0; d < 35; d++) RunOneDay(sim);

            sim.Apply(new Command(sim.TickIndex, CommandKind.Hire, 0));
            Assert.Equal(2, sim.Cooks);

            // The old one has gained a level, the new one is at zero.
            Assert.Equal(1, sim.StaffLevel(0, 0));
            Assert.Equal(0, sim.StaffLevel(0, 1));
            Assert.Equal(0, sim.StaffDaysWorked(0, 1));
        }

        [Fact]
        public void Firing_and_rehiring_resets_the_experience()
        {
            // Firing takes FROM THE END: the newest one goes. Otherwise a
            // meaningless decision - "fire the most experienced" - would arise.
            // Whoever is taken back is a new person too: cutting the crew and
            // taking them back is not free.
            Simulation sim = NewSim();
            sim.Apply(new Command(sim.TickIndex, CommandKind.Hire, 0));
            for (int d = 0; d < 31; d++) RunOneDay(sim);

            // We do not expect an exact LEVEL: the trait is in play too. An
            // apprentice earns two points a day, an experienced hand earns none
            // (docs/14). What is being tested is not the level's value but that
            // it is RESET when they are fired.
            int before0 = sim.StaffLevel(0, 0);
            int days0 = sim.StaffDaysWorked(0, 0);
            Assert.True(sim.StaffDaysWorked(0, 1) > 0);

            sim.Apply(new Command(sim.TickIndex, CommandKind.Fire, 0));
            sim.Apply(new Command(sim.TickIndex, CommandKind.Hire, 0));

            Assert.Equal(2, sim.Cooks);
            Assert.Equal(before0, sim.StaffLevel(0, 0));
            Assert.Equal(days0, sim.StaffDaysWorked(0, 0));
            Assert.Equal(0, sim.StaffLevel(0, 1));
            Assert.Equal(0, sim.StaffDaysWorked(0, 1));
        }

        [Fact]
        public void Experience_does_not_touch_a_dishs_cooking_time()
        {
            // docs/27 Decision D. The ladder only divides the TIME TIED UP; the
            // dish's wall-clock time (prepMs) stays fixed in the content.
            ContentSet c = Content();
            EconomyConfig e = Economy();

            int prep = 0;
            for (int i = 0; i < c.Dishes.Length; i++)
                if (c.Dishes[i].PrepMs > prep) prep = c.Dishes[i].PrepMs;
            Assert.True(prep > 0);

            long busyAtZero = Fx.MulDiv(prep, Fx.One, e.XpSpeedBp(0, true));
            long busyAtTop = Fx.MulDiv(prep, Fx.One, e.XpSpeedBp(3, true));
            _out.WriteLine($"tied up {busyAtZero} -> {busyAtTop} ms (prepMs {prep})");

            Assert.Equal(prep, (int)busyAtZero);
            Assert.True(busyAtTop < busyAtZero);
        }

        [Fact]
        public void Experience_makes_a_measurable_difference()
        {
            // The same seed, the same strategy: experience is the only variable.
            // This test measures not the balance but THAT THE MECHANIC IS WIRED
            // UP - if experience touched nothing, the two windows would come out
            // the same.
            Simulation sim = NewSim();

            int early = 0, late = 0;
            for (int d = 0; d < 10; d++) early += RunOneDay(sim).ServedPeople;
            for (int d = 0; d < 80; d++) RunOneDay(sim);
            for (int d = 0; d < 10; d++) late += RunOneDay(sim).ServedPeople;

            _out.WriteLine($"first ten days {early} people, days 90-100 {late} people");
            Assert.Equal(3, sim.StaffLevel(0, 0));
            Assert.NotEqual(early, late);
        }

        [Fact]
        public void The_wage_table_matches_its_generator()
        {
            // weeklyWageMultiplierBp and weeklyXpWageGrowthBp write THE SAME
            // THING in two places. The table was not deleted (the balance harness
            // reads it) but it is now immutable: ContentLoader validates it at
            // startup. This test shows that the validation runs - a broken table
            // MUST NOT open the game.
            string dir = Paths.Content;
            string economyJson = File.ReadAllText(Path.Combine(dir, "economy.json"));
            string rolesJson = File.ReadAllText(Path.Combine(dir, "staff-roles.json"));

            EconomyConfig ok = ContentLoader.LoadEconomy(dir);
            Assert.Equal(220, ok.WeeklyXpWageGrowthBp);

            EconomyDto dto = JsonConvert.DeserializeObject<EconomyDto>(economyJson);
            List<StaffRoleDto> roles =
                JsonConvert.DeserializeObject<List<StaffRoleDto>>(rolesJson);

            dto.Staffing.WeeklyWageMultiplierBp[3] += 50;

            ContentException ex = Assert.Throws<ContentException>(
                () => ContentLoader.Build(dto, roles));
            _out.WriteLine(ex.Message);
            Assert.Contains("weeklyWageMultiplierBp", ex.Message);
        }

        [Fact]
        public void The_game_does_not_open_if_the_ladder_is_broken()
        {
            string dir = Paths.Content;
            EconomyDto dto = JsonConvert.DeserializeObject<EconomyDto>(
                File.ReadAllText(Path.Combine(dir, "economy.json")));
            List<StaffRoleDto> roles = JsonConvert.DeserializeObject<List<StaffRoleDto>>(
                File.ReadAllText(Path.Combine(dir, "staff-roles.json")));

            // A decreasing ladder: staff who gain experience do not get slower.
            roles[0].XpSpeedBp = new List<int> { 10000, 11000, 9000, 13000 };
            Assert.Throws<ContentException>(() => ContentLoader.Build(dto, roles));

            // The zeroth rung must be 10000: level 0 gives no speed.
            roles[0].XpSpeedBp = new List<int> { 12000, 13000, 14000, 15000 };
            Assert.Throws<ContentException>(() => ContentLoader.Build(dto, roles));
        }
    }
}
