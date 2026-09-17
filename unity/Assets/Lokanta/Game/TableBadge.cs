using Lokanta.Core.Sim;
using UnityEngine;

namespace Lokanta.Game
{
    /// <summary>
    /// THE STATUS BADGE that stands over the table: a patience bar and
    /// its colour.
    ///
    /// Without this the service phase could not be watched. For eight
    /// minutes nothing moved on screen and no information flowed apart
    /// from the two numbers in the top strip; the player turned the speed
    /// up and stopped looking. And yet the core already knew every table's
    /// stage and how much patience it had left - it simply was not being
    /// drawn.
    ///
    /// The bar carries LENGTH, NOT COLOUR: it gets shorter as patience
    /// runs out. The colour says which stage it is in, and the two back
    /// each other up - a colour-blind player reads the length, and a
    /// player who cannot pick the length out on a small screen reads the
    /// colour.
    ///
    /// Two rectangles: a dark base and a slice filling over it.
    ///
    /// THE MATERIAL HAS TO BE UNLIT (URP/Unlit). This once said "because
    /// emission is off, the colours read independently of the light", and
    /// that was the exact opposite of the truth: if emission is off the
    /// colour depends entirely on the light. It was measured - a green
    /// badge sharing the same Lit material as the floor came out at 1.47:1
    /// contrast on screen, where its authored colour on the same floor
    /// gives 7.57:1. The threshold for a meaningful graphic is 3:1.
    /// </summary>
    public sealed class TableBadge : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        /// <summary>
        /// The height above the centre of the table.
        ///
        /// Measured: 1.75 m was tried and the badges ended up behind the
        /// heads of the seated figures. A figure is 1.45 m and sitting
        /// raises it by 0.35 m, and their heads are a third of the body on
        /// top of that - so the top of the head is around 2.1 m. 2.45 m
        /// stands above them.
        /// </summary>
        private const float Height = 2.45f;

        /// <summary>
        /// The width is 0.88 m: the same as the table's diameter. The badge
        /// has to look as if it BELONGS to the table, not like a separate
        /// object hanging in the air.
        private const float Width = 0.88f;
        // 0.11 -> 0.18 -> 0.36, AND THE SECOND NUMBER WAS NEVER RE-TAKEN.
        //
        // The note that used to stand here said 0.11 gave 9 dp and implied
        // 0.18 fixed it. Measured off the shipped store frame on 17 September:
        // the badge is 45 x 11 px at 2.5 px per dp, i.e. 18 x 4.4 dp. Under a
        // millimetre on a phone.
        //
        // The measurement was right when it was taken and the camera moved
        // afterwards - the street was added and the fit pulled back to hold
        // it, so every world-space size in the frame shrank and nothing
        // re-measured. Same shape as the shadow distance in ProjectSetup.
        //
        // 0.36 is double, and the constraint it has to respect is unchanged:
        // table spacing is 1.70 m, so two neighbouring badges are nowhere near
        // touching. It is still small - the real answer is docs/31 8.1, which
        // decided that in the OVERVIEW the badge belongs on the room and not
        // on the table, and was never built. This makes the existing channel
        // legible; it does not replace that decision.
        private const float Thickness = 0.36f;

        private Transform _fill;
        private Renderer _fillRenderer;
        private MaterialPropertyBlock _block;
        private Transform _camera;

        private int _shownBp = -1;
        private CustomerStage _shownStage = (CustomerStage)(-1);

        /// <summary>
        /// The selection mark: a slightly wider, brighter frame standing
        /// behind the badge.
        ///
        /// A selection HAS TO BE VISIBLE. A player who touches and sees no
        /// mark cannot tell whether the touch registered, and touches again -
        /// and the second touch drops the selection, that is, does exactly
        /// the opposite.
        /// </summary>
        private Transform _mark;
        private bool _shownSelected;

        // =====================================================================
        public static TableBadge Create(Transform parent, Material material)
        {
            GameObject root = new GameObject("Badge");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = new Vector3(0f, Height, 0f);

            TableBadge badge = root.AddComponent<TableBadge>();
            badge.Build(material);
            root.SetActive(false);
            return badge;
        }

        private void Build(Material material)
        {
            GameObject back = Quad("Base", material, new Color(0.08f, 0.09f, 0.11f));
            back.transform.localScale = new Vector3(Width, Thickness, Thickness);
            back.transform.localPosition = Vector3.zero;

            GameObject fill = Quad("Fill", material, Color.white);
            fill.transform.localScale = new Vector3(Width, Thickness, Thickness);
            // The slice fills FROM THE LEFT: because scale grows from the
            // centre, it is shifted through a parent of its own.
            GameObject pivot = new GameObject("Pivot");
            pivot.transform.SetParent(transform, false);
            pivot.transform.localPosition = new Vector3(-Width * 0.5f, 0f, -0.004f);
            fill.transform.SetParent(pivot.transform, false);
            fill.transform.localPosition = new Vector3(Width * 0.5f, 0f, 0f);

            // The selection frame is BIGGER than the badge and BEHIND it: it
            // does not cover the bar, it leaves an edge around it.
            //
            // +0.10 / +0.06 WAS NOT AN EDGE, IT WAS A ROUNDING ERROR. Against
            // a badge that measured 4.4 dp on screen, three hundredths of a
            // metre each side came to under a dp - one reviewer compared the
            // tour's "a table is selected" screenshot with the one before it
            // and could find no difference at all in the picture.
            //
            // It matters more now than it did: the target readout has come off
            // the bottom strip (it cost 75 dp of the one group that had none
            // to spare, and "> 3" is a number the player has to match up
            // against a table anyway), so THIS is the channel that says which
            // table is selected. A channel that carries the whole answer has
            // to be visible.
            GameObject mark = Quad("Mark", material, new Color(1f, 0.93f, 0.72f));
            mark.transform.localScale =
                new Vector3(Width + 0.22f, Thickness + 0.18f, Thickness);
            mark.transform.localPosition = new Vector3(0f, 0f, 0.006f);
            mark.SetActive(false);

            _fill = pivot.transform;
            _mark = mark.transform;
            _fillRenderer = fill.GetComponent<Renderer>();
            _block = new MaterialPropertyBlock();
        }

