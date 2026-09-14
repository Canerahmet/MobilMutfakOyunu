using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using Lokanta.Content;
using Lokanta.Core.Content;
using Lokanta.Core.Economy;
using Lokanta.Core.Sim;
using Xunit;
using Xunit.Abstractions;

namespace Lokanta.Core.Tests
{
    public class SimulationTests
    {
        private readonly ITestOutputHelper _out;
        public SimulationTests(ITestOutputHelper output) { _out = output; }

        private const ulong Seed = 20260910UL;

        private static EconomyConfig Economy() => ContentLoader.LoadEconomy(Paths.Content);
        private static ContentSet Content() => ContentSetLoader.Load(Paths.Content, "fastfood");

        private static TimingConfig Timing()
        {
            ContentSet c = Content();
            return c.SlotDurationsBp != null
                ? TimingConfig.Default().WithSlotDurations(c.SlotDurationsBp).WithEatMs(c.EatMs)
                : TimingConfig.Default();
        }

        /// <summary>
        /// TOPLAM kadroyu kurar, EKLENECEK sayiyi degil.
        ///
        /// Oyun bir asci VE bir garsonla basliyor (devralinan kadro), o
        /// yuzden ikisi de birden sayiliyor: salon:2 istemek BIR kisi ise
        /// almak demek. Once yalnizca asci boyle sayiliyordu ve garson
        /// eklenince "salon: 2" sessizce UC kisi oldu.
        ///
        /// Taban da bir: baslangic kadrosunun altina inilemiyor.
        /// </summary>
        private static Simulation NewSim(int cooks = 1, int salon = 1)
        {
            Simulation sim = new Simulation(Economy(), Content(), Timing(), Seed);
            for (int i = 1; i < cooks; i++) sim.Apply(new Command(0, CommandKind.Hire, 0));
            for (int i = 1; i < salon; i++) sim.Apply(new Command(0, CommandKind.Hire, 1));
            return sim;
        }

        /// <summary>Bir servis gununu bastan sona kosar ve raporu doner.</summary>
        private static DayReport RunOneDay(Simulation sim, int extraTicks = 4000)
        {
            sim.Apply(new Command(sim.TickIndex, CommandKind.OpenService));
            int limit = Timing().ServiceTicks + extraTicks;
            for (int t = 0; t < limit; t++)
            {
                sim.Tick();
                if (sim.ServiceComplete) break;
            }
            sim.Apply(new Command(sim.TickIndex, CommandKind.CloseDay));
            return sim.BuildDayReport();
        }

        // ====================================================================
        [Fact]
        public void Icerik_yuklenir_ve_dogrulanir()
        {
            ContentSet c = Content();
            Assert.Equal("fastfood", c.Cuisine);
            Assert.Equal(32, c.Dishes.Length);
            Assert.Equal(20, c.Archetypes.Length);      // 8 paylasilan + 12 mutfaga ozel
            Assert.True(c.Ingredients.Length > 30);

            // Her yemegin malzeme maliyeti hesaplanmis ve fiyatin altinda
            foreach (DishDef d in c.Dishes)
            {
                Assert.True(d.IngredientCost > 0, d.Id + " maliyeti sifir");
                Assert.True(d.IngredientCost < d.Price,
                    d.Id + " maliyeti fiyattan yuksek: " + d.IngredientCost + " / " + d.Price);
            }
        }

        [Fact]
        public void Malzeme_maliyeti_yuzde_otuz_iki_civarinda()
        {
            ContentSet c = Content();
            int min = int.MaxValue, max = 0;
            foreach (DishDef d in c.Dishes)
            {
                int bp = (int)Fx.MulDiv(d.IngredientCost, Fx.One, d.Price);
                if (bp < min) min = bp;
                if (bp > max) max = bp;
            }
            _out.WriteLine($"malzeme orani: {min} - {max} bp");
            Assert.InRange(min, 2500, 3600);
            Assert.InRange(max, 2500, 3800);
        }

