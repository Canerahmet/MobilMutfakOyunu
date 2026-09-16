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
    /// <summary>If the content is invalid the game does not open. No silent defaults.</summary>
    public sealed class ContentException : Exception
    {
        public ContentException(string message) : base(message) { }
    }

    public static class ContentLoader
    {
        public const string KitchenPool = "kitchen";
        // The VALUE is a content token: it has to match `"pool"` in
        // content/staff-roles.json, which tools/balance/export.py writes.
        // Both sides moved from "salon" to "hall" in the same change - a
        // loader that reads a pool name nothing writes finds no hall roles
        // at all, and says nothing about it.
        public const string HallPool = "hall";

        internal static readonly Regex IdPattern =
            new Regex("^[a-z0-9_]+$", RegexOptions.CultureInvariant);

        private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            // docs/23 4.2: every parse and every format uses the invariant culture.
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
        /// Loads content from a SOURCE. A folder, the inside of an APK,
        /// whatever it is. The validations are the same on both routes: the
        /// platform must not change whether content is valid.
        /// </summary>
        public static EconomyConfig LoadEconomy(IContentSource src)
        {
            if (src == null) throw new ArgumentNullException(nameof(src));

            EconomyDto economy = ReadJson<EconomyDto>(src, "economy.json");
            List<StaffRoleDto> roles = ReadJson<List<StaffRoleDto>>(src, "staff-roles.json");

            // Staff traits. The file is optional: a traitless staff runs
            // too (that is how the unit tests do it), but if it is there it
            // is validated IN FULL.
            List<TraitDto> traits = null;
            if (src.Exists("staff-traits.json"))
                traits = ReadJson<List<TraitDto>>(src, "staff-traits.json");

            return Build(economy, roles, traits);
        }

        public static EconomyConfig Build(EconomyDto economy, List<StaffRoleDto> roles,
                                          List<TraitDto> traitDtos = null)
        {
            if (economy == null) throw new ContentException("economy.json could not be read");
            if (roles == null || roles.Count == 0) throw new ContentException("staff-roles.json is empty");
            if (economy.Staffing == null) throw new ContentException("economy.json: no staffing block");

            ValidateRoles(roles);

            StaffRoleDto cook = null;
            StaffRoleDto hallLead = null;
            int hallWorkMicro = 0;
            long hallWageNumerator = 0;

            for (int i = 0; i < roles.Count; i++)
            {
                StaffRoleDto r = roles[i];
                if (string.Equals(r.Pool, KitchenPool, StringComparison.Ordinal))
                {
                    if (cook != null)
                        throw new ContentException("More than one kitchen role; the model assumes a single cook pool");
                    cook = r;
                }
                else if (string.Equals(r.Pool, HallPool, StringComparison.Ordinal))
                {
                    hallWorkMicro += r.WorkPerCustomerMicro;
                    hallWageNumerator += (long)r.WorkPerCustomerMicro * r.DailyWage;

                    // The hall pool is modelled as a single "person" (the
                    // work is summed and then divided), so there has to be a
                    // single experience ladder as well. If the roles drift
                    // apart it is unclear which one applies; rather than pick
                    // one silently, we refuse.
                    if (hallLead == null) hallLead = r;
                    else if (!SameLadder(hallLead.XpSpeedBp, r.XpSpeedBp))
                        throw new ContentException(
                            "The hall roles have different xpSpeedBp ladders (" +
                            hallLead.Id + " vs " + r.Id +
                            "); the hall pool assumes a single ladder");
                }
                else
                {
                    throw new ContentException("Unknown pool: " + r.Pool + " (role " + r.Id + ")");
                }
            }

            if (cook == null) throw new ContentException("No role in the kitchen pool");
            if (hallWorkMicro <= 0) throw new ContentException("No role in the hall pool");

            List<TierDto> tierDtos = economy.Staffing.Tiers;
            if (tierDtos == null || tierDtos.Count == 0)
                throw new ContentException("economy.json: staffing.tiers is empty");

            TierConfig[] tiers = new TierConfig[tierDtos.Count];
            for (int i = 0; i < tierDtos.Count; i++)
            {
                TierDto t = tierDtos[i];
                if (i > 0)
                {
                    if (t.Tables <= tierDtos[i - 1].Tables)
                        throw new ContentException("staffing.tiers: the table count must increase");
                    if (t.StaffCap < tierDtos[i - 1].StaffCap)
                        throw new ContentException("staffing.tiers: the staff cap cannot fall");
                }
                // Reputation cap: unlimited if it is not written down. An
                // older content file may not carry this field and its absence
                // must not stop the game - in that case the behaviour stays
                // exactly as it was.
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
                hallWorkMicro,
                hallWageNumerator,
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
                economy.PriceElasticityBp > 0 ? economy.PriceElasticityBp : 9000,
                economy.DemandVarianceBp,
                economy.AttendWorkCutBp)
                .WithXpSpeed(Ladder(cook.XpSpeedBp),
                             Ladder(hallLead != null ? hallLead.XpSpeedBp : null),
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

        // docs/14 "Experience and level": 1 point for every day worked, a
        // level every 30 points, 3 levels at most. Neither number is in
        // content, because neither belongs to balance but to DESIGN; if they
        // change, docs/14 changes.
        private const int XpDaysPerLevel = 30;
        private const int MaxXpLevel = 3;

        private static int[] Ladder(List<int> values)
        {
            if (values == null || values.Count == 0) return null;
            int[] a = values.ToArray();
            if (a[0] != 10000)
                throw new ContentException(
                    "the first rung of xpSpeedBp must be 10000 (level 0 = no speed-up), found " + a[0]);
            for (int i = 1; i < a.Length; i++)
                if (a[i] < a[i - 1])
                    throw new ContentException("the xpSpeedBp ladder cannot fall");
            if (a.Length != MaxXpLevel + 1)
                throw new ContentException(
                    "xpSpeedBp must have " + (MaxXpLevel + 1) + " rungs (level 0.." +
                    MaxXpLevel + "), found " + a.Length);
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
        /// ownerPool was not being read, because the code already ASSUMES
        /// "hall": StaffingModel takes the owner's working day off the hall
        /// load, DispatchHall runs the owner as waiter number zero, and
        /// docs/14 forbids the owner from working in the kitchen.
        ///
        /// So the field is not a CHOICE, it is an ASSUMPTION written down.
        /// Deleting it would have made the assumption invisible; reading it
        /// would have meant writing a second pool, and the design does not
        /// want one. The third way: it is an invariant. If content writes
        /// any other pool, the game does not open.
        /// </summary>
        private static void ValidateOwnerPool(StaffingDto st)
        {
            if (string.IsNullOrEmpty(st.OwnerPool)) return;
            if (!string.Equals(st.OwnerPool, HallPool, StringComparison.Ordinal))
                throw new ContentException(
                    "staffing.ownerPool '" + st.OwnerPool + "'; the simulation only supports '" +
                    HallPool + "' (docs/14: the owner does not work in the kitchen)");
        }

        /// <summary>
        /// weeklyWageMultiplierBp is weeklyXpWageGrowthBp compounded. That
        /// is, the same thing is written in TWO PLACES - and a thing written
        /// in two places drifts apart silently. The table is not deleted
        /// (the balance tool and the interface read it) but it is now an
        /// INVARIANT: at start-up it is checked against its generator.
        /// </summary>
        private static void ValidateWageTable(StaffingDto st)
        {
            List<int> table = st.WeeklyWageMultiplierBp;
            if (table == null || table.Count == 0) return;
            if (table[0] != 10000)
                throw new ContentException(
                    "weeklyWageMultiplierBp must be 10000 in the first week, found " + table[0]);

            for (int w = 1; w < table.Count; w++)
            {
                long nano = Fx.PowNano(Fx.Nano + Fx.BpToNano(st.WeeklyXpWageGrowthBp), w);
                int expected = (int)Fx.MulDiv(10000, nano, Fx.Nano);
                // A one basis point tolerance: the table is worked out in
                // Python, the check in C#. If the gap grows beyond that, the
                // two sides have drifted apart.
                int diff = table[w] - expected;
                if (diff < 0) diff = -diff;
                if (diff > 1)
                    throw new ContentException(
                        "weeklyWageMultiplierBp[" + w + "] = " + table[w] +
                        " but with weeklyXpWageGrowthBp " + st.WeeklyXpWageGrowthBp +
                        " it works out to " + expected);
            }
        }

        /// <summary>
        /// Staff traits. docs/13 staff-traits.json, the docs/14 table.
        ///
        /// The conflict list is turned into INDICES and its symmetry is
        /// checked: if A conflicts with B then B must conflict with A. A
        /// conflict written in one direction only leaves a rule in the
        /// hiring code that silently does nothing - which means a member of
        /// staff carrying two conflicting traits could have been generated.
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
                    throw new ContentException("Invalid trait id: " + d.Id);
                if (index.ContainsKey(d.Id))
                    throw new ContentException("Duplicate trait: " + d.Id);
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
                            "Trait " + dtos[i].Id + " conflicts with a trait that does not exist: " + conf[k]);
                    idx[k] = j;

                    List<string> back = dtos[j].ConflictsWith;
                    if (back == null || !back.Contains(dtos[i].Id))
                        throw new ContentException(
                            "The conflict is one-way: " + dtos[i].Id + " -> " + conf[k]);
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
                    throw new ContentException("Invalid id: '" + r.Id + "'. The rule: [a-z0-9_]+");
                if (!seen.Add(r.Id))
                    throw new ContentException("Duplicate id: " + r.Id);
                if (r.CapacityPerDay <= 0)
                    throw new ContentException("Role " + r.Id + ": capacityPerDay must be positive");
                if (r.WorkPerCustomerMicro <= 0)
                    throw new ContentException("Role " + r.Id + ": workPerCustomerMicro must be positive");
                if (r.DailyWage <= 0)
                    throw new ContentException("Role " + r.Id + ": dailyWage must be positive");
            }
        }

        /// <summary>
        /// docs/23 8.4: decimal numbers are banned in JSON. The loader
        /// refuses a file that has one. The decimal point is searched for
        /// textually; ids and strings stay out of the search.
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
                        fileName + ": a decimal number was found (position " + i.ToString(CultureInfo.InvariantCulture)
                        + "). Every number must be an integer. See docs/23-core-contract.md 2.2");
                }
            }
        }

        private static T ReadJson<T>(string path)
        {
            return ReadJson<T>(new DirectoryContentSource(Path.GetDirectoryName(path)),
                               Path.GetFileName(path));
        }

        /// <summary>
        /// A flat key-value string table. For the localisation file.
        ///
        /// AssertNoDecimals DOES NOT RUN HERE, on purpose: that rule exists
        /// so that numbers stay integers (docs/23 8.4), and in a string file
        /// a full stop is just the end of a sentence. Applying the same
        /// check here would reject every string that reads "Done.".
        /// </summary>
        public static Dictionary<string, string> ReadStringMap(IContentSource src, string rel)
        {
            if (src == null) throw new ArgumentNullException(nameof(src));
            if (!src.Exists(rel))
                throw new ContentException("No such file: " + src.Describe(rel));

            var map = JsonConvert.DeserializeObject<Dictionary<string, string>>(
                src.ReadText(rel), Settings);
            if (map == null || map.Count == 0)
                throw new ContentException("Empty string table: " + src.Describe(rel));
            return map;
        }

        internal static T ReadJson<T>(IContentSource src, string rel)
        {
            if (!src.Exists(rel))
                throw new ContentException("No such file: " + src.Describe(rel));

            string text = src.ReadText(rel);
            AssertNoDecimals(text, rel);

            T result = JsonConvert.DeserializeObject<T>(text, Settings);
            if (result == null)
                throw new ContentException("Empty or invalid JSON: " + src.Describe(rel));
            return result;
        }

        /// <summary>Reads content files that are shaped as a list.</summary>
        public static List<T> ReadJsonList<T>(string path)
        {
            return ReadJson<List<T>>(path);
        }

        /// <summary>Reads content files that are shaped as a single object.</summary>
        public static T ReadJsonObject<T>(string path)
        {
            return ReadJson<T>(path);
        }

        /// <summary>
        /// docs/23 4.2 and 9.1: ids are lower-case ASCII.
        /// Because in the Turkish culture "ID".ToLower() gives a dotless i
        /// and not "id", an id must never contain a capital at all.
        /// </summary>
        public static void RequireId(string id, string where)
        {
            if (string.IsNullOrEmpty(id) || !IdPattern.IsMatch(id))
                throw new ContentException(
                    where + ": invalid id '" + id + "'. The rule: [a-z0-9_]+");
        }
    }
}
