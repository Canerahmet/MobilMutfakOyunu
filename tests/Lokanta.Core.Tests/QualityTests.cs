using Lokanta.Content;
using Lokanta.Core.Content;
using Lokanta.Core.Economy;
using Lokanta.Core.Save;
using Lokanta.Core.Sim;
using Xunit;
using Xunit.Abstractions;

namespace Lokanta.Core.Tests
{
    /// <summary>
    /// INGREDIENT QUALITY. All 77 ingredients in the content had a three-rung
    /// table (low / standard / high) and the simulation never read it;
    /// tools/audit_content.py found it that way.
    ///
    /// The content carries a design decision here: all SIX of the most sensitive
    /// ingredients are meat. Salt and black pepper are almost insensitive. So the
    /// rule "going cheap is free on the salt and a disaster on the meat" is
    /// written into the data, and even a single global quality setting gives a
    /// different result from dish to dish.
    /// </summary>
    public class QualityTests
    {
        private readonly ITestOutputHelper _out;
        public QualityTests(ITestOutputHelper output) { _out = output; }

        private const ulong Seed = 20260910UL;
        private const int Low = 0, Standard = 1, High = 2;

        private static EconomyConfig Economy() => ContentLoader.LoadEconomy(Paths.Content);
        private static ContentSet Content() => ContentSetLoader.Load(Paths.Content, "fastfood");

        private static TimingConfig Timing(ContentSet c)
        {
            return c.SlotDurationsBp != null
                ? TimingConfig.Default().WithSlotDurations(c.SlotDurationsBp).WithEatMs(c.EatMs)
                : TimingConfig.Default();
        }

        private static Simulation NewSim()
        {
            ContentSet c = Content();
            return new Simulation(Economy(), c, Timing(c), Seed);
        }

        // ====================================================================
        [Fact]
        public void The_quality_table_loads_and_standard_is_the_reference()
        {
            ContentSet c = Content();
            foreach (IngredientDef d in c.Ingredients)
            {
                Assert.NotNull(d.QualityPriceBp);
                Assert.NotNull(d.QualitySatisfactionCenti);
                Assert.Equal(3, d.QualityPriceBp.Length);

                // Standard is the REFERENCE: price multiplier 1.0 and zero effect on satisfaction.
                Assert.Equal(Fx.One, d.QualityPriceBp[Standard]);
                Assert.Equal(0, d.QualitySatisfactionCenti[Standard]);

                // Cheap is cheaper and worse, expensive is dearer and better.
                Assert.True(d.QualityPriceBp[Low] < Fx.One, d.Id);
                Assert.True(d.QualityPriceBp[High] > Fx.One, d.Id);
                Assert.True(d.QualitySatisfactionCenti[Low] < 0, d.Id);
                Assert.True(d.QualitySatisfactionCenti[High] > 0, d.Id);
            }
        }

        [Fact]
        public void The_most_sensitive_ingredients_are_MEAT()
        {
            // This test protects a DESIGN DECISION, not a piece of code.
            // This is why quality can be a single global setting: the ingredients
            // that matter most are the meats, the ones that matter least are the
            // spices. If the distribution were reversed a single setting would
            // become meaningless and a per-ingredient choice would be needed.
            ContentSet c = Content();
            int worst = 0;
            foreach (IngredientDef d in c.Ingredients)
                if (d.QualitySatisfactionCenti[Low] < worst)
                    worst = d.QualitySatisfactionCenti[Low];

            foreach (IngredientDef d in c.Ingredients)
            {
                if (d.QualitySatisfactionCenti[Low] != worst) continue;
                _out.WriteLine($"most sensitive: {d.Id} ({d.QualitySatisfactionCenti[Low]})");
                Assert.True(d.BasePrice >= 4000,
                    d.Id + ": the most sensitive ingredient came out cheap, the distribution is broken");
            }
        }

        [Fact]
        public void Cheap_quality_costs_less()
        {
            ContentSet c = Content();
            int meat = c.IngredientIndexOf("kiyma");
            Assert.True(meat >= 0);

            Simulation cheap = NewSim();
            cheap.Apply(new Command(0, CommandKind.SetQuality, Low));
            long before = cheap.Cash;
            cheap.Apply(new Command(0, CommandKind.OrderIngredient, meat, 10_000));
            long cheapCost = before - cheap.Cash;

            Simulation fancy = NewSim();
            fancy.Apply(new Command(0, CommandKind.SetQuality, High));
            before = fancy.Cash;
            fancy.Apply(new Command(0, CommandKind.OrderIngredient, meat, 10_000));
            long fancyCost = before - fancy.Cash;

            _out.WriteLine($"10 kg of mince: cheap {cheapCost / 100}, dear {fancyCost / 100}");
            Assert.True(cheapCost < fancyCost, "the cheap quality is not cheaper");
        }