        [Fact]
        public void Bir_gun_bastan_sona_koseuyor()
        {
            Simulation sim = NewSim();
            Assert.Equal(DayPhase.Morning, sim.Phase);

            DayReport r = RunOneDay(sim);

            _out.WriteLine($"planlanan grup {r.PlannedParties}, servis {r.ServedParties}, " +
                           $"kizgin {r.AngryParties}, kisi {r.ServedPeople}, " +
                           $"ciro {r.Revenue}, memnuniyet {r.AverageSatisfactionCenti}");

            Assert.Equal(DayPhase.Evening, sim.Phase);
            Assert.True(r.PlannedParties > 0, "hic musteri planlanmadi");
            Assert.Equal(r.PlannedParties, r.ServedParties + r.AngryParties);
            Assert.Equal(0, sim.ActiveParties);
        }

        [Fact]
        public void Gun_sonunda_hicbir_musteri_asili_kalmiyor()
        {
            Simulation sim = NewSim(cooks: 2, salon: 2);
            RunOneDay(sim);

            for (int i = 0; i < Simulation.MaxParties; i++)
                Assert.False(sim.PartyActive(i), $"grup {i} hala aktif");
        }

        [Fact]
        public void Ayni_tohum_ayni_gunu_veriyor()
        {
            DayReport a = RunOneDay(NewSim(2, 2));
            DayReport b = RunOneDay(NewSim(2, 2));

            Assert.Equal(a.PlannedParties, b.PlannedParties);
            Assert.Equal(a.ServedParties, b.ServedParties);
            Assert.Equal(a.AngryParties, b.AngryParties);
            Assert.Equal(a.Revenue, b.Revenue);
            Assert.Equal(a.AverageSatisfactionCenti, b.AverageSatisfactionCenti);
            Assert.Equal(a.ReputationCenti, b.ReputationCenti);
        }

        [Fact]
        public void Farkli_tohum_farkli_gun_veriyor()
        {
            Simulation s1 = new Simulation(Economy(), Content(), Timing(), 1UL);
            Simulation s2 = new Simulation(Economy(), Content(), Timing(), 2UL);
            DayReport a = RunOneDay(s1);
            DayReport b = RunOneDay(s2);

            // Talep formulu ayni sayida KISI veriyor ama gruplar ve gelis
            // zamanlari farkli olmali.
            Assert.True(a.Revenue != b.Revenue || a.PlannedParties != b.PlannedParties,
                "iki farkli tohum ayni gunu uretti");
        }

        [Fact]
        public void Kare_basina_tick_sayisi_sonucu_degistirmiyor()
        {
            // docs/23 1.4 kare bagimsizlik testi. Cekirdek gercek zamani
            // gormedigi icin surucunun kac tick cagirdigi onemsiz olmali.
            Simulation a = NewSim(2, 2);
            Simulation b = NewSim(2, 2);

            a.Apply(new Command(0, CommandKind.OpenService));
            b.Apply(new Command(0, CommandKind.OpenService));

            int limit = Timing().ServiceTicks + 4000;
            for (int t = 0; t < limit; t++) { a.Tick(); if (a.ServiceComplete) break; }

            // b'yi bes'erli partiler halinde ilerlet
            int done = 0;
            while (done < limit && !b.ServiceComplete)
            {
                for (int k = 0; k < 5 && done < limit; k++, done++)
                {
                    b.Tick();
                    if (b.ServiceComplete) break;
                }
            }

            a.Apply(new Command(a.TickIndex, CommandKind.CloseDay));
            b.Apply(new Command(b.TickIndex, CommandKind.CloseDay));

            DayReport ra = a.BuildDayReport(), rb = b.BuildDayReport();
            Assert.Equal(ra.ServedParties, rb.ServedParties);
            Assert.Equal(ra.Revenue, rb.Revenue);
            Assert.Equal(ra.ReputationCenti, rb.ReputationCenti);
        }

        [Fact]
        public void Yeterli_kadro_musterilerin_cogunu_agirliyor()
        {
            Simulation sim = NewSim(cooks: 2, salon: 2);
            DayReport r = RunOneDay(sim);

            _out.WriteLine($"servis {r.ServedParties} / {r.PlannedParties}, " +
                           $"kizgin {r.AngryParties}");

            Assert.True(r.ServedParties > 0, "hicbir musteri agirlanmadi");
            // Ilk gun kucuk restoran; en az yarisi agirlanmali
            Assert.True(r.ServedParties * 2 >= r.PlannedParties,
                $"cogunluk kaybedildi: {r.ServedParties}/{r.PlannedParties}");
        }

