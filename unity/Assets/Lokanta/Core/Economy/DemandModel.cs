namespace Lokanta.Core.Economy
{
    /// <summary>
    /// docs/12-economy.md 5.1
    ///   musteri = masa x taban x (0,5 + itibar/100) x gun_katsayisi
    ///
    /// Tamsayi cevirisi: itibar santi-puan (30 = 3000) tutuluyor ve
    /// itibar/100 orani baz puan cinsinden tam olarak santi-puana esit,
    /// cunku itibar_centi = itibar x 100 ve (itibar/100) x 10000 = itibar x 100.
    /// Yani talep carpani = 5000 + itibarSanti baz puan.
    /// </summary>
    public static class DemandModel
    {
        /// <summary>
        /// Talep carpani, baz puan. Itibar 30 -> 8000 (0,80).
        ///
        /// Dogrusal kismin bir TABANI vardi ve fazla yuksekti: itibar sifira
        /// inse bile restoran taban talebin yarisini aliyordu. Yani kimsenin
        /// konusmadigi bir dukkan hala yari doluymus gibi davraniyordu ve
        /// ihmalin olum sarmali hissedilmiyordu.
        ///
        /// Kirilma noktasi 20 puan, baslangic itibarinin (30) ALTINDA.
        ///
        /// Once 30'a konmustu ve test yakaladi: itibar ilk gunden erimeye
        /// basliyor, yani her oyuncu daha ikinci gunde dik bolgeye giriyor
        /// ve acilis haftasi herkes icin cokuyordu. Kirilma baslangicin
        /// altinda olmali ki yalnizca GERCEK cokus cezalandirilsin.
        /// </summary>
        public static int DemandMultiplierBp(int reputationCenti)
        {
            int linear = (Fx.One / 2) + reputationCenti;
            if (reputationCenti >= StartReputationCenti) return linear;

            // Sifirda %10, baslangic itibarinda %100.
            int k = FloorBp + (int)Fx.MulDiv(Fx.One - FloorBp,
                                             reputationCenti, StartReputationCenti);
            return (int)Fx.MulDiv(linear, k, Fx.One);
        }

        /// <summary>
        /// FIYATIN TALEBE DOGRUDAN ETKISI.
        ///
        /// Bu kanal bir zamanlar HIC YOKTU ve oyundaki en buyuk acigi
        /// aciyordu. Fiyatin tek yolu memnuniyet -> itibar idi; itibar
        /// ise masa kademesinin tavanina KIRPILIYOR. Yani tavana dayanmis
        /// bir oyuncu icin memnuniyet kaybi hicbir sey satin almiyordu ve
        /// kucuk bir zam BEDAVAYDI.
        ///
        /// Olculdu (24 tohum, 60 gun, fast food): piyasanin %10 ustunde
        /// fiyatlayan bir bot 27.849 sikke ile bitiriyordu - oyunun en
        /// gelismis stratejisi 25.092, taban strateji 18.670. Yani sabah
        /// bir kez basilan bir dugme, DAHA AZ masa ve DAHA AZ kadroyla
        /// her seyi geciyordu. Ceza yalnizca bandin disinda vardi (%30
        /// zamda itibar sifirlaniyor ve dukkan batiyor), arasi bostu.
        ///
        /// Kanal ayrica OKUNABILIRLIK: zam yapan oyuncu artik ertesi gun
        /// daha az musteri goruyor. Eskiden hicbir ekran ona zammin bir
        /// bedeli oldugunu soylemiyordu.
        ///
        /// Taban ve tavan var, cunku esneklik dogrusal: %50 indirim
        /// talebi ikiye katlamamali, %60 zam da dukkani bir gunde
        /// bosaltmamali. Itibar cokusu zaten ayri bir cezadir.
        /// </summary>
        public static int ApplyPrice(int people, long priceDiffBp, int elasticityBp)
        {
            if (people <= 0 || elasticityBp <= 0 || priceDiffBp == 0) return people;

            long multBp = Fx.One - Fx.MulDiv(priceDiffBp, elasticityBp, Fx.One);
            if (multBp < PriceFloorBp) multBp = PriceFloorBp;
            if (multBp > PriceCeilBp) multBp = PriceCeilBp;
            return (int)Fx.MulDiv(people, multBp, Fx.One);
        }

        /// <summary>Fiyat kanalinin talebi indirebilecegi en dusuk oran.</summary>
        private const int PriceFloorBp = 3000;

        /// <summary>Fiyat kanalinin talebi cikarabilecegi en yuksek oran.</summary>
        private const int PriceCeilBp = 13000;

        /// <summary>Egrinin kirilma noktasi, santi-puan. 20 puan.</summary>
        private const int StartReputationCenti = 2000;

        /// <summary>Itibar sifirken talebin kalan payi, baz puan.</summary>
        private const int FloorBp = 1000;

        /// <summary>
        /// Bir gunun musteri sayisi. Tek yuvarlama en sonda yapilir;
        /// ara adimlarda yuvarlama YOK, cunku Python modeli de tek kez yuvarliyor.
        /// </summary>
        public static int CustomersPerDay(int tables, int reputationCenti,
                                          int basePerTable, int dayFactorBp)
        {
            long seats = (long)tables * basePerTable;
            long numerator = seats * DemandMultiplierBp(reputationCenti) * dayFactorBp;
            return (int)Fx.MulDiv(numerator, 1, (long)Fx.One * Fx.One);
        }

        public static int WeekdayCustomers(int tables, int reputationCenti, EconomyConfig cfg)
        {
            return CustomersPerDay(tables, reputationCenti,
                                   cfg.CustomerBasePerTable, cfg.WeekdayMultiplierBp);
        }

        public static int WeekendCustomers(int tables, int reputationCenti, EconomyConfig cfg)
        {
            return CustomersPerDay(tables, reputationCenti,
                                   cfg.CustomerBasePerTable, cfg.WeekendMultiplierBp);
        }

        /// <summary>Haftanin toplam musterisi. Hafta sonu gunleri ayri katsayili.</summary>
        public static int WeekCustomers(int tables, int reputationCenti, EconomyConfig cfg)
        {
            int weekday = WeekdayCustomers(tables, reputationCenti, cfg);
            int weekend = WeekendCustomers(tables, reputationCenti, cfg);
            int weekendDays = cfg.WeekendDaysPerWeek;
            return weekday * (7 - weekendDays) + weekend * weekendDays;
        }
    }
}
