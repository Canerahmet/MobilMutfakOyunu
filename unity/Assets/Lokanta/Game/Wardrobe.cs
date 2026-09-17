using UnityEngine;

namespace Lokanta.Game
{
    /// <summary>
    /// THE STAFF'S CLOTHES: three roles, three silhouettes.
    ///
    /// The user's request was plain: "the cook should have a chef's hat
    /// and chef's whites, the dishwasher should wear an apron and gloves,
    /// the waiter should wear a suit".
    ///
    /// Here the staff and the guests were THE SAME figures (BuildGameScene
    /// picks the staff out of male a/b/c), so in the hall the player could
    /// only tell who the waiter was FROM THEIR MOVEMENT. The rule of
    /// docs/25: identity is built out of the ENVIRONMENT, the LIGHT, the
    /// SILHOUETTE and the CLOTHES.
    ///
    /// IT IS ATTACHED TO A BONE, NOT TO THE ROOT. The body and the head
    /// swing as the figure walks; a hat attached to the root hangs in the
    /// air, and that makes the animation ITSELF look broken. The pack's
    /// skeleton has "head", "torso", "arm-left" and "arm-right" bones
    /// (verified in the FBX).
    ///
    /// THE MEASUREMENTS ARE READ FROM THE FIGURE, NOT FROM FIXED NUMBERS.
    /// The first version placed the hat with fixed numbers relative to the
    /// bone and nothing appeared on screen at all: the hat stayed INSIDE
    /// the head (the bone chain's own scale is 1.19 and the head bone is
    /// as long as the body). Every part is now derived from the figure's
    /// height and from the world box of its skin.
    /// </summary>
    public static class Wardrobe
    {
        /// <summary>The staff member's role. The clothes are built from it.</summary>
        /// <summary>
        /// The staff member's role. The clothes are built from it.
        ///
        /// CLEANER IS NOT A DECORATION. docs/51 removed the waiter from fast
        /// food entirely and put a cleaner in their place - it is the sharpest
        /// mechanical difference between the two cuisines, worth +43% volume
        /// and -22% on the ticket. The interface renames the role, the
        /// simulation runs a different hall loop for it, and the FIGURE went
        /// on wearing a dinner jacket, a white shirt and a burgundy bow tie,
        /// because Dress had no arm for it and fell through to `default`.
        ///
        /// A self-service burger bar with a waiter standing in it contradicts
        /// the one thing that picture is supposed to say.
        /// </summary>
        public enum Role { Cook, Dishwasher, Waiter, Cleaner }

        /// <summary>
        /// How many dressings were ATTEMPTED and how many were COMPLETED.
        ///
        /// Dress has four separate early exits (no bone, no renderer, an
        /// absurd measurement, no arm). All of them print a warning now, but
        /// a warning can get lost in the log; these two numbers feed the
        /// tour's "staff dressed N/M" line, so if the clothes disappear the
        /// automatic measurement goes RED.
        /// </summary>
        public static int Attempted, Dressed;

        /// <summary>Resets the measurement. Called while the crew is rebuilt.</summary>
        public static void ResetCounters() { Attempted = 0; Dressed = 0; }


        private const string HatName = "Hat";
        private const string BodyName = "Outfit";
        private const string GloveName = "Glove";

        /// <summary>Chef white: the hat and the jacket.</summary>
        private static readonly Color Chef = new Color(0.949f, 0.945f, 0.929f);

        /// <summary>The buttons and the collar line of the chef's jacket.</summary>
        private static readonly Color ChefTrim = new Color(0.427f, 0.451f, 0.494f);

        /// <summary>The dishwasher's leather apron: dark and matt.</summary>
        private static readonly Color Rubber = new Color(0.278f, 0.318f, 0.365f);

        /// <summary>A rubber glove. Yellow: the one colour that reads from a distance.</summary>
        private static readonly Color Glove = new Color(0.937f, 0.761f, 0.216f);

