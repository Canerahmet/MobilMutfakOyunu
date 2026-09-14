using System.Collections.Generic;
using System.IO;
using Lokanta.Content;
using Lokanta.Core.Content;
using Lokanta.Core.Economy;
using Lokanta.Core.Sim;
using Xunit;
using Xunit.Abstractions;

namespace Lokanta.Core.Tests
{
    /// <summary>
    /// FIYATIN TALEBE DOGRUDAN ETKISI.
    ///
    /// Bu kanal bir zamanlar HIC YOKTU ve oyunun en buyuk tasarim
    /// acigiydi. Fiyatin tek yolu memnuniyet -> itibar idi; itibar da
    /// masa kademesinin tavanina KIRPILIYOR. Yani tavana dayanmis bir
    /// oyuncu icin memnuniyet kaybi hicbir sey satin almiyordu ve kucuk
    /// bir zam BEDAVAYDI.
    ///
    /// Denge harness'i olctu (24 tohum, 60 gun, fast food): piyasanin
    /// %10 ustunde fiyatlayan bot 27.849 sikke ile bitiriyordu; oyunun
    /// en gelismis stratejisi 25.092, taban strateji 18.670. Sabah bir
    /// kez basilan bir dugme, DAHA AZ masa ve DAHA AZ kadroyla her seyi
    /// geciyordu. Ceza yalnizca bandin disinda vardi (%30 zamda itibar
    /// sifirlaniyor ve dukkan batiyor); arasi bostu.
    ///
    /// Bu dosya o kanalin KOSTUGUNU sinar. Kanal silinirse ya da bir
    /// cagri yeri dogrudan DemandModel.CustomersPerDay'e donerse
    /// testlerin biri kirilir.
    /// </summary>
    public sealed class PricingTests
    {
        private readonly ITestOutputHelper _out;
        public PricingTests(ITestOutputHelper o) { _out = o; }

        private const ulong Seed = 20260913UL;

        private static EconomyConfig Economy() => ContentLoader.LoadEconomy(Paths.Content);
        private static ContentSet Content() => ContentSetLoader.Load(Paths.Content, "fastfood");

        private static TimingConfig Timing()
        {
            ContentSet c = Content();
            return c.SlotDurationsBp != null
                ? TimingConfig.Default().WithSlotDurations(c.SlotDurationsBp).WithEatMs(c.EatMs)
                : TimingConfig.Default();
        }

        private static Simulation NewSim() =>
            new Simulation(Economy(), Content(), Timing(), Seed);

        /// <summary>Acilmis butun yemekleri menuye koyar.</summary>
        private static void MenuyuAc(Simulation sim)
        {
            for (int i = 0; i < sim.DishCount; i++)
                if (sim.IsUnlocked(i))
                    sim.Apply(new Command(sim.TickIndex, CommandKind.SetMenuSlot, i, 1));
        }

        /// <summary>Menudeki her yemegi piyasanin verilen orani kadarina ceker.</summary>
        private static void PriceAll(Simulation sim, int markupBp)
        {
            for (int i = 0; i < sim.DishCount; i++)
            {
                long market = sim.BasePriceOf(i);
                if (market <= 0) continue;
                sim.Apply(new Command(sim.TickIndex, CommandKind.SetPrice,
                                      i, (int)Fx.Bp(market, markupBp)));
            }
        }

        [Fact]
        public void Zam_talebi_dusuruyor()
        {
            Simulation taban = NewSim();
            int piyasa = taban.ExpectedPeopleToday();

            Simulation zamli = NewSim();
            PriceAll(zamli, 11000);                 // piyasanin %10 ustu
            int zamliTalep = zamli.ExpectedPeopleToday();

            _out.WriteLine($"piyasa {piyasa} kisi, %10 zamli {zamliTalep} kisi");

            // ONCE CANLILIK: taban talep sifir olsaydi asagidaki
            // karsilastirma vakumda yesil kalirdi.
            Assert.True(piyasa > 0, "taban talep sifir - olcum kosmamis");
            Assert.True(zamliTalep < piyasa,
                $"zam talebi dusurmuyor ({piyasa} -> {zamliTalep})");
        }

        [Fact]
        public void Indirim_talebi_yukseltiyor()
        {
            Simulation taban = NewSim();
            int piyasa = taban.ExpectedPeopleToday();

            Simulation ucuz = NewSim();
            PriceAll(ucuz, 9000);                   // piyasanin %10 alti
            int ucuzTalep = ucuz.ExpectedPeopleToday();

            _out.WriteLine($"piyasa {piyasa} kisi, %10 indirimli {ucuzTalep} kisi");

            Assert.True(piyasa > 0, "taban talep sifir - olcum kosmamis");
            Assert.True(ucuzTalep > piyasa,
                $"indirim talebi yukseltmiyor ({piyasa} -> {ucuzTalep})");
        }

