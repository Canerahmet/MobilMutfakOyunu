using System.Collections.Generic;
using Newtonsoft.Json;

namespace Lokanta.Content
{
    // docs/23-core-contract.md 8.3: every dish carries four mandatory
    // parameters. Field names are marked explicitly; no name is derived by
    // reflection.

    public sealed class DishIngredientDto
    {
        [JsonProperty("id")] public string Id { get; set; }
        [JsonProperty("grams")] public int Grams { get; set; }
    }

    public sealed class PlatingDto
    {
        [JsonProperty("base")] public string Base { get; set; }
        [JsonProperty("toppings")] public List<string> Toppings { get; set; }
    }

    public sealed class DishDto
    {
        [JsonProperty("id")] public string Id { get; set; }
        [JsonProperty("nameKey")] public string NameKey { get; set; }
        [JsonProperty("cuisine")] public string Cuisine { get; set; }
        [JsonProperty("group")] public string Group { get; set; }
        [JsonProperty("price")] public long Price { get; set; }
        [JsonProperty("prepMs")] public int PrepMs { get; set; }
        [JsonProperty("station")] public string Station { get; set; }
        [JsonProperty("complexity")] public int Complexity { get; set; }
        [JsonProperty("unlockSeason")] public int UnlockSeason { get; set; }
        [JsonProperty("unlockDay")] public int UnlockDay { get; set; }
        /// <summary>The minimum station tier this dish needs.</summary>
        [JsonProperty("requiresStationTier")] public int RequiresStationTier { get; set; }
        /// <summary>The reputation needed to unlock it, in centi-points.</summary>
        [JsonProperty("unlockReputationCenti")] public int UnlockReputationCenti { get; set; }
        [JsonProperty("ingredients")] public List<DishIngredientDto> Ingredients { get; set; }
        [JsonProperty("plating")] public PlatingDto Plating { get; set; }
    }

    /// <summary>
    /// A named regular customer. docs/11: "a named customer is one single
    /// person, hand-written, with a story, and always the same person."
    /// </summary>
    public sealed class RegularDto
    {
        [JsonProperty("id")] public string Id { get; set; }
        [JsonProperty("cuisine")] public string Cuisine { get; set; }
        [JsonProperty("nameKey")] public string NameKey { get; set; }
        [JsonProperty("jobKey")] public string JobKey { get; set; }
        [JsonProperty("archetypeBase")] public string ArchetypeBase { get; set; }
        [JsonProperty("favouriteDish")] public string FavouriteDish { get; set; }
        [JsonProperty("arrivesFromDay")] public int ArrivesFromDay { get; set; }
        [JsonProperty("veresiyeEligible")] public bool TabEligible { get; set; }
        [JsonProperty("story")] public List<StoryBeatDto> Story { get; set; }
    }

    public sealed class StoryBeatDto
    {
        [JsonProperty("beat")] public int Beat { get; set; }
        [JsonProperty("requiresVisits")] public int RequiresVisits { get; set; }
        [JsonProperty("requiresSatisfaction")] public int RequiresSatisfaction { get; set; }
        [JsonProperty("textKey")] public string TextKey { get; set; }
    }

    /// <summary>docs/23 8.2: the mechanic lives in code, the numbers in data.</summary>
    /// <summary>The staff name pool. content/names.json.</summary>
    public sealed class NamesDto
    {
        [JsonProperty("staff")] public string[] Staff { get; set; }
    }

    /// <summary>The cuisine's year-end axis. docs/13 cuisines/*.json.</summary>
    public sealed class ScoreAxisDto
    {
        [JsonProperty("kind")] public string Kind { get; set; }
        [JsonProperty("nameKey")] public string NameKey { get; set; }
        [JsonProperty("target")] public int Target { get; set; }
    }

    public sealed class SignatureDto
    {
        [JsonProperty("kind")] public string Kind { get; set; }
        [JsonProperty("combo")] public ComboDto Combo { get; set; }
        [JsonProperty("credit")] public CreditDto Credit { get; set; }
    }

    public sealed class ComboDto
    {
        [JsonProperty("items")] public List<string> Items { get; set; }
        [JsonProperty("priceBp")] public int PriceBp { get; set; }
        [JsonProperty("kitchenLoadBp")] public int KitchenLoadBp { get; set; }
    }

    public sealed class CreditDto
    {
        [JsonProperty("maxPerRegular")] public long MaxPerRegular { get; set; }
        [JsonProperty("dueDays")] public int DueDays { get; set; }
        [JsonProperty("collectChanceBp")] public int CollectChanceBp { get; set; }
        [JsonProperty("teaCollectBonusBp")] public int TeaCollectBonusBp { get; set; }
        [JsonProperty("defaultRepPenaltyCenti")] public int DefaultRepPenaltyCenti { get; set; }
        [JsonProperty("loyaltyBonusCenti")] public int LoyaltyBonusCenti { get; set; }
        [JsonProperty("teaCostCenti")] public int TeaCostCenti { get; set; }
        [JsonProperty("loyaltyDemandBp")] public int LoyaltyDemandBp { get; set; }
        [JsonProperty("loyaltyCapBp")] public int LoyaltyCapBp { get; set; }
        [JsonProperty("askChanceBp")] public int AskChanceBp { get; set; }
        [JsonProperty("refusedPenaltyCenti")] public int RefusedPenaltyCenti { get; set; }
        [JsonProperty("repayBonusBp")] public int RepayBonusBp { get; set; }
        [JsonProperty("trustPerVisitBp")] public int TrustPerVisitBp { get; set; }
        [JsonProperty("trustCapBp")] public int TrustCapBp { get; set; }
        [JsonProperty("chanceCapBp")] public int ChanceCapBp { get; set; }
    }

