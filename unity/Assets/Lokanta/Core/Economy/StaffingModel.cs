namespace Lokanta.Core.Economy
{
    /// <summary>Bir haftanin kadrosu.</summary>
    public readonly struct Crew
    {
        public readonly int Cooks;
        public readonly int Salon;

        // Burada bir SalonWorkMicro alani vardi: zirve gunun salon is yuku.
        // Hicbir yerde okunmuyordu, ve dort kurucusundan ikisi ona 0 yaziyordu
        // - yani okuyan biri cikarsa YANLIS deger okuyacakti. Yuk zaten
        // peakCustomers'tan yeniden hesaplanabiliyor; alan silindi.
        public Crew(int cooks, int salon)
        {
            Cooks = cooks; Salon = salon;
        }

        public int Total { get { return Cooks + Salon; } }
    }

    /// <summary>
    /// docs/14-personel-sistemi.md kapasite modeli.
    ///
    /// Iki havuz:
    ///   Mutfak  gereken = tavan(zirve / asci_kapasitesi). Patron pisiremez.
    ///   Salon   is-gunu uzerinden. Patronun katkisi once dusulur.
    ///
    /// Kadro ZIRVE gune (hafta sonu) kurulur, ucreti yedi gun odenir.
    /// Bu, ilk ise alimin ikinci haftaya dusmesinin sebebi: senaryo degil, yuk.
    /// </summary>
    public static class StaffingModel
    {
        public static Crew Required(int peakCustomers, EconomyConfig cfg)
        {
            if (peakCustomers <= 0) return new Crew(0, 0);

            int cooks = Fx.CeilDiv(peakCustomers, cfg.CookCapacityPerDay);

            long salonWork = (long)peakCustomers * cfg.SalonWorkPerCustomerMicro;
            long afterOwner = salonWork - cfg.OwnerWorkMicro;
            int salon = afterOwner <= 0 ? 0 : (int)Fx.CeilDivL(afterOwner, Fx.Micro);

            return new Crew(cooks, salon);
        }

        /// <summary>
        /// Haftalik maas, santi-sikke. Deneyim zammi birikimli ve nano
        /// hassasiyetinde; baz puanla ussalmak sekizinci haftada birkac
        /// sikkelik sapma uretiyordu.
        /// </summary>
        /// <summary>
        /// Zammin us alabilecegi en fazla hafta. %2,2'de 32 hafta 2,00
        /// kati asiyor, yani tavan zaten orada devreye giriyor; bu sinir
        /// PowNano'nun kendisini tasmadan onceye baglayan ikinci kemer.
        /// </summary>
        private const int MaxWageGrowthWeeks = 64;

        public static long WeeklyWageBill(Crew crew, int week, EconomyConfig cfg)
        {
            long cookBill = (long)crew.Cooks * 7 * cfg.CookDailyWage;

            // Kisi basi salon ucretini onceden yuvarlamiyoruz: pay bir arada
            // tutulup bolme tek seferde yapiliyor.
            long salonBill = crew.Salon == 0
                ? 0
                : Fx.MulDiv((long)crew.Salon * 7 * cfg.SalonWageNumerator,
                            1, cfg.SalonWorkPerCustomerMicro);

            long baseBill = cookBill + salonBill;
            if (week <= 1) return baseBill;

            // ZAM TAVANLI: en fazla IKI KAT.
            //
            // Buyume haftada %2,2 ve BILESIK; kampanya sekiz haftalik
            // oldugu icin orada 1,16 kat ediyor ve denge o pencereye
            // gore kuruldu. Ama oyun altmisinci gunde BITMIYOR - docs/08
            // serbest oyuna geciyor - ve orada tavansiz bir ussel,
            // tavanli bir gelirle karsi karsiya kaliyordu:
            //
            //     gun 200  (hafta 28)   1,80 kat
            //     gun 365  (hafta 52)   3,03 kat
            //     gun ~728 (hafta 104)  PowNano long'u TASIYOR
            //
            // Tasma CloseDay'in icinde, durumun bir kismi zaten
            // degistikten SONRA atiyordu: itibar dusmus, stok
            // yaslanmis, deneyim artmis ama asama Evening'e gecmemis.
            // Unity dugme geri cagrisindaki istisnayi yutuyor, yani
            // oyuncu "Gunu Kapat"a basiyor, hicbir sey olmuyor, tekrar
            // basiyor - ve her basista ayni zarar BIR KEZ DAHA
            // uygulaniyordu. Kalici, geri donussuz bir kilit.
            //
            // Tavan hem tasmayi hem de "gelir tavanli, gider tavansiz"
            // asimetrisini kapatiyor. Iki kat, gercekci bir ust sinir:
            // kidem zammi sonsuza kadar bilesik islemez.
            long growthNano = Fx.PowNano(
                Fx.Nano + Fx.BpToNano(cfg.WeeklyXpWageGrowthBp),
                week - 1 < MaxWageGrowthWeeks ? week - 1 : MaxWageGrowthWeeks);
            if (growthNano > 2 * Fx.Nano) growthNano = 2 * Fx.Nano;
            return Fx.MulDiv(baseBill, growthNano, Fx.Nano);
        }

        /// <summary>Bilgi amacli: salonda bir kisinin gunluk ucreti, santi-sikke.</summary>
        public static long SalonDailyWage(EconomyConfig cfg)
        {
            return Fx.MulDiv(cfg.SalonWageNumerator, 1, cfg.SalonWorkPerCustomerMicro);
        }
    }
}
