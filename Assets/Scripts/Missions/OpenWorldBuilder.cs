using System.Collections.Generic;
using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// The open world: Kiriha Village by night, built on the same premium standard as the mission worlds
    /// (<see cref="PrototypeWorld.BuildVillage"/>): the gate, the lantern-lit market street, the plaza under the
    /// great cherry tree with the co-op gates and the quest board, the mill by the river bridge and the shrine.
    /// Villagers go about their evening, a few treasure chests are hidden around, and the plaza is the players'
    /// hub (<see cref="VillageHub"/>). Houses, trees and big props are solid (<see cref="Obstacles"/>).
    /// </summary>
    public static class OpenWorldBuilder
    {
        /// <summary>The village route (streets and clearings you can walk in).</summary>
        public static Journey Village { get; private set; }

        /// <summary>Where the team arrives: the top of the market street, looking up at the plaza.</summary>
        public static readonly Vector3 Spawn = new Vector3(0f, 0f, -3f);

        static System.Random rng;
        static float R(float a, float b) { return a + (float)rng.NextDouble() * (b - a); }

        static Material wood, gold;

        public static ArenaTheme Theme()
        {
            return new ArenaTheme
            {
                kind = EnvironmentKind.Village, night = true, burning = false, petals = true,
                ground = new Color(0.22f, 0.4f, 0.36f), groundAccent = new Color(0.6f, 0.58f, 0.64f),
                sky = new Color(0.1f, 0.12f, 0.26f), fog = new Color(0.28f, 0.24f, 0.42f), fogStart = 34f, fogEnd = 250f,
                lantern = new Color(1f, 0.66f, 0.32f), foliage = new Color(0.96f, 0.62f, 0.78f),
                sun = new Color(0.74f, 0.8f, 1f), sunIntensity = 0.95f
            };
        }

        public static JourneyBuilder.Result Build(Transform parent)
        {
            rng = new System.Random(20260928);
            wood = MaterialFactory.Toon(new Color(0.55f, 0.38f, 0.22f));
            gold = MaterialFactory.Toon(new Color(1f, 0.8f, 0.25f), 0.01f, new Color(0.4f, 0.28f, 0f));
            Village = Journey.Village();
            var res = PrototypeWorld.BuildVillage(Village, parent, true);
            var dynGo = new GameObject("OpenWorldDynamic");
            dynGo.transform.SetParent(parent, false);
            Chests(dynGo.transform);
            Villagers(dynGo.transform);
            return res;
        }

        /// <summary>Keeps a point on the streets and in the clearings of the village.</summary>
        public static Vector3 Clamp(Vector3 p)
        {
            if (Village == null) return p;
            p = Village.Clamp(p);
            p.y = 0f;
            return p;
        }

        public static readonly List<Transform> ChestSpots = new List<Transform>();

        static void Chests(Transform dyn)
        {
            ChestSpots.Clear();
            foreach (var s in PrototypeWorld.VillageChestSpots)
            {
                Vector3 p = s;
                var c = new GameObject("Chest").transform;
                c.SetParent(dyn, false);
                c.position = p;
                c.rotation = Quaternion.Euler(0f, R(0f, 360f), 0f);
                MeshFactory.Primitive(PrimitiveType.Cube, c, new Vector3(0f, 0.35f, 0f), new Vector3(1f, 0.7f, 0.7f), wood);
                var lid = MeshFactory.Primitive(PrimitiveType.Cube, c, new Vector3(0f, 0.78f, 0f), new Vector3(1.05f, 0.18f, 0.75f), gold);
                lid.name = "Lid";
                MeshFactory.Primitive(PrimitiveType.Cube, c, new Vector3(0f, 0.5f, 0.36f), new Vector3(0.2f, 0.25f, 0.05f), gold);
                MeshFactory.MeshObject(MeshFactory.Ring(0.8f), c, Vector3.up * 0.05f, Vector3.one * 1.3f, MaterialFactory.Additive(new Color(1f, 0.85f, 0.3f, 0.7f)), false).AddComponent<Pulse>().Speed = 3f;
                ChestSpots.Add(c);
            }
        }

        static readonly string[][] Talk =
        {
            new[] { "Lovely night, isn't it? The blossoms glow under the lanterns.", "The cherry tree in the plaza is older than the village.", "My grandson wants to be a slayer. I told him to eat his vegetables first." },
            new[] { "Fresh dumplings! Two for one, tonight only!", "Have you tried the river fish? Best in the valley.", "Business is good now that the demons are gone." },
            new[] { "The shrine at the top of the village is very old.", "I heard a treasure chest is hidden near the mill...", "Thank you for saving us, slayers!" },
            new[] { "I'm on night patrol. Very serious patrol.", "Slayers gather at the gates in the plaza. They say the demons beyond are fierce.", "Stay sharp — the forest is never fully safe." },
            new[] { "Keep your stance low and your heart steady.", "Go through a gate with two friends. Alone, you won't last.", "You've grown strong." },
            new[] { "Want to trade? I have ribbons, bells and very shiny rocks.", "Diamonds? You want the summoning shrine for those.", "Buy three, get... well, three." },
        };

        static void Villagers(Transform dyn)
        {
            string[] ids = { "npc_villager", "npc_merchant", "npc_villager2", "npc_soldier", "npc_tessai", "npc_merchant" };
            var places = Village.places;
            int n = 0;
            for (int h = 0; h < places.Count; h++)
            {
                var pl = places[h];
                bool plaza = pl.name.Contains("Plaza");
                int count = plaza ? 5 : pl.name.Contains("Market") ? 4 : 2;
                for (int i = 0; i < count; i++, n++)
                {
                    int kind = pl.name.Contains("Market") ? (i % 2 == 0 ? 1 : 5) : n % ids.Length;
                    if (kind == 4 && !plaza) kind = 0; // Master Tessai stays in the plaza
                    // Walk a ring inside the clearing (the plaza's inner ring is the cherry tree's planter).
                    float inner = plaza ? 4f : 1.5f, outer = Mathf.Max(inner + 2f, pl.radius - 2f);
                    var w = NpcWalker.Spawn(dyn, ids[kind], Journey.Flat(pl.pos), inner, outer, Talk[kind]);
                    if (w != null) w.Speed = R(1.1f, 1.6f);
                }
            }
        }
    }
}
