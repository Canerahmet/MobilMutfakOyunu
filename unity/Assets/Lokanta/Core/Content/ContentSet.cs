using System;

namespace Lokanta.Core.Content
{
    /// <summary>
    /// Cekirdegin gordugu icerik tipleri. Hepsi degismez ve TAMSAYI.
    /// JSON'u Lokanta.Content ayristirir; cekirdek hazir yapiyi alir.
    /// docs/23-cekirdek-sozlesmesi.md 6.1.
    /// </summary>
    public readonly struct DishIngredient
    {
        public readonly int IngredientIndex;   // ContentSet.Ingredients icindeki sira
        public readonly int Grams;

        public DishIngredient(int ingredientIndex, int grams)
        {
            IngredientIndex = ingredientIndex;
            Grams = grams;
        }
    }

    public sealed class DishDef
    {
        public string Id { get; }
        public string NameKey { get; }
        public string Cuisine { get; }
        public string Group { get; }
        public long Price { get; }            // santi-sikke
        public int PrepMs { get; }
        public int StationIndex { get; }
        /// <summary>
        /// 1..3. Karmasik yemek daha pahali ve daha uzun surer; iyi servis
        /// edilince daha cok memnun eder, gec kalinca daha cok cezalandirir.
        ///
        /// Bu alan icerikte vardi ve HICBIR YERDE okunmuyordu; ne fiyati,
        /// ne memnuniyeti, ne kilidi etkiliyordu. tools/audit_content.py
        /// boyle buldu.
        /// </summary>
        public int Complexity { get; }

        /// <summary>Kilit acilmadan once gecmesi gereken en az gun.</summary>
        public int UnlockDay { get; }

        /// <summary>
        /// Kilidin dustugu mevsim, 1-4. UnlockDay'den TURETILIR - iki ayri
        /// gercek degil, ayni gercegin iki gosterimi. Yukleme sirasinda
        /// dogrulaniyor; ilerleme ekrani yemekleri buna gore grupluyor.
        /// </summary>
        public int UnlockSeason { get; }

        /// <summary>
        /// Kilit icin istasyonun en az bu kademesi gerekli. Ekipman satin
        /// almak boylece YALNIZCA hiz degil, MENU aciyor.
        /// </summary>
        public int RequiresStationTier { get; }

        /// <summary>Kilit icin gereken itibar, santi-puan.</summary>
        public int UnlockReputationCenti { get; }
        public DishIngredient[] Ingredients { get; }

        /// <summary>Malzeme maliyeti, santi-sikke. Yuklemede bir kez hesaplanir.</summary>
        public long IngredientCost { get; }

        public DishDef(string id, string nameKey, string cuisine, string group,
                       long price, int prepMs, int stationIndex, int complexity,
                       int unlockDay, DishIngredient[] ingredients, long ingredientCost,
                       int requiresStationTier = 0, int unlockReputationCenti = 0,
                       int unlockSeason = 1)
        {
            Id = id; NameKey = nameKey; Cuisine = cuisine; Group = group;
            Price = price; PrepMs = prepMs; StationIndex = stationIndex;
            Complexity = complexity; UnlockDay = unlockDay;
            UnlockSeason = unlockSeason;
            Ingredients = ingredients; IngredientCost = ingredientCost;
            RequiresStationTier = requiresStationTier;
            UnlockReputationCenti = unlockReputationCenti;
        }
    }

    public sealed class IngredientDef
    {
        public string Id { get; }
        public string NameKey { get; }
        public bool Shared { get; }
        /// <summary>Kilo basina santi-sikke.</summary>
        public long BasePrice { get; }
        public bool Perishable { get; }
        public int SpoilDays { get; }

        /// <summary>
        /// Mevsime gore fiyat carpani, baz puan, dort deger:
        /// ilkbahar, yaz, sonbahar, kis. 10000 = degisiklik yok.
        ///
        /// Icerikte 77 malzemenin 35'inin gercek oynamasi var (domates
        /// yazin %14 ucuz, kisin %20 pahali) ama simulasyon bu alani hic
        /// okumuyordu. tools/audit_content.py bunu boyle buldu.
        /// </summary>
        public int[] SeasonPriceBp { get; }

