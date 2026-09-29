using System.Collections.Generic;
using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// An AI slayer (co-op teammates, PvP teammates and opponents), built as a small state machine that runs
    /// Observe → Decide → Act several times a second:
    ///
    ///   Observe  — its target and how hurt it is, its own health and how fast it's losing it, cooldowns, how many
    ///              enemies are bunched up where, allies in trouble, red warning zones, whether it's being hit,
    ///              whether a straight line to where it wants to be is actually walkable.
    ///   Decide   — picks a state: Search, Navigate, Position, Attack, Special, Ultimate, Reposition, Dodge,
    ///              Retreat, Help Ally, Defend, Recover (from stuck). A state is held for a moment (no flickering)
    ///              unless something urgent (a red zone, a heavy hit) interrupts it.
    ///   Act      — moves with a <see cref="NavAgent"/> (road routing, obstacle feelers, stuck detection and detours),
    ///              turns smoothly, eases between walk and run, and attacks.
    ///
    /// Roles (from the slayer's weapon and role) decide the shape of its fighting, and specials are used only when
    /// they make sense — never on an empty target, never dashing into a wall:
    ///   • Vanguard (tank): holds the front between the enemies and the weakest ally, leaps in with Ground Breaker
    ///     when 2+ demons are bunched (or a big one), and plants Iron Wall (damage cut + stagger) when it's taking
    ///     heavy damage.
    ///   • Duelist (close fighter / assassin): circles to the target's flank before committing, prefers weak or
    ///     isolated targets, Flash Steps only along a clear line with a demon on it, hops back after.
    ///   • Skirmisher (ranged): keeps 6–8 m, backs off when something closes in, fires Crescent Volley when 2+ demons
    ///     sit inside the fan.
    ///   • Support: stays near the ally who needs it most, out of melee, heals when someone is hurt and saves
    ///     Sanctuary for when two or more are.
    /// Personalities change the numbers: Aggressive (pushes, low retreat threshold), Balanced, Defensive (keeps
    /// distance, retreats early), Tactical (waits for better special openings, focuses weak targets).
    /// </summary>
    public class PartySlayer : Combatant
    {
        public enum Style { Vanguard, Duelist, Skirmisher, Support }
        public enum Personality { Aggressive, Balanced, Defensive, Tactical }
        public enum AIState { Idle, Search, Navigate, Position, Attack, Special, Ultimate, Reposition, Dodge, Retreat, HelpAlly, Defend, Recover }

        public static readonly List<PartySlayer> Party = new List<PartySlayer>();

        public string DisplayName = "";
        /// <summary>Where they respawn and wander when there's no one to follow (PvP bases).</summary>
        public Vector3 Home;
        /// <summary>A point worth fighting over (the PvP crystal or boss); they head there when idle.</summary>
        public Vector3? Objective;
        public float RespawnDelay = 10f;
        /// <summary>Raised when a party slayer goes down (PvP scoring).</summary>
        public static event System.Action<PartySlayer> Downed;
        public Style PlayStyle { get; private set; }
        public Personality Mind { get; private set; }
        public AIState State { get; private set; }
        public string Bubble { get; private set; }
        public float BubbleUntil { get; private set; }
        public bool Down { get; private set; }

        /// <summary>The numbers a role and personality play by.</summary>
        struct Profile
        {
            public float range;          // where it wants to stand from its target
            public float retreatHp;      // retreats below this health
            public float specialGroup;   // enemies needed near the special's area before using it
            public float thinkRate;      // seconds between decisions
            public float risk;           // 0 careful … 1 reckless
            public float keepAway;       // minimum distance from enemies it tries to keep (0 = none)
        }

        CharacterVisual visual;
        CharacterDefinition def;
        Color tint;
        Combatant target;
        NavAgent nav;
        Profile prof;
        float aggression, skill, slotAngle;
        float thinkTimer, stateUntil, swingTimer, specialTimer, ultTimer, defendTimer, dodgeTimer, idleTalk, downTimer, recoverTimer;
        float moveBlend, dashTimer;
        int comboStep;
        Vector3 stateGoal;
        bool hunt;
        // Damage taken in the last few seconds (for "being hurt fast").
        readonly Queue<KeyValuePair<float, float>> recentHits = new Queue<KeyValuePair<float, float>>();

        static readonly string[] Specials = { "Ground Breaker", "Flash Step", "Crescent Volley", "Healing Bloom" };
        static readonly string[] Ultimates = { "Titan Fall", "Thousand Cuts", "Starfall Barrage", "Sanctuary" };

        public static PartySlayer Spawn(Transform parent, Vector3 pos, int level, string characterId, string name, int index, Style? avoid, CombatTeam team = CombatTeam.Player)
        {
            var d = GameDatabase.GetCharacter(characterId);
            if (d == null) return null;
            var go = new GameObject("Party " + name);
            go.transform.SetParent(parent, false);
            // Only ever on valid walkable ground (never inside scenery or off the road).
            pos = BattleController.ClampToArena(pos);
            go.transform.position = pos;
            go.AddComponent<HealthSystem>();
            var a = go.AddComponent<PartySlayer>();
            a.def = d;

            a.Team = team;
            a.Home = pos;
            a.Element = d.element;
            a.DisplayName = name;
            a.tint = ElementChart.ColorOf(d.element);
            a.PlayStyle = StyleFor(d);
            if (avoid.HasValue && a.PlayStyle == avoid.Value) a.PlayStyle = (Style)(((int)a.PlayStyle + 1 + Random.Range(0, 3)) % 4);
            if (avoid.HasValue && a.PlayStyle == avoid.Value) a.PlayStyle = (Style)(((int)a.PlayStyle + 1) % 4);
            a.Mind = (Personality)Random.Range(0, 4);
            float scale = 1f + level * 0.1f;
            float hp = a.PlayStyle == Style.Vanguard ? 5600f : a.PlayStyle == Style.Skirmisher ? 3400f : 4200f;
            float atk = a.PlayStyle == Style.Duelist ? 250f : a.PlayStyle == Style.Support ? 170f : 220f;
            float spd = a.PlayStyle == Style.Duelist ? 5.2f : a.PlayStyle == Style.Vanguard ? 4.2f : 4.7f;
            a.Stats = new StatBlock(hp * scale, atk * scale, 150f * scale, 0.12f, 1.6f, spd, 0f);
            a.Radius = 0.45f;
            a.Health.Init(a.Stats.hp);
            a.Health.Died += a.OnDied;
            a.visual = CharacterVisual.BuildHero(d, go.transform);
            a.skill = Random.Range(0.5f, 0.95f);
            a.aggression = a.Mind == Personality.Aggressive ? Random.Range(0.75f, 1f) : a.Mind == Personality.Defensive ? Random.Range(0.25f, 0.5f) : Random.Range(0.45f, 0.75f);
            a.prof = MakeProfile(a.PlayStyle, a.Mind);
            a.nav = new NavAgent();
            a.slotAngle = index == 0 ? -70f : 70f;
            a.swingTimer = Random.Range(0.3f, 0.9f);
            a.specialTimer = Random.Range(3f, 6f);
            a.ultTimer = Random.Range(18f, 26f);
            a.idleTalk = Random.Range(6f, 14f);
            a.defendTimer = -10f;
            a.State = AIState.Search;
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

        static Profile MakeProfile(Style s, Personality m)
        {
            var p = new Profile();
            switch (s)
            {
                case Style.Vanguard: p.range = 1.6f; p.retreatHp = 0.18f; p.specialGroup = 2f; p.keepAway = 0f; break;
                case Style.Duelist: p.range = 1.5f; p.retreatHp = 0.28f; p.specialGroup = 1f; p.keepAway = 0f; break;
                case Style.Skirmisher: p.range = 7f; p.retreatHp = 0.3f; p.specialGroup = 2f; p.keepAway = 4f; break;
                default: p.range = 6f; p.retreatHp = 0.3f; p.specialGroup = 1f; p.keepAway = 4.5f; break;
            }
            p.thinkRate = 0.25f;
            p.risk = 0.5f;
            switch (m)
            {
                case Personality.Aggressive: p.retreatHp *= 0.6f; p.risk = 0.9f; p.thinkRate = 0.18f; p.specialGroup = Mathf.Max(1f, p.specialGroup - 1f); break;
                case Personality.Defensive: p.retreatHp += 0.15f; p.risk = 0.2f; p.keepAway += 1.2f; if (p.range > 3f) p.range += 1.5f; break;
                case Personality.Tactical: p.specialGroup += 1f; p.risk = 0.45f; p.thinkRate = 0.2f; break;
            }
            return p;
        }

        protected override void OnEnable() { base.OnEnable(); Party.Add(this); Downed += OnPartyDowned; GameEvents.PlayerMemberDown += OnPlayerDowned; }
        protected override void OnDisable() { base.OnDisable(); Party.Remove(this); Downed -= OnPartyDowned; GameEvents.PlayerMemberDown -= OnPlayerDowned; }

        PlayerCharacter Lead
        {
            get
            {
                if (Team != CombatTeam.Player) return null;
                var b = BattleController.Current;
                return b != null && b.Team != null ? b.Team.Active : null;
            }
        }

        public void Say(string text, float seconds = 3.2f)
        {
            Bubble = ChatBrain.Casual(text, skill);
            BubbleUntil = Time.time + seconds;
        }

        static Vector3 Flat(Vector3 v) { return new Vector3(v.x, 0f, v.z); }
        static bool Valid(Combatant c) { return c != null && c.IsAlive && c.gameObject.activeInHierarchy; }
        bool IsFoe(Combatant c) { return c != null && c != this && c.IsAlive && (c.Team != Team || c.Neutral); }

        // ================================================================== Update

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
            if (!IsAlive || nav == null) return;
            dashTimer -= dt;
            swingTimer -= dt; specialTimer -= dt; ultTimer -= dt; defendTimer -= dt; dodgeTimer -= dt; idleTalk -= dt; recoverTimer -= dt;
            while (recentHits.Count > 0 && Time.time - recentHits.Peek().Key > 3f) recentHits.Dequeue();
            if (defendTimer <= 0f && DamageTakenMultiplier < 1f) DamageTakenMultiplier = 1f;

            var bc = BattleController.Current;
            hunt = Team == CombatTeam.Player && bc != null && bc.Def != null && bc.Def.pvpMode < 0;
            var lead = Lead;

            // ---- Urgent: step out of red zones right away (skilled players react sooner).
            if (dodgeTimer <= 0f && State != AIState.Dodge)
            {
                var zone = ThreatAt(Position, 0.3f);
                if (zone != null)
                {
                    dodgeTimer = Mathf.Lerp(0.7f, 0.25f, skill);
                    if (Random.value < 0.35f + skill * 0.6f) Enter(AIState.Dodge, 0.35f, EscapeFrom(zone));
                }
            }

            // ---- Observe → Decide (a few times a second, or when the current state has run out).
            thinkTimer -= dt;
            if (thinkTimer <= 0f || Time.time >= stateUntil)
            {
                thinkTimer = prof.thinkRate * Random.Range(0.8f, 1.2f);
                Decide(lead, bc);
            }

            // ---- Act.
            Act(lead, dt);
        }

        // ================================================================== Observe

        /// <summary>A red warning zone covering p (with some padding), if any.</summary>
        static Telegraph ThreatAt(Vector3 p, float pad)
        {
            foreach (var t in Telegraph.Active)
                if (t != null && t.Threatens(p, pad)) return t;
            return null;
        }

        /// <summary>The nearest walkable spot outside a zone that isn't inside another one.</summary>
        Vector3 EscapeFrom(Telegraph t)
        {
            Vector3 away = Flat(Position - t.DangerCenter);
            if (away.sqrMagnitude < 0.01f) away = -transform.forward;
            away.Normalize();
            Vector3 best = Position;
            float bestScore = float.MaxValue;
            float dist = Mathf.Max(1.5f, t.DangerRadius - Flat(Position - t.DangerCenter).magnitude + 1.8f);
            for (int i = -3; i <= 3; i++)
            {
                Vector3 d = Quaternion.Euler(0f, i * 35f, 0f) * away;
                Vector3 end = BattleController.ClampToArena(Obstacles.Sweep(Position, Position + d * dist));
                float moved = Flat(end - Position).magnitude;
                float score = Mathf.Abs(i) * 0.4f + (ThreatAt(end, 0.4f) != null ? 20f : 0f) + (moved < 1f ? 10f : 0f);
                if (score < bestScore) { bestScore = score; best = end; }
            }
            return best;
        }

        /// <summary>How many foes stand within radius of point.</summary>
        int FoesNear(Vector3 point, float radius)
        {
            int n = 0;
            foreach (var c in All) if (IsFoe(c) && Flat(c.Position - point).magnitude <= radius + c.Radius) n++;
            return n;
        }

        float DamageLately()
        {
            float sum = 0f;
            foreach (var h in recentHits) sum += h.Value;
            return Health.Max > 0f ? sum / Health.Max : 0f;
        }

        /// <summary>The ally (the player or a party member) who most needs help, and how badly (0..1).</summary>
        Combatant NeediestAlly(PlayerCharacter lead, out float need)
        {
            Combatant best = null;
            need = 0f;
            if (lead != null && lead.IsAlive) { best = lead; need = 1f - lead.Health.Normalized; }
            foreach (var o in Party)
            {
                if (o == null || o == this || o.Team != Team || o.Down) continue;
                float n = 1f - o.Health.Normalized;
                if (n > need || best == null) { need = n; best = o; }
            }
            return best;
        }

        Combatant ChooseTarget(PlayerCharacter lead)
        {
            Combatant best = null;
            float bestScore = float.MaxValue;
            float reach = hunt ? 45f : Objective.HasValue ? 30f : 22f;
            float needAlly;
            var ward = NeediestAlly(lead, out needAlly);
            foreach (var c in All)
            {
                if (!IsFoe(c)) continue;
                float dSelf = Flat(c.Position - Position).magnitude;
                if (dSelf > reach) continue;
                if (lead != null && !hunt && Flat(c.Position - lead.Position).magnitude > 22f) continue;
                float score = dSelf;
                // Demons on top of a hurt ally matter more (tanks and supports care most).
                if (ward != null && Flat(c.Position - ward.Position).magnitude < 3.5f)
                    score -= (PlayStyle == Style.Vanguard || PlayStyle == Style.Support ? 8f : 4f) * (0.4f + needAlly);
                if (c.Health != null)
                {
                    // Assassins and tacticians go for the weak; tanks for the big ones.
                    if (PlayStyle == Style.Duelist || Mind == Personality.Tactical) score *= 0.45f + c.Health.Normalized;
                    var e = c as EnemyController;
                    if (PlayStyle == Style.Vanguard && e != null && (e.IsBoss || e.Def.archetype == EnemyArchetype.Elite || e.Def.archetype == EnemyArchetype.Tank)) score -= 4f;
                }
                // Isolated targets suit duelists.
                if (PlayStyle == Style.Duelist && FoesNear(c.Position, 3.5f) <= 1) score -= 2f;
                // Stick with the current target unless something is clearly better (no thrashing).
                if (c == target) score -= 3f;
                score += Random.Range(0f, 2f) * (1f - skill);
                if (score < bestScore) { bestScore = score; best = c; }
            }
            return best;
        }

        // ================================================================== Decide

        void Enter(AIState s, float hold, Vector3 goal)
        {
            State = s;
            stateUntil = Time.time + hold;
            stateGoal = BattleController.ClampToArena(goal);
        }

        void Decide(PlayerCharacter lead, BattleController bc)
        {
            // Committed moves finish first.
            if ((State == AIState.Dodge || State == AIState.Recover || State == AIState.Special || State == AIState.Ultimate || State == AIState.Defend) && Time.time < stateUntil) return;

            // Stuck? Recover (the nav agent is already taking a detour; give it room).
            if (nav.StuckCount >= 3 && recoverTimer <= 0f)
            {
                recoverTimer = 3f;
                Enter(AIState.Recover, 1.4f, nav.Waypoint);
                if (Random.value < 0.25f) Say(Pick("hold on, going around", "wrong way lol", "brb pathing"));
                return;
            }

            if (!Valid(target) || !IsFoe(target)) target = null;
            var pick = ChooseTarget(lead);
            if (pick != target && pick != null && target != null && Random.value < 0.15f) Say(Pick("switching target", "got the one on the left", "on it"), 1.8f);
            target = pick;

            float hp = Health.Normalized;
            float hurtFast = DamageLately();

            // Defend (tank): brace when it's being chewed up.
            if (PlayStyle == Style.Vanguard && defendTimer <= -6f && (hurtFast > 0.2f || (hp < 0.5f && FoesNear(Position, 3f) >= 2)))
            {
                IronWall();
                Enter(AIState.Defend, 0.8f, Position);
                return;
            }

            // Retreat when badly hurt and something is close (a support heals itself instead when it can).
            if (hp < prof.retreatHp && target != null && Flat(target.Position - Position).magnitude < 6f)
            {
                if (PlayStyle == Style.Support && specialTimer <= 0f) { Heal(false); Enter(AIState.Special, 0.8f, Position); return; }
                Enter(AIState.Retreat, 1.6f, RetreatPoint(lead));
                if (Random.value < 0.3f) Say(Pick("low hp backing off", "need a sec", "heal pls", "retreating!"));
                return;
            }

            // Support: heal / stay with whoever needs it.
            if (PlayStyle == Style.Support)
            {
                float need;
                var ward = NeediestAlly(lead, out need);
                if (ultTimer <= 0f && CountHurtAllies(lead, 0.5f) >= 2) { Ultimate(); Enter(AIState.Ultimate, 1f, Position); return; }
                if (specialTimer <= 0f && ward != null && need > 0.35f && Flat(ward.Position - Position).magnitude < 9f) { Heal(false); Enter(AIState.Special, 0.8f, Position); return; }
                if (ward != null && Flat(ward.Position - Position).magnitude > 6.5f) { Enter(AIState.HelpAlly, 0.8f, ward.Position); return; }
            }

            if (target == null)
            {
                Enter(AIState.Search, 1f, SearchPoint(lead, bc));
                return;
            }

            float td = Flat(target.Position - Position).magnitude;

            // Ultimate: only with a real payoff.
            if (ultTimer <= 0f && PlayStyle != Style.Support && UltimateWorthIt(td))
            {
                Ultimate();
                Enter(AIState.Ultimate, 1.1f, Position);
                return;
            }

            // Special: evaluated per role (group size, clear line, valid landing).
            if (specialTimer <= 0f && TrySpecial(td)) return;

            // Keep away (ranged/support): back off when something closes in.
            if (prof.keepAway > 0f)
            {
                var close = NearestFoe(prof.keepAway);
                if (close != null)
                {
                    Vector3 away = Flat(Position - close.Position).normalized;
                    Enter(AIState.Reposition, 0.7f, Position + away * 3.5f + Quaternion.Euler(0f, 90f, 0f) * away * Random.Range(-1.5f, 1.5f));
                    return;
                }
            }

            // Position → Attack.
            Vector3 spot = AttackSpot(target, lead);
            float reach = PlayStyle == Style.Skirmisher || PlayStyle == Style.Support ? prof.range + 1.5f : target.Radius + 1.9f;
            if (td <= reach) Enter(AIState.Attack, 0.6f, spot);
            else Enter(td > 12f ? AIState.Navigate : AIState.Position, 0.5f, spot);
        }

        int CountHurtAllies(PlayerCharacter lead, float below)
        {
            int n = 0;
            if (lead != null && lead.IsAlive && lead.Health.Normalized < below) n++;
            foreach (var o in Party) if (o != null && o.Team == Team && !o.Down && o.Health.Normalized < below) n++;
            return n;
        }

        Combatant NearestFoe(float within)
        {
            Combatant best = null;
            float bd = within;
            foreach (var c in All)
            {
                if (!IsFoe(c)) continue;
                float d = Flat(c.Position - Position).magnitude - c.Radius;
                if (d < bd) { bd = d; best = c; }
            }
            return best;
        }

        /// <summary>Where to stand to attack this target (role-shaped, never inside a red zone).</summary>
        Vector3 AttackSpot(Combatant t, PlayerCharacter lead)
        {
            Vector3 fromT = Flat(Position - t.Position);
            if (fromT.sqrMagnitude < 0.01f) fromT = Quaternion.Euler(0f, slotAngle, 0f) * Vector3.back;
            fromT.Normalize();
            Vector3 dir = fromT;
            switch (PlayStyle)
            {
                case Style.Duelist:
                {
                    // Around to the flank/back of the target (opposite the player, so they pincer it).
                    Vector3 behind = lead != null ? Flat(t.Position - lead.Position) : Flat(-t.transform.forward);
                    if (behind.sqrMagnitude < 0.01f) behind = fromT;
                    dir = Vector3.Slerp(fromT, Quaternion.Euler(0f, slotAngle * 0.5f, 0f) * behind.normalized, 0.7f).normalized;
                    break;
                }
                case Style.Vanguard:
                {
                    // Between the target and the ally it threatens.
                    float need;
                    var ward = NeediestAlly(lead, out need);
                    if (ward != null)
                    {
                        Vector3 toWard = Flat(ward.Position - t.Position);
                        if (toWard.sqrMagnitude > 0.01f) dir = Vector3.Slerp(fromT, toWard.normalized, 0.6f).normalized;
                    }
                    break;
                }
                default:
                    dir = Quaternion.Euler(0f, slotAngle * 0.3f, 0f) * fromT;
                    break;
            }
            float want = PlayStyle == Style.Skirmisher || PlayStyle == Style.Support ? prof.range : t.Radius + prof.range * 0.8f;
            Vector3 spot = t.Position + dir * want;
            if (ThreatAt(spot, 0.3f) != null) spot = t.Position + Quaternion.Euler(0f, 70f, 0f) * dir * (want + 1.5f);
            // Don't stack on another party member.
            foreach (var o in Party)
                if (o != this && o.Team == Team && !o.Down && Flat(o.Position - spot).magnitude < 1.4f) spot += Quaternion.Euler(0f, 90f, 0f) * dir * 1.6f;
            return BattleController.ClampToArena(spot);
        }

        Vector3 RetreatPoint(PlayerCharacter lead)
        {
            Vector3 anchor = lead != null ? lead.Position : Home;
            // Toward a support ally if there is one.
            foreach (var o in Party) if (o != this && o.Team == Team && !o.Down && o.PlayStyle == Style.Support) { anchor = o.Position; break; }
            Vector3 away = target != null ? Flat(Position - target.Position).normalized : Vector3.zero;
            return anchor + away * 3f;
        }

        Vector3 SearchPoint(PlayerCharacter lead, BattleController bc)
        {
            if (Objective.HasValue) return Objective.Value + new Vector3(Random.Range(-3f, 3f), 0f, Random.Range(-3f, 3f));
            if (hunt && bc != null && bc.Mission != null && bc.Mission.ObjectiveTarget.HasValue)
                return bc.Mission.ObjectiveTarget.Value + Quaternion.Euler(0f, slotAngle, 0f) * Vector3.forward * 2.5f;
            if (lead != null)
            {
                Vector3 fwd = Flat(lead.transform.forward);
                if (fwd.sqrMagnitude < 0.01f) fwd = Vector3.forward;
                return lead.Position + Quaternion.Euler(0f, slotAngle * 0.6f + Random.Range(-30f, 30f), 0f) * fwd.normalized * Random.Range(3.5f, 6f);
            }
            return Home + new Vector3(Random.Range(-4f, 4f), 0f, Random.Range(-4f, 4f));
        }

        bool UltimateWorthIt(float td)
        {
            if (!Valid(target) || td > 6f) return false;
            int group = FoesNear(Position, 5.5f);
            var e = target as EnemyController;
            bool big = e != null && (e.IsBoss || e.Def.archetype == EnemyArchetype.Elite);
            if (group >= 3) return true;
            if (big && target.Health.Normalized > 0.3f) return true;
            if (Mind == Personality.Aggressive && group >= 2) return true;
            return false;
        }

        /// <summary>Uses the role's special only when it makes sense right now. True if it acted (or repositioned for it).</summary>
        bool TrySpecial(float td)
        {
            if (!Valid(target)) return false;
            var e = target as EnemyController;
            bool big = e != null && (e.IsBoss || e.Def.archetype == EnemyArchetype.Elite || e.Def.archetype == EnemyArchetype.Tank);
            switch (PlayStyle)
            {
                case Style.Vanguard:
                {
                    // Leap-slam where the crowd is: needs a reachable landing spot and enough demons there.
                    if (td > 7f) return false;
                    Vector3 land = target.Position - Flat(target.Position - Position).normalized * 1.2f;
                    Vector3 reach = BattleController.ClampToArena(Obstacles.Sweep(Position, land));
                    if (Flat(reach - land).magnitude > 1.5f) return false; // something in the way
                    if (FoesNear(target.Position, 3.6f) < prof.specialGroup && !big) return false;
                    GroundBreaker(reach);
                    Enter(AIState.Special, 0.6f, reach);
                    return true;
                }
                case Style.Duelist:
                {
                    // Flash Step along a clear line with the target on it; else step to a spot with a clear line.
                    if (td > 6f) return false;
                    Vector3 dir = Flat(target.Position - Position).normalized;
                    Vector3 end = BattleController.ClampToArena(Obstacles.Sweep(Position, Position + dir * 6f));
                    if (Flat(end - Position).magnitude < 4f)
                    {
                        // Blocked by a wall or the edge: find a better angle instead of dashing into it.
                        Enter(AIState.Reposition, 0.6f, target.Position + Quaternion.Euler(0f, 90f * (slotAngle > 0f ? 1f : -1f), 0f) * dir * 3f);
                        return true;
                    }
                    FlashStep(dir, end);
                    Enter(AIState.Special, 0.35f, end);
                    return true;
                }
                case Style.Skirmisher:
                {
                    if (td > 10f) return false;
                    // Count demons inside the fan (±27°).
                    Vector3 fwd = Flat(target.Position - Position).normalized;
                    int inFan = 0;
                    foreach (var c in All)
                    {
                        if (!IsFoe(c)) continue;
                        Vector3 to = Flat(c.Position - Position);
                        if (to.magnitude <= 10.5f && Vector3.Angle(fwd, to) <= 27f) inFan++;
                    }
                    if (inFan < prof.specialGroup && !big) return false;
                    CrescentVolley(fwd);
                    Enter(AIState.Special, 0.5f, Position);
                    return true;
                }
                default:
                    return false; // support heals are decided above
            }
        }

        // ================================================================== Act

        void Act(PlayerCharacter lead, float dt)
        {
            Vector3 goal = stateGoal;
            float stop = 0.35f, speedMul = 1f;
            bool face = true;
            switch (State)
            {
                case AIState.Attack:
                    if (Valid(target)) goal = AttackSpot(target, lead);
                    stop = 0.5f;
                    if (hunt) speedMul = 1.3f;
                    break;
                case AIState.Position: stop = 0.4f; if (hunt) speedMul = 1.45f; break;
                case AIState.Navigate: stop = 1f; speedMul = hunt ? 1.4f : 1.15f; break;
                case AIState.Search: stop = 1.2f; speedMul = hunt ? 1.35f : 0.75f; face = false; break;
                case AIState.Reposition: stop = 0.3f; speedMul = 1.15f; break;
                case AIState.Retreat: stop = 0.6f; speedMul = 1.3f; face = false; break;
                case AIState.HelpAlly: stop = 3.5f; speedMul = 1.2f; face = false; break;
                case AIState.Dodge: stop = 0.2f; speedMul = 2.6f; face = false; break;
                case AIState.Recover: stop = 0.3f; speedMul = 1.2f; face = false; break;
                case AIState.Defend: case AIState.Special: case AIState.Ultimate: case AIState.Idle: speedMul = 0f; break;
            }

            // Co-op: dash-lunge at demons that are a few metres off (only along a clear, walkable line).
            if (hunt && dashTimer <= 0f && Valid(target) && (State == AIState.Position || State == AIState.Navigate || State == AIState.Attack))
            {
                Vector3 toT = Flat(target.Position - Position);
                float dT = toT.magnitude - target.Radius - 1.2f;
                if (dT > 3f && dT < 16f)
                {
                    Vector3 dirT = toT.normalized;
                    Vector3 end = BattleController.ClampToArena(Obstacles.Sweep(Position, Position + dirT * Mathf.Min(dT, 7f)));
                    if (Flat(end - Position).magnitude > 2.5f)
                    {
                        dashTimer = Random.Range(0.9f, 1.6f) * (1.25f - skill * 0.4f);
                        VFX.KnockTrail(Position, dirT, tint);
                        VFX.Dust(Position, 4);
                        transform.position = end;
                        transform.rotation = Quaternion.LookRotation(dirT);
                        visual.DashAttack(0.15f);
                        swingTimer = Mathf.Min(swingTimer, 0.05f);
                        if (GameManager.Instance != null && Random.value < 0.4f) GameManager.Instance.Audio.PlayVaried("dash", 0.22f, 0.1f);
                    }
                }
            }

            // Move with the nav agent (road routing, feelers, stuck detection).
            Vector3 dir = speedMul > 0f ? nav.Steer(Position, goal, stop, dt) : Vector3.zero;
            // Keep a little space from teammates and the player.
            foreach (var o in Party)
            {
                if (o == this || o.Down || o.Team != Team) continue;
                Vector3 sep = Flat(Position - o.Position);
                if (sep.magnitude < 1.4f && sep.sqrMagnitude > 0.0001f) dir += sep.normalized * (1.4f - sep.magnitude) * 1.5f;
            }
            if (lead != null)
            {
                Vector3 sep = Flat(Position - lead.Position);
                if (sep.magnitude < 1.1f && sep.sqrMagnitude > 0.0001f) dir += sep.normalized * (1.1f - sep.magnitude) * 2f;
            }
            bool moving = dir.sqrMagnitude > 0.0004f;
            // Smooth walk/run blend (no snapping between standing and sprinting).
            moveBlend = Mathf.MoveTowards(moveBlend, moving ? Mathf.Clamp01(speedMul) : 0f, dt * 5f);
            if (moving)
            {
                // Co-op teammates run noticeably faster than a stroll: they're there to clear.
                float speed = Stats.speed * speedMul * (hunt ? 1.3f : 1f) * Mathf.Lerp(0.45f, 1f, moveBlend);
                Vector3 step = dir.normalized * speed * dt;
                transform.position = BattleController.ClampToArena(Obstacles.Sweep(transform.position, transform.position + step));
            }
            visual.SetMoving(moveBlend, speedMul > 1.2f);

            // Turn toward the target (or the way it's going), smoothly.
            Vector3 look = face && Valid(target) ? Flat(target.Position - Position) : dir;
            look.y = 0f;
            if (look.sqrMagnitude > 0.01f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(look), dt * 9f);

            // Basic attacks whenever a target is in reach, it's facing it and not busy.
            if (Valid(target) && State != AIState.Retreat && State != AIState.Dodge && State != AIState.Defend && State != AIState.Recover && swingTimer <= 0f)
            {
                Vector3 to = Flat(target.Position - Position);
                float reach = PlayStyle == Style.Skirmisher || PlayStyle == Style.Support ? 9f : target.Radius + 1.9f;
                if (to.magnitude <= reach && Vector3.Angle(transform.forward, to) < 35f) Swing();
            }

            if (!Valid(target) && idleTalk <= 0f)
            {
                idleTalk = Random.Range(10f, 22f);
                if (Random.value < 0.6f) Say(Pick(IdleLines));
            }
        }

        // ================================================================== Attacks

        void Swing()
        {
            int step = comboStep % 3;
            comboStep++;
            bool finisher = step == 2;
            float gap = PlayStyle == Style.Duelist ? 0.32f : PlayStyle == Style.Vanguard ? 0.62f : 0.5f;
            // Recovery after the combo finisher: a short pause, longer for careful players.
            swingTimer = finisher ? Random.Range(0.7f, 1.2f) * (1.4f - aggression * 0.5f) : gap;
            // Co-op: quicker strikes and shorter recovery.
            if (hunt) swingTimer *= finisher ? 0.55f : 0.7f;
            if (PlayStyle == Style.Skirmisher || PlayStyle == Style.Support)
            {
                // A thrown crescent (support: a lighter bolt) that flies to the demon.
                visual.Attack(step, 0.2f);
                Vector3 fwd = transform.forward;
                var tag = AttackTag.Basic(PlayStyle == Style.Support ? 0.6f : finisher ? 1.3f : 0.9f, tint);
                tag.hitStop = 0f; tag.shake = 0f; tag.stagger = 1f;
                CombatSystem.HitArc(this, Position, fwd, 9f, 16f, tag);
                int n = PlayStyle == Style.Support ? 1 : 3;
                for (int i = 0; i < n; i++) VFX.Slash(Position + fwd * (1.8f + i * 2.4f) + Vector3.down * 0.2f, fwd, 1.1f, 80f, i * 20f - 20f, tint, 0.12f + i * 0.04f);
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

        AttackTag SpecialTag()
        {
            var tag = AttackTag.Basic(2.4f, tint);
            tag.hitStop = 0f; tag.shake = 0f; tag.stagger = 3f;
            return tag;
        }

        void SpecialUsed()
        {
            specialTimer = Random.Range(6f, 9f) * (1.3f - skill * 0.4f) * (hunt ? 0.65f : 1f);
            swingTimer = hunt ? 0.35f : 0.7f;
            if (Random.value < 0.6f) Say(Specials[(int)PlayStyle] + "!!", 2f);
        }

        void GroundBreaker(Vector3 land)
        {
            SpecialUsed();
            VFX.HitStar(Position + Vector3.up * 1.6f, tint, 0.8f, 0.14f);
            transform.position = land;
            visual.HeavyAttack(0.3f);
            CombatSystem.HitRadius(this, Position, 3.6f, SpecialTag());
            VFX.Shockwave(Position, 3.6f, tint, 0.45f);
            VFX.Dust(Position, 14);
        }

        void FlashStep(Vector3 dir, Vector3 end)
        {
            SpecialUsed();
            Vector3 from = Position;
            visual.DashAttack(0.2f);
            CombatSystem.HitArc(this, from, dir, Flat(end - from).magnitude + 0.5f, 30f, SpecialTag());
            VFX.KnockTrail(from + dir * 1.5f, dir, tint);
            transform.position = end;
        }

        void CrescentVolley(Vector3 fwd)
        {
            SpecialUsed();
            transform.rotation = Quaternion.LookRotation(fwd);
            visual.HeavyAttack(0.25f);
            for (int k = -1; k <= 1; k++)
            {
                Vector3 dir = Quaternion.Euler(0f, k * 18f, 0f) * fwd;
                CombatSystem.HitArc(this, Position, dir, 10f, 14f, SpecialTag());
                for (int i = 0; i < 3; i++) VFX.Slash(Position + dir * (2f + i * 2.6f), dir, 1.2f, 70f, 0f, tint, 0.14f + i * 0.04f);
            }
        }

        /// <summary>Tank defence: brace (damage taken cut to 40% for 3.5 s) and stagger everything close.</summary>
        void IronWall()
        {
            defendTimer = 3.5f;
            DamageTakenMultiplier = 0.4f;
            visual.Guard(true);
            var tag = AttackTag.Basic(0.8f, tint);
            tag.stagger = 4f; tag.knockback = 3f; tag.hitStop = 0f; tag.shake = 0f;
            CombatSystem.HitRadius(this, Position, 3f, tag);
            VFX.Shockwave(Position, 3.2f, Color.Lerp(tint, Color.white, 0.4f), 0.5f);
            VFX.BurstDisc(Position, 2.4f, new Color(0.7f, 0.85f, 1f), 0.5f);
            Say(Pick("Iron Wall!", "i got aggro, hit them!", "tanking!"), 2f);
            Invoke("DropGuard", 3.5f);
        }

        void DropGuard() { if (visual != null) visual.Guard(false); }

        void Heal(bool big)
        {
            specialTimer = Random.Range(6f, 9f);
            visual.Victory();
            float frac = big ? 0.35f : 0.14f;
            var b = BattleController.Current;
            if (b != null && b.Team != null && Team == CombatTeam.Player)
                foreach (var m in b.Team.Members)
                    if (m != null && m.IsAlive && (m.Position - Position).magnitude < 10f) m.Health.Heal(m.Health.Max * frac);
            foreach (var o in Party) if (o != null && o.Team == Team && !o.Down) o.Health.Heal(o.Health.Max * frac);
            VFX.BurstDisc(Position, big ? 9f : 7f, new Color(0.5f, 1f, 0.6f), 0.6f);
            VFX.Breath(Position, new Color(0.5f, 1f, 0.6f), 40);
            if (Random.value < 0.5f) Say(Pick("healing!!", "heals up", "stay close, healing", "got u"));
        }

        void Ultimate()
        {
            if (PlayStyle != Style.Support && !Valid(target)) return; // target gone: don't waste it
            ultTimer = Random.Range(22f, 30f) * (hunt ? 0.7f : 1f);
            specialTimer = Mathf.Max(specialTimer, 2.5f);
            swingTimer = 1.2f;
            Say(Ultimates[(int)PlayStyle].ToUpper() + "!!!", 2.4f);
            if (PlayStyle == Style.Support) { Heal(true); return; }
            var tag = AttackTag.Basic(5f, tint);
            tag.hitStop = 0.04f; tag.shake = 0.3f; tag.stagger = 5f;
            visual.Spin(0.5f, 2);
            CombatSystem.HitRadius(this, Position, 5.5f, tag);
            VFX.Pillar(Position, tint, 7f, 0.8f);
            VFX.Shockwave(Position, 5.5f, tint, 0.6f);
            VFX.HitStar(Position + Vector3.up * 1.4f, tint, 2.2f, 0.2f);
            VFX.ImpactLight(Position + Vector3.up, tint, 9f, 0.6f);
            if (GameManager.Instance != null) GameManager.Instance.Audio.Play("ultimate", 0.35f);
        }

        // ================================================================== Reactions

        public override void OnHitReceived(DamageInfo info)
        {
            recentHits.Enqueue(new KeyValuePair<float, float>(Time.time, info.amount));
            if (visual != null) { visual.Hit(info.knockback); visual.Flash(Color.white, 0.6f); }
            transform.position = BattleController.ClampToArena(Obstacles.Sweep(transform.position, transform.position + info.knockback * 0.25f));
            if (Health.Normalized < 0.3f && Random.value < 0.3f) Say(Pick("low hp help", "heal pls", "im dying lol", "ouch"));
            // Careful players step out after a heavy hit instead of standing in the next one.
            if (info.amount > Health.Max * 0.12f && prof.risk < 0.7f && State != AIState.Special && State != AIState.Ultimate && State != AIState.Defend)
            {
                Vector3 kb = Flat(info.knockback);
                if (kb.sqrMagnitude < 0.01f) kb = -transform.forward;
                kb.Normalize();
                Vector3 side = Quaternion.Euler(0f, Random.value < 0.5f ? 90f : -90f, 0f) * kb;
                Enter(AIState.Reposition, 0.5f, Position + side * 2.5f + kb * 1.5f);
            }
        }

        void OnPartyDowned(PartySlayer p)
        {
            if (p == this || p == null || p.Team != Team || Down) return;
            if (Random.value < 0.6f) Say(Pick("NOO " + p.DisplayName, "they got " + p.DisplayName + "!", "avenge them!!"), 2.2f);
            // Aggressive players get angry; careful ones regroup.
            if (Mind == Personality.Aggressive) { aggression = Mathf.Min(1f, aggression + 0.2f); specialTimer = Mathf.Min(specialTimer, 0.5f); }
            else if (Mind == Personality.Defensive) Enter(AIState.Retreat, 1.2f, RetreatPoint(Lead));
        }

        void OnPlayerDowned(PlayerCharacter pc)
        {
            if (Team != CombatTeam.Player || Down || pc == null) return;
            if (Random.value < 0.6f) Say(Pick("nooo u ok?", "hang in there!", "i got this, get up!"), 2.2f);
            if (PlayStyle == Style.Support) specialTimer = Mathf.Min(specialTimer, 0.3f);
        }

        void OnDied()
        {
            Down = true;
            downTimer = RespawnDelay;
            DamageTakenMultiplier = 1f;
            if (Downed != null) Downed(this);
            if (visual != null) visual.PlayDeath();
            Say(Pick("noooo", "rip ;-;", "down! brb", "welp"), 3f);
        }

        void GetUp()
        {
            Down = false;
            var lead = Lead;
            if (lead != null) transform.position = BattleController.ClampToArena(lead.Position + Quaternion.Euler(0f, slotAngle, 0f) * Vector3.back * 2f);
            else transform.position = BattleController.ClampToArena(Home);
            Health.Revive(0.5f);
            Destroy(visual.gameObject);
            visual = CharacterVisual.BuildHero(def, transform);
            VFX.Pillar(Position, tint, 5f, 0.5f);
            target = null;
            State = AIState.Search;
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
