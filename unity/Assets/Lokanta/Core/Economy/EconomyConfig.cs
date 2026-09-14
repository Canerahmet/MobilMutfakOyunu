using System;

namespace Lokanta.Core.Economy
{
    /// <summary>
    /// Bir personel huyu. docs/14: her personele havuzdan IKI tane dusuyor.
    ///
    /// Tasarim niyeti docs/14'te yazili: "hicbir huy saf iyi veya saf kotu
    /// degil." Bedelsiz gorunen ikisinin (musteriyle iyi anlasan, ekip
    /// moralini yukselten) bedeli havuzda: her birinin bir KOTU IKIZI var
    /// ve ikisi cakisiyor, yani iyi huy secilen bir avantaj degil bir sans.
    /// </summary>
    public sealed class TraitDef
    {
        public string Id { get; }
        public string NameKey { get; }
        /// <summary>Gorev hizina etki, baz puan. +1800 = %18 hizli.</summary>
        public int SpeedBp { get; }
        /// <summary>Servis ettigi masada memnuniyet farki, santi-puan.</summary>
        public int SatisfactionCenti { get; }
        /// <summary>Ucret farki, baz puan.</summary>
        public int WageBp { get; }
        /// <summary>Deneyim kazanim carpani, baz puan. 20000 = iki kat, 0 = hic.</summary>
        public int XpBp { get; }
        /// <summary>Yogun dilimde ek yavaslama, baz puan.</summary>
        public int PeakPenaltyBp { get; }
        /// <summary>Gunun son ceyreginde ek yavaslama, baz puan.</summary>
        public int FatiguePenaltyBp { get; }
        /// <summary>Diger personelin moraline etki.</summary>
        public int MoraleAura { get; }
        /// <summary>Pisirdigi yemegin memnuniyetine etki, baz puan.</summary>
        public int QualityBp { get; }
        /// <summary>Masa toplama hizina etki, baz puan.</summary>
        public int CleanlinessBp { get; }
        public bool PeakImmune { get; }
        public bool FatigueImmune { get; }
        /// <summary>Birlikte olamayacagi huylarin indeksleri.</summary>
        public int[] ConflictsWith { get; private set; }

        /// <summary>
        /// Cakisma listesi yuklemede baglanir: huy adlari once indekse
        /// cevrilmeli, ve o ancak butun huylar okunduktan sonra yapilabilir.
        /// </summary>
        public void BindConflicts(int[] indices)
        {
            ConflictsWith = indices ?? new int[0];
        }

        /// <summary>Bu huy, verilen huyla birlikte olabilir mi.</summary>
        public bool ConflictsWithIndex(int other)
        {
            for (int i = 0; i < ConflictsWith.Length; i++)
                if (ConflictsWith[i] == other) return true;
            return false;
        }

        public TraitDef(string id, string nameKey, int speedBp, int satisfactionCenti,
                        int wageBp, int xpBp, int peakPenaltyBp, int fatiguePenaltyBp,
                        int moraleAura, bool peakImmune, bool fatigueImmune,
                        int qualityBp = 0, int cleanlinessBp = 0)
        {
            QualityBp = qualityBp;
            CleanlinessBp = cleanlinessBp;
            Id = id; NameKey = nameKey;
            SpeedBp = speedBp; SatisfactionCenti = satisfactionCenti;
            WageBp = wageBp; XpBp = xpBp;
            PeakPenaltyBp = peakPenaltyBp; FatiguePenaltyBp = fatiguePenaltyBp;
            MoraleAura = moraleAura;
            PeakImmune = peakImmune; FatigueImmune = fatigueImmune;
            ConflictsWith = new int[0];
        }
    }

