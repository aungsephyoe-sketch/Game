using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// Destructible crate / barrel. Player attacks smash it into flying debris; sometimes it drops a
    /// wisteria charm that heals the active slayer. Uses its own registry so demons never target it.
    /// </summary>
    public class Breakable : MonoBehaviour
    {
        public static readonly List<Breakable> All = new List<Breakable>();

        public float Radius = 0.6f;
        bool broken;
        Color color;

        public Vector3 Position { get { return transform.position; } }

        public static Breakable Create(Transform parent, Vector3 pos, bool barrel)
        {
            var go = new GameObject(barrel ? "Barrel" : "Crate");
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            var b = go.AddComponent<Breakable>();
            b.color = barrel ? new Color(0.45f, 0.28f, 0.16f) : new Color(0.6f, 0.45f, 0.28f);
            var mat = MaterialFactory.Toon(b.color);
            var band = MaterialFactory.Toon(new Color(0.2f, 0.18f, 0.18f));
            if (barrel)
            {
                MeshFactory.Primitive(PrimitiveType.Cylinder, go.transform, new Vector3(0f, 0.55f, 0f), new Vector3(0.8f, 0.55f, 0.8f), mat);
                MeshFactory.Primitive(PrimitiveType.Cylinder, go.transform, new Vector3(0f, 0.3f, 0f), new Vector3(0.84f, 0.04f, 0.84f), band);
                MeshFactory.Primitive(PrimitiveType.Cylinder, go.transform, new Vector3(0f, 0.8f, 0f), new Vector3(0.84f, 0.04f, 0.84f), band);
            }
            else
            {
                MeshFactory.Primitive(PrimitiveType.Cube, go.transform, new Vector3(0f, 0.45f, 0f), new Vector3(0.9f, 0.9f, 0.9f), mat);
                MeshFactory.Primitive(PrimitiveType.Cube, go.transform, new Vector3(0f, 0.45f, 0f), new Vector3(0.95f, 0.12f, 0.95f), band);
            }
            go.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            return b;
        }

        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); }

        /// <summary>Called by CombatSystem for player attacks. Returns true if it broke.</summary>
        public static void HitInArc(Vector3 origin, Vector3 forward, float range, float arcDegrees)
        {
            if (All.Count == 0) return;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f) forward = Vector3.forward;
            forward.Normalize();
            float halfCos = Mathf.Cos(arcDegrees * 0.5f * Mathf.Deg2Rad);
            for (int i = All.Count - 1; i >= 0; i--)
            {
                var b = All[i];
                if (b == null || b.broken) continue;
                Vector3 to = b.Position - origin;
                to.y = 0f;
                float d = to.magnitude;
                if (d > range + b.Radius) continue;
                if (arcDegrees < 359f && d > b.Radius + 0.3f && Vector3.Dot(forward, to / d) < halfCos) continue;
                b.Break(to.sqrMagnitude > 0.001f ? to.normalized : forward);
            }
        }

        void Break(Vector3 dir)
        {
            broken = true;
            if (GameManager.Instance != null) GameManager.Instance.Audio.Play("thud", 0.6f);
            VFX.Dust(Position, 10);
            VFX.HitSpark(Position + Vector3.up * 0.5f, new Color(1f, 0.85f, 0.6f), 10);
            var debrisMat = MaterialFactory.Toon(color);
            for (int i = 0; i < 7; i++)
            {
                var piece = MeshFactory.Primitive(PrimitiveType.Cube, null, Position + Vector3.up * Random.Range(0.3f, 0.9f),
                    new Vector3(Random.Range(0.12f, 0.35f), Random.Range(0.08f, 0.2f), Random.Range(0.2f, 0.5f)), debrisMat);
                var v = (dir + Random.insideUnitSphere * 0.9f).normalized * Random.Range(3f, 7f);
                v.y = Random.Range(3f, 7f);
                piece.AddComponent<Debris>().Launch(v);
            }
            if (Random.value < 0.35f) HealDrop();
            Destroy(gameObject);
        }

        void HealDrop()
        {
            var b = BattleController.Current;
            if (b == null || b.Team == null || b.Team.Active == null || !b.Team.Active.IsAlive) return;
            var pc = b.Team.Active;
            pc.Health.Heal(pc.Health.Max * 0.08f);
            VFX.Breath(pc.Position, new Color(0.7f, 0.5f, 1f), 25);
            DamageNumbers.SpawnText(pc.Position + Vector3.up * 2.4f, "+HP", new Color(0.5f, 1f, 0.6f), 44f);
        }
    }

    /// <summary>Simple ballistic debris chunk that bounces once and fades.</summary>
    public class Debris : MonoBehaviour
    {
        Vector3 velocity;
        Vector3 spin;
        float life;

        public void Launch(Vector3 v)
        {
            velocity = v;
            spin = Random.insideUnitSphere * 720f;
            life = Random.Range(1.2f, 1.8f);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            velocity.y -= 20f * dt;
            transform.position += velocity * dt;
            transform.Rotate(spin * dt);
            if (transform.position.y < 0.05f)
            {
                var p = transform.position;
                p.y = 0.05f;
                transform.position = p;
                velocity = new Vector3(velocity.x * 0.4f, Mathf.Abs(velocity.y) * 0.3f, velocity.z * 0.4f);
                spin *= 0.5f;
            }
            life -= dt;
            if (life < 0.3f) transform.localScale *= 1f - dt * 4f;
            if (life <= 0f) Destroy(gameObject);
        }
    }
}
