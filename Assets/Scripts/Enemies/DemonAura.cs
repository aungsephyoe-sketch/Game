using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// Makes demons read as dangerous at a glance: a pulsing ring of their colour on the ground (bigger and
    /// brighter for elites, tanks and bosses), embers drifting up off them, and a red-hot glow while they wind
    /// up an attack. Purely visual; cleaned up with the demon.
    /// </summary>
    public class DemonAura : MonoBehaviour
    {
        EnemyController owner;
        Transform ring;
        Material ringMat;
        Color col;
        float baseScale, t, emberTimer;
        bool big;

        public static void Attach(EnemyController e, EnemyDefinition def)
        {
            var a = e.gameObject.AddComponent<DemonAura>();
            a.owner = e;
            a.col = def.accentColor;
            a.big = def.archetype == EnemyArchetype.Elite || def.archetype == EnemyArchetype.Tank || def.archetype == EnemyArchetype.Boss;
            a.baseScale = (def.radius + (a.big ? 1.1f : 0.6f)) * Mathf.Max(0.8f, def.scale * 0.7f);
            a.ringMat = MaterialFactory.Additive(new Color(a.col.r, a.col.g, a.col.b, a.big ? 0.5f : 0.3f));
            var go = MeshFactory.MeshObject(MeshFactory.Ring(0.78f), e.transform, e.transform.position + Vector3.up * 0.05f, Vector3.one * a.baseScale, a.ringMat, false);
            go.name = "DemonAura";
            a.ring = go.transform;
            a.t = Random.Range(0f, 10f);
        }

        void LateUpdate()
        {
            if (owner == null || ring == null) return;
            if (!owner.IsAlive) { ring.gameObject.SetActive(false); enabled = false; return; }
            float dt = Time.deltaTime;
            t += dt;
            bool winding = owner.IsWindingUp;
            float pulse = 0.5f + 0.5f * Mathf.Sin(t * (winding ? 14f : 2.5f));
            // Keep the ring flat on the ground under the demon (not tilting with its body).
            Vector3 p = owner.transform.position;
            ring.position = new Vector3(p.x, Ground.HeightAt(p.x, p.z) + 0.05f, p.z);
            ring.rotation = Quaternion.Euler(0f, t * 25f, 0f);
            ring.localScale = Vector3.one * baseScale * (1f + 0.06f * pulse + (winding ? 0.15f : 0f)) / Mathf.Max(0.01f, owner.transform.lossyScale.x);
            Color c = winding ? Color.Lerp(col, new Color(1f, 0.15f, 0.1f), 0.6f) : col;
            ringMat.color = new Color(c.r, c.g, c.b, (big ? 0.35f : 0.2f) + (big ? 0.25f : 0.15f) * pulse + (winding ? 0.25f : 0f));
            // Embers drifting off it.
            emberTimer -= dt;
            if (emberTimer <= 0f)
            {
                emberTimer = big ? Random.Range(0.25f, 0.5f) : Random.Range(0.6f, 1.2f);
                VFX.Breath(p + new Vector3(Random.Range(-0.4f, 0.4f), -0.6f, Random.Range(-0.4f, 0.4f)), col, big ? 3 : 1);
            }
        }
    }
}
