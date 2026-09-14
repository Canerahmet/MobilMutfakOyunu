using Lokanta.Content;
using Lokanta.Core.Content;
using Lokanta.Core.Economy;
using Lokanta.Core.Sim;
using Xunit;
using Xunit.Abstractions;

namespace Lokanta.Core.Tests
{
    /// <summary>
    /// TABAK DONGUSU.
    ///
    /// Lokantada sayili tabak var ve doniyor:
    ///
    ///   temiz -> (asci tabakliyor) -> kullanimda
    ///   kullanimda -> (garson masayi topluyor) -> kirli
    ///   kirli -> (lavaboda yikaniyor) -> temiz
    ///
    /// docs/14 bulasikciyi bir DARBOGAZ olarak tarif ediyor: "tabak
    /// biterse servis durur - gorunmeyen ama tikaninca fark edilen".
    /// Bugune kadar o darbogaz salon kapasitesinin icine gomuluydu.
    /// </summary>
    public sealed class PlateTests
    {
        private readonly ITestOutputHelper _out;
        public PlateTests(ITestOutputHelper o) { _out = o; }

        private const ulong Seed = 20260912UL;

        private static EconomyConfig Economy() => ContentLoader.LoadEconomy(Paths.Content);
        private static ContentSet Content() => ContentSetLoader.Load(Paths.Content, "fastfood");

        private static TimingConfig Timing()
        {
            ContentSet c = Content();
            return c.SlotDurationsBp != null
                ? TimingConfig.Default().WithSlotDurations(c.SlotDurationsBp).WithEatMs(c.EatMs)
                : TimingConfig.Default();
        }

        private static Simulation NewSim(int salon = 1, int dishwashers = 0)
        {
            Simulation sim = new Simulation(Economy(), Content(), Timing(), Seed);
            for (int i = 1; i < salon; i++)
                sim.Apply(new Command(0, CommandKind.Hire, 1));
            if (dishwashers > 0)
                sim.Apply(new Command(0, CommandKind.SetDishwashers, dishwashers));
            return sim;
        }

        /// <summary>
        /// SAGLIKLI BUYUYEN bir sabah.
        ///
        /// Eski yardimci "kasa 400.000'in ustundeyse UC kademeyi birden
        /// dene, sonra uc kisi ise al" diyordu ve dukkan bes gunde
        /// batiyordu: 6-40. gunler arasi SIFIR grup servis ediliyordu.
        /// Yani "buyuyen lokantanin yogun gunleri" diye adlandirilan
        /// olcum, olu bir dukkani olcuyordu.
        ///
        /// Kural artik gercek oyuncununki: bir kademe, ve ancak bedelin
        /// UC KATI kasada varsa; kadro da yarinin talebine gore.
        /// </summary>
        private static void GrowSanely(Simulation sim)
        {
            for (int tier = 1; tier < 8; tier++)
            {
                long bedel = sim.UpgradeCostFor(tier);
                if (bedel <= 0) continue;
                if (sim.Cash < bedel * 3) break;
                sim.Apply(new Command(sim.TickIndex, CommandKind.Expand, tier));
                break;
            }

            Crew need = sim.RequiredCrewTomorrow();
            while (sim.Cooks < need.Cooks && sim.Cooks + sim.SalonStaff < sim.StaffCap)
                sim.Apply(new Command(sim.TickIndex, CommandKind.Hire, 0));
            while (sim.SalonStaff < need.Salon && sim.Cooks + sim.SalonStaff < sim.StaffCap)
                sim.Apply(new Command(sim.TickIndex, CommandKind.Hire, 1));
        }

        private static DayReport RunOneDay(Simulation sim)
        {
            sim.Apply(new Command(sim.TickIndex, CommandKind.OpenService));
            int limit = Timing().ServiceTicks + 4000;
            for (int t = 0; t < limit; t++)
            {
                sim.Tick();
                if (sim.ServiceComplete) break;
            }
            sim.Apply(new Command(sim.TickIndex, CommandKind.CloseDay));
            return sim.BuildDayReport();
        }

