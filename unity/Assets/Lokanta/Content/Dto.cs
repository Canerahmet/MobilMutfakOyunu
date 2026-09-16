using System.Collections.Generic;
using Newtonsoft.Json;

namespace Lokanta.Content
{
    // docs/23-core-contract.md 6.1: her alan acikca isaretlenir,
    // yansima ad turetmez. IL2CPP budamasi icin link.xml bu tipleri korur.

    public sealed class TierDto
    {
        [JsonProperty("tables")] public int Tables { get; set; }
        [JsonProperty("rent")] public long Rent { get; set; }
        [JsonProperty("upgrade")] public long Upgrade { get; set; }
        [JsonProperty("staffCap")] public int StaffCap { get; set; }
        [JsonProperty("reputationCapCenti")] public int ReputationCapCenti { get; set; }

        /// <summary>Bu kademedeki tabak sayisi. Tabak dongusunun toplami.</summary>
        [JsonProperty("plates")] public int Plates { get; set; }
    }

    /// <summary>Personel huyu. docs/13 staff-traits.json.</summary>
    public sealed class TraitDto
    {
        [JsonProperty("id")] public string Id { get; set; }
        [JsonProperty("nameKey")] public string NameKey { get; set; }
        [JsonProperty("effects")] public Dictionary<string, int> Effects { get; set; }
        [JsonProperty("conflictsWith")] public List<string> ConflictsWith { get; set; }
    }

    /// <summary>Moral ayarlari. docs/14 "Moral" tablosu.</summary>
    public sealed class MoraleDto
    {
        [JsonProperty("starting")] public int Starting { get; set; }
        [JsonProperty("lowThreshold")] public int LowThreshold { get; set; }
        [JsonProperty("quitThreshold")] public int QuitThreshold { get; set; }
        [JsonProperty("quitChanceBp")] public int QuitChanceBp { get; set; }
        [JsonProperty("slowPenaltyBp")] public int SlowPenaltyBp { get; set; }
        [JsonProperty("paidDelta")] public int PaidDelta { get; set; }
        [JsonProperty("lateDelta")] public int LateDelta { get; set; }
        [JsonProperty("busyDelta")] public int BusyDelta { get; set; }
        [JsonProperty("recoveryDelta")] public int RecoveryDelta { get; set; }
    }

    /// <summary>Patron mudahalesi ayarlari. docs/12 5.4.</summary>
    public sealed class InterventionDto
    {
        [JsonProperty("attentionSatisfactionCenti")]
        public int AttentionSatisfactionCenti { get; set; }
        [JsonProperty("treatSatisfactionCenti")]
        public int TreatSatisfactionCenti { get; set; }
        [JsonProperty("rushCutBp")] public int RushCutBp { get; set; }
        [JsonProperty("attentionPatienceMult")] public int AttentionPatienceMult { get; set; }
        [JsonProperty("treatPatienceMult")] public int TreatPatienceMult { get; set; }
    }

    /// <summary>Isimli duzenli musteri ayarlari. docs/11.</summary>
    public sealed class RegularsDto
    {
        [JsonProperty("visitChanceBp")] public int VisitChanceBp { get; set; }
        [JsonProperty("missedFavouriteCenti")] public int MissedFavouriteCenti { get; set; }
        [JsonProperty("upsetCenti")] public int UpsetCenti { get; set; }
        [JsonProperty("awayDays")] public int AwayDays { get; set; }
    }

    public sealed class StaffingDto
    {
        [JsonProperty("ownerPool")] public string OwnerPool { get; set; }
        [JsonProperty("ownerWorkMicro")] public int OwnerWorkMicro { get; set; }
        [JsonProperty("weeklyXpWageGrowthBp")] public int WeeklyXpWageGrowthBp { get; set; }
        [JsonProperty("weeklyWageMultiplierBp")] public List<int> WeeklyWageMultiplierBp { get; set; }
        [JsonProperty("tiers")] public List<TierDto> Tiers { get; set; }
    }

    public sealed class OrderDto
    {
        [JsonProperty("sideChanceBp")] public int SideChanceBp { get; set; }
        [JsonProperty("drinkChanceBp")] public int DrinkChanceBp { get; set; }
        [JsonProperty("dessertChanceBp")] public int DessertChanceBp { get; set; }
        [JsonProperty("askChanceBp")] public int AskChanceBp { get; set; }
        [JsonProperty("askMissCenti")] public int AskMissCenti { get; set; }
        [JsonProperty("kitchenMsPerPerson")] public int KitchenMsPerPerson { get; set; }
    }

    public sealed class EconomyDto
    {
        [JsonProperty("schemaVersion")] public int SchemaVersion { get; set; }

        [JsonProperty("startingCash")] public long StartingCash { get; set; }
        [JsonProperty("startingReputationCenti")] public int StartingReputationCenti { get; set; }
        [JsonProperty("campaignDays")] public int CampaignDays { get; set; }
        [JsonProperty("seasonDays")] public int SeasonDays { get; set; }
        [JsonProperty("rentDayInterval")] public int RentDayInterval { get; set; }
        [JsonProperty("weekendDaysPerWeek")] public int WeekendDaysPerWeek { get; set; }

