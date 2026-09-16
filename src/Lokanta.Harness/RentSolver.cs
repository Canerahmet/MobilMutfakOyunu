using System;
using System.Collections.Generic;
using System.Globalization;
using Lokanta.Core;
using Lokanta.Core.Content;
using Lokanta.Core.Economy;
using Lokanta.Core.Sim;

namespace Lokanta.Harness
{
    /// <summary>
    /// Searches for the rent FROM THE SIMULATION.
    ///
    /// Why it was needed: the rent and expansion costs had been solved by
    /// `tools/balance/solve.py` from the CLOSED-FORM weekly model. That model
    /// assumes 5,200 coins of revenue a week; the simulation produces 2,364.
    /// The difference bankrupts every strategy, because the rent alone comes to
    /// 82% of the revenue.
    ///
    /// Which model is right is a separate question. But the model the game
    /// plays is the simulation, so the rent has to be derived from it.
    ///
    /// Running it: dotnet run --project src/Lokanta.Harness -- --solve
    /// </summary>
    public static class RentSolver
    {
        public static void Run(EconomyConfig baseEconomy, ContentSet content,
                               TimingConfig timing, int seeds, int days)
        {
            Console.WriteLine("=== Rent search ===");
            Console.WriteLine("Target: the reasonable player ends in the black, the passive player goes under.");
            Console.WriteLine();
            Console.WriteLine("| demand | rent | expansion | reas. cash | reas. debt | pass. cash | pass. debt | verdict |");
            Console.WriteLine("|-------:|-----:|----------:|-----------:|-----------:|-----------:|-----------:|---------|");

            int bestRent = 0, bestUpgrade = 0, bestDemand = 0;
            double bestScore = double.MaxValue;

            // The demand is searched as well. Hypothesis: 4 people per table
            // per day is far too low for a real restaurant. 13 customers at
            // four tables means 3 turns per table per day; a real restaurant
            // does 4-6 turns across two services. The rent alone does not close
            // the gap, so the demand is a variable too.
            int[] rentScales = { 5000, 7500, 10000 };
            int[] upgradeScales = { 6000, 10000 };
            int[] demandBases = { 4, 6, 8, 10, 12 };

            foreach (int demand in demandBases)
            foreach (int rent in rentScales)
            {
                foreach (int up in upgradeScales)
                {
                    EconomyConfig eco = Scale(baseEconomy, rent, up, demand);

                    Outcome reasonable = Play(eco, content, timing, seeds, days, "makul");
                    Outcome passive = Play(eco, content, timing, seeds, days, "pasif");

                    bool ok = reasonable.AvgCash > 0 && reasonable.DebtShare < 0.35
                              && passive.DebtShare > 0.5;

                    // Score: the reasonable player should end on 2-3 times the start.
                    double target = baseEconomy.StartingCash * 2.5;
                    double score = Math.Abs(reasonable.AvgCash - target) / target
                                   + reasonable.DebtShare
                                   + (1.0 - passive.DebtShare);

                    Console.WriteLine(
                        $"| {demand,2} | {rent / 100.0,4:0.00} | {up / 100.0,9:0.00} | " +
                        $"{reasonable.AvgCash / 100.0,10:N0} | {reasonable.DebtShare,10:P0} | " +
                        $"{passive.AvgCash / 100.0,10:N0} | {passive.DebtShare,10:P0} | " +
                        $"{(ok ? "OK" : "-"),-7} |");

                    if (ok && score < bestScore)
                    {
                        bestScore = score;
                        bestRent = rent;
                        bestUpgrade = up;
                        bestDemand = demand;
                    }
                }
            }

            Console.WriteLine();
            if (bestRent == 0)
            {
                Console.WriteLine("No scale satisfied both conditions at once.");
                Console.WriteLine("That means the rent alone is not enough; the demand, the");
                Console.WriteLine("ticket or the capacity has to change as well.");
                return;
            }

            Console.WriteLine($"BEST: {bestDemand} people per table, " +
                              $"rent scale {bestRent / 100.0:0.00}, " +
                              $"expansion scale {bestUpgrade / 100.0:0.00}");
            Console.WriteLine();
            Console.WriteLine("Suggested rents (centi-coins):");
            for (int i = 0; i < baseEconomy.TierCount; i++)
            {
                TierConfig t = baseEconomy.TierAt(i);
                Console.WriteLine(
                    $"  {t.Tables,2} tables  rent {Fx.Bp(t.Rent, bestRent),8:N0}  " +
                    $"expansion {Fx.Bp(t.Upgrade, bestUpgrade),8:N0}");
            }
        }

        private static EconomyConfig Scale(EconomyConfig e, int rentBp, int upgradeBp,
                                           int demandBase)
        {
            TierConfig[] tiers = new TierConfig[e.TierCount];
            for (int i = 0; i < e.TierCount; i++)
            {
                TierConfig t = e.TierAt(i);
                tiers[i] = new TierConfig(t.Tables, Fx.Bp(t.Rent, rentBp),
                                          Fx.Bp(t.Upgrade, upgradeBp), t.StaffCap);
            }

            return new EconomyConfig(
                e.StartingCash, e.StartingReputationCenti, e.CampaignDays,
                e.WeekendDaysPerWeek, demandBase,
                e.WeekdayMultiplierBp, e.WeekendMultiplierBp, e.IngredientRateBp,
                e.CookCapacityPerDay, e.CookDailyWage,
                e.HallWorkPerCustomerMicro, e.HallWageNumerator,
                e.OwnerWorkMicro, e.WeeklyXpWageGrowthBp, tiers,
                e.ReputationDecayPerDayCenti, e.SideChanceBp, e.DrinkChanceBp);
        }

        private struct Outcome
        {
            public double AvgCash;
            public double DebtShare;
        }

        private static Outcome Play(EconomyConfig eco, ContentSet content,
                                    TimingConfig timing, int seeds, int days,
                                    string strategyName)
        {
            long cash = 0;
            int debt = 0;

            for (int s = 0; s < seeds; s++)
            {
                ulong seed = 20260910UL + (ulong)s * 7919UL;
                IStrategy strategy = strategyName == "makul"
                    ? (IStrategy)new ReasonablePlayer()
                    : new PassivePlayer();

                Simulation sim = new Simulation(eco, content, timing, seed);
                int limit = timing.ServiceTicks + 6000;

                for (int day = 1; day <= days; day++)
                {
                    strategy.OnMorning(sim);
                    sim.Apply(new Command(sim.TickIndex, CommandKind.OpenService));
                    for (int t = 0; t < limit; t++)
                    {
                        sim.Tick();
                        if (sim.ServiceComplete) break;
                    }
                    sim.Apply(new Command(sim.TickIndex, CommandKind.CloseDay));
                    strategy.OnEvening(sim, sim.BuildDayReport());
                    sim.AdvanceToNextDay();
                }

                cash += sim.Cash;
                if (sim.FirstDebtDay > 0) debt++;
            }

            return new Outcome
            {
                AvgCash = (double)cash / seeds,
                DebtShare = (double)debt / seeds
            };
        }
    }
}
