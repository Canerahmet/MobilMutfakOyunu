using System;
using System.Collections.Generic;
using System.IO;
using Lokanta.Core.Content;

namespace Lokanta.Content
{
    /// <summary>
    /// Loads and validates all the content of one cuisine.
    /// docs/23-core-contract.md 9.1: if the content is invalid the game DOES
    /// NOT OPEN. No silent defaults, no dangling references.
    /// </summary>
    public static class ContentSetLoader
    {
        /// <summary>
        /// docs/23 8.3: the dish stations are a closed list. The order is
        /// binding; a dish's StationIndex value follows this order.
        /// </summary>
        public static readonly string[] StationIds =
        {
            "ocak", "izgara", "firin", "soguk", "icecek", "tatli"
        };

        // The VALUES are content tokens: the frequency tiers as archetypes/*.json
        // writes them (common, medium, rare).
        private static readonly string[] Tiers = { "sik", "orta", "nadir" };

        /// <summary>A gram's cost from the price per kilo: price x grams / 1000.</summary>
        private const int GramsPerKilo = 1000;

        public static ContentSet Load(string contentDirectory, string cuisine)
        {
            if (string.IsNullOrEmpty(contentDirectory))
                throw new ArgumentNullException(nameof(contentDirectory));
            return Load(new DirectoryContentSource(contentDirectory), cuisine);
        }

        /// <summary>Loads content from a SOURCE. See IContentSource.</summary>
        public static ContentSet Load(IContentSource src, string cuisine)
        {
            if (src == null) throw new ArgumentNullException(nameof(src));
            if (string.IsNullOrEmpty(cuisine))
                throw new ArgumentNullException(nameof(cuisine));

            List<IngredientDto> ingredientDtos =
                ContentLoader.ReadJson<List<IngredientDto>>(src, "ingredients.json");

            List<DishDto> dishDtos =
                ContentLoader.ReadJson<List<DishDto>>(src, "dishes/" + cuisine + ".json");

            List<ArchetypeDto> archetypeDtos =
                ContentLoader.ReadJson<List<ArchetypeDto>>(src, "archetypes/shared.json");
            archetypeDtos.AddRange(
                ContentLoader.ReadJson<List<ArchetypeDto>>(
                    src, "archetypes/" + cuisine + ".json"));

            // docs/23 9.1: the equipment file is MANDATORY. Without station
            // slots and attendBp the kitchen cannot be simulated, and there
            // are no silent defaults.
            EquipmentFileDto equipmentDto =
                ContentLoader.ReadJson<EquipmentFileDto>(src, "equipment.json");

            // Named regular customers. The file is optional: a cuisine runs
            // without regulars too (that is how the unit tests do it), but if
            // it is there it is validated IN FULL.
            List<RegularDto> regularDtos = null;
            string regularPath = "regulars/" + cuisine + ".json";
            if (src.Exists(regularPath))
                regularDtos = ContentLoader.ReadJson<List<RegularDto>>(src, regularPath);

            // The cuisine file is optional: without it, equal slots are assumed.
            CuisineDto cuisineDto = null;
            string cuisinePath = "cuisines/" + cuisine + ".json";
            if (src.Exists(cuisinePath))
                cuisineDto = ContentLoader.ReadJson<CuisineDto>(src, cuisinePath);

            // The season length lives in economy.json. The reason it is read
            // here is the unlockSeason invariant: the season is derived from
            // unlockDay, and that derivation cannot be checked without knowing
            // the season length. Rather than write a second constant we read
            // from the ONE source.
            int seasonDays = DefaultSeasonDays;
            if (src.Exists("economy.json"))
            {
                EconomyDto eco = ContentLoader.ReadJson<EconomyDto>(src, "economy.json");
                if (eco != null && eco.SeasonDays > 0) seasonDays = eco.SeasonDays;
            }

            List<StaffRoleDto> staffRoles =
                ContentLoader.ReadJson<List<StaffRoleDto>>(src, "staff-roles.json");

            return Build(cuisine, ingredientDtos, dishDtos, archetypeDtos,
                         equipmentDto, cuisineDto, seasonDays, regularDtos,
                         LoadStaffNames(src), staffRoles);
        }

