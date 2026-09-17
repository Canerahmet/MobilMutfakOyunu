using System.Collections.Generic;
using System.IO;
using Lokanta.Game;
using Lokanta.Game.Ui;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;

namespace Lokanta.EditorTools
{
    /// <summary>
    /// Builds the playable scene: Lokanta > Build the game scene.
    ///
    /// The scene is a GENERATED thing. A scene put together by hand turns into
    /// a binary file where nobody remembers who wired what to where; this is
    /// those connections WRITTEN DOWN and repeatable.
    /// </summary>
    public static class BuildGameScene
    {
        private const string ScenePath = "Assets/Lokanta/Game.unity";
        private const string Prefabs = "Assets/Lokanta/Art/Prefab";
        private const string PanelPath = "Assets/Lokanta/Ui.asset";
        private const string ThemePath =
            "Assets/UI Toolkit/UnityThemes/UnityDefaultRuntimeTheme.tss";

        [MenuItem("Lokanta/Build the game scene")]
        public static void Run()
        {
            SyncContent.Run();
            ArtPrefabs.Run();

            var scene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // --- light --------------------------------------------------------
            // The intensities were found BY MEASURING. In the first build the
            // hall came out dark and the reason was not clear: the preview tool
            // was adding ITS OWN lights on top of the scene's, so the frame I
            // was looking at was twice as bright. Once the tool was fixed the
            // real value became visible.
            GameObject sun = new GameObject("Sun");
            Light light = sun.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.45f;
            light.color = new Color(1f, 0.96f, 0.90f);
            light.shadows = LightShadows.Soft;
            // From the top right: the rooms' left and back walls are tall, and
            // this angle throws the shadow outside the floor plan rather than
            // across it.
            sun.transform.rotation = Quaternion.Euler(52f, 208f, 0f);

            // Ambient light: in a low-poly scene the faces left in shadow fall
            // to pure black and the object loses its silhouette. A flat ambient
            // colour keeps those faces readable.
            RenderSettings.ambientMode =
                UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.29f, 0.31f, 0.36f);

            GameObject fill = new GameObject("Fill");
            Light fillLight = fill.AddComponent<Light>();
            fillLight.type = LightType.Directional;
            fillLight.intensity = 0.55f;
            fillLight.color = new Color(0.72f, 0.78f, 0.92f);
            fillLight.shadows = LightShadows.None;
            fill.transform.rotation = Quaternion.Euler(28f, 40f, 0f);

            // THE EVENING FILL: the hall's own warm light as it darkens
            // outside. During the day it stays off (DayLight turns it on).
            GameObject warm = new GameObject("WarmFill");
            Light warmLight = warm.AddComponent<Light>();
            warmLight.type = LightType.Directional;
            warmLight.intensity = 0f;
            warmLight.color = new Color(1.00f, 0.80f, 0.55f);
            warmLight.shadows = LightShadows.None;
            warmLight.enabled = false;
            warm.transform.rotation = Quaternion.Euler(62f, 20f, 0f);

            // --- camera -------------------------------------------------------
            GameObject camGo = new GameObject("Camera");
            Camera cam = camGo.AddComponent<Camera>();
            cam.tag = "MainCamera";
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.055f, 0.062f, 0.075f);
            CameraRig rig = camGo.AddComponent<CameraRig>();
            camGo.AddComponent<AudioListener>();

            // THE COLOUR GRADE IS SWITCHED ON HERE (docs/19, docs/58).
            //
            // docs/19 has always permitted "at most a light colour grading
            // table" and it was never wired: postProcessData was 0, the
            // default volume profile was untouched, and the game shipped with
            // Unity's raw linear output.
            //
            // ONLY THE ON-TILE EFFECTS. In URP 17 tonemapping, colour
            // adjustments, white balance and split toning are folded into the
            // uber pass and stay on-tile; bloom and depth of field resolve to
            // memory and do not. A published Android benchmark takes a frame
            // from 25 ms to 60.5 ms with bloom on, which is the whole budget
            // twice over - so the profile leaves every one of those neutral
            // and tools/check_grade.py fails the build if one wakes up.
            //
            // THE FRAME COST IS STILL UNMEASURED ON A PHONE. There is no test
            // device; what is measured here is the PICTURE (render/*.png) and
            // the CONTENT of the profile. docs/21 carries the device check.
            UniversalAdditionalCameraData camData =
                camGo.AddComponent<UniversalAdditionalCameraData>();
            camData.renderPostProcessing = true;
            // No anti-aliasing on top of it: the render scale is already 0.8
            // and a second full-screen pass is exactly what this is avoiding.
            camData.antialiasing = AntialiasingMode.None;

            // --- hall --------------------------------------------------------
            GameObject viewGo = new GameObject("Restaurant");
            RestaurantView view = viewGo.AddComponent<RestaurantView>();
            WireArt(view);

            // --- game ---------------------------------------------------------
            GameObject appGo = new GameObject("Game");
            GameApp app = appGo.AddComponent<GameApp>();