        /// <summary>
        /// Kalite kademesine gore fiyat carpani, baz puan. Uc deger:
        /// dusuk, standart, yuksek. Standart her zaman 10000.
        /// </summary>
        public int[] QualityPriceBp { get; }

        /// <summary>
        /// Kalite kademesinin memnuniyete etkisi, santi-puan.
        ///
        /// Icerik burada bir tasarim karari tasiyor: en hassas alti
        /// malzemenin HEPSI et (kiyma, tavuk gogsu, balik filetosu, dana
        /// ve kuzu kusbasi, kuzu pirzola). Tuz ile karabiber neredeyse
        /// duyarsiz. Yani ucuza kacmak tuzda serbest, ette felaket.
        ///
        /// Bu yuzden kalite TEK BIR kuresel ayar olabiliyor: sonuc yemeğe
        /// gore kendiliginden degisiyor ve oyuncuya 77 ayri karar
        /// yuklenmiyor (docs/16 dokunus butcesi).
        /// </summary>
        public int[] QualitySatisfactionCenti { get; }

        public IngredientDef(string id, string nameKey, bool shared,
                             long basePrice, bool perishable, int spoilDays,
                             int[] seasonPriceBp = null, int[] qualityPriceBp = null,
                             int[] qualitySatisfactionCenti = null)
        {
            Id = id; NameKey = nameKey; Shared = shared;
            BasePrice = basePrice; Perishable = perishable; SpoilDays = spoilDays;
            SeasonPriceBp = seasonPriceBp;
            QualityPriceBp = qualityPriceBp;
            QualitySatisfactionCenti = qualitySatisfactionCenti;
        }

        /// <summary>Verilen mevsim ve kalitedeki kilo fiyati, santi-sikke.</summary>
        public long PriceAt(int season, int quality)
        {
            long p = PriceInSeason(season);
            if (QualityPriceBp == null || quality < 0 || quality >= QualityPriceBp.Length)
                return p;
            return Core.Fx.MulDiv(p, QualityPriceBp[quality], Core.Fx.One);
        }

        /// <summary>Kalite kademesinin memnuniyete etkisi, santi-puan.</summary>
        public int QualityDelta(int quality)
        {
            if (QualitySatisfactionCenti == null
                || quality < 0 || quality >= QualitySatisfactionCenti.Length) return 0;
            return QualitySatisfactionCenti[quality];
        }

        /// <summary>Verilen mevsimdeki kilo fiyati, santi-sikke.</summary>
        public long PriceInSeason(int season)
        {
            if (SeasonPriceBp == null || season < 0 || season >= SeasonPriceBp.Length)
                return BasePrice;
            return Core.Fx.MulDiv(BasePrice, SeasonPriceBp[season], Core.Fx.One);
        }
    }

    /// <summary>
    /// Bir ekipman basamagi. docs/27 Karar D: yukseltme yemegin pisme
    /// suresine DOKUNMAZ; ya istasyona yuva ekler ya asciyi erken birakir.
    /// Boylece "ekipman alinca her sey hizlanir" enflasyonu kapali kaliyor.
    /// </summary>
    public readonly struct StationTier
    {
        /// <summary>Istasyonun ayni anda alabildigi tabak sayisi.</summary>
        public readonly int Slots;

        /// <summary>
        /// Duvar saatinin yuzde kaci ascinin ELINDE geciyor, baz puan.
        /// Firin 2000: koyar, kapatir, gider. Icecek 10000: bosluk yok.
        /// </summary>
        public readonly int AttendBp;

        /// <summary>Santi-sikke. Kademe 0 bedava ve baslangicta var.</summary>
        public readonly long Price;

        /// <summary>
        /// Bu basamagin ilk gerektigi masa sayisi. 0 ise zorunlu degil,
        /// yalnizca asciyi rahatlatiyor.
        /// </summary>
        public readonly int NeededAtTables;

        public StationTier(int slots, int attendBp, long price, int neededAtTables)
        {
            Slots = slots;
            AttendBp = attendBp;
            Price = price;
            NeededAtTables = neededAtTables;
        }
    }

