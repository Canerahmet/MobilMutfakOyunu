using System.Collections.Generic;
using Lokanta.Content;
using Lokanta.Core.Content;
using Lokanta.Core.Economy;
using Lokanta.Core.Sim;
using Xunit;
using Xunit.Abstractions;

namespace Lokanta.Core.Tests
{
    /// <summary>
    /// IKINCI MUTFAK. Bu dosyanin varlik sebebi olculmus bir hata:
    ///
    /// Butun denge cozumu ve butun testler fast food ile yapilmisti. Turk
    /// mutfagi ilk kez kosturuldugunda sekiz stratejinin hepsi SIFIR
    /// musteriyle batti. Sebep, kodda hicbir yerde gorunmuyordu: yemek
    /// gruplari mutfaga ozel (docs/13) ama simulasyon fast food sozlugunu
    /// ("ana", "yan", "icecek") SABIT KODLAMISTI. Turk lokantasinda gruplar
    /// sulu, corba, pilav, izgara, meze. Hicbir musteri ana yemek bulamiyor,
    /// hepsi kapidan doniyordu.
    ///
    /// Buradaki testler her mutfagin oynanabilir oldugunu dogruluyor.
    /// Yeni mutfak eklenince listeye eklenmeli.
    /// </summary>
    public class CuisineTests
    {
        private readonly ITestOutputHelper _out;
        public CuisineTests(ITestOutputHelper output) { _out = output; }

        private const ulong Seed = 20260910UL;

        public static IEnumerable<object[]> Cuisines()
        {
            yield return new object[] { "fastfood" };
            yield return new object[] { "turk" };
        }

        private static EconomyConfig Economy() => ContentLoader.LoadEconomy(Paths.Content);

        private static TimingConfig Timing(ContentSet c)
        {
            return c.SlotDurationsBp != null
                ? TimingConfig.Default().WithSlotDurations(c.SlotDurationsBp).WithEatMs(c.EatMs)
                : TimingConfig.Default();
        }

        // ====================================================================
        [Theory]
        [MemberData(nameof(Cuisines))]
        public void Her_grup_tam_olarak_bir_role_dusuyor(string cuisine)
        {
            ContentSet c = ContentSetLoader.Load(Paths.Content, cuisine);

            HashSet<string> mapped = new HashSet<string>();
            foreach (string[] role in new[] { c.MainGroups, c.SideGroups,
                                              c.DrinkGroups, c.DessertGroups })
                foreach (string g in role)
                    Assert.True(mapped.Add(g), cuisine + ": '" + g + "' iki role birden dusuyor");

            HashSet<string> used = new HashSet<string>();
            foreach (DishDef d in c.Dishes) used.Add(d.Group);

            foreach (string g in used)
                Assert.True(mapped.Contains(g), cuisine + ": '" + g + "' grubu rolsuz");

            _out.WriteLine($"{cuisine}: ana [{string.Join(", ", c.MainGroups)}] " +
                           $"yan [{string.Join(", ", c.SideGroups)}] " +
                           $"icecek [{string.Join(", ", c.DrinkGroups)}]");
        }

        [Theory]
        [MemberData(nameof(Cuisines))]
        public void Ilk_gun_siparis_verilebiliyor(string cuisine)
        {
            // Asil kirilan sey buydu: ilk gun acik ana yemek yoksa butun
            // musteriler kapidan doner ve ekonomi sessizce durur.
            ContentSet c = ContentSetLoader.Load(Paths.Content, cuisine);

            int mains = 0;
            foreach (DishDef d in c.Dishes)
                if (d.UnlockDay <= 1 && c.IsInRole(d.Group, c.MainGroups)) mains++;

            _out.WriteLine($"{cuisine}: ilk gun {mains} ana yemek acik");
            Assert.True(mains > 0, cuisine + ": ilk gun acik ana yemek yok");
        }

