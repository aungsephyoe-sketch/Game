using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// The game's chibi character style: a big round white head with dot eyes, a simple rounded body, and a
    /// glowing weapon. Every character still has their own silhouette — hairstyle, proportions, outfit, accessories
    /// and weapon — plus a movement personality (<see cref="MotionStyle"/>) that drives idle, walk and run.
    /// </summary>
    public partial class CharacterVisual
    {
        public MotionStyle Motion = MotionStyle.Steady;
        public WeaponKind Weapon = WeaponKind.Katana;
        public float HeadY { get; private set; }
        Transform head;

        static readonly Color HeadWhite = new Color(0.97f, 0.97f, 0.98f);
        static readonly Color Ink = new Color(0.04f, 0.04f, 0.06f);

        GameObject Part(PrimitiveType t, Transform parent, Vector3 pos, Vector3 scale, Material m, Vector3? euler = null)
        {
            var go = MeshFactory.Primitive(t, parent, pos, scale, m);
            if (euler.HasValue) go.transform.localRotation = Quaternion.Euler(euler.Value);
            Add(go);
            return go;
        }

        GameObject ConePart(Transform parent, Vector3 pos, Vector3 scale, Material m, Vector3 euler)
        {
            var go = MeshFactory.MeshObject(MeshFactory.Cone(), parent, pos, scale, m);
            go.transform.localRotation = Quaternion.Euler(euler);
            Add(go);
            return go;
        }

        void BuildChibi(CharacterDefinition def)
        {
            Motion = def.motion;
            Weapon = def.weapon;
            float h = def.bodyHeight, w = def.bodyWidth;
            // The reference look: a near-black robe for everyone, the colour lives in the hair, hat and blade.
            Color robeC = Color.Lerp(def.bodyColor, new Color(0.05f, 0.05f, 0.07f), 0.82f);
            var body = MaterialFactory.Toon(robeC, 0.035f);
            var outfit = MaterialFactory.Toon(Color.Lerp(def.haoriColor, robeC, 0.75f), 0.035f);
            var hair = MaterialFactory.Toon(def.hairColor, 0.035f);
            var accent = MaterialFactory.Toon(def.accentColor, 0.035f);
            var white = MaterialFactory.Toon(HeadWhite, 0.035f);
            var ink = MaterialFactory.Toon(Ink, 0f);

            // Body: a bell-shaped robe, narrow at the shoulders and flaring to the hem, with short sleeves.
            var robe = MeshFactory.Lathe("robe", new[]
            {
                new Vector2(0.44f, 0f), new Vector2(0.46f, 0.05f), new Vector2(0.42f, 0.2f), new Vector2(0.36f, 0.45f),
                new Vector2(0.31f, 0.7f), new Vector2(0.28f, 0.88f), new Vector2(0.22f, 0.98f), new Vector2(0.1f, 1.02f)
            });
            var robeGo = MeshFactory.MeshObject(robe, Model, Vector3.zero, new Vector3(w, h, w * 0.92f), body);
            Add(robeGo);
            // A faint fold down the front and an inner collar in the outfit colour.
            Part(PrimitiveType.Cube, Model, new Vector3(0.03f, 0.45f * h, 0.36f * w), new Vector3(0.03f, 0.75f * h, 0.03f), outfit, new Vector3(-10f, 0f, 0f));
            Part(PrimitiveType.Sphere, Model, new Vector3(0f, 0.96f * h, 0.12f * w), new Vector3(0.26f * w, 0.12f, 0.16f * w), outfit);
            // Sleeves hanging from the shoulders toward the hands.
            Part(PrimitiveType.Capsule, Model, new Vector3(0.3f * w, 0.72f * h, 0.06f), new Vector3(0.17f, 0.24f * h, 0.17f), body, new Vector3(10f, 0f, 28f));
            Part(PrimitiveType.Capsule, Model, new Vector3(-0.3f * w, 0.72f * h, 0.06f), new Vector3(0.17f, 0.24f * h, 0.17f), body, new Vector3(10f, 0f, -28f));

            // Head: big, round and white, resting on the robe, with two tall dot eyes.
            HeadY = 1.0f * h + 0.36f;
            head = new GameObject("Head").transform;
            head.SetParent(Model, false);
            head.localPosition = new Vector3(0f, HeadY, 0f);
            Part(PrimitiveType.Sphere, head, Vector3.zero, new Vector3(0.8f, 0.78f, 0.78f), white);
            Part(PrimitiveType.Sphere, head, new Vector3(0.13f, -0.03f, 0.365f), new Vector3(0.085f, 0.15f, 0.05f), ink);
            Part(PrimitiveType.Sphere, head, new Vector3(-0.13f, -0.03f, 0.365f), new Vector3(0.085f, 0.15f, 0.05f), ink);
            BuildHair(def.hair, hair, accent);

            // Accessories.
            if (def.scarf)
            {
                Part(PrimitiveType.Cylinder, Model, new Vector3(0f, 1.08f * h, 0f), new Vector3(0.5f * w, 0.07f, 0.48f * w), accent);
                var tail = Part(PrimitiveType.Cube, Model, new Vector3(0.12f, 0.95f * h, -0.3f * w), new Vector3(0.14f, 0.45f, 0.04f), accent, new Vector3(15f, 0f, -10f));
                tail.AddComponent<Sway>().Amount = 10f;
            }
            if (def.cape)
            {
                var capeGo = Part(PrimitiveType.Cube, Model, new Vector3(0f, 0.62f * h, -0.3f * w), new Vector3(0.62f * w, 0.95f * h, 0.035f), outfit, new Vector3(6f, 0f, 0f));
                capeGo.AddComponent<Sway>().Amount = 4f;
            }
            if (def.pelt)
                Part(PrimitiveType.Sphere, Model, new Vector3(-0.3f * w, 1.0f * h, 0f), new Vector3(0.42f, 0.28f, 0.42f), MaterialFactory.Toon(new Color(0.6f, 0.6f, 0.62f), 0.035f));
            if (def.armor)
            {
                var metal = MaterialFactory.Toon(Color.Lerp(def.accentColor, new Color(0.7f, 0.72f, 0.78f), 0.5f), 0.035f);
                Part(PrimitiveType.Sphere, Model, new Vector3(0.36f * w, 0.98f * h, 0f), new Vector3(0.3f, 0.2f, 0.32f), metal);
                Part(PrimitiveType.Sphere, Model, new Vector3(-0.36f * w, 0.98f * h, 0f), new Vector3(0.3f, 0.2f, 0.32f), metal);
                Part(PrimitiveType.Cube, Model, new Vector3(0f, 0.85f * h, 0.26f * w), new Vector3(0.42f * w, 0.3f * h, 0.06f), metal);
            }
            if (def.bell)
                Part(PrimitiveType.Sphere, Model, new Vector3(0.22f * w, 0.62f * h, 0.28f * w), Vector3.one * 0.1f, MaterialFactory.Toon(new Color(0.95f, 0.8f, 0.3f), 0.02f));

            BuildWeapon(def.weapon, def.bladeColor, h, w);
        }

        void BuildHair(HairStyle style, Material hair, Material accent)
        {
            switch (style)
            {
                case HairStyle.Messy:
                    // A snug hood that covers the top, sides and back of the head, leaving the face open.
                    Part(PrimitiveType.Sphere, head, new Vector3(0f, 0.06f, -0.08f), new Vector3(0.9f, 0.88f, 0.86f), hair);
                    Part(PrimitiveType.Sphere, head, new Vector3(0f, 0.24f, 0.16f), new Vector3(0.66f, 0.3f, 0.44f), hair);
                    Part(PrimitiveType.Capsule, head, new Vector3(0f, -0.34f, -0.18f), new Vector3(0.5f, 0.2f, 0.42f), hair, new Vector3(0f, 0f, 90f));
                    break;
                case HairStyle.Spiky:
                    // Black spikes bursting out in every direction.
                    Part(PrimitiveType.Sphere, head, new Vector3(0f, 0.1f, -0.06f), new Vector3(0.84f, 0.7f, 0.82f), hair);
                    for (int ring = 0; ring < 2; ring++)
                    {
                        int n = ring == 0 ? 10 : 7;
                        for (int i = 0; i < n; i++)
                        {
                            float a = -100f + i * (200f / (n - 1));
                            float tilt = ring == 0 ? 0f : -35f;
                            Vector3 dir = Quaternion.Euler(tilt, 0f, a) * Vector3.up;
                            ConePart(head, dir * 0.3f + new Vector3(0f, 0.08f, -0.06f - ring * 0.12f), new Vector3(0.17f, 0.38f - ring * 0.06f, 0.17f), hair, new Vector3(tilt, 0f, a));
                        }
                    }
                    // Fringe spikes over the forehead.
                    for (int i = 0; i < 4; i++)
                        ConePart(head, new Vector3(-0.18f + i * 0.12f, 0.24f, 0.26f), new Vector3(0.13f, 0.22f, 0.13f), hair, new Vector3(150f, 0f, -20f + i * 13f));
                    break;
                case HairStyle.Long:
                    // Flowing hair over the back and sides, bangs, and a little top-knot.
                    Part(PrimitiveType.Sphere, head, new Vector3(0f, 0.08f, -0.06f), new Vector3(0.88f, 0.8f, 0.84f), hair);
                    Part(PrimitiveType.Capsule, head, new Vector3(0f, -0.38f, -0.2f), new Vector3(0.74f, 0.46f, 0.36f), hair);
                    Part(PrimitiveType.Capsule, head, new Vector3(0.33f, -0.22f, 0.08f), new Vector3(0.16f, 0.3f, 0.16f), hair);
                    Part(PrimitiveType.Capsule, head, new Vector3(-0.33f, -0.22f, 0.08f), new Vector3(0.16f, 0.3f, 0.16f), hair);
                    Part(PrimitiveType.Sphere, head, new Vector3(0.05f, 0.26f, 0.24f), new Vector3(0.58f, 0.2f, 0.2f), hair, new Vector3(0f, 0f, -8f));
                    Part(PrimitiveType.Sphere, head, new Vector3(0f, 0.47f, -0.05f), new Vector3(0.18f, 0.2f, 0.18f), hair);
                    ConePart(head, new Vector3(0.05f, 0.55f, -0.05f), new Vector3(0.1f, 0.18f, 0.1f), hair, new Vector3(0f, 0f, -20f));
                    break;
                case HairStyle.Ponytail:
                    Part(PrimitiveType.Sphere, head, new Vector3(0f, 0.13f, -0.04f), new Vector3(0.78f, 0.55f, 0.78f), hair);
                    Part(PrimitiveType.Sphere, head, new Vector3(0f, 0.28f, -0.34f), Vector3.one * 0.14f, accent);
                    var tail = Part(PrimitiveType.Capsule, head, new Vector3(0f, 0.05f, -0.52f), new Vector3(0.2f, 0.36f, 0.2f), hair, new Vector3(-35f, 0f, 0f));
                    tail.AddComponent<Sway>().Amount = 8f;
                    break;
                case HairStyle.Braid:
                    Part(PrimitiveType.Sphere, head, new Vector3(0f, 0.12f, -0.04f), new Vector3(0.8f, 0.56f, 0.8f), hair);
                    for (int i = 0; i < 4; i++)
                        Part(PrimitiveType.Sphere, head, new Vector3(0.3f + i * 0.02f, -0.18f - i * 0.15f, 0.12f), Vector3.one * (0.16f - i * 0.015f), hair);
                    Part(PrimitiveType.Sphere, head, new Vector3(-0.2f, 0.3f, 0.2f), Vector3.one * 0.1f, accent); // flower pin
                    break;
                case HairStyle.Short:
                    Part(PrimitiveType.Sphere, head, new Vector3(0f, 0.17f, -0.04f), new Vector3(0.77f, 0.48f, 0.77f), hair);
                    break;
                case HairStyle.Bun:
                    Part(PrimitiveType.Sphere, head, new Vector3(0f, 0.14f, -0.04f), new Vector3(0.78f, 0.52f, 0.78f), hair);
                    Part(PrimitiveType.Sphere, head, new Vector3(0f, 0.42f, -0.12f), Vector3.one * 0.3f, hair);
                    break;
                case HairStyle.Hood:
                    Part(PrimitiveType.Sphere, head, new Vector3(0f, 0.04f, -0.07f), new Vector3(0.84f, 0.84f, 0.84f), hair);
                    ConePart(head, new Vector3(0f, 0.35f, -0.12f), new Vector3(0.3f, 0.3f, 0.3f), hair, new Vector3(-30f, 0f, 0f));
                    break;
                case HairStyle.Cap:
                    Part(PrimitiveType.Sphere, head, new Vector3(0f, 0.2f, -0.02f), new Vector3(0.8f, 0.48f, 0.8f), accent);
                    Part(PrimitiveType.Cylinder, head, new Vector3(0f, 0.17f, 0.3f), new Vector3(0.55f, 0.02f, 0.35f), accent);
                    break;
                case HairStyle.StrawHat:
                    Part(PrimitiveType.Sphere, head, new Vector3(0f, 0.12f, -0.04f), new Vector3(0.77f, 0.5f, 0.77f), hair);
                    var hat = MeshFactory.MeshObject(MeshFactory.Cone(), head, new Vector3(0f, 0.24f, 0f), new Vector3(1.35f, 0.32f, 1.35f), accent);
                    Add(hat);
                    break;
                case HairStyle.Wild:
                    Part(PrimitiveType.Sphere, head, new Vector3(0f, 0.14f, -0.05f), new Vector3(0.84f, 0.6f, 0.84f), hair);
                    for (int i = 0; i < 11; i++)
                    {
                        float a = i * 33f;
                        ConePart(head, Quaternion.Euler(0f, a, 0f) * new Vector3(0f, 0.22f, -0.3f), new Vector3(0.15f, 0.3f, 0.15f), hair, new Vector3(-70f, a + 180f, 0f));
                    }
                    Part(PrimitiveType.Cylinder, head, new Vector3(0f, 0.12f, 0f), new Vector3(0.79f, 0.03f, 0.79f), MaterialFactory.Toon(new Color(0.8f, 0.15f, 0.15f), 0.02f)); // headband
                    break;
                case HairStyle.Crest:
                    Part(PrimitiveType.Sphere, head, new Vector3(0f, 0.16f, -0.04f), new Vector3(0.78f, 0.5f, 0.78f), hair);
                    for (int i = 0; i < 4; i++)
                        ConePart(head, new Vector3(0f, 0.36f, 0.12f - i * 0.14f), new Vector3(0.08f, 0.38f - i * 0.05f, 0.24f), hair, new Vector3(-20f - i * 12f, 0f, 0f));
                    break;
            }
        }

        void BuildWeapon(WeaponKind kind, Color blade, float h, float w)
        {
            Vector3 hand = new Vector3(0.42f * w, 0.52f * h, 0.14f);
            var glow = MaterialFactory.Toon(blade, 0.01f, blade * 0.7f);
            switch (kind)
            {
                case WeaponKind.Fists:
                {
                    // Glowing gauntlets on both fists; the "sword" pivot is the right fist.
                    BuildSword(blade, 0.05f, hand);
                    var gl = MaterialFactory.Toon(Color.Lerp(blade, Color.white, 0.2f), 0.02f, blade * 0.8f);
                    Add(MeshFactory.Primitive(PrimitiveType.Sphere, SwordPivot, Vector3.zero, new Vector3(0.3f, 0.3f, 0.34f), gl));
                    Part(PrimitiveType.Sphere, Model, new Vector3(-0.42f * w, 0.52f * h, 0.14f), new Vector3(0.3f, 0.3f, 0.34f), gl, Vector3.zero);
                    // Wrist wraps.
                    var wrap = MaterialFactory.Toon(new Color(0.9f, 0.88f, 0.8f), 0.02f);
                    Add(MeshFactory.Primitive(PrimitiveType.Cylinder, SwordPivot, new Vector3(0f, 0f, -0.2f), new Vector3(0.22f, 0.08f, 0.22f), wrap));
                    break;
                }
                case WeaponKind.TwinBlades:
                    BuildSword(blade, 0.8f, hand);
                    {
                        // Second blade, held low in the left hand like the reference.
                        var left = new GameObject("LeftBlade").transform;
                        left.SetParent(Model, false);
                        left.localPosition = new Vector3(-0.42f * w, 0.52f * h, 0.14f);
                        left.localRotation = Quaternion.Euler(48f, -62f, 0f);
                        Add(MeshFactory.Primitive(PrimitiveType.Cube, left, new Vector3(0f, 0f, 0.45f), new Vector3(0.05f, 0.08f, 0.8f), glow));
                        var lh = MeshFactory.Primitive(PrimitiveType.Cube, left, new Vector3(0f, 0f, 0.46f), new Vector3(0.16f, 0.2f, 0.84f), MaterialFactory.Additive(new Color(blade.r, blade.g, blade.b, 0.3f)));
                        lh.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    }
                    break;
                case WeaponKind.Greatsword:
                    BuildSword(blade, 1.5f, hand);
                    Add(MeshFactory.Primitive(PrimitiveType.Cube, SwordPivot, new Vector3(0f, 0f, 0.85f), new Vector3(0.09f, 0.26f, 1.3f), glow));
                    break;
                case WeaponKind.SwordShield:
                    BuildSword(blade, 1f, hand);
                    var shield = Part(PrimitiveType.Cylinder, Model, new Vector3(-0.44f * w, 0.7f * h, 0.18f), new Vector3(0.62f, 0.05f, 0.62f), MaterialFactory.Toon(new Color(0.3f, 0.36f, 0.5f), 0.03f), new Vector3(0f, 0f, 90f));
                    MeshFactory.Primitive(PrimitiveType.Sphere, shield.transform, new Vector3(0f, -1.1f, 0f), new Vector3(0.35f, 0.5f, 0.35f), MaterialFactory.Toon(new Color(0.95f, 0.8f, 0.3f), 0.02f));
                    break;
                case WeaponKind.Spear:
                    BuildSword(new Color(0.35f, 0.28f, 0.2f), 2.1f, hand);
                    var tip = MeshFactory.MeshObject(MeshFactory.Cone(), SwordPivot, new Vector3(0f, 0f, 2.1f), new Vector3(0.14f, 0.45f, 0.14f), glow);
                    tip.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                    Add(tip);
                    break;
                case WeaponKind.Staff:
                    BuildSword(new Color(0.55f, 0.45f, 0.6f), 1.4f, hand);
                    Add(MeshFactory.Primitive(PrimitiveType.Sphere, SwordPivot, new Vector3(0f, 0f, 1.45f), Vector3.one * 0.26f, glow));
                    break;
                case WeaponKind.Bow:
                    BuildSword(blade, 0.3f, hand);
                    var bow = MeshFactory.MeshObject(MeshFactory.Sector(150f, 0.9f), Model, new Vector3(-0.42f * w, 0.8f * h, 0.25f), new Vector3(0.9f, 1f, 0.9f), MaterialFactory.Toon(new Color(0.45f, 0.3f, 0.18f), 0.02f), false);
                    bow.transform.localRotation = Quaternion.Euler(0f, 90f, 90f);
                    Add(bow);
                    break;
                case WeaponKind.Fans:
                    BuildSword(blade, 0.35f, hand);
                    var fanMat = MaterialFactory.Toon(blade, 0.01f, blade * 0.35f);
                    var fan = MeshFactory.MeshObject(MeshFactory.Sector(110f, 0.25f), SwordPivot, new Vector3(0f, 0f, 0.15f), Vector3.one * 0.55f, fanMat, false);
                    fan.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                    Add(fan);
                    var fan2 = MeshFactory.MeshObject(MeshFactory.Sector(110f, 0.25f), Model, new Vector3(-0.42f * w, 0.75f * h, 0.3f), Vector3.one * 0.55f, fanMat, false);
                    fan2.transform.localRotation = Quaternion.Euler(-20f, 0f, 90f);
                    Add(fan2);
                    break;
                case WeaponKind.Cleavers:
                    BuildSword(blade, 0.75f, hand);
                    Add(MeshFactory.Primitive(PrimitiveType.Cube, SwordPivot, new Vector3(0.1f, 0f, 0.5f), new Vector3(0.07f, 0.3f, 0.65f), glow));
                    Part(PrimitiveType.Cube, Model, new Vector3(-0.45f * w, 0.6f * h, 0.28f), new Vector3(0.07f, 0.3f, 0.65f), glow, new Vector3(30f, -10f, 0f));
                    break;
                case WeaponKind.Cane:
                    BuildSword(new Color(0.45f, 0.32f, 0.2f), 0.95f, hand);
                    break;
                case WeaponKind.Moon:
                    BuildSword(blade, 1.1f, hand);
                    var moon = MeshFactory.MeshObject(MeshFactory.Sector(160f, 0.8f), SwordPivot, new Vector3(0f, 0f, 0.9f), Vector3.one * 0.6f, glow, false);
                    moon.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                    Add(moon);
                    break;
                default:
                    BuildSword(blade, 1.2f, hand);
                    break;
            }
        }
    }
}