    /// <summary>Bir duzenli musterinin hikaye sahnesi.</summary>
    public readonly struct StoryBeat
    {
        public readonly int Beat;
        public readonly int RequiresVisits;
        /// <summary>Ortalama memnuniyet esigi, santi-puan.</summary>
        public readonly int RequiresSatisfactionCenti;
        public readonly string TextKey;

        public StoryBeat(int beat, int requiresVisits,
                         int requiresSatisfactionCenti, string textKey)
        {
            Beat = beat;
            RequiresVisits = requiresVisits;
            RequiresSatisfactionCenti = requiresSatisfactionCenti;
            TextKey = textKey;
        }
    }

    /// <summary>
    /// Isimli duzenli musteri. docs/11: arketip binlerce musteri uretir,
    /// isimli musteri TEK BIR KISIDIR ve hep ayni kisidir.
    ///
    /// Davranisi arketipten geliyor (sabir, grup buyuklugu, fiyat
    /// duyarliligi, gelis saati); kendine ait olan uc sey var: sevdigi
    /// yemek, kampanyaya girdigi gun, ve veresiye defterine yazilip
    /// yazilamayacagi.
    /// </summary>
    public sealed class RegularDef
    {
        public string Id { get; }
        public string NameKey { get; }
        public string JobKey { get; }
        /// <summary>Taban arketibin indeksi.</summary>
        public int ArchetypeIndex { get; }
        /// <summary>Sevdigi yemegin indeksi. Menude yoksa hayal kirikligi.</summary>
        public int FavouriteDish { get; }
        public int ArrivesFromDay { get; }
        public bool VeresiyeEligible { get; }
        public StoryBeat[] Story { get; }

        public RegularDef(string id, string nameKey, string jobKey,
                          int archetypeIndex, int favouriteDish,
                          int arrivesFromDay, bool veresiyeEligible,
                          StoryBeat[] story)
        {
            Id = id; NameKey = nameKey; JobKey = jobKey;
            ArchetypeIndex = archetypeIndex; FavouriteDish = favouriteDish;
            ArrivesFromDay = arrivesFromDay; VeresiyeEligible = veresiyeEligible;
            Story = story ?? new StoryBeat[0];
        }
    }

    /// <summary>Mutfagin imza mekaniginin turu. docs/23 8.2 kapali liste.</summary>
    /// <summary>
    /// Yil sonu degerlendirmesinin MUTFAGA OZEL ekseni. docs/08.
    ///
    /// Mekanik kodda (Simulation.Score), SAYI burada: hangi olcu ve o
    /// olcunun tam puan verdigi deger. Fast food'da bir gunun en yuksek
    /// kuver sayisi, Turk mutfaginda veresiye tahsilat orani.
    /// </summary>
    public sealed class ScoreAxisDef
    {
        public string Kind { get; }
        public string NameKey { get; }
        public int Target { get; }

        public ScoreAxisDef(string kind, string nameKey, int target)
        {
            Kind = kind ?? "none";
            NameKey = nameKey ?? "score.signature";
            Target = target > 0 ? target : 1;
        }
    }

    public enum SignatureKind
    {
        None = 0,
        Combo = 1,      // fast food: kombo ve akis
        Credit = 2,     // turk: veresiye ve duzenli musteri
        Courses = 3,    // italyan: masa suresi ve kurs zamanlamasi
        Broth = 4,      // japon: corba suyu ve tukenme
    }

    /// <summary>
    /// Mutfagin imza mekanigi. docs/07: "en onemli satir - satin almanin
    /// yeniden boyama degil BASKA BIR OYUN oldugunu gosteren sey bu."
    ///
    /// Mekanik kodda, sayilar burada. Blok eksikse mutfak yuklenmiyor.
    /// </summary>
    public sealed class SignatureDef
    {
        public SignatureKind Kind { get; }

