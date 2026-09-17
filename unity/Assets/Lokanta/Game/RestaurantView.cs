using System.Collections.Generic;
using Lokanta.Core.Sim;
using UnityEngine;

namespace Lokanta.Game
{
    /// <summary>
    /// Builds the hall and updates it every frame by READING the
    /// simulation. It writes nothing back to the simulation.
    ///
    /// The models are bound in the scene (BuildGameScene finds them with
    /// AssetDatabase) rather than loaded from Resources: everything put
    /// into Resources goes into the build, whereas most of the models are
    /// used in a single scene.
    ///
    /// The guest figures come FROM A POOL. On a busy day a few tables fill
    /// per second; an Instantiate/Destroy every time triggers the garbage
    /// collector on mobile and drops a frame.
    /// </summary>
    public sealed partial class RestaurantView : MonoBehaviour
    {
        public GameApp App;

        /// <summary>
        /// A simulation handed in directly, for the preview. Used when there
        /// is no App.
        ///
        /// Why it exists: seeing the scene with my own eyes meant RUNNING the
        /// game, and play mode is not reliable in batch mode. This field lets
        /// the editor tool run the REAL view code - writing a separate preview
        /// path would have meant that what I saw was not what the player sees.
        /// </summary>
        [System.NonSerialized] public Simulation Preview;

        /// <summary>
        /// For sampling the poses from the clip for a SINGLE FRAME. In editor
        /// mode the Animator does not run and every figure stays in the bind
        /// pose - with its arms out to the sides. Off in the game.
        /// </summary>
        [System.NonSerialized] public bool PreviewPoses;

        /// <summary>Which second of the clip the preview samples.</summary>
        [System.NonSerialized] public float PreviewTime = 0.4f;

        private Simulation Source { get { return App != null && App.Sim != null ? App.Sim : Preview; } }

        [Header("Mobilya")]
        public GameObject TablePrefab;
        public GameObject ChairPrefab;
        public GameObject StovePrefab;
        public GameObject FridgePrefab;
        public GameObject CounterPrefab;
        public GameObject ShelfPrefab;
        public GameObject SinkPrefab;
        public GameObject PlantPrefab;

        /// <summary>The plate the waiter carries.</summary>
        public GameObject PlatePrefab;

        // =====================================================================
        // THE TRANSPARENT MATERIALS COME FROM THE SCENE, THEY ARE NOT MADE
        // HERE.
        //
        // A transparent material built at runtime looks right IN THE EDITOR
        // but is drawn opaque IN THE REAL BUILD: URP does not put the
        // transparent pass's shader variant into the build if no asset refers
        // to it. The walls, the door leaves and the oven glass came out on
        // the device as flat white slabs.
        //
        // The same class of bug happened before with URP/Unlit (the badges).
        // The materials are now .mat ASSETS produced by ArtPrefabs, and
        // BuildGameScene binds them here.

        /// <summary>The room walls (transparent). ArtPrefabs produces it.</summary>
        public Material WallMaterial;

        /// <summary>The door leaf (transparent). ArtPrefabs produces it.</summary>
        public Material DoorMaterial;

        /// <summary>The oven glass (transparent). ArtPrefabs produces it.</summary>
        public Material GlassMaterial;

        /// <summary>The street lamp's pool of light (unlit, additive).</summary>
        public Material GlowMaterial;

        /// <summary>
        /// The additive material for the restaurant's inside ceiling lights.
        ///
        /// A SEPARATE asset from the street's, not the same material coloured
        /// with a property block: writing a property block throws those
        /// renderers out of SRP batching, and a fully expanded restaurant has
        /// 25 ceiling lights.
        /// </summary>
        public Material CeilingGlowMaterial;

        /// <summary>The transparent material of running water. It has to be an asset.</summary>
        public Material WaterMaterial;

        /// <summary>The colour of the threshold mat. Lighter than the floor, and not attention-seeking.</summary>
        public Color MatColor = new Color(0.42f, 0.36f, 0.30f);

        [Header("Insanlar")]
        public GameObject[] CustomerPrefabs;
        public GameObject[] StaffPrefabs;

        /// <summary>
        /// The distance from the chair's centre to the table's centre (m). A
        /// seated figure stands exactly here too: the sitting clip already
        /// places the body on the chair. At first the figure was put at 0.80
        /// m, slightly outside the chair - so that a standing figure would not
        /// cut through it. Once the clip arrived that was no longer needed.
        /// <summary>
        /// The seat's distance from the table's centre. 0.48 was chosen BY
        /// MEASUREMENT.
        ///
        /// Between the two constraints there is only one right interval:
        ///   - Two guests must not merge: 2r >= the figure's width (0.90)
        ///   - The table set must not spill into the NEIGHBOURING set:
        ///     r + width/2 <= 0.925 (the cell is 1.85 m)
        /// Both at once: 0.45 <= r <= 0.475. 0.48 sits on the upper limit, and
        /// the 5 mm of spill is invisible when the neighbouring set's same
        /// side is empty.
        /// </summary>
        /// <summary>
        /// The seat's distance from the table's centre. THE SAME ON ALL FOUR
        /// SIDES.
        ///
        /// Because the table is SQUARE a single number is enough - on a
        /// rectangular table the radius had to change with the axis and the
        /// two sides were never both right (measured from above: the chairs on
        /// the short edge stuck to the table, the ones on the long edge 0.15 m
        /// away).
        ///
        /// 0.58 = 0.41 (half the width) + 0.17 of margin. The upper limit
        /// comes from the neighbouring table: 0.58 + the figure's width/2
        /// (0.32) = 0.90 <= 0.925 (cell 1.85). Empty chairs in Z: 0.58 + 0.15
        /// = 0.73 <= 0.85 (cell 1.70).
        ///
        /// The same number also closes the "the characters are too close to
        /// the table" complaint: the distance to the table's edge goes from
        /// 0.04 to 0.17 m.
        /// </summary>
        private const float SeatRadius = 0.58f;

        /// <summary>So that the scale screenshot uses the same number.</summary>
        public const float SeatRadiusM = SeatRadius;

        /// <summary>
        /// So that the measurement screenshot uses THE SAME calculation as
        /// the game. Writing the same number in two places has drifted apart
        /// silently five times in this project.
        public static Vector3 SeatAt(int k) { return Seat(k); }

        /// <summary>
        /// HOW MANY GUESTS ARE DRAWN ON SCREEN at most. Not four but TWO.
        ///
        /// This is a compromise, and it was taken by measurement. A seated
        /// figure's footprint is 0.90 x 1.01 m; the distance between two
        /// neighbouring seats is r = 0.48 m. To fill four seats the figure's
        /// width or depth would have to come below r, that is 0.48 m - which
        /// means a person 0.66 m tall. The arithmetic comes out the same at
        /// every scale:
        ///
        ///   cell 1.85 x 1.70 m | table diameter 0.88 | figure 0.90 x 1.01
        ///   -> for 4 people the width needed is <= 0.545 m (height ~0.66 m)
        ///
        /// So FOUR PEOPLE DO NOT FIT AT THIS TABLE AT ANY REASONABLE SCALE. In
        /// parties of three or four, two figures are drawn and the others are
        /// not. The information lost is the size of the party; but that was
        /// not being read anyway - four figures turned into a single mass, and
        /// the table's state is written on its badge.
        ///
        /// The simulation IS NOT AFFECTED: the party is still four people and
        /// so is its bill.
        /// </summary>
        private const int VisibleGuests = 2;

        /// <summary>
        /// THE FURNITURE MODELS' LOCAL "FRONT" IS THE OPPOSITE OF THE
        /// CHARACTER'S.
        ///
        /// Measured (Editor/FigureShot, with a red cube at +Z and a blue cube
        /// at -Z, seven pieces of furniture in one frame):
        ///
        ///   character  yaw 0 -> faces  +Z
        ///   furniture  yaw 0 -> front  -Z   (the chair's cushion, the stove's
        ///                                    door, the fridge's handle, the
        ///                                    sink's tap, all of them)
        ///
        /// The code did not know about this difference and, when writing an
        /// angle, thought "like a character": so the stoves faced the wall,
        /// the counters faced outwards and the chairs turned their BACKS to
        /// the table. The user's sentence was "the chairs are the wrong way
        /// round"; the chair was only the most visible example, all the
        /// furniture was 180 degrees out.
        ///
        /// The angles at the call sites are written in the CHARACTER's rule (0
        /// = face +Z) and this constant closes the difference - so no call
        /// site has to remember a 180 of its own.
        /// </summary>
        private const float PropYaw = 180f;
        private const int Seats = 4;

        /// <summary>
        /// The height of a seated figure above the floor (m).
        ///
        /// Not a guess but a measurement (Lokanta > Material diagnosis): the
        /// sitting clip lowers the body within itself, so a figure put down
        /// where it stands sinks into the floor. Lifting it by the same amount
        /// brings its hips up to the height of the seat.
        ///
        /// 0.26 -> 0.334: NO LONGER A GUESS, it is the chair's own surface. It
        /// was measured (Editor/FigureShot -> the SITTING lines): the cushion
        /// surface is at 0.355 m and the bottom of the figure's pelvis at
        /// 0.281 m - so the figure sat 7.4 cm BELOW the cushion and the
        /// cushion passed through its legs. That was the user's sentence.
        ///
        /// 0.316 -> 0.410: measured again after the knee bone was added. The
        /// thigh is now HORIZONTAL and the surface that meets the cushion is
        /// not the middle of the pelvis but the UNDERSIDE OF THE THIGHS - and
        /// that is 0.094 m below the hip bone. The old value sat the middle of
        /// the pelvis on the cushion, so the thighs passed 9.4 cm through it.
        ///
        /// The feet stay ~0.23 m off the floor: this pack's legs are short
        /// relative to the body (hip to foot 0.32 m, 32% of the height; in
        /// reality 52%) and a chair that put the feet on the floor would be
        /// 0.24 m high - a toy chair. The user also said "they can stay in the
        /// air".
        /// </summary>
        private const float SitLift = 0.410f;

        /// <summary>
        /// How far the seated figure slides towards the table (m).
        ///
        /// Sat on the chair's centre, the figure's BACK stayed 0.108 m inside
        /// the backrest - the backrest passed through the body. The chair
        /// stays where it is and only the figure comes forward: 0.108 + 0.022
        /// of margin.
        ///
        /// The upper limit is set by the table, and it is ZERO-SUM: the gap
        /// between the chair and the table is 0.268 m, while a seated figure's
        /// depth from back to chest is 0.363 m. The figure is 0.095 m too
        /// deep; one of them has to intersect.
        ///
        /// THE BACKREST was chosen because it is the one that SHOWS: the top
        /// of the backrest is above the table top (0.55), at 0.636 - a
        /// backrest inside the body is directly visible from a 34 degree view.
        /// The table top overlapping the belly, on the other hand, stays UNDER
        /// the top and reads as "sitting close to the table".
        ///
        /// 0.15: the back of the body is at -0.056, the front of the backrest
        /// at -0.098 - 0.042 of margin. Going further forward pushes the hips
        /// out over the front edge of the cushion; going further back puts the
        /// backrest inside the body.
        /// </summary>
        private const float SitForward = 0.15f;

        /// <summary>
        /// The radius of an OCCUPIED chair. An empty chair is at SeatRadius,
        /// stuck to the table.
        ///
        /// 0.65 = 0.58 + 0.07: the figure stays where it is (0.65 - 0.22 =
        /// 0.43, previously 0.58 - 0.15), only the chair moves back. The upper
        /// limit is the neighbouring cell: 0.65 + half the chair's depth
        /// (0.148) = 0.80 <= 0.85 (the Z cell is 1.70) and <= 0.925 (the X
        /// cell is 1.85).
        /// </summary>
        private const float SeatRadiusUsed = 0.65f;

        /// <summary>So that the scale screenshot uses the same number.</summary>
        public const float SeatRadiusUsedM = SeatRadiusUsed;

        /// <summary>So that the scale screenshot uses the same numbers.</summary>
        public const float SitLiftM = SitLift;

        /// <summary>So that the scale screenshot uses the same numbers.</summary>
        public const float SitForwardM = SitForward;

        [Header("Renkler")]
        // A CLOSED ROOM IS NO LONGER DRAWN - so it has no colour either.
        //
        // Rooms that had not been opened used to stand there as dark grey
        // slabs, and their colour (0.16) had been balanced by measuring three
        // brightnesses: the floor 0.061, a closed room 0.161, an open hall
        // 0.258. The numbers were right, the QUESTION was wrong.
        //
        // At the first tier only 102.6 of the plot's 172.8 m2 are open; so 41%
        // of the screen was a slab "that is not yours yet". The user's
        // sentence: "the empty rooms should not take up space, the restaurant
        // should be shown in the places where it fills the screen."
        //
        // A room that is not drawn does not come into the framing either
        // (CameraFit.OpenBounds), and an expansion is now really an OPENING:
        // the room appears where there was none. The touch collider went too -
        // touching a closed room took the camera to an empty slab.
        public Color RoomKitchen = new Color(0.22f, 0.24f, 0.27f);
        public Color RoomService = new Color(0.25f, 0.25f, 0.24f);

        private readonly List<Transform> _tables = new List<Transform>();
        private readonly List<TableBadge> _badges = new List<TableBadge>();
        private readonly List<GameObject> _pool = new List<GameObject>();
        private readonly Dictionary<int, GameObject> _seated = new Dictionary<int, GameObject>();
        private readonly List<GameObject> _staff = new List<GameObject>();
        private readonly List<Figure> _staffFigure = new List<Figure>();
        private int _staffBuilt = -1;

        /// <summary>
        /// The shader property id is resolved ONCE. Hashing the string again
        /// on every call meant eight needless lookups a frame.
        /// </summary>
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private Material _floorMat;
        private int _builtTables = -1;
        private MaterialPropertyBlock _block;

        // =====================================================================
        private void Awake()
        {
            // Shader.Find CAN RETURN NULL IN A BUILD.
            //
            // URP/Lit is not in the "always included" list; it gets into the
            // build only because a material on a prefab in the scene uses it.
            // That link is indirect: if the art prefabs are moved to another
            // renderer, or if variant stripping comes into play, this call
            // returns null, new Material(null) produces an invalid material and
            // every floor is drawn magenta. It is never seen in the editor - only
            // in the Android build.
            Shader lit = Shader.Find("Universal Render Pipeline/Lit");
            if (lit == null)
            {
                Debug.LogError("PROBLEMS: URP/Lit was not found; the floor cannot be drawn.");
                enabled = false;
                return;
            }

            _floorMat = new Material(lit);
            _floorMat.SetFloat("_Smoothness", 0.05f);

            // THE BADGE USES AN UNLIT MATERIAL.
            //
            // It used to share the same URP/Lit material as the floor, and
            // TableBadge's comment said "because emission is off the colours
            // read independently of the light". The OPPOSITE is true: if
            // emission is off the colour depends entirely on the light. It was
            // measured - a green badge came out at 1.47:1 contrast on screen;
            // its authored colour on the same floor would give 7.57:1. The light
            // was eating five times the colour.
            //
            // The result: the ONE channel that tells the player which table is
            // about to walk out was at half the 3:1 threshold a meaningful
            // graphic needs.
            // A SHADER GETS INTO THE BUILD THROUGH A REFERENCE.
            //
            // Shader.Find ALWAYS succeeds IN THE EDITOR - every shader is
            // loaded. On a device a shader only gets into the build if an asset
            // refers to it, or if it is in GraphicsSettings' "always included"
            // list. NOT A SINGLE asset in the project referred to URP/Unlit: on
            // Android Find would return null, the code would quietly fall back
            // to the Lit material and the badge would drop back to 1.47:1
            // contrast - the very bug fixed today, only in the form that is
            // invisible in the editor.
            //
            // Two guards: the shader was added to the list
            // (ProjectSettings/GraphicsSettings.asset) and this place NO LONGER
            // FALLS BACK SILENTLY. A silent fallback is the thing nobody will
            // notice next time round.
            // THE TRANSPARENT MATERIALS: all of them ASSETS, nothing built at
            // runtime.
            //
            // If one is missing WE DO NOT FALL BACK SILENTLY. A silent fallback
            // is the thing nobody will notice next time round - and this class of
            // bug (drawn opaque in the build) gives no sign at all in the editor.
            _glassMat = GlassMaterial;
            _wallMat = WallMaterial;
            _doorMat = DoorMaterial;
            _glowMat = GlowMaterial;
            _ceilMat = CeilingGlowMaterial;
            _waterMat = WaterMaterial;

            if (_glassMat == null || _wallMat == null
                || _doorMat == null || _glowMat == null || _ceilMat == null
                || _waterMat == null)
                Debug.LogError("PROBLEMS: the transparent materials are not bound "
                               + "(run ArtPrefabs.Run + BuildGameScene.Run). "
                               + "A transparent material built at runtime is drawn opaque IN THE BUILD.");

            Shader unlit = Shader.Find("Universal Render Pipeline/Unlit");
            if (unlit == null)
            {
                Debug.LogError("PROBLEMS: URP/Unlit is not in the build; the badges "
                               + "fall back to a lit material and cannot be read.");
                _badgeMat = _floorMat;
            }
            else
            {
                _badgeMat = new Material(unlit);
            }
        }

        private Material _badgeMat;
        private Material _glassMat;
        private Material _wallMat;
        private Material _doorMat;

        /// <summary>The colour of the door leaf. Darker than the wall and more opaque.</summary>
        private static readonly Color DoorColor =
            new Color(0.62f, 0.45f, 0.30f, 0.55f);

        // THE WALL'S COLOUR AND TRANSPARENCY ARE NOW IN AN ASSET:
        // Art/Materials/custom_wall.mat, alpha 0.10.
        //
        // There used to be a `WallColor` field here; transparent materials
        // have to be ASSETS (the transparent shader variant is stripped from
        // the build, see docs/36) and the field was deleted. Its summary was
        // left OWNERLESS for a while - a comment that described no field at
        // all, and carried the old value (0.20) into the bargain. The .mat is
        // the only true source.
        /// <summary>
        /// The wall's height (m). The character is 1.00 m; 1.15 is a little
        /// above that - it draws the line of the room, but the camera still
        /// sees inside from its 34 degree angle. A full-height wall (2.4 m)
        /// would hide the front row completely.
        /// </summary>
        private const float WallHeight = 1.15f;

        /// <summary>The wall's thickness (m).</summary>
        private const float WallThick = 0.06f;
        private readonly List<Appliance> _stoves = new List<Appliance>();

        /// <summary>
        /// The badge material's shader name. So the tour can ask: the
        /// question "is it unlit" only means anything IN A REAL BUILD,
        /// because Shader.Find always succeeds in the editor.
        /// </summary>
        public string BadgeShaderName
        {
            get
            {
                return _badgeMat != null && _badgeMat.shader != null
                    ? _badgeMat.shader.name : null;
            }
        }

        /// <summary>
        /// The floor material built at runtime. If it is not destroyed it
        /// leaks when the scene closes.
        /// </summary>
        /// <summary>
        /// ONLY the materials BUILT HERE are destroyed.
        ///
        /// The wall, the door, the glass and the pool of light are .mat ASSETS
        /// now (ArtPrefabs produces them, BuildGameScene binds them) -
        /// destroying those would delete the loaded instance of an asset on
        /// disk and the material would be missing in the next scene. That is
        /// why the list got shorter, not because something was forgotten.
        /// </summary>
        private void OnDestroy()
        {
            // NO MANUAL CLEANUP ON QUIT: Unity is unloading everything anyway
            // and calling Destroy at that moment can make the process crash (see
            // OwnedMesh).
            if (Application.isPlaying && !Application.isEditor
                && GameApp.Quitting) return;

            ClearTints();
            if (_floorMat != null) Destroy(_floorMat);
            if (_badgeMat != null && _badgeMat != _floorMat) Destroy(_badgeMat);
            if (_lampMat != null && _lampMat != _floorMat) Destroy(_lampMat);
        }

        /// <summary>
        /// Deletes everything that was built.
        ///
        /// IN EDITOR MODE DestroyImmediate IS ESSENTIAL. Object.Destroy defers
        /// the deletion to the end of the frame, and in batch mode that frame
        /// never comes - so the old floor plan stays in the scene. It went
        /// unseen for a long time because both cuisines were built with the
        /// SAME number of tables, and the same geometry on top of itself gave
        /// the same picture. It came out when the opening screenshot was
        /// added: a screenshot labelled "4 tables" had ten tables in it.
        /// </summary>
        public void Clear()
        {
            bool playing = Application.isPlaying;
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                GameObject go = transform.GetChild(i).gameObject;
                if (playing)
                {
                    // IN PLAY MODE Destroy IS DEFERRED TO THE END OF THE FRAME and
                    // Rebuild() builds the new scene right afterwards: IN THAT FRAME
                    // THE OLD AND THE NEW ARE BOTH IN THE SCENE.
                    //
                    // There are two costs. The visible one: a single frame of double
                    // the draw load at a tier change, a stutter on mobile. The sly
                    // one: WallCount, WallsClear and AccessOk COUNT the children - the
                    // tour reading them in that frame was SILENTLY misleading the
                    // check.
                    //
                    // The answer is not to make the destruction faster but to take the
                    // object OUT OF THE TREE AND OUT OF THE PICTURE at once:
                    // SetParent and SetActive take effect immediately, Destroy
                    // finishes in its own time.
                    go.SetActive(false);
                    go.transform.SetParent(null, false);
                    Destroy(go);
                }
                else DestroyImmediate(go);
            }

