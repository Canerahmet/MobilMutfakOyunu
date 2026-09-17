using UnityEngine;

namespace Lokanta.Game
{
    /// <summary>
    /// THE SCENE'S DECORATION AND THE CUISINE'S IDENTITY.
    ///
    /// The reference the user brought shows four cuisines SIDE BY SIDE:
    /// the same interface, the same camera angle, but four different
    /// restaurants. What makes the difference is not the architecture - it
    /// is the colour of the wall, the light of the sign, the lamps hanging
    /// from the ceiling, the rug on the floor and the plant pots.
    ///
    /// docs/25 had already written this down as a rule: "cuisines are told
    /// apart by the ENVIRONMENT, the LIGHT, the SILHOUETTE and the
    /// CLOTHES; never by facial features". This is the environment leg of
    /// that rule.
    ///
    /// It is all PROCEDURAL (Modeler) and gathered BY COLOUR into single
    /// meshes: the whole decoration, however many parts it is made of, is
    /// a handful of draws.
    /// </summary>
    public sealed partial class RestaurantView
    {
        /// <summary>
        /// A cuisine's environment palette.
        ///
        /// The colours go through the game's own material (URP/Lit + a
        /// property block), so there is no texture - the difference is
        /// entirely COLOUR and FORM.
        /// </summary>
        public struct Palette
        {
            public Color Wall;       // the back wall
            public Color WallTrim;   // skirting and frame
            public Color Wood;       // wood: the counter, the shelf, the body of a pendant
            public Color Metal;      // metal: the extractor hood, a leg
            public Color Accent;     // the colour of the identity: neon / copper
            public Color Sign;       // the sign's light (emissive)
            public Color Lamp;       // the pendant lamp's light (emissive)
            public Color Plant;      // leaf
            public Color Rug;        // rug / floor covering
            public Color Seat;       // chair cushion / bench
            public Color Floor;      // the light tone of the floor pattern
            public Color FloorDark;   // the dark tone of the floor pattern
            public bool HasRug;      // is there a rug?
            public bool Planks;      // the floor pattern: planks or tiles?

            // --- THE SURFACE OF THE WALL ------------------------------
            //
            // The wall was a single flat box: body + skirting + cornice. The
            // two cuisines showed the same wall in different colours, so half
            // of the "I have walked into somewhere else" feeling was missing.
            //
            // The distinction that came out of the research (sources in
            // docs/50):
            //   The Turkish restaurant - WOODEN WAINSCOT. The defining surface
            //   of the traditional tradesman's restaurant; plastered wall above
            //   it.
            //   Fast food            - PANEL SEAMS + A STEEL BAND. Hard,
            //   wipe-clean surfaces; a horizontal steel strip and vertical
            //   panel joints.
            //
            // Both are Modeler boxes, so there is no new asset - and no
            // downloaded texture either (nothing is added that would go into
            // the licence ledger).

            /// <summary>The wainscot height, in metres. 0 means no wainscot.</summary>
            public float Wainscot;

            /// <summary>The wainscot / panel colour.</summary>
            public Color WainscotColor;

            /// <summary>The colour of the thin band along the wainscot's top edge.</summary>
            public Color WainscotCap;

            /// <summary>The vertical seam spacing, in metres. 0 means no seams.</summary>
            public float SeamStep;

            /// <summary>The tint of the outside; it is mixed into the background sky.</summary>
            public Color Sky;
        }

        /// <summary>
        /// THE CUISINE'S PALETTE.
        ///
        /// Fast food: dark brick, NEON RED light, steel metal. The first
        /// frame of the reference is exactly this - a red sign, a stainless
        /// counter, a black-and-red floor.
        ///
        /// Turkish: warm brick and wood on the wall, COPPER light, a rug on
        /// the floor. The second frame of the reference: brass lanterns, a
        /// kilim, wooden lattice.
        /// </summary>
        public static Palette Pal(string cuisine)
        {
            if (cuisine == "turk")
            {
                return new Palette
                {
                    Wall = new Color(0.290f, 0.196f, 0.137f),
                    WallTrim = new Color(0.176f, 0.118f, 0.082f),
                    Wood = new Color(0.424f, 0.282f, 0.173f),
                    Metal = new Color(0.706f, 0.545f, 0.267f),
                    Accent = new Color(0.804f, 0.561f, 0.239f),
                    Sign = new Color(1.000f, 0.729f, 0.322f),
                    Lamp = new Color(1.000f, 0.843f, 0.596f),
                    Seat = new Color(0.451f, 0.192f, 0.176f),
                    Plant = new Color(0.267f, 0.427f, 0.243f),
                    Rug = new Color(0.478f, 0.161f, 0.133f),
                    Floor = new Color(0.404f, 0.290f, 0.196f),
                    FloorDark = new Color(0.341f, 0.239f, 0.161f),
                    HasRug = true,
                    Planks = true,

                    // A WOODEN WAINSCOT: the defining surface of the traditional
                    // tradesman's restaurant. Plastered wall above it, a thin
                    // copper band along the top edge.
                    //
                    // 1.05 m was chosen because that is the height of a SEATED
                    // person's back: in real life a wainscot is there to protect
                    // against chair height, not as decoration.
                    Wainscot = 1.05f,
                    WainscotColor = new Color(0.361f, 0.235f, 0.141f),
                    WainscotCap = new Color(0.706f, 0.545f, 0.267f),
                    SeamStep = 0.85f,
                    // The outside is warm too: a neighbourhood, dusty midday light.
                    Sky = new Color(0.82f, 0.68f, 0.48f),
                };
            }

            return new Palette
            {
                Wall = new Color(0.173f, 0.184f, 0.212f),
                WallTrim = new Color(0.106f, 0.114f, 0.133f),
                // THE FURNITURE: LAMINATE, NOT WOOD.
                //
                // Putting the two halls SIDE BY SIDE (render/salon_*_oda.png)
                // showed that the floor and the wall had separated but THE
                // TABLES WERE IDENTICAL - both of them the same brown wood. The
                // surface that takes up most of the hall is the table top, so
                // half the distinction was still missing.
                //
                // Fast food furniture is wipe-clean laminate: a light, almost
                // cold beige. A dark floor + a light top + a red cushion is the
                // arrangement in all three of the frames the user brought. The
                // Turkish side STAYS brown wood.
                Wood = new Color(0.686f, 0.612f, 0.510f),
                Metal = new Color(0.616f, 0.651f, 0.702f),
                Accent = new Color(0.847f, 0.239f, 0.196f),
                Sign = new Color(1.000f, 0.314f, 0.251f),
                // THE LAMP'S LIGHT IS SEPARATE FROM THE SIGN'S.
                //
                // In the first attempt the mouths of the pendants glowed in the
                // sign's colour too, and the fast food hall filled with a PINK
                // light. The sign is the colour of the identity (neon red), while
                // the lamp is the same thing in every restaurant: warm white.
                Lamp = new Color(1.000f, 0.898f, 0.749f),
                // The fast food chair is RED: in the first frame of the
                // reference it is what gives the hall its colour. On the Turkish
                // side it is dark burgundy - the same family, a different tone.
                Seat = new Color(0.729f, 0.220f, 0.192f),
                Plant = new Color(0.286f, 0.478f, 0.290f),
                Rug = new Color(0.216f, 0.231f, 0.267f),
                // THE FLOOR WAS LIGHTENED: 0.32/0.24 -> 0.42/0.33.
                //
                // The dark tile looked "shot at night" close up and swallowed
                // the dark furniture standing on it. In the reference's fast
                // food frame the floor is a MID tone; what gives the identity is
                // not the darkness of the floor but the neon red and the steel.
                Floor = new Color(0.420f, 0.435f, 0.467f),
                FloorDark = new Color(0.325f, 0.341f, 0.373f),
                HasRug = false,
                Planks = false,

                // PANEL SEAMS + A STEEL BAND.
                //
                // The surface of the fast food hall has to be "wipe-clean":
                // laminate panel joints and a stainless strip at counter height.
                // NO wainscot - that is the language of a different place.
                Wainscot = 1.15f,
                WainscotColor = new Color(0.137f, 0.145f, 0.169f),
                WainscotCap = new Color(0.616f, 0.651f, 0.702f),
                SeamStep = 1.15f,
                // The outside is cold and urban: a main road, tarmac, glass.
                Sky = new Color(0.58f, 0.68f, 0.82f),
            };
        }

