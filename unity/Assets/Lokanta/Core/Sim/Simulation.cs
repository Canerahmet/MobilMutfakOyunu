using System;
using Lokanta.Core.Content;
using Lokanta.Core.Economy;

namespace Lokanta.Core.Sim
{
    public enum DayPhase
    {
        Closed = 0,
        Morning = 1,
        Service = 2,
        Evening = 3
    }

    public enum CustomerStage
    {
        None = 0,
        WaitingForTable = 1,
        WaitingToOrder = 2,
        WaitingForFood = 3,
        Eating = 4,
        WaitingToPay = 5,
        Done = 6,
        LeftAngry = 7
    }

    internal enum TaskKind
    {
        None = 0,
        SeatOrder = 1,
        Cook = 2,
        Serve = 3,
        Pay = 4,
        Clear = 5,

        /// <summary>
        /// Bulasik yikama. Salon havuzundan bir kisi lavaboya geciyor.
        ///
        /// docs/14 bulasikciyi bir DARBOGAZ olarak tarif ediyor: "tabak
        /// biterse servis durur - gorunmeyen ama tikaninca fark edilen".
        /// O darbogaz bugune kadar salon kapasitesinin icine gomuluydu,
        /// yani hic gorunmuyordu.
        /// </summary>
        Wash = 6
    }

    /// <summary>
    /// Sabit adimli restoran simulasyonu.
    ///
    /// docs/23-cekirdek-sozlesmesi.md sozlesmesi geceridir:
    ///   - Tick() parametresizdir, 100 ms ilerletir, gercek zamani gormez
    ///   - butun durum tamsayidir
    ///   - rastgelelik alt sistem basina ayri akislardan gelir
    ///   - ayni tohum ve ayni komut gunlugu ayni durumu verir
    ///
    /// Personel ajan olarak modellenmez. docs/14: "karmasik yol bulma yok".
    /// Personel, is havuzunda bir SUNUCU'dur; ayni anda tek gorev isler.
    /// Gunluk kapasite bu gorev surelerinden dogal olarak cikar.
    ///
    /// Musteri birimi: bir yuva bir MASAYI isgal eden GRUPTUR. Talep formulu
    /// KISI uretir; gruplar kisiler bitene kadar olusturulur. Is ve hesap
    /// grup buyuklugu ile olceklenir. Degerlendirmenin "kisi/grup netlessin"
    /// bulgusunun karari budur.
    /// </summary>
    public sealed partial class Simulation
    {
        public const int MaxParties = 256;
        public const int MaxTables = 16;
        public const int MaxServers = 16;

        /// <summary>Acilis stogu, malzeme basina gram.</summary>
        public const int OpeningStockGrams = 6000;

        /// <summary>
        /// Menude duran her yemek icin en az bu kadar porsiyonluk malzeme
        /// tutulur. Azami grup buyuklugu kadar.
        /// </summary>
        public const int MinPartyBuffer = 4;

        /// <summary>
        /// Gun basina azami komut. docs/23 7.3: dokunus butcesi 60,
        /// dort kat pay birakildi. Asilirsa komut reddediliyor.
        /// </summary>
        public const int MaxCommandsPerDay = 256;

        // Kredi sartlari ICERIKTEN geliyor (economy.json). Once burada
        // sabit kodluydular ve icerikteki degerlerle ayni olmalari
        // tesaduftu; tools/audit_content.py bunu yakaladi.

        /// <summary>Bir gramin kilo fiyatindan maliyeti: fiyat x gram / 1000.</summary>
        private const int GramsPerKilo = 1000;

        // ---- degismezler ---------------------------------------------------
        private readonly EconomyConfig _economy;
        private readonly ContentSet _content;
        private readonly TimingConfig _timing;
        private readonly ulong _masterSeed;

        // ---- rastgelelik ---------------------------------------------------
        private Rng _rngArrival;
        private Rng _rngArchetype;
        private Rng _rngOrder;
        private Rng _rngStaffError;

        /// <summary>Hal fiyatlarinin gunluk oynamasi. RngStream.Market.</summary>
        private Rng _rngMarket;
        private Rng _rngCredit;
        private Rng _rngRegular;
        private Rng _rngHiring;
        private Rng _rngEvent;

        /// <summary>Personel ismi. Oynanisa girmiyor, sunuma giriyor.</summary>
        private Rng _rngName;

        // ---- durum ---------------------------------------------------------
        private long _tickIndex;
        private int _day;
        private DayPhase _phase;
        private int _serviceTick;

        private int _tableCount;
        private int _reputationCenti;
        private long _cash;

        private int _cooks;
        private int _salon;

        // --- Veresiye, docs/07 Turk imza mekanigi ---------------------------
        // Acik hesaplar. Bir hesap = bir grubun odemedigi fis.
        // Islem SIRASI onemli: dizide bosluk birakilmiyor, silinen hesabin
        // yerine sonuncusu kayiyor, boylece tarama _tabCount ile bitiyor.
        public const int MaxTabs = 24;
        private readonly long[] _tabAmount = new long[MaxTabs];
        private readonly int[] _tabDueDay = new int[MaxTabs];
        private readonly int[] _tabTea = new int[MaxTabs];

        /// <summary>
        /// Bu hesap KIME yazildi (duzenli musteri indisi, yoksa -1).
        ///
        /// Defter kimin borcu oldugunu TUTMUYORDU, o yuzden tahsilat
        /// sansi herkes icin ayniydi ve "kime yazayim" diye bir soru
        /// dogamiyordu. Guven bu alandan okunuyor.
        /// </summary>
        private readonly int[] _tabRegular = new int[MaxTabs];
        private int _tabCount;
        /// <summary>Bu grubun fisi veresiyeye yazilacak mi.</summary>
        private readonly bool[] _pCredit = new bool[MaxParties];
        /// <summary>Bu gruba cay ikram edildi mi; tahsilat sansini yukseltiyor.</summary>
        private readonly bool[] _pTea = new bool[MaxParties];
        /// <summary>Bu grup veresiye ISTIYOR mu. Istegi geri cevirmek bedelli.</summary>
        private readonly bool[] _pAsksCredit = new bool[MaxParties];
        /// <summary>Kombo alan grup: fis kombodan hesaplaniyor.</summary>
        private readonly bool[] _pCombo = new bool[MaxParties];
        /// <summary>Kombo menude ve aciksa true. Oyuncunun karari.</summary>
        private bool _comboOn;
        /// <summary>Veresiyenin biriktirdigi sadakat: talebe kalici katki, bp.</summary>
        private int _creditLoyaltyBp;

        // --- Isimli duzenli musteriler, docs/11 -----------------------------
        // Arketip binlerce musteri uretir; duzenli musteri TEK BIR KISIDIR
        // ve hep ayni kisidir. Bu yuzden durumu kisi basina tutuluyor:
        // kac kez geldi, ortalama ne kadar memnun ayrildi, hikayesinin
        // kacinci sahnesi acildi, ve kirilip kac gun gelmeyecek.
        public const int MaxRegulars = 16;
        private readonly int[] _regVisits = new int[MaxRegulars];
        private readonly long[] _regSatSum = new long[MaxRegulars];
        private readonly int[] _regBeat = new int[MaxRegulars];
        private readonly int[] _regAwayDays = new int[MaxRegulars];
        private readonly bool[] _regComing = new bool[MaxRegulars];
        /// <summary>Bu grup hangi duzenli musteri; -1 ise isimsiz kalabalik.</summary>
        private readonly int[] _pRegular = new int[MaxParties];
        /// <summary>Sevdigi yemegi menude bulamadi mi.</summary>
        private readonly bool[] _pMissedFavourite = new bool[MaxParties];
        private readonly int[] _arrRegular = new int[MaxParties];

        // docs/14 "Deneyim ve seviye": kisi basina CALISILAN GUN. Sim personeli
        // sayi olarak tutuyor, ama deneyim sayilamaz - kim ne kadar suredir
        // burada, o kisiye ait. Dizinin ilk _cooks / _salon elemani gecerli.
        // Ise alim sona ekler, cikarma sondan alir: en yeni giden. Aksi halde
        // "en deneyimliyi kov" diye bir karar ortaya cikardi ki anlamsiz.
        private readonly int[] _cookXpDays = new int[MaxServers];
        private readonly int[] _salonXpDays = new int[MaxServers];

        // --- Huy ve moral, docs/14 -------------------------------------------
        // Her personele havuzdan IKI huy dusuyor; cakisan ikili cikmiyor.
        // Moral 0-100, yeni personel 70 ile basliyor.
        private readonly int[] _cookTraitA = new int[MaxServers];
        private readonly int[] _cookTraitB = new int[MaxServers];
        private readonly int[] _salonTraitA = new int[MaxServers];
        private readonly int[] _salonTraitB = new int[MaxServers];
        /// <summary>
        /// Personelin isim havuzundaki sirasi.
        ///
        /// Isim OYUNUN KURALLARINA girmiyor; yalnizca oyuncunun bagini
        /// kuruyor. Yine de durumda tutuluyor ve kaydediliyor: "Nurten
        /// Abla" ikinci acilista baska biri olursa bag da gider.
        /// </summary>
        private readonly int[] _cookName = new int[MaxServers];
        private readonly int[] _salonName = new int[MaxServers];

        private readonly int[] _cookMorale = new int[MaxServers];
        private readonly int[] _salonMorale = new int[MaxServers];
        /// <summary>Bu grubu son hangi garson agirladi; huyun memnuniyete etkisi icin.</summary>
        private readonly int[] _pServer = new int[MaxParties];
        /// <summary>Bu grubun yemegini son hangi asci pisirdi.</summary>
        private readonly int[] _pCook = new int[MaxParties];
        /// <summary>Ust uste yogun gun sayaci. docs/14: gunde -3 moral.</summary>
        private int _busyStreak;

        // --- Ise alim adaylari, docs/14 --------------------------------------
        // "Ise alim ekraninda ayni anda uc aday gorunur. Adaylar uretilir:
        //  rol, iki huy, gorunum, isim. Aday havuzu her uc gunde bir
        //  yenilenir."
        //
        // Bu havuz olmadan huy bir PIYANGO: oyuncu kimi aldigini bilmiyor.
        // Olcum bunu gosterdi - kor ise alim, fast food'da iyi oyuncunun
        // itibarini 96,5'ten 87'ye indirdi, cunku pahali bir kadro cekmek
        // butceyi daraltiyor ve daralan butce servisi duzeltecek kisiyi
        // almayi engelliyordu. docs/14'un SECIM'i tam olarak bunun cevabi.
        public const int CandidateSlots = 3;
        private readonly int[] _candTraitA = new int[CandidateSlots * 2];
        private readonly int[] _candTraitB = new int[CandidateSlots * 2];
        private int _candDay = -1;

        private readonly long[] _dishPrice;      // oyuncunun belirledigi fiyat
        private readonly bool[] _dishOnMenu;

        /// <summary>
        /// Bu yemek DUN acik miydi. Yalnizca acilisi duyurmak icin;
        /// oyunun kurallarina girmiyor.
        /// </summary>
        private readonly bool[] _dishWasUnlocked;

        /// <summary>
        /// Bu istasyonu kullanan bir yemek var mi. Mutfak yuklenirken bir
        /// kez hesaplaniyor.
        /// </summary>
        private readonly bool[] _stationUsed;

        // ---- stok (hal asamasi) --------------------------------------------
        // docs/02 gun dongusu: malzeme sabah HAL'den pesin alinir.
        // Bu olmadan restoran kendi kendini isletiyordu ve hic mudahale
        // etmeyen oyuncu altmis gunu karla kapatiyordu. Ihmalin bedeli budur.
        private readonly int[] _stockGrams;

        /// <summary>
        /// Malzemenin YASI, gun. Soguk hava olmadan anlamsiz: bozulabilir
        /// her sey gece olyor. Soguk hava kademesi geldiginde malzemenin
        /// kendi raf omrunun bir kismi kadar yasayabiliyor.
        ///
        /// Yas parti basina degil MALZEME basina tutuluyor; alim yapinca
        /// AGIRLIKLI ORTALAMA aliniyor. Basit "alinca sifirla" kurali bir
        /// istismar aciyordu: her gun bir gram alip saati sonsuza kadar
        /// sifirda tutabiliyordun.
        /// </summary>
        private readonly int[] _stockAgeDays;

        /// <summary>Sahip olunan soguk hava kademesi. 0 = yok.</summary>
        private int _storageTier;

        /// <summary>
        /// BUGUNKU hal fiyat carpani, malzeme basina, baz puan.
        ///
        /// docs/12 3: "erken alim avantaji yok, stok bozuluyor. Ucuz gune
        /// denk gelmek sans degil, takip meselesi." Icerikte
        /// priceVolatilityBp yaziliydi ve hicbir yerde okunmuyordu; hal
        /// her gun ayni fiyati veriyor, yani takip edilecek bir sey yoktu.
        ///
        /// Gun basinda kuruluyor ve gun boyunca sabit: oyuncu sabah
        /// fiyatlari gorup karar veriyor.
        /// </summary>
        private readonly int[] _marketBp;

        /// <summary>
        /// Halden alinan malzemenin kalite kademesi: 0 dusuk, 1 standart,
        /// 2 yuksek. TEK bir kuresel ayar, malzeme basina secim yok.
        ///
        /// Sebep icerikte: en hassas alti malzemenin hepsi et, tuz ile
        /// karabiber neredeyse duyarsiz. Yani tek ayar bile yemege gore
        /// farkli sonuc veriyor ve oyuncuya 77 karar yuklenmiyor.
        /// </summary>
        private int _quality = 1;

        /// <summary>
        /// Stoktaki malzemenin ORTALAMA kalite etkisi, santi-puan.
        /// Alim yapinca agirlikli ortalama aliniyor; ucuz alip sonra
        /// pahali alan, elindeki ucuz maldan hemen kurtulamiyor.
        /// </summary>
        private readonly int[] _stockQualityCenti;
        private int _stockOutEvents;
        private int _turnedAwayParties;

        /// <summary>
        /// Kapidan donen musterinin memnuniyeti. Notr esik 6000; bunun
        /// altinda ama sifira yakin degil. "Hicbir sey yokmus" kotu bir
        /// deneyim, ama "kirk dakika bekledim" kadar degil.
        /// </summary>
        private const int TurnAwaySatisfactionCenti = 4000;

        // ---- masalar -------------------------------------------------------
        private readonly int[] _tableParty;      // -1 bos
        private readonly bool[] _tableDirty;

        // ---- TABAK DONGUSU -------------------------------------------------
        //
        // Lokantada SAYILI tabak var ve donuyor:
        //
        //   temiz  -> (asci yemegi tabakliyor)  -> kullanimda
        //   kullanimda -> (garson masayi topluyor) -> kirli
        //   kirli  -> (lavaboda yikaniyor)      -> temiz
        //
        // Toplam degismez: temiz + kullanimda + kirli = kademe tabagi.
        // Bu bir DEGISMEZ ve test onu boyle sinar - sizan bir tabak,
        // servisi yavas yavas durduran ve sebebi gorunmeyen bir hata olur.
        //
        // Neden sayili: docs/14'un bulasikci gerekcesi. Temiz tabak
        // bitince asci pisen yemegi tabaga koyamiyor ve servis DURUYOR.
        // Oyuncuya "onemsiz gorunen seyi ihmal etme" dersini veren sey bu.
        private int _platesClean;
        private int _platesDirty;
        private int _platesInUse;

        /// <summary>Masada duran tabak sayisi; masa toplaninca lavaboya gider.</summary>
        private readonly int[] _tablePlates;

        /// <summary>Grubun elindeki tabak sayisi.</summary>
        private readonly int[] _pPlates;

        /// <summary>Yemegi PISTI ama tabak bekliyor.</summary>
        private readonly bool[] _pCooked;

        /// <summary>Temiz tabak olmadigi icin bekleyen tick sayisi (gunluk).</summary>
        private int _plateBlockedTicks;

        // ---- gruplar -------------------------------------------------------
        private readonly bool[] _pActive;
        private readonly int[] _pArchetype;
        private readonly int[] _pDishMain;
        private readonly int[] _pDishSide;    // -1 yok
        private readonly int[] _pDishDrink;   // -1 yok

        /// <summary>
        /// Tatli. Bu alan yazilana kadar tatli grubu OLU icerikti: fast
        /// food'da 6, Turk lokantasinda 3 tatli yemegi vardi ve hicbiri
        /// siparis edilemiyordu. docs/27 3.3 zirve tablosu tatliya 0,08
        /// es zamanli tabak veriyor, ekipman merdiveninde tatli
        /// istasyonunun bir yukseltmesi var; ikisi de bosa calisiyordu.
        /// </summary>
        private readonly int[] _pDishDessert; // -1 yok

        /// <summary>
        /// Musterinin SORDUGU ama restoranin yapamadigi yemek. -1 yok.
        ///
        /// Kilit sistemi yazilinca olculdu ki kilitli yemek PASIFLIGI
        /// odullendiriyor: yapilamayan yemegin stok masrafi da yok, yani
        /// yatirim yapmayan oyuncu bedavaya kar ediyordu. Bu alan o bedavayi
        /// kaldiriyor: gunu ve itibari gelmis ama EKIPMANI alinmamis yemegi
        /// musteri gelip soruyor, bulamayinca memnuniyeti dusuyor.
        /// </summary>
        private readonly int[] _pAskedDish;
        private readonly int[] _pSize;
        private readonly CustomerStage[] _pStage;

        /// <summary>
        /// Patron bu masayla BIZZAT ilgilendi mi - siradaki salon isi
        /// icin. Is yapilinca temizleniyor: ilgi bir ADIM, surekli bir
        /// hal degil.
        /// </summary>
        private readonly bool[] _pAttended;
        private readonly int[] _pTable;
        private readonly int[] _pPatienceLeftMs;
        private readonly int[] _pPatienceTotalMs;
        private readonly int[] _pWaitedMs;
        private readonly int[] _pEatLeftMs;
        private readonly int[] _pSatisfactionCenti;
        private readonly int[] _pBonusCenti;     // ozur, ikram, patron ilgisi

        /// <summary>
        /// Bugun kalan patron mudahalesi hakki. docs/02 59: gun basina 3-5.
        /// Sinir olmadan her kizgin musteri bedava kurtarilabiliyordu.
        /// </summary>
        private int _interventionsLeft;
        private readonly bool[] _pInTask;
        private readonly bool[] _pWarned;
        private int _partyCount;

        // ---- istasyonlar ve ekipman ----------------------------------------
        // docs/27 Karar D: prepMs yemegin DUVAR SAATI suresi, asci
        // mesguliyeti prepMs x attendBp / 10000. Ekipman yukseltmesi ya
        // yuva ekler ya attendBp dusurur, prepMs'e dokunmaz.
        //
        // Bu yazilana kadar simulasyon asciyi butun duvar saati boyunca
        // mesgul tutuyordu, yani firin ile ocak arasinda hicbir fark yoktu
        // ve ekipmanin anlatacagi hikaye de yoktu.
        private readonly int[] _stationTier;    // sahip olunan ekipman kademesi
        private readonly int[] _stationBusy;    // o an dolu yuva

        /// <summary>Bir grubun en fazla kac istasyon isi olabilir: ana, yan, icecek.</summary>
        /// <summary>Ana, yan, icecek, tatli: dordu de ayri istasyon olabilir.</summary>
        private const int MaxJobsPerParty = 4;

        private readonly int[] _jobStation;     // MaxParties x 3, -1 bos
        private readonly int[] _jobMs;          // kalan duvar saati
        private readonly int[] _jobState;       // 0 bekliyor, 1 pisiyor, 2 bitti
        private readonly int[] _jobPlates;      // kac tabak
        private readonly int[] _jobSlots;       // is baslarken kac yuva tuttu
        private readonly int[] _pJobsLeft;

        // ---- is havuzlari --------------------------------------------------
        private readonly TaskKind[] _salonTaskKind;
        private readonly int[] _salonTaskTarget;
        private readonly int[] _salonTaskLeftMs;
        private readonly TaskKind[] _kitchenTaskKind;
        private readonly int[] _kitchenTaskTarget;
        private readonly int[] _kitchenTaskLeftMs;

        // ---- gelis plani ---------------------------------------------------
        private readonly int[] _arrTick;
        private readonly int[] _arrArchetype;
        private readonly int[] _arrSize;
        private int _arrCount;
        private int _arrNext;

        /// <summary>
        /// Gun icinde her rolden kac kalem siparis edildi: 0 ana, 1 yan,
        /// 2 icecek, 3 tatli. Yalnizca olcum icin; tatli grubunun bir daha
        /// sessizce olu icerige donmemesi test bununla korunuyor.
        /// </summary>
        private readonly int[] _orderedRole = new int[4];

        // ---- gun sayaclari -------------------------------------------------
        private int _servedParties;
        private int _servedPeople;
        private int _angryParties;

        /// <summary>
        /// Bugun MASADAN kizgin ayrilan grup sayisi.
        ///
        /// _angryParties kapidan donenleri de sayiyor - masa bulamayan
        /// grup hic oturmadi, yani kriz seridinde HIC GORUNMEDI. Ikisi
        /// ayrilmazsa "kizgin musteri varsa uyari cikmis olmali"
        /// cikarimi yanlis: uyari yalnizca OTURAN bir grup icin
        /// verilebilir.
        /// </summary>
        private int _angrySeated;
        private int _salonRushWashes;
        private long _revenue;
        private long _ingredientCost;
        private long _satisfactionSum;      // santi, kisi agirlikli
        private long _revenueAll;           // kampanya boyunca, sifirlanmiyor
        private long _reputationDeltaMicro; // birikimli, 1e-6 puan
        private long _weeklyWagesPaid;
        private long _weeklyRentPaid;
        private int _firstDebtDay;          // 0 = hic borca dusmedi

        /// <summary>
        /// Batma merdivenine kac kez inildi. Yil sonu degerlendirmesinin
        /// SAGLAMLIK ekseni bunu okuyor (docs/08): ceza anlik degil
        /// birikimli.
        /// </summary>
        private int _debtRungs;

        /// <summary>
        /// BUGUN odenen ucret ve kira. Haftada bir gun dolu, digerlerinde
        /// sifir; gun raporu bunlari gosteriyor.
        /// </summary>
        private long _dayWages, _dayRent;

        /// <summary>
        /// BUGUN cope giden stogun degeri. Toplam degil gunluk: aksam
        /// raporu bugunu anlatiyor.
        /// </summary>
        private long _daySpoiled;

        /// <summary>
        /// Sezon boyunca COPE GIDEN stogun degeri, santi. Gelir
        /// tablosunun eksik dorduncu satiri.
        /// </summary>
        private long _spoiledValue;

        /// <summary>Ekipmana ve genislemeye odenen toplam, santi.</summary>
        private long _equipmentSpend, _expansionSpend;

        /// <summary>
        /// Malzemeye HARCANAN nakit, santi. IngredientCost'tan farkli:
        /// o SATILAN MALIN maliyeti (gun raporu icin), bu kasadan cikan
        /// para. Ikisini karistirmak denge aracinin gelir tablosunu
        /// gercek kasa hareketinden koparmisti.
        /// </summary>
        private long _ingredientSpend;

        /// <summary>
        /// Batma merdiveninin kasaya SOKTUGU para, santi: satilan
        /// ekipmanin yarisi, kuculmenin iadesi ve silinen borc.
        ///
        /// Gelir tablosunda ayri bir satir olmasi sart: onsuz "net" ile
        /// gercek kasa hareketi tutmuyor ve fark aciklanamiyor.
        /// </summary>
        private long _rescueValue;

        // ---- yil sonu degerlendirmesi (docs/08) -------------------------
        //
        // Bunlar oyunun KURALLARINA girmiyor; yalnizca altmisinci gunun
        // sonunda okunuyor. Kural etkisi olmayan bir sayaci durumda tutmak
        // ucuz, ve gerekcesi su: yil sonu puani gecmise bakiyor, ve gecmis
        // yalnizca biriktirilirse var.

        /// <summary>
        /// Sezon boyunca ANA YEMEK siparis eden grup sayisi ve bunlarin
        /// kacinin komboya dondugu.
        ///
        /// Imza ekseninin paydasi ile payi. Payda "butun gruplar" degil
        /// ANA YEMEK SIPARISI OLANLAR: kombo yalnizca ana yemege
        /// ekleniyor, yani tatli ya da icecek alan bir grup oyuncunun
        /// kararini olcmuyor - paydaya girerse ekseni menu bilesimi
        /// bulandirir.
        /// </summary>
        private int _mainOrders, _comboOrders;

        /// <summary>
        /// Veresiye acilirken ikram edilen cayin toplam bedeli, santi.
        ///
        /// SAYILMASI SART: para kasadan cikiyordu ve HICBIR gider
        /// kalemine yazilmiyordu, yani veresiye acan bir oyuncunun
        /// gelir tablosu mutabakati asla kapanamazdi. Denge aracinin
        /// "fark" sutunu Turk mutfaginda imzaci botta 134 sikke
        /// gosteriyordu ve o sayi tam olarak buydu.
        ///
        /// Gorunmesi de sart: veresiye BEDAVA DEGIL ve bedelinin
        /// hicbir ekranda olmamasi, mekanigi oldugundan ucuz
        /// gosteriyordu.
        /// </summary>
        private long _teaSpend;

        /// <summary>Sezon boyunca deftere yazilan toplam, santi.</summary>
        private long _creditIssued;

        /// <summary>Sezon boyunca tahsil edilen toplam, santi.</summary>
        private long _creditCollected;

        // ---- kredi (docs/12 4) ---------------------------------------------
        // Ayni anda tek kredi. Taksit haftalik, kira ve maasla ayni gun.
        private long _loanInstallment;      // haftalik taksit, santi-sikke

        /// <summary>Cekilen kredinin anaparasi toplami, santi.</summary>
        private long _loanTaken;

        /// <summary>
        /// BUTUN kredilere odenen toplam taksit, santi.
        ///
        /// _loanTotalRepaid'den farkli: o, yeni bir kredi cekilince
        /// SIFIRLANIYOR (o alanin isi "bu kredinin ne kadari odendi"
        /// sorusu). Denge aracinin mutabakati onu kullaninca ikinci
        /// krediyi ceken stratejilerde 6.750 sikkelik bir acik cikti ve
        /// acik tam olarak unutulan geri odemelerdi.
        /// </summary>
        private long _loanRepaidAll;
        private int _loanWeeksLeft;
        private long _loanTotalRepaid;

        private readonly EventBuffer _events = new EventBuffer();

        // ---- komut gunlugu (docs/23 7) --------------------------------------
        // Kayit: gun basi anlik goruntusu + o gunden beri uygulanan komutlar.
        // Yukleme, gunlugu azami hizda tekrar oynatiyor. Deterministik oldugu
        // icin sonuc kesintisiz oyunla bayt bayt ayni.
        private readonly Command[] _commandLog = new Command[MaxCommandsPerDay];
        private int _commandCount;

        // ====================================================================
        public Simulation(EconomyConfig economy, ContentSet content,
                          TimingConfig timing, ulong masterSeed)
        {
            _economy = economy ?? throw new ArgumentNullException(nameof(economy));

            // MUTFAGIN SALON HAVUZU EKONOMIYE UYGULANIYOR.
            //
            // Icerik kendi rol listesini verdiyse (cuisines/*.json:
            // salonRoles) salon yuku ve ucreti ondan geliyor. Vermediyse
            // WithSalonPool hicbir sey degistirmiyor, yani eski davranis
            // birebir korunuyor.
            if (content != null && content.SalonWorkPerCustomerMicro > 0)
                _economy = _economy.WithSalonPool(
                    content.SalonWorkPerCustomerMicro, content.SalonWageNumerator);

            // MUTFAGIN KIRASI.
            //
            // Gerekce gercekci: zincirler yuksek trafikli, pahali
            // yerlerde oturur - hacmin bedeli kira.
            //
            // AMA YON SEZGISEL DEGIL, OLCULDU (docs/52):
            //
            //   kira x1,15  makul 22.263  planci 25.094  imzaci 22.433
            //   kira x1,25  makul 23.493  planci 26.548  imzaci 22.578
            //
            // Kirayi ARTIRMAK botun kasasini ARTIRIYOR. Sebep hacim
            // carpaninda gorulen sebebin aynisi (docs/51 §5): bot
            // maliyete genislemeyerek cevap veriyor ve genislememek
            // zaten daha karli. Yani BITIS KASASI BU BOT ICIN BIR
            // ZORLUK OLCUSU DEGIL; olcu, iki mutfak ARASINDAKI FARK.
            //
            // 11500 secildi cunku farki daraltan deger o:
            //   carpansiz  %29,5  |  x1,15  %28,3  |  x1,25  %35,4
            // (Turk makul 17.351'e karsi.)
            if (content != null && content.RentMultiplierBp > 0
                && content.RentMultiplierBp != Fx.One)
            {
                TierConfig[] k = new TierConfig[_economy.TierCount];
                for (int i = 0; i < k.Length; i++)
                {
                    TierConfig t = _economy.TierAt(i);
                    k[i] = new TierConfig(
                        t.Tables,
                        Fx.Bp(t.Rent, content.RentMultiplierBp),
                        t.Upgrade, t.StaffCap, t.ReputationCapCenti, t.Plates);
                }
                _economy = _economy.WithTiers(k);
            }
            _content = content ?? throw new ArgumentNullException(nameof(content));
            _timing = timing ?? throw new ArgumentNullException(nameof(timing));
            _masterSeed = masterSeed;

            _tableParty = new int[MaxTables];
            _tableDirty = new bool[MaxTables];
            _tablePlates = new int[MaxTables];
            _pPlates = new int[MaxParties];
            _pCooked = new bool[MaxParties];

            _pActive = new bool[MaxParties];
            _pArchetype = new int[MaxParties];
            _pDishMain = new int[MaxParties];
            _pDishSide = new int[MaxParties];
            _pDishDrink = new int[MaxParties];
            _pDishDessert = new int[MaxParties];
            _pAskedDish = new int[MaxParties];
            _pSize = new int[MaxParties];
            _pStage = new CustomerStage[MaxParties];
            _pAttended = new bool[MaxParties];
            _pTable = new int[MaxParties];
            _pPatienceLeftMs = new int[MaxParties];
            _pPatienceTotalMs = new int[MaxParties];
            _pWaitedMs = new int[MaxParties];
            _pEatLeftMs = new int[MaxParties];
            _pSatisfactionCenti = new int[MaxParties];
            _pBonusCenti = new int[MaxParties];
            _pInTask = new bool[MaxParties];
            _pWarned = new bool[MaxParties];

            _salonTaskKind = new TaskKind[MaxServers];
            _salonTaskTarget = new int[MaxServers];
            _salonTaskLeftMs = new int[MaxServers];
            _stationTier = new int[content.Stations.Length];
            _stationBusy = new int[content.Stations.Length];
            _jobStation = new int[MaxParties * MaxJobsPerParty];
            _jobMs = new int[MaxParties * MaxJobsPerParty];
            _jobState = new int[MaxParties * MaxJobsPerParty];
            _jobPlates = new int[MaxParties * MaxJobsPerParty];
            _jobSlots = new int[MaxParties * MaxJobsPerParty];
            _pJobsLeft = new int[MaxParties];

            _kitchenTaskKind = new TaskKind[MaxServers];
            _kitchenTaskTarget = new int[MaxServers];
            _kitchenTaskLeftMs = new int[MaxServers];

            _arrTick = new int[MaxParties];
            _arrArchetype = new int[MaxParties];
            _arrSize = new int[MaxParties];

            _stockGrams = new int[content.Ingredients.Length];
            _stockAgeDays = new int[content.Ingredients.Length];
            _stockQualityCenti = new int[content.Ingredients.Length];
            _marketBp = new int[content.Ingredients.Length];
            for (int i = 0; i < _marketBp.Length; i++) _marketBp[i] = Fx.One;
            _dishPrice = new long[content.Dishes.Length];
            _dishOnMenu = new bool[content.Dishes.Length];
            _dishWasUnlocked = new bool[content.Dishes.Length];

            _stationUsed = new bool[content.Stations.Length];
            for (int i = 0; i < content.Dishes.Length; i++)
            {
                int st = content.Dishes[i].StationIndex;
                if (st >= 0 && st < _stationUsed.Length) _stationUsed[st] = true;
            }
            for (int i = 0; i < content.Dishes.Length; i++)
            {
                _dishPrice[i] = content.Dishes[i].Price;
                _dishOnMenu[i] = true;
            }

            ResetStreams();

            _day = 1;
            _phase = DayPhase.Morning;
            _tableCount = economy.TierAt(0).Tables;
            _platesClean = economy.TierAt(0).Plates;
            _reputationCenti = economy.StartingReputationCenti;
            _cash = economy.StartingCash;
            _cooks = 1;
            // Baslangic ascisi HUYSUZ ama moralli baslar.
            //
            // Iki ayri sey. Moral: bu satir olmadan ilk ascinin morali 0
            // ile basliyordu - istifa esiginin ALTINDA - ve restoran ikinci
            // gun ascisiz kaliyordu.
            //
            // Huy: baslangic ascisini oyuncu SECMIYOR, oyun veriyor. Ona
            // rastgele huy atmak, kampanyanin ilk gununde gorunmez bir zar
            // atmak demek - ve olculdu: kotu huylu bir baslangic ascisi
            // ceken kosuda memnuniyet 9.000’den 5.000’e siziyor, dukkan
            // dort masada kaliyor ve altmis gun boyunca toparlanamiyor.
            // Devraldigin asci SIRADAN; karakter, SECTIGIN kisilerle geliyor.
            _cookMorale[0] = _economy.StartingMorale;
            _cookTraitA[0] = -1;
            _cookTraitB[0] = -1;

            // Devraldigin ascinin da bir adi var - huyu olmasa bile.
            // Isimsiz bir insan, oyuncunun ilgilenmedigi bir sayidir.
            for (int i = 0; i < MaxServers; i++) { _cookName[i] = -1; _salonName[i] = -1; }
            if (content.StaffNames.Length > 0)
            {
                _cookName[0] = _rngName.NextInt(content.StaffNames.Length);
                _salonName[0] = _rngName.NextInt(content.StaffNames.Length);
            }

            // Aday havuzu BIRINCI GUNDE de dolu.
            //
            // Once yalnizca gun acilisinda kuruluyordu ve ilk gun hic
            // acilis yok - oyun zaten birinci gunun sabahinda basliyor.
            // Sonuc: alti aday da dizilerin sifir varsayilanini gosteriyordu,
            // yani hepsi ayni huyu IKI KEZ tasiyan ayni kisiydi. Ekranda
            // ucu de tipatip ayni gorunuyordu ve secim diye bir sey yoktu.
            for (int i = 0; i < CandidateSlots * 2; i++)
            {
                _candTraitA[i] = -1;
                _candTraitB[i] = -1;
            }
            RefreshCandidates();
            // Ilk gun ZATEN ACIK basliyor, yani OpenDay hic cagrilmiyor.
            // Hakki yalnizca orada kurmak, birinci gunu haksiz birakiyordu.
            // DEFTER SAHIPLERI -1 ILE BASLIYOR.
            //
            // int dizisinin varsayilani 0 ve 0 gecerli bir duzenli
            // musteri indisi: bos bir hesap, hic tanimadigi birinin
            // guvenini kullanirdi. Sessiz olurdu.
            for (int i = 0; i < MaxTabs; i++) _tabRegular[i] = -1;

            _interventionsLeft = economy.InterventionsPerDay;   // kurulusta masa sayisi taban

            // DEVRALDIGIN KADRO: BIR ASCI, BIR GARSON.
            //
            // Onceden salon BOSTU ve birinci gun butun servisi patron tek
            // basina yapiyordu. Iki sebeple yanlis:
            //
            //   1. Oyun "patronsun, sef degilsin" diyor ama acilista
            //      oyuncunun gordugu sey tek kisilik bir dukkan - kendisi.
            //      Devralinan bir lokantanin bir garsonu olur.
            //   2. Ogretici acidan: garsonun ne yaptigini gormeden
            //      "garson tuttum" kararinin ne ise yaradigi anlasilmiyor.
            //      Birinci gun gorulen sey, sonradan cogaltilacak sey
            //      olmali.
            //
            // Ascida oldugu gibi: morali BASLANGIC MORALI (yoksa istifa
            // esiginin altinda dogar ve ikinci gun salon bosalir), huyu
            // YOK (devralinan personel siradan; karakter SECTIGIN
            // kisilerle geliyor), ama adi var.
            _salon = 1;
            _salonMorale[0] = _economy.StartingMorale;
            _salonTraitA[0] = -1;
            _salonTraitB[0] = -1;

            // Acilis stogu: ilk gunu cikarmaya yeter, ikinci gun icin
            // oyuncunun hal'e gitmesi gerekir.
            //
            // BIR GUNLUK, MENUYE GORE. Onceden yetmis yedi malzemenin
            // HEPSINE altisar kilo konuyordu - menude olmayan yemeklerin
            // malzemesi, daha kilidi acilmamis yemeklerin malzemesi,
            // hepsi. Kirk dordu bozulabilir oldugu icin tamami birinci
            // gece cope gidiyordu: 14.713 sikke, oyuncunun baslangic
            // kasasinin uc katindan fazla.
            //
            // Gorunmedigi surece zararsiz sanildi. Aksam raporuna "cope
            // giden" satiri eklenince ortaya cikti: oyuncu daha ilk
            // gunun sonunda, hicbir sey satin almadan, kasasindan buyuk
            // bir zayiat rakami gorecekti.
            RestockForOneDay();

            // Birinci gunun acik yemekleri DUYURULMUYOR: oyuncu onlari
            // zaten menude goruyor. Bayragi simdi doldurmak, ilk gun
            // ekranin on yedi "yeni yemek" bildirimiyle dolmasini
            // engelliyor.
            for (int i = 0; i < _content.Dishes.Length; i++)
                _dishWasUnlocked[i] = Unlocked(i);

            // Ilk gunun hal fiyatlari da OYNASIN.
            //
            // Once RollMarket yalnizca gun acilisinda cagriliyordu ve ilk
            // gun hic acilis yok - oyuncunun gordugu ilk ekonomi ekraninda
            // "bugunku fiyat" ile "yil ortalamasi" her satirda birbirinin
            // ayniydi. Iki sutunun neden var oldugu anlasilmiyordu.
            RollMarket();

            for (int i = 0; i < MaxTables; i++) _tableParty[i] = -1;

            // SIFIRINCI HAFTANIN FOTOGRAFI.
            //
            // Ilk karnenin farki, bu satir olmadan eksenlerin KENDISI
            // olurdu: gecen hafta sifir sayilir ve oyuncu yedinci gunde
            // "Mekan +33" gibi, kendisinin yapmadigi bir sicrama gorurdu.
            // Devraldigi dukkanin puani onun kazanci degil.
            SeasonScore acilis = Score();
            for (int i = 0; i < SeasonScore.AxisCount; i++)
            {
                _weekAxis[i] = acilis.AxisAt(i);
                _weekAxisPrev[i] = _weekAxis[i];
            }
        }