        public static ContentSet Build(string cuisine,
                                       List<IngredientDto> ingredientDtos,
                                       List<DishDto> dishDtos,
                                       List<ArchetypeDto> archetypeDtos,
                                       EquipmentFileDto equipmentDto,
                                       CuisineDto cuisineDto = null,
                                       int seasonDays = DefaultSeasonDays,
                                       List<RegularDto> regularDtos = null,
                                       string[] staffNames = null,
                                       List<StaffRoleDto> staffRoles = null)
        {
            IngredientDef[] ingredients = BuildIngredients(ingredientDtos, out var index);
            // Stations come before DISHES: a dish's station field can now
            // point at a piece of equipment specific to the cuisine.
            StationDef[] stations = BuildStations(equipmentDto, cuisine);
            DishDef[] dishes = BuildDishes(dishDtos, ingredients, index, cuisine,
                                           stations, seasonDays);
            ArchetypeDef[] archetypes = BuildArchetypes(archetypeDtos);
            StorageDef storage = BuildStorage(equipmentDto);
            int[] slots = BuildSlotDurations(cuisineDto, cuisine);

            string[] main, side, drink, dessert;
            BuildMenuRoles(cuisineDto, cuisine, dishes,
                           out main, out side, out drink, out dessert);

            CheckCuisineStationsOpen(equipmentDto, cuisine, dishes, stations);
            CheckDishTiersExist(cuisine, dishes, stations);

            int eatMs = cuisineDto != null ? cuisineDto.EatMs : 0;

            // THE CUISINE'S HALL POOL.
            //
            // The SUM of the hall roles in economy.json was being used -
            // waiter + dishwasher + cashier. Because fast food is SELF
            // SERVICE this was wrong: the player was paying the wage of a
            // waiter who does no work, and the staffing model was asking for
            // a person to match.
            //
            // If the cuisine gives its own list (hallRoles) only those roles
            // are summed. If it gives none, this returns zero and the old
            // behaviour stays exactly as it was.
            int hallWork = 0;
            long hallWage = 0;
            // NO ROLES GIVEN MEANS NO OVERRIDE EITHER.
            //
            // `Build` is a pure DTO constructor; the caller is not obliged to
            // pass the roles (the tests do not). The first time I wrote this
            // I treated that as an error and SignatureTests broke - it blew
            // up with "hallRoles matched no role" before it could reach its
            // own assertion. "Not given" and "given but matched nothing" are
            // different things; only the second is an error.
            if (staffRoles != null && staffRoles.Count > 0
                && cuisineDto != null && cuisineDto.HallRoles != null
                && cuisineDto.HallRoles.Count > 0)
            {
                // THE ROLES COME IN AS A PARAMETER, not from a file:
                // `Build` takes DTOs and returns a ContentSet - reading the
                // source is the caller's job. Breaking that separation would
                // tie a pure constructor to the file system.
                for (int i = 0; staffRoles != null && i < staffRoles.Count; i++)
                {
                    StaffRoleDto r = staffRoles[i];
                    if (!cuisineDto.HallRoles.Contains(r.Id)) continue;
                    hallWork += r.WorkPerCustomerMicro;
                    hallWage += (long)r.WorkPerCustomerMicro * r.DailyWage;
                }
                if (hallWork <= 0)
                    throw new ContentException(
                        cuisine + ": hallRoles matched no role");
            }

            SignatureDef signature = BuildSignature(cuisineDto, cuisine, dishes, seasonDays);
            RegularDef[] regulars = BuildRegulars(regularDtos, cuisine, dishes,
                                                  archetypes, signature);

            return new ContentSet(cuisine, ingredients, dishes, archetypes, stations,
                                  storage, slots, eatMs, main, side, drink, dessert,
                                  signature, regulars, BuildScoreAxis(cuisineDto),
                                  staffNames,
                                  cuisineDto != null && cuisineDto.SelfService,
                                  hallWork, hallWage,
                                  cuisineDto != null ? cuisineDto.CustomerMultiplierBp : 0,
                                  cuisineDto != null ? cuisineDto.RentMultiplierBp : 0);
        }

        /// <summary>
        /// The staff name pool. If the file is missing this returns empty:
        /// names do not enter the game's rules, only its presentation.
        /// </summary>
        private static string[] LoadStaffNames(IContentSource src)
        {
            const string path = "names.json";
            if (!src.Exists(path)) return new string[0];

            NamesDto dto = ContentLoader.ReadJson<NamesDto>(src, path);
            if (dto == null || dto.Staff == null || dto.Staff.Length == 0)
                return new string[0];

            for (int i = 0; i < dto.Staff.Length; i++)
                if (string.IsNullOrWhiteSpace(dto.Staff[i]))
                    throw new ContentException("names.json: empty name at index " + i);

            return dto.Staff;
        }

        /// <summary>
        /// The cuisine's year-end axis. If there is none, a neutral axis is
        /// returned: a missing field must not stop the game, it just means
        /// that axis scores full marks.
        /// </summary>
        private static ScoreAxisDef BuildScoreAxis(CuisineDto dto)
        {
            if (dto == null || dto.ScoreAxis == null)
                return new ScoreAxisDef("none", "score.signature", 1);

            ScoreAxisDto a = dto.ScoreAxis;
            return new ScoreAxisDef(a.Kind, a.NameKey, a.Target);
        }

        /// <summary>
        /// DOES the equipment tier each dish asks for ACTUALLY EXIST?
        ///
        /// Three fast food desserts asked for "oven tier 2"; the oven ladder
        /// ends at tier 1. Those three dishes never unlocked across all sixty
        /// days and nothing complained - not the generator, not the loader,
        /// not the balance tool. Content had silently become unreachable.
        ///
        /// Better to FAIL TO LOAD than to stay quiet: the same choice as
        /// docs/23 9.1.
        /// </summary>
        private static void CheckDishTiersExist(string cuisine, DishDef[] dishes,
                                                StationDef[] stations)
        {
            for (int i = 0; i < dishes.Length; i++)
            {
                DishDef d = dishes[i];
                if (d.RequiresStationTier <= 0) continue;

                int st = d.StationIndex;
                if (st < 0 || st >= stations.Length)
                    throw new ContentException(
                        cuisine + "/" + d.Id + ": station not found");

                int top = stations[st].Tiers.Length - 1;
                if (d.RequiresStationTier > top)
                    throw new ContentException(
                        cuisine + "/" + d.Id + ": it asks for " + stations[st].Id
                        + " tier " + d.RequiresStationTier
                        + " but the ladder ends at tier " + top + "."
                        + " This dish can never be unlocked.");
            }
        }

