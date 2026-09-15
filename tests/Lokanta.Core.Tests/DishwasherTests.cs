using Lokanta.Content;
using Lokanta.Core.Content;
using Lokanta.Core.Economy;
using Lokanta.Core.Sim;
using Xunit;
using Xunit.Abstractions;

namespace Lokanta.Core.Tests
{
    /// <summary>
    /// ADANMIS BULASIKCI: bir salon calisanini lavaboya ayirmak.
    ///
    /// Mekanik arayuzde ve cekirdekte vardi ama hicbir denge botu
    /// kullanmiyordu (docs/49). Harness'ta olculmeye calisildi ve olcum
    /// KIRLI cikti: bulasikci ayiran kol daha kucuk bir dukkanla
    /// bitiyordu, yani kasadaki fark bulasiktan mi buyume farkindan mi
    /// geldigi ayirt edilemiyordu.
    ///
    /// Burasi o karisikligi kaldiriyor: IKI KOL BIREBIR AYNI oyuncuyu
    /// kosuyor, tek fark `SetDishwashers`. Ayni tohum, ayni kararlar,
    /// ayni gunler - degisen tek sey olculen sey.
    ///
    /// (Ayrica bir zorunluluk: Smart App Control bu makinede harness
    /// ikilisini engelliyor ve kapatilmasi yasak.)
    /// </summary>
    public sealed class DishwasherTests
    {
        private readonly ITestOutputHelper _out;
        public DishwasherTests(ITestOutputHelper o) { _out = o; }

        private const ulong Seed = 20260915UL;

        private static EconomyConfig Economy() => ContentLoader.LoadEconomy(Paths.Content);
        private static ContentSet Content() => ContentSetLoader.Load(Paths.Content, "turk");

        private static TimingConfig Timing()
        {
            ContentSet c = Content();
            return c.SlotDurationsBp != null
                ? TimingConfig.Default().WithSlotDurations(c.SlotDurationsBp).WithEatMs(c.EatMs)
                : TimingConfig.Default();
        }

        /// <summary>Bir kampanyanin sonucu.</summary>
        private readonly struct Sonuc
        {
            public readonly long Cash;
            public readonly int Served;
            public readonly int StallTicks;     // temiz tabak SIFIR olan tik
            public readonly int MaxDirty;
            public readonly int Tables;

            public Sonuc(long cash, int served, int stall, int maxDirty, int tables)
            {
                Cash = cash; Served = served; StallTicks = stall;
                MaxDirty = maxDirty; Tables = tables;
            }
        }