            _tables.Clear();
            _badges.Clear();
            _stoves.Clear();
            _tableSquareZ = 0f;
            _chairs.Clear();
            _potSpots.Clear();
            _potCount = 0;
            _tableFood.Clear();
            _tableTop = 0f;
            _chairBack.Clear();
            _doors.Clear();
            _roomGlow.Clear();
            _dirtyStack.Clear();
            _cleanStack.Clear();
            _foam.Clear();
            _cookRoutine.Clear();
            _staffTask.Clear();

            // THE PEOPLE ON THE STREET ARE CLEARED TOO.
            //
            // Clear() destroys all the children and the pedestrians are among
            // them - but StreetLife went on keeping five DEAD records in its own
            // list and touching destroyed objects every frame.
            if (_streetLife != null) _streetLife.Clear();
            _pool.Clear();
            _seated.Clear();
            _queued.Clear();
            _leaving.Clear();
            _staff.Clear();
            _staffFigure.Clear();

            // THE WORK-CLIP MEASUREMENT IS RESET TOO.
            //
            // The dictionary is keyed by the staff INDEX. When the crew changes
            // the indices are reused, but the old progress value stayed: the new
            // staff member was compared against the old (large) value on its
            // first frame and WorkAnimStalled went up. That counter is the ONLY
            // measure of "is the animation really playing" - a dirty start goes
            // on showing a bug that has been FIXED as broken.
            _workClip.Clear();
            _washHold.Clear();
            _figureOf.Clear();
            _builtTables = -1;
            _staffBuilt = -1;
        }

        /// <summary>Builds the floor plan again from scratch. Called when the table count changes.</summary>
        public void Rebuild()
        {
            Simulation src = Source;
            if (src == null) return;
            Clear();

            int tables = src.TableCount;
            // The tint cache is emptied at build time: when the save changes
            // and another cuisine is loaded, the old colours must not remain.
            // The copies ARE NOW DESTROYED TOO - see ClearTints().
            ClearTints();
            BuildFloors(tables);
            BuildStreet(tables);
            BuildStreetLife();
            BuildWalls(tables);
            BuildRoomLights(tables);
            BuildRoomProps(tables);
            BuildTables(tables);
            // THE SCENE'S DECORATION COMES LAST: the back wall, the sign, the
            // plant pots and the kitchen's extractor hood. They all take their
            // colour from the cuisine's IDENTITY.
            BuildDecor(tables);
            BuildPots(Pal(CuisineId));
            _builtTables = tables;
        }

        private void Update()
        {
            Simulation sim = Source;
            if (sim == null) return;
            if (sim.TableCount != _builtTables) { Rebuild(); return; }

            // The floor is painted AT BUILD TIME ONLY.
            //
            // It used to be painted every frame: eight floors, a property block
            // read and write plus a shader id lookup for each - and the colours
            // only change when the table count changes, that is at most three
            // times in sixty days. Writing a property block also throws those
            // renderers out of SRP batching.
            UpdateCustomers(sim);
            UpdateStaff(sim);
            UpdatePlateStacks(sim);
            UpdateWater();
            if (WashingCount > 0) _washSeen++;
            UpdateAppliances(sim);
            UpdateDoors();
        }

        // =====================================================================
        private void BuildFloors(int tables)
        {
            if (_block == null) _block = new MaterialPropertyBlock();

            for (int i = 0; i < RoomPlan.Rooms.Length; i++)
            {
                RoomPlan.Room r = RoomPlan.Rooms[i];

                // A ROOM THAT HAS NOT BEEN OPENED IS BUILT AS A SHELL.
                //
                // It used to be skipped entirely, and that left a HOLE. The
                // backdrop wall spans the bounding box of the OPEN rooms
                // (RestaurantView.Decor.cs), and that box is not the same
                // shape as the rooms in it: at tier 1 the open rooms reach
                // x 13.4 and z 9.6, but Hall2 (x 8.4-13.4, z 4.4-9.6) is
                // closed. So a 5.0 x 5.2 m rectangle of sky sat inside the
                // building, with a 2.6 m wall standing over it and a side
                // return hanging in mid-air on nothing.
                //
                // It is on screen for the whole first session and in every
                // store screenshot, and it is the single clearest "this is a
                // level editor" tell in the frame.
                //
                // A SHELL IS BETTER THAN A CLIPPED WALL. Clipping the backdrop
                // to the built footprint would work and it would give a
                // staircase-shaped wall, because the footprint is L-shaped.
                // Building the closed room as a bare, unfinished slab fills
                // the rectangle AND shows the player the space they can expand
                // into - and expansion is the campaign's main progression
                // path, which until now had no before and after on screen.
                //
                // IT COSTS NOTHING IN FRAMING. CameraFit.OpenBounds is built
                // from the OPEN rooms and the street only, so a shell does not
                // push the camera back by a millimetre.
                if (!RoomPlan.RoomOpen(in r, tables)) { BuildShell(in r); continue; }

                GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
                floor.name = "Room_" + r.Name;
                floor.transform.SetParent(transform, false);
                floor.transform.localPosition = new Vector3(r.CenterX, -0.05f, r.CenterZ);
                // A 4 cm gap: the dividing line has to be visible, otherwise the
                // whole floor reads as a single slab.
                floor.transform.localScale = new Vector3(r.W - 0.04f, 0.1f, r.D - 0.04f);

                Renderer ren = floor.GetComponent<Renderer>();
                ren.sharedMaterial = _floorMat;

                // The colour is given AT BUILD TIME, not every frame: a floor's
                // colour can only change when the table count changes, and that
                // happens at most three times in sixty days. Writing a property
                // block also throws the renderers out of SRP batching, so it is
                // not free.
                ren.GetPropertyBlock(_block);
                _block.SetColor(BaseColorId, RoomColor(in r));
                ren.SetPropertyBlock(_block);

                // The touch target is the ROOM (the docs/31 measurement). The
                // collider box extends above the floor so that the ray lands on the
                // room and not on a table.
                BoxCollider box = floor.GetComponent<BoxCollider>();
                box.size = new Vector3(1f, 14f, 1f);
                floor.AddComponent<RoomTouch>().RoomIndex = i;
            }
        }

        /// <summary>
        /// A ROOM THAT HAS NOT BEEN BOUGHT YET: a bare concrete slab.
        ///
        /// It is deliberately NOT a room. No wall, no door, no furniture, no
        /// RoomTouch - it is not a place the player can act on, and it must
        /// not read as one. What it does is stop the building having a hole
        /// in it, and give expansion something visible to buy.
        ///
        /// THE COLLIDER IS REMOVED. The floor primitive comes with one, and
        /// leaving it would put an invisible 14 m box in the ray's path
        /// without a RoomTouch behind it - the tap would hit nothing and the
        /// player would learn that part of the building does not respond.
        /// Destroying it is what makes "not touchable" true rather than
        /// merely untagged.
        ///
        /// It sits 2 cm BELOW the finished floors. Level with them the two
        /// would z-fight along the 4 cm gap; below, the step reads as a floor
        /// that has not been laid yet.
        /// </summary>
        private void BuildShell(in RoomPlan.Room r)
        {
            GameObject shell = GameObject.CreatePrimitive(PrimitiveType.Cube);
            shell.name = "Shell_" + r.Name;
            shell.transform.SetParent(transform, false);
            shell.transform.localPosition = new Vector3(r.CenterX, -0.07f, r.CenterZ);
            shell.transform.localScale = new Vector3(r.W - 0.04f, 0.1f, r.D - 0.04f);

            Collider c = shell.GetComponent<Collider>();
            if (c != null) Destroy(c);

            Renderer ren = shell.GetComponent<Renderer>();
            ren.sharedMaterial = _floorMat;
            ren.GetPropertyBlock(_block);
            _block.SetColor(BaseColorId, ShellColor);
            ren.SetPropertyBlock(_block);
        }

        /// <summary>
        /// Unfinished concrete. It is the same in both cuisines on purpose:
        /// an empty shell has no identity yet - that is what buying it is for.
        /// </summary>
        private static readonly Color ShellColor = new Color(0.255f, 0.243f, 0.231f);

        /// <summary>
        /// THE TRANSPARENT WALLS AND DOORS THAT SEPARATE THE ROOMS.
        ///
        /// Why they are needed: the floor plan read as a single floor slab;
        /// the only things marking the rooms' boundaries were a 4 cm gap and
        /// a difference in colour.
        ///
        /// Why TRANSPARENT: an opaque wall would hide the back rooms
        /// completely from a 34 degree view, and all of the game's
        /// information is in there.
        ///
        /// THE WALLS COME LINE BY LINE, THE DOORS PAIR BY PAIR.
        ///
        /// The first version drew each room's four edges separately and the
        /// shared lines were drawn twice, so the alpha doubled up. The second
        /// version put ONE door per line, and that broke the floor plan's
        /// logic: the line x = 5.2 has three separate neighbouring pairs
        /// (entrance-wash, kitchen-wash, kitchen-store) and a single door
        /// only helped one of them.
        ///
        /// The doors now come FROM PAIRS OF ROOMS. Which pair opens onto
        /// which is what Connect() says - and the rule the user asked for is
        /// written there: THE STORE IS ENTERED FROM THE KITCHEN ONLY.
        /// </summary>
        private void BuildWalls(int tables)
        {
            if (_wallMat == null) return;

            Dictionary<int, List<Vector2>> vertical = new Dictionary<int, List<Vector2>>();
            Dictionary<int, List<Vector2>> horizontal = new Dictionary<int, List<Vector2>>();
            Dictionary<int, List<Gap>> verticalDoors = new Dictionary<int, List<Gap>>();
            Dictionary<int, List<Gap>> horizontalDoors = new Dictionary<int, List<Gap>>();
            _gaps = 0;

            for (int i = 0; i < RoomPlan.Rooms.Length; i++)
            {
                RoomPlan.Room r = RoomPlan.Rooms[i];
                if (!RoomPlan.RoomOpen(in r, tables)) continue;

                Add(vertical, r.X0, r.Z0, r.Z0 + r.D);
                Add(vertical, r.X0 + r.W, r.Z0, r.Z0 + r.D);
                Add(horizontal, r.Z0, r.X0, r.X0 + r.W);
                Add(horizontal, r.Z0 + r.D, r.X0, r.X0 + r.W);
            }

            // --- doors: one for every neighbouring pair that OPENS onto each other
            _links.Clear();
            for (int i = 0; i < RoomPlan.Rooms.Length; i++)
            {
                RoomPlan.Room A = RoomPlan.Rooms[i];
                if (!RoomPlan.RoomOpen(in A, tables)) continue;

                for (int j = i + 1; j < RoomPlan.Rooms.Length; j++)
                {
                    RoomPlan.Room B = RoomPlan.Rooms[j];
                    if (!RoomPlan.RoomOpen(in B, tables)) continue;
                    if (!Connect(A.Name, B.Name)) continue;

                    float coord, spot;
                    bool d;
                    if (!Shared(in A, in B, out coord, out spot, out d)) continue;

                    AddDoor(d ? verticalDoors : horizontalDoors, coord, spot,
                            Paneled(A.Name, B.Name));
                    _links.Add(new Link { A = i, B = j });
                }
            }

            // THE MAIN DOOR: on the front face, in the middle of the entrance
            // room. It ALWAYS has a leaf - this is the threshold between the
            // street and the hall.
            AddDoor(horizontalDoors, 0f, Paths.DoorX, true);

            foreach (KeyValuePair<int, List<Vector2>> h in vertical)
                Line(h.Key * 0.01f, Merge(h.Value), true, Doors(verticalDoors, h.Key));
            foreach (KeyValuePair<int, List<Vector2>> h in horizontal)
                Line(h.Key * 0.01f, Merge(h.Value), false, Doors(horizontalDoors, h.Key));
        }

        /// <summary>
        /// Do these two rooms open onto each other?
        ///
        /// THE STORE FROM THE KITCHEN ONLY. The user's rule; in a real
        /// restaurant the larder is behind the kitchen too and is not entered
        /// straight from the hall or from the wash room. The wash room is
        /// connected to the kitchen - they are adjacent anyway.
        /// </summary>
        private static bool Connect(string a, string b)
        {
            if (a == "Store" || b == "Store")
            {
                string other = a == "Store" ? b : a;
                return other == "Kitchen";
            }
            return true;
        }

        /// <summary>
        /// Does this doorway have A LEAF - or is it only a gap?
        ///
        /// The user's decision: "we can take away the doors other than the
        /// entrance and the kitchen door, let there just be a gap between the
        /// rooms that they can walk straight through, with no door opening
        /// and closing".
        ///
        /// A right decision, and there is a reason for it too: in a real
        /// restaurant there is no door between one dining room and another,
        /// there is an open passage. A leaf only marks a THRESHOLD - from the
        /// street into the hall, from the hall into the kitchen. And eight
        /// transparent leaves opening and closing constantly in a 34 degree
        /// view had turned the movement itself into noise: the eye goes to
        /// the most moving thing in the scene every frame, and that is not
        /// the game's information.
        ///
        /// THE KITCHEN DOOR = kitchen-entrance. The store-kitchen doorway
        /// could have had a leaf too, but the user said "the entrance and the
        /// kitchen"; and the store door is not visible from anywhere in the
        /// hall.
        /// </summary>
        private static bool Paneled(string a, string b)
        {
            return (a == "Kitchen" && b == "Entry")
                || (a == "Entry" && b == "Kitchen");
        }

        /// <summary>A gap in a wall: where it is and whether it has a leaf.</summary>
        private struct Gap
        {
            public float Spot;
            public bool Leaf;
        }

        /// <summary>
        /// The shared edge of two rooms. coord: the line's coordinate, spot:
        /// where the door goes along that line, vertical: whether the line
        /// has a fixed x.
        ///
        /// The door goes to the MIDDLE of the shared edge - unless the shared
        /// edge contains the front corridor (Paths.LaneZ), in which case it
        /// goes there: that is where the crossing happens anyway, and a door
        /// put anywhere else would have the figures walking through the wall.
        /// </summary>
        private static bool Shared(in RoomPlan.Room A, in RoomPlan.Room B,
                                   out float coord, out float spot, out bool vertical)
        {
            coord = 0f; spot = 0f; vertical = true;

            // A vertical neighbouring pair: one's right edge is the other's left edge.
            float ax1 = A.X0 + A.W, bx1 = B.X0 + B.W;
            if (Mathf.Abs(ax1 - B.X0) < 0.01f || Mathf.Abs(bx1 - A.X0) < 0.01f)
            {
                coord = Mathf.Abs(ax1 - B.X0) < 0.01f ? ax1 : bx1;
                float z0 = Mathf.Max(A.Z0, B.Z0);
                float z1 = Mathf.Min(A.Z0 + A.D, B.Z0 + B.D);
                if (z1 - z0 < DoorWidth + MinJamb * 2f) return false;
                spot = (Paths.LaneZ > z0 && Paths.LaneZ < z1)
                    ? Paths.LaneZ : (z0 + z1) * 0.5f;
                vertical = true;
                return true;
            }

            // A horizontal neighbouring pair: one's top edge is the other's bottom edge.
            float az1 = A.Z0 + A.D, bz1 = B.Z0 + B.D;
            if (Mathf.Abs(az1 - B.Z0) < 0.01f || Mathf.Abs(bz1 - A.Z0) < 0.01f)
            {
                coord = Mathf.Abs(az1 - B.Z0) < 0.01f ? az1 : bz1;
                float x0 = Mathf.Max(A.X0, B.X0);
                float x1 = Mathf.Min(A.X0 + A.W, B.X0 + B.W);
                if (x1 - x0 < DoorWidth + MinJamb * 2f) return false;
                spot = (x0 + x1) * 0.5f;
                vertical = false;
                return true;
            }
            return false;
        }

        private static void AddDoor(Dictionary<int, List<Gap>> line, float coord,
                                    float spot, bool leaf)
        {
            int k = Mathf.RoundToInt(coord * 100f);
            List<Gap> l;
            if (!line.TryGetValue(k, out l)) { l = new List<Gap>(); line[k] = l; }
            l.Add(new Gap { Spot = spot, Leaf = leaf });
        }

        private static List<Gap> Doors(Dictionary<int, List<Gap>> line, int key)
        {
            List<Gap> l;
            if (!line.TryGetValue(key, out l)) return _empty;
            l.Sort((p, q) => p.Spot.CompareTo(q.Spot));
            return l;
        }

        private static readonly List<Gap> _empty = new List<Gap>();

        private static void Add(Dictionary<int, List<Vector2>> line,
                                float coord, float a, float b)
        {
            int k = Mathf.RoundToInt(coord * 100f);
            List<Vector2> l;
            if (!line.TryGetValue(k, out l)) { l = new List<Vector2>(); line[k] = l; }
            l.Add(new Vector2(a, b));
        }

        /// <summary>Merges overlapping spans.</summary>
        private static List<Vector2> Merge(List<Vector2> spans)
        {
            spans.Sort((p, q) => p.x.CompareTo(q.x));
            List<Vector2> result = new List<Vector2>();
            foreach (Vector2 v in spans)
            {
                if (result.Count > 0 && v.x <= result[result.Count - 1].y + 0.01f)
                {
                    Vector2 last = result[result.Count - 1];
                    last.y = Mathf.Max(last.y, v.y);
                    result[result.Count - 1] = last;
                }
                else result.Add(v);
            }
            return result;
        }

        /// <summary>
        /// Builds a line's walls, leaving a door gap at the given places. A
        /// gap may run past the end of a piece - an opening flush with a
        /// corner is right, that is where people walk through.
        /// </summary>
        private void Line(float coord, List<Vector2> pieces, bool vertical,
                          List<Gap> doors)
        {
            foreach (Vector2 p in pieces)
            {
                float cursor = p.x;
                for (int k = 0; k < doors.Count; k++)
                {
                    float g0 = doors[k].Spot - DoorWidth * 0.5f;
                    float g1 = doors[k].Spot + DoorWidth * 0.5f;
                    if (g1 <= p.x + 0.05f || g0 >= p.y - 0.05f) continue;

                    Slab(coord, cursor, Mathf.Min(g0, p.y), vertical);
                    cursor = Mathf.Max(cursor, Mathf.Min(g1, p.y));
                    _gaps++;

                    // A GAP AT EVERY DOORWAY, A LEAF AT ONLY TWO OF THEM.
                    //
                    // The gap goes on being cut - Connect() says which room opens
                    // onto which and that rule has not changed (the store from the
                    // kitchen only). The only thing that changed is whether a LEAF is
                    // put in that gap.
                    if (!doors[k].Leaf) continue;

                    Vector3 spot = vertical ? new Vector3(coord, 0f, doors[k].Spot)
                                        : new Vector3(doors[k].Spot, 0f, coord);
                    Door d = Door.Create(transform, spot, vertical ? 0f : 90f,
                                         DoorWidth, WallHeight - 0.08f, _doorMat);
                    if (d != null) _doors.Add(d);
                }
                Slab(coord, cursor, p.y, vertical);
            }
        }

        private void Slab(float coord, float a, float b, bool vertical)
        {
            float length = b - a;
            if (length < 0.05f) return;

            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "Wall";
            wall.transform.SetParent(transform, false);

            float center = (a + b) * 0.5f;
            wall.transform.localPosition = vertical
                ? new Vector3(coord, WallHeight * 0.5f, center)
                : new Vector3(center, WallHeight * 0.5f, coord);
            wall.transform.localScale = vertical
                ? new Vector3(WallThick, WallHeight, length)
                : new Vector3(length, WallHeight, WallThick);

            // Remove the collider: in the editor Destroy IS DEFERRED and the
            // box stays in the scene. The touch target has to be the room floor.
            Collider col = wall.GetComponent<Collider>();
            if (col != null)
            {
                if (Application.isPlaying) Destroy(col); else DestroyImmediate(col);
            }

            Renderer ren = wall.GetComponent<Renderer>();
            ren.sharedMaterial = _wallMat;

            // A TRANSPARENT WALL CASTS NO SHADOW.
            //
            // What the user reported: "the shadows in the rooms slide into
            // other rooms". The biggest of the causes was this - the walls are
            // GLASS (alpha 0.10) but SOLID in the shadow map: a 1.15 m slab
            // leaves a 6.5 m long dark band when the sun is at 10 degrees, and
            // that band covers half of the neighbouring room.
            //
            // It is wrong twice over: a glass partition casts no shadow anyway,
            // and the shadow it cast fell exactly where the player NEEDS TO
            // LOOK.
            //
            // (A transparent wall's shadow falls OPAQUE: the URP shadow pass
            // does not read alpha.)
            ren.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ren.receiveShadows = false;
        }

        private struct Link { public int A, B; }

        private readonly List<Link> _links = new List<Link>();