        /// <summary>
        /// Named regular customers. docs/13 regulars/*.json.
        ///
        /// The validation is strict because this file is HAND-written
        /// content: archetype and dish names are open to typos, and a
        /// regular that drops out silently is never noticed - in the game
        /// "did not come today" and "does not exist" look the same.
        /// </summary>
        private static RegularDef[] BuildRegulars(List<RegularDto> dtos, string cuisine,
                                                  DishDef[] dishes,
                                                  ArchetypeDef[] archetypes,
                                                  SignatureDef signature)
        {
            if (dtos == null || dtos.Count == 0) return new RegularDef[0];

            RegularDef[] result = new RegularDef[dtos.Count];
            HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
            int lastDay = 0;

            for (int i = 0; i < dtos.Count; i++)
            {
                RegularDto d = dtos[i];
                if (string.IsNullOrEmpty(d.Id) || !ContentLoader.IdPattern.IsMatch(d.Id))
                    throw new ContentException("Invalid regular customer id: " + d.Id);
                if (!seen.Add(d.Id))
                    throw new ContentException("Duplicate regular customer: " + d.Id);
                if (!string.IsNullOrEmpty(d.Cuisine)
                    && !string.Equals(d.Cuisine, cuisine, StringComparison.Ordinal))
                    throw new ContentException(
                        "Regular customer " + d.Id + " belongs to another cuisine: " + d.Cuisine);

                int arch = IndexOfArchetype(archetypes, d.ArchetypeBase);
                if (arch < 0)
                    throw new ContentException(
                        "Regular customer " + d.Id + ": no such archetype " + d.ArchetypeBase);

                int dish = IndexOfDish(dishes, d.FavouriteDish);
                if (dish < 0)
                    throw new ContentException(
                        "Regular customer " + d.Id + ": no such dish " + d.FavouriteDish);

                if (d.ArrivesFromDay < 1)
                    throw new ContentException(
                        "Regular customer " + d.Id + ": arrivesFromDay is below 1");

                // Their favourite dish cannot be locked on the day they
                // arrive: greeting them with a gap the player cannot close is
                // unfair.
                if (dishes[dish].UnlockDay > d.ArrivesFromDay)
                    throw new ContentException(
                        "Regular customer " + d.Id + " arrives on day " + d.ArrivesFromDay +
                        " but their favourite dish unlocks on day " +
                        dishes[dish].UnlockDay);

                // Being in order is not a matter of style: the arrival
                // calendar has to be visible while reading the file, and the
                // code has to be able to stop at the first one not yet due.
                if (d.ArrivesFromDay < lastDay)
                    throw new ContentException(
                        "regulars/" + cuisine + ".json: arrival days must be in increasing order, " +
                        d.Id + " breaks it");
                lastDay = d.ArrivesFromDay;

                // The tab (credit) is the Turkish cuisine's signature
                // mechanic. Marking a regular as eligible in another cuisine
                // is writing a field that will never do anything.
                if (d.TabEligible && signature.Kind != SignatureKind.Credit)
                    throw new ContentException(
                        "Regular customer " + d.Id + " is eligible for a tab but " +
                        "the signature mechanic of the " + cuisine + " cuisine is not credit");

                result[i] = new RegularDef(d.Id, d.NameKey ?? ("regular." + d.Id + ".name"),
                                           d.JobKey ?? ("regular." + d.Id + ".job"),
                                           arch, dish, d.ArrivesFromDay,
                                           d.TabEligible,
                                           BuildStory(d));
            }
            return result;
        }

        private static StoryBeat[] BuildStory(RegularDto d)
        {
            if (d.Story == null || d.Story.Count == 0) return new StoryBeat[0];

            StoryBeat[] beats = new StoryBeat[d.Story.Count];
            int lastVisits = 0;
            for (int i = 0; i < d.Story.Count; i++)
            {
                StoryBeatDto b = d.Story[i];
                if (b.Beat != i + 1)
                    throw new ContentException(
                        "Regular customer " + d.Id + ": story beat numbers must count up from 1");
                if (b.RequiresVisits < lastVisits)
                    throw new ContentException(
                        "Regular customer " + d.Id + ": story beat visit thresholds cannot fall");
                lastVisits = b.RequiresVisits;
                if (b.RequiresSatisfaction < 0 || b.RequiresSatisfaction > Core.Fx.One)
                    throw new ContentException(
                        "Regular customer " + d.Id + ": a story beat satisfaction threshold must be 0-10000");
                beats[i] = new StoryBeat(b.Beat, b.RequiresVisits,
                                         b.RequiresSatisfaction, b.TextKey);
            }
            return beats;
        }

        private static int IndexOfArchetype(ArchetypeDef[] a, string id)
        {
            for (int i = 0; i < a.Length; i++)
                if (string.Equals(a[i].Id, id, StringComparison.Ordinal)) return i;
            return -1;
        }

        /// <summary>
        /// The signature mechanic. docs/23 8.2: "if the kind is unknown the
        /// validation refuses. If the block is missing the cuisine does not
        /// load."
        ///
        /// The reason it is strict is docs/07: the signature mechanic is the
        /// ONLY thing that separates one cuisine from another. A silent
        /// default would amount to saying "the cuisine you bought is
        /// actually the same game".
        /// </summary>
        private static SignatureDef BuildSignature(CuisineDto dto, string cuisine,
                                                   DishDef[] dishes,
                                                   int seasonDays)
        {
            // A cuisine with no cuisine file (the unit tests) has no signature.
            if (dto == null) return new SignatureDef(SignatureKind.None);

            // docs/09: the mechanic arrives on the first day of the second season.
            int fromDay = seasonDays + 1;

            SignatureDto sig = dto.Signature;
            if (sig == null || string.IsNullOrEmpty(sig.Kind))
                throw new ContentException(
                    "cuisines/" + cuisine + ".json: no signature block (docs/23 8.2)");

            switch (sig.Kind)
            {
                case "combo":
                    return BuildCombo(sig.Combo, cuisine, dishes, fromDay);
                case "credit":
                    return BuildCredit(sig.Credit, cuisine, fromDay);
                default:
                    throw new ContentException(
                        "cuisines/" + cuisine + ".json: unknown signature kind '" +
                        sig.Kind + "' (combo, credit)");
            }
        }

