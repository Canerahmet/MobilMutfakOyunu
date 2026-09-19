using System;
using Lokanta.Core.Save;

namespace Lokanta.Core.Sim
{
    /// <summary>
    /// The simulation's state walk and command log.
    /// docs/23-core-contract.md 6.2 and 7.
    ///
    /// Write and Restore have to walk THE SAME ORDER. The two sit side by
    /// side so that any drift is visible. The order is a contract: if it
    /// changes, old saves cannot be read and a migration is needed.
    /// </summary>
    public sealed partial class Simulation
    {
        /// <summary>
        /// The save format version. It goes up whenever the field order
        /// changes.
        /// 2: the station equipment tier and the cooking jobs were added.
        /// 3: the cold-store tier and ingredient age were added.
        /// 4: the dessert item was added (a fourth station job).
        /// 5: the dish that was asked for but cannot be made was added.
        /// 6: ingredient quality was added.
        /// 7: the owner's intervention allowance was added.
        /// 8: the day's market prices were added.
        /// 9-14: UNDOCUMENTED. The number climbed from 8 to 14 but the list
        ///       was not kept up; anyone wanting to write a migration cannot
        ///       tell what changed. New versions will be written down from
        ///       here on.
        /// 15: the party's KITCHEN work moved to a flag of its own
        ///     (kitchenTask). Patience now runs while the food cooks too.
        ///
        /// THE MIGRATION RULE: new fields are read through IStateReader.Has
        /// and left at their default when absent. Throwing on a missing key
        /// would have wiped every campaign on the first patch after release.
        /// </summary>
        // 16 -> 17: the signature axis moved from `peakCovers` to
        // `comboShare`; the "peakCovers" field gave way to "mainOrders" +
        // "comboOrders". Also "teaSpend": the cost of the tab's tea is now
        // counted.
        // 20 -> 21: badges (badges/badgesToday/creditEverOpened) and the
        // weekly report (weekAxis/weekAxisPrev/weekReportDay).
        // 21 -> 22: staff TENURE (cookTenure/salonTenure) is kept
        // separately. The screen was showing experience under the word
        // "days", and because experience depends on the trait, an
        // `experienced` hand who had worked for sixty days was showing
        // "0 days".
        //
        // MIGRATION: an old save has no tenure. The default is 0 - so after
        // the patch everyone's tenure starts being counted from zero. That
        // is the right answer: making one up (deriving it from XP, say)
        // would bring back the very lie being fixed, in another disguise.
        // 22 -> 23: A STATION WAS INSERTED IN THE MIDDLE OF THE CLOSED LIST.
        //
        // `tier` and `_jobStation` are written as ARRAYS INDEXED BY STATION,
        // and on 18 September the fryer went in between the hob and the grill
        // (ContentSetLoader.StationIds). Every shared station after the hob
        // shifted by one and the array grew by one.
        //
        // WITHOUT THIS BUMP a version 22 save does not merely load the wrong
        // tiers - it throws on the length check, falls through to the backup,
        // which is also version 22 and also throws, and leaves the simulation
        // HALF RESTORED. And the slot card would still have shown a healthy
        // campaign, because the card checks the version: the player presses
        // Continue and the game dies.
        // MIGRATION: `fritoz` did not exist in a version 22 save, so its tier
        // is 0 and every station after the hob moves up one slot. That is a
        // derivation, not a guess, which is why 22 stays readable.
        // VERSION 24: the owner's attention REGENERATES instead of being a
        // day budget, and the door can be shut without ending the day. Two
        // new fields, `interventionMs` (milliseconds banked toward the next
        // charge) and `doorsClosed`.
        //
        // A version 23 file has neither, and both have a correct answer
        // rather than a guessed one: a save is taken between days, so the
        // service has not started - nothing is banked and the door is open.
        // That is a derivation, which is why 20 through 23 stay readable.
        public const int SaveVersion = 25;

