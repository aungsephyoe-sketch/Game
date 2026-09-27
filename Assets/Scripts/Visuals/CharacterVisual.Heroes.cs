using System.Collections;
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

        // ------------------------------------------------------------------ Signature poses

        public bool Posing { get { return posing; } }

        /// <summary>Body tilt, crouch, weapon angle and head tilt for each personality's showcase pose.</summary>
        void PoseValues(out Vector3 body, out float drop, out Vector3 weapon, out Vector3 headE)
        {
            switch (Motion)
            {
                case MotionStyle.Confident: body = new Vector3(-4f, 0f, 0f); drop = 0f; weapon = new Vector3(-125f, 25f, 0f); headE = new Vector3(-8f, 0f, 0f); break;   // blade on the shoulder
                case MotionStyle.Nervous: body = new Vector3(10f, 0f, -4f); drop = 0.04f; weapon = new Vector3(-8f, 12f, 0f); headE = new Vector3(0f, 10f, 5f); break;    // low guard
                case MotionStyle.Aggressive: body = new Vector3(15f, 0f, 0f); drop = 0.1f; weapon = new Vector3(-65f, 20f, 0f); headE = new Vector3(8f, 0f, 0f); break;  // crouched, fist raised
                case MotionStyle.Graceful: body = new Vector3(-3f, 0f, 6f); drop = 0f; weapon = new Vector3(-95f, 0f, 0f); headE = new Vector3(0f, 0f, -8f); break;      // staff / fan raised high
                case MotionStyle.Stoic: body = Vector3.zero; drop = 0f; weapon = new Vector3(88f, 0f, 0f); headE = Vector3.zero; break;                                  // blade planted in front
                case MotionStyle.Sly: body = new Vector3(-5f, 15f, 8f); drop = 0f; weapon = new Vector3(40f, 150f, 0f); headE = new Vector3(0f, -12f, 6f); break;       // blade hidden behind
                case MotionStyle.Light: body = new Vector3(6f, -10f, 0f); drop = -0.05f; weapon = new Vector3(-35f, -40f, 0f); headE = new Vector3(0f, 8f, -6f); break;  // weapon across the body, on tiptoe
                default: body = new Vector3(6f, 0f, 0f); drop = 0.03f; weapon = new Vector3(-22f, 32f, 0f); headE = Vector3.zero; break;                                 // ready stance
            }
        }

        /// <summary>Snap straight into the showcase pose (portraits).</summary>
        public void ApplySignaturePose()
        {
            Vector3 b, wpn, he; float drop;
            PoseValues(out b, out drop, out wpn, out he);
            Model.localRotation = Quaternion.Euler(b);
            Model.localPosition = Vector3.down * drop;
            if (SwordPivot != null) SwordPivot.localRotation = Quaternion.Euler(wpn);
            if (head != null) head.localRotation = Quaternion.Euler(he);
        }

        /// <summary>Ease into the showcase pose and hold it, breathing (team line-up, character screen).</summary>
        public void HoldSignaturePose()
        {
            if (driver != null || dead) return;
            StartPose(SignaturePoseRoutine());
        }

        IEnumerator SignaturePoseRoutine()
        {
            Vector3 b, wpn, he; float drop;
            PoseValues(out b, out drop, out wpn, out he);
            float t = 0f;
            while (true)
            {
                t += Time.deltaTime;
                float k = 1f - Mathf.Exp(-Time.deltaTime * 6f);
                float breathe = Mathf.Sin(t * 2f) * 0.015f;
                Model.localRotation = Quaternion.Slerp(Model.localRotation, Quaternion.Euler(b + new Vector3(Mathf.Sin(t * 1.3f) * 1.5f, 0f, 0f)), k);
                Model.localPosition = Vector3.Lerp(Model.localPosition, Vector3.down * drop + Vector3.up * breathe, k);
                Model.localScale = Vector3.one * baseScale * (1f + breathe * 0.5f);
                if (SwordPivot != null) SwordPivot.localRotation = Quaternion.Slerp(SwordPivot.localRotation, Quaternion.Euler(wpn + new Vector3(Mathf.Sin(t * 1.7f) * 3f, 0f, 0f)), k);
                if (head != null) head.localRotation = Quaternion.Slerp(head.localRotation, Quaternion.Euler(he + new Vector3(0f, Mathf.Sin(t * 0.7f) * 6f, 0f)), k);
                UpdateFlash();
                yield return null;
            }
        }

        /// <summary>Smooth, high-resolution sphere part (faces, hair, hands).</summary>
        GameObject Ball(Transform parent, Vector3 pos, Vector3 scale, Material m, Vector3? euler = null)
        {
            var go = MeshFactory.MeshObject(MeshFactory.SmoothSphere(), parent, pos, scale, m);
            if (euler.HasValue) go.transform.localRotation = Quaternion.Euler(euler.Value);
            Add(go);
            return go;
        }

        void BuildChibi(CharacterDefinition def)
        {
            Motion = def.motion;
            Weapon = def.weapon;
            float h = def.bodyHeight, w = def.bodyWidth;
            // A dark robe for everyone (as in the reference art), with colour in the trims, sash, hair and blade.
            Color robeC = Color.Lerp(def.bodyColor, new Color(0.06f, 0.06f, 0.08f), 0.78f);
            var body = MaterialFactory.Toon(robeC, 0.02f);
            var trim = MaterialFactory.Toon(Color.Lerp(def.haoriColor, robeC, 0.25f), 0.015f);
            var sash = MaterialFactory.Toon(def.accentColor, 0.015f);
            var hair = MaterialFactory.Toon(def.hairColor, 0.02f);
            var accent = MaterialFactory.Toon(def.accentColor, 0.02f);
            var skin = MaterialFactory.Toon(def.skinTone, 0.02f);
            var ink = MaterialFactory.Toon(Ink, 0f);
            var shoe = MaterialFactory.Toon(Color.Lerp(robeC, new Color(0.2f, 0.14f, 0.1f), 0.5f), 0.015f);

            // Robe: a smooth bell that flares to the hem.
            var robe = MeshFactory.Lathe("robe2", new[]
            {
                new Vector2(0.44f, 0.04f), new Vector2(0.46f, 0.08f), new Vector2(0.44f, 0.16f), new Vector2(0.41f, 0.28f),
                new Vector2(0.37f, 0.42f), new Vector2(0.34f, 0.56f), new Vector2(0.31f, 0.7f), new Vector2(0.29f, 0.82f),
                new Vector2(0.26f, 0.91f), new Vector2(0.2f, 0.98f), new Vector2(0.11f, 1.02f), new Vector2(0.02f, 1.03f)
            }, 40);
            Add(MeshFactory.MeshObject(robe, Model, Vector3.zero, new Vector3(w, h, w * 0.92f), body));
            // Hem trim, sash with a knot, and a crossed collar in the outfit colour.
            var hem = MeshFactory.Lathe("hem", new[] { new Vector2(0.455f, 0.035f), new Vector2(0.47f, 0.07f), new Vector2(0.455f, 0.105f) }, 40);
            Add(MeshFactory.MeshObject(hem, Model, Vector3.zero, new Vector3(w, h, w * 0.92f), trim));
            var band = MeshFactory.Lathe("sash", new[] { new Vector2(0.34f, 0.54f), new Vector2(0.355f, 0.58f), new Vector2(0.345f, 0.62f) }, 40);
            Add(MeshFactory.MeshObject(band, Model, Vector3.zero, new Vector3(w, h, w * 0.92f), sash));
            Ball(Model, new Vector3(0.2f * w, 0.57f * h, 0.26f * w), new Vector3(0.12f, 0.1f, 0.08f), sash);
            Part(PrimitiveType.Cube, Model, new Vector3(0.2f * w, 0.47f * h, 0.28f * w), new Vector3(0.05f, 0.14f * h, 0.02f), sash, new Vector3(-8f, 0f, 12f));
            Part(PrimitiveType.Cube, Model, new Vector3(0.07f * w, 0.86f * h, 0.24f * w), new Vector3(0.05f, 0.26f * h, 0.03f), trim, new Vector3(-18f, 0f, -32f));
            Part(PrimitiveType.Cube, Model, new Vector3(-0.07f * w, 0.86f * h, 0.24f * w), new Vector3(0.05f, 0.26f * h, 0.03f), trim, new Vector3(-18f, 0f, 32f));
            // Sleeves with trimmed cuffs, and hands.
            for (int sgn = -1; sgn <= 1; sgn += 2)
            {
                Part(PrimitiveType.Capsule, Model, new Vector3(0.3f * sgn * w, 0.72f * h, 0.06f), new Vector3(0.18f, 0.24f * h, 0.18f), body, new Vector3(10f, 0f, 28f * sgn));
                Part(PrimitiveType.Cylinder, Model, new Vector3(0.39f * sgn * w, 0.56f * h, 0.1f), new Vector3(0.17f, 0.02f, 0.17f), trim, new Vector3(10f, 0f, 28f * sgn));
                Ball(Model, new Vector3(0.42f * sgn * w, 0.5f * h, 0.14f), Vector3.one * 0.13f, skin);
            }
            // Shoes peeking out under the hem.
            Ball(Model, new Vector3(0.13f * w, 0.05f, 0.2f * w), new Vector3(0.17f, 0.1f, 0.26f), shoe);
            Ball(Model, new Vector3(-0.13f * w, 0.05f, 0.2f * w), new Vector3(0.17f, 0.1f, 0.26f), shoe);

            // Head: big and round in the character's skin tone, with a friendly face.
            HeadY = 1.0f * h + 0.36f;
            head = new GameObject("Head").transform;
            head.SetParent(Model, false);
            head.localPosition = new Vector3(0f, HeadY, 0f);
            Ball(head, Vector3.zero, new Vector3(0.8f, 0.78f, 0.78f), skin);
            Ball(head, new Vector3(0.39f, -0.04f, 0f), new Vector3(0.1f, 0.14f, 0.1f), skin);
            Ball(head, new Vector3(-0.39f, -0.04f, 0f), new Vector3(0.1f, 0.14f, 0.1f), skin);
            BuildFace(def, ink);
            BuildHair(def.hair, hair, accent);

            // Accessories.
            if (def.scarf)
            {
                Part(PrimitiveType.Cylinder, Model, new Vector3(0f, 1.0f * h, 0f), new Vector3(0.52f * w, 0.07f, 0.5f * w), accent);
                var tail = Part(PrimitiveType.Cube, Model, new Vector3(0.12f, 0.88f * h, -0.3f * w), new Vector3(0.14f, 0.45f, 0.04f), accent, new Vector3(15f, 0f, -10f));
                tail.AddComponent<Sway>().Amount = 10f;
            }
            if (def.cape)
            {
                var capeGo = Part(PrimitiveType.Cube, Model, new Vector3(0f, 0.55f * h, -0.34f * w), new Vector3(0.62f * w, 0.9f * h, 0.035f), trim, new Vector3(8f, 0f, 0f));
                capeGo.AddComponent<Sway>().Amount = 4f;
            }
            if (def.pelt)
                Ball(Model, new Vector3(-0.28f * w, 0.95f * h, 0f), new Vector3(0.42f, 0.26f, 0.42f), MaterialFactory.Toon(new Color(0.62f, 0.6f, 0.58f), 0.02f));
            if (def.armor)
            {
                var metal = MaterialFactory.Toon(Color.Lerp(def.accentColor, new Color(0.7f, 0.72f, 0.78f), 0.5f), 0.02f);
                Ball(Model, new Vector3(0.34f * w, 0.92f * h, 0f), new Vector3(0.3f, 0.2f, 0.32f), metal);
                Ball(Model, new Vector3(-0.34f * w, 0.92f * h, 0f), new Vector3(0.3f, 0.2f, 0.32f), metal);
                Part(PrimitiveType.Cube, Model, new Vector3(0f, 0.76f * h, 0.3f * w), new Vector3(0.4f * w, 0.26f * h, 0.05f), metal, new Vector3(-10f, 0f, 0f));
            }
            if (def.bell)
                Ball(Model, new Vector3(-0.2f * w, 0.55f * h, 0.3f * w), Vector3.one * 0.1f, MaterialFactory.Toon(new Color(0.95f, 0.8f, 0.3f), 0.02f));

            BuildWeapon(def.weapon, def.bladeColor, h, w);
        }

        /// <summary>Eyes with a shine, brows that show personality, a small smile and rosy cheeks.</summary>
        void BuildFace(CharacterDefinition def, Material ink)
        {
            var shine = MaterialFactory.Toon(Color.white, 0f, new Color(0.6f, 0.6f, 0.6f));
            Color browC = Color.Lerp(def.hairColor, Color.black, 0.45f);
            var brow = MaterialFactory.Toon(browC, 0f);
            float browTilt;
            switch (def.motion)
            {
                case MotionStyle.Aggressive: browTilt = 22f; break;
                case MotionStyle.Nervous: browTilt = -18f; break;
                case MotionStyle.Stoic: browTilt = 8f; break;
                case MotionStyle.Sly: browTilt = 14f; break;
                case MotionStyle.Graceful: case MotionStyle.Light: browTilt = -6f; break;
                default: browTilt = 4f; break;
            }
            for (int sgn = -1; sgn <= 1; sgn += 2)
            {
                Ball(head, new Vector3(0.13f * sgn, -0.03f, 0.365f), new Vector3(0.09f, 0.15f, 0.05f), ink);
                Ball(head, new Vector3(0.13f * sgn + 0.022f, 0.02f, 0.385f), new Vector3(0.035f, 0.045f, 0.02f), shine);
                Ball(head, new Vector3(0.13f * sgn - 0.015f, -0.07f, 0.385f), new Vector3(0.015f, 0.02f, 0.01f), shine);
                Part(PrimitiveType.Capsule, head, new Vector3(0.14f * sgn, 0.11f, 0.35f), new Vector3(0.03f, 0.055f, 0.02f), brow, new Vector3(0f, 0f, 90f + browTilt * sgn));
                var blush = MaterialFactory.Transparent(new Color(1f, 0.45f, 0.45f, 0.35f));
                var b = MeshFactory.MeshObject(MeshFactory.SmoothSphere(), head, new Vector3(0.23f * sgn, -0.12f, 0.315f), new Vector3(0.1f, 0.05f, 0.02f), blush, false);
                b.transform.localRotation = Quaternion.Euler(0f, 30f * sgn, 0f);
            }
            // A small smile.
            var mouth = MaterialFactory.Toon(new Color(0.35f, 0.14f, 0.14f), 0f);
            Part(PrimitiveType.Capsule, head, new Vector3(-0.025f, -0.17f, 0.37f), new Vector3(0.018f, 0.035f, 0.015f), mouth, new Vector3(0f, 0f, 65f));
            Part(PrimitiveType.Capsule, head, new Vector3(0.025f, -0.17f, 0.37f), new Vector3(0.018f, 0.035f, 0.015f), mouth, new Vector3(0f, 0f, -65f));
        }

        void BuildHair(HairStyle style, Material hair, Material accent)
        {
            switch (style)
            {
                case HairStyle.Messy:
                    // Tousled hair: a soft cap with tufts sticking out and a swept fringe.
                    Ball(head, new Vector3(0f, 0.12f, -0.06f), new Vector3(0.86f, 0.66f, 0.84f), hair);
                    for (int i = 0; i < 9; i++)
                    {
                        float a = -120f + i * 30f;
                        ConePart(head, Quaternion.Euler(-25f, 0f, a) * new Vector3(0f, 0.34f, -0.02f), new Vector3(0.16f, 0.24f, 0.16f), hair, new Vector3(-25f, 0f, a));
                    }
                    for (int i = 0; i < 4; i++)
                        ConePart(head, new Vector3(-0.2f + i * 0.13f, 0.22f, 0.27f), new Vector3(0.15f, 0.24f, 0.12f), hair, new Vector3(160f, 0f, 25f - i * 6f));
                    break;
                case HairStyle.Spiky:
                    Ball(head, new Vector3(0f, 0.1f, -0.06f), new Vector3(0.84f, 0.7f, 0.82f), hair);
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
                    for (int i = 0; i < 4; i++)
                        ConePart(head, new Vector3(-0.18f + i * 0.12f, 0.24f, 0.26f), new Vector3(0.13f, 0.22f, 0.13f), hair, new Vector3(150f, 0f, -20f + i * 13f));
                    break;
                case HairStyle.Long:
                    Ball(head, new Vector3(0f, 0.08f, -0.07f), new Vector3(0.88f, 0.8f, 0.84f), hair);
                    Part(PrimitiveType.Capsule, head, new Vector3(0f, -0.38f, -0.2f), new Vector3(0.74f, 0.46f, 0.36f), hair);
                    Part(PrimitiveType.Capsule, head, new Vector3(0.34f, -0.22f, 0.08f), new Vector3(0.15f, 0.3f, 0.15f), hair);
                    Part(PrimitiveType.Capsule, head, new Vector3(-0.34f, -0.22f, 0.08f), new Vector3(0.15f, 0.3f, 0.15f), hair);
                    Ball(head, new Vector3(0.05f, 0.25f, 0.24f), new Vector3(0.58f, 0.2f, 0.2f), hair, new Vector3(0f, 0f, -8f));
                    Ball(head, new Vector3(0f, 0.47f, -0.05f), new Vector3(0.18f, 0.2f, 0.18f), hair);
                    Ball(head, new Vector3(0f, 0.4f, -0.03f), new Vector3(0.12f, 0.05f, 0.12f), accent);
                    break;
                case HairStyle.Ponytail:
                    Ball(head, new Vector3(0f, 0.12f, -0.05f), new Vector3(0.84f, 0.64f, 0.82f), hair);
                    Ball(head, new Vector3(0.06f, 0.23f, 0.25f), new Vector3(0.5f, 0.16f, 0.18f), hair, new Vector3(0f, 0f, -12f));
                    Ball(head, new Vector3(0f, 0.3f, -0.34f), Vector3.one * 0.14f, accent);
                    var tail = Part(PrimitiveType.Capsule, head, new Vector3(0f, 0.02f, -0.52f), new Vector3(0.22f, 0.38f, 0.22f), hair, new Vector3(-35f, 0f, 0f));
                    tail.AddComponent<Sway>().Amount = 8f;
                    break;
                case HairStyle.Braid:
                    Ball(head, new Vector3(0f, 0.11f, -0.05f), new Vector3(0.86f, 0.66f, 0.84f), hair);
                    Ball(head, new Vector3(-0.08f, 0.24f, 0.24f), new Vector3(0.52f, 0.16f, 0.18f), hair, new Vector3(0f, 0f, 10f));
                    for (int i = 0; i < 5; i++)
                        Ball(head, new Vector3(0.32f + i * 0.015f, -0.14f - i * 0.13f, 0.12f), Vector3.one * (0.17f - i * 0.015f), hair);
                    Ball(head, new Vector3(0.36f, -0.8f, 0.13f), Vector3.one * 0.07f, accent);
                    Ball(head, new Vector3(-0.22f, 0.3f, 0.2f), Vector3.one * 0.1f, accent);
                    break;
                case HairStyle.Short:
                    Ball(head, new Vector3(0f, 0.16f, -0.05f), new Vector3(0.83f, 0.56f, 0.82f), hair);
                    Ball(head, new Vector3(0.36f, -0.02f, 0.02f), new Vector3(0.06f, 0.16f, 0.1f), hair);
                    Ball(head, new Vector3(-0.36f, -0.02f, 0.02f), new Vector3(0.06f, 0.16f, 0.1f), hair);
                    break;
                case HairStyle.Bun:
                    Ball(head, new Vector3(0f, 0.13f, -0.05f), new Vector3(0.84f, 0.62f, 0.82f), hair);
                    Ball(head, new Vector3(0f, 0.44f, -0.12f), Vector3.one * 0.3f, hair);
                    Part(PrimitiveType.Cylinder, head, new Vector3(0.08f, 0.47f, -0.12f), new Vector3(0.03f, 0.2f, 0.03f), accent, new Vector3(0f, 0f, 60f));
                    break;
                case HairStyle.Hood:
                    Ball(head, new Vector3(0f, 0.04f, -0.08f), new Vector3(0.9f, 0.88f, 0.86f), hair);
                    ConePart(head, new Vector3(0f, 0.35f, -0.12f), new Vector3(0.3f, 0.3f, 0.3f), hair, new Vector3(-30f, 0f, 0f));
                    break;
                case HairStyle.Cap:
                    Ball(head, new Vector3(0f, 0.2f, -0.02f), new Vector3(0.84f, 0.52f, 0.84f), accent);
                    Part(PrimitiveType.Cylinder, head, new Vector3(0f, 0.17f, 0.3f), new Vector3(0.55f, 0.02f, 0.35f), accent);
                    Ball(head, new Vector3(0f, -0.05f, -0.2f), new Vector3(0.7f, 0.4f, 0.5f), hair);
                    break;
                case HairStyle.StrawHat:
                    Ball(head, new Vector3(0f, 0.1f, -0.05f), new Vector3(0.83f, 0.56f, 0.82f), hair);
                    Ball(head, new Vector3(0f, 0.08f, -0.45f), new Vector3(0.2f, 0.2f, 0.2f), hair);
                    var hat = MeshFactory.MeshObject(MeshFactory.Cone(), head, new Vector3(0f, 0.24f, 0f), new Vector3(1.35f, 0.32f, 1.35f), accent);
                    Add(hat);
                    Part(PrimitiveType.Cylinder, head, new Vector3(0f, 0.27f, 0f), new Vector3(0.62f, 0.03f, 0.62f), MaterialFactory.Toon(new Color(0.55f, 0.15f, 0.12f), 0.01f));
                    break;
                case HairStyle.Wild:
                    Ball(head, new Vector3(0f, 0.13f, -0.06f), new Vector3(0.88f, 0.66f, 0.86f), hair);
                    for (int i = 0; i < 11; i++)
                    {
                        float a = i * 33f;
                        ConePart(head, Quaternion.Euler(0f, a, 0f) * new Vector3(0f, 0.22f, -0.3f), new Vector3(0.15f, 0.3f, 0.15f), hair, new Vector3(-70f, a + 180f, 0f));
                    }
                    Part(PrimitiveType.Cylinder, head, new Vector3(0f, 0.12f, 0f), new Vector3(0.83f, 0.035f, 0.83f), MaterialFactory.Toon(new Color(0.8f, 0.15f, 0.15f), 0.02f));
                    break;
                case HairStyle.Crest:
                    Ball(head, new Vector3(0f, 0.15f, -0.05f), new Vector3(0.82f, 0.54f, 0.8f), hair);
                    for (int i = 0; i < 5; i++)
                        ConePart(head, new Vector3(0f, 0.36f, 0.16f - i * 0.13f), new Vector3(0.09f, 0.4f - i * 0.05f, 0.24f), hair, new Vector3(-20f - i * 12f, 0f, 0f));
                    break;
                case HairStyle.Bob:
                    // A neat bob to the jaw with a straight fringe.
                    Ball(head, new Vector3(0f, 0.04f, -0.09f), new Vector3(0.94f, 0.86f, 0.86f), hair);
                    Ball(head, new Vector3(0f, 0.2f, 0.27f), new Vector3(0.66f, 0.2f, 0.18f), hair);
                    Ball(head, new Vector3(0.36f, -0.2f, 0.1f), new Vector3(0.16f, 0.3f, 0.2f), hair);
                    Ball(head, new Vector3(-0.36f, -0.2f, 0.1f), new Vector3(0.16f, 0.3f, 0.2f), hair);
                    Ball(head, new Vector3(0.24f, 0.3f, 0.2f), new Vector3(0.1f, 0.06f, 0.06f), accent);
                    break;
                case HairStyle.Twintails:
                    Ball(head, new Vector3(0f, 0.12f, -0.05f), new Vector3(0.86f, 0.66f, 0.84f), hair);
                    Ball(head, new Vector3(0f, 0.23f, 0.25f), new Vector3(0.56f, 0.18f, 0.18f), hair);
                    for (int sgn = -1; sgn <= 1; sgn += 2)
                    {
                        Ball(head, new Vector3(0.4f * sgn, 0.2f, -0.08f), Vector3.one * 0.12f, accent);
                        var t = Part(PrimitiveType.Capsule, head, new Vector3(0.52f * sgn, -0.12f, -0.1f), new Vector3(0.22f, 0.36f, 0.22f), hair, new Vector3(0f, 0f, -12f * sgn));
                        t.AddComponent<Sway>().Amount = 6f;
                    }
                    break;
                case HairStyle.Curly:
                    // Soft round curls all over.
                    Ball(head, new Vector3(0f, 0.12f, -0.06f), new Vector3(0.86f, 0.68f, 0.84f), hair);
                    for (int ring = 0; ring < 3; ring++)
                    {
                        int n = 9 - ring * 2;
                        float el = 15f + ring * 28f;
                        for (int i = 0; i < n; i++)
                        {
                            float az = i * 360f / n + ring * 20f;
                            Vector3 d = Quaternion.Euler(-el, az, 0f) * Vector3.forward;
                            if (d.z > 0.55f && d.y < 0.55f) continue; // keep the face clear
                            Ball(head, d * 0.42f + new Vector3(0f, 0.1f, -0.04f), Vector3.one * 0.24f, hair);
                        }
                    }
                    Ball(head, new Vector3(0f, 0.5f, -0.05f), Vector3.one * 0.26f, hair);
                    break;
            }
        }

        void BuildWeapon(WeaponKind kind, Color blade, float h, float w)
        {
            Vector3 hand = new Vector3(0.42f * w, 0.52f * h, 0.14f);
            Vector3 leftHand = new Vector3(-0.42f * w, 0.52f * h, 0.14f);
            var steel = MaterialFactory.Toon(new Color(0.74f, 0.76f, 0.8f), 0.012f, new Color(0.05f, 0.05f, 0.06f));
            var wood = new Color(0.42f, 0.28f, 0.16f);
            var tint = MaterialFactory.Toon(Color.Lerp(blade, new Color(0.6f, 0.6f, 0.62f), 0.35f), 0.01f, blade * 0.15f);
            switch (kind)
            {
                case WeaponKind.Fists:
                {
                    // Steel-plated gauntlets on both fists; the "sword" pivot is the right fist.
                    BuildSword(blade, 0.05f, hand);
                    var leather = MaterialFactory.Toon(new Color(0.35f, 0.24f, 0.16f), 0.015f);
                    Add(MeshFactory.Primitive(PrimitiveType.Sphere, SwordPivot, Vector3.zero, new Vector3(0.24f, 0.24f, 0.28f), leather));
                    Add(MeshFactory.Primitive(PrimitiveType.Cube, SwordPivot, new Vector3(0f, 0.02f, 0.1f), new Vector3(0.22f, 0.1f, 0.08f), steel));
                    Add(MeshFactory.Primitive(PrimitiveType.Cube, SwordPivot, new Vector3(0f, 0.06f, 0.14f), new Vector3(0.2f, 0.03f, 0.03f), tint));
                    var l = new GameObject("LeftFist").transform;
                    l.SetParent(Model, false);
                    l.localPosition = leftHand;
                    Add(MeshFactory.Primitive(PrimitiveType.Sphere, l, Vector3.zero, new Vector3(0.24f, 0.24f, 0.28f), leather));
                    Add(MeshFactory.Primitive(PrimitiveType.Cube, l, new Vector3(0f, 0.02f, 0.1f), new Vector3(0.22f, 0.1f, 0.08f), steel));
                    Add(MeshFactory.Primitive(PrimitiveType.Cube, l, new Vector3(0f, 0.06f, 0.14f), new Vector3(0.2f, 0.03f, 0.03f), tint));
                    break;
                }
                case WeaponKind.TwinBlades:
                {
                    BuildSword(blade, 0.8f, hand);
                    var left = new GameObject("LeftBlade").transform;
                    left.SetParent(Model, false);
                    left.localPosition = leftHand;
                    left.localRotation = Quaternion.Euler(48f, -62f, 0f);
                    SteelBlade(left, 0.8f, blade, 1f);
                    break;
                }
                case WeaponKind.Greatsword:
                    BuildSword(blade, 1.45f, hand);
                    // Broad blade.
                    Add(MeshFactory.Primitive(PrimitiveType.Cube, SwordPivot, new Vector3(0f, 0f, 0.8f), new Vector3(0.035f, 0.2f, 1.2f), steel));
                    break;
                case WeaponKind.SwordShield:
                {
                    BuildSword(blade, 1f, hand);
                    var sh = new GameObject("Shield").transform;
                    sh.SetParent(Model, false);
                    sh.localPosition = new Vector3(-0.46f * w, 0.62f * h, 0.2f);
                    sh.localRotation = Quaternion.Euler(0f, -70f, 90f);
                    var face = MaterialFactory.Toon(Color.Lerp(new Color(0.3f, 0.34f, 0.46f), blade, 0.15f), 0.015f);
                    var rim = MaterialFactory.Toon(new Color(0.6f, 0.5f, 0.28f), 0.015f);
                    Add(MeshFactory.MeshObject(MeshFactory.FacetCylinder(14), sh, Vector3.zero, new Vector3(0.68f, 0.05f, 0.68f), rim));
                    Add(MeshFactory.MeshObject(MeshFactory.FacetCylinder(14), sh, new Vector3(0f, 0.02f, 0f), new Vector3(0.6f, 0.05f, 0.6f), face));
                    Add(MeshFactory.Primitive(PrimitiveType.Sphere, sh, new Vector3(0f, 0.07f, 0f), new Vector3(0.16f, 0.08f, 0.16f), steel));
                    break;
                }
                case WeaponKind.Spear:
                {
                    BuildSword(blade, 0.05f, hand);
                    Shaft(SwordPivot, 2.1f, wood);
                    var head = MeshFactory.MeshObject(MeshFactory.FacetCone(4), SwordPivot, new Vector3(0f, 0f, 1.78f), new Vector3(0.05f, 0.34f, 0.13f), steel);
                    head.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                    Add(head);
                    Add(MeshFactory.Primitive(PrimitiveType.Cylinder, SwordPivot, new Vector3(0f, 0f, 1.74f), new Vector3(0.08f, 0.02f, 0.08f), tint));
                    var tassel = MeshFactory.MeshObject(MeshFactory.Cone(), SwordPivot, new Vector3(0f, -0.05f, 1.7f), new Vector3(0.08f, 0.18f, 0.08f), tint);
                    tassel.transform.localRotation = Quaternion.Euler(180f, 0f, 0f);
                    Add(tassel);
                    break;
                }
                case WeaponKind.Staff:
                {
                    BuildSword(blade, 0.05f, hand);
                    Shaft(SwordPivot, 1.7f, new Color(0.5f, 0.36f, 0.22f));
                    var ring = MeshFactory.MeshObject(MeshFactory.Ring(0.75f), SwordPivot, new Vector3(0f, 0f, 1.45f), Vector3.one * 0.16f, MaterialFactory.Toon(new Color(0.75f, 0.62f, 0.3f), 0.01f), false);
                    ring.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
                    Add(ring);
                    // A softly lit gem, not a lamp.
                    Add(MeshFactory.MeshObject(MeshFactory.SmoothSphere(), SwordPivot, new Vector3(0f, 0f, 1.45f), Vector3.one * 0.14f, MaterialFactory.Toon(Color.Lerp(blade, Color.white, 0.2f), 0.01f, blade * 0.3f)));
                    break;
                }
                case WeaponKind.Bow:
                {
                    BuildSword(blade, 0.05f, hand);
                    var bowT = new GameObject("Bow").transform;
                    bowT.SetParent(Model, false);
                    bowT.localPosition = new Vector3(-0.44f * w, 0.62f * h, 0.24f);
                    var limb = MaterialFactory.Toon(new Color(0.42f, 0.27f, 0.15f), 0.015f);
                    for (int i = 0; i < 9; i++)
                    {
                        float a = Mathf.Lerp(-65f, 65f, i / 8f) * Mathf.Deg2Rad;
                        Vector3 p = new Vector3(0f, Mathf.Sin(a) * 0.55f, Mathf.Cos(a) * 0.22f - 0.1f);
                        Add(MeshFactory.Primitive(PrimitiveType.Sphere, bowT, p, new Vector3(0.05f, 0.1f, 0.05f), limb));
                    }
                    Add(MeshFactory.Primitive(PrimitiveType.Cube, bowT, new Vector3(0f, 0f, -0.1f), new Vector3(0.008f, 1.0f, 0.008f), MaterialFactory.Toon(new Color(0.9f, 0.88f, 0.8f), 0f)));
                    // Quiver on the back.
                    var quiver = Part(PrimitiveType.Cylinder, Model, new Vector3(0.15f, 0.8f * h, -0.34f * w), new Vector3(0.14f, 0.25f, 0.14f), limb, new Vector3(-15f, 0f, -20f));
                    for (int i = 0; i < 3; i++)
                        Add(MeshFactory.Primitive(PrimitiveType.Cube, quiver.transform, new Vector3((i - 1) * 0.2f, 1.2f, 0f), new Vector3(0.1f, 0.6f, 0.1f), tint));
                    break;
                }
                case WeaponKind.Fans:
                {
                    BuildSword(blade, 0.05f, hand);
                    var paper = MaterialFactory.Toon(Color.Lerp(blade, new Color(0.95f, 0.93f, 0.88f), 0.55f), 0.01f);
                    var rib = MaterialFactory.Toon(new Color(0.3f, 0.2f, 0.14f), 0.01f);
                    for (int side = 0; side < 2; side++)
                    {
                        Transform parent = SwordPivot;
                        if (side == 1)
                        {
                            parent = new GameObject("LeftFan").transform;
                            parent.SetParent(Model, false);
                            parent.localPosition = leftHand;
                            parent.localRotation = Quaternion.Euler(20f, -40f, 0f);
                        }
                        var fan = MeshFactory.MeshObject(MeshFactory.Sector(120f, 0.2f), parent, new Vector3(0f, 0f, 0.05f), Vector3.one * 0.5f, paper, false);
                        fan.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                        Add(fan);
                        for (int i = 0; i < 5; i++)
                        {
                            var r = MeshFactory.Primitive(PrimitiveType.Cube, parent, Vector3.zero, new Vector3(0.012f, 0.012f, 0.5f), rib);
                            r.transform.localRotation = Quaternion.Euler(Mathf.Lerp(-55f, 55f, i / 4f), 0f, 0f);
                            r.transform.localPosition = r.transform.localRotation * new Vector3(0f, 0f, 0.25f);
                            Add(r);
                        }
                    }
                    break;
                }
                case WeaponKind.Cleavers:
                {
                    BuildSword(blade, 0.05f, hand);
                    for (int side = 0; side < 2; side++)
                    {
                        Transform parent = SwordPivot;
                        if (side == 1)
                        {
                            parent = new GameObject("LeftCleaver").transform;
                            parent.SetParent(Model, false);
                            parent.localPosition = leftHand;
                            parent.localRotation = Quaternion.Euler(48f, -62f, 0f);
                        }
                        Add(MeshFactory.Primitive(PrimitiveType.Cube, parent, new Vector3(0f, 0f, 0.05f), new Vector3(0.05f, 0.05f, 0.25f), MaterialFactory.Toon(wood, 0.012f)));
                        Add(MeshFactory.Primitive(PrimitiveType.Cube, parent, new Vector3(0f, -0.08f, 0.42f), new Vector3(0.03f, 0.24f, 0.5f), steel));
                        Add(MeshFactory.Primitive(PrimitiveType.Cube, parent, new Vector3(0f, -0.2f, 0.42f), new Vector3(0.02f, 0.02f, 0.5f), tint));
                    }
                    break;
                }
                case WeaponKind.Cane:
                    BuildSword(blade, 0.05f, hand);
                    Shaft(SwordPivot, 1.1f, wood);
                    break;
                case WeaponKind.Moon:
                {
                    BuildSword(blade, 0.05f, hand);
                    Add(MeshFactory.Primitive(PrimitiveType.Cube, SwordPivot, new Vector3(0f, 0f, 0.05f), new Vector3(0.05f, 0.05f, 0.35f), MaterialFactory.Toon(new Color(0.12f, 0.1f, 0.12f), 0.01f)));
                    var moon = MeshFactory.MeshObject(MeshFactory.Sector(170f, 0.78f), SwordPivot, new Vector3(0f, 0f, 0.55f), Vector3.one * 0.55f, steel, false);
                    moon.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                    Add(moon);
                    var edgeM = MeshFactory.MeshObject(MeshFactory.Sector(170f, 0.94f), SwordPivot, new Vector3(0.005f, 0f, 0.55f), Vector3.one * 0.56f, tint, false);
                    edgeM.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                    Add(edgeM);
                    break;
                }
                default:
                    BuildSword(blade, 1.15f, hand);
                    break;
            }
        }
    }
}
