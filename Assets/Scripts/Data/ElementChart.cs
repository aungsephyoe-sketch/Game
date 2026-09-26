using UnityEngine;

namespace HashiraChronicles
{
    public static class ElementChart
    {
        public const float Advantage = 1.5f;
        public const float Disadvantage = 0.75f;

        /// <summary>The element this element is strong against (Light/Dark are strong against each other).</summary>
        public static Element StrongAgainst(Element e)
        {
            switch (e)
            {
                case Element.Water: return Element.Flame;
                case Element.Flame: return Element.Beast;
                case Element.Beast: return Element.Thunder;
                case Element.Thunder: return Element.Water;
                case Element.Light: return Element.Dark;
                default: return Element.Light;
            }
        }

        public static float Multiplier(Element attacker, Element defender)
        {
            if (StrongAgainst(attacker) == defender) return Advantage;
            bool attackerIsSpecial = attacker == Element.Light || attacker == Element.Dark;
            bool defenderIsSpecial = defender == Element.Light || defender == Element.Dark;
            if (!attackerIsSpecial && !defenderIsSpecial && StrongAgainst(defender) == attacker) return Disadvantage;
            return 1f;
        }

        public static Color ColorOf(Element e)
        {
            switch (e)
            {
                case Element.Water: return new Color(0.25f, 0.6f, 1f);
                case Element.Flame: return new Color(1f, 0.45f, 0.12f);
                case Element.Beast: return new Color(0.62f, 0.5f, 0.85f);
                case Element.Thunder: return new Color(1f, 0.88f, 0.2f);
                case Element.Light: return new Color(1f, 0.95f, 0.75f);
                default: return new Color(0.7f, 0.15f, 0.45f);
            }
        }

        public static string Icon(Element e)
        {
            switch (e)
            {
                case Element.Water: return "WATER";
                case Element.Flame: return "FLAME";
                case Element.Beast: return "BEAST";
                case Element.Thunder: return "THUNDER";
                case Element.Light: return "LIGHT";
                default: return "DARK";
            }
        }
    }
}