        /// <summary>
        /// Where the fryer was inserted into the closed station list.
        ///
        /// Written here as well as in ContentSetLoader because the SAVE
        /// migration has to know it and the core cannot see the content
        /// loader. If the two ever disagree an old save is silently shifted
        /// to the wrong stations, so SaveTests asserts they match.
        /// </summary>
        public const int FryerIndex = 1;

        /// <summary>
        /// The OLDEST save version that can be read.
        ///
        /// `Restore` rejects anything older than this; every version in
        /// between is read through A VERSION GATE - that is, fields that did
        /// not yet exist in that version are skipped and left at their
        /// defaults.
        ///
        /// WHY NOT "Has()": for sixteen months the rule at the top of this
        /// file said "new fields are read through Has()", and it had been
        /// applied in TWO of 126 reads. What is more, it is the wrong tool:
        /// Has() always treats a field's ABSENCE as legitimate, so it cannot
        /// tell a genuinely CORRUPT save from an old one. A version gate
        /// separates the two - if a version 21 save has no "badges" then
        /// that save is corrupt, and blowing up is the RIGHT thing to do.
        ///
        /// 20 WAS CHOSEN because anything older would be invented: what
        /// versions 9-14 changed is UNDOCUMENTED, so nobody can write the
        /// right gate for them. And there is no released save either, so
        /// nothing is lost.
        ///
        /// EVERY version in this range is loaded by a test.
        /// SaveTests.An_old_version_save_opens is a theory over
        /// MinReadableVersion..SaveVersion-1; it used to wind the version
        /// back by `SaveVersion - 1`, which slid forward with every release
        /// and left THIS number untested.
        ///
        /// FOR THE NEXT VERSION: read the fields under `if (version >= N)`,
        /// write a line into the list above, AND add a row to
        /// SaveTests.FieldsAddedIn - SaveTests.Every_readable_version_is_covered
        /// goes red until you do.
        /// </summary>
        public const int MinReadableVersion = 20;

        // ---- command log readers --------------------------------------------
        public int CommandCount { get { return _commandCount; } }
        public Command CommandAt(int i) { return _commandLog[i]; }

        /// <summary>
        /// The commands applied since the start of the day. The save file
        /// carries this.
        /// </summary>
        public Command[] CopyCommandLog()
        {
            Command[] copy = new Command[_commandCount];
            Array.Copy(_commandLog, copy, _commandCount);
            return copy;
        }

        /// <summary>A byte-by-byte hash of the state. The determinism tests compare this.</summary>
        public ulong StateHash()
        {
            HashStateWriter w = new HashStateWriter();
            Write(w);
            return w.Result;
        }

        // =====================================================================
        // Writing
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
            // IN "restaurant" AND NOT NEXT TO interventionsLeft, WHICH IS IN
            // "stations". These two are day state, not station state, and the
            // migration test walks the restaurant block looking for the
            // fields FieldsAddedIn names - the fryer's version 23 needed a
            // special branch because ITS arrays live under "stations", and
            // there is no reason to make a second one.
            w.Int("interventionMs", _interventionMs);
            w.Int("doorsClosed", _doorsClosed ? 1 : 0);
            w.IntArray("weekAxis", _weekAxis, SeasonScore.AxisCount);
            w.IntArray("weekAxisPrev", _weekAxisPrev, SeasonScore.AxisCount);
            w.Long("cash", _cash);
            w.Int("cooks", _cooks);
            w.Int("hall", _hall);
            w.IntArray("cookXp", _cookXpDays, MaxServers);
            w.IntArray("cookTenure", _cookTenure, MaxServers);
            w.IntArray("salonTenure", _hallTenure, MaxServers);
            w.IntArray("salonXp", _hallXpDays, MaxServers);
            w.IntArray("cookName", _cookName, MaxServers);
            w.IntArray("salonName", _hallName, MaxServers);
            w.IntArray("cookTraitA", _cookTraitA, MaxServers);
            w.IntArray("cookTraitB", _cookTraitB, MaxServers);
            w.IntArray("salonTraitA", _hallTraitA, MaxServers);
            w.IntArray("salonTraitB", _hallTraitB, MaxServers);
            w.IntArray("cookMorale", _cookMorale, MaxServers);
            w.IntArray("salonMorale", _hallMorale, MaxServers);
            w.Int("busyStreak", _busyStreak);
            w.Int("candDay", _candDay);
            w.IntArray("candA", _candTraitA, CandidateSlots * 2);
            w.IntArray("candB", _candTraitB, CandidateSlots * 2);
            // In the RESTAURANT block, not beside the other regular arrays in
            // "signature": the migration test builds an old save by removing
            // from this block the fields the table says were added later,
            // and a field anywhere else cannot be migrated by it.
            w.IntArray("regDefaults", _regDefaults, MaxRegulars);
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

