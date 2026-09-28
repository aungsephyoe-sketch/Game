using System.Collections.Generic;
using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// Combines many small pieces of scenery (grass tufts, flowers, tree canopies, rocks) into a few large
    /// vertex-coloured meshes that share one material, so a dense, detailed world costs only a handful of draw
    /// calls on mobile. Vertex alpha carries the wind weight (0 = rigid, 1 = sways fully).
    /// </summary>
    public class WorldMeshBuilder
    {
        readonly List<Vector3> v = new List<Vector3>();
        readonly List<Vector3> n = new List<Vector3>();
        readonly List<Color> c = new List<Color>();
        readonly List<int> t = new List<int>();
        readonly Transform parent;
        readonly Material mat;
        readonly string name;
        readonly bool shadows;
        public readonly List<GameObject> Built = new List<GameObject>();

        static readonly Dictionary<Mesh, Vector3[]> vcache = new Dictionary<Mesh, Vector3[]>();
        static readonly Dictionary<Mesh, Vector3[]> ncache = new Dictionary<Mesh, Vector3[]>();
        static readonly Dictionary<Mesh, int[]> tcache = new Dictionary<Mesh, int[]>();

        public WorldMeshBuilder(Transform parent, Material mat, string name, bool shadows)
        {
            this.parent = parent; this.mat = mat; this.name = name; this.shadows = shadows;
        }

        /// <summary>
        /// Adds a copy of <paramref name="mesh"/>. Wind sway grows with the vertex's local height
        /// (windBase at the bottom → wind at local y = windTop), so trunks stay put while leaves move.
        /// </summary>
        public void Add(Mesh mesh, Vector3 pos, Quaternion rot, Vector3 scale, Color col, float wind = 0f, float windTop = 1f, float windBase = 0f)
        {
            Vector3[] mv, mn; int[] mt;
            if (!vcache.TryGetValue(mesh, out mv)) { mv = mesh.vertices; vcache[mesh] = mv; }
            if (!ncache.TryGetValue(mesh, out mn)) { mn = mesh.normals; ncache[mesh] = mn; }
            if (!tcache.TryGetValue(mesh, out mt)) { mt = mesh.triangles; tcache[mesh] = mt; }
            if (v.Count + mv.Length > 60000) Flush();
            int b = v.Count;
            Vector3 inv = new Vector3(1f / Mathf.Max(0.0001f, scale.x), 1f / Mathf.Max(0.0001f, scale.y), 1f / Mathf.Max(0.0001f, scale.z));
            for (int i = 0; i < mv.Length; i++)
            {
                Vector3 lp = mv[i];
                v.Add(pos + rot * Vector3.Scale(lp, scale));
                Vector3 nn = i < mn.Length ? mn[i] : Vector3.up;
                n.Add((rot * Vector3.Scale(nn, inv)).normalized);
                float w = wind <= 0f ? 0f : Mathf.Lerp(windBase, 1f, Mathf.Clamp01(lp.y / Mathf.Max(0.001f, windTop))) * wind;
                c.Add(new Color(col.r, col.g, col.b, w));
            }
            for (int i = 0; i < mt.Length; i++) t.Add(b + mt[i]);
        }

        /// <summary>Adds raw geometry (terrain, ribbons) with per-vertex colours.</summary>
        public void AddRaw(List<Vector3> verts, List<Color> cols, List<int> tris)
        {
            int b = v.Count;
            v.AddRange(verts);
            c.AddRange(cols);
            for (int i = 0; i < verts.Count; i++) n.Add(Vector3.up);
            for (int i = 0; i < tris.Count; i++) t.Add(b + tris[i]);
        }

        public void Flush(bool recalcNormals = false)
        {
            if (v.Count == 0) return;
            var mesh = new Mesh { name = name };
            if (v.Count > 65000) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(v);
            mesh.SetColors(c);
            mesh.SetTriangles(t, 0);
            if (recalcNormals) mesh.RecalculateNormals(); else mesh.SetNormals(n);
            mesh.RecalculateBounds();
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.receiveShadows = true;
            mr.shadowCastingMode = shadows ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off;
            Built.Add(go);
            v.Clear(); n.Clear(); c.Clear(); t.Clear();
        }
    }

    /// <summary>Scrolls a material's texture (flowing water, creeping lava crust, drifting fog).</summary>
    public class UvScroll : MonoBehaviour
    {
        public Vector2 Speed = new Vector2(0f, 0.3f);
        Material mat;

        void Start()
        {
            var r = GetComponent<Renderer>();
            if (r != null) mat = r.material;
        }

        void Update()
        {
            if (mat != null) mat.mainTextureOffset += Speed * Time.deltaTime;
        }
    }

    /// <summary>Puts global lighting settings back when a hand-lit world is torn down.</summary>
    public class LightingRestorer : MonoBehaviour
    {
        public float farClip = 200f;

        void OnDestroy()
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            var cam = Camera.main;
            if (cam != null) cam.farClipPlane = farClip;
            var sun = RenderSettings.sun;
            if (sun != null) sun.shadows = LightShadows.Soft;
        }
    }

    public static class WorldKit
    {
        static readonly Dictionary<PrimitiveType, Mesh> prims = new Dictionary<PrimitiveType, Mesh>();

        /// <summary>The mesh of a built-in primitive (for combining).</summary>
        public static Mesh Prim(PrimitiveType type)
        {
            Mesh m;
            if (prims.TryGetValue(type, out m) && m != null) return m;
            var go = GameObject.CreatePrimitive(type);
            m = go.GetComponent<MeshFilter>().sharedMesh;
            Object.Destroy(go);
            prims[type] = m;
            return m;
        }

        static Texture2D streaks, crust;

        /// <summary>Soft white streaks on transparent (flowing water foam, blowing snow).</summary>
        public static Texture2D Streaks
        {
            get
            {
                if (streaks != null) return streaks;
                const int w = 64, h = 128;
                streaks = new Texture2D(w, h, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Repeat, name = "Streaks" };
                var px = new Color[w * h];
                var rnd = new System.Random(11);
                for (int i = 0; i < px.Length; i++) px[i] = new Color(1f, 1f, 1f, 0f);
                for (int k = 0; k < 26; k++)
                {
                    int x0 = rnd.Next(w), y0 = rnd.Next(h), len = 10 + rnd.Next(30);
                    float a = 0.35f + (float)rnd.NextDouble() * 0.5f;
                    for (int y = 0; y < len; y++)
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            int x = (x0 + dx + w) % w, yy = (y0 + y) % h;
                            float fall = Mathf.Sin(y / (float)len * Mathf.PI) * (dx == 0 ? 1f : 0.4f);
                            px[yy * w + x].a = Mathf.Max(px[yy * w + x].a, a * fall);
                        }
                }
                streaks.SetPixels(px);
                streaks.Apply();
                return streaks;
            }
        }

        /// <summary>Dark cooled-crust blotches on transparent (lava).</summary>
        public static Texture2D Crust
        {
            get
            {
                if (crust != null) return crust;
                const int s = 128;
                crust = new Texture2D(s, s, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Repeat, name = "Crust" };
                var px = new Color[s * s];
                for (int y = 0; y < s; y++)
                    for (int x = 0; x < s; x++)
                    {
                        float n = Mathf.PerlinNoise(x * 0.07f, y * 0.07f) * 0.7f + Mathf.PerlinNoise(x * 0.2f + 30f, y * 0.2f) * 0.3f;
                        float a = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.48f, 0.62f, n));
                        px[y * s + x] = new Color(0.12f, 0.05f, 0.04f, a * 0.9f);
                    }
                crust.SetPixels(px);
                crust.Apply();
                return crust;
            }
        }

        public static float Noise(float x, float z) { return Mathf.PerlinNoise(x + 100f, z + 100f); }

        /// <summary>Ridged noise (sharp crests) for rocky and volcanic ranges.</summary>
        public static float Ridged(float x, float z)
        {
            float n = 1f - Mathf.Abs(Mathf.PerlinNoise(x + 50f, z + 50f) * 2f - 1f);
            return n * n;
        }
    }
}
