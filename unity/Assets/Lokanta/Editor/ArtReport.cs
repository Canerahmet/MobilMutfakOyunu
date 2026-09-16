using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Lokanta.EditorTools
{
    /// <summary>
    /// Writes down WHAT the imported models actually are: size, triangle
    /// count, bones, animation and MATERIAL COLOURS.
    ///
    /// Why it is needed: you cannot tell what an FBX is from its name. In the
    /// first render all the furniture came out one colour and the size of a
    /// room; rather than guess at the reason, we measure.
    /// </summary>
    public static class ArtReport
    {
        private const string Art = "Assets/Lokanta/Art";

        [MenuItem("Lokanta/Asset report")]
        public static void Run()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("=== Asset report ===");

            foreach (string folder in Directory.GetDirectories(
                         Path.Combine(Application.dataPath, "Lokanta/Art")))
            {
                string name = Path.GetFileName(folder);
                // "Materials" was called "Malzeme" before the folders were
                // renamed to English; the skip has to follow the folder on
                // disk or the material folder gets scanned for models.
                if (name == "Textures" || name == "Materials") continue;

                string[] guids = AssetDatabase.FindAssets(
                    "t:Model", new[] { Art + "/" + name });
                sb.AppendLine("");
                sb.AppendFormat("--- {0} ({1} models) ---\n", name, guids.Length);

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
                                        + "{4} triangles{5}\n",
                            Path.GetFileNameWithoutExtension(path),
                            b.size.x, b.size.y, b.size.z, t,
                            hasRig ? ", rigged" : "");
                        shown++;
                    }
                }

                sb.AppendFormat("  {0} triangles in total, {1} rigged, {2} distinct materials\n",
                                tris, rigged, materials.Count);

                int listed = 0;
                foreach (string m in materials)
                {
                    if (listed++ >= 8) { sb.AppendLine("    ..."); break; }
                    sb.AppendLine("    material: " + m);
                }
            }

            Debug.Log(sb.ToString());
        }
    }
}
