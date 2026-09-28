using System.Collections.Generic;
using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// The height of the walkable surface anywhere in the current world, matching what is drawn.
    ///   • Terrain: the world builder hands over the exact height grid it meshed, and heights are read with the same
    ///     triangle split the mesh uses, so the answer is the rendered surface itself — not the smooth formula the
    ///     grid was sampled from (the two differ by up to a metre on banks, which is what buried feet in the snow).
    ///   • Platforms: bridge decks and the like register their top surface (with an optional arch or sag), and win
    ///     over the terrain underneath them.
    ///   • No world registered (the classic flat arenas): the ground is y = 0.
    /// Gameplay works on a flat plane (y = height above the ground); <see cref="GroundFollower"/> turns that into
    /// world height every frame, so every mover — walking, dodges, lunges, knockback, leaps, specials — lands on
    /// the real surface without each one needing to know about terrain.
    /// </summary>
    public static class Ground
    {
        struct Platform
        {
            public Vector2 c;
            public float cos, sin, halfW, halfL, top, curve;
        }

        static float[] hts;
        static int nx, nz;
        static Vector2 origin;
        static float step;
        static readonly List<Platform> platforms = new List<Platform>();

        public static bool HasTerrain { get { return hts != null; } }

        public static void Clear()
        {
            hts = null;
            platforms.Clear();
        }

        /// <summary>The terrain grid exactly as meshed: heights[iz * countX + ix] at origin + (ix, iz) * cell.</summary>
        public static void SetTerrain(Vector3 worldOrigin, float cell, int countX, int countZ, float[] heights, float baseY)
        {
            origin = new Vector2(worldOrigin.x, worldOrigin.z);
            step = cell;
            nx = countX;
            nz = countZ;
            hts = new float[heights.Length];
            for (int i = 0; i < heights.Length; i++) hts[i] = heights[i] + baseY;
        }

        /// <summary>
        /// A walkable deck: centre (its y is the top surface at the ends), yaw, half width across and half length
        /// along. curve lifts (+) or sags (−) the middle by that much, following a sine along its length.
        /// </summary>
        public static void AddPlatform(Vector3 center, float yaw, float halfWidth, float halfLength, float curve)
        {
            float a = yaw * Mathf.Deg2Rad;
            platforms.Add(new Platform { c = new Vector2(center.x, center.z), cos = Mathf.Cos(a), sin = Mathf.Sin(a), halfW = halfWidth, halfL = halfLength, top = center.y, curve = curve });
        }

        /// <summary>Height of the walkable surface at (x, z).</summary>
        public static float HeightAt(float x, float z)
        {
            for (int i = 0; i < platforms.Count; i++)
            {
                var p = platforms[i];
                float dx = x - p.c.x, dz = z - p.c.y;
                // Into the platform's frame (x across, z along).
                float lx = dx * p.cos - dz * p.sin, lz = dx * p.sin + dz * p.cos;
                if (Mathf.Abs(lx) > p.halfW || Mathf.Abs(lz) > p.halfL) continue;
                float t = (lz / p.halfL) * 0.5f + 0.5f;
                return p.top + p.curve * Mathf.Sin(t * Mathf.PI);
            }
            return TerrainAt(x, z);
        }

        public static float HeightAt(Vector3 p) { return HeightAt(p.x, p.z); }

        /// <summary>The meshed terrain height (same triangles as the mesh: a,c,b and b,c,d per cell).</summary>
        public static float TerrainAt(float x, float z)
        {
            if (hts == null) return 0f;
            float gx = (x - origin.x) / step, gz = (z - origin.y) / step;
            int ix = Mathf.Clamp(Mathf.FloorToInt(gx), 0, nx - 2), iz = Mathf.Clamp(Mathf.FloorToInt(gz), 0, nz - 2);
            float fx = Mathf.Clamp01(gx - ix), fz = Mathf.Clamp01(gz - iz);
            float ha = hts[iz * nx + ix], hb = hts[iz * nx + ix + 1], hc = hts[(iz + 1) * nx + ix], hd = hts[(iz + 1) * nx + ix + 1];
            if (fx + fz <= 1f) return ha + (hb - ha) * fx + (hc - ha) * fz;
            return hd + (hc - hd) * (1f - fx) + (hb - hd) * (1f - fz);
        }
    }
}