        /// <summary>
        /// THE ROOM ACCESS CHECK.
        ///
        /// The user's request: "check the rooms by their properties - the
        /// store should only be reachable from the kitchen". A rule is only a
        /// rule IF IT CAN BE TESTED: this walks the door graph and verifies
        ///   1. that every open room can be reached from the entrance,
        ///   2. that the store is entered ONLY from the kitchen.
        /// Both can break silently - when the floor plan changes nobody would
        /// notice.
        /// </summary>
        public bool AccessOk(out string report)
        {
            int n = RoomPlan.Rooms.Length;
            bool[] open = new bool[n];
            for (int i = 0; i < n; i++)
                open[i] = RoomPlan.RoomOpen(in RoomPlan.Rooms[i], _builtTables);

            // 1. Can every room be reached from the entrance?
            int entry = -1;
            for (int i = 0; i < n; i++)
                if (RoomPlan.Rooms[i].Name == "Entry") entry = i;

            bool[] found = new bool[n];
            if (entry >= 0) Flood(entry, found, -1);

            var sb = new System.Text.StringBuilder();
            bool ok = true;
            for (int i = 0; i < n; i++)
            {
                if (!open[i] || found[i]) continue;
                ok = false;
                sb.Append("unreachable:" + RoomPlan.Rooms[i].Name + " ");
            }

            // 2. The store must not be reachable while the kitchen is closed.
            int store = -1, kitchen = -1;
            for (int i = 0; i < n; i++)
            {
                if (RoomPlan.Rooms[i].Name == "Store") store = i;
                if (RoomPlan.Rooms[i].Name == "Kitchen") kitchen = i;
            }
            if (store >= 0 && kitchen >= 0 && open[store])
            {
                bool[] withoutKitchen = new bool[n];
                if (entry >= 0) Flood(entry, withoutKitchen, kitchen);
                if (withoutKitchen[store])
                {
                    ok = false;
                    sb.Append("the store is entered without the kitchen ");
                }
            }

            report = sb.Length == 0 ? "the access rules hold" : sb.ToString();
            return ok;
        }

        private void Flood(int from, bool[] seen, int blocked)
        {
            if (from < 0 || from == blocked || seen[from]) return;
            seen[from] = true;
            for (int i = 0; i < _links.Count; i++)
            {
                if (_links[i].A == from) Flood(_links[i].B, seen, blocked);
                else if (_links[i].B == from) Flood(_links[i].A, seen, blocked);
            }
        }

        /// <summary>
        /// THE DOORS OPEN TO WHOEVER COMES NEAR.
        ///
        /// Every door x every figure, every frame: the front door, seven inner
        /// doors and at most forty figures - a few hundred distance
        /// comparisons a frame, too cheap to measure. So as not to confuse it
        /// with pathfinding: the door DOES NOT STOP the figure, it only
        /// opens.
        /// </summary>
        private void UpdateDoors()
        {
            if (_doors.Count == 0) return;

            _movers.Clear();
            for (int i = 0; i < _staff.Count; i++)
                if (_staff[i] != null && _staff[i].activeSelf)
                    _movers.Add(_staff[i].transform.localPosition);
            foreach (KeyValuePair<int, GameObject> kv in _seated)
                if (kv.Value != null && kv.Value.activeSelf)
                    _movers.Add(kv.Value.transform.localPosition);
            foreach (KeyValuePair<int, GameObject> kv in _queued)
                if (kv.Value != null && kv.Value.activeSelf)
                    _movers.Add(kv.Value.transform.localPosition);
            for (int i = 0; i < _leaving.Count; i++)
                if (_leaving[i] != null && _leaving[i].activeSelf)
                    _movers.Add(_leaving[i].transform.localPosition);

            float r2 = Door.Sense * Door.Sense;
            for (int d = 0; d < _doors.Count; d++)
            {
                if (_doors[d] == null) continue;
                Vector3 k = _doors[d].Spot;
                bool near = false;
                for (int m = 0; m < _movers.Count; m++)
                {
                    Vector3 delta = _movers[m] - k;
                    delta.y = 0f;
                    if (delta.sqrMagnitude <= r2) { near = true; break; }
                }
                _doors[d].SetOpen(near);
            }
        }

        /// <summary>The number of open doors. So the tour can ask.</summary>
        public int OpenDoorCount
        {
            get
            {
                int n = 0;
                for (int i = 0; i < _doors.Count; i++)
                    if (_doors[i] != null && _doors[i].IsOpen) n++;
                return n;
            }
        }

        /// <summary>
        /// The number of cooks that have a task. So the tour can tell "there
        /// is no work in the kitchen" apart from "the cook is not taking the
        /// work".
        /// </summary>
        public int BusyCooks
        {
            get
            {
                int n = 0;
                for (int i = 0; i < _staffTask.Count && i < _staffCooks; i++)
                    if (_staffTask[i] >= 0) n++;
                return n;
            }
        }

        /// <summary>The cooks' current poses. For the diagnostics.</summary>
        public string CookPoses
        {
            get
            {
                var sb = new System.Text.StringBuilder();
                for (int i = 0; i < _staff.Count && i < _staffCooks; i++)
                {
                    Figure f = FigureOf(_staff[i]);
                    Walker w = WalkerOf(_staff[i]);
                    sb.Append(i + ":" + (f == null ? "none" : f.Current.ToString())
                              + "/g" + (i < _staffTask.Count ? _staffTask[i] : -9)
                              + (w != null && w.Moving ? "/walking" : "") + " ");
                }
                return sb.ToString();
            }
        }

        /// <summary>The number of doors with a leaf. So the tour can ask.</summary>
        public int DoorCount { get { return _doors.Count; } }

        /// <summary>
        /// The number of PASSAGE gaps opened in the walls (with and without
        /// a leaf).
        ///
        /// Once the leaves were removed, "the number of doors" no longer
        /// measured the passages: the number of leaves dropped to two, but
        /// which rooms open onto each other should not have changed. They
        /// have to be two separate numbers, otherwise a change that removes
        /// leaves could quietly wall a room off as well and nobody would
        /// notice.
        /// </summary>
        public int GapCount { get { return _gaps; } }

        /// <summary>The number of room pairs that open onto each other.</summary>
        public int LinkCount { get { return _links.Count; } }

        private int _gaps;

        /// <summary>
        /// The number of figures standing OUTSIDE the plot (on the street).
        ///
        /// So the tour can ask: "the guest walks in from the street" is only
        /// verified when a figure is really seen outside. A street point being
        /// inside the path is not enough - if Warp put it in the wrong place
        /// the path would still look right.
        /// </summary>
        public int OutsideCount
        {
            get
            {
                int n = 0;
                foreach (KeyValuePair<int, GameObject> kv in _seated)
                    if (kv.Value != null && kv.Value.activeSelf
                        && kv.Value.transform.localPosition.z < -0.2f) n++;
                for (int i = 0; i < _leaving.Count; i++)
                    if (_leaving[i] != null && _leaving[i].activeSelf
                        && _leaving[i].transform.localPosition.z < -0.2f) n++;
                return n;
            }
        }

        /// <summary>
        /// The local positions of the figures standing ON THE STREET.
        ///
        /// StreetLife keeps its pedestrians away from these: a pedestrian
        /// walking through a guest waiting in front of the door is exactly the
        /// bug the user reported. The list comes from the caller - allocating
        /// a new list every frame would produce visible garbage at forty
        /// figures.
        /// </summary>
        public void OutsideFigures(List<Vector3> into)
        {
            if (into == null) return;
            foreach (KeyValuePair<int, GameObject> kv in _seated)
                if (kv.Value != null && kv.Value.activeSelf
                    && kv.Value.transform.localPosition.z < -0.02f)
                    into.Add(kv.Value.transform.localPosition);
            foreach (KeyValuePair<int, GameObject> kv in _queued)
                if (kv.Value != null && kv.Value.activeSelf
                    && kv.Value.transform.localPosition.z < -0.02f)
                    into.Add(kv.Value.transform.localPosition);
            for (int i = 0; i < _leaving.Count; i++)
                if (_leaving[i] != null && _leaving[i].activeSelf
                    && _leaving[i].transform.localPosition.z < -0.02f)
                    into.Add(_leaving[i].transform.localPosition);
        }

        /// <summary>
        /// The pairs of pedestrians merged into each other on the street. So
        /// the tour can ask.
        public int StreetOverlaps
        {
            get { return _streetLife == null ? 0 : _streetLife.Overlaps; }
        }

        /// <summary>
        /// The number of pedestrians that have walked into a street lamp
        /// post.
        ///
        /// If there is no view or no street it returns 1, not 0: "there is
        /// nothing to measure" and "everything is fine" must not give the
        /// same number.
        public int PostOverlaps
        {
            get { return _streetLife == null ? 1 : _streetLife.PostOverlaps; }
        }

        /// <summary>The number of posts the street pushing knows about.</summary>
        public int StreetPostsKnown
        {
            get { return _streetLife == null ? 0 : _streetLife.PostsKnown; }
        }

        private readonly List<Door> _doors = new List<Door>();
        private readonly List<Vector3> _movers = new List<Vector3>();

        /// <summary>The width of the door gap (m). The figure's width is 0.85.</summary>
        private const float DoorWidth = 1.10f;

        /// <summary>
        /// The SMALLEST wall margin on either side of a doorway (m).
        ///
        /// It used to be a single number, `DoorWidth + 0.3f`; the same number
        /// now stands with a name. THE BEHAVIOUR HAS NOT CHANGED - a
        /// refactoring, not a fix.
        ///
        /// The shared edge of the kitchen and the wash room is EXACTLY 1.40 m
        /// and the threshold is exactly 1.40: whether this doorway is ruled
        /// out comes down to the last digit of the floating point. IT WAS
        /// MEASURED - the doorway IS there (the link count is 5; it would be
        /// 4 otherwise), because 5.4f - 4.0f = 1.4000001 and the threshold is
        /// 1.4000000.
        ///
        /// So this edge rests not on a safe margin but on a coincidence in
        /// one digit. If the floor plan changes, look here first: the
        /// kitchen's neighbouring the wash room is the game's most used
        /// doorway (docs/36 "the place the washing up is done is connected to
        /// the kitchen").
        ///
        /// An opening flush with a corner is right in itself (see Line): the
        /// gap is clipped, not ruled out.
        /// </summary>
        private const float MinJamb = 0.15f;

        /// <summary>
        /// THE STREET IN FRONT OF THE RESTAURANT.
        ///
        /// Why it is needed: the guests used to appear at the bottom edge of
        /// the frame, and that place was nothing at all - neither pavement
        /// nor road, only emptiness. What gives the feeling of "it came from
        /// outside" is the outside existing.
        ///
        /// Why NARROW: the camera framing depends on the depth (docs/31) and
        /// every metre added at the front makes the restaurant smaller on
        /// screen - the touch-target measurement is already close to Google's
        /// 48 dp minimum. A 1.6 m street means a 1.1 m widening of the
        /// framing; the pavement and a strip of the tarmac are visible, and
        /// that much is enough.
        ///
        /// Three slabs: the pavement, the kerb line, the tarmac. No colliders.
        /// </summary>
        private void BuildStreet(int tables)
        {
            _lampHeads.Clear();
            _lampGlow.Clear();
            _lampX.Clear();
            _streetSlabs.Clear();
            _streetBase.Clear();
            BuildStamp++;
            // THE PAVEMENT IS AS WIDE AS THE PEDESTRIANS NEED.
            //
            // The old margins made the pavement 0.60 m and the walking line
            // (Paths.PavementZ) was at -1.05: so everybody walked ON THE TARMAC,
            // beyond the kerb. The pavement is now 1.10 m and both pedestrian
            // lanes are inside it.
            //
            // THE MARGINS FOLLOW THE FRAMING: the camera sees only
            // CameraFit.StreetInFrame of the street (1.70 m) and the rest is
            // drawn but invisible. In the first version the pavement was widened
            // to 1.12 m but the framing was at 1.10: the kerb and the tarmac fell
            // completely outside it, so the "street" turned into a strip of
            // pavement with no tarmac.
            //
            // The pavement is 1.40 m: two pedestrian lanes (0.70 apart) plus half
            // a body on each side.
            Street("Pavement", -1.42f, -0.02f, new Color(0.62f, 0.60f, 0.57f));
            // The kerb: thin and light - the line that separates the pavement
            // from the road.
            Street("Kerb", -1.54f, -1.42f, new Color(0.78f, 0.76f, 0.72f));
            // The tarmac. 0.42 m of it comes into the frame - the least that
            // will say "this is a road".
            Street("Asphalt", -2.20f, -1.54f, new Color(0.26f, 0.26f, 0.28f));

            // THE STREET LAMPS: AT THE START, IN THE MIDDLE AND AT THE END.
            //
            // At first four posts divided the plot into four, and because one of
            // them fell in front of the door it was shifted by 2.2 m; the
            // spacings came out as 2.3 / 4.5 / 4.5 m. The one readable property
            // of a row of posts is EQUAL SPACING - a shifted post reads not as "a
            // street" but as "a few scattered posts".
            //
            // BY THE OPEN ROOMS, NOT BY THE PLOT. The first fix put three posts
            // evenly over the plot (0 / 9 / 18 m) and it was symmetric on paper -
            // but the camera frames not the PLOT but the OPEN ROOMS
            // (CameraFit.OpenBounds). At the opening tier the player sees between
            // 0 and 13.4 m, so the post at 18 is NOT on screen: what is seen is
            // two posts and a row leaning to the left - precisely the complaint
            // that was meant to be fixed.
            //
            // The posts now stand at the start, the middle and the end of the
            // strip that is visible at each tier. As the restaurant grows the
            // street grows with it - a street that grows with the building is the
            // game's premise anyway.
            // The right edge of the OPEN rooms - the same calculation as
            // CameraFit.OpenBounds. Looking at the left edge of the closed rooms
            // would be wrong: that would rely on the tiers always opening from
            // the right.
            float last = 0f;
            for (int i = 0; i < RoomPlan.Rooms.Length; i++)
            {
                RoomPlan.Room r = RoomPlan.Rooms[i];
                if (!RoomPlan.RoomOpen(in r, tables)) continue;
                if (r.X0 + r.W > last) last = r.X0 + r.W;
            }
            if (last < 1f) last = RoomPlan.PlotW;
            for (int i = 0; i < 3; i++)
                StreetLamp(last * i * 0.5f);
        }

        /// <summary>
        /// THE CEILING LAMPS THAT LIGHT THE INSIDE OF THE RESTAURANT.
        ///
        /// The user's sentence: "when evening comes the inside of the
        /// restaurant goes dark, let us light the inside too, but unlike the
        /// street ones the lamps should not be physically visible, since they
        /// will be on the ceiling".
        ///
        /// The right request and the right reason: the camera looks down at a
        /// building with no ceiling. A ceiling fitting, if it were drawn,
        /// would cover the very place it lights - and there is no ceiling
        /// there anyway, so the fitting would hang in mid-air.
        ///
        /// HENCE ONLY A POOL OF LIGHT: the same technique as the street
        /// lamps' (an unlit, ADDITIVELY blended slab), because additional
        /// lights are OFF in the URP asset (m_AdditionalLightsRenderingMode:
        /// 0, the docs/19 mobile budget) and a spot light placed in the scene
        /// does NOTHING AT ALL - without warning.
        ///
        /// A grid sized to the room: a single large pool gives a blotch that
        /// is bright in the middle and dark at the edges in a rectangular room
        /// - it reads as "a lantern on the floor" rather than "ceiling
        /// lighting".
        ///
        /// The colour is DIFFERENT from the street's: the street lamp is
        /// sodium yellow (1.00 / 0.80 / 0.45), the inside is warm white. Were
        /// the two the same colour, "inside" and "outside" would read as the
        /// continuation of one place; being different, the building stands in
        /// its own light.
        /// </summary>
        private void BuildRoomLights(int tables)
        {
            _roomGlow.Clear();
            if (_ceilMat == null) return;

            for (int i = 0; i < RoomPlan.Rooms.Length; i++)
            {
                RoomPlan.Room r = RoomPlan.Rooms[i];
                if (!RoomPlan.RoomOpen(in r, tables)) continue;

                int width = Mathf.Max(1, Mathf.RoundToInt(r.W / RoomLampSpan));
                int deep = Mathf.Max(1, Mathf.RoundToInt(r.D / RoomLampSpan));
                float dx = r.W / width, dz = r.D / deep;
                // The pools SPILL INTO ONE ANOTHER (1.55 times): at exactly the
                // cell size, dark junctions were left between the slabs and the
                // grid itself became visible.
                float size = Mathf.Min(dx, dz) * 1.55f;

                for (int cc = 0; cc < width; cc++)
                    for (int rr = 0; rr < deep; rr++)
                        RoomLamp(r.X0 + dx * (cc + 0.5f),
                                 r.Z0 + dz * (rr + 0.5f), size);
            }
        }

        /// <summary>The light a single ceiling lamp casts on the floor.</summary>
        private void RoomLamp(float x, float z, float size)
        {
            GameObject pool = GameObject.CreatePrimitive(PrimitiveType.Quad);
            pool.name = "CeilingLight";
            pool.transform.SetParent(transform, false);
            // 0.014 - just above the street pool (0.012). At the same height
            // the two would z-fight in front of the door and flicker.
            pool.transform.localPosition = new Vector3(x, 0.014f, z);
            pool.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            pool.transform.localScale = new Vector3(size, size, 1f);

            Collider col = pool.GetComponent<Collider>();
            if (col != null)
            {
                if (Application.isPlaying) Destroy(col); else DestroyImmediate(col);
            }

            Renderer ren = pool.GetComponent<Renderer>();
            // ITS OWN MATERIAL: the colour is inside the asset, no property
            // block. Warm white and fainter than the street pool - the sum of
            // dozens of pools across eight rooms was saturating the floor to
            // white.
            ren.sharedMaterial = _ceilMat;
            ren.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ren.receiveShadows = false;
            pool.SetActive(false);
            _roomGlow.Add(pool);
        }

        /// <summary>The target spacing between two ceiling lamps (m).</summary>
        private const float RoomLampSpan = 2.30f;

