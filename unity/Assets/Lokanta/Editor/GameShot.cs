using System.IO;
using Lokanta.Content;
using Lokanta.Core.Content;
using Lokanta.Core.Economy;
using Lokanta.Core.Sim;
using Lokanta.Game;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;

namespace Lokanta.EditorTools
{
    /// <summary>
    /// Photographs the hall - by running the REAL view code.
    ///
    /// The point is to be able to look at it: a floor plan can be right in the
    /// numbers and still read wrongly. Three layouts were thrown out on this
    /// project in exactly that way (RoomLayout.cs), and the camera's framing
    /// bug was found the same way (docs/34 22).
    ///
    /// Play mode is not used: it does not run reliably in batch mode and it
    /// hangs. Instead a real Simulation is built and handed to
    /// RestaurantView.Preview; what is drawn is the very code from the game.
    ///
    ///   Unity.exe -batchmode -quit -projectPath ...
    ///     -executeMethod Lokanta.EditorTools.GameShot.Capture
    /// </summary>
    public static class GameShot
    {
        [MenuItem("Lokanta/Take a hall screenshot")]
        public static void Capture()
        {
            EditorSceneManager.OpenScene("Assets/Lokanta/Game.unity");

            foreach (string cuisine in new[] { "turk", "fastfood" })
            foreach (var tier in new[]
                     { new { Name = "opening", Tables = 4 },
                       new { Name = "overview", Tables = 99 } })
            {
                Simulation sim = Build(cuisine, tier.Tables, out ContentSet content);
                if (sim == null) continue;

                RestaurantView view = Object.FindFirstObjectByType<RestaurantView>();
                if (view == null) { Debug.LogError("No RestaurantView"); return; }

                view.Preview = sim;
                view.PreviewPoses = true;
                // THE IDENTITY CARRIES INTO THE SCREENSHOT TOO: the decoration
                // takes its colour from the cuisine and the tool has to show
                // exactly what the game will show.
                view.PreviewCuisine = cuisine;
                // The self-service flag is not in the palette but IN THE
                // CONTENT; if the tool does not pass it, the frame differs from
                // what the game shows.
                view.PreviewContent = content;
                Invoke(view, "Awake");
                view.Rebuild();

                // THE DAYLIGHT IS APPLIED FIRST.
                //
                // Otherwise every screenshot comes out as though the daylight
                // had never run: a black background and shadows at the default
                // angle. The tool has to show what the game will show - the
                // middle of service (0.35) is the default view.
                {
                    DayLight d0 = Object.FindFirstObjectByType<DayLight>();
                    if (d0 != null)
                        d0.Apply(Lokanta.Core.Sim.DayPhase.Service, 0.35f);
                }
                Invoke(view, "Update");

                Audit(view);
                // THE CROWD, COUNTED. The recolouring has no symptom when it
                // fails - a crowd in the pack's own clothes is still a crowd -
                // so this tool says the number next to the picture it just took.
                UnityEngine.Debug.Log("  MEASURED crowd (" + cuisine + "): "
                                      + view.CrowdDressed + " figures in the "
                                      + "cuisine's clothes, " + view.CrowdUndressed
                                      + " in the pack's own");
                UnityEngine.Debug.Log("  DIAGNOSIS tables: eating " + view.EatingTables);

                // THE SCENE BUDGET AT FULL EXPANSION (docs/19 B5).
                //
                // The tour measures the same two numbers but AT FOUR TABLES:
                // the tour plays sixty days and never expands. To see the
                // ceiling the measurement has to exist here as well - this tool
                // builds the scene asking for 99 tables, that is, with every
                // room open.
                {
                    int renderers = 0, blocked = 0;
                    long triangles = 0;
                    foreach (Renderer rr in Object.FindObjectsByType<Renderer>(
                                 FindObjectsSortMode.None))
                    {
                        if (!rr.enabled || !rr.gameObject.activeInHierarchy) continue;
                        renderers++;

                        // THE ONES THAT FALL OUTSIDE BATCHING.
                        //
                        // A renderer with a MaterialPropertyBlock written to it
                        // cannot enter SRP batching - the project itself wrote
                        // this down repeatedly and designed the floor and the
                        // room light around it. But three newer systems
                        // (badges, clothing, the stove top) go on paying the
                        // same price and WERE NEVER MEASURED AT ALL. And
                        // because they were not measured, nobody noticed.
                        if (rr.HasPropertyBlock()) blocked++;

                        Mesh mm = null;
                        MeshFilter mf2 = rr.GetComponent<MeshFilter>();
                        if (mf2 != null) mm = mf2.sharedMesh;
                        SkinnedMeshRenderer sm = rr as SkinnedMeshRenderer;
                        if (sm != null) mm = sm.sharedMesh;
                        if (mm == null) continue;
                        for (int i = 0; i < mm.subMeshCount; i++)
                            triangles += (long)(mm.GetIndexCount(i) / 3);
                    }
                    UnityEngine.Debug.Log("  MEASURED scene budget (" + cuisine + ", "
                                          + tier.Name + ", " + sim.TableCount
                                          + " tables): " + renderers + " renderers, "
                                          + triangles + " triangles, "
                                          + blocked + " outside batching");

                    // THE THRESHOLD IS HERE - NOT IN THE TOUR.
                    //
                    // The tour's threshold (400 / 80,000) is measured on the
                    // FOUR-TABLE base scene and the tour never expands: if a
                    // fourteen-table scene went over budget nothing would warn.
                    // This place builds the scene asking for 99 tables, that
                    // is, it measures THE CEILING - and the thresholds belong
                    // where the ceiling is.
                    //
                    // The numbers do not come from docs/19 B5 but FROM
                    // MEASUREMENT: today's ceiling is ~270 renderers / ~45,000
                    // triangles. The thresholds leave room above that so small
                    // additions do not break them, while a silent bloat is
                    // still caught.
                    if (renderers > 360)
                        UnityEngine.Debug.LogError("PROBLEMS: the renderer ceiling was passed ("
                                                   + renderers + " > 360, " + cuisine + " "
                                                   + tier.Name + ")");
                    if (triangles > 70000)
                        UnityEngine.Debug.LogError("PROBLEMS: the triangle ceiling was passed ("
                                                   + triangles + " > 70000, " + cuisine + " "
                                                   + tier.Name + ")");
                    if (blocked > 220)
                        UnityEngine.Debug.LogError("PROBLEMS: too many renderers outside "
                                                   + "batching (" + blocked + " > 220, "
                                                   + cuisine + " " + tier.Name + ")");
                }

                // The frame comes from THE SAME source as the game's: the open
                // rooms. It used to use PlotBounds and the screenshot was
                // therefore wider than what the player sees - the right-hand
                // third of the screen was empty plot and the screenshot did not
                // show it that way.
                Shoot("hall_" + cuisine + "_" + tier.Name + ".png",
                      CameraFit.OpenBounds(sim.TableCount), 1280, 576);

                // And the view zoomed in ON A ROOM: at this scale the touch
                // target is a table set (docs/31), so this frame has to be seen
                // too. The room frame does not depend on the table count, so
                // once is enough.
                if (tier.Tables > 4)
                {
                    // THE ROOM NAMES ARE THE ONES RoomPlan.cs PRODUCES. They
                    // are looked up by exact string, so they stay as that file
                    // spells them.
                    int hall = FindRoom("Hall1");
                    Shoot("hall_" + cuisine + "_room.png",
                          CameraFit.RoomBounds(hall), 1280, 576);

                    // THE KITCHEN CLOSE-UP: the clothing (the cook's hat, the
                    // apron) and the service counter can only be seen at this
                    // scale.
                    int kitchen = FindRoom("Kitchen");
                    Shoot("kitchen_" + cuisine + ".png",
                          CameraFit.RoomBounds(kitchen), 1100, 700);

                    // THE HOT LINE, CLOSER AND FROM THE SIDE.
                    //
                    // The room shot is taken at the GAME's angle, which is the
                    // right test for "can the player read it" and the wrong one
                    // for "is the model correct": at 34 degrees a stone oven's
                    // dome and a doner spit's cone are forty pixels each. The
                    // equipment built in docs/59 needs a frame where a wrong
                    // model is visible as a wrong model.
                    {
                        RoomPlan.Room kr = RoomPlan.Rooms[kitchen];
                        Bounds line = new Bounds(
                            new Vector3(kr.CenterX, 0.85f, kr.Z0 + kr.D - 0.9f),
                            new Vector3(kr.W + 0.4f, 2.4f, 2.0f));
                        Shoot("kitchen_" + cuisine + "_line.png", line, 1400, 620,
                              18f, 12f);
                    }

                    // ONE TABLE, CLOSE UP.
                    //
                    // The question "do the models look like they overlap" can
                    // only be answered at this scale: in the overview the
                    // 34-degree view already shows everything on top of
                    // everything else, that is, it draws real intersection and
                    // innocent depth in the same way.
                    Transform table = FindTable(view);
                    if (table != null)
                    {
                        Shoot("hall_" + cuisine + "_table.png",
                              new Bounds(table.position + new Vector3(0f, 0.6f, 0f),
                                         new Vector3(2.6f, 1.6f, 2.6f)),
                              1280, 720);

                        // THE CLOSEST: the limit the player can reach with two
                        // fingers (CameraRig 0.45). Nothing closer than this is
                        // ever seen, so this is the hardest scale at which the
                        // models still have to look right.
                        Shoot("hall_" + cuisine + "_close.png",
                              new Bounds(table.position + new Vector3(0f, 0.45f, 0f),
                                         new Vector3(1.25f, 0.9f, 1.25f)),
                              1280, 720);
                    }
                }

                // THE EVENING SCREENSHOT.
                //
                // The daylight is driven from GameApp and that does not run in
                // editor mode; what changes across the day can only be seen
                // here. Morning and evening are taken from THE SAME frame so
                // that the only difference comes from the light.
                DayLight day = Object.FindFirstObjectByType<DayLight>();
                if (day != null && tier.Name == "overview")
                {
                    Bounds frame = CameraFit.OpenBounds(tier.Tables);

                    day.Apply(Lokanta.Core.Sim.DayPhase.Morning, 0f);
                    Shoot("hall_" + cuisine + "_morning.png", frame, 1280, 560);

                    day.Apply(Lokanta.Core.Sim.DayPhase.Service, 0.5f);
                    Shoot("hall_" + cuisine + "_noon.png", frame, 1280, 560);

                    day.Apply(Lokanta.Core.Sim.DayPhase.Evening, 1f);
                    Shoot("hall_" + cuisine + "_evening.png", frame, 1280, 560);

                    // THE STREET LAMP CLOSE UP, BY NIGHT AND BY DAY.
                    //
                    // In the overview frame the lamp is 40 pixels: whether the
                    // model looks right cannot be told at that scale. It is
                    // taken twice because there are two separate questions - by
                    // day the MODEL (does the lantern read, where does the arm
                    // point), by night the LIGHT (what the beam, the halo and
                    // the pool do together).
                    Vector3 post = new Vector3(
                        CameraFit.OpenBounds(tier.Tables).center.x,
                        1.05f, RestaurantView.LampPostZ + 0.35f);
                    Bounds close = new Bounds(post, new Vector3(3.2f, 2.4f, 3.2f));

                    // THE CAMERA ANGLE TRIAL.
                    //
                    // The user asked for "an angle like the reference", and
                    // that choice cannot be made by eye: the yaw changes both
                    // how the frame is filled and THE TOUCH TARGET (docs/31:
                    // a -12 degree yaw had dropped the floor from 71 dp to
                    // 48 dp). First we LOOK.
                    if (cuisine == "turk")
                    {
                        Bounds frame2 = CameraFit.OpenBounds(tier.Tables);
                        float[,] angles = { {34f, 0f}, {34f, 20f}, {34f, 30f},
                                            {40f, 30f}, {30f, 45f}, {45f, 45f} };
                        for (int a = 0; a < angles.GetLength(0); a++)
                            Shoot("angle_" + (int)angles[a, 0] + "_"
                                  + (int)angles[a, 1] + ".png", frame2, 873, 393,
                                  angles[a, 0], angles[a, 1]);
                    }

                    day.Apply(Lokanta.Core.Sim.DayPhase.Service, 0.35f);
                    Shoot("lamp_" + cuisine + "_day.png", close, 900, 700);

                    day.Apply(Lokanta.Core.Sim.DayPhase.Evening, 1f);
                    Shoot("lamp_" + cuisine + "_night.png", close, 900, 700);

                    day.Apply(Lokanta.Core.Sim.DayPhase.Service, 0.35f);
                }

                // THE WASHING-UP AREA, CLOSE UP.
                //
                // The sink is only worth seeing while somebody is washing, and
                // in editor mode nobody washes: when the scene is built the
                // staff are idle. It is staged BY HAND here - a figure is put
                // at the sink and posed into the washing stance, and the tap
                // and the sponge are turned on.
                //
                // Why it is needed: the answer to "is the water running, is
                // there a sponge" is not in a number but in the picture. Here,
                // measuring means LOOKING.
                if (tier.Name == "overview")
                {
                    view.PreviewWash();
                    Vector3 ls = view.WashSpot;
                    Shoot("dishwashing_" + cuisine + ".png",
                          new Bounds(ls + new Vector3(0f, 0.7f, 0.55f),
                                     new Vector3(2.6f, 1.6f, 2.6f)),
                          1280, 720);
                }

                view.Preview = null;
                view.PreviewPoses = false;
                view.Clear();
            }

            Debug.Log("Screenshots taken: render/");
        }

