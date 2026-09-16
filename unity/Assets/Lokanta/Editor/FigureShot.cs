using Lokanta.Game;
using UnityEditor;
using UnityEngine;

namespace Lokanta.EditorTools
{
    /// <summary>
    /// ONE figure, ONE chair, from the side. Nothing else at all.
    ///
    /// Why it was needed: the question "are the guests sitting down, or
    /// standing in front of the chair" cannot be answered in a full hall - at
    /// a 34 degree view a figure standing in front can look seated, and a
    /// seated figure can look as though it is standing. The measurement tool
    /// gave contradictory numbers too (the same figure was 1.27 m in one place
    /// and 1.60 m in another).
    ///
    /// In this screenshot no ambiguity is left: the camera looks from the
    /// side, and the chair's cushion and the figure's hip are in the same
    /// frame.
    ///
    ///   -executeMethod Lokanta.EditorTools.FigureShot.Capture
    /// </summary>
    public static class FigureShot
    {
        private const string PrefabDir = "Assets/Lokanta/Art/Prefab";

        [MenuItem("Lokanta/Figure scale screenshot")]
        public static void Capture()
        {
            try
            {
                UnityEditor.SceneManagement.EditorSceneManager.OpenScene(
                    "Assets/Lokanta/Game.unity");

                // The folders are "Furniture" and "Characters" on disk; they
                // used to be "Mobilya" and "Karakter", and a path left behind by
                // that rename loads nothing.
                GameObject chair = Load("Furniture/chairCushion");
                GameObject character = Load("Characters/character-male-a");
                if (chair == null || character == null)
                {
                    Debug.LogError("PROBLEMS: no prefab");
                    if (Application.isBatchMode) EditorApplication.Exit(2);
                    return;
                }

                GameObject root = new GameObject("ScaleStage");

                // The floor: 1 m squares, so the scale can be read off.
                for (int i = -2; i <= 2; i++)
                {
                    Slab(root.transform, new Vector3(i * 1.0f, -0.01f, 0f),
                         new Vector3(0.02f, 0.02f, 4f), new Color(0.5f, 0.5f, 0.5f));
                    Slab(root.transform, new Vector3(0f, -0.01f, i * 1.0f),
                         new Vector3(4f, 0.02f, 0.02f), new Color(0.5f, 0.5f, 0.5f));
                }
                Slab(root.transform, new Vector3(0f, -0.06f, 0f),
                     new Vector3(6f, 0.1f, 6f), new Color(0.30f, 0.25f, 0.20f));

                // The chair at the origin, its back to the camera's left.
                // +180: THE SAME as in the game. The chair model faces the
                // opposite way from the character (see the facing shot below).
                GameObject s = Object.Instantiate(chair, root.transform);
                s.transform.localPosition = Vector3.zero;
                s.transform.localRotation = Quaternion.Euler(0f, 90f + 180f, 0f);

                // The figure: THE SAME arithmetic as in the game - the seat
                // position and the lift are read from RestaurantView, we do not
                // write a second number here. Writing the same number in two
                // places has drifted apart silently five times on this project.
                // The chair faces +X (90+180), which means the table is at +X:
                // so the figure's forward shift is in +X too.
                GameObject k = Object.Instantiate(character, root.transform);
                k.transform.localPosition = new Vector3(
                    RestaurantView.SitForwardM, RestaurantView.SitLiftM, 0f);
                k.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);

                Figure f = k.GetComponentInChildren<Figure>(true);
                if (f != null) f.Sample(Figure.Pose.Sit, 0.4f);

                // The frame is TIGHT: the few centimetres between the figure's
                // leg and the chair's cushion could not be read in a 2.6 m box
                // anyway - a screenshot written to answer "is it sitting down"
                // does not answer it from that far away.
                Bounds target = new Bounds(new Vector3(0f, 0.55f, 0f),
                                           new Vector3(1.3f, 1.1f, 1.3f));
                GameShot.Shoot("scale_sitting.png", target, 1280, 720,
                               pitch: 8f, yaw: 0f);

                // SITTING WITHOUT THE CHAIR: the pose itself, with nothing
                // standing in front of it. The chair hides half the silhouette
                // and the question "is that the thigh or the shin" goes
                // unanswered.
                s.SetActive(false);
                GameShot.Shoot("scale_sitting_bare.png", target, 1280, 720,
                               pitch: 8f, yaw: 0f);
                s.SetActive(true);

                // THE LEG ANGLES ARE SWEPT.
                //
                // A single screenshot could not answer "did the thigh turn or
                // the shin": the chair hides half the silhouette and the arm and
                // the leg are the same colour. Put four different angles side by
                // side and the mechanism reads at a glance.
                {
                    float oldThigh = Figure.ThighAngle, oldShin = Figure.ShinTilt;
                    float[,] trials = { { 0f, 0f }, { -45f, 0f },
                                        { -78f, 0f }, { -78f, -40f } };
                    for (int d = 0; d < 4; d++)
                    {
                        Figure.ThighAngle = trials[d, 0];
                        Figure.ShinTilt = trials[d, 1];
                        GameObject copy = Object.Instantiate(character, root.transform);
                        copy.transform.localPosition =
                            new Vector3(-1.2f + d * 0.8f, RestaurantView.SitLiftM, 2.5f);
                        copy.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
                        Figure kf = copy.GetComponentInChildren<Figure>(true);
                        if (kf != null) kf.Sample(Figure.Pose.Sit, 0.4f);
                    }
                    Figure.ThighAngle = oldThigh;
                    Figure.ShinTilt = oldShin;

                    GameShot.Shoot("scale_leg_sweep.png",
                                   new Bounds(new Vector3(0.05f, 0.55f, 2.5f),
                                              new Vector3(3.6f, 1.2f, 1.2f)),
                                   1600, 620, pitch: 8f, yaw: 0f);
                }

                if (f != null) f.Sample(Figure.Pose.Idle, 0.4f);
                k.transform.localPosition = new Vector3(-0.9f, 0f, 0f);
                GameShot.Shoot("scale_standing.png", target, 1280, 720,
                               pitch: 8f, yaw: 0f);

                // --- WHICH WAY DOES THE CHAIR FACE -------------------
                //
                // "The chairs are the wrong way round" can be said by eye, but
                // not which axis they are wrong about. This shot settles it: the
                // chair stands UNROTATED (yaw 0), a RED cube at +Z and a BLUE
                // cube at -Z. Whichever cube the backrest is on is the chair's
                // "back".
                //
                // RestaurantView lays the seats out with Seat(k) = rot(k*90) *
                // (0,0,-r), so the k=0 chair is at -Z and has to face +Z to look
                // at the table.
                // Backwards: DestroyImmediate changes the child list
                // IMMEDIATELY, and a forward loop skips half of it.
                for (int c = root.transform.childCount - 1; c >= 0; c--)
                    Object.DestroyImmediate(root.transform.GetChild(c).gameObject);

                Slab(root.transform, new Vector3(0f, -0.06f, 0f),
                     new Vector3(6f, 0.1f, 6f), new Color(0.30f, 0.25f, 0.20f));

                GameObject s2 = Object.Instantiate(chair, root.transform);
                s2.transform.localPosition = Vector3.zero;
                s2.transform.localRotation = Quaternion.identity;   // yaw 0

                Slab(root.transform, new Vector3(0f, 0.12f, 0.62f),
                     new Vector3(0.22f, 0.22f, 0.22f), new Color(0.90f, 0.20f, 0.18f));
                Slab(root.transform, new Vector3(0f, 0.12f, -0.62f),
                     new Vector3(0.22f, 0.22f, 0.22f), new Color(0.20f, 0.45f, 0.95f));

                GameShot.Shoot("scale_chair.png",
                               new Bounds(new Vector3(0f, 0.45f, 0f),
                                          new Vector3(2.0f, 1.2f, 2.0f)),
                               1280, 720, pitch: 12f, yaw: 90f);

                // WHICH WAY DOES THE FIGURE FACE - the same method.
                //
                // The chair and the figure use THE SAME angle (k*90); their
                // local "front" directions do not HAVE to be the same, and
                // assuming they are would mean turning both of them the wrong
                // way round at once.
                Object.DestroyImmediate(s2);
                GameObject k2 = Object.Instantiate(character, root.transform);
                k2.transform.localPosition = Vector3.zero;
                k2.transform.localRotation = Quaternion.identity;   // yaw 0
                Figure f2 = k2.GetComponentInChildren<Figure>(true);
                if (f2 != null) f2.Sample(Figure.Pose.Idle, 0.4f);

                GameShot.Shoot("scale_figure_facing.png",
                               new Bounds(new Vector3(0f, 0.55f, 0f),
                                          new Vector3(2.0f, 1.4f, 2.0f)),
                               1280, 720, pitch: 12f, yaw: 90f);

                // ALL THE FURNITURE, WITH THE SAME MARKER, IN ONE FRAME.
                //
                // Once the chair turned out to be the wrong way round, the
                // question was whether there were other cases of the same class:
                // every model's local "front" has to be measured one by one, not
                // assumed. They all stand at yaw 0; the red cube is at each
                // one's +Z.
                for (int c = root.transform.childCount - 1; c >= 0; c--)
                    Object.DestroyImmediate(root.transform.GetChild(c).gameObject);

                Slab(root.transform, new Vector3(0f, -0.06f, 0f),
                     new Vector3(14f, 0.1f, 6f), new Color(0.30f, 0.25f, 0.20f));

                string[] names =
                {
                    "Furniture/chairCushion", "Furniture/tableRound",
                    "Furniture/kitchenStove", "Furniture/kitchenBar",
                    "Furniture/kitchenSink", "Furniture/kitchenFridgeLarge",
                    "Furniture/bookcaseClosedDoors",
                };
                for (int i = 0; i < names.Length; i++)
                {
                    GameObject p = Load(names[i]);
                    if (p == null) { Debug.LogWarning("no prefab: " + names[i]); continue; }
                    float x = (i - (names.Length - 1) * 0.5f) * 1.7f;

                    GameObject g = Object.Instantiate(p, root.transform);
                    g.transform.localPosition = new Vector3(x, 0f, 0f);
                    g.transform.localRotation = Quaternion.identity;

                    Slab(root.transform, new Vector3(x, 0.10f, 0.75f),
                         new Vector3(0.20f, 0.20f, 0.20f), new Color(0.90f, 0.20f, 0.18f));
                }

                GameShot.Shoot("scale_furniture_facing.png",
                               new Bounds(new Vector3(0f, 0.7f, 0f),
                                          new Vector3(13f, 2.2f, 3.0f)),
                               1600, 600, pitch: 18f, yaw: 0f);

                // THE STOVE FROM ABOVE: is the flame over the burner?
                //
                // "The flame looks slightly off" can be said by eye, but not by
                // how much - at a 34 degree view everything on a top surface
                // looks skewed. Looking straight down puts the burner and the
                // flame in the same frame, on the same plane.
                for (int c = root.transform.childCount - 1; c >= 0; c--)
                    Object.DestroyImmediate(root.transform.GetChild(c).gameObject);

                GameObject stovePrefab = Load("Furniture/kitchenStove");
                if (stovePrefab != null)
                {
                    GameObject stove = Object.Instantiate(stovePrefab, root.transform);
                    stove.transform.localPosition = Vector3.zero;
                    stove.transform.localRotation = Quaternion.identity;

                    Shader unlit = Shader.Find("Universal Render Pipeline/Unlit");
                    Material m = unlit != null ? new Material(unlit) : null;
                    Appliance ap = Appliance.Attach(stove, m, null);
                    if (ap != null) ap.SetWorking(true);

                    GameShot.Shoot("scale_stove_from_above.png",
                                   new Bounds(new Vector3(0f, 0.5f, 0f),
                                              new Vector3(1.1f, 0.4f, 1.1f)),
                                   900, 900, pitch: 89.9f, yaw: 0f);
                }

                // THE TABLE FROM ABOVE: are the seats on an edge or on a
                // corner?
                //
                // The table is six-sided (a low-poly round) and the seats are
                // laid out at 90 degree intervals - while a hexagon's sides are
                // at 60. If the two do not line up the guest sits at the table's
                // CORNER. That can only be measured looking straight down.
                for (int c = root.transform.childCount - 1; c >= 0; c--)
                    Object.DestroyImmediate(root.transform.GetChild(c).gameObject);

                GameObject tablePrefab = Load("Furniture/table");
                if (tablePrefab != null)
                {
                    GameObject m2 = Object.Instantiate(tablePrefab, root.transform);
                    m2.transform.localPosition = Vector3.zero;
                    m2.transform.localRotation = Quaternion.identity;
                    // THE SAME as the game: the table is squared off.
                    Renderer[] mr = m2.GetComponentsInChildren<Renderer>(true);
                    if (mr.Length > 0)
                    {
                        Bounds mb = mr[0].bounds;
                        for (int i = 1; i < mr.Length; i++) mb.Encapsulate(mr[i].bounds);
                        if (mb.size.z > 0.0001f)
                            m2.transform.localScale =
                                new Vector3(1f, 1f, mb.size.x / mb.size.z);
                    }

                    for (int seat = 0; seat < 4; seat++)
                    {
                        GameObject ch = Object.Instantiate(chair, root.transform);
                        // THE SAME arithmetic as the game: the radius varies by
                        // axis (a square table).
                        ch.transform.localPosition = RestaurantView.SeatAt(seat);
                        ch.transform.localRotation =
                            Quaternion.Euler(0f, seat * 90f + 180f, 0f);
                    }

                    GameShot.Shoot("scale_table_from_above.png",
                                   new Bounds(new Vector3(0f, 0.35f, 0f),
                                              new Vector3(2.0f, 0.7f, 2.0f)),
                                   900, 900, pitch: 89.9f, yaw: 0f);

                    // AND FROM THE GAME'S ANGLE: an alignment that looks right
                    // from above can read differently at 34 degrees, where the Z
                    // axis is compressed to 0.56. The decision has to be taken
                    // from the angle the game looks through.
                    GameShot.Shoot("scale_table_game_angle.png",
                                   new Bounds(new Vector3(0f, 0.35f, 0f),
                                              new Vector3(1.9f, 0.9f, 1.9f)),
                                   900, 620);
                }

                Object.DestroyImmediate(root);
                SeatProfile(chair, character);
                Debug.Log("=== scale screenshots taken: render/scale_*.png ===");
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (System.Exception e)
            {
                Debug.LogError("PROBLEMS: figure scale -> " + e.Message);
                if (Application.isBatchMode) EditorApplication.Exit(2);
            }
        }

