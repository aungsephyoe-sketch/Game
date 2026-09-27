using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>Dodgeable demon projectile (blood orb). Hits the first player it touches.</summary>
    public class EnemyProjectile : MonoBehaviour
    {
        Combatant owner;
        Vector3 dir;
        float speed, range, travelled;
        AttackTag attackTag;
        Color color;

        public static void Fire(Combatant owner, Vector3 pos, Vector3 dir, float speed, float range, AttackTag tag, Color color, float size = 0.45f)
        {
            dir.y = 0f;
            dir.Normalize();
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            var col = go.GetComponent<Collider>();
            if (col != null) Destroy(col);
            go.name = "EnemyProjectile";
            go.transform.position = pos + Vector3.up * 1.1f;
            go.transform.localScale = Vector3.one * size;
            var mr = go.GetComponent<MeshRenderer>();
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.material = MaterialFactory.Additive(color);
            var p = go.AddComponent<EnemyProjectile>();
            p.owner = owner;
            p.dir = dir;
            p.speed = speed;
            p.range = range;
            p.attackTag = tag;
            p.color = color;
        }

        void Update()
        {
            if (EnemyController.Frozen && owner != null && owner.Team == CombatTeam.Enemy) return;
            float step = speed * Time.deltaTime;
            transform.position += dir * step;
            travelled += step;
            if (Random.value < 0.5f) VFX.Breath(transform.position - Vector3.up, color, 1);

            if (owner != null)
            {
                Vector3 ground = new Vector3(transform.position.x, 0f, transform.position.z);
                var hits = CombatSystem.Query(owner, ground, dir, transform.localScale.x * 0.8f, 360f);
                if (hits.Count > 0)
                {
                    var target = hits[0];
                    // Projectiles pass through a dodging player (and still trigger perfect dodge).
                    CombatSystem.ApplyHit(owner, target, attackTag, ground - dir);
                    if (!target.Health.Invulnerable)
                    {
                        Destroy(gameObject);
                        return;
                    }
                }
            }
            if (travelled > range) Destroy(gameObject);
        }

        void OnDestroy()
        {
            var mr = GetComponent<MeshRenderer>();
            if (mr != null && mr.material != null) Destroy(mr.material);
        }
    }
}