        /// <summary>
        /// Writes down the REAL sizes in the scene as it was built.
        ///
        /// The prefab's size may be right and the object in the scene may still
        /// come out the wrong height: the scale of every parent in between is
        /// multiplied in, and sampling the animation can touch the root
        /// transform. This puts the two measurements side by side and makes the
        /// difference visible.
        /// </summary>
        private static void Audit(RestaurantView view)
        {
            int n = 0;
            foreach (Figure f in view.GetComponentsInChildren<Figure>(true))
            {
                if (n++ > 3) break;
                Transform t = f.transform;
                Debug.Log(string.Format(
                    "  figure pose={0,-5} parent {1,-22} scale {2:0.000}"
                    + "  y={3:0.00}  {4}  {5}",
                    f.Current, t.parent == null ? "-" : t.parent.name,
                    t.lossyScale.y, t.position.y, Box(t), BindBox(t)));
            }

            // THE CROWDING MEASUREMENT. The user's sentence: "the characters
            // look a bit big, they make the middle look very cramped." The only
            // way to turn that into a number: how much of a table set's
            // footprint the people sitting at it take up.
            foreach (Figure f0 in view.GetComponentsInChildren<Figure>(true))
            {
                Bounds? bb = null;
                foreach (SkinnedMeshRenderer smr in
                         f0.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    if (smr.sharedMesh == null) continue;
                    Bounds mb = smr.sharedMesh.bounds;
                    Vector3 sc = smr.transform.lossyScale;
                    Bounds w = new Bounds(Vector3.Scale(mb.center, sc),
                                          Vector3.Scale(mb.size, sc));
                    if (bb == null) bb = w; else { Bounds a = bb.Value; a.Encapsulate(w); bb = a; }
                }
                if (bb == null) break;

                float width = bb.Value.size.x;
                Debug.Log(string.Format(
                    "  CROWDING figure width {0:0.00} m | table pitch {1:0.00} m"
                    + " | two figures {2:0.00} m = {3:0}% of the pitch"
                    + " | chair height 0.92 m, figure height {4:0.00} m,"
                    + " ratio {5:0.00} (a real person is 1.85)",
                    width, RoomPlan.CellX, width * 2f, width * 2f / RoomPlan.CellX * 100f,
                    bb.Value.size.y, bb.Value.size.y / 0.92f));
                break;
            }

            foreach (Transform t in view.GetComponentsInChildren<Transform>(true))
            {
                if (!t.name.StartsWith("chairCushion")) continue;
                Debug.Log(string.Format(
                    "  chair parent {0,-22} scale {1:0.000}  y={2:0.00}  {3}",
                    t.parent == null ? "-" : t.parent.name,
                    t.lossyScale.y, t.position.y, Box(t)));
                break;
            }
        }

