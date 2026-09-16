using System;

namespace Lokanta.Core.Sim
{
    /// <summary>
    /// Servis gununun zaman modeli. Butun sureler milisaniye, simulasyon zamani.
    ///
    /// Bu sayilar KAPASITE MODELINDEN turetilir, secilmez:
    ///   salon_ms_per_person   = servis_gunu_ms / garson_kapasitesi
    ///   mutfak_ms_per_person  = servis_gunu_ms / asci_kapasitesi
    ///
    /// Aksi halde iki model birbiriyle celisir. Uretilen icerikteki ilk
    /// prepMs degerleri (hamburger 75.000 ms) bu kontrolden gecmiyordu:
    /// 8 dakikalik gunde bir asci gunde alti hamburger yapabilirdi.
    ///
    /// Ayrinti ve turetme: docs/27-time-model.md
    /// </summary>
    public sealed class TimingConfig
    {
        /// <summary>Bir tick'in simulasyon zamani. docs/23 1.2.</summary>
        public const int TickMs = 100;

        public int ServiceDayMs { get; }
        public int SlotCount { get; }

        // --- Dilim sureleri, MUTFAGA gore ------------------------------------
        //
        // docs/28-peak-decision.md Karar G. Dilimler ESIT DEGIL.
        //
        // Cakisma soyleydi: Turk lokantasinin musterilerinin %60'i ogle
        // diliminde geliyor (kimlik diregi), ama esit dilimde bu fiziksel
        // olarak servis edilemiyor; gereken 16 personel ve 17 masa, tavanlar
        // 12 ve 14. Cozum paylari degil SURELERI mutfaga gore degistirmek:
        // Turk'te ogle dilimi gunun %48'ini kapliyor, pay %60 olarak kaliyor.
        private readonly int[] _slotTicks;
        private readonly int[] _slotStartTick;

        // --- Salon havuzu, kisi basina ---------------------------------------
        public int SeatOrderMs { get; }
        public int ServeMs { get; }
        public int PayMs { get; }
        public int ClearMs { get; }

        /// <summary>
        /// BIR TABAGIN elde yikanma suresi.
        ///
        /// ClearMs'in ucte biri: masa toplamak yurumeyi, tepsiyi ve
        /// silmeyi iceriyor; bir tabagi yikamak tek bir hareket. Sayi
        /// TURETILMIS, ayri bir sabit degil - ikisi ayri ayri
        /// ayarlandiginda birinin degismesi otekini sessizce anlamsiz
        /// yapardi.
        /// </summary>
        public int WashMs { get { return ClearMs / 3; } }

        /// <summary>
        /// ADANMIS BULASIKCININ bir tabagi yikama suresi.
        ///
        /// Lavaboya adanmis kisi UZMANDIR ve bu, icerikte zaten yaziyor:
        /// staff-roles.json'da bulasikci rolunun gunluk kapasitesi 48,
        /// garsonunki 26 - yani "bu isi yapan kisi" bir buculuk kattan
        /// fazla verimli. Simulasyon bu farki HIC kullanmiyordu: adanmis
        /// bulasikci da, imdada kosan garson da ayni WashMs ile yikiyordu.
        ///
        /// Sonucu olculmustu: bulasikci ayirmak tabaksiz beklemeyi
        /// 263'ten 349'a CIKARIYORDU, cunku tek kisi, kriz aninda birden
        /// lavaboya kosan uc garsondan az yikiyor (docs/49).
        ///
        /// Oran ROL TABLOSUNDAN turetiliyor, uydurulmuyor: 26/48.
        /// </summary>
        public int DishwasherWashMs
        {
            get
            {
                int ms = (int)Core.Fx.MulDiv(WashMs, DishwasherSpeedBp, Core.Fx.One);
                return ms < 1 ? 1 : ms;
            }
        }

        /// <summary>
        /// Adanmis bulasikcinin yikama suresi carpani, baz puan.
        /// 10000 = fark yok. Rol tablosundaki 26/48 orani ~5400.
        /// </summary>
        public int DishwasherSpeedBp { get; }

        // --- Mutfak havuzu ---------------------------------------------------
        /// <summary>Kisi basina ortalama tabak sayisi (kombo bunu buyutuyor).</summary>
        public int DishesPerPersonBp { get; }

        // --- Musterinin kendi zamani (havuz tuketmez) -------------------------
        public int EatMs { get; }

        /// <summary>Sabir bu orana dustugunde gorunum uyarilir.</summary>
        public int PatienceWarnBp { get; }

