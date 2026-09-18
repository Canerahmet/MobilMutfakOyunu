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
        /// The entrance's plant pots and the wash room's counter once stood in
        /// this lane and both were moved back
        /// (RestaurantView.BuildRoomProps).
        ///
        /// 0.55 -> 0.30, AND THE OLD NUMBER WAS MEASURED AGAINST THE WRONG
        /// EDGE. The note here used to say the front row "comes down to z=0.9
        /// with the chair radius, so 0.55 passes in front of them too". The
        /// chair radius is not the edge that matters: a chair CENTRE sits
        /// 0.58 m from the table and the chair is about 0.40 m deep, so the
        /// set's outer edge is at 1.35 - 0.78 = 0.57 - and a figure is 0.44 m
        /// across. At 0.55 the walker's near side was at 0.77, a fifth of a
        /// metre INSIDE the front chairs, which is what the tour finally said
        /// out loud once it was made to name its offender: "at (10,0, 0,6) is
        /// inside table 0 at (10,0, 1,4)".
        ///
        /// THE AISLE IS 0.57 m WIDE AND THE FIGURE IS 0.44. That leaves 13 cm
        /// of slack in total, so the lane is not "chosen" any more - it is
        /// CENTRED in the only gap it has: the walker's centre may lie between
        /// 0.22 (clear of the front wall at z=0) and 0.35 (clear of the
        /// chairs), and 0.30 is the middle of that.
        ///
        /// Worth knowing rather than hiding: a hall with this table density
        /// has no room for a second aisle anywhere. The side lanes
        /// (Paths.SideLane) exist because the perimeter is the only clear
        /// ground there is.
        /// </summary>
        public const float LaneZ = 0.30f;

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
        ///
        /// -0.72 -> -1.32, AND THE REASON IS THE TERRACE.
        ///
        /// The street was laid out in the wrong physical order. From the
        /// building outwards it ran: the near pedestrian lane (-0.37), the
        /// terrace rail (-0.42), the planters (out to -0.65), the cafe
        /// tables (-0.95), the far lane (-1.07), the kerb. So the NEAR LANE
        /// WAS INSIDE THE TERRACE - 0.37 m from a wall, with a rail 5 cm
        /// beyond it - and a pedestrian's torso (half-width 0.29 at rail
        /// height) went through that rail along the whole length of the
        /// building, every time anybody walked left. The zoomed frame
        /// (render/zoom/before-pedestrians-in-the-terrace.png) shows the rest of it: passers-by
        /// standing inside the cafe tables.
        ///
        /// The order is now the one a real pavement has:
        ///
        ///   wall 0 | terrace seating -0.19 | rail + planters -0.42 (out to
        ///   -0.65) | lane -0.97 | lane -1.67 | kerb -2.02 | tarmac -2.34
        ///
        /// Each gap is the figure's own half-width: the near lane clears the
        /// planters by 0.29 m and nothing continuous stands on either lane.
        ///
        /// THE PRICE, MEASURED NOT GUESSED: CameraFit.StreetInFrame goes
        /// 1.94 -> 2.66, and every metre at the front makes the restaurant
        /// smaller on screen. Editor/RoomLayout's BASE line was run again -
        /// see the number recorded there - against Google's 48 dp minimum.
        /// </summary>
        public const float PavementZ = -1.32f;

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
            if (targetRoom == sourceRoom) { InRoom(into, from, target, targetRoom); return; }

            // LEAVING A DINING ROOM IS A WALK ACROSS IT TOO.
            //
            // The arrival was fixed first: a waiter coming from the kitchen
            // used to go straight up the target's own column. The departure
            // has the same shape and was found the same way - the tour named
            // it, "at (9,5, 2,1) inside table 0 at (10,0, 1,4), from
            // (9,5, 3,1) heading for (9,4, -0,4)": a guest getting up from the
            // BACK row and walking straight down the column to the street,
            // through the front table on the way.
            //
            // So the figure leaves by the side lane first, which is the same
            // route InRoom gives any walk inside the room. A front-row table
            // is under the 1.2 m InRoom ignores, so it still walks straight
            // out - and it only passes its own table doing it.
            Vector3 exit;
            bool sourceBack = BackDoor(sourceRoom, out exit);
            if (!sourceBack && sourceRoom >= 0 && sourceRoom != targetRoom
                && RoomPlan.Rooms[sourceRoom].IsDining)
            {
                int wasCount = into.Count;
                InRoom(into, from, new Vector3(from.x, 0f, LaneZ), sourceRoom);
                if (into.Count > wasCount) from = into[into.Count - 1];
            }

            if (sourceBack)
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

            if (Mathf.Abs(from.x - target.x) >= 0.35f)
                into.Add(new Vector3(from.x, 0f, LaneZ));

            // COMING IN FROM ANOTHER ROOM IS STILL A WALK ACROSS THIS ONE.
            //
            // InRoom was added for the case where both ends are in the SAME
            // room and the tour went green at 0.00 m - then came back red at
            // 0.51 m on a later run with nothing about walking changed. It was
            // called flaky for a while, and it was not: the two cases are not
            // the same path.
            //
            // A waiter coming from the kitchen reached the front corridor at
            // THE TARGET'S OWN X and then went straight up that column,
            // through every table set standing between the corridor and the
            // table. The tour's own words once it was made to name the
            // offender: "character-male-c at (10,5, 1,4) is inside table 0 at
            // (10,0, 1,4), heading for (10,5, 3,1)" - the back table of the
            // column, reached through the front one. Which tables are in the
            // way depends on which table the party was seated at, so it came
            // and went with the seating.
            //
            // The approach is now the same one InRoom uses for a walk inside
            // the room: in at the SIDE lane nearest the target, up the lane to
            // the target's row, across. Measured against the real grid: the
            // lanes sit 0.28 m off the wall and the nearest table centre is
            // 1.10-1.30 m away, over the 1.00 m a figure needs.
            //
            // The corridor point is written out rather than read back off
            // `into`: the first version used the last waypoint added, which
            // after a back-room exit is a point in the OTHER room, and the
            // approach was then computed from the wrong end of the building.
            Vector3 mouth = new Vector3(target.x, 0f, LaneZ);
            int before = into.Count;
            InRoom(into, mouth, target, targetRoom);
            if (into.Count > before) return;

            // Not a dining room: there is nothing in the middle of the kitchen
            // or the sink room to walk through, so it is approached head-on.
            if (Mathf.Abs(from.x - target.x) < 0.35f) return;
            into.Add(new Vector3(target.x, 0f, LaneZ));
        }

        /// <summary>The room this point is in. -1: outside the plot.</summary>
        /// <summary>
        /// A path WITHIN one room, around the table block instead of through
        /// it.
        ///
        /// THE BUG THIS FIXES. `Lane` used to return immediately when the two
        /// ends were in the same room, so a waiter crossing a dining room or a
        /// guest walking to a back table went in a straight line - through
        /// every table and chair on the way.
        ///
        /// THERE IS NO GAP TO WALK BETWEEN TABLES, and that is measured, not
        /// assumed. A chair sits `SeatRadius` 0.58 m from the table's centre
        /// and is about 0.40 m deep, so a table set is roughly 1.56 m across.
        /// The grid cell is 1.85 x 1.70, which leaves 0.29 m between sets in x
        /// and 0.14 m in z - and a figure is about 0.45 m wide. Routing
        /// "between the tables" is not available at this table density.
        ///
        /// WHAT IS AVAILABLE IS THE PERIMETER. `RoomPlan.TableSpots` centres
        /// the grid in the room, so a border is left on all four sides: in the
        /// first hall that is 0.65 m at the left and right and 0.50 m front
        /// and back. Every table in a 2x2 or 3-table room touches that border,
        /// so every table can be reached from it.
        ///
        /// So the route is: out to the nearest side lane, along it to the
        /// target's own row, and in. Three waypoints and no pathfinding -
        /// which is the rule this layer was built on ("no pathfinding, there
        /// is a lane"), applied inside the room as well as between rooms.
        /// </summary>
        private static void InRoom(List<Vector3> into, Vector3 from, Vector3 target,
                                   int room)
        {
            if (room < 0) return;
            RoomPlan.Room r = RoomPlan.Rooms[room];
            if (!r.IsDining) return;

            // Already alongside: a step of this size does not cross anything.
            if ((from - target).sqrMagnitude < 1.2f * 1.2f) return;

            // The side lanes, half the border in from each wall.
            float leftLane = r.X0 + SideLane;
            float rightLane = r.X0 + r.W - SideLane;

            // Leave by the side the figure is already nearer, and come in on
            // the side the TARGET is nearer: crossing the room along a wall
            // is what a person does, and it keeps the walk out of the middle.
            float outLane = from.x - r.X0 < r.W * 0.5f ? leftLane : rightLane;
            float inLane = target.x - r.X0 < r.W * 0.5f ? leftLane : rightLane;

            into.Add(new Vector3(outLane, 0f, from.z));

            // WHEN THE TWO ENDS ARE ON OPPOSITE SIDES, GO ROUND THE FRONT.
            //
            // This used to cross at `from.z` - out to the near lane, then
            // STRAIGHT ACROSS THE WHOLE ROOM still at the starting row, then
            // along the far lane. That middle leg is exactly the "through
            // every table" walk this routine was written to stop: a waiter
            // moving from table 2 to table 3 in Hall1 passed 0.75 m from two
            // table centres, a quarter of a metre inside both sets.
            //
            // The front margin is clear ground in every hall: the grid is
            // centred, so the nearest row is 1.07-1.47 m from Z0 + SideLane.
            if (Mathf.Abs(outLane - inLane) > 0.01f)
            {
                float frontLane = r.Z0 + SideLane;
                into.Add(new Vector3(outLane, 0f, frontLane));
                into.Add(new Vector3(inLane, 0f, frontLane));
            }
            into.Add(new Vector3(inLane, 0f, target.z));
        }

        /// <summary>
        /// How far into the room the side lane runs.
        ///
        /// The border left by the centred grid is 0.50-0.65 m depending on the
        /// room, so half of 0.55 keeps the lane inside the narrowest of them
        /// and still clear of the wall.
        /// </summary>
        private const float SideLane = 0.28f;

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

            // WHERE THE GAP IS, ASKED OF THE SAME FUNCTION THE WALL USES.
            //
            // This used to return the middle of the room and
            // RestaurantView.Shared independently put the gap at the middle of
            // the shared edge. They agreed only by coincidence, and the
            // comment at the top of Lane already warned what happens when the
            // door is in one place and the way through in another. The
            // neighbour is looked up here so both sides can call DoorAlong.
            float x = r.CenterX;
            for (int i = 0; i < RoomPlan.Rooms.Length; i++)
            {
                RoomPlan.Room n = RoomPlan.Rooms[i];
                if (Mathf.Abs(n.Z0 + n.D - r.Z0) > 0.01f) continue;
                float x0 = Mathf.Max(n.X0, r.X0);
                float x1 = Mathf.Min(n.X0 + n.W, r.X0 + r.W);
                if (x1 - x0 < 1.4f) continue;
                x = DoorAlong(in r, in n, x0, x1);
                break;
            }

            door = new Vector3(x, 0f, r.Z0);
            return true;
        }

        /// <summary>
        /// Where the gap sits along an edge two rooms share.
        ///
        /// THE MIDDLE, EXCEPT BETWEEN THE KITCHEN AND THE WASH ROOM. The wash
        /// room's back wall is a solid run of four units - clean counter,
        /// sink, sink, dirty counter - and the middle of that wall is a sink.
        /// The user watched a cook come through it: "when going from the
        /// kitchen to the wash room it opens the door behind the sink and goes
        /// through the sink".
        ///
        /// So the gap takes the LEFT END of the wall, where the user asked for
        /// it, and BuildDishStation starts its run clear of it. The two
        /// numbers are written next to each other for that reason.
        /// </summary>
        public static float DoorAlong(in RoomPlan.Room a, in RoomPlan.Room b,
                                      float from, float to)
        {
            bool kitchenWash = (a.Name == "Kitchen" && b.Name == "Sink")
                               || (a.Name == "Sink" && b.Name == "Kitchen");
            if (kitchenWash) return from + BackDoorInset;
            return (from + to) * 0.5f;
        }

        /// <summary>The kitchen gap's centre, measured in from the left end of the wall.</summary>
        public const float BackDoorInset = 0.72f;

        /// <summary>
        /// The first x right of that gap that furniture may use: the far jamb
        /// plus a finger's width. RestaurantView.DoorWidth is 1.10.
        /// </summary>
        public const float BackDoorClear = BackDoorInset + 0.55f + 0.06f;

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

        /// <summary>
        /// Where the cook waits when idle: the middle of the room.
        ///
        /// THE BAND NARROWED WHEN THE WALLS FILLED UP. It used to run from
        /// x = 0.9 to 4.1, which was the whole floor when the kitchen was
        /// three stoves against the back wall. Both side walls now carry
        /// equipment (docs/59) and a figure is 1.14 m across the arms, so an
        /// idle cook at 1.4 stood 0.22 m inside the stone oven - the placement
        /// audit said so by name.
        ///
        /// 1.70 to 3.90 clears the left wall's run (which reaches x = 1.05)
        /// and the right wall's (which starts at 4.25) with a figure's half
        /// width to spare at both ends.
        /// </summary>
        public static Vector3 CookHome(int index, int count)
        {
            RoomPlan.Room m = Room("Kitchen");
            if (count < 1) count = 1;
            if (index < 0) index = 0;

            // TWO ROWS, because three abreast does not fit. The clear band is
            // 3.0 m and a figure is 1.14 m across the arms, so three of them
            // side by side want 3.42 - the audit caught the middle pair 0.04 m
            // inside each other, which is arms, but an audit that is allowed
            // to be a little bit red stops being read.
            int row = index % 2;
            int col = index / 2;
            int cols = Mathf.Max(1, (count + 1) / 2);
            float t = (col + 0.5f) / cols;

            return new Vector3(m.X0 + 1.15f + 3.00f * t, 0f,
                               m.CenterZ - 0.3f - row * 0.80f);
        }

        /// <summary>
        /// Where the waiter waits when idle: beside the till at the entrance.
        ///
        /// TWO COLUMNS, NOT ONE ROW - and the room swap is why. This used to
        /// spread them along the room's width, which was fine while the
        /// entrance was the 5.2 m room: three waiters got 1.2 m each and a
        /// figure is 0.9 m across. The entrance is now the 3.2 m room
        /// (RoomPlan, 18 September), the usable width dropped to 1.6 m, and
        /// the placement audit caught it on the first run afterwards - two
        /// pairs of staff overlapping by 0.41 m.
        ///
        /// The new room is narrow and DEEP (3.2 x 5.4), so the queue turns
        /// through ninety degrees: two columns 1.5 m apart, stepping back in
        /// rows. The band it steps through is bounded at both ends by things
        /// that are already there - the front corridor (Paths.LaneZ) at one
        /// end and the till counter at the other, which sits at z = Z0 + D -
        /// 0.8 and is 0.6 deep.
        /// </summary>
        public static Vector3 HallHome(int index, int count)
        {
            RoomPlan.Room g = Room("Entry");
            if (count < 1) count = 1;
            if (index < 0) index = 0;

            int col = index % 2;
            int row = index / 2;

            float x = g.X0 + 0.85f + col * (g.W - 1.70f);

            float zFrom = g.Z0 + 1.30f;              // clear of the corridor
            float zTo = g.Z0 + g.D - 1.55f;          // clear of the till counter
            int rows = Mathf.Max(1, (count + 1) / 2);
            float step = rows > 1
                ? Mathf.Min(1.25f, (zTo - zFrom) / (rows - 1))
                : 0f;

            return new Vector3(x, 0f, zFrom + step * row);
        }

        /// <summary>
        /// Where a party waiting for a table stands: behind the door.
        ///
        /// IT RUNS BACK INTO THE ROOM, NOT SIDEWAYS ALONG THE FRONT. Four
        /// parties at 0.55 m need 1.65 m of frontage and the entrance is now
        /// the 3.2 m room (RoomPlan, 18 September), with the door in the
        /// middle - so a sideways queue put the last two parties past the wall
        /// and into the first hall.
        ///
        /// Receding from the door is the better picture anyway: a queue that
        /// grows towards the camera reads as a queue, where one that grows
        /// sideways reads as a row of people standing about.
        /// </summary>
        public static Vector3 QueueSpot(int index)
        {
            RoomPlan.Room g = Room("Entry");
            float x = Mathf.Min(DoorX + 0.85f, g.X0 + g.W - 0.55f);
            return new Vector3(x, 0f, LaneZ + 0.30f + (index % 4) * 0.60f);
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
