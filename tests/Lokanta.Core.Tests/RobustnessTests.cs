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
    /// Oyunu KIRAN durumlar. Hepsi bir QA incelemesinde bulundu ve
    /// hepsi gercek bir oyuncunun karsilasabilecegi bir yoldan geliyor:
    /// telefonu kilitlemek, uzun oynamak, bozulmus bir kayit.
    ///
    /// Bu dosya bir regresyon duvari. Her testin basindaki yorum, hatanin
    /// NASIL tetiklendigini anlatiyor - cunku duzeltmeyi geri alan biri
    /// once o cumleyi okumali.
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

        /// <summary>Tamponu bosaltir ve DishUnlocked olaylarini sayar.</summary>
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
        public void Ucret_zammi_tavanli_ve_tasmiyor()
        {
            // Serbest oyunda ucret haftada %2,2 BILESIK buyuyordu ve
            // tavani yoktu; gelir ise masa ve itibar tavanina bagli.
            // Yuz dorduncu haftada PowNano long'u tasiyor ve istisna
            // CloseDay'in ORTASINDA atiyordu: itibar dusmus, stok
            // yaslanmis, ama asama gecmemis. Oyuncu "Gunu Kapat"a her
            // bastiginda ayni zarar bir kez daha isliyor ve gun asla
            // kapanmiyordu.
            EconomyConfig cfg = Economy();
            Crew crew = new Crew(3, 3);

            long week1 = StaffingModel.WeeklyWageBill(crew, 1, cfg);
            Assert.True(week1 > 0);

            long prev = week1;
            foreach (int week in new[] { 8, 28, 52, 104, 520, 5200 })
            {
                long bill = StaffingModel.WeeklyWageBill(crew, week, cfg);
                _out.WriteLine($"hafta {week,5}: {bill} ({(double)bill / week1:0.00} kat)");

                Assert.True(bill >= prev, "ucret geriye gitti: hafta " + week);
                Assert.True(bill <= week1 * 2,
                            "ucret iki kati asti: hafta " + week + " -> " + bill);
                prev = bill;
            }
        }

        [Fact]
        public void Gun_kapanisi_uzun_oyunda_atmiyor()
        {
            // Yukaridakinin ucu ucuna karsiligi: yalnizca formul degil
            // GUNUN KENDISI de kapanabilmeli. Ikiyuz gun oynaniyor.
            Simulation sim = NewSim();
            for (int day = 1; day <= 200; day++)
            {
                sim.Apply(new Command(sim.TickIndex, CommandKind.OpenService));
                sim.Apply(new Command(sim.TickIndex, CommandKind.CloseDay));
                Assert.Equal(DayPhase.Evening, sim.Phase);
                sim.AdvanceToNextDay();
            }
            _out.WriteLine($"200 gun sonra kasa {sim.Cash}, itibar {sim.ReputationCenti}");
            Assert.Equal(201, sim.Day);
        }

        // =====================================================================
        [Fact]
        public void Gun_ici_sayaclar_kayitta_duruyor()
        {
            // Aksam raporu yuklemeden sonra YALAN SOYLUYORDU: ucret,
            // kira ve zayiat kaydedilmedigi icin "Gunun kari" haftanin
            // en buyuk giderini yok sayip buyuk bir arti gosteriyordu.
            //
            // Tetikleme: kira gununu kapat, telefonu kilitle, geri don.
            Simulation sim = NewSim();
            for (int day = 1; day <= 7; day++)
            {
                sim.Apply(new Command(sim.TickIndex, CommandKind.OpenService));
                for (int t = 0; t < 200; t++) sim.Tick();
                sim.Apply(new Command(sim.TickIndex, CommandKind.CloseDay));
                if (day < 7) sim.AdvanceToNextDay();
            }

            DayReport before = sim.BuildDayReport();
            _out.WriteLine($"once : ucret {before.WageCost}, kira {before.RentCost}, "
                           + $"zayiat {before.SpoiledValue}, ciro {sim.TotalRevenue}");

            string json = Save(sim);
            Simulation loaded = NewSim();
            loaded.Restore(new JsonStateReader(json));

            DayReport after = loaded.BuildDayReport();
            _out.WriteLine($"sonra: ucret {after.WageCost}, kira {after.RentCost}, "
                           + $"zayiat {after.SpoiledValue}, ciro {loaded.TotalRevenue}");

            Assert.Equal(before.WageCost, after.WageCost);
            Assert.Equal(before.RentCost, after.RentCost);
            Assert.Equal(before.SpoiledValue, after.SpoiledValue);
            Assert.Equal(before.NetProfit, after.NetProfit);
            Assert.Equal(sim.TotalRevenue, loaded.TotalRevenue);
        }

        [Fact]
        public void Yuklenen_oyun_eski_yemekleri_yeniden_duyurmuyor()
        {
            // _dishWasUnlocked kaydedilmiyordu ve kurucu onu BIRINCI
            // GUNUN durumuyla dolduruyordu. Otuzuncu gunden bir kayit
            // acip ilerleyince, aradaki butun yemekler yeniden "acildi"
            // diye duyuruluyordu: her biri icin bir bildirim ve bir
            // seviye atlama sesi.
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
            CountUnlocks(loaded);          // gunun olaylarini temizle
            loaded.AdvanceToNextDay();

            int announced = CountUnlocks(loaded);

            _out.WriteLine($"yuklemeden sonraki gunde duyurulan yemek: {announced}");
            Assert.True(announced <= 2,
                        "yukleme sonrasi eski yemekler yeniden duyuruldu: " + announced);
        }

        // =====================================================================
        [Fact]
        public void Onerilen_stok_TEK_komut()
        {
            // Arayuz malzeme basina bir OrderIngredient gonderiyordu ve
            // gunluk komut siniri 256: "Onerilen stogu al" dugmesine bes
            // kez basmak gunun butcesini bitiriyor, sonrasinda fiyat,
            // menu, ise alim, ekipman, genisleme, mudahale ve veresiye
            // dahil HER komut sessizce reddediliyordu.
            //
            // Test iki seyi birden soyluyor: komut GERCEKTEN aliyor, ve
            // bunu gunlukte tek yer kaplayarak yapiyor.
            // BIR GUN OYNANIYOR, cunku acilis stogu tam bir gunluk:
            // birinci sabah RecommendedRestock her kalem icin sifir
            // donuyor ve alinacak bir sey olmuyor. Testin olctugu sey
            // "komut aliyor mu", "stok bos mu" degil.
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
            _out.WriteLine($"stok {stockBefore} -> {stockAfter}, "
                           + $"kasa {cashBefore} -> {sim.Cash}");

            Assert.True(stockAfter > stockBefore, "stok artmadi");
            Assert.True(sim.Cash < cashBefore, "para harcanmadi");

            // ASIL OLCU: gunluk komut butcesi. Yuz kez basmak bile
            // butceyi bitirmemeli - tek komutun yeri bir.
            for (int k = 0; k < 100; k++)
                sim.Apply(new Command(sim.TickIndex, CommandKind.OrderRecommended));

            // Butce bittiyse asama komutu disinda her sey reddedilir;
            // fiyat komutu hala geciyorsa butce duruyordur.
            long priceBefore = sim.DishPrice(0);
            sim.Apply(new Command(sim.TickIndex, CommandKind.SetPrice, 0,
                                  (int)(priceBefore + 100)));
            Assert.NotEqual(priceBefore, sim.DishPrice(0));
        }

        // =====================================================================
        [Theory]
        [InlineData("storageTier", 9)]
        [InlineData("quality", 7)]
        public void Bozuk_kayit_YUKLEMEDE_yakalaniyor(string field, int bad)
        {
            // Validate'in kendi yorumu "hata OYUNCUYA, oyunun icine
            // girmeden once soyleniyor" diyor. Bu iki alan agi geciyordu
            // ve oyun ICINDE cokuyordu: soguk hava kademesi her
            // CloseDay'de, kalite ise fiyat tablosunda.
            //
            // Yuva "saglam" gorunup oyunun acilip oynanamamasi, acik
            // hata vermekten daha kotu.
            Simulation sim = NewSim();
            string json = Save(sim);

            string broken = Replace(json, field, bad);
            Assert.NotEqual(json, broken);

            Simulation target = NewSim();
            Assert.Throws<InvalidOperationException>(
                () => target.Restore(new JsonStateReader(broken)));
        }

        /// <summary>
        /// "alan": sayi -> "alan": yeni. Kayit duz JSON oldugu icin
        /// metin uzerinde yapmak yeterli ve testin nereye dokundugu
        /// okunur kaliyor.
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
