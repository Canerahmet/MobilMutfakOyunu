using UnityEngine;

namespace Lokanta.Game
{
    /// <summary>
    /// THE COOK'S COOKING SEQUENCE: fetch the ingredient, wash it, chop
    /// it, cook it, put it on the plate.
    ///
    /// WHY A SEQUENCE, AND WHY IT DOES NOT LOOK AT THE SIMULATION EVERY
    /// FRAME:
    ///
    /// It was measured (Autopilot, "the simulation gave work 145 times"):
    /// the core keeps the cook's job open on only 10% of the frames - a
    /// job lasts a few hundred milliseconds. When the view followed that
    /// directly, the cook took two steps towards the fridge and turned
    /// back; it could NEVER REACH a station, and so could never enter a
    /// working pose. The kitchen was completely empty, and this was why.
    ///
    /// The answer: when the core gives a job the view STARTS A SEQUENCE
    /// and plays that sequence through to the end. The economy does not
    /// change - nothing here is written back to the core, it only tells
    /// what it reads at human speed.
    ///
    /// It is also the answer to the "let us see the stages" request: every
    /// stage has its own place, its own pose and its own object.
    /// </summary>
    public sealed class CookRoutine : MonoBehaviour
    {
        public enum Stage
        {
            Idle, ToPlate, TakePlate, ToFridge, Take, ToCounter, Wash, Chop,
            ToStove, Cook, Plate, ToPass
        }

        private Walker _walk;
        private Figure _fig;
        private int _post;          // the cook's index (how the counters are shared out)
        private int _posts = 3;

        private Stage _stage = Stage.Idle;
        private float _left;
        private int _station = -1;
        private bool _issued;       // has this stage's walk order been given?

        private GameObject _held;   // the ingredient/plate in its hand
        private GameObject _pan;    // the pan on the stove
        private Transform _cold;
        private Transform _stove;   // the stove the pan goes on

        private GameObject[] _ingredients;
        private GameObject _platePrefab;

        /// <summary>The front of the clean plate stack. It comes from the view.</summary>
        private Vector3 _plateSpot;

        /// <summary>Is a sequence running right now?</summary>
        public bool Busy { get { return _stage != Stage.Idle; } }

        /// <summary>The current stage. So the tour can ask.</summary>
        public Stage Current { get { return _stage; } }

        // =====================================================================
        public void Init(Walker walk, Figure fig, int post, int posts,
                         GameObject[] ingredients, GameObject platePrefab,
                         Vector3 plateSpot, Transform cold)
        {
            // WHERE THE COLD STORE ACTUALLY IS, handed in rather than derived.
            //
            // Paths.Fridge and Paths.FridgeFace still point at
            // `X0 + W - 0.55`, which is where FridgePrefab stood before the
            // kitchen was rebuilt from the station list (docs/59). That prefab
            // is gone; the cold counter is the `soguk` station and the layout
            // decides where it stands. The cook was walking to a fixed point
            // that now lands INSIDE whatever station the packer put on the
            // back wall, playing "pick up" against a steel side for a second,
            // once per plate.
            _cold = cold;
            _walk = walk;
            _fig = fig;
            _post = post;
            _posts = Mathf.Max(1, posts);
            _ingredients = ingredients;
            _platePrefab = platePrefab;
            _plateSpot = plateSpot;
        }

        /// <summary>
        /// A new job. If the sequence is already running it IS NOT TOUCHED:
        /// a half-finished cook must not start again because a new order has
        /// come in.
        /// </summary>
        public void Begin(int station, Transform stove)
        {
            if (_stage != Stage.Idle) return;
            _station = station;
            _stove = stove;
            Go(Stage.ToPlate);
        }

        /// <summary>Cuts the sequence off and drops what it is holding. At the end of the day.</summary>
        public void Cancel()
        {
            _stage = Stage.Idle;
            // CUT THE WALK TOO: a half-finished path kept the figure walking
            // with its sequence already cancelled.
            if (_walk != null) _walk.Stop();
            Drop();
            Pan(false);
        }