        // --- Sabir tuketme hizlari, asamaya gore ------------------------------
        //
        // Neden sabit degil: docs/12 5.2 sabri 8-40 saniye veriyor ama ayni
        // bolum bir servisin ~120 saniye surdugunu soyluyor. Tek hizla bu ikisi
        // celisir; her musteri her zaman cikip giderdi. Ilk simulasyon kosusu
        // tam olarak bunu gosterdi: alti gruptan altisi kizgin ayrildi.
        //
        // Dogru okuma: sabir ILGILENILMEME toleransidir. Masa beklerken ve
        // siparisi alinmayi beklerken tam hizla, yemegi beklerken yavas
        // tukenir; garson masadayken hic tukenmez.
        public int DrainWaitingTableBp { get; }
        public int DrainWaitingOrderBp { get; }
        public int DrainWaitingFoodBp { get; }
        public int DrainWaitingPayBp { get; }

        /// <summary>
        /// Musteri, bekleyemeyecegi yemegi siparis etmez. Aday yemekler
        /// prepMs &lt;= sabir x bu katsayi olanlar. Gercekci ve ucuz: aceleci
        /// musteri hizli kalem alir.
        /// </summary>
        public int OrderPatienceFactorBp { get; }

        public TimingConfig(int serviceDayMs, int seatOrderMs, int serveMs, int payMs,
                            int clearMs, int eatMs, int dishesPerPersonBp,
                            int[] slotDurationsBp = null,
                            int slotCount = 4, int patienceWarnBp = 3000,
                            int drainWaitingTableBp = 10_000,
                            int drainWaitingOrderBp = 10_000,
                            // 3500 -> 500: OLCUM SONUCU.
                            //
                            // Sabir eskiden yemek piserken DONUYORDU
                            // (grup "gorevde" isaretleniyordu) ve bu,
                            // "yemek bekliyor" tiklerinin %87'sini
                            // kapsiyordu; yani 3500 fiilen ~465 olarak
                            // isliyordu. Donma kaldirilinca ayni sayi
                            // 7,5 kat sert oldu ve tek ascili bir
                            // restoranda kimse servis edilemiyordu
                            // (deneyim testi bunu yakaladi: yuz gunde
                            // asci hala 0. seviye).
                            //
                            // Yeni deger eski ETKIYI koruyor ama artik
                            // pisme suresine BAGLI: uzun pisen yemek
                            // gercekten daha cok sabir yiyor, ekipman
                            // ve asci deneyimi musteri tarafinda
                            // goruunuyor. docs/27 Karar D'nin isteyip de
                            // alamadigi sey buydu.
                            int drainWaitingFoodBp = 500,
                            int drainWaitingPayBp = 5_000,
                            int orderPatienceFactorBp = 20_000,
                            // Rol tablosundan: garson 26 / bulasikci 48
                            // gunluk kapasite -> 26/48 = 5417 bp.
                            int dishwasherSpeedBp = 5_417)
        {
            if (dishwasherSpeedBp <= 0)
                throw new ArgumentOutOfRangeException(nameof(dishwasherSpeedBp));
            DishwasherSpeedBp = dishwasherSpeedBp;
            DrainWaitingTableBp = drainWaitingTableBp;
            DrainWaitingOrderBp = drainWaitingOrderBp;
            DrainWaitingFoodBp = drainWaitingFoodBp;
            DrainWaitingPayBp = drainWaitingPayBp;
            OrderPatienceFactorBp = orderPatienceFactorBp;
            if (serviceDayMs <= 0) throw new ArgumentOutOfRangeException(nameof(serviceDayMs));
            if (serviceDayMs % TickMs != 0)
                throw new ArgumentException("Servis gunu tick'e tam bolunmeli", nameof(serviceDayMs));

            ServiceDayMs = serviceDayMs;
            SeatOrderMs = seatOrderMs;
            ServeMs = serveMs;
            PayMs = payMs;
            ClearMs = clearMs;
            EatMs = eatMs;
            DishesPerPersonBp = dishesPerPersonBp;
            SlotCount = slotCount;
            PatienceWarnBp = patienceWarnBp;
            // Dilim paylari SAKLANIYOR: WithEatMs gibi turetilmis
            // kopyalar onlari yeniden vermek zorunda.
            _slotDurationsBp = slotDurationsBp;

            int ticks = serviceDayMs / TickMs;
            _slotTicks = new int[slotCount];
            _slotStartTick = new int[slotCount];

            if (slotDurationsBp == null)
            {
                // Varsayilan esit dilim. Mutfak icerigi yuklenmemisse bu kullanilir.
                int each = ticks / slotCount;
                for (int i = 0; i < slotCount; i++) _slotTicks[i] = each;
                _slotTicks[slotCount - 1] += ticks - each * slotCount;
            }
            else
            {
                if (slotDurationsBp.Length != slotCount)
                    throw new ArgumentException(
                        "slotDurationsBp " + slotCount + " deger olmali", nameof(slotDurationsBp));

                int sum = 0;
                for (int i = 0; i < slotCount; i++) sum += slotDurationsBp[i];
                if (sum != Fx.One)
                    throw new ArgumentException(
                        "slotDurationsBp toplami " + sum + ", 10000 olmali",
                        nameof(slotDurationsBp));

                int used = 0;
                for (int i = 0; i < slotCount - 1; i++)
                {
                    _slotTicks[i] = (int)Fx.MulDiv(ticks, slotDurationsBp[i], Fx.One);
                    used += _slotTicks[i];
                }
                // Son dilim kalani alir: yuvarlama artigi gunu kisaltmasin.
                _slotTicks[slotCount - 1] = ticks - used;
            }

            int acc = 0;
            for (int i = 0; i < slotCount; i++)
            {
                _slotStartTick[i] = acc;
                acc += _slotTicks[i];
            }
        }

