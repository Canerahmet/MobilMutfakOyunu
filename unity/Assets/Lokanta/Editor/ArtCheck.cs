using Lokanta.Game;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Lokanta.EditorTools
{
    /// <summary>
    /// Malzeme tanisi. Render'da her model AYNI PEMBE aileye cikiyor ama
    /// malzeme varliklarinin kendisi dogru: shader URP/Lit, desteklenir,
    /// renk ve doku yerinde. Demek ki sorun malzemede degil, CIZIMDE.
    ///
    /// Bu arac ikisini AYIRIYOR: once malzemeleri okuyup yaziyor, sonra
    /// bilinen malzemelerle boyanmis kupleri AYNI cizim yolundan gecirip
    /// goruntuye aliyor. Kupler dogru renkte cikarsa sorun modellerde,
    /// pembe cikarsa cizim yolunda.
    /// </summary>
    public static class ArtCheck
    {
        [MenuItem("Lokanta/Malzeme tanisi")]
        public static void Run()
        {
            Debug.Log("=== Lokanta malzeme tanisi ===");

            RenderPipelineAsset cur = GraphicsSettings.currentRenderPipeline;
            Debug.Log("  boru hatti : " + (cur == null ? "YOK (built-in)" : cur.name));

            UniversalRenderPipelineAssetInfo();

            Shader lit = Shader.Find("Universal Render Pipeline/Lit");
            Debug.Log("  URP/Lit    : "
                      + (lit == null ? "NULL" : "desteklenir=" + lit.isSupported));

            Report("Assets/Lokanta/Art/Malzeme/Mobilya_wood.mat");
            Report("Assets/Lokanta/Art/Malzeme/Mobilya_metal.mat");
            Report("Assets/Lokanta/Art/Malzeme/Karakter_colormap.mat");

            Sizes();
            SitPose();
            Rig("Assets/Lokanta/Art/Karakter/character-male-a.fbx");
            Bench(lit);

            Debug.Log("=== tanisi tamam ===");
        }

        /// <summary>
        /// Oturma durusunun figuru NEREYE koydugunu olcer.
        ///
        /// Gerek duyuldu cunku oturan musteriler zemine oturmus gibi
        /// goründü. Klip govdeyi kendi icinde asagi indiriyorsa figuru
        /// oturak yuksekligine KALDIRMAK gerekiyor; indirmiyorsa
        /// kaldirmak havada birakir. Tahmin yerine olcum.
        /// </summary>
        private static void SitPose()
        {
            GameObject p = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Lokanta/Art/Prefab/Karakter/character-male-a.prefab");
            if (p == null) { Debug.LogWarning("  oturma: prefab yok"); return; }

            foreach (Figure.Pose pose in new[] { Figure.Pose.Idle, Figure.Pose.Sit })
            {
                GameObject inst = Object.Instantiate(p);
                inst.transform.position = Vector3.zero;

                Figure f = inst.GetComponentInChildren<Figure>();
                if (f == null) { Debug.LogWarning("  oturma: Figure yok"); Object.DestroyImmediate(inst); return; }
                f.Sample(pose, 0.4f);

                Renderer[] rs = inst.GetComponentsInChildren<Renderer>(true);
                Bounds b = rs[0].bounds;
                for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);

                Debug.Log(string.Format(
                    "  durus {0,-5} kutu {1:0.00} x {2:0.00} x {3:0.00} m   "
                    + "taban y={4:0.00}  merkez z={5:0.00}",
                    pose, b.size.x, b.size.y, b.size.z, b.min.y, b.center.z));
                Object.DestroyImmediate(inst);
            }
        }

        /// <summary>
        /// Karakterin iskeletini yazar.
        ///
        /// Neden: figurler T DURUSUNDA cikiyor - kol acikligi 1,94 m, yani
        /// boyundan genis. Pakette animasyon klibi YOK, dolayisiyla durus
        /// ya kemikten duzeltilecek ya da disaridan klip gelecek. Once
        /// kemiklerin adi ve sayisi lazim.
        /// </summary>
        private static void Rig(string path)
        {
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (model == null) { Debug.LogWarning("  model yok: " + path); return; }

            ModelImporter im = AssetImporter.GetAtPath(path) as ModelImporter;
            Debug.Log("  iskelet turu : "
                      + (im == null ? "?" : im.animationType.ToString())
                      + "   klip sayisi : "
                      + (im == null ? 0 : im.defaultClipAnimations.Length));

            Animator an = model.GetComponentInChildren<Animator>();
            Debug.Log("  animator     : " + (an == null ? "yok" : "var, insansi="
                      + an.isHuman));

            if (im != null)
                foreach (ModelImporterClipAnimation c in im.defaultClipAnimations)
                    Debug.Log(string.Format("  klip {0,-24} {1:0.0} - {2:0.0} kare",
                                            c.name, c.firstFrame, c.lastFrame));

            int n = 0;
            foreach (Transform t in model.GetComponentsInChildren<Transform>(true))
            {
                if (n++ > 40) { Debug.Log("  ... (kesildi)"); break; }
                string indent = "";
                Transform p = t.parent;
                while (p != null) { indent += "  "; p = p.parent; }
                Debug.Log("  kemik " + indent + t.name);
            }
        }

        /// <summary>
        /// Uretilmis prefablarin SON boyutunu olcer.
        ///
        /// Neden ayri bir olcum: ArtPrefabs kendi hesabini uretim aninda
        /// yaziyor, ama o hesap ham modelin sinir kutusuna dayaniyor.
        /// Deriye bagli aglarda (karakterler) o kutu baglanma duruşundan
        /// geliyor ve gercegi yansitmayabilir. Burada olculen sey, sahneye
        /// konacak olan seyin ta kendisi.
        /// </summary>
        private static void Sizes()
        {
            string[] names =
            {
                "Mobilya/chairCushion", "Mobilya/tableRound",
                "Mobilya/kitchenFridgeLarge", "Mobilya/kitchenStove",
                "Karakter/character-male-a", "Karakter/character-female-b",
            };

            foreach (string n in names)
            {
                GameObject p = AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/Lokanta/Art/Prefab/" + n + ".prefab");
                if (p == null) { Debug.LogWarning("  prefab yok: " + n); continue; }

                GameObject inst = Object.Instantiate(p);
                inst.transform.position = Vector3.zero;
                Renderer[] rs = inst.GetComponentsInChildren<Renderer>(true);
                if (rs.Length == 0) { Object.DestroyImmediate(inst); continue; }

                Bounds b = rs[0].bounds;
                for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
                Debug.Log(string.Format(
                    "  son boyut {0,-32} {1:0.00} x {2:0.00} x {3:0.00} m   taban y={4:0.00}"
                    + "   cizici={5}",
                    n, b.size.x, b.size.y, b.size.z, b.min.y, rs.Length));
                Object.DestroyImmediate(inst);
            }
        }

        /// <summary>
        /// Bilinen renkte dort kup, ayni cizim yolundan. Sirasiyla:
        /// kahve (varlik), gri metal (varlik), dokulu karakter (varlik),
        /// yesil (calisma aninda kurulmus). Sonuncusu KONTROL: zeminler de
        /// boyle kuruluyor ve onlar dogru cikiyor.
        /// </summary>
        private static void Bench(Shader lit)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            string[] paths =
            {
                "Assets/Lokanta/Art/Malzeme/Mobilya_wood.mat",
                "Assets/Lokanta/Art/Malzeme/Mobilya_metal.mat",
                "Assets/Lokanta/Art/Malzeme/Karakter_colormap.mat",
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

            GameShot.Shoot("tani_kupler.png",
                           new Bounds(new Vector3(3.3f, 0.5f, 0f), new Vector3(12f, 2f, 6f)),
                           960, 432);
            Debug.Log("  kupler yazildi : render/tani_kupler.png");
        }

        private static void UniversalRenderPipelineAssetInfo()
        {
            RenderPipelineAsset a = GraphicsSettings.currentRenderPipeline;
            if (a == null) return;
            SerializedObject so = new SerializedObject(a);
            SerializedProperty srpBatch = so.FindProperty("m_UseSRPBatcher");
            Debug.Log("  SRP toplu cizim : "
                      + (srpBatch == null ? "?" : srpBatch.boolValue.ToString()));
        }

        private static void Report(string path)
        {
            Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
            Debug.Log("  " + path.Substring(path.LastIndexOf('/') + 1) + " : " + Describe(m));
        }

        private static string Describe(Material m)
        {
            if (m == null) return "NULL";
            Shader s = m.shader;
            if (s == null) return m.name + " -> shader NULL";
            return string.Format("'{0}' renk={1} doku={2}", s.name,
                m.HasProperty("_BaseColor") ? m.GetColor("_BaseColor").ToString("F2") : "-",
                m.HasProperty("_BaseMap") && m.GetTexture("_BaseMap") != null ? "var" : "yok");
        }
    }
}
