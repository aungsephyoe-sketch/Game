using System.IO;
using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// Local JSON save in persistentDataPath with a backup copy. Designed so a cloud save backend
    /// (Firebase/PlayFab) can later replace Load/Save without changing callers.
    /// </summary>
    public static class SaveSystem
    {
        const string FileName = "hashira_save.json";

        static string PathMain { get { return Path.Combine(Application.persistentDataPath, FileName); } }
        static string PathBackup { get { return PathMain + ".bak"; } }

        /// <summary>
        /// The game was renamed (Hashira Chronicles → Blade Legends), which moves Unity's save folder. On the first
        /// run under the new name, bring the old save across so no progress is lost.
        /// </summary>
        static void MigrateRenamedSave()
        {
            try
            {
                if (File.Exists(PathMain)) return;
                string dir = Application.persistentDataPath;
                string oldDir = dir.Replace(GameConfig.GameName, "Hashira Chronicles");
                if (oldDir == dir) return;
                string oldMain = Path.Combine(oldDir, FileName);
                if (!File.Exists(oldMain)) oldMain = oldMain + ".bak";
                if (!File.Exists(oldMain)) return;
                Directory.CreateDirectory(dir);
                File.Copy(oldMain, PathMain, false);
                Debug.Log("[Save] Brought the save across from " + oldDir);
            }
            catch (System.Exception e) { Debug.LogWarning("[Save] Could not bring the old save across: " + e.Message); }
        }

        public static PlayerData Load()
        {
            MigrateRenamedSave();
            var data = TryRead(PathMain) ?? TryRead(PathBackup);
            // Saves from before the story overhaul don't match the new world; start the new story fresh.
            if (data != null && data.saveVersion < 2) data = null;
            if (data != null && data.saveVersion == 2) MigrateToV3(data);
            if (data == null)
            {
                data = CreateNew();
                Save(data);
            }
            Validate(data);
            return data;
        }

        /// <summary>
        /// v3: rarity tiers moved down one step (2★ Common .. 6★ Mythic), levels are capped by rarity,
        /// scrolls and ore became XP, teams hold three slayers.
        /// </summary>
        static void MigrateToV3(PlayerData d)
        {
            d.xp += d.expScrolls * ExperienceSystem.ExpPerScroll + d.skillScrolls * 500 + d.ascensionOre * 800;
            d.expScrolls = d.skillScrolls = d.ascensionOre = 0;
            foreach (var c in d.characters)
            {
                c.stars = Mathf.Clamp(c.stars - 1, 2, CharacterSystem.MaxStars);
                c.level = Mathf.Clamp(c.level, 1, ExperienceSystem.Cap(c));
            }
            if (d.team.Count > 3) d.team.RemoveRange(3, d.team.Count - 3);
            if (d.teamPresets != null) foreach (var p in d.teamPresets) if (p.ids.Count > 3) p.ids.RemoveRange(3, p.ids.Count - 3);
            if (d.copies == null) d.copies = new System.Collections.Generic.List<CopyStack>();
            d.saveVersion = PlayerData.CurrentVersion;
        }

        public static void Save(PlayerData data)
        {
            if (data == null) return;
            try
            {
                string json = JsonUtility.ToJson(data, false);
                if (File.Exists(PathMain)) File.Copy(PathMain, PathBackup, true);
                string tmp = PathMain + ".tmp";
                File.WriteAllText(tmp, json);
                if (File.Exists(PathMain)) File.Delete(PathMain);
                File.Move(tmp, PathMain);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[SaveSystem] Save failed: " + e.Message);
            }
        }

        public static PlayerData ResetProgress()
        {
            try
            {
                if (File.Exists(PathMain)) File.Delete(PathMain);
                if (File.Exists(PathBackup)) File.Delete(PathBackup);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[SaveSystem] Reset failed: " + e.Message);
            }
            var data = CreateNew();
            Save(data);
            return data;
        }

        static PlayerData TryRead(string path)
        {
            try
            {
                if (!File.Exists(path)) return null;
                var data = JsonUtility.FromJson<PlayerData>(File.ReadAllText(path));
                return data != null && data.characters != null && data.characters.Count > 0 ? data : null;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[SaveSystem] Could not read " + path + ": " + e.Message);
                return null;
            }
        }

        public static PlayerData CreateNew()
        {
            GameDatabase.EnsureBuilt();
            var data = new PlayerData();
            foreach (var id in GameDatabase.StarterTeam)
            {
                var def = GameDatabase.GetCharacter(id);
                data.characters.Add(new OwnedCharacter { id = id, stars = def.rarity });
                data.team.Add(id);
            }
            // Give the first character a starter kit so equipment is visible from minute one.
            var lead = data.characters[0];
            foreach (var eqId in GameDatabase.StarterEquipment)
            {
                var item = InventorySystem.AddEquipment(data, eqId);
                var def = GameDatabase.GetEquipment(eqId);
                lead.SetEquipped(def.slot, item.uid);
            }
            return data;
        }

        /// <summary>Repairs saves from older versions or with removed content.</summary>
        static void Validate(PlayerData data)
        {
            // Teams hold three slayers.
            if (data.team.Count > 3) data.team.RemoveRange(3, data.team.Count - 3);
            if (data.copies == null) data.copies = new System.Collections.Generic.List<CopyStack>();
            // One-time gift: 100,000 diamonds.
            if (!data.diamondGift) { data.diamondGift = true; data.crystals += 100000; }
            GameDatabase.EnsureBuilt();
            data.characters.RemoveAll(c => GameDatabase.GetCharacter(c.id) == null);
            data.equipment.RemoveAll(e => GameDatabase.GetEquipment(e.defId) == null);
            foreach (var c in data.characters)
            {
                if (c.skillLevels == null || c.skillLevels.Length != 4) c.skillLevels = new[] { 1, 1, 1, 1 };
                if (c.stars <= 0) c.stars = GameDatabase.GetCharacter(c.id).rarity;
                if (data.GetItem(c.swordUid) == null) c.swordUid = "";
                if (data.GetItem(c.haoriUid) == null) c.haoriUid = "";
                if (data.GetItem(c.accessoryUid) == null) c.accessoryUid = "";
            }
            data.team.RemoveAll(id => data.GetCharacter(id) == null);
            while (data.team.Count > 4) data.team.RemoveAt(data.team.Count - 1);
            if (data.team.Count == 0 && data.characters.Count > 0) data.team.Add(data.characters[0].id);
            if (data.characters.Count == 0)
            {
                var fresh = CreateNew();
                data.characters = fresh.characters;
                data.team = fresh.team;
                data.equipment = fresh.equipment;
                data.nextEquipmentUid = fresh.nextEquipmentUid;
            }
        }
    }
}
