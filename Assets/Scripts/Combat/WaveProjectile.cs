using System.Collections.Generic;
using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>A travelling sword wave that pierces and hits every enemy on its path once.</summary>
    public class WaveProjectile : MonoBehaviour
    {
        Combatant owner;
        Vector3 dir;
        float speed, range, width, travelled;
        AttackTag attackTag;
        DamageTally tally;
        readonly HashSet<Combatant> hit = new HashSet<Combatant>();
        Material mat;

        public static void Launch(Combatant owner, Vector3 pos, Vector3 dir, float speed, float range, float width, AttackTag tag, DamageTally tally)
        {
            dir.y = 0f;
            dir.Normalize();
            var go = new GameObject("Wave");
            go.transform.SetPositionAndRotation(pos + Vector3.up, Quaternion.LookRotation(dir));
            go.transform.localScale = new Vector3(width * 1.3f, 1f, width * 0.9f);
            go.AddComponent<MeshFilter>().sharedMesh = MeshFactory.Sector(150f, 0.6f);
            var mr = go.AddComponent<MeshRenderer>();
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var w = go.AddComponent<WaveProjectile>();
            w.mat = MaterialFactory.Additive(tag.color);
            mr.material = w.mat;
            w.owner = owner;
            w.dir = dir;
            w.speed = speed;
            w.range = range;
            w.width = width;
            w.attackTag = tag;
            w.tally = tally;
            if (GameManager.Instance != null) GameManager.Instance.Audio.Play("wave", 0.7f);
        }

        void Update()
        {
            float step = speed * Time.deltaTime;
            transform.position += dir * step;
            travelled += step;
            if (owner != null)
            {
                Vector3 ground = new Vector3(transform.position.x, 0f, transform.position.z);
                foreach (var c in new List<Combatant>(CombatSystem.Query(owner, ground, dir, width, 360f)))
                {
                    if (!hit.Add(c)) continue;
                    float dmg = CombatSystem.ApplyHit(owner, c, attackTag, ground - dir);
                    if (tally != null) tally.Add(dmg);
                }
            }
            VFX.Breath(transform.position - Vector3.up, attackTag.color, 1);
            if (mat != null)
            {
                var col = attackTag.color;
                col.a = 1f - Mathf.Clamp01(travelled / range) * 0.7f;
                mat.color = col;
            }
            if (travelled >= range || Mathf.Abs(transform.position.x) > 40f || Mathf.Abs(transform.position.z) > 40f) Destroy(gameObject);
        }

        void OnDestroy()
        {
            if (mat != null) Destroy(mat);
        }
    }
}