        /// <summary>The waiter's suit, shirt and bow tie.</summary>
        private static readonly Color Suit = new Color(0.137f, 0.153f, 0.192f);
        private static readonly Color Shirt = new Color(0.929f, 0.933f, 0.945f);
        private static readonly Color Bow = new Color(0.545f, 0.125f, 0.133f);

        /// <summary>The apron's belt.</summary>
        private static readonly Color Belt = new Color(0.106f, 0.122f, 0.149f);

        /// <summary>
        /// Dresses the figure. If the same figure is passed twice it clears
        /// the old clothes - when the crew changes, the same GameObject can
        /// move to another role (a cook leaves and a waiter is hired, a
        /// waiter moves to the sink) and it must not end up wearing two
        /// outfits at once.
        /// </summary>
        public static void Dress(GameObject figure, Role role, Material mat,
                                 MaterialPropertyBlock block)
        {
            Attempted++;
            if (figure == null || mat == null) return;

            Transform head = Find(figure.transform, "head");
            Transform torso = Find(figure.transform, "torso");
            if (head == null || torso == null)
            {
                // NO SILENT EARLY EXIT.
                //
                // This file's own comment says the bug happened ONCE ("it
                // SILENTLY did nothing"), but no guard had been added - only the
                // source of the measurement had been fixed. The clothes are the
                // ONLY channel that tells the three roles apart: when they
                // disappear the mechanic becomes invisible, and not a single
                // warning was printed.
                Debug.LogWarning("Wardrobe: no head/torso in the skeleton - "
                                 + figure.name + " could not be dressed");
                return;
            }

            Transform leftArm = Find(figure.transform, "arm-left");
            Transform rightArm = Find(figure.transform, "arm-right");

            Strip(head, HatName);
            Strip(torso, BodyName);
            Strip(leftArm, GloveName);
            Strip(rightArm, GloveName);

            // THE SKIN IS NOT A SINGLE SkinnedMeshRenderer.
            //
            // The first version assumed it was and SILENTLY did nothing: the
            // pack's character is built from two separate parts, "body-mesh"
            // and "head-mesh". The boxes of all the renderers are combined -
            // except for the clothes themselves (so that on a second call it
            // does not measure its own box).
            Bounds b = new Bounds();
            bool first = true;
            foreach (Renderer rr in figure.GetComponentsInChildren<Renderer>())
            {
                Transform p = rr.transform.parent;
                if (p != null && (p.name == HatName || p.name == BodyName
                                  || p.name == GloveName)) continue;
                if (first) { b = rr.bounds; first = false; }
                else b.Encapsulate(rr.bounds);
            }
            if (first)
            {
                Debug.LogWarning("Wardrobe: the figure has no renderer at all - "
                                 + figure.name + " could not be dressed");
                return;
            }

            float height = b.size.y;
            if (height < 0.05f)
            {
                Debug.LogWarning("Wardrobe: could not measure the figure (height "
                                 + height.ToString("0.000") + " m) - "
                                 + figure.name + " could not be dressed");
                return;
            }

            // THE CLOTHES WRAP THE BODY, THEY DO NOT STICK TO ITS FRONT.
            //
            // The first version put a slab on the FRONT face only, and the
            // game's camera sees the staff's BACK most of the time: both the
            // cook's jacket and the dishwasher's apron were invisible half
            // the time.
            //
            // The shell's depth comes from the figure's MEASURED body depth;
            // the details (the buttons, the bow tie) go on the front face
            // only. THE SCALE FROM MEASUREMENT: 0.40 -> 0.80.
            //
            // The first value was judged by eye and the clothes stayed INSIDE
            // THE BODY: it was measured (the diagnostic line), and the
            // figure's box is 1.14 x 1.00 x 0.51 - the 1.14 of width is the
            // ARMS, while the body is ~0.5 m. A jacket 0.70 wide in local
            // space came to 0.28 m at a scale of 0.40, that is, half the
            // body.
            //
            // Now local 1.0 = 80% of the height: the jacket is 0.56 m wide and
            // wraps the body. The thickness comes from the MEASURED depth too.
            float scale = height * 0.80f;
            float depth = b.size.z;
            float thickness = Mathf.Clamp(depth * 0.80f
                                         / Mathf.Max(0.0001f, scale), 0.30f, 0.75f);

            switch (role)
            {
                case Role.Cook:
                    Hat(head, height, b.max.y, mat, block);
                    ChefJacket(figure.transform, torso, scale, thickness, mat, block);
                    break;

                case Role.Dishwasher:
                    RubberApron(figure.transform, torso, scale, thickness, mat, block);
                    Gloves(leftArm, rightArm, height, mat, block);
                    break;

                // THE CLEANER: the dishwasher's apron and gloves, and a cap.
                //
                // Built out of the parts that already exist rather than new
                // geometry - the apron says "work clothes", the yellow gloves
                // are the one colour that reads from a distance (the reason
                // they were chosen for the dishwasher), and the cap separates
                // the two from each other at a glance. It has to read at ~32
                // dp, so the silhouette does the work, not the detail.
                case Role.Cleaner:
                    RubberApron(figure.transform, torso, scale, thickness, mat, block);
                    Gloves(leftArm, rightArm, height, mat, block);
                    Cap(head, height, b.max.y, mat, block);
                    break;

                default:
                    SuitJacket(figure.transform, torso, scale, thickness, mat, block);
                    break;
            }

            Dressed++;
        }

