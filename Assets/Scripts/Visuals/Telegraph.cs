using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// Ground warning for enemy attacks: a faint outline of the danger zone plus a fill that grows
    /// until the attack lands. Readable telegraphs are what make dodging feel fair.
    /// </summary>
    public class Telegraph : MonoBehaviour
    {
        Transform fill;
        Material outlineMat, fillMat;
        Vector3 fullScale;
        float duration, t;
        bool lengthOnly;

        static readonly Color DangerColor = new Color(1f, 0.12f, 0.1f, 1f);

        /// <summary>Every warning on the ground right now (the co-op party's AI reads them to dodge).</summary>
        public static readonly System.Collections.Generic.List<Telegraph> Active = new System.Collections.Generic.List<Telegraph>();
        /// <summary>A rough circle covering the danger zone.</summary>
        public Vector3 DangerCenter { get; private set; }
        public float DangerRadius { get; private set; }

        void OnEnable() { Active.Add(this); }
        void OnDisable() { Active.Remove(this); }

        public bool Threatens(Vector3 p, float pad)
        {
            Vector3 d = p - DangerCenter;
            d.y = 0f;
            return d.magnitude < DangerRadius + pad;
        }

        public static Telegraph Circle(Vector3 center, float radius, float duration)
        {
            var t = Create(MeshFactory.Disc(), center, Quaternion.identity, new Vector3(radius, 1f, radius), duration, false);
            t.DangerCenter = center; t.DangerRadius = radius;
            return t;
        }

        public static Telegraph Sector(Vector3 origin, Vector3 forward, float range, float arcDegrees, float duration)
        {
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.001f) forward = Vector3.forward;
            var t = Create(MeshFactory.Sector(arcDegrees, 0f), origin, Quaternion.LookRotation(forward), new Vector3(range, 1f, range), duration, false);
            t.DangerCenter = origin + forward.normalized * range * 0.5f; t.DangerRadius = range * 0.6f;
            return t;
        }

        public static Telegraph Line(Vector3 origin, Vector3 forward, float width, float length, float duration)
        {
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.001f) forward = Vector3.forward;
            var t = Create(MeshFactory.Line(), origin, Quaternion.LookRotation(forward), new Vector3(width, 1f, length), duration, true);
            t.DangerCenter = origin + forward.normalized * length * 0.5f; t.DangerRadius = Mathf.Max(width, length * 0.5f);
            return t;
        }

        public static Telegraph Ring(Vector3 center, float innerRadius, float outerRadius, float duration)
        {
            var t = Create(MeshFactory.Ring(innerRadius / outerRadius), center, Quaternion.identity,
                new Vector3(outerRadius, 1f, outerRadius), duration, false, true);
            t.DangerCenter = center; t.DangerRadius = outerRadius;
            return t;
        }

        static Telegraph Create(Mesh mesh, Vector3 pos, Quaternion rot, Vector3 scale, float duration, bool lengthOnly, bool pulseOnly = false)
        {
            var go = new GameObject("Telegraph");
            go.transform.SetPositionAndRotation(new Vector3(pos.x, Ground.HeightAt(pos.x, pos.z) + 0.03f, pos.z), rot);
            var tg = go.AddComponent<Telegraph>();
            tg.duration = Mathf.Max(0.05f, duration);
            tg.fullScale = scale;
            tg.lengthOnly = lengthOnly;

            var outline = new GameObject("Area");
            outline.transform.SetParent(go.transform, false);
            outline.transform.localScale = scale;
            outline.AddComponent<MeshFilter>().sharedMesh = mesh;
            var omr = outline.AddComponent<MeshRenderer>();
            omr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            omr.receiveShadows = false;
            tg.outlineMat = MaterialFactory.Transparent(new Color(DangerColor.r, DangerColor.g, DangerColor.b, 0.22f));
            omr.material = tg.outlineMat;

            var fillGo = new GameObject("Fill");
            fillGo.transform.SetParent(go.transform, false);
            fillGo.transform.localPosition = Vector3.up * 0.01f;
            fillGo.AddComponent<MeshFilter>().sharedMesh = mesh;
            var fmr = fillGo.AddComponent<MeshRenderer>();
            fmr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            fmr.receiveShadows = false;
            tg.fillMat = MaterialFactory.Transparent(new Color(DangerColor.r, DangerColor.g, DangerColor.b, 0.35f));
            fmr.material = tg.fillMat;
            tg.fill = fillGo.transform;
            if (pulseOnly) tg.fill.localScale = scale;
            else tg.fill.localScale = lengthOnly ? new Vector3(scale.x, 1f, 0f) : new Vector3(0f, 1f, 0f);
            tg.pulse = pulseOnly;
            return tg;
        }

        bool pulse;

        void Update()
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            if (pulse)
            {
                var c = fillMat.color;
                c.a = 0.15f + 0.35f * k + 0.1f * Mathf.Sin(t * 30f);
                fillMat.color = c;
            }
            else if (lengthOnly) fill.localScale = new Vector3(fullScale.x, 1f, fullScale.z * k);
            else fill.localScale = new Vector3(fullScale.x * k, 1f, fullScale.z * k);

            if (k >= 1f && !pulse)
            {
                var c = fillMat.color;
                c.a = 0.35f + 0.25f * Mathf.Sin(t * 40f);
                fillMat.color = c;
            }
            // Safety net: telegraphs never outlive their attack by long.
            if (t > duration + 2f) Destroy(gameObject);
        }

        public void Finish()
        {
            if (this != null && gameObject != null) Destroy(gameObject);
        }

        void OnDestroy()
        {
            if (outlineMat != null) Destroy(outlineMat);
            if (fillMat != null) Destroy(fillMat);
        }
    }
}
