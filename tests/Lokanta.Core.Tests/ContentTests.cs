using System.Collections.Generic;
using System.IO;
using Lokanta.Content;
using Lokanta.Core.Content;
using Lokanta.Core.Economy;
using Xunit;

namespace Lokanta.Core.Tests
{
    public class ContentTests
    {
        private static List<StaffRoleDto> ValidRoles() => new List<StaffRoleDto>
        {
            new StaffRoleDto { Id = "asci", Pool = "kitchen", CapacityPerDay = 28,
                               WorkPerCustomerMicro = 35714, DailyWage = 14000 },
            new StaffRoleDto { Id = "garson", Pool = "hall", CapacityPerDay = 25,
                               WorkPerCustomerMicro = 40000, DailyWage = 11000 },
        };

        private static EconomyDto ValidEconomy() => new EconomyDto
        {
            SchemaVersion = 1,
            StartingCash = 800000,
            CustomerBasePerTable = 4,
            WeekdayMultiplierBp = 10000,
            WeekendMultiplierBp = 12500,
            WeekendDaysPerWeek = 2,
            IngredientRateBp = 3200,
            Staffing = new StaffingDto
            {
                OwnerPool = "hall",
                OwnerWorkMicro = 1400000,
                WeeklyXpWageGrowthBp = 220,
                Tiers = new List<TierDto>
                {
                    new TierDto { Tables = 4, Rent = 195000, Upgrade = 0, StaffCap = 3 },
                    new TierDto { Tables = 7, Rent = 435000, Upgrade = 325000, StaffCap = 5 },
                }
            }
        };

        [Fact]
        public void The_real_content_loads()
        {
            EconomyConfig cfg = ContentLoader.LoadEconomy(Paths.Content);

            Assert.Equal(800000, cfg.StartingCash);
            Assert.Equal(4, cfg.CustomerBasePerTable);
            Assert.Equal(3200, cfg.IngredientRateBp);
            // 28, NOT 30, AND THE DISHES ARE WHY.
            //
            // tools/balance/timing.py derives the cook's time per guest
            // from the content's own prepMs and attendBp: 17,127 ms,
            // which is a capacity of 480,000 / 17,127 = 28. The model
            // had declared 30 since the first commit, so the staffing
            // said a cook serves more guests than the dishes allow.
            // Measured at 8 seeds, closing the gap moved the reasonable
            // player by 0.2% and the planner by 1.9% - it costs nothing
            // and it stops two parts of the content disagreeing.
            Assert.Equal(28, cfg.CookCapacityPerDay);
            Assert.Equal(14000, cfg.CookDailyWage);
            Assert.Equal(4, cfg.TierCount);

            // The hall pool is made of three roles: capacities 26 + 48 + 70
            Assert.Equal(73581, cfg.HallWorkPerCustomerMicro);
            Assert.Equal(1300000, cfg.OwnerWorkMicro);
        }

        [Fact]
        public void The_hall_daily_wage_is_the_weighted_average()
        {
            EconomyConfig cfg = ContentLoader.LoadEconomy(Paths.Content);
            long wage = StaffingModel.HallDailyWage(cfg);

            // The Python model gives 102.3749 coins = 10237.49 centi-coins
            Assert.InRange(wage, 10230, 10245);
        }

        [Fact]
        public void A_decimal_number_is_rejected()
        {
            string json = "{ \"reputationDecayPerDay\": 0.3 }";
            ContentException ex = Assert.Throws<ContentException>(
                () => ContentLoader.AssertNoDecimals(json, "test.json"));
            Assert.Contains("decimal number", ex.Message);
        }

        [Fact]
        public void A_full_stop_inside_a_string_does_not_count_as_a_decimal()
        {
            // Strings holding a version number or a file name must not trigger it
            ContentLoader.AssertNoDecimals("{ \"nameKey\": \"dish.kuru_fasulye\" }", "t.json");
            ContentLoader.AssertNoDecimals("{ \"note\": \"1.5 kat\" }", "t.json");
        }

        [Fact]
        public void There_are_no_decimals_in_the_real_content_files()
        {
            foreach (string f in Directory.GetFiles(Paths.Content, "*.json"))
                ContentLoader.AssertNoDecimals(File.ReadAllText(f), Path.GetFileName(f));
        }

        [Fact]
        public void An_invalid_id_is_rejected()
        {
            List<StaffRoleDto> roles = ValidRoles();
            roles[0].Id = "Asci";   // a capital letter: the tr-TR ToLower trap
            ContentException ex = Assert.Throws<ContentException>(
                () => ContentLoader.Build(ValidEconomy(), roles));
            Assert.Contains("Invalid id", ex.Message);
        }

