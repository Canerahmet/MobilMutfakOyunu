using System.Collections.Generic;
using Lokanta.Content;
using Lokanta.Core.Content;
using Lokanta.Core.Economy;
using Lokanta.Core.Sim;
using Xunit;
using Xunit.Abstractions;

namespace Lokanta.Core.Tests
{
    /// <summary>
    /// THE SECOND CUISINE. This file exists because of a measured bug:
    ///
    /// The whole balance solution and all the tests had been done with fast food.
    /// The first time the Turkish cuisine was run, all eight strategies went
    /// under with ZERO customers. The cause was visible nowhere in the code: dish
    /// groups are per-cuisine (docs/13) but the simulation had HARD-CODED fast
    /// food's vocabulary ("ana", "yan", "icecek"). In a Turkish restaurant the
    /// groups are sulu, corba, pilav, izgara, meze. No customer could find a main
    /// course, and they all turned away at the door.
    ///
    /// The tests here verify that every cuisine is playable. A new cuisine must
    /// be added to the list.
    /// </summary>
    public class CuisineTests
    {
        private readonly ITestOutputHelper _out;
        public CuisineTests(ITestOutputHelper output) { _out = output; }

        private const ulong Seed = 20260910UL;

        public static IEnumerable<object[]> Cuisines()
        {
            yield return new object[] { "fastfood" };
            yield return new object[] { "turk" };
        }

        private static EconomyConfig Economy() => ContentLoader.LoadEconomy(Paths.Content);

        private static TimingConfig Timing(ContentSet c)
        {
            return c.SlotDurationsBp != null
                ? TimingConfig.Default().WithSlotDurations(c.SlotDurationsBp).WithEatMs(c.EatMs)
                : TimingConfig.Default();
        }

        // ====================================================================
        [Theory]
        [MemberData(nameof(Cuisines))]
        public void Every_group_falls_into_exactly_one_role(string cuisine)
        {
            ContentSet c = ContentSetLoader.Load(Paths.Content, cuisine);

            HashSet<string> mapped = new HashSet<string>();
            foreach (string[] role in new[] { c.MainGroups, c.SideGroups,
                                              c.DrinkGroups, c.DessertGroups })
                foreach (string g in role)
                    Assert.True(mapped.Add(g), cuisine + ": '" + g + "' falls into two roles at once");

            HashSet<string> used = new HashSet<string>();
            foreach (DishDef d in c.Dishes) used.Add(d.Group);

            foreach (string g in used)
                Assert.True(mapped.Contains(g), cuisine + ": the '" + g + "' group has no role");

            _out.WriteLine($"{cuisine}: main [{string.Join(", ", c.MainGroups)}] " +
                           $"side [{string.Join(", ", c.SideGroups)}] " +
                           $"drink [{string.Join(", ", c.DrinkGroups)}]");
        }

        [Theory]
        [MemberData(nameof(Cuisines))]
        public void An_order_can_be_placed_on_the_first_day(string cuisine)
        {
            // This is what actually broke: if there is no main course open on day
            // one, every customer turns away at the door and the economy quietly
            // stops.
            ContentSet c = ContentSetLoader.Load(Paths.Content, cuisine);

            int mains = 0;
            foreach (DishDef d in c.Dishes)
                if (d.UnlockDay <= 1 && c.IsInRole(d.Group, c.MainGroups)) mains++;

            _out.WriteLine($"{cuisine}: {mains} main courses open on day one");
            Assert.True(mains > 0, cuisine + ": no main course is open on day one");
        }

