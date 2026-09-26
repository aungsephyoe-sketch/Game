using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>Settings: graphics tier, audio volumes, camera shake and damage numbers.</summary>
    public partial class UIManager
    {
        static readonly string[] TierNames = { "LOW", "MEDIUM", "HIGH", "ULTRA" };
        static readonly string[] TierHints =
        {
            "30 FPS · no shadows · fewer particles – older phones",
            "60 FPS · hard shadows · reduced effects – most phones",
            "60 FPS · soft shadows · bloom & colour grading – recent phones",
            "60 FPS · everything maxed · extra lights & particles – flagships / desktop"
        };

        void DrawSettings()
        {
            TopBar("SETTINGS", gm.SettingsReturn);
            DrawSettingsPanel(new Rect(W * 0.5f - 800f, safe.y + 150f, 1600f, H - safe.y - 190f), false);
            if (gm.SettingsReturn == GameScreen.MainMenu &&
                Btn(new Rect(safe.x + 560f, safe.y + 22f, 280f, 66f), Time.unscaledTime - resetArmed < 3f ? "CONFIRM RESET" : "Reset Save", UIStyles.ButtonSmall))
            {
                if (Time.unscaledTime - resetArmed < 3f) { resetArmed = -10f; gm.ResetSave(); }
                else { resetArmed = Time.unscaledTime; Toast("Tap again to erase all progress and restart the story."); }
            }
        }

        /// <summary>Shared by the settings screen and the pause menu.</summary>
        void DrawSettingsPanel(Rect r, bool compact)
        {
            UIStyles.PanelBox(r);
            float x = r.x + 50f, y = r.y + 30f, w = r.width - 100f;
            bool changed = false;

            GUI.Label(new Rect(x, y, w, 50f), "GRAPHICS QUALITY", UIStyles.H2);
            y += 60f;
            float bw = (w - 3 * 20f) / 4f;
            for (int i = 0; i < 4; i++)
            {
                bool sel = (int)GameSettings.Tier == i;
                if (Btn(new Rect(x + i * (bw + 20f), y, bw, 90f), TierNames[i], sel ? UIStyles.ButtonBig : UIStyles.Button))
                {
                    GameSettings.Tier = (GraphicsTier)i;
                    changed = true;
                }
            }
            y += 100f;
            GUI.Label(new Rect(x, y, w, 40f), TierHints[(int)GameSettings.Tier] +
                (GameSettings.DetectTier() == GameSettings.Tier ? "   <color=#7CFF8A>(recommended for this device)</color>" : ""), UIStyles.Small);
            y += compact ? 60f : 80f;

            changed |= VolumeRow(ref y, x, w, "MUSIC", ref GameSettings.MusicVolume);
            changed |= VolumeRow(ref y, x, w, "SOUND EFFECTS", ref GameSettings.SfxVolume);

            GUI.Label(new Rect(x, y + 20f, 420f, 50f), "CAMERA SHAKE", UIStyles.H2);
            string[] shakes = { "OFF", "LOW", "FULL" };
            float[] shakeValues = { 0f, 0.5f, 1f };
            for (int i = 0; i < 3; i++)
            {
                bool sel = Mathf.Approximately(GameSettings.ShakeIntensity, shakeValues[i]);
                if (Btn(new Rect(x + 460f + i * 230f, y, 210f, 80f), shakes[i], sel ? UIStyles.ButtonBig : UIStyles.Button))
                {
                    GameSettings.ShakeIntensity = shakeValues[i];
                    changed = true;
                }
            }
            y += 100f;

            GUI.Label(new Rect(x, y + 20f, 420f, 50f), "DAMAGE NUMBERS", UIStyles.H2);
            if (Btn(new Rect(x + 460f, y, 210f, 80f), GameSettings.ShowDamageNumbers ? "ON" : "OFF",
                GameSettings.ShowDamageNumbers ? UIStyles.ButtonBig : UIStyles.Button))
            {
                GameSettings.ShowDamageNumbers = !GameSettings.ShowDamageNumbers;
                changed = true;
            }

            if (changed) GameSettings.Save();
        }

        bool VolumeRow(ref float y, float x, float w, string label, ref float value)
        {
            bool changed = false;
            GUI.Label(new Rect(x, y + 20f, 420f, 50f), label, UIStyles.H2);
            if (Btn(new Rect(x + 460f, y, 90f, 80f), "–")) { value = Mathf.Clamp01(value - 0.1f); changed = true; }
            UIStyles.Bar(new Rect(x + 570f, y + 25f, 520f, 30f), value, UIStyles.Gold);
            GUI.Label(new Rect(x + 1110f, y + 18f, 120f, 50f), Mathf.RoundToInt(value * 100f) + "%", UIStyles.Body);
            if (Btn(new Rect(x + 1230f, y, 90f, 80f), "+")) { value = Mathf.Clamp01(value + 0.1f); changed = true; }
            y += 100f;
            return changed;
        }
    }
}
