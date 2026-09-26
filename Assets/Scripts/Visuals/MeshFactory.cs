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