        /// <summary>
        /// A street lamp: a classic post with an octagonal base and a
        /// lantern.
        ///
        /// FROM BOXES TO A MESH. The previous lamp was THREE BOXES (post, arm,
        /// head) and at phone size it read not as "a lamp" but as "a white dot
        /// on the end of a thin stick". The example the user brought was a
        /// classic street lantern: an octagonal base, a body tapering as it
        /// rises, a curving arm and an octagonal glazed lantern.
        ///
        /// All the parts are merged into A SINGLE MESH: one metal mesh, one
        /// glass mesh. Fifteen separate boxes meant fifteen draw calls;
        /// merging brings it down to two per lamp, and the three lamps SHARE
        /// the same two meshes (the Mesh is built once).
        ///
        /// NO shadow: the post is thin and its shadow falls across the floor
        /// plan; it asks for one more shadow map and adds nothing to the
        /// game's readability.
        /// </summary>
        private void StreetLamp(float x)
        {
            GameObject root = new GameObject("StreetLamp");
            root.transform.SetParent(transform, false);
            _lampX.Add(x);
            // The post stands ON THE KERB. -1.30 was inside the tarmac: a post
            // standing in the middle of the road. A real street lamp sits on
            // the kerb.
            root.transform.localPosition = new Vector3(x, 0f, LampPostZ);

            if (_lampMetalMesh == null) BuildLampMeshes();

            // --- the metal body ---
            GameObject metal = new GameObject("Body");
            metal.transform.SetParent(root.transform, false);
            MeshFilter mf = metal.AddComponent<MeshFilter>();
            mf.sharedMesh = _lampMetalMesh;
            MeshRenderer mr = metal.AddComponent<MeshRenderer>();
            mr.sharedMaterial = _floorMat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            if (_block == null) _block = new MaterialPropertyBlock();
            mr.GetPropertyBlock(_block);
            _block.SetColor(BaseColorId, LampIron);
            mr.SetPropertyBlock(_block);

            // --- the glass lantern ---
            //
            // A material with emission is ESSENTIAL: while the keyword is off
            // the shader never reads the emission field, so the colour written
            // in the evening to "light the lamp" does nothing.
            if (_lampMat == null && _floorMat != null)
            {
                _lampMat = new Material(_floorMat);
                _lampMat.EnableKeyword("_EMISSION");
                _lampMat.globalIlluminationFlags =
                    MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            GameObject glass = new GameObject("Glass");
            glass.transform.SetParent(root.transform, false);
            MeshFilter cf = glass.AddComponent<MeshFilter>();
            cf.sharedMesh = _lampGlassMesh;
            MeshRenderer cr = glass.AddComponent<MeshRenderer>();
            cr.sharedMaterial = _lampMat != null ? _lampMat : _floorMat;
            cr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _lampHeads.Add(cr);

            // THE POOL OF LIGHT: NOT A REAL LIGHT.
            //
            // A point light was put here once and it did NOTHING: additional
            // lights are OFF in the URP asset
            // (m_AdditionalLightsRenderingMode: 0, the docs/19 mobile budget).
            // A light that stands in the scene but never enters the drawing
            // means looking at "the lamp is lit" and seeing nothing at all -
            // and nothing warns you.
            //
            // In its place THREE LAYERS, all of them unlit and additively
            // blended:
            //
            //   1. THE CONE - the beam of light coming down from the lantern to
            //      the ground. This is the one part that turns the night from
            //      "a dark street + white dots" into "lamps that give light":
            //      that the light COMES OUT OF A SOURCE is only visible through
            //      the beam.
            //   2. THE HALO - the glow around the lantern. The glass itself is
            //      small; the halo makes it visible at phone size.
            //   3. THE POOL - the bright patch lying on the pavement.
            //
            // All three come on at the SAME moment in the evening
            // (DayLight.Lamps).
            LampBeam(root.transform);
            LampHalo(root.transform);
            LampPool(root.transform);
        }

        /// <summary>Cast iron: almost black, as in the reference.</summary>
        private static readonly Color LampIron = new Color(0.13f, 0.13f, 0.15f);

        /// <summary>
        /// The lantern's axis: THE TOP OF THE POST, that is, no offset.
        ///
        /// The curving arm was removed (the user's request); the number stays
        /// because all the light parts are read from it - if the lantern is
        /// ever to be offset again, it moves from one place.
        /// </summary>
        private const float LampHeadZ = 0f;

        /// <summary>The middle height of the glass lantern.</summary>
        private const float LampGlassY = 1.875f;

        private Mesh _lampMetalMesh;
        private Mesh _lampGlassMesh;

        /// <summary>
        /// The beam of light coming down from the lantern to the ground.
        ///
        /// A trick so simple it has almost NO texture: the MIDDLE ROW of the
        /// pool of light's round texture is read - u=0.5 is the centre
        /// (bright), u=1 the edge (transparent). On the cone u grows from top
        /// to bottom, so the beam is bright at the lantern and fades out at
        /// the ground.
        ///
        /// Why like this: transparent material is STRIPPED FROM THE BUILD;
        /// only the shader variant a .mat ASSET points at gets in. Using the
        /// existing pool material instead of a new one means never walking
        /// into that trap.
        /// </summary>
        private void LampBeam(Transform root)
        {
            Mesh m = new Mesh();
            m.name = "LampBeam";
            var vs = new List<Vector3>();
            var ns = new List<Vector3>();
            var uv = new List<Vector2>();
            var ts = new List<int>();

            const int sides = 8;
            const float top = 1.70f, bottom = 0.02f;
            const float rTop = 0.22f, rBottom = 1.45f;
            for (int i = 0; i < sides; i++)
            {
                float a0 = Mathf.PI * 2f * i / sides;
                float a1 = Mathf.PI * 2f * (i + 1) / sides;
                Vector3 u0 = new Vector3(Mathf.Cos(a0) * rTop, top,
                                         Mathf.Sin(a0) * rTop + LampHeadZ);
                Vector3 u1 = new Vector3(Mathf.Cos(a1) * rTop, top,
                                         Mathf.Sin(a1) * rTop + LampHeadZ);
                Vector3 a2 = new Vector3(Mathf.Cos(a0) * rBottom, bottom,
                                         Mathf.Sin(a0) * rBottom + LampHeadZ);
                Vector3 a3 = new Vector3(Mathf.Cos(a1) * rBottom, bottom,
                                         Mathf.Sin(a1) * rBottom + LampHeadZ);
                int b = vs.Count;
                vs.Add(u0); vs.Add(u1); vs.Add(a2); vs.Add(a3);
                Vector3 n = Vector3.Cross(u1 - u0, a2 - u0).normalized;
                ns.Add(n); ns.Add(n); ns.Add(n); ns.Add(n);
                // u: 0.62 at the lantern (close to bright), 0.98 at the ground (faint).
                uv.Add(new Vector2(0.62f, 0.5f)); uv.Add(new Vector2(0.62f, 0.5f));
                uv.Add(new Vector2(0.98f, 0.5f)); uv.Add(new Vector2(0.98f, 0.5f));
                ts.Add(b); ts.Add(b + 2); ts.Add(b + 1);
                ts.Add(b + 1); ts.Add(b + 2); ts.Add(b + 3);
                // VISIBLE FROM THE INSIDE TOO: were the cone single-sided, half
                // of it would disappear depending on the camera angle.
                ts.Add(b); ts.Add(b + 1); ts.Add(b + 2);
                ts.Add(b + 1); ts.Add(b + 3); ts.Add(b + 2);
            }
            m.SetVertices(vs);
            m.SetNormals(ns);
            m.SetUVs(0, uv);
            m.SetTriangles(ts, 0);
            m.RecalculateBounds();

            GameObject go = new GameObject("Beam");
            go.transform.SetParent(root, false);
            go.AddComponent<MeshFilter>().sharedMesh = m;
            MeshRenderer r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = _glowMat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            // THE BEAM IS STRONGER THAN THE POOL.
            //
            // The pool material's alpha is 0.55 and in the first screenshot the
            // beam was NOT VISIBLE - a slab lying flat is wide at a glance,
            // while a cone standing upright is very thin. The same material,
            // a different strength: a property block throws three renderers out
            // of batching, an acceptable price for three renderers.
            if (_block == null) _block = new MaterialPropertyBlock();
            r.GetPropertyBlock(_block);
            _block.SetColor(BaseColorId, new Color(1.00f, 0.82f, 0.48f, 0.62f));
            r.SetPropertyBlock(_block);
            go.SetActive(false);
            _lampGlow.Add(go);
        }

        /// The glow around the lantern.
        ///
        /// IT FOLLOWS THE CAMERA (FaceCamera). It used to be built at a fixed
        /// angle, and the reason was "the game's camera angle is fixed, only
        /// its position changes" - which stopped being true when two-finger
        /// turning was added (CameraRig +-35 degrees). Three rotations a
        /// frame for three lanterns is well worth the price.
        /// </summary>
        private void LampHalo(Transform root)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = "Halo";
            go.transform.SetParent(root, false);
            // IN FRONT OF THE LANTERN: put behind it, the lantern's own body
            // covers the halo and the halo is never seen.
            // It is shifted a touch towards the camera (the camera is on the
            // street side, that is at -Z): so that the lantern's body does not
            // cover the middle of the halo.
            go.transform.localPosition = new Vector3(0f, LampGlassY, LampHeadZ - 0.12f);
            // THE QUAD'S FACE POINTS AT -Z.
            //
            // Giving it the camera's angle directly turns the slab the WRONG
            // WAY ROUND and, because the back face is culled, the halo was
            // never drawn - in the screenshot it looked like "there is no
            // halo", when the halo was there with its back turned. The 180
            // degree correction is inside FaceCamera; the pool slab does not
            // fall into this trap because its 90 degree rotation leaves it
            // facing upwards.
            go.transform.localRotation = CameraFit.Rotation
                                         * Quaternion.Euler(0f, 180f, 0f);
            go.AddComponent<FaceCamera>();
            // HDR IS OFF (LokantaURP m_SupportsHDR: 0), so there is no such
            // thing as bloom: emission above 1 is only clipped to white. The
            // glow around the lantern IS THIS SLAB - which is why it is not
            // small but twice the width of the lantern.
            go.transform.localScale = new Vector3(1.35f, 1.35f, 1f);

            Collider c = go.GetComponent<Collider>();
            if (c != null)
            {
                if (Application.isPlaying) Destroy(c); else DestroyImmediate(c);
            }
            Renderer r = go.GetComponent<Renderer>();
            r.sharedMaterial = _glowMat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            if (_block == null) _block = new MaterialPropertyBlock();
            r.GetPropertyBlock(_block);
            _block.SetColor(BaseColorId, new Color(1.00f, 0.84f, 0.52f, 0.75f));
            r.SetPropertyBlock(_block);
            go.SetActive(false);
            _lampGlow.Add(go);
        }

        /// <summary>The bright patch lying on the pavement.</summary>
        private void LampPool(Transform root)
        {
            GameObject pool = GameObject.CreatePrimitive(PrimitiveType.Quad);
            pool.name = "LightPool";
            pool.transform.SetParent(root, false);
            // The pool falls in the MIDDLE of the pavement (the post at -1.52,
            // the pool at +0.80 => z = -0.72 = Paths.PavementZ): the light has
            // to light the place that is walked on, not the road.
            // The pool sits UNDER the lantern (the post's axis). The post is on
            // the kerb but the pool is 2.9 m deep: the pavement's walking lane
            // (Paths.PavementZ +- 0.35, that is between -1.07 and -0.37) stays
            // inside the pool - the light has to light THE PLACE THAT IS WALKED
            // ON.
            pool.transform.localPosition = new Vector3(0f, 0.012f, LampHeadZ);
            pool.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            // LONG ALONG THE STREET. A circular patch read as "a circle lying
            // on the ground"; a real lamp's light leaves an ellipse stretched
            // along the pavement.
            pool.transform.localScale = new Vector3(3.60f, 2.90f, 1f);

            Collider hcol = pool.GetComponent<Collider>();
            if (hcol != null)
            {
                if (Application.isPlaying) Destroy(hcol); else DestroyImmediate(hcol);
            }
            Renderer hr = pool.GetComponent<Renderer>();
            hr.sharedMaterial = _glowMat;
            hr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            hr.receiveShadows = false;
            pool.SetActive(false);
            _lampGlow.Add(pool);
        }

        /// Builds the lamp's two meshes: metal and glass.
        ///
        /// The measurements follow the figure: the figures are 1.10 m, the
        /// lamp 1.86 m. In real life the ratio is larger (a person 1.7 - a
        /// lamp 4.5) but the game's whole scale is compressed; what matters
        /// here is that the lantern stays ABOVE HEAD HEIGHT (its underside at
        /// 1.31 m) - otherwise it looks as if it passes through the figures
        /// walking on the pavement.
        /// </summary>
        private void BuildLampMeshes()
        {
            var v = new List<Vector3>();
            var n = new List<Vector3>();
            var t = new List<int>();

            // --- the base ----------------------------------------------
            Prism(v, n, t, 0.185f, 0.165f, 0.000f, 0.050f, 0f, 0f);   // the base plate
            Prism(v, n, t, 0.155f, 0.088f, 0.050f, 0.310f, 0f, 0f);   // the tapered base
            Prism(v, n, t, 0.105f, 0.098f, 0.360f, 0.050f, 0f, 0f);   // the collar
            // --- the body ----------------------------------------------
            Prism(v, n, t, 0.052f, 0.042f, 0.410f, 1.210f, 0f, 0f);   // the post
            Prism(v, n, t, 0.068f, 0.062f, 0.950f, 0.055f, 0f, 0f);   // the middle collar
            Prism(v, n, t, 0.058f, 0.050f, 1.585f, 0.045f, 0f, 0f);   // the upper collar
            // --- the lantern: ON TOP OF THE POST ------------------------
            //
            // It used to hang over the pavement on a curving arm (the middle
            // model in the reference). The user wanted it on top - the
            // rightmost model in the reference - and the game's camera bears
            // that out: the arm reached towards +Z, that is TOWARDS THE
            // CAMERA, and at the fixed angle it foreshortened away
            // completely. An invisible arm made the lantern look "stuck into
            // the post"; a lantern on top is a lantern from every angle.
            //
            // The light moved with the lamp (z = 0): the pool is 2.8 m deep
            // and the post is on the kerb - so the pavement's walking lane
            // (Paths.PavementZ +- 0.35) stays INSIDE the pool.
            float z = LampHeadZ;
            // THE LANTERN GREW (the radii x1.25).
            //
            // At the first sizing the model was right but the SCALE was wrong:
            // in the game's real framing the lamp is 40 pixels and the lantern
            // a fifth of that - a blotch eight pixels across. It was chosen by
            // looking at the screenshot; the ratio that was right on paper did
            // not read on screen.
            Prism(v, n, t, 0.072f, 0.066f, 1.600f, 0.070f, z, 0f);    // the lantern's seat
            Prism(v, n, t, 0.190f, 0.160f, 1.660f, 0.045f, z, 0f);    // the lower skirt
            Prism(v, n, t, 0.188f, 0.181f, 2.045f, 0.040f, z, 0f);    // the cap collar
            Prism(v, n, t, 0.225f, 0.088f, 2.085f, 0.110f, z, 0f);    // the tapered cap
            Prism(v, n, t, 0.038f, 0.018f, 2.195f, 0.060f, z, 0f);    // the finial

            _lampMetalMesh = Build("LampMetal", v, n, t);

            // --- the glass ---------------------------------------------
            //
            // The lantern's glass: wide at the bottom, narrow at the top - the
            // octagonal lantern of the reference itself. The metal's collars
            // fall above and below the glass, so the glass reads as "framed".
            v.Clear(); n.Clear(); t.Clear();
            Prism(v, n, t, 0.178f, 0.148f, 1.705f, 0.340f, LampHeadZ, 0f);
            _lampGlassMesh = Build("LampGlass", v, n, t);

            // THE UNDERSIDE OF THE LANTERN IS MEASURED, NOT WRITTEN DOWN.
            //
            // The lantern stands RIGHT ABOVE the pavement; the figures passing
            // under it are 1.10 m. Saying "the measurements were chosen
            // correctly" is not enough - somebody changing the height of a part
            // (me enlarging the lantern, for instance) could break it without
            // noticing. The number is read FROM THE MESH, that is, from the
            // thing that is drawn.
            // THE MEASURE THAT PICKS THE LANTERN OUT: BEING WIDER THAN THE POST.
            //
            // It used to be picked out as "the corners close to the lantern on
            // the z axis", and while the lantern was on the end of the ARM that
            // was a correct distinction. Once the lantern moved on top of the
            // post the same condition picks out the whole lamp - the base
            // included - and the measurement comes back as 0.00 m. Instead of a
            // condition, the SILHOUETTE: the post's radius is 0.052; anything
            // wider than that and above waist height is the lantern's body.
            float lowest = float.MaxValue;
            ScanLantern(_lampMetalMesh, ref lowest);
            ScanLantern(_lampGlassMesh, ref lowest);
            LampLanternBottom = lowest == float.MaxValue ? 0f : lowest;
        }

        /// <summary>The lowest point of the lantern's body.</summary>
        private static void ScanLantern(Mesh m, ref float lowest)
        {
            if (m == null) return;
            Vector3[] vs = m.vertices;
            for (int i = 0; i < vs.Length; i++)
            {
                Vector3 p = vs[i];
                if (p.y < 0.90f) continue;                       // the base and the body
                float r = Mathf.Sqrt(p.x * p.x + (p.z - LampHeadZ) * (p.z - LampHeadZ));
                if (r < 0.10f) continue;                          // the post and the collars
                if (p.y < lowest) lowest = p.y;
            }
        }

        /// The lantern's lowest point (m). So the tour can ask: the heads of
        /// the figures passing underneath have to stay below it.
        /// </summary>
        public float LampLanternBottom { get; private set; }

        private static Mesh Build(string name, List<Vector3> v, List<Vector3> n,
                                  List<int> t)
        {
            Mesh m = new Mesh();
            m.name = name;
            m.SetVertices(v);
            m.SetNormals(n);
            m.SetTriangles(t, 0);
            m.RecalculateBounds();
            return m;
        }

        /// An octagonal prism. For the parts that stand upright.
        ///
        /// FLAT SHADING: every face has its own corners, so the edges are
        /// SHARP. Shared corners would give a soft cylinder - every model in
        /// this game is low-poly and hard-edged.
        /// </summary>
        private static void Prism(List<Vector3> v, List<Vector3> n, List<int> t,
                                  float rBottom, float rTop, float y0, float h,
                                  float z, float x)
        {
            PrismAt(v, n, t, rBottom, rTop, h, new Vector3(x, y0, z),
                    Quaternion.identity);
        }

        /// <summary>An octagonal prism, at the given place and angle.</summary>
        private static void PrismAt(List<Vector3> v, List<Vector3> n, List<int> t,
                                    float rBottom, float rTop, float h,
                                    Vector3 pos, Quaternion rot)
        {
            const int sides = 8;
            Matrix4x4 m = Matrix4x4.TRS(pos, rot, Vector3.one);

            for (int i = 0; i < sides; i++)
            {
                float a0 = Mathf.PI * 2f * i / sides;
                float a1 = Mathf.PI * 2f * (i + 1) / sides;
                Vector3 p0 = m.MultiplyPoint3x4(
                    new Vector3(Mathf.Cos(a0) * rBottom, 0f, Mathf.Sin(a0) * rBottom));
                Vector3 p1 = m.MultiplyPoint3x4(
                    new Vector3(Mathf.Cos(a1) * rBottom, 0f, Mathf.Sin(a1) * rBottom));
                Vector3 p2 = m.MultiplyPoint3x4(
                    new Vector3(Mathf.Cos(a0) * rTop, h, Mathf.Sin(a0) * rTop));
                Vector3 p3 = m.MultiplyPoint3x4(
                    new Vector3(Mathf.Cos(a1) * rTop, h, Mathf.Sin(a1) * rTop));

                int b = v.Count;
                v.Add(p0); v.Add(p1); v.Add(p2); v.Add(p3);
                Vector3 nn = Vector3.Cross(p1 - p0, p2 - p0).normalized;
                n.Add(nn); n.Add(nn); n.Add(nn); n.Add(nn);
                t.Add(b); t.Add(b + 2); t.Add(b + 1);
                t.Add(b + 1); t.Add(b + 2); t.Add(b + 3);
            }

            // The caps: the top always, the bottom only visible on parts that
            // taper - but both are four triangles, not worth counting.
            Cap(v, n, t, m, rTop, h, true);
            Cap(v, n, t, m, rBottom, 0f, false);
        }

        private static void Cap(List<Vector3> v, List<Vector3> n, List<int> t,
                                Matrix4x4 m, float r, float y, bool top)
        {
            const int sides = 8;
            int b = v.Count;
            Vector3 nn = m.MultiplyVector(top ? Vector3.up : Vector3.down);
            for (int i = 0; i < sides; i++)
            {
                float a = Mathf.PI * 2f * i / sides;
                v.Add(m.MultiplyPoint3x4(
                    new Vector3(Mathf.Cos(a) * r, y, Mathf.Sin(a) * r)));
                n.Add(nn);
            }
            for (int i = 1; i < sides - 1; i++)
            {
                if (top) { t.Add(b); t.Add(b + i); t.Add(b + i + 1); }
                else { t.Add(b); t.Add(b + i + 1); t.Add(b + i); }
            }
        }

        /// The people passing along the street. NOT guests: the core does not
        /// know about them.
        ///
        /// The seed is fixed: the tour has to see the same street on every
        /// run, otherwise the "is there a pair chatting" check is green on one
        /// run and red on the next - and an unstable check is not a check.
        /// </summary>
        private void BuildStreetLife()
        {
            if (_streetLife == null) _streetLife = gameObject.GetComponent<StreetLife>();
            if (_streetLife == null) _streetLife = gameObject.AddComponent<StreetLife>();
            _streetLife.Build(transform, CustomerPrefabs, 20260912);
        }

        private StreetLife _streetLife;

        /// <summary>The number of people passing on the street. So the tour can ask.</summary>
        public int StreetWalkers { get { return _streetLife == null ? 0 : _streetLife.Count; } }

        /// <summary>The number of passers-by chatting. So the tour can ask.</summary>
        public int StreetChatting
        {
            get { return _streetLife == null ? 0 : _streetLife.Chatting; }
        }

        private Material _lampMat;
        private readonly List<Renderer> _lampHeads = new List<Renderer>();
        private Material _glowMat;
        private Material _ceilMat;
        private Material _waterMat;
        private readonly List<GameObject> _lampGlow = new List<GameObject>();
        private readonly List<GameObject> _roomGlow = new List<GameObject>();
        private readonly List<float> _lampX = new List<float>();
        private readonly List<Renderer> _streetSlabs = new List<Renderer>();
        private readonly List<Color> _streetBase = new List<Color>();

        /// How many times it has been built. The daylight component rebinds
        /// the street lamps by looking at this: when the restaurant grows the
        /// lamps are created again and the old references pointed at destroyed
        /// objects - the lamps did not light in the evening and nothing warned
        /// about it.
        /// </summary>
        public int BuildStamp { get; private set; }

        /// <summary>The street lamps. Handed to the daylight component.</summary>
        public Renderer[] LampHeads { get { return _lampHeads.ToArray(); } }

        /// <summary>The lamps' pools of light on the ground.</summary>
        public GameObject[] LampGlow { get { return _lampGlow.ToArray(); } }

        /// <summary>The restaurant's inside ceiling lights. No body, only light.</summary>
        public GameObject[] RoomGlow { get { return _roomGlow.ToArray(); } }

        /// <summary>
        /// The LARGEST DEVIATION in the street lamps' spacing (m).
        ///
        /// The user's complaint: "let the street lights be at the start, in
        /// the middle and at the end; at the moment it is not symmetric and
        /// the distance between them looks bad". The measurable form of that
        /// is how different the distances between neighbouring posts are: zero
        /// in a symmetric row.
        ///
        /// Why it was turned into a number: the old code placed four posts
        /// and, when one of them fell in front of the door, shifted it by 2.2
        /// m - the spacings came out as 2.3 / 4.5 / 4.5. The shift was A
        /// SINGLE LINE in the code and looked innocent; a case where what
        /// spoiled the screen could not be seen by reading the code, only by
        /// measuring.
        /// </summary>
        public float LampSpacingError
        {
            get
            {
                if (_lampX.Count < 3) return 0f;
                float smallest = float.MaxValue, largest = 0f;
                for (int i = 1; i < _lampX.Count; i++)
                {
                    float d = Mathf.Abs(_lampX[i] - _lampX[i - 1]);
                    if (d < smallest) smallest = d;
                    if (d > largest) largest = d;
                }
                return largest - smallest;
            }
        }

