using System.IO;
using Lokanta.Game;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Lokanta.EditorTools
{
    /// <summary>
    /// Uygulama simgesini OYUNUN KENDI VARLIKLARINDAN uretir.
    ///
    /// Neden cizim degil render: kodla cizilmis bir simge, oyunun icindeki
    /// hicbir seye benzemiyor ve magazada yanlis soz veriyor. Burada
    /// kurulan sahne oyundakinin ta kendisi - ayni masa, ayni sandalye,
    /// ayni figur, ayni malzemeler. Oyuncu simgede gordugu seyi oyunda da
    /// goruyor.
    ///
    /// Cerceve yakin: 1024 x 1024'luk bir karede butun restorani gostermek,
    /// telefon ekraninda 48 dp'ye indiginde gri bir lekeye donuyor. Tek bir
    /// masa takimi o olcekte bile okunuyor.
    ///
    ///   .\tools\unity\shot.ps1 -Method Lokanta.EditorTools.IconShot.Run
    /// </summary>
    public static class IconShot
    {
        private const string Dir = "Assets/Lokanta/Art/Icons";

        /// <summary>Oyunun icindeki simge. Buyuk olcu, uyarlanabilir maske icin.</summary>
        private const string PathBig = Dir + "/app-icon.png";

        /// <summary>
        /// MAGAZA SIMGESI: 512 x 512, 32 bit, ALFA KANALLI.
        ///
        /// Play bunu sart kosuyor ve uretilen dosya RGB24 idi - yani
        /// alfa kanali YOKTU ve yukleme reddedilirdi. Ayri bir dosya,
        /// cunku olcu ve bicim farkli; oyunun icindeki simgeyi kucultup
        /// magazaya vermek, iki gereksinimi tek dosyaya sikistirmak
        /// olurdu.
        /// </summary>
        private const string PathStore = Dir + "/store-icon-512.png";

        private const int Size = 1024;
        private const int StoreSize = 512;

        /// <summary>Simgenin zemini. Sicak, koyu, arayuzun vurgu rengiyle akraba.</summary>
        private static readonly Color Ground = new Color(0.42f, 0.26f, 0.16f);

        [MenuItem("Lokanta/Uygulama simgesini uret")]
        public static void Run()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameObject stage = BuildStage();
            if (stage == null) { Debug.LogError("SORUNLAR: simge sahnesi kurulamadi"); return; }

            byte[] png = Shoot(stage);
            Object.DestroyImmediate(stage);

            if (!AssetDatabase.IsValidFolder(Dir))
                AssetDatabase.CreateFolder("Assets/Lokanta/Art", "Simge");

            string root = System.IO.Path.GetDirectoryName(Application.dataPath);
            File.WriteAllBytes(System.IO.Path.Combine(root, PathBig), png);
            AssetDatabase.ImportAsset(PathBig, ImportAssetOptions.ForceUpdate);

            // MAGAZA KOPYASI: 512 x 512, 32 BIT, TAM OPAK.
            //
            // Buradaki yorum bir sure "Play 32 bit ALFAYI sart kosuyor"
            // diyordu ve gerekce yanlisti. Play iki seyi birden
            // istiyor: 32 bit PNG *ve* SAYDAMLIK YOK. Uretilen dosyada
            // en dusuk alfa 205'ti - kenar yumusatmasindan kalan yari
            // saydam pikseller - ve magaza girisi bu yuzden
            // reddedilirdi.
            //
            // Baslatici simgesi (1024) saydam KALIYOR: orada saydamlik
            // dogru, Android uyarlanabilir maskeyi kendisi uyguluyor.
            // Iki gereksinim ayri dosyada; tek dosyaya sikistirmak
            // ikisini de bozar.
            byte[] store = Flatten(Resize(png, StoreSize));
            File.WriteAllBytes(System.IO.Path.Combine(root, PathStore), store);
            AssetDatabase.ImportAsset(PathStore, ImportAssetOptions.ForceUpdate);

            Configure(PathBig);
            Apply(PathBig);

            Debug.Log("=== Lokanta simge ===\n  oyun    : " + PathBig
                      + " (" + Size + " x " + Size + ")"
                      + "\n  magaza  : " + PathStore
                      + " (" + StoreSize + " x " + StoreSize + ", 32 bit, tam opak)"
                      + "\n=== simge tamam ===");
        }

        // =====================================================================
        /// <summary>
        /// Tek bir masa takimi: masa, dort sandalye, bir oturan musteri ve
        /// masada bir tabak. Salonun ozeti.
        /// </summary>
        private static GameObject BuildStage()
        {
            GameObject root = new GameObject("SimgeSahnesi");

            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.transform.SetParent(root.transform, false);
            floor.transform.localScale = new Vector3(6f, 0.1f, 6f);
            floor.transform.localPosition = new Vector3(0f, -0.05f, 0f);
            floor.GetComponent<Renderer>().sharedMaterial = Flat(new Color(0.31f, 0.22f, 0.15f));

            if (!Place(root, "Mobilya/tableRound", Vector3.zero, 0f)) return null;

            for (int k = 0; k < 4; k++)
            {
                float yaw = k * 90f + 45f;
                Vector3 at = Quaternion.Euler(0f, yaw, 0f) * new Vector3(0f, 0f, -0.62f);
                Place(root, "Mobilya/chairCushion", at, yaw);
            }

            // Oturan figur masanin KARSI tarafinda, 135 derecede.
            //
            // Olculdu: kameraya yakin oturaga konuldugunda figurun SIRTI
            // karenin yarisini kapliyor ve masa hic gorunmuyordu. Karsi
            // tarafta oturan bir musteri masaya, yani kameraya donuk
            // oluyor - hem yuzu goruluyor hem masa acikta kaliyor.
            //
            // Oturma klibi govdeyi 0,35 m indiriyor, o yuzden ayni kadar
            // kaldiriliyor (bkz. RestaurantView.SitLift).
            Seat(root, "Karakter/character-female-c", 135f);
            Seat(root, "Karakter/character-male-d", 225f);

            Place(root, "Yemek/plate-dinner", new Vector3(0f, 0.74f, -0.10f), 0f);
            Place(root, "Yemek/bowl-soup", new Vector3(0.22f, 0.74f, 0.14f), 0f);
            Place(root, "Yemek/cup-tea", new Vector3(-0.22f, 0.74f, 0.12f), 0f);

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
            if (go == null) Debug.LogWarning("Simge icin prefab yok: " + path);
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
        /// PNG'yi kucultup yeniden kodlar. Alfa kanali korunuyor.
        ///
        /// Bilinear: simge zaten yumusak bir render ve kucultmede
        /// en yakin komsu tarak deseni uretir.
        /// </summary>
        /// <summary>
        /// Alfayi zemine yedirir: sonuc tam opak.
        ///
        /// Zemin, goruntunun kendi sol ust kosesinden aliniyor - simge
        /// zaten dolu bir arka plan uzerinde ciziliyor, yani orasi
        /// simgenin kendi rengi. Sabit bir renk yazmak, simgenin zemini
        /// degisince sessizce yanlis bir cerceve birakirdi.
        /// </summary>
        private static byte[] Flatten(byte[] png)
        {
            Texture2D t = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            t.LoadImage(png);

            Color32[] px = t.GetPixels32();
            if (px.Length == 0) { Object.DestroyImmediate(t); return png; }

            Color32 zemin = px[0];
            zemin.a = 255;

            for (int i = 0; i < px.Length; i++)
            {
                if (px[i].a == 255) continue;
                float k = px[i].a / 255f;
                px[i] = new Color32(
                    (byte)(px[i].r * k + zemin.r * (1f - k)),
                    (byte)(px[i].g * k + zemin.g * (1f - k)),
                    (byte)(px[i].b * k + zemin.b * (1f - k)),
                    255);
            }

            t.SetPixels32(px);
            t.Apply();
            byte[] outPng = t.EncodeToPNG();
            Object.DestroyImmediate(t);
            return outPng;
        }

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
            GameObject sunGo = new GameObject("Gunes");
            Light sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.5f;
            sun.color = new Color(1f, 0.95f, 0.88f);
            sun.shadows = LightShadows.Soft;
            sunGo.transform.rotation = Quaternion.Euler(48f, 200f, 0f);

            GameObject fillGo = new GameObject("Dolgu");
            Light fill = fillGo.AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.intensity = 0.5f;
            fill.color = new Color(0.75f, 0.80f, 0.95f);
            fillGo.transform.rotation = Quaternion.Euler(25f, 35f, 0f);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.34f, 0.30f, 0.28f);

            GameObject camGo = new GameObject("SimgeKamerasi");
            Camera cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Ground;
            cam.fieldOfView = 30f;
            cam.aspect = 1f;
            camGo.transform.rotation = Quaternion.Euler(30f, -20f, 0f);

            // Mesafe 7,2 m.
            //
            // Yakin bir cerceve daha etkileyici ama Android 8'den beri
            // simgeler UYARLANABILIR: sistem simgeyi daire, kare ya da
            // damla seklinde KIRPIYOR ve yalnizca ortadaki ~%66 garanti.
            // Bu mesafede masa takimi o guvenli alanin icinde kaliyor.
            Bounds b = new Bounds(new Vector3(0f, 0.72f, 0f), Vector3.one);
            camGo.transform.position = b.center
                - camGo.transform.rotation * Vector3.forward * 7.2f;

            // Simge render'i da SRP toplu ciziminden etkileniyor; oyun
            // goruntusuyle ayni sebep (bkz. GameShot.SrpBatcher).
            RenderTexture rt = new RenderTexture(Size, Size, 24) { antiAliasing = 8 };
            cam.targetTexture = rt;

            bool batcher = SrpBatcher(false);
            try { cam.Render(); }
            finally { SrpBatcher(batcher); }

            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = rt;
            // RGBA32, RGB24 DEGIL.
            //
            // Play magaza simgesinin 32 bit ve alfa kanalli olmasini
            // sart kosuyor; uretilen dosya RGB24 oldugu icin alfa
            // kanali hic yoktu ve yukleme reddedilirdi. Zemin zaten
            // opak, yani gorunusu degismiyor - degisen tek sey PNG'nin
            // renk turu.
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

            // Simge dokusu SIKISTIRILMIYOR ve mipmap almiyor: Unity onu
            // yapi sirasinda kendi olcekliyor, bozuk bir kaynak bozuk bir
            // simge demek.
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
            if (tex == null) { Debug.LogWarning("Simge dokusu yuklenemedi"); return; }

            // Her boyut icin AYNI doku veriliyor; Unity kendisi olcekliyor.
            // Boyut basina ayri gorsel hazirlamak, altmis dort piksellik bir
            // simge icin gereksiz.
            //
            // NamedBuildTarget ile: eski BuildTargetGroup imzalari Unity 6'da
            // kaldirildi ve simge turleri artik PlatformIconKind uzerinden
            // sorgulaniyor.
            // YALNIZCA UYARLANABILIR SIMGE.
            //
            // Once desteklenen BUTUN turler dolduruluyordu ve Unity
            // yapida iki uyari basiyordu: "Round icons are deprecated",
            // "Legacy icons are deprecated". Uyarlanabilir simge API
            // 26'dan itibaren yeterli ve yapinin alt siniri artik 29
            // (Android 10, docs/19'un yazdigi taban) - yani eski ve
            // yuvarlak yuvalar hicbir cihaza gitmiyor.
            //
            // Doldurulmayan tur TEMIZLENIYOR da: birakilan eski bir
            // simge, sonraki yapida uyariyi geri getirirdi.
            int applied = 0;
            foreach (PlatformIconKind kind in
                     PlayerSettings.GetSupportedIconKinds(NamedBuildTarget.Android))
            {
                PlatformIcon[] icons =
                    PlayerSettings.GetPlatformIcons(NamedBuildTarget.Android, kind);
                bool uyarlanabilir = kind.ToString().IndexOf(
                    "Adaptive", System.StringComparison.OrdinalIgnoreCase) >= 0;

                for (int i = 0; i < icons.Length; i++)
                    icons[i].SetTexture(uyarlanabilir ? tex : null, 0);
                PlayerSettings.SetPlatformIcons(NamedBuildTarget.Android, kind, icons);
                if (uyarlanabilir) applied += icons.Length;
            }
            Debug.Log("  simge   : " + applied + " uyarlanabilir Android boyutu baglandi");

            PlayerSettings.SetIcons(NamedBuildTarget.Standalone,
                                    new[] { tex }, IconKind.Any);

            // Acilis ekrani: Unity rozeti Personal lisansta zorunlu, ama
            // ZEMIN RENGI bizim. Varsayilan koyu gri yerine oyunun kendi
            // zemini, acilisi oyunun bir parcasi gibi gosteriyor.
            PlayerSettings.SplashScreen.backgroundColor = new Color(0.086f, 0.094f, 0.110f);
            PlayerSettings.SplashScreen.animationMode = PlayerSettings.SplashScreen.AnimationMode.Dolly;
            PlayerSettings.SplashScreen.showUnityLogo = false;

            AssetDatabase.SaveAssets();
        }
    }
}