        /// <summary>
        /// Mekanigin acildigi gun. docs/09: "Imza mekanigi IKINCI MEVSIMIN
        /// BASINDA gelir. Birinci mevsime konursa ogretici yuku cok
        /// agirlasir, cunku oyuncu zaten menu ve fiyati ogreniyor."
        /// </summary>
        public int FromDay { get; }

        // --- Kombo (fast food) --------------------------------------------
        /// <summary>Komboyu olusturan yemek indeksleri: ana, yan, icecek.</summary>
        public int[] ComboDishes { get; }
        /// <summary>Uc kalemin toplamina uygulanan fiyat, baz puan.</summary>
        public int ComboPriceBp { get; }
        /// <summary>Kombo isinin asciyi ne kadar daha uzun bagladigi.</summary>
        public int ComboKitchenLoadBp { get; }

        // --- Veresiye (turk) ----------------------------------------------
        public long CreditMaxPerRegular { get; }
        public int CreditDueDays { get; }
        public int CreditCollectChanceBp { get; }
        public int CreditTeaCollectBonusBp { get; }
        public int CreditDefaultRepPenaltyCenti { get; }
        public int CreditLoyaltyBonusCenti { get; }
        public int CreditTeaCostCenti { get; }

        /// <summary>
        /// GUVEN: musterinin her ziyareti tahsilat sansina bu kadar
        /// ekliyor, baz puan.
        ///
        /// Bu alan olcum sonucu eklendi. Sans herkes icin SABITTI
        /// (8500, cayla 9500) ve odeyen fisin %112'sini odiyordu:
        /// beklenen nakit 0,95 x 1,12 = 1,064 x fis, yani veresiye
        /// PESIN SATISTAN KARLIYDI. Reddetmek icin hicbir gun yoktu ve
        /// mekanik bir defter degil, bedava bir prim dugmesiydi.
        ///
        /// Sans artik KIME yazdigina bagli: yeni tanistigin biri
        /// kotu bir bahis, yillardir gelen biri iyi. Sorunun kendisi
        /// bu - "veresiye acayim mi" degil, "BU ADAMA acayim mi".
        /// </summary>
        public int CreditTrustPerVisitBp { get; }

        /// <summary>Guvenin ekleyebilecegi en yuksek pay, baz puan.</summary>
        public int CreditTrustCapBp { get; }

        /// <summary>
        /// Tahsilat sansinin TAVANI. Tam kesinlik olmamali: risksiz
        /// bir defter yine karar uretmeyen bir prim dugmesidir.
        /// </summary>
        public int CreditChanceCapBp { get; }

        /// <summary>
        /// Tahsil edilen her hesabin TALEBE kalici katkisi, baz puan.
        ///
        /// Bu alan olcum sonucu eklendi. Veresiyenin tek getirisi itibar
        /// oldugunda mekanik ISE YARAMIYORDU: iyi oynayan zaten itibar
        /// tavaninda, yani sadakat primi bir sey satin almiyordu. docs/07
        /// zaten iki sey soyluyor - "sadakati VE itibari yukseltir" - ve
        /// sadakatin karsiligi geri gelen musteridir, tavana dayali bir
        /// puan degil.
        /// </summary>
        public int CreditLoyaltyDemandBp { get; }
        /// <summary>Sadakatin tavani. Sonsuz birikirse veresiye zorunlu olur.</summary>
        public int CreditLoyaltyCapBp { get; }

