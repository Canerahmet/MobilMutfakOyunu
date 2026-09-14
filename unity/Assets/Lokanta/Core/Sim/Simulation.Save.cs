using System;
using Lokanta.Core.Save;

namespace Lokanta.Core.Sim
{
    /// <summary>
    /// Simulasyonun durum yuruyusu ve komut gunlugu.
    /// docs/23-cekirdek-sozlesmesi.md 6.2 ve 7.
    ///
    /// Write ve Restore AYNI SIRAYI yurumek zorunda. Ikisi yan yana
    /// duruyor ki kayma gorunur olsun. Sira sozlesmedir: degisirse eski
    /// kayitlar okunamaz ve gocurme gerekir.
    /// </summary>
    public sealed partial class Simulation
    {
        /// <summary>
        /// Kayit bicimi surumu. Alan sirasi degisirse artar.
        /// 2: istasyon ekipman kademesi ve pisen isler eklendi.
        /// 3: soguk hava kademesi ve malzeme yasi eklendi.
        /// 4: tatli kalemi eklendi (dorduncu istasyon isi).
        /// 5: sorulan ama yapilamayan yemek eklendi.
        /// 6: malzeme kalitesi eklendi.
        /// 7: patron mudahalesi hakki eklendi.
        /// 8: gunluk hal fiyatlari eklendi.
        /// 9-14: BELGESIZ. Sayi 8'den 14'e cikmis ama liste
        ///       guncellenmemis; goc yazmak isteyen kisi neyin
        ///       degistigini bilemez. Yeni surumler buradan itibaren
        ///       yazilacak.
        /// 15: grubun MUTFAK isi ayri bayraga tasindi (kitchenTask).
        ///     Sabir artik yemek piserken de isliyor.
        ///
        /// GOC KURALI: yeni alanlar IStateReader.Has ile okunur ve
        /// yoksa varsayilanda birakilir. Eksik anahtarda istisna atmak,
        /// yayindan sonraki ilk yamada butun kampanyalari silerdi.
        /// </summary>
        // 16 -> 17: imza ekseni `peakCovers`'tan `comboShare`'e gecti;
        // "peakCovers" alani yerine "mainOrders" + "comboOrders".
        // Ayrica "teaSpend": veresiye cayinin bedeli artik sayiliyor.
        // 20 -> 21: nisanlar (badges/badgesToday/creditEverOpened) ve
        // haftalik karne (weekAxis/weekAxisPrev/weekReportDay).
        public const int SaveVersion = 21;

        // ---- komut gunlugu okuyuculari --------------------------------------
        public int CommandCount { get { return _commandCount; } }
        public Command CommandAt(int i) { return _commandLog[i]; }

        /// <summary>
        /// Gun basindan beri uygulanan komutlar. Kayit dosyasi bunu tasiyor.
        /// </summary>
        public Command[] CopyCommandLog()
        {
            Command[] copy = new Command[_commandCount];
            Array.Copy(_commandLog, copy, _commandCount);
            return copy;
        }

        /// <summary>Durumun bayt bayt ozeti. Determinizm testleri bunu karsilastirir.</summary>
        public ulong StateHash()
        {
            HashStateWriter w = new HashStateWriter();
            Write(w);
            return w.Result;
        }

