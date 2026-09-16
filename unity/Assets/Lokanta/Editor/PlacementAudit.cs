using System.Collections.Generic;
using Lokanta.Game;
using UnityEditor;
using UnityEngine;

namespace Lokanta.EditorTools
{
    /// <summary>
    /// DO THE OBJECTS IN THE SCENE RUN INTO EACH OTHER?
    ///
    /// The user's sentence: "the models placed around the restaurant look like
    /// they are on top of each other." Judging by eye is not enough - at a
    /// 34 degree view an object behind can look as though it sits on top of the
    /// one in front, and two objects that really do intersect can look
    /// innocent. This tool turns the question into a measurement: it takes each
    /// object's box IN ITS REAL POSE and writes down the pairs that intersect.
    ///
    /// WHY BakeMesh: on a skinned mesh Renderer.bounds lies. Unity derives it
    /// from the root bone and does not update it as the pose changes - in the
    /// first measurement a SEATED figure came out 1.68 m tall and 1.66 m wide,
    /// both impossible. BakeMesh actually produces the posed mesh, so its box
    /// is the box of what is on screen at that moment.
    ///
    ///   Unity.exe -batchmode -quit -projectPath ...
    ///     -executeMethod Lokanta.EditorTools.PlacementAudit.Run
    /// </summary>
    public static class PlacementAudit
    {
        /// <summary>
        /// The smallest overlap that counts as an intersection.
        ///
        /// It cannot be zero: a chair HAS TO touch the table, a plate HAS TO
        /// stand ON the counter. Touching is not overlapping. 4 cm is the
        /// threshold at which, in a low-poly scene, two objects are sharing the
        /// same place.
        /// </summary>
        private const float Threshold = 0.04f;

