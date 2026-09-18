using UnityEngine;

namespace Lokanta.Game
{
    /// <summary>
    /// Places the camera so that it FRAMES A BOX EXACTLY.
    ///
    /// The reason this class exists was measured, and found by looking.
    /// The touch-target measurement (Editor/RoomLayout.cs) FITS the camera
    /// to the plot: a 32 degree field of view, 34 degrees of pitch, -12
    /// degrees of yaw, and a distance that fills the box exactly. The game
    /// camera, though, had been written with a fixed height of 15.5 m and
    /// 42 degrees.
    ///
    /// The result: in the first render the restaurant covered only 38% of
    /// the frame. So a room measured at 81 dp in the general view dropped
    /// to ~32 dp in the game - far below Google's 48 dp minimum.
    ///
    /// The fix is not CHANGING THE NUMBER but SHARING THE RULE: the camera
    /// now uses the same fitting calculation as the measurement. The same
    /// thing written in two places drifts apart silently - it has happened
    /// five times in this project.
    /// </summary>
    public static class CameraFit
    {
        /// <summary>
        /// THE FIELD OF VIEW, THE PITCH AND THE YAW - every one of them
        /// chosen by measurement.
        ///
        /// The first values were a 32 degree field of view, 34 of pitch and
        /// -12 of YAW. That -12 degree turn had been put in for the "2.5D"
        /// look and its price had never been measured. It was measured when
        /// the user looked at a screenshot and said "the empty rooms should
        /// not take up space, the restaurant should fill the screen":
        ///
        ///   setting       frame fill (opening/grown)      BASE touch target
        ///   32, -12       27% / 33%                       52 -> 48 dp
        ///   32,  -6       34% / 39%                       -
        ///   22,   0       48% / 43%                       71 -> 71 dp
        ///
        /// BASE = the short edge on screen of the smallest OPEN room, with
        /// the interface bars in place (Editor/RoomLayout.cs). Google's
        /// minimum is 48 dp: the old setting fell to EXACTLY 48 at the
        /// largest tier, so it had no margin at all. With the new setting it
        /// is 71 dp and it DOES NOT CHANGE with the tier.
        ///
        /// Zeroing the yaw is what really opens the frame up (27% -> 41%);
        /// bringing the field of view down from 32 to 22 flattens the
        /// perspective and wins the rest. The pitch stayed at 34 - the sense
        /// of depth comes from there, and raising it lowers the fill (18% at
        /// 42, 17% at 50).
        ///
        /// These numbers are the shared source for the game and for the
        /// MEASUREMENT. Anyone changing one should first run
        /// RoomLayout.Capture and look at the BASE line.
        /// </summary>
        public const float FieldOfView = 22f;

