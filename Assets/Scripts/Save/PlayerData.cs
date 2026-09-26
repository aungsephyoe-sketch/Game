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

    /// <summary>Everything persisted between sessions. Serialized with JsonUtility.</summary>
    [System.Serializable]
    public class PlayerData
    {
        public int saveVersion = 1;
        public int coins = 5000;
        public int crystals = 100;
        public int expScrolls = 5;
        public int skillScrolls = 3;
        public int ascensionOre = 0;
        public int nextEquipmentUid = 1;
        public int totalKills;
        public int missionsCleared;

        public List<OwnedCharacter> characters = new List<OwnedCharacter>();
        public List<string> team = new List<string>();
        public List<EquipmentItem> equipment = new List<EquipmentItem>();
        public List<MissionProgress> missions = new List<MissionProgress>();

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
