using System.Collections.Generic;
using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// NEW DESIGNS → ROSTER: the six Character Design Bible reference slayers standing together in a promotional
    /// line-up, each in their own natural showcase pose. Tap one for their design sheet; SILHOUETTE turns everyone
    /// into flat black shapes to check they can still be told apart. VIEW 3D and TRY open the viewer or a trial.
    /// </summary>
    public partial class UIManager
    {
        static readonly List<string> RosterIds = new List<string>
            { "tobi_kazami", "bunta_okuyama", "sayo_mikage", "nene_hanabusa", "nagi_kurokiri", "seiran_mizuchi" };
        static readonly string[] RosterRole = { "FAST MELEE", "HEAVY", "RANGED", "SUPPORT", "ASSASSIN", "ELEMENTAL POWER" };
        static readonly string[] RosterPersona = { "Cocky · quick", "Easygoing · powerful", "Cool · composed", "Cheerful · gentle", "Silent · mysterious", "Serene · commanding" };
        static readonly Color[][] RosterPalette =
        {
            new[] { new Color(0.62f, 0.86f, 0.2f), new Color(0.2f, 0.25f, 0.3f), new Color(1f, 0.52f, 0.12f) },
            new[] { new Color(0.72f, 0.32f, 0.16f), new Color(0.34f, 0.22f, 0.14f), new Color(0.86f, 0.66f, 0.28f) },
            new[] { new Color(0.14f, 0.42f, 0.28f), new Color(0.94f, 0.9f, 0.8f), new Color(0.78f, 0.12f, 0.2f) },
            new[] { new Color(0.72f, 0.62f, 0.92f), new Color(0.98f, 0.95f, 0.88f), new Color(1f, 0.66f, 0.55f) },
            new[] { new Color(0.17f, 0.17f, 0.21f), new Color(0.4f, 0.2f, 0.42f), new Color(0.7f, 1f, 0.25f) },
            new[] { new Color(0.1f, 0.2f, 0.45f), new Color(0.84f, 0.87f, 0.92f), new Color(0.3f, 0.9f, 0.95f) }
        };
        static readonly string[][] RosterNotes =
        {
            new[]
            {
                "Silhouette: short and light with long legs; swept-back hair and orange headband tails streaming behind.",
                "Face: round head, sharp upturned eyes, a grin with one fang, a bandage across the nose.",
                "Outfit: sleeveless lime jacket, bare arms, cropped trousers, long shin wraps, split-toe tabi.",
                "Weapon: short straight blade with a square guard; rests it on his shoulder.",
                "Moves: fast tempo, high knees, forward lean, bouncing on his toes."
            },
            new[]
            {
                "Silhouette: towering and broad, a small head on huge shoulders, one great iron pauldron, top-knot.",
                "Face: square jaw, heavy brows, small calm eyes, broad nose, a cheek scar, a lopsided smile.",
                "Outfit: open rust vest over a bare chest, thick rope belt, wide hakama, iron greaves and heavy boots.",
                "Weapon: iron war hammer with brass bands and a glowing core; on his shoulder, other hand on the hip.",
                "Moves: slow heavy tempo, wide planted stance, the whole body turns into every swing."
            },
            new[]
            {
                "Silhouette: tall and slim, very long legs, high ponytail, crimson half-cape over the bow shoulder.",
                "Face: narrow oval head, almond eyes with lashes, fine brows, a beauty mark.",
                "Outfit: forest-green long jacket split at the hips, crimson high collar, thigh-high boots, quiver on the back.",
                "Weapon: tall asymmetric longbow (grip a third of the way up), held low; the other hand on her hip.",
                "Moves: long unhurried strides, almost no bounce, upright and still when aiming."
            },
            new[]
            {
                "Silhouette: tiny and round, a big head, a round bob, huge bell sleeves, a big peach obi bow.",
                "Face: very round eyes, soft brows, a little 'o' of a mouth, rosy cheeks; flower pin in her hair.",
                "Outfit: lavender over-robe to the ankles on a cream under-robe, wooden geta.",
                "Weapon: lantern staff: a curled crook with a glowing paper lantern; both hands resting on it.",
                "Moves: small light steps, a floaty sway, soft gentle swings."
            },
            new[]
            {
                "Silhouette: small and compact, a deep hood with a hanging point, a trailing tattered scarf.",
                "Face: hidden below the eyes by a cloth mask; narrow, sharp, upturned eyes; one long lock of pale hair.",
                "Outfit: close charcoal wraps, a plum tabard edged in acid green, pouch belt, tabi.",
                "Weapon: twin sickles with short chains, held low at both sides.",
                "Moves: a forward hunch, soft quiet steps, sudden explosive lunges."
            },
            new[]
            {
                "Silhouette: the tallest, long legs, a stiff collar rising behind the head, a long panelled cape, a long braid.",
                "Face: calm oval face, level brows, lashes; a silver circlet with swept fins and an aqua gem.",
                "Outfit: deep-blue long coat with silver edges, silver shoulder guards, greaves.",
                "Weapon: water glaive: a long curved blade over a spinning ring of water; a water orb in the free hand.",
                "Moves: slow and deliberate, long strides, nothing wasted, heavy committed swings."
            }
        };

        int rosterDesignPick;

        void DrawRosterDesigns()
        {
            var d = gm.Data;
            gm.Home.ShowLineup(RosterIds);
            rosterDesignPick = Mathf.Clamp(rosterDesignPick, 0, RosterIds.Count - 1);
            var cam = Camera.main;
            float s = HudLayout.Scale;
            if (designNoteStyle == null) designNoteStyle = new GUIStyle(UIStyles.Sized(UIStyles.Small, 18)) { wordWrap = true };

            // The six checks from the brief, and the silhouette switch.
            var check = new Rect(safe.x + 24f, safe.y + 104f, 560f, 40f);
            GUI.Label(check, "<b>Roster test.</b> Can you tell all six apart instantly, even in silhouette?", UIStyles.Sized(UIStyles.Body, 20));
            if (FlatBtn(new Rect(safe.xMax - 250f, safe.y + 100f, 226f, 50f), gm.Home.Silhouette ? "COLOUR" : "SILHOUETTE", gm.Home.Silhouette ? TileRed : new Color(0.16f, 0.17f, 0.26f), true, 20))
                gm.Home.Silhouette = !gm.Home.Silhouette;

            // A nameplate over each slayer.
            for (int i = 0; i < RosterIds.Count && cam != null; i++)
            {
                var def = GameDatabase.GetCharacter(RosterIds[i]);
                if (def == null) continue;
                Vector3 feet = gm.Home.LineupSlot(i);
                Vector3 sp = cam.WorldToScreenPoint(feet);
                Vector3 hp = cam.WorldToScreenPoint(feet + Vector3.up * 2.7f);
                if (sp.z <= 0f) continue;
                var fp = new Vector2(sp.x / s, (Screen.height - sp.y) / s);
                var headP = new Vector2(hp.x / s, (Screen.height - hp.y) / s);
                Color ec = ElementChart.ColorOf(def.element);
                Color rc = RarityInfo.Color(def.rarity);
                var hit = new Rect(fp.x - 80f, headP.y, 160f, fp.y - headP.y + 10f);
                if (rosterDesignPick == i) RoundFrame(Grow(hit, 4f), new Color(rc.r, rc.g, rc.b, 0.5f + 0.3f * Mathf.Sin(Time.unscaledTime * 4f)), 3f, 14f);
                var tag = new Rect(fp.x - 84f, fp.y + 8f, 168f, 58f);
                Round(tag, new Color(0.06f, 0.07f, 0.12f, 0.9f), 8f);
                RoundFrame(tag, gm.Home.Silhouette ? new Color(0.5f, 0.5f, 0.5f) : ec, 2f, 8f);
                UIStyles.Outlined(new Rect(tag.x, tag.y + 2f, tag.width, 28f), gm.Home.Silhouette ? "?" : def.displayName.ToUpper(), UIStyles.Sized(UIStyles.Center, 20), Color.white, 2f);
                UIStyles.Outlined(new Rect(tag.x, tag.y + 29f, tag.width, 24f), RosterRole[i], UIStyles.Sized(UIStyles.Center, 14), rc, 1.5f);
                if (GUI.Button(hit, GUIContent.none, GUIStyle.none)) { gm.Audio.Play("click", 0.5f); rosterDesignPick = i; gm.Home.LineupCheer(i); }
            }

            // Design sheet for the picked slayer (bottom-left), buttons to view or play.
            var pdef = GameDatabase.GetCharacter(RosterIds[rosterDesignPick]);
            if (pdef == null) return;
            var sheet = new Rect(safe.x + 24f, H - 330f, 620f, 300f);
            Round(Offset(sheet, 0f, 5f), new Color(0f, 0f, 0f, 0.35f), 16f);
            Round(sheet, new Color(0.05f, 0.06f, 0.1f, 0.92f), 16f);
            Color prc = RarityInfo.Color(pdef.rarity);
            RoundFrame(sheet, Color.Lerp(prc, Color.black, 0.3f), 2f, 16f);
            float y = sheet.y + 12f;
            UIStyles.Outlined(new Rect(sheet.x + 18f, y, 300f, 38f), pdef.displayName.ToUpper(), UIStyles.Sized(UIStyles.H2, 30), Color.white, 2f);
            GUI.Label(new Rect(sheet.x + 250f, y + 6f, 360f, 30f), RosterRole[rosterDesignPick] + "  ·  " + RarityInfo.Name(pdef.rarity) + "  ·  " + ElementName(pdef.element) + "  ·  " + RosterPersona[rosterDesignPick], UIStyles.Sized(UIStyles.Small, 16));
            y += 42f;
            for (int k = 0; k < 3; k++)
            {
                var sw = new Rect(sheet.x + 18f + k * 60f, y, 52f, 18f);
                Round(sw, RosterPalette[rosterDesignPick][k], 5f);
                RoundFrame(sw, new Color(1f, 1f, 1f, 0.25f), 1f, 5f);
            }
            GUI.Label(new Rect(sheet.x + 200f, y - 4f, 400f, 24f), "<color=#AAAAAA>" + pdef.versionTitle + " · " + pdef.breathingStyle + "</color>", UIStyles.Sized(UIStyles.Small, 15));
            y += 26f;
            var notes = RosterNotes[rosterDesignPick];
            for (int k = 0; k < notes.Length; k++)
            {
                float nh = designNoteStyle.CalcHeight(new GUIContent(notes[k]), sheet.width - 50f);
                UIStyles.CircleTex(new Vector2(sheet.x + 24f, y + 11f), 4f, prc);
                GUI.Label(new Rect(sheet.x + 36f, y, sheet.width - 50f, nh + 2f), notes[k], designNoteStyle);
                y += nh + 4f;
            }
            var owned = d.GetCharacter(pdef.id);
            if (FlatBtn(new Rect(sheet.xMax + 14f, sheet.yMax - 124f, 180f, 54f), "VIEW 3D", new Color(0.12f, 0.3f, 0.62f), true, 20))
            {
                if (owned == null) { InventorySystem.AddCharacter(d, pdef.id); gm.Save(); Toast(pdef.displayName + " added to your roster for testing."); }
                gm.Home.Silhouette = false;
                gm.SelectedCharacterId = pdef.id;
                gm.GoTo(GameScreen.CharacterDetail);
                return;
            }
            if (FlatBtn(new Rect(sheet.xMax + 14f, sheet.yMax - 62f, 180f, 54f), "TRY ▶", TileRed, true, 20)) { gm.Home.Silhouette = false; gm.BeginTrial(pdef.id); return; }
        }
    }
}