        // ====================================================================
        /// <summary>
        /// TABAK KAYBOLMUYOR, COGALMIYOR.
        ///
        /// Bu degismez butun mekanigin temeli: sizan bir tabak, servisi
        /// gun gun yavaslatan ve sebebi hicbir yerde gorunmeyen bir hata
        /// olur. Her tick'te sinaniyor, gun sonunda degil - ara bir
        /// durumda bozulup sonunda toparlanan bir sayac, gun sonu
        /// kontrolunden gecerdi.
        /// </summary>
        [Fact]
        public void Tabak_sayisi_korunuyor()
        {
            Simulation sim = NewSim(salon: 2);
            int toplam = sim.PlatesTotal;
            Assert.True(toplam > 0, "kademe tabaksiz");

            sim.Apply(new Command(sim.TickIndex, CommandKind.OpenService));
            int limit = Timing().ServiceTicks + 4000;
            int enAzTemiz = int.MaxValue, enCokKirli = 0;

            for (int t = 0; t < limit; t++)
            {
                sim.Tick();

                int sum = sim.PlatesClean + sim.PlatesInUse + sim.PlatesDirty;
                Assert.True(sum == toplam,
                    $"tick {t}: temiz {sim.PlatesClean} + kullanimda {sim.PlatesInUse}"
                    + $" + kirli {sim.PlatesDirty} = {sum}, beklenen {toplam}");
                Assert.True(sim.PlatesClean >= 0 && sim.PlatesInUse >= 0
                            && sim.PlatesDirty >= 0, $"tick {t}: eksi tabak");

                if (sim.PlatesClean < enAzTemiz) enAzTemiz = sim.PlatesClean;
                if (sim.PlatesDirty > enCokKirli) enCokKirli = sim.PlatesDirty;
                if (sim.ServiceComplete) break;
            }

            _out.WriteLine($"toplam {toplam} tabak | en az temiz {enAzTemiz}"
                           + $" | en cok kirli {enCokKirli}"
                           + $" | yikanan {sim.PlatesWashedToday}"
                           + $" | tabaksiz bekleme {sim.PlateBlockedTicks} tick");

            // Dongu GERCEKTEN donuyor: bir seyler kirlendi ve yikandi.
            Assert.True(enCokKirli > 0, "hic tabak kirlenmedi - dongu islemiyor");
            Assert.True(sim.PlatesWashedToday > 0, "hic tabak yikanmadi");
        }

        /// <summary>
        /// BULASIKCI ALINCA HERKES KENDI ISINI YAPAR.
        ///
        /// Kullanicinin cumlesi bu. Olculebilir hali: lavaboya adanmis
        /// biri varken salon personeli isini birakip bulasiga
        /// KOSMUYOR, yani masa toplama ve servis aksamiyor.
        ///
        /// ESKI HALI HICBIR SEY OLCMUYORDU. Dort masalik sakin bir
        /// dukkanda iki kosu BIREBIR ayniydi (tabaksiz bekleme 0/0,
        /// yikanan 15/15, servis 7/7) ve tek iddia "0 &lt;= 0" idi:
        /// bulasikci mekanigi tamamen silinse test yine yesil kalirdi.
        ///
        /// Simdi once BASKI kuruluyor - yirmi bes gun buyuyen bir
        /// dukkan - ve baskinin gercekten olustugu ON KOSUL olarak
        /// iddia ediliyor. Baski yoksa test kaliyor, "olculemedi" diye
        /// sessizce gecmiyor.
        ///
        /// Kadro ESIT: iki kosuda da ayni sayida salon calisani. Aksi
        /// halde olculen sey bulasikci degil, fazladan bir kisi olur.
        /// </summary>
        [Fact]
        public void Bulasikci_salonu_lavabodan_kurtariyor()
        {
            const int Gun = 25;
            Simulation yok = new Simulation(Economy(), Content(), Timing(), Seed);
            Simulation var = new Simulation(Economy(), Content(), Timing(), Seed);

            int yokYikama = 0, varYikama = 0;
            int yokBekleme = 0, varBekleme = 0;
            int yokServis = 0, varServis = 0;
            int yokKirliZirve = 0, varKirliZirve = 0;

            for (int gun = 0; gun < Gun; gun++)
            {
                GrowSanely(yok);
                GrowSanely(var);

                // BULASIKCI HER SABAH YENIDEN: kadro degistikce tavan
                // degisiyor ve SetDishwashers tavana kirpiyor.
                if (var.SalonStaff >= 2)
                    var.Apply(new Command(var.TickIndex, CommandKind.SetDishwashers, 1));

                // STOK TAZELENMEDEN dukkan buyuyemez: RunOneDay
                // yalnizca servisi aciyor, sabah alisverisini yapmiyor.
                yok.Apply(new Command(yok.TickIndex, CommandKind.OrderRecommended));
                var.Apply(new Command(var.TickIndex, CommandKind.OrderRecommended));

                Assert.True(yok.SalonStaff == var.SalonStaff,
                    $"gun {gun}: kadro ayrildi ({yok.SalonStaff} / {var.SalonStaff}) - "
                    + "olculen sey bulasikci degil, fazladan bir kisi olur");

                DayReport a = RunOneDay(yok);
                DayReport b = RunOneDay(var);

                yokYikama += yok.SalonRushWashes;
                varYikama += var.SalonRushWashes;
                yokBekleme += yok.PlateBlockedTicks;
                varBekleme += var.PlateBlockedTicks;
                yokServis += a.ServedParties;
                varServis += b.ServedParties;
                if (yok.PlatesDirty > yokKirliZirve) yokKirliZirve = yok.PlatesDirty;
                if (var.PlatesDirty > varKirliZirve) varKirliZirve = var.PlatesDirty;

                yok.AdvanceToNextDay();
                var.AdvanceToNextDay();
            }

            _out.WriteLine($"bulasikcisiz: salon acil lavaboda {yokYikama} kez, "
                           + $"tabaksiz bekleme {yokBekleme}, servis {yokServis} grup, "
                           + $"{yok.TableCount} masa");
            _out.WriteLine($"bulasikcili : salon acil lavaboda {varYikama} kez, "
                           + $"tabaksiz bekleme {varBekleme}, servis {varServis} grup, "
                           + $"{var.TableCount} masa");

            // ON KOSUL: baski gercekten olustu mu. Bu satir olmadan test
            // "hicbir sey olmadi" durumunda da yesil kalirdi.
            Assert.True(yokKirliZirve > 0,
                "hic tabak kirlenmedi - baski kurulamadi, karsilastirma anlamsiz");
            Assert.True(yokYikama > 0,
                $"bulasikcisiz kosuda salon lavaboya hic kosmadi ({yokYikama} kez) - "
                + "baski kurulamadi, bulasikcinin farki olculemez");

            // ASIL IDDIA: bulasikci varken salon acil lavabo kosusunu
            // BIRAKIYOR. "Sifir" degil yarisindan az, cunku ilk gunler
            // salon kadrosu iki kisiye ulasmadan bulasikci atanamiyor -
            // SetDishwashers en az bir kisiyi sahada tutuyor.
            // Olculdu: 31 kosuya karsi 3.
            Assert.True(varYikama * 2 < yokYikama,
                $"bulasikci salonu lavabodan kurtarmadi: {varYikama} kez / "
                + $"{yokYikama} kez");

            // Ve bu bedava degil: tabak yine yikaniyor, servis
            // aksamiyor.
            Assert.True(varBekleme <= yokBekleme,
                $"bulasikci tabaksiz beklemeyi artirdi: {varBekleme} > {yokBekleme}");
        }