        /// <summary>
        /// The pitch. It STAYED at 34 - and 28 was tried.
        ///
        /// At the early tiers what binds the framing is the DEPTH (the open
        /// rooms are 13.4 x 9.6, that is, nearly square); lowering the pitch
        /// was expected to squash the depth further and so make the building
        /// bigger. It was measured: at 28 degrees the base FELL from 45 dp to
        /// 42. The projection is three-dimensional; the box's eight corners
        /// do not fit into a single line of trigonometry.
        /// </summary>
        public const float Pitch = 34f;
        /// <summary>
        /// THE YAW. It came back at the request for the reference look - but
        /// BY MEASUREMENT.
        ///
        /// Zero had been chosen in docs/31 to fill the frame and to enlarge
        /// the touch target (a -12 degree yaw dropped the base from 71 dp to
        /// 48 dp). When the user brought reference images and said "it would
        /// be good if the camera angle were like the reference too", it was
        /// looked at again: in every frame of the reference the building is
        /// seen THREE-QUARTER on, that is, two of its faces at once.
        ///
        /// As the yaw grows the rooms turn into RHOMBUSES on screen and their
        /// short edge shortens; the measurement comes from the BASE line of
        /// RoomLayout.Capture. The value was chosen by looking at that line.
        /// </summary>
        /// <summary>
        /// THE YAW. It came back at the request for the reference look - and
        /// ITS PRICE WAS MEASURED.
        ///
        /// Zero had been chosen deliberately in docs/31: a -12 degree yaw
        /// dropped the touch-target base from 71 dp to 48. When the user
        /// brought reference images and said "wouldn't it be better if the
        /// camera angle were like the reference as well, we are a long way
        /// from the reference point", it was looked at again - in all four
        /// frames of the reference the building stands THREE-QUARTER on, both
        /// of its faces visible.
        ///
        /// Measured with RoomLayout.Capture (BASE = the short edge, with the
        /// bars in place, of the smallest OPEN room, on a 20:9 phone):
        ///
        ///     yaw      BASE 20:9     BASE 16:9
        ///       0        53 dp         66 dp
        ///      10        45 dp         57 dp
        ///      18        41 dp         51 dp
        ///      30        39 dp         49 dp
        ///
        /// AND IT WAS LOOKED AT ON THE PHONE TOO. Twenty degrees looked
        /// wonderful in the screenshot tool (1280x560), but in the real
        /// build, at 873x393 and with the bars in place, the building GOT
        /// SMALLER: a turned rectangle wants more room on screen, and the
        /// fitting pulls the camera back for it. So the yaw on its own does
        /// not bring us CLOSER to the reference, it takes us further away.
        ///
        /// Ten degrees was chosen: the three-quarter feel comes through and
        /// the base stays at 45 dp. What falls below the base is not a BUTTON
        /// but the SHORT edge on screen of a five-metre room; its long edge
        /// is more than twice that, and the player can pinch in.
        ///
        /// THE FLOOR PLAN IS NOT TO BLAME - THAT CLAIM DIED BY MEASUREMENT.
        ///
        /// I had written before that "the reference's building is square,
        /// ours is a long strip; if the plan gets deeper both the yaw and the
        /// size come back". When a frame without the interface was taken
        /// (873x393) it turned out the building ALREADY fills the screen: the
        /// width is at the limit, with a little margin in depth. The 34
        /// degree pitch squashes the depth by sin(34)=0.56, so the 18 x 9.6 m
        /// plot stands at 3.3:1 on screen and the band between the bars is
        /// 3.7:1. If the plot were SQUARE the building would get smaller, not
        /// bigger.
        ///
        /// The real bottleneck is the INTERFACE: the bars were taking 156 of
        /// the 393 dp (40%). The buttons came down from 62 to 54 dp, the
        /// capsule from 42 to 38.
        ///
        /// The real answer is elsewhere: the reference's building is SQUARE,
        /// ours is a long 18 x 9.6 m strip. If the floor plan gets deeper, a
        /// room grows on screen at the same yaw. It is next in line as item 5
        /// of docs/41.
        /// </summary>
        public const float Yaw = 10f;

        /// <summary>The edge margin. The measurement leaves 2%.</summary>
        public const float Margin = 1.02f;

        public static Quaternion Rotation
        {
            get { return Quaternion.Euler(Pitch, Yaw, 0f); }
        }

        /// <summary>The framing that uses the whole screen.</summary>
        public static Vector3 Position(Bounds target, float aspect)
        {
            return Position(target, aspect, 0f, 0f);
        }

        /// <summary>
        /// The same calculation, but framing INSIDE THE SAFE BAND.
        ///
        /// top01 and bottom01 are the fraction of the screen (0..1) that the
        /// interface covers at the top and at the bottom. The game screen has
        /// a top bar and an action bar; fitting the box to the whole screen
        /// left part of the restaurant under the bars and made an empty band
        /// at the top.
        ///
        /// Two things are done: the vertical fit goes by the height of the
        /// band alone, and the camera is shifted so that the middle of the
        /// band falls on the middle of the box.
        /// </summary>
        public static Vector3 Position(Bounds target, float aspect,
                                       float top01, float bottom01)
        {
            // The distance is found by SEARCH-AND-VERIFY, not by
            // CALCULATE-AND-HOPE.
            //
            // There used to be a closed formula: the widest extent over the
            // tangent of the field of view, plus half the box's depth, plus a
            // margin. Every term of that formula erred on the safe side and the
            // errors added up - the restaurant covered only 875 pixels in a
            // 1280-wide frame, so the room touch target was needlessly 30% too
            // small.
            //
            // The binary search REALLY projects the box's eight corners and
            // finds the SMALLEST distance that fits in the frame. No
            // approximation, no accumulated margin.
            float lo = 0.5f, hi = 400f;
            for (int i = 0; i < 28; i++)
            {
                float mid = (lo + hi) * 0.5f;
                if (Fits(target, mid, aspect, top01, bottom01)) hi = mid;
                else lo = mid;
            }
            return CameraAt(target, hi * Margin, top01, bottom01);
        }

        /// <summary>
        /// The camera position at a given distance. The centre of the box is
        /// shifted so that it falls in the middle of the band BETWEEN the
        /// interface bars.
        /// </summary>
        private static Vector3 CameraAt(Bounds target, float dist,
                                        float top01, float bottom01)
        {
            float tanV = Mathf.Tan(FieldOfView * Mathf.Deg2Rad * 0.5f);

            // The middle of the band, normalised against the middle of the screen.
            float centre = bottom01 - top01;
            float shift = centre * dist * tanV;

            return target.center
                   - Rotation * Vector3.forward * dist
                   - Rotation * Vector3.up * shift;
        }

