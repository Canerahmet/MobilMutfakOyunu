using System.Collections.Generic;

namespace Lokanta.Game
{
    /// <summary>
    /// The restaurant's floor plan. THE PLOT IS FIXED, the rooms differ
    /// in size.
    ///
    /// This table once lived only in the editor script
    /// (Editor/RoomLayout.cs), which means the runtime DID NOT KNOW the
    /// plan. Writing the same numbers in two places has drifted apart
    /// silently four times in this project; so the plan moved here, into
    /// the runtime, and the editor tool reads it from here too.
    ///
    /// The reasoning is in docs/31-rooms-and-camera.md, and in the
    /// measurement:
    ///   - The plot is 18.0 x 9.6 m and NEVER GROWS; the building grows
    ///     inside it. The camera frames only the OPEN rooms
    ///     (CameraFit.OpenBounds) and a room that has not been opened is
    ///     not drawn - empty slabs were eating 41% of the screen.
    ///   - The touch target is independent of the tier all the same: the
    ///     measurement gives the smallest open room 71 dp at every tier,
    ///     because what binds the framing is not the WIDTH but the DEPTH,
    ///     and the kitchen block already covers the plot's whole depth.
    ///   - The rooms differ in size and the dividing lines are not
    ///     aligned. An even 2x2 grid hit the same numbers, but the render
    ///     looked artificial.
    ///   - Tier 0 rooms are open from the start; 1-4 open with expansion.
    /// </summary>
    public static class RoomPlan
    {
        public const float PlotW = 18.00f;
        public const float PlotD = 9.60f;

        /// <summary>Table-set spacing, width and depth.</summary>
        public const float CellX = 1.85f;
        public const float CellZ = 1.70f;
        /// <summary>The room's table-free edge margin.</summary>
        public const float Margin = 0.90f;

        public struct Room
        {
            public string Name;
            public float X0, Z0, W, D;
            /// <summary>0 open from the start; 1-4 the expansion tier.</summary>
            public int Tier;
            /// <summary>How many table sets fit in this room. 0 means a service room.</summary>
            public int Tables;

            public float CenterX { get { return X0 + W * 0.5f; } }
            public float CenterZ { get { return Z0 + D * 0.5f; } }
            public bool IsDining { get { return Tables > 0; } }
        }

        public static readonly Room[] Rooms =
        {
            new Room { Name = "Kitchen",  X0 =  0.0f, Z0 = 4.0f, W = 5.2f, D = 5.6f, Tier = 0 },
            new Room { Name = "Entry",   X0 =  0.0f, Z0 = 0.0f, W = 5.2f, D = 4.0f, Tier = 0 },
            new Room { Name = "Sink", X0 =  5.2f, Z0 = 0.0f, W = 3.2f, D = 5.4f, Tier = 0 },
            new Room { Name = "Store",    X0 =  5.2f, Z0 = 5.4f, W = 3.2f, D = 4.2f, Tier = 0 },
            new Room { Name = "Hall1",  X0 =  8.4f, Z0 = 0.0f, W = 5.0f, D = 4.4f, Tier = 1, Tables = 4 },
            new Room { Name = "Hall2",  X0 =  8.4f, Z0 = 4.4f, W = 5.0f, D = 5.2f, Tier = 2, Tables = 3 },
            new Room { Name = "Hall3",  X0 = 13.4f, Z0 = 0.0f, W = 4.6f, D = 5.0f, Tier = 3, Tables = 3 },
            new Room { Name = "Hall4",  X0 = 13.4f, Z0 = 5.0f, W = 4.6f, D = 4.6f, Tier = 4, Tables = 4 },
        };

        /// <summary>
        /// A room's table grid. The epsilon is ESSENTIAL: 4.6 - 0.9 comes
        /// out as 3.6999998 in floating point and without the epsilon a
        /// column disappears.
        /// </summary>
        public static void Fit(in Room r, out int cols, out int rows)
        {
            cols = (int)((r.W - Margin) / CellX + 0.002f);
            rows = (int)((r.D - Margin) / CellZ + 0.002f);
            if (cols < 1) cols = 1;
            if (rows < 1) rows = 1;
        }

        /// <summary>The table spots of the rooms open up to a given table count.</summary>
        public static List<TableSpot> TableSpots(int tableCount)
        {
            List<TableSpot> spots = new List<TableSpot>();
            for (int i = 0; i < Rooms.Length; i++)
            {
                Room r = Rooms[i];
                if (!r.IsDining) continue;
                if (spots.Count >= tableCount) break;

                Fit(in r, out int cols, out int rows);
                int want = r.Tables;
                int placed = 0;

                float sx = r.X0 + (r.W - cols * CellX) * 0.5f;
                float sz = r.Z0 + (r.D - rows * CellZ) * 0.5f;

                for (int rr = 0; rr < rows && placed < want; rr++)
                {
                    int inRow = cols;
                    if (want - placed < cols) inRow = want - placed;
                    float off = (cols - inRow) * CellX * 0.5f;

                    for (int cc = 0; cc < inRow; cc++)
                    {
                        if (spots.Count >= tableCount) break;
                        spots.Add(new TableSpot
                        {
                            Room = i,
                            X = sx + off + CellX * (cc + 0.5f),
                            Z = sz + CellZ * (rr + 0.5f),
                        });
                        placed++;
                    }
                }
            }
            return spots;
        }

        public struct TableSpot
        {
            public int Room;
            public float X, Z;
        }

        /// <summary>The index of the first hall room. Open from day one.</summary>
        public static int FirstDiningRoom()
        {
            for (int i = 0; i < Rooms.Length; i++)
                if (Rooms[i].IsDining) return i;
            return 0;
        }

        /// <summary>Which rooms are open at this table count.</summary>
        public static bool RoomOpen(in Room r, int tableCount)
        {
            if (!r.IsDining) return true;
            int seen = 0;
            for (int i = 0; i < Rooms.Length; i++)
            {
                if (!Rooms[i].IsDining) continue;
                if (Rooms[i].Name == r.Name) return seen < tableCount;
                seen += Rooms[i].Tables;
            }
            return false;
        }
    }
}