        [Theory]
        [MemberData(nameof(Cuisines))]
        public void One_service_day_serves_customers(string cuisine)
        {
            // End to end: is the cuisine really playable. The numeric target is
            // kept low; what is measured is not the balance but DOES IT RUN.
            ContentSet c = ContentSetLoader.Load(Paths.Content, cuisine);
            Simulation sim = new Simulation(Economy(), c, Timing(c), Seed);

            sim.Apply(new Command(0, CommandKind.Hire, 0));
            sim.Apply(new Command(0, CommandKind.Hire, 1));

            for (int i = 0; i < sim.IngredientCount; i++)
            {
                int need = sim.RecommendedRestock(i);
                if (need > 0)
                    sim.Apply(new Command(sim.TickIndex, CommandKind.OrderIngredient, i, need));
            }

            sim.Apply(new Command(sim.TickIndex, CommandKind.OpenService));
            TimingConfig t = Timing(c);
            for (int i = 0; i < t.ServiceTicks + 4000; i++)
            {
                sim.Tick();
                if (sim.ServiceComplete) break;
            }
            sim.Apply(new Command(sim.TickIndex, CommandKind.CloseDay));
            DayReport r = sim.BuildDayReport();

            _out.WriteLine($"{cuisine}: {r.ServedParties}/{r.PlannedParties} parties, " +
                           $"{r.TurnedAwayParties} at the door, revenue {r.Revenue / 100}");

            Assert.True(r.ServedParties > 0, cuisine + ": not a single party was served");
            Assert.True(r.TurnedAwayParties < r.PlannedParties,
                cuisine + ": everyone turned away at the door");
            Assert.True(r.Revenue > 0, cuisine + ": revenue is zero");
        }

        [Theory]
        [MemberData(nameof(Cuisines))]
        public void An_order_is_placed_from_every_role(string cuisine)
        {
            // DESSERT WAS DEAD CONTENT. There were 6 dessert dishes in fast food
            // and 3 in the Turkish restaurant, and none of them could be ordered;
            // PickOrder chose a main, a side and a drink and never looked at
            // dessert. The docs/27 3.3 peak table gives dessert 0.08 concurrent
            // plates and the equipment ladder has an upgrade for the dessert
            // station: both were running for nothing.
            //
            // This test verifies that all four roles really do get ordered.
            ContentSet c = ContentSetLoader.Load(Paths.Content, cuisine);
            Simulation sim = new Simulation(Economy(), c, Timing(c), Seed);

            sim.Apply(new Command(0, CommandKind.Hire, 0));
            sim.Apply(new Command(0, CommandKind.Hire, 1));

            int[] byRole = new int[4];
            for (int day = 0; day < 5; day++)
            {
                for (int i = 0; i < sim.IngredientCount; i++)
                {
                    int need = sim.RecommendedRestock(i);
                    if (need > 0)
                        sim.Apply(new Command(sim.TickIndex, CommandKind.OrderIngredient, i, need));
                }

                sim.Apply(new Command(sim.TickIndex, CommandKind.OpenService));
                TimingConfig t = Timing(c);
                for (int i = 0; i < t.ServiceTicks + 4000; i++)
                {
                    sim.Tick();
                    if (sim.ServiceComplete) break;
                }
                sim.Apply(new Command(sim.TickIndex, CommandKind.CloseDay));

                // The counter is reset at the start of the day, so it is summed
                // once the day closes.
                for (int r = 0; r < 4; r++) byRole[r] += sim.OrderedInRole(r);
                sim.AdvanceToNextDay();
            }

            _out.WriteLine($"{cuisine}: main {byRole[0]}, side {byRole[1]}, " +
                           $"drink {byRole[2]}, dessert {byRole[3]}");

            Assert.True(byRole[0] > 0, cuisine + ": no main course was ever ordered");
            Assert.True(byRole[3] > 0, cuisine + ": no DESSERT was ever ordered");
        }

        [Theory]
        [MemberData(nameof(Cuisines))]
        public void Named_equipment_stays_locked_until_it_is_bought(string cuisine)
        {
            // docs/09: 10 SPECIAL cooking stations per cuisine. What sets them
            // apart from the six shared stations is that they ARE NOT THERE at
            // the start.
            //
            // "Station tier 2 required" is an abstract condition; "buy a stone
            // oven and borek opens up" is a readable one. Same mechanic, readable
            // name.
            ContentSet c = ContentSetLoader.Load(Paths.Content, cuisine);

            const int Shared = 6;
            if (c.Stations.Length <= Shared) return;    // this cuisine has no special stations

            Simulation sim = new Simulation(Economy(), c, Timing(c), Seed);
            sim.Apply(new Command(0, CommandKind.TakeLoan, 2));

            for (int st = Shared; st < c.Stations.Length; st++)
            {
                // The dishes tied to this equipment
                var locked = new List<int>();
                for (int i = 0; i < c.Dishes.Length; i++)
                    if (c.Dishes[i].StationIndex == st && c.Dishes[i].RequiresStationTier > 0)
                        locked.Add(i);

                _out.WriteLine($"{cuisine}: {c.Stations[st].Id} -> {locked.Count} dishes");
                Assert.True(locked.Count > 0,
                    c.Stations[st].Id + ": no dish is tied to it, the equipment sits idle");

                // It must NOT be owned at the start
                Assert.Equal(0, sim.StationTier(st));
                Assert.True(sim.NextEquipmentPrice(st) > 0,
                    c.Stations[st].Id + ": it is free, so it need not be bought at all");

                // Even once its day and reputation arrive it cannot be made without the equipment
                foreach (int dish in locked)
                    Assert.False(sim.IsUnlocked(dish),
                        c.Dishes[dish].Id + ": appears unlocked without the equipment");

                sim.Apply(new Command(sim.TickIndex, CommandKind.BuyEquipment, st));
                Assert.Equal(1, sim.StationTier(st));
            }
        }