        // =====================================================================
        // Yazma
        // =====================================================================
        public void Write(IStateWriter w)
        {
            if (w == null) throw new ArgumentNullException(nameof(w));

            w.Begin("header");
            w.Int("version", SaveVersion);
            w.Long("seed", unchecked((long)_masterSeed));
            w.Long("tick", _tickIndex);
            w.Int("day", _day);
            w.Int("phase", (int)_phase);
            w.Int("serviceTick", _serviceTick);
            w.Str("cuisine", _content.Cuisine);
            w.End();

            w.Begin("restaurant");
            w.Int("tables", _tableCount);
            w.Int("reputationCenti", _reputationCenti);
            w.Int("reputationOverflow", _reputationOverflowCenti);
            w.Int("badges", _badges);
            w.Int("badgesToday", _badgesToday);
            w.Bool("creditEverOpened", _creditEverOpened);
            w.Int("weekReportDay", _weekReportDay);
            w.IntArray("weekAxis", _weekAxis, SeasonScore.AxisCount);
            w.IntArray("weekAxisPrev", _weekAxisPrev, SeasonScore.AxisCount);
            w.Long("cash", _cash);
            w.Int("cooks", _cooks);
            w.Int("salon", _salon);
            w.IntArray("cookXp", _cookXpDays, MaxServers);
            w.IntArray("salonXp", _salonXpDays, MaxServers);
            w.IntArray("cookName", _cookName, MaxServers);
            w.IntArray("salonName", _salonName, MaxServers);
            w.IntArray("cookTraitA", _cookTraitA, MaxServers);
            w.IntArray("cookTraitB", _cookTraitB, MaxServers);
            w.IntArray("salonTraitA", _salonTraitA, MaxServers);
            w.IntArray("salonTraitB", _salonTraitB, MaxServers);
            w.IntArray("cookMorale", _cookMorale, MaxServers);
            w.IntArray("salonMorale", _salonMorale, MaxServers);
            w.Int("busyStreak", _busyStreak);
            w.Int("candDay", _candDay);
            w.IntArray("candA", _candTraitA, CandidateSlots * 2);
            w.IntArray("candB", _candTraitB, CandidateSlots * 2);
            w.End();

            w.Begin("rng");
            WriteRng(w, "arrival", _rngArrival);
            WriteRng(w, "archetype", _rngArchetype);
            WriteRng(w, "order", _rngOrder);
            WriteRng(w, "staffError", _rngStaffError);
            WriteRng(w, "event", _rngEvent);
            WriteRng(w, "name", _rngName);
            WriteRng(w, "market", _rngMarket);
            WriteRng(w, "credit", _rngCredit);
            WriteRng(w, "regular", _rngRegular);
            WriteRng(w, "hiring", _rngHiring);
            w.End();

            // Imza mekanigi durumu. Veresiye defteri kaydin parcasi:
            // acik hesaplar gunler sonra kapaniyor.
            w.Begin("signature");
            w.Int("comboOn", _comboOn ? 1 : 0);
            w.Int("creditLoyaltyBp", _creditLoyaltyBp);
            w.Int("tabCount", _tabCount);
            w.LongArray("tabAmount", _tabAmount, MaxTabs);
            w.IntArray("tabDueDay", _tabDueDay, MaxTabs);
            w.IntArray("tabTea", _tabTea, MaxTabs);
            w.IntArray("tabRegular", _tabRegular, MaxTabs);
            w.BoolArray("pCredit", _pCredit, MaxParties);
            w.BoolArray("pTea", _pTea, MaxParties);
            w.BoolArray("pCombo", _pCombo, MaxParties);
            w.BoolArray("pAsksCredit", _pAsksCredit, MaxParties);
            w.IntArray("pRegular", _pRegular, MaxParties);
            w.BoolArray("pMissedFav", _pMissedFavourite, MaxParties);
            w.IntArray("pServer", _pServer, MaxParties);
            w.IntArray("pCook", _pCook, MaxParties);

            // TABAK DONGUSU. Degismez: temiz + kullanimda + kirli = toplam.
            w.Int("platesClean", _platesClean);
            w.Int("platesDirty", _platesDirty);
            w.Int("platesInUse", _platesInUse);
            w.Int("dishwashers", _dishwashers);
            w.IntArray("tablePlates", _tablePlates, MaxTables);
            w.IntArray("pPlates", _pPlates, MaxParties);
            w.BoolArray("pCooked", _pCooked, MaxParties);
            w.BoolArray("pAttended", _pAttended, MaxParties);
            w.IntArray("regVisits", _regVisits, MaxRegulars);
            w.LongArray("regSatSum", _regSatSum, MaxRegulars);
            w.IntArray("regBeat", _regBeat, MaxRegulars);
            w.IntArray("regAway", _regAwayDays, MaxRegulars);
            w.BoolArray("regComing", _regComing, MaxRegulars);
            w.End();

            w.Begin("menu");
            w.LongArray("price", _dishPrice, _dishPrice.Length);
            w.BoolArray("onMenu", _dishOnMenu, _dishOnMenu.Length);
            w.End();

            w.Begin("stock");
            w.IntArray("grams", _stockGrams, _stockGrams.Length);
            w.Int("stockOuts", _stockOutEvents);
            w.End();

            w.Begin("tables");
            w.IntArray("party", _tableParty, MaxTables);
            w.BoolArray("dirty", _tableDirty, MaxTables);
            w.End();

            w.Begin("parties");
            w.Int("count", _partyCount);
            w.BoolArray("active", _pActive, MaxParties);
            w.IntArray("archetype", _pArchetype, MaxParties);
            w.IntArray("dishMain", _pDishMain, MaxParties);
            w.IntArray("dishSide", _pDishSide, MaxParties);
            w.IntArray("dishDrink", _pDishDrink, MaxParties);
            w.IntArray("dishDessert", _pDishDessert, MaxParties);
            w.IntArray("askedDish", _pAskedDish, MaxParties);
            w.IntArray("size", _pSize, MaxParties);
            w.IntArray("stage", StageAsInt(), MaxParties);
            w.IntArray("table", _pTable, MaxParties);
            w.IntArray("patienceLeft", _pPatienceLeftMs, MaxParties);
            w.IntArray("patienceTotal", _pPatienceTotalMs, MaxParties);
            w.IntArray("waited", _pWaitedMs, MaxParties);
            w.IntArray("eatLeft", _pEatLeftMs, MaxParties);
            w.IntArray("satisfaction", _pSatisfactionCenti, MaxParties);
            w.IntArray("bonus", _pBonusCenti, MaxParties);
            w.BoolArray("inTask", _pInTask, MaxParties);
            w.BoolArray("kitchenTask", _pKitchenTask, MaxParties);
            w.BoolArray("warned", _pWarned, MaxParties);
            w.End();

            w.Begin("work");
            w.IntArray("salonKind", TaskAsInt(_salonTaskKind), MaxServers);
            w.IntArray("salonTarget", _salonTaskTarget, MaxServers);
            w.IntArray("salonLeft", _salonTaskLeftMs, MaxServers);
            w.IntArray("kitchenKind", TaskAsInt(_kitchenTaskKind), MaxServers);
            w.IntArray("kitchenTarget", _kitchenTaskTarget, MaxServers);
            w.IntArray("kitchenLeft", _kitchenTaskLeftMs, MaxServers);
            w.End();

            // Ekipman ve pisen isler. Yuva sayaci TUREVDIR: _jobState'ten
            // yeniden kuruluyor, yazilmiyor. Iki yerde tutulan bir sayi
            // kaydin bozulabilecegi fazladan bir yer demek.
            w.Begin("stations");
            w.Int("storageTier", _storageTier);
            w.Int("quality", _quality);
            w.Int("interventionsLeft", _interventionsLeft);
            w.IntArray("market", _marketBp, _marketBp.Length);
            w.IntArray("stockQuality", _stockQualityCenti, _stockQualityCenti.Length);
            w.IntArray("stockAge", _stockAgeDays, _stockAgeDays.Length);
            w.IntArray("tier", _stationTier, _stationTier.Length);
            w.IntArray("jobStation", _jobStation, _jobStation.Length);
            w.IntArray("jobMs", _jobMs, _jobMs.Length);
            w.IntArray("jobPlates", _jobPlates, _jobPlates.Length);
            w.IntArray("jobSlots", _jobSlots, _jobSlots.Length);
            w.IntArray("jobState", _jobState, _jobState.Length);
            w.IntArray("jobsLeft", _pJobsLeft, MaxParties);
            w.End();

            w.Begin("arrivals");
            w.Int("count", _arrCount);
            w.Int("next", _arrNext);
            w.IntArray("tick", _arrTick, MaxParties);
            w.IntArray("archetype", _arrArchetype, MaxParties);
            w.IntArray("size", _arrSize, MaxParties);
            w.IntArray("regular", _arrRegular, MaxParties);
            w.End();

            w.Begin("day");
            w.Int("servedParties", _servedParties);
            w.Int("servedPeople", _servedPeople);
            w.Int("angryParties", _angryParties);
            w.Int("turnedAway", _turnedAwayParties);
            w.Long("revenue", _revenue);
            w.Long("ingredientCost", _ingredientCost);
            w.Long("satisfactionSum", _satisfactionSum);
            w.Long("reputationDelta", _reputationDeltaMicro);

            // BUGUNUN UCRETI, KIRASI VE ZAYIATI DA YAZILIYOR.
            //
            // Uculu de aksam raporunun icinde ve NetProfit'i belirliyor;
            // kaydedilmedikleri icin yukleme sonrasi rapor YALAN
            // SOYLUYORDU: yedinci gunu (kira gunu) kapat, telefonu
            // kilitle, geri don - "Gunun kari" haftanin en buyuk
            // giderini yok sayip buyuk bir arti gosteriyor, kasadaki
            // sayi ise dusmus.
            //
            // Bu, DayReport.WageCost'un yorumunda anlatilan hatanin
            // (oyunun temel gerilimi hicbir yerde gorunmuyordu)
            // kayit yoluyla aynen geri gelmesiydi.
            w.Long("dayWages", _dayWages);
            w.Long("dayRent", _dayRent);
            w.Long("daySpoiled", _daySpoiled);

            // Kampanya boyunca ciro: her yuklemede sifirlaniyordu.
            w.Long("revenueAll", _revenueAll);
            w.End();

            w.Begin("finance");
            w.Long("wagesPaid", _weeklyWagesPaid);
            w.Long("rentPaid", _weeklyRentPaid);
            w.Int("firstDebtDay", _firstDebtDay);

            // Yil sonu degerlendirmesinin gecmisi. Kayda giriyor cunku
            // puan GECMISE bakiyor: zirve kuver, tahsilat orani ve
            // merdivene kac kez inildigi bir gunun degil butun sezonun
            // ozeti. Kaydedilmezse yuklenen bir oyun gecmissiz kaliyor.
            w.Int("debtRungs", _debtRungs);
            w.Long("spoiledValue", _spoiledValue);
            w.Long("ingredientSpend", _ingredientSpend);
            w.Long("rescueValue", _rescueValue);
            w.Long("loanTaken", _loanTaken);
            w.Long("loanRepaidAll", _loanRepaidAll);
            w.Long("equipmentSpend", _equipmentSpend);
            w.Long("expansionSpend", _expansionSpend);
            // IMZA EKSENININ SAYACLARI.
            //
            // Yil sonu puani GECMISE bakiyor ve gecmis yalnizca
            // biriktirilirse var: kaydedilmezse bir oyuncunun altmis
            // gunluk kombo kullanimi, tek bir yuklemede silinir ve
            // puan sessizce yanlis cikar. (Once burada `peakCovers`
            // duruyordu; eksen komboyu olcmeye gecince o alan silindi.)
            w.Int("mainOrders", _mainOrders);
            w.Int("comboOrders", _comboOrders);
            w.Long("teaSpend", _teaSpend);
            w.Long("creditIssued", _creditIssued);
            w.Long("creditCollected", _creditCollected);
            w.Bool("seasonScored", _seasonScored);
            w.Long("loanInstallment", _loanInstallment);
            w.Int("loanWeeksLeft", _loanWeeksLeft);
            w.Long("loanRepaid", _loanTotalRepaid);
            w.End();
        }

