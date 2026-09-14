using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Lokanta.EditorTools
{
    /// <summary>
    /// Unity'den bassiz ekran goruntusu.
    ///
    /// Neden: Blender tarafinda render dongusu kanitlandi ve her turda
    /// gercek hata yakaladi. Unity tarafinda ayni dongu yoksa gorunum
    /// katmani gorulmeden yazilacak demektir.
    ///
    /// -nographics BAYRAGI KULLANILMAZ: o bayrak render'i tamamen kapatiyor.
    /// Toplu kipte grafik baglami acik kaliyor ve RenderTexture'a ciziliyor.
    ///
    /// Calistirma:
    ///   tools/unity/shot.ps1
    /// </summary>
    public static class SceneShot
    {
        private const string OutDir = "../tools/art/out/unity";

        [MenuItem("Lokanta/Sahne goruntusu al")]
        public static void Capture()
        {
            try
            {
                string dir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", OutDir));
                Directory.CreateDirectory(dir);

                BuildProbeScene();
                // Her kosuda yeni ad: ayni dosyayi okuyup eski goruntuyu
                // yorumlama riskini kaldiriyor.
                string stamp = DateTime.Now.ToString("HHmmss");
                string path = Path.Combine(dir, "probe_" + stamp + ".png");
                Shoot(path, 800, 600);

                Debug.Log("=== Lokanta goruntu ===");
                Debug.Log("  betik surumu: v2-basecolor");
                Debug.Log("  yazildi: " + path);
                Debug.Log("  boyut  : " + new FileInfo(path).Length + " bayt");
                Debug.Log("=== goruntu tamam ===");

                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogError("SORUNLAR: goruntu alinamadi -> "
                               + e.GetType().Name + ": " + e.Message);
                if (Application.isBatchMode) EditorApplication.Exit(2);
            }
        }

        /// <summary>
        /// Kucuk bir sinama sahnesi: zemin, uc masa, bir tezgah kutusu.
        /// Amaci gorunum katmani degil, RENDER YOLUNUN calistigini kanitlamak.
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
            floor.name = "Zemin";
            floor.transform.localScale = new Vector3(1.2f, 1f, 1.2f);
            Paint(floor, new Color(0.55f, 0.50f, 0.45f));

            for (int i = 0; i < 3; i++)
            {
                GameObject t = GameObject.CreatePrimitive(PrimitiveType.Cube);
                t.name = "Masa" + i;
                t.transform.position = new Vector3(-2f + i * 2f, 0.37f, 0f);
                t.transform.localScale = new Vector3(0.9f, 0.74f, 0.9f);
                Paint(t, new Color(0.42f, 0.26f, 0.15f));
            }

            GameObject counter = GameObject.CreatePrimitive(PrimitiveType.Cube);
            counter.name = "Tezgah";
            counter.transform.position = new Vector3(0f, 0.45f, 2.6f);
            counter.transform.localScale = new Vector3(3.6f, 0.9f, 0.6f);
            Paint(counter, new Color(0.72f, 0.18f, 0.14f));

            GameObject lightGo = new GameObject("Gunes");
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

            // Toplu kipte URP Lit'in varyantlari derlenmiyor ve isik alan
            // her yuzey ayni geri donus rengini veriyordu; malzeme CPU'da
            // dogruydu, piksel yanlisti. Unlit'in varyant sayisi cok daha az.
            Shader sh = Shader.Find("Universal Render Pipeline/Unlit");
            if (sh == null) sh = Shader.Find("Universal Render Pipeline/Lit");
            Material m = sh != null ? new Material(sh) : new Material(r.sharedMaterial);

            // URP Lit rengi _BaseColor'da tutuyor; Material.color ise _Color'a
            // yaziyor ve URP onu okumuyor. Ilk denemede butun nesneler ayni
            // renge boyandi, cunku hicbir atama tutmadi.
            bool painted = false;
            if (m.HasProperty(BaseColorId)) { m.SetColor(BaseColorId, c); painted = true; }
            if (m.HasProperty(ColorId)) { m.SetColor(ColorId, c); painted = true; }
            if (!painted)
                Debug.LogWarning("SORUNLAR: " + go.name + " icin renk ozelligi yok");

            r.sharedMaterial = m;

            // Tanilama: ilk uc turda goruntu tamamen kirmizi cikti ve sebebi
            // malzeme mi shader mi anlasilmadi. Geri okuyup gunluge yaziyoruz.
            Color back = m.HasProperty(BaseColorId) ? m.GetColor(BaseColorId)
                       : (m.HasProperty(ColorId) ? m.GetColor(ColorId) : Color.clear);
            Debug.Log(string.Format(
                "  boya {0,-8} shader={1} istenen=({2:0.00},{3:0.00},{4:0.00}) geri=({5:0.00},{6:0.00},{7:0.00})",
                go.name, m.shader != null ? m.shader.name : "YOK",
                c.r, c.g, c.b, back.r, back.g, back.b));
        }

        /// <summary>
        /// 2.5D kamera acisi: docs/02 yatay mod, hafif tepeden bakis.
        /// RenderTexture'a ciziliyor; ekran gerekmiyor.
        /// </summary>
        private static void Shoot(string path, int width, int height)
        {
            GameObject camGo = new GameObject("Kamera");
            Camera cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.93f, 0.94f, 0.91f);
            cam.fieldOfView = 34f;

            camGo.transform.position = new Vector3(4.2f, 4.6f, -5.4f);
            camGo.transform.LookAt(new Vector3(0f, 0.6f, 0.4f));

            // Tanilama: malzeme renkleri dogru atandigi halde goruntu
            // tamamen kirmizi cikiyordu. Kamera ayarlari uygulaniyor
            // (arka plan dogru), geometri dogru, ama malzeme yok sayiliyor.
            // Bu, render aninda URP'nin etkin olmamasina isaret ediyor.
            var current = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;
            var defaultRp = UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline;
            Debug.Log("  boru hatti current  = "
                      + (current == null ? "NULL" : current.name));
            Debug.Log("  boru hatti default  = "
                      + (defaultRp == null ? "NULL" : defaultRp.name));
            Debug.Log("  kalite renderPipeline = "
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

            // Tanilama: goruntuyu yorumlamak yerine Unity'ye piksel
            // okutuyoruz. Uc kosuda dosya bayt bayt ayni cikti, malzemeler
            // dogru atanmisti ve URP etkindi; hangi asamada koptugu
            // ancak boyle anlasilir.
            Color[] samples =
            {
                shot.GetPixel(width / 2, height - 40),   // arka plan
                shot.GetPixel(60, 120),                  // zemin sol on
                shot.GetPixel(width / 2, 300),           // orta masa
                shot.GetPixel(width - 140, 380),         // tezgah
            };
            string[] names = { "arkaplan", "zemin", "masa", "tezgah" };
            for (int i = 0; i < samples.Length; i++)
            {
                Debug.Log(string.Format("  piksel {0,-9} = ({1:0.00},{2:0.00},{3:0.00})",
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