        /// <summary>
        /// KUCULEN DUKKAN TABAK DA KAYBEDIYOR.
        ///
        /// Expand fark kadar TEMIZ tabak ekliyordu, Downsize hicbir sey
        /// cikarmiyordu: kucullme gununde
        /// "temiz + kullanimda + kirli > PlatesTotal" oluyordu ve
        /// WashNeeded'in esikleri kucullmus toplama gore hesaplandigi
        /// icin bulasik nobeti yanlis zamanda tetikleniyordu.
        ///
        /// Hata KENDINI GIZLIYORDU: gece AdvanceToNextDay tabaklari
        /// kademeye yeniden yaziyor, yani degismez yalnizca O GUN
        /// kirikti - ve "Tabak_sayisi_korunuyor" hic kucullme
        /// kosmadigi icin gormuyordu.
        /// </summary>
        [Fact]
        public void Kuculen_dukkan_tabak_da_kaybediyor()
        {
            Simulation sim = new Simulation(Economy(), Content(), Timing(), Seed);

            // Once BUYU: kucullecek bir kademe olmali.
            for (int tier = 1; tier < 8; tier++)
            {
                long bedel = sim.UpgradeCostFor(tier);
                if (bedel <= 0 || sim.Cash < bedel) break;
                sim.Apply(new Command(sim.TickIndex, CommandKind.Expand, tier));
            }
            Assert.True(sim.TableCount > 4, $"dukkan buyumedi ({sim.TableCount} masa) - "
                                            + "kucullme sinanamaz");

            int buyukToplam = sim.PlatesTotal;
            Assert.Equal(buyukToplam,
                         sim.PlatesClean + sim.PlatesInUse + sim.PlatesDirty);

            // KUCULT: kasayi eksiye dusurup batma merdivenini cagiriyoruz.
            // Merdiven once ekipman satiyor, sonra kuculuyor.
            int guard = 0;
            int oncekiMasa = sim.TableCount;
            while (sim.TableCount == oncekiMasa && guard++ < 60)
            {
                sim.Apply(new Command(sim.TickIndex, CommandKind.OrderRecommended));
                sim.Apply(new Command(sim.TickIndex, CommandKind.OpenService));
                sim.Apply(new Command(sim.TickIndex, CommandKind.CloseDay));
                sim.AdvanceToNextDay();
            }
            Assert.True(sim.TableCount < oncekiMasa,
                $"dukkan {guard} gunde kuculmedi ({sim.TableCount} masa)");

            // ASIL IDDIA: degismez kucullmeden HEMEN SONRA da tutuyor.
            int sum = sim.PlatesClean + sim.PlatesInUse + sim.PlatesDirty;
            _out.WriteLine($"{oncekiMasa} -> {sim.TableCount} masa | toplam {sim.PlatesTotal}"
                           + $" | temiz {sim.PlatesClean} kullanimda {sim.PlatesInUse}"
                           + $" kirli {sim.PlatesDirty}");
            Assert.True(sum <= sim.PlatesTotal,
                $"kucullmeden sonra tabak fazla: {sum} > {sim.PlatesTotal}");
        }