        // =====================================================================
        /// <summary>
        /// The hat: a band with a puffed body on top. A UNIT mesh is built
        /// and pulled to world scale afterwards.
        /// </summary>
        private static void Hat(Transform head, float height, float top,
                                Material mat, MaterialPropertyBlock block)
        {
            Modeler m = new Modeler();
            m.Prism(10, 0.40f, 0.42f, 0.22f, Vector3.zero,
                    Quaternion.identity, Chef);                       // band
            m.Prism(10, 0.38f, 0.52f, 0.52f, new Vector3(0f, 0.20f, 0f),
                    Quaternion.identity, Chef);                       // the puffed body

            GameObject go = m.Build(head, HatName, mat, block);

            // The hat is as wide as 31% of the height: a real chef's hat is
            // roughly the diameter of the head too, and at this scale it gives
            // a silhouette.
            Scale(go.transform, head, height * 0.31f);
            go.transform.rotation = head.rotation;
            go.transform.position = new Vector3(head.position.x,
                                                top - height * 0.045f,
                                                head.position.z);
        }

        /// <summary>
        /// THE CLEANER'S CAP: a flat band and a short peak.
        ///
        /// It is built like Hat and sized well under it - a chef's toque is
        /// 31% of the figure's height on purpose, because it is the one piece
        /// of state that has to read at overview size. The cleaner is not
        /// competing for that; the cap only has to separate them from the
        /// dishwasher, who wears the same apron and gloves.
        /// </summary>
        private static void Cap(Transform head, float height, float top,
                                Material mat, MaterialPropertyBlock block)
        {
            Modeler m = new Modeler();
            m.Prism(10, 0.42f, 0.44f, 0.20f, Vector3.zero,
                    Quaternion.identity, Rubber);                     // the band
            m.Box(new Vector3(0f, -0.02f, 0.30f),
                  new Vector3(0.42f, 0.06f, 0.28f), Rubber);          // the peak

            GameObject go = m.Build(head, HatName, mat, block);
            Scale(go.transform, head, height * 0.20f);
            go.transform.rotation = head.rotation;
            go.transform.position = new Vector3(head.position.x,
                                                top - height * 0.030f,
                                                head.position.z);
        }

