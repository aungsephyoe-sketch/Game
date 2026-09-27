using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// A gold coin dropped by a defeated demon: it pops out, bounces, then gets pulled into the active slayer
    /// and counts toward the mission's gold.
    /// </summary>
    public class GoldCoin : MonoBehaviour
    {
        static Material mat, rimMat;
        Vector3 vel;
        float age, spin;
        int value;
        bool homing;

        public static void Burst(Vector3 at, int coins, int valueEach)
        {
            if (mat == null)
            {
                mat = MaterialFactory.Toon(new Color(1f, 0.78f, 0.2f), 0.015f, new Color(0.45f, 0.3f, 0.02f));
                rimMat = MaterialFactory.Toon(new Color(0.85f, 0.55f, 0.08f), 0.01f);
            }
            for (int i = 0; i < coins; i++)
            {
                var go = new GameObject("GoldCoin");
                go.transform.position = at + Vector3.up * 0.8f;
                var face = MeshFactory.MeshObject(MeshFactory.FacetCylinder(12), go.transform, Vector3.zero, new Vector3(0.32f, 0.05f, 0.32f), mat, false);
                face.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                var rim = MeshFactory.MeshObject(MeshFactory.FacetCylinder(12), go.transform, new Vector3(0f, 0f, -0.001f), new Vector3(0.36f, 0.04f, 0.36f), rimMat, false);
                rim.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                var c = go.AddComponent<GoldCoin>();
                Vector2 r = Random.insideUnitCircle * 3f;
                c.vel = new Vector3(r.x, Random.Range(5f, 8f), r.y);
                c.value = valueEach;
                c.spin = Random.Range(360f, 720f);
            }
        }

        void Update()
        {
            float dt = Time.deltaTime;
            age += dt;
            transform.Rotate(0f, spin * dt, 0f, Space.World);
            var b = BattleController.Current;
            var target = b != null && b.Team != null ? b.Team.Active : null;
            if (target == null || b.Finished) { Destroy(gameObject, 0.1f); return; }
            if (!homing)
            {
                vel += Vector3.down * 20f * dt;
                transform.position += vel * dt;
                if (transform.position.y < 0.2f)
                {
                    var p = transform.position;
                    p.y = 0.2f;
                    transform.position = p;
                    vel = new Vector3(vel.x * 0.5f, -vel.y * 0.4f, vel.z * 0.5f);
                }
                if (age > 0.7f) homing = true;
                return;
            }
            // Sucked into the slayer.
            Vector3 to = target.Position + Vector3.up * 1f - transform.position;
            float speed = 6f + (age - 0.7f) * 30f;
            if (to.magnitude < 0.5f)
            {
                if (b.Mission != null) b.Mission.CollectGold(value);
                VFX.HitSpark(transform.position, new Color(1f, 0.85f, 0.3f), 3);
                if (GameManager.Instance != null) GameManager.Instance.Audio.PlayPitched("coin", 0.25f, Random.Range(1.1f, 1.4f));
                Destroy(gameObject);
                return;
            }
            transform.position += to.normalized * Mathf.Min(speed * dt, to.magnitude);
        }
    }
}
