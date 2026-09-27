using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// A summoned elemental dragon for special attacks: a glowing serpent (horned head, jaw, eyes, a long body
    /// of shrinking segments and a tail fin) that flies along a path, coils, and breathes its element.
    /// The body follows the head's trail, so it moves like a real serpent.
    /// </summary>
    public class SpiritDragon : MonoBehaviour
    {
        const int Segments = 18;
        const float Spacing = 0.55f;
        Transform headT;
        readonly List<Transform> body = new List<Transform>();
        readonly List<Vector3> trail = new List<Vector3>();
        Element element;
        Color color;
        float scale = 1f;

        public Vector3 HeadPosition { get { return headT.position; } }
        public Vector3 HeadForward { get { return headT.forward; } }

        public static SpiritDragon Summon(Vector3 at, Element e, float size = 1f)
        {
            var go = new GameObject("SpiritDragon");
            var d = go.AddComponent<SpiritDragon>();
            d.element = e;
            d.color = ElementChart.ColorOf(e);
            d.scale = size;
            d.Build(at);
            return d;
        }

        void Build(Vector3 at)
        {
            Color c = color;
            var skin = MaterialFactory.Toon(Color.Lerp(c, Color.white, 0.2f), 0.02f, c * 0.55f);
            var belly = MaterialFactory.Toon(Color.Lerp(c, Color.white, 0.65f), 0.02f, c * 0.3f);
            var glow = MaterialFactory.Additive(new Color(c.r, c.g, c.b, 0.35f));
            var horn = MaterialFactory.Toon(new Color(1f, 0.95f, 0.8f), 0.02f);
            var eye = MaterialFactory.Toon(Color.white, 0f, new Color(1f, 1f, 0.8f));
            var sphere = MeshFactory.SmoothSphere();

            headT = new GameObject("Head").transform;
            headT.SetParent(transform, false);
            headT.position = at;
            float s = scale;
            MeshFactory.MeshObject(sphere, headT, Vector3.zero, new Vector3(0.9f, 0.75f, 1.3f) * s, skin);
            MeshFactory.MeshObject(sphere, headT, new Vector3(0f, -0.2f, 0.55f) * s, new Vector3(0.7f, 0.35f, 0.9f) * s, belly); // snout / jaw
            MeshFactory.MeshObject(sphere, headT, Vector3.zero, new Vector3(1.5f, 1.3f, 1.9f) * s, glow, false);
            for (int k = -1; k <= 1; k += 2)
            {
                var h = MeshFactory.MeshObject(MeshFactory.Cone(), headT, new Vector3(0.28f * k, 0.3f, -0.35f) * s, new Vector3(0.14f, 0.7f, 0.14f) * s, horn);
                h.transform.localRotation = Quaternion.Euler(-60f, 0f, -15f * k);
                MeshFactory.MeshObject(sphere, headT, new Vector3(0.3f * k, 0.15f, 0.35f) * s, new Vector3(0.16f, 0.12f, 0.12f) * s, eye);
                // Whiskers.
                var w = MeshFactory.Primitive(PrimitiveType.Cube, headT, new Vector3(0.35f * k, -0.15f, 0.7f) * s, new Vector3(0.03f, 0.03f, 0.9f) * s, glow);
                w.transform.localRotation = Quaternion.Euler(10f, 35f * k, 0f);
            }
            for (int i = 0; i < Segments; i++)
            {
                var seg = new GameObject("Seg" + i).transform;
                seg.SetParent(transform, false);
                seg.position = at;
                float t = (float)i / Segments;
                float r = Mathf.Lerp(0.75f, 0.2f, t) * s;
                MeshFactory.MeshObject(sphere, seg, Vector3.zero, new Vector3(r, r * 0.9f, r * 1.5f), skin);
                MeshFactory.MeshObject(sphere, seg, new Vector3(0f, -r * 0.2f, 0f), new Vector3(r * 0.8f, r * 0.6f, r * 1.3f), belly);
                if (i % 2 == 0) MeshFactory.MeshObject(MeshFactory.Cone(), seg, new Vector3(0f, r * 0.45f, 0f), new Vector3(0.12f, 0.3f, 0.12f) * s * (1f - t * 0.6f), horn); // spines
                MeshFactory.MeshObject(sphere, seg, Vector3.zero, new Vector3(r * 1.6f, r * 1.5f, r * 2f), glow, false);
                body.Add(seg);
            }
            // Tail fin.
            var fin = MeshFactory.MeshObject(MeshFactory.Sector(90f, 0f), body[Segments - 1], new Vector3(0f, 0f, -0.2f), Vector3.one * 0.7f * s, glow, false);
            fin.transform.localRotation = Quaternion.Euler(0f, 180f, 90f);
            for (int i = 0; i < Segments * 8; i++) trail.Add(at);
        }

        void Update()
        {
            // Record the head's path; each segment sits a fixed distance back along it.
            if (trail.Count == 0 || (trail[0] - headT.position).sqrMagnitude > 0.004f)
            {
                trail.Insert(0, headT.position);
                if (trail.Count > Segments * 12) trail.RemoveAt(trail.Count - 1);
            }
            float want = Spacing * scale;
            int idx = 0;
            Vector3 prev = headT.position;
            for (int i = 0; i < body.Count; i++)
            {
                float acc = 0f;
                Vector3 p = prev;
                while (idx < trail.Count - 1 && acc < want)
                {
                    acc += Vector3.Distance(trail[idx], trail[idx + 1]);
                    idx++;
                    p = trail[idx];
                }
                // A gentle ripple down the body.
                p += Vector3.up * Mathf.Sin(Time.time * 6f - i * 0.6f) * 0.08f * scale;
                Vector3 look = prev - p;
                body[i].position = p;
                if (look.sqrMagnitude > 0.0001f) body[i].rotation = Quaternion.LookRotation(look);
                prev = p;
            }
        }

        /// <summary>Flies the head smoothly through the points over the given time.</summary>
        public IEnumerator Fly(Vector3[] pts, float duration)
        {
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / duration) * (pts.Length - 1);
                int i = Mathf.Min(Mathf.FloorToInt(u), pts.Length - 2);
                float f = u - i;
                Vector3 p0 = pts[Mathf.Max(0, i - 1)], p1 = pts[i], p2 = pts[i + 1], p3 = pts[Mathf.Min(pts.Length - 1, i + 2)];
                Vector3 pos = 0.5f * ((2f * p1) + (-p0 + p2) * f + (2f * p0 - 5f * p1 + 4f * p2 - p3) * f * f + (-p0 + 3f * p1 - 3f * p2 + p3) * f * f * f);
                Vector3 dir = pos - headT.position;
                if (dir.sqrMagnitude > 0.0001f) headT.rotation = Quaternion.Slerp(headT.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 12f);
                headT.position = pos;
                if (Random.value < 0.3f) VFX.Breath(pos, color, 2);
                yield return null;
            }
        }

        /// <summary>Hovers, turns toward the target and breathes its element for the given time.</summary>
        public IEnumerator Breathe(Vector3 target, float duration, System.Action<Vector3> tick)
        {
            float t = 0f, next = 0f;
            Vector3 hover = headT.position;
            while (t < duration)
            {
                t += Time.deltaTime;
                headT.position = hover + Vector3.up * Mathf.Sin(t * 5f) * 0.2f;
                Vector3 dir = (target - headT.position).normalized;
                headT.rotation = Quaternion.Slerp(headT.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 8f);
                ElementFx.Stream(headT.position + headT.forward * 0.9f * scale, headT.forward, element, 6);
                if (t >= next)
                {
                    next = t + 0.18f;
                    if (tick != null) tick(target);
                }
                yield return null;
            }
        }

        /// <summary>Soars up and fades away.</summary>
        public IEnumerator Leave()
        {
            Vector3 a = headT.position;
            yield return Fly(new[] { a, a + headT.forward * 4f + Vector3.up * 3f, a + headT.forward * 6f + Vector3.up * 12f }, 0.7f);
            float t = 0f;
            while (t < 0.3f)
            {
                t += Time.deltaTime;
                transform.localScale = Vector3.one * (1f - t / 0.3f);
                yield return null;
            }
            Destroy(gameObject);
        }
    }
}