        [Theory]
        [MemberData(nameof(Cuisines))]
        public void A_price_sensitive_customer_orders_something_cheaper(string cuisine)
        {
            // docs/13 had designed an orderPreference per archetype, and the
            // simulation made no customer behave differently from any other:
            // everyone ordered from the same distribution.
            //
            // The twenty-four weight table was NOT written by hand; the
            // preference was derived from the price sensitivity that was ALREADY
            // LOADED. This test measures that the derivation really works.
            ContentSet c = ContentSetLoader.Load(Paths.Content, cuisine);

            int stingy = -1, generous = -1;
            for (int i = 0; i < c.Archetypes.Length; i++)
            {
                if (stingy < 0
                    || c.Archetypes[i].PriceSensitivityBp > c.Archetypes[stingy].PriceSensitivityBp)
                    stingy = i;
                if (generous < 0
                    || c.Archetypes[i].PriceSensitivityBp < c.Archetypes[generous].PriceSensitivityBp)
                    generous = i;
            }

            Simulation sim = new Simulation(Economy(), c, Timing(c), Seed);
            sim.Apply(new Command(0, CommandKind.TakeLoan, 2));
            for (int i = 0; i < sim.IngredientCount; i++)
                sim.Apply(new Command(sim.TickIndex, CommandKind.OrderIngredient, i, 30_000));

            // We make it choose from the same role many times; the difference is
            // in the distribution, not in a single draw.
            long sumStingy = 0, sumGenerous = 0;
            const int N = 400;
            for (int k = 0; k < N; k++)
            {
                int a1 = sim.WouldPick(stingy, 0);
                int a2 = sim.WouldPick(generous, 0);
                if (a1 >= 0) sumStingy += c.Dishes[a1].Price;
                if (a2 >= 0) sumGenerous += c.Dishes[a2].Price;
            }

            _out.WriteLine($"{cuisine}: {c.Archetypes[stingy].Id} " +
                           $"(sensitivity {c.Archetypes[stingy].PriceSensitivityBp}) " +
                           $"avg {sumStingy / N / 100.0:0.0}");
            _out.WriteLine($"{cuisine}: {c.Archetypes[generous].Id} " +
                           $"(sensitivity {c.Archetypes[generous].PriceSensitivityBp}) " +
                           $"avg {sumGenerous / N / 100.0:0.0}");

            Assert.True(sumStingy < sumGenerous,
                cuisine + ": the price-sensitive customer does not order anything cheaper");
        }

        [Theory]
        [MemberData(nameof(Cuisines))]
        public void The_slot_durations_divide_exactly_into_ticks(string cuisine)
        {
            // docs/28 Decision G: slot DURATIONS vary with the cuisine. The
            // Turkish restaurant's lunch slot is 48% of the day. If the division
            // is not exact the day's length shifts from cuisine to cuisine and
            // the comparison becomes meaningless.
            ContentSet c = ContentSetLoader.Load(Paths.Content, cuisine);
            if (c.SlotDurationsBp == null) return;

            int sum = 0;
            foreach (int bp in c.SlotDurationsBp) sum += bp;
            Assert.Equal(Fx.One, sum);

            TimingConfig t = Timing(c);
            int ticks = 0;
            for (int i = 0; i < c.SlotDurationsBp.Length; i++) ticks += t.SlotTicks(i);
            Assert.Equal(t.ServiceTicks, ticks);

            _out.WriteLine($"{cuisine}: slot ticks " +
                           $"{t.SlotTicks(0)}/{t.SlotTicks(1)}/{t.SlotTicks(2)}/{t.SlotTicks(3)}");
        }
    }
}