        private void ResetStreams()
        {
            _rngArrival = RngSeeder.Create(_masterSeed, RngStream.Arrival);
            _rngArchetype = RngSeeder.Create(_masterSeed, RngStream.Archetype);
            _rngOrder = RngSeeder.Create(_masterSeed, RngStream.Order);
            _rngStaffError = RngSeeder.Create(_masterSeed, RngStream.StaffError);
            _rngMarket = RngSeeder.Create(_masterSeed, RngStream.Market);
            _rngCredit = RngSeeder.Create(_masterSeed, RngStream.Credit);
            _rngRegular = RngSeeder.Create(_masterSeed, RngStream.Regular);
            _rngHiring = RngSeeder.Create(_masterSeed, RngStream.Hiring);
            _rngEvent = RngSeeder.Create(_masterSeed, RngStream.Event);
            _rngName = RngSeeder.Create(_masterSeed, RngStream.Name);
        }

        // ---- okuma yuzeyi --------------------------------------------------
        public long TickIndex { get { return _tickIndex; } }
        public int Day { get { return _day; } }
        public DayPhase Phase { get { return _phase; } }
        public int ServiceTick { get { return _serviceTick; } }

        /// <summary>
        /// Servisin ilerlemesi, ON BINDE (0-10000).
        ///
        /// TAMSAYI: cekirdek kayan nokta alip vermiyor (docs/23) ve bir
        /// test bunu makineyle kontrol ediyor - ilk yazim float
        /// donduruyordu ve test HAKLI olarak kirmiziya dustu. Orani
        /// kayan noktaya cevirmek gorunumun isi.
        ///
        /// Gorunum bunu gunun saatine ceviriyor (DayLight): golgelerin
        /// yonu, isigin rengi ve sokak lambalari bu tek sayidan
        /// okunuyor.
        /// </summary>
        public int ServiceProgressBp
        {
            get
            {
                int n = _timing.ServiceTicks;
                if (n <= 0) return 0;
                long t = (long)_serviceTick * 10000L / n;
                return t < 0L ? 0 : (t > 10000L ? 10000 : (int)t);
            }
        }
        public int TableCount { get { return _tableCount; } }

        /// <summary>Su an dolu masa sayisi. Gorunum ve muzik icin.</summary>
        public int OccupiedTables
        {
            get
            {
                int n = 0;
                for (int t = 0; t < _tableCount; t++) if (_tableParty[t] >= 0) n++;
                return n;
            }
        }

        /// <summary>Bu masada oturan bir grup var mi. Gorunum katmani icin.</summary>
        public bool TableOccupied(int table)
        {
            return table >= 0 && table < _tableCount && _tableParty[table] >= 0;
        }

        /// <summary>
        /// Bu masada kac kisi oturuyor. Gorunum katmani sandalyeye figur
        /// oturtmak icin kullaniyor: her dolu masaya tek figur koymak,
        /// dort kisilik bir grubu tek kisi gibi gosteriyordu.
        /// </summary>
        public int TableGuests(int table)
        {
            if (table < 0 || table >= _tableCount) return 0;
            int p = _tableParty[table];
            return p >= 0 ? _pSize[p] : 0;
        }

        /// <summary>
        /// Bu masadaki grubun ASAMASI. Bos masada None.
        ///
        /// Gorunum katmani her masanin uzerine durumunu ciziyor. Bu
        /// olmadan servis asamasi izlenemiyordu: sekiz dakika boyunca
        /// ekranda hicbir sey kipirdamiyor, ust seritteki iki sayi
        /// disinda hicbir bilgi akmiyordu.
        /// </summary>
        /// <summary>
        /// Bu masada oturan GRUBUN indisi. -1 masa bossa.
        ///
        /// IKI AYRI INDIS UZAYI VAR ve karistirmak sessiz bir hata
        /// uretiyor: masa 0..15 (MaxTables), grup 0..255 (MaxParties).
        /// Arayuz masaya dokunuyor, Intervene ise GRUP bekliyor. Ceviri
        /// olmadan "3. masaya cay ikram et" komutu 3 NUMARALI GRUBA
        /// gidiyor - gun ilerledikce bambaska bir masa, ya da coktan
        /// cikmis bir grup; o zaman komut sessizce reddediliyor ve
        /// oyuncu yine "ikram edildi" yazisini okuyor.
        /// </summary>
        public int PartyAtTable(int table)
        {
            if (table < 0 || table >= _tableCount) return -1;
            return _tableParty[table];
        }

        public CustomerStage TableStage(int table)
        {
            if (table < 0 || table >= _tableCount) return CustomerStage.None;
            int p = _tableParty[table];
            return p >= 0 ? _pStage[p] : CustomerStage.None;
        }

        /// <summary>
        /// Bu masadaki grubun KALAN SABRI, baz puan (10000 = dolu).
        ///
        /// Sabir uyarisi zaten olay olarak uretiliyordu ve hicbir yerde
        /// gorunmuyordu; oysa oyuncunun mudahale kararini verecegi tek
        /// bilgi bu.
        /// </summary>
        /// <summary>
        /// Sabrin "kritik" sayildigi esik, baz puan.
        ///
        /// Gorunum bunu kriz seridini kurmak icin soruyor: hangi masanin
        /// sabri bitmek uzere. Esigi arayuze ikinci kez yazmak, denge
        /// degisince sessizce ayrisirdi.
        /// </summary>
        public int PatienceWarnBp { get { return _timing.PatienceWarnBp; } }

        /// <summary>
        /// Su an sabri UYARI ESIGININ altina inmis bir grup var mi.
        ///
        /// Denge araci icin: "mudahale kazandiriyor mu" sorusunun
        /// cevabi, haklarin NE ZAMAN harcandigina bagli. Bot onlari
        /// gunun ilk seksen saniyesinde yakarken olculen sey mekanik
        /// degil, botun kotu oynamasiydi.
        /// </summary>
        public bool AnyPartyCritical
        {
            get
            {
                for (int i = 0; i < MaxParties; i++)
                {
                    if (!_pActive[i] || _pPatienceTotalMs[i] <= 0) continue;
                    if (DrainRateBp(i) <= 0) continue;
                    long kalanBp = Fx.MulDiv(_pPatienceLeftMs[i], Fx.One,
                                             _pPatienceTotalMs[i]);
                    if (kalanBp <= _timing.PatienceWarnBp) return true;
                }
                return false;
            }
        }

        public int TablePatienceBp(int table)
        {
            if (table < 0 || table >= _tableCount) return 0;
            int p = _tableParty[table];
            if (p < 0 || _pPatienceTotalMs[p] <= 0) return 0;

            long bp = (long)_pPatienceLeftMs[p] * Fx.One / _pPatienceTotalMs[p];
            if (bp < 0) bp = 0;
            if (bp > Fx.One) bp = Fx.One;
            return (int)bp;
        }

        /// <summary>Bu masa toplanmayi bekliyor mu.</summary>
        public bool TableDirty(int table)
        {
            return table >= 0 && table < _tableCount && _tableDirty[table];
        }
        public int ReputationCenti { get { return _reputationCenti; } }

        /// <summary>
        /// Bu kademede itibarin cikabilecegi en yuksek deger. Arayuz
        /// bunu gostermeli: tavana dayanan oyuncu, neden artik
        /// yukselmedigini bilmeli.
        /// </summary>
        public int ReputationCapCenti
        {
            get
            {
                int cap = _economy.TierForTables(_tableCount).ReputationCapCenti;
                return cap <= 0 || cap > 10000 ? 10000 : cap;
            }
        }
        /// <summary>
        /// Tavanda KAYBOLAN itibar burada birikiyor, santi-puan.
        ///
        /// Tavan dogru bir fikir - dort masalik bir dukkan semtin
        /// konustugu lokanta olamaz - ama tasan degeri SILMEK bir sey
        /// daha yapiyordu: tavandaki oyuncu icin MUKEMMEL bir gun ile
        /// IDARE EDEN bir gun arasinda olculebilir fark kalmiyordu.
        /// Olculdu: iyi oynayan yedi masada 75'e dayanip otuz iki gun
        /// orada duruyor - kampanyanin yarisindan fazlasi karsiliksiz.
        ///
        /// Ayni kural fiyat aciginda olculmustu: BIR EKSEN
        /// KIRPILIYORSA, O EKSENE ODENEN HER BEDEL TAVANIN USTUNDE
        /// BEDAVADIR. Burasi onun ters yonu - tavanin ustunde
        /// KAZANILAN da bedava veriliyordu.
        /// </summary>
        private int _reputationOverflowCenti;

        /// <summary>Tavanda biriken itibar. Arayuz ve tur icin.</summary>
        public int ReputationOverflowCenti { get { return _reputationOverflowCenti; } }

        // =====================================================================
        // NISANLAR VE HAFTALIK KARNE
        //
        // Ikisi ayni yeri kapatiyor: oyun yedi eksende puan veriyordu ve
        // oyuncu onlari TAM BIR KEZ goruyordu - altmisinci gunde.
        // Goremedigin bir seyde ilerleme hissedemezsin. Haftalik karne
        // tek basari anini dokuza cikariyor; nisanlar da aradaki gunlerde
        // basarilani ADIYLA soyluyor.
        //
        // Neden gunluk degil HAFTALIK: gunluk gurultu olurdu (eksenler
        // bir gunde kipirdamiyor) ve oyunun kendi ritmi zaten haftalik -
        // ucret ve kira haftalik odeniyor, zirve haftada iki gun.
        // =====================================================================

        /// <summary>Kazanilmis nisanlarin bit maskesi.</summary>
        private int _badges;

        /// <summary>BUGUN kazanilanlar. Aksam ekrani bunu vurguluyor.</summary>
        private int _badgesToday;

        /// <summary>
        /// Defter bir kez acildi mi. "Defter kapandi" nisani bunsuz
        /// SESSIZCE yanlis olurdu: hic veresiye vermemis oyuncunun da
        /// acik veresiyesi sifirdir, yani nisan ilk gun kendiliginden
        /// dagitilirdi.
        /// </summary>
        private bool _creditEverOpened;

        /// <summary>Bu haftanin eksenleri; hafta sonunda dolduruluyor.</summary>
        private readonly int[] _weekAxis = new int[SeasonScore.AxisCount];

        /// <summary>Gecen haftanin eksenleri. Fark bu ikisinden cikiyor.</summary>
        private readonly int[] _weekAxisPrev = new int[SeasonScore.AxisCount];

        /// <summary>Karnenin cikarildigi gun; 0 ise hic cikmadi.</summary>
        private int _weekReportDay;

        public int BadgeCount { get { return Badges.Count; } }

        /// <summary>Nisan kazanildi mi.</summary>
        public bool HasBadge(int i)
        {
            return i >= 0 && i < Badges.Count && (_badges & (1 << i)) != 0;
        }

        /// <summary>Nisan BUGUN mu kazanildi.</summary>
        public bool BadgeEarnedToday(int i)
        {
            return i >= 0 && i < Badges.Count && (_badgesToday & (1 << i)) != 0;
        }

        /// <summary>Kac nisan kazanildi.</summary>
        public int BadgesEarned
        {
            get
            {
                int n = 0;
                for (int i = 0; i < Badges.Count; i++) if (HasBadge(i)) n++;
                return n;
            }
        }

        /// <summary>
        /// Haftalik karne bu gun cikti mi. Aksam ekrani bunu soruyor;
        /// "Day % 7 == 0" diye sormak YANLIS olurdu, cunku karne gun
        /// KAPANISINDA cikiyor ve oyuncu onu aksam ekraninda goruyor.
        /// </summary>
        public bool WeekReportReady { get { return _weekReportDay > 0 && _weekReportDay == _day; } }

        /// <summary>Karnenin kacinci hafta oldugu.</summary>
        public int WeekNumber { get { return _weekReportDay / 7; } }

        public int WeekAxis(int i)
        {
            return i >= 0 && i < SeasonScore.AxisCount ? _weekAxis[i] : 0;
        }

        /// <summary>Gecen haftaya gore fark. Karnenin butun anlami bu.</summary>
        public int WeekAxisDelta(int i)
        {
            return i >= 0 && i < SeasonScore.AxisCount
                ? _weekAxis[i] - _weekAxisPrev[i] : 0;
        }

        public long Cash { get { return _cash; } }
        public int Cooks { get { return _cooks; } }
        public int SalonStaff { get { return _salon; } }

        /// <summary>Lavaboya adanmis salon calisani sayisi.</summary>
        public int Dishwashers { get { return _dishwashers; } }

        /// <summary>Temiz tabak sayisi.</summary>
        public int PlatesClean { get { return _platesClean; } }

        /// <summary>Lavaboda bekleyen kirli tabak sayisi.</summary>
        public int PlatesDirty { get { return _platesDirty; } }

        /// <summary>Masalarda ve serviste olan tabak sayisi.</summary>
        public int PlatesInUse { get { return _platesInUse; } }

        /// <summary>Lokantanin toplam tabagi.</summary>
        public int PlatesTotal
        {
            get { return _economy.TierForTables(_tableCount).Plates; }
        }

        /// <summary>
        /// Temiz tabak olmadigi icin mutfagin bekledigi tick sayisi.
        ///
        /// "Bulasikci ihmal edildi" cumlesinin OLCUSU. Sifirdan buyukse
        /// servis gercekten durmus demektir.
        /// </summary>
        public int PlateBlockedTicks { get { return _plateBlockedTicks; } }

        /// <summary>Bugun yikanan tabak.</summary>
        public int PlatesWashedToday { get { return _washedToday; } }

        /// <summary>
        /// Salon personelinin BUGUN kac kez isini birakip lavaboya
        /// kostugu. Bulasikcinin olculebilir etkisi bu sayida.
        /// </summary>
        public int SalonRushWashes { get { return _salonRushWashes; } }

        /// <summary>Bugun kirlenen tabak (birikmeli).</summary>
        public int PlatesDirtiedToday { get { return _dirtiedToday; } }
        public int ActiveParties { get { return _partyCount; } }
        public int ServedParties { get { return _servedParties; } }
        public int ServedPeople { get { return _servedPeople; } }
        /// <summary>
        /// Su an SABRI ISLEYEN grup sayisi - yani bir sey bekleyen.
        ///
        /// Cayin hedefi bu kume: cay salona gidiyor ve bekleyeni
        /// olmayan bir salonda gonderilecek kimse yok. Arayuz de
        /// bunu okuyup dugmeyi kapatiyor, yoksa oyuncu bos salonda
        /// mudahale hakki yakmaya calisirdi.
        /// </summary>
        public int WaitingParties
        {
            get
            {
                int n = 0;
                for (int i = 0; i < MaxParties; i++)
                    if (_pActive[i] && DrainRateBp(i) > 0) n++;
                return n;
            }
        }

        public int AngryParties { get { return _angryParties; } }

        /// <summary>Bugun masadan kizgin ayrilan grup sayisi.</summary>
        public int AngrySeatedParties { get { return _angrySeated; } }

        /// <summary>
        /// Menude yapabilecegi bir sey bulamayip KAPIDAN DONEN grup.
        ///
        /// Gun raporunda vardi, servis sirasinda SORULAMIYORDU - yani
        /// ilk haftanin en sik olum bicimi oyuncuya ancak gun bitince
        /// gorunuyordu, hala duzeltebilecegi saatlerde degil.
        /// </summary>
        public int TurnedAwayParties { get { return _turnedAwayParties; } }
        public long Revenue { get { return _revenue; } }

        /// <summary>
        /// Bugunku ortalama memnuniyet (santi). Kisi agirlikli.
        ///
        /// Gun raporu bunu zaten hesapliyordu ama YALNIZCA gun sonunda:
        /// oyuncu gun boyunca nasil gittigini hicbir yerden okuyamiyordu.
        /// Ayni bolme, servis sirasinda da anlamli - kimse servis
        /// edilmediyse 0.
        /// </summary>
        public int AverageSatisfactionCenti
        {
            get
            {
                return _servedPeople > 0
                    ? (int)(_satisfactionSum / _servedPeople) : 0;
            }
        }
        public int PlannedArrivals { get { return _arrCount; } }
        public EventBuffer Events { get { return _events; } }
        public long TotalWagesPaid { get { return _weeklyWagesPaid; } }

        /// <summary>
        /// KAMPANYA BOYUNCA toplam ciro. _revenue her gun sifirlaniyor.
        ///
        /// Denge araci ciroyu gun raporlarindan TOPLUYORDU ve bu bir
        /// sinir artigi uretiyordu: AdvanceToNextDay() vadesi gelen
        /// veresiyeyi tahsil ediyor, ama o cagri gunun raporu ALINDIKTAN
        /// sonra calisiyor - yani son gunun tahsilati hicbir rapora
        /// girmiyordu ve gelir tablosunun mutabakati imzaci oyuncuda
        /// kapanmiyordu. Ucret ve kira zaten kumulatif okunuyordu; ciro
        /// da oyle okunmali.
        /// </summary>
        public long TotalRevenue { get { return _revenueAll; } }
        public long TotalRentPaid { get { return _weeklyRentPaid; } }
        /// <summary>Kasanin ilk kez eksiye dustugu gun; 0 ise hic dusmedi.</summary>
        public int FirstDebtDay { get { return _firstDebtDay; } }

        /// <summary>
        /// Kira ve maas gunune kac gun kaldi. Bugun odenecekse 0.
        ///
        /// docs/02 haftalik kirayi "baskinin metronomu" ilan ediyor:
        /// "oyuncu dorduncu gunden itibaren cuma gununu dusunmeye baslar."
        /// Oyunda cuma gunu diye bir sey YOKTU - para kasadan cikiyor,
        /// oyuncu bunu ancak sayi dustukten sonra fark ediyordu. Metronom
        /// sessizdi.
        /// </summary>
        public int DaysToRent
        {
            get
            {
                int n = _economy.RentDayInterval;
                if (n <= 0) return 0;
                int r = _day % n;
                return r == 0 ? 0 : n - r;
            }
        }

        /// <summary>Bu hafta odenecek kira ve maasin toplami.</summary>
        public long WeeklyBill
        {
            get
            {
                Crew crew = new Crew(_cooks, _salon);
                long wages = StaffingModel.WeeklyWageBill(crew, _day / 7, _economy);
                wages = Fx.MulDiv(wages, TraitWageMultiplierBp(), Fx.One);
                return wages + _economy.TierForTables(_tableCount).Rent + _loanInstallment;
            }
        }

        /// <summary>Batma merdivenine kac kez inildi.</summary>
        public int DebtRungs { get { return _debtRungs; } }

        /// <summary>Cope giden stogun toplam degeri, santi.</summary>
        public long SpoiledValue { get { return _spoiledValue; } }

        /// <summary>Malzemeye harcanan toplam nakit, santi.</summary>
        public long IngredientSpend { get { return _ingredientSpend; } }

        /// <summary>Batma merdiveninin kasaya soktugu toplam, santi.</summary>
        public long RescueValue { get { return _rescueValue; } }

        /// <summary>Cekilen kredinin anaparasi, santi.</summary>
        public long LoanTaken { get { return _loanTaken; } }

        /// <summary>Butun kredilere odenen toplam taksit, santi.</summary>
        public long LoanRepaid { get { return _loanRepaidAll; } }

        /// <summary>Ekipmana odenen toplam, santi.</summary>
        public long EquipmentSpend { get { return _equipmentSpend; } }

        /// <summary>Genislemeye odenen toplam, santi.</summary>
        public long ExpansionSpend { get { return _expansionSpend; } }

        /// <summary>Bir gunde agirlanan en yuksek kisi sayisi.</summary>
        /// <summary>
        /// Ana yemek siparislerinin yuzde kaci komboya dondu, bin-puan.
        ///
        /// Imza ekseninin ham hali. Denge araci bunu basiyor ki eksenin
        /// HEDEFI olcumden gelsin - uydurulmus bir hedef, ekseni ya
        /// doygun ya erisilmez yapar ("doygun bir eksene odenen odul
        /// gorunmez").
        /// </summary>
        /// <summary>Veresiye cayina harcanan toplam, santi.</summary>
        public long TeaSpend { get { return _teaSpend; } }

        public int ComboShareBp
        {
            get
            {
                if (_mainOrders <= 0) return 0;
                return (int)((long)_comboOrders * Fx.One / _mainOrders);
            }
        }

        /// <summary>Kampanyanin uzunlugu, gun.</summary>
        public int CampaignDays { get { return _economy.CampaignDays; } }

        /// <summary>
        /// Kampanya doldu ve degerlendirme HENUZ GOSTERILMEDI.
        ///
        /// Gorunum katmani bunu gorup ekrani aciyor ve MarkSeasonScored
        /// cagiriyor. Bayrak kayda giriyor, yani ikinci acilista ayni
        /// ekran tekrar cikmiyor.
        /// </summary>
        public bool SeasonJustEnded
        {
            get { return !_seasonScored && _day > _economy.CampaignDays; }
        }

        public void MarkSeasonScored() { _seasonScored = true; }

        /// <summary>
        /// Kampanya bitti mi. SeasonJustEnded'dan farki: bu bir daha
        /// KAPANMIYOR, yani degerlendirme ekrani menuden her zaman
        /// yeniden acilabiliyor.
        /// </summary>
        public bool SeasonOver { get { return _day > _economy.CampaignDays; } }

        /// <summary>
        /// Yil sonu degerlendirmesi. docs/08-oyun-sonu.md yedi eksen.
        ///
        /// Her eksen 0-100 ve hicbiri digerinin yerine gecmiyor: buyuyerek
        /// de, kucuk ama sevilen bir dukkan isleterek de iyi puan
        /// alinabilmeli.
        ///
        /// Olculer OYUNUN KENDI SAYILARI, ayri bir puan ekonomisi degil.
        /// Ayri bir olcek uydurmak, oyuncunun oynarken takip ettigi seyle
        /// yil sonunda odullendirilen seyi birbirinden ayirirdi.
        /// </summary>
        /// <summary>Duzenli musteri basina yazilmis sahne sayisi (docs/13).</summary>
        private const int StoryBeatsPerRegular = 3;

        public SeasonScore Score()
        {
            // --- varlik: kasa + defter + SAHIP OLUNANLAR
            //
            // YATIRIM CEZALANDIRILMIYOR ARTIK. Once yalnizca kasa ve
            // defter sayiliyordu: ekipman aldikca "Varlik" cubugu
            // KISALIYORDU, yani oyunun tesvik ettigi sey karnede ceza
            // olarak donuyordu. Oyuncunun gordugu sey suydu - dogru
            // oynadikca puani dusuyor ve sebebini hicbir ekran
            // soylemiyor.
            //
            // Sahip olunanlarin degeri, katalogun tamamindan KALANI
            // cikararak bulunuyor. Ayri bir toplama yazmadim bilerek:
            // iki ayri hesap bir gun birbirinden ayrilir ve hangisinin
            // dogru oldugu anlasilmaz (bu dosyada ayni hata bir kez
            // yasandi - RemainingPurchaseCost soguk havayi sayiyordu,
            // oteki saymiyordu).
            long yardstick = RemainingPurchaseCostTotal();
            long owned = yardstick - RemainingPurchaseCost();
            if (owned < 0) owned = 0;
            long worth = _cash + OpenCredit + owned;
            int wealth = yardstick > 0 ? (int)(worth * 100 / yardstick) : 100;

            // --- itibar: dogrudan
            int reputation = _reputationCenti / 100;

            // --- duzenli musteriler: ILISKININ DERINLIGI
            //
            // "Ugradi mi" degil "kac sahnesini actin". Ilk olcum yalnizca
            // ziyarete bakiyordu ve altmis gunde herkes en az bir kez
            // ugradigi icin eksen HERKESTE 100 cikiyordu - yani hicbir sey
            // olcmuyordu. Sahne acmak ise emek istiyor: sik gelmesi ve
            // memnun ayrilmasi gerekiyor.
            int beats = 0;
            for (int i = 0; i < RegularCount; i++)
            {
                int b = _regBeat[i];
                beats += b > StoryBeatsPerRegular ? StoryBeatsPerRegular : b;
            }
            int regulars = RegularCount > 0
                ? beats * 100 / (RegularCount * StoryBeatsPerRegular) : 0;

            // --- ekip: kadro doluluğu ve morali, yari yariya
            int cap = StaffCap;
            int head = _cooks + _salon;
            int fill = cap > 0 ? head * 100 / cap : 0;
            int crew = head > 0 ? (fill + AverageMorale()) / 2 : 0;

            // --- mekan: son kademeye gore masa
            int topTables = _economy.TierAt(_economy.TierCount - 1).Tables;
            int place = topTables > 0 ? _tableCount * 100 / topTables : 0;

            // --- saglamlik: merdivene hic inmemek tam puan
            //
            // docs/08'in en degerli yan etkisi: batma merdivenine inmek
            // artik ANLIK degil BIRIKIMLI bir bedel. Her basamak 30 puan.
            int resilience = 100 - _debtRungs * 30;
            if (_firstDebtDay > 0 && _debtRungs == 0) resilience -= 15;

            return new SeasonScore(wealth, reputation, regulars, crew,
                                   place, resilience, SignatureAxis());
        }

        /// <summary>Kampanyanin tamami satin alinabilir olsa ne tutardi.</summary>
        private long RemainingPurchaseCostTotal()
        {
            long total = 0;
            for (int t = 1; t < _economy.TierCount; t++)
                total += _economy.TierAt(t).Upgrade;

            for (int i = 0; i < _content.Stations.Length; i++)
            {
                StationDef def = _content.Stations[i];
                for (int t = 1; t < def.Tiers.Length; t++) total += def.Tiers[t].Price;
            }

            // SOGUK HAVA DA SATIN ALINABILIR BIR SEY.
            //
            // Buradan atlanmisti ve RemainingPurchaseCost() - ayni
            // soruyu soran oteki fonksiyon - onu SAYIYORDU. Iki
            // fonksiyon "geriye ne satin alinacak kaldi" sorusuna iki
            // farkli cevap veriyordu, ve yil sonu SERVET ekseninin
            // paydasi kucuk olani kullaniyordu: oyuncunun serveti,
            // satin alabileceklerinin tamamina degil bir kismina
            // oranlaniyordu.
            if (_content.Storage != null)
                for (int t = 1; t < _content.Storage.Tiers.Length; t++)
                    total += _content.Storage.Tiers[t].Price;

            return total;
        }

        private int AverageMorale()
        {
            int sum = 0, n = 0;
            for (int i = 0; i < _cooks && i < MaxServers; i++) { sum += _cookMorale[i]; n++; }
            for (int i = 0; i < _salon && i < MaxServers; i++) { sum += _salonMorale[i]; n++; }
            return n > 0 ? sum / n : 0;
        }

        /// <summary>
        /// Mutfaga ozel eksen. Olcunun KENDISI kodda, HEDEFI icerikte
        /// (content/cuisines/*.json scoreAxis) - docs/23 8.2.
        /// </summary>
        private int SignatureAxis()
        {
            ScoreAxisDef axis = _content.ScoreAxis;
            if (axis == null || axis.Target <= 0) return 100;

            switch (axis.Kind)
            {
                case "comboShare":
                    // ANA YEMEK SIPARISLERININ YUZDE KACI KOMBO OLDU.
                    //
                    // Fast food'un ekseni bir sure `peakCovers` idi ve
                    // OLCULDU ki imzayi degil GENISLEMEYI izliyordu:
                    // komboyu her sabah acan bot ile hic acmayan bot
                    // ayni puani aliyordu (38 / 38), en yuksek puanlar
                    // ise en cok masa acanlardaydi - yani eksen "Mekan"
                    // ekseninin kopyasiydi. docs/08 o eksen icin "imza
                    // mekanigini DOGRUDAN odullendirir" diyor ve
                    // soylemedigi sey buydu.
                    //
                    // Bu olcu bir KARAR olcuyor: kombo ortalama fisi
                    // yukseltiyor ama mutfak yukunu de artiriyor, yani
                    // zirvede kapatmak mesru bir oyun. Hic acmamak sifir
                    // degil ama tam puan da degil.
                    //
                    // Payda sifirsa (hic ana yemek satilmadi) puan 0:
                    // mekanigi kullanma FIRSATI dogmadiysa bile, kimseye
                    // yemek satmamis bir yil imza puani hak etmiyor.
                    if (_mainOrders <= 0) return 0;
                    int komboBp = (int)((long)_comboOrders * Fx.One / _mainOrders);
                    return komboBp * 100 / axis.Target;

                case "creditCollected":
                    // Hic veresiye acmamak tam puan DEGIL: mekanigi hic
                    // kullanmamak, onu iyi kullanmakla ayni sayilamaz.
                    if (_creditIssued <= 0) return 0;
                    int rateBp = (int)(_creditCollected * Fx.One / _creditIssued);
                    return rateBp * 100 / axis.Target;

                default:
                    return 100;
            }
        }

        private bool _seasonScored;
        public int StaffCap { get { return _economy.TierForTables(_tableCount).StaffCap; } }
        public long UpgradeCostFor(int tierIndex) { return _economy.TierAt(tierIndex).Upgrade; }
        public int TablesAtTier(int tierIndex) { return _economy.TierAt(tierIndex).Tables; }
        public int TierCount { get { return _economy.TierCount; } }
        public int DishCount { get { return _content.Dishes.Length; } }

        /// <summary>Icerikteki piyasa fiyati. Oyuncunun ayarladigi fiyat degil.</summary>
        public long BasePriceOf(int dishIndex)
        {
            if (dishIndex < 0 || dishIndex >= _content.Dishes.Length) return 0;
            return _content.Dishes[dishIndex].Price;
        }

        public CustomerStage StageOf(int party) { return _pStage[party]; }

        // =====================================================================
        // GORUNUM ICIN SALT-OKUNUR SORULAR.
        //
        // Salonda kimin nerede DURDUGUNU degil, kimin neyle MESGUL
        // oldugunu soyluyorlar. Konum gorunum katmaninin isi; simulasyon
        // metre bilmiyor ve bilmemeli (docs/23: cekirdekte Unity yok,
        // kayan nokta yok). Bu erisimciler durum DEGISTIRMIYOR.

        /// <summary>Grup hangi masada; -1 ise henuz oturmamis.</summary>
        public int TableOfParty(int party)
        {
            if (party < 0 || party >= MaxParties) return -1;
            for (int t = 0; t < _tableCount && t < MaxTables; t++)
                if (_tableParty[t] == party) return t;
            return -1;
        }

        /// <summary>
        /// Salon calisani su an hangi masayla ilgileniyor; -1 ise bosta.
        ///
        /// 0 numarali calisan PATRONUN kendisi (DispatchSalon: "patron da
        /// salonda calisiyor"), yani ekranda o da yuruyor.
        /// </summary>
        public int SalonTaskTable(int server)
        {
            if (server < 0 || server >= MaxServers) return -1;
            TaskKind k = _salonTaskKind[server];
            if (k == TaskKind.None) return -1;
            // Temizlik hedefi zaten MASA; digerlerinde hedef GRUP.
            if (k == TaskKind.Clear) return _salonTaskTarget[server];
            return TableOfParty(_salonTaskTarget[server]);
        }

        /// <summary>
        /// Bu istasyonda su an KAC TABAK pisiyor. 0 ise istasyon bosta.
        ///
        /// Gorunum bunu ocagin alevine ve firinin lambasina ceviriyor:
        /// calisan bir ocak yanmali, bos bir ocak yanmamali. Sayinin
        /// kendisi de bilgi - iki tabak pisen bir ocak ile alti tabak
        /// pisen bir ocak ayni gorunmemeli.
        /// </summary>
        public int StationLoad(int station)
        {
            if (station < 0 || station >= _stationBusy.Length) return 0;
            return _stationBusy[station];
        }

        /// <summary>
        /// Salon calisani su an YEMEK MI TASIYOR.
        ///
        /// Gorunum bunu garsonun elindeki tabaga ceviriyor: servis bir
        /// yonetim oyununun ana fiili ve goruntusu olmadan oyuncu neyin
        /// olup bittigini takip edemiyor. Hesap almak, siparis almak ve
        /// masa toplamak elde bir sey gerektirmiyor.
        /// </summary>
        /// <summary>
        /// Salon calisani su an LAVABODA mi.
        ///
        /// Gorunum icin ayri bir soru: yikamanin hedefi bir masa degil
        /// (SalonTaskTable -1 donuyor) ve -1, "bosta" ile ayni sayi.
        /// Ayirt edilmezse yikayan garson evine yollanir ve oyuncu
        /// bulasigin yikandigini hic gormez.
        /// </summary>
        public bool SalonWashing(int server)
        {
            if (server < 0 || server >= MaxServers) return false;
            return _salonTaskKind[server] == TaskKind.Wash;
        }

        public bool SalonCarrying(int server)
        {
            if (server < 0 || server >= MaxServers) return false;
            return _salonTaskKind[server] == TaskKind.Serve;
        }

        /// <summary>Asci su an hangi istasyonda calisiyor; -1 ise bosta.</summary>
        public int CookTaskStation(int cook)
        {
            if (cook < 0 || cook >= MaxServers) return -1;
            if (_kitchenTaskKind[cook] != TaskKind.Cook) return -1;
            int job = _kitchenTaskTarget[cook];
            if (job < 0 || job >= _jobStation.Length) return -1;
            return _jobStation[job];
        }
        public bool PartyActive(int party) { return _pActive[party]; }
        public int PartyArchetype(int party) { return _pArchetype[party]; }
        public long DishPrice(int dish) { return _dishPrice[dish]; }

        /// <summary>Servis penceresi doldu ve salonda kimse kalmadi.</summary>
        public bool ServiceComplete
        {
            get { return _serviceTick >= _timing.ServiceTicks && _partyCount == 0; }
        }