        /// <summary>
        /// TALEP TEK KAPIDAN GECIYOR.
        ///
        /// Gunluk musteri sayisi ALTI ayri yerde soruluyor: bugunku
        /// kadro, yarinki kadro, zirve kadro, onerilen stok, beklenen
        /// kisi ve gelis plani. Fiyat kanali eklenirken bunlardan birini
        /// atlamak, o ekranin GERCEKTE GELMEYECEK musteriye gore tavsiye
        /// vermesi demekti - orn. hal, zam yapmis bir lokantaya hala
        /// kalabalik gune gore stok onerirdi ve oyuncunun parasi cope
        /// giderdi. Sessiz, ve hicbir ekranda gorunmez.
        ///
        /// KOSULU DAVRANISLA OLCMEYI DENEDIM, OLMADI: kadro ve stok
        /// tamsayi ve 1. gunde zaten tabanda (zirve kadro 1+0, acilis
        /// stogu genis menuyu bile karsiliyor), yani fark yuvarlanip
        /// kayboluyor ve test VAKUMDA yesil kaliyordu. Degismezin
        /// kendisi zaten yapisal: "Simulation icinde CustomersPerDay'i
        /// yalnizca ExpectedCustomers cagirir". Onu dogrudan kaynakta
        /// olcmek hem kesin hem kirilabilir.
        /// </summary>
        [Fact]
        public void Talep_tek_kapidan_geciyor()
        {
            string yol = Path.Combine(Paths.Root, "unity", "Assets", "Lokanta",
                                      "Core", "Sim", "Simulation.cs");
            Assert.True(File.Exists(yol), "Simulation.cs bulunamadi: " + yol);

            string[] satirlar = File.ReadAllLines(yol);

            // TARAMA GERCEKTEN OLDU MU: dosya bos okunsa test "ihlal yok"
            // diye yesil kalirdi.
            Assert.True(satirlar.Length > 1000,
                $"Simulation.cs yalnizca {satirlar.Length} satir okundu - yol yanlis olabilir");

            List<string> ihlal = new List<string>();
            bool yardimcidaMiyiz = false;

            for (int i = 0; i < satirlar.Length; i++)
            {
                string satir = satirlar[i];

                if (satir.Contains("private int ExpectedCustomers(")) yardimcidaMiyiz = true;
                else if (yardimcidaMiyiz && satir.StartsWith("        }")) yardimcidaMiyiz = false;

                // Yorum satirlari sayilmiyor: gerekce metni icinde adi geciyor.
                string kirp = satir.TrimStart();
                if (kirp.StartsWith("//") || kirp.StartsWith("///")) continue;

                if (satir.Contains("DemandModel.CustomersPerDay") && !yardimcidaMiyiz)
                    ihlal.Add($"satir {i + 1}: {kirp}");
            }

            Assert.True(ihlal.Count == 0,
                "Talep ExpectedCustomers disindan hesaplaniyor - fiyat kanali "
                + "o cagri yerinde CALISMAZ: " + string.Join("; ", ihlal));
        }

        /// <summary>
        /// PIYASA FIYATINDA KANAL SESSIZ.
        ///
        /// Sapma yoksa carpan tam 1 olmali. Olmazsa kanal butun dengeyi
        /// kaydirir ve bunu hicbir sey soylemez: harness'taki piyasa
        /// fiyatli stratejilerin hepsi ayni anda, ayni yonde kayardi.
        /// </summary>
        [Fact]
        public void Piyasa_fiyatinda_talep_degismiyor()
        {
            Simulation taban = NewSim();
            int piyasa = taban.ExpectedPeopleToday();

            Simulation ayni = NewSim();
            PriceAll(ayni, 10000);                  // tam piyasa
            int ayniTalep = ayni.ExpectedPeopleToday();

            Assert.True(piyasa > 0, "taban talep sifir - olcum kosmamis");
            Assert.Equal(piyasa, ayniTalep);
        }

        /// <summary>
        /// CAY SALONA GIDIYOR, TEK MASAYA DEGIL.
        ///
        /// Eski hali uc fiilden birini OLU birakiyordu: cay her eksende
        /// patron ilgisinin altindaydi (memnuniyet 900'e karsi 2400,
        /// sabir x1'e karsi x2, mutfagi hizlandirmiyor) ve ustelik
        /// kasadan para cikariyordu - ilgi bedava. Ayni hakki yaktiklari
        /// icin caya basmak icin hicbir gun yoktu.
        ///
        /// Bu test yeni isini olcuyor: TEK bir cay, BIRDEN COK bekleyen
        /// masanin sabrini uzatmali. Cay yine tek masaya giderse sayi 1
        /// kalir ve test kirilir.
        /// </summary>
        [Fact]
        public void Cay_butun_bekleyenlere_gidiyor()
        {
            Simulation sim = NewSim();
            MenuyuAc(sim);
            sim.Apply(new Command(sim.TickIndex, CommandKind.OpenService));

            // Salon dolana kadar ilerlet: en az iki masa BEKLIYOR olmali,
            // yoksa "hepsine gitti" ile "birine gitti" ayirt edilemez ve
            // olcum VAKUMDA yesil kalir.
            int bekleyen = 0;
            for (int t = 0; t < 4000 && bekleyen < 2; t++)
            {
                sim.Tick();
                bekleyen = sim.WaitingParties;
            }

            Assert.True(bekleyen >= 2,
                $"iki bekleyen masa olusmadi ({bekleyen}) - olcum kosmamis");

            int hakOnce = sim.InterventionsLeft;
            int uzayan = sim.PartiesWithTea;
            sim.Apply(new Command(sim.TickIndex, CommandKind.Intervene,
                                  -1, (int)InterventionKind.FreeTea));

            Assert.True(sim.InterventionsLeft < hakOnce, "cay hak yakmadi - komut reddedildi");
            _out.WriteLine($"{bekleyen} bekleyen masa, cay alan {sim.PartiesWithTea - uzayan}");
            Assert.True(sim.PartiesWithTea - uzayan >= 2,
                $"cay yalnizca {sim.PartiesWithTea - uzayan} masaya gitti, {bekleyen} bekliyordu");
        }