        [JsonProperty("reputationDecayPerDayCenti")] public int ReputationDecayPerDayCenti { get; set; }
        [JsonProperty("satisfactionNeutralCenti")] public int SatisfactionNeutralCenti { get; set; }

        [JsonProperty("customerBasePerTable")] public int CustomerBasePerTable { get; set; }
        [JsonProperty("weekdayMultiplierBp")] public int WeekdayMultiplierBp { get; set; }
        [JsonProperty("weekendMultiplierBp")] public int WeekendMultiplierBp { get; set; }

        [JsonProperty("ingredientRateBp")] public int IngredientRateBp { get; set; }
        [JsonProperty("serviceMs")] public int ServiceMs { get; set; }
        [JsonProperty("interventionsPerDay")] public int InterventionsPerDay { get; set; }

        [JsonProperty("loanMultiplierBp")] public int LoanMultiplierBp { get; set; }
        [JsonProperty("loanWeeks")] public int LoanWeeks { get; set; }
        [JsonProperty("loanOptions")] public List<long> LoanOptions { get; set; }

        [JsonProperty("regulars")] public RegularsDto Regulars { get; set; }
        [JsonProperty("morale")] public MoraleDto Morale { get; set; }
        [JsonProperty("intervention")] public InterventionDto Intervention { get; set; }
        [JsonProperty("realisationBp")] public int RealisationBp { get; set; }
        [JsonProperty("priceVolatilityBp")] public int PriceVolatilityBp { get; set; }
        [JsonProperty("underpriceFloorBp")] public int UnderpriceFloorBp { get; set; }
        [JsonProperty("overpriceCeilingBp")] public int OverpriceCeilingBp { get; set; }
        [JsonProperty("priceElasticityBp")] public int PriceElasticityBp { get; set; }
        [JsonProperty("demandVarianceBp")] public int DemandVarianceBp { get; set; }
        [JsonProperty("attendWorkCutBp")] public int AttendWorkCutBp { get; set; }

        [JsonProperty("order")] public OrderDto Order { get; set; }
        [JsonProperty("staffing")] public StaffingDto Staffing { get; set; }
    }

    /// <summary>content/equipment.json. Uretilen dosya, elle degistirilmez.</summary>
    public sealed class EquipmentFileDto
    {
        [JsonProperty("schemaVersion")] public int SchemaVersion { get; set; }
        [JsonProperty("stations")] public List<StationDto> Stations { get; set; }
        /// <summary>Mutfaga ozel adlandirilmis ekipman: mutfak kimligi -> istasyonlar.</summary>
        [JsonProperty("cuisineStations")]
        public Dictionary<string, List<StationDto>> CuisineStations { get; set; }
        [JsonProperty("storage")] public StorageDto Storage { get; set; }
    }

    public sealed class StorageDto
    {
        [JsonProperty("nameKey")] public string NameKey { get; set; }
        [JsonProperty("tiers")] public List<StorageTierDto> Tiers { get; set; }
    }

    public sealed class StorageTierDto
    {
        [JsonProperty("tier")] public int Tier { get; set; }
        [JsonProperty("keepBp")] public int KeepBp { get; set; }
        [JsonProperty("price")] public long Price { get; set; }
    }

    public sealed class StationDto
    {
        [JsonProperty("id")] public string Id { get; set; }
        [JsonProperty("nameKey")] public string NameKey { get; set; }
        [JsonProperty("tiers")] public List<StationTierDto> Tiers { get; set; }
        /// <summary>Bu ekipmanin actigi yemekler. Yalnizca dogrulama icin.</summary>
        [JsonProperty("opens")] public List<string> Opens { get; set; }
    }

    public sealed class StationTierDto
    {
        [JsonProperty("tier")] public int Tier { get; set; }
        [JsonProperty("slots")] public int Slots { get; set; }
        [JsonProperty("attendBp")] public int AttendBp { get; set; }
        [JsonProperty("price")] public long Price { get; set; }
        [JsonProperty("neededAtTables")] public int NeededAtTables { get; set; }
    }

    public sealed class StaffRoleDto
    {
        [JsonProperty("id")] public string Id { get; set; }
        [JsonProperty("nameKey")] public string NameKey { get; set; }
        [JsonProperty("pool")] public string Pool { get; set; }
        [JsonProperty("capacityPerDay")] public int CapacityPerDay { get; set; }
        [JsonProperty("workPerCustomerMicro")] public int WorkPerCustomerMicro { get; set; }
        [JsonProperty("dailyWage")] public long DailyWage { get; set; }
        [JsonProperty("stations")] public List<string> Stations { get; set; }
        [JsonProperty("xpSpeedBp")] public List<int> XpSpeedBp { get; set; }
    }
}
