using System.Collections.Generic;
using System.IO;
using Lokanta.Content;
using Lokanta.Core.Content;
using Lokanta.Core.Economy;
using Lokanta.Core.Sim;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;

namespace Lokanta.Core.Tests
{
    /// <summary>
    /// Isimli duzenli musteriler. docs/11: "isimli musteri tek bir kisidir,
    /// elle yazilmistir, hikayesi vardir ve hep ayni kisidir. Arketip ise
    /// binlerce musteri uretir."
    ///
    /// En onemli degismez en altta: duzenli musteri talebi SISIRMIYOR.
    /// Isimli musteri, gunun planindan yer ALIYOR; aksi halde her yeni isim
    /// ekonomiyi buyutur ve kalibrasyon her icerik eklemesinde kayardi.
    /// </summary>
    public class RegularTests
    {
        private readonly ITestOutputHelper _out;
        public RegularTests(ITestOutputHelper output) { _out = output; }

        private const ulong Seed = 20260910UL;

        private static EconomyConfig Economy() => ContentLoader.LoadEconomy(Paths.Content);
        private static ContentSet Content(string cuisine) =>
            ContentSetLoader.Load(Paths.Content, cuisine);

        private static TimingConfig Timing(ContentSet c) =>
            c.SlotDurationsBp != null
                ? TimingConfig.Default().WithSlotDurations(c.SlotDurationsBp).WithEatMs(c.EatMs)
                : TimingConfig.Default();

        private static Simulation NewSim(string cuisine)
        {
            ContentSet c = Content(cuisine);
            return new Simulation(Economy(), c, Timing(c), Seed);
        }

        private static void Restock(Simulation sim)
        {
            for (int i = 0; i < sim.IngredientCount; i++)
            {
                int need = sim.RecommendedRestock(i);
                if (need > 0)
                    sim.Apply(new Command(sim.TickIndex, CommandKind.OrderIngredient, i, need));
            }
        }

        private static DayReport RunOneDay(Simulation sim, ContentSet c)
        {
            Restock(sim);
            sim.Apply(new Command(sim.TickIndex, CommandKind.OpenService));
            int limit = Timing(c).ServiceTicks + 4000;
            for (int t = 0; t < limit; t++)
            {
                sim.Tick();
                if (sim.ServiceComplete) break;
            }
            sim.Apply(new Command(sim.TickIndex, CommandKind.CloseDay));
            DayReport rep = sim.BuildDayReport();
            sim.AdvanceToNextDay();
            return rep;
        }

        // ====================================================================
        // Icerik
        // ====================================================================
        [Theory]
        [InlineData("fastfood")]
        [InlineData("turk")]
        public void Mutfak_basina_on_duzenli_musteri(string cuisine)
        {
            ContentSet c = Content(cuisine);
            Assert.Equal(10, c.Regulars.Length);      // docs/09 envanteri

            foreach (RegularDef r in c.Regulars)
            {
                Assert.True(r.ArchetypeIndex >= 0 && r.ArchetypeIndex < c.Archetypes.Length);
                Assert.True(r.FavouriteDish >= 0 && r.FavouriteDish < c.Dishes.Length);
                Assert.True(r.ArrivesFromDay >= 1);
                Assert.Equal(3, r.Story.Length);       // docs/09: uc ile dort sahne
            }
        }

        [Fact]
        public void Sevdigi_yemek_geldigi_gun_ACIK_olmali()
        {
            // Yoksa mekanik ilk gunden haksiz calisir: oyuncunun elinde
            // olmayan bir eksikle karsilanir.
            foreach (string cuisine in new[] { "fastfood", "turk" })
            {
                ContentSet c = Content(cuisine);
                foreach (RegularDef r in c.Regulars)
                    Assert.True(c.Dishes[r.FavouriteDish].UnlockDay <= r.ArrivesFromDay,
                                cuisine + "/" + r.Id + " sevdigi yemek kapali geliyor");
            }
        }

        [Fact]
        public void Veresiye_adayi_yalnizca_Turk_mutfaginda()
        {
            // docs/07: veresiye Turk mutfaginin imza mekanigi. Fast food'da
            // uygun bir duzenli musteri yazmak, hic calismayacak alan yazmak.
            int turk = 0;
            foreach (RegularDef r in Content("turk").Regulars)
                if (r.VeresiyeEligible) turk++;
            Assert.True(turk >= 4, "Turk mutfaginda veresiye adayi az");

            foreach (RegularDef r in Content("fastfood").Regulars)
                Assert.False(r.VeresiyeEligible);
        }

        [Fact]
        public void Baska_mutfagin_veresiye_adayi_reddediliyor()
        {
            List<RegularDto> regs = Load<List<RegularDto>>("regulars", "fastfood.json");
            regs[0].VeresiyeEligible = true;

            ContentException ex = Assert.Throws<ContentException>(
                () => BuildFastfood(regs));
            _out.WriteLine(ex.Message);
            Assert.Contains("veresiye", ex.Message);
        }