        /// <summary>
        /// Do all eight corners of the box fit inside the band at this
        /// distance?
        /// </summary>
        private static bool Fits(Bounds target, float dist, float aspect,
                                 float top01, float bottom01)
        {
            Vector3 cam = CameraAt(target, dist, top01, bottom01);
            Quaternion inv = Quaternion.Inverse(Rotation);

            float tanV = Mathf.Tan(FieldOfView * Mathf.Deg2Rad * 0.5f);
            float tanH = tanV * aspect;

            // Normalised vertical limits: -1 the bottom edge, +1 the top edge.
            float yMin = -1f + 2f * bottom01;
            float yMax = 1f - 2f * top01;

            Vector3 e = target.extents;
            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = target.center + new Vector3(
                    (i & 1) == 0 ? -e.x : e.x,
                    (i & 2) == 0 ? -e.y : e.y,
                    (i & 4) == 0 ? -e.z : e.z);

                Vector3 v = inv * (corner - cam);
                if (v.z <= 0.05f) return false;          // behind the camera

                float x = v.x / (v.z * tanH);
                float y = v.y / (v.z * tanV);
                if (x < -1f || x > 1f) return false;
                if (y < yMin || y > yMax) return false;
            }
            return true;
        }

        /// <summary>
        /// The whole plot. Height 2.6 m: the walls have to come into the
        /// frame too, otherwise the back wall spills over the top edge.
        /// </summary>
        public static Bounds PlotBounds()
        {
            // Height 1.2 m and an edge margin of 0.4 m.
            //
            // The box's HEIGHT is what binds: the fitting projects eight
            // corners and those corners are at the EDGES of the plot, where
            // there is nothing at that height at all. It said 2.6 m at first
            // ("the walls have to come in as well" - there is no wall there),
            // then 2.0 m. Every extra metre pulls the camera back and makes
            // the restaurant smaller; on a 873x393 dp phone that means the
            // thing the player has to look at falling to a third of the
            // screen.
            //
            // At 1.2 m the tallest object (the fridge, 1.80 m) spills out of
            // the box, but that object stands in the MIDDLE of the plot, not
            // at its edge - so it does not leave the frame.
            return new Bounds(
                new Vector3(RoomPlan.PlotW * 0.5f, 0.5f, RoomPlan.PlotD * 0.5f),
                new Vector3(RoomPlan.PlotW + 0.4f, 1.2f, RoomPlan.PlotD + 0.4f));
        }

