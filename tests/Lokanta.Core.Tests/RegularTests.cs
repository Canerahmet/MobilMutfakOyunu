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
    /// Named regular customers. docs/11: "a named customer is one single person,
    /// written by hand, with a story, and always the same person. An archetype, by
    /// contrast, produces thousands of customers."
    ///
    /// The most important invariant is at the bottom: a regular DOES NOT INFLATE
    /// the demand. A named customer TAKES a place in the day's plan; otherwise
    /// every new name would grow the economy and the calibration would drift with
    /// every content addition.
    /// </summary>
    public class RegularTests
    {
        private readonly ITestOutputHelper _out;
        public RegularTests(ITestOutputHelper output) { _out = output; }

        private const ulong Seed = 20260910UL;

        private static EconomyConfig Economy() => ContentLoader.LoadEconomy(Paths.Content);
        private static ContentSet Content(string cuisine) =>
            ContentSetLoader.Load(Paths.Content, cuisine);

        private static TimingConfig Timing(ContentSet c) =>
            c.SlotDurationsBp != null
                ? TimingConfig.Default().WithSlotDurations(c.SlotDurationsBp).WithEatMs(c.EatMs)
                : TimingConfig.Default();

        private static Simulation NewSim(string cuisine)
        {
            ContentSet c = Content(cuisine);
            return new Simulation(Economy(), c, Timing(c), Seed);
        }

        private static void Restock(Simulation sim)
        {
            for (int i = 0; i < sim.IngredientCount; i++)
            {
                int need = sim.RecommendedRestock(i);
                if (need > 0)
                    sim.Apply(new Command(sim.TickIndex, CommandKind.OrderIngredient, i, need));
            }
        }

        private static DayReport RunOneDay(Simulation sim, ContentSet c)
        {
            Restock(sim);
            sim.Apply(new Command(sim.TickIndex, CommandKind.OpenService));
            int limit = Timing(c).ServiceTicks + 4000;
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
        // Content
        // ====================================================================
        [Theory]
        [InlineData("fastfood")]
        [InlineData("turk")]
        public void Ten_regulars_per_cuisine(string cuisine)
        {
            ContentSet c = Content(cuisine);
            Assert.Equal(10, c.Regulars.Length);      // the docs/09 inventory

            foreach (RegularDef r in c.Regulars)
            {
                Assert.True(r.ArchetypeIndex >= 0 && r.ArchetypeIndex < c.Archetypes.Length);
                Assert.True(r.FavouriteDish >= 0 && r.FavouriteDish < c.Dishes.Length);
                Assert.True(r.ArrivesFromDay >= 1);
                Assert.Equal(3, r.Story.Length);       // docs/09: three to four beats
            }
        }

        [Fact]
        public void Their_favourite_dish_must_be_OPEN_on_the_day_they_arrive()
        {
            // Otherwise the mechanic runs unfairly from day one: they are met with
            // a failing that is out of the player's hands.
            foreach (string cuisine in new[] { "fastfood", "turk" })
            {
                ContentSet c = Content(cuisine);
                foreach (RegularDef r in c.Regulars)
                    Assert.True(c.Dishes[r.FavouriteDish].UnlockDay <= r.ArrivesFromDay,
                                cuisine + "/" + r.Id + " arrives while their favourite dish is closed");
            }
        }

        [Fact]
        public void Tab_candidates_exist_only_in_the_Turkish_cuisine()
        {
            // docs/07: the tab is the Turkish cuisine's signature mechanic. Writing
            // an eligible regular in fast food means writing a field that will
            // never run.
            int turkish = 0;
            foreach (RegularDef r in Content("turk").Regulars)
                if (r.TabEligible) turkish++;
            Assert.True(turkish >= 4, "too few tab candidates in the Turkish cuisine");

            foreach (RegularDef r in Content("fastfood").Regulars)
                Assert.False(r.TabEligible);
        }

        [Fact]
        public void A_tab_candidate_from_another_cuisine_is_rejected()
        {
            List<RegularDto> regs = Load<List<RegularDto>>("regulars", "fastfood.json");
            regs[0].TabEligible = true;

            ContentException ex = Assert.Throws<ContentException>(
                () => BuildFastfood(regs));
            _out.WriteLine(ex.Message);
            Assert.Contains("eligible for a tab", ex.Message);
        }

        [Fact]
        public void A_non_existent_archetype_is_rejected()
        {
            List<RegularDto> regs = Load<List<RegularDto>>("regulars", "fastfood.json");
            regs[2].ArchetypeBase = "olmayan_arketip";   // "a nonexistent archetype"
            Assert.Throws<ContentException>(() => BuildFastfood(regs));
        }

        [Fact]
        public void A_customer_whose_favourite_dish_is_locked_is_rejected()
        {
            List<RegularDto> regs = Load<List<RegularDto>>("regulars", "fastfood.json");
            regs[0].FavouriteDish = "buzlu_cay";        // unlocks on day 50
            ContentException ex = Assert.Throws<ContentException>(
                () => BuildFastfood(regs));
            _out.WriteLine(ex.Message);
            Assert.Contains("unlocks on day", ex.Message);
        }

        [Fact]
        public void A_broken_story_order_is_rejected()
        {
            List<RegularDto> regs = Load<List<RegularDto>>("regulars", "fastfood.json");
            regs[0].Story[2].RequiresVisits = 1;        // a decreasing threshold
            Assert.Throws<ContentException>(() => BuildFastfood(regs));
        }

        private static T Load<T>(params string[] parts)
        {
            string path = Paths.Content;
            foreach (string p in parts) path = Path.Combine(path, p);
            return JsonConvert.DeserializeObject<T>(File.ReadAllText(path));
        }

        private static void BuildFastfood(List<RegularDto> regs)
        {
            ContentSetLoader.Build(
                "fastfood",
                Load<List<IngredientDto>>("ingredients.json"),
                Load<List<DishDto>>("dishes", "fastfood.json"),
                Sharedplus("fastfood"),
                Load<EquipmentFileDto>("equipment.json"),
                Load<CuisineDto>("cuisines", "fastfood.json"),
                15, regs);
        }

        // ====================================================================
        // Behaviour
        // ====================================================================
        [Fact]
        public void They_DO_NOT_INFLATE_the_demand()
        {
            // This file's reason to exist. A named customer TAKES a place in the
            // day's plan, they are not ADDED to it. With the same seed, content
            // with regulars and content without must plan the same number of
            // customers.
            ContentSet withReg = Content("turk");
            ContentSet without = ContentSetLoader.Build(
                "turk",
                Load<List<IngredientDto>>("ingredients.json"),
                Load<List<DishDto>>("dishes", "turk.json"),
                Sharedplus("turk"),
                Load<EquipmentFileDto>("equipment.json"),
                Load<CuisineDto>("cuisines", "turk.json"),
                15, null);

            Assert.NotEmpty(withReg.Regulars);
            Assert.Empty(without.Regulars);

            // The first regular arrives on the THIRD day. So days 1 and 2 are
            // identical in both runs, which means day 3's reputation is identical
            // too - and day 3's PLAN must come out identical. This isolates the
            // mechanic's structural invariant.
            //
            // On the days after that the numbers DIVERGE, and it is right that
            // they should: a customer who finds their favourite dish leaves happier,
            // the reputation moves differently, the demand differs. That difference
            // is the mechanic itself; it is not inflation.
            int[] a = PlannedDays(withReg, 3);
            int[] b = PlannedDays(without, 3);
            _out.WriteLine($"with regulars    {string.Join(", ", a)}");
            _out.WriteLine($"without regulars {string.Join(", ", b)}");
            Assert.Equal(b, a);
        }

        private static int[] PlannedDays(ContentSet c, int days)
        {
            Simulation sim = new Simulation(Economy(), c, Timing(c), Seed);
            int[] out_ = new int[days];
            for (int d = 0; d < days; d++) out_[d] = RunOneDay(sim, c).PlannedParties;
            return out_;
        }

        private static List<ArchetypeDto> Sharedplus(string cuisine)
        {
            List<ArchetypeDto> a = Load<List<ArchetypeDto>>("archetypes", "shared.json");
            a.AddRange(Load<List<ArchetypeDto>>("archetypes", cuisine + ".json"));
            return a;
        }

        private static long PlannedOver(ContentSet c, int days)
        {
            Simulation sim = new Simulation(Economy(), c, Timing(c), Seed);
            long total = 0;
            for (int d = 0; d < days; d++) total += RunOneDay(sim, c).PlannedParties;
            return total;
        }

        [Fact]
        public void They_do_not_arrive_before_their_day()
        {
            ContentSet c = Content("turk");
            Simulation sim = NewSim("turk");

            // The latest customer arrives on day 44; they must not show up at all
            // before day 20.
            int late = c.Regulars.Length - 1;
            Assert.True(c.Regulars[late].ArrivesFromDay > 20);

            for (int d = 0; d < 20; d++) RunOneDay(sim, c);
            Assert.Equal(0, sim.RegularVisits(late));
        }

        [Fact]
        public void An_early_customer_does_call_in()
        {
            ContentSet c = Content("turk");
            Simulation sim = NewSim("turk");
            for (int d = 0; d < 30; d++) RunOneDay(sim, c);

            int visits = 0;
            for (int i = 0; i < c.Regulars.Length; i++)
            {
                if (sim.RegularVisits(i) > 0)
                    _out.WriteLine($"{c.Regulars[i].Id,-16} {sim.RegularVisits(i),3} visits, " +
                                   $"avg satisfaction {sim.RegularSatisfactionCenti(i) / 100.0:0.0}, " +
                                   $"beat {sim.RegularBeat(i)}");
                visits += sim.RegularVisits(i);
            }
            Assert.True(visits > 0, "not one regular came in thirty days");
        }

        [Fact]
        public void A_story_beat_opens_with_visits()
        {
            ContentSet c = Content("turk");
            Simulation sim = NewSim("turk");
            for (int d = 0; d < 60; d++) RunOneDay(sim, c);

            int opened = 0, top = 0;
            for (int i = 0; i < c.Regulars.Length; i++)
            {
                opened += sim.RegularBeat(i);
                if (sim.RegularBeat(i) > top) top = sim.RegularBeat(i);
            }
            _out.WriteLine($"{opened} beats opened in sixty days, the furthest customer is on beat {top}");

            Assert.True(opened > 0, "not one story beat opened in sixty days");
            // A beat must not open before its THRESHOLD is met.
            for (int i = 0; i < c.Regulars.Length; i++)
            {
                int beat = sim.RegularBeat(i);
                if (beat == 0) continue;
                Assert.True(sim.RegularVisits(i) >= c.Regulars[i].Story[beat - 1].RequiresVisits);
            }
        }

        [Fact]
        public void The_save_carries_the_regulars_history()
        {
            ContentSet c = Content("turk");
            Simulation sim = NewSim("turk");
            for (int d = 0; d < 25; d++) RunOneDay(sim, c);

            JsonStateWriter w = new JsonStateWriter();
            sim.Write(w);
            Simulation restored = NewSim("turk");
            restored.Restore(new JsonStateReader(w.ToJson()));

            for (int i = 0; i < c.Regulars.Length; i++)
            {
                Assert.Equal(sim.RegularVisits(i), restored.RegularVisits(i));
                Assert.Equal(sim.RegularBeat(i), restored.RegularBeat(i));
                Assert.Equal(sim.RegularSatisfactionCenti(i),
                             restored.RegularSatisfactionCenti(i));
            }
            Assert.Equal(sim.StateHash(), restored.StateHash());
        }

        [Fact]
        public void A_tab_is_now_opened_for_a_NAMED_customer()
        {
            // docs/13 put the tab eligibility field in the regulars file, and that
            // is the right place for it: a tab is opened for someone whose name you
            // know. The rule used to be derived from "a frequently arriving
            // archetype" - an approach that worked but had no identity to it.
            ContentSet c = Content("turk");
            Simulation sim = NewSim("turk");
            while (sim.Day < sim.SignatureFromDay) RunOneDay(sim, c);

            int checkedParties = 0;
            for (int d = 0; d < 20; d++)
            {
                Restock(sim);
                sim.Apply(new Command(sim.TickIndex, CommandKind.OpenService));
                int limit = Timing(c).ServiceTicks + 4000;
                for (int t = 0; t < limit; t++)
                {
                    sim.Tick();
                    for (int p = 0; p < Simulation.MaxParties; p++)
                    {
                        if (!sim.CreditEligible(p)) continue;
                        int reg = sim.PartyRegular(p);
                        Assert.True(reg >= 0, "a tab is being opened for a nameless customer");
                        Assert.True(c.Regulars[reg].TabEligible);
                        checkedParties++;
                    }
                    if (sim.ServiceComplete) break;
                }
                sim.Apply(new Command(sim.TickIndex, CommandKind.CloseDay));
                sim.AdvanceToNextDay();
            }
            _out.WriteLine($"tab eligibility was seen {checkedParties} times");
            Assert.True(checkedParties > 0, "no tab was ever asked for in twenty days");
        }
    }
}
