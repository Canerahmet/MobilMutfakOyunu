using System.Collections.Generic;
using Newtonsoft.Json;

namespace Lokanta.Content
{
    // docs/23-cekirdek-sozlesmesi.md 8.3: her yemek dort zorunlu parametre
    // tasir. Alan adlari acikca isaretli; yansima ad turetmiyor.

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
        /// <summary>Bu yemek icin istasyonun en az kaci kademesi gerekli.</summary>
        [JsonProperty("requiresStationTier")] public int RequiresStationTier { get; set; }
        /// <summary>Kilidin acilmasi icin gereken itibar, santi-puan.</summary>
        [JsonProperty("unlockReputationCenti")] public int UnlockReputationCenti { get; set; }
        [JsonProperty("ingredients")] public List<DishIngredientDto> Ingredients { get; set; }
        [JsonProperty("plating")] public PlatingDto Plating { get; set; }
    }

    /// <summary>
    /// Isimli duzenli musteri. docs/11: "isimli musteri tek bir kisidir,
    /// elle yazilmistir, hikayesi vardir ve hep ayni kisidir."
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
        [JsonProperty("veresiyeEligible")] public bool VeresiyeEligible { get; set; }
        [JsonProperty("story")] public List<StoryBeatDto> Story { get; set; }
    }

    public sealed class StoryBeatDto
    {
        [JsonProperty("beat")] public int Beat { get; set; }
        [JsonProperty("requiresVisits")] public int RequiresVisits { get; set; }
        [JsonProperty("requiresSatisfaction")] public int RequiresSatisfaction { get; set; }
        [JsonProperty("textKey")] public string TextKey { get; set; }
    }

    /// <summary>docs/23 8.2: mekanik kodda, sayilar veride.</summary>
    /// <summary>Personel isim havuzu. content/names.json.</summary>
    public sealed class NamesDto
    {
        [JsonProperty("staff")] public string[] Staff { get; set; }
    }

    /// <summary>Yil sonu mutfak ekseni. docs/13 cuisines/*.json.</summary>
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
        /// <summary>Dort dilimin suresi, baz puan, toplami 10000.</summary>
        [JsonProperty("slotDurationsBp")] public List<int> SlotDurationsBp { get; set; }
        [JsonProperty("eatMs")] public int EatMs { get; set; }

        /// <summary>
        /// SELF SERVIS MI. Fast food'da masaya garson gelmiyor.
        ///
        /// Mutfaklari ayiran en buyuk yapisal fark bu: hizli yemekte
        /// musteri tezgahta siparis verip PARASINI ORADA odiyor, tepsisini
        /// kendi tasiyor, masasini kendi buluyor. Salonda garson yok -
        /// olan kisi TEMIZLIKCI: birakilan tepsileri topluyor.
        /// </summary>
        [JsonProperty("selfService")] public bool SelfService { get; set; }

        /// <summary>
        /// Bu mutfakta salon havuzunda HANGI roller var.
        ///
        /// Bos ya da yoksa economy.json'daki butun salon rolleri gecerli
        /// (eski davranis). Hizli yemekte garson YOK: kasiyer + temizlik.
        /// </summary>
        [JsonProperty("salonRoles")] public List<string> SalonRoles { get; set; }

        /// <summary>
        /// Bu mutfagin talep carpani, baz puan. 10000 = degisiklik yok.
        ///
        /// Fast food HACIM oyunu: ayni masa sayisina daha cok insan
        /// geliyor. Olculdu ki bu vaat sayilarda YOKTU - iki mutfak
        /// neredeyse ayni sayida grup agirliyordu (1945'e 1819).
        /// </summary>
        [JsonProperty("customerMultiplierBp")] public int CustomerMultiplierBp { get; set; }

        /// <summary>
        /// Bu mutfagin kira carpani, baz puan. 10000 = degisiklik yok.
        ///
        /// Gercekte de zincirler YUKSEK TRAFIKLI, pahali yerlerde oturur -
        /// hacmin bedeli kira.
        ///
        /// Ama etkisi sezgisel degil: kirayi artirmak denge botunun bitis
        /// kasasini ARTIRIYOR, cunku bot maliyete genislemeyerek cevap
        /// veriyor (docs/52 §1). Bu kolun olcutu toplam kasa degil, iki
        /// mutfak arasindaki FARK.
        /// </summary>
        [JsonProperty("rentMultiplierBp")] public int RentMultiplierBp { get; set; }
        /// <summary>Hangi yemek gruplari hangi rolu oynuyor. docs/13.</summary>
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
        /// <summary>ilkbahar / yaz / sonbahar / kis -> baz puan carpani.</summary>
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
