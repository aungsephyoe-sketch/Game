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

        public static PlayerData Load()
        {
            var data = TryRead(PathMain) ?? TryRead(PathBackup);
            if (data == null)
            {
                data = CreateNew();
                Save(data);
            }
            Validate(data);
            return data;
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
            foreach (var c in data.characters)
            {
                if (data.team.Count >= 3) break;
                if (!data.team.Contains(c.id)) data.team.Add(c.id);
            }
        }
    }
}
