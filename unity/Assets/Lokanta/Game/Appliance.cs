using UnityEngine;

namespace Lokanta.Game
{
    /// <summary>
    /// A WORKING STOVE. The door opens, the inside glows, the burners
    /// catch light.
    ///
    /// Why it is needed: the kitchen covers a third of the screen and
    /// nothing was happening inside it. The simulation knows at every
    /// moment how many plates are cooking at which station; that
    /// information was drawn nowhere. In a management game "the kitchen
    /// is jammed" is the decision taken most often, and the only place
    /// the player can see it is the kitchen itself.
    ///
    /// NO PARTICLES. docs/19 targets a low-end Adreno and the particle
    /// system is a documented fill-rate bottleneck on that class of
    /// device. The flame and the lamp are A BOX EACH: emissive colour,
    /// casting no shadow, with no collider. Six boxes for three stoves -
    /// unmeasurable as a draw call.
    ///
    /// THE GLASS IS REALLY TRANSPARENT. A thin transparent panel goes in
    /// front of the oven door and a lamp panel BEHIND it: when the lamp
    /// lights, the light shows through from behind the glass; when it
    /// goes out, dark glass is left. Transparency breaks SRP batching,
    /// but there are at most three of these in the scene.
    /// </summary>
    public sealed class Appliance : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        /// <summary>The angle the door opens to. A real oven door drops downwards.</summary>
        private const float OpenAngle = -72f;

        /// <summary>How long opening and closing takes. The door is heavy; it must not fly open.</summary>
        private const float DoorSeconds = 0.45f;

        private Transform _hinge;      // the door's hinge
        private Renderer _lamp;        // lamp panel inside the oven
        private Renderer[] _flames;    // the stove's hobs
        private MaterialPropertyBlock _block;

        private bool _on;
        private float _t;              // 0 closed, 1 open

        private static readonly Color LampOff = new Color(0.05f, 0.05f, 0.06f);
        private static readonly Color LampOn = new Color(1.00f, 0.62f, 0.20f);
        private static readonly Color FlameOff = new Color(0.07f, 0.07f, 0.09f);
        private static readonly Color FlameOn = new Color(0.35f, 0.62f, 1.00f);

        // =====================================================================
        /// <summary>
        /// Sets the stove up: a hinge on the door, a lamp inside it, a flame on each hob.
        /// </summary>
        /// <param name="mat">
        /// Must be UNLIT (URP/Unlit). The lamp and the flame carry MEANING;
        /// in a lit material the lamp inside the oven is drawn as a dark
        /// panel, because no light reaches in there.
        /// </param>
        public static Appliance Attach(GameObject stove, Material mat, Material glass)
        {
            if (stove == null) return null;
            Appliance a = stove.AddComponent<Appliance>();
            a.Build(mat, glass);
            return a;
        }

