using Lokanta.Core.Economy;
using Lokanta.Core.Sim;

namespace Lokanta.Harness
{
    /// <summary>
    /// Denge aracinin oynattigi oyuncu stratejileri.
    /// docs/12-ekonomi.md 8: aracin cevaplamasi gereken sorular bunlarla olculuyor.
    /// </summary>
    public interface IStrategy
    {
        string Name { get; }
        string Question { get; }
        void OnMorning(Simulation sim);
        void OnEvening(Simulation sim, in DayReport report);
    }

    /// <summary>
    /// Hic mudahale etmeyen oyuncu. Hal'e gitmiyor, ise almiyor, genislemiyor.
    /// Soru 1: kacinci gunde batar.
    /// </summary>
    public sealed class PassivePlayer : IStrategy
    {
        public string Name => "pasif";
        public string Question => "Hic mudahale etmeyen oyuncu kacinci gunde batar";
        public void OnMorning(Simulation sim) { }
        public void OnEvening(Simulation sim, in DayReport report) { }
    }

    /// <summary>Her sabah hal'e giden ama baska hicbir sey yapmayan oyuncu.</summary>
    /// <summary>
    /// Ekipman alma kurali. Kizgin musteriye degil KAPASITEYE bakiyor:
    /// masa sayisinin gerektirdigi kademeden geride kalan istasyon varsa
    /// ve kasa fiyatin iki katini tasiyorsa alinir.
    ///
    /// Kadro kararinda ayni hatanin yapildigi ve denge aracinin yakaladigi
    /// not: yanlis sinyalden ise alim maaslari 19.647'den 10.446'ya dusurdu.
    /// </summary>
    /// <summary>
    /// Ise alim secimi. docs/14: uc aday gorunur, oyuncu secer.
    ///
    /// Bu sinifin varlik sebebi olculdu: aday havuzu OLMADAN huy bir
    /// piyangoydu ve pahali bir kadro cekmek iyi oyuncunun itibarini
    /// 96,5'ten 87'ye indiriyordu. Secim, mekanigin eksik olan yarisiydi.
    ///
    /// Bot secimi basit ve savunulabilir: HIZ EKSI UCRET. Gercek oyuncu
    /// da bu iki sayiya bakar; hangisine agirlik verdigi ona kalmis.
    /// </summary>
    public static class Hiring
    {
        public static int Pick(Simulation sim, int pool)
        {
            int best = 0, bestScore = int.MinValue;
            for (int i = 0; i < Simulation.CandidateSlots; i++)
            {
                int score = sim.CandidateScore(pool, i);
                if (score > bestScore) { bestScore = score; best = i; }
            }
            return best;
        }

        /// <summary>
        /// Kotu bir personeli, belirgin olarak daha iyi bir adayla
        /// degistirir. Bir kez yaptiysa true doner.
        ///
        /// Bu, huy sisteminin eksik yarisiydi. Huy rastgele geliyor ve
        /// bot ona TEPKI VEREMIYORDU: suratsiz bir garson altmis gun
        /// boyunca her masaya -6 puan yaziyor, memnuniyet 9.000'den
        /// 5.000'e siziyor, dukkan dort masada kaliyor ve kosu tek bir
        /// zar atisi yuzunden kayboluyordu.
        ///
        /// Esik GENIS (3.000 puan) ve degisim BEDELLI: giden kisinin
        /// deneyimi sifirlaniyor. Yani "her gun en iyiyi ara" degil,
        /// "gercekten kotuyse degistir".
        /// </summary>
        public static bool ReplaceWorst(Simulation sim, int pool)
        {
            int count = pool == 0 ? sim.Cooks : sim.SalonStaff;
            if (count == 0) return false;

            int worst = -1, worstScore = int.MaxValue;
            for (int i = 0; i < count; i++)
            {
                int score = sim.StaffTraitScore(pool, i);
                if (score < worstScore) { worstScore = score; worst = i; }
            }
            if (worst < 0) return false;

            int cand = Pick(sim, pool);
            int candScore = sim.CandidateScore(pool, cand);
            if (candScore == int.MinValue) return false;
            // Esik 1.200. Ilk deger 3.000 idi ve huy puanlarinin toplam
            // yayilimi ~4.700; yani degistirme neredeyse hic tetiklenmiyordu
            // ve mekanik yine karar degil zar olarak kaliyordu. 1.200,
            // "suratsiz garson yerine iyi anlasan garson" farkini yakaliyor.
            if (candScore - worstScore < 1200) return false;

            // EN KOTU kisiyi cikariyor. Komut artik indis aliyor; indissiz
            // gonderildiginde her zaman sonuncu gidiyordu ve "en kotuyu
            // degistir" stratejisi aslinda "sonuncuyu degistir"di.
            sim.Apply(new Command(sim.TickIndex, CommandKind.Fire, pool, worst));
            sim.Apply(new Command(sim.TickIndex, CommandKind.Hire, pool, cand));
            return true;
        }
    }

    public static class Equipment
    {
        /// <summary>
        /// Botun cikabilecegi EN UST soguk hava kademesi. Olcum icin.
        ///
        /// Merdivenin her basamaginin kendini odeyip odemedigi ancak
        /// basamaklar TEK TEK kapatilarak olculebiliyor. Bu bir zamanlar
        /// content/equipment.json'a erisilemez fiyatlar yazarak
        /// yapiliyordu ve iki kez icerigi yamali birakti: olcum sureci
        /// oldurulunce geri alma calismiyor, ve bir sonraki kalibrasyon
        /// 99.000.000 sikkelik bir depoyla kosuyor.
        ///
        /// Bir bayrak, icerigi hic ellemeden ayni seyi olcuyor.
        /// </summary>
        public static int StorageCap = int.MaxValue;

