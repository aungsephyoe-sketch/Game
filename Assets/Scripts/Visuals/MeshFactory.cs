using System.Collections.Generic;
using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>Procedural flat meshes (XZ plane, radius 1, facing +Z) for telegraphs, slashes and shockwaves.</summary>
    public static class MeshFactory
    {
        static readonly Dictionary<string, Mesh> cache = new Dictionary<string, Mesh>();
        static Mesh quad;

        public static Mesh Disc() { return Sector(360f, 0f); }
        public static Mesh Ring(float innerRatio) { return Sector(360f, innerRatio); }

        /// <summary>A pie slice / ring segment centred on +Z. innerRatio 0 = solid.</summary>
        public static Mesh Sector(float angleDegrees, float innerRatio, int segments = 40)
        {
            string key = angleDegrees.ToString("F1") + "_" + innerRatio.ToString("F2") + "_" + segments;
            Mesh m;
            if (cache.TryGetValue(key, out m) && m != null) return m;

            var verts = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();
            float half = angleDegrees * 0.5f * Mathf.Deg2Rad;
            for (int i = 0; i <= segments; i++)
            {
                float t = (float)i / segments;
                float a = -half + t * half * 2f;
                var dir = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));
                verts.Add(dir * innerRatio);
                verts.Add(dir);
                uvs.Add(new Vector2(t, 0f));
                uvs.Add(new Vector2(t, 1f));
                if (i < segments)
                {
                    int b = i * 2;
                    tris.Add(b); tris.Add(b + 1); tris.Add(b + 3);
                    tris.Add(b); tris.Add(b + 3); tris.Add(b + 2);
                }
            }
            m = new Mesh { name = "Sector_" + key };
            m.SetVertices(verts);
            m.SetUVs(0, uvs);
            m.SetTriangles(tris, 0);
            m.RecalculateNormals();
            m.RecalculateBounds();
            cache[key] = m;
            return m;
        }

        static Mesh planarDisc;

        /// <summary>Unit disc (radius 1) with planar UVs (-1..1), so tiling textures lie flat across the ground.</summary>
        public static Mesh PlanarDisc()
        {
            if (planarDisc != null) return planarDisc;
            const int seg = 64;
            var verts = new List<Vector3> { Vector3.zero };
            var uvs = new List<Vector2> { Vector2.zero };
            var tris = new List<int>();
            for (int i = 0; i <= seg; i++)
            {
                float a = i * Mathf.PI * 2f / seg;
                var p = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));
                verts.Add(p);
                uvs.Add(new Vector2(p.x, p.z));
                if (i > 0) { tris.Add(0); tris.Add(i); tris.Add(i + 1); }
            }
            planarDisc = new Mesh { name = "PlanarDisc" };
            planarDisc.SetVertices(verts);
            planarDisc.SetUVs(0, uvs);
            planarDisc.SetTriangles(tris, 0);
            planarDisc.RecalculateNormals();
            planarDisc.RecalculateBounds();
            return planarDisc;
        }

        static Mesh cone;

        /// <summary>Cone with base radius 0.5 at y=0 and apex at y=1 (pines, spikes, roofs).</summary>
        public static Mesh Cone()
        {
            if (cone != null) return cone;
            const int n = 16;
            var v = new List<Vector3>();
            var t = new List<int>();
            v.Add(new Vector3(0f, 1f, 0f));
            for (int i = 0; i < n; i++)
            {
                float a = i * Mathf.PI * 2f / n;
                v.Add(new Vector3(Mathf.Cos(a) * 0.5f, 0f, Mathf.Sin(a) * 0.5f));
            }
            for (int i = 0; i < n; i++)
            {
                int a = 1 + i, b = 1 + (i + 1) % n;
                t.Add(0); t.Add(b); t.Add(a);
            }
            cone = new Mesh { name = "Cone" };
            cone.SetVertices(v);
            cone.SetTriangles(t, 0);
            cone.RecalculateNormals();
            cone.RecalculateBounds();
            return cone;
        }

        static readonly Dictionary<int, Mesh> facetCones = new Dictionary<int, Mesh>();
        static readonly Dictionary<int, Mesh> facetCylinders = new Dictionary<int, Mesh>();
        static readonly Dictionary<int, Mesh> rocks = new Dictionary<int, Mesh>();

        /// <summary>Flat-shaded cone with a base cap (unit height, radius 0.5): low-poly trees, roofs, spires.</summary>
        public static Mesh FacetCone(int sides)
        {
            Mesh m;
            if (facetCones.TryGetValue(sides, out m) && m != null) return m;
            var v = new List<Vector3>();
            var t = new List<int>();
            for (int i = 0; i < sides; i++)
            {
                float a0 = i * Mathf.PI * 2f / sides, a1 = (i + 1) * Mathf.PI * 2f / sides;
                Vector3 p0 = new Vector3(Mathf.Cos(a0) * 0.5f, 0f, Mathf.Sin(a0) * 0.5f);
                Vector3 p1 = new Vector3(Mathf.Cos(a1) * 0.5f, 0f, Mathf.Sin(a1) * 0.5f);
                int b = v.Count;
                v.Add(Vector3.up); v.Add(p1); v.Add(p0);
                t.Add(b); t.Add(b + 1); t.Add(b + 2);
                b = v.Count;
                v.Add(Vector3.zero); v.Add(p0); v.Add(p1);
                t.Add(b); t.Add(b + 1); t.Add(b + 2);
            }
            m = new Mesh { name = "FacetCone" + sides };
            m.SetVertices(v);
            m.SetTriangles(t, 0);
            m.RecalculateNormals();
            m.RecalculateBounds();
            facetCones[sides] = m;
            return m;
        }

        /// <summary>Flat-shaded prism (unit height from 0 to 1, radius 0.5) with caps: pedestals, towers, cliffs.</summary>
        public static Mesh FacetCylinder(int sides)
        {
            Mesh m;
            if (facetCylinders.TryGetValue(sides, out m) && m != null) return m;
            var v = new List<Vector3>();
            var t = new List<int>();
            for (int i = 0; i < sides; i++)
            {
                float a0 = i * Mathf.PI * 2f / sides, a1 = (i + 1) * Mathf.PI * 2f / sides;
                Vector3 p0 = new Vector3(Mathf.Cos(a0) * 0.5f, 0f, Mathf.Sin(a0) * 0.5f);
                Vector3 p1 = new Vector3(Mathf.Cos(a1) * 0.5f, 0f, Mathf.Sin(a1) * 0.5f);
                Vector3 up = Vector3.up;
                int b = v.Count;
                v.Add(p0); v.Add(p1 + up); v.Add(p1); v.Add(p0 + up);
                t.Add(b); t.Add(b + 1); t.Add(b + 2);
                t.Add(b); t.Add(b + 3); t.Add(b + 1);
                b = v.Count;
                v.Add(up); v.Add(p1 + up); v.Add(p0 + up);
                t.Add(b); t.Add(b + 1); t.Add(b + 2);
                b = v.Count;
                v.Add(Vector3.zero); v.Add(p0); v.Add(p1);
                t.Add(b); t.Add(b + 1); t.Add(b + 2);
            }
            m = new Mesh { name = "FacetCylinder" + sides };
            m.SetVertices(v);
            m.SetTriangles(t, 0);
            m.RecalculateNormals();
            m.RecalculateBounds();
            facetCylinders[sides] = m;
            return m;
        }

        /// <summary>A lumpy flat-shaded boulder (about unit size, sitting on y = 0). Variants are cached by seed.</summary>
        public static Mesh Rock(int seed)
        {
            seed = Mathf.Abs(seed) % 8;
            Mesh m;
            if (rocks.TryGetValue(seed, out m) && m != null) return m;
            var rng = new System.Random(seed * 7919 + 13);
            const int rings = 3, seg = 7;
            var pts = new List<Vector3>();
            pts.Add(new Vector3(0f, 0.95f + (float)rng.NextDouble() * 0.15f, 0f));
            for (int r = 1; r <= rings; r++)
            {
                float phi = r * Mathf.PI * 0.5f / rings;
                for (int s = 0; s < seg; s++)
                {
                    float th = s * Mathf.PI * 2f / seg + r * 0.4f;
                    float k = 0.8f + (float)rng.NextDouble() * 0.35f;
                    pts.Add(new Vector3(Mathf.Sin(phi) * Mathf.Cos(th) * 0.55f * k, Mathf.Cos(phi) * 0.95f * k, Mathf.Sin(phi) * Mathf.Sin(th) * 0.55f * k));
                }
            }
            for (int i = 1; i < pts.Count; i++) if (i > pts.Count - seg - 1) pts[i] = new Vector3(pts[i].x, 0f, pts[i].z);
            var v = new List<Vector3>();
            var t = new List<int>();
            System.Action<Vector3, Vector3, Vector3> tri = (a, b, c) => { int n = v.Count; v.Add(a); v.Add(b); v.Add(c); t.Add(n); t.Add(n + 1); t.Add(n + 2); };
            for (int s = 0; s < seg; s++) tri(pts[0], pts[1 + (s + 1) % seg], pts[1 + s]);
            for (int r = 0; r < rings - 1; r++)
                for (int s = 0; s < seg; s++)
                {
                    int a = 1 + r * seg + s, b = 1 + r * seg + (s + 1) % seg;
                    int c = 1 + (r + 1) * seg + s, d = 1 + (r + 1) * seg + (s + 1) % seg;
                    tri(pts[a], pts[b], pts[d]);
                    tri(pts[a], pts[d], pts[c]);
                }
            m = new Mesh { name = "Rock" + seed };
            m.SetVertices(v);
            m.SetTriangles(t, 0);
            m.RecalculateNormals();
            m.RecalculateBounds();
            rocks[seed] = m;
            return m;
        }

        static readonly Dictionary<string, Mesh> lathes = new Dictionary<string, Mesh>();

        /// <summary>Smooth surface of revolution around Y from a (radius, height) profile, bottom to top, capped.</summary>
        public static Mesh Lathe(string key, Vector2[] profile, int segments = 28)
        {
            Mesh m;
            if (lathes.TryGetValue(key, out m) && m != null) return m;
            var v = new List<Vector3>();
            var t = new List<int>();
            int rows = profile.Length;
            for (int r = 0; r < rows; r++)
                for (int s = 0; s <= segments; s++)
                {
                    float a = s * Mathf.PI * 2f / segments;
                    v.Add(new Vector3(Mathf.Cos(a) * profile[r].x, profile[r].y, Mathf.Sin(a) * profile[r].x));
                }
            int stride = segments + 1;
            for (int r = 0; r < rows - 1; r++)
                for (int s = 0; s < segments; s++)
                {
                    int a = r * stride + s, b = a + 1, c = a + stride, d = c + 1;
                    t.Add(a); t.Add(c); t.Add(b);
                    t.Add(b); t.Add(c); t.Add(d);
                }
            // Caps.
            int bottom = v.Count; v.Add(new Vector3(0f, profile[0].y, 0f));
            int top = v.Count; v.Add(new Vector3(0f, profile[rows - 1].y, 0f));
            for (int s = 0; s < segments; s++)
            {
                t.Add(bottom); t.Add(s); t.Add(s + 1);
                int o = (rows - 1) * stride;
                t.Add(top); t.Add(o + s + 1); t.Add(o + s);
            }
            m = new Mesh { name = "Lathe_" + key };
            m.SetVertices(v);
            m.SetTriangles(t, 0);
            m.RecalculateNormals();
            m.RecalculateBounds();
            lathes[key] = m;
            return m;
        }

        static Mesh smoothSphere;

        /// <summary>High-resolution UV sphere (diameter 1, like Unity's primitive) for smooth faces and hair.</summary>
        static readonly Dictionary<int, Mesh> spheres = new Dictionary<int, Mesh>();

        /// <summary>UV sphere (diameter 1) at a chosen resolution, for small parts that don't need the full smooth sphere.</summary>
        public static Mesh Sphere(int lat, int lon)
        {
            int key = lat * 1000 + lon;
            Mesh m;
            if (spheres.TryGetValue(key, out m) && m != null) return m;
            var v = new List<Vector3>();
            var n = new List<Vector3>();
            var t = new List<int>();
            for (int i = 0; i <= lat; i++)
            {
                float phi = Mathf.PI * i / lat;
                for (int j = 0; j <= lon; j++)
                {
                    float th = Mathf.PI * 2f * j / lon;
                    var d = new Vector3(Mathf.Sin(phi) * Mathf.Cos(th), Mathf.Cos(phi), Mathf.Sin(phi) * Mathf.Sin(th));
                    v.Add(d * 0.5f);
                    n.Add(d);
                }
            }
            int stride = lon + 1;
            for (int i = 0; i < lat; i++)
                for (int j = 0; j < lon; j++)
                {
                    int a = i * stride + j, b = a + 1, c = a + stride, d = c + 1;
                    t.Add(a); t.Add(b); t.Add(c);
                    t.Add(b); t.Add(d); t.Add(c);
                }
            m = new Mesh { name = "Sphere" + lat + "x" + lon };
            m.SetVertices(v);
            m.SetNormals(n);
            m.SetTriangles(t, 0);
            m.RecalculateBounds();
            spheres[key] = m;
            return m;
        }

        public static Mesh SmoothSphere()
        {
            if (smoothSphere != null) return smoothSphere;
            const int lat = 36, lon = 56;
            var v = new List<Vector3>();
            var n = new List<Vector3>();
            var t = new List<int>();
            for (int i = 0; i <= lat; i++)
            {
                float phi = Mathf.PI * i / lat;
                for (int j = 0; j <= lon; j++)
                {
                    float th = Mathf.PI * 2f * j / lon;
                    var d = new Vector3(Mathf.Sin(phi) * Mathf.Cos(th), Mathf.Cos(phi), Mathf.Sin(phi) * Mathf.Sin(th));
                    v.Add(d * 0.5f);
                    n.Add(d);
                }
            }
            int stride = lon + 1;
            for (int i = 0; i < lat; i++)
                for (int j = 0; j < lon; j++)
                {
                    int a = i * stride + j, b = a + 1, c = a + stride, d = c + 1;
                    t.Add(a); t.Add(b); t.Add(c);
                    t.Add(b); t.Add(d); t.Add(c);
                }
            smoothSphere = new Mesh { name = "SmoothSphere" };
            smoothSphere.SetVertices(v);
            smoothSphere.SetNormals(n);
            smoothSphere.SetTriangles(t, 0);
            smoothSphere.RecalculateBounds();
            return smoothSphere;
        }

        /// <summary>Game object rendering a given mesh with a material (no collider).</summary>
        public static GameObject MeshObject(Mesh mesh, Transform parent, Vector3 localPos, Vector3 localScale, Material mat, bool shadows = true)
        {
            var go = new GameObject(mesh.name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = localScale;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = shadows ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off;
            return go;
        }

        static Mesh smoothCapsule, smoothCylinder;

        /// <summary>High-resolution capsule with the same size as Unity's primitive (height 2, radius 0.5).</summary>
        public static Mesh SmoothCapsule()
        {
            if (smoothCapsule != null) return smoothCapsule;
            var prof = new List<Vector2>();
            for (int i = 0; i <= 8; i++)
            {
                float a = -Mathf.PI * 0.5f + i * Mathf.PI * 0.5f / 8f;
                prof.Add(new Vector2(Mathf.Cos(a) * 0.5f, -0.5f + Mathf.Sin(a) * 0.5f));
            }
            for (int i = 0; i <= 8; i++)
            {
                float a = i * Mathf.PI * 0.5f / 8f;
                prof.Add(new Vector2(Mathf.Cos(a) * 0.5f, 0.5f + Mathf.Sin(a) * 0.5f));
            }
            prof[0] = new Vector2(0.0001f, -1f);
            prof[prof.Count - 1] = new Vector2(0.0001f, 1f);
            smoothCapsule = Lathe("smoothCapsule", prof.ToArray(), 32);
            return smoothCapsule;
        }

        /// <summary>Cylinder with softly rounded edges, sized like Unity's primitive (height 2, radius 0.5).</summary>
        public static Mesh SmoothCylinder()
        {
            if (smoothCylinder != null) return smoothCylinder;
            smoothCylinder = Lathe("smoothCylinder", new[]
            {
                new Vector2(0f, -1f), new Vector2(0.44f, -1f), new Vector2(0.49f, -0.98f), new Vector2(0.5f, -0.93f),
                new Vector2(0.5f, 0.93f), new Vector2(0.49f, 0.98f), new Vector2(0.44f, 1f), new Vector2(0f, 1f)
            }, 32);
            return smoothCylinder;
        }

        /// <summary>Unit quad from z=0 to z=1, x in [-0.5, 0.5]. Scale z for length.</summary>
        public static Mesh Line()
        {
            if (quad != null) return quad;
            quad = new Mesh { name = "Line" };
            quad.vertices = new[] { new Vector3(-0.5f, 0, 0), new Vector3(-0.5f, 0, 1), new Vector3(0.5f, 0, 1), new Vector3(0.5f, 0, 0) };
            quad.uv = new[] { new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 1), new Vector2(1, 0) };
            quad.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            quad.RecalculateNormals();
            quad.RecalculateBounds();
            return quad;
        }

        /// <summary>Creates a primitive with its collider removed (the game uses no physics).</summary>
        public static GameObject Primitive(PrimitiveType type, Transform parent, Vector3 localPos, Vector3 localScale, Material mat)
        {
            var go = GameObject.CreatePrimitive(type);
            var col = go.GetComponent<Collider>();
            if (col != null) Object.Destroy(col);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = localScale;
            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            return go;
        }
    }
}
