using System.Collections.Generic;
using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// Sculpted stylised hair, built as geometry rather than from primitives:
    ///  • a scalp shell cut along a real hairline (forehead, temples, sideburns, nape), so the hair visibly grows from
    ///    the head and there are no gaps between the locks,
    ///  • locks — tapered, flattened clumps of strands that start embedded in the scalp, follow the curve of the head
    ///    and then fall with weight (or, for spiky styles, stand out and curve), each at its own layer depth,
    ///  • bangs that stop above the brows so the eyes are always clear, face-framing side pieces, a back section,
    ///  • highlight ribbons and a shade colour so the hair reads as layered, not as one plastic helmet.
    /// Everything is generated in head space (the head is an ellipsoid at <see cref="hc"/> with radii
    /// <see cref="hr"/>); locks are also kept out of the body so long hair falls over the shoulders, not through them.
    /// Parts are grouped (bangs, sides, back…) so each group can swing on its own (<see cref="HairSway"/>).
    /// </summary>
    public class HairGeometry
    {
        public const int Base = 0, Shade = 1, Shine = 2;

        public class Part
        {
            public string group;
            public int mat;
            public readonly List<Vector3> v = new List<Vector3>();
            public readonly List<int> t = new List<int>();
        }

        struct Solid { public Vector3 c, r; public float margin; }

        public readonly Vector3 hc, hr;
        public readonly List<Part> parts = new List<Part>();
        /// <summary>Where each group swings from (head space).</summary>
        public readonly Dictionary<string, Vector3> pivots = new Dictionary<string, Vector3>();
        readonly List<Solid> solids = new List<Solid>();
        readonly System.Random rng;

        public HairGeometry(Vector3 headCenter, Vector3 headRadii, int seed)
        {
            hc = headCenter;
            hr = headRadii;
            rng = new System.Random(seed);
        }

        /// <summary>Something the hair must fall around (shoulders, back, chest), in head space.</summary>
        public void AddBody(Vector3 center, Vector3 radii, float margin = 0.02f)
        {
            solids.Add(new Solid { c = center, r = radii, margin = margin });
        }

        /// <summary>How far along a lock it keeps its full width before tapering (long hair stays full, spikes taper early).</summary>
        public float taperStart = 0.12f;
        /// <summary>How much of its width a lock keeps at the very tip (0 = pointed, higher = soft, blunt ends).</summary>
        public float tipWidth = 0f;

        public float R(float a, float b) { return a + (float)rng.NextDouble() * (b - a); }

        Part Get(string group, int mat)
        {
            foreach (var p in parts) if (p.group == group && p.mat == mat) return p;
            var n = new Part { group = group, mat = mat };
            parts.Add(n);
            return n;
        }

        // ------------------------------------------------------------------ Head surface

        /// <summary>Unit direction for azimuth (0 = front, +90 = the character's right side, +x) and polar angle from the top.</summary>
        public static Vector3 Dir(float az, float polar)
        {
            float a = az * Mathf.Deg2Rad, t = polar * Mathf.Deg2Rad;
            return new Vector3(Mathf.Sin(t) * Mathf.Sin(a), Mathf.Cos(t), Mathf.Sin(t) * Mathf.Cos(a));
        }

        public Vector3 Surf(float az, float polar, float off)
        {
            return hc + Vector3.Scale(Dir(az, polar), hr) * (1f + off);
        }

        Vector3 Normal(Vector3 p)
        {
            Vector3 d = p - hc;
            Vector3 n = new Vector3(d.x / (hr.x * hr.x), d.y / (hr.y * hr.y), d.z / (hr.z * hr.z));
            return n.sqrMagnitude > 1e-8f ? n.normalized : Vector3.up;
        }

        static Vector3 PushOut(Vector3 p, Vector3 c, Vector3 r)
        {
            Vector3 q = new Vector3((p.x - c.x) / r.x, (p.y - c.y) / r.y, (p.z - c.z) / r.z);
            float m = q.magnitude;
            if (m >= 1f || m < 1e-5f) return p;
            q /= m;
            return c + Vector3.Scale(q, r);
        }

        /// <summary>Onto the head's surface at layer depth <paramref name="off"/> (and out of the body).</summary>
        Vector3 Snap(Vector3 p, float off)
        {
            Vector3 r = hr * (1f + off);
            Vector3 q = new Vector3((p.x - hc.x) / r.x, (p.y - hc.y) / r.y, (p.z - hc.z) / r.z);
            if (q.sqrMagnitude > 1e-10f) p = hc + Vector3.Scale(q.normalized, r);
            return Collide(p, off);
        }

        Vector3 Collide(Vector3 p, float off)
        {
            p = PushOut(p, hc, hr * (1f + off));
            for (int i = 0; i < solids.Count; i++)
            {
                var s = solids[i];
                p = PushOut(p, s.c, s.r + Vector3.one * (s.margin + off * 0.5f));
            }
            return p;
        }

        // ------------------------------------------------------------------ Scalp

        /// <summary>
        /// The scalp shell: a smooth layer over the skull, cut along a hairline that sits at <paramref name="front"/>
        /// degrees from the top over the forehead, <paramref name="side"/> at the temples and <paramref name="back"/>
        /// at the nape, with the temple corners set back by <paramref name="temple"/> degrees.
        /// </summary>
        public void Cap(string group, int mat, float off, float front, float side, float back, float temple, float partAz = 0f)
        {
            const int A = 44, V = 12;
            var p = Get(group, mat);
            int start = p.v.Count;
            for (int ia = 0; ia < A; ia++)
            {
                float az = ia * 360f / A;
                float c = Mathf.Cos(az * Mathf.Deg2Rad);
                float lim = c >= 0f ? Mathf.Lerp(side, front, c) : Mathf.Lerp(side, back, -c);
                float fa = Mathf.DeltaAngle(0f, az);
                float tDist = (Mathf.Abs(fa) - 48f) / 14f;
                lim -= temple * Mathf.Exp(-tDist * tDist);
                for (int iv = 0; iv <= V; iv++)
                {
                    float pol = lim * iv / V;
                    // A soft rise along the part line gives the top some shape.
                    float lift = 1f + off + 0.012f * Mathf.Exp(-Sq(Mathf.DeltaAngle(partAz, az) / 20f)) * Mathf.Sin(Mathf.PI * iv / V);
                    // The edge tucks in slightly so it meets the skin cleanly.
                    if (iv == V) lift = 1f + off * 0.35f;
                    p.v.Add(hc + Vector3.Scale(Dir(az, pol), hr) * lift);
                }
            }
            for (int ia = 0; ia < A; ia++)
            {
                int a0 = start + ia * (V + 1), a1 = start + ((ia + 1) % A) * (V + 1);
                for (int iv = 0; iv < V; iv++)
                {
                    p.t.Add(a0 + iv); p.t.Add(a0 + iv + 1); p.t.Add(a1 + iv);
                    p.t.Add(a1 + iv); p.t.Add(a0 + iv + 1); p.t.Add(a1 + iv + 1);
                }
            }
        }

        static float Sq(float x) { return x * x; }

        // ------------------------------------------------------------------ Locks

        /// <summary>
        /// One lock (a clump of strands). It starts inside the scalp at (<paramref name="az"/>, <paramref name="polar"/>),
        /// heads off along <paramref name="flow"/> (flattened onto the head), hugs the head at layer depth
        /// <paramref name="off"/>, and bends with <paramref name="gravity"/> (weight), <paramref name="lift"/> (volume
        /// away from the head) and <paramref name="curl"/> (degrees over the whole lock; positive curls the tip in toward
        /// the head, negative flicks it out). <paramref name="minFrontY"/> stops bangs above the brows.
        /// A lock lies on the head (at its layer depth) until the head turns under — below <paramref name="hugTo"/>
        /// (the surface normal's height) — or until its last <paramref name="tipFree"/> part; from there it hangs
        /// with its own weight, so long hair falls straight from the widest point of the head instead of wrapping
        /// under it, and short tips can lift away.
        /// </summary>
        public void Lock(string group, int mat, float az, float polar, float off, Vector3 flow, float len, float width, float thick,
            float gravity = 1f, float lift = 0f, float curl = 0f, int segs = 10, float minFrontY = -9f, float hugTo = -0.25f, float tipFree = 0.2f)
        {
            var pts = new List<Vector3>();
            Vector3 p = Surf(az, polar, off * 0.3f);
            pts.Add(p);
            Vector3 n0 = Normal(p);
            Vector3 d = Vector3.ProjectOnPlane(flow, n0);
            if (d.sqrMagnitude < 1e-6f) d = Vector3.ProjectOnPlane(Vector3.down, n0);
            if (d.sqrMagnitude < 1e-6f) d = Vector3.back;
            d.Normalize();
            float ds = len / segs;
            bool hugging = true;
            float curlStep = 0f;
            for (int i = 0; i < segs; i++)
            {
                float s = (i + 1f) / segs;
                // Roots sit deep in the scalp; the lock rises to its layer over the first quarter.
                float layer = Mathf.Lerp(off * 0.3f, off, Mathf.Min(1f, s / 0.25f));
                Vector3 n = Normal(p);
                if (hugging && (n.y < hugTo || s > 1f - tipFree))
                {
                    hugging = false;
                    curlStep = curl / Mathf.Max(1, segs - i);
                }
                Vector3 np;
                if (hugging)
                {
                    // Flow over the head, drifting downward with weight.
                    d += Vector3.down * (gravity * ds * 6f);
                    Vector3 t = Vector3.ProjectOnPlane(d, n);
                    d = t.sqrMagnitude > 1e-8f ? t.normalized : Vector3.ProjectOnPlane(Vector3.down, n).normalized;
                    np = Snap(p + d * ds, layer);
                }
                else
                {
                    Vector3 side = Vector3.Cross(d, n);
                    // Positive curl turns the tip toward the head.
                    if (side.sqrMagnitude > 1e-6f) d = Quaternion.AngleAxis(-curlStep, side.normalized) * d;
                    d += Vector3.down * (gravity * ds * 10f) + n * (lift * ds * 6f);
                    d.Normalize();
                    np = Collide(p + d * ds, layer);
                }
                // Bangs end above the brows: never across the eyes.
                if (np.z - hc.z > 0.05f && Mathf.Abs(np.x - hc.x) < 0.24f && np.y < hc.y + minFrontY) break;
                Vector3 step = np - p;
                if (step.sqrMagnitude > 1e-8f) d = step.normalized;
                p = np;
                pts.Add(p);
            }
            Tube(group, mat, pts, width, thick);
        }

        /// <summary>A narrow ribbon of lighter colour lying along the top layer (the stylised shine).</summary>
        public void Highlight(string group, float az, float polar, float off, Vector3 flow, float len, float width)
        {
            Lock(group, Shine, az, polar, off, flow, len, width, width * 0.25f, 0.4f, 0f, 0f, 6, -9f, -1f, 0f);
        }

        void Tube(string group, int mat, List<Vector3> pts, float width, float thick)
        {
            int n = pts.Count;
            if (n < 3) return;
            const int K = 6;
            var part = Get(group, mat);
            int start = part.v.Count;
            for (int i = 0; i < n; i++)
            {
                Vector3 a = pts[Mathf.Max(0, i - 1)], b = pts[Mathf.Min(n - 1, i + 1)];
                Vector3 T = (b - a).sqrMagnitude > 1e-10f ? (b - a).normalized : Vector3.down;
                Vector3 N = Normal(pts[i]);
                Vector3 side = Vector3.Cross(T, N);
                if (side.sqrMagnitude < 1e-6f) side = Vector3.Cross(T, Vector3.forward);
                side.Normalize();
                Vector3 up = Vector3.Cross(side, T).normalized;
                float s = i / (float)(n - 1);
                float ts = Mathf.Clamp(taperStart, 0.05f, 0.9f);
                float taper = s < 0.1f ? Mathf.Lerp(0.8f, 1f, s / 0.1f) : s < ts ? 1f : Mathf.Pow(Mathf.Max(0f, 1f - (s - ts) / (1f - ts)), 0.55f);
                if (i == n - 1) taper = tipWidth * 0.5f;
                else taper = Mathf.Max(taper, tipWidth);
                float w = width * 0.5f * taper;
                float th = thick * 0.5f * Mathf.Lerp(1f, 0.4f, s) * Mathf.Min(1f, taper * 3f);
                // Clumps are slightly crescent-shaped in section: the edges curl toward the head.
                for (int k = 0; k < K; k++)
                {
                    float phi = k * Mathf.PI * 2f / K;
                    float cx = Mathf.Cos(phi), sy = Mathf.Sin(phi);
                    part.v.Add(pts[i] + side * (cx * w) + up * (sy * th - cx * cx * th * 0.35f));
                }
            }
            for (int i = 0; i < n - 1; i++)
                for (int k = 0; k < K; k++)
                {
                    int a0 = start + i * K + k, a1 = start + i * K + (k + 1) % K;
                    int b0 = a0 + K, b1 = a1 + K;
                    part.t.Add(a0); part.t.Add(b0); part.t.Add(a1);
                    part.t.Add(a1); part.t.Add(b0); part.t.Add(b1);
                }
            // Close the root (hidden inside the scalp, but keeps the outline clean).
            int c0 = part.v.Count;
            part.v.Add(pts[0]);
            for (int k = 0; k < K; k++)
            {
                part.t.Add(c0); part.t.Add(start + k); part.t.Add(start + (k + 1) % K);
            }
        }

        /// <summary>A direction in head space from rough components, normalised.</summary>
        public static Vector3 V(float x, float y, float z) { return new Vector3(x, y, z).normalized; }
    }
}
