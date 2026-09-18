using System.Collections.Generic;
using UnityEngine;

namespace Lokanta.Game
{
    /// <summary>
    /// ONE OBJECT PER STATION, AND YOU CAN SEE WHAT YOU BOUGHT.
    ///
    /// docs/32 built the whole equipment ladder - seven shared stations, more per
    /// cuisine, slots, `attendBp`, a price derived from the rent of the tier at
    /// which the item becomes necessary - and the kitchen drew none of it.
    /// Three copies of one stove prefab stood at the back wall and
    /// `UpdateStoves` spread sixteen possible stations over them with
    /// `station % 3`, so stove 0 showed the sum of stations 0, 3, 6 and 9.
    /// Buying a grill changed nothing on screen. docs/59 puts the measurement
    /// bluntly: buy a station and count how many pixels of the frame change -
    /// the answer was zero.
    ///
    /// So every station the cuisine actually uses gets its own object, built
    /// here from the `Modeler` like everything else in this project: no new
    /// asset, no licence row, nothing downloaded.
    ///
    /// THE TIER IS IN THE GEOMETRY. A second burner ring, a second oven rack,
    /// a third fryer well - the thing you paid for is the thing that appears.
    /// That is the only way an equipment screen means anything in a game whose
    /// kitchen is a third of the frame.
    ///
    /// WHAT GLOWS IS A SEPARATE OBJECT ON AN UNLIT MATERIAL. `Appliance`
    /// learned this the hard way and the note is worth repeating: with a lit
    /// material the lamp inside an oven is drawn as a dark panel, because no
    /// light reaches in there. Each controllable light is its own small mesh
    /// so it can be lit on its own - the burners come on one at a time as the
    /// station fills.
    ///
    /// NO PARTICLES anywhere. docs/19 targets a low-end Adreno and the
    /// particle system is a documented fill-rate bottleneck on that class of
    /// device; a flame is a box.
    /// </summary>
    public sealed class KitchenStation : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        // --- the shared palette of a commercial kitchen ----------------------
        //
        // STAINLESS, NOT THE CUISINE'S METAL. The palette's Metal is brass in
        // the Turkish identity and the project has already been here once:
        // "the first attempt sent the palette's metal to every metal part and
        // the kitchen turned GOLD". Equipment is stainless in every
        // restaurant; the cuisine shows up in the STATIONS THAT EXIST - a
        // stone oven and a doner spit against a fryer and a waffle iron - not
        // in the paint.
        //
        // BUT "STAINLESS" WAS BEING READ AS "ONE COLOUR", AND IT IS NOT ONE.
        //
        // Twelve models were built and the line still came out as a grey band
        // with two warm dots in it, because three greys of the same hue is a
        // monochrome however many shapes are cut from it. A real kitchen line
        // is stainless AND a worktop AND a dark plinth AND a painted carcass -
        // four materials, not one, and they are what give a row of boxes its
        // rhythm.
        //
        // So the family is now four hues rather than one, and every value is
        // lifted: Graphite was 0.13, which at this light level is black, and
        // it is on every door panel in the room.
        //
        //   Steel     the appliance skin, a light neutral with a little warmth
        //   Worktop   the top surface: warm composite, the lightest thing here
        //   Carcass   the body below it, a soft slate - NEW, and the biggest
        //             single area in the line
        //   Graphite  the dark panels: warm charcoal, not blue-black
        // The spread between them matters as much as where they sit: at
        // 0.68 / 0.82 / 0.48 the line read as one pale mass, because three
        // light colours under a strong fill are one light colour. The
        // carcass drops to 0.40 so the worktop has something to sit on.
        private static readonly Color Steel = new Color(0.647f, 0.667f, 0.686f);
        private static readonly Color Worktop = new Color(0.804f, 0.776f, 0.729f);
        private static readonly Color SteelDark = new Color(0.404f, 0.427f, 0.463f);
        private static readonly Color Graphite = new Color(0.255f, 0.243f, 0.251f);
        private static readonly Color Rubber = new Color(0.169f, 0.161f, 0.169f);
        private static readonly Color Brick = new Color(0.596f, 0.443f, 0.365f);
        private static readonly Color Plaster = new Color(0.839f, 0.808f, 0.761f);
        private static readonly Color Oil = new Color(0.647f, 0.514f, 0.251f);
        private static readonly Color Meat = new Color(0.694f, 0.435f, 0.333f);
        private static readonly Color MeatCrust = new Color(0.569f, 0.337f, 0.251f);
        private static readonly Color Chip = new Color(0.902f, 0.780f, 0.424f);
        private static readonly Color Wood = new Color(0.647f, 0.514f, 0.384f);

        /// <summary>
        /// The kick strip's colour, which is the ONE part of the equipment the
        /// cuisine paints.
        ///
        /// The gold-kitchen failure is the reason this is a single named part
        /// and not a parameter threaded through every box: it is a 10 cm strip
        /// at floor level, under the whole run, and it does for a kitchen what
        /// a skirting board does for a room - it grounds it and it carries the
        /// temperature of the place without being the place. The appliances
        /// stay stainless.
        /// </summary>
        private Color _kick = new Color(0.255f, 0.243f, 0.251f);

        // --- what the lights mean -------------------------------------------
        //
        // The same three-step reading as the stove badge: at or under capacity
        // is calm, over it is amber, twice over is red. A colour alone would
        // not carry at this camera distance, so the COUNT of lit elements is
        // the channel and the colour backs it up - exactly the pairing the
        // table badge uses (length plus colour).
        private static readonly Color Off = new Color(0.055f, 0.055f, 0.070f);
        private static readonly Color Calm = new Color(0.353f, 0.620f, 1.000f);
        private static readonly Color Busy = new Color(1.000f, 0.600f, 0.161f);
        private static readonly Color Jammed = new Color(1.000f, 0.271f, 0.220f);

