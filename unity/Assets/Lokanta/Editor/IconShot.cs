using System.IO;
using Lokanta.Game;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Lokanta.EditorTools
{
    /// <summary>
    /// Generates the app icon FROM THE GAME'S OWN ASSETS.
    ///
    /// Why a render and not a drawing: an icon drawn in code looks like
    /// nothing inside the game and makes a false promise in the store. The
    /// scene built here is the very one from the game - the same table, the
    /// same chair, the same figure, the same materials. What the player sees
    /// in the icon they see in the game too.
    ///
    /// The framing is close: showing the whole restaurant in a 1024 x 1024
    /// frame turns into a grey smudge once it comes down to 48 dp on a phone
    /// screen. A single table set is still readable even at that size.
    ///
    ///   .\tools\unity\shot.ps1 -Method Lokanta.EditorTools.IconShot.Run
    /// </summary>
    public static class IconShot
    {
        private const string Dir = "Assets/Lokanta/Art/Icons";

        /// <summary>The icon inside the game. Large, for the adaptive mask.</summary>
        private const string PathBig = Dir + "/app-icon.png";

        /// <summary>
        /// THE STORE ICON: 512 x 512, 32 bit, WITH AN ALPHA CHANNEL.
        ///
        /// Play requires this, and the generated file was RGB24 - that is,
        /// there was NO alpha channel and the upload would have been refused.
        /// A separate file, because the size and the format differ; shrinking
        /// the in-game icon and handing it to the store would be squeezing two
        /// requirements into one file.
        /// </summary>
        private const string PathStore = Dir + "/store-icon-512.png";

        private const int Size = 1024;
        private const int StoreSize = 512;

        /// <summary>The icon's ground. Warm, dark, a relative of the interface's accent colour.</summary>
        private static readonly Color Ground = new Color(0.42f, 0.26f, 0.16f);

        [MenuItem("Lokanta/Generate the app icon")]
        public static void Run()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameObject stage = BuildStage();
            if (stage == null) { Debug.LogError("PROBLEMS: the icon scene could not be built"); return; }

            byte[] png = Shoot(stage);
            Object.DestroyImmediate(stage);

            // The leaf has to match Dir. It said "Simge" here while Dir says
            // "Icons"; a folder created under one name and then written to
            // under another is an import that silently never happens.
            if (!AssetDatabase.IsValidFolder(Dir))
                AssetDatabase.CreateFolder("Assets/Lokanta/Art", "Icons");

            string root = System.IO.Path.GetDirectoryName(Application.dataPath);
            File.WriteAllBytes(System.IO.Path.Combine(root, PathBig), png);
            AssetDatabase.ImportAsset(PathBig, ImportAssetOptions.ForceUpdate);

            // THE STORE COPY: 512 x 512, 32 BIT, FULLY OPAQUE.
            //
            // For a while the comment here said "Play requires 32 bit ALPHA"
            // and the justification was wrong. Play wants two things at once: a
            // 32 bit PNG *and* NO TRANSPARENCY. In the generated file the
            // lowest alpha was 205 - semi-transparent pixels left over from the
            // anti-aliasing - and the store listing would have been refused
            // because of them.
            //
            // The launcher icon (1024) STAYS transparent: there transparency is
            // right, and Android applies the adaptive mask itself. Two
            // requirements in two files; squeezing them into one breaks both.
            byte[] store = Flatten(Resize(png, StoreSize));
            File.WriteAllBytes(System.IO.Path.Combine(root, PathStore), store);
            AssetDatabase.ImportAsset(PathStore, ImportAssetOptions.ForceUpdate);

            Configure(PathBig);
            Apply(PathBig);

            Debug.Log("=== Lokanta icon ===\n  game    : " + PathBig
                      + " (" + Size + " x " + Size + ")"
                      + "\n  store   : " + PathStore
                      + " (" + StoreSize + " x " + StoreSize + ", 32 bit, fully opaque)"
                      + "\n=== icon done ===");
        }

        // =====================================================================
        /// <summary>
        /// A single table set: a table, four chairs, a seated customer and a
        /// plate on the table. The hall in miniature.
        /// </summary>
        private static GameObject BuildStage()
        {
            GameObject root = new GameObject("IconStage");

            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.transform.SetParent(root.transform, false);
            floor.transform.localScale = new Vector3(6f, 0.1f, 6f);
            floor.transform.localPosition = new Vector3(0f, -0.05f, 0f);
            floor.GetComponent<Renderer>().sharedMaterial = Flat(new Color(0.31f, 0.22f, 0.15f));

            if (!Place(root, "Furniture/tableRound", Vector3.zero, 0f)) return null;

            for (int k = 0; k < 4; k++)
            {
                float yaw = k * 90f + 45f;
                Vector3 at = Quaternion.Euler(0f, yaw, 0f) * new Vector3(0f, 0f, -0.62f);
                Place(root, "Furniture/chairCushion", at, yaw);
            }

            // The seated figure sits on the FAR side of the table, at 135
            // degrees.
            //
            // Measured: placed on the seat nearest the camera, the figure's
            // BACK filled half the frame and the table was not visible at all.
            // A customer sitting on the far side faces the table, which is to
            // say the camera - their face can be seen and the table stays
            // clear.
            //
            // The sitting clip lowers the body by 0.35 m, so it is raised by
            // the same amount (see RestaurantView.SitLift).
            Seat(root, "Characters/character-female-c", 135f);
            Seat(root, "Characters/character-male-d", 225f);

            Place(root, "Food/plate-dinner", new Vector3(0f, 0.74f, -0.10f), 0f);
            Place(root, "Food/bowl-soup", new Vector3(0.22f, 0.74f, 0.14f), 0f);
            Place(root, "Food/cup-tea", new Vector3(-0.22f, 0.74f, 0.12f), 0f);

            return root;
        }

        private static void Seat(GameObject root, string rel, float yaw)
        {
            GameObject prefab = Load(rel);
            if (prefab == null) return;

            GameObject go = Object.Instantiate(prefab, root.transform);
            go.transform.localPosition =
                Quaternion.Euler(0f, yaw, 0f) * new Vector3(0f, 0f, -0.62f)
                + new Vector3(0f, 0.35f, 0f);
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

            Figure f = go.GetComponentInChildren<Figure>();
            if (f != null) f.Sample(Figure.Pose.Sit, 0.4f);
        }

        private static bool Place(GameObject root, string rel, Vector3 at, float yaw)
        {
            GameObject prefab = Load(rel);
            if (prefab == null) return false;

            GameObject go = Object.Instantiate(prefab, root.transform);
            go.transform.localPosition = at;
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            return true;
        }

        private static GameObject Load(string rel)
        {
            string path = "Assets/Lokanta/Art/Prefab/" + rel + ".prefab";
            GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (go == null) Debug.LogWarning("No prefab for the icon: " + path);
            return go;
        }

        private static Material Flat(Color c)
        {
            Material m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            m.SetColor("_BaseColor", c);
            m.SetFloat("_Smoothness", 0.05f);
            return m;
        }

        // =====================================================================
        /// <summary>
        /// Flattens the alpha into the ground: the result is fully opaque.
        ///
        /// The ground is taken from the image's own top-left corner - the icon
        /// is drawn on a filled background anyway, so that pixel is the icon's
        /// own colour. Writing a fixed colour would silently leave a wrong
        /// border the moment the icon's ground changed.
        /// </summary>
        private static byte[] Flatten(byte[] png)
        {
            Texture2D t = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            t.LoadImage(png);

            Color32[] px = t.GetPixels32();
            if (px.Length == 0) { Object.DestroyImmediate(t); return png; }

            Color32 ground = px[0];
            ground.a = 255;

            for (int i = 0; i < px.Length; i++)
            {
                if (px[i].a == 255) continue;
                float k = px[i].a / 255f;
                px[i] = new Color32(
                    (byte)(px[i].r * k + ground.r * (1f - k)),
                    (byte)(px[i].g * k + ground.g * (1f - k)),
                    (byte)(px[i].b * k + ground.b * (1f - k)),
                    255);
            }

            t.SetPixels32(px);
            t.Apply();
            byte[] outPng = t.EncodeToPNG();
            Object.DestroyImmediate(t);
            return outPng;
        }

        /// <summary>
        /// Shrinks the PNG and re-encodes it. The alpha channel is preserved.
        ///
        /// Bilinear: the icon is a soft render already, and nearest neighbour
        /// produces a comb pattern when shrinking.
        /// </summary>
        private static byte[] Resize(byte[] png, int size)
        {
            Texture2D src = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            src.LoadImage(png);

            RenderTexture rt = new RenderTexture(size, size, 0);
            RenderTexture previous = RenderTexture.active;
            Graphics.Blit(src, rt);
            RenderTexture.active = rt;

            Texture2D dst = new Texture2D(size, size, TextureFormat.RGBA32, false);
            dst.ReadPixels(new Rect(0, 0, size, size), 0, 0);
            dst.Apply();
            RenderTexture.active = previous;

            byte[] outPng = dst.EncodeToPNG();
            Object.DestroyImmediate(src);
            Object.DestroyImmediate(dst);
            Object.DestroyImmediate(rt);
            return outPng;
        }

        private static byte[] Shoot(GameObject stage)
        {
            GameObject sunGo = new GameObject("Sun");
            Light sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.5f;
            sun.color = new Color(1f, 0.95f, 0.88f);
            sun.shadows = LightShadows.Soft;
            sunGo.transform.rotation = Quaternion.Euler(48f, 200f, 0f);

            GameObject fillGo = new GameObject("Fill");
            Light fill = fillGo.AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.intensity = 0.5f;
            fill.color = new Color(0.75f, 0.80f, 0.95f);
            fillGo.transform.rotation = Quaternion.Euler(25f, 35f, 0f);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.34f, 0.30f, 0.28f);

            GameObject camGo = new GameObject("IconCamera");
            Camera cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Ground;
            cam.fieldOfView = 30f;
            cam.aspect = 1f;
            camGo.transform.rotation = Quaternion.Euler(30f, -20f, 0f);

            // The distance is 7.2 m.
            //
            // A closer frame is more striking, but since Android 8 icons are
            // ADAPTIVE: the system CROPS the icon into a circle, a square or a
            // teardrop and only the middle ~66% is guaranteed. At this distance
            // the table set stays inside that safe area.
            Bounds b = new Bounds(new Vector3(0f, 0.72f, 0f), Vector3.one);
            camGo.transform.position = b.center
                - camGo.transform.rotation * Vector3.forward * 7.2f;

            // The icon render is affected by SRP batching too; the same reason
            // as the game screenshot (see GameShot.SrpBatcher).
            RenderTexture rt = new RenderTexture(Size, Size, 24) { antiAliasing = 8 };
            cam.targetTexture = rt;

            bool batcher = SrpBatcher(false);
            try { cam.Render(); }
            finally { SrpBatcher(batcher); }

            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = rt;
            // RGBA32, NOT RGB24.
            //
            // Play requires the store icon to be 32 bit with an alpha channel;
            // because the generated file was RGB24 there was no alpha channel
            // at all and the upload would have been refused. The ground is
            // opaque anyway, so nothing changes in how it looks - the only
            // thing that changes is the PNG's colour type.
            Texture2D shot = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
            shot.ReadPixels(new Rect(0, 0, Size, Size), 0, 0);
            shot.Apply();
            RenderTexture.active = previous;

            byte[] png = shot.EncodeToPNG();

            cam.targetTexture = null;
            Object.DestroyImmediate(camGo);
            Object.DestroyImmediate(sunGo);
            Object.DestroyImmediate(fillGo);
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(shot);
            return png;
        }

        private static bool SrpBatcher(bool on)
        {
            UnityEngine.Rendering.RenderPipelineAsset asset =
                UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;
            if (asset == null) return true;

            SerializedObject so = new SerializedObject(asset);
            SerializedProperty p = so.FindProperty("m_UseSRPBatcher");
            if (p == null) return true;

            bool was = p.boolValue;
            if (was != on) { p.boolValue = on; so.ApplyModifiedPropertiesWithoutUndo(); }
            return was;
        }

        // =====================================================================
        private static void Configure(string path)
        {
            TextureImporter im = AssetImporter.GetAtPath(path) as TextureImporter;
            if (im == null) return;

            // The icon texture IS NOT COMPRESSED and gets no mipmaps: Unity
            // scales it itself during the build, and a spoiled source means a
            // spoiled icon.
            im.textureType = TextureImporterType.Default;
            im.mipmapEnabled = false;
            im.npotScale = TextureImporterNPOTScale.None;
            im.textureCompression = TextureImporterCompression.Uncompressed;
            im.maxTextureSize = Size;
            im.isReadable = false;
            im.SaveAndReimport();
        }

        private static void Apply(string path)
        {
            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tex == null) { Debug.LogWarning("The icon texture could not be loaded"); return; }

            // THE SAME texture is given for every size; Unity scales it itself.
            // Preparing a separate image per size is pointless for a
            // sixty-four-pixel icon.
            //
            // Through NamedBuildTarget: the old BuildTargetGroup signatures were
            // removed in Unity 6 and the icon kinds are now queried through
            // PlatformIconKind.
            // THE ADAPTIVE ICON ONLY.
            //
            // ALL the supported kinds were being filled before, and Unity
            // printed two warnings during the build: "Round icons are
            // deprecated", "Legacy icons are deprecated". The adaptive icon is
            // enough from API 26 onwards and the build's floor is now 29
            // (Android 10, the floor docs/19 writes down) - so the legacy and
            // round slots go to no device at all.
            //
            // A kind that is not filled is also CLEARED: an old icon left
            // behind would bring the warning back on the next build.
            int applied = 0;
            foreach (PlatformIconKind kind in
                     PlayerSettings.GetSupportedIconKinds(NamedBuildTarget.Android))
            {
                PlatformIcon[] icons =
                    PlayerSettings.GetPlatformIcons(NamedBuildTarget.Android, kind);
                bool adaptive = kind.ToString().IndexOf(
                    "Adaptive", System.StringComparison.OrdinalIgnoreCase) >= 0;

                for (int i = 0; i < icons.Length; i++)
                    icons[i].SetTexture(adaptive ? tex : null, 0);
                PlayerSettings.SetPlatformIcons(NamedBuildTarget.Android, kind, icons);
                if (adaptive) applied += icons.Length;
            }
            Debug.Log("  icon    : " + applied + " adaptive Android sizes wired");

            PlayerSettings.SetIcons(NamedBuildTarget.Standalone,
                                    new[] { tex }, IconKind.Any);

            // The splash screen: the Unity badge is compulsory on a Personal
            // licence, but THE BACKGROUND COLOUR is ours. The game's own ground
            // instead of the default dark grey makes the start-up look like
            // part of the game.
            PlayerSettings.SplashScreen.backgroundColor = new Color(0.086f, 0.094f, 0.110f);
            PlayerSettings.SplashScreen.animationMode = PlayerSettings.SplashScreen.AnimationMode.Dolly;
            PlayerSettings.SplashScreen.showUnityLogo = false;

            AssetDatabase.SaveAssets();
        }
    }
}
