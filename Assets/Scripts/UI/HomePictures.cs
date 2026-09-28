using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>Pictures inside the home tiles: faces for CHARACTERS, the team together for TEAM, and so on.</summary>
    public partial class UIManager
    {
        void FaceCircle(Vector2 c, float r, CharacterDefinition def, Color ring)
        {
            UIStyles.CircleTex(c, r + 3f, ring);
            UIStyles.CircleTex(c, r, new Color(0.1f, 0.1f, 0.16f));
            var tex = ArtLibrary.Character(def);
            if (tex != null) GUI.DrawTexture(new Rect(c.x - r, c.y - r, r * 2f, r * 2f), tex, ScaleMode.ScaleAndCrop, true, 0f, Color.white, 0f, r);
        }

        /// <summary>The three Mythics of the banners, fanned out with a diamond.</summary>
        void PicSummon(Rect r)
        {
            var ids = SummonSystem.FeaturedIds;
            int[] order = { 0, 2, 1 }; // the middle one last, in front
            foreach (int k in order)
            {
                if (k >= ids.Length) continue;
                var d2 = GameDatabase.GetCharacter(ids[k]);
                if (d2 == null) continue;
                float cx = r.x + r.width * (0.26f + k * 0.24f);
                float rad = k == 1 ? r.height * 0.36f : r.height * 0.28f;
                FaceCircle(new Vector2(cx, r.center.y + (k == 1 ? -4f : 8f)), rad, d2, RarityInfo.Color(6));
            }
            DiamondIcon(new Vector2(r.xMax - 26f, r.y + 24f), 34f);
        }

        /// <summary>A little grid of the slayers you own.</summary>
        void PicCharacters(Rect r)
        {
            var d = gm.Data;
            int n = Mathf.Min(6, d.characters.Count);
            int cols = 3;
            float rad = Mathf.Min(r.width / (cols * 2.4f), r.height / 4.8f);
            for (int i = 0; i < n; i++)
            {
                var def = GameDatabase.GetCharacter(d.characters[i].id);
                if (def == null) continue;
                float cx = r.x + r.width * (0.2f + (i % cols) * 0.3f), cy = r.y + r.height * (i < 3 ? 0.3f : 0.74f);
                FaceCircle(new Vector2(cx, cy), rad, def, ElementChart.ColorOf(def.element));
            }
        }

        /// <summary>The team standing together.</summary>
        void PicTeam(Rect r)
        {
            var d = gm.Data;
            int n = Mathf.Min(3, d.team.Count);
            for (int pass = 0; pass < 2; pass++)
                for (int i = 0; i < n; i++)
                {
                    bool middle = i == 0;
                    if ((pass == 1) != middle) continue; // leader drawn last, in front
                    var def = GameDatabase.GetCharacter(d.team[i]);
                    if (def == null) continue;
                    var tex = ArtLibrary.CharacterFull(def);
                    if (tex == null) continue;
                    float h = r.height * (middle ? 1.12f : 0.96f), w = h * tex.width / Mathf.Max(1f, tex.height);
                    float cx = r.center.x + (middle ? 0f : (i == 1 ? -1f : 1f) * r.width * 0.26f);
                    GUI.DrawTexture(new Rect(cx - w * 0.5f, r.yMax - h + 6f, w, h), tex, ScaleMode.ScaleToFit, true);
                }
        }

        /// <summary>The leader with a big green arrow and stars.</summary>
        void PicUpgrade(Rect r)
        {
            var d = gm.Data;
            var def = d.team.Count > 0 ? GameDatabase.GetCharacter(d.team[0]) : null;
            if (def != null) FaceCircle(new Vector2(r.x + r.width * 0.38f, r.center.y + 4f), r.height * 0.36f, def, UIStyles.Gold);
            float bob = Mathf.Abs(Mathf.Sin(Time.unscaledTime * 3f)) * 6f;
            var ar = new Rect(r.x + r.width * 0.62f, r.y + r.height * 0.14f - bob, r.height * 0.55f, r.height * 0.55f);
            var o = GUI.color;
            GUI.color = new Color(0.4f, 1f, 0.45f);
            GUI.DrawTexture(ar, IconFactory.Get("up"), ScaleMode.ScaleToFit, true);
            GUI.color = o;
            UIStyles.Outlined(new Rect(r.x, r.yMax - 30f, r.width, 30f), "★★★", UIStyles.Sized(UIStyles.Center, 22), new Color(1f, 0.82f, 0.3f), 1.5f);
        }

        /// <summary>A mission scroll with the next demon to beat.</summary>
        void PicMissions(Rect r)
        {
            var next = gm.NextStoryMission();
            EnemyDefinition e = null;
            if (next != null) e = GameDatabase.GetEnemy(!string.IsNullOrEmpty(next.bossId) ? next.bossId : (next.waves.Count > 0 && next.waves[0].spawns.Count > 0 ? next.waves[0].spawns[0].enemyId : null));
            var o = GUI.color;
            GUI.color = new Color(1f, 0.9f, 0.75f);
            GUI.DrawTexture(new Rect(r.x + 4f, r.y + r.height * 0.1f, r.height * 0.8f, r.height * 0.8f), IconFactory.Get("scroll"), ScaleMode.ScaleToFit, true);
            GUI.color = o;
            if (e != null)
            {
                var tex = ArtLibrary.Monster(e);
                float rad = r.height * 0.32f;
                Vector2 c = new Vector2(r.xMax - rad - 6f, r.center.y);
                UIStyles.CircleTex(c, rad + 3f, UIStyles.Crimson);
                if (tex != null) GUI.DrawTexture(new Rect(c.x - rad, c.y - rad, rad * 2f, rad * 2f), tex, ScaleMode.ScaleAndCrop, true, 0f, Color.white, 0f, rad);
            }
        }

        /// <summary>A pile of diamonds and gold.</summary>
        void PicShop(Rect r)
        {
            float t = Time.unscaledTime;
            CoinIcon(new Vector2(r.x + r.width * 0.3f, r.center.y + 16f), r.height * 0.42f);
            CoinIcon(new Vector2(r.x + r.width * 0.42f, r.center.y + 26f), r.height * 0.36f);
            DiamondIcon(new Vector2(r.x + r.width * 0.66f, r.center.y - 4f + Mathf.Sin(t * 2f) * 3f), r.height * 0.55f);
            DiamondIcon(new Vector2(r.x + r.width * 0.8f, r.center.y + 22f), r.height * 0.34f);
        }

        /// <summary>Three pieces of gear, fanned out.</summary>
        void PicInventory(Rect r)
        {
            string[] ids = { "sword_moonfall", "acc_phoenixheart", "haori_starweave" };
            for (int i = 0; i < ids.Length; i++)
            {
                var e = GameDatabase.GetEquipment(ids[i]);
                if (e == null) continue;
                float s = r.height * (i == 1 ? 0.78f : 0.62f);
                float cx = r.x + r.width * (0.24f + i * 0.26f);
                var gr = new Rect(cx - s * 0.5f, r.center.y - s * 0.5f + (i == 1 ? -4f : 6f), s, s);
                var tex = GearArt.Get(e);
                if (tex != null) GUI.DrawTexture(gr, tex, ScaleMode.ScaleToFit, true);
            }
        }
    }
}