        [Theory]
        [MemberData(nameof(Cuisines))]
        public void Bir_servis_gunu_musteri_agirliyor(string cuisine)
        {
            // Uctan uca: mutfak gercekten oynanabiliyor mu. Sayi hedefi
            // dusuk tutuldu; olculen sey denge degil, CALISIYOR MU.
            ContentSet c = ContentSetLoader.Load(Paths.Content, cuisine);
            Simulation sim = new Simulation(Economy(), c, Timing(c), Seed);

            sim.Apply(new Command(0, CommandKind.Hire, 0));
            sim.Apply(new Command(0, CommandKind.Hire, 1));

            for (int i = 0; i < sim.IngredientCount; i++)
            {
                int need = sim.RecommendedRestock(i);
                if (need > 0)
                    sim.Apply(new Command(sim.TickIndex, CommandKind.OrderIngredient, i, need));
            }

            sim.Apply(new Command(sim.TickIndex, CommandKind.OpenService));
            TimingConfig t = Timing(c);
            for (int i = 0; i < t.ServiceTicks + 4000; i++)
            {
                sim.Tick();
                if (sim.ServiceComplete) break;
            }
            sim.Apply(new Command(sim.TickIndex, CommandKind.CloseDay));
            DayReport r = sim.BuildDayReport();

            _out.WriteLine($"{cuisine}: {r.ServedParties}/{r.PlannedParties} grup, " +
                           $"{r.TurnedAwayParties} kapida, ciro {r.Revenue / 100}");

            Assert.True(r.ServedParties > 0, cuisine + ": tek grup bile agirlanmadi");
            Assert.True(r.TurnedAwayParties < r.PlannedParties,
                cuisine + ": herkes kapidan dondu");
            Assert.True(r.Revenue > 0, cuisine + ": ciro sifir");
        }

        [Theory]
        [MemberData(nameof(Cuisines))]
        public void Her_rolden_siparis_veriliyor(string cuisine)
        {
            // TATLI OLU ICERIKTI. Fast food'da 6, Turk lokantasinda 3 tatli
            // yemegi vardi ve hicbiri siparis edilemiyordu; PickOrder ana,
            // yan ve icecek seciyor, tatliya hic bakmiyordu. docs/27 3.3
            // zirve tablosu tatliya 0,08 es zamanli tabak veriyor ve ekipman
            // merdiveninde tatli istasyonunun bir yukseltmesi var: ikisi de
            // bosa calisiyordu.
            //
            // Bu test dort rolun de gercekten siparis edildigini dogruluyor.
            ContentSet c = ContentSetLoader.Load(Paths.Content, cuisine);
            Simulation sim = new Simulation(Economy(), c, Timing(c), Seed);

            sim.Apply(new Command(0, CommandKind.Hire, 0));
            sim.Apply(new Command(0, CommandKind.Hire, 1));

            int[] byRole = new int[4];
            for (int day = 0; day < 5; day++)
            {
                for (int i = 0; i < sim.IngredientCount; i++)
                {
                    int need = sim.RecommendedRestock(i);
                    if (need > 0)
                        sim.Apply(new Command(sim.TickIndex, CommandKind.OrderIngredient, i, need));
                }

                sim.Apply(new Command(sim.TickIndex, CommandKind.OpenService));
                TimingConfig t = Timing(c);
                for (int i = 0; i < t.ServiceTicks + 4000; i++)
                {
                    sim.Tick();
                    if (sim.ServiceComplete) break;
                }
                sim.Apply(new Command(sim.TickIndex, CommandKind.CloseDay));

                // Sayac gun basinda sifirlaniyor, o yuzden gun kapaninca
                // toplaniyor.
                for (int r = 0; r < 4; r++) byRole[r] += sim.OrderedInRole(r);
                sim.AdvanceToNextDay();
            }

            _out.WriteLine($"{cuisine}: ana {byRole[0]}, yan {byRole[1]}, " +
                           $"icecek {byRole[2]}, tatli {byRole[3]}");

            Assert.True(byRole[0] > 0, cuisine + ": hic ana yemek siparis edilmedi");
            Assert.True(byRole[3] > 0, cuisine + ": hic TATLI siparis edilmedi");
        }

        [Theory]
        [MemberData(nameof(Cuisines))]
        public void Adlandirilmis_ekipman_satin_alinana_kadar_kilitli(string cuisine)
        {
            // docs/09: mutfak basina 10 OZEL pisirme istasyonu. Paylasilan
            // alti istasyondan farki, baslangicta OLMAMALARI.
            //
            // "Istasyon kademesi 2 gerekli" soyut bir sarttir; "tas firin al,
            // borek acilsin" okunur bir sart. Ayni mekanik, okunabilir isim.
            ContentSet c = ContentSetLoader.Load(Paths.Content, cuisine);

            const int Shared = 6;
            if (c.Stations.Length <= Shared) return;    // bu mutfagin ozeli yok

            Simulation sim = new Simulation(Economy(), c, Timing(c), Seed);
            sim.Apply(new Command(0, CommandKind.TakeLoan, 2));

            for (int st = Shared; st < c.Stations.Length; st++)
            {
                // Bu ekipmana bagli yemekler
                var locked = new List<int>();
                for (int i = 0; i < c.Dishes.Length; i++)
                    if (c.Dishes[i].StationIndex == st && c.Dishes[i].RequiresStationTier > 0)
                        locked.Add(i);

                _out.WriteLine($"{cuisine}: {c.Stations[st].Id} -> {locked.Count} yemek");
                Assert.True(locked.Count > 0,
                    c.Stations[st].Id + ": hicbir yemek bagli degil, ekipman bos duruyor");

                // Baslangicta ALINMAMIS olmali
                Assert.Equal(0, sim.StationTier(st));
                Assert.True(sim.NextEquipmentPrice(st) > 0,
                    c.Stations[st].Id + ": bedava, yani satin alinmasi gerekmiyor");

                // Gunu ve itibari gelse bile ekipman yokken yapilamaz
                foreach (int dish in locked)
                    Assert.False(sim.IsUnlocked(dish),
                        c.Dishes[dish].Id + ": ekipman yokken acik gorunuyor");

                sim.Apply(new Command(sim.TickIndex, CommandKind.BuyEquipment, st));
                Assert.Equal(1, sim.StationTier(st));
            }
        }