        /// <summary>
        /// Uygun bir grubun veresiye ISTEME olasiligi, baz puan.
        ///
        /// Mekanigin asil yonu bu. Ilk yazimda veresiye bir prim dugmesiydi
        /// ve olcum reddetti: iyi oynayan zaten itibar tavaninda ve masalari
        /// dolu, yani ne itibar ne talep bir sey satin aliyordu - veresiye
        /// yalnizca nakit kaybettiriyordu. Dogru yon TERSI: musteri ISTIYOR,
        /// vermeyen kaybediyor. Kilitli yemegi soran musteri mekanigi
        /// (docs/34 6) ile ayni fikir.
        /// </summary>
        public int CreditAskChanceBp { get; }
        /// <summary>Isteyen musteriye veresiye acilmazsa memnuniyet cezasi.</summary>
        public int CreditRefusedPenaltyCenti { get; }

        /// <summary>
        /// Hesabini kapatan musterinin USTUNE koydugu pay, baz puan.
        ///
        /// Mekanigin kazanc tarafi burasi. Ilk iki denemede veresiyenin
        /// getirisi ITIBAR ve TALEP idi; ikisi de iyi oynayanda ise
        /// yaramiyor - itibar zaten tavanda, masalar zaten dolu. Kalan
        /// tek gercek getiri PARA: veresiye defterine yazilan adam
        /// hesabini kapatirken fazlasiyla kapatiyor.
        ///
        /// Beklenen deger: tahsilat sansi x (1 + bu pay). Cay ikramiyla
        /// sans yukseldigi icin cay veren kazanir, ayrim gozetmeyen kaybeder.
        /// </summary>
        public int CreditRepayBonusBp { get; }

        public SignatureDef(SignatureKind kind, int fromDay = 1,
                            int[] comboDishes = null, int comboPriceBp = 0,
                            int comboKitchenLoadBp = 0,
                            long creditMaxPerRegular = 0, int creditDueDays = 0,
                            int creditCollectChanceBp = 0,
                            int creditTeaCollectBonusBp = 0,
                            int creditDefaultRepPenaltyCenti = 0,
                            int creditLoyaltyBonusCenti = 0,
                            int creditTeaCostCenti = 0,
                            int creditLoyaltyDemandBp = 0,
                            int creditLoyaltyCapBp = 0,
                            int creditAskChanceBp = 0,
                            int creditRefusedPenaltyCenti = 0,
                            int creditRepayBonusBp = 0,
                            int creditTrustPerVisitBp = 0,
                            int creditTrustCapBp = 0,
                            int creditChanceCapBp = 0)
        {
            Kind = kind;
            FromDay = fromDay < 1 ? 1 : fromDay;
            ComboDishes = comboDishes;
            ComboPriceBp = comboPriceBp;
            ComboKitchenLoadBp = comboKitchenLoadBp;
            CreditMaxPerRegular = creditMaxPerRegular;
            CreditDueDays = creditDueDays;
            CreditCollectChanceBp = creditCollectChanceBp;
            CreditTeaCollectBonusBp = creditTeaCollectBonusBp;
            CreditDefaultRepPenaltyCenti = creditDefaultRepPenaltyCenti;
            CreditLoyaltyBonusCenti = creditLoyaltyBonusCenti;
            CreditTeaCostCenti = creditTeaCostCenti;
            CreditLoyaltyDemandBp = creditLoyaltyDemandBp;
            CreditLoyaltyCapBp = creditLoyaltyCapBp;
            CreditAskChanceBp = creditAskChanceBp;
            CreditRefusedPenaltyCenti = creditRefusedPenaltyCenti;
            CreditRepayBonusBp = creditRepayBonusBp;
            CreditTrustPerVisitBp = creditTrustPerVisitBp;
            CreditTrustCapBp = creditTrustCapBp;
            CreditChanceCapBp = creditChanceCapBp > 0 ? creditChanceCapBp : 10000;
        }
    }

    public sealed class StationDef
    {
        public string Id { get; }
        public string NameKey { get; }
        public StationTier[] Tiers { get; }

        /// <summary>
        /// Paylasilan alti istasyondan biri mi. false ise mutfaga OZEL
        /// adlandirilmis ekipman (tas firin, doner ocagi...): baslangicta
        /// yoktur, masa sayisi yuzunden hicbir zaman ZORUNLU olmaz, yalnizca
        /// menu acar.
        /// </summary>
        public bool Shared { get; }

