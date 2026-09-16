using Lokanta.Content;
using Lokanta.Core.Economy;
using Lokanta.Game;
using Xunit;
using Xunit.Abstractions;

namespace Lokanta.Core.Tests
{
    /// <summary>
    /// The floor plan. docs/31-rooms-and-camera.md and the touch target
    /// measurement.
    ///
    /// Why this file exists: until now the plan was validated ONLY in the editor
    /// script, which means it was only tested once Unity was open. The floor plan
    /// is part of the economy - the number of tables per tier is the very table
    /// the rents were solved from - and it must not be left untested.
    /// </summary>
    public class RoomPlanTests
    {
        private readonly ITestOutputHelper _out;
        public RoomPlanTests(ITestOutputHelper output) { _out = output; }

        [Fact]
        public void The_rooms_cover_the_plot_EXACTLY()
        {
            // If a gap is left there is a hole in the render; if they overlap a
            // wall is drawn twice. Both are hard to see by eye and easy to
            // measure.
            float area = 0f;
            foreach (RoomPlan.Room r in RoomPlan.Rooms) area += r.W * r.D;

            float plot = RoomPlan.PlotW * RoomPlan.PlotD;
            _out.WriteLine($"rooms {area:0.00} m2, plot {plot:0.00} m2");
            Assert.True(System.Math.Abs(area - plot) < 0.01f);
        }

        [Fact]
        public void The_rooms_do_not_overlap()
        {
            RoomPlan.Room[] rooms = RoomPlan.Rooms;
            for (int i = 0; i < rooms.Length; i++)
                for (int j = i + 1; j < rooms.Length; j++)
                {
                    bool overlap =
                        rooms[i].X0 < rooms[j].X0 + rooms[j].W - 0.001f &&
                        rooms[j].X0 < rooms[i].X0 + rooms[i].W - 0.001f &&
                        rooms[i].Z0 < rooms[j].Z0 + rooms[j].D - 0.001f &&
                        rooms[j].Z0 < rooms[i].Z0 + rooms[i].D - 0.001f;
                    Assert.False(overlap, rooms[i].Name + " overlaps " + rooms[j].Name);
                }
        }

        [Fact]
        public void The_rooms_do_not_spill_outside_the_plot()
        {
            foreach (RoomPlan.Room r in RoomPlan.Rooms)
            {
                Assert.True(r.X0 >= -0.001f && r.Z0 >= -0.001f, r.Name + " starts at a negative coordinate");
                Assert.True(r.X0 + r.W <= RoomPlan.PlotW + 0.001f, r.Name + " overflows the width");
                Assert.True(r.Z0 + r.D <= RoomPlan.PlotD + 0.001f, r.Name + " overflows the depth");
            }
        }

        [Fact]
        public void The_table_counts_match_the_TIER_table()
        {
            // The plan and the economy must say the same number. The docs/12
            // tiers are 4 / 7 / 10 / 14 tables; the rooms have to give exactly
            // that, otherwise the player cannot see the table they bought out in
            // the hall.
            EconomyConfig cfg = ContentLoader.LoadEconomy(Paths.Content);

            int running = 0;
            int tier = 0;
            foreach (RoomPlan.Room r in RoomPlan.Rooms)
            {
                if (!r.IsDining) continue;
                running += r.Tables;
                _out.WriteLine($"{r.Name,-8} +{r.Tables} = {running} tables " +
                               $"(tier {tier} -> {cfg.TierAt(tier).Tables})");
                Assert.Equal(cfg.TierAt(tier).Tables, running);
                tier++;
            }
            Assert.Equal(cfg.TierCount, tier);
        }

        [Fact]
        public void Every_dining_room_TAKES_its_tables()
        {
            // Without Fit()'s epsilon, 4.6 - 0.9 comes out as 3.6999998 and a
            // column disappears. The room then cannot hold the tables it says it
            // holds, and the tier table silently lies.
            foreach (RoomPlan.Room r in RoomPlan.Rooms)
            {
                if (!r.IsDining) continue;
                RoomPlan.Fit(in r, out int cols, out int rows);
                _out.WriteLine($"{r.Name,-8} {cols}x{rows} slots, {r.Tables} tables required");
                Assert.True(cols * rows >= r.Tables,
                            r.Name + " cannot take its tables: " + cols + "x" + rows);
            }
        }

        [Fact]
        public void The_table_spots_are_inside_their_own_room()
        {
            EconomyConfig cfg = ContentLoader.LoadEconomy(Paths.Content);
            int maxTables = cfg.TierAt(cfg.TierCount - 1).Tables;

            var spots = RoomPlan.TableSpots(maxTables);
            Assert.Equal(maxTables, spots.Count);

            foreach (RoomPlan.TableSpot s in spots)
            {
                RoomPlan.Room r = RoomPlan.Rooms[s.Room];
                Assert.True(s.X > r.X0 && s.X < r.X0 + r.W, r.Name + " has a table outside it");
                Assert.True(s.Z > r.Z0 && s.Z < r.Z0 + r.D, r.Name + " has a table outside it");
            }
        }

        [Fact]
        public void The_tables_are_not_too_close_together()
        {
            // The touch target: the gap between table sets decides the size of
            // the thing being tapped. Two overlapping tables mean one single
            // touch target.
            EconomyConfig cfg = ContentLoader.LoadEconomy(Paths.Content);
            var spots = RoomPlan.TableSpots(cfg.TierAt(cfg.TierCount - 1).Tables);

            float worst = float.MaxValue;
            for (int i = 0; i < spots.Count; i++)
                for (int j = i + 1; j < spots.Count; j++)
                {
                    float dx = spots[i].X - spots[j].X;
                    float dz = spots[i].Z - spots[j].Z;
                    float d = (float)System.Math.Sqrt(dx * dx + dz * dz);
                    if (d < worst) worst = d;
                }

            _out.WriteLine($"the two closest tables are {worst:0.00} m apart");
            Assert.True(worst > 1.2f, $"the tables are too close: {worst:0.00} m");
        }
    }
}
