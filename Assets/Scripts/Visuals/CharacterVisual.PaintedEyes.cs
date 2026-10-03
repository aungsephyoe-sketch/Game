using System.Collections.Generic;
using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// Painted eyes: each eye is one small patch curved to the face, drawn per pixel by the PaintedEye shader
    /// (iris gradient, pupil, highlights, lash line and flick, lower lash). It replaces the stack of little discs
    /// and capsules the eyes used to be made of, so faces look hand-painted and stay sharp on posters. Blinks and
    /// expressions still work through the eye's transform; the shader turns the squash into a closing lid.
    /// </summary>
    public partial class CharacterVisual
    {
        /// <summary>Patch size relative to the eye (room for the lashes and the flick).</summary>
        const float EyePatchX = 1.5f, EyePatchY = 1.6f;

        static Shader eyeShader;
        static bool eyeShaderTried;
        static readonly Dictionary<int, Mesh> eyeMeshes = new Dictionary<int, Mesh>();

        static Shader EyeShader()
        {
            if (!eyeShaderTried)
            {
                eyeShaderTried = true;
                var s = Resources.Load<Shader>("Shaders/PaintedEye");
                eyeShader = s != null && s.isSupported ? s : null;
            }
            return eyeShader;
        }

        /// <summary>Builds a painted eye on the eye pivot. False when painted eyes are off or unsupported.</summary>
        bool PaintedEye(Transform e, int side, float w, float h, float headR, Color iris, Color irisLow, bool flick)
        {
            if (!GameConfig.PaintedEyes || EyeShader() == null) return false;
            var m = new Material(EyeShader()) { name = "PaintedEye" };
            m.SetColor("_Iris", Vivid(iris, 1.1f, 0.3f));
            m.SetColor("_IrisLow", irisLow);
            m.SetColor("_Ink", new Color(0.06f, 0.04f, 0.08f));
            m.SetFloat("_Aspect", h / Mathf.Max(0.01f, w));
            m.SetFloat("_Side", side);
            m.SetFloat("_Wing", flick ? 1f : 0.55f);
            m.SetFloat("_Tilt", 0.04f);
            m.SetFloat("_MX", EyePatchX);
            m.SetFloat("_MY", EyePatchY);
            // The first child carries the eye's size (LivelyFace and the species code read it).
            var go = MeshFactory.MeshObject(EyePatch(w, h, headR), e, Vector3.zero, new Vector3(w, h, 1f), m, false);
            go.name = "PaintedEye";
            go.transform.SetAsFirstSibling();
            Add(go);
            return true;
        }

        /// <summary>A grid patch in eye units (scaled by w, h on its transform), bent to the head's curve.</summary>
        static Mesh EyePatch(float w, float h, float headR)
        {
            int key = Mathf.RoundToInt(w * 1000f) * 1000003 + Mathf.RoundToInt(h * 1000f) * 1009 + Mathf.RoundToInt(headR * 100f);
            Mesh mesh;
            if (eyeMeshes.TryGetValue(key, out mesh) && mesh != null) return mesh;
            const int nx = 9, ny = 9;
            var v = new Vector3[nx * ny];
            var n = new Vector3[nx * ny];
            var uv = new Vector2[nx * ny];
            float R = Mathf.Max(0.1f, headR);
            for (int j = 0; j < ny; j++)
                for (int i = 0; i < nx; i++)
                {
                    float u = i / (float)(nx - 1), t = j / (float)(ny - 1);
                    float X = (u - 0.5f) * EyePatchX, Y = (t - 0.5f) * EyePatchY;
                    float px = X * w, py = Y * h;
                    // Just above the skin, following its curve.
                    float z = 0.0045f - (px * px + py * py) / (2f * R);
                    int k = j * nx + i;
                    v[k] = new Vector3(X, Y, z);
                    n[k] = new Vector3(px / R * w, py / R * h, 1f).normalized;
                    uv[k] = new Vector2(u, t);
                }
            var tris = new int[(nx - 1) * (ny - 1) * 6];
            int q = 0;
            for (int j = 0; j < ny - 1; j++)
                for (int i = 0; i < nx - 1; i++)
                {
                    int a = j * nx + i, b = a + 1, c = a + nx, d = c + 1;
                    tris[q++] = a; tris[q++] = b; tris[q++] = c;
                    tris[q++] = b; tris[q++] = d; tris[q++] = c;
                }
            mesh = new Mesh { name = "EyePatch" };
            mesh.vertices = v;
            mesh.normals = n;
            mesh.uv = uv;
            mesh.triangles = tris;
            mesh.RecalculateBounds();
            eyeMeshes[key] = mesh;
            return mesh;
        }
    }
}