        /// <summary>
        /// DARBOGAZ ZIRVEDE GERCEKTEN ISIRIYOR MU - TANI.
        ///
        /// Birinci gun hicbir sey olmuyor (yedi grup, yirmi dort tabak).
        /// Bir mekanigin "var" olmasi yetmez, bir yerde HISSEDILMESI
        /// gerekir; bu test buyumus bir lokantanin yogun gunlerini kosup
        /// tabak basincini basiyor.
        ///
        /// Iddia etmiyor, OLCUYOR: sayilar tabak sayisini ayarlamak icin.
        /// </summary>
        [Fact]
        public void Tabak_basinci_zirvede_olculuyor()
        {
            Simulation sim = new Simulation(Economy(), Content(), Timing(), Seed);

            int enCokBekleme = 0, enCokKirli = 0, enAzTemiz = int.MaxValue;
            int toplamBekleme = 0;
            int enCokDoluMasa = 0;
            int sonOnGun = 0;

            for (int gun = 0; gun < 40; gun++)
            {
                // BUYUYEN BIR LOKANTA: darbogaz ancak DOLULUK zirvesinde
                // anlam kazaniyor ve dort masalik sakin bir dukkanda oyle
                // bir zirve yok. Her sabah gucu yettigince buyuyup ise
                // aliyoruz - "planci" botunun kaba hali.
                // Rahat para varken buyu. Ilk yazim her sabah butun
                // kademeleri deniyordu ve dukkan onuncu gunde iflas etti -
                // olculen sey zirve degil, colun kendisiydi.
                GrowSanely(sim);

                sim.Apply(new Command(sim.TickIndex, CommandKind.OrderRecommended));
                sim.Apply(new Command(sim.TickIndex, CommandKind.OpenService));
                int limit = Timing().ServiceTicks + 4000;
                for (int t = 0; t < limit; t++)
                {
                    sim.Tick();
                    if (sim.PlatesDirty > enCokKirli) enCokKirli = sim.PlatesDirty;
                    if (sim.PlatesClean < enAzTemiz) enAzTemiz = sim.PlatesClean;
                    if (sim.OccupiedTables > enCokDoluMasa) enCokDoluMasa = sim.OccupiedTables;
                    if (sim.ServiceComplete) break;
                }
                sim.Apply(new Command(sim.TickIndex, CommandKind.CloseDay));
                DayReport r = sim.BuildDayReport();
                if (gun >= 30) sonOnGun += r.ServedParties;
                if (sim.PlateBlockedTicks > enCokBekleme) enCokBekleme = sim.PlateBlockedTicks;
                toplamBekleme += sim.PlateBlockedTicks;
                if (gun % 5 == 4 || sim.PlateBlockedTicks > 0)
                    _out.WriteLine($"gun {sim.Day}: {r.ServedParties} grup, {sim.TableCount} masa, "
                                   + $"temiz-az {sim.PlatesClean}, kirli {sim.PlatesDirty}, "
                                   + $"tabaksiz bekleme {sim.PlateBlockedTicks} tick");
                sim.AdvanceToNextDay();
            }

            _out.WriteLine($"TANI toplam {sim.PlatesTotal} tabak | en az temiz {enAzTemiz}"
                           + $" | en cok kirli {enCokKirli}"
                           + $" | en cok dolu masa {enCokDoluMasa}/{sim.TableCount}"
                           + $" | en kotu gun {enCokBekleme} tick"
                           + $" | 40 gun toplami {toplamBekleme} tick");

            // CANLILIK ON KOSULU.
            //
            // "enCokKirli > 0" iddiasini 1-5. gunler tek basina
            // dolduruyordu; dukkan 6. gunden sonra olse de test yesil
            // kaliyordu. Bu satir olcumun OLCTUGU SEYI sinaniyor: son on
            // gun hala musteri agirlaniyor mu.
            Assert.True(sonOnGun > 0,
                $"son on gunde hic grup agirlanmadi - olculen sey zirve degil, "
                + $"olu bir dukkan (buyume mantigi ya da batma merdiveni bozuk)");
            Assert.True(enCokDoluMasa > 4,
                $"dukkan hic buyumedi (en cok dolu masa {enCokDoluMasa}) - "
                + "zirve olcumu icin dort masa yetmez");
            Assert.True(enCokKirli > 0, "hic tabak kirlenmedi");
        }
    }
}
