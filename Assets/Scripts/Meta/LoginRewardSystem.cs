using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// A random login reward every day: 500–5,000 gold and 50–200 diamonds, rolled the first time the game is
    /// opened that day and claimed from a popup on the home screen.
    /// </summary>
    public static class LoginRewardSystem
    {
        public const int MinGold = 500, MaxGold = 5000, MinDiamonds = 50, MaxDiamonds = 200;

        static string Today() { return System.DateTime.Now.ToString("yyyyMMdd"); }

        /// <summary>Rolls today's reward if it hasn't been rolled yet. Returns true when a reward is waiting.</summary>
        public static bool Check(PlayerData d)
        {
            if (d == null) return false;
            string today = Today();
            if (d.loginKey != today)
            {
                d.loginKey = today;
                d.loginDays++;
                d.loginGold = Mathf.RoundToInt(Random.Range(MinGold, MaxGold + 1) / 50f) * 50;
                d.loginDiamonds = Mathf.RoundToInt(Random.Range(MinDiamonds, MaxDiamonds + 1) / 5f) * 5;
                d.loginGold = Mathf.Clamp(d.loginGold, MinGold, MaxGold);
                d.loginDiamonds = Mathf.Clamp(d.loginDiamonds, MinDiamonds, MaxDiamonds);
                d.loginPending = true;
                if (GameManager.Instance != null) GameManager.Instance.Save();
            }
            return d.loginPending;
        }

        public static void Claim(PlayerData d)
        {
            if (d == null || !d.loginPending) return;
            d.coins += d.loginGold;
            d.crystals += d.loginDiamonds;
            d.loginPending = false;
        }
    }
}