    public sealed class CuisineDto
    {
        [JsonProperty("id")] public string Id { get; set; }
        [JsonProperty("nameKey")] public string NameKey { get; set; }
        [JsonProperty("signature")] public SignatureDto Signature { get; set; }
        [JsonProperty("scoreAxis")] public ScoreAxisDto ScoreAxis { get; set; }
        /// <summary>The duration of the four slots, in basis points, summing to 10000.</summary>
        [JsonProperty("slotDurationsBp")] public List<int> SlotDurationsBp { get; set; }
        [JsonProperty("eatMs")] public int EatMs { get; set; }

        /// <summary>
        /// IS IT SELF SERVICE? In fast food no waiter comes to the table.
        ///
        /// This is the biggest structural difference between the cuisines:
        /// in fast food the customer orders at the counter and PAYS THERE,
        /// carries their own tray and finds their own table. There is no
        /// waiter in the hall - the person there is the CLEANER, collecting
        /// the trays that are left behind.
        /// </summary>
        [JsonProperty("selfService")] public bool SelfService { get; set; }

        /// <summary>
        /// WHICH roles are in the hall pool for this cuisine.
        ///
        /// If it is empty or missing, every hall role in economy.json
        /// applies (the old behaviour). In fast food there is NO waiter:
        /// cashier + cleaning.
        /// </summary>
        [JsonProperty("hallRoles")] public List<string> HallRoles { get; set; }

        /// <summary>
        /// This cuisine's demand multiplier, in basis points. 10000 = no change.
        ///
        /// Fast food is a game of VOLUME: more people come to the same
        /// number of tables. It was measured that this promise WAS NOT in
        /// the numbers - the two cuisines were serving almost the same
        /// number of groups (1945 against 1819).
        /// </summary>
        [JsonProperty("customerMultiplierBp")] public int CustomerMultiplierBp { get; set; }

        /// <summary>
        /// This cuisine's rent multiplier, in basis points. 10000 = no change.
        ///
        /// In real life too, chains sit in HIGH-TRAFFIC, expensive places -
        /// rent is the price of volume.
        ///
        /// But the effect is not intuitive: raising the rent RAISES the
        /// balance bot's closing cash, because the bot answers a cost by
        /// not expanding (docs/52 §1). The yardstick for this lever is not
        /// total cash but the DIFFERENCE between the two cuisines.
        /// </summary>
        [JsonProperty("rentMultiplierBp")] public int RentMultiplierBp { get; set; }
        /// <summary>Which dish groups play which role. docs/13.</summary>
        [JsonProperty("menuRoles")] public MenuRolesDto MenuRoles { get; set; }
    }

    public sealed class MenuRolesDto
    {
        [JsonProperty("main")] public List<string> Main { get; set; }
        [JsonProperty("side")] public List<string> Side { get; set; }
        [JsonProperty("drink")] public List<string> Drink { get; set; }
        [JsonProperty("dessert")] public List<string> Dessert { get; set; }
    }

    public sealed class IngredientDto
    {
        [JsonProperty("id")] public string Id { get; set; }
        [JsonProperty("nameKey")] public string NameKey { get; set; }
        [JsonProperty("shared")] public bool Shared { get; set; }
        [JsonProperty("cuisines")] public List<string> Cuisines { get; set; }
        [JsonProperty("basePrice")] public long BasePrice { get; set; }
        [JsonProperty("unit")] public string Unit { get; set; }
        [JsonProperty("perishable")] public bool Perishable { get; set; }
        [JsonProperty("spoilDays")] public int SpoilDays { get; set; }
        /// <summary>spring / summer / autumn / winter -> basis point multiplier.</summary>
        [JsonProperty("seasonModifierBp")]
        public Dictionary<string, int> SeasonModifierBp { get; set; }
        [JsonProperty("qualityPriceMultiplierBp")]
        public Dictionary<string, int> QualityPriceMultiplierBp { get; set; }
        [JsonProperty("qualitySatisfactionCenti")]
        public Dictionary<string, int> QualitySatisfactionCenti { get; set; }
    }

    public sealed class ArchetypeDto
    {
        [JsonProperty("id")] public string Id { get; set; }
        [JsonProperty("nameKey")] public string NameKey { get; set; }
        [JsonProperty("tier")] public string Tier { get; set; }
        [JsonProperty("weight")] public int Weight { get; set; }
        [JsonProperty("patienceMs")] public int PatienceMs { get; set; }
        [JsonProperty("priceSensitivityBp")] public int PriceSensitivityBp { get; set; }
        [JsonProperty("groupSizeMin")] public int GroupSizeMin { get; set; }
        [JsonProperty("groupSizeMax")] public int GroupSizeMax { get; set; }
        [JsonProperty("reputationWeight")] public int ReputationWeight { get; set; }
        [JsonProperty("tipChanceBp")] public int TipChanceBp { get; set; }
        [JsonProperty("arrivalWeightsBp")] public List<int> ArrivalWeightsBp { get; set; }
        [JsonProperty("wardrobe")] public List<string> Wardrobe { get; set; }
    }
}
