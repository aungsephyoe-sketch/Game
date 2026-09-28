using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>Root of one battle: builds the arena, team and mission, tracks combo/damage, owns cleanup.</summary>
    public class BattleController : MonoBehaviour
    {
        public static BattleController Current { get; private set; }
        public const float ArenaRadius = 15.5f;

        public MissionDefinition Def { get; private set; }
        public TeamSystem Team { get; private set; }
        public MissionSystem Mission { get; private set; }
        public PlayerController Controller { get; private set; }

        public int Combo { get; private set; }
        public int MaxCombo { get; private set; }
        public float ComboTimer { get; private set; }
        public float TotalDamage { get; private set; }
        public bool Finished { get { return Mission != null && Mission.Finished; } }

        /// <summary>The mission's route (null in the training ground, which is a single arena).</summary>
        public Journey Journey { get; private set; }
        public JourneyBuilder.Result World { get; private set; }
        /// <summary>True while a fight holds the team inside a clearing (barrier up).</summary>
        public bool Locked { get; private set; }
        public Vector3 LockCenter { get; private set; }
        public float LockRadius { get; private set; }
        /// <summary>Player input is ignored while a cinematic plays.</summary>
        public bool CinematicLock;
        GameObject barrier;

        /// <summary>Keeps a position inside the playable space: the locked clearing, else the road and its clearings.</summary>
        /// <summary>
        /// Which of the new-standard worlds a mission is built in: forests, villages and kingdom roads become the
        /// Forest world; mountains and temples the Snow Mountain; demon lands, castles and fallen cities the Volcano.
        /// </summary>
        public static string WorldEnv(MissionDefinition def)
        {
            if (def == null || def.openWorld || def.training) return null;
            if (!string.IsNullOrEmpty(def.prototypeEnv)) return def.prototypeEnv;
            if (!GameConfig.NewWorlds || def.theme == null) return null;
            switch (def.theme.kind)
            {
                case EnvironmentKind.Mountain: case EnvironmentKind.Temple: return "snow";
                case EnvironmentKind.DemonLand: case EnvironmentKind.Castle: case EnvironmentKind.FallenCity: return "volcano";
                default: return "forest";
            }
        }

        public static Vector3 ClampToArena(Vector3 p)
        {
            p.y = 0f;
            var b = Current;
            if (b != null && b.Journey != null)
            {
                if (b.Locked)
                {
                    Vector3 o = p - b.LockCenter;
                    o.y = 0f;
                    if (o.magnitude > b.LockRadius) p = b.LockCenter + o.normalized * b.LockRadius;
                    p = Obstacles.Resolve(p);
                    p.y = 0f;
                    return p;
                }
                p = Obstacles.Resolve(b.Journey.Clamp(p));
                p.y = 0f;
                return p;
            }
            if (b != null && b.Def != null && b.Def.openWorld) return Obstacles.Resolve(OpenWorldBuilder.Clamp(p));
            float m = new Vector2(p.x, p.z).magnitude;
            if (m > ArenaRadius) p *= ArenaRadius / m;
            p = Obstacles.Resolve(p);
            m = new Vector2(p.x, p.z).magnitude;
            if (m > ArenaRadius) p *= ArenaRadius / m;
            p.y = 0f;
            return p;
        }

        /// <summary>Centre of the current fighting area (the locked clearing, else the origin arena).</summary>
        public static Vector3 ArenaCenter
        {
            get
            {
                var b = Current;
                if (b == null || b.Journey == null) return Vector3.zero;
                if (b.Locked) return b.LockCenter;
                return b.Team != null && b.Team.Active != null ? b.Team.Active.Position : Vector3.zero;
            }
        }

        public static float CurrentArenaRadius
        {
            get
            {
                var b = Current;
                return b != null && b.Journey != null && b.Locked ? b.LockRadius : ArenaRadius;
            }
        }

        /// <summary>Raises a spirit barrier around a clearing: nobody leaves until the fight is over.</summary>
        public void Lock(Vector3 center, float radius, Color color)
        {
            Unlock();
            Locked = true;
            LockCenter = new Vector3(center.x, 0f, center.z);
            LockRadius = radius;
            barrier = new GameObject("Barrier");
            barrier.transform.SetParent(transform, false);
            barrier.transform.position = LockCenter;
            var mat = MaterialFactory.Additive(new Color(color.r, color.g, color.b, 0.55f));
            var ring = MeshFactory.MeshObject(MeshFactory.Ring(0.97f), barrier.transform, Vector3.up * 0.05f, new Vector3(radius + 0.5f, 1f, radius + 0.5f), mat, false);
            ring.AddComponent<Spinner>().DegreesPerSecond = new Vector3(0f, 12f, 0f);
            int n = 28;
            var wall = MaterialFactory.Additive(new Color(color.r, color.g, color.b, 0.22f));
            for (int i = 0; i < n; i++)
            {
                float a = i * Mathf.PI * 2f / n;
                var post = MeshFactory.Primitive(PrimitiveType.Cube, barrier.transform, new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * (radius + 0.5f) + Vector3.up * 1.2f,
                    new Vector3(0.08f, 2.4f, radius * 2f * Mathf.PI / n), wall);
                post.transform.localRotation = Quaternion.Euler(0f, -a * Mathf.Rad2Deg, 0f);
                post.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            VFX.Shockwave(LockCenter, radius, color, 0.6f);
            if (GameManager.Instance != null) GameManager.Instance.Audio.Play("charge", 0.4f);
        }

        public void Unlock()
        {
            if (barrier != null)
            {
                VFX.Shockwave(LockCenter, LockRadius, Color.white, 0.4f);
                Destroy(barrier);
            }
            barrier = null;
            Locked = false;
        }

        public void Setup(MissionDefinition def, PlayerData data)
        {
            Current = this;
            Def = def;
            TimeController.ResetAll();
            EnemyController.ResetTokens();
            DamageNumbers.Clear();
            PlayerCharacter.LockTarget = null;

            Vector3 spawn = new Vector3(0f, 0f, -6f);
            Obstacles.Clear();
            if (def.openWorld)
            {
                World = OpenWorldBuilder.Build(transform);
                spawn = OpenWorldBuilder.Spawn;
                gameObject.AddComponent<VillageHub>();
            }
            else if (def.collisionTest) CollisionTestArena.Build(def.theme, transform);
            else if (def.training) ArenaBuilder.Build(def.theme, transform);
            else
            {
                // Every mission is a journey through the region toward its destination.
                try
                {
                    Journey = Journey.Build(def);
                    World = string.IsNullOrEmpty(WorldEnv(def)) ? JourneyBuilder.Build(Journey, def, transform) : PrototypeWorld.Build(Journey, def, transform);
                    spawn = Journey.Start;
                }
                catch (System.Exception ex)
                {
                    // Never block a mission: fall back to the single-arena layout.
                    Debug.LogError("[Battle] journey build failed, using a single arena: " + ex);
                    ArenaDecor.Ambient = true;
                    foreach (Transform c in transform) Destroy(c.gameObject);
                    Journey = null;
                    World = null;
                    Obstacles.Clear();
                    ArenaBuilder.Build(def.theme, transform);
                }
            }

            // Solid scenery blocks movement (the prototype worlds register their own obstacles).
            if (Journey == null || string.IsNullOrEmpty(WorldEnv(def))) Obstacles.Scan(transform);
            // Scenery never hides the fighting slayer.
            CameraOcclusion.Attach(this);
            // Tall set pieces (towers, spires, far hills) threw long jagged shadow streaks across the road.
            foreach (var rend in GetComponentsInChildren<Renderer>(true))
                if (rend.bounds.size.y > 5f) rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            if (GameManager.Instance != null) GameManager.Instance.Audio.SetAmbience(AmbienceFor(def.theme));
            var teamGo = new GameObject("Team");
            teamGo.transform.SetParent(transform, false);
            Team = teamGo.AddComponent<TeamSystem>();
            Team.Setup(data, spawn);
            if (Journey != null && Team.Active != null && Journey.path.Count > 1)
            {
                Vector3 dir = Journey.path[1] - Journey.path[0];
                dir.y = 0f;
                if (dir.sqrMagnitude > 0.001f) foreach (var mbr in Team.Members) mbr.transform.rotation = Quaternion.LookRotation(dir);
            }
            Team.ActiveChanged += OnActiveChanged;

            Controller = gameObject.AddComponent<PlayerController>();
            Controller.Team = Team;

            if (CameraController.Instance != null && Team.Active != null)
                CameraController.Instance.Follow(Team.Active.transform, true);

            Mission = gameObject.AddComponent<MissionSystem>();
            Mission.Begin(def, this);
        }

        public static string AmbienceFor(ArenaTheme t)
        {
            switch (t.kind)
            {
                case EnvironmentKind.Village: return t.burning ? "fire" : "village";
                case EnvironmentKind.Forest: case EnvironmentKind.Temple: return "forest";
                case EnvironmentKind.Mountain: return "wind";
                case EnvironmentKind.Kingdom: return "village";
                case EnvironmentKind.FallenCity: case EnvironmentKind.DemonLand: return "fire";
                default: return "dark";
            }
        }

        void OnActiveChanged(PlayerCharacter pc)
        {
            if (CameraController.Instance != null) CameraController.Instance.Follow(pc.transform, false);
        }

        public void RegisterHit(float damage)
        {
            Combo++;
            if (Combo > MaxCombo) MaxCombo = Combo;
            ComboTimer = 2.2f;
            TotalDamage += damage;
        }

        void Update()
        {
            if (World != null && World.follow != null && Team != null && Team.Active != null) World.follow.position = Team.Active.Position + Vector3.up * 6f;
            if (ComboTimer > 0f)
            {
                ComboTimer -= Time.deltaTime;
                if (ComboTimer <= 0f) Combo = 0;
            }
        }

        void OnDestroy()
        {
            if (Team != null) Team.ActiveChanged -= OnActiveChanged;
            if (Current == this) { Current = null; Obstacles.Clear(); }
            TimeController.ResetAll();
            DamageNumbers.Clear();
            RenderSettings.fog = false;
            if (GameManager.Instance != null) GameManager.Instance.Audio.SetAmbience(null);
        }
    }
}