        public static void Upgrade(Simulation sim, long cashMultiple = 2)
        {
            // HAFTALIK ODEME KORUNUYOR. Ekipman alimi kirayi ve maasi
            // yiyemez.
            //
            // Bu satir olculerek eklendi: adlandirilmis ekipman gelince
            // planci stratejisi 56. gunde 19.649'dan 330'a dustu, ertesi
            // gun malzeme alamadi, servisi sifira indi ve itibari uc gunde
            // 100'den 31'e cokdu. Gercek oyuncu maas gunu yaklasirken
            // waffle makinesi almaz.
            long safety = sim.WeeklyFixedCost();
            // Once ZORUNLU olan: masa sayisinin gerektirdigi yuva.
            for (int st = 0; st < sim.StationCount; st++)
            {
                if (sim.StationTier(st) >= sim.RequiredStationTier(st)) continue;
                long price = sim.NextEquipmentPrice(st);
                if (price < 0 || sim.Cash < price * cashMultiple) continue;
                if (sim.Cash - price < safety) continue;
                sim.Apply(new Command(sim.TickIndex, CommandKind.BuyEquipment, st));
                return;      // gunde bir ekipman; kasayi tek sabahta bosaltma
            }

            // Sonra ISTEGE BAGLI olan: asciyi erken birakan yukseltmeler
            // ve soguk hava. Yalnizca kasa rahatken, cunku bunlar zorunlu
            // degil. Oyunun "sekizinci haftada bile bir sey icin
            // biriktiriyorsun" hedefi (docs/12 7) buradan besleniyor.
            // Kredi varken istege bagli yukseltme yok - BIR ISTISNA ile:
            // adlandirilmis ekipman MENU aciyor ve musteriler o yemekleri
            // aktif olarak SORUYOR (docs/34 6). Kredi bittiginde almak,
            // altmis gunun yarisini "yok" cevabi vererek gecirmek demek.
            //
            // Olculdu: Turk mutfagina doner ve pide gelince genislemeyen
            // stratejinin itibari 85,1'den 50,7'ye dustu, cunku kredisi
            // vardi ve ocagi HIC alamiyordu; her gun sorulan yemege yok
            // diyordu. Ekonomi degil bot hataliydi.
            bool tightBudget = sim.HasLoan;

            // Soguk hava once: menu genisligi aciyor, yani hem ortalama
            // fisi hem memnuniyeti besliyor. Tek bir istasyon yuvasindan
            // daha genis etkili.
            // Soguk hava kredi varken de alinabiliyor, ama iki haftalik
            // gideri koruyarak. Tamamen yasaklamak olculdu ve pahaliydi:
            // huy ucretleri gelince butce oynadi, kredi daha erken cekildi,
            // ve kredi soguk havayi kilitleyince menu daralip memnuniyet
            // dustu. Soguk hava kendi parasini cikaran bir yatirim - kredi
            // varken YASAK degil, DIKKATLI alinmali.
            long cold = sim.StorageTier >= StorageCap ? -1 : sim.NextStoragePrice();
            long coldSafety = tightBudget ? safety * 2 : safety;
            if (cold >= 0 && sim.Cash >= cold * (cashMultiple + 2)
                && sim.Cash - cold >= coldSafety)
            {
                sim.Apply(new Command(sim.TickIndex, CommandKind.BuyStorage));
                return;
            }

            for (int st = 0; st < sim.StationCount; st++)
            {
                long price = sim.NextEquipmentPrice(st);
                if (price < 0 || sim.Cash < price * (cashMultiple + 2)) continue;
                if (sim.Cash - price < safety) continue;
                // Kredi varken YALNIZCA menu acan ekipman, ve iki haftalik
                // sabit gideri koruyarak. Bir hafta yetmiyor: planci
                // stratejisi genisleme takvimini kaciriyordu (12,5 masa,
                // hedef 13) cunku parayi ocaga yatirip kademeyi geciktirdi.
                if (tightBudget && !sim.IsCuisineStation(st)) continue;
                if (tightBudget && sim.Cash - price < safety * 2) continue;

                // Adlandirilmis ekipman (tas firin, doner ocagi, pide firini)
                // MENU aciyor, kapasite acmiyor. Dort masada oturan bir
                // lokantanin acacak yeri yok: menuyu genisletmek stogu boler
                // ve parayi yatirim degil GIDER yapar.
                //
                // Olculdu: Turk mutfagina doner ve pide gelince genislemeyen
                // strateji 10.200 sikkeyi uc ocaga yatirdi, son kasasi
                // 8.748'e dustu ve buyume carpani 4,14'e cikti - yani
                // "genislemek fazla odullendiriyor" gibi gorundu. Ekonomi
                // degil BOT hataliydi: gercek oyuncu dort masaya ucuncu
                // firini almaz.
                if (sim.IsCuisineStation(st) && sim.TableCount <= 4
                    && price * 6 > sim.Cash) continue;


                sim.Apply(new Command(sim.TickIndex, CommandKind.BuyEquipment, st));
                return;
            }
        }
    }

    public sealed class RestockOnly : IStrategy
    {
        public string Name => "sadece_hal";
        public string Question => "Sadece malzeme alip baska hicbir sey yapmayan ne kazanir";

        public void OnMorning(Simulation sim) { Restock(sim); }
        public void OnEvening(Simulation sim, in DayReport report) { }

        /// <summary>Onerilen stogu siparis eder. Kasa yetmezse kismi kalir.</summary>
        public static void Restock(Simulation sim)
        {
            Restock(sim, false);
        }

        /// <summary>
        /// Sabah halden malzeme alir.
        ///
        /// stockAhead: soguk hava varsa ve malzeme BUGUN ucuzsa fazladan
        /// alir. Ucuzluk iki kaynaktan gelir: mevsim (yavas, ongorulebilir)
        /// ve halin gunluk oynamasi (hizli, ongorulemez). Ikisi de tek
        /// basina bir karar degil, sadece bir gider oynamasi; karar olmasi
        /// icin ucuzken alip saklayabilmek gerekiyor. Soguk hava merdiveni
        /// tam olarak bunu satiyor - docs/12 3.
        /// </summary>
        public static void Restock(Simulation sim, bool stockAhead)
        {
            for (int i = 0; i < sim.IngredientCount; i++)
            {
                int need = sim.RecommendedRestock(i);
                if (need <= 0) continue;

                if (stockAhead && sim.CanKeep(i))
                {
                    // Bugunku fiyat, yil ortalamasinin altindaysa stok yap.
                    // Ortalama halin oynamasini icermez (oynamanin ortalamasi
                    // 1.0), yani karsilastirma mevsim + hal toplamini olcer.
                    long today = sim.IngredientPriceToday(i);
                    long mean = sim.IngredientPriceMean(i);
                    if (mean > 0 && today * 100 < mean * 92)
                    {
                        // Tavan MaxUsefulDays'ten geliyor, elle yazilan
                        // bir sayidan degil: arayuz oyuncuya "en fazla uc
                        // gunluk" diyecekken botun dort gunluk almasi,
                        // aracin oyuncunun goremeyecegi bir stratejiyi
                        // olcmesi demekti.
                        int days = sim.KeepDays(i);
                        int cap = sim.MaxUsefulDays(i);
                        if (days > cap) days = cap;
                        if (days > 1) need *= days;
                    }
                }

                sim.Apply(new Command(sim.TickIndex, CommandKind.OrderIngredient, i, need));
            }
        }
    }

    /// <summary>
    /// Makul oyuncu: kizgin musteri gorunce ise alir, kasa yeterliyse
    /// ve itibar tasiyorsa genisler. Soru 2: yili hangi net varlikla bitirir.
    /// </summary>
    public sealed class ReasonablePlayer : IStrategy
    {
        private readonly bool _expand;
        private int _lastServiceRateBp = 10_000;

        /// <summary>Kac gundur salon kadrosu fazla. Ucte cikariyor.</summary>
        private int _fazlaGun;

        /// <summary>
        /// Kredi cekmeye izin var mi. Yalnizca OLCUM icin: kredisiz
        /// stratejisi bunu kapatip ayni oyuncuyu kosuyor, boylece tek
        /// degisken kredi oluyor.
        /// </summary>
        public static bool AllowLoan = true;

        /// <summary>
        /// Salon kadrosunu kapasite modelinin istediginden KAC KISI
        /// eksik tutacagi. Yalnizca OLCUM icin.
        ///
        /// Neden var: mudahalenin degeri rahat bir restoranda olculemez.
        /// Olculdu - mudahaleci bot 1440 mudahalenin 1440'ini gecirdi ve
        /// makul oyuncuyla AYNI sayida kisi agirladi. Sebep tavan: iyi
        /// yonetilen bir dukkanda gunde ~0,3 grup kaciyor, yani
        /// kurtarilacak bir sey yok.
        ///
        /// Bu kol, ayni oyuncuyu bir garson eksikle kosturuyor; o zaman
        /// kaybedilen grup sayisi anlamli hale geliyor ve "mudahale ise
        /// yariyor mu" sorusu gercekten sorulabiliyor.
        /// </summary>
        public static int SalonShort = 0;

        public ReasonablePlayer(bool expand = true) { _expand = expand; }

        public string Name => _expand ? "makul" : "genislemeyen";
        public string Question => _expand
            ? "Iyi oynayan oyuncu yili hangi net varlikla bitirir"
            : "Hic genislemeyen oyuncu ne kadar kazaniyor";