        private static SignatureDef BuildCombo(ComboDto c, string cuisine,
                                               DishDef[] dishes, int fromDay)
        {
            if (c == null || c.Items == null || c.Items.Count != 3)
                throw new ContentException(
                    cuisine + ": a combo must have exactly three items (main, side, drink)");
            if (c.PriceBp <= 0 || c.PriceBp >= Core.Fx.One)
                throw new ContentException(
                    cuisine + ": a combo priceBp must be a DISCOUNT between 0 and 10000, found " +
                    c.PriceBp);
            if (c.KitchenLoadBp < Core.Fx.One)
                throw new ContentException(
                    cuisine + ": a combo kitchenLoadBp cannot be below 10000; " +
                    "a combo does NOT EASE the kitchen (docs/07)");

            int[] idx = new int[3];
            for (int i = 0; i < 3; i++)
            {
                idx[i] = IndexOfDish(dishes, c.Items[i]);
                if (idx[i] < 0)
                    throw new ContentException(
                        cuisine + ": combo item not on the menu: " + c.Items[i]);
                // The items have to be unlocked BY THE TIME the mechanic
                // arrives. They do not have to be there on day one - docs/09
                // puts the mechanic in the second season - but a combo that is
                // still locked on the day it arrives can never be sold.
                if (dishes[idx[i]].UnlockDay > fromDay)
                    throw new ContentException(
                        cuisine + ": combo item '" + c.Items[i] + "' unlocks on day " +
                        dishes[idx[i]].UnlockDay + " but the mechanic arrives on day " +
                        fromDay);
            }
            return new SignatureDef(SignatureKind.Combo, fromDay,
                                    idx, c.PriceBp, c.KitchenLoadBp);
        }

        private static SignatureDef BuildCredit(CreditDto c, string cuisine, int fromDay)
        {
            if (c == null)
                throw new ContentException(cuisine + ": no credit block");
            if (c.MaxPerRegular <= 0)
                throw new ContentException(cuisine + ": credit maxPerRegular must be positive");
            if (c.DueDays <= 0)
                throw new ContentException(cuisine + ": credit dueDays must be positive");
            if (c.CollectChanceBp <= 0 || c.CollectChanceBp > Core.Fx.One)
                throw new ContentException(cuisine + ": credit collectChanceBp must be 1-10000");
            // A RISK-FREE LEDGER PRODUCES NO DECISION. If the cap climbs to
            // full certainty the tab becomes a free bonus button again.
            if (c.ChanceCapBp >= Core.Fx.One)
                throw new ContentException(cuisine + ": credit chanceCapBp must be below 10000");

            if (c.DefaultRepPenaltyCenti < 0)
                throw new ContentException(cuisine + ": credit defaultRepPenaltyCenti cannot be negative");

            return new SignatureDef(SignatureKind.Credit, fromDay,
                                    creditMaxPerRegular: c.MaxPerRegular,
                                    creditDueDays: c.DueDays,
                                    creditCollectChanceBp: c.CollectChanceBp,
                                    creditTeaCollectBonusBp: c.TeaCollectBonusBp,
                                    creditDefaultRepPenaltyCenti: c.DefaultRepPenaltyCenti,
                                    creditLoyaltyBonusCenti: c.LoyaltyBonusCenti,
                                    creditTeaCostCenti: c.TeaCostCenti,
                                    creditLoyaltyDemandBp: c.LoyaltyDemandBp,
                                    creditLoyaltyCapBp: c.LoyaltyCapBp,
                                    creditAskChanceBp: c.AskChanceBp,
                                    creditRefusedPenaltyCenti: c.RefusedPenaltyCenti,
                                    creditRepayBonusBp: c.RepayBonusBp,
                                    creditTrustPerVisitBp: c.TrustPerVisitBp,
                                    creditTrustCapBp: c.TrustCapBp,
                                    creditChanceCapBp: c.ChanceCapBp);
        }

        private static int IndexOfDish(DishDef[] dishes, string id)
        {
            for (int i = 0; i < dishes.Length; i++)
                if (string.Equals(dishes[i].Id, id, StringComparison.Ordinal)) return i;
            return -1;
        }

        /// <summary>
        /// Menu roles: which dish group stands in for main, side, drink and
        /// dessert. docs/13 designed the groups per cuisine.
        ///
        /// The validation is strict: EVERY group in the dish file must fall
        /// into exactly one role, and at least one main must be unlocked on
        /// day one. Without this check the Turkish cuisine broke silently;
        /// all eight strategies went bust with zero customers and the reason
        /// was visible nowhere.
        /// </summary>
        private static void BuildMenuRoles(CuisineDto dto, string cuisine, DishDef[] dishes,
                                           out string[] main, out string[] side,
                                           out string[] drink, out string[] dessert)
        {
            MenuRolesDto r = dto?.MenuRoles;
            if (r == null)
                throw new ContentException(
                    "cuisines/" + cuisine + ".json: no menuRoles");

            main = Role(r.Main, cuisine, "main");
            side = Role(r.Side, cuisine, "side");
            drink = Role(r.Drink, cuisine, "drink");
            dessert = Role(r.Dessert, cuisine, "dessert");

            HashSet<string> mapped = new HashSet<string>(StringComparer.Ordinal);
            foreach (string[] set in new[] { main, side, drink, dessert })
                foreach (string g in set)
                    if (!mapped.Add(g))
                        throw new ContentException(
                            "cuisines/" + cuisine + ".json: '" + g + "' falls into two roles at once");

            HashSet<string> used = new HashSet<string>(StringComparer.Ordinal);
            foreach (DishDef d in dishes) used.Add(d.Group);

            foreach (string g in used)
                if (!mapped.Contains(g))
                    throw new ContentException(
                        "cuisines/" + cuisine + ".json: the group '" + g + "' has been given no role");

            bool firstDayMain = false;
            foreach (DishDef d in dishes)
                if (d.UnlockDay <= 1 && Array.IndexOf(main, d.Group) >= 0)
                { firstDayMain = true; break; }
            if (!firstDayMain)
                throw new ContentException(
                    "dishes/" + cuisine + ".json: no main dish is unlocked on day one");
        }

        private static string[] Role(List<string> list, string cuisine, string name)
        {
            if (list == null || list.Count == 0)
                throw new ContentException(
                    "cuisines/" + cuisine + ".json: menuRoles." + name + " is empty");
            return list.ToArray();
        }