        [Fact]
        public void Kadrosuz_restoran_musteri_kaybediyor()
        {
            // Tek asci, salonda sadece patron: yogun bir gunde kayip olmali.
            Simulation weak = NewSim(cooks: 1, salon: 1);
            Simulation strong = NewSim(cooks: 3, salon: 4);

            DayReport w = RunOneDay(weak);
            DayReport s = RunOneDay(strong);

            _out.WriteLine($"zayif kadro: {w.ServedParties} servis, {w.AngryParties} kizgin");
            _out.WriteLine($"guclu kadro: {s.ServedParties} servis, {s.AngryParties} kizgin");

            Assert.True(s.ServedParties >= w.ServedParties,
                "kadro artinca servis edilen musteri azaldi");
            Assert.True(s.AngryParties <= w.AngryParties,
                "kadro artinca kizgin musteri artti");
        }

        [Fact]
        public void Kizgin_musteri_itibar_kazancini_dusuruyor()
        {
            // Bir kizgin musteri, bes memnun musterinin kazancini SILMEK
            // zorunda degil; ilk hali bunu iddia ediyordu ve yanlisti.
            // Dogru iddia: kayip yasayan gun, yasamayan gunden az kazandirir.
            //
            // Ikinci duzeltme (istasyon yuvalari yazilinca): test
            // "cooks: 3, salon: 4" istiyordu ama dort masada kadro tavani
            // UC. Dort salon isesi sessizce reddediliyordu ve "guclu kadro"
            // aslinda yalnizca fazladan iki ASCI demekti. Fazladan asci ise
            // tek yuvali istasyonda ise yaramiyor, hatta zarar veriyor:
            // erken baslayan acelesi olmayan is yuvayi tutuyor ve sonradan
            // gelen aceleci musteri sirada bekliyor.
            //
            // Guclu kadro artik SALON kadrosu: masaya oturtma ve servis
            // hizlaniyor, kayip dusuyor. Tavan da acikca dogrulaniyor.
            // Zayif kadro = DEVRALINAN kadro (bir asci, bir garson);
            // guclu kadro onun ustune iki garson daha.
            // Guclu kadro devralinanin ustune BIR garson: birinci kademe
            // kadro tavani UC ve oyun ikiyle basliyor, yani bu kademede
            // tek bir ise alim siginyor. Tavanin ustundeki komut sessizce
            // reddedilir ve test "guclu kadro" kurdugunu sanip devraldigi
            // kadroyu olcerdi.
            Simulation weak = NewSim(cooks: 1, salon: 1);
            Simulation strong = NewSim(cooks: 1, salon: 2);

            Assert.Equal(1, weak.Cooks);
            Assert.Equal(1, weak.SalonStaff);
            Assert.Equal(1, strong.Cooks);
            Assert.Equal(2, strong.SalonStaff);

            int start = weak.ReputationCenti;
            DayReport w = RunOneDay(weak);
            DayReport s = RunOneDay(strong);

            int weakGain = w.ReputationCenti - start;
            int strongGain = s.ReputationCenti - start;
            _out.WriteLine($"zayif kadro itibar {weakGain:+#;-#;0} ({w.AngryParties} kizgin), " +
                           $"guclu kadro {strongGain:+#;-#;0} ({s.AngryParties} kizgin)");

            if (w.AngryParties > s.AngryParties)
                Assert.True(weakGain < strongGain,
                    $"daha cok musteri kaybedilen gun daha cok itibar kazandirdi: " +
                    $"{weakGain} >= {strongGain}");
        }