        /// <summary>The number of street lamps.</summary>
        public int LampPostCount { get { return _lampX.Count; } }

        /// <summary>
        /// The number of light parts on the lamps (beam + halo + pool).
        ///
        /// If one of the three is forgotten the night is quietly poorer: the
        /// light still looks "there", only weaker - and a weak light reads as
        /// a choice rather than as a bug.
        /// </summary>
        public int LampGlowCount { get { return _lampGlow.Count; } }

        /// <summary>
        /// The street lamp post's z: on top of the kerb.
        ///
        /// The outer pedestrian lane is at -1.07; the 0.45 m between them is
        /// larger than the smallest distance StreetLife uses for a post
        /// (PostClear 0.40). It has to be larger: were it smaller, every
        /// pedestrian in the outer lane would be pushed inwards constantly and
        /// the lane would be useless.
        /// </summary>
        public const float LampPostZ = -1.52f;

        /// <summary>
        /// The local positions of the FIXED obstacles on the street - the
        /// posts.
        ///
        /// Why it was needed: the placement audit found pedestrians
        /// overlapping the posts. Most of the overlap came from the figure's
        /// box measuring the arm span, but there is a real post at body height
        /// and a pedestrian walking through it is the very bug the user
        /// reported. Not pathfinding: the pushing takes them aside, and that
        /// is how people react to a post too.
        /// </summary>
        public void StreetObstacles(List<Vector3> into)
        {
            if (into == null) return;
            for (int i = 0; i < _lampX.Count; i++)
                into.Add(new Vector3(_lampX[i], 0f, LampPostZ));

            // THE TERRACE TABLES ARE OBSTACLES TOO.
            //
            // Two tables were put on the pavement (decoration) and the pavement
            // is also where the pedestrians walk: if they were not recorded, the
            // passers-by would walk THROUGH the table. Putting something in the
            // scene means making it part of the path.
            for (int i = 0; i < _patio.Count; i++) into.Add(_patio[i]);
        }

        private void Street(string name, float z0, float z1, Color c)
        {
            _streetBase.Add(c);
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(
                RoomPlan.PlotW * 0.5f, -0.05f, (z0 + z1) * 0.5f);
            go.transform.localScale = new Vector3(
                RoomPlan.PlotW + 2.4f, 0.1f, z1 - z0);

            Collider col = go.GetComponent<Collider>();
            if (col != null)
            {
                if (Application.isPlaying) Destroy(col); else DestroyImmediate(col);
            }

            if (_block == null) _block = new MaterialPropertyBlock();
            Renderer ren = go.GetComponent<Renderer>();
            ren.sharedMaterial = _floorMat;
            ren.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ren.GetPropertyBlock(_block);
            _block.SetColor(BaseColorId, c);
            ren.SetPropertyBlock(_block);
            _streetSlabs.Add(ren);
        }

        /// DARKENS THE STREET SLABS AT NIGHT. k = 1 day, 0 night.
        ///
        /// Why it was needed - and this was MEASURED, not judged by eye: in
        /// the evening screenshot the hall's MEDIAN brightness was 46 and the
        /// street's average 51. That is, the inside of the restaurant was
        /// darker than the pavement in front of it. This is the numerical form
        /// of the "it is not bright enough inside" complaint.
        ///
        /// The reason is simple: every lever I have is GLOBAL. The ambient,
        /// the fill, the warm key - all of them light the pavement as much as
        /// the restaurant, because additional (local) lights are off in URP
        /// and so are light layers. The only way to make the inside brighter
        /// relative to the outside is to DARKEN THE OUTSIDE locally.
        ///
        /// It is the right thing in reality too: a pavement at night is as
        /// bright as the light falling on it - the same tarmac that was there
        /// in the day is nearly black at night. Nothing changes during the
        /// day.
        /// </summary>
        public void TintStreet(float k)
        {
            _streetTint = k;
            if (_streetSlabs.Count == 0) return;
            if (_block == null) _block = new MaterialPropertyBlock();

            for (int i = 0; i < _streetSlabs.Count && i < _streetBase.Count; i++)
            {
                if (_streetSlabs[i] == null) continue;
                Color c = _streetBase[i] * k;
                c.a = 1f;
                _streetSlabs[i].GetPropertyBlock(_block);
                _block.SetColor(BaseColorId, c);
                _streetSlabs[i].SetPropertyBlock(_block);
            }
        }

        /// <summary>The number of street slabs. So the tour can ask.</summary>
        public int StreetSlabCount { get { return _streetSlabs.Count; } }

        /// The tint last applied to the street. 1 day, a small value night.
        ///
        /// So the tour can ask: this single number is the mechanism behind the
        /// claim "the inside of the restaurant is brighter than its outside".
        /// That raising the global and lowering the local works is only
        /// visible when the two are measured together - a check that looked
        /// only at the inside light would stay green while the street glowed
        /// just as brightly.
        /// </summary>
        public float StreetTint { get { return _streetTint; } }

        private float _streetTint = 1f;

        // WHAT THE TOUR WILL ASK.
        //
        // None of these three properties turned into a number: a wall is
        // either there or not, a cook either faces the stove or does not, an
        // animation either runs or freezes. What cannot be asked cannot be
        // checked either - and in this project everything that is not
        // measured has broken silently at least once.

        /// <summary>The number of room walls in the scene.</summary>
        public int WallCount
        {
            get
            {
                int n = 0;
                foreach (Transform t in transform) if (t.name == "Wall") n++;
                return n;
            }
        }

        /// Are the walls really transparent AND without colliders?
        ///
        /// Both can break silently: _Surface on its own is only an inspector
        /// setting in URP (exactly what happened with the oven glass), and if
        /// a collider is left, touching the room stops working.
        /// </summary>
        public bool WallsClear
        {
            get
            {
                // WHAT IS BEING ASKED: is the material IN THE TRANSPARENT PASS
                // and is it really half transparent?
                //
                // It used to ask whether _SrcBlend was SrcAlpha, and it went red
                // when the materials moved into a .mat ASSET: for an asset URP
                // writes the blending fields ITSELF (deriving them from
                // _Surface/_Blend) and it wrote One + OneMinusSrcAlpha - that is,
                // premultiplied alpha, and the transparency was drawn correctly on
                // screen. The check was interrogating a detail that belongs to
                // URP.
                //
                // The facts the author controls: the transparent surface type, the
                // transparent draw queue and a low alpha.
                if (_wallMat == null) return false;
                if (!_wallMat.HasProperty("_Surface")) return false;
                if ((int)_wallMat.GetFloat("_Surface") != 1) return false;
                if (_wallMat.renderQueue < 2900) return false;
                if (_wallMat.GetColor(BaseColorId).a > 0.5f) return false;

                foreach (Transform t in transform)
                    if (t.name == "Wall" && t.GetComponent<Collider>() != null)
                        return false;
                return true;
            }
        }

        /// The number of staff doing kitchen work, and the number whose
        /// ANIMATOR IS RUNNING.
        ///
        /// The second is essential: Figure switches the Animator off ~1 s
        /// after the transition (which is right for a seated guest), and
        /// that is why the cook froze on the first frame of the chopping
        /// clip.
        /// </summary>
        public void KitchenWork(out int working, out int animated)
        {
            working = 0;
            animated = 0;
            for (int i = 0; i < _staff.Count; i++)
            {
                Figure f = FigureOf(_staff[i]);
                if (f == null) continue;
                if (f.Current != Figure.Pose.Chop && f.Current != Figure.Pose.Wash
                    && f.Current != Figure.Pose.Serve) continue;
                working++;
                if (f.Anim != null && f.Anim.enabled) animated++;
            }
        }

        /// The ERROR in the cooks' facing towards the stove, for the cooks
        /// doing kitchen work (degrees).
        ///
        /// -1: there is no cook working at the moment. A large number means
        /// the cook is using the stove with its back turned - the measurable
        /// form of what the user meant by "it keeps looking that way".
        /// </summary>
        public float CookFacingErrorDeg
        {
            get
            {
                float worst = -1f;
                for (int i = 0; i < _staff.Count && i < _staffCooks; i++)
                {
                    Figure f = FigureOf(_staff[i]);
                    if (f == null) continue;
                    if (f.Current != Figure.Pose.Chop && f.Current != Figure.Pose.Wash
                        && f.Current != Figure.Pose.Serve) continue;

                    Transform t = _staff[i].transform;

                    // THE TARGET COMES FROM THE STAGE: while washing it faces the
                    // counter, while cooking the stove. Counting them all as "the
                    // stove" would make a cook that is standing correctly look
                    // wrong.
                    CookRoutine cr = i < _cookRoutine.Count ? _cookRoutine[i] : null;
                    Vector3 lookAt = cr != null && cr.Busy
                        ? cr.LookTarget
                        : StovePos(i, t.localPosition);
                    Vector3 target = lookAt - t.localPosition;
                    target.y = 0f;
                    if (target.sqrMagnitude < 0.01f) continue;

                    float angle = Vector3.Angle(t.forward, target);
                    if (angle > worst) worst = angle;
                }
                return worst;
            }
        }

        private int _staffCooks;

        /// The number of frames in which a working staff member's work clip
        /// advanced and in which it was FROZEN. So the tour can ask;
        /// cumulative.
        /// </summary>
        public static int WorkAnimAdvanced, WorkAnimStalled;

        private readonly Dictionary<int, float> _workClip =
            new Dictionary<int, float>();

        /// The room's floor colour.
        ///
        /// The floor now has a PATTERN (FloorPattern) and the pattern sits on
        /// top of this slab; even so it shows at the edges and between the
        /// pattern. A base colour out of tune with the identity's palette made
        /// the whole room look like "the wrong cuisine" - a warm brown floor
        /// in fast food, a cold grey floor in the Turkish one.
        ///
        /// The service rooms are still SEPARATE: the kitchen and the wash room
        /// colder, the store darker. The player has to be able to tell "this
        /// is the back of house".
        /// </summary>
        private Color RoomColor(in RoomPlan.Room r)
        {
            Palette p = Pal(CuisineId);

            // THE DINING ROOM IS NO LONGER THE DARKEST FLOOR IN THE BUILDING.
            //
            // It used to take FloorDark - the darkest tone in the palette -
            // so the one room the player is asked to watch had less light
            // coming back off it than the store room. Combined with a sky
            // brighter than the interior (DayLight.cs), the subject of the
            // picture was its darkest region.
            //
            // The back of house still reads as the back of house: the lerps
            // below start from FloorDark and stay where they were, so the
            // kitchen and the wash room are colder and the store darker than
            // the hall, which is the distinction this method exists to make.
            // What changes is that the hall is now the brightest floor rather
            // than the dimmest.
            if (r.IsDining) return p.Floor;
            if (r.Name == "Kitchen" || r.Name == "Sink")
                return Color.Lerp(p.FloorDark, RoomKitchen, 0.65f);
            return Color.Lerp(p.FloorDark, RoomService, 0.65f);
        }

        /// The furnishings of the service rooms. The kitchen has stoves and a
        /// cupboard, the wash room a sink, the store shelves. The hall rooms
        /// are empty - their furniture is the tables.
        /// </summary>
        private void BuildRoomProps(int tables)
        {
            foreach (RoomPlan.Room r in RoomPlan.Rooms)
            {
                // NO EQUIPMENT IS PUT IN A CLOSED ROOM.
                //
                // Harmless today: in the plan the kitchen, the entrance, the wash
                // room and the store have IsDining == false, so they are always
                // open (RoomPlan.cs). But if a service room with tables were
                // added to the plan, a stove and a sink would be placed SILENTLY
                // in a closed room - equipment hanging in a gap with no walls. A
                // single line closes the trap now.
                if (!RoomPlan.RoomOpen(in r, tables)) continue;

                switch (r.Name)
                {
                    case "Kitchen":
                        // THE FRIDGE IS IN THE BACK RIGHT CORNER, and the row of stoves
                        // leaves room for it.
                        //
                        // Both used to be in the back LEFT corner: the placement audit
                        // measured a 0.30 m overlap between the stove and the fridge -
                        // the fridge stood inside the first stove and in the general
                        // view they read as a single shapeless mass.
                        LineUp(r, StovePrefab, 3, 0.55f, 180f, rightInset: 1.15f,
                               appliance: true);
                        // THE FRONT ROW OF COUNTERS WAS REMOVED.
                        //
                        // The SERVICE COUNTER came in its place
                        // (RestaurantView.Decor) and the two stood in the SAME place: in
                        // the screenshot the counter boxes came out through the service
                        // counter and the pans hung in the air above it. Without a
                        // close-up it would not have been noticed.
                        //
                        // The service counter is a better counter anyway: it has a top, a
                        // row of trays and a glass screen. The cook's working posts are
                        // derived from the stoves (KitchenPosts), so removing the row of
                        // counters leaves nobody out of work.
                        // On the right wall: its face at -X, that is, towards the room.
                        // While it was on the left wall 90 was right; it was corrected
                        // when it moved.
                        Place(FridgePrefab, r.X0 + r.W - 0.55f, r.Z0 + r.D - 0.7f, -90f);
                        break;
                    case "Sink":
                        BuildDishStation(r);
                        // THE FRONT CORRIDOR HAS TO STAY CLEAR (Paths.LaneZ = 0.55).
                        // The counter was at z=0.7, standing right on the corridor;
                        // the guests and the waiters walk through there. It was moved
                        // to the middle of the room - which is the right place for a
                        // prep counter anyway.
                        Place(CounterPrefab, r.CenterX, r.Z0 + 2.2f, 0f);
                        break;
                    case "Store":
                        LineUp(r, ShelfPrefab, 2, 0.6f, 180f);
                        Place(FridgePrefab, r.X0 + r.W - 0.7f, r.Z0 + 0.8f, -90f);
                        break;
                    case "Entry":
                        // THE DOOR IS ON THE FRONT EDGE, in line with Paths.DoorX.
                        // Where the guest walks in and where the door stands come from
                        // the same number; writing it in two places has drifted apart
                        // silently five times in this project.
                        Threshold(r);
                        Place(CounterPrefab, r.CenterX, r.Z0 + r.D - 0.8f, 180f);
                        // The plant pots go in the BACK corner: the front edge is now
                        // the door and the corridor, that is, the way through (Paths).
                        Place(PlantPrefab, r.X0 + 0.55f, r.Z0 + r.D - 0.7f, 0f);
                        Place(PlantPrefab, r.X0 + r.W - 0.55f, r.Z0 + r.D - 0.7f, 0f);
                        break;
                }
            }
        }

        /// THE WASH ROOM: the sinks, the dirty stack, the clean stack, the
        /// washing spot.
        ///
        /// All of it in ONE FUNCTION, because it all derives from one
        /// another's position: the washing figure stands in front of the sink,
        /// the dirty stack is to the sink's left and the clean stack to its
        /// right. Placing the sinks with LineUp and working the stacks out
        /// somewhere else would mean writing the same number in two places -
        /// which has drifted apart silently five times in this project.
        ///
        /// The user's description: "let empty and dirty plates pile up at the
        /// dishwasher's place, let the dishwasher wash them by hand in the
        /// sink and stack the clean ones on the other side".
        /// </summary>
        private void BuildDishStation(RoomPlan.Room r)
        {
            _dirtyStack.Clear();
            _cleanStack.Clear();

            const float Inset = 0.6f;
            float z = r.Z0 + r.D - Inset;

            // ONE SINK, NOT TWO - and it was THE PLACEMENT AUDIT that said so.
            //
            // The first version put two sinks and two counters; the audit
            // found a 0.16 m overlap. The reason is arithmetic: the room is 3.2
            // m wide and each of the four objects is ~0.84 m, so 3.36 m is
            // needed. A counter + a sink + a counter is 2.52 m and fits
            // comfortably.
            //
            // It is right for the telling too: the user said "let them wash it
            // by hand in the sink" - a single sink unit.
            float sink = r.CenterX;
            float sinkTop = TopOf(Place(SinkPrefab, sink, z, 180f));

            // THE WASHING FIGURE stands IN FRONT of the sink, not behind it:
            // behind it is the wall. Its face is towards the sink, that is +Z
            // (yaw 0).
            _washSpot = new Vector3(sink, 0f, z - 0.75f);

            // The two stacks have to be SEPARATE: two stacks piling up in the
            // same place read as "sitting there", not as "being washed".
            //
            // The stacks stand ON TOP OF THE COUNTER and the height is taken
            // by MEASURING the counter rather than by writing it down: in the
            // first version 0.92 m was guessed and the plates hung in the air.
            // If the counter model's height changes, the stack changes with
            // it.
            float leftX = r.X0 + 0.55f;
            float rightX = r.X0 + r.W - 0.55f;
            float stackZ = z - 0.02f;
            float top = TopOf(Place(CounterPrefab, leftX, stackZ, 180f));
            TopOf(Place(CounterPrefab, rightX, stackZ, 180f));

            // BY THE FLOW: dirty on the RIGHT, clean on the LEFT.
            //
            // The dirty plates come from the hall (the halls are at x > 8.4,
            // that is, on the right), the clean plates go to the kitchen (the
            // kitchen is at x < 5.2, on the left). The first version had it the
            // other way round and every plate crossed the room once more for
            // nothing.
            BuildPlateStack(_cleanStack, leftX, stackZ, top);
            BuildPlateStack(_dirtyStack, rightX, stackZ, top);

            // THE TAP'S WATER: it runs only while somebody is washing.
            //
            // A thin slab, from above the sink down into the basin. No
            // particles - the target is a low-end Adreno (docs/19) and at this
            // camera distance a jet of water is a few pixels anyway. Its height
            // is taken by MEASURING the sink (TopOf), not by writing it down.
            _water = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _water.name = "TapWater";
            _water.transform.SetParent(transform, false);
            _water.transform.localPosition =
                new Vector3(sink, sinkTop - 0.07f, z - 0.06f);
            _water.transform.localScale = new Vector3(0.035f, 0.15f, 0.035f);
            Collider waterCol = _water.GetComponent<Collider>();
            if (waterCol != null)
            {
                if (Application.isPlaying) Destroy(waterCol); else DestroyImmediate(waterCol);
            }
            Renderer waterRen = _water.GetComponent<Renderer>();
            if (_waterMat != null) waterRen.sharedMaterial = _waterMat;
            waterRen.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            waterRen.receiveShadows = false;
            _water.SetActive(false);

            // FOAM: a few small white clumps inside the basin.
            //
            // Running water on its own reads as "rinsing"; the foam says
            // "washing". No particles - four small boxes, all created at build
            // time with only their visibility changing.
            // Clustered and of DIFFERENT SIZES: four slabs of equal size at
            // equal spacing read as tiling rather than as foam. Five
            // overlapping pieces of different sizes give a mass.
            float[] fx = { -0.07f, 0.00f, 0.06f, -0.03f, 0.03f };
            float[] fz = { -0.02f, 0.03f, -0.01f, 0.05f, -0.04f };
            float[] fy = { 0.000f, 0.014f, 0.004f, 0.020f, 0.008f };
            float[] fw = { 0.085f, 0.070f, 0.078f, 0.055f, 0.062f };

            _foam.Clear();
            for (int i = 0; i < fx.Length; i++)
            {
                GameObject k = GameObject.CreatePrimitive(PrimitiveType.Cube);
                k.name = "Foam";
                k.transform.SetParent(transform, false);
                k.transform.localPosition = new Vector3(
                    sink + fx[i], sinkTop - 0.062f + fy[i], z - 0.05f + fz[i]);
                k.transform.localScale = new Vector3(fw[i], 0.030f, fw[i] * 0.85f);
                k.transform.localRotation = Quaternion.Euler(0f, i * 17f, 0f);

                Collider kc = k.GetComponent<Collider>();
                if (kc != null)
                {
                    if (Application.isPlaying) Destroy(kc); else DestroyImmediate(kc);
                }
                Renderer kr = k.GetComponent<Renderer>();
                kr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                kr.receiveShadows = false;
                if (_block == null) _block = new MaterialPropertyBlock();
                kr.GetPropertyBlock(_block);
                _block.SetColor(BaseColorId, new Color(0.97f, 0.98f, 0.99f));
                kr.SetPropertyBlock(_block);
                k.SetActive(false);
                _foam.Add(k);
            }

            // WHERE THE COOK PICKS UP A PLATE: in front of the clean stack.
            _plateSpot = new Vector3(leftX, 0f, stackZ - 0.75f);
        }

        /// A stack of plates: thin slabs one on top of another.
        ///
        /// They are all created AT BUILD TIME and afterwards only their
        /// visibility changes. Creating and destroying objects every frame
        /// means dozens of allocations a second at the peak - and this scene
        /// targets a low-end Adreno.
        /// </summary>
        private void BuildPlateStack(List<GameObject> into, float x, float z, float top)
        {
            if (PlatePrefab == null) return;
            for (int i = 0; i < PlateStackMax; i++)
            {
                GameObject p = Instantiate(PlatePrefab, transform);
                p.name = "PlateStack";
                p.transform.localPosition = new Vector3(x, top + i * PlateStep, z);
                p.transform.localRotation = Quaternion.identity;
                foreach (Collider c in p.GetComponentsInChildren<Collider>())
                {
                    if (Application.isPlaying) Destroy(c); else DestroyImmediate(c);
                }
                p.SetActive(false);
                into.Add(p);
            }
        }