        /// <summary>
        /// The cold storage ladder. Tier 0 is mandatory and its keepBp is
        /// ZERO: the designed baseline of docs/12 3, which is that perishable
        /// stock dies overnight. The tiers above change that baseline.
        /// </summary>
        private static StorageDef BuildStorage(EquipmentFileDto dto)
        {
            StorageDto sd = dto.Storage;
            if (sd == null)
                throw new ContentException("equipment.json: no storage section");
            if (sd.Tiers == null || sd.Tiers.Count < 2)
                throw new ContentException("equipment.json: storage must have at least two rungs");

            StorageTier[] tiers = new StorageTier[sd.Tiers.Count];
            for (int t = 0; t < sd.Tiers.Count; t++)
            {
                StorageTierDto td = sd.Tiers[t];
                if (td.Tier != t)
                    throw new ContentException(
                        "equipment.json: the storage rung order is broken, expected "
                        + t + " but found " + td.Tier);
                if (td.KeepBp < 0 || td.KeepBp > Core.Fx.One)
                    throw new ContentException(
                        "equipment.json: storage t" + t + " keepBp must be 0-10000");
                if (t == 0)
                {
                    if (td.KeepBp != 0)
                        throw new ContentException("equipment.json: storage t0 keepBp must be zero");
                    if (td.Price != 0)
                        throw new ContentException("equipment.json: storage t0 must be free");
                }
                else
                {
                    StorageTierDto prev = sd.Tiers[t - 1];
                    if (td.KeepBp <= prev.KeepBp)
                        throw new ContentException(
                            "equipment.json: storage t" + t + " keepBp does not increase");
                    if (td.Price <= prev.Price)
                        throw new ContentException(
                            "equipment.json: the price of storage t" + t + " does not increase");
                }
                tiers[t] = new StorageTier(td.KeepBp, td.Price);
            }
            return new StorageDef(sd.NameKey ?? "storage.soguk_hava", tiers);
        }

        /// <summary>
        /// content/equipment.json. docs/27 Decision D: every station has an
        /// equipment ladder; a rung either adds a slot or lowers attendBp,
        /// and neither of them touches prepMs.
        ///
        /// The ids must be IN THE SAME ORDER as the closed list: a dish's
        /// StationIndex value was worked out from that order, and a shift by
        /// one would silently occupy the wrong station.
        /// </summary>
        private static StationDef[] BuildStations(EquipmentFileDto dto, string cuisine)
        {
            if (dto == null)
                throw new ContentException("equipment.json could not be read");
            if (dto.Stations == null || dto.Stations.Count != StationIds.Length)
                throw new ContentException(
                    "equipment.json: there must be " + StationIds.Length + " stations, there are "
                    + (dto.Stations == null ? 0 : dto.Stations.Count));

            StationDef[] defs = new StationDef[StationIds.Length];
            for (int i = 0; i < StationIds.Length; i++)
            {
                StationDto sd = dto.Stations[i];
                if (sd == null || !string.Equals(sd.Id, StationIds[i], StringComparison.Ordinal))
                    throw new ContentException(
                        "equipment.json: station " + i + " must be '" + StationIds[i]
                        + "', got '" + (sd == null ? "null" : sd.Id) + "'");
                if (sd.Tiers == null || sd.Tiers.Count == 0)
                    throw new ContentException("equipment.json: " + sd.Id + " has no rungs");

                StationTier[] tiers = new StationTier[sd.Tiers.Count];
                for (int t = 0; t < sd.Tiers.Count; t++)
                {
                    StationTierDto td = sd.Tiers[t];
                    if (td.Tier != t)
                        throw new ContentException(
                            "equipment.json: the rung order of " + sd.Id + " is broken, expected "
                            + t + " but found " + td.Tier);
                    if (td.Slots < 1)
                        throw new ContentException(
                            "equipment.json: " + sd.Id + " t" + t + " slots must be positive");
                    if (td.AttendBp < 1 || td.AttendBp > Core.Fx.One)
                        throw new ContentException(
                            "equipment.json: " + sd.Id + " t" + t + " attendBp must be 1-10000");
                    if (td.Price < 0)
                        throw new ContentException(
                            "equipment.json: " + sd.Id + " t" + t + " price is negative");
                    if (t == 0 && td.Price != 0)
                        throw new ContentException(
                            "equipment.json: " + sd.Id + " t0 must be free");
                    if (t > 0)
                    {
                        StationTierDto prev = sd.Tiers[t - 1];
                        if (td.Price <= prev.Price)
                            throw new ContentException(
                                "equipment.json: the price of " + sd.Id + " t" + t + " does not increase");
                        if (td.Slots < prev.Slots || td.AttendBp > prev.AttendBp)
                            throw new ContentException(
                                "equipment.json: " + sd.Id + " t" + t + " goes backwards");
                        if (td.Slots == prev.Slots && td.AttendBp == prev.AttendBp)
                            throw new ContentException(
                                "equipment.json: " + sd.Id + " t" + t + " changes nothing");
                    }

                    tiers[t] = new StationTier(td.Slots, td.AttendBp, td.Price, td.NeededAtTables);
                }

                defs[i] = new StationDef(sd.Id, sd.NameKey ?? ("station." + sd.Id), tiers);
            }

            // Named equipment SPECIFIC to a cuisine is appended AFTER the
            // shared base. The order is binding: a dish's StationIndex value
            // follows it and the save file carries the tiers by index.
            List<StationDto> extra = null;
            if (dto.CuisineStations != null)
                dto.CuisineStations.TryGetValue(cuisine, out extra);
            if (extra == null || extra.Count == 0) return defs;

            StationDef[] all = new StationDef[defs.Length + extra.Count];
            Array.Copy(defs, all, defs.Length);
            for (int i = 0; i < extra.Count; i++)
            {
                StationDto sd = extra[i];
                if (string.IsNullOrEmpty(sd.Id))
                    throw new ContentException("equipment.json: the cuisine station has no id");
                if (Array.IndexOf(StationIds, sd.Id) >= 0)
                    throw new ContentException(
                        "equipment.json: '" + sd.Id + "' carries the same name as a shared station");
                if (sd.Tiers == null || sd.Tiers.Count < 2)
                    throw new ContentException(
                        "equipment.json: " + sd.Id + " must have at least two rungs; "
                        + "named equipment HAS TO BE BOUGHT");

                StationTier[] t = new StationTier[sd.Tiers.Count];
                for (int k = 0; k < sd.Tiers.Count; k++)
                    t[k] = new StationTier(sd.Tiers[k].Slots, sd.Tiers[k].AttendBp,
                                           sd.Tiers[k].Price, sd.Tiers[k].NeededAtTables);
                all[defs.Length + i] =
                    new StationDef(sd.Id, sd.NameKey ?? ("station." + sd.Id), t,
                                   shared: false);
            }
            return all;
        }