        [Fact]
        public void Patron_mudahalesi_gun_basina_sinirli()
        {
            // docs/02 59: "sinirli sayida patron mudahalesi hakkin var
            // (gun basina 3-5)". Icerikte interventionsPerDay yaziliydi ve
            // HICBIR SEY onu zorlamiyordu: her kizgin musteri bedava
            // kurtarilabiliyordu, yani kriz yonetimi bir kaynak degil
            // sinirsiz bir dugmeydi.
            Simulation sim = NewSim(cooks: 2, salon: 2);
            EconomyConfig eco = Economy();

            Assert.Equal(eco.InterventionsPerDay, sim.InterventionsLeft);

            sim.Apply(new Command(sim.TickIndex, CommandKind.OpenService));
            for (int t = 0; t < 2000; t++) sim.Tick();

            // AYNI masaya tekrar tekrar mudahale ediliyor. MostImpatientParty
            // mudahale gormus masayi eliyor, yani onunla olcmeye calismak
            // "yeterli musteri yok" yuzunden erken bitiyordu; olculmek
            // istenen sey musteri sayisi degil HAK sayisi.
            int party = sim.MostImpatientParty();
            Assert.True(party >= 0, "olculecek musteri yok");

            int used = 0;
            for (int i = 0; i < eco.InterventionsPerDay + 3; i++)
            {
                int before = sim.InterventionsLeft;
                sim.Apply(new Command(sim.TickIndex, CommandKind.Intervene,
                                      party, (int)InterventionKind.OwnerAttention));
                if (sim.InterventionsLeft < before) used++;
            }

            _out.WriteLine($"hak {eco.InterventionsPerDay}, kullanilan {used}, " +
                           $"kalan {sim.InterventionsLeft}");
            Assert.True(used <= eco.InterventionsPerDay,
                $"gun basina {eco.InterventionsPerDay} hak varken {used} mudahale gecti");
            Assert.Equal(0, sim.InterventionsLeft);
        }

        [Fact]
        public void Istasyon_acele_ettirme_isi_kisaltiyor()
        {
            // docs/02 59'un ucuncu mudahalesi: "bir istasyonu hizlandir".
            // Diger iki tur SALON tarafinda; mutfak darbogaz oldugunda
            // patronun servis sirasinda yapabilecegi hicbir sey yoktu.
            //
            // Patron PISIRMIYOR (docs/14 bunu yasakliyor); yolu aciyor,
            // yani isin kalan DUVAR SAATI kisaliyor.
            Simulation sim = NewSim(cooks: 2, salon: 2);
            sim.Apply(new Command(sim.TickIndex, CommandKind.OpenService));
            for (int t = 0; t < 2000; t++) sim.Tick();

            int station = sim.BusiestStation();
            Assert.True(station >= 0, "mesgul istasyon yok");

            int before = sim.InterventionsLeft;
            sim.Apply(new Command(sim.TickIndex, CommandKind.Intervene,
                                  station, (int)InterventionKind.RushStation));

            _out.WriteLine($"istasyon {station} acele ettirildi, hak {before} -> {sim.InterventionsLeft}");
            Assert.Equal(before - 1, sim.InterventionsLeft);
        }

        [Fact]
        public void Bos_istasyonu_acele_ettirmek_hakki_yakmiyor()
        {
            // Hak kit bir kaynak. Bos bir istasyona basmak onu harcamamali,
            // yoksa yanlis dokunus gunun butun butcesini goturur.
            Simulation sim = NewSim(cooks: 1, salon: 0);
            int before = sim.InterventionsLeft;

            // Servis hic acilmadi: hicbir istasyonda is yok.
            for (int st = 0; st < 6; st++)
                sim.Apply(new Command(sim.TickIndex, CommandKind.Intervene,
                                      st, (int)InterventionKind.RushStation));

            Assert.Equal(before, sim.InterventionsLeft);
        }