        public void OnMorning(Simulation sim)
        {
            // Once menuyu talebe gore daralt, SONRA hal'e git.
            //
            // Genis menu tasimak pahali: menude duran her yemek icin bir
            // gruba yetecek malzeme stoklanmali ve bozulabilir olanlar her
            // gun sifirlaniyor. On iki ana yemekli menu, gunde on uc
            // musterisi olan bir dukkani batiriyor. Hal asamasinin gerilimi
            // tam olarak bu.
            NarrowMenu(sim);

            // Kasa iki haftalik sabit gideri karsilamiyorsa kredi cek.
            // docs/12 4 bu durum icin var. Kredisiz oyuncu bir kez
            // bosalinca malzeme alamiyor ve toparlanamiyor.
            if (AllowLoan && !sim.HasLoan && sim.Cash < sim.WeeklyFixedCost() * 2)
                sim.Apply(new Command(sim.TickIndex, CommandKind.TakeLoan, 0));

            RestockOnly.Restock(sim, stockAhead: true);

            // Ekipman genislemeden ONCE geliyor. Masasi dolmayan bir
            // restoran yeni masa degil, yetisemeyen bir mutfak icin
            // ekipman almali.
            Equipment.Upgrade(sim);

            if (!_expand) return;

            // Bir sonraki kademeye gecmeye gucu yetiyorsa ve itibar
            // yeterliyse genisle. Kasayi tamamen bosaltma.
            for (int t = 0; t < sim.TierCount; t++)
            {
                if (sim.TablesAtTier(t) <= sim.TableCount) continue;
                long cost = sim.UpgradeCostFor(t);

                // Genislemenin uc sarti: para var, itibar var ve mevcut
                // dukkan zaten talebi karsilayabiliyor. Ucuncusu olmadan
                // oyuncu doldurmadigi masalara kira odemeye basliyor.
                bool canServe = _lastServiceRateBp > 8500;

                // BORCU VARKEN GENISLEMIYOR - ve bu kural OLCULDU,
                // kusurlu bulundu, ama DUZELTILMEDI. Sebebi asagida.
                //
                // Kural makul oyuncuyu kendi eliyle sakatliyor: krediye
                // hic dokunmayan ayni oyuncu (kredisiz stratejisi) 14
                // masaya ve 100 itibara cikip 27.853 ile bitiriyor,
                // makul ise 7,4 masada kalip 25.424 ile. Yani kredi
                // cekmek buyume egrisini SEKIZ HAFTA kapatiyor - ve
                // bunu yapan ekonomi degil, botun kendi kurali.
                //
                // Kapi "karsilayabiliyor mu"ya cevrildi ve olculdu:
                //
                //   pay = 4 taksit        makul 14 masa, 43.746 - plancıyı GECIYOR
                //   pay = kalan borcun
                //         tamami         makul 14 masa, 39.877 - yine geciyor
                //   kalibrasyon cezasi   9 -> 32, dort hedef daha kiriliyor:
                //                        imzaci/makul 0,84 ve 0,82 (taban 0,90),
                //                        turk buyume carpani 4,20 (tavan 4,0),
                //                        para makul'de de onemsizlesiyor
                //
                // Yani duzeltme dogru ama TEK BASINA yapilamaz: butun
                // kalibrasyon hedefleri bu sakat referansa gore
                // ayarlanmis, ve referansi degistirmek sabit noktayi,
                // kiralari ve hedef bantlari yeniden turetmeyi
                // gerektiriyor. Yarim ayarlanmis bir denge, belgelenmis
                // bir kusurdan kotudur.
                //
                // Ayrintili kayit ve kapatma sirasi: docs/12 8d.
                if (sim.Cash > cost * 2 && sim.ReputationCenti > 4500 && canServe
                    && !sim.HasLoan)
                    sim.Apply(new Command(sim.TickIndex, CommandKind.Expand, t));
                break;
            }
        }

        /// <summary>
        /// Talebin kaldiracagi kadar ana yemek acik tutar, gerisini kapatir.
        /// Kaba kural: her ana yemege gunde en az dort kisi dusmeli.
        /// </summary>
        private static void NarrowMenu(Simulation sim)
        {
            int people = sim.ExpectedPeopleToday();

            // Menu genisligini SOGUK HAVA belirliyor. Soguk hava yokken
            // bozulabilir her sey gece oldugu icin menude duran her yemek
            // her gun yeniden stoklanmali ve arta kalan cope gidiyor;
            // o yuzden ana yemek basina dort musteri istiyoruz. Soguk hava
            // arta kalani yasattigi icin bu esik dusuyor.
            //
            // Otuz iki yemeklik icerik envanterinin var olma sebebi bu:
            // yukseltme, menu genisligi satin aliyor.
            // T2 ILE T3 AYNI DEGIL.
            //
            // Once { 4, 3, 2, 2 } yaziyordu: en ust kademe, bir
            // oncekinden fazla hicbir sey vermiyordu. Bot da haliyle
            // onu hic almiyordu - olcum "t3 hic satin alinmiyor" diye
            // cikiyor ve bu bir ICERIK bulgusu sanilıyordu, oysa
            // BOTUN tablosuydu. Simulasyon t3'te menuyu daha da
            // genisletmeye izin veriyor (Awaited cezasi oranli), yani
            // botun o firsati gormesi gerekiyor.
            int[] perMain = { 4, 3, 2, 1 };
            int need = perMain[sim.StorageTier < perMain.Length ? sim.StorageTier : perMain.Length - 1];
            int allowedMains = people / need;
            if (allowedMains < 2) allowedMains = 2;

            // ISIMLI MUSTERININ SEVDIGI YEMEK ONCE.
            //
            // Duzenli musteri sevdigi yemegi menude bulamazsa memnuniyeti
            // dusuyor (docs/11). Menuyu kor bir sirayla daraltmak, tam da
            // o mekanigin cezasini her gun odemek demek - olcum bunu
            // yakaladi: iyi oyuncunun itibari 96,5'ten 87'ye indi ve bazi
            // kosularda dukkan bosaldi. Ekonomi degil BOT hataliydi;
            // gercek oyuncu Hasan Usta'nin kuru fasulyesini menuden
            // cikarmaz.
            //
            // Bu, mekanigin yarattigi KARAR: menu genisligi sinirli ve
            // isimli musterinin favorisi o sinirdan bir yer aliyor.
            int kept = 0;
            for (int pass = 0; pass < 2; pass++)
            {
                for (int i = 0; i < sim.DishCount; i++)
                {
                    if (!sim.IsUnlocked(i)) continue;
                    if (!sim.IsMain(i))
                    {
                        if (pass == 0)
                            sim.Apply(new Command(sim.TickIndex, CommandKind.SetMenuSlot, i, 1));
                        continue;
                    }

                    bool favourite = sim.IsFavouriteOfArrivedRegular(i);
                    if (pass == 0 && !favourite) continue;      // once favoriler
                    if (pass == 1 && favourite) continue;       // sonra kalanlar

                    bool on = kept < allowedMains;
                    if (on) kept++;
                    sim.Apply(new Command(sim.TickIndex, CommandKind.SetMenuSlot, i, on ? 1 : 0));
                }
            }
        }

