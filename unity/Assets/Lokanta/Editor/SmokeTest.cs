using System.Text;
using Lokanta.Content;
using Lokanta.Core.Content;
using Lokanta.Core.Economy;
using Lokanta.Core.Sim;
using Lokanta.Game;
using UnityEditor;
using UnityEngine;

namespace Lokanta.EditorTools
{
    /// <summary>
    /// Calisma zamani yukleme sinavi: icerik RESOURCES'tan yukleniyor mu,
    /// ve simulasyon Unity icinde bir gunu kosabiliyor mu.
    ///
    /// Neden ayri bir sinav: birim testleri icerigi DISKTEN okuyor. Oyun
    /// onu Resources'tan okuyacak ve o yol Android'e bakan yol. Iki yolun
    /// ayni sonucu verdigini varsaymak, telefonda acilinca ogrenmek demek.
    ///
    /// Toplu kipte de kosuyor:
    ///   Unity.exe -batchmode -quit -executeMethod Lokanta.EditorTools.SmokeTest.Run
    /// </summary>
    public static class SmokeTest
    {
        [MenuItem("Lokanta/Calisma zamani sinavi")]
        public static void Run()
        {
            StringBuilder log = new StringBuilder();
            bool ok = true;

            foreach (string cuisine in new[] { "fastfood", "turk" })
            {
                try
                {
                    IContentSource src = new ResourcesContentSource();
                    EconomyConfig economy = ContentLoader.LoadEconomy(src);
                    ContentSet content = ContentSetLoader.Load(src, cuisine);

                    TimingConfig timing = content.SlotDurationsBp != null
                        ? TimingConfig.Default()
                            .WithSlotDurations(content.SlotDurationsBp)
                            .WithEatMs(content.EatMs)
                        : TimingConfig.Default();

                    Simulation sim = new Simulation(economy, content, timing, 20260911UL);

                    // Bir gunu bastan sona kosuyoruz: yalnizca yukleme degil,
                    // TIK dongusu de sinaniyor.
                    for (int i = 0; i < sim.IngredientCount; i++)
                    {
                        int need = sim.RecommendedRestock(i);
                        if (need > 0)
                            sim.Apply(new Command(sim.TickIndex,
                                CommandKind.OrderIngredient, i, need));
                    }
                    sim.Apply(new Command(sim.TickIndex, CommandKind.OpenService));
                    for (int t = 0; t < timing.ServiceTicks + 4000; t++)
                    {
                        sim.Tick();
                        if (sim.ServiceComplete) break;
                    }
                    sim.Apply(new Command(sim.TickIndex, CommandKind.CloseDay));
                    DayReport r = sim.BuildDayReport();

                    log.AppendFormat(
                        "{0,-9} {1} yemek, {2} arketip, {3} duzenli musteri, {4} huy | " +
                        "1. gun: {5} grup agirlandi, ciro {6}, memnuniyet {7:0.0}\n",
                        cuisine, content.Dishes.Length, content.Archetypes.Length,
                        content.Regulars.Length, economy.TraitCount,
                        r.ServedParties, r.Revenue / 100,
                        r.AverageSatisfactionCenti / 100.0);

                    if (r.ServedParties == 0)
                    {
                        ok = false;
                        log.AppendLine("  HATA: hicbir grup agirlanmadi");
                    }
                }
                catch (System.Exception e)
                {
                    ok = false;
                    log.AppendFormat("{0,-9} HATA: {1}\n", cuisine, e.Message);
                }
            }

            // Kat plani ile ekonominin ayni seyi soyledigini burada da
            // sinamak ucuz: birim testi diskteki icerige bakiyor, bu
            // Resources kopyasina.
            try
            {
                EconomyConfig cfg = ContentLoader.LoadEconomy(new ResourcesContentSource());
                int running = 0, tier = 0;
                foreach (RoomPlan.Room room in RoomPlan.Rooms)
                {
                    if (!room.IsDining) continue;
                    running += room.Tables;
                    if (cfg.TierAt(tier).Tables != running)
                    {
                        ok = false;
                        log.AppendFormat("  HATA: {0} odasi {1} masa veriyor, kademe {2} ise {3} diyor\n",
                            room.Name, running, tier, cfg.TierAt(tier).Tables);
                    }
                    tier++;
                }
                log.AppendFormat("kat plani  {0} kademe, kademe tablosuyla uyumlu\n", tier);
            }
            catch (System.Exception e)
            {
                ok = false;
                log.AppendLine("kat plani HATA: " + e.Message);
            }

            if (ok) Debug.Log("CALISMA ZAMANI SINAVI GECTI\n" + log);
            else Debug.LogError("CALISMA ZAMANI SINAVI BASARISIZ\n" + log);
        }
    }
}
