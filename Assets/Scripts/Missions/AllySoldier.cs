using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// A royal guard fighting beside the team in large battles: picks the nearest demon, closes in and trades
    /// sword blows. Weaker than a slayer, but they soak hits and make sieges feel like wars.
    /// The same brain drives the AI slayers of a co-op party (<see cref="SpawnSlayer"/>): tougher, faster,
    /// with a spinning special every few seconds in their element's colour.
    /// </summary>
    public class AllySoldier : Combatant
    {
        public string DisplayName = "";
        public bool IsSlayer { get; private set; }
        Color tint = new Color(0.7f, 0.8f, 1f);
        float specialTimer;
        CharacterVisual visual;
        float swingTimer;
        float retarget;
        Combatant target;
        Vector3 home;

        public static AllySoldier Spawn(Transform parent, Vector3 pos, int level)
        {
            var go = new GameObject("Royal Guard");
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.AddComponent<HealthSystem>();
            var a = go.AddComponent<AllySoldier>();
            a.Team = CombatTeam.Player;
            a.Element = Element.Water;
            float scale = 1f + level * 0.08f;
            a.Stats = new StatBlock(1800f * scale, 90f * scale, 80f * scale, 0.05f, 0.5f, 3.4f, 0f);
            a.Radius = 0.45f;
            a.Health.Init(a.Stats.hp);
            a.Health.Died += a.OnDied;
            a.visual = CharacterVisual.BuildHero(GameDatabase.GetCharacter("npc_soldier"), go.transform);
            a.home = pos;
            a.swingTimer = Random.Range(0.5f, 1.5f);
            return a;
        }

        /// <summary>A party member from the village gate: a slayer from the roster, played by the AI.</summary>
        public static AllySoldier SpawnSlayer(Transform parent, Vector3 pos, int level, string characterId, string name)
        {
            var def = GameDatabase.GetCharacter(characterId);
            if (def == null) return Spawn(parent, pos, level);
            var go = new GameObject("Party " + name);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.AddComponent<HealthSystem>();
            var a = go.AddComponent<AllySoldier>();
            a.Team = CombatTeam.Player;
            a.Element = def.element;
            a.IsSlayer = true;
            a.DisplayName = name;
            a.tint = ElementChart.ColorOf(def.element);
            float scale = 1f + level * 0.1f;
            a.Stats = new StatBlock(4200f * scale, 230f * scale, 150f * scale, 0.12f, 1.6f, 4.6f, 0f);
            a.Radius = 0.45f;
            a.Health.Init(a.Stats.hp);
            a.Health.Died += a.OnDied;
            a.visual = CharacterVisual.BuildHero(def, go.transform);
            a.home = pos;
            a.swingTimer = Random.Range(0.3f, 0.9f);
            a.specialTimer = Random.Range(5f, 9f);
            VFX.Breath(pos, a.tint, 30);
            return a;
        }

        void Update()
        {
            if (!IsAlive || TimeController.Paused) return;
            float dt = Time.deltaTime;
            retarget -= dt;
            if (target == null || !target.IsAlive || retarget <= 0f)
            {
                target = Nearest(CombatTeam.Enemy, Position, 16f);
                retarget = 1.5f;
            }
            // With nothing to fight, the guard marches alongside the team.
            var lead = BattleController.Current != null && BattleController.Current.Team != null ? BattleController.Current.Team.Active : null;
            if (target == null && lead != null) home = lead.Position + (Position - lead.Position).normalized * 2.5f;
            if (target != null && lead != null && Vector3.Distance(target.Position, lead.Position) > 18f) target = null;
            Vector3 goal = target != null ? target.Position : home;
            Vector3 to = goal - Position;
            to.y = 0f;
            float stop = target != null ? target.Radius + 1.3f : IsSlayer ? 1.2f : 0.5f;
            if (to.magnitude > stop)
            {
                transform.position = BattleController.ClampToArena(Obstacles.Sweep(transform.position, transform.position + to.normalized * Stats.speed * dt));
                visual.SetMoving(1f);
            }
            else visual.SetMoving(0f);
            if (to.sqrMagnitude > 0.01f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(to), dt * 10f);

            swingTimer -= dt;
            specialTimer -= dt;
            if (IsSlayer && target != null && specialTimer <= 0f && to.magnitude <= stop + 1.5f)
            {
                // The party member's special: a spinning sweep that hits everything around them.
                specialTimer = Random.Range(7f, 10f);
                swingTimer = 1.2f;
                visual.Spin(0.4f, 2);
                var sp = AttackTag.Basic(2.6f, tint);
                sp.hitStop = 0f;
                sp.shake = 0f;
                sp.stagger = 3f;
                CombatSystem.HitArc(this, Position, transform.forward, 3.4f, 360f, sp);
                VFX.Shockwave(Position, 3.4f, tint, 0.5f);
                VFX.Breath(Position, tint, 30);
            }
            else if (target != null && swingTimer <= 0f && to.magnitude <= stop + 0.4f)
            {
                swingTimer = IsSlayer ? Random.Range(0.6f, 0.9f) : Random.Range(1.1f, 1.6f);
                visual.Attack(Random.Range(0, 3), 0.18f);
                var tag = AttackTag.Basic(1f, IsSlayer ? tint : new Color(0.6f, 0.75f, 1f));
                tag.hitStop = 0f;
                tag.shake = 0f;
                tag.stagger = 1.5f;
                CombatSystem.HitArc(this, Position, transform.forward, 2.2f, 120f, tag);
                VFX.Slash(Position, transform.forward, 1.8f, 120f, 0f, IsSlayer ? tint : new Color(0.7f, 0.8f, 1f), 0.15f);
            }
        }

        public override void OnHitReceived(DamageInfo info)
        {
            if (visual != null) { visual.Hit(info.knockback); visual.Flash(Color.white, 0.6f); }
            transform.position = BattleController.ClampToArena(Obstacles.Sweep(transform.position, transform.position + info.knockback * 0.1f));
        }

        void OnDied()
        {
            if (visual != null) visual.PlayDeath();
            Destroy(gameObject, 2f);
        }
    }
}