        [Fact]
        public void Quality_changes_satisfaction()
        {
            ContentSet c = Content();
            int[] sat = new int[3];

            for (int q = 0; q < 3; q++)
            {
                Simulation sim = NewSim();
                sim.Apply(new Command(0, CommandKind.TakeLoan, 2));
                sim.Apply(new Command(0, CommandKind.SetQuality, q));

                // Bought EXPLICITLY. The simulation starts with an opening
                // stock, and because that stock never went through the purchase
                // path its quality is neutral; RecommendedRestock also sees a full
                // stock and returns zero. In the first version this is exactly why
                // the test measured the same satisfaction at all three qualities.
                for (int i = 0; i < sim.IngredientCount; i++)
                    sim.Apply(new Command(sim.TickIndex, CommandKind.OrderIngredient, i, 30_000));

                sim.Apply(new Command(sim.TickIndex, CommandKind.OpenService));
                TimingConfig t = Timing(c);
                for (int i = 0; i < t.ServiceTicks + 4000; i++)
                {
                    sim.Tick();
                    if (sim.ServiceComplete) break;
                }
                sim.Apply(new Command(sim.TickIndex, CommandKind.CloseDay));
                sat[q] = sim.BuildDayReport().AverageSatisfactionCenti;
            }

            _out.WriteLine($"satisfaction: cheap {sat[0]}, standard {sat[1]}, dear {sat[2]}");
            Assert.True(sat[Low] < sat[Standard], "the cheap ingredient did not lower satisfaction");
            Assert.True(sat[High] > sat[Standard], "the dear ingredient did not raise satisfaction");
        }

        [Fact]
        public void Cheap_meat_cannot_hide_behind_a_crowded_recipe()
        {
            // A REGRESSION TEST. In the first implementation a dish's quality
            // effect was the AVERAGE of its ingredients, and the measurement threw
            // it out: in the Turkish cuisine the player buying cheap ingredients
            // BEAT good play by 35,200 to 26,211. The cause was the average
            // itself; the cheap onion thrown into the pot was hiding the cheap
            // meat.
            //
            // The right rule: whatever the most DECISIVE ingredient says goes.
            // This test protects it: a dish with many ingredients must not be
            // punished less than one with few.
            ContentSet c = Content();

            int few = -1, many = -1;
            for (int i = 0; i < c.Dishes.Length; i++)
            {
                if (!HasSensitiveMeat(c, i)) continue;
                int n = c.Dishes[i].Ingredients.Length;
                if (few < 0 || n < c.Dishes[few].Ingredients.Length) few = i;
                if (many < 0 || n > c.Dishes[many].Ingredients.Length) many = i;
            }
            Assert.True(few >= 0 && many >= 0, "no dish containing sensitive meat was found");
            Assert.True(c.Dishes[many].Ingredients.Length > c.Dishes[few].Ingredients.Length,
                        "no two dishes with different ingredient counts were found");

            Simulation sim = NewSim();
            sim.Apply(new Command(0, CommandKind.TakeLoan, 2));
            sim.Apply(new Command(0, CommandKind.SetQuality, Low));
            for (int i = 0; i < sim.IngredientCount; i++)
                sim.Apply(new Command(sim.TickIndex, CommandKind.OrderIngredient, i, 30_000));

            int qFew = sim.DishQualityCentiOf(few);
            int qMany = sim.DishQualityCentiOf(many);

            _out.WriteLine($"{c.Dishes[few].Id} ({c.Dishes[few].Ingredients.Length} ingredients): {qFew}");
            _out.WriteLine($"{c.Dishes[many].Id} ({c.Dishes[many].Ingredients.Length} ingredients): {qMany}");

            // THE MEASURE IS NOT THE TWO DISHES COMPARED WITH EACH OTHER.
            //
            // It had been written that way, and after a while it started
            // measuring the NOISE rather than the mechanic: the opening stock is
            // now built from the menu, so every ingredient is left holding a
            // different amount of "standard" goods and the cheap goods ordered get
            // blended into it. Both dishes' sensitive meat is exactly -2000 in the
            // content and yet the measured values came out as -1965 and -1939; the
            // 26 centi between them had nothing to do with the dishes' ingredient
            // counts and everything to do with the stock blend of those two
            // ingredients. The test broke saying "the dish with many ingredients
            // is hiding the cheap meat" - when nothing was hiding anything.
            //
            // The claim is this: a dish's quality equals the stock quality of its
            // MOST DECISIVE ingredient. The ingredient count changes nothing.
            // Measuring that for each dish against ITS OWN ingredients throws the
            // blend out of both sides of the equation.
            Assert.Equal(WorstStockQuality(sim, c, few), qFew);
            Assert.Equal(WorstStockQuality(sim, c, many), qMany);
        }