        [Fact]
        public void Cay_ikrami_bedava_degil()
        {
            // docs/12 3: "porsiyon basina 2 maliyet, bedava verilir".
            // Bedeli olmayan ikram, bedava bir memnuniyet muslugu olurdu.
            Simulation sim = NewSim(cooks: 2, salon: 2);
            sim.Apply(new Command(sim.TickIndex, CommandKind.OpenService));
            for (int t = 0; t < 2000; t++) sim.Tick();

            int party = sim.MostImpatientParty();
            Assert.True(party >= 0, "olculecek musteri yok");

            long before = sim.Cash;
            sim.Apply(new Command(sim.TickIndex, CommandKind.Intervene,
                                  party, (int)InterventionKind.FreeTea));
            long after = sim.Cash;

            _out.WriteLine($"ikram maliyeti {(before - after) / 100.0:0.00} sikke");
            Assert.True(after < before, "cay ikrami kasadan hic para dusurmedi");

            // Patron ilgisi ise para degil ZAMAN harciyor: ucretsiz olmali.
            int other = sim.MostImpatientParty();
            if (other >= 0)
            {
                long b2 = sim.Cash;
                sim.Apply(new Command(sim.TickIndex, CommandKind.Intervene,
                                      other, (int)InterventionKind.OwnerAttention));
                Assert.Equal(b2, sim.Cash);
            }
        }

        [Fact]
        public void Itibar_bir_haftada_tavana_vurmuyor()
        {
            // Denge aracinin bulgusu: sonumsuz formulle itibar 30'dan 100'e
            // dokuz gunde ciktigi icin uzun vadeli ilerleme ekseni olmaktan
            // cikiyordu. Sonumleme sonrasi bu test onu koruyor.
            Simulation sim = NewSim(cooks: 3, salon: 4);
            for (int day = 1; day <= 7; day++)
            {
                RunOneDay(sim);
                sim.AdvanceToNextDay();
            }
            _out.WriteLine($"yedi gun sonunda itibar {sim.ReputationCenti / 100.0:0.0}");
            Assert.True(sim.ReputationCenti < 8500,
                $"itibar bir haftada {sim.ReputationCenti / 100.0:0.0} oldu; cok hizli");
        }

        [Fact]
        public void Yuksek_fiyat_memnuniyeti_dusuruyor()
        {
            ContentSet c = Content();

            Simulation normal = NewSim(3, 4);
            DayReport rn = RunOneDay(normal);

            Simulation pricey = NewSim(3, 4);
            for (int i = 0; i < c.Dishes.Length; i++)
                pricey.Apply(new Command(0, CommandKind.SetPrice, i,
                                         (int)(c.Dishes[i].Price * 13 / 10)));   // %30 zam
            DayReport rp = RunOneDay(pricey);

            _out.WriteLine($"normal fiyat memnuniyet {rn.AverageSatisfactionCenti}, " +
                           $"zamli {rp.AverageSatisfactionCenti}");

            Assert.True(rp.AverageSatisfactionCenti < rn.AverageSatisfactionCenti,
                "fiyat artti ama memnuniyet dusmedi");
            Assert.True(rp.Revenue > 0);
        }

        [Fact]
        public void Ardisik_gunler_isliyor()
        {
            Simulation sim = NewSim(2, 2);
            List<int> reputations = new List<int>();

            for (int day = 1; day <= 7; day++)
            {
                DayReport r = RunOneDay(sim);
                reputations.Add(r.ReputationCenti);
                Assert.Equal(day, r.Day);
                sim.AdvanceToNextDay();
            }

            _out.WriteLine("itibar: " + string.Join(", ", reputations));
            Assert.Equal(8, sim.Day);
            Assert.Equal(DayPhase.Morning, sim.Phase);
        }

