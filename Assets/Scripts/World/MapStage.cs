using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// The 3D world map: a continent with a landmark for every region (village, forest, snowy peaks, the capital,
    /// the burning wastes, the temple, the fallen city, the Demon Lord's castle), winding stone paths, glowing
    /// location markers, drifting clouds, and the team leader who physically walks the road between locations.
    /// </summary>
    public partial class MapStage : MonoBehaviour
    {
        public static readonly string[] RouteOrder = { "village", "forest", "mountain", "kingdom", "demonland", "temple", "fallen", "castle" };

        class Node
        {
            public RegionDefinition region;
            public Vector3 pos;
            public Material ringMat, beamMat;
            public Transform beam;
        }

        readonly Dictionary<string, Node> nodes = new Dictionary<string, Node>();
        readonly List<List<Vector3>> segments = new List<List<Vector3>>();
        readonly List<List<Renderer>> segmentStones = new List<List<Renderer>>();
        GameObject world;
        Transform token;
        CharacterVisual tokenVisual;
        string tokenHeroId;
        Coroutine travel;
        Material stoneOn, stoneOff;

        public bool Traveling { get { return travel != null; } }
        /// <summary>Called once, halfway along a journey; return true to pause travel (an encounter is shown).</summary>
        public System.Func<string, string, bool> OnMidway;
        /// <summary>Travel waits while this returns true (encounter dialog open).</summary>
        public System.Func<bool> Paused;
        float arriveZoom;
        public string CurrentRegion { get; private set; }
        public Vector3 CameraFocus { get; private set; }

        public void Show(PlayerData data)
        {
            arriveZoom = 0f;
            EnsureWorld();
            gameObject.SetActive(true);
            CurrentRegion = string.IsNullOrEmpty(data.currentRegion) || !nodes.ContainsKey(data.currentRegion) ? "village" : data.currentRegion;
            if (travel == null && !AreaMode) token.position = nodes[CurrentRegion].pos;
            string lead = data.team.Count > 0 ? data.team[0] : GameDatabase.Protagonist;
            if (lead != tokenHeroId)
            {
                if (tokenVisual != null) Destroy(tokenVisual.gameObject);
                tokenVisual = CharacterVisual.BuildHero(GameDatabase.GetCharacter(lead), token);
                tokenHeroId = lead;
            }
            RefreshStates(data);
            if (AreaMode) ApplyAreaLighting();
            else ApplyLighting();
        }

        public void Hide() { ExitArea(); gameObject.SetActive(false); }

        void OnDisable()
        {
            // Hiding the map mid-journey cancels the walk (coroutines stop with the object).
            travel = null;
            if (tokenVisual != null) tokenVisual.SetMoving(0f);
        }

        public Vector3 NodeWorld(string regionId)
        {
            Node n;
            return nodes.TryGetValue(regionId, out n) ? n.pos : Vector3.zero;
        }

        void ApplyLighting()
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.55f, 0.7f, 0.85f);
            RenderSettings.fogStartDistance = 45f;
            RenderSettings.fogEndDistance = 140f;
            RenderSettings.ambientLight = new Color(0.6f, 0.62f, 0.7f);
            if (Camera.main != null) Camera.main.backgroundColor = new Color(0.45f, 0.62f, 0.85f);
            if (RenderSettings.sun != null)
            {
                RenderSettings.sun.color = new Color(1f, 0.95f, 0.85f);
                RenderSettings.sun.intensity = 1.2f;
                RenderSettings.sun.transform.rotation = Quaternion.Euler(55f, 25f, 0f);
            }
        }

        // ------------------------------------------------------------------ Travel

        /// <summary>Walks the leader along the road to a region; the camera follows. Calls back on arrival.</summary>
        public void TravelTo(string regionId, PlayerData data, System.Action onArrive)
        {
            ExitArea();
            if (!nodes.ContainsKey(regionId)) { if (onArrive != null) onArrive(); return; }
            if (travel != null) StopCoroutine(travel);
            travel = StartCoroutine(TravelRoutine(regionId, data, onArrive));
        }

        IEnumerator TravelRoutine(string target, PlayerData data, System.Action onArrive)
        {
            int from = System.Array.IndexOf(RouteOrder, CurrentRegion);
            int to = System.Array.IndexOf(RouteOrder, target);
            var points = new List<Vector3>();
            if (from >= 0 && to >= 0 && from != to)
            {
                int step = to > from ? 1 : -1;
                for (int i = from; i != to; i += step)
                {
                    var seg = segments[Mathf.Min(i, i + step)];
                    if (step > 0) points.AddRange(seg);
                    else for (int k = seg.Count - 1; k >= 0; k--) points.Add(seg[k]);
                }
            }
            points.Add(nodes[target].pos);
            arriveZoom = 0f;
            string fromRegion = CurrentRegion;
            float total = 0f;
            Vector3 prev = token.position;
            foreach (var p in points) { total += Vector3.Distance(prev, p); prev = p; }
            float walked = 0f;
            bool midwayDone = total < 12f;
            var destTheme = nodes[target].region.theme;
            Color fog0 = RenderSettings.fogColor;
            if (tokenVisual != null) tokenVisual.SetMoving(1f, true);
            var audio = GameManager.Instance != null ? GameManager.Instance.Audio : null;
            float dustTimer = 0f;
            const float speed = 22f;
            foreach (var p in points)
            {
                while ((token.position - p).sqrMagnitude > 0.05f)
                {
                    Vector3 d = p - token.position;
                    d.y = 0f;
                    float stepLen = speed * Time.unscaledDeltaTime;
                    walked += Mathf.Min(stepLen, d.magnitude);
                    if (d.magnitude <= stepLen) token.position = p;
                    else token.position += d.normalized * stepLen;
                    // The land changes as he travels: the light shifts toward the destination's mood.
                    RenderSettings.fogColor = Color.Lerp(fog0, Color.Lerp(fog0, destTheme.fog, 0.6f), Mathf.Clamp01(walked / Mathf.Max(1f, total)));
                    if (!midwayDone && walked > total * 0.5f)
                    {
                        midwayDone = true;
                        if (OnMidway != null && OnMidway(fromRegion, target))
                        {
                            if (tokenVisual != null) tokenVisual.SetMoving(0f);
                            while (Paused != null && Paused()) yield return null;
                            if (tokenVisual != null) tokenVisual.SetMoving(1f, true);
                        }
                    }
                    if (d.sqrMagnitude > 0.001f) token.rotation = Quaternion.Slerp(token.rotation, Quaternion.LookRotation(d), Time.unscaledDeltaTime * 10f);
                    dustTimer -= Time.unscaledDeltaTime;
                    if (dustTimer <= 0f)
                    {
                        dustTimer = 0.18f;
                        VFX.Dust(token.position, 2);
                        if (audio != null) audio.Play("step", 0.15f);
                    }
                    yield return null;
                }
            }
            if (tokenVisual != null) tokenVisual.SetMoving(0f);
            CurrentRegion = target;
            data.currentRegion = target;
            VFX.Shockwave(token.position, 3f, new Color(1f, 0.85f, 0.4f), 0.5f);
            // The camera swoops down into the destination before the mission begins.
            if (onArrive != null)
            {
                float z = 0f;
                while (z < 1f)
                {
                    z += Time.unscaledDeltaTime / 1.1f;
                    arriveZoom = Mathf.SmoothStep(0f, 1f, z);
                    yield return null;
                }
            }
            travel = null;
            if (onArrive != null) onArrive();
        }

        void Update()
        {
            if (token == null || CameraController.Instance == null) return;
            if (AreaMode) { UpdateArea(); return; }
            CameraFocus = token.position;
            float t = Time.unscaledTime;
            Vector3 far = new Vector3(Mathf.Sin(t * 0.1f) * 1.5f, 26f, -21f);
            Vector3 camPos = token.position + Vector3.Lerp(far, new Vector3(0f, 5f, -8f), arriveZoom);
            CameraController.Instance.SetFixed(camPos, token.position + Vector3.up * (1f + arriveZoom));
            // Idle hop so the leader never feels static on the map.
            if (travel == null && tokenVisual != null) tokenVisual.SetMoving(0f);
            foreach (var n in nodes.Values)
                if (n.beam != null) n.beam.localScale = new Vector3(0.6f + Mathf.Sin(t * 2f) * 0.08f, n.beam.localScale.y, 0.6f + Mathf.Sin(t * 2f) * 0.08f);
        }

        // ------------------------------------------------------------------ Building

        void RefreshStates(PlayerData data)
        {
            foreach (var kv in nodes)
            {
                var n = kv.Value;
                bool unlocked = data.IsRegionUnlocked(n.region);
                bool current = kv.Key == CurrentRegion;
                bool bossOpen = false;
                foreach (var m in GameDatabase.MissionsInRegion(kv.Key))
                    if (m.type == MissionType.Boss && data.IsMissionUnlocked(m) && !data.IsMissionCleared(m.id)) bossOpen = true;
                Color c = !unlocked ? new Color(0.4f, 0.4f, 0.45f) : current ? new Color(1f, 0.85f, 0.35f) : bossOpen ? new Color(1f, 0.3f, 0.25f) : new Color(0.4f, 0.8f, 1f);
                n.ringMat.color = new Color(c.r, c.g, c.b, 0.8f);
                n.beamMat.color = new Color(c.r, c.g, c.b, unlocked ? 0.5f : 0.15f);
            }
            for (int i = 0; i < segmentStones.Count; i++)
            {
                bool open = data.IsRegionUnlocked(GameDatabase.GetRegion(RouteOrder[i + 1]));
                foreach (var r in segmentStones[i]) r.sharedMaterial = open ? stoneOn : stoneOff;
            }
        }

        void EnsureWorld()
        {
            if (world != null) return;
            GameDatabase.EnsureBuilt();
            world = new GameObject("MapWorld");
            world.transform.SetParent(transform, false);
            var root = world.transform;

            var sea = MeshFactory.MeshObject(MeshFactory.Disc(), root, new Vector3(0f, -0.3f, 0f), new Vector3(160f, 1f, 160f), MaterialFactory.Toon(new Color(0.18f, 0.38f, 0.6f), 0f), false);
            sea.GetComponent<Renderer>().receiveShadows = false;
            var land = MaterialFactory.Toon(new Color(0.42f, 0.55f, 0.3f), 0f);
            MeshFactory.MeshObject(MeshFactory.Disc(), root, Vector3.zero, new Vector3(62f, 1f, 42f), land, false);
            MeshFactory.MeshObject(MeshFactory.Disc(), root, new Vector3(-30f, 0.01f, -5f), new Vector3(28f, 1f, 26f), land, false);
            MeshFactory.MeshObject(MeshFactory.Disc(), root, new Vector3(32f, 0.01f, -8f), new Vector3(26f, 1f, 28f), land, false);

            stoneOn = MaterialFactory.Toon(new Color(0.95f, 0.85f, 0.55f), 0f);
            stoneOff = MaterialFactory.Toon(new Color(0.45f, 0.45f, 0.45f), 0f);
            var rng = new System.Random(11);

            foreach (var id in RouteOrder)
            {
                var r = GameDatabase.GetRegion(id);
                var n = new Node { region = r, pos = new Vector3(r.mapPosition.x, 0f, r.mapPosition.y) };
                MeshFactory.MeshObject(MeshFactory.Disc(), root, n.pos + Vector3.up * 0.03f, new Vector3(8f, 1f, 8f), MaterialFactory.Toon(r.mapColor, 0f), false);
                Landmark(root, r, n.pos, rng);
                n.ringMat = MaterialFactory.Additive(Color.white);
                var ring = MeshFactory.MeshObject(MeshFactory.Ring(0.82f), root, n.pos + Vector3.up * 0.08f, new Vector3(3.2f, 1f, 3.2f), n.ringMat, false);
                var sp = ring.AddComponent<Spinner>();
                sp.DegreesPerSecond = new Vector3(0f, 30f, 0f);
                sp.Unscaled = true;
                n.beamMat = MaterialFactory.Additive(Color.white);
                var beam = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                var col = beam.GetComponent<Collider>();
                if (col != null) Destroy(col);
                beam.transform.SetParent(root, false);
                beam.transform.position = n.pos + Vector3.up * 6f;
                beam.transform.localScale = new Vector3(0.6f, 6f, 0.6f);
                var br = beam.GetComponent<Renderer>();
                br.sharedMaterial = n.beamMat;
                br.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                n.beam = beam.transform;
                nodes[id] = n;
            }

            // Winding stone paths between consecutive locations.
            for (int i = 0; i < RouteOrder.Length - 1; i++)
            {
                Vector3 a = nodes[RouteOrder[i]].pos, b = nodes[RouteOrder[i + 1]].pos;
                Vector3 mid = (a + b) * 0.5f + Vector3.Cross(Vector3.up, (b - a).normalized) * ((i % 2 == 0) ? 5f : -5f);
                var pts = new List<Vector3>();
                var stones = new List<Renderer>();
                float len = Vector3.Distance(a, b);
                int count = Mathf.Max(6, Mathf.RoundToInt(len / 1.8f));
                for (int k = 1; k < count; k++)
                {
                    float t = (float)k / count;
                    Vector3 p = (1 - t) * (1 - t) * a + 2 * (1 - t) * t * mid + t * t * b;
                    pts.Add(p);
                    var s = MeshFactory.MeshObject(MeshFactory.Disc(), root, p + Vector3.up * 0.06f, new Vector3(0.55f, 1f, 0.55f), stoneOff, false);
                    stones.Add(s.GetComponent<Renderer>());
                }
                segments.Add(pts);
                segmentStones.Add(stones);
            }

            CloudDrift.CreateLayer(root, Vector3.zero, 10, 16f, new Color(1f, 1f, 1f, 0.5f), 70f);
            BirdFlock.Create(root, new Vector3(-10f, 0f, 0f), 6, new Color(0.95f, 0.95f, 0.95f)).Radius = 30f;
            var sea2 = EnvFx.Weather(root, Vector3.zero, "leaves", 90f);
            sea2.transform.position = new Vector3(0f, 12f, 0f);

            token = new GameObject("MapToken").transform;
            token.SetParent(transform, false);
            token.localScale = Vector3.one * 1.8f;
        }

        System.Random landRng;
        float Rnd(float a, float b) { return a + (float)landRng.NextDouble() * (b - a); }
        static Material M(Color c, bool glow = false) { return MaterialFactory.Toon(c, 0.02f, glow ? c : (Color?)null); }

        void Landmark(Transform root, RegionDefinition r, Vector3 p, System.Random rng)
        {
            landRng = rng;
            switch (r.theme.kind)
            {
                case EnvironmentKind.Village:
                    for (int i = 0; i < 4; i++)
                    {
                        Vector3 h = p + new Vector3(Rnd(-4f, 4f), 0f, Rnd(-4f, 4f));
                        MeshFactory.Primitive(PrimitiveType.Cube, root, h + Vector3.up * 0.6f, new Vector3(1.6f, 1.2f, 1.3f), M(new Color(0.85f, 0.75f, 0.6f)));
                        MeshFactory.MeshObject(MeshFactory.Cone(), root, h + Vector3.up * 1.2f, new Vector3(2.2f, 1f, 2f), M(new Color(0.5f, 0.3f, 0.2f)));
                    }
                    break;
                case EnvironmentKind.Forest:
                    for (int i = 0; i < 14; i++)
                    {
                        Vector3 t = p + new Vector3(Rnd(-6f, 6f), 0f, Rnd(-6f, 6f));
                        MeshFactory.MeshObject(MeshFactory.Cone(), root, t, new Vector3(2f, Rnd(3f, 5f), 2f), M(new Color(0.08f, 0.25f, 0.14f)));
                    }
                    break;
                case EnvironmentKind.Mountain:
                    for (int i = 0; i < 4; i++)
                    {
                        Vector3 m = p + new Vector3(Rnd(-5f, 5f), 0f, Rnd(-3f, 6f));
                        MeshFactory.MeshObject(MeshFactory.Cone(), root, m, new Vector3(Rnd(6f, 9f), Rnd(6f, 10f), Rnd(6f, 9f)), M(new Color(0.88f, 0.9f, 0.96f)));
                    }
                    break;
                case EnvironmentKind.Kingdom:
                    MeshFactory.Primitive(PrimitiveType.Cube, root, p + Vector3.up * 1f, new Vector3(8f, 2f, 6f), M(new Color(0.9f, 0.86f, 0.78f)));
                    for (int i = 0; i < 4; i++)
                    {
                        Vector3 t = p + new Vector3(i % 2 == 0 ? -3.5f : 3.5f, 0f, i < 2 ? -2.5f : 2.5f);
                        MeshFactory.Primitive(PrimitiveType.Cylinder, root, t + Vector3.up * 2.5f, new Vector3(1.4f, 2.5f, 1.4f), M(new Color(0.9f, 0.86f, 0.78f)));
                        MeshFactory.MeshObject(MeshFactory.Cone(), root, t + Vector3.up * 5f, new Vector3(2f, 2f, 2f), M(new Color(0.25f, 0.35f, 0.6f)));
                    }
                    MeshFactory.Primitive(PrimitiveType.Cylinder, root, p + Vector3.up * 4f, new Vector3(2f, 4f, 2f), M(new Color(0.95f, 0.9f, 0.82f)));
                    MeshFactory.MeshObject(MeshFactory.Cone(), root, p + Vector3.up * 8f, new Vector3(3f, 3f, 3f), M(new Color(0.25f, 0.35f, 0.6f)));
                    break;
                case EnvironmentKind.DemonLand:
                    MeshFactory.MeshObject(MeshFactory.Disc(), root, p + Vector3.up * 0.05f, new Vector3(5f, 1f, 5f), M(new Color(1f, 0.35f, 0.05f), true), false);
                    for (int i = 0; i < 10; i++)
                        MeshFactory.MeshObject(MeshFactory.Cone(), root, p + new Vector3(Rnd(-6f, 6f), 0f, Rnd(-6f, 6f)), new Vector3(1f, Rnd(2f, 5f), 1f), M(new Color(0.15f, 0.06f, 0.05f)));
                    EnvFx.Smoke(root, p + Vector3.up * 2f, 2f);
                    break;
                case EnvironmentKind.Temple:
                    for (int i = 0; i < 8; i++)
                    {
                        Vector3 c = p + Quaternion.Euler(0f, i * 45f, 0f) * Vector3.forward * 3.5f;
                        MeshFactory.Primitive(PrimitiveType.Cylinder, root, c + Vector3.up * 1.5f, new Vector3(0.6f, 1.5f, 0.6f), M(new Color(0.6f, 0.6f, 0.55f)));
                    }
                    MeshFactory.MeshObject(MeshFactory.Ring(0.85f), root, p + Vector3.up * 0.07f, new Vector3(4.5f, 1f, 4.5f), M(new Color(0.4f, 0.9f, 1f), true), false);
                    break;
                case EnvironmentKind.FallenCity:
                    for (int i = 0; i < 6; i++)
                    {
                        Vector3 b = p + new Vector3(Rnd(-5f, 5f), 0f, Rnd(-5f, 5f));
                        var bld = MeshFactory.Primitive(PrimitiveType.Cube, root, b + Vector3.up * 1f, new Vector3(1.5f, Rnd(1.2f, 3f), 1.5f), M(new Color(0.35f, 0.3f, 0.28f)));
                        bld.transform.localRotation = Quaternion.Euler(Rnd(-10f, 10f), Rnd(0f, 90f), Rnd(-10f, 10f));
                    }
                    EnvFx.Fire(root, p, 2f, false);
                    EnvFx.Smoke(root, p + Vector3.up * 3f, 2.5f);
                    break;
                default:
                    for (int i = 0; i < 5; i++)
                    {
                        Vector3 s = p + new Vector3((i - 2) * 1.8f, 0f, Mathf.Abs(i - 2) * 1.2f);
                        float h = 10f - Mathf.Abs(i - 2) * 2.5f;
                        MeshFactory.Primitive(PrimitiveType.Cube, root, s + Vector3.up * h * 0.5f, new Vector3(1.4f, h, 1.4f), M(new Color(0.12f, 0.08f, 0.15f)));
                        MeshFactory.MeshObject(MeshFactory.Cone(), root, s + Vector3.up * h, new Vector3(1.8f, 3f, 1.8f), M(new Color(0.35f, 0.08f, 0.2f)));
                    }
                    var orb = MeshFactory.Primitive(PrimitiveType.Sphere, root, p + Vector3.up * 13f, Vector3.one * 2f, M(new Color(0.9f, 0.15f, 0.4f), true));
                    orb.AddComponent<Spinner>().BobHeight = 0.6f;
                    break;
            }
        }
    }
}