        private void Build(Material mat, Material glass)
        {
            _block = new MaterialPropertyBlock();

            Transform door = FindChild(transform, "door");
            if (door != null)
            {
                // THE HINGE IS ON THE DOOR'S BOTTOM EDGE.
                //
                // The model's own pivot is in the MIDDLE of the door; turning it
                // from there buries the door inside the oven. An intermediate
                // object is placed at the bottom edge and the door is parented to
                // it - a real oven door turns from there too.
                Bounds b = Bounds(door);
                float bottom = b.min.y;

                GameObject pivot = new GameObject("Hinge");
                pivot.transform.SetParent(door.parent, false);
                pivot.transform.position = new Vector3(
                    door.position.x, bottom, door.position.z);
                pivot.transform.rotation = door.rotation;

                door.SetParent(pivot.transform, true);
                _hinge = pivot.transform;

                // GLASS ON THE DOOR, LAMP ON THE BODY.
                //
                // Both used to be parented to the hinge, so when the door opened
                // the lamp turned with it - in a real oven the lamp sits inside
                // the BODY and only the glass comes away with the door. Closed:
                // the light shows through from behind the glass. Open: it shows
                // directly.
                Vector3 modelSize = b.size;
                float width = Mathf.Max(0.12f, modelSize.x * 0.62f);
                float height = Mathf.Max(0.08f, modelSize.y * 0.55f);

                // THE CENTRE, NOT THE PIVOT.
                //
                // The hinge is placed on the door's BOTTOM EDGE, and the door
                // model's own pivot is not centred (its local x is 0.268).
                // Placing the panels at (0, ...) relative to the hinge pushed both
                // of them SIDEWAYS by that offset - the lamp burned between the
                // two hobs instead of inside the oven.
                //
                // The door's VISUAL centre is converted into hinge space; where
                // the model's pivot sits no longer matters.
                Vector3 center = pivot.transform.InverseTransformPoint(b.center);

                // THE LAMP SITS BETWEEN THE GLASS AND THE DOOR'S SURFACE.
                //
                // Two attempts:
                //   1. Parented to the hinge, in front of the door -> the lamp
                //      turned with the door as it opened; in a real oven the lamp
                //      stays on the body.
                //   2. Parented to the body, inside the oven -> RIGHT but NOT
                //      VISIBLE: the asset pack's door mesh is fully opaque, so
                //      nothing inside shows through the glass.
                //
                // Realism loses to legibility here: the lamp is ON THE SURFACE of
                // the door, right behind the glass. While the door is shut a lit
                // panel shows through the glass; when the door opens the panel
                // faces upwards and is visible again. The offsets have to be in
                // hinge-local space too, by dividing world metres by the scale.
                float pz = Mathf.Max(0.0001f, pivot.transform.lossyScale.z);
                _lamp = Panel(pivot.transform, "Lamp", mat,
                              center + new Vector3(0f, 0f, -0.030f / pz),
                              new Vector3(width, height, 0.01f), LampOff);

                if (glass != null)
                    Panel(pivot.transform, "Glass", glass,
                          center + new Vector3(0f, 0f, -0.042f / pz),
                          new Vector3(width + 0.02f, height + 0.02f, 0.012f),
                          new Color(0.14f, 0.16f, 0.18f, 0.45f));
            }

            // THE HOBS: a flame on ALL FOUR of them, ON TOP OF THE HOB.
            //
            // The positions were RENDERED FROM ABOVE AND MEASURED
            // (Editor/FigureShot -> render/olcek_ocak_ustten.png):
            //
            //   x: the hobs are SYMMETRIC about the stove's centre, +-20.5%
            //   z: NOT SYMMETRIC - back row +20.4%, front row -7.5%
            //
            // The first time round both were +-21% and x held, but the front
            // row fell about 9 cm in front of the hob: the flame burned in the
            // gap in the grill rather than on the hob. This is the numerical
            // form of the user saying "the flame looks a bit off".
            //
            // Why it is not symmetric: the model has a row of knobs along its
            // front edge and the hob grid has shifted backwards to leave them
            // room.
            Bounds whole = Bounds(transform);
            float top = whole.max.y - transform.position.y;
            const float BackZ = 0.204f;
            const float FrontZ = -0.075f;
            _flames = new Renderer[4];
            for (int i = 0; i < 4; i++)
            {
                float dx = ((i & 1) == 0 ? -1f : 1f) * whole.size.x * 0.205f;
                float dz = whole.size.z * ((i & 2) == 0 ? FrontZ : BackZ);
                _flames[i] = Panel(transform, "Flame" + i, mat,
                                   new Vector3(dx, top + 0.012f, dz),
                                   new Vector3(0.085f, 0.006f, 0.085f), FlameOff);
            }
        }

        // =====================================================================
        /// <summary>Is the station working? Not every frame - ON CHANGE.</summary>
        public void SetWorking(bool on)
        {
            SetLoad(on ? 1 : 0, 4);
        }