        [Fact]
        public void Hafta_sonu_hafta_icinden_kalabalik()
        {
            // 6. ve 7. gunler hafta sonu (WeekendDaysPerWeek = 2)
            Simulation sim = NewSim(3, 4);
            int weekdayPeople = 0, weekendPeople = 0;

            for (int day = 1; day <= 7; day++)
            {
                // STOK TAZELENIYOR - yoksa olculen sey hafta sonu degil
                // ACLIK oluyor.
                //
                // Once tazelenmiyordu ve test, gunun sivriltilmesiyle
                // birlikte kirildi: uzayan bekleme memnuniyeti dusurdu,
                // itibar yedi gunde 33'ten 11'e indi ve dusen talep hafta
                // sonu carpanini yuttu (6. gun 9 grup, 7. gun 4). Yani
                // test "hafta sonu kalabalik mi" diye sorarken aslinda
                // "itibar spirali carpandan hizli mi" diye soruyordu.
                for (int i = 0; i < sim.IngredientCount; i++)
                {
                    int need = sim.RecommendedRestock(i);
                    if (need > 0)
                        sim.Apply(new Command(sim.TickIndex,
                                              CommandKind.OrderIngredient, i, need));
                }

                DayReport r = RunOneDay(sim);
                // TALEBI olcuyoruz, servisi degil: stok kisiti devreye girince
                // hafta sonunun fazlasi kapidan donebiliyor ve servis edilen
                // sayi talebi yansitmiyor.
                _out.WriteLine($"gun {day}: plan {r.PlannedParties} grup, itibar {r.ReputationCenti / 100}, kizgin {r.AngrySeatedParties}");
                if (day <= 5) weekdayPeople += r.PlannedParties;
                else weekendPeople += r.PlannedParties;
                sim.AdvanceToNextDay();
            }

            int weekdayAvg = weekdayPeople / 5;
            int weekendAvg = weekendPeople / 2;
            _out.WriteLine($"hafta ici toplam {weekdayPeople} (ort {weekdayAvg}), "
                           + $"hafta sonu toplam {weekendPeople} (ort {weekendAvg})");
            Assert.True(weekendAvg > weekdayAvg,
                $"hafta sonu kalabalik degil: {weekendAvg} <= {weekdayAvg}");
        }

        [Fact]
        public void Olaylar_uretiliyor_ve_bosaltilabiliyor()
        {
            Simulation sim = NewSim(2, 2);
            RunOneDay(sim);

            SimEvent[] buffer = new SimEvent[4096];
            int n = sim.Events.Drain(buffer);
            _out.WriteLine($"olay sayisi {n}, dusen {sim.Events.Dropped}");

            Assert.True(n > 0, "hic olay uretilmedi");

            int arrived = 0, paid = 0, seated = 0;
            for (int i = 0; i < n; i++)
            {
                if (buffer[i].Kind == SimEventKind.CustomerArrived) arrived++;
                if (buffer[i].Kind == SimEventKind.CustomerPaid) paid++;
                if (buffer[i].Kind == SimEventKind.CustomerSeated) seated++;
            }
            Assert.True(arrived > 0);
            Assert.True(seated > 0);
            Assert.True(paid > 0);
            Assert.True(seated <= arrived);
        }

        [Theory]
        [InlineData("tr-TR")]
        [InlineData("en-US")]
        [InlineData("de-DE")]
        public void Kultur_simulasyonu_degistirmiyor(string cultureName)
        {
            CultureInfo previous = Thread.CurrentThread.CurrentCulture;
            try
            {
                CultureInfo culture = new CultureInfo(cultureName);
                Thread.CurrentThread.CurrentCulture = culture;
                CultureInfo.DefaultThreadCurrentCulture = culture;

                DayReport r = RunOneDay(NewSim(2, 2));
                Assert.True(r.ServedParties > 0);
                Assert.Equal(r.PlannedParties, r.ServedParties + r.AngryParties);
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = previous;
                CultureInfo.DefaultThreadCurrentCulture = null;
            }
        }

        [Fact]
        public void Kadro_tavani_asilamiyor()
        {
            Simulation sim = NewSim();
            int cap = Economy().TierAt(0).StaffCap;

            for (int i = 0; i < 20; i++) sim.Apply(new Command(0, CommandKind.Hire, 1));

            Assert.True(sim.Cooks + sim.SalonStaff <= cap,
                $"tavan {cap} asildi: {sim.Cooks + sim.SalonStaff}");
        }

        [Fact]
        public void Zaman_ayari_kapasite_modeliyle_tutarli()
        {
            // docs/27-zaman-modeli.md kurali: salon isi kapasiteden turer.
            TimingConfig t = TimingConfig.Default();
            bool ok = t.MatchesCapacity(25, tolerancePercent: 2, out int expected);
            _out.WriteLine($"salon ms/kisi: beklenen {expected}, gercek {t.SalonMsPerPerson}");
            Assert.True(ok,
                $"salon is suresi kapasite modeliyle tutmuyor: " +
                $"{t.SalonMsPerPerson} vs {expected}");
        }
    }
}
