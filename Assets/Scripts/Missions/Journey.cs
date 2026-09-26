using System.Collections.Generic;
using UnityEngine;

namespace HashiraChronicles
{
    public enum StageKind { Travel, Fight, Investigate, Seal, BossApproach, Boss, Finish }

    /// <summary>A named location along a mission route: a clearing the player reaches and (usually) fights in.</summary>
    public class JourneyPlace
    {
        public string name;
        public Vector3 pos;
        public float radius;
        public bool isBossArena;
        public bool isBridge;
    }

    public class JourneyStage
    {
        public StageKind kind;
        public int place;
        public int wave = -1;
        public string objective;
    }

    /// <summary>
    /// Every mission is a journey: a winding road through the region from an entrance, past named places where
    /// the fighting happens, to the destination (usually a boss arena). This class generates the route and answers
    /// geometry questions (where is the road, is a point inside the playable area).
    /// </summary>
    public class Journey
    {
        public readonly List<JourneyPlace> places = new List<JourneyPlace>();
        public readonly List<JourneyStage> stages = new List<JourneyStage>();
        /// <summary>Road centre line, sampled roughly every 2.5 m.</summary>
        public readonly List<Vector3> path = new List<Vector3>();
        public float halfWidth = 5.5f;
        public float Length { get; private set; }
        public Vector3 Start { get { return path[0]; } }
        public Vector3 EndDirection { get; private set; }

        static readonly Dictionary<EnvironmentKind, string[]> PlaceNames = new Dictionary<EnvironmentKind, string[]>
        {
            { EnvironmentKind.Village, new[] { "Village Gate", "Market Square", "Rice Terraces", "Old Mill Bridge", "Shrine Steps" } },
            { EnvironmentKind.Forest, new[] { "Village Entrance", "Forest Path", "Broken Bridge", "Demon Camp", "Ancient Ruins" } },
            { EnvironmentKind.Mountain, new[] { "Mountain Trail", "Frozen Waterfall", "Rope Bridge", "Ice Cave", "Summit Pass" } },
            { EnvironmentKind.Kingdom, new[] { "Outer Road", "City Gate", "Market Street", "Canal Bridge", "Arena Gate" } },
            { EnvironmentKind.FallenCity, new[] { "Burning Gate", "Main Street", "Collapsed Market", "Guard Barracks", "Palace Stairs" } },
            { EnvironmentKind.Temple, new[] { "Jungle Stairs", "Statue Garden", "Seal Chamber", "Flooded Hall", "Waterfall Bridge" } },
            { EnvironmentKind.DemonLand, new[] { "Ashen Road", "Lava Bridge", "Bone Spires", "Rift Field", "Eclipse Monolith" } },
            { EnvironmentKind.Castle, new[] { "Castle Gate", "Great Stair", "Hall of Statues", "Burning Gallery", "Chain Bridge" } },
        };

        public static string BossArenaName(string bossId, EnvironmentKind kind)
        {
            switch (bossId)
            {
                case "boss_gorvath": return "Shrine Courtyard";
                case "boss_thousandarm": return "Demon Shrine";
                case "boss_hyoga": return "Stormy Summit";
                case "boss_chancellor": return "Palace Courtyard";
                case "boss_goken": return "Crimson Plateau";
                case "boss_seal_guardian": return "Guardian's Court";
                case "boss_morgrath": return "Throne Hall of Solmere";
                case "boss_nyx": case "boss_vex": return "The Final Stair";
                case "boss_veyrath": return "Throne of the Eclipse";
            }
            return kind == EnvironmentKind.Kingdom ? "The Arena" : "Battlefield";
        }

