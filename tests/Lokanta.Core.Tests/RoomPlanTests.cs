using Lokanta.Content;
using Lokanta.Core.Economy;
using Lokanta.Game;
using Xunit;
using Xunit.Abstractions;

namespace Lokanta.Core.Tests
{
    /// <summary>
    /// Kat plani. docs/31-rooms-and-camera.md ve dokunma hedefi olcumu.
    ///
    /// Bu dosyanin varlik sebebi: plan bugune kadar YALNIZCA editor
    /// betiginde dogrulaniyordu, yani ancak Unity acilinca sinaniyordu.
    /// Kat plani ekonominin bir parcasi - kademe basina masa sayisi
    /// kiralarin cozuldugu tablonun ta kendisi - ve testsiz kalmamali.
    /// </summary>
    public class RoomPlanTests
    {
        private readonly ITestOutputHelper _out;
        public RoomPlanTests(ITestOutputHelper output) { _out = output; }

        [Fact]
        public void Odalar_arsayi_TAM_kapliyor()
        {
            // Bosluk kalirsa render'da delik olur; ust uste binerse duvar
            // iki kez cizilir. Ikisi de gozle zor, olcumle kolay.
            float area = 0f;
            foreach (RoomPlan.Room r in RoomPlan.Rooms) area += r.W * r.D;

            float plot = RoomPlan.PlotW * RoomPlan.PlotD;
            _out.WriteLine($"odalar {area:0.00} m2, arsa {plot:0.00} m2");
            Assert.True(System.Math.Abs(area - plot) < 0.01f);
        }

        [Fact]
        public void Odalar_ust_uste_binmiyor()
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
                    Assert.False(overlap, rooms[i].Name + " ile " + rooms[j].Name + " cakisiyor");
                }
        }

        [Fact]
        public void Odalar_arsanin_disina_tasmiyor()
        {
            foreach (RoomPlan.Room r in RoomPlan.Rooms)
            {
                Assert.True(r.X0 >= -0.001f && r.Z0 >= -0.001f, r.Name + " eksi baslangic");
                Assert.True(r.X0 + r.W <= RoomPlan.PlotW + 0.001f, r.Name + " ene tasiyor");
                Assert.True(r.Z0 + r.D <= RoomPlan.PlotD + 0.001f, r.Name + " derinlige tasiyor");
            }
        }

        [Fact]
        public void Masa_sayilari_KADEME_tablosuyla_ayni()
        {
            // Plan ile ekonomi ayni sayiyi soylemeli. docs/12 kademeleri
            // 4 / 7 / 10 / 14 masa; odalar da tam bunu vermeli, yoksa
            // oyuncu satin aldigi masayi salonda goremez.
            EconomyConfig cfg = ContentLoader.LoadEconomy(Paths.Content);

            int running = 0;
            int tier = 0;
            foreach (RoomPlan.Room r in RoomPlan.Rooms)
            {
                if (!r.IsDining) continue;
                running += r.Tables;
                _out.WriteLine($"{r.Name,-8} +{r.Tables} = {running} masa " +
                               $"(kademe {tier} -> {cfg.TierAt(tier).Tables})");
                Assert.Equal(cfg.TierAt(tier).Tables, running);
                tier++;
            }
            Assert.Equal(cfg.TierCount, tier);
        }

        [Fact]
        public void Her_salon_odasi_masalarini_ALIYOR()
        {
            // Fit() epsilonu olmadan 4,6 - 0,9 = 3,6999998 cikiyor ve bir
            // sutun kayboluyor. O zaman oda, tasidigini soyledigi masayi
            // tasiyamiyor ve kademe tablosu sessizce yalan soyluyor.
            foreach (RoomPlan.Room r in RoomPlan.Rooms)
            {
                if (!r.IsDining) continue;
                RoomPlan.Fit(in r, out int cols, out int rows);
                _out.WriteLine($"{r.Name,-8} {cols}x{rows} yuva, {r.Tables} masa isteniyor");
                Assert.True(cols * rows >= r.Tables,
                            r.Name + " masalarini alamiyor: " + cols + "x" + rows);
            }
        }

        [Fact]
        public void Masa_noktalari_kendi_odasinin_icinde()
        {
            EconomyConfig cfg = ContentLoader.LoadEconomy(Paths.Content);
            int maxTables = cfg.TierAt(cfg.TierCount - 1).Tables;

            var spots = RoomPlan.TableSpots(maxTables);
            Assert.Equal(maxTables, spots.Count);

            foreach (RoomPlan.TableSpot s in spots)
            {
                RoomPlan.Room r = RoomPlan.Rooms[s.Room];
                Assert.True(s.X > r.X0 && s.X < r.X0 + r.W, r.Name + " masasi disarida");
                Assert.True(s.Z > r.Z0 && s.Z < r.Z0 + r.D, r.Name + " masasi disarida");
            }
        }

        [Fact]
        public void Masalar_birbirine_cok_yakin_degil()
        {
            // Dokunma hedefi: masa takimlari arasindaki aralik, dokunulacak
            // seyin buyuklugunu belirliyor. Cakisan iki masa, tek bir
            // dokunma hedefi demek.
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

            _out.WriteLine($"en yakin iki masa {worst:0.00} m");
            Assert.True(worst > 1.2f, $"masalar cok yakin: {worst:0.00} m");
        }
    }
}
