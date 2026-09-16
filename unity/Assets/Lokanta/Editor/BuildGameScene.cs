using System.Collections.Generic;
using System.IO;
using Lokanta.Game;
using Lokanta.Game.Ui;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace Lokanta.EditorTools
{
    /// <summary>
    /// Oynanabilir sahneyi kurar: Lokanta > Oyun sahnesini kur.
    ///
    /// Sahne URETILEN bir sey. Elle kurulan bir sahne, kimin neyi nereye
    /// bagladigini kimsenin hatirlamadigi bir ikili dosyaya donuyor;
    /// burasi o baglantilarin YAZILI ve tekrar edilebilir hali.
    /// </summary>
    public static class BuildGameScene
    {
        private const string ScenePath = "Assets/Lokanta/Game.unity";
        private const string Prefabs = "Assets/Lokanta/Art/Prefab";
        private const string PanelPath = "Assets/Lokanta/Ui.asset";
        private const string ThemePath =
            "Assets/UI Toolkit/UnityThemes/UnityDefaultRuntimeTheme.tss";

        [MenuItem("Lokanta/Oyun sahnesini kur")]
        public static void Run()
        {
            SyncContent.Run();
            ArtPrefabs.Run();

            var scene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // --- isik ---------------------------------------------------------
            // Siddetler OLCULEREK bulundu. Ilk yapida salon karanlik
            // ciktı ve sebebi anlasilmadi: onizleme araci sahnenin
            // isiklarina KENDI isiklarini ekliyordu, yani gordugum kare
            // iki kat aydinliktı. Arac duzeltildikten sonra gercek deger
            // gorunur oldu.
            GameObject sun = new GameObject("Gunes");
            Light light = sun.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.45f;
            light.color = new Color(1f, 0.96f, 0.90f);
            light.shadows = LightShadows.Soft;
            // Sag ustten: odalarin sol ve arka duvarlari yuksek, bu aci
            // golgeyi kat planinin uzerine degil disina atiyor.
            sun.transform.rotation = Quaternion.Euler(52f, 208f, 0f);

            // Ortam isigi: dusuk poligonlu bir sahnede golgede kalan
            // yuzler tamamen siyaha dusuyor ve nesne siluetini kaybediyor.
            // Duz bir ortam rengi, o yuzleri okunur tutuyor.
            RenderSettings.ambientMode =
                UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.29f, 0.31f, 0.36f);

            GameObject fill = new GameObject("Dolgu");
            Light fillLight = fill.AddComponent<Light>();
            fillLight.type = LightType.Directional;
            fillLight.intensity = 0.55f;
            fillLight.color = new Color(0.72f, 0.78f, 0.92f);
            fillLight.shadows = LightShadows.None;
            fill.transform.rotation = Quaternion.Euler(28f, 40f, 0f);

            // AKSAM ICI DOLGUSU: disarisi kararirken salonun kendi
            // sicak isigi. Gunduz kapali duruyor (DayLight aciyor).
            GameObject warm = new GameObject("SicakDolgu");
            Light warmLight = warm.AddComponent<Light>();
            warmLight.type = LightType.Directional;
            warmLight.intensity = 0f;
            warmLight.color = new Color(1.00f, 0.80f, 0.55f);
            warmLight.shadows = LightShadows.None;
            warmLight.enabled = false;
            warm.transform.rotation = Quaternion.Euler(62f, 20f, 0f);

            // --- kamera -------------------------------------------------------
            GameObject camGo = new GameObject("Kamera");
            Camera cam = camGo.AddComponent<Camera>();
            cam.tag = "MainCamera";
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.055f, 0.062f, 0.075f);
            CameraRig rig = camGo.AddComponent<CameraRig>();
            camGo.AddComponent<AudioListener>();

            // --- salon --------------------------------------------------------
            GameObject viewGo = new GameObject("Restoran");
            RestaurantView view = viewGo.AddComponent<RestaurantView>();
            WireArt(view);

            // --- oyun ---------------------------------------------------------
            GameObject appGo = new GameObject("Oyun");
            GameApp app = appGo.AddComponent<GameApp>();

            GameObject musicGo = new GameObject("Muzik");
            musicGo.transform.SetParent(appGo.transform, false);
            musicGo.AddComponent<AudioSource>();
            Music music = musicGo.AddComponent<Music>();

            // --- arayuz -------------------------------------------------------
            GameObject uiGo = new GameObject("Arayuz");
            UIDocument doc = uiGo.AddComponent<UIDocument>();
            doc.panelSettings = PanelSettings();
            UiRoot ui = uiGo.AddComponent<UiRoot>();
            ui.Font = AssetDatabase.LoadAssetAtPath<Font>(
                "Assets/Lokanta/Art/Fonts/Rubik.ttf");
            if (ui.Font == null)
                Debug.LogWarning("Yazi tipi yok: Assets/Lokanta/Art/Fonts/Rubik.ttf");

            // CJK YAZI TIPI: Cince icin. Rubik CJK tasimiyor.
            ui.FontCJK = AssetDatabase.LoadAssetAtPath<Font>(
                "Assets/Lokanta/Art/Fonts/NotoSansSC-Lokanta.ttf");
            if (ui.FontCJK == null)
                Debug.LogWarning(
                    "CJK yazi tipi yok: Assets/Lokanta/Art/Fonts/NotoSansSC-Lokanta.ttf"
                    + " - Cince secilirse butun metin bos kutu cikar.");

            app.Ui = ui;
            app.View = view;
            app.Rig = rig;

            // GUN ISIGI: isiklar, kamera ve sokak lambalari tek bir
            // bilesende toplaniyor. Sokak lambalari RestaurantView
            // kuruldugunda olusuyor, o yuzden BURADA degil sahne
            // kurulumunun sonunda baglaniyor.
            DayLight gun = appGo.AddComponent<DayLight>();
            gun.Sun = light;
            gun.Fill = fillLight;
            gun.Warm = warmLight;
            gun.Cam = cam;
            app.Light = gun;
            app.Music = music;
            ui.App = app;
            view.App = app;

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AddToBuild();

            Debug.Log("Sahne kuruldu: " + ScenePath + "  (Play'e bas)");
        }

        // =====================================================================
        /// <summary>
        /// Prefablari sahnedeki alanlara baglar.
        ///
        /// FBX'in kendisi DEGIL, ArtPrefabs'in urettigi prefab: FBX'e
        /// dogrudan baglandiginda malzeme yeniden eslemesi devreye girmiyor
        /// ve her sey URP'de magenta cikiyor. Prefabin icinde malzeme de
        /// olcek de pismis durumda.
        ///
        /// Resources yerine dogrudan baglama, cunku Resources'a konan her
        /// sey derlemeye giriyor. Burada baglanan bir model yalnizca bu
        /// sahne yuklendiginde bellege geliyor.
        /// </summary>
        private static void WireArt(RestaurantView view)
        {
            // DORTGEN masa: dort oturak dort kenara oturuyor. Yuvarlak
            // (aslinda altigen) masada oturaklarin ikisi koseye
            // dusuyordu - altigenin kenarlari 60 derecede, oturaklar
            // 90 derecede ve ikisi hizalanamiyor.
            view.TablePrefab = Prefab("Mobilya/table");
            view.ChairPrefab = Prefab("Mobilya/chairCushion");
            view.StovePrefab = Prefab("Mobilya/kitchenStove");
            view.FridgePrefab = Prefab("Mobilya/kitchenFridgeLarge");
            view.CounterPrefab = Prefab("Mobilya/kitchenCabinet");
            view.ShelfPrefab = Prefab("Mobilya/bookcaseClosedDoors");
            view.SinkPrefab = Prefab("Mobilya/kitchenSink");
            view.PlantPrefab = Prefab("Mobilya/pottedPlant");
            view.PlatePrefab = Prefab("Yemek/plate-deep");

            // SAYDAM MALZEMELER: varlik olarak baglaniyor.
            //
            // Calisma aninda kurulan saydam malzemenin golgelendirici
            // varyanti yapiya girmiyor ve cihazda opak ciziliyor -
            // editorde gorunmeyen bir hata sinifi.
            view.WallMaterial = Mat("custom_wall");
            view.DoorMaterial = Mat("custom_door");
            view.GlassMaterial = Mat("custom_glass");
            view.GlowMaterial = Mat("custom_lightpool");
            view.CeilingGlowMaterial = Mat("custom_ceilinglight");
            view.WaterMaterial = Mat("custom_water");

            // ASCININ ELINDEKI MALZEMELER. Dort ayri sebze/et: ayni
            // asci ayni istasyonda hep ayni seyi tasiyor, yani goruntu
            // titremiyor ama mutfakta cesit var.
            view.IngredientPrefabs = new[]
            {
                Prefab("Yemek/tomato"), Prefab("Yemek/onion"),
                Prefab("Yemek/meat-patty"), Prefab("Yemek/cheese"),
            };

            List<GameObject> customers = new List<GameObject>();
            foreach (string s in new[] { "a", "b", "c", "d", "e", "f" })
            {
                GameObject f = Prefab("Karakter/character-female-" + s);
                GameObject m = Prefab("Karakter/character-male-" + s);
                if (f != null) customers.Add(f);
                if (m != null) customers.Add(m);
            }
            view.CustomerPrefabs = customers.ToArray();

            // Personel AYRI figurler: salonda kimin calisan kimin musteri
            // oldugu ayirt edilebilmeli. Ayni havuzdan secmek, on dort
            // masalik bir salonda kimin garson oldugunu belirsizlestirirdi.
            List<GameObject> staff = new List<GameObject>();
            foreach (string s in new[] { "a", "b", "c" })
            {
                GameObject m = Prefab("Karakter/character-male-" + s);
                if (m != null) staff.Add(m);
            }
            view.StaffPrefabs = staff.ToArray();

            int missing = 0;
            if (view.TablePrefab == null) missing++;
            if (view.ChairPrefab == null) missing++;
            if (customers.Count == 0) missing++;
            if (missing > 0)
                Debug.LogWarning(missing + " prefab baglanamadi. "
                    + "'python tools/art/import_vendor.py' calistirilmis mi?");
        }

        private static Material Mat(string ad)
        {
            Material m = AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/Lokanta/Art/Materials/" + ad + ".mat");
            if (m == null)
                Debug.LogError("SORUNLAR: malzeme yok: " + ad
                               + " ('Lokanta/Model prefablarini uret' calistir)");
            return m;
        }

        private static GameObject Prefab(string relative)
        {
            string path = Prefabs + "/" + relative + ".prefab";
            GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (go == null) Debug.LogWarning("Prefab yok: " + path);
            return go;
        }

        // =====================================================================
        /// <summary>
        /// UI Toolkit panel ayari.
        ///
        /// Olcek FIZIKSEL, referans cozunurlukten DEGIL.
        ///
        /// Once "ScaleWithScreenSize" kullaniliyordu ve o kip ekranin
        /// PIKSEL SAYISINI takip ediyor, fiziksel boyutunu degil. Olculdu:
        /// 2400x1080 bir telefonda 52 birimlik bir dokunma hedefi 31,7 dp
        /// cikiyordu - Google'in 48 dp asgarisinin ucte bir altinda. Ayni
        /// hesapla govde yazisi 10,4 sp, kucuk yazi 8,5 sp oluyordu; yani
        /// aciklayici metnin tamami okunamaz haldeydi.
        ///
        /// ConstantPhysicalSize + 160 dpi referansiyla 1 birim TAM 1 dp.
        /// Theme'deki sayilar artik yazdiklari seyi soyluyor.
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
            ps.referenceDpi = 160f;      // 1 birim = 1 dp
            ps.fallbackDpi = 160f;

            // Tema PROJEDEKI dosyadan.
            //
            // Once paket yolundan yukleniyordu:
            //   Packages/com.unity.ui/PackageResources/.../DefaultRuntimeTheme.tss
            // Unity 6'da UI Toolkit bir paket degil, yerlesik modul - o yol
            // yok, LoadAssetAtPath null donuyor ve panel TEMASIZ kaliyordu.
            // Uc ayri belirti bu tek satirdan geliyordu: yapida hicbir yazi
            // gorunmuyordu, Slider bombos ciziliyordu, ve ScrollView'in
            // kaydirma cubugu hic cizilmiyordu - yani oyuncu bir listenin
            // devami oldugunu anlayamiyordu.
            ps.themeStyleSheet = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(ThemePath);
            if (ps.themeStyleSheet == null)
                Debug.LogWarning("SORUNLAR: arayuz temasi yuklenemedi: " + ThemePath);

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
