using System;

namespace Lokanta.Core.Content
{
    /// <summary>
    /// The content types the core sees. All of them immutable and INTEGER.
    /// Lokanta.Content parses the JSON; the core takes the finished
    /// structure.
    /// docs/23-core-contract.md 6.1.
    /// </summary>
    public readonly struct DishIngredient
    {
        public readonly int IngredientIndex;   // the position within ContentSet.Ingredients
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
        public long Price { get; }            // centi-coins
        public int PrepMs { get; }
        public int StationIndex { get; }
        /// <summary>
        /// 1..3. A complex dish is dearer and takes longer; served well it
        /// pleases more, served late it punishes more.
        ///
        /// This field was in the content and was read NOWHERE; it affected
        /// neither the price, nor satisfaction, nor the unlock.
        /// tools/audit_content.py found it that way.
        /// </summary>
        public int Complexity { get; }

        /// <summary>The earliest day on which the lock may come off.</summary>
        public int UnlockDay { get; }

        /// <summary>
        /// The season the lock comes off in, 1-4. DERIVED from UnlockDay -
        /// not two separate facts but two presentations of the same fact. It
        /// is validated during loading; the progression screen groups the
        /// dishes by it.
        /// </summary>
        public int UnlockSeason { get; }

        /// <summary>
        /// The unlock requires at least this tier of the station. Buying
        /// equipment therefore opens NOT ONLY speed but MENU.
        /// </summary>
        public int RequiresStationTier { get; }

        /// <summary>The reputation the unlock requires, in centi-points.</summary>
        public int UnlockReputationCenti { get; }
        public DishIngredient[] Ingredients { get; }

        /// <summary>The ingredient cost, in centi-coins. Computed once at load.</summary>
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
        /// <summary>Centi-coins per kilo.</summary>
        public long BasePrice { get; }
        public bool Perishable { get; }
        public int SpoilDays { get; }

        /// <summary>
        /// The seasonal price multiplier, in basis points, four values:
        /// spring, summer, autumn, winter. 10000 = no change.
        ///
        /// In the content 35 of the 77 ingredients have real movement
        /// (tomatoes 14% cheaper in summer, 20% dearer in winter), but the
        /// simulation was not reading this field at all.
        /// tools/audit_content.py found it that way.
        /// </summary>
        public int[] SeasonPriceBp { get; }

        /// <summary>
        /// The price multiplier by quality tier, in basis points. Three
        /// values: low, standard, high. Standard is always 10000.
        /// </summary>
        public int[] QualityPriceBp { get; }

        /// <summary>
        /// The quality tier's effect on satisfaction, in centi-points.
        ///
        /// The content carries a design decision here: the six most
        /// sensitive ingredients are ALL meat (mince, chicken breast, fish
        /// fillet, diced beef and lamb, lamb chops). Salt and black pepper
        /// are all but insensitive. So cutting corners is free on salt and
        /// a disaster on meat.
        ///
        /// That is why quality can be ONE SINGLE global setting: the result
        /// varies by dish on its own and the player is not loaded with 77
        /// separate decisions (docs/16, the touch budget).
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

        /// <summary>The price per kilo at the given season and quality, in centi-coins.</summary>
        public long PriceAt(int season, int quality)
        {
            long p = PriceInSeason(season);
            if (QualityPriceBp == null || quality < 0 || quality >= QualityPriceBp.Length)
                return p;
            return Core.Fx.MulDiv(p, QualityPriceBp[quality], Core.Fx.One);
        }

        /// <summary>The quality tier's effect on satisfaction, in centi-points.</summary>
        public int QualityDelta(int quality)
        {
            if (QualitySatisfactionCenti == null
                || quality < 0 || quality >= QualitySatisfactionCenti.Length) return 0;
            return QualitySatisfactionCenti[quality];
        }

        /// <summary>The price per kilo in the given season, in centi-coins.</summary>
        public long PriceInSeason(int season)
        {
            if (SeasonPriceBp == null || season < 0 || season >= SeasonPriceBp.Length)
                return BasePrice;
            return Core.Fx.MulDiv(BasePrice, SeasonPriceBp[season], Core.Fx.One);
        }
    }

