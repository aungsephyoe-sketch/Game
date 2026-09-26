namespace HashiraChronicles
{
    /// <summary>
    /// Per-character ability tree:
    ///            ATK +5%
    ///               │
    ///            ATK +10%
    ///               │
    ///          CRIT +5%
    ///           /      \
    ///      HP +10%    ATK +15%
    ///           \      /
    ///       SPECIAL DMG +10%
    /// </summary>
    public static class SkillTree
    {
        public class Node
        {
            public string label;
            public float atkPct, hpPct, crit, specialDmg;
            public int[] requiresAny;
            public int scrollCost;
            public int coinCost;
        }

        public static readonly Node[] Nodes =
        {
            new Node { label = "ATK +5%", atkPct = 0.05f, requiresAny = new int[0], scrollCost = 1, coinCost = 1000 },
            new Node { label = "ATK +10%", atkPct = 0.10f, requiresAny = new[] { 0 }, scrollCost = 2, coinCost = 2500 },
            new Node { label = "CRIT +5%", crit = 0.05f, requiresAny = new[] { 1 }, scrollCost = 3, coinCost = 5000 },
            new Node { label = "HP +10%", hpPct = 0.10f, requiresAny = new[] { 2 }, scrollCost = 3, coinCost = 6000 },
            new Node { label = "ATK +15%", atkPct = 0.15f, requiresAny = new[] { 2 }, scrollCost = 4, coinCost = 8000 },
            new Node { label = "SPECIAL +10%", specialDmg = 0.10f, requiresAny = new[] { 3, 4 }, scrollCost = 5, coinCost = 12000 },
        };

        public static bool IsUnlocked(OwnedCharacter c, int node)
        {
            return (c.treeNodes & (1 << node)) != 0;
        }

        public static bool CanUnlock(OwnedCharacter c, int node)
        {
            if (IsUnlocked(c, node)) return false;
            var n = Nodes[node];
            if (n.requiresAny.Length == 0) return true;
            foreach (int r in n.requiresAny)
                if (IsUnlocked(c, r)) return true;
            return false;
        }

        public static bool TryUnlock(PlayerData data, OwnedCharacter c, int node)
        {
            if (!CanUnlock(c, node)) return false;
            var n = Nodes[node];
            if (data.skillScrolls < n.scrollCost || data.coins < n.coinCost) return false;
            data.skillScrolls -= n.scrollCost;
            data.coins -= n.coinCost;
            c.treeNodes |= 1 << node;
            return true;
        }

        public static void Accumulate(OwnedCharacter c, out float atkPct, out float hpPct, out float crit, out float special)
        {
            atkPct = hpPct = crit = special = 0f;
            for (int i = 0; i < Nodes.Length; i++)
            {
                if (!IsUnlocked(c, i)) continue;
                atkPct += Nodes[i].atkPct;
                hpPct += Nodes[i].hpPct;
                crit += Nodes[i].crit;
                special += Nodes[i].specialDmg;
            }
        }
    }
}