        // =====================================================================
        private void Update()
        {
            if (_stage == Stage.Idle) return;
            if (_walk == null || _fig == null) { _stage = Stage.Idle; return; }

            // WHEN THE GAME IS PAUSED THE KITCHEN STOPS TOO.
            //
            // The Mathf.Max(0.25f, ...) floor below was SWALLOWING the zero:
            // in a paused world the cook went on chopping at quarter speed.
            // The pause screen covers a large part of the game (the morning,
            // the evening, the settings), so this was on screen constantly.
            if (Walker.GameSpeed <= 0.001f) return;

            // A working figure's Animator has to stay awake: Figure switches
            // it off ~1 s after the transition and the chopping clip freezes
            // on its first frame.
            _fig.HoldAwake();

            _stageAge += Time.deltaTime;
            if (_stageAge > StageTimeout)
            {
                // The sequence is stuck: drop it, clean up, go idle. The next
                // job starts it again. Better than staying silently locked.
                Debug.LogWarning("the cook sequence timed out: " + _stage);
                Cancel();
                Pose(Figure.Pose.Idle);
                return;
            }

            switch (_stage)
            {
                case Stage.ToPlate:
                    // THE CLEAN PLATE IS FETCHED FIRST.
                    //
                    // The user's sentence: "let the cook take the plate from there
                    // and put the food on it". It goes at the VERY START of the
                    // sequence, not in the middle of the cooking: a cook who leaves
                    // a hot pan and walks out of the room does not read as
                    // "fetching a plate" but as "went off somewhere". In a real
                    // kitchen the plate is got ready beforehand too.
                    //
                    // This path goes through the kitchen-wash doorway, and that
                    // doorway is a close thing: the shared edge is 1.40 m and the
                    // threshold is exactly 1.40 (see RestaurantView.MinJamb). If the
                    // floor plan changes, the cook will start going round through
                    // the entrance.
                    if (Walk(_plateSpot, _plateSpot + new Vector3(0f, 0f, 1f)))
                        Go(Stage.TakePlate);
                    break;

                case Stage.TakePlate:
                    if (Enter()) Pose(Figure.Pose.Pick);
                    if (Tick(0.8f)) Go(Stage.ToFridge);
                    break;

                case Stage.ToFridge:
                    // The cook stands in front of the cold counter and faces
                    // it - the station's own local -Z is its working face, the
                    // same rule Post() uses. With no cold station in this
                    // cuisine the stage is skipped rather than walked to a
                    // remembered coordinate.
                    if (_cold == null) { Go(Stage.Take); break; }
                    if (Walk(_cold.localPosition
                             + _cold.localRotation * new Vector3(0f, 0f, -0.95f * KitchenStation.FurnitureScale),
                             _cold.localPosition)) Go(Stage.Take);
                    break;

                case Stage.Take:
                    if (Enter()) { Pose(Figure.Pose.Pick); Hold(Ingredient()); }
                    if (Tick(1.0f)) Go(Stage.ToCounter);
                    break;

                case Stage.ToCounter:
                    // THE PREP COUNTERS ARE GONE TOO. The kitchen rebuild
                    // replaced the left-wall prep run with stations; the cook
                    // now washes and chops at its OWN station, which is where
                    // it is going to cook anyway. Paths.PrepPost still derives
                    // from a `LineUp back:false` that no longer exists.
                    if (Walk(Post(), StovePos())) Go(Stage.Wash);
                    break;

                case Stage.Wash:
                    if (Enter()) Pose(Figure.Pose.Wash);
                    if (Tick(1.5f)) Go(Stage.Chop);
                    break;

                case Stage.Chop:
                    if (Enter()) Pose(Figure.Pose.Chop);
                    if (Tick(1.9f)) Go(Stage.ToStove);
                    break;

                case Stage.ToStove:
                    if (Walk(Post(), StovePos()))
                    {
                        Drop();
                        Pan(true);
                        Go(Stage.Cook);
                    }
                    break;

                case Stage.Cook:
                    if (Enter()) Pose(Figure.Pose.Serve);
                    if (Tick(2.8f)) Go(Stage.Plate);
                    break;

                case Stage.Plate:
                    if (Enter()) { Pose(Figure.Pose.Pick); }
                    if (Tick(1.0f))
                    {
                        Pan(false);
                        Hold(_platePrefab);
                        Go(Stage.ToPass);
                    }
                    break;

                case Stage.ToPass:
                    if (Walk(Paths.PrepPost(_post, _posts),
                             Paths.PrepCounter(_post, _posts)))
                    {
                        Drop();
                        Pose(Figure.Pose.Idle);
                        _stage = Stage.Idle;
                    }
                    break;
            }
        }

        // =====================================================================
        private void Go(Stage s)
        {
            _stage = s;
            _left = -1f;
            _issued = false;
            _stageAge = 0f;
        }

        /// <summary>Has this stage just been ENTERED? The pose is given once.</summary>
        private bool Enter()
        {
            if (_issued) return false;
            _issued = true;
            return true;
        }