        /// <summary>
        /// THE FURNITURE CARRIES THE IDENTITY TOO.
        ///
        /// The pack's materials HAVE NO TEXTURE - they are all a flat
        /// _BaseColor (Furniture_wood, Furniture_metal, ...). So colouring
        /// them by cuisine does not mean wrestling with textures: a COPY of
        /// the material is taken and its colour written from the palette.
        ///
        /// ONE COPY PER MATERIAL. The alternative was writing a property
        /// block to every renderer, and that throws the renderers OUT of SRP
        /// batching (this project learned that on the floor slabs): a hundred
        /// pieces of furniture would have meant a hundred separate draws. A
        /// single copy paints all the furniture sharing that material at
        /// once.
        /// </summary>
        private Material Tinted(Material src)
        {
            if (src == null) return null;
            if (_tinted.TryGetValue(src, out Material ready)) return ready;

            Palette p = Pal(CuisineId);
            string srcName = src.name.ToLowerInvariant();
            Color? tint = null;

            if (srcName.Contains("carpet")) tint = p.Seat;
            else if (srcName.Contains("wooddark")) tint = p.Wood * 0.72f;
            else if (srcName.Contains("wood")) tint = p.Wood;
            // THE METAL IS NOT PAINTED.
            //
            // In the first attempt the palette's metal (BRASS in the Turkish
            // one) went to every metal part and the kitchen turned GOLD: the
            // sink, the counter, the fridge. What was learned on the
            // extractor hood holds here too - equipment is stainless in every
            // restaurant; brass is a DECORATION colour (a railing, a lantern)
            // and it stays there.
            else if (srcName.Contains("lamp")) tint = p.Sign;
            else if (srcName.Contains("plant")) tint = p.Plant;

            Material m = src;
            if (tint.HasValue)
            {
                Color c = tint.Value;
                c.a = 1f;
                m = new Material(src);
                m.name = src.name + "_" + CuisineId;
                m.SetColor(BaseColorId, c);
            }
            _tinted[src] = m;
            return m;
        }

        private readonly System.Collections.Generic.Dictionary<Material, Material>
            _tinted = new System.Collections.Generic.Dictionary<Material, Material>();

        /// <summary>
        /// Destroys the tint copies and empties the cache.
        ///
        /// The copies are produced with `new Material(src)`, so each of them
        /// is a SEPARATE UnityEngine.Object. The cache was emptied in
        /// Rebuild() but the copies were not destroyed: up to five materials
        /// per tier leaked permanently, and it multiplied in the editor
        /// tools when the identity changed (PreviewCuisine).
        ///
        /// THE SOURCE ITSELF IS NOT DESTROYED: `Tinted` returns the source
        /// unchanged for a material that needs no colour, and that is a DISK
        /// ASSET - deleting it would lose the material for the next scene.
        /// That is exactly why the list in OnDestroy is short.
        /// </summary>
        private void ClearTints()
        {
            foreach (System.Collections.Generic.KeyValuePair<Material, Material> kv in _tinted)
            {
                if (kv.Value == null || kv.Value == kv.Key) continue;
                if (Application.isPlaying) Destroy(kv.Value);
                else DestroyImmediate(kv.Value);
            }
            _tinted.Clear();
        }

        /// <summary>
        /// Turns all the renderers of a prefab copy to the identity's tint.
        /// Called from every place that puts one down.
        /// </summary>
        private void Retint(GameObject go)
        {
            if (go == null) return;
            foreach (Renderer r in go.GetComponentsInChildren<Renderer>(true))
            {
                Material[] ms = r.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < ms.Length; i++)
                {
                    Material t = Tinted(ms[i]);
                    if (t == ms[i]) continue;
                    ms[i] = t;
                    changed = true;
                }
                if (changed) r.sharedMaterials = ms;
            }
        }

        /// <summary>For the screenshot tool: so the identity can be chosen outside the game too.</summary>
        [System.NonSerialized] public string PreviewCuisine;

        private string CuisineId
        {
            get
            {
                if (App != null && App.Content != null) return App.Content.Cuisine;
                return string.IsNullOrEmpty(PreviewCuisine) ? "fastfood" : PreviewCuisine;
            }
        }

        /// <summary>The height of the back wall. Much taller than the rooms' walls.</summary>
        private const float BackWallHeight = 2.60f;