    /// <summary>Bir genisleme kademesi. Para santi-sikke.</summary>
    public readonly struct TierConfig
    {
        public readonly int Tables;
        public readonly long Rent;        // haftalik
        public readonly long Upgrade;     // bu kademeye gecis bedeli
        public readonly int StaffCap;

        /// <summary>
        /// Bu kademede itibarin cikabilecegi EN YUKSEK deger, santi-puan.
        ///
        /// Dort masalik bir dukkan semtin konustugu lokanta olamaz. Tavan,
        /// itibari doymus bir eksen olmaktan cikariyor: 25. gunde tepeye
        /// varip kalan 35 gunu platoda gecirmek yerine, buyumek zorunda
        /// kaliyorsun.
        /// </summary>
        public readonly int ReputationCapCenti;

        /// <summary>
        /// Bu kademede lokantanin SAHIP OLDUGU tabak sayisi.
        ///
        /// Tabak sayili ve doniyor: temiz -> kullanimda -> kirli -> temiz.
        /// Temiz tabak bitince asci pisen yemegi cikaramiyor ve servis
        /// duruyor - docs/14'un bulasikci gerekcesi ("tabak biterse servis
        /// durur") bu sayi yuzunden gercek bir darbogaz.
        ///
        /// Kademeyle buyuyor: buyuyen dukkan tabak da alir. Buyumeseydi
        /// on dort masalik bir lokanta dort masalik bir mutfagin
        /// tabagiyla calisirdi ve darbogaz bir mekanik degil bir duvar
        /// olurdu.
        /// </summary>
        public readonly int Plates;

        public TierConfig(int tables, long rent, long upgrade, int staffCap,
                          int reputationCapCenti = 10000, int plates = 0)
        {
            Tables = tables; Rent = rent; Upgrade = upgrade; StaffCap = staffCap;
            ReputationCapCenti = reputationCapCenti;
            // Icerikte yazmiyorsa masa basina alti: eski kayitlar ve
            // testler sifir tabakla kilitlenmesin.
            Plates = plates > 0 ? plates : tables * 6;
        }
    }

    /// <summary>
    /// Cekirdegin ihtiyac duydugu butun ekonomi sabitleri.
    /// Lokanta.Content bunu JSON'dan kurar; cekirdek JSON bilmez.
    ///
    /// Personel iki havuz:
    ///   mutfak  asci, patron giremez (oyuncu patron, sef degil)
    ///   salon   garson + bulasikci + kasiyer, tek is havuzu
    /// </summary>
    public sealed class EconomyConfig
    {
        public long StartingCash { get; private set; }
        public int StartingReputationCenti { get; private set; }
        public int CampaignDays { get; private set; }

        /// <summary>
        /// Bir mevsim kac gun. docs/09: altmis gunluk kampanya dort mevsim,
        /// yani on beser gun. Malzeme fiyatlari mevsime gore oynuyor.
        /// </summary>
        public int SeasonDays { get; private set; }

        // Asagidaki alanlar ICERIKTE yaziliydi ama kodda SABIT kodlanmisti.
        // tools/audit_content.py uc numarali kontrolu bunlari buldu: DTO
        // bagliyor, cekirdege hic ulasmiyor. Bugun ayni degerler, yani
        // davranis degismiyor; ama icerikte bir sayi degistirildiginde
        // artik gercekten degisiyor.

        /// <summary>
        /// Servis gununun uzunlugu, milisaniye. docs/23 1.3: 480.000 ms,
        /// yani 4.800 tick. TimingConfig bunu kullaniyor.
        /// </summary>
        public int ServiceMs { get; private set; }

        /// <summary>
        /// Gun basina patron mudahalesi hakki. docs/02 59: "sinirli sayida
        /// patron mudahalesi hakkin var (gun basina 3-5)".
        ///
        /// Icerikte yaziliydi ve hicbir sey onu zorlamiyordu: oyuncu
        /// sinirsiz mudahale edebiliyordu, yani her kizgin musteri bedava
        /// kurtarilabilirdi.
        /// </summary>
        public int InterventionsPerDay { get; private set; }

        /// <summary>
        /// Cay ikraminin KISI BASINA maliyeti, santi-sikke.
        /// docs/12 3: "porsiyon basina 2 maliyet, bedava verilir".
        /// </summary>
        public long TreatCost { get; private set; }

