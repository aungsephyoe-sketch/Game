using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>The daily login reward popup over the home screen: spinning rays, gold and diamonds, CLAIM.</summary>
    public partial class UIManager
    {
        float loginShownAt = -1f;

        /// <summary>Home screen, with today's login reward on top (the home buttons wait until it's claimed).</summary>
        void DrawHome()
        {
            var d = gm.Data;
            bool ready = gm.TransitionAlpha < 0.05f;
            // One modal at a time: profile first, then the birthday gift, then today's login reward.
            int modal = 0;
            if (ready)
            {
                if (!d.profileDone) modal = 1;
                else if (ProfileSystem.BirthdayDue(d)) modal = 2;
                else if (LoginRewardSystem.Check(d)) modal = 3;
                else if (SocialOpen) modal = 4;
                else if (!d.homeTourDone && TutorialSystem.Begin(TutorialSystem.Kind.Home)) modal = 5;
                else if (TutorialSystem.Active == TutorialSystem.Kind.Home) modal = 5;
            }
            bool old = GUI.enabled;
            if (modal != 0) GUI.enabled = false;
            DrawMainMenu();
            GUI.enabled = old;
            if (modal == 1) DrawProfileSetup(d);
            else if (modal == 2) DrawBirthday(d);
            else if (modal == 3) DrawLoginReward(d);
            else if (modal == 4) DrawSocial(d);
            else loginShownAt = -1f; // (no speech boxes over the home screen)
        }

        void DrawLoginReward(PlayerData d)
        {
            if (loginShownAt < 0f) { loginShownAt = Time.unscaledTime; gm.Audio.Play("perfect", 0.6f); }
            float t = Time.unscaledTime - loginShownAt;
            float k = Mathf.Clamp01(t / 0.35f);
            k = 1f - Mathf.Pow(1f - k, 3f);
            UIStyles.Rect(new Rect(0f, 0f, W, H), new Color(0f, 0f, 0f, 0.7f * k));

            Vector2 c = new Vector2(W * 0.5f, H * 0.5f - 10f);
            // Slowly turning light rays behind the panel.
            var saved = GUI.matrix;
            for (int i = 0; i < 12; i++)
            {
                GUI.matrix = saved;
                RotateGui(i * 30f + t * 12f, c);
                UIStyles.Rect(new Rect(c.x, c.y - 26f, 620f * k, 52f), new Color(1f, 0.85f, 0.35f, 0.06f));
            }
            GUI.matrix = saved;

            float pw = 940f, ph = 640f;
            float s = Mathf.Lerp(0.7f, 1f, k);
            var panel = new Rect(c.x - pw * 0.5f * s, c.y - ph * 0.5f * s, pw * s, ph * s);
            Round(Offset(panel, 0f, 8f), new Color(0f, 0f, 0f, 0.5f), 26f);
            Round(panel, new Color(0.07f, 0.08f, 0.14f, 0.97f), 26f);
            Round(new Rect(panel.x, panel.y, panel.width, 120f * s), new Color(0.95f, 0.65f, 0.15f, 0.9f), 26f);
            Round(new Rect(panel.x, panel.y + 60f * s, panel.width, 60f * s), new Color(0.95f, 0.65f, 0.15f, 0.9f), 0f);
            RoundFrame(panel, new Color(1f, 0.85f, 0.4f, 0.9f), 3f, 26f);
            if (k < 1f) return;

            UIStyles.Outlined(new Rect(panel.x, panel.y + 14f, pw, 60f), "DAILY LOGIN REWARD", UIStyles.Sized(UIStyles.H1, 48), Color.white, 3f);
            GUI.Label(new Rect(panel.x, panel.y + 70f, pw, 40f), "<color=#3A1E00>Day " + Mathf.Max(1, d.loginDays) + " · a new random reward every day</color>", UIStyles.Sized(UIStyles.Center, 24));

            // Two reward tiles pop in one after the other; the amounts count up.
            float tw = 360f, th = 300f, gap = 60f;
            float tx = c.x - tw - gap * 0.5f, ty = panel.y + 150f;
            for (int i = 0; i < 2; i++)
            {
                float pk = Mathf.Clamp01((t - 0.45f - i * 0.25f) / 0.3f);
                if (pk <= 0f) continue;
                float pop = pk < 1f ? Mathf.Lerp(0.4f, 1.12f, pk) : 1f + 0.02f * Mathf.Sin(Time.unscaledTime * 3f + i);
                var tr = new Rect(tx + i * (tw + gap), ty, tw, th);
                var sr = new Rect(tr.center.x - tw * 0.5f * pop, tr.center.y - th * 0.5f * pop, tw * pop, th * pop);
                Color col = i == 0 ? new Color(1f, 0.75f, 0.2f) : new Color(0.35f, 0.7f, 1f);
                Round(sr, Color.Lerp(new Color(0.1f, 0.1f, 0.16f), col, 0.25f), 20f);
                RoundFrame(sr, col, 3f, 20f);
                Vector2 ic = new Vector2(sr.center.x, sr.y + sr.height * 0.4f);
                float glow = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 4f + i);
                UIStyles.CircleTex(ic, 95f * pop, new Color(col.r, col.g, col.b, 0.12f + 0.08f * glow));
                if (i == 0) CoinIcon(ic, 130f * pop); else DiamondIcon(ic, 150f * pop);
                float count = Mathf.Clamp01((t - 0.6f - i * 0.25f) / 0.8f);
                int amount = i == 0 ? d.loginGold : d.loginDiamonds;
                UIStyles.Outlined(new Rect(sr.x, sr.yMax - 96f, sr.width, 50f), "+" + Mathf.RoundToInt(amount * count).ToString("N0"), UIStyles.Sized(UIStyles.H1, 46), Color.white, 3f);
                GUI.Label(new Rect(sr.x, sr.yMax - 46f, sr.width, 34f), i == 0 ? "<color=#FFD36B>GOLD</color>" : "<color=#7FD8FF>DIAMONDS</color>", UIStyles.Sized(UIStyles.Center, 22));
            }

            if (t > 1.1f && FlatBtn(new Rect(c.x - 220f, panel.yMax - 118f, 440f, 90f), "CLAIM", TileGreen, true, 40))
            {
                int g = d.loginGold, dm = d.loginDiamonds;
                LoginRewardSystem.Claim(d);
                gm.Save();
                gm.Audio.Play("gem", 0.8f);
                gm.Audio.Play("coinSpill", 0.7f);
                Toast("Claimed " + g.ToString("N0") + " gold and " + dm + " diamonds!");
                if (gm.Home != null) gm.Home.Celebrate(new Color(1f, 0.85f, 0.3f));
            }
        }
    }
}
