using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>Every slayer is designed around one dominant shape, used subtly: proportions, hair volume, weapon
    /// details and victory pose.</summary>
    public enum ShapeLanguage { Circle, Square, Triangle, Diamond }

    /// <summary>
    /// Stylized hero art direction, applied to the jointed roster models after they are built. The characters stay
    /// clearly human; shape language works underneath:
    ///   • Shape language — Circle (friendly, support), Square (tank, heavy), Triangle (fast, aggressive),
    ///     Diamond (magical, elegant) — picked from role, style, weapon and manner. It only nudges proportions,
    ///     hair volume, poses and the victory, never adds literal blocks, spikes or balls.
    ///   • Face and neck — a U-shaped jaw and a human-thickness neck sloping into the shoulders (eyes, lashes and
    ///     brows are in <see cref="PremiumEyes"/>).
    ///   • Weapon — a signature guard, head or crown, a glowing element line and a pommel ribbon.
    /// The nine hand-designed slayers keep their own weapons; they get the proportions and faces.
    /// </summary>
    public partial class CharacterVisual
    {
        /// <summary>This slayer's dominant shape (set when built).</summary>
        public ShapeLanguage Shape { get; private set; }
        bool shapeSet;
        Vector3 faceC, faceR;
        bool hasFace;

        void SetFace(Vector3 hc, Vector3 hr) { faceC = hc; faceR = hr; hasFace = true; }

        public static ShapeLanguage ShapeOf(CharacterDefinition def)
        {
            if (def.role == Role.Tank || def.style == CombatStyle.Heavy || def.style == CombatStyle.Brawler) return ShapeLanguage.Square;
            if (def.role == Role.Support || def.style == CombatStyle.Healer) return ShapeLanguage.Circle;
            if (def.weapon == WeaponKind.Staff || def.weapon == WeaponKind.Fans || def.weapon == WeaponKind.Moon
                || def.motion == MotionStyle.Graceful || def.style == CombatStyle.Technical) return ShapeLanguage.Diamond;
            if (def.style == CombatStyle.Swift || def.motion == MotionStyle.Aggressive || def.motion == MotionStyle.Sly
                || def.weapon == WeaponKind.TwinBlades || def.weapon == WeaponKind.Cleavers || def.role == Role.Burst) return ShapeLanguage.Triangle;
            if (def.motion == MotionStyle.Light || def.motion == MotionStyle.Nervous) return ShapeLanguage.Circle;
            if (def.motion == MotionStyle.Stoic) return ShapeLanguage.Square;
            return ShapeLanguage.Triangle;
        }

        /// <summary>Colours pushed to be vivid and readable on a phone: more saturation, never muddy-dark.</summary>
        static Color Vivid(Color c, float satK = 1.18f, float minV = 0.24f)
        {
            float h, s, v;
            Color.RGBToHSV(c, out h, out s, out v);
            if (s > 0.08f) s = Mathf.Clamp01(s * satK);
            v = Mathf.Max(v, minV);
            var o = Color.HSVToRGB(h, s, v);
            o.a = c.a;
            return o;
        }

        /// <summary>Moves a colour's brightness away from another so neighbouring areas stay clearly separate.</summary>
        static Color SeparateFrom(Color c, Color other, float minDiff = 0.45f)
        {
            if (Diff(c, other) >= minDiff) return c;
            float lc = c.r * 0.3f + c.g * 0.59f + c.b * 0.11f, lo = other.r * 0.3f + other.g * 0.59f + other.b * 0.11f;
            return lc >= lo && lc < 0.75f ? Color.Lerp(c, Color.white, 0.3f) : Color.Lerp(c, Color.black, 0.35f);
        }

        void Stylize(CharacterDefinition def)
        {
            Shape = ShapeOf(def);
            shapeSet = true;
            if (rig == null || head == null) { deferOptimize = false; return; }
            bool handMade = IsPremium(def.id) || IsDesign(def.id);
            if (hasFace) JawAndNeck(def);
            if (!handMade)
            {
                Color elem = def.npc ? def.bladeColor : ElementChart.ColorOf(def.element);
                int rarity = def.npc ? 1 : Mathf.Clamp(def.rarity, 2, 7);
                var mA = PM(Vivid(def.accentColor), 0.01f);
                var mTrim = PM(rarity >= 4 ? new Color(0.98f, 0.8f, 0.3f) : Vivid(def.accentColor), 0.01f);
                var mGlow = PMe(elem, 0f, elem * 0.9f);
                // Shape language stays subtle (proportions, pose, hair volume); no literal blocks, spikes or balls.
                if (hasFace) HairShapes(def);
                if (!def.npc) SignatureWeapon(def, mTrim, mA, mGlow, elem);
            }
            SurfaceTextures(def);
            deferOptimize = false;
            OptimizeParts();
        }

        /// <summary>
        /// Turns on the toon shader's painted surface texture: fabric weave and folds on clothes, strands on hair,
        /// a faint warmth variation on skin. Glowing parts and the weapon (crisp) stay clean.
        /// </summary>
        void SurfaceTextures(CharacterDefinition def)
        {
            var seen = new System.Collections.Generic.HashSet<Material>();
            foreach (var r in Model.GetComponentsInChildren<Renderer>(true))
            {
                foreach (var m in r.sharedMaterials)
                {
                    if (m == null || !seen.Add(m) || !m.HasProperty("_TexAmt")) continue;
                    if (m.HasProperty("_Emission"))
                    {
                        Color em = m.GetColor("_Emission");
                        if (em.r + em.g + em.b > 0.05f) continue;
                    }
                    Color c = m.color;
                    float kind = Diff(c, def.skinTone) < 0.04f ? 2f : Diff(c, def.hairColor) < 0.12f || InHair(r.transform) ? 1f : 0f;
                    // Tiny face parts (eyes, lashes, mouth) stay flat.
                    if (InFace(r.transform)) continue;
                    m.SetFloat("_TexKind", kind);
                    m.SetFloat("_TexAmt", 1f);
                }
            }
        }

        bool InHair(Transform t)
        {
            for (var p = t; p != null && p != head; p = p.parent)
                if (p.name.StartsWith("Hair")) return true;
            return false;
        }

        bool InFace(Transform t)
        {
            for (var p = t; p != null && p != head; p = p.parent)
                if (p.name == "Face" || p.GetComponent<EyeLid>() != null) return true;
            return false;
        }

        // ------------------------------------------------------------------ Helpers

        /// <summary>A cone from its base toward dir (w = base width, len = length, flat squashes it into a blade).</summary>
        Transform Spike(Transform parent, Vector3 pos, Vector3 dir, float w, float len, Material m, float flat = 1f, float sway = 0f)
        {
            var j = J("Spike", parent, pos);
            j.localRotation = Quaternion.FromToRotation(Vector3.up, dir.normalized);
            ConePart(j, Vector3.zero, new Vector3(w, len, w * flat), m, Vector3.zero);
            if (sway > 0f)
            {
                var s = j.gameObject.AddComponent<Sway>();
                s.Amount = sway;
                s.Speed = 1f + (pos.x + pos.z) * 0.7f;
            }
            return j;
        }

        /// <summary>Point on the head's ellipsoid: yaw 0 = front, 90 = the right side; pitch 90 = the top.</summary>
        Vector3 OnHead(float yawDeg, float pitchDeg, float push = 1f)
        {
            float yaw = yawDeg * Mathf.Deg2Rad, pit = pitchDeg * Mathf.Deg2Rad;
            var d = new Vector3(Mathf.Sin(yaw) * Mathf.Cos(pit), Mathf.Sin(pit), Mathf.Cos(yaw) * Mathf.Cos(pit));
            return faceC + Vector3.Scale(d, faceR) * push;
        }

        Vector3 HeadNormal(Vector3 p)
        {
            Vector3 d = p - faceC;
            return new Vector3(d.x / (faceR.x * faceR.x), d.y / (faceR.y * faceR.y), d.z / (faceR.z * faceR.z)).normalized;
        }

        // ------------------------------------------------------------------ Face

        /// <summary>
        /// A "U"-shaped head: a jaw that keeps the cheeks wide lower down and rounds off at the bottom (no pointed
        /// chin), set back a little so the mouth stays in front of it, and a real neck under it.
        /// </summary>
        void JawAndNeck(CharacterDefinition def)
        {
            var skin = PM(def.skinTone);
            Ball(head, faceC + new Vector3(0f, -faceR.y * 0.35f, 0f), new Vector3(faceR.x * 1.92f, faceR.y * 1.24f, faceR.z * 1.64f), skin);
            Part(PrimitiveType.Capsule, head, new Vector3(0f, -0.03f, -0.015f), new Vector3(0.155f, 0.1f, 0.14f), skin);
            Ball(head, new Vector3(0f, -0.1f, -0.02f), new Vector3(0.22f, 0.09f, 0.17f), skin);
        }

        // ------------------------------------------------------------------ Hair

        void HairShapes(CharacterDefinition def)
        {
            Color hc = Vivid(def.hairColor, 1.12f, 0.12f);
            var hair = PM(hc, 0.014f);
            var hairLit = PM(Color.Lerp(hc, Color.white, 0.18f), 0.012f);
            // 1) Push the existing hair bigger (bolder shape), around the head's centre.
            float hk = 1.05f;
            Vector3 hs = Shape == ShapeLanguage.Diamond || Shape == ShapeLanguage.Triangle ? new Vector3(hk, hk * 1.03f, hk) : Vector3.one * hk;
            // (Sculpted hair groups and the classic hair root are all named "Hair…" and not merged yet.)
            for (int i = 0; i < head.childCount; i++)
            {
                var c = head.GetChild(i);
                if (!c.name.StartsWith("Hair")) continue;
                c.localPosition = faceC + Vector3.Scale(c.localPosition - faceC, hs);
                c.localScale = Vector3.Scale(c.localScale, hs);
            }
        }

        // ------------------------------------------------------------------ Weapon

        void SignatureWeapon(CharacterDefinition def, Material trim, Material accent, Material glow, Color elem)
        {
            var sp = SwordPivot;
            if (sp == null) return;
            switch (def.weapon)
            {
                case WeaponKind.Katana: BladeSignature(sp, 0.12f, 0.2f, 1.05f, 0.06f, 0.018f, -0.2f, trim, accent, glow); break;
                case WeaponKind.SwordShield:
                    BladeSignature(sp, 0.12f, 0.2f, 0.9f, 0.066f, 0.018f, -0.2f, trim, accent, glow);
                    ShieldEmblem(FindPart(rig.lower[1], "Shield"), trim, glow);
                    break;
                case WeaponKind.Greatsword: BladeSignature(sp, 0.2f, 0.3f, 1.4f, 0.24f, 0.046f, -0.28f, trim, accent, glow); break;
                case WeaponKind.TwinBlades:
                {
                    var kd = FindPart(sp, "Kodachi");
                    BladeSignature(kd != null ? kd : sp, 0.11f, 0.18f, 0.7f, 0.058f, 0.018f, -0.15f, trim, accent, glow);
                    break;
                }
                case WeaponKind.Spear: PolearmSignature(sp, 1.3f, trim, glow); break;
                case WeaponKind.Staff: StaffCrown(sp, new Vector3(0f, 0f, 1.42f), trim, glow, elem); break;
                case WeaponKind.Bow: BowSignature(FindPart(rig.hand[1], "Bow"), trim, glow); break;
                case WeaponKind.Fists:
                    for (int i = 0; i < 2; i++) KnuckleShape(FindPart(rig.hand[i], "Gauntlet"), trim, glow);
                    break;
                default:
                    // Fans, cleavers, crescent, cane: a glowing element gem and a streaming ribbon.
                    Ball(sp, new Vector3(0f, 0f, 0.02f), Vector3.one * 0.07f, glow);
                    Ribbon(sp, new Vector3(0f, 0f, -0.14f), accent);
                    break;
            }
        }

        static Transform FindPart(Transform root, string name)
        {
            if (root == null) return null;
            if (root.name == name) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                var f = FindPart(root.GetChild(i), name);
                if (f != null) return f;
            }
            return null;
        }

        /// <summary>Guard in the slayer's shape, a glowing element line down the blade and a ribbon on the pommel.</summary>
        void BladeSignature(Transform k, float guardZ, float bladeFrom, float bladeTo, float bladeH, float thick, float pommelZ, Material trim, Material accent, Material glow)
        {
            float mid = (bladeFrom + bladeTo) * 0.5f, len = bladeTo - bladeFrom;
            switch (Shape)
            {
                case ShapeLanguage.Triangle:
                    for (int s = -1; s <= 1; s += 2)
                        Spike(k, new Vector3(0f, s * 0.03f, guardZ), new Vector3(0f, s, -0.7f), 0.06f, 0.22f, trim, 0.35f);
                    for (int i = 0; i < 3; i++)
                        Spike(k, new Vector3(0f, bladeH * 0.45f, bladeFrom + len * (0.25f + i * 0.2f)), new Vector3(0f, 1f, -0.5f), 0.045f, 0.08f, trim, 0.3f);
                    break;
                case ShapeLanguage.Square:
                    Part(PrimitiveType.Cube, k, new Vector3(0f, 0f, guardZ), new Vector3(0.09f, bladeH * 2.6f + 0.06f, 0.08f), trim);
                    Part(PrimitiveType.Cube, k, new Vector3(0f, bladeH * 0.5f + 0.012f, mid - len * 0.08f), new Vector3(0.034f, 0.03f, len * 0.75f), trim);
                    break;
                case ShapeLanguage.Circle:
                {
                    var disc = MeshFactory.MeshObject(MeshFactory.FacetCylinder(18), k, new Vector3(0f, 0f, guardZ), new Vector3(0.24f, 0.024f, 0.24f), trim);
                    disc.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                    Add(disc);
                    Ball(k, new Vector3(0f, 0f, pommelZ), Vector3.one * 0.09f, trim);
                    break;
                }
                default:
                    Part(PrimitiveType.Cube, k, new Vector3(0f, 0f, guardZ), new Vector3(0.03f, 0.15f, 0.15f), trim, new Vector3(45f, 0f, 0f));
                    Ball(k, new Vector3(0f, 0f, guardZ), new Vector3(0.05f, 0.06f, 0.06f), glow);
                    break;
            }
            for (int s = -1; s <= 1; s += 2)
                Part(PrimitiveType.Cube, k, new Vector3(s * (thick * 0.5f + 0.002f), 0f, mid), new Vector3(0.004f, Mathf.Max(0.012f, bladeH * 0.2f), len * 0.62f), glow);
            Ribbon(k, new Vector3(0f, 0f, pommelZ - 0.02f), accent);
        }

        void Ribbon(Transform parent, Vector3 pos, Material m)
        {
            var j = J("Ribbon", parent, pos);
            var sw = j.gameObject.AddComponent<Sway>();
            sw.Amount = 14f; sw.Speed = 1.8f;
            for (int s = -1; s <= 1; s += 2)
                Part(PrimitiveType.Cube, j, new Vector3(s * 0.012f, -0.1f, -0.02f), new Vector3(0.01f, 0.2f, 0.045f), m, new Vector3(0f, 0f, s * 12f));
        }

        void PolearmSignature(Transform sp, float z, Material trim, Material glow)
        {
            switch (Shape)
            {
                case ShapeLanguage.Triangle:
                    for (int s = -1; s <= 1; s += 2) Spike(sp, new Vector3(0f, s * 0.04f, z), new Vector3(0f, s, 0.45f), 0.07f, 0.26f, trim, 0.3f);
                    break;
                case ShapeLanguage.Square:
                    Part(PrimitiveType.Cube, sp, new Vector3(0f, 0.13f, z - 0.03f), new Vector3(0.022f, 0.22f, 0.22f), trim);
                    break;
                case ShapeLanguage.Circle:
                    for (int s = -1; s <= 1; s += 2) Ball(sp, new Vector3(0f, s * 0.1f, z - 0.02f), new Vector3(0.03f, 0.1f, 0.12f), trim);
                    break;
                default:
                    Part(PrimitiveType.Cube, sp, new Vector3(0f, 0f, z - 0.02f), new Vector3(0.03f, 0.2f, 0.2f), trim, new Vector3(45f, 0f, 0f));
                    break;
            }
            Ball(sp, new Vector3(0f, 0f, z - 0.02f), Vector3.one * 0.08f, glow);
        }

        void StaffCrown(Transform sp, Vector3 oc, Material trim, Material glow, Color elem)
        {
            int n = Shape == ShapeLanguage.Square ? 4 : Shape == ShapeLanguage.Circle ? 6 : 3;
            for (int k = 0; k < n; k++)
            {
                float a = k * Mathf.PI * 2f / n;
                Vector3 o = new Vector3(0f, Mathf.Cos(a), Mathf.Sin(a));
                Vector3 p = oc + o * 0.13f - Vector3.forward * 0.1f;
                if (Shape == ShapeLanguage.Square) Part(PrimitiveType.Cube, sp, p + Vector3.forward * 0.08f, new Vector3(0.05f, 0.05f, 0.22f), trim);
                else if (Shape == ShapeLanguage.Circle) Ball(sp, oc + o * 0.2f, Vector3.one * 0.07f, trim);
                else Spike(sp, p, o * 0.5f + Vector3.forward, 0.07f, Shape == ShapeLanguage.Triangle ? 0.34f : 0.28f, trim, 0.45f);
            }
            if (Shape == ShapeLanguage.Diamond)
            {
                var orbit = J("Shards", sp, oc);
                orbit.gameObject.AddComponent<Spinner>().DegreesPerSecond = new Vector3(0f, 0f, 90f);
                for (int k = 0; k < 3; k++)
                {
                    float a = k * Mathf.PI * 2f / 3f;
                    Part(PrimitiveType.Cube, orbit, new Vector3(Mathf.Cos(a) * 0.3f, Mathf.Sin(a) * 0.3f, 0f), new Vector3(0.05f, 0.05f, 0.05f), glow, new Vector3(45f, 0f, 45f));
                }
            }
        }

        void BowSignature(Transform bow, Material trim, Material glow)
        {
            if (bow == null) return;
            for (int s = -1; s <= 1; s += 2)
            {
                if (Shape == ShapeLanguage.Circle) Ball(bow, new Vector3(0f, s * 0.62f, -0.08f), Vector3.one * 0.1f, trim);
                else if (Shape == ShapeLanguage.Square) Part(PrimitiveType.Cube, bow, new Vector3(0f, s * 0.6f, -0.06f), new Vector3(0.06f, 0.12f, 0.12f), trim);
                else Spike(bow, new Vector3(0f, s * 0.58f, -0.08f), new Vector3(0f, s, 0.55f), 0.07f, 0.24f, trim, 0.35f);
            }
            Ball(bow, new Vector3(0f, 0f, 0.02f), Vector3.one * 0.075f, glow);
        }

        void ShieldEmblem(Transform sh, Material trim, Material glow)
        {
            if (sh == null) return;
            switch (Shape)
            {
                case ShapeLanguage.Triangle:
                    for (int k = 0; k < 3; k++)
                    {
                        float a = (90f + k * 120f) * Mathf.Deg2Rad;
                        Spike(sh, new Vector3(Mathf.Cos(a) * 0.12f, 0.05f, Mathf.Sin(a) * 0.12f), new Vector3(Mathf.Cos(a), 0.3f, Mathf.Sin(a)), 0.08f, 0.2f, trim, 0.3f);
                    }
                    break;
                case ShapeLanguage.Square:
                    Part(PrimitiveType.Cube, sh, new Vector3(0f, 0.06f, 0f), new Vector3(0.22f, 0.05f, 0.22f), trim);
                    break;
                default:
                    Part(PrimitiveType.Cube, sh, new Vector3(0f, 0.06f, 0f), new Vector3(0.2f, 0.04f, 0.2f), trim, new Vector3(0f, 45f, 0f));
                    break;
            }
            Ball(sh, new Vector3(0f, 0.09f, 0f), new Vector3(0.09f, 0.05f, 0.09f), glow);
        }

        void KnuckleShape(Transform g, Material trim, Material glow)
        {
            if (g == null) return;
            switch (Shape)
            {
                case ShapeLanguage.Triangle:
                    for (int k = 0; k < 3; k++) Spike(g, new Vector3(-0.03f + k * 0.03f, -0.1f, 0.06f), new Vector3(0f, -0.3f, 1f), 0.035f, 0.08f, trim);
                    break;
                case ShapeLanguage.Circle:
                    Ball(g, new Vector3(0f, -0.1f, 0.05f), new Vector3(0.16f, 0.1f, 0.1f), trim);
                    break;
                default:
                    Part(PrimitiveType.Cube, g, new Vector3(0f, -0.1f, 0.06f), new Vector3(0.15f, 0.08f, 0.07f), trim);
                    break;
            }
            Ball(g, new Vector3(0f, 0.02f, 0.05f), Vector3.one * 0.045f, glow);
        }
    }
}