        [Fact]
        public void A_duplicate_id_is_rejected()
        {
            List<StaffRoleDto> roles = ValidRoles();
            roles[1].Id = "asci";
            Assert.Throws<ContentException>(() => ContentLoader.Build(ValidEconomy(), roles));
        }

        [Fact]
        public void An_unknown_pool_is_rejected()
        {
            List<StaffRoleDto> roles = ValidRoles();
            roles[1].Pool = "bahce";
            ContentException ex = Assert.Throws<ContentException>(
                () => ContentLoader.Build(ValidEconomy(), roles));
            Assert.Contains("Unknown pool", ex.Message);
        }

        [Fact]
        public void It_is_rejected_when_there_is_no_kitchen_role()
        {
            List<StaffRoleDto> roles = ValidRoles();
            roles.RemoveAt(0);
            Assert.Throws<ContentException>(() => ContentLoader.Build(ValidEconomy(), roles));
        }

        [Fact]
        public void A_decreasing_crew_cap_is_rejected()
        {
            EconomyDto e = ValidEconomy();
            e.Staffing.Tiers[1].StaffCap = 2;
            ContentException ex = Assert.Throws<ContentException>(
                () => ContentLoader.Build(e, ValidRoles()));
            Assert.Contains("staff cap", ex.Message);
        }

        [Fact]
        public void A_table_count_that_does_not_increase_is_rejected()
        {
            EconomyDto e = ValidEconomy();
            e.Staffing.Tiers[1].Tables = 4;
            Assert.Throws<ContentException>(() => ContentLoader.Build(e, ValidRoles()));
        }

        [Fact]
        public void A_zero_capacity_is_rejected()
        {
            List<StaffRoleDto> roles = ValidRoles();
            roles[0].CapacityPerDay = 0;
            Assert.Throws<ContentException>(() => ContentLoader.Build(ValidEconomy(), roles));
        }

        [Fact]
        public void No_tier_is_found_for_an_unknown_table_count()
        {
            EconomyConfig cfg = ContentLoader.LoadEconomy(Paths.Content);

            // The query DOES NOT THROW, it returns the nearest tier below.
            //
            // It used to throw, and that was the wrong place for it: a reader
            // must never bring things down. An invalid table count coming out of
            // a corrupt save killed the game the moment the player opened the
            // Staff screen and left the save unusable. A save's validity is now
            // checked ON LOAD (Simulation.Validate).
            Assert.False(cfg.HasTierForTables(99));
            Assert.Equal(cfg.TierAt(cfg.TierCount - 1).Tables,
                         cfg.TierForTables(99).Tables);

            // For a number below all the tiers, the lowest tier.
            Assert.False(cfg.HasTierForTables(1));
            Assert.Equal(cfg.TierAt(0).Tables, cfg.TierForTables(1).Tables);
        }

        // ====================================================================
        // unlockSeason: derived from unlockDay, not a separate fact.
        // ====================================================================
        [Theory]
        [InlineData("fastfood")]
        [InlineData("turk")]
        public void The_season_is_derived_from_the_day(string cuisine)
        {
            ContentSet c = ContentSetLoader.Load(Paths.Content, cuisine);
            EconomyConfig e = ContentLoader.LoadEconomy(Paths.Content);

            for (int i = 0; i < c.Dishes.Length; i++)
            {
                DishDef d = c.Dishes[i];
                int expected = (d.UnlockDay - 1) / e.SeasonDays + 1;
                if (expected > 4) expected = 4;
                Assert.Equal(expected, d.UnlockSeason);
            }
        }