        /// <summary>
        /// THE CHEF'S JACKET: a double-breasted white jacket and a white
        /// apron.
        ///
        /// Before this it was only a white apron, and the cook and the
        /// waiter had the same silhouette. The jacket covers the body's
        /// FRONT and its SHOULDERS; the double row of buttons is the one
        /// detail that makes a chef's jacket a chef's jacket.
        /// </summary>
        private static void ChefJacket(Transform root, Transform torso, float scale,
                                       float thick, Material mat,
                                       MaterialPropertyBlock block)
        {
            Modeler m = new Modeler();
            float front = thick * 0.5f + 0.02f;

            // The jacket's body: the shell that wraps the body.
            // THE WIDTH FOLLOWS THE BODY: 0.70 -> 0.56.
            //
            // The first shell was WIDER than the body and stood on screen
            // like white wings sticking out on both sides. The figure's body
            // is ~0.45 m; the shell has to be a little wider than that, not
            // twice as wide.
            m.Box(new Vector3(0f, 0.02f, 0f), new Vector3(0.56f, 0.46f, thick), Chef);
            m.Box(new Vector3(0f, 0.24f, 0f), new Vector3(0.60f, 0.12f, thick + 0.03f), Chef);

            // The double row of buttons: on the FRONT face only.
            for (int i = 0; i < 3; i++)
            {
                float y = 0.16f - i * 0.13f;
                m.Box(new Vector3(-0.10f, y, front),
                      new Vector3(0.06f, 0.06f, 0.04f), ChefTrim);
                m.Box(new Vector3(0.10f, y, front),
                      new Vector3(0.06f, 0.06f, 0.04f), ChefTrim);
            }

            // The apron below the waist and the belt (the belt goes all the way round).
            m.Box(new Vector3(0f, -0.32f, 0f), new Vector3(0.52f, 0.34f, thick), Chef);
            m.Box(new Vector3(0f, -0.17f, 0f), new Vector3(0.58f, 0.07f, thick + 0.03f), Belt);

            Place(m, root, torso, scale, mat, block);
        }

        /// <summary>
        /// THE DISHWASHER'S APRON: a leather apron covering the body from
        /// top to bottom, and a neck strap.
        /// </summary>
        private static void RubberApron(Transform root, Transform torso, float scale,
                                        float thick, Material mat,
                                        MaterialPropertyBlock block)
        {
            Modeler m = new Modeler();
            float front = thick * 0.5f + 0.02f;

            // THE APRON IN FRONT, THE BACK OPEN: a real leather apron is like
            // that too. But the straps are visible at the back - when the
            // figure turns its back the player should still be able to say
            // "that is the dishwasher".
            m.Box(new Vector3(0f, -0.14f, front), new Vector3(0.52f, 0.66f, 0.06f), Rubber);
            m.Box(new Vector3(0f, 0.22f, front), new Vector3(0.28f, 0.20f, 0.06f), Rubber);

            // The neck and back straps: over the shoulder, crossing at the back.
            for (int k = 0; k < 2; k++)
            {
                float x = k == 0 ? -0.13f : 0.13f;
                m.Box(new Vector3(x, 0.28f, 0f),
                      new Vector3(0.06f, 0.14f, thick + 0.03f), Rubber);
                m.Box(new Vector3(x, 0.02f, -front),
                      new Vector3(0.06f, 0.44f, 0.05f), Rubber);
            }

            // The belt, all the way round.
            m.Box(new Vector3(0f, -0.16f, 0f), new Vector3(0.58f, 0.07f, thick + 0.03f), Belt);

            Place(m, root, torso, scale, mat, block);
        }

