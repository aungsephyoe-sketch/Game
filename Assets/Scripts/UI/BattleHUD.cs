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

            DrawLowHealth(b);
            DrawEnemyBars(cam);
            DrawDamageNumbers(cam);
            DrawImpactFrame();
            if (!b.CinematicLock)
            {
                DrawObjectives(b);
                DrawRouteStrip(b);
            }
            DrawWaypoint(b, cam);
            DrawAreaTitle();
            DrawPortraits(b);
            DrawBossBar(b);
            DrawCombo(b);
            DrawPlayerBars(b);
            DrawControls(b);
            DrawCallouts();
            DrawLockMarker(cam);
            DrawSubtitle();
            DrawBossIntro();

            // Pause button.
            var pr = HudLayout.Pause;
            UIStyles.Rect(pr, new Color(0f, 0f, 0f, 0.45f));
            GUI.Label(pr, "II", UIStyles.Sized(UIStyles.Center, 40));

            if (TimeController.Paused) DrawPauseMenu();
        }

        void DrawLockMarker(Camera cam)
        {
            var t = PlayerCharacter.LockTarget;
            if (t == null || !t.IsAlive || cam == null) return;
            float s = HudLayout.Scale;
            var e = t as EnemyController;
            float h = e != null ? 1.3f * e.Def.scale : 1.3f;
            Vector3 sp = cam.WorldToScreenPoint(t.Position + Vector3.up * h);
            if (sp.z < 0f) return;
            var p = new Vector2(sp.x / s, (Screen.height - sp.y) / s);
            float r = 46f + Mathf.Sin(Time.unscaledTime * 6f) * 5f;
            var old = GUI.color;
            GUI.color = new Color(1f, 0.25f, 0.3f, 0.9f);
            GUI.DrawTexture(new Rect(p.x - r, p.y - r, r * 2f, r * 2f), UIStyles.Ring);
            GUI.color = old;
            UIStyles.Rect(new Rect(p.x - 3f, p.y - r - 14f, 6f, 16f), UIStyles.Crimson);
            UIStyles.Rect(new Rect(p.x - 3f, p.y + r - 2f, 6f, 16f), UIStyles.Crimson);
        }

        void DrawSubtitle()
        {
            float age = Time.unscaledTime - subTime;
            if (age > 4.2f || string.IsNullOrEmpty(subText)) return;
            float a = Mathf.Clamp01(age / 0.2f) * Mathf.Clamp01((4.2f - age) / 0.4f);
            var r = new Rect(W * 0.5f - 700f, H - 400f, 1400f, 110f);
            UIStyles.Rect(r, new Color(0f, 0f, 0f, 0.6f * a));
            Color c = SpeakerColor(subSpeaker);
            UIStyles.Colored(new Rect(r.x, r.y + 6f, r.width, 40f), subSpeaker, UIStyles.Sized(UIStyles.Center, 28), new Color(c.r, c.g, c.b, a));
            UIStyles.Colored(new Rect(r.x + 20f, r.y + 46f, r.width - 40f, 60f), subText, UIStyles.Sized(UIStyles.Center, 32), new Color(1f, 1f, 1f, a));
        }

        void DrawBossIntro()
        {
            float age = Time.unscaledTime - bossIntroTime;
            if (bossIntro == null || age > 3.2f) return;
            float a = Mathf.Clamp01(age / 0.25f) * Mathf.Clamp01((3.2f - age) / 0.5f);
            float slide = 1f - Mathf.Pow(1f - Mathf.Clamp01(age / 0.4f), 3f);
            // Letterbox + diagonal name card.
            UIStyles.Rect(new Rect(0f, 0f, W, 110f * a), Color.black);
            UIStyles.Rect(new Rect(0f, H - 110f * a, W, 110f * a), Color.black);
            Color acc = bossIntro.accentColor;
            var band = new Rect(-200f + (1f - slide) * -W, H * 0.62f, W + 400f, 170f);
            UIStyles.Rect(band, new Color(0f, 0f, 0f, 0.7f * a));
            UIStyles.Rect(new Rect(band.x, band.y, band.width, 4f), new Color(acc.r, acc.g, acc.b, a));
            UIStyles.Rect(new Rect(band.x, band.yMax - 4f, band.width, 4f), new Color(acc.r, acc.g, acc.b, a));
            float x = safe.x + 120f + (1f - slide) * -600f;
            UIStyles.Colored(new Rect(x, band.y + 14f, 1400f, 44f), bossIntro.bossTitle, UIStyles.Sized(UIStyles.H2, 32), new Color(acc.r, acc.g, acc.b, a));
            UIStyles.Outlined(new Rect(x, band.y + 52f, 1600f, 110f), bossIntro.displayName.ToUpper(), UIStyles.Sized(UIStyles.Title, 90), new Color(1f, 1f, 1f, a), 4f);
        }

        void DrawObjectives(BattleController b)
        {
            var m = b.Mission;
            var r = new Rect(safe.x + 24f, safe.y + 20f, 540f, 214f);
            UIStyles.Rect(r, new Color(0f, 0f, 0f, 0.42f));
            UIStyles.Rect(new Rect(r.x, r.y, 4f, r.height), UIStyles.Gold);
            int secs = Mathf.FloorToInt(m.Elapsed);
            GUI.Label(new Rect(r.x + 18f, r.y + 8f, r.width - 36f, 30f), "<color=#FFD36B>CURRENT OBJECTIVE</color>   <color=#999999>" + m.Def.id + "  " + secs / 60 + ":" + (secs % 60).ToString("00") + "</color>",
                UIStyles.Sized(UIStyles.Small, 20));
            if (m.Def.training)
            {
                GUI.Label(new Rect(r.x + 18f, r.y + 42f, r.width - 36f, 44f), "⚔ Practise freely", UIStyles.Sized(UIStyles.H2, 30));
                GUI.Label(new Rect(r.x + 18f, r.y + 88f, r.width - 36f, 36f), "<color=#AAAAAA>Pause → Retreat to leave</color>", UIStyles.Sized(UIStyles.Body, 22));
                return;
            }
            string title = string.IsNullOrEmpty(m.ObjectiveTitle) ? m.Def.name : m.ObjectiveTitle;
            GUI.Label(new Rect(r.x + 18f, r.y + 38f, r.width - 36f, 44f), "⚔ " + title, UIStyles.Sized(UIStyles.H2, 30));
            string detail = "";
            var a = b.Team.Active;
            if (m.ObjectiveTarget.HasValue && a != null)
                detail = "Distance: " + Mathf.Max(0, Mathf.RoundToInt((Journey.Flat(m.ObjectiveTarget.Value) - Journey.Flat(a.Position)).magnitude)) + " m";
            else if (m.StageTotal > 0)
                detail = m.StageKills + " / " + m.StageTotal + " defeated";
            else if (m.Boss != null && m.Boss.IsAlive)
                detail = "Phase " + (m.Boss.Phase + 1) + " / " + m.Boss.PhaseCount;
            GUI.Label(new Rect(r.x + 18f, r.y + 84f, r.width - 36f, 36f), "<color=#DDDDDD>" + detail + "</color>", UIStyles.Sized(UIStyles.Body, 25));
            // The three star objectives, compact.
            for (int i = 0; i < 3; i++)
            {
                bool met = m.ObjectiveMet(i);
                GUI.Label(new Rect(r.x + 18f, r.y + 124f + i * 28f, r.width - 36f, 28f), (met ? "<color=#FFD36B>★</color> " : "<color=#777777>☆</color> ") +
                    "<color=#BBBBBB>" + m.ObjectiveLabel(i) + "  " + m.ObjectiveProgress(i) + "</color>", UIStyles.Sized(UIStyles.Small, 19));
            }
        }

        /// <summary>The journey at a glance: every place on the road, where the team is, and what waits at the end.</summary>
        void DrawRouteStrip(BattleController b)
        {
            var j = b.Journey;
            var m = b.Mission;
            if (j == null || m.InBossStage) return;
            int n = j.places.Count;
            float w = Mathf.Min(760f, W - 1300f);
            if (w < 360f) w = 360f;
            float x0 = W * 0.5f - w * 0.5f, y = safe.y + 40f;
            int current = m.StageIndex < j.stages.Count ? j.stages[Mathf.Min(m.StageIndex, j.stages.Count - 1)].place : n - 1;
            bool travelling = m.ObjectiveTarget.HasValue;
            UIStyles.Rect(new Rect(x0 - 20f, y - 26f, w + 40f, 84f), new Color(0f, 0f, 0f, 0.3f));
            for (int i = 0; i < n; i++)
            {
                float x = x0 + (n > 1 ? w * i / (n - 1) : w * 0.5f);
                if (i < n - 1)
                {
                    float x2 = x0 + w * (i + 1) / (n - 1);
                    UIStyles.Rect(new Rect(x, y - 2f, x2 - x, 4f), i < current ? UIStyles.Gold : new Color(1f, 1f, 1f, 0.25f));
                }
                bool done = i < current || (i == current && !travelling);
                bool here = i == current;
                bool boss = j.places[i].isBossArena;
                float rad = here ? 13f + Mathf.Sin(Time.unscaledTime * 5f) * 2f : 9f;
                Color c = boss ? UIStyles.Crimson : done ? UIStyles.Gold : new Color(0.6f, 0.6f, 0.65f);
                UIStyles.CircleTex(new Vector2(x, y), rad, c);
                if (boss) GUI.Label(new Rect(x - 20f, y - 20f, 40f, 40f), "☠", UIStyles.Sized(UIStyles.Center, 22));
                if (here || i == n - 1)
                    GUI.Label(new Rect(x - 150f, y + 14f, 300f, 30f), (here ? "<color=#FFD36B>" : "<color=#FF8080>") + j.places[i].name + "</color>", UIStyles.Sized(UIStyles.Center, 18));
            }
        }

        /// <summary>A diamond over the objective when it's on screen, an edge arrow when it isn't.</summary>
        void DrawWaypoint(BattleController b, Camera cam)
        {
            var m = b.Mission;
            var a = b.Team.Active;
            if (cam == null || a == null || !m.ObjectiveTarget.HasValue || b.CinematicLock) return;
            Vector3 target = m.ObjectiveTarget.Value + Vector3.up * 3f;
            float s = HudLayout.Scale;
            Vector3 sp = cam.WorldToScreenPoint(target);
            var p = new Vector2(sp.x / s, (Screen.height - sp.y) / s);
            int dist = Mathf.RoundToInt((Journey.Flat(target) - Journey.Flat(a.Position)).magnitude);
            bool onScreen = sp.z > 0f && p.x > 60f && p.x < W - 60f && p.y > 60f && p.y < H - 60f;
            float bob = Mathf.Sin(Time.unscaledTime * 4f) * 6f;
            if (onScreen)
            {
                UIStyles.Outlined(new Rect(p.x - 40f, p.y - 40f + bob, 80f, 60f), "◆", UIStyles.Sized(UIStyles.Center, 40), UIStyles.Gold, 2f);
                GUI.Label(new Rect(p.x - 80f, p.y + 14f + bob, 160f, 30f), dist + " m", UIStyles.Sized(UIStyles.Center, 22));
                return;
            }
            // Edge arrow pointing toward the objective (from the player's screen position).
            Vector3 ps = cam.WorldToScreenPoint(a.Position);
            Vector2 from = new Vector2(ps.x / s, (Screen.height - ps.y) / s);
            Vector2 dir = p - from;
            if (sp.z < 0f) dir = -dir;
            if (dir.sqrMagnitude < 1f) return;
            dir.Normalize();
            Vector2 at = from + dir * 190f;
            float ang = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + 90f;
            var old = GUI.matrix;
            GUIUtility.RotateAroundPivot(ang, at * HudLayout.Scale);
            UIStyles.Outlined(new Rect(at.x - 30f, at.y - 34f, 60f, 60f), "▲", UIStyles.Sized(UIStyles.Center, 40), UIStyles.Gold, 2f);
            GUI.matrix = old;
            GUI.Label(new Rect(at.x - 80f, at.y + 22f, 160f, 30f), dist + " m", UIStyles.Sized(UIStyles.Center, 20));
        }

        void DrawAreaTitle()
        {
            float age = Time.unscaledTime - areaTime;
            if (string.IsNullOrEmpty(areaName) || age > 3.6f) return;
            float a = Mathf.Clamp01(age / 0.5f) * Mathf.Clamp01((3.6f - age) / 0.8f);
            float spread = Mathf.Lerp(0.6f, 1f, Mathf.Clamp01(age / 0.8f));
            float y = H * 0.24f;
            UIStyles.Rect(new Rect(W * 0.5f - 420f * spread, y + 78f, 840f * spread, 2f), new Color(1f, 0.85f, 0.5f, a));
            UIStyles.Outlined(new Rect(0f, y, W, 80f), areaName.ToUpper(), UIStyles.Sized(UIStyles.Title, 64), new Color(1f, 0.95f, 0.85f, a), 3f);
            UIStyles.Colored(new Rect(0f, y + 84f, W, 40f), areaSub, UIStyles.Sized(UIStyles.Center, 26), new Color(0.85f, 0.85f, 0.9f, a));
        }

        void DrawPortraits(BattleController b)
        {
            var team = b.Team;
            for (int i = 0; i < team.Members.Count && i < 4; i++)
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
            string rank = b.Combo >= 100 ? "LEGENDARY" : b.Combo >= 50 ? "AWESOME!" : b.Combo >= 25 ? "GREAT!" : b.Combo >= 10 ? "GOOD" : "";
            if (rank != "")
            {
                Color rc = b.Combo >= 100 ? new Color(1f, 0.4f, 1f) : b.Combo >= 50 ? new Color(1f, 0.45f, 0.3f) : b.Combo >= 25 ? new Color(0.4f, 0.9f, 1f) : Color.white;
                UIStyles.Outlined(new Rect(safe.xMax - 560f, H * 0.32f + 80f, 520f, 50f), rank, UIStyles.Sized(UIStyles.Right, 34), rc, 3f);
            }
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
            DrawButton(HudLayout.Lock, PlayerCharacter.LockTarget != null ? "◎" : "LOCK", "", 0f, controls.Pressed[8], PlayerCharacter.LockTarget != null ? UIStyles.Crimson : Color.white);
            DrawButton(HudLayout.Guard, "GUARD", pc.Guarding ? "PARRY: TAP" : "", 0f, Mathf.Max(controls.Pressed[6], pc.Guarding ? 0.6f : 0f), new Color(0.9f, 0.9f, 0.6f));

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

        bool pauseSettings;

        void DrawPauseMenu()
        {
            UIStyles.Rect(new Rect(0f, 0f, W, H), new Color(0f, 0f, 0f, 0.6f));
            if (pauseSettings)
            {
                DrawSettingsPanel(new Rect(W * 0.5f - 800f, 60f, 1600f, H - 230f), true);
                if (Btn(new Rect(W * 0.5f - 250f, H - 150f, 500f, 100f), "BACK", UIStyles.ButtonBig)) pauseSettings = false;
                return;
            }
            var r = new Rect(W * 0.5f - 450f, H * 0.5f - 400f, 900f, 800f);
            UIStyles.PanelBox(r);
            GUI.Label(new Rect(r.x, r.y + 30f, r.width, 80f), "PAUSED", UIStyles.Big);
            GUI.Label(new Rect(r.x + 60f, r.y + 130f, r.width - 120f, 200f),
                "Keyboard: WASD move · J attack (hold = charge) · Space dodge · F guard/parry\n1 2 3 skills · R ultimate · Q/E switch slayer · Esc pause", UIStyles.CenterSmall);
            if (Btn(new Rect(r.x + 150f, r.y + 330f, r.width - 300f, 110f), "RESUME", UIStyles.ButtonBig)) gm.TogglePause();
            if (Btn(new Rect(r.x + 150f, r.y + 470f, r.width - 300f, 110f), "SETTINGS")) pauseSettings = true;
            if (Btn(new Rect(r.x + 150f, r.y + 610f, r.width - 300f, 110f), "RETREAT")) gm.RetreatFromBattle();
        }

        /// <summary>Big HP + energy bars for the active slayer (bottom centre).</summary>
        void DrawPlayerBars(BattleController b)
        {
            var pc = b.Team.Active;
            if (pc == null) return;
            float w = Mathf.Min(760f, W - 1500f);
            if (w < 380f) w = 380f;
            var r = new Rect(W * 0.5f - w * 0.5f, H - 88f - (H - safe.yMax), w, 26f);
            float hp = pc.Health.Normalized;
            Color hc = hp > 0.5f ? new Color(0.35f, 0.9f, 0.45f) : hp > 0.25f ? new Color(1f, 0.8f, 0.25f) : UIStyles.Bad;
            UIStyles.Bar(r, hp, hc);
            UIStyles.Outlined(new Rect(r.x, r.y - 38f, w, 36f), pc.Def.displayName + "   <size=22>" + Mathf.CeilToInt(pc.Health.Current).ToString("N0") + " / " +
                Mathf.CeilToInt(pc.Health.Max).ToString("N0") + "</size>", UIStyles.Sized(UIStyles.CenterSmall, 26), Color.white, 2f);
            Color el = ElementChart.ColorOf(pc.Element);
            UIStyles.Bar(new Rect(r.x, r.yMax + 6f, w, 14f), pc.UltGauge / PlayerCharacter.UltMax, pc.UltReady ? UIStyles.Gold : el);
            if (pc.Sprinting) UIStyles.Colored(new Rect(r.xMax + 12f, r.y - 4f, 200f, 34f), "» SPRINT", UIStyles.Sized(UIStyles.Body, 24), new Color(0.8f, 0.9f, 1f));
        }

        /// <summary>Impact frame (flash) + radial speed lines on heavy hits, parries and ultimates.</summary>
        void DrawImpactFrame()
        {
            float age = Time.unscaledTime - impactTime;
            if (age > 0.3f) return;
            if (age < 0.05f) UIStyles.Rect(new Rect(0f, 0f, W, H), new Color(1f, 1f, 1f, 0.28f * impactStrength));
            else if (age < 0.09f) UIStyles.Rect(new Rect(0f, 0f, W, H), new Color(0f, 0f, 0f, 0.18f * impactStrength));
            float a = (1f - age / 0.3f) * impactStrength;
            var center = new Vector2(W * 0.5f, H * 0.5f);
            var saved = GUI.matrix;
            var rng = new System.Random(Mathf.FloorToInt(impactTime * 1000f));
            int lines = 28;
            for (int i = 0; i < lines; i++)
            {
                float angle = (float)rng.NextDouble() * 360f;
                float inner = H * (0.42f + (float)rng.NextDouble() * 0.2f);
                float len = H * (0.3f + (float)rng.NextDouble() * 0.5f);
                float thick = 2f + (float)rng.NextDouble() * 5f;
                GUI.matrix = saved;
                GUIUtility.RotateAroundPivot(angle, center);
                UIStyles.Rect(new Rect(center.x + inner, center.y - thick * 0.5f, len, thick), new Color(1f, 1f, 1f, 0.55f * a));
            }
            GUI.matrix = saved;
        }

        void DrawLowHealth(BattleController b)
        {
            var pc = b.Team.Active;
            if (pc == null || !pc.IsAlive) return;
            float danger = Mathf.Clamp01((0.3f - pc.Health.Normalized) / 0.3f);
            if (danger <= 0f) return;
            float pulse = 0.55f + 0.45f * Mathf.Sin(Time.unscaledTime * 6f);
            var old = GUI.color;
            GUI.color = new Color(0.9f, 0.05f, 0.05f, danger * pulse * 0.8f);
            GUI.DrawTexture(new Rect(0f, 0f, W, H), UIStyles.Vignette);
            GUI.color = old;
        }
    }
}
