using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Lokanta.EditorTools
{
    /// <summary>
    /// The modular ROOM-BASED layout and the touch target measurement.
    ///
    /// Round 1 (an open hall, RestaurantScene.cs): a table was 15-20 dp at
    /// every tier against a 48 dp minimum. Failed.
    ///
    /// Round 2 (rooms in a single row): the restaurant turned into a
    /// 25 x 4.6 m corridor, half of the 20:9 frame was left empty, and
    /// because the camera pulled back at every tier the touch target SHRANK
    /// as you grew. The idea of rooms was right, the arrangement wrong.
    ///
    /// Round 3 (an even 2x2 grid): the numbers held up but the render looked
    /// artificial. Every room the same size, every dividing line aligned.
    ///
    /// Round 4, this file: rectangles OF DIFFERENT SIZES covering one plot.
    ///   - The plot is a fixed 18.0 x 9.6 m. The camera frames the OPEN
    ///     rooms and the distance does not change between tiers, so the
    ///     touch target stays independent of the tier.
    ///
    /// Round 5: the camera now uses THE GAME'S own arithmetic
    /// (Lokanta.Game.CameraFit). There used to be separate angles and a
    /// separate distance formula here; the two had drifted apart and the
    /// measurement was looking through a camera 30% further away than the
    /// one the game shows.
    ///   - The rooms are of different sizes and the dividing lines are not
    ///     aligned (z=4.4 on the left half, z=5.0 on the right). That is how
    ///     a real floor plan reads.
    ///   - The walls are generated FROM THE ROOM'S EDGE: an edge whose
    ///     neighbour is built gets a low partition (0.85 m), an unbuilt one
    ///     or one facing outward - the back and left edges - gets a full
    ///     wall (2.6 m), and the front and right edges facing the camera are
    ///     left open. That is why the building grows by itself as rooms are
    ///     added.
    ///
    /// The verification scenes are UNLIT: URP Lit does not work in the
    /// editor's batch mode.
    /// </summary>
    public static class RoomLayout
    {
        private const string OutDir = "../tools/art/out/unity";
        private const int ShotW = 960;
        private const int ShotH = 432;      // 20:9 phone landscape

        // THE NARROWEST ASPECT RATIO IS MEASURED TOO.
        //
        // Phones run between 16:9 and 21:9; on the narrow one the camera goes
        // FURTHER AWAY to fit the same depth (31.7 m against 28.4) and the
        // touch target shrinks. Measuring 20:9 alone meant never seeing the
        // worst case.
        private const int NarrowW = 960;
        private const int NarrowH = 540;    // 16:9

        private const float CellX = Lokanta.Game.RoomPlan.CellX;  // table set pitch, across
        private const float CellZ = Lokanta.Game.RoomPlan.CellZ;  // table set pitch, in depth
        private const float Margin = Lokanta.Game.RoomPlan.Margin; // the room's tableless edge margin

        private const float WallT = 0.14f;
        private const float WallH = 2.60f;
        // 1.10 m was tried and it cut through the backs of the chairs in the
        // row behind.
        private const float PartH = 0.85f;
        private const float DoorW = 1.30f;

        private const float PlotW = Lokanta.Game.RoomPlan.PlotW;
        private const float PlotD = Lokanta.Game.RoomPlan.PlotD;

        private static readonly Color FloorWood = new Color(0.50f, 0.36f, 0.24f);
        private static readonly Color FloorWood2 = new Color(0.45f, 0.32f, 0.21f);
        private static readonly Color FloorKitchen = new Color(0.74f, 0.76f, 0.74f);
        private static readonly Color FloorWet = new Color(0.60f, 0.68f, 0.70f);
        private static readonly Color FloorStore = new Color(0.48f, 0.45f, 0.41f);
        private static readonly Color FloorEntry = new Color(0.68f, 0.63f, 0.55f);
        private static readonly Color FloorEmpty = new Color(0.62f, 0.60f, 0.56f);
        private static readonly Color Wall = new Color(0.88f, 0.86f, 0.80f);
        private static readonly Color Part = new Color(0.80f, 0.77f, 0.70f);
        private static readonly Color Wood = new Color(0.38f, 0.23f, 0.13f);
        private static readonly Color Seat = new Color(0.72f, 0.18f, 0.14f);
        private static readonly Color Metal = new Color(0.62f, 0.64f, 0.66f);
        private static readonly Color Dark = new Color(0.30f, 0.32f, 0.34f);
        private static readonly Color Outline = new Color(0.50f, 0.48f, 0.44f);
        private static readonly Color Temp = new Color(0.78f, 0.72f, 0.58f);

        private struct Room
        {
            public string Name;
            public float X0, Z0, W, D;
            public Color Floor;
            public int Tier;      // 0 = always there, 1..4 = opens at that tier
            public int Tables;    // 0 if it is not a hall

            public float X1 { get { return X0 + W; } }
            public float Z1 { get { return Z0 + D; } }
            public Vector3 Center
            {
                get { return new Vector3(X0 + W * 0.5f, 0.55f, Z0 + D * 0.5f); }
            }
        }

        /// <summary>
        /// The floor plan. ONE SOURCE: Lokanta.Game.RoomPlan.
        ///
        /// This array once stood here with its own numbers while the run time
        /// knew nothing about the plan. Writing the same numbers in two places
        /// has drifted apart silently four times on this project (see
        /// docs/34); so the plan moved into the run time and this tool now
        /// DERIVES from it. The only thing left here is COLOUR - that is,
        /// appearance.
        ///
        /// The rectangles in the plan cover the plot with no gaps but are of
        /// different sizes; the left half's dividing line is at z=4.4, the
        /// right half's at z=5.0. The store room is AT THE BACK and joined to
        /// the kitchen's right edge: a delivery comes in from the back, down
        /// into the store and out into the kitchen.
        /// </summary>
        private static Room[] Plan
        {
            get
            {
                var src = Lokanta.Game.RoomPlan.Rooms;
                Room[] plan = new Room[src.Length];
                for (int i = 0; i < src.Length; i++)
                {
                    plan[i] = new Room
                    {
                        Name = src[i].Name,
                        X0 = src[i].X0, Z0 = src[i].Z0,
                        W = src[i].W, D = src[i].D,
                        Tier = src[i].Tier,
                        Tables = src[i].Tables,
                        Floor = FloorOf(src[i].Name),
                    };
                }
                return plan;
            }
        }

        /// <summary>
        /// The floor colour of a room. THE ROOM NAMES ARE THE ONES RoomPlan.cs
        /// PRODUCES and they are matched by exact string, so they stay as that
        /// file spells them.
        /// </summary>
        private static Color FloorOf(string room)
        {
            switch (room)
            {
                case "Kitchen": return FloorKitchen;
                case "Entry": return FloorEntry;
                case "Sink": return FloorWet;
                case "Store": return FloorStore;
                case "Hall1":
                case "Hall4": return FloorWood;
                case "Hall2":
                case "Hall3": return FloorWood2;
                default: return FloorEmpty;
            }
        }

        private static readonly int[] TierTables = { 4, 7, 10, 14 };

        /// <summary>
        /// The share of the screen taken by the top strip and the action bar
        /// on the game screen. In the game these numbers are MEASURED and
        /// reported to the camera (GameScreen -> CameraRig.SetSafeArea); what
        /// stands here is a stand-in value, because this measurement does not
        /// build the interface. If the bar grows the target shrinks, so these
        /// two numbers should be treated as the interface's UPPER BOUND.
        /// </summary>
        // MEASURED VALUES, NOT GUESSES.
        //
        // The old 0.13 / 0.17 (30% in total) was written before the interface
        // was redesigned and was described as an "upper bound". The tour now
        // measures the strip FOR REAL: 156 dp / 393 dp = 40% in total. So the
        // number was not an upper bound but a LOWER one - the measurement was
        // saying the target was LARGER than it really is.
        //
        // The top strip is 56 dp (a 42 capsule plus padding), the bottom 100 dp
        // (a 62 icon button plus padding plus room for the crisis strip).
        // THESE TWO NUMBERS ARE A COPY OF THE STRIP THE TOUR MEASURES.
        //
        // The tour really measures `GameScreen.StripHeight`; this place runs in
        // editor mode, cannot build the interface, and has to keep a
        // hand-written copy. And that is where the danger is: the tour's strip
        // BUDGET allows 220 dp while this copy assumes 40% (~157 dp). If the
        // strip grew to 200 dp the tour would stay green and the touch target
        // measurement would still be working from 157 dp.
        //
        // So the copy is now tied to the same ceiling as THE TOUR'S BUDGET, and
        // the check below tests that the two agree.
        private const float BandTop = 0.145f;       // 57 dp
        private const float BandBottom = 0.293f;    // 115 dp

        /// <summary>
        /// The strip height (dp) the tour measures AT THE WORST phase.
        ///
        /// The measurement of 13 September 2026: morning 154, service 154,
        /// evening 172 - the same in both languages. The copy was built around
        /// this number.
        ///
        /// Not to be confused with THE TOUR'S BUDGET (220 dp): the budget is
        /// the ceiling that is allowed, this is the value MEASURED TODAY.
        /// Building the copy around the budget would reserve room for a strip
        /// that is not there and report the touch target as SMALLER than it is;
        /// building it below what is measured reports it LARGER. The check
        /// below catches the second case.
        ///
        /// When the strip changes: run the tour, take the largest of the "strip
        /// budget" lines, write it here, and set the band to match.
        /// </summary>
        private const float TourMeasuredStripDp = 172f;

        /// <summary>The phone's short edge, in dp (873x393).</summary>
        private const float PhoneShortDp = 393f;

        private static readonly List<Bounds> DiningRooms = new List<Bounds>();
        private static int _wallSeq;

        [MenuItem("Lokanta/Render the room layout")]
        public static void Capture()
        {
            try
            {
                string dir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", OutDir));
                Directory.CreateDirectory(dir);
                string stamp = DateTime.Now.ToString("HHmmss");

                Debug.Log("=== Lokanta room layout ===");
                Debug.Log(string.Format("  MEASURED plot {0:0.0} x {1:0.0} m, fixed", PlotW, PlotD));
                Debug.Log("  MEASURED minimum touch target 48 dp (Google), 44 pt (Apple)");
                Debug.Log("  MEASURED scale: a 960 px render -> a 2400 px phone, dp = px * 2.5 / 2.75");
                CheckPlan();

                // DO THE COPY AND THE BUDGET AGREE?
                //
                // This tool cannot build the strip (editor mode) and measures
                // against a hand-written ratio. If that ratio falls SHORT of the
                // tour's strip budget, the tour stays green while this
                // measurement assumes there is more room and reports the touch
                // target as larger than it is.
                float copyDp = (BandTop + BandBottom) * PhoneShortDp;
                Debug.Log(string.Format(
                    "  MEASURED the strip copy is {0:0} dp, the tour measured {1:0} dp",
                    copyDp, TourMeasuredStripDp));
                if (copyDp < TourMeasuredStripDp - 1f)
                    Debug.LogError(string.Format(
                        "PROBLEMS: the strip copy ({0:0} dp) is smaller than what the "
                        + "tour measured ({1:0} dp) - the touch target is being "
                        + "measured LARGER than it is", copyDp, TourMeasuredStripDp));

                for (int tier = 1; tier <= 4; tier++)
                {
                    Build(tier);
                    int tables = TierTables[tier - 1];

                    Vector3 near = Shoot(
                        Path.Combine(dir, string.Format("floor_{0:00}_oneroom_{1}.png", tables, stamp)),
                        Pad(DiningRooms[0], 0.5f), DiningRooms[0], null);

                    // THE OVERVIEW frames THE OPEN ROOMS, not the whole plot -
                    // and that is what the game does too. Counting the closed
                    // wing into the frame was measuring the target SMALLER than
                    // it is.
                    List<string> roomDp = new List<string>();
                    Bounds open = Lokanta.Game.CameraFit.OpenBounds(tables);
                    Vector3 far = Shoot(
                        Path.Combine(dir, string.Format("floor_{0:00}_all_{1}.png", tables, stamp)),
                        open, DiningRooms[0], roomDp);

                    // And once more WITH THE INTERFACE BARS IN PLACE: in the
                    // game the top strip and the action bar take part of the
                    // screen and the camera fits into the band that is left.
                    // This is the target's real floor.
                    List<string> bandDp = new List<string>();
                    Vector3 band = Shoot(
                        Path.Combine(dir, string.Format("floor_{0:00}_strip_{1}.png", tables, stamp)),
                        open, DiningRooms[0], bandDp, BandTop, BandBottom);

                    Debug.Log(string.Format(
                        "  MEASURED {0,2} tables | ROOM: table {1:0} dp, table+chairs {2:0} dp"
                        + " || OVERVIEW: table {3:0} dp, first hall {4:0} dp"
                        + " || WITH STRIPS: first hall {5:0} dp",
                        tables, Dp(near.x), Dp(near.y), Dp(far.x), Dp(far.z), Dp(band.z)));
                    Debug.Log("  MEASURED   the rooms in the overview: "
                              + string.Join(", ", roomDp.ToArray()));

                    // THE FLOOR: the smallest OPEN room's value with the strips
                    // in place. This is the number that meets the 48 dp minimum
                    // or fails it - the worst room, not the average.
                    List<string> narrowDp = new List<string>();
                    Shoot(Path.Combine(dir,
                              string.Format("floor_{0:00}_narrow_{1}.png", tables, stamp)),
                          open, DiningRooms[0], narrowDp, BandTop, BandBottom,
                          NarrowW, NarrowH);

                    string floor209 = Smallest(bandDp, tables);
                    string floor169 = Smallest(narrowDp, tables);
                    Debug.Log("  MEASURED   the rooms with strips: "
                              + string.Join(", ", bandDp.ToArray())
                              + "  -> FLOOR 20:9 " + floor209
                              + " dp, 16:9 " + floor169 + " dp");

                    // THE 48 dp THRESHOLD IS NOW ASSERTED.
                    //
                    // It was written above as a line of TEXT ("minimum touch
                    // target 48 dp") but was compared against NOTHING under any
                    // condition. The finding held in memory - "the real floor was
                    // 48 dp" - could have crept back silently, which is exactly
                    // what this tool is supposed to catch.
                    Threshold(floor209, tables, "20:9");
                    Threshold(floor169, tables, "16:9");
                }

                Debug.Log("=== room layout done ===");
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogError("PROBLEMS: room layout -> " + e.GetType().Name + ": " + e.Message);
                if (Application.isBatchMode) EditorApplication.Exit(2);
            }
        }

        /// <summary>
        /// Does the floor plan cover the plot with no gaps and no overlaps, and
        /// does every room fit the tables it asks for? In hand-written
        /// rectangles one misplaced decimal leaves a gap silently.
        /// </summary>
        private static void CheckPlan()
        {
            float area = 0f;
            for (int i = 0; i < Plan.Length; i++)
            {
                area += Plan[i].W * Plan[i].D;
                for (int j = i + 1; j < Plan.Length; j++)
                {
                    bool overlap =
                        Plan[i].X0 < Plan[j].X1 - 0.001f && Plan[j].X0 < Plan[i].X1 - 0.001f &&
                        Plan[i].Z0 < Plan[j].Z1 - 0.001f && Plan[j].Z0 < Plan[i].Z1 - 0.001f;
                    if (overlap)
                        throw new Exception("the rooms overlap: " + Plan[i].Name + " / " + Plan[j].Name);
                }

                int cols, rows;
                Fit(Plan[i], out cols, out rows);
                if (Plan[i].Tables > cols * rows)
                    throw new Exception(string.Format("{0}: it asks for {1} tables, {2} fit",
                                                      Plan[i].Name, Plan[i].Tables, cols * rows));
            }
            if (Mathf.Abs(area - PlotW * PlotD) > 0.01f)
                throw new Exception(string.Format(
                    "the plot is not covered: the rooms are {0:0.00} m2, the plot {1:0.00} m2", area, PlotW * PlotD));

            Debug.Log(string.Format("  MEASURED the floor plan is consistent: {0} rooms, {1:0.0} m2",
                                    Plan.Length, area));
        }

        /// <summary>
        /// How many columns and rows of tables fit in a room. The epsilon is
        /// essential: 4.6 - 0.9 comes out as 3.6999998 and divided by 1.85
        /// gives 1.99999, so taking the floor gives one column instead of two.
        /// The check caught this.
        /// </summary>
        private static void Fit(Room r, out int cols, out int rows)
        {
            cols = Mathf.Max(1, Mathf.FloorToInt((r.W - Margin) / CellX + 0.002f));
            rows = Mathf.Max(1, Mathf.FloorToInt((r.D - Margin) / CellZ + 0.002f));
        }

        // ---------------------------------------------------------------------
        private static void Build(int tier)
        {
            Clear();
            DiningRooms.Clear();
            _wallSeq = 0;

            List<Room> built = new List<Room>();
            foreach (Room r in Plan) if (r.Tier <= tier) built.Add(r);

            int tableIndex = 0;
            foreach (Room r in Plan)
            {
                if (r.Tier > tier)
                {
                    Slab(r.Name + "Plot", r.X0, r.Z0, r.W, r.D, FloorEmpty);
                    Frame(r.Name + "Frame", r.X0, r.Z0, r.W, r.D);
                    continue;
                }

                Slab(r.Name + "Floor", r.X0, r.Z0, r.W, r.D, r.Floor);
                Edges(r, built);

                if (r.Tables > 0)
                {
                    int cols, rows;
                    Fit(r, out cols, out rows);
                    float sx = r.X0 + (r.W - cols * CellX) * 0.5f;
                    float sz = r.Z0 + (r.D - rows * CellZ) * 0.5f;

                    int made = 0;
                    for (int rr = 0; rr < rows && made < r.Tables; rr++)
                    {
                        // Centre a last row that is not full; a single table
                        // pushed against the edge looks forgotten.
                        int inRow = Mathf.Min(cols, r.Tables - made);
                        float off = (cols - inRow) * CellX * 0.5f;
                        for (int cc = 0; cc < inRow; cc++, made++)
                            Table(tableIndex++, new Vector3(
                                sx + off + CellX * (cc + 0.5f), 0f,
                                sz + CellZ * (rr + 0.5f)));
                    }

                    DiningRooms.Add(new Bounds(r.Center, new Vector3(r.W, 1.60f, r.D)));
                }
                else
                {
                    Props(r);
                }
            }

        }

        /// <summary>
        /// What stands inside the service rooms. The room names are the ones
        /// RoomPlan.cs produces, so they stay as that file spells them.
        /// </summary>
        private static void Props(Room r)
        {
            switch (r.Name)
            {
                case "Kitchen":
                    Box("Stove", new Vector3(r.X0 + 1.30f, 0.43f, r.Z1 - 0.75f),
                        new Vector3(1.80f, 0.86f, 0.72f), Dark);
                    Box("Counter", new Vector3(r.X0 + 3.50f, 0.45f, r.Z1 - 0.75f),
                        new Vector3(2.20f, 0.90f, 0.66f), Metal);
                    Box("ExtractorHood", new Vector3(r.X0 + 1.30f, 1.95f, r.Z1 - 0.75f),
                        new Vector3(1.90f, 0.45f, 0.85f), Metal);
                    // The fridge was TAKEN OUT of the kitchen: cold storage is
                    // the store room's job now and there is a visible upgrade
                    // ladder there. Showing both would be a lie.
                    break;
                case "Sink":
                    Box("Sink", new Vector3(r.X0 + r.W * 0.5f, 0.45f, r.Z1 - 0.70f),
                        new Vector3(2.40f, 0.90f, 0.64f), Metal);
                    Box("DishRack", new Vector3(r.X1 - 0.40f, 0.60f, r.Z0 + r.D * 0.40f),
                        new Vector3(0.50f, 1.20f, 2.20f), Wood);
                    break;
                case "Store":
                    // The cold room is at the back, the dry shelf on the side
                    // wall. The two match the perishable / non-perishable split
                    // of the ingredient list; they are not decoration.
                    Box("ColdRoom", new Vector3(r.X0 + r.W * 0.5f, 1.05f, r.Z1 - 0.65f),
                        new Vector3(r.W - 0.50f, 2.10f, 0.90f), Metal);
                    Box("ColdRoomDoor", new Vector3(r.X0 + r.W * 0.5f, 0.95f, r.Z1 - 1.12f),
                        new Vector3(0.85f, 1.80f, 0.08f), Dark);
                    Box("DryShelf", new Vector3(r.X1 - 0.40f, 0.70f, r.Z0 + 1.15f),
                        new Vector3(0.50f, 1.40f, 1.60f), Wood);
                    Box("Crate1", new Vector3(r.X0 + 0.80f, 0.30f, r.Z0 + 0.70f),
                        new Vector3(0.90f, 0.60f, 0.80f), Wood);
                    break;
                case "Entry":
                    Box("Till", new Vector3(r.X0 + 1.50f, 0.50f, r.Z0 + 1.40f),
                        new Vector3(1.80f, 1.00f, 0.70f), Wood);
                    Box("Door", new Vector3(r.X1 - 1.30f, 1.05f, r.Z0 + 0.08f),
                        new Vector3(1.30f, 2.10f, 0.10f), Wood);
                    break;
            }
        }

        // ---------------------------------------------------------------------
        /// <summary>
        /// Walks the room's four edges and classifies each stretch: a low
        /// partition if its neighbour is built, a full wall if not. No full
        /// wall goes on the front (-Z) and right (+X) edges that face the
        /// camera, or it would close the scene off. So that inner edges are not
        /// drawn twice, partitions are only generated from the +Z and +X edges.
        /// </summary>
        private static void Edges(Room r, List<Room> built)
        {
            EdgeRun(r, built, true, r.Z1, r.X0, r.X1, +1f, true);    // back
            EdgeRun(r, built, true, r.Z0, r.X0, r.X1, -1f, false);   // front
            EdgeRun(r, built, false, r.X0, r.Z0, r.Z1, -1f, true);   // left
            EdgeRun(r, built, false, r.X1, r.Z0, r.Z1, +1f, false);  // right
        }

        private static void EdgeRun(Room r, List<Room> built, bool horizontal,
                                    float fixedC, float a0, float a1, float dir,
                                    bool wallIfOpen)
        {
            const float step = 0.20f;
            int n = Mathf.Max(1, Mathf.RoundToInt((a1 - a0) / step));
            int runStart = 0;
            int runKind = -2;

            for (int i = 0; i <= n; i++)
            {
                int kind = -2;
                if (i < n)
                {
                    float mid = a0 + (i + 0.5f) * (a1 - a0) / n;
                    float px = horizontal ? mid : fixedC + dir * 0.10f;
                    float pz = horizontal ? fixedC + dir * 0.10f : mid;
                    bool neighbour = InBuilt(built, px, pz);
                    bool outside = px < 0f || px > PlotW || pz < 0f || pz > PlotD;
                    // 0 = partition, 1 = full wall, 2 = temporary wall, -1 = nothing
                    if (neighbour) kind = 0;
                    else if (outside) kind = wallIfOpen ? 1 : -1;
                    else kind = 2;      // inside the plot but not built yet
                    if (kind == 0 && dir < 0f) kind = -1;   // an inner edge once only
                }

                if (kind != runKind)
                {
                    if (runKind >= 0)
                        EdgeSegment(horizontal, fixedC,
                                    a0 + runStart * (a1 - a0) / n,
                                    a0 + i * (a1 - a0) / n,
                                    runKind, r.Name);
                    runStart = i;
                    runKind = kind;
                }
            }
        }

        /// <summary>
        /// kind: 0 an inner partition, 1 a full wall on the plot boundary, 2 a
        /// temporary wall.
        ///
        /// A temporary wall is an edge facing a room that is inside the plot
        /// but not built yet. A full wall was tried and it hid the expansion
        /// area completely; the player could not see where they would grow.
        /// Low and in a different colour, it says "this will open up".
        /// </summary>
        private static void EdgeSegment(bool horizontal, float fixedC,
                                        float b0, float b1, int kind, string owner)
        {
            float len = b1 - b0;
            if (len < 0.05f) return;
            float h = kind == 1 ? WallH : PartH;
            Color c = kind == 1 ? Wall : (kind == 2 ? Temp : Part);

            // A doorway in the middle of every long partition; without one the
            // rooms stay shut.
            if (kind == 0 && len > DoorW + 0.8f)
            {
                float half = (len - DoorW) * 0.5f;
                EdgeBox(horizontal, fixedC, b0, b0 + half, h, c, owner);
                EdgeBox(horizontal, fixedC, b1 - half, b1, h, c, owner);
                return;
            }
            EdgeBox(horizontal, fixedC, b0, b1, h, c, owner);
        }

        private static void EdgeBox(bool horizontal, float fixedC,
                                    float b0, float b1, float h, Color c, string owner)
        {
            float len = b1 - b0;
            if (len < 0.05f) return;
            Vector3 pos = horizontal
                ? new Vector3((b0 + b1) * 0.5f, h * 0.5f, fixedC)
                : new Vector3(fixedC, h * 0.5f, (b0 + b1) * 0.5f);
            Vector3 size = horizontal
                ? new Vector3(len, h, WallT)
                : new Vector3(WallT, h, len);
            Box("Wall" + (_wallSeq++) + "_" + owner, pos, size, c);
        }

        private static bool InBuilt(List<Room> built, float x, float z)
        {
            foreach (Room r in built)
                if (x > r.X0 && x < r.X1 && z > r.Z0 && z < r.Z1) return true;
            return false;
        }

        // ---------------------------------------------------------------------
        private static void Slab(string name, float x0, float z0, float w, float d, Color c)
        {
            // A two-centimetre gap: a thin line is left between adjoining
            // floors, so a room's boundary reads regardless of the colour
            // difference.
            Box(name, new Vector3(x0 + w * 0.5f, -0.05f, z0 + d * 0.5f),
                new Vector3(w - 0.02f, 0.10f, d - 0.02f), c);
        }

        private static void Frame(string name, float x0, float z0, float w, float d)
        {
            const float t = 0.10f;
            Box(name + "A", new Vector3(x0 + w * 0.5f, 0.01f, z0 + t * 0.5f),
                new Vector3(w, 0.06f, t), Outline);
            Box(name + "B", new Vector3(x0 + w * 0.5f, 0.01f, z0 + d - t * 0.5f),
                new Vector3(w, 0.06f, t), Outline);
            Box(name + "C", new Vector3(x0 + t * 0.5f, 0.01f, z0 + d * 0.5f),
                new Vector3(t, 0.06f, d), Outline);
            Box(name + "D", new Vector3(x0 + w - t * 0.5f, 0.01f, z0 + d * 0.5f),
                new Vector3(t, 0.06f, d), Outline);
        }

        private static void Table(int i, Vector3 at)
        {
            // These names belong to this validation scene alone; the probe
            // below looks up "Table0".
            Box("Table" + i, at + new Vector3(0f, 0.74f, 0f),
                new Vector3(0.86f, 0.06f, 0.86f), Wood);
            Box("TableLeg" + i, at + new Vector3(0f, 0.36f, 0f),
                new Vector3(0.12f, 0.72f, 0.12f), Wood);
            Chair("Chair" + i + "a", at + new Vector3(0f, 0f, 0.62f), 1f);
            Chair("Chair" + i + "b", at + new Vector3(0f, 0f, -0.62f), -1f);
        }

        private static void Chair(string name, Vector3 at, float away)
        {
            Box(name, at + new Vector3(0f, 0.44f, 0f),
                new Vector3(0.42f, 0.06f, 0.42f), Seat);
            Box(name + "Leg", at + new Vector3(0f, 0.21f, 0f),
                new Vector3(0.10f, 0.42f, 0.10f), Dark);
            Box(name + "Back", at + new Vector3(0f, 0.68f, away * 0.18f),
                new Vector3(0.42f, 0.46f, 0.06f), Seat);
        }

        private static Bounds Pad(Bounds b, float m)
        {
            Bounds r = b;
            r.Expand(new Vector3(m, 0f, m));
            return r;
        }

        private static float Dp(float renderPx) { return renderPx * 2.5f / 2.75f; }

        /// <summary>
        /// The touch target thresholds. The number was already being worked
        /// out; the only thing missing was tying it to an ASSERTION - 48 dp was
        /// written to the log as a line of TEXT and compared against nothing
        /// under any condition.
        ///
        /// TWO THRESHOLDS, BECAUSE 48 CANNOT BE RED HERE.
        ///
        /// Google's floor is 48 dp (Apple's 44 pt), and when the project turned
        /// the camera 10 degrees it MEASURED the drop to 45 dp at 20:9 and
        /// accepted it (docs/41): "what falls below the floor is not a button
        /// but the SHORT edge on screen of a five-metre room; its long edge is
        /// more than twice that and the player can come closer with two
        /// fingers." Making 48 red would mean reporting a decision already
        /// taken as an error on every run.
        ///
        ///   - below 48 dp: a WARNING (the industry floor, a known price)
        ///   - below 40 dp: RED (a clear regression from the accepted 45; at
        ///     that point "you can zoom in and touch it" no longer holds either)
        /// </summary>
        /// <summary>
        /// The hard floor, and it is a RATCHET rather than a round number.
        ///
        /// 40, AND THE FIRST ATTEMPT AT A RATCHET PUT IT AT 41 AND WAS WRONG.
        ///
        /// 41 is the four-table number. This runs at four tiers and the floor
        /// falls as the restaurant grows, so at fourteen tables it is 40 -
        /// and a ratchet set from one tier's reading fails the run on a tier
        /// it never looked at. The right value is the worst one across every
        /// tier, and that is 40, which is where docs/41 put it.
        ///
        /// docs/41 set the red line at 40 when the measured value was 42-43,
        /// and argued the gap to Google's 48 as an accepted price: what falls
        /// below it is not a button but the SHORT edge of a room whose long
        /// edge is twice that, with two-finger zoom available. That argument
        /// is unchanged. What changed is the room left under it - the street
        /// widening took 43 to 41, so three dp of margin became one, and a
        /// red line a single dp below the accepted price is a red line that
        /// would let the last one go quietly.
        ///
        /// Between 41 and 48 this still only WARNS, deliberately: docs/41 is
        /// right that reporting an accepted price as an error on every run
        /// buries a real regression in the noise.
        /// </summary>
        private const int FloorDp = 40;

        private static void Threshold(string dpText, int tables, string aspect)
        {
            if (!int.TryParse(dpText, out int dp)) return;
            if (dp < FloorDp)
            {
                Debug.LogError("PROBLEMS: touch target " + dp + " dp (< " + FloorDp
                               + ", the value this game already stands at), "
                               + tables + " tables, " + aspect);
                return;
            }
            if (dp < 48)
                Debug.LogWarning("Touch target " + dp + " dp against an industry floor of 48 - "
                                 + tables + " tables, " + aspect
                                 + " (docs/31: measured, known, and open)");
        }

        /// <summary>
        /// The smallest dp value among the open rooms. A closed room does not
        /// count - the player cannot touch it, because it is no longer drawn.
        /// </summary>
        private static string Smallest(List<string> dp, int tables)
        {
            int best = int.MaxValue;
            for (int i = 0; i < Plan.Length && i < dp.Count; i++)
            {
                Lokanta.Game.RoomPlan.Room r = Lokanta.Game.RoomPlan.Rooms[i];
                if (!Lokanta.Game.RoomPlan.RoomOpen(in r, tables)) continue;

                string[] parts = dp[i].Split(' ');
                if (parts.Length < 2) continue;
                if (int.TryParse(parts[parts.Length - 1], out int v) && v < best) best = v;
            }
            return best == int.MaxValue ? "?" : best.ToString();
        }

        // ---------------------------------------------------------------------
        private static void Clear()
        {
            foreach (GameObject go in UnityEngine.Object.FindObjectsByType<GameObject>(
                         FindObjectsSortMode.None))
            {
                if (go.hideFlags == HideFlags.None && go.scene.IsValid())
                    UnityEngine.Object.DestroyImmediate(go);
            }
        }

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        private static GameObject Box(string name, Vector3 pos, Vector3 size, Color c)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.position = pos;
            go.transform.localScale = size;

            Shader sh = Shader.Find("Universal Render Pipeline/Unlit");
            if (sh == null) sh = Shader.Find("Universal Render Pipeline/Lit");
            Material m = new Material(sh);
            if (m.HasProperty(BaseColorId)) m.SetColor(BaseColorId, c);
            if (m.HasProperty(ColorId)) m.SetColor(ColorId, c);
            go.GetComponent<Renderer>().sharedMaterial = m;
            return go;
        }

        /// <summary>
        /// Frames the target, renders it and returns the measurements.
        /// x = the table's size on screen, y = the table plus its chairs,
        /// z = the room's size. If roomDp is given, every room's dp value is
        /// written down as well.
        /// </summary>
        private static Vector3 Shoot(string path, Bounds target, Bounds roomBounds,
                                     List<string> roomDp,
                                     float top01 = 0f, float bottom01 = 0f,
                                     int w = ShotW, int h = ShotH)
        {
            GameObject camGo = new GameObject("Camera");
            Camera cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.93f, 0.94f, 0.91f);
            // THE ANGLES AND THE DISTANCE COME FROM THE GAME ITSELF.
            //
            // `32f`, `Euler(34, -12, 0)` and a closed-form distance formula
            // were once WRITTEN HERE. That is exactly why CameraFit exists: two
            // copies of the same arithmetic drift apart silently - and they
            // did. The game was finding the smallest distance by binary search
            // while the formula here erred on the safe side in every term and
            // gave 30% too much distance. So THE MEASUREMENT was reporting a
            // SMALLER target than the game shows; the numbers were on the safe
            // side but what was measured was not the game.
            cam.fieldOfView = Lokanta.Game.CameraFit.FieldOfView;
            cam.aspect = w / (float)h;

            camGo.transform.rotation = Lokanta.Game.CameraFit.Rotation;
            camGo.transform.position =
                Lokanta.Game.CameraFit.Position(target, cam.aspect, top01, bottom01);

            RenderTexture rt = new RenderTexture(w, h, 24) { antiAliasing = 4 };
            cam.targetTexture = rt;
            cam.Render();

            RenderTexture prev = RenderTexture.active;
            RenderTexture.active = rt;
            Texture2D shot = new Texture2D(w, h, TextureFormat.RGB24, false);
            shot.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            shot.Apply();
            RenderTexture.active = prev;
            File.WriteAllBytes(path, shot.EncodeToPNG());

            float px = 0f, pxSet = 0f;
            GameObject probe = GameObject.Find("Table0");
            if (probe != null)
            {
                px = Smaller(cam, probe.transform.position, 0.43f, 0.43f);
                pxSet = Smaller(cam, probe.transform.position, 0.93f, 0.93f);
            }
            float pxRoom = Smaller(cam, roomBounds.center,
                                   roomBounds.extents.x, roomBounds.extents.z);

            if (roomDp != null)
                foreach (Room r in Plan)
                    roomDp.Add(string.Format("{0} {1:0}", r.Name,
                        Dp(Smaller(cam, r.Center, r.W * 0.5f, r.D * 0.5f))));

            cam.targetTexture = null;
            UnityEngine.Object.DestroyImmediate(shot);
            rt.Release();
            UnityEngine.Object.DestroyImmediate(rt);
            UnityEngine.Object.DestroyImmediate(camGo);
            return new Vector3(px, pxSet, pxRoom);
        }

        /// <summary>
        /// The SMALLER of the target's two axes on screen. At a 34 degree view
        /// the depth direction is foreshortened to 0.56; measuring only the
        /// horizontal makes the target look larger than it is.
        /// </summary>
        private static float Smaller(Camera cam, Vector3 c, float halfX, float halfZ)
        {
            Vector3 a = cam.WorldToScreenPoint(c + new Vector3(-halfX, 0f, 0f));
            Vector3 b = cam.WorldToScreenPoint(c + new Vector3(halfX, 0f, 0f));
            float wide = Mathf.Abs(b.x - a.x);

            Vector3 p = cam.WorldToScreenPoint(c + new Vector3(0f, 0f, -halfZ));
            Vector3 q = cam.WorldToScreenPoint(c + new Vector3(0f, 0f, halfZ));
            float deep = Mathf.Abs(q.y - p.y);

            return Mathf.Min(wide, deep);
        }
    }
}
