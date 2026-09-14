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
            new StaffRoleDto { Id = "garson", Pool = "salon", CapacityPerDay = 25,
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
                OwnerPool = "salon",
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
        public void Gercek_icerik_yuklenir()
        {
            EconomyConfig cfg = ContentLoader.LoadEconomy(Paths.Content);

            Assert.Equal(800000, cfg.StartingCash);
            Assert.Equal(4, cfg.CustomerBasePerTable);
            Assert.Equal(3200, cfg.IngredientRateBp);
            Assert.Equal(30, cfg.CookCapacityPerDay);
            Assert.Equal(14000, cfg.CookDailyWage);
            Assert.Equal(4, cfg.TierCount);

            // Salon havuzu uc rolden olusuyor: 25 + 46 + 66 kapasiteli
            Assert.Equal(73581, cfg.SalonWorkPerCustomerMicro);
            Assert.Equal(1300000, cfg.OwnerWorkMicro);
        }

        [Fact]
        public void Salon_gunluk_ucreti_agirlikli_ortalamayi_verir()
        {
            EconomyConfig cfg = ContentLoader.LoadEconomy(Paths.Content);
            long wage = StaffingModel.SalonDailyWage(cfg);

            // Python modeli 102,3749 sikke veriyor = 10237,49 santi-sikke
            Assert.InRange(wage, 10230, 10245);
        }

        [Fact]
        public void Ondalik_sayi_reddedilir()
        {
            string json = "{ \"reputationDecayPerDay\": 0.3 }";
            ContentException ex = Assert.Throws<ContentException>(
                () => ContentLoader.AssertNoDecimals(json, "test.json"));
            Assert.Contains("ondalik", ex.Message);
        }

        [Fact]
        public void Metin_icindeki_nokta_ondalik_sayilmaz()
        {
            // Surum numarasi ya da dosya adi iceren metinler tetiklememeli
            ContentLoader.AssertNoDecimals("{ \"nameKey\": \"dish.kuru_fasulye\" }", "t.json");
            ContentLoader.AssertNoDecimals("{ \"note\": \"1.5 kat\" }", "t.json");
        }

        [Fact]
        public void Gercek_icerik_dosyalarinda_ondalik_yok()
        {
            foreach (string f in Directory.GetFiles(Paths.Content, "*.json"))
                ContentLoader.AssertNoDecimals(File.ReadAllText(f), Path.GetFileName(f));
        }

        [Fact]
        public void Gecersiz_kimlik_reddedilir()
        {
            List<StaffRoleDto> roles = ValidRoles();
            roles[0].Id = "Asci";   // buyuk harf: tr-TR'de ToLower tuzagi
            ContentException ex = Assert.Throws<ContentException>(
                () => ContentLoader.Build(ValidEconomy(), roles));
            Assert.Contains("Gecersiz kimlik", ex.Message);
        }

        [Fact]
        public void Tekrarlanan_kimlik_reddedilir()
        {
            List<StaffRoleDto> roles = ValidRoles();
            roles[1].Id = "asci";
            Assert.Throws<ContentException>(() => ContentLoader.Build(ValidEconomy(), roles));
        }

        [Fact]
        public void Bilinmeyen_havuz_reddedilir()
        {
            List<StaffRoleDto> roles = ValidRoles();
            roles[1].Pool = "bahce";
            ContentException ex = Assert.Throws<ContentException>(
                () => ContentLoader.Build(ValidEconomy(), roles));
            Assert.Contains("Bilinmeyen havuz", ex.Message);
        }

        [Fact]
        public void Mutfak_rolu_yoksa_reddedilir()
        {
            List<StaffRoleDto> roles = ValidRoles();
            roles.RemoveAt(0);
            Assert.Throws<ContentException>(() => ContentLoader.Build(ValidEconomy(), roles));
        }

        [Fact]
        public void Azalan_kadro_tavani_reddedilir()
        {
            EconomyDto e = ValidEconomy();
            e.Staffing.Tiers[1].StaffCap = 2;
            ContentException ex = Assert.Throws<ContentException>(
                () => ContentLoader.Build(e, ValidRoles()));
            Assert.Contains("kadro tavani", ex.Message);
        }

        [Fact]
        public void Artmayan_masa_sayisi_reddedilir()
        {
            EconomyDto e = ValidEconomy();
            e.Staffing.Tiers[1].Tables = 4;
            Assert.Throws<ContentException>(() => ContentLoader.Build(e, ValidRoles()));
        }

        [Fact]
        public void Sifir_kapasite_reddedilir()
        {
            List<StaffRoleDto> roles = ValidRoles();
            roles[0].CapacityPerDay = 0;
            Assert.Throws<ContentException>(() => ContentLoader.Build(ValidEconomy(), roles));
        }

        [Fact]
        public void Bilinmeyen_masa_sayisi_icin_kademe_bulunmaz()
        {
            EconomyConfig cfg = ContentLoader.LoadEconomy(Paths.Content);

            // Sorgu ISTISNA FIRLATMIYOR, en yakin alt kademeyi donuyor.
            //
            // Once firlatiyordu ve yanlis yerdeydi: bir okuyucu hicbir
            // zaman cokertmemeli. Bozuk bir kayittan gelen gecersiz masa
            // sayisi, oyuncu Personel ekranini actigi anda oyunu olduruyor
            // ve kaydi kullanilamaz birakiyordu. Kaydin gecerliligi artik
            // YUKLEMEDE denetleniyor (Simulation.Validate).
            Assert.False(cfg.HasTierForTables(99));
            Assert.Equal(cfg.TierAt(cfg.TierCount - 1).Tables,
                         cfg.TierForTables(99).Tables);

            // Kademelerin altinda bir sayi icin de en dusuk kademe.
            Assert.False(cfg.HasTierForTables(1));
            Assert.Equal(cfg.TierAt(0).Tables, cfg.TierForTables(1).Tables);
        }

        // ====================================================================
        // unlockSeason: unlockDay'in turetilmisi, ayri bir gercek degil.
        // ====================================================================
        [Theory]
        [InlineData("fastfood")]
        [InlineData("turk")]
        public void Mevsim_gunden_turetiliyor(string cuisine)
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
        public void Ayrisan_mevsim_reddediliyor()
        {
            // Bu testin varlik sebebi: alan silinmedi, DEGISMEZE cevrildi.
            // Degismez calismiyorsa alan yine olu demektir.
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

            // Once saglam icerik aciliyor.
            ContentSetLoader.Build("fastfood", ing, dishes, arc, eq, cui, 15);

            // Sonra tek bir yemegin mevsimi kaydiriliyor.
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
        public void Kilit_kapilari_uretecin_kuralina_uyuyor(string cuisine)
        {
            // Bu testin ac bir sebebi var: requiresStationTier ve
            // unlockReputationCenti bir zamanlar dogrudan JSON'a elle
            // yazilmisti ve URETEC ONLARI BILMIYORDU. gen_dishes.py'yi
            // calistirmak butun kilit sistemini sessizce siliyordu -
            // hicbir sey kirilmadan, sadece her yemek kilitsiz kaliyordu.
            //
            // Kural artik uretecte (tools/content/gen_dishes.py) ve burada
            // ayni kural disaridan sinaniyor.
            ContentSet c = ContentSetLoader.Load(Paths.Content, cuisine);

            foreach (DishDef d in c.Dishes)
            {
                // Itibar esigi gunle dogru orantili: gun basina 0,9 puan.
                Assert.Equal((d.UnlockDay - 1) * 90, d.UnlockReputationCenti);

                // Acilis menusu hicbir sey istemez.
                if (d.UnlockDay <= 1)
                {
                    Assert.Equal(0, d.RequiresStationTier);
                    Assert.Equal(0, d.UnlockReputationCenti);
                }

                // Adlandirilmis ekipmana bagli yemek TAM OLARAK kademe 1
                // ister: o merdiven iki basamakli, kademe 2 diye bir sey yok.
                StationDef st = c.Stations[d.StationIndex];
                if (!st.Shared)
                {
                    Assert.Equal(1, d.RequiresStationTier);
                    Assert.Equal(1, st.MaxTier);
                }
            }
        }

        [Fact]
        public void Turk_mutfagi_doner_ve_pideyi_kendi_ekipmaninda_tutuyor()
        {
            // Kullanicinin istedigi hali: "doner icin doner takilan tezgah
            // gereksin, pide icin tas firin". Ayri ekipman, ayri kilit.
            ContentSet c = ContentSetLoader.Load(Paths.Content, "turk");

            foreach (string pair in new[] { "doner:doner_ocagi",
                                            "iskender:doner_ocagi",
                                            "kiymali_pide:pide_firini",
                                            "lahmacun:pide_firini" })
            {
                string[] parts = pair.Split(':');
                int i = c.DishIndexOf(parts[0]);
                Assert.True(i >= 0, parts[0] + " menude yok");
                Assert.Equal(parts[1], c.Stations[c.Dishes[i].StationIndex].Id);
                Assert.Equal(1, c.Dishes[i].RequiresStationTier);
            }
        }

        [Theory]
        [InlineData("fastfood")]
        [InlineData("turk")]
        public void Mevsim_dagilimi_docs09_egrisini_tutuyor(string cuisine)
        {
            // docs/09 ilerleme egrisi: 6 -> 13 -> 21 -> 27 -> 32 birikimli,
            // yani mevsim basina 6, 7, 8, 6, 5. Ilk mevsimde 6 acilis + 7
            // yeni = 13. Egri iceriktedir ve tablo docs/09'da; ikisi
            // ayrisirsa oyunun temposu belgeden farkli olur.
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
