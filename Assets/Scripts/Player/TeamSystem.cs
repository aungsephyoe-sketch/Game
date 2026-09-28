using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// The three-slayer team: only the active member is on the field. Benched members slowly regenerate
    /// HP and keep ticking cooldowns, which rewards switching. Tagging in performs a switch-in attack.
    /// </summary>
    public class TeamSystem : MonoBehaviour
    {
        public const float SwitchCooldown = 1.2f;
        const float BenchRegenPerSecond = 0.01f;

        public readonly List<PlayerCharacter> Members = new List<PlayerCharacter>();
        public int ActiveIndex { get; private set; }
        public PlayerCharacter Active { get { return Members.Count > 0 ? Members[ActiveIndex] : null; } }
        public float SwitchTimer { get; private set; }
        public bool AnyMemberFell { get; private set; }
        public bool AllDefeated { get; private set; }

        public event System.Action<PlayerCharacter> ActiveChanged;

        bool pendingForcedSwitch;

        public void Setup(PlayerData data, Vector3 spawnPos)
        {
            var b = BattleController.Current;
            if (b != null && b.Def != null && !string.IsNullOrEmpty(b.Def.trialCharacterId))
            {
                SetupTrial(data, b.Def.trialCharacterId, spawnPos);
                return;
            }
            foreach (var id in data.team)
            {
                var owned = data.GetCharacter(id);
                var def = GameDatabase.GetCharacter(id);
                if (owned == null || def == null) continue;
                var go = new GameObject("Slayer_" + def.displayName);
                go.transform.SetParent(transform, false);
                go.transform.position = spawnPos;
                go.AddComponent<HealthSystem>();
                var pc = go.AddComponent<PlayerCharacter>();
                pc.Init(def, owned, CharacterSystem.ComputeStats(data, owned));
                go.SetActive(false);
                Members.Add(pc);
            }
            ActiveIndex = 0;
            if (Members.Count > 0) Members[0].gameObject.SetActive(true);
            GameEvents.PlayerMemberDown += OnMemberDown;
        }

        /// <summary>Trial: a borrowed copy of the slayer at max power — every star purple, max level, max skills, special ready.</summary>
        void SetupTrial(PlayerData data, string id, Vector3 spawnPos)
        {
            var def = GameDatabase.GetCharacter(id);
            if (def == null) return;
            var owned = new OwnedCharacter { id = id, stars = CharacterSystem.MaxStars, awaken = ExperienceSystem.MaxAwaken, treeNodes = ~0 };
            owned.level = ExperienceSystem.Cap(owned);
            owned.skillLevels = new[] { CharacterSystem.MaxSkillLevel, CharacterSystem.MaxSkillLevel, CharacterSystem.MaxSkillLevel, CharacterSystem.MaxSkillLevel };
            var go = new GameObject("Trial_" + def.displayName);
            go.transform.SetParent(transform, false);
            go.transform.position = spawnPos;
            go.AddComponent<HealthSystem>();
            var pc = go.AddComponent<PlayerCharacter>();
            pc.Init(def, owned, CharacterSystem.ComputeStats(new PlayerData(), owned));
            pc.UltGauge = PlayerCharacter.UltMax;
            Members.Add(pc);
            ActiveIndex = 0;
            GameEvents.PlayerMemberDown += OnMemberDown;
        }

        void OnDestroy()
        {
            GameEvents.PlayerMemberDown -= OnMemberDown;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            SwitchTimer = Mathf.Max(0f, SwitchTimer - dt);
            for (int i = 0; i < Members.Count; i++)
            {
                var m = Members[i];
                m.TickCooldowns(dt);
                if (i != ActiveIndex && m.IsAlive) m.Health.Heal(m.Health.Max * BenchRegenPerSecond * dt);
            }
        }

        public bool CanSwitchTo(int index)
        {
            if (index < 0 || index >= Members.Count || index == ActiveIndex) return false;
            if (!Members[index].IsAlive || SwitchTimer > 0f || pendingForcedSwitch) return false;
            return Active == null || Active.CanSwitchOut;
        }

        public bool TrySwitch(int index)
        {
            if (!CanSwitchTo(index)) return false;
            DoSwitch(index);
            return true;
        }

        void DoSwitch(int index)
        {
            var from = Active;
            var to = Members[index];
            Vector3 pos = from != null ? from.transform.position : Vector3.zero;
            Quaternion rot = from != null ? from.transform.rotation : Quaternion.identity;
            if (from != null)
            {
                from.OnSwitchOut();
                from.gameObject.SetActive(false);
            }
            ActiveIndex = index;
            to.transform.SetPositionAndRotation(pos, rot);
            to.gameObject.SetActive(true);
            to.Visual.ResetPose();
            to.OnSwitchIn();
            SwitchTimer = SwitchCooldown;
            if (ActiveChanged != null) ActiveChanged(to);
        }

        /// <summary>PvP: a fallen member returns; if nobody was standing, they become the active slayer.</summary>
        public void Respawn(PlayerCharacter pc, Vector3 at)
        {
            if (pc == null || !Members.Contains(pc)) return;
            pc.Revive(at, 1f);
            if (Active == null || !Active.IsAlive || AllDefeated)
            {
                AllDefeated = false;
                int idx = Members.IndexOf(pc);
                var from = Active;
                if (from != null && from != pc) { from.OnSwitchOut(); from.gameObject.SetActive(false); }
                ActiveIndex = idx;
                pc.gameObject.SetActive(true);
                pc.transform.position = at;
                pc.Visual.ResetPose();
                pc.OnSwitchIn();
                if (ActiveChanged != null) ActiveChanged(pc);
            }
        }

        void OnMemberDown(PlayerCharacter pc)
        {
            if (!Members.Contains(pc)) return;
            AnyMemberFell = true;
            if (pc == Active) StartCoroutine(ForcedSwitch());
        }

        IEnumerator ForcedSwitch()
        {
            pendingForcedSwitch = true;
            yield return new WaitForSeconds(0.9f);
            pendingForcedSwitch = false;
            for (int i = 1; i <= Members.Count; i++)
            {
                int idx = (ActiveIndex + i) % Members.Count;
                if (Members[idx].IsAlive)
                {
                    DoSwitch(idx);
                    yield break;
                }
            }
            AllDefeated = true;
        }
    }
}
