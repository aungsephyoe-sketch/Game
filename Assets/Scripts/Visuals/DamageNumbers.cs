using System.Collections.Generic;
using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>Floating damage numbers, drawn by the battle HUD.</summary>
    public static class DamageNumbers
    {
        public class Entry
        {
            public Vector3 worldPos;
            public string text;
            public Color color;
            public float size;
            public float born;
            public float xJitter;
            public string tag;
        }

        public const float Lifetime = 0.9f;
        public static readonly List<Entry> Entries = new List<Entry>();

        public static void Spawn(Vector3 pos, float amount, bool crit, float elementMultiplier, bool playerHit)
        {
            if (!GameSettings.ShowDamageNumbers && !playerHit) return;
            if (Entries.Count > 60) Entries.RemoveAt(0);
            var e = new Entry
            {
                worldPos = pos,
                text = Mathf.RoundToInt(amount).ToString("N0"),
                born = Time.unscaledTime,
                xJitter = Random.Range(-30f, 30f),
                size = crit ? 54f : 38f,
                color = playerHit ? new Color(1f, 0.3f, 0.3f) : (crit ? new Color(1f, 0.85f, 0.2f) : Color.white)
            };
            if (!playerHit && elementMultiplier > 1.01f) e.tag = "WEAK!";
            else if (!playerHit && elementMultiplier < 0.99f) e.tag = "RESIST";
            Entries.Add(e);
        }

        public static void SpawnText(Vector3 pos, string text, Color color, float size = 44f)
        {
            Entries.Add(new Entry { worldPos = pos, text = text, color = color, size = size, born = Time.unscaledTime });
        }

        public static void Clear() { Entries.Clear(); }
    }
}
