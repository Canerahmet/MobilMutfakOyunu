namespace Lokanta.Core.Sim
{
    /// <summary>
    /// Cekirdekten gorunume tek yonlu akan olaylar.
    /// Yuk tamsayi; metin yok. Gorunum kimligi anahtara, anahtari metne cevirir.
    /// docs/23-cekirdek-sozlesmesi.md 6.3.
    /// </summary>
    public enum SimEventKind
    {
        None = 0,
        DayOpened = 1,          // A gun
        ServiceOpened = 2,
        DayClosed = 3,          // A gun
        CustomerArrived = 4,    // A musteri, B arketip, C kisi sayisi
        CustomerSeated = 5,     // A musteri, B masa
        OrderTaken = 6,         // A musteri, B yemek
        FoodReady = 7,          // A musteri, B yemek
        FoodServed = 8,         // A musteri, B masa
        CustomerPaid = 9,       // A musteri, B santi-sikke (int'e sigar), C memnuniyet santi
        CustomerLeftAngry = 10, // A musteri, B asama, C beklenen ms
        TableCleared = 11,      // A masa
        PatienceWarning = 12,   // A musteri, B kalan yuzde (bp)
        // OLAYIN ADI YAPTIGI SEYI SOYLEMIYORDU.
        //
        // Adi "StockOut" ve yorumu "A yemek" idi; yayan satir ise
        // A yerine PARTI indisini veriyordu (Simulation.cs). Arayuz
        // de A yi yemek sanip isim basiyordu, yani oyuncuya ilgisiz
        // bir yemek adi gosteriliyordu - parti yuvalari kucuk
        // numaralardan dagitildigi icin cogu zaman GECERLI ama
        // YANLIS bir ad. Hicbir sey hata vermiyordu.
        //
        // Olay zaten "malzeme bitti" degil: musteri menude
        // yapabilecegi ana yemek bulamayinca KAPIDAN DONUYOR.
        TurnedAway = 13,        // A parti, B arketip, C kisi
        ReputationChanged = 14, // A yeni itibar santi, B degisim santi
        CommandRejected = 15,   // A komut turu, B sebep
        WeeklyCostsPaid = 16,   // A kira, B maas, C kalan kasa (santi, int'e sigar)
        CashWentNegative = 17,  // A gun, B borc
        EquipmentBought = 18,   // A istasyon, B yeni kademe
        StorageBought = 19,     // A yeni kademe, B keepBp
        DishRequested = 20,     // A yemek: musteri sordu ama yapilamiyor
        StationRushed = 21,     // A istasyon, B kac is hizlandi
        StaffLeveledUp = 22,    // A havuz (0 mutfak, 1 salon), B yeni seviye
        CreditExtended = 23,    // A masa/grup, B tutar (santi)
        CreditCollected = 24,   // A tutar (santi), B kalan acik veresiye
        CreditDefaulted = 25,   // A tutar (santi), B itibar cezasi
        ComboOrdered = 26,      // A grup, B kombo fiyati (santi)
        RegularVisited = 27,    // A duzenli musteri, B memnuniyeti
        RegularStoryBeat = 28,  // A duzenli musteri, B acilan sahne
        RegularUpset = 29,      // A duzenli musteri, B kac gun gelmeyecek
        StaffResigned = 30,     // A havuz (0 mutfak, 1 salon), B sira
        WagesLate = 31,         // A gun, B eksik kalan tutar (santi)
        DishUnlocked = 32,      // A yemek: bugun acildi
        EquipmentSold = 33,     // A istasyon, B yeni kademe (batma merdiveni)
        Downsized = 34,         // A yeni masa sayisi, B yeni kademe

        /// <summary>
        /// TEMIZ TABAK BITTI: mutfak pisen yemegi cikaramiyor.
        /// A kirli tabak sayisi, B lavaboda kac kisi var.
        ///
        /// Neden olay: darbogazin oyuncuya GORUNMESI gerekiyor. Servis
        /// sebepsiz yavaslarsa oyuncu bunu mekanik degil HATA diye
        /// okuyor - docs/14 bulasikciyi "gorunmeyen ama tikaninca fark
        /// edilen" bir darbogaz diye tarif ediyor ve "fark edilen"
        /// kismi ancak soylenirse oluyor.
        /// </summary>
        PlatesOut = 35,

        /// <summary>
        /// NISAN KAZANILDI. A nisan indisi (Badges.*).
        ///
        /// Gorev degil TANIMA: olay gun kapanisinda, oyuncu o seyi
        /// ZATEN yaptiktan sonra cikiyor.
        /// </summary>
        BadgeEarned = 36,

        /// <summary>
        /// PERSONEL KIDEMI. A havuz (0 mutfak, 1 salon), B gun sayisi.
        ///
        /// Nisanla ayni aile: gorev degil TANIMA. Oyuncu bir sey
        /// yapmiyor, bir sey OLDUGU icin soyleniyor - biri uzun suredir
        /// burada.
        ///
        /// Neden bu an secildi (docs/53): yirmi mudavimin ucer sahnesi
        /// vardi, personelin sifir satiri. Arastirma taradigi on iki
        /// oyunda uzun kidem icin YAZILMIS tek satir bulamadi - yani
        /// sahipsiz. Ve Turkce kaynaklardaki asil sikayetle ortusuyor:
        /// bulasikci kendine "restoranin kalbi" diyor ama "hicbir sey
        /// yapmiyormusuz gibi gorunuyoruz". "Beni gor" diyen bir satir,
        /// "bana zam ver"den daha sert iniyor.
        /// </summary>
        StaffTenure = 37,
        Count = 38
    }

    public readonly struct SimEvent
    {
        public readonly long Tick;
        public readonly SimEventKind Kind;
        public readonly int A;
        public readonly int B;
        public readonly int C;
        public readonly int D;

        public SimEvent(long tick, SimEventKind kind, int a = 0, int b = 0, int c = 0, int d = 0)
        {
            Tick = tick; Kind = kind; A = a; B = b; C = c; D = d;
        }

        public override string ToString()
        {
            return "[" + Tick + "] " + Kind + "(" + A + "," + B + "," + C + "," + D + ")";
        }
    }

    /// <summary>
    /// Sabit kapasiteli halka tampon. Dolarsa en eskisi dusuyor.
    /// Bir tick partisinde 4096 olay uretmek zaten tasarim hatasi olurdu.
    /// </summary>
    public sealed class EventBuffer
    {
        private readonly SimEvent[] _items;
        private int _head;
        private int _count;
        private int _dropped;

        public EventBuffer(int capacity = 4096)
        {
            _items = new SimEvent[capacity];
        }

        public int Count { get { return _count; } }
        public int Dropped { get { return _dropped; } }

        public void Push(in SimEvent e)
        {
            if (_count == _items.Length)
            {
                _head = (_head + 1) % _items.Length;
                _count--;
                _dropped++;
            }
            int tail = (_head + _count) % _items.Length;
            _items[tail] = e;
            _count++;
        }

        /// <summary>Tamponu bosaltir ve verilen diziye kopyalar. Donen sayi kadar gecerli.</summary>
        public int Drain(SimEvent[] destination)
        {
            int n = _count < destination.Length ? _count : destination.Length;
            for (int i = 0; i < n; i++)
                destination[i] = _items[(_head + i) % _items.Length];
            _head = (_head + n) % _items.Length;
            _count -= n;
            return n;
        }

        public void Clear()
        {
            _head = 0; _count = 0; _dropped = 0;
        }
    }
}
