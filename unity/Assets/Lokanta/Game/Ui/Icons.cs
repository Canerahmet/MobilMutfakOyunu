using UnityEngine;
using UnityEngine.UIElements;

namespace Lokanta.Game.Ui
{
    /// <summary>
    /// DRAWN ICONS.
    ///
    /// Not one of them comes from the font, and that is not a preference
    /// but a lesson: Rubik has no star glyph and the player saw an EMPTY
    /// BOX instead (tools/art/check_font.py caught it). The same goes for
    /// the arrow, tick and gear glyphs - every one of them is there or
    /// not there depending on the font.
    ///
    /// All of them are built out of rectangles, circles (corner radius at
    /// half the side) and ROTATION. UI Toolkit's own drawing API
    /// (Painter2D) is not used: every "worked in the editor, did not come
    /// out in the build" story in this project came from trusting
    /// something that reaches the build indirectly. A rectangle is a
    /// rectangle everywhere.
    ///
    /// Sizing: every one of them returns a SQUARE box whose side is 's'.
    /// That way one number is enough to line them up in a row.
    /// </summary>
    public static class Icons
    {
        private static VisualElement Box(float s)
        {
            VisualElement v = new VisualElement();
            v.style.width = s;
            v.style.height = s;
            v.style.flexShrink = 0;
            return v;
        }

        private static VisualElement Rect(float w, float h, Color c, float r = 0f)
        {
            VisualElement v = new VisualElement();
            v.style.width = w;
            v.style.height = h;
            v.style.backgroundColor = c;
            v.style.position = Position.Absolute;
            if (r > 0f) Theme.Round(v, r);
            return v;
        }

        private static void At(VisualElement v, float x, float y)
        {
            v.style.left = x;
            v.style.top = y;
        }

        /// <summary>Coin: a gold disc with a darker ring inside it.</summary>
        public static VisualElement Coin(float s = 22f)
        {
            VisualElement box = Box(s);
            VisualElement outer = Rect(s, s, Kit.Coin, s * 0.5f);
            At(outer, 0f, 0f);
            box.Add(outer);

            VisualElement inner = Rect(s * 0.52f, s * 0.52f,
                                       new Color(0.85f, 0.60f, 0.10f), s * 0.26f);
            At(inner, s * 0.24f, s * 0.24f);
            box.Add(inner);
            return box;
        }

        /// <summary>
        /// The reputation gem: a square turned 45 degrees.
        ///
        /// A five-pointed star cannot be drawn out of rectangles; the
        /// turned square reads both as "precious stone" and as "score",
        /// and its silhouette separates it from the till coin - the two
        /// capsules sit side by side and must not look alike.
        /// </summary>
        public static VisualElement Gem(float s = 22f)
        {
            VisualElement box = Box(s);
            VisualElement d = Rect(s * 0.68f, s * 0.68f, Kit.Gem, 3f);
            At(d, s * 0.16f, s * 0.16f);
            d.style.rotate = new Rotate(45f);
            box.Add(d);

            VisualElement shine = Rect(s * 0.22f, s * 0.22f,
                                       new Color(0.85f, 0.74f, 1f), 2f);
            At(shine, s * 0.26f, s * 0.26f);
            shine.style.rotate = new Rotate(45f);
            box.Add(shine);
            return box;
        }

        /// <summary>Gear: a ring and four teeth.</summary>
        public static VisualElement Gear(Color c, float s = 22f)
        {
            VisualElement box = Box(s);
            for (int i = 0; i < 4; i++)
            {
                VisualElement tooth = Rect(s * 0.22f, s * 0.9f, c, 2f);
                At(tooth, s * 0.39f, s * 0.05f);
                tooth.style.rotate = new Rotate(i * 45f);
                box.Add(tooth);
            }
            VisualElement ring = Rect(s * 0.62f, s * 0.62f, c, s * 0.31f);
            At(ring, s * 0.19f, s * 0.19f);
            box.Add(ring);

            VisualElement hole = Rect(s * 0.26f, s * 0.26f, Kit.CardBg, s * 0.13f);
            At(hole, s * 0.37f, s * 0.37f);
            hole.style.backgroundColor = new Color(0.071f, 0.086f, 0.118f, 1f);
            box.Add(hole);
            return box;
        }