        /// <summary>
        /// Altmis gunluk kampanya. `dishwashers` sifirdan buyukse her
        /// sabah o kadar kisi lavaboya adaniyor.
        /// </summary>
        private Sonuc Kosu(int dishwashers)
        {
            Simulation sim = new Simulation(Economy(), Content(), Timing(), Seed);
            int stall = 0, maxDirty = 0, served = 0;

            // MENU DARALTILIYOR - bu olmadan olcum hic kosmuyor.
            //
            // Oyun BUTUN yemekler acik basliyor ve otuz iki yemeklik bir
            // menu gunde on uc musterisi olan dukkani batiriyor
            // (menudeki her yemek icin stok tutuluyor, bozulan gece
            // gidiyor). Iki denemede de bunu atladim: dukkan altmisinci
            // gunu 1 sikke ve 252 grupla bitirdi, tabak darbogazi HIC
            // olusmadi ve iki kol bayt bayt ayni cikti.
            //
            // Hangi yemeklerin kalacagi UYDURULMUYOR: simulasyonun kendi
            // `WouldPick` olcusu soruluyor - "bu arketip bu rolden hangi
            // yemegi secerdi". Yani menude kalanlar gercekten siparis
            // edilen yemekler.
            bool[] tut = new bool[sim.DishCount];
            for (int a = 0; a < 5; a++)
                for (int role = 0; role < 4; role++)
                {
                    int d = sim.WouldPick(a, role);
                    if (d >= 0) tut[d] = true;
                }
            for (int d = 0; d < sim.DishCount; d++)
                if (!tut[d] && sim.IsOnMenu(d))
                    sim.Apply(new Command(sim.TickIndex, CommandKind.SetMenuSlot, d, 0));

            for (int gun = 1; gun <= sim.CampaignDays; gun++)
            {
                // --- sabah: stok, kadro, genisleme ---
                for (int i = 0; i < sim.IngredientCount; i++)
                {
                    int need = sim.RecommendedRestock(i);
                    if (need > 0)
                        sim.Apply(new Command(sim.TickIndex,
                                              CommandKind.OrderIngredient, i, need));
                }

                // GENISLEME YOK - bilerek.
                //
                // Ilk halinde vardi ve bu olcum oyuncusu (menuyu
                // daraltmayi bilmiyor) genisleyince batiyordu: altmisinci
                // gun dort masa, 537 kasa. Iki kol da ayni sekilde
                // battigi icin sonuc BAYT BAYT ayni cikti ve esik hic
                // tetiklenmedi.
                //
                // Gerek de yok: kademe 0'da kadro tavani 3, yani bir asci
                // + IKI salon mumkun. Birini lavaboya adamak icin bu
                // yetiyor ve dukkan ayakta kaliyor.

                Crew gereken = sim.RequiredCrewTomorrow();
                for (int guard = 0; guard < 12 && sim.Cooks < gereken.Cooks
                                    && sim.Cooks + sim.SalonStaff < sim.StaffCap; guard++)
                {
                    int once = sim.Cooks;
                    sim.Apply(new Command(sim.TickIndex, CommandKind.Hire, 0, 0));
                    if (sim.Cooks == once) break;
                }
                for (int guard = 0; guard < 12 && sim.SalonStaff < gereken.Salon
                                    && sim.Cooks + sim.SalonStaff < sim.StaffCap; guard++)
                {
                    int once = sim.SalonStaff;
                    sim.Apply(new Command(sim.TickIndex, CommandKind.Hire, 1, 0));
                    if (sim.SalonStaff == once) break;
                }

                // TEK FARK BURASI.
                int hedef = dishwashers > 0 && sim.SalonStaff >= 2 ? dishwashers : 0;
                if (sim.Dishwashers != hedef)
                    sim.Apply(new Command(sim.TickIndex, CommandKind.SetDishwashers, hedef));

                // --- servis ---
                sim.Apply(new Command(sim.TickIndex, CommandKind.OpenService));
                for (int t = 0; t < 5000; t++)
                {
                    sim.Tick();
                    if (sim.PlatesClean == 0) stall++;
                    if (sim.PlatesDirty > maxDirty) maxDirty = sim.PlatesDirty;
                    if (sim.ServiceComplete) break;
                }
                sim.Apply(new Command(sim.TickIndex, CommandKind.CloseDay));
                served += sim.BuildDayReport().ServedParties;
                sim.AdvanceToNextDay();
            }

            return new Sonuc(sim.Cash, served, stall, maxDirty, sim.TableCount);
        }

        /// <summary>
        /// UZMAN GERCEKTEN DAHA HIZLI YIKIYOR.
        ///
        /// Kullanicinin cumlesi: "bulasikcinin yikama hizinin digerlerine
        /// gore cok daha fazla olmasi lazim, cunku o isi yapan kisi o."
        /// Icerik de bunu zaten soyluyordu - staff-roles.json'da
        /// bulasikci rolunun gunluk kapasitesi 48, garsonunki 26 - ama
        /// simulasyon farki HIC kullanmiyordu: adanmis bulasikci da,
        /// imdada kosan garson da ayni WashMs ile yikiyordu.
        ///
        /// Bu test o baglantiyi tutuyor. Carpan 10000'e (fark yok)
        /// donerse kirilir.
        /// </summary>
        [Fact]
        public void Uzman_daha_hizli_yikiyor()
        {
            TimingConfig t = Timing();
            _out.WriteLine($"garson {t.WashMs} ms, bulasikci {t.DishwasherWashMs} ms "
                           + $"(carpan {t.DishwasherSpeedBp} bp)");

            Assert.True(t.WashMs > 0, "yikama suresi sifir - olcum kosmamis");
            Assert.True(t.DishwasherWashMs < t.WashMs,
                $"adanmis bulasikci daha hizli degil ({t.DishwasherWashMs} >= {t.WashMs})");
        }

        // KAMPANYA KARSILASTIRMASI BURADA DEGIL, HARNESS'TA.
        //
        // Denedim ve KOSMADI: tabak darbogazi bir BUYUK DUKKAN olgusu
        // (harness'ta planci 12 masada 263 tik veriyor). Test projesinde
        // yasayabilir bir buyuk oyuncu kurmak, harness'in stratejilerini
        // bastan yazmak demek - ve uc denememde de dukkan dort masada
        // kalip battigi icin iki kol BAYT BAYT ayni cikti, tabaksiz tik
        // sifir oldu.
        //
        // Yazdigim canlilik satiri ("temiz tabak hic bitmedi - olcum
        // kosmamis") ucunu de yakaladi; o satir olmasaydi test yesil
        // yanip hicbir sey olcmeyecekti.
        //
        // Denge karsilastirmasinin yeri harness, kablolamanin yeri burasi.
    }
}
