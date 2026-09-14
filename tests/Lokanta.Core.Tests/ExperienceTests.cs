using System.Collections.Generic;
using System.IO;
using Lokanta.Content;
using Lokanta.Core.Content;
using Lokanta.Core.Economy;
using Lokanta.Core.Sim;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;

namespace Lokanta.Core.Tests
{
    /// <summary>
    /// docs/14-personel-sistemi.md "Deneyim ve seviye":
    ///   - calisilan her gun 1 puan
    ///   - 30 puanda seviye atlar, azami 3 seviye
    ///   - her seviye hiz +%10
    ///
    /// Merdiven icerikte (staff-roles.json xpSpeedBp) aylardir yaziliydi ve
    /// hicbir yerde okunmuyordu - denetleyicinin kuyrugundaki son
    /// maddelerden biri.
    ///
    /// Onemli sinir: deneyim YEMEGIN PISME SURESINI kisaltmiyor. docs/27
    /// Karar D ekipman icin ne diyorsa deneyim icin de gecerli - kisalan
    /// sey kisinin o ise BAGLI KALDIGI sure.
    /// </summary>
    public class ExperienceTests
    {
        private readonly ITestOutputHelper _out;
        public ExperienceTests(ITestOutputHelper output) { _out = output; }

        private const ulong Seed = 20260910UL;

        private static EconomyConfig Economy() => ContentLoader.LoadEconomy(Paths.Content);
        private static ContentSet Content() => ContentSetLoader.Load(Paths.Content, "fastfood");

        private static TimingConfig Timing()
        {
            ContentSet c = Content();
            return c.SlotDurationsBp != null
                ? TimingConfig.Default().WithSlotDurations(c.SlotDurationsBp).WithEatMs(c.EatMs)
                : TimingConfig.Default();
        }

        private static Simulation NewSim() =>
            new Simulation(Economy(), Content(), Timing(), Seed);

        private static void Restock(Simulation sim)
        {
            for (int i = 0; i < sim.IngredientCount; i++)
            {
                int need = sim.RecommendedRestock(i);
                if (need > 0)
                    sim.Apply(new Command(sim.TickIndex, CommandKind.OrderIngredient, i, need));
            }
        }

        private static DayReport RunOneDay(Simulation sim)
        {
            Restock(sim);
            sim.Apply(new Command(sim.TickIndex, CommandKind.OpenService));
            int limit = Timing().ServiceTicks + 4000;
            for (int t = 0; t < limit; t++)
            {
                sim.Tick();
                if (sim.ServiceComplete) break;
            }
            sim.Apply(new Command(sim.TickIndex, CommandKind.CloseDay));
            DayReport rep = sim.BuildDayReport();
            sim.AdvanceToNextDay();
            return rep;
        }

        // ====================================================================
        [Fact]
        public void Icerik_dort_basamakli_merdiven_yukluyor()
        {
            EconomyConfig e = Economy();
            Assert.Equal(30, e.XpDaysPerLevel);
            Assert.Equal(3, e.MaxXpLevel);

            // Seviye 0 hizsiz, her seviye +%10.
            Assert.Equal(10_000, e.XpSpeedBp(0, true));
            Assert.Equal(11_000, e.XpSpeedBp(1, true));
            Assert.Equal(12_000, e.XpSpeedBp(2, true));
            Assert.Equal(13_000, e.XpSpeedBp(3, true));

            // Tavanin ustu tavana kirpiliyor, dizi disina tasilmiyor.
            Assert.Equal(13_000, e.XpSpeedBp(9, true));
        }

        [Fact]
        public void Otuz_gunde_seviye_atliyor()
        {
            Simulation sim = NewSim();
            Assert.Equal(0, sim.StaffLevel(0, 0));

            for (int d = 0; d < 29; d++) RunOneDay(sim);
            Assert.Equal(29, sim.StaffDaysWorked(0, 0));
            Assert.Equal(0, sim.StaffLevel(0, 0));

            RunOneDay(sim);
            Assert.Equal(30, sim.StaffDaysWorked(0, 0));
            Assert.Equal(1, sim.StaffLevel(0, 0));
        }

        [Fact]
        public void Seviye_ucte_duruyor()
        {
            // 60 gunluk kampanyada ucuncu seviye 90. gune dusuyor: tavan
            // kampanya suresinde ULASILAMAZ. docs/29 serbest oyunun devam
            // ettigini soyluyor, o yuzden tavan yine de sinaniyor.
            EconomyConfig e = Economy();
            Assert.Equal(0, e.XpLevelOf(0));
            Assert.Equal(0, e.XpLevelOf(29));
            Assert.Equal(1, e.XpLevelOf(30));
            Assert.Equal(2, e.XpLevelOf(60));
            Assert.Equal(3, e.XpLevelOf(90));
            Assert.Equal(3, e.XpLevelOf(300));
        }

        [Fact]
        public void Sonradan_alinan_sifirdan_basliyor()
        {
            Simulation sim = NewSim();
            for (int d = 0; d < 35; d++) RunOneDay(sim);

            sim.Apply(new Command(sim.TickIndex, CommandKind.Hire, 0));
            Assert.Equal(2, sim.Cooks);

            // Eskisi bir seviye atlamis, yenisi sifirda.
            Assert.Equal(1, sim.StaffLevel(0, 0));
            Assert.Equal(0, sim.StaffLevel(0, 1));
            Assert.Equal(0, sim.StaffDaysWorked(0, 1));
        }