        /// <summary>
        /// Hal fiyatlarinin gunluk oynama araligi, baz puan. 2500 = %25.
        /// docs/12 3: "ucuz gune denk gelmek sans degil, takip meselesi".
        /// </summary>
        public int PriceVolatilityBp { get; private set; }

        /// <summary>Kira ve maasin odendigi gun araligi. docs/12 2.</summary>
        public int RentDayInterval { get; private set; }

        /// <summary>
        /// Memnuniyetin notr esigi, santi-puan. Bunun ustu itibar
        /// kazandiriyor, alti kaybettiriyor. docs/12 5.5.
        /// </summary>
        public int SatisfactionNeutralCenti { get; private set; }

        /// <summary>
        /// Fiyati piyasanin bu oraninin altina indirmenin faydasi yok,
        /// baz puan. 8500 = %15 altina inmek bir sey kazandirmiyor.
        /// </summary>
        public int UnderpriceFloorBp { get; private set; }

        /// <summary>
        /// Fiyat sapmasinin talebe etkisi. 10000 = birim esneklik
        /// (%10 zam -> %10 az musteri).
        ///
        /// Bu kanal bir zamanlar YOKTU ve oyunun en buyuk acigiydi:
        /// fiyatin tek yolu memnuniyet -> itibar idi, itibar da
        /// kademe tavanina kirpiliyordu, yani tavandaki oyuncu icin
        /// kucuk bir zam bedavaydi. Olculdu: %10 zam yapan bot 27.849
        /// sikke ile bitiriyordu, taban strateji 18.670.
        ///
        /// ICERIKTEN geliyor ki denge araci onu arayabilsin; elle
        /// yazilmis bir sabit olsaydi calibrate.py bu ekseni hic
        /// goremezdi.
        /// </summary>
        public int PriceElasticityBp { get; private set; }

        /// <summary>
        /// Gunluk talebin BEKLENTIDEN sapma araligi, baz puan.
        /// 1000 = gun basina -%10 ile +%10 arasi.
        ///
        /// Talep bir zamanlar TAMAMEN belirlenimciydi: ayni itibar ve
        /// masa sayisindaki her sali birebir ayni sayida musteri
        /// getiriyordu. Sonucu, sabah stok kararinin bir YARGI degil
        /// bir dugme olmasiydi - hal onerisi her zaman tam dogruydu
        /// ve "Stok 8/13 kisiye yetiyor" satiri hicbir zaman
        /// kirmiziya donmuyordu.
        ///
        /// SAPMA YALNIZCA GERCEKLESEN SAYIDA. Tahmin (kadro onerisi,
        /// hal onerisi, beklenen kisi) beklentiyi gostermeye devam
        /// ediyor - yoksa oyuncu yine kesin bilgiye sahip olurdu ve
        /// oynaklik dekor kalirdi.
        /// </summary>
        public int DemandVarianceBp { get; private set; }

        /// <summary>
        /// Fiyatin piyasaya gore TAVANI, baz puan. 25000 = piyasanin
        /// 2,5 katindan pahaliya satilamaz.
        ///
        /// NEDEN VAR: memnuniyet [0, 10000] arasina kirpiliyor ve talep
        /// fiyati hic gormuyor. Yani bir kalemin fiyati, o kalemi alan
        /// musterinin memnuniyetini sifira indirmeye yettigi noktadan
        /// sonra HER EK SIFIR BEDAVA. Olculdu: yan kalemleri 2000 kat
        /// pahalilastiran bir bot 3,4 milyon sikke topladi ve itibar,
        /// memnuniyet, agirlanan kisi sayisi HIC degismedi - simulasyon
        /// o noktadan sonra fiyati gormuyordu.
        ///
        /// Taban zaten vardi (UnderpriceFloorBp); tavanin olmamasi
        /// simetri hatasiydi.
        /// </summary>
        public int OverpriceCeilingBp { get; private set; }

        /// <summary>Kredi geri odemesi anaparanin kaci, baz puan.</summary>
        public int LoanMultiplierBp { get; private set; }

