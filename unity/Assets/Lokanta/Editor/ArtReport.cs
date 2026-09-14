using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Lokanta.EditorTools
{
    /// <summary>
    /// Iceri alinan modellerin NE OLDUGUNU yaziyor: olcu, ucgen sayisi,
    /// kemik, animasyon ve MALZEME RENKLERI.
    ///
    /// Neden gerekli: bir FBX'in adindan ne oldugu anlasilmiyor. Ilk
    /// render'da butun mobilya tek renk ve oda boyunda cikti; sebebini
    /// tahmin etmek yerine olcuyoruz.
    /// </summary>
    public static class ArtReport
    {
        private const string Art = "Assets/Lokanta/Art";

        [MenuItem("Lokanta/Varlik raporu")]
        public static void Run()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("=== Varlik raporu ===");

            foreach (string folder in Directory.GetDirectories(
                         Path.Combine(Application.dataPath, "Lokanta/Art")))
            {
                string name = Path.GetFileName(folder);
                if (name == "Textures" || name == "Malzeme") continue;

                string[] guids = AssetDatabase.FindAssets(
                    "t:Model", new[] { Art + "/" + name });
                sb.AppendLine("");
                sb.AppendFormat("--- {0} ({1} model) ---\n", name, guids.Length);

                int rigged = 0, tris = 0, shown = 0;
                HashSet<string> materials = new HashSet<string>();

                foreach (string g in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(g);
                    GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (go == null) continue;

                    bool hasRig = go.GetComponentInChildren<SkinnedMeshRenderer>() != null;
                    if (hasRig) rigged++;

                    Bounds b = new Bounds();
                    bool first = true;
                    int t = 0;

                    foreach (Renderer r in go.GetComponentsInChildren<Renderer>(true))
                    {
                        Mesh mesh = r is SkinnedMeshRenderer sm
                            ? sm.sharedMesh
                            : r.GetComponent<MeshFilter>()?.sharedMesh;
                        if (mesh == null) continue;

                        t += mesh.triangles.Length / 3;
                        if (first) { b = mesh.bounds; first = false; }
                        else b.Encapsulate(mesh.bounds);

                        foreach (Material m in r.sharedMaterials)
                            if (m != null) materials.Add(m.name);
                    }
                    tris += t;

                    if (shown < 5)
                    {
                        sb.AppendFormat("    {0,-26} {1:0.00} x {2:0.00} x {3:0.00} m, "
                                        + "{4} ucgen{5}\n",
                            Path.GetFileNameWithoutExtension(path),
                            b.size.x, b.size.y, b.size.z, t,
                            hasRig ? ", kemikli" : "");
                        shown++;
                    }
                }

                sb.AppendFormat("  toplam {0} ucgen, {1} kemikli, {2} farkli malzeme\n",
                                tris, rigged, materials.Count);

                int listed = 0;
                foreach (string m in materials)
                {
                    if (listed++ >= 8) { sb.AppendLine("    ..."); break; }
                    sb.AppendLine("    malzeme: " + m);
                }
            }

            Debug.Log(sb.ToString());
        }
    }
}
