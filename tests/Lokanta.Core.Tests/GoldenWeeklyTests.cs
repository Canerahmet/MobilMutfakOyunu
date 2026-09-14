using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using Lokanta.Content;
using Lokanta.Core.Economy;
using Newtonsoft.Json;
using Xunit;

namespace Lokanta.Core.Tests
{
    public sealed class GoldenWeek
    {
        [JsonProperty("week")] public int Week { get; set; }
        [JsonProperty("tables")] public int Tables { get; set; }
        [JsonProperty("reputationCenti")] public int ReputationCenti { get; set; }
        [JsonProperty("ticket")] public long Ticket { get; set; }
        [JsonProperty("weekdayCustomers")] public int WeekdayCustomers { get; set; }
        [JsonProperty("weekendCustomers")] public int WeekendCustomers { get; set; }
        [JsonProperty("weekCustomers")] public int WeekCustomers { get; set; }
        [JsonProperty("cooks")] public int Cooks { get; set; }
        [JsonProperty("salon")] public int Salon { get; set; }
        [JsonProperty("crewTotal")] public int CrewTotal { get; set; }
        [JsonProperty("staffCap")] public int StaffCap { get; set; }
        [JsonProperty("revenue")] public long Revenue { get; set; }
        [JsonProperty("ingredients")] public long Ingredients { get; set; }
        [JsonProperty("wages")] public long Wages { get; set; }
        [JsonProperty("rent")] public long Rent { get; set; }
        [JsonProperty("expansion")] public long Expansion { get; set; }
        [JsonProperty("net")] public long Net { get; set; }
        [JsonProperty("cash")] public long Cash { get; set; }
    }

    public sealed class GoldenFile
    {
        [JsonProperty("source")] public string Source { get; set; }
        [JsonProperty("toleranceCenti")] public long ToleranceCenti { get; set; }
        [JsonProperty("weeks")] public List<GoldenWeek> Weeks { get; set; }
    }

    /// <summary>
    /// docs/23-cekirdek-sozlesmesi.md 10, son kabul olcutu:
    /// C# cekirdek ile tools/balance/model.py sekiz haftalik tabloda eslesmeli.
    ///
    /// Bu, tek bir testte uc seyi birden dogruluyor:
    ///   1. Fx tamsayi aritmetigi ondalik modelden sapmiyor
    ///   2. Icerik JSON'u dogru yukleniyor
    ///   3. Talep, kadro ve haftalik hesap formulleri dogru cevrildi
    /// </summary>
    public class GoldenWeeklyTests
    {
        private static GoldenFile LoadGolden()
        {
            string path = Path.Combine(Paths.Golden, "weekly.json");
            Assert.True(File.Exists(path),
                "Altin veri yok. Once 'python tools/balance/export.py' calistirin. Beklenen: " + path);
            return JsonConvert.DeserializeObject<GoldenFile>(File.ReadAllText(path));
        }

        private static EconomyConfig LoadConfig() => ContentLoader.LoadEconomy(Paths.Content);

        private static WeekResult[] RunModel(GoldenFile golden, EconomyConfig cfg)
        {
            WeekPlan[] plans = new WeekPlan[golden.Weeks.Count];
            for (int i = 0; i < plans.Length; i++)
            {
                GoldenWeek g = golden.Weeks[i];
                plans[i] = new WeekPlan(g.Week, g.Tables, g.ReputationCenti, g.Ticket);
            }
            return WeeklyPlanner.Run(plans, cfg);
        }

        [Fact]
        public void Musteri_sayilari_birebir_esitr()
        {
            GoldenFile golden = LoadGolden();
            WeekResult[] got = RunModel(golden, LoadConfig());

            for (int i = 0; i < golden.Weeks.Count; i++)
            {
                GoldenWeek g = golden.Weeks[i];
                Assert.True(g.WeekdayCustomers == got[i].WeekdayCustomers,
                    $"H{g.Week} hafta ici: beklenen {g.WeekdayCustomers}, gelen {got[i].WeekdayCustomers}");
                Assert.True(g.WeekendCustomers == got[i].WeekendCustomers,
                    $"H{g.Week} hafta sonu: beklenen {g.WeekendCustomers}, gelen {got[i].WeekendCustomers}");
                Assert.True(g.WeekCustomers == got[i].WeekCustomers,
                    $"H{g.Week} hafta toplami: beklenen {g.WeekCustomers}, gelen {got[i].WeekCustomers}");
            }
        }

        [Fact]
        public void Kadro_birebir_esittir()
        {
            GoldenFile golden = LoadGolden();
            WeekResult[] got = RunModel(golden, LoadConfig());

            for (int i = 0; i < golden.Weeks.Count; i++)
            {
                GoldenWeek g = golden.Weeks[i];
                Assert.True(g.Cooks == got[i].Crew.Cooks,
                    $"H{g.Week} asci: beklenen {g.Cooks}, gelen {got[i].Crew.Cooks}");
                Assert.True(g.Salon == got[i].Crew.Salon,
                    $"H{g.Week} salon: beklenen {g.Salon}, gelen {got[i].Crew.Salon}");
                Assert.True(g.CrewTotal == got[i].Crew.Total,
                    $"H{g.Week} toplam kadro: beklenen {g.CrewTotal}, gelen {got[i].Crew.Total}");
            }
        }