            GameObject musicGo = new GameObject("Music");
            musicGo.transform.SetParent(appGo.transform, false);
            musicGo.AddComponent<AudioSource>();
            Music music = musicGo.AddComponent<Music>();

            // --- interface ----------------------------------------------------
            GameObject uiGo = new GameObject("Ui");
            UIDocument doc = uiGo.AddComponent<UIDocument>();
            doc.panelSettings = PanelSettings();
            UiRoot ui = uiGo.AddComponent<UiRoot>();
            ui.Font = AssetDatabase.LoadAssetAtPath<Font>(
                "Assets/Lokanta/Art/Fonts/Rubik.ttf");
            if (ui.Font == null)
                Debug.LogWarning("No font: Assets/Lokanta/Art/Fonts/Rubik.ttf");

            // THE CJK FONT: for Chinese. Rubik carries no CJK.
            ui.FontCJK = AssetDatabase.LoadAssetAtPath<Font>(
                "Assets/Lokanta/Art/Fonts/NotoSansSC-Lokanta.ttf");
            if (ui.FontCJK == null)
                Debug.LogWarning(
                    "No CJK font: Assets/Lokanta/Art/Fonts/NotoSansSC-Lokanta.ttf"
                    + " - if Chinese is chosen every string comes out as empty boxes.");

            app.Ui = ui;
            app.View = view;
            app.Rig = rig;

            // DAYLIGHT: the lights, the camera and the street lamps are
            // gathered into one component. The street lamps are created when
            // RestaurantView is built, which is why this is wired at the END of
            // the scene build and not HERE.
            DayLight day = appGo.AddComponent<DayLight>();
            day.Sun = light;
            day.Fill = fillLight;
            day.Warm = warmLight;
            day.Cam = cam;
            app.Light = day;
            app.Music = music;
            ui.App = app;
            view.App = app;

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AddToBuild();

