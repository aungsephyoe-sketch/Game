using System.Collections.Generic;
using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// Higgsfield-generated 2D art (character masters, bestiary, region key art) loaded from Resources/Art.
    /// Everything is optional: screens fall back to plain panels when an image hasn't been downloaded
    /// (see tools/fetch_art.sh).
    /// </summary>
    public static class ArtLibrary
    {
        static readonly Dictionary<string, Texture2D> cache = new Dictionary<string, Texture2D>();

        static Texture2D Load(string path)
        {
            Texture2D t;
            if (cache.TryGetValue(path, out t)) return t;
            t = Resources.Load<Texture2D>(path);
            cache[path] = t;
            return t;
        }

        /// <summary>Master art for a character (by base id, e.g. "ren" for every version of Ren).</summary>
        public static Texture2D Character(CharacterDefinition def)
        {
            if (def == null) return null;
            return Load("Art/Characters/" + (string.IsNullOrEmpty(def.baseId) ? def.id : def.baseId));
        }

        public static Texture2D Monster(EnemyDefinition def)
        {
            if (def == null || string.IsNullOrEmpty(def.artKey)) return null;
            return Load("Art/Monsters/" + def.artKey);
        }

        public static Texture2D Region(string regionId)
        {
            if (string.IsNullOrEmpty(regionId)) return null;
            // The fallen capital reuses the capital art; everything else has its own.
            return Load("Art/Regions/" + (regionId == "fallen" ? "kingdom" : regionId));
        }

        public static Texture2D WorldMap() { return Load("Art/Regions/worldmap"); }

        /// <summary>Chapter key art: a cinematic keyframe for the big story moments, else the chapter's region.</summary>
        public static Texture2D Chapter(ChapterDefinition ch)
        {
            if (ch == null) return null;
            string key = ch.number == 1 ? "awakening" : ch.number == 5 ? "guardian_reveal" : ch.number == 7 ? "final_confrontation" : null;
            var t = key != null ? Load("Art/Keyframes/" + key) : null;
            return t != null ? t : Region(ch.regionId);
        }

        /// <summary>Draws a texture cropped to fill the rect (like CSS object-fit: cover).</summary>
        public static void DrawCover(Rect r, Texture2D tex, float alpha = 1f)
        {
            if (tex == null) return;
            float ta = (float)tex.width / tex.height, ra = r.width / r.height;
            Rect uv = ta > ra ? new Rect((1f - ra / ta) * 0.5f, 0f, ra / ta, 1f) : new Rect(0f, (1f - ta / ra) * 0.5f, 1f, ta / ra);
            var old = GUI.color;
            GUI.color = new Color(old.r, old.g, old.b, old.a * alpha);
            GUI.DrawTextureWithTexCoords(r, tex, uv);
            GUI.color = old;
        }

        /// <summary>Draws a texture scaled to fit inside the rect, keeping its aspect ratio.</summary>
        public static void DrawFit(Rect r, Texture2D tex, float alpha = 1f)
        {
            if (tex == null) return;
            var old = GUI.color;
            GUI.color = new Color(old.r, old.g, old.b, old.a * alpha);
            GUI.DrawTexture(r, tex, ScaleMode.ScaleToFit);
            GUI.color = old;
        }
    }
}