        [Fact]
        public void Olmayan_arketip_reddediliyor()
        {
            List<RegularDto> regs = Load<List<RegularDto>>("regulars", "fastfood.json");
            regs[2].ArchetypeBase = "olmayan_arketip";
            Assert.Throws<ContentException>(() => BuildFastfood(regs));
        }

        [Fact]
        public void Kapali_yemegi_seven_musteri_reddediliyor()
        {
            List<RegularDto> regs = Load<List<RegularDto>>("regulars", "fastfood.json");
            regs[0].FavouriteDish = "buzlu_cay";        // 50. gunde aciliyor
            ContentException ex = Assert.Throws<ContentException>(
                () => BuildFastfood(regs));
            _out.WriteLine(ex.Message);
            Assert.Contains("gunde aciliyor", ex.Message);
        }

        [Fact]
        public void Bozuk_hikaye_sirasi_reddediliyor()
        {
            List<RegularDto> regs = Load<List<RegularDto>>("regulars", "fastfood.json");
            regs[0].Story[2].RequiresVisits = 1;        // azalan esik
            Assert.Throws<ContentException>(() => BuildFastfood(regs));
        }

        private static T Load<T>(params string[] parts)
        {
            string path = Paths.Content;
            foreach (string p in parts) path = Path.Combine(path, p);
            return JsonConvert.DeserializeObject<T>(File.ReadAllText(path));
        }

        private static void BuildFastfood(List<RegularDto> regs)
        {
            ContentSetLoader.Build(
                "fastfood",
                Load<List<IngredientDto>>("ingredients.json"),
                Load<List<DishDto>>("dishes", "fastfood.json"),
                Sharedplus("fastfood"),
                Load<EquipmentFileDto>("equipment.json"),
                Load<CuisineDto>("cuisines", "fastfood.json"),
                15, regs);
        }

        // ====================================================================
        // Davranis
        // ====================================================================
        [Fact]
        public void Talebi_SISIRMIYOR()
        {
            // Bu dosyanin varlik sebebi. Isimli musteri gunun planindan yer
            // ALIYOR, planina EKLENMIYOR. Ayni tohumla, duzenli musterisi
            // olan ve olmayan iki icerik ayni sayida musteri planlamali.
            ContentSet withReg = Content("turk");
            ContentSet without = ContentSetLoader.Build(
                "turk",
                Load<List<IngredientDto>>("ingredients.json"),
                Load<List<DishDto>>("dishes", "turk.json"),
                Sharedplus("turk"),
                Load<EquipmentFileDto>("equipment.json"),
                Load<CuisineDto>("cuisines", "turk.json"),
                15, null);

            Assert.NotEmpty(withReg.Regulars);
            Assert.Empty(without.Regulars);

            // Ilk duzenli musteri UCUNCU gun geliyor. Yani 1. ve 2. gun iki
            // kosuda birebir ayni, dolayisiyla 3. gunun itibari da ayni -
            // ve 3. gunun PLANI ayni cikmali. Bu, mekanigin yapisal
            // degismezini yalitiyor.
            //
            // Sonraki gunlerde sayilar AYRISIYOR ve ayrilmalari dogru:
            // sevdigi yemegi bulan musteri daha memnun ayriliyor, itibar
            // farkli isliyor, talep farkli oluyor. O fark mekanigin ta
            // kendisi; sisirme degil.
            int[] a = PlannedDays(withReg, 3);
            int[] b = PlannedDays(without, 3);
            _out.WriteLine($"duzenli musterili {string.Join(", ", a)}");
            _out.WriteLine($"musterisiz      {string.Join(", ", b)}");
            Assert.Equal(b, a);
        }

        private static int[] PlannedDays(ContentSet c, int days)
        {
            Simulation sim = new Simulation(Economy(), c, Timing(c), Seed);
            int[] out_ = new int[days];
            for (int d = 0; d < days; d++) out_[d] = RunOneDay(sim, c).PlannedParties;
            return out_;
        }

        private static List<ArchetypeDto> Sharedplus(string cuisine)
        {
            List<ArchetypeDto> a = Load<List<ArchetypeDto>>("archetypes", "shared.json");
            a.AddRange(Load<List<ArchetypeDto>>("archetypes", cuisine + ".json"));
            return a;
        }

        private static long PlannedOver(ContentSet c, int days)
        {
            Simulation sim = new Simulation(Economy(), c, Timing(c), Seed);
            long total = 0;
            for (int d = 0; d < days; d++) total += RunOneDay(sim, c).PlannedParties;
            return total;
        }