        // ====================================================================
        // Komutlar
        // ====================================================================
        public void Apply(in Command c)
        {
            // Gun basindan beri uygulanan her komut gunluge yaziliyor.
            // Hiz, duraklatma ve kamera komut DEGIL: onlar gorunum durumu.
            // ASAMA KOMUTLARI SINIRDAN MUAF.
            //
            // Servisi acmak ve gunu kapatmak oyuncunun "eylemi" degil,
            // gunun ilerlemesi. Onlari reddetmek yumusak kilit demek: gun
            // hic kapanmiyor, ertesi gune gecilemiyor, vadesi gelen
            // veresiye kapanmiyor. Bu tam olarak oldu ve iki imza
            // mekanigi testi bunu yakaladi.
            bool phase = c.Kind == CommandKind.OpenService || c.Kind == CommandKind.CloseDay;

            if (_commandCount < MaxCommandsPerDay)
            {
                _commandLog[_commandCount++] = c;
            }
            else if (!phase)
            {
                // RETURN SART. Once yalnizca olay basiliyor ve komut YINE DE
                // isleniyordu: durum degisiyor ama gunluge girmiyor. Bu,
                // "ayni tohum + ayni komut gunlugu = ayni durum"
                // sozlesmesini (docs/23 7) sessizce boziyordu - kayittan
                // tekrar oynatma ayrisiyordu.
                // 12 = GUNLUK KOMUT HAKKI BITTI. Arayuz bu sayiya
                // bakip sebebi soyluyor (Notices.cs), yani sayi bir
                // ARAYUZ SOZLESMESI - baska bir sebebe verilmemeli.
                Emit(SimEventKind.CommandRejected, (int)c.Kind, 12);
                return;
            }

            switch (c.Kind)
            {
                case CommandKind.OpenService: OpenService(); break;
                case CommandKind.CloseDay: CloseDay(); break;
                case CommandKind.SetPrice: SetPrice(c.A, c.B); break;
                case CommandKind.SetMenuSlot: SetMenuSlot(c.A, c.B != 0); break;
                case CommandKind.BuyEquipment: BuyEquipment(c.A); break;
                case CommandKind.BuyStorage: BuyStorage(); break;
                case CommandKind.SetQuality: SetQuality(c.A); break;
                case CommandKind.ExtendCredit: ExtendCredit(c.A); break;
                case CommandKind.CollectCredit: CollectCredit(c.A); break;
                case CommandKind.SetCombo: SetCombo(c.A != 0); break;
                case CommandKind.Hire: Hire(c.A, c.B); break;
                case CommandKind.SetDishwashers: SetDishwashers(c.A); break;
                case CommandKind.Fire: Fire(c.A, c.B); break;
                case CommandKind.OrderIngredient: OrderIngredient(c.A, c.B); break;
                case CommandKind.OrderRecommended: OrderRecommended(); break;
                case CommandKind.TakeLoan: TakeLoan(c.A); break;
                case CommandKind.Intervene: Intervene(c.A, (InterventionKind)c.B); break;
                case CommandKind.Expand: Expand(c.A); break;
                default:
                    Emit(SimEventKind.CommandRejected, (int)c.Kind, 1);
                    break;
            }
        }

        private void OpenService()
        {
            if (_phase != DayPhase.Morning)
            {
                Emit(SimEventKind.CommandRejected, (int)CommandKind.OpenService, 2);
                return;
            }
            _phase = DayPhase.Service;
            _serviceTick = 0;
            BuildArrivalPlan();
            Emit(SimEventKind.ServiceOpened, _day);
        }

        private void CloseDay()
        {
            if (_phase != DayPhase.Service)
            {
                Emit(SimEventKind.CommandRejected, (int)CommandKind.CloseDay, 2);
                return;
            }

            // Salonda kalanlar kizgin cikar.
            for (int i = 0; i < MaxParties; i++)
                if (_pActive[i]) LeaveAngry(i);

            ApplyReputation();
            SpoilPerishables();
            GainExperience();
            PayWeeklyCostsIfDue();
            UpdateMorale();

            // NISANLAR HER SEY ISLEDIKTEN SONRA bakiliyor.
            //
            // Sira onemli: "kasada ilk on bin" ucret ve kira odenmeden
            // once bakilsaydi, oyuncu haftanin faturasini odemeden once
            // bir an icin zengin gorunur ve nisani HAK ETMEDEN alirdi.
            EvaluateBadges();
            WeeklyReportIfDue();

            _phase = DayPhase.Evening;
            Emit(SimEventKind.DayClosed, _day);
        }

        /// <summary>
        /// Bugun hangi nisanlar kazanildi.
        ///
        /// Hepsi GERIYE DONUK: bakilan sey oyuncunun yaptigi, yapmasi
        /// istenen degil. Bu yuzden hicbiri oyuncunun planini bozamaz -
        /// bilerek kadrosu eksik calisan oyuncu bir gorevi kacirirdi,
        /// burada nisan aliyor.
        /// </summary>
        private void EvaluateBadges()
        {
            // Defter bir kez acildiysa bunu KALICI hatirliyoruz: nisanin
            // kosulu "acik veresiye sifir" degil, "defter acildi ve
            // kapandi". Bayrak olmadan nisan birinci gun dagitilirdi.
            if (OpenCredit > 0) _creditEverOpened = true;

            bool zirve = IsWeekend(_day);
            bool kizginYok = _angrySeated == 0;

            // 1. Zirvede kimse ac donmedi.
            if (zirve && kizginYok && _turnedAwayParties == 0 && _servedParties > 0)
                Earn(Badges.HerkesDoydu);

            // 2. Zirveyi eksik kadroyla gecti.
            //
            // "Eksik" = gereken kadronun ALTINDA. Iki havuzdan biri bile
            // eksikse sayiliyor: oyunun takasinda bir kisi eksik
            // calismak bir kisi eksik calismaktir.
            if (zirve && kizginYok && _servedParties > 0)
            {
                Crew gereken = RequiredCrewToday();
                if (_cooks < gereken.Cooks || _salon < gereken.Salon)
                    Earn(Badges.ZirveEksikKadro);
            }

            // 3. Defter kapandi.
            if (_creditEverOpened && OpenCredit == 0) Earn(Badges.DefterKapandi);

            // 4. Ilk hikaye sahnesi.
            for (int i = 0; i < RegularCount; i++)
                if (_regBeat[i] > 0) { Earn(Badges.IlkSahne); break; }

            // 5. Kasada ilk on bin.
            if (_cash >= Badges.CashMilestone) Earn(Badges.IlkOnBin);

            // 6. Dukkan buyudu.
            if (_tableCount > _economy.TierAt(0).Tables) Earn(Badges.IlkGenisleme);

            // 7. Itibar 90. Once genislemeyi gerektiriyor - itibar masa
            // kademesinin tavanina kirpiliyor.
            if (_reputationCenti >= Badges.ReputationMilestoneCenti)
                Earn(Badges.SemtinKonustugu);
        }

        /// <summary>
        /// Nisani ver - YALNIZCA ilk kez. Ikinci kez "bugun kazanildi"
        /// diye isaretlemek, oyuncuya her hafta ayni seyi yeni gibi
        /// gostermek olurdu ve nisanin degerini sifirlardi.
        /// </summary>
        private void Earn(int badge)
        {
            int bit = 1 << badge;
            if ((_badges & bit) != 0) return;
            _badges |= bit;
            _badgesToday |= bit;
            Emit(SimEventKind.BadgeEarned, badge);
        }

        /// <summary>
        /// Hafta dolduysa yedi ekseni fotografliyor.
        ///
        /// Score() mevcut durumun SAF bir fonksiyonu - kampanyanin
        /// herhangi bir gununde calisiyor, yani karne icin yeni bir
        /// hesap yazmak gerekmedi. Iki ayri hesap bir gun birbirinden
        /// ayrilirdi ve hangisinin dogru oldugu anlasilmazdi.
        /// </summary>
        private void WeeklyReportIfDue()
        {
            if (_day % 7 != 0) return;

            SeasonScore s = Score();
            for (int i = 0; i < SeasonScore.AxisCount; i++)
            {
                _weekAxisPrev[i] = _weekAxis[i];
                _weekAxis[i] = s.AxisAt(i);
            }
            _weekReportDay = _day;
        }

        /// <summary>
        /// docs/14: calisilan her gun 1 puan. Gun KAPANISINDA veriliyor,
        /// yani bugun ise alinan kisi bugunun servisini acemi gecirir.
        /// </summary>
        private void GainExperience()
        {
            int before, after;
            for (int i = 0; i < _cooks && i < MaxServers; i++)
            {
                before = _economy.XpLevelOf(_cookXpDays[i]);
                _cookXpDays[i] += XpGainOf(0, i);
                after = _economy.XpLevelOf(_cookXpDays[i]);
                if (after > before) Emit(SimEventKind.StaffLeveledUp, 0, after);
            }
            for (int i = 0; i < _salon && i < MaxServers; i++)
            {
                before = _economy.XpLevelOf(_salonXpDays[i]);
                _salonXpDays[i] += XpGainOf(1, i);
                after = _economy.XpLevelOf(_salonXpDays[i]);
                if (after > before) Emit(SimEventKind.StaffLeveledUp, 1, after);
            }
        }

        // =====================================================================
        // Moral. docs/14 "Moral" tablosu ve esikleri.
        //
        // Bu sistemin varlik sebebi docs/14'te yazili: "Batma merdiveniyle
        // baglanti: maas gecikmesi merdivenin ucuncu kademesi. Moral cokusu
        // ve istifa, batmanin somut yuzu oluyor. SAYI KAYBETMEK SOYUT,
        // ADINI BILDIGIN BIR CALISANIN ISTIFA ETMESI SOMUT."
        // =====================================================================

        /// <summary>
        /// Gunun sonunda moral: yogunluk, huy aurasi, ve istifa riski.
        /// Maas etkisi PayWeeklyCostsIfDue icinde, odeme aninda isliyor.
        /// </summary>
        private void UpdateMorale()
        {
            if (_cooks + _salon == 0) return;

            // Yogun gun = mutfak TASARIM KAPASITESININ ustunde calisti.
            //
            // Ilk tanim "masa basina uc grup" idi ve olcum reddetti: iyi
            // yonetilen bir dukkanda her gun yogun sayiliyordu, moral tek
            // yonlu dusuyordu ve butun kadro bir ayda istifa ediyordu.
            // Yogunluk mutlak bir esik degil, KADROYA GORE bir esik: ayni
            // musteri sayisi iki asciyla sakin, bir asciyla yorucu.
            long capacity = (long)_cooks * _economy.CookCapacityPerDay;
            bool busy = capacity > 0 && _servedPeople > capacity;
            _busyStreak = busy ? _busyStreak + 1 : 0;

            int busyDelta = _busyStreak >= 3 ? _economy.MoraleBusyDelta : 0;
            if (!busy) busyDelta = _economy.MoraleRecoveryDelta;

            // Huy aurasi: ekip moralini yukselten +10, huysuz -8. Kendi
            // aurasi kendine islemiyor - yoksa huysuz kendi kendini
            // dovuyor ve moralin cok altina dusuyordu.
            int aura = 0;
            for (int i = 0; i < _cooks && i < MaxServers; i++)
                aura += TraitSum(0, i, t => t.MoraleAura);
            for (int i = 0; i < _salon && i < MaxServers; i++)
                aura += TraitSum(1, i, t => t.MoraleAura);

            for (int pool = 0; pool < 2; pool++)
            {
                int count = pool == 0 ? _cooks : _salon;
                int[] morale = pool == 0 ? _cookMorale : _salonMorale;
                for (int i = 0; i < count && i < MaxServers; i++)
                {
                    int own = TraitSum(pool, i, t => t.MoraleAura);
                    // Aura gunluk degil KADEMELI: bir huysuz her gun -8
                    // vermez, ekibin havasini o kadar asagi CEKER. Onda
                    // birini uygulamak, dengeyi yillik degil haftalik
                    // olcege oturtuyor.
                    morale[i] += busyDelta + (aura - own) / 10;

                    // Toparlanma baslangic moralini GECMIYOR: sakin gunler
                    // bir personeli mutlu etmez, yalnizca normale dondurur.
                    // Ustune cikmak icin oyuncunun bir sey YAPMASI lazim
                    // (zam, izin gunu - docs/14 olay listesi).
                    if (busyDelta > 0 && morale[i] > _economy.StartingMorale)
                        morale[i] = _economy.StartingMorale;
                    ClampMorale(pool, i);
                }
            }

            RollResignations();
        }

        /// <summary>
        /// Kadronun huylarindan gelen ucret carpani, baz puan.
        /// Bir cirak ucuz, bir tecrubeli pahali; ikisi bir aradaysa fatura
        /// arada bir yerde.
        /// </summary>
        public int TraitWageMultiplierBp()
        {
            int people = _cooks + _salon;
            if (people == 0) return Fx.One;

            long sum = 0;
            for (int i = 0; i < _cooks && i < MaxServers; i++)
                sum += Fx.One + TraitSum(0, i, t => t.WageBp);
            for (int i = 0; i < _salon && i < MaxServers; i++)
                sum += Fx.One + TraitSum(1, i, t => t.WageBp);

            int bp = (int)(sum / people);
            return bp < 1000 ? 1000 : bp;      // taban: ucret sifirlanamaz
        }

        /// <summary>Butun kadronun moraline ayni miktari uygular.</summary>
        private void MoraleEvent(int delta)
        {
            for (int i = 0; i < _cooks && i < MaxServers; i++)
            { _cookMorale[i] += delta; ClampMorale(0, i); }
            for (int i = 0; i < _salon && i < MaxServers; i++)
            { _salonMorale[i] += delta; ClampMorale(1, i); }
        }

        private void ClampMorale(int pool, int index)
        {
            int[] m = pool == 0 ? _cookMorale : _salonMorale;
            if (m[index] > 100) m[index] = 100;
            if (m[index] < 0) m[index] = 0;
        }

        /// <summary>
        /// docs/14: moral 15'in altindaysa her gun %10 istifa riski.
        /// Istifa eden SONDAN degil, kendi yerinden gidiyor; kalanlar
        /// kayiyor. Yani en deneyimliyi de kaybedebilirsin.
        /// </summary>
        private void RollResignations()
        {
            for (int pool = 0; pool < 2; pool++)
            {
                int[] morale = pool == 0 ? _cookMorale : _salonMorale;
                for (int i = (pool == 0 ? _cooks : _salon) - 1; i >= 0; i--)
                {
                    if (i >= MaxServers) continue;
                    if (morale[i] >= _economy.MoraleQuitThreshold) continue;
                    if (!_rngHiring.Chance(_economy.MoraleQuitChanceBp)) continue;

                    RemoveStaff(pool, i);
                    Emit(SimEventKind.StaffResigned, pool, i);
                }
            }
        }

        /// <summary>
        /// Bir personeli listeden cikarir; sonrakiler kayiyor.
        ///
        /// AD DA KAYIYOR. Once ad dizisi kaydirilmiyordu ve bu fonksiyonun
        /// tek cagirani ISTIFA: dusuk morallu biri ayrilinca listede
        /// altindaki HERKESIN adi bir kayiyordu, ve ad kayda yazildigi
        /// icin hata kaliciydi.
        ///
        /// Bu mekanigin var olma sebebi tam da buydu: "sayi kaybetmek
        /// soyut, ADINI BILDIGIN bir calisanin istifa etmesi somut."
        /// Adlar yalan soyleyince mekanik tersine doniyordu.
        /// </summary>
        /// <summary>
        /// Grubun MUTFAKTA isi var mi.
        ///
        /// _pInTask'tan AYRI. Ikisi de "bu grupla ilgileniliyor" diye
        /// yazilmisti ama anlamlari farkli: _pInTask "garson masada"
        /// demek ve sabri DONDURUYOR; asci ocakta olmak ise musteriyle
        /// ilgilenmek degil - musteri tam da o sirada bekliyor.
        ///
        /// Olculdu: "yemek bekliyor" tiklerinin %87'sinde sabir
        /// DONMUSTU, yani DrainWaitingFoodBp = 3500 fiilen ~465 olarak
        /// isliyordu (7,5 kat zayif). Sonucu: prepMs, ekipman kademesi
        /// ve kombonun mutfak yuku musteri tarafinda neredeyse hic
        /// gorunmuyordu - "mutfak sikisti" gerilimi vardi ama bedeli
        /// yoktu.
        ///
        /// Gorev dagitimi iki bayragi da okuyor (bir grubu ayni anda
        /// iki ise almamak icin); yalnizca SABIR ayrildi.
        /// </summary>
        private readonly bool[] _pKitchenTask = new bool[MaxParties];

        private void RemoveStaff(int pool, int index)
        {
            int[] xp = pool == 0 ? _cookXpDays : _salonXpDays;
            int[] ta = pool == 0 ? _cookTraitA : _salonTraitA;
            int[] tb = pool == 0 ? _cookTraitB : _salonTraitB;
            int[] mo = pool == 0 ? _cookMorale : _salonMorale;
            int[] nm = pool == 0 ? _cookName : _salonName;
            int count = pool == 0 ? _cooks : _salon;

            for (int k = index; k < count - 1 && k + 1 < MaxServers; k++)
            {
                xp[k] = xp[k + 1];
                ta[k] = ta[k + 1];
                tb[k] = tb[k + 1];
                mo[k] = mo[k + 1];
                nm[k] = nm[k + 1];
            }
            int last = count - 1;
            if (last >= 0 && last < MaxServers)
            {
                xp[last] = 0; ta[last] = -1; tb[last] = -1; mo[last] = 0;
                nm[last] = -1;
            }
            if (pool == 0) _cooks--; else _salon--;
        }

        /// <summary>
        /// Gunluk deneyim puani. docs/14: normalde 1, cirak huyu varsa 2,
        /// tecrubeli huyu varsa 0 - "tecrubeli deneyim kazanmaz."
        /// </summary>
        private int XpGainOf(int pool, int index)
        {
            int mult = Fx.One;
            for (int slot = 0; slot < 2; slot++)
            {
                int t = StaffTrait(pool, index, slot);
                if (t < 0) continue;
                int xp = _economy.TraitAt(t).XpBp;
                if (xp != Fx.One) mult = xp;      // en belirleyici huy
            }
            return (int)Fx.MulDiv(1, mult, Fx.One);
        }

        /// <summary>
        /// Bozulabilir malzeme gunu kapatinca degerinin tamamini kaybediyor.
        /// docs/12 3: mutfaklarin risk profilini ayiran sey bu, ve soguk
        /// hava YOKKEN gecerli olan kural.
        ///
        /// Soguk hava kademesi malzemenin KENDI raf omrunun bir kismini
        /// kazandiriyor. Icerikteki spoilDays alani boylece canlaniyor:
        /// o alan yazilmisti ama simulasyon onu hic okumuyordu, yani
        /// yirmi gun dayanan sogan ile bir gun dayanan kiyma ayni gece
        /// cope gidiyordu.
        /// </summary>
        private void SpoilPerishables()
        {
            int keepBp = StorageKeepBp();
            for (int i = 0; i < _stockGrams.Length; i++)
            {
                if (_stockGrams[i] <= 0) { _stockAgeDays[i] = 0; continue; }
                if (!_content.Ingredients[i].Perishable) continue;

                _stockAgeDays[i]++;

                // Omur = KAC GUN kullanilabilir. Yas her gecenin sonunda
                // bir artiyor, yani omur 1 "yalnizca alindigi gun" demek
                // ve omur 0 ile ayni gece cope gidiyor.
                //
                // Burada bir zamanlar "soguk hava varsa en az bir gun"
                // diye bir taban vardi; hicbir sey yapmiyordu, cunku omur
                // 1 ile omur 0 ayni. Taban buyutulerek "gercek" yapilmak
                // istendi ve TEST CURUTTU: icerikteki spoilDays, TAM
                // SOGUTMADAKI raf omru. Kiymanin spoilDays'i 1, yani en
                // ust kademede bile bir gun - ve oyle olmali. Taban
                // buyutulseydi kiyma buzdolabinda etten uzun yasardi.
                //
                // Yani kisa omurlu malzeme her gece oluyor, her kademede.
                // Soguk hava onlari kurtarmiyor; UZUN omurlulari kurtariyor
                // ve menu genisligini oradan satin aliyor.
                int life = keepBp > 0
                    ? (int)Fx.MulDiv(_content.Ingredients[i].SpoilDays, keepBp, Fx.One)
                    : 0;

                if (_stockAgeDays[i] >= life)
                {
                    // ZAYIATIN DEGERI sayiliyor.
                    //
                    // Denge araci "net" sutununda gercek kasa
                    // hareketinin 2-20 kati bir sayi raporluyordu ve
                    // farkin tamami buydu: cope giden stok hicbir yerde
                    // toplanmiyordu. Olculdu - hicbir sey almayan ama
                    // her gun hal'e giden bir botta, alinan her 100
                    // sikkelik malzemenin 43'u cope gidiyor. Uc tasarim
                    // sorusunun cevabi bu yuzden TERS ISARETLIYDI.
                    long lost = Fx.MulDiv(
                        _content.Ingredients[i].BasePrice, _stockGrams[i], 1000);
                    _spoiledValue += lost;
                    _daySpoiled += lost;

                    _stockGrams[i] = 0;
                    _stockAgeDays[i] = 0;
                }
            }
        }

        /// <summary>
        /// Soguk hava kademesinin kazandirdigi raf omru payi, baz puan.
        /// 0 = soguk hava yok, malzeme gece oluyor.
        /// </summary>
        private int StorageKeepBp()
        {
            if (_content.Storage == null || _storageTier <= 0) return 0;
            return _content.Storage.Tiers[_storageTier].KeepBp;
        }

        /// <summary>
        /// Kira ve maaslar HAFTALIK, yedinci gunun sonunda tek seferde odenir.
        /// docs/12 2: baski metronomu bu.
        /// </summary>
        private void PayWeeklyCostsIfDue()
        {
            if (_day % _economy.RentDayInterval != 0) return;

            int week = _day / 7;
            Crew crew = new Crew(_cooks, _salon);
            long wages = StaffingModel.WeeklyWageBill(crew, week, _economy);

            // Huyun ucrete etkisi. docs/14: cirak -%25, tecrubeli +%30.
            // Kadro carpani ORTALAMA aliniyor cunku WeeklyWageBill kisi
            // basi degil havuz basi hesapliyor; ucret modelinin sekli
            // (docs/14 kapasite modeli) o yuzden bozulmuyor.
            wages = Fx.MulDiv(wages, TraitWageMultiplierBp(), Fx.One);

            long rent = _economy.TierForTables(_tableCount).Rent;

            long installment = 0;
            if (_loanWeeksLeft > 0)
            {
                installment = _loanInstallment;
                _loanTotalRepaid += installment;
                _loanRepaidAll += installment;
                _loanWeeksLeft--;
                if (_loanWeeksLeft == 0) _loanInstallment = 0;
            }

            // docs/14: "maas zamaninda odendi +5, maas gecikti -25", ve bu
            // batma merdiveninin ucuncu kademesi.
            //
            // Olcut BUTUN FATURA: kira, maas ve taksit odendikten sonra
            // kasa artida mi. Ilk yazim yalnizca maasa bakiyordu ve o gun
            // sabah malzeme almis SAGLAM bir dukkani da "gecikti" sayiyordu;
            // bir kisi istifa edince servis dusuyor, ciro dusuyor ve iyi
            // oyuncu kendi kendine cokuyordu. Gecikme, borca dusmektir.
            bool onTime = _cash - (wages + rent + installment) >= 0;
            MoraleEvent(onTime ? _economy.MoralePaidDelta : _economy.MoraleLateDelta);
            if (!onTime) Emit(SimEventKind.WagesLate, _day, (int)(wages - _cash));

            bool wasPositive = _cash >= 0;
            _cash -= wages + rent + installment;
            _weeklyWagesPaid += wages;
            _weeklyRentPaid += rent;
            _dayWages = wages;
            _dayRent = rent + installment;

            Emit(SimEventKind.WeeklyCostsPaid, (int)rent, (int)wages,
                 _cash > int.MaxValue ? int.MaxValue : (int)_cash);

            if (wasPositive && _cash < 0)
            {
                _firstDebtDay = _day;
                Emit(SimEventKind.CashWentNegative, _day, (int)(-_cash));
            }

            if (_cash < 0) ClimbDownDebtLadder();
        }

        /// <summary>
        /// Batma merdiveni. docs/02 "yumusak ama disli basarisizlik".
        ///
        /// Bu yazilana kadar kasa eksiye dusunce HICBIR SEY olmuyordu ve
        /// sonuc bir yumusak kilitti: eksi kasayla malzeme alinamiyor
        /// (OrderIngredient "cost > _cash" ile reddediyor), yani ciro
        /// sifira duşuyor; ama kira ve maas kosulsuz kesilmeye devam
        /// ediyor. Olculdu: atilgan bot 7. gunde borca dusuyor, kalan 53
        /// gunu musterisiz geciriyor ve -48.370 ile bitiriyor. Ne
        /// ogreticiydi ne keyfi - oyun bitmiyordu, sadece donuyordu.
        ///
        /// Merdiven uc basamak, ve HER BASAMAK KASAYI TOPARLIYOR:
        ///
        ///   1. Ekipman satisi - en ust kademeden baslayarak, alis
        ///      fiyatinin yarisina. Kapasite dusuyor ama dukkan aciliyor.
        ///   2. Kucullme - bir masa kademesi asagi, gecis bedelinin
        ///      yarisi geri. Kira da dusuyor, yani bu asil kurtarma.
        ///   3. Kalan borc SILINIYOR ve itibar bedeli aliniyor.
        ///
        /// Kayit silinmiyor, oyun bitmiyor (docs/08). Bedel birikimli:
        /// merdivene inmek yil sonu degerlendirmesinde saglamlik eksenini
        /// dusuruyor.
        /// </summary>
        private void ClimbDownDebtLadder()
        {
            _debtRungs++;

            // --- 1. ekipman sat ------------------------------------------
            while (_cash < 0 && SellBestEquipment()) { }

            // --- 2. kucul ------------------------------------------------
            while (_cash < 0 && Downsize()) { }

            // --- 3. borcu sil, itibari ode ------------------------------
            if (_cash < 0)
            {
                _rescueValue += -_cash;
                _cash = 0;
                _reputationCenti -= _economy.DebtWriteOffRepCenti;
                if (_reputationCenti < 0) _reputationCenti = 0;

                // SIFIR KASA DA BIR KILITTI.
                //
                // Merdiven tam olarak yumusak kilidi cozmek icin yazildi,
                // ama ucuncu basamak kasayi SIFIRA birakiyordu ve Buy
                // "cost > _cash" ile reddediyor - sifirla da hicbir
                // malzeme alinamiyor. Olculdu: tabak testinin tani
                // ciktisi 10. gunden 40. gune kadar HER GUN "0 grup, 4
                // masa" basiyordu. Otuz bes gun ust uste tek musteri yok,
                // kira ve maas kesilmeye devam ediyor, merdiven her hafta
                // yeniden iniyor. Yani merdiven kilidi cozmuyor,
                // SONSUZA KADAR TEKRARLIYORDU.
                //
                // Basamak artik dukkani CALISIR halde birakiyor: bir
                // gunluk onerilen stogun bedeli kadar kurtarma payi.
                // Sayi uydurma degil - RecommendedRestock zaten menuyu,
                // talebi ve emniyet payini biliyor, yani "yarin sabah
                // acilabilecek kadar".
                //
                // Bedeli var: kurtarma degeri yil sonu saglamlik
                // eksenine yaziliyor, yani merdivene inmek hep pahali.
                long tabanKasa = RecommendedRestockCost();
                if (tabanKasa > 0)
                {
                    _cash = tabanKasa;
                    _rescueValue += tabanKasa;
                }
            }
        }

        /// <summary>
        /// Bir gunluk onerilen stogun guncel mevsim fiyatiyla bedeli.
        ///
        /// Merdivenin kurtarma payi bundan geliyor. Buy ile AYNI fiyat
        /// yolunu kullaniyor (mevsim oynamasi + pazar carpani), yoksa
        /// "yetecek kadar verdim" diye hesaplanan para yetmezdi.
        /// </summary>
        private long RecommendedRestockCost()
        {
            long toplam = 0;
            for (int i = 0; i < _stockGrams.Length; i++)
            {
                int need = RecommendedRestock(i);
                if (need <= 0) continue;
                long kilo = _content.Ingredients[i].PriceAt(Season, _quality);
                kilo = Fx.MulDiv(kilo, _marketBp[i], Fx.One);
                toplam += Fx.MulDiv(kilo, need, GramsPerKilo);
            }
            return toplam;
        }

        /// <summary>
        /// En yuksek kademeli istasyonu bir basamak satar. Fiyatin yarisi
        /// geri geliyor; satis her zaman zarardir, bilerek.
        /// </summary>
        private bool SellBestEquipment()
        {
            int best = -1, bestTier = 0;
            long bestPrice = 0;

            for (int i = 0; i < _stationTier.Length; i++)
            {
                int t = _stationTier[i];
                if (t <= 0) continue;

                // Masa sayisinin ZORUNLU kildigi kademenin altina inilmiyor:
                // satis dukkani calisamaz hale getirmemeli.
                if (t <= RequiredStationTier(i)) continue;

                long price = _content.Stations[i].Tiers[t].Price;
                if (price > bestPrice) { best = i; bestTier = t; bestPrice = price; }
            }
            if (best < 0) return false;

            _stationTier[best] = bestTier - 1;
            _cash += bestPrice / 2;
            _rescueValue += bestPrice / 2;
            Emit(SimEventKind.EquipmentSold, best, bestTier - 1);
            return true;
        }

        /// <summary>Bir masa kademesi asagi iner. Kira da dusuyor.</summary>
        private bool Downsize()
        {
            int tier = -1;
            for (int t = 0; t < _economy.TierCount; t++)
                if (_economy.TierAt(t).Tables == _tableCount) { tier = t; break; }
            if (tier <= 0) return false;

            TierConfig lower = _economy.TierAt(tier - 1);
            _cash += _economy.TierAt(tier).Upgrade / 2;
            _rescueValue += _economy.TierAt(tier).Upgrade / 2;

            // KUCULEN DUKKAN TABAK DA KAYBEDIYOR.
            //
            // Expand fark kadar TEMIZ tabak ekliyordu, Downsize hicbir
            // sey cikarmiyordu: kucullme gununde
            // "temiz + kullanimda + kirli > PlatesTotal" oluyor ve
            // WashNeeded'in esikleri kucullmus toplama gore hesaplandigi
            // icin bulasik nobeti yanlis zamanda tetikleniyordu. Gece
            // AdvanceToNextDay tabaklari kademeye yeniden yazdigi icin
            // hata KENDINI GIZLIYORDU - degismez yalnizca o gun kirikti.
            //
            // Once temizden, yetmezse kirliden dusuyor. Masadaki
            // (kullanimdaki) tabaga dokunulmuyor: elinde tabak olan
            // misafir ortadan kaybolmaz.
            int fazlaTabak = _economy.TierAt(tier).Plates - lower.Plates;
            if (fazlaTabak > 0)
            {
                int temizden = fazlaTabak < _platesClean ? fazlaTabak : _platesClean;
                _platesClean -= temizden;
                fazlaTabak -= temizden;
                if (fazlaTabak > 0)
                {
                    int kirliden = fazlaTabak < _platesDirty ? fazlaTabak : _platesDirty;
                    _platesDirty -= kirliden;
                }
            }

            _tableCount = lower.Tables;

            // Kadro tavani da dustu; fazla kalanlar gidiyor.
            int cap = lower.StaffCap;
            while (_cooks + _salon > cap && _salon > 0) Fire(1, _salon - 1);
            while (_cooks + _salon > cap && _cooks > 1) Fire(0, _cooks - 1);

            Emit(SimEventKind.Downsized, _tableCount, tier - 1);
            return true;
        }

        /// <summary>Aksam asamasindan ertesi sabaha gecer.</summary>
        public void AdvanceToNextDay()
        {
            if (_phase != DayPhase.Evening) return;

            // Gunun kuveri SIFIRLANMADAN once zirveyi guncelle.

            _day++;
            _phase = DayPhase.Morning;
            _serviceTick = 0;
            _dayWages = 0;
            _dayRent = 0;
            _daySpoiled = 0;
            _servedParties = 0;
            _servedPeople = 0;
            _angryParties = 0;
            _angrySeated = 0;
            _salonRushWashes = 0;
            _revenue = 0;
            _ingredientCost = 0;
            _satisfactionSum = 0;
            _reputationDeltaMicro = 0;
            _arrCount = 0;
            _arrNext = 0;
            _turnedAwayParties = 0;
            _commandCount = 0;
            // BUGUN kazanilanlar sifirlaniyor; kazanilmis nisanlar
            // (_badges) elbette duruyor.
            _badgesToday = 0;
            for (int i = 0; i < MaxTables; i++)
            {
                _tableParty[i] = -1;
                _tableDirty[i] = false;
                _tablePlates[i] = 0;
            }

            // GECE BULASIK BITIYOR.
            //
            // Kapanis vardiyasi lavaboyu bosaltir; ertesi sabah butun
            // tabaklar temiz. Darbogaz GUN ICINDE bir darbogaz - dunku
            // ihmali bugune tasimak, oyuncunun goremedigi bir yerden gelen
            // bir ceza olurdu.
            _platesClean = _economy.TierForTables(_tableCount).Plates;
            _platesDirty = 0;
            _platesInUse = 0;
            _plateBlockedTicks = 0;
            _plateStalled = false;
            _washedToday = 0;
            _dirtiedToday = 0;
            _washing = false;
            _plateWarned = false;
            for (int i = 0; i < MaxParties; i++) { _pPlates[i] = 0; _pCooked[i] = false; }
            for (int i = 0; i < MaxServers; i++)
            {
                _salonTaskKind[i] = TaskKind.None;
                _kitchenTaskKind[i] = TaskKind.None;
            }
            // Gun basinda hicbir yuva dolu olmamali. Onceki gunden kalan
            // bir sayac mutfagi kalici olarak daraltirdi.
            for (int i = 0; i < _stationBusy.Length; i++) _stationBusy[i] = 0;
            for (int j = 0; j < _jobStation.Length; j++)
            {
                _jobStation[j] = -1;
                _jobMs[j] = 0;
                _jobPlates[j] = 0;
                _jobSlots[j] = 0;
                _jobState[j] = 0;
            }
            for (int i = 0; i < MaxParties; i++) _pJobsLeft[i] = 0;
            for (int i = 0; i < MaxParties; i++) _pAskedDish[i] = -1;
            _interventionsLeft = InterventionsToday;
            for (int i = 0; i < MaxParties; i++)
            {
                _pCredit[i] = false;
                _pTea[i] = false;
                _pCombo[i] = false;
                _pAsksCredit[i] = false;
                _pRegular[i] = -1;
                _pMissedFavourite[i] = false;
            }
            SettleDueTabs();
            AnnounceUnlocks();
            RollMarket();
            PlanRegularVisits();
            RefreshCandidates();
            for (int i = 0; i < _orderedRole.Length; i++) _orderedRole[i] = 0;

            Emit(SimEventKind.DayOpened, _day);
        }

        /// <summary>
        /// Bugun acilan yemekleri duyurur.
        ///
        /// Neden bir olay: acilis SESSIZ oluyordu. Yemekler 3. gunden 57.
        /// gune kadar teker teker aciliyor ve oyuncu bunu ancak Menu
        /// ekranini acip asagi kaydirirsa goruyordu. Acilan her sey bir
        /// odul; duyurulmayan odul odul degil.
        ///
        /// Dun kapali bugun acik olanlar taraniyor, yani kosul yemegin
        /// GUNU olmak zorunda degil - itibarla ya da ekipmanla acilan bir
        /// yemek de duyuruluyor.
        /// </summary>
        private void AnnounceUnlocks()
        {
            for (int i = 0; i < _content.Dishes.Length; i++)
            {
                bool open = Unlocked(i);
                if (open == _dishWasUnlocked[i]) continue;

                _dishWasUnlocked[i] = open;
                if (open) Emit(SimEventKind.DishUnlocked, i);
            }
        }

        /// <summary>
        /// Bir yemegin fiyatini ayarlar.
        ///
        /// TAVAN VAR. Memnuniyet [0, 10000] arasina kirpiliyor ve talep
        /// fiyati hic gormuyor; yani ceza bir noktadan sonra DOYUYOR ve
        /// ondan sonraki her sifir bedava. Olculdu: yan kalemleri 2000
        /// kat pahalilastiran bir bot 3,4 milyon sikke topladi, itibar
        /// ve memnuniyet hic degismedi. Kombo acikken 24,8 milyon.
        ///
        /// Taban (UnderpriceFloorBp) zaten vardi; tavanin olmamasi
        /// simetri hatasiydi. Tavan PIYASA fiyatina gore: icerik
        /// degisince sinir da degisiyor.
        /// </summary>
        private void SetPrice(int dishIndex, int price)
        {
            if (dishIndex < 0 || dishIndex >= _dishPrice.Length || price <= 0)
            {
                Emit(SimEventKind.CommandRejected, (int)CommandKind.SetPrice, 3);
                return;
            }

            long piyasa = _content.Dishes[dishIndex].Price;
            if (piyasa > 0)
            {
                long tavan = Fx.MulDiv(piyasa, _economy.OverpriceCeilingBp, Fx.One);
                if (price > tavan)
                {
                    // Sebep 13: fiyat tavani. Arayuz bunu "bu fiyata
                    // kimse gelmez" diye gosteriyor.
                    Emit(SimEventKind.CommandRejected, (int)CommandKind.SetPrice, 13);
                    return;
                }
            }

            _dishPrice[dishIndex] = price;
        }

        private void SetMenuSlot(int dishIndex, bool on)
        {
            if (dishIndex < 0 || dishIndex >= _dishOnMenu.Length)
            {
                Emit(SimEventKind.CommandRejected, (int)CommandKind.SetMenuSlot, 3);
                return;
            }
            _dishOnMenu[dishIndex] = on;
        }

