using System;
using System.Collections.Generic;
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
    /// docs/23-cekirdek-sozlesmesi.md 7.6 dogrulama listesi.
    /// Kesinti testi bu dosyanin varlik sebebi: altmis gunluk kosu
    /// rastgele noktalarda kaydedilip yuklenince kesintisiz kosuyla
    /// BAYT BAYT ayni bitmeli.
    /// </summary>
    public class SaveTests
    {
        private readonly ITestOutputHelper _out;
        public SaveTests(ITestOutputHelper output) { _out = output; }

        private const ulong Seed = 20260911UL;

        private static EconomyConfig Economy() => ContentLoader.LoadEconomy(Paths.Content);
        private static ContentSet Content() => ContentSetLoader.Load(Paths.Content, "fastfood");

        private static TimingConfig Timing()
        {
            ContentSet c = Content();
            return c.SlotDurationsBp != null
                ? TimingConfig.Default().WithSlotDurations(c.SlotDurationsBp).WithEatMs(c.EatMs)
                : TimingConfig.Default();
        }

        private static Simulation NewSim(ulong seed = Seed)
        {
            return new Simulation(Economy(), Content(), Timing(), seed);
        }

        /// <summary>Kaydeder ve yeni bir simulasyona yukler.</summary>
        private static Simulation RoundTrip(Simulation sim, out int bytes)
        {
            JsonStateWriter w = new JsonStateWriter();
            sim.Write(w);
            string json = w.ToJson();
            bytes = json.Length;

            Simulation restored = NewSim();
            restored.Restore(new JsonStateReader(json));
            return restored;
        }

        // ====================================================================
        [Fact]
        public void Ayni_durum_ayni_ozeti_veriyor()
        {
            Simulation a = NewSim();
            Simulation b = NewSim();
            Assert.Equal(a.StateHash(), b.StateHash());
        }

        [Fact]
        public void Tek_tick_ozeti_degistiriyor()
        {
            Simulation sim = NewSim();
            sim.Apply(new Command(0, CommandKind.OpenService));
            ulong before = sim.StateHash();
            sim.Tick();
            Assert.NotEqual(before, sim.StateHash());
        }

        [Fact]
        public void Kaydet_yukle_ozeti_koruyor()
        {
            Simulation sim = NewSim();
            sim.Apply(new Command(0, CommandKind.OpenService));
            for (int i = 0; i < 900; i++) sim.Tick();

            ulong before = sim.StateHash();
            Simulation restored = RoundTrip(sim, out int bytes);

            _out.WriteLine($"kayit boyutu {bytes / 1024} KB");
            Assert.Equal(before, restored.StateHash());
        }

        [Fact]
        public void Kayit_boyutu_butcede()
        {
            // docs/23 7.3: anlik goruntu sikistirilmadan 40 KB alti hedefi.
            // JSON metin hali daha buyuk; gzip oncesi tavan gevsek tutuluyor.
            Simulation sim = NewSim();
            sim.Apply(new Command(0, CommandKind.OpenService));
            for (int i = 0; i < 2000; i++) sim.Tick();

            RoundTrip(sim, out int bytes);
            _out.WriteLine($"sikistirilmamis JSON {bytes / 1024} KB");
            Assert.True(bytes < 400 * 1024,
                $"kayit cok buyuk: {bytes / 1024} KB");
        }

        [Fact]
        public void Yukledikten_sonra_ayni_devam_ediyor()
        {
            Simulation a = NewSim();
            a.Apply(new Command(0, CommandKind.OpenService));
            for (int i = 0; i < 500; i++) a.Tick();

            Simulation b = RoundTrip(a, out _);

            // Ikisini de ayni kadar ilerlet: ozetler ayni kalmali
            for (int i = 0; i < 1500; i++) { a.Tick(); b.Tick(); }

            Assert.Equal(a.StateHash(), b.StateHash());
            Assert.Equal(a.ServedParties, b.ServedParties);
            Assert.Equal(a.Cash, b.Cash);
        }

        /// <summary>
        /// ESKI SURUM KAYDI ACILIYOR - ve mekanizma GERCEKTEN kosuyor.
        ///
        /// `SaveVersion` artarsa her oyuncunun altmis gunluk kampanyasi
        /// gider; docs/README bunu ilk guncellemeden onceki sart diye
        /// yaziyordu ve goc yolu yazilmamisti. Dosyanin kendi kurali
        /// ("yeni alanlar Has() ile okunur") 126 okumanin IKISINDE
        /// uygulanmisti - yani yine akil yurutmeyle yazilmis, hic
        /// kosturulmamis bir koruma.
        ///
        /// Bu test 21. surum kaydini alip 21'de EKLENEN alanlari
        /// siliyor ve surumu 20 yapiyor - yani yayindan sonraki gercek
        /// durumun aynisini kuruyor: elinde eski bir kayit var, kod
        /// yeni. Sonra yukluyor.
        ///
        /// Olcut iki yonlu: kayit ACILACAK (istisna yok, oyun devam
        /// ediyor) ve eksik alanlar VARSAYILANDA kalacak. Yalnizca
        /// birincisini sormak, her seyi sifirlayan bir goc yolunu da
        /// yesil gecirirdi.
        /// </summary>
        [Fact]
        public void Eski_surum_kaydi_aciliyor()
        {
            Simulation a = NewSim();
            for (int i = 0; i < 400; i++) a.Tick();

            JsonStateWriter w = new JsonStateWriter();
            a.Write(w);
            Newtonsoft.Json.Linq.JObject root =
                Newtonsoft.Json.Linq.JObject.Parse(w.ToJson());

            // 21. surumde eklenen alanlari sil, surumu geriye al.
            //
            // SURUM "header"DA, ALANLAR "restaurant"TA. Ilk yazimda
            // ikisini de header'da aradim ve testin kendi dogrulama
            // satiri beni durdurdu - kurdugum "eski kayit" gercekci
            // degildi ve mekanizmayi hic sinamadan yesil gececekti.
            Newtonsoft.Json.Linq.JObject restaurant =
                (Newtonsoft.Json.Linq.JObject)root["restaurant"];
            foreach (string alan in new[] { "badges", "badgesToday",
                                            "creditEverOpened", "weekReportDay",
                                            "weekAxis", "weekAxisPrev" })
            {
                Assert.True(restaurant[alan] != null,
                    "21. surum kaydinda olmasi gereken alan yok: " + alan
                    + " - testin kurdugu 'eski kayit' gercekci degil");
                restaurant.Remove(alan);
            }
            ((Newtonsoft.Json.Linq.JObject)root["header"])["version"] =
                Simulation.SaveVersion - 1;

            Simulation b = NewSim();
            b.Restore(new JsonStateReader(root));

            // Kayit acildi: oyun kaldigi yerden devam ediyor.
            Assert.Equal(a.Day, b.Day);
            Assert.Equal(a.Cash, b.Cash);
            Assert.Equal(a.ServedParties, b.ServedParties);

            // Eksik alanlar varsayilanda: nisan kazanilmamis sayiliyor.
            Assert.Equal(0, b.BadgesEarned);

            // Ve devam edebiliyor - yuklenen durum kosabilir durumda.
            for (int i = 0; i < 200; i++) b.Tick();
        }

        /// <summary>
        /// Okunabilen araligin DISI reddediliyor.
        ///
        /// Tek yonlu bir goc testi, "her surumu kabul et ve alanlari
        /// bos birak" gibi bir uygulamayi da gecirirdi. Bu kol, kapinin
        /// hala bir kapi oldugunu soyluyor.
        /// </summary>
        [Fact]
        public void Cok_eski_surum_reddediliyor()
        {
            Simulation a = NewSim();
            JsonStateWriter w = new JsonStateWriter();
            a.Write(w);
            Newtonsoft.Json.Linq.JObject root =
                Newtonsoft.Json.Linq.JObject.Parse(w.ToJson());
            ((Newtonsoft.Json.Linq.JObject)root["header"])["version"] =
                Simulation.MinReadableVersion - 1;

            Simulation b = NewSim();
            Assert.ThrowsAny<Exception>(() => b.Restore(new JsonStateReader(root)));
        }

        [Fact]
        public void Kesinti_testi_altmis_gun()
        {
            // docs/23 7.6: 60 gunluk kosu, rastgele noktalarda kaydet ve
            // yukle, son ozet kesintisiz kosuyla esit olmali.
            const int days = 60;
            const int interruptions = 200;

            ulong clean = RunCampaign(days, null);
            ulong interrupted = RunCampaign(days, BuildInterruptionPoints(interruptions, days));

            _out.WriteLine($"kesintisiz  {clean:X16}");
            _out.WriteLine($"kesintili   {interrupted:X16}");
            Assert.Equal(clean, interrupted);
        }

        /// <summary>
        /// Kampanyayi kosar. interruptAt null degilse o gunlerde servis
        /// ortasinda kaydedip yukler.
        /// </summary>
        private ulong RunCampaign(int days, HashSet<int> interruptAt)
        {
            Simulation sim = NewSim();
            TimingConfig timing = Timing();
            int limit = timing.ServiceTicks + 6000;

            for (int day = 1; day <= days; day++)
            {
                // Sade ama gercekci bir oyuncu: her sabah hal'e gidiyor.
                for (int i = 0; i < sim.IngredientCount; i++)
                {
                    int need = sim.RecommendedRestock(i);
                    if (need > 0)
                        sim.Apply(new Command(sim.TickIndex, CommandKind.OrderIngredient, i, need));
                }

                sim.Apply(new Command(sim.TickIndex, CommandKind.OpenService));

                int cut = interruptAt != null && interruptAt.Contains(day)
                    ? 200 + (day * 37) % 2000
                    : -1;

                for (int t = 0; t < limit; t++)
                {
                    sim.Tick();
                    if (t == cut)
                        sim = RoundTrip(sim, out _);
                    if (sim.ServiceComplete) break;
                }

                sim.Apply(new Command(sim.TickIndex, CommandKind.CloseDay));
                sim.AdvanceToNextDay();
            }

            return sim.StateHash();
        }

        private static HashSet<int> BuildInterruptionPoints(int count, int days)
        {
            // Deterministik dagilim; testin kendisi de tekrarlanabilir olmali.
            HashSet<int> set = new HashSet<int>();
            Rng rng = RngSeeder.Create(4242UL, RngStream.Event);
            for (int i = 0; i < count; i++) set.Add(rng.NextInt(days) + 1);
            return set;
        }

        [Fact]
        public void Komut_gunlugu_kaydediliyor()
        {
            Simulation sim = NewSim();
            sim.Apply(new Command(0, CommandKind.OrderIngredient, 0, 500));
            sim.Apply(new Command(0, CommandKind.OpenService));
            sim.Apply(new Command(5, CommandKind.SetPrice, 0, 5000));

            Assert.Equal(3, sim.CommandCount);
            Assert.Equal(CommandKind.OrderIngredient, sim.CommandAt(0).Kind);
            Assert.Equal(CommandKind.SetPrice, sim.CommandAt(2).Kind);

            Command[] copy = sim.CopyCommandLog();
            Assert.Equal(3, copy.Length);
        }

        [Fact]
        public void Komut_gunlugu_gun_basinda_temizleniyor()
        {
            Simulation sim = NewSim();
            sim.Apply(new Command(0, CommandKind.OpenService));
            sim.Apply(new Command(0, CommandKind.CloseDay));
            Assert.True(sim.CommandCount > 0);

            sim.AdvanceToNextDay();
            Assert.Equal(0, sim.CommandCount);
        }

        [Fact]
        public void Gunluk_siniri_asilinca_komut_reddediliyor()
        {
            Simulation sim = NewSim();

            // Sinira kadar: fiyat degisiyor.
            sim.Apply(new Command(0, CommandKind.SetPrice, 0, 4000));
            Assert.Equal(4000, sim.DishPrice(0));

            for (int i = 1; i < Simulation.MaxCommandsPerDay; i++)
                sim.Apply(new Command(0, CommandKind.SetPrice, 0, 4000));

            // Sinirdan SONRA: komut ISLENMIYOR da.
            //
            // Testin adi bastan beri "reddediliyor" diyordu ama yalnizca
            // GUNLUK UZUNLUGUNU olcuyordu; komut gunluge girmiyor ama
            // durumu yine de degistiriyordu. Yani "ayni tohum + ayni
            // gunluk = ayni durum" sozlesmesi kirilabiliyordu ve test bunu
            // gormuyordu.
            sim.Apply(new Command(0, CommandKind.SetPrice, 0, 9999));
            Assert.Equal(Simulation.MaxCommandsPerDay, sim.CommandCount);
            Assert.Equal(4000, sim.DishPrice(0));
        }

        [Fact]
        public void Bozuk_kayit_sessizce_kabul_edilmiyor()
        {
            Simulation sim = NewSim();
            JsonStateWriter w = new JsonStateWriter();
            sim.Write(w);

            Newtonsoft.Json.Linq.JObject root = w.Root;
            ((Newtonsoft.Json.Linq.JObject)root["header"])["version"] = 99;

            Simulation target = NewSim();
            Assert.Throws<InvalidOperationException>(
                () => target.Restore(new JsonStateReader(root)));
        }

        [Fact]
        public void Eksik_alan_sessizce_gecilmiyor()
        {
            Simulation sim = NewSim();
            JsonStateWriter w = new JsonStateWriter();
            sim.Write(w);

            Newtonsoft.Json.Linq.JObject root = w.Root;
            ((Newtonsoft.Json.Linq.JObject)root["restaurant"]).Remove("cash");

            Simulation target = NewSim();
            Assert.Throws<ContentException>(
                () => target.Restore(new JsonStateReader(root)));
        }
    }
}