        /// <summary>Ember, flame and element colours, which are warm even when calm.</summary>
        private static readonly Color EmberCalm = new Color(0.937f, 0.353f, 0.157f);
        private static readonly Color EmberBusy = new Color(1.000f, 0.549f, 0.180f);

        // =====================================================================
        /// <summary>The station's content id, so the view can find it again.</summary>
        public string Id { get; private set; }

        /// <summary>The tier this object was BUILT at. If it changes, it is rebuilt.</summary>
        public int Tier { get; private set; }

        /// <summary>
        /// How much wall this station takes, MEASURED off the object once it
        /// is built rather than written down beside it.
        ///
        /// The layout used to space everything 1.05 m apart, which is right
        /// for a 0.90 m range and wrong for a 1.16 m stone oven: the placement
        /// audit caught the Turkish kitchen with its stone oven and its pide
        /// oven 0.86 m inside each other. A number typed in two places is a
        /// number that will disagree with itself.
        /// </summary>
        public float Width { get; private set; }

        /// <summary>How deep it is, measured the same way.</summary>
        public float Depth { get; private set; }

        private readonly List<Renderer> _lamps = new List<Renderer>();
        private MaterialPropertyBlock _block;
        private bool _warm;                 // ember colours rather than blue
        private int _lit = -1;
        private Color _colour = Color.clear;
        private Transform _hinge;           // an oven door, if it has one
        private float _stand, _standW, _standD;   // a bench appliance stands on one

        /// <summary>
        /// Where a pan can stand on this station, if anywhere.
        ///
        /// THE PAN GOES ON A BURNER, not on the middle of the top. The
        /// user's objection was exactly this - "when the cook is making the
        /// food, let the ring under the pot light up" - and it only reads
        /// as that if the flame and the pan are in the same place. So the
        /// anchor is created AT the ring, and the ring that lights first is
        /// the ring the first pan is on.
        /// </summary>
        public readonly List<Transform> PotSpots = new List<Transform>();
        private float _doorT, _doorTarget;

        // =====================================================================
        /// <summary>
        /// Builds the station. `unlit` must be URP/Unlit - see the note above.
        /// </summary>
        public static KitchenStation Build(Transform parent, string id, int tier,
                                           Vector3 at, float yaw,
                                           Material lit, Material unlit,
                                           MaterialPropertyBlock block,
                                           Color kick)
        {
            if (lit == null || unlit == null) return null;

            GameObject root = new GameObject("Station_" + id);
            root.transform.SetParent(parent, false);
            root.transform.localPosition = at;
            root.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

            KitchenStation s = root.AddComponent<KitchenStation>();
            s.Id = id;
            s.Tier = tier;
            s._block = block ?? new MaterialPropertyBlock();
            s._kick = kick;

            // EVERYTHING STANDS ON THE FLOOR, and the small ones stand on a
            // stand.
            //
            // The first version sorted the stations into "floor" and "bench"
            // and put the bench ones on the prep counters. It ran out of
            // kitchen: Turkish uses six floor stations and the back wall takes
            // four, so a stone oven, a doner spit and a pide oven were left
            // with nowhere to stand - the audit found all three of them piled
            // at the origin, 0.58 m outside the wash room.
            //
            // A milkshake machine on a stainless stand is what a burger bar
            // actually has, so the small appliances bring their own cabinet
            // and every station is laid out the same way. It also means the
            // layout has one list to solve instead of two.
            GameObject rig = new GameObject("Rig");
            rig.transform.SetParent(root.transform, false);

            Modeler m = new Modeler();
            switch (id)
            {
                case "ocak": s.Range(m, rig.transform, tier, unlit); break;
                case "izgara": s.Grill(m, rig.transform, tier, unlit); break;
                case "firin": s.Oven(m, rig.transform, tier, lit, unlit); break;
                case "fritoz": s.Fryer(m, rig.transform, tier, unlit); break;
                case "soguk": s.ColdCounter(m, rig.transform, tier, unlit); break;
                case "icecek": s.DrinksTower(m, rig.transform, tier, unlit); break;
                case "tatli": s.DessertCase(m, rig.transform, tier, unlit); break;
                case "milkshake_makinesi": s.Milkshake(m, rig.transform, tier, unlit); break;
                case "waffle_makinesi": s.WaffleIron(m, rig.transform, tier, unlit); break;
                case "tas_firin": s.StoneOven(m, rig.transform, tier, unlit); break;
                case "doner_ocagi": s.DonerSpit(m, rig.transform, tier, unlit); break;
                case "pide_firini": s.PideOven(m, rig.transform, tier, unlit); break;
                default:
                    // AN UNKNOWN STATION IS A BOX, NOT NOTHING. A station the
                    // content adds and this file has not caught up with would
                    // otherwise be invisible, and invisible is the exact
                    // failure this class exists to end. A grey crate is ugly
                    // enough to get noticed.
                    Debug.LogWarning("KitchenStation: no model for '" + id
                                     + "' - it is standing as a plain crate");
                    m.Box(new Vector3(0f, 0.44f, 0f), new Vector3(0.88f, 0.88f, 0.60f),
                          SteelDark);
                    s.Width = 0.90f;
                    break;
            }

            if (s._stand > 0f)
            {
                rig.transform.localPosition = new Vector3(0f, s._stand, 0f);
                Modeler sm = new Modeler();
                s.Plinth(sm, s._standW, s._standD, s._stand);
                sm.Build(root.transform, "Stand", lit, s._block);
            }

            GameObject body = m.Build(rig.transform, "Body", lit, s._block);
            body.transform.localPosition = Vector3.zero;

            // THE WIDTH COMES OFF THE OBJECT. Each model sets a Width as it
            // builds, and every one of those is a second copy of a number that
            // is already in the geometry. Measuring closes the gap: a peel
            // leaning against the stone oven counts, and it is what will
            // actually touch the neighbour.
            Renderer[] rs = root.GetComponentsInChildren<Renderer>();
            if (rs.Length > 0)
            {
                Bounds bb = rs[0].bounds;
                for (int i = 1; i < rs.Length; i++) bb.Encapsulate(rs[i].bounds);
                s.Width = bb.size.x;
                s.Depth = bb.size.z;
            }

            s.SetLoad(0, 1);
            return s;
        }

