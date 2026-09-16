using System.Collections.Generic;
using UnityEngine;

namespace Lokanta.Game
{
    /// <summary>
    /// A PROCEDURAL MODEL BUILDER: boxes, prisms, slabs.
    ///
    /// Why it exists: until now the scene's furnishings were built either
    /// from ready-made prefabs or from GameObjects one by one. Both cost
    /// the same thing - one draw call per part. The street lamp solved
    /// that once (fifteen boxes -> two meshes); this class opens the same
    /// solution to every piece of furniture.
    ///
    /// GROUPED BY COLOUR. URP/Lit does not read vertex colour, so there is
    /// no way of putting more than one colour into a single mesh. The
    /// builder gathers the parts into separate meshes BY THEIR COLOUR: a
    /// three-colour item is three draws, not a fifteen-part one.
    ///
    /// Every measurement is LOCAL: relative to the builder's root. Placing
    /// it is the caller's business.
    /// </summary>
    public sealed class Modeler
    {
        private sealed class Part
        {
            public readonly List<Vector3> V = new List<Vector3>();
            public readonly List<Vector3> N = new List<Vector3>();
            public readonly List<int> T = new List<int>();
        }

        private readonly Dictionary<Color32, Part> _parts =
            new Dictionary<Color32, Part>();

        private Part Get(Color c)
        {
            Color32 k = c;
            Part p;
            if (!_parts.TryGetValue(k, out p))
            {
                p = new Part();
                _parts[k] = p;
            }
            return p;
        }

        /// <summary>
        /// THE CHAMFER RADIUS, in metres. 0 = off, the boxes are sharp.
        ///
        /// The user's sentence: "the models look very sharp, it would be a
        /// softer picture if the corners were a bit more rounded".
        ///
        /// At this camera distance the chamfer's real job is not to change
        /// the SILHOUETTE - a 3 cm chamfer is a pixel or two on screen. Its
        /// job is to BREAK THE LIGHT along the edge: the chamfer face sits at
        /// a different angle from its two neighbours, so a thin light (or
        /// dark) strip appears along every edge. In a flat-shaded scene that
        /// strip is what gives the softness.
        ///
        /// It can be set per Modeler instance: a caller that wants its own
        /// item sharp writes 0.
        /// </summary>
        public float Chamfer = 0.055f;

        /// <summary>
        /// The chamfer is limited on EVERY axis of the box by the length of
        /// that axis: so that it cannot eat the box even on the thinnest one.
        /// 0.32 = at most 32% of the axis (64% from the two ends).
        /// </summary>
        private const float ChamferRatio = 0.32f;

        /// <summary>
        /// The threshold at which an axis counts as "big" for the chamfer.
        ///
        /// THE GATE DOES NOT LOOK AT THE THINNEST EDGE, IT LOOKS AT HOW MANY
        /// EDGES ARE BIG. In the first version the condition was "the
        /// thinnest edge >= 5 cm", and it ruled out the game's MOST VISIBLE
        /// part: a table top is 0.80 x 0.04 x 0.80, that is, its thinnest
        /// edge is 4 cm. The table top, the counter top, the chair seat, the
        /// shelf - all of them flat slabs and all of them ruled out. The
        /// measurement said the same: across the whole scene the triangle
        /// count rose by only 12%, because the chamfer had never touched the
        /// actual furniture.
        ///
        /// Protecting the thin axis is not the gate's job anyway: the chamfer
        /// is limited on EVERY axis to 22% of that axis, so on a 4 cm top the
        /// vertical chamfer comes down to 0.9 cm by itself. 3 cm across, 0.9
        /// cm vertically - which is exactly the right chamfer for a flat
        /// slab.
        ///
        /// The two-big-edges condition leaves rod-shaped parts out (a railing
        /// baluster, a table leg, a post): they have one long axis, the
        /// chamfer would stay invisible and cost 44 triangles.
        private const float ChamferBigEdge = 0.10f;

        /// <summary>An axis-aligned box about the given centre.</summary>
        public Modeler Box(Vector3 center, Vector3 size, Color c)
        {
            return BoxAt(center, size, Quaternion.identity, c);
        }

