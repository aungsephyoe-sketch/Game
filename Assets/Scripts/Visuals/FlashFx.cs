using System.Collections.Generic;
using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>Pooled one-shot mesh effect (slash arcs, shockwaves, pillars): scales and fades, then returns to the pool.</summary>
    public class FlashFx : MonoBehaviour
    {
        static readonly Stack<FlashFx> pool = new Stack<FlashFx>();
        static readonly Stack<FlashFx> darkPool = new Stack<FlashFx>();
        bool dark;
        static Transform poolRoot;

        Material mat;
        MeshFilter filter;
        Color color;
        Vector3 from, to;
        float duration, t;

        public static FlashFx Get(Mesh mesh) { return Get(mesh, false); }

        /// <summary>alphaBlend: a normally-blended (not glowing) flash, used for dark backings that give slashes contrast on bright ground.</summary>
        public static FlashFx Get(Mesh mesh, bool alphaBlend)
        {
            FlashFx fx = null;
            var src = alphaBlend ? darkPool : pool;
            while (fx == null && src.Count > 0) fx = src.Pop();
            if (fx == null)
            {
                if (poolRoot == null)
                {
                    var root = new GameObject("[FlashPool]");
                    DontDestroyOnLoad(root);
                    poolRoot = root.transform;
                }
                var go = new GameObject("Flash");
                go.transform.SetParent(poolRoot, false);
                fx = go.AddComponent<FlashFx>();
                fx.filter = go.AddComponent<MeshFilter>();
                var mr = go.AddComponent<MeshRenderer>();
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mr.receiveShadows = false;
                fx.dark = alphaBlend;
                fx.mat = alphaBlend ? MaterialFactory.Transparent(Color.white) : MaterialFactory.Additive(Color.white);
                mr.sharedMaterial = fx.mat;
            }
            fx.filter.sharedMesh = mesh;
            fx.gameObject.SetActive(true);
            return fx;
        }

        public void Play(Vector3 pos, Quaternion rot, Vector3 fromScale, Vector3 toScale, Color c, float dur)
        {
            transform.SetPositionAndRotation(pos, rot);
            transform.localScale = fromScale;
            color = c;
            from = fromScale;
            to = toScale;
            duration = Mathf.Max(0.01f, dur);
            t = 0f;
            mat.color = c;
        }

        void Update()
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            float ease = 1f - (1f - k) * (1f - k);
            transform.localScale = Vector3.LerpUnclamped(from, to, ease);
            var c = color;
            c.a = color.a * (1f - k * k);
            mat.color = c;
            if (k >= 1f)
            {
                gameObject.SetActive(false);
                (dark ? darkPool : pool).Push(this);
            }
        }

        void OnDestroy()
        {
            if (mat != null) Destroy(mat);
        }
    }
}
