using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// Environmental danger in a boss arena that switches on when the boss enrages: lightning on the storm summit,
    /// falling rocks in collapsing halls, fire rain in the demon lands, dark bolts in the throne room.
    /// Every strike is telegraphed on the ground first.
    /// </summary>
    public class ArenaHazard : MonoBehaviour
    {
        public static ArenaHazard Current { get; private set; }

        public string Kind = "rocks";
        public Vector3 Center;
        public float Radius = 16f;
        public bool Active;
        public Combatant Owner;
        float timer = 3f;

        void OnEnable() { Current = this; }
        void OnDisable() { if (Current == this) Current = null; }

        public void Activate(Combatant owner)
        {
            Owner = owner;
            if (Active) return;
            Active = true;
            timer = 1.5f;
            string title = Kind == "lightning" ? "THE STORM BREAKS" : Kind == "fire" ? "THE SKY IS BURNING" : Kind == "dark" ? "THE ECLIPSE SPREADS" : "THE ARENA IS COLLAPSING";
            GameEvents.RaiseBanner(title, "Watch the ground for strikes!");
            // The atmosphere closes in.
            RenderSettings.fogStartDistance *= 0.7f;
            RenderSettings.fogEndDistance *= 0.75f;
        }

        void Update()
        {
            if (!Active || TimeController.Paused) return;
            if (Owner == null || !Owner.IsAlive) { Active = false; return; }
            timer -= Time.deltaTime;
            if (timer > 0f) return;
            timer = Random.Range(1.6f, 2.8f);
            var b = BattleController.Current;
            Vector3 target = b != null && b.Team.Active != null ? b.Team.Active.Position : Center;
            Vector3 p = target + new Vector3(Random.Range(-3f, 3f), 0f, Random.Range(-3f, 3f));
            if ((Journey.Flat(p) - Journey.Flat(Center)).magnitude > Radius) p = Center + (Journey.Flat(p) - Journey.Flat(Center)).normalized * Radius;
            StartCoroutine(Strike(p));
        }

        System.Collections.IEnumerator Strike(Vector3 p)
        {
            const float radius = 2.4f;
            var t = Telegraph.Circle(p, radius, 1.1f);
            GameObject rock = null;
            if (Kind == "rocks")
            {
                rock = MeshFactory.Primitive(PrimitiveType.Sphere, transform, p + Vector3.up * 14f, Vector3.one * 1.4f, MaterialFactory.Toon(new Color(0.4f, 0.37f, 0.35f)));
                float e = 0f;
                while (e < 1.1f)
                {
                    e += Time.deltaTime;
                    rock.transform.position = p + Vector3.up * Mathf.Lerp(14f, 0.6f, (e / 1.1f) * (e / 1.1f));
                    yield return null;
                }
            }
            else yield return new WaitForSeconds(1.1f);
            if (t != null) Destroy(t.gameObject);
            var audio = GameManager.Instance != null ? GameManager.Instance.Audio : null;
            switch (Kind)
            {
                case "lightning":
                    VFX.Pillar(p, new Color(0.8f, 0.9f, 1f), 30f, 0.2f);
                    VFX.ImpactLight(p + Vector3.up * 4f, new Color(0.7f, 0.85f, 1f), 18f, 0.25f);
                    if (audio != null) audio.Play("thud", 0.9f);
                    break;
                case "fire":
                    VFX.Pillar(p, new Color(1f, 0.45f, 0.1f), 12f, 0.4f);
                    VFX.Breath(p, new Color(1f, 0.4f, 0.05f), 25);
                    if (audio != null) audio.Play("slam", 0.8f);
                    break;
                case "dark":
                    VFX.Pillar(p, new Color(0.7f, 0.1f, 0.4f), 14f, 0.4f);
                    VFX.Shockwave(p, radius, new Color(0.8f, 0.1f, 0.4f), 0.3f);
                    if (audio != null) audio.Play("slam", 0.8f);
                    break;
                default:
                    // A boulder falls from the collapsing ceiling/cliff.
                    VFX.Dust(p, 16);
                    if (audio != null) audio.Play("thud", 0.9f);
                    Destroy(rock, 3f);
                    break;
            }
            if (CameraController.Instance != null) CameraController.Instance.Shake(0.25f);
            if (Owner != null)
            {
                var tag = AttackTag.Basic(1.6f, Color.white);
                tag.knockback = 5f;
                tag.stagger = 3f;
                CombatSystem.HitRadius(Owner, p, radius, tag);
            }
        }
    }
}
