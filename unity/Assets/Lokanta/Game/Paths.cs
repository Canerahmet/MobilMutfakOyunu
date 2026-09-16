using System.Collections.Generic;
using UnityEngine;

namespace Lokanta.Game
{
    /// <summary>
    /// The hall's WALKING GEOMETRY: where the door is, where the corridor
    /// runs, from which point a table is approached.
    ///
    /// RoomPlan says WHERE the floor plan is; this says HOW IT IS WALKED
    /// THROUGH. They are separate because the floor plan is tested and is
    /// the source of the touch-target measurement - adding new numbers
    /// there for the walking would mix a measured file up with unmeasured
    /// decisions.
    ///
    /// NO PATHFINDING, BUT A CORRIDOR. Implementing an A* is unnecessary
    /// for this scene: the floor plan is fixed, the rooms are rectangles
    /// and everybody goes back and forth between the same two things. A
    /// two-part path - out to the front corridor first, then up along the
    /// target's line - both reaches every table and, when watched, reads
    /// as "it came in through the door and went to its table". There is no
    /// collision either: the figures may pass through one another, and
    /// that is better than their jamming and freezing.
    /// </summary>
    public static class Paths
    {
        /// <summary>
        /// The front corridor's z. The plot's front edge is z=0; the
        /// corridor is just above it and runs past the front of every room.
        ///
        /// The 0.55 was chosen BY MEASUREMENT: the entrance's plant pots and
        /// the wash room's counter stood in this lane and both were moved
        /// back (RestaurantView.BuildRoomProps). The first hall's front row
        /// of tables is at z=1.35 and, with the chair radius, comes down to
        /// z=0.9 - so 0.55 passes in front of them too.
        /// </summary>
        public const float LaneZ = 0.55f;

        /// <summary>The door's x: the middle of the entrance room.</summary>
        public static float DoorX
        {
            get
            {
                RoomPlan.Room g = Room("Entry");
                return g.CenterX;
            }
        }

        /// <summary>
        /// The OUTSIDE of the door. The guest appears here, and leaves to
        /// here.
        ///
        /// -0.85: the camera framing is the box of the open rooms and the
        /// plot's front edge is z=0. Anything further out has the figure
        /// appear entirely outside the frame - it read as "it ended at the
        /// bottom edge" rather than "it came in through the door". At this
        /// distance the upper half of the body is visible and the walk
        /// inwards is watched from the start.
        /// </summary>
        public static Vector3 Outside
        {
            get { return new Vector3(DoorX, 0f, -0.55f); }
        }

        /// <summary>
        /// The MIDDLE of the pavement.
        ///
        /// The -0.57 is a correction: it used to be -1.05 while the pavement
        /// slab ran from z = -0.62 to -0.02 - so the pedestrians and the
        /// arriving guests walked ON THE TARMAC, 0.35 m beyond the kerb. The
        /// pavement now runs from -1.12 to -0.02 and this number is its
        /// middle.
        ///
        /// The pavement was widened enough to take TWO pedestrian lanes and
        /// the price was measured: the depth of street in the frame went
        /// 1.10 -> 1.94 m (CameraFit.StreetInFrame) and the touch-target
        /// measurement (the BASE line of Editor/RoomLayout) was run again -
        /// 59 dp, above Google's 48 dp minimum.
        /// </summary>
        public const float PavementZ = -0.72f;

        /// <summary>
        /// HALF the distance between the two pedestrian lanes.
        ///
        /// 0.35 - so the lanes are 0.70 m apart. Why there are lanes at all:
        /// two figures walking in opposite directions on the same line went
        /// through each other. Separate lanes solve the meeting
        /// STRUCTURALLY; for those who catch each other up in the same lane
        /// there is a push as well (StreetLife.LateUpdate).
        ///
        /// The 0.70 was chosen BY MEASUREMENT: Editor/PlacementAudit prints
        /// the figure's horizontal profile against height, and apart from
        /// the arms the widest band is at HEAD height - 0.67 m (in this
        /// pack's proportions the head is a third of the body, that is,
        /// wider than the shoulders). Had the shoulder measurement (0.58)
        /// been used, two heads would still have overlapped by 7 cm.
        ///
        /// THE ARMS ARE NOT IN THIS NUMBER, and that is deliberate: the arms
        /// hang away from the body and at hand height (y 0.20-0.31) the
        /// width rises to 1.08 m - for a figure a metre tall. Two lanes 1.08
        /// m apart would make the pavement half as deep as the restaurant.
        /// Two bodies passing separately is enough; one hand passing through
        /// another's is a few pixels at this camera distance.
        /// </summary>
        public const float LaneHalf = 0.35f;

