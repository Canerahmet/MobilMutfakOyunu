using System;
using System.Collections.Generic;
using System.IO;
using Lokanta.Core.Content;

namespace Lokanta.Content
{
    /// <summary>
    /// Bir mutfagin butun icerigini yukler ve dogrular.
    /// docs/23-cekirdek-sozlesmesi.md 9.1: icerik gecersizse oyun ACILMAZ.
    /// Sessiz varsayilan yok, eksik referans yok.
    /// </summary>
    public static class ContentSetLoader
    {
        /// <summary>
        /// docs/23 8.3: yemek istasyonlari kapali liste. Sira baglayici;
        /// yemeklerin StationIndex degeri bu siraya gore.
        /// </summary>
        public static readonly string[] StationIds =
        {
            "ocak", "izgara", "firin", "soguk", "icecek", "tatli"
        };

        private static readonly string[] Tiers = { "sik", "orta", "nadir" };

        /// <summary>Bir gramin kilo fiyatindan maliyeti: fiyat x gram / 1000.</summary>
        private const int GramsPerKilo = 1000;

        public static ContentSet Load(string contentDirectory, string cuisine)
        {
            if (string.IsNullOrEmpty(contentDirectory))
                throw new ArgumentNullException(nameof(contentDirectory));
            return Load(new DirectoryContentSource(contentDirectory), cuisine);
        }

        /// <summary>Icerigi bir KAYNAKTAN yukler. Bkz. IContentSource.</summary>
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

            // docs/23 9.1: ekipman dosyasi ZORUNLU. Istasyon yuvasi ve
            // attendBp olmadan mutfak simule edilemez, sessiz varsayilan yok.
            EquipmentFileDto equipmentDto =
                ContentLoader.ReadJson<EquipmentFileDto>(src, "equipment.json");

            // Isimli duzenli musteriler. Dosya istege bagli: bir mutfak
            // duzenli musterisiz de kosuyor (birim testleri boyle), ama
            // varsa TAMAMEN dogrulaniyor.
            List<RegularDto> regularDtos = null;
            string regularPath = "regulars/" + cuisine + ".json";
            if (src.Exists(regularPath))
                regularDtos = ContentLoader.ReadJson<List<RegularDto>>(src, regularPath);

            // Mutfak dosyasi istege bagli: yoksa esit dilim varsayilir.
            CuisineDto cuisineDto = null;
            string cuisinePath = "cuisines/" + cuisine + ".json";
            if (src.Exists(cuisinePath))
                cuisineDto = ContentLoader.ReadJson<CuisineDto>(src, cuisinePath);

            // Mevsim uzunlugu economy.json'da. Burada okunmasinin sebebi
            // unlockSeason degismezi: mevsim, unlockDay'den turetiliyor ve
            // turetim mevsim uzunlugunu bilmeden dogrulanamaz. Ikinci bir
            // sabit yazmak yerine TEK kaynaktan okuyoruz.
            int seasonDays = DefaultSeasonDays;
            if (src.Exists("economy.json"))
            {
                EconomyDto eco = ContentLoader.ReadJson<EconomyDto>(src, "economy.json");
                if (eco != null && eco.SeasonDays > 0) seasonDays = eco.SeasonDays;
            }

            return Build(cuisine, ingredientDtos, dishDtos, archetypeDtos,
                         equipmentDto, cuisineDto, seasonDays, regularDtos,
                         LoadStaffNames(src));
        }

        public static ContentSet Build(string cuisine,
                                       List<IngredientDto> ingredientDtos,
                                       List<DishDto> dishDtos,
                                       List<ArchetypeDto> archetypeDtos,
                                       EquipmentFileDto equipmentDto,
                                       CuisineDto cuisineDto = null,
                                       int seasonDays = DefaultSeasonDays,
                                       List<RegularDto> regularDtos = null,
                                       string[] staffNames = null)
        {
            IngredientDef[] ingredients = BuildIngredients(ingredientDtos, out var index);
            // Istasyonlar YEMEKLERDEN once: yemegin station alani artik
            // mutfaga ozel bir ekipmani da isaret edebiliyor.
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

            SignatureDef signature = BuildSignature(cuisineDto, cuisine, dishes, seasonDays);
            RegularDef[] regulars = BuildRegulars(regularDtos, cuisine, dishes,
                                                  archetypes, signature);

            return new ContentSet(cuisine, ingredients, dishes, archetypes, stations,
                                  storage, slots, eatMs, main, side, drink, dessert,
                                  signature, regulars, BuildScoreAxis(cuisineDto),
                                  staffNames);
        }

        /// <summary>
        /// Personel isim havuzu. Dosya yoksa bos donuyor: isimler oyunun
        /// kurallarina girmiyor, yalnizca sunumuna.
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
                    throw new ContentException("names.json: bos isim, sira " + i);