        /// <summary>A turned box. For leaning slabs and slanted parts.</summary>
        public Modeler BoxAt(Vector3 center, Vector3 size, Quaternion rot, Color c)
        {
            Part p = Get(c);
            Vector3 h = size * 0.5f;
            Matrix4x4 m = Matrix4x4.TRS(center, rot, Vector3.one);

            int bigEdges = (size.x >= ChamferBigEdge ? 1 : 0)
                           + (size.y >= ChamferBigEdge ? 1 : 0)
                           + (size.z >= ChamferBigEdge ? 1 : 0);
            if (Chamfer > 0.0001f && bigEdges >= 2)
            {
                ChamferedBox(p, m, h, new Vector3(
                    Mathf.Min(Chamfer, size.x * ChamferRatio),
                    Mathf.Min(Chamfer, size.y * ChamferRatio),
                    Mathf.Min(Chamfer, size.z * ChamferRatio)));
                return this;
            }

            // Six faces, each with ITS OWN corners: flat shading, sharp edges.
            // Shared corners would give a soft box, and every model in the
            // game is low-poly.
            Face(p, m, new Vector3(-h.x, -h.y, h.z), new Vector3(h.x, -h.y, h.z),
                new Vector3(h.x, h.y, h.z), new Vector3(-h.x, h.y, h.z));      // +Z
            Face(p, m, new Vector3(h.x, -h.y, -h.z), new Vector3(-h.x, -h.y, -h.z),
                new Vector3(-h.x, h.y, -h.z), new Vector3(h.x, h.y, -h.z));    // -Z
            Face(p, m, new Vector3(h.x, -h.y, h.z), new Vector3(h.x, -h.y, -h.z),
                new Vector3(h.x, h.y, -h.z), new Vector3(h.x, h.y, h.z));      // +X
            Face(p, m, new Vector3(-h.x, -h.y, -h.z), new Vector3(-h.x, -h.y, h.z),
                new Vector3(-h.x, h.y, h.z), new Vector3(-h.x, h.y, -h.z));    // -X
            Face(p, m, new Vector3(-h.x, h.y, h.z), new Vector3(h.x, h.y, h.z),
                new Vector3(h.x, h.y, -h.z), new Vector3(-h.x, h.y, -h.z));    // +Y
            Face(p, m, new Vector3(-h.x, -h.y, -h.z), new Vector3(h.x, -h.y, -h.z),
                new Vector3(h.x, -h.y, h.z), new Vector3(-h.x, -h.y, h.z));    // -Y
            return this;
        }

        /// <summary>
        /// A CHAMFERED BOX: 6 inset faces + 12 edge strips + 8 corner
        /// triangles.
        ///
        /// 44 triangles (a sharp box is 12). The cost is real, so the limit
        /// is not at the caller but HERE: ChamferBigEdge.
        ///
        /// At every corner there are three points - the ones belonging to the
        /// box's three faces:
        ///
        ///     Point(..., axis, ...) = the point that is AT THE EDGE (h) on
        ///     that axis and INSIDE (h - r) on the other two.
        ///
        /// The faces are built from four of those points, the edge strips
        /// from two points each of two neighbouring faces, and the corner
        /// triangles from the three points of one corner.
        ///
        /// TWELVE EDGES EXACTLY ONCE: the (axis, s) face is paired only with
        /// the two faces of axis `c`. As the three axes come round, all
        /// 3 x 2 x 2 = 12 edges are covered once and none of them twice.
        ///
        /// THE WINDING IS NOT WRITTEN BY HAND, IT IS WORKED OUT. Writing the
        /// winding of twenty-six surfaces correctly by hand meant one of them
        /// coming out reversed and producing an inward-facing surface - and a
        /// reversed surface becomes INVISIBLE without raising any error at
        /// all. Because the shape is convex and its centre is at the local
        /// origin, the test is simple: a surface's normal has to point AWAY
        /// from its own centre; if it does not, the order is reversed.
        /// </summary>
        private static void ChamferedBox(Part p, Matrix4x4 m, Vector3 h, Vector3 r)
        {
            Vector3 inner = new Vector3(h.x - r.x, h.y - r.y, h.z - r.z);

            for (int axis = 0; axis < 3; axis++)
            {
                int b = (axis + 1) % 3;
                int c = (axis + 2) % 3;

                for (int s = -1; s <= 1; s += 2)
                {
                    OutwardFace(p, m,
                        Point(h, inner, axis, s, b, -1, c, -1),
                        Point(h, inner, axis, s, b, 1, c, -1),
                        Point(h, inner, axis, s, b, 1, c, 1),
                        Point(h, inner, axis, s, b, -1, c, 1));

                    for (int sc = -1; sc <= 1; sc += 2)
                        OutwardFace(p, m,
                            Point(h, inner, axis, s, b, -1, c, sc),
                            Point(h, inner, axis, s, b, 1, c, sc),
                            Point(h, inner, c, sc, axis, s, b, 1),
                            Point(h, inner, c, sc, axis, s, b, -1));
                }
            }

            for (int sx = -1; sx <= 1; sx += 2)
                for (int sy = -1; sy <= 1; sy += 2)
                    for (int sz = -1; sz <= 1; sz += 2)
                        OutwardTriangle(p, m,
                            new Vector3(sx * h.x, sy * inner.y, sz * inner.z),
                            new Vector3(sx * inner.x, sy * h.y, sz * inner.z),
                            new Vector3(sx * inner.x, sy * inner.y, sz * h.z));
        }