        public void OnEvening(Simulation sim, in DayReport report)
        {
            if (report.PlannedParties > 0)
                _lastServiceRateBp = (int)Lokanta.Core.Fx.MulDiv(
                    report.ServedParties, Lokanta.Core.Fx.One, report.PlannedParties);

            // Kadro KAPASITE MODELINDEN okunur, kizgin musteri sayisindan degil.
            //
            // Ilk hali "iki gun ust uste kizgin musteri varsa ise al" diyordu.
            // Ama kizgin musteri her zaman kadro sinyali degil: sabri kisa bir
            // arketip zirvede bekleyip cikabilir, stok tukenmis olabilir.
            // Denge araci sonucu gosterdi: dort masada iki salon personeli,
            // haftalik 2.408 sikke maas, kira 1.950. Kapasite modeli o hacimde
            // sifir salon personeli istiyor; patron tek basina yetiyor.
            // KADRO BUGUNE GORE, ISTEN CIKARMA ISRARA GORE.
            //
            // RequiredCrewToday artik gercekten BUGUNU olcuyor (eskiden
            // her gun hafta sonu zirvesini veriyordu). Bu dogru ama
            // botu her hafta ise alip cikarmaya iter: cuma tut,
            // pazartesi kov. Isten cikarma DENEYIMI sifirliyor, yani
            // churn bedava degil.
            //
            // Kural: eksikse HEMEN al, fazlaysa UC GUN ust uste fazla
            // olsun. Gercek oyuncu da hafta sonu icin tuttugu garsonu
            // pazartesi kovmaz.
            //
            // KARAR AKSAM VERILIYOR AMA YARINI ETKILIYOR.
            //
            // Burasi OnEvening: AdvanceToNextDay bu cagridan SONRA
            // geliyor, yani "bugun"un gun tipiyle kurulan kadro yarin
            // sahaya cikiyor. 5. gun (hafta ici) aksami hafta ici
            // kadrosuna gore karar veriliyor, 6. gune (hafta sonu)
            // eksik kadroyla giriliyordu. Metodun adi dogruydu,
            // CAGIRANI yanlis gunu soruyordu.
            Crew need = sim.RequiredCrewTomorrow();

            if (sim.Cooks < need.Cooks && sim.Cooks + sim.SalonStaff < sim.StaffCap)
            {
                sim.Apply(new Command(sim.TickIndex, CommandKind.Hire, 0, Hiring.Pick(sim, 0)));
                _fazlaGun = 0;
                return;
            }
            int salonHedef = need.Salon - SalonShort;
            if (salonHedef < 0) salonHedef = 0;

            _fazlaGun = sim.SalonStaff > salonHedef ? _fazlaGun + 1 : 0;

            if (sim.SalonStaff < salonHedef && sim.Cooks + sim.SalonStaff < sim.StaffCap)
            {
                sim.Apply(new Command(sim.TickIndex, CommandKind.Hire, 1, Hiring.Pick(sim, 1)));
                return;
            }

            // Kadro dogruysa KALITESINE bak: elindeki kisi kapidaki adaydan
            // belirgin olarak kotuyse degistir. Salon once, cunku salon
            // huyu her masaya dogrudan yaziliyor.
            // Kosul ">=" - "==" degil. Ilk yazim tam esitlik ariyordu ve
            // kapasite modeli cogu gun tam esitlik vermiyor; degistirme
            // neredeyse hic calismadi. Kotu bir garsonla altmis gun
            // gecirmek, mekanigin cezasini almak ama kararini hic
            // vermemek demekti.
            if (sim.SalonStaff >= salonHedef && sim.SalonStaff > 0
                && Hiring.ReplaceWorst(sim, 1)) return;
            if (sim.Cooks >= need.Cooks && Hiring.ReplaceWorst(sim, 0)) return;

            // Fazla kadro dogrudan zarar: maas musteri gelsin gelmesin odeniyor.
            if (sim.SalonStaff > salonHedef && _fazlaGun >= 3)
            {
                sim.Apply(new Command(sim.TickIndex, CommandKind.Fire, 1,
                                      sim.SalonStaff - 1));
                _fazlaGun = 0;
            }
            else if (sim.Cooks > need.Cooks && sim.Cooks > 1)
                sim.Apply(new Command(sim.TickIndex, CommandKind.Fire, 0,
                                      sim.Cooks - 1));
        }
    }

    /// <summary>
    /// Fiyati surekli piyasa ustunde tutan oyuncu.
    /// Soru 4: bu strateji kazaniyor mu.
    /// </summary>
    /// <summary>
    /// Fiyati piyasanin ALTINDA tutan oyuncu.
    ///
    /// yuksek_fiyat'in karsiti ve arac uzun sure ASIMETRIKTI: asiri
    /// fiyatlamayi olcuyor, indirim kirmayi olcmuyordu. Oysa gercek bir
    /// oyuncunun ilk refleksi cogu zaman "ucuzlatayim, kalabalik gelsin".
    ///
    /// Fiyatin talebe DOGRUDAN etkisi yok (DemandModel fiyat almiyor);
    /// tek yol memnuniyet uzerinden itibar. Yani bu strateji ayni
    /// zamanda o dolayli yolun gercekten calisip calismadigini sinıyor.
    /// </summary>
    public sealed class CheapPricer : IStrategy
    {
        private readonly ReasonablePlayer _inner = new ReasonablePlayer();
        private bool _applied;

        public string Name => "ucuz_fiyat";
        public string Question => "Fiyati piyasanin altinda tutmak kazandiriyor mu";

        public void OnMorning(Simulation sim)
        {
            if (!_applied)
            {
                // %15 alti: icerikteki underpriceFloorBp tabani tam
                // burada, yani asagisi saf ciro kaybi olmali.
                for (int i = 0; i < 64; i++)
                {
                    long baseline = sim.BasePriceOf(i);
                    if (baseline <= 0) break;
                    int target = (int)Lokanta.Core.Fx.Bp(baseline, 8500);
                    sim.Apply(new Command(sim.TickIndex, CommandKind.SetPrice, i, target));
                }
                _applied = true;
            }
            _inner.OnMorning(sim);
        }

        public void OnEvening(Simulation sim, in DayReport report)
        {
            _inner.OnEvening(sim, report);
        }
    }

    /// <summary>
    /// ANA YEMEGE DOKUNMADAN yanlari, icecekleri ve tatlilari uc katina
    /// cikaran oyuncu. Bir acigin kalici nobetcisi.
    ///
    /// Duzeltmeden once: memnuniyet yalnizca ana yemegin fiyatina
    /// bakiyordu (ComputeSatisfaction), fis ise dort kalemi birden
    /// yaziyordu (OrderPrice). Yani 32 yemegin 20'si sinirsizca
    /// pahalilastirilabiliyor ve musteri hic tepki vermiyordu.
    /// Olculdu: Turk mutfaginda tek icecegi 16'dan 160'a cikarmak
    /// +109.000 santi, kampanyanin butun karinin ALTI KATI.
    ///
    /// Beklenen sonuc: `makul`u GECMEMELI. Gecerse ceza yine yalnizca
    /// bir kaleme bakiyor demektir.
    /// </summary>
    public sealed class ExtrasGouger : IStrategy
    {
        private readonly ReasonablePlayer _inner = new ReasonablePlayer();
        private bool _applied;

        public string Name => "pahali_ekstra";
        public string Question => "Ana yemege dokunmadan ekstralari pahalilastirmak kazandiriyor mu";

        public void OnMorning(Simulation sim)
        {
            if (!_applied)
            {
                for (int i = 0; i < sim.DishCount; i++)
                {
                    if (sim.IsMainDish(i)) continue;          // ana yemek ELLENMIYOR
                    long baseline = sim.BasePriceOf(i);
                    if (baseline <= 0) continue;
                    // TAVANIN ALTINDA: 2,3 kat. Uc kat yazmak artik
                    // REDDEDILIYOR (overpriceCeilingBp 25000) ve
                    // reddedilen bir komut hicbir seyi sinamaz - bot
                    // sessizce "makul oyuncu"ya donusurdu.
                    sim.Apply(new Command(sim.TickIndex, CommandKind.SetPrice,
                                          i, (int)(baseline * 23 / 10)));
                }
                _applied = true;
            }
            _inner.OnMorning(sim);
        }

        public void OnEvening(Simulation sim, in DayReport report)
        {
            _inner.OnEvening(sim, report);
        }
    }

