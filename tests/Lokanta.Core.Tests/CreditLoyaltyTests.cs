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
    /// THE TAB'S LOYALTY BRINGS CUSTOMERS, NOT STOCK.
    ///
    /// Every settled account adds to a demand bonus: "the customer who comes
    /// back". Until 19 September that bonus was applied inside the market
    /// recommendation alone, after the demand gate had returned - so the
    /// neighbourhood's trust raised the number of people the player BOUGHT
    /// FOR and not the number who ARRIVED. Measured in Turkish, 8 seeds: the
    /// bot that runs tabs served fewer people than the one that does not
    /// (1,829 against 1,846) and spoiled 2,700 coins more. The gate's own
    /// comment had warned about "recommending stock for customers who would
    /// not actually arrive".
    ///
    /// The invariant asserted here needs no number: once loyalty is above
    /// zero, the stock bought for a single main dish alone on the menu must
    /// be built from the SAME people the arrival forecast reports. Under the
    /// old code the two disagree by exactly the loyalty bonus, which is how
    /// it was proved red.
    /// </summary>
    public sealed class CreditLoyaltyTests
    {
        private readonly ITestOutputHelper _out;
        public CreditLoyaltyTests(ITestOutputHelper o) { _out = o; }

        private const ulong Seed = 20260919UL;

        private static EconomyConfig Economy() => ContentLoader.LoadEconomy(Paths.Content);
        private static ContentSet Content() => ContentSetLoader.Load(Paths.Content, "turk");
        private static TimingConfig Timing(ContentSet c) =>
            c.SlotDurationsBp != null
                ? TimingConfig.Default().WithSlotDurations(c.SlotDurationsBp).WithEatMs(c.EatMs)
                : TimingConfig.Default();

        private static void Restock(Simulation sim)
        {
            for (int i = 0; i < sim.IngredientCount; i++)
            {
                int need = sim.RecommendedRestock(i);
                if (need > 0)
                    sim.Apply(new Command(sim.TickIndex, CommandKind.OrderIngredient, i, need));
            }
        }

        /// <summary>Runs a day, opening a tab for every party that qualifies.</summary>
        private static void RunDayGrantingCredit(Simulation sim, ContentSet c)
        {
            Restock(sim);
            sim.Apply(new Command(sim.TickIndex, CommandKind.OpenService));
            int limit = Timing(c).ServiceTicks + 4000;
            for (int t = 0; t < limit; t++)
            {
                sim.Tick();
                for (int p = 0; p < Simulation.MaxParties; p++)
                    if (sim.CreditEligible(p))
                        sim.Apply(new Command(sim.TickIndex, CommandKind.ExtendCredit, p));
                if (sim.ServiceComplete) break;
            }
            sim.Apply(new Command(sim.TickIndex, CommandKind.CloseDay));
            sim.AdvanceToNextDay();
        }

        [Fact]
        public void Loyalty_from_settled_tabs_raises_the_arrivals_the_stock_is_bought_for()
        {
            ContentSet c = Content();
            Simulation sim = new Simulation(Economy(), c, Timing(c), Seed);

            // The tab arrives with the second season; run until it is there,
            // then grant credit for long enough that some accounts fall due
            // and settle. Sixty days is the whole campaign, so stop early
            // once loyalty is on the books.
            // ONE WHOLE PERSON OF LOYALTY, NOT ONE SETTLED TAB. A settled tab
            // is worth 60 bp, and on a forecast of twenty-odd people 0.6% is
            // swallowed by the integer: the first version of this test
            // stopped at the first settlement, and its red proof PASSED,
            // because misplacing a bonus that rounds to nothing changes
            // nothing. The bonus only acts once it has accumulated to a
            // person - which, over a campaign, it does (cap 15%).
            int days = 0;
            while (days < 58 && (long)sim.CreditLoyaltyBp * sim.ExpectedPeopleToday() < 10000L)
            {
                RunDayGrantingCredit(sim, c);
                days++;
            }
            _out.WriteLine($"after {days} days: loyalty {sim.CreditLoyaltyBp} bp on a forecast of "
                           + $"{sim.ExpectedPeopleToday()} people");
            Assert.True((long)sim.CreditLoyaltyBp * sim.ExpectedPeopleToday() >= 10000L,
                "loyalty never reached a whole person in " + days + " days, so nothing can be compared");

            // One main dish alone on the menu: a main is ordered by everybody,
            // so its stock is (people x grams) + 20%, with no floor in the way
            // as long as the forecast exceeds a party of four.
            int main = -1;
            for (int i = 0; i < sim.DishCount; i++)
                if (sim.IsUnlocked(i) && sim.DishRole(i) == 0) { main = i; break; }
            Assert.True(main >= 0, "no unlocked main dish");
            for (int i = 0; i < sim.DishCount; i++)
                sim.Apply(new Command(sim.TickIndex, CommandKind.SetMenuSlot, i, i == main ? 1 : 0));

            int people = sim.ExpectedPeopleToday();
            Assert.True(people > Simulation.MinPartyBuffer, "the forecast is under the floor; nothing to compare");

            DishDef d = c.Dishes[main];
            for (int k = 0; k < d.Ingredients.Length; k++)
            {
                int idx = d.Ingredients[k].IngredientIndex;
                int grams = d.Ingredients[k].Grams;
                int need = sim.DailyNeed(idx);
                long fromArrivals = Fx.Bp((long)people * grams, 12000);
                _out.WriteLine($"  {c.Ingredients[idx].Id}: need {need} g, from the arrival forecast {fromArrivals} g");
                Assert.True(need == fromArrivals,
                    $"{c.Ingredients[idx].Id}: the stock is bought for {need} g but the arrival forecast "
                    + $"gives {fromArrivals} g - the tab's loyalty is reaching the market and not the door");
            }
        }
    }
}
