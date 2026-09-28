using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// The village hub's HUD (open world): name tags over the other players with INVITE, the chat box, the party
    /// strip (FIND PARTY / LEAVE), how many players are online, labels over the co-op gates and the gate panel
    /// (ENTER needs a full party of three). Everything online is labelled as a simulated preview.
    /// </summary>
    public partial class UIManager
    {
        string chatDraft = "";
        bool chatOpen = true;
        GUIStyle chatLineStyle, chatFieldStyle;

        void HubBlock(Rect r)
        {
            if (Event.current.type == EventType.Repaint) MobileControls.UiBlockers.Add(r);
        }

        /// <summary>The online line in the objective panel.</summary>
        string HubOnlineText()
        {
            var hub = VillageHub.Instance;
            int here = hub != null ? hub.Players.Count + 1 : 1;
            return "<color=#6BFF8A>●</color> <color=#DDDDDD>" + VillageOnline.OnlineNow.ToString("N0") + " online · " + here + " in the village</color>  <color=#8899AA><size=16>(simulated preview)</size></color>";
        }

        void DrawVillageHub(BattleController b)
        {
            var hub = VillageHub.Instance;
            if (hub == null) return;
            if (Event.current.type == EventType.Repaint)
            {
                MobileControls.UiBlockers.Clear();
                MobileControls.UiBlockersFrame = Time.frameCount;
            }
            if (chatLineStyle == null)
            {
                chatLineStyle = new GUIStyle(UIStyles.Sized(UIStyles.Small, 18)) { wordWrap = false, clipping = TextClipping.Clip, richText = true };
                chatFieldStyle = new GUIStyle(GUI.skin.textField) { fontSize = 20, alignment = TextAnchor.MiddleLeft, padding = new RectOffset(12, 12, 4, 4) };
            }
            var cam = Camera.main;
            var a = b.Team.Active;
            DrawHubTags(hub, cam, a);
            DrawGateLabels(cam, a);
            Rect chat = DrawHubChat(hub);
            DrawHubParty(hub, b, new Rect(chat.x, chat.yMax + 10f, chat.width, 92f));
            if (hub.NearGate >= 0) DrawGatePanel(hub, hub.NearGate);
        }

        void DrawHubTags(VillageHub hub, Camera cam, PlayerCharacter a)
        {
            if (cam == null) return;
            float s = HudLayout.Scale;
            foreach (var p in hub.Players)
            {
                if (p.walker == null) continue;
                Vector3 wp = p.walker.transform.position;
                if (a != null && (wp - a.Position).magnitude > 24f) continue;
                Vector3 sp = cam.WorldToScreenPoint(wp + Vector3.up * 2.2f);
                if (sp.z < 0f) continue;
                var c = new Vector2(sp.x / s, (Screen.height - sp.y) / s);
                string label = (p.inParty ? "<color=#FFD36B>★</color> " : "") + "<b>" + p.name + "</b>  <color=#AAB4C8>Lv " + p.level + "</color>";
                float w = Mathf.Clamp(p.name.Length * 13f + 90f, 150f, 300f);
                var r = new Rect(c.x - w * 0.5f, c.y - 6f, w, 30f);
                Round(r, new Color(0.03f, 0.04f, 0.08f, 0.66f), 12f);
                Round(new Rect(r.x + 8f, r.y + 10f, 10f, 10f), p.color, 5f);
                GUI.Label(new Rect(r.x + 24f, r.y + 1f, r.width - 28f, 28f), label, chatLineStyle);
                if (a != null && hub.CanInvite(p) && (wp - a.Position).magnitude < 4.5f)
                {
                    var br = new Rect(c.x - 70f, r.yMax + 6f, 140f, 44f);
                    HubBlock(br);
                    if (FlatBtn(br, "INVITE", TileGreen, true, 20)) hub.Invite(p);
                }
            }
        }

        void DrawGateLabels(Camera cam, PlayerCharacter a)
        {
            if (cam == null) return;
            float s = HudLayout.Scale;
            for (int i = 0; i < PrototypeWorld.CoopGateSpots.Count && i < GameDatabase.CoopGateNames.Length; i++)
            {
                Vector3 gp = PrototypeWorld.CoopGateSpots[i];
                if (a != null && (gp - a.Position).magnitude > 32f) continue;
                Vector3 sp = cam.WorldToScreenPoint(gp + Vector3.up * 4.9f);
                if (sp.z < 0f) continue;
                var c = new Vector2(sp.x / s, (Screen.height - sp.y) / s);
                Color gc = VillageHub.GateColor(i);
                UIStyles.Outlined(new Rect(c.x - 200f, c.y - 44f, 400f, 36f), GameDatabase.CoopGateNames[i], UIStyles.Sized(UIStyles.Center, 26), Color.white, 2f);
                UIStyles.Outlined(new Rect(c.x - 200f, c.y - 12f, 400f, 28f), "CO-OP · " + GameDatabase.CoopGateTiers[i], UIStyles.Sized(UIStyles.Center, 19), gc, 2f);
            }
        }

        Rect DrawHubChat(VillageHub hub)
        {
            float x0 = Mathf.Max(safe.x + 590f, W * 0.5f - 330f);
            float w = Mathf.Min(660f, W - 380f - x0);
            var r = new Rect(x0, safe.y + 20f, w, chatOpen ? 268f : 58f);
            HubBlock(r);
            Round(r, new Color(0.03f, 0.04f, 0.08f, 0.66f), 14f);
            GUI.Label(new Rect(r.x + 16f, r.y + 8f, r.width - 120f, 30f), "<color=#FFD36B><b>VILLAGE CHAT</b></color>  <color=#8899AA><size=15>simulated preview</size></color>", chatLineStyle);
            if (GUI.Button(new Rect(r.xMax - 92f, r.y + 6f, 80f, 36f), GUIContent.none, GUIStyle.none)) chatOpen = !chatOpen;
            Round(new Rect(r.xMax - 92f, r.y + 8f, 80f, 32f), new Color(1f, 1f, 1f, 0.1f), 10f);
            UIStyles.Outlined(new Rect(r.xMax - 92f, r.y + 8f, 80f, 32f), chatOpen ? "HIDE" : "SHOW", UIStyles.Sized(UIStyles.Center, 16), Color.white, 1f);
            if (!chatOpen)
            {
                if (hub.Chat.Count > 0)
                {
                    var last = hub.Chat[hub.Chat.Count - 1];
                    GUI.Label(new Rect(r.x + 250f, r.y + 10f, r.width - 350f, 30f), ChatText(last), chatLineStyle);
                }
                MobileControls.KeyboardBlocked = false;
                return r;
            }
            // The latest lines, newest at the bottom.
            const int shown = 6;
            int start = Mathf.Max(0, hub.Chat.Count - shown);
            for (int i = start; i < hub.Chat.Count; i++)
                GUI.Label(new Rect(r.x + 16f, r.y + 44f + (i - start) * 28f, r.width - 32f, 28f), ChatText(hub.Chat[i]), chatLineStyle);
            // Input line.
            var field = new Rect(r.x + 12f, r.yMax - 50f, r.width - 134f, 40f);
            var ev = Event.current;
            bool focused = GUI.GetNameOfFocusedControl() == "VillageChat";
            if (focused && ev.type == EventType.KeyDown && (ev.keyCode == KeyCode.Return || ev.keyCode == KeyCode.KeypadEnter))
            {
                hub.Send(chatDraft);
                chatDraft = "";
                ev.Use();
            }
            if (focused && ev.type == EventType.KeyDown && ev.keyCode == KeyCode.Escape)
            {
                GUI.FocusControl(null);
                ev.Use();
            }
            GUI.SetNextControlName("VillageChat");
            chatDraft = GUI.TextField(field, chatDraft ?? "", 120, chatFieldStyle);
            if (string.IsNullOrEmpty(chatDraft) && !focused)
                GUI.Label(new Rect(field.x + 14f, field.y + 6f, field.width - 20f, 28f), "<color=#8A8A99>Say something... (Enter to send)</color>", chatLineStyle);
            if (FlatBtn(new Rect(r.xMax - 114f, r.yMax - 52f, 102f, 44f), "SEND", new Color(0.24f, 0.44f, 0.8f), !string.IsNullOrEmpty(chatDraft), 20))
            {
                hub.Send(chatDraft);
                chatDraft = "";
            }
            MobileControls.KeyboardBlocked = GUI.GetNameOfFocusedControl() == "VillageChat";
            return r;
        }

        static string ChatText(VillageHub.ChatLine l)
        {
            // Player text is shown as plain text (no rich-text tags from what people type).
            string t = l.text.Replace("<", "‹").Replace(">", "›");
            if (l.system) return "<color=#" + UIStyles.Hex(l.color) + "><i>" + t + "</i></color>";
            return "<color=#" + UIStyles.Hex(l.color) + "><b>" + l.who.Replace("<", "‹") + "</b></color><color=#EEEEEE>: " + t + "</color>";
        }

        void DrawHubParty(VillageHub hub, BattleController b, Rect r)
        {
            HubBlock(r);
            Round(r, new Color(0.03f, 0.04f, 0.08f, 0.66f), 14f);
            GUI.Label(new Rect(r.x + 16f, r.y + 6f, 160f, 26f), "<color=#FFD36B><b>PARTY " + (hub.Party.Count + 1) + "/" + VillageHub.PartySize + "</b></color>", chatLineStyle);
            float slotW = Mathf.Min(150f, (r.width - 200f) / 3f);
            var a = b.Team.Active;
            for (int i = 0; i < VillageHub.PartySize; i++)
            {
                var sr = new Rect(r.x + 14f + i * (slotW + 8f), r.y + 36f, slotW, 44f);
                if (i == 0)
                {
                    Color el = a != null ? ElementChart.ColorOf(a.Element) : UIStyles.Gold;
                    Round(sr, new Color(el.r * 0.35f, el.g * 0.35f, el.b * 0.35f, 0.9f), 10f);
                    GUI.Label(new Rect(sr.x + 10f, sr.y + 8f, sr.width - 14f, 28f), "<b>" + VillageHub.YourName + "</b>", chatLineStyle);
                }
                else if (i - 1 < hub.Party.Count)
                {
                    var p = hub.Party[i - 1];
                    Round(sr, new Color(p.color.r * 0.35f, p.color.g * 0.35f, p.color.b * 0.35f, 0.9f), 10f);
                    GUI.Label(new Rect(sr.x + 10f, sr.y + 8f, sr.width - 14f, 28f), "<b>" + p.name + "</b>", chatLineStyle);
                }
                else
                {
                    Round(sr, new Color(1f, 1f, 1f, 0.06f), 10f);
                    GUI.Label(new Rect(sr.x + 10f, sr.y + 8f, sr.width - 14f, 28f), hub.Searching ? "<color=#AAAAAA>searching…</color>" : "<color=#777788>empty</color>", chatLineStyle);
                }
            }
            var br = new Rect(r.xMax - 176f, r.y + 20f, 162f, 56f);
            if (hub.Party.Count >= VillageHub.PartySize - 1)
            {
                if (FlatBtn(br, "LEAVE PARTY", new Color(0.5f, 0.24f, 0.26f), true, 19)) hub.LeaveParty();
            }
            else if (FlatBtn(br, hub.Searching ? "SEARCHING" : "FIND PARTY", TileGreen, !hub.Searching, 20)) hub.FindParty();
        }

        void DrawGatePanel(VillageHub hub, int gate)
        {
            if (gate >= GameDatabase.CoopGateNames.Length) return;
            Color gc = VillageHub.GateColor(gate);
            var r = new Rect(W * 0.5f - 320f, H * 0.52f, 640f, 226f);
            HubBlock(r);
            Round(r, new Color(0.03f, 0.03f, 0.07f, 0.86f), 16f);
            Round(new Rect(r.x, r.y + 14f, 6f, r.height - 28f), gc, 3f);
            UIStyles.Outlined(new Rect(r.x + 24f, r.y + 10f, r.width - 48f, 44f), GameDatabase.CoopGateNames[gate], UIStyles.Sized(UIStyles.H2, 34), Color.white, 2f);
            float power = GameDatabase.CoopGatePower[gate];
            GUI.Label(new Rect(r.x + 24f, r.y + 58f, r.width - 48f, 30f), "<color=#" + UIStyles.Hex(gc) + "><b>CO-OP · " + GameDatabase.CoopGateTiers[gate] + "</b></color>   <color=#FF8A7A>Demons " + power.ToString("0.#") + "x stronger</color>", chatLineStyle);
            GUI.Label(new Rect(r.x + 24f, r.y + 88f, r.width - 48f, 30f), "<color=#CCCCCC>Rewards x" + (2 + gate) + " · +" + (100 * (gate + 1)) + " diamonds on the first clear</color>", chatLineStyle);
            bool full = hub.Party.Count >= VillageHub.PartySize - 1;
            var br = new Rect(r.xMax - 244f, r.yMax - 84f, 220f, 66f);
            if (full)
            {
                GUI.Label(new Rect(r.x + 24f, r.yMax - 70f, r.width - 290f, 40f), "<color=#6BFF8A>Party of 3 ready</color>", UIStyles.Sized(UIStyles.Body, 22));
                if (FlatBtn(br, "ENTER GATE", new Color(0.75f, 0.2f, 0.2f), true, 26)) hub.EnterGate(gate);
            }
            else
            {
                GUI.Label(new Rect(r.x + 24f, r.yMax - 76f, r.width - 290f, 56f), "<color=#FFB36B>You need a party of 3 to enter (" + (hub.Party.Count + 1) + "/3). Invite players nearby or find a party.</color>", UIStyles.Sized(UIStyles.Small, 18));
                if (FlatBtn(br, hub.Searching ? "SEARCHING" : "FIND PARTY", TileGreen, !hub.Searching, 24)) hub.FindParty();
            }
        }
    }
}