        public StationDef(string id, string nameKey, StationTier[] tiers,
                          bool shared = true)
        {
            Shared = shared;
            Id = id ?? throw new ArgumentNullException(nameof(id));
            NameKey = nameKey ?? throw new ArgumentNullException(nameof(nameKey));
            Tiers = tiers ?? throw new ArgumentNullException(nameof(tiers));
            if (tiers.Length == 0)
                throw new ArgumentException("istasyonun en az bir basamagi olmali", nameof(tiers));
        }

        public int MaxTier { get { return Tiers.Length - 1; } }
    }

    /// <summary>
    /// Soguk hava kademesi. Bozulabilir malzemenin KENDI raf omrunun
    /// (spoilDays) yuzde kacinin gecerli oldugunu soyluyor.
    ///
    /// Kademe 0'da pay sifir: bozulabilir her sey gece oluyor. docs/12 3
    /// bunu tasarlanmis temel olarak yaziyor; soguk hava o temeli DEGISTIREN
    /// yukseltme, eksigi kapatan bir duzeltme degil.
    /// </summary>
    public readonly struct StorageTier
    {
        /// <summary>spoilDays'in yuzde kaci gecerli, baz puan. 0 = hic.</summary>
        public readonly int KeepBp;
        public readonly long Price;      // santi-sikke

        public StorageTier(int keepBp, long price)
        {
            KeepBp = keepBp;
            Price = price;
        }
    }

    public sealed class StorageDef
    {
        public string NameKey { get; }
        public StorageTier[] Tiers { get; }

        public StorageDef(string nameKey, StorageTier[] tiers)
        {
            NameKey = nameKey ?? throw new ArgumentNullException(nameof(nameKey));
            Tiers = tiers ?? throw new ArgumentNullException(nameof(tiers));
            if (tiers.Length == 0)
                throw new ArgumentException("deponun en az bir basamagi olmali", nameof(tiers));
        }

        public int MaxTier { get { return Tiers.Length - 1; } }
    }

    /// <summary>Servis gununun dort dilimi. docs/12 5.6.</summary>
    public enum DaySlot
    {
        Acilis = 0,
        Ogle = 1,
        OgledenSonra = 2,
        Aksam = 3,
        Count = 4
    }

    public sealed class ArchetypeDef
    {
        public string Id { get; }
        public string NameKey { get; }
        public int TierIndex { get; }          // 0 sik, 1 orta, 2 nadir
        public int Weight { get; }
        public int PatienceMs { get; }
        public int PriceSensitivityBp { get; }
        public int GroupSizeMin { get; }
        public int GroupSizeMax { get; }
        public int ReputationWeightBp { get; }
        public int TipChanceBp { get; }
        /// <summary>Dort dilimin agirligi, toplami 10000.</summary>
        public int[] ArrivalWeightsBp { get; }

        public ArchetypeDef(string id, string nameKey, int tierIndex, int weight,
                            int patienceMs, int priceSensitivityBp,
                            int groupSizeMin, int groupSizeMax,
                            int reputationWeightBp, int tipChanceBp,
                            int[] arrivalWeightsBp)
        {
            Id = id; NameKey = nameKey; TierIndex = tierIndex; Weight = weight;
            PatienceMs = patienceMs; PriceSensitivityBp = priceSensitivityBp;
            GroupSizeMin = groupSizeMin; GroupSizeMax = groupSizeMax;
            ReputationWeightBp = reputationWeightBp; TipChanceBp = tipChanceBp;
            ArrivalWeightsBp = arrivalWeightsBp;
        }
    }

    /// <summary>
    /// Tek bir mutfagin butun icerigi. Yuklemeden sonra degismez.
    /// Diziler sirali ve indeksle adreslenir: kayitta kimlik degil indeks
    /// tutulur, ama indeks icerik surumune bagli oldugu icin kayit dosyasi
    /// kimlikleri de saklar ve yuklemede yeniden esler.
    /// </summary>
    public sealed class ContentSet
    {
        public string Cuisine { get; }