        // =====================================================================
        // THE CEILING LAMP IS NOT DRAWN - THE LIGHT IS.
        //
        // There WAS a pendant lamp here once and the user said the same
        // thing twice: "the lamps should not be physically visible, since
        // they will be on the ceiling" (docs/38) and then again after the
        // reference images: "the ceiling lights should not be visible".
        //
        // In between I had looked at the reference and added the pendants;
        // the reference's camera is lower and there a pendant is half the
        // room. Our camera looks from 34 degrees at a building with NO
        // CEILING - the pendants turned into objects hanging over the hall
        // and COVERING the place they lit.
        //
        // The light stays: the pools on the floor (BuildRoomLights) and the
        // evening's warm fill. What the player saw was ALREADY the light,
        // not the lamp itself.
        //
        // The empty-bodied Pendants() method and the PendantY constant were
        // DELETED: "a field that lies to its call sites is deleted, not
        // wired up".
        private void BuildDecor(int tables)
        {
            Palette p = Pal(CuisineId);
            if (_block == null) _block = new MaterialPropertyBlock();

            // THE BOUNDARY OF THE OPEN ROOMS: the decoration grows with the
            // tier too. The street lamps use the same calculation.
            float left = float.MaxValue, right = 0f, back = 0f;
            for (int i = 0; i < RoomPlan.Rooms.Length; i++)
            {
                RoomPlan.Room r = RoomPlan.Rooms[i];
                if (!RoomPlan.RoomOpen(in r, tables)) continue;
                if (r.X0 < left) left = r.X0;
                if (r.X0 + r.W > right) right = r.X0 + r.W;
                if (r.Z0 + r.D > back) back = r.Z0 + r.D;
            }
            if (left > right) return;

            // THE OUTSIDE BELONGS TO THE CUISINE TOO.
            //
            // The background stands in for the sky and it was identical in
            // both cuisines. It is bound here because the cuisine is chosen AT
            // RUNTIME - it is not yet known while the scene is being built
            // (BuildGameScene).
            DayLight dayLight = FindFirstObjectByType<DayLight>();
            if (dayLight != null) dayLight.SkyTint = p.Sky;

            Modeler m = new Modeler();
            Modeler glow = new Modeler();

            FloorPattern(m, p, tables);
            Backdrop(m, p, left, right, back);
            Skyline(m, p, left, right, back);
            Planters(m, p, left, right);
            KitchenHood(m, p, tables);
            ServiceCounter(m, glow, p, tables);
            TrayStation(m, glow, p, tables);
            Shelves(m, p, tables);
            Booths(m, p, tables);
            Boards(m, glow, p, left, right, back, tables);
            Rugs(m, p, tables);
            Terrace(m, p, left, right);
            Storefront(m, p, left, right);
            PatioSeats(m, p, left, right);

            // receive: true - THE FLOOR IS IN HERE. `FloorPattern` is part of this
            // group and it is what the player sees underfoot; with shadows
            // refused, the guests and the staff stood on a flat colour and
            // nothing in the building was attached to the ground.
            _decor = m.Build(transform, "Decor", _floorMat, _block, receive: true);

            // THE GLOWING PARTS ARE ON A SEPARATE MATERIAL: while the emission
            // keyword is off the shader never reads that field, so a colour
            // written to the sign does nothing at all.
            if (_lampMat == null && _floorMat != null)
            {
                _lampMat = new Material(_floorMat);
                _lampMat.EnableKeyword("_EMISSION");
                _lampMat.globalIlluminationFlags =
                    MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            // THE GLOWING PARTS' COLOUR COMES FROM THE MESH.
            //
            // Because Modeler separates by colour, every glowing colour is on
            // ITS OWN renderer: the sign can glow red while the pendants glow
            // warm white. The emission is derived from the renderer's OWN base
            // colour - there is no need to write the colour in two places.
            _decorGlow = glow.Build(transform, "DecorGlow", _lampMat, _block);
            foreach (Renderer r in _decorGlow.GetComponentsInChildren<Renderer>())
            {
                r.GetPropertyBlock(_block);
                Color c = _block.GetColor(BaseColorId);
                if (c.a <= 0f) c = p.Lamp;
                _block.SetColor(BaseColorId, c);
                _block.SetColor(Shader.PropertyToID("_EmissionColor"), c * 1.9f);
                r.SetPropertyBlock(_block);
            }
        }

        private GameObject _decor, _decorGlow;

        /// <summary>The number of decoration parts (draws). So the tour can ask.</summary>
        public int DecorDrawCount
        {
            get
            {
                int n = _decor != null ? _decor.transform.childCount : 0;
                return n + (_decorGlow != null ? _decorGlow.transform.childCount : 0);
            }
        }

        // =====================================================================
        /// THE BACKDROP: a tall and SOLID wall, with returns at either
        /// side.
        ///
        /// The room walls are 1.15 m and transparent - they are like that so
        /// that the inside of the hall can be seen from a 34 degree view. But
        /// that left the BACK of the restaurant empty too: the sky began
        /// where the building ended and the place read like a "floor plan".
        ///
        /// In all four reference frames the back wall is SOLID: brick, wood,
        /// with a sign and shelves on it. The back wall covers nothing - the
        /// camera is looking at it from behind anyway - but it makes the
        /// place a ROOM.
        /// </summary>
        /// <summary>
        /// THE BLOCK THE RESTAURANT STANDS IN: a row of neighbouring facades
        /// behind it, and one at each end of the street.
        ///
        /// WHY. Measured off the store frame: 37% of it was a single flat
        /// colour - the camera's clear colour standing in for the sky - and
        /// the building sat in it as a slab with a hard dark edge and nothing
        /// behind, above or beside it. That is the clearest "this is a level
        /// editor, not a place" tell in the picture, and at night it is
        /// forgiven only because darkness is a plausible thing to see.
        ///
        /// IT COSTS NOTHING IN FRAMING. `CameraFit.OpenBounds` is built from
        /// the open rooms plus `StreetInFrame` and nothing else - I read it
        /// before writing this - so geometry placed BEHIND the back wall does
        /// not move the camera a millimetre and does not shrink the
        /// restaurant. That is the whole reason this is the cheap fix and
        /// "pull the camera in" is not: the camera is already bound by the
        /// touch-target measurement in docs/31.
        ///
        /// WHAT THEY ARE NOT. They are not buildings you can enter, light or
        /// expand into; they are a backdrop. So they are deliberately dull:
        /// darker than the restaurant's own wall, low contrast between
        /// neighbours, no windows lit by day. A skyline that competes with the
        /// hall for attention would be worse than the flat colour, which at
        /// least does not pretend to be interesting.
        ///
        /// THE HEIGHTS ARE VARIED BUT NOT RANDOM PER FRAME. The pattern is
        /// derived from the facade's index, so the street looks the same every
        /// time the scene is built - a skyline that reshuffles on a rebuild
        /// would be a bug the player sees as flicker.
        /// </summary>
        private void Skyline(Modeler m, Palette p, float left, float right, float back)
        {
            // FAR BACK. The first attempt put them at back + 2.6 and they
            // loomed: a dark mass directly behind the roofline, filling the
            // top of the frame and sitting under the day counter in the HUD.
            // The measurement improved (37.3% of the frame flat, down to
            // 26.5%) and the PICTURE got worse, which is the whole argument
            // for looking at the frame as well as at the number.
            float z = back + 7.0f;

            // Wider than the plot on both sides: the row has to run out of the
            // frame, not stop inside it. A skyline with visible ends is a
            // stage set.
            float from = left - 9f;
            float to = right + 9f;

            // ATMOSPHERE, NOT DARKNESS.
            //
            // The first attempt made them DARKER than the restaurant's wall,
            // on the reasoning that the building in front should stay the
            // brightest thing. It does - but a dark block against a mid-grey
            // sky reads as a heavy near object, not a distant one, and the
            // row came out as a black wall pressing on the roofline.
            //
            // Distance desaturates and lifts towards the sky; it does not
            // darken. These are the wall colour pulled most of the way to a
            // cool haze, so they sit BEHIND the sky's own value rather than
            // in front of it, and the restaurant stays the only saturated
            // thing in the frame.
            Color haze = new Color(0.36f, 0.38f, 0.42f);
            Color a = Color.Lerp(p.Wall, haze, 0.72f);
            Color b = Color.Lerp(p.Wall, haze, 0.80f);
            Color roof = Color.Lerp(p.Wall, haze, 0.62f);

            int i = 0;
            for (float x = from; x < to; i++)
            {
                // Widths cycle 4.2 / 6.0 / 5.1 so the rhythm does not read as
                // a fence.
                float w = i % 3 == 0 ? 4.2f : (i % 3 == 1 ? 6.0f : 5.1f);
                // Heights cycle over five steps between 3.6 and 6.0 m. The
                // back wall is 2.6, so every one clears it - and the ceiling
                // is low enough that the row sits along the top of the frame
                // instead of filling it.
                float h = 3.6f + (i % 5) * 0.6f;

                m.Box(new Vector3(x + w * 0.5f, h * 0.5f, z + 2.0f),
                      new Vector3(w - 0.35f, h, 4.0f), i % 2 == 0 ? a : b);

                // A parapet: the line that stops a block reading as a
                // rectangle of colour.
                m.Box(new Vector3(x + w * 0.5f, h + 0.13f, z + 2.0f),
                      new Vector3(w - 0.20f, 0.26f, 4.3f), roof);

                x += w;
            }
        }

        /// <summary>Scales a colour's brightness, keeping its alpha.</summary>
        private static Color Mul(Color c, float k)
        {
            return new Color(c.r * k, c.g * k, c.b * k, c.a);
        }

        private void Backdrop(Modeler m, Palette p, float left, float right, float back)
        {
            float width = right - left;
            float center = (left + right) * 0.5f;

            // The body of the wall
            m.Box(new Vector3(center, BackWallHeight * 0.5f, back + 0.09f),
                  new Vector3(width + 0.36f, BackWallHeight, 0.18f), p.Wall);

            // The skirting and the top cornice: two thin strips that break
            // the wall's flat surface and make its scale readable.
            m.Box(new Vector3(center, 0.09f, back + 0.05f),
                  new Vector3(width + 0.40f, 0.18f, 0.26f), p.WallTrim);
            m.Box(new Vector3(center, BackWallHeight - 0.07f, back + 0.05f),
                  new Vector3(width + 0.40f, 0.14f, 0.26f), p.WallTrim);

            // --- THE SURFACE: WAINSCOT / PANEL ------------------------
            //
            // Up to these lines the wall was a SINGLE FLAT BOX and the two
            // cuisines showed the same surface in different colours. The
            // difference is now in the material: a wooden wainscot in the
            // Turkish one, panel seams + a steel band in the fast food one.
            //
            // It is all thin boxes; no new asset, no downloaded texture.
            if (p.Wainscot > 0f)
            {
                // The body: a 2 cm projection in front of the wall - that is
                // what makes it different from a flat change of colour, a
                // shadow forms along its edge.
                m.Box(new Vector3(center, p.Wainscot * 0.5f, back + 0.02f),
                      new Vector3(width + 0.36f, p.Wainscot, 0.06f),
                      p.WainscotColor);

                // The top band: the thin strip that finishes the wainscot
                // (copper in the Turkish one, stainless in the fast food one).
                // Both are there in the reference.
                m.Box(new Vector3(center, p.Wainscot + 0.02f, back + 0.00f),
                      new Vector3(width + 0.38f, 0.05f, 0.10f),
                      p.WainscotCap);
            }

            // Vertical seams: they divide the wainscot into boards and the
            // panel into sheets. This is what makes the scale readable - on a
            // flat surface the eye cannot judge distance.
            if (p.SeamStep > 0.2f && p.Wainscot > 0f)
            {
                int count = Mathf.FloorToInt(width / p.SeamStep);
                for (int i = 1; i <= count; i++)
                {
                    float x = left + p.SeamStep * i;
                    if (x > right - 0.1f) break;
                    m.Box(new Vector3(x, p.Wainscot * 0.5f, back - 0.01f),
                          new Vector3(0.025f, p.Wainscot - 0.04f, 0.04f),
                          p.WallTrim);
                }
            }

            // The side returns: the place is closed off on both sides.
            for (int i = 0; i < 2; i++)
            {
                float x = i == 0 ? left - 0.09f : right + 0.09f;
                m.Box(new Vector3(x, BackWallHeight * 0.5f, back - 0.85f),
                      new Vector3(0.18f, BackWallHeight, 1.9f), p.Wall);
                m.Box(new Vector3(x, 0.09f, back - 0.85f),
                      new Vector3(0.24f, 0.18f, 1.9f), p.WallTrim);

                // The same surface on the side walls too: if one were
                // wainscoted and the other flat, the place would look half
                // finished.
                if (p.Wainscot > 0f)
                {
                    float xi = i == 0 ? x + 0.10f : x - 0.10f;
                    m.Box(new Vector3(xi, p.Wainscot * 0.5f, back - 0.85f),
                          new Vector3(0.06f, p.Wainscot, 1.9f), p.WainscotColor);
                    m.Box(new Vector3(xi, p.Wainscot + 0.02f, back - 0.85f),
                          new Vector3(0.10f, 0.05f, 1.9f), p.WainscotCap);
                }
            }
        }

        /// PLANT POTS: along the front and at the edge of the terrace.
        ///
        /// There is greenery in all four reference frames and it does the
        /// same job in all of them: it breaks the building's straight edge
        /// and says "this is a place that is looked after". The plant also
        /// does not change with the cuisine - it is in all four, because it
        /// is universal.
        /// </summary>
        private void Planters(Modeler m, Palette p, float left, float right)
        {
            // Along the front: between the pavement and the building (z = -0.10).
            int count = Mathf.Max(2, Mathf.RoundToInt((right - left) / 3.2f));
            float gap = (right - left) / count;

            for (int i = 0; i <= count; i++)
            {
                float x = left + gap * i;
                // Not blocking the front of the door: the entrance is at Paths.DoorX.
                if (Mathf.Abs(x - Paths.DoorX) < 1.3f) continue;
                Planter(m, p, x, -0.26f);
            }
        }

        private void Planter(Modeler m, Palette p, float x, float z)
        {
            // The pot: narrow at the bottom, wide at the top - the
            // silhouette of a cast planter.
            m.Prism(6, 0.17f, 0.22f, 0.30f, new Vector3(x, 0f, z),
                    Quaternion.identity, p.WallTrim);
            m.Box(new Vector3(x, 0.31f, z), new Vector3(0.46f, 0.05f, 0.46f), p.Wood);

            // The leaves: a cluster at three different heights. A single
            // sphere read like a "ball"; three clusters read like a shrub.
            m.Prism(6, 0.20f, 0.05f, 0.34f, new Vector3(x, 0.32f, z),
                    Quaternion.identity, p.Plant);
            m.Prism(5, 0.13f, 0.03f, 0.26f, new Vector3(x - 0.12f, 0.33f, z + 0.06f),
                    Quaternion.identity, p.Plant);
            m.Prism(5, 0.12f, 0.03f, 0.22f, new Vector3(x + 0.11f, 0.33f, z - 0.05f),
                    Quaternion.identity, p.Plant);
        }

        /// THE EXTRACTOR HOOD: above the row of stoves.
        ///
        /// In all four reference frames there is a stainless hood above the
        /// kitchen and it is the one part that says "this is a kitchen". Our
        /// kitchen had stoves but nothing above them.
        /// </summary>
        private void KitchenHood(Modeler m, Palette p, int tables)
        {
            for (int i = 0; i < RoomPlan.Rooms.Length; i++)
            {
                RoomPlan.Room r = RoomPlan.Rooms[i];
                if (r.Name != "Kitchen") continue;
                if (!RoomPlan.RoomOpen(in r, tables)) return;

                float z = r.Z0 + r.D - 0.75f;
                float x = r.CenterX - 0.30f;
                float w = r.W - 1.9f;

                // The body: a funnel widening downwards.
                // THE HOOD IS STEEL IN EVERY CUISINE.
                //
                // In the first attempt it took the identity's metal colour and
                // stood in the Turkish kitchen as a GOLD box. A hood is not
                // decoration but equipment; it is stainless in every restaurant.
                Color steel = new Color(0.576f, 0.612f, 0.659f);
                m.Box(new Vector3(x, 1.62f, z), new Vector3(w, 0.34f, 0.86f), steel);
                m.Box(new Vector3(x, 1.82f, z), new Vector3(w * 0.55f, 0.30f, 0.50f),
                      steel);
                // The lower mouth: a dark strip, the mouth of the funnel.
                m.Box(new Vector3(x, 1.44f, z), new Vector3(w - 0.12f, 0.06f, 0.74f),
                      p.WallTrim);
                return;
            }
        }

        /// THE SIGN AND THE MENU BOARD.
        ///
        /// Both are in every frame of the reference and both are things that
        /// make a RESTAURANT a restaurant: your name is written outside, what
        /// you sell is written inside.
        ///
        /// NO TEXT. The game's font is tied to UI Toolkit; drawing text in
        /// the world needs a separate package (TextMeshPro) and that package
        /// is not in this project. Instead the sign speaks with COLOUR and
        /// LIGHT: a neon red frame in the fast food one, a copper plate in
        /// the Turkish one. The menu board shows its lines as light strips
        /// too - from a distance that is exactly how a menu board reads
        /// anyway.
        /// </summary>
        private void Boards(Modeler m, Modeler glow, Palette p,
                            float left, float right, float back, int tables)
        {
            // --- THE FRONT SIGN: above the door, facing the street -----
            //
            // In the first attempt it was 3.10 m wide and had NO FRAME: on
            // screen it looked like "a glowing plank". A sign is a PLATE - a
            // glowing face sitting inside a dark frame.
            float signX = Paths.DoorX;
            const float tw = 2.30f, th = 0.46f;
            const float tz = -0.055f;
            m.Box(new Vector3(signX, 1.86f, tz),
                  new Vector3(tw, th, 0.10f), p.WallTrim);

            // WHAT GLOWS IS NOT THE WHOLE PLATE BUT ITS FRAME.
            //
            // In the first attempt the whole face of the plate glowed and on
            // screen it read as "a glowing yellow plank" - a lamp, not a
            // sign. On real signs what glows is the LETTERING and the frame;
            // we cannot draw lettering in this pipeline (text in the world
            // needs TextMeshPro, which is not in the project), but a NEON
            // FRAME does the same job: a dark plate with a glowing line
            // around its edge.
            const float tube = 0.05f;
            glow.Box(new Vector3(signX, 1.86f + th * 0.5f - tube, tz - 0.06f),
                     new Vector3(tw - 0.10f, tube, 0.04f), p.Sign);
            glow.Box(new Vector3(signX, 1.86f - th * 0.5f + tube, tz - 0.06f),
                     new Vector3(tw - 0.10f, tube, 0.04f), p.Sign);
            for (int i = 0; i < 2; i++)
                glow.Box(new Vector3(signX + (i == 0 ? -1f : 1f) * (tw * 0.5f - 0.05f),
                                     1.86f, tz - 0.06f),
                         new Vector3(tube, th - 0.10f, 0.04f), p.Sign);

            // A small emblem in the middle: a circle. Whatever the cuisine,
            // there is something in the middle of a sign.
            glow.Prism(10, 0.11f, 0.11f, 0.03f,
                       new Vector3(signX, 1.86f, tz - 0.075f),
                       Quaternion.Euler(-90f, 0f, 0f), p.Sign);

            // A small canopy above the sign: it keeps the light down and
            // TIES the plate to the wall - it is not a plate floating in the
            // air.
            m.Box(new Vector3(signX, 2.13f, -0.14f),
                  new Vector3(tw + 0.24f, 0.07f, 0.30f), p.Accent);
            for (int i = 0; i < 2; i++)
                m.Box(new Vector3(signX + (i == 0 ? -1f : 1f) * (tw * 0.5f - 0.06f),
                                  2.02f, -0.10f),
                      new Vector3(0.06f, 0.24f, 0.06f), p.Accent);

            // --- THE SELF-SERVICE MENU PANELS: ON THE KITCHEN'S BACK WALL ---
            //
            // THE FIRST ATTEMPT WAS WRONG and it was seen in the frame: the
            // panels had been hung ABOVE the counter, at y = 2.0, and at a 34
            // degree view they fell right in front of the cooks - huge,
            // empty, glowing plates on screen. This is the same thing that
            // was learned twice on the pendant lamps (see above): when you
            // look down at a building with NO CEILING, anything hung up
            // COVERS WHAT IS BEHIND IT, and the user had twice said "the
            // ceiling lights should not be visible".
            //
            // The answer was to move them: the panels go ON THE KITCHEN'S
            // BACK WALL. With this camera the back wall stands right ABOVE
            // the counter, so the player still reads them as "the menu above
            // the counter" - but they cover nothing.
            //
            // The panel itself is not an empty plate either: a dark face,
            // glowing lines, a price column on the right. The hall's menu
            // board speaks the same language - from a distance a menu reads
            // exactly like that.
            if (SelfService)
            {
                for (int i = 0; i < RoomPlan.Rooms.Length; i++)
                {
                    RoomPlan.Room r = RoomPlan.Rooms[i];
                    if (r.Name != "Kitchen") continue;
                    if (!RoomPlan.RoomOpen(in r, tables)) break;

                    float pz = r.Z0 + r.D - 0.10f;
                    float pw = Mathf.Min(r.W - 1.4f, 3.60f);
                    const int panels = 3;
                    float bw = pw / panels;

                    for (int k = 0; k < panels; k++)
                    {
                        float px = r.CenterX - pw * 0.5f + bw * (k + 0.5f);

                        m.Box(new Vector3(px, 2.08f, pz),
                              new Vector3(bw - 0.05f, 0.66f, 0.05f), p.WallTrim);
                        m.Box(new Vector3(px, 2.08f, pz - 0.04f),
                              new Vector3(bw - 0.13f, 0.56f, 0.02f),
                              new Color(0.09f, 0.10f, 0.11f));

                        // The top strip is the HEADING (the identity's colour), the
                        // ones below it are lines; a narrow price column on the
                        // far right.
                        glow.Box(new Vector3(px - 0.04f, 2.30f, pz - 0.055f),
                                 new Vector3(bw - 0.24f, 0.05f, 0.02f), p.Sign);
                        for (int j = 0; j < 3; j++)
                        {
                            float y = 2.14f - j * 0.11f;
                            glow.Box(new Vector3(px - 0.10f, y, pz - 0.055f),
                                     new Vector3(bw - 0.36f, 0.035f, 0.02f), p.Lamp);
                            glow.Box(new Vector3(px + bw * 0.5f - 0.20f, y, pz - 0.055f),
                                     new Vector3(0.13f, 0.035f, 0.02f), p.Accent);
                        }
                    }
                    break;
                }
            }

            // --- THE MENU BOARD: on the hall's back wall --------------
            float menuX = float.NaN;
            for (int i = 0; i < RoomPlan.Rooms.Length; i++)
            {
                RoomPlan.Room r = RoomPlan.Rooms[i];
                if (!r.Name.StartsWith("Hall")) continue;
                if (!RoomPlan.RoomOpen(in r, tables)) continue;
                if (Mathf.Abs(r.Z0 + r.D - back) > 0.05f) continue;
                menuX = r.CenterX;
                break;
            }
            if (float.IsNaN(menuX)) return;

            m.Box(new Vector3(menuX, 1.70f, back - 0.02f),
                  new Vector3(1.70f, 1.05f, 0.06f), p.WallTrim);
            m.Box(new Vector3(menuX, 1.70f, back - 0.06f),
                  new Vector3(1.52f, 0.90f, 0.03f), new Color(0.09f, 0.10f, 0.11f));

            // The lines: five light strips. The top one is long (a
            // heading), the ones below are short and leave a "price" column
            // towards the right.
            for (int i = 0; i < 5; i++)
            {
                float y = 2.03f - i * 0.16f;
                float w = i == 0 ? 0.90f : 0.72f;
                m.Box(new Vector3(menuX - 0.28f, y, back - 0.08f),
                      new Vector3(w, i == 0 ? 0.07f : 0.045f, 0.02f),
                      i == 0 ? p.Sign : new Color(0.78f, 0.76f, 0.72f));
                if (i == 0) continue;
                m.Box(new Vector3(menuX + 0.52f, y, back - 0.08f),
                      new Vector3(0.22f, 0.045f, 0.02f), p.Accent);
            }

            // Two frames on the wall: they are in every frame of the reference.
            for (int i = 0; i < 2; i++)
            {
                float x = menuX + (i == 0 ? -1.45f : 1.45f);
                if (x < left + 0.3f || x > right - 0.3f) continue;
                m.Box(new Vector3(x, 1.72f, back - 0.02f),
                      new Vector3(0.52f, 0.68f, 0.05f), p.Wood);
                m.Box(new Vector3(x, 1.72f, back - 0.06f),
                      new Vector3(0.40f, 0.54f, 0.02f), p.Accent);
            }
        }

        /// THE FLOOR PATTERN: planks or tiles.
        ///
        /// The room floors were a single flat colour and at a 34 degree view
        /// that colour read as an EMPTY area - in the reference the floor
        /// itself is half the place. The pattern also gives SCALE: an eye
        /// that knows the width of a plank also understands how big the room
        /// is.
        ///
        /// Turkish: long wooden planks, running towards the street.
        /// Fast food: square tiles, chequered in two tones.
        ///
        /// It all goes into a SINGLE MESH (by colour), so the pattern is
        /// free: hundreds of strips are two draws.
        /// </summary>
        private void FloorPattern(Modeler m, Palette p, int tables)
        {
            // THE PATTERN IS ABOVE THE FLOOR, NOT STUCK TO IT.
            //
            // In the first measurement the BOTTOM face of the pattern slabs
            // was in the same plane as the TOP face of the floor slab (both
            // at y=0) and the result was z-fighting: irregular dark blotches
            // appeared on the floor close up. From a distance it read like
            // "crumbs of shadow" and sent people looking through the shadow
            // settings.
            //
            // It now starts 6 mm higher; the camera looks from 26-35 m, so
            // the difference is invisible but the fight is over.
            for (int i = 0; i < RoomPlan.Rooms.Length; i++)
            {
                RoomPlan.Room r = RoomPlan.Rooms[i];
                if (!RoomPlan.RoomOpen(in r, tables)) continue;

                float w = r.W - 0.10f, d = r.D - 0.10f;

                if (p.Planks)
                {
                    // Planks: 0.42 m wide, running the length of the room.
                    const float plank = 0.42f;
                    int n = Mathf.Max(1, Mathf.RoundToInt(d / plank));
                    float dz = d / n;
                    for (int k = 0; k < n; k++)
                    {
                        float z = r.Z0 + 0.05f + dz * (k + 0.5f);
                        m.Box(new Vector3(r.CenterX, 0.016f, z),
                              new Vector3(w, 0.020f, dz - 0.035f),
                              (k % 2 == 0) ? p.Floor : p.FloorDark);
                    }
                    continue;
                }

                // Tiles: 0.62 m square, chequered.
                const float square = 0.62f;
                int nx = Mathf.Max(1, Mathf.RoundToInt(w / square));
                int nz = Mathf.Max(1, Mathf.RoundToInt(d / square));
                float sx = w / nx, sz = d / nz;
                for (int a = 0; a < nx; a++)
                    for (int b = 0; b < nz; b++)
                        m.Box(new Vector3(r.X0 + 0.05f + sx * (a + 0.5f), 0.016f,
                                          r.Z0 + 0.05f + sz * (b + 0.5f)),
                              new Vector3(sx - 0.03f, 0.020f, sz - 0.03f),
                              ((a + b) % 2 == 0) ? p.Floor : p.FloorDark);
            }
        }

        /// THE SERVICE COUNTER: in front of the kitchen, a hot display
        /// counter.
        ///
        /// It is in all four reference frames and it is the part that makes
        /// a kitchen a "kitchen": a long counter with rows of food trays on
        /// it and a glass screen above. In the game the kitchen was nothing
        /// but stoves and cupboards - so it looked like a STOREROOM rather
        /// than a KITCHEN.
        ///
        /// It is not the colour of the trays but their NUMBER that carries
        /// no information: this is decoration, not tied to the simulation.
        /// Had it been tied, it would have to be rebuilt every frame.
        /// </summary>
        /// <summary>
        /// CONTENT FOR THE SCREENSHOT TOOL.
        ///
        /// PreviewCuisine carries the palette, but the self-service flag is
        /// NOT IN THE PALETTE, IT IS IN THE CONTENT (cuisines/*.json:
        /// selfService), so the tool needed a separate source. Until it had
        /// one, the tool showed something DIFFERENT from what the game shows:
        /// the menu panels, the tills and the drinks machine were not in the
        /// frame at all, and it had been written down that they had been
        /// "added".
        ///
        /// Writing the flag here as "self-service if fastfood" would have
        /// been easy and wrong: that fact belongs to the content, not to the
        /// view.
        /// </summary>
        [System.NonSerialized] public Lokanta.Core.Content.ContentSet PreviewContent;

        /// Is this cuisine self-service? It comes from the content
        /// (docs/51). The chain is the same as CuisineId's: the game first,
        /// then the tool.
        /// </summary>
        private bool SelfService
        {
            get
            {
                if (App != null && App.Content != null) return App.Content.SelfService;
                return PreviewContent != null && PreviewContent.SelfService;
            }
        }

        private void ServiceCounter(Modeler m, Modeler glow, Palette p, int tables)
        {
            for (int i = 0; i < RoomPlan.Rooms.Length; i++)
            {
                RoomPlan.Room r = RoomPlan.Rooms[i];
                if (r.Name != "Kitchen") continue;
                if (!RoomPlan.RoomOpen(in r, tables)) return;

                // Along the room's FRONT edge, without blocking the corridor
                // (Paths.LaneZ).
                float z = r.Z0 + 0.62f;
                float x = r.CenterX;
                float w = r.W - 1.5f;

                Color steel = new Color(0.588f, 0.624f, 0.671f);

                // The body and the counter top.
                m.Box(new Vector3(x, 0.42f, z), new Vector3(w, 0.84f, 0.62f), p.Wood);
                m.Box(new Vector3(x, 0.86f, z), new Vector3(w + 0.08f, 0.06f, 0.70f),
                      steel);

                // The hot trays: in a row on the counter.
                int pans = Mathf.Max(3, Mathf.RoundToInt(w / 0.55f));
                float dx = w / pans;
                for (int k = 0; k < pans; k++)
                {
                    float kx = x - w * 0.5f + dx * (k + 0.5f);
                    m.Box(new Vector3(kx, 0.91f, z), new Vector3(dx - 0.07f, 0.05f, 0.44f),
                          steel);
                    // The food in them: in the identity's accent colour. There is
                    // no need to model the dishes one by one - from a distance a
                    // display counter reads as a row of colours anyway.
                    m.Box(new Vector3(kx, 0.945f, z),
                          new Vector3(dx - 0.13f, 0.03f, 0.36f),
                          k % 2 == 0 ? p.Accent : p.Sign);
                }

                // The glass screen: two thin legs and a glowing plate.
                // Transparent material can be drawn opaque IN A BUILD (docs/37),
                // so instead of glass there is a FAINTLY GLOWING plate - the same
                // silhouette, zero risk.
                //
                // IN SELF-SERVICE THE SCREEN IS NARROW. The tills stand at the
                // ends of the counter and a screen running the full length was
                // COVERING them: in the first frame all that was left of the
                // tills was two white blotches on the counter. A real fast food
                // counter is divided like that too - the hot line in the middle,
                // the tills at the ends.
                float sw = SelfService ? w * 0.60f : w;
                for (int k = 0; k < 2; k++)
                    m.Box(new Vector3(x + (k == 0 ? -1f : 1f) * (sw * 0.5f - 0.05f),
                                      1.12f, z - 0.24f),
                          new Vector3(0.05f, 0.46f, 0.05f), steel);
                m.Box(new Vector3(x, 1.34f, z - 0.10f),
                      new Vector3(sw, 0.05f, 0.34f), steel);

                // --- THE SELF-SERVICE COUNTER -------------------------
                //
                // The same structure is in three of the reference frames and all
                // of them say THE SAME THING: the queue meets the counter here.
                //   1. the menu panels above - glowing, in a row
                //   2. the till points - on the counter
                //   3. the drinks machine - at the end, on its own
                //
                // In a table-service cuisine there is none of this: there the
                // order is taken at the table and the menu is in your hand.
                if (SelfService)
                {
                    // The till points: two blocks on the counter. In the reference
                    // they stand in rows of two or three as well.
                    // The body is STEEL, not dark: a dark body disappeared against
                    // the dark floor and all that was left on screen was a glowing
                    // rectangle floating in the air.
                    for (int k = 0; k < 2; k++)
                    {
                        float kx = x + (k == 0 ? -1f : 1f) * (w * 0.5f - 0.40f);
                        // The body stands on the counter, facing THE GUEST'S SIDE
                        // (small z = towards the hall).
                        m.Box(new Vector3(kx, 1.01f, z - 0.06f),
                              new Vector3(0.36f, 0.24f, 0.30f), p.WallTrim);
                        // The screen: the thing that makes a till a till. Not tilted
                        // slightly back - upright reads better with this camera.
                        m.Box(new Vector3(kx, 1.30f, z + 0.02f),
                              new Vector3(0.30f, 0.34f, 0.05f), p.WallTrim);
                        glow.Box(new Vector3(kx, 1.31f, z - 0.02f),
                                 new Vector3(0.24f, 0.26f, 0.02f), p.Lamp);
                    }

                    // The drinks machine: at the end of the counter, a full-height
                    // cabinet. The most familiar part of self-service.
                    //
                    // Its body is STEEL too (the same reason: a dark body
                    // disappears against a dark floor - in the first frame the
                    // machine read as "a red plate floating in the air").
                    float mx = x + w * 0.5f + 0.34f;
                    m.Box(new Vector3(mx, 0.84f, z), new Vector3(0.52f, 1.68f, 0.56f),
                          steel);
                    m.Box(new Vector3(mx, 1.30f, z - 0.29f),
                          new Vector3(0.44f, 0.66f, 0.03f), p.Accent);
                    // The taps: three small projections.
                    for (int k = 0; k < 3; k++)
                        m.Box(new Vector3(mx - 0.16f + 0.16f * k, 0.90f, z - 0.31f),
                              new Vector3(0.05f, 0.09f, 0.07f), p.WallTrim);
                    // The cup recess: a dark hollow.
                    m.Box(new Vector3(mx, 0.68f, z - 0.30f),
                          new Vector3(0.30f, 0.34f, 0.05f), p.Wall);
                }
                return;
            }
        }

        /// THE TRAY STATION: the visible END of self-service.
        ///
        /// In self-service the guest carries their own tray and leaves it on
        /// the table; the cleaner collects them (docs/51). But where would
        /// they take them AFTER collecting them - there was no such place in
        /// the hall. It is in all three of the frames the user brought: a
        /// waist-high cabinet with a stack of trays on top and a dark mouth
        /// in its front.
        ///
        /// For the same reason NO QUEUE RAIL WAS ADDED: there is a rail in
        /// front of the counter in the reference, but in this simulation
        /// nobody queues at the counter - the guest walks from the door to a
        /// table. An empty queue rail would promise a mechanic that does not
        /// exist.
        ///
        /// It goes in the entrance room, on the LEFT wall: that is the only
        /// empty strip left apart from the front corridor (Paths.LaneZ =
        /// 0.55) and the counter + plant pots behind it (z = 3.2 / 3.3).
        /// </summary>
        private void TrayStation(Modeler m, Modeler glow, Palette p, int tables)
        {
            if (!SelfService) return;

            for (int i = 0; i < RoomPlan.Rooms.Length; i++)
            {
                RoomPlan.Room r = RoomPlan.Rooms[i];
                if (r.Name != "Entry") continue;
                if (!RoomPlan.RoomOpen(in r, tables)) return;

                float x = r.X0 + 0.46f;
                float z = r.Z0 + 1.90f;

                // The body and the top.
                m.Box(new Vector3(x, 0.44f, z), new Vector3(0.62f, 0.88f, 1.20f),
                      p.WallTrim);
                m.Box(new Vector3(x, 0.90f, z), new Vector3(0.68f, 0.06f, 1.26f),
                      p.Metal);

                // The waste mouth: a dark hollow facing forwards. It is the
                // only thing that tells the station apart from a cabinet.
                m.Box(new Vector3(x + 0.30f, 0.60f, z),
                      new Vector3(0.06f, 0.34f, 0.74f), p.Wall);

                // A stack of trays on top: three thin slabs, slightly offset.
                for (int k = 0; k < 3; k++)
                    m.Box(new Vector3(x - 0.02f * k, 0.95f + 0.045f * k, z - 0.34f),
                          new Vector3(0.46f, 0.035f, 0.40f), p.Accent);

                // A small glowing plate: "leave your tray here".
                m.Box(new Vector3(x, 1.42f, z), new Vector3(0.08f, 0.98f, 0.08f),
                      p.WallTrim);
                glow.Box(new Vector3(x - 0.04f, 1.78f, z),
                         new Vector3(0.03f, 0.30f, 0.56f), p.Sign);
                return;
            }
        }

        /// WALL SHELVES: in the kitchen and in the store.
        ///
        /// In the reference the back of the kitchen was full - plates, jars,
        /// spices. Our back wall was completely empty. The shelves do the
        /// same job and say that the place is "somewhere that is worked in".
        /// </summary>
        private void Shelves(Modeler m, Palette p, int tables)
        {
            for (int i = 0; i < RoomPlan.Rooms.Length; i++)
            {
                RoomPlan.Room r = RoomPlan.Rooms[i];
                if (r.Name != "Kitchen" && r.Name != "Store") continue;
                if (!RoomPlan.RoomOpen(in r, tables)) continue;

                float z = r.Z0 + r.D - 0.16f;
                float w = Mathf.Min(r.W - 1.2f, 3.4f);
                if (w < 0.8f) continue;

                for (int k = 0; k < 2; k++)
                {
                    float y = 1.16f + k * 0.44f;
                    m.Box(new Vector3(r.CenterX, y, z),
                          new Vector3(w, 0.05f, 0.30f), p.Wood);

                    // The containers on them: two sizes, at varying spacings.
                    int pans = Mathf.Max(3, Mathf.RoundToInt(w / 0.42f));
                    float dx = w / pans;
                    for (int j = 0; j < pans; j++)
                    {
                        if ((j + k) % 3 == 0) continue;      // a gap is part of the pattern too
                        float h = (j % 2 == 0) ? 0.20f : 0.14f;
                        m.Box(new Vector3(r.CenterX - w * 0.5f + dx * (j + 0.5f),
                                          y + 0.03f + h * 0.5f, z),
                              new Vector3(dx * 0.55f, h, 0.20f),
                              (j % 2 == 0) ? p.Metal : p.WallTrim);
                    }
                }
            }
        }

        /// THE BENCH: seating along the hall's back wall.
        ///
        /// In all four reference frames there are benches along the wall and
        /// half the tables are in front of them. The game's tables have four
        /// chairs and they stay that way (the seating points are the
        /// simulation's business); the bench only fills the foot of the wall
        /// - an empty foot of wall is the most "unfurnished" looking part of
        /// the place.
        /// </summary>
        private void Booths(Modeler m, Palette p, int tables)
        {
            for (int i = 0; i < RoomPlan.Rooms.Length; i++)
            {
                RoomPlan.Room r = RoomPlan.Rooms[i];
                if (!r.Name.StartsWith("Hall")) continue;
                if (!RoomPlan.RoomOpen(in r, tables)) continue;

                // Only in the halls whose BACK wall is on the plot boundary:
                // the inner walls are ways through, and a bench must not block a
                // way through.
                if (r.Z0 + r.D < RoomPlan.PlotD - 0.05f) continue;

                float z = r.Z0 + r.D - 0.34f;
                float w = r.W - 1.0f;
                if (w < 1.0f) continue;

                m.Box(new Vector3(r.CenterX, 0.22f, z),
                      new Vector3(w, 0.10f, 0.52f), p.Accent);       // seat
                m.Box(new Vector3(r.CenterX, 0.11f, z),
                      new Vector3(w - 0.2f, 0.22f, 0.42f), p.WallTrim); // base
                m.Box(new Vector3(r.CenterX, 0.44f, z + 0.22f),
                      new Vector3(w, 0.44f, 0.10f), p.Accent);       // back
            }
        }

        /// THE TERRACE RAILING: between the front of the building and the
        /// pavement.
        ///
        /// In all four reference frames there is a low boundary between the
        /// building and the street - a plant pot, a railing or a fence. Its
        /// job is clear: "this is the front of the restaurant". In ours the
        /// building opened straight onto the pavement.
        /// </summary>
        private void Terrace(Modeler m, Palette p, float left, float right)
        {
            const float z = -0.42f;
            const float y = 0.36f;

            // Along the top rail; the front of the door stays OPEN.
            float gap0 = Paths.DoorX - 0.95f, gap1 = Paths.DoorX + 0.95f;
            RailPost(m, p, left + 0.05f, z);
            RailPost(m, p, right - 0.05f, z);

            Rail(m, p, left + 0.05f, gap0, z, y);
            Rail(m, p, gap1, right - 0.05f, z, y);

            // The posts on either side of the door: the gap has to read as a
            // "gateway", not as a "break".
            RailPost(m, p, gap0, z);
            RailPost(m, p, gap1, z);
        }

        private void RailPost(Modeler m, Palette p, float x, float z)
        {
            m.Box(new Vector3(x, 0.20f, z), new Vector3(0.07f, 0.40f, 0.07f),
                  p.WallTrim);
        }

        private void Rail(Modeler m, Palette p, float x0, float x1, float z, float y)
        {
            float w = x1 - x0;
            if (w < 0.3f) return;
            float center = (x0 + x1) * 0.5f;

            m.Box(new Vector3(center, y, z), new Vector3(w, 0.06f, 0.07f), p.Metal);
            m.Box(new Vector3(center, y - 0.16f, z), new Vector3(w, 0.04f, 0.05f),
                  p.WallTrim);

            // The posts in between: one every 1.6 m.
            int n = Mathf.Max(1, Mathf.RoundToInt(w / 1.6f));
            for (int i = 1; i < n; i++)
                RailPost(m, p, x0 + w * i / n, z);
        }

        /// THE FRONT: THE SHOPFRONT FRAME.
        ///
        /// In all four reference frames the building looks onto the street
        /// through BIG WINDOWS, and the mullions dividing that glass set up
        /// the whole rhythm of the front. Our front was a transparent slab -
        /// it read less like glass than like "there is no wall".
        ///
        /// We are not adding glass (transparent material can be drawn opaque
        /// in a build, docs/37); what is added is the FRAME: the bottom kerb,
        /// the top beam and the mullions between them. The glass is the gap
        /// in between.
        /// </summary>
        private void Storefront(Modeler m, Palette p, float left, float right)
        {
            const float z = -0.02f;

            // THE BEAM IS LOW, OTHERWISE IT COVERS THE HALL.
            //
            // In the first attempt the top beam was at 2.05 m and in the
            // picture it came out as A DARK BAND crossing in front of the
            // hall's front row - it covered the place where the player sees
            // the tables. The room walls are 1.15 m already (so the camera can
            // see inside from 34 degrees); the front must not rise above that
            // line either.
            const float top = 1.34f;

            // The top beam and the bottom kerb.
            float w = right - left;
            m.Box(new Vector3((left + right) * 0.5f, top, z),
                  new Vector3(w, 0.10f, 0.15f), p.WallTrim);
            m.Box(new Vector3((left + right) * 0.5f, 0.22f, z),
                  new Vector3(w, 0.44f, 0.14f), p.Wall);
            m.Box(new Vector3((left + right) * 0.5f, 0.46f, z),
                  new Vector3(w, 0.07f, 0.17f), p.WallTrim);

            // The mullions: one every 1.9 m, with the front of the door clear.
            int n = Mathf.Max(2, Mathf.RoundToInt(w / 1.9f));
            for (int i = 0; i <= n; i++)
            {
                float x = left + w * i / n;
                if (Mathf.Abs(x - Paths.DoorX) < 0.85f) continue;
                m.Box(new Vector3(x, 0.90f, z), new Vector3(0.09f, 0.90f, 0.12f),
                      p.WallTrim);
            }

            // Either side of the door: a thicker mullion - THIS is the way in.
            for (int i = 0; i < 2; i++)
                m.Box(new Vector3(Paths.DoorX + (i == 0 ? -1f : 1f) * 0.78f,
                                  0.90f, z),
                      new Vector3(0.13f, 0.90f, 0.14f), p.Accent);
        }

        /// TERRACE SEATING: two small tables on the pavement.
        ///
        /// In the first and third reference frames there are guests sitting
        /// outside. In ours THE SIMULATION does not serve outside - so the
        /// tables here are EMPTY and will stay that way: decoration, not game
        /// state. An empty terrace table still says "this is a restaurant"; a
        /// full one would be telling a lie.
        /// </summary>
        private void PatioSeats(Modeler m, Palette p, float left, float right)
        {
            _patio.Clear();
            float[] spots = { Paths.DoorX - 2.6f, Paths.DoorX + 2.6f };
            foreach (float x in spots)
            {
                if (x < left + 0.6f || x > right - 0.6f) continue;
                PatioSet(m, p, x, -0.95f);
                // So that the pedestrians walk round them: a table is an obstacle
                // like a post.
                _patio.Add(new Vector3(x, 0f, -0.95f));
            }
        }

        /// <summary>Where the terrace tables are. An obstacle for pedestrian movement.</summary>
        private readonly System.Collections.Generic.List<Vector3> _patio =
            new System.Collections.Generic.List<Vector3>();

        private void PatioSet(Modeler m, Palette p, float x, float z)
        {
            // The table: a round top, a single leg, a foot.
            m.Prism(10, 0.36f, 0.36f, 0.05f, new Vector3(x, 0.62f, z),
                    Quaternion.identity, p.Wood);
            m.Prism(6, 0.05f, 0.05f, 0.62f, new Vector3(x, 0f, z),
                    Quaternion.identity, p.WallTrim);
            m.Prism(8, 0.20f, 0.16f, 0.04f, new Vector3(x, 0f, z),
                    Quaternion.identity, p.WallTrim);

            // Two chairs: opposite each other, facing the table.
            PatioChair(m, p, x - 0.62f, z, 90f);
            PatioChair(m, p, x + 0.62f, z, -90f);
        }

        private void PatioChair(Modeler m, Palette p, float x, float z, float angle)
        {
            Quaternion r = Quaternion.Euler(0f, angle, 0f);
            Vector3 c = new Vector3(x, 0f, z);

            // The seat and the back, in the chair's own axis.
            m.BoxAt(c + r * new Vector3(0f, 0.40f, 0f),
                    new Vector3(0.36f, 0.05f, 0.36f), r, p.Accent);
            m.BoxAt(c + r * new Vector3(0f, 0.60f, -0.16f),
                    new Vector3(0.36f, 0.36f, 0.05f), r, p.WallTrim);
            for (int i = 0; i < 4; i++)
            {
                float ax = (i % 2 == 0) ? -0.14f : 0.14f;
                float az = (i < 2) ? -0.14f : 0.14f;
                m.BoxAt(c + r * new Vector3(ax, 0.19f, az),
                        new Vector3(0.045f, 0.38f, 0.045f), r, p.WallTrim);
            }
        }

        /// THE RUG: in the Turkish cuisine only.
        ///
        /// In the second reference frame there is a kilim under the tables
        /// and that single piece is one of the first things that makes the
        /// place a kebab house. On the fast food side there is NO rug - that
        /// place wants a floor that can be wiped; the difference in identity
        /// is exactly this.
        /// </summary>
        private void Rugs(Modeler m, Palette p, int tables)
        {
            if (!p.HasRug) return;

            for (int i = 0; i < RoomPlan.Rooms.Length; i++)
            {
                RoomPlan.Room r = RoomPlan.Rooms[i];
                if (!r.Name.StartsWith("Hall")) continue;
                if (!RoomPlan.RoomOpen(in r, tables)) continue;

                float w = r.W - 0.9f, d = r.D - 0.9f;
                if (w < 0.5f || d < 0.5f) continue;

                // 2 cm above the floor: so that there is no z-fighting.
                m.Box(new Vector3(r.CenterX, 0.02f, r.Z0 + r.D * 0.5f),
                      new Vector3(w, 0.02f, d), p.Rug);
                // An inner border: a single-colour rectangle read as "a painted
                // floor" rather than a "rug".
                m.Box(new Vector3(r.CenterX, 0.025f, r.Z0 + r.D * 0.5f),
                      new Vector3(w - 0.34f, 0.02f, d - 0.34f), p.Accent);
                m.Box(new Vector3(r.CenterX, 0.03f, r.Z0 + r.D * 0.5f),
                      new Vector3(w - 0.52f, 0.02f, d - 0.52f), p.Rug);
            }
        }
    }
}
