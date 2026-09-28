using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// A gold coin (or, sometimes, a diamond) dropped by a defeated demon: it pops out, bounces, then gets pulled
    /// into the active slayer and counts toward the mission's gold or diamonds.
    /// </summary>
    public class GoldCoin : MonoBehaviour
    {
        static Material mat, rimMat, gemMat, gemLight, glowMat, gemGlow;
        Vector3 vel;
        float age, spin;
        int value;
        bool homing, diamond;

        static void EnsureMats()
        {
            if (mat != null) return;
            mat = MaterialFactory.Toon(new Color(1f, 0.8f, 0.22f), 0.015f, new Color(0.5f, 0.34f, 0.03f));
            rimMat = MaterialFactory.Toon(new Color(0.86f, 0.56f, 0.08f), 0.01f, new Color(0.25f, 0.15f, 0f));
            glowMat = MaterialFactory.Additive(new Color(1f, 0.8f, 0.3f, 0.35f));
            gemMat = MaterialFactory.Toon(new Color(0.3f, 0.7f, 1f), 0.01f, new Color(0.1f, 0.35f, 0.8f));
            gemLight = MaterialFactory.Toon(new Color(0.8f, 0.95f, 1f), 0.01f, new Color(0.4f, 0.7f, 1f));
            gemGlow = MaterialFactory.Additive(new Color(0.4f, 0.8f, 1f, 0.45f));
        }

        /// <summary>Drops diamonds: a faceted blue gem with a glow that sparkles and chimes on pickup.</summary>
        public static void Diamonds(Vector3 at, int gems, int valueEach)
        {
            EnsureMats();
            for (int i = 0; i < gems; i++)
            {
                var go = new GameObject("Diamond");
                go.transform.position = at + Vector3.up * 1f;
                MeshFactory.MeshObject(MeshFactory.Cone(), go.transform, new Vector3(0f, 0.12f, 0f), new Vector3(0.5f, 0.22f, 0.5f), gemLight, false);
                var bottom = MeshFactory.MeshObject(MeshFactory.Cone(), go.transform, new Vector3(0f, 0.12f, 0f), new Vector3(0.5f, 0.5f, 0.5f), gemMat, false);
                bottom.transform.localRotation = Quaternion.Euler(180f, 0f, 0f);
                MeshFactory.MeshObject(MeshFactory.SmoothSphere(), go.transform, Vector3.zero, Vector3.one * 1.1f, gemGlow, false);
                var c = go.AddComponent<GoldCoin>();
                c.diamond = true;
                Vector2 r = Random.insideUnitCircle * 2f;
                c.vel = new Vector3(r.x, Random.Range(7f, 9f), r.y);
                c.value = valueEach;
                c.spin = 240f;
                VFX.Pillar(at, new Color(0.45f, 0.8f, 1f), 3f, 0.4f);
            }
            if (GameManager.Instance != null) GameManager.Instance.Audio.PlayPitched("gem", 0.6f, 1f);
        }

        public static void Burst(Vector3 at, int coins, int valueEach)
        {
            EnsureMats();
            if (GameManager.Instance != null) GameManager.Instance.Audio.PlayPitched("coinSpill", 0.55f, Random.Range(0.95f, 1.1f));
            for (int i = 0; i < coins; i++)
            {
                var go = new GameObject("GoldCoin");
                go.transform.position = at + Vector3.up * 0.8f;
                // Big, chunky coins: a thick rim, a raised face with a stamped centre, and a soft glow.
                var rim = MeshFactory.MeshObject(MeshFactory.FacetCylinder(16), go.transform, new Vector3(0f, 0f, -0.06f), new Vector3(0.62f, 0.12f, 0.62f), rimMat, false);
                rim.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                var face = MeshFactory.MeshObject(MeshFactory.FacetCylinder(16), go.transform, new Vector3(0f, 0f, -0.075f), new Vector3(0.52f, 0.15f, 0.52f), mat, false);
                face.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                var stamp = MeshFactory.MeshObject(MeshFactory.FacetCylinder(6), go.transform, new Vector3(0f, 0f, -0.09f), new Vector3(0.22f, 0.18f, 0.22f), rimMat, false);
                stamp.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                MeshFactory.MeshObject(MeshFactory.SmoothSphere(), go.transform, Vector3.zero, Vector3.one * 0.85f, glowMat, false);
                var c = go.AddComponent<GoldCoin>();
                Vector2 r = Random.insideUnitCircle * 3f;
                c.vel = new Vector3(r.x, Random.Range(5f, 8f), r.y);
                c.value = valueEach;
                c.spin = Random.Range(360f, 720f);
            }
        }

        static float lastPickup;
        static int pickupStreak;

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
                if (diamond)
                {
                    if (b.Mission != null) b.Mission.CollectDiamonds(value);
                    // The open world has no results screen: treasure goes straight into the wallet.
                    if (b.Def != null && b.Def.openWorld && GameManager.Instance != null) GameManager.Instance.Data.crystals += value;
                    VFX.HitSpark(transform.position, new Color(0.5f, 0.85f, 1f), 10);
                    DamageNumbers.SpawnText(transform.position + Vector3.up * 0.8f, "+" + value + " DIAMOND" + (value > 1 ? "S" : ""), new Color(0.55f, 0.85f, 1f), 40f);
                    if (GameManager.Instance != null) GameManager.Instance.Audio.PlayPitched("gem", 0.5f, 1.15f);
                }
                else
                {
                    if (b.Mission != null) b.Mission.CollectGold(value);
                    if (b.Def != null && b.Def.openWorld && GameManager.Instance != null) GameManager.Instance.Data.coins += value;
                    VFX.HitSpark(transform.position, new Color(1f, 0.85f, 0.3f), 5);
                    // Pitch climbs a little with each coin in a streak, so a big drop "rings up".
                    pickupStreak = Time.time - lastPickup < 0.4f ? Mathf.Min(pickupStreak + 1, 10) : 0;
                    lastPickup = Time.time;
                    if (GameManager.Instance != null) GameManager.Instance.Audio.PlayPitched("coin", 0.4f, 1f + pickupStreak * 0.035f);
                }
                Destroy(gameObject);
                return;
            }
            transform.position += to.normalized * Mathf.Min(speed * dt, to.magnitude);
        }
    }
}
