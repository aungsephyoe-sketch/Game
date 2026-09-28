using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// Hand-directed set pieces for the prototype worlds: a gate at the start, something to find along the trail,
    /// a bridge over the river, ruins or a shrine, a demon camp, and a boss arena with the world's landmark:
    ///   Forest  — the Great Tree: an ancient sacred tree with a rope and lanterns above a circular stone shrine.
    ///   Snow    — the Frozen Summit Shrine: a pagoda in front of a frozen waterfall, on an ice floor with crystals.
    ///   Volcano — the Crater Throne: a basalt platform ringed by a lava moat, obsidian spires with chains, a throne.
    /// Every object has a reason to be where it is; nothing sits on the road or in the fighting space.
    /// </summary>
    public static partial class PrototypeWorld
    {
        static int lightBudget;
        static Transform grp;
        static readonly System.Collections.Generic.Stack<Transform> grpStack = new System.Collections.Generic.Stack<Transform>();

        /// <summary>
        /// Starts a set piece: its parts are built under one parent, so the camera fades the whole piece (never a
        /// single beam or post out of the middle of it) when it gets between the camera and the slayer.
        /// </summary>
        static void BeginProp(string name, bool neverHide = false)
        {
            grpStack.Push(grp);
            grp = new GameObject("Prop_" + name).transform;
            grp.SetParent(stat, false);
            var og = grp.gameObject.AddComponent<OcclusionGroup>();
            og.neverHide = neverHide;
        }

        static void EndProp() { grp = grpStack.Count > 0 ? grpStack.Pop() : null; }

        static Transform Par(Transform parent) { return parent == stat && grp != null ? grp : parent; }

        // ------------------------------------------------------------------ Primitive helpers

        static GameObject Box(Transform parent, Vector3 pos, Vector3 scale, Color c, Quaternion rot, float outline = 0.02f)
        {
            var go = MeshFactory.Primitive(PrimitiveType.Cube, Par(parent), Vector3.zero, scale, T(c, outline));
            go.transform.position = pos;
            go.transform.rotation = rot;
            return go;
        }

        static GameObject Cyl(Transform parent, Vector3 pos, float radius, float height, Color c, Quaternion rot, int sides = 12)
        {
            var go = MeshFactory.MeshObject(MeshFactory.FacetCylinder(sides), Par(parent), Vector3.zero, new Vector3(radius * 2f, height, radius * 2f), T(c));
            go.transform.position = pos;
            go.transform.rotation = rot;
            return go;
        }

        static GameObject Sph(Transform parent, Vector3 pos, Vector3 scale, Color c)
        {
            var go = MeshFactory.MeshObject(MeshFactory.SmoothSphere(), Par(parent), Vector3.zero, scale, T(c));
            go.transform.position = pos;
            return go;
        }

        static GameObject ConeAt(Transform parent, Vector3 pos, Vector3 scale, Color c, Quaternion rot, int sides = 0)
        {
            var go = MeshFactory.MeshObject(sides > 0 ? MeshFactory.FacetCone(sides) : MeshFactory.Cone(), Par(parent), Vector3.zero, scale, T(c));
            go.transform.position = pos;
            go.transform.rotation = rot;
            return go;
        }

        static void PointLight(Vector3 pos, Color c, float range, float intensity)
        {
            if (lightBudget <= 0) return;
            lightBudget--;
            var lg = new GameObject("Light");
            lg.transform.SetParent(dyn, false);
            lg.transform.position = pos;
            var l = lg.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = c;
            l.range = range;
            l.intensity = intensity;
            lg.AddComponent<LanternFlicker>();
        }

        static Vector3 OnGround(Vector3 p) { return new Vector3(p.x, Ground(p), p.z); }

        // ------------------------------------------------------------------ Reusable set pieces

        /// <summary>A stone lantern with a glowing firebox (snow ones wear a snow cap).</summary>
        static void StoneLantern(Vector3 p, float s, bool light)
        {
            BeginProp("Lantern");
            StoneLanternParts(p, s, light);
            EndProp();
        }

        static void StoneLanternParts(Vector3 p, float s, bool light)
        {
            p = OnGround(p);
            Color stone = K == Kind.Volcano ? new Color(0.18f, 0.15f, 0.15f) : new Color(0.58f, 0.58f, 0.55f);
            var q = Quaternion.Euler(0f, R(0f, 90f), 0f);
            Cyl(stat, p, 0.32f * s, 0.18f * s, stone, q, 6);
            Cyl(stat, p + Vector3.up * 0.18f * s, 0.12f * s, 0.7f * s, stone, q, 8);
            Box(stat, p + Vector3.up * 0.95f * s, new Vector3(0.5f, 0.1f, 0.5f) * s, stone, q);
            var fire = MeshFactory.Primitive(PrimitiveType.Cube, Par(stat), Vector3.zero, new Vector3(0.34f, 0.32f, 0.34f) * s, TE(P.lantern));
            fire.transform.position = p + Vector3.up * 1.16f * s;
            fire.transform.rotation = q;
            for (int k = 0; k < 4; k++)
                Box(stat, p + Vector3.up * 1.16f * s + q * (Quaternion.Euler(0f, k * 90f, 0f) * new Vector3(0.18f * s, 0f, 0.18f * s)), new Vector3(0.07f, 0.34f, 0.07f) * s, stone, q);
            ConeAt(stat, p + Vector3.up * 1.33f * s, new Vector3(0.95f, 0.32f, 0.95f) * s, stone, q, 4);
            if (K == Kind.Snow) ConeAt(stat, p + Vector3.up * 1.38f * s, new Vector3(0.8f, 0.24f, 0.8f) * s, new Color(0.95f, 0.97f, 1f), q, 4);
            Sph(stat, p + Vector3.up * 1.67f * s, Vector3.one * 0.12f * s, stone);
            if (light) PointLight(p + Vector3.up * 1.2f * s, P.lantern, 7f, 1.2f);
        }

        /// <summary>A torii gate across the road (dir = the way through it).</summary>
        static void Torii(Vector3 c, Vector3 dir, float width, float height, Color col, bool broken = false)
        {
            BeginProp("Torii");
            c = OnGround(c);
            var rot = Quaternion.LookRotation(dir);
            var across = rot * Quaternion.Euler(0f, 90f, 0f);
            Vector3 side = Vector3.Cross(Vector3.up, dir);
            Color black = new Color(0.08f, 0.06f, 0.06f);
            for (int s = -1; s <= 1; s += 2)
            {
                Vector3 b = c + side * s * width * 0.5f;
                float ph = broken && s > 0 ? height * 0.45f : height;
                // One solid pillar from the ground to the top beam, a black footing ring around its base.
                Cyl(stat, b, 0.28f, ph + 0.05f, col, rot, 12);
                Cyl(stat, b, 0.36f, 0.5f, black, rot, 12);
                if (broken && s > 0)
                {
                    // The broken half lies on the ground beside the road.
                    var fallen = Cyl(stat, b + side * 2.2f + dir * 1.5f + Vector3.up * 0.3f, 0.28f, height * 0.5f, col, rot * Quaternion.Euler(0f, 0f, 90f) * Quaternion.Euler(20f, 0f, 0f), 12);
                    fallen.name = "FallenPillar";
                }
            }
            if (!broken)
            {
                // Tie beam, centre post and plaque: separate depths so no two faces ever share a plane.
                Box(stat, c + Vector3.up * height * 0.72f, new Vector3(width + 0.9f, 0.32f, 0.3f), col, across);
                Box(stat, c + Vector3.up * (height * 0.72f + 0.16f + height * 0.07f), new Vector3(0.26f, height * 0.14f, 0.22f), col, rot);
                Box(stat, c + Vector3.up * (height * 0.86f) - dir * 0.14f, new Vector3(0.8f, 0.55f, 0.06f), black, rot);
                // The top beam (kasagi) is one continuous piece; only its tips sweep upward.
                float len = width + 1.6f;
                Box(stat, c + Vector3.up * (height + 0.17f), new Vector3(len, 0.34f, 0.5f), col, across);
                Box(stat, c + Vector3.up * (height + 0.42f), new Vector3(len + 0.3f, 0.16f, 0.62f), black, across);
                for (int s = -1; s <= 1; s += 2)
                {
                    var tip = rot * Quaternion.Euler(0f, 90f, 0f) * Quaternion.Euler(0f, 0f, s * 14f);
                    Box(stat, c + Vector3.up * (height + 0.5f) + side * s * (len * 0.5f + 0.45f), new Vector3(1.1f, 0.2f, 0.66f), black, tip);
                }
            }
            else
            {
                // Only the left half of the beam survives, snapped off and hanging.
                float len = width * 0.55f;
                Box(stat, c + Vector3.up * (height + 0.1f) - side * (width * 0.5f - len * 0.5f + 0.3f), new Vector3(len, 0.34f, 0.5f), col, across * Quaternion.Euler(0f, 0f, -8f));
            }
            EndProp();
        }

        static void Signpost(Vector3 p, Vector3 facing)
        {
            BeginProp("Sign");
            p = OnGround(p);
            Color wood = new Color(0.42f, 0.3f, 0.18f);
            Cyl(stat, p, 0.08f, 2f, wood, Quaternion.identity, 6);
            var rot = Quaternion.LookRotation(Vector3.Cross(Vector3.up, facing));
            Box(stat, p + Vector3.up * 1.75f, new Vector3(1.2f, 0.28f, 0.06f), wood, rot);
            Box(stat, p + Vector3.up * 1.4f, new Vector3(1f, 0.24f, 0.06f), wood * 0.9f, rot * Quaternion.Euler(0f, 0f, 4f));
            if (K == Kind.Snow) Box(stat, p + Vector3.up * 1.92f, new Vector3(1.25f, 0.08f, 0.14f), new Color(0.95f, 0.97f, 1f), rot);
            EndProp();
        }

        static void Crates(Vector3 p, int n)
        {
            for (int i = 0; i < n; i++)
            {
                Vector3 at = OnGround(p + new Vector3(R(-1.2f, 1.2f), 0f, R(-1.2f, 1.2f)));
                Breakable.Create(dyn, at, rng.Next(2) == 0);
            }
        }

        static void Banner(Vector3 p, Color cloth, float h)
        {
            p = OnGround(p);
            Color pole = new Color(0.15f, 0.1f, 0.08f);
            Cyl(stat, p, 0.06f, h, pole, Quaternion.identity, 6);
            var flag = new GameObject("Banner").transform;
            flag.SetParent(dyn, false);
            flag.position = p + Vector3.up * (h - 0.1f) + Vector3.right * 0.05f;
            var f = MeshFactory.Primitive(PrimitiveType.Cube, flag, new Vector3(0.35f, -0.7f, 0f), new Vector3(0.7f, 1.4f, 0.03f), T(cloth));
            f.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            MeshFactory.Primitive(PrimitiveType.Cube, flag, new Vector3(0.35f, -0.9f, 0.02f), new Vector3(0.3f, 0.3f, 0.02f), T(Color.Lerp(cloth, Color.black, 0.6f), 0f));
            var sw = flag.gameObject.AddComponent<Sway>();
            sw.Amount = 7f;
            sw.Speed = 1.3f;
        }

        static void Campfire(Vector3 p)
        {
            p = OnGround(p);
            Color log = new Color(0.3f, 0.2f, 0.12f);
            for (int k = 0; k < 4; k++)
                Cyl(stat, p + Vector3.up * 0.15f, 0.09f, 1.1f, log, Quaternion.Euler(0f, k * 45f, 80f), 6);
            for (int k = 0; k < 8; k++)
            {
                float a = k * 45f * Mathf.Deg2Rad;
                Sph(stat, p + new Vector3(Mathf.Cos(a), 0.05f, Mathf.Sin(a)) * 0.75f, new Vector3(0.35f, 0.22f, 0.35f), P.rockDark);
            }
            EnvFx.Fire(dyn, p + Vector3.up * 0.3f, 0.8f, lightBudget-- > 0);
            EnvFx.Smoke(dyn, p + Vector3.up * 1.2f, 0.6f);
        }

        static void Tent(Vector3 p, Vector3 facing, Color cloth)
        {
            BeginProp("Tent");
            p = OnGround(p);
            var rot = Quaternion.LookRotation(facing);
            for (int s = -1; s <= 1; s += 2)
                Box(stat, p + Vector3.up * 0.85f + rot * new Vector3(s * 0.62f, 0f, 0f), new Vector3(0.06f, 2f, 2.4f), cloth, rot * Quaternion.Euler(0f, 0f, s * 38f));
            Cyl(stat, p + Vector3.up * 1.62f + rot * new Vector3(0f, 0f, -1.3f), 0.05f, 2.6f, new Color(0.3f, 0.2f, 0.12f), rot * Quaternion.Euler(90f, 0f, 0f), 6);
            if (K == Kind.Snow)
                for (int s = -1; s <= 1; s += 2)
                    Box(stat, p + Vector3.up * 1.2f + rot * new Vector3(s * 0.36f, 0f, 0f), new Vector3(0.08f, 0.9f, 2.44f), new Color(0.95f, 0.97f, 1f), rot * Quaternion.Euler(0f, 0f, s * 38f));
            EndProp();
            Obstacles.AddBox(p, new Vector2(1.2f, 1.2f), rot.eulerAngles.y);
        }

        static void Palisade(Vector3 center, float radius, float fromA, float toA)
        {
            Color log = new Color(0.34f, 0.24f, 0.15f);
            for (float a = fromA; a < toA; a += 7f)
            {
                Vector3 p = center + Quaternion.Euler(0f, a, 0f) * Vector3.forward * radius;
                if (PathDist(p.x, p.z) < J.halfWidth + 1f) continue;
                p = OnGround(p);
                float h = R(1.8f, 2.4f);
                Cyl(stat, p, 0.16f, h, log, Quaternion.Euler(R(-4f, 4f), 0f, R(-4f, 4f)), 6);
                ConeAt(stat, p + Vector3.up * h, new Vector3(0.32f, 0.4f, 0.32f), log, Quaternion.identity, 6);
            }
        }

        static void WeaponRack(Vector3 p, Vector3 facing)
        {
            BeginProp("Rack");
            p = OnGround(p);
            var rot = Quaternion.LookRotation(facing);
            Color wood = new Color(0.32f, 0.22f, 0.14f), steel = new Color(0.62f, 0.64f, 0.68f);
            for (int s = -1; s <= 1; s += 2) Cyl(stat, p + rot * new Vector3(s * 0.8f, 0f, 0f), 0.06f, 1.4f, wood, rot, 6);
            Box(stat, p + Vector3.up * 1.2f, new Vector3(1.8f, 0.1f, 0.1f), wood, rot);
            for (int k = 0; k < 4; k++)
                Box(stat, p + rot * new Vector3(-0.55f + k * 0.37f, 0.7f, 0.08f), new Vector3(0.05f, 1.3f, 0.02f), k % 2 == 0 ? steel : wood, rot * Quaternion.Euler(0f, 0f, R(-8f, 8f)));
            EndProp();
        }

        // ------------------------------------------------------------------ Places

        static void Landmarks()
        {
            lightBudget = Mathf.Max(2, GameSettings.MaxDynamicLights);
            if (K == Kind.Village) { VillageLandmarks(); return; }
            for (int i = 0; i < J.places.Count; i++)
            {
                var pl = J.places[i];
                Vector3 c = Journey.Flat(pl.pos);
                Vector3 inDir = i > 0 ? (c - Journey.Flat(J.places[i - 1].pos)).normalized : (Journey.Flat(J.places[Mathf.Min(1, J.places.Count - 1)].pos) - c).normalized;
                if (inDir.sqrMagnitude < 0.01f) inDir = Vector3.forward;
                Vector3 side = Vector3.Cross(Vector3.up, inDir);
                string n = pl.name;
                if (pl.isBossArena) Arena(pl, c, inDir, side);
                else if (i == 0) StartGate(pl, c, inDir, side);
                else if (n.Contains("Camp")) Camp(pl, c, inDir, side);
                else if (n.Contains("Ruins") || n.Contains("Shrine") || n.Contains("Temple")) Ruins(pl, c, inDir, side);
                else if (!pl.isBridge) Trail(pl, c, inDir, side);
                if (i > 0 && !pl.isBossArena) Signpost(c - inDir * (pl.radius + 1.5f) + side * (J.halfWidth + 1.2f), inDir);
            }
            if (hasBridge) BridgeSet();
            RoadGuides();
        }

        static void StartGate(JourneyPlace pl, Vector3 c, Vector3 dir, Vector3 side)
        {
            Vector3 gate = c + dir * (pl.radius + 2f);
            switch (K)
            {
                case Kind.Snow:
                {
                    // A wooden trailhead gate strung with prayer flags, and a stone marker.
                    Color wood = new Color(0.36f, 0.25f, 0.16f);
                    for (int s = -1; s <= 1; s += 2) Cyl(stat, OnGround(gate + side * s * (J.halfWidth + 0.6f)), 0.22f, 4.4f, wood, Quaternion.identity, 8);
                    Box(stat, OnGround(gate) + Vector3.up * 4.2f, new Vector3(J.halfWidth * 2f + 2.2f, 0.3f, 0.32f), wood, Quaternion.LookRotation(side));
                    Box(stat, OnGround(gate) + Vector3.up * 4.42f, new Vector3(J.halfWidth * 2f + 2.4f, 0.14f, 0.5f), new Color(0.95f, 0.97f, 1f), Quaternion.LookRotation(side));
                    FlagLine(OnGround(gate - side * (J.halfWidth + 0.6f)) + Vector3.up * 4f, OnGround(gate + side * (J.halfWidth + 0.6f)) + Vector3.up * 4f, 12);
                    var marker = OnGround(c + side * (pl.radius - 1f) - dir * 1f);
                    Box(stat, marker + Vector3.up * 0.9f, new Vector3(0.7f, 1.8f, 0.4f), P.rock, Quaternion.LookRotation(dir));
                    Box(stat, marker + Vector3.up * 1.85f, new Vector3(0.75f, 0.12f, 0.45f), new Color(0.95f, 0.97f, 1f), Quaternion.LookRotation(dir));
                    StoneLantern(c - side * (pl.radius - 1f), 1f, true);
                    break;
                }
                case Kind.Volcano:
                    Torii(gate, dir, J.halfWidth * 2f + 1.2f, 5.2f, new Color(0.12f, 0.08f, 0.09f), true);
                    Banner(c + side * (pl.radius - 0.5f), P.accent, 3.2f);
                    Banner(c - side * (pl.radius - 0.5f), P.accent, 3.2f);
                    Brazier(c + side * (pl.radius - 1.5f) + dir * 2f);
                    Brazier(c - side * (pl.radius - 1.5f) + dir * 2f);
                    break;
                default:
                    Torii(gate, dir, J.halfWidth * 2f + 1.2f, 5.4f, new Color(0.82f, 0.16f, 0.12f));
                    StoneLantern(gate + side * (J.halfWidth + 1.4f) + dir * 1.2f, 1f, true);
                    StoneLantern(gate - side * (J.halfWidth + 1.4f) + dir * 1.2f, 1f, false);
                    // A small roadside shrine with a stone guardian wearing a red bib.
                    Vector3 js = OnGround(c + side * (pl.radius + 0.5f) - dir * 2f);
                    Box(stat, js + Vector3.up * 0.2f, new Vector3(1f, 0.4f, 0.8f), P.rock, Quaternion.LookRotation(side));
                    Sph(stat, js + Vector3.up * 0.75f, new Vector3(0.5f, 0.7f, 0.5f), new Color(0.62f, 0.62f, 0.6f));
                    Sph(stat, js + Vector3.up * 1.25f, Vector3.one * 0.42f, new Color(0.64f, 0.64f, 0.62f));
                    ConeAt(stat, js + Vector3.up * 0.85f - side * 0.18f, new Vector3(0.5f, 0.35f, 0.5f), P.accent, Quaternion.Euler(180f, 0f, 0f));
                    break;
            }
        }

        static void FlagLine(Vector3 a, Vector3 b, int n)
        {
            var rope = T(new Color(0.55f, 0.48f, 0.35f), 0f);
            var r = MeshFactory.Primitive(PrimitiveType.Cube, stat, Vector3.zero, new Vector3(0.03f, 0.03f, Vector3.Distance(a, b)), rope);
            r.transform.position = (a + b) * 0.5f + Vector3.down * 0.25f;
            r.transform.rotation = Quaternion.LookRotation(b - a);
            Color[] cols = { new Color(0.2f, 0.4f, 0.85f), new Color(0.95f, 0.95f, 0.95f), new Color(0.85f, 0.2f, 0.2f), new Color(0.2f, 0.7f, 0.35f), new Color(0.95f, 0.8f, 0.2f) };
            for (int k = 1; k < n; k++)
            {
                float t = k / (float)n;
                Vector3 p = Vector3.Lerp(a, b, t) + Vector3.down * (0.25f + Mathf.Sin(t * Mathf.PI) * 0.5f);
                var f = new GameObject("Flag").transform;
                f.SetParent(dyn, false);
                f.position = p;
                f.rotation = Quaternion.LookRotation(b - a);
                MeshFactory.Primitive(PrimitiveType.Cube, f, new Vector3(0f, -0.2f, 0f), new Vector3(0.02f, 0.4f, 0.32f), T(cols[k % cols.Length], 0f));
                f.gameObject.AddComponent<Sway>().Amount = 14f;
            }
        }

        static void Brazier(Vector3 p)
        {
            BeginProp("Brazier");
            p = OnGround(p);
            Color iron = new Color(0.12f, 0.1f, 0.1f);
            for (int k = 0; k < 3; k++) Cyl(stat, p, 0.05f, 1.1f, iron, Quaternion.Euler(0f, k * 120f, 12f), 6);
            Cyl(stat, p + Vector3.up * 1f, 0.42f, 0.25f, iron, Quaternion.identity, 10);
            EnvFx.Fire(dyn, p + Vector3.up * 1.25f, 0.5f, lightBudget-- > 0);
            EndProp();
        }

        static void Trail(JourneyPlace pl, Vector3 c, Vector3 dir, Vector3 side)
        {
            Vector3 left = c - side * (pl.radius + 1.5f), right = c + side * (pl.radius + 1.5f);
            switch (K)
            {
                case Kind.Snow:
                {
                    // An old sled, a half-buried fence and a snowman someone left for the travellers.
                    Color wood = new Color(0.4f, 0.28f, 0.18f);
                    Vector3 sled = OnGround(left + dir * 2f);
                    Box(stat, sled + Vector3.up * 0.35f, new Vector3(0.9f, 0.1f, 1.8f), wood, Quaternion.LookRotation(dir));
                    for (int s = -1; s <= 1; s += 2) Box(stat, sled + Vector3.up * 0.12f + side * s * 0.4f, new Vector3(0.06f, 0.1f, 2f), new Color(0.5f, 0.5f, 0.55f), Quaternion.LookRotation(dir));
                    for (int k = 0; k < 7; k++)
                    {
                        Vector3 fp = OnGround(right + dir * (k * 1.6f - 5f));
                        Cyl(stat, fp, 0.08f, 1.1f - (k % 2) * 0.3f, wood, Quaternion.Euler(0f, 0f, R(-6f, 6f)), 6);
                        if (k > 0) Box(stat, fp + Vector3.up * 0.7f - dir * 0.8f, new Vector3(0.06f, 0.12f, 1.6f), wood, Quaternion.LookRotation(dir));
                    }
                    Vector3 sm = OnGround(right - dir * 3f + side * 1.5f);
                    Sph(stat, sm + Vector3.up * 0.5f, Vector3.one * 1.05f, new Color(0.96f, 0.98f, 1f));
                    Sph(stat, sm + Vector3.up * 1.25f, Vector3.one * 0.72f, new Color(0.96f, 0.98f, 1f));
                    Sph(stat, sm + Vector3.up * 1.8f, Vector3.one * 0.5f, new Color(0.96f, 0.98f, 1f));
                    ConeAt(stat, sm + Vector3.up * 1.8f - side * 0.25f, new Vector3(0.08f, 0.25f, 0.08f), new Color(1f, 0.5f, 0.1f), Quaternion.LookRotation(Vector3.up, side) * Quaternion.Euler(-90f, 0f, 0f));
                    Box(stat, sm + Vector3.up * 1.5f, new Vector3(0.75f, 0.12f, 0.75f), P.accent, Quaternion.identity);
                    break;
                }
                case Kind.Volcano:
                {
                    // Cooled lava flows, a broken cart, a sword left behind in the ground.
                    Basalt(OnGround(left + dir * 1f), 1.2f);
                    Basalt(OnGround(right - dir * 3f), 1f);
                    Vector3 cart = OnGround(right + dir * 3f);
                    Color wood = new Color(0.22f, 0.15f, 0.1f);
                    Box(stat, cart + Vector3.up * 0.6f, new Vector3(1.4f, 0.5f, 2.2f), wood, Quaternion.LookRotation(dir) * Quaternion.Euler(0f, 0f, 14f));
                    Cyl(stat, cart + Vector3.up * 0.5f + side * 0.8f, 0.5f, 0.12f, wood, Quaternion.LookRotation(dir) * Quaternion.Euler(0f, 0f, 90f), 10);
                    Vector3 sw = OnGround(left - dir * 2f);
                    Box(stat, sw + Vector3.up * 0.55f, new Vector3(0.05f, 1.1f, 0.12f), new Color(0.6f, 0.6f, 0.62f), Quaternion.Euler(0f, 30f, 12f));
                    Box(stat, sw + Vector3.up * 1.05f, new Vector3(0.3f, 0.05f, 0.08f), new Color(0.5f, 0.4f, 0.2f), Quaternion.Euler(0f, 30f, 12f));
                    solid.Add(lowSphere, OnGround(left + dir * 4f), Quaternion.identity, new Vector3(3f, 0.3f, 2f), new Color(0.1f, 0.08f, 0.08f));
                    break;
                }
                default:
                {
                    // A fallen log, a mushroom ring and a moss-covered stone marker.
                    Vector3 log = OnGround(left + dir * 2f);
                    var lg = Cyl(stat, log + Vector3.up * 0.45f, 0.45f, 5f, P.trunk, Quaternion.LookRotation(dir) * Quaternion.Euler(90f, 0f, 0f) * Quaternion.Euler(0f, 0f, R(-10f, 10f)), 10);
                    lg.transform.position = log + Vector3.up * 0.45f - dir * 2.5f;
                    Sph(stat, log + Vector3.up * 0.85f, new Vector3(1.2f, 0.25f, 2f), Color.Lerp(P.grassA, P.leafDark, 0.3f));
                    for (int k = 0; k < 9; k++)
                    {
                        float a = k * 40f * Mathf.Deg2Rad;
                        Mushroom(OnGround(right + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * 1.3f), R(0.25f, 0.4f));
                    }
                    Vector3 st = OnGround(right + dir * 4f);
                    Box(stat, st + Vector3.up * 0.8f, new Vector3(0.6f, 1.6f, 0.35f), P.rock, Quaternion.LookRotation(-side) * Quaternion.Euler(0f, 0f, 5f));
                    Sph(stat, st + Vector3.up * 1.65f, new Vector3(0.7f, 0.2f, 0.45f), Color.Lerp(P.grassA, P.leafDark, 0.3f));
                    StoneLantern(c + side * (pl.radius + 0.5f) - dir * 3f, 0.9f, false);
                    break;
                }
            }
        }

        static void Ruins(JourneyPlace pl, Vector3 c, Vector3 dir, Vector3 side)
        {
            Reserve(c, pl.radius + 6f);
            switch (K)
            {
                case Kind.Snow:
                {
                    // A small mountain shrine (hokora) with a snowy red roof, offerings and lanterns.
                    Vector3 s = OnGround(c + dir * (pl.radius + 3f) + side * 3f);
                    var rot = Quaternion.LookRotation(-dir);
                    Box(stat, s + Vector3.up * 0.3f, new Vector3(3.4f, 0.6f, 3f), P.rock, rot);
                    Box(stat, s + Vector3.up * 1.5f, new Vector3(2.4f, 1.8f, 2f), new Color(0.5f, 0.34f, 0.2f), rot);
                    Box(stat, s + Vector3.up * 1.4f - dir * 1.01f, new Vector3(1.1f, 1.3f, 0.05f), new Color(0.12f, 0.08f, 0.06f), rot);
                    for (int k = -1; k <= 1; k += 2)
                    {
                        Box(stat, s + Vector3.up * 2.75f + side * k * 0.7f, new Vector3(1.7f, 0.12f, 3f), new Color(0.72f, 0.14f, 0.12f), rot * Quaternion.Euler(0f, 0f, -k * 28f));
                        Box(stat, s + Vector3.up * 2.9f + side * k * 0.66f, new Vector3(1.5f, 0.12f, 2.9f), new Color(0.96f, 0.98f, 1f), rot * Quaternion.Euler(0f, 0f, -k * 28f));
                    }
                    for (int k = -1; k <= 1; k += 2) Cyl(stat, s + side * k * 1.1f - dir * 1f, 0.1f, 2.5f, new Color(0.72f, 0.14f, 0.12f), Quaternion.identity, 8);
                    Box(stat, s + Vector3.up * 0.8f - dir * 1.8f, new Vector3(0.9f, 0.5f, 0.5f), new Color(0.4f, 0.28f, 0.18f), rot);
                    StoneLantern(s - dir * 2.5f + side * 2.2f, 0.9f, true);
                    StoneLantern(s - dir * 2.5f - side * 2.2f, 0.9f, false);
                    FlagLine(OnGround(s + side * 3f) + Vector3.up * 3.2f, OnGround(c - side * (pl.radius + 1f)) + Vector3.up * 3.4f, 10);
                    Cyl(stat, OnGround(c - side * (pl.radius + 1f)), 0.1f, 3.6f, new Color(0.36f, 0.25f, 0.16f), Quaternion.identity, 6);
                    break;
                }
                case Kind.Volcano:
                {
                    // A ruined fire temple: broken dark pillars with red banners, a fallen statue head, braziers.
                    Color stone = new Color(0.2f, 0.17f, 0.17f);
                    for (int k = 0; k < 7; k++)
                    {
                        float a = (k - 3) * 22f;
                        Vector3 p = OnGround(c + Quaternion.Euler(0f, a, 0f) * dir * (pl.radius + 2.2f));
                        float h = k % 3 == 1 ? R(1.5f, 2.5f) : R(4f, 6f);
                        Cyl(stat, p, 0.55f, h, stone, Quaternion.Euler(0f, R(0f, 60f), R(-3f, 3f)), 8);
                        Box(stat, p + Vector3.up * h, new Vector3(1.4f, 0.35f, 1.4f), stone, Quaternion.Euler(0f, R(0f, 45f), 0f));
                        if (h > 3f && k % 2 == 0) Banner(p + (c - p).normalized * 0.7f, P.accent, h - 0.4f);
                    }
                    Vector3 head = OnGround(c + side * (pl.radius + 2.5f) - dir * 2f);
                    Sph(stat, head + Vector3.up * 1f, new Vector3(2.2f, 2.4f, 2f), new Color(0.24f, 0.2f, 0.2f));
                    for (int s = -1; s <= 1; s += 2)
                        ConeAt(stat, head + Vector3.up * 2f + side * s * 0.7f, new Vector3(0.3f, 1f, 0.3f), new Color(0.12f, 0.1f, 0.1f), Quaternion.Euler(0f, 0f, -s * 30f));
                    Brazier(c + side * (pl.radius - 1f) + dir * (pl.radius * 0.5f));
                    Brazier(c - side * (pl.radius - 1f) + dir * (pl.radius * 0.5f));
                    break;
                }
                default:
                {
                    // Moss ruins: a broken colonnade, fallen columns, a mossy wall and a guardian statue.
                    Color stone = new Color(0.62f, 0.62f, 0.58f), moss = Color.Lerp(P.grassA, P.leafDark, 0.3f);
                    for (int k = 0; k < 8; k++)
                    {
                        float a = (k - 3.5f) * 20f;
                        Vector3 p = OnGround(c + Quaternion.Euler(0f, a, 0f) * dir * (pl.radius + 2.5f));
                        float h = k % 3 == 0 ? R(1f, 2f) : R(3.5f, 5f);
                        Cyl(stat, p, 0.5f, h, stone, Quaternion.Euler(R(-3f, 3f), R(0f, 60f), R(-3f, 3f)), 10);
                        Sph(stat, p + Vector3.up * h, new Vector3(1.1f, 0.35f, 1.1f), moss);
                    }
                    for (int k = 0; k < 2; k++)
                    {
                        Vector3 f = OnGround(c + side * (k == 0 ? 1f : -1f) * (pl.radius + 1.5f) + dir * R(-2f, 2f));
                        var col = Cyl(stat, f + Vector3.up * 0.5f, 0.5f, 3.6f, stone, Quaternion.LookRotation(dir) * Quaternion.Euler(90f, 0f, 0f) * Quaternion.Euler(0f, 0f, R(-30f, 30f)), 10);
                        col.transform.position = f + Vector3.up * 0.5f - dir * 1.8f;
                    }
                    Vector3 wall = OnGround(c - side * (pl.radius + 3f));
                    for (int k = 0; k < 4; k++)
                    {
                        float h = 2.4f - Mathf.Abs(k - 1.5f) * 0.6f + R(-0.3f, 0.3f);
                        Box(stat, wall + dir * (k * 1.6f - 2.4f) + Vector3.up * h * 0.5f, new Vector3(0.8f, h, 1.6f), stone, Quaternion.LookRotation(dir));
                        Sph(stat, wall + dir * (k * 1.6f - 2.4f) + Vector3.up * h, new Vector3(1f, 0.3f, 1.8f), moss);
                    }
                    Vector3 st = OnGround(c + side * (pl.radius + 3f) + dir * 2f);
                    Box(stat, st + Vector3.up * 0.4f, new Vector3(1.4f, 0.8f, 1.4f), stone, Quaternion.LookRotation(-side));
                    Sph(stat, st + Vector3.up * 1.6f, new Vector3(1f, 1.4f, 0.9f), stone);
                    Sph(stat, st + Vector3.up * 2.6f, new Vector3(0.9f, 0.85f, 0.85f), stone);
                    for (int s = -1; s <= 1; s += 2) Sph(stat, st + Vector3.up * 2.65f - side * 0.4f + dir * s * 0.18f, Vector3.one * 0.12f, new Color(0.3f, 0.8f, 0.9f));
                    Sph(stat, st + Vector3.up * 3.05f, new Vector3(0.9f, 0.25f, 0.8f), moss);
                    // Sunbeams through the canopy.
                    SunBeams(c, 5);
                    break;
                }
            }
        }

        static void SunBeams(Vector3 c, int n)
        {
            var mat = MaterialFactory.Additive(new Color(1f, 0.95f, 0.7f, 0.07f));
            var sunRot = Quaternion.Euler(P.sunEuler);
            for (int k = 0; k < n; k++)
            {
                Vector3 p = c + new Vector3(R(-6f, 6f), 0f, R(-6f, 6f));
                var beam = MeshFactory.Primitive(PrimitiveType.Cube, dyn, Vector3.zero, new Vector3(R(0.8f, 1.8f), 18f, 0.05f), mat);
                beam.transform.rotation = sunRot * Quaternion.Euler(90f, 0f, 0f);
                beam.transform.position = OnGround(p) - (sunRot * Vector3.forward) * 9f;
                beam.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                var pu = beam.AddComponent<Pulse>();
                pu.Speed = 0.6f + k * 0.1f;
                pu.Amount = 0.08f;
            }
        }

        static void Camp(JourneyPlace pl, Vector3 c, Vector3 dir, Vector3 side)
        {
            Color cloth = K == Kind.Volcano ? new Color(0.35f, 0.08f, 0.08f) : K == Kind.Snow ? new Color(0.35f, 0.3f, 0.45f) : new Color(0.3f, 0.12f, 0.2f);
            Palisade(c, pl.radius + 2.2f, 0f, 360f);
            Tent(c + side * (pl.radius - 1f) + dir * 2f, -side, cloth);
            Tent(c - side * (pl.radius - 1f) - dir * 1f, side, cloth);
            Campfire(c + dir * (pl.radius * 0.35f) + side * (pl.radius * 0.45f));
            Crates(c - side * (pl.radius - 2.5f) + dir * (pl.radius - 3f), 3);
            Crates(c + side * (pl.radius - 2.5f) - dir * (pl.radius - 4f), 2);
            WeaponRack(c + dir * (pl.radius - 1f) - side * 3f, -dir);
            Banner(c + side * (pl.radius + 0.8f) + dir * (pl.radius - 2f), cloth, 3.6f);
            Banner(c - side * (pl.radius + 0.8f) + dir * (pl.radius - 2f), cloth, 3.6f);
            if (K == Kind.Volcano)
            {
                // The demons' forge: an anvil and a glowing furnace.
                Vector3 f = OnGround(c - side * (pl.radius - 2f) + dir * 2f);
                Box(stat, f + Vector3.up * 0.4f, new Vector3(0.9f, 0.8f, 0.5f), new Color(0.15f, 0.14f, 0.15f), Quaternion.LookRotation(side));
                Box(stat, f + Vector3.up * 0.9f, new Vector3(1.2f, 0.25f, 0.45f), new Color(0.2f, 0.2f, 0.22f), Quaternion.LookRotation(side));
                Vector3 fu = OnGround(c - side * (pl.radius + 1.5f) - dir * 3f);
                Cyl(stat, fu, 1.2f, 2.4f, new Color(0.18f, 0.14f, 0.13f), Quaternion.identity, 10);
                var mouth = MeshFactory.Primitive(PrimitiveType.Cube, stat, Vector3.zero, new Vector3(1f, 0.8f, 0.2f), TE(new Color(1f, 0.45f, 0.1f)));
                mouth.transform.position = fu + Vector3.up * 0.7f + side * 1.15f;
                mouth.transform.rotation = Quaternion.LookRotation(side);
                EnvFx.Smoke(dyn, fu + Vector3.up * 2.6f, 0.8f);
            }
            if (K == Kind.Snow)
            {
                // The camp sits at the mouth of an ice cave.
                Vector3 cave = OnGround(c + dir * (pl.radius + 5f));
                for (int k = 0; k < 9; k++)
                {
                    float a = Mathf.Lerp(-80f, 80f, k / 8f);
                    Vector3 p = cave + Quaternion.LookRotation(dir) * (Quaternion.Euler(0f, 0f, a) * new Vector3(0f, 3.2f, 0f));
                    Sph(stat, p, new Vector3(2.2f, 2.2f, 3f), k % 2 == 0 ? new Color(0.72f, 0.88f, 1f) : new Color(0.6f, 0.78f, 0.95f));
                }
                Sph(stat, cave + Vector3.up * 1.4f + dir * 0.6f, new Vector3(3f, 2.6f, 0.5f), new Color(0.05f, 0.08f, 0.14f));
                for (int k = 0; k < 7; k++)
                    ConeAt(stat, cave + Vector3.up * 3.6f + Quaternion.LookRotation(dir) * new Vector3(R(-2.2f, 2.2f), 0f, -1.2f), new Vector3(0.18f, R(0.6f, 1.2f), 0.18f), new Color(0.8f, 0.94f, 1f), Quaternion.Euler(180f, 0f, 0f), 5);
            }
        }

        // ------------------------------------------------------------------ Bridge

        static void BridgeSet()
        {
            BeginProp("Bridge", true);
            Vector3 c = bridgeCenter;
            Vector3 dir = bridgeDir;
            Vector3 side = Vector3.Cross(Vector3.up, dir);
            var rot = Quaternion.LookRotation(dir);
            Reserve(c, 8f);
            float span = (channels.Count > 0 ? channels[0].half : 3f) * 2f + 5f;
            float w = J.halfWidth * 1.3f;
            // Invisible banks beside the deck: the only way over the water is the bridge.
            float chHalf = channels.Count > 0 ? channels[0].half + 0.6f : 3f;
            for (int s = -1; s <= 1; s += 2)
                Obstacles.AddBox(c + side * s * (w * 0.5f + 4.1f), new Vector2(4f, chHalf), rot.eulerAngles.y);
            switch (K)
            {
                case Kind.Volcano:
                {
                    // A heavy stone bridge with low walls.
                    Color stone = new Color(0.24f, 0.2f, 0.19f);
                    Box(stat, c + Vector3.down * 0.25f, new Vector3(w, 0.6f, span), stone, rot);
                    for (int s = -1; s <= 1; s += 2)
                    {
                        Box(stat, c + side * s * (w * 0.5f + 0.2f) + Vector3.up * 0.35f, new Vector3(0.4f, 0.8f, span), stone, rot);
                        for (int e = -1; e <= 1; e += 2) Brazier(c + side * s * (w * 0.5f + 0.6f) + dir * e * (span * 0.5f + 0.5f));
                    }
                    Box(stat, c + Vector3.down * 1.2f, new Vector3(w * 0.6f, 1.6f, 1.6f), stone, rot);
                    break;
                }
                case Kind.Snow:
                {
                    // A rope bridge over the frozen chasm, its planks dusted with snow.
                    Color wood = new Color(0.42f, 0.3f, 0.18f), rope = new Color(0.6f, 0.52f, 0.38f);
                    int n = Mathf.RoundToInt(span / 0.55f);
                    for (int k = 0; k <= n; k++)
                    {
                        float t = k / (float)n;
                        float sag = Mathf.Sin(t * Mathf.PI) * 0.1f;
                        Vector3 p = c + dir * (t - 0.5f) * span + Vector3.down * (sag + 0.05f);
                        Box(stat, p, new Vector3(w, 0.1f, 0.42f), Jitter(wood, 0.08f), rot * Quaternion.Euler(0f, 0f, R(-2f, 2f)));
                        if (k % 2 == 0) Box(stat, p + Vector3.up * 0.07f, new Vector3(w * R(0.4f, 0.9f), 0.04f, 0.4f), new Color(0.95f, 0.97f, 1f), rot);
                    }
                    for (int s = -1; s <= 1; s += 2)
                    {
                        for (int e = -1; e <= 1; e += 2) Cyl(stat, OnGround(c + side * s * (w * 0.5f + 0.2f) + dir * e * (span * 0.5f + 0.6f)), 0.16f, 1.6f, wood, Quaternion.identity, 8);
                        for (int k = 0; k < 8; k++)
                        {
                            float t0 = k / 8f, t1 = (k + 1) / 8f;
                            Vector3 a = c + side * s * (w * 0.5f + 0.2f) + dir * (t0 - 0.5f) * span + Vector3.up * (1.2f - Mathf.Sin(t0 * Mathf.PI) * 0.45f);
                            Vector3 b = c + side * s * (w * 0.5f + 0.2f) + dir * (t1 - 0.5f) * span + Vector3.up * (1.2f - Mathf.Sin(t1 * Mathf.PI) * 0.45f);
                            var r = Box(stat, (a + b) * 0.5f, new Vector3(0.05f, 0.05f, Vector3.Distance(a, b) + 0.02f), rope, Quaternion.LookRotation(b - a), 0f);
                            r.name = "Rope";
                        }
                    }
                    break;
                }
                default:
                {
                    // An arched wooden bridge with red railings over the stream.
                    Color wood = new Color(0.46f, 0.32f, 0.2f), rail = new Color(0.8f, 0.18f, 0.14f);
                    int n = Mathf.RoundToInt(span / 0.5f);
                    for (int k = 0; k <= n; k++)
                    {
                        float t = k / (float)n;
                        float arch = Mathf.Sin(t * Mathf.PI) * 0.12f;
                        Vector3 p = c + dir * (t - 0.5f) * span + Vector3.up * (arch - 0.07f);
                        float slope = Mathf.Cos(t * Mathf.PI) * 0.12f * Mathf.PI / span;
                        Box(stat, p, new Vector3(w, 0.12f, 0.46f), Jitter(wood, 0.06f), rot * Quaternion.Euler(-Mathf.Atan(slope) * Mathf.Rad2Deg, 0f, 0f));
                    }
                    for (int s = -1; s <= 1; s += 2)
                    {
                        for (int k = 0; k <= 6; k++)
                        {
                            float t = k / 6f;
                            Vector3 p = c + side * s * (w * 0.5f) + dir * (t - 0.5f) * span + Vector3.up * (Mathf.Sin(t * Mathf.PI) * 0.12f);
                            Cyl(stat, p, 0.08f, 1.1f, rail, Quaternion.identity, 8);
                            if (k == 0 || k == 6) Sph(stat, p + Vector3.up * 1.15f, Vector3.one * 0.2f, new Color(0.9f, 0.75f, 0.3f));
                        }
                        for (int k = 0; k < 6; k++)
                        {
                            float t0 = k / 6f, t1 = (k + 1) / 6f;
                            Vector3 a = c + side * s * (w * 0.5f) + dir * (t0 - 0.5f) * span + Vector3.up * (Mathf.Sin(t0 * Mathf.PI) * 0.12f + 1f);
                            Vector3 b = c + side * s * (w * 0.5f) + dir * (t1 - 0.5f) * span + Vector3.up * (Mathf.Sin(t1 * Mathf.PI) * 0.12f + 1f);
                            Box(stat, (a + b) * 0.5f, new Vector3(0.1f, 0.1f, Vector3.Distance(a, b) + 0.05f), rail, Quaternion.LookRotation(b - a));
                        }
                    }
                    // Stepping stones and reeds along the stream.
                    for (int k = 0; k < 16; k++)
                    {
                        Vector3 p = c + side * R(-18f, 18f) + dir * R(-3f, 3f);
                        if (Mathf.Abs(Vector3.Dot(p - c, side)) < w) continue;
                        if (rng.Next(2) == 0) Rock(new Vector3(p.x, -0.6f, p.z), R(0.4f, 0.9f), P.rock, true);
                        else Tuft(new Vector3(p.x, Ground(p), p.z), R(0.6f, 0.9f), P.leafMid);
                    }
                    break;
                }
            }
            EndProp();
        }

        /// <summary>Lanterns, flag poles or braziers along the road so the way forward is always readable.</summary>
        static void RoadGuides()
        {
            int n = 0;
            for (float d = 8f; d < J.Length - 8f; d += 13f, n++)
            {
                Vector3 p = Journey.Flat(J.PointAt(d));
                Vector3 ahead = Journey.Flat(J.PointAt(d + 1f)) - p;
                ahead.Normalize();
                Vector3 side = Vector3.Cross(Vector3.up, ahead) * (J.halfWidth + 1f) * (n % 2 == 0 ? 1f : -1f);
                Vector3 at = p + side;
                bool skip = NearChannel(at.x, at.z, 2f);
                foreach (var pl in J.places) if ((Journey.Flat(at) - Journey.Flat(pl.pos)).magnitude < pl.radius + 3f) skip = true;
                if (skip) continue;
                switch (K)
                {
                    case Kind.Snow:
                        Cyl(stat, OnGround(at), 0.09f, 2.8f, new Color(0.36f, 0.25f, 0.16f), Quaternion.identity, 6);
                        FlagLine(OnGround(at) + Vector3.up * 2.7f, OnGround(at + ahead * 3f + side.normalized * 1.5f) + Vector3.up * 1.2f, 5);
                        break;
                    case Kind.Volcano:
                        if (n % 2 == 0) Brazier(at);
                        else Shard(OnGround(at), 1.2f);
                        break;
                    default:
                        StoneLantern(at, 0.85f, n % 3 == 0);
                        break;
                }
            }
        }

        // ------------------------------------------------------------------ Boss arenas and landmarks

        static void Arena(JourneyPlace pl, Vector3 c, Vector3 dir, Vector3 side)
        {
            float r = pl.radius;
            // The arena turns hostile when the boss enrages (falling rocks and ice, or eruptions of fire).
            var hz = new GameObject("ArenaHazard").AddComponent<ArenaHazard>();
            hz.transform.SetParent(dyn, false);
            hz.Kind = K == Kind.Volcano ? "fire" : "rocks";
            hz.Center = pl.pos;
            hz.Radius = r - 2f;
            Vector3 back = c + dir * (r + 12f);
            switch (K)
            {
                case Kind.Snow:
                {
                    // Ice floor rings, crystal pillars, a pagoda shrine and the frozen waterfall behind it.
                    var ice = MaterialFactory.Transparent(new Color(0.75f, 0.92f, 1f, 0.35f));
                    for (int k = 0; k < 3; k++)
                    {
                        var ring = MeshFactory.MeshObject(MeshFactory.Ring(0.9f - k * 0.02f), dyn, OnGround(c) + Vector3.up * (0.08f + k * 0.005f), Vector3.one * (r - 2f - k * 4f), ice, false);
                        ring.name = "IceRing";
                    }
                    for (int k = 0; k < 10; k++)
                    {
                        float a = k * 36f;
                        Vector3 p = c + Quaternion.Euler(0f, a, 0f) * Vector3.forward * (r + 1.5f);
                        if (PathDist(p.x, p.z) < J.halfWidth + 1.5f) continue;
                        Crystal(OnGround(p), R(1.6f, 2.6f), new Color(0.6f, 0.86f, 1f));
                    }
                    Pagoda(OnGround(back), -dir);
                    FrozenFall(c + dir * (r + 26f), -dir);
                    StoneLantern(c + side * (r - 1f) + dir * (r * 0.5f), 1.1f, true);
                    StoneLantern(c - side * (r - 1f) + dir * (r * 0.5f), 1.1f, true);
                    break;
                }
                case Kind.Volcano:
                {
                    // Basalt platform ring, obsidian spires with chains, and the throne.
                    Color stone = new Color(0.16f, 0.13f, 0.13f);
                    for (int k = 0; k < 28; k++)
                    {
                        float a = k * 360f / 28f;
                        Vector3 p = c + Quaternion.Euler(0f, a, 0f) * Vector3.forward * (r + 0.6f);
                        if (PathDist(p.x, p.z) < J.halfWidth + 0.5f) continue;
                        Box(stat, OnGround(p) + Vector3.up * 0.15f, new Vector3(2.2f, 0.4f, 1.2f), stone, Quaternion.Euler(0f, a, 0f));
                    }
                    var rune = MeshFactory.MeshObject(MeshFactory.Ring(0.94f), dyn, OnGround(c) + Vector3.up * 0.09f, Vector3.one * (r - 3f), MaterialFactory.Additive(new Color(1f, 0.35f, 0.08f, 0.5f)), false);
                    rune.AddComponent<Pulse>().Speed = 1.5f;
                    var spires = new Vector3[6];
                    for (int k = 0; k < 6; k++)
                    {
                        float a = k * 60f + 30f;
                        spires[k] = c + Quaternion.Euler(0f, a, 0f) * Vector3.forward * (r + 9f);
                        Vector3 p = OnGround(spires[k]);
                        ConeAt(stat, p + Vector3.down * 0.5f, new Vector3(2.2f, R(9f, 13f), 2.2f), new Color(0.06f, 0.04f, 0.07f), Quaternion.Euler(0f, R(0f, 90f), 0f), 5);
                        var seam = MeshFactory.Primitive(PrimitiveType.Cube, stat, Vector3.zero, new Vector3(0.12f, 6f, 0.12f), TE(new Color(1f, 0.35f, 0.08f)));
                        seam.transform.position = p + Vector3.up * 3f;
                        seam.transform.rotation = Quaternion.Euler(0f, a, 6f);
                    }
                    for (int k = 0; k < 6; k++)
                    {
                        Vector3 a = OnGround(spires[k]) + Vector3.up * 5f, b = OnGround(spires[(k + 1) % 6]) + Vector3.up * 5f;
                        for (int l = 0; l < 14; l++)
                        {
                            float t = (l + 0.5f) / 14f;
                            Vector3 p = Vector3.Lerp(a, b, t) + Vector3.down * Mathf.Sin(t * Mathf.PI) * 2.2f;
                            var link = MeshFactory.MeshObject(MeshFactory.Ring(0.6f), stat, Vector3.zero, Vector3.one * 0.35f, T(new Color(0.3f, 0.28f, 0.3f), 0f), false);
                            link.transform.position = p;
                            link.transform.rotation = Quaternion.LookRotation(b - a) * Quaternion.Euler(0f, 0f, l % 2 == 0 ? 0f : 90f) * Quaternion.Euler(90f, 0f, 0f);
                        }
                    }
                    Vector3 th = OnGround(back);
                    var trot = Quaternion.LookRotation(-dir);
                    Box(stat, th + Vector3.up * 0.6f, new Vector3(6f, 1.2f, 4f), stone, trot);
                    Box(stat, th + Vector3.up * 1.8f, new Vector3(3f, 1.2f, 2.2f), new Color(0.1f, 0.08f, 0.09f), trot);
                    Box(stat, th + Vector3.up * 4f + dir * 0.9f, new Vector3(3f, 4.4f, 0.6f), new Color(0.1f, 0.08f, 0.09f), trot);
                    for (int s = -1; s <= 1; s += 2)
                    {
                        ConeAt(stat, th + Vector3.up * 6f + side * s * 1.3f + dir * 0.9f, new Vector3(0.5f, 2.2f, 0.5f), new Color(0.06f, 0.04f, 0.07f), Quaternion.Euler(0f, 0f, -s * 15f), 5);
                        Brazier(th - dir * 3f + side * s * 4f);
                    }
                    Sph(stat, th + Vector3.up * 5.4f + dir * 0.55f, Vector3.one * 0.8f, new Color(0.9f, 0.2f, 0.1f));
                    EnvFx.Smoke(dyn, c + dir * (r + 6f) + side * 8f, 1.5f);
                    EnvFx.Smoke(dyn, c + dir * (r + 4f) - side * 10f, 1.2f);
                    break;
                }
                default:
                {
                    // A circular stone shrine under the Great Tree.
                    Color stone = new Color(0.66f, 0.65f, 0.6f);
                    for (int k = 0; k < 36; k++)
                    {
                        float a = k * 10f;
                        Vector3 p = c + Quaternion.Euler(0f, a, 0f) * Vector3.forward * (r - 0.8f);
                        if (PathDist(p.x, p.z) < J.halfWidth - 1f && Vector3.Dot(p - c, dir) < 0f) continue;
                        Box(stat, OnGround(p) + Vector3.up * 0.05f, new Vector3(1.6f, 0.2f, 0.9f), Jitter(stone, 0.05f), Quaternion.Euler(0f, a, 0f));
                    }
                    for (int k = 0; k < 3; k++)
                    {
                        var ring = MeshFactory.MeshObject(MeshFactory.Ring(0.93f), stat, OnGround(c) + Vector3.up * (0.09f + k * 0.004f), Vector3.one * (r - 4f - k * 3.5f), T(Color.Lerp(stone, P.clearing, 0.3f + k * 0.2f), 0f), false);
                        ring.name = "ShrineRing";
                    }
                    for (int k = 0; k < 8; k++)
                    {
                        float a = k * 45f + 22.5f;
                        Vector3 p = c + Quaternion.Euler(0f, a, 0f) * Vector3.forward * (r + 1.2f);
                        if (PathDist(p.x, p.z) < J.halfWidth + 1.5f) continue;
                        StoneLantern(p, 1.1f, k % 2 == 0);
                    }
                    Torii(c - dir * (r - 0.5f), dir, J.halfWidth * 2f + 1f, 5.8f, new Color(0.82f, 0.16f, 0.12f));
                    GreatTree(c + dir * (r + 10f));
                    var spirit = EnvFx.Weather(dyn, c, "motes", r * 2f);
                    if (spirit != null) spirit.transform.position = c + Vector3.up * 1.5f;
                    SunBeams(c, 6);
                    break;
                }
            }
        }

        /// <summary>The forest's landmark: an ancient sacred tree with a rope of paper streamers and hanging lanterns.</summary>
        static void GreatTree(Vector3 p)
        {
            p = OnGround(p);
            Reserve(p, 16f);
            Obstacles.AddCircle(p, 4.2f);
            var b = new WorldMeshBuilder(treeRoot, folMat, "GreatTree", true);
            Color bark = new Color(0.36f, 0.26f, 0.18f);
            b.Add(trunkMesh, p + Vector3.down * 0.5f, Quaternion.identity, new Vector3(3.6f, 20f, 3.6f), bark);
            for (int k = 0; k < 7; k++)
            {
                float a = k * 51f;
                var q = Quaternion.Euler(0f, a, 0f) * Quaternion.Euler(0f, 0f, 70f);
                b.Add(trunkMesh, p + Vector3.up * 1.2f, q, new Vector3(0.9f, 5f, 0.9f), bark * 0.9f);
            }
            for (int k = 0; k < 5; k++)
            {
                var q = Quaternion.Euler(0f, k * 72f + 20f, 0f) * Quaternion.Euler(0f, 0f, 55f);
                b.Add(trunkMesh, p + Vector3.up * 11f, q, new Vector3(0.9f, 9f, 0.9f), bark);
            }
            Vector3 crown = p + Vector3.up * 18f;
            b.Add(lowSphere, crown, Quaternion.identity, new Vector3(24f, 14f, 24f), P.leafMid, 0.25f, 0.5f, 0.4f);
            for (int k = 0; k < 7; k++)
            {
                float a = k * 51f * Mathf.Deg2Rad;
                Vector3 o = new Vector3(Mathf.Cos(a) * 10f, R(-3f, 2f), Mathf.Sin(a) * 10f);
                b.Add(lowSphere, crown + o, Quaternion.identity, Vector3.one * R(10f, 14f), Jitter(k % 2 == 0 ? P.leafDark : P.leafMid, 0.05f), 0.25f, 0.5f, 0.4f);
            }
            b.Add(lowSphere, crown + Vector3.up * 6f, Quaternion.identity, new Vector3(16f, 9f, 16f), P.leafLight, 0.3f, 0.5f, 0.5f);
            b.Flush();
            // Shimenawa rope and zigzag paper streamers.
            var rope = MeshFactory.Lathe("pw_rope", new[] { new Vector2(1.9f, -0.25f), new Vector2(2.05f, 0f), new Vector2(1.9f, 0.25f) }, 24);
            var ropeGo = MeshFactory.MeshObject(rope, stat, Vector3.zero, Vector3.one, T(new Color(0.85f, 0.75f, 0.5f)));
            ropeGo.transform.position = p + Vector3.up * 4.2f;
            for (int k = 0; k < 8; k++)
            {
                float a = k * 45f * Mathf.Deg2Rad;
                Vector3 at = p + Vector3.up * 3.8f + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * 2.1f;
                for (int z = 0; z < 3; z++)
                    Box(stat, at + Vector3.down * (z * 0.25f), new Vector3(0.22f, 0.2f, 0.03f), Color.white, Quaternion.LookRotation(new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a))) * Quaternion.Euler(0f, 0f, z % 2 == 0 ? 15f : -15f), 0.005f);
            }
            // Lanterns hanging from the lowest branches.
            for (int k = 0; k < 6; k++)
            {
                float a = (k * 60f + 15f) * Mathf.Deg2Rad;
                Vector3 hang = p + new Vector3(Mathf.Cos(a) * 7f, 9f, Mathf.Sin(a) * 7f);
                Box(stat, hang + Vector3.up * 1f, new Vector3(0.03f, 2f, 0.03f), new Color(0.2f, 0.15f, 0.1f), Quaternion.identity, 0f);
                var lant = MeshFactory.MeshObject(MeshFactory.SmoothSphere(), dyn, hang, new Vector3(0.6f, 0.8f, 0.6f), TE(new Color(1f, 0.55f, 0.25f)), false);
                lant.AddComponent<Sway>().Amount = 4f;
            }
            PointLight(p + Vector3.up * 8f + Vector3.back * 4f, new Color(1f, 0.7f, 0.4f), 18f, 1.2f);
        }

        /// <summary>A three-tier pagoda with red pillars and snow-laden roofs.</summary>
        static void Pagoda(Vector3 p, Vector3 facing)
        {
            BeginProp("Pagoda");
            Reserve(p, 8f);
            var rot = Quaternion.LookRotation(facing);
            Color red = new Color(0.72f, 0.14f, 0.12f), wall = new Color(0.92f, 0.88f, 0.8f), roof = new Color(0.2f, 0.22f, 0.3f), snow = new Color(0.96f, 0.98f, 1f);
            Box(stat, p + Vector3.up * 0.4f, new Vector3(8f, 0.8f, 8f), P.rock, rot);
            float y = 0.8f;
            for (int k = 0; k < 3; k++)
            {
                float w = 5.4f - k * 1.2f, h = 2.4f - k * 0.3f;
                Box(stat, p + Vector3.up * (y + h * 0.5f), new Vector3(w, h, w), wall, rot);
                for (int s = 0; s < 4; s++)
                {
                    Vector3 corner = rot * (Quaternion.Euler(0f, s * 90f + 45f, 0f) * Vector3.forward) * (w * 0.7f);
                    Cyl(stat, p + corner + Vector3.up * y, 0.14f, h, red, rot, 8);
                }
                Box(stat, p + Vector3.up * (y + h * 0.4f) + facing * (w * 0.5f + 0.01f), new Vector3(w * 0.4f, h * 0.6f, 0.05f), new Color(0.3f, 0.12f, 0.08f), rot);
                y += h;
                ConeAt(stat, p + Vector3.up * y, new Vector3(w * 1.9f, 1.4f, w * 1.9f), roof, rot * Quaternion.Euler(0f, 45f, 0f), 4);
                ConeAt(stat, p + Vector3.up * (y + 0.18f), new Vector3(w * 1.7f, 1.25f, w * 1.7f), snow, rot * Quaternion.Euler(0f, 45f, 0f), 4);
                y += 0.9f;
            }
            Cyl(stat, p + Vector3.up * y, 0.08f, 3f, new Color(0.85f, 0.7f, 0.3f), rot, 8);
            for (int k = 0; k < 4; k++) Cyl(stat, p + Vector3.up * (y + 0.5f + k * 0.55f), 0.3f - k * 0.05f, 0.08f, new Color(0.85f, 0.7f, 0.3f), rot, 10);
            PointLight(p + Vector3.up * 2f + facing * 4f, P.lantern, 10f, 1.1f);
            EndProp();
        }

        /// <summary>A cliff face with a frozen waterfall pouring into a frozen pool.</summary>
        static void FrozenFall(Vector3 p, Vector3 facing)
        {
            p = OnGround(p);
            Reserve(p, 14f);
            var rot = Quaternion.LookRotation(facing);
            Vector3 side = Vector3.Cross(Vector3.up, facing);
            Obstacles.AddBox(p, new Vector2(18f, 4f), Quaternion.LookRotation(facing).eulerAngles.y);
            for (int k = 0; k < 9; k++)
            {
                Vector3 o = side * ((k - 4) * 4f) + Vector3.up * R(-2f, 2f) - facing * R(0f, 3f);
                solid.Add(MeshFactory.Rock(k), p + o, Quaternion.Euler(0f, R(0f, 360f), 0f), new Vector3(7f, R(18f, 26f), 6f), Jitter(P.rock, 0.08f));
                solid.Add(lowSphere, p + o + Vector3.up * 20f, Quaternion.identity, new Vector3(6f, 2.5f, 5f), new Color(0.95f, 0.97f, 1f));
            }
            var iceA = T(new Color(0.72f, 0.9f, 1f), 0.01f);
            var iceB = T(new Color(0.88f, 0.97f, 1f), 0.01f);
            for (int k = 0; k < 22; k++)
            {
                float x = R(-4f, 4f);
                float h = R(10f, 19f);
                var col = MeshFactory.MeshObject(MeshFactory.SmoothCapsule(), stat, Vector3.zero, new Vector3(R(0.8f, 1.6f), h * 0.5f, R(0.6f, 1.2f)), k % 2 == 0 ? iceA : iceB);
                col.transform.position = p + side * x + facing * (2.5f + R(0f, 1f)) + Vector3.up * (h * 0.5f + R(0f, 2f));
                col.transform.rotation = rot * Quaternion.Euler(R(-6f, 6f), 0f, R(-4f, 4f));
            }
            var pool = MeshFactory.MeshObject(MeshFactory.PlanarDisc(), stat, p + facing * 6f + Vector3.up * 0.06f, new Vector3(8f, 1f, 5f), T(new Color(0.7f, 0.88f, 1f), 0f), false);
            pool.name = "FrozenPool";
            var sheen = MeshFactory.MeshObject(MeshFactory.SmoothSphere(), dyn, p + facing * 3f + Vector3.up * 10f, new Vector3(9f, 20f, 3f), MaterialFactory.Additive(new Color(0.6f, 0.85f, 1f, 0.08f)), false);
            sheen.AddComponent<Pulse>().Speed = 0.8f;
        }
    }
}