    /// <summary>
    /// KOMBOYU ACIP EKSTRALARI SISIREN BOT.
    ///
    /// Neden var: acik tam iki botun KESISIMINDE duruyordu.
    /// "pahali_ekstra" ekstralari pahalilastiriyor ama komboyu
    /// ACMIYOR; "imzaci" komboyu aciyor ama fiyata DOKUNMUYOR. Ikisini
    /// birlestiren bot yoktu ve o yuzden denge araci yillarca yesil
    /// kaldi.
    ///
    /// Olculen istismar: kombo acikken yan ve icecegin fiyati
    /// memnuniyete HIC girmiyordu (yalnizca ana yemek olculuyordu) ve
    /// fiyatin tavani yoktu - bot 24,8 MILYON sikke topluyordu, taban
    /// kosunun 1470 kati.
    ///
    /// Bu bot artik bir DENETIM: kasasi makul oyuncunun birkac katini
    /// gecerse acik geri gelmis demektir.
    /// </summary>
    public sealed class ComboGouger : IStrategy
    {
        private readonly ReasonablePlayer _inner = new ReasonablePlayer();
        private bool _applied;

        public string Name => "kombo_sismesi";
        public string Question => "Komboyu acip ekstralari sismek kazandiriyor mu";

        public void OnMorning(Simulation sim)
        {
            _inner.OnMorning(sim);

            if (sim.HasCombo && !sim.ComboEnabled)
                sim.Apply(new Command(sim.TickIndex, CommandKind.SetCombo, 1));

            if (_applied) return;
            _applied = true;

            // Ana yemege DOKUNMADAN ekstralari elden geldigince pahali
            // yap. Tavan varsa komut reddedilir ve fiyat piyasada kalir;
            // yoksa yirmi kat yazilir.
            for (int i = 0; i < sim.DishCount; i++)
            {
                if (sim.IsMainDish(i)) continue;
                long baseline = sim.BasePriceOf(i);
                if (baseline <= 0) continue;
                // Tavanin hemen altinda: 2,4 kat. Daha fazlasi
                // reddediliyor ve bot hicbir seyi sinamaz olurdu.
                sim.Apply(new Command(sim.TickIndex, CommandKind.SetPrice,
                                      i, (int)(baseline * 24 / 10)));
            }
        }

        public void OnEvening(Simulation sim, in DayReport report)
        {
            _inner.OnEvening(sim, report);
        }
    }

    public sealed class GreedyPricer : IStrategy
    {
        private readonly int _markupBp;
        private readonly ReasonablePlayer _inner = new ReasonablePlayer();
        private bool _applied;

        private readonly string _name;

        /// <summary>
        /// ZAM BANDI ARTIK IKI NOKTADAN OLCULUYOR.
        //
        /// Fiyat botlari 8500 (ucuz), 13000, 23000 ve 24000 bp'de
        /// duruyordu; 10000 ile 13000 ARASINDA hicbir olcum yoktu.
        /// Onemliydi, cunku fiyatin talebe dogrudan kanali yok -
        /// tek yol memnuniyet -> itibar, ve itibar kademe tavanina
        /// kirpiliyor. Tavandaki oyuncu icin memnuniyet kaybi
        /// hicbir sey satin almiyor olabilir, yani kucuk bir zam
        /// BEDAVA olabilir. Bunu ancak bandin icinden bir bot
        /// gosterir.
        /// </summary>
        public GreedyPricer(int markupBp = 13000, string name = "yuksek_fiyat")
        {
            _markupBp = markupBp;
            _name = name;
        }

        public string Name => _name;
        public string Question => "Fiyati surekli piyasa ustunde tutan strateji kazaniyor mu";

        public void OnMorning(Simulation sim)
        {
            if (!_applied)
            {
                // Fiyatlar bir kez ayarlanir; SetPrice mutlak deger aliyor.
                for (int i = 0; i < 64; i++)
                {
                    long baseline = sim.BasePriceOf(i);
                    if (baseline <= 0) break;
                    int target = (int)Lokanta.Core.Fx.Bp(baseline, _markupBp);
                    sim.Apply(new Command(sim.TickIndex, CommandKind.SetPrice, i, target));
                }
                _applied = true;
            }
            _inner.OnMorning(sim);
        }

        public void OnEvening(Simulation sim, in DayReport report)
        {
            _inner.OnEvening(sim, report);
        }
    }

    /// <summary>
    /// Parayi gorur gormez genisleyen oyuncu. Servis orani sartini aramiyor.
    /// Olctugu sey: makul oyuncunun %85 servis orani kapisi fazla mi siki.
    /// </summary>
    public sealed class Expansionist : IStrategy
    {
        public string Name => "atilgan";
        public string Question => "Genisleme kapilari fazla mi siki";

        public void OnMorning(Simulation sim)
        {
            RestockOnly.Restock(sim);

            if (!sim.HasLoan && sim.Cash < sim.WeeklyFixedCost() * 2)
                sim.Apply(new Command(sim.TickIndex, CommandKind.TakeLoan, 0));

            for (int t = 0; t < sim.TierCount; t++)
            {
                if (sim.TablesAtTier(t) <= sim.TableCount) continue;
                // Tek sart: parasi yetsin. Itibar ve servis orani aranmiyor.
                if (sim.Cash > sim.UpgradeCostFor(t))
                    sim.Apply(new Command(sim.TickIndex, CommandKind.Expand, t));
                break;
            }
        }

        public void OnEvening(Simulation sim, in DayReport report)
        {
            Crew need = sim.RequiredCrewToday();
            if (sim.Cooks < need.Cooks && sim.Cooks + sim.SalonStaff < sim.StaffCap)
                sim.Apply(new Command(sim.TickIndex, CommandKind.Hire, 0, Hiring.Pick(sim, 0)));
            else if (sim.SalonStaff < need.Salon && sim.Cooks + sim.SalonStaff < sim.StaffCap)
                sim.Apply(new Command(sim.TickIndex, CommandKind.Hire, 1, Hiring.Pick(sim, 1)));
            else if (sim.SalonStaff > need.Salon)
                sim.Apply(new Command(sim.TickIndex, CommandKind.Fire, 1));
        }
    }

    /// <summary>
    /// Kapali form modelin takvimine gore genisleyen oyuncu:
    /// 15. gun 7 masa, 29. gun 10 masa, 43. gun 14 masa.
    /// Olctugu sey: o takvim simulasyonda karsilanabiliyor mu.
    /// </summary>
    public sealed class PlannerSchedule : IStrategy
    {
        private static readonly int[] Days = { 15, 29, 43 };
        private int _next;

        public string Name => "planci";
        public string Question => "Kapali form modelin genisleme takvimi karsilanabiliyor mu";

        public void OnMorning(Simulation sim)
        {
            RestockOnly.Restock(sim, stockAhead: true);

            if (!sim.HasLoan && sim.Cash < sim.WeeklyFixedCost() * 2)
                sim.Apply(new Command(sim.TickIndex, CommandKind.TakeLoan, 0));

            // Takvim masaya bakiyor; mutfak da o masaya yetismek zorunda.
            Equipment.Upgrade(sim);

            if (_next >= Days.Length || sim.Day < Days[_next]) return;

            for (int t = 0; t < sim.TierCount; t++)
            {
                if (sim.TablesAtTier(t) <= sim.TableCount) continue;
                sim.Apply(new Command(sim.TickIndex, CommandKind.Expand, t));
                break;
            }
            _next++;
        }