        // =====================================================================
        // Okuma. Write ile AYNI SIRA.
        // =====================================================================
        public void Restore(IStateReader r)
        {
            if (r == null) throw new ArgumentNullException(nameof(r));

            r.Begin("header");
            int version = r.Int("version");
            if (version != SaveVersion)
                throw new InvalidOperationException(
                    "Kayit surumu " + version + ", beklenen " + SaveVersion
                    + ". Gocurme gerekiyor.");
            r.Long("seed");                     // tohum kurucuda verildi
            _tickIndex = r.Long("tick");
            _day = r.Int("day");
            _phase = (DayPhase)r.Int("phase");
            _serviceTick = r.Int("serviceTick");
            string cuisine = r.Str("cuisine");
            if (!string.Equals(cuisine, _content.Cuisine, StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "Kayit mutfagi '" + cuisine + "', yuklenen '" + _content.Cuisine + "'");
            r.End();

            r.Begin("restaurant");
            _tableCount = r.Int("tables");
            _reputationCenti = r.Int("reputationCenti");
            _reputationOverflowCenti = r.Int("reputationOverflow");
            _badges = r.Int("badges");
            _badgesToday = r.Int("badgesToday");
            _creditEverOpened = r.Bool("creditEverOpened");
            _weekReportDay = r.Int("weekReportDay");
            r.IntArray("weekAxis", _weekAxis, SeasonScore.AxisCount);
            r.IntArray("weekAxisPrev", _weekAxisPrev, SeasonScore.AxisCount);
            _cash = r.Long("cash");
            _cooks = r.Int("cooks");
            _salon = r.Int("salon");
            r.IntArray("cookXp", _cookXpDays, MaxServers);
            r.IntArray("salonXp", _salonXpDays, MaxServers);
            r.IntArray("cookName", _cookName, MaxServers);
            r.IntArray("salonName", _salonName, MaxServers);
            r.IntArray("cookTraitA", _cookTraitA, MaxServers);
            r.IntArray("cookTraitB", _cookTraitB, MaxServers);
            r.IntArray("salonTraitA", _salonTraitA, MaxServers);
            r.IntArray("salonTraitB", _salonTraitB, MaxServers);
            r.IntArray("cookMorale", _cookMorale, MaxServers);
            r.IntArray("salonMorale", _salonMorale, MaxServers);
            _busyStreak = r.Int("busyStreak");
            _candDay = r.Int("candDay");
            r.IntArray("candA", _candTraitA, CandidateSlots * 2);
            r.IntArray("candB", _candTraitB, CandidateSlots * 2);
            r.End();

            r.Begin("rng");
            _rngArrival = ReadRng(r, "arrival");
            _rngArchetype = ReadRng(r, "archetype");
            _rngOrder = ReadRng(r, "order");
            _rngStaffError = ReadRng(r, "staffError");
            _rngEvent = ReadRng(r, "event");
            _rngName = ReadRng(r, "name");
            _rngMarket = ReadRng(r, "market");
            _rngCredit = ReadRng(r, "credit");
            _rngRegular = ReadRng(r, "regular");
            _rngHiring = ReadRng(r, "hiring");
            r.End();

            r.Begin("signature");
            _comboOn = r.Int("comboOn") != 0;
            _creditLoyaltyBp = r.Int("creditLoyaltyBp");
            _tabCount = r.Int("tabCount");
            r.LongArray("tabAmount", _tabAmount, MaxTabs);
            r.IntArray("tabDueDay", _tabDueDay, MaxTabs);
            r.IntArray("tabTea", _tabTea, MaxTabs);
            r.IntArray("tabRegular", _tabRegular, MaxTabs);
            r.BoolArray("pCredit", _pCredit, MaxParties);
            r.BoolArray("pTea", _pTea, MaxParties);
            r.BoolArray("pCombo", _pCombo, MaxParties);
            r.BoolArray("pAsksCredit", _pAsksCredit, MaxParties);
            r.IntArray("pRegular", _pRegular, MaxParties);
            r.BoolArray("pMissedFav", _pMissedFavourite, MaxParties);
            r.IntArray("pServer", _pServer, MaxParties);
            r.IntArray("pCook", _pCook, MaxParties);

            // TABAK DONGUSU. Eski kayitta yok: o zaman butun tabaklar
            // temiz sayiliyor - Has() ile soruluyor, cunku eksik alani
            // sifir okumak lokantayi tabaksiz birakir ve servis hic
            // baslamazdi.
            if (r.Has("platesClean"))
            {
                _platesClean = r.Int("platesClean");
                _platesDirty = r.Int("platesDirty");
                _platesInUse = r.Int("platesInUse");
                _dishwashers = r.Int("dishwashers");
                r.IntArray("tablePlates", _tablePlates, MaxTables);
                r.IntArray("pPlates", _pPlates, MaxParties);
                r.BoolArray("pCooked", _pCooked, MaxParties);
            r.BoolArray("pAttended", _pAttended, MaxParties);
            }
            else
            {
                _platesClean = _economy.TierForTables(_tableCount).Plates;
                _platesDirty = 0;
                _platesInUse = 0;
                _dishwashers = 0;
                for (int i = 0; i < MaxTables; i++) _tablePlates[i] = 0;
                for (int i = 0; i < MaxParties; i++) { _pPlates[i] = 0; _pCooked[i] = false; }
            }
            r.IntArray("regVisits", _regVisits, MaxRegulars);
            r.LongArray("regSatSum", _regSatSum, MaxRegulars);
            r.IntArray("regBeat", _regBeat, MaxRegulars);
            r.IntArray("regAway", _regAwayDays, MaxRegulars);
            r.BoolArray("regComing", _regComing, MaxRegulars);
            r.End();

            r.Begin("menu");
            r.LongArray("price", _dishPrice, _dishPrice.Length);
            r.BoolArray("onMenu", _dishOnMenu, _dishOnMenu.Length);
            r.End();

            r.Begin("stock");
            r.IntArray("grams", _stockGrams, _stockGrams.Length);
            _stockOutEvents = r.Int("stockOuts");
            r.End();

            r.Begin("tables");
            r.IntArray("party", _tableParty, MaxTables);
            r.BoolArray("dirty", _tableDirty, MaxTables);
            r.End();

            r.Begin("parties");
            _partyCount = r.Int("count");
            r.BoolArray("active", _pActive, MaxParties);
            r.IntArray("archetype", _pArchetype, MaxParties);
            r.IntArray("dishMain", _pDishMain, MaxParties);
            r.IntArray("dishSide", _pDishSide, MaxParties);
            r.IntArray("dishDrink", _pDishDrink, MaxParties);
            r.IntArray("dishDessert", _pDishDessert, MaxParties);
            r.IntArray("askedDish", _pAskedDish, MaxParties);
            r.IntArray("size", _pSize, MaxParties);
            int[] stages = new int[MaxParties];
            r.IntArray("stage", stages, MaxParties);
            for (int i = 0; i < MaxParties; i++) _pStage[i] = (CustomerStage)stages[i];
            r.IntArray("table", _pTable, MaxParties);
            r.IntArray("patienceLeft", _pPatienceLeftMs, MaxParties);
            r.IntArray("patienceTotal", _pPatienceTotalMs, MaxParties);
            r.IntArray("waited", _pWaitedMs, MaxParties);
            r.IntArray("eatLeft", _pEatLeftMs, MaxParties);
            r.IntArray("satisfaction", _pSatisfactionCenti, MaxParties);
            r.IntArray("bonus", _pBonusCenti, MaxParties);
            r.BoolArray("inTask", _pInTask, MaxParties);

            // ESKI KAYITTA YOK: sabir mutfak isini de kapsiyordu, ayri
            // bayrak 15. surumde geldi. Eksikse varsayilan (false)
            // kaliyor - yani eski kayit acilir ve o gun biraz daha
            // kolay gecer. Istisna atmak yerine bu.
            if (r.Has("kitchenTask"))
                r.BoolArray("kitchenTask", _pKitchenTask, MaxParties);
            else
                for (int i = 0; i < MaxParties; i++) _pKitchenTask[i] = false;
            r.BoolArray("warned", _pWarned, MaxParties);
            r.End();

            r.Begin("work");
            int[] kinds = new int[MaxServers];
            r.IntArray("salonKind", kinds, MaxServers);
            for (int i = 0; i < MaxServers; i++) _salonTaskKind[i] = (TaskKind)kinds[i];
            r.IntArray("salonTarget", _salonTaskTarget, MaxServers);
            r.IntArray("salonLeft", _salonTaskLeftMs, MaxServers);
            r.IntArray("kitchenKind", kinds, MaxServers);
            for (int i = 0; i < MaxServers; i++) _kitchenTaskKind[i] = (TaskKind)kinds[i];
            r.IntArray("kitchenTarget", _kitchenTaskTarget, MaxServers);
            r.IntArray("kitchenLeft", _kitchenTaskLeftMs, MaxServers);
            r.End();

            r.Begin("stations");
            _storageTier = r.Int("storageTier");
            _quality = r.Int("quality");
            _interventionsLeft = r.Int("interventionsLeft");
            r.IntArray("market", _marketBp, _marketBp.Length);
            r.IntArray("stockQuality", _stockQualityCenti, _stockQualityCenti.Length);
            r.IntArray("stockAge", _stockAgeDays, _stockAgeDays.Length);
            r.IntArray("tier", _stationTier, _stationTier.Length);
            r.IntArray("jobStation", _jobStation, _jobStation.Length);
            r.IntArray("jobMs", _jobMs, _jobMs.Length);
            r.IntArray("jobPlates", _jobPlates, _jobPlates.Length);
            r.IntArray("jobSlots", _jobSlots, _jobSlots.Length);
            r.IntArray("jobState", _jobState, _jobState.Length);
            r.IntArray("jobsLeft", _pJobsLeft, MaxParties);
            r.End();

            // Yuva sayaci turev: pisen islerden yeniden sayiliyor.
            for (int i = 0; i < _stationBusy.Length; i++) _stationBusy[i] = 0;
            for (int j = 0; j < _jobStation.Length; j++)
                if (_jobState[j] == 1 && _jobStation[j] >= 0)
                    _stationBusy[_jobStation[j]] += _jobSlots[j];

            r.Begin("arrivals");
            _arrCount = r.Int("count");
            _arrNext = r.Int("next");
            r.IntArray("tick", _arrTick, MaxParties);
            r.IntArray("archetype", _arrArchetype, MaxParties);
            r.IntArray("size", _arrSize, MaxParties);
            r.IntArray("regular", _arrRegular, MaxParties);
            r.End();

            r.Begin("day");
            _servedParties = r.Int("servedParties");
            _servedPeople = r.Int("servedPeople");
            _angryParties = r.Int("angryParties");
            _turnedAwayParties = r.Int("turnedAway");
            _revenue = r.Long("revenue");
            _ingredientCost = r.Long("ingredientCost");
            _dayWages = r.Long("dayWages");
            _dayRent = r.Long("dayRent");
            _daySpoiled = r.Long("daySpoiled");
            _revenueAll = r.Long("revenueAll");
            _satisfactionSum = r.Long("satisfactionSum");
            _reputationDeltaMicro = r.Long("reputationDelta");
            r.End();

            r.Begin("finance");
            _weeklyWagesPaid = r.Long("wagesPaid");
            _weeklyRentPaid = r.Long("rentPaid");
            _firstDebtDay = r.Int("firstDebtDay");
            _debtRungs = r.Int("debtRungs");
            _spoiledValue = r.Long("spoiledValue");
            _ingredientSpend = r.Long("ingredientSpend");
            _rescueValue = r.Long("rescueValue");
            _loanTaken = r.Long("loanTaken");
            _loanRepaidAll = r.Long("loanRepaidAll");
            _equipmentSpend = r.Long("equipmentSpend");
            _expansionSpend = r.Long("expansionSpend");
            _mainOrders = r.Int("mainOrders");
            _comboOrders = r.Int("comboOrders");
            _teaSpend = r.Long("teaSpend");
            _creditIssued = r.Long("creditIssued");
            _creditCollected = r.Long("creditCollected");
            _seasonScored = r.Bool("seasonScored");
            _loanInstallment = r.Long("loanInstallment");
            _loanWeeksLeft = r.Int("loanWeeksLeft");
            _loanTotalRepaid = r.Long("loanRepaid");
            r.End();

            // Gunluk yuklemeden sonra ayri tekrar oynatiliyor.
            _commandCount = 0;
            _events.Clear();

            Validate();

            // DUYURULMUS YEMEKLER, DURUMDAN TURETILIYOR.
            //
            // _dishWasUnlocked kaydedilmiyordu ve kurucu onu BIRINCI
            // GUNUN durumuyla dolduruyordu. Otuzuncu gunden bir kayit
            // acip "Ertesi Gun"e basinca, aradaki butun yemekler yeniden
            // "acildi" diye duyuruluyor: her biri icin bir bildirim ve
            // bir seviye atlama sesi. Kurucunun yorumunun birinci gun
            // icin engelledigi sey, yukleme yolundan geri geliyordu.
            //
            // Diziyi kaydetmek yerine turetmek daha dogru: "su anda acik
            // olan her sey duyurulmus sayilir" tam olarak istenen anlam,
            // ve kayit bicimini buyutmuyor. Validate'ten SONRA, cunku
            // Unlocked() gecerli bir duruma ihtiyac duyuyor.
            for (int i = 0; i < _dishWasUnlocked.Length; i++)
                _dishWasUnlocked[i] = Unlocked(i);
        }

        /// <summary>
        /// Yuklenen durumun MANTIKLI oldugunu dogrular.
        ///
        /// Neden gerekli: kayit dosyasi oyuncunun cihazinda, duz metin,
        /// sifresiz ve saglama toplamsiz duruyor. Tek bayti bozulmus ama
        /// hala gecerli JSON olan bir dosya - "tables": 5 gibi - sessizce
        /// kabul ediliyordu. Oyun aciliyor, salon ciziliyor, sonra oyuncu
        /// Personel ekranini actiginda ya da gunu kapattiginda
        /// TierForTables(5) istisna firlatiyordu: kayit "aciliyor ama
        /// oynanamiyor" durumuna dusuyor ve silmekten baska care kalmiyor.
        ///
        /// Burada atilan istisna kayit katmaninda yakalaniyor ve yuva
        /// "bozuk" gosteriliyor - yani hata OYUNCUYA, oyunun icine
        /// girmeden once soyleniyor.
        /// </summary>
        private void Validate()
        {
            Check(_day >= 1, "gun", _day);
            Check(_phase >= DayPhase.Morning && _phase <= DayPhase.Evening,
                  "asama", (int)_phase);
            Check(_serviceTick >= 0, "servis sayaci", _serviceTick);

            Check(_tableCount >= 1 && _tableCount <= MaxTables, "masa", _tableCount);
            Check(_economy.HasTierForTables(_tableCount), "masa kademesi", _tableCount);

            Check(_cooks >= 0 && _cooks <= MaxServers, "asci", _cooks);
            Check(_salon >= 0 && _salon <= MaxServers, "salon kadrosu", _salon);
            Check(_partyCount >= 0 && _partyCount <= MaxParties, "grup", _partyCount);
            Check(_tabCount >= 0 && _tabCount <= _tabAmount.Length, "veresiye", _tabCount);

            Check(_reputationCenti >= 0 && _reputationCenti <= 10000,
                  "itibar", _reputationCenti);

            // BIR FAZLAYDI. MaxTier = Tiers.Length - 1, yani gecerli
            // en ust kademe Tiers.Length-1. "<=" yazmak, dizinin bir
            // sonrasini gecerli sayiyordu ve o deger StationSlots ile
            // StationAttendBp icinde servis ORTASINDA patliyordu -
            // yani kayit "saglam" gorunup oyun icinde cokuyordu.
            // Validate tam olarak bunu engellemek icin var.
            for (int i = 0; i < _stationTier.Length; i++)
                Check(_stationTier[i] >= 0
                      && _stationTier[i] < _content.Stations[i].Tiers.Length,
                      "istasyon kademesi", _stationTier[i]);

            // Soguk hava kademesi hic bakilmiyordu: bozuk bir deger
            // StorageKeepBp() icinde, her CloseDay'de cokuyordu.
            if (_content.Storage != null)
                Check(_storageTier >= 0 && _storageTier < _content.Storage.Tiers.Length,
                      "soguk hava kademesi", _storageTier);

            Check(_quality >= 0 && _quality < QualityCount, "kalite", _quality);

            for (int i = 0; i < _pTable.Length; i++)
                Check(_pTable[i] >= -1 && _pTable[i] < MaxTables, "grup masasi", _pTable[i]);

            // MASA -> GRUP eslemesi. Gorunum katmani her karede
            // TableStage(t) cagiriyor ve o _pStage[_tableParty[t]]
            // okuyor: bozuk bir deger, salon cizilirken HER KAREDE
            // patliyor.
            for (int t = 0; t < _tableParty.Length; t++)
                Check(_tableParty[t] >= -1 && _tableParty[t] < MaxParties,
                      "masadaki grup", _tableParty[t]);

            // Varis plani. SpawnArrivals bunlari dogrudan
            // _content.Archetypes'a indis olarak veriyor.
            Check(_arrCount >= 0 && _arrCount <= _arrTick.Length, "varis sayisi", _arrCount);
            Check(_arrNext >= 0 && _arrNext <= _arrCount, "varis sirasi", _arrNext);
            for (int i = 0; i < _arrCount; i++)
                Check(_arrArchetype[i] >= 0 && _arrArchetype[i] < _content.Archetypes.Length,
                      "varis arketipi", _arrArchetype[i]);

            // Siparis edilen yemekler. AddJob bunlari
            // _content.Dishes'a indis olarak veriyor.
            for (int i = 0; i < MaxParties; i++)
            {
                CheckDish(_pDishMain[i], "ana yemek");
                CheckDish(_pDishSide[i], "yan yemek");
                CheckDish(_pDishDrink[i], "icecek");
                CheckDish(_pDishDessert[i], "tatli");
            }
        }

        /// <summary>-1 (siparis yok) ya da gecerli bir yemek indisi.</summary>
        private void CheckDish(int dish, string what)
        {
            Check(dish >= -1 && dish < _content.Dishes.Length, what, dish);
        }

        private static void Check(bool ok, string what, int value)
        {
            if (!ok)
                throw new InvalidOperationException(
                    "Kayit bozuk: " + what + " degeri gecersiz (" + value + ")");
        }

        // ---- yardimcilar -----------------------------------------------------
        private static void WriteRng(IStateWriter w, string key, Rng rng)
        {
            w.Begin(key);
            w.UInt("s0", rng.S0);
            w.UInt("s1", rng.S1);
            w.UInt("s2", rng.S2);
            w.UInt("s3", rng.S3);
            w.End();
        }

        private static Rng ReadRng(IStateReader r, string key)
        {
            r.Begin(key);
            uint s0 = r.UInt("s0");
            uint s1 = r.UInt("s1");
            uint s2 = r.UInt("s2");
            uint s3 = r.UInt("s3");
            r.End();
            return new Rng(s0, s1, s2, s3);
        }

        private readonly int[] _scratchStage = new int[MaxParties];
        private readonly int[] _scratchTask = new int[MaxServers];

        private int[] StageAsInt()
        {
            for (int i = 0; i < MaxParties; i++) _scratchStage[i] = (int)_pStage[i];
            return _scratchStage;
        }

        private int[] TaskAsInt(TaskKind[] kinds)
        {
            for (int i = 0; i < MaxServers; i++) _scratchTask[i] = (int)kinds[i];
            return _scratchTask;
        }
    }
}