        [Fact]
        public void Kovup_yeniden_almak_deneyimi_sifirliyor()
        {
            // Kovma SONDAN aliyor: en yeni giden. Aksi halde "en deneyimliyi
            // kov" diye anlamsiz bir karar dogardi. Geri alinan da yeni biri:
            // kadro kesip geri almak bedava degil.
            Simulation sim = NewSim();
            sim.Apply(new Command(sim.TickIndex, CommandKind.Hire, 0));
            for (int d = 0; d < 31; d++) RunOneDay(sim);

            // Kesin bir SEVIYE beklemiyoruz: huy da isin icinde. Cirak
            // gunde iki puan kazaniyor, tecrubeli hic kazanmiyor (docs/14).
            // Sinanan sey seviyenin degeri degil, kovulunca SIFIRLANMASI.
            int before0 = sim.StaffLevel(0, 0);
            int days0 = sim.StaffDaysWorked(0, 0);
            Assert.True(sim.StaffDaysWorked(0, 1) > 0);

            sim.Apply(new Command(sim.TickIndex, CommandKind.Fire, 0));
            sim.Apply(new Command(sim.TickIndex, CommandKind.Hire, 0));

            Assert.Equal(2, sim.Cooks);
            Assert.Equal(before0, sim.StaffLevel(0, 0));
            Assert.Equal(days0, sim.StaffDaysWorked(0, 0));
            Assert.Equal(0, sim.StaffLevel(0, 1));
            Assert.Equal(0, sim.StaffDaysWorked(0, 1));
        }

        [Fact]
        public void Deneyim_yemegin_pisme_suresine_dokunmuyor()
        {
            // docs/27 Karar D. Merdiven yalnizca MESGULIYETI boluyor;
            // yemegin duvar saati suresi (prepMs) icerikte sabit kaliyor.
            ContentSet c = Content();
            EconomyConfig e = Economy();

            int prep = 0;
            for (int i = 0; i < c.Dishes.Length; i++)
                if (c.Dishes[i].PrepMs > prep) prep = c.Dishes[i].PrepMs;
            Assert.True(prep > 0);

            long busyAtZero = Fx.MulDiv(prep, Fx.One, e.XpSpeedBp(0, true));
            long busyAtTop = Fx.MulDiv(prep, Fx.One, e.XpSpeedBp(3, true));
            _out.WriteLine($"mesguliyet {busyAtZero} -> {busyAtTop} ms (prepMs {prep})");

            Assert.Equal(prep, (int)busyAtZero);
            Assert.True(busyAtTop < busyAtZero);
        }

        [Fact]
        public void Deneyim_olculebilir_fark_yaratiyor()
        {
            // Ayni tohum, ayni strateji: tek degisken deneyim. Bu test
            // dengeyi degil MEKANIGIN BAGLI OLDUGUNU olcuyor - deneyim
            // hicbir seye dokunmasaydi iki pencere ayni cikardi.
            Simulation sim = NewSim();

            int early = 0, late = 0;
            for (int d = 0; d < 10; d++) early += RunOneDay(sim).ServedPeople;
            for (int d = 0; d < 80; d++) RunOneDay(sim);
            for (int d = 0; d < 10; d++) late += RunOneDay(sim).ServedPeople;

            _out.WriteLine($"ilk on gun {early} kisi, 90-100 arasi {late} kisi");
            Assert.Equal(3, sim.StaffLevel(0, 0));
            Assert.NotEqual(early, late);
        }

        [Fact]
        public void Maas_tablosu_uretecine_uyuyor()
        {
            // weeklyWageMultiplierBp ile weeklyXpWageGrowthBp AYNI SEYI iki
            // yerde yaziyor. Tablo silinmedi (denge araci onu okuyor) ama
            // artik degismez: ContentLoader acilista dogruluyor. Bu test
            // dogrulamanin calistigini gosteriyor - bozuk bir tablo oyunu
            // ACMAMALI.
            string dir = Paths.Content;
            string economyJson = File.ReadAllText(Path.Combine(dir, "economy.json"));
            string rolesJson = File.ReadAllText(Path.Combine(dir, "staff-roles.json"));

            EconomyConfig ok = ContentLoader.LoadEconomy(dir);
            Assert.Equal(220, ok.WeeklyXpWageGrowthBp);

            EconomyDto dto = JsonConvert.DeserializeObject<EconomyDto>(economyJson);
            List<StaffRoleDto> roles =
                JsonConvert.DeserializeObject<List<StaffRoleDto>>(rolesJson);

            dto.Staffing.WeeklyWageMultiplierBp[3] += 50;

            ContentException ex = Assert.Throws<ContentException>(
                () => ContentLoader.Build(dto, roles));
            _out.WriteLine(ex.Message);
            Assert.Contains("weeklyWageMultiplierBp", ex.Message);
        }

        [Fact]
        public void Merdiven_bozuksa_oyun_acilmiyor()
        {
            string dir = Paths.Content;
            EconomyDto dto = JsonConvert.DeserializeObject<EconomyDto>(
                File.ReadAllText(Path.Combine(dir, "economy.json")));
            List<StaffRoleDto> roles = JsonConvert.DeserializeObject<List<StaffRoleDto>>(
                File.ReadAllText(Path.Combine(dir, "staff-roles.json")));

            // Azalan merdiven: deneyim kazanan personel yavaslamaz.
            roles[0].XpSpeedBp = new List<int> { 10000, 11000, 9000, 13000 };
            Assert.Throws<ContentException>(() => ContentLoader.Build(dto, roles));

            // Sifirinci basamak 10000 olmali: seviye 0 hizsizdir.
            roles[0].XpSpeedBp = new List<int> { 12000, 13000, 14000, 15000 };
            Assert.Throws<ContentException>(() => ContentLoader.Build(dto, roles));
        }
    }
}
