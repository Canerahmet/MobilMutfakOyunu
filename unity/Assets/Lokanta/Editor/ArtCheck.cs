using Lokanta.Game;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Lokanta.EditorTools
{
    /// <summary>
    /// A material diagnosis. In the render every model comes out in the SAME
    /// PINK family, yet the material assets themselves are right: the shader
    /// is URP/Lit, it is supported, the colour and the texture are in place.
    /// So the problem is not in the material but in the DRAWING.
    ///
    /// This tool SEPARATES the two: first it reads the materials and writes
    /// them down, then it puts cubes painted with those known materials
    /// through the SAME drawing path and photographs them. If the cubes come
    /// out the right colour the problem is in the models; if they come out
    /// pink it is in the drawing path.
    /// </summary>
    public static class ArtCheck
    {
        /// <summary>
        /// How many things are wrong. IT USED TO HAVE NO FAILURE PATH AT ALL.
        ///
        /// This tool was written as a diagnosis - print everything, let a
        /// human read it - and then it was wired into tools/check.py as a
        /// check called "art check". A diagnosis that prints is not a check:
        /// every line here could have said NULL and the run would still have
        /// ended OK, which is CLAUDE.md rule 4 word for word. The printing
        /// stays, because that is what makes a failure diagnosable; what is
        /// added is the part that says no.
        /// </summary>
        private static int _problems;

        private static void Problem(string what)
        {
            _problems++;
            Debug.LogError("PROBLEMS: " + what);
        }

        [MenuItem("Lokanta/Material diagnosis")]
        public static void Run()
        {
            Debug.Log("=== Lokanta material diagnosis ===");
            _problems = 0;

            RenderPipelineAsset cur = GraphicsSettings.currentRenderPipeline;
            Debug.Log("  pipeline   : " + (cur == null ? "NONE (built-in)" : cur.name));
            if (cur == null)
            {
                // Without the URP asset every material falls back to the
                // built-in pipeline's error shader - which is the pink this
                // whole file was written to diagnose.
                Problem("there is no render pipeline asset - the project is "
                        + "drawing with the built-in pipeline");
            }

            UniversalRenderPipelineAssetInfo();

            Shader lit = Shader.Find("Universal Render Pipeline/Lit");
            Debug.Log("  URP/Lit    : "
                      + (lit == null ? "NULL" : "supported=" + lit.isSupported));
            if (lit == null) Problem("URP/Lit is not in the build at all");
            else if (!lit.isSupported) Problem("URP/Lit reports isSupported=false");

            Report("Assets/Lokanta/Art/Materials/Furniture_wood.mat");
            Report("Assets/Lokanta/Art/Materials/Furniture_metal.mat");
            Report("Assets/Lokanta/Art/Materials/Character_colormap.mat");

            Sizes();
            SitPose();
            Rig("Assets/Lokanta/Art/Characters/character-male-a.fbx");
            Bench(lit);

            Debug.Log("=== diagnosis done ===");
            if (_problems > 0)
                Debug.LogError("PROBLEMS: the art diagnosis found " + _problems
                               + " thing(s) wrong");
        }

        /// <summary>
        /// Measures WHERE the sitting pose actually puts the figure.
        ///
        /// It was needed because seated customers looked as though they were
        /// sitting on the floor. If the clip lowers the body within itself,
        /// the figure has to be RAISED to seat height; if it does not, raising
        /// it leaves the figure in mid-air. A measurement instead of a guess.
        /// </summary>
        private static void SitPose()
        {
            GameObject p = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Lokanta/Art/Prefab/Characters/character-male-a.prefab");
            if (p == null)
            {
                Problem("there is no character-male-a prefab - the sitting "
                        + "pose cannot be measured and nothing can be spawned");
                return;
            }

            foreach (Figure.Pose pose in new[] { Figure.Pose.Idle, Figure.Pose.Sit })
            {
                GameObject inst = Object.Instantiate(p);
                inst.transform.position = Vector3.zero;

                Figure f = inst.GetComponentInChildren<Figure>();
                if (f == null)
                {
                    Problem("the character prefab carries no Figure component "
                            + "- poses are set through it, so nothing would "
                            + "sit, walk or work");
                    Object.DestroyImmediate(inst);
                    return;
                }
                f.Sample(pose, 0.4f);

                Renderer[] rs = inst.GetComponentsInChildren<Renderer>(true);
                Bounds b = rs[0].bounds;
                for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);

                Debug.Log(string.Format(
                    "  pose {0,-5} box {1:0.00} x {2:0.00} x {3:0.00} m   "
                    + "base y={4:0.00}  centre z={5:0.00}",
                    pose, b.size.x, b.size.y, b.size.z, b.min.y, b.center.z));
                Object.DestroyImmediate(inst);
            }
        }

        /// <summary>
        /// Writes out the character's skeleton.
        ///
        /// Why: the figures come out in a T POSE - an arm span of 1.94 m, that
        /// is, wider than they are tall. There is NO animation clip in the
        /// package, so the pose will either be fixed from the bones or a clip
        /// will come from outside. First we need the names and the number of
        /// the bones.
        /// </summary>
        private static void Rig(string path)
        {
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (model == null) { Problem("no model at " + path); return; }

            ModelImporter im = AssetImporter.GetAtPath(path) as ModelImporter;
            Debug.Log("  rig type     : "
                      + (im == null ? "?" : im.animationType.ToString())
                      + "   clip count   : "
                      + (im == null ? 0 : im.defaultClipAnimations.Length));

            Animator an = model.GetComponentInChildren<Animator>();
            Debug.Log("  animator     : " + (an == null ? "none" : "present, humanoid="
                      + an.isHuman));
            // NEITHER OF THESE IS ASSERTED, AND THE FIRST ATTEMPT ASSERTED
            // BOTH.
            //
            // It demanded an Animator on the FBX and a humanoid rig, and the
            // truth is "Generic, 32 clips, no Animator" - correct on both
            // counts. The clips ship inside this same model, so there is
            // nothing to retarget and Generic is the cheaper right answer;
            // the Animator belongs to the PREFAB that ArtPrefabs builds, not
            // to the imported model. What matters is that the clips are THERE
            // - a model that lost them plays nothing, and the failure looks
            // like figures standing still rather than like an error.
            if (im != null && im.defaultClipAnimations.Length == 0)
                Problem(path + " imported with no animation clips - the "
                        + "figures would stand still and nothing would say so");

            if (im != null)
                foreach (ModelImporterClipAnimation c in im.defaultClipAnimations)
                    Debug.Log(string.Format("  clip {0,-24} frames {1:0.0} - {2:0.0}",
                                            c.name, c.firstFrame, c.lastFrame));

            int n = 0;
            foreach (Transform t in model.GetComponentsInChildren<Transform>(true))
            {
                if (n++ > 40) { Debug.Log("  ... (cut off)"); break; }
                string indent = "";
                Transform p = t.parent;
                while (p != null) { indent += "  "; p = p.parent; }
                Debug.Log("  bone " + indent + t.name);
            }
        }

        /// <summary>
        /// Measures the FINAL size of the generated prefabs.
        ///
        /// Why a separate measurement: ArtPrefabs writes down its own working
        /// at generation time, but that working rests on the raw model's
        /// bounding box. For skinned meshes (the characters) that box comes
        /// from the bind pose and may not reflect reality. What is measured
        /// here is the very thing that will be placed in the scene.
        /// </summary>
        private static void Sizes()
        {
            // The folders are "Furniture" and "Characters" on disk. They used
            // to be "Mobilya" and "Karakter"; a path left behind by that rename
            // loads nothing and this check turns into a row of warnings that
            // measures nothing at all.
            string[] names =
            {
                "Furniture/chairCushion", "Furniture/tableRound",
                "Furniture/kitchenFridgeLarge", "Furniture/kitchenStove",
                "Characters/character-male-a", "Characters/character-female-b",
            };

            foreach (string n in names)
            {
                GameObject p = AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/Lokanta/Art/Prefab/" + n + ".prefab");
                if (p == null) { Problem("no prefab: " + n); continue; }

                GameObject inst = Object.Instantiate(p);
                inst.transform.position = Vector3.zero;
                Renderer[] rs = inst.GetComponentsInChildren<Renderer>(true);
                if (rs.Length == 0) { Object.DestroyImmediate(inst); continue; }

                Bounds b = rs[0].bounds;
                for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
                Debug.Log(string.Format(
                    "  final size {0,-32} {1:0.00} x {2:0.00} x {3:0.00} m   base y={4:0.00}"
                    + "   renderers={5}",
                    n, b.size.x, b.size.y, b.size.z, b.min.y, rs.Length));

                // THE SCALE COMES FROM THE PREFABS, so the prefabs are where
                // it has to be checked. Every distance in this project - the
                // pedestrian lanes, the seat radius, the door width - was
                // measured against a figure 1.10 m tall. An import setting
                // that changed that would move all of them at once while
                // every other check went on passing, because they all measure
                // against the same moved figure.
                // HEIGHT ONLY, AND THE WIDTH IS DELIBERATELY NOT CHECKED.
                //
                // Every distance in this project - the pedestrian lanes, the
                // seat radius, the door width - was measured against a figure
                // about a metre tall, and an import scale that changed that
                // would move all of them at once while every other check went
                // on passing, because they all measure against the same moved
                // figure. So the height is asserted.
                //
                // The WIDTH was asserted too, for one run, and it was wrong:
                // it reported 1.14 m across for a 1.00 m figure and called it
                // a T pose. A SkinnedMeshRenderer's bounds come from the ROOT
                // BONE and do not follow the animation unless
                // updateWhenOffscreen is set, so this box is the BIND pose
                // whatever pose is sampled - which is exactly what
                // ArtPrefabs' own note says. The measurement is right for
                // SCALE, which the bind pose carries, and meaningless for
                // POSE, which it does not. A guard that cannot tell the
                // difference between "the arms are out" and "the bounds do
                // not animate" is not a guard.
                if (n.StartsWith("Characters/")
                    && (b.size.y < 0.85f || b.size.y > 1.30f))
                    Problem(n + " is " + b.size.y.ToString("0.00")
                            + " m tall in its bind pose; the figures are about "
                            + "a metre and every distance in the game is "
                            + "measured against that");
                if (b.min.y < -0.02f || b.min.y > 0.02f)
                    Problem(n + " has its base at y=" + b.min.y.ToString("0.00")
                            + "; a prefab that is not zeroed floats or sinks "
                            + "wherever it is placed");
                Object.DestroyImmediate(inst);
            }
        }

        /// <summary>
        /// Four cubes in known colours, through the same drawing path. In
        /// order: brown (an asset), grey metal (an asset), a textured
        /// character (an asset), green (built at run time). The last one is the
        /// CONTROL: the floors are built that way too and they come out right.
        /// </summary>
        private static void Bench(Shader lit)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            string[] paths =
            {
                "Assets/Lokanta/Art/Materials/Furniture_wood.mat",
                "Assets/Lokanta/Art/Materials/Furniture_metal.mat",
                "Assets/Lokanta/Art/Materials/Character_colormap.mat",
            };

            for (int i = 0; i < 4; i++)
            {
                GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.transform.position = new Vector3(i * 2.2f, 0.5f, 0f);
                Material m;
                if (i < paths.Length)
                {
                    m = AssetDatabase.LoadAssetAtPath<Material>(paths[i]);
                }
                else
                {
                    m = new Material(lit);
                    m.SetColor("_BaseColor", new Color(0.20f, 0.70f, 0.30f));
                }
                cube.GetComponent<Renderer>().sharedMaterial = m;
            }

            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.transform.position = new Vector3(3.3f, -0.2f, 0f);
            ground.transform.localScale = new Vector3(12f, 0.2f, 6f);
            Material g = new Material(lit);
            g.SetColor("_BaseColor", new Color(0.12f, 0.12f, 0.14f));
            ground.GetComponent<Renderer>().sharedMaterial = g;

            GameShot.Shoot("diagnosis_cubes.png",
                           new Bounds(new Vector3(3.3f, 0.5f, 0f), new Vector3(12f, 2f, 6f)),
                           960, 432);
            Debug.Log("  cubes written : render/diagnosis_cubes.png");
        }

        private static void UniversalRenderPipelineAssetInfo()
        {
            RenderPipelineAsset a = GraphicsSettings.currentRenderPipeline;
            if (a == null) return;
            SerializedObject so = new SerializedObject(a);
            SerializedProperty srpBatch = so.FindProperty("m_UseSRPBatcher");
            Debug.Log("  SRP batching : "
                      + (srpBatch == null ? "?" : srpBatch.boolValue.ToString()));
        }

        private static void Report(string path)
        {
            Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
            Debug.Log("  " + path.Substring(path.LastIndexOf('/') + 1) + " : " + Describe(m));
            if (m == null) Problem("the material " + path + " does not load");
            else if (m.shader == null) Problem(path + " has no shader");
            else if (!m.shader.isSupported)
                Problem(path + " uses " + m.shader.name
                        + ", which reports isSupported=false - this is the pink");
        }

        private static string Describe(Material m)
        {
            if (m == null) return "NULL";
            Shader s = m.shader;
            if (s == null) return m.name + " -> shader NULL";
            return string.Format("'{0}' colour={1} texture={2}", s.name,
                m.HasProperty("_BaseColor") ? m.GetColor("_BaseColor").ToString("F2") : "-",
                m.HasProperty("_BaseMap") && m.GetTexture("_BaseMap") != null ? "yes" : "no");
        }
    }
}
