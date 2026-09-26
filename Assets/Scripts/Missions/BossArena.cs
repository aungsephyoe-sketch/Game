using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// Each boss gets a memorable arena at the end of its road: the burning shrine courtyard, the rotten heart of
    /// the forest, a storm-lashed cliff, the palace courtyard, the crimson plateau, the circular temple court,
    /// the ruined throne hall, the final stair, and the Demon Lord's throne under the eclipse.
    /// Also installs the arena's hazard, which wakes when the boss enrages.
    /// </summary>
    public static class BossArena
    {
        static Transform S, D;
        static Vector3 C;
        static float Rad;

        static GameObject Box(Vector3 local, Vector3 scale, Material m, float yaw = 0f)
        {
            var go = MeshFactory.Primitive(PrimitiveType.Cube, S, C + local, scale, m);
            go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            return go;
        }

        static GameObject Pillar(Vector3 local, float h, float w, Material m)
        {
            return MeshFactory.Primitive(PrimitiveType.Cylinder, S, C + local + Vector3.up * h * 0.5f, new Vector3(w, h * 0.5f, w), m);
        }

        static Vector3 Ring(float deg, float r) { return Quaternion.Euler(0f, deg, 0f) * Vector3.forward * r; }

        public static void Dress(Journey j, MissionDefinition m, JourneyPlace pl, ArenaTheme theme, Transform stat, Transform dyn)
        {
            int before = stat.childCount;
            S = stat; D = dyn; C = pl.pos; Rad = pl.radius;
            string hazard = "rocks";
            var dark = MaterialFactory.Toon(new Color(0.1f, 0.08f, 0.1f));
            switch (m.bossId)
            {
                case "boss_gorvath":
                {
                    // Burning shrine courtyard: torii gate, shrine hall, fire.
                    var red = MaterialFactory.Toon(new Color(0.75f, 0.12f, 0.1f));
                    var wood = MaterialFactory.Toon(new Color(0.35f, 0.22f, 0.14f));
                    Pillar(new Vector3(-3f, 0f, Rad + 3f), 6f, 0.5f, red); Pillar(new Vector3(3f, 0f, Rad + 3f), 6f, 0.5f, red);
                    Box(new Vector3(0f, 6.1f, Rad + 3f), new Vector3(9f, 0.5f, 0.6f), red); Box(new Vector3(0f, 5f, Rad + 3f), new Vector3(7f, 0.3f, 0.4f), red);
                    Box(new Vector3(0f, 2f, Rad + 9f), new Vector3(12f, 4f, 6f), wood);
                    for (int i = 0; i < 5; i++) EnvFx.Fire(D, C + Ring(120f + i * 30f, Rad + 2f), 1.3f, i == 2);
                    hazard = "fire";
                    break;
                }
                case "boss_thousandarm":
                {
                    // The rotten heart of the forest: colossal dead trees, glowing roots, a corrupted shrine.
                    var bark = MaterialFactory.Toon(new Color(0.18f, 0.14f, 0.12f));
                    var sap = MaterialFactory.Toon(new Color(0.5f, 1f, 0.35f), 0f, new Color(0.45f, 1f, 0.3f));
                    for (int i = 0; i < 9; i++)
                    {
                        if (i == 4) continue;
                        Vector3 p = Ring(200f + i * 40f, Rad + 3f);
                        Pillar(p, 14f, 2.4f, bark);
                        var br = MeshFactory.Primitive(PrimitiveType.Cylinder, S, C + p + Vector3.up * 11f, new Vector3(0.5f, 4f, 0.5f), bark);
                        br.transform.rotation = Quaternion.Euler(0f, i * 40f, 55f);
                    }
                    for (int i = 0; i < 8; i++)
                    {
                        var root = Box(Ring(i * 45f, Rad * 0.55f) + Vector3.up * 0.05f, new Vector3(0.25f, 0.08f, Rad * 0.9f), sap, i * 45f);
                        root.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    }
                    Box(new Vector3(0f, 1.5f, Rad + 4f), new Vector3(5f, 3f, 3f), dark);
                    EnvFx.Weather(D, C, "motes", Rad * 2f);
                    hazard = "rocks";
                    break;
                }
                case "boss_hyoga":
                {
                    // Storm-lashed summit: a ring of jagged cliffs open to the sky.
                    var rock = MaterialFactory.Toon(new Color(0.55f, 0.58f, 0.65f));
                    var ice = MaterialFactory.Toon(new Color(0.75f, 0.9f, 1f), 0f, new Color(0.3f, 0.45f, 0.6f));
                    for (int i = 0; i < 10; i++)
                    {
                        if (i == 5) continue;
                        MeshFactory.MeshObject(MeshFactory.Cone(), S, C + Ring(i * 36f + 18f, Rad + 4f), new Vector3(7f, Random.Range(9f, 16f), 7f), rock);
                    }
                    for (int i = 0; i < 6; i++)
                        MeshFactory.MeshObject(MeshFactory.Cone(), S, C + Ring(i * 60f, Rad * 0.8f), new Vector3(0.9f, 2.4f, 0.9f), ice);
                    hazard = "lightning";
                    break;
                }
                case "boss_chancellor":
                {
                    // Palace courtyard: white colonnade, the palace facade, royal banners.
                    var white = MaterialFactory.Toon(new Color(0.92f, 0.9f, 0.85f));
                    var blue = MaterialFactory.Toon(new Color(0.2f, 0.3f, 0.6f));
                    for (int i = 0; i < 12; i++) if (i != 6) Pillar(Ring(i * 30f, Rad + 1.5f), 7f, 0.9f, white);
                    Box(new Vector3(0f, 6f, Rad + 8f), new Vector3(26f, 12f, 5f), white);
                    Box(new Vector3(0f, 13f, Rad + 8f), new Vector3(28f, 2f, 7f), blue);
                    for (int i = 0; i < 4; i++) Box(new Vector3(-9f + i * 6f, 7f, Rad + 5.4f), new Vector3(1.6f, 4f, 0.1f), MaterialFactory.Toon(new Color(0.55f, 0.1f, 0.6f)));
                    hazard = "dark";
                    break;
                }
                case "boss_goken":
                {
                    // Crimson plateau: red rock spires and lava cracks.
                    var red = MaterialFactory.Toon(new Color(0.45f, 0.12f, 0.1f));
                    var lava = MaterialFactory.Toon(new Color(1f, 0.4f, 0.05f), 0f, new Color(1f, 0.35f, 0.05f));
                    for (int i = 0; i < 11; i++) if (i != 5) MeshFactory.MeshObject(MeshFactory.Cone(), S, C + Ring(i * 33f, Rad + 3f), new Vector3(3f, Random.Range(8f, 14f), 3f), red);
                    for (int i = 0; i < 6; i++) Box(Ring(i * 60f + 15f, Rad * 0.6f) + Vector3.up * 0.04f, new Vector3(0.3f, 0.05f, 6f), lava, i * 60f + 30f);
                    hazard = "fire";
                    break;
                }
                case "boss_seal_guardian":
                {
                    // Circular temple court: ring of pillars and swordsman statues, great stairs, glowing rune circle.
                    var stone = MaterialFactory.Toon(new Color(0.62f, 0.62f, 0.58f));
                    var rune = MaterialFactory.Additive(new Color(0.35f, 0.9f, 1f, 0.5f));
                    for (int i = 0; i < 12; i++)
                    {
                        if (i == 6) continue;
                        Pillar(Ring(i * 30f, Rad + 1f), 9f, 1.1f, stone);
                        if (i % 3 == 0)
                        {
                            Pillar(Ring(i * 30f + 15f, Rad + 3.5f), 1.2f, 1.6f, stone);
                            var body = MeshFactory.Primitive(PrimitiveType.Capsule, S, C + Ring(i * 30f + 15f, Rad + 3.5f) + Vector3.up * 3f, new Vector3(1.2f, 1.8f, 1f), stone);
                            body.transform.rotation = Quaternion.Euler(0f, i * 30f + 195f, 0f);
                        }
                    }
                    for (int k = 0; k < 5; k++) Box(new Vector3(0f, k * 0.5f + 0.25f, Rad + 3f + k * 1.2f), new Vector3(14f - k, 0.5f, 1.2f), stone);
                    var circle = MeshFactory.MeshObject(MeshFactory.Ring(0.93f), D, C + Vector3.up * 0.04f, new Vector3(Rad * 0.8f, 1f, Rad * 0.8f), rune, false);
                    circle.AddComponent<Spinner>().DegreesPerSecond = new Vector3(0f, 6f, 0f);
                    hazard = "rocks";
                    break;
                }
                case "boss_morgrath":
                {
                    // Ruined battlefield of the throne hall: broken banners, burning debris, spears in the ground.
                    var banner = MaterialFactory.Toon(new Color(0.55f, 0.08f, 0.1f));
                    var metal = MaterialFactory.Toon(new Color(0.35f, 0.35f, 0.38f));
                    for (int i = 0; i < 10; i++)
                    {
                        Vector3 p = Ring(i * 36f + 10f, Rad + Random.Range(0.5f, 3f));
                        var pole = Pillar(p, 5f, 0.12f, dark);
                        pole.transform.rotation = Quaternion.Euler(Random.Range(-15f, 15f), 0f, Random.Range(-15f, 15f));
                        Box(p + Vector3.up * 4.2f + Vector3.right * 0.5f, new Vector3(1f, 1.4f, 0.05f), banner, Random.Range(0f, 180f));
                    }
                    for (int i = 0; i < 14; i++)
                    {
                        var sp = Pillar(Ring(Random.Range(0f, 360f), Random.Range(4f, Rad)), 2f, 0.06f, metal);
                        sp.transform.rotation = Quaternion.Euler(Random.Range(-35f, 35f), 0f, Random.Range(-35f, 35f));
                    }
                    for (int i = 0; i < 4; i++) EnvFx.Fire(D, C + Ring(i * 90f + 45f, Rad * 0.9f), 1.1f, i == 0);
                    hazard = "fire";
                    break;
                }
                case "boss_nyx":
                case "boss_vex":
                {
                    // The final stair: a colossal staircase with hanging chains.
                    var stone = MaterialFactory.Toon(new Color(0.16f, 0.13f, 0.18f));
                    for (int k = 0; k < 10; k++) Box(new Vector3(0f, k * 0.6f + 0.3f, Rad + 2f + k * 1.4f), new Vector3(20f, 0.6f, 1.4f), stone);
                    for (int i = 0; i < 8; i++) if (i != 4) Pillar(Ring(i * 45f, Rad + 2f), 14f, 1.4f, stone);
                    var chain = MaterialFactory.Toon(new Color(0.4f, 0.4f, 0.45f));
                    for (int i = 0; i < 6; i++) Box(Ring(i * 60f, Rad * 0.9f) + Vector3.up * 9f, new Vector3(0.12f, 10f, 0.12f), chain);
                    hazard = "dark";
                    break;
                }
                case "boss_veyrath":
                {
                    // Throne of the Eclipse: towering black pillars, the throne atop its stair, the eclipse window.
                    var red = MaterialFactory.Toon(new Color(0.6f, 0.05f, 0.15f));
                    var glow = MaterialFactory.Toon(new Color(0.9f, 0.1f, 0.3f), 0f, new Color(0.9f, 0.1f, 0.3f));
                    for (int i = 0; i < 12; i++)
                    {
                        if (i == 6) continue;
                        Pillar(Ring(i * 30f, Rad + 2f), 20f, 2f, dark);
                        Box(Ring(i * 30f, Rad + 0.8f) + Vector3.up * 9f, new Vector3(1.4f, 7f, 0.08f), red, i * 30f);
                    }
                    for (int k = 0; k < 6; k++) Box(new Vector3(0f, k * 0.5f + 0.25f, Rad + 2f + k * 1.1f), new Vector3(10f - k, 0.5f, 1.1f), dark);
                    Box(new Vector3(0f, 4.5f, Rad + 8.5f), new Vector3(3f, 3.5f, 1.5f), dark);
                    Box(new Vector3(0f, 7.5f, Rad + 9f), new Vector3(3.6f, 3f, 0.5f), red);
                    var eclipse = MeshFactory.MeshObject(MeshFactory.Ring(0.82f), S, C + new Vector3(0f, 18f, Rad + 12f), Vector3.one * 9f, glow, false);
                    eclipse.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);
                    var sigil = MeshFactory.MeshObject(MeshFactory.Ring(0.95f), D, C + Vector3.up * 0.05f, new Vector3(Rad * 0.9f, 1f, Rad * 0.9f), MaterialFactory.Additive(new Color(0.9f, 0.1f, 0.3f, 0.45f)), false);
                    sigil.AddComponent<Spinner>().DegreesPerSecond = new Vector3(0f, -10f, 0f);
                    for (int i = 0; i < 4; i++) EnvFx.Fire(D, C + Ring(i * 90f + 45f, Rad + 0.5f), 1f, i % 2 == 0);
                    hazard = "dark";
                    break;
                }
                default:
                {
                    for (int i = 0; i < 8; i++) if (i != 4) Pillar(Ring(i * 45f, Rad + 1.5f), 6f, 0.8f, dark);
                    break;
                }
            }
            // Keep the road into the arena clear.
            for (int i = stat.childCount - 1; i >= before; i--)
            {
                var c = stat.GetChild(i);
                if ((Journey.Flat(c.position) - Journey.Flat(C)).magnitude > Rad - 0.5f && j.DistanceToPath(c.position) < j.halfWidth + 2f)
                    Object.DestroyImmediate(c.gameObject);
            }
            var hz = new GameObject("ArenaHazard").AddComponent<ArenaHazard>();
            hz.transform.SetParent(D, false);
            hz.Kind = hazard;
            hz.Center = C;
            hz.Radius = Rad - 2f;
        }
    }
}
