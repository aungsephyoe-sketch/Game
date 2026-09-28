using System.Collections.Generic;
using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// Environment quality test: three hand-directed worlds (Forest, Snow Mountain, Volcano) built around a
    /// mission route, on the same premium standard as the new characters.
    ///   • A sculpted terrain: the playable road and clearings stay flat while banks, terraces, ridges and hills
    ///     rise around them, painted per vertex (grass patches, dirt road, rock on slopes, snow, ash, lava heat).
    ///   • Three layers: foreground detail at the road edge (grass, flowers, rocks, lanterns), the midground
    ///     set pieces at each named place (bridge, ruins, shrine, camp) and a background of hills, forests and
    ///     distant mountain ranges fading into the sky.
    ///   • A landmark and a boss arena designed for each world, water or lava that flows, wind in the foliage,
    ///     weather, drifting fog, birds and clouds.
    ///   • Lighting per world: warm forest sun, cold blue snow light, red volcanic glow; a gradient sky dome.
    /// Scenery is combined into a few large vertex-coloured meshes so it stays cheap to render on mobile.
    /// </summary>
    public static partial class PrototypeWorld
    {
        public enum Kind { Forest, Snow, Volcano, Village }

        public static Kind Parse(string s)
        {
            if (s == "snow") return Kind.Snow;
            if (s == "volcano") return Kind.Volcano;
            if (s == "village") return Kind.Village;
            return Kind.Forest;
        }

        /// <summary>Green worlds (the forest and Kiriha Village share their terrain, plants and hills).</summary>
        static bool Leafy { get { return K == Kind.Forest || K == Kind.Village; } }

        class Palette
        {
            public Color grassA, grassB, grassC, path, pathEdge, clearing, rock, rockDark, bank;
            public Color leafDark, leafMid, leafLight, trunk, accent, flowerA, flowerB, flowerC;
            public Color water, waterFoam, skyTop, skyHorizon, sunGlow, fog, amSky, amEquator, amGround, sun, shadow, rim, far;
            public float sunIntensity, fogStart, fogEnd;
            public Vector3 sunEuler;
            public Color lantern;
        }

        class Channel
        {
            public List<Vector3> pts = new List<Vector3>();
            public float half, depth, surface;
            public bool lava, ice, causeway;
        }

        static Kind K;
        static Palette P;
        static Journey J;
        static System.Random rng;
        static Transform root, stat, dyn;
        static readonly List<Channel> channels = new List<Channel>();
        static readonly Dictionary<string, Material> matCache = new Dictionary<string, Material>();
        static Vector3 boss;
        static float bossR;
        static Vector3[] segA, segB;

        static float R(float a, float b) { return a + (float)rng.NextDouble() * (b - a); }
        static float SS(float a, float b, float x) { return Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(a, b, x)); }

        /// <summary>Shared cel material per colour, so the set pieces static-batch into a few draw calls.</summary>
        static Material T(Color c, float outline = 0.02f)
        {
            // Quantised so slightly varied props still share materials (and static-batch together).
            c = new Color(Mathf.Round(c.r * 24f) / 24f, Mathf.Round(c.g * 24f) / 24f, Mathf.Round(c.b * 24f) / 24f, 1f);
            string key = ColorUtility.ToHtmlStringRGB(c) + outline.ToString("F3");
            Material m;
            if (matCache.TryGetValue(key, out m) && m != null) return m;
            m = MaterialFactory.Toon(c, outline);
            if (m.HasProperty("_ShadowColor")) m.SetColor("_ShadowColor", P.shadow);
            if (m.HasProperty("_RimColor")) m.SetColor("_RimColor", P.rim);
            matCache[key] = m;
            return m;
        }

        static Material TE(Color c)
        {
            string key = "E" + ColorUtility.ToHtmlStringRGB(c);
            Material m;
            if (matCache.TryGetValue(key, out m) && m != null) return m;
            m = MaterialFactory.Toon(c, 0f, c);
            matCache[key] = m;
            return m;
        }

        public static JourneyBuilder.Result Build(Journey j, MissionDefinition m, Transform parent)
        {
            var kind = Parse(BattleController.WorldEnv(m));
            int idSeed = 0;
            if (!string.IsNullOrEmpty(m.id)) foreach (char ch in m.id) idSeed = idSeed * 31 + ch;
            // Prototype missions keep their tuned layout; every other mission gets its own variation.
            return BuildCore(kind, j, 1234 + (int)kind * 77 + (string.IsNullOrEmpty(m.prototypeEnv) ? idSeed : 0), parent, true);
        }

        /// <summary>
        /// Kiriha Village by night on the same standard as the mission worlds: the home screen backdrop
        /// (battleCamera = false) and the open-world hub.
        /// </summary>
        public static JourneyBuilder.Result BuildVillage(Journey j, Transform parent, bool battleCamera)
        {
            return BuildCore(Kind.Village, j, 20260928, parent, battleCamera);
        }

        /// <summary>
        /// A story-scene set in the new worlds: Kiriha for village scenes, otherwise a short route with a wide
        /// clearing at the origin (where the scene's actors stand) in the matching world (forest, snow, volcano).
        /// </summary>
        public static JourneyBuilder.Result BuildStage(ArenaTheme theme, Transform parent)
        {
            if (theme != null && theme.kind == EnvironmentKind.Village) return BuildVillage(Journey.Village(), parent, false);
            Kind k = Kind.Forest;
            if (theme != null)
                switch (theme.kind)
                {
                    case EnvironmentKind.Mountain: case EnvironmentKind.Temple: k = Kind.Snow; break;
                    case EnvironmentKind.DemonLand: case EnvironmentKind.Castle: case EnvironmentKind.FallenCity: k = Kind.Volcano; break;
                }
            return BuildCore(k, Journey.Stage(), 4242 + (int)k, parent, false);
        }

        /// <summary>Re-applies the village's night lighting (the home screen, after a battle changed it).</summary>
        public static void ApplyVillageLighting()
        {
            ApplyPalette(MakePalette(Kind.Village), Kind.Village);
        }

        static JourneyBuilder.Result BuildCore(Kind kind, Journey j, int seed, Transform parent, bool battleCamera)
        {
            K = kind;
            J = j;
            rng = new System.Random(seed);
            P = MakePalette(K);
            hasBridge = false;
            houses.Clear();
            channels.Clear();
            matCache.Clear();
            var res = new JourneyBuilder.Result();
            root = new GameObject("PrototypeWorld").transform;
            root.SetParent(parent, false);
            HashiraChronicles.Ground.Clear();
            res.root = root.gameObject;
            stat = new GameObject("Static").transform;
            stat.SetParent(root, false);
            dyn = new GameObject("Dynamic").transform;
            dyn.SetParent(root, false);
            var bp = j.places[j.places.Count - 1];
            boss = bp.pos;
            bossR = bp.radius;
            segA = new Vector3[Mathf.Max(1, j.path.Count - 1)];
            segB = new Vector3[segA.Length];
            for (int i = 0; i < j.path.Count - 1; i++) { segA[i] = Journey.Flat(j.path[i]); segB[i] = Journey.Flat(j.path[i + 1]); }
            if (j.path.Count < 2) { segA[0] = Journey.Flat(j.path[0]); segB[0] = segA[0] + Vector3.forward; }

            PlanChannels();
            Terrain();
            Water();
            Sky();
            Background();
            Nature();
            Landmarks();
            FlushNature();
            // Everything solid that was built as a separate set piece blocks movement too.
            Obstacles.Scan(stat);
            Atmosphere(res);
            Lighting(battleCamera);

            StaticBatchingUtility.Combine(stat.gameObject);
            return res;
        }

        // ------------------------------------------------------------------ Palettes

        static Palette MakePalette(Kind k)
        {
            var p = new Palette();
            switch (k)
            {
                case Kind.Snow:
                    // Soft blue-grey snow, not paper white: glowing attacks have to stand out against it.
                    p.grassA = new Color(0.72f, 0.78f, 0.88f); p.grassB = new Color(0.66f, 0.73f, 0.85f); p.grassC = new Color(0.79f, 0.84f, 0.92f);
                    p.path = new Color(0.58f, 0.63f, 0.75f); p.pathEdge = new Color(0.78f, 0.84f, 0.93f); p.clearing = new Color(0.72f, 0.78f, 0.88f);
                    p.rock = new Color(0.42f, 0.47f, 0.58f); p.rockDark = new Color(0.26f, 0.3f, 0.4f); p.bank = new Color(0.75f, 0.83f, 0.95f);
                    p.leafDark = new Color(0.08f, 0.26f, 0.28f); p.leafMid = new Color(0.12f, 0.36f, 0.36f); p.leafLight = new Color(0.2f, 0.46f, 0.44f);
                    p.trunk = new Color(0.32f, 0.22f, 0.18f); p.accent = new Color(0.82f, 0.16f, 0.16f);
                    p.flowerA = new Color(0.72f, 0.62f, 0.45f); p.flowerB = new Color(0.62f, 0.9f, 1f); p.flowerC = new Color(0.85f, 0.5f, 0.6f);
                    p.water = new Color(0.62f, 0.86f, 0.97f); p.waterFoam = new Color(1f, 1f, 1f, 0.55f);
                    p.skyTop = new Color(0.36f, 0.56f, 0.86f); p.skyHorizon = new Color(0.82f, 0.9f, 0.98f); p.sunGlow = new Color(1f, 0.97f, 0.9f);
                    p.fog = new Color(0.8f, 0.87f, 0.96f); p.fogStart = 30f; p.fogEnd = 280f;
                    p.amSky = new Color(0.62f, 0.72f, 0.95f); p.amEquator = new Color(0.7f, 0.78f, 0.92f); p.amGround = new Color(0.62f, 0.68f, 0.82f);
                    p.sun = new Color(0.92f, 0.94f, 1f); p.sunIntensity = 0.82f; p.sunEuler = new Vector3(58f, 150f, 0f); // high sun: short shadows, no long streaks
                    p.shadow = new Color(0.55f, 0.62f, 0.85f); p.rim = new Color(0.85f, 0.95f, 1f); p.far = new Color(0.62f, 0.72f, 0.88f);
                    p.lantern = new Color(1f, 0.72f, 0.4f);
                    break;
                case Kind.Volcano:
                    // Cartoon volcano: deep purples and reds instead of muddy grey-brown.
                    p.grassA = new Color(0.26f, 0.14f, 0.24f); p.grassB = new Color(0.36f, 0.18f, 0.26f); p.grassC = new Color(0.48f, 0.26f, 0.3f);
                    p.path = new Color(0.36f, 0.31f, 0.28f); p.pathEdge = new Color(0.27f, 0.22f, 0.2f); p.clearing = new Color(0.3f, 0.26f, 0.24f);
                    p.rock = new Color(0.34f, 0.2f, 0.32f); p.rockDark = new Color(0.16f, 0.08f, 0.16f); p.bank = new Color(0.2f, 0.08f, 0.14f);
                    p.leafDark = new Color(0.12f, 0.09f, 0.08f); p.leafMid = new Color(0.2f, 0.15f, 0.12f); p.leafLight = new Color(0.3f, 0.22f, 0.16f);
                    p.trunk = new Color(0.1f, 0.08f, 0.08f); p.accent = new Color(0.75f, 0.14f, 0.1f);
                    p.flowerA = new Color(1f, 0.45f, 0.1f); p.flowerB = new Color(1f, 0.75f, 0.2f); p.flowerC = new Color(0.8f, 0.2f, 0.1f);
                    p.water = new Color(1f, 0.42f, 0.06f); p.waterFoam = new Color(0.15f, 0.05f, 0.03f, 0.85f);
                    p.skyTop = new Color(0.12f, 0.05f, 0.1f); p.skyHorizon = new Color(0.62f, 0.2f, 0.12f); p.sunGlow = new Color(1f, 0.45f, 0.2f);
                    p.fog = new Color(0.42f, 0.16f, 0.12f); p.fogStart = 24f; p.fogEnd = 230f;
                    p.amSky = new Color(0.36f, 0.14f, 0.16f); p.amEquator = new Color(0.46f, 0.2f, 0.14f); p.amGround = new Color(0.55f, 0.22f, 0.08f);
                    p.sun = new Color(1f, 0.62f, 0.42f); p.sunIntensity = 0.95f; p.sunEuler = new Vector3(38f, 20f, 0f);
                    p.shadow = new Color(0.5f, 0.32f, 0.42f); p.rim = new Color(1f, 0.55f, 0.3f); p.far = new Color(0.3f, 0.1f, 0.1f);
                    p.lantern = new Color(1f, 0.5f, 0.15f);
                    break;
                case Kind.Village:
                    // Kiriha at dusk: vibrant but deep — rich green grass, warm rose-stone streets, pink blossom,
                    // a blue-violet sky glowing magenta-orange at the horizon, a low warm sun, lanterns doing the rest.
                    p.grassA = new Color(0.16f, 0.52f, 0.3f); p.grassB = new Color(0.22f, 0.6f, 0.3f); p.grassC = new Color(0.36f, 0.7f, 0.28f);
                    p.path = new Color(0.36f, 0.24f, 0.26f); p.pathEdge = new Color(0.32f, 0.26f, 0.24f); p.clearing = new Color(0.38f, 0.26f, 0.28f);
                    p.rock = new Color(0.46f, 0.44f, 0.6f); p.rockDark = new Color(0.26f, 0.24f, 0.4f); p.bank = new Color(0.14f, 0.4f, 0.26f);
                    p.leafDark = new Color(0.8f, 0.28f, 0.55f); p.leafMid = new Color(0.98f, 0.5f, 0.72f); p.leafLight = new Color(1f, 0.74f, 0.86f);
                    p.trunk = new Color(0.34f, 0.18f, 0.18f); p.accent = new Color(0.95f, 0.2f, 0.18f);
                    p.flowerA = new Color(1f, 0.82f, 0.2f); p.flowerB = new Color(1f, 0.42f, 0.62f); p.flowerC = new Color(0.5f, 0.55f, 1f);
                    p.water = new Color(0.14f, 0.42f, 0.85f); p.waterFoam = new Color(0.85f, 0.92f, 1f, 0.5f);
                    p.skyTop = new Color(0.07f, 0.09f, 0.32f); p.skyHorizon = new Color(0.95f, 0.38f, 0.45f); p.sunGlow = new Color(1f, 0.62f, 0.3f);
                    p.fog = new Color(0.36f, 0.24f, 0.46f); p.fogStart = 32f; p.fogEnd = 220f;
                    p.amSky = new Color(0.46f, 0.44f, 0.82f); p.amEquator = new Color(0.72f, 0.46f, 0.62f); p.amGround = new Color(0.3f, 0.24f, 0.32f);
                    // Low warm sun from over the viewer's left shoulder (faces lit), long deep shadows.
                    p.sun = new Color(1f, 0.72f, 0.5f); p.sunIntensity = 0.95f; p.sunEuler = new Vector3(24f, 34f, 0f);
                    p.shadow = new Color(0.34f, 0.26f, 0.62f); p.rim = new Color(1f, 0.7f, 0.45f); p.far = new Color(0.3f, 0.22f, 0.5f);
                    p.lantern = new Color(1f, 0.62f, 0.24f);
                    break;
                default:
                    p.grassA = new Color(0.32f, 0.72f, 0.24f); p.grassB = new Color(0.44f, 0.82f, 0.28f); p.grassC = new Color(0.62f, 0.88f, 0.3f);
                    p.path = new Color(0.66f, 0.52f, 0.34f); p.pathEdge = new Color(0.52f, 0.46f, 0.28f); p.clearing = new Color(0.6f, 0.52f, 0.36f);
                    p.rock = new Color(0.5f, 0.5f, 0.46f); p.rockDark = new Color(0.34f, 0.35f, 0.33f); p.bank = new Color(0.3f, 0.46f, 0.22f);
                    p.leafDark = new Color(0.12f, 0.46f, 0.2f); p.leafMid = new Color(0.22f, 0.66f, 0.24f); p.leafLight = new Color(0.5f, 0.84f, 0.26f);
                    p.trunk = new Color(0.4f, 0.28f, 0.18f); p.accent = new Color(0.84f, 0.18f, 0.14f);
                    p.flowerA = new Color(1f, 0.85f, 0.3f); p.flowerB = new Color(0.95f, 0.5f, 0.6f); p.flowerC = new Color(0.6f, 0.7f, 1f);
                    p.water = new Color(0.25f, 0.6f, 0.78f); p.waterFoam = new Color(1f, 1f, 1f, 0.5f);
                    p.skyTop = new Color(0.32f, 0.58f, 0.9f); p.skyHorizon = new Color(0.8f, 0.9f, 0.9f); p.sunGlow = new Color(1f, 0.95f, 0.78f);
                    p.fog = new Color(0.72f, 0.84f, 0.82f); p.fogStart = 34f; p.fogEnd = 290f;
                    p.amSky = new Color(0.62f, 0.76f, 0.92f); p.amEquator = new Color(0.56f, 0.64f, 0.46f); p.amGround = new Color(0.34f, 0.3f, 0.2f);
                    p.sun = new Color(1f, 0.93f, 0.78f); p.sunIntensity = 1.12f; p.sunEuler = new Vector3(46f, -38f, 0f);
                    p.shadow = new Color(0.46f, 0.52f, 0.66f); p.rim = new Color(1f, 0.95f, 0.75f); p.far = new Color(0.42f, 0.58f, 0.62f);
                    p.lantern = new Color(1f, 0.7f, 0.35f);
                    break;
            }
            return p;
        }

        // ------------------------------------------------------------------ Terrain

        static float SegDist(Vector3 p, Vector3 a, Vector3 b)
        {
            Vector3 ab = b - a;
            float t = Mathf.Clamp01(Vector3.Dot(p - a, ab) / Mathf.Max(0.0001f, ab.sqrMagnitude));
            return (a + ab * t - p).magnitude;
        }

        static float PathDist(float x, float z)
        {
            var p = new Vector3(x, 0f, z);
            float best = float.MaxValue;
            for (int i = 0; i < segA.Length; i++)
            {
                float d = SegDist(p, segA[i], segB[i]);
                if (d < best) best = d;
            }
            return best;
        }

        static float PlaceDist(float x, float z, out JourneyPlace nearest)
        {
            float best = float.MaxValue;
            nearest = null;
            foreach (var pl in J.places)
            {
                float d = new Vector2(x - pl.pos.x, z - pl.pos.z).magnitude - pl.radius;
                if (d < best) { best = d; nearest = pl; }
            }
            return best;
        }

        /// <summary>Distance from the edge of the playable area (negative inside it).</summary>
        static float Gap(float x, float z)
        {
            JourneyPlace pl;
            return Mathf.Min(PathDist(x, z) - (J.halfWidth + 1.2f), PlaceDist(x, z, out pl) - 1.5f);
        }

        static float ChannelDist(Channel c, float x, float z)
        {
            var p = new Vector3(x, 0f, z);
            float best = float.MaxValue;
            for (int i = 0; i < c.pts.Count - 1; i++)
            {
                float d = SegDist(p, c.pts[i], c.pts[i + 1]);
                if (d < best) best = d;
            }
            return best;
        }

        /// <summary>Terrain height: flat where you play, rising into banks, hills, terraces or ridges around it.</summary>
        public static float Height(float x, float z)
        {
            float d = Gap(x, z);
            float h = 0f;
            if (d > 0f)
            {
                float n1 = WorldKit.Noise(x * 0.035f, z * 0.035f), n2 = WorldKit.Noise(x * 0.11f, z * 0.11f);
                switch (K)
                {
                    case Kind.Snow:
                        h = SS(0f, 4f, d) * 1.3f + SS(4f, 22f, d) * (3.5f + 8f * n1) + SS(12f, 70f, d) * 14f * n1;
                        float step = 2.6f, q = h / step, fr = q - Mathf.Floor(q);
                        float terr = Mathf.Floor(q) * step + step * SS(0.55f, 0.95f, fr);
                        h = Mathf.Lerp(h, terr, 0.75f * SS(3f, 10f, d)) + n2 * 0.35f;
                        break;
                    case Kind.Volcano:
                        h = SS(0f, 5f, d) * 1.1f + SS(5f, 24f, d) * (2f + 8f * WorldKit.Ridged(x * 0.03f, z * 0.03f)) + SS(12f, 70f, d) * 11f * WorldKit.Ridged(x * 0.014f, z * 0.014f) + n2 * 0.45f;
                        break;
                    case Kind.Village:
                        // Level ground for the houses beside the street, then gentle hills beyond the village.
                        h = SS(7f, 30f, d) * (1.6f + 5f * n1) + SS(20f, 70f, d) * 10f * n1 * n1 + n2 * 0.25f * SS(4f, 10f, d);
                        break;
                    default:
                        h = SS(0f, 5f, d) * 0.9f + SS(3f, 26f, d) * (2.2f + 5.5f * n1) + SS(10f, 70f, d) * 9f * n1 * n1 + n2 * 0.5f * SS(0f, 4f, d);
                        break;
                }
            }
            foreach (var c in channels)
            {
                float cd = ChannelDist(c, x, z);
                if (cd > c.half + 3f) continue;
                if (c.causeway && PathDist(x, z) < J.halfWidth + 2f) continue;
                float bottom = -c.depth;
                h = Mathf.Min(h, Mathf.Lerp(bottom, h, SS(c.half * 0.6f, c.half + 3f, cd)));
            }
            return h;
        }

        static float Ground(Vector3 p) { return Height(p.x, p.z); }

        static void PlanChannels()
        {
            // A stream (or frozen river / lava river) crossing the road at the bridge, meandering off both ways.
            foreach (var pl in J.places)
            {
                if (!pl.isBridge) continue;
                float at = J.Progress(pl.pos) - pl.radius - 7f;
                if (at < 4f) continue;
                Vector3 c = Journey.Flat(J.PointAt(at));
                Vector3 dir = Journey.Flat(J.PointAt(at + 1f)) - c;
                dir.Normalize();
                Vector3 side = Vector3.Cross(Vector3.up, dir);
                var ch = new Channel { half = K == Kind.Snow ? 3.2f : 2.6f, depth = K == Kind.Snow ? 3.2f : 1.6f, lava = K == Kind.Volcano, ice = K == Kind.Snow };
                ch.surface = K == Kind.Snow ? -2.2f : -0.55f;
                for (int k = -30; k <= 30; k++)
                {
                    float s = k * 4f;
                    float wob = Mathf.Sin(s * 0.05f) * Mathf.Clamp01((Mathf.Abs(s) - 10f) / 30f) * 9f;
                    ch.pts.Add(c + side * s + dir * wob);
                }
                channels.Add(ch);
                bridgeCenter = c;
                bridgeDir = dir;
                hasBridge = true;
                break;
            }
            if (K == Kind.Volcano)
            {
                // A lava moat around the crater arena (the road crosses on a natural rock causeway).
                var moat = new Channel { half = 2.2f, depth = 1.8f, lava = true, surface = -0.6f, causeway = true };
                for (int k = 0; k <= 40; k++)
                {
                    float a = k * Mathf.PI * 2f / 40f;
                    moat.pts.Add(Journey.Flat(boss) + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * (bossR + 5.5f));
                }
                channels.Add(moat);
                // And a lava river running beside the road through the middle of the route.
                var side = new Channel { half = 2.4f, depth = 1.6f, lava = true, surface = -0.55f };
                float L = J.Length;
                for (float s = L * 0.12f; s < L * 0.7f; s += 4f)
                {
                    Vector3 p = Journey.Flat(J.PointAt(s));
                    Vector3 d = Journey.Flat(J.PointAt(s + 1f)) - p;
                    d.Normalize();
                    side.pts.Add(p + Vector3.Cross(Vector3.up, d) * (J.halfWidth + 13f + Mathf.Sin(s * 0.04f) * 3f));
                }
                if (side.pts.Count > 2) channels.Add(side);
            }
        }

        static Vector3 bridgeCenter, bridgeDir;
        static bool hasBridge;

        static void Terrain()
        {
            Vector3 mn = J.places[0].pos, mx = mn;
            foreach (var p in J.path) { mn = Vector3.Min(mn, p); mx = Vector3.Max(mx, p); }
            foreach (var pl in J.places) { mn = Vector3.Min(mn, pl.pos - Vector3.one * pl.radius); mx = Vector3.Max(mx, pl.pos + Vector3.one * pl.radius); }
            float margin = 75f;
            mn -= new Vector3(margin, 0f, margin);
            mx += new Vector3(margin, 0f, margin + 20f);
            float step = 1.6f;
            int nx = Mathf.CeilToInt((mx.x - mn.x) / step) + 1, nz = Mathf.CeilToInt((mx.z - mn.z) / step) + 1;
            var hts = new float[nx * nz];
            for (int iz = 0; iz < nz; iz++)
                for (int ix = 0; ix < nx; ix++)
                {
                    float x = mn.x + ix * step, z = mn.z + iz * step;
                    hts[iz * nx + ix] = Height(x, z);
                }
            // Characters stand on exactly this surface (see Ground / GroundFollower).
            HashiraChronicles.Ground.SetTerrain(root.position + new Vector3(mn.x, 0f, mn.z), step, nx, nz, hts, root.position.y);
            var verts = new List<Vector3>(nx * nz);
            var cols = new List<Color>(nx * nz);
            var tris = new List<int>((nx - 1) * (nz - 1) * 6);
            for (int iz = 0; iz < nz; iz++)
                for (int ix = 0; ix < nx; ix++)
                {
                    float x = mn.x + ix * step, z = mn.z + iz * step;
                    float h = hts[iz * nx + ix];
                    verts.Add(new Vector3(x, h, z));
                    float hl = hts[iz * nx + Mathf.Max(0, ix - 1)], hr = hts[iz * nx + Mathf.Min(nx - 1, ix + 1)];
                    float hd = hts[Mathf.Max(0, iz - 1) * nx + ix], hu = hts[Mathf.Min(nz - 1, iz + 1) * nx + ix];
                    Vector3 nrm = new Vector3(hl - hr, 2f * step, hd - hu).normalized;
                    cols.Add(GroundColor(x, z, h, nrm));
                }
            for (int iz = 0; iz < nz - 1; iz++)
                for (int ix = 0; ix < nx - 1; ix++)
                {
                    int a = iz * nx + ix, b = a + 1, c = a + nx, d = c + 1;
                    tris.Add(a); tris.Add(c); tris.Add(b);
                    tris.Add(b); tris.Add(c); tris.Add(d);
                }
            var mat = MaterialFactory.World(Color.white, 0f, K == Kind.Volcano ? new Color(1f, 0.38f, 0.06f) : (Color?)null, P.shadow, P.rim);
            var b2 = new WorldMeshBuilder(stat.parent, mat, "Terrain", false);
            b2.AddRaw(verts, cols, tris);
            b2.Flush(true);
            foreach (var go in b2.Built) go.GetComponent<Renderer>().receiveShadows = true;
        }

        static Color GroundColor(float x, float z, float h, Vector3 nrm)
        {
            float n1 = WorldKit.Noise(x * 0.06f, z * 0.06f), n2 = WorldKit.Noise(x * 0.23f, z * 0.23f), n3 = WorldKit.Noise(x * 0.9f, z * 0.9f);
            Color g = Color.Lerp(P.grassA, P.grassB, SS(0.35f, 0.65f, n1));
            g = Color.Lerp(g, P.grassC, SS(0.62f, 0.78f, n2) * 0.7f);
            g *= 0.94f + n3 * 0.1f;
            float pd = PathDist(x, z) + (n2 - 0.5f) * 1.4f;
            JourneyPlace pl;
            float cd = PlaceDist(x, z, out pl) + (n2 - 0.5f) * 1.6f;
            float pw = 1f - SS(J.halfWidth - 2f, J.halfWidth + 0.4f, pd);
            float cw = 1f - SS(-2f, 0.8f, cd);
            Color road = Color.Lerp(P.pathEdge, P.path, SS(J.halfWidth - 1f, J.halfWidth - 3.5f, pd)) * (0.95f + n3 * 0.08f);
            Color c = Color.Lerp(g, road, pw * 0.92f);
            c = Color.Lerp(c, Color.Lerp(P.clearing, road, 0.4f) * (0.95f + n3 * 0.08f), cw * 0.85f * (1f - pw * 0.5f));
            // Slopes turn to rock; banks darken.
            float slope = SS(0.86f, 0.6f, nrm.y);
            c = Color.Lerp(c, Color.Lerp(P.rock, P.rockDark, n2), slope);
            if (K == Kind.Snow) c = Color.Lerp(c, P.grassC, SS(6f, 14f, h) * (1f - slope) * 0.6f);
            if (Leafy) c = Color.Lerp(c, P.bank, SS(0.5f, 3f, h) * 0.25f * (1f - slope));
            if (K == Kind.Village)
            {
                // Paving: faint flagstone joints on the streets and the plaza.
                float paved = Mathf.Max(pw, cw);
                float joint = SS(0.8f, 0.95f, Mathf.Abs(Mathf.Sin(x * 1.9f + Mathf.Sin(z * 0.7f))) ) * SS(0.8f, 0.95f, Mathf.Abs(Mathf.Sin(z * 1.9f)));
                c = Color.Lerp(c, c * 0.82f, paved * (0.35f + joint * 0.4f) * n3);
            }
            float glow = 0f;
            foreach (var ch in channels)
            {
                float d = ChannelDist(ch, x, z);
                if (d > ch.half + 4f) continue;
                float wet = 1f - SS(ch.half * 0.5f, ch.half + 3f, d);
                c = Color.Lerp(c, ch.lava ? new Color(0.14f, 0.07f, 0.05f) : ch.ice ? P.rock : Color.Lerp(P.bank, P.rockDark, 0.5f), wet * 0.8f);
                if (ch.lava) glow = Mathf.Max(glow, (1f - SS(ch.half * 0.8f, ch.half + 4f, d)) * 0.75f);
            }
            if (K == Kind.Volcano && pw < 0.5f && cw < 0.5f)
            {
                // Glowing cracks through the basalt.
                float cr = WorldKit.Ridged(x * 0.16f, z * 0.16f);
                glow = Mathf.Max(glow, SS(0.86f, 0.97f, cr) * 0.7f);
            }
            c.a = glow;
            return c;
        }

        // ------------------------------------------------------------------ Water / ice / lava

        static void Water()
        {
            foreach (var ch in channels)
            {
                var verts = new List<Vector3>();
                var uvs = new List<Vector2>();
                var tris = new List<int>();
                float along = 0f;
                float w = ch.half + 1.2f;
                for (int i = 0; i < ch.pts.Count; i++)
                {
                    Vector3 d = i < ch.pts.Count - 1 ? ch.pts[i + 1] - ch.pts[i] : ch.pts[i] - ch.pts[i - 1];
                    d.y = 0f;
                    Vector3 side = Vector3.Cross(Vector3.up, d.normalized) * w;
                    if (i > 0) along += Vector3.Distance(ch.pts[i], ch.pts[i - 1]);
                    verts.Add(ch.pts[i] - side + Vector3.up * ch.surface);
                    verts.Add(ch.pts[i] + side + Vector3.up * ch.surface);
                    uvs.Add(new Vector2(0f, along / (w * 2f)));
                    uvs.Add(new Vector2(1f, along / (w * 2f)));
                    if (i > 0)
                    {
                        int b = verts.Count - 4;
                        tris.AddRange(new[] { b, b + 2, b + 1, b + 1, b + 2, b + 3 });
                    }
                }
                Material baseMat;
                if (ch.lava) baseMat = MaterialFactory.Toon(P.water, 0f, P.water * 0.95f);
                else if (ch.ice) baseMat = MaterialFactory.Toon(P.water, 0f, new Color(0.25f, 0.35f, 0.45f));
                else baseMat = MaterialFactory.Toon(P.water, 0f, new Color(0.05f, 0.12f, 0.16f));
                Ribbon("Water", verts, uvs, tris, baseMat, null);
                // A second, scrolling layer: foam streaks on water, cooled crust on lava, frost on ice.
                var top = new List<Vector3>();
                foreach (var v in verts) top.Add(v + Vector3.up * 0.03f);
                var flow = MaterialFactory.Transparent(ch.lava ? Color.white : P.waterFoam, false);
                flow.mainTexture = ch.lava ? WorldKit.Crust : WorldKit.Streaks;
                flow.mainTextureScale = new Vector2(1f, ch.lava ? 0.6f : 1f);
                var scroll = Ribbon("Flow", top, uvs, tris, flow, null);
                if (!ch.ice) scroll.AddComponent<UvScroll>().Speed = new Vector2(0f, ch.lava ? 0.05f : 0.35f);
                if (ch.lava && GameSettings.MaxDynamicLights > 2)
                {
                    // A couple of warm lights over the lava so it lights its banks.
                    for (int i = 0; i < ch.pts.Count; i += Mathf.Max(1, ch.pts.Count / 2))
                    {
                        var lg = new GameObject("LavaLight");
                        lg.transform.SetParent(dyn, false);
                        lg.transform.position = ch.pts[i] + Vector3.up * 1.5f;
                        var l = lg.AddComponent<Light>();
                        l.type = LightType.Point;
                        l.color = new Color(1f, 0.45f, 0.15f);
                        l.range = 12f;
                        l.intensity = 1.4f;
                        lg.AddComponent<LanternFlicker>();
                    }
                }
                if (ch.ice)
                {
                    // Cracks in the ice.
                    var crack = T(new Color(0.9f, 0.97f, 1f), 0f);
                    for (int k = 0; k < 30; k++)
                    {
                        int i = rng.Next(ch.pts.Count - 1);
                        Vector3 p = Vector3.Lerp(ch.pts[i], ch.pts[i + 1], R(0f, 1f));
                        var c = MeshFactory.Primitive(PrimitiveType.Cube, stat, p + new Vector3(R(-2f, 2f), ch.surface + 0.02f, R(-2f, 2f)), new Vector3(0.05f, 0.01f, R(0.8f, 2.2f)), crack);
                        c.transform.rotation = Quaternion.Euler(0f, R(0f, 180f), 0f);
                        c.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    }
                }
            }
        }

        static GameObject Ribbon(string name, List<Vector3> verts, List<Vector2> uvs, List<int> tris, Material mat, Transform parent)
        {
            var mesh = new Mesh { name = name };
            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            var go = new GameObject(name);
            go.transform.SetParent(parent != null ? parent : dyn, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return go;
        }

        // ------------------------------------------------------------------ Sky and background

        static void Sky()
        {
            // Gradient dome: horizon colour at the bottom, deep sky at the top, a warm glow toward the sun.
            const int lat = 14, lon = 32;
            var verts = new List<Vector3>();
            var cols = new List<Color>();
            var tris = new List<int>();
            Vector3 sunDir = Quaternion.Euler(P.sunEuler) * Vector3.back;
            sunDir.y = Mathf.Abs(sunDir.y) * 0.4f + 0.1f;
            sunDir.Normalize();
            // At night the glow belongs to the moon, which hangs ahead over the village (where the camera looks).
            if (K == Kind.Village) sunDir = new Vector3(-0.25f, 0.12f, 0.96f).normalized;
            for (int a = 0; a <= lat; a++)
            {
                float el = Mathf.Lerp(-0.25f, 1f, a / (float)lat) * Mathf.PI * 0.5f;
                for (int b = 0; b <= lon; b++)
                {
                    float az = b * Mathf.PI * 2f / lon;
                    Vector3 d = new Vector3(Mathf.Cos(el) * Mathf.Cos(az), Mathf.Sin(el), Mathf.Cos(el) * Mathf.Sin(az));
                    verts.Add(d);
                    float up = Mathf.Clamp01(d.y);
                    Color c = Color.Lerp(P.skyHorizon, P.skyTop, Mathf.Pow(up, 0.6f));
                    if (d.y < 0f) c = P.fog;
                    float sun = Mathf.Pow(Mathf.Clamp01(Vector3.Dot(d, sunDir)), 6f);
                    c = Color.Lerp(c, P.sunGlow, sun * 0.6f);
                    cols.Add(c);
                    if (a < lat && b < lon)
                    {
                        int i0 = a * (lon + 1) + b, i1 = i0 + 1, i2 = i0 + lon + 1, i3 = i2 + 1;
                        tris.Add(i0); tris.Add(i1); tris.Add(i2);
                        tris.Add(i1); tris.Add(i3); tris.Add(i2);
                    }
                }
            }
            var mesh = new Mesh { name = "SkyDome" };
            mesh.SetVertices(verts);
            mesh.SetColors(cols);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 2000f);
            var go = new GameObject("SkyDome");
            go.transform.SetParent(dyn, false);
            go.transform.localScale = Vector3.one * 380f;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = MaterialFactory.Sky();
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            go.AddComponent<SkyFollow>();
            // Sun or moon disc.
            var disc = MeshFactory.MeshObject(MeshFactory.SmoothSphere(), go.transform, sunDir * 0.92f, Vector3.one * (K == Kind.Volcano ? 0.09f : 0.06f),
                MaterialFactory.Additive(new Color(P.sunGlow.r, P.sunGlow.g, P.sunGlow.b, 0.9f)), false);
            disc.name = "Sun";
            if (K == Kind.Village)
            {
                // A big cartoon sunset disc low over the village (reads like a poster), plus the first stars.
                disc.transform.localScale = Vector3.one * 0.16f;
                disc.transform.localPosition = sunDir * 0.9f;
                MoonAndStars(go.transform, sunDir);
            }
        }

        /// <summary>A soft breathing glow round the sun disc and a field of first stars (one combined mesh).</summary>
        static void MoonAndStars(Transform dome, Vector3 moonDir)
        {
            var halo = MeshFactory.MeshObject(MeshFactory.SmoothSphere(), dome, moonDir * 0.9f, Vector3.one * 0.2f, MaterialFactory.Additive(new Color(1f, 0.6f, 0.4f, 0.22f)), false);
            halo.name = "MoonHalo";
            halo.AddComponent<Pulse>().Speed = 0.35f;
            var verts = new List<Vector3>();
            var tris = new List<int>();
            var srand = new System.Random(77);
            for (int k = 0; k < 260; k++)
            {
                float az = (float)srand.NextDouble() * Mathf.PI * 2f;
                float el = Mathf.Lerp(0.12f, 1.45f, Mathf.Pow((float)srand.NextDouble(), 0.7f));
                Vector3 d = new Vector3(Mathf.Cos(el) * Mathf.Cos(az), Mathf.Sin(el), Mathf.Cos(el) * Mathf.Sin(az));
                if (Vector3.Dot(d, moonDir) > 0.97f) continue;
                Vector3 a = Vector3.Cross(d, Vector3.up).normalized;
                if (a.sqrMagnitude < 0.5f) a = Vector3.right;
                Vector3 b = Vector3.Cross(d, a);
                float s = 0.0016f + (float)srand.NextDouble() * (k % 9 == 0 ? 0.0035f : 0.0014f);
                Vector3 c = d * 0.95f;
                int i0 = verts.Count;
                verts.Add(c + a * s); verts.Add(c + b * s); verts.Add(c - a * s); verts.Add(c - b * s);
                tris.AddRange(new[] { i0, i0 + 1, i0 + 2, i0, i0 + 2, i0 + 3, i0, i0 + 2, i0 + 1, i0, i0 + 3, i0 + 2 });
            }
            var mesh = new Mesh { name = "Stars" };
            mesh.SetVertices(verts);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 2000f);
            var go = new GameObject("Stars");
            go.transform.SetParent(dome, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = MaterialFactory.Additive(new Color(0.9f, 0.92f, 1f, 0.9f));
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        /// <summary>Distant ranges in two or three layers, fading into the sky with distance.</summary>
        static void Background()
        {
            var mat = MaterialFactory.World(Color.white, 0f, K == Kind.Volcano ? new Color(1f, 0.35f, 0.05f) : (Color?)null, P.shadow, P.rim);
            var b = new WorldMeshBuilder(stat.parent, mat, "Ranges", false);
            Vector3 c = Journey.Flat(J.places[J.places.Count / 2].pos);
            Vector3 fwd = J.EndDirection;
            fwd.y = 0f;
            fwd.Normalize();
            for (int layer = 0; layer < 3; layer++)
            {
                float dist = 150f + layer * 55f;
                int count = 9 + layer * 3;
                for (int i = 0; i < count; i++)
                {
                    // Mostly ahead and to the sides (where the camera looks), a few behind.
                    float a = Mathf.Lerp(-150f, 150f, (i + R(0.2f, 0.8f)) / count);
                    Vector3 dir = Quaternion.Euler(0f, a, 0f) * fwd;
                    Vector3 p = c + dir * (dist + R(-15f, 15f)) + Vector3.up * -2f;
                    float hgt = Leafy ? R(35f, 70f) : K == Kind.Snow ? R(60f, 110f) : R(40f, 80f);
                    hgt *= 1f + layer * 0.25f;
                    float fade = 0.25f + layer * 0.25f;
                    Mountain(b, p, R(45f, 75f) * (1f + layer * 0.2f), hgt, fade, rng.Next(1000));
                }
            }
            // The world's great landmark peak straight ahead, beyond the destination.
            Vector3 hero = Journey.Flat(boss) + fwd * 120f;
            if (K == Kind.Volcano) Volcano(b, hero + Vector3.down * 2f, 70f, 85f);
            else Mountain(b, hero + Vector3.down * 2f, 80f, K == Kind.Snow ? 150f : 105f, 0.15f, 7);
            b.Flush(true);
        }

        static void Mountain(WorldMeshBuilder b, Vector3 p, float radius, float height, float fade, int seed)
        {
            const int rings = 9, seg = 18;
            var verts = new List<Vector3>();
            var cols = new List<Color>();
            var tris = new List<int>();
            float off = seed * 1.37f;
            Color rock = K == Kind.Forest ? new Color(0.36f, 0.48f, 0.4f) : K == Kind.Snow ? new Color(0.46f, 0.52f, 0.66f) : K == Kind.Village ? new Color(0.28f, 0.32f, 0.46f) : new Color(0.14f, 0.1f, 0.1f);
            Color lower = K == Kind.Forest ? new Color(0.22f, 0.38f, 0.24f) : K == Kind.Village ? new Color(0.16f, 0.26f, 0.3f) : rock * 0.8f;
            Color cap = K == Kind.Volcano ? new Color(0.28f, 0.24f, 0.24f) : new Color(0.96f, 0.98f, 1f);
            for (int r = 0; r <= rings; r++)
            {
                float t = r / (float)rings;
                for (int s = 0; s <= seg; s++)
                {
                    float a = s * Mathf.PI * 2f / seg;
                    float n = WorldKit.Noise(Mathf.Cos(a) * 1.3f + off, Mathf.Sin(a) * 1.3f + t * 2.2f);
                    float rr = radius * Mathf.Pow(1f - t, 1.25f) * (0.75f + 0.5f * n);
                    float y = height * t * (0.85f + 0.3f * WorldKit.Noise(a * 0.7f + off, t * 3f));
                    verts.Add(p + new Vector3(Mathf.Cos(a) * rr, y, Mathf.Sin(a) * rr));
                    Color col = Color.Lerp(lower, rock, SS(0.05f, 0.45f, t));
                    col = Color.Lerp(col, cap, SS(0.58f, 0.72f, t + (n - 0.5f) * 0.15f));
                    col = Color.Lerp(col, P.far, fade);
                    col.a = 0f;
                    cols.Add(col);
                    if (r < rings && s < seg)
                    {
                        int i0 = r * (seg + 1) + s, i1 = i0 + 1, i2 = i0 + seg + 1, i3 = i2 + 1;
                        tris.Add(i0); tris.Add(i2); tris.Add(i1);
                        tris.Add(i1); tris.Add(i2); tris.Add(i3);
                    }
                }
            }
            b.AddRaw(verts, cols, tris);
        }

        /// <summary>The volcano on the horizon: a broad cone with a glowing crater and lava streams down its flanks.</summary>
        static void Volcano(WorldMeshBuilder b, Vector3 p, float radius, float height)
        {
            const int rings = 12, seg = 24;
            var verts = new List<Vector3>();
            var cols = new List<Color>();
            var tris = new List<int>();
            for (int r = 0; r <= rings; r++)
            {
                float t = r / (float)rings;
                for (int s = 0; s <= seg; s++)
                {
                    float a = s * Mathf.PI * 2f / seg;
                    float n = WorldKit.Noise(Mathf.Cos(a) * 1.5f, Mathf.Sin(a) * 1.5f + t * 2f);
                    float rr = Mathf.Lerp(radius, radius * 0.18f, Mathf.Pow(t, 0.8f)) * (0.85f + 0.3f * n);
                    float y = height * t;
                    if (r == rings) { rr = radius * 0.1f; y = height * 0.9f; }
                    verts.Add(p + new Vector3(Mathf.Cos(a) * rr, y, Mathf.Sin(a) * rr));
                    float stream = SS(0.85f, 0.97f, Mathf.Abs(Mathf.Sin(a * 3f + 0.5f))) * SS(0.3f, 0.9f, t);
                    Color col = Color.Lerp(new Color(0.12f, 0.08f, 0.08f), new Color(0.2f, 0.14f, 0.13f), n);
                    col = Color.Lerp(col, new Color(0.9f, 0.3f, 0.05f), stream);
                    col.a = Mathf.Max(stream, SS(0.85f, 1f, t)) * 0.9f;
                    cols.Add(col);
                    if (r < rings && s < seg)
                    {
                        int i0 = r * (seg + 1) + s, i1 = i0 + 1, i2 = i0 + seg + 1, i3 = i2 + 1;
                        tris.Add(i0); tris.Add(i2); tris.Add(i1);
                        tris.Add(i1); tris.Add(i2); tris.Add(i3);
                    }
                }
            }
            b.AddRaw(verts, cols, tris);
            // Smoke plume and a red glow over the crater.
            var plume = new GameObject("Plume").transform;
            plume.SetParent(dyn, false);
            plume.position = p + Vector3.up * height;
            var smoke = EnvFx.Smoke(plume, Vector3.zero, 6f);
            if (smoke != null)
            {
                var m = smoke.main;
                m.startColor = new Color(0.18f, 0.12f, 0.12f, 0.55f);
                m.startLifetime = 12f;
            }
            var glow = MeshFactory.MeshObject(MeshFactory.SmoothSphere(), dyn, p + Vector3.up * (height * 0.95f), Vector3.one * radius * 0.5f,
                MaterialFactory.Additive(new Color(1f, 0.35f, 0.08f, 0.25f)), false);
            glow.AddComponent<Pulse>().Speed = 1.2f;
        }

        // ------------------------------------------------------------------ Lighting and atmosphere

        static void ApplyPalette(Palette p, Kind k)
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = p.fog;
            RenderSettings.fogStartDistance = p.fogStart;
            RenderSettings.fogEndDistance = p.fogEnd;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            // Cartoon look: strong, bright ambient so nothing sinks into murky shadow.
            float amb = GameConfig.CartoonStyle ? 0.92f : 0.85f;
            if (k == Kind.Snow) amb = 0.78f;
            // Bright snow glowed under bloom and washed out the attacks; only real highlights bloom there.
            if (PostFX.Instance != null)
            {
                PostFX.Instance.Threshold = k == Kind.Snow ? 1.2f : 0.85f;
                PostFX.Instance.BloomIntensity = k == Kind.Snow ? 0.28f : 0.45f;
            }
            RenderSettings.ambientSkyColor = p.amSky * amb;
            RenderSettings.ambientEquatorColor = p.amEquator * amb;
            RenderSettings.ambientGroundColor = p.amGround * amb;
            var cam = Camera.main;
            if (cam != null)
            {
                cam.backgroundColor = p.skyHorizon;
                cam.farClipPlane = 420f;
            }
            var sun = RenderSettings.sun;
            if (sun != null)
            {
                sun.color = p.sun;
                sun.intensity = p.sunIntensity;
                sun.transform.rotation = Quaternion.Euler(p.sunEuler);
                sun.shadows = LightShadows.Soft;
                sun.shadowStrength = k == Kind.Snow ? 0.4f : k == Kind.Village ? 0.6f : 0.7f;
            }
        }

        static void Lighting(bool battleCamera)
        {
            ApplyPalette(P, K);
            root.gameObject.AddComponent<LightingRestorer>();
            if (battleCamera && CameraController.Instance != null)
            {
                // A slightly lower, more cinematic angle so the landscape and horizon read behind the fight.
                var restore = root.gameObject.AddComponent<CameraOffsetRestorer>();
                restore.previous = CameraController.Instance.Offset;
                CameraController.Instance.Offset = new Vector3(0f, 8.6f, -10.6f);
            }
        }

        static void Atmosphere(JourneyBuilder.Result res)
        {
            Vector3 c = Journey.Flat(J.places[J.places.Count / 2].pos);
            if (K == Kind.Village) CloudDrift.CreateLayer(dyn, c, 10, 36f, new Color(0.52f, 0.5f, 0.74f, 0.42f), 120f);
            else if (K != Kind.Volcano) CloudDrift.CreateLayer(dyn, c, 12, 34f, new Color(1f, 1f, 1f, K == Kind.Snow ? 0.7f : 0.6f), 120f);
            else CloudDrift.CreateLayer(dyn, c, 10, 30f, new Color(0.25f, 0.15f, 0.15f, 0.6f), 120f);
            if (K == Kind.Snow || K == Kind.Forest)
            {
                BirdFlock.Create(dyn, J.places[1].pos + Vector3.up * 4f, 6, K == Kind.Snow ? new Color(0.2f, 0.22f, 0.3f) : new Color(0.95f, 0.95f, 0.95f)).Radius = 26f;
                BirdFlock.Create(dyn, boss + Vector3.up * 8f, 5, new Color(0.2f, 0.2f, 0.25f)).Radius = 34f;
            }
            var follow = new GameObject("WeatherAnchor").transform;
            follow.SetParent(dyn, false);
            follow.position = J.Start;
            res.follow = follow;
            switch (K)
            {
                case Kind.Snow:
                    res.weather = EnvFx.Weather(follow, Vector3.zero, "snow", 36f);
                    break;
                case Kind.Volcano:
                    res.weather = EnvFx.Weather(follow, Vector3.zero, "embers", 36f);
                    EnvFx.Weather(follow, Vector3.zero, "ash", 36f);
                    break;
                case Kind.Village:
                    // Cherry petals on the night breeze and fireflies drifting over the grass.
                    res.weather = EnvFx.Weather(follow, Vector3.zero, "petals", 40f);
                    EnvFx.Weather(follow, Vector3.zero, "motes", 34f);
                    break;
                default:
                    res.weather = EnvFx.Weather(follow, Vector3.zero, "leaves", 36f);
                    EnvFx.Weather(follow, Vector3.zero, "motes", 30f);
                    break;
            }
            // Low fog banks drifting over the ground away from the road.
            var fogMat = MaterialFactory.Transparent(new Color(P.fog.r, P.fog.g, P.fog.b, K == Kind.Volcano ? 0.22f : 0.16f), true);
            for (int i = 0; i < (K == Kind.Village ? 14 : 26); i++)
            {
                float s = R(0f, J.Length);
                Vector3 p = Journey.Flat(J.PointAt(s));
                Vector3 d = Journey.Flat(J.PointAt(s + 1f)) - p;
                d.Normalize();
                Vector3 side = Vector3.Cross(Vector3.up, d) * (J.halfWidth + R(8f, 28f)) * (rng.Next(2) == 0 ? 1f : -1f);
                Vector3 at = p + side;
                var f = MeshFactory.MeshObject(MeshFactory.Disc(), dyn, at + Vector3.up * (Ground(at) + R(0.6f, 1.8f)), new Vector3(R(8f, 14f), 1f, R(5f, 9f)), fogMat, false);
                f.transform.rotation = Quaternion.Euler(0f, R(0f, 360f), 0f);
                var drift = f.AddComponent<CloudDrift>();
                drift.Speed = R(0.15f, 0.4f);
                drift.WrapX = 8f;
            }
        }
    }

    /// <summary>Keeps the sky dome centred on the camera.</summary>
    public class SkyFollow : MonoBehaviour
    {
        void LateUpdate()
        {
            var cam = Camera.main;
            if (cam != null) transform.position = cam.transform.position;
        }
    }

    public class CameraOffsetRestorer : MonoBehaviour
    {
        public Vector3 previous = new Vector3(0f, 11f, -8.5f);

        void OnDestroy()
        {
            if (CameraController.Instance != null) CameraController.Instance.Offset = previous;
        }
    }
}