        /// <summary>
        /// The "opens" list of a piece of named equipment and the dishes'
        /// actual station must AGREE.
        ///
        /// A thing written in two places can drift apart in two places. The
        /// list is not just documentation, it is a cross-check: if a dish is
        /// moved to another station and the list is not updated, the game
        /// DOES NOT OPEN.
        /// </summary>
        private static void CheckCuisineStationsOpen(
            EquipmentFileDto dto, string cuisine, DishDef[] dishes, StationDef[] stations)
        {
            if (dto.CuisineStations == null) return;
            if (!dto.CuisineStations.TryGetValue(cuisine, out List<StationDto> extra)) return;
            if (extra == null) return;

            foreach (StationDto sd in extra)
            {
                if (sd.Opens == null || sd.Opens.Count == 0) continue;
                int st = StationIndex(sd.Id, stations);
                foreach (string dishId in sd.Opens)
                {
                    int di = -1;
                    for (int i = 0; i < dishes.Length; i++)
                        if (string.Equals(dishes[i].Id, dishId, StringComparison.Ordinal))
                        { di = i; break; }

                    if (di < 0)
                        throw new ContentException(
                            "equipment.json: " + sd.Id + " opens '" + dishId + "' but"
                            + " that dish is not on the " + cuisine + " menu");
                    if (dishes[di].StationIndex != st)
                        throw new ContentException(
                            "equipment.json: " + sd.Id + " opens '" + dishId + "' but"
                            + " that dish is at another station");
                    if (dishes[di].RequiresStationTier <= 0)
                        throw new ContentException(
                            "dishes/" + cuisine + ".json: '" + dishId + "' sits on named"
                            + " equipment but its requiresStationTier is zero, so it is unlocked");
                }
            }
        }

        private static int StationIndex(string id, StationDef[] stations)
        {
            for (int i = 0; i < stations.Length; i++)
                if (string.Equals(stations[i].Id, id, StringComparison.Ordinal)) return i;
            return -1;
        }

        /// <summary>
        /// docs/28-peak-decision.md Decision G. Four values summing to 10000.
        /// The same validation shape as arrivalWeightsBp.
        /// </summary>
        private static int[] BuildSlotDurations(CuisineDto dto, string cuisine)
        {
            if (dto == null || dto.SlotDurationsBp == null) return null;

            if (dto.SlotDurationsBp.Count != (int)DaySlot.Count)
                throw new ContentException(
                    "cuisines/" + cuisine + ".json: slotDurationsBp must have four values");

            int sum = 0;
            for (int i = 0; i < dto.SlotDurationsBp.Count; i++)
            {
                if (dto.SlotDurationsBp[i] <= 0)
                    throw new ContentException(
                        "cuisines/" + cuisine + ".json: a slot duration must be positive");
                sum += dto.SlotDurationsBp[i];
            }
            if (sum != Core.Fx.One)
                throw new ContentException(
                    "cuisines/" + cuisine + ".json: slotDurationsBp sums to "
                    + sum + ", it must be 10000");

            return dto.SlotDurationsBp.ToArray();
        }

        // -------------------------------------------------------------------
        private static IngredientDef[] BuildIngredients(
            List<IngredientDto> dtos, out Dictionary<string, int> index)
        {
            if (dtos == null || dtos.Count == 0)
                throw new ContentException("ingredients.json is empty");

            index = new Dictionary<string, int>(StringComparer.Ordinal);
            IngredientDef[] result = new IngredientDef[dtos.Count];

            for (int i = 0; i < dtos.Count; i++)
            {
                IngredientDto d = dtos[i];
                ContentLoader.RequireId(d.Id, "ingredients.json");
                if (index.ContainsKey(d.Id))
                    throw new ContentException("Duplicate ingredient id: " + d.Id);
                if (d.BasePrice <= 0)
                    throw new ContentException("Ingredient " + d.Id + ": basePrice must be positive");

                index[d.Id] = i;
                result[i] = new IngredientDef(d.Id, d.NameKey, d.Shared,
                                              d.BasePrice, d.Perishable, d.SpoilDays,
                                              BuildSeason(d),
                                              BuildQualityPrice(d),
                                              BuildQualitySatisfaction(d));
            }
            return result;
        }

        /// <summary>
        /// Quality tiers. The order is binding: 0 low, 1 standard, 2 high.
        /// The VALUES are content tokens - the keys as ingredients.json
        /// writes them - so they stay exactly as they are.
        /// </summary>
        private static readonly string[] QualityIds = { "dusuk", "standart", "yuksek" };