        /// <summary>
        /// THE WAITER'S SUIT: a dark jacket, a strip of white shirt and a
        /// bow tie.
        ///
        /// The shirt fills the opening of the collar; the bow tie is small,
        /// but it is the one mark that says "this is service" from a
        /// distance.
        /// </summary>
        private static void SuitJacket(Transform root, Transform torso, float scale,
                                       float thick, Material mat,
                                       MaterialPropertyBlock block)
        {
            Modeler m = new Modeler();
            float front = thick * 0.5f + 0.02f;

            // The jacket: a dark shell wrapping the body.
            m.Box(new Vector3(0f, 0.02f, 0f), new Vector3(0.56f, 0.50f, thick), Suit);
            m.Box(new Vector3(0f, 0.26f, 0f), new Vector3(0.60f, 0.12f, thick + 0.03f), Suit);
            m.Box(new Vector3(0f, -0.30f, 0f), new Vector3(0.52f, 0.20f, thick), Suit);

            // The shirt and the bow tie: on the FRONT face only, between the lapels.
            m.Box(new Vector3(0f, 0.02f, front), new Vector3(0.18f, 0.46f, 0.05f), Shirt);
            m.Box(new Vector3(0f, 0.21f, front + 0.02f),
                  new Vector3(0.15f, 0.07f, 0.05f), Bow);

            Place(m, root, torso, scale, mat, block);
        }

        /// <summary>
        /// THE GLOVES: a cuff at the end of each arm.
        ///
        /// The arm bone's OWN axis can differ from pack to pack; that is why
        /// the glove is placed not at the end of the bone but at a point
        /// below the bone's WORLD position (in proportion to the figure's
        /// height). When the arm hangs down, the hand is there.
        /// </summary>
        private static void Gloves(Transform left, Transform right, float height,
                                   Material mat, MaterialPropertyBlock block)
        {
            OneGlove(left, height, mat, block);
            OneGlove(right, height, mat, block);
        }

        private static void OneGlove(Transform arm, float height, Material mat,
                                   MaterialPropertyBlock block)
        {
            if (arm == null)
            {
                Debug.LogWarning("Wardrobe: no arm-left/arm-right in the skeleton - "
                                 + "could not attach the dishwasher glove");
                return;
            }

            Modeler m = new Modeler();
            m.Prism(8, 0.50f, 0.46f, 0.62f, Vector3.zero, Quaternion.identity, Glove);
            m.Prism(8, 0.54f, 0.54f, 0.16f, new Vector3(0f, 0.60f, 0f),
                    Quaternion.identity, Glove);        // the cuff opening

            GameObject go = m.Build(arm, GloveName, mat, block);
            Scale(go.transform, arm, height * 0.13f);
            go.transform.rotation = arm.rotation;
            go.transform.position = arm.position - Vector3.up * (height * 0.20f);
        }

        // =====================================================================
        /// <summary>Puts the body garment in place: scale, direction, position.</summary>
        private static void Place(Modeler m, Transform root, Transform torso,
                                  float scale, Material mat,
                                  MaterialPropertyBlock block)
        {
            GameObject go = m.Build(torso, BodyName, mat, block);
            Scale(go.transform, torso, scale);

            // THE CLOTHES SIT AT THE BODY'S CENTRE.
            //
            // It used to be shifted forwards (a slab stuck to the front of
            // the figure); once it became a shell, the centre is the right
            // place. The direction still comes FROM THE ROOT: the bone's own
            // axis can differ from pack to pack.
            go.transform.rotation = root.rotation;
            go.transform.position = torso.position;
        }

        /// <summary>Writes a local scale such that the world scale comes out as k.</summary>
        private static void Scale(Transform t, Transform parent, float k)
        {
            Vector3 ls = parent.lossyScale;
            t.localScale = new Vector3(
                k / Mathf.Max(0.0001f, ls.x),
                k / Mathf.Max(0.0001f, ls.y),
                k / Mathf.Max(0.0001f, ls.z));
        }

        /// <summary>Finds a bone by name (case-insensitive).</summary>
        private static Transform Find(Transform root, string name)
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                string n = t.name.ToLowerInvariant();
                if (n == name || n == name + "_skel" || n.StartsWith(name + "."))
                    return t;
            }
            return null;
        }

        private static void Strip(Transform parent, string name)
        {
            if (parent == null) return;
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                Transform c = parent.GetChild(i);
                if (c.name != name) continue;
                if (Application.isPlaying) Object.Destroy(c.gameObject);
                else Object.DestroyImmediate(c.gameObject);
            }
        }
    }
}