        /// <summary>
        /// Istasyonun bir ust ekipman kademesini satin alir. docs/27
        /// Karar D: yeni kademe ya yuva ekler ya attendBp dusurur; pisme
        /// suresine dokunmaz.
        ///
        /// Fiyat pesin ve kasadan dusuyor; borca girilerek alinmiyor.
        /// </summary>
        private void BuyEquipment(int station)
        {
            if (station < 0 || station >= _stationTier.Length)
            {
                Emit(SimEventKind.CommandRejected, (int)CommandKind.BuyEquipment, 3);
                return;
            }

            StationDef def = _content.Stations[station];
            int next = _stationTier[station] + 1;
            if (next > def.MaxTier)
            {
                Emit(SimEventKind.CommandRejected, (int)CommandKind.BuyEquipment, 4);
                return;
            }

            long price = def.Tiers[next].Price;
            if (_cash < price)
            {
                Emit(SimEventKind.CommandRejected, (int)CommandKind.BuyEquipment, 5);
                return;
            }

            _cash -= price;
            _equipmentSpend += price;
            _stationTier[station] = next;
            Emit(SimEventKind.EquipmentBought, station, next);
        }

        /// <summary>
        /// Soguk havanin bir ust kademesini satin alir.
        ///
        /// Bunun oyundaki karsiligi MENU GENISLIGI. Soguk hava yokken
        /// bozulabilir her sey gece oldugu icin dar menu kesinlikle dogru
        /// strateji; denge aracinda makul oyuncuyu ayakta tutan sey menuyu
        /// uc ana yemege daraltmak. Soguk hava o kisiti gevsetiyor ve otuz
        /// iki yemeklik icerik envanterinin var olma sebebi oluyor.
        /// </summary>
        private void BuyStorage()
        {
            StorageDef def = _content.Storage;
            if (def == null)
            {
                Emit(SimEventKind.CommandRejected, (int)CommandKind.BuyStorage, 3);
                return;
            }

            int next = _storageTier + 1;
            if (next > def.MaxTier)
            {
                Emit(SimEventKind.CommandRejected, (int)CommandKind.BuyStorage, 4);
                return;
            }

            long price = def.Tiers[next].Price;
            if (_cash < price)
            {
                Emit(SimEventKind.CommandRejected, (int)CommandKind.BuyStorage, 5);
                return;
            }

            _cash -= price;
            _equipmentSpend += price;
            _storageTier = next;
            Emit(SimEventKind.StorageBought, next, def.Tiers[next].KeepBp);
        }

        /// <summary>
        /// Halden alinacak malzemenin kalite kademesini secer.
        /// 0 dusuk, 1 standart, 2 yuksek. Elde olan stogu DEGISTIRMEZ;
        /// yalnizca bundan sonraki alimlari etkiler.
        /// </summary>
        /// <summary>
        /// Kalite kademesi sayisi: dusuk, standart, yuksek.
        ///
        /// Tek yerde duruyor cunku iki yerde okunuyor - komut dogrulamasi
        /// ve kayit dogrulamasi. Ikisinde ayri yazilmis bir sayi, birinin
        /// kabul edip otekinin reddettigi bir deger demek.
        /// </summary>
        public const int QualityCount = 3;

        private void SetQuality(int level)
        {
            if (level < 0 || level >= QualityCount)
            {
                Emit(SimEventKind.CommandRejected, (int)CommandKind.SetQuality, 3);
                return;
            }
            _quality = level;
        }

        /// <summary>
        /// Verilen arketip, verilen rolden hangi yemegi secerdi. Yalnizca
        /// olcum icin; simulasyonun durumunu degistirmiyor.
        /// </summary>
        public int WouldPick(int archetype, int role, int servings = 1)
        {
            if (archetype < 0 || archetype >= _content.Archetypes.Length) return -1;
            string[] set = role == 0 ? _content.MainGroups
                         : role == 1 ? _content.SideGroups
                         : role == 2 ? _content.DrinkGroups
                                     : _content.DessertGroups;
            return PickFromRole(set, int.MaxValue, true, servings,
                                _content.Archetypes[archetype]);
        }

        /// <summary>Secili kalite kademesi.</summary>
        public int Quality { get { return _quality; } }

        /// <summary>Yemegin bugunku kalite etkisi, santi-puan. Olcum icin.</summary>
        public int DishQualityCentiOf(int dish)
        {
            if (dish < 0 || dish >= _content.Dishes.Length) return 0;
            return DishQualityCenti(dish);
        }

        /// <summary>Stoktaki malzemenin ortalama kalite etkisi, santi-puan.</summary>
        public int StockQualityOf(int ingredient)
        {
            if (ingredient < 0 || ingredient >= _stockQualityCenti.Length) return 0;
            return _stockQualityCenti[ingredient];
        }

        /// <summary>Sahip olunan soguk hava kademesi.</summary>
        public int StorageTier { get { return _storageTier; } }

        /// <summary>Bir ust soguk hava kademesinin fiyati; en ustteyse -1.</summary>
        public long NextStoragePrice()
        {
            StorageDef def = _content.Storage;
            if (def == null) return -1;
            int next = _storageTier + 1;
            return next > def.MaxTier ? -1 : def.Tiers[next].Price;
        }

        /// <summary>Istasyonun sahip olunan ekipman kademesi.</summary>
        public int StationTier(int station)
        {
            if (station < 0 || station >= _stationTier.Length) return 0;
            return _stationTier[station];
        }

        /// <summary>
        /// Bugunun mevsimi: 0 ilkbahar, 1 yaz, 2 sonbahar, 3 kis.
        /// Gun 1'de ilkbahar; her SeasonDays gunde bir donuyor.
        /// </summary>
        public int Season
        {
            get
            {
                int len = _economy.SeasonDays;
                if (len <= 0) return 0;
                int d = _day > 0 ? _day - 1 : 0;
                return (d / len) % 4;
            }
        }

        /// <summary>
        /// Malzemenin BUGUNKU kilo fiyati: mevsim, kalite ve gunluk hal
        /// oynamasi birlikte.
        /// </summary>
        public long IngredientPriceToday(int ingredient)
        {
            if (ingredient < 0 || ingredient >= _content.Ingredients.Length) return 0;
            long p = _content.Ingredients[ingredient].PriceAt(Season, _quality);
            return Fx.MulDiv(p, _marketBp[ingredient], Fx.One);
        }

        /// <summary>Bugunku hal carpani, baz puan. 10000 = normal gun.</summary>
        public int MarketBpOf(int ingredient)
        {
            if (ingredient < 0 || ingredient >= _marketBp.Length) return Fx.One;
            return _marketBp[ingredient];
        }

        /// <summary>
        /// Gunun hal fiyatlarini atar. Her malzeme bagimsiz oynuyor:
        /// bir gun domates ucuz, et pahali olabiliyor. Tek carpanla
        /// oynatmak "bugun her sey pahali" demek olurdu ve takip edilecek
        /// bir sey birakmazdi.
        /// </summary>
        private void RollMarket()
        {
            int vol = _economy.PriceVolatilityBp;
            if (vol <= 0)
            {
                for (int i = 0; i < _marketBp.Length; i++) _marketBp[i] = Fx.One;
                return;
            }
            for (int i = 0; i < _marketBp.Length; i++)
                _marketBp[i] = Fx.One - vol + _rngMarket.NextInt(2 * vol + 1);
        }

        /// <summary>Malzemenin dort mevsim ORTALAMA kilo fiyati.</summary>
        public long IngredientPriceMean(int ingredient)
        {
            if (ingredient < 0 || ingredient >= _content.Ingredients.Length) return 0;
            IngredientDef d = _content.Ingredients[ingredient];
            if (d.SeasonPriceBp == null) return d.BasePrice;
            long sum = 0;
            for (int i = 0; i < d.SeasonPriceBp.Length; i++) sum += d.PriceInSeason(i);
            return sum / d.SeasonPriceBp.Length;
        }

        /// <summary>Malzeme bugunku soguk havayla kac gun dayanir.</summary>
        public int KeepDays(int ingredient)
        {
            if (ingredient < 0 || ingredient >= _content.Ingredients.Length) return 0;
            IngredientDef d = _content.Ingredients[ingredient];
            if (!d.Perishable) return int.MaxValue;
            int keepBp = StorageKeepBp();
            if (keepBp <= 0) return 1;
            int life = (int)Fx.MulDiv(d.SpoilDays, keepBp, Fx.One);
            // Arayuz icin taban 1: "0 gun dayanir" diye bir sey yok,
            // bugun kullanilabiliyor. Simulasyonda omur 0 ve 1 ayni sey.
            return life < 1 ? 1 : life;
        }

        /// <summary>
        /// Bu malzeme hic bozulur mu. Tuz, un ve yag bozulmaz; onlar icin
        /// KeepDays int.MaxValue donuyor ve arayuz o sayiyi OLDUGU GIBI
        /// yazarsa oyuncu "2147483647 gun" goruyor - ilk masaustu
        /// yapisinda tam olarak bu oldu.
        /// </summary>
        public bool IsPerishable(int ingredient)
        {
            return ingredient >= 0 && ingredient < _content.Ingredients.Length
                && _content.Ingredients[ingredient].Perishable;
        }

        /// <summary>Malzeme bir gunden uzun saklanabiliyor mu.</summary>
        public bool CanKeep(int ingredient)
        {
            return KeepDays(ingredient) > 1;
        }

        public int StationCount { get { return _stationTier.Length; } }

        /// <summary>
        /// Bu istasyon mutfaga OZEL adlandirilmis bir ekipman mi (tas firin,
        /// doner ocagi...) yoksa paylasilan alti istasyondan biri mi.
        /// Adlandirilmis olan hicbir zaman masa sayisi yuzunden ZORUNLU
        /// olmaz; yalnizca menu acar.
        /// </summary>
        public bool IsCuisineStation(int station)
        {
            return station >= 0 && station < _content.Stations.Length
                && !_content.Stations[station].Shared;
        }

        /// <summary>Bugun bu rolden kac kalem siparis edildi. 0 ana, 3 tatli.</summary>
        public int OrderedInRole(int role)
        {
            return role >= 0 && role < _orderedRole.Length ? _orderedRole[role] : 0;
        }

        /// <summary>
        /// Bu masa sayisinda istasyonun gerektirdigi EN DUSUK kademe.
        /// Ekipman merdiveninin neededAtTables alanindan okunuyor; o alan
        /// tools/balance icinde docs/27 3.3 zirve tablosundan turetiliyor.
        ///
        /// Kizgin musteri sayisindan degil KAPASITEDEN okunmasi bilincli:
        /// kadro kararinda ayni hata yapilmis ve denge araci yakalanmisti.
        /// </summary>
        public int RequiredStationTier(int station)
        {
            if (station < 0 || station >= _stationTier.Length) return 0;

            // BU MUTFAGIN KULLANMADIGI istasyon zorunlu olamaz.
            //
            // equipment.json butun mutfaklarda ortak ve "neededAtTables"
            // orada duruyor. Firin, on dort masada zorunlu isaretli; ama
            // otuz iki Turk yemeginin HICBIRI firin kullanmiyor. Yani Turk
            // lokantasi buyudugunde hicbir ise yaramayan bir firina 4.800
            // sikke odemek zorunda kaliyordu.
            if (!_stationUsed[station]) return 0;

            StationDef def = _content.Stations[station];
            int want = 0;
            for (int t = 1; t < def.Tiers.Length; t++)
                if (def.Tiers[t].NeededAtTables > 0
                    && def.Tiers[t].NeededAtTables <= _tableCount)
                    want = t;
            return want;
        }

        /// <summary>
        /// Personelin adi. Isim havuzu yoksa bos donuyor ve arayuz
        /// sirali ada (Asci 1) duser.
        /// </summary>
        public string StaffName(int pool, int index)
        {
            int count = pool == 0 ? _cooks : _salon;
            if (index < 0 || index >= count || index >= MaxServers) return null;

            int[] names = pool == 0 ? _cookName : _salonName;
            int n = names[index];
            return n >= 0 && n < _content.StaffNames.Length
                ? _content.StaffNames[n] : null;
        }

        /// <summary>Bu istasyonu kullanan en az bir yemek var mi.</summary>
        public bool IsStationUsed(int station)
        {
            return station >= 0 && station < _stationUsed.Length && _stationUsed[station];
        }

        /// <summary>
        /// Oyuncunun HALA satin alabilecegi her seyin toplami: kalan
        /// genisleme kademeleri artı kalan ekipman basamaklari.
        ///
        /// "Para sorun olmaktan cikti" olcusu bunu kullaniyor. Onceki olcu
        /// "kasa en pahali genislemenin uc katini asti mi" diyordu ve iki
        /// yerden yaniliyordu: ekipmani hic saymiyordu, ve uc kat keyfi bir
        /// sayiydi. Dogru soru "biriktirecek bir sey kaldi mi": kasa kalan
        /// her seyi tek seferde aliyorsa gercekten kalmamistir.
        /// </summary>
        public long RemainingPurchaseCost()
        {
            long total = 0;
            for (int t = 0; t < _economy.TierCount; t++)
                if (_economy.TierAt(t).Tables > _tableCount)
                    total += _economy.TierAt(t).Upgrade;

            for (int st = 0; st < _stationTier.Length; st++)
            {
                StationDef def = _content.Stations[st];
                for (int t = _stationTier[st] + 1; t < def.Tiers.Length; t++)
                    total += def.Tiers[t].Price;
            }

            if (_content.Storage != null)
                for (int t = _storageTier + 1; t < _content.Storage.Tiers.Length; t++)
                    total += _content.Storage.Tiers[t].Price;

            return total;
        }

        /// <summary>Bir ust kademenin fiyati; en ustteyse -1.</summary>
        public long NextEquipmentPrice(int station)
        {
            if (station < 0 || station >= _stationTier.Length) return -1;
            StationDef def = _content.Stations[station];
            int next = _stationTier[station] + 1;
            return next > def.MaxTier ? -1 : def.Tiers[next].Price;
        }

        /// <summary>
        /// Kac salon calisani lavaboya adanacak.
        ///
        /// TAVAN _salon. Patron sayilmiyor: oyuncu patron, bulasikci
        /// degil - DispatchSalon lavaboya sifirinci sunucuyu (patronu)
        /// hicbir zaman koymuyor (`sinkFrom < 1` koruması).
        ///
        /// YORUM BIR ZAMANLAR "salon kadrosunun TAMAMI lavaboya
        /// verilemez, yoksa oyun kendi kendini kilitler" diyordu ve
        /// altinda iki satir bu tabani TAKLIT EDIYORDU:
        ///
        ///     int enFazla = _salon > 0 ? _salon - 0 : 0;   // == _salon
        ///     if (enFazla > _salon) enFazla = _salon;      // hic dogru olamaz
        ///
        /// Denetim ikisinin de no-op oldugunu dogru buldu; yanlis olan
        /// KODU degil YORUMU idi. Kilit diye bir sey yok: butun hirsli
        /// personel lavaboda olsa bile patron sahada kaliyor ve servis
        /// suruyor. Ustelik ilk gun tek salon calisani varken onu
        /// lavaboya vermek MESRU bir karar - taban konunca tabak
        /// darbogazi kampanyanin ilk gunlerinde hic denenemiyordu.
        ///
        /// Yani kural "taban yok, tavan _salon" ve ClampDishwashers
        /// zaten ayni tavani uyguluyor.
        /// </summary>
        private void SetDishwashers(int n)
        {
            if (n < 0) n = 0;
            if (n > _salon) n = _salon;
            _dishwashers = n;
        }

        private void Hire(int pool, int candidate)
        {
            int cap = _economy.TierForTables(_tableCount).StaffCap;
            if (_cooks + _salon >= cap)
            {
                Emit(SimEventKind.CommandRejected, (int)CommandKind.Hire, 4);
                return;
            }
            if (pool == 0)
            {
                if (_cooks < MaxServers) { _cookXpDays[_cooks] = 0; RollTraits(0, _cooks, candidate); }
                _cooks++;
            }
            else
            {
                if (_salon < MaxServers) { _salonXpDays[_salon] = 0; RollTraits(1, _salon, candidate); }
                _salon++;
            }
        }

        /// <summary>
        /// Lavabo kadrosunu salon kadrosunun icinde tutar.
        ///
        /// Istifa ya da isten cikarma salon sayisini dusurunce lavaboya
        /// adanmis sayi ondan buyuk kalabiliyor - o zaman DispatchSalon
        /// var olmayan kisileri lavaboda sayar ve salon sessizce boyle
        /// bir kisi kadar kucululur.
        /// </summary>
        private void ClampDishwashers()
        {
            if (_dishwashers > _salon) _dishwashers = _salon;
            if (_dishwashers < 0) _dishwashers = 0;
        }

        /// <summary>
        /// Yeni personelin iki huyu. docs/14: havuzdan iki tane, cakisan
        /// ikili olmadan. Ikinci huy cekilirken cakisanlar ELENIYOR - once
        /// cekip sonra reddetmek, ayni tohumda farkli sayida rastgele
        /// cagrisi demek olurdu ve tekrar oynatmayi bozardi.
        /// </summary>
        private void RollTraits(int pool, int index, int candidate = 0)
        {
            int[] a = pool == 0 ? _cookTraitA : _salonTraitA;
            int[] b = pool == 0 ? _cookTraitB : _salonTraitB;
            int[] morale = pool == 0 ? _cookMorale : _salonMorale;

            morale[index] = _economy.StartingMorale;
            a[index] = -1;
            b[index] = -1;

            // Isim de simdi seciliyor, ayni akistan: ise alim tek bir
            // olay, ismi ayri bir zar atisina birakmak tekrar oynatmayi
            // gereksiz yere karmasiklastirirdi.
            // Isim KENDI AKISINDAN.
            //
            // Once ise alim akisindan cekiliyordu ve bu, huy zarini
            // kaydirdi: ayni tohumla kosan bir test bir anda baska huylar
            // gordu ve mesgul istasyon bulamadi. docs/23 bagimsiz akislar
            // tam olarak bunun icin var - sunum, oynanisin zarina
            // dokunmamali.
            int[] names = pool == 0 ? _cookName : _salonName;
            names[index] = _content.StaffNames.Length > 0
                ? _rngName.NextInt(_content.StaffNames.Length) : -1;
            if (_economy.TraitCount == 0) return;

            // Havuz henuz kurulmadiysa (kurucu, ilk asci) simdi kur.
            if (_candDay < 0) RefreshCandidates();

            if (candidate < 0) candidate = 0;
            if (candidate >= CandidateSlots) candidate = CandidateSlots - 1;

            a[index] = CandidateTrait(pool, candidate, 0);
            b[index] = CandidateTrait(pool, candidate, 1);

            // Alinan aday havuzdan cikiyor ve yerine YENISI GELMIYOR:
            // docs/14 "begenmedigin adayi reddedebilirsin ama yenisi hemen
            // gelmez." Yeri, havuz tazelenene kadar bos duruyor.
            int slot = pool * CandidateSlots + candidate;
            _candTraitA[slot] = -1;
            _candTraitB[slot] = -1;
        }

        /// <summary>
        /// Aday havuzunu her uc gunde bir yeniler. docs/14: "begenmedigin
        /// adayi reddedebilirsin ama yenisi hemen gelmez."
        /// </summary>
        private void RefreshCandidates()
        {
            if (_economy.TraitCount == 0) return;
            if (_candDay >= 0 && _day - _candDay < _economy.CandidateRefreshDays) return;

            _candDay = _day;
            for (int i = 0; i < CandidateSlots * 2; i++)
                RollCandidate(i);
        }

        private void RollCandidate(int slot)
        {
            int n = _economy.TraitCount;
            _candTraitA[slot] = _rngHiring.NextInt(n);
            TraitDef first = _economy.TraitAt(_candTraitA[slot]);

            int free = 0;
            for (int i = 0; i < n; i++)
                if (i != _candTraitA[slot] && !first.ConflictsWithIndex(i)) free++;
            _candTraitB[slot] = -1;
            if (free == 0) return;

            int pick = _rngHiring.NextInt(free);
            for (int i = 0; i < n; i++)
            {
                if (i == _candTraitA[slot] || first.ConflictsWithIndex(i)) continue;
                if (pick == 0) { _candTraitB[slot] = i; return; }
                pick--;
            }
        }

        /// <summary>Adayin huyu. pool 0 mutfak, 1 salon; slot 0..2.</summary>
        public int CandidateTrait(int pool, int slot, int which)
        {
            if (slot < 0 || slot >= CandidateSlots) return -1;
            int i = pool * CandidateSlots + slot;
            return which == 0 ? _candTraitA[i] : _candTraitB[i];
        }

        /// <summary>Adayin ucret farki, baz puan. Arayuz bunu gosterecek.</summary>
        public int CandidateWageBp(int pool, int slot)
        {
            int total = 0;
            for (int which = 0; which < 2; which++)
            {
                int t = CandidateTrait(pool, slot, which);
                if (t >= 0) total += _economy.TraitAt(t).WageBp;
            }
            return total;
        }

        /// <summary>
        /// Bir huy ikilisinin kaba degeri: hiz + memnuniyet - ucret.
        ///
        /// Uc ayri birim toplaniyor ve bu bilincli bir kabalik. Amac bir
        /// denge hesabi degil, KIYASLAMA: elindeki kisi mi daha iyi, kapida
        /// bekleyen aday mi. Arayuz de bu siralamayi gosterecek.
        /// </summary>
        private int TraitScore(int a, int b)
        {
            int score = 0;
            for (int k = 0; k < 2; k++)
            {
                int t = k == 0 ? a : b;
                if (t < 0) continue;
                TraitDef d = _economy.TraitAt(t);
                score += d.SpeedBp + d.SatisfactionCenti - d.WageBp;
            }
            return score;
        }

        /// <summary>Calisan bir personelin huy puani.</summary>
        public int StaffTraitScore(int pool, int index)
        {
            return TraitScore(StaffTrait(pool, index, 0), StaffTrait(pool, index, 1));
        }

        /// <summary>Bir adayin huy puani. Aday alinmissa int.MinValue.</summary>
        public int CandidateScore(int pool, int slot)
        {
            int a = CandidateTrait(pool, slot, 0);
            if (a < 0) return int.MinValue;
            return TraitScore(a, CandidateTrait(pool, slot, 1));
        }

        /// <summary>Adayin hiz farki, baz puan.</summary>
        public int CandidateSpeedBp(int pool, int slot)
        {
            int total = 0;
            for (int which = 0; which < 2; which++)
            {
                int t = CandidateTrait(pool, slot, which);
                if (t >= 0) total += _economy.TraitAt(t).SpeedBp;
            }
            return total;
        }

        /// <summary>Bir personelin huyu; slot 0 veya 1. Yoksa -1.</summary>
        public int StaffTrait(int pool, int index, int slot)
        {
            int count = pool == 0 ? _cooks : _salon;
            if (index < 0 || index >= count || index >= MaxServers) return -1;
            if (slot == 0) return pool == 0 ? _cookTraitA[index] : _salonTraitA[index];
            return pool == 0 ? _cookTraitB[index] : _salonTraitB[index];
        }

        /// <summary>Bir personelin morali, 0-100.</summary>
        public int StaffMorale(int pool, int index)
        {
            int count = pool == 0 ? _cooks : _salon;
            if (index < 0 || index >= count || index >= MaxServers) return 0;
            return pool == 0 ? _cookMorale[index] : _salonMorale[index];
        }

        /// <summary>Bir personelin huylarinin toplam etkisi.</summary>
        private int TraitSum(int pool, int index, System.Func<TraitDef, int> pick)
        {
            int total = 0;
            for (int slot = 0; slot < 2; slot++)
            {
                int t = StaffTrait(pool, index, slot);
                if (t >= 0) total += pick(_economy.TraitAt(t));
            }
            return total;
        }

        private bool TraitAny(int pool, int index, System.Func<TraitDef, bool> pick)
        {
            for (int slot = 0; slot < 2; slot++)
            {
                int t = StaffTrait(pool, index, slot);
                if (t >= 0 && pick(_economy.TraitAt(t))) return true;
            }
            return false;
        }

        /// <summary>
        /// Belirli bir kisiyi isten cikarir.
        ///
        /// INDIS SART. Once yalnizca havuz aliniyordu ve her zaman SONUNCU
        /// kisi gidiyordu: oyuncu "Asci 2" kartindaki dugmeye basiyor,
        /// oyun Asci 1'i cikariyordu. Huysuz bir asci butun ekibin
        /// moralini cekiyor (aura mekanigi) ve oyuncu tam da onu
        /// cikaramiyordu - personel sisteminin tek aci karari
        /// calismiyordu.
        ///
        /// Cikarilan kisinin yerine SONUNCU kisi kaydiriliyor; dizide
        /// bosluk birakmak, butun donguleri "bos mu" kontroluyle
        /// kirletirdi.
        /// </summary>
        private void Fire(int pool, int index)
        {
            int[] xp = pool == 0 ? _cookXpDays : _salonXpDays;
            int[] a = pool == 0 ? _cookTraitA : _salonTraitA;
            int[] b = pool == 0 ? _cookTraitB : _salonTraitB;
            int[] morale = pool == 0 ? _cookMorale : _salonMorale;
            int count = pool == 0 ? _cooks : _salon;

            if (count <= 0 || index < 0 || index >= count || index >= MaxServers)
            {
                Emit(SimEventKind.CommandRejected, (int)CommandKind.Fire, 5);
                return;
            }

            int last = count - 1;
            if (index != last && last < MaxServers)
            {
                xp[index] = xp[last];
                a[index] = a[last];
                b[index] = b[last];
                morale[index] = morale[last];
            }

            if (last < MaxServers)
            {
                xp[last] = 0;
                a[last] = -1;
                b[last] = -1;
                morale[last] = 0;
            }

            int[] names = pool == 0 ? _cookName : _salonName;
            if (index != last && last < MaxServers) names[index] = names[last];
            if (last < MaxServers) names[last] = -1;

            if (pool == 0) _cooks--; else _salon--;
            Emit(SimEventKind.StaffResigned, pool, index);
        }

        // =====================================================================
        // Imza mekanikleri. docs/07: "en onemli satir - satin almanin
        // yeniden boyama degil BASKA BIR OYUN oldugunu gosteren sey bu."
        // Mekanik burada, sayilar cuisines/*.json icinde (docs/23 8.2).
        // =====================================================================

        // =====================================================================
        // Isimli duzenli musteriler. docs/11: "isimli musteri tek bir
        // kisidir, elle yazilmistir, hikayesi vardir ve hep ayni kisidir."
        // =====================================================================

        public int RegularCount { get { return _content.Regulars.Length; } }
        public int RegularVisits(int i)
        {
            return i >= 0 && i < RegularCount ? _regVisits[i] : 0;
        }
        public int RegularBeat(int i)
        {
            return i >= 0 && i < RegularCount ? _regBeat[i] : 0;
        }
        public bool RegularAway(int i)
        {
            return i >= 0 && i < RegularCount && _regAwayDays[i] > 0;
        }
        /// <summary>Ortalama memnuniyeti, santi-puan. Hic gelmemisse 0.</summary>
        public int RegularSatisfactionCenti(int i)
        {
            if (i < 0 || i >= RegularCount || _regVisits[i] == 0) return 0;
            return (int)(_regSatSum[i] / _regVisits[i]);
        }
        /// <summary>
        /// Bu yemek, kampanyaya girmis bir duzenli musterinin sevdigi
        /// yemek mi. Arayuz menude yildizla isaretleyecek; denge araci
        /// menuyu daraltirken bunlari koruyor.
        /// </summary>
        public bool IsFavouriteOfArrivedRegular(int dish)
        {
            return FavouriteRegularOf(dish) >= 0;
        }

        /// <summary>
        /// Bu yemegi seven, GELMIS duzenli musterinin indisi; yoksa -1.
        ///
        /// Once yalnizca "birisi seviyor mu" sorulabiliyordu ve menu
        /// ekrani bunu adsiz bir noktayla gosteriyordu. Kimin sevdigi
        /// bilinmeden o nokta bir bilgi degil bir susleme.
        /// </summary>
        public int FavouriteRegularOf(int dish)
        {
            RegularDef[] regs = _content.Regulars;
            for (int i = 0; i < regs.Length && i < MaxRegulars; i++)
                if (regs[i].FavouriteDish == dish && regs[i].ArrivesFromDay <= _day)
                    return i;
            return -1;
        }

        /// <summary>Bu grup hangi duzenli musteri; -1 ise isimsiz kalabalik.</summary>
        public int PartyRegular(int party)
        {
            return party >= 0 && party < MaxParties ? _pRegular[party] : -1;
        }

        /// <summary>
        /// Bugun kimler ugrayacak. Gun acilisinda, gelis planindan ONCE.
        ///
        /// Duzenli musteri talebe EKLENMIYOR, talebin icinden ALINIYOR:
        /// aksi halde isimli musteri yazmak ekonomiyi sisirirdi ve
        /// kalibrasyon her yeni isim ile kayardi.
        /// </summary>
        private void PlanRegularVisits()
        {
            RegularDef[] regs = _content.Regulars;
            for (int i = 0; i < regs.Length && i < MaxRegulars; i++)
            {
                if (_regAwayDays[i] > 0) { _regAwayDays[i]--; _regComing[i] = false; continue; }
                _regComing[i] = regs[i].ArrivesFromDay <= _day
                                && _rngRegular.Chance(_economy.RegularVisitChanceBp);
            }
        }

        /// <summary>
        /// Bugun gelecek duzenli musterileri gelis planindaki uygun
        /// satirlara baglar. Once KENDI arketibindeki bir satir aranir;
        /// bulunamazsa en erken bos satir onun adina yazilir.
        /// </summary>
        private void BindRegularsToPlan()
        {
            RegularDef[] regs = _content.Regulars;
            for (int i = 0; i < regs.Length && i < MaxRegulars; i++)
            {
                if (!_regComing[i]) continue;

                int pick = -1;
                for (int k = 0; k < _arrCount; k++)
                {
                    if (_arrRegular[k] >= 0) continue;
                    if (_arrArchetype[k] != regs[i].ArchetypeIndex) continue;
                    pick = k;
                    break;
                }
                if (pick < 0)
                {
                    for (int k = 0; k < _arrCount; k++)
                    {
                        if (_arrRegular[k] >= 0) continue;
                        pick = k;
                        _arrArchetype[k] = regs[i].ArchetypeIndex;
                        break;
                    }
                }
                if (pick < 0) { _regComing[i] = false; continue; }   // bugun yer yok
                _arrRegular[pick] = i;
            }
        }

        /// <summary>
        /// Ziyareti kaydeder ve hikaye sahnesi acilip acilmadigina bakar.
        /// Odeme aninda cagriliyor.
        /// </summary>
        private void RecordRegularVisit(int party, int satisfaction)
        {
            int i = _pRegular[party];
            if (i < 0 || i >= RegularCount) return;

            _regVisits[i]++;
            _regSatSum[i] += satisfaction;
            Emit(SimEventKind.RegularVisited, i, satisfaction);

            // Kotu agirlanan duzenli musteri BIR SURE GELMIYOR. Ceza itibar
            // degil: adini bildigin birinin kapiyi calmamasi, bir puandan
            // daha cok anlatir.
            if (satisfaction < _economy.RegularUpsetCenti)
            {
                _regAwayDays[i] = _economy.RegularAwayDays;
                Emit(SimEventKind.RegularUpset, i, _economy.RegularAwayDays);
                return;
            }

            StoryBeat[] story = _content.Regulars[i].Story;
            int avg = RegularSatisfactionCenti(i);
            while (_regBeat[i] < story.Length)
            {
                StoryBeat b = story[_regBeat[i]];
                if (_regVisits[i] < b.RequiresVisits) break;
                if (avg < b.RequiresSatisfactionCenti) break;
                _regBeat[i]++;
                Emit(SimEventKind.RegularStoryBeat, i, b.Beat);
            }
        }

        /// <summary>Toplam acik veresiye, santi-sikke.</summary>
        // ---- DEFTER, ARAYUZ ICIN ----------------------------------------
        //
        // Veresiye bir "prim dugmesi" olmaktan cikip karar uretmeye
        // basladi (tahsilat sansi artik musterinin guvenine bagli), ama
        // oyuncu defteri GOREMIYORDU: kimin ne kadar borcu var, vadesi ne
        // zaman, guveni ne - hicbiri ekranda yoktu ve mekanigin ikinci
        // karari (erken tahsilat) arayuzsuz duruyordu.
        //
        // Sans da aciliyor, bilerek: gizli bir olasilik uzerine karar
        // verilemez. Oyuncunun gordugu sayi, simulasyonun kullandigi
        // sayinin TA KENDISI olmali - iki ayri hesap olsaydi ekran
        // yalan soylerdi.

        /// <summary>Defterdeki acik hesap sayisi.</summary>
        public int TabCount { get { return _tabCount; } }

        /// <summary>Hesabin tutari, santi.</summary>
        public long TabAmount(int tab)
        {
            return tab >= 0 && tab < _tabCount ? _tabAmount[tab] : 0;
        }

        /// <summary>Vadeye kalan gun. Negatifse gecmis.</summary>
        public int TabDaysLeft(int tab)
        {
            return tab >= 0 && tab < _tabCount ? _tabDueDay[tab] - _day : 0;
        }

        /// <summary>Hesabin sahibi (duzenli musteri indisi), yoksa -1.</summary>
        public int TabRegular(int tab)
        {
            return tab >= 0 && tab < _tabCount ? _tabRegular[tab] : -1;
        }

        /// <summary>Bu hesap acilirken cay ikram edilmis miydi.</summary>
        public bool TabHadTea(int tab)
        {
            return tab >= 0 && tab < _tabCount && _tabTea[tab] != 0;
        }

        /// <summary>
        /// VADESINDE beklenirse tahsilat sansi, baz puan.
        ///
        /// SettleTab ile AYNI hesap; orada bir kez daha yazilmiyor, cunku
        /// iki kopya bir gun ayrilir ve ekran oyuncuya simulasyonun
        /// kullanmadigi bir sayi gosterirdi.
        /// </summary>
        public int TabCollectChanceBp(int tab)
        {
            if (tab < 0 || tab >= _tabCount) return 0;
            return TabChanceBp(tab, false);
        }

        /// <summary>ERKEN kovalanirsa tahsilat sansi, baz puan.</summary>
        public int TabEarlyChanceBp(int tab)
        {
            if (tab < 0 || tab >= _tabCount) return 0;
            return TabChanceBp(tab, true);
        }

        public long OpenCredit
        {
            get
            {
                long t = 0;
                for (int i = 0; i < _tabCount; i++) t += _tabAmount[i];
                return t;
            }
        }

        public int OpenCreditCount { get { return _tabCount; } }
        /// <summary>Veresiyenin biriktirdigi talep primi, baz puan.</summary>
        public int CreditLoyaltyBp { get { return _creditLoyaltyBp; } }
        public bool ComboEnabled { get { return _comboOn; } }
        /// <summary>
        /// Imza mekanigi acildi mi. docs/09: ikinci mevsimin ilk gunu.
        /// Birinci mevsimde ogretici yuku zaten menu ve fiyatla dolu.
        /// </summary>
        public bool SignatureOpen
        {
            get
            {
                return _content.Signature.Kind != SignatureKind.None
                    && _day >= _content.Signature.FromDay;
            }
        }

        public int SignatureFromDay { get { return _content.Signature.FromDay; } }

        /// <summary>Bu mutfagin imza mekanigi veresiye mi VE acildi mi.</summary>
        public bool HasCredit
        {
            get { return _content.Signature.Kind == SignatureKind.Credit && SignatureOpen; }
        }
        public bool HasCombo
        {
            get { return _content.Signature.Kind == SignatureKind.Combo && SignatureOpen; }
        }

        /// <summary>
        /// Bu gruba veresiye acilabilir mi. Kural TURETILMIS: yalnizca
        /// SIK gelen arketipler (TierIndex 0). docs/13 veresiyeEligible
        /// alanini "duzenli musteri" dosyasina koymus ama o icerik henuz
        /// yok; sik gelen musteri zaten mahallenin duzenlisi. Elle bir
        /// liste daha yazmak, ayni karakteri ikinci kez yazmak olurdu.
        /// </summary>
        public bool CreditEligible(int party)
        {
            if (!HasCredit) return false;
            if (party < 0 || party >= MaxParties || !_pActive[party]) return false;
            // Veresiye TEKLIF EDILMEZ, ISTENIR. Istemeyene acmak, olmayan
            // bir sorunu cozmek icin nakit baglamak olurdu.
            return _pAsksCredit[party];
        }

        /// <summary>
        /// Bu gruba veresiye acilabilir mi - KIMLIK sarti.
        ///
        /// docs/13 veresiyeEligible alanini duzenli musteri dosyasina
        /// koymus, ve dogrusu bu: veresiye adini bildigin birine acilir.
        /// Duzenli musteri icerigi yokken kural SIK GELEN arketipten
        /// turetiliyordu; artik icerik varsa ondan geliyor, yoksa eski
        /// turetim yedek olarak duruyor (birim testleri regulars dosyasi
        /// olmadan kosuyor).
        /// </summary>
        private bool CreditIdentityOk(int party)
        {
            if (_content.Regulars.Length == 0)
                return _content.Archetypes[_pArchetype[party]].TierIndex == 0;

            int i = _pRegular[party];
            return i >= 0 && i < RegularCount && _content.Regulars[i].VeresiyeEligible;
        }

