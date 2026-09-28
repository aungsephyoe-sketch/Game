using System.Collections.Generic;
using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// Kiriha Village on the premium world standard: a timber village gate with plastered walls, a street of
    /// two-storey townhouses (glowing paper windows, noren curtains, tiled gable roofs), a lantern-lit market,
    /// the plaza under a great cherry tree (the hub, with its co-op gates and quest board), an old water mill by
    /// the bridge, and a shrine at the top of the village. Used for the home screen and the open-world hub.
    /// </summary>
    public static partial class PrototypeWorld
    {
        class HouseSpot
        {
            public Vector3 p, face;
            public float w, d;
            public bool two, mill;
            public int style;
        }

        static readonly List<HouseSpot> houses = new List<HouseSpot>();

        /// <summary>Where the co-op gates stand in the plaza (on the ground), and the way through each one.</summary>
        public static readonly List<Vector3> CoopGateSpots = new List<Vector3>();
        public static readonly List<Vector3> CoopGateDirs = new List<Vector3>();
        /// <summary>Hidden treasure spots in the village (for the open world's chest hunt).</summary>
        public static readonly List<Vector3> VillageChestSpots = new List<Vector3>();
        public static Vector3 VillagePlaza { get; private set; }
        public static Vector3 VillageBoard { get; private set; }

        static Mesh gableMesh;

        static readonly Color Timber = new Color(0.38f, 0.22f, 0.14f);
        static readonly Color Vermilion = new Color(0.84f, 0.2f, 0.14f);
        static readonly Color PaperGlow = new Color(1f, 0.8f, 0.52f);
        static readonly Color StoneGrey = new Color(0.52f, 0.52f, 0.58f);

        // ------------------------------------------------------------------ Planning

        static Vector3 PlaceInDir(int i)
        {
            Vector3 c = Journey.Flat(J.places[i].pos);
            Vector3 d = i > 0 ? c - Journey.Flat(J.places[i - 1].pos) : Journey.Flat(J.places[Mathf.Min(1, J.places.Count - 1)].pos) - c;
            d.y = 0f;
            return d.sqrMagnitude < 0.01f ? Vector3.forward : d.normalized;
        }

        static float RiverWobble(float s) { return Mathf.Sin(s * 0.05f) * Mathf.Clamp01((Mathf.Abs(s) - 10f) / 30f) * 9f; }

        /// <summary>Claims the village's building plots before any plant is scattered.</summary>
        static void VillageReserve()
        {
            for (int i = 0; i < J.places.Count; i++)
            {
                var pl = J.places[i];
                Vector3 c = Journey.Flat(pl.pos), dir = PlaceInDir(i), side = Vector3.Cross(Vector3.up, dir);
                if (pl.name.Contains("Shrine")) Reserve(c + dir * (pl.radius + 5f), 11f);
                if (i == 0)
                    for (float t = 8f; t < 24f; t += 3f)
                    {
                        Reserve(c - dir * 4f + side * t, 3f);
                        Reserve(c - dir * 4f - side * t, 3f);
                    }
            }
            PlanHouses();
            foreach (var h in houses) Reserve(h.p, Mathf.Max(h.w, h.d) * 0.62f + 1.2f);
        }

        static void PlanHouses()
        {
            houses.Clear();
            // The old mill stands on the far bank, its wheel turning in the river.
            if (hasBridge)
            {
                Vector3 flow = Vector3.Cross(Vector3.up, bridgeDir);
                float half = channels.Count > 0 ? channels[0].half : 2.6f;
                const float s = 13f;
                float dep = 6f;
                Vector3 mp = bridgeCenter + flow * s + bridgeDir * (RiverWobble(s) + half + 0.9f + dep * 0.5f);
                houses.Add(new HouseSpot { p = mp, face = -bridgeDir, w = 7f, d = dep, two = true, mill = true, style = 1 });
            }
            // Townhouses along both sides of the street, a second row behind them, and more round the clearings.
            for (int row = 0; row < 2; row++)
            {
                for (float s = 3f; s < J.Length - 3f; s += 7f)
                {
                    Vector3 p = Journey.Flat(J.PointAt(s));
                    Vector3 d = Journey.Flat(J.PointAt(s + 1f)) - p;
                    if (d.sqrMagnitude < 0.0001f) continue;
                    d.Normalize();
                    Vector3 side = Vector3.Cross(Vector3.up, d);
                    for (int sg = -1; sg <= 1; sg += 2)
                    {
                        float w = R(6f, 7.6f), dep = R(5.2f, 6.2f);
                        float off = J.halfWidth + 1.5f + dep * 0.5f + row * 7.5f;
                        TryHouse(p + side * sg * off + d * R(-0.8f, 0.8f), -side * sg, w, dep);
                    }
                }
                for (int i = 0; i < J.places.Count - 1; i++)
                {
                    var pl = J.places[i];
                    float r = pl.radius + 1.6f + row * 7.5f;
                    int n = Mathf.RoundToInt(r * Mathf.PI * 2f / 7.5f);
                    for (int k = 0; k < n; k++)
                    {
                        float a = (k + 0.5f * row) * Mathf.PI * 2f / n;
                        Vector3 o = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                        float w = R(6f, 7.6f), dep = R(5.2f, 6.2f);
                        TryHouse(Journey.Flat(pl.pos) + o * (r + dep * 0.5f), -o, w, dep);
                    }
                }
            }
        }

        static bool TryHouse(Vector3 at, Vector3 face, float w, float d)
        {
            if (PathDist(at.x, at.z) < J.halfWidth + 1.1f + d * 0.5f) return false;
            JourneyPlace pl;
            if (PlaceDist(at.x, at.z, out pl) < d * 0.5f + 1.2f) return false;
            if (NearChannel(at.x, at.z, d * 0.5f + 2.5f)) return false;
            if (hasBridge && (Journey.Flat(at) - bridgeCenter).magnitude < 12f) return false;
            var first = J.places[0];
            if ((Journey.Flat(at) - Journey.Flat(first.pos)).magnitude < first.radius + 5f) return false;
            // Nothing outside the village walls.
            if (Vector3.Dot(Journey.Flat(at) - Journey.Flat(first.pos), PlaceInDir(0)) < 2f + d * 0.5f) return false;
            int li = J.places.Count - 1;
            var last = J.places[li];
            if ((Journey.Flat(at) - Journey.Flat(last.pos)).magnitude < last.radius + 4f) return false;
            // Not in the shrine grounds behind the last clearing.
            if ((Journey.Flat(at) - (Journey.Flat(last.pos) + PlaceInDir(li) * (last.radius + 5f))).magnitude < 12f + d * 0.5f) return false;
            foreach (var h in houses)
                if ((h.p - at).magnitude < (Mathf.Max(h.w, h.d) + Mathf.Max(w, d)) * 0.5f + 0.5f) return false;
            houses.Add(new HouseSpot { p = at, face = face.normalized, w = w, d = d, two = rng.Next(5) < 2, style = rng.Next(3) });
            return true;
        }

        // ------------------------------------------------------------------ Building

        static void VillageLandmarks()
        {
            CoopGateSpots.Clear();
            CoopGateDirs.Clear();
            VillageChestSpots.Clear();
            // Most important first: the lights go where the camera spends its time.
            string[] order = { "Plaza", "Market", "Gate", "Bridge", "Shrine" };
            foreach (var key in order)
                for (int i = 0; i < J.places.Count; i++)
                {
                    var pl = J.places[i];
                    if (!pl.name.Contains(key)) continue;
                    Vector3 c = Journey.Flat(pl.pos), dir = PlaceInDir(i), side = Vector3.Cross(Vector3.up, dir);
                    switch (key)
                    {
                        case "Plaza": Plaza(pl, c, dir, side); break;
                        case "Market": Market(pl, c, dir, side); break;
                        case "Gate": VillageGate(pl, c, dir, side); break;
                        case "Bridge": Teahouse(pl, c, dir, side); break;
                        default: VillageShrine(pl, c, dir, side); break;
                    }
                }
            foreach (var h in houses) House(h);
            Paving();
            StreetStrings();
            if (hasBridge)
            {
                BridgeSet();
                MillWheel();
            }
            RoadGuides();
        }

        /// <summary>
        /// Real paving: irregular flagstones laid over the streets, the market and the plaza, each a slightly
        /// different grey with a worn top, with dark joints and moss between them and a kerb along the street
        /// edge. Built into the combined rock mesh, so thousands of stones cost a couple of draw calls.
        /// </summary>
        static void Paving()
        {
            var cube = WorldKit.Prim(PrimitiveType.Cube);
            // Big, bright cartoon flagstones (reads cleanly from the battle camera too).
            Color[] stones = { new Color(0.78f, 0.56f, 0.52f), new Color(0.72f, 0.5f, 0.48f), new Color(0.84f, 0.62f, 0.56f), new Color(0.68f, 0.48f, 0.5f), new Color(0.8f, 0.58f, 0.5f) };
            const float cell = 1.45f;
            Vector3 mn = J.places[0].pos, mx = mn;
            foreach (var p in J.path) { mn = Vector3.Min(mn, p); mx = Vector3.Max(mx, p); }
            foreach (var pl in J.places) { mn = Vector3.Min(mn, pl.pos - Vector3.one * pl.radius); mx = Vector3.Max(mx, pl.pos + Vector3.one * pl.radius); }
            int laid = 0;
            for (float z = mn.z - 2f; z < mx.z + 2f; z += cell)
            {
                // Running bond: every other row shifted by half a stone.
                float shift = (Mathf.FloorToInt(z / cell) & 1) == 0 ? 0f : cell * 0.5f;
                for (float x = mn.x - 8f + shift; x < mx.x + 8f; x += cell)
                {
                    float jx = x + R(-0.06f, 0.06f), jz = z + R(-0.06f, 0.06f);
                    float pd = PathDist(jx, jz);
                    JourneyPlace pl;
                    float cd = PlaceDist(jx, jz, out pl);
                    bool street = pd < J.halfWidth - 0.35f;
                    bool square = cd < -0.6f && pl != null && !pl.isBridge && pl != J.places[J.places.Count - 1];
                    if (!street && !square) continue;
                    if (NearChannel(jx, jz, 0.5f)) continue;
                    if (hasBridge && (new Vector3(jx, 0f, jz) - bridgeCenter).magnitude < 6f) continue;
                    // Worn gaps where a stone is missing and grass creeps in.
                    if (rng.NextDouble() < 0.03) { Tuft(new Vector3(jx, Ground(new Vector3(jx, 0f, jz)), jz), R(0.3f, 0.45f), P.grassA); continue; }
                    Vector3 at = new Vector3(jx, 0f, jz);
                    at.y = Ground(at) + 0.02f;
                    float w = cell * R(0.84f, 0.93f), d = cell * R(0.8f, 0.92f);
                    Color c = Jitter(stones[rng.Next(stones.Length)], 0.06f);
                    var q = Quaternion.Euler(R(-0.8f, 0.8f), R(-3f, 3f), R(-0.8f, 0.8f));
                    solid.Add(cube, at, q, new Vector3(w, 0.12f, d), c);
                    laid++;
                    // Moss in some joints.
                    if (rng.NextDouble() < 0.06) solid.Add(cube, at + new Vector3(w * 0.5f, -0.01f, R(-0.3f, 0.3f)), q, new Vector3(0.1f, 0.05f, R(0.3f, 0.7f)), new Color(0.2f, 0.34f, 0.22f));
                }
            }
            // Kerb stones along both edges of the street (not through the squares).
            for (float s = 0f; s < J.Length; s += 1.1f)
            {
                Vector3 p = Journey.Flat(J.PointAt(s));
                Vector3 dr = Journey.Flat(J.PointAt(s + 0.5f)) - p;
                if (dr.sqrMagnitude < 0.0001f) continue;
                dr.Normalize();
                Vector3 side = Vector3.Cross(Vector3.up, dr);
                for (int sg = -1; sg <= 1; sg += 2)
                {
                    Vector3 at = p + side * sg * (J.halfWidth - 0.15f);
                    JourneyPlace pl;
                    if (PlaceDist(at.x, at.z, out pl) < 0.5f || NearChannel(at.x, at.z, 0.5f)) continue;
                    if (hasBridge && (at - bridgeCenter).magnitude < 6f) continue;
                    at.y = Ground(at) + 0.05f;
                    solid.Add(cube, at, Quaternion.LookRotation(dr), new Vector3(0.28f, 0.14f, 1.02f), Jitter(new Color(0.44f, 0.44f, 0.48f), 0.05f));
                }
            }
        }

        static GameObject Glow(Vector3 pos, Vector3 scale, Quaternion rot, Color c)
        {
            var go = MeshFactory.Primitive(PrimitiveType.Cube, Par(stat), Vector3.zero, scale, TE(c));
            go.transform.position = pos;
            go.transform.rotation = rot;
            go.name = "WindowGlow";
            return go;
        }

        /// <summary>Unit triangular prism (the gable end under a roof): x = length, z = base width, apex at y = 1.</summary>
        static Mesh Gable()
        {
            if (gableMesh != null) return gableMesh;
            var v = new List<Vector3>();
            var t = new List<int>();
            Vector3[] L = { new Vector3(-0.5f, 0f, -0.5f), new Vector3(-0.5f, 0f, 0.5f), new Vector3(-0.5f, 1f, 0f) };
            Vector3[] Rr = { new Vector3(0.5f, 0f, -0.5f), new Vector3(0.5f, 0f, 0.5f), new Vector3(0.5f, 1f, 0f) };
            // End triangles.
            v.AddRange(new[] { L[0], L[1], L[2] }); t.AddRange(new[] { 0, 1, 2 });
            v.AddRange(new[] { Rr[0], Rr[2], Rr[1] }); t.AddRange(new[] { 3, 4, 5 });
            // Sloped faces.
            int b = v.Count;
            v.AddRange(new[] { L[1], Rr[1], Rr[2], L[2] }); t.AddRange(new[] { b, b + 1, b + 2, b, b + 2, b + 3 });
            b = v.Count;
            v.AddRange(new[] { Rr[0], L[0], L[2], Rr[2] }); t.AddRange(new[] { b, b + 1, b + 2, b, b + 2, b + 3 });
            gableMesh = new Mesh { name = "Gable" };
            gableMesh.SetVertices(v);
            gableMesh.SetTriangles(t, 0);
            gableMesh.RecalculateNormals();
            gableMesh.RecalculateBounds();
            return gableMesh;
        }

        /// <summary>A gable roof whose ridge runs along the local x axis: two tiled slabs, tile ribs, the ridge cap
        /// and upturned end tiles, with the gable ends filled in below.</summary>
        static void GableRoof(Vector3 baseCenter, Quaternion rot, float length, float depth, float pitch, float overhang, Color roof, Color wall, bool ribs)
        {
            Vector3 f = rot * Vector3.forward, sd = rot * Vector3.right;
            float tan = Mathf.Tan(pitch * Mathf.Deg2Rad), cos = Mathf.Cos(pitch * Mathf.Deg2Rad);
            float rise = depth * 0.5f * tan;
            var gable = MeshFactory.MeshObject(Gable(), Par(stat), Vector3.zero, new Vector3(length, rise, depth), T(wall));
            gable.transform.position = baseCenter;
            gable.transform.rotation = rot;
            float hd = depth * 0.5f + overhang;
            float ridgeY = rise + 0.1f;
            float len = length + 1.2f;
            for (int s = -1; s <= 1; s += 2)
            {
                var q = rot * Quaternion.Euler(s * pitch, 0f, 0f);
                Vector3 center = baseCenter + Vector3.up * (ridgeY - hd * tan * 0.5f) + f * s * hd * 0.5f;
                Box(stat, center, new Vector3(len, 0.34f, hd / cos), roof, q);
                // The lowest course of tiles, a shade darker, reads as the eave edge.
                Box(stat, baseCenter + Vector3.up * (ridgeY - hd * tan + 0.06f) + f * s * (hd - 0.12f), new Vector3(len + 0.05f, 0.14f, 0.3f), roof * 0.8f, q);
                if (ribs)
                {
                    Vector3 nrm = q * Vector3.up;
                    for (float x = -len * 0.5f + 0.45f; x < len * 0.5f - 0.3f; x += 0.9f)
                    {
                        var rib = Box(stat, center + nrm * 0.1f + sd * x, new Vector3(0.1f, 0.07f, hd / cos - 0.1f), roof * 0.85f, q, 0.005f);
                        rib.name = "Rib";
                    }
                }
            }
            Box(stat, baseCenter + Vector3.up * (ridgeY + 0.2f), new Vector3(len + 0.2f, 0.42f, 0.5f), roof * 0.72f, rot);
            for (int s = -1; s <= 1; s += 2)
                Box(stat, baseCenter + Vector3.up * (ridgeY + 0.3f) + sd * s * (len * 0.5f + 0.02f), new Vector3(0.32f, 0.46f, 0.42f), roof * 0.6f, rot * Quaternion.Euler(0f, 0f, -s * 10f));
        }

        static void House(HouseSpot h)
        {
            BeginProp(h.mill ? "Mill" : "House");
            Vector3 p = OnGround(h.p);
            var rot = Quaternion.LookRotation(h.face);
            Vector3 f = h.face, sd = rot * Vector3.right;
            // Cartoon palette: cream and warm walls, bold roof colours.
            Color[] walls = { new Color(0.96f, 0.86f, 0.72f), new Color(0.62f, 0.38f, 0.28f), new Color(0.92f, 0.76f, 0.62f) };
            Color[] roofs = { new Color(0.18f, 0.28f, 0.7f), new Color(0.72f, 0.16f, 0.2f), new Color(0.1f, 0.48f, 0.46f) };
            Color[] norens = { new Color(0.16f, 0.22f, 0.46f), new Color(0.62f, 0.14f, 0.14f), new Color(0.2f, 0.36f, 0.3f), new Color(0.36f, 0.2f, 0.42f) };
            Color wall = walls[h.style % 3], roof = roofs[(h.style + (h.two ? 1 : 0)) % 3];
            float w = h.w, d = h.d, H1 = 2.9f, b0 = 0.4f;
            // Stone footing that runs into the ground, so a house on a gentle slope never floats.
            Box(stat, p + Vector3.up * ((b0 - 1.6f) * 0.5f), new Vector3(w + 0.3f, b0 + 1.6f, d + 0.3f), StoneGrey, rot);
            Box(stat, p + Vector3.up * (b0 + H1 * 0.5f), new Vector3(w, H1, d), wall, rot);
            // Timber frame: corner posts, front posts and the head beam.
            for (int sx = -1; sx <= 1; sx += 2)
                for (int sz = -1; sz <= 1; sz += 2)
                    Box(stat, p + sd * sx * (w * 0.5f) + f * sz * (d * 0.5f) + Vector3.up * (b0 + H1 * 0.5f), new Vector3(0.24f, H1, 0.24f), Timber, rot);
            int bays = Mathf.Max(2, Mathf.RoundToInt(w / 2.2f));
            for (int k = 1; k < bays; k++)
                Box(stat, p + sd * (-w * 0.5f + k * w / bays) + f * (d * 0.5f + 0.04f) + Vector3.up * (b0 + H1 * 0.5f), new Vector3(0.16f, H1, 0.1f), Timber, rot);
            Box(stat, p + f * (d * 0.5f + 0.06f) + Vector3.up * (b0 + H1 - 0.12f), new Vector3(w + 0.12f, 0.22f, 0.12f), Timber, rot);
            Box(stat, p + f * (d * 0.5f + 0.05f) + Vector3.up * (b0 + 0.1f), new Vector3(w + 0.1f, 0.16f, 0.1f), Timber, rot);

            // The doorway with its noren curtain.
            float doorX = (h.style == 1 ? -0.26f : 0.22f) * w;
            Box(stat, p + f * (d * 0.5f + 0.03f) + sd * doorX + Vector3.up * (b0 + 1.05f), new Vector3(1.4f, 2.1f, 0.06f), new Color(0.12f, 0.08f, 0.07f), rot);
            Glow(p + f * (d * 0.5f + 0.01f) + sd * doorX + Vector3.up * (b0 + 0.95f), new Vector3(1.1f, 1.7f, 0.04f), rot, PaperGlow * 0.7f);
            var noren = new GameObject("Noren").transform;
            noren.SetParent(dyn, false);
            noren.position = p + f * (d * 0.5f + 0.12f) + sd * doorX + Vector3.up * (b0 + 2.08f);
            noren.rotation = rot;
            var nc = T(norens[(h.style + rng.Next(4)) % 4]);
            for (int k = 0; k < 3; k++)
                MeshFactory.Primitive(PrimitiveType.Cube, noren, new Vector3((k - 1) * 0.47f, -0.42f, 0f), new Vector3(0.45f, 0.84f, 0.03f), nc);
            MeshFactory.MeshObject(MeshFactory.SmoothSphere(), noren, new Vector3(0f, -0.36f, 0.02f), new Vector3(0.3f, 0.3f, 0.02f), T(new Color(0.95f, 0.93f, 0.88f), 0f), false);
            MeshFactory.Primitive(PrimitiveType.Cube, noren, Vector3.zero, new Vector3(1.55f, 0.05f, 0.05f), T(Timber));
            var nsw = noren.gameObject.AddComponent<Sway>();
            nsw.Amount = 4f;
            nsw.Speed = R(0.6f, 1f);

            // Paper windows glowing with lamplight (a lattice over each), or a full slatted shop front.
            if (h.style == 2 && !h.mill)
            {
                float from = -w * 0.5f + 0.3f, to = w * 0.5f - 0.3f;
                for (float x = from; x < to; x += 0.3f)
                {
                    if (Mathf.Abs(x - doorX) < 0.9f) continue;
                    Box(stat, p + f * (d * 0.5f + 0.08f) + sd * x + Vector3.up * (b0 + 1.25f), new Vector3(0.07f, 2.1f, 0.05f), Timber, rot, 0.005f);
                }
                for (int s = -1; s <= 1; s += 2)
                {
                    float cx = s < 0 ? (from + doorX - 0.9f) * 0.5f : (doorX + 0.9f + to) * 0.5f;
                    float cw = s < 0 ? (doorX - 0.9f - from) : (to - doorX - 0.9f);
                    if (cw > 0.4f) Glow(p + f * (d * 0.5f + 0.02f) + sd * cx + Vector3.up * (b0 + 1.25f), new Vector3(cw, 2f, 0.04f), rot, PaperGlow * 0.85f);
                }
            }
            else
            {
                foreach (float xf in new[] { -0.3f, 0.3f })
                {
                    float x = xf * w;
                    if (Mathf.Abs(x - doorX) < 1.4f) continue;
                    Vector3 wc = p + f * (d * 0.5f + 0.03f) + sd * x + Vector3.up * (b0 + 1.5f);
                    Glow(wc, new Vector3(1.2f, 0.95f, 0.04f), rot, PaperGlow);
                    Box(stat, wc + f * 0.03f, new Vector3(0.06f, 1f, 0.04f), Timber, rot, 0.005f);
                    Box(stat, wc + f * 0.03f, new Vector3(1.25f, 0.06f, 0.04f), Timber, rot, 0.005f);
                    Box(stat, wc + f * 0.03f + Vector3.down * 0.5f, new Vector3(1.35f, 0.08f, 0.1f), Timber, rot, 0.005f);
                }
            }
            // A short tiled canopy over the ground floor.
            float topY = b0 + H1;
            Box(stat, p + Vector3.up * (topY + 0.12f) + f * (d * 0.5f + 0.5f), new Vector3(w + 0.5f, 0.12f, 1.3f), roof, rot * Quaternion.Euler(20f, 0f, 0f));

            float roofDepth = d;
            Vector3 roofBase = p;
            if (h.two)
            {
                // Upper storey, set back a little, with a slatted window.
                float H2 = 2.1f, d2 = d - 1f;
                roofBase = p - f * 0.5f;
                roofDepth = d2;
                Box(stat, roofBase + Vector3.up * (topY + H2 * 0.5f), new Vector3(w, H2, d2), wall, rot);
                for (int sx = -1; sx <= 1; sx += 2)
                    Box(stat, roofBase + sd * sx * (w * 0.5f) + f * (d2 * 0.5f) + Vector3.up * (topY + H2 * 0.5f), new Vector3(0.22f, H2, 0.22f), Timber, rot);
                Vector3 uw = roofBase + f * (d2 * 0.5f + 0.02f) + Vector3.up * (topY + 1.1f);
                Glow(uw, new Vector3(w * 0.55f, 0.55f, 0.04f), rot, PaperGlow * 0.9f);
                for (float x = -w * 0.26f; x <= w * 0.26f; x += w * 0.075f)
                    Box(stat, uw + f * 0.04f + sd * x, new Vector3(0.09f, 0.6f, 0.05f), Timber, rot, 0.005f);
                topY += H2;
            }
            GableRoof(roofBase + Vector3.up * topY, rot, w, roofDepth, 30f, 1.05f, roof, wall, false);

            // Life around the house.
            if (rng.Next(10) < 6 || h.mill)
                HangingLantern(p + f * (d * 0.5f + 0.4f) + sd * (doorX + (doorX < 0f ? 1.1f : -1.1f)) + Vector3.up * (b0 + 2.65f), rng.Next(2) == 0 ? new Color(0.95f, 0.36f, 0.22f) : new Color(1f, 0.86f, 0.6f), false);
            Vector3 corner = p + f * (d * 0.5f + 0.45f) + sd * (w * 0.5f - 0.45f) * (doorX < 0f ? 1f : -1f);
            switch (rng.Next(4))
            {
                case 0:
                    Cyl(stat, OnGround(corner), 0.34f, 0.85f, new Color(0.4f, 0.28f, 0.18f), rot, 10);
                    Cyl(stat, OnGround(corner) + Vector3.up * 0.84f, 0.3f, 0.02f, new Color(0.12f, 0.2f, 0.3f), rot, 10);
                    break;
                case 1:
                    Cyl(stat, OnGround(corner), 0.24f, 0.4f, new Color(0.46f, 0.3f, 0.22f), rot, 8);
                    Sph(stat, OnGround(corner) + Vector3.up * 0.65f, new Vector3(0.7f, 0.5f, 0.7f), VillageGreen * 1.2f);
                    break;
                case 2:
                    for (int k = 0; k < 3; k++)
                        Cyl(stat, OnGround(corner) + Vector3.up * (0.12f + k * 0.2f) + sd * R(-0.1f, 0.1f), 0.1f, 1f, new Color(0.5f, 0.36f, 0.24f), rot * Quaternion.Euler(0f, 0f, 90f), 6);
                    break;
            }
            EndProp();
        }

        static readonly System.Collections.Generic.Dictionary<string, Material> glowMats = new System.Collections.Generic.Dictionary<string, Material>();

        static Material LanternHalo(Color c)
        {
            string key = "halo" + ColorUtility.ToHtmlStringRGB(c);
            Material m;
            if (!glowMats.TryGetValue(key, out m) || m == null) { m = MaterialFactory.Additive(new Color(c.r, c.g * 0.85f, c.b * 0.6f, 0.14f)); glowMats[key] = m; }
            return m;
        }

        /// <summary>A warm pool of lantern light on the ground under a light source.</summary>
        static void GlowPool(Vector3 above, Color c, float radius)
        {
            Vector3 g = OnGround(above);
            if (above.y - g.y > 6f) return;
            string key = "pool" + ColorUtility.ToHtmlStringRGB(c);
            Material m;
            if (!glowMats.TryGetValue(key, out m) || m == null) { m = MaterialFactory.Additive(new Color(c.r, c.g * 0.75f, c.b * 0.45f, 0.22f), true); glowMats[key] = m; }
            var pool = MeshFactory.MeshObject(MeshFactory.PlanarDisc(), dyn, g + Vector3.up * 0.05f, new Vector3(radius, 1f, radius), m, false);
            pool.name = "LightPool";
        }

        static void HangingLantern(Vector3 top, Color c, bool light)
        {
            var l = new GameObject("PaperLantern").transform;
            l.SetParent(dyn, false);
            l.position = top;
            var sw = l.gameObject.AddComponent<Sway>();
            sw.Amount = 4f;
            sw.Speed = R(0.5f, 0.9f);
            var body = MeshFactory.MeshObject(MeshFactory.SmoothSphere(), l, new Vector3(0f, -0.34f, 0f), new Vector3(0.38f, 0.48f, 0.38f), TE(c), false);
            body.name = "Glow";
            var cap = T(new Color(0.12f, 0.08f, 0.07f), 0f);
            MeshFactory.MeshObject(MeshFactory.SmoothCylinder(), l, new Vector3(0f, -0.1f, 0f), new Vector3(0.2f, 0.03f, 0.2f), cap, false);
            MeshFactory.MeshObject(MeshFactory.SmoothCylinder(), l, new Vector3(0f, -0.58f, 0f), new Vector3(0.2f, 0.03f, 0.2f), cap, false);
            // Fake glow (no real light needed): a soft halo round the paper and a warm pool on the ground below.
            var halo = MeshFactory.MeshObject(MeshFactory.SmoothSphere(), l, new Vector3(0f, -0.34f, 0f), Vector3.one * 1.3f, LanternHalo(c), false);
            halo.name = "Halo";
            GlowPool(top + Vector3.down * 0.4f, c, 2.6f);
            if (light) PointLight(top + Vector3.down * 0.4f, P.lantern, 7f, 1.1f);
        }

        /// <summary>Lantern strings between tall poles across the street.</summary>
        static void StreetStrings()
        {
            Color wood = new Color(0.3f, 0.21f, 0.15f), cord = new Color(0.15f, 0.1f, 0.08f);
            int n = 0;
            for (float s = 12f; s < J.Length - 6f; s += 7f)
            {
                Vector3 p = Journey.Flat(J.PointAt(s));
                JourneyPlace pl;
                float pd = PlaceDist(p.x, p.z, out pl);
                if (pl != null && pd < 1.5f) continue;
                // Keep clear of the stone lanterns RoadGuides puts along the road every 13 m from 8 m.
                float m = (s - 8f) % 13f;
                if (m < 2.5f || m > 10.5f) continue;
                if (hasBridge && (p - bridgeCenter).magnitude < 10f) continue;
                if (NearChannel(p.x, p.z, 3f)) continue;
                Vector3 d = Journey.Flat(J.PointAt(s + 1f)) - p;
                if (d.sqrMagnitude < 0.0001f) continue;
                d.Normalize();
                Vector3 side = Vector3.Cross(Vector3.up, d);
                Vector3 a = OnGround(p - side * (J.halfWidth + 0.8f)), b = OnGround(p + side * (J.halfWidth + 0.8f));
                BeginProp("LanternPoles");
                Cyl(stat, a, 0.1f, 4.5f, wood, Quaternion.identity, 8);
                Cyl(stat, b, 0.1f, 4.5f, wood, Quaternion.identity, 8);
                EndProp();
                Vector3 ta = a + Vector3.up * 4.3f, tb = b + Vector3.up * 4.3f;
                Vector3 prev = ta;
                const int seg = 6;
                for (int k = 1; k <= seg; k++)
                {
                    float t = k / (float)seg;
                    Vector3 q = Vector3.Lerp(ta, tb, t) + Vector3.down * Mathf.Sin(t * Mathf.PI) * 0.45f;
                    Box(stat, (prev + q) * 0.5f, new Vector3(0.03f, 0.03f, Vector3.Distance(prev, q) + 0.02f), cord, Quaternion.LookRotation(q - prev), 0f);
                    prev = q;
                }
                const int count = 5;
                for (int k = 1; k <= count; k++)
                {
                    float t = k / (float)(count + 1);
                    Vector3 q = Vector3.Lerp(ta, tb, t) + Vector3.down * Mathf.Sin(t * Mathf.PI) * 0.45f;
                    HangingLantern(q, (k + n) % 2 == 0 ? new Color(0.95f, 0.36f, 0.22f) : new Color(1f, 0.86f, 0.6f), k == 3 && n % 2 == 0);
                }
                n++;
            }
        }

        // ------------------------------------------------------------------ Places

        static void VillageGate(JourneyPlace pl, Vector3 c, Vector3 dir, Vector3 side)
        {
            Vector3 g = OnGround(c - dir * 4f);
            var rot = Quaternion.LookRotation(dir);
            Color wood = new Color(0.34f, 0.22f, 0.15f), roof = new Color(0.24f, 0.26f, 0.36f), plaster = new Color(0.9f, 0.86f, 0.78f);
            float half = J.halfWidth + 0.6f;
            BeginProp("VillageGate", true);
            for (int s = -1; s <= 1; s += 2)
            {
                Box(stat, g + side * s * half + Vector3.up * 2.4f, new Vector3(0.5f, 4.8f, 0.5f), wood, rot);
                Box(stat, g + side * s * half - dir * 1f + Vector3.up * 1.6f, new Vector3(0.3f, 3.2f, 0.3f), wood, rot);
                Box(stat, g + side * s * half - dir * 0.5f + Vector3.up * 3f, new Vector3(0.2f, 0.2f, 1f), wood, rot);
                // The great doors stand open against the walls.
                var door = Box(stat, g + side * s * (half - 0.35f) + dir * 1.35f + Vector3.up * 2f, new Vector3(0.14f, 3.6f, 2.5f), wood * 0.8f, rot);
                door.name = "GateDoor";
                for (int k = 0; k < 3; k++)
                    Box(stat, g + side * s * (half - 0.44f) + dir * 1.35f + Vector3.up * (0.8f + k * 1.2f), new Vector3(0.05f, 0.12f, 2.5f), new Color(0.1f, 0.08f, 0.07f), rot, 0f);
            }
            Box(stat, g + Vector3.up * 4.6f, new Vector3(half * 2f + 1.4f, 0.4f, 0.55f), wood, rot);
            Box(stat, g + Vector3.up * 3.9f + dir * 0.3f, new Vector3(1.8f, 0.7f, 0.08f), new Color(0.1f, 0.07f, 0.06f), rot);
            Box(stat, g + Vector3.up * 3.9f + dir * 0.35f, new Vector3(1.5f, 0.45f, 0.04f), new Color(0.85f, 0.7f, 0.3f), rot, 0f);
            GableRoof(g + Vector3.up * 4.8f, rot * Quaternion.Euler(0f, 0f, 0f), half * 2f + 1f, 2.2f, 30f, 0.6f, roof, wood, false);
            // Plastered walls running off either side, capped with tiles.
            for (int s = -1; s <= 1; s += 2)
            {
                const float len = 16f;
                Vector3 wc = g + side * s * (half + 0.3f + len * 0.5f);
                wc.y = Ground(wc);
                Box(stat, wc + Vector3.up * 0.1f, new Vector3(len, 1.4f, 0.6f), StoneGrey, rot);
                Box(stat, wc + Vector3.up * 1.6f, new Vector3(len, 1.8f, 0.45f), plaster, rot);
                Box(stat, wc + Vector3.up * 2.6f, new Vector3(len + 0.2f, 0.2f, 0.95f), roof, rot);
                Box(stat, wc + Vector3.up * 2.76f, new Vector3(len + 0.2f, 0.16f, 0.3f), roof * 0.7f, rot);
            }
            EndProp();
            StoneLantern(g + dir * 2f + side * (half + 1.4f), 1.1f, true);
            StoneLantern(g + dir * 2f - side * (half + 1.4f), 1.1f, false);
            Banner(c + side * (pl.radius - 1f) + dir * 2f, P.accent, 3.6f);
            Banner(c - side * (pl.radius - 1f) + dir * 2f, new Color(0.2f, 0.3f, 0.6f), 3.6f);
            Signpost(c + side * (J.halfWidth + 1.6f) + dir * 4f, dir);
            VillageChestSpots.Add(OnGround(c - side * (pl.radius - 2f) - dir * 1.5f));
        }

        static void Market(JourneyPlace pl, Vector3 c, Vector3 dir, Vector3 side)
        {
            int kind = 0;
            for (int k = -1; k <= 1; k++)
                for (int s = -1; s <= 1; s += 2)
                {
                    Vector3 at = c + dir * (k * 5.6f) + side * s * (J.halfWidth + 2.1f);
                    if (PathDist(at.x, at.z) < J.halfWidth + 1.2f) continue;
                    Stall(at, -side * s, kind++);
                }
            Banner(c + dir * (pl.radius - 1f) + side * (J.halfWidth + 0.9f), new Color(0.9f, 0.74f, 0.3f), 3.4f);
            Banner(c - dir * (pl.radius - 1f) - side * (J.halfWidth + 0.9f), P.accent, 3.4f);
            VillageChestSpots.Add(OnGround(c + side * (J.halfWidth + 3.6f) + dir * 8.2f));
        }

        static void Stall(Vector3 p, Vector3 face, int kind)
        {
            BeginProp("Stall");
            p = OnGround(p);
            var rot = Quaternion.LookRotation(face);
            Vector3 sd = rot * Vector3.right;
            Color wood = new Color(0.46f, 0.32f, 0.2f);
            Color[] cloths = { new Color(0.82f, 0.2f, 0.18f), new Color(0.2f, 0.34f, 0.62f), new Color(0.9f, 0.74f, 0.3f), new Color(0.3f, 0.52f, 0.4f), new Color(0.56f, 0.3f, 0.6f), new Color(0.9f, 0.5f, 0.3f) };
            Color cloth = cloths[kind % cloths.Length], cloth2 = Color.Lerp(cloth, Color.white, 0.78f);
            for (int sx = -1; sx <= 1; sx += 2)
                for (int sz = -1; sz <= 1; sz += 2)
                    Cyl(stat, p + sd * sx * 1.3f + face * sz * 0.8f, 0.06f, sz > 0 ? 2.2f : 2.7f, wood, rot, 6);
            Box(stat, p + Vector3.up * 0.45f + face * 0.35f, new Vector3(2.5f, 0.9f, 0.8f), wood, rot);
            Box(stat, p + Vector3.up * 0.93f + face * 0.4f, new Vector3(2.7f, 0.06f, 1f), wood * 1.15f, rot);
            for (int k = 0; k < 5; k++)
                Box(stat, p + Vector3.up * 2.46f + sd * (-1.16f + k * 0.58f), new Vector3(0.58f, 0.05f, 2.1f), k % 2 == 0 ? cloth : cloth2, rot * Quaternion.Euler(14f, 0f, 0f), 0.01f);
            Box(stat, p + Vector3.up * 2.12f + face * 1.1f, new Vector3(2.95f, 0.3f, 0.04f), cloth, rot, 0.01f);
            // Goods on the counter: fruit, pottery or bolts of cloth.
            for (int g = 0; g < 6; g++)
            {
                Vector3 at = p + Vector3.up * 0.98f + face * R(0.15f, 0.65f) + sd * (-1.05f + g * 0.42f);
                switch (kind % 3)
                {
                    case 0: Sph(stat, at + Vector3.up * 0.1f, Vector3.one * 0.22f, g % 3 == 0 ? new Color(1f, 0.55f, 0.15f) : g % 3 == 1 ? new Color(0.85f, 0.2f, 0.2f) : new Color(0.55f, 0.75f, 0.25f)); break;
                    case 1: Cyl(stat, at, 0.12f, R(0.2f, 0.36f), g % 2 == 0 ? new Color(0.3f, 0.36f, 0.5f) : new Color(0.72f, 0.56f, 0.4f), rot, 8); break;
                    default: Cyl(stat, at + Vector3.up * 0.1f, 0.1f, 0.34f, cloths[(g + kind) % cloths.Length], rot * Quaternion.Euler(0f, 0f, 90f), 8); break;
                }
            }
            Box(stat, p - face * 0.9f + sd * 0.8f + Vector3.up * 0.3f, new Vector3(0.6f, 0.6f, 0.6f), wood * 0.85f, rot * Quaternion.Euler(0f, 12f, 0f));
            EndProp();
            HangingLantern(p + face * 1.12f + Vector3.up * 1.95f, kind % 2 == 0 ? new Color(0.95f, 0.36f, 0.22f) : new Color(1f, 0.86f, 0.6f), kind == 1);
        }

        static void Plaza(JourneyPlace pl, Vector3 c, Vector3 dir, Vector3 side)
        {
            VillagePlaza = OnGround(c);
            SakuraTree(c);
            // The three co-op gates on the rim of the plaza.
            float[] angs = { -70f, 70f, -145f };
            Color[] gateCols = { Vermilion, new Color(0.3f, 0.26f, 0.62f), new Color(0.12f, 0.1f, 0.1f) };
            for (int i = 0; i < angs.Length; i++)
            {
                Vector3 o = Quaternion.Euler(0f, angs[i], 0f) * dir;
                Vector3 gp = OnGround(c + o * (pl.radius - 2.8f));
                CoopGateSpots.Add(gp);
                CoopGateDirs.Add(o);
                var q = Quaternion.LookRotation(o);
                Cyl(stat, gp + Vector3.down * 0.05f, 2.2f, 0.2f, StoneGrey * 0.9f, q, 20);
                Cyl(stat, gp + Vector3.up * 0.14f, 1.8f, 0.06f, StoneGrey * 1.1f, q, 20);
                Torii(gp + o * 0.3f, o, 3.4f, 3.8f, gateCols[i]);
                Vector3 across = Vector3.Cross(Vector3.up, o);
                StoneLantern(gp - o * 1.4f + across * 2.6f, 0.7f, false);
                StoneLantern(gp - o * 1.4f - across * 2.6f, 0.7f, false);
            }
            // A ring of stone lanterns (not on the road through the plaza, not in front of a gate).
            for (int k = 0; k < 10; k++)
            {
                Vector3 q = c + Quaternion.Euler(0f, k * 36f + 18f, 0f) * dir * (pl.radius - 5.5f);
                if (PathDist(q.x, q.z) < J.halfWidth + 0.6f) continue;
                bool nearGate = false;
                foreach (var gs in CoopGateSpots) if ((Journey.Flat(gs) - q).magnitude < 4f) nearGate = true;
                if (nearGate) continue;
                StoneLantern(q, 0.9f, k % 4 == 0);
            }
            // Benches round the tree.
            for (int k = 0; k < 4; k++)
            {
                Vector3 o = Quaternion.Euler(0f, k * 90f + 45f, 0f) * dir;
                Bench(c + o * 4.4f, -o, k % 2 == 0 ? new Color(0.46f, 0.32f, 0.2f) : new Color(0.72f, 0.18f, 0.16f));
            }
            Well(c - side * 9.5f - dir * 5f, side);
            QuestBoard(c + side * 9.5f - dir * 4f, -side);
            // Flower beds along the plaza rim.
            for (int k = 0; k < 40; k++)
            {
                float a = R(0f, Mathf.PI * 2f);
                Vector3 at = c + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * (pl.radius + R(-0.8f, 0.6f));
                if (PathDist(at.x, at.z) < J.halfWidth + 0.5f) continue;
                at.y = Ground(at);
                if (k % 3 == 0) Bush(at, R(0.5f, 0.8f), Color.Lerp(P.leafMid, VillageGreen, R(0.3f, 0.8f)));
                else Flower(at, k % 2 == 0 ? P.flowerA : P.flowerB);
            }
            VillageChestSpots.Add(OnGround(c - side * 11.5f + dir * 7f));
        }

        static void SakuraTree(Vector3 p)
        {
            p = OnGround(p);
            Obstacles.AddCircle(p, 2.3f);
            Reserve(p, 7f);
            BeginProp("TreePlanter", true);
            Cyl(stat, p, 2.3f, 0.5f, StoneGrey, Quaternion.identity, 22);
            Cyl(stat, p + Vector3.up * 0.5f, 2.38f, 0.1f, StoneGrey * 0.8f, Quaternion.identity, 22);
            Cyl(stat, p + Vector3.up * 0.52f, 2.1f, 0.04f, new Color(0.24f, 0.2f, 0.2f), Quaternion.identity, 22);
            EndProp();
            var b = new WorldMeshBuilder(treeRoot, folMat, "SakuraTree", true);
            Color bark = new Color(0.3f, 0.2f, 0.2f);
            b.Add(trunkMesh, p + Vector3.down * 0.3f, Quaternion.Euler(0f, 0f, 3f), new Vector3(1.15f, 7.2f, 1.15f), bark);
            for (int k = 0; k < 6; k++)
            {
                var q = Quaternion.Euler(0f, k * 60f + R(-15f, 15f), 0f) * Quaternion.Euler(0f, 0f, R(48f, 62f));
                b.Add(trunkMesh, p + Vector3.up * R(3.2f, 4.2f), q, new Vector3(0.5f, R(5f, 6.5f), 0.5f), bark * 0.92f);
            }
            // The crown: dozens of blossom clusters spread wide over the limbs, pale on top, deeper underneath,
            // with blossom-laden sprays drooping at the edge (never a few big balls).
            Vector3 crown = p + Vector3.up * 7.2f;
            for (int k = 0; k < 20; k++)
            {
                float a = R(0f, Mathf.PI * 2f), rr = Mathf.Sqrt(R(0.05f, 1f)) * 6f;
                float hy = Mathf.Lerp(2.2f, -1.4f, rr / 7f) + R(-0.8f, 0.8f);
                Vector3 o = new Vector3(Mathf.Cos(a) * rr, hy, Mathf.Sin(a) * rr);
                float sz = R(3.2f, 4.6f);
                bool under = hy < 0f;
                Color col = under ? Color.Lerp(P.leafDark, P.leafMid, R(0f, 0.6f)) : Color.Lerp(P.leafMid, P.leafLight, R(0.1f, 1f));
                b.Add(lowSphere, crown + o, Quaternion.Euler(0f, R(0f, 360f), 0f), new Vector3(sz, sz * 0.7f, sz), Jitter(col, 0.04f), 0.2f, 0.5f, 0.4f);
            }
            b.Flush();
            // Sacred rope with paper streamers round the trunk.
            var rope = MeshFactory.Lathe("pw_rope_sakura", new[] { new Vector2(0.72f, -0.14f), new Vector2(0.82f, 0f), new Vector2(0.72f, 0.14f) }, 20);
            var ropeGo = MeshFactory.MeshObject(rope, stat, Vector3.zero, Vector3.one, T(new Color(0.85f, 0.75f, 0.5f)));
            ropeGo.transform.position = p + Vector3.up * 2.3f;
            for (int k = 0; k < 6; k++)
            {
                float a = k * 60f * Mathf.Deg2Rad;
                Vector3 at = p + Vector3.up * 2.05f + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * 0.84f;
                for (int z = 0; z < 3; z++)
                    Box(stat, at + Vector3.down * (z * 0.2f), new Vector3(0.18f, 0.17f, 0.03f), Color.white, Quaternion.LookRotation(new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a))) * Quaternion.Euler(0f, 0f, z % 2 == 0 ? 15f : -15f), 0.005f);
            }
            // Lanterns hung from the lowest limbs.
            for (int k = 0; k < 6; k++)
            {
                float a = (k * 60f + 30f) * Mathf.Deg2Rad;
                Vector3 hang = p + new Vector3(Mathf.Cos(a) * 4.6f, 4.9f, Mathf.Sin(a) * 4.6f);
                Box(stat, hang + Vector3.up * 0.5f, new Vector3(0.03f, 1f, 0.03f), new Color(0.15f, 0.1f, 0.08f), Quaternion.identity, 0f);
                HangingLantern(hang, k % 2 == 0 ? new Color(1f, 0.62f, 0.32f) : new Color(1f, 0.86f, 0.6f), false);
            }
            PointLight(p + Vector3.up * 4.2f + Vector3.back * 2.5f, new Color(1f, 0.7f, 0.45f), 16f, 1.3f);
            // Fallen petals carpet the ground under the crown.
            var petals = MeshFactory.MeshObject(MeshFactory.PlanarDisc(), dyn, p + Vector3.up * 0.03f, new Vector3(10f, 1f, 10f), MaterialFactory.Transparent(new Color(1f, 0.72f, 0.84f, 0.32f), true), false);
            petals.name = "PetalCarpet";
        }

        static void Bench(Vector3 p, Vector3 face, Color seat)
        {
            BeginProp("Bench");
            p = OnGround(p);
            var rot = Quaternion.LookRotation(face);
            Vector3 sd = rot * Vector3.right;
            Color wood = new Color(0.36f, 0.25f, 0.17f);
            for (int s = -1; s <= 1; s += 2) Box(stat, p + sd * s * 0.75f + Vector3.up * 0.22f, new Vector3(0.12f, 0.44f, 0.5f), wood, rot);
            Box(stat, p + Vector3.up * 0.5f, new Vector3(1.9f, 0.1f, 0.62f), seat, rot);
            EndProp();
        }

        static void Well(Vector3 p, Vector3 face)
        {
            BeginProp("Well");
            p = OnGround(p);
            var rot = Quaternion.LookRotation(face);
            Vector3 sd = rot * Vector3.right;
            Color wood = new Color(0.36f, 0.25f, 0.17f), roof = new Color(0.26f, 0.28f, 0.38f);
            Cyl(stat, p, 0.95f, 0.85f, StoneGrey, rot, 14);
            Cyl(stat, p + Vector3.up * 0.8f, 0.72f, 0.03f, new Color(0.08f, 0.12f, 0.22f), rot, 14);
            for (int s = -1; s <= 1; s += 2) Box(stat, p + sd * s * 1.05f + Vector3.up * 1.25f, new Vector3(0.14f, 2.5f, 0.14f), wood, rot);
            Cyl(stat, p + sd * 1.1f + Vector3.up * 2.1f, 0.07f, 2.2f, wood, rot * Quaternion.Euler(0f, 0f, 90f), 8);
            Cyl(stat, p + Vector3.up * 1.95f + sd * 0.1f, 0.16f, 0.2f, wood * 0.8f, rot * Quaternion.Euler(0f, 0f, 90f), 10);
            Box(stat, p + Vector3.up * 1.4f, new Vector3(0.02f, 1.1f, 0.02f), new Color(0.6f, 0.5f, 0.35f), rot, 0f);
            Cyl(stat, p + Vector3.up * 0.72f + sd * 0.55f + rot * Vector3.forward * 0.55f, 0.16f, 0.26f, new Color(0.5f, 0.36f, 0.24f), rot, 8);
            GableRoof(p + Vector3.up * 2.45f, rot * Quaternion.Euler(0f, 90f, 0f), 1.2f, 2.2f, 30f, 0.25f, roof, wood, false);
            EndProp();
        }

        static void QuestBoard(Vector3 p, Vector3 face)
        {
            BeginProp("QuestBoard");
            p = OnGround(p);
            VillageBoard = p;
            var rot = Quaternion.LookRotation(face);
            Vector3 sd = rot * Vector3.right, f = face;
            Color wood = new Color(0.36f, 0.25f, 0.17f), roof = new Color(0.26f, 0.28f, 0.38f);
            for (int s = -1; s <= 1; s += 2) Box(stat, p + sd * s * 1.3f + Vector3.up * 1.4f, new Vector3(0.18f, 2.8f, 0.18f), wood, rot);
            Box(stat, p + Vector3.up * 1.6f, new Vector3(2.5f, 1.5f, 0.12f), wood * 1.2f, rot);
            for (int k = 0; k < 7; k++)
            {
                Vector3 at = p + f * 0.08f + Vector3.up * (1.3f + (k % 2) * 0.62f) + sd * (-0.9f + (k / 2) * 0.6f);
                Box(stat, at, new Vector3(0.42f, 0.5f, 0.02f), k == 3 ? new Color(0.95f, 0.8f, 0.5f) : new Color(0.95f, 0.93f, 0.86f), rot * Quaternion.Euler(0f, 0f, R(-7f, 7f)), 0.005f);
                Box(stat, at + f * 0.015f + Vector3.up * 0.16f, new Vector3(0.08f, 0.08f, 0.02f), Vermilion, rot, 0f);
            }
            GableRoof(p + Vector3.up * 2.8f, rot, 2.8f, 0.9f, 28f, 0.3f, roof, wood, false);
            EndProp();
            HangingLantern(p + f * 0.8f + sd * 1.1f + Vector3.up * 2.6f, new Color(1f, 0.86f, 0.6f), false);
        }

        static void Teahouse(JourneyPlace pl, Vector3 c, Vector3 dir, Vector3 side)
        {
            // A roadside tea stop: a red-covered bench under a paper parasol, and a lantern.
            Vector3 tp = c + side * (pl.radius - 2.5f) + dir * 1f;
            if (PathDist(tp.x, tp.z) < J.halfWidth + 1f) tp = c - side * (pl.radius - 2.5f) + dir * 1f;
            Bench(tp, (c - tp).normalized, new Color(0.78f, 0.16f, 0.14f));
            BeginProp("Parasol");
            Vector3 pp = OnGround(tp + dir * 1.2f);
            Cyl(stat, pp, 0.05f, 2.6f, new Color(0.36f, 0.25f, 0.17f), Quaternion.identity, 6);
            ConeAt(stat, pp + Vector3.up * 2.3f, new Vector3(3.4f, 0.7f, 3.4f), Vermilion, Quaternion.identity, 16);
            EndProp();
            StoneLantern(c - side * (pl.radius - 1.5f) - dir * 2f, 0.9f, false);
            Banner(tp - dir * 1.4f, new Color(0.3f, 0.52f, 0.4f), 3f);
            VillageChestSpots.Add(OnGround(tp - dir * 2.6f + (c - tp).normalized * 0.5f));
        }

        static void MillWheel()
        {
            Vector3 flow = Vector3.Cross(Vector3.up, bridgeDir);
            const float s = 13f;
            float half = channels.Count > 0 ? channels[0].half : 2.6f;
            float surface = channels.Count > 0 ? channels[0].surface : -0.55f;
            Vector3 axle = bridgeCenter + flow * s + bridgeDir * (RiverWobble(s) + half - 0.1f);
            axle.y = surface + 1.6f;
            var wheel = new GameObject("MillWheel").transform;
            wheel.SetParent(dyn, false);
            wheel.position = axle;
            wheel.rotation = Quaternion.LookRotation(bridgeDir);
            var wood = T(new Color(0.38f, 0.26f, 0.18f));
            for (int k = 0; k < 10; k++)
            {
                var q = Quaternion.Euler(0f, 0f, k * 36f);
                var spoke = MeshFactory.Primitive(PrimitiveType.Cube, wheel, q * new Vector3(0f, 1f, 0f), new Vector3(0.1f, 2f, 0.1f), wood);
                spoke.transform.localRotation = q;
                var paddle = MeshFactory.Primitive(PrimitiveType.Cube, wheel, q * new Vector3(0f, 2.05f, 0f), new Vector3(0.08f, 0.5f, 0.9f), wood);
                paddle.transform.localRotation = q;
                var q2 = Quaternion.Euler(0f, 0f, k * 36f + 18f);
                for (int e = -1; e <= 1; e += 2)
                {
                    var rim = MeshFactory.Primitive(PrimitiveType.Cube, wheel, q2 * new Vector3(0f, 1.72f, 0f) + Vector3.forward * e * 0.4f, new Vector3(1.12f, 0.1f, 0.08f), wood);
                    rim.transform.localRotation = q2;
                }
            }
            var hub = MeshFactory.MeshObject(MeshFactory.SmoothCylinder(), wheel, Vector3.zero, new Vector3(0.45f, 0.5f, 0.45f), wood);
            hub.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            wheel.gameObject.AddComponent<Spinner>().DegreesPerSecond = new Vector3(0f, 0f, -24f);
            Box(stat, axle + bridgeDir * 1.1f, new Vector3(0.18f, 0.18f, 2.2f), new Color(0.3f, 0.21f, 0.15f), Quaternion.LookRotation(bridgeDir));
            var foam = MeshFactory.MeshObject(MeshFactory.PlanarDisc(), dyn, new Vector3(axle.x, surface + 0.06f, axle.z), new Vector3(2.4f, 1f, 1.2f), MaterialFactory.Transparent(new Color(0.9f, 0.95f, 1f, 0.4f), true), false);
            foam.AddComponent<Pulse>().Speed = 3f;
        }

        static void VillageShrine(JourneyPlace pl, Vector3 c, Vector3 dir, Vector3 side)
        {
            Torii(c - dir * (pl.radius - 2f), dir, 5.4f, 5.2f, Vermilion);
            for (int t = -6; t <= 4; t += 5)
                for (int s = -1; s <= 1; s += 2)
                    StoneLantern(c + dir * t + side * s * 3.4f, 1f, t == 4 && s < 0);
            for (int s = -1; s <= 1; s += 2) Guardian(c + dir * (pl.radius - 3.5f) + side * s * 3.2f, -dir);
            Chozuya(c - side * (pl.radius - 3f) - dir * 1.5f, side);
            EmaRack(c + side * (pl.radius - 3f) - dir * 1f, -side);
            Hall(c + dir * (pl.radius + 4.5f), -dir);
            NearTree(OnGround(c + dir * (pl.radius + 2f) + side * 9.5f), 12f, 1);
            NearTree(OnGround(c + dir * (pl.radius + 2f) - side * 9.5f), 12f, 1);
            VillageChestSpots.Add(OnGround(c + side * (pl.radius - 2f) + dir * 5f));
        }

        static void Guardian(Vector3 p, Vector3 face)
        {
            BeginProp("Guardian");
            p = OnGround(p);
            var rot = Quaternion.LookRotation(face);
            Color stone = new Color(0.66f, 0.66f, 0.7f);
            Box(stat, p + Vector3.up * 0.5f, new Vector3(0.95f, 1f, 0.95f), StoneGrey, rot);
            Sph(stat, p + Vector3.up * 1.45f, new Vector3(0.7f, 0.85f, 0.9f), stone);
            Sph(stat, p + Vector3.up * 2.05f + face * 0.25f, Vector3.one * 0.62f, stone);
            Sph(stat, p + Vector3.up * 2.05f + face * 0.05f, new Vector3(0.82f, 0.62f, 0.5f), stone * 0.9f);
            Sph(stat, p + Vector3.up * 1.95f + face * 0.52f, new Vector3(0.28f, 0.2f, 0.2f), stone * 1.05f);
            ConeAt(stat, p + Vector3.up * 1.72f + face * 0.3f, new Vector3(0.6f, 0.35f, 0.6f), P.accent, rot * Quaternion.Euler(180f, 0f, 0f));
            EndProp();
        }

        static void Chozuya(Vector3 p, Vector3 face)
        {
            BeginProp("Chozuya");
            p = OnGround(p);
            var rot = Quaternion.LookRotation(face);
            Vector3 sd = rot * Vector3.right;
            Color wood = new Color(0.36f, 0.25f, 0.17f), roof = new Color(0.24f, 0.26f, 0.36f);
            for (int sx = -1; sx <= 1; sx += 2)
                for (int sz = -1; sz <= 1; sz += 2)
                    Box(stat, p + sd * sx * 1.2f + face * sz * 0.85f + Vector3.up * 1.25f, new Vector3(0.14f, 2.5f, 0.14f), wood, rot);
            Box(stat, p + Vector3.up * 0.38f, new Vector3(1.9f, 0.75f, 0.95f), StoneGrey, rot);
            Box(stat, p + Vector3.up * 0.76f, new Vector3(1.7f, 0.02f, 0.75f), P.water, rot, 0f);
            for (int k = 0; k < 3; k++)
                Cyl(stat, p + Vector3.up * 0.8f + sd * (-0.5f + k * 0.5f) + face * 0.3f, 0.02f, 0.8f, new Color(0.7f, 0.64f, 0.4f), rot * Quaternion.Euler(80f, 0f, 0f), 6);
            GableRoof(p + Vector3.up * 2.5f, rot, 2.4f, 1.7f, 30f, 0.35f, roof, wood, false);
            EndProp();
        }

        static void EmaRack(Vector3 p, Vector3 face)
        {
            BeginProp("EmaRack");
            p = OnGround(p);
            var rot = Quaternion.LookRotation(face);
            Vector3 sd = rot * Vector3.right;
            Color wood = new Color(0.36f, 0.25f, 0.17f);
            for (int s = -1; s <= 1; s += 2) Box(stat, p + sd * s * 1.2f + Vector3.up * 0.9f, new Vector3(0.12f, 1.8f, 0.12f), wood, rot);
            for (int r = 0; r < 2; r++)
            {
                Box(stat, p + Vector3.up * (1.05f + r * 0.5f), new Vector3(2.5f, 0.06f, 0.06f), wood, rot);
                for (int k = 0; k < 7; k++)
                    Box(stat, p + face * 0.05f + Vector3.up * (0.92f + r * 0.5f) + sd * (-1f + k * 0.33f), new Vector3(0.26f, 0.18f, 0.02f), new Color(0.86f, 0.74f, 0.52f), rot * Quaternion.Euler(0f, 0f, R(-8f, 8f)), 0.005f);
            }
            Box(stat, p + Vector3.up * 1.88f, new Vector3(2.7f, 0.1f, 0.5f), new Color(0.24f, 0.26f, 0.36f), rot);
            EndProp();
        }

        static void Hall(Vector3 p, Vector3 face)
        {
            BeginProp("ShrineHall", true);
            p = OnGround(p);
            var rot = Quaternion.LookRotation(face);
            Vector3 sd = rot * Vector3.right, f = face;
            Color white = new Color(0.94f, 0.9f, 0.84f), wood = new Color(0.4f, 0.28f, 0.19f), roof = new Color(0.2f, 0.22f, 0.3f), gold = new Color(0.9f, 0.74f, 0.3f);
            const float y0 = 1.2f;
            Box(stat, p + Vector3.up * 0.1f, new Vector3(11f, 2.2f, 9f), StoneGrey, rot);
            for (int k = 0; k < 3; k++)
            {
                float hgt = y0 - k * 0.4f;
                Box(stat, p + f * (4.5f + (k + 1) * 0.3f) + Vector3.up * (hgt * 0.5f), new Vector3(4f, hgt, (k + 1) * 0.6f), StoneGrey * 1.05f, rot);
            }
            foreach (float x in new[] { -3.6f, -1.2f, 1.2f, 3.6f })
                for (int sz = -1; sz <= 1; sz += 2)
                    Cyl(stat, p + sd * x + f * sz * 2.8f + Vector3.up * y0, 0.2f, 3.2f, Vermilion, rot, 10);
            Box(stat, p + Vector3.up * (y0 + 1.6f) - f * 0.2f, new Vector3(7.2f, 3.2f, 5.2f), white, rot);
            Glow(p + f * 2.42f + Vector3.up * (y0 + 1.35f), new Vector3(6.8f, 2.6f, 0.05f), rot, PaperGlow * 0.85f);
            for (float x = -3.2f; x <= 3.21f; x += 0.45f)
                Box(stat, p + f * 2.47f + sd * x + Vector3.up * (y0 + 1.35f), new Vector3(0.07f, 2.6f, 0.05f), Vermilion * 0.7f, rot, 0.005f);
            for (int k = 0; k < 2; k++)
                Box(stat, p + f * 2.47f + Vector3.up * (y0 + 0.6f + k * 1.5f), new Vector3(6.9f, 0.07f, 0.05f), Vermilion * 0.7f, rot, 0.005f);
            Box(stat, p + f * 3.3f + Vector3.up * (y0 + 0.05f), new Vector3(8f, 0.12f, 1.6f), wood, rot);
            Box(stat, p + f * 2.8f + Vector3.up * (y0 + 3.15f), new Vector3(8.2f, 0.36f, 0.36f), Vermilion, rot);
            // A two-pitch roof (steep above, flatter at the eaves) gives the curved temple silhouette.
            const float W = 9.8f;
            float tanU = Mathf.Tan(34f * Mathf.Deg2Rad), tanL = Mathf.Tan(18f * Mathf.Deg2Rad);
            float eave = y0 + 3.3f, ridgeY = eave + 2.8f * tanU;
            var gable = MeshFactory.MeshObject(Gable(), Par(stat), Vector3.zero, new Vector3(7.2f, 2.8f * tanU, 5.6f), T(white));
            gable.transform.position = p + Vector3.up * eave;
            gable.transform.rotation = rot;
            for (int s = -1; s <= 1; s += 2)
            {
                Box(stat, p + Vector3.up * (ridgeY - 1.4f * tanU) + f * s * 1.4f, new Vector3(W, 0.25f, 2.8f / Mathf.Cos(34f * Mathf.Deg2Rad)), roof, rot * Quaternion.Euler(s * 34f, 0f, 0f));
                Box(stat, p + Vector3.up * (eave - 0.8f * tanL) + f * s * 3.6f, new Vector3(W, 0.25f, 1.6f / Mathf.Cos(18f * Mathf.Deg2Rad)), roof, rot * Quaternion.Euler(s * 18f, 0f, 0f));
                Box(stat, p + Vector3.up * (eave - 1.6f * tanL - 0.05f) + f * s * 4.35f, new Vector3(W + 0.1f, 0.18f, 0.2f), gold, rot * Quaternion.Euler(s * 18f, 0f, 0f), 0.01f);
            }
            Box(stat, p + Vector3.up * (ridgeY + 0.15f), new Vector3(W + 0.3f, 0.4f, 0.5f), roof * 0.7f, rot);
            for (int s = -1; s <= 1; s += 2)
            {
                Box(stat, p + Vector3.up * (ridgeY + 0.45f) + sd * s * (W * 0.5f + 0.1f), new Vector3(0.4f, 0.7f, 0.55f), roof * 0.55f, rot * Quaternion.Euler(0f, 0f, -s * 12f));
                Sph(stat, p + Vector3.up * (ridgeY + 0.85f) + sd * s * (W * 0.5f + 0.18f), Vector3.one * 0.22f, gold);
            }
            // Offering box, bell and rope, and the sacred rope across the front.
            Box(stat, p + f * 3.6f + Vector3.up * (y0 + 0.4f), new Vector3(1.4f, 0.7f, 0.8f), wood * 0.8f, rot);
            Box(stat, p + f * 3.3f + Vector3.up * (y0 + 1.9f), new Vector3(0.1f, 2.4f, 0.1f), new Color(0.9f, 0.3f, 0.25f), rot, 0.005f);
            Sph(stat, p + f * 3.3f + Vector3.up * (y0 + 3.1f), Vector3.one * 0.42f, gold);
            Cyl(stat, p + f * 3.05f + sd * 3.5f + Vector3.up * (y0 + 2.85f), 0.17f, 7f, new Color(0.85f, 0.75f, 0.5f), rot * Quaternion.Euler(0f, 0f, 90f), 10);
            for (int k = 0; k < 4; k++)
                for (int z = 0; z < 3; z++)
                    Box(stat, p + f * 3.1f + sd * (-2.4f + k * 1.6f) + Vector3.up * (y0 + 2.55f - z * 0.2f), new Vector3(0.18f, 0.17f, 0.03f), Color.white, rot * Quaternion.Euler(0f, 0f, z % 2 == 0 ? 15f : -15f), 0.005f);
            EndProp();
            for (int s = -1; s <= 1; s += 2) HangingLantern(p + f * 4.1f + sd * s * 3.6f + Vector3.up * (y0 + 3f), new Color(1f, 0.86f, 0.6f), false);
            PointLight(p + f * 5.5f + Vector3.up * 3f, P.lantern, 11f, 1.1f);
        }
    }
}
