using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// A distant painted horizon: the region's Higgsfield key art wrapped around the scene on a huge inward-facing
    /// cylinder (mirrored so it never shows a seam). Makes the far background match the characters' art style.
    /// </summary>
    public static class PaintedBackdrop
    {
        public static GameObject Create(Transform parent, Texture2D art, Vector3 center, float radius, Color tint)
        {
            if (art == null) return null;
            const int seg = 64;
            const float bottom = -12f, top = 75f;
            var verts = new Vector3[(seg + 1) * 2];
            var uvs = new Vector2[verts.Length];
            var tris = new int[seg * 6];
            for (int i = 0; i <= seg; i++)
            {
                float t = (float)i / seg;
                float a = t * Mathf.PI * 2f;
                var dir = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));
                float u = Mathf.PingPong(t * 2f, 1f) * 0.9f + 0.05f;
                verts[i * 2] = dir * radius + Vector3.up * bottom;
                verts[i * 2 + 1] = dir * radius + Vector3.up * top;
                uvs[i * 2] = new Vector2(u, 0.28f);
                uvs[i * 2 + 1] = new Vector2(u, 1f);
                if (i < seg)
                {
                    int b = i * 2, k = i * 6;
                    // Wound to face inward.
                    tris[k] = b; tris[k + 1] = b + 1; tris[k + 2] = b + 2;
                    tris[k + 3] = b + 1; tris[k + 4] = b + 3; tris[k + 5] = b + 2;
                }
            }
            var mesh = new Mesh { name = "Backdrop" };
            mesh.vertices = verts;
            mesh.uv = uvs;
            mesh.triangles = tris;
            mesh.RecalculateBounds();
            var go = new GameObject("PaintedBackdrop");
            go.transform.SetParent(parent, false);
            go.transform.position = center;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            var mat = MaterialFactory.Transparent(new Color(tint.r, tint.g, tint.b, 1f));
            mat.mainTexture = art;
            mat.renderQueue = 2900; // behind every other transparent effect
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            return go;
        }

        /// <summary>Tint so the painting matches the scene's time of day (night scenes darken and colour it).</summary>
        public static Color TintFor(ArenaTheme t)
        {
            if (!t.night) return Color.white;
            Color sky = t.sky * (1f / Mathf.Max(0.05f, t.sky.maxColorComponent));
            return Color.Lerp(Color.white, sky, 0.55f) * (t.burning ? 0.75f : 0.5f);
        }
    }
}