        /// THE SPONGE: in the washing figure's other hand.
        ///
        /// A small yellow-green box. A plate in one hand, the sponge in the
        /// other - this pair is the screen's version of the sentence "they are
        /// scrubbing"; a figure holding a plate on its own looks as if it is
        /// carrying it.
        /// </summary>
        private void ShowSponge(GameObject staff, bool on)
        {
            if (staff == null) return;

            Transform t = staff.transform.Find("Sponge");
            if (t == null)
            {
                if (!on) return;
                GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cube);
                g.name = "Sponge";
                g.transform.SetParent(staff.transform, false);
                g.transform.localScale = new Vector3(0.10f, 0.05f, 0.07f);
                Collider c = g.GetComponent<Collider>();
                if (c != null)
                {
                    if (Application.isPlaying) Destroy(c); else DestroyImmediate(c);
                }
                Renderer ren = g.GetComponent<Renderer>();
                ren.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                if (_block == null) _block = new MaterialPropertyBlock();
                ren.GetPropertyBlock(_block);
                _block.SetColor(BaseColorId, new Color(0.88f, 0.82f, 0.35f));
                ren.SetPropertyBlock(_block);
                t = g.transform;
            }

            // A little beside the plate and below it: the two hands are
            // holding different things.
            t.localPosition = new Vector3(0.17f, 0.58f, 0.26f);
            if (t.gameObject.activeSelf != on) t.gameObject.SetActive(on);
        }

        /// <summary>The most plates shown in a stack. Above that it is told with a number.</summary>
        /// <summary>
        /// The task number for "at the sink".
        ///
        /// The table numbers start at 0 and -1 means "idle"; the washing needs
        /// a number of its own so that it is not confused with either.
        /// </summary>
        private const int WashTask = -7;

        /// How long it stays VISIBLE at the sink (real seconds).
        ///
        /// 2.5: the tap opens and closes, the sponge turns a few times, the
        /// foam is visible. Shorter is a blink, longer leaves the hall empty.
        /// </summary>
        private const float WashVisitSeconds = 2.5f;

        /// The safety timeout for the walk to the sink (real seconds).
        ///
        /// A change of task is not listened to until the walk finishes; this
        /// number only frees the figure in the case where "the path did not
        /// complete somehow". Leaving it uncapped would lock a figure that got
        /// stuck once at the sink for the whole day.
        /// </summary>
        private const float WashWalkTimeout = 12f;

        private readonly List<float> _washHold = new List<float>();

        private const int PlateStackMax = 10;

        /// <summary>The height between two plates (m).</summary>
        private const float PlateStep = 0.022f;

        /// The height of an object's TOP surface (local y).
        ///
        /// A measurement instead of a guess: if the model changes, what is put
        /// on top of it moves with it. If there is no object it returns a
        /// reasonable counter height - returning zero would spread the plates
        /// on the floor.
        /// </summary>
        private static float TopOf(GameObject go)
        {
            if (go == null) return 0.90f;
            Renderer[] rs = go.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) return 0.90f;

            Bounds b = rs[0].bounds;
            for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
            return b.max.y - go.transform.parent.position.y;
        }

        private readonly List<GameObject> _dirtyStack = new List<GameObject>();
        private readonly List<GameObject> _cleanStack = new List<GameObject>();
        private Vector3 _washSpot;

        /// PREVIEW: puts a staff member at the sink and has them wash.
        ///
        /// For taking a screenshot only. In editor mode nobody is washing (the
        /// staff are built idle) and the answer to "is the tap running, is
        /// there a sponge" can only be seen while somebody is washing.
        /// </summary>
        public void PreviewWash()
        {
            if (_staff.Count == 0) return;
            int i = _staff.Count - 1;              // the last person: the hall side
            Walker w = WalkerOf(_staff[i]);
            Figure f = _staffFigure[i];
            if (w == null || f == null) return;

            w.Warp(_washSpot, 0f);
            f.Sample(Figure.Pose.Wash, 0.5f);
            ShowCarry(_staff[i], 1);
            ShowSponge(_staff[i], true);
            if (_water != null) _water.SetActive(true);

            // Let the stacks look full too: an empty sink does not say
            // "washing".
            Show(_dirtyStack, 5);
            Show(_cleanStack, 3);
            for (int k = 0; k < _foam.Count; k++)
                if (_foam[k] != null) _foam[k].SetActive(true);
        }

        /// The largest relative deviation between the clip speed and the
        /// GROUND SPEED, over WALKING figures. So the tour can ask.
        ///
        /// This is the MEASURE of foot sliding: however many metres a second
        /// the figure covers, the legs have to turn to that distance.
        /// Anim.speed x WalkClipSpeed has to equal the ground speed; if it
        /// does not, the feet slide.
        ///
        /// A bug that is hard to notice by eye - and that is exactly why it
        /// stood for months.
        /// </summary>
        public float WalkSlipWorst
        {
            get
            {
                float worst = 0f;
                for (int i = 0; i < _staff.Count; i++)
                {
                    Figure f = _staffFigure[i];
                    Walker w = WalkerOf(_staff[i]);
                    if (f == null || w == null || !w.Moving) continue;
                    if (f.Current != Figure.Pose.Walk) continue;
                    if (f.Anim == null || !f.Anim.enabled) continue;

                    float spot = w.LastGroundSpeed;
                    float clip = f.Anim.speed * Figure.WalkClipSpeed;
                    if (spot < 0.01f) continue;
                    float deviation = Mathf.Abs(clip - spot) / spot;
                    if (deviation > worst) worst = deviation;
                }
                return worst;
            }
        }

        /// <summary>In front of the sink: where the washing figure stands.</summary>
        public Vector3 WashSpot { get { return _washSpot; } }

        /// <summary>In front of the clean plate stack: where the cook picks up a plate.</summary>
        public Vector3 PlateSpot { get { return _plateSpot; } }

        private Vector3 _plateSpot;
        private GameObject _water;
        private readonly List<GameObject> _foam = new List<GameObject>();

        /// The number of figures standing at the sink right now. So the tour
        /// can ask.
        ///
        /// The simulation saying "it is washing" and the figure BEING SEEN at
        /// the sink are two different things; if the second is not measured
        /// the washing can quietly be played at home.
        /// </summary>
        /// The number of frames in which the player SAW somebody at the sink
        /// today.
        ///
        /// Sampled within a window, the instantaneous WashingCount comes out
        /// almost always zero - the washing is short and the window narrow. A
        /// cumulative counter is the right measure of "was washing seen
        /// today".
        /// </summary>
        public int WashSeenFrames { get { return _washSeen; } }

        private int _washSeen;

        public int WashingCount
        {
            get
            {
                int n = 0;
                for (int i = 0; i < _staff.Count; i++)
                {
                    Figure f = _staffFigure[i];
                    if (f == null || f.Current != Figure.Pose.Wash) continue;
                    Vector3 d = _staff[i].transform.localPosition - _washSpot;
                    d.y = 0f;
                    if (d.sqrMagnitude < 1.2f * 1.2f) n++;
                }
                return n;
            }
        }

        /// Shows the stacks according to the number in the simulation.
        ///
        /// The visible stack is CAPPED (PlateStackMax): a tower of fifty-six
        /// plates would go through the room's ceiling and is not read anyway.
        /// What the player sees is "a lot or a little" - and ten slabs carry
        /// that comfortably.
        /// </summary>
        private void UpdatePlateStacks(Simulation sim)
        {
            Show(_dirtyStack, sim.PlatesDirty);
            Show(_cleanStack, sim.PlatesClean);
        }

        /// The tap runs ONLY while somebody is washing.
        ///
        /// A tap that runs all the time does not say "washing", it says
        /// "left on" - and it leaves the player no reason to look at the
        /// sink.
        /// </summary>
        private void UpdateWater()
        {
            bool running = WashingCount > 0;
            if (_water != null && _water.activeSelf != running) _water.SetActive(running);
            for (int i = 0; i < _foam.Count; i++)
                if (_foam[i] != null && _foam[i].activeSelf != running)
                    _foam[i].SetActive(running);
        }

        private static void Show(List<GameObject> stack, int count)
        {
            if (count > PlateStackMax) count = PlateStackMax;
            for (int i = 0; i < stack.Count; i++)
            {
                if (stack[i] == null) continue;
                bool on = i < count;
                if (stack[i].activeSelf != on) stack[i].SetActive(on);
            }
        }

        /// <param name="rightInset">
        /// Extra room to leave free on the right. So that the row does not run
        /// into another object on the same wall (the fridge in the kitchen).
        /// </param>
        private void LineUp(RoomPlan.Room r, GameObject prefab, int count,
                            float inset, float yaw, bool back = true,
                            float rightInset = 0f, bool appliance = false)
        {
            if (prefab == null || count <= 0) return;
            float z = back ? r.Z0 + r.D - inset : r.Z0 + inset;
            float width = r.W - inset * 2f - rightInset;
            for (int i = 0; i < count; i++)
            {
                float t = (i + 0.5f) / count;
                GameObject go = Place(prefab, r.X0 + inset + width * t, z, yaw);
                if (appliance && go != null)
                {
                    // A POT ON TOP OF THE STOVE.
                    //
                    // The user's complaint: "no pots or anything are put on the
                    // stoves in the kitchen, it does not look like a real kitchen".
                    // The stoves were empty, and an empty stove made the kitchen
                    // look like an "equipment showroom".
                    //
                    // The pot's height is measured FROM THE STOVE: if the asset pack
                    // changes, the pot must not end up in the air or inside it.
                    _potSpots.Add(go.transform);
                    // An UNLIT material: the lamp and the flame carry MEANING, they
                    // are not the result of lighting. With a lit material the lamp
                    // inside the oven was drawn as a dark panel.
                    Appliance a = Appliance.Attach(go, _badgeMat, _glassMat);
                    if (a != null) _stoves.Add(a);
                }
            }
        }

        /// <summary>Where the stoves are; the pots are placed at the end of the build.</summary>
        private readonly List<Transform> _potSpots = new List<Transform>();

        /// ON TOP OF THE STOVE: a pot, a pan and a lid.
        ///
        /// The same path as the decoration (Modeler, one mesh per colour) but
        /// separate because of the BUILD ORDER: the stoves are placed in
        /// BuildRoomProps and the decoration comes after them. The pots sit on
        /// the MEASURED top surface of the stoves.
        /// </summary>
        private void BuildPots(Palette p)
        {
            if (_potSpots.Count == 0) return;

            Modeler m = new Modeler();
            Color steel = new Color(0.647f, 0.678f, 0.722f);
            Color dark = new Color(0.239f, 0.255f, 0.278f);

            for (int i = 0; i < _potSpots.Count; i++)
            {
                Transform t = _potSpots[i];
                if (t == null) continue;

                // The stove's top surface: from the renderers' world box.
                float top = 0.9f;
                bool first = true;
                Bounds b = new Bounds();
                foreach (Renderer r in t.GetComponentsInChildren<Renderer>())
                {
                    if (first) { b = r.bounds; first = false; }
                    else b.Encapsulate(r.bounds);
                }
                if (!first) top = b.max.y - transform.position.y;

                Vector3 spot = t.localPosition;

                if (i % 2 == 0)
                {
                    // The pot: a body, a lid and two handles.
                    m.Prism(10, 0.155f, 0.165f, 0.20f,
                            new Vector3(spot.x - 0.12f, top, spot.z),
                            Quaternion.identity, steel);
                    m.Prism(10, 0.175f, 0.155f, 0.035f,
                            new Vector3(spot.x - 0.12f, top + 0.20f, spot.z),
                            Quaternion.identity, dark);
                    m.Box(new Vector3(spot.x - 0.12f, top + 0.245f, spot.z),
                          new Vector3(0.05f, 0.04f, 0.05f), dark);
                    for (int k = 0; k < 2; k++)
                        m.Box(new Vector3(spot.x - 0.12f + (k == 0 ? -0.185f : 0.185f),
                                          top + 0.13f, spot.z),
                              new Vector3(0.06f, 0.04f, 0.10f), dark);
                }
                else
                {
                    // The pan: a shallow body and a long handle.
                    m.Prism(10, 0.19f, 0.21f, 0.075f,
                            new Vector3(spot.x + 0.10f, top, spot.z),
                            Quaternion.identity, dark);
                    m.Box(new Vector3(spot.x + 0.10f, top + 0.055f, spot.z + 0.28f),
                          new Vector3(0.05f, 0.035f, 0.34f), dark);
                }
            }

            if (_block == null) _block = new MaterialPropertyBlock();
            m.Build(transform, "StoveTop", _floorMat, _block);
            _potCount = _potSpots.Count;
        }

        /// The number of stoves that have a pan on top.
        ///
        /// IT EXISTS FOR THE MEASUREMENT. The user said "no pots or anything
        /// are put on the stoves in the kitchen, it does not look like a real
        /// kitchen" and the pans were added - but NO CHECK was asking about
        /// them. If the stoves' layout changed, if the call to BuildPots were
        /// dropped or if _potSpots stayed empty, the kitchen would quietly
        /// empty again and only the user would see it.
        /// </summary>
        public int PotCount { get { return _potCount; } }

        private int _potCount;

        /// THE THRESHOLD MAT: the horizontal mark that says where the door
        /// is.
        ///
        /// A door FRAME was put there first and it did not read: the pack's
        /// doorwayOpen model is only two side posts (it has no head) and from
        /// a 34 degree view it looks like two planks lying on the floor.
        /// wallDoorway, on the other hand, is a full wall; putting a wall on
        /// the front edge hides the entrance room from the camera.
        ///
        /// In a floor plan with no walls the right tool for marking a door is
        /// HORIZONTAL, not VERTICAL. With a camera looking from above, a mat
        /// reads and a post does not.
        ///
        /// The width is 1.6 m: as wide as a doorway two people pass through
        /// side by side, and centred on Paths.DoorX - where the guest walks in
        /// and where the mark stands come from the same number.
        /// </summary>
        private void Threshold(RoomPlan.Room r)
        {
            GameObject mat = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mat.name = "Threshold";
            mat.transform.SetParent(transform, false);
            mat.transform.localPosition = new Vector3(Paths.DoorX, -0.045f, r.Z0 + 0.45f);
            mat.transform.localScale = new Vector3(1.6f, 0.1f, 0.9f);
            // IN EDITOR MODE Destroy IS DEFERRED and the collider box STAYS in
            // the scene. The screenshot tool runs Rebuild in editor mode; the
            // box that was left cut off the room's touch ray in front of it.
            Collider matCol = mat.GetComponent<Collider>();
            if (matCol != null)
            {
                if (Application.isPlaying) Destroy(matCol); else DestroyImmediate(matCol);
            }

            Renderer ren = mat.GetComponent<Renderer>();
            ren.sharedMaterial = _floorMat;
            ren.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            if (_block == null) _block = new MaterialPropertyBlock();
            ren.GetPropertyBlock(_block);
            _block.SetColor(BaseColorId, MatColor);
            ren.SetPropertyBlock(_block);
        }

        private GameObject Place(GameObject prefab, float x, float z, float yaw)
        {
            if (prefab == null) return null;
            GameObject go = Instantiate(prefab, transform);
            go.transform.localPosition = new Vector3(x, 0f, z);
            go.transform.localRotation = Quaternion.Euler(0f, yaw + PropYaw, 0f);
            Retint(go);
            return go;
        }

        private void BuildTables(int tables)
        {
            List<RoomPlan.TableSpot> spots = RoomPlan.TableSpots(tables);
            foreach (RoomPlan.TableSpot s in spots)
            {
                GameObject holder = new GameObject("Table_" + _tables.Count);
                holder.transform.SetParent(transform, false);
                holder.transform.localPosition = new Vector3(s.X, 0f, s.Z);

                if (TablePrefab != null)
                {
                    GameObject t = Instantiate(TablePrefab, holder.transform);
                    t.transform.localPosition = Vector3.zero;

                    // THE TABLE IS MADE SQUARE.
                    //
                    // The pack's model is a 0.82 x 0.45 m rectangle; the depth is
                    // made equal to the width so that the four seats are EQUALLY far
                    // from the four edges. The ratio is measured AT BUILD TIME, not
                    // written by hand - if the prefab changes, the calculation
                    // changes with it.
                    t.transform.localScale = new Vector3(1f, 1f, TableSquareZ(t));
                    Retint(t);

                    // The top's height: BEFORE the chairs are added.
                    if (_tableTop <= 0f)
                    {
                        float width = -1f;
                        foreach (Renderer rr in t.GetComponentsInChildren<Renderer>())
                        {
                            float top = rr.bounds.max.y - holder.transform.position.y;
                            if (top > width) width = top;
                        }
                        if (width > 0.2f) _tableTop = width + 0.01f;
                    }
                }

                // Four chairs, on the table's four sides and facing the table.
                //
                // PropYaw IS ESSENTIAL: the chair model faces the opposite way to
                // the character. Seat(k) places the seat with Rot(k*90) * (0,0,-r),
                // so k=0 is on the table's -Z side; the guest faced +Z, towards the
                // table - correct. The chair at the same angle, though, turned its
                // BACK to the table and the guest looked buried in the backrest.
                if (ChairPrefab != null)
                {
                    for (int k = 0; k < Seats; k++)
                    {
                        GameObject chair = Instantiate(ChairPrefab, holder.transform);
                        chair.transform.localPosition = Seat(k);
                        chair.transform.localRotation =
                            Quaternion.Euler(0f, k * 90f + PropYaw, 0f);
                        Retint(chair);
                        _chairs[_tables.Count * Seats + k] = chair.transform;
                    }
                }

                _badges.Add(TableBadge.Create(holder.transform, _badgeMat));
                // The touch target: so that a table can be SELECTED when the room
                // is zoomed into. The interventions no longer pick their target
                // themselves.
                TableTouch.Attach(holder.transform, _tables.Count);
                _tables.Add(holder.transform);
            }
        }

        // =====================================================================
        /// The seated guests. There is a figure for every PERSON at the
        /// table: a couple of two and a family of four have to be
        /// distinguishable when you look at the hall. At first a single figure
        /// was put at each occupied table and a full hall looked the same as a
        /// crowded one.
        ///
        /// The figures come from a pool and go back by being hidden.
        /// </summary>
        /// The plate and the food in front of one seat.
        ///
        /// POOLED: the plate is built once and afterwards only switched on
        /// and off. Creating and destroying one at every service meant
        /// fourteen tables x four seats = dozens of allocations a frame.
        ///
        /// The food comes from THE PACK's ingredient models (tomato, meatball,
        /// cheese): they have their own materials, so they go into the
        /// batching. A procedural disc would look cheaper, but it would need a
        /// property block on each one, and that breaks the batching.
        /// </summary>
        private void TablePlate(int table, int seat, bool want)
        {
            int key = table * Seats + seat;
            GameObject go;
            if (!_tableFood.TryGetValue(key, out go))
            {
                if (!want || PlatePrefab == null) return;
                if (table >= _tables.Count || _tables[table] == null) return;

                go = Instantiate(PlatePrefab, _tables[table]);
                go.name = "TablePlate" + seat;

                // The plate is IN FRONT OF the seat and ON TOP OF the table: 0.30
                // m in the seat's direction, with the top's height measured.
                Vector3 dir = Seat(seat, 0.30f);
                go.transform.localPosition = new Vector3(dir.x, TableTop(table), dir.z);
                go.transform.localRotation = Quaternion.identity;
                Retint(go);

                // The food: on the plate, an ingredient that changes with the
                // table and the seat - so that not every plate looks the same.
                if (IngredientPrefabs != null && IngredientPrefabs.Length > 0)
                {
                    GameObject y = IngredientPrefabs[(table * 3 + seat)
                                                     % IngredientPrefabs.Length];
                    if (y != null)
                    {
                        GameObject food = Instantiate(y, go.transform);
                        food.name = "Food";
                        food.transform.localPosition = new Vector3(0f, 0.03f, 0f);
                        food.transform.localScale = Vector3.one * 0.55f;
                    }
                }
                _tableFood[key] = go;

            }

            if (go != null && go.activeSelf != want) go.SetActive(want);
        }

        /// The height of the table's top (local). Not a guess but a
        /// MEASUREMENT: if the pack changes, the plate must not end up in the
        /// air or inside the top.
        /// </summary>
        private float TableTop(int table)
        {
            // THE MEASUREMENT COMES FROM THE TABLE ITSELF, NOT FROM THE
            // HOLDER.
            //
            // The first version scanned all the holder's renderers, and the
            // holder also has the chairs (0.9 m) and the BADGE (2.45 m): the
            // plates came out level with the badge rather than the table, that
            // is, in mid-air. In the screenshot it looked like "there is no
            // food on the tables, there are white blotches in the air".
            //
            // The value is measured while the table is being built, before the
            // chairs are added (BuildTables) - at that moment the holder
            // contains nothing but the table.
            return _tableTop > 0f ? _tableTop : 0.62f;
        }

        private float _tableTop;

        private readonly Dictionary<int, GameObject> _tableFood =
            new Dictionary<int, GameObject>();

        /// <summary>The number of tables eating right now. For the diagnostics and the tour.</summary>
        public int EatingTables { get { return _eatingLast; } }

        /// The number of plates VISIBLE on the tables right now.
        ///
        /// "The core is eating" and "the player can see the food" are two
        /// different claims (this project learned that on the washing up: the
        /// sim was washing the plates and on screen nobody ever reached the
        /// sink). The tour asks both.
        /// </summary>
        public int TablePlatesVisible
        {
            get
            {
                int n = 0;
                foreach (KeyValuePair<int, GameObject> kv in _tableFood)
                    if (kv.Value != null && kv.Value.activeSelf) n++;
                return n;
            }
        }

        private int _eatingNow, _eatingLast;

        private void UpdateCustomers(Simulation sim)
        {
            _eatingNow = 0;
            for (int t = 0; t < _tables.Count; t++)
            {
                // The table's state: the stage and the patience left.
                if (t < _badges.Count && _badges[t] != null)
                    _badges[t].Show(sim.TableStage(t), sim.TablePatienceBp(t),
                                    App != null && App.SelectedTable == t);

                int guests = Mathf.Min(sim.TableGuests(t), VisibleGuests);
                Vector3 table = _tables[t].localPosition;

                // IS THERE FOOD ON THE TABLE?
                //
                // In all four reference frames there are plates and food on the
                // tables; our tables were EMPTY even while being served. The only
                // place the player could get "that table is eating" from was the
                // badge.
                //
                // The condition comes FROM THE CORE: only a table whose food HAS
                // ARRIVED has a plate. Putting a plate on every table would have
                // been easier and would have been a lie - a waiting table and an
                // eating table would look the same on screen.
                CustomerStage stage = sim.TableStage(t);
                bool hasFood = stage == CustomerStage.Eating
                                || stage == CustomerStage.WaitingToPay;
                if (hasFood) _eatingNow++;

                for (int k = 0; k < Seats; k++)
                {
                    int key = t * Seats + k;
                    bool want = SeatOrder(k) < guests;
                    bool has = _seated.TryGetValue(key, out GameObject figure);

                    // An occupied chair is back, an empty chair is stuck to the
                    // table.
                    //
                    // Written ONLY ON CHANGE: 14 tables x 4 chairs = 56 transform
                    // writes a frame, and Unity's setter does no equality check -
                    // every write makes the subtree's matrix be recalculated. The
                    // value itself does not change unless a table fills or empties.
                    bool wasOccupied;
                    if (!_chairBack.TryGetValue(key, out wasOccupied)
                        || wasOccupied != want)
                    {
                        _chairBack[key] = want;
                        if (_chairs.TryGetValue(key, out Transform chairT)
                            && chairT != null)
                            chairT.localPosition =
                                Seat(k, want ? SeatRadiusUsed : SeatRadius);
                    }

                    TablePlate(t, k, want && hasFood);

                    if (want && !has)
                    {
                        figure = Take();
                        if (figure == null) continue;

                        // IT COMES IN THROUGH THE DOOR.
                        //
                        // It used to TELEPORT onto the seat: when a table filled, the
                        // figure appeared on the chair in a single frame. The
                        // simulation already holds a delay between "the guest
                        // arrived" and "sat down at the table"; that delay told
                        // nothing at all on screen.
                        //
                        // The figure IS NO LONGER A CHILD OF THE TABLE: walking
                        // happens in world space, and a figure parented to the table
                        // walked in the table's local space. The seat position is now
                        // worked out by adding to the table's position.
                        // THE CHAIR STAYS, THE FIGURE COMES FORWARD: the seat radius
                        // is where the chair is; the figure slides SitForward towards
                        // the table from there.
                        Vector3 seat = table + Seat(k, SeatRadiusUsed - SitForward)
                                       + new Vector3(0f, SitLift, 0f);
                        float yaw = k * 90f;

                        figure.transform.SetParent(transform, false);
                        figure.SetActive(true);

                        Walker w = WalkerOf(figure);
                        Figure f = FigureOf(figure);
                        // IT APPEARS ON THE STREET, NOT IN FRONT OF THE DOOR.
                        //
                        // Appearing in front of the door read as "it materialised"
                        // rather than "it arrived". It now appears on the pavement, a
                        // few metres from the door, and comes in on foot - the moment
                        // of walking in through the door can be watched.
                        w.Warp(Paths.Street(t), 0f);

                        Paths.FromStreet(_path, seat);
                        w.GoTo(_path, yaw, () => Pose(f, Figure.Pose.Sit));

                        // In the preview (the editor screenshot) there is NO walking:
                        // because a single frame is sampled, everyone would stand at
                        // the door and the hall would come out empty.
                        if (PreviewPoses)
                        {
                            w.Warp(seat, yaw);
                            Pose(f, Figure.Pose.Sit);
                        }

                        _seated[key] = figure;
                    }
                    else if (!want && has)
                    {
                        _seated.Remove(key);
                        SendHome(figure);
                    }
                }
            }

            UpdateQueue(sim);

            // Those leaving: they go back to the pool when they reach the door.
            for (int i = _leaving.Count - 1; i >= 0; i--)
            {
                GameObject go = _leaving[i];
                if (go == null) { _leaving.RemoveAt(i); continue; }
                Walker w = WalkerOf(go);
                if (w.Moving) continue;
                _leaving.RemoveAt(i);
                Give(go);
            }
            _eatingLast = _eatingNow;
        }

        /// THOSE WAITING FOR A TABLE. They stand beside the door.
        ///
        /// There is a stage in the simulation called WaitingForTable and the
        /// view never drew it: a guest with no table WAS NOT ON SCREEN until
        /// their patience ran out and they left angry. The game's most
        /// expensive event (a full hall) was only a column in the evening
        /// report.
        ///
        /// What is shown is not how many people are in a party but HOW MANY
        /// PARTIES are waiting - drawing everyone in the queue would block the
        /// front of the door, and what the player needs to read is not a
        /// number but "is there a queue".
        /// </summary>
        private void UpdateQueue(Simulation sim)
        {
            int n = 0;
            for (int p = 0; p < Simulation.MaxParties && n < MaxQueue; p++)
            {
                if (sim.StageOf(p) != CustomerStage.WaitingForTable) continue;
                if (sim.TableOfParty(p) >= 0) continue;

                GameObject go;
                if (!_queued.TryGetValue(p, out go) || go == null)
                {
                    go = Take();
                    if (go == null) break;
                    go.transform.SetParent(transform, false);
                    go.SetActive(true);

                    Walker w0 = WalkerOf(go);
                    Figure f0 = FigureOf(go);
                    Vector3 spot = Paths.QueueSpot(n);

                    if (PreviewPoses)
                    {
                        w0.Warp(spot, 0f);
                        Pose(f0, Figure.Pose.Idle);
                    }
                    else
                    {
                        w0.Warp(Paths.Street(p), 0f);
                        _path.Clear();
                        _path.Add(Paths.Inside);
                        _path.Add(spot);
                        w0.GoTo(_path, 0f, () => Pose(f0, Figure.Pose.Idle));
                    }
                    _queued[p] = go;
                }
                n++;
            }

            // Those leaving the queue: either they sat down at a table or they left.
            _queueGone.Clear();
            foreach (KeyValuePair<int, GameObject> kv in _queued)
            {
                if (sim.StageOf(kv.Key) == CustomerStage.WaitingForTable
                    && sim.TableOfParty(kv.Key) < 0) continue;
                _queueGone.Add(kv.Key);
            }
            for (int i = 0; i < _queueGone.Count; i++)
            {
                GameObject go = _queued[_queueGone[i]];
                _queued.Remove(_queueGone[i]);
                // The party that sat down has its own figures drawn
                // separately; the representative in the queue walks out through
                // the door.
                SendHome(go);
            }
        }

        /// How many parties are shown at the door at most. More than that
        /// blocks the door and what is read stops being "there is a queue"
        /// and becomes "a crowd".
        /// </summary>
        private const int MaxQueue = 4;

        private readonly Dictionary<int, GameObject> _queued =
            new Dictionary<int, GameObject>();
        private readonly List<int> _queueGone = new List<int>();

        /// A guest getting up: it stands up, walks TO THE DOOR and then goes
        /// back to the pool.
        ///
        /// It used to be deleted from the table instantly. A guest leaving -
        /// especially leaving angry - is the game's most expensive event and
        /// it was not visible on screen at all.
        /// </summary>
        private void SendHome(GameObject figure)
        {
            if (figure == null) return;

            if (PreviewPoses) { Give(figure); return; }

            Walker w = WalkerOf(figure);
            Vector3 here = figure.transform.localPosition;
            here.y = 0f;
            w.Warp(here, figure.transform.localEulerAngles.y);

            Paths.ToStreet(_path, here, _leaving.Count);
            GameObject captured = figure;
            w.GoTo(_path, float.NaN, () => { /* at the door: it stays in the loop */ });
            _leaving.Add(captured);
        }

        private readonly List<GameObject> _leaving = new List<GameObject>();

        /// <summary>A SINGLE buffer for the waypoints: it produces no garbage per frame.</summary>
        private readonly List<Vector3> _path = new List<Vector3>(8);

        /// The figure's walker. It is added to an object coming from the pool
        /// on first use - there was no Walker when the prefabs were produced,
        /// and running the prefab generator again would show every asset as
        /// touched.
        /// </summary>
        /// HOW MANY figures are walking right now. So the tour can ask.
        ///
        /// The question "is there movement" has no answer in a screenshot: a
        /// single frame shows a standing figure and a walking figure the
        /// same. The only countable thing is the movement itself.
        /// The number of figures on their way.
        ///
        /// NO ALLOCATION. It used to use GetComponentsInChildren and allocate
        /// a new ~60-element array on every read; the automatic tour reads
        /// this ON EVERY FRAME of a 1500-frame loop, so the measuring tool
        /// itself was spoiling the frame time it was measuring. All the
        /// figures to be counted are already in hand.
        /// </summary>
        /// <summary>
        /// THE WORST DISTANCE A WALKING FIGURE GETS INSIDE A TABLE SET, in
        /// metres. Zero means nobody walked through the furniture.
        ///
        /// This exists because the fix needed a measurement. `Paths.Lane`
        /// returned immediately when both ends of a walk were in the same
        /// room, so inside a dining room figures went in a straight line -
        /// through the tables and chairs. It now routes round the table block
        /// by the room's side lanes, and without a number that claim is just
        /// a claim.
        ///
        /// A TABLE SET IS A CIRCLE, not the table top: a chair sits
        /// `SeatRadius` from the centre and is about 0.2 m deep, so the set's
        /// radius is about 0.78 m. Half a figure's width is added, because a
        /// figure whose CENTRE clears the chairs but whose shoulder does not
        /// is still walking through them.
        ///
        /// Only MOVING figures count. A guest sitting at a table is inside
        /// the set by definition, and so is the waiter standing beside it to
        /// serve - both are the point.
        /// </summary>
        public float WorstFurnitureOverlap
        {
            get
            {
                float worst = 0f;
                for (int i = 0; i < _staff.Count; i++) worst = Mathf.Max(worst, Intruding(_staff[i]));
                foreach (KeyValuePair<int, GameObject> kv in _seated)
                    worst = Mathf.Max(worst, Intruding(kv.Value));
                for (int i = 0; i < _leaving.Count; i++) worst = Mathf.Max(worst, Intruding(_leaving[i]));
                return worst;
            }
        }

        /// <summary>How far this figure is inside a table set, if it is walking.</summary>
        private float Intruding(GameObject go)
        {
            if (go == null || Moving(go) == 0) return 0f;
            Vector3 p = go.transform.localPosition;

            // The set's radius plus half a body. SeatRadius is the chair's
            // CENTRE, so the chair's outer edge is about 0.2 m beyond it.
            const float setRadius = SeatRadius + 0.20f;
            const float halfBody = 0.22f;

            float worst = 0f;
            for (int i = 0; i < _tables.Count; i++)
            {
                Vector3 t = _tables[i].localPosition;
                float dx = p.x - t.x, dz = p.z - t.z;
                float d = Mathf.Sqrt(dx * dx + dz * dz);
                float into = setRadius + halfBody - d;
                if (into > worst) worst = into;
            }
            return worst;
        }

        public int MovingCount
        {
            get
            {
                int n = 0;
                for (int i = 0; i < _staff.Count; i++) n += Moving(_staff[i]);
                foreach (KeyValuePair<int, GameObject> kv in _seated) n += Moving(kv.Value);
                foreach (KeyValuePair<int, GameObject> kv in _queued) n += Moving(kv.Value);
                for (int i = 0; i < _leaving.Count; i++) n += Moving(_leaving[i]);
                return n;
            }
        }

        private int Moving(GameObject go)
        {
            if (go == null || !go.activeSelf) return 0;
            Walker w = WalkerOf(go);
            return w != null && w.Moving ? 1 : 0;
        }

        private Walker WalkerOf(GameObject go)
        {
            Walker w = go.GetComponent<Walker>();
            if (w == null)
            {
                w = go.AddComponent<Walker>();
                w.Body = go.GetComponentInChildren<Figure>(true);
            }
            return w;
        }

        /// The position of seat number k relative to the table's centre. The
        /// chair and the figure use the same formula; when they were written
        /// separately the figure ended up inside the chair.
        /// </summary>
        /// The scale factor that makes the table's depth equal to its width.
        /// Measured once and kept: scanning the renderers fourteen times for
        /// fourteen tables is needless.
        /// </summary>
        private float TableSquareZ(GameObject table)
        {
            if (_tableSquareZ > 0f) return _tableSquareZ;

            Renderer[] rs = table.GetComponentsInChildren<Renderer>(true);
            if (rs.Length == 0) return 1f;
            Bounds b = rs[0].bounds;
            for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);

            _tableSquareZ = b.size.z > 0.0001f
                ? Mathf.Clamp(b.size.x / b.size.z, 0.25f, 4f) : 1f;
            return _tableSquareZ;
        }

        private float _tableSquareZ;

        /// The chair bodies. An occupied one is PULLED BACK.
        ///
        /// Why: there is NO KNEE in the skeleton - one bone per leg - so a leg
        /// can do nothing but hang down from the hip. When the figure was sat
        /// onto the cushion, the thighs cut through the cushion's front edge
        /// this time ("as if their knees had gone into the chair").
        ///
        /// The answer is to move the chair: an empty chair stands stuck to the
        /// table, an occupied chair slides back - in reality a chair is pulled
        /// back to sit down too. The legs hang in front of the cushion, into
        /// the gap.
        /// </summary>
        private readonly Dictionary<int, Transform> _chairs =
            new Dictionary<int, Transform>();

        /// <summary>Was the chair's last position an occupied one? See UpdateCustomers.</summary>
        private readonly Dictionary<int, bool> _chairBack = new Dictionary<int, bool>();

        private static Vector3 Seat(int k) { return Seat(k, SeatRadius); }

        /// The seat direction at the given radius. The chair and the figure
        /// stand in the SAME direction but at different radii: the figure is
        /// SitForward further forward, otherwise the backrest passes through
        /// its body.
        /// </summary>
        private static Vector3 Seat(int k, float r)
        {
            // Rot(k*90) * (0,0,-r): k=0 -> -Z, k=1 -> -X,
            // k=2 -> +Z, k=3 -> +X.
            return Quaternion.Euler(0f, k * 90f, 0f)
                   * new Vector3(0f, 0f, -r);
        }

        /// In which order this seat is filled: the LEFT and the RIGHT first,
        /// then the back, and last of all the front one that turns its back to
        /// the camera.
        ///
        /// Two fixes together:
        ///
        /// 1. It used to fill in order (0, 1, 2, 3) and a party of two sat
        ///    SIDE BY SIDE: the distance between two seats is 0.88 m and this
        ///    pack's figures are nearly that wide at the shoulder, so two
        ///    guests turned into a single mass. Sitting opposite each other
        ///    there is 1.24 m between them - the same table, the same chairs,
        ///    zero cost.
        ///
        /// 2. Opposite is not enough, the AXIS matters too. In the first
        ///    attempt the pair was 0-2, that is the Z axis; because the camera
        ///    looks along Z at a 34 degree tilt, the two figures OVERLAPPED on
        ///    screen. The 1.24 m between them was not even visible. The 1-3
        ///    pair is on the X axis: the same distance, but side by side on
        ///    screen.
        ///
        /// The user's sentence was "it makes the middle look very cramped",
        /// and the close-up screenshot showed the cause was not the HEIGHT but
        /// the WIDTH; this is the one lever that reduces the crowding without
        /// touching the width.
        /// </summary>
        private static int SeatOrder(int k)
        {
            switch (k)
            {
                case 1: return 0;    // left
                case 3: return 1;    // right
                case 2: return 2;    // back, facing the camera
                default: return 3;   // front, its back to the camera
            }
        }

        /// The staff figures. The cooks in the kitchen, the hall crew at the
        /// entrance. There is no movement in this slice - the aim is to show
        /// WHO IS THERE.
        /// </summary>
        private void UpdateStaff(Simulation sim)
        {
            _staffCooks = sim.Cooks;
            int want = sim.Cooks + sim.HallStaff;

            // THE STAMP COMES FROM THE MAKE-UP, NOT FROM THE TOTAL.
            //
            // Only the TOTAL number used to be looked at, and cooks and
            // waiters are independent: if the player lets a waiter go and
            // hires a cook on the same morning, the total does not change, the
            // block is skipped and the new cook's routine component is never
            // built. That figure neither cooks nor serves - it stays at the
            // entrance, in its old place, without moving at all.
            // THE NUMBER OF DISHWASHERS IS IN THE STAMP TOO.
            //
            // The stamp was written as "from the make-up" but the number of
            // dishwashers had been left out: when the player puts a waiter on
            // the washing up, neither the total nor the cook count changes - so
            // the block is skipped and the CLOTHES stay in the old role. A
            // dishwasher wearing a waiter's apron makes the mechanic itself
            // invisible.
            int stamp = sim.Cooks * 1000000 + sim.HallStaff * 1000
                        + sim.Dishwashers;

            if (stamp != _staffBuilt)
            {
                while (_staff.Count > want)
                {
                    int last = _staff.Count - 1;
                    if (_staff[last] != null)
                    {
                        if (Application.isPlaying) Destroy(_staff[last]);
                        else DestroyImmediate(_staff[last]);
                    }
                    _staff.RemoveAt(last);
                    _staffFigure.RemoveAt(last);
                    // The measurement record of the dropped index goes too: when
                    // the index is reused, no comparison must be made against the
                    // old progress value.
                    _workClip.Remove(last);
                    if (_staffTask.Count > last) _staffTask.RemoveAt(last);
                    if (_cookRoutine.Count > last) _cookRoutine.RemoveAt(last);
                    if (_washHold.Count > last) _washHold.RemoveAt(last);
                }
                while (_staff.Count < want && StaffPrefabs != null && StaffPrefabs.Length > 0)
                {
                    GameObject prefab = StaffPrefabs[_staff.Count % StaffPrefabs.Length];
                    GameObject go = Instantiate(prefab, transform);
                    _staff.Add(go);
                    // INACTIVE CHILD OBJECTS ARE SCANNED TOO (true).
                    //
                    // Walker.Body was already searching with `true`, but this line
                    // and FigureOf were not. If Figure is under an inactive node in
                    // the prefab, this returns null, null is passed to
                    // CookRoutine.Init and CookRoutine.Update quietly dropped to
                    // "Idle": the cook does no work at all, AND RAISES NO ERROR.
                    Figure fig = go.GetComponentInChildren<Figure>(true);
                    if (fig == null)
                        Debug.LogError("PROBLEMS: no Figure on the staff prefab: "
                                       + go.name);
                    _staffFigure.Add(fig);
                    _staffTask.Add(int.MinValue);
                    _cookRoutine.Add(null);
                    _washHold.Add(0f);
                }

                // THE CLOTHES: the chef's hat and the apron.
                //
                // Like the routine component, these are handed out again when
                // the crew changes - the same GameObject can be a cook one day
                // and a waiter the next.
                if (_block == null) _block = new MaterialPropertyBlock();

                // The measurement starts from zero at every crew build: the
                // "staff dressed N/M" line has to describe THIS crew.
                Wardrobe.ResetCounters();

                for (int i = 0; i < _staff.Count; i++)
                {
                    // THE ROLE: the cooks first, then the DISHWASHERS, then the
                    // waiters. Dishwasher is not a separate role - it is the part
                    // of the hall crew put on the washing up (SetDishwashers) and
                    // it is counted from the END of the list, because the core
                    // hands the duty out from there too.
                    Wardrobe.Role role;
                    if (i < sim.Cooks) role = Wardrobe.Role.Cook;
                    else if (i >= _staff.Count - sim.Dishwashers)
                        role = Wardrobe.Role.Dishwasher;
                    // AND IN SELF SERVICE THE HALL IS A CLEANER, NOT A
                    // WAITER. docs/51 replaced the role outright; the interface
                    // renames it and the simulation runs a different hall loop
                    // for it. This line fell through to Waiter, so a
                    // self-service burger bar had a figure in a dinner jacket
                    // and a bow tie standing in it - the picture contradicting
                    // the mechanic that is supposed to separate the two
                    // cuisines.
                    else role = sim.SelfService
                        ? Wardrobe.Role.Cleaner : Wardrobe.Role.Waiter;
                    Wardrobe.Dress(_staff[i], role, _floorMat, _block);
                }

                // THE ROUTINE COMPONENT only on the cooks. When the crew
                // changes, who is a cook changes, so it is handed out again
                // every time.
                for (int i = 0; i < _staff.Count; i++)
                {
                    CookRoutine cr = _staff[i].GetComponent<CookRoutine>();
                    if (i < sim.Cooks)
                    {
                        if (cr == null) cr = _staff[i].AddComponent<CookRoutine>();
                        cr.Init(WalkerOf(_staff[i]), _staffFigure[i],
                                i, KitchenPosts, IngredientPrefabs, PlatePrefab,
                                _plateSpot);
                        _cookRoutine[i] = cr;
                    }
                    else
                    {
                        if (cr != null)
                        {
                            cr.Cancel();
                            if (Application.isPlaying) Destroy(cr);
                            else DestroyImmediate(cr);
                        }
                        _cookRoutine[i] = null;
                    }
                }

                for (int i = 0; i < _staff.Count; i++)
                {
                    bool isCook = i < sim.Cooks;

                    // CANCEL THE ROUTINE BEFORE TELEPORTING.
                    //
                    // Warp clears the path and Walker.Moving becomes false; the
                    // routine reads that as "I have arrived", puts the pan on an
                    // empty stove and played the cooking pose at home.
                    if (i < _cookRoutine.Count && _cookRoutine[i] != null)
                        _cookRoutine[i].Cancel();

                    Vector3 home = isCook ? Paths.CookHome(i, Mathf.Max(1, sim.Cooks))
                                      : Paths.HallHome(i - sim.Cooks,
                                                        Mathf.Max(1, sim.HallStaff));
                    WalkerOf(_staff[i]).Warp(home, isCook ? 180f : 0f);
                    _staffTask[i] = int.MinValue;
                    if (i < _washHold.Count) _washHold[i] = 0f;
                }

                _staffBuilt = stamp;
            }

            // THE STAFF SET OFF NOT EVERY FRAME BUT WHEN THEIR TASK CHANGES.
            //
            // The simulation says "the waiter is dealing with table 3", "the
            // cook is at the grill"; where to walk is the view layer's
            // business. While the target stays the same no new path is given -
            // otherwise the figure would start again every frame and never
            // arrive.
            // A WORKING FIGURE'S ANIMATOR IS KEPT AWAKE - EVERY FRAME.
            //
            // This line used to sit INSIDE the loop below, and that loop only
            // runs WHEN THE TASK CHANGES: so the Animator was woken once,
            // switched off a second later, and the cook froze on the first
            // frame of the chopping clip. And it was, by mistake, only in the
            // preview branch - it was never called in the game at all.
            //
            // "The pose was given" and "the animation is running" are two
            // different things; this loop provides the second.
            for (int i = 0; i < _staff.Count; i++)
            {
                Figure af = _staffFigure[i];
                if (af == null) continue;
                Figure.Pose p = af.Current;
                if (p == Figure.Pose.Chop || p == Figure.Pose.Wash
                    || p == Figure.Pose.Serve || p == Figure.Pose.Carry)
                {
                    af.HoldAwake();

                    // IS THE WORK CLIP REALLY PLAYING?
                    //
                    // "The pose was given" and "the animation is running" are two
                    // different things, and the second is only measured by asking
                    // whether the clip IS ADVANCING. This was exactly the user's
                    // complaint: "the dishwasher is not scrubbing the dishes, the
                    // cook is not stirring the food". The cause was not the
                    // animator but THE CLIPS BEING IMPORTED WITHOUT LOOPING - the
                    // clip played once and stopped on its last frame.
                    float progress = af.ClipProgress;
                    if (progress >= 0f)
                    {
                        float previous;
                        if (_workClip.TryGetValue(i, out previous))
                        {
                            if (progress > previous + 0.0001f) WorkAnimAdvanced++;
                            else WorkAnimStalled++;
                        }
                        _workClip[i] = progress;
                    }
                }
                else _workClip.Remove(i);
            }

            // THE COOKS EVERY FRAME, THE WAITERS WHEN THEIR TASK CHANGES.
            //
            // They HAVE to be separate: the core's cook task can be shorter
            // than a tick, and a loop that asks "has the task changed" MISSES
            // it - it was measured, the simulation gave work ten times and the
            // view saw it zero times. The cook's routine runs its own timing
            // anyway, so asking "is there work" every frame is cheap and
            // right.
            for (int i = 0; i < _staff.Count && i < sim.Cooks; i++)
            {
                CookRoutine cr = i < _cookRoutine.Count ? _cookRoutine[i] : null;
                if (cr == null || cr.Busy) continue;

                int task = sim.CookTaskStation(i);
                if (task >= 0) cr.Begin(task, StoveOf(task));
                else
                {
                    Walker cw = WalkerOf(_staff[i]);
                    if (cw != null && !cw.Moving)
                        SendCookHome(i, sim, cw, _staffFigure[i]);
                }
            }

            for (int i = sim.Cooks; i < _staff.Count; i++)
            {
                bool isCook = false;
                int server = i - sim.Cooks + 1;

                // THE VISIT TO THE SINK IS HELD FOR A WHILE.
                //
                // The core's washing task is 2,000 ms and the game runs at x4:
                // half a second. The walk to the sink, though, is a few seconds -
                // so the task ends BEFORE the figure arrives, the view sends it
                // somewhere else and the player NEVER sees the washing up being
                // done. It was measured: "seen at the sink 0" on every run of the
                // tour.
                //
                // The same class of bug was there with the cook and was solved the
                // same way (CookRoutine): the view runs its own sequence rather
                // than the core's instantaneous flag. The core says "it was
                // washed"; the view tells the DURATION.
                if (_washHold[i] > 0f)
                {
                    _washHold[i] -= Time.deltaTime;
                    if (_washHold[i] > 0f) continue;
                }

                // THE SINK IS A SEPARATE TASK, NOT "IDLE".
                //
                // The washing's target is not a table, so HallTaskTable returns
                // -1 - and -1 is the same number as "idle". If they are not told
                // apart, the washing waiter is sent home and the player never
                // sees the washing up being done.
                int task = sim.HallWashing(server) ? WashTask
                                                     : sim.HallTaskTable(server);

                if (task == _staffTask[i]) continue;
                _staffTask[i] = task;

                Walker w = WalkerOf(_staff[i]);
                Figure f = _staffFigure[i];
                if (w == null) continue;

                if (PreviewPoses)
                {
                    // The preview is a single frame: no walking, the pose is enough.
                    Pose(f, isCook ? KitchenPose(task) : Figure.Pose.Carry);
                    continue;
                }

                ShowSponge(_staff[i], task == WashTask);

                if (task == WashTask)
                {
                    int idx = i;
                    // IT GOES TO THE WASHING UP, WITH DIRTY PLATES IN ITS HANDS.
                    //
                    // The user's sentence: "let the waiter take the plates that
                    // have been eaten from and leave them in the dishwasher's
                    // dirty-plate area". The plates are let go when it reaches the
                    // sink - their being added to the stack comes from the
                    // simulation, here only their being carried is shown.
                    // THE HOLD STARTS WHEN IT SETS OFF, NOT ON ARRIVAL.
                    //
                    // The first version started the counter in the arrival callback
                    // and IT DID NOT WORK - it was measured, "seen at the sink 0"
                    // on all three runs. The reason: the core's task ends before
                    // the figure arrives, the task changes and the view sends it
                    // somewhere else halfway along. So the arrival callback NEVER
                    // ran.
                    //
                    // It now sets off with the safety timeout (nobody interrupts
                    // the walk) and on arrival the counter is brought down to the
                    // real waiting time.
                    _washHold[i] = WashWalkTimeout;

                    ShowCarry(_staff[i], 2);
                    Paths.Between(_path, w.transform.localPosition, _washSpot);
                    GameObject washBody = _staff[i];
                    w.GoTo(_path, 0f, () =>
                    {
                        Pose(f, Figure.Pose.Wash);
                        // The dirty stack has been put down, ONE PLATE IS LEFT IN
                        // HAND: that is the plate being washed. A figure making a
                        // scrubbing motion with an empty hand does not read as
                        // "washing".
                        ShowCarry(washBody, 1);
                        // It stays at the sink for a VISIBLE length of time.
                        _washHold[idx] = WashVisitSeconds;
                    });
                    continue;
                }

                if (task < 0)
                {
                    ShowCarry(_staff[i], 0);
                    // Idle: it walks home.
                    Vector3 home = isCook ? Paths.CookHome(i, Mathf.Max(1, sim.Cooks))
                                      : Paths.HallHome(i - sim.Cooks,
                                                        Mathf.Max(1, sim.HallStaff));
                    Paths.Between(_path, w.transform.localPosition, home);

                    // NaN = stay facing the direction of the last step. Writing
                    // a fixed angle had idle staff always facing the same way -
                    // one of the things the user saw.
                    w.GoTo(_path, float.NaN, () => Pose(f, Figure.Pose.Idle));
                    continue;
                }

                else
                {
                    // THE WAITER WALKS TO THE TABLE, with a plate in hand if it
                    // is carrying food.
                    if (task >= _tables.Count) continue;
                    Vector3 table = _tables[task].localPosition;
                    Vector3 beside = Paths.BesideTable(table);
                    Paths.Between(_path, w.transform.localPosition, beside);

                    // HOW MANY PLATES: the one this waiter is carrying, plus the
                    // other tables waiting for food at the same moment - up to the
                    // tray's capacity. The number is read from the simulation.
                    bool carrying = sim.HallCarrying(i - sim.Cooks + 1);
                    ShowCarry(_staff[i], carrying ? 1 + WaitingForFood(sim, task) : 0);

                    GameObject body = _staff[i];
                    w.GoTo(_path, Paths.FaceFrom(beside, table), () =>
                    {
                        Pose(f, Figure.Pose.Carry);
                        // The plate stays on the table ON ARRIVAL: that is the
                        // picture of its having been served.
                        ShowCarry(body, 0);
                    });
                }
            }
        }

        /// <summary>The cook going back to where it stands when idle.</summary>
        private void SendCookHome(int index, Simulation sim, Walker w, Figure f)
        {
            Vector3 home = Paths.CookHome(index, Mathf.Max(1, sim.Cooks));
            if ((w.transform.localPosition - home).sqrMagnitude < 0.09f) return;
            Paths.Between(_path, w.transform.localPosition, home);
            w.GoTo(_path, float.NaN, () => Pose(f, Figure.Pose.Idle));
        }

        /// How many tables besides this one are waiting for food.
        ///
        /// It decides how many plates go on the waiter's tray: carrying a tray
        /// in an empty hall makes no sense, and nor does going back and forth
        /// with a single plate in a full one.
        /// </summary>
        private static int WaitingForFood(Simulation sim, int exceptTable)
        {
            int n = 0;
            for (int t = 0; t < sim.TableCount; t++)
            {
                if (t == exceptTable) continue;
                if (sim.TableStage(t) == CustomerStage.WaitingForFood) n++;
            }
            return n;
        }

        /// <summary>The station's stove (as an object). The pan is put there.</summary>
        private Transform StoveOf(int station)
        {
            if (_stoves.Count == 0) return null;
            int i = (station < 0 ? 0 : station) % _stoves.Count;
            return _stoves[i] != null ? _stoves[i].transform : null;
        }

        /// The position of the station's stove. The wall side if there is no
        /// stove.
        ///
        /// The mapping is THE SAME as UpdateAppliances': stove i stands for
        /// station i and station (i+3). Had they been written separately the
        /// cook would be facing a stove that is not lit.
        /// </summary>
        private Vector3 StovePos(int station, Vector3 fallbackFrom)
        {
            if (_stoves.Count > 0)
            {
                int i = (station < 0 ? 0 : station) % _stoves.Count;
                if (_stoves[i] != null)
                    return _stoves[i].transform.localPosition;
            }
            // With no stove, facing the back (the wall): the counter is there.
            return fallbackFrom + new Vector3(0f, 0f, 1f);
        }

        /// The station's kitchen job. Three different actions, fixed per
        /// station: the same cook does the same job at the same stove every
        /// time, so the picture does not flicker while three different things
        /// happen in the kitchen.
        /// </summary>
        private static Figure.Pose KitchenPose(int station)
        {
            int i = station < 0 ? 0 : station;
            switch (i % 3)
            {
                case 0: return Figure.Pose.Chop;
                case 1: return Figure.Pose.Wash;
                default: return Figure.Pose.Serve;
            }
        }

        /// THE STOVES LIGHT ACCORDING TO THE SIMULATION.
        ///
        /// The kitchen covers a third of the screen and nothing was happening
        /// inside it. The core knows at every moment how many plates are
        /// cooking at which station (StationLoad); that information was drawn
        /// nowhere. In a management game "the kitchen is jammed" is the
        /// decision taken most often, and the only place the player can see it
        /// is the kitchen itself.
        ///
        /// The number of stoves is NOT the same as the number of stations:
        /// three stoves, six stations. Stove i stands for stations i, (i+3),
        /// and so on - the same mapping is in Paths.KitchenPost, which picks
        /// the counter the cook walks to, so the cook goes to the stove that
        /// is lit.
        /// </summary>
        private void UpdateAppliances(Simulation sim)
        {
            if (_stoves.Count == 0) return;

            int n = _stoves.Count;
            for (int i = 0; i < n; i++)
            {
                // THE BACKLOG IS SUMMED, NOT TESTED.
                //
                // This used to `break` on the first station with anything on
                // it, so the stove knew "somebody is cooking" and nothing
                // else. Stove i stands for stations i, i+n, i+2n..., so the
                // load it should show is the load of all of them together -
                // and the slots likewise, or a stove standing for two stations
                // would read as jammed whenever both were merely busy.
                //
                // The upper limit is FIXED: App can be null in the preview and
                // StationLoad returns 0 out of range anyway.
                int load = 0, slots = 0;
                for (int st = i; st < 16; st += n)
                {
                    load += sim.StationLoad(st);
                    slots += sim.StationSlotCount(st);
                }
                _stoves[i].SetLoad(load, slots);
            }
        }

        /// Switches the plate in the waiter's hand on and off.
        ///
        /// The plate is attached to the BODY, not to a bone. Looking for a
        /// hand bone (by name matching) breaks silently when the asset pack
        /// changes; a plate standing in front of the body at chest height
        /// reads as "carrying a tray" at this camera distance and carries no
        /// assumptions at all.
        ///
        /// The object is built once and afterwards only hidden and shown:
        /// creating and destroying it dozens of times through a service means
        /// visible garbage on mobile.
        /// </summary>
        /// IN THE WAITER'S HANDS: A SINGLE PLATE OR A TRAY.
        ///
        /// The user's request: "let the waiter be able to carry a certain
        /// number of dishes and drinks each time, and let whether they use a
        /// tray affect that number too".
        ///
        /// THE RULE: a single plate is carried IN THE HAND; two or more ON A
        /// TRAY. The tray's capacity is TrayCapacity. If fewer tables are
        /// waiting than that, the waiter takes only that many - carrying an
        /// empty tray would read as "wandering about" rather than "working
        /// efficiently".
        ///
        /// WHY IN THE VIEW LAYER: the core's hall capacity is a person-day
        /// model (the crew scale in the docs), not a per-trip carry. Putting a
        /// real constraint here would change the economy and
        /// would need the sixty-day balance to be solved again. The number is
        /// read FROM THE SIMULATION (how many tables are waiting for food), so
        /// it is not made up; it is simply a PICTURE rather than a constraint.
        /// </summary>
        private void ShowCarry(GameObject staff, int count)
        {
            if (staff == null || PlatePrefab == null) return;
            count = Mathf.Clamp(count, 0, TrayCapacity);

            Transform tray = staff.transform.Find("Tray");
            if (tray == null && count > 1) tray = BuildTray(staff.transform);
            if (tray != null) tray.gameObject.SetActive(count > 1);

            for (int i = 0; i < TrayCapacity; i++)
            {
                string name = "Plate" + i;
                Transform t = staff.transform.Find(name);
                bool wanted = i < count;

                if (t == null)
                {
                    if (!wanted) continue;
                    GameObject p = Instantiate(PlatePrefab, staff.transform);
                    p.name = name;
                    p.transform.localRotation = Quaternion.identity;
                    t = p.transform;
                }

                // A single plate IN THE HAND (a little lower), several plates ON
                // THE TRAY (a little higher and laid out sideways).
                t.localPosition = count > 1
                    ? new Vector3((i - (count - 1) * 0.5f) * 0.17f, 0.66f, 0.30f)
                    : new Vector3(0f, 0.62f, 0.28f);

                if (t.gameObject.activeSelf != wanted)
                    t.gameObject.SetActive(wanted);
            }
        }

        /// <summary>The tray: a single box, because the pack does not carry one.</summary>
        private Transform BuildTray(Transform staff)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Tray";
            go.transform.SetParent(staff, false);
            go.transform.localPosition = new Vector3(0f, 0.63f, 0.30f);
            go.transform.localScale = new Vector3(0.52f, 0.025f, 0.34f);

            Collider col = go.GetComponent<Collider>();
            if (col != null)
            {
                if (Application.isPlaying) Destroy(col); else DestroyImmediate(col);
            }

            Renderer r = go.GetComponent<Renderer>();
            r.sharedMaterial = _floorMat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            if (_block == null) _block = new MaterialPropertyBlock();
            r.GetPropertyBlock(_block);
            _block.SetColor(BaseColorId, new Color(0.34f, 0.26f, 0.19f));
            r.SetPropertyBlock(_block);
            return go.transform;
        }

        /// The most plates that can be carried on a tray.
        ///
        /// Three: a fourth spills past the figure's width (the plate is 0.17 m
        /// across, the figure 0.85 m) and from a 34 degree view the tray reads
        /// as a slab.
        /// </summary>
        public const int TrayCapacity = 3;

        /// <summary>The number of waiters carrying a tray right now. For the tour.</summary>
        /// <summary>The number of staff built in the scene. The upper limit of the tray measurement.</summary>
        public int StaffCount { get { return _staff.Count; } }

        public int TrayCount
        {
            get
            {
                int n = 0;
                for (int i = 0; i < _staff.Count; i++)
                {
                    if (_staff[i] == null) continue;
                    Transform t = _staff[i].transform.Find("Tray");
                    if (t != null && t.gameObject.activeSelf) n++;
                }
                return n;
            }
        }

        /// The number of working posts in the kitchen. The row of stoves is
        /// three pieces; the cook moves between them according to the station.
        /// </summary>
        private const int KitchenPosts = 3;

        /// <summary>The last task each staff member saw. int.MinValue: none yet.</summary>
        private readonly List<int> _staffTask = new List<int>();

        /// <summary>Each cook's cooking routine. Null on the waiters.</summary>
        private readonly List<CookRoutine> _cookRoutine = new List<CookRoutine>();

        /// <summary>The prefabs for the ingredients in the cook's hands.</summary>
        public GameObject[] IngredientPrefabs;

        /// Sets the figure's pose.
        ///
        /// The Figure reference comes FROM THE CALLER and is not looked up
        /// every time: the component is not at the prefab's root but INSIDE it
        /// (the clip paths are written relative to the FBX root) and
        /// GetComponentInChildren walks the subtree.
        /// </summary>
        private void Pose(Figure f, Figure.Pose pose)
        {
            if (f == null) return;
            if (PreviewPoses) f.Sample(pose, PreviewTime);
            else f.Set(pose);
        }

        // --- the pool ---------------------------------------------------------
        private GameObject Take()
        {
            for (int i = 0; i < _pool.Count; i++)
                if (_pool[i] != null && !_pool[i].activeSelf) return _pool[i];

            if (CustomerPrefabs == null || CustomerPrefabs.Length == 0) return null;

            // The figure is chosen BY POOL ORDER, not at random: randomness has
            // to come from the core's streams, the view layer must not roll its
            // own dice (docs/23, replay).
            GameObject prefab = CustomerPrefabs[_pool.Count % CustomerPrefabs.Length];
            GameObject go = Instantiate(prefab, transform);
            go.SetActive(false);
            _pool.Add(go);
            return go;
        }

        /// The Figure component of a figure in the pool. Resolved once and
        /// kept in a dictionary: the pool is of fixed size and the same
        /// objects sit down again and again through the day.
        /// </summary>
        private Figure FigureOf(GameObject go)
        {
            if (_figureOf.TryGetValue(go, out Figure f)) return f;
            f = go.GetComponentInChildren<Figure>(true);
            _figureOf[go] = f;
            return f;
        }

        private readonly Dictionary<GameObject, Figure> _figureOf =
            new Dictionary<GameObject, Figure>();

        private void Give(GameObject go)
        {
            if (go == null) return;

            // A FIGURE GOING BACK TO THE POOL FORGETS ITS POSE.
            //
            // The Animator switches off when the transition ends
            // (Figure.Update); if a figure taken from the pool again asks for
            // the same pose, Set() returns early and the Animator stayed off -
            // so the figure comes back in its old pose and no new transition
            // ever starts.
            Figure f = FigureOf(go);
            if (f != null) f.Release();

            go.SetActive(false);
            go.transform.SetParent(transform, false);
        }
    }
}