        public int ServiceTicks { get { return ServiceDayMs / TickMs; } }

        /// <summary>Dilimin tick cinsinden uzunlugu. Dilimler esit degil.</summary>
        public int SlotTicks(int slot) { return _slotTicks[slot]; }

        /// <summary>Dilimin servis gunu icindeki baslangic tick'i.</summary>
        public int SlotStartTick(int slot) { return _slotStartTick[slot]; }

        /// <summary>Ayni ayarlarin mutfaga ozel dilim sureleriyle kopyasi.</summary>
        /// <summary>
        /// Yemek yeme suresini mutfaktan alir. Icerik 38.000 diyordu,
        /// varsayilan 45.000 kullaniyordu; fark masa devir hizinda %18.
        /// </summary>
        private readonly int[] _slotDurationsBp;

        public TimingConfig WithEatMs(int eatMs)
        {
            if (eatMs <= 0) return this;
            return new TimingConfig(
                ServiceDayMs, SeatOrderMs, ServeMs, PayMs, ClearMs, eatMs,
                DishesPerPersonBp, _slotDurationsBp, SlotCount, PatienceWarnBp,
                DrainWaitingTableBp, DrainWaitingOrderBp, DrainWaitingFoodBp,
                DrainWaitingPayBp, OrderPatienceFactorBp);
        }

        public TimingConfig WithSlotDurations(int[] slotDurationsBp)
        {
            return new TimingConfig(
                ServiceDayMs, SeatOrderMs, ServeMs, PayMs, ClearMs, EatMs,
                DishesPerPersonBp, slotDurationsBp, SlotCount, PatienceWarnBp,
                DrainWaitingTableBp, DrainWaitingOrderBp, DrainWaitingFoodBp,
                DrainWaitingPayBp, OrderPatienceFactorBp);
        }

        /// <summary>Kisi basina toplam salon isi.</summary>
        public int SalonMsPerPerson
        {
            get { return SeatOrderMs + ServeMs + PayMs + ClearMs; }
        }

        /// <summary>Kisi basina mutfak isi, ortalama tabak sayisiyla olceklenmis.</summary>
        public int KitchenMsPerPerson(int averagePrepMs)
        {
            return (int)Fx.MulDiv(averagePrepMs, DishesPerPersonBp, Fx.One);
        }

        /// <summary>
        /// Kapasite modeliyle tutarlilik denetimi. Yukleme sirasinda cagrilir;
        /// tutmuyorsa icerik reddedilir, sessizce devam edilmez.
        /// </summary>
        public bool MatchesCapacity(int salonCapacityPerDay, int tolerancePercent, out int expectedMs)
        {
            expectedMs = ServiceDayMs / salonCapacityPerDay;
            int actual = SalonMsPerPerson;
            int diff = actual > expectedMs ? actual - expectedMs : expectedMs - actual;
            return diff * 100 <= expectedMs * tolerancePercent;
        }

        /// <summary>
        /// Gecici varsayilan. docs/27-time-model.md tamamlaninca oradaki
        /// sayilarla degistirilecek; o zamana kadar kapasite modelinden
        /// dogrudan turetilmis degerler kullaniliyor.
        ///   servis gunu 480.000 ms, garson kapasitesi 25 -> kisi basi 19.200 ms
        /// </summary>
        public static TimingConfig Default()
        {
            return new TimingConfig(
                serviceDayMs: 480_000,
                seatOrderMs: 5_000,
                serveMs: 4_000,
                payMs: 4_200,
                clearMs: 6_000,     // toplam 19.200 = 480.000 / 25
                eatMs: 45_000,
                dishesPerPersonBp: 14_000);   // kisi basi 1,4 tabak
        }
    }
}
