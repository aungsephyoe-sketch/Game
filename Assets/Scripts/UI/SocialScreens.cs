using System.Collections.Generic;
using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// Profile (tap your name top-left): change your name once a week, your gamer code with COPY, stats and
    /// medals. Friends (FRIENDS on the left): your list with who's online, add by code, suggestions, requests,
    /// and private chats where bad words are blurred. Achievements: every goal with its medal, progress and
    /// reward. Plus the "ACHIEVEMENT UNLOCKED" popup, shown over any screen.
    /// </summary>
    public partial class UIManager
    {
        enum SocialPanel { None, Profile, Friends, Achievements, Arena }

        SocialPanel social = SocialPanel.None;
        float socialOpenedAt;
        int friendsTab;
        string chatWith;
        string dmDraft = "", codeInput = "", renameInput = "", socialMsg = "";
        bool renaming;
        Vector2 friendsScroll, dmScroll, achScroll;
        int dmSeen = -1;
        GUIStyle socialLine, socialField;
        float achPopupAt = -1f;
        AchievementSystem.Def achShowing;

        bool SocialOpen { get { return social != SocialPanel.None; } }

        void OpenSocial(SocialPanel p)
        {
            social = p;
            socialOpenedAt = Time.unscaledTime;
            socialMsg = "";
            renaming = false;
            if (gm != null) { SocialSystem.EnsureCode(gm.Data); gm.Audio.Play("click", 0.5f); }
        }

        void EnsureSocialStyles()
        {
            if (socialLine != null) return;
            socialLine = new GUIStyle(UIStyles.Sized(UIStyles.Body, 20)) { wordWrap = true, richText = true };
            socialField = new GUIStyle(GUI.skin.textField) { fontSize = 22, alignment = TextAnchor.MiddleLeft, padding = new RectOffset(14, 14, 4, 4) };
        }

        /// <summary>Blurred bad words: the filter's dots drawn as a grey smudge; typed tags are shown as text.</summary>
        static string ChatSafe(string t)
        {
            if (string.IsNullOrEmpty(t)) return "";
            t = t.Replace("<", "‹").Replace(">", "›");
            var sb = new System.Text.StringBuilder();
            bool inBlur = false;
            foreach (char c in t)
            {
                bool dot = c == '•';
                if (dot && !inBlur) { sb.Append("<color=#8C8C9E88>"); inBlur = true; }
                if (!dot && inBlur) { sb.Append("</color>"); inBlur = false; }
                sb.Append(dot ? '▒' : c);
            }
            if (inBlur) sb.Append("</color>");
            return sb.ToString();
        }

        /// <summary>Drag to scroll (touch), on top of the mouse wheel.</summary>
        static Vector2 DragScroll(Rect view, Vector2 scroll, float contentH)
        {
            var ev = Event.current;
            if (ev.type == EventType.MouseDrag && view.Contains(ev.mousePosition)) { scroll.y -= ev.delta.y; ev.Use(); }
            scroll.y = Mathf.Clamp(scroll.y, 0f, Mathf.Max(0f, contentH - view.height));
            return scroll;
        }

        void CloseButton(Rect panel)
        {
            var r = new Rect(panel.xMax - 76f, panel.y + 16f, 60f, 60f);
            UIStyles.CircleTex(r.center, 28f, new Color(1f, 1f, 1f, 0.1f));
            GUI.Label(r, "✕", UIStyles.Sized(UIStyles.Center, 30));
            if (GUI.Button(r, GUIContent.none, GUIStyle.none)) { social = SocialPanel.None; chatWith = null; gm.Audio.Play("click", 0.5f); }
        }

        void DrawSocial(PlayerData d)
        {
            EnsureSocialStyles();
            float k = Mathf.Clamp01((Time.unscaledTime - socialOpenedAt) / 0.25f);
            k = 1f - Mathf.Pow(1f - k, 3f);
            ModalBackdrop(k * 0.9f);
            // Clicks outside the panel close it.
            switch (social)
            {
                case SocialPanel.Profile: DrawProfilePanel(d, k); break;
                case SocialPanel.Friends: DrawFriendsPanel(d, k); break;
                case SocialPanel.Achievements: DrawAchievementsPanel(d, k); break;
                case SocialPanel.Arena: DrawArenaPanel(d, k); break;
            }
        }

        // ------------------------------------------------------------------ Profile

        void DrawProfilePanel(PlayerData d, float k)
        {
            var panel = new Rect(W * 0.5f - 560f, H * 0.5f - 390f + (1f - k) * 60f, 1120f, 780f);
            Round(Offset(panel, 0f, 8f), new Color(0f, 0f, 0f, 0.5f), 26f);
            Round(panel, new Color(0.07f, 0.08f, 0.14f, 0.98f), 26f);
            RoundFrame(panel, new Color(1f, 0.8f, 0.3f, 0.8f), 3f, 26f);
            CloseButton(panel);
            UIStyles.Outlined(new Rect(panel.x + 40f, panel.y + 24f, 600f, 56f), "PROFILE", UIStyles.Sized(UIStyles.H1, 44), UIStyles.Gold, 3f);

            string leaderId = d.team.Count > 0 ? d.team[0] : GameDatabase.Protagonist;
            var leader = GameDatabase.GetCharacter(leaderId);
            var fc = new Vector2(panel.x + 150f, panel.y + 210f);
            UIStyles.CircleTex(fc, 96f, new Color(0f, 0f, 0f, 0.35f));
            if (leader != null) FaceCircle(fc, 86f, leader, UIStyles.Gold);
            float frac;
            int lv = AccountLevel(d, out frac);
            UIStyles.Outlined(new Rect(fc.x - 100f, fc.y + 96f, 200f, 36f), "Lv. " + lv, UIStyles.Sized(UIStyles.Center, 28), Color.white, 2f);

            // Name and rename.
            float x = panel.x + 290f, w = panel.width - 330f, y = panel.y + 110f;
            GUI.Label(new Rect(x, y, w, 28f), "<color=#AAAAAA>NAME</color>", UIStyles.Sized(UIStyles.Small, 18));
            if (!renaming)
            {
                UIStyles.Outlined(new Rect(x, y + 26f, w - 240f, 60f), string.IsNullOrEmpty(d.playerName) ? "Slayer" : d.playerName, UIStyles.Sized(UIStyles.H1, 46), Color.white, 2f);
                bool can = SocialSystem.CanChangeName(d);
                if (FlatBtn(new Rect(x + w - 220f, y + 30f, 210f, 56f), "EDIT NAME", can ? TileBlue : TileNavy, can, 22))
                {
                    renaming = true;
                    renameInput = d.playerName;
                    socialMsg = "";
                }
                GUI.Label(new Rect(x, y + 92f, w, 28f), can ? "<color=#8FD19E>You can change your name now (once every 7 days).</color>"
                    : "<color=#CCAA77>Next name change in " + SocialSystem.WaitText(SocialSystem.NameChangeWait(d)) + ".</color>", UIStyles.Sized(UIStyles.Small, 18));
            }
            else
            {
                var field = new Rect(x, y + 30f, w - 460f, 58f);
                GUI.SetNextControlName("Rename");
                renameInput = GUI.TextField(field, renameInput ?? "", 14, socialField);
                if (FlatBtn(new Rect(field.xMax + 12f, field.y, 210f, 58f), "SAVE", TileGreen, true, 22))
                {
                    string reason;
                    if (SocialSystem.ChangeName(d, renameInput, out reason)) { renaming = false; socialMsg = "<color=#8FD19E>Name changed!</color>"; gm.Save(); gm.Audio.Play("perfect", 0.6f); }
                    else socialMsg = "<color=#FF8A7A>" + reason + "</color>";
                }
                if (FlatBtn(new Rect(field.xMax + 232f, field.y, 210f, 58f), "CANCEL", TileNavy, true, 22)) { renaming = false; socialMsg = ""; }
                GUI.Label(new Rect(x, y + 92f, w, 28f), "<color=#CCAA77>After saving you can't change it again for 7 days.</color>", UIStyles.Sized(UIStyles.Small, 18));
            }
            if (!string.IsNullOrEmpty(socialMsg)) GUI.Label(new Rect(x, y + 120f, w, 30f), socialMsg, UIStyles.Sized(UIStyles.Small, 19));

            // Gamer code.
            y += 170f;
            var codeBox = new Rect(x, y, w, 96f);
            Round(codeBox, new Color(1f, 1f, 1f, 0.06f), 16f);
            GUI.Label(new Rect(codeBox.x + 20f, codeBox.y + 8f, 400f, 28f), "<color=#AAAAAA>YOUR GAMER CODE</color>  <color=#777788><size=15>share it so friends can add you</size></color>", UIStyles.Sized(UIStyles.Small, 18));
            UIStyles.Outlined(new Rect(codeBox.x + 20f, codeBox.y + 34f, 500f, 56f), SocialSystem.EnsureCode(d), UIStyles.Sized(UIStyles.H1, 40), new Color(0.6f, 0.9f, 1f), 2f);
            if (FlatBtn(new Rect(codeBox.xMax - 190f, codeBox.y + 20f, 170f, 56f), "COPY", TileBlue, true, 22))
            {
                GUIUtility.systemCopyBuffer = d.gamerCode;
                Toast("Gamer code copied: " + d.gamerCode);
            }

            // Stats.
            y = codeBox.yMax + 24f;
            string[] labels = { "Missions", "Demons", "Bosses", "Summons", "Slayers", "Friends" };
            int[] values = { d.missionsCleared, d.totalKills, d.bossesDefeated, d.totalSummons, d.characters.Count, d.friends.Count };
            float cw = (w - 50f) / 6f;
            for (int i = 0; i < labels.Length; i++)
            {
                var c = new Rect(x + i * (cw + 10f), y, cw, 96f);
                Round(c, new Color(1f, 1f, 1f, 0.05f), 14f);
                UIStyles.Outlined(new Rect(c.x, c.y + 10f, c.width, 46f), values[i].ToString("N0"), UIStyles.Sized(UIStyles.Center, 32), Color.white, 2f);
                GUI.Label(new Rect(c.x, c.y + 58f, c.width, 30f), "<color=#AAAAAA>" + labels[i] + "</color>", UIStyles.Sized(UIStyles.CenterSmall, 17));
            }

            // Medals.
            y += 124f;
            GUI.Label(new Rect(panel.x + 60f, y, 400f, 30f), "<color=#FFD36B><b>MEDALS</b></color>", UIStyles.Sized(UIStyles.Body, 22));
            float mx = panel.x + 90f;
            for (int m = 0; m < 4; m++)
            {
                var medal = (Medal)m;
                var c = new Vector2(mx + m * 170f, y + 90f);
                MedalIcon(c, 40f, medal, AchievementSystem.Count(d, medal) > 0);
                UIStyles.Outlined(new Rect(c.x + 44f, c.y - 22f, 100f, 44f), "×" + AchievementSystem.Count(d, medal), UIStyles.Sized(UIStyles.Body, 30), Color.white, 2f);
            }
            if (FlatBtn(new Rect(panel.xMax - 380f, y + 56f, 330f, 70f), "ACHIEVEMENTS  <size=20>" + AchievementSystem.Count(d) + "/" + AchievementSystem.All.Count + "</size>", TileOrange, true, 24))
                OpenSocial(SocialPanel.Achievements);
        }

        /// <summary>A medal: ribbon, rim and face in the medal's metal (dim when not earned yet).</summary>
        void MedalIcon(Vector2 c, float r, Medal m, bool earned)
        {
            Color mc = AchievementSystem.MedalColor(m);
            if (!earned) mc = Color.Lerp(mc, new Color(0.25f, 0.25f, 0.3f), 0.7f);
            Color ribbon = m == Medal.Bronze ? new Color(0.7f, 0.2f, 0.2f) : m == Medal.Silver ? new Color(0.2f, 0.35f, 0.75f) : m == Medal.Gold ? new Color(0.75f, 0.15f, 0.2f) : new Color(0.45f, 0.2f, 0.75f);
            if (!earned) ribbon = Color.Lerp(ribbon, Color.gray, 0.6f);
            Round(new Rect(c.x - r * 0.55f, c.y - r * 1.6f, r * 0.45f, r * 0.9f), ribbon, 3f);
            Round(new Rect(c.x + r * 0.1f, c.y - r * 1.6f, r * 0.45f, r * 0.9f), ribbon * 0.85f, 3f);
            UIStyles.CircleTex(c, r + 3f, new Color(0f, 0f, 0f, 0.4f));
            UIStyles.CircleTex(c, r, mc * 0.75f);
            UIStyles.CircleTex(c, r * 0.8f, mc);
            UIStyles.CircleTex(c + new Vector2(-r * 0.2f, -r * 0.25f), r * 0.3f, new Color(1f, 1f, 1f, earned ? 0.35f : 0.1f));
            GUI.Label(new Rect(c.x - r, c.y - r, r * 2f, r * 2f), "★", UIStyles.Sized(UIStyles.Center, Mathf.RoundToInt(r * 0.9f)));
        }

        // ------------------------------------------------------------------ Friends

        void DrawFriendsPanel(PlayerData d, float k)
        {
            float pw = Mathf.Min(820f, W * 0.5f);
            var panel = new Rect(W - pw - safe.x * 0.5f + (1f - k) * pw, safe.y + 10f, pw, H - safe.y - 20f);
            Round(Offset(panel, -6f, 0f), new Color(0f, 0f, 0f, 0.5f), 24f);
            Round(panel, new Color(0.06f, 0.07f, 0.12f, 0.98f), 24f);
            RoundFrame(panel, new Color(0.45f, 0.7f, 1f, 0.6f), 2f, 24f);
            CloseButton(panel);
            var friend = chatWith != null ? d.friends.Find(f => f.code == chatWith) : null;
            if (friend != null) { DrawDm(d, friend, panel); return; }
            chatWith = null;
            UIStyles.Outlined(new Rect(panel.x + 30f, panel.y + 22f, 500f, 56f), "FRIENDS  <size=26>" + d.friends.Count + "/" + SocialSystem.MaxFriends + "</size>", UIStyles.Sized(UIStyles.H1, 40), Color.white, 2f);
            string[] tabs = { "FRIENDS", "ADD FRIEND", "REQUESTS" + (d.friendRequests.Count > 0 ? " (" + d.friendRequests.Count + ")" : "") };
            float tw = (panel.width - 60f - 20f) / 3f;
            for (int i = 0; i < 3; i++)
            {
                var tr = new Rect(panel.x + 30f + i * (tw + 10f), panel.y + 94f, tw, 56f);
                if (FlatBtn(tr, tabs[i], friendsTab == i ? TileBlue : TileNavy, true, 20)) { friendsTab = i; socialMsg = ""; friendsScroll = Vector2.zero; }
            }
            var area = new Rect(panel.x + 20f, panel.y + 166f, panel.width - 40f, panel.height - 186f);
            if (friendsTab == 0) DrawFriendList(d, area);
            else if (friendsTab == 1) DrawAddFriend(d, area);
            else DrawRequests(d, area);
        }

        void DrawFriendList(PlayerData d, Rect area)
        {
            if (d.friends.Count == 0)
            {
                GUI.Label(new Rect(area.x, area.y + 60f, area.width, 120f), "<color=#AAAAAA>No friends yet.\nAdd players with their gamer code, or from the suggestions.</color>", UIStyles.Sized(UIStyles.Center, 22));
                if (FlatBtn(new Rect(area.center.x - 170f, area.y + 200f, 340f, 70f), "ADD FRIEND", TileGreen, true, 24)) friendsTab = 1;
                return;
            }
            // Online first, then by unread.
            var list = new List<FriendEntry>(d.friends);
            list.Sort((a, b) =>
            {
                int oa = SocialSystem.IsOnline(a) ? 0 : 1, ob = SocialSystem.IsOnline(b) ? 0 : 1;
                return oa != ob ? oa - ob : b.unread - a.unread;
            });
            float rowH = 104f;
            float contentH = list.Count * (rowH + 10f);
            friendsScroll = DragScroll(area, friendsScroll, contentH);
            friendsScroll = GUI.BeginScrollView(area, friendsScroll, new Rect(0f, 0f, area.width - 20f, contentH));
            for (int i = 0; i < list.Count; i++)
            {
                var f = list[i];
                var r = new Rect(0f, i * (rowH + 10f), area.width - 24f, rowH);
                DrawPlayerRow(f, r);
                if (f.unread > 0) Badge(new Vector2(r.x + 88f, r.y + 20f), f.unread > 9 ? "9+" : f.unread.ToString(), UIStyles.Crimson, 0.75f);
                if (FlatBtn(new Rect(r.xMax - 250f, r.y + 22f, 150f, 60f), "CHAT", TileGreen, true, 22)) { chatWith = f.code; dmSeen = -1; f.unread = 0; dmDraft = ""; }
                if (FlatBtn(new Rect(r.xMax - 90f, r.y + 22f, 76f, 60f), "✕", new Color(0.45f, 0.2f, 0.24f), true, 22))
                {
                    SocialSystem.RemoveFriend(d, f.code);
                    Toast(f.name + " was removed from your friends.");
                    gm.Save();
                    break;
                }
            }
            GUI.EndScrollView();
        }

        void DrawPlayerRow(FriendEntry f, Rect r)
        {
            Round(r, new Color(1f, 1f, 1f, 0.05f), 16f);
            var def = GameDatabase.GetCharacter(f.charId);
            var c = new Vector2(r.x + 56f, r.center.y);
            if (def != null) FaceCircle(c, 38f, def, ElementChart.ColorOf(def.element));
            bool on = SocialSystem.IsOnline(f);
            UIStyles.CircleTex(c + new Vector2(30f, 28f), 10f, new Color(0.05f, 0.06f, 0.1f));
            UIStyles.CircleTex(c + new Vector2(30f, 28f), 7f, on ? new Color(0.35f, 1f, 0.5f) : new Color(0.5f, 0.5f, 0.55f));
            GUI.Label(new Rect(r.x + 110f, r.y + 14f, r.width - 380f, 34f), "<b>" + ChatSafe(f.name) + "</b>  <color=#AAB4C8><size=18>Lv " + f.level + "</size></color>", UIStyles.Sized(UIStyles.Body, 24));
            GUI.Label(new Rect(r.x + 110f, r.y + 52f, r.width - 380f, 30f), (on ? "<color=#7CFF8A>" : "<color=#888899>") + SocialSystem.Status(f) + "</color>" + (def != null ? "  <color=#777788>· " + def.displayName + "</color>" : ""), UIStyles.Sized(UIStyles.Small, 17));
        }

        void DrawAddFriend(PlayerData d, Rect area)
        {
            float y = area.y;
            var mine = new Rect(area.x, y, area.width, 86f);
            Round(mine, new Color(1f, 1f, 1f, 0.05f), 14f);
            GUI.Label(new Rect(mine.x + 18f, mine.y + 6f, 300f, 26f), "<color=#AAAAAA>YOUR CODE</color>", UIStyles.Sized(UIStyles.Small, 17));
            UIStyles.Outlined(new Rect(mine.x + 18f, mine.y + 30f, 400f, 50f), SocialSystem.EnsureCode(d), UIStyles.Sized(UIStyles.H2, 32), new Color(0.6f, 0.9f, 1f), 2f);
            if (FlatBtn(new Rect(mine.xMax - 170f, mine.y + 16f, 150f, 54f), "COPY", TileBlue, true, 20)) { GUIUtility.systemCopyBuffer = d.gamerCode; Toast("Gamer code copied: " + d.gamerCode); }
            y += 106f;
            GUI.Label(new Rect(area.x, y, area.width, 28f), "<color=#AAAAAA>Enter a friend's gamer code</color>", UIStyles.Sized(UIStyles.Small, 18));
            var field = new Rect(area.x, y + 32f, area.width - 190f, 60f);
            GUI.SetNextControlName("FriendCode");
            codeInput = GUI.TextField(field, codeInput ?? "", 16, socialField);
            if (string.IsNullOrEmpty(codeInput) && GUI.GetNameOfFocusedControl() != "FriendCode")
                GUI.Label(new Rect(field.x + 16f, field.y, field.width, field.height), "<color=#777788>BL-XXXX-XXXX</color>", UIStyles.Sized(UIStyles.Body, 22));
            if (FlatBtn(new Rect(field.xMax + 12f, field.y, 170f, 60f), "ADD", TileGreen, !string.IsNullOrEmpty(codeInput), 22))
            {
                string code = SocialSystem.NormaliseCode(codeInput);
                string reason;
                if (code == null) socialMsg = "<color=#FF8A7A>That doesn't look like a gamer code (BL-XXXX-XXXX).</color>";
                else
                {
                    var e = SocialSystem.Lookup(code);
                    if (SocialSystem.AddFriend(d, e, out reason)) { socialMsg = "<color=#8FD19E>" + e.name + " added!</color>"; codeInput = ""; gm.Save(); gm.Audio.Play("perfect", 0.5f); }
                    else socialMsg = "<color=#FF8A7A>" + reason + "</color>";
                }
            }
            if (!string.IsNullOrEmpty(socialMsg)) GUI.Label(new Rect(area.x, field.yMax + 6f, area.width, 30f), socialMsg, UIStyles.Sized(UIStyles.Small, 19));
            y = field.yMax + 50f;
            GUI.Label(new Rect(area.x, y, area.width, 30f), "<color=#FFD36B><b>SUGGESTED PLAYERS</b></color>", UIStyles.Sized(UIStyles.Body, 21));
            y += 38f;
            foreach (var e in SocialSystem.Suggestions(d, 4))
            {
                if (y + 104f > area.yMax) break;
                var r = new Rect(area.x, y, area.width, 96f);
                DrawPlayerRow(e, r);
                if (FlatBtn(new Rect(r.xMax - 170f, r.y + 18f, 150f, 60f), "+ ADD", TileGreen, true, 22))
                {
                    string reason;
                    if (SocialSystem.AddFriend(d, e, out reason)) { Toast(e.name + " added to your friends!"); gm.Save(); gm.Audio.Play("perfect", 0.5f); }
                    else Toast(reason);
                }
                y += 106f;
            }
        }

        void DrawRequests(PlayerData d, Rect area)
        {
            if (d.friendRequests.Count == 0)
            {
                GUI.Label(new Rect(area.x, area.y + 60f, area.width, 80f), "<color=#AAAAAA>No friend requests right now.\nPlayers you team up with in the village may send you one.</color>", UIStyles.Sized(UIStyles.Center, 21));
                return;
            }
            float y = area.y;
            for (int i = 0; i < d.friendRequests.Count; i++)
            {
                var e = d.friendRequests[i];
                var r = new Rect(area.x, y, area.width, 96f);
                DrawPlayerRow(e, r);
                if (FlatBtn(new Rect(r.xMax - 330f, r.y + 18f, 150f, 60f), "ACCEPT", TileGreen, true, 20))
                {
                    string reason;
                    if (SocialSystem.AddFriend(d, e, out reason)) { Toast(e.name + " is now your friend!"); gm.Save(); }
                    else { Toast(reason); d.friendRequests.RemoveAt(i); }
                    break;
                }
                if (FlatBtn(new Rect(r.xMax - 170f, r.y + 18f, 150f, 60f), "DECLINE", TileNavy, true, 20)) { d.friendRequests.RemoveAt(i); gm.Save(); break; }
                y += 106f;
            }
        }

        void DrawDm(PlayerData d, FriendEntry f, Rect panel)
        {
            f.unread = 0;
            if (FlatBtn(new Rect(panel.x + 24f, panel.y + 20f, 110f, 56f), "‹ BACK", TileNavy, true, 20)) { chatWith = null; return; }
            var def = GameDatabase.GetCharacter(f.charId);
            if (def != null) FaceCircle(new Vector2(panel.x + 186f, panel.y + 48f), 30f, def, ElementChart.ColorOf(def.element));
            bool on = SocialSystem.IsOnline(f);
            GUI.Label(new Rect(panel.x + 230f, panel.y + 16f, panel.width - 330f, 34f), "<b>" + ChatSafe(f.name) + "</b>", UIStyles.Sized(UIStyles.Body, 26));
            GUI.Label(new Rect(panel.x + 230f, panel.y + 50f, panel.width - 330f, 26f), (on ? "<color=#7CFF8A>● " : "<color=#888899>○ ") + SocialSystem.Status(f) + "</color>", UIStyles.Sized(UIStyles.Small, 17));

            var thread = SocialSystem.Thread(d, f.code);
            var view = new Rect(panel.x + 20f, panel.y + 96f, panel.width - 40f, panel.height - 200f);
            Round(view, new Color(0f, 0f, 0f, 0.25f), 16f);
            // Measure bubbles.
            float bw = view.width * 0.72f;
            var heights = new List<float>();
            float total = 12f;
            foreach (var l in thread.lines)
            {
                float h = socialLine.CalcHeight(new GUIContent(ChatSafe(l.text)), bw - 28f) + 22f;
                heights.Add(h);
                total += h + 10f;
            }
            bool typing = SocialSystem.TypingCode == f.code;
            if (typing) total += 50f;
            if (dmSeen != thread.lines.Count + (typing ? 1 : 0)) { dmSeen = thread.lines.Count + (typing ? 1 : 0); dmScroll.y = 99999f; }
            dmScroll = DragScroll(view, dmScroll, total);
            dmScroll = GUI.BeginScrollView(view, dmScroll, new Rect(0f, 0f, view.width - 20f, total));
            float y = 12f;
            if (thread.lines.Count == 0)
                GUI.Label(new Rect(0f, 40f, view.width - 20f, 60f), "<color=#777788>Say hi to " + ChatSafe(f.name) + "!</color>", UIStyles.Sized(UIStyles.Center, 20));
            for (int i = 0; i < thread.lines.Count; i++)
            {
                var l = thread.lines[i];
                float h = heights[i];
                float tw = Mathf.Min(bw, socialLine.CalcSize(new GUIContent(ChatSafe(l.text))).x + 30f);
                var br = new Rect(l.mine ? view.width - 36f - tw : 12f, y, tw, h);
                Round(br, l.mine ? new Color(0.95f, 0.72f, 0.28f, 0.95f) : new Color(0.2f, 0.23f, 0.34f, 0.95f), 16f);
                GUI.Label(new Rect(br.x + 14f, br.y + 10f, br.width - 24f, br.height - 14f), (l.mine ? "<color=#1A1208>" : "<color=#F0F0F6>") + ChatSafe(l.text) + "</color>", socialLine);
                y += h + 10f;
            }
            if (typing)
            {
                var tr = new Rect(12f, y, 110f, 40f);
                Round(tr, new Color(0.2f, 0.23f, 0.34f, 0.95f), 16f);
                for (int i = 0; i < 3; i++)
                    UIStyles.CircleTex(new Vector2(tr.x + 32f + i * 22f, tr.center.y + Mathf.Sin(Time.unscaledTime * 8f + i) * 3f), 6f, new Color(1f, 1f, 1f, 0.7f));
            }
            GUI.EndScrollView();

            var field = new Rect(panel.x + 20f, panel.yMax - 92f, panel.width - 200f, 70f);
            var ev = Event.current;
            bool focused = GUI.GetNameOfFocusedControl() == "DmField";
            if (focused && ev.type == EventType.KeyDown && (ev.keyCode == KeyCode.Return || ev.keyCode == KeyCode.KeypadEnter))
            {
                SocialSystem.Send(d, f, dmDraft);
                dmDraft = "";
                gm.Save();
                ev.Use();
            }
            GUI.SetNextControlName("DmField");
            dmDraft = GUI.TextField(field, dmDraft ?? "", 160, socialField);
            if (string.IsNullOrEmpty(dmDraft) && !focused)
                GUI.Label(new Rect(field.x + 16f, field.y, field.width, field.height), "<color=#777788>Message... (Enter to send)</color>", UIStyles.Sized(UIStyles.Body, 21));
            if (FlatBtn(new Rect(field.xMax + 12f, field.y, 156f, 70f), "SEND", TileGreen, !string.IsNullOrEmpty(dmDraft), 22))
            {
                SocialSystem.Send(d, f, dmDraft);
                dmDraft = "";
                gm.Save();
            }
        }

        // ------------------------------------------------------------------ Achievements

        void DrawAchievementsPanel(PlayerData d, float k)
        {
            var panel = new Rect(W * 0.5f - 680f, H * 0.5f - 440f + (1f - k) * 60f, 1360f, 880f);
            Round(Offset(panel, 0f, 8f), new Color(0f, 0f, 0f, 0.5f), 26f);
            Round(panel, new Color(0.07f, 0.08f, 0.14f, 0.98f), 26f);
            RoundFrame(panel, new Color(1f, 0.7f, 0.3f, 0.8f), 3f, 26f);
            CloseButton(panel);
            if (FlatBtn(new Rect(panel.x + 30f, panel.y + 24f, 150f, 56f), "‹ PROFILE", TileNavy, true, 19)) { OpenSocial(SocialPanel.Profile); return; }
            UIStyles.Outlined(new Rect(panel.x + 200f, panel.y + 22f, 700f, 60f), "ACHIEVEMENTS  <size=28>" + AchievementSystem.Count(d) + " / " + AchievementSystem.All.Count + "</size>", UIStyles.Sized(UIStyles.H1, 42), UIStyles.Gold, 3f);
            var view = new Rect(panel.x + 24f, panel.y + 100f, panel.width - 48f, panel.height - 124f);
            int cols = 2;
            float cw = (view.width - 30f - 20f) / cols, ch = 132f;
            int rows = Mathf.CeilToInt(AchievementSystem.All.Count / (float)cols);
            float contentH = rows * (ch + 14f);
            achScroll = DragScroll(view, achScroll, contentH);
            achScroll = GUI.BeginScrollView(view, achScroll, new Rect(0f, 0f, view.width - 22f, contentH));
            // Unlocked first, then closest to done.
            var list = new List<AchievementSystem.Def>(AchievementSystem.All);
            list.Sort((a, b) =>
            {
                bool ua = AchievementSystem.State(d, a.id).unlocked, ub = AchievementSystem.State(d, b.id).unlocked;
                if (ua != ub) return ua ? -1 : 1;
                float pa = AchievementSystem.Progress(d, a) / (float)a.target, pb = AchievementSystem.Progress(d, b) / (float)b.target;
                return pb.CompareTo(pa);
            });
            for (int i = 0; i < list.Count; i++)
            {
                var a = list[i];
                var s = AchievementSystem.State(d, a.id);
                var r = new Rect((i % cols) * (cw + 20f), (i / cols) * (ch + 14f), cw, ch);
                Color mc = AchievementSystem.MedalColor(a.medal);
                Round(r, s.unlocked ? new Color(mc.r * 0.18f, mc.g * 0.18f, mc.b * 0.18f, 0.9f) : new Color(1f, 1f, 1f, 0.04f), 16f);
                if (s.unlocked) RoundFrame(r, new Color(mc.r, mc.g, mc.b, 0.6f), 2f, 16f);
                MedalIcon(new Vector2(r.x + 70f, r.y + 76f), 34f, a.medal, s.unlocked);
                GUI.Label(new Rect(r.x + 130f, r.y + 12f, r.width - 150f, 34f), "<b>" + a.title + "</b>  <color=#" + UIStyles.Hex(mc) + "><size=16>" + a.medal.ToString().ToUpper() + "</size></color>", UIStyles.Sized(UIStyles.Body, 24));
                GUI.Label(new Rect(r.x + 130f, r.y + 46f, r.width - 150f, 28f), "<color=#BBBBCC>" + a.desc + "</color>", UIStyles.Sized(UIStyles.Small, 18));
                int pr = AchievementSystem.Progress(d, a);
                var bar = new Rect(r.x + 130f, r.y + 86f, r.width - 400f, 14f);
                Round(bar, new Color(0f, 0f, 0f, 0.5f), 7f);
                if (pr > 0) Round(new Rect(bar.x, bar.y, Mathf.Max(14f, bar.width * pr / (float)a.target), bar.height), s.unlocked ? mc : new Color(0.4f, 0.7f, 1f), 7f);
                GUI.Label(new Rect(bar.xMax + 10f, r.y + 76f, 110f, 34f), "<color=#DDDDDD>" + pr.ToString("N0") + "/" + a.target.ToString("N0") + "</color>", UIStyles.Sized(UIStyles.Small, 17));
                GUI.Label(new Rect(r.xMax - 150f, r.y + 76f, 140f, 34f), s.unlocked ? "<color=#7CFF8A>✔ EARNED</color>" : "<color=#FFD36B>" + a.crystals + " ◆</color>", UIStyles.Sized(UIStyles.Right, 19));
            }
            GUI.EndScrollView();
        }

        // ------------------------------------------------------------------ Popup (any screen)

        void DrawAchievementPopup()
        {
            if (achShowing == null)
            {
                if (AchievementSystem.Popups.Count == 0) return;
                achShowing = AchievementSystem.Popups.Dequeue();
                achPopupAt = Time.unscaledTime;
                gm.Audio.Play("perfect", 0.7f);
                gm.Audio.Play("gem", 0.5f);
            }
            float t = Time.unscaledTime - achPopupAt;
            const float dur = 4.2f;
            if (t > dur) { achShowing = null; return; }
            float k = Mathf.Clamp01(t / 0.35f) * Mathf.Clamp01((dur - t) / 0.35f);
            k = 1f - Mathf.Pow(1f - k, 3f);
            var a = achShowing;
            Color mc = AchievementSystem.MedalColor(a.medal);
            var r = new Rect(W * 0.5f - 400f, safe.y + 16f - (1f - k) * 160f, 800f, 150f);
            Round(Offset(r, 0f, 6f), new Color(0f, 0f, 0f, 0.5f), 22f);
            Round(r, new Color(0.08f, 0.07f, 0.14f, 0.97f), 22f);
            RoundFrame(r, mc, 3f, 22f);
            // Shine sweep.
            float sx = Mathf.Repeat(t * 700f, r.width + 300f) - 150f;
            if (sx < r.width) UIStyles.Rect(new Rect(r.x + Mathf.Max(0f, sx), r.y + 4f, Mathf.Min(40f, r.width - sx), r.height - 8f), new Color(1f, 1f, 1f, 0.06f));
            MedalIcon(new Vector2(r.x + 84f, r.y + 88f), 44f, a.medal, true);
            UIStyles.Outlined(new Rect(r.x + 160f, r.y + 12f, 620f, 36f), "ACHIEVEMENT UNLOCKED!", UIStyles.Sized(UIStyles.Body, 24), mc, 2f);
            UIStyles.Outlined(new Rect(r.x + 160f, r.y + 46f, 620f, 50f), a.title, UIStyles.Sized(UIStyles.H1, 38), Color.white, 2f);
            GUI.Label(new Rect(r.x + 160f, r.y + 98f, 620f, 40f), "<color=#BBBBCC>" + a.desc + "</color>   <color=#FFD36B>+" + a.coins.ToString("N0") + " gold  +" + a.crystals + " ◆</color>", UIStyles.Sized(UIStyles.Small, 19));
        }
    }
}
