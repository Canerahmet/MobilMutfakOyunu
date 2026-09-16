using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Lokanta.EditorTools
{
    /// <summary>
    /// A headless screenshot out of Unity.
    ///
    /// Why: on the Blender side the render loop proved itself and caught a
    /// real bug on every round. If there is no such loop on the Unity side,
    /// the view layer is being written without ever being looked at.
    ///
    /// THE -nographics FLAG IS NOT USED: that flag turns rendering off
    /// completely. In batch mode the graphics context stays open and the
    /// drawing goes to a RenderTexture.
    ///
    /// To run it:
    ///   tools/unity/shot.ps1
    /// </summary>
    public static class SceneShot
    {
        private const string OutDir = "../tools/art/out/unity";

        [MenuItem("Lokanta/Take a scene screenshot")]
        public static void Capture()
        {
            try
            {
                string dir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", OutDir));
                Directory.CreateDirectory(dir);

                BuildProbeScene();
                // A new name on every run: it removes the risk of opening the
                // same file and reading an old screenshot.
                string stamp = DateTime.Now.ToString("HHmmss");
                string path = Path.Combine(dir, "probe_" + stamp + ".png");
                Shoot(path, 800, 600);

                Debug.Log("=== Lokanta screenshot ===");
                Debug.Log("  script version: v2-basecolor");
                Debug.Log("  written: " + path);
                Debug.Log("  size   : " + new FileInfo(path).Length + " bytes");
                Debug.Log("=== screenshot done ===");

                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogError("PROBLEMS: the screenshot could not be taken -> "
                               + e.GetType().Name + ": " + e.Message);
                if (Application.isBatchMode) EditorApplication.Exit(2);
            }
        }

        /// <summary>
        /// A small probe scene: a floor, three tables, one counter box. Its
        /// point is not the view layer but to prove THE RENDER PATH works.
        /// </summary>
        private static void BuildProbeScene()
        {
            foreach (GameObject go in UnityEngine.Object.FindObjectsByType<GameObject>(
                         FindObjectsSortMode.None))
            {
                if (go.hideFlags == HideFlags.None && go.scene.IsValid())
                    UnityEngine.Object.DestroyImmediate(go);
            }

            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Floor";
            floor.transform.localScale = new Vector3(1.2f, 1f, 1.2f);
            Paint(floor, new Color(0.55f, 0.50f, 0.45f));

            for (int i = 0; i < 3; i++)
            {
                GameObject t = GameObject.CreatePrimitive(PrimitiveType.Cube);
                t.name = "Table" + i;
                t.transform.position = new Vector3(-2f + i * 2f, 0.37f, 0f);
                t.transform.localScale = new Vector3(0.9f, 0.74f, 0.9f);
                Paint(t, new Color(0.42f, 0.26f, 0.15f));
            }

            GameObject counter = GameObject.CreatePrimitive(PrimitiveType.Cube);
            counter.name = "Counter";
            counter.transform.position = new Vector3(0f, 0.45f, 2.6f);
            counter.transform.localScale = new Vector3(3.6f, 0.9f, 0.6f);
            Paint(counter, new Color(0.72f, 0.18f, 0.14f));

            GameObject lightGo = new GameObject("Sun");
            Light sun = lightGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.4f;
            sun.color = new Color(1f, 0.96f, 0.88f);
            lightGo.transform.rotation = Quaternion.Euler(52f, 35f, 0f);
        }

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        private static void Paint(GameObject go, Color c)
        {
            Renderer r = go.GetComponent<Renderer>();
            if (r == null) return;

            // In batch mode the URP Lit variants are not compiled and every
            // lit surface came back the same colour; the material was right on
            // the CPU, the pixel was wrong. Unlit has far fewer variants.
            Shader sh = Shader.Find("Universal Render Pipeline/Unlit");
            if (sh == null) sh = Shader.Find("Universal Render Pipeline/Lit");
            Material m = sh != null ? new Material(sh) : new Material(r.sharedMaterial);

            // URP Lit keeps the colour in _BaseColor, while Material.color
            // writes to _Color and URP does not read that. On the first attempt
            // every object came out the same colour, because no assignment
            // stuck at all.
            bool painted = false;
            if (m.HasProperty(BaseColorId)) { m.SetColor(BaseColorId, c); painted = true; }
            if (m.HasProperty(ColorId)) { m.SetColor(ColorId, c); painted = true; }
            if (!painted)
                Debug.LogWarning("PROBLEMS: no colour property for " + go.name);

            r.sharedMaterial = m;

            // Diagnostics: on the first three rounds the screenshot came out
            // entirely red and it was not clear whether the material or the
            // shader was to blame. We read it back and write it to the log.
            Color back = m.HasProperty(BaseColorId) ? m.GetColor(BaseColorId)
                       : (m.HasProperty(ColorId) ? m.GetColor(ColorId) : Color.clear);
            Debug.Log(string.Format(
                "  paint {0,-8} shader={1} asked=({2:0.00},{3:0.00},{4:0.00}) back=({5:0.00},{6:0.00},{7:0.00})",
                go.name, m.shader != null ? m.shader.name : "NONE",
                c.r, c.g, c.b, back.r, back.g, back.b));
        }

        /// <summary>
        /// The 2.5D camera angle: docs/02 landscape mode, a slight look from
        /// above. It draws to a RenderTexture; no screen is needed.
        /// </summary>
        private static void Shoot(string path, int width, int height)
        {
            GameObject camGo = new GameObject("Camera");
            Camera cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.93f, 0.94f, 0.91f);
            cam.fieldOfView = 34f;

            camGo.transform.position = new Vector3(4.2f, 4.6f, -5.4f);
            camGo.transform.LookAt(new Vector3(0f, 0.6f, 0.4f));

            // Diagnostics: although the material colours were assigned
            // correctly the screenshot still came out entirely red. The camera
            // settings are applied (the background is right), the geometry is
            // right, but the material is being ignored. That points at URP not
            // being active at the moment of the render.
            var current = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;
            var defaultRp = UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline;
            Debug.Log("  pipeline current  = "
                      + (current == null ? "NULL" : current.name));
            Debug.Log("  pipeline default  = "
                      + (defaultRp == null ? "NULL" : defaultRp.name));
            Debug.Log("  quality renderPipeline = "
                      + (QualitySettings.renderPipeline == null
                         ? "NULL" : QualitySettings.renderPipeline.name));

            RenderTexture rt = new RenderTexture(width, height, 24);
            rt.antiAliasing = 4;
            cam.targetTexture = rt;
            cam.Render();

            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = rt;
            Texture2D shot = new Texture2D(width, height, TextureFormat.RGB24, false);
            shot.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            shot.Apply();
            RenderTexture.active = previous;

            // Diagnostics: instead of interpreting the picture ourselves we
            // have Unity read the pixels. Across three runs the file came out
            // byte for byte identical, the materials were assigned correctly
            // and URP was active; this is the only way to tell which stage
            // breaks.
            Color[] samples =
            {
                shot.GetPixel(width / 2, height - 40),   // background
                shot.GetPixel(60, 120),                  // floor, front left
                shot.GetPixel(width / 2, 300),           // the middle table
                shot.GetPixel(width - 140, 380),         // the counter
            };
            string[] names = { "background", "floor", "table", "counter" };
            for (int i = 0; i < samples.Length; i++)
            {
                Debug.Log(string.Format("  pixel {0,-9} = ({1:0.00},{2:0.00},{3:0.00})",
                                        names[i], samples[i].r, samples[i].g, samples[i].b));
            }

            File.WriteAllBytes(path, shot.EncodeToPNG());

            cam.targetTexture = null;
            UnityEngine.Object.DestroyImmediate(shot);
            rt.Release();
            UnityEngine.Object.DestroyImmediate(rt);
        }
    }
}