        public void OnEvening(Simulation sim, in DayReport report)
        {
            Crew need = sim.RequiredCrewToday();
            if (sim.Cooks < need.Cooks && sim.Cooks + sim.SalonStaff < sim.StaffCap)
                sim.Apply(new Command(sim.TickIndex, CommandKind.Hire, 0, Hiring.Pick(sim, 0)));
            else if (sim.SalonStaff < need.Salon && sim.Cooks + sim.SalonStaff < sim.StaffCap)
                sim.Apply(new Command(sim.TickIndex, CommandKind.Hire, 1, Hiring.Pick(sim, 1)));
            else if (sim.SalonStaff > need.Salon)
                sim.Apply(new Command(sim.TickIndex, CommandKind.Fire, 1));
        }
    }

    /// <summary>
    /// Malzemeden kisan oyuncu: her seyi EN UCUZ kaliteden aliyor,
    /// baska her sey makul oyuncu gibi.
    ///
    /// Olctugu soru: malzemeden kismak gecerli bir strateji mi, tuzak mi.
    /// Icerik en hassas alti malzemeyi ET yapmis, yani cevabin menuye
    /// gore degismesi bekleniyor.
    /// </summary>
    public sealed class CheapIngredients : IStrategy
    {
        private readonly ReasonablePlayer _inner = new ReasonablePlayer();
        private bool _set;

        public string Name => "ucuz_malzeme";
        public string Question => "Malzemeden kismak kazandiriyor mu";

        public void OnMorning(Simulation sim)
        {
            if (!_set)
            {
                sim.Apply(new Command(sim.TickIndex, CommandKind.SetQuality, 0));
                _set = true;
            }
            _inner.OnMorning(sim);
        }

        public void OnEvening(Simulation sim, in DayReport report)
        {
            _inner.OnEvening(sim, report);
        }
    }

    /// <summary>
    /// Servis sirasinda MUDAHALE eden oyuncu: makul oyuncu gibi oynuyor,
    /// ayrica gun icinde sabri en az kalan masalara patron hakkini
    /// harciyor.
    ///
    /// Olctugu soru: docs/02'nin cekirdek dongusu -- "servis sirasinda
    /// sadece krizlere mudahale edersin" -- gercekten kazandiriyor mu.
    /// Mudahale hakki gun basina sinirli ve cay ikrami parayla.
    /// </summary>
    /// <summary>
    /// Imza mekanigini kullanan oyuncu. docs/07: mekanik mutfagi mutfaktan
    /// ayiran tek sey, o yuzden ONU KULLANMAK kazandirmali - ama mecbur
    /// birakmamali. Bu strateji o bandi olcuyor.
    ///
    /// Fast food -> komboyu ikinci mevsimde acar ve acik tutar.
    /// Turk       -> veresiyeyi SECEREK acar: yalnizca cay ikram ettigi
    ///               gruba, gunde en fazla ikisine, ve acik hesap tavani
    ///               bir haftalik sabit gideri gecmiyorsa.
    /// </summary>
    public sealed class SignaturePlayer : IStrategy
    {
        /// <summary>
        /// Veresiye icin en az ziyaret sayisi. 0 = herkese.
        /// STATIK: her strateji basinda yeniden yaziliyor, yoksa bir
        /// sonraki kol seciciligi miras alir ve olcum sessizce baska
        /// bir seyi olcer.
        /// </summary>
        public static int MinVisits;

        private readonly ReasonablePlayer _inner = new ReasonablePlayer();
        private int _grantedToday;

        public string Name => "imzaci";
        public string Question => "Imza mekanigini kullanmak kazandiriyor mu";

        public void OnMorning(Simulation sim)
        {
            _grantedToday = 0;
            _inner.OnMorning(sim);

            // Kombo bedava degil (mutfagi yoruyor) ama menude tutmasi
            // gereken uc kalem zaten acilis menusunun icinde.
            if (sim.HasCombo && !sim.ComboEnabled)
                sim.Apply(new Command(sim.TickIndex, CommandKind.SetCombo, 1));
        }

        public void OnEvening(Simulation sim, in DayReport report)
        {
            _inner.OnEvening(sim, report);
        }

        /// <summary>
        /// ZIRVEDE KOMBOYU KAPATAN KOL. 0 = hic kapatma.
        ///
        /// Tasarimin yazili niyeti: "kombo mutfak yukunu de artirdigi
        /// icin zirvede kapatmak MESRU bir oyun ve eksen onu
        /// cezalandirmamali". Ama o oyunu oynayan bir bot yoktu, yani
        /// eksenin hedefi ancak UYDURULARAK konabilirdi. Bu kol onu
        /// olcuyor: salon doluluk esigini gecince kombo kapaniyor,
        /// dusunce yeniden aciliyor.
        ///
        /// STATIK: her strateji basinda yeniden yaziliyor.
        /// </summary>
        public static int CloseAtOccupancyBp;

        /// <summary>Veresiye ISTEYENE veriyor. Servis sirasinda cagriliyor.</summary>
        public void DuringService(Simulation sim)
        {
            // Zirvede kombo kapaniyor: mutfagi rahatlatmak icin.
            if (CloseAtOccupancyBp > 0 && sim.HasCombo && sim.TableCount > 0)
            {
                int dolulukBp = sim.OccupiedTables * 10000 / sim.TableCount;
                bool olmali = dolulukBp < CloseAtOccupancyBp;
                if (sim.ComboEnabled != olmali)
                    sim.Apply(new Command(sim.TickIndex, CommandKind.SetCombo, olmali ? 1 : 0));
            }

            if (!sim.HasCredit || _grantedToday >= 4) return;

            // Acik hesap bir haftalik sabit gideri gecerse dur: veresiye
            // nakit akisini bozar, ve bozulan nakit akisi kirayi odeyemez.
            if (sim.OpenCredit > sim.WeeklyFixedCost()) return;

            for (int p = 0; p < Simulation.MaxParties; p++)
            {
                if (!sim.CreditEligible(p)) continue;

                // GUVEN ESIGI: kime yazdigi onemli mi?
                //
                // Tahsilat sansi artik musterinin ziyaret sayisina
                // bagli. Bu kol sifir esikle herkese yaziyor; secici
                // kol yalnizca tanidiklara. Ikisinin farki, "kime
                // yazayim" sorusunun GERCEKTEN bir karar olup
                // olmadigini soyluyor - tek kol, bir cevap.
                if (MinVisits > 0)
                {
                    int r = sim.PartyRegular(p);
                    if (r < 0 || sim.RegularVisits(r) < MinVisits) continue;
                }


                // Cay veresiyenin PARCASI: ExtendCredit onu kendi oduyor,
                // gunluk mudahale hakkini yemiyor. Ilk yazimda cay bir
                // mudahaleydi ve butce yetmedigi icin veresiyelerin cogu
                // caysiz aciliyordu - yani tahsilat primi hic isletilemedi.
                sim.Apply(new Command(sim.TickIndex, CommandKind.ExtendCredit, p));
                if (++_grantedToday >= 4) return;
            }
        }
    }

    /// <summary>
    /// SABIRLI MUDAHALECI: haklarini SAKLIYOR.
    ///
    /// "mudahaleci" ile tek farki zamanlama - ikisi de ayni fiilleri
    /// ayni siraya gore kullaniyor. Fark, bu kolun bir masa uyari
    /// esiginin altina inmeden HICBIR hak harcamamasi.
    ///
    /// Neden ayri bir kol: mudahalenin degeri olculurken bot haklarini
    /// gunun ilk seksen saniyesinde yakiyordu, zirve ise ikinci
    /// dilimde. Yani "mudahale kazandiriyor mu" sorusu, oyuncunun
    /// verdigi TEK gercek karari ("simdi mi, zirvede mi") sabit
    /// tutarak - ustelik en kotu degerinde - olculuyordu.
    ///
    /// Bu kol mudahaleciyi GECERSE mekanik saglam, sorun oyuncuya
    /// "sakla" demeyi ogretmemek. Gecmezse mekanigin kendisi zayif.
    /// Tek kol, bir cevap.
    /// </summary>
    /// <summary>
    /// SECICI VERESIYECI: yalnizca TANIDIGA yaziyor.
    ///
    /// "imzaci" ile tek farki bu; ikisi de ayni gunlerde ayni sayida
    /// hesap acabiliyor. Fark, bu kolun musteriyi taniyip tanimadigina
    /// bakmasi.
    ///
    /// Neden ayri bir kol: tahsilat sansi artik ziyaret sayisina bagli
    /// ama bunun bir KARAR uretip uretmedigi, ancak seciciligi olculerek
    /// bilinir. Bu kol imzaciyi gecerse soru gercek; gecmezse guven
    /// boyutu yalnizca bir sayi.
    /// </summary>
    public sealed class PickyCreditor : IStrategy
    {
        private readonly SignaturePlayer _inner = new SignaturePlayer();