        // =====================================================================
        /// <summary>
        /// Reflects the station's load. `slots` is its capacity, so a full
        /// station reads as FULL rather than as some arbitrary fraction - the
        /// picture means "this station is at its limit", which is the thing
        /// the player is deciding about.
        /// </summary>
        public void SetLoad(int load, int slots)
        {
            if (slots < 1) slots = 1;
            int n = _lamps.Count;
            int want = load <= 0 ? 0
                : Mathf.Clamp(Mathf.CeilToInt((float)n * load / slots), 1, n);

            Color c;
            if (_warm)
                c = load <= slots ? EmberCalm
                    : (load >= slots * 2 ? Jammed : EmberBusy);
            else
                c = load <= slots ? Calm
                    : (load >= slots * 2 ? Jammed : Busy);

            _doorTarget = load > 0 ? 1f : 0f;

            if (want == _lit && c == _colour) return;
            _lit = want;
            _colour = c;

            for (int i = 0; i < n; i++) Paint(_lamps[i], i < want ? c : Off);
        }

        private void Paint(Renderer r, Color c)
        {
            if (r == null) return;
            r.GetPropertyBlock(_block);
            _block.SetColor(BaseColorId, c);
            r.SetPropertyBlock(_block);
        }

        /// <summary>
        /// The oven door. It drops open while something is inside and closes
        /// after - the same motion `Appliance` gives the prefab oven, and the
        /// same reason: a door that snaps is read as a glitch, so it takes
        /// nearly half a second.
        /// </summary>
        private void Update()
        {
            if (_hinge == null) return;
            if (Mathf.Approximately(_doorT, _doorTarget)) return;

            _doorT = Mathf.MoveTowards(_doorT, _doorTarget, Time.deltaTime / 0.45f);
            _hinge.localRotation = Quaternion.Euler(
                Mathf.SmoothStep(0f, -72f, _doorT), 0f, 0f);
        }

        // =====================================================================
        /// <summary>
        /// A controllable light: its own little mesh, so it can come on alone.
        ///
        /// It costs one renderer each and they are counted: the budget is 400
        /// renderers at full expansion and the whole kitchen's lights come to
        /// around thirty.
        /// </summary>
        private void Lamp(Transform parent, Vector3 at, Vector3 size, Material unlit)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Lamp";
            go.transform.SetParent(parent, false);
            go.transform.localPosition = at;
            go.transform.localScale = size;

            Collider col = go.GetComponent<Collider>();
            if (col != null)
            {
                if (Application.isPlaying) Destroy(col); else DestroyImmediate(col);
            }