    /// <summary>
    /// One rung of equipment. docs/27 Decision D: an upgrade DOES NOT TOUCH
    /// a dish's cooking time; it either adds a slot to the station or
    /// releases the cook earlier. That keeps the "buy equipment and
    /// everything speeds up" inflation shut off.
    /// </summary>
    public readonly struct StationTier
    {
        /// <summary>How many plates the station can take at once.</summary>
        public readonly int Slots;

        /// <summary>
        /// What share of the wall clock is spent IN THE COOK'S HANDS, in
        /// basis points. The oven is 2000: put it in, shut it, walk away.
        /// Drinks are 10000: no slack at all.
        /// </summary>
        public readonly int AttendBp;

        /// <summary>Centi-coins. Tier 0 is free and is there from the start.</summary>
        public readonly long Price;

        /// <summary>
        /// The table count at which this rung first becomes necessary. If 0
        /// it is not compulsory, it only makes the cook's life easier.
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

    /// <summary>One story beat of a regular.</summary>
    public readonly struct StoryBeat
    {
        public readonly int Beat;
        public readonly int RequiresVisits;
        /// <summary>The mean satisfaction threshold, in centi-points.</summary>
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
    /// A named regular. docs/11: an archetype produces thousands of
    /// customers, whereas a named customer is ONE SINGLE PERSON and is
    /// always the same person.
    ///
    /// Their behaviour comes from the archetype (patience, group size,
    /// price sensitivity, time of arrival); three things are their own: the
    /// dish they favour, the day they enter the campaign, and whether they
    /// may be written into the tab book.
    /// </summary>
    public sealed class RegularDef
    {
        public string Id { get; }
        public string NameKey { get; }
        public string JobKey { get; }
        /// <summary>The index of the underlying archetype.</summary>
        public int ArchetypeIndex { get; }
        /// <summary>The index of their favourite dish. Not on the menu means disappointment.</summary>
        public int FavouriteDish { get; }
        public int ArrivesFromDay { get; }
        public bool TabEligible { get; }
        public StoryBeat[] Story { get; }

        public RegularDef(string id, string nameKey, string jobKey,
                          int archetypeIndex, int favouriteDish,
                          int arrivesFromDay, bool tabEligible,
                          StoryBeat[] story)
        {
            Id = id; NameKey = nameKey; JobKey = jobKey;
            ArchetypeIndex = archetypeIndex; FavouriteDish = favouriteDish;
            ArrivesFromDay = arrivesFromDay; TabEligible = tabEligible;
            Story = story ?? new StoryBeat[0];
        }
    }

    /// <summary>The kind of the cuisine's signature mechanic. docs/23 8.2, a closed list.</summary>
    /// <summary>
    /// The CUISINE-SPECIFIC axis of the year-end evaluation. docs/08.
    ///
    /// The mechanic is in the code (Simulation.Score), the NUMBER is here:
    /// which measure, and the value of that measure that scores full marks.
    /// On fast food the highest covers served in a day, on Turkish cuisine
    /// the tab collection rate.
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
        Combo = 1,      // fast food: the combo and the flow
        Credit = 2,     // turk: the tab and the regulars
        Courses = 3,    // italian: table time and course timing
        Broth = 4,      // japanese: the broth and running out
    }

    /// <summary>
    /// The cuisine's signature mechanic. docs/07: "the most important line -
    /// this is the thing that shows a purchase is not a repaint but ANOTHER
    /// GAME."
    ///
    /// The mechanic is in the code, the numbers are here. If the block is
    /// missing, the cuisine does not load.
    /// </summary>
    public sealed class SignatureDef
    {
        public SignatureKind Kind { get; }

        /// <summary>
        /// The day the mechanic opens. docs/09: "The signature mechanic
        /// arrives AT THE START OF THE SECOND SEASON. Put in the first
        /// season it makes the teaching load far too heavy, because the
        /// player is already learning the menu and the price."
        /// </summary>
        public int FromDay { get; }

        // --- The combo (fast food) ----------------------------------------
        /// <summary>The dish indices that make up the combo: main, side, drink.</summary>
        public int[] ComboDishes { get; }
        /// <summary>The price applied to the sum of the three items, in basis points.</summary>
        public int ComboPriceBp { get; }
        /// <summary>How much longer a combo job ties up the cook.</summary>
        public int ComboKitchenLoadBp { get; }

        // --- The tab (turk) -----------------------------------------------
        public long CreditMaxPerRegular { get; }
        public int CreditDueDays { get; }
        public int CreditCollectChanceBp { get; }
        public int CreditTeaCollectBonusBp { get; }
        public int CreditDefaultRepPenaltyCenti { get; }
        public int CreditLoyaltyBonusCenti { get; }
        public int CreditTeaCostCenti { get; }

        /// <summary>
        /// TRUST: each visit by the customer adds this much to the
        /// collection chance, in basis points.
        ///
        /// This field was added as the result of a measurement. The chance
        /// was FIXED for everyone (8500, or 9500 with tea) and whoever paid
        /// paid 112% of the ticket: the expected cash was
        /// 0.95 x 1.12 = 1.064 x the ticket, so the tab was MORE PROFITABLE
        /// THAN A CASH SALE. There was never a day to refuse, and the
        /// mechanic was not a book but a free bonus button.
        ///
        /// The chance now depends on WHO it is written against: someone you
        /// have just met is a bad bet, someone who has come for years is a
        /// good one. That is the question itself - not "shall I open a tab"
        /// but "shall I open one FOR THIS MAN".
        /// </summary>
        public int CreditTrustPerVisitBp { get; }

        /// <summary>The largest share trust may add, in basis points.</summary>
        public int CreditTrustCapBp { get; }

        /// <summary>
        /// The CEILING on the collection chance. There must be no complete
        /// certainty: a riskless book is once again a bonus button that
        /// produces no decision.
        /// </summary>
        public int CreditChanceCapBp { get; }

        /// <summary>
        /// Each account collected adds this much permanently TO DEMAND, in
        /// basis points.
        ///
        /// This field was added as the result of a measurement. When the
        /// tab's only return was reputation the mechanic WAS OF NO USE: a
        /// good player is already at the reputation ceiling, so the loyalty
        /// bonus bought nothing. docs/07 says two things already - "it
        /// raises loyalty AND reputation" - and what loyalty buys is the
        /// customer coming back, not a score pressed against a ceiling.
        /// </summary>
        public int CreditLoyaltyDemandBp { get; }
        /// <summary>The ceiling on loyalty. If it accumulated without limit the tab would become compulsory.</summary>
        public int CreditLoyaltyCapBp { get; }

        /// <summary>
        /// The chance that an eligible party ASKS for a tab, in basis points.
        ///
        /// This is the mechanic's real direction. In the first draft the tab
        /// was a bonus button and measurement rejected it: a good player is
        /// already at the reputation ceiling with full tables, so neither
        /// reputation nor demand bought anything - the tab only lost them
        /// cash. The right direction is THE OTHER WAY ROUND: the customer
        /// ASKS, and whoever refuses loses. The same idea as the mechanic
        /// where a customer asks for a locked dish (docs/34 6).
        /// </summary>
        public int CreditAskChanceBp { get; }
        /// <summary>The satisfaction penalty when a customer who asks is not given a tab.</summary>
        public int CreditRefusedPenaltyCenti { get; }

        /// <summary>
        /// The share a customer settling their account puts ON TOP, in basis
        /// points.
        ///
        /// This is the mechanic's earning side. In the first two attempts
        /// the tab's return was REPUTATION and DEMAND; neither is any use to
        /// a good player - reputation is already at the ceiling, the tables
        /// are already full. The only real return left is MONEY: the man
        /// written into the tab book settles his account and then some.
        ///
        /// The expected value: collection chance x (1 + this share). Because
        /// a glass of tea raises the chance, whoever offers the tea wins and
        /// whoever makes no distinction loses.
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
        /// Is this one of the seven shared stations. If false it is equipment
        /// named SPECIFICALLY for the cuisine (tas_firin, doner_ocagi, ...):
        /// it is not there at the start, it never becomes COMPULSORY because
        /// of the table count, and it only opens menu.
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
                throw new ArgumentException("a station must have at least one rung", nameof(tiers));
        }

        public int MaxTier { get { return Tiers.Length - 1; } }
    }