        /// <summary>
        /// The pedestrian lane for a direction. dir > 0 going right, dir < 0
        /// going left.
        ///
        /// The one going right is on the KERB side, the one going left on the
        /// building side - traffic separates that way on a real pavement too,
        /// and what matters is not which lane is which but that they are
        /// SEPARATE.
        /// </summary>
        public static float PavementLane(int dir)
        {
            return PavementZ + (dir > 0 ? -LaneHalf : LaneHalf);
        }

        /// <summary>
        /// The point on the street where THE GUEST APPEARS.
        ///
        /// The guest no longer appears in front of the door: they appear on
        /// the street, a few metres away from it, and walk in along the
        /// pavement. Appearing in front of the door read as "it materialised"
        /// rather than "it arrived" - the whole point of the walking-in
        /// animation is being able to watch the arrival.
        ///
        /// The side alternates IN TURN: if everyone came from the same side
        /// the street would look like a one-way band.
        /// </summary>
        public static Vector3 Street(int index)
        {
            float side = (index % 2 == 0) ? -1f : 1f;
            float distance = 2.6f + (index % 3) * 1.3f;
            // One that appears to the LEFT of the door is going to walk to
            // the right: its lane goes with that. Otherwise the arriving
            // guest walks in the opposite lane and comes face to face with
            // every pedestrian.
            int dir = side < 0f ? 1 : -1;
            return new Vector3(DoorX + side * distance, 0f, PavementLane(dir));
        }

        /// <summary>The INSIDE of the door: just behind the threshold.</summary>
        public static Vector3 Inside
        {
            get { return new Vector3(DoorX, 0f, LaneZ); }
        }

        // =====================================================================
        /// <summary>
        /// A path from the door to a point. The threshold first, then
        /// sideways along the corridor, then up along the target's line.
        /// </summary>
        public static void FromDoor(List<Vector3> into, Vector3 target)
        {
            into.Clear();
            into.Add(Outside);
            into.Add(Inside);
            Lane(into, Inside, target);
            into.Add(target);
        }

        /// <summary>
        /// FROM THE STREET to a table. Walk along the pavement, come to the
        /// front of the door, go in.
        public static void FromStreet(List<Vector3> into, Vector3 target)
        {
            into.Clear();
            into.Add(new Vector3(DoorX, 0f, PavementZ));
            into.Add(Outside);
            into.Add(Inside);
            Lane(into, Inside, target);
            into.Add(target);
        }

        /// <summary>From inside to the street: out through the door, away along the pavement.</summary>
        public static void ToStreet(List<Vector3> into, Vector3 from, int index)
        {
            into.Clear();
            Lane(into, from, Inside);
            into.Add(Inside);
            into.Add(Outside);
            into.Add(new Vector3(DoorX, 0f, PavementZ));
            into.Add(Street(index));
        }

        /// <summary>From a point to the door, until it is outside.</summary>
        public static void ToDoor(List<Vector3> into, Vector3 from)
        {
            into.Clear();
            Lane(into, from, Inside);
            into.Add(Inside);
            into.Add(Outside);
        }

        /// <summary>A path between two points that uses the corridor.</summary>
        public static void Between(List<Vector3> into, Vector3 from, Vector3 target)
        {
            into.Clear();
            Lane(into, from, target);
            into.Add(target);
        }

