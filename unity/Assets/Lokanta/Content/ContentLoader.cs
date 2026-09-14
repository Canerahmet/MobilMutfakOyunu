using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.IO;
using System.Text.RegularExpressions;
using Lokanta.Core;
using Lokanta.Core.Economy;
using Newtonsoft.Json;

namespace Lokanta.Content
{
    /// <summary>Icerik gecersizse oyun acilmaz. Sessiz varsayilan yok.</summary>
    public sealed class ContentException : Exception
    {
        public ContentException(string message) : base(message) { }
    }

    public static class ContentLoader
    {
        public const string KitchenPool = "kitchen";
        public const string SalonPool = "salon";

        internal static readonly Regex IdPattern =
            new Regex("^[a-z0-9_]+$", RegexOptions.CultureInvariant);

        private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            // docs/23 4.2: her ayristirma ve bicimleme degismez kulturde.
            Culture = CultureInfo.InvariantCulture,
            FloatParseHandling = FloatParseHandling.Decimal,
            MissingMemberHandling = MissingMemberHandling.Ignore,
            DateParseHandling = DateParseHandling.None
        };

        public static EconomyConfig LoadEconomy(string contentDirectory)
        {
            if (string.IsNullOrEmpty(contentDirectory))
                throw new ArgumentNullException(nameof(contentDirectory));
            return LoadEconomy(new DirectoryContentSource(contentDirectory));
        }

        /// <summary>
        /// Icerigi bir KAYNAKTAN yukler. Klasor, APK ici, ne olursa.
        /// Dogrulamalar her iki yolda da ayni: platform, iceriğin gecerli
        /// olup olmadigini degistirmemeli.
        /// </summary>
        public static EconomyConfig LoadEconomy(IContentSource src)
        {
            if (src == null) throw new ArgumentNullException(nameof(src));

            EconomyDto economy = ReadJson<EconomyDto>(src, "economy.json");
            List<StaffRoleDto> roles = ReadJson<List<StaffRoleDto>>(src, "staff-roles.json");

            // Personel huylari. Dosya istege bagli: huysuz bir kadro da
            // kosuyor (birim testleri boyle), ama varsa TAMAMEN dogrulaniyor.
            List<TraitDto> traits = null;
            if (src.Exists("staff-traits.json"))
                traits = ReadJson<List<TraitDto>>(src, "staff-traits.json");

            return Build(economy, roles, traits);
        }

