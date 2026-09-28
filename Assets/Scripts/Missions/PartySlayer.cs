using System.Collections.Generic;
using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// A co-op party member played by the AI, meant to feel like another person playing:
    ///   • Its own play style, from its slayer: a Vanguard dives into the biggest crowd and slams, a Duelist chains
    ///     fast combos and flash-steps through lines of demons, a Skirmisher keeps its distance and throws crescent
    ///     waves, a Support stays near the team and heals. Two party members never share a style.
    ///   • Regular three-hit combos with little pauses, a special every few seconds and a big ultimate, shouted
    ///     in the chat the way players do.
    ///   • It picks its own fights (the demon nearest to it, the one hurting you, the weakest), dodges out of red
    ///   warning zones, keeps its own space instead of stacking on the other party member, wanders ahead or lags
    ///   behind when nothing is happening, and only jogs back when it has drifted far away.
    ///   • When it goes down it says so, and gets back up a few seconds later.
    /// </summary>
    public class PartySlayer : Combatant
    {
        public enum Style { Vanguard, Duelist, Skirmisher, Support }

        public static readonly List<PartySlayer> Party = new List<PartySlayer>();

        public string DisplayName = "";
        public Style PlayStyle { get; private set; }
        public string Bubble { get; private set; }
        public float BubbleUntil { get; private set; }
        public bool Down { get; private set; }

        CharacterVisual visual;
        CharacterDefinition def;
        Color tint;
        Combatant target;
        Vector3 roam;
        float roamTimer, retarget, swingTimer, specialTimer, ultTimer, dodgeTimer, idleTalk, downTimer, reaction;
        int comboStep;
        float slotAngle;
        float aggression, skill;

        static readonly string[] Specials = { "Ground Breaker", "Flash Step", "Crescent Volley", "Healing Bloom" };
        static readonly string[] Ultimates = { "Titan Fall", "Thousand Cuts", "Starfall Barrage", "Sanctuary" };

        public static PartySlayer Spawn(Transform parent, Vector3 pos, int level, string characterId, string name, int index, Style? avoid)
        {
            var d = GameDatabase.GetCharacter(characterId);
            if (d == null) return null;
            var go = new GameObject("Party " + name);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.AddComponent<HealthSystem>();
            var a = go.AddComponent<PartySlayer>();
            a.def = d;

            a.Team = CombatTeam.Player;
            a.Element = d.element;
            a.DisplayName = name;
            a.tint = ElementChart.ColorOf(d.element);
            a.PlayStyle = StyleFor(d);
            if (avoid.HasValue && a.PlayStyle == avoid.Value) a.PlayStyle = (Style)(((int)a.PlayStyle + 1 + Random.Range(0, 3)) % 4);
            if (avoid.HasValue && a.PlayStyle == avoid.Value) a.PlayStyle = (Style)(((int)a.PlayStyle + 1) % 4);
            float scale = 1f + level * 0.1f;
            float hp = a.PlayStyle == Style.Vanguard ? 5600f : a.PlayStyle == Style.Skirmisher ? 3400f : 4200f;
            float atk = a.PlayStyle == Style.Duelist ? 250f : a.PlayStyle == Style.Support ? 170f : 220f;
            float spd = a.PlayStyle == Style.Duelist ? 5.2f : a.PlayStyle == Style.Vanguard ? 4.2f : 4.7f;
            a.Stats = new StatBlock(hp * scale, atk * scale, 150f * scale, 0.12f, 1.6f, spd, 0f);
            a.Radius = 0.45f;
            a.Health.Init(a.Stats.hp);
            a.Health.Died += a.OnDied;
            a.visual = CharacterVisual.BuildHero(d, go.transform);
            // Personality: some players are reckless, some careful; some are simply better.
            a.aggression = Random.Range(0.3f, 1f);
            a.skill = Random.Range(0.45f, 0.95f);
            a.slotAngle = index == 0 ? -70f : 70f;
            a.swingTimer = Random.Range(0.3f, 0.9f);
            a.specialTimer = Random.Range(4f, 8f);
            a.ultTimer = Random.Range(20f, 30f);
            a.idleTalk = Random.Range(6f, 14f);
            a.roam = pos;
            VFX.Breath(pos, a.tint, 30);
            return a;
        }

        static Style StyleFor(CharacterDefinition d)
        {
            if (d.style == CombatStyle.Healer || d.role == Role.Support) return Style.Support;
            if (d.style == CombatStyle.Ranged || d.weapon == WeaponKind.Bow || d.weapon == WeaponKind.Staff || d.weapon == WeaponKind.Fans) return Style.Skirmisher;
            if (d.role == Role.Tank || d.style == CombatStyle.Heavy || d.weapon == WeaponKind.Greatsword || d.weapon == WeaponKind.SwordShield) return Style.Vanguard;
            return Style.Duelist;
        }

        protected override void OnEnable() { base.OnEnable(); Party.Add(this); }
        protected override void OnDisable() { base.OnDisable(); Party.Remove(this); }

        PlayerCharacter Lead
        {
            get
            {
                var b = BattleController.Current;
                return b != null && b.Team != null ? b.Team.Active : null;
            }
        }

        public void Say(string text, float seconds = 3.2f)
        {
            Bubble = ChatBrain.Casual(text, skill);
            BubbleUntil = Time.time + seconds;
        }

        void Update()
        {
            if (TimeController.Paused) return;
            float dt = Time.deltaTime;
            if (Down)
            {
                downTimer -= dt;
                if (downTimer <= 0f) GetUp();
                return;
            }
            if (!IsAlive) return;
            var lead = Lead;
            retarget -= dt; swingTimer -= dt; specialTimer -= dt; ultTimer -= dt; dodgeTimer -= dt; idleTalk -= dt; reaction -= dt;

            // 1. Danger first: step out of a red zone (better players react sooner).
            if (dodgeTimer <= 0f)
                foreach (var t in Telegraph.Active)
                {
                    if (t == null || !t.Threatens(Position, 0.4f)) continue;
                    if (Random.value > skill) { dodgeTimer = 0.5f; break; }
                    Vector3 away = Position - t.DangerCenter;
                    away.y = 0f;
                    if (away.sqrMagnitude < 0.01f) away = -transform.forward;
                    Vector3 to2 = Position + away.normalized * (t.DangerRadius - away.magnitude + 1.6f);
                    transform.position = BattleController.ClampToArena(Obstacles.Sweep(transform.position, to2));
                    VFX.Dust(Position, 6);
                    dodgeTimer = 1.1f;
                    if (Random.value < 0.25f) Say(Pick("woah", "close one", "nope nope", "dodged it :)"));
                    return;
                }

            // 2. Choose a fight.
            if (target == null || !target.IsAlive || retarget <= 0f)
            {
                retarget = Random.Range(1.2f, 2.4f);
                target = ChooseTarget(lead);
            }
            if (target != null && lead != null && Vector3.Distance(target.Position, lead.Position) > 22f) target = null;

            // 3. Support: heal when the team is hurt.
            if (PlayStyle == Style.Support && specialTimer <= 0f && TeamHurt(lead)) { Heal(false); return; }

            Vector3 goal;
            float stop;
            if (target != null)
            {
                Vector3 fromT = Position - target.Position;
                fromT.y = 0f;
                if (fromT.sqrMagnitude < 0.01f) fromT = Quaternion.Euler(0f, slotAngle, 0f) * Vector3.back;
                float want = PlayStyle == Style.Skirmisher ? 6.5f : target.Radius + 1.3f;
                // Stand on its own side of the demon, never on top of the other party member.
                Vector3 slot = Quaternion.Euler(0f, slotAngle * 0.35f, 0f) * fromT.normalized;
                goal = target.Position + slot * want;
                stop = 0.35f;
            }
            else
            {
                // Nothing to fight: wander around the team rather than glue to the player.
                roamTimer -= dt;
                if (lead != null && (roamTimer <= 0f || (roam - lead.Position).magnitude > 11f))
                {
                    roamTimer = Random.Range(2.5f, 6f);
                    Vector3 fwd = lead.transform.forward;
                    fwd.y = 0f;
                    float ang = Random.Range(-100f, 100f) + slotAngle * 0.4f;
                    roam = lead.Position + Quaternion.Euler(0f, ang, 0f) * fwd.normalized * Random.Range(3.5f, 8f);
                }
                goal = roam;
                stop = 0.6f;
                if (idleTalk <= 0f)
                {
                    idleTalk = Random.Range(10f, 22f);
                    if (Random.value < 0.6f) Say(Pick(IdleLines));
                }
            }
            Vector3 to = goal - Position;
            to.y = 0f;
            // Keep apart from the other party member.
            foreach (var o in Party)
            {
                if (o == this || o.Down) continue;
                Vector3 sep = Position - o.Position;
                sep.y = 0f;
                if (sep.magnitude < 1.8f) to += sep.normalized * (1.8f - sep.magnitude) * 3f;
            }
            if (lead != null)
            {
                Vector3 sep = Position - lead.Position;
                sep.y = 0f;
                if (sep.magnitude < 1.2f) to += sep.normalized * (1.2f - sep.magnitude) * 3f;
            }
            float dist = to.magnitude;
            bool far = lead != null && (Position - lead.Position).magnitude > 12f;
            if (dist > stop)
            {
                float speed = Stats.speed * (target == null && !far ? 0.55f : 1f) * (far ? 1.35f : 1f);
                transform.position = BattleController.ClampToArena(Obstacles.Sweep(transform.position, transform.position + to.normalized * Mathf.Min(dist, speed * dt)));
                visual.SetMoving(target == null && !far ? 0.5f : 1f);
            }
            else visual.SetMoving(0f);
            Vector3 face = target != null ? target.Position - Position : to;
            face.y = 0f;
            if (face.sqrMagnitude > 0.01f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(face), dt * 10f);

            if (target == null) return;
            float td = (target.Position - Position).magnitude;
            // 4. Ultimate, special, combo.
            if (ultTimer <= 0f && td < 7f && Random.value < 0.02f + aggression * 0.03f) { Ultimate(); return; }
            if (specialTimer <= 0f && (PlayStyle == Style.Skirmisher ? td < 10f : td < 4.5f)) { Special(); return; }
            float reach = PlayStyle == Style.Skirmisher ? 9f : target.Radius + 1.9f;
            if (swingTimer <= 0f && td <= reach) Swing();
        }

        Combatant ChooseTarget(PlayerCharacter lead)
        {
            Combatant best = null;
            float bestScore = float.MaxValue;
            foreach (var c in All)
            {
                if (c == null || !c.IsAlive || c.Team != CombatTeam.Enemy) continue;
                float dSelf = (c.Position - Position).magnitude;
                if (dSelf > 18f) continue;
                float score = dSelf;
                if (lead != null)
                {
                    float dLead = (c.Position - lead.Position).magnitude;
                    // Vanguards and supports protect the player; duelists go for the weakest.
                    if (PlayStyle == Style.Vanguard || PlayStyle == Style.Support) score = Mathf.Min(score, dLead * 0.8f);
                    if (dLead > 20f) continue;
                }
                if (PlayStyle == Style.Duelist && c.Health != null) score *= 0.5f + c.Health.Normalized;
                score += Random.Range(0f, 2.5f) * (1f - skill);
                if (score < bestScore) { bestScore = score; best = c; }
            }
            return best;
        }

        bool TeamHurt(PlayerCharacter lead)
        {
            if (lead != null && lead.Health != null && lead.Health.Normalized < 0.7f) return true;
            foreach (var o in Party) if (o != null && !o.Down && o.Health.Normalized < 0.6f) return true;
            return false;
        }

        void Swing()
        {
            int step = comboStep % 3;
            comboStep++;
            bool finisher = step == 2;
            float gap = PlayStyle == Style.Duelist ? 0.32f : PlayStyle == Style.Vanguard ? 0.62f : 0.5f;
            swingTimer = finisher ? Random.Range(0.8f, 1.4f) * (1.4f - aggression * 0.5f) : gap;
            if (PlayStyle == Style.Skirmisher)
            {
                // A thrown crescent that flies to the demon.
                visual.Attack(step, 0.2f);
                Vector3 fwd = transform.forward;
                var tag = AttackTag.Basic(finisher ? 1.3f : 0.9f, tint);
                tag.hitStop = 0f; tag.shake = 0f; tag.stagger = 1f;
                CombatSystem.HitArc(this, Position, fwd, 9f, 16f, tag);
                VFX.Flash(MeshFactory.Line(), Position + Vector3.up, Quaternion.LookRotation(fwd), new Vector3(0.6f, 1f, 1f), new Vector3(0.2f, 1f, 9f), tint, 0.25f);
                return;
            }
            if (finisher && PlayStyle == Style.Vanguard) visual.HeavyAttack(0.28f);
            else visual.Attack(step, 0.18f);
            var t2 = AttackTag.Basic(finisher ? 1.6f : 1f, tint);
            t2.hitStop = 0f; t2.shake = 0f; t2.stagger = finisher ? 2.5f : 1.2f;
            float arc = PlayStyle == Style.Vanguard ? 160f : 120f;
            CombatSystem.HitArc(this, Position, transform.forward, 2.4f, arc, t2);
            VFX.Slash(Position, transform.forward, 2f, arc, step == 1 ? 25f : -15f, tint, 0.16f);
        }

        void Special()
        {
            specialTimer = Random.Range(6f, 9.5f) * (1.3f - skill * 0.4f);
            swingTimer = 0.9f;
            if (Random.value < 0.55f) Say(Specials[(int)PlayStyle] + "!!", 2f);
            var tag = AttackTag.Basic(2.4f, tint);
            tag.hitStop = 0f; tag.shake = 0f; tag.stagger = 3f;
            switch (PlayStyle)
            {
                case Style.Vanguard:
                {
                    // Leap into the crowd and slam.
                    Vector3 land = target != null ? target.Position : Position + transform.forward * 3f;
                    transform.position = BattleController.ClampToArena(Obstacles.Sweep(transform.position, land - (land - Position).normalized * 1.2f));
                    visual.HeavyAttack(0.3f);
                    CombatSystem.HitRadius(this, Position, 3.6f, tag);
                    VFX.Shockwave(Position, 3.6f, tint, 0.45f);
                    VFX.Dust(Position, 14);
                    break;
                }
                case Style.Duelist:
                {
                    // Flash-step through the line of demons.
                    Vector3 dir = transform.forward;
                    Vector3 from = Position;
                    Vector3 end = BattleController.ClampToArena(Obstacles.Sweep(from, from + dir * 6f));
                    visual.DashAttack(0.2f);
                    CombatSystem.HitArc(this, from, dir, 6.5f, 30f, tag);
                    VFX.Flash(MeshFactory.Line(), from + Vector3.up, Quaternion.LookRotation(dir), new Vector3(1.2f, 1f, 1f), new Vector3(0.05f, 1f, 6.5f), tint, 0.3f);
                    transform.position = end;
                    break;
                }
                case Style.Skirmisher:
                {
                    // Three crescents in a fan.
                    visual.HeavyAttack(0.25f);
                    for (int k = -1; k <= 1; k++)
                    {
                        Vector3 dir = Quaternion.Euler(0f, k * 18f, 0f) * transform.forward;
                        CombatSystem.HitArc(this, Position, dir, 10f, 14f, tag);
                        VFX.Flash(MeshFactory.Line(), Position + Vector3.up, Quaternion.LookRotation(dir), new Vector3(0.8f, 1f, 1f), new Vector3(0.2f, 1f, 10f), tint, 0.3f);
                    }
                    break;
                }
                default:
                    Heal(false);
                    break;
            }
        }

        void Heal(bool big)
        {
            specialTimer = Random.Range(7f, 10f);
            visual.Victory();
            float frac = big ? 0.35f : 0.14f;
            var b = BattleController.Current;
            if (b != null && b.Team != null)
                foreach (var m in b.Team.Members)
                    if (m != null && m.IsAlive && (m.Position - Position).magnitude < 10f) m.Health.Heal(m.Health.Max * frac);
            foreach (var o in Party) if (o != null && !o.Down) o.Health.Heal(o.Health.Max * frac);
            VFX.BurstDisc(Position, big ? 9f : 7f, new Color(0.5f, 1f, 0.6f), 0.6f);
            VFX.Breath(Position, new Color(0.5f, 1f, 0.6f), 40);
            if (Random.value < 0.5f) Say(Pick("healing!!", "heals up", "stay close, healing", "got u"));
        }

        void Ultimate()
        {
            ultTimer = Random.Range(24f, 34f);
            specialTimer = Mathf.Max(specialTimer, 3f);
            swingTimer = 1.4f;
            Say(Ultimates[(int)PlayStyle].ToUpper() + "!!!", 2.4f);
            if (PlayStyle == Style.Support) { Heal(true); return; }
            var tag = AttackTag.Basic(5f, tint);
            tag.hitStop = 0.04f; tag.shake = 0.3f; tag.stagger = 5f;
            visual.Spin(0.5f, 2);
            CombatSystem.HitRadius(this, Position, 5.5f, tag);
            VFX.Pillar(Position, tint, 7f, 0.8f);
            VFX.Shockwave(Position, 5.5f, tint, 0.6f);
            VFX.ImpactLight(Position + Vector3.up, tint, 9f, 0.6f);
            if (GameManager.Instance != null) GameManager.Instance.Audio.Play("ultimate", 0.35f);
        }

        public override void OnHitReceived(DamageInfo info)
        {
            if (visual != null) { visual.Hit(info.knockback); visual.Flash(Color.white, 0.6f); }
            transform.position = BattleController.ClampToArena(Obstacles.Sweep(transform.position, transform.position + info.knockback * 0.1f));
            if (Health.Normalized < 0.3f && Random.value < 0.3f) Say(Pick("low hp help", "heal pls", "im dying lol", "ouch"));
        }

        void OnDied()
        {
            Down = true;
            downTimer = 10f;
            if (visual != null) visual.PlayDeath();
            Say(Pick("noooo", "rip ;-;", "down! brb", "welp"), 3f);
        }

        void GetUp()
        {
            Down = false;
            var lead = Lead;
            if (lead != null) transform.position = BattleController.ClampToArena(lead.Position + Quaternion.Euler(0f, slotAngle, 0f) * Vector3.back * 2f);
            Health.Revive(0.5f);
            Destroy(visual.gameObject);
            visual = CharacterVisual.BuildHero(def, transform);
            VFX.Pillar(Position, tint, 5f, 0.5f);
            Say(Pick("back!", "im back", "ok round 2", "revived :D"));
        }

        static readonly string[] IdleLines =
        {
            "where r the demons", "this map is pretty", "lets gooo", "anyone else lagging? jk", "follow the road i think",
            "brb 1 sec... ok back", "gg so far", "i need better gear lol", "ur pretty good at this"
        };

        static string Pick(params string[] options) { return options[Random.Range(0, options.Length)]; }
    }
}