        /// <summary>Bu grup veresiye istiyor mu. Arayuz bunu isaretleyecek.</summary>
        /// <summary>
        /// Su an veresiye isteyen ILK grup, yoksa -1.
        ///
        /// Arayuz ayni dongueyi kendi yaziyordu; tur da yazacakti.
        /// Uc kopya, uc ayri "uygun mu" tanimi demek - ve bir gun
        /// biri otekini yalanlar.
        /// </summary>
        public int FirstCreditAsker()
        {
            for (int p = 0; p < MaxParties; p++)
                if (CreditEligible(p)) return p;
            return -1;
        }

        /// <summary>
        /// Bu gruba veresiye ACILDI mi.
        ///
        /// ExtendCredit deftere HEMEN yazmiyor: yalnizca grubu
        /// isaretliyor, defter kaydi hesap ODENDIGINDE olusuyor.
        /// Komutun kabul edildigini olcmek isteyen (tur, testler)
        /// OpenCredit'e bakarsa YANLIS ZAMANI olcer ve komut
        /// reddedilmis sanir - bir kez tam bunu yaptim.
        /// </summary>
        public bool PartyHasCredit(int party)
        {
            return party >= 0 && party < MaxParties && _pActive[party] && _pCredit[party];
        }

        public bool AsksForCredit(int party)
        {
            return party >= 0 && party < MaxParties && _pActive[party] && _pAsksCredit[party];
        }

        private void ExtendCredit(int party)
        {
            SignatureDef sig = _content.Signature;
            if (!HasCredit)
            {
                Emit(SimEventKind.CommandRejected, (int)CommandKind.ExtendCredit, 10);
                return;
            }
            if (!CreditEligible(party) || _pCredit[party])
            {
                Emit(SimEventKind.CommandRejected, (int)CommandKind.ExtendCredit, 11);
                return;
            }
            if (_tabCount >= MaxTabs)
            {
                // 16, 12 DEGIL. Kod 12 gunluk komut hakkinin bitmesi
                // demek ve arayuz o sayiya bakip "Bugunluk bu kadar is
                // yeter" yaziyor - defteri dolu bir oyuncuya bu cumle
                // yanlis. Red sebepleri ARAYUZE konusuyor; ayni sayiyi
                // iki sebebe vermek, oyuncuya yanlis sey soylemek.
                Emit(SimEventKind.CommandRejected, (int)CommandKind.ExtendCredit, 16);
                return;
            }
            long bill = OrderPrice(party) * _pSize[party];
            if (bill > sig.CreditMaxPerRegular)
            {
                Emit(SimEventKind.CommandRejected, (int)CommandKind.ExtendCredit, 13);
                return;
            }
            // Deftere yazilan adama cay konur. Adet degil MEKANIK: cay
            // tahsilat sansini yukseltiyor (docs/12 3), ve maliyeti
            // icerikteki teaCostCenti - o alan bugune kadar okunmuyordu.
            long tea = (long)sig.CreditTeaCostCenti * _pSize[party];
            if (_cash >= tea)
            {
                _cash -= tea;
                _teaSpend += tea;
                _pTea[party] = true;
            }

            _pCredit[party] = true;
            Emit(SimEventKind.CreditExtended, party, (int)bill);
        }

        /// <summary>
        /// Vadesi gelmemis bir hesabi ERKEN kovalar. Karsiligi var: sans
        /// yariya iniyor ve tutmazsa hesap orada kapaniyor. "Simdi al ama
        /// kotu ihtimalle" ile "bekle" arasinda gercek bir takas.
        /// </summary>
        private void CollectCredit(int tab)
        {
            if (_content.Signature.Kind != SignatureKind.Credit
                || tab < 0 || tab >= _tabCount)
            {
                Emit(SimEventKind.CommandRejected, (int)CommandKind.CollectCredit, 14);
                return;
            }
            SettleTab(tab, halfChance: true);
        }

        /// <summary>
        /// Bir hesabin tahsilat sansi, baz puan.
        ///
        /// TEK YERDE: hem SettleTab hem arayuz buradan okuyor. Arayuz
        /// oyuncuya sansi GOSTERIYOR (gizli bir olasilik uzerine karar
        /// verilemez) ve ekranin gosterdigi sayi, simulasyonun
        /// kullandigi sayinin ta kendisi olmali.
        ///
        /// SANS KIME YAZDIGINA BAGLI. Sabitti ve odeme primiyle birlikte
        /// veresiyeyi pesin satistan KARLI yapiyordu (0,95 x 1,12 =
        /// 1,064 x fis), yani reddetmek icin hicbir gun yoktu. Artik
        /// musterinin ziyaret sayisi guven olarak ekleniyor: yeni
        /// tanistigin biri kotu bir bahis, yillardir gelen biri iyi.
        /// </summary>
        private int TabChanceBp(int tab, bool halfChance)
        {
            SignatureDef sig = _content.Signature;
            int chance = sig.CreditCollectChanceBp + TabTrustBp(tab);
            if (_tabTea[tab] != 0) chance += sig.CreditTeaCollectBonusBp;

            // Tavan TAM KESINLIK DEGIL: risksiz bir defter yine karar
            // uretmeyen bir prim dugmesi olurdu.
            if (chance > sig.CreditChanceCapBp) chance = sig.CreditChanceCapBp;
            return halfChance ? chance / 2 : chance;
        }

        /// <summary>
        /// Bir hesabi kapatir: ya tahsil edilir ya batar. Batinca itibar
        /// dusuyor - kovalamak zorunda kalmak dukkanin havasini bozuyor.
        /// </summary>
        private void SettleTab(int tab, bool halfChance)
        {
            SignatureDef sig = _content.Signature;
            long amount = _tabAmount[tab];

            int chance = TabChanceBp(tab, halfChance);

            bool paid = _rngCredit.Chance(chance);
            RemoveTab(tab);

            if (paid)
            {
                // Hesabini kapatan ustune koyuyor. Mekanigin kazanc tarafi.
                long settled = amount + Fx.Bp(amount, sig.CreditRepayBonusBp);
                _cash += settled;
                _revenue += settled;
                _revenueAll += settled;
                _creditCollected += amount;

                // Odenen her hesap SADAKAT birakiyor: o musteri geri
                // geliyor. Itibar degil talep - itibar tavana dayaninca
                // duruyor, mahalleye guvenmek durmuyor.
                _creditLoyaltyBp += sig.CreditLoyaltyDemandBp;
                if (_creditLoyaltyBp > sig.CreditLoyaltyCapBp)
                    _creditLoyaltyBp = sig.CreditLoyaltyCapBp;

                Emit(SimEventKind.CreditCollected, (int)settled, (int)OpenCredit);
            }
            else
            {
                _reputationCenti -= sig.CreditDefaultRepPenaltyCenti;
                if (_reputationCenti < 0) _reputationCenti = 0;

                // Batan hesap sadakati de goturuyor: kovaladigin musteri
                // bir daha gelmiyor.
                _creditLoyaltyBp -= sig.CreditLoyaltyDemandBp;
                if (_creditLoyaltyBp < 0) _creditLoyaltyBp = 0;

                Emit(SimEventKind.CreditDefaulted, (int)amount,
                     sig.CreditDefaultRepPenaltyCenti);
            }
        }

        /// <summary>
        /// Bu hesabin sahibine duyulan guven, baz puan.
        ///
        /// Ziyaret sayisindan geliyor ve icerikteki tavanla sinirli.
        /// Adi bilinmeyen bir musteriye (duzenli degilse) guven yok -
        /// zaten veresiye de yalnizca adi bilinene aciliyor
        /// (CreditIdentityOk), ama kayit eski bir kayittan gelirse
        /// -1 olabiliyor.
        /// </summary>
        private int TabTrustBp(int tab)
        {
            int r = _tabRegular[tab];
            if (r < 0 || r >= RegularCount) return 0;

            long bp = (long)_regVisits[r] * _content.Signature.CreditTrustPerVisitBp;
            int cap = _content.Signature.CreditTrustCapBp;
            return bp > cap ? cap : (int)bp;
        }

        private void RemoveTab(int tab)
        {
            int last = _tabCount - 1;
            _tabAmount[tab] = _tabAmount[last];
            _tabDueDay[tab] = _tabDueDay[last];
            _tabTea[tab] = _tabTea[last];
            _tabRegular[tab] = _tabRegular[last];
            _tabAmount[last] = 0;
            _tabDueDay[last] = 0;
            _tabTea[last] = 0;
            _tabRegular[last] = -1;
            _tabCount--;
        }

        /// <summary>Vadesi gelen hesaplari kapatir. Gun acilisinda.</summary>
        private void SettleDueTabs()
        {
            // HasCredit'e bakmiyoruz bilerek: acilmis bir hesap, mekanik
            // ne olursa olsun kapanmali.
            if (_content.Signature.Kind != SignatureKind.Credit) return;
            for (int i = _tabCount - 1; i >= 0; i--)
                if (_tabDueDay[i] <= _day) SettleTab(i, halfChance: false);
        }

        /// <summary>
        /// Komboyu acar veya kapatir. Acikken kombonun ana yemegini secen
        /// grup yanini ve icecegini de KESIN aliyor, ucune birden indirimli
        /// bir fiyat oduyor, ve mutfak daha uzun mesgul kaliyor.
        /// </summary>
        private void SetCombo(bool on)
        {
            if (!HasCombo)
            {
                Emit(SimEventKind.CommandRejected, (int)CommandKind.SetCombo, 15);
                return;
            }
            _comboOn = on;
        }

        /// <summary>Komboyu satabiliyor muyuz: acik VE uc kalem de menude.</summary>
        /// <summary>
        /// Kombo su an satilabilir mi.
        ///
        /// STOK DA SORULUYOR. Once yalnizca menude mi ve acik mi diye
        /// bakiliyordu; tek tek yemek secen yol (PickFromRole) her aday
        /// icin CanMake kontrol ederken kombo blogu onu ATLIYORDU.
        /// Sonucu olculdu: yan ve icecegin malzemesi hic alinmadiginda
        /// bile 24 gunde 325 yan + 318 icecek satildi - tam fiyattan,
        /// SIFIR malzeme maliyetiyle (Consume negatifi sifira kirpiyor).
        /// Yani kombo, stoksuz bedava uretim kapisiydi.
        /// </summary>
        private bool ComboSellable()
        {
            if (!_comboOn || !HasCombo) return false;
            int[] d = _content.Signature.ComboDishes;
            if (d == null) return false;
            for (int i = 0; i < d.Length; i++)
            {
                if (!_dishOnMenu[d[i]] || !Unlocked(d[i])) return false;
                if (!CanMake(d[i], 1)) return false;
            }
            return true;
        }

        /// <summary>Bir kisinin deneyim seviyesi. pool 0 mutfak, 1 salon.</summary>
        public int StaffLevel(int pool, int index)
        {
            int[] xp = pool == 0 ? _cookXpDays : _salonXpDays;
            int count = pool == 0 ? _cooks : _salon;
            if (index < 0 || index >= count || index >= MaxServers) return 0;
            return _economy.XpLevelOf(xp[index]);
        }

        /// <summary>Bir kisinin calistigi gun sayisi. Arayuz ve test icin.</summary>
        public int StaffDaysWorked(int pool, int index)
        {
            int[] xp = pool == 0 ? _cookXpDays : _salonXpDays;
            int count = pool == 0 ? _cooks : _salon;
            if (index < 0 || index >= count || index >= MaxServers) return 0;
            return xp[index];
        }

        /// <summary>
        /// Deneyimin isi kisaltmasi: sure / hiz. Yemegin KENDI pisme suresi
        /// degil, kisinin o ise BAGLI KALDIGI sure kisaliyor - docs/27
        /// Karar D ekipman icin ne diyorsa deneyim icin de o gecerli.
        /// </summary>
        private int XpAdjusted(int pool, int index, int ms)
        {
            int[] xp = pool == 0 ? _cookXpDays : _salonXpDays;
            if (index < 0 || index >= MaxServers) return ms;

            // Personelin toplam hizi: deneyim + huy - moral - yogunluk -
            // yorgunluk. Hepsi ayni carpanda toplaniyor, cunku hepsi ayni
            // seyi soyluyor: bu kisi bu isi ne kadar cabuk bitiriyor.
            int speedBp = _economy.XpSpeedBp(_economy.XpLevelOf(xp[index]), pool == 0);
            speedBp += TraitSum(pool, index, t => t.SpeedBp);

            // docs/14 moral esikleri: 30 altinda hiz -%20.
            if (StaffMorale(pool, index) < _economy.MoraleLowThreshold)
                speedBp -= _economy.MoraleSlowPenaltyBp;

            // Yogunluk ve yorgunluk TOPLANMIYOR, buyugu aliniyor.
            //
            // Toplama ilk yazimdi ve olcum reddetti: kalabalikta panikleyen
            // + cabuk yorulan bir asci, zirvenin son ceyreginde -%45'e
            // dusuyordu; ustune dusuk moral -%20 binince kisi neredeyse
            // duruyordu. Fast food'da iyi oyuncunun itibari 96,5'ten
            // 87'ye indi ve bazi kosularda dukkan 50. gunde bosaldi.
            //
            // docs/14 bu iki huyu AYRI DURUMLAR icin yaziyor ("yogun
            // dilimlerde", "gunun son ceyreginde"); ust uste bindiklerinde
            // ikisini birden odemek tasarimin soyledigi sey degil. Kotu
            // anda kotu olmak yeter, iki kat kotu olmak gerekmiyor.
            int situational = 0;
            if (InPeakSlot() && !TraitAny(pool, index, t => t.PeakImmune))
                situational = TraitSum(pool, index, t => t.PeakPenaltyBp);
            if (InLastQuarter() && !TraitAny(pool, index, t => t.FatigueImmune))
            {
                int f = TraitSum(pool, index, t => t.FatiguePenaltyBp);
                if (f > situational) situational = f;
            }
            speedBp -= situational;

            // Taban %50. Ilk yazimda %20 idi - yani bir gorev bes kat
            // uzayabiliyordu. O kadar derin bir cukur, huyu kisisel bir
            // fark olmaktan cikarip kosuyu belirleyen sey yapiyor.
            if (speedBp < 5000) speedBp = 5000;
            if (speedBp == Fx.One) return ms;

            int adjusted = (int)Fx.MulDiv(ms, Fx.One, speedBp);
            return adjusted < 1 ? 1 : adjusted;
        }

        /// <summary>Servis gununun yogun dilimindeyiz miyiz.</summary>
        private bool InPeakSlot()
        {
            // Yogun dilim EN UZUN dilim: docs/28 Karar G pay degil SURE
            // degistirdi, yani mutfagin zirvesi artik dilim uzunlugunda
            // yaziyor. Sabit "ikinci dilim" yazmak, Turk lokantasinin
            // ogle zirvesini fast food'a da dayatirdi.
            int peak = 0, best = 0;
            for (int i = 0; i < _timing.SlotCount; i++)
                if (_timing.SlotTicks(i) > best) { best = _timing.SlotTicks(i); peak = i; }

            int start = _timing.SlotStartTick(peak);
            return _serviceTick >= start && _serviceTick < start + _timing.SlotTicks(peak);
        }

        /// <summary>Gunun son ceyregi. docs/14 "cabuk yorulan".</summary>
        private bool InLastQuarter()
        {
            return _serviceTick * 4 >= _timing.ServiceTicks * 3;
        }

        /// <summary>
        /// Hal'den malzeme alir. Pesin odenir; kasada yoksa reddedilir.
        /// docs/02: sabah asamasi.
        /// </summary>
        private void OrderIngredient(int ingredient, int grams)
        {
            if (ingredient < 0 || ingredient >= _stockGrams.Length || grams <= 0)
            {
                Emit(SimEventKind.CommandRejected, (int)CommandKind.OrderIngredient, 3);
                return;
            }
            if (!Buy(ingredient, grams))
                Emit(SimEventKind.CommandRejected, (int)CommandKind.OrderIngredient, 9);
        }

        /// <summary>
        /// Satin alir. Parasi yetmiyorsa false doner ve HICBIR SEY
        /// degistirmez.
        ///
        /// Ayri duruyor cunku iki cagiran var: tek kalem alan komut ve
        /// onerilen stogun tamamini alan komut. Ikincisi elli kalemi
        /// dener ve yetmeyenleri sessizce atlar; her biri icin ret olayi
        /// basmak bildirim alanini doldurmaktan baska bir ise yaramaz.
        /// </summary>
        private bool Buy(int ingredient, int grams)
        {
            // MEVSIM FIYATI. docs/09 kampanyayi dort mevsime boluyor ve
            // icerikte 77 malzemenin 35'inin gercek oynamasi var: domates
            // yazin %14 ucuz, kisin %20 pahali. Bu satira kadar simulasyon
            // hep taban fiyati oduyordu.
            long kilo = _content.Ingredients[ingredient].PriceAt(Season, _quality);
            kilo = Fx.MulDiv(kilo, _marketBp[ingredient], Fx.One);
            long cost = Fx.MulDiv(kilo, grams, GramsPerKilo);
            if (cost > _cash) return false;

            _cash -= cost;
            _ingredientSpend += cost;

            // Yeni mal eskisiyle karisiyor: yas AGIRLIKLI ORTALAMA.
            // "Alinca sifirla" demek, her gun bir gram alip saati sonsuza
            // kadar durdurmak demekti.
            int had = _stockGrams[ingredient];
            if (had > 0 && _stockAgeDays[ingredient] > 0)
                _stockAgeDays[ingredient] =
                    (int)Fx.MulDiv(_stockAgeDays[ingredient], had, had + grams);
            else
                _stockAgeDays[ingredient] = 0;

            // Kalite de karisiyor: ucuz alip sonra pahali alan, elindeki
            // ucuz maldan hemen kurtulamiyor.
            int bought = _content.Ingredients[ingredient].QualityDelta(_quality);
            _stockQualityCenti[ingredient] = had > 0
                ? (int)((( long)_stockQualityCenti[ingredient] * had + (long)bought * grams)
                        / (had + grams))
                : bought;

            _stockGrams[ingredient] += grams;
            return true;
        }

        /// <summary>
        /// Kredi ceker. Ayni anda tek kredi tasinabiliyor.
        /// Toplam geri odeme anaparanin 1,35 kati, sekiz haftalik esit taksit.
        /// </summary>
        private void TakeLoan(int option)
        {
            if (option < 0 || option >= _economy.LoanOptions.Length)
            {
                Emit(SimEventKind.CommandRejected, (int)CommandKind.TakeLoan, 10);
                return;
            }
            if (_loanWeeksLeft > 0)
            {
                Emit(SimEventKind.CommandRejected, (int)CommandKind.TakeLoan, 11);
                return;
            }

            long principal = _economy.LoanOptions[option];
            long repay = Fx.Bp(principal, _economy.LoanMultiplierBp);

            _cash += principal;
            _loanTaken += principal;
            _loanInstallment = Fx.CeilDivL(repay, _economy.LoanWeeks);
            _loanWeeksLeft = _economy.LoanWeeks;
            _loanTotalRepaid = 0;
        }

        /// <summary>
        /// Bugunun ZIRVE gunune gore gereken kadro. docs/14 kapasite modeli.
        /// Strateji ve arayuz bunu kullanir; kizgin musteri sayisi kadro
        /// sinyali degildir.
        /// </summary>
        /// <summary>
        /// BUGUN gereken kadro. Adi artik dogru.
        ///
        /// Eski hali her gun HAFTA SONU carpanini kullaniyordu, yani
        /// "bugun" demesine ragmen her zaman ZIRVEYI olcuyordu.
        /// Denge araci bunun bedelini gosterdi: kapasite modelinin
        /// dedigi kadar garson tutan oyuncu, BIR GARSON EKSIK calisan
        /// oyuncudan 5.300 sikke daha az kazaniyordu (18.712'ye 24.022).
        /// Ucret her gun odeniyor, zirve ise haftada iki gun.
        ///
        /// Isim yalan soyluyordu ve yalan pahaliydi: arayuz "bugun 3
        /// kisi gerek" yaziyor, oyuncu tutuyor, para kaybediyordu.
        ///
        /// Zirve ayri bir soru ve ayri bir metot (RequiredCrewPeak):
        /// hafta sonuna hazirlanmak isteyen oyuncunun da onu gormesi
        /// gerekiyor.
        /// </summary>
        public Crew RequiredCrewToday()
        {
            int bugun = ExpectedCustomers(
                IsWeekend(_day) ? _economy.WeekendMultiplierBp
                                : _economy.WeekdayMultiplierBp);
            return StaffingModel.Required(bugun, _economy);
        }

        /// <summary>
        /// YARIN gereken kadro.
        ///
        /// Kadro kararlari AKSAM veriliyor ama ertesi gunu etkiliyor:
        /// bugun hafta ici diye kucuk kadro kuran oyuncu, yarin hafta
        /// sonuysa zirveye eksik kadroyla giriyor. RequiredCrewToday'in
        /// adi dogru, CAGIRANI yanlis zamanda soruyordu.
        /// </summary>
        public Crew RequiredCrewTomorrow()
        {
            int yarin = ExpectedCustomers(
                IsWeekend(_day + 1) ? _economy.WeekendMultiplierBp
                                    : _economy.WeekdayMultiplierBp);
            return StaffingModel.Required(yarin, _economy);
        }

        /// <summary>Hafta sonu zirvesinde gereken kadro.</summary>
        public Crew RequiredCrewPeak()
        {
            int zirve = ExpectedCustomers(_economy.WeekendMultiplierBp);
            return StaffingModel.Required(zirve, _economy);
        }

        public bool HasLoan { get { return _loanWeeksLeft > 0; } }
        public int LoanWeeksLeft { get { return _loanWeeksLeft; } }
        public long LoanInstallment { get { return _loanInstallment; } }

        /// <summary>Haftalik sabit gider: kira, maas ve varsa kredi taksiti.</summary>
        public long WeeklyFixedCost()
        {
            Crew crew = new Crew(_cooks, _salon);
            int week = _day / 7 + 1;

            // Huy carpani BURADA DA olmali. Olmayinca oyuncunun (ve denge
            // aracinin) butce koruyucusu gercek faturayi kucuk gosteriyordu:
            // bir tecrubeli asci maasi %30 buyutuyor, koruyucu bunu
            // gormuyor, kadro ve ekipman o yanlis sayiya gore aliniyordu.
            // Olcum: fast food iyi oyuncusunun itibari 96,5’ten 86,9’a
            // indi ve dukkan 36. gunde bosaldi - tek sebep buydu.
            long wages = Fx.MulDiv(StaffingModel.WeeklyWageBill(crew, week, _economy),
                                   TraitWageMultiplierBp(), Fx.One);
            return wages
                   + _economy.TierForTables(_tableCount).Rent
                   + (_loanWeeksLeft > 0 ? _loanInstallment : 0);
        }

        /// <summary>
        /// Acilis stogu: menunun birinci gun icin istedigi kadar.
        ///
        /// RecommendedRestock zaten menuyu, beklenen talebi, emniyet
        /// payini ve yemek basina tabani biliyor - devraldigin dukkanin
        /// deposunda tam olarak o var. Bir yemekte kullanilmayan
        /// malzemeye sifir donuyor, yani depo ARTIK SECICI.
        /// </summary>
        private void RestockForOneDay()
        {
            for (int i = 0; i < _stockGrams.Length; i++)
            {
                int need = RecommendedRestock(i);
                // Bozulmayan malzeme (tuz, un, yag) biraz bolca: onlar
                // zaten cope gitmiyor ve ilk sabahi yirmi kalem alarak
                // gecirmek, oyunun ilk dakikasini bir hesap tablosuna
                // cevirir.
                if (need > 0 && !_content.Ingredients[i].Perishable)
                    need *= 2;
                _stockGrams[i] = need;
            }
        }

        /// <summary>Bu yemek su anki stokla, verilen porsiyonda yapilabilir mi.</summary>
        public bool CanMake(int dishIndex, int servings)
        {
            if (dishIndex < 0 || dishIndex >= _content.Dishes.Length) return false;
            DishIngredient[] parts = _content.Dishes[dishIndex].Ingredients;
            for (int i = 0; i < parts.Length; i++)
                if (_stockGrams[parts[i].IngredientIndex] < parts[i].Grams * servings)
                    return false;
            return true;
        }

        private void Consume(int dishIndex, int servings)
        {
            if (dishIndex < 0) return;
            DishIngredient[] parts = _content.Dishes[dishIndex].Ingredients;
            for (int i = 0; i < parts.Length; i++)
            {
                int idx = parts[i].IngredientIndex;
                _stockGrams[idx] -= parts[i].Grams * servings;
                if (_stockGrams[idx] < 0) _stockGrams[idx] = 0;
            }
        }

        public int StockOf(int ingredient)
        {
            return (ingredient >= 0 && ingredient < _stockGrams.Length)
                ? _stockGrams[ingredient] : 0;
        }

        /// <summary>
        /// Bugun icin onerilen stok, gram. Menuden hesaplaniyor: her acik
        /// yemegin beklenen porsiyonu, o yemegin tarifindeki gramajla carpiliyor.
        ///
        /// Bu, docs/02 ilke 2'deki "malzeme siparisi otomatiklesir"
        /// ozelliginin cekirdek tarafi. Oyuncu tek dokunusla bunu
        /// siparis edebilir; elle secmek isteyen secer.
        /// </summary>
        public int RecommendedRestock(int ingredient)
        {
            return RecommendedRestock(ingredient, ignoreStock: false);
        }

        /// <summary>
        /// ignoreStock: elde ne varsa yok sayar, yani BIR GUNLUK
        /// ihtiyacin kendisini verir.
        /// </summary>
        private int RecommendedRestock(int ingredient, bool ignoreStock)
        {
            if (ingredient < 0 || ingredient >= _stockGrams.Length) return 0;

            int dayFactorBp = IsWeekend(_day)
                ? _economy.WeekendMultiplierBp : _economy.WeekdayMultiplierBp;
            int people = ExpectedCustomers(dayFactorBp);

            // Veresiye sadakati: geri gelen musteri. Tavani icerikte.
            if (_creditLoyaltyBp > 0)
                people = (int)Fx.MulDiv(people, Fx.One + _creditLoyaltyBp, Fx.One);
            if (people <= 0) return 0;

            // Her roldeki ACIK yemek sayisi; siparisler rol icinde
            // esit dagiliyor.
            //
            // Yan ve icecek icin bir zamanlar sabit "/4" yaziyordu -
            // "yaklasik dort secenek var" demek. Menude tek bir icecek
            // acikken o bolme, ihtiyacin dortte birini stokluyor ve
            // musteri kapidan donuyordu. Sayim artik gercek.
            int mains = 0, sides = 0, drinks = 0, desserts = 0;
            for (int i = 0; i < _content.Dishes.Length; i++)
            {
                if (!_dishOnMenu[i] || !Unlocked(i)) continue;
                string g = _content.Dishes[i].Group;
                if (_content.IsInRole(g, _content.MainGroups)) mains++;
                else if (_content.IsInRole(g, _content.SideGroups)) sides++;
                else if (_content.IsInRole(g, _content.DrinkGroups)) drinks++;
                else if (_content.IsInRole(g, _content.DessertGroups)) desserts++;
            }
            if (mains == 0) mains = 1;
            if (sides == 0) sides = 1;
            if (drinks == 0) drinks = 1;
            if (desserts == 0) desserts = 1;

            long grams = 0;
            for (int i = 0; i < _content.Dishes.Length; i++)
            {
                DishDef d = _content.Dishes[i];
                if (!_dishOnMenu[i] || !Unlocked(i)) continue;

                // Ana yemek herkese; yan, icecek ve TATLI olasilikla.
                //
                // Tatli buraya sonradan geldi ve gelmesi sart: siparis
                // modeli tatliyi ISTIYOR (DessertChanceBp, satir 3271)
                // ama hal modeli onun malzemesini HIC ALMIYORDU. Yani
                // her iki mutfagin tatlilari, acilis stogu bitince
                // ulasilamaz oluyordu - CanMake false donuyor, siparis
                // sessizce -1'e dusuyor ve oyuncu hicbir sey gormuyordu.
                //
                // Acilis stogu yetmis yedi malzemenin hepsine altisar
                // kilo koydugu surece ortuluydu; stok menuye baglaninca
                // testler "hic TATLI siparis edilmedi" diye kirildi.
                int shareBp;
                if (_content.IsInRole(d.Group, _content.MainGroups))
                    shareBp = Fx.One / mains;
                else if (_content.IsInRole(d.Group, _content.SideGroups))
                    shareBp = _economy.SideChanceBp / sides;
                else if (_content.IsInRole(d.Group, _content.DrinkGroups))
                    shareBp = _economy.DrinkChanceBp / drinks;
                else if (_content.IsInRole(d.Group, _content.DessertGroups))
                    shareBp = _economy.DessertChanceBp / desserts;
                else
                    continue;

                for (int k = 0; k < d.Ingredients.Length; k++)
                {
                    if (d.Ingredients[k].IngredientIndex != ingredient) continue;

                    long expected = Fx.MulDiv((long)people * d.Ingredients[k].Grams,
                                              shareBp, Fx.One);

                    // Taban: menude duran her yemek EN AZ bir grubu
                    // karsilayabilmeli. Stok kontrolu grup basina yapiliyor;
                    // gunluk ortalama yeterli gorunse de tek bir dort kisilik
                    // grup o yemegi isteyince stok yetmiyor ve musteri
                    // kapidan donuyor.
                    long floorGrams = (long)d.Ingredients[k].Grams * MinPartyBuffer;
                    grams += expected > floorGrams ? expected : floorGrams;
                }
            }

            // %20 emniyet payi: talep dalgalaniyor, tukenen mutfak musteri kaybettiriyor.
            grams = Fx.Bp(grams, 12000);

            long missing = grams - (ignoreStock ? 0 : _stockGrams[ingredient]);
            return missing > 0 ? (int)missing : 0;
        }

        /// <summary>
        /// Onerilen stogun tamamini alir. Parasi yetmeyen kalemler
        /// sessizce atlaniyor - elli ayri ret olayi, gunun bildirim
        /// alanini doldurmaktan baska bir sey yapmaz.
        /// </summary>
        private void OrderRecommended()
        {
            if (_phase != DayPhase.Morning)
            {
                Emit(SimEventKind.CommandRejected,
                     (int)CommandKind.OrderRecommended, 2);
                return;
            }

            int bought = 0;
            for (int i = 0; i < _stockGrams.Length; i++)
            {
                int need = RecommendedRestock(i);
                if (need <= 0) continue;
                if (Buy(i, need)) bought++;
            }
            // Hicbir sey alinamadiysa oyuncuya soylenmeli: dugmeye
            // basildi ve kasa kipirdamadi.
            if (bought == 0)
                Emit(SimEventKind.CommandRejected,
                     (int)CommandKind.OrderRecommended, 9);
        }

        /// <summary>
        /// BIR GUNLUK ihtiyac, elde ne varsa ondan bagimsiz.
        ///
        /// RecommendedRestock "eksigi" veriyor, yani stok doluyken sifir
        /// donuyor. Arayuzun fazladan alim teklif edebilmesi icin ihtiyacin
        /// KENDISI lazim: ucuz bir gunde uc gunluk almak, soguk hava
        /// deposunun satin aldigi seyin ta kendisi - ve o karar, ihtiyac
        /// bilinmeden sunulamiyor.
        /// </summary>
        public int DailyNeed(int ingredient)
        {
            if (ingredient < 0 || ingredient >= _stockGrams.Length) return 0;

            // ELDEKI STOGU SIFIRLAMADAN hesapliyor.
            //
            // Once stogu gecici olarak sifirlayip RecommendedRestock
            // cagiriyor ve sonra geri yaziyordu. Bu bir OKUMA fonksiyonu
            // ve gorunum katmani onu hal ekraninin her kurulusunda
            // malzeme basina cagiriyor - "gorunum simulasyonu okur, ona
            // yazmaz" kuralinin tam ortasinda bir yazma.
            //
            // Zararsiz gorunuyordu cunku geri yazma hemen arkasinda; ama
            // aradaki hesapta bir tasma olsaydi (Fx.MulDiv tasmada
            // atiyor) stok KALICI OLARAK sifir kalirdi.
            return RecommendedRestock(ingredient, ignoreStock: true);
        }

        /// <summary>
        /// Bu malzemeden EN FAZLA kac gunluk alinmasi mantikli.
        ///
        /// Bozulmayan malzemede sinir yok (uc gun yeter, fazlasi nakit
        /// baglamak). Bozulabilende soguk havanin tuttugu kadar: soguk
        /// hava yoksa bir gun, cunku gece hepsi gidiyor.
        /// </summary>
        public int MaxUsefulDays(int ingredient)
        {
            if (!IsPerishable(ingredient)) return 3;
            int keep = KeepDays(ingredient);
            if (keep < 1) keep = 1;
            return keep > 3 ? 3 : keep;
        }

        /// <summary>Bugun beklenen musteri sayisi. Strateji ve arayuz icin.</summary>
        /// <summary>
        /// Menudeki yemeklerin kaci su anki stokla YAPILABILIR.
        ///
        /// Sabahki hazirlik ozeti icin: oyuncu servisi acmadan once
        /// "stok bugunu cikarir mi" sorusunun cevabini gormeli. Bir
        /// yemek yapilabiliyorsa gun aksar; hicbiri yapilamiyorsa gun
        /// bastan kayiptir ve bunu aksam raporunda ogrenmek gec.
        ///
        /// Kaba ama dogru bir olcu: menudeki yapilabilir yemek sayisi.
        /// "Kac gun yeter" gercek cevabi icin talep tahmini gerekir ve
        /// o, sabah ekraninda tasiyamayacagi kadar belirsiz bir sayi.
        /// </summary>
        /// <summary>
        /// Menude KAC YEMEK yapilabiliyor. Gun DEGIL, yemek sayisi.
        ///
        /// Adi bir zamanlar StockDaysLeft idi ve gun vaat ediyordu;
        /// govdesi ise "en az bir porsiyonu yapilabilen yemek" sayiyordu.
        /// Arayuz de ona bakip yesil tik veriyordu: alti yemegin her
        /// birinden BIRER porsiyonu olan oyuncu "hazir" gorunuyor,
        /// servisi aciyor ve ilk on dakikada mal bitiyordu.
        ///
        /// Ad artik ne yaptigini soyluyor; "gune yetiyor mu" sorusunun
        /// cevabi StockCoverageBp'de.
        /// </summary>
        public int MakeableDishCount()
        {
            int yapilabilir = 0;
            for (int i = 0; i < _dishOnMenu.Length; i++)
            {
                if (!_dishOnMenu[i] || !Unlocked(i)) continue;
                if (CanMake(i, 1)) yapilabilir++;
            }
            return yapilabilir;
        }

        /// <summary>
        /// Bugunun beklenen talebinin yuzde kaci elde var. 10000 = tamami.
        ///
        /// OLCUT EN KIT MALZEME, toplam degil. Stogun toplamina bakmak
        /// yaniltir: yirmi malzemesi bol, biri bitmis bir mutfak toplamda
        /// "dolu" gorunur ama o bir malzemeyi isteyen her siparis
        /// kapidan doner. Gunu belirleyen sey en kit olan.
        ///
        /// Ihtiyac, halin kendi hesabindan geliyor (RecommendedRestock,
        /// ignoreStock: true) - yani ekranin vaat ettigi sayi ile hal
        /// ekraninin onerdigi miktar AYNI kaynaktan cikiyor. Iki ayri
        /// hesap olsaydi biri otekini yalanlardi.
        /// </summary>
        public int StockCoverageBp()
        {
            int enAz = int.MaxValue;
            for (int i = 0; i < _stockGrams.Length; i++)
            {
                int gerek = RecommendedRestock(i, true);
                if (gerek <= 0) continue;

                int var = _stockGrams[i];
                int bp = var >= gerek ? Fx.One : (int)Fx.MulDiv(var, Fx.One, gerek);
                if (bp < enAz) enAz = bp;
            }

            // Hicbir malzeme gerekmiyorsa menu bostur; o ayri bir uyari.
            return enAz == int.MaxValue ? 0 : enAz;
        }

        public int ExpectedPeopleToday()
        {
            int dayFactorBp = IsWeekend(_day)
                ? _economy.WeekendMultiplierBp : _economy.WeekdayMultiplierBp;
            return ExpectedCustomers(dayFactorBp);
        }