        /// <summary>Trolley: a body and two wheels. The market screen.</summary>
        public static VisualElement Cart(Color c, float s = 22f)
        {
            VisualElement box = Box(s);
            VisualElement body = Rect(s * 0.72f, s * 0.40f, c, 3f);
            At(body, s * 0.20f, s * 0.22f);
            box.Add(body);

            VisualElement handle = Rect(s * 0.22f, s * 0.10f, c, 2f);
            At(handle, s * 0.02f, s * 0.14f);
            box.Add(handle);

            VisualElement wheel1 = Rect(s * 0.18f, s * 0.18f, c, s * 0.09f);
            At(wheel1, s * 0.26f, s * 0.70f);
            box.Add(wheel1);
            VisualElement wheel2 = Rect(s * 0.18f, s * 0.18f, c, s * 0.09f);
            At(wheel2, s * 0.62f, s * 0.70f);
            box.Add(wheel2);
            return box;
        }

        /// <summary>Chef's hat: three puffs and a band. The crew screen.</summary>
        public static VisualElement Hat(Color c, float s = 22f)
        {
            VisualElement box = Box(s);
            VisualElement puff1 = Rect(s * 0.34f, s * 0.34f, c, s * 0.17f);
            At(puff1, s * 0.02f, s * 0.16f);
            box.Add(puff1);
            VisualElement puff2 = Rect(s * 0.40f, s * 0.40f, c, s * 0.20f);
            At(puff2, s * 0.30f, s * 0.06f);
            box.Add(puff2);
            VisualElement puff3 = Rect(s * 0.34f, s * 0.34f, c, s * 0.17f);
            At(puff3, s * 0.64f, s * 0.16f);
            box.Add(puff3);

            VisualElement body = Rect(s * 0.74f, s * 0.30f, c, 2f);
            At(body, s * 0.13f, s * 0.38f);
            box.Add(body);

            VisualElement band = Rect(s * 0.80f, s * 0.24f, c, 3f);
            At(band, s * 0.10f, s * 0.66f);
            box.Add(band);
            return box;
        }

        /// <summary>List: three rows, each with a dot in front. The menu screen.</summary>
        public static VisualElement List(Color c, float s = 22f)
        {
            VisualElement box = Box(s);
            for (int i = 0; i < 3; i++)
            {
                float y = s * (0.14f + i * 0.30f);
                VisualElement dot = Rect(s * 0.16f, s * 0.16f, c, s * 0.08f);
                At(dot, 0f, y);
                box.Add(dot);

                VisualElement row = Rect(s * 0.62f, s * 0.14f, c, 2f);
                At(row, s * 0.26f, y + s * 0.01f);
                box.Add(row);
            }
            return box;
        }

        /// <summary>Up arrow: a shaft and a turned square instead of a triangle.</summary>
        public static VisualElement ArrowUp(Color c, float s = 22f)
        {
            VisualElement box = Box(s);
            VisualElement shaft = Rect(s * 0.22f, s * 0.52f, c, 2f);
            At(shaft, s * 0.39f, s * 0.40f);
            box.Add(shaft);

            // A turned square reads as an arrowhead: its top half shows and
            // its bottom half hides behind the shaft.
            VisualElement head = Rect(s * 0.46f, s * 0.46f, c, 2f);
            At(head, s * 0.27f, s * 0.10f);
            head.style.rotate = new Rotate(45f);
            box.Add(head);
            return box;
        }

        /// <summary>Two people: the satisfaction card.</summary>
        public static VisualElement People(Color c, float s = 22f)
        {
            VisualElement box = Box(s);
            VisualElement head1 = Rect(s * 0.30f, s * 0.30f, c, s * 0.15f);
            At(head1, s * 0.04f, s * 0.10f);
            box.Add(head1);
            VisualElement body1 = Rect(s * 0.40f, s * 0.34f, c, s * 0.10f);
            At(body1, 0f, s * 0.46f);
            box.Add(body1);

            VisualElement head2 = Rect(s * 0.34f, s * 0.34f, c, s * 0.17f);
            At(head2, s * 0.50f, s * 0.04f);
            box.Add(head2);
            VisualElement body2 = Rect(s * 0.46f, s * 0.38f, c, s * 0.11f);
            At(body2, s * 0.44f, s * 0.44f);
            box.Add(body2);
            return box;
        }

