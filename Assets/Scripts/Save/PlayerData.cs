using System.Collections.Generic;

namespace HashiraChronicles
{
    [System.Serializable]
    public class OwnedCharacter
    {
        public string id;
        public int level = 1;
        public int exp;
        public int stars;
        /// <summary>Levels of Skill 1, Skill 2, Skill 3 and Ultimate (1..10).</summary>
        public int[] skillLevels = { 1, 1, 1, 1 };
        public string swordUid = "";
        public string haoriUid = "";
        public string accessoryUid = "";
        /// <summary>Bitmask of unlocked ability-tree nodes.</summary>
        public int treeNodes;
        /// <summary>Awakenings from duplicates of this same slayer (0..6): each is a purple star, +30 max level, +20% stats.</summary>
        public int awaken;

        public string GetEquipped(EquipSlot slot)
        {
            switch (slot)
            {
                case EquipSlot.Sword: return swordUid;
                case EquipSlot.Haori: return haoriUid;
                default: return accessoryUid;
            }
        }

        public void SetEquipped(EquipSlot slot, string uid)
        {
            switch (slot)
            {
                case EquipSlot.Sword: swordUid = uid ?? ""; break;
                case EquipSlot.Haori: haoriUid = uid ?? ""; break;
                default: accessoryUid = uid ?? ""; break;
            }
        }
    }

    [System.Serializable]
    public class EquipmentItem
    {
        public string uid;
        public string defId;
        public int level = 1;
    }

    [System.Serializable]
    public class MissionProgress
    {
        public string id;
        public bool cleared;
        /// <summary>Bitmask of the three mission objectives ever completed.</summary>
        public int objectivesMask;
        public float bestTime;
        public int clears;
    }

    [System.Serializable]
    public class CopyStack
    {
        public string id;
        public int count;
    }

    [System.Serializable]
    public class TeamPreset
    {
        public List<string> ids = new List<string>();
    }

    [System.Serializable]
    public class QuestState
    {
        public string id;
        public int progress;
        public bool claimed;
    }

    /// <summary>Everything persisted between sessions. Serialized with JsonUtility.</summary>
    [System.Serializable]
    public class PlayerData
    {
        public const int CurrentVersion = 3;
        public int saveVersion = CurrentVersion;
        public int coins = 5000;
        public int crystals = 100;
        /// <summary>XP, the third currency (with gold and diamonds): trains slayers' levels, skills and stars.</summary>
        public int xp = 5000;
        /// <summary>Duplicate slayers from summons: feed them to others for XP or sell them for gold.</summary>
        public List<CopyStack> copies = new List<CopyStack>();
        public int expScrolls = 0;
        public int skillScrolls = 0;
        public int ascensionOre = 0;
        public int nextEquipmentUid = 1;
        public int totalKills;
        public int missionsCleared;

        public List<OwnedCharacter> characters = new List<OwnedCharacter>();
        public List<string> team = new List<string>();
        /// <summary>Saved line-ups for TEAM 1-4; <see cref="team"/> is always the active one.</summary>
        public List<TeamPreset> teamPresets = new List<TeamPreset>();
        public int activeTeam;
        public List<EquipmentItem> equipment = new List<EquipmentItem>();
        public List<MissionProgress> missions = new List<MissionProgress>();

        // ---- Story & world
        public bool introSeen;
        public string currentRegion = "village";
        public List<string> seenCutscenes = new List<string>();

        // ---- Summons
        public int summonPity;
        public int totalSummons;

        // ---- Missions board (daily / weekly quests)
        public string dailyKey = "";

        // ---- Player profile
        public bool profileDone;
        public string playerName = "";
        public int playerAge;
        public int birthMonth = 1, birthDay = 1;
        /// <summary>The last year the birthday gift was given.</summary>
        public int birthdayRewardYear;
        /// <summary>The year the profile was made (the age entered then is already correct that year).</summary>
        public int profileYear;

        // ---- Step-up summons: which step of the ×10 ladder is next, and exchange tokens earned from paid ×10s.
        public int summonStep;
        public int summonTokens;
        /// <summary>One-time gift of 100,000 diamonds.</summary>
        public bool diamondGift;

        // ---- Daily login reward (rolled once per day, claimed from a popup on the home screen)
        public string loginKey = "";
        public int loginDays;
        public int loginGold, loginDiamonds;
        public bool loginPending;
        public string weeklyKey = "";
        public List<QuestState> quests = new List<QuestState>();
        public int bossesDefeated;

        public bool HasSeen(string cutsceneId) { return seenCutscenes.Contains(cutsceneId); }
        public void MarkSeen(string cutsceneId) { if (!seenCutscenes.Contains(cutsceneId)) seenCutscenes.Add(cutsceneId); }

        public bool IsRegionUnlocked(RegionDefinition r)
        {
            return r != null && (string.IsNullOrEmpty(r.unlockAfterMission) || IsMissionCleared(r.unlockAfterMission));
        }

        public OwnedCharacter GetCharacter(string id)
        {
            return characters.Find(c => c.id == id);
        }

        public EquipmentItem GetItem(string uid)
        {
            if (string.IsNullOrEmpty(uid)) return null;
            return equipment.Find(e => e.uid == uid);
        }

        public MissionProgress GetMission(string id, bool create = false)
        {
            var m = missions.Find(x => x.id == id);
            if (m == null && create)
            {
                m = new MissionProgress { id = id };
                missions.Add(m);
            }
            return m;
        }

        public bool IsMissionCleared(string id)
        {
            var m = GetMission(id);
            return m != null && m.cleared;
        }

        public bool IsMissionUnlocked(MissionDefinition def)
        {
            return string.IsNullOrEmpty(def.requiresMissionId) || IsMissionCleared(def.requiresMissionId);
        }

        /// <summary>Who (if anyone) has this equipment item on.</summary>
        public OwnedCharacter WhoEquipped(string uid)
        {
            if (string.IsNullOrEmpty(uid)) return null;
            return characters.Find(c => c.swordUid == uid || c.haoriUid == uid || c.accessoryUid == uid);
        }
    }
}