        /// <summary>Kredi taksit sayisi, hafta.</summary>
        public int LoanWeeks { get; private set; }

        /// <summary>Kredi secenekleri, santi-sikke.</summary>
        public long[] LoanOptions { get; private set; }
        public int WeekendDaysPerWeek { get; private set; }

        public int CustomerBasePerTable { get; private set; }
        public int WeekdayMultiplierBp { get; private set; }
        public int WeekendMultiplierBp { get; private set; }

        public int IngredientRateBp { get; private set; }

        public int CookCapacityPerDay { get; private set; }
        public long CookDailyWage { get; private set; }

        /// <summary>Bir musterinin salona yukledigi is, mikro-is-gunu.</summary>
        public int SalonWorkPerCustomerMicro { get; private set; }

        /// <summary>
        /// Sum(workMicro_r * dailyWage_r) salon rolleri uzerinden.
        /// Kisi basi ucreti onceden yuvarlamamak icin pay ayri tutuluyor;
        /// yuvarlama tek seferde, ucret faturasi hesaplanirken yapiliyor.
        /// </summary>
        public long SalonWageNumerator { get; private set; }

        public int OwnerWorkMicro { get; private set; }
        public int WeeklyXpWageGrowthBp { get; private set; }

        // --- Deneyim, docs/14 "Deneyim ve seviye" -----------------------------
        // Calisilan her gun 1 puan, 30 puanda seviye, azami 3 seviye,
        // seviye basina +%10 hiz. Hiz merdiveni icerikten gelir.
        /// <summary>Bir seviye icin gereken calisma gunu. docs/14: 30.</summary>
        // --- Personel huylari ve moral, docs/14 -------------------------------
        private TraitDef[] _traits;

        public int TraitCount { get { return _traits == null ? 0 : _traits.Length; } }
        public TraitDef TraitAt(int i) { return _traits[i]; }

        /// <summary>Yeni personelin morali. docs/14: 70 ile baslar.</summary>
        public int StartingMorale { get; private set; }
        /// <summary>Bunun altinda hiz kaybi ve hata sansi artiyor.</summary>
        public int MoraleLowThreshold { get; private set; }
        /// <summary>Bunun altinda her gun istifa riski var.</summary>
        public int MoraleQuitThreshold { get; private set; }
        /// <summary>Istifa riski, baz puan. docs/14: gunde %10.</summary>
        public int MoraleQuitChanceBp { get; private set; }
        /// <summary>Dusuk moralin hiz cezasi, baz puan. docs/14: %20.</summary>
        public int MoraleSlowPenaltyBp { get; private set; }
        /// <summary>Maas zamaninda odendi.</summary>
        public int MoralePaidDelta { get; private set; }
        /// <summary>Maas gecikti.</summary>
        public int MoraleLateDelta { get; private set; }
        /// <summary>Ust uste yogun gun.</summary>
        public int MoraleBusyDelta { get; private set; }
        /// <summary>Sakin gunun toparlatmasi; baslangic moraline dogru.</summary>
        public int MoraleRecoveryDelta { get; private set; }
        /// <summary>Aday havuzu kac gunde bir yenileniyor. docs/14: uc.</summary>
        public int CandidateRefreshDays { get; private set; }

        /// <summary>
        /// Silinen borcun itibar bedeli, santi-puan. Merdivenin son
        /// basamagi: ekipman da masa da satildiktan sonra kalan borc
        /// siliniyor ve bedeli itibardan aliniyor.
        /// </summary>
        public int DebtWriteOffRepCenti { get; private set; }

        /// <summary>
        /// Mudahalenin memnuniyet katkisi, santi-puan. docs/12 5.4.
        ///
        /// Asil etki artik SABIR uzatmasi; bu iki sayi yalnizca
        /// kucuk bir dokunus. Once tersi olduğu icin mudahale
        /// olculebilir zarardi.
        /// </summary>
        public int AttentionSatisfactionCenti { get; private set; }
        public int TreatSatisfactionCenti { get; private set; }