        /// <summary>
        /// HOW HARD the station is working, not merely whether it is.
        ///
        /// This class's own comment says the case: "in a management game 'the
        /// kitchen is jammed' is the decision taken most often, and the only
        /// place the player can see it is the kitchen itself". The core has
        /// always known - `StationLoad` returns a BACKLOG COUNT - and the view
        /// threw the number away with `if (load <= 0) continue;` and lit a
        /// boolean lamp. A backlog of one and a backlog of nine were the same
        /// picture, while "rush the kitchen" is one of the three verbs the
        /// whole service phase is built on.
        ///
        /// Two channels, both free - the hobs already exist and are already
        /// coloured on change:
        ///
        ///   HOW MANY hobs are lit   = how much work is on the station
        ///   WHAT COLOUR they are    = whether it is coping
        ///
        ///     load <= slots        blue    working
        ///     load  > slots        amber   over capacity, a queue is forming
        ///     load >= 2 x slots    red     jammed
        ///
        /// Colour alone would not do it: the stove is a handful of pixels at
        /// the default camera and the kitchen sits in the frame's weakest
        /// corner. The count of lit hobs is the channel that survives at that
        /// size, and the colour is what the player reads once they look.
        /// </summary>
        public void SetLoad(int load, int slots)
        {
            if (slots < 1) slots = 1;
            bool on = load > 0;

            // Four hobs stand for the station's capacity, so a stove at its
            // slot count is FULL rather than at some arbitrary fraction: the
            // picture means "this station is at its limit", which is the thing
            // being decided about.
            int lit = load <= 0 ? 0
                : Mathf.Clamp(Mathf.CeilToInt(4f * load / slots), 1, 4);

            Color flame = load <= slots ? FlameOn
                : (load >= slots * 2 ? FlameJammed : FlameBusy);

            if (on == _on && lit == _lit && flame == _flame) return;
            _on = on;
            _lit = lit;
            _flame = flame;

            Paint(_lamp, on ? LampOn : LampOff);
            for (int i = 0; _flames != null && i < _flames.Length; i++)
                Paint(_flames[i], i < lit ? flame : FlameOff);
        }

        private int _lit = -1;
        private Color _flame = Color.clear;

        /// <summary>Over capacity: a queue is forming at this station.</summary>
        private static readonly Color FlameBusy = new Color(1.00f, 0.60f, 0.16f);

        /// <summary>Jammed: twice the slots or worse.</summary>
        private static readonly Color FlameJammed = new Color(1.00f, 0.28f, 0.18f);

        private void Update()
        {
            if (_hinge == null) return;

            float target = _on ? 1f : 0f;
            if (Mathf.Approximately(_t, target)) return;

            _t = Mathf.MoveTowards(_t, target, Time.deltaTime / DoorSeconds);
            // Easing out: the door is heavy and settles towards the end.
            float k = 1f - (1f - _t) * (1f - _t);
            _hinge.localRotation = Quaternion.Euler(OpenAngle * k, 0f, 0f);
        }

        // =====================================================================
        /// <summary>
        /// A panel. THE SIZE IS IN WORLD METRES, not local scale.
        ///
        /// The hinge sits under the model's own scale (0.204); writing world
        /// metres straight into localScale shrank the panels fivefold - the
        /// glass showed up as a small square in the middle of the door. The
        /// parent's scale is compensated for here, so the caller can write
        /// metres.
        /// </summary>
        private Renderer Panel(Transform parent, string name, Material mat,
                               Vector3 localPos, Vector3 size, Color c)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;

            Vector3 ps = parent != null ? parent.lossyScale : Vector3.one;
            go.transform.localScale = new Vector3(
                size.x / Mathf.Max(0.0001f, ps.x),
                size.y / Mathf.Max(0.0001f, ps.y),
                size.z / Mathf.Max(0.0001f, ps.z));
            Collider col = go.GetComponent<Collider>();
            if (col != null)
            {
                if (Application.isPlaying) Destroy(col); else DestroyImmediate(col);
            }

            Renderer r = go.GetComponent<Renderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            Paint(r, c);
            return r;
        }

        private void Paint(Renderer r, Color c)
        {
            if (r == null) return;
            if (_block == null) _block = new MaterialPropertyBlock();
            r.GetPropertyBlock(_block);
            _block.SetColor(BaseColorId, c);
            r.SetPropertyBlock(_block);
        }

        private static Transform FindChild(Transform t, string name)
        {
            foreach (Transform c in t.GetComponentsInChildren<Transform>(true))
                if (c.name == name) return c;
            return null;
        }

        private static Bounds Bounds(Transform t)
        {
            Renderer[] rs = t.GetComponentsInChildren<Renderer>(true);
            if (rs.Length == 0) return new Bounds(t.position, Vector3.one * 0.3f);
            Bounds b = rs[0].bounds;
            for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
            return b;
        }
    }
}
