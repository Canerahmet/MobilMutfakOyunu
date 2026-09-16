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
        [JsonProperty("hall")] public int Hall { get; set; }
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
    /// docs/23-core-contract.md 10, the final acceptance criterion:
    /// the C# core and tools/balance/model.py must match across the eight-week
    /// table.
    ///
    /// This validates three things in a single test:
    ///   1. Fx integer arithmetic does not drift from the decimal model
    ///   2. the content JSON loads correctly
    ///   3. the demand, crew and weekly account formulas were ported correctly
    /// </summary>
    public class GoldenWeeklyTests
    {
        private static GoldenFile LoadGolden()
        {
            string path = Path.Combine(Paths.Golden, "weekly.json");
            Assert.True(File.Exists(path),
                "No golden data. Run 'python tools/balance/export.py' first. Expected: " + path);
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
        public void The_customer_counts_match_exactly()
        {
            GoldenFile golden = LoadGolden();
            WeekResult[] got = RunModel(golden, LoadConfig());

            for (int i = 0; i < golden.Weeks.Count; i++)
            {
                GoldenWeek g = golden.Weeks[i];
                Assert.True(g.WeekdayCustomers == got[i].WeekdayCustomers,
                    $"W{g.Week} weekday: expected {g.WeekdayCustomers}, got {got[i].WeekdayCustomers}");
                Assert.True(g.WeekendCustomers == got[i].WeekendCustomers,
                    $"W{g.Week} weekend: expected {g.WeekendCustomers}, got {got[i].WeekendCustomers}");
                Assert.True(g.WeekCustomers == got[i].WeekCustomers,
                    $"W{g.Week} week total: expected {g.WeekCustomers}, got {got[i].WeekCustomers}");
            }
        }

        [Fact]
        public void The_crew_matches_exactly()
        {
            GoldenFile golden = LoadGolden();
            WeekResult[] got = RunModel(golden, LoadConfig());

            for (int i = 0; i < golden.Weeks.Count; i++)
            {
                GoldenWeek g = golden.Weeks[i];
                Assert.True(g.Cooks == got[i].Crew.Cooks,
                    $"W{g.Week} cooks: expected {g.Cooks}, got {got[i].Crew.Cooks}");
                Assert.True(g.Hall == got[i].Crew.Hall,
                    $"W{g.Week} hall: expected {g.Hall}, got {got[i].Crew.Hall}");
                Assert.True(g.CrewTotal == got[i].Crew.Total,
                    $"W{g.Week} crew total: expected {g.CrewTotal}, got {got[i].Crew.Total}");
            }
        }

        [Fact]
        public void The_money_fields_match_within_tolerance()
        {
            GoldenFile golden = LoadGolden();
            long tol = golden.ToleranceCenti;
            WeekResult[] got = RunModel(golden, LoadConfig());

            for (int i = 0; i < golden.Weeks.Count; i++)
            {
                GoldenWeek g = golden.Weeks[i];
                WeekResult r = got[i];
                Near(g.Week, "revenue", g.Revenue, r.Revenue, tol);
                Near(g.Week, "ingredients", g.Ingredients, r.Ingredients, tol);
                Near(g.Week, "wages", g.Wages, r.Wages, tol);
                Near(g.Week, "rent", g.Rent, r.Rent, 0);
                Near(g.Week, "expansion", g.Expansion, r.Expansion, 0);
                Near(g.Week, "net", g.Net, r.Net, tol);
                Near(g.Week, "cash", g.Cash, r.Cash, tol);
            }
        }

        private static void Near(int week, string field, long expected, long actual, long tol)
        {
            long diff = Math.Abs(expected - actual);
            Assert.True(diff <= tol,
                $"W{week} {field}: expected {expected}, got {actual}, difference {diff} centi-coins (tolerance {tol})");
        }

        [Fact]
        public void The_design_constraints_still_hold()
        {
            GoldenFile golden = LoadGolden();
            EconomyConfig cfg = LoadConfig();
            WeekResult[] got = RunModel(golden, cfg);

            for (int i = 0; i < got.Length; i++)
            {
                WeekResult r = got[i];
                Assert.True(r.CrewWithinCap,
                    $"W{r.Week} exceeds the crew cap: {r.Crew.Total}/{r.StaffCap}");
                Assert.True(r.Cash > 0, $"W{r.Week} the till went negative: {r.Cash}");

                if (r.Expansion > 0)
                    Assert.True(r.Net < 0, $"W{r.Week} an expansion week does not lose money: {r.Net}");
                else
                    Assert.True(r.Net > 0, $"W{r.Week} a mature week loses money: {r.Net}");

                if (i > 0)
                    Assert.True(r.Crew.Total >= got[i - 1].Crew.Total,
                        $"W{r.Week} the crew goes backwards");
            }

            // The first hire must fall in the second week: not a script, a result of the workload
            Assert.Equal(1, got[0].Crew.Total);
            Assert.Equal(2, got[1].Crew.Total);
        }

        [Theory]
        [InlineData("tr-TR")]   // the dotted/dotless i trap
        [InlineData("en-US")]
        [InlineData("de-DE")]   // comma as the decimal separator
        [InlineData("ar-SA")]   // different digit shapes
        public void The_culture_does_not_change_the_result(string cultureName)
        {
            // The docs/23 4.3 culture test. The developer's machine is Turkish,
            // most of the players' machines are not: the bug is invisible here
            // and only here.
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
        public void The_same_input_gives_the_same_output()
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