        /// <summary>
        /// The bounding box of the OPEN rooms. Closed wings do not come into
        /// the frame - and RestaurantView does not draw them anyway.
        ///
        /// The whole plot used to be framed at all times; the reason was that
        /// the touch target should stay CONSTANT between tiers (docs/31). The
        /// reason collapsed when the user looked at a screenshot: at the first
        /// tier only 102.6 of the plot's 172.8 m2 were open, so 41% of the
        /// screen was an empty slab "that is not yours yet".
        ///
        /// It may also be that the camera DOES NOT come closer when the box
        /// is narrow: on a landscape screen what binds is usually not the
        /// WIDTH but the DEPTH (the kitchen block covers the plot's whole
        /// depth). It was measured: at the current angles the distance does
        /// not change between tiers at all - so the target really is
        /// constant, but no longer thanks to an empty slab.
        ///
        /// The box itself is still needed: framing a room that is not drawn
        /// would mean the measurement (RoomLayout) measuring something
        /// DIFFERENT from the game.
        ///
        /// The height and the edge margin are for the same reasons as in
        /// PlotBounds.
        /// </summary>
        /// <summary>
        /// The depth of street that comes into the frame (m).
        ///
        /// 1.10 -> 1.94. The reason was measured: the pavement needed TWO
        /// pedestrian lanes (figures walking in opposite directions passed
        /// through one another) and the spacing between the lanes comes from
        /// the figure's WIDEST body band - 0.67 m, at head height (the
        /// PROFILE lines of Editor/PlacementAudit). A 0.70 m lane spacing +
        /// half a body on each side = a 1.40 m pavement; a kerb (0.12) and a
        /// visible strip of tarmac (0.42) on top of that = 1.94.
        ///
        /// At 1.10 the pavement fitted into a single lane, and widening only
        /// the pavement pushed the road OUT of the frame: the street turned
        /// into a strip of pavement with no tarmac.
        ///
        /// THE PRICE WAS MEASURED, NOT JUDGED BY EYE: the BASE line of
        /// Editor/RoomLayout (the short edge on screen of the smallest open
        /// room, with the interface bars in place). Google's minimum is 48
        /// dp.
        ///
        /// Along the way it turned out that the 71 dp figure in docs/31 is
        /// STALE: that measurement was taken when there was NO street at
        /// all. The street itself had already brought it down to ~64 and
        /// nobody had measured again. The value now is 59 dp at 20:9 and 74
        /// dp at 16:9.
        ///
        /// Anyone changing this number should first run RoomLayout.Capture
        /// and look at the BASE line - the framing depends on the depth, and
        /// every metre added at the front makes the restaurant smaller on
        /// screen.
        ///
        /// 1.94 -> 2.66, AND THIS TIME IT WAS THE TERRACE.
        ///
        /// The pavement was one lane too narrow to hold a terrace as well,
        /// so the near lane was inside the rail and the passers-by walked
        /// through it. The full arithmetic is in Paths.PavementZ. What
        /// arrives here is the consequence: the outer lane moved from -1.07
        /// to -1.67 and the kerb and the tarmac moved with it.
        ///
        /// THE PRICE WAS MEASURED AT EVERY TIER, same build, one constant
        /// changed, because the floor falls as the restaurant grows and a
        /// reading from one tier is not the answer:
        ///
        ///     tables        4     7    10    14
        ///     1.94 m       43    43    42    42   dp, 20:9
        ///     2.66 m       41    41    40    40
        ///
        /// Two dp, flat. 2.50 was tried as well and measures the same 41/40,
        /// so the extra 0.16 m is spent on the tarmac strip rather than
        /// saved: at 0.16 m of road in frame the street stops reading as a
        /// street.
        ///
        /// AND THE NUMBER WRITTEN ABOVE - 59 dp - WAS STALE, by the same
        /// mechanism the paragraph above it describes happening to docs/31's
        /// 71. The floor at 1.94 m is 43, not 59, and always was.
        ///
        /// That is a bookkeeping failure and not a design one: docs/41
        /// re-measured this line on 13 September, got 42-43, and argued the
        /// price - what falls under 48 is not a button but the short edge of
        /// a room whose long edge is twice it, with two-finger zoom
        /// available. THIS comment simply had not been told.
        ///
        /// What it does cost is MARGIN, and all of it. RoomLayout goes red
        /// below 40; the worst tier was 42 and is now 40. So this change
        /// spends the whole gap between the accepted price and the red line.
        ///
        /// IT IS STILL THE RIGHT TRADE, and the case is worth writing down
        /// because it is one constant to reverse. What the two dp buy is a
        /// defect that is in EVERY frame, along the whole front of the
        /// building, permanently: passers-by walking through the terrace
        /// railing and standing inside the cafe tables. What they cost is
        /// two dp on a number whose subject is a ROOM - long edge twice the
        /// short one, two-finger zoom available - which docs/41 argued as an
        /// accepted price at 42-43 and which is met at 40. Anybody who
        /// disagrees changes this one number back and re-runs RoomLayout.
        /// </summary>
        public const float StreetInFrame = 2.66f;

        public static Bounds OpenBounds(int tableCount)
        {
            float x1 = float.MaxValue, z1 = float.MaxValue;
            float x2 = float.MinValue, z2 = float.MinValue;

            for (int i = 0; i < RoomPlan.Rooms.Length; i++)
            {
                RoomPlan.Room r = RoomPlan.Rooms[i];
                if (!RoomPlan.RoomOpen(in r, tableCount)) continue;

                if (r.X0 < x1) x1 = r.X0;
                if (r.Z0 < z1) z1 = r.Z0;
                if (r.X0 + r.W > x2) x2 = r.X0 + r.W;
                if (r.Z0 + r.D > z2) z2 = r.Z0 + r.D;
            }

            if (x1 > x2 || z1 > z2) return PlotBounds();

            // THE STREET COMES INTO THE FRAME TOO.
            //
            // The guest now walks in from the pavement; arriving from
            // somewhere invisible would be the same as never walking at all.
            //
            // The 1.1 m was chosen BY MEASUREMENT, not by eye: the framing
            // depends on the depth and every metre added at the front makes
            // the restaurant smaller on screen. The touch-target measurement
            // (docs/31) is already close to Google's 48 dp minimum; at 1.1 m
            // the smallest open room still stays above the threshold. More
            // than that widens the street but shrinks the restaurant.
            z1 -= StreetInFrame;

            return new Bounds(
                new Vector3((x1 + x2) * 0.5f, 0.5f, (z1 + z2) * 0.5f),
                new Vector3(x2 - x1 + 0.4f, 1.2f, z2 - z1 + 0.4f));
        }

        /// <summary>A single room, with a little margin.</summary>
        public static Bounds RoomBounds(int room)
        {
            RoomPlan.Room r = RoomPlan.Rooms[room];
            return new Bounds(
                new Vector3(r.CenterX, 0.55f, r.CenterZ),
                new Vector3(r.W + 1.0f, 1.8f, r.D + 1.0f));
        }
    }
}