        public string Name => "secici_veresiye";
        public string Question => "Veresiyeyi yalnizca TANIDIGA acmak kazandiriyor mu";

        public void OnMorning(Simulation sim) { _inner.OnMorning(sim); }
        public void OnEvening(Simulation sim, in DayReport report)
        {
            _inner.OnEvening(sim, report);
        }
        public void DuringService(Simulation sim) { _inner.DuringService(sim); }
    }

    /// <summary>
    /// ZIRVEDE KAPATAN IMZACI.
    ///
    /// "imzaci" ile tek farki: salon dolulugu %75'i gecince komboyu
    /// kapatiyor, dusunce aciyor. Tasarimin MESRU dedigi oyun bu.
    ///
    /// Neden gerekli: imza ekseninin hedefi bu oyunun ulastigi paya gore
    /// konmali, yoksa hedef uydurulmus olur - ve kodun kendi uyarisi
    /// "uydurulmus bir hedef ekseni ya doygun ya erisilmez yapar" diyor.
    /// </summary>
    public sealed class PeakCloser : IStrategy
    {
        private readonly SignaturePlayer _inner = new SignaturePlayer();

        public string Name => "zirvede_kapat";
        public string Question => "Zirvede komboyu kapatmak kazandiriyor mu";

        public void OnMorning(Simulation sim) { _inner.OnMorning(sim); }
        public void OnEvening(Simulation sim, in DayReport report)
        {
            _inner.OnEvening(sim, report);
        }
        public void DuringService(Simulation sim) { _inner.DuringService(sim); }
    }

    public sealed class PatientInterventionist : IStrategy
    {
        private readonly ReasonablePlayer _inner = new ReasonablePlayer();

        public string Name => "sabirli_mudahale";
        public string Question => "Mudahaleyi ZIRVEYE saklamak kazandiriyor mu";

        public void OnMorning(Simulation sim) { _inner.OnMorning(sim); }
        public void OnEvening(Simulation sim, in DayReport report)
        {
            _inner.OnEvening(sim, report);
        }
    }

    public sealed class Interventionist : IStrategy
    {
        private readonly ReasonablePlayer _inner = new ReasonablePlayer();

        public string Name => "mudahaleci";
        public string Question => "Servis sirasinda mudahale kazandiriyor mu";

        public void OnMorning(Simulation sim) { _inner.OnMorning(sim); }

        public void OnEvening(Simulation sim, in DayReport report)
        {
            _inner.OnEvening(sim, report);
        }

        /// <summary>
        /// Servis sirasinda cagriliyor. Sabri en az kalan, henuz
        /// mudahale gormemis masaya patron ilgisi gosteriyor.
        /// </summary>
        /// <summary>
        /// Kac mudahale DENENDI ve kaci GECTI.
        ///
        /// "Reddedilen bir bot, bot degildir" (bu proje fiyat tavaninda
        /// ogrendi: tavan gelince yuksek_fiyat botunun fiyatlari
        /// reddediliyordu ve bot sessizce makul oyuncunun kopyasi
        /// olmustu). Mudahalenin etkisini olcmeden once mudahalenin
        /// GERCEKTEN olup olmadigi olculmeli.
        /// </summary>
        public static int Tried, Applied;

        /// <summary>
        /// Sayaclari sifirlar. Strateji basinda cagriliyor.
        ///
        /// STATIK OLDUKLARI ICIN SART: DuringService'i hem "mudahale"
        /// hem "baskili_mudahale" cagiriyor. Sifirlanmadan iki kolun
        /// toplami tek satirda basiliyordu ve sayaclarin var olus
        /// sebebi - KOL BASINA "mudahale gercekten oldu mu" - okunamaz
        /// hale geliyordu.
        /// </summary>
        public static void ResetCounters() { Tried = 0; Applied = 0; }

        /// <summary>
        /// CAY MI, ILGI MI - ARTIK BIR SORU.
        ///
        /// Cay eskiden her eksende ilginin altindaydi ve bu bot ona hic
        /// basmiyordu; yani "uc fiilden biri olu" tespitinin kanitlarindan
        /// biri botun kendi davranisiydi. Cay artik BEKLEYEN HERKESE
        /// gidiyor, yani kalabalikta degeri ilgiyi geciyor olabilir.
        ///
        /// Kural: kac masa bekliyorsa. Esik ve ustunde salona cay, altinda
        /// tek masaya ilgi. Esigin dogru yerde olup olmadigi ancak
        /// olculerek bilinir - bu yuzden esik bir PARAMETRE.
        /// </summary>
        public static int TeaThreshold = 3;

        /// <summary>
        /// SABIRLI KIP: hak yalnizca GERCEK baski varken harcaniyor.
        ///
        /// Bot haklarini her 20 sim-saniyede bir yakiyordu, yani
        /// gunde dort hak 480 saniyelik gunun ilk ~80 saniyesinde
        /// bitiyordu - zirve ise ikinci dilimde. Yani "mudahale
        /// kazandiriyor mu" sorusunun cevabi, mekanigi degil BOTUN
        /// KOTU OYNAMASINI olcuyor olabilirdi. Oyuncunun verdigi
        /// tek gercek karar - "simdi mi, zirvede mi" - sabit
        /// tutulmustu, ustelik en kotu degerinde.
        ///
        /// Bu kip onu serbest birakiyor: bir masa uyari esiginin
        /// altina inmeden hicbir hak harcanmiyor.
        /// </summary>
        public static bool OnlyWhenUrgent;

        public static void DuringService(Simulation sim)
        {
            if (sim.InterventionsLeft <= 0) return;

            // Sabirli kipte: kimse kritik degilse hicbir sey yapma.
            if (OnlyWhenUrgent && !sim.AnyPartyCritical) return;

            // MUTFAK once. Sabri biten musteriyi yatistirmak semptomu
            // orter; tikanan istasyonu acmak sebebi cozer ve o istasyonda
            // bekleyen HERKESI birden kurtarir.
            int station = sim.BusiestStation();
            if (station >= 0)
            {
                int once = sim.InterventionsLeft;
                Tried++;
                sim.Apply(new Command(sim.TickIndex, CommandKind.Intervene,
                                      station, (int)InterventionKind.RushStation));
                if (sim.InterventionsLeft < once) Applied++;
                if (sim.InterventionsLeft <= 0) return;
            }

            // COK MASA BEKLIYORSA SALONA CAY.
            if (TeaThreshold > 0 && sim.WaitingParties >= TeaThreshold)
            {
                int oncesi = sim.InterventionsLeft;
                Tried++;
                sim.Apply(new Command(sim.TickIndex, CommandKind.Intervene,
                                      -1, (int)InterventionKind.FreeTea));
                if (sim.InterventionsLeft < oncesi) Applied++;
                if (sim.InterventionsLeft <= 0) return;
            }

            int worst = sim.MostImpatientParty();
            if (worst < 0) return;

            int oncekiHak = sim.InterventionsLeft;
            Tried++;
            sim.Apply(new Command(sim.TickIndex, CommandKind.Intervene,
                                  worst, (int)InterventionKind.OwnerAttention));
            if (sim.InterventionsLeft < oncekiHak) Applied++;
        }
    }