        /// <summary>
        /// A corner point of the chamfered box: at the edge (h) on the
        /// <paramref name="axis"/> axis, inside (h - r) on the other two.
        /// </summary>
        private static Vector3 Point(Vector3 h, Vector3 inner,
                                     int axis, int sAxis,
                                     int b, int sb, int c, int sc)
        {
            Vector3 v = Vector3.zero;
            v[axis] = sAxis * h[axis];
            v[b] = sb * inner[b];
            v[c] = sc * inner[c];
            return v;
        }

        /// <summary>Adds a quad, fixing the winding so that it faces OUTWARDS.</summary>
        private static void OutwardFace(Part p, Matrix4x4 m,
                                      Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            Vector3 n = Vector3.Cross(b - a, c - a);
            Vector3 center = (a + b + c + d) * 0.25f;
            if (Vector3.Dot(n, center) < 0f) Face(p, m, d, c, b, a);
            else Face(p, m, a, b, c, d);
        }

        /// <summary>Adds a triangle, fixing the winding so that it faces OUTWARDS.</summary>
        private static void OutwardTriangle(Part p, Matrix4x4 m,
                                        Vector3 a, Vector3 b, Vector3 c)
        {
            Vector3 n = Vector3.Cross(b - a, c - a);
            if (Vector3.Dot(n, (a + b + c) / 3f) < 0f) Triangle(p, m, c, b, a);
            else Triangle(p, m, a, b, c);
        }

        private static void Triangle(Part p, Matrix4x4 m, Vector3 a, Vector3 b, Vector3 c)
        {
            int i = p.V.Count;
            Vector3 pa = m.MultiplyPoint3x4(a);
            Vector3 pb = m.MultiplyPoint3x4(b);
            Vector3 pc = m.MultiplyPoint3x4(c);
            Vector3 n = Vector3.Cross(pb - pa, pc - pa).normalized;
            p.V.Add(pa); p.V.Add(pb); p.V.Add(pc);
            p.N.Add(n); p.N.Add(n); p.N.Add(n);
            p.T.Add(i); p.T.Add(i + 2); p.T.Add(i + 1);
        }

        private static void Face(Part p, Matrix4x4 m,
                                Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            int i = p.V.Count;
            Vector3 pa = m.MultiplyPoint3x4(a);
            Vector3 pb = m.MultiplyPoint3x4(b);
            Vector3 pc = m.MultiplyPoint3x4(c);
            Vector3 pd = m.MultiplyPoint3x4(d);
            Vector3 n = Vector3.Cross(pb - pa, pc - pa).normalized;
            p.V.Add(pa); p.V.Add(pb); p.V.Add(pc); p.V.Add(pd);
            p.N.Add(n); p.N.Add(n); p.N.Add(n); p.N.Add(n);
            p.T.Add(i); p.T.Add(i + 2); p.T.Add(i + 1);
            p.T.Add(i); p.T.Add(i + 3); p.T.Add(i + 2);
        }

