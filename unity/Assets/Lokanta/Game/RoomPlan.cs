using System.Collections.Generic;

namespace Lokanta.Game
{
    /// <summary>
    /// Restoranin kat plani. ARSA SABIT, odalar farkli olcude.
    ///
    /// Bu tablo bir zamanlar yalnizca editor betiginde (Editor/RoomLayout.cs)
    /// duruyordu, yani calisma zamani plani BILMIYORDU. Ayni sayilari iki
    /// yere yazmak bu projede dort kez sessizce ayristi; o yuzden plan
    /// buraya, calisma zamanina tasindi ve editor araci da buradan okuyor.
    ///
    /// Kararin gerekcesi docs/31-rooms-and-camera.md ve olcum:
    ///   - Arsa 18,0 x 9,6 m ve HIC BUYUMUYOR; bina onun icinde
    ///     buyuyor. Kamera yalnizca ACIK odalari cerceveliyor
    ///     (CameraFit.OpenBounds) ve acilmamis oda cizilmiyor - bos
    ///     levhalar ekranin %41'ini yiyordu.
    ///   - Dokunma hedefi yine de kademeden bagimsiz: olcum en kucuk
    ///     acik odayi her kademede 71 dp veriyor, cunku cerceveyi
    ///     bagliyan sey EN degil DERINLIK ve mutfak blogu arsanin
    ///     butun derinligini zaten kapliyor.
    ///   - Odalarin olculeri farkli ve ayrim cizgileri hizali degil.
    ///     Esit 2x2 izgara sayilari tutturuyordu ama render yapay duruyordu.
    ///   - Kademe 0 odalari basta acik; 1-4 arasi genislemeyle aciliyor.
    /// </summary>
    public static class RoomPlan
    {
        public const float PlotW = 18.00f;
        public const float PlotD = 9.60f;

        /// <summary>Masa takimi araligi, en ve derinlik.</summary>
        public const float CellX = 1.85f;
        public const float CellZ = 1.70f;
        /// <summary>Odanin masasiz kenar payi.</summary>
        public const float Margin = 0.90f;

        public struct Room
        {
            public string Name;
            public float X0, Z0, W, D;
            /// <summary>0 basta acik; 1-4 genisleme kademesi.</summary>
            public int Tier;
            /// <summary>Bu odaya kac masa takimi giriyor. 0 ise servis odasi.</summary>
            public int Tables;

            public float CenterX { get { return X0 + W * 0.5f; } }
            public float CenterZ { get { return Z0 + D * 0.5f; } }
            public bool IsDining { get { return Tables > 0; } }
        }

        public static readonly Room[] Rooms =
        {
            new Room { Name = "Mutfak",  X0 =  0.0f, Z0 = 4.0f, W = 5.2f, D = 5.6f, Tier = 0 },
            new Room { Name = "Giris",   X0 =  0.0f, Z0 = 0.0f, W = 5.2f, D = 4.0f, Tier = 0 },
            new Room { Name = "Bulasik", X0 =  5.2f, Z0 = 0.0f, W = 3.2f, D = 5.4f, Tier = 0 },
            new Room { Name = "Depo",    X0 =  5.2f, Z0 = 5.4f, W = 3.2f, D = 4.2f, Tier = 0 },
            new Room { Name = "Salon1",  X0 =  8.4f, Z0 = 0.0f, W = 5.0f, D = 4.4f, Tier = 1, Tables = 4 },
            new Room { Name = "Salon2",  X0 =  8.4f, Z0 = 4.4f, W = 5.0f, D = 5.2f, Tier = 2, Tables = 3 },
            new Room { Name = "Salon3",  X0 = 13.4f, Z0 = 0.0f, W = 4.6f, D = 5.0f, Tier = 3, Tables = 3 },
            new Room { Name = "Salon4",  X0 = 13.4f, Z0 = 5.0f, W = 4.6f, D = 4.6f, Tier = 4, Tables = 4 },
        };

        /// <summary>
        /// Bir odaya sigan masa izgarasi. Epsilon SART: 4,6 - 0,9 kayan
        /// noktada 3,6999998 cikiyor ve epsilonsuz bir sutun kayboluyor.
        /// </summary>
        public static void Fit(in Room r, out int cols, out int rows)
        {
            cols = (int)((r.W - Margin) / CellX + 0.002f);
            rows = (int)((r.D - Margin) / CellZ + 0.002f);
            if (cols < 1) cols = 1;
            if (rows < 1) rows = 1;
        }

        /// <summary>Belirli bir masa sayisina kadar acik odalarin masa noktalari.</summary>
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

        /// <summary>Ilk salon odasinin sirasi. Birinci gunden acik.</summary>
        public static int FirstDiningRoom()
        {
            for (int i = 0; i < Rooms.Length; i++)
                if (Rooms[i].IsDining) return i;
            return 0;
        }

        /// <summary>Bu masa sayisinda hangi odalar acik.</summary>
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
