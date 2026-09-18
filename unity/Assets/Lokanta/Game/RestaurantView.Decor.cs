using System.Collections.Generic;
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

            /// <summary>The splashback tile behind the kitchen line.</summary>
            public Color Splash;

            /// <summary>Its joints.</summary>
            public Color SplashJoint;
        }

        /// <summary>
        /// THE CUISINE'S PALETTE.
        ///
        /// Fast food: dark brick, NEON RED light, steel metal. The first
        /// frame of the reference is exactly this - a red sign, a stainless
        /// counter, a black-and-red floor.
        ///
        /// Turkish: warm plaster and wood on the wall, COPPER light, a rug
        /// on the floor. The second frame of the reference: brass lanterns, a
        /// kilim, wooden lattice.
        ///
        /// ---------------------------------------------------------------
        /// BOTH PALETTES WERE LIFTED AND SOFTENED, AND IT WAS MEASURED FIRST.
        ///
        /// The user's words: "the floor colour and the back wall colour are a
        /// bit tiring to the eye", and "one colour may be what makes the game
        /// boring - we could widen the palette a little, and softer colours
        /// would be kinder to the eye".
        ///
        /// Sampling the noon renders rather than judging by eye
        /// (render/hall_*_noon.png, 12 x 12 px patches) said the same thing
        /// in numbers - the whole picture lived in the bottom third of the
        /// value range:
        ///
        ///     surface        fast food     Turkish
        ///     hall floor       29%           14%
        ///     back wall        16%           19%
        ///     table top        24%           32%
        ///     pavement         43%           43%
        ///     sky              28%           27%
        ///
        /// Nothing indoors reached 35%. The brightest thing in the frame was
        /// a flat pavement, and the sky was DARKER than the ground it stood
        /// over. A picture with no mid-tones has nothing for the eye to rest
        /// on, and it has to be read by squinting - which is the tiring part.
        ///
        /// THE TWO BIGGEST SURFACES CARRY MOST OF IT, so the floor and the
        /// wall move the furthest. Everything else follows one rule, applied
        /// to every colour in both palettes:
        ///
        ///   LIFT THE VALUE, DROP THE CHROMA, KEEP THE HUE.
        ///
        /// The hue is the identity and it does not move: the Turkish room
        /// stays warm and coppery, the fast food room stays cool with a red.
        /// What changes is that they stop being dark and stop being pure. A
        /// signal red (0.85/0.24/0.20) is a colour for a warning light, not
        /// for a wall a player looks at for sixty days; the same red at
        /// 0.81/0.40/0.33 still reads as the identity and stops shouting.
        ///
        /// AND EACH CUISINE GAINS A THIRD HUE, because two colours plus grey
        /// is what "boring" looks like from the inside. Turkish had warm
        /// brown and copper: the greenery and the plaster now make a third
        /// and a fourth note. Fast food had slate and red: the laminate is
        /// warm cream now, so its cool room has something warm in it.
        /// </summary>
        public static Palette Pal(string cuisine)
        {
            if (cuisine == "turk")
            {
                return new Palette
                {
                    // THE WALL ABOVE THE WAINSCOT IS PLASTER, AND PLASTER IS
                    // PALE. It was 0.29/0.20/0.14 - a dark brown that made the
                    // room read as a cellar. A traditional lokanta has timber
                    // to chair height and light plaster above it; that is the
                    // contrast the wainscot exists for, and a dark wall threw
                    // it away.
                    Wall = new Color(0.616f, 0.529f, 0.443f),
                    WallTrim = new Color(0.420f, 0.337f, 0.267f),
                    Wood = new Color(0.561f, 0.416f, 0.286f),
                    Metal = new Color(0.745f, 0.616f, 0.400f),
                    Accent = new Color(0.816f, 0.639f, 0.404f),
                    Sign = new Color(1.000f, 0.780f, 0.447f),
                    Lamp = new Color(1.000f, 0.867f, 0.651f),
                    // Burgundy -> terracotta. The old seat was nearly black at
                    // this light level and every chair in the hall is one.
                    Seat = new Color(0.580f, 0.345f, 0.325f),
                    Plant = new Color(0.404f, 0.549f, 0.392f),
                    // A FADED KILIM, NOT A NEW ONE. The rug covers almost the
                    // whole of every dining room, so it IS the hall floor -
                    // and a saturated red-brown over that much of the frame is
                    // the single heaviest thing in the picture.
                    Rug = new Color(0.741f, 0.569f, 0.494f),
                    // THE FLOOR, WHICH IS THE BIGGEST SURFACE IN THE FRAME.
                    // 0.40/0.29/0.20 measured 14% on screen - a dark red-brown
                    // that swallowed the furniture standing on it. A pale oak
                    // plank with most of the red taken out: the warmth is in
                    // the hue, not in the darkness.
                    Floor = new Color(0.604f, 0.518f, 0.427f),
                    FloorDark = new Color(0.541f, 0.451f, 0.365f),
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
                    WainscotColor = new Color(0.486f, 0.361f, 0.251f),
                    WainscotCap = new Color(0.745f, 0.600f, 0.376f),
                    SeamStep = 0.85f,
                    // The outside is warm too: a neighbourhood, dusty midday light.
                    Sky = new Color(0.86f, 0.74f, 0.56f),
                    // A warm cream tile with a sandy joint: the glazed tile of
                    // a neighbourhood kitchen, not a laboratory.
                    Splash = new Color(0.871f, 0.824f, 0.741f),
                    SplashJoint = new Color(0.678f, 0.616f, 0.518f),
                };
            }

            return new Palette
            {
                // 0.17/0.18/0.21 measured 16% on screen: not a dark wall,
                // a BLACK one. The identity here is "wipe-clean and cool",
                // which a soft blue-grey says as well as a near-black does -
                // and a near-black says it by removing the room.
                // WARM, AND THAT IS WHAT MAKES THE STAINLESS READ.
                //
                // The first lift took this to a light COOL grey and the fast
                // food kitchen came back from the render washed out, while
                // the Turkish one - identical equipment - read perfectly.
                // The difference was behind the line, not in it: steel is a
                // cool grey, and a cool grey wall behind a cool grey
                // appliance is one surface. The Turkish kitchen works because
                // its plaster and timber give the metal something to sit
                // against.
                //
                // So the fast food wall keeps its lightness and turns warm.
                // The room is still the cool one - that lives in the floor,
                // the steel band and the tables, against a coral accent - and
                // now its equipment has an edge.
                Wall = new Color(0.561f, 0.541f, 0.514f),
                WallTrim = new Color(0.361f, 0.349f, 0.329f),
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
                // Warmer as well as lighter: this is the cool room's THIRD
                // NOTE, the one thing in it that is not slate or red.
                Wood = new Color(0.792f, 0.725f, 0.627f),
                Metal = new Color(0.694f, 0.722f, 0.765f),
                // A SIGNAL RED IS FOR A WARNING LIGHT. 0.85/0.24/0.20 is as
                // saturated as the colour gets, and it is on the sign, the
                // counter, the cushions and the boards - every one of them in
                // shot at once, for sixty days. The same hue at a third less
                // chroma still reads as the identity from across the room.
                Accent = new Color(0.816f, 0.400f, 0.337f),
                Sign = new Color(1.000f, 0.482f, 0.404f),
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
                Seat = new Color(0.792f, 0.408f, 0.353f),
                Plant = new Color(0.424f, 0.576f, 0.416f),
                Rug = new Color(0.478f, 0.506f, 0.545f),
                // THE FLOOR WAS LIGHTENED: 0.32/0.24 -> 0.42/0.33.
                //
                // The dark tile looked "shot at night" close up and swallowed
                // the dark furniture standing on it. In the reference's fast
                // food frame the floor is a MID tone; what gives the identity is
                // not the darkness of the floor but the neon red and the steel.
                // AND LIGHTENED AGAIN, for the same reason and with a
                // measurement this time: 0.42/0.44/0.47 came out at 29% on
                // screen, because what is authored is not what is lit. It is
                // also warmed slightly - a cold grey floor under a cold grey
                // wall was two thirds of the frame in one hue.
                // AND ONCE MORE, WARMER. At 0.62 the tiles still came back
                // from the render as night tarmac: the ambient in this
                // pipeline is slightly blue, so a neutral floor under it is a
                // blue floor, and blue plus dark is the one combination that
                // reads as "outdoors, at night". A light warm grey is also
                // the honest colour for a wipe-clean tile.
                Floor = new Color(0.745f, 0.737f, 0.718f),
                FloorDark = new Color(0.663f, 0.655f, 0.635f),
                HasRug = false,
                Planks = false,

                // PANEL SEAMS + A STEEL BAND.
                //
                // The surface of the fast food hall has to be "wipe-clean":
                // laminate panel joints and a stainless strip at counter height.
                // NO wainscot - that is the language of a different place.
                Wainscot = 1.15f,
                WainscotColor = new Color(0.443f, 0.427f, 0.404f),
                WainscotCap = new Color(0.706f, 0.733f, 0.776f),
                SeamStep = 1.15f,
                // The outside is cold and urban: a main road, tarmac, glass.
                Sky = new Color(0.66f, 0.75f, 0.88f),
                // A cool white tile with a grey joint. This is the wipe-clean
                // surface the fast food identity is built on, and it is the
                // one place the room may be colder than its wall.
                Splash = new Color(0.878f, 0.886f, 0.882f),
                SplashJoint = new Color(0.639f, 0.659f, 0.667f),
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

            // THE CROWD IS A TEXTURE SWAP, NOT A TINT.
            //
            // Everything else in this method is a flat _BaseColor material, so
            // a colour copy is the whole job. The figures are not: all twelve
            // of them share ONE material and ONE colormap, and the shirt, the
            // trousers and the FACE are different rectangles of that single
            // texture. Multiplying _BaseColor would repaint the skin, and
            // docs/25 is explicit that identity here comes from the
            // environment, the light, the silhouette and the clothes and never
            // from a face.
            //
            // So the swap happens a step earlier, in tools/art/gen_crowd.py,
            // which recolours the clothing swatches and asserts the four skin
            // tones come through byte-identical. Here it is one copy with a
            // different _BaseMap - and because it is still ONE material for
            // every figure in the scene, the batching does not change.
            if (CharacterMaterial != null && src == CharacterMaterial)
            {
                Texture2D map = CuisineId == "turk" ? CrowdMapTurk : CrowdMapFastfood;
                Material dressed = src;
                if (map == null)
                {
                    // NOT SILENT: with the texture missing the crowd simply
                    // wears the pack's own colours, which is exactly what the
                    // scene looked like before this existed - a failure with
                    // no symptom. Autopilot's CrowdUndressed check is the
                    // measurement; this is the reason printed next to it.
                    Debug.LogWarning("RestaurantView: no crowd colormap for '"
                                     + CuisineId + "' - the figures keep the "
                                     + "pack's own clothes (run "
                                     + "tools/art/gen_crowd.py and rebuild the "
                                     + "scene)");
                }
                else
                {
                    dressed = new Material(src);
                    dressed.name = src.name + "_" + CuisineId;
                    dressed.SetTexture(BaseMapId, map);
                }
                _tinted[src] = dressed;
                return dressed;
            }

            Palette p = Pal(CuisineId);
            string srcName = src.name.ToLowerInvariant();
            Color? tint = null;

            if (srcName.Contains("carpet")) tint = p.Seat;
            // 0.72 -> 0.82. The dark wood is the chair frame, and there are
            // four chairs to every table: once the floor and the wall had
            // been lifted, the chairs were the heaviest dark mass left in the
            // room. The two woods still read as two woods at 0.82 - the point
            // of the darker one is that the frame is not the table top, not
            // that it is nearly black.
            else if (srcName.Contains("wooddark")) tint = p.Wood * 0.82f;
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
            Paving(m);
            Backdrop(m, p, left, right, back);
            SkyBands(left, right, back);
            Planters(m, p, left, right);
            Splashback(m, p, tables);
            KitchenHood(m, p, tables);
            ServiceCounter(m, glow, p, tables);
            TrayStation(m, glow, p, tables);
            Shelves(m, p, tables);
            Booths(m, p, tables);
            Boards(m, glow, p, left, right, back, tables);
            Rugs(m, p, tables);
            Terrace(m, p, left, right);
            Storefront(m, p, left, right);
            TerraceSeats(m, p, left, right);

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

        private GameObject _decor, _decorGlow, _skyline;

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
        /// THE SKY, AND THE THIRD ANSWER TO THE SAME COMPLAINT.
        ///
        /// There was a row of distant buildings here. The user reported it as
        /// "shadows behind the restaurant that I do not understand" twice, and
        /// each time a different cause was found and fixed - first it was
        /// receiving the near scene's shadows, then it was lit where it should
        /// have been unlit. The third report settled it: "those shadow-like
        /// parts at the top of the screen spoil the look, let us remove them or
        /// make a nicer background".
        ///
        /// Two fixes that each answered a real cause and still left the user
        /// looking at something they could not name is the signal to stop
        /// fixing and change the thing.
        ///
        /// Silhouettes are gone. What replaces them is what docs/58 §10
        /// proposed for the flat backdrop in the first place, and it is a
        /// better answer than shapes: a GRADIENT, lighter at the horizon and
        /// deeper above, with nothing in it that can be mistaken for an object.
        ///
        /// IT FOLLOWS THE DAY. The bands are painted from the same colour
        /// DayLight gives the camera's clear (DayLight.Apply calls TintSky), so
        /// morning, noon, evening and night carry it without a second curve to
        /// keep in step - which is the drift this project has been bitten by
        /// five times.
        ///
        /// Eight bands, unlit, no shadow, no collider. It stands at the far
        /// side of the plot and CameraFit.OpenBounds is built from the open
        /// rooms and the street only, so it costs nothing in framing.
        /// </summary>
        private void SkyBands(float left, float right, float back)
        {
            _skyBands.Clear();
            if (_badgeMat == null) return;

            // THE GRADIENT'S BRIGHT END WAS BEHIND THE BUILDING.
            //
            // The stack started at y = -1.5 and the bright band is the bottom
            // one, so the only part of the gradient the camera could ever see
            // was its DARK half - which is why a frame sampled at noon found
            // 44% of its pixels in one bucket at 25% value. The sky was not
            // flat because it had no gradient; it was flat because the half
            // with the gradient in it was hidden behind a roof.
            //
            // It starts at the roof line now. Same eight bands, same cost.
            const int Bands = 8;
            const float Height = 22f;
            const float Base = 3.0f;
            const float Width = 90f;

            float cx = (left + right) * 0.5f;
            float z = back + 13f;

            GameObject root = new GameObject("Sky");
            root.transform.SetParent(transform, false);
            _skyline = root;

            for (int i = 0; i < Bands; i++)
            {
                GameObject band = GameObject.CreatePrimitive(PrimitiveType.Cube);
                band.name = "SkyBand" + i;
                band.transform.SetParent(root.transform, false);
                band.transform.localPosition =
                    new Vector3(cx, Base + (i + 0.5f) * (Height / Bands), z);
                band.transform.localScale =
                    new Vector3(Width, Height / Bands + 0.02f, 0.2f);

                Collider col = band.GetComponent<Collider>();
                if (col != null)
                {
                    if (Application.isPlaying) Destroy(col); else DestroyImmediate(col);
                }

                Renderer r = band.GetComponent<Renderer>();
                r.sharedMaterial = _badgeMat;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.receiveShadows = false;
                _skyBands.Add(r);
            }

            TintSky(new Color(0.145f, 0.195f, 0.255f));
        }

        private readonly System.Collections.Generic.List<Renderer> _skyBands =
            new System.Collections.Generic.List<Renderer>();

        /// <summary>
        /// Paints the sky from the colour the camera is clearing to.
        ///
        /// Lighter at the horizon and deeper above - which is the way round a
        /// real sky is, and the way round the first attempt got wrong when it
        /// made the distant buildings DARKER than the sky and they read as a
        /// heavy near object pressing on the roofline.
        /// </summary>
        public void TintSky(Color sky)
        {
            if (_skyBands.Count == 0) return;
            if (_block == null) _block = new MaterialPropertyBlock();

            for (int i = 0; i < _skyBands.Count; i++)
            {
                if (_skyBands[i] == null) continue;
                // A WIDER GRADIENT, BECAUSE THE BACKGROUND IS 40% OF THE
                // FRAME AND IT WAS FLAT.
                //
                // Sampling a noon frame: 44% of every pixel fell in ONE
                // colour bucket at 25% value - the field around the building.
                // docs/58 darkened that field on purpose, and rightly: a
                // bright sky outshone the subject and the eye slid off the
                // building. But dark and FLAT are different problems, and
                // only the first one was solved. 1.30 -> 0.66 is a range of
                // half a stop over the whole sky; at these values that is
                // eight bands nobody can tell apart.
                //
                // 1.85 -> 0.50 keeps the same average - so the figure/ground
                // measurement docs/58 took is not spent - and gives the eye
                // somewhere to travel: bright at the horizon behind the roof
                // line, deep at the top of the frame, which is what a sky
                // actually does.
                float t = _skyBands.Count == 1 ? 0f : i / (float)(_skyBands.Count - 1);
                float k = Mathf.Lerp(1.85f, 0.50f, t);
                _skyBands[i].GetPropertyBlock(_block);
                _block.SetColor(BaseColorId,
                                new Color(sky.r * k, sky.g * k, sky.b * k, 1f));
                _skyBands[i].SetPropertyBlock(_block);
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
            List<float> xs = PlanterSpots(left, right);
            for (int i = 0; i < xs.Count; i++) Planter(m, p, xs[i], TerraceZ);
        }

        /// <summary>
        /// Where the planters stand along the front.
        ///
        /// SHARED WITH THE RAILING, and that is the point. The pots used to
        /// sit at z = -0.26 and the rail runs at -0.42; the pot is 0.46 deep,
        /// so the rail went straight through every one of them. The user saw
        /// it: "there is a fence going through the plants or pots in front of
        /// the restaurant".
        ///
        /// There is nowhere to move them to. The band between the shopfront
        /// (front face -0.10) and the rail (back face -0.46) is 0.36 m and the
        /// pot is 0.46; in front of the rail is the pavement, where the
        /// pedestrian lanes run. Both were tried on paper and both leave the
        /// pot clipping something.
        ///
        /// So the pots stay ON the rail line and the RAIL BREAKS AROUND THEM,
        /// exactly as it already breaks around the door. A planter set into a
        /// railing run is a real streetscape detail rather than a compromise -
        /// it reads as a pier, and it gives the long front a rhythm it did not
        /// have.
        ///
        /// The two ends are left out: the corner posts stand there.
        /// </summary>
        private static List<float> PlanterSpots(float left, float right)
        {
            List<float> xs = new List<float>();
            int count = Mathf.Max(2, Mathf.RoundToInt((right - left) / 3.2f));
            float gap = (right - left) / count;

            for (int i = 0; i <= count; i++)
            {
                float x = left + gap * i;
                // Not blocking the front of the door: the entrance is at Paths.DoorX.
                if (Mathf.Abs(x - Paths.DoorX) < 1.3f) continue;
                // Nor standing inside a corner post.
                if (x - left < 0.45f || right - x < 0.45f) continue;
                // NOR ON A BENCH. Both stand on the terrace and they overlap
                // in z; see BenchSpots for why this is one list and not two.
                bool onSeat = false;
                List<float> seats = BenchSpots(left, right);
                for (int k = 0; k < seats.Count && !onSeat; k++)
                    onSeat = Mathf.Abs(x - seats[k]) < BenchClear;
                if (onSeat) continue;
                xs.Add(x);
            }
            return xs;
        }

        /// <summary>Half the width of the break a planter takes out of the rail.</summary>
        private const float PlanterHalf = 0.27f;

        /// <summary>The line the railing and the planters share.</summary>
        private const float TerraceZ = -0.42f;

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

        /// <summary>
        /// THE TILED WALL BEHIND THE KITCHEN LINE.
        ///
        /// docs/60 finished with the kitchen still the most monochrome room
        /// in the building, and named its own cheapest next step: the
        /// equipment is stainless in every restaurant, so the thing that can
        /// separate the two kitchens is what stands BEHIND it.
        ///
        /// It is also the answer to the defect that document ended on. The
        /// Turkish kitchen read beautifully and the fast food one came back
        /// washed out with identical equipment, because a cool grey appliance
        /// in front of a cool grey wall is one surface. A tiled splashback
        /// gives the steel an edge in both rooms, and it is the surface every
        /// real kitchen has for exactly the reason this one needs it: the
        /// wall behind a cooking line is not the wall of the room.
        ///
        /// It costs about 300 triangles for both rooms and no new renderer -
        /// the Modeler merges it into the Decor mesh by colour - because the
        /// joints are LINES rather than tiles. At 34 degrees from twenty
        /// metres a grid of thin strips and a grid of separate squares are
        /// the same picture, and one of them is a hundred and sixty boxes.
        ///
        /// It stops under the hood mouth (1.44 m, KitchenHood) so the two
        /// never argue about the same band of wall.
        ///
        /// THE KITCHEN ONLY, AND THE FIRST VERSION DID THE WASH ROOM TOO.
        ///
        /// It came back floating in mid-air over the sinks, and the reason is
        /// a number in this file: the interior walls are 1.15 m (WallHeight),
        /// because a full-height wall would hide the front row from a 34
        /// degree camera. The kitchen's back wall is the BUILDING's, at
        /// z = PlotD, and it is tall; the wash room's is the partition it
        /// shares with the kitchen - 1.15 m, with the doorway the user asked
        /// for cut through it. A tiled band from 0.90 to 1.62 on that wall is
        /// half a metre of tiles with nothing behind them and a doorway
        /// running under them.
        ///
        /// Tiling it properly would mean capping at 1.15 and breaking around
        /// the door, for a strip 25 cm tall that is mostly hidden behind the
        /// sinks. The sinks read perfectly well against the plain partition.
        /// </summary>
        private void Splashback(Modeler m, Palette p, int tables)
        {
            for (int i = 0; i < RoomPlan.Rooms.Length; i++)
            {
                RoomPlan.Room r = RoomPlan.Rooms[i];
                if (r.Name != "Kitchen") continue;
                if (!RoomPlan.RoomOpen(in r, tables)) continue;

                float z = r.Z0 + r.D - 0.035f;
                float w = r.W - 0.30f;
                if (w < 0.8f) continue;

                // From just above a 0.92 m worktop to just under whatever is
                // above it.
                const float y0 = 0.90f;
                const float y1 = 1.42f;
                const float h = y1 - y0;

                m.Box(new Vector3(r.CenterX, y0 + h * 0.5f, z),
                      new Vector3(w, h, 0.04f), p.Splash);

                int rows = Mathf.Max(2, Mathf.RoundToInt(h / 0.26f));
                for (int k = 1; k < rows; k++)
                    m.Box(new Vector3(r.CenterX, y0 + h * k / rows, z - 0.012f),
                          new Vector3(w, 0.014f, 0.02f), p.SplashJoint);

                int cols = Mathf.Max(3, Mathf.RoundToInt(w / 0.52f));
                for (int k = 1; k < cols; k++)
                    m.Box(new Vector3(r.CenterX - w * 0.5f + w * k / cols,
                                      y0 + h * 0.5f, z - 0.012f),
                          new Vector3(0.014f, h, 0.02f), p.SplashJoint);
            }
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

                // MEASURED OFF THE LINE, not off the room. `r.W - 1.9` was
                // three stove prefabs' worth of wall; the line is now whatever
                // the cuisine owns and it is a different width in each.
                if (HoodTo - HoodFrom < 0.5f) return;
                float w = (HoodTo - HoodFrom) + 0.16f;
                float x = (HoodFrom + HoodTo) * 0.5f;
                float z = r.Z0 + r.D - 0.78f;

                // IT HANGS ABOVE THE TALLEST THING IN THE LINE. The mouth used
                // to sit at 1.44 m and an oven is 1.52 - the hood was cutting
                // through the oven's top. A real extraction hood clears the
                // equipment by about a hand's width.
                float mouth = Mathf.Max(1.44f, HoodTop + 0.12f);

                // The body: a funnel widening downwards.
                // THE HOOD IS STEEL IN EVERY CUISINE.
                //
                // In the first attempt it took the identity's metal colour and
                // stood in the Turkish kitchen as a GOLD box. A hood is not
                // decoration but equipment; it is stainless in every restaurant.
                Color steel = new Color(0.576f, 0.612f, 0.659f);
                m.Box(new Vector3(x, mouth + 0.19f, z), new Vector3(w, 0.32f, 0.78f),
                      steel);
                m.Box(new Vector3(x, mouth + 0.42f, z),
                      new Vector3(w * 0.45f, 0.26f, 0.44f), steel);
                // The lower mouth: a dark strip, the mouth of the funnel.
                m.Box(new Vector3(x, mouth, z), new Vector3(w - 0.12f, 0.06f, 0.70f),
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

        /// <summary>
        /// THE PAVEMENT'S SLAB JOINTS.
        ///
        /// The pavement is the last large flat surface in the frame. It runs
        /// the full width of the plot and two metres deep, it fills the bottom
        /// third of the picture, and it was a single unbroken colour - which
        /// the floors inside stopped being a long time ago, for the reason
        /// FloorPattern gives: at 34 degrees a flat colour reads as an EMPTY
        /// AREA, and a pattern also gives SCALE, because an eye that knows the
        /// size of a slab knows the size of the street.
        ///
        /// Joints, not slabs - the same trick as the kitchen splashback. A
        /// grid of thin lines and a grid of separate rectangles are the same
        /// picture at this distance, and this one is 23 boxes instead of 66.
        ///
        /// It is NOT in the palette: the pavement is municipal, the same
        /// outside both restaurants, and the cuisine stops at the door. The
        /// street's own colours live in BuildStreet for the same reason.
        /// </summary>
        private void Paving(Modeler m)
        {
            // The slab is where BuildStreet puts it. These two numbers are
            // the pavement's own edges and they are written down twice, which
            // this project knows the price of - but the alternative is
            // publishing a field from the street builder for a decoration,
            // and the street builder runs AFTER the decor.
            const float z0 = -0.02f, z1 = -2.02f;
            float x0 = -1.2f, x1 = RoomPlan.PlotW + 1.2f;

            // A municipal slab, and the joint a shade darker than the stone
            // rather than black: a dark line at this scale reads as a CRACK.
            //
            // AND BOTH OF THESE WERE TOO STRONG ON THE FIRST TRY. 0.92 m
            // slabs with a 2 cm joint at 0.53 came back as graph paper: at
            // this camera the columns are dense enough to read as a wire grid
            // rather than as stone, and the eye goes to the grid instead of
            // to the restaurant standing on it. A pavement is a SURFACE that
            // happens to have joints, not a drawing of joints.
            //
            // 1.36 m slabs, two courses, a 1.6 cm joint one shade off the
            // stone. The purpose is to stop the area reading as empty, and
            // that is all it has to do.
            Color joint = new Color(0.565f, 0.545f, 0.514f);
            const float width = 0.016f;

            // Across: two courses over two metres.
            const int rows = 2;
            for (int k = 1; k < rows; k++)
            {
                float z = z0 + (z1 - z0) * k / rows;
                m.Box(new Vector3((x0 + x1) * 0.5f, 0.008f, z),
                      new Vector3(x1 - x0, 0.016f, width), joint);
            }

            // Along: a slab wider than a person, which is what makes a
            // 1.10 m figure read as somebody walking on paving.
            int cols = Mathf.Max(4, Mathf.RoundToInt((x1 - x0) / 1.36f));
            for (int k = 1; k < cols; k++)
            {
                float x = x0 + (x1 - x0) * k / cols;
                m.Box(new Vector3(x, 0.008f, (z0 + z1) * 0.5f),
                      new Vector3(width, 0.016f, z1 - z0), joint);
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
        /// empty strip left apart from the front corridor (Paths.LaneZ) and
        /// the counter + plant pots behind it (z = 3.2 / 3.3).
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
            const float z = TerraceZ;
            const float y = 0.36f;

            // WHAT THE RAIL HAS TO MISS: the door, and every planter.
            //
            // The rail used to be two long runs with a gap at the door, and it
            // was drawn without reference to the planters - which stand on the
            // same line and are 0.54 m across, so it went through all of them.
            // The breaks are built from the SAME list the pots are placed
            // from (PlanterSpots); two lists would drift apart the first time
            // the spacing changed.
            List<float> breaks = new List<float>();
            breaks.Add(Paths.DoorX - 0.95f);
            breaks.Add(Paths.DoorX + 0.95f);

            List<float> pots = PlanterSpots(left, right);
            for (int i = 0; i < pots.Count; i++)
            {
                breaks.Add(pots[i] - PlanterHalf);
                breaks.Add(pots[i] + PlanterHalf);
            }
            breaks.Sort();

            RailPost(m, p, left + 0.05f, z);
            RailPost(m, p, right - 0.05f, z);

            // The posts on either side of the door: the gap has to read as a
            // "gateway", not as a "break". The planters need no post - the pot
            // IS the pier.
            RailPost(m, p, Paths.DoorX - 0.95f, z);
            RailPost(m, p, Paths.DoorX + 0.95f, z);

            // The runs between the breaks. Rail() itself drops anything under
            // 0.3 m, so a planter that lands next to the door gap simply
            // merges with it instead of leaving a stub.
            float from = left + 0.05f;
            for (int i = 0; i < breaks.Count; i += 2)
            {
                Rail(m, p, from, breaks[i], z, y);
                from = breaks[i + 1];
            }
            Rail(m, p, from, right - 0.05f, z, y);
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

        /// TERRACE SEATING: two benches, INSIDE the rail.
        ///
        /// In the first and third reference frames there are guests sitting
        /// outside. In ours THE SIMULATION does not serve outside - so the
        /// seats here are EMPTY and will stay that way: decoration, not game
        /// state. An empty terrace seat still says "this is a restaurant"; a
        /// full one would be telling a lie.
        ///
        /// THEY USED TO BE ROUND CAFE TABLES ON THE PAVEMENT, at z -0.95,
        /// which is where the pedestrians walk. That was handled by making
        /// each table an obstacle to push against - and a single 0.40 m push
        /// radius around the table's CENTRE is smaller than the table (0.36)
        /// plus a body (0.29), so the passers-by stood inside them anyway
        /// (render/zoom/before-pedestrians-in-the-terrace.png).
        ///
        /// The deeper problem was that the set is 0.72 m deep and the
        /// pavement is not wide enough for a terrace AND two pedestrian
        /// lanes: see the arithmetic in Paths.PavementZ. A bench 0.30 m deep
        /// with its back to the building fits in the 0.42 m the rail
        /// encloses, costs the pavement nothing, and is a terrace the
        /// passers-by cannot walk into because it is behind the rail.
        /// </summary>
        private void TerraceSeats(Modeler m, Palette p, float left, float right)
        {
            List<float> xs = BenchSpots(left, right);
            for (int i = 0; i < xs.Count; i++) TerraceBench(m, p, xs[i], BenchZ);
        }

        /// <summary>
        /// Where the terrace benches stand. SHARED with PlanterSpots, which
        /// drops any pot that would land on one.
        ///
        /// This is the third time in this file that two things placed along
        /// the same front had to be told about each other, and the first two
        /// were both bugs the user saw before the code did: the rail through
        /// the planters, and the extractor hood through the oven. A bench is
        /// 1.46 m across including its arms and a planter 0.46 m, and they
        /// overlap in z between -0.19 and -0.34, so a pot 3.2 m along the
        /// front WILL eventually land on one. Two lists drift; one does not.
        /// </summary>
        private static List<float> BenchSpots(float left, float right)
        {
            List<float> xs = new List<float>();
            float[] spots = { Paths.DoorX - 2.6f, Paths.DoorX + 2.6f };
            foreach (float x in spots)
            {
                if (x < left + 0.9f || x > right - 0.9f) continue;
                xs.Add(x);
            }
            return xs;
        }

        /// <summary>Half a bench plus half a planter: the room a pot needs.</summary>
        private const float BenchClear = 0.94f;

        /// <summary>The line the terrace benches stand on.</summary>
        private const float BenchZ = -0.19f;

        /// <summary>
        /// One bench: back to the building, facing the street.
        ///
        /// The front edge is at -0.34 and the rail's inner face at -0.385,
        /// so it clears the rail by 4.5 cm. A seat that touched the rail
        /// would read as a rail with a plank stuck to it.
        /// </summary>
        private void TerraceBench(Modeler m, Palette p, float x, float z)
        {
            // The seat, and the back panel against the wall.
            m.Box(new Vector3(x, 0.42f, z), new Vector3(1.40f, 0.06f, 0.30f), p.Wood);
            m.Box(new Vector3(x, 0.62f, z + 0.13f),
                  new Vector3(1.40f, 0.34f, 0.05f), p.Wood);

            // Four legs, and the arm at each end - the arm is what makes a
            // plank on legs read as a BENCH at this camera's forty pixels.
            for (int i = 0; i < 4; i++)
            {
                float lx = x + ((i % 2 == 0) ? -0.62f : 0.62f);
                float lz = z + ((i < 2) ? -0.11f : 0.11f);
                m.Box(new Vector3(lx, 0.20f, lz),
                      new Vector3(0.06f, 0.40f, 0.06f), p.WallTrim);
            }
            m.Box(new Vector3(x - 0.68f, 0.56f, z),
                  new Vector3(0.05f, 0.05f, 0.30f), p.WallTrim);
            m.Box(new Vector3(x + 0.68f, 0.56f, z),
                  new Vector3(0.05f, 0.05f, 0.30f), p.WallTrim);
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