            // The signature mechanic's state. The tab book is part of the
            // save: open accounts are settled days later.
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

            // THE PLATE CYCLE. The invariant: clean + in use + dirty = total.
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
            w.IntArray("salonKind", TaskAsInt(_hallTaskKind), MaxServers);
            w.IntArray("salonTarget", _hallTaskTarget, MaxServers);
            w.IntArray("salonLeft", _hallTaskLeftMs, MaxServers);
            w.IntArray("kitchenKind", TaskAsInt(_kitchenTaskKind), MaxServers);
            w.IntArray("kitchenTarget", _kitchenTaskTarget, MaxServers);
            w.IntArray("kitchenLeft", _kitchenTaskLeftMs, MaxServers);
            w.End();

            // Equipment and the cooking jobs. The slot counter is DERIVED:
            // it is rebuilt from _jobState rather than written. A number
            // held in two places is one more place the save can go wrong.
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

            // TODAY'S WAGES, RENT AND SPOILAGE ARE WRITTEN TOO.
            //
            // All three are inside the evening report and they determine
            // NetProfit; because they were not saved, the report after a
            // load WAS TELLING A LIE: close day seven (the rent day), lock
            // the phone, come back - and "Today's profit" ignored the
            // week's largest outgoing and showed a big plus, while the
            // number in the till had gone down.
            //
            // This was the bug explained in the comment on
            // DayReport.WageCost (the game's central tension was visible
            // nowhere) coming back exactly as it was, by way of the save.
            w.Long("dayWages", _dayWages);
            w.Long("dayRent", _dayRent);
            w.Long("daySpoiled", _daySpoiled);

            // Revenue across the whole campaign: it was being zeroed on
            // every load.
            w.Long("revenueAll", _revenueAll);
            w.End();

            w.Begin("finance");
            w.Long("wagesPaid", _weeklyWagesPaid);
            w.Long("rentPaid", _weeklyRentPaid);
            w.Int("firstDebtDay", _firstDebtDay);