        /// <summary>
        /// The LARGEST stock quality in absolute value among a dish's
        /// ingredients. The test's counterpart to the simulation's rule of
        /// "whatever the most decisive ingredient says goes".
        /// </summary>
        private static int WorstStockQuality(Simulation sim, ContentSet c, int dish)
        {
            int worst = 0;
            foreach (DishIngredient p in c.Dishes[dish].Ingredients)
            {
                int q = sim.StockQualityOf(p.IngredientIndex);
                if (System.Math.Abs(q) > System.Math.Abs(worst)) worst = q;
            }
            return worst;
        }

        private static bool HasSensitiveMeat(ContentSet c, int dish)
        {
            foreach (DishIngredient p in c.Dishes[dish].Ingredients)
                if (c.Ingredients[p.IngredientIndex].QualitySatisfactionCenti[Low] <= -2000)
                    return true;
            return false;
        }

        [Fact]
        public void The_stock_quality_is_a_weighted_average()
        {
            // Someone who buys cheap and then buys dear cannot shed the cheap
            // goods in their hands straight away. Otherwise buying a single gram
            // of the dear stuff would clear the whole stock.
            ContentSet c = Content();
            int meat = c.IngredientIndexOf("kiyma");

            Simulation sim = NewSim();
            sim.Apply(new Command(0, CommandKind.TakeLoan, 2));
            sim.Apply(new Command(0, CommandKind.SetQuality, Low));
            sim.Apply(new Command(0, CommandKind.OrderIngredient, meat, 20_000));
            int afterCheap = sim.StockQualityOf(meat);

            sim.Apply(new Command(0, CommandKind.SetQuality, High));
            sim.Apply(new Command(0, CommandKind.OrderIngredient, meat, 1_000));
            int afterTiny = sim.StockQualityOf(meat);

            _out.WriteLine($"20 kg cheap then 1 kg dear: {afterCheap} -> {afterTiny}");
            Assert.True(afterTiny < 0, "one kilo of the dear ingredient cleared the whole stock");
        }

        [Fact]
        public void The_market_prices_swing_every_day()
        {
            // docs/12 3: "there is no advantage to buying early, the stock goes
            // off. Catching a cheap day is not luck, it is a matter of WATCHING."
            //
            // priceVolatilityBp was written in the content and was not read: the
            // market gave the same price every day, so there was nothing to
            // watch.
            ContentSet c = Content();
            Simulation sim = NewSim();
            int meat = c.IngredientIndexOf("kiyma");

            var seen = new System.Collections.Generic.List<long>();
            for (int d = 0; d < 10; d++)
            {
                seen.Add(sim.IngredientPriceToday(meat));
                sim.Apply(new Command(sim.TickIndex, CommandKind.OpenService));
                sim.Apply(new Command(sim.TickIndex, CommandKind.CloseDay));
                sim.AdvanceToNextDay();
            }

            long lo = long.MaxValue, hi = 0;
            foreach (long v in seen) { if (v < lo) lo = v; if (v > hi) hi = v; }
            _out.WriteLine($"mince over ten days {lo / 100}-{hi / 100} coins");

            Assert.True(hi > lo, "the market price never swings, there is nothing to watch");

            // The same seed must give the same prices: the swing is random but
            // DETERMINISTIC, otherwise replay breaks.
            Simulation twin = NewSim();
            Assert.Equal(seen[0], twin.IngredientPriceToday(meat));
        }

        [Fact]
        public void The_save_carries_the_quality()
        {
            ContentSet c = Content();
            Simulation sim = NewSim();
            sim.Apply(new Command(0, CommandKind.SetQuality, High));
            sim.Apply(new Command(0, CommandKind.TakeLoan, 1));
            sim.Apply(new Command(0, CommandKind.OrderIngredient,
                                  c.IngredientIndexOf("kiyma"), 5_000));

            JsonStateWriter w = new JsonStateWriter();
            sim.Write(w);
            Simulation restored = NewSim();
            restored.Restore(new JsonStateReader(w.ToJson()));

            Assert.Equal(sim.Quality, restored.Quality);
            Assert.Equal(sim.StateHash(), restored.StateHash());
        }
    }
}
