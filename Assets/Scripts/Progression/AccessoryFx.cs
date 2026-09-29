using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>Looks up the special effect of the accessory a slayer is wearing, and team-wide reward effects.</summary>
    public static class AccessoryFx
    {
        public static string EffectOf(OwnedCharacter c)
        {
            if (c == null || string.IsNullOrEmpty(c.accessoryUid) || GameManager.Instance == null || GameManager.Instance.Data == null) return "";
            var item = GameManager.Instance.Data.GetItem(c.accessoryUid);
            var def = item != null ? GameDatabase.GetEquipment(item.defId) : null;
            return def != null && def.effect != null ? def.effect : "";
        }

        /// <summary>True if anyone in the team wears an accessory with this effect (gold and EXP boosts).</summary>
        public static bool TeamHas(PlayerData d, string effect)
        {
            if (d == null || d.team == null) return false;
            foreach (var id in d.team)
            {
                var c = d.GetCharacter(id);
                if (c == null || string.IsNullOrEmpty(c.accessoryUid)) continue;
                var item = d.GetItem(c.accessoryUid);
                var def = item != null ? GameDatabase.GetEquipment(item.defId) : null;
                if (def != null && def.effect == effect) return true;
            }
            return false;
        }
    }
}