        /// <summary>
        /// Quality price multipliers. Standard MUST be 10000: standard is the
        /// reference point, the base price is built on top of it.
        /// </summary>
        private static int[] BuildQualityPrice(IngredientDto d)
        {
            int[] bp = ReadQuality(d.QualityPriceMultiplierBp, d.Id,
                                   "qualityPriceMultiplierBp");
            if (bp[1] != Core.Fx.One)
                throw new ContentException(
                    "Ingredient " + d.Id + ": the standard quality multiplier must be 10000");
            if (bp[0] >= bp[1] || bp[2] <= bp[1])
                throw new ContentException(
                    "Ingredient " + d.Id + ": the quality prices must increase");
            return bp;
        }

        /// <summary>
        /// What quality does to satisfaction. Standard has to be ZERO, low
        /// negative, high positive.
        /// </summary>
        private static int[] BuildQualitySatisfaction(IngredientDto d)
        {
            int[] c = ReadQuality(d.QualitySatisfactionCenti, d.Id,
                                  "qualitySatisfactionCenti");
            if (c[1] != 0)
                throw new ContentException(
                    "Ingredient " + d.Id + ": standard quality satisfaction must be zero");
            if (c[0] > 0 || c[2] < 0)
                throw new ContentException(
                    "Ingredient " + d.Id + ": low quality must be negative and high positive");
            return c;
        }

        private static int[] ReadQuality(System.Collections.Generic.Dictionary<string, int> src,
                                         string id, string field)
        {
            if (src == null || src.Count == 0)
                throw new ContentException("Ingredient " + id + ": no " + field);
            int[] v = new int[QualityIds.Length];
            for (int i = 0; i < QualityIds.Length; i++)
            {
                if (!src.TryGetValue(QualityIds[i], out int x))
                    throw new ContentException(
                        "Ingredient " + id + ": '" + QualityIds[i] + "' is missing from " + field);
                v[i] = x;
            }
            return v;
        }

        /// <summary>
        /// docs/09: the campaign has four seasons. The order is binding.
        /// The VALUES are content tokens - the season keys as
        /// ingredients.json writes them (spring, summer, autumn, winter).
        /// </summary>
        private static readonly string[] SeasonIds =
        {
            "ilkbahar", "yaz", "sonbahar", "kis"
        };

        /// <summary>
        /// The ingredient's season multipliers. All four seasons have to be
        /// there; a missing season would silently mean "no change", and it is
        /// exactly this class of bug (content promises a field, the code never
        /// reads it) that left the seasons dead for months.
        /// </summary>
        private static int[] BuildSeason(IngredientDto d)
        {
            if (d.SeasonModifierBp == null || d.SeasonModifierBp.Count == 0)
                throw new ContentException(
                    "Ingredient " + d.Id + ": no seasonModifierBp");

            int[] bp = new int[SeasonIds.Length];
            for (int i = 0; i < SeasonIds.Length; i++)
            {
                if (!d.SeasonModifierBp.TryGetValue(SeasonIds[i], out int v))
                    throw new ContentException(
                        "Ingredient " + d.Id + ": the '" + SeasonIds[i] + "' season is missing");
                if (v <= 0)
                    throw new ContentException(
                        "Ingredient " + d.Id + ": the '" + SeasonIds[i] + "' multiplier must be positive");
                bp[i] = v;
            }
            return bp;
        }

        // -------------------------------------------------------------------
        /// <summary>
        /// docs/09: the campaign is 60 days, 15 per season. If there is no
        /// economy.json (the unit tests build DTOs by hand) this value is used.
        /// </summary>
        private const int DefaultSeasonDays = 15;
        private const int SeasonCount = 4;

        private static DishDef[] BuildDishes(List<DishDto> dtos, IngredientDef[] ingredients,
                                             Dictionary<string, int> index, string cuisine,
                                             StationDef[] stations,
                                             int seasonDays = DefaultSeasonDays)
        {
            if (dtos == null || dtos.Count == 0)
                throw new ContentException("The dish list is empty: " + cuisine);

            HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
            DishDef[] result = new DishDef[dtos.Count];

            for (int i = 0; i < dtos.Count; i++)
            {
                DishDto d = dtos[i];
                string where = "dishes/" + cuisine + ".json";

                ContentLoader.RequireId(d.Id, where);
                if (!seen.Add(d.Id))
                    throw new ContentException("Duplicate dish id: " + d.Id);

                if (!string.Equals(d.Cuisine, cuisine, StringComparison.Ordinal))
                    throw new ContentException(
                        "Dish " + d.Id + " has cuisine '" + d.Cuisine + "', the file is '" + cuisine + "'");

                // docs/23 8.3, the four mandatory parameters
                if (d.Price <= 0)
                    throw new ContentException("Dish " + d.Id + ": price must be positive");
                if (d.PrepMs <= 0)
                    throw new ContentException("Dish " + d.Id + ": prepMs must be positive");
                if (d.Complexity < 1 || d.Complexity > 3)
                    throw new ContentException("Dish " + d.Id + ": complexity must be 1-3");
                if (d.Ingredients == null || d.Ingredients.Count == 0)
                    throw new ContentException("Dish " + d.Id + ": the ingredient list is empty");

                int station = StationIndex(d.Station, stations);
                if (station < 0)
                    throw new ContentException(
                        "Dish " + d.Id + ": unknown station '" + d.Station + "'");

                DishIngredient[] parts = new DishIngredient[d.Ingredients.Count];
                long cost = 0;
                for (int k = 0; k < d.Ingredients.Count; k++)
                {
                    DishIngredientDto p = d.Ingredients[k];
                    if (!index.TryGetValue(p.Id ?? "", out int ing))
                        throw new ContentException(
                            "Dish " + d.Id + ": ingredient not found '" + p.Id + "'");
                    if (p.Grams <= 0)
                        throw new ContentException(
                            "Dish " + d.Id + ": the grammage of '" + p.Id + "' must be positive");

                    parts[k] = new DishIngredient(ing, p.Grams);
                    cost += Core.Fx.MulDiv(ingredients[ing].BasePrice, p.Grams, GramsPerKilo);
                }

                int unlockDay = d.UnlockDay > 0 ? d.UnlockDay : 1;
                // Unlock conditions. A dish that is open on day one must ask
                // for nothing, otherwise it stays locked the moment the game
                // starts.
                if (unlockDay <= 1 && (d.RequiresStationTier > 0 || d.UnlockReputationCenti > 0))
                    throw new ContentException(
                        "Dish " + d.Id + ": it is open on day one yet carries an unlock condition");
                if (d.RequiresStationTier < 0)
                    throw new ContentException(
                        "Dish " + d.Id + ": requiresStationTier is negative");
                if (d.UnlockReputationCenti < 0 || d.UnlockReputationCenti > Core.Fx.One)
                    throw new ContentException(
                        "Dish " + d.Id + ": unlockReputationCenti must be 0-10000");

                // unlockSeason is DERIVED from unlockDay. The same fact is
                // written in two places, and a thing written in two places
                // drifts apart silently - it has happened four times in this
                // file so far. The field is not deleted (the progress screen
                // groups dishes by season) but it is now an INVARIANT: if it
                // does not agree, the game does not open.
                int season = (unlockDay - 1) / (seasonDays > 0 ? seasonDays : DefaultSeasonDays) + 1;
                if (season > SeasonCount) season = SeasonCount;
                if (d.UnlockSeason != 0 && d.UnlockSeason != season)
                    throw new ContentException(
                        "Dish " + d.Id + ": unlockSeason is " + d.UnlockSeason +
                        " but unlockDay " + unlockDay + " falls in season " + season +
                        " (a season is " + seasonDays + " days)");

                result[i] = new DishDef(d.Id, d.NameKey, d.Cuisine, d.Group, d.Price,
                                        d.PrepMs, station, d.Complexity, unlockDay,
                                        parts, cost,
                                        d.RequiresStationTier, d.UnlockReputationCenti,
                                        season);
            }
            return result;
        }

