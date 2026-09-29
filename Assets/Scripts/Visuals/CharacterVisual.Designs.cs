using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// The roster design test: six original slayers built to the Character Design Bible
    /// (docs/CHARACTER_DESIGN_BIBLE.md). Each has its own proportions, head shape, face, sculpted hairstyle, outfit
    /// silhouette, weapon, palette, movement personality and showcase pose, so they read apart even as silhouettes:
    ///
    ///   Tobi   fast melee   short, long legs, swept-back hair, headband tails, sleeveless jacket, short straight blade
    ///   Bunta  heavy        towering, huge shoulders, small head, top-knot, one great pauldron, war hammer
    ///   Sayo   ranged       tall and slim, long legs, high ponytail, half cape, quiver, tall asymmetric longbow
    ///   Nene   support      tiny and round, big head, bob and flower pin, huge sleeves, obi bow, lantern staff
    ///   Nagi   assassin     small, hooded and masked, close wraps, twin sickles
    ///   Seiran elemental    tallest, long braid, high collar, long cape, water glaive, orb in the free hand
    /// </summary>
    public partial class CharacterVisual
    {
        public static bool IsDesign(string id)
        {
            switch (id)
            {
                case "tobi_kazami": case "bunta_okuyama": case "sayo_mikage": case "nene_hanabusa": case "nagi_kurokiri": case "seiran_mizuchi":
                    return true;
            }
            return false;
        }

        /// <summary>Body measurements for one design (rig space before the overall scale).</summary>
        class Proportions
        {
            public float scale = 1f, hipY = 0.6f, hipW = 0.085f, legA = 0.27f, legB = 0.27f, ankle = 0.08f, armA = 0.2f, armB = 0.19f;
            public Vector3 shoulder = new Vector3(0.215f, 0.29f, -0.01f), neck = new Vector3(0f, 0.36f, 0f);
            public Vector3 hc = new Vector3(0f, 0.33f, 0.02f), hr = new Vector3(0.37f, 0.36f, 0.35f);
            public float chestR = 0.145f, torsoW = 1.05f, torsoD = 0.82f, hipR = 0.15f, thighR = 0.075f, shinR = 0.052f, armR = 0.058f, handS = 1f, footS = 1f;
            public Vector3 TS { get { return new Vector3(torsoW, neck.y / 0.36f, torsoD); } }
        }

        /// <summary>Face recipe: eye shape, brows, nose, mouth and one optional mark.</summary>
        class FaceSpec
        {
            public float eyeX = 0.125f, eyeY = -0.035f, eyeW = 0.112f, eyeH = 0.13f, lid = 0.06f, tilt = 2f, brow = 4f, browThick = 0.02f;
            public bool lashes;
            public Color iris = new Color(0.3f, 0.5f, 0.9f), brows = new Color(0.1f, 0.08f, 0.08f);
            public string nose = "dot", mouth = "smile", mark = "";
        }

        void BuildDesign(CharacterDefinition def)
        {
            Motion = def.motion;
            Weapon = def.weapon;
            Color elem = ElementChart.ColorOf(def.element);
            pRim = Color.Lerp(elem, Color.white, 0.35f);
            pShadow = Color.Lerp(new Color(0.55f, 0.5f, 0.72f), Color.Lerp(elem, new Color(0.4f, 0.4f, 0.5f), 0.6f), 0.35f);
            switch (def.id)
            {
                case "tobi_kazami": BuildTobi(def); break;
                case "bunta_okuyama": BuildBunta(def); break;
                case "sayo_mikage": BuildSayo(def); break;
                case "nene_hanabusa": BuildNene(def); break;
                case "nagi_kurokiri": BuildNagi(def); break;
                default: BuildSeiran(def); break;
            }
            OptimizeParts();
            if (rig != null) rig.Solve(0f);
        }

        // ------------------------------------------------------------------ Shared kit

        PremiumRig DRig(Proportions b)
        {
            var r = NewRig(b.hipY, b.hipW, b.legA, b.legB, b.ankle, b.shoulder, b.armA, b.armB, b.neck);
            Model.localScale = Vector3.one * b.scale;
            r.stance = b.hipW + 0.015f;
            r.restHandR = new Vector3(b.shoulder.x + 0.08f, 0.03f, 0.08f);
            r.restHandL = new Vector3(-b.shoulder.x - 0.08f, 0.03f, 0.06f);
            r.hasPose = true;
            return r;
        }

        /// <summary>Chest shell (the under layer), scaled to the build.</summary>
        void DChest(PremiumRig r, Proportions b, Material m, string key)
        {
            float c = b.chestR;
            Shell(r.torso, key, new[] { new Vector2(0f, -0.04f), new Vector2(c * 0.8f, -0.03f), new Vector2(c * 0.84f, 0.05f), new Vector2(c * 0.96f, 0.15f),
                new Vector2(c, 0.23f), new Vector2(c * 0.87f, 0.3f), new Vector2(c * 0.5f, 0.345f), new Vector2(0f, 0.355f) }, Vector3.zero, b.TS, m);
        }

        /// <summary>Legs from the hip joint to the ankle: trousers, optional shin wraps or greaves.</summary>
        void DLegs(PremiumRig r, Proportions b, Material pants, Material shin, string style)
        {
            for (int i = 0; i < 2; i++)
            {
                Taper(r.thigh[i], Vector3.zero, b.legA - 0.03f, b.thighR, b.thighR * 1.15f, pants);
                Ball(r.shin[i], Vector3.zero, Vector3.one * b.thighR * 1.3f, pants);
                Taper(r.shin[i], Vector3.zero, b.legB - 0.02f, b.shinR, b.shinR * 0.82f, style == "bare" ? shin : pants);
                if (style == "wraps")
                    for (int k = 0; k < 4; k++)
                        Band(r.shin[i], new Vector3(0f, -(b.legB * 0.26f) - k * b.legB * 0.16f, 0f), b.shinR - k * 0.003f, 0.026f, 0.009f, shin, null, new Vector3(0f, 0f, k % 2 == 0 ? 12f : -12f));
                else if (style == "greaves")
                {
                    Taper(r.shin[i], new Vector3(0f, -0.02f, 0.012f), b.legB * 0.72f, b.shinR * 1.1f, b.shinR * 0.95f, shin, new Vector3(1f, 1f, 1.05f));
                }
                else if (style == "boots")
                    Taper(r.shin[i], new Vector3(0f, -b.legB * 0.35f, 0f), b.legB * 0.62f, b.shinR * 1.12f, b.shinR * 1.0f, shin);
            }
        }

        /// <summary>Feet: "boot", "tabi" (split toe), "geta" (wooden sandal) or "heavy".</summary>
        void DFeet(PremiumRig r, Proportions b, Material boot, Material sole, string style)
        {
            float fs = b.footS;
            for (int i = 0; i < 2; i++)
            {
                var f = r.foot[i];
                if (style == "geta")
                {
                    Ball(f, new Vector3(0f, -0.03f, 0.04f) * fs, new Vector3(0.085f, 0.07f, 0.17f) * fs, boot);
                    Part(PrimitiveType.Cube, f, new Vector3(0f, -0.07f, 0.04f) * fs, new Vector3(0.1f, 0.022f, 0.22f) * fs, sole);
                    for (int k = -1; k <= 1; k += 2) Part(PrimitiveType.Cube, f, new Vector3(0f, -0.09f, 0.04f + k * 0.06f) * fs, new Vector3(0.09f, 0.03f, 0.02f) * fs, sole);
                    continue;
                }
                float w = style == "heavy" ? 1.3f : 1f;
                Ball(f, new Vector3(0f, -0.035f, 0.045f) * fs, new Vector3(0.11f * w, 0.085f, 0.2f * w) * fs, boot);
                Ball(f, new Vector3(0f, -0.045f, 0.11f * w) * fs, new Vector3(0.1f * w, 0.065f, 0.1f * w) * fs, boot);
                Part(PrimitiveType.Cube, f, new Vector3(0f, -b.ankle + 0.012f, 0.05f) * fs, new Vector3(0.106f * w, 0.022f, 0.23f * w) * fs, sole);
                if (style == "tabi") Part(PrimitiveType.Cube, f, new Vector3(0f, -0.042f, 0.158f) * fs, new Vector3(0.006f, 0.05f, 0.04f) * fs, sole);
                if (style == "heavy") Band(f, new Vector3(0f, 0.01f, 0f), 0.06f * fs, 0.05f, 0.012f, sole);
            }
        }

        /// <summary>Arms: "bare" (skin, with a wrist wrap), "fitted" (sleeve to the wrist), "wide" (loose sleeve) or "bell" (huge robe sleeve).</summary>
        void DArms(PremiumRig r, Proportions b, string style, Material sleeve, Material skin, Material cuff, Material glove, bool fists = true)
        {
            for (int i = 0; i < 2; i++)
            {
                int side = i == 0 ? 1 : -1;
                float ua = b.armR;
                switch (style)
                {
                    case "bare":
                        Ball(r.upper[i], new Vector3(0f, -0.01f, 0f), Vector3.one * ua * 2.1f, skin);
                        Taper(r.upper[i], Vector3.zero, b.armA - 0.01f, ua, ua * 0.9f, skin);
                        Ball(r.lower[i], Vector3.zero, Vector3.one * ua * 1.6f, skin);
                        Taper(r.lower[i], Vector3.zero, b.armB - 0.02f, ua * 0.85f, ua * 0.72f, skin);
                        Band(r.lower[i], new Vector3(0f, -b.armB * 0.75f, 0f), ua * 0.75f, 0.05f, 0.01f, cuff);
                        break;
                    case "fitted":
                        Ball(r.upper[i], new Vector3(0f, -0.01f, 0f), Vector3.one * ua * 2.2f, sleeve);
                        Taper(r.upper[i], Vector3.zero, b.armA - 0.01f, ua * 1.02f, ua * 0.95f, sleeve);
                        Ball(r.lower[i], Vector3.zero, Vector3.one * ua * 1.6f, sleeve);
                        Taper(r.lower[i], Vector3.zero, b.armB - 0.02f, ua * 0.86f, ua * 0.74f, sleeve);
                        Band(r.lower[i], new Vector3(0f, -b.armB * 0.8f, 0f), ua * 0.76f, 0.022f, 0.008f, cuff);
                        break;
                    case "wide":
                        Ball(r.upper[i], new Vector3(0f, -0.01f, 0f), Vector3.one * ua * 2.3f, sleeve);
                        Taper(r.upper[i], Vector3.zero, b.armA, ua + 0.012f, ua + 0.03f, sleeve);
                        Band(r.upper[i], new Vector3(0f, -b.armA + 0.01f, 0f), ua + 0.028f, 0.026f, 0.009f, cuff);
                        Ball(r.lower[i], Vector3.zero, Vector3.one * ua * 1.5f, skin);
                        Taper(r.lower[i], Vector3.zero, b.armB - 0.02f, ua * 0.8f, ua * 0.68f, skin);
                        break;
                    default: // bell
                        Ball(r.upper[i], new Vector3(0f, -0.01f, 0f), Vector3.one * ua * 2.1f, sleeve);
                        Taper(r.upper[i], Vector3.zero, b.armA - 0.01f, ua, ua * 1.05f, sleeve);
                        Shell(r.lower[i], "ds_bell" + b.armB.ToString("F2"), new[] { new Vector2(0.15f, -b.armB - 0.02f), new Vector2(0.155f, -b.armB), new Vector2(0.11f, -b.armB * 0.45f),
                            new Vector2(0.065f, 0f), new Vector2(0.05f, 0.03f) }, Vector3.zero, Vector3.one, sleeve);
                        Band(r.lower[i], new Vector3(0f, -b.armB + 0.005f, 0f), 0.153f, 0.026f, 0.009f, cuff);
                        Taper(r.lower[i], Vector3.zero, b.armB - 0.02f, ua * 0.62f, ua * 0.56f, skin);
                        break;
                }
                PremiumHand(r.hand[i], glove, glove, side, fists, b.handS, null);
            }
        }

        /// <summary>Neck, head (with a shape of its own) and ears; optional square jaw.</summary>
        void DHead(Proportions b, Material skin, bool squareJaw)
        {
            Ball(head, new Vector3(0f, 0.04f, 0f), new Vector3(0.1f, 0.16f, 0.1f) * (b.hr.x / 0.37f) * 1.1f, skin);
            Ball(head, b.hc, b.hr * 2f, skin); SetFace(b.hc, b.hr);
            if (squareJaw) Ball(head, b.hc + new Vector3(0f, -b.hr.y * 0.42f, b.hr.z * 0.12f), new Vector3(b.hr.x * 1.7f, b.hr.y * 0.9f, b.hr.z * 1.6f), skin);
            for (int s = -1; s <= 1; s += 2) Ball(head, b.hc + new Vector3(s * (b.hr.x - 0.01f), -0.03f, -0.01f), new Vector3(0.07f, 0.12f, 0.08f) * (b.hr.y / 0.36f), skin);
        }

        void DFace(Proportions b, FaceSpec f, Material skin, Color skinC)
        {
            PremiumEyes(b.hc, b.hr, f.eyeX, f.eyeY, f.eyeW, f.eyeH, f.iris, Color.Lerp(f.iris, Color.white, 0.45f), f.lid, f.tilt, f.lashes, f.brow, f.browThick, f.brows, skin);
            var noseM = PM(Color.Lerp(skinC, new Color(0.8f, 0.5f, 0.45f), 0.4f), 0f);
            if (f.nose == "dot") Ball(OnFace(b.hc, b.hr, 0f, -0.09f), Vector3.zero, new Vector3(0.022f, 0.016f, 0.012f), noseM);
            else if (f.nose == "broad") Ball(OnFace(b.hc, b.hr, 0f, -0.08f), Vector3.zero, new Vector3(0.06f, 0.035f, 0.035f), PM(Color.Lerp(skinC, Color.black, 0.08f), 0.006f));
            else if (f.nose == "line") Part(PrimitiveType.Capsule, OnFace(b.hc, b.hr, 0.01f, -0.075f), Vector3.zero, new Vector3(0.008f, 0.02f, 0.008f), noseM, new Vector3(0f, 0f, 15f));
            var ink = PM(new Color(0.3f, 0.1f, 0.1f), 0f);
            var teeth = PM(new Color(0.99f, 0.98f, 0.96f), 0f);
            var mo = OnFace(b.hc, b.hr, 0f, -0.17f);
            switch (f.mouth)
            {
                case "grin":
                    Ball(mo, new Vector3(0f, 0f, -0.004f), new Vector3(0.1f, 0.045f, 0.02f), ink);
                    Ball(mo, new Vector3(0f, 0.01f, 0.001f), new Vector3(0.086f, 0.016f, 0.016f), teeth);
                    ConePart(mo, new Vector3(0.025f, -0.002f, 0.004f), new Vector3(0.012f, 0.018f, 0.01f), teeth, new Vector3(180f, 0f, 0f));
                    break;
                case "flat":
                    Part(PrimitiveType.Capsule, mo, Vector3.zero, new Vector3(0.012f, 0.035f, 0.01f), ink, new Vector3(0f, 0f, 90f));
                    break;
                case "smirk":
                    Part(PrimitiveType.Capsule, mo, new Vector3(0.012f, 0.004f, 0f), new Vector3(0.012f, 0.035f, 0.01f), ink, new Vector3(0f, 0f, 76f));
                    break;
                case "o":
                    Ball(mo, new Vector3(0f, 0.005f, -0.003f), new Vector3(0.05f, 0.045f, 0.02f), ink);
                    Ball(mo, new Vector3(0f, -0.006f, 0f), new Vector3(0.03f, 0.018f, 0.016f), PM(new Color(0.95f, 0.5f, 0.5f), 0f));
                    break;
                case "none":
                    break;
                default: // soft smile
                    for (int s = -1; s <= 1; s += 2)
                        Part(PrimitiveType.Capsule, mo, new Vector3(s * 0.016f, 0.004f, 0f), new Vector3(0.011f, 0.022f, 0.01f), ink, new Vector3(0f, 0f, 90f - s * 22f));
                    break;
            }
            switch (f.mark)
            {
                case "bandage":
                    Part(PrimitiveType.Cube, OnFace(b.hc, b.hr, 0f, -0.07f, 0.004f), Vector3.zero, new Vector3(0.09f, 0.025f, 0.008f), PM(new Color(0.96f, 0.92f, 0.84f), 0.004f), new Vector3(0f, 0f, -8f));
                    break;
                case "scar":
                    Part(PrimitiveType.Capsule, OnFace(b.hc, b.hr, -0.15f, -0.1f), Vector3.zero, new Vector3(0.01f, 0.05f, 0.008f), PM(Color.Lerp(skinC, new Color(0.6f, 0.3f, 0.3f), 0.5f), 0f), new Vector3(0f, 0f, 30f));
                    break;
                case "beauty":
                    Ball(OnFace(b.hc, b.hr, 0.19f, -0.11f), Vector3.zero, Vector3.one * 0.014f, PM(new Color(0.2f, 0.12f, 0.1f), 0f));
                    break;
                case "blush":
                    Blush(b.hc, b.hr, 0.45f);
                    break;
            }
        }

        /// <summary>Hanging cloth strips (scarf ends, headband tails, cape panels) that sway.</summary>
        void DStrips(Transform parent, Vector3 pos, Vector3 euler, int count, float spacing, float len, float width, int segs, Material m, Material tip)
        {
            var root = J("Strips", parent, pos);
            root.localRotation = Quaternion.Euler(euler);
            for (int k = 0; k < count; k++)
            {
                Transform prev = J("Strip" + k, root, new Vector3((k - (count - 1) * 0.5f) * spacing, 0f, 0f));
                prev.localRotation = Quaternion.Euler(6f + k * 3f, 0f, (k - (count - 1) * 0.5f) * 5f);
                float sl = len / segs;
                for (int sg = 0; sg < segs; sg++)
                {
                    var p = Part(PrimitiveType.Cube, prev, new Vector3(0f, -sl * 0.5f, 0f), new Vector3(width * (1f - sg * 0.12f), sl * 1.04f, 0.016f), m);
                    if (sg == segs - 1 && tip != null) Part(PrimitiveType.Cube, p.transform, new Vector3(0f, -0.42f, 0f), new Vector3(1.04f, 0.14f, 1.2f), tip);
                    var sw = prev.gameObject.AddComponent<Sway>();
                    sw.Amount = 4f + sg * 4f;
                    sw.Speed = 1.2f + k * 0.2f;
                    var next = J("Seg", prev, new Vector3(0f, -sl, 0f));
                    next.localRotation = Quaternion.Euler(8f, 0f, 0f);
                    prev = next;
                }
            }
        }

        /// <summary>A hairband ring around the head at a given height (headbands, circlets).</summary>
        void DHeadRing(Proportions b, float y, float extra, float h, Material m)
        {
            float k = Mathf.Sqrt(Mathf.Max(0.05f, 1f - (y * y) / (b.hr.y * b.hr.y)));
            Band(head, b.hc + new Vector3(0f, y, 0f), b.hr.x * k + extra, h, 0.012f, m, new Vector3(1f, 1f, b.hr.z / b.hr.x));
        }

        void DRarity(PremiumRig r, Proportions b, int rarity, Color elem, Material trim, float chestZ)
        {
            var glow = PMe(elem, 0f, elem * 0.9f);
            if (rarity >= 5)
            {
                Ball(r.torso, new Vector3(0f, 0.15f, chestZ + 0.02f), new Vector3(0.055f, 0.055f, 0.022f), glow);
                Ball(r.torso, new Vector3(0f, 0.15f, chestZ + 0.01f), new Vector3(0.08f, 0.08f, 0.018f), trim);
            }
            if (rarity >= 6)
            {
                var halo = J("Halo", head, b.hc + new Vector3(0f, 0.1f, -b.hr.z - 0.1f));
                halo.localRotation = Quaternion.Euler(90f, 0f, 0f);
                var spin = J("HaloSpin", halo, Vector3.zero);
                MeshFactory.MeshObject(MeshFactory.Ring(0.9f), spin, Vector3.zero, Vector3.one * 0.34f, MaterialFactory.Additive(new Color(elem.r, elem.g, elem.b, 0.7f)), false);
                spin.gameObject.AddComponent<Spinner>().DegreesPerSecond = new Vector3(0f, 25f, 0f);
            }
        }

        // ------------------------------------------------------------------ 1. Tobi — fast melee (wind)

        void BuildTobi(CharacterDefinition def)
        {
            var b = new Proportions { scale = 0.92f, hipY = 0.62f, hipW = 0.075f, legA = 0.29f, legB = 0.29f, ankle = 0.078f, armA = 0.19f, armB = 0.18f,
                shoulder = new Vector3(0.195f, 0.27f, -0.01f), neck = new Vector3(0f, 0.33f, 0f), hc = new Vector3(0f, 0.31f, 0.02f), hr = new Vector3(0.36f, 0.35f, 0.34f),
                chestR = 0.128f, torsoW = 1f, torsoD = 0.8f, thighR = 0.068f, shinR = 0.047f, armR = 0.05f };
            Color lime = new Color(0.62f, 0.86f, 0.2f), slate = new Color(0.2f, 0.25f, 0.3f), orange = new Color(1f, 0.52f, 0.12f);
            var mLime = PM(lime); var mSlate = PM(slate); var mOrange = PM(orange, 0.01f); var mSkin = PM(def.skinTone);
            var mWrap = PM(new Color(0.9f, 0.88f, 0.82f), 0.01f); var mSole = PM(new Color(0.08f, 0.08f, 0.08f), 0.01f); var mDark = PM(new Color(0.12f, 0.14f, 0.17f));
            var r = DRig(b);
            // Fast: short quick steps, high knees, forward lean, a light bounce at rest.
            r.crouch = 0.03f; r.lean = 9f; r.tempo = 14f; r.stride = 0.2f; r.lift = 0.16f; r.idleBounce = 0.009f; r.idleBounceFreq = 2.6f;
            r.twistGain = 0.35f; r.swingDip = 0.05f; r.swingLunge = 14f;
            r.showPose = PremiumRig.ShowPose.WeaponShoulder;
            var T = r.torso;
            // Sleeveless lime jacket over a slate undershirt, open collar, orange sash.
            DChest(r, b, mSlate, "tb_chest");
            Shell(T, "tb_jacket", new[] { new Vector2(0.14f, 0.02f), new Vector2(0.145f, 0.1f), new Vector2(0.15f, 0.2f), new Vector2(0.14f, 0.29f),
                new Vector2(0.09f, 0.34f), new Vector2(0.06f, 0.36f) }, Vector3.zero, new Vector3(1.02f, b.TS.y, 0.84f), mLime);
            for (int s = -1; s <= 1; s += 2)
            {
                var lap = Part(PrimitiveType.Cube, T, new Vector3(s * 0.035f, 0.24f, 0.113f), new Vector3(0.06f, 0.16f, 0.012f), mLime, new Vector3(-14f, 0f, s * 20f));
                Part(PrimitiveType.Cube, lap.transform, new Vector3(s * 0.5f, 0f, 0.2f), new Vector3(0.2f, 1f, 1.2f), mOrange);
            }
            Band(T, new Vector3(0f, 0.04f, 0f), 0.122f, 0.055f, 0.016f, mOrange, new Vector3(1f, 1f, 0.84f));
            DStrips(T, new Vector3(-0.1f, 0.03f, 0.06f), new Vector3(-8f, 0f, 12f), 2, 0.05f, 0.3f, 0.05f, 2, mOrange, null);
            // Short cropped trousers and long wrapped shins: legs read long and fast.
            Shell(r.pelvis, "tb_hips", new[] { new Vector2(0.14f, -0.12f), new Vector2(0.138f, -0.05f), new Vector2(0.125f, 0f), new Vector2(0.11f, 0.03f) },
                Vector3.zero, new Vector3(1.05f, 1f, 0.86f), mSlate);
            DLegs(r, b, mSlate, mWrap, "wraps");
            DFeet(r, b, mDark, mSole, "tabi");
            DArms(r, b, "bare", mLime, mSkin, mWrap, mSkin);
            for (int i = 0; i < 2; i++) Ball(r.upper[i], new Vector3(0f, 0.01f, 0f), Vector3.one * 0.12f, mLime);
            // Head: round, sharp grinning face with a nose bandage; swept-back hair, orange headband with long tails.
            DHead(b, mSkin, false);
            DFace(b, new FaceSpec { eyeX = 0.12f, eyeW = 0.11f, eyeH = 0.13f, lid = 0.05f, tilt = 9f, brow = 16f, browThick = 0.022f,
                iris = new Color(0.4f, 0.85f, 0.35f), brows = new Color(0.55f, 0.3f, 0.1f), nose = "dot", mouth = "grin", mark = "bandage" }, mSkin, def.skinTone);
            SculptedHair(def, b.hc, b.hr, null);
            DHeadRing(b, 0.14f, 0.05f, 0.05f, mOrange);
            DStrips(head, b.hc + new Vector3(0.04f, 0.13f, -b.hr.z - 0.03f), new Vector3(25f, 0f, 30f), 2, 0.05f, 0.42f, 0.05f, 3, mOrange, mLime);
            ShortBlade(r, mDark, mOrange, def.bladeColor);
            DRarity(r, b, def.rarity, ElementChart.ColorOf(def.element), mOrange, b.chestR * 0.84f * 1.02f);
            HeadY = (b.hipY + b.neck.y + b.hc.y) * b.scale;
        }

        void ShortBlade(PremiumRig r, Material wrap, Material accent, Color bc)
        {
            r.grip = PremiumRig.Grip.OneHand;
            var sp = SwordPivot;
            sp.localRotation = swordRest;
            var steel = PM(new Color(0.8f, 0.83f, 0.86f), 0.01f);
            var edge = PMe(Color.Lerp(new Color(0.9f, 0.92f, 0.95f), bc, 0.45f), 0f, bc * 0.3f);
            Part(PrimitiveType.Cube, sp, new Vector3(0f, 0f, -0.04f), new Vector3(0.036f, 0.044f, 0.22f), wrap);
            for (int i = 0; i < 4; i++) Part(PrimitiveType.Cube, sp, new Vector3(0f, 0f, -0.12f + i * 0.05f), new Vector3(0.04f, 0.04f, 0.016f), accent, new Vector3(0f, 0f, 45f));
            Part(PrimitiveType.Cube, sp, new Vector3(0f, 0f, 0.085f), new Vector3(0.1f, 0.1f, 0.018f), accent);
            Part(PrimitiveType.Cube, sp, new Vector3(0f, 0.008f, 0.39f), new Vector3(0.016f, 0.07f, 0.58f), steel);
            Part(PrimitiveType.Cube, sp, new Vector3(0f, -0.028f, 0.39f), new Vector3(0.01f, 0.012f, 0.58f), edge);
            var tip = MeshFactory.MeshObject(MeshFactory.FacetCone(4), sp, new Vector3(0f, 0.008f, 0.72f), new Vector3(0.016f, 0.1f, 0.07f), steel);
            tip.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            Add(tip);
            PremiumTrail(0.75f, bc);
        }

        // ------------------------------------------------------------------ 2. Bunta — heavy (flame)

        void BuildBunta(CharacterDefinition def)
        {
            var b = new Proportions { scale = 1.28f, hipY = 0.52f, hipW = 0.12f, legA = 0.23f, legB = 0.22f, ankle = 0.09f, armA = 0.23f, armB = 0.22f,
                shoulder = new Vector3(0.36f, 0.33f, -0.01f), neck = new Vector3(0f, 0.42f, 0f), hc = new Vector3(0f, 0.25f, 0.05f), hr = new Vector3(0.3f, 0.3f, 0.29f),
                chestR = 0.24f, torsoW = 1.3f, torsoD = 0.95f, thighR = 0.11f, shinR = 0.085f, armR = 0.09f, handS = 1.45f, footS = 1.2f };
            Color rust = new Color(0.72f, 0.32f, 0.16f), bark = new Color(0.34f, 0.22f, 0.14f), brass = new Color(0.86f, 0.66f, 0.28f);
            var mRust = PM(rust); var mBark = PM(bark); var mBrass = PM(brass, 0.01f); var mSkin = PM(def.skinTone);
            var mRope = PM(new Color(0.85f, 0.76f, 0.55f), 0.01f); var mIron = PM(new Color(0.36f, 0.36f, 0.4f), 0.012f); var mSole = PM(new Color(0.1f, 0.08f, 0.07f), 0.01f);
            var r = DRig(b);
            // Powerful: slow heavy tempo, wide planted stance, shoulders swing into every blow.
            r.stance = 0.16f; r.toeOut = 16f; r.crouch = 0.02f; r.lean = 2f; r.tempo = 8f; r.stride = 0.17f; r.lift = 0.08f;
            r.weightShift = 0.018f; r.twistGain = 0.45f; r.swingDip = 0.09f; r.swingLunge = 10f;
            r.showPose = PremiumRig.ShowPose.HandOnHipL;
            var T = r.torso;
            // Bare barrel chest under an open rust vest, a thick rope belt, one huge iron pauldron.
            DChest(r, b, mSkin, "bu_chest");
            Shell(T, "bu_vest", new[] { new Vector2(0.25f, -0.02f), new Vector2(0.255f, 0.1f), new Vector2(0.26f, 0.22f), new Vector2(0.24f, 0.3f),
                new Vector2(0.16f, 0.345f), new Vector2(0.1f, 0.36f) }, Vector3.zero, new Vector3(1.32f, b.TS.y, 0.98f), mRust);
            Part(PrimitiveType.Cube, T, new Vector3(0f, 0.13f, 0.235f), new Vector3(0.13f, 0.28f, 0.04f), mSkin, new Vector3(-6f, 0f, 0f));
            Band(T, new Vector3(0f, 0.02f, 0f), 0.255f, 0.07f, 0.03f, mRope, new Vector3(1.3f, 1f, 0.97f));
            Ball(T, new Vector3(0.1f, 0.0f, 0.24f), new Vector3(0.09f, 0.08f, 0.06f), mRope);
            DStrips(T, new Vector3(0.12f, -0.02f, 0.24f), new Vector3(-10f, 0f, -6f), 2, 0.045f, 0.2f, 0.035f, 1, mRope, null);
            var pad = J("Pauldron", r.upper[1], new Vector3(-0.03f, 0.05f, 0f));
            for (int k = 0; k < 3; k++)
                Ball(pad, new Vector3(-0.02f * k, -k * 0.07f, 0f), new Vector3(0.36f - k * 0.05f, 0.14f, 0.32f - k * 0.04f), k == 1 ? mBrass : mIron, new Vector3(0f, 0f, 22f));
            Ball(pad, new Vector3(-0.08f, 0.04f, 0f), Vector3.one * 0.06f, mBrass);
            // Heavy dark hakama, iron-shod boots.
            Shell(r.pelvis, "bu_hakama", new[] { new Vector2(0.3f, -0.4f), new Vector2(0.302f, -0.37f), new Vector2(0.27f, -0.2f), new Vector2(0.24f, -0.05f),
                new Vector2(0.22f, 0f), new Vector2(0.2f, 0.03f) }, Vector3.zero, new Vector3(1.1f, 1f, 0.95f), mBark);
            Band(r.pelvis, new Vector3(0f, -0.385f, 0f), 0.3f, 0.03f, 0.01f, mRust, new Vector3(1.1f, 1f, 0.95f));
            DLegs(r, b, mBark, mIron, "greaves");
            DFeet(r, b, mBark, mSole, "heavy");
            DArms(r, b, "bare", mRust, mSkin, mBrass, PM(new Color(0.3f, 0.2f, 0.14f)));
            // Small head on a huge body: square jaw, thick brows, calm heavy eyes, top-knot.
            DHead(b, mSkin, true);
            DFace(b, new FaceSpec { eyeX = 0.1f, eyeY = -0.02f, eyeW = 0.085f, eyeH = 0.09f, lid = 0.32f, tilt = -2f, brow = 10f, browThick = 0.036f,
                iris = new Color(0.95f, 0.55f, 0.2f), brows = new Color(0.12f, 0.08f, 0.06f), nose = "broad", mouth = "smirk", mark = "scar" }, mSkin, def.skinTone);
            SculptedHair(def, b.hc, b.hr, null);
            var hairM = PM(def.hairColor, 0.01f);
            Vector3 knot = b.hc + new Vector3(0f, b.hr.y * 0.95f, -b.hr.z * 0.25f);
            Ball(head, knot, new Vector3(0.11f, 0.1f, 0.13f), hairM);
            Band(head, knot + new Vector3(0f, -0.02f, 0f), 0.05f, 0.03f, 0.01f, mRust);
            Ball(head, knot + new Vector3(0f, 0.03f, 0.08f), new Vector3(0.06f, 0.05f, 0.12f), hairM, new Vector3(-20f, 0f, 0f));
            WarHammer(r, mBark, mIron, mBrass, def.bladeColor);
            DRarity(r, b, def.rarity, ElementChart.ColorOf(def.element), mBrass, b.chestR * 0.98f);
            HeadY = (b.hipY + b.neck.y + b.hc.y) * b.scale;
        }

        void WarHammer(PremiumRig r, Material haft, Material iron, Material brass, Color bc)
        {
            r.grip = PremiumRig.Grip.TwoHand;
            r.handleOffset = -0.22f;
            swordRest = Quaternion.Euler(-108f, 32f, 0f);
            var sp = SwordPivot;
            sp.localRotation = swordRest;
            r.poseWeapon = swordRest.eulerAngles;
            Part(PrimitiveType.Cylinder, sp, new Vector3(0f, 0f, 0.32f), new Vector3(0.06f, 0.62f, 0.06f), haft, new Vector3(90f, 0f, 0f));
            for (int i = 0; i < 3; i++) Band(sp, new Vector3(0f, 0f, -0.18f + i * 0.08f), 0.034f, 0.03f, 0.008f, brass, null, new Vector3(90f, 0f, 0f));
            Ball(sp, new Vector3(0f, 0f, -0.3f), Vector3.one * 0.08f, iron);
            // The head: a heavy iron block with brass bands and a glowing core.
            Part(PrimitiveType.Cube, sp, new Vector3(0f, 0f, 0.98f), new Vector3(0.24f, 0.44f, 0.24f), iron);
            for (int k = -1; k <= 1; k += 2)
            {
                Part(PrimitiveType.Cube, sp, new Vector3(0f, k * 0.17f, 0.98f), new Vector3(0.26f, 0.05f, 0.26f), brass);
                Part(PrimitiveType.Cube, sp, new Vector3(0f, k * 0.25f, 0.98f), new Vector3(0.2f, 0.06f, 0.2f), iron);
            }
            Ball(sp, new Vector3(0.125f, 0f, 0.98f), new Vector3(0.02f, 0.1f, 0.1f), PMe(bc, 0f, bc * 0.8f));
            Ball(sp, new Vector3(-0.125f, 0f, 0.98f), new Vector3(0.02f, 0.1f, 0.1f), PMe(bc, 0f, bc * 0.8f));
            PremiumTrail(1.1f, bc);
        }

        // ------------------------------------------------------------------ 3. Sayo — ranged (thunder)

        void BuildSayo(CharacterDefinition def)
        {
            var b = new Proportions { scale = 1.08f, hipY = 0.74f, hipW = 0.07f, legA = 0.34f, legB = 0.34f, ankle = 0.08f, armA = 0.21f, armB = 0.2f,
                shoulder = new Vector3(0.19f, 0.28f, -0.01f), neck = new Vector3(0f, 0.35f, 0f), hc = new Vector3(0f, 0.34f, 0.02f), hr = new Vector3(0.33f, 0.37f, 0.33f),
                chestR = 0.122f, torsoW = 0.96f, torsoD = 0.8f, thighR = 0.066f, shinR = 0.045f, armR = 0.048f };
            Color forest = new Color(0.14f, 0.42f, 0.28f), cream = new Color(0.94f, 0.9f, 0.8f), crimson = new Color(0.78f, 0.12f, 0.2f);
            var mForest = PM(forest); var mForestDk = PM(Color.Lerp(forest, Color.black, 0.35f)); var mCream = PM(cream, 0.01f); var mCrimson = PM(crimson, 0.01f);
            var mSkin = PM(def.skinTone); var mLeather = PM(new Color(0.36f, 0.24f, 0.15f)); var mSole = PM(new Color(0.08f, 0.07f, 0.07f), 0.01f);
            var mGold = PM(new Color(0.96f, 0.8f, 0.36f), 0.008f);
            var r = DRig(b);
            // Composed: long unhurried strides, very little bounce, upright.
            r.crouch = 0.005f; r.lean = 1f; r.tempo = 9.5f; r.stride = 0.22f; r.lift = 0.1f; r.weightShift = 0.012f; r.twistGain = 0.2f; r.swingDip = 0.02f; r.swingLunge = 5f;
            r.showPose = PremiumRig.ShowPose.HandOnHipR;
            var T = r.torso;
            // Fitted cream top under a forest-green long jacket split at the hips, crimson high collar.
            DChest(r, b, mCream, "sy_chest");
            Shell(T, "sy_coat", new[] { new Vector2(0.14f, -0.02f), new Vector2(0.134f, 0.1f), new Vector2(0.138f, 0.22f), new Vector2(0.125f, 0.3f),
                new Vector2(0.075f, 0.345f), new Vector2(0f, 0.355f) }, Vector3.zero, new Vector3(1f, 1f, 0.84f), mForest);
            Shell(T, "sy_collar", new[] { new Vector2(0.07f, 0.3f), new Vector2(0.08f, 0.34f), new Vector2(0.09f, 0.4f) }, Vector3.zero, Vector3.one, mCrimson);
            Band(T, new Vector3(0f, 0.03f, 0f), 0.13f, 0.035f, 0.01f, mLeather, new Vector3(1f, 1f, 0.84f));
            Part(PrimitiveType.Cube, T, new Vector3(0f, 0.03f, 0.11f), new Vector3(0.05f, 0.04f, 0.02f), mGold);
            // Coat tails front and back (split for the long legs).
            for (int s = -1; s <= 1; s += 2)
            {
                var tail = Part(PrimitiveType.Cube, r.pelvis, new Vector3(0f, -0.2f, s * 0.12f), new Vector3(0.24f, 0.38f, 0.02f), mForest, new Vector3(s * 8f, 0f, 0f));
                Part(PrimitiveType.Cube, tail.transform, new Vector3(0f, -0.49f, 0f), new Vector3(1.02f, 0.06f, 1.3f), mCrimson);
            }
            // Half cape over the bow shoulder and a quiver on the back.
            var cape = J("HalfCape", T, new Vector3(-0.1f, 0.33f, -0.04f));
            cape.localRotation = Quaternion.Euler(0f, 0f, 18f);
            Ball(cape, new Vector3(-0.04f, -0.02f, 0f), new Vector3(0.24f, 0.1f, 0.28f), mCrimson);
            DStrips(cape, new Vector3(-0.05f, -0.02f, -0.1f), new Vector3(10f, 0f, 0f), 3, 0.07f, 0.42f, 0.075f, 2, mCrimson, mGold);
            var q = J("Quiver", T, new Vector3(0.1f, 0.2f, -0.17f));
            q.localRotation = Quaternion.Euler(-12f, 0f, -24f);
            Part(PrimitiveType.Cylinder, q, Vector3.zero, new Vector3(0.11f, 0.2f, 0.11f), mLeather);
            Band(q, new Vector3(0f, 0.17f, 0f), 0.058f, 0.03f, 0.008f, mGold);
            for (int i = 0; i < 4; i++)
            {
                Part(PrimitiveType.Cylinder, q, new Vector3((i - 1.5f) * 0.022f, 0.28f, (i % 2) * 0.018f), new Vector3(0.01f, 0.1f, 0.01f), mCream);
                Part(PrimitiveType.Cube, q, new Vector3((i - 1.5f) * 0.022f, 0.38f, (i % 2) * 0.018f), new Vector3(0.004f, 0.06f, 0.03f), mCrimson);
            }
            // Slim trousers and thigh-high boots: the legs are a big part of her silhouette.
            Shell(r.pelvis, "sy_hips", new[] { new Vector2(0.125f, -0.1f), new Vector2(0.123f, -0.04f), new Vector2(0.11f, 0f), new Vector2(0.1f, 0.03f) },
                Vector3.zero, new Vector3(1.05f, 1f, 0.86f), mForestDk);
            DLegs(r, b, mForestDk, mLeather, "boots");
            for (int i = 0; i < 2; i++) Taper(r.thigh[i], new Vector3(0f, -b.legA * 0.45f, 0f), b.legA * 0.52f, b.thighR * 1.08f, b.thighR * 1.2f, mLeather);
            DFeet(r, b, mLeather, mSole, "boot");
            DArms(r, b, "fitted", mForest, mSkin, mCrimson, mLeather);
            var guard = J("ArmGuard", r.lower[1], new Vector3(0f, -0.1f, 0f));
            Taper(guard, Vector3.zero, 0.12f, 0.05f, 0.046f, mLeather);
            // Narrow oval face, almond eyes, beauty mark, high ponytail.
            DHead(b, mSkin, false);
            DFace(b, new FaceSpec { eyeX = 0.115f, eyeY = -0.03f, eyeW = 0.12f, eyeH = 0.1f, lid = 0.2f, tilt = 6f, brow = 4f, browThick = 0.016f, lashes = true,
                iris = new Color(0.95f, 0.8f, 0.2f), brows = new Color(0.25f, 0.13f, 0.08f), nose = "line", mouth = "smile", mark = "beauty" }, mSkin, def.skinTone);
            SculptedHair(def, b.hc, b.hr, null);
            Band(head, b.hc + new Vector3(0f, 0.2f, -0.29f), 0.04f, 0.04f, 0.012f, mCrimson, null, new Vector3(-35f, 0f, 0f));
            Longbow(r, mLeather, mCrimson, mGold, def.bladeColor);
            DRarity(r, b, def.rarity, ElementChart.ColorOf(def.element), mGold, 0.14f * 0.84f);
            HeadY = (b.hipY + b.neck.y + b.hc.y) * b.scale;
        }

        void Longbow(PremiumRig r, Material leather, Material accent, Material gold, Color bc)
        {
            r.grip = PremiumRig.Grip.Caster;
            r.restHandL = new Vector3(-r.shoulder.x - 0.1f, 0.12f, 0.2f);
            SwordPivot.localRotation = swordRest;
            var limb = PM(new Color(0.3f, 0.18f, 0.1f), 0.012f);
            var bow = J("Longbow", r.hand[1], new Vector3(0f, -0.06f, 0.02f));
            bow.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            // Asymmetric: the grip sits a third of the way up, so the upper limb is much longer.
            for (int i = 0; i < 17; i++)
            {
                float t = i / 16f;
                float y = Mathf.Lerp(-0.42f, 0.95f, t);
                float bend = Mathf.Sin(t * Mathf.PI) * 0.16f + (t > 0.9f ? (t - 0.9f) * 0.6f : 0f) + (t < 0.08f ? (0.08f - t) * 0.6f : 0f);
                float grip = Mathf.Abs(y) < 0.05f ? 1.35f : 1f;
                Ball(bow, new Vector3(0f, y, -bend), new Vector3(0.04f, 0.12f, 0.045f) * grip, Mathf.Abs(y) < 0.05f ? leather : (i % 4 == 0 ? accent : limb));
            }
            Part(PrimitiveType.Cube, bow, new Vector3(0f, 0.265f, 0.015f), new Vector3(0.005f, 1.37f, 0.005f), PM(new Color(0.95f, 0.93f, 0.85f), 0f));
            Ball(bow, new Vector3(0f, 0.95f, -0.02f), Vector3.one * 0.045f, gold);
            Ball(bow, new Vector3(0f, -0.42f, -0.02f), Vector3.one * 0.045f, gold);
            PremiumTrail(0.2f, bc);
        }

        // ------------------------------------------------------------------ 4. Nene — support (light)

        void BuildNene(CharacterDefinition def)
        {
            var b = new Proportions { scale = 0.84f, hipY = 0.44f, hipW = 0.075f, legA = 0.19f, legB = 0.19f, ankle = 0.07f, armA = 0.16f, armB = 0.15f,
                shoulder = new Vector3(0.18f, 0.25f, -0.01f), neck = new Vector3(0f, 0.3f, 0f), hc = new Vector3(0f, 0.36f, 0.02f), hr = new Vector3(0.41f, 0.4f, 0.38f),
                chestR = 0.15f, torsoW = 1.05f, torsoD = 0.9f, thighR = 0.07f, shinR = 0.05f, armR = 0.05f, handS = 0.9f, footS = 0.9f };
            Color lavender = new Color(0.72f, 0.62f, 0.92f), cream = new Color(0.98f, 0.95f, 0.88f), peach = new Color(1f, 0.66f, 0.55f);
            var mLav = PM(lavender); var mCream = PM(cream, 0.01f); var mPeach = PM(peach, 0.01f);
            var mSkin = PM(def.skinTone); var mWood = PM(new Color(0.5f, 0.34f, 0.22f), 0.01f); var mGold = PM(new Color(0.98f, 0.82f, 0.4f), 0.008f);
            var r = DRig(b);
            // Gentle: small light steps, a floaty weight shift, soft swings.
            r.crouch = 0f; r.lean = -2f; r.tempo = 12f; r.stride = 0.1f; r.lift = 0.09f; r.idleBounce = 0.006f; r.idleBounceFreq = 1.6f; r.weightShift = 0.015f;
            r.twistGain = 0.15f; r.swingDip = 0.01f; r.swingLunge = 3f;
            r.showPose = PremiumRig.ShowPose.StaffBothHands;
            r.showButt = 0.52f;
            var T = r.torso;
            // Round layered robe: cream under, lavender over, huge bell sleeves, peach obi with a big bow at the back.
            DChest(r, b, mCream, "nn_chest");
            Shell(T, "nn_over", new[] { new Vector2(0.162f, -0.02f), new Vector2(0.165f, 0.1f), new Vector2(0.168f, 0.2f), new Vector2(0.15f, 0.27f),
                new Vector2(0.09f, 0.31f), new Vector2(0.05f, 0.32f) }, Vector3.zero, new Vector3(1.05f, b.TS.y, 0.9f), mLav);
            for (int s = -1; s <= 1; s += 2)
                Part(PrimitiveType.Cube, T, new Vector3(s * 0.03f, 0.21f, 0.14f), new Vector3(0.06f, 0.14f, 0.012f), mCream, new Vector3(-16f, 0f, s * 24f));
            Band(T, new Vector3(0f, 0.05f, 0f), 0.17f, 0.09f, 0.02f, mPeach, new Vector3(1.05f, 1f, 0.9f));
            var bow = J("ObiBow", T, new Vector3(0f, 0.07f, -0.17f));
            for (int s = -1; s <= 1; s += 2)
                Ball(bow, new Vector3(s * 0.11f, 0.02f, -0.02f), new Vector3(0.18f, 0.13f, 0.06f), mPeach, new Vector3(0f, 0f, s * 15f));
            Ball(bow, new Vector3(0f, 0.01f, -0.03f), new Vector3(0.07f, 0.08f, 0.06f), mGold);
            DStrips(bow, new Vector3(0f, -0.02f, -0.03f), new Vector3(8f, 0f, 0f), 2, 0.08f, 0.26f, 0.08f, 2, mPeach, null);
            Shell(r.pelvis, "nn_robe", new[] { new Vector2(0.27f, -0.36f), new Vector2(0.272f, -0.34f), new Vector2(0.24f, -0.2f), new Vector2(0.2f, -0.06f),
                new Vector2(0.18f, 0f), new Vector2(0.17f, 0.03f) }, Vector3.zero, new Vector3(1f, 1f, 0.95f), mLav);
            Band(r.pelvis, new Vector3(0f, -0.35f, 0f), 0.27f, 0.028f, 0.009f, mGold, new Vector3(1f, 1f, 0.95f));
            DLegs(r, b, mCream, mCream, "plain");
            DFeet(r, b, mCream, mWood, "geta");
            DArms(r, b, "bell", mLav, mSkin, mGold, mSkin, false);
            // Big round head, huge round eyes, blush, round bob with a flower pin.
            DHead(b, mSkin, false);
            DFace(b, new FaceSpec { eyeX = 0.14f, eyeY = -0.05f, eyeW = 0.13f, eyeH = 0.16f, lid = 0f, tilt = -4f, brow = -10f, browThick = 0.016f, lashes = true,
                iris = new Color(0.95f, 0.72f, 0.3f), brows = new Color(0.7f, 0.5f, 0.3f), nose = "dot", mouth = "o", mark = "blush" }, mSkin, def.skinTone);
            SculptedHair(def, b.hc, b.hr, null);
            var pin = J("FlowerPin", head, b.hc + new Vector3(b.hr.x * 0.72f, b.hr.y * 0.52f, b.hr.z * 0.35f));
            for (int k = 0; k < 5; k++)
            {
                float a = k * 72f * Mathf.Deg2Rad;
                Ball(pin, new Vector3(Mathf.Cos(a) * 0.045f, Mathf.Sin(a) * 0.045f, 0f), new Vector3(0.055f, 0.055f, 0.025f), mPeach);
            }
            Ball(pin, Vector3.zero, Vector3.one * 0.035f, mGold);
            pin.localRotation = Quaternion.Euler(0f, 50f, 0f);
            LanternStaff(r, mWood, mPeach, mGold, def.bladeColor);
            DRarity(r, b, def.rarity, ElementChart.ColorOf(def.element), mGold, 0.17f * 0.9f);
            HeadY = (b.hipY + b.neck.y + b.hc.y) * b.scale;
        }

        void LanternStaff(PremiumRig r, Material wood, Material paper, Material gold, Color bc)
        {
            r.grip = PremiumRig.Grip.Caster;
            r.restHandL = new Vector3(-r.shoulder.x - 0.02f, 0.28f, 0.28f);
            swordRest = Quaternion.Euler(-78f, 12f, 0f);
            var sp = SwordPivot;
            sp.localRotation = swordRest;
            r.poseWeapon = swordRest.eulerAngles;
            Part(PrimitiveType.Cylinder, sp, new Vector3(0f, 0f, 0.32f), new Vector3(0.04f, 0.84f, 0.04f), wood, new Vector3(90f, 0f, 0f));
            Ball(sp, new Vector3(0f, 0f, -0.52f), Vector3.one * 0.05f, gold);
            // The crook: a curled top that the lantern hangs from.
            for (int k = 0; k < 8; k++)
            {
                float a = k / 7f * 200f * Mathf.Deg2Rad;
                Ball(sp, new Vector3(0f, Mathf.Sin(a) * 0.12f, 1.16f + (1f - Mathf.Cos(a)) * 0.12f - 0.12f + 0.12f), Vector3.one * 0.05f, k % 3 == 0 ? gold : wood);
            }
            var hang = J("Lantern", sp, new Vector3(0f, 0.23f, 1.12f));
            hang.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            var sw = hang.gameObject.AddComponent<Sway>();
            sw.Amount = 8f;
            sw.Speed = 1.1f;
            Part(PrimitiveType.Cylinder, hang, new Vector3(0f, -0.06f, 0f), new Vector3(0.008f, 0.06f, 0.008f), gold);
            Ball(hang, new Vector3(0f, -0.2f, 0f), new Vector3(0.17f, 0.2f, 0.17f), PMe(paper.color, 0f, bc * 0.55f));
            for (int k = 0; k < 3; k++) Band(hang, new Vector3(0f, -0.14f - k * 0.06f, 0f), 0.08f - Mathf.Abs(k - 1) * 0.015f, 0.008f, 0.004f, gold);
            Part(PrimitiveType.Cylinder, hang, new Vector3(0f, -0.1f, 0f), new Vector3(0.1f, 0.012f, 0.1f), wood);
            Part(PrimitiveType.Cylinder, hang, new Vector3(0f, -0.3f, 0f), new Vector3(0.1f, 0.012f, 0.1f), wood);
            MeshFactory.MeshObject(MeshFactory.SmoothSphere(), hang, new Vector3(0f, -0.2f, 0f), Vector3.one * 0.3f, MaterialFactory.Additive(new Color(bc.r, bc.g, bc.b, 0.25f)), false);
            var glowGo = MeshFactory.MeshObject(MeshFactory.SmoothSphere(), r.hand[1], new Vector3(0f, -0.1f, 0.06f), Vector3.one * 0.08f, MaterialFactory.Additive(new Color(bc.r, bc.g, bc.b, 0.45f)), false);
            r.castGlow = glowGo.transform;
            PremiumTrail(1.2f, bc);
        }

        // ------------------------------------------------------------------ 5. Nagi — assassin (dark)

        void BuildNagi(CharacterDefinition def)
        {
            var b = new Proportions { scale = 0.9f, hipY = 0.56f, hipW = 0.07f, legA = 0.26f, legB = 0.26f, ankle = 0.075f, armA = 0.19f, armB = 0.18f,
                shoulder = new Vector3(0.18f, 0.27f, -0.01f), neck = new Vector3(0f, 0.33f, 0f), hc = new Vector3(0f, 0.31f, 0.02f), hr = new Vector3(0.34f, 0.34f, 0.33f),
                chestR = 0.12f, torsoW = 0.98f, torsoD = 0.8f, thighR = 0.064f, shinR = 0.044f, armR = 0.046f };
            Color charcoal = new Color(0.17f, 0.17f, 0.21f), plum = new Color(0.4f, 0.2f, 0.42f), acid = new Color(0.7f, 1f, 0.25f);
            var mChar = PM(charcoal); var mPlum = PM(plum); var mAcid = PMe(acid, 0.006f, acid * 0.35f); var mSkin = PM(def.skinTone);
            var mWrap = PM(new Color(0.3f, 0.28f, 0.34f), 0.01f); var mSole = PM(new Color(0.06f, 0.06f, 0.07f), 0.01f);
            var r = DRig(b);
            // Mysterious: low and quiet, a forward hunch, soft steps, sudden fast swings.
            r.crouch = 0.04f; r.lean = 11f; r.tempo = 12.5f; r.stride = 0.15f; r.lift = 0.07f; r.weightShift = 0.006f;
            r.twistGain = 0.4f; r.swingDip = 0.06f; r.swingLunge = 16f;
            r.showPose = PremiumRig.ShowPose.Natural;
            var T = r.torso;
            // Close-fitting charcoal wraps, a plum tabard with an acid-green edge, a belt of small pouches.
            DChest(r, b, mChar, "ng_chest");
            Part(PrimitiveType.Cube, T, new Vector3(0f, 0.12f, 0.1f), new Vector3(0.17f, 0.34f, 0.02f), mPlum, new Vector3(-4f, 0f, 0f));
            Part(PrimitiveType.Cube, T, new Vector3(0f, -0.07f, 0.1f), new Vector3(0.18f, 0.03f, 0.022f), mAcid);
            Part(PrimitiveType.Cube, r.pelvis, new Vector3(0f, -0.14f, 0.1f), new Vector3(0.16f, 0.24f, 0.02f), mPlum, new Vector3(-6f, 0f, 0f));
            Part(PrimitiveType.Cube, r.pelvis, new Vector3(0f, -0.27f, 0.114f), new Vector3(0.162f, 0.02f, 0.022f), mAcid, new Vector3(-6f, 0f, 0f));
            for (int k = 0; k < 3; k++) Band(T, new Vector3(0f, 0.2f - k * 0.07f, 0f), 0.125f, 0.018f, 0.006f, mWrap, new Vector3(1f, 1f, 0.84f), new Vector3(0f, 0f, 10f));
            Band(T, new Vector3(0f, 0.02f, 0f), 0.118f, 0.04f, 0.01f, mWrap, new Vector3(1f, 1f, 0.84f));
            for (int s = -1; s <= 1; s += 2) Ball(T, new Vector3(s * 0.1f, 0.0f, -0.06f), new Vector3(0.06f, 0.07f, 0.05f), mPlum);
            // A short tattered scarf trailing from the neck.
            DStrips(T, new Vector3(0.03f, 0.32f, -0.09f), new Vector3(24f, 0f, -8f), 2, 0.05f, 0.34f, 0.055f, 3, mPlum, mAcid);
            Shell(r.pelvis, "ng_hips", new[] { new Vector2(0.12f, -0.1f), new Vector2(0.118f, -0.04f), new Vector2(0.105f, 0f), new Vector2(0.095f, 0.03f) },
                Vector3.zero, new Vector3(1.05f, 1f, 0.86f), mChar);
            DLegs(r, b, mChar, mWrap, "wraps");
            DFeet(r, b, mChar, mSole, "tabi");
            DArms(r, b, "fitted", mChar, mSkin, mWrap, mWrap);
            for (int i = 0; i < 2; i++)
                for (int k = 0; k < 3; k++) Band(r.lower[i], new Vector3(0f, -0.04f - k * 0.045f, 0f), 0.044f - k * 0.002f, 0.014f, 0.006f, mWrap, null, new Vector3(0f, 0f, k % 2 == 0 ? 14f : -14f));
            // Face: sharp narrow eyes over a cloth mask; the hood and its point come from the hair system (cloth).
            DHead(b, mSkin, false);
            DFace(b, new FaceSpec { eyeX = 0.12f, eyeY = -0.02f, eyeW = 0.12f, eyeH = 0.085f, lid = 0.3f, tilt = 12f, brow = 22f, browThick = 0.02f,
                iris = new Color(0.72f, 1f, 0.3f), brows = new Color(0.2f, 0.2f, 0.24f), nose = "none", mouth = "none" }, mSkin, def.skinTone);
            Ball(head, b.hc + new Vector3(0f, -0.19f, 0.02f), new Vector3(b.hr.x * 2.06f, b.hr.y * 1.24f, b.hr.z * 2.08f), mChar);
            Part(PrimitiveType.Cube, head, b.hc + new Vector3(0f, -0.1f, b.hr.z + 0.005f), new Vector3(0.2f, 0.012f, 0.012f), mAcid);
            SculptedHair(def, b.hc, b.hr, mPlum);
            TwinSickles(r, mChar, mWrap, mAcid, def.bladeColor);
            DRarity(r, b, def.rarity, ElementChart.ColorOf(def.element), mAcid, 0.12f);
            HeadY = (b.hipY + b.neck.y + b.hc.y) * b.scale;
        }

        void Sickle(Transform parent, Material handle, Material wrap, Material steel, Material edge)
        {
            Part(PrimitiveType.Cube, parent, new Vector3(0f, 0f, 0.08f), new Vector3(0.034f, 0.034f, 0.3f), handle);
            for (int i = 0; i < 3; i++) Part(PrimitiveType.Cube, parent, new Vector3(0f, 0f, -0.02f + i * 0.06f), new Vector3(0.038f, 0.038f, 0.012f), wrap, new Vector3(0f, 0f, 45f));
            // The blade: a curved row of tapering plates bending forward from the top of the handle.
            for (int k = 0; k < 6; k++)
            {
                float a = k * 0.2f;
                var seg = Part(PrimitiveType.Cube, parent, new Vector3(0f, -Mathf.Sin(a) * 0.12f - 0.03f, 0.24f + Mathf.Cos(a) * 0.04f + k * 0.012f),
                    new Vector3(0.012f, 0.06f - k * 0.006f, 0.06f), steel, new Vector3(-a * Mathf.Rad2Deg - 20f, 0f, 0f));
                Part(PrimitiveType.Cube, seg.transform, new Vector3(0f, -0.5f, 0f), new Vector3(0.9f, 0.18f, 1f), edge);
            }
            // A short length of chain from the pommel.
            var chain = J("Chain", parent, new Vector3(0f, 0f, -0.08f));
            chain.localRotation = Quaternion.Euler(90f, 0f, 0f);
            var sw = chain.gameObject.AddComponent<Sway>();
            sw.Amount = 10f;
            sw.Speed = 1.8f;
            for (int k = 0; k < 5; k++) Band(chain, new Vector3(0f, -0.02f - k * 0.03f, 0f), 0.012f, 0.02f, 0.004f, wrap, null, new Vector3(0f, k * 90f, 90f));
        }

        void TwinSickles(PremiumRig r, Material handle, Material wrap, Material accent, Color bc)
        {
            r.grip = PremiumRig.Grip.Twin;
            r.twinLeft = Quaternion.Euler(90f, 0f, 0f);
            r.twinLeftPos = new Vector3(0f, -0.055f, 0f);
            SwordPivot.localRotation = swordRest;
            var steel = PM(new Color(0.72f, 0.74f, 0.78f), 0.01f);
            var edge = PMe(Color.Lerp(new Color(0.85f, 0.88f, 0.9f), bc, 0.5f), 0f, bc * 0.4f);
            Sickle(SwordPivot, handle, wrap, steel, edge);
            var left = J("LeftSickle", r.hand[1], r.twinLeftPos);
            left.localRotation = r.twinLeft;
            Sickle(left, handle, wrap, steel, edge);
            PremiumTrail(0.3f, bc);
        }

        // ------------------------------------------------------------------ 6. Seiran — elemental powerhouse (water)

        void BuildSeiran(CharacterDefinition def)
        {
            var b = new Proportions { scale = 1.18f, hipY = 0.76f, hipW = 0.08f, legA = 0.35f, legB = 0.35f, ankle = 0.08f, armA = 0.23f, armB = 0.22f,
                shoulder = new Vector3(0.23f, 0.31f, -0.01f), neck = new Vector3(0f, 0.37f, 0f), hc = new Vector3(0f, 0.34f, 0.02f), hr = new Vector3(0.34f, 0.36f, 0.33f),
                chestR = 0.15f, torsoW = 1.05f, torsoD = 0.84f, thighR = 0.072f, shinR = 0.05f, armR = 0.055f };
            Color deep = new Color(0.1f, 0.2f, 0.45f), silver = new Color(0.84f, 0.87f, 0.92f), aqua = new Color(0.3f, 0.9f, 0.95f);
            var mDeep = PM(deep); var mDeepDk = PM(Color.Lerp(deep, Color.black, 0.4f)); var mSilver = PM(silver, 0.01f); var mAqua = PMe(aqua, 0.006f, aqua * 0.4f);
            var mSkin = PM(def.skinTone); var mSole = PM(new Color(0.07f, 0.08f, 0.1f), 0.01f);
            var r = DRig(b);
            // Powerful and calm: slow deliberate tempo, long stride, nothing wasted, heavy committed swings.
            r.crouch = 0f; r.lean = -1f; r.tempo = 8f; r.stride = 0.2f; r.lift = 0.09f; r.weightShift = 0.01f; r.twistGain = 0.3f; r.swingDip = 0.04f; r.swingLunge = 8f;
            r.showPose = PremiumRig.ShowPose.OrbHand;
            r.showButt = 0.75f;
            var T = r.torso;
            // Long deep-blue coat with silver edging, tall stiff collar rising behind the head, long cape.
            DChest(r, b, mSilver, "sr_chest");
            Shell(T, "sr_coat", new[] { new Vector2(0.162f, -0.02f), new Vector2(0.165f, 0.1f), new Vector2(0.17f, 0.22f), new Vector2(0.155f, 0.3f),
                new Vector2(0.1f, 0.345f), new Vector2(0.06f, 0.36f) }, Vector3.zero, new Vector3(1.05f, b.TS.y, 0.86f), mDeep);
            for (int s = -1; s <= 1; s += 2)
            {
                var lap = Part(PrimitiveType.Cube, T, new Vector3(s * 0.035f, 0.22f, 0.135f), new Vector3(0.065f, 0.2f, 0.012f), mSilver, new Vector3(-14f, 0f, s * 20f));
                Part(PrimitiveType.Cube, lap.transform, new Vector3(s * 0.5f, 0f, 0.2f), new Vector3(0.2f, 1f, 1.2f), mAqua);
            }
            Shell(T, "sr_collar", new[] { new Vector2(0.11f, 0.3f), new Vector2(0.13f, 0.38f), new Vector2(0.17f, 0.52f), new Vector2(0.175f, 0.54f) },
                new Vector3(0f, 0f, -0.03f), new Vector3(1.1f, 1f, 0.8f), mDeepDk, 24);
            Band(T, new Vector3(0f, 0.53f, -0.03f), 0.175f, 0.02f, 0.008f, mSilver, new Vector3(1.1f, 1f, 0.8f));
            Band(T, new Vector3(0f, 0.04f, 0f), 0.165f, 0.05f, 0.012f, mSilver, new Vector3(1.05f, 1f, 0.86f));
            Ball(T, new Vector3(0f, 0.04f, 0.14f), new Vector3(0.06f, 0.06f, 0.03f), mAqua);
            Shell(r.pelvis, "sr_coatskirt", new[] { new Vector2(0.26f, -0.56f), new Vector2(0.262f, -0.53f), new Vector2(0.22f, -0.3f), new Vector2(0.18f, -0.08f),
                new Vector2(0.165f, 0f), new Vector2(0.155f, 0.03f) }, Vector3.zero, new Vector3(1.05f, 1f, 0.9f), mDeep);
            Band(r.pelvis, new Vector3(0f, -0.545f, 0f), 0.26f, 0.026f, 0.008f, mSilver, new Vector3(1.05f, 1f, 0.9f));
            var cape = J("Cape", T, new Vector3(0f, 0.34f, -0.15f));
            cape.localRotation = Quaternion.Euler(6f, 0f, 0f);
            for (int k = 0; k < 5; k++)
            {
                var strip = J("Panel", cape, new Vector3(-0.18f + k * 0.09f, 0f, -Mathf.Abs(k - 2) * 0.014f));
                strip.localRotation = Quaternion.Euler(3f, 0f, (k - 2) * 3f);
                float len = 1.05f - Mathf.Abs(k - 2) * 0.06f;
                Part(PrimitiveType.Cube, strip, new Vector3(0f, -len * 0.5f, 0f), new Vector3(0.095f, len, 0.02f), mDeepDk);
                Part(PrimitiveType.Cube, strip, new Vector3(0f, -len + 0.02f, 0f), new Vector3(0.098f, 0.04f, 0.024f), mAqua);
                var sw = strip.gameObject.AddComponent<Sway>();
                sw.Amount = 3f;
                sw.Speed = 0.8f + k * 0.07f;
            }
            DLegs(r, b, mDeepDk, mSilver, "greaves");
            DFeet(r, b, mDeepDk, mSole, "boot");
            DArms(r, b, "wide", mDeep, mSkin, mSilver, PM(new Color(0.9f, 0.92f, 0.96f)), true);
            for (int i = 0; i < 2; i++) Ball(r.upper[i], new Vector3(0f, 0.03f, 0f), new Vector3(0.2f, 0.1f, 0.18f), mSilver, new Vector3(0f, 0f, (i == 0 ? -1f : 1f) * 18f));
            // Calm elegant face, a silver circlet with swept fins and an aqua gem, a long braid.
            DHead(b, mSkin, false);
            DFace(b, new FaceSpec { eyeX = 0.12f, eyeY = -0.03f, eyeW = 0.115f, eyeH = 0.11f, lid = 0.25f, tilt = 2f, brow = 2f, browThick = 0.016f, lashes = true,
                iris = new Color(0.3f, 0.85f, 1f), brows = new Color(0.1f, 0.18f, 0.35f), nose = "line", mouth = "flat" }, mSkin, def.skinTone);
            SculptedHair(def, b.hc, b.hr, null);
            DHeadRing(b, 0.16f, 0.07f, 0.03f, mSilver);
            Ball(head, b.hc + new Vector3(0f, 0.18f, b.hr.z * 0.86f + 0.07f), new Vector3(0.05f, 0.06f, 0.03f), mAqua);
            for (int s = -1; s <= 1; s += 2)
                Part(PrimitiveType.Cube, head, b.hc + new Vector3(s * (b.hr.x * 0.86f + 0.06f), 0.2f, 0.02f), new Vector3(0.02f, 0.05f, 0.16f), mSilver, new Vector3(-30f, 0f, s * -20f));
            var hairM = PM(def.hairColor, 0.01f);
            var braid = J("Braid", head, b.hc + new Vector3(0f, -0.1f, -b.hr.z - 0.03f));
            Transform prev = braid;
            for (int k = 0; k < 7; k++)
            {
                Ball(prev, new Vector3(k % 2 == 0 ? 0.012f : -0.012f, -0.05f, 0f), new Vector3(0.1f - k * 0.006f, 0.1f, 0.08f - k * 0.004f), hairM, new Vector3(0f, 0f, k % 2 == 0 ? 20f : -20f));
                var sw = prev.gameObject.AddComponent<Sway>();
                sw.Amount = 2f + k * 0.8f;
                sw.Speed = 0.9f;
                var next = J("B", prev, new Vector3(0f, -0.085f, -0.005f));
                prev = next;
            }
            Band(prev, new Vector3(0f, 0.02f, 0f), 0.03f, 0.03f, 0.01f, mAqua);
            ConePart(prev, new Vector3(0f, -0.05f, 0f), new Vector3(0.07f, 0.12f, 0.06f), hairM, new Vector3(180f, 0f, 0f));
            Glaive(r, mDeepDk, mSilver, mAqua, def.bladeColor);
            DRarity(r, b, def.rarity, ElementChart.ColorOf(def.element), mSilver, 0.165f * 0.86f);
            HeadY = (b.hipY + b.neck.y + b.hc.y) * b.scale;
        }

        void Glaive(PremiumRig r, Material haft, Material silver, Material aqua, Color bc)
        {
            r.grip = PremiumRig.Grip.TwoHand;
            r.handleOffset = -0.34f;
            swordRest = Quaternion.Euler(-65f, 25f, 0f);
            var sp = SwordPivot;
            sp.localRotation = swordRest;
            r.poseWeapon = swordRest.eulerAngles;
            Part(PrimitiveType.Cylinder, sp, new Vector3(0f, 0f, 0.2f), new Vector3(0.042f, 0.95f, 0.042f), haft, new Vector3(90f, 0f, 0f));
            for (int i = 0; i < 3; i++) Band(sp, new Vector3(0f, 0f, -0.5f + i * 0.55f), 0.024f, 0.03f, 0.007f, silver, null, new Vector3(90f, 0f, 0f));
            Ball(sp, new Vector3(0f, 0f, -0.75f), new Vector3(0.05f, 0.05f, 0.08f), silver);
            // A long curved blade, silver with an aqua edge, above a ring that holds a turning water orb.
            var ring = J("WaterRing", sp, new Vector3(0f, 0f, 1.12f));
            MeshFactory.MeshObject(MeshFactory.Ring(0.8f), ring, Vector3.zero, Vector3.one * 0.2f, MaterialFactory.Additive(new Color(bc.r, bc.g, bc.b, 0.8f)), false).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            ring.gameObject.AddComponent<Spinner>().DegreesPerSecond = new Vector3(0f, 0f, 90f);
            Band(sp, new Vector3(0f, 0f, 1.12f), 0.05f, 0.05f, 0.012f, silver, null, new Vector3(90f, 0f, 0f));
            for (int k = 0; k < 7; k++)
            {
                float t = k / 6f;
                float bend = t * t * 0.12f;
                var seg = Part(PrimitiveType.Cube, sp, new Vector3(0f, bend, 1.2f + t * 0.5f), new Vector3(0.018f, 0.1f - t * 0.05f, 0.09f), PM(new Color(0.84f, 0.87f, 0.92f), 0.01f), new Vector3(-t * 25f, 0f, 0f));
                Part(PrimitiveType.Cube, seg.transform, new Vector3(0f, -0.52f, 0f), new Vector3(0.8f, 0.14f, 1f), aqua);
            }
            // The orb for the free hand (the rig's showcase pose holds it out, palm up).
            var orb = MeshFactory.MeshObject(MeshFactory.SmoothSphere(), r.hand[1], new Vector3(0f, -0.12f, 0.08f), Vector3.one * 0.1f, MaterialFactory.Additive(new Color(bc.r, bc.g, bc.b, 0.55f)), false);
            r.castGlow = orb.transform;
            PremiumTrail(1.7f, bc);
        }
    }
}