            // The history behind the year-end evaluation. It goes into the
            // save because the score looks AT THE PAST: peak covers, the
            // collection rate and how many times the ladder was climbed down
            // are a summary of the whole season, not of one day. Unsaved, a
            // loaded game is left without a past.
            w.Int("debtRungs", _debtRungs);
            w.Long("spoiledValue", _spoiledValue);
            w.Long("ingredientSpend", _ingredientSpend);
            w.Long("rescueValue", _rescueValue);
            w.Long("loanTaken", _loanTaken);
            w.Long("loanRepaidAll", _loanRepaidAll);
            w.Long("equipmentSpend", _equipmentSpend);
            w.Long("expansionSpend", _expansionSpend);
            // THE SIGNATURE AXIS'S COUNTERS.
            //
            // The year-end score looks AT THE PAST, and the past only exists
            // if it is accumulated: unsaved, a player's sixty days of combo
            // use is wiped by a single load and the score comes out silently
            // wrong. (`peakCovers` used to stand here; once the axis moved
            // to measuring the combo, that field was deleted.)
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
        // Reading. THE SAME ORDER as Write.
        // =====================================================================
        public void Restore(IStateReader r)
        {
            if (r == null) throw new ArgumentNullException(nameof(r));

            r.Begin("header");
            int version = r.Int("version");
            if (version > SaveVersion || version < MinReadableVersion)
                throw new InvalidOperationException(
                    "Save version " + version + ", the readable range is "
                    + MinReadableVersion + "-" + SaveVersion + ".");
            r.Long("seed");                     // the seed was given in the constructor
            _tickIndex = r.Long("tick");
            _day = r.Int("day");
            _phase = (DayPhase)r.Int("phase");
            _serviceTick = r.Int("serviceTick");
            string cuisine = r.Str("cuisine");
            if (!string.Equals(cuisine, _content.Cuisine, StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "The save's cuisine is '" + cuisine + "', the one loaded is '" + _content.Cuisine + "'");
            r.End();

            r.Begin("restaurant");
            _tableCount = r.Int("tables");
            _reputationCenti = r.Int("reputationCenti");
            _reputationOverflowCenti = r.Int("reputationOverflow");
            // ADDED IN VERSION 21: badges and the weekly report.
            //
            // In an older save these fields ARE NOT THERE, and it is right
            // that they are not. They are left at their defaults: no badge
            // earned, report day 0. The player carries on from where they
            // were; they simply do not earn the pre-patch badges
            // retrospectively - nor could they have, since those days were
            // played with the mechanic absent.
            if (version >= 21)
            {
                _badges = r.Int("badges");
                _badgesToday = r.Int("badgesToday");
                _creditEverOpened = r.Bool("creditEverOpened");
                _weekReportDay = r.Int("weekReportDay");
            if (version >= 24)
            {
                _interventionMs = r.Int("interventionMs");
                _doorsClosed = r.Int("doorsClosed") != 0;
            }
            else
            {
                // A save is written between days, so service has not begun:
                // nothing is banked and the door is open. Both are what
                // OpenService sets anyway, so this is a derivation rather
                // than a default.
                _interventionMs = 0;
                _doorsClosed = false;
            }
                r.IntArray("weekAxis", _weekAxis, SeasonScore.AxisCount);
                r.IntArray("weekAxisPrev", _weekAxisPrev, SeasonScore.AxisCount);
            }
            _cash = r.Long("cash");
            _cooks = r.Int("cooks");
            _hall = r.Int("hall");
            r.IntArray("cookXp", _cookXpDays, MaxServers);

            // ADDED IN VERSION 22: staff tenure. It is not in an old save
            // and starts being counted from zero (see the note on
            // SaveVersion).
            if (version >= 22)
            {
                r.IntArray("cookTenure", _cookTenure, MaxServers);
                r.IntArray("salonTenure", _hallTenure, MaxServers);
            }
            r.IntArray("salonXp", _hallXpDays, MaxServers);
            r.IntArray("cookName", _cookName, MaxServers);
            r.IntArray("salonName", _hallName, MaxServers);
            r.IntArray("cookTraitA", _cookTraitA, MaxServers);
            r.IntArray("cookTraitB", _cookTraitB, MaxServers);
            r.IntArray("salonTraitA", _hallTraitA, MaxServers);
            r.IntArray("salonTraitB", _hallTraitB, MaxServers);
            r.IntArray("cookMorale", _cookMorale, MaxServers);
            r.IntArray("salonMorale", _hallMorale, MaxServers);
            _busyStreak = r.Int("busyStreak");
            _candDay = r.Int("candDay");
            r.IntArray("candA", _candTraitA, CandidateSlots * 2);
            r.IntArray("candB", _candTraitB, CandidateSlots * 2);
            if (version >= 25)
                r.IntArray("regDefaults", _regDefaults, MaxRegulars);
            else
                for (int i = 0; i < MaxRegulars; i++) _regDefaults[i] = 0;
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

            // THE PLATE CYCLE. Not in an old save: in that case every plate
            // counts as clean - it is asked for with Has(), because reading a
            // missing field as zero would leave the restaurant with no plates
            // and service would never start.
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

            // NOT IN AN OLD SAVE: patience used to cover the kitchen work
            // as well, and the separate flag arrived in version 15. If it is
            // missing it stays at its default (false) - so an old save opens
            // and that one day passes a little more easily. This rather than
            // throwing.
            if (r.Has("kitchenTask"))
                r.BoolArray("kitchenTask", _pKitchenTask, MaxParties);
            else
                for (int i = 0; i < MaxParties; i++) _pKitchenTask[i] = false;
            r.BoolArray("warned", _pWarned, MaxParties);
            r.End();

            r.Begin("work");
            int[] kinds = new int[MaxServers];
            r.IntArray("salonKind", kinds, MaxServers);
            for (int i = 0; i < MaxServers; i++) _hallTaskKind[i] = (TaskKind)kinds[i];
            r.IntArray("salonTarget", _hallTaskTarget, MaxServers);
            r.IntArray("salonLeft", _hallTaskLeftMs, MaxServers);
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
            // THE STATION LIST GREW IN THE MIDDLE, SO OLD ARRAYS ARE SHIFTED.
            //
            // `fritoz` went in at index 1 on 18 September
            // (ContentSetLoader.StationIds), so a version 22 save's arrays are
            // one short and everything from the old index 1 onwards means a
            // different station now.
            //
            // The migration is exact rather than a guess: the fryer did not
            // exist, so its tier is 0 in every old save, and every other
            // station keeps its tier and moves up one slot. The same shift
            // applies to the job array, which stores station indices.
            if (version < 23)
            {
                int[] oldTier = new int[_stationTier.Length - 1];
                r.IntArray("tier", oldTier, oldTier.Length);
                _stationTier[0] = oldTier.Length > 0 ? oldTier[0] : 0;
                _stationTier[FryerIndex] = 0;
                for (int i = 1; i < oldTier.Length; i++)
                    _stationTier[i + 1] = oldTier[i];

                r.IntArray("jobStation", _jobStation, _jobStation.Length);
                for (int j = 0; j < _jobStation.Length; j++)
                    if (_jobStation[j] >= FryerIndex) _jobStation[j]++;
            }
            else
            {
                r.IntArray("tier", _stationTier, _stationTier.Length);
                r.IntArray("jobStation", _jobStation, _jobStation.Length);
            }
            r.IntArray("jobMs", _jobMs, _jobMs.Length);
            r.IntArray("jobPlates", _jobPlates, _jobPlates.Length);
            r.IntArray("jobSlots", _jobSlots, _jobSlots.Length);
            r.IntArray("jobState", _jobState, _jobState.Length);
            r.IntArray("jobsLeft", _pJobsLeft, MaxParties);
            r.End();

            // The slot counter is derived: it is counted again from the
            // cooking jobs.
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

            // The day's log is replayed separately after loading.
            _commandCount = 0;
            _events.Clear();

            Validate();

            // THE DISHES ALREADY ANNOUNCED ARE DERIVED FROM THE STATE.
            //
            // _dishWasUnlocked was not being saved, and the constructor was
            // filling it with the state of THE FIRST DAY. Open a save from
            // day thirty, press "Next Day", and every dish in between is
            // announced as "unlocked" all over again: a notification and a
            // level-up sound for each one. What the comment in the
            // constructor prevented for the first day was coming back by way
            // of the load path.
            //
            // Deriving the array is better than saving it: "everything open
            // right now counts as announced" is exactly the meaning wanted,
            // and it does not grow the save format. AFTER Validate, because
            // Unlocked() needs a valid state.
            for (int i = 0; i < _dishWasUnlocked.Length; i++)
                _dishWasUnlocked[i] = Unlocked(i);
        }

        /// <summary>
        /// Checks that the loaded state MAKES SENSE.
        ///
        /// Why it is needed: the save file sits on the player's device in
        /// plain text, unencrypted and without a checksum. A file with a
        /// single byte corrupted but still valid JSON - "tables": 5, say -
        /// was being accepted in silence. The game opened, the hall was
        /// drawn, and then, when the player opened the Staff screen or
        /// closed the day, TierForTables(5) threw: the save fell into the
        /// "it opens but it cannot be played" state and there was nothing
        /// for it but to delete it.
        ///
        /// The exception thrown here is caught in the save layer and the
        /// slot is shown as "corrupt" - that is, the error is told TO THE
        /// PLAYER, before they go into the game.
        /// </summary>
        private void Validate()
        {
            Check(_day >= 1, "day", _day);
            Check(_phase >= DayPhase.Morning && _phase <= DayPhase.Evening,
                  "stage", (int)_phase);
            Check(_serviceTick >= 0, "service counter", _serviceTick);

            Check(_tableCount >= 1 && _tableCount <= MaxTables, "tables", _tableCount);
            Check(_economy.HasTierForTables(_tableCount), "table tier", _tableCount);

            Check(_cooks >= 0 && _cooks <= MaxServers, "cooks", _cooks);
            Check(_hall >= 0 && _hall <= MaxServers, "hall crew", _hall);
            Check(_partyCount >= 0 && _partyCount <= MaxParties, "party", _partyCount);
            Check(_tabCount >= 0 && _tabCount <= _tabAmount.Length, "tab", _tabCount);

            Check(_reputationCenti >= 0 && _reputationCenti <= 10000,
                  "reputation", _reputationCenti);

            // IT WAS ONE TOO MANY. MaxTier = Tiers.Length - 1, so the highest
            // valid tier is Tiers.Length-1. Writing "<=" treated one past the
            // end of the array as valid, and that value blew up inside
            // StationSlots and StationAttendBp IN THE MIDDLE OF SERVICE - so
            // the save looked "sound" and then came down inside the game.
            // Validate exists precisely to stop that.
            for (int i = 0; i < _stationTier.Length; i++)
                Check(_stationTier[i] >= 0
                      && _stationTier[i] < _content.Stations[i].Tiers.Length,
                      "station tier", _stationTier[i]);

            // The cold-store tier was not being looked at at all: a corrupt
            // value came down inside StorageKeepBp(), on every CloseDay.
            if (_content.Storage != null)
                Check(_storageTier >= 0 && _storageTier < _content.Storage.Tiers.Length,
                      "cold-store tier", _storageTier);

            Check(_quality >= 0 && _quality < QualityCount, "quality", _quality);

            for (int i = 0; i < _pTable.Length; i++)
                Check(_pTable[i] >= -1 && _pTable[i] < MaxTables, "the party's table", _pTable[i]);

            // The TABLE -> PARTY mapping. The view layer calls TableStage(t)
            // on every frame and that reads _pStage[_tableParty[t]]: a
            // corrupt value blows up ON EVERY FRAME while the hall is being
            // drawn.
            for (int t = 0; t < _tableParty.Length; t++)
                Check(_tableParty[t] >= -1 && _tableParty[t] < MaxParties,
                      "the party at the table", _tableParty[t]);

            // The arrival plan. SpawnArrivals feeds these straight into
            // _content.Archetypes as an index.
            Check(_arrCount >= 0 && _arrCount <= _arrTick.Length, "arrival count", _arrCount);
            Check(_arrNext >= 0 && _arrNext <= _arrCount, "arrival index", _arrNext);
            for (int i = 0; i < _arrCount; i++)
                Check(_arrArchetype[i] >= 0 && _arrArchetype[i] < _content.Archetypes.Length,
                      "arrival archetype", _arrArchetype[i]);

            // The dishes ordered. AddJob feeds these into _content.Dishes as
            // an index.
            for (int i = 0; i < MaxParties; i++)
            {
                CheckDish(_pDishMain[i], "main dish");
                CheckDish(_pDishSide[i], "side dish");
                CheckDish(_pDishDrink[i], "drink");
                CheckDish(_pDishDessert[i], "dessert");
            }
        }

        /// <summary>-1 (nothing ordered) or a valid dish index.</summary>
        private void CheckDish(int dish, string what)
        {
            Check(dish >= -1 && dish < _content.Dishes.Length, what, dish);
        }

        private static void Check(bool ok, string what, int value)
        {
            if (!ok)
                throw new InvalidOperationException(
                    "The save is corrupt: the value of " + what + " is invalid (" + value + ")");
        }

        // ---- helpers ---------------------------------------------------------
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
