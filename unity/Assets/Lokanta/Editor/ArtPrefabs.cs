using System.Collections.Generic;
using System.IO;
using Lokanta.Game;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Lokanta.EditorTools
{
    /// <summary>
    /// Generates, for every model, a prefab with its MATERIAL - SCALE - POSE
    /// BAKED IN.
    ///
    /// Why a prefab: the import remapping (ModelImporter.AddRemap) is an
    /// invisible layer. If the material lives inside the prefab the result is
    /// readable - you can open the file and see which material it uses.
    ///
    /// The scale IS NOT A MULTIPLIER WRITTEN DOWN IN ADVANCE, it is DERIVED
    /// FROM THE MODEL'S MEASURED SIZE. There used to be one multiplier per
    /// folder (Furniture 0.20); the chairs were the right height but the table
    /// came out 1.6 m across and swallowed them - the package's models are not
    /// at the same scale as each other. Here a REAL WORLD TARGET is written for
    /// every model and the multiplier is found by dividing it by the measured
    /// size.
    ///
    /// The characters also get ANIMATION. The package carries 32 clips and the
    /// skeleton is the same on every figure; a single controller drives them
    /// all. Left without clips the figures stay in the bind pose - arms out to
    /// the sides, a 1.94 m wingspan.
    /// </summary>
    public static class ArtPrefabs
    {
        private const string Art = "Assets/Lokanta/Art";
        // The folder is "Materials" on disk. It used to be "Malzeme"; a path
        // left behind by that rename writes the materials somewhere nothing
        // reads them, and the scene comes up magenta.
        private const string MatDir = Art + "/Materials";
        private const string PrefabDir = Art + "/Prefab";
        private const string AnimDir = Art + "/Animator";
        private const string ControllerPath = AnimDir + "/Character.controller";

        /// <summary>The reference figure the clips and the scale are taken from.</summary>
        private const string RefCharacter = "character-male-a";

        // =====================================================================
        /// <summary>A target size: the axis to measure and the value in metres.</summary>
        private struct Target
        {
            public bool Width;      // if true, the larger of X/Z, otherwise Y
            public float Metres;
            /// <summary>If greater than zero, Y is scaled independently.</summary>
            public float StretchY;

            public Target(bool width, float metres, float stretchY = 0f)
            {
                Width = width; Metres = metres; StretchY = stretchY;
            }
        }

        private static Target Tall(float m) { return new Target(false, m); }
        private static Target Wide(float m) { return new Target(true, m); }

        /// <summary>
        /// Sets the base to a diameter and the height to a separate target.
        ///
        /// It was needed for one single model: the package's round table is in
        /// the right proportion to its own chair but 1.4 m across - while the
        /// cell a table set has to fit into is 1.85 x 1.70 m and leaves no room
        /// for the chairs. Bringing the diameter down to a realistic 0.88 m
        /// turned the table into a 0.40 m coffee table. Stretching its legs is
        /// the fix that is invisible on a low-poly model and right in its
        /// proportions.
        /// </summary>
        private static Target WideTall(float wide, float tall)
        {
            return new Target(true, wide, tall);
        }

        /// <summary>
        /// The real world target per model. Most furniture heights are standard
        /// and tied to human height - a counter 0.92 m, a fridge 1.80 m - which
        /// is why the measured axis is usually Y. The table is the exception:
        /// what matters is not its height but its DIAMETER, because that is
        /// what decides how many tables fit in the hall (RoomPlan.CellX/CellZ).
        /// </summary>
        private static readonly Dictionary<string, Target> Targets =
            new Dictionary<string, Target>
            {
                // --- hall ---
                // THE TABLE SETTING IS SIZED AGAINST THE CHARACTER.
                //
                // Measured: at a 0.74 m top, the head of a seated figure was
                // only 0.37 m above the table (on a real person it is 0.51).
                // The furniture is at real scale and the characters at half of
                // it - and that inconsistency shows up most at the table,
                // because that is where the player is looking.
                //
                // The diameter STAYS at 0.88: a table set has to fit into the
                // 1.85 m cell, and shrinking the diameter would move two guests
                // closer to one another.
                { "tableRound", WideTall(0.88f, 0.58f) },
                // The chair is in the same proportion as the table: with a
                // 0.92 backrest it was THE SAME height as a 0.94 m figure.
                { "chairCushion", Tall(0.68f) },
                { "chair", Tall(0.68f) },
                { "chairRounded", Tall(0.68f) },
                // A SQUARE TABLE. Four seats sit on four SIDES; on a hexagon
                // two of them landed on a corner.
                //
                // The height is 0.55 for the same reason as the round table:
                // the furniture is at real scale, the characters at half of it,
                // and a 0.74 top came up to a seated figure's chin (measured:
                // the head 0.37 m above the table against 0.51 in reality).
                { "table", WideTall(0.82f, 0.58f) },
                { "tableCloth", Tall(0.76f) },
                { "tableCrossCloth", Tall(0.76f) },
                { "stoolBar", Tall(0.56f) },
                { "rugRectangle", Wide(3.00f) },
                { "rugRounded", Wide(2.40f) },
                { "pottedPlant", Tall(0.64f) },
                { "lampSquareCeiling", Tall(0.40f) },

                // --- kitchen ---
                //
                // THE HALL WAS SCALED TO THE FIGURE AND THE KITCHEN WAS NOT,
                // and that half-finished job is why the dishwasher could not
                // reach into the sink.
                //
                // The two notes above record the hall's side of it: a 0.74 m
                // table came up to a seated figure's chin, so it went to 0.58,
                // and the chairs came down with it. Everything through this
                // door kept its real-world height - a 0.92 m counter, a 1.80 m
                // fridge - so the same room contained furniture drawn to two
                // different scales. The visible result was a cook standing at
                // a worktop level with their shoulders and a washer holding a
                // plate at chest height because the basin was above their
                // reach.
                //
                // THE RATIO IS THE ONE THIS FILE ALREADY USES, not a new one.
                // The door note states the method: take the real size, divide
                // by a real person, multiply by ours - and then leave a margin
                // so the object still reads as itself. The hall lands at about
                // 0.75 of real (a 0.74 table at 0.58, a 0.88 chair at 0.68).
                // Every number below is its real size times 0.75, which puts
                // the kitchen and the hall on one scale for the first time.
                //
                // A 0.69 m worktop against a 1.00 m figure is 69% of its
                // height. That is still taller than life (a real counter is
                // 54% of a real person) and it is EXACTLY the hall's own
                // exaggeration, which was measured and kept on purpose: at
                // this camera, furniture drawn dead to scale reads as too low.
                { "kitchenFridgeLarge", Tall(1.35f) },
                { "kitchenFridge", Tall(1.05f) },
                { "kitchenCabinetUpper", Tall(0.52f) },
                { "kitchenCoffeeMachine", Tall(0.34f) },
                { "kitchenMicrowave", Tall(0.24f) },
                { "kitchenBar", Tall(0.82f) },
                { "kitchenBarEnd", Tall(0.82f) },
                { "bookcaseClosedDoors", Tall(1.35f) },

                // --- structure ---
                // THE DOOR, SIZED AGAINST THE CHARACTER.
                //
                // 2.10 m is a real door size, but the figures in this package
                // are 1.10 m; next to them the door turned into a triumphal
                // arch. At the real ratio (door/person = 1.17) the equivalent
                // is 1.29 m; 1.45 leaves a little margin and still reads as a
                // "door". The rest of the furniture stays at realistic size -
                // the door is the one exception, because it is the one thing
                // you walk through.
                { "doorwayOpen", Tall(1.45f) },
                { "wallDoorway", Tall(2.60f) },

                // --- food ---
                // The plate diameter is the starting point: a burger has to fit
                // on the plate, a glass has to be smaller than the plate. Given
                // one single target, the egg came out the size of the plate.
                { "plate", Wide(0.26f) },
                { "plate-deep", Wide(0.26f) },
                { "plate-dinner", Wide(0.26f) },
                { "bowl", Wide(0.16f) },
                { "bowl-soup", Wide(0.16f) },
                { "salad", Wide(0.16f) },
                { "cup", Wide(0.08f) },
                { "cup-tea", Wide(0.08f) },
                { "cup-saucer", Wide(0.13f) },
                { "glass", Wide(0.07f) },
                { "soda-glass", Wide(0.08f) },
                { "burger", Wide(0.12f) },
                { "burger-cheese", Wide(0.12f) },
                { "meat-patty", Wide(0.11f) },
                { "meat-cooked", Wide(0.16f) },
                { "fries", Tall(0.13f) },
                { "bread", Wide(0.18f) },
                { "cake", Wide(0.20f) },
                { "cheese", Wide(0.14f) },
                { "egg", Wide(0.05f) },
                { "onion", Wide(0.07f) },
                { "tomato", Wide(0.06f) },
                { "rice-ball", Wide(0.06f) },
            };

        /// <summary>The folder default, for anything not in the list.</summary>
        private static readonly Dictionary<string, Target> FolderTarget =
            new Dictionary<string, Target>
            {
                // The folder keys are the folder names on disk. They used to be
                // "Mobilya", "Karakter" and "Yemek"; a key left behind by that
                // rename matches no folder and every model falls back to a
                // scale of 1.
                // 0.92 -> 0.69: the folder default is a WORKTOP, and it is
                // what the sink and every unnamed unit in the kitchen took.
                // See the kitchen block in Targets for why it moved.
                { "Furniture", Tall(0.69f) },
                // 1.28 m, NOT a real 1.70 m.
                //
                // Measured: even at 1.55 m the figures looked enormous next to
                // the furniture. The reason is not a scale error but a
                // difference of STYLE - the package's figures are big-headed
                // (the head is 35% of the height) while the furniture is
                // realistically proportioned (a chair 0.92 x 0.40 m). The way
                // to marry the two styles is to shrink the figure a little; and
                // what is looked at is not height but BULK.
                //
                // 11 September 2026: 1.45 -> 1.28 -> 1.10.
                //
                // At the first reduction the yardstick was the
                // "figure/chair height ratio", and that yardstick WAS ASKING THE
                // WRONG QUESTION: the figures in this package are bigger ACROSS
                // than they are tall (the head is a third of the body). While
                // the height ratio looked fine, a seated figure's footprint came
                // out 1.05 x 1.18 m - against 0.88 m between two seats, so
                // neighbours were intersecting by definition.
                //
                // The right yardstick is THE PLACEMENT AUDIT
                // (Editor/PlacementAudit): every object in the scene's box in
                // its real pose, and the intersecting pairs. At 1.45 there were
                // 68 clashing pairs; with 1.10 plus the seat arrangement plus
                // the staff spacing there are NONE.
                //
                // 1.10 m was also confirmed with a separate screenshot
                // (Editor/FigureShot): one figure, one chair, on a 1 m grid,
                // from the side. The figure sits on the chair and its height
                // reads as proportional to the chair.
                { "Characters", Tall(CharacterHeight) },
                { "Food", Wide(0.22f) },
            };

        /// <summary>
        /// The references that take a folder's scale from ONE SINGLE model.
        ///
        /// Essential for the characters: forcing each figure to 1.70 m on its
        /// own shrinks a figure that measures taller because of its hair or its
        /// hat. The result is a staff who are all the same height but whose
        /// bodies are different sizes. If they all use the same multiplier, the
        /// height differences within the package are preserved.
        /// </summary>
        private static readonly Dictionary<string, string> FolderReference =
            new Dictionary<string, string> { { "Characters", RefCharacter } };

        // =====================================================================
        // The Kenney furniture palette. The colour of an untextured FBX lives
        // only in the material's NAME.
        private static readonly Dictionary<string, Color> Palette =
            new Dictionary<string, Color>
            {
                { "wood", Hex(0x9C6B45) },
                { "woodDark", Hex(0x6B472E) },
                { "woodLight", Hex(0xC79A6B) },
                { "metal", Hex(0x9AA0A6) },
                { "metalDark", Hex(0x5C6166) },
                { "metalMedium", Hex(0x7D848A) },
                { "metalLight", Hex(0xC2C8CE) },
                { "carpet", Hex(0x8E5B5B) },
                { "carpetWhite", Hex(0xD9D2C7) },
                { "carpetDark", Hex(0x5E3E3E) },
                { "carpetDarker", Hex(0x4A3030) },
                { "glass", Hex(0xBFD8E0) },
                { "plastic", Hex(0xD8D3C8) },
                { "leather", Hex(0x6E4B3A) },
                { "fabric", Hex(0xB2A894) },
                { "stone", Hex(0xA8A49C) },
                { "white", Hex(0xE8E4DC) },
                { "black", Hex(0x33363A) },
                { "green", Hex(0x6E9E58) },
                { "red", Hex(0xB3524A) },
                { "lamp", Hex(0xF0E2BC) },
                { "plant", Hex(0x5E8C46) },
                { "_defaultMat", Hex(0xB9B2A6) },
            };

        private static Color Hex(int rgb)
        {
            return new Color(((rgb >> 16) & 0xFF) / 255f,
                             ((rgb >> 8) & 0xFF) / 255f,
                             (rgb & 0xFF) / 255f);
        }

        // =====================================================================
        [MenuItem("Lokanta/Generate the model prefabs")]
        public static void Run()
        {
            if (!AssetDatabase.IsValidFolder(Art))
            {
                Debug.LogWarning("There is no Art folder. Run 'python tools/art/import_vendor.py'.");
                return;
            }
            MakeFolder(Art, "Materials");
            MakeFolder(Art, "Prefab");
            MakeFolder(Art, "Animator");
            MakeFolder(Art, "Mesh");
            MakeCharactersReadable();

            Shader lit = Shader.Find("Universal Render Pipeline/Lit");
            if (lit == null) { Debug.LogError("There is no URP Lit shader."); return; }

            // The materials and the controller are saved FIRST and in one go:
            // the prefab is going to reference them, so they have to be on
            // disk.
            Dictionary<string, Material> cache = BuildMaterials(lit);
            BuildSpecialMaterials(lit);
            AnimationClip[] clips = FindClips();
            AnimatorController ctrl = BuildController(clips);
            AssetDatabase.SaveAssets();

            Dictionary<string, float> folderScale = ReferenceScales();

            int made = 0;
            foreach (string g in AssetDatabase.FindAssets("t:Model", new[] { Art }))
            {
                string path = AssetDatabase.GUIDToAssetPath(g);
                if (MakePrefab(path, cache, ctrl, clips, folderScale)) made++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(string.Format(
                "{0} prefabs generated, {1} URP materials set up, {2} clips wired.",
                made, cache.Count, ClipCount(clips)));
        }

        private static int ClipCount(AnimationClip[] clips)
        {
            int n = 0;
            if (clips != null)
                foreach (AnimationClip c in clips) if (c != null) n++;
            return n;
        }

        // =====================================================================
        /// <summary>
        /// TRANSPARENT AND UNLIT MATERIALS - AS ASSETS.
        ///
        /// WHY AN ASSET AND NOT AT RUN TIME:
        ///
        /// The walls, the door leaves, the oven glass and the street lamp's
        /// light pool were built at run time with `new Material(...)` and
        /// looked right IN THE EDITOR. In a real build they all came out
        /// OPAQUE: URP DOES NOT PUT the transparent pass's shader variant into
        /// the build unless some asset references it. The same class of bug had
        /// already happened with URP/Unlit (the badges).
        ///
        /// Once the material is a .mat asset, Unity collects the variant. These
        /// are also wired into the scene (BuildGameScene), so they really are
        /// "referenced" assets.
        ///
        /// This class of bug IS INVISIBLE IN THE EDITOR - it only shows in a
        /// build.
        /// </summary>
        private static void BuildSpecialMaterials(Shader lit)
        {
            Shader unlit = Shader.Find("Universal Render Pipeline/Unlit");

            // THE WALL IS FAINTER: 0.16 -> 0.10.
            //
            // As the scene gained decoration (the floor pattern, the rug, the
            // pendant lamp, the furniture tone) the milky white of the walls
            // laid a HAZE over the whole hall: the colours underneath washed
            // out, while the separation between rooms already reads from the
            // floor and the props. The wall's job is to DRAW a boundary, not to
            // paint an area.
            //
            // Note: there was a "WallColor" field on RestaurantView and nothing
            // read it - the real colour is HERE, because a transparent material
            // has to be a .mat ASSET (docs/37).
            //
            // THE NAMES ARE THE ONES ON DISK: custom_wall.mat and so on. They
            // used to be "ozel_duvar"; a name left behind by that rename
            // generates a second material that nothing loads.
            Transparent(lit, "wall", new Color(0.74f, 0.78f, 0.86f, 0.10f), 0.10f);
            Transparent(lit, "door", new Color(0.52f, 0.37f, 0.24f, 0.55f), 0.15f);
            Transparent(lit, "glass", new Color(0.14f, 0.16f, 0.18f, 0.45f), 0.75f);
            // RUNNING WATER. Transparent and glossy.
            //
            // It HAS TO be an ASSET: URP does not put the transparent pass's
            // shader variant into the build unless an asset references it, and
            // a transparent material built at run time is drawn OPAQUE on the
            // device - with no symptom at all in the editor. This project hit
            // that bug once already, with the walls.
            Transparent(lit, "water", new Color(0.62f, 0.84f, 0.96f, 0.42f), 0.90f);
            if (unlit != null) Additive(unlit, "lightpool",
                                        new Color(1.00f, 0.80f, 0.45f, 0.55f));
            // THE CEILING LIGHT IS A SEPARATE ASSET, NOT a property block.
            //
            // The same material used to be tinted with a
            // MaterialPropertyBlock. That had two costs: writing a property
            // block throws those renderers OUT of SRP batching (this project
            // learned that once with the floor slabs), and a fully expanded
            // restaurant has 25 ceiling lights - every one a separate draw.
            //
            // The colour differs from the street's: the street lamp is sodium
            // yellow, indoors is warm white and fainter (the sum of dozens of
            // pools across eight rooms was saturating the floor to white).
            if (unlit != null) Additive(unlit, "ceilinglight",
                                        new Color(1.00f, 0.88f, 0.70f, 0.34f));
        }

        private static Material Transparent(Shader lit, string name, Color c, float smooth)
        {
            string path = MatDir + "/custom_" + name + ".mat";
            Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(lit);
                AssetDatabase.CreateAsset(m, path);
            }
            if (m.shader != lit) m.shader = lit;

            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 0f);
            m.SetFloat("_ZWrite", 0f);
            m.SetFloat("_AlphaClip", 0f);
            m.SetFloat("_Smoothness", smooth);
            m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend",
                       (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.DisableKeyword("_ALPHATEST_ON");
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = 3000;
            m.SetOverrideTag("RenderType", "Transparent");
            m.SetColor("_BaseColor", c);
            EditorUtility.SetDirty(m);
            return m;
        }

        private static Material Additive(Shader unlit, string name, Color c)
        {
            string path = MatDir + "/custom_" + name + ".mat";
            Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(unlit);
                AssetDatabase.CreateAsset(m, path);
            }
            if (m.shader != unlit) m.shader = unlit;

            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 1f);
            m.SetFloat("_ZWrite", 0f);
            m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = 3100;
            m.SetOverrideTag("RenderType", "Transparent");
            m.SetColor("_BaseColor", c);
            m.SetTexture("_BaseMap", GlowTexture());
            EditorUtility.SetDirty(m);
            return m;
        }

        /// <summary>
        /// The light pool's soft circular texture - AS AN ASSET.
        ///
        /// A flat-coloured quad gives a SQUARE light pool. The texture could be
        /// generated at run time, but then the material would have to be built
        /// at run time too; as assets, both of them go into the build.
        /// </summary>
        private static Texture2D GlowTexture()
        {
            string path = MatDir + "/custom_lightpool_texture.asset";
            Texture2D t = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (t != null) return t;

            const int N = 64;
            t = new Texture2D(N, N, TextureFormat.RGBA32, false);
            t.name = "lightpool";
            t.wrapMode = TextureWrapMode.Clamp;
            t.filterMode = FilterMode.Bilinear;

            Color32[] px = new Color32[N * N];
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    float dx = (x + 0.5f) / N * 2f - 1f;
                    float dy = (y + 0.5f) / N * 2f - 1f;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    // COMPLETELY ZERO AT THE EDGE. Unlike a square falloff,
                    // the cubic leaves no visible residue in the corner - under
                    // additive blending that residue gave away that the quad
                    // was SQUARE.
                    // THE FALLOFF IS WRITTEN INTO THE COLOUR TOO, NOT JUST THE
                    // ALPHA.
                    //
                    // Alpha alone did not work: the quad was drawn as a square
                    // patch without fading at its edge. Under ADDITIVE blending
                    // (SrcAlpha + One) the contribution is src.rgb * src.a; once
                    // the colour is faded as well, the edge goes to black
                    // REGARDLESS of how the alpha is handled, and black adds
                    // nothing.
                    float a = Mathf.Clamp01(1f - d);
                    a = a * a;
                    byte v = (byte)(a * 255f);
                    px[y * N + x] = new Color32(v, v, v, v);
                }
            t.SetPixels32(px);
            t.Apply(false, false);
            AssetDatabase.CreateAsset(t, path);
            return t;
        }

        private static Dictionary<string, Material> BuildMaterials(Shader lit)
        {
            Dictionary<string, Material> cache = new Dictionary<string, Material>();

            foreach (string g in AssetDatabase.FindAssets("t:Model", new[] { Art }))
            {
                string path = AssetDatabase.GUIDToAssetPath(g);
                string folder = FolderOf(path);
                string texture = FindTexture(Art + "/" + folder);

                GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (model == null) continue;

                foreach (Renderer r in model.GetComponentsInChildren<Renderer>(true))
                foreach (Material src in r.sharedMaterials)
                {
                    if (src == null) continue;

                    string name = Clean(src.name, folder);
                    string key = folder + "/" + name;
                    if (cache.ContainsKey(key)) continue;

                    string matPath = MatDir + "/" + folder + "_" + name + ".mat";
                    Material m = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                    if (m == null)
                    {
                        m = new Material(lit);
                        AssetDatabase.CreateAsset(m, matPath);
                    }
                    if (m.shader != lit) m.shader = lit;

                    if (!string.IsNullOrEmpty(texture))
                    {
                        Texture2D t = AssetDatabase.LoadAssetAtPath<Texture2D>(texture);
                        if (t != null)
                        {
                            m.SetTexture("_BaseMap", t);
                            m.SetColor("_BaseColor", Color.white);
                        }
                    }
                    else if (Palette.TryGetValue(name, out Color c))
                    {
                        m.SetTexture("_BaseMap", null);
                        m.SetColor("_BaseColor", c);
                    }
                    else
                    {
                        m.SetColor("_BaseColor", Hex(0xB9B2A6));
                        Debug.LogWarning("No palette entry: " + key);
                    }

                    // Low smoothness: in a low-poly hall glossy surfaces
                    // exaggerate the faceting.
                    m.SetFloat("_Smoothness", 0.10f);
                    m.SetFloat("_Metallic", 0f);
                    EditorUtility.SetDirty(m);
                    cache[key] = m;
                }
            }
            return cache;
        }

        // =====================================================================
        /// <summary>
        /// PUTS THE CLIPS ON A LOOP.
        ///
        /// The user's two separate complaints came from ONE cause: "the
        /// characters do not take steps, they look like they are sliding over
        /// the floor" and "the cook does not stir the food, the dishwasher does
        /// not scrub".
        ///
        /// The clips arrived from the FBX with `clipAnimations: []`, that is,
        /// with Unity's default import settings - and there loopTime is OFF. A
        /// clip plays once and then FREEZES ON ITS LAST FRAME:
        ///
        ///   - the walk clip is ~1 second; if a figure walks for nine seconds
        ///     it SLIDES for eight of them on frozen legs,
        ///   - the chopping clip comes down once and the cleaver stays in
        ///     mid-air,
        ///   - the washing clip scrubs once and stops.
        ///
        /// Keeping the animator awake (HoldAwake) did not fix this - "the
        /// animator is running" and "the clip loops" are two different things.
        /// The answer to something hunted for months on the code side was in an
        /// IMPORT setting.
        ///
        /// Only the clips WE USE are put on a loop; the rest of the package's
        /// thirty-two clips are left alone.
        /// </summary>
        private static void LoopClips()
        {
            string path = Art + "/Characters/" + RefCharacter + ".fbx";
            ModelImporter mi = AssetImporter.GetAtPath(path) as ModelImporter;
            if (mi == null)
            {
                Debug.LogWarning("No importer: " + path);
                return;
            }

            ModelImporterClipAnimation[] defs = mi.clipAnimations;
            if (defs == null || defs.Length == 0) defs = mi.defaultClipAnimations;
            if (defs == null || defs.Length == 0)
            {
                Debug.LogWarning("No clip definitions: " + path);
                return;
            }

            int looped = 0;
            for (int i = 0; i < defs.Length; i++)
            {
                bool ours = false;
                for (int k = 0; k < Figure.ClipNames.Length; k++)
                    if (defs[i].name == Figure.ClipNames[k]) { ours = true; break; }
                if (!ours) continue;

                if (defs[i].loopTime) { looped++; continue; }
                defs[i].loopTime = true;
                defs[i].loopPose = true;
                looped++;
            }

            mi.clipAnimations = defs;
            mi.SaveAndReimport();
            Debug.Log("  clip looping: " + looped + " clips put on a loop ("
                      + Figure.ClipNames.Length + " wanted)");
        }

        /// <summary>
        /// Finds the reference figure's clips. They come back in Figure.Pose
        /// order; where one is not found the slot stays null.
        /// </summary>
        private static AnimationClip[] FindClips()
        {
            LoopClips();
            string path = Art + "/Characters/" + RefCharacter + ".fbx";
            AnimationClip[] found = new AnimationClip[Figure.ClipNames.Length];

            Object[] all = AssetDatabase.LoadAllAssetsAtPath(path);
            if (all == null || all.Length == 0)
            {
                Debug.LogWarning("No reference figure: " + path);
                return found;
            }

            foreach (Object o in all)
            {
                AnimationClip c = o as AnimationClip;
                if (c == null) continue;
                for (int i = 0; i < Figure.ClipNames.Length; i++)
                    if (found[i] == null && c.name == Figure.ClipNames[i]) found[i] = c;
            }

            for (int i = 0; i < found.Length; i++)
                if (found[i] == null)
                    Debug.LogWarning("Clip not found: " + Figure.ClipNames[i]);
            return found;
        }

        /// <summary>
        /// A single controller: every pose is a state, and the transitions are
        /// done in code with CrossFade.
        ///
        /// NO conditions and NO parameters. An integer parameter called "state"
        /// plus Any State transitions was considered first; five transitions
        /// and five conditions for five poses, all to do what CrossFade already
        /// does. Calling CrossFade by name does the same job in one line and
        /// leaves the controller readable.
        /// </summary>
        private static AnimatorController BuildController(AnimationClip[] clips)
        {
            AnimatorController c =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (c == null) c = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);

            AnimatorStateMachine sm = c.layers[0].stateMachine;
            for (int i = sm.states.Length - 1; i >= 0; i--)
                sm.RemoveState(sm.states[i].state);

            for (int i = 0; i < Figure.StateNames.Length; i++)
            {
                AnimatorState s = sm.AddState(Figure.StateNames[i]);
                s.motion = i < clips.Length ? clips[i] : null;
                s.writeDefaultValues = true;
                if (i == 0) sm.defaultState = s;
            }

            EditorUtility.SetDirty(c);
            return c;
        }

        // =====================================================================
        /// <summary>Works out a single scale for the folders that have a reference.</summary>
        private static Dictionary<string, float> ReferenceScales()
        {
            Dictionary<string, float> scales = new Dictionary<string, float>();

            foreach (KeyValuePair<string, string> kv in FolderReference)
            {
                string path = Art + "/" + kv.Key + "/" + kv.Value + ".fbx";
                GameObject src = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (src == null) { Debug.LogWarning("No reference: " + path); continue; }

                GameObject tmp = Object.Instantiate(src);
                tmp.transform.position = Vector3.zero;
                tmp.transform.rotation = Quaternion.identity;
                tmp.transform.localScale = Vector3.one;

                Bounds b = Measure(tmp);
                float s = ScaleFor(kv.Value, kv.Key, b);
                Object.DestroyImmediate(tmp);

                scales[kv.Key] = s;
                Debug.Log(string.Format(
                    "  the {0} folder was scaled from the reference {1}: {2:0.000}"
                    + "   (measured {3:0.00} x {4:0.00} x {5:0.00} m)",
                    kv.Key, kv.Value, s, b.size.x, b.size.y, b.size.z));
            }
            return scales;
        }

        private static bool MakePrefab(string modelPath, Dictionary<string, Material> cache,
                                       AnimatorController ctrl, AnimationClip[] clips,
                                       Dictionary<string, float> folderScale)
        {
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            if (source == null) return false;

            string folder = FolderOf(modelPath);
            string name = Path.GetFileNameWithoutExtension(modelPath);
            bool character = folder == "Characters";

            // A root is put ON TOP OF the model, not INSIDE it.
            //
            // The reason is measurement: some models have their pivot in the
            // middle of the body - the round table was sinking 0.32 m into the
            // floor. To sit it on its base the model has to be shifted, but the
            // root itself cannot be shifted: the placement uses it. A separate
            // root keeps the two apart.
            GameObject root = new GameObject(name);
            GameObject model = Object.Instantiate(source);
            model.name = "model";
            model.transform.SetParent(root.transform, false);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
            model.transform.localScale = Vector3.one;

            Bounds b = Measure(model);
            float scale;
            if (!folderScale.TryGetValue(folder, out scale))
                scale = ScaleFor(name, folder, b);

            float scaleY = scale;
            Target tgt;
            if (Targets.TryGetValue(name, out tgt) && tgt.StretchY > 0f && b.size.y > 0.0001f)
                scaleY = tgt.StretchY / b.size.y;

            model.transform.localScale = new Vector3(scale, scaleY, scale);

            // Centring horizontally applies to PROPS ONLY. On a character the
            // bind pose can be asymmetric (a figure's hand holding something)
            // and centring shifts the body off the chair.
            model.transform.localPosition = new Vector3(
                character ? 0f : -b.center.x * scale,
                -b.min.y * scaleY,
                character ? 0f : -b.center.z * scale);

            if (character) ShrinkHead(model);
            if (character) AddKnees(model, name);

            foreach (Renderer r in model.GetComponentsInChildren<Renderer>(true))
            {
                Material[] mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i] == null) continue;
                    string key = folder + "/" + Clean(mats[i].name, folder);
                    if (cache.TryGetValue(key, out Material m)) mats[i] = m;
                }
                r.sharedMaterials = mats;

                // Only people cast a shadow. Small props' shadows dirty the
                // floor plan and at a 34 degree view a chair's shadow does not
                // read anyway; and on mobile every shadow caster means another
                // draw.
                r.shadowCastingMode = character
                    ? UnityEngine.Rendering.ShadowCastingMode.On
                    : UnityEngine.Rendering.ShadowCastingMode.Off;
            }

            if (character) Animate(model, ctrl, clips);

            Debug.Log(string.Format(
                "  {0,-24} {1:0.00} x {2:0.00} x {3:0.00} m  ->  scale {4:0.000}"
                + "  ({5:0.00} x {6:0.00} x {7:0.00} m)",
                name, b.size.x, b.size.y, b.size.z, scale,
                b.size.x * scale, b.size.y * scaleY, b.size.z * scale));

            MakeFolder(PrefabDir, folder);
            string path = PrefabDir + "/" + folder + "/" + name + ".prefab";
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return true;
        }

        /// <summary>
        /// The character's height (m). The placement audit asks FROM HERE too:
        /// writing the number in two places means one of them goes stale - the
        /// audit printed "target 1.28" for a long time after the target had
        /// already changed.
        /// </summary>
        public const float CharacterHeight = 1.00f;

        /// <summary>The head's proportion to the body. 1 = the package's own.</summary>
        private const float HeadScale = 0.80f;

        /// <summary>
        /// Shrinks the head bone.
        ///
        /// Instead of shrinking the whole figure: it was measured, and against
        /// the furniture the figures are already at half of reality (0.94 m
        /// standing, a 0.92 m chair back; the real ratio is 1.9). What looks
        /// big is the head - 36% of the body, against 13% on a real person.
        ///
        /// The bone's scale carries into the skinned mesh, so only the vertices
        /// bound to it shrink: the body, the arms and the legs are unchanged.
        /// Because hats and hair are bound to the head bone they shrink with
        /// it - no need to handle them separately.
        /// </summary>
        private static void ShrinkHead(GameObject model)
        {
            foreach (Transform t in model.GetComponentsInChildren<Transform>(true))
            {
                if (t.name != "head") continue;
                t.localScale = t.localScale * HeadScale;
                return;
            }
            Debug.LogWarning("the head bone was not found: " + model.name);
        }

        // =====================================================================
        // THE KNEE BONE.
        //
        // The package's skeleton has ONE bone per leg (root, leg-left,
        // leg-right, torso, arm-left, arm-right, head) - which means the leg
        // cannot bend at the knee. The price of that was measured on a chair:
        // if the hip is to sit on the cushion, a single-piece leg coming down
        // from the hip HAS TO pass through the cushion. That is why the
        // package's own sitting clip holds the thigh horizontal with the feet
        // stretched forward - not "a person sitting on a chair" but "a person
        // sitting cross-legged on the floor".
        //
        // The fix is to ADD a bone. The leg mesh allows it: there are 22
        // distinct vertex levels along the leg, so the new bone really bends
        // rather than just skewing the box.
        //
        // WHY IN THE PIPELINE: a one-off edit would be wiped by the next run of
        // "Generate the model prefabs". Prefab generation is the single source
        // of truth on this project.
        // =====================================================================

        /// <summary>The knee bone's name. Figure looks for this too.</summary>
        public const string KneeLeft = "knee-left";

        /// <summary>The knee bone's name. Figure looks for this too.</summary>
        public const string KneeRight = "knee-right";

        private static readonly string[] LegBones = { "leg-left", "leg-right" };
        private static readonly string[] KneeBones = { KneeLeft, KneeRight };

        /// <summary>
        /// Turns on mesh reading for the character models.
        ///
        /// Without reading the weights and the vertices no reweighting is
        /// possible, and the package arrives with it OFF by default
        /// (isReadable: 0) - on the first attempt .vertices came back empty and
        /// that was why.
        ///
        /// The cost: a copy of the mesh data stays in memory. For a 771-vertex
        /// figure that is immeasurable; only the Characters folder is turned
        /// on, the furniture stays off.
        /// </summary>
        private static void MakeCharactersReadable()
        {
            int opened = 0;
            foreach (string g in AssetDatabase.FindAssets(
                         "t:Model", new[] { Art + "/Characters" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(g);
                ModelImporter mi = AssetImporter.GetAtPath(path) as ModelImporter;
                if (mi == null || mi.isReadable) continue;
                mi.isReadable = true;
                mi.SaveAndReimport();
                opened++;
            }
            if (opened > 0) Debug.Log("  mesh reading turned on: " + opened + " characters");
        }

        /// <summary>
        /// Adds a knee bone to each leg and binds the vertices below the knee
        /// to it.
        ///
        /// THE SPLIT PLANE passes BETWEEN two rings of vertices, not through
        /// one: if it went through a ring, on a flat-shaded model two vertices
        /// at the same point would fall to different bones and the surface
        /// would SPLIT OPEN. Passing between them stretches only ONE quad -
        /// which is exactly what a low-poly knee wants.
        /// </summary>
        private static void AddKnees(GameObject model, string name)
        {
            foreach (SkinnedMeshRenderer smr in
                     model.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                Mesh src = smr.sharedMesh;
                if (src == null) continue;
                if (!src.isReadable)
                {
                    Debug.LogWarning("The mesh cannot be read, no knee was added: " + name);
                    continue;
                }

                Transform[] bones = smr.bones;
                if (BoneIndex(bones, KneeLeft) >= 0) continue;

                int[] leg = new int[2];
                bool ok = true;
                for (int i = 0; i < 2; i++)
                {
                    leg[i] = BoneIndex(bones, LegBones[i]);
                    if (leg[i] < 0) ok = false;
                }
                if (!ok) continue;

                Vector3[] verts = src.vertices;
                BoneWeight[] w = src.boneWeights;
                Matrix4x4[] binds = src.bindposes;
                if (w.Length != verts.Length || binds.Length != bones.Length) continue;

                List<Transform> newBones = new List<Transform>(bones);
                List<Matrix4x4> newBinds = new List<Matrix4x4>(binds);

                // TEST THE FORMULA AGAINST THE MODEL'S OWN MATRIX.
                //
                // The new bone's bind matrix is built by hand, and if it is
                // built wrongly the mesh shifts SILENTLY - the placement audit
                // measured the figure at 0.90 m against a target of 1.00. The
                // same formula is applied to the package's OWN leg bone and
                // compared with the matrix the model shipped: if they do not
                // agree the formula is wrong, and so would the generated bone
                // be.
                {
                    Matrix4x4 trial = bones[leg[0]].worldToLocalMatrix
                                      * smr.transform.localToWorldMatrix;
                    float worst = 0f;
                    for (int e = 0; e < 16; e++)
                        worst = Mathf.Max(worst,
                                          Mathf.Abs(trial[e] - binds[leg[0]][e]));
                    if (worst > 0.001f)
                        Debug.LogWarning("KNEE bind matrix test: deviation "
                                         + worst.ToString("0.0000") + " (" + name + ")");
                    else
                        Debug.Log("  KNEE bind matrix test PASSED (" + name + ")");
                }

                for (int i = 0; i < 2 && ok; i++)
                {
                    float plane = KneePlane(verts, w, leg[i]);
                    if (float.IsNaN(plane)) { ok = false; break; }

                    Transform knee = NewBone(bones[leg[i]], KneeBones[i], plane, smr);
                    int kneeIndex = newBones.Count;
                    newBones.Add(knee);
                    newBinds.Add(knee.worldToLocalMatrix * smr.transform.localToWorldMatrix);

                    int moved = 0;
                    for (int v = 0; v < verts.Length; v++)
                    {
                        if (verts[v].y >= plane) continue;
                        BoneWeight bw = w[v];
                        if (Influences(bw, leg[i])) moved++;
                        if (bw.boneIndex0 == leg[i]) bw.boneIndex0 = kneeIndex;
                        if (bw.boneIndex1 == leg[i]) bw.boneIndex1 = kneeIndex;
                        if (bw.boneIndex2 == leg[i]) bw.boneIndex2 = kneeIndex;
                        if (bw.boneIndex3 == leg[i]) bw.boneIndex3 = kneeIndex;
                        w[v] = bw;
                    }
                    Debug.Log("  KNEE " + name + " " + KneeBones[i] + ": plane y "
                        + plane.ToString("0.0000") + " (leg " + LegSpan(verts, w, leg[i])
                        + "), " + moved + " vertices moved");
                }
                if (!ok) continue;

                // A COPY of the mesh: the package's asset is not modified, and
                // there has to be an asset on disk for the prefab to reference.
                Mesh copy = Object.Instantiate(src);
                copy.name = src.name;
                copy.boneWeights = w;
                copy.bindposes = newBinds.ToArray();

                string mp = Art + "/Mesh/" + name + "-" + src.name + ".asset";
                AssetDatabase.DeleteAsset(mp);
                AssetDatabase.CreateAsset(copy, mp);

                smr.sharedMesh = copy;
                smr.bones = newBones.ToArray();
            }
        }

        /// <summary>
        /// The knee's height (in mesh space). THE GAP BETWEEN THE TWO RINGS
        /// nearest the middle of the leg's own vertices.
        /// </summary>
        private static float KneePlane(Vector3[] verts, BoneWeight[] w, int leg)
        {
            List<float> levels = new List<float>();
            float lowest = float.MaxValue, highest = float.MinValue;
            for (int v = 0; v < verts.Length; v++)
            {
                if (!Influences(w[v], leg)) continue;
                float y = verts[v].y;
                if (y < lowest) lowest = y;
                if (y > highest) highest = y;

                bool isNew = true;
                for (int j = 0; j < levels.Count; j++)
                    if (Mathf.Abs(levels[j] - y) < 0.0005f) { isNew = false; break; }
                if (isNew) levels.Add(y);
            }
            if (levels.Count < 3) return float.NaN;

            levels.Sort();
            float middle = (lowest + highest) * 0.5f;

            float below = levels[0], above = levels[levels.Count - 1];
            for (int j = 0; j < levels.Count - 1; j++)
                if (levels[j] <= middle && levels[j + 1] >= middle)
                {
                    below = levels[j];
                    above = levels[j + 1];
                    break;
                }
            return (below + above) * 0.5f;
        }

        /// <summary>The y range of the leg's vertices. Only against this can
        /// you tell whether the plane is in the right place.</summary>
        private static string LegSpan(Vector3[] verts, BoneWeight[] w, int leg)
        {
            float a = float.MaxValue, b = float.MinValue;
            int n = 0;
            for (int v = 0; v < verts.Length; v++)
            {
                if (!Influences(w[v], leg)) continue;
                n++;
                if (verts[v].y < a) a = verts[v].y;
                if (verts[v].y > b) b = verts[v].y;
            }
            return a.ToString("0.000") + ".." + b.ToString("0.000") + " / " + n + " vertices";
        }

        private static bool Influences(BoneWeight bw, int bone)
        {
            return (bw.boneIndex0 == bone && bw.weight0 > 0.01f)
                || (bw.boneIndex1 == bone && bw.weight1 > 0.01f)
                || (bw.boneIndex2 == bone && bw.weight2 > 0.01f)
                || (bw.boneIndex3 == bone && bw.weight3 > 0.01f);
        }

        private static int BoneIndex(Transform[] bones, string name)
        {
            for (int i = 0; i < bones.Length; i++)
                if (bones[i] != null && bones[i].name == name) return i;
            return -1;
        }

        /// <summary>
        /// The knee bone: a child of the leg, at the height of the split
        /// plane, facing THE SAME way as the leg.
        ///
        /// Facing the same way matters: Figure stores the knee's rest angle
        /// relative to the figure and writes it back when sitting. There is no
        /// need to know where the bone's own axes point.
        /// </summary>
        private static Transform NewBone(Transform leg, string name, float planeY,
                                         SkinnedMeshRenderer smr)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(leg, false);

            Vector3 legInMesh = smr.transform.InverseTransformPoint(leg.position);
            go.transform.position = smr.transform.TransformPoint(
                new Vector3(legInMesh.x, planeY, legInMesh.z));
            go.transform.rotation = leg.rotation;
            return go.transform;
        }

        /// <summary>
        /// Writes the leg bones and their BIND angles into the prefab.
        ///
        /// The reason it is written here is timing: right now the model is
        /// definitely in the bind pose (legs down). Trying to read it at run
        /// time meant mistaking a pose the clip had already changed for the
        /// "bind angle" - and that is exactly what happened: the shin was going
        /// in the direction of the thigh instead of downwards.
        /// </summary>
        private static void BindLegs(GameObject model, Figure f)
        {
            List<Transform> thighs = new List<Transform>();
            List<Transform> shins = new List<Transform>();
            foreach (Transform t in model.GetComponentsInChildren<Transform>(true))
            {
                for (int i = 0; i < 2; i++)
                {
                    if (t.name == LegBones[i]) thighs.Add(t);
                    else if (t.name == KneeBones[i]) shins.Add(t);
                }
            }

            Quaternion invRoot = Quaternion.Inverse(model.transform.rotation);
            f.Legs = thighs.ToArray();
            f.LegRest = new Quaternion[f.Legs.Length];
            for (int i = 0; i < f.Legs.Length; i++)
                f.LegRest[i] = invRoot * f.Legs[i].rotation;

            f.Knees = shins.ToArray();
            f.KneeRest = new Quaternion[f.Knees.Length];
            for (int i = 0; i < f.Knees.Length; i++)
                f.KneeRest[i] = invRoot * f.Knees[i].rotation;

            if (f.Knees.Length != 2)
                Debug.LogWarning("A knee bone is missing: " + model.name
                                 + " (" + f.Knees.Length + ")");
        }

        private static void Animate(GameObject model, AnimatorController ctrl,
                                    AnimationClip[] clips)
        {
            // The Animator has to be on THE MODEL'S root: the paths in the
            // clips ("root/torso/arm-left") are written relative to the FBX
            // root.
            Animator a = model.GetComponent<Animator>();
            if (a == null) a = model.AddComponent<Animator>();
            a.runtimeAnimatorController = ctrl;
            a.applyRootMotion = false;
            // There is no need to run the skeleton of a figure that is not
            // visible; in a hall of fourteen tables that makes a difference.
            a.cullingMode = AnimatorCullingMode.CullUpdateTransforms;

            Figure f = model.GetComponent<Figure>();
            if (f == null) f = model.AddComponent<Figure>();
            f.Anim = a;
            f.Clips = clips;
            BindLegs(model, f);
        }

        // =====================================================================
        /// <summary>
        /// The model's bounding box, RELATIVE TO ITS ROOT.
        ///
        /// Renderer.bounds IS NOT USED. On skinned meshes that value comes from
        /// the root bone's space and does not reflect reality: the characters
        /// measured 0.67 m, were scaled by 2.53 accordingly, and were drawn in
        /// the hall as three-metre giants. Transforming the mesh's own bounding
        /// box into the model's root gives the right answer for both kinds of
        /// renderer.
        /// </summary>
        private static Bounds Measure(GameObject go)
        {
            Matrix4x4 toRoot = go.transform.worldToLocalMatrix;
            Bounds b = new Bounds();
            bool any = false;

            foreach (MeshFilter mf in go.GetComponentsInChildren<MeshFilter>(true))
                if (mf.sharedMesh != null)
                    Add(ref b, ref any, mf.sharedMesh.bounds,
                        toRoot * mf.transform.localToWorldMatrix);

            foreach (SkinnedMeshRenderer sk in
                     go.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                if (sk.sharedMesh != null)
                    Add(ref b, ref any, sk.sharedMesh.bounds,
                        toRoot * sk.transform.localToWorldMatrix);

            return any ? b : new Bounds(Vector3.zero, Vector3.one);
        }

        /// <summary>Transforms the local box's eight corners and merges them.</summary>
        private static void Add(ref Bounds b, ref bool any, Bounds local, Matrix4x4 m)
        {
            Vector3 c = local.center, e = local.extents;
            for (int i = 0; i < 8; i++)
            {
                Vector3 p = m.MultiplyPoint3x4(new Vector3(
                    c.x + ((i & 1) == 0 ? -e.x : e.x),
                    c.y + ((i & 2) == 0 ? -e.y : e.y),
                    c.z + ((i & 4) == 0 ? -e.z : e.z)));
                if (!any) { b = new Bounds(p, Vector3.zero); any = true; }
                else b.Encapsulate(p);
            }
        }

        private static float ScaleFor(string name, string folder, Bounds b)
        {
            Target t;
            if (!Targets.TryGetValue(name, out t)
                && !FolderTarget.TryGetValue(folder, out t))
                return 1f;

            float measured = t.Width ? Mathf.Max(b.size.x, b.size.z) : b.size.y;
            if (measured <= 0.0001f) return 1f;
            return t.Metres / measured;
        }

        // =====================================================================
        /// <summary>
        /// Reduces names such as "wood (Instance)" and "Furniture_wood" to a
        /// plain "wood". It also strips the folder prefix, so that on a second
        /// run the tool does not duplicate a material it generated itself as
        /// "Furniture_Furniture_wood".
        /// </summary>
        private static string Clean(string name, string folder)
        {
            int p = name.IndexOf(' ');
            if (p > 0) name = name.Substring(0, p);
            if (folder.Length > 0 && name.StartsWith(folder + "_"))
                name = name.Substring(folder.Length + 1);
            return name.Replace(".", "_");
        }

        private static string FolderOf(string path)
        {
            string[] parts = path.Split('/');
            for (int i = 0; i < parts.Length - 1; i++)
                if (parts[i] == "Art") return parts[i + 1];
            return "";
        }

        /// <summary>
        /// Creates the folder. NOT Directory.CreateDirectory - a folder opened
        /// that way has no counterpart in the asset database and CreateAsset
        /// fails silently.
        /// </summary>
        private static void MakeFolder(string parent, string child)
        {
            if (!AssetDatabase.IsValidFolder(parent + "/" + child))
                AssetDatabase.CreateFolder(parent, child);
        }

        /// <summary>
        /// The pack's own colormap for a folder.
        ///
        /// THE NAME IS MATCHED EXACTLY, and that is a scar. This used to
        /// return the first texture whose PATH CONTAINED "colormap", in
        /// whatever order the asset database happened to hand them over.
        /// The moment tools/art/gen_crowd.py put
        /// `colormap-crowd-fastfood.png` next to `colormap.png`, that became
        /// the first match: every character material in the project was
        /// silently re-pointed at the fast-food crowd texture, the prefabs
        /// were rewritten to match, and the game went on rendering perfectly
        /// - in the wrong clothes, in both cuisines.
        ///
        /// It cost an hour of looking in the wrong place, because the
        /// symptom was "my new texture has no effect" and the cause was
        /// "your new texture replaced the base one". A substring match over
        /// an unordered list is not a lookup; it is a race with the file
        /// system.
        ///
        /// So: the exact file name wins, and if there is no exact match but
        /// there ARE near misses, this says so instead of picking one.
        /// </summary>
        private static string FindTexture(string folder)
        {
            if (!AssetDatabase.IsValidFolder(folder + "/Textures")) return null;

            string exact = null;
            List<string> near = new List<string>();
            foreach (string g in AssetDatabase.FindAssets(
                         "t:Texture2D", new[] { folder + "/Textures" }))
            {
                string p = AssetDatabase.GUIDToAssetPath(g);
                string file = System.IO.Path.GetFileName(p).ToLowerInvariant();
                if (file == "colormap.png") exact = p;
                else if (file.Contains("colormap")) near.Add(p);
            }
            if (exact != null) return exact;
            if (near.Count == 1) return near[0];
            if (near.Count > 1)
            {
                Debug.LogError("PROBLEMS: " + folder + "/Textures has no "
                               + "colormap.png but " + near.Count + " files that "
                               + "look like one (" + string.Join(", ", near)
                               + ") - refusing to guess which one the pack's "
                               + "materials should use");
            }
            return null;
        }
    }
}
