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
            var body = MaterialFactory.Toon(def.bodyColor, 0.035f);
            var outfit = MaterialFactory.Toon(def.haoriColor, 0.035f);
            var hair = MaterialFactory.Toon(def.hairColor, 0.035f);
            var accent = MaterialFactory.Toon(def.accentColor, 0.035f);
            var white = MaterialFactory.Toon(HeadWhite, 0.035f);
            var ink = MaterialFactory.Toon(Ink, 0f);
            var element = MaterialFactory.Toon(ElementChart.ColorOf(def.element), 0.02f, ElementChart.ColorOf(def.element) * 0.35f);

            // Body: one soft rounded shape, with the outfit wrapped around the lower half.
            Part(PrimitiveType.Capsule, Model, new Vector3(0f, 0.55f * h, 0f), new Vector3(0.64f * w, 0.55f * h, 0.56f * w), body);
            Part(PrimitiveType.Capsule, Model, new Vector3(0f, 0.46f * h, -0.02f), new Vector3(0.7f * w, 0.42f * h, 0.62f * w), outfit);
            Part(PrimitiveType.Cube, Model, new Vector3(0f, 0.5f * h, 0.29f * w), new Vector3(0.16f * w, 0.62f * h, 0.04f), body); // open coat front
            Part(PrimitiveType.Cylinder, Model, new Vector3(0f, 0.66f * h, 0f), new Vector3(0.68f * w, 0.045f, 0.6f * w), element); // belt in element colour

            // Head.
            HeadY = 1.12f * h + 0.3f;
            head = new GameObject("Head").transform;
            head.SetParent(Model, false);
            head.localPosition = new Vector3(0f, HeadY, 0f);
            Part(PrimitiveType.Sphere, head, Vector3.zero, Vector3.one * 0.74f, white);
            Part(PrimitiveType.Sphere, head, new Vector3(0.13f, 0.0f, 0.34f), new Vector3(0.075f, 0.14f, 0.05f), ink);
            Part(PrimitiveType.Sphere, head, new Vector3(-0.13f, 0.0f, 0.34f), new Vector3(0.075f, 0.14f, 0.05f), ink);
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
                    Part(PrimitiveType.Sphere, head, new Vector3(0f, 0.14f, -0.04f), new Vector3(0.8f, 0.56f, 0.8f), hair);
                    for (int i = 0; i < 5; i++)
                        Part(PrimitiveType.Sphere, head, new Vector3(-0.2f + i * 0.1f, 0.24f - Mathf.Abs(i - 2) * 0.03f, 0.27f), new Vector3(0.16f, 0.18f, 0.14f), hair);
                    break;
                case HairStyle.Spiky:
                    Part(PrimitiveType.Sphere, head, new Vector3(0f, 0.14f, -0.05f), new Vector3(0.78f, 0.55f, 0.78f), hair);
                    for (int i = 0; i < 8; i++)
                    {
                        float a = -70f + i * 20f;
                        ConePart(head, Quaternion.Euler(0f, 0f, a) * new Vector3(0f, 0.3f, -0.08f), new Vector3(0.16f, 0.32f, 0.16f), hair, new Vector3(-25f, 0f, a));
                    }
                    break;
                case HairStyle.Long:
                    Part(PrimitiveType.Sphere, head, new Vector3(0f, 0.12f, -0.04f), new Vector3(0.8f, 0.58f, 0.8f), hair);
                    Part(PrimitiveType.Capsule, head, new Vector3(0f, -0.35f, -0.22f), new Vector3(0.62f, 0.5f, 0.3f), hair);
                    Part(PrimitiveType.Sphere, head, new Vector3(0f, 0.22f, 0.26f), new Vector3(0.5f, 0.16f, 0.14f), hair); // bangs
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
            Vector3 hand = new Vector3(0.4f * w, 0.72f * h, 0.16f);
            var glow = MaterialFactory.Toon(blade, 0.01f, blade * 0.7f);
            switch (kind)
            {
                case WeaponKind.TwinBlades:
                    BuildSword(blade, 0.8f, hand);
                    Part(PrimitiveType.Cube, Model, new Vector3(-0.42f * w, 0.62f * h, 0.3f), new Vector3(0.05f, 0.07f, 0.8f), glow, new Vector3(35f, -15f, 0f));
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
