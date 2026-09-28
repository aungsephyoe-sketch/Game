using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// Steering for AI characters (party slayers and demons), built on the game's own navigation data — the journey
    /// road, the arena bounds (<see cref="BattleController.ClampToArena"/>) and the solid scenery
    /// (<see cref="Obstacles"/>) — so it knows exactly what is walkable. Each call returns a direction to move:
    ///   • Route: if the straight line to the goal is blocked (a wall, the river beside a bridge, the edge of the
    ///     road), it follows the road toward the goal instead (over the bridge, around the bend) until the goal is in
    ///     clear sight again. Re-evaluated as the goal moves.
    ///   • Feelers: a short probe ahead; if it runs into something, it tries angles either side (keeping to one
    ///     side so it slides around corners instead of dithering) and takes the first open one.
    ///   • Stuck recovery: if it has meant to move but barely moved for ~1.2 s, it samples escape points around
    ///     itself, picks the reachable one that best closes on the goal, and takes that detour before retrying.
    /// Plain C# (no MonoBehaviour), so any AI can own one.
    /// </summary>
    public class NavAgent
    {
        public const float ProbeLength = 1.3f;

        Vector3 lastCheck;
        float checkTimer, feelTimer, detourUntil;
        Vector3? detour;
        Vector3 cachedDir, routeGoal, routeWay;
        float routeTimer;
        // Progress watch: catches circling (moving, but never getting closer).
        float bestToGoal = float.MaxValue, bestAt;
        Vector3 watchGoal;
        int side = 1;
        public int StuckCount { get; private set; }
        public bool Detouring { get { return detour.HasValue && Time.time < detourUntil; } }
        /// <summary>Where it is actually heading right now (the goal, a road waypoint or a detour point).</summary>
        public Vector3 Waypoint { get; private set; }

        public NavAgent() { side = Random.value < 0.5f ? -1 : 1; }

        static Vector3 Flat(Vector3 v) { return new Vector3(v.x, 0f, v.z); }

        /// <summary>Can a body walk straight from a to b? (No solid in the way, stays on walkable ground.)</summary>
        public static bool ClearLine(Vector3 a, Vector3 b)
        {
            a = Flat(a); b = Flat(b);
            float len = (b - a).magnitude;
            if (len < 0.3f) return true;
            Vector3 end = Obstacles.Sweep(a, b);
            if ((Flat(end) - b).magnitude > 0.6f) return false;
            // Every couple of metres must be inside the playable area (not the river, not off the road).
            int n = Mathf.Clamp(Mathf.CeilToInt(len / 2f), 1, 20);
            for (int i = 1; i <= n; i++)
            {
                Vector3 p = Vector3.Lerp(a, b, i / (float)n);
                if ((Flat(BattleController.ClampToArena(p)) - p).magnitude > 0.4f) return false;
            }
            return true;
        }

        /// <summary>The next point to walk to on the way to goal: the goal itself when in clear sight, else along the road.</summary>
        public static Vector3 NextWaypoint(Vector3 from, Vector3 goal)
        {
            var b = BattleController.Current;
            if (b == null || b.Journey == null || b.Locked) return goal;
            if (ClearLine(from, goal)) return goal;
            var j = b.Journey;
            float sFrom = j.Progress(from), sTo = j.Progress(goal);
            if (Mathf.Abs(sTo - sFrom) < 3f) return goal;
            float dir = Mathf.Sign(sTo - sFrom);
            // Aim a few metres further along the road (not past the goal's point on it).
            float s = sFrom + dir * 4f;
            if (dir > 0f) s = Mathf.Min(s, sTo); else s = Mathf.Max(s, sTo);
            Vector3 w = j.PointAt(s);
            return new Vector3(w.x, 0f, w.z);
        }

        /// <summary>Direction to move this frame to make progress toward goal (zero when it's there).</summary>
        public Vector3 Steer(Vector3 pos, Vector3 goal, float stopDistance, float dt)
        {
            pos = Flat(pos); goal = Flat(goal);
            float toGoal = (goal - pos).magnitude;
            if (toGoal <= stopDistance) { checkTimer = 0f; lastCheck = pos; return Vector3.zero; }

            // Circling? No real progress toward the same goal for 3 s: settle if close, else detour.
            if ((goal - watchGoal).magnitude > 1.5f) { watchGoal = goal; bestToGoal = toGoal; bestAt = Time.time; }
            else if (toGoal < bestToGoal - 0.5f) { bestToGoal = toGoal; bestAt = Time.time; }
            else if (Time.time - bestAt > 3f)
            {
                bestAt = Time.time;
                bestToGoal = toGoal;
                if (toGoal < stopDistance + 3f) { checkTimer = 0f; lastCheck = pos; return Vector3.zero; }
                StuckCount++;
                PickDetour(pos, goal);
            }

            // Stuck? (meant to move, barely moved)
            checkTimer += dt;
            if (checkTimer >= 1.2f)
            {
                float moved = (pos - lastCheck).magnitude;
                if (moved < 0.45f && toGoal > stopDistance + 0.8f) { StuckCount++; PickDetour(pos, goal); }
                else if (moved > 1.5f) StuckCount = 0;
                checkTimer = 0f;
                lastCheck = pos;
            }

            Vector3 target;
            if (Detouring)
            {
                target = detour.Value;
                if ((target - pos).magnitude < 0.5f) { detour = null; target = NextWaypoint(pos, goal); }
            }
            else
            {
                // Re-route a few times a second, or at once if the goal moved a lot.
                routeTimer -= dt;
                if (routeTimer <= 0f || (goal - routeGoal).magnitude > 2f)
                {
                    routeTimer = 0.25f;
                    routeGoal = goal;
                    routeWay = NextWaypoint(pos, goal);
                }
                target = routeWay;
                if ((target - pos).magnitude < 0.6f) { routeWay = goal; target = goal; }
            }
            Waypoint = target;

            Vector3 want = target - pos;
            want.y = 0f;
            if (want.sqrMagnitude < 0.0001f) return Vector3.zero;
            want.Normalize();

            // Feelers (a few times a second).
            feelTimer -= dt;
            if (feelTimer <= 0f || Vector3.Dot(cachedDir, want) < 0.5f)
            {
                feelTimer = 0.12f;
                cachedDir = Avoid(pos, want);
            }
            return cachedDir;
        }

        Vector3 Avoid(Vector3 pos, Vector3 want)
        {
            if (Open(pos, want)) return want;
            float[] angles = { 30f, 55f, 80f, 110f, 140f };
            foreach (float a in angles)
                for (int k = 0; k < 2; k++)
                {
                    int sgn = k == 0 ? side : -side;
                    Vector3 d = Quaternion.Euler(0f, a * sgn, 0f) * want;
                    if (Open(pos, d))
                    {
                        // Keep sliding around the same side next time (no dithering at corners).
                        side = sgn;
                        return d;
                    }
                }
            return want;
        }

        static bool Open(Vector3 pos, Vector3 dir)
        {
            Vector3 probe = pos + dir * ProbeLength;
            Vector3 got = BattleController.ClampToArena(Obstacles.Sweep(pos, probe));
            return (Flat(got) - pos).magnitude > ProbeLength * 0.6f;
        }

        void PickDetour(Vector3 pos, Vector3 goal)
        {
            Vector3 best = pos;
            float bestScore = float.MaxValue;
            for (int i = 0; i < 12; i++)
            {
                Vector3 d = Quaternion.Euler(0f, i * 30f + Random.Range(-10f, 10f), 0f) * Vector3.forward;
                foreach (float dist in new[] { 2.5f, 5f })
                {
                    Vector3 end = Flat(BattleController.ClampToArena(Obstacles.Sweep(pos, pos + d * dist)));
                    float moved = (end - pos).magnitude;
                    if (moved < 1.2f) continue;
                    // Close on the goal (by the road if needed), prefer bigger free moves, a little randomness.
                    Vector3 via = NextWaypoint(end, goal);
                    float score = (via - end).magnitude + (goal - via).magnitude * 0.5f - moved * 0.4f + Random.Range(0f, 1.5f);
                    if (StuckCount > 2) score = -moved + Random.Range(0f, 3f); // really stuck: just get free
                    if (score < bestScore) { bestScore = score; best = end; }
                }
            }
            if (bestScore < float.MaxValue)
            {
                detour = best;
                detourUntil = Time.time + 1.6f;
                side = -side;
            }
        }
    }
}