        /// <summary>
        /// TAVANDA KAZANILAN ITIBAR SILINMIYOR, BIRIKIYOR.
        ///
        /// Tavan dogru bir fikir ama tasan degeri silmek, tavandaki
        /// oyuncu icin MUKEMMEL bir gun ile IDARE EDEN bir gunu ayni
        /// yapiyordu - ve iyi oynayan kampanyanin yarisindan fazlasini
        /// tavanda geciriyor.
        ///
        /// Test tavana DAYATIYOR ve birikimin olustugunu, sonra
        /// genislemede ODENDIGINI olcuyor. Birikim silinirse ya da
        /// genislemede odenmezse kiriliyor.
        /// </summary>
        [Fact]
        public void Tavanda_kazanilan_itibar_genislemede_odeniyor()
        {
            // TAVAN ICERIKTEN DUSURULUYOR.
            //
            // Gercek icerikte dort masada tavan 55 ve kucuk bir dukkanin
            // dogal denge noktasi ~34,6: tavan orada HIC baglayici degil,
            // yani tasma diye bir sey olusmuyor. Once bunu kadro ve
            // genislemeyle asmayi denedim - itibar 120 gunde 3730'da
            // dondu, tavan 7500. Tavana dayanmak icin testin harness
            // botu kadar iyi oynamasi gerekirdi, yani testin icine bir
            // bot yazmak.
            //
            // Kural ayni kural; yalnizca gorunur oldugu esik
            // yaklastiriliyor. Tavan 32 puana cekiliyor (denge ~34,6'nin
            // ALTINDA), ikinci kademe 60'ta - odemenin gorulebilmesi icin.
            EconomyConfig taban = Economy();
            TierConfig[] kademe = new TierConfig[taban.TierCount];
            for (int i = 0; i < kademe.Length; i++)
            {
                TierConfig t = taban.TierAt(i);
                kademe[i] = new TierConfig(t.Tables, t.Rent, t.Upgrade, t.StaffCap,
                                           i == 0 ? 3200 : 6000, t.Plates);
            }

            Simulation sim = new Simulation(taban.WithTiers(kademe), Content(), Timing(), Seed);
            MenuyuAc(sim);
            sim.Apply(new Command(sim.TickIndex, CommandKind.Hire, 1, 0));

            int gun = 0;
            while (gun < 40 && sim.ReputationOverflowCenti <= 0)
            {
                sim.Apply(new Command(sim.TickIndex, CommandKind.OrderRecommended));
                sim.Apply(new Command(sim.TickIndex, CommandKind.OpenService));
                while (!sim.ServiceComplete) sim.Tick();
                sim.Apply(new Command(sim.TickIndex, CommandKind.CloseDay));
                gun++;
            }

            _out.WriteLine($"{gun}. gun: itibar {sim.ReputationCenti}, "
                           + $"tavan {sim.ReputationCapCenti}, "
                           + $"birikim {sim.ReputationOverflowCenti}");

            // CANLILIK: birikim hic olusmadiysa asagisi vakumda yesil kalirdi.
            Assert.True(sim.ReputationOverflowCenti > 0,
                $"{gun} gunde tavanda birikim olusmadi - olcum kosmamis "
                + $"(itibar {sim.ReputationCenti}, tavan {sim.ReputationCapCenti})");
            Assert.Equal(sim.ReputationCapCenti, sim.ReputationCenti);

            int birikim = sim.ReputationOverflowCenti;
            int oncekiTavan = sim.ReputationCapCenti;

            sim.Apply(new Command(sim.TickIndex, CommandKind.Expand, 1));

            Assert.True(sim.ReputationCapCenti > oncekiTavan,
                $"genisleme tavani acmadi - olcum kosmamis (kasa {sim.Cash})");
            Assert.Equal(0, sim.ReputationOverflowCenti);
            Assert.True(sim.ReputationCenti > oncekiTavan,
                $"birikmis {birikim} santi odenmedi (itibar {sim.ReputationCenti})");
        }
    }
}