        /// <summary>Acele ettirilen isin kalan suresinden silinen pay, baz puan.</summary>
        public int RushCutBp { get; private set; }

        /// <summary>
        /// Sabir uzatmasi, oturma-siparis suresinin kati.
        ///
        /// Bir TAKAS ayarliyor: uzatma gitmek uzere olan grubu tutuyor
        /// ama masayi da daha uzun isgal ediyor, yani baskasina hizmet
        /// edilemiyor. Bedava bir iyilik degil.
        /// </summary>
        public int AttentionPatienceMult { get; private set; }
        public int TreatPatienceMult { get; private set; }

        /// <summary>
        /// Mudahale sayilari. Icerikten geliyor (docs/23 8.2).
        ///
        /// Sifir gelen her alan eski koddaki degerine dusuyor: icerigi
        /// eski bir kayit ya da eksik bir dosya, mudahaleyi sessizce
        /// etkisiz birakmasin.
        /// </summary>
        public EconomyConfig WithIntervention(int attentionCenti, int treatCenti,
                                              int rushCutBp, int attentionMult,
                                              int treatMult)
        {
            EconomyConfig c = (EconomyConfig)MemberwiseClone();
            c.AttentionSatisfactionCenti = attentionCenti > 0 ? attentionCenti : 1200;
            c.TreatSatisfactionCenti = treatCenti > 0 ? treatCenti : 900;
            c.RushCutBp = rushCutBp > 0 ? rushCutBp : 4000;
            c.AttentionPatienceMult = attentionMult > 0 ? attentionMult : 3;
            c.TreatPatienceMult = treatMult > 0 ? treatMult : 1;
            return c;
        }

        /// <summary>
        /// Kademeleri degistirilmis bir kopya.
        ///
        /// TESTLER ICIN: itibar tavaninin davranisini sinamak, tavana
        /// DAYANAN bir dukkan gerektiriyor. Gercek icerikte tavan dort
        /// masada 55 ve kucuk bir dukkanin dogal denge noktasi ~34,6 -
        /// yani tavan orada hic baglayici degil. Tavana dayanmak icin
        /// testin harness botu kadar iyi oynamasi gerekirdi, yani
        /// testin icine bir bot yazmak.
        ///
        /// Tavani ICERIKTEN dusurmek dogru cozum: kural ayni kural,
        /// yalnizca gorunur oldugu esik yaklastiriliyor.
        /// </summary>
        public EconomyConfig WithTiers(TierConfig[] tiers)
        {
            if (tiers == null || tiers.Length == 0) return this;
            EconomyConfig c = (EconomyConfig)MemberwiseClone();
            c._tiers = tiers;
            return c;
        }

        public EconomyConfig WithTraits(TraitDef[] traits)
        {
            EconomyConfig c = (EconomyConfig)MemberwiseClone();
            c._traits = traits;
            return c;
        }

        public EconomyConfig WithMorale(int starting, int low, int quit, int quitChanceBp,
                                        int slowPenaltyBp, int paid, int late, int busy,
                                        int recovery)
        {
            EconomyConfig c = (EconomyConfig)MemberwiseClone();
            c.StartingMorale = starting;
            c.MoraleLowThreshold = low;
            c.MoraleQuitThreshold = quit;
            c.MoraleQuitChanceBp = quitChanceBp;
            c.MoraleSlowPenaltyBp = slowPenaltyBp;
            c.MoralePaidDelta = paid;
            c.MoraleLateDelta = late;
            c.MoraleBusyDelta = busy;
            c.MoraleRecoveryDelta = recovery > 0 ? recovery : 2;
            c.CandidateRefreshDays = 3;
            c.DebtWriteOffRepCenti = 2000;
            return c;
        }