        [Fact]
        public void Gununden_once_gelmiyor()
        {
            ContentSet c = Content("turk");
            Simulation sim = NewSim("turk");

            // En gec gelen musteri 44. gunde; 20. gune kadar o hic gorunmemeli.
            int late = c.Regulars.Length - 1;
            Assert.True(c.Regulars[late].ArrivesFromDay > 20);

            for (int d = 0; d < 20; d++) RunOneDay(sim, c);
            Assert.Equal(0, sim.RegularVisits(late));
        }

        [Fact]
        public void Erken_gelen_musteri_ugruyor()
        {
            ContentSet c = Content("turk");
            Simulation sim = NewSim("turk");
            for (int d = 0; d < 30; d++) RunOneDay(sim, c);

            int visits = 0;
            for (int i = 0; i < c.Regulars.Length; i++)
            {
                if (sim.RegularVisits(i) > 0)
                    _out.WriteLine($"{c.Regulars[i].Id,-16} {sim.RegularVisits(i),3} ziyaret, " +
                                   $"ort. memnuniyet {sim.RegularSatisfactionCenti(i) / 100.0:0.0}, " +
                                   $"sahne {sim.RegularBeat(i)}");
                visits += sim.RegularVisits(i);
            }
            Assert.True(visits > 0, "otuz gunde hicbir duzenli musteri gelmedi");
        }

        [Fact]
        public void Hikaye_sahnesi_ziyaretle_aciliyor()
        {
            ContentSet c = Content("turk");
            Simulation sim = NewSim("turk");
            for (int d = 0; d < 60; d++) RunOneDay(sim, c);

            int opened = 0, top = 0;
            for (int i = 0; i < c.Regulars.Length; i++)
            {
                opened += sim.RegularBeat(i);
                if (sim.RegularBeat(i) > top) top = sim.RegularBeat(i);
            }
            _out.WriteLine($"altmis gunde {opened} sahne acildi, en ileri musteri {top}. sahnede");

            Assert.True(opened > 0, "altmis gunde hicbir hikaye sahnesi acilmadi");
            // Sahne, ESIGI karsilamadan acilmamali.
            for (int i = 0; i < c.Regulars.Length; i++)
            {
                int beat = sim.RegularBeat(i);
                if (beat == 0) continue;
                Assert.True(sim.RegularVisits(i) >= c.Regulars[i].Story[beat - 1].RequiresVisits);
            }
        }

        [Fact]
        public void Kayit_duzenli_musteri_gecmisini_tasiyor()
        {
            ContentSet c = Content("turk");
            Simulation sim = NewSim("turk");
            for (int d = 0; d < 25; d++) RunOneDay(sim, c);

            JsonStateWriter w = new JsonStateWriter();
            sim.Write(w);
            Simulation restored = NewSim("turk");
            restored.Restore(new JsonStateReader(w.ToJson()));

            for (int i = 0; i < c.Regulars.Length; i++)
            {
                Assert.Equal(sim.RegularVisits(i), restored.RegularVisits(i));
                Assert.Equal(sim.RegularBeat(i), restored.RegularBeat(i));
                Assert.Equal(sim.RegularSatisfactionCenti(i),
                             restored.RegularSatisfactionCenti(i));
            }
            Assert.Equal(sim.StateHash(), restored.StateHash());
        }

        [Fact]
        public void Veresiye_artik_ISIMLI_musteriye_aciliyor()
        {
            // docs/13 veresiyeEligible alanini duzenli musteri dosyasina
            // koymus, ve dogrusu bu: veresiye adini bildigin birine acilir.
            // Once kural "sik gelen arketip"ten turetiliyordu - calisan ama
            // kimliksiz bir yaklasimdi.
            ContentSet c = Content("turk");
            Simulation sim = NewSim("turk");
            while (sim.Day < sim.SignatureFromDay) RunOneDay(sim, c);

            int checkedParties = 0;
            for (int d = 0; d < 20; d++)
            {
                Restock(sim);
                sim.Apply(new Command(sim.TickIndex, CommandKind.OpenService));
                int limit = Timing(c).ServiceTicks + 4000;
                for (int t = 0; t < limit; t++)
                {
                    sim.Tick();
                    for (int p = 0; p < Simulation.MaxParties; p++)
                    {
                        if (!sim.CreditEligible(p)) continue;
                        int reg = sim.PartyRegular(p);
                        Assert.True(reg >= 0, "isimsiz musteriye veresiye aciliyor");
                        Assert.True(c.Regulars[reg].VeresiyeEligible);
                        checkedParties++;
                    }
                    if (sim.ServiceComplete) break;
                }
                sim.Apply(new Command(sim.TickIndex, CommandKind.CloseDay));
                sim.AdvanceToNextDay();
            }
            _out.WriteLine($"{checkedParties} kez veresiye uygunlugu goruldu");
            Assert.True(checkedParties > 0, "yirmi gunde hic veresiye istenmedi");
        }
    }
}
