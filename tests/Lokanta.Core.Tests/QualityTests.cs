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
    /// MALZEME KALITESI. Icerikte 77 malzemenin hepsinde uc kademelik bir
    /// tablo vardi (dusuk / standart / yuksek) ve simulasyon onu hic
    /// okumuyordu; tools/audit_content.py boyle buldu.
    ///
    /// Icerik burada bir tasarim karari tasiyor: en hassas ALTI malzemenin
    /// hepsi et. Tuz ile karabiber neredeyse duyarsiz. Yani "ucuza kacmak
    /// tuzda serbest, ette felaket" kurali veriye yazilmis durumda ve tek
    /// bir kuresel kalite ayari bile yemege gore farkli sonuc veriyor.
    /// </summary>
    public class QualityTests
    {
        private readonly ITestOutputHelper _out;
        public QualityTests(ITestOutputHelper output) { _out = output; }

        private const ulong Seed = 20260910UL;
        private const int Dusuk = 0, Standart = 1, Yuksek = 2;

        private static EconomyConfig Economy() => ContentLoader.LoadEconomy(Paths.Content);
        private static ContentSet Content() => ContentSetLoader.Load(Paths.Content, "fastfood");

        private static TimingConfig Timing(ContentSet c)
        {
            return c.SlotDurationsBp != null
                ? TimingConfig.Default().WithSlotDurations(c.SlotDurationsBp).WithEatMs(c.EatMs)
                : TimingConfig.Default();
        }

        private static Simulation NewSim()
        {
            ContentSet c = Content();
            return new Simulation(Economy(), c, Timing(c), Seed);
        }

        // ====================================================================
        [Fact]
        public void Kalite_tablosu_yukleniyor_ve_standart_referans()
        {
            ContentSet c = Content();
            foreach (IngredientDef d in c.Ingredients)
            {
                Assert.NotNull(d.QualityPriceBp);
                Assert.NotNull(d.QualitySatisfactionCenti);
                Assert.Equal(3, d.QualityPriceBp.Length);

                // Standart REFERANS: fiyat carpani 1,0 ve memnuniyet etkisi sifir.
                Assert.Equal(Fx.One, d.QualityPriceBp[Standart]);
                Assert.Equal(0, d.QualitySatisfactionCenti[Standart]);

                // Ucuz daha ucuz ve daha kotu, pahali daha pahali ve daha iyi.
                Assert.True(d.QualityPriceBp[Dusuk] < Fx.One, d.Id);
                Assert.True(d.QualityPriceBp[Yuksek] > Fx.One, d.Id);
                Assert.True(d.QualitySatisfactionCenti[Dusuk] < 0, d.Id);
                Assert.True(d.QualitySatisfactionCenti[Yuksek] > 0, d.Id);
            }
        }

        [Fact]
        public void En_hassas_malzemeler_ET()
        {
            // Bu test bir TASARIM KARARINI koruyor, bir kodu degil.
            // Kalite tek kuresel ayar olabiliyorsa sebebi bu: en cok onemsenen
            // malzemeler et, en az onemsenenler bahar. Dagilim tersine
            // donerse tek ayar anlamsizlasir ve malzeme basina secim gerekir.
            ContentSet c = Content();
            int worst = 0;
            foreach (IngredientDef d in c.Ingredients)
                if (d.QualitySatisfactionCenti[Dusuk] < worst)
                    worst = d.QualitySatisfactionCenti[Dusuk];

            foreach (IngredientDef d in c.Ingredients)
            {
                if (d.QualitySatisfactionCenti[Dusuk] != worst) continue;
                _out.WriteLine($"en hassas: {d.Id} ({d.QualitySatisfactionCenti[Dusuk]})");
                Assert.True(d.BasePrice >= 4000,
                    d.Id + ": en hassas malzeme ucuz cikti, dagilim bozulmus");
            }
        }

        [Fact]
        public void Ucuz_kalite_daha_az_odetiyor()
        {
            ContentSet c = Content();
            int meat = c.IngredientIndexOf("kiyma");
            Assert.True(meat >= 0);

            Simulation cheap = NewSim();
            cheap.Apply(new Command(0, CommandKind.SetQuality, Dusuk));
            long before = cheap.Cash;
            cheap.Apply(new Command(0, CommandKind.OrderIngredient, meat, 10_000));
            long cheapCost = before - cheap.Cash;

            Simulation fancy = NewSim();
            fancy.Apply(new Command(0, CommandKind.SetQuality, Yuksek));
            before = fancy.Cash;
            fancy.Apply(new Command(0, CommandKind.OrderIngredient, meat, 10_000));
            long fancyCost = before - fancy.Cash;

            _out.WriteLine($"10 kg kiyma: ucuz {cheapCost / 100}, pahali {fancyCost / 100}");
            Assert.True(cheapCost < fancyCost, "ucuz kalite daha ucuz degil");
        }

        [Fact]
        public void Kalite_memnuniyeti_degistiriyor()
        {
            ContentSet c = Content();
            int[] sat = new int[3];

            for (int q = 0; q < 3; q++)
            {
                Simulation sim = NewSim();
                sim.Apply(new Command(0, CommandKind.TakeLoan, 2));
                sim.Apply(new Command(0, CommandKind.SetQuality, q));

                // ACIKCA aliniyor. Simulasyon acilis stoguyla basliyor ve o
                // stok satin alma yolundan gecmedigi icin kalitesi notr;
                // RecommendedRestock da dolu stok gorup sifir donuyor.
                // Ilk yazimda test tam bu yuzden uc kalitede de ayni
                // memnuniyeti olcuyordu.
                for (int i = 0; i < sim.IngredientCount; i++)
                    sim.Apply(new Command(sim.TickIndex, CommandKind.OrderIngredient, i, 30_000));

                sim.Apply(new Command(sim.TickIndex, CommandKind.OpenService));
                TimingConfig t = Timing(c);
                for (int i = 0; i < t.ServiceTicks + 4000; i++)
                {
                    sim.Tick();
                    if (sim.ServiceComplete) break;
                }
                sim.Apply(new Command(sim.TickIndex, CommandKind.CloseDay));
                sat[q] = sim.BuildDayReport().AverageSatisfactionCenti;
            }

            _out.WriteLine($"memnuniyet: ucuz {sat[0]}, standart {sat[1]}, pahali {sat[2]}");
            Assert.True(sat[Dusuk] < sat[Standart], "ucuz malzeme memnuniyeti dusurmedi");
            Assert.True(sat[Yuksek] > sat[Standart], "pahali malzeme memnuniyeti artirmadi");
        }

        [Fact]
        public void Ucuz_et_kalabalik_tarifin_arkasina_saklanamiyor()
        {
            // GERILEME TESTI. Ilk uygulamada yemegin kalite etkisi
            // malzemelerin ORTALAMASI aliniyordu ve olcum reddetti: Turk
            // mutfaginda ucuz malzeme alan oyuncu 35.200 ile iyi oyunun
            // 26.211'ini GECIYORDU. Sebep ortalamanin kendisiydi; tencereye
            // atilan ucuz sogan, ucuz eti gizliyordu.
            //
            // Dogru kural: en BELIRLEYICI malzeme ne diyorsa o. Bu test onu
            // koruyor: cok malzemeli bir yemek, az malzemeli bir yemekten
            // daha az cezalanmamali.
            ContentSet c = Content();

            int few = -1, many = -1;
            for (int i = 0; i < c.Dishes.Length; i++)
            {
                if (!HasSensitiveMeat(c, i)) continue;
                int n = c.Dishes[i].Ingredients.Length;
                if (few < 0 || n < c.Dishes[few].Ingredients.Length) few = i;
                if (many < 0 || n > c.Dishes[many].Ingredients.Length) many = i;
            }
            Assert.True(few >= 0 && many >= 0, "hassas et iceren yemek bulunamadi");
            Assert.True(c.Dishes[many].Ingredients.Length > c.Dishes[few].Ingredients.Length,
                        "malzeme sayisi farkli iki yemek bulunamadi");

            Simulation sim = NewSim();
            sim.Apply(new Command(0, CommandKind.TakeLoan, 2));
            sim.Apply(new Command(0, CommandKind.SetQuality, Dusuk));
            for (int i = 0; i < sim.IngredientCount; i++)
                sim.Apply(new Command(sim.TickIndex, CommandKind.OrderIngredient, i, 30_000));

            int qFew = sim.DishQualityCentiOf(few);
            int qMany = sim.DishQualityCentiOf(many);

            _out.WriteLine($"{c.Dishes[few].Id} ({c.Dishes[few].Ingredients.Length} malzeme): {qFew}");
            _out.WriteLine($"{c.Dishes[many].Id} ({c.Dishes[many].Ingredients.Length} malzeme): {qMany}");

            // OLCU, IKI YEMEGIN BIRBIRIYLE KARSILASTIRILMASI DEGIL.
            //
            // Oyle yazilmisti ve bir sure sonra mekanigi degil GURULTUYU
            // olcmeye basladi: acilis stogu artik menuye gore kuruluyor,
            // yani her malzemenin elinde farkli miktarda "standart" mal
            // kaliyor ve siparis edilen ucuz mal onunla harmanlaniyor.
            // Iki yemegin hassas eti de icerikte tam -2000 oldugu halde
            // olculen degerler -1965 ve -1939 cikti; aradaki 26 santi
            // yemegin malzeme sayisiyla degil, o iki malzemenin stok
            // harmaniyla ilgiliydi. Test, "cok malzemeli yemek ucuz eti
            // gizliyor" diye kirildi - oysa gizleyen bir sey yoktu.
            //
            // Iddia su: yemegin kalitesi EN BELIRLEYICI malzemesinin
            // stok kalitesine esit. Malzeme sayisi hicbir sey
            // degistirmiyor. Bunu her yemek icin KENDI malzemeleriyle
            // olcmek, harmani denklemin iki tarafindan da atiyor.
            Assert.Equal(WorstStockQuality(sim, c, few), qFew);
            Assert.Equal(WorstStockQuality(sim, c, many), qMany);
        }

        /// <summary>
        /// Yemegin malzemeleri arasinda mutlak degeri EN BUYUK stok
        /// kalitesi. Simulasyonun "en belirleyici malzeme ne diyorsa o"
        /// kuralinin testteki karsiligi.
        /// </summary>
        private static int WorstStockQuality(Simulation sim, ContentSet c, int dish)
        {
            int worst = 0;
            foreach (DishIngredient p in c.Dishes[dish].Ingredients)
            {
                int q = sim.StockQualityOf(p.IngredientIndex);
                if (System.Math.Abs(q) > System.Math.Abs(worst)) worst = q;
            }
            return worst;
        }

        private static bool HasSensitiveMeat(ContentSet c, int dish)
        {
            foreach (DishIngredient p in c.Dishes[dish].Ingredients)
                if (c.Ingredients[p.IngredientIndex].QualitySatisfactionCenti[Dusuk] <= -2000)
                    return true;
            return false;
        }

        [Fact]
        public void Stok_kalitesi_agirlikli_ortalama()
        {
            // Ucuz alip sonra pahali alan, elindeki ucuz maldan hemen
            // kurtulamiyor. Aksi halde bir gram pahali alip butun stogu
            // temize cikarmak mumkun olurdu.
            ContentSet c = Content();
            int meat = c.IngredientIndexOf("kiyma");

            Simulation sim = NewSim();
            sim.Apply(new Command(0, CommandKind.TakeLoan, 2));
            sim.Apply(new Command(0, CommandKind.SetQuality, Dusuk));
            sim.Apply(new Command(0, CommandKind.OrderIngredient, meat, 20_000));
            int afterCheap = sim.StockQualityOf(meat);

            sim.Apply(new Command(0, CommandKind.SetQuality, Yuksek));
            sim.Apply(new Command(0, CommandKind.OrderIngredient, meat, 1_000));
            int afterTiny = sim.StockQualityOf(meat);

            _out.WriteLine($"20 kg ucuz sonra 1 kg pahali: {afterCheap} -> {afterTiny}");
            Assert.True(afterTiny < 0, "bir kilo pahali malzeme butun stogu temize cikardi");
        }

        [Fact]
        public void Hal_fiyatlari_her_gun_oynuyor()
        {
            // docs/12 3: "erken alim avantaji yok, stok bozuluyor. Ucuz
            // gune denk gelmek sans degil, TAKIP meselesi."
            //
            // priceVolatilityBp icerikte yaziliydi ve okunmuyordu: hal her
            // gun ayni fiyati veriyordu, yani takip edilecek bir sey yoktu.
            ContentSet c = Content();
            Simulation sim = NewSim();
            int meat = c.IngredientIndexOf("kiyma");

            var seen = new System.Collections.Generic.List<long>();
            for (int d = 0; d < 10; d++)
            {
                seen.Add(sim.IngredientPriceToday(meat));
                sim.Apply(new Command(sim.TickIndex, CommandKind.OpenService));
                sim.Apply(new Command(sim.TickIndex, CommandKind.CloseDay));
                sim.AdvanceToNextDay();
            }

            long lo = long.MaxValue, hi = 0;
            foreach (long v in seen) { if (v < lo) lo = v; if (v > hi) hi = v; }
            _out.WriteLine($"on gunde kiyma {lo / 100}-{hi / 100} sikke");

            Assert.True(hi > lo, "hal fiyati hic oynamiyor, takip edilecek bir sey yok");

            // Ayni tohum ayni fiyatlari vermeli: oynama rastgele ama
            // BELIRLENIMCI, yoksa tekrar oynatma bozulur.
            Simulation twin = NewSim();
            Assert.Equal(seen[0], twin.IngredientPriceToday(meat));
        }

        [Fact]
        public void Kayit_kaliteyi_tasiyor()
        {
            ContentSet c = Content();
            Simulation sim = NewSim();
            sim.Apply(new Command(0, CommandKind.SetQuality, Yuksek));
            sim.Apply(new Command(0, CommandKind.TakeLoan, 1));
            sim.Apply(new Command(0, CommandKind.OrderIngredient,
                                  c.IngredientIndexOf("kiyma"), 5_000));

            JsonStateWriter w = new JsonStateWriter();
            sim.Write(w);
            Simulation restored = NewSim();
            restored.Restore(new JsonStateReader(w.ToJson()));

            Assert.Equal(sim.Quality, restored.Quality);
            Assert.Equal(sim.StateHash(), restored.StateHash());
        }
    }
}