        public static EconomyConfig Build(EconomyDto economy, List<StaffRoleDto> roles,
                                          List<TraitDto> traitDtos = null)
        {
            if (economy == null) throw new ContentException("economy.json okunamadi");
            if (roles == null || roles.Count == 0) throw new ContentException("staff-roles.json bos");
            if (economy.Staffing == null) throw new ContentException("economy.json: staffing blogu yok");

            ValidateRoles(roles);

            StaffRoleDto cook = null;
            StaffRoleDto salonLead = null;
            int salonWorkMicro = 0;
            long salonWageNumerator = 0;

            for (int i = 0; i < roles.Count; i++)
            {
                StaffRoleDto r = roles[i];
                if (string.Equals(r.Pool, KitchenPool, StringComparison.Ordinal))
                {
                    if (cook != null)
                        throw new ContentException("Birden fazla mutfak rolu var; model tek asci havuzu varsayiyor");
                    cook = r;
                }
                else if (string.Equals(r.Pool, SalonPool, StringComparison.Ordinal))
                {
                    salonWorkMicro += r.WorkPerCustomerMicro;
                    salonWageNumerator += (long)r.WorkPerCustomerMicro * r.DailyWage;

                    // Salon havuzu tek bir "kisi" gibi modelleniyor (isler
                    // toplanip bolunuyor), o yuzden deneyim merdiveni de tek
                    // olmali. Roller ayrisirsa hangisinin gecerli oldugu
                    // belirsiz kalir; sessiz secmek yerine reddediyoruz.
                    if (salonLead == null) salonLead = r;
                    else if (!SameLadder(salonLead.XpSpeedBp, r.XpSpeedBp))
                        throw new ContentException(
                            "Salon rollerinin xpSpeedBp merdivenleri farkli (" +
                            salonLead.Id + " vs " + r.Id +
                            "); salon havuzu tek merdiven varsayiyor");
                }
                else
                {
                    throw new ContentException("Bilinmeyen havuz: " + r.Pool + " (rol " + r.Id + ")");
                }
            }

            if (cook == null) throw new ContentException("Mutfak havuzunda rol yok");
            if (salonWorkMicro <= 0) throw new ContentException("Salon havuzunda rol yok");

            List<TierDto> tierDtos = economy.Staffing.Tiers;
            if (tierDtos == null || tierDtos.Count == 0)
                throw new ContentException("economy.json: staffing.tiers bos");

            TierConfig[] tiers = new TierConfig[tierDtos.Count];
            for (int i = 0; i < tierDtos.Count; i++)
            {
                TierDto t = tierDtos[i];
                if (i > 0)
                {
                    if (t.Tables <= tierDtos[i - 1].Tables)
                        throw new ContentException("staffing.tiers: masa sayisi artan olmali");
                    if (t.StaffCap < tierDtos[i - 1].StaffCap)
                        throw new ContentException("staffing.tiers: kadro tavani azalamaz");
                }
                // Itibar tavani: yazilmamissa sinirsiz. Eski bir icerik
                // dosyasi bu alani tasimiyor olabilir ve yokluğu oyunu
                // durdurmamali - o durumda davranis eskisiyle ayni kaliyor.
                int repCap = t.ReputationCapCenti > 0 ? t.ReputationCapCenti : 10000;
                tiers[i] = new TierConfig(t.Tables, t.Rent, t.Upgrade, t.StaffCap,
                                          repCap, t.Plates);
            }

            ValidateWageTable(economy.Staffing);
            ValidateOwnerPool(economy.Staffing);

            return new EconomyConfig(
                economy.StartingCash,
                economy.StartingReputationCenti,
                economy.CampaignDays,
                economy.WeekendDaysPerWeek,
                economy.CustomerBasePerTable,
                economy.WeekdayMultiplierBp,
                economy.WeekendMultiplierBp,
                economy.IngredientRateBp,
                cook.CapacityPerDay,
                cook.DailyWage,
                salonWorkMicro,
                salonWageNumerator,
                economy.Staffing.OwnerWorkMicro,
                economy.Staffing.WeeklyXpWageGrowthBp,
                tiers,
                economy.ReputationDecayPerDayCenti,
                economy.SeasonDays > 0 ? economy.SeasonDays : 15,
                economy.ServiceMs,
                economy.RentDayInterval,
                economy.InterventionsPerDay,
                200,
                economy.PriceVolatilityBp,
                economy.SatisfactionNeutralCenti,
                economy.UnderpriceFloorBp,
                economy.LoanMultiplierBp,
                economy.LoanWeeks,
                economy.LoanOptions != null ? economy.LoanOptions.ToArray() : null,
                economy.Order != null ? economy.Order.SideChanceBp : 3000,
                economy.Order != null ? economy.Order.DrinkChanceBp : 4000,
                economy.Order != null ? economy.Order.DessertChanceBp : 1800,
                economy.Order != null && economy.Order.AskChanceBp > 0
                    ? economy.Order.AskChanceBp : 2500,
                economy.Order != null && economy.Order.AskMissCenti > 0
                    ? economy.Order.AskMissCenti : 1500,
                economy.RealisationBp > 0 ? economy.RealisationBp : 10000,
                economy.OverpriceCeilingBp > 0 ? economy.OverpriceCeilingBp : 25000,
                economy.PriceElasticityBp > 0 ? economy.PriceElasticityBp : 9000)
                .WithXpSpeed(Ladder(cook.XpSpeedBp),
                             Ladder(salonLead != null ? salonLead.XpSpeedBp : null),
                             XpDaysPerLevel, MaxXpLevel)
                .WithRegulars(
                    economy.Regulars != null ? economy.Regulars.VisitChanceBp : 0,
                    economy.Regulars != null ? economy.Regulars.MissedFavouriteCenti : 900,
                    economy.Regulars != null ? economy.Regulars.UpsetCenti : 5000,
                    economy.Regulars != null ? economy.Regulars.AwayDays : 0)
                .WithTraits(BuildTraits(traitDtos))
                .WithMorale(
                    economy.Morale != null && economy.Morale.Starting > 0
                        ? economy.Morale.Starting : 70,
                    economy.Morale != null ? economy.Morale.LowThreshold : 30,
                    economy.Morale != null ? economy.Morale.QuitThreshold : 15,
                    economy.Morale != null && economy.Morale.QuitChanceBp > 0
                        ? economy.Morale.QuitChanceBp : 1000,
                    economy.Morale != null ? economy.Morale.SlowPenaltyBp : 2000,
                    economy.Morale != null ? economy.Morale.PaidDelta : 5,
                    economy.Morale != null ? economy.Morale.LateDelta : -25,
                    economy.Morale != null ? economy.Morale.BusyDelta : -3,
                    economy.Morale != null ? economy.Morale.RecoveryDelta : 2)
                .WithIntervention(
                    economy.Intervention != null
                        ? economy.Intervention.AttentionSatisfactionCenti : 0,
                    economy.Intervention != null
                        ? economy.Intervention.TreatSatisfactionCenti : 0,
                    economy.Intervention != null ? economy.Intervention.RushCutBp : 0,
                    economy.Intervention != null
                        ? economy.Intervention.AttentionPatienceMult : 0,
                    economy.Intervention != null
                        ? economy.Intervention.TreatPatienceMult : 0);
        }