        // --- Isimli duzenli musteriler, docs/11 -------------------------------
        /// <summary>Tanistiktan sonra bir gunde ugrama sansi, baz puan.</summary>
        public int RegularVisitChanceBp { get; private set; }
        /// <summary>Sevdigi yemegi bulamayan duzenli musterinin cezasi, santi.</summary>
        public int RegularMissedFavouriteCenti { get; private set; }
        /// <summary>Bunun altinda ayrilirsa bir sure gelmiyor, santi-puan.</summary>
        public int RegularUpsetCenti { get; private set; }
        /// <summary>Kirilinca kac gun gelmiyor.</summary>
        public int RegularAwayDays { get; private set; }

        /// <summary>Duzenli musteri ayarlarini takar. Icerikten geliyor.</summary>
        public EconomyConfig WithRegulars(int visitChanceBp, int missedFavouriteCenti,
                                          int upsetCenti, int awayDays)
        {
            EconomyConfig c = (EconomyConfig)MemberwiseClone();
            c.RegularVisitChanceBp = visitChanceBp > 0 ? visitChanceBp : 4500;
            c.RegularMissedFavouriteCenti = missedFavouriteCenti;
            c.RegularUpsetCenti = upsetCenti;
            c.RegularAwayDays = awayDays > 0 ? awayDays : 3;
            return c;
        }

        public int XpDaysPerLevel { get; private set; }
        /// <summary>Azami seviye. docs/14: 3.</summary>
        public int MaxXpLevel { get; private set; }

        private int[] _cookXpSpeedBp;
        private int[] _salonXpSpeedBp;

        /// <summary>
        /// Seviyenin hiz carpani, baz puan. Merdiven kisaysa son basamak
        /// tekrarlanir; icerik hep MaxXpLevel+1 uzunlugunda olmali ama
        /// eksik merdiven yuzunden dizi disina tasmak istemiyoruz.
        /// </summary>
        public int XpSpeedBp(int level, bool kitchen)
        {
            int[] ladder = kitchen ? _cookXpSpeedBp : _salonXpSpeedBp;
            if (ladder == null || ladder.Length == 0) return 10_000;
            if (level < 0) level = 0;
            if (level >= ladder.Length) level = ladder.Length - 1;
            return ladder[level];
        }

        /// <summary>Calisilan gun sayisindan seviye. docs/14: tavan 3.</summary>
        public int XpLevelOf(int daysWorked)
        {
            if (XpDaysPerLevel <= 0) return 0;
            int level = daysWorked / XpDaysPerLevel;
            return level > MaxXpLevel ? MaxXpLevel : level;
        }

        /// <summary>
        /// Deneyim merdivenlerini takar. Kirk argumanli kuruculara iki
        /// arguman daha eklemek yerine kopya donduruyoruz: o listede sira
        /// hatasi yapmak, derleyicinin yakalayamadigi bir hata turu.
        /// </summary>
        public EconomyConfig WithXpSpeed(int[] cookLadder, int[] salonLadder,
                                         int daysPerLevel, int maxLevel)
        {
            EconomyConfig c = (EconomyConfig)MemberwiseClone();
            c._cookXpSpeedBp = cookLadder;
            c._salonXpSpeedBp = salonLadder;
            c.XpDaysPerLevel = daysPerLevel > 0 ? daysPerLevel : 30;
            c.MaxXpLevel = maxLevel > 0 ? maxLevel : 3;
            return c;
        }

        /// <summary>Ihmal edilirse itibar eriyor. docs/12 5.5: gunde 0,3 puan.</summary>
        public int ReputationDecayPerDayCenti { get; private set; }

        // --- Siparis modeli ---------------------------------------------------
        // Kisi basina bir ANA yemek kesin, yan ve icecek olasilikli.
        // Denge araci bunlar olmadan ortalama fisi cok dusuk hesapliyordu:
        // her musteri menuden esit olasilikla tek kalem seciyor, cogu kola
        // aliyordu. docs/07 kombo mekaniginin taban hali.
        /// <summary>Kisi basina yan yemek olasiligi, baz puan.</summary>
        public int SideChanceBp { get; private set; }
        /// <summary>Kisi basina icecek olasiligi, baz puan.</summary>
        public int DrinkChanceBp { get; private set; }
        /// <summary>
        /// Kisi basina tatli olasiligi, baz puan. Icecekten dusuk:
        /// tatli sonda gelir ve herkes almaz.
        /// </summary>
        public int DessertChanceBp { get; private set; }