        private static bool HasDirectionalLight()
        {
            foreach (Light l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (l.type == LightType.Directional && l.enabled && l.intensity > 0.01f)
                    return true;
            return false;
        }

        private static string Box(Transform t)
        {
            Renderer[] rs = t.GetComponentsInChildren<Renderer>(true);
            if (rs.Length == 0) return "no renderer";
            Bounds b = rs[0].bounds;
            for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
            return string.Format("box {0:0.00} x {1:0.00} x {2:0.00} base={3:0.00}",
                                 b.size.x, b.size.y, b.size.z, b.min.y);
        }

        /// <summary>
        /// The real size IN THE BIND POSE.
        ///
        /// On a skinned mesh Renderer.bounds LIES: Unity derives it from the
        /// root bone and does not update it as the pose changes. In the first
        /// measurement a seated figure came out 1.68 m tall and 1.66 m wide -
        /// both impossible. sharedMesh.bounds, on the other hand, is the model's
        /// own box; multiplied by the world scale it gives the real height.
        /// </summary>
        private static string BindBox(Transform t)
        {
            Bounds? acc = null;
            foreach (SkinnedMeshRenderer smr in
                     t.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (smr.sharedMesh == null) continue;
                Bounds mb = smr.sharedMesh.bounds;
                Vector3 sc = smr.transform.lossyScale;
                Bounds w = new Bounds(
                    Vector3.Scale(mb.center, sc), Vector3.Scale(mb.size, sc));
                if (acc == null) acc = w; else { Bounds a = acc.Value; a.Encapsulate(w); acc = a; }
            }
            if (acc == null) return "no skinned mesh";
            Vector3 z = acc.Value.size;
            return string.Format("BIND width {0:0.00} height {1:0.00} depth {2:0.00} m",
                                 z.x, z.y, z.z);
        }

        // =====================================================================
        /// <param name="maxTables">
        /// Past this number no further expansion is attempted. It exists for
        /// two screenshots: the OPENING (four tables) and the GROWN
        /// restaurant. Because the camera frame is built from the open rooms
        /// the two do not give the same frame, and both have to be seen.
        /// </param>
        private static Simulation Build(string cuisine, int maxTables,
                                        out ContentSet content)
        {
            content = null;
            try
            {
                IContentSource src = new ResourcesContentSource();
                Loc.Load(src);
                EconomyConfig economy = ContentLoader.LoadEconomy(src);
                content = ContentSetLoader.Load(src, cuisine);

                TimingConfig timing = content.SlotDurationsBp != null
                    ? TimingConfig.Default()
                        .WithSlotDurations(content.SlotDurationsBp)
                        .WithEatMs(content.EatMs)
                    : TimingConfig.Default();

                Simulation sim = new Simulation(economy, content, timing, 20260911UL);

                // Play a few days and expand the hall: a picture of an empty
                // four-table shop does not describe the game.
                for (int day = 0; day < 12; day++)
                {
                    for (int i = 0; i < sim.IngredientCount; i++)
                    {
                        int need = sim.RecommendedRestock(i);
                        if (need > 0)
                            sim.Apply(new Command(sim.TickIndex,
                                CommandKind.OrderIngredient, i, need));
                    }
                    // Growth is ATTEMPTED, NOT pinned to a day: in the first
                    // version it was told to expand on day 3, the cash did not
                    // stretch that day and the command dropped silently - the
                    // screenshot was still four tables on day twelve.
                    if (sim.Cooks < 2)
                        sim.Apply(new Command(sim.TickIndex, CommandKind.Hire, 0, 0));
                    if (sim.HallStaff < 3)
                        sim.Apply(new Command(sim.TickIndex, CommandKind.Hire, 1, 0));

                    int tier = 0;
                    while (tier < sim.TierCount
                           && sim.TablesAtTier(tier) <= sim.TableCount) tier++;
                    if (tier < sim.TierCount
                        && sim.TablesAtTier(tier) <= maxTables
                        && sim.Cash > sim.UpgradeCostFor(tier) * 2)
                        sim.Apply(new Command(sim.TickIndex, CommandKind.Expand, tier));
                    sim.Apply(new Command(sim.TickIndex, CommandKind.OpenService));

                    // We stop IN THE MIDDLE of service, so the hall is busy.
                    int stop = timing.ServiceTicks / 2;
                    for (int t = 0; t < stop; t++)
                    {
                        sim.Tick();
                        if (sim.ServiceComplete) break;
                    }
                    if (day == 11) break;             // on the last day leave service open

                    for (int t = 0; t < timing.ServiceTicks; t++)
                    {
                        sim.Tick();
                        if (sim.ServiceComplete) break;
                    }
                    sim.Apply(new Command(sim.TickIndex, CommandKind.CloseDay));
                    sim.AdvanceToNextDay();
                }

                Debug.Log(string.Format(
                    "{0}: day {1}, {2} tables, {3} occupied, {4} staff, reputation {5:0.0}",
                    cuisine, sim.Day, sim.TableCount, sim.OccupiedTables,
                    sim.Cooks + sim.HallStaff, sim.ReputationCenti / 100f));
                return sim;
            }
            catch (System.Exception e)
            {
                Debug.LogError("The preview could not be built (" + cuisine + "): " + e.Message);
                return null;
            }
        }

        /// <summary>So that the placement audit can build the same preview.</summary>
        internal static Simulation BuildFor(string cuisine)
        {
            return Build(cuisine, 99, out _);
        }

        /// <summary>For the same reason: firing the private Awake/Update from outside.</summary>
        internal static void Kick(object target, string method)
        {
            Invoke(target, method);
        }

        /// <summary>
        /// The first table with a guest at it; failing that, the first table.
        ///
        /// The "Table_" prefix is the name RestaurantView gives the table
        /// holders it creates, so it stays exactly as that file spells it.
        /// </summary>
        private static Transform FindTable(RestaurantView view)
        {
            Transform first = null;
            foreach (Transform t in view.transform)
            {
                if (!t.name.StartsWith("Table_")) continue;
                if (first == null) first = t;
                if (t.GetComponentInChildren<Figure>(true) != null) return t;
            }
            return first;
        }

        private static int FindRoom(string name)
        {
            for (int i = 0; i < RoomPlan.Rooms.Length; i++)
                if (RoomPlan.Rooms[i].Name == name) return i;
            return 0;
        }

        /// <summary>
        /// Calls a MonoBehaviour's private method. In editor mode Awake and
        /// Update do not run; they are fired by hand so that the preview runs
        /// the real code.
        /// </summary>
        private static void Invoke(object target, string method)
        {
            var m = target.GetType().GetMethod(method,
                System.Reflection.BindingFlags.Instance
                | System.Reflection.BindingFlags.NonPublic
                | System.Reflection.BindingFlags.Public);
            m?.Invoke(target, null);
        }

        // =====================================================================
        internal static void Shoot(string name, Bounds target, int width, int height,
                                   float pitch = float.NaN, float yaw = float.NaN)
        {
            bool batcher = SrpBatcher(false);
            try { Draw(name, target, width, height, pitch, yaw); }
            finally { SrpBatcher(batcher); }
        }

        /// <summary>
        /// Turns SRP batching off temporarily and returns its previous state.
        ///
        /// Why: in batch mode a hand-called Camera.Render() DOES NOT FILL the
        /// batcher's per-material constant buffer. Every model was coming out
        /// one single colour - pink on one run, black on the next; that is,
        /// what was being read was leftover data. That the floors came out
        /// right confirms it: their colour comes from a MaterialPropertyBlock,
        /// and that route already disables batching.
        ///
        /// The game itself is not affected (on a real run the buffer is
        /// filled); what is turned off is only the SCREENSHOT path.
        /// </summary>
        private static bool SrpBatcher(bool on)
        {
            RenderPipelineAsset asset = GraphicsSettings.currentRenderPipeline;
            if (asset == null) return true;

            SerializedObject so = new SerializedObject(asset);
            SerializedProperty p = so.FindProperty("m_UseSRPBatcher");
            if (p == null) return true;

            bool was = p.boolValue;
            if (was != on)
            {
                p.boolValue = on;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            return was;
        }

        private static void Draw(string name, Bounds target, int width, int height,
                                 float pitch = float.NaN, float yaw = float.NaN)
        {
            GameObject camGo = new GameObject("ShotCamera");
            Camera cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;

            // THE BACKGROUND COMES FROM THE SCENE'S CAMERA.
            //
            // A fixed colour was written here, and once the daylight was added
            // the tool SILENTLY showed the wrong thing: in the game the sky is
            // blue in the morning and black in the evening, but in the
            // screenshot it stayed the same dark grey at every hour. The tool
            // has to show what the game will show.
            Camera sceneCam = Object.FindFirstObjectByType<Lokanta.Game.CameraRig>()
                           != null
                ? Object.FindFirstObjectByType<Lokanta.Game.CameraRig>()
                        .GetComponent<Camera>()
                : null;
            cam.backgroundColor = sceneCam != null
                ? sceneCam.backgroundColor
                : new Color(0.055f, 0.062f, 0.075f);

            // THE SAME arithmetic as CameraRig: what we see has to be what the
            // player sees. In the first version there were separate numbers and
            // the render showed the restaurant across 38% of the frame.
            cam.fieldOfView = CameraFit.FieldOfView;
            cam.aspect = width / (float)height;

            // THE PREVIEW CAMERA HAS TO ASK FOR THE COLOUR GRADE TOO.
            //
            // This tool builds its OWN camera - the file already carries the
            // scar about the lights, "the preview's job is to SHOW the game,
            // not to flatter it" - and a camera built in code has
            // renderPostProcessing FALSE. So the day the grade was wired
            // (docs/58, the colour grading row) every render in render/ came
            // out ungraded and looked exactly right, because it looked exactly
            // like the day before.
            //
            // It was caught by measuring rather than by looking: the contrast
            // was driven to -100, the frame was rendered again, and the mean
            // colour of the two images was IDENTICAL to a tenth of a level.
            // A grade you cannot see in the picture and cannot see in the
            // numbers is a grade that is not running.
            UniversalAdditionalCameraData camData =
                camGo.AddComponent<UniversalAdditionalCameraData>();
            camData.renderPostProcessing = true;
            camData.antialiasing = AntialiasingMode.None;

            // If no angle is given, THE GAME's angle. When one is given it is
            // only for inspection shots: the question "is it sitting down"
            // cannot be answered from above, you have to look from the side.
            bool custom = !float.IsNaN(pitch) || !float.IsNaN(yaw);
            Quaternion rot = custom
                ? Quaternion.Euler(float.IsNaN(pitch) ? CameraFit.Pitch : pitch,
                                   float.IsNaN(yaw) ? CameraFit.Yaw : yaw, 0f)
                : CameraFit.Rotation;
            camGo.transform.rotation = rot;

            if (custom)
            {
                float tanV = Mathf.Tan(CameraFit.FieldOfView * Mathf.Deg2Rad * 0.5f);
                float spread = Mathf.Max(target.extents.y,
                                         target.extents.x / cam.aspect);
                float d = spread / tanV + target.extents.z + 0.4f;
                camGo.transform.position = target.center - rot * Vector3.forward * d;
            }
            else
            {
                camGo.transform.position = CameraFit.Position(target, cam.aspect);
            }

            // IF THE SCENE ALREADY HAS A LIGHT, NO NEW ONE IS ADDED.
            //
            // One used to be added unconditionally and the result was a frame
            // twice as bright. That is, lighting that looked right in the
            // preview was half as strong in the game, and in the first desktop
            // build the hall came out dark. The preview's job is to SHOW the
            // game, not to flatter it.
            GameObject sun = null, fillGo = null;
            if (!HasDirectionalLight())
            {
                sun = new GameObject("Sun");
                Light key = sun.AddComponent<Light>();
                key.type = LightType.Directional;
                key.intensity = 1.45f;
                key.color = new Color(1f, 0.96f, 0.90f);
                sun.transform.rotation = Quaternion.Euler(52f, 208f, 0f);

                fillGo = new GameObject("Fill");
                Light fill = fillGo.AddComponent<Light>();
                fill.type = LightType.Directional;
                fill.intensity = 0.55f;
                fill.color = new Color(0.72f, 0.78f, 0.92f);
                fillGo.transform.rotation = Quaternion.Euler(28f, 40f, 0f);
            }

            RenderTexture rt = new RenderTexture(width, height, 24) { antiAliasing = 4 };
            cam.targetTexture = rt;
            cam.Render();

            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = rt;
            Texture2D shot = new Texture2D(width, height, TextureFormat.RGB24, false);
            shot.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            shot.Apply();
            RenderTexture.active = previous;

            string dir = Path.Combine(
                Path.GetDirectoryName(Path.GetDirectoryName(Application.dataPath)), "render");
            Directory.CreateDirectory(dir);
            File.WriteAllBytes(Path.Combine(dir, name), shot.EncodeToPNG());

            cam.targetTexture = null;
            Object.DestroyImmediate(camGo);
            if (sun != null) Object.DestroyImmediate(sun);
            if (fillGo != null) Object.DestroyImmediate(fillGo);
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(shot);
        }
    }
}