        // docs/14 "Deneyim ve seviye": calisilan her gun 1 puan, 30 puanda
        // seviye, azami 3 seviye. Ikisi de icerikte degil cunku ikisi de
        // dengeye degil TASARIMA ait; degisirlerse docs/14 degisir.
        private const int XpDaysPerLevel = 30;
        private const int MaxXpLevel = 3;

        private static int[] Ladder(List<int> values)
        {
            if (values == null || values.Count == 0) return null;
            int[] a = values.ToArray();
            if (a[0] != 10000)
                throw new ContentException(
                    "xpSpeedBp ilk basamagi 10000 olmali (seviye 0 = hizsiz), " + a[0] + " bulundu");
            for (int i = 1; i < a.Length; i++)
                if (a[i] < a[i - 1])
                    throw new ContentException("xpSpeedBp merdiveni azalamaz");
            if (a.Length != MaxXpLevel + 1)
                throw new ContentException(
                    "xpSpeedBp " + (MaxXpLevel + 1) + " basamak olmali (seviye 0.." +
                    MaxXpLevel + "), " + a.Length + " bulundu");
            return a;
        }

        private static bool SameLadder(List<int> a, List<int> b)
        {
            if (a == null || b == null) return a == b;
            if (a.Count != b.Count) return false;
            for (int i = 0; i < a.Count; i++) if (a[i] != b[i]) return false;
            return true;
        }

        /// <summary>
        /// ownerPool okunmuyordu cunku kod zaten "salon" VARSAYIYOR:
        /// StaffingModel patronun is gununu salon yukundan dusuyor,
        /// DispatchSalon patronu sifirinci garson olarak calistiriyor ve
        /// docs/14 patronun mutfakta calismasini yasakliyor.
        ///
        /// Yani alan bir SECIM degil, bir VARSAYIMIN yazili hali. Silmek
        /// varsayimi gorunmez yapardi; okumak icin ikinci bir havuz
        /// yazmak gerekirdi ve tasarim onu istemiyor. Ucuncu yol: degismez.
        /// Icerik baska bir havuz yazarsa oyun acilmiyor.
        /// </summary>
        private static void ValidateOwnerPool(StaffingDto st)
        {
            if (string.IsNullOrEmpty(st.OwnerPool)) return;
            if (!string.Equals(st.OwnerPool, SalonPool, StringComparison.Ordinal))
                throw new ContentException(
                    "staffing.ownerPool '" + st.OwnerPool + "'; simulasyon yalnizca '" +
                    SalonPool + "' destekliyor (docs/14: patron mutfakta calismaz)");
        }

