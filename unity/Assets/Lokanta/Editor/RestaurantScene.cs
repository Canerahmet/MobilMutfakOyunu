using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Lokanta.EditorTools
{
    /// <summary>
    /// Builds the restaurant layout and renders all four tiers.
    ///
    /// The question it answers: does a fourteen-table restaurant fit on a
    /// phone screen? docs/19 B5 sets the lowest device at 16:9, docs/16 sets
    /// landscape mode. The render is taken at a real phone's aspect ratio.
    ///
    /// The verification scenes use UNLIT materials: docs/24, in the editor's
    /// batch mode the URP Lit variants are not compiled and every lit surface
    /// falls to the same colour. The game itself carries on using Lit.
    /// </summary>
    public static class RestaurantScene
    {
        private const string OutDir = "../tools/art/out/unity";

        // The docs/12 tiers
        private static readonly int[] Tiers = { 4, 7, 10, 14 };

        // The phone's landscape ratio. 20:9 is today's common one, 16:9 is
        // the narrowest we support.
        private const int ShotWidth = 960;
        private const int ShotHeight = 432;    // 20:9
        private const int NarrowHeight = 540;  // 16:9

        // Measurements in metres. docs/24: a table set is a 0.86 m top, 2.1 m
        // with its chairs.
        private const float CellX = 1.85f;
        private const float CellZ = 1.70f;

        [MenuItem("Lokanta/Render the restaurant layout")]
        public static void Capture()
        {
            try
            {
                string dir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", OutDir));
                Directory.CreateDirectory(dir);
                string stamp = DateTime.Now.ToString("HHmmss");

                Debug.Log("=== Lokanta layout ===");

                foreach (int tables in Tiers)
                {
                    Bounds b = Build(tables);
                    string p = Path.Combine(dir, string.Format("layout_{0:00}_{1}.png", tables, stamp));
                    Shoot(p, b, ShotWidth, ShotHeight);
                    Debug.Log(string.Format(
                        "  {0,2} tables  hall {1:0.0} x {2:0.0} m  written {3}",
                        tables, b.size.x, b.size.z, Path.GetFileName(p)));
                }

                // The narrowest supported ratio, at the largest tier
                Bounds wide = Build(14);
                string narrow = Path.Combine(dir, "layout_14_16x9_" + stamp + ".png");
                Shoot(narrow, wide, ShotWidth, NarrowHeight);
                Debug.Log("  14 tables at 16:9 written " + Path.GetFileName(narrow));

                Debug.Log("=== layout done ===");
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogError("PROBLEMS: layout -> " + e.GetType().Name + ": " + e.Message);
                if (Application.isBatchMode) EditorApplication.Exit(2);
            }
        }

        // ---------------------------------------------------------------------
        /// <summary>Builds the tier and returns the hall's bounding box.</summary>
        private static Bounds Build(int tableCount)
        {
            Clear();

            // The tables are laid out on a grid; the hall grows in WIDTH and
            // not in depth, because on a landscape screen width is plentiful
            // and depth is scarce.
            int cols = Mathf.CeilToInt(Mathf.Sqrt(tableCount * 1.9f));
            int rows = Mathf.CeilToInt(tableCount / (float)cols);

            float width = cols * CellX;
            float depth = rows * CellZ;

            // The floor: the hall plus the kitchen strip
            const float kitchenDepth = 2.4f;
            // The floor stretches BACKWARDS so that it covers the kitchen strip
            // as well; in its first form the wall fell outside the floor and
            // stood in mid-air.
            float floorDepth = depth + kitchenDepth + 1.6f;
            float floorCenterZ = (kitchenDepth - 1.6f) * 0.5f;
            GameObject floor = Box("Floor",
                new Vector3(0f, -0.05f, floorCenterZ),
                new Vector3(width + 2.2f, 0.1f, floorDepth),
                new Color(0.55f, 0.50f, 0.45f));

            // The back wall and the kitchen
            float backZ = depth * 0.5f + kitchenDepth * 0.5f;
            float wallZ = floorCenterZ + floorDepth * 0.5f;
            Box("Wall", new Vector3(0f, 1.5f, wallZ),
                new Vector3(width + 2.2f, 3.0f, 0.16f), new Color(0.88f, 0.86f, 0.80f));

            Box("Counter", new Vector3(-width * 0.18f, 0.45f, backZ - 0.9f),
                new Vector3(3.6f, 0.9f, 0.62f), new Color(0.72f, 0.18f, 0.14f));
            Box("Stove", new Vector3(width * 0.22f, 0.43f, backZ - 0.35f),
                new Vector3(1.2f, 0.86f, 0.70f), new Color(0.34f, 0.36f, 0.38f));
            Box("Fridge", new Vector3(width * 0.40f, 0.95f, backZ - 0.35f),
                new Vector3(0.80f, 1.90f, 0.70f), new Color(0.62f, 0.64f, 0.66f));

            // The tables
            int made = 0;
            for (int r = 0; r < rows && made < tableCount; r++)
            {
                int inRow = Mathf.Min(cols, tableCount - made);
                float rowWidth = inRow * CellX;
                for (int c = 0; c < inRow; c++, made++)
                {
                    float x = -rowWidth * 0.5f + CellX * (c + 0.5f);
                    float z = depth * 0.5f - CellZ * (r + 0.5f);
                    Table(made, new Vector3(x, 0f, z));
                }
            }

            // The door: on the front edge of the hall
            Box("Door", new Vector3(width * 0.36f, 1.05f, -depth * 0.5f - 0.8f),
                new Vector3(1.1f, 2.1f, 0.12f), new Color(0.42f, 0.26f, 0.15f));

            Bounds b = new Bounds(new Vector3(0f, 0.6f, floorCenterZ), Vector3.zero);
            b.Encapsulate(new Vector3(-width * 0.5f - 1.1f, 0f, floorCenterZ - floorDepth * 0.5f));
            b.Encapsulate(new Vector3(width * 0.5f + 1.1f, 2.4f, wallZ));
            return b;
        }

        private static void Table(int index, Vector3 at)
        {
            Color wood = new Color(0.42f, 0.26f, 0.15f);
            Color seat = new Color(0.72f, 0.18f, 0.14f);

            Box("Table" + index, at + new Vector3(0f, 0.74f, 0f),
                new Vector3(0.86f, 0.06f, 0.86f), wood);
            Box("TableLeg" + index, at + new Vector3(0f, 0.36f, 0f),
                new Vector3(0.12f, 0.72f, 0.12f), wood);
            Box("Chair" + index + "a", at + new Vector3(0f, 0.44f, 0.62f),
                new Vector3(0.40f, 0.05f, 0.40f), seat);
            Box("Chair" + index + "b", at + new Vector3(0f, 0.44f, -0.62f),
                new Vector3(0.40f, 0.05f, 0.40f), seat);
        }

        // ---------------------------------------------------------------------
        private static void Clear()
        {
            foreach (GameObject go in UnityEngine.Object.FindObjectsByType<GameObject>(
                         FindObjectsSortMode.None))
            {
                if (go.hideFlags == HideFlags.None && go.scene.IsValid())
                    UnityEngine.Object.DestroyImmediate(go);
            }
        }

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        private static GameObject Box(string name, Vector3 pos, Vector3 size, Color c)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.position = pos;
            go.transform.localScale = size;

            Shader sh = Shader.Find("Universal Render Pipeline/Unlit");
            if (sh == null) sh = Shader.Find("Universal Render Pipeline/Lit");
            Material m = new Material(sh);
            if (m.HasProperty(BaseColorId)) m.SetColor(BaseColorId, c);
            if (m.HasProperty(ColorId)) m.SetColor(ColorId, c);
            go.GetComponent<Renderer>().sharedMaterial = m;
            return go;
        }

        /// <summary>
        /// A 2.5D camera that fits the whole hall into the frame.
        /// The angle is fixed; the only thing that changes is the distance. So
        /// when the tiers are compared the perspective stays the same.
        /// </summary>
        private static void Shoot(string path, Bounds target, int width, int height)
        {
            GameObject camGo = new GameObject("Camera");
            Camera cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.93f, 0.94f, 0.91f);
            cam.fieldOfView = 32f;
            cam.aspect = width / (float)height;

            // 2.5D: 30 degrees. On the first attempt it was 38 degrees and was
            // looking almost straight down; the wall read as a tilted panel.
            Quaternion rot = Quaternion.Euler(30f, -16f, 0f);
            Vector3 center = target.center;

            // Not the bounding SPHERE but the projection of the BOX onto the
            // camera's axes. Fitting by the sphere filled 40% of the frame in a
            // wide flat hall and left the rest empty.
            Quaternion inv = Quaternion.Inverse(rot);
            float maxX = 0f, maxY = 0f, maxZ = 0f;
            Vector3 e = target.extents;
            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = new Vector3(
                    (i & 1) == 0 ? -e.x : e.x,
                    (i & 2) == 0 ? -e.y : e.y,
                    (i & 4) == 0 ? -e.z : e.z);
                Vector3 v = inv * corner;
                maxX = Mathf.Max(maxX, Mathf.Abs(v.x));
                maxY = Mathf.Max(maxY, Mathf.Abs(v.y));
                maxZ = Mathf.Max(maxZ, Mathf.Abs(v.z));
            }

            float vFov = cam.fieldOfView * Mathf.Deg2Rad;
            float hFov = 2f * Mathf.Atan(Mathf.Tan(vFov * 0.5f) * cam.aspect);
            float distV = maxY / Mathf.Tan(vFov * 0.5f);
            float distH = maxX / Mathf.Tan(hFov * 0.5f);
            float dist = Mathf.Max(distV, distH) + maxZ + 0.4f;   // a 4% margin
            dist *= 1.04f;

            camGo.transform.position = center - rot * Vector3.forward * dist;
            camGo.transform.rotation = rot;

            // How many pixels a table is on screen: the touch target decision
            // depends on this.
            GameObject probe = GameObject.Find("Table0");
            if (probe != null)
            {
                Vector3 p0 = cam.WorldToScreenPoint(probe.transform.position
                                                    + new Vector3(-0.43f, 0f, 0f));
                Vector3 p1 = cam.WorldToScreenPoint(probe.transform.position
                                                    + new Vector3(0.43f, 0f, 0f));
                float px = Mathf.Abs(p1.x - p0.x);
                Debug.Log(string.Format("    a table is {0:0} pixels on screen (at width {1})",
                                        px, width));
            }

            RenderTexture rt = new RenderTexture(width, height, 24) { antiAliasing = 4 };
            cam.targetTexture = rt;
            cam.Render();

            RenderTexture prev = RenderTexture.active;
            RenderTexture.active = rt;
            Texture2D shot = new Texture2D(width, height, TextureFormat.RGB24, false);
            shot.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            shot.Apply();
            RenderTexture.active = prev;

            File.WriteAllBytes(path, shot.EncodeToPNG());

            cam.targetTexture = null;
            UnityEngine.Object.DestroyImmediate(shot);
            rt.Release();
            UnityEngine.Object.DestroyImmediate(rt);
        }
    }
}