        // =====================================================================
        /// <summary>
        /// THE CHAIR PROFILE AND THE SEATED FIGURE - IN NUMBERS, not in a
        /// picture.
        ///
        /// The user's sentence: "when the characters sit on the chair their
        /// back and their behind end up on top of the chair". A shot from the
        /// side shows this but does not say BY HOW MUCH - and whether the fix
        /// is right can only be told from a number.
        ///
        /// The chair stands at THE GAME'S angle (yaw PropYaw), so its front
        /// faces +Z and the backrest is at -Z. The figure is at yaw 0 as in the
        /// game (facing +Z).
        /// </summary>
        private static void SeatProfile(GameObject chairPrefab,
                                        GameObject characterPrefab)
        {
            GameObject root = new GameObject("SittingMeasurement");

            GameObject s = Object.Instantiate(chairPrefab, root.transform);
            s.transform.localPosition = Vector3.zero;
            s.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

            // --- the chair's centre-strip profile ---
            //
            // THE STRIP GOES THROUGH THE MODEL'S CENTRE, not through the world
            // origin: the first version tested |x| < 0.06 and found NO vertices
            // at all - the package's chair pivot is not centred. The bounding
            // box's centre is asked for instead, so where the pivot sits does
            // not matter.
            Bounds sb = new Bounds(s.transform.position, Vector3.zero);
            bool first = true;
            foreach (Renderer r in s.GetComponentsInChildren<Renderer>(true))
            {
                if (first) { sb = r.bounds; first = false; }
                else sb.Encapsulate(r.bounds);
            }
            Debug.Log(string.Format(
                "SITTING chair box: centre ({0:0.000} {1:0.000} {2:0.000}) "
                + "size ({3:0.000} {4:0.000} {5:0.000})",
                sb.center.x, sb.center.y, sb.center.z,
                sb.size.x, sb.size.y, sb.size.z));

            // A RAY, NOT A VERTEX READ.
            //
            // The package's meshes have Read/Write OFF (isReadable: 0), so
            // .vertices comes back empty even in the editor - which is exactly
            // why the first version found no vertices. MeshCollider does not
            // need it: the collision data is baked at import. A ray coming down
            // from above gives the chair's side silhouette directly.
            foreach (MeshFilter mf in s.GetComponentsInChildren<MeshFilter>(true))
            {
                if (mf.sharedMesh == null) continue;
                MeshCollider mc = mf.gameObject.AddComponent<MeshCollider>();
                mc.sharedMesh = mf.sharedMesh;
            }
            Physics.SyncTransforms();

            float half = sb.size.z * 0.5f + 0.02f;
            var profile = new System.Collections.Generic.List<Vector2>();
            for (float z = -half; z <= half + 0.001f; z += 0.01f)
            {
                RaycastHit h;
                if (Physics.Raycast(new Vector3(sb.center.x, sb.max.y + 1f,
                                                sb.center.z + z),
                                    Vector3.down, out h, 3f))
                    profile.Add(new Vector2(z, h.point.y));
            }

            if (profile.Count == 0)
            {
                Debug.LogWarning("SITTING: the chair silhouette could not be measured");
                Object.DestroyImmediate(root);
                return;
            }

            float top = 0f;
            for (int i = 0; i < profile.Count; i++) top = Mathf.Max(top, profile[i].y);

            var line = new System.Text.StringBuilder(
                "SITTING chair silhouette (z -> surface y):");
            for (int i = 0; i < profile.Count; i += 3)
                line.Append("  " + profile[i].x.ToString("0.00")
                            + ":" + profile[i].y.ToString("0.00"));
            Debug.Log(line.ToString());

            // THE SEAT: the flat part IN FRONT OF the backrest. The chair is at
            // yaw 180, so its front is +Z and the backrest is on the -Z side.
            float seatY = 0f;
            for (int i = 0; i < profile.Count; i++)
                if (profile[i].y < top * 0.80f)
                    seatY = Mathf.Max(seatY, profile[i].y);

            // THE FRONT FACE OF THE BACKREST: the frontmost z at backrest
            // height. If the figure's back falls behind this it goes into the
            // backrest.
            float backFrontZ = -99f;
            for (int i = 0; i < profile.Count; i++)
                if (profile[i].y > top * 0.80f)
                    backFrontZ = Mathf.Max(backFrontZ, profile[i].x);

            // THE FRONT EDGE OF THE CUSHION: the thighs have to stay IN FRONT of
            // this, or the legs pass through the cushion.
            float seatFrontZ = -99f;
            for (int i = 0; i < profile.Count; i++)
                if (profile[i].y > seatY - 0.02f && profile[i].y < top * 0.80f)
                    seatFrontZ = Mathf.Max(seatFrontZ, profile[i].x);

            Debug.Log(string.Format(
                "SITTING chair: top {0:0.000} m, seat surface {1:0.000} m, "
                + "backrest front face z {2:0.000}",
                top, seatY, backFrontZ));

            // THE CHAIR'S COLLIDERS ARE REMOVED.
            //
            // Leaving them turned the figure measurement SILENTLY into a
            // measurement of the chair: the ray from below hit the chair's rail
            // first and the ray from behind hit the backrest first. The line
            // "pelvis underside 0.281" had never seen the figure at all, and
            // gave THE SAME number even when the figure was raised by 7.4 cm - a
            // measurement that does not change is a sign it is not measuring
            // what you think.
            foreach (MeshCollider mc in s.GetComponentsInChildren<MeshCollider>(true))
                Object.DestroyImmediate(mc);
            Physics.SyncTransforms();

            // --- the seated figure ---
            // EXACTLY as in the game: both the lift and the forward shift are
            // read from RestaurantView. Writing a second number here would mean
            // the measurement was measuring itself rather than the real game.
            GameObject k = Object.Instantiate(characterPrefab, root.transform);
            k.transform.localPosition = new Vector3(
                0f, RestaurantView.SitLiftM, RestaurantView.SitForwardM);
            k.transform.localRotation = Quaternion.identity;
            // The zero of the chair numbers is THE MODEL'S centre; the
            // figure's has to be moved to the same place, or the two numbers are
            // not on the same axis.
            float shift = sb.center.z;

            Figure f = k.GetComponentInChildren<Figure>(true);
            if (f != null) f.Sample(Figure.Pose.Sit, 0.4f);
            {
                Transform knee = null, leg = null;
                foreach (Transform t in k.GetComponentsInChildren<Transform>(true))
                {
                    if (t.name == "knee-left") knee = t;
                    if (t.name == "leg-left") leg = t;
                }
                Debug.Log("SITTING knee count " + (f != null ? f.KneeCount : -1)
                    + "; leg " + (leg == null ? "none" : leg.position.ToString("0.000"))
                    + "; knee " + (knee == null ? "none" : knee.position.ToString("0.000")));
            }

            Bounds? fb = PlacementAudit.PosedBounds(k.transform);
            if (fb != null)
            {
                Bounds b = fb.Value;
                Debug.Log(string.Format(
                    "SITTING figure (seated): y {0:0.000}..{1:0.000}, "
                    + "z {2:0.000}..{3:0.000}, width {4:0.000}",
                    b.min.y, b.max.y, b.min.z - shift, b.max.z - shift,
                    b.size.x));

            }

            // --- THE BODY SURFACES: A RAY, NOT A VERTEX ---
            //
            // Two wrong methods were tried:
            //   1. The bounding box -> its width came out at 1.01 m, and that is
            //      THE ARMS; the bottom of the box can be a leg and its back a
            //      shoulder.
            //   2. Vertex sampling -> IT DOES NOT WORK ON LOW POLY: a box-shaped
            //      body has only eight vertices and none at all in the middle.
            //      In the profile the z = -0.08 and -0.03 slices came out
            //      completely empty, and that meant "there is no VERTEX there",
            //      not "there is no body there".
            //
            // The right way: the posed mesh is taken with BakeMesh (that mesh is
            // OURS, so the package's Read/Write setting is no obstacle), hung on
            // a temporary MeshCollider, and the surfaces are read with rays.
            var temporary = new System.Collections.Generic.List<GameObject>();
            foreach (SkinnedMeshRenderer smr in
                     k.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (smr.sharedMesh == null) continue;
                Mesh posed = new Mesh();
                smr.BakeMesh(posed, false);

                GameObject g = new GameObject("Posed");
                g.transform.position = smr.transform.position;
                g.transform.rotation = smr.transform.rotation;
                g.AddComponent<MeshCollider>().sharedMesh = posed;
                temporary.Add(g);
            }
            Physics.SyncTransforms();

            // THE UNDERSIDE SILHOUETTE: a ray from below going up. Because the
            // legs hang forward, the pelvis is in the REAR slices and the feet
            // in front.
            var below = new System.Text.StringBuilder("SITTING figure underside (z -> y):");
            // THE FOOT: the ray at x=0 passes BETWEEN the two legs and hits the
            // pelvis - the foot height cannot be read from there. The base of
            // the posed box is the right answer.
            float footBottom = fb != null ? fb.Value.min.y : 99f;
            float hipBottom = 99f;
            for (float z = -0.30f; z <= 0.301f; z += 0.03f)
            {
                RaycastHit h;
                if (!Physics.Raycast(new Vector3(0f, -1f, shift + z),
                                     Vector3.up, out h, 4f)) continue;
                below.Append("  " + z.ToString("0.00") + ":" + h.point.y.ToString("0.00"));
                // The pelvis: relative to THE FIGURE'S own place, not to a fixed
                // z. When the figure was shifted forward the window was left
                // over empty air and the measurement returned 99 - a fixed
                // window against a measurement that moves.
                if (Mathf.Abs(z - RestaurantView.SitForwardM) < 0.06f)
                    hipBottom = Mathf.Min(hipBottom, h.point.y);
            }
            Debug.Log(below.ToString());

            // THE BACK: a ray from behind going forward, at body height.
            float backRear = 99f;
            for (float y = seatY + 0.06f; y <= seatY + 0.34f; y += 0.02f)
            {
                RaycastHit h;
                if (Physics.Raycast(new Vector3(0f, y, shift - 1f),
                                    Vector3.forward, out h, 2f))
                    backRear = Mathf.Min(backRear, h.point.z - shift);
            }

            // THE UNDERSIDE OF THE LEG: is it ON TOP of the cushion?
            //
            // The legs stretch out over the cushion (that is the clip's pose),
            // so the question is not horizontal but VERTICAL: across the z range
            // above the cushion, does the lowest point of the leg drop below the
            // cushion surface? The ray runs from x = +-0.06: x = 0 passes
            // BETWEEN the two legs.
            float legBottom = 99f;
            for (float x = -0.07f; x <= 0.071f; x += 0.14f)
                for (float z = -0.05f; z <= seatFrontZ + 0.001f; z += 0.02f)
                {
                    RaycastHit h;
                    if (Physics.Raycast(new Vector3(x, -1f, shift + z),
                                        Vector3.up, out h, 4f))
                        legBottom = Mathf.Min(legBottom, h.point.y);
                }

            // THE FRONT OF THE CHEST: whether it runs into the table. Shifting
            // the figure forward ends the backrest problem, but the table top
            // can then cut through the body; the two compete on the same axis.
            // A FRONT PROFILE, against height. A single number was not enough -
            // because the band started at knee height the frontmost thing was
            // THE KNEE, and it read as "the chest goes into the table".
            var front = new System.Text.StringBuilder("SITTING figure front (y -> z):");
            float chestFront = -99f;
            for (float y = 0.10f; y <= 1.00f; y += 0.05f)
            {
                RaycastHit h;
                if (!Physics.Raycast(new Vector3(0f, y, shift + 1f),
                                     Vector3.back, out h, 2f)) continue;
                float z = h.point.z - shift;
                front.Append("  " + y.ToString("0.00") + ":" + z.ToString("0.00"));
                // THE TABLE TOP'S OWN BAND ONLY. Saying "above the table top"
                // amounted to measuring THE HEAD at y=0.90 - the head is far
                // above the table and can lean forward freely.
                if (y >= TableTop - 0.06f && y <= TableTop + 0.02f)
                    chestFront = Mathf.Max(chestFront, z);
            }
            Debug.Log(front.ToString());

            for (int i = 0; i < temporary.Count; i++) Object.DestroyImmediate(temporary[i]);

            if (hipBottom < 90f)
                Debug.Log(string.Format(
                    "SITTING -> the underside of the hip {0:0.000} m, the seat {1:0.000} m "
                    + "=> {2:0.000} m (negative: sunk in, positive: in mid-air)",
                    hipBottom, seatY, hipBottom - seatY));
            if (footBottom < 90f)
                Debug.Log(string.Format(
                    "SITTING -> the feet are {0:0.000} m off the floor", footBottom));
            if (backRear < 90f)
                Debug.Log(string.Format(
                    "SITTING -> the back of the body z {0:0.000}, the front of the backrest z {1:0.000} "
                    + "=> {2:0.000} m INSIDE it (negative: in front of it, healthy)",
                    backRear, backFrontZ, backFrontZ - backRear));

            if (chestFront > -90f)
            {
                // How far the table top reaches out from the centre, and how
                // close the figure's chest comes.
                float tableHalf = 0.41f;
                float clearance = RestaurantView.SeatRadiusUsedM - chestFront - tableHalf;
                Debug.Log(string.Format(
                    "SITTING -> the front of the chest z {0:0.000} (the top is at {1:0.00} m); "
                    + "{2:0.000} m to the table edge (negative: the top cuts the body)",
                    chestFront, TableTop, clearance));
            }

            if (legBottom < 90f)
                Debug.Log(string.Format(
                    "SITTING -> the underside of the leg {0:0.000} m, the cushion surface {1:0.000} m "
                    + "=> {2:0.000} m of margin (negative: the cushion passes through the leg)",
                    legBottom, seatY, legBottom - seatY));

            // THE VERDICT: are both numbers right? So that it can be seen at a
            // glance instead of running the tool and reading the lines one by
            // one.
            // THE YARDSTICK: THE SURFACE THAT TOUCHES THE CUSHION IS THE
            // UNDERSIDE OF THE THIGHS.
            //
            // The MIDDLE of the pelvis used to be sat on the cushion and the
            // number was green, while the thighs passed 9.4 cm through the
            // cushion: on this model the underside of the middle of the pelvis
            // is at hip-bone height while the thighs sit below it. Whichever
            // surface makes contact is the one to measure. The middle of the
            // pelvis is now only checked for "it must not drop BELOW the
            // cushion".
            bool pelvisOk = hipBottom < 90f && hipBottom >= seatY - 0.01f;
            // The back: it has to stay IN FRONT OF the backrest. Because the
            // table and the backrest compete on the same axis the margin is
            // small - the rule is "it must not go inside it".
            bool backOk = backRear < 90f && backRear >= backFrontZ;
            bool legOk = legBottom < 90f
                         && Mathf.Abs(legBottom - seatY) < 0.02f;
            Debug.Log((pelvisOk && backOk && legOk
                       ? "SITTING VERDICT OK" : "SITTING VERDICT BROKEN")
                      + string.Format(
                          ": pelvis deviation {0:0.000} m, back margin {1:0.000} m, "
                          + "leg margin {2:0.000} m",
                          hipBottom - seatY, backRear - backFrontZ,
                          legBottom - seatY));

            // THE LEG GEOMETRY: can it bend at the knee?
            //
            // Adding a bone is not enough on its own - without an INTERMEDIATE
            // RING OF VERTICES along the length of the leg box the new bone does
            // not bend anything, it only skews the box. This counts how many
            // distinct y levels have vertices along the leg's length.
            {
                GameObject standing = Object.Instantiate(characterPrefab, root.transform);
                standing.transform.localPosition = new Vector3(0f, 0f, -1.5f);
                Figure af = standing.GetComponentInChildren<Figure>(true);
                if (af != null) af.Sample(Figure.Pose.Idle, 0f);

                var levels = new System.Collections.Generic.List<float>();
                int total = 0;
                foreach (SkinnedMeshRenderer smr in
                         standing.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    if (smr.sharedMesh == null) continue;
                    Mesh mp = new Mesh();
                    smr.BakeMesh(mp, false);
                    Vector3[] vv = mp.vertices;
                    total += vv.Length;
                    for (int i = 0; i < vv.Length; i++)
                    {
                        // The leg region: below the body, out to the sides.
                        if (vv[i].y > 0.30f || Mathf.Abs(vv[i].x) < 0.02f) continue;
                        bool isNew = true;
                        for (int j = 0; j < levels.Count; j++)
                            if (Mathf.Abs(levels[j] - vv[i].y) < 0.005f) { isNew = false; break; }
                        if (isNew) levels.Add(vv[i].y);
                    }
                    Object.DestroyImmediate(mp);
                }
                levels.Sort();
                var legLine = new System.Text.StringBuilder(
                    "SITTING leg vertex levels (" + total + " vertices, model):");
                for (int i = 0; i < levels.Count; i++)
                    legLine.Append(" " + levels[i].ToString("0.000"));
                Debug.Log(legLine.ToString());
                Object.DestroyImmediate(standing);
            }

            var bones = new System.Text.StringBuilder("SITTING bones:");
            foreach (Transform t in k.GetComponentsInChildren<Transform>(true))
                bones.Append(" " + t.name);
            Debug.Log(bones.ToString());

            Transform hip = null;
            foreach (Transform t in k.GetComponentsInChildren<Transform>(true))
            {
                string name = t.name.ToLowerInvariant();
                if (name.Contains("hip") || name.Contains("pelvis")
                    || name == "torso" || name == "root")
                {
                    Debug.Log("SITTING bone " + t.name + ": y "
                              + t.position.y.ToString("0.000")
                              + ", z " + (t.position.z - shift).ToString("0.000"));
                    if (hip == null && (name.Contains("hip") || name.Contains("pelvis")))
                        hip = t;
                }
            }
            if (hip != null)
                Debug.Log(string.Format(
                    "SITTING -> the hip is {0:0.000} m ABOVE the seat "
                    + "(0 means fully seated)", hip.position.y - seatY));

            Object.DestroyImmediate(root);
        }

        /// <summary>The height of the table top (the ArtPrefabs target).</summary>
        private const float TableTop = 0.58f;

        private static GameObject Load(string rel)
        {
            return AssetDatabase.LoadAssetAtPath<GameObject>(
                PrefabDir + "/" + rel + ".prefab");
        }

        private static void Slab(Transform parent, Vector3 pos, Vector3 size, Color c)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = size;
            Object.DestroyImmediate(go.GetComponent<Collider>());

            Shader sh = Shader.Find("Universal Render Pipeline/Unlit");
            if (sh == null) sh = Shader.Find("Universal Render Pipeline/Lit");
            Material m = new Material(sh);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            go.GetComponent<Renderer>().sharedMaterial = m;
        }
    }
}