        /// <summary>
        /// Gunun dort diliminin SURESI, baz puan, toplami 10000.
        /// docs/28-zirve-karari.md Karar G: dilimler esit degil ve mutfaga
        /// gore degisiyor. Turk lokantasinin ogle dilimi gunun %48'i.
        /// </summary>
        public int[] SlotDurationsBp { get; }

        /// <summary>
        /// Musterinin yemek yeme suresi, milisaniye. Mutfaga gore degisiyor:
        /// fast food'da kisa, lokantada uzun. Masa devir hizini dogrudan
        /// belirliyor.
        ///
        /// Bu alan icerikte 38.000 yaziyordu ama TimingConfig 45.000
        /// kullaniyordu; ikisi yillardir ayrisikti ve kimse gormemisti.
        /// tools/audit_content.py uc numarali kontrolu boyle buldu.
        /// </summary>
        public int EatMs { get; }
        public IngredientDef[] Ingredients { get; }
        public DishDef[] Dishes { get; }
        public ArchetypeDef[] Archetypes { get; }
        public StationDef[] Stations { get; }

        /// <summary>Soguk hava merdiveni. Icerik yoksa null.</summary>
        public StorageDef Storage { get; }

        /// <summary>
        /// Bu mutfagin ORTALAMA yemek karmasikligi, baz puan (15000 = 1,5).
        ///
        /// Karmasiklik riski MUTLAK degil BAGIL olmali. Fast food'un
        /// ortalamasi 1,50, Turk lokantasinin 2,34: mutlak olcekte Turk
        /// menusunun 17'si "zor" sayiliyor ve iyi oyuncunun itibari 51,6'ya
        /// dusuyordu, fast food'da 99,0 iken. Ayni hata kilit kuralinda da
        /// yapilmisti (bkz. docs/34 5).
        ///
        /// Bagil olcekte her mutfagin kendi ortalamasi notr: ortalamanin
        /// ustundeki yemek risk tasiyor, altindaki rahatlik veriyor.
        /// </summary>
        public int MeanComplexityBp { get; }

        /// <summary>Yemegin, kendi mutfaginin ortalamasina gore karmasikligi.</summary>
        public int RelativeComplexityBp(int dish)
        {
            if (MeanComplexityBp <= 0) return Core.Fx.One;
            return (int)Core.Fx.MulDiv(Dishes[dish].Complexity * Core.Fx.One,
                                       Core.Fx.One, MeanComplexityBp);
        }

        /// <summary>
        /// Menu ROLLERI: hangi yemek gruplari ana, yan, icecek ve tatli
        /// yerine geciyor. Mutfak basina degisiyor.
        ///
        /// docs/13 mutfaga ozel grup adlarini KASITLI tasarlamis: fast
        /// food'da ana/yan, Turk lokantasinda sulu/corba/pilav/izgara/meze.
        /// Simulasyon ise fast food sozlugunu sabit kodlamisti ve ikinci
        /// mutfakta hicbir musteri ana yemek bulamiyordu; sekiz stratejinin
        /// hepsi sifir musteriyle batiyordu.
        /// </summary>
        public string[] MainGroups { get; }
        public string[] SideGroups { get; }
        public string[] DrinkGroups { get; }
        public string[] DessertGroups { get; }

        /// <summary>Mutfagin imza mekanigi. docs/23 8.2: eksikse yuklenmez.</summary>
        public SignatureDef Signature { get; }

        /// <summary>Yil sonu degerlendirmesinin mutfaga ozel ekseni.</summary>
        public ScoreAxisDef ScoreAxis { get; }

        /// <summary>
        /// Personel isim havuzu. Bos olabilir - o zaman arayuz "Asci 1"
        /// gibi sirali adlara duser ve oyun calismaya devam eder.
        /// </summary>
        public string[] StaffNames { get; }