        [Fact]
        public void Para_alanlari_tolerans_icinde_esitr()
        {
            GoldenFile golden = LoadGolden();
            long tol = golden.ToleranceCenti;
            WeekResult[] got = RunModel(golden, LoadConfig());

            for (int i = 0; i < golden.Weeks.Count; i++)
            {
                GoldenWeek g = golden.Weeks[i];
                WeekResult r = got[i];
                Near(g.Week, "ciro", g.Revenue, r.Revenue, tol);
                Near(g.Week, "malzeme", g.Ingredients, r.Ingredients, tol);
                Near(g.Week, "maas", g.Wages, r.Wages, tol);
                Near(g.Week, "kira", g.Rent, r.Rent, 0);
                Near(g.Week, "genisleme", g.Expansion, r.Expansion, 0);
                Near(g.Week, "net", g.Net, r.Net, tol);
                Near(g.Week, "kasa", g.Cash, r.Cash, tol);
            }
        }

        private static void Near(int week, string field, long expected, long actual, long tol)
        {
            long diff = Math.Abs(expected - actual);
            Assert.True(diff <= tol,
                $"H{week} {field}: beklenen {expected}, gelen {actual}, fark {diff} santi-sikke (tolerans {tol})");
        }

        [Fact]
        public void Tasarim_kisitlari_hala_gecerli()
        {
            GoldenFile golden = LoadGolden();
            EconomyConfig cfg = LoadConfig();
            WeekResult[] got = RunModel(golden, cfg);

            for (int i = 0; i < got.Length; i++)
            {
                WeekResult r = got[i];
                Assert.True(r.CrewWithinCap,
                    $"H{r.Week} kadro tavani asiyor: {r.Crew.Total}/{r.StaffCap}");
                Assert.True(r.Cash > 0, $"H{r.Week} kasa eksiye dustu: {r.Cash}");

                if (r.Expansion > 0)
                    Assert.True(r.Net < 0, $"H{r.Week} genisleme haftasi zarar etmiyor: {r.Net}");
                else
                    Assert.True(r.Net > 0, $"H{r.Week} olgun hafta zarar ediyor: {r.Net}");

                if (i > 0)
                    Assert.True(r.Crew.Total >= got[i - 1].Crew.Total,
                        $"H{r.Week} kadro geri gidiyor");
            }

            // Ilk ise alim ikinci haftada olmali: senaryo degil, is yuku sonucu
            Assert.Equal(1, got[0].Crew.Total);
            Assert.Equal(2, got[1].Crew.Total);
        }

        [Theory]
        [InlineData("tr-TR")]   // noktali/noktasiz i tuzagi
        [InlineData("en-US")]
        [InlineData("de-DE")]   // ondalik ayraci virgul
        [InlineData("ar-SA")]   // farkli rakam sekilleri
        public void Kultur_sonucu_degistirmez(string cultureName)
        {
            // docs/23 4.3 kultur testi. Gelistiricinin makinesi Turkce,
            // oyuncularin cogunun degil: hata sadece burada gorunmez.
            CultureInfo previous = Thread.CurrentThread.CurrentCulture;
            try
            {
                CultureInfo culture = new CultureInfo(cultureName);
                Thread.CurrentThread.CurrentCulture = culture;
                CultureInfo.DefaultThreadCurrentCulture = culture;

                GoldenFile golden = LoadGolden();
                WeekResult[] got = RunModel(golden, LoadConfig());

                for (int i = 0; i < golden.Weeks.Count; i++)
                {
                    GoldenWeek g = golden.Weeks[i];
                    Assert.Equal(g.WeekCustomers, got[i].WeekCustomers);
                    Assert.Equal(g.CrewTotal, got[i].Crew.Total);
                    Near(g.Week, "net", g.Net, got[i].Net, golden.ToleranceCenti);
                }
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = previous;
                CultureInfo.DefaultThreadCurrentCulture = null;
            }
        }

        [Fact]
        public void Ayni_girdi_ayni_ciktiyi_verir()
        {
            GoldenFile golden = LoadGolden();
            EconomyConfig cfg = LoadConfig();

            WeekResult[] a = RunModel(golden, cfg);
            WeekResult[] b = RunModel(golden, cfg);

            for (int i = 0; i < a.Length; i++)
            {
                Assert.Equal(a[i].Net, b[i].Net);
                Assert.Equal(a[i].Cash, b[i].Cash);
                Assert.Equal(a[i].Crew.Total, b[i].Crew.Total);
            }
        }
    }
}
