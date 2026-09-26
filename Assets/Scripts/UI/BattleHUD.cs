using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>In-battle HUD: controls, team portraits, objectives, boss bar, damage numbers, banners, ultimate cut-in.</summary>
    public partial class UIManager
    {
        void DrawBattleHUD()
        {
            var b = BattleController.Current;
            if (b == null || b.Team == null || b.Mission == null) return;
            var cam = Camera.main;

            DrawEnemyBars(cam);
            DrawDamageNumbers(cam);
            DrawObjectives(b);
            DrawPortraits(b);
            DrawBossBar(b);
            DrawCombo(b);
            DrawControls(b);
            DrawCallouts();

            // Pause button.
            var pr = HudLayout.Pause;
            UIStyles.Rect(pr, new Color(0f, 0f, 0f, 0.45f));
            GUI.Label(pr, "II", UIStyles.Sized(UIStyles.Center, 40));

            if (TimeController.Paused) DrawPauseMenu();
        }

        void DrawObjectives(BattleController b)
        {
            var m = b.Mission;
            var r = new Rect(safe.x + 24f, safe.y + 20f, 520f, 214f);
            UIStyles.Rect(r, new Color(0f, 0f, 0f, 0.45f));
            int secs = Mathf.FloorToInt(m.Elapsed);
            string stage = m.InBossStage ? "<color=#FF6060>BOSS</color>" : "WAVE " + Mathf.Min(m.WaveIndex + 1, m.WaveCount) + "/" + m.WaveCount;
            GUI.Label(new Rect(r.x + 18f, r.y + 10f, r.width - 36f, 40f), m.Def.id + "  " + stage + "   <color=#FFD36B>" + secs / 60 + ":" + (secs % 60).ToString("00") + "</color>",
                UIStyles.Sized(UIStyles.H2, 30));
            for (int i = 0; i < 3; i++)
            {
                bool met = m.ObjectiveMet(i);
                string mark = met ? "<color=#7CFF8A>☑</color>" : "☐";
                GUI.Label(new Rect(r.x + 18f, r.y + 58f + i * 50f, r.width - 36f, 44f), mark + " " + m.ObjectiveLabel(i) + "  <color=#AAAAAA>" + m.ObjectiveProgress(i) + "</color>",
                    UIStyles.Sized(UIStyles.Body, 25));
            }
        }

        void DrawPortraits(BattleController b)
        {
            var team = b.Team;
            for (int i = 0; i < team.Members.Count && i < 3; i++)
            {
                var m = team.Members[i];
                var r = HudLayout.Portrait(i);
                bool active = i == team.ActiveIndex;
                Color el = ElementChart.ColorOf(m.Element);
                UIStyles.Rect(r, active ? new Color(el.r * 0.35f, el.g * 0.35f, el.b * 0.35f, 0.85f) : new Color(0f, 0f, 0f, 0.55f));
                if (active) UIStyles.Frame(r, el, 4f);
                UIStyles.Rect(new Rect(r.x, r.y, 12f, r.height), m.Def.haoriColor);
                string name = m.Def.displayName.Split(' ')[0].ToUpper();
                GUI.Label(new Rect(r.x + 24f, r.y + 6f, r.width - 30f, 36f), name + "  <size=20><color=#AAAAAA>Lv." + m.Owned.level + "</color></size>", UIStyles.Sized(UIStyles.H2, 28));
                if (!m.IsAlive)
                {
                    UIStyles.Colored(new Rect(r.x + 24f, r.y + 44f, r.width - 30f, 40f), "DOWN", UIStyles.Sized(UIStyles.H2, 30), UIStyles.Bad);
                    continue;
                }
                float hp = m.Health.Normalized;
                UIStyles.Bar(new Rect(r.x + 24f, r.y + 48f, r.width - 40f, 22f), hp, hp > 0.3f ? new Color(0.35f, 0.9f, 0.45f) : UIStyles.Bad);
                UIStyles.Bar(new Rect(r.x + 24f, r.y + 76f, r.width - 40f, 14f), m.UltGauge / PlayerCharacter.UltMax, m.UltReady ? UIStyles.Gold : el * 0.9f);
                if (!active && team.SwitchTimer > 0f)
                    UIStyles.Rect(new Rect(r.x, r.y, r.width * team.SwitchTimer / TeamSystem.SwitchCooldown, r.height), new Color(0f, 0f, 0f, 0.4f));
                if (!active && m.UltReady) UIStyles.Colored(new Rect(r.xMax - 110f, r.y + 6f, 100f, 30f), "ULT", UIStyles.Sized(UIStyles.Right, 24), UIStyles.Gold);
            }
        }

        void DrawBossBar(BattleController b)
        {
            var boss = b.Mission.Boss;
            if (boss == null || !boss.IsAlive) return;
            // Sits between the objectives panel (left) and the pause button (right).
            float w = Mathf.Clamp(W - 1200f, 420f, 900f);
            var r = new Rect(W * 0.5f - w * 0.5f, safe.y + 30f, w, 34f);
            UIStyles.Outlined(new Rect(r.x, r.y + 38f, r.width, 40f), boss.Def.displayName + (boss.DestructiveMode ? "  <color=#7FB8FF>DESTRUCTIVE MODE</color>" : "") +
                (boss.Exhausted ? "  <color=#80FFFF>BREAK!</color>" : ""), UIStyles.Sized(UIStyles.Center, 28), Color.white, 2f);
            UIStyles.Bar(r, boss.Health.Normalized, boss.Exhausted ? new Color(0.4f, 1f, 1f) : new Color(0.85f, 0.12f, 0.15f));
            if (boss.Def.phaseThresholds != null)
                foreach (var t in boss.Def.phaseThresholds)
                    UIStyles.Rect(new Rect(r.x + r.width * t - 2f, r.y - 4f, 4f, r.height + 8f), UIStyles.Gold);
            UIStyles.Colored(new Rect(r.xMax - 200f, r.y + 38f, 200f, 40f), "PHASE " + (boss.Phase + 1) + "/" + boss.PhaseCount, UIStyles.Sized(UIStyles.Right, 24), UIStyles.Gold);
        }

        void DrawCombo(BattleController b)
        {
            if (b.Combo < 3) return;
            float pulse = 1f + 0.15f * Mathf.Clamp01(b.ComboTimer - 1.9f) * 3f;
            var style = UIStyles.Sized(UIStyles.Right, Mathf.RoundToInt(64f * pulse));
            UIStyles.Outlined(new Rect(safe.xMax - 560f, H * 0.32f, 520f, 90f), b.Combo + " <size=34>HITS</size>", style, UIStyles.Gold, 3f);
        }

        void DrawControls(BattleController b)
        {
            var pc = b.Team.Active;
            var controls = gm.Controls;
            if (pc == null || controls == null) return;
            Color el = ElementChart.ColorOf(pc.Element);

            // Joystick.
            UIStyles.CircleTex(controls.JoystickCenter, HudLayout.JoystickRadius, new Color(1f, 1f, 1f, controls.JoystickActive ? 0.18f : 0.08f));
            UIStyles.CircleTex(controls.JoystickCenter, HudLayout.JoystickRadius, new Color(1f, 1f, 1f, 0.35f), UIStyles.Ring);
            UIStyles.CircleTex(controls.JoystickKnob, 60f, new Color(1f, 1f, 1f, controls.JoystickActive ? 0.55f : 0.3f));

            // Attack (shows charge).
            var atk = HudLayout.Attack;
            DrawButton(atk, "ATTACK", pc.ChargeAmount > 0f ? "CHARGE" : (pc.ComboStep > 0 ? "COMBO " + pc.ComboStep : ""), 0f, controls.Pressed[0], el);
            if (pc.ChargeAmount > 0f) UIStyles.CircleFill(atk.center, atk.radius, pc.ChargeAmount, new Color(el.r, el.g, el.b, 0.45f));

            DrawButton(HudLayout.Dodge, "DODGE", "", 0f, controls.Pressed[1], new Color(0.7f, 0.8f, 1f));

            for (int i = 0; i < 3; i++)
            {
                var ab = pc.Def.skills[i];
                float cd = pc.Cooldowns[i];
                string shortName = ab.name.Contains(":") ? ab.name.Substring(ab.name.IndexOf(':') + 1).Trim() : ab.name;
                DrawButton(HudLayout.Skill(i), (i + 1).ToString(), shortName, cd > 0f ? cd / ab.cooldown : 0f, controls.Pressed[2 + i], el, cd);
            }

            var ult = HudLayout.Ultimate;
            float gauge = pc.UltGauge / PlayerCharacter.UltMax;
            UIStyles.CircleTex(ult.center, ult.radius, new Color(0f, 0f, 0f, 0.55f));
            UIStyles.CircleFill(ult.center, ult.radius, gauge, new Color(el.r, el.g, el.b, pc.UltReady ? 0.9f : 0.45f));
            float ring = pc.UltReady ? 0.7f + 0.3f * Mathf.Sin(Time.unscaledTime * 8f) : 0.4f;
            UIStyles.CircleTex(ult.center, ult.radius + (pc.UltReady ? 6f * ring : 0f), new Color(1f, 0.85f, 0.4f, ring), UIStyles.Ring);
            UIStyles.Outlined(new Rect(ult.center.x - ult.radius, ult.center.y - 30f, ult.radius * 2f, 40f), "ULT", UIStyles.Sized(UIStyles.Center, 34), Color.white, 2f);
            GUI.Label(new Rect(ult.center.x - ult.radius, ult.center.y + 8f, ult.radius * 2f, 30f), pc.UltReady ? "READY" : Mathf.FloorToInt(gauge * 100f) + "%",
                UIStyles.Sized(UIStyles.CenterSmall, 22));
        }

        void DrawButton(HudLayout.Circle c, string label, string sub, float cooldown01, float pressed, Color accent, float seconds = 0f)
        {
            float r = c.radius * (1f - pressed * 0.08f);
            UIStyles.CircleTex(c.center, r, new Color(0f, 0f, 0f, 0.5f));
            UIStyles.CircleTex(c.center, r, new Color(accent.r, accent.g, accent.b, 0.25f + pressed * 0.4f));
            UIStyles.CircleTex(c.center, r, new Color(1f, 1f, 1f, 0.55f), UIStyles.Ring);
            if (cooldown01 > 0f)
            {
                UIStyles.CircleFill(c.center, r, cooldown01, new Color(0f, 0f, 0f, 0.6f));
                UIStyles.Outlined(new Rect(c.center.x - r, c.center.y - 26f, r * 2f, 50f), Mathf.CeilToInt(seconds).ToString(), UIStyles.Sized(UIStyles.Center, 40), Color.white, 2f);
            }
            else UIStyles.Outlined(new Rect(c.center.x - r, c.center.y - (sub == "" ? 20f : 34f), r * 2f, 40f), label, UIStyles.Sized(UIStyles.Center, label.Length > 2 ? 28 : 38), Color.white, 2f);
            if (sub != "") GUI.Label(new Rect(c.center.x - r - 20f, c.center.y + 6f, r * 2f + 40f, 50f), sub, UIStyles.Sized(UIStyles.CenterSmall, 18));
        }

        void DrawEnemyBars(Camera cam)
        {
            if (cam == null) return;
            float s = HudLayout.Scale;
            foreach (var c in Combatant.All)
            {
                var e = c as EnemyController;
                if (e == null || !e.IsAlive || e.IsBoss || e.Health.Normalized >= 0.999f) continue;
                Vector3 sp = cam.WorldToScreenPoint(e.Position + Vector3.up * (2.4f * e.Def.scale));
                if (sp.z < 0f) continue;
                var p = new Vector2(sp.x / s, (Screen.height - sp.y) / s);
                UIStyles.Bar(new Rect(p.x - 50f, p.y, 100f, 12f), e.Health.Normalized, new Color(0.9f, 0.2f, 0.2f));
            }
        }

        void DrawDamageNumbers(Camera cam)
        {
            if (cam == null) return;
            float s = HudLayout.Scale;
            float now = Time.unscaledTime;
            for (int i = DamageNumbers.Entries.Count - 1; i >= 0; i--)
            {
                var e = DamageNumbers.Entries[i];
                float age = now - e.born;
                if (age > DamageNumbers.Lifetime)
                {
                    DamageNumbers.Entries.RemoveAt(i);
                    continue;
                }
                Vector3 sp = cam.WorldToScreenPoint(e.worldPos);
                if (sp.z < 0f) continue;
                var p = new Vector2(sp.x / s + e.xJitter, (Screen.height - sp.y) / s - age * 90f);
                float pop = age < 0.08f ? 1.35f - age * 4f : 1f;
                var col = e.color;
                col.a = 1f - Mathf.Clamp01((age - 0.55f) / 0.35f);
                var style = UIStyles.Sized(UIStyles.Center, Mathf.RoundToInt(e.size * pop));
                UIStyles.Outlined(new Rect(p.x - 200f, p.y - 40f, 400f, 80f), e.text, style, col, 3f);
                if (!string.IsNullOrEmpty(e.tag))
                    UIStyles.Outlined(new Rect(p.x - 200f, p.y - 80f, 400f, 50f), e.tag, UIStyles.Sized(UIStyles.Center, 24),
                        e.tag == "WEAK!" ? new Color(1f, 0.6f, 0.2f, col.a) : new Color(0.6f, 0.6f, 0.7f, col.a), 2f);
            }
        }

        void DrawCallouts()
        {
            float now = Time.unscaledTime;

            // Banner (mission start, waves, boss phases).
            float bt = now - bannerTime;
            if (bt < 2.4f)
            {
                float a = Mathf.Clamp01(bt * 5f) * Mathf.Clamp01((2.4f - bt) * 3f);
                float slide = (1f - Mathf.Clamp01(bt * 4f)) * 200f;
                UIStyles.Rect(new Rect(0f, H * 0.36f, W, 170f), new Color(0f, 0f, 0f, 0.5f * a));
                UIStyles.Rect(new Rect(0f, H * 0.36f, W, 4f), new Color(UIStyles.Crimson.r, UIStyles.Crimson.g, UIStyles.Crimson.b, a));
                UIStyles.Rect(new Rect(0f, H * 0.36f + 166f, W, 4f), new Color(UIStyles.Crimson.r, UIStyles.Crimson.g, UIStyles.Crimson.b, a));
                bool warn = bannerTitle == "WARNING" || bannerTitle == "ANNIHILATION" || bannerTitle == "DEFEAT";
                UIStyles.Outlined(new Rect(slide, H * 0.36f + 10f, W, 100f), bannerTitle, UIStyles.Sized(UIStyles.Big, 80),
                    new Color(warn ? 1f : 1f, warn ? 0.35f : 0.9f, warn ? 0.35f : 0.7f, a), 4f);
                if (bannerSub != "")
                    UIStyles.Outlined(new Rect(-slide, H * 0.36f + 108f, W, 50f), bannerSub, UIStyles.Sized(UIStyles.Center, 30), new Color(1f, 1f, 1f, a), 2f);
            }

            // Skill name callout.
            float ct = now - calloutTime;
            if (ct < 1.4f)
            {
                float a = Mathf.Clamp01((1.4f - ct) * 3f);
                UIStyles.Outlined(new Rect(0f, H - 170f, W, 60f), callout, UIStyles.Sized(UIStyles.Center, 38), new Color(calloutColor.r, calloutColor.g, calloutColor.b, a), 3f);
            }

            // Ultimate cut-in.
            float ut = now - ultTime;
            if (ut < 1.1f)
            {
                float a = Mathf.Clamp01(ut * 8f) * Mathf.Clamp01((1.1f - ut) * 4f);
                float x = Mathf.Lerp(-W * 0.3f, 0f, Mathf.Clamp01(ut * 5f));
                UIStyles.Rect(new Rect(0f, 0f, W, H), new Color(0f, 0f, 0f, 0.35f * a));
                var band = new Rect(x, H * 0.3f, W * 1.3f, 260f);
                UIStyles.Rect(band, new Color(ultColor.r * 0.4f, ultColor.g * 0.4f, ultColor.b * 0.4f, 0.9f * a));
                UIStyles.Rect(new Rect(band.x, band.y, band.width, 8f), new Color(ultColor.r, ultColor.g, ultColor.b, a));
                UIStyles.Rect(new Rect(band.x, band.yMax - 8f, band.width, 8f), new Color(ultColor.r, ultColor.g, ultColor.b, a));
                UIStyles.Outlined(new Rect(x + 120f, band.y + 30f, W, 80f), ultCharacter.ToUpper(), UIStyles.Sized(UIStyles.H1, 60), new Color(1f, 1f, 1f, a), 3f);
                UIStyles.Outlined(new Rect(x + 120f, band.y + 120f, W, 110f), ultName, UIStyles.Sized(UIStyles.H1, 76), new Color(1f, 0.85f, 0.4f, a), 4f);
            }

            float tt = now - ultTotalTime;
            if (tt < 2.2f)
            {
                float a = Mathf.Clamp01(tt * 6f) * Mathf.Clamp01((2.2f - tt) * 2f);
                float scale = 1f + Mathf.Max(0f, 0.3f - tt) * 1.5f;
                UIStyles.Outlined(new Rect(0f, H * 0.2f, W, 50f), "TOTAL DAMAGE", UIStyles.Sized(UIStyles.Center, 36), new Color(1f, 1f, 1f, a), 3f);
                UIStyles.Outlined(new Rect(0f, H * 0.2f + 50f, W, 120f), Mathf.RoundToInt(ultTotal).ToString("N0"),
                    UIStyles.Sized(UIStyles.Big, Mathf.RoundToInt(96f * scale)), new Color(1f, 0.82f, 0.3f, a), 4f);
            }
        }

        void DrawPauseMenu()
        {
            UIStyles.Rect(new Rect(0f, 0f, W, H), new Color(0f, 0f, 0f, 0.6f));
            var r = new Rect(W * 0.5f - 450f, H * 0.5f - 330f, 900f, 660f);
            UIStyles.PanelBox(r);
            GUI.Label(new Rect(r.x, r.y + 30f, r.width, 80f), "PAUSED", UIStyles.Big);
            GUI.Label(new Rect(r.x + 60f, r.y + 130f, r.width - 120f, 200f),
                "Keyboard: WASD move · J attack (hold = charge) · Space dodge\n1 2 3 skills · R ultimate · Q/E switch slayer · Esc pause", UIStyles.CenterSmall);
            if (Btn(new Rect(r.x + 150f, r.y + 330f, r.width - 300f, 110f), "RESUME", UIStyles.ButtonBig)) gm.TogglePause();
            if (Btn(new Rect(r.x + 150f, r.y + 470f, r.width - 300f, 110f), "RETREAT")) gm.RetreatFromBattle();
        }
    }
}
