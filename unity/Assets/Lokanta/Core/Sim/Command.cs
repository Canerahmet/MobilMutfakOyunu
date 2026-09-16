namespace Lokanta.Core.Sim
{
    /// <summary>
    /// docs/23-core-contract.md 7.2 komut listesi.
    /// Sadece simulasyonu DEGISTIREN oyuncu girdileri komuttur.
    /// Hiz, duraklatma, kamera ve ekran gecisi komut DEGILDIR: onlar
    /// gorunum durumudur, kaydedilmez.
    /// </summary>
    public enum CommandKind
    {
        None = 0,
        OpenService = 1,
        CloseDay = 2,
        SetPrice = 3,
        SetMenuSlot = 4,
        // 5, 9 ve 16 BOS: SetDailySpecial, AssignStation ve
        // RefillBroth silindi. Uculuk de tanimliydi, hicbir yerden
        // GONDERILMIYORDU ve Apply'in switch'inde de yoktu - yani
        // gonderilseydi default'a dusup reddedilecekti. Uc mekanik
        // vaadi (gunun yemegi, istasyon atama, tencere tazeleme)
        // kodda hic yoktu; enum onlari VAR gosteriyordu.
        //
        // Sayilar YENIDEN NUMARALANMIYOR: komut turu kayitli
        // oyunlarda ve olay akisinda sayi olarak geciyor.
        OrderIngredient = 6,
        Hire = 7,
        Fire = 8,
        Intervene = 10,
        Expand = 11,
        BuyEquipment = 12,
        TakeLoan = 13,
        ExtendCredit = 14,
        CollectCredit = 15,
        BuyStorage = 17,          // soguk hava kademesi
        SetQuality = 18,          // A: 0 dusuk, 1 standart, 2 yuksek
        SetCombo = 19,            // A: 0 kapali, 1 acik. Fast food imza mekanigi

        /// <summary>
        /// Kac salon calisani LAVABOYA adanacak. A: sayi.
        ///
        /// Bulasikci ayri bir personel havuzu DEGIL, lavaboya adanmis bir
        /// salon calisani. Sebebi tasarimin kendisi: docs/14 salonu
        /// "garson + bulasikci + kasiyer, tek is havuzu" diye kuruyor ve
        /// maas da o harmandan geliyor. Ayri bir havuz, ayni kisiyi iki
        /// ucret tablosunda saymak olurdu.
        ///
        /// Oyuncu icin karar ayni: bir kisilik kadroyu bulasiga ayiriyor.
        /// Ayirmazsa bulasik birikince garson kendiliginden lavaboya
        /// geciyor ve servis aksiyor.
        /// </summary>
        SetDishwashers = 21,

        /// <summary>
        /// Onerilen stogun TAMAMI, tek komutla.
        ///
        /// Once arayuz malzeme basina bir OrderIngredient gonderiyordu:
        /// icerikte yetmis yedi malzeme var ve bir sabahta elliye
        /// yakini "eksik" cikabiliyor. Gunluk komut siniri 256, yani
        /// "Onerilen stogu al" dugmesine bes kez basmak gunun butun
        /// butcesini yiyordu - ve sonrasinda fiyat, menu, ise alim,
        /// ekipman, genisleme, mudahale ve veresiye dahil HER komut
        /// sessizce reddediliyordu. Oyuncu "dugmeler calismiyor"
        /// goruyor, sebebini hicbir yerde ogrenemiyordu.
        ///
        /// Miktari simulasyon kendisi hesapliyor, yani tekrar oynatma
        /// da ayni sonucu veriyor.
        /// </summary>
        OrderRecommended = 20,
        Count = 22
    }

    /// <summary>Patronun bir masaya mudahalesi. docs/12 5.4.</summary>
    public enum InterventionKind
    {
        /// <summary>
        /// Gecersiz. Eskiden "Apology" idi: tanimliydi, HICBIR yerden
        /// gonderilmiyordu, ve gonderilseydi sessizce CAYIN etkisini
        /// alirdi - ustelik cayin parasini odemeden, yani cay
        /// dugmesinden kesin ustun bir hayalet.
        ///
        /// 0 bosa cikarilmiyor, None olarak tutuluyor: varsayilan
        /// deger (default(InterventionKind)) bir yere gelirse
        /// gecerli bir mudahaleye donusmesin, REDDEDILSIN.
        /// </summary>
        None = 0,
        FreeTea = 1,      // ikram
        OwnerAttention = 2, // patron bizzat ilgilendi: +20 puan

        /// <summary>
        /// Istasyonu acele ettirir. docs/02 59 uc mudahale sayiyor ve bu
        /// ucuncusuydu: "bir istasyonu hizlandir".
        ///
        /// Patron PISIRMIYOR -- docs/14 bunu acikca yasakliyor. Yaptigi sey
        /// yolu acmak: malzemeyi getirmek, tabagi almak, siraya girmek.
        /// Mekanik karsiligi, o istasyonda pisen islerin kalan DUVAR
        /// SAATININ bir kismini silmek.
        ///
        /// Neden gerekliydi: diger iki tur de SALON tarafinda. Mutfak
        /// darbogaz oldugunda patronun servis sirasinda yapabilecegi
        /// hicbir sey yoktu.
        /// </summary>
        RushStation = 3
    }

    /// <summary>
    /// Yirmi bayt, tamamen tamsayi. Kayit dosyasindaki komut gunlugu
    /// bunlardan olusuyor ve yuklemede sirayla tekrar oynatiliyor.
    /// </summary>
    public readonly struct Command
    {
        public readonly long Tick;
        public readonly CommandKind Kind;
        public readonly int A;
        public readonly int B;
        public readonly int C;

        public Command(long tick, CommandKind kind, int a = 0, int b = 0, int c = 0)
        {
            Tick = tick; Kind = kind; A = a; B = b; C = c;
        }

        public override string ToString()
        {
            return "[" + Tick + "] " + Kind + "(" + A + "," + B + "," + C + ")";
        }
    }
}