        private GameObject Quad(string name, Material material, Color color)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(transform, false);

            // NO COLLIDER BOX: the badge is not a touch target, and it must
            // not get in the way of the ray cast at the room.
            // IN EDITOR MODE Destroy IS DEFERRED and the collider box STAYS
            // in the scene. The screenshot tool runs Rebuild in editor mode;
            // the box left behind was cutting off the room's touch ray in
            // front of it.
            Collider col = go.GetComponent<Collider>();
            if (col != null)
            {
                if (Application.isPlaying) Destroy(col); else DestroyImmediate(col);
            }

            Renderer r = go.GetComponent<Renderer>();
            r.sharedMaterial = material;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;

            MaterialPropertyBlock b = new MaterialPropertyBlock();
            b.SetColor(BaseColorId, color);
            r.SetPropertyBlock(b);
            return go;
        }

        // =====================================================================
        /// <summary>Reflects the table's state. On an empty table the badge is hidden.</summary>
        public void Show(CustomerStage stage, int patienceBp, bool selected = false)
        {
            // LEAVING ANGRY IS THE ONE MOMENT THIS BADGE EXISTS FOR, and it
            // was the one moment the badge switched itself off.
            //
            // LeftAngry was in this hide list, so the instant the thing the
            // player was supposed to prevent actually happened, the only
            // indicator of it vanished. The game plays a sound for it
            // (Sfx.Upset) and shows a notice - and the table itself, the place
            // the player is looking, went quiet.
            //
            // It stays, in a solid dark red at full width: not a meter any
            // more but a mark. The party is gone a moment later and the stage
            // falls to None, so it clears itself.
            bool visible = stage != CustomerStage.None
                           && stage != CustomerStage.Done;

            if (gameObject.activeSelf != visible) gameObject.SetActive(visible);
            if (!visible) return;

            if (selected != _shownSelected)
            {
                _shownSelected = selected;
                if (_mark != null) _mark.gameObject.SetActive(selected);
            }

            // Written only ON CHANGE: writing a property block every frame
            // means fourteen needless draw groups per frame at fourteen
            // tables.
            int step = patienceBp / 200;              // 2% steps
            if (step == _shownBp && stage == _shownStage) return;
            _shownBp = step;
            _shownStage = stage;

            // The angry mark is FULL WIDTH. Patience is zero by then, so the
            // meter would draw nothing at all - which is how it managed to be
            // invisible even after it stopped being hidden.
            float k = stage == CustomerStage.LeftAngry
                ? 1f : Mathf.Clamp01(patienceBp / 10000f);
            _fill.localScale = new Vector3(k, 1f, 1f);

            _fillRenderer.GetPropertyBlock(_block);
            _block.SetColor(BaseColorId, ColorFor(stage, k));
            _fillRenderer.SetPropertyBlock(_block);
        }

        /// <summary>
        /// The stage colour, sliding towards red as patience runs out.
        ///
        /// A table that is EATING is a calm green: there is nothing to do
        /// there and the player's eye should go to the tables that are
        /// waiting.
        /// </summary>
        private static Color ColorFor(CustomerStage stage, float patience)
        {
            // Darker and more saturated than the "running out" red, so that
            // "they are about to go" and "they have gone" do not read as the
            // same state at a glance.
            if (stage == CustomerStage.LeftAngry) return new Color(0.68f, 0.13f, 0.12f);
            if (stage == CustomerStage.Eating) return new Color(0.42f, 0.68f, 0.44f);
            if (stage == CustomerStage.WaitingToPay) return new Color(0.85f, 0.72f, 0.35f);

            // A waiting table: red once patience falls below 30%.
            if (patience < 0.3f) return new Color(0.93f, 0.36f, 0.33f);
            if (patience < 0.6f) return new Color(0.93f, 0.70f, 0.33f);
            return new Color(0.55f, 0.72f, 0.90f);
        }

        // =====================================================================
        /// <summary>
        /// The badge turns TO THE CAMERA. Even though the camera sits at a
        /// fixed angle it moves between the two steps; a fixed rotation would
        /// be seen edge-on when zoomed in.
        /// </summary>
        private void LateUpdate()
        {
            if (_camera == null)
            {
                Camera cam = Camera.main;
                if (cam == null) return;
                _camera = cam.transform;
            }
            transform.rotation = _camera.rotation;
        }
    }
}
