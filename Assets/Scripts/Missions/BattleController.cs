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

        public static Vector3 ClampToArena(Vector3 p)
        {
            p.y = 0f;
            float m = new Vector2(p.x, p.z).magnitude;
            if (m > ArenaRadius) p *= ArenaRadius / m;
            return p;
        }

        public void Setup(MissionDefinition def, PlayerData data)
        {
            Current = this;
            Def = def;
            TimeController.ResetAll();
            EnemyController.ResetTokens();
            DamageNumbers.Clear();

            ArenaBuilder.Build(def.theme, transform);

            var teamGo = new GameObject("Team");
            teamGo.transform.SetParent(transform, false);
            Team = teamGo.AddComponent<TeamSystem>();
            Team.Setup(data, new Vector3(0f, 0f, -6f));
            Team.ActiveChanged += OnActiveChanged;

            Controller = gameObject.AddComponent<PlayerController>();
            Controller.Team = Team;

            if (CameraController.Instance != null && Team.Active != null)
                CameraController.Instance.Follow(Team.Active.transform, true);

            Mission = gameObject.AddComponent<MissionSystem>();
            Mission.Begin(def, this);
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
            if (ComboTimer > 0f)
            {
                ComboTimer -= Time.deltaTime;
                if (ComboTimer <= 0f) Combo = 0;
            }
        }

        void OnDestroy()
        {
            if (Team != null) Team.ActiveChanged -= OnActiveChanged;
            if (Current == this) Current = null;
            TimeController.ResetAll();
            DamageNumbers.Clear();
            RenderSettings.fog = false;
        }
    }
}