            Renderer r = go.GetComponent<Renderer>();
            r.sharedMaterial = unlit;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            _lamps.Add(r);
            Paint(r, Off);
        }

        /// <summary>An empty marker a pan can be parented to.</summary>
        private void PanSpot(Transform parent, Vector3 at)
        {
            GameObject go = new GameObject("PanSpot");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = at;
            PotSpots.Add(go.transform);
        }

        /// <summary>A round controllable light - a burner ring, a hotplate.</summary>
        private void Ring(Transform parent, Vector3 at, float radius, Material unlit)
        {
            Lamp(parent, at, new Vector3(radius * 2f, 0.018f, radius * 2f), unlit);
        }

        /// <summary>
        /// The stand a bench appliance sits on: the same cabinet as every
        /// other station, so the row reads as one run of equipment rather than
        /// as machines of random heights.
        ///
        /// Separate from Carcass because Carcass also claims the station's
        /// Width, and the width that matters here is the APPLIANCE's.
        /// </summary>
        private void Plinth(Modeler m, float w, float d, float h)
        {
            m.Box(new Vector3(0f, h * 0.5f, 0f), new Vector3(w, h, d), SteelDark);
            m.Box(new Vector3(0f, h - 0.02f, 0f), new Vector3(w + 0.04f, 0.04f, d + 0.04f),
                  Worktop);
            m.Box(new Vector3(0f, 0.05f, 0f), new Vector3(w - 0.10f, 0.10f, d - 0.06f),
                  _kick);
            // A shelf inside, because an open stand with nothing in it reads
            // as a hole.
            m.Box(new Vector3(0f, h * 0.42f, 0f), new Vector3(w - 0.12f, 0.03f, d - 0.10f),
                  SteelDark);
        }

        /// <summary>The carcass every counter-height station shares.</summary>
        private void Carcass(Modeler m, float w, float d, float h)
        {
            Width = w + 0.04f;
            m.Box(new Vector3(0f, h * 0.5f, 0f), new Vector3(w, h, d), SteelDark);
            // The top, a touch proud of the body: a worktop has a lip. And it
            // is a WORKTOP colour now, not more steel - the top surface is the
            // one the 34 degree camera sees most of, so it is where a second
            // material buys the most.
            m.Box(new Vector3(0f, h + 0.02f, 0f), new Vector3(w + 0.05f, 0.04f, d + 0.05f),
                  Worktop);
            // The kick strip, so it does not read as a box sitting on the
            // floor - and it is the cuisine's one painted part. See _kick.
            m.Box(new Vector3(0f, 0.05f, 0f), new Vector3(w - 0.10f, 0.10f, d - 0.06f),
                  _kick);
            // Feet.
            for (int i = 0; i < 4; i++)
            {
                float fx = (i % 2 == 0 ? -1f : 1f) * (w * 0.5f - 0.08f);
                float fz = (i < 2 ? -1f : 1f) * (d * 0.5f - 0.08f);
                m.Box(new Vector3(fx, 0.025f, fz), new Vector3(0.05f, 0.05f, 0.05f),
                      Rubber);
            }

            // THE FRONT IS NOT A BLANK PANEL.
            //
            // The first line of stations came out as a row of plain steel
            // boxes: every interesting thing - the burner rings, the grill
            // bars, the oil - is on the TOP, and the top is only visible at
            // the game's 34 degree camera. Seen from anywhere lower the row
            // read as filing cabinets.
            //
            // A recessed door and a full-width rail is what the front of every
            // piece of commercial kitchen equipment has, and it costs four
            // boxes.
            m.Box(new Vector3(0f, h * 0.50f, -d * 0.5f - 0.012f),
                  new Vector3(w - 0.10f, h - 0.26f, 0.025f), Steel);
            m.Box(new Vector3(0f, h * 0.50f, -d * 0.5f - 0.026f),
                  new Vector3(w - 0.24f, h - 0.40f, 0.012f), SteelDark);
            m.Box(new Vector3(0f, h - 0.16f, -d * 0.5f - 0.055f),
                  new Vector3(w - 0.18f, 0.045f, 0.045f), Steel);
            for (int i = 0; i < 2; i++)
                m.Box(new Vector3((i == 0 ? -1f : 1f) * (w * 0.5f - 0.10f),
                                  h - 0.16f, -d * 0.5f - 0.035f),
                      new Vector3(0.035f, 0.045f, 0.05f), SteelDark);
        }

        /// <summary>Knobs along the front: the detail that says "this is a machine".</summary>
        private void Knobs(Modeler m, int count, float w, float d, float y)
        {
            for (int i = 0; i < count; i++)
            {
                float t = count == 1 ? 0.5f : (i + 0.5f) / count;
                float x = -w * 0.5f + 0.12f + (w - 0.24f) * t;
                m.Prism(8, 0.035f, 0.035f, 0.05f,
                        new Vector3(x, y, -d * 0.5f - 0.02f),
                        Quaternion.Euler(90f, 0f, 0f), Graphite);
            }
        }

        // =====================================================================
        // THE STATIONS. Each one is its own shape at a glance: a player who
        // has learned "the tall one with the arch is the stone oven" can read
        // the kitchen without labels, and at this camera nothing else would.

        /// <summary>The hob range: burner rings on a steel top, one per slot.</summary>
        private void Range(Modeler m, Transform root, int tier, Material unlit)
        {
            const float w = 0.90f, d = 0.62f, h = 0.86f;
            Carcass(m, w, d, h);
            _warm = true;

            // The back splash, which is also what stops the row of stations
            // reading as a line of identical boxes from the front.
            m.Box(new Vector3(0f, h + 0.24f, d * 0.5f - 0.03f),
                  new Vector3(w, 0.44f, 0.05f), Steel);

            // 1 -> 4 burners with the tier. Two rows of two once there are
            // more than two, which is how a four-burner range is actually
            // laid out.
            int burners = Mathf.Clamp(tier + 1, 1, 4);
            for (int i = 0; i < burners; i++)
            {
                float bx = burners <= 2
                    ? (burners == 1 ? 0f : (i == 0 ? -0.20f : 0.20f))
                    : (i % 2 == 0 ? -0.20f : 0.20f);
                float bz = burners <= 2 ? 0f : (i < 2 ? -0.14f : 0.14f);

                // The pan support: a dark disc under the ring.
                m.Prism(12, 0.115f, 0.115f, 0.015f, new Vector3(bx, h + 0.045f, bz),
                        Quaternion.identity, Graphite);
                Ring(root, new Vector3(bx, h + 0.055f, bz), 0.085f, unlit);
                PanSpot(root, new Vector3(bx, h + 0.06f, bz));
            }
            Knobs(m, burners, w, d, h * 0.72f);
        }

        /// <summary>The char grill: bars over an ember bed, with a drip tray.</summary>
        private void Grill(Modeler m, Transform root, int tier, Material unlit)
        {
            const float w = 0.90f, d = 0.62f, h = 0.84f;
            Carcass(m, w, d, h);
            _warm = true;

            // The bed is sunk into the top: a well, then the embers, then the
            // bars over them. The embers have to be BELOW the bars or the
            // glow sits on top of the food.
            m.Box(new Vector3(0f, h + 0.02f, 0f), new Vector3(w - 0.08f, 0.10f, d - 0.10f),
                  Graphite);

            int embers = Mathf.Clamp(tier + 2, 2, 5);
            for (int i = 0; i < embers; i++)
            {
                float t = (i + 0.5f) / embers;
                float x = -(w - 0.24f) * 0.5f + (w - 0.24f) * t;
                Lamp(root, new Vector3(x, h + 0.035f, 0f),
                     new Vector3((w - 0.26f) / embers, 0.02f, d - 0.22f), unlit);
            }

            // The bars. Nine of them, because a grill you can count the bars
            // on reads as a grill and a solid plate reads as a griddle.
            for (int i = 0; i < 9; i++)
            {
                float z = -(d - 0.20f) * 0.5f + (d - 0.20f) * (i / 8f);
                m.Box(new Vector3(0f, h + 0.085f, z),
                      new Vector3(w - 0.12f, 0.022f, 0.028f), SteelDark);
            }

            PanSpot(root, new Vector3(0f, h + 0.10f, 0f));

            // The drip tray at the front, pulled out a finger's width.
            m.Box(new Vector3(0f, h * 0.55f, -d * 0.5f - 0.03f),
                  new Vector3(w - 0.14f, 0.05f, 0.08f), Steel);
            Knobs(m, 2, w, d, h * 0.34f);
        }

        /// <summary>The oven: a glass door with a lamp behind it, and racks.</summary>
        private void Oven(Modeler m, Transform root, int tier, Material lit, Material unlit)
        {
            const float w = 0.86f, d = 0.64f, h = 1.52f;
            Width = w + 0.04f;
            _warm = true;

            m.Box(new Vector3(0f, h * 0.5f, 0f), new Vector3(w, h, d), SteelDark);
            m.Box(new Vector3(0f, h + 0.03f, 0f), new Vector3(w + 0.06f, 0.06f, d + 0.06f),
                  Steel);
            m.Box(new Vector3(0f, 0.06f, 0f), new Vector3(w - 0.08f, 0.12f, d - 0.06f),
                  Graphite);

            // THE CHAMBERS. One at tier 0, two after: the second door is the
            // slot you paid for, and a stacked double oven is what a kitchen
            // this size actually buys.
            int doors = Mathf.Clamp(tier + 1, 1, 2);
            for (int k = 0; k < doors; k++)
            {
                float y = doors == 1 ? h * 0.52f : (k == 0 ? h * 0.30f : h * 0.70f);
                float dh = doors == 1 ? 0.78f : 0.52f;

                // The mouth, recessed.
                m.Box(new Vector3(0f, y, -d * 0.5f + 0.06f),
                      new Vector3(w - 0.12f, dh, 0.08f), Graphite);
                // The lamp panel INSIDE, behind the glass.
                Lamp(root, new Vector3(0f, y, -d * 0.5f + 0.10f),
                     new Vector3(w - 0.22f, dh - 0.12f, 0.02f), unlit);

                // The door itself, on a hinge at its bottom edge. Only the
                // first one animates - two doors flapping in a 34 degree view
                // is the noise the project already refused for the room doors.
                GameObject leaf = new GameObject("OvenDoor" + k);
                leaf.transform.SetParent(root, false);
                leaf.transform.localPosition =
                    new Vector3(0f, y - dh * 0.5f, -d * 0.5f - 0.01f);
                if (k == 0) _hinge = leaf.transform;

                Modeler dm = new Modeler();
                dm.Box(new Vector3(0f, dh * 0.5f, 0f),
                       new Vector3(w - 0.06f, dh, 0.05f), Steel);
                dm.Box(new Vector3(0f, dh * 0.5f, -0.04f),
                       new Vector3(w - 0.26f, dh - 0.16f, 0.02f), Graphite);
                // The handle, full width, standing off the door.
                dm.Box(new Vector3(0f, dh - 0.07f, -0.08f),
                       new Vector3(w - 0.14f, 0.045f, 0.045f), Steel);
                dm.Build(leaf.transform, "Leaf", lit, _block);
            }

            // The control panel across the top.
            m.Box(new Vector3(0f, h - 0.10f, -d * 0.5f - 0.01f),
                  new Vector3(w - 0.06f, 0.16f, 0.03f), Graphite);
            Knobs(m, 3, w, d, h - 0.10f);
        }

        /// <summary>
        /// THE FRYER, and the user asked for it by name: "for fast food, is
        /// there a model of the place where the chips are fried? Is there a
        /// model of the chips? What about the oil?"
        ///
        /// All three are here. The oil is a still amber surface a few
        /// millimetres below the rim - still, because a moving surface would
        /// need either a shader or a per-frame mesh rebuild and this is a
        /// low-end Adreno; the chips are in the basket; and the heat under the
        /// wells is what lights up.
        /// </summary>
        private void Fryer(Modeler m, Transform root, int tier, Material unlit)
        {
            const float w = 0.90f, d = 0.62f, h = 0.86f;
            Carcass(m, w, d, h);
            _warm = true;

            // 1..4, NOT 1..3. The fryer was given the hob's four-rung ladder,
            // and a cap of three made tiers 2 and 3 identical geometry - the
            // player pays a million for the top rung and, in this file's own
            // measure, zero pixels of the frame change. That is the bug this
            // class exists to end, reintroduced by a clamp.
            int wells = Mathf.Clamp(tier + 1, 1, 4);
            float span = w - 0.22f;
            for (int i = 0; i < wells; i++)
            {
                float t = wells == 1 ? 0.5f : (i + 0.5f) / wells;
                float x = -span * 0.5f + span * t;
                float ww = span / wells - 0.03f;

                // The well: a dark box sunk into the top.
                m.Box(new Vector3(x, h + 0.02f, 0.02f),
                      new Vector3(ww, 0.10f, d - 0.20f), Graphite);
                // THE OIL. A thin slab a centimetre under the rim, so there is
                // a lip of steel above it - an oil surface flush with the top
                // reads as a painted panel.
                m.Box(new Vector3(x, h + 0.045f, 0.02f),
                      new Vector3(ww - 0.05f, 0.012f, d - 0.25f), Oil);
                // The heat element under the well.
                Lamp(root, new Vector3(x, h - 0.01f, 0.02f),
                     new Vector3(ww - 0.06f, 0.02f, d - 0.26f), unlit);

                // The basket, hanging on the rail with its handle towards the
                // cook, and the chips in it.
                m.Box(new Vector3(x, h + 0.075f, 0.02f),
                      new Vector3(ww - 0.09f, 0.05f, d - 0.30f), SteelDark);
                m.Box(new Vector3(x, h + 0.095f, 0.02f),
                      new Vector3(ww - 0.13f, 0.03f, d - 0.34f), Chip);
                m.Box(new Vector3(x, h + 0.13f, -d * 0.5f + 0.09f),
                      new Vector3(0.035f, 0.035f, 0.18f), Graphite);
            }

            // The drain rail along the back: where a basket rests to drip.
            m.Box(new Vector3(0f, h + 0.14f, d * 0.5f - 0.07f),
                  new Vector3(w - 0.12f, 0.03f, 0.03f), Steel);
            Knobs(m, wells, w, d, h * 0.68f);
        }

        /// <summary>The cold counter: doors, a handle rail and a temperature panel.</summary>
        private void ColdCounter(Modeler m, Transform root, int tier, Material unlit)
        {
            const float w = 0.90f, d = 0.64f, h = 0.88f;
            Carcass(m, w, d, h);

            int doors = Mathf.Clamp(tier + 1, 1, 2);
            for (int i = 0; i < doors; i++)
            {
                float t = doors == 1 ? 0.5f : (i + 0.5f) / doors;
                float x = -(w - 0.14f) * 0.5f + (w - 0.14f) * t;
                float dw = (w - 0.16f) / doors - 0.02f;
                m.Box(new Vector3(x, h * 0.48f, -d * 0.5f - 0.015f),
                      new Vector3(dw, h - 0.26f, 0.04f), Steel);
                m.Box(new Vector3(x, h * 0.72f, -d * 0.5f - 0.05f),
                      new Vector3(dw - 0.10f, 0.035f, 0.035f), SteelDark);

                // A TEMPERATURE PANEL PER DOOR - and it is not decoration.
                //
                // This was the ONE station with no controllable light at all,
                // so SetLoad looped zero times and the cold counter never
                // showed its load. It carries two fast-food dishes and three
                // Turkish ones, and docs/59's claim that "station i's object
                // shows station i's load" was simply false for it.
                //
                // Cold is the one station whose light should NOT be a flame,
                // so `_warm` stays off and it reads in the blue-amber-red
                // scale every other readout in this game uses.
                Lamp(root, new Vector3(x, h * 0.86f, -d * 0.5f - 0.055f),
                     new Vector3(0.16f, 0.05f, 0.02f), unlit);
            }
        }

        /// <summary>The drinks tower: nozzles over a cup rest, on a lit panel.</summary>
        private void DrinksTower(Modeler m, Transform root, int tier, Material unlit)
        {
            const float w = 0.52f, d = 0.42f, hh = 0.74f;
            Width = w + 0.04f;
            // It stands on its own cabinet - see Plinth.
            _stand = 0.86f; _standW = w + 0.10f; _standD = d + 0.06f;

            // It stands ON a counter, so there is no carcass and no feet: the
            // base is a plinth.
            m.Box(new Vector3(0f, 0.03f, 0f), new Vector3(w, 0.06f, d), SteelDark);
            m.Box(new Vector3(0f, hh * 0.5f + 0.06f, d * 0.5f - 0.06f),
                  new Vector3(w, hh, 0.12f), Steel);

            // The lit menu panel on the front of the tower.
            Lamp(root, new Vector3(0f, hh * 0.62f, d * 0.5f - 0.13f),
                 new Vector3(w - 0.10f, 0.26f, 0.02f), unlit);

            int taps = Mathf.Clamp(tier + 3, 3, 4);
            for (int i = 0; i < taps; i++)
            {
                float t = (i + 0.5f) / taps;
                float x = -(w - 0.12f) * 0.5f + (w - 0.12f) * t;
                m.Box(new Vector3(x, 0.34f, d * 0.5f - 0.13f),
                      new Vector3(0.05f, 0.14f, 0.06f), SteelDark);
                m.Prism(6, 0.02f, 0.02f, 0.05f, new Vector3(x, 0.27f, d * 0.5f - 0.14f),
                        Quaternion.identity, Graphite);
            }

            // The drip tray and a cup waiting on it.
            m.Box(new Vector3(0f, 0.08f, -0.04f), new Vector3(w - 0.08f, 0.03f, d - 0.16f),
                  SteelDark);
            m.Prism(8, 0.035f, 0.045f, 0.10f, new Vector3(0.09f, 0.10f, -0.04f),
                    Quaternion.identity, Plaster);
        }

        /// <summary>The dessert case: glass shelves with cakes on them, lit.</summary>
        private void DessertCase(Modeler m, Transform root, int tier, Material unlit)
        {
            const float w = 0.78f, d = 0.50f, hh = 0.62f;
            Width = w + 0.04f;
            // It stands on its own cabinet - see Plinth.
            _stand = 0.86f; _standW = w + 0.10f; _standD = d + 0.06f;

            m.Box(new Vector3(0f, 0.05f, 0f), new Vector3(w, 0.10f, d), SteelDark);
            // The frame: four uprights and a cap, so the glass is implied by
            // its edges. Transparent material can be drawn opaque in a build
            // (docs/37), so there is no glass here - only its frame.
            for (int i = 0; i < 4; i++)
            {
                float fx = (i % 2 == 0 ? -1f : 1f) * (w * 0.5f - 0.02f);
                float fz = (i < 2 ? -1f : 1f) * (d * 0.5f - 0.02f);
                m.Box(new Vector3(fx, hh * 0.5f + 0.08f, fz),
                      new Vector3(0.03f, hh, 0.03f), Steel);
            }
            m.Box(new Vector3(0f, hh + 0.10f, 0f), new Vector3(w, 0.05f, d), Steel);

            int shelves = Mathf.Clamp(tier + 1, 1, 2);
            for (int k = 0; k < shelves; k++)
            {
                float y = 0.22f + k * 0.26f;
                m.Box(new Vector3(0f, y, 0f), new Vector3(w - 0.08f, 0.02f, d - 0.08f),
                      Steel);
                // The cakes: three slabs of different sizes. A row of equal
                // boxes reads as tiling, which is the lesson the foam learned.
                float[] cw = { 0.16f, 0.13f, 0.18f };
                for (int i = 0; i < 3; i++)
                {
                    float x = -0.24f + i * 0.24f;
                    m.Prism(10, cw[i] * 0.5f, cw[i] * 0.5f, 0.07f,
                            new Vector3(x, y + 0.02f, 0f), Quaternion.identity,
                            i == 0 ? new Color(0.847f, 0.678f, 0.478f)
                                   : (i == 1 ? new Color(0.596f, 0.361f, 0.251f)
                                             : new Color(0.914f, 0.784f, 0.812f)));
                }
            }

            // The strip light under the cap.
            Lamp(root, new Vector3(0f, hh + 0.06f, 0f),
                 new Vector3(w - 0.14f, 0.02f, d - 0.14f), unlit);
        }

        /// <summary>The milkshake machine: spindles over a cup rest.</summary>
        private void Milkshake(Modeler m, Transform root, int tier, Material unlit)
        {
            const float w = 0.46f, d = 0.38f, hh = 0.58f;
            Width = w + 0.04f;
            // It stands on its own cabinet - see Plinth.
            _stand = 0.86f; _standW = w + 0.10f; _standD = d + 0.06f;

            m.Box(new Vector3(0f, 0.04f, 0f), new Vector3(w, 0.08f, d), SteelDark);
            m.Box(new Vector3(0f, hh * 0.5f + 0.08f, d * 0.5f - 0.05f),
                  new Vector3(w, hh, 0.10f), Steel);

            int spindles = Mathf.Clamp(tier + 3, 3, 4);
            for (int i = 0; i < spindles; i++)
            {
                float t = (i + 0.5f) / spindles;
                float x = -(w - 0.10f) * 0.5f + (w - 0.10f) * t;
                // The shaft and the cup under it.
                m.Box(new Vector3(x, 0.30f, 0.02f), new Vector3(0.022f, 0.20f, 0.022f),
                      SteelDark);
                m.Prism(10, 0.038f, 0.048f, 0.12f, new Vector3(x, 0.14f, 0.02f),
                        Quaternion.identity, Plaster);
            }
            Lamp(root, new Vector3(0f, hh * 0.78f, d * 0.5f - 0.11f),
                 new Vector3(w - 0.10f, 0.05f, 0.02f), unlit);
        }

        /// <summary>The waffle iron: two hinged round plates and a ready lamp.</summary>
        private void WaffleIron(Modeler m, Transform root, int tier, Material unlit)
        {
            const float w = 0.44f, d = 0.40f;
            Width = w + 0.04f;
            // It stands on its own cabinet - see Plinth.
            _stand = 0.86f; _standW = w + 0.10f; _standD = d + 0.06f;
            _warm = true;

            m.Box(new Vector3(0f, 0.05f, 0f), new Vector3(w, 0.10f, d), SteelDark);

            int irons = Mathf.Clamp(tier + 1, 1, 2);
            for (int k = 0; k < irons; k++)
            {
                float x = irons == 1 ? 0f : (k == 0 ? -0.11f : 0.11f);
                float r = irons == 1 ? 0.17f : 0.10f;
                // The bottom plate, the top plate tipped back, and the handle.
                m.Prism(14, r, r, 0.05f, new Vector3(x, 0.12f, 0f),
                        Quaternion.identity, Graphite);
                m.Prism(14, r, r, 0.045f, new Vector3(x, 0.20f, 0.06f),
                        Quaternion.Euler(-22f, 0f, 0f), Steel);
                m.Box(new Vector3(x, 0.21f, -r - 0.04f),
                      new Vector3(r * 0.9f, 0.035f, 0.05f), Graphite);
                Lamp(root, new Vector3(x, 0.145f, 0f),
                     new Vector3(r * 1.2f, 0.012f, r * 1.2f), unlit);
            }
        }

        /// <summary>
        /// The stone oven: a dome with an arched mouth and a fire inside.
        ///
        /// This is the object that says "Turkish kitchen" from across the
        /// room, so the silhouette does the work: a dome where everything else
        /// in the kitchen is a box.
        /// </summary>
        private void StoneOven(Modeler m, Transform root, int tier, Material unlit)
        {
            const float w = 1.10f, d = 0.92f;
            Width = w + 0.06f;
            _warm = true;

            // The plinth it stands on.
            m.Box(new Vector3(0f, 0.32f, 0f), new Vector3(w, 0.64f, d), Brick);
            m.Box(new Vector3(0f, 0.66f, 0f), new Vector3(w + 0.06f, 0.06f, d + 0.06f),
                  SteelDark);

            // The dome: three stacked prisms narrowing upwards. A single
            // tapered prism came out as a cone; three steps read as masonry.
            m.Prism(12, w * 0.48f, w * 0.44f, 0.26f, new Vector3(0f, 0.70f, 0f),
                    Quaternion.identity, Plaster);
            m.Prism(12, w * 0.44f, w * 0.33f, 0.20f, new Vector3(0f, 0.96f, 0f),
                    Quaternion.identity, Plaster);
            m.Prism(12, w * 0.33f, w * 0.14f, 0.16f, new Vector3(0f, 1.16f, 0f),
                    Quaternion.identity, Plaster);

            // The chimney.
            m.Box(new Vector3(0f, 1.42f, 0.12f), new Vector3(0.16f, 0.36f, 0.16f),
                  Brick);

            // The mouth: a brick arch with the fire behind it. It WIDENS with
            // the tier, which is the only honest way to show a second slot on
            // an oven that has no second door.
            float mouth = tier >= 1 ? 0.52f : 0.40f;
            m.Box(new Vector3(0f, 0.86f, -d * 0.5f + 0.04f),
                  new Vector3(mouth + 0.16f, 0.34f, 0.10f), Brick);
            m.Box(new Vector3(0f, 0.84f, -d * 0.5f + 0.10f),
                  new Vector3(mouth, 0.26f, 0.06f), Graphite);
            Lamp(root, new Vector3(0f, 0.82f, -d * 0.5f + 0.14f),
                 new Vector3(mouth - 0.05f, 0.18f, 0.02f), unlit);

            // THE PEEL LEANS ON THE FRONT, NOT THE SIDE.
            //
            // It used to lean against the flank, where it added 0.30 m to the
            // station's MEASURED width - and width is the scarce thing: the
            // layout reported "pide_firini (1.36 m wide) has nowhere to stand
            // in a 5.2 x 5.6 m kitchen", and a third of that was a wooden
            // paddle. Leaning it on the front costs depth, which the room has,
            // and it is where a peel actually stands anyway: to hand.
            m.BoxAt(new Vector3(w * 0.22f, 0.62f, -d * 0.5f - 0.10f),
                    new Vector3(0.22f, 0.03f, 1.0f),
                    Quaternion.Euler(72f, 0f, 0f), Wood);
        }

        /// <summary>
        /// The doner spit: a tapered cone of meat turning in front of a
        /// radiant panel.
        /// </summary>
        private void DonerSpit(Modeler m, Transform root, int tier, Material unlit)
        {
            const float w = 0.76f, d = 0.60f;
            Width = w + 0.04f;
            _warm = true;

            // The base and the back column the panel is mounted on.
            m.Box(new Vector3(0f, 0.06f, 0f), new Vector3(w, 0.12f, d), SteelDark);
            m.Box(new Vector3(0f, 0.86f, d * 0.5f - 0.06f),
                  new Vector3(w, 1.48f, 0.10f), Steel);
            m.Box(new Vector3(0f, 1.62f, 0.02f), new Vector3(w, 0.08f, d), Steel);

            int spits = Mathf.Clamp(tier + 1, 1, 2);
            for (int k = 0; k < spits; k++)
            {
                float x = spits == 1 ? -0.06f : (k == 0 ? -0.19f : 0.19f);
                float r = spits == 1 ? 0.19f : 0.13f;

                // The spindle, and the meat: wide at the top, narrow at the
                // bottom, which is the shape a doner actually wears down into.
                m.Box(new Vector3(x, 0.84f, 0f), new Vector3(0.03f, 1.44f, 0.03f),
                      SteelDark);
                m.Prism(14, r * 0.55f, r, 0.44f, new Vector3(x, 0.70f, 0f),
                        Quaternion.identity, MeatCrust);
                m.Prism(14, r, r * 0.92f, 0.46f, new Vector3(x, 1.14f, 0f),
                        Quaternion.identity, Meat);

                // The radiant panel behind it.
                Lamp(root, new Vector3(x, 1.00f, d * 0.5f - 0.13f),
                     new Vector3(r * 1.8f, 0.86f, 0.02f), unlit);
            }

            // The drip tray and the knife on its rest.
            m.Box(new Vector3(0f, 0.22f, -0.04f), new Vector3(w - 0.10f, 0.05f, d - 0.16f),
                  Steel);
            m.BoxAt(new Vector3(-w * 0.5f - 0.03f, 1.05f, -0.10f),
                    new Vector3(0.04f, 0.30f, 0.02f),
                    Quaternion.Euler(0f, 0f, 12f), Steel);
        }

        /// <summary>The pide oven: a wide low mouth and a peel.</summary>
        private void PideOven(Modeler m, Transform root, int tier, Material unlit)
        {
            const float w = 1.06f, d = 0.78f, h = 1.06f;
            Width = w + 0.06f;
            _warm = true;

            m.Box(new Vector3(0f, h * 0.5f, 0f), new Vector3(w, h, d), SteelDark);
            m.Box(new Vector3(0f, h + 0.04f, 0f), new Vector3(w + 0.06f, 0.08f, d + 0.06f),
                  Steel);
            m.Box(new Vector3(0f, 0.07f, 0f), new Vector3(w - 0.10f, 0.14f, d - 0.06f),
                  Graphite);

            // A WIDE, LOW MOUTH. That is the whole difference between this and
            // the oven: a pide is a metre long and goes in sideways.
            float mouthH = tier >= 1 ? 0.34f : 0.26f;
            m.Box(new Vector3(0f, h * 0.58f, -d * 0.5f + 0.05f),
                  new Vector3(w - 0.10f, mouthH + 0.10f, 0.08f), Brick);
            m.Box(new Vector3(0f, h * 0.58f, -d * 0.5f + 0.10f),
                  new Vector3(w - 0.22f, mouthH, 0.05f), Graphite);
            Lamp(root, new Vector3(0f, h * 0.56f, -d * 0.5f + 0.14f),
                 new Vector3(w - 0.28f, mouthH - 0.06f, 0.02f), unlit);

            // The tiled hood over the mouth, and the peel leaning beside it.
            m.Box(new Vector3(0f, h * 0.86f, -d * 0.5f + 0.02f),
                  new Vector3(w - 0.04f, 0.14f, 0.14f), Brick);
            // On the front, for the same reason as the stone oven's - see there.
            m.BoxAt(new Vector3(-w * 0.28f, 0.66f, -d * 0.5f - 0.10f),
                    new Vector3(0.24f, 0.03f, 1.0f),
                    Quaternion.Euler(72f, 0f, 0f), Wood);
        }
    }
}