        /// <summary>
        /// weeklyWageMultiplierBp, weeklyXpWageGrowthBp'nin bilesikleri.
        /// Yani ayni sey IKI YERDE yaziyor - ve iki yerde yazilan sey
        /// sessizce ayrisir. Tablo silinmiyor (denge araci ve arayuz onu
        /// okuyor) ama artik DEGISMEZ: acilista uretecine karsi dogrulaniyor.
        /// </summary>
        private static void ValidateWageTable(StaffingDto st)
        {
            List<int> table = st.WeeklyWageMultiplierBp;
            if (table == null || table.Count == 0) return;
            if (table[0] != 10000)
                throw new ContentException(
                    "weeklyWageMultiplierBp ilk hafta 10000 olmali, " + table[0] + " bulundu");

            for (int w = 1; w < table.Count; w++)
            {
                long nano = Fx.PowNano(Fx.Nano + Fx.BpToNano(st.WeeklyXpWageGrowthBp), w);
                int expected = (int)Fx.MulDiv(10000, nano, Fx.Nano);
                // Bir baz puanlik tolerans: tablo Python'da, dogrulama
                // C#'ta hesaplaniyor. Sapma buyurse iki taraf ayrismistir.
                int diff = table[w] - expected;
                if (diff < 0) diff = -diff;
                if (diff > 1)
                    throw new ContentException(
                        "weeklyWageMultiplierBp[" + w + "] = " + table[w] +
                        " ama weeklyXpWageGrowthBp " + st.WeeklyXpWageGrowthBp +
                        " ile " + expected + " cikiyor");
            }
        }

        /// <summary>
        /// Personel huylari. docs/13 staff-traits.json, docs/14 tablo.
        ///
        /// Cakisma listesi INDEKSE cevriliyor ve simetri dogrulaniyor:
        /// A ile B cakisiyorsa B ile A da cakismali. Tek yonlu yazilmis bir
        /// cakisma, ise alim kodunda sessizce calismayan bir kural birakir -
        /// yani iki cakisan huyu tasiyan bir personel uretilebilirdi.
        /// </summary>
        private static TraitDef[] BuildTraits(List<TraitDto> dtos)
        {
            if (dtos == null || dtos.Count == 0) return new TraitDef[0];

            TraitDef[] result = new TraitDef[dtos.Count];
            Dictionary<string, int> index = new Dictionary<string, int>(StringComparer.Ordinal);

            for (int i = 0; i < dtos.Count; i++)
            {
                TraitDto d = dtos[i];
                if (string.IsNullOrEmpty(d.Id) || !IdPattern.IsMatch(d.Id))
                    throw new ContentException("Gecersiz huy kimligi: " + d.Id);
                if (index.ContainsKey(d.Id))
                    throw new ContentException("Tekrarlanan huy: " + d.Id);
                index[d.Id] = i;

                Dictionary<string, int> e = d.Effects ?? new Dictionary<string, int>();
                result[i] = new TraitDef(
                    d.Id, d.NameKey ?? ("trait." + d.Id),
                    Eff(e, "speedBp"), Eff(e, "satisfactionCenti"),
                    Eff(e, "wageBp"),
                    e.ContainsKey("xpBp") ? e["xpBp"] : 10000,
                    Eff(e, "peakPenaltyBp"), Eff(e, "fatiguePenaltyBp"),
                    Eff(e, "moraleAura"),
                    Eff(e, "peakImmune") != 0, Eff(e, "fatigueImmune") != 0,
                    Eff(e, "qualityBp"), Eff(e, "cleanlinessBp"));
            }

            for (int i = 0; i < dtos.Count; i++)
            {
                List<string> conf = dtos[i].ConflictsWith;
                int n = conf == null ? 0 : conf.Count;
                int[] idx = new int[n];
                for (int k = 0; k < n; k++)
                {
                    if (!index.TryGetValue(conf[k], out int j))
                        throw new ContentException(
                            "Huy " + dtos[i].Id + " olmayan huyla cakisiyor: " + conf[k]);
                    idx[k] = j;

                    List<string> back = dtos[j].ConflictsWith;
                    if (back == null || !back.Contains(dtos[i].Id))
                        throw new ContentException(
                            "Cakisma tek yonlu: " + dtos[i].Id + " -> " + conf[k]);
                }
                result[i].BindConflicts(idx);
            }
            return result;
        }

        private static int Eff(Dictionary<string, int> e, string key)
        {
            return e.TryGetValue(key, out int v) ? v : 0;
        }