    /// <summary>
    /// The cold-store tier. It says what share of a perishable ingredient's
    /// OWN shelf life (spoilDays) applies.
    ///
    /// At tier 0 the share is zero: everything perishable goes off
    /// overnight. docs/12 3 writes that down as the designed baseline; the
    /// cold store is the upgrade that CHANGES that baseline, not a fix that
    /// patches a shortcoming.
    /// </summary>
    public readonly struct StorageTier
    {
        /// <summary>What share of spoilDays applies, in basis points. 0 = none at all.</summary>
        public readonly int KeepBp;
        public readonly long Price;      // centi-coins

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
                throw new ArgumentException("a store must have at least one rung", nameof(tiers));
        }

        public int MaxTier { get { return Tiers.Length - 1; } }
    }

    /// <summary>The four slots of the service day. docs/12 5.6.</summary>
    public enum DaySlot
    {
        Opening = 0,
        Lunch = 1,
        Afternoon = 2,
        Evening = 3,
        Count = 4
    }

    public sealed class ArchetypeDef
    {
        public string Id { get; }
        public string NameKey { get; }
        public int TierIndex { get; }          // 0 frequent, 1 middling, 2 rare
        public int Weight { get; }
        public int PatienceMs { get; }
        public int PriceSensitivityBp { get; }
        public int GroupSizeMin { get; }
        public int GroupSizeMax { get; }
        public int ReputationWeightBp { get; }
        public int TipChanceBp { get; }
        /// <summary>The weights of the four slots; they sum to 10000.</summary>
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
    /// The whole content of a single cuisine. Immutable once loaded.
    /// The arrays are ordered and addressed by index: the save holds the
    /// index rather than the id, but because an index depends on the content
    /// version the save file also keeps the ids and re-matches them on load.
    /// </summary>
    public sealed class ContentSet
    {
        public string Cuisine { get; }

        /// <summary>
        /// The DURATION of the day's four slots, in basis points, summing to
        /// 10000. docs/28-peak-decision.md Decision G: the slots are not
        /// equal and they vary by cuisine. A Turkish restaurant's lunch slot
        /// is 48% of the day.
        /// </summary>
        public int[] SlotDurationsBp { get; }

        /// <summary>
        /// No waiter comes to the table: ordering and paying happen AT THE
        /// COUNTER and the customer carries the tray. The hall's work is
        /// counter + clearing + washing-up.
        /// </summary>
        public bool SelfService { get; }

        /// <summary>
        /// This cuisine's hall workload, in micro per customer. If 0, the
        /// total from economy.json applies.
        /// </summary>
        public int HallWorkPerCustomerMicro { get; }

        /// <summary>The wage numerator: sum(work x daily wage). If 0 it is unchanged.</summary>
        public long HallWageNumerator { get; }

        /// <summary>
        /// The demand multiplier, in basis points. 0 or 10000 means no
        /// change. Fast food is a volume game, Turkish is a ticket game.
        /// </summary>
        public int CustomerMultiplierBp { get; }

        /// <summary>The rent multiplier, in basis points. 0 or 10000 means no change.</summary>
        public int RentMultiplierBp { get; }

        /// <summary>
        /// How long the customer takes to eat, in milliseconds. It varies by
        /// cuisine: short on fast food, long in a restaurant. It sets the
        /// table turnover rate directly.
        ///
        /// This field read 38,000 in the content while TimingConfig was
        /// using 45,000; the two had been out of step for ages and nobody
        /// had seen it. Check number three of tools/audit_content.py found
        /// it that way.
        /// </summary>
        public int EatMs { get; }
        public IngredientDef[] Ingredients { get; }
        public DishDef[] Dishes { get; }
        public ArchetypeDef[] Archetypes { get; }
        public StationDef[] Stations { get; }

        /// <summary>The cold-store ladder. Null when the content has none.</summary>
        public StorageDef Storage { get; }

        /// <summary>
        /// This cuisine's MEAN dish complexity, in basis points (15000 = 1.5).
        ///
        /// Complexity risk must be RELATIVE, not ABSOLUTE. Fast food's mean
        /// is 1.50, the Turkish restaurant's 2.34: on an absolute scale 17
        /// dishes on the Turkish menu counted as "hard" and a good player's
        /// reputation fell to 51.6, against 99.0 on fast food. The same
        /// mistake had been made in the unlock rule as well (see docs/34 5).
        ///
        /// On a relative scale each cuisine's own mean is neutral: a dish
        /// above the mean carries risk, one below it gives relief.
        /// </summary>
        public int MeanComplexityBp { get; }

        /// <summary>A dish's complexity relative to the mean of its own cuisine.</summary>
        public int RelativeComplexityBp(int dish)
        {
            if (MeanComplexityBp <= 0) return Core.Fx.One;
            return (int)Core.Fx.MulDiv(Dishes[dish].Complexity * Core.Fx.One,
                                       Core.Fx.One, MeanComplexityBp);
        }

        /// <summary>
        /// The menu ROLES: which dish groups stand for the main, the side,
        /// the drink and the dessert. They vary per cuisine.
        ///
        /// docs/13 designed the cuisine-specific group names ON PURPOSE:
        /// ana/yan on fast food, sulu/corba/pilav/izgara/meze in the Turkish
        /// restaurant. But the simulation had hard-coded the fast food
        /// vocabulary, and on the second cuisine no customer could find a
        /// main dish; all eight strategies went under with zero customers.
        /// </summary>
        public string[] MainGroups { get; }
        public string[] SideGroups { get; }
        public string[] DrinkGroups { get; }
        public string[] DessertGroups { get; }

        /// <summary>The cuisine's signature mechanic. docs/23 8.2: if missing, it does not load.</summary>
        public SignatureDef Signature { get; }

        /// <summary>The cuisine-specific axis of the year-end evaluation.</summary>
        public ScoreAxisDef ScoreAxis { get; }

        /// <summary>
        /// The pool of staff names. It may be empty - the UI then falls back
        /// to numbered names such as "Cook 1" and the game carries on.
        /// </summary>
        public string[] StaffNames { get; }

        /// <summary>The named regulars, ordered by the day they arrive.</summary>
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
                          string[] staffNames = null,
                          bool selfService = false,
                          int hallWorkPerCustomerMicro = 0,
                          long hallWageNumerator = 0,
                          int customerMultiplierBp = 0,
                          int rentMultiplierBp = 0)
        {
            CustomerMultiplierBp = customerMultiplierBp;
            RentMultiplierBp = rentMultiplierBp;
            SelfService = selfService;
            HallWorkPerCustomerMicro = hallWorkPerCustomerMicro;
            HallWageNumerator = hallWageNumerator;
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

        /// <summary>How many dishes are open on a given day.</summary>
        public int UnlockedDishCount(int day)
        {
            int n = 0;
            for (int i = 0; i < Dishes.Length; i++)
                if (Dishes[i].UnlockDay <= day) n++;
            return n;
        }

        /// <summary>
        /// How many dishes open in that season. The progression curve of
        /// docs/09 is built on this (6 -> 13 -> 21 -> 27 -> 32) and the
        /// progression screen groups the dishes by season.
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
