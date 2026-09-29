using System.IO;
using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// Owner-only unlock: if a file named "unlock_all" sits next to this computer's save file, every playable
    /// slayer is added to that save (new ones too, each launch). The file is never part of the game or the repo —
    /// it is created on one machine by tools/unlock_all.sh — so nobody else's game is affected.
    /// </summary>
    public static class OwnerUnlock
    {
        const string Marker = "unlock_all";

        public static bool Active
        {
            get
            {
                try { return File.Exists(Path.Combine(Application.persistentDataPath, Marker)); }
                catch { return false; }
            }
        }

        /// <summary>Adds every playable slayer the save doesn't own yet. Returns how many were added.</summary>
        public static int Apply(PlayerData data)
        {
            if (data == null || !Active) return 0;
            int added = 0;
            foreach (var def in GameDatabase.Characters)
            {
                if (def.npc || data.GetCharacter(def.id) != null) continue;
                if (InventorySystem.AddCharacter(data, def.id)) added++;
            }
            if (added > 0)
            {
                SaveSystem.Save(data);
                Debug.Log("[OwnerUnlock] Unlocked " + added + " slayers on this computer.");
            }
            return added;
        }
    }
}