        private static void ValidateRoles(List<StaffRoleDto> roles)
        {
            HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < roles.Count; i++)
            {
                StaffRoleDto r = roles[i];
                if (string.IsNullOrEmpty(r.Id) || !IdPattern.IsMatch(r.Id))
                    throw new ContentException("Gecersiz kimlik: '" + r.Id + "'. Kural: [a-z0-9_]+");
                if (!seen.Add(r.Id))
                    throw new ContentException("Tekrarlanan kimlik: " + r.Id);
                if (r.CapacityPerDay <= 0)
                    throw new ContentException("Rol " + r.Id + ": capacityPerDay pozitif olmali");
                if (r.WorkPerCustomerMicro <= 0)
                    throw new ContentException("Rol " + r.Id + ": workPerCustomerMicro pozitif olmali");
                if (r.DailyWage <= 0)
                    throw new ContentException("Rol " + r.Id + ": dailyWage pozitif olmali");
            }
        }

        /// <summary>
        /// docs/23 8.4: JSON'da ondalik sayi yasak. Doguslayici gorurse reddeder.
        /// Ondalik nokta metinsel olarak aranir; kimlikler ve metinler disinda kalir.
        /// </summary>
        public static void AssertNoDecimals(string json, string fileName)
        {
            bool inString = false;
            bool escaped = false;
            for (int i = 0; i < json.Length; i++)
            {
                char c = json[i];
                if (escaped) { escaped = false; continue; }
                if (c == '\\') { escaped = true; continue; }
                if (c == '"') { inString = !inString; continue; }
                if (inString) continue;

                if (c == '.' && i > 0 && i + 1 < json.Length
                    && char.IsDigit(json[i - 1]) && char.IsDigit(json[i + 1]))
                {
                    throw new ContentException(
                        fileName + ": ondalik sayi bulundu (konum " + i.ToString(CultureInfo.InvariantCulture)
                        + "). Butun sayilar tamsayi olmali. Bkz. docs/23-cekirdek-sozlesmesi.md 2.2");
                }
            }
        }

        private static T ReadJson<T>(string path)
        {
            return ReadJson<T>(new DirectoryContentSource(Path.GetDirectoryName(path)),
                               Path.GetFileName(path));
        }

        /// <summary>
        /// Duz anahtar-deger metin tablosu. Yerellestirme dosyasi icin.
        ///
        /// AssertNoDecimals BURADA CALISMIYOR, bilerek: o kural sayilarin
        /// tamsayi olmasi icin (docs/23 8.4) ve metin dosyasinda nokta
        /// zaten cumle sonu. Ayni kontrolu buraya uygulamak, "Tamam." yazan
        /// her metni reddederdi.
        /// </summary>
        public static Dictionary<string, string> ReadStringMap(IContentSource src, string rel)
        {
            if (src == null) throw new ArgumentNullException(nameof(src));
            if (!src.Exists(rel))
                throw new ContentException("Dosya yok: " + src.Describe(rel));

            var map = JsonConvert.DeserializeObject<Dictionary<string, string>>(
                src.ReadText(rel), Settings);
            if (map == null || map.Count == 0)
                throw new ContentException("Bos metin tablosu: " + src.Describe(rel));
            return map;
        }

        internal static T ReadJson<T>(IContentSource src, string rel)
        {
            if (!src.Exists(rel))
                throw new ContentException("Dosya yok: " + src.Describe(rel));

            string text = src.ReadText(rel);
            AssertNoDecimals(text, rel);

            T result = JsonConvert.DeserializeObject<T>(text, Settings);
            if (result == null)
                throw new ContentException("Bos veya gecersiz JSON: " + src.Describe(rel));
            return result;
        }

        /// <summary>Liste bicimindeki icerik dosyalarini okur.</summary>
        public static List<T> ReadJsonList<T>(string path)
        {
            return ReadJson<List<T>>(path);
        }

        /// <summary>Tek nesne bicimindeki icerik dosyalarini okur.</summary>
        public static T ReadJsonObject<T>(string path)
        {
            return ReadJson<T>(path);
        }

        /// <summary>
        /// docs/23 4.2 ve 9.1: kimlikler kucuk harf ASCII.
        /// Turkce kulturde "ID".ToLower() "id" degil "ıd" verdigi icin
        /// kimliklerde buyuk harf hic bulunmamali.
        /// </summary>
        public static void RequireId(string id, string where)
        {
            if (string.IsNullOrEmpty(id) || !IdPattern.IsMatch(id))
                throw new ContentException(
                    where + ": gecersiz kimlik '" + id + "'. Kural: [a-z0-9_]+");
        }
    }
}
