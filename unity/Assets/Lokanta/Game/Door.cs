using UnityEngine;

namespace Lokanta.Game
{
    /// <summary>
    /// A DOOR THAT OPENS AND CLOSES.
    ///
    /// Why it exists: the walls separated the rooms but the figures walked
    /// straight through them - what a wall is for only becomes clear once
    /// it has a door in it. A door is also the hall's most repeated piece
    /// of movement: the waiter crosses the same threshold dozens of times
    /// a day.
    ///
    /// THE HINGE IS ON THE EDGE OF THE LEAF. Turning it from the model's
    /// own pivot would bury the leaf in the wall (exactly what happened
    /// with the oven door, see Appliance): a separate hinge object is
    /// placed and the leaf is parented to it. A real door turns from there
    /// too.
    ///
    /// NO COLLIDER. The touch target is the room floor (the docs/31
    /// measurement); a ray that hit the door would break room selection.
    /// The figures do not collide with the door either - the path goes
    /// through the corridor, the door is only a picture.
    /// </summary>
    public sealed class Door : MonoBehaviour
    {
        /// <summary>The angle it opens to. It opens inwards.</summary>
        private const float OpenAngle = 88f;

        /// <summary>How long opening and closing takes (s). The door is light, it opens fast.</summary>
        private const float OpenSeconds = 0.28f;

        /// <summary>
        /// The distance at which the door "sees" someone (m).
        ///
        /// 1.10: a figure walks at 1.15 m/s and the door opens in 0.28 s -
        /// so the leaf is fully open about 0.8 m before the figure reaches
        /// the door. At a shorter distance the figure would walk through a
        /// leaf that was still closing.
        /// </summary>
        public const float Sense = 1.10f;

        private Transform _hinge;
        private float _t;          // 0 closed, 1 open
        private bool _open;

        /// <summary>The door's world (local) position: the distance is measured from here.</summary>
        public Vector3 Spot { get; private set; }

        /// <summary>Is it open right now? So the tour can ask.</summary>
        public bool IsOpen { get { return _t > 0.5f; } }

        /// <summary>How far open, 0-1. So the tour can ask.</summary>
        public float Openness { get { return _t; } }

        // =====================================================================
        /// <summary>
        /// Builds the door. spot: where the door is; yaw: the direction of
        /// the wall; width: the width of the gap; height: the height of the
        /// leaf.
        /// </summary>
        public static Door Create(Transform parent, Vector3 spot, float yaw,
                                  float width, float height, Material mat)
        {
            GameObject root = new GameObject("Door");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = spot;
            root.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

            // THE HINGE IS ON ONE EDGE of the gap, not in the middle.
            GameObject hinge = new GameObject("Hinge");
            hinge.transform.SetParent(root.transform, false);
            hinge.transform.localPosition = new Vector3(0f, 0f, -width * 0.5f);

            GameObject leaf = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leaf.name = "Leaf";
            leaf.transform.SetParent(hinge.transform, false);
            // The leaf is IN FRONT OF the hinge: as it turns its tip swings outwards.
            leaf.transform.localPosition = new Vector3(0f, height * 0.5f, width * 0.5f);
            leaf.transform.localScale = new Vector3(0.05f, height, width * 0.94f);

            Collider col = leaf.GetComponent<Collider>();
            if (col != null)
            {
                if (Application.isPlaying) Destroy(col); else DestroyImmediate(col);
            }

            Renderer ren = leaf.GetComponent<Renderer>();
            ren.sharedMaterial = mat;
            // A transparent leaf casts an OPAQUE shadow: the URP shadow pass
            // does not read alpha (switched off on the walls for the same
            // reason).
            ren.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ren.receiveShadows = false;

            Door d = root.AddComponent<Door>();
            d._hinge = hinge.transform;
            d.Spot = spot;
            return d;
        }

        /// <summary>Has someone come close? Not every frame - ON CHANGE.</summary>
        public void SetOpen(bool on)
        {
            _open = on;
        }

        private void Update()
        {
            if (_hinge == null) return;

            float target = _open ? 1f : 0f;
            if (Mathf.Approximately(_t, target)) return;

            _t = Mathf.MoveTowards(_t, target, Time.deltaTime / OpenSeconds);
            // Easing out: the leaf settles towards the end.
            float k = 1f - (1f - _t) * (1f - _t);
            _hinge.localRotation = Quaternion.Euler(0f, -OpenAngle * k, 0f);
        }
    }
}
