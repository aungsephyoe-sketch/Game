using System.Collections.Generic;
using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// A small yard for checking collision by hand: a tree, a pole, a rock, a wall, a fence, a gap narrower than a
    /// slayer and a gap wide enough to walk through. The props are built exactly like the ones in the real maps and
    /// registered through the same <see cref="Obstacles.Scan"/>, so what passes here passes everywhere. A checklist
    /// in the corner watches the slayer's capsule against the generated colliders and ticks each test off.
    /// </summary>
    public class CollisionTestArena : MonoBehaviour
    {
        class Test
        {
            public string name;
            public Vector3 at;           // centre of the prop
            public Collider col;         // its generated collider
            public bool touched, breached, sideA, sideB;
        }

        readonly List<Test> tests = new List<Test>();
        // The narrow gap: its centre and half width, and whether the slayer ever got to it / through it.
        Vector3 gapAt, wideAt;
        float gapHalf, wideHalf;
        bool gapTried, gapCrossed, wideCrossed;
        float lastZ = float.NaN;
        GUIStyle style;

        public static GameObject Build(ArenaTheme theme, Transform parent)
        {
            var root = new GameObject("CollisionTest");
            root.transform.SetParent(parent, false);
            var t = root.transform;
            var ground = MaterialFactory.Toon(new Color(0.46f, 0.62f, 0.36f), 0f);
            var line = MaterialFactory.Toon(new Color(0.9f, 0.88f, 0.8f), 0f);
            var bark = MaterialFactory.Toon(new Color(0.45f, 0.32f, 0.22f));
            var leaf = MaterialFactory.Toon(new Color(0.3f, 0.55f, 0.28f));
            var stone = MaterialFactory.Toon(new Color(0.55f, 0.55f, 0.58f));
            var plaster = MaterialFactory.Toon(new Color(0.9f, 0.86f, 0.76f));
            var wood = MaterialFactory.Toon(new Color(0.5f, 0.34f, 0.2f));
            var red = MaterialFactory.Toon(new Color(0.8f, 0.2f, 0.16f));

            var disc = MeshFactory.MeshObject(MeshFactory.PlanarDisc(), t, Vector3.zero, new Vector3(BattleController.ArenaRadius + 4f, 1f, BattleController.ArenaRadius + 4f), ground, false);
            disc.name = "Ground";
            disc.GetComponent<Renderer>().receiveShadows = true;
            // A painted start line and lane marks so the tests are easy to find.
            Flat(t, new Vector3(0f, 0.01f, -6f), new Vector3(6f, 1f, 0.12f), line);
            Flat(t, new Vector3(0f, 0.01f, 5.6f), new Vector3(22f, 1f, 0.08f), line);

            var arena = root.AddComponent<CollisionTestArena>();

            // 1 — tree: trunk plus a canopy that overhangs (only the trunk is solid).
            var tree = new GameObject("Tree").transform;
            tree.SetParent(t, false);
            tree.localPosition = new Vector3(-9f, 0f, 2f);
            MeshFactory.Primitive(PrimitiveType.Cylinder, tree, Vector3.up * 1.6f, new Vector3(0.6f, 1.6f, 0.6f), bark);
            MeshFactory.Primitive(PrimitiveType.Sphere, tree, new Vector3(0f, 3.8f, 0f), Vector3.one * 3.2f, leaf);
            MeshFactory.Primitive(PrimitiveType.Sphere, tree, new Vector3(0.7f, 3.3f, 0.4f), Vector3.one * 2.2f, leaf);
            arena.Add("Tree", tree.position + Vector3.up);

            // 2 — pole: thin, the classic thing a sweep can skip past.
            var pole = MeshFactory.Primitive(PrimitiveType.Cylinder, t, new Vector3(-4f, 1.6f, 2f), new Vector3(0.22f, 1.6f, 0.22f), red);
            pole.name = "Pole";
            MeshFactory.Primitive(PrimitiveType.Cube, t, new Vector3(-4f, 3.25f, 2f), new Vector3(0.8f, 0.14f, 0.2f), red).name = "PoleCap";
            arena.Add("Pole", new Vector3(-4f, 1f, 2f));

            // 3 — rock: a squashed, turned boulder with a smaller one leaning on it.
            var rock = MeshFactory.Primitive(PrimitiveType.Sphere, t, new Vector3(1.5f, 0.5f, 2f), new Vector3(2.2f, 1.3f, 1.5f), stone);
            rock.name = "Rock";
            rock.transform.localRotation = Quaternion.Euler(4f, 28f, -6f);
            arena.Add("Rock", new Vector3(1.5f, 0.6f, 2f));

            // 4 — wall: a plastered wall at an angle.
            var wall = MeshFactory.Primitive(PrimitiveType.Cube, t, new Vector3(7.5f, 1.1f, 2f), new Vector3(4.5f, 2.2f, 0.4f), plaster);
            wall.name = "Wall";
            wall.transform.localRotation = Quaternion.Euler(0f, -20f, 0f);
            MeshFactory.Primitive(PrimitiveType.Cube, wall.transform, new Vector3(0f, 0.55f, 0f), new Vector3(1.04f, 0.08f, 1.5f), wood).name = "WallCap";
            arena.Add("Wall", new Vector3(7.5f, 1f, 2f));

            // 5 — two gaps between three blocks: 0.6 m (too narrow) and 1.6 m (walkable).
            float narrow = 0.6f, wide = 1.6f;
            Block(t, -7.3f, -3f - narrow * 0.5f, 9f, stone);
            Block(t, -3f + narrow * 0.5f, 3.5f - wide * 0.5f, 9f, stone);
            Block(t, 3.5f + wide * 0.5f, 7.5f, 9f, stone);
            arena.gapAt = new Vector3(-3f, 0f, 9f);
            arena.gapHalf = narrow * 0.5f;
            arena.wideAt = new Vector3(3.5f, 0f, 9f);
            arena.wideHalf = wide * 0.5f;

            // 6 — a post-and-rail fence behind the start line.
            for (int i = 0; i <= 5; i++)
            {
                float x = -4f + i * 1.6f;
                MeshFactory.Primitive(PrimitiveType.Cube, t, new Vector3(x, 0.55f, -11f), new Vector3(0.16f, 1.1f, 0.16f), wood).name = "FencePost";
                if (i < 5)
                {
                    MeshFactory.Primitive(PrimitiveType.Cube, t, new Vector3(x + 0.8f, 0.85f, -11f), new Vector3(1.6f, 0.1f, 0.07f), wood).name = "FenceRail";
                    MeshFactory.Primitive(PrimitiveType.Cube, t, new Vector3(x + 0.8f, 0.45f, -11f), new Vector3(1.6f, 0.1f, 0.07f), wood).name = "FenceRail";
                }
            }
            arena.Add("Fence", new Vector3(0f, 0.85f, -11f));

            ArenaBuilder.ApplyLighting(theme);
            Obstacles.Scan(t);
            Physics.SyncTransforms();
            foreach (var test in arena.tests) test.col = FindCollider(test.at);
            return root;
        }

        void Add(string name, Vector3 at) { tests.Add(new Test { name = name, at = at }); }

        static void Flat(Transform t, Vector3 p, Vector3 s, Material m)
        {
            var go = MeshFactory.Primitive(PrimitiveType.Cube, t, p, new Vector3(s.x, 0.02f, s.z), m);
            go.name = "Ground";
        }

        static void Block(Transform t, float x0, float x1, float z, Material m)
        {
            MeshFactory.Primitive(PrimitiveType.Cube, t, new Vector3((x0 + x1) * 0.5f, 0.9f, z), new Vector3(x1 - x0, 1.8f, 1f), m).name = "GapBlock";
        }

        static Collider FindCollider(Vector3 at)
        {
            var hits = Physics.OverlapSphere(at, 0.08f, ~(1 << Obstacles.CharacterLayer), QueryTriggerInteraction.Ignore);
            return hits.Length > 0 ? hits[0] : null;
        }

        PlayerCharacter Player
        {
            get
            {
                var b = BattleController.Current;
                return b != null && b.Team != null ? b.Team.Active : null;
            }
        }

        void Update()
        {
            var pc = Player;
            if (pc == null) return;
            Vector3 p = pc.transform.position;
            float r = PlayerCharacter.BodyRadius;
            Vector3 a = new Vector3(p.x, r + 0.05f, p.z), b = new Vector3(p.x, PlayerCharacter.BodyHeight - r, p.z);
            int mask = ~(1 << Obstacles.CharacterLayer);
            var near = Physics.OverlapCapsule(a, b, r + 0.1f, mask, QueryTriggerInteraction.Ignore);
            var inside = Physics.OverlapCapsule(a, b, r - 0.08f, mask, QueryTriggerInteraction.Ignore);
            foreach (var test in tests)
            {
                if (test.col == null) continue;
                if (System.Array.IndexOf(near, test.col) >= 0) test.touched = true;
                if (System.Array.IndexOf(inside, test.col) >= 0) test.breached = true;
                // Walking around: seen on both sides of it (front and back), close enough to count.
                Vector3 d = p - test.at;
                if (Mathf.Abs(d.x) < 3.5f && Mathf.Abs(d.z) < 3.5f)
                {
                    if (d.z < -0.9f) test.sideA = true;
                    if (d.z > 0.9f) test.sideB = true;
                }
            }
            // Gaps: crossing the block line inside a gap.
            if (Mathf.Abs(p.x - gapAt.x) < 1.2f && Mathf.Abs(p.z - gapAt.z) < 1.4f) gapTried = true;
            if (!float.IsNaN(lastZ) && (lastZ - gapAt.z) * (p.z - gapAt.z) < 0f)
            {
                if (Mathf.Abs(p.x - gapAt.x) < gapHalf + 0.2f) gapCrossed = true;
                if (Mathf.Abs(p.x - wideAt.x) < wideHalf) wideCrossed = true;
            }
            lastZ = p.z;
        }

        void OnGUI()
        {
            if (style == null) style = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(17 * HudLayout.Scale), richText = true };
            float s = HudLayout.Scale;
            var box = new Rect(16f * s, Screen.height * 0.22f, 430f * s, (tests.Count + 4) * 26f * s + 20f * s);
            GUI.color = new Color(0f, 0f, 0f, 0.6f);
            GUI.DrawTexture(box, Texture2D.whiteTexture);
            GUI.color = Color.white;
            float y = box.y + 8f * s;
            Line(box, ref y, "<b>COLLISION TEST</b>  (walk into, then around, each prop)");
            foreach (var t in tests)
            {
                string state = t.col == null ? "<color=#ff6060>NO COLLIDER</color>"
                    : t.breached ? "<color=#ff6060>FAIL — walked into it</color>"
                    : t.touched ? "<color=#7dff7d>blocked ✓</color>" : "<color=#bbbbbb>walk into it</color>";
                string around = t.sideA && t.sideB ? "  <color=#7dff7d>around ✓</color>" : "";
                Line(box, ref y, t.name + ": " + state + around);
            }
            Line(box, ref y, "Narrow gap (0.6 m): " + (gapCrossed ? "<color=#ff6060>FAIL — squeezed through</color>" : gapTried ? "<color=#7dff7d>blocked ✓</color>" : "<color=#bbbbbb>try to pass</color>"));
            Line(box, ref y, "Wide gap (1.6 m): " + (wideCrossed ? "<color=#7dff7d>walked through ✓</color>" : "<color=#bbbbbb>walk through it</color>"));
        }

        void Line(Rect box, ref float y, string text)
        {
            float s = HudLayout.Scale;
            GUI.Label(new Rect(box.x + 12f * s, y, box.width - 20f * s, 26f * s), text, style);
            y += 26f * s;
        }
    }
}
