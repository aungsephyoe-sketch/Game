using System.Collections.Generic;
using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// Temple puzzle: ancient seal stones must be struck in the order their runes glow. A wrong strike resets the
    /// sequence and wakes the temple's shadows. Solving it opens the way (and the waves begin).
    /// </summary>
    public class SealStone : MonoBehaviour
    {
        public static readonly List<SealStone> All = new List<SealStone>();
        /// <summary>Raised with the stone that was struck.</summary>
        public static event System.Action<SealStone> Struck;

        public int Order;
        public bool Lit { get; private set; }
        public Vector3 Position { get { return transform.position; } }
        Material runeMat;
        float lastHit;

        public static SealStone Create(Transform parent, Vector3 pos, int order)
        {
            var go = new GameObject("Seal " + order);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            var s = go.AddComponent<SealStone>();
            s.Order = order;
            var stone = MaterialFactory.Toon(new Color(0.35f, 0.37f, 0.4f));
            MeshFactory.Primitive(PrimitiveType.Cube, go.transform, new Vector3(0f, 1f, 0f), new Vector3(1f, 2f, 0.6f), stone);
            MeshFactory.Primitive(PrimitiveType.Cube, go.transform, new Vector3(0f, 2.1f, 0f), new Vector3(1.3f, 0.25f, 0.8f), stone);
            s.runeMat = MaterialFactory.Additive(new Color(0.3f, 0.5f, 0.6f, 0.4f));
            // The rune: one to four marks show the stone's place in the sequence.
            for (int i = 0; i < order; i++)
            {
                var mark = GameObject.CreatePrimitive(PrimitiveType.Quad);
                var col = mark.GetComponent<Collider>();
                if (col != null) Destroy(col);
                mark.transform.SetParent(go.transform, false);
                mark.transform.localPosition = new Vector3((i - (order - 1) * 0.5f) * 0.22f, 1.2f, -0.31f);
                mark.transform.localScale = new Vector3(0.12f, 0.7f, 1f);
                mark.GetComponent<Renderer>().sharedMaterial = s.runeMat;
            }
            return s;
        }

        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); }

        public void SetLit(bool lit, Color color)
        {
            Lit = lit;
            runeMat.color = lit ? new Color(color.r, color.g, color.b, 0.95f) : new Color(0.3f, 0.5f, 0.6f, 0.4f);
        }

        public void Pulse(Color color)
        {
            VFX.Pillar(Position, color, 5f, 0.4f);
            VFX.Breath(Position + Vector3.up, color, 12);
        }

        /// <summary>Called by CombatSystem for player attacks.</summary>
        public static void HitInArc(Vector3 origin, Vector3 forward, float range, float arcDegrees)
        {
            if (All.Count == 0) return;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f) forward = Vector3.forward;
            forward.Normalize();
            float halfCos = Mathf.Cos(arcDegrees * 0.5f * Mathf.Deg2Rad);
            for (int i = All.Count - 1; i >= 0; i--)
            {
                var s = All[i];
                if (s == null || Time.time - s.lastHit < 0.5f) continue;
                Vector3 to = s.Position - origin;
                to.y = 0f;
                float d = to.magnitude;
                if (d > range + 0.7f) continue;
                if (arcDegrees < 359f && d > 1f && Vector3.Dot(forward, to / d) < halfCos) continue;
                s.lastHit = Time.time;
                VFX.HitSpark(s.Position + Vector3.up * 1.2f, new Color(0.6f, 0.9f, 1f), 10);
                if (GameManager.Instance != null) GameManager.Instance.Audio.Play("thud", 0.6f);
                var h = Struck;
                if (h != null) h(s);
            }
        }
    }
}