        /// <summary>
        /// The play triangle.
        ///
        /// UI Toolkit has no triangle of its own; the second way is the
        /// BORDER TRICK (a zero-sized element with three transparent
        /// borders). That trick depends on the shader and on how borders
        /// are joined - exactly the kind of bet this project avoids.
        /// Instead: a square turned 45 degrees and clipped in half.
        /// Clipping (overflow: hidden) is UI Toolkit's most basic
        /// behaviour.
        /// </summary>
        public static VisualElement Play(Color c, float s = 18f)
        {
            VisualElement box = Box(s);
            box.style.overflow = Overflow.Hidden;

            VisualElement square = Rect(s * 0.72f, s * 0.72f, c, 2f);
            At(square, -s * 0.22f, s * 0.14f);
            square.style.rotate = new Rotate(45f);
            box.Add(square);

            // FOR SOMEONE READING RIGHT TO LEFT, "FORWARD" IS TO THE LEFT.
            //
            // This is the only icon that carries a direction: "open the
            // day", "the next day". With everything flowing to the right in
            // the Arabic interface, a triangle pointing right would be
            // pointing the player back the way they came. The other icons
            // (pause, book, plate) have no direction - those are not
            // flipped.
            if (Loc.IsRightToLeft)
                box.style.scale = new Scale(new Vector2(-1f, 1f));
            return box;
        }

        /// <summary>Pause: two bars.</summary>
        public static VisualElement Pause(Color c, float s = 20f)
        {
            VisualElement box = Box(s);
            VisualElement left = Rect(s * 0.26f, s * 0.80f, c, 2f);
            At(left, s * 0.14f, s * 0.10f);
            box.Add(left);
            VisualElement right = Rect(s * 0.26f, s * 0.80f, c, 2f);
            At(right, s * 0.60f, s * 0.10f);
            box.Add(right);
            return box;
        }

        /// <summary>Combo: two stacked plates. The signature mechanic.</summary>
        public static VisualElement Combo(Color c, float s = 20f)
        {
            VisualElement box = Box(s);
            VisualElement bottom = Rect(s, s * 0.26f, c, s * 0.13f);
            At(bottom, 0f, s * 0.62f);
            box.Add(bottom);
            VisualElement middle = Rect(s * 0.78f, s * 0.24f, c, s * 0.12f);
            At(middle, s * 0.11f, s * 0.34f);
            box.Add(middle);
            VisualElement top = Rect(s * 0.52f, s * 0.22f, c, s * 0.11f);
            At(top, s * 0.24f, s * 0.08f);
            box.Add(top);
            return box;
        }

        /// <summary>The book of tabs: a cover and a page edge.</summary>
        public static VisualElement Book(Color c, float s = 20f)
        {
            VisualElement box = Box(s);
            VisualElement cover = Rect(s * 0.82f, s * 0.90f, c, 3f);
            At(cover, s * 0.09f, s * 0.05f);
            box.Add(cover);
            VisualElement page = Rect(s * 0.12f, s * 0.90f,
                                      new Color(0.071f, 0.086f, 0.118f, 1f), 1f);
            At(page, s * 0.26f, s * 0.05f);
            box.Add(page);
            return box;
        }

        /// <summary>Plus: two bars.</summary>
        public static VisualElement Plus(Color c, float s = 14f)
        {
            VisualElement box = Box(s);
            VisualElement vertical = Rect(s * 0.22f, s, c, 1f);
            At(vertical, s * 0.39f, 0f);
            box.Add(vertical);
            VisualElement horizontal = Rect(s, s * 0.22f, c, 1f);
            At(horizontal, 0f, s * 0.39f);
            box.Add(horizontal);
            return box;
        }

        /// <summary>
        /// Checkbox: an empty square, or one with a tick in it.
        ///
        /// The tick is two bars: the short one leans left, the long one
        /// right.
        /// </summary>
        public static VisualElement Check(bool ok, Color c, float s = 16f)
        {
            VisualElement box = Box(s);
            VisualElement frame = Rect(s, s, Color.clear, 4f);
            frame.style.borderTopWidth = 2;
            frame.style.borderBottomWidth = 2;
            frame.style.borderLeftWidth = 2;
            frame.style.borderRightWidth = 2;
            Color edge = ok ? c : Theme.Line;
            frame.style.borderTopColor = edge;
            frame.style.borderBottomColor = edge;
            frame.style.borderLeftColor = edge;
            frame.style.borderRightColor = edge;
            if (ok) frame.style.backgroundColor = new Color(c.r, c.g, c.b, 0.22f);
            At(frame, 0f, 0f);
            box.Add(frame);

            if (!ok) return box;

            VisualElement shortArm = Rect(s * 0.14f, s * 0.34f, c, 1f);
            At(shortArm, s * 0.26f, s * 0.42f);
            shortArm.style.rotate = new Rotate(-45f);
            box.Add(shortArm);

            VisualElement longArm = Rect(s * 0.14f, s * 0.62f, c, 1f);
            At(longArm, s * 0.56f, s * 0.16f);
            longArm.style.rotate = new Rotate(35f);
            box.Add(longArm);
            return box;
        }
    }
}