        /// <summary>
        /// Musterinin, duydugu ama yapilamayan bir yemegi SORMA olasiligi.
        /// </summary>
        public int AskChanceBp { get; private set; }

        /// <summary>
        /// Sordugu yemegi bulamayan musterinin memnuniyet kaybi, santi-puan.
        /// </summary>
        public int AskMissCenti { get; private set; }

        /// <summary>
        /// Talebin ne kadarinin GERCEKTEN ciroya donustugu, baz puan.
        ///
        /// Kapali form model talebin tamaminin agirlandigini varsayardi.
        /// Simulasyon ayni genisleme takviminde modelin cirosunun %65'ini
        /// uretiyor: sabri biten musteri, tukenen stok, dolan masa.
        /// Kiralar modelin cirosundan cozuldugu icin %35 fazlaydi.
        ///
        /// OLCULEN bir degerdir, secilmis degil. Simulasyon degistikce
        /// yeniden olculmeli.
        /// </summary>
        public int RealisationBp { get; private set; }

        private TierConfig[] _tiers;

        public EconomyConfig(
            long startingCash, int startingReputationCenti, int campaignDays,
            int weekendDaysPerWeek, int customerBasePerTable,
            int weekdayMultiplierBp, int weekendMultiplierBp, int ingredientRateBp,
            int cookCapacityPerDay, long cookDailyWage,
            int salonWorkPerCustomerMicro, long salonWageNumerator,
            int ownerWorkMicro, int weeklyXpWageGrowthBp, TierConfig[] tiers,
            int reputationDecayPerDayCenti = 30,
            int seasonDays = 15, int serviceMs = 480_000, int rentDayInterval = 7,
            int interventionsPerDay = 4, long treatCost = 200,
            int priceVolatilityBp = 2500,
            int satisfactionNeutralCenti = 6000, int underpriceFloorBp = 8500,
            int loanMultiplierBp = 13500, int loanWeeks = 8,
            long[] loanOptions = null, int sideChanceBp = 3000, int drinkChanceBp = 4000,
            int dessertChanceBp = 1800, int askChanceBp = 2500,
            int askMissCenti = 1500, int realisationBp = 10000,
            int overpriceCeilingBp = 25000,
            int priceElasticityBp = 9000,
            int demandVarianceBp = 0)
        {
            if (tiers == null || tiers.Length == 0)
                throw new ArgumentException("En az bir kademe gerekli", nameof(tiers));
            if (customerBasePerTable <= 0)
                throw new ArgumentOutOfRangeException(nameof(customerBasePerTable));
            if (cookCapacityPerDay <= 0)
                throw new ArgumentOutOfRangeException(nameof(cookCapacityPerDay));
            if (salonWorkPerCustomerMicro <= 0)
                throw new ArgumentOutOfRangeException(nameof(salonWorkPerCustomerMicro));

            StartingCash = startingCash;
            StartingReputationCenti = startingReputationCenti;
            CampaignDays = campaignDays;
            SeasonDays = seasonDays > 0 ? seasonDays : 15;
            ServiceMs = serviceMs > 0 ? serviceMs : 480_000;
            InterventionsPerDay = interventionsPerDay > 0 ? interventionsPerDay : 4;
            TreatCost = treatCost > 0 ? treatCost : 200;
            PriceVolatilityBp = priceVolatilityBp >= 0 ? priceVolatilityBp : 2500;
            RentDayInterval = rentDayInterval > 0 ? rentDayInterval : 7;
            SatisfactionNeutralCenti =
                satisfactionNeutralCenti > 0 ? satisfactionNeutralCenti : 6000;
            UnderpriceFloorBp = underpriceFloorBp > 0 ? underpriceFloorBp : 8500;
            PriceElasticityBp = priceElasticityBp > 0 ? priceElasticityBp : 9000;
            DemandVarianceBp = demandVarianceBp < 0 ? 0 : demandVarianceBp;
            OverpriceCeilingBp =
                overpriceCeilingBp > Fx.One ? overpriceCeilingBp : 25000;
            LoanMultiplierBp = loanMultiplierBp > 0 ? loanMultiplierBp : 13500;
            LoanWeeks = loanWeeks > 0 ? loanWeeks : 8;
            LoanOptions = loanOptions != null && loanOptions.Length > 0
                ? loanOptions
                : new long[] { 500_000, 1_000_000, 2_000_000 };
            WeekendDaysPerWeek = weekendDaysPerWeek;
            CustomerBasePerTable = customerBasePerTable;
            WeekdayMultiplierBp = weekdayMultiplierBp;
            WeekendMultiplierBp = weekendMultiplierBp;
            IngredientRateBp = ingredientRateBp;
            CookCapacityPerDay = cookCapacityPerDay;
            CookDailyWage = cookDailyWage;
            SalonWorkPerCustomerMicro = salonWorkPerCustomerMicro;
            SalonWageNumerator = salonWageNumerator;
            OwnerWorkMicro = ownerWorkMicro;
            WeeklyXpWageGrowthBp = weeklyXpWageGrowthBp;
            // Varsayilan: deneyim yok. Icerik WithXpSpeed ile takiyor.
            XpDaysPerLevel = 30;
            MaxXpLevel = 3;
            StartingMorale = 70;
            MoraleLowThreshold = 30;
            MoraleQuitThreshold = 15;
            MoraleQuitChanceBp = 1000;
            MoraleSlowPenaltyBp = 2000;
            MoralePaidDelta = 5;
            MoraleLateDelta = -25;
            MoraleBusyDelta = -3;
            MoraleRecoveryDelta = 2;
            CandidateRefreshDays = 3;
            DebtWriteOffRepCenti = 2000;
            AttentionSatisfactionCenti = 1200;
            TreatSatisfactionCenti = 900;
            RegularVisitChanceBp = 4500;
            RegularMissedFavouriteCenti = 900;
            RegularUpsetCenti = 5000;
            RegularAwayDays = 3;
            _cookXpSpeedBp = null;
            _salonXpSpeedBp = null;
            ReputationDecayPerDayCenti = reputationDecayPerDayCenti;
            SideChanceBp = sideChanceBp;
            DrinkChanceBp = drinkChanceBp;
            DessertChanceBp = dessertChanceBp;
            AskChanceBp = askChanceBp;
            AskMissCenti = askMissCenti;
            RealisationBp = realisationBp;
            _tiers = tiers;
        }

