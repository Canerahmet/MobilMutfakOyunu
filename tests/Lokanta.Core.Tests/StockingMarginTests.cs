using Lokanta.Content;
using Lokanta.Core;
using Lokanta.Core.Content;
using Lokanta.Core.Economy;
using Lokanta.Core.Sim;
using Xunit;
using Xunit.Abstractions;

namespace Lokanta.Core.Tests
{
    /// <summary>
    /// THE SAFETY MARGIN GOES ON THE FORECAST, NOT ON THE FLOOR.
    ///
    /// The market recommendation holds a floor of a full party's portions for
    /// every dish on the menu, and adds a 20% margin for demand that
    /// fluctuates around the forecast. The floor is not a forecast - it is
    /// already a buffer - and the margin used to be applied to whichever of
    /// the two was larger. For a dish that sells one portion a day that meant
    /// 4.8 portions of every perishable bought every morning, and at four
    /// tables without a cold store all of it died that night. Measured on the
    /// Turkish non-expander over sixty days, 8 seeds: with the menu narrowed
    /// the way a lokanta narrows it, this line alone took earnings from 1,954
    /// to 3,628 and spoilage from 8,452 to 6,541 (docs/63 12).
    ///
    /// So: with one low-selling dish on the menu, the recommended stock of
    /// each of its ingredients is EXACTLY the floor. No margin on a buffer.
    /// Proved red by restoring the old line - every ingredient then comes
    /// out at 120% of the floor.
    /// </summary>
    public sealed class StockingMarginTests
    {
        private readonly ITestOutputHelper _out;
        public StockingMarginTests(ITestOutputHelper o) { _out = o; }

        private const ulong Seed = 20260919UL;

        [Theory]
        [InlineData("fastfood")]
        [InlineData("turk")]
        public void A_low_selling_dish_is_stocked_to_the_floor_and_not_beyond(string cuisine)
        {
            EconomyConfig eco = ContentLoader.LoadEconomy(Paths.Content);
            ContentSet content = ContentSetLoader.Load(Paths.Content, cuisine);
            TimingConfig timing = content.SlotDurationsBp != null
                ? TimingConfig.Default().WithSlotDurations(content.SlotDurationsBp)
                                        .WithEatMs(content.EatMs)
                : TimingConfig.Default();
            Simulation sim = new Simulation(eco, content, timing, Seed);

            // One unlocked dessert, alone on the menu. Desserts are ordered by
            // one guest in ten, so on an opening day the forecast is far below
            // the floor and the floor is what the recommendation returns.
            int dessert = -1;
            for (int i = 0; i < sim.DishCount; i++)
                if (sim.IsUnlocked(i) && sim.DishRole(i) == 3) { dessert = i; break; }
            Assert.True(dessert >= 0, "no unlocked dessert to put on the menu");
            for (int i = 0; i < sim.DishCount; i++)
                sim.Apply(new Command(sim.TickIndex, CommandKind.SetMenuSlot, i, i == dessert ? 1 : 0));

            long people = sim.ExpectedPeopleToday();
            long expectedPortionsBp = people * sim.RoleChanceBp(3);   // portions x 10000
            _out.WriteLine($"{cuisine}: {people} people expected, dessert chance {sim.RoleChanceBp(3)} bp, "
                           + $"forecast {expectedPortionsBp / 10000.0:0.00} portions against a floor of {Simulation.MinPartyBuffer}");
            Assert.True(expectedPortionsBp * 12 < (long)Simulation.MinPartyBuffer * 10000 * 10,
                "the forecast with its margin is not below the floor, so this menu cannot test the floor");

            DishDef d = content.Dishes[dessert];
            int checkedIngredients = 0;
            for (int k = 0; k < d.Ingredients.Length; k++)
            {
                int idx = d.Ingredients[k].IngredientIndex;
                int grams = d.Ingredients[k].Grams;
                int need = sim.DailyNeed(idx);
                int floor = grams * Simulation.MinPartyBuffer;
                _out.WriteLine($"  {content.Ingredients[idx].Id}: need {need} g, floor {floor} g");
                Assert.True(need == floor,
                    $"{content.Ingredients[idx].Id}: the recommendation is {need} g for a floor of {floor} g - "
                    + "a margin is being applied to the buffer");
                checkedIngredients++;
            }
            Assert.True(checkedIngredients > 0, "the dessert has no ingredients to check");
        }
    }
}