            return dto.Staff;
        }

        /// <summary>
        /// Yil sonu mutfak ekseni. Yoksa notr bir eksen donuyor: eksik
        /// bir alan oyunu durdurmamali, yalnizca o eksen tam puan verir.
        /// </summary>
        private static ScoreAxisDef BuildScoreAxis(CuisineDto dto)
        {
            if (dto == null || dto.ScoreAxis == null)
                return new ScoreAxisDef("none", "score.signature", 1);

            ScoreAxisDto a = dto.ScoreAxis;
            return new ScoreAxisDef(a.Kind, a.NameKey, a.Target);
        }

        /// <summary>
        /// Her yemegin istedigi ekipman kademesi GERCEKTEN VAR MI.
        ///
        /// Uc fast food tatlisi "firin kademe 2" istiyordu; firin
        /// merdiveni kademe 1'de bitiyor. O uc yemek altmis gun boyunca
        /// hic acilmiyordu ve hicbir sey sikayet etmiyordu - ne uretec, ne
        /// yukleyici, ne denge araci. Icerik sessizce erisilemez olmustu.
        ///
        /// Sessiz kalmaktansa YUKLENMEMEK: docs/23 9.1 ile ayni tercih.
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
                        cuisine + "/" + d.Id + ": istasyon bulunamadi");

                int top = stations[st].Tiers.Length - 1;
                if (d.RequiresStationTier > top)
                    throw new ContentException(
                        cuisine + "/" + d.Id + ": " + stations[st].Id
                        + " kademe " + d.RequiresStationTier
                        + " istiyor ama merdiven kademe " + top + "'de bitiyor."
                        + " Bu yemek hicbir zaman acilamaz.");
            }
        }

        /// <summary>
        /// Isimli duzenli musteriler. docs/13 regulars/*.json.
        ///
        /// Dogrulama sert cunku bu dosya ELLE yazilmis icerik: arketip ve
        /// yemek adlari yazim hatasina acik, ve sessizce dusen bir duzenli
        /// musteri hic fark edilmez - oyunda "gelmedi" ile "yok" ayni
        /// gorunur.
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
                    throw new ContentException("Gecersiz duzenli musteri kimligi: " + d.Id);
                if (!seen.Add(d.Id))
                    throw new ContentException("Tekrarlanan duzenli musteri: " + d.Id);
                if (!string.IsNullOrEmpty(d.Cuisine)
                    && !string.Equals(d.Cuisine, cuisine, StringComparison.Ordinal))
                    throw new ContentException(
                        "Duzenli musteri " + d.Id + " baska mutfaga ait: " + d.Cuisine);

                int arch = IndexOfArchetype(archetypes, d.ArchetypeBase);
                if (arch < 0)
                    throw new ContentException(
                        "Duzenli musteri " + d.Id + ": olmayan arketip " + d.ArchetypeBase);

                int dish = IndexOfDish(dishes, d.FavouriteDish);
                if (dish < 0)
                    throw new ContentException(
                        "Duzenli musteri " + d.Id + ": olmayan yemek " + d.FavouriteDish);

                if (d.ArrivesFromDay < 1)
                    throw new ContentException(
                        "Duzenli musteri " + d.Id + ": arrivesFromDay 1'den kucuk");

                // Geldigi gun sevdigi yemek kapali olamaz: oyuncunun
                // elinde olmayan bir eksikle karsilamak haksizlik.
                if (dishes[dish].UnlockDay > d.ArrivesFromDay)
                    throw new ContentException(
                        "Duzenli musteri " + d.Id + " " + d.ArrivesFromDay +
                        ". gunde geliyor ama sevdigi yemek " +
                        dishes[dish].UnlockDay + ". gunde aciliyor");

                // Sirali olmasi uslup degil: gelis takvimi dosyayi okurken
                // gorunur olmali, ve kod ilk gelmeyeni gorunce kesebilmeli.
                if (d.ArrivesFromDay < lastDay)
                    throw new ContentException(
                        "regulars/" + cuisine + ".json: gelis gunleri artan sirada olmali, " +
                        d.Id + " bozuyor");
                lastDay = d.ArrivesFromDay;

                // Veresiye Turk mutfaginin imza mekanigi. Baska mutfakta
                // uygun bir duzenli musteri yazmak, hic calismayacak bir
                // alan yazmaktir.
                if (d.VeresiyeEligible && signature.Kind != SignatureKind.Credit)
                    throw new ContentException(
                        "Duzenli musteri " + d.Id + " veresiyeye uygun ama " +
                        cuisine + " mutfaginin imza mekanigi veresiye degil");

                result[i] = new RegularDef(d.Id, d.NameKey ?? ("regular." + d.Id + ".name"),
                                           d.JobKey ?? ("regular." + d.Id + ".job"),
                                           arch, dish, d.ArrivesFromDay,
                                           d.VeresiyeEligible,
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
                        "Duzenli musteri " + d.Id + ": sahne numaralari 1'den artan olmali");
                if (b.RequiresVisits < lastVisits)
                    throw new ContentException(
                        "Duzenli musteri " + d.Id + ": sahne ziyaret esikleri azalamaz");
                lastVisits = b.RequiresVisits;
                if (b.RequiresSatisfaction < 0 || b.RequiresSatisfaction > Core.Fx.One)
                    throw new ContentException(
                        "Duzenli musteri " + d.Id + ": sahne memnuniyet esigi 0-10000 olmali");
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
        /// Imza mekanigi. docs/23 8.2: "kind bilinmiyorsa dogrulama
        /// reddeder. Blok eksikse mutfak yuklenmez."
        ///
        /// Sert olmasinin sebebi docs/07: imza mekanigi bir mutfagi
        /// digerinden ayiran TEK sey. Sessiz varsayilan, "satin aldigin
        /// mutfak aslinda ayni oyun" demek olurdu.
        /// </summary>
        private static SignatureDef BuildSignature(CuisineDto dto, string cuisine,
                                                   DishDef[] dishes,
                                                   int seasonDays)
        {
            // Mutfak dosyasi olmayan bir mutfak (birim testleri) imzasiz kalir.
            if (dto == null) return new SignatureDef(SignatureKind.None);

            // docs/09: mekanik ikinci mevsimin ilk gununde geliyor.
            int fromDay = seasonDays + 1;

            SignatureDto sig = dto.Signature;
            if (sig == null || string.IsNullOrEmpty(sig.Kind))
                throw new ContentException(
                    "cuisines/" + cuisine + ".json: signature blogu yok (docs/23 8.2)");

            switch (sig.Kind)
            {
                case "combo":
                    return BuildCombo(sig.Combo, cuisine, dishes, fromDay);
                case "credit":
                    return BuildCredit(sig.Credit, cuisine, fromDay);
                default:
                    throw new ContentException(
                        "cuisines/" + cuisine + ".json: bilinmeyen imza turu '" +
                        sig.Kind + "' (combo, credit)");
            }
        }

        private static SignatureDef BuildCombo(ComboDto c, string cuisine,
                                               DishDef[] dishes, int fromDay)
        {
            if (c == null || c.Items == null || c.Items.Count != 3)
                throw new ContentException(
                    cuisine + ": kombo tam uc kalem olmali (ana, yan, icecek)");
            if (c.PriceBp <= 0 || c.PriceBp >= Core.Fx.One)
                throw new ContentException(
                    cuisine + ": kombo priceBp 0-10000 arasi INDIRIM olmali, " +
                    c.PriceBp + " bulundu");
            if (c.KitchenLoadBp < Core.Fx.One)
                throw new ContentException(
                    cuisine + ": kombo kitchenLoadBp 10000'den kucuk olamaz; " +
                    "kombo mutfagi RAHATLATMAZ (docs/07)");

            int[] idx = new int[3];
            for (int i = 0; i < 3; i++)
            {
                idx[i] = IndexOfDish(dishes, c.Items[i]);
                if (idx[i] < 0)
                    throw new ContentException(
                        cuisine + ": kombo kalemi menude yok: " + c.Items[i]);
                // Kalemler mekanik GELDIGINDE acik olmali. Ilk gun olmalari
                // sart degil - docs/09 mekanigi ikinci mevsime koyuyor - ama
                // geldigi gun kilitli bir kombo hic satilamaz.
                if (dishes[idx[i]].UnlockDay > fromDay)
                    throw new ContentException(
                        cuisine + ": kombo kalemi '" + c.Items[i] + "' " +
                        dishes[idx[i]].UnlockDay + ". gunde aciliyor ama mekanik " +
                        fromDay + ". gunde geliyor");
            }
            return new SignatureDef(SignatureKind.Combo, fromDay,
                                    idx, c.PriceBp, c.KitchenLoadBp);
        }

        private static SignatureDef BuildCredit(CreditDto c, string cuisine, int fromDay)
        {
            if (c == null)
                throw new ContentException(cuisine + ": credit blogu yok");
            if (c.MaxPerRegular <= 0)
                throw new ContentException(cuisine + ": credit maxPerRegular pozitif olmali");
            if (c.DueDays <= 0)
                throw new ContentException(cuisine + ": credit dueDays pozitif olmali");
            if (c.CollectChanceBp <= 0 || c.CollectChanceBp > Core.Fx.One)
                throw new ContentException(cuisine + ": credit collectChanceBp 1-10000 olmali");
            // RISKSIZ DEFTER KARAR URETMEZ. Tavan tam kesinlige
            // cikarsa veresiye yine bedava bir prim dugmesi olur.
            if (c.ChanceCapBp >= Core.Fx.One)
                throw new ContentException(cuisine + ": credit chanceCapBp 10000'in altinda olmali");

            if (c.DefaultRepPenaltyCenti < 0)
                throw new ContentException(cuisine + ": credit defaultRepPenaltyCenti negatif olamaz");

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
        /// Menu rolleri: hangi yemek grubu ana, yan, icecek, tatli yerine
        /// geciyor. docs/13 gruplari mutfaga ozel tasarlamis.
        ///
        /// Dogrulama sert: yemek dosyasindaki HER grup tam olarak bir role
        /// dusmeli ve ilk gun acik en az bir ana yemek olmali. Bu kontrol
        /// olmadan Turk mutfagi sessizce kirilmisti; sekiz strateji de
        /// sifir musteriyle batiyordu ve sebep hicbir yerde gorunmuyordu.
        /// </summary>
        private static void BuildMenuRoles(CuisineDto dto, string cuisine, DishDef[] dishes,
                                           out string[] main, out string[] side,
                                           out string[] drink, out string[] dessert)
        {
            MenuRolesDto r = dto?.MenuRoles;
            if (r == null)
                throw new ContentException(
                    "cuisines/" + cuisine + ".json: menuRoles yok");

            main = Role(r.Main, cuisine, "main");
            side = Role(r.Side, cuisine, "side");
            drink = Role(r.Drink, cuisine, "drink");
            dessert = Role(r.Dessert, cuisine, "dessert");

            HashSet<string> mapped = new HashSet<string>(StringComparer.Ordinal);
            foreach (string[] set in new[] { main, side, drink, dessert })
                foreach (string g in set)
                    if (!mapped.Add(g))
                        throw new ContentException(
                            "cuisines/" + cuisine + ".json: '" + g + "' iki role birden dusuyor");

            HashSet<string> used = new HashSet<string>(StringComparer.Ordinal);
            foreach (DishDef d in dishes) used.Add(d.Group);

            foreach (string g in used)
                if (!mapped.Contains(g))
                    throw new ContentException(
                        "cuisines/" + cuisine + ".json: '" + g + "' grubuna rol verilmemis");

            bool firstDayMain = false;
            foreach (DishDef d in dishes)
                if (d.UnlockDay <= 1 && Array.IndexOf(main, d.Group) >= 0)
                { firstDayMain = true; break; }
            if (!firstDayMain)
                throw new ContentException(
                    "dishes/" + cuisine + ".json: ilk gun acik ana yemek yok");
        }

        private static string[] Role(List<string> list, string cuisine, string name)
        {
            if (list == null || list.Count == 0)
                throw new ContentException(
                    "cuisines/" + cuisine + ".json: menuRoles." + name + " bos");
            return list.ToArray();
        }

        /// <summary>
        /// Soguk hava merdiveni. Kademe 0 zorunlu ve keepBp'si SIFIR:
        /// docs/12 3'un tasarlanmis temeli, yani bozulabilir malzeme gece
        /// oluyor. Ustteki kademeler o temeli degistiriyor.
        /// </summary>
        private static StorageDef BuildStorage(EquipmentFileDto dto)
        {
            StorageDto sd = dto.Storage;
            if (sd == null)
                throw new ContentException("equipment.json: storage bolumu yok");
            if (sd.Tiers == null || sd.Tiers.Count < 2)
                throw new ContentException("equipment.json: storage en az iki basamak olmali");

            StorageTier[] tiers = new StorageTier[sd.Tiers.Count];
            for (int t = 0; t < sd.Tiers.Count; t++)
            {
                StorageTierDto td = sd.Tiers[t];
                if (td.Tier != t)
                    throw new ContentException(
                        "equipment.json: storage basamak sirasi bozuk, "
                        + t + " beklenirken " + td.Tier);
                if (td.KeepBp < 0 || td.KeepBp > Core.Fx.One)
                    throw new ContentException(
                        "equipment.json: storage t" + t + " keepBp 0-10000 olmali");
                if (t == 0)
                {
                    if (td.KeepBp != 0)
                        throw new ContentException("equipment.json: storage t0 keepBp sifir olmali");
                    if (td.Price != 0)
                        throw new ContentException("equipment.json: storage t0 bedava olmali");
                }
                else
                {
                    StorageTierDto prev = sd.Tiers[t - 1];
                    if (td.KeepBp <= prev.KeepBp)
                        throw new ContentException(
                            "equipment.json: storage t" + t + " keepBp artmiyor");
                    if (td.Price <= prev.Price)
                        throw new ContentException(
                            "equipment.json: storage t" + t + " fiyati artmiyor");
                }
                tiers[t] = new StorageTier(td.KeepBp, td.Price);
            }
            return new StorageDef(sd.NameKey ?? "storage.soguk_hava", tiers);
        }

        /// <summary>
        /// content/equipment.json. docs/27 Karar D: her istasyonun bir
        /// ekipman merdiveni var; basamak ya yuva ekliyor ya attendBp
        /// dusuruyor, ikisi de prepMs'e dokunmuyor.
        ///
        /// Kimlikler kapali listeyle AYNI SIRADA olmali: yemeklerin
        /// StationIndex degeri o siraya gore hesaplandi ve bir kayma
        /// sessizce yanlis istasyonu mesgul ederdi.
        /// </summary>
        private static StationDef[] BuildStations(EquipmentFileDto dto, string cuisine)
        {
            if (dto == null)
                throw new ContentException("equipment.json okunamadi");
            if (dto.Stations == null || dto.Stations.Count != StationIds.Length)
                throw new ContentException(
                    "equipment.json: " + StationIds.Length + " istasyon olmali, "
                    + (dto.Stations == null ? 0 : dto.Stations.Count) + " var");

            StationDef[] defs = new StationDef[StationIds.Length];
            for (int i = 0; i < StationIds.Length; i++)
            {
                StationDto sd = dto.Stations[i];
                if (sd == null || !string.Equals(sd.Id, StationIds[i], StringComparison.Ordinal))
                    throw new ContentException(
                        "equipment.json: " + i + ". istasyon '" + StationIds[i]
                        + "' olmali, '" + (sd == null ? "null" : sd.Id) + "' geldi");
                if (sd.Tiers == null || sd.Tiers.Count == 0)
                    throw new ContentException("equipment.json: " + sd.Id + " basamaksiz");

                StationTier[] tiers = new StationTier[sd.Tiers.Count];
                for (int t = 0; t < sd.Tiers.Count; t++)
                {
                    StationTierDto td = sd.Tiers[t];
                    if (td.Tier != t)
                        throw new ContentException(
                            "equipment.json: " + sd.Id + " basamak sirasi bozuk, "
                            + t + " beklenirken " + td.Tier);
                    if (td.Slots < 1)
                        throw new ContentException(
                            "equipment.json: " + sd.Id + " t" + t + " yuva pozitif olmali");
                    if (td.AttendBp < 1 || td.AttendBp > Core.Fx.One)
                        throw new ContentException(
                            "equipment.json: " + sd.Id + " t" + t + " attendBp 1-10000 olmali");
                    if (td.Price < 0)
                        throw new ContentException(
                            "equipment.json: " + sd.Id + " t" + t + " fiyat negatif");
                    if (t == 0 && td.Price != 0)
                        throw new ContentException(
                            "equipment.json: " + sd.Id + " t0 bedava olmali");
                    if (t > 0)
                    {
                        StationTierDto prev = sd.Tiers[t - 1];
                        if (td.Price <= prev.Price)
                            throw new ContentException(
                                "equipment.json: " + sd.Id + " t" + t + " fiyati artmiyor");
                        if (td.Slots < prev.Slots || td.AttendBp > prev.AttendBp)
                            throw new ContentException(
                                "equipment.json: " + sd.Id + " t" + t + " geriye gidiyor");
                        if (td.Slots == prev.Slots && td.AttendBp == prev.AttendBp)
                            throw new ContentException(
                                "equipment.json: " + sd.Id + " t" + t + " hicbir sey degistirmiyor");
                    }

                    tiers[t] = new StationTier(td.Slots, td.AttendBp, td.Price, td.NeededAtTables);
                }

                defs[i] = new StationDef(sd.Id, sd.NameKey ?? ("station." + sd.Id), tiers);
            }

            // Mutfaga OZEL adlandirilmis ekipman paylasilan altinin ARDINA
            // ekleniyor. Sira baglayici: yemeklerin StationIndex degeri buna
            // gore ve kayit dosyasi kademeleri indise gore tasiyor.
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
                    throw new ContentException("equipment.json: mutfak istasyonunun kimligi yok");
                if (Array.IndexOf(StationIds, sd.Id) >= 0)
                    throw new ContentException(
                        "equipment.json: '" + sd.Id + "' paylasilan istasyonla ayni adi tasiyor");
                if (sd.Tiers == null || sd.Tiers.Count < 2)
                    throw new ContentException(
                        "equipment.json: " + sd.Id + " en az iki basamak olmali; "
                        + "adlandirilmis ekipman SATIN ALINMAK zorunda");

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
        /// Adlandirilmis ekipmanin "opens" listesi ile yemeklerin gercek
        /// istasyonu TUTARLI olmali.
        ///
        /// Iki yerde yazili bir sey, iki yerde ayrisabilir. Liste yalnizca
        /// belge degil, capraz kontrol: bir yemek baska istasyona tasinip
        /// liste guncellenmezse oyun ACILMIYOR.
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
                            "equipment.json: " + sd.Id + " '" + dishId + "' aciyor ama"
                            + " o yemek " + cuisine + " menusunde yok");
                    if (dishes[di].StationIndex != st)
                        throw new ContentException(
                            "equipment.json: " + sd.Id + " '" + dishId + "' aciyor ama"
                            + " o yemek baska istasyonda");
                    if (dishes[di].RequiresStationTier <= 0)
                        throw new ContentException(
                            "dishes/" + cuisine + ".json: '" + dishId + "' adlandirilmis"
                            + " ekipmanda ama requiresStationTier sifir, yani kilitsiz");
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
        /// docs/28-zirve-karari.md Karar G. Dort deger, toplami 10000.
        /// arrivalWeightsBp ile ayni dogrulama kalibi.
        /// </summary>
        private static int[] BuildSlotDurations(CuisineDto dto, string cuisine)
        {
            if (dto == null || dto.SlotDurationsBp == null) return null;

            if (dto.SlotDurationsBp.Count != (int)DaySlot.Count)
                throw new ContentException(
                    "cuisines/" + cuisine + ".json: slotDurationsBp dort deger olmali");

            int sum = 0;
            for (int i = 0; i < dto.SlotDurationsBp.Count; i++)
            {
                if (dto.SlotDurationsBp[i] <= 0)
                    throw new ContentException(
                        "cuisines/" + cuisine + ".json: dilim suresi pozitif olmali");
                sum += dto.SlotDurationsBp[i];
            }
            if (sum != Core.Fx.One)
                throw new ContentException(
                    "cuisines/" + cuisine + ".json: slotDurationsBp toplami "
                    + sum + ", 10000 olmali");

            return dto.SlotDurationsBp.ToArray();
        }

        // -------------------------------------------------------------------
        private static IngredientDef[] BuildIngredients(
            List<IngredientDto> dtos, out Dictionary<string, int> index)
        {
            if (dtos == null || dtos.Count == 0)
                throw new ContentException("ingredients.json bos");

            index = new Dictionary<string, int>(StringComparer.Ordinal);
            IngredientDef[] result = new IngredientDef[dtos.Count];

            for (int i = 0; i < dtos.Count; i++)
            {
                IngredientDto d = dtos[i];
                ContentLoader.RequireId(d.Id, "ingredients.json");
                if (index.ContainsKey(d.Id))
                    throw new ContentException("Tekrarlanan malzeme kimligi: " + d.Id);
                if (d.BasePrice <= 0)
                    throw new ContentException("Malzeme " + d.Id + ": basePrice pozitif olmali");

                index[d.Id] = i;
                result[i] = new IngredientDef(d.Id, d.NameKey, d.Shared,
                                              d.BasePrice, d.Perishable, d.SpoilDays,
                                              BuildSeason(d),
                                              BuildQualityPrice(d),
                                              BuildQualitySatisfaction(d));
            }
            return result;
        }

        /// <summary>Kalite kademeleri. Sira baglayici: 0 dusuk, 1 standart, 2 yuksek.</summary>
        private static readonly string[] QualityIds = { "dusuk", "standart", "yuksek" };

        /// <summary>
        /// Kalite fiyat carpanlari. Standart 10000 olmak ZORUNDA: standart
        /// referans nokta, taban fiyat onun uzerine kuruluyor.
        /// </summary>
        private static int[] BuildQualityPrice(IngredientDto d)
        {
            int[] bp = ReadQuality(d.QualityPriceMultiplierBp, d.Id,
                                   "qualityPriceMultiplierBp");
            if (bp[1] != Core.Fx.One)
                throw new ContentException(
                    "Malzeme " + d.Id + ": standart kalite carpani 10000 olmali");
            if (bp[0] >= bp[1] || bp[2] <= bp[1])
                throw new ContentException(
                    "Malzeme " + d.Id + ": kalite fiyatlari artan olmali");
            return bp;
        }

        /// <summary>
        /// Kalitenin memnuniyete etkisi. Standart SIFIR olmak zorunda,
        /// dusuk negatif, yuksek pozitif.
        /// </summary>
        private static int[] BuildQualitySatisfaction(IngredientDto d)
        {
            int[] c = ReadQuality(d.QualitySatisfactionCenti, d.Id,
                                  "qualitySatisfactionCenti");
            if (c[1] != 0)
                throw new ContentException(
                    "Malzeme " + d.Id + ": standart kalite memnuniyeti sifir olmali");
            if (c[0] > 0 || c[2] < 0)
                throw new ContentException(
                    "Malzeme " + d.Id + ": dusuk kalite negatif, yuksek pozitif olmali");
            return c;
        }

        private static int[] ReadQuality(System.Collections.Generic.Dictionary<string, int> src,
                                         string id, string field)
        {
            if (src == null || src.Count == 0)
                throw new ContentException("Malzeme " + id + ": " + field + " yok");
            int[] v = new int[QualityIds.Length];
            for (int i = 0; i < QualityIds.Length; i++)
            {
                if (!src.TryGetValue(QualityIds[i], out int x))
                    throw new ContentException(
                        "Malzeme " + id + ": " + field + " icinde '" + QualityIds[i] + "' yok");
                v[i] = x;
            }
            return v;
        }

        /// <summary>docs/09: kampanya dort mevsim. Sira baglayici.</summary>
        private static readonly string[] SeasonIds =
        {
            "ilkbahar", "yaz", "sonbahar", "kis"
        };

        /// <summary>
        /// Malzemenin mevsim carpanlari. Dort mevsim de bulunmali; eksik
        /// bir mevsim sessizce "degisiklik yok" demek olurdu ve tam olarak
        /// bu sinif hata (icerik vaat ediyor, kod okumuyor) yuzunden
        /// mevsimler aylarca olu kaldi.
        /// </summary>
        private static int[] BuildSeason(IngredientDto d)
        {
            if (d.SeasonModifierBp == null || d.SeasonModifierBp.Count == 0)
                throw new ContentException(
                    "Malzeme " + d.Id + ": seasonModifierBp yok");

            int[] bp = new int[SeasonIds.Length];
            for (int i = 0; i < SeasonIds.Length; i++)
            {
                if (!d.SeasonModifierBp.TryGetValue(SeasonIds[i], out int v))
                    throw new ContentException(
                        "Malzeme " + d.Id + ": '" + SeasonIds[i] + "' mevsimi yok");
                if (v <= 0)
                    throw new ContentException(
                        "Malzeme " + d.Id + ": '" + SeasonIds[i] + "' carpani pozitif olmali");
                bp[i] = v;
            }
            return bp;
        }

        // -------------------------------------------------------------------
        /// <summary>
        /// docs/09: kampanya 60 gun, mevsim basina 15. economy.json yoksa
        /// (birim testleri elle DTO kuruyor) bu deger kullaniliyor.
        /// </summary>
        private const int DefaultSeasonDays = 15;
        private const int SeasonCount = 4;

        private static DishDef[] BuildDishes(List<DishDto> dtos, IngredientDef[] ingredients,
                                             Dictionary<string, int> index, string cuisine,
                                             StationDef[] stations,
                                             int seasonDays = DefaultSeasonDays)
        {
            if (dtos == null || dtos.Count == 0)
                throw new ContentException("Yemek listesi bos: " + cuisine);

            HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
            DishDef[] result = new DishDef[dtos.Count];

            for (int i = 0; i < dtos.Count; i++)
            {
                DishDto d = dtos[i];
                string where = "dishes/" + cuisine + ".json";

                ContentLoader.RequireId(d.Id, where);
                if (!seen.Add(d.Id))
                    throw new ContentException("Tekrarlanan yemek kimligi: " + d.Id);

                if (!string.Equals(d.Cuisine, cuisine, StringComparison.Ordinal))
                    throw new ContentException(
                        "Yemek " + d.Id + " mutfagi '" + d.Cuisine + "', dosya '" + cuisine + "'");

                // docs/23 8.3 dort zorunlu parametre
                if (d.Price <= 0)
                    throw new ContentException("Yemek " + d.Id + ": price pozitif olmali");
                if (d.PrepMs <= 0)
                    throw new ContentException("Yemek " + d.Id + ": prepMs pozitif olmali");
                if (d.Complexity < 1 || d.Complexity > 3)
                    throw new ContentException("Yemek " + d.Id + ": complexity 1-3 olmali");
                if (d.Ingredients == null || d.Ingredients.Count == 0)
                    throw new ContentException("Yemek " + d.Id + ": malzeme listesi bos");

                int station = StationIndex(d.Station, stations);
                if (station < 0)
                    throw new ContentException(
                        "Yemek " + d.Id + ": bilinmeyen istasyon '" + d.Station + "'");

                DishIngredient[] parts = new DishIngredient[d.Ingredients.Count];
                long cost = 0;
                for (int k = 0; k < d.Ingredients.Count; k++)
                {
                    DishIngredientDto p = d.Ingredients[k];
                    if (!index.TryGetValue(p.Id ?? "", out int ing))
                        throw new ContentException(
                            "Yemek " + d.Id + ": malzeme bulunamadi '" + p.Id + "'");
                    if (p.Grams <= 0)
                        throw new ContentException(
                            "Yemek " + d.Id + ": '" + p.Id + "' gramaji pozitif olmali");

                    parts[k] = new DishIngredient(ing, p.Grams);
                    cost += Core.Fx.MulDiv(ingredients[ing].BasePrice, p.Grams, GramsPerKilo);
                }

                int unlockDay = d.UnlockDay > 0 ? d.UnlockDay : 1;
                // Kilit sartlari. Ilk gun acik bir yemek hicbir sey
                // istememeli, yoksa oyun baslar baslamaz kilitli kalir.
                if (unlockDay <= 1 && (d.RequiresStationTier > 0 || d.UnlockReputationCenti > 0))
                    throw new ContentException(
                        "Yemek " + d.Id + ": ilk gun acik ama kilit sarti tasiyor");
                if (d.RequiresStationTier < 0)
                    throw new ContentException(
                        "Yemek " + d.Id + ": requiresStationTier negatif");
                if (d.UnlockReputationCenti < 0 || d.UnlockReputationCenti > Core.Fx.One)
                    throw new ContentException(
                        "Yemek " + d.Id + ": unlockReputationCenti 0-10000 olmali");

                // unlockSeason, unlockDay'in TURETILMISI. Ayni gercek iki
                // yerde yaziliyor ve iki yerde yazilan sey sessizce ayrisir -
                // bu dosyada bugune kadar dort kez oldu. Alan silinmiyor
                // (ilerleme ekrani yemekleri mevsime gore grupluyor) ama
                // artik DEGISMEZ: tutmazsa oyun acilmiyor.
                int season = (unlockDay - 1) / (seasonDays > 0 ? seasonDays : DefaultSeasonDays) + 1;
                if (season > SeasonCount) season = SeasonCount;
                if (d.UnlockSeason != 0 && d.UnlockSeason != season)
                    throw new ContentException(
                        "Yemek " + d.Id + ": unlockSeason " + d.UnlockSeason +
                        " ama unlockDay " + unlockDay + " " + season +
                        ". mevsime dusuyor (mevsim " + seasonDays + " gun)");

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
                throw new ContentException("Arketip listesi bos");

            HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
            ArchetypeDef[] result = new ArchetypeDef[dtos.Count];

            for (int i = 0; i < dtos.Count; i++)
            {
                ArchetypeDto a = dtos[i];
                ContentLoader.RequireId(a.Id, "archetypes");
                if (!seen.Add(a.Id))
                    throw new ContentException("Tekrarlanan arketip kimligi: " + a.Id);

                int tier = Array.IndexOf(Tiers, a.Tier);
                if (tier < 0)
                    throw new ContentException(
                        "Arketip " + a.Id + ": bilinmeyen kademe '" + a.Tier + "'");
                if (a.Weight <= 0)
                    throw new ContentException("Arketip " + a.Id + ": weight pozitif olmali");
                if (a.PatienceMs <= 0)
                    throw new ContentException("Arketip " + a.Id + ": patienceMs pozitif olmali");
                if (a.GroupSizeMin < 1 || a.GroupSizeMax < a.GroupSizeMin)
                    throw new ContentException("Arketip " + a.Id + ": grup buyuklugu gecersiz");

                if (a.ArrivalWeightsBp == null
                    || a.ArrivalWeightsBp.Count != (int)DaySlot.Count)
                    throw new ContentException(
                        "Arketip " + a.Id + ": arrivalWeightsBp dort deger olmali");

                int sum = 0;
                for (int k = 0; k < a.ArrivalWeightsBp.Count; k++) sum += a.ArrivalWeightsBp[k];
                if (sum != Core.Fx.One)
                    throw new ContentException(
                        "Arketip " + a.Id + ": arrivalWeightsBp toplami "
                        + sum + ", 10000 olmali");

                result[i] = new ArchetypeDef(
                    a.Id, a.NameKey, tier, a.Weight, a.PatienceMs, a.PriceSensitivityBp,
                    a.GroupSizeMin, a.GroupSizeMax, a.ReputationWeight, a.TipChanceBp,
                    a.ArrivalWeightsBp.ToArray());
            }

            CheckTierWeights(result);
            return result;
        }

        /// <summary>
        /// Siklik kademesi ile trafik agirligi TUTARLI olmali.
        ///
        /// docs/13 144: "Trafik paylari economy.json'da degil, SIKLIK
        /// KADEMESINDEN turetilir." Icerik bugun buna uyuyor (sik 550-1300,
        /// orta 220-400, nadir 100-150, hic ortusme yok) ama bunu hicbir
        /// sey zorlamiyordu ve TierIndex alani hicbir yerde okunmuyordu.
        ///
        /// Bu kontrol iki isi birden goruyor: alani canlandiriyor ve iki
        /// veri parcasinin sessizce ayrisamamasini sagliyor. Bir arketipin
        /// kademesi degistirilip agirligi unutulursa oyun ACILMIYOR.
        /// </summary>
        private static void CheckTierWeights(ArchetypeDef[] defs)
        {
            // Her kademenin en dusuk ve en yuksek agirligi
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
                        "Arketip agirliklari siklik kademesiyle celisiyor: '"
                        + Tiers[t] + "' en dusugu " + lo[t] + ", '" + Tiers[t + 1]
                        + "' en yuksegi " + hi[t + 1] + " (docs/13)");
            }
        }
    }
}