        public int TierCount { get { return _tiers.Length; } }

        public TierConfig TierAt(int index) { return _tiers[index]; }

        /// <summary>Bu masa sayisinin bir kademe karsiligi var mi.</summary>
        public bool HasTierForTables(int tables)
        {
            for (int i = 0; i < _tiers.Length; i++)
                if (_tiers[i].Tables == tables) return true;
            return false;
        }

        /// <summary>
        /// Masa sayisinin kademesi. Tam eslesme yoksa EN YAKIN ALT kademe.
        ///
        /// Once istisna firlatiyordu ve bu yanlis yerdeydi: bir okuyucu
        /// hicbir zaman cokertmemeli. Bozuk bir kayittan gelen gecersiz
        /// masa sayisi, oyuncu Personel ekranini actigi anda oyunu
        /// oldurup kaydi kullanilamaz yapiyordu. Kaydin gecerliligi
        /// YUKLEMEDE denetleniyor (Simulation.Validate); burasi yalnizca
        /// makul bir cevap vermekle yukumlu.
        /// </summary>
        public TierConfig TierForTables(int tables)
        {
            TierConfig best = _tiers[0];
            for (int i = 0; i < _tiers.Length; i++)
            {
                if (_tiers[i].Tables == tables) return _tiers[i];
                if (_tiers[i].Tables <= tables) best = _tiers[i];
            }
            return best;
        }
    }
}