        /// <summary>
        /// The INTERMEDIATE points that join two points through the
        /// corridor.
        ///
        /// If they are on the same line (if the difference in x is small)
        /// there is no need to come down to the corridor: going straight
        /// up or down is both shorter and more natural. Otherwise: down to
        /// the corridor, sideways, then up to the target's line.
        /// </summary>
        private static void Lane(List<Vector3> into, Vector3 from, Vector3 target)
        {
            // A BACK ROOM IS ENTERED THROUGH ITS DOOR.
            //
            // It used to go straight up along the target's line, and once the
            // walls were added that meant the figure walking through a wall.
            // The back room's door is worked out from the SAME place as the
            // gap RestaurantView opens (BackDoorX) - had it been written in
            // two places, the door would be in one place and the way through
            // in another.
            int targetRoom = RoomAt(target);
            int sourceRoom = RoomAt(from);
            if (targetRoom == sourceRoom) return;

            Vector3 exit;
            if (BackDoor(sourceRoom, out exit))
            {
                into.Add(new Vector3(exit.x, 0f, exit.z + 0.5f));
                into.Add(new Vector3(exit.x, 0f, exit.z - 0.5f));
                from = new Vector3(exit.x, 0f, exit.z - 0.5f);
            }

            Vector3 entry;
            if (BackDoor(targetRoom, out entry))
            {
                if (Mathf.Abs(from.x - entry.x) > 0.35f)
                {
                    into.Add(new Vector3(from.x, 0f, LaneZ));
                    into.Add(new Vector3(entry.x, 0f, LaneZ));
                }
                into.Add(new Vector3(entry.x, 0f, entry.z - 0.5f));
                into.Add(new Vector3(entry.x, 0f, entry.z + 0.5f));
                return;
            }

            if (Mathf.Abs(from.x - target.x) < 0.35f) return;

            into.Add(new Vector3(from.x, 0f, LaneZ));
            into.Add(new Vector3(target.x, 0f, LaneZ));
        }

        /// <summary>The room this point is in. -1: outside the plot.</summary>
        public static int RoomAt(Vector3 p)
        {
            for (int i = 0; i < RoomPlan.Rooms.Length; i++)
            {
                RoomPlan.Room r = RoomPlan.Rooms[i];
                if (p.x >= r.X0 - 0.01f && p.x <= r.X0 + r.W + 0.01f
                    && p.z >= r.Z0 - 0.01f && p.z <= r.Z0 + r.D + 0.01f) return i;
            }
            return -1;
        }

        /// <summary>
        /// THE BACK ROOM'S DOOR. Rooms in the front row have no door (the
        /// corridor already runs inside them) and it returns false.
        ///
        /// The floor plan has two rows: a front and a back room in every
        /// column. A back room is entered through the MIDDLE of that room's
        /// front edge. The gap in the wall opens at exactly that point
        /// (RestaurantView.Line).
        /// </summary>
        public static bool BackDoor(int room, out Vector3 door)
        {
            door = Vector3.zero;
            if (room < 0 || room >= RoomPlan.Rooms.Length) return false;

            RoomPlan.Room r = RoomPlan.Rooms[room];
            if (r.Z0 < 0.01f) return false;             // front row

            door = new Vector3(r.CenterX, 0f, r.Z0);
            return true;
        }

        // =====================================================================
        /// <summary>
        /// The point at which a table is APPROACHED: in front of the table,
        /// outside the seats. The waiter comes here; it does not step onto
        /// the table.
        /// </summary>
        public static Vector3 BesideTable(Vector3 table)
        {
            return new Vector3(table.x, 0f, table.z - 0.95f);
        }

        /// <summary>The angle needed to face the given point.</summary>
        public static float FaceFrom(Vector3 at, Vector3 target)
        {
            Vector3 d = target - at;
            d.y = 0f;
            if (d.sqrMagnitude < 0.0001f) return 0f;
            return Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
        }

