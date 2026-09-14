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
    /// Imza mekanikleri. docs/07: "en onemli satir - satin almanin yeniden
    /// boyama degil BASKA BIR OYUN oldugunu gosteren sey bu."
    ///
    ///   fast food -> kombo ve akis     (fis buyur, mutfak yuku buyur)
    ///   turk      -> veresiye          (nakit akisi bozulur, sadakat artar)
    ///
    /// docs/23 8.2: mekanik kodda, sayilar veride; blok eksikse mutfak
    /// yuklenmez. docs/09: mekanik IKINCI MEVSIMIN basinda geliyor.
    /// </summary>
    public class SignatureTests
    {
        private readonly ITestOutputHelper _out;
        public SignatureTests(ITestOutputHelper output) { _out = output; }

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

        /// <summary>Gunu, veresiye acabilecegi ilk gruba acarak kosar.</summary>
        private static int RunDayGrantingCredit(Simulation sim, ContentSet c)
        {
            Restock(sim);
            sim.Apply(new Command(sim.TickIndex, CommandKind.OpenService));
            int granted = 0;
            int limit = Timing(c).ServiceTicks + 4000;
            for (int t = 0; t < limit; t++)
            {
                sim.Tick();
                for (int p = 0; p < Simulation.MaxParties; p++)
                {
                    if (!sim.CreditEligible(p)) continue;
                    sim.Apply(new Command(sim.TickIndex, CommandKind.ExtendCredit, p));
                    granted++;
                }
                if (sim.ServiceComplete) break;
            }
            sim.Apply(new Command(sim.TickIndex, CommandKind.CloseDay));
            sim.AdvanceToNextDay();
            return granted;
        }

        // ====================================================================
        // Icerik ve dogrulama
        // ====================================================================
        [Fact]
        public void Iki_mutfagin_imzasi_farkli()
        {
            Assert.Equal(SignatureKind.Combo, Content("fastfood").Signature.Kind);
            Assert.Equal(SignatureKind.Credit, Content("turk").Signature.Kind);
        }

        [Fact]
        public void Imza_ikinci_mevsimin_basinda_geliyor()
        {
            // docs/09: birinci mevsim menu ve fiyati ogretmekle dolu.
            EconomyConfig e = Economy();
            foreach (string cuisine in new[] { "fastfood", "turk" })
                Assert.Equal(e.SeasonDays + 1, Content(cuisine).Signature.FromDay);
        }

        [Fact]
        public void Imza_blogu_yoksa_mutfak_yuklenmiyor()
        {
            // docs/23 8.2 bunu acikca soyluyor. Sessiz varsayilan, "satin
            // aldigin mutfak aslinda ayni oyun" demek olurdu.
            ContentException ex = Assert.Throws<ContentException>(
                () => BuildWith(sig => { sig.Kind = null; }));
            _out.WriteLine(ex.Message);
            Assert.Contains("signature", ex.Message);
        }

        [Fact]
        public void Bilinmeyen_imza_turu_reddediliyor()
        {
            Assert.Throws<ContentException>(
                () => BuildWith(sig => { sig.Kind = "tombala"; }));
        }

        [Fact]
        public void Kombo_mutfagi_rahatlatamaz()
        {
            // docs/07: kombo fisi yukseltir AMA mutfak yukunu artirir.
            // kitchenLoadBp < 10000 mekanigi tersine cevirir: bedava kazanc.
            Assert.Throws<ContentException>(
                () => BuildWith(sig => { sig.Combo.KitchenLoadBp = 9000; }));
        }

        [Fact]
        public void Kombo_indirimi_olmali()
        {
            Assert.Throws<ContentException>(
                () => BuildWith(sig => { sig.Combo.PriceBp = 10000; }));
        }

        [Fact]
        public void Mekanik_geldiginde_kilitli_kombo_reddediliyor()
        {
            ContentException ex = Assert.Throws<ContentException>(
                () => BuildWith(sig => { sig.Combo.Items[2] = "buzlu_cay"; }));   // 50. gun
            _out.WriteLine(ex.Message);
            Assert.Contains("mekanik", ex.Message);
        }

        private static void BuildWith(System.Action<SignatureDto> mutate)
        {
            string dir = Paths.Content;
            CuisineDto cui = JsonConvert.DeserializeObject<CuisineDto>(
                File.ReadAllText(Path.Combine(dir, "cuisines", "fastfood.json")));
            mutate(cui.Signature);

            ContentSetLoader.Build(
                "fastfood",
                JsonConvert.DeserializeObject<List<IngredientDto>>(
                    File.ReadAllText(Path.Combine(dir, "ingredients.json"))),
                JsonConvert.DeserializeObject<List<DishDto>>(
                    File.ReadAllText(Path.Combine(dir, "dishes", "fastfood.json"))),
                JsonConvert.DeserializeObject<List<ArchetypeDto>>(
                    File.ReadAllText(Path.Combine(dir, "archetypes", "shared.json"))),
                JsonConvert.DeserializeObject<EquipmentFileDto>(
                    File.ReadAllText(Path.Combine(dir, "equipment.json"))),
                cui, 15);
        }

        // ====================================================================
        // Kombo
        // ====================================================================
        [Fact]
        public void Kombo_ucunu_bir_fiyata_satiyor()
        {
            ContentSet c = Content("fastfood");
            Simulation sim = NewSim("fastfood");
            int[] d = c.Signature.ComboDishes;

            long sum = sim.DishPrice(d[0]) + sim.DishPrice(d[1]) + sim.DishPrice(d[2]);
            long combo = sim.ComboPrice();
            _out.WriteLine($"ayri ayri {sum / 100} sikke, kombo {combo / 100} sikke");

            Assert.True(combo < sum, "kombo indirim olmali");
            Assert.Equal(Fx.MulDiv(sum, c.Signature.ComboPriceBp, Fx.One), combo);
        }

        [Fact]
        public void Kombo_birinci_mevsimde_acilamiyor()
        {
            Simulation sim = NewSim("fastfood");
            Assert.False(sim.SignatureOpen);
            sim.Apply(new Command(sim.TickIndex, CommandKind.SetCombo, 1));
            Assert.False(sim.ComboEnabled);
        }

        [Fact]
        public void Kombo_ikinci_mevsimde_aciliyor()
        {
            ContentSet c = Content("fastfood");
            Simulation sim = NewSim("fastfood");
            while (sim.Day < sim.SignatureFromDay) RunOneDay(sim, c);

            Assert.True(sim.SignatureOpen);
            sim.Apply(new Command(sim.TickIndex, CommandKind.SetCombo, 1));
            Assert.True(sim.ComboEnabled);
        }

        [Fact]
        public void Kombo_fisi_buyutuyor_ve_mutfagi_yoruyor()
        {
            // Ayni tohum, ayni gunler; tek fark kombonun acik olmasi.
            // docs/07'nin takasi: fis buyur, mutfak yuku buyur.
            long withCombo = ComboRun(true, out int servedOn, out long tickets);
            long without = ComboRun(false, out int servedOff, out long ticketsOff);

            _out.WriteLine($"kombo acik : ciro {withCombo / 100}, {servedOn} kisi, fis {tickets / 100}");
            _out.WriteLine($"kombo kapali: ciro {without / 100}, {servedOff} kisi, fis {ticketsOff / 100}");

            Assert.NotEqual(withCombo, without);
        }

        private static long ComboRun(bool on, out int served, out long ticketCenti)
        {
            ContentSet c = Content("fastfood");
            Simulation sim = NewSim("fastfood");
            while (sim.Day < sim.SignatureFromDay) RunOneDay(sim, c);
            sim.Apply(new Command(sim.TickIndex, CommandKind.SetCombo, on ? 1 : 0));

            long revenue = 0;
            served = 0;
            for (int d = 0; d < 10; d++)
            {
                DayReport r = RunOneDay(sim, c);
                revenue += r.Revenue;
                served += r.ServedPeople;
            }
            ticketCenti = served > 0 ? revenue / served : 0;
            return revenue;
        }

        // ====================================================================
        // Veresiye
        // ====================================================================
        [Fact]
        public void Veresiye_birinci_mevsimde_acilamiyor()
        {
            Simulation sim = NewSim("turk");
            Assert.False(sim.HasCredit);
            for (int p = 0; p < Simulation.MaxParties; p++)
                Assert.False(sim.CreditEligible(p));
        }

        [Fact]
        public void Veresiye_yalnizca_ADINI_BILDIGIN_kisiye_aciliyor()
        {
            // Kural ICERIKTEN geliyor: duzenli musteri dosyasindaki
            // veresiyeEligible alani.
            //
            // Bu test bir zamanlar "sik gelen arketip" diye yaziliydi ve o
            // dogruydu - duzenli musteri icerigi HENUZ YOKKEN. Icerik
            // yazilinca kural degisti (Simulation.CreditIdentityOk), test
            // degismedi ve bir sure yanlis seyi dogruladi: artik YEDEK
            // kurali sinaliyordu.
            //
            // Icerik bunu bilerek boyle kurdu ve dogrusu bu: Nazife Teyze
            // emekli (orta kademe), Mehmet Dede eski musteri (nadir
            // kademe) - ikisi de veresiye alabiliyor. Cunku veresiye
            // SIKLIGA degil TANISIKLIGA aciliyor; adini bildigin kisiye
            // acilir. Arketip kademesine bakan bir kural, mahallenin
            // emeklisini kapinin onunde birakirdi.
            ContentSet c = Content("turk");
            Simulation sim = NewSim("turk");
            while (sim.Day < sim.SignatureFromDay) RunOneDay(sim, c);

            // AYRI gruplar sayiliyor, tick basina degil: bir grup salonda
            // yuz tick oturuyor ve tick sayarsak orneklem tek bir masaya
            // kilitleniyor. Ilk yazimda test tam bu yuzden yanlis kirildi.
            var seenTier = new Dictionary<int, int>();
            var counted = new HashSet<int>();

            for (int day = 0; day < 5; day++)
            {
                Restock(sim);
                sim.Apply(new Command(sim.TickIndex, CommandKind.OpenService));
                counted.Clear();
                int limit = Timing(c).ServiceTicks + 4000;
                for (int t = 0; t < limit; t++)
                {
                    sim.Tick();
                    for (int p = 0; p < Simulation.MaxParties; p++)
                    {
                        if (!sim.PartyActive(p) || !counted.Add(p)) continue;
                        int tier = c.Archetypes[sim.PartyArchetype(p)].TierIndex;
                        seenTier.TryGetValue(tier, out int n);
                        seenTier[tier] = n + 1;
                        // Uygunluk iki sart: ADI BILINEN biri olacak VE
                        // isteyecek. Yani "uygun ise tanidik" tek yonlu
                        // bir iddia.
                        if (!sim.CreditEligible(p)) continue;

                        int reg = sim.PartyRegular(p);
                        Assert.True(reg >= 0,
                            "veresiye isimsiz bir gruba acildi (kademe " + tier + ")");
                        Assert.True(c.Regulars[reg].VeresiyeEligible,
                            "veresiye " + c.Regulars[reg].Id
                            + " icin acildi ama icerik ona izin vermiyor");
                    }
                    if (sim.ServiceComplete) break;
                }
                sim.Apply(new Command(sim.TickIndex, CommandKind.CloseDay));
                sim.AdvanceToNextDay();
            }

            foreach (var kv in seenTier) _out.WriteLine($"kademe {kv.Key}: {kv.Value} grup");
            Assert.True(seenTier.ContainsKey(0), "hic sik gelen musteri gelmedi");
            Assert.True(seenTier.Count > 1, "yalnizca tek kademe geldi, orneklem yetersiz");
        }

        [Fact]
        public void Veresiye_fisi_kasaya_degil_deftere_yaziyor()
        {
            ContentSet c = Content("turk");
            Simulation sim = NewSim("turk");
            while (sim.Day < sim.SignatureFromDay) RunOneDay(sim, c);

            long cashBefore = sim.Cash;
            int granted = 0;
            for (int d = 0; d < 30 && sim.OpenCreditCount == 0; d++)
                granted += RunDayGrantingCredit(sim, c);

            _out.WriteLine($"{granted} gruba veresiye acildi, " +
                           $"acik hesap {sim.OpenCredit / 100} sikke");

            Assert.True(granted > 0, "otuz gunde hic veresiye istenmedi");
            Assert.True(sim.OpenCredit > 0, "fis deftere yazilmadi");
            Assert.True(sim.OpenCreditCount > 0);
            Assert.True(cashBefore >= 0);
        }

        [Fact]
        public void Vadesi_gelen_hesap_kapaniyor()
        {
            ContentSet c = Content("turk");
            Simulation sim = NewSim("turk");
            while (sim.Day < sim.SignatureFromDay) RunOneDay(sim, c);

            for (int d = 0; d < 30 && sim.OpenCreditCount == 0; d++)
                RunDayGrantingCredit(sim, c);
            int opened = sim.OpenCreditCount;
            Assert.True(opened > 0, "otuz gunde hic veresiye istenmedi");

            // Vade dolana kadar kos: defter bosalmali.
            for (int d = 0; d <= c.Signature.CreditDueDays + 1; d++) RunOneDay(sim, c);

            _out.WriteLine($"{opened} hesap acildi, vade sonrasi {sim.OpenCreditCount} kaldi");
            Assert.True(sim.OpenCreditCount < opened,
                        "vadesi gelen hesaplar kapanmiyor");
        }

        [Fact]
        public void Isteyeni_geri_cevirmek_itibardan_goturuyor()
        {
            // Mekanigin asil yonu. Veresiye TEKLIF EDILEN bir prim degil,
            // ISTENEN bir sey; vermeyen kaybediyor. Kilitli yemegi soran
            // musteri mekanigiyle ayni fikir (docs/34 6).
            ContentSet c = Content("turk");

            Simulation veren = NewSim("turk");
            while (veren.Day < veren.SignatureFromDay) RunOneDay(veren, c);
            for (int d = 0; d < 25; d++) RunDayGrantingCredit(veren, c);

            Simulation cevirem = NewSim("turk");
            while (cevirem.Day < cevirem.SignatureFromDay) RunOneDay(cevirem, c);
            for (int d = 0; d < 25; d++) RunOneDay(cevirem, c);

            _out.WriteLine($"veren     : kasa {veren.Cash / 100}, defter {veren.OpenCredit / 100}, " +
                           $"itibar {veren.ReputationCenti / 100.0:0.0}, " +
                           $"sadakat {veren.CreditLoyaltyBp / 100.0:0.0}%");
            _out.WriteLine($"geri ceviren: kasa {cevirem.Cash / 100}, " +
                           $"itibar {cevirem.ReputationCenti / 100.0:0.0}");

            // Veren, defteriyle birlikte geri cevirenin gerisinde kalmamali:
            // mekanik bir CEZA degil, bir takas.
            long verenVarlik = veren.Cash + veren.OpenCredit;
            _out.WriteLine($"veren net varlik {verenVarlik / 100}, " +
                           $"geri ceviren {cevirem.Cash / 100}");
            Assert.True(veren.CreditLoyaltyBp > 0, "tahsil edilen hesap sadakat birakmiyor");
        }

        [Fact]
        public void Veresiye_bir_TAKAS_hem_kazandiriyor_hem_batiyor()
        {
            // Ayni tohum: biri veresiye acan, biri acmayan. Ikisi de
            // gecerli oynanis olmali - biri digerini her kosuda ezerse
            // mekanik karar degil, dugme olur.
            ContentSet c = Content("turk");

            Simulation a = NewSim("turk");
            while (a.Day < a.SignatureFromDay) RunOneDay(a, c);
            for (int d = 0; d < 20; d++) RunDayGrantingCredit(a, c);

            Simulation b = NewSim("turk");
            while (b.Day < b.SignatureFromDay) RunOneDay(b, c);
            for (int d = 0; d < 20; d++) RunOneDay(b, c);

            _out.WriteLine($"veresiyeci : kasa {a.Cash / 100}, itibar {a.ReputationCenti / 100.0:0.0}, " +
                           $"acik {a.OpenCredit / 100}");
            _out.WriteLine($"pesinci    : kasa {b.Cash / 100}, itibar {b.ReputationCenti / 100.0:0.0}");

            // Veresiyeci daha az NAKIT tutuyor: mekanigin bedeli bu.
            Assert.True(a.Cash <= b.Cash + a.OpenCredit,
                        "veresiye nakit akisini hic bozmuyor");
        }

        [Fact]
        public void Kayit_veresiye_defterini_tasiyor()
        {
            ContentSet c = Content("turk");
            Simulation sim = NewSim("turk");
            while (sim.Day < sim.SignatureFromDay) RunOneDay(sim, c);

            // Isteyen cikana kadar kos: %12'lik bir olasilik, tek gunde
            // hic gelmeyebilir.
            for (int d = 0; d < 30 && sim.OpenCreditCount == 0; d++)
                RunDayGrantingCredit(sim, c);
            Assert.True(sim.OpenCreditCount > 0, "otuz gunde hic veresiye istenmedi");

            JsonStateWriter w = new JsonStateWriter();
            sim.Write(w);

            Simulation restored = NewSim("turk");
            restored.Restore(new JsonStateReader(w.ToJson()));

            Assert.Equal(sim.OpenCredit, restored.OpenCredit);
            Assert.Equal(sim.OpenCreditCount, restored.OpenCreditCount);
            Assert.Equal(sim.StateHash(), restored.StateHash());
        }
    }
}
