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
    /// NISANLAR VE HAFTALIK KARNE.
    ///
    /// Ikisi ayni bosluğu kapatiyor: oyun yedi eksende puan veriyordu ve
    /// oyuncu onlari TAM BIR KEZ goruyordu - altmisinci gunde.
    ///
    /// Bu dosyanin asil isi bir SESSIZ HATA sinifini tutmak. Nisanlarin
    /// tehlikesi kirilmalari degil, HAK EDILMEDEN DAGITILMALARI: bir
    /// kosul yanlis yazilirsa oyun daha birinci gunde nisan verir, hicbir
    /// sey hata vermez, ve tanima degersizlesir. "Defter kapandi" tam da
    /// bu tuzagi tasiyor - hic veresiye vermemis oyuncunun da acik
    /// veresiyesi sifirdir.
    /// </summary>
    public sealed class BadgeTests
    {
        private readonly ITestOutputHelper _out;
        public BadgeTests(ITestOutputHelper o) { _out = o; }

        private const ulong Seed = 20260914UL;

        private static EconomyConfig Economy() => ContentLoader.LoadEconomy(Paths.Content);

        private static ContentSet Content(string cuisine) =>
            ContentSetLoader.Load(Paths.Content, cuisine);

        private static TimingConfig Timing(string cuisine)
        {
            ContentSet c = Content(cuisine);
            return c.SlotDurationsBp != null
                ? TimingConfig.Default().WithSlotDurations(c.SlotDurationsBp).WithEatMs(c.EatMs)
                : TimingConfig.Default();
        }

        private static Simulation NewSim(string cuisine = "fastfood") =>
            new Simulation(Economy(), Content(cuisine), Timing(cuisine), Seed);

        /// <summary>Hicbir sey yapmadan gun gun ilerler.</summary>
        private static void RunTo(Simulation sim, int day)
        {
            while (sim.Day < day)
            {
                sim.Apply(new Command(sim.TickIndex, CommandKind.OpenService));
                for (int t = 0; t < 5000; t++)
                {
                    sim.Tick();
                    if (sim.ServiceComplete) break;
                }
                sim.Apply(new Command(sim.TickIndex, CommandKind.CloseDay));
                sim.AdvanceToNextDay();
            }
        }

        /// <summary>
        /// DEFTER NISANI HAK EDILMEDEN DAGITILMIYOR.
        ///
        /// Bu testin tuttugu hata sessiz: kosul yalnizca "acik veresiye
        /// sifir" olsaydi, hic veresiye vermemis oyuncu nisani BIRINCI
        /// GUNUN sonunda alirdi. Hicbir sey kirilmaz, hicbir istisna
        /// atilmaz - yalnizca tanima degersizlesir.
        /// </summary>
        [Fact]
        public void Defter_acilmadan_kapanmis_sayilmiyor()
        {
            Simulation sim = NewSim("turk");
            RunTo(sim, 5);

            _out.WriteLine($"gun {sim.Day}, acik veresiye {sim.OpenCredit}");

            // ONCE CANLILIK: defter gercekten bos olmali, yoksa test
            // dogru sebepten degil yanlis sebepten gecerdi.
            Assert.Equal(0, sim.OpenCredit);
            Assert.False(sim.HasBadge(Badges.DefterKapandi),
                "veresiye hic verilmeden 'defter kapandi' nisani dagitildi");
        }

        /// <summary>
        /// Nisan IKINCI kez "bugun kazanildi" diye isaretlenmiyor.
        ///
        /// Aksam ekrani yalnizca bugun kazanilani gosteriyor; kosul her
        /// gun saglanmaya devam ettigi icin (ornegin kasa on binin
        /// ustunde kaldigi surece) nisan her aksam yeniden "yeni" diye
        /// cikardi.
        /// </summary>
        [Fact]
        public void Nisan_bir_kez_kazaniliyor()
        {
            Simulation sim = NewSim();

            // "BUGUN KAZANILDI" YALNIZCA AKSAM OKUNABILIYOR.
            //
            // AdvanceToNextDay onu sifirliyor, yani deger CloseDay ile
            // ertesi gunun acilisi arasinda - tam da aksam ekraninin
            // gorundugu aralikta - yasiyor. Ilk yazdigim test bunu
            // atlayip gun ilerledikten SONRA bakiyordu ve hep sifir
            // goruyordu: nisan dogru calisiyordu, olcum yanlis yerden
            // bakiyordu.
            int kazanildigiGun = 0;
            for (int gun = 1; gun <= 40 && kazanildigiGun == 0; gun++)
            {
                MakulSabah(sim);
                GunuKos(sim);
                if (sim.BadgeEarnedToday(Badges.IlkOnBin)) kazanildigiGun = sim.Day;
                sim.AdvanceToNextDay();
            }

            _out.WriteLine($"on bin nisani {kazanildigiGun}. gunde, kasa {sim.Cash}");
            Assert.True(kazanildigiGun > 0,
                "kirk gunde kasa hic on bini gormedi - olcum kosmamis");

            // Ertesi gun: nisan DURUYOR ama artik "bugun" degil.
            GunuKos(sim);
            Assert.True(sim.HasBadge(Badges.IlkOnBin), "kazanilmis nisan kayboldu");
            Assert.False(sim.BadgeEarnedToday(Badges.IlkOnBin),
                "nisan ikinci kez 'bugun kazanildi' diye isaretlendi");
        }

        /// <summary>
        /// MAKUL OYUNCU: stok alir ve yarina gereken kadroyu kurar.
        ///
        /// Pasif bot ile olcmek yaniltiyordu: hicbir sey yapmayan oyuncu
        /// zirveyi tek asci tek garsonla karsiliyor ve elbette kimse
        /// mutlu ayrilmiyor. "Hicbir nisan kazanilmadi" sonucu nisanlarin
        /// ulasilamaz oldugunu DEGIL, olcen oyuncunun kotu oynadigini
        /// gosteriyordu.
        /// </summary>
        private static void MakulSabah(Simulation sim)
        {
            for (int i = 0; i < sim.IngredientCount; i++)
            {
                int need = sim.RecommendedRestock(i);
                if (need > 0)
                    sim.Apply(new Command(sim.TickIndex,
                                          CommandKind.OrderIngredient, i, need));
            }

            // GENISLEME DE MAKUL OYUNUN PARCASI.
            //
            // Ilk olcumde yoktu ve "genisleme" ile "itibar 90" nisanlari
            // HIC kazanilmiyor gorundu. Sebep nisanlar degil olcen
            // oyuncuydu: hic buyumeyen bir dukkanda itibar masa
            // kademesinin tavanina kirpiliyor, yani 90 zaten imkansiz.
            // Kural turdaki ile ayni: bedelin uc kati kasada varsa.
            // BIRINCI HAFTA GENISLEME YOK.
            //
            // Olculdu: 3x kurali birinci gunde ZATEN saglaniyor (baslangic
            // 8.000 sikke, ilk kademe 2.500) ve bu olcum oyuncusu o gun
            // genisleyince altmisinci gunu dort masa, sifir itibar, sifir
            // kasa ile bitiriyor - kira 850'den 1.950'ye ciktigi halde
            // dolduracak musteri henuz yok.
            //
            // Bu bir DENGE bulgusu degil olcum aracinin sinirini gosteren
            // bir sonuc: tur de ayni 3x kuralini kullaniyor ve kirkinci
            // gunde 14 masaya, 96,8 itibara ulasiyor - cunku baska seyleri
            // de dogru yapiyor. Buradaki oyuncu yalnizca stok ve kadro
            // biliyor, o yuzden ona bir haftalik sabir veriliyor.
            if (sim.Day >= 8)
                for (int i = 1; i < sim.TierCount; i++)
                {
                    if (sim.TablesAtTier(i) <= sim.TableCount) continue;
                    long bedel = sim.UpgradeCostFor(i);
                    if (bedel > 0 && sim.Cash >= bedel * 3)
                        sim.Apply(new Command(sim.TickIndex, CommandKind.Expand, i));
                    break;
                }

            Crew gereken = sim.RequiredCrewTomorrow();
            while (sim.Cooks < gereken.Cooks && sim.Cooks < sim.StaffCap)
            {
                int once = sim.Cooks;
                sim.Apply(new Command(sim.TickIndex, CommandKind.Hire, 0, 0));
                if (sim.Cooks == once) break;         // tavan ya da para
            }
            while (sim.SalonStaff < gereken.Salon && sim.Cooks + sim.SalonStaff < sim.StaffCap)
            {
                int once = sim.SalonStaff;
                sim.Apply(new Command(sim.TickIndex, CommandKind.Hire, 1, 0));
                if (sim.SalonStaff == once) break;
            }
        }

        /// <summary>Gunu acar, servisi bitirir, kapatir - ILERLETMEZ.</summary>
        private static void GunuKos(Simulation sim)
        {
            sim.Apply(new Command(sim.TickIndex, CommandKind.OpenService));
            for (int t = 0; t < 5000; t++) { sim.Tick(); if (sim.ServiceComplete) break; }
            sim.Apply(new Command(sim.TickIndex, CommandKind.CloseDay));
        }

        /// <summary>
        /// Haftalik karne yedinci gunun KAPANISINDA cikiyor - altinci ya
        /// da sekizinci gunde degil.
        /// </summary>
        [Fact]
        public void Karne_yedinci_gunde_cikiyor()
        {
            Simulation sim = NewSim();

            RunTo(sim, 6);
            sim.Apply(new Command(sim.TickIndex, CommandKind.OpenService));
            for (int t = 0; t < 5000; t++) { sim.Tick(); if (sim.ServiceComplete) break; }
            sim.Apply(new Command(sim.TickIndex, CommandKind.CloseDay));
            Assert.False(sim.WeekReportReady, "altinci gunde karne cikti");

            sim.AdvanceToNextDay();
            sim.Apply(new Command(sim.TickIndex, CommandKind.OpenService));
            for (int t = 0; t < 5000; t++) { sim.Tick(); if (sim.ServiceComplete) break; }
            sim.Apply(new Command(sim.TickIndex, CommandKind.CloseDay));

            Assert.Equal(7, sim.Day);
            Assert.True(sim.WeekReportReady, "yedinci gunde karne cikmadi");
            Assert.Equal(1, sim.WeekNumber);
        }

        /// <summary>
        /// ILK HAFTANIN FARKI DEVRALINAN DUKKANI SAYMIYOR.
        ///
        /// Bu da sessiz bir hata olurdu: sifirinci gun fotografi
        /// cekilmeseydi gecen hafta sifir sayilir ve oyuncu yedinci
        /// gunde "Mekan +33" gibi, KENDISININ YAPMADIGI bir sicrama
        /// gorurdu. Devraldigi dort masali dukkanin puani onun kazanci
        /// degil.
        /// </summary>
        [Fact]
        public void Ilk_karnenin_farki_devralinani_saymiyor()
        {
            Simulation sim = NewSim();
            RunTo(sim, 7);
            sim.Apply(new Command(sim.TickIndex, CommandKind.OpenService));
            for (int t = 0; t < 5000; t++) { sim.Tick(); if (sim.ServiceComplete) break; }
            sim.Apply(new Command(sim.TickIndex, CommandKind.CloseDay));

            Assert.True(sim.WeekReportReady, "karne cikmadi - olcum kosmamis");

            // Mekan ekseni ilk haftada degismiyor (genisleme olmadi):
            // fark SIFIR olmali. Sifirinci fotograf cekilmeseydi
            // buradaki deger ekseni'n kendisi olurdu.
            int mekan = sim.WeekAxis(4);
            int fark = sim.WeekAxisDelta(4);
            _out.WriteLine($"mekan ekseni {mekan}, ilk hafta farki {fark}");

            Assert.True(mekan > 0, "mekan ekseni sifir - olcum kosmamis");
            Assert.Equal(0, fark);
        }

        /// <summary>
        /// NISANLAR AYIRT EDIYOR MU.
        ///
        /// Tanimanin tehlikesi kirilmasi degil DEGERSIZLESMESI: hepsi
        /// ilk haftada kendiliginden dagiliyorsa oyuncu bes bildirim
        /// gorur ve kampanyanin kalan elli uc gunu bos kalir. Bu, bu
        /// projenin yasasinin ayni yuzu - doymus bir eksene odenen odul
        /// gorunmez.
        ///
        /// Test bir DEGER degil bir DAGILIM sinifiyor: pasif oyuncu
        /// hepsini almamali.
        /// </summary>
        [Fact]
        public void Nisanlar_ilk_haftada_toptan_dagilmiyor()
        {
            Simulation sim = NewSim();
            int[] gun = new int[sim.BadgeCount];

            for (int g = 1; g <= sim.CampaignDays; g++)
            {
                MakulSabah(sim);
                GunuKos(sim);
                for (int i = 0; i < sim.BadgeCount; i++)
                    if (gun[i] == 0 && sim.BadgeEarnedToday(i)) gun[i] = sim.Day;
                sim.AdvanceToNextDay();
            }

            for (int i = 0; i < sim.BadgeCount; i++)
                _out.WriteLine($"{Badges.NameKey(i),-22} {(gun[i] == 0 ? "hic" : gun[i] + ". gun")}");
            _out.WriteLine($"son: masa {sim.TableCount}, itibar {sim.ReputationCenti / 100}, kasa {sim.Cash / 100}");

            int ilkHafta = 0;
            for (int i = 0; i < sim.BadgeCount; i++)
                if (gun[i] > 0 && gun[i] <= 7) ilkHafta++;

            Assert.True(ilkHafta < sim.BadgeCount,
                "butun nisanlar ilk haftada dagitildi - tanima degersiz");
        }

        /// <summary>
        /// Nisanlar ve karne kayitta tasiniyor.
        ///
        /// Kaydedilmeselerdi belirti sessiz olurdu: oyuncu oyunu kapatip
        /// acinca nisanlarini kaybeder, ve kosullar hala saglandigi icin
        /// bir kismi yeniden "yeni nisan" diye cikardi.
        /// </summary>
        [Fact]
        public void Nisanlar_kayitta_tasiniyor()
        {
            Simulation sim = NewSim();
            RunTo(sim, 8);

            int nisan = sim.BadgesEarned;
            int hafta = sim.WeekNumber;
            int eksen0 = sim.WeekAxis(0);
            _out.WriteLine($"kayit oncesi: {nisan} nisan, {hafta}. hafta, eksen0 {eksen0}");

            JsonStateWriter w = new JsonStateWriter();
            sim.Write(w);

            Simulation geri = NewSim();
            geri.Restore(new JsonStateReader(w.ToJson()));

            Assert.Equal(nisan, geri.BadgesEarned);
            Assert.Equal(hafta, geri.WeekNumber);
            Assert.Equal(eksen0, geri.WeekAxis(0));
            for (int i = 0; i < sim.BadgeCount; i++)
                Assert.Equal(sim.HasBadge(i), geri.HasBadge(i));
        }
    }
}