        /// <summary>
        /// Yemegin kilidi acik mi. Uc sart birden:
        ///   1. gun geldi mi        (tempo tabani, docs/09)
        ///   2. itibar yetiyor mu   (kazanilan sey)
        ///   3. ekipman var mi      (satin alinan sey)
        ///
        /// Ikinci ve ucuncu sart olmadan kilit bir TAKVIMDI: oyuncu hicbir
        /// sey yapmadan yemekler kendiliginden aciliyordu. Simdi ekipman
        /// almak menu aciyor, yani ekipman merdiveni yalnizca hiz degil
        /// ICERIK satin aliyor.
        /// </summary>
        private bool Unlocked(int dish)
        {
            DishDef d = _content.Dishes[dish];
            if (d.UnlockDay > _day) return false;
            if (_reputationCenti < d.UnlockReputationCenti) return false;
            if (d.RequiresStationTier > 0
                && _stationTier[d.StationIndex] < d.RequiresStationTier) return false;
            return true;
        }

        public bool IsUnlocked(int dish)
        {
            return dish >= 0 && dish < _content.Dishes.Length
                && Unlocked(dish);
        }

        /// <summary>Bu yemek su an menude mi. Arayuz icin.</summary>
        /// <summary>
        /// Bu yemek ANA yemek mi. Rol icerikten geliyor; mutfaklar kendi
        /// grup adlarini kullaniyor (docs/33) ve sabit bir liste ikinci
        /// mutfakta sifir musteri uretmisti.
        /// </summary>
        public bool IsMainDish(int dish)
        {
            if (dish < 0 || dish >= _content.Dishes.Length) return false;
            return _content.IsInRole(_content.Dishes[dish].Group, _content.MainGroups);
        }

        public bool IsOnMenu(int dish)
        {
            return dish >= 0 && dish < _dishOnMenu.Length && _dishOnMenu[dish];
        }

        public bool IsMain(int dish)
        {
            return dish >= 0 && dish < _content.Dishes.Length
                && _content.IsInRole(_content.Dishes[dish].Group, _content.MainGroups);
        }

        public int IngredientCount { get { return _content.Ingredients.Length; } }
        public long IngredientPrice(int i) { return _content.Ingredients[i].BasePrice; }
        public int StockOutEvents { get { return _stockOutEvents; } }

        private void Intervene(int party, InterventionKind kind)
        {
            // Istasyon acele ettirme, masaya degil ISTASYONA yapiliyor;
            // A alani orada masa degil istasyon indisi.
            if (kind == InterventionKind.RushStation)
            {
                RushStation(party);
                return;
            }

            // BILINMEYEN TUR SESSIZCE CAYA DUSMUYOR.
            //
            // Asagidaki dallar "OwnerAttention mi?" diye soruyor ve
            // degilse CAY gibi davraniyordu. Yani tanimsiz bir tur
            // (ornegin default(InterventionKind)) cayin etkisini
            // PARASINI ODEMEDEN aliyordu. Artik acikca reddediliyor.
            if (kind != InterventionKind.FreeTea
                && kind != InterventionKind.OwnerAttention)
            {
                Emit(SimEventKind.CommandRejected, (int)CommandKind.Intervene, 10);
                return;
            }

            if (_interventionsLeft <= 0)
            {
                Emit(SimEventKind.CommandRejected, (int)CommandKind.Intervene, 8);
                return;
            }

            // CAY ARTIK SALONA GIDIYOR, TEK MASAYA DEGIL.
            //
            // Eski hali UC FIILDEN BIRINI OLU BIRAKIYORDU. Cay her
            // eksende patron ilgisinin altindaydi: memnuniyet 900'e
            // karsi 2400, sabir x1'e karsi x2, mutfagi hizlandirmiyor -
            // ve ustelik KASADAN PARA CIKARIYOR, ilgi bedava. Ayni
            // mudahale hakkini yaktiklari icin cayin basilmasi icin
            // hicbir gun yoktu. Anlayan oyuncu altmis gun boyunca o
            // dugmeye hic basmiyordu; anlamayan para odeyip yarisini
            // aliyordu. Ekranda yer kaplayan bir tuzakti.
            //
            // Simdi ikisi FARKLI SORUYA cevap veriyor:
            //   ilgi -> BIR masaya derin mudahale (x2 sabir + mutfagi
            //           one alma). Krizdeki tek masa icin.
            //   cay  -> BEKLEYEN HERKESE sig mudahale. Zirvede, alti
            //           masa birden sabirsizlanirken.
            //
            // Bedeli de oradan geliyor: cay artik salondaki BUTUN
            // bekleyenlerin kisi sayisi kadar tutuyor. Yani kalabalikta
            // hem en degerli hem en pahali.
            //
            // HEDEF ISTEMIYOR, o yuzden parti gecerliligi bu daldan
            // SONRA kontrol ediliyor. Ilk yazista kontrolun altindaydi
            // ve arayuzun secim yokken yolladigi -1 sessizce
            // reddediliyordu: dugme hicbir sey yapmiyordu. Test yakaladi.
            if (kind == InterventionKind.FreeTea)
            {
                int kisi = 0;
                for (int i = 0; i < MaxParties; i++)
                    if (_pActive[i] && DrainRateBp(i) > 0) kisi += _pSize[i];

                // Bekleyeni olmayan salonda gonderilecek kimse yok;
                // bos yere hak yakmasin.
                if (kisi <= 0)
                {
                    Emit(SimEventKind.CommandRejected, (int)CommandKind.Intervene, 11);
                    return;
                }

                long cost = _economy.TreatCost * kisi;
                if (_cash < cost)
                {
                    Emit(SimEventKind.CommandRejected, (int)CommandKind.Intervene, 9);
                    return;
                }
                _cash -= cost;
                _teaSpend += cost;
                _interventionsLeft--;

                int ekstra = _timing.SeatOrderMs * _economy.TreatPatienceMult;
                for (int i = 0; i < MaxParties; i++)
                {
                    if (!_pActive[i] || DrainRateBp(i) <= 0) continue;
                    _pTea[i] = true;
                    _pBonusCenti[i] += _economy.TreatSatisfactionCenti;
                    _pPatienceLeftMs[i] += ekstra;
                }
                return;
            }

            if (party < 0 || party >= MaxParties || !_pActive[party])
            {
                Emit(SimEventKind.CommandRejected, (int)CommandKind.Intervene, 6);
                return;
            }

            _interventionsLeft--;

            // ODUL MEMNUNIYETE DEGIL, ZAMANA.
            //
            // Olculdu: mudahale eden bot, hic mudahale etmeyenden DAHA AZ
            // kazaniyordu (26.526'ya 26.969). Sebep matematikti - gunde
            // dort mudahale x yirmi grup, gruplarin %20'sine +20 puan,
            // yani ortalamaya +4 puan; ortalama memnuniyet zaten 78 ve
            // itibarin tek esigi 62. Odul DOYMUS bir eksene odeniyordu ve
            // hicbir seyi degistiremiyordu.
            //
            // Sabir eklemek ise doymuyor: bekleyen masa gitmiyor, masa
            // devir hizi artiyor, ve o dogrudan ciro demek. Patron
            // ilgisi bir gunu KURTARIYOR, guzellestirmiyor.
            //
            // Buraya artik yalnizca OwnerAttention geliyor: cay
            // yukaridaki dalda kendi yolunu tamamlayip donuyor.
            _pBonusCenti[party] += _economy.AttentionSatisfactionCenti;

            _pPatienceLeftMs[party] += _timing.SeatOrderMs
                                     * _economy.AttentionPatienceMult;

            // Yemegi bekleyen bir masaya ilgi gostermek, mutfaktaki
            // isini de one aliyor: patron yolu aciyor, pisirmiyor.
            HurryPartyJob(party);

            // VE SALON ISINI DE PATRON USTLENIYOR.
            //
            // Ilgi eskiden yalnizca sabri uzatiyor ve MUTFAGI one
            // aliyordu; salon tarafina hic dokunmuyordu, oysa darbogaz
            // cogu zaman orada. Olculdu: mudahale eden bot etmeyenle
            // ayni yerde bitiyordu (18.869 / 18.670), cunku mekanik
            // yalnizca KRIZ aninda ise yariyordu ve kriz neredeyse hic
            // olmuyor.
            //
            // Siradaki salon isi kisaliyor: masa daha cabuk donuyor,
            // yani ayni gunde daha cok musteri.
            _pAttended[party] = true;
        }

        /// <summary>
        /// Bu grubun mutfaktaki isini kisaltir.
        ///
        /// Patron PISIRMIYOR (docs/14 yasakliyor); onceligi degistiriyor.
        /// Etki, isin kalan duvar saatinin ucte biri kadar.
        /// </summary>
        private void HurryPartyJob(int party)
        {
            // IKI HATA BIRDEN VARDI.
            //
            // (1) INDIS UZAYI. _kitchenTaskTarget bir GRUP degil bir IS
            //     tutuyor: DispatchKitchen "job" yaziyor ve job =
            //     party * MaxJobsPerParty + k. Burada ham karsilastirma
            //     yapiliyordu (target != party), yani 7 numarali grubun
            //     isi hic hizlanmiyor, onun yerine IS INDISI 7 olan is -
            //     1 numarali grubun dorduncu kalemi - hizlaniyordu.
            //     0-3 numarali gruplarda hepsi 0. grubun isine gidiyordu.
            //     CancelTasksFor bir fonksiyon asagida bolmeyi DOGRU
            //     yapiyor; burada unutulmus.
            //
            // (2) YANLIS SURE. Kisaltilan sey _kitchenTaskLeftMs idi:
            //     ascinin o ise BAGLI KALMA suresi. Yemegin duvar saati
            //     _jobMs ve musterinin bekledigi sey o. RushStation
            //     dogrusunu yapiyor.
            //
            // Ikisi birlikte sunu uretiyordu: oyuncu hakkini harciyor,
            // "ilgi gosterildi" balonunu okuyor, mutfakta hicbir sey
            // degismiyor.
            for (int j = 0; j < _jobStation.Length; j++)
            {
                if (_jobState[j] == 0) continue;
                if (j / MaxJobsPerParty != party) continue;

                int cut = _jobMs[j] / 3;
                _jobMs[j] -= cut;
                if (_jobMs[j] < TimingConfig.TickMs) _jobMs[j] = TimingConfig.TickMs;
            }
        }

        /// <summary>
        /// Sabri en az kalan, henuz mudahale gormemis grup. -1 yoksa.
        ///
        /// "Henuz gormemis" sarti onemli: ayni masaya ust uste mudahale
        /// etmek gunun hakkini bir masaya harcamak olurdu.
        /// </summary>
        public int MostImpatientParty()
        {
            int best = -1;
            int least = int.MaxValue;
            for (int i = 0; i < MaxParties; i++)
            {
                if (!_pActive[i] || _pBonusCenti[i] != 0) continue;
                if (_pStage[i] == CustomerStage.Eating) continue;
                if (_pPatienceLeftMs[i] >= least) continue;
                least = _pPatienceLeftMs[i];
                best = i;
            }
            return best;
        }

        /// <summary>
        /// Istasyonu acele ettirir: o istasyonda pisen butun islerin kalan
        /// duvar saatinden pay siliniyor.
        ///
        /// Patron PISIRMIYOR (docs/14); yolu aciyor. O yuzden ascinin
        /// bagli kaldigi sure degil, isin kalan suresi kisaliyor.
        /// </summary>
        private void RushStation(int station)
        {
            if (station < 0 || station >= _stationBusy.Length)
            {
                Emit(SimEventKind.CommandRejected, (int)CommandKind.Intervene, 6);
                return;
            }
            if (_interventionsLeft <= 0)
            {
                Emit(SimEventKind.CommandRejected, (int)CommandKind.Intervene, 8);
                return;
            }

            int touched = 0;
            for (int j = 0; j < _jobStation.Length; j++)
            {
                if (_jobState[j] != 1 || _jobStation[j] != station) continue;
                int cut = (int)Fx.MulDiv(_jobMs[j], _economy.RushCutBp, Fx.One);
                _jobMs[j] -= cut;
                if (_jobMs[j] < TimingConfig.TickMs) _jobMs[j] = TimingConfig.TickMs;
                touched++;
            }

            if (touched == 0)
            {
                // Bos istasyonu acele ettirmek hakki yakmasin.
                Emit(SimEventKind.CommandRejected, (int)CommandKind.Intervene, 10);
                return;
            }

            _interventionsLeft--;
            Emit(SimEventKind.StationRushed, station, touched);
        }

        /// <summary>
        /// En sikisik istasyon: kuyrugu en uzun olan. -1 hicbiri mesgul
        /// degilse. Mudahale hedefi secmek icin.
        /// </summary>
        public int BusiestStation()
        {
            int best = -1, most = 0;
            for (int st = 0; st < _stationBusy.Length; st++)
            {
                int waiting = 0;
                for (int j = 0; j < _jobStation.Length; j++)
                    if (_jobStation[j] == st && _jobState[j] == 1) waiting++;
                if (waiting > most) { most = waiting; best = st; }
            }
            return best;
        }

        /// <summary>Bugun kalan patron mudahalesi hakki.</summary>
        /// <summary>
        /// Bugunun mudahale hakki. TAVAN MASA SAYISINA BAGLI.
        ///
        /// Sabit dorttu ve mekanik tam da en gerekli oldugu yerde
        /// siliniyordu: dort masalik bir dukkanda dort hak gunun
        /// krizlerinin cogunu kapatiyor, on dort masalik hafta sonu
        /// zirvesinde kucuk bir kismini. Yani oyunun GEC bolumunde
        /// oyuncu erken bolumunden DAHA AZ karar veriyordu - buyumek
        /// ajansi arttirmiyor, eritiyordu.
        ///
        /// Dort masada taban korunuyor (oyunun acilisi degismesin),
        /// her dort masa basina bir hak ekleniyor: 4 masa 4, 8 masa 5,
        /// 12 masa 6, 14 masa 6.
        /// </summary>
        public int InterventionsToday
        {
            get
            {
                // Taban masa sayisi ICERIKTEN: ilk kademe. Sabit 4
                // yazmak, kademeler degisince sessizce yanlis olurdu.
                int ek = (_tableCount - _economy.TierAt(0).Tables) / 4;
                if (ek < 0) ek = 0;
                return _economy.InterventionsPerDay + ek;
            }
        }

        private int _plannedPeople;

        /// <summary>
        /// Bugun GERCEKTEN gelmesi planlanan kisi sayisi.
        ///
        /// ExpectedPeopleToday BEKLENTIYI veriyor; ikisinin farki gunun
        /// sapmasi. Oyuncu bunu goremiyor (gormemeli - gorebilseydi
        /// oynaklik yine dekor olurdu); testler ve tur icin var.
        /// </summary>
        public int PlannedPeopleToday { get { return _plannedPeople; } }

        public int InterventionsLeft { get { return _interventionsLeft; } }

        /// <summary>
        /// Su an salonda olup kendisine cay ikram edilmis grup sayisi.
        ///
        /// Cayin SALONA gittigini sinamak icin var: tek bir cay birden
        /// cok masaya dokunmali. Bu sayi olmadan "hepsine gitti" ile
        /// "birine gitti" ayirt edilemiyordu.
        /// </summary>
        public int PartiesWithTea
        {
            get
            {
                int n = 0;
                for (int i = 0; i < MaxParties; i++)
                    if (_pActive[i] && _pTea[i]) n++;
                return n;
            }
        }

        /// <summary>
        /// Gunluk mudahale hakki. Arayuz ipucunun sayiyi ICERIKTEN
        /// okuyabilmesi icin: metne elle yazilan bir sayi, denge araci
        /// degeri degistirdiginde sessizce yalan soyler.
        /// </summary>
        public int InterventionsPerDay { get { return _economy.InterventionsPerDay; } }

        private void Expand(int tierIndex)
        {
            if (tierIndex < 0 || tierIndex >= _economy.TierCount)
            {
                Emit(SimEventKind.CommandRejected, (int)CommandKind.Expand, 7);
                return;
            }
            TierConfig t = _economy.TierAt(tierIndex);
            if (t.Tables <= _tableCount || _cash < t.Upgrade)
            {
                Emit(SimEventKind.CommandRejected, (int)CommandKind.Expand, 8);
                return;
            }
            _cash -= t.Upgrade;
            _expansionSpend += t.Upgrade;

            // BUYUYEN DUKKAN YENI TABAK GETIRIYOR.
            //
            // Fark kadar, ve TEMIZ olarak. Toplami kademeye baglamak
            // yerine fark eklemek sart: kirli yigin ve masadakiler
            // yerinde duruyor, toplami yeniden yazmak onlari yok ederdi
            // ve degismez bozulurdu.
            int eskiTabak = t.Plates - _economy.TierForTables(_tableCount).Plates;
            if (eskiTabak > 0) _platesClean += eskiTabak;

            _tableCount = t.Tables;

            // TAVANDA BIRIKEN ITIBAR BURADA ODENIYOR.
            //
            // Genisleme yalnizca masa satin almiyor: dar tavanda
            // verilen iyi servisin karsiligi da o gun geliyor. Boylece
            // tavandaki gunler karsiliksiz gecmiyor ve genisleme
            // "yeni masalar" degil "birikmis unun serbest kalmasi"
            // gibi okunuyor.
            if (_reputationOverflowCenti > 0)
            {
                _reputationCenti += _reputationOverflowCenti;
                _reputationOverflowCenti = 0;

                int yeniCap = _economy.TierForTables(_tableCount).ReputationCapCenti;
                if (yeniCap <= 0 || yeniCap > 10000) yeniCap = 10000;
                if (_reputationCenti > yeniCap) _reputationCenti = yeniCap;
            }
        }

        // ====================================================================
        // Tick
        // ====================================================================
        public void Tick()
        {
            if (_phase == DayPhase.Service) TickService();
            _tickIndex++;
        }

        private void TickService()
        {
            SpawnArrivals();
            AdvancePatience();
            SeatWaitingParties();
            DispatchKitchen();
            PlateUp();
            DispatchSalon();
            AdvanceTasks();
            AdvanceEating();
            _serviceTick++;
        }

        // ---- 1. gelisler ---------------------------------------------------
        private void SpawnArrivals()
        {
            while (_arrNext < _arrCount && _arrTick[_arrNext] <= _serviceTick)
            {
                int slot = FindFreeParty();
                if (slot < 0) { _arrNext++; continue; }   // havuz dolu, musteri kaybi

                int arch = _arrArchetype[_arrNext];
                ArchetypeDef a = _content.Archetypes[arch];

                _pActive[slot] = true;
                _pArchetype[slot] = arch;
                _pSize[slot] = _arrSize[_arrNext];
                _pStage[slot] = CustomerStage.WaitingForTable;
                _pTable[slot] = -1;
                _pPatienceTotalMs[slot] = a.PatienceMs;
                _pPatienceLeftMs[slot] = a.PatienceMs;
                _pWaitedMs[slot] = 0;
                _pEatLeftMs[slot] = 0;
                _pSatisfactionCenti[slot] = 0;
                _pBonusCenti[slot] = 0;
                _pInTask[slot] = false;
                _pKitchenTask[slot] = false;
                _pWarned[slot] = false;
                _pRegular[slot] = _arrRegular[_arrNext];
                _pServer[slot] = -1;
                _pCook[slot] = -1;
                PickOrder(slot, a.PatienceMs);

                // Menude yapabilecegi bir ana yemek yoksa musteri KAPIDAN
                // doner. Masaya oturup sabri bitene kadar beklemez: bu hem
                // gercek disi olurdu hem de bir stok hatasina kirk dakika
                // bekletilmis musterinin itibar cezasini verirdi.
                if (_pDishMain[slot] < 0)
                {
                    _pActive[slot] = false;
                    _stockOutEvents++;
                    _turnedAwayParties++;
                    AccumulateReputation(slot, TurnAwaySatisfactionCenti);
                    Emit(SimEventKind.TurnedAway, slot, arch, _pSize[slot]);
                    _arrNext++;
                    continue;
                }

                _partyCount++;
                Emit(SimEventKind.CustomerArrived, slot, arch, _pSize[slot]);
                _arrNext++;
            }
        }

        private int FindFreeParty()
        {
            for (int i = 0; i < MaxParties; i++)
                if (!_pActive[i]) return i;
            return -1;
        }


        /// <summary>
        /// Bir kisilik siparis: ANA yemek kesin, yan ve icecek olasilikli.
        ///
        /// Neden tek kalem degil: denge aracinin ilk kosusunda pasif oyuncu
        /// batmiyor, iyi oynayan bativordu. Sebep ortalama fisin cok dusuk
        /// olmasiydi; menuden esit olasilikla tek kalem secilince musterilerin
        /// buyuk kismi sadece icecek aliyordu. docs/07 kombo mekaniginin tabani.
        ///
        /// Aceleci musteri agir yemek siparis etmez: aday yemekler
        /// prepMs &lt;= sabir x katsayi olanlar. Bu olmadan sabri 8 sn olan
        /// kurye hicbir zaman servis edilemezdi.
        /// </summary>
        private void PickOrder(int slot, int patienceMs)
        {
            long limit = Fx.MulDiv(patienceMs, _timing.OrderPatienceFactorBp, Fx.One);

            int servings = _pSize[slot];
            _pAskedDish[slot] = AskForMissingDish();

            ArchetypeDef arch = _content.Archetypes[_pArchetype[slot]];

            // Roller ICERIKTEN geliyor: fast food'da ana/yan/icecek, Turk
            // lokantasinda sulu+izgara / corba+pilav+meze / icecek.
            _pDishMain[slot] = PickFromRole(_content.MainGroups, limit, true, servings, arch);

            // Duzenli musteri SEVDIGI yemegi ister. Menude yoksa hayal
            // kirikligi: geldigi tek sey oydu ve bulamadi.
            _pMissedFavourite[slot] = false;
            int reg = _pRegular[slot];
            if (reg >= 0 && reg < RegularCount)
            {
                int fav = _content.Regulars[reg].FavouriteDish;
                if (_dishOnMenu[fav] && Unlocked(fav) && CanMake(fav, servings))
                    _pDishMain[slot] = fav;
                else if (_pDishMain[slot] >= 0)
                    _pMissedFavourite[slot] = true;
            }
            _pDishSide[slot] = _rngOrder.Chance(_economy.SideChanceBp)
                ? PickFromRole(_content.SideGroups, limit, false, servings, arch) : -1;
            _pDishDrink[slot] = _rngOrder.Chance(_economy.DrinkChanceBp)
                ? PickFromRole(_content.DrinkGroups, limit, false, servings, arch) : -1;
            _pDishDessert[slot] = _rngOrder.Chance(ExtrasChanceBp(arch, _economy.DessertChanceBp))
                ? PickFromRole(_content.DessertGroups, limit, false, servings, arch) : -1;

            // Veresiye isteyen musteri. Karar odeme aninda verilecek ama
            // KIMIN soracagi burada, gelis planiyla ayni belirlenimcilikte
            // atiliyor - servis sirasinda rastgelelik cagirmiyoruz.
            _pAsksCredit[slot] = HasCredit
                && CreditIdentityOk(slot)
                && _rngCredit.Chance(_content.Signature.CreditAskChanceBp);

            // Kombo: ana yemek HANGISI OLURSA OLSUN yan ve icecek KESIN
            // geliyor. Fis buyuyor (ihtimal degil kesinlik), mutfak yuku de
            // buyuyor - docs/07: "dogru kombo kurgusu ortalama fisi
            // yukseltir AMA mutfak yukunu artirir."
            //
            // Once TEK BIR ana yemege bagliydi (kombonun kendi anasi,
            // yani hamburger). Olculdu: siparislerin yalnizca %6'sinda
            // tetikleniyordu, cunku menude bes ana yemek acik kaliyor ve
            // duzenli musterinin favorisi de ana yemegi eziyor. Ortalama
            // fise katkisi +0,7 sikke, yani %1,3 - oysa docs/12 %44
            // vadediyor. Gruba baglamak, imza mekanigini gercekten
            // hissedilir yapiyor.
            _pCombo[slot] = false;
            // PAYDA YALNIZCA MEKANIK ACIKKEN SAYIYOR.
            //
            // Kosulsuz sayiyordu, oysa kombo 16. gunde aciliyor: payda
            // payin YAPISAL OLARAK SIFIR oldugu on bes gunu de
            // iceriyordu ve eksen gercek kullanimi ucte bir oraninda
            // eksik gosteriyordu. Olcum adiyla soylemeli: "komboya
            // donebilecek siparislerin yuzde kaci komboya dondu".
            if (HasCombo && IsMain(_pDishMain[slot])) _mainOrders++;
            if (ComboSellable() && IsMain(_pDishMain[slot]))
            {
                _comboOrders++;
                int[] cd = _content.Signature.ComboDishes;
                _pDishSide[slot] = cd[1];
                _pDishDrink[slot] = cd[2];
                _pCombo[slot] = true;
                Emit(SimEventKind.ComboOrdered, slot, (int)ComboPrice(slot));
            }

            // Ana yemek bulunamadiysa cagiran taraf (SpawnArrivals) grubu
            // kapidan cevirir; burada sadece -1 birakiyoruz.
        }

        /// <summary>
        /// Musteri, DUYDUGU ama yapilamayan bir yemegi soruyor mu.
        ///
        /// Aday: gunu ve itibari gelmis, ama ekipmani alinmamis ana yemek.
        /// Yani oyuncunun ELINDE olan bir eksik; takvimin daha getirmedigi
        /// yemek sorulmuyor, o haksizlik olurdu.
        /// </summary>
        private int AskForMissingDish()
        {
            int n = 0, total = 0;
            for (int i = 0; i < _content.Dishes.Length; i++)
            {
                if (!UnlockedMain(i)) continue;
                total++;
                if (Awaited(i)) n++;
            }
            if (n == 0 || total == 0) return -1;

            // Sorulma olasiligi EKSIK ORANIYLA buyuyor, sayisiyla degil.
            //
            // Once mutlak sayiydi ve dortte doyuyordu: "dort yemek eksik"
            // ile "on iki yemek eksik" ayni cezayi aliyordu. Ekipman
            // eksigi birkac yemekle sinirli oldugu surece sorun degildi,
            // ama menuden cikarilanlar da sayilmaya baslayinca ceza
            // HERKESTE tavana vurdu: menusunu makul olcude daraltan
            // oyuncu ile tek yemek tutan oyuncu ayni cezayi yiyordu.
            // Olculdu - tek yemek stratejisi 27.361'den 116'ya dustu ve
            // dukkan dokuzuncu gunde bosaldi. Dar menu "bedava"dan
            // "olumcul"e gecti; ikisi de yanlis.
            //
            // Oran adil: acik ana yemeklerin yarisi menude degilse ceza
            // yarim, hicbiri yoksa tam. Menu genisligi boylece SUREKLI
            // bir eksen oluyor, esikli bir tuzak degil.
            int chance = (int)Fx.MulDiv(_economy.AskChanceBp * 4, n, total);
            if (chance > Fx.One) chance = Fx.One;
            if (!_rngOrder.Chance(chance)) return -1;

            int pick = _rngOrder.NextInt(n);
            for (int i = 0; i < _content.Dishes.Length; i++)
            {
                if (!Awaited(i)) continue;
                if (pick == 0) { Emit(SimEventKind.DishRequested, i); return i; }
                pick--;
            }
            return -1;
        }

        /// <summary>
        /// Gunu ve itibari gelmis ama BUGUN YAPILAMAYAN ana yemek.
        /// Musterinin sorup bulamadigi sey.
        ///
        /// Iki sebep var ve MUSTERI ACISINDAN IKISI AYNI:
        ///   1. ekipman alinmamis  - oyuncunun kapatabilecegi eksik
        ///   2. menude degil       - oyuncunun BUGUN verdigi karar
        ///
        /// Ikincisi uzun sure YOKTU ve bu, oyunun en derin denge
        /// hatasiydi: menuden cikarilan yemek hicbir zaman "sorulmus"
        /// olmuyordu, yani menuyu daraltmanin talep tarafinda SIFIR
        /// bedeli vardi. Olculdu - menude tek ana yemek tutan oyuncu
        /// makul oyuncuyu fast food'da %12, Turk mutfaginda %38
        /// geciyordu. Dar menu kesin baskin stratejiydi.
        ///
        /// Sonucu buydu: soguk hava deposunun ikinci odulu (menu
        /// genisligi tasiyabilmek) degersizdi, yani merdivenin ust
        /// kademeleri satin alinmiyordu; ve otuz iki yemeklik icerik
        /// envanterinin var olma sebebi ortadan kalkiyordu. docs/32 200
        /// "soguk hava menu genisligi satin aldiriyor" diyor - simdi
        /// gercekten oyle.
        ///
        /// Takvimin daha getirmedigi yemek sorulmuyor; o haksizlik olurdu.
        /// </summary>
        /// <summary>
        /// Takvimi ve itibari gelmis ANA yemek. Awaited'in paydasi:
        /// "kac yemek olabilirdi" sorusunun cevabi.
        /// </summary>
        private bool UnlockedMain(int dish)
        {
            DishDef d = _content.Dishes[dish];
            if (d.UnlockDay > _day) return false;
            if (_reputationCenti < d.UnlockReputationCenti) return false;
            return _content.IsInRole(d.Group, _content.MainGroups);
        }

        private bool Awaited(int dish)
        {
            DishDef d = _content.Dishes[dish];
            if (d.UnlockDay > _day) return false;
            if (_reputationCenti < d.UnlockReputationCenti) return false;
            if (!_content.IsInRole(d.Group, _content.MainGroups)) return false;

            // Ekipman eksik: oyuncunun kapatabilecegi eksik.
            if (d.RequiresStationTier > 0
                && _stationTier[d.StationIndex] < d.RequiresStationTier)
                return true;

            // Ekipman var ama menude degil: bugunku karar.
            return !_dishOnMenu[dish];
        }

        /// <summary>
        /// Gruptan, sabire sigan yemekler arasindan esit olasilikla secer.
        /// Hicbiri sigmiyorsa: zorunlu grupta grubun en hizlisi, degilse -1.
        /// </summary>
        /// <summary>
        /// Tatli gibi EK kalemlerin olasiligi arketipe gore degisiyor.
        /// Bahsis egilimi, harcamaya yatkinligin vekili: cok bahsis birakan
        /// tatli da alir, hic birakmayan almaz.
        ///
        /// docs/13 arketip basina bir orderPreference tasarlamisti. Elle
        /// yirmi dort agirlik tablosu yazmak yerine, ZATEN YUKLU olan
        /// karakter alanlarindan turetiliyor; boylece uydurma sayi yok ve
        /// TipChanceBp ile PriceSensitivityBp cift is goruyor.
        /// </summary>
        private static int ExtrasChanceBp(ArchetypeDef a, int baseBp)
        {
            // Bahsis 0 -> yarisi, 3200 -> iki kati.
            int scale = Fx.One / 2 + a.TipChanceBp * 3;
            if (scale > 2 * Fx.One) scale = 2 * Fx.One;
            return (int)Fx.MulDiv(baseBp, scale, Fx.One);
        }

        /// <summary>
        /// Rolden yemek secer. Secim ESIT OLASILIKLI DEGIL: arketipin fiyat
        /// duyarliligi ucuz ya da pahali tarafa yaslaniyor.
        ///
        /// Duyarlilik 10000 notr. Pazarlikci 25000 ile en ucuza, denetim
        /// gorevlisi 5000 ile en pahaliya yaslaniyor. Bu alan zaten
        /// yukluydu ve yalnizca memnuniyet cezasinda kullaniliyordu; yemek
        /// SECIMINDE hic rol oynamiyordu, yani butun musteriler ayni
        /// dagilimla siparis veriyordu.
        /// </summary>
        private int PickFromRole(string[] role, long limit, bool required, int servings,
                                 ArchetypeDef arch)
        {
            int fastest = -1;
            int fastestMs = int.MaxValue;
            long lo = long.MaxValue, hi = long.MinValue;
            int n = 0;

            for (int i = 0; i < _content.Dishes.Length; i++)
            {
                DishDef d = _content.Dishes[i];
                if (!_dishOnMenu[i] || !Unlocked(i)) continue;
                if (!_content.IsInRole(d.Group, role)) continue;
                if (!CanMake(i, servings)) continue;      // stokta yok, siparis edilemez

                if (d.PrepMs < fastestMs) { fastestMs = d.PrepMs; fastest = i; }
                if (d.PrepMs > limit) continue;
                n++;
                if (_dishPrice[i] < lo) lo = _dishPrice[i];
                if (_dishPrice[i] > hi) hi = _dishPrice[i];
            }

            if (n == 0) return required ? fastest : -1;

            // Agirliklar. Tek aday varsa ya da hepsi ayni fiyatsa esit.
            int lean = arch != null ? arch.PriceSensitivityBp - Fx.One : 0;
            long total = 0;
            for (int i = 0; i < _content.Dishes.Length; i++)
            {
                int w = RoleWeight(i, role, limit, servings, lo, hi, lean);
                if (w > 0) total += w;
            }
            if (total <= 0) return fastest;

            long pick = _rngOrder.NextInt((int)(total > int.MaxValue ? int.MaxValue : total));
            for (int i = 0; i < _content.Dishes.Length; i++)
            {
                int w = RoleWeight(i, role, limit, servings, lo, hi, lean);
                if (w <= 0) continue;
                if (pick < w) return i;
                pick -= w;
            }
            return fastest;
        }

        /// <summary>Adayin agirligi; aday degilse sifir.</summary>
        private int RoleWeight(int dish, string[] role, long limit, int servings,
                               long lo, long hi, int lean)
        {
            DishDef d = _content.Dishes[dish];
            if (!_dishOnMenu[dish] || !Unlocked(dish)) return 0;
            if (!_content.IsInRole(d.Group, role)) return 0;
            if (!CanMake(dish, servings)) return 0;
            if (d.PrepMs > limit) return 0;
            if (hi <= lo || lean == 0) return Fx.One;

            // 0 = en ucuz, 10000 = en pahali
            int rel = (int)Fx.MulDiv(_dishPrice[dish] - lo, Fx.One, hi - lo);
            int w = Fx.One + (int)Fx.MulDiv(Fx.One - 2 * rel, lean, Fx.One);
            return w < 1000 ? 1000 : w;      // hicbir yemek tamamen dislanmasin
        }

        /// <summary>Bir kisilik siparisin toplam pisirme suresi.</summary>
        private int OrderPrepMs(int party)
        {
            int ms = 0;
            if (_pDishMain[party] >= 0) ms += _content.Dishes[_pDishMain[party]].PrepMs;
            if (_pDishSide[party] >= 0) ms += _content.Dishes[_pDishSide[party]].PrepMs;
            if (_pDishDrink[party] >= 0) ms += _content.Dishes[_pDishDrink[party]].PrepMs;
            if (_pDishDessert[party] >= 0) ms += _content.Dishes[_pDishDessert[party]].PrepMs;
            return ms;
        }

        /// <summary>Bir kisilik siparisin tutari, oyuncunun belirledigi fiyatlarla.</summary>
        private long OrderPrice(int party)
        {
            long p = 0;
            if (_pCombo[party])
            {
                // Uc kalem tek fiyat; tatli varsa ustune tam fiyattan biniyor.
                p = ComboPrice(party);
                if (_pDishDessert[party] >= 0) p += _dishPrice[_pDishDessert[party]];
                return p;
            }
            if (_pDishMain[party] >= 0) p += _dishPrice[_pDishMain[party]];
            if (_pDishSide[party] >= 0) p += _dishPrice[_pDishSide[party]];
            if (_pDishDrink[party] >= 0) p += _dishPrice[_pDishDrink[party]];
            if (_pDishDessert[party] >= 0) p += _dishPrice[_pDishDessert[party]];
            return p;
        }

        /// <summary>
        /// Kombonun kisi basi fiyati: SECILEN ana yemek + kombonun yani ve
        /// icecegi, hepsi priceBp ile indirimli.
        ///
        /// Ana yemek artik sabit degil (bkz. PickOrder): oyuncu hangi ana
        /// yemegi menude tutuyorsa kombo onun uzerine kuruluyor. Fiyat da
        /// o yuzden secilen yemekten hesaplaniyor - sabit bir yemegin
        /// fiyatini kullanmak, pahali bir ana yemegi ucuza satmak olurdu.
        /// </summary>
        public long ComboPrice(int party)
        {
            int[] d = _content.Signature.ComboDishes;
            if (d == null) return 0;

            int main = party >= 0 && party < MaxParties && _pDishMain[party] >= 0
                ? _pDishMain[party] : d[0];

            long sum = _dishPrice[main] + _dishPrice[d[1]] + _dishPrice[d[2]];
            return Fx.MulDiv(sum, _content.Signature.ComboPriceBp, Fx.One);
        }

        /// <summary>Arayuz icin: kombonun ornek fiyati (kombonun kendi anasiyla).</summary>
        public long ComboPrice() { return ComboPrice(-1); }

        /// <summary>Bir kisilik siparisin malzeme maliyeti.</summary>
        private long OrderCost(int party)
        {
            long c = 0;
            if (_pDishMain[party] >= 0) c += _content.Dishes[_pDishMain[party]].IngredientCost;
            if (_pDishSide[party] >= 0) c += _content.Dishes[_pDishSide[party]].IngredientCost;
            if (_pDishDrink[party] >= 0) c += _content.Dishes[_pDishDrink[party]].IngredientCost;
            if (_pDishDessert[party] >= 0)
                c += _content.Dishes[_pDishDessert[party]].IngredientCost;
            return c;
        }