        public static Journey Build(MissionDefinition m)
        {
            var j = new Journey();
            var rng = new System.Random(m.id.GetHashCode());
            var names = PlaceNames.ContainsKey(m.theme.kind) ? PlaceNames[m.theme.kind] : PlaceNames[EnvironmentKind.Village];
            bool hasBoss = !string.IsNullOrEmpty(m.bossId);

            // 1. Places: an entrance, one fighting place per wave, an optional investigation, the seal chamber, the boss arena.
            var plan = new List<string>();
            var kinds = new List<StageKind>();
            plan.Add(m.route.Count > 0 ? m.route[0] : names[0]); kinds.Add(StageKind.Travel);
            int nameIdx = 1;
            for (int w = 0; w < m.waves.Count; w++)
            {
                if (m.sealPuzzle && w == 0) { plan.Add(NameAt(m, names, nameIdx++, "Seal Chamber")); kinds.Add(StageKind.Seal); }
                if (!string.IsNullOrEmpty(m.investigate) && w == m.waves.Count - 1)
                {
                    plan.Add(char.ToUpper(m.investigate[0]) + m.investigate.Substring(1)); kinds.Add(StageKind.Investigate);
                }
                plan.Add(NameAt(m, names, nameIdx++, names[Mathf.Min(nameIdx - 1, names.Length - 1)])); kinds.Add(StageKind.Fight);
            }
            if (hasBoss) { plan.Add(BossArenaName(m.bossId, m.theme.kind)); kinds.Add(StageKind.Boss); }

            // 2. Lay them out along a gently wandering road that always heads "up" the screen (+Z).
            Vector3 pos = Vector3.zero;
            float heading = 0f;
            for (int i = 0; i < plan.Count; i++)
            {
                bool boss = kinds[i] == StageKind.Boss;
                var place = new JourneyPlace
                {
                    name = plan[i], pos = pos,
                    radius = i == 0 ? 9f : boss ? 18f : kinds[i] == StageKind.Seal ? 12f : 13f,
                    isBossArena = boss,
                    isBridge = plan[i].Contains("Bridge")
                };
                j.places.Add(place);
                if (i < plan.Count - 1)
                {
                    heading = Mathf.Clamp(heading + (float)(rng.NextDouble() * 50.0 - 25.0), -32f, 32f);
                    float dist = 36f + (float)rng.NextDouble() * 8f + (kinds[i + 1] == StageKind.Boss ? 14f : 0f);
                    pos += Quaternion.Euler(0f, heading, 0f) * Vector3.forward * dist;
                }
            }

            // 3. Road centre line: gentle S-curves between places.
            for (int i = 0; i < j.places.Count - 1; i++)
            {
                Vector3 a = j.places[i].pos, b = j.places[i + 1].pos;
                Vector3 side = Vector3.Cross(Vector3.up, (b - a).normalized) * ((i % 2 == 0 ? 1f : -1f) * (4f + (float)rng.NextDouble() * 4f));
                Vector3 c1 = Vector3.Lerp(a, b, 0.33f) + side, c2 = Vector3.Lerp(a, b, 0.66f) - side * 0.6f;
                int n = Mathf.Max(8, Mathf.RoundToInt(Vector3.Distance(a, b) / 2.5f));
                for (int k = (i == 0 ? 0 : 1); k <= n; k++)
                {
                    float t = (float)k / n, u = 1f - t;
                    j.path.Add(u * u * u * a + 3f * u * u * t * c1 + 3f * u * t * t * c2 + t * t * t * b);
                }
            }
            if (j.path.Count == 0) j.path.Add(Vector3.zero);
            for (int i = 1; i < j.path.Count; i++) j.Length += Vector3.Distance(j.path[i - 1], j.path[i]);
            j.EndDirection = j.path.Count > 1 ? (j.path[j.path.Count - 1] - j.path[j.path.Count - 2]).normalized : Vector3.forward;

            // 4. Stages.
            int wave = 0;
            for (int i = 1; i < j.places.Count; i++)
            {
                var k = kinds[i];
                var p = j.places[i];
                j.stages.Add(new JourneyStage
                {
                    kind = k == StageKind.Boss ? StageKind.BossApproach : StageKind.Travel, place = i,
                    objective = ReachText(p.name)
                });
                switch (k)
                {
                    case StageKind.Fight:
                        j.stages.Add(new JourneyStage { kind = StageKind.Fight, place = i, wave = wave++, objective = "Defeat the demons" });
                        break;
                    case StageKind.Investigate:
                        j.stages.Add(new JourneyStage { kind = StageKind.Investigate, place = i, objective = "Investigate " + m.investigate });
                        break;
                    case StageKind.Seal:
                        j.stages.Add(new JourneyStage { kind = StageKind.Seal, place = i, objective = "Break the ancient seals" });
                        break;
                    case StageKind.Boss:
                        var bd = GameDatabase.GetEnemy(m.bossId);
                        j.stages.Add(new JourneyStage { kind = StageKind.Boss, place = i, objective = "Defeat " + (bd != null ? bd.displayName : "the boss") });
                        break;
                }
            }
            j.stages.Add(new JourneyStage { kind = StageKind.Finish, place = j.places.Count - 1, objective = "Mission complete" });
            return j;
        }