            Debug.Log("Scene built: " + ScenePath + "  (press Play)");
        }

        // =====================================================================
        /// <summary>
        /// Wires the prefabs into the scene's fields.
        ///
        /// NOT the FBX itself but the prefab ArtPrefabs generates: wired
        /// straight to an FBX, the material remapping never kicks in and
        /// everything comes out magenta under URP. In the prefab both the
        /// material and the scale are already baked in.
        ///
        /// Wired directly rather than through Resources, because everything
        /// put into Resources goes into the build. A model wired here only
        /// comes into memory when this scene is loaded.
        /// </summary>
        private static void WireArt(RestaurantView view)
        {
            // A SQUARE table: four seats sit on four sides. On the round
            // (actually hexagonal) table two of the seats were landing on a
            // corner - a hexagon's sides are at 60 degrees, the seats at 90,
            // and the two cannot be lined up.
            //
            // The folder is "Furniture" on disk (it used to be "Mobilya"), and
            // "Food" (it used to be "Yemek"). A path left behind by that rename
            // loads nothing and the scene comes up with no furniture at all.
            view.TablePrefab = Prefab("Furniture/table");
            view.ChairPrefab = Prefab("Furniture/chairCushion");
            view.StovePrefab = Prefab("Furniture/kitchenStove");
            view.FridgePrefab = Prefab("Furniture/kitchenFridgeLarge");
            view.CounterPrefab = Prefab("Furniture/kitchenCabinet");
            view.ShelfPrefab = Prefab("Furniture/bookcaseClosedDoors");
            view.SinkPrefab = Prefab("Furniture/kitchenSink");
            view.PlantPrefab = Prefab("Furniture/pottedPlant");
            view.PlatePrefab = Prefab("Food/plate-deep");

            // TRANSPARENT MATERIALS: wired as assets.
            //
            // A transparent material built at run time does not get its shader
            // variant into the build and is drawn opaque on the device - a
            // class of bug that is invisible in the editor.
            view.WallMaterial = Mat("custom_wall");
            view.DoorMaterial = Mat("custom_door");
            view.GlassMaterial = Mat("custom_glass");
            view.GlowMaterial = Mat("custom_lightpool");
            view.CeilingGlowMaterial = Mat("custom_ceilinglight");
            view.WaterMaterial = Mat("custom_water");

            // THE INGREDIENTS IN THE COOK'S HAND. Four different
            // vegetables/meats: the same cook at the same station always
            // carries the same thing, so the picture does not flicker, but
            // there is variety across the kitchen.
            view.IngredientPrefabs = new[]
            {
                Prefab("Food/tomato"), Prefab("Food/onion"),
                Prefab("Food/meat-patty"), Prefab("Food/cheese"),
            };

            // THE CROWD WEARS THE CUISINE (tools/art/gen_crowd.py).
            //
            // THE MATERIAL IS THE ONE ArtPrefabs GENERATES, not the one the FBX
            // import produced. There are two, and only the generated one is on
            // the prefabs: `Character_colormap.mat` comes out of the FBX and
            // `Characters_Character_colormap.mat` is what ArtPrefabs writes and
            // then re-points every character prefab at. Wiring the FBX one here
            // made `Tinted` compare against a material NOTHING IN THE SCENE
            // USES - so the crowd was never recoloured and nothing said so.
            //
            // It is handed over by reference rather than matched by name
            // because that is what Tinted keys the swap on, and a name match
            // would also catch Food_colormap - the plates.
            view.CharacterMaterial = Mat("Characters_Character_colormap");
            view.CrowdMapFastfood = Tex("colormap-crowd-fastfood");
            view.CrowdMapTurk = Tex("colormap-crowd-turk");

            List<GameObject> customers = new List<GameObject>();
            foreach (string s in new[] { "a", "b", "c", "d", "e", "f" })
            {
                GameObject f = Prefab("Characters/character-female-" + s);
                GameObject m = Prefab("Characters/character-male-" + s);
                if (f != null) customers.Add(f);
                if (m != null) customers.Add(m);
            }
            view.CustomerPrefabs = customers.ToArray();

            // Staff are SEPARATE figures: in the hall you have to be able to
            // tell who works there and who is a customer. Drawing from the same
            // pool would leave it unclear who the waiter is in a hall of
            // fourteen tables.
            List<GameObject> staff = new List<GameObject>();
            foreach (string s in new[] { "a", "b", "c" })
            {
                GameObject m = Prefab("Characters/character-male-" + s);
                if (m != null) staff.Add(m);
            }
            view.StaffPrefabs = staff.ToArray();

            int missing = 0;
            if (view.TablePrefab == null) missing++;
            if (view.ChairPrefab == null) missing++;
            if (customers.Count == 0) missing++;
            if (missing > 0)
                Debug.LogWarning(missing + " prefabs could not be wired. "
                    + "Has 'python tools/art/import_vendor.py' been run?");
        }

        private static Material Mat(string name)
        {
            Material m = AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/Lokanta/Art/Materials/" + name + ".mat");
            if (m == null)
                Debug.LogError("PROBLEMS: no material: " + name
                               + " (run 'Lokanta/Generate the model prefabs')");
            return m;
        }

        private static Texture2D Tex(string name)
        {
            string path = "Assets/Lokanta/Art/Characters/Textures/" + name + ".png";
            Texture2D t = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (t == null)
                Debug.LogError("PROBLEMS: no texture: " + path
                               + " (run 'python tools/art/gen_crowd.py')");
            return t;
        }

        private static GameObject Prefab(string relative)
        {
            string path = Prefabs + "/" + relative + ".prefab";
            GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (go == null) Debug.LogWarning("No prefab: " + path);
            return go;
        }

        // =====================================================================
        /// <summary>
        /// The UI Toolkit panel setting.
        ///
        /// The scale is PHYSICAL, NOT from a reference resolution.
        ///
        /// "ScaleWithScreenSize" was used before, and that mode follows the
        /// screen's PIXEL COUNT, not its physical size. Measured: on a
        /// 2400x1080 phone a 52-unit touch target came out at 31.7 dp - a third
        /// below Google's 48 dp minimum. By the same arithmetic body text
        /// became 10.4 sp and small text 8.5 sp; that is, every line of
        /// explanatory text was unreadable.
        ///
        /// With ConstantPhysicalSize and a 160 dpi reference, 1 unit is EXACTLY
        /// 1 dp. The numbers in the theme now say what they mean.
        /// </summary>
        private static PanelSettings PanelSettings()
        {
            PanelSettings ps = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelPath);
            if (ps == null)
            {
                ps = ScriptableObject.CreateInstance<PanelSettings>();
                AssetDatabase.CreateAsset(ps, PanelPath);
            }

            ps.scaleMode = PanelScaleMode.ConstantPhysicalSize;
            ps.referenceDpi = 160f;      // 1 unit = 1 dp
            ps.fallbackDpi = 160f;

            // The theme comes from THE FILE IN THE PROJECT.
            //
            // It used to be loaded from the package path:
            //   Packages/com.unity.ui/PackageResources/.../DefaultRuntimeTheme.tss
            // In Unity 6 UI Toolkit is not a package but a built-in module -
            // that path does not exist, LoadAssetAtPath returns null and the
            // panel was left WITH NO THEME. Three separate symptoms came from
            // this one line: no text at all was visible in the build, the
            // Slider was drawn completely empty, and the ScrollView's scroll
            // bar was never drawn - so the player could not tell that a list
            // carried on below.
            ps.themeStyleSheet = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(ThemePath);
            if (ps.themeStyleSheet == null)
                Debug.LogWarning("PROBLEMS: the interface theme could not be loaded: " + ThemePath);

            EditorUtility.SetDirty(ps);
            AssetDatabase.SaveAssets();
            return ps;
        }

        private static void AddToBuild()
        {
            List<EditorBuildSettingsScene> list =
                new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            foreach (EditorBuildSettingsScene s in list)
                if (s.path == ScenePath) return;

            list.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = list.ToArray();
        }
    }
}