        // ---- 2. sabir ------------------------------------------------------
        private void AdvancePatience()
        {
            for (int i = 0; i < MaxParties; i++)
            {
                if (!_pActive[i]) continue;

                int rateBp = DrainRateBp(i);
                if (rateBp <= 0) continue;

                int eksilen = (int)Fx.MulDiv(TimingConfig.TickMs, rateBp, Fx.One);
                _pPatienceLeftMs[i] -= eksilen;

                // BEKLEME DE AGIRLIKLI SAYILIYOR.
                //
                // Once her tik TAM sayiliyordu ve bu, sabir donmus
                // oldugu surece zararsizdi. Sabir yemek piserken de
                // isleyince ortaya cikti: bir dakikalik pisme, memnuniyet
                // cezasini (waited/sabir x 6000) doyuruyor ve butun
                // musteriler sifir memnuniyetle cikiyordu - referans
                // botlarin hepsi iflas etti.
                //
                // Dogrusu ayni olcu: musteriyi ne kadar SINIRLENDIREN bir
                // bekleme ise o kadar sayiliyor. Masa beklemek tam,
                // yemek beklemek az - ikisi de ayni birimde.
                _pWaitedMs[i] += eksilen;

                if (!_pWarned[i] && _pPatienceTotalMs[i] > 0)
                {
                    int leftBp = (int)Fx.MulDiv(_pPatienceLeftMs[i], Fx.One, _pPatienceTotalMs[i]);
                    if (leftBp <= _timing.PatienceWarnBp)
                    {
                        _pWarned[i] = true;
                        Emit(SimEventKind.PatienceWarning, i, leftBp);
                    }
                }

                if (_pPatienceLeftMs[i] <= 0) LeaveAngry(i);
            }
        }

        /// <summary>
        /// Sabir tuketme hizi. Garson masadayken (_pInTask) hic tukenmez:
        /// ilgilenilen musteri beklemis sayilmaz.
        /// </summary>
        private int DrainRateBp(int i)
        {
            if (_pInTask[i]) return 0;
            switch (_pStage[i])
            {
                case CustomerStage.WaitingForTable: return _timing.DrainWaitingTableBp;
                case CustomerStage.WaitingToOrder: return _timing.DrainWaitingOrderBp;
                case CustomerStage.WaitingForFood: return _timing.DrainWaitingFoodBp;
                case CustomerStage.WaitingToPay: return _timing.DrainWaitingPayBp;
                default: return 0;
            }
        }

        private void LeaveAngry(int i)
        {
            // Sabri biten musteri cikar ve itibari sert dusurur.
            int stage = (int)_pStage[i];
            _pSatisfactionCenti[i] = 0;
            AccumulateReputation(i, 0);
            _angryParties++;
            if (_pTable[i] >= 0) _angrySeated++;

            FreeTableOf(i, dirty: _pTable[i] >= 0);
            CancelTasksFor(i);
            _pStage[i] = CustomerStage.LeftAngry;
            _pActive[i] = false;
            _partyCount--;

            Emit(SimEventKind.CustomerLeftAngry, i, stage, _pWaitedMs[i]);
        }

        private void FreeTableOf(int party, bool dirty)
        {
            int t = _pTable[party];
            if (t < 0) return;
            _tableParty[t] = -1;
            _tableDirty[t] = dirty;

            // TABAKLAR MASADA KALIYOR. Garson masayi toplayana kadar
            // "kullanimda" sayiliyorlar - lavaboya ancak toplaninca
            // gidiyorlar. Yemeden kalkan grubun elinde tabak yoktur ve
            // burada sifir eklenir.
            _tablePlates[t] += _pPlates[party];
            _pPlates[party] = 0;

            _pTable[party] = -1;
        }

        private void CancelTasksFor(int party)
        {
            for (int s = 0; s < MaxServers; s++)
            {
                if (_salonTaskKind[s] != TaskKind.None && _salonTaskKind[s] != TaskKind.Clear
                    && _salonTaskTarget[s] == party)
                    _salonTaskKind[s] = TaskKind.None;
                if (_kitchenTaskKind[s] != TaskKind.None
                    && _kitchenTaskTarget[s] / MaxJobsPerParty == party)
                    _kitchenTaskKind[s] = TaskKind.None;
            }
            ReleaseJobs(party);
            _pInTask[party] = false;
            _pKitchenTask[party] = false;
        }

        /// <summary>
        /// Grubun istasyon islerini iptal eder ve YUVALARI BOSALTIR.
        /// Bu olmadan kizip giden her grup bir yuvayi omur boyu kilitler
        /// ve mutfak gun ilerledikce sessizce duruyordu.
        /// </summary>
        private void ReleaseJobs(int party)
        {
            int b = party * MaxJobsPerParty;
            for (int k = 0; k < MaxJobsPerParty; k++)
            {
                if (_jobState[b + k] == 1) _stationBusy[_jobStation[b + k]] -= _jobSlots[b + k];
                _jobStation[b + k] = -1;
                _jobMs[b + k] = 0;
                _jobPlates[b + k] = 0;
                _jobSlots[b + k] = 0;
                _jobState[b + k] = 0;
            }
            _pJobsLeft[party] = 0;
        }

        // ---- 3. masaya oturtma ---------------------------------------------
        private void SeatWaitingParties()
        {
            for (int t = 0; t < _tableCount; t++)
            {
                if (_tableParty[t] >= 0 || _tableDirty[t]) continue;

                int best = MostUrgent(CustomerStage.WaitingForTable);
                if (best < 0) return;

                // TEMIZ TABAK YOKSA MASAYA OTURTULMUYOR.
                //
                // Bu satir bir OLUM SARMALINI onluyor ve sarmal otomatik
                // turda goruldu: on iki tabakli dort masali bir dukkanda
                // dort kisilik iki grup stogu tuketiyordu, ucuncu grup
                // masaya oturuyor, siparis veriyor, mutfak tabak
                // bulamiyor, sabri bitiyor, kizgin cikiyor - ve boylece
                // gun kimse servis edilmeden kapaniyordu.
                //
                // Dogru davranis lokantanin kendisinde de bu: tabak
                // yoksa masaya OTURTMAZSIN, oturtup ac birakmazsin.
                // Bekleyen grup kapida bekliyor, temiz tabak cikinca
                // giriyor - basinc goruunur, dukkan kilitlenmiyor.
                //
                // Rezerve degil ESIK: tabaklar tabaklama aninda
                // dusuluyor. Burada tutmak, siparis vermeyen bir grubun
                // tabagi elinde tutmasi olurdu.
                if (_platesClean < _pSize[best]) return;

                _tableParty[t] = best;
                _pTable[best] = t;
                _pStage[best] = CustomerStage.WaitingToOrder;
                Emit(SimEventKind.CustomerSeated, best, t);
            }
        }

        /// <summary>
        /// Verilen asamadaki, goreve baglanmamis, sabri EN AZ kalan grup.
        /// Esitlikte kucuk indeks kazanir: kural deterministik.
        /// "Cikmaya en yakin olana once bak" kurali, dogru onceligi
        /// ayrica kodlamaya gerek birakmiyor.
        /// </summary>
        private int MostUrgent(CustomerStage stage)
        {
            int best = -1;
            int bestPatience = int.MaxValue;
            for (int i = 0; i < MaxParties; i++)
            {
                if (!_pActive[i] || _pStage[i] != stage) continue;
                if (_pInTask[i] || _pKitchenTask[i]) continue;
                if (_pPatienceLeftMs[i] < bestPatience)
                {
                    bestPatience = _pPatienceLeftMs[i];
                    best = i;
                }
            }
            return best;
        }

        // ---- 4. is dagitimi ------------------------------------------------
        /// <summary>
        /// Pisirilmeyi bekleyen grup. _pEatLeftMs == 0 "pisiyor", -1 "pisti".
        /// Bu ayrim olmadan mutfak, servis bekleyen hazir yemegi yeniden
        /// pisirmeye baslıyordu.
        /// </summary>
        /// <summary>
        /// Siparisi ISTASYON ISLERINE bolyor. Ayni istasyona giden kalemler
        /// tek iste birlesiyor. Is, TEK TABAGIN suresini ve TABAK SAYISINI
        /// ayri tutuyor; kac tabagin es zamanli pisecegi is baslarken bos
        /// yuva sayisina gore belli oluyor (docs/27 3.3).
        ///
        /// Ilk yazimda bir grubun butun tabaklari TEK yuvada sirayla
        /// pisiyordu. O modelde ekipman almak hicbir sey degistirmiyordu,
        /// cunku yuva sayisi sureyi etkilemiyordu.
        /// </summary>
        private void BuildJobs(int party)
        {
            int b = party * MaxJobsPerParty;
            for (int k = 0; k < MaxJobsPerParty; k++)
            {
                _jobStation[b + k] = -1;
                _jobMs[b + k] = 0;
                _jobPlates[b + k] = 0;
                _jobSlots[b + k] = 0;
                _jobState[b + k] = 0;
            }
            _pJobsLeft[party] = 0;

            AddJob(party, _pDishMain[party]);
            AddJob(party, _pDishSide[party]);
            AddJob(party, _pDishDrink[party]);
            AddJob(party, _pDishDessert[party]);
        }

        private void AddJob(int party, int dish)
        {
            if (dish < 0) return;
            int station = _content.Dishes[dish].StationIndex;
            int b = party * MaxJobsPerParty;

            for (int k = 0; k < MaxJobsPerParty; k++)
            {
                // Ayni istasyona giden ikinci kalem: tek tabak suresi
                // toplaniyor, tabak sayisi ayni kaliyor.
                if (_jobStation[b + k] == station)
                {
                    _jobMs[b + k] += _content.Dishes[dish].PrepMs;
                    return;
                }
                if (_jobStation[b + k] < 0)
                {
                    _jobStation[b + k] = station;
                    _jobMs[b + k] = _content.Dishes[dish].PrepMs;
                    _jobPlates[b + k] = _pSize[party];
                    _jobState[b + k] = 0;
                    _pJobsLeft[party]++;
                    return;
                }
            }
        }

        /// <summary>Ekipman kademesine gore istasyonun yuva sayisi.</summary>
        private int StationSlots(int station)
        {
            return _content.Stations[station].Tiers[_stationTier[station]].Slots;
        }

        /// <summary>Ekipman kademesine gore istasyonun attendBp degeri.</summary>
        private int StationAttendBp(int station)
        {
            return _content.Stations[station].Tiers[_stationTier[station]].AttendBp;
        }

        /// <summary>
        /// Baslatilabilecek islerin en aceleci olani. Sabri en az kalan grup
        /// once; ayni grubun birden fazla isi olabilir, o yuzden grup
        /// gorevdeyken de bakiyor.
        /// </summary>
        private int MostUrgentJob()
        {
            int best = -1;
            int bestPatience = int.MaxValue;
            for (int i = 0; i < MaxParties; i++)
            {
                if (!_pActive[i]) continue;
                if (_pStage[i] != CustomerStage.WaitingForFood) continue;
                if (_pEatLeftMs[i] != 0) continue;      // 0 = henuz pismedi
                if (_pPatienceLeftMs[i] >= bestPatience) continue;

                int b = i * MaxJobsPerParty;
                for (int k = 0; k < MaxJobsPerParty; k++)
                {
                    if (_jobStation[b + k] < 0 || _jobState[b + k] != 0) continue;
                    if (_stationBusy[_jobStation[b + k]] >= StationSlots(_jobStation[b + k]))
                        continue;
                    bestPatience = _pPatienceLeftMs[i];
                    best = b + k;
                    break;
                }
            }
            return best;
        }

        private void DispatchKitchen()
        {
            for (int s = 0; s < _cooks && s < MaxServers; s++)
            {
                if (_kitchenTaskKind[s] != TaskKind.None) continue;

                int job = MostUrgentJob();
                if (job < 0) return;

                int station = _jobStation[job];
                int plates = _jobPlates[job];
                int attendBp = StationAttendBp(station);
                int free = StationSlots(station) - _stationBusy[station];

                // Kac tabak es zamanli pisiyor. Uc sinir birden:
                //   - bos yuva sayisi
                //   - grubun tabak sayisi
                //   - ASCININ ayni anda bakabilecegi tabak sayisi
                //
                // Ucuncusu sart. Onsuz dort kisilik bir grup dort yuvayi
                // birden tutuyordu ama asci onlara sirayla bakiyordu, yani
                // yuvalar bos bos dolu goruniyordu. Olcum bunu yakaladi:
                // ekipman alan mutfak memnuniyeti 87'den 81'e DUSURUYORDU.
                //
                // docs/27 3.3 ayni sayiyi soyluyor: kademe 4 zirvesinde
                // 8,66 es zamanli tabak, 4 asci, yani asci basina 2,2.
                int perCook = Fx.CeilDiv(Fx.One, attendBp);
                int take = plates;
                if (free < take) take = free;
                if (perCook < take) take = perCook;
                if (take < 1) take = 1;
                int rounds = Fx.CeilDiv(plates, take);

                // Ascinin el emegi tabak basina; paralellik onu azaltmiyor.
                // Deneyim burada isliyor: DENEYIMLI ASCI DAHA CABUK SERBEST
                // KALIYOR, yemek daha cabuk pismiyor. wallMs asagida ayri
                // hesaplaniyor ve ona dokunulmuyor.
                int attendMs = (int)Fx.MulDiv((long)_jobMs[job] * plates,
                                              attendBp, Fx.One);
                attendMs = XpAdjusted(0, s, attendMs);

                // Kombonun bedeli MUTFAKTA odeniyor: ayni tabak asciyi daha
                // uzun bagliyor. Indirim salonda, yuk mutfakta - imza
                // mekaniginin takasi bu (docs/07).
                if (_pCombo[job / MaxJobsPerParty] && _content.Signature.ComboKitchenLoadBp > 0)
                    attendMs = (int)Fx.MulDiv(attendMs,
                                              _content.Signature.ComboKitchenLoadBp, Fx.One);

                if (attendMs < TimingConfig.TickMs) attendMs = TimingConfig.TickMs;

                int wallMs = _jobMs[job] * rounds;
                if (wallMs < attendMs) wallMs = attendMs;

                _jobState[job] = 1;
                _jobSlots[job] = take;
                _jobMs[job] = wallMs;
                _stationBusy[station] += take;

                _kitchenTaskKind[s] = TaskKind.Cook;
                _kitchenTaskTarget[s] = job;
                _pCook[job / MaxJobsPerParty] = s;
                _kitchenTaskLeftMs[s] = attendMs;
                _pKitchenTask[job / MaxJobsPerParty] = true;
            }
        }

        private void DispatchSalon()
        {
            ClampDishwashers();

            int servers = _salon + 1;   // patron da salonda calisiyor
            if (servers > MaxServers) servers = MaxServers;

            // LAVABOYA ADANMIS OLANLAR: salon dizisinin SONUNDAN sayiliyor.
            // Sifirinci patron ve patron lavaboya girmiyor.
            int sinkFrom = servers - _dishwashers;
            if (sinkFrom < 1) sinkFrom = 1;

            for (int s = 0; s < servers; s++)
            {
                if (_salonTaskKind[s] != TaskKind.None) continue;

                if (s >= sinkFrom)
                {
                    // ADANMIS BULASIKCI: yalnizca yikiyor, masaya gitmiyor.
                    // Kullanicinin cumlesi: "bulasikci alinca herkes kendi
                    // isini yapar" - karsiligi tam olarak bu satir.
                    //
                    // BU SATIR IKI KEZ ZAYIFLATILMAYA CALISILDI, OLCUM
                    // IKISINI DE REDDETTI (32 tohum, docs/53):
                    //
                    //   yigin bir esigi gecmeden yikamasin  229 -> 293
                    //   yikayacak sey yokken salona donsun  197 -> 216
                    //
                    // Ikisi de makul geliyordu ve ikisi de ayni seyi
                    // bozuyor: uzmanin butun degeri ARALIKSIZ ve HEMEN
                    // yikamasinda. Salona donen bulasikci, tabak
                    // kirlendiginde bir musteri isine bagli kaliyor ve
                    // lavaboya GEC donuyor.
                    if (_platesDirty > 0)
                    {
                        _salonTaskKind[s] = TaskKind.Wash;
                        _salonTaskTarget[s] = -1;
                        // UZMANIN SURESI: adanmis bulasikci daha hizli
                        // yikiyor. Fark rol tablosunda zaten yaziyordu
                        // (bulasikci 48 / garson 26 gunluk kapasite) ve
                        // simulasyon onu hic kullanmiyordu.
                        _salonTaskLeftMs[s] =
                            XpAdjusted(1, s - 1, _timing.DishwasherWashMs);
                    }
                    continue;
                }

                // ============================================================
                // KRIZ: MUTFAK DURDU. Bu dal MUSTERI ISINDEN ONCE geliyor.
                //
                // Neden istisna mesru: temiz tabak bitince tabak dolum
                // dongusu KOMPLE duruyor (bkz. `_plateStalled`), yani
                // pismis yemek tezgahta bekliyor. O anda bir garsonun
                // yeni siparis almasi degersiz is - servis edilecek bir
                // sey zaten cikmiyor. Lavaboya gitmek dukkani ACIYOR.
                //
                // NEDEN ONCEKI DENEME TUTMADI (docs/49 §6, 351 -> 351):
                // istisna musteri isinden SONRA yazilmisti ve "bos kisi"
                // ariyordu; zirvede salon zaten dolu oldugu icin hic
                // ateslenmedi. Bos kisi aramak yanlis soruydu - dogru
                // soru "su an yapilan is degerli mi".
                //
                // NEDEN BULASIKCIYI OLDURMUYOR: bu dal yalnizca mutfak
                // GERCEKTEN durduysa aciliyor. Adanmis bulasikci varken
                // kriz zaten olusmuyor, yani kullanicinin kurali
                // ("bulasikci alinca herkes kendi isini yapar") normal
                // gunde birebir duruyor - `WashNeeded` hala bulasikci
                // varsa salona rutin yikama vermiyor.
                //
                // Arastirma (docs/53): sevk edilmis hicbir oyunda uzman
                // almak genel havuzu sessizce kapatmiyor. RimWorld'un
                // YANGIN davranisi tam bu kalip - nadir, agir, kapsamli
                // bir kosul normal onceligi geciyor.
                if (!(s == 0 && _salon > 0) && _plateStalled && _platesDirty > 0)
                {
                    _salonTaskKind[s] = TaskKind.Wash;
                    _salonTaskTarget[s] = -1;
                    _salonTaskLeftMs[s] =
                        OwnerAdjusted(s, XpAdjusted(1, s - 1, _timing.WashMs));
                    _washing = true;
                    _salonRushWashes++;
                    continue;
                }

                // Once musteriye dokunan isler, sabri en az olandan basla.
                int party = MostUrgentSalon(out TaskKind kind, out int ms);
                if (party >= 0)
                {
                    _salonTaskKind[s] = kind;
                    _salonTaskTarget[s] = party;
                    _pServer[party] = s;
                    _salonTaskLeftMs[s] = OwnerAdjusted(s, XpAdjusted(1, s - 1, ms));
                    _pInTask[party] = true;
                    continue;
                }

                // BULASIK COK BIRIKTIYSE SALON LAVABOYA GECIYOR.
                //
                // Kullanicinin cumlesi: "bulasiklar cok biriktigi zaman
                // garson bulasiklari yikamaya gecsin". Musteriye dokunan
                // isten SONRA, masa toplamaktan ONCE: biriken bulasik
                // kirli bir masadan daha aciledir, cunku temiz tabak
                // bitince MUTFAK duruyor.
                //
                // Iki esik (histerezis) sart: tek esikte garson bir
                // tabak yikayip servise donuyor, bir sonraki karede geri
                // geliyor - "yikiyor" degil "gidip geliyor" diye
                // okunuyordu.
                // LAVABO PATRONUN ISI DEGIL - personel varsa.
                //
                // Sifirinci sunucu PATRON ve patron gorunumde CIZILMIYOR
                // (oyuncunun kendisi). Bulasigi patron yikayinca oyuncu
                // hicbir sey gormuyor: olculdu, bir kosuda cekirdek on
                // iki tabak yikadi ve ekranda sifir kare yikama goruldu.
                //
                // Kurgusal olarak da dogrusu bu: patron salonu toplar,
                // musteriyle ilgilenir, bosluk doldurur - lavaboya
                // baglanan kisi personeldir. Tek basinaysa (salon
                // kadrosu yok) yine yikiyor, yoksa tabaklar hic
                // temizlenmez ve dukkan kilitlenirdi.
                bool patron = s == 0 && _salon > 0;

                if (!patron && WashNeeded())
                {
                    _salonTaskKind[s] = TaskKind.Wash;
                    _salonTaskTarget[s] = -1;
                    _salonTaskLeftMs[s] = OwnerAdjusted(s, XpAdjusted(1, s - 1, _timing.WashMs));
                    _washing = true;

                    // BU SATIR OLCUM ICIN.
                    //
                    // Salonun bulasiga IKI yolu var: burasi (acil - isini
                    // BIRAKIP lavaboya kosuyor) ve asagidaki bos vakit
                    // dali. Bulasikcinin tum degeri birincisini
                    // engellemesinde; ikincisi zaten zararsiz. Iki yol
                    // tek sayaca katlanirsa bulasikcinin farki
                    // olculemez - "bulasikci beklemeyi dusurdu mu"
                    // testi tam olarak bu yuzden sifiri sifirla
                    // karsilastiriyordu.
                    _salonRushWashes++;
                    continue;
                }

                // Kimse beklemiyorsa kirli masa topla.
                int table = FirstDirtyTable();
                if (table < 0)
                {
                    // Masa da yoksa bos vakitte bulasiga bakiliyor:
                    // gercek bir garson da oyle yapar ve bu, zirveye
                    // temiz tabakla girmeyi sagliyor.
                    if (!patron && _platesDirty > 0)
                    {
                        _salonTaskKind[s] = TaskKind.Wash;
                        _salonTaskTarget[s] = -1;
                        _salonTaskLeftMs[s] = OwnerAdjusted(s, XpAdjusted(1, s - 1, _timing.WashMs));
                        _washing = true;
                        continue;
                    }
                    return;
                }
                // BAYRAK BURADA DUSMUYOR, GOREV BITINCE DUSUYOR.
                //
                // Once temizlik BASLARKEN dusuyordu ve masa o saniyede
                // yeni musteriye aciliyordu: SeatWaitingParties yalnizca
                // _tableDirty'ye bakiyor. Sonuc, ClearMs'in kapasiteyi
                // hic kisitlamamasiydi - garsonu mesgul ediyor ama masayi
                // tutmuyordu - ve salonda yeni bir grubun hala toplanan
                // masaya oturdugu goruluyordu.
                //
                // Ayni masanin iki garson tarafindan secilmesini
                // FirstDirtyTable engelliyor.
                _salonTaskKind[s] = TaskKind.Clear;
                _salonTaskTarget[s] = table;
                // Temizlik huyu YALNIZCA masa toplamaya isliyor: "hizli
                // ama dagilnik" servis ederken hizli, toplarken yavas.
                // Etkiyi butun gorevlere yaymak, huyu ikinci bir hiz
                // carpanina cevirir ve dagilnikligi anlamsizlastirirdi.
                int clearMs = _timing.ClearMs;
                if (s > 0)
                {
                    int clean = TraitSum(1, s - 1, t => t.CleanlinessBp);
                    if (clean != 0)
                    {
                        int bp = Fx.One + clean;
                        if (bp < 2000) bp = 2000;
                        clearMs = (int)Fx.MulDiv(clearMs, Fx.One, bp);
                    }
                }
                _salonTaskLeftMs[s] = OwnerAdjusted(s, XpAdjusted(1, s - 1, clearMs));
            }
        }

        private int MostUrgentSalon(out TaskKind kind, out int ms)
        {
            int best = -1;
            int bestPatience = int.MaxValue;
            CustomerStage bestStage = CustomerStage.None;

            for (int i = 0; i < MaxParties; i++)
            {
                if (!_pActive[i] || _pInTask[i] || _pKitchenTask[i]) continue;
                CustomerStage st = _pStage[i];
                bool needsSalon = st == CustomerStage.WaitingToOrder
                               || st == CustomerStage.WaitingToPay;
                if (!needsSalon) continue;
                if (_pPatienceLeftMs[i] < bestPatience)
                {
                    bestPatience = _pPatienceLeftMs[i];
                    best = i;
                    bestStage = st;
                }
            }

            // Yemegi hazir olanlar servis bekliyor; onlar ayri isaretli.
            for (int i = 0; i < MaxParties; i++)
            {
                if (!_pActive[i] || _pInTask[i] || _pKitchenTask[i]) continue;
                if (_pStage[i] != CustomerStage.WaitingForFood) continue;
                if (_pEatLeftMs[i] != -1) continue;    // -1 = pisti, servis bekliyor
                if (_pPatienceLeftMs[i] < bestPatience)
                {
                    bestPatience = _pPatienceLeftMs[i];
                    best = i;
                    bestStage = CustomerStage.WaitingForFood;
                }
            }

            if (best < 0) { kind = TaskKind.None; ms = 0; return -1; }

            int size = _pSize[best];
            switch (bestStage)
            {
                case CustomerStage.WaitingToOrder:
                    kind = TaskKind.SeatOrder; ms = _timing.SeatOrderMs * size; break;
                case CustomerStage.WaitingForFood:
                    kind = TaskKind.Serve; ms = _timing.ServeMs * size; break;
                default:
                    kind = TaskKind.Pay; ms = _timing.PayMs * size; break;
            }

            // PATRONUN ILGILENDIGI MASA: is kisaliyor ve isaret TUKENIYOR.
            // Ilgi bir ADIM, surekli bir hal degil - yoksa bir kez
            // ilgilenilen masa gun boyu ayricalikli olurdu.
            if (_pAttended[best] && _economy.AttendWorkCutBp > 0)
            {
                ms = (int)Fx.Bp(ms, Fx.One - _economy.AttendWorkCutBp);
                if (ms < 1) ms = 1;
                _pAttended[best] = false;
            }
            return best;
        }

        /// <summary>
        /// Salonun sifirinci sunucusu PATRON. Kapasite modeli patronu
        /// 1,4 is-gunu sayiyor (docs/14), yani bir personelden hizli.
        /// Ayni katsayiyi burada gorev suresine uyguluyoruz; aksi halde
        /// simulasyon kapasite modelinden dusuk verim uretirdi.
        /// </summary>
        private int OwnerAdjusted(int serverIndex, int ms)
        {
            if (serverIndex != 0) return ms;
            return (int)Fx.MulDiv(ms, Fx.One, _economy.OwnerWorkMicro / 100);
        }

        /// <summary>
        /// Bulasik ACIL mi - yani salon isini birakip lavaboya gecmeli mi.
        ///
        /// Iki sebep yeter:
        ///   - temiz tabak bitti ya da bitmek uzere (mutfak duruyor),
        ///   - kirli yigin esigi asti.
        ///
        /// Histerezis: bir kez baslayinca yigin ALT esige inene kadar
        /// suruyor. Tek esikle garson her karede gidip geliyordu.
        /// </summary>
        private bool WashNeeded()
        {
            if (_platesDirty <= 0) return false;

            // BULASIKCI VARSA SALON KARISMAZ.
            //
            // Kuralin gerekcesi kullanicinin cumlesi: "bulasikci alinca
            // herkes kendi isini yapar".
            //
            // BUNU BIR KEZ GEVSETTIM VE GERI ALDIM. Adanmis bulasikci
            // tabak darbogazini acmiyor, kotulestiriyordu (tabaksiz
            // bekleme 263 -> 351), ve "temiz tabak bitmek uzereyken salon
            // imdada kossun" istisnasi hicbir sey degistirmedi: 351 ->
            // 351. Sebep, istisnanin ateslenecek BOS KISI bulamamasi -
            // yikamaya musteri isinden SONRA bakiliyor ve zirvede salon
            // zaten dolu.
            //
            // Olculmemis bir gerekceyle kullanicinin tasarim kuralini
            // zayiflatmak yanlis olurdu; kural duruyor. Asil sebep ve
            // acik karar docs/49 §5'te.
            if (_dishwashers > 0) return false;

            int toplam = _economy.TierForTables(_tableCount).Plates;
            if (_platesClean * 4 <= toplam) return true;        // temizin dortte biri kaldi
            if (_washing && _platesDirty * 4 > toplam) return true;   // alt esige inmedi
            return _platesDirty * 2 >= toplam;                  // yarisi kirli
        }

        /// <summary>Su an salondan biri lavaboda mi (histerezis icin).</summary>
        private bool _washing;

        /// <summary>Lavaboya adanmis salon calisani sayisi.</summary>
        private int _dishwashers;

        /// <summary>Bugun yikanan tabak sayisi. Rapor ve tani icin.</summary>
        private int _washedToday;

        /// <summary>
        /// Bugun KIRLENEN tabak sayisi - birikmeli.
        ///
        /// Anlik kirli sayisi ("su an lavaboda kac tabak var") dongunun
        /// isleyip islemedigini SOYLEMIYOR: gunun basinda kimse yemegini
        /// bitirmemistir ve sayi sifirdir. Birikmeli sayac, "bugun
        /// dongu dondu mu" sorusunun dogru olcusu.
        /// </summary>
        private int _dirtiedToday;

        /// <summary>
        /// Tabak bittigi bildirimi verildi mi.
        ///
        /// Her tick'te duyurmak bildirim seridini tek bir cumleyle
        /// doldururdu; bayrak tabak cikinca dusuyor, yani her YENI
        /// tikanma bir kez soyleniyor.
        /// </summary>
        private bool _plateWarned;

        /// <summary>
        /// MUTFAK SU AN TABAK YOKLUGUNDAN DURDU MU.
        ///
        /// `_plateWarned` ile ayni omurde ama isi ayri: o BILDIRIM
        /// bayragi, bu KARAR girdisi. Ikisini tek alana bindirmek,
        /// bildirimi susturan bir degisikligin sessizce salonun kriz
        /// davranisini de kapatmasi demekti.
        /// </summary>
        private bool _plateStalled;

        private int FirstDirtyTable()
        {
            for (int t = 0; t < _tableCount; t++)
            {
                if (!_tableDirty[t] || _tableParty[t] >= 0) continue;
                if (BeingCleared(t)) continue;
                return t;
            }
            return -1;
        }

        /// <summary>
        /// Bu masayi toplayan bir garson var mi.
        ///
        /// Kirli bayragi artik gorev BITINCE dusuyor, yani ayni masa iki
        /// garson tarafindan secilebilirdi. Ayri bir "toplaniyor" dizisi
        /// tutmak yerine gorev listesine bakiliyor: yeni bir durum alani
        /// kayda, dogrulamaya ve tekrar oynatmaya da girerdi.
        /// </summary>
        private bool BeingCleared(int table)
        {
            for (int s = 0; s < MaxServers; s++)
                if (_salonTaskKind[s] == TaskKind.Clear && _salonTaskTarget[s] == table)
                    return true;
            return false;
        }

        // ---- 5. gorevleri ilerlet -------------------------------------------
        private void AdvanceTasks()
        {
            // Ascinin bagli kaldigi sure. Bittiginde asci serbest, ama
            // yemek ISTASYONDA pismeye devam ediyor.
            for (int s = 0; s < MaxServers; s++)
            {
                if (_kitchenTaskKind[s] == TaskKind.None) continue;
                _kitchenTaskLeftMs[s] -= TimingConfig.TickMs;
                if (_kitchenTaskLeftMs[s] > 0) continue;
                _kitchenTaskKind[s] = TaskKind.None;
            }

            // Istasyondaki isler. Yuva, is bitince bosaliyor.
            //
            // DIZI DEGIL GRUPLAR taraniyor. _jobStation.Length =
            // MaxParties * MaxJobsPerParty = 1024 ve bu dongu her tick
            // KOSULSUZ doniyordu; oysa aktif is sayisi hicbir zaman
            // 14 masa x 4 = 56'yi gecemez. Grup basina tek bir kontrol,
            // 1024 yinelemeyi 256'ya indiriyor ve gruplarin cogunda
            // dortlu ic dongu hic acilmiyor.
            //
            // Tick saniyede 10 kez (en yuksek hizda 160 kez) calisiyor,
            // yani buradaki her yineleme kare butcesinden yiyor.
            for (int p = 0; p < MaxParties; p++)
            {
                if (_pJobsLeft[p] <= 0) continue;

                int b = p * MaxJobsPerParty;
                for (int k = 0; k < MaxJobsPerParty; k++)
                {
                    int j = b + k;
                    if (_jobState[j] != 1) continue;

                    _jobMs[j] -= TimingConfig.TickMs;
                    if (_jobMs[j] > 0) continue;

                    _jobState[j] = 2;
                    _stationBusy[_jobStation[j]] -= _jobSlots[j];

                    if (!_pActive[p]) continue;
                    if (--_pJobsLeft[p] > 0) continue;

                    _pKitchenTask[p] = false;

                    // PISTI AMA HENUZ TABAKTA DEGIL.
                    //
                    // Eskiden burada dogrudan servis bekliyor isaretleniyordu.
                    // Simdi arada bir adim var: asci temiz tabak almadan
                    // yemegi cikaramiyor (PlateUp). Temiz tabak yoksa
                    // yemek tezgahta bekliyor - tabak darbogazinin
                    // oyuncuya gorunen hali bu.
                    _pCooked[p] = true;
                }
            }

            for (int s = 0; s < MaxServers; s++)
            {
                if (_salonTaskKind[s] == TaskKind.None) continue;
                _salonTaskLeftMs[s] -= TimingConfig.TickMs;
                if (_salonTaskLeftMs[s] > 0) continue;

                TaskKind kind = _salonTaskKind[s];
                int target = _salonTaskTarget[s];
                _salonTaskKind[s] = TaskKind.None;

                if (kind == TaskKind.Clear)
                {
                    // Masa ANCAK SIMDI bosaliyor.
                    if (target >= 0 && target < MaxTables)
                    {
                        _tableDirty[target] = false;

                        // Kirli tabaklar garsonla birlikte lavaboya gidiyor.
                        int tasinan = _tablePlates[target];
                        _tablePlates[target] = 0;
                        _platesInUse -= tasinan;
                        _platesDirty += tasinan;
                        _dirtiedToday += tasinan;
                    }
                    Emit(SimEventKind.TableCleared, target);
                    continue;
                }

                if (kind == TaskKind.Wash)
                {
                    // Bir partide bir tabak. Yikama bitince temiz yigina.
                    if (_platesDirty > 0)
                    {
                        _platesDirty--;
                        _platesClean++;
                    }
                    _washedToday++;
                    if (_platesDirty == 0) _washing = false;
                    continue;
                }

                if (!_pActive[target]) continue;
                _pInTask[target] = false;

                switch (kind)
                {
                    case TaskKind.SeatOrder:
                        // Stok siparis ALINIRKEN dusuluyor. Sonradan tukenirse
                        // musteri masada bekletilmis olurdu; hal asamasinin
                        // baskisi siparis aninda hissedilmeli.
                        Consume(_pDishMain[target], _pSize[target]);
                        Consume(_pDishSide[target], _pSize[target]);
                        Consume(_pDishDrink[target], _pSize[target]);
                        Consume(_pDishDessert[target], _pSize[target]);
                        _pStage[target] = CustomerStage.WaitingForFood;
                        _pEatLeftMs[target] = 0;      // 0 = pisiyor
                        BuildJobs(target);
                        if (_pDishMain[target] >= 0) _orderedRole[0] += _pSize[target];
                        if (_pDishSide[target] >= 0) _orderedRole[1] += _pSize[target];
                        if (_pDishDrink[target] >= 0) _orderedRole[2] += _pSize[target];
                        if (_pDishDessert[target] >= 0) _orderedRole[3] += _pSize[target];
                        Emit(SimEventKind.OrderTaken, target, _pDishMain[target]);
                        break;

                    case TaskKind.Serve:
                        _pStage[target] = CustomerStage.Eating;
                        _pEatLeftMs[target] = _timing.EatMs;
                        Emit(SimEventKind.FoodServed, target, _pTable[target]);
                        break;

                    case TaskKind.Pay:
                        CompletePayment(target);
                        break;
                }
            }
        }