    /// <summary>
    /// BASKI ALTINDAKI OYUNCU: bir garson eksik.
    ///
    /// Iki kopyasi kosuyor - biri mudahale ediyor, biri etmiyor - ve
    /// aralarindaki TEK fark bu. Rahat bir restoranda mudahalenin
    /// olculemedigi zaten olculmustu (1440/1440 gecti, sonuc degismedi);
    /// bu cift, sorunun "mekanik zayif mi" mi yoksa "zaten kurtarilacak
    /// bir sey yok mu" oldugunu ayiriyor.
    ///
    /// Eksik kadro bilincli bir secim: gercek oyuncu da maas kismak
    /// icin bunu yapiyor ve oyunun vaadi tam da o anda devreye giriyor -
    /// "patronsun, yetismediginde sen mudahale edersin".
    /// </summary>
    public sealed class PressuredPlayer : IStrategy
    {
        private readonly ReasonablePlayer _inner = new ReasonablePlayer();
        private readonly bool _intervene;

        public PressuredPlayer(bool intervene) { _intervene = intervene; }

        public bool Intervenes => _intervene;

        public string Name => _intervene ? "baskili_mudahale" : "baskili";

        public string Question => _intervene
            ? "Kadro yetismezken mudahale kurtariyor mu"
            : "Bir garson eksik calismak ne kaybettiriyor";

        public void OnMorning(Simulation sim) { _inner.OnMorning(sim); }

        public void OnEvening(Simulation sim, in DayReport report)
        {
            _inner.OnEvening(sim, report);
        }
    }

    /// <summary>
    /// Krediye hic dokunmayan oyuncu.
    ///
    /// docs/12 8'in ALTINCI SORUSU: "kredi cekmek ise yariyor mu, yoksa
    /// tuzak mi". Arac o soruyu soruyor ve hicbir strateji cevaplayamiyordu
    /// - makul, planci ve atilgan krediyi firsatci aliyor, KREDISIZ bir
    /// kontrol yoktu. Yani bir tasarim sorusunun cevabi olculmeden
    /// "biliniyor" sayiliyordu.
    ///
    /// Soru simdi anlamli, cunku kredinin bir ARAYUZU var: mekanik
    /// cekirdekte eksiksiz yaziliydi ve hicbir ekranda dugmesi yoktu.
    /// </summary>
    public sealed class NoLoanPlayer : IStrategy
    {
        private readonly ReasonablePlayer _inner = new ReasonablePlayer();

        public string Name => "kredisiz";
        public string Question => "Kredi cekmek ise yariyor mu, yoksa tuzak mi";

        public void OnMorning(Simulation sim)
        {
            // Makul oyuncunun kendisi, TEK farkla: kredi yok.
            // ReasonablePlayer krediyi kendi karar veriyor, o yuzden
            // burada engellemek icin bayrak gerekiyor.
            ReasonablePlayer.AllowLoan = false;
            try { _inner.OnMorning(sim); }
            finally { ReasonablePlayer.AllowLoan = true; }
        }

        public void OnEvening(Simulation sim, in DayReport report)
        {
            _inner.OnEvening(sim, report);
        }
    }

    /// <summary>
    /// Menude TEK ana yemek tutan oyuncu. Bir KABUL TESTI.
    ///
    /// Dar menu bir zamanlar kesin baskin stratejiydi ve arac bunu
    /// goremiyordu, cunku hicbir strateji denemiyordu: menuden cikarilan
    /// yemek "sorulmus" sayilmiyordu, yani daraltmanin talep tarafinda
    /// sifir bedeli vardi. Olculdu - bu strateji makul oyuncuyu fast
    /// food'da %12, Turk mutfaginda %38 geciyordu.
    ///
    /// Bu satirlar o hatanin REGRESYON KORUMASI: tek_yemek makul'u bir
    /// daha gecerse, Awaited() yine menuyu gormuyor demektir.
    /// </summary>
    public sealed class OneDishPlayer : IStrategy
    {
        private readonly ReasonablePlayer _inner = new ReasonablePlayer();

        public string Name => "tek_yemek";
        public string Question => "Menuyu tek yemege daraltmak kazandiriyor mu";

        public void OnMorning(Simulation sim)
        {
            _inner.OnMorning(sim);

            // Makul oyuncu menuyu zaten talebe gore daraltti; bu oyuncu
            // BIR ana yemek disinda hepsini kapatiyor. Yan ve icecekler
            // duruyor: olculen sey menu GENISLIGI, menunun varligi degil.
            int kept = -1;
            for (int i = 0; i < sim.DishCount; i++)
            {
                if (!sim.IsOnMenu(i)) continue;
                if (!sim.IsMainDish(i)) continue;
                if (kept < 0) { kept = i; continue; }
                sim.Apply(new Command(sim.TickIndex, CommandKind.SetMenuSlot, i, 0));
            }
        }

        public void OnEvening(Simulation sim, in DayReport report)
        {
            _inner.OnEvening(sim, report);
        }
    }

    /// <summary>
    /// Menuyu HIC daraltmayan oyuncu: kilidi acilan her ana yemek
    /// menude kaliyor.
    ///
    /// tek_yemek'in KARSITI ve ayni sorunun oteki ucu. docs/32 "soguk
    /// hava menu genisligi satin aldiriyor" diyor; o cumle ancak genis
    /// menu bir SEY kazandiriyorsa dogru. Dar menunun bedeli olcüldu
    /// (tek_yemek iflas ediyor), genis menunun odulu olculmedi.
    ///
    /// Ikisi birden olculmeden merdiven fiyatlandirilamaz: depo, genis
    /// menuyu TASINABILIR kiliyor, yani odulu genis menunun odulu.
    /// </summary>
    public sealed class WideMenuPlayer : IStrategy
    {
        private readonly ReasonablePlayer _inner = new ReasonablePlayer();

        public string Name => "genis_menu";
        public string Question => "Menuyu hic daraltmamak kazandiriyor mu";

        public void OnMorning(Simulation sim)
        {
            _inner.OnMorning(sim);

            // Makul oyuncu menuyu daraltti; bu oyuncu hepsini geri aciyor.
            for (int i = 0; i < sim.DishCount; i++)
            {
                if (sim.IsOnMenu(i) || !sim.IsUnlocked(i)) continue;
                sim.Apply(new Command(sim.TickIndex, CommandKind.SetMenuSlot, i, 1));
            }
        }

        public void OnEvening(Simulation sim, in DayReport report)
        {
            _inner.OnEvening(sim, report);
        }
    }

    /// <summary>
    /// Kadroyu tavana kadar sisiren oyuncu. Fazla kadronun cezasini olcer.
    /// </summary>
    public sealed class OverStaffer : IStrategy
    {
        public string Name => "fazla_kadro";
        public string Question => "Kadroyu tavana dayamak kazandiriyor mu";

        public void OnMorning(Simulation sim)
        {
            RestockOnly.Restock(sim);
            while (sim.Cooks + sim.SalonStaff < sim.StaffCap)
            {
                int pool = sim.SalonStaff <= sim.Cooks ? 1 : 0;
                int before = sim.Cooks + sim.SalonStaff;
                sim.Apply(new Command(sim.TickIndex, CommandKind.Hire, pool, Hiring.Pick(sim, pool)));
                if (sim.Cooks + sim.SalonStaff == before) break;
            }
        }

        public void OnEvening(Simulation sim, in DayReport report) { }
    }
}