        // =====================================================================
        /// <summary>
        /// The WORKING POSTS in the kitchen: in front of the row of stoves.
        ///
        /// The station index is not tied to one piece of furniture - there
        /// are six stations in the kitchen and three stoves. The station
        /// walks over the posts: the cook does not go to the same place for
        /// every job but to a different counter according to the job's
        /// station, and looking from the hall there is MOVEMENT in the
        /// kitchen - which is the thing being told.
        /// </summary>
        public static Vector3 KitchenPost(int station, int count)
        {
            RoomPlan.Room m = Room("Kitchen");
            if (count < 1) count = 1;
            int i = station < 0 ? 0 : station % count;
            float t = (i + 0.5f) / count;
            return new Vector3(m.X0 + 0.75f + (m.W - 2.0f) * t, 0f,
                               m.Z0 + m.D - 1.35f);
        }

        /// <summary>
        /// IN FRONT OF THE PREP COUNTER: where the cook washes and chops.
        ///
        /// The counters are in the kitchen's FRONT row
        /// (RestaurantView.BuildRoomProps, LineUp back:false -> z = Z0 +
        /// 0.55). The cook stands IN FRONT of them, that is, further inside,
        /// and turns towards the counter.
        /// </summary>
        public static Vector3 PrepPost(int index, int count)
        {
            RoomPlan.Room m = Room("Kitchen");
            if (count < 1) count = 1;
            float t = (index % count + 0.5f) / count;
            return new Vector3(m.X0 + 0.75f + (m.W - 2.0f) * t, 0f, m.Z0 + 1.25f);
        }

        /// <summary>The prep counter itself: the direction the cook faces.</summary>
        public static Vector3 PrepCounter(int index, int count)
        {
            Vector3 p = PrepPost(index, count);
            return new Vector3(p.x, 0f, p.z - 0.7f);
        }

        /// <summary>In front of the fridge: where the cook fetches ingredients.</summary>
        public static Vector3 Fridge
        {
            get
            {
                RoomPlan.Room m = Room("Kitchen");
                return new Vector3(m.X0 + m.W - 1.35f, 0f, m.Z0 + m.D - 0.85f);
            }
        }

        /// <summary>
        /// THE FRIDGE ITSELF: the point the cook looks at.
        ///
        /// The cupboard is on the right wall (RestaurantView.BuildRoomProps,
        /// X0 + W - 0.55) and faces into the room. The cook has to stand in
        /// front of it and turn RIGHT; +Z was written at first and the cook
        /// played "pick up" while standing side-on to the cupboard.
        /// </summary>
        public static Vector3 FridgeFace
        {
            get
            {
                RoomPlan.Room m = Room("Kitchen");
                return new Vector3(m.X0 + m.W - 0.55f, 0f, m.Z0 + m.D - 0.7f);
            }
        }

        /// <summary>Where the cook waits when idle: behind the counter.</summary>
        public static Vector3 CookHome(int index, int count)
        {
            RoomPlan.Room m = Room("Kitchen");
            if (count < 1) count = 1;
            float t = (index % count + 0.5f) / count;
            return new Vector3(m.X0 + 0.9f + (m.W - 2.2f) * t, 0f, m.CenterZ - 0.3f);
        }

        /// <summary>Where the waiter waits when idle: beside the till at the entrance.</summary>
        public static Vector3 HallHome(int index, int count)
        {
            RoomPlan.Room g = Room("Entry");
            if (count < 1) count = 1;
            float t = (index % count + 0.5f) / count;
            // -2.05: the till counter is at z = Z0 + D - 0.8 and is 0.6 deep,
            // so it starts at 2.9. The figure's depth is 1.12 m; standing at
            // 2.4 the placement audit measured a 0.14 m overlap - the waiter
            // was waiting inside the counter.
            return new Vector3(g.X0 + 0.8f + (g.W - 1.6f) * t, 0f, g.Z0 + g.D - 2.05f);
        }

        /// <summary>Where a party waiting for a table stands: beside the door.</summary>
        public static Vector3 QueueSpot(int index)
        {
            return new Vector3(DoorX + 0.85f + (index % 4) * 0.55f, 0f, LaneZ + 0.15f);
        }

        // =====================================================================
        private static RoomPlan.Room Room(string name)
        {
            for (int i = 0; i < RoomPlan.Rooms.Length; i++)
                if (RoomPlan.Rooms[i].Name == name) return RoomPlan.Rooms[i];
            return RoomPlan.Rooms[0];
        }
    }
}