        /// <summary>
        /// PISEN YEMEGI TEMIZ TABAGA KOYAR.
        ///
        /// Mutfak isini bitirdiginde yemek hazir ama servis edilebilir
        /// degil: asci temiz bir tabak almali. Tabak yoksa yemek tezgahta
        /// bekliyor ve sayac isliyor - o sayac, "bulasikci ihmal edildi"
        /// cumlesinin OLCUSU.
        ///
        /// Sabri en az olandan basliyor: tabak kitken kimin yemeginin
        /// cikacagi rastgele olmamali.
        /// </summary>
        private void PlateUp()
        {
            while (true)
            {
                int best = -1;
                int bestPatience = int.MaxValue;
                for (int i = 0; i < MaxParties; i++)
                {
                    if (!_pActive[i] || !_pCooked[i]) continue;
                    if (_pPatienceLeftMs[i] >= bestPatience) continue;
                    bestPatience = _pPatienceLeftMs[i];
                    best = i;
                }
                if (best < 0) return;

                int need = _pSize[best];
                if (_platesClean < need)
                {
                    // TABAK YOK. Sayac isliyor ve dongu burada duruyor -
                    // daha az tabak isteyen kucuk bir grubu araya sokmak,
                    // sabri en az olani beklemeye birakmak olurdu.
                    _plateBlockedTicks++;
                    _plateStalled = true;

                    // OYUNCUYA BIR KEZ SOYLENIYOR.
                    //
                    // Her tick'te duyurmak bildirim seridini tek bir
                    // cumleyle doldururdu; susmak ise servisin sebepsiz
                    // yavasladigi anlamina gelir ve oyuncu bunu mekanik
                    // degil HATA diye okur. Bayrak tabak cikinca dusuyor,
                    // yani her YENI tikanma bir kez soyleniyor.
                    if (!_plateWarned)
                    {
                        _plateWarned = true;
                        Emit(SimEventKind.PlatesOut, _platesDirty, _dishwashers);
                    }
                    return;
                }

                _plateWarned = false;
                _plateStalled = false;
                _platesClean -= need;
                _platesInUse += need;
                _pPlates[best] += need;
                _pCooked[best] = false;
                // SELF SERVISTE SERVIS ADIMI YOK.
                //
                // Tezgahta siparis veren musteri tepsisini KENDI aliyor;
                // masaya kimse getirmiyor. Bu, iki mutfagi ayiran en
                // buyuk yapisal fark (docs/51) ve salon yukunun yarisini
                // bu adim tasiyordu.
                //
                // Muhasebeye dokunulmuyor: memnuniyet, itibar ve masanin
                // kirli birakilmasi hala CompletePayment'ta.
                if (_content.SelfService)
                {
                    _pStage[best] = CustomerStage.Eating;
                    _pEatLeftMs[best] = _timing.EatMs;
                    Emit(SimEventKind.FoodServed, best, _pTable[best]);
                }
                else
                {
                    _pEatLeftMs[best] = -1;           // tabakta, servis bekliyor
                }
                Emit(SimEventKind.FoodReady, best, _pDishMain[best]);
            }
        }

        private void AdvanceEating()
        {
            for (int i = 0; i < MaxParties; i++)
            {
                if (!_pActive[i] || _pStage[i] != CustomerStage.Eating) continue;
                _pEatLeftMs[i] -= TimingConfig.TickMs;
                if (_pEatLeftMs[i] > 0) continue;
                // SELF SERVISTE ODEME BEKLEME YOK.
                //
                // Para tezgahta, siparis aninda odendi. Musteri kalkip
                // gidiyor - masada tepsisi kaliyor ve onu TEMIZLIKCI
                // topluyor (CompletePayment masayi kirli birakiyor).
                //
                // CompletePayment yine cagriliyor: memnuniyet, itibar,
                // mudavim kaydi ve ciro orada. Degisen tek sey,
                // oyuncunun bir garsonu masaya gondermesinin
                // GEREKMEMESI.
                if (_content.SelfService)
                {
                    _pStage[i] = CustomerStage.WaitingToPay;
                    CompletePayment(i);
                    continue;
                }

                _pStage[i] = CustomerStage.WaitingToPay;
                // Odeme beklerken sabir yeniden isliyor ama tazelenmis olarak:
                // yemek yiyen musteri sifirdan sabirli degil, yarisiyla basliyor.
                _pPatienceLeftMs[i] = _pPatienceTotalMs[i] / 2;
                _pWarned[i] = false;
            }
        }

        // ---- odeme ve memnuniyet -------------------------------------------
        private void CompletePayment(int party)
        {
            int size = _pSize[party];
            long bill = OrderPrice(party) * size;
            long cost = OrderCost(party) * size;

            int satisfaction = ComputeSatisfaction(party, _pDishMain[party]);

            // Masaya bakan garsonun huyu. docs/14: musteriyle iyi anlasan
            // +8 puan, suratsiz -6. Patron (sifirinci sunucu) huysuz degil.
            int server = _pServer[party];
            if (server > 0)
                satisfaction += TraitSum(1, server - 1, t => t.SatisfactionCenti);

            // Pisiren ascinin huyu: "yavas ama titiz" yemek kalitesini
            // yukseltiyor (docs/14). Etki memnuniyetin KALAN payina
            // uygulaniyor - zaten tavandaki bir yemegi daha iyi yapamaz.
            int cook = _pCook[party];
            if (cook >= 0)
            {
                int q = TraitSum(0, cook, t => t.QualityBp);
                if (q > 0 && satisfaction < Fx.One)
                    satisfaction += (int)Fx.MulDiv(Fx.One - satisfaction, q, Fx.One);
                else if (q < 0)
                    satisfaction += (int)Fx.MulDiv(satisfaction, q, Fx.One);
            }

            // Sevdigi yemegi bulamayan duzenli musteri, dogru sekilde
            // agirlansa bile eksik ayriliyor.
            if (_pMissedFavourite[party])
            {
                satisfaction -= _economy.RegularMissedFavouriteCenti;
                if (satisfaction < 0) satisfaction = 0;
            }
            _pSatisfactionCenti[party] = satisfaction;

            // Bahsis: memnun musteri, arketipin bahsis egilimine gore
            ArchetypeDef a = _content.Archetypes[_pArchetype[party]];
            if (satisfaction > 8000 && _rngStaffError.Chance(a.TipChanceBp))
                bill += Fx.Bp(bill, 1000);   // %10 bahsis

            // Malzeme SABAH halde pesin odendi (OrderIngredient); burada
            // ikinci kez dusulmez. Hal asamasi yokken burada dusuluyordu ve
            // o gecici satir kaldirilmadigi icin malzeme iki kez odeniyordu.
            // Gunluk izde brut kar 476 sikke iken kasa 116 sikke artiyordu;
            // aradaki 360 tam olarak ikinci odemeydi.
            SignatureDef sig = _content.Signature;

            // Veresiye istedi ve alamadi: mahcup kalkiyor masadan.
            if (_pAsksCredit[party] && !_pCredit[party] && HasCredit)
            {
                satisfaction -= sig.CreditRefusedPenaltyCenti;
                if (satisfaction < 0) satisfaction = 0;
                _pSatisfactionCenti[party] = satisfaction;
            }

            if (_pCredit[party] && HasCredit && _tabCount < MaxTabs)
            {
                // Fis kasaya GIRMIYOR: veresiye defterine yaziliyor.
                // Ciro da bugun sayilmiyor; tahsil edilince sayilacak.
                // docs/07: "nakit akisini bozar ama sadakati yukseltir."
                _tabAmount[_tabCount] = bill;
                _tabDueDay[_tabCount] = _day + sig.CreditDueDays;
                _tabTea[_tabCount] = _pTea[party] ? 1 : 0;
                _tabRegular[_tabCount] = _pRegular[party];
                _tabCount++;
                _creditIssued += bill;      // yil sonu tahsilat orani icin
                satisfaction += sig.CreditLoyaltyBonusCenti;
                if (satisfaction > Fx.One) satisfaction = Fx.One;
                _pSatisfactionCenti[party] = satisfaction;
            }
            else
            {
                _cash += bill;
                _revenue += bill;
                _revenueAll += bill;
            }
            _ingredientCost += cost;
            _servedParties++;
            _servedPeople += size;
            _satisfactionSum += (long)satisfaction * size;

            AccumulateReputation(party, satisfaction);
            RecordRegularVisit(party, satisfaction);

            FreeTableOf(party, dirty: true);
            _pStage[party] = CustomerStage.Done;
            _pActive[party] = false;
            _partyCount--;

            Emit(SimEventKind.CustomerPaid, party, (int)bill, satisfaction);
        }

        /// <summary>
        /// Yemegin kalite etkisi: EN BELIRLEYICI malzeme ne diyorsa o.
        ///
        /// Once ortalama alinmisti ve olcum reddetti: Turk mutfaginda
        /// ucuz malzeme alan oyuncu 35.200 ile iyi oyunun 26.211'ini
        /// GECIYORDU. Sebep ortalamanin kendisi: tencereye atilan ucuz
        /// sogan, ucuz eti gizliyordu. Alti malzemeli sulu yemek cezayi
        /// altiya boluyor, uc malzemeli hamburger uce.
        ///
        /// Musteri boyle dusunmuyor. "Etin ucuz" der; yaninda kac tane
        /// sogan oldugunu saymaz. O yuzden mutlak degeri en buyuk olan
        /// malzeme belirliyor.
        /// </summary>
        private int DishQualityCenti(int dish)
        {
            DishIngredient[] parts = _content.Dishes[dish].Ingredients;
            if (parts == null || parts.Length == 0) return 0;

            int strongest = 0;
            for (int i = 0; i < parts.Length; i++)
            {
                int q = _stockQualityCenti[parts[i].IngredientIndex];
                if (q < 0 ? -q > (strongest < 0 ? -strongest : strongest)
                          : q > (strongest < 0 ? -strongest : strongest))
                    strongest = q;
            }
            return strongest;
        }

        /// <summary>
        /// Fisteki butun kalemlerin piyasadan sapmasi, kalemin fisteki
        /// PAYIYLA agirliklandirilmis, baz puan.
        ///
        /// Neden agirlikli: 46 santilik bir ana yemekte %10 zam ile 16
        /// santilik bir ayranda %10 zam ayni sey degil. Oyuncunun
        /// hissettigi sey yuzdelerin ortalamasi degil, FISIN ne kadar
        /// sismis oldugu.
        ///
        /// Kombo fisi kendi indirimini zaten tasiyor (ComboPrice) ve
        /// oradaki fiyat karari ayri bir mekanik; kombo varken yalnizca
        /// ana yemek olculuyor - yoksa oyuncu imza mekanigini actigi
        /// icin cezalandirilirdi.
        /// </summary>
        private long WeightedPriceDiffBp(int party, int main)
        {
            // KOMBODA DA FISIN TAMAMI OLCULUYOR.
            //
            // Once yalnizca ana yemege bakiliyordu ve gerekce "kombo
            // kendi indirimini tasiyor, oyuncu imza mekanigini actigi
            // icin cezalandirilmasin" idi. Gerekce dogru, uygulama
            // yanlisti: kombo fisi UC kalemin fiyatini topluyor ve
            // kombo her gruba dayatiliyor. Yani ana yemegi piyasada
            // birakip yan ile icecegi istedigin kadar pahalilastirmak
            // MUSTERIYE HIC YANSIMIYORDU.
            //
            // Olculdu: kombo acikken ekstralari 2000 kat pahalilastiran
            // bir bot 24,8 MILYON sikke topladi ve memnuniyet 68,9'dan
            // yalnizca 66,8'e dustu.
            //
            // Dogrusu: kombonun KENDI piyasa karsiligi (uc kalemin
            // piyasa toplami x kombo indirimi) ile oyuncunun kombo
            // fiyati karsilastiriliyor. Indirim cezalandirilmiyor,
            // sismanlik olculuyor.
            if (_pCombo[party])
            {
                int[] cd = _content.Signature.ComboDishes;
                if (cd == null || cd.Length < 3) return DishPriceDiffBp(main);

                long piyasa = _content.Dishes[main].Price
                            + _content.Dishes[cd[1]].Price
                            + _content.Dishes[cd[2]].Price;
                piyasa = Fx.MulDiv(piyasa, _content.Signature.ComboPriceBp, Fx.One);
                if (piyasa <= 0) return 0;

                long sapma = Fx.MulDiv(ComboPrice(party) - piyasa, Fx.One, piyasa);
                long taban = _economy.UnderpriceFloorBp - Fx.One;
                return sapma < taban ? taban : sapma;
            }

            long toplam = 0, agirlik = 0;
            Add(ref toplam, ref agirlik, main);
            Add(ref toplam, ref agirlik, _pDishSide[party]);
            Add(ref toplam, ref agirlik, _pDishDrink[party]);
            Add(ref toplam, ref agirlik, _pDishDessert[party]);

            return agirlik > 0 ? toplam / agirlik : 0;
        }

        private void Add(ref long toplam, ref long agirlik, int dish)
        {
            if (dish < 0) return;
            long market = _content.Dishes[dish].Price;
            if (market <= 0) return;
            // Agirlik PIYASA fiyati, oyuncunun fiyati degil: yoksa
            // pahaliya satmak kalemin agirligini da buyutur ve ceza
            // kendi kendini besler.
            toplam += DishPriceDiffBp(dish) * market;
            agirlik += market;
        }

        /// <summary>Tek yemegin piyasadan sapmasi, tabanli.</summary>
        /// <summary>
        /// Menunun ORTALAMA fiyat sapmasi, baz puan. Talep kanali icin.
        ///
        /// Agirliklar siparis olasiligiyla ayni: ana yemegi herkes
        /// aliyor, yan/icecek/tatli olasilikla. Ayni agirliklari
        /// RecommendedRestock da kullaniyor - iki yerde iki ayri agirlik
        /// olsaydi hal ekrani ile talep birbirini yalanlardi.
        ///
        /// Tek bir yemegi pahalilastirmak talebi az etkiliyor, menunun
        /// tamamini pahalilastirmak cok: olculen sey oyuncunun FIYAT
        /// SIYASETI, tek bir kalem degil.
        /// </summary>
        private long MenuPriceDiffBp()
        {
            int mains = 0, sides = 0, drinks = 0, desserts = 0;
            for (int i = 0; i < _content.Dishes.Length; i++)
            {
                if (!_dishOnMenu[i] || !Unlocked(i)) continue;
                string g = _content.Dishes[i].Group;
                if (_content.IsInRole(g, _content.MainGroups)) mains++;
                else if (_content.IsInRole(g, _content.SideGroups)) sides++;
                else if (_content.IsInRole(g, _content.DrinkGroups)) drinks++;
                else if (_content.IsInRole(g, _content.DessertGroups)) desserts++;
            }
            if (mains == 0) return 0;          // menu bos: sapma da yok
            if (sides == 0) sides = 1;
            if (drinks == 0) drinks = 1;
            if (desserts == 0) desserts = 1;

            long toplam = 0, agirlik = 0;
            for (int i = 0; i < _content.Dishes.Length; i++)
            {
                if (!_dishOnMenu[i] || !Unlocked(i)) continue;
                string g = _content.Dishes[i].Group;

                long w;
                if (_content.IsInRole(g, _content.MainGroups))
                    w = Fx.One / mains;
                else if (_content.IsInRole(g, _content.SideGroups))
                    w = _economy.SideChanceBp / sides;
                else if (_content.IsInRole(g, _content.DrinkGroups))
                    w = _economy.DrinkChanceBp / drinks;
                else if (_content.IsInRole(g, _content.DessertGroups))
                    w = _economy.DessertChanceBp / desserts;
                else continue;

                if (w <= 0) continue;
                toplam += DishPriceDiffBp(i) * w;
                agirlik += w;
            }
            return agirlik > 0 ? toplam / agirlik : 0;
        }

        /// <summary>
        /// Bir gunun beklenen musteri sayisi, FIYAT DAHIL.
        ///
        /// ALTI CAGRI YERI DE BURADAN GECIYOR. Once her biri
        /// DemandModel.CustomersPerDay'i dogrudan cagiriyordu; fiyat
        /// kanali eklenirken birini atlamak, hal ekraninin gercekte
        /// gelmeyecek musteriye gore stok onermesi demekti - yani
        /// oyuncunun parasini cope attiracak sessiz bir tutarsizlik.
        /// </summary>
        private int ExpectedCustomers(int dayFactorBp)
        {
            int people = DemandModel.CustomersPerDay(
                _tableCount, _reputationCenti, _economy.CustomerBasePerTable, dayFactorBp);

            // MUTFAGIN HACMI. Fast food ayni masaya daha cok insan
            // getiriyor - "kalabalik, dusuk fis" vaadinin sayidaki
            // karsiligi. Carpan TEK KAPIDA uygulaniyor, yani kadro
            // onerisi, hal onerisi ve gelis plani ayni sayiyi goruyor.
            int carpan = _content.CustomerMultiplierBp;
            if (carpan > 0 && carpan != Fx.One)
                people = (int)Fx.Bp(people, carpan);

            return DemandModel.ApplyPrice(people, MenuPriceDiffBp(),
                                          _economy.PriceElasticityBp);
        }

        /// <summary>
        /// Bugun GERCEKTEN gelecek musteri sayisi.
        ///
        /// ExpectedCustomers BEKLENTI; bu onun uzerine gunun sapmasini
        /// koyuyor. AYRIM KASITLI ve mekanigin tamami bu ayrimda:
        ///
        ///   tahmin  -> kadro onerisi, hal onerisi, beklenen kisi
        ///   gercek  -> yalnizca gelis plani
        ///
        /// Sapma tahmine de yansisaydi oyuncu yine kesin bilgiye
        /// sahip olurdu ve oynaklik dekor kalirdi. Asil kazanc
        /// burada: sabah stok karari artik bir YARGI - fazla alirsan
        /// coper, az alirsan musteri kapidan doner.
        ///
        /// Cekilis GUNDE BIR ve _rngEvent akisindan: o akis zaten
        /// vardi, kayda giriyordu ve hic kullanilmiyordu. Tekrar
        /// oynatma birebir ayni kaliyor.
        /// </summary>
        private int ActualCustomers(int dayFactorBp)
        {
            int beklenen = ExpectedCustomers(dayFactorBp);
            int varyans = _economy.DemandVarianceBp;
            if (varyans <= 0 || beklenen <= 0) return beklenen;

            // [-varyans, +varyans] araliginda tek cekilis. Tamsayi:
            // kayan nokta cekirdekte yasak (docs/23 2.5).
            int aralik = 2 * varyans + 1;
            int sapma = (int)(_rngEvent.Next() % (uint)aralik) - varyans;

            int gercek = (int)Fx.MulDiv(beklenen, Fx.One + sapma, Fx.One);
            return gercek < 0 ? 0 : gercek;
        }

        private long DishPriceDiffBp(int dish)
        {
            if (dish < 0) return 0;
            long market = _content.Dishes[dish].Price;
            if (market <= 0) return 0;

            long diffBp = Fx.MulDiv(_dishPrice[dish] - market, Fx.One, market);
            // Piyasanin altina inmenin bir tabani var; icerikte
            // underpriceFloorBp olarak yaziyor (8500 = %15).
            long floorBp = _economy.UnderpriceFloorBp - Fx.One;
            if (diffBp < floorBp) diffBp = floorBp;
            return diffBp;
        }

        /// <summary>docs/12 5.4 memnuniyet formulu, santi-puan.</summary>
        private int ComputeSatisfaction(int party, int dish)
        {
            int sat = 10000;

            // Bekleme cezasi: (beklenen / sabir) x 60 puan
            if (_pPatienceTotalMs[party] > 0)
            {
                long penalty = Fx.MulDiv(_pWaitedMs[party], 6000, _pPatienceTotalMs[party]);
                sat -= (int)penalty;
            }

            // FIYAT CEZASI BUTUN FISE, yalnizca ana yemege degil.
            //
            // Once yalnizca ANA YEMEGIN fiyati piyasayla
            // karsilastiriliyordu; oysa fis dort kalemi birden yaziyor
            // (OrderPrice) ve SetPrice'in ust siniri yok. Yani her
            // mutfakta 32 yemegin 20'si - yanlar, icecekler, tatlilar -
            // istenildigi kadar pahali satilabiliyordu ve musteri bunu
            // HIC gormuyordu.
            //
            // Olculdu: Turk mutfaginda tek icecek var (ayran, 16).
            // Altmis gunde 1896 kisi, %40 icecek olasiligi. Ayrani
            // 160'a cikarmak +109.000 santi getiriyor - kampanyanin
            // butun karinin alti kati, sifir risk.
            //
            // Ceza artik her kalemin FISTEKI PAYIYLA agirliklandirilmis
            // sapmasi. Fiyatlar icerik degerindeyken her sapma sifir,
            // yani bu degisiklik dokunulmamis bir oyunda DAVRANISI
            // DEGISTIRMIYOR - kalibrasyonun referans botlari fiyata
            // dokunmuyor.
            long sapma = WeightedPriceDiffBp(party, dish);
            if (sapma != 0)
            {
                int sens = _content.Archetypes[_pArchetype[party]].PriceSensitivityBp;
                sat -= (int)Fx.MulDiv(sapma, sens, Fx.One);
            }

            sat += _pBonusCenti[party];

            // MALZEME KALITESI. Yemegin tarifindeki malzemelerin stoktaki
            // ortalama kalitesi. Icerik en hassas alti malzemeyi ET yaptigi
            // icin ayni kuresel ayar, etli yemekte agir, makarnada hafif
            // sonuc veriyor: ucuza kacmak tuzda serbest, ette felaket.
            if (dish >= 0) sat += DishQualityCenti(dish);

            // Sordugu yemegi bulamayan musteri. docs/02 "gorunur buyume":
            // eksik ekipmanin bedeli soyut bir hiz kaybi degil, masadaki
            // hayal kirikligi.
            if (_pAskedDish[party] >= 0)
            {
                // Buyutec de ORAN. Sebebi AskForMissingDish ile ayni:
                // mutlak sayi, menusunu makul daraltan oyuncu ile tek
                // yemek tutani ayni kefeye koyuyordu.
                int waiting = 0, total = 0;
                for (int i = 0; i < _content.Dishes.Length; i++)
                {
                    if (!UnlockedMain(i)) continue;
                    total++;
                    if (Awaited(i)) waiting++;
                }
                int scale = total > 0
                    ? Fx.One + (int)Fx.MulDiv(Fx.One, waiting, total)
                    : Fx.One;
                sat -= (int)Fx.MulDiv(_economy.AskMissCenti, scale, Fx.One);
            }

            // KARMASIKLIK YALNIZCA RISK, odul degil.
            //
            // Ilk yazimda sapma iki yonde de buyutuluyordu ve olcum bunu
            // yakaladi: Turk menusunun 17'si karmasiklik 3 oldugu icin
            // hicbir sey yapmayan "sadece_hal" oyuncusu 5.094'ten 33.488'e
            // firladi. Musterilerin cogu zaten memnun oldugu icin buyutec
            // pratikte tek yonlu calisti ve itibari sisirdi.
            //
            // Odul zaten FIYATTA: karmasiklik 3 yemek karmasiklik 1'in iki
            // kati fiyatli. Burada olmasi gereken sey bedeli: usta isi
            // yemegi gec goturursen musteri daha cok kiziyor.
            // Olcek MUTLAK degil BAGIL: yemegin karmasikligi kendi
            // mutfaginin ortalamasina gore. Mutlak olcekte Turk lokantasinin
            // 17 yemegi "zor" sayiliyor ve iyi oyuncunun itibari 51,6'da
            // kaliyordu; fast food'da 99,0 idi. Bagil olcekte fast food'un
            // birkac zor yemegi GERCEKTEN zor, Turk lokantasinin sulu
            // yemegi ise onun icin siradan.
            int neutral = _economy.SatisfactionNeutralCenti;
            if (dish >= 0 && sat < neutral)
            {
                int relBp = _content.RelativeComplexityBp(dish);
                if (relBp > Fx.One)
                {
                    int lossBp = Fx.One + (int)Fx.MulDiv(relBp - Fx.One, 4000, Fx.One);
                    sat = neutral - (int)Fx.MulDiv(neutral - sat, lossBp, Fx.One);
                }
                else if (relBp < Fx.One)
                {
                    // Ortalamanin altindaki yemek daha bagislayici.
                    int easeBp = Fx.One - (int)Fx.MulDiv(Fx.One - relBp, 4000, Fx.One);
                    sat = neutral - (int)Fx.MulDiv(neutral - sat, easeBp, Fx.One);
                }
            }

            if (sat < 0) sat = 0;
            if (sat > 10000) sat = 10000;
            return sat;
        }

        /// <summary>
        /// docs/12 5.5: gunluk_degisim = Toplam (memnuniyet - 60) x agirlik / 100
        /// Santi-puan ve baz puanla: (sat - 6000) x agirlikBp / 1.000.000
        /// Ara toplam mikro-puanda tutuluyor, gun sonunda bir kez yuvarlaniyor.
        /// </summary>
        private void AccumulateReputation(int party, int satisfactionCenti)
        {
            ArchetypeDef a = _content.Archetypes[_pArchetype[party]];
            long delta = (long)(satisfactionCenti - _economy.SatisfactionNeutralCenti)
                         * a.ReputationWeightBp * _pSize[party];
            _reputationDeltaMicro += delta;   // 1e6 olceginde
        }

        /// <summary>
        /// Tasma kabinin tavani: bir SONRAKI kademenin tavanina olan
        /// fark. En ust kademede sifir - orada tavan zaten 100.
        /// </summary>
        private int OverflowCapCenti()
        {
            int simdiki = _economy.TierForTables(_tableCount).ReputationCapCenti;
            if (simdiki <= 0 || simdiki > 10000) simdiki = 10000;

            int ustu = simdiki;
            for (int i = 0; i < _economy.TierCount; i++)
            {
                int c = _economy.TierAt(i).ReputationCapCenti;
                if (c > simdiki && (ustu == simdiki || c < ustu)) ustu = c;
            }
            return ustu > simdiki ? ustu - simdiki : 0;
        }

        private void ApplyReputation()
        {
            // Dogal erime: her gun 0,3 puan = 30 santi
            long deltaCenti = _reputationDeltaMicro / 1_000_000L
                              - _economy.ReputationDecayPerDayCenti;

            // Azalan getiri. Denge aracinin bulgusu: sonumsuz formulle
            // gunde +12 puan kazaniliyor ve itibar dokuz gunde 30'dan 100'e
            // cikiyordu; boylece altmis gunluk kampanyanin ana ilerleme
            // ekseni ilk haftada tukeniyordu.
            //
            // Kazanc kalan bosluga oranlanir, KAYIP oranlanmaz: itibar zor
            // kazanilir, kolay kaybedilir. Restoran isletmeciliginin dogrusu
            // da bu ve araştırmadaki oyuncu yorumlariyla ortusuyor.
            if (deltaCenti > 0)
            {
                int headroom = 10000 - _reputationCenti;
                if (headroom < 0) headroom = 0;

                // TABAN. Sonumleme, itibarin dokuz gunde tavana vurmasini
                // engellemek icin kondu ve o isi goruyor. Ama tepeye yakin
                // bolgede fazla sertti: olcum, iki mutfak arasindaki 3,5
                // puanlik MEMNUNIYET farkinin 20 puanlik ITIBAR farkina
                // donustugunu gosterdi (fast food 94,2, Turk 74,5).
                //
                // Sebep sonumlemenin dogrusal olmasi: itibar 90'a gelince
                // kazanc onda birine iniyor ve gunluk erime onu yeniyor.
                // Taban, tepedeki bolgeyi biciak sirti olmaktan cikariyor;
                // erken sonumleme aynen duruyor.
                // Taban 1500. Once 3000'di ve tepedeki bolgeyi fazla
                // yumusatiyordu: itibar 100'de bile gunluk kazanc pozitif
                // kaliyor, yani tepede DURMAK caba istemiyordu.
                if (headroom < 1500) headroom = 1500;
                deltaCenti = Fx.MulDiv(deltaCenti, headroom, 10000);
            }

            int before = _reputationCenti;
            _reputationCenti += (int)deltaCenti;
            if (_reputationCenti < 0) _reputationCenti = 0;

            // TAVAN KADEMEDEN. Dort masalik bir dukkan semtin konustugu
            // lokanta olamaz; itibar ancak buyudukce yukari acilir.
            int cap = _economy.TierForTables(_tableCount).ReputationCapCenti;
            if (cap <= 0 || cap > 10000) cap = 10000;
            if (_reputationCenti > cap)
            {
                // TASAN DEGER SILINMIYOR, BIRIKIYOR.
                _reputationOverflowCenti += _reputationCenti - cap;
                _reputationCenti = cap;

                // KAP BIR KADEME KADAR: sonsuz birikim, genisleme
                // gununde itibari dogrudan tavana firlatir ve yeni
                // kademenin kendi emegini anlamsiz kilardi.
                int kap = OverflowCapCenti();
                if (_reputationOverflowCenti > kap) _reputationOverflowCenti = kap;
            }

            Emit(SimEventKind.ReputationChanged, _reputationCenti, _reputationCenti - before);
        }

        // ---- gelis plani ---------------------------------------------------
        /// <summary>
        /// Gun basinda butun gelisler onceden hesaplanir ve tick'e gore
        /// siralanir. Boylece servis sirasinda rastgelelik cagrilmiyor ve
        /// tekrar oynatma ucuzluyor.
        /// </summary>
        private void BuildArrivalPlan()
        {
            int dayFactorBp = IsWeekend(_day)
                ? _economy.WeekendMultiplierBp
                : _economy.WeekdayMultiplierBp;

            // GERCEK sayi: tahmin degil. Fark oyuncunun sabah verdigi
            // stok kararinin karsiligi.
            int people = ActualCustomers(dayFactorBp);
            _plannedPeople = people;

            _arrCount = 0;
            _arrNext = 0;
            int assigned = 0;

            while (assigned < people && _arrCount < MaxParties)
            {
                int arch = PickArchetype();
                ArchetypeDef a = _content.Archetypes[arch];

                int size = a.GroupSizeMin;
                if (a.GroupSizeMax > a.GroupSizeMin)
                    size = _rngArrival.NextInt(a.GroupSizeMin, a.GroupSizeMax + 1);
                if (assigned + size > people) size = people - assigned;
                if (size < 1) size = 1;

                // Dilimler ESIT DEGIL; her mutfagin kendi gun bicimi var.
                // docs/28-zirve-karari.md Karar G.
                int slot = PickSlot(a);
                int tick = _timing.SlotStartTick(slot)
                           + _rngArrival.NextInt(_timing.SlotTicks(slot));

                _arrTick[_arrCount] = tick;
                _arrArchetype[_arrCount] = arch;
                _arrSize[_arrCount] = size;
                _arrRegular[_arrCount] = -1;
                _arrCount++;
                assigned += size;
            }

            SortArrivals();

            // Duzenli musteriler plan kurulup SIRALANDIKTAN sonra baglaniyor:
            // talebe eklenmiyorlar, planin icinden yer aliyorlar. Siralamadan
            // sonra olmasi sart - SortArrivals _arrRegular dizisini tasimiyor.
            BindRegularsToPlan();
        }

        private bool IsWeekend(int day)
        {
            int dayOfWeek = ((day - 1) % 7) + 1;      // 1..7
            return dayOfWeek > 7 - _economy.WeekendDaysPerWeek;
        }

        private int PickArchetype()
        {
            int total = 0;
            for (int i = 0; i < _content.Archetypes.Length; i++)
                total += _content.Archetypes[i].Weight;
            if (total <= 0) return 0;

            int pick = _rngArchetype.NextInt(total);
            for (int i = 0; i < _content.Archetypes.Length; i++)
            {
                pick -= _content.Archetypes[i].Weight;
                if (pick < 0) return i;
            }
            return _content.Archetypes.Length - 1;
        }

        private int PickSlot(ArchetypeDef a)
        {
            int pick = _rngArrival.NextInt(Fx.One);
            int acc = 0;
            for (int s = 0; s < a.ArrivalWeightsBp.Length; s++)
            {
                acc += a.ArrivalWeightsBp[s];
                if (pick < acc) return s;
            }
            return a.ArrivalWeightsBp.Length - 1;
        }

        /// <summary>
        /// Ekleme siralamasi. Kararli ve deterministik; Array.Sort'un
        /// karsilastirma temsilcisiyle kararliligi garanti degil.
        /// </summary>
        private void SortArrivals()
        {
            for (int i = 1; i < _arrCount; i++)
            {
                int t = _arrTick[i], a = _arrArchetype[i], s = _arrSize[i];
                int j = i - 1;
                while (j >= 0 && _arrTick[j] > t)
                {
                    _arrTick[j + 1] = _arrTick[j];
                    _arrArchetype[j + 1] = _arrArchetype[j];
                    _arrSize[j + 1] = _arrSize[j];
                    j--;
                }
                _arrTick[j + 1] = t;
                _arrArchetype[j + 1] = a;
                _arrSize[j + 1] = s;
            }
        }

        // ---- gun raporu ----------------------------------------------------
        public DayReport BuildDayReport()
        {
            int avgSat = _servedPeople > 0
                ? (int)(_satisfactionSum / _servedPeople)
                : 0;
            return new DayReport(_day, _servedParties, _servedPeople, _angryParties,
                                 _revenue, _ingredientCost, avgSat, _reputationCenti,
                                 _arrCount, _cash, _turnedAwayParties,
                                 _dayWages, _dayRent, _daySpoiled, _angrySeated);
        }

        private void Emit(SimEventKind kind, int a = 0, int b = 0, int c = 0, int d = 0)
        {
            _events.Push(new SimEvent(_tickIndex, kind, a, b, c, d));
        }
    }

    /// <summary>Bir servis gununun ozeti.</summary>
    public readonly struct DayReport
    {
        public readonly int Day;
        public readonly int ServedParties;
        public readonly int ServedPeople;
        public readonly int AngryParties;

        /// <summary>
        /// MASAYA OTURDUKTAN SONRA kizgin ayrilan grup sayisi.
        ///
        /// AngryParties ikisini birden sayiyor: masa bulamayip kapidan
        /// donenleri ve oturup bekleyip sinirini asanlari. Bunlar AYNI
        /// sey degil - biri kapasite sorunu, oteki servis sorunu - ve
        /// oyuncunun yapabilecegi sey de farkli. Ekranlar, denge
        /// aracinin CSV'si ve uyarilari bu ayrimi tasimadigi surece
        /// "kizgin musteri" sayisi iki farkli derdi tek sayiya
        /// katliyordu.
        /// </summary>
        public readonly int AngrySeatedParties;
        public readonly long Revenue;
        public readonly long IngredientCost;
        public readonly int AverageSatisfactionCenti;
        public readonly int ReputationCenti;
        public readonly int PlannedParties;
        public readonly long Cash;
        /// <summary>Menude bir sey bulamayip kapidan donen grup sayisi.</summary>
        public readonly int TurnedAwayParties;

        /// <summary>
        /// BUGUN odenen ucret ve kira. Sifir olabilir - haftada bir gun
        /// odeniyor.
        ///
        /// Rapora eklendi cunku "Kar" diye gosterilen sayi bunlari
        /// ICERMIYORDU: oyuncu personel alip cironun arttigini goruyor,
        /// "kar"in da arttigini goruyor, sonra kasa bosaliyor ve sebebini
        /// hicbir ekranda bulamiyordu. Oyunun temel gerilimi - kadro
        /// kapasite demek ama para demek - hicbir yerde gorunmuyordu.
        /// </summary>
        public readonly long WageCost;
        public readonly long RentCost;

        /// <summary>
        /// Bu gece cope giden stogun degeri.
        ///
        /// Rapora eklendi cunku GORUNMEYEN EN BUYUK GIDERDI: makul oynayan
        /// bir oyuncu altmis gunde aldigi malzemenin yuzde elli yedisini
        /// cope atiyor - 21.912 sikke, yilin net karindan fazla - ve bu
        /// sayi oyunun hicbir ekraninda yoktu. Oyuncu her sabah stok
        /// tazeliyor, aksam "kar" goruyor, kasanin neden dolmadigini
        /// anlamiyordu.
        ///
        /// NET KARA GIRMIYOR ve girmemeli: para stok alinirken cikti,
        /// burada bir daha dusulurse iki kere sayilir. Bu bir KAYIP
        /// kalemi, bir odeme degil.
        /// </summary>
        public readonly long SpoiledValue;

        /// <summary>Gunun NET kari: ciro - malzeme - ucret - kira.</summary>
        public long NetProfit
        {
            get { return Revenue - IngredientCost - WageCost - RentCost; }
        }

        public DayReport(int day, int servedParties, int servedPeople, int angryParties,
                         long revenue, long ingredientCost, int averageSatisfactionCenti,
                         int reputationCenti, int plannedParties, long cash,
                         int turnedAwayParties = 0,
                         long wageCost = 0, long rentCost = 0, long spoiledValue = 0,
                         int angrySeatedParties = 0)
        {
            AngrySeatedParties = angrySeatedParties;
            TurnedAwayParties = turnedAwayParties;
            WageCost = wageCost;
            RentCost = rentCost;
            SpoiledValue = spoiledValue;
            Day = day; ServedParties = servedParties; ServedPeople = servedPeople;
            AngryParties = angryParties; Revenue = revenue; IngredientCost = ingredientCost;
            AverageSatisfactionCenti = averageSatisfactionCenti;
            ReputationCenti = reputationCenti; PlannedParties = plannedParties; Cash = cash;
        }

        public long GrossProfit { get { return Revenue - IngredientCost; } }
    }
}
