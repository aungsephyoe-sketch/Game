using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// A royal guard fighting beside the team in large battles: picks the nearest demon, closes in and trades
    /// sword blows. Weaker than a slayer, but they soak hits and make sieges feel like wars.
    /// </summary>
    public class AllySoldier : Combatant
    {
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

        void Update()
        {
            if (!IsAlive || TimeController.Paused) return;
            float dt = Time.deltaTime;
            retarget -= dt;
            if (target == null || !target.IsAlive || retarget <= 0f)
            {
                target = Nearest(CombatTeam.Enemy, Position, 30f);
                retarget = 1.5f;
            }
            Vector3 goal = target != null ? target.Position : home;
            Vector3 to = goal - Position;
            to.y = 0f;
            float stop = target != null ? target.Radius + 1.3f : 0.5f;
            if (to.magnitude > stop)
            {
                transform.position = BattleController.ClampToArena(transform.position + to.normalized * Stats.speed * dt);
                visual.SetMoving(1f);
            }
            else visual.SetMoving(0f);
            if (to.sqrMagnitude > 0.01f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(to), dt * 10f);

            swingTimer -= dt;
            if (target != null && swingTimer <= 0f && to.magnitude <= stop + 0.4f)
            {
                swingTimer = Random.Range(1.1f, 1.6f);
                visual.Attack(Random.Range(0, 3), 0.18f);
                var tag = AttackTag.Basic(1f, new Color(0.6f, 0.75f, 1f));
                tag.hitStop = 0f;
                tag.shake = 0f;
                tag.stagger = 1.5f;
                CombatSystem.HitArc(this, Position, transform.forward, 2.2f, 120f, tag);
                VFX.Slash(Position, transform.forward, 1.8f, 120f, 0f, new Color(0.7f, 0.8f, 1f), 0.15f);
            }
        }

        public override void OnHitReceived(DamageInfo info)
        {
            if (visual != null) { visual.Hit(info.knockback); visual.Flash(Color.white, 0.6f); }
            transform.position = BattleController.ClampToArena(transform.position + info.knockback * 0.1f);
        }

        void OnDied()
        {
            if (visual != null) visual.PlayDeath();
            Destroy(gameObject, 2f);
        }
    }
}
