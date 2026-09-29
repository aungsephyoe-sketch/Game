using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// Elemental advantage: a cycle of five — Water beats Fire, Fire beats Earth, Earth beats Wind, Wind beats Thunder,
    /// Thunder beats Water. Hitting the element you beat deals 15% more; hitting the one that beats you deals 15%
    /// less. Light and Dark are neutral: no advantage or disadvantage against anything.
    /// </summary>
    public static class ElementChart
    {
        public const float Advantage = 1.15f;
        public const float Disadvantage = 0.85f;

        public static readonly Element[] Cycle = { Element.Water, Element.Flame, Element.Earth, Element.Beast, Element.Thunder };

        public static bool Neutral(Element e) { return e == Element.Light || e == Element.Dark; }

        /// <summary>The element this element is strong against (Light and Dark: none — returns itself).</summary>
        public static Element StrongAgainst(Element e)
        {
            switch (e)
            {
                case Element.Water: return Element.Flame;
                case Element.Flame: return Element.Earth;
                case Element.Earth: return Element.Beast;
                case Element.Beast: return Element.Thunder;
                case Element.Thunder: return Element.Water;
                default: return e;
            }
        }

        /// <summary>The element that is strong against this one (Light and Dark: none — returns itself).</summary>
        public static Element WeakTo(Element e)
        {
            foreach (var a in Cycle) if (a != e && StrongAgainst(a) == e) return a;
            return e;
        }

        public static float Multiplier(Element attacker, Element defender)
        {
            if (Neutral(attacker) || Neutral(defender) || attacker == defender) return 1f;
            if (StrongAgainst(attacker) == defender) return Advantage;
            if (StrongAgainst(defender) == attacker) return Disadvantage;
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
                case Element.Earth: return new Color(0.55f, 0.78f, 0.28f);
                default: return new Color(0.7f, 0.15f, 0.45f);
            }
        }

        /// <summary>The name players see (Beast is Wind; Flame is Fire).</summary>
        public static string Name(Element e)
        {
            switch (e)
            {
                case Element.Water: return "Water";
                case Element.Flame: return "Fire";
                case Element.Beast: return "Wind";
                case Element.Thunder: return "Thunder";
                case Element.Light: return "Light";
                case Element.Earth: return "Earth";
                default: return "Dark";
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
                case Element.Earth: return "EARTH";
                default: return "DARK";
            }
        }

        /// <summary>"Water types +15% · Earth types −15%" for a monster of this element (null for Light/Dark).</summary>
        public static string MatchupHint(Element monster)
        {
            if (Neutral(monster)) return null;
            return Name(WeakTo(monster)) + " types +15%  ·  " + Name(StrongAgainst(monster)) + " types −15%";
        }
    }
}