        private bool Tick(float seconds)
        {
            if (_left < 0f) _left = seconds;
            _left -= Time.deltaTime * Mathf.Max(0.25f, Walker.GameSpeed);
            return _left <= 0f;
        }

        /// <summary>
        /// Walks to the target; true on arrival. The order is given ONCE,
        /// otherwise the figure starts again every frame and never arrives.
        /// </summary>
        /// <summary>
        /// The stage's LOOK target. The check measures against this.
        ///
        /// It used to be assumed that "a working cook faces the stove", and
        /// once the stages were added that assumption became wrong: while
        /// washing and chopping it faces the counter, not the stove. The
        /// check measured a 176 degree deviation and it was RIGHT - it was
        /// looking at the wrong target.
        /// </summary>
        public Vector3 LookTarget { get; private set; }

        private bool Walk(Vector3 target, Vector3 lookAt)
        {
            if (!_issued)
            {
                _issued = true;
                LookTarget = lookAt;
                _target = target;
                _stageAge = 0f;
                _path.Clear();
                Paths.Between(_path, transform.localPosition, target);
                _walk.GoTo(_path, Paths.FaceFrom(target, lookAt), null);
                Pose(Figure.Pose.Walk);
                return false;
            }

            // ARRIVAL IS MEASURED BY DISTANCE, NOT BY A FLAG.
            //
            // Only !Moving used to be checked, and Warp (when the crew is
            // laid out again) clears the path and makes Moving false - so a
            // cook teleported HOME said "I have reached the stove", the pan
            // was put on an empty stove and the cooking pose played at home.
            if (_walk.Moving) return false;
            return (transform.localPosition - _target).sqrMagnitude < 0.16f;
        }

        /// <summary>
        /// How long has been spent in this stage. For the timeout: a walk
        /// that never arrives would lock the sequence for ever, and the cook
        /// would freeze in whatever it was doing at that moment.
        /// </summary>
        private float _stageAge;
        private Vector3 _target;

        /// <summary>The longest a stage may take (s).</summary>
        private const float StageTimeout = 18f;

        private static readonly System.Collections.Generic.List<Vector3> _path =
            new System.Collections.Generic.List<Vector3>();

        private void Pose(Figure.Pose p) { if (_fig != null) _fig.Set(p); }

        private Vector3 StovePos()
        {
            if (_stove != null) return _stove.localPosition;
            return Paths.KitchenPost(_station, _posts) + new Vector3(0f, 0f, 1f);
        }

        /// <summary>
        /// Where the cook stands to work: IN FRONT OF ITS OWN STATION.
        ///
        /// It used to stand at Paths.KitchenPost, a fixed row along the back
        /// wall, whatever station the job was at. That was fine while the
        /// kitchen was three identical stoves in a row; now that every station
        /// has its own object and they line three walls (docs/59), a cook
        /// working at the stone oven on the LEFT wall stood at the back of the
        /// room facing nothing - and the placement audit caught it standing
        /// 0.07 m inside the oven.
        ///
        /// The station's own local -Z is its front: the models are built with
        /// their working face at -d/2 and the layout turns them to the wall.
        /// So the post is the station's position pushed a metre out of its
        /// face, and it follows the station wherever the layout puts it.
        /// </summary>
        private Vector3 Post()
        {
            if (_stove == null) return Paths.KitchenPost(_station, _posts);
            return _stove.localPosition
                   + _stove.localRotation * new Vector3(0f, 0f, -0.95f * KitchenStation.FurnitureScale);
        }

        private GameObject Ingredient()
        {
            if (_ingredients == null || _ingredients.Length == 0) return null;
            int i = Mathf.Abs(_station + _post * 7) % _ingredients.Length;
            return _ingredients[i];
        }

        /// <summary>Puts something in its hand. Deletes the previous one.</summary>
        private void Hold(GameObject prefab)
        {
            Drop();
            if (prefab == null) return;
            _held = Instantiate(prefab, transform);
            _held.name = "InHand";
            _held.transform.localPosition = new Vector3(0f, 0.60f, 0.26f);
            _held.transform.localRotation = Quaternion.identity;
        }

        private void Drop()
        {
            if (_held == null) return;
            if (Application.isPlaying) Destroy(_held); else DestroyImmediate(_held);
            _held = null;
        }

