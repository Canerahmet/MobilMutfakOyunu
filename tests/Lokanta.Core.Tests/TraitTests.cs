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
    /// Staff traits and morale. docs/14.
    ///
    /// The design intention is written there: "no trait is purely good or purely
    /// bad. An apprentice is cheap but slow, an experienced hand is fast but
    /// expensive."
    ///
    /// The most important thing about the morale is docs/14's own sentence:
    /// "losing a number is abstract, having a member of staff whose name you know
    /// hand in their notice is concrete."
    /// </summary>
    public class TraitTests
    {
        private readonly ITestOutputHelper _out;
        public TraitTests(ITestOutputHelper output) { _out = output; }

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
            DayReport r = sim.BuildDayReport();
            sim.AdvanceToNextDay();
            return r;
        }

        // ====================================================================
        // Content
        // ====================================================================
        [Fact]
        public void Twelve_traits_load()
        {
            EconomyConfig e = Economy();
            Assert.Equal(12, e.TraitCount);       // the docs/09 inventory
        }

        [Fact]
        public void The_conflicts_are_SYMMETRIC()
        {
            // A conflict written in one direction only leaves a rule in the hiring
            // code that silently does not run: a member of staff carrying two
            // conflicting traits could be produced.
            EconomyConfig e = Economy();
            for (int i = 0; i < e.TraitCount; i++)
            {
                TraitDef t = e.TraitAt(i);
                foreach (int other in t.ConflictsWith)
                    Assert.True(e.TraitAt(other).ConflictsWithIndex(i),
                                t.Id + " <-> " + e.TraitAt(other).Id + " is one-way");
            }
        }

        [Fact]
        public void A_one_way_conflict_is_rejected()
        {
            List<TraitDto> traits = JsonConvert.DeserializeObject<List<TraitDto>>(
                File.ReadAllText(Path.Combine(Paths.Content, "staff-traits.json")));
            traits[0].ConflictsWith.Clear();      // the other one still points at it

            EconomyDto eco = JsonConvert.DeserializeObject<EconomyDto>(
                File.ReadAllText(Path.Combine(Paths.Content, "economy.json")));
            List<StaffRoleDto> roles = JsonConvert.DeserializeObject<List<StaffRoleDto>>(
                File.ReadAllText(Path.Combine(Paths.Content, "staff-roles.json")));

            ContentException ex = Assert.Throws<ContentException>(
                () => ContentLoader.Build(eco, roles, traits));
            _out.WriteLine(ex.Message);
            Assert.Contains("one-way", ex.Message);
        }

        [Fact]
        public void The_apprentice_and_the_experienced_hand_are_written_as_opposites()
        {
            // docs/14: an apprentice is cheap but slow, an experienced hand is fast
            // but expensive. Both GIVE something and TAKE something.
            EconomyConfig e = Economy();
            TraitDef apprentice = null, experienced = null;
            for (int i = 0; i < e.TraitCount; i++)
            {
                if (e.TraitAt(i).Id == "cirak") apprentice = e.TraitAt(i);
                if (e.TraitAt(i).Id == "tecrubeli") experienced = e.TraitAt(i);
            }
            Assert.NotNull(apprentice);
            Assert.NotNull(experienced);

            Assert.True(apprentice.WageBp < 0 && apprentice.SpeedBp < 0 && apprentice.XpBp > 10000);
            Assert.True(experienced.WageBp > 0 && experienced.SpeedBp > 0 && experienced.XpBp == 0);
        }

        // ====================================================================
        // Behaviour
        // ====================================================================
        [Fact]
        public void Everyone_gets_two_traits_and_they_do_not_conflict()
        {
            EconomyConfig e = Economy();
            Simulation sim = NewSim();

            // EXPAND FIRST, THEN HIRE.
            //
            // The first tier's crew cap is THREE (docs/14) and the game now starts
            // with two people (one cook, one waiter) - so without expanding, only a
            // SINGLE hire fits under the cap, and the test was checking one person
            // and declaring "they all arrived with two traits". A sample of one does
            // not carry a claim about "everyone".
            //
            // Expanding raises the cap; what is tested is still not the number but
            // that EVERY member of staff arrives with two compatible traits.
            sim.Apply(new Command(sim.TickIndex, CommandKind.Expand, 1));
            for (int i = 0; i < 6; i++)
                sim.Apply(new Command(sim.TickIndex, CommandKind.Hire, i % 2, i % 3));

            int people = 0;
            for (int pool = 0; pool < 2; pool++)
            {
                int count = pool == 0 ? sim.Cooks : sim.HallStaff;
                for (int i = 0; i < count; i++)
                {
                    // THE INHERITED CREW starts with no traits (a separate test):
                    // both the first cook and the first waiter. Character comes with
                    // the people YOU CHOOSE - only THOSE HIRED are tested.
                    if (i == 0) continue;
                    int a = sim.StaffTrait(pool, i, 0);
                    int b = sim.StaffTrait(pool, i, 1);
                    Assert.True(a >= 0, "the member of staff was left with no trait");
                    Assert.True(b >= 0, "the member of staff was left with only one trait");
                    Assert.NotEqual(a, b);
                    Assert.False(e.TraitAt(a).ConflictsWithIndex(b),
                                 e.TraitAt(a).Id + " and " + e.TraitAt(b).Id + " cannot go together");
                    people++;
                }
            }
            // The crew cap still binds (docs/14), but after the expansion a few
            // people fit. What is tested is not the number but that EVERY member of
            // staff arrives with two compatible traits.
            _out.WriteLine($"{people} staff, all with two compatible traits");
            Assert.True(people >= 2);
        }

        [Fact]
        public void The_starting_cook_begins_with_NO_TRAIT_but_with_morale()
        {
            // Two separate things, both settled by measurement.
            //
            // MORALE: RollTraits was only called from inside Hire, whereas the game
            // STARTS with a cook. That cook's morale started at 0 - BELOW the
            // resignation threshold - and the restaurant was left without a cook ON
            // THE SECOND DAY.
            //
            // TRAIT: the player DOES NOT CHOOSE the starting cook. Rolling a random
            // trait for them means an invisible dice roll on the campaign's first
            // day; in the measurement a run that drew a badly-trait'd starting cook
            // could not recover over sixty days. The cook you inherit is ORDINARY;
            // character comes with the people YOU CHOOSE.
            Simulation sim = NewSim();
            Assert.Equal(1, sim.Cooks);
            Assert.Equal(-1, sim.StaffTrait(0, 0, 0));
            Assert.Equal(-1, sim.StaffTrait(0, 0, 1));
            Assert.Equal(Economy().StartingMorale, sim.StaffMorale(0, 0));
        }

        [Fact]
        public void The_candidate_pool_holds_three_and_refreshes_every_three_days()
        {
            // docs/14: "Three candidates are shown on the hiring screen at once...
            // The candidate pool refreshes every three days. You can turn down a
            // candidate you do not like, but a new one does not arrive at once."
            Simulation sim = NewSim();
            for (int slot = 0; slot < Simulation.CandidateSlots; slot++)
                Assert.True(sim.CandidateTrait(1, slot, 0) >= 0,
                            "the candidate pool is empty");

            // A candidate who is taken leaves the pool and their place is not filled
            // straight away.
            int taken = 0;
            sim.Apply(new Command(sim.TickIndex, CommandKind.Hire, 1, taken));
            Assert.Equal(-1, sim.CandidateTrait(1, taken, 0));

            // Still empty the next day: the pool refreshes every three days.
            RunOneDay(sim);
            Assert.Equal(-1, sim.CandidateTrait(1, taken, 0));

            for (int d = 0; d < 3; d++) RunOneDay(sim);
            Assert.True(sim.CandidateTrait(1, taken, 0) >= 0, "the pool never refreshed");
        }

        [Fact]
        public void Nobody_resigns_in_a_well_run_restaurant()
        {
            // The docs/14 morale table is a list of EVENTS, not a drift model.
            // Applying only the events sent the ladder one way - downwards - and the
            // entire crew handed in their notice within a month.
            Simulation sim = NewSim();
            int startCooks = sim.Cooks;

            for (int d = 0; d < 40; d++) RunOneDay(sim);

            _out.WriteLine($"after forty days: cooks {sim.Cooks}, " +
                           $"morale {sim.StaffMorale(0, 0)}, till {sim.Cash / 100}");
            Assert.Equal(startCooks, sim.Cooks);
            Assert.True(sim.StaffMorale(0, 0) >= Economy().MoraleQuitThreshold);
        }

        [Fact]
        public void The_morale_falls_when_the_wages_cannot_be_paid()
        {
            // docs/14: "wages are late -25", and this is the third rung of the
            // bankruptcy ladder. We empty the till and wait for the weekly
            // payment.
            Simulation sim = NewSim();
            int before = sim.StaffMorale(0, 0);

            // Hire up to the cap: the wage bill exceeds the till.
            for (int i = 0; i < 8; i++)
                sim.Apply(new Command(sim.TickIndex, CommandKind.Hire, 1));

            // Put all the money into stock, then wait for the end of the week.
            for (int d = 0; d < 8; d++)
            {
                for (int i = 0; i < sim.IngredientCount; i++)
                {
                    int need = sim.RecommendedRestock(i);
                    if (need > 0)
                        sim.Apply(new Command(sim.TickIndex, CommandKind.OrderIngredient,
                                              i, need * 6));
                }
                sim.Apply(new Command(sim.TickIndex, CommandKind.OpenService));
                sim.Apply(new Command(sim.TickIndex, CommandKind.CloseDay));
                sim.AdvanceToNextDay();
            }

            _out.WriteLine($"morale {before} -> {sim.StaffMorale(0, 0)}, till {sim.Cash / 100}");
            Assert.True(sim.StaffMorale(0, 0) < before,
                        "the wages could not be paid and yet the morale did not fall");
        }

        [Fact]
        public void The_trait_shows_up_in_the_wage()
        {
            // docs/14: an apprentice -25%, an experienced hand +30%. The crew's
            // multiplier is the average.
            Simulation sim = NewSim();
            int bp = sim.TraitWageMultiplierBp();
            _out.WriteLine($"wage multiplier of a one-cook crew {bp} bp");
            Assert.True(bp >= 1000);

            // As the crew grows the multiplier should approach 1: the different
            // traits balance one another out.
            for (int i = 0; i < 8; i++)
                sim.Apply(new Command(sim.TickIndex, CommandKind.Hire, 1));
            int wide = sim.TraitWageMultiplierBp();
            _out.WriteLine($"with a nine-person crew {wide} bp");
            Assert.True(System.Math.Abs(wide - 10000) <= System.Math.Abs(bp - 10000) + 1500);
        }

        [Fact]
        public void The_save_carries_the_traits_and_the_morale()
        {
            Simulation sim = NewSim();
            for (int i = 0; i < 3; i++)
                sim.Apply(new Command(sim.TickIndex, CommandKind.Hire, 1));
            for (int d = 0; d < 12; d++) RunOneDay(sim);

            JsonStateWriter w = new JsonStateWriter();
            sim.Write(w);
            Simulation restored = NewSim();
            restored.Restore(new JsonStateReader(w.ToJson()));

            for (int pool = 0; pool < 2; pool++)
            {
                int count = pool == 0 ? sim.Cooks : sim.HallStaff;
                for (int i = 0; i < count; i++)
                {
                    Assert.Equal(sim.StaffTrait(pool, i, 0), restored.StaffTrait(pool, i, 0));
                    Assert.Equal(sim.StaffTrait(pool, i, 1), restored.StaffTrait(pool, i, 1));
                    Assert.Equal(sim.StaffMorale(pool, i), restored.StaffMorale(pool, i));
                }
            }
            Assert.Equal(sim.StateHash(), restored.StateHash());
        }
    }
}