        [Fact]
        public void A_drifted_season_is_rejected()
        {
            // Why this test exists: the field was not deleted, it was turned into
            // an INVARIANT. If the invariant does not run, the field is dead
            // again.
            string path = Path.Combine(Paths.Content, "dishes", "fastfood.json");
            List<DishDto> dishes = Newtonsoft.Json.JsonConvert
                .DeserializeObject<List<DishDto>>(File.ReadAllText(path));
            List<IngredientDto> ing = Newtonsoft.Json.JsonConvert
                .DeserializeObject<List<IngredientDto>>(
                    File.ReadAllText(Path.Combine(Paths.Content, "ingredients.json")));
            List<ArchetypeDto> arc = Newtonsoft.Json.JsonConvert
                .DeserializeObject<List<ArchetypeDto>>(
                    File.ReadAllText(Path.Combine(Paths.Content, "archetypes", "shared.json")));
            arc.AddRange(Newtonsoft.Json.JsonConvert
                .DeserializeObject<List<ArchetypeDto>>(
                    File.ReadAllText(Path.Combine(Paths.Content, "archetypes", "fastfood.json"))));
            EquipmentFileDto eq = Newtonsoft.Json.JsonConvert
                .DeserializeObject<EquipmentFileDto>(
                    File.ReadAllText(Path.Combine(Paths.Content, "equipment.json")));
            CuisineDto cui = Newtonsoft.Json.JsonConvert
                .DeserializeObject<CuisineDto>(
                    File.ReadAllText(Path.Combine(Paths.Content, "cuisines", "fastfood.json")));

            // First the sound content is opened.
            ContentSetLoader.Build("fastfood", ing, dishes, arc, eq, cui, 15);

            // Then a single dish's season is shifted.
            for (int i = 0; i < dishes.Count; i++)
            {
                if (dishes[i].UnlockDay <= 1) continue;
                dishes[i].UnlockSeason = dishes[i].UnlockSeason == 4 ? 1 : 4;
                break;
            }
            Assert.Throws<ContentException>(
                () => ContentSetLoader.Build("fastfood", ing, dishes, arc, eq, cui, 15));
        }

        [Theory]
        [InlineData("fastfood")]
        [InlineData("turk")]
        public void The_unlock_gates_follow_the_generators_rule(string cuisine)
        {
            // This test has a raw reason to exist: requiresStationTier and
            // unlockReputationCenti were once written into the JSON by hand and
            // THE GENERATOR DID NOT KNOW ABOUT THEM. Running gen_dishes.py
            // silently deleted the entire unlock system - nothing broke, every
            // dish was simply left unlocked.
            //
            // The rule now lives in the generator (tools/content/gen_dishes.py)
            // and the same rule is tested here from the outside.
            ContentSet c = ContentSetLoader.Load(Paths.Content, cuisine);

            foreach (DishDef d in c.Dishes)
            {
                // The reputation threshold is proportional to the day: 0.9 points per day.
                Assert.Equal((d.UnlockDay - 1) * 90, d.UnlockReputationCenti);

                // The opening menu asks for nothing.
                if (d.UnlockDay <= 1)
                {
                    Assert.Equal(0, d.RequiresStationTier);
                    Assert.Equal(0, d.UnlockReputationCenti);
                }

                // A dish tied to named equipment asks for EXACTLY tier 1: that
                // ladder has two rungs, there is no such thing as tier 2.
                StationDef st = c.Stations[d.StationIndex];
                if (!st.Shared)
                {
                    Assert.Equal(1, d.RequiresStationTier);
                    Assert.Equal(1, st.MaxTier);
                }
            }
        }

        [Fact]
        public void The_Turkish_cuisine_keeps_doner_and_pide_on_their_own_equipment()
        {
            // What the user asked for: "doner should need a counter with a doner
            // spit on it, pide should need a stone oven". Separate equipment,
            // separate unlock.
            ContentSet c = ContentSetLoader.Load(Paths.Content, "turk");

            foreach (string pair in new[] { "doner:doner_ocagi",
                                            "iskender:doner_ocagi",
                                            "kiymali_pide:pide_firini",
                                            "lahmacun:pide_firini" })
            {
                string[] parts = pair.Split(':');
                int i = c.DishIndexOf(parts[0]);
                Assert.True(i >= 0, parts[0] + " is not on the menu");
                Assert.Equal(parts[1], c.Stations[c.Dishes[i].StationIndex].Id);
                Assert.Equal(1, c.Dishes[i].RequiresStationTier);
            }
        }

        [Theory]
        [InlineData("fastfood")]
        [InlineData("turk")]
        public void The_season_spread_follows_the_docs09_curve(string cuisine)
        {
            // The docs/09 progression curve: 6 -> 13 -> 21 -> 27 -> 32
            // cumulative, that is 6, 7, 8, 6, 5 per season. In the first season 6
            // opening dishes + 7 new = 13. The curve lives in the content and the
            // table lives in docs/09; if the two drift apart the game's pace
            // differs from the document.
            ContentSet c = ContentSetLoader.Load(Paths.Content, cuisine);

            int[] perSeason = new int[5];
            for (int s = 1; s <= 4; s++) perSeason[s] = c.DishCountInSeason(s);

            Assert.Equal(32, perSeason[1] + perSeason[2] + perSeason[3] + perSeason[4]);
            Assert.Equal(13, perSeason[1]);
            Assert.Equal(8, perSeason[2]);
            Assert.Equal(6, perSeason[3]);
            Assert.Equal(5, perSeason[4]);
        }
    }
}