        /// <summary>
        /// A many-sided prism; its base at the given point, running along +Y.
        ///
        /// Eight sides for a column and a lampshade, six for a plant pot; and
        /// four sides means a turned box.
        /// </summary>
        public Modeler Prism(int sides, float rBottom, float rTop, float height,
                             Vector3 at, Quaternion rot, Color c, bool caps = true)
        {
            Part p = Get(c);
            Matrix4x4 m = Matrix4x4.TRS(at, rot, Vector3.one);

            for (int i = 0; i < sides; i++)
            {
                float a0 = Mathf.PI * 2f * i / sides;
                float a1 = Mathf.PI * 2f * (i + 1) / sides;
                Vector3 p0 = m.MultiplyPoint3x4(
                    new Vector3(Mathf.Cos(a0) * rBottom, 0f, Mathf.Sin(a0) * rBottom));
                Vector3 p1 = m.MultiplyPoint3x4(
                    new Vector3(Mathf.Cos(a1) * rBottom, 0f, Mathf.Sin(a1) * rBottom));
                Vector3 p2 = m.MultiplyPoint3x4(
                    new Vector3(Mathf.Cos(a0) * rTop, height, Mathf.Sin(a0) * rTop));
                Vector3 p3 = m.MultiplyPoint3x4(
                    new Vector3(Mathf.Cos(a1) * rTop, height, Mathf.Sin(a1) * rTop));

                int b = p.V.Count;
                p.V.Add(p0); p.V.Add(p1); p.V.Add(p2); p.V.Add(p3);
                Vector3 n = Vector3.Cross(p1 - p0, p2 - p0).normalized;
                p.N.Add(n); p.N.Add(n); p.N.Add(n); p.N.Add(n);
                p.T.Add(b); p.T.Add(b + 2); p.T.Add(b + 1);
                p.T.Add(b + 1); p.T.Add(b + 2); p.T.Add(b + 3);
            }

            if (!caps) return this;
            Cap(p, m, sides, rTop, height, true);
            Cap(p, m, sides, rBottom, 0f, false);
            return this;
        }

        private static void Cap(Part p, Matrix4x4 m, int sides, float r,
                                  float y, bool top)
        {
            if (r <= 0.0001f) return;
            int b = p.V.Count;
            Vector3 n = m.MultiplyVector(top ? Vector3.up : Vector3.down);
            for (int i = 0; i < sides; i++)
            {
                float a = Mathf.PI * 2f * i / sides;
                p.V.Add(m.MultiplyPoint3x4(
                    new Vector3(Mathf.Cos(a) * r, y, Mathf.Sin(a) * r)));
                p.N.Add(n);
            }
            for (int i = 1; i < sides - 1; i++)
            {
                if (top) { p.T.Add(b); p.T.Add(b + i); p.T.Add(b + i + 1); }
                else { p.T.Add(b); p.T.Add(b + i + 1); p.T.Add(b + i); }
            }
        }

        /// <summary>
        /// Builds the meshes and hangs them in the scene. One draw per
        /// colour.
        ///
        /// THE GLOWING-PART BRANCH WAS REMOVED. There were `glowMat` /
        /// `glowColor` parameters, but no call ever passed more than four
        /// arguments: the glowing parts (the sign, the neon) are built with a
        /// separate Modeler instance and a separate material (_decorGlow).
        /// The dead branch was misleading - it read as "a glowing part gets a
        /// separate material", when in fact it never ran.
        /// </summary>
        public GameObject Build(Transform parent, string name, Material mat,
                                MaterialPropertyBlock block)
        {
            GameObject root = new GameObject(name);
            root.transform.SetParent(parent, false);

            foreach (KeyValuePair<Color32, Part> kv in _parts)
            {
                Part p = kv.Value;
                if (p.V.Count == 0) continue;

                Mesh mesh = new Mesh();
                mesh.name = name + "_" + kv.Key.r + "_" + kv.Key.g + "_" + kv.Key.b;
                mesh.SetVertices(p.V);
                mesh.SetNormals(p.N);
                mesh.SetTriangles(p.T, 0);
                mesh.RecalculateBounds();

                GameObject go = new GameObject(mesh.name);
                go.transform.SetParent(root.transform, false);
                go.AddComponent<MeshFilter>().sharedMesh = mesh;

                // THE MESH IS OWNED. A Mesh is a UnityEngine.Object and it DOES
                // NOT FOLLOW the GameObject when that is destroyed; OwnedMesh
                // ties it to the object's lifetime. The full reasoning is in
                // OwnedMesh.cs.
                go.AddComponent<OwnedMesh>().Mesh = mesh;
                MeshRenderer r = go.AddComponent<MeshRenderer>();
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.receiveShadows = false;

                r.sharedMaterial = mat;
                r.GetPropertyBlock(block);
                block.SetColor(Shader.PropertyToID("_BaseColor"), (Color)kv.Key);
                r.SetPropertyBlock(block);
            }
            return root;
        }
    }
}
