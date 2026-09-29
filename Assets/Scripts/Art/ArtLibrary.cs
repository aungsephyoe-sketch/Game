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
        public static Texture Character(CharacterDefinition def) { return PortraitStudio.Hero(def); }

        public static Texture CharacterFull(CharacterDefinition def) { return PortraitStudio.Hero(def, true); }
        /// <summary>Full body in an action pose, for banners.</summary>
        public static Texture CharacterAction(CharacterDefinition def) { return PortraitStudio.HeroAction(def); }

        /// <summary>The slayer's card poster (head and upper body, expression, pose and environment light).</summary>
        public static Texture CharacterPoster(CharacterDefinition def) { return PortraitStudio.HeroPoster(def); }

        public static Texture Monster(EnemyDefinition def) { return PortraitStudio.Enemy(def); }

        /// <summary>Region key art belonged to the earlier painted art direction; scenes are shown in 3D instead.</summary>
        public static Texture2D Region(string regionId) { return null; }

        /// <summary>Hand-painted tileable surface textures (grass, dirt, stone, snow, demon, wall, roof, bark, ui_panel).</summary>
        public static Texture2D Surface(string key)
        {
            // Flat low-poly colours are the art direction; painted textures are no longer used.
            if (key != null) return null;
            var t = Load("Art/Textures/" + key);
            if (t != null && t.wrapMode != TextureWrapMode.Repeat && key != "ui_panel") t.wrapMode = TextureWrapMode.Repeat;
            return t;
        }

        /// <summary>Ground texture for a region.</summary>
        public static string GroundKey(ArenaTheme t)
        {
            switch (t.kind)
            {
                case EnvironmentKind.Mountain: return "snow";
                case EnvironmentKind.DemonLand: case EnvironmentKind.FallenCity: return "demon";
                case EnvironmentKind.Castle: return "stone";
                default: return "grass";
            }
        }

        /// <summary>Road / clearing texture for a region.</summary>
        public static string RoadKey(ArenaTheme t)
        {
            switch (t.kind)
            {
                case EnvironmentKind.Kingdom: case EnvironmentKind.FallenCity: case EnvironmentKind.Temple: case EnvironmentKind.Castle: return "stone";
                case EnvironmentKind.DemonLand: return "demon";
                case EnvironmentKind.Mountain: return "snow";
                default: return "dirt";
            }
        }

        /// <summary>Region id whose painted art matches an environment kind (for backdrops).</summary>
        public static string RegionFor(EnvironmentKind k)
        {
            switch (k)
            {
                case EnvironmentKind.Forest: return "forest";
                case EnvironmentKind.Mountain: return "mountain";
                case EnvironmentKind.Kingdom: case EnvironmentKind.FallenCity: return "kingdom";
                case EnvironmentKind.Temple: return "temple";
                case EnvironmentKind.DemonLand: return "demonland";
                case EnvironmentKind.Castle: return "castle";
                default: return "village";
            }
        }

        public static Texture2D WorldMap() { return null; }

        /// <summary>Chapter key art: a cinematic keyframe for the big story moments, else the chapter's region.</summary>
        public static Texture2D Chapter(ChapterDefinition ch) { return null; }

        /// <summary>Draws a texture cropped to fill the rect (like CSS object-fit: cover).</summary>
        public static void DrawCover(Rect r, Texture tex, float alpha = 1f)
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
        public static void DrawFit(Rect r, Texture tex, float alpha = 1f)
        {
            if (tex == null) return;
            var old = GUI.color;
            GUI.color = new Color(old.r, old.g, old.b, old.a * alpha);
            GUI.DrawTexture(r, tex, ScaleMode.ScaleToFit);
            GUI.color = old;
        }
    }
}
