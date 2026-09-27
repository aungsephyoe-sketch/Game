using System.Collections.Generic;
using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>A towering wall of water with a white crest that rolls forward, sweeping up every demon it meets.</summary>
    public class TidalWave : MonoBehaviour
    {
        Combatant owner;
        AttackTag hitTag;
        DamageTally tally;
        Vector3 dir;
        float speed, life, t, width;
        readonly HashSet<Combatant> hit = new HashSet<Combatant>();
        Transform crest;

        public static void Launch(Combatant owner, Vector3 from, Vector3 dir, float width, float distance, AttackTag tag, DamageTally tally)
        {
            var go = new GameObject("TidalWave");
            var w = go.AddComponent<TidalWave>();
            w.owner = owner; w.hitTag = tag; w.tally = tally; w.width = width;
            dir.y = 0f;
            w.dir = dir.normalized;
            w.speed = 14f;
            w.life = distance / w.speed;
            go.transform.position = from;
            go.transform.rotation = Quaternion.LookRotation(w.dir);
            var water = MaterialFactory.Transparent(new Color(0.2f, 0.55f, 0.95f, 0.75f));
            var light = MaterialFactory.Additive(new Color(0.5f, 0.85f, 1f, 0.4f));
            var foam = MaterialFactory.Toon(new Color(0.95f, 0.98f, 1f), 0.01f, new Color(0.3f, 0.35f, 0.4f));
            MeshFactory.MeshObject(MeshFactory.SmoothSphere(), go.transform, Vector3.up * 0.9f, new Vector3(width, 3.2f, 2.2f), water, false);
            MeshFactory.MeshObject(MeshFactory.SmoothSphere(), go.transform, Vector3.up * 1f, new Vector3(width * 1.08f, 3.6f, 2.6f), light, false);
            w.crest = new GameObject("Crest").transform;
            w.crest.SetParent(go.transform, false);
            w.crest.localPosition = new Vector3(0f, 2.4f, 0.5f);
            int n = Mathf.RoundToInt(width * 1.5f);
            for (int i = 0; i < n; i++)
                MeshFactory.MeshObject(MeshFactory.SmoothSphere(), w.crest, new Vector3(-width * 0.45f + i * width * 0.9f / Mathf.Max(1, n - 1), Random.Range(-0.1f, 0.2f), Random.Range(0f, 0.3f)), Vector3.one * Random.Range(0.5f, 0.8f), foam);
        }

        void Update()
        {
            t += Time.deltaTime;
            transform.position += dir * speed * Time.deltaTime;
            // Rise up out of the ground, then crash down at the end.
            float grow = Mathf.Clamp01(t / 0.25f) * (1f - Mathf.Clamp01((t - life + 0.25f) / 0.25f));
            transform.localScale = new Vector3(1f, Mathf.Max(0.05f, grow), 1f);
            crest.localRotation = Quaternion.Euler(Mathf.Sin(t * 10f) * 6f, 0f, 0f);
            if (Random.value < 0.8f) ElementFx.Stream(transform.position + transform.right * Random.Range(-width * 0.5f, width * 0.5f) + Vector3.up * 2.5f, dir + Vector3.up, Element.Water, 3);
            if (owner != null) tally.Add(CombatSystem.HitArc(owner, transform.position - dir * 0.5f, dir, 2.5f, 180f, hitTag, hit));
            foreach (var c in hit) if (c != null && c.IsAlive) c.transform.position += dir * speed * 0.6f * Time.deltaTime;
            if (t >= life) Destroy(gameObject);
        }
    }
}
