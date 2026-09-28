using System.Collections.Generic;
using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// NEW DESIGNS: the three premium quality-test fighters standing in the village, each with a character card
    /// (portrait, name, element, level, stars, rarity, power), a design sheet (silhouette, palette, outfit, weapon,
    /// personality) and buttons to open the 3D animation viewer or play them in a 60-second trial.
    /// </summary>
    public partial class UIManager
    {
        static readonly List<string> DesignIds = new List<string> { "kaito_gale", "oboro_iron", "shion_storm" };
        int designPick;
        GUIStyle designNoteStyle;

        static readonly string[] DesignRole = { "FAST MELEE", "HEAVY MELEE", "RANGED / ELEMENTAL" };
        static readonly Color[][] DesignPalette =
        {
            new[] { new Color(0.13f, 0.58f, 0.34f), new Color(0.95f, 0.96f, 0.93f), new Color(0.96f, 0.76f, 0.3f) },
            new[] { new Color(0.66f, 0.08f, 0.09f), new Color(0.1f, 0.08f, 0.09f), new Color(1f, 0.5f, 0.12f) },
            new[] { new Color(0.1f, 0.12f, 0.32f), new Color(1f, 0.84f, 0.22f), new Color(0.6f, 0.32f, 1f) }
        };
        static readonly string[][] DesignNotes =
        {
            new[]
            {
                "Silhouette: swept-back spikes, long high ponytail, headband and scarf tails streaming behind.",
                "Outfit: cropped high-collar jacket over a white wrap, belt pouches, short hakama, shin wraps, split-toe boots.",
                "Weapon: twin kodachi — one forward grip, one reverse grip along the forearm.",
                "Moves: low crouch, bouncing on his toes, long quick strides; both blades cut in mirrored X-strikes.",
                "Special: Crosswind Cyclone — a green cyclone pulls demons in, then an X-cut from above."
            },
            new[]
            {
                "Silhouette: huge frame, triple pauldrons, flat-top with a topknot, one-shoulder tattered cape.",
                "Outfit: lacquered crimson chest plate with gold studs, sun buckle on a wide obi, pleated hakama, iron shin guards.",
                "Weapon: iron kanabo club — corded grip, gold bands, rows of studs, ember cracks that glow.",
                "Moves: wide planted stance, slow weight shifts, club resting on his shoulder; two-handed swings with a deep squat.",
                "Special: Sunbreaker — a leaping slam that splits the ground into erupting rivers of light."
            },
            new[]
            {
                "Silhouette: floats off the ground, gold halo, blunt bangs, side braid, crescent ornament, orbiting charms.",
                "Outfit: two-tier robe, capelet with gold trim, bell sleeves, big obi bow, hanging talismans, geta.",
                "Weapon: staff with a gold crescent cradling a thunder orb, spiked butt cap, hanging charms.",
                "Moves: drifts rather than walks; casting hand always raised and glowing, thrusts it out when attacking.",
                "Special: Heavenly Thunder Array — six charms form a seal, lightning strikes each point, then the centre."
            }
        };

        bool designWorlds;

        void DrawDesigns()
        {
            var d = gm.Data;
            gm.Home.ShowLineup(DesignIds);
            TopBar("NEW DESIGNS", GameScreen.Characters);
            // Tabs: the character quality test and the environment quality test.
            if (FlatBtn(new Rect(safe.x + 560f, safe.y + 26f, 220f, 54f), "CHARACTERS", !designWorlds ? TileRed : new Color(0.16f, 0.17f, 0.26f), true, 20)) designWorlds = false;
            if (FlatBtn(new Rect(safe.x + 790f, safe.y + 26f, 200f, 54f), "WORLDS", designWorlds ? TileRed : new Color(0.16f, 0.17f, 0.26f), true, 20)) designWorlds = true;
            if (designWorlds) { DrawWorldPrototypes(); return; }
            designPick = Mathf.Clamp(designPick, 0, DesignIds.Count - 1);

            // The three fighters in the village: a card under each.
            var cam = Camera.main;
            float s = HudLayout.Scale;
            for (int i = 0; i < DesignIds.Count && cam != null; i++)
            {
                var def = GameDatabase.GetCharacter(DesignIds[i]);
                if (def == null) continue;
                Vector3 feet = gm.Home.LineupSlot(i);
                Vector3 sp = cam.WorldToScreenPoint(feet);
                Vector3 hp = cam.WorldToScreenPoint(feet + Vector3.up * 2.35f);
                if (sp.z <= 0f) continue;
                var fp = new Vector2(sp.x / s, (Screen.height - sp.y) / s);
                var headP = new Vector2(hp.x / s, (Screen.height - hp.y) / s);
                var hit = new Rect(fp.x - 100f, headP.y, 200f, fp.y - headP.y + 10f);
                var owned = d.GetCharacter(def.id);
                int stars = owned != null ? owned.stars : def.rarity;
                int level = owned != null ? owned.level : 1;
                Color rc = RarityInfo.Color(stars);
                Color ec = ElementChart.ColorOf(def.element);
                if (designPick == i) RoundFrame(Grow(hit, 4f), new Color(rc.r, rc.g, rc.b, 0.5f + 0.3f * Mathf.Sin(Time.unscaledTime * 4f)), 3f, 14f);
                var tag = new Rect(headP.x - 100f, headP.y - 46f, 200f, 38f);
                Round(tag, new Color(0.06f, 0.07f, 0.12f, 0.9f), 8f);
                RoundFrame(tag, rc, 2f, 8f);
                UIStyles.Outlined(tag, DesignRole[i], UIStyles.Sized(UIStyles.Center, 18), rc, 2f);

                var card = new Rect(fp.x - 125f, fp.y + 6f, 250f, 150f);
                Round(Offset(card, 0f, 4f), new Color(0f, 0f, 0f, 0.4f), 12f);
                Round(card, Color.Lerp(new Color(0.07f, 0.08f, 0.12f, 0.94f), rc, 0.18f), 12f);
                RoundFrame(card, Color.Lerp(rc, Color.black, 0.2f), 2f, 12f);
                var face = new Rect(card.x + 8f, card.y + 8f, 76f, 76f);
                Aura(face.center, 38f, ec, false);
                var tex = ArtLibrary.Character(def);
                if (tex != null) GUI.DrawTexture(face, tex, ScaleMode.ScaleAndCrop, true);
                UIStyles.CircleTex(new Vector2(face.xMax - 6f, face.yMax - 6f), 14f, Color.Lerp(ec, Color.black, 0.25f));
                GUI.DrawTexture(new Rect(face.xMax - 17f, face.yMax - 17f, 22f, 22f), IconFactory.Get(IconFactory.ForElement(def.element)), ScaleMode.ScaleToFit, true);
                UIStyles.Outlined(new Rect(card.x + 92f, card.y + 6f, 150f, 30f), def.displayName, UIStyles.Sized(UIStyles.Body, 24), Color.white, 2f);
                UIStyles.Outlined(new Rect(card.x + 92f, card.y + 34f, 150f, 24f), RarityInfo.Name(stars), UIStyles.Sized(UIStyles.Body, 16), rc, 1.5f);
                GUI.Label(new Rect(card.x + 92f, card.y + 56f, 150f, 26f), "Lv. " + level + "   " + ElementName(def.element), UIStyles.Sized(UIStyles.Small, 17));
                StarStrip(new Vector2(card.x + 10f, card.y + 90f), stars, 18f, owned != null ? owned.awaken : 0);
                var probe = owned ?? new OwnedCharacter { id = def.id, stars = def.rarity };
                UIStyles.Outlined(new Rect(card.x + 130f, card.y + 86f, 110f, 28f), "⚔ " + CharacterSystem.Power(d, probe).ToString("N0"), UIStyles.Sized(UIStyles.Right, 18), new Color(1f, 0.85f, 0.35f), 1.5f);
                if (FlatBtn(new Rect(card.x + 8f, card.yMax - 40f, 112f, 34f), "VIEW 3D", new Color(0.12f, 0.3f, 0.62f), true, 17))
                {
                    if (owned == null) { InventorySystem.AddCharacter(d, def.id); gm.Save(); Toast(def.displayName + " added to your roster for testing."); }
                    gm.SelectedCharacterId = def.id;
                    gm.GoTo(GameScreen.CharacterDetail);
                    return;
                }
                if (FlatBtn(new Rect(card.xMax - 120f, card.yMax - 40f, 112f, 34f), "TRY ▶", TileRed, true, 17)) { gm.BeginTrial(def.id); return; }
                if (GUI.Button(hit, GUIContent.none, GUIStyle.none)) { gm.Audio.Play("click", 0.5f); designPick = i; gm.Home.LineupCheer(i); }
            }

            // Design sheet for the picked fighter.
            var pdef = GameDatabase.GetCharacter(DesignIds[designPick]);
            if (pdef == null) return;
            float x0 = safe.x + 24f, top = safe.y + 130f;
            var sheet = new Rect(x0, top, 430f, H - top - 30f);
            Round(Offset(sheet, 0f, 5f), new Color(0f, 0f, 0f, 0.35f), 16f);
            Round(sheet, new Color(0.05f, 0.06f, 0.1f, 0.92f), 16f);
            Color prc = RarityInfo.Color(pdef.rarity);
            RoundFrame(sheet, Color.Lerp(prc, Color.black, 0.3f), 2f, 16f);
            float y = sheet.y + 18f;
            UIStyles.Outlined(new Rect(sheet.x + 22f, y, 390f, 44f), pdef.displayName.ToUpper(), UIStyles.Sized(UIStyles.H2, 36), Color.white, 2f);
            y += 44f;
            GUI.Label(new Rect(sheet.x + 22f, y, 390f, 30f), pdef.versionTitle + "  ·  " + pdef.breathingStyle, UIStyles.Sized(UIStyles.Small, 18));
            y += 34f;
            UIStyles.Outlined(new Rect(sheet.x + 22f, y, 390f, 30f), DesignRole[designPick] + "  ·  " + RarityInfo.Name(pdef.rarity) + "  ·  " + ElementName(pdef.element), UIStyles.Sized(UIStyles.Body, 18), prc, 1.5f);
            y += 40f;
            GUI.Label(new Rect(sheet.x + 22f, y, 120f, 30f), "PALETTE", UIStyles.Sized(UIStyles.Body, 18));
            string[] roles = { "primary", "secondary", "accent" };
            for (int k = 0; k < 3; k++)
            {
                var sw = new Rect(sheet.x + 130f + k * 96f, y, 86f, 30f);
                Round(sw, DesignPalette[designPick][k], 6f);
                RoundFrame(sw, new Color(1f, 1f, 1f, 0.25f), 1.5f, 6f);
                GUI.Label(new Rect(sw.x, sw.yMax, sw.width, 22f), "<color=#AAAAAA>" + roles[k] + "</color>", UIStyles.Sized(UIStyles.Center, 14));
            }
            y += 62f;
            var notes = DesignNotes[designPick];
            if (designNoteStyle == null) designNoteStyle = new GUIStyle(UIStyles.Sized(UIStyles.Small, 18)) { wordWrap = true };
            var noteSt = designNoteStyle;
            for (int k = 0; k < notes.Length; k++)
            {
                float nh = noteSt.CalcHeight(new GUIContent(notes[k]), 380f);
                UIStyles.CircleTex(new Vector2(sheet.x + 28f, y + 12f), 5f, prc);
                GUI.Label(new Rect(sheet.x + 42f, y, 370f, nh + 4f), notes[k], noteSt);
                y += nh + 10f;
            }
            y += 6f;
            var foot = new Rect(sheet.x + 16f, Mathf.Max(y, sheet.yMax - 128f), sheet.width - 32f, 112f);
            Round(foot, new Color(1f, 0.85f, 0.35f, 0.08f), 10f);
            GUI.Label(new Rect(foot.x + 12f, foot.y + 6f, foot.width - 24f, foot.height - 12f),
                "<b>Quality test.</b> These three are original designs built to the new standard: jointed limbs, real hands and boots, layered outfits, detailed weapons and faces. Approve them and the rest of the roster gets the same treatment.", noteSt);
        }
    
        // ------------------------------------------------------------------ Worlds (environment prototypes)

        static readonly string[] WorldIds = { "forest", "snow", "volcano" };
        static readonly string[] WorldNames = { "FOREST", "SNOW MOUNTAIN", "VOLCANO" };
        static readonly string[] WorldLandmark = { "The Great Tree", "Frozen Summit Shrine", "The Crater Throne" };
        static readonly string[] WorldRoute =
        {
            "Forest Gate → Mossy Trail → Old Stream Bridge → Moss Ruins → Demon Camp → Heart of the Great Tree",
            "Trailhead → Pine Ridge → Frozen River Bridge → Mountain Shrine → Ice Cave Camp → Frozen Summit Shrine",
            "Ashen Gate → Basalt Path → Lava River Bridge → Ruined Fire Temple → Demon Forge Camp → Crater Throne"
        };
        static readonly string[] WorldMood =
        {
            "Warm sunlight and soft shadows, sunbeams through the canopy, drifting leaves and spirit motes. Green, warm yellow, brown and a clear blue stream.",
            "Cold blue light on bright snow, terraced cliffs, snow-laden pines, ice crystals, a frozen river and a frozen waterfall. White, light blue and deep blue.",
            "Red and orange glow from lava rivers and cracked basalt, burnt trees with ember tips, smoke, ash and embers, an erupting volcano on the horizon."
        };
        static readonly Color[][] WorldPalette =
        {
            new[] { new Color(0.36f, 0.6f, 0.26f), new Color(0.95f, 0.8f, 0.35f), new Color(0.45f, 0.3f, 0.18f), new Color(0.3f, 0.6f, 0.8f) },
            new[] { new Color(0.95f, 0.97f, 1f), new Color(0.65f, 0.85f, 1f), new Color(0.2f, 0.3f, 0.55f), new Color(0.8f, 0.16f, 0.16f) },
            new[] { new Color(0.1f, 0.08f, 0.08f), new Color(0.3f, 0.2f, 0.16f), new Color(1f, 0.45f, 0.08f), new Color(0.85f, 0.15f, 0.1f) }
        };
        readonly Texture2D[] worldArt = new Texture2D[3];

        void DrawWorldPrototypes()
        {
            UIStyles.Rect(new Rect(0f, 106f + safe.y, W, H), new Color(0.03f, 0.04f, 0.07f, 0.72f));
            float top = safe.y + 130f;
            GUI.Label(new Rect(safe.x + 30f, top, W - 60f, 40f), "Environment quality test — three worlds built to the new standard. Play one to see it at gameplay camera distance.", UIStyles.Sized(UIStyles.Body, 22));
            float cw = (safe.width - 60f - 2f * 24f) / 3f, ch = H - top - 90f;
            if (designNoteStyle == null) designNoteStyle = new GUIStyle(UIStyles.Sized(UIStyles.Small, 18)) { wordWrap = true };
            for (int i = 0; i < 3; i++)
            {
                var r = new Rect(safe.x + 30f + i * (cw + 24f), top + 54f, cw, ch);
                Round(Offset(r, 0f, 5f), new Color(0f, 0f, 0f, 0.4f), 16f);
                Round(r, new Color(0.07f, 0.08f, 0.12f, 0.96f), 16f);
                RoundFrame(r, WorldPalette[i][i == 2 ? 2 : 1], 2f, 16f);
                var art = new Rect(r.x + 10f, r.y + 10f, r.width - 20f, (r.width - 20f) * 0.56f);
                if (worldArt[i] == null) worldArt[i] = PaintWorld(i);
                GUI.DrawTexture(art, worldArt[i], ScaleMode.ScaleAndCrop, false);
                float y = art.yMax + 10f;
                UIStyles.Outlined(new Rect(r.x + 18f, y, r.width - 36f, 40f), WorldNames[i], UIStyles.Sized(UIStyles.H2, 30), Color.white, 2f);
                y += 40f;
                UIStyles.Colored(new Rect(r.x + 18f, y, r.width - 36f, 28f), "Landmark: " + WorldLandmark[i], UIStyles.Sized(UIStyles.Body, 18), new Color(1f, 0.85f, 0.4f));
                y += 32f;
                for (int k = 0; k < 4; k++)
                {
                    var sw = new Rect(r.x + 18f + k * 58f, y, 50f, 22f);
                    Round(sw, WorldPalette[i][k], 5f);
                    RoundFrame(sw, new Color(1f, 1f, 1f, 0.2f), 1f, 5f);
                }
                y += 32f;
                float mh = designNoteStyle.CalcHeight(new GUIContent(WorldMood[i]), r.width - 36f);
                GUI.Label(new Rect(r.x + 18f, y, r.width - 36f, mh + 4f), WorldMood[i], designNoteStyle);
                y += mh + 8f;
                float rh = designNoteStyle.CalcHeight(new GUIContent(WorldRoute[i]), r.width - 36f);
                GUI.Label(new Rect(r.x + 18f, y, r.width - 36f, rh + 4f), "<color=#AAAAAA>" + WorldRoute[i] + "</color>", designNoteStyle);
                if (FlatBtn(new Rect(r.x + 18f, r.yMax - 76f, r.width - 36f, 60f), "PLAY ▶", TileRed, true, 26)) { gm.BeginPrototype(WorldIds[i]); return; }
            }
        }

        /// <summary>A small painted preview of each world: sky, layered ranges, the landmark and the foreground.</summary>
        static Texture2D PaintWorld(int k)
        {
            const int w = 320, h = 180;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "WorldArt" + k };
            var px = new Color[w * h];
            Color skyTop = k == 0 ? new Color(0.32f, 0.58f, 0.9f) : k == 1 ? new Color(0.36f, 0.56f, 0.86f) : new Color(0.12f, 0.05f, 0.1f);
            Color skyLow = k == 0 ? new Color(0.85f, 0.92f, 0.88f) : k == 1 ? new Color(0.85f, 0.92f, 0.99f) : new Color(0.7f, 0.25f, 0.12f);
            Color[] layers = k == 0
                ? new[] { new Color(0.55f, 0.7f, 0.75f), new Color(0.35f, 0.55f, 0.4f), new Color(0.2f, 0.42f, 0.2f), new Color(0.36f, 0.6f, 0.26f) }
                : k == 1
                    ? new[] { new Color(0.7f, 0.78f, 0.92f), new Color(0.52f, 0.62f, 0.8f), new Color(0.9f, 0.94f, 1f), new Color(0.96f, 0.98f, 1f) }
                    : new[] { new Color(0.4f, 0.14f, 0.12f), new Color(0.22f, 0.09f, 0.09f), new Color(0.12f, 0.07f, 0.07f), new Color(0.16f, 0.12f, 0.12f) };
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float t = y / (float)h;
                    Color c = Color.Lerp(skyLow, skyTop, Mathf.Pow(t, 0.8f));
                    // Layered ranges, far to near.
                    for (int l = 0; l < 4; l++)
                    {
                        float baseY = h * (0.62f - l * 0.13f);
                        float amp = h * (0.2f - l * 0.03f);
                        float n = Mathf.PerlinNoise(x * (0.012f + l * 0.008f) + l * 13f + k * 7f, l * 3.1f);
                        float ridge = baseY + amp * n;
                        if (k == 1 && l < 2) ridge += Mathf.Max(0f, 1f - Mathf.Abs(((x + l * 60) % 110) - 55f) / 55f) * h * 0.18f;
                        if (y < ridge)
                        {
                            c = layers[l];
                            if (k == 1 && l < 2 && y > ridge - h * 0.05f) c = Color.Lerp(c, Color.white, 0.8f);
                        }
                    }
                    px[y * w + x] = c;
                }
            // Landmark silhouettes.
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float cx = x - w * 0.62f, cy = y - h * 0.3f;
                    if (k == 0)
                    {
                        if (Mathf.Abs(cx) < 7f && cy > 0f && cy < h * 0.35f) px[y * w + x] = new Color(0.32f, 0.22f, 0.15f);
                        float d = new Vector2(cx / 1.3f, cy - h * 0.45f).magnitude;
                        if (d < h * 0.24f) px[y * w + x] = Color.Lerp(new Color(0.46f, 0.68f, 0.26f), new Color(0.2f, 0.42f, 0.2f), d / (h * 0.24f));
                    }
                    else if (k == 1)
                    {
                        for (int tier = 0; tier < 3; tier++)
                        {
                            float ty = h * 0.3f + tier * 14f, tw = 34f - tier * 8f;
                            if (y > ty && y < ty + 10f && Mathf.Abs(cx) < tw - (y - ty) * 1.2f) px[y * w + x] = new Color(0.95f, 0.97f, 1f);
                            if (y > ty - 10f && y <= ty && Mathf.Abs(cx) < tw * 0.6f) px[y * w + x] = new Color(0.72f, 0.14f, 0.12f);
                        }
                    }
                    else
                    {
                        float vx = x - w * 0.5f, vy = y - h * 0.5f;
                        if (vy > 0f && vy < h * 0.35f && Mathf.Abs(vx) < 90f - vy * 1.6f) px[y * w + x] = vy > h * 0.3f ? new Color(1f, 0.5f, 0.1f) : new Color(0.14f, 0.09f, 0.09f);
                        if (vy > h * 0.35f && vy < h * 0.5f && Mathf.Abs(vx + (vy - h * 0.35f) * 0.6f) < 12f - (vy - h * 0.35f) * 0.4f) px[y * w + x] = new Color(0.3f, 0.26f, 0.26f, 1f);
                    }
                }
            // Foreground road and scatter.
            var rnd = new System.Random(k * 31 + 5);
            for (int y = 0; y < h * 0.22f; y++)
                for (int x = 0; x < w; x++)
                {
                    float road = Mathf.Abs(x - w * 0.5f - (h * 0.22f - y) * 0.8f);
                    if (road < 18f + y * 0.9f) px[y * w + x] = k == 0 ? new Color(0.66f, 0.52f, 0.34f) : k == 1 ? new Color(0.68f, 0.74f, 0.86f) : new Color(0.36f, 0.31f, 0.28f);
                }
            for (int i = 0; i < 26; i++)
            {
                int bx = rnd.Next(w), by = rnd.Next((int)(h * 0.3f));
                if (Mathf.Abs(bx - w * 0.5f) < 40) continue;
                int th = 18 + rnd.Next(26);
                Color tc = k == 0 ? new Color(0.18f, 0.4f, 0.18f) : k == 1 ? new Color(0.1f, 0.3f, 0.3f) : new Color(0.08f, 0.06f, 0.06f);
                for (int y = 0; y < th; y++)
                {
                    int half = k == 2 ? 1 : (int)((th - y) * 0.35f);
                    for (int x = -half; x <= half; x++)
                    {
                        int px0 = bx + x, py = by + y;
                        if (px0 < 0 || px0 >= w || py >= h) continue;
                        px[py * w + px0] = k == 1 && y % 7 > 4 ? new Color(0.95f, 0.97f, 1f) : tc;
                    }
                }
                if (k == 2 && by + th < h) px[(by + th) * w + bx] = new Color(1f, 0.5f, 0.1f);
            }
            if (k == 2)
                for (int x = 0; x < w; x++)
                {
                    int ly = (int)(h * 0.12f + Mathf.Sin(x * 0.04f) * 6f);
                    for (int y = ly - 3; y <= ly + 3; y++) if (y >= 0 && y < h && Mathf.Abs(x - w * 0.5f) > 30f) px[y * w + x] = new Color(1f, 0.45f + Mathf.PerlinNoise(x * 0.1f, y * 0.3f) * 0.3f, 0.08f);
                }
            tex.SetPixels(px);
            tex.Apply();
            return tex;
        }
    }
}