        [MenuItem("Lokanta/Placement audit")]
        public static void Run()
        {
            try
            {
                UnityEditor.SceneManagement.EditorSceneManager.OpenScene(
                    "Assets/Lokanta/Game.unity");

                Debug.Log("=== Lokanta placement audit ===");

                foreach (string cuisine in new[] { "turk", "fastfood" })
                {
                    Core.Sim.Simulation sim = GameShot.BuildFor(cuisine);
                    if (sim == null) continue;

                    RestaurantView view = Object.FindFirstObjectByType<RestaurantView>();
                    if (view == null) { Debug.LogError("PROBLEMS: no RestaurantView"); return; }

                    view.Preview = sim;
                    view.PreviewPoses = true;
                    GameShot.Kick(view, "Awake");
                    view.Rebuild();
                    GameShot.Kick(view, "Update");

                    Audit(cuisine, sim, view);

                    view.Preview = null;
                    view.PreviewPoses = false;
                    view.Clear();
                }

                Debug.Log("=== placement audit done ===");
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (System.Exception e)
            {
                Debug.LogError("PROBLEMS: placement -> " + e.GetType().Name + ": " + e.Message);
                if (Application.isBatchMode) EditorApplication.Exit(2);
            }
        }

        private struct Item
        {
            public string Name;
            public Bounds Box;
            public bool IsFigure;
            /// <summary>Which table set it belongs to. Empty if none.</summary>
            public string Table;
        }

        private static void Audit(string cuisine, Core.Sim.Simulation sim,
                                    RestaurantView view)
        {
            List<Item> all = new List<Item>();

            // THE FIGURES - from the posed mesh.
            foreach (Figure f in view.GetComponentsInChildren<Figure>(true))
            {
                if (!f.gameObject.activeInHierarchy) continue;
                Bounds? b = PosedBounds(f.transform);
                if (b == null) continue;
                // A GUEST'S TABLE IS FOUND FROM THE POSITION.
                //
                // The parent chain was consulted before. Once the customers
                // started walking the view detached them from the table - the
                // walk has to happen in world space - and when the pairing
                // broke, every seated guest counted as "intersecting" with
                // their own chair: 45 false records.
                //
                // The 1.2 m threshold: a seated figure is at most 0.48 m away
                // (SeatRadius), while the next table is 1.85 m off.
                //
                // "Table_" is the name RestaurantView gives the table holders it
                // creates, so it stays as that file spells it.
                string table = "";
                float nearest = 1.2f;
                foreach (Transform mt in view.transform)
                {
                    if (!mt.name.StartsWith("Table_")) continue;
                    float d = Vector3.Distance(
                        new Vector3(mt.position.x, 0f, mt.position.z),
                        new Vector3(b.Value.center.x, 0f, b.Value.center.z));
                    if (d < nearest) { nearest = d; table = mt.name; }
                }

                all.Add(new Item
                {
                    Name = "figure " + f.transform.parent.name + "/" + f.name,
                    Box = b.Value,
                    IsFigure = true,
                    Table = table,
                });
            }

            // FURNITURE AND PROPS - the direct children of RestaurantView that
            // carry no figure. The floor and the badges are left out: the floor
            // is under everything, the badge is above everything.
            //
            // THE OBJECT NAMES BELOW ARE THE ONES RestaurantView CREATES. They
            // are matched by exact string, so they stay as that file spells
            // them.
            foreach (Transform t in view.transform)
            {
                if (!t.gameObject.activeInHierarchy) continue;
                if (t.name.StartsWith("Room_")) continue;

                // THE WALL IS LEFT OUT: it stands on a room boundary and by
                // definition intersects every counter pushed against that
                // boundary. Counting a transparent, collisionless, 6 cm thick
                // panel as a "placement clash" would drown the audit in noise.
                if (t.name == "Wall" || t.name == "Door") continue;

                // THE STREET is left out too: outside the plot, at ground level
                // and competing with nothing.
                if (t.name == "Pavement" || t.name == "Kerb"
                    || t.name == "Asphalt") continue;

                // THE STREET LAMP and THE CEILING LIGHT are left out as well.
                //
                // The lamp is outside the plot, on the kerb. Its only part at
                // body height is a 9 cm post, and the pedestrians now ACTIVELY
                // avoid it (StreetLife's shoving counts the posts as obstacles
                // too) - that clearance is measured on its own line. The only
                // reason it entered the box at all is that a figure's box
                // measures the ARM SPAN; a 1.14 m arm span "hits" everything on
                // a narrow pavement.
                //
                // The ceiling light has no body: it is only a light panel lying
                // on the floor. Like the floor, it is under everything.
                if (t.name == "StreetLamp" || t.name == "CeilingLight") continue;
                if (t.GetComponentInChildren<Figure>(true) != null
                    && !t.name.StartsWith("Table_")) continue;

                if (t.name.StartsWith("Table_"))
                {
                    // A table set is NOT ONE PIECE: the table and the four
                    // chairs are separate objects and touching each other is
                    // normal. They are gathered separately so that a clash with
                    // the neighbouring set becomes visible.
                    foreach (Transform c in t)
                    {
                        // A figure is NOT counted IN THIS LOOP; its posed form
                        // was already gathered above. The first version used
                        // GetComponent, and because the Figure component sits on
                        // a grandchild every guest was entered TWICE - the tool
                        // found every figure intersecting with itself.
                        if (c.GetComponentInChildren<Figure>(true) != null) continue;
                        if (c.name == "Badge") continue;
                        Bounds? cb = PosedBounds(c);
                        if (cb != null)
                            all.Add(new Item { Name = t.name + "/" + c.name,
                                               Box = cb.Value, Table = t.name });
                    }
                    continue;
                }

                Bounds? b = PosedBounds(t);
                if (b != null) all.Add(new Item { Name = t.name, Box = b.Value });
            }

            int clashes = 0, figureClashes = 0;
            float largest = 0f;
            string largestName = "";

            for (int i = 0; i < all.Count; i++)
            {
                for (int j = i + 1; j < all.Count; j++)
                {
                    // PARTS OF THE SAME TABLE SET ARE NOT COUNTED.
                    //
                    // The guest is sitting on their chair with their knees under
                    // the table; both intersect by definition. Counting those was
                    // hiding the real clashes among 47 false records.
                    if (!string.IsNullOrEmpty(all[i].Table)
                        && all[i].Table == all[j].Table) continue;

                    Vector3 o = Overlap(all[i].Box, all[j].Box);
                    if (o.x <= Threshold || o.y <= Threshold || o.z <= Threshold) continue;

                    // The HORIZONTAL overlap is what is measured: a plate
                    // standing on a counter is not a clash, sharing the same
                    // PATCH OF FLOOR is.
                    float horizontal = Mathf.Min(o.x, o.z);
                    clashes++;
                    if (all[i].IsFigure || all[j].IsFigure) figureClashes++;
                    if (horizontal > largest)
                    {
                        largest = horizontal;
                        largestName = all[i].Name + "  x  " + all[j].Name;
                    }

                    if (clashes <= 12)
                        Debug.Log(string.Format(
                            "  CLASH {0,-34} x {1,-34} overlap {2:0.00} x {3:0.00} m",
                            Short(all[i].Name), Short(all[j].Name), o.x, o.z));
                }
            }

            Debug.Log(string.Format(
                "  RESULT {0}: {1} objects, {2} clashing pairs ({3} with a figure), largest {4:0.00} m [{5}]",
                cuisine, all.Count, clashes, figureClashes, largest, Short(largestName)));

            // How much room a figure takes on screen: the average footprint.
            float width = 0f, depth = 0f, height = 0f;
            int n = 0;
            foreach (Item x in all)
            {
                if (!x.IsFigure) continue;
                width += x.Box.size.x; depth += x.Box.size.z; height += x.Box.size.y; n++;
            }
            if (n > 0)
            {
                Debug.Log(string.Format(
                    "  FIGURE {0}: seated average width {1:0.00} depth {2:0.00} height {3:0.00} m"
                    + " | seat pitch 0.88 m | table pitch {4:0.00} m",
                    cuisine, width / n, depth / n, height / n, RoomPlan.CellX));

                // DOES THE TABLE REACH THEIR HEADS?
                //
                // The user said "the table looks like it touches the
                // characters' heads". Turned into something measurable: the
                // difference between a seated figure's CHIN line (the body just
                // below the top of its box) and the top of the table. The table
                // top is 0.74 m; a seated person's table should be at chest
                // height, not at chin height.
                foreach (Item x in all)
                {
                    if (!x.IsFigure) continue;
                    float headTop = x.Box.max.y;
                    float tableTop = 0f;
                    foreach (Item y in all)
                    {
                        if (y.IsFigure || y.Table != x.Table) continue;
                        if (y.Name.IndexOf("table", System.StringComparison.Ordinal) < 0) continue;
                        tableTop = y.Box.max.y;
                        break;
                    }
                    if (tableTop <= 0f) continue;
                    Debug.Log(string.Format(
                        "  TABLE-HEAD top of the seated figure {0:0.00} m, table top {1:0.00} m"
                        + "  -> the head is {2:0.00} m above the table",
                        headTop, tableTop, headTop - tableTop));
                    break;
                }

                // IS THE POSE ACTUALLY BEING APPLIED?
                //
                // In the close-up screenshot the guests were STANDING IN FRONT
                // OF their chairs and the cushions were empty. That is SEPARATE
                // from the "the figures are big" question: if the pose is never
                // applied, the scale can be anything and it will still look
                // wrong.
                //
                // The same figure is measured in two poses. If the boxes come
                // out THE SAME the clip is doing nothing; a seated body has to
                // be noticeably LOWER than a standing one.
                foreach (Figure f in view.GetComponentsInChildren<Figure>(true))
                {
                    if (!f.gameObject.activeInHierarchy) continue;

                    f.Sample(Figure.Pose.Idle, 0.4f);
                    Bounds? standing = PosedBounds(f.transform);
                    f.Sample(Figure.Pose.Sit, 0.4f);
                    Bounds? seated = PosedBounds(f.transform);
                    if (standing == null || seated == null) break;

                    Vector3 a = standing.Value.size, o = seated.Value.size;
                    bool changed = Mathf.Abs(a.y - o.y) > 0.05f
                                   || Mathf.Abs(a.z - o.z) > 0.05f;
                    Debug.Log(string.Format(
                        "  POSE idle {0:0.00}x{1:0.00}x{2:0.00} | sit {3:0.00}x{4:0.00}x{5:0.00}"
                        + "  -> {6}",
                        a.x, a.y, a.z, o.x, o.y, o.z,
                        changed ? "the pose IS APPLIED" : "THE POSE IS NOT APPLIED - the clip does nothing"));
                    break;
                }

                // THE ORIENTATION OF THE PROPS.
                //
                // The user's request: "check the placement and the orientation
                // of the props inside the restaurant".
                //
                // The rule is measurable: a prop PUSHED AGAINST A WALL has to
                // face INTO the room. A stove's mouth cannot be turned to the
                // wall, a counter's front cannot face the wall. A prop's
                // "front" comes from the RestaurantView.PropYaw rule (furniture
                // faces -Z at yaw 0), so there is no second assumption here.
                //
                // WHY MEASURE: "somebody looked at it" is true once; when the
                // floor plan or the layout changes nobody looks again.
                {
                    int checkedProps = 0, backwards = 0;
                    var bad = new System.Text.StringBuilder();

                    foreach (Transform t in view.transform)
                    {
                        if (!t.gameObject.activeInHierarchy) continue;
                        if (t.GetComponentInChildren<Figure>(true) != null) continue;
                        if (t.name.StartsWith("Room_") || t.name == "Wall"
                            || t.name == "Door" || t.name == "Pavement"
                            || t.name == "Kerb" || t.name == "Asphalt"
                            || t.name == "StreetLamp"
                            // THE DOORMAT is a flat panel: it has no "front",
                            // and therefore no orientation rule either. The
                            // audit was counting it as facing the wall, and that
                            // was not a bug but the rule not applying to that
                            // object.
                            || t.name == "Threshold"
                            || t.name.StartsWith("Table_")) continue;

                        Vector3 p = t.localPosition;
                        int roomIndex = Lokanta.Game.Paths.RoomAt(p);
                        if (roomIndex < 0) continue;

                        RoomPlan.Room r = RoomPlan.Rooms[roomIndex];

                        // Which wall it is against: the nearest edge.
                        float dLeft = p.x - r.X0;
                        float dRight = r.X0 + r.W - p.x;
                        float dFront = p.z - r.Z0;
                        float dBack = r.Z0 + r.D - p.z;
                        float nearestEdge = Mathf.Min(Mathf.Min(dLeft, dRight), Mathf.Min(dFront, dBack));
                        if (nearestEdge > 1.0f) continue;   // a prop out in the middle: no rule

                        Vector3 inwards;
                        if (nearestEdge == dLeft) inwards = Vector3.right;
                        else if (nearestEdge == dRight) inwards = Vector3.left;
                        else if (nearestEdge == dFront) inwards = Vector3.forward;
                        else inwards = Vector3.back;

                        // A piece of furniture's front: -Z at yaw 0
                        // (RestaurantView.PropYaw).
                        Vector3 front = t.localRotation * Vector3.back;
                        front.y = 0f;

                        checkedProps++;
                        float angle = Vector3.Angle(front, inwards);
                        if (angle > 100f)
                        {
                            backwards++;
                            if (bad.Length < 220)
                                bad.Append(t.name + "(" + r.Name + ","
                                           + angle.ToString("0") + ") ");
                        }
                    }

                    Debug.Log(string.Format(
                        "  FACING {0} props are against a wall, {1} of them face the wall {2}",
                        checkedProps, backwards, bad.ToString()));
                }

                // SELF-VERIFICATION: the measured height of a standing member
                // of staff has to come out the same as ArtPrefabs' target. If it
                // does not, what is being measured is not the figure in this
                // scene.
                foreach (Figure f in view.GetComponentsInChildren<Figure>(true))
                {
                    if (!f.gameObject.activeInHierarchy) continue;
                    if (f.Current == Figure.Pose.Sit) continue;

                    // SAMPLE THE STANDING POSE EXPLICITLY.
                    //
                    // The figure used to be measured in whatever pose IT HAD IN
                    // THE SCENE, and once the kitchen work was added that pose
                    // was no longer "standing": a cook chopping measured 0.90 m
                    // against a target of 1.00. The check is called "standing
                    // figure height" - so it has to be made to stand.
                    f.Sample(Figure.Pose.Idle, 0.4f);
                    Bounds? b = PosedBounds(f.transform);
                    if (b == null) continue;
                    Debug.Log(string.Format(
                        "  VERIFICATION standing figure height {0:0.00} m (the ArtPrefabs target"
                        + " for the reference model is " + ArtPrefabs.CharacterHeight.ToString("0.00")
                        + ") - if the gap is large the measurement is wrong",
                        b.Value.size.y));

                    // BODY WIDTH: this number sets the minimum distance between
                    // two pedestrians on the street (StreetLife.Personal).
                    //
                    // Why it had to be measured: the user saw pedestrians
                    // walking through one another, and the fix was two pedestrian
                    // lanes plus shoving - both of which want a DISTANCE.
                    // Guessing that distance would have been making the same
                    // mistake a second time.
                    //
                    // WHY NOT THE BOUNDING BOX: the first version used the box
                    // and measured 0.60 x 1.14 m - an absurd footprint for a
                    // figure a metre tall. The box measures the ARM SPAN; the
                    // figures in this package are larger across than they are
                    // tall (the same trap was hit in the sitting measurement,
                    // docs/35). And two pedestrians "walking through each other"
                    // is their BODIES overlapping, not their arms: at a 34 degree
                    // view a swinging arm is not visible, two interpenetrating
                    // legs are.
                    //
                    // THE SECOND MEASUREMENT WAS ALSO WRONG: the hip band (18%
                    // to 50% of the height) gave 1.08 m, because at those
                    // proportions THE HANDS are at that height too. Both bands
                    // had been chosen with a "reasonable" justification and both
                    // were measuring arms.
                    //
                    // The only way to find the right band was to print THE WHOLE
                    // PROFILE; that is why Profile() is a permanent diagnostic.
                    // The number used is the widest band excluding the arms - on
                    // these figures that is THE HEAD (0.67 m), wider than the
                    // shoulders (0.58).
                    WalkSpeed(f);
                    f.Sample(Figure.Pose.Idle, 0.4f);
                    b = PosedBounds(f.transform);
                    if (b == null) break;

                    float body = BodyWidth(f.transform, b.Value);
                    Debug.Log(string.Format(
                        "  BODY widest band excluding the arms {0:0.00} m"
                        + " (box {1:0.00} x {2:0.00} - WITH THE ARMS)"
                        + " | pedestrian minimum distance {3:0.00} | lane pitch {4:0.00}"
                        + " - {5}",
                        body, b.Value.size.x, b.Value.size.z,
                        StreetLife.Personal, Paths.LaneHalf * 2f,
                        (StreetLife.Personal >= body && Paths.LaneHalf * 2f >= body)
                            ? "the distance is enough"
                            : "THE DISTANCE IS SHORT - Personal / LaneHalf must be raised"));
                    break;
                }
            }
        }

        /// <summary>
        /// The figure's widest horizontal band EXCLUDING THE ARMS.
        ///
        /// This is the answer to how close two pedestrians can pass. The
        /// bounding box measures the arm span (1.14 m) and so do the bands at
        /// hand height (1.08 m) - both are wrong. The arms hang down the lower
        /// half of the body, so the measurement looks at THE UPPER HALF: the
        /// shoulders, the head and what is between them. At this package's
        /// proportions the widest of those is THE HEAD (the head is a third of
        /// the body).
        ///
        /// The vertices of a baked mesh can be read; the package's own mesh
        /// cannot (isReadable: 0), and not knowing that distinction led to one
        /// empty measurement on this project.
        /// </summary>
        private static float BodyWidth(Transform t, Bounds box)
        {
            Profile(t, box);

            float widest = 0f;
            int totalVertices = 0;

            // The bands of the upper half: from 50% to 100%, a tenth at a time.
            for (int k = 5; k < 10; k++)
            {
                float y0 = box.min.y + box.size.y * (k / 10f);
                float y1 = box.min.y + box.size.y * ((k + 1) / 10f);
                int count;
                float g = BandDiameter(t, y0, y1, out count);
                totalVertices += count;
                if (g > widest) widest = g;
            }

            // IF NO VERTICES WERE FOUND we do not return zero: zero means "there
            // is no body", and that is a number that passes every threshold
            // silently. The box itself is returned - on the safe side, and
            // visibly large.
            if (totalVertices < 8)
            {
                Debug.LogWarning("  the BODY measurement is empty: " + totalVertices
                                 + " vertices found, falling back to the box");
                return Mathf.Max(box.size.x, box.size.z);
            }
            return widest;
        }

        /// <summary>The largest horizontal diameter within one y band.</summary>
        private static float BandDiameter(Transform t, float y0, float y1, out int count)
        {
            float xa = float.MaxValue, xb = float.MinValue;
            float za = float.MaxValue, zb = float.MinValue;
            count = 0;

            foreach (SkinnedMeshRenderer smr in
                     t.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (smr.sharedMesh == null || !smr.gameObject.activeInHierarchy) continue;

                Mesh baked = new Mesh();
                smr.BakeMesh(baked, false);
                Vector3[] v = baked.vertices;
                Object.DestroyImmediate(baked);

                // The scale is already inside BakeMesh's output (the bones sit
                // under the scaled root); only the position and the rotation are
                // applied - PosedBounds carries the same justification.
                Matrix4x4 m = Matrix4x4.TRS(smr.transform.position,
                                            smr.transform.rotation, Vector3.one);
                for (int i = 0; i < v.Length; i++)
                {
                    Vector3 p = m.MultiplyPoint3x4(v[i]);
                    if (p.y < y0 || p.y > y1) continue;
                    if (p.x < xa) xa = p.x;
                    if (p.x > xb) xb = p.x;
                    if (p.z < za) za = p.z;
                    if (p.z > zb) zb = p.z;
                    count++;
                }
            }
            return count == 0 ? 0f : Mathf.Max(xb - xa, zb - za);
        }

        /// <summary>
        /// The figure's HORIZONTAL PROFILE against height - not a one-line
        /// diagnostic but ten bands.
        ///
        /// Why it is permanent: no single number says how wide these figures
        /// are at which height. The box said 1.14 (the arms), the hip band said
        /// 1.08 (the hands), the shoulders 0.58, the head 0.67. Those four only
        /// made sense side by side, and the pedestrian lane pitch was chosen by
        /// looking at that profile. If the figure scale or the package changes,
        /// these lines are where to look.
        /// </summary>
        private static void Profile(Transform t, Bounds box)
        {
            for (int k = 0; k < 10; k++)
            {
                float y0 = box.min.y + box.size.y * (k / 10f);
                float y1 = box.min.y + box.size.y * ((k + 1) / 10f);
                int n;
                float diameter = BandDiameter(t, y0, y1, out n);
                Debug.Log(string.Format(
                    "  PROFILE y {0:0.00}-{1:0.00} vertices {2,4} | widest diameter {3:0.00} m",
                    y0, y1, n, diameter));
            }
        }

        /// <summary>
        /// THE WALK CLIP'S NATURAL SPEED (m/s).
        ///
        /// Why it had to be measured: the figure's ground speed (Walker.Speed x
        /// the game speed) and the clip's playback rate WERE NOT TIED TOGETHER -
        /// Anim.speed was not set anywhere. So at x4 the figure moves four times
        /// as fast while the legs keep the same tempo: the feet slide along the
        /// floor. That is what the user was seeing.
        ///
        /// To fix it you need the clip's OWN speed, and that number is written
        /// nowhere (the clip is a loop marking time; there is no root motion).
        /// The measurement:
        ///
        ///   stride length = the WIDEST the two feet open across the cycle
        ///   one cycle     = two steps
        ///   natural speed = 2 x stride / clip length
        ///
        /// The feet are found FROM THE MESH, not from a bone: this skeleton has
        /// no foot bone (one bone per leg plus a knee added later). Among the
        /// vertices each leg influences, THE LOWEST one counts as that leg's
        /// foot.
        /// </summary>
        private static void WalkSpeed(Figure f)
        {
            if (f == null || f.Clips == null) return;
            int idx = (int)Figure.Pose.Walk;
            if (idx >= f.Clips.Length || f.Clips[idx] == null) return;

            AnimationClip clip = f.Clips[idx];
            float length = clip.length;
            if (length <= 0.001f) return;

            SkinnedMeshRenderer smr = null;
            foreach (SkinnedMeshRenderer r in f.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                if (r.sharedMesh != null && r.sharedMesh.isReadable) { smr = r; break; }
            if (smr == null)
            {
                Debug.Log("  WALK could not be measured: no readable mesh");
                return;
            }

            int left = BoneIndex(smr.bones, "knee-left");
            int right = BoneIndex(smr.bones, "knee-right");
            if (left < 0) left = BoneIndex(smr.bones, "leg-left");
            if (right < 0) right = BoneIndex(smr.bones, "leg-right");
            if (left < 0 || right < 0)
            {
                Debug.Log("  WALK could not be measured: no leg bone found");
                return;
            }

            BoneWeight[] w = smr.sharedMesh.boneWeights;
            const int Samples = 24;
            float widest = 0f;

            for (int i = 0; i < Samples; i++)
            {
                f.Sample(Figure.Pose.Walk, length * i / Samples);

                Mesh baked = new Mesh();
                smr.BakeMesh(baked, false);
                Vector3[] v = baked.vertices;
                Object.DestroyImmediate(baked);
                if (v.Length != w.Length) return;

                Vector3 footLeft = Foot(v, w, left);
                Vector3 footRight = Foot(v, w, right);
                if (footLeft == Vector3.zero || footRight == Vector3.zero) continue;

                Vector3 d = footLeft - footRight;
                d.y = 0f;
                if (d.magnitude > widest) widest = d.magnitude;
            }

            f.Sample(Figure.Pose.Idle, 0.4f);

            if (widest <= 0.001f)
            {
                Debug.Log("  WALK could not be measured: no stride found");
                return;
            }

            float natural = 2f * widest / length;
            Debug.Log(string.Format(
                "  WALK clip {0:0.00} s | stride {1:0.000} m | NATURAL SPEED {2:0.00} m/s"
                + " | Figure.WalkClipSpeed {3:0.00} - {4}",
                length, widest, natural, Figure.WalkClipSpeed,
                Mathf.Abs(natural - Figure.WalkClipSpeed) < 0.12f
                    ? "in step" : "THEY HAVE DRIFTED - WalkClipSpeed must be updated"));
        }

        /// <summary>THE LOWEST of the vertices one leg influences.</summary>
        private static Vector3 Foot(Vector3[] v, BoneWeight[] w, int bone)
        {
            Vector3 best = Vector3.zero;
            float low = float.MaxValue;
            for (int i = 0; i < v.Length; i++)
            {
                if (!Influences(w[i], bone)) continue;
                if (v[i].y >= low) continue;
                low = v[i].y;
                best = v[i];
            }
            return best;
        }

        private static bool Influences(BoneWeight b, int bone)
        {
            return (b.boneIndex0 == bone && b.weight0 > 0.5f)
                || (b.boneIndex1 == bone && b.weight1 > 0.5f)
                || (b.boneIndex2 == bone && b.weight2 > 0.5f)
                || (b.boneIndex3 == bone && b.weight3 > 0.5f);
        }

        private static int BoneIndex(Transform[] bones, string name)
        {
            for (int i = 0; i < bones.Length; i++)
                if (bones[i] != null && bones[i].name == name) return i;
            return -1;
        }

        private static string Short(string s)
        {
            s = s.Replace("(Clone)", "").Replace("figure ", "");
            return s.Length <= 34 ? s : s.Substring(0, 34);
        }

        /// <summary>How much two boxes overlap on each axis.</summary>
        private static Vector3 Overlap(Bounds a, Bounds b)
        {
            return new Vector3(
                Mathf.Min(a.max.x, b.max.x) - Mathf.Max(a.min.x, b.min.x),
                Mathf.Min(a.max.y, b.max.y) - Mathf.Max(a.min.y, b.min.y),
                Mathf.Min(a.max.z, b.max.z) - Mathf.Max(a.min.z, b.min.z));
        }

        /// <summary>
        /// The object's world box IN ITS REAL POSE. Skinned meshes are posed
        /// and then measured, the rest are measured directly.
        ///
        /// FigureShot calls it FROM HERE too: writing a second copy has drifted
        /// apart silently five times on this project.
        /// </summary>
        internal static Bounds? PosedBounds(Transform t)
        {
            Bounds? acc = null;

            foreach (SkinnedMeshRenderer smr in
                     t.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (smr.sharedMesh == null || !smr.gameObject.activeInHierarchy) continue;

                // THE SCALE ONCE ONLY. This was got wrong twice:
                //
                //   1. useScale=true + localToWorldMatrix -> the scale twice
                //   2. useScale=false + localToWorldMatrix -> STILL twice
                //
                // The second one showed why: BakeMesh deforms the mesh against
                // THE BONES, and the bones already sit under the scaled root, so
                // the scale IS inside the output. The useScale flag only adds
                // THE RENDERER'S own scale. The right way: the flag off and the
                // scale taken out of the transform - position and rotation only.
                //
                // The reason this tool was written was "measuring the wrong
                // thing", and the tool did exactly that to itself twice. That is
                // why the VERIFICATION line below exists: the measured height is
                // compared against the target in ArtPrefabs, and if they do not
                // agree the numbers are rubbish.
                Mesh baked = new Mesh();
                smr.BakeMesh(baked, false);
                Bounds lb = baked.bounds;
                Object.DestroyImmediate(baked);

                Matrix4x4 m = Matrix4x4.TRS(smr.transform.position,
                                            smr.transform.rotation, Vector3.one);
                acc = Merge(acc, ToWorld(lb, m));
            }

            foreach (MeshRenderer mr in t.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (!mr.gameObject.activeInHierarchy) continue;
                if (mr.name == "Badge" || mr.transform.parent != null
                    && mr.transform.parent.name == "Badge") continue;
                acc = Merge(acc, mr.bounds);
            }

            return acc;
        }

        private static Bounds? Merge(Bounds? a, Bounds b)
        {
            if (a == null) return b;
            Bounds x = a.Value;
            x.Encapsulate(b);
            return x;
        }

        private static Bounds ToWorld(Bounds b, Matrix4x4 m)
        {
            Vector3 c = m.MultiplyPoint3x4(b.center);
            Vector3 e = b.extents;
            Vector3 ne = new Vector3(
                Mathf.Abs(m.m00) * e.x + Mathf.Abs(m.m01) * e.y + Mathf.Abs(m.m02) * e.z,
                Mathf.Abs(m.m10) * e.x + Mathf.Abs(m.m11) * e.y + Mathf.Abs(m.m12) * e.z,
                Mathf.Abs(m.m20) * e.x + Mathf.Abs(m.m21) * e.y + Mathf.Abs(m.m22) * e.z);
            return new Bounds(c, ne * 2f);
        }
    }
}
