using System;
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
    /// Situations that BREAK the game. All of them were found in a QA pass and all
    /// of them come down a road a real player could travel: locking the phone,
    /// playing for a long time, a corrupted save.
    ///
    /// This file is a regression wall. The comment at the head of each test
    /// explains HOW the bug was triggered - because anyone about to undo the fix
    /// should read that sentence first.
    /// </summary>
    public sealed class RobustnessTests
    {
        private readonly ITestOutputHelper _out;
        public RobustnessTests(ITestOutputHelper output) { _out = output; }

        private const ulong Seed = 20260910UL;

        private static EconomyConfig Economy() => ContentLoader.LoadEconomy(Paths.Content);
        private static ContentSet Content(string cuisine = "fastfood")
            => ContentSetLoader.Load(Paths.Content, cuisine);

        private static TimingConfig Timing()
        {
            ContentSet c = Content();
            return c.SlotDurationsBp != null
                ? TimingConfig.Default().WithSlotDurations(c.SlotDurationsBp).WithEatMs(c.EatMs)
                : TimingConfig.Default();
        }

        private static Simulation NewSim(string cuisine = "fastfood")
            => new Simulation(Economy(), Content(cuisine), Timing(), Seed);

        private static string Save(Simulation sim)
        {
            JsonStateWriter w = new JsonStateWriter();
            sim.Write(w);
            return w.ToJson();
        }

        /// <summary>Drains the buffer and counts the DishUnlocked events.</summary>
        private static int CountUnlocks(Simulation sim)
        {
            SimEvent[] buf = new SimEvent[4096];
            int n = sim.Events.Drain(buf);
            int unlocks = 0;
            for (int i = 0; i < n; i++)
                if (buf[i].Kind == SimEventKind.DishUnlocked) unlocks++;
            return unlocks;
        }

        // =====================================================================
        [Fact]
        public void The_wage_rise_is_capped_and_does_not_overflow()
        {
            // In free play the wage grew at 2.2% COMPOUND a week with no ceiling,
            // while the income is bound by the table and reputation ceilings. In
            // the one hundred and fourth week PowNano overflowed a long and the
            // exception was thrown IN THE MIDDLE of CloseDay: the reputation had
            // fallen, the stock had aged, but the phase had not advanced. Every
            // time the player pressed "Close the day" the same damage was applied
            // once more and the day never closed.
            EconomyConfig cfg = Economy();
            Crew crew = new Crew(3, 3);

            long week1 = StaffingModel.WeeklyWageBill(crew, 1, cfg);
            Assert.True(week1 > 0);

            long prev = week1;
            foreach (int week in new[] { 8, 28, 52, 104, 520, 5200 })
            {
                long bill = StaffingModel.WeeklyWageBill(crew, week, cfg);
                _out.WriteLine($"week {week,5}: {bill} ({(double)bill / week1:0.00} times)");

                Assert.True(bill >= prev, "the wage went backwards: week " + week);
                Assert.True(bill <= week1 * 2,
                            "the wage passed twice the start: week " + week + " -> " + bill);
                prev = bill;
            }
        }

        [Fact]
        public void Closing_the_day_does_not_throw_in_a_long_game()
        {
            // The end-to-end counterpart of the one above: it is not just the
            // formula, THE DAY ITSELF has to be able to close. Two hundred days are
            // played.
            Simulation sim = NewSim();
            for (int day = 1; day <= 200; day++)
            {
                sim.Apply(new Command(sim.TickIndex, CommandKind.OpenService));
                sim.Apply(new Command(sim.TickIndex, CommandKind.CloseDay));
                Assert.Equal(DayPhase.Evening, sim.Phase);
                sim.AdvanceToNextDay();
            }
            _out.WriteLine($"after 200 days: till {sim.Cash}, reputation {sim.ReputationCenti}");
            Assert.Equal(201, sim.Day);
        }

        // =====================================================================
        [Fact]
        public void The_within_day_counters_survive_a_save()
        {
            // The evening report LIED after a load: because the wages, the rent and
            // the spoilage were not saved, "today's profit" ignored the week's
            // biggest expense and showed a large surplus.
            //
            // To trigger it: close the rent day, lock the phone, come back.
            Simulation sim = NewSim();
            for (int day = 1; day <= 7; day++)
            {
                sim.Apply(new Command(sim.TickIndex, CommandKind.OpenService));
                for (int t = 0; t < 200; t++) sim.Tick();
                sim.Apply(new Command(sim.TickIndex, CommandKind.CloseDay));
                if (day < 7) sim.AdvanceToNextDay();
            }

            DayReport before = sim.BuildDayReport();
            _out.WriteLine($"before: wages {before.WageCost}, rent {before.RentCost}, "
                           + $"spoilage {before.SpoiledValue}, revenue {sim.TotalRevenue}");

            string json = Save(sim);
            Simulation loaded = NewSim();
            loaded.Restore(new JsonStateReader(json));

            DayReport after = loaded.BuildDayReport();
            _out.WriteLine($"after : wages {after.WageCost}, rent {after.RentCost}, "
                           + $"spoilage {after.SpoiledValue}, revenue {loaded.TotalRevenue}");

            Assert.Equal(before.WageCost, after.WageCost);
            Assert.Equal(before.RentCost, after.RentCost);
            Assert.Equal(before.SpoiledValue, after.SpoiledValue);
            Assert.Equal(before.NetProfit, after.NetProfit);
            Assert.Equal(sim.TotalRevenue, loaded.TotalRevenue);
        }

        [Fact]
        public void A_loaded_game_does_not_announce_old_dishes_again()
        {
            // _dishWasUnlocked was not saved and the constructor filled it with
            // DAY ONE's state. Opening a save from day thirty and moving on, every
            // dish in between was announced as "unlocked" all over again: a
            // notification and a level-up sound for each one.
            Simulation sim = NewSim();
            for (int day = 1; day <= 30; day++)
            {
                sim.Apply(new Command(sim.TickIndex, CommandKind.OpenService));
                sim.Apply(new Command(sim.TickIndex, CommandKind.CloseDay));
                sim.AdvanceToNextDay();
            }

            Simulation loaded = NewSim();
            loaded.Restore(new JsonStateReader(Save(sim)));

            loaded.Apply(new Command(loaded.TickIndex, CommandKind.OpenService));
            loaded.Apply(new Command(loaded.TickIndex, CommandKind.CloseDay));
            CountUnlocks(loaded);          // clear the day's events
            loaded.AdvanceToNextDay();

            int announced = CountUnlocks(loaded);

            _out.WriteLine($"dishes announced on the day after loading: {announced}");
            Assert.True(announced <= 2,
                        "old dishes were announced again after loading: " + announced);
        }

        // =====================================================================
        [Fact]
        public void The_recommended_restock_is_a_SINGLE_command()
        {
            // The interface sent one OrderIngredient per ingredient and the daily
            // command limit is 256: pressing "Buy the recommended stock" five times
            // exhausted the day's budget, after which EVERY command - price, menu,
            // hiring, equipment, expansion, intervention and tab included - was
            // silently rejected.
            //
            // The test says two things at once: the command really does BUY, and it
            // does so taking a single place in the log.
            // ONE DAY IS PLAYED FIRST, because the opening stock is exactly one
            // day's worth: on the first morning RecommendedRestock returns zero for
            // every item and there is nothing to buy. What the test measures is
            // "does the command buy", not "is the stock empty".
            Simulation sim = NewSim();
            sim.Apply(new Command(sim.TickIndex, CommandKind.TakeLoan, 2));
            sim.Apply(new Command(sim.TickIndex, CommandKind.OpenService));
            for (int t = 0; t < 3000 && !sim.ServiceComplete; t++) sim.Tick();
            sim.Apply(new Command(sim.TickIndex, CommandKind.CloseDay));
            sim.AdvanceToNextDay();

            long cashBefore = sim.Cash;
            int stockBefore = 0, stockAfter = 0;
            for (int i = 0; i < sim.IngredientCount; i++) stockBefore += sim.StockOf(i);

            sim.Apply(new Command(sim.TickIndex, CommandKind.OrderRecommended));

            for (int i = 0; i < sim.IngredientCount; i++) stockAfter += sim.StockOf(i);
            _out.WriteLine($"stock {stockBefore} -> {stockAfter}, "
                           + $"till {cashBefore} -> {sim.Cash}");

            Assert.True(stockAfter > stockBefore, "the stock did not increase");
            Assert.True(sim.Cash < cashBefore, "no money was spent");

            // THE REAL MEASURE: the daily command budget. Even pressing it a
            // hundred times must not exhaust the budget - one command takes one
            // place.
            for (int k = 0; k < 100; k++)
                sim.Apply(new Command(sim.TickIndex, CommandKind.OrderRecommended));

            // If the budget is exhausted everything but a phase command is
            // rejected; if a price command still goes through, the budget is
            // intact.
            long priceBefore = sim.DishPrice(0);
            sim.Apply(new Command(sim.TickIndex, CommandKind.SetPrice, 0,
                                  (int)(priceBefore + 100)));
            Assert.NotEqual(priceBefore, sim.DishPrice(0));
        }

        // =====================================================================
        [Theory]
        [InlineData("storageTier", 9)]
        [InlineData("quality", 7)]
        public void A_corrupt_save_is_caught_ON_LOAD(string field, int bad)
        {
            // Validate's own comment says "the error is told TO THE PLAYER, before
            // they get inside the game". These two fields slipped through the net
            // and brought things down INSIDE the game: the cold storage tier on
            // every CloseDay, the quality in the price table.
            //
            // A slot that looks "sound" while the game opens and cannot be played
            // is worse than an outright error.
            Simulation sim = NewSim();
            string json = Save(sim);

            string broken = Replace(json, field, bad);
            Assert.NotEqual(json, broken);

            Simulation target = NewSim();
            Assert.Throws<InvalidOperationException>(
                () => target.Restore(new JsonStateReader(broken)));
        }

        /// <summary>
        /// "field": number -> "field": new value. Because the save is plain JSON,
        /// doing it on the text is enough and it keeps what the test touches
        /// readable.
        /// </summary>
        private static string Replace(string json, string field, int value)
        {
            int i = json.IndexOf("\"" + field + "\"", StringComparison.Ordinal);
            if (i < 0) return json;
            int colon = json.IndexOf(':', i);
            if (colon < 0) return json;
            int end = colon + 1;
            while (end < json.Length && json[end] != ',' && json[end] != '}') end++;
            return json.Substring(0, colon + 1) + value + json.Substring(end);
        }
    }
}
