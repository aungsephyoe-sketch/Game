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
            // Menus over the battle re-register the areas where they eat touches every frame.
            if (Event.current.type == EventType.Repaint) MobileControls.UiBlockers.Clear();

            DrawLowHealth(b);
            DrawEnemyBars(cam);
            if (b.Def != null && (b.Def.coopAllyIds.Count > 0 || b.Def.pvpMode >= 0)) DrawPartyTags(cam);
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

            // PvP uses that corner for the kill feed.
            if (!b.CinematicLock && (b.Def == null || b.Def.pvpMode < 0)) DrawMinimap(b);
            // Pause button (not in co-op: the party keeps playing, so there's nothing to pause).
            if (b.Def == null || b.Def.coopTier < 0)
            {
                var pr = HudLayout.Pause;
                Round(pr, new Color(0.05f, 0.06f, 0.1f, 0.7f), 16f);
                RoundFrame(pr, new Color(1f, 1f, 1f, 0.15f), 2f, 16f);
                Round(new Rect(pr.center.x - 16f, pr.center.y - 22f, 11f, 44f), Color.white, 4f);
                Round(new Rect(pr.center.x + 5f, pr.center.y - 22f, 11f, 44f), Color.white, 4f);
            }

            if (TimeController.Paused) DrawPauseMenu();
        }

        /// <summary>Co-op gates: the party's AI slayers wear their player names and a health bar.</summary>
        void DrawPartyTags(Camera cam)
        {
            if (cam == null) return;
            float s = HudLayout.Scale;
            bool b0pvp = PvpMatch.Current != null;
            foreach (var ally in PartySlayer.Party)
            {
                if (ally == null) continue;
                Vector3 sp = cam.WorldToScreenPoint(ally.Position + Vector3.up * 2.3f);
                if (sp.z < 0f) continue;
                var p = new Vector2(sp.x / s, (Screen.height - sp.y) / s);
                Color el = ally.Team == CombatTeam.Enemy ? PvpRed : b0pvp ? PvpBlue : ElementChart.ColorOf(ally.Element);
                UIStyles.Outlined(new Rect(p.x - 120f, p.y - 40f, 240f, 28f), ally.DisplayName + (ally.Down ? "  <size=16>DOWN</size>" : ""), UIStyles.Sized(UIStyles.Center, 20), Color.Lerp(el, Color.white, 0.5f), 2f);
                UIStyles.Bar(new Rect(p.x - 60f, p.y - 10f, 120f, 9f), ally.Down ? 0f : ally.Health.Normalized, ally.Team == CombatTeam.Enemy ? PvpRed : new Color(0.4f, 0.95f, 0.5f));
                if (!string.IsNullOrEmpty(ally.Bubble) && Time.time < ally.BubbleUntil)
                {
                    float w = Mathf.Clamp(ally.Bubble.Length * 12f + 30f, 120f, 360f);
                    var r = new Rect(p.x - w * 0.5f, p.y - 86f, w, 40f);
                    Round(r, new Color(1f, 0.98f, 0.92f, 0.94f), 12f);
                    GUI.Label(new Rect(r.x + 10f, r.y + 6f, r.width - 20f, 30f), "<color=#222222>" + ally.Bubble.Replace("<", "‹") + "</color>", UIStyles.Sized(UIStyles.Center, 18));
                }
            }
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
            if (m.Def.pvpMode >= 0) { DrawPvpHud(b); return; }
            var r = new Rect(safe.x + 24f, safe.y + 20f, 540f, 214f);
            Round(r, new Color(0.04f, 0.05f, 0.08f, 0.62f), 14f);
            Round(new Rect(r.x, r.y + 12f, 5f, r.height - 24f), UIStyles.Gold, 2f);
            int secs = Mathf.FloorToInt(m.Elapsed);
            GUI.Label(new Rect(r.x + 18f, r.y + 8f, r.width - 36f, 30f), "<color=#FFD36B>CURRENT OBJECTIVE</color>   <color=#999999>" + m.Def.id + "  " + secs / 60 + ":" + (secs % 60).ToString("00") + "</color>",
                UIStyles.Sized(UIStyles.Small, 20));
            if (!string.IsNullOrEmpty(m.Def.trialCharacterId))
            {
                int left = Mathf.CeilToInt(m.TrialLeft);
                GUI.Label(new Rect(r.x + 18f, r.y + 42f, r.width - 36f, 44f), "MYTHIC TRIAL", UIStyles.Sized(UIStyles.H2, 30));
                UIStyles.Outlined(new Rect(r.x + 18f, r.y + 84f, r.width - 36f, 60f), left + "s", UIStyles.Sized(UIStyles.H1, 52), left <= 10 ? new Color(1f, 0.4f, 0.35f) : Color.white, 2f);
                GUI.Label(new Rect(r.x + 18f, r.y + 146f, r.width - 36f, 30f), "<color=#AAAAAA>Demons defeated " + m.Kills + "</color>", UIStyles.Sized(UIStyles.Body, 20));
                return;
            }
            if (m.Def.openWorld)
            {
                GUI.Label(new Rect(r.x + 18f, r.y + 42f, r.width - 36f, 44f), "Explore Kiriha Village", UIStyles.Sized(UIStyles.H2, 30));
                GUI.Label(new Rect(r.x + 18f, r.y + 80f, r.width - 36f, 30f), "<color=#DDDDDD>Villagers met " + m.VillagersMet + "   ·   Chests " + m.ChestsFound + "/" + m.ChestsTotal + "</color>", UIStyles.Sized(UIStyles.Body, 20));
                GUI.Label(new Rect(r.x + 18f, r.y + 110f, r.width - 36f, 30f), HubOnlineText(), UIStyles.Sized(UIStyles.Body, 20));
                if (FlatBtn(new Rect(r.x + 18f, r.y + 146f, 200f, 54f), "LEAVE", new Color(0.2f, 0.24f, 0.4f), true, 24)) m.Retreat();
                DrawVillageTalk(b);
                DrawVillageHub(b);
                return;
            }
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
            UIStyles.Rect(new Rect(x0 - 20f, y - 50f, w + 40f, 108f), new Color(0f, 0f, 0f, 0.3f));
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
                // Where you are sits under the bar; the destination sits above it, so the two names never overlap.
                if (here)
                    GUI.Label(new Rect(x - 150f, y + 14f, 300f, 30f), "<color=#FFD36B>" + j.places[i].name + "</color>", UIStyles.Sized(UIStyles.Center, 18));
                else if (i == n - 1)
                    GUI.Label(new Rect(x - 220f, y - 46f, 240f, 28f), "<color=#FF8080>" + j.places[i].name + "</color>", UIStyles.Sized(UIStyles.Right, 16));
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
                Round(r, active ? new Color(el.r * 0.3f, el.g * 0.3f, el.b * 0.3f, 0.88f) : new Color(0.04f, 0.05f, 0.08f, 0.7f), 14f);
                RoundFrame(r, active ? el : new Color(1f, 1f, 1f, 0.12f), active ? 3f : 2f, 14f);
                // Face portrait.
                var face = new Rect(r.x + 8f, r.y + 8f, r.height - 16f, r.height - 16f);
                Round(face, Color.Lerp(new Color(0.1f, 0.1f, 0.16f), el, 0.3f), 10f);
                var tex = ArtLibrary.Character(m.Def);
                if (tex != null) GUI.DrawTexture(face, tex, ScaleMode.ScaleAndCrop, true);
                float tx = face.xMax + 12f, tw = r.xMax - tx - 12f;
                string name = m.Def.displayName.Split(' ')[0].ToUpper();
                // Name and level as separate labels so a long name never wraps the level under the HP bar.
                var nameStyle = new GUIStyle(UIStyles.Sized(UIStyles.H2, name.Length > 7 ? 20 : 24)) { wordWrap = false, clipping = TextClipping.Clip };
                GUI.Label(new Rect(tx, r.y + 4f, tw - 70f, 34f), name, nameStyle);
                GUI.Label(new Rect(tx, r.y + 8f, tw, 30f), "<color=#AAAAAA>Lv." + m.Owned.level + "</color>", UIStyles.Sized(UIStyles.Right, 18));
                if (!m.IsAlive)
                {
                    UIStyles.Colored(new Rect(tx, r.y + 40f, tw, 36f), "DOWN", UIStyles.Sized(UIStyles.H2, 26), UIStyles.Bad);
                    continue;
                }
                float hp = m.Health.Normalized;
                Round(new Rect(tx, r.y + 44f, tw, 18f), new Color(0f, 0f, 0f, 0.5f), 6f);
                if (hp > 0.01f) Round(new Rect(tx, r.y + 44f, Mathf.Max(12f, tw * hp), 18f), hp > 0.3f ? new Color(0.35f, 0.9f, 0.45f) : UIStyles.Bad, 6f);
                Round(new Rect(tx, r.y + 68f, tw, 10f), new Color(0f, 0f, 0f, 0.5f), 4f);
                float ug = m.UltGauge / PlayerCharacter.UltMax;
                if (ug > 0.01f) Round(new Rect(tx, r.y + 68f, Mathf.Max(8f, tw * ug), 10f), m.UltReady ? UIStyles.Gold : el, 4f);
                if (!active && team.SwitchTimer > 0f)
                    Round(new Rect(r.x, r.y, r.width * team.SwitchTimer / TeamSystem.SwitchCooldown, r.height), new Color(0f, 0f, 0f, 0.4f), 14f);
            }
        }

        /// <summary>North-up minimap: the road, demons, the objective and you.</summary>
        void DrawMinimap(BattleController b)
        {
            var a = b.Team.Active;
            if (a == null) return;
            var s0 = HudLayout.Safe;
            Vector2 c = new Vector2(s0.xMax - 255f, s0.y + 118f);
            const float R = 92f, scale = 1.7f; // pixels per metre
            UIStyles.CircleTex(c, R + 4f, new Color(1f, 1f, 1f, 0.18f));
            UIStyles.CircleTex(c, R, new Color(0.04f, 0.06f, 0.1f, 0.78f));
            Vector3 me = a.Position;
            if (b.Journey != null)
            {
                var path = b.Journey.path;
                for (int i = 0; i < path.Count; i++)
                {
                    Vector2 p = c + new Vector2(path[i].x - me.x, -(path[i].z - me.z)) * scale;
                    if ((p - c).sqrMagnitude < (R - 6f) * (R - 6f)) UIStyles.CircleTex(p, 5f, new Color(0.9f, 0.85f, 0.7f, 0.55f));
                }
            }
            foreach (var cb in Combatant.All)
            {
                var e = cb as EnemyController;
                if (e == null || !e.IsAlive) continue;
                Vector2 p = c + new Vector2(e.Position.x - me.x, -(e.Position.z - me.z)) * scale;
                if ((p - c).sqrMagnitude < (R - 6f) * (R - 6f)) UIStyles.CircleTex(p, e.IsBoss ? 8f : 5f, e.IsBoss ? new Color(1f, 0.2f, 0.3f) : new Color(1f, 0.35f, 0.3f));
            }
            var m = b.Mission;
            if (m != null && m.ObjectiveTarget.HasValue)
            {
                Vector3 t = m.ObjectiveTarget.Value;
                Vector2 d = new Vector2(t.x - me.x, -(t.z - me.z)) * scale;
                if (d.magnitude > R - 10f) d = d.normalized * (R - 10f);
                UIStyles.CircleTex(c + d, 8f, new Color(1f, 0.85f, 0.25f));
            }
            // You: a white dot with a facing tick.
            UIStyles.CircleTex(c, 8f, Color.white);
            Vector3 f = a.transform.forward;
            UIStyles.CircleTex(c + new Vector2(f.x, -f.z) * 12f, 4f, Color.white);
        }

        void DrawBossBar(BattleController b)
        {
            var boss = b.Mission.Boss;
            if (boss == null || !boss.IsAlive) return;
            // PvP draws its own boss bar under the scoreboard.
            if (b.Def != null && b.Def.pvpMode >= 0) return;
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

        /// <summary>Open world: speech bubbles over villagers and a TALK button when you're next to one.</summary>
        void DrawVillageTalk(BattleController b)
        {
            var cam = Camera.main;
            float s = HudLayout.Scale;
            if (cam != null)
                foreach (var n in NpcWalker.All)
                {
                    if (n == null || string.IsNullOrEmpty(n.CurrentLine) || Time.unscaledTime > n.LineUntil) continue;
                    Vector3 sp = cam.WorldToScreenPoint(n.transform.position + Vector3.up * 2.4f);
                    if (sp.z < 0f) continue;
                    var p = new Vector2(sp.x / s, (Screen.height - sp.y) / s);
                    float w = Mathf.Clamp(n.CurrentLine.Length * 12f, 220f, 480f);
                    var r = new Rect(p.x - w * 0.5f, p.y - 86f, w, 76f);
                    Round(r, new Color(1f, 0.98f, 0.92f, 0.94f), 14f);
                    GUI.Label(new Rect(r.x + 12f, r.y + 4f, r.width - 24f, 26f), "<color=#8A4B2A><b>" + n.SpeakerName + "</b></color>", UIStyles.Sized(UIStyles.Small, 17));
                    GUI.Label(new Rect(r.x + 12f, r.y + 28f, r.width - 24f, 46f), "<color=#222222>" + n.CurrentLine + "</color>", UIStyles.Sized(UIStyles.Small, 18));
                }
            var near = b.Mission.NearNpc;
            if (near != null && cam != null)
            {
                Vector3 sp = cam.WorldToScreenPoint(near.transform.position + Vector3.up * 2.4f);
                if (sp.z > 0f && (string.IsNullOrEmpty(near.CurrentLine) || Time.unscaledTime > near.LineUntil))
                {
                    var p = new Vector2(sp.x / s, (Screen.height - sp.y) / s);
                    if (FlatBtn(new Rect(p.x - 80f, p.y - 70f, 160f, 58f), "TALK", TileGreen, true, 26)) b.Mission.TalkToNear();
                }
                if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.T) { b.Mission.TalkToNear(); Event.current.Use(); }
            }
        }

        void DrawCombo(BattleController b)
        {
            if (b.Combo < 3) return;
            float pulse = 1f + 0.15f * Mathf.Clamp01(b.ComboTimer - 1.9f) * 3f;
            // Top-centre, under the route strip, so it never sits behind the SPECIAL button.
            var style = UIStyles.Sized(UIStyles.Center, Mathf.RoundToInt(56f * pulse));
            float cy = safe.y + 150f;
            UIStyles.Outlined(new Rect(W * 0.5f - 260f, cy, 520f, 80f), b.Combo + " <size=30>HITS</size>", style, UIStyles.Gold, 3f);
            string rank = b.Combo >= 100 ? "LEGENDARY" : b.Combo >= 50 ? "AWESOME!" : b.Combo >= 25 ? "GREAT!" : b.Combo >= 10 ? "GOOD" : "";
            if (rank != "")
            {
                Color rc = b.Combo >= 100 ? new Color(1f, 0.4f, 1f) : b.Combo >= 50 ? new Color(1f, 0.45f, 0.3f) : b.Combo >= 25 ? new Color(0.4f, 0.9f, 1f) : Color.white;
                UIStyles.Outlined(new Rect(W * 0.5f - 260f, cy + 70f, 520f, 44f), rank, UIStyles.Sized(UIStyles.Center, 30), rc, 3f);
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
            DrawButton(atk, "ATTACK", pc.ChargeAmount > 0f ? "CHARGE" : (pc.ComboStep > 0 ? "COMBO " + pc.ComboStep : ""), 0f, controls.Pressed[0], el, 0f,
                IconFactory.Get(IconFactory.ForAttack(pc.Def.style, pc.Def.weapon)));
            if (pc.ChargeAmount > 0f) UIStyles.CircleFill(atk.center, atk.radius, pc.ChargeAmount, new Color(el.r, el.g, el.b, 0.45f));

            DrawButton(HudLayout.Dodge, "DODGE", "", 0f, controls.Pressed[1], new Color(0.55f, 0.75f, 1f), 0f, IconFactory.Get("dodge"));
            DrawButton(HudLayout.Guard, "GUARD", pc.Guarding ? "PARRY: TAP" : "", 0f, Mathf.Max(controls.Pressed[6], pc.Guarding ? 0.6f : 0f), new Color(0.95f, 0.85f, 0.45f), 0f, IconFactory.Get("shield"));

            for (int i = 0; i < 3; i++)
            {
                if (i >= pc.SkillSlots)
                {
                    // Locked by rarity: ascend to unlock more strong attacks.
                    var lc = HudLayout.Skill(i);
                    UIStyles.CircleTex(lc.center, lc.radius, new Color(0f, 0f, 0f, 0.5f));
                    UIStyles.CircleTex(lc.center, lc.radius, new Color(1f, 1f, 1f, 0.15f), UIStyles.Ring);
                    LockIcon(lc.center + new Vector2(0f, -8f), lc.radius * 0.7f, new Color(1f, 1f, 1f, 0.8f));
                    UIStyles.Outlined(new Rect(lc.center.x - lc.radius, lc.center.y + lc.radius * 0.3f, lc.radius * 2f, 24f), RarityInfo.Name(CharacterSystem.SkillSlotRarity(i)), UIStyles.Sized(UIStyles.Center, 15), RarityInfo.Color(CharacterSystem.SkillSlotRarity(i)), 1.5f);
                    continue;
                }
                var ab = pc.Def.skills[i];
                float cd = pc.Cooldowns[i];
                string shortName = ab.name.Contains(":") ? ab.name.Substring(ab.name.IndexOf(':') + 1).Trim() : ab.name;
                DrawButton(HudLayout.Skill(i), (i + 1).ToString(), shortName, cd > 0f ? cd / ab.cooldown : 0f, controls.Pressed[2 + i], el, cd,
                    IconFactory.Get(IconFactory.ForSkill(ab.shape)), IconFactory.Get(IconFactory.ForElement(pc.Element)));
            }

            var ult = HudLayout.Ultimate;
            float gauge = pc.UltGauge / PlayerCharacter.UltMax;
            UIStyles.CircleTex(ult.center, ult.radius, new Color(0f, 0f, 0f, 0.55f));
            UIStyles.CircleFill(ult.center, ult.radius, gauge, new Color(el.r, el.g, el.b, pc.UltReady ? 0.9f : 0.45f));
            float ring = pc.UltReady ? 0.7f + 0.3f * Mathf.Sin(Time.unscaledTime * 8f) : 0.4f;
            UIStyles.CircleTex(ult.center, ult.radius + (pc.UltReady ? 6f * ring : 0f), new Color(1f, 0.85f, 0.4f, ring), UIStyles.Ring);
            float ui = ult.radius * 0.95f;
            DrawIcon(new Rect(ult.center.x - ui * 0.5f, ult.center.y - ui * 0.62f, ui, ui), IconFactory.Get(IconFactory.ForElement(pc.Element)), pc.UltReady ? Color.white : new Color(1f, 1f, 1f, 0.6f));
            UIStyles.Outlined(new Rect(ult.center.x - ult.radius, ult.center.y + ult.radius * 0.28f, ult.radius * 2f, 30f), pc.UltReady ? "SPECIAL" : Mathf.FloorToInt(gauge * 100f) + "%",
                UIStyles.Sized(UIStyles.Center, 20), Color.white, 2f);
        }

        void DrawButton(HudLayout.Circle c, string label, string sub, float cooldown01, float pressed, Color accent, float seconds = 0f, Texture2D icon = null, Texture2D badge = null)
        {
            float r = c.radius * (1f - pressed * 0.08f);
            // Dark base, element-tinted fill with a lighter core, bright rim.
            UIStyles.CircleTex(c.center + new Vector2(0f, 4f), r, new Color(0f, 0f, 0f, 0.35f));
            UIStyles.CircleTex(c.center, r, new Color(0.04f, 0.04f, 0.08f, 0.7f));
            UIStyles.CircleTex(c.center, r * 0.94f, new Color(accent.r * 0.6f, accent.g * 0.6f, accent.b * 0.6f, 0.55f + pressed * 0.35f));
            UIStyles.CircleTex(c.center - new Vector2(0f, r * 0.18f), r * 0.62f, new Color(1f, 1f, 1f, 0.08f + pressed * 0.1f));
            UIStyles.CircleTex(c.center, r, new Color(Mathf.Lerp(accent.r, 1f, 0.5f), Mathf.Lerp(accent.g, 1f, 0.5f), Mathf.Lerp(accent.b, 1f, 0.5f), 0.75f), UIStyles.Ring);
            if (cooldown01 > 0f)
            {
                if (icon != null) DrawIcon(new Rect(c.center.x - r * 0.55f, c.center.y - r * 0.55f, r * 1.1f, r * 1.1f), icon, new Color(1f, 1f, 1f, 0.25f));
                UIStyles.CircleFill(c.center, r, cooldown01, new Color(0f, 0f, 0f, 0.6f));
                UIStyles.Outlined(new Rect(c.center.x - r, c.center.y - 26f, r * 2f, 50f), Mathf.CeilToInt(seconds).ToString(), UIStyles.Sized(UIStyles.Center, 40), Color.white, 2f);
            }
            else if (icon != null)
            {
                float s = r * (sub == "" ? 1.1f : 0.9f);
                float cy = c.center.y - (sub == "" ? 0f : r * 0.16f);
                DrawIcon(new Rect(c.center.x - s * 0.5f + 2f, cy - s * 0.5f + 3f, s, s), icon, new Color(0f, 0f, 0f, 0.45f));
                DrawIcon(new Rect(c.center.x - s * 0.5f, cy - s * 0.5f, s, s), icon, Color.white);
            }
            else UIStyles.Outlined(new Rect(c.center.x - r, c.center.y - (sub == "" ? 20f : 34f), r * 2f, 40f), label, UIStyles.Sized(UIStyles.Center, label.Length > 2 ? 28 : 38), Color.white, 2f);
            if (badge != null)
            {
                Vector2 bc = c.center + new Vector2(r * 0.72f, -r * 0.72f);
                UIStyles.CircleTex(bc, r * 0.32f, new Color(0.05f, 0.05f, 0.1f, 0.9f));
                DrawIcon(new Rect(bc.x - r * 0.22f, bc.y - r * 0.22f, r * 0.44f, r * 0.44f), badge, accent);
            }
            if (sub != "") UIStyles.Outlined(new Rect(c.center.x - r, c.center.y + r * 0.34f, r * 2f, 28f), sub, UIStyles.Sized(UIStyles.Center, Mathf.RoundToInt(Mathf.Clamp(r * 0.26f, 13f, 18f))), new Color(1f, 1f, 1f, 0.92f), 2f);
        }

        static void DrawIcon(Rect r, Texture2D icon, Color c)
        {
            if (icon == null) return;
            var old = GUI.color;
            GUI.color = new Color(old.r * c.r, old.g * c.g, old.b * c.b, old.a * c.a);
            GUI.DrawTexture(r, icon, ScaleMode.ScaleToFit, true);
            GUI.color = old;
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
                // Pops in overshooting, then arcs up and drifts sideways, shrinking as it fades.
                float rise = age * 170f - age * age * 110f;
                var p = new Vector2(sp.x / s + e.xJitter * (0.4f + age), (Screen.height - sp.y) / s - rise);
                float pop = age < 0.09f ? Mathf.Lerp(0.45f, 1.6f, age / 0.09f) : 1f + 0.6f * Mathf.Exp(-(age - 0.09f) * 13f);
                pop *= Mathf.Lerp(1f, 0.75f, Mathf.Clamp01((age - 0.6f) / 0.4f));
                if (e.crit && age < 0.25f) p += new Vector2(Random.Range(-4f, 4f), Random.Range(-4f, 4f));
                var col = e.color;
                col.a = 1f - Mathf.Clamp01((age - 0.62f) / 0.38f);
                if (e.crit && age < 0.12f) col = Color.Lerp(Color.white, col, age / 0.12f);
                var style = UIStyles.Sized(UIStyles.Center, Mathf.RoundToInt(e.size * pop));
                float ow = e.size >= 60f ? 5f : 4f;
                UIStyles.Outlined(new Rect(p.x - 240f, p.y - 50f, 480f, 100f), e.text, style, col, ow);
                if (!string.IsNullOrEmpty(e.tag))
                    UIStyles.Outlined(new Rect(p.x - 240f, p.y - 50f - e.size * 0.8f, 480f, 50f), e.tag, UIStyles.Sized(UIStyles.Center, Mathf.RoundToInt(26 * Mathf.Min(pop, 1.3f))),
                        e.tag.StartsWith("CRIT") ? new Color(1f, 0.45f, 0.15f, col.a) : e.tag == "WEAK!" ? new Color(1f, 0.6f, 0.2f, col.a) : new Color(0.6f, 0.6f, 0.7f, col.a), 3f);
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
                RotateGui(angle, center);
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