        public static string ReachText(string place)
        {
            return place.StartsWith("The ") ? "Reach " + place : "Reach the " + place;
        }

        static string NameAt(MissionDefinition m, string[] names, int idx, string fallback)
        {
            if (idx < m.route.Count) return m.route[idx];
            return idx < names.Length ? names[idx] : fallback;
        }

        /// <summary>Route names for mission pages ("Village Entrance → Forest Path → … → Demon Shrine").</summary>
        public string RouteText()
        {
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < places.Count; i++)
            {
                if (i > 0) sb.Append("  →  ");
                sb.Append(places[i].name);
            }
            return sb.ToString();
        }

        // ------------------------------------------------------------------ Geometry

        public Vector3 NearestOnPath(Vector3 p, out int segment)
        {
            segment = 0;
            float best = float.MaxValue;
            Vector3 bestP = path[0];
            for (int i = 0; i < path.Count - 1; i++)
            {
                Vector3 q = ClosestOnSegment(path[i], path[i + 1], p);
                float d = (Flat(q) - Flat(p)).sqrMagnitude;
                if (d < best) { best = d; bestP = q; segment = i; }
            }
            return bestP;
        }

        public float DistanceToPath(Vector3 p)
        {
            int s;
            return Vector3.Distance(Flat(NearestOnPath(p, out s)), Flat(p));
        }

        /// <summary>How far along the road (in metres) the point is.</summary>
        public float Progress(Vector3 p)
        {
            int seg;
            Vector3 q = NearestOnPath(p, out seg);
            float d = 0f;
            for (int i = 0; i < seg; i++) d += Vector3.Distance(path[i], path[i + 1]);
            return d + Vector3.Distance(path[seg], q);
        }

        /// <summary>The point <paramref name="metres"/> along the road.</summary>
        public Vector3 PointAt(float metres)
        {
            if (metres <= 0f) return path[0];
            for (int i = 0; i < path.Count - 1; i++)
            {
                float l = Vector3.Distance(path[i], path[i + 1]);
                if (metres <= l) return Vector3.Lerp(path[i], path[i + 1], metres / Mathf.Max(0.001f, l));
                metres -= l;
            }
            return path[path.Count - 1];
        }

        /// <summary>Keeps a point on the road or inside one of the clearings.</summary>
        public Vector3 Clamp(Vector3 p)
        {
            foreach (var pl in places)
                if ((Flat(p) - Flat(pl.pos)).sqrMagnitude <= pl.radius * pl.radius) return p;
            int s;
            Vector3 q = NearestOnPath(p, out s);
            Vector3 off = Flat(p) - Flat(q);
            if (off.magnitude <= halfWidth) return p;
            Vector3 best = q + off.normalized * halfWidth;
            best.y = p.y;
            float bestD = (Flat(best) - Flat(p)).sqrMagnitude;
            foreach (var pl in places)
            {
                Vector3 o = Flat(p) - Flat(pl.pos);
                Vector3 c = pl.pos + o.normalized * pl.radius;
                c.y = p.y;
                float d = (Flat(c) - Flat(p)).sqrMagnitude;
                if (d < bestD) { bestD = d; best = c; }
            }
            return best;
        }

        public static Vector3 Flat(Vector3 v) { return new Vector3(v.x, 0f, v.z); }

        static Vector3 ClosestOnSegment(Vector3 a, Vector3 b, Vector3 p)
        {
            Vector3 ab = b - a;
            float t = Mathf.Clamp01(Vector3.Dot(p - a, ab) / Mathf.Max(0.0001f, ab.sqrMagnitude));
            return a + ab * t;
        }
    }
}
