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
    /// Kirayi SIMULASYONDAN arar.
    ///
    /// Neden gerekti: kira ve genisleme bedelleri `tools/balance/solve.py`
    /// tarafindan KAPALI FORM haftalik modelden cozulmustu. O model haftada
    /// 5.200 sikke ciro varsayiyor; simulasyon 2.364 uretiyor. Aradaki fark
    /// butun stratejileri batiriyor, cunku kira tek basina cironun %82'si
    /// oluyor.
    ///
    /// Hangi modelin dogru oldugu ayri bir soru. Ama oyunun oynadigi model
    /// simulasyon, o yuzden kira ondan turemeli.
    ///
    /// Calistirma: dotnet run --project src/Lokanta.Harness -- --solve
    /// </summary>
    public static class RentSolver
    {
        public static void Run(EconomyConfig baseEconomy, ContentSet content,
                               TimingConfig timing, int seeds, int days)
        {
            Console.WriteLine("=== Kira arayicisi ===");
            Console.WriteLine("Hedef: makul oyuncu artida bitsin, pasif oyuncu batsin.");
            Console.WriteLine();
            Console.WriteLine("| talep | kira | genisleme | makul kasa | makul borc | pasif kasa | pasif borc | sonuc |");
            Console.WriteLine("|------:|-----:|----------:|-----------:|-----------:|-----------:|-----------:|-------|");

            int bestRent = 0, bestUpgrade = 0, bestDemand = 0;
            double bestScore = double.MaxValue;

            // Talep de aranıyor. Hipotez: masa basina gunde 4 kisi gercek bir
            // lokanta icin cok dusuk. Dort masada 13 musteri, masa basina
            // gunde 3 devir demek; gercek lokanta iki serviste 4-6 devir yapar.
            // Kira tek basina aciyi kapatmiyor, o yuzden talep de degisken.
            int[] rentScales = { 5000, 7500, 10000 };
            int[] upgradeScales = { 6000, 10000 };
            int[] demandBases = { 4, 6, 8, 10, 12 };

            foreach (int demand in demandBases)
            foreach (int rent in rentScales)
            {
                foreach (int up in upgradeScales)
                {
                    EconomyConfig eco = Scale(baseEconomy, rent, up, demand);

                    Outcome makul = Play(eco, content, timing, seeds, days, "makul");
                    Outcome pasif = Play(eco, content, timing, seeds, days, "pasif");

                    bool ok = makul.AvgCash > 0 && makul.DebtShare < 0.35
                              && pasif.DebtShare > 0.5;

                    // Puan: makul oyuncu baslangicin 2-3 katiyla bitsin.
                    double target = baseEconomy.StartingCash * 2.5;
                    double score = Math.Abs(makul.AvgCash - target) / target
                                   + makul.DebtShare
                                   + (1.0 - pasif.DebtShare);

                    Console.WriteLine(
                        $"| {demand,2} | {rent / 100.0,4:0.00} | {up / 100.0,9:0.00} | " +
                        $"{makul.AvgCash / 100.0,10:N0} | {makul.DebtShare,10:P0} | " +
                        $"{pasif.AvgCash / 100.0,10:N0} | {pasif.DebtShare,10:P0} | " +
                        $"{(ok ? "TAMAM" : "-"),-5} |");

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
                Console.WriteLine("Hicbir olcek her iki kosulu birden saglamadi.");
                Console.WriteLine("Kira tek basina yetmiyor demektir; talep, fis ya da");
                Console.WriteLine("kapasite tarafinda da degisiklik gerekiyor.");
                return;
            }

            Console.WriteLine($"EN IYI: masa basina {bestDemand} kisi, " +
                              $"kira olcegi {bestRent / 100.0:0.00}, " +
                              $"genisleme olcegi {bestUpgrade / 100.0:0.00}");
            Console.WriteLine();
            Console.WriteLine("Onerilen kiralar (santi-sikke):");
            for (int i = 0; i < baseEconomy.TierCount; i++)
            {
                TierConfig t = baseEconomy.TierAt(i);
                Console.WriteLine(
                    $"  {t.Tables,2} masa  kira {Fx.Bp(t.Rent, bestRent),8:N0}  " +
                    $"genisleme {Fx.Bp(t.Upgrade, bestUpgrade),8:N0}");
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
                e.SalonWorkPerCustomerMicro, e.SalonWageNumerator,
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