        /// <summary>
        /// A PAN ON THE STOVE. The pack carries no pan model; it is made
        /// from two boxes - a body and a handle. And on top of it, what is
        /// cooking: a small, warm-coloured box.
        /// </summary>
        private void Pan(bool on)
        {
            // THE PAN IS BUILT ONCE, THEN SWITCHED ON AND OFF.
            //
            // It used to be built with a `new GameObject` + three
            // `CreatePrimitive` on every cook and destroyed at the plating
            // stage. The material leak had already been fixed in this file,
            // but the OBJECT rubbish was still there: three cooks x a few
            // plates a minute = steady GC pressure. The rest of the project
            // (the plate stack, the table's plate, the tray, the guest figure)
            // all use a pool and write down why - "creating and destroying
            // objects every frame means dozens of allocations a second at the
            // peak".
            if (!on)
            {
                if (_pan != null && _pan.activeSelf) _pan.SetActive(false);
                return;
            }
            if (_stove == null) return;

            if (_pan == null)
            {
                if (PanMaterial == null) return;
                _pan = new GameObject("Pan");
                Box(_pan.transform, new Vector3(0f, 0.02f, 0f),
                    new Vector3(0.26f, 0.04f, 0.26f), new Color(0.16f, 0.16f, 0.18f));
                Box(_pan.transform, new Vector3(0f, 0.02f, -0.22f),
                    new Vector3(0.05f, 0.03f, 0.18f), new Color(0.14f, 0.14f, 0.15f));
                Box(_pan.transform, new Vector3(0f, 0.05f, 0f),
                    new Vector3(0.18f, 0.03f, 0.18f), new Color(0.85f, 0.50f, 0.22f));
            }

            // THE STOVE CAN CHANGE WITH EVERY JOB (Begin gives a new stove),
            // so a pan coming out of the pool is positioned AGAIN every
            // time. Without these lines the pan would stay on the first
            // stove and the cook would stir an empty pot on another one.
            _pan.transform.SetParent(_stove.parent, false);
            Vector3 p = _stove.localPosition;
            _pan.transform.localPosition = new Vector3(p.x, StoveTop, p.z - 0.05f);
            if (!_pan.activeSelf) _pan.SetActive(true);
        }

        /// <summary>The height of the stove top (m). The ArtPrefabs target is 0.92.</summary>
        private const float StoveTop = 0.94f;

        /// <summary>
        /// A part of the pan.
        ///
        /// THE MATERIAL IS SHARED, NOT CREATED AFRESH FOR EVERY BOX.
        ///
        /// The first version did a `Shader.Find` + `new Material` for every
        /// box: three materials per pan, and because `Pan(false)` only
        /// destroyed the object, the materials were left orphaned. That
        /// means three leaks per cook FOR EVERY PLATE - thousands of
        /// material instances over a sixty-day campaign. On a low-end
        /// Adreno that is a slow exhaustion of memory.
        ///
        /// The colour is given with a MaterialPropertyBlock; the rest of the
        /// project already does it that way (Appliance.Paint).
        /// </summary>
        private static void Box(Transform parent, Vector3 pos, Vector3 size, Color c)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = size;

            Collider col = go.GetComponent<Collider>();
            if (col != null)
            {
                if (Application.isPlaying) Destroy(col); else DestroyImmediate(col);
            }

            Renderer r = go.GetComponent<Renderer>();
            r.sharedMaterial = PanMaterial;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            if (_paint == null) _paint = new MaterialPropertyBlock();
            r.GetPropertyBlock(_paint);
            _paint.SetColor(BaseColorId, c);
            r.SetPropertyBlock(_paint);
        }

        private static MaterialPropertyBlock _paint;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static Material _panMat;

        /// <summary>
        /// The pan's shared material. Built once.
        ///
        /// The last place where the view layer builds a material of its own;
        /// it is not transparent, so its shader variant is not stripped
        /// (transparent materials have to be assets - see docs/36).
        /// </summary>
        private static Material PanMaterial
        {
            get
            {
                if (_panMat != null) return _panMat;
                Shader sh = Shader.Find("Universal Render Pipeline/Lit");
                if (sh == null)
                {
                    // NO SILENT FALLBACK.
                    //
                    // It used to try `return null` and Box() then wrote a null
                    // material: on the device the pan is drawn in the DEFAULT
                    // (magenta) and it is never seen in the editor.
                    // RestaurantView.Awake logs an error in the same situation and
                    // switches itself off, and writes down why: "a silent fallback
                    // is the thing nobody will notice next time round".
                    Debug.LogError("PROBLEMS: the URP/Lit shader was not found, "
                                   + "the pan cannot be drawn");
                    return null;
                }
                _panMat = new Material(sh) { name = "PanShared" };
                return _panMat;
            }
        }
    }
}