        // -------------------------------------------------------------------
        private static ArchetypeDef[] BuildArchetypes(List<ArchetypeDto> dtos)
        {
            if (dtos == null || dtos.Count == 0)
                throw new ContentException("The archetype list is empty");

            HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
            ArchetypeDef[] result = new ArchetypeDef[dtos.Count];

            for (int i = 0; i < dtos.Count; i++)
            {
                ArchetypeDto a = dtos[i];
                ContentLoader.RequireId(a.Id, "archetypes");
                if (!seen.Add(a.Id))
                    throw new ContentException("Duplicate archetype id: " + a.Id);

                int tier = Array.IndexOf(Tiers, a.Tier);
                if (tier < 0)
                    throw new ContentException(
                        "Archetype " + a.Id + ": unknown tier '" + a.Tier + "'");
                if (a.Weight <= 0)
                    throw new ContentException("Archetype " + a.Id + ": weight must be positive");
                if (a.PatienceMs <= 0)
                    throw new ContentException("Archetype " + a.Id + ": patienceMs must be positive");
                if (a.GroupSizeMin < 1 || a.GroupSizeMax < a.GroupSizeMin)
                    throw new ContentException("Archetype " + a.Id + ": the group size is invalid");

                if (a.ArrivalWeightsBp == null
                    || a.ArrivalWeightsBp.Count != (int)DaySlot.Count)
                    throw new ContentException(
                        "Archetype " + a.Id + ": arrivalWeightsBp must have four values");

                int sum = 0;
                for (int k = 0; k < a.ArrivalWeightsBp.Count; k++) sum += a.ArrivalWeightsBp[k];
                if (sum != Core.Fx.One)
                    throw new ContentException(
                        "Archetype " + a.Id + ": arrivalWeightsBp sums to "
                        + sum + ", it must be 10000");

                result[i] = new ArchetypeDef(
                    a.Id, a.NameKey, tier, a.Weight, a.PatienceMs, a.PriceSensitivityBp,
                    a.GroupSizeMin, a.GroupSizeMax, a.ReputationWeight, a.TipChanceBp,
                    a.ArrivalWeightsBp.ToArray());
            }

            CheckTierWeights(result);
            return result;
        }

        /// <summary>
        /// The frequency tier and the traffic weight must AGREE.
        ///
        /// docs/13 144: "The traffic shares are not in economy.json, they are
        /// derived FROM THE FREQUENCY TIER." Content obeys that today (common
        /// 550-1300, medium 220-400, rare 100-150, with no overlap at all) but
        /// nothing was enforcing it and the TierIndex field was read nowhere.
        ///
        /// This check does two jobs at once: it brings the field to life, and
        /// it makes it impossible for the two pieces of data to drift apart
        /// silently. If an archetype's tier is changed and its weight is
        /// forgotten, the game DOES NOT OPEN.
        /// </summary>
        private static void CheckTierWeights(ArchetypeDef[] defs)
        {
            // The lowest and the highest weight of each tier
            int[] lo = { int.MaxValue, int.MaxValue, int.MaxValue };
            int[] hi = { 0, 0, 0 };
            foreach (ArchetypeDef d in defs)
            {
                int t = d.TierIndex;
                if (t < 0 || t >= 3) continue;
                if (d.Weight < lo[t]) lo[t] = d.Weight;
                if (d.Weight > hi[t]) hi[t] = d.Weight;
            }

            for (int t = 0; t + 1 < 3; t++)
            {
                if (hi[t] == 0 || lo[t + 1] == int.MaxValue) continue;
                if (lo[t] <= hi[t + 1])
                    throw new ContentException(
                        "The archetype weights contradict the frequency tier: the lowest of '"
                        + Tiers[t] + "' is " + lo[t] + ", the highest of '" + Tiers[t + 1]
                        + "' is " + hi[t + 1] + " (docs/13)");
            }
        }
    }
}