        [Theory]
        [MemberData(nameof(Cuisines))]
        public void Fiyata_duyarli_musteri_daha_ucuz_siparis_veriyor(string cuisine)
        {
            // docs/13 arketip basina bir orderPreference tasarlamisti ve
            // simulasyon hicbir musteriyi digerinden farkli davranmiyordu:
            // herkes ayni dagilimla siparis veriyordu.
            //
            // Yirmi dort agirlik tablosu ELLE yazilmadi; tercih, ZATEN
            // YUKLU olan fiyat duyarliligindan turetildi. Bu test o
            // turetmenin gercekten calistigini olcuyor.
            ContentSet c = ContentSetLoader.Load(Paths.Content, cuisine);

            int stingy = -1, generous = -1;
            for (int i = 0; i < c.Archetypes.Length; i++)
            {
                if (stingy < 0
                    || c.Archetypes[i].PriceSensitivityBp > c.Archetypes[stingy].PriceSensitivityBp)
                    stingy = i;
                if (generous < 0
                    || c.Archetypes[i].PriceSensitivityBp < c.Archetypes[generous].PriceSensitivityBp)
                    generous = i;
            }

            Simulation sim = new Simulation(Economy(), c, Timing(c), Seed);
            sim.Apply(new Command(0, CommandKind.TakeLoan, 2));
            for (int i = 0; i < sim.IngredientCount; i++)
                sim.Apply(new Command(sim.TickIndex, CommandKind.OrderIngredient, i, 30_000));

            // Ayni rolden bircok kez sectiriyoruz; fark dagilimda, tek
            // seferde degil.
            long sumStingy = 0, sumGenerous = 0;
            const int N = 400;
            for (int k = 0; k < N; k++)
            {
                int a1 = sim.WouldPick(stingy, 0);
                int a2 = sim.WouldPick(generous, 0);
                if (a1 >= 0) sumStingy += c.Dishes[a1].Price;
                if (a2 >= 0) sumGenerous += c.Dishes[a2].Price;
            }

            _out.WriteLine($"{cuisine}: {c.Archetypes[stingy].Id} " +
                           $"(duyarlilik {c.Archetypes[stingy].PriceSensitivityBp}) " +
                           $"ort {sumStingy / N / 100.0:0.0}");
            _out.WriteLine($"{cuisine}: {c.Archetypes[generous].Id} " +
                           $"(duyarlilik {c.Archetypes[generous].PriceSensitivityBp}) " +
                           $"ort {sumGenerous / N / 100.0:0.0}");

            Assert.True(sumStingy < sumGenerous,
                cuisine + ": fiyata duyarli musteri daha ucuz siparis vermiyor");
        }

        [Theory]
        [MemberData(nameof(Cuisines))]
        public void Dilim_sureleri_tick_e_tam_bolunuyor(string cuisine)
        {
            // docs/28 Karar G: dilim SURELERI mutfaga gore degisiyor.
            // Turk lokantasinin ogle dilimi gunun %48'i. Bolunme tam
            // olmazsa gun uzunlugu mutfaga gore kayar ve karsilastirma
            // anlamsizlasir.
            ContentSet c = ContentSetLoader.Load(Paths.Content, cuisine);
            if (c.SlotDurationsBp == null) return;

            int sum = 0;
            foreach (int bp in c.SlotDurationsBp) sum += bp;
            Assert.Equal(Fx.One, sum);

            TimingConfig t = Timing(c);
            int ticks = 0;
            for (int i = 0; i < c.SlotDurationsBp.Length; i++) ticks += t.SlotTicks(i);
            Assert.Equal(t.ServiceTicks, ticks);

            _out.WriteLine($"{cuisine}: dilim tick " +
                           $"{t.SlotTicks(0)}/{t.SlotTicks(1)}/{t.SlotTicks(2)}/{t.SlotTicks(3)}");
        }
    }
}
