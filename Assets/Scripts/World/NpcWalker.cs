using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// Ambient townsfolk: wander between points inside a ring, pause, look around, and occasionally say
    /// something (shown as a speech bubble by the UI). Keeps towns and the home screen feeling alive.
    /// </summary>
    public class NpcWalker : MonoBehaviour
    {
        public static readonly System.Collections.Generic.List<NpcWalker> All = new System.Collections.Generic.List<NpcWalker>();

        public Vector3 Center;
        public float MinRadius = 17f;
        public float MaxRadius = 21f;
        public float Speed = 1.4f;
        public string[] Lines;
        public string SpeakerName = "";

        /// <summary>When set, the walker keeps close behind this (a party member following you round the village).</summary>
        public Transform FollowTarget;
        public Vector3 FollowOffset = new Vector3(0f, 0f, -2f);

        public string CurrentLine { get; private set; }
        public float LineUntil { get; private set; }

        CharacterVisual visual;
        Vector3 target;
        float wait;
        float talkTimer;

        public static NpcWalker Spawn(Transform parent, string npcId, Vector3 center, float minR, float maxR, string[] lines, float scale = 1f)
        {
            var def = GameDatabase.GetCharacter(npcId);
            if (def == null) return null;
            var go = new GameObject("NPC_" + def.displayName);
            go.transform.SetParent(parent, false);
            var w = go.AddComponent<NpcWalker>();
            w.Center = center;
            w.MinRadius = minR;
            w.MaxRadius = maxR;
            w.Lines = lines;
            w.SpeakerName = def.displayName;
            w.visual = CharacterVisual.BuildHero(def, go.transform);
            go.transform.localScale = Vector3.one * scale;
            go.transform.position = w.RandomPoint();
            w.PickTarget();
            w.talkTimer = Random.Range(3f, 12f);
            return w;
        }

        /// <summary>Talked to by a slayer: stop, turn to face them and say something.</summary>
        public void Say(Vector3 listener)
        {
            if (Lines == null || Lines.Length == 0) return;
            CurrentLine = Lines[Random.Range(0, Lines.Length)];
            LineUntil = Time.unscaledTime + 4f;
            talkTimer = Random.Range(10f, 20f);
            wait = 4f;
            Vector3 d = listener - transform.position;
            d.y = 0f;
            if (d.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(d);
        }

        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); }

        Vector3 RandomPoint()
        {
            float a = Random.Range(0f, Mathf.PI * 2f);
            float r = Random.Range(MinRadius, MaxRadius);
            return Center + new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
        }

        void PickTarget()
        {
            // Mostly short hops so walkers stroll rather than sprint across the map.
            Vector3 p = RandomPoint();
            target = Vector3.Lerp(transform.position, p, 0.5f);
            target = Center + (target - Center).normalized * Mathf.Clamp((target - Center).magnitude, MinRadius, MaxRadius);
            target.y = Center.y;
        }

        /// <summary>Stops following and strolls from wherever it now stands.</summary>
        public void StopFollowing()
        {
            FollowTarget = null;
            Center = transform.position;
            MinRadius = 1f;
            MaxRadius = 5f;
            PickTarget();
        }

        void Update()
        {
            talkTimer -= Time.deltaTime;
            if (talkTimer <= 0f && Lines != null && Lines.Length > 0)
            {
                CurrentLine = Lines[Random.Range(0, Lines.Length)];
                LineUntil = Time.unscaledTime + 3.5f;
                talkTimer = Random.Range(8f, 18f);
            }

            if (FollowTarget != null)
            {
                Vector3 goal = FollowTarget.position + FollowTarget.rotation * FollowOffset;
                Vector3 d = goal - transform.position;
                d.y = 0f;
                float dist = d.magnitude;
                if (dist > 0.35f)
                {
                    float sp = Mathf.Clamp(dist * 2.2f, 1.5f, 7f);
                    Vector3 next = transform.position + d / dist * Mathf.Min(dist, sp * Time.deltaTime);
                    var bc = BattleController.Current;
                    if (bc != null && bc.Def != null && bc.Def.openWorld) next = OpenWorldBuilder.Clamp(next);
                    transform.position = next;
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(d), Time.deltaTime * 8f);
                    visual.SetMoving(Mathf.Clamp01(sp / 5f));
                }
                else
                {
                    visual.SetMoving(0f);
                    transform.rotation = Quaternion.Slerp(transform.rotation, FollowTarget.rotation, Time.deltaTime * 4f);
                }
                if (dist > 14f) transform.position = goal;
                return;
            }
            if (wait > 0f)
            {
                wait -= Time.deltaTime;
                visual.SetMoving(0f);
                transform.rotation = Quaternion.Slerp(transform.rotation, transform.rotation * Quaternion.Euler(0f, Mathf.Sin(Time.time) * 30f * Time.deltaTime, 0f), 1f);
                return;
            }
            Vector3 to = target - transform.position;
            to.y = 0f;
            if (to.magnitude < 0.3f)
            {
                wait = Random.Range(1.5f, 5f);
                PickTarget();
                return;
            }
            Vector3 dir = to.normalized;
            transform.position += dir * Speed * Time.deltaTime;
            // In the open world, villagers walk around houses and trees instead of through them.
            var b = BattleController.Current;
            if (b != null && b.Def != null && b.Def.openWorld)
            {
                Vector3 before = transform.position;
                transform.position = OpenWorldBuilder.Clamp(before);
                if ((transform.position - before).sqrMagnitude > 0.0001f) { wait = 0.5f; PickTarget(); }
            }
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 5f);
            visual.SetMoving(0.5f);
        }
    }
}