        /// <summary>Isimli duzenli musteriler, gelis gunune gore sirali.</summary>
        public RegularDef[] Regulars { get; }

        public bool IsInRole(string group, string[] role)
        {
            if (group == null || role == null) return false;
            for (int i = 0; i < role.Length; i++)
                if (string.Equals(group, role[i], StringComparison.Ordinal)) return true;
            return false;
        }

        public ContentSet(string cuisine, IngredientDef[] ingredients, DishDef[] dishes,
                          ArchetypeDef[] archetypes, StationDef[] stations,
                          StorageDef storage = null, int[] slotDurationsBp = null,
                          int eatMs = 0,
                          string[] mainGroups = null, string[] sideGroups = null,
                          string[] drinkGroups = null, string[] dessertGroups = null,
                          SignatureDef signature = null,
                          RegularDef[] regulars = null,
                          ScoreAxisDef scoreAxis = null,
                          string[] staffNames = null)
        {
            StaffNames = staffNames ?? new string[0];
            Signature = signature ?? new SignatureDef(SignatureKind.None);
            ScoreAxis = scoreAxis ?? new ScoreAxisDef("none", "score.signature", 1);
            Regulars = regulars ?? new RegularDef[0];
            Storage = storage;
            MainGroups = mainGroups ?? new[] { "ana" };
            SideGroups = sideGroups ?? new[] { "yan" };
            DrinkGroups = drinkGroups ?? new[] { "icecek" };
            DessertGroups = dessertGroups ?? new[] { "tatli" };
            SlotDurationsBp = slotDurationsBp;
            EatMs = eatMs;
            Cuisine = cuisine ?? throw new ArgumentNullException(nameof(cuisine));
            Ingredients = ingredients ?? throw new ArgumentNullException(nameof(ingredients));
            Dishes = dishes ?? throw new ArgumentNullException(nameof(dishes));
            Archetypes = archetypes ?? throw new ArgumentNullException(nameof(archetypes));
            Stations = stations ?? throw new ArgumentNullException(nameof(stations));

            long cx = 0;
            for (int i = 0; i < Dishes.Length; i++) cx += Dishes[i].Complexity;
            MeanComplexityBp = Dishes.Length > 0
                ? (int)(cx * Core.Fx.One / Dishes.Length) : Core.Fx.One;
        }

        public int StationIndexOf(string id)
        {
            for (int i = 0; i < Stations.Length; i++)
                if (string.Equals(Stations[i].Id, id, StringComparison.Ordinal)) return i;
            return -1;
        }

        public int IngredientIndexOf(string id)
        {
            for (int i = 0; i < Ingredients.Length; i++)
                if (string.Equals(Ingredients[i].Id, id, StringComparison.Ordinal)) return i;
            return -1;
        }

        public int DishIndexOf(string id)
        {
            for (int i = 0; i < Dishes.Length; i++)
                if (string.Equals(Dishes[i].Id, id, StringComparison.Ordinal)) return i;
            return -1;
        }

        public int ArchetypeIndexOf(string id)
        {
            for (int i = 0; i < Archetypes.Length; i++)
                if (string.Equals(Archetypes[i].Id, id, StringComparison.Ordinal)) return i;
            return -1;
        }

        /// <summary>Belirli bir gunde acik olan yemeklerin indeksleri.</summary>
        public int UnlockedDishCount(int day)
        {
            int n = 0;
            for (int i = 0; i < Dishes.Length; i++)
                if (Dishes[i].UnlockDay <= day) n++;
            return n;
        }

        /// <summary>
        /// O mevsimde acilan yemek sayisi. docs/09 ilerleme egrisi bunun
        /// uzerine kurulu (6 -> 13 -> 21 -> 27 -> 32) ve ilerleme ekrani
        /// yemekleri mevsime gore grupluyor.
        /// </summary>
        public int DishCountInSeason(int season)
        {
            int n = 0;
            for (int i = 0; i < Dishes.Length; i++)
                if (Dishes[i].UnlockSeason == season) n++;
            return n;
        }
    }
}
