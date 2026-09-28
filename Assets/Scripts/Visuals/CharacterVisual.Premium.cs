using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// Premium character builder — the quality test for the new art standard. Instead of a chibi bell body, these
    /// fighters have a jointed rig (<see cref="PremiumRig"/>): shoulders, elbows, hands with fingers and thumbs,
    /// hips, knees and booted feet. On top of that they get layered outfits, a detailed weapon with a handle, guard
    /// and fittings, a face with irises, highlights, lashes and brows, and a three-colour element palette with an
    /// element-tinted rim light and shadow. Rarity shows in the design itself: more trims, gold and ornaments the
    /// rarer the fighter is.
    ///
    ///   Kaito  (Rare, wind)       fast twin-blade striker — green / white / gold
    ///   Oboro  (Epic, flame)      heavy iron-club bruiser — crimson / black / orange
    ///   Shion  (Legendary, thunder) floating staff caster — navy / yellow / electric purple
    /// </summary>
    public partial class CharacterVisual
    {
        public static bool IsPremium(string id) { return id == "kaito_gale" || id == "oboro_iron" || id == "shion_storm"; }

        PremiumRig rig;
        Color pShadow = new Color(0.55f, 0.5f, 0.72f), pRim = Color.white;

        void BuildPremium(CharacterDefinition def)
        {
            Motion = def.motion;
            Weapon = def.weapon;
            switch (def.id)
            {
                case "kaito_gale": BuildKaito(def); break;
                case "oboro_iron": BuildOboro(def); break;
                default: BuildShion(def); break;
            }
            if (rig != null) rig.Solve(0f);
        }

        // ------------------------------------------------------------------ Helpers

        Material PM(Color c, float outline = 0.014f) { return PMe(c, outline, null); }

        /// <summary>Cel material with the character's element rim light and palette-tinted shadow.</summary>
        Material PMe(Color c, float outline, Color? emission)
        {
            var m = MaterialFactory.Toon(c, outline, emission);
            if (m.HasProperty("_ShadowColor")) m.SetColor("_ShadowColor", pShadow);
            if (m.HasProperty("_RimColor")) m.SetColor("_RimColor", pRim);
            if (m.HasProperty("_RimPower")) m.SetFloat("_RimPower", 2.4f);
            return m;
        }

        static Transform J(string n, Transform p, Vector3 pos)
        {
            var t = new GameObject(n).transform;
            t.SetParent(p, false);
            t.localPosition = pos;
            return t;
        }

        /// <summary>A rounded tapering limb/sleeve hanging from the joint along −Y.</summary>
        GameObject Taper(Transform p, Vector3 pos, float len, float rTop, float rBot, Material m, Vector3? scale = null, Vector3? euler = null)
        {
            string key = "pt" + len.ToString("F3") + "_" + rTop.ToString("F3") + "_" + rBot.ToString("F3");
            var mesh = MeshFactory.Lathe(key, new[]
            {
                new Vector2(0f, -len - rBot * 0.55f), new Vector2(rBot * 0.75f, -len - rBot * 0.4f), new Vector2(rBot, -len),
                new Vector2(Mathf.Lerp(rBot, rTop, 0.5f) * 1.04f, -len * 0.5f), new Vector2(rTop, 0f),
                new Vector2(rTop * 0.75f, rTop * 0.4f), new Vector2(0f, rTop * 0.55f)
            }, 20);
            var go = MeshFactory.MeshObject(mesh, p, pos, scale ?? Vector3.one, m);
            if (euler.HasValue) go.transform.localRotation = Quaternion.Euler(euler.Value);
            Add(go);
            return go;
        }

        /// <summary>A band wrapped around a limb, waist or head (radius r, height h, thickness t).</summary>
        GameObject Band(Transform p, Vector3 pos, float r, float h, float t, Material m, Vector3? scale = null, Vector3? euler = null)
        {
            string key = "pb" + r.ToString("F3") + "_" + h.ToString("F3") + "_" + t.ToString("F3");
            var mesh = MeshFactory.Lathe(key, new[]
            {
                new Vector2(r, -h * 0.5f), new Vector2(r + t, -h * 0.25f), new Vector2(r + t, h * 0.25f), new Vector2(r, h * 0.5f)
            }, 28);
            var go = MeshFactory.MeshObject(mesh, p, pos, scale ?? Vector3.one, m);
            if (euler.HasValue) go.transform.localRotation = Quaternion.Euler(euler.Value);
            Add(go);
            return go;
        }

        GameObject Shell(Transform p, string key, Vector2[] profile, Vector3 pos, Vector3 scale, Material m, int seg = 32)
        {
            var go = MeshFactory.MeshObject(MeshFactory.Lathe(key, profile, seg), p, pos, scale, m);
            Add(go);
            return go;
        }

        /// <summary>A pivot on the surface of the head ellipsoid, facing outward (for eyes, brows, mouth, marks).</summary>
        Transform OnFace(Vector3 c, Vector3 rad, float x, float y, float push = 0f)
        {
            float zz = 1f - (x * x) / (rad.x * rad.x) - (y * y) / (rad.y * rad.y);
            float z = rad.z * Mathf.Sqrt(Mathf.Max(0.02f, zz));
            Vector3 n = new Vector3(x / (rad.x * rad.x), y / (rad.y * rad.y), z / (rad.z * rad.z)).normalized;
            var t = new GameObject("Face").transform;
            t.SetParent(head, false);
            t.localPosition = c + new Vector3(x, y, z) + n * push;
            t.localRotation = Quaternion.LookRotation(n, Vector3.up);
            return t;
        }

        /// <summary>
        /// Anime eyes: sclera, two-tone iris, pupil, two highlights, a thick upper lash line (with a lid for calm or
        /// fierce looks), a lower lash, and a brow. tilt &gt; 0 lifts the outer corners; brow &gt; 0 is angry.
        /// </summary>
        void PremiumEyes(Vector3 hc, Vector3 hr, float x, float y, float w, float h, Color iris, Color irisLow, float lid, float tilt,
            bool flick, float brow, float browThick, Color browC, Material skin)
        {
            var white = PM(new Color(0.99f, 0.99f, 1f), 0f);
            var mIris = PMe(iris, 0f, iris * 0.18f);
            var mLow = PMe(irisLow, 0f, irisLow * 0.3f);
            var ink = PM(new Color(0.05f, 0.04f, 0.07f), 0f);
            var shine = MaterialFactory.Toon(Color.white, 0f, new Color(0.8f, 0.8f, 0.8f));
            var lowLash = PM(Color.Lerp(browC, new Color(0.6f, 0.3f, 0.3f), 0.5f), 0f);
            var mBrow = PM(browC, 0f);
            for (int s = -1; s <= 1; s += 2)
            {
                var e = OnFace(hc, hr, s * x, y);
                e.localRotation *= Quaternion.Euler(0f, 0f, s * tilt);
                Ball(e, new Vector3(0f, 0f, -0.006f), new Vector3(w, h, 0.032f), white);
                Ball(e, new Vector3(0f, -h * 0.05f, -0.002f), new Vector3(w * 0.7f, h * 0.82f, 0.03f), mIris);
                Ball(e, new Vector3(0f, -h * 0.22f, 0f), new Vector3(w * 0.5f, h * 0.32f, 0.028f), mLow);
                Ball(e, new Vector3(0f, -h * 0.02f, 0.002f), new Vector3(w * 0.3f, h * 0.42f, 0.026f), ink);
                Ball(e, new Vector3(-w * 0.16f, h * 0.18f, 0.006f), new Vector3(w * 0.26f, h * 0.24f, 0.02f), shine);
                Ball(e, new Vector3(w * 0.14f, -h * 0.24f, 0.006f), new Vector3(w * 0.12f, h * 0.1f, 0.02f), shine);
                float lidBottom = h * 0.5f - lid * h;
                if (lid > 0f) Ball(e, new Vector3(0f, lidBottom + h * 0.35f, 0f), new Vector3(w * 1.15f, h * 0.7f, 0.036f), skin);
                Part(PrimitiveType.Capsule, e, new Vector3(0f, lidBottom, 0.01f), new Vector3(0.026f, w * 0.58f, 0.022f), ink, new Vector3(0f, 0f, 90f));
                if (flick)
                {
                    Part(PrimitiveType.Capsule, e, new Vector3(s * w * 0.52f, lidBottom + 0.012f, 0.008f), new Vector3(0.014f, 0.03f, 0.014f), ink, new Vector3(0f, 0f, -50f * s));
                    Part(PrimitiveType.Capsule, e, new Vector3(s * w * 0.4f, lidBottom + 0.022f, 0.009f), new Vector3(0.012f, 0.024f, 0.012f), ink, new Vector3(0f, 0f, -30f * s));
                }
                Part(PrimitiveType.Capsule, e, new Vector3(0f, -h * 0.47f, 0.004f), new Vector3(0.011f, w * 0.28f, 0.011f), lowLash, new Vector3(0f, 0f, 90f));
                var b = OnFace(hc, hr, s * (x + 0.01f), y + h * 0.5f + 0.055f);
                Part(PrimitiveType.Capsule, b, new Vector3(0f, 0f, 0.004f), new Vector3(browThick, 0.062f, 0.016f), mBrow, new Vector3(0f, 0f, 90f + s * brow));
            }
        }

        void Blush(Vector3 hc, Vector3 hr, float alpha)
        {
            var blush = MaterialFactory.Transparent(new Color(1f, 0.45f, 0.45f, alpha));
            for (int s = -1; s <= 1; s += 2)
            {
                var f = OnFace(hc, hr, s * 0.2f, -0.12f);
                MeshFactory.MeshObject(MeshFactory.SmoothSphere(), f, Vector3.zero, new Vector3(0.09f, 0.045f, 0.01f), blush, false);
            }
        }

        /// <summary>A hand at the wrist joint: palm, cuff, and either a fist (knuckles + wrapped thumb) or open fingers.</summary>
        void PremiumHand(Transform h, Material glove, Material fingers, int side, bool fist, float size, Material plate = null)
        {
            var g = J("HandShape", h, Vector3.zero);
            g.localScale = Vector3.one * size;
            Ball(g, new Vector3(0f, -0.05f, 0.005f), new Vector3(0.085f, 0.1f, 0.078f), glove);
            Band(g, new Vector3(0f, -0.004f, 0f), 0.042f, 0.03f, 0.01f, glove);
            if (fist)
            {
                for (int k = 0; k < 4; k++)
                    Ball(g, new Vector3(-0.027f + k * 0.018f, -0.094f + (k == 0 || k == 3 ? 0.006f : 0f), 0.024f), new Vector3(0.024f, 0.03f, 0.034f), fingers);
                Ball(g, new Vector3(-side * 0.03f, -0.072f, 0.036f), new Vector3(0.028f, 0.045f, 0.028f), fingers, new Vector3(0f, 0f, side * 30f));
                if (plate != null) Part(PrimitiveType.Cube, g, new Vector3(side * 0.042f, -0.07f, 0.01f), new Vector3(0.01f, 0.03f, 0.07f), plate);
            }
            else
            {
                for (int k = 0; k < 4; k++)
                    Part(PrimitiveType.Capsule, g, new Vector3(-0.027f + k * 0.018f, -0.12f + (k == 0 || k == 3 ? 0.012f : 0f), 0.008f), new Vector3(0.018f, 0.034f, 0.018f), fingers, new Vector3(-12f, 0f, (k - 1.5f) * 7f));
                Part(PrimitiveType.Capsule, g, new Vector3(-side * 0.046f, -0.066f, 0.024f), new Vector3(0.019f, 0.03f, 0.019f), fingers, new Vector3(0f, 0f, side * 42f));
            }
        }

        void PremiumTrail(float z, Color c)
        {
            var tipT = new GameObject("Trail");
            tipT.transform.SetParent(SwordPivot, false);
            tipT.transform.localPosition = new Vector3(0f, 0f, z);
            Trail = tipT.AddComponent<TrailRenderer>();
            SetupTrail(Trail, c, 1f);
        }

        PremiumRig NewRig(float hipY, float hipW, float legA, float legB, float ankle, Vector3 shoulder, float armA, float armB, Vector3 neck)
        {
            var r = gameObject.AddComponent<PremiumRig>();
            r.cv = this;
            r.body = J("Body", Model, Vector3.zero);
            r.pelvis = J("Pelvis", r.body, new Vector3(0f, hipY, 0f));
            r.torso = J("Torso", r.body, new Vector3(0f, hipY + 0.02f, 0f));
            head = J("Head", r.body, new Vector3(0f, hipY + 0.02f + neck.y, 0f));
            r.head = head;
            for (int i = 0; i < 2; i++)
            {
                string s = i == 0 ? "R" : "L";
                r.upper[i] = J("UpperArm" + s, r.body, Vector3.zero);
                r.lower[i] = J("Forearm" + s, r.body, Vector3.zero);
                r.hand[i] = J("Hand" + s, r.body, Vector3.zero);
                r.thigh[i] = J("Thigh" + s, r.body, Vector3.zero);
                r.shin[i] = J("Shin" + s, r.body, Vector3.zero);
                r.foot[i] = J("Foot" + s, r.body, Vector3.zero);
            }
            r.hipY = hipY; r.hipW = hipW; r.legA = legA; r.legB = legB; r.ankle = ankle;
            r.shoulder = shoulder; r.armA = armA; r.armB = armB; r.neck = neck;
            SwordPivot = J("SwordPivot", r.body, Vector3.zero);
            rig = r;
            return r;
        }

        /// <summary>A short curved sword along +Z: wrapped grip, square guard, collar, steel blade with an edge line and tip.</summary>
        void Kodachi(Transform parent, Vector3 pos, Quaternion rot, float len, Material steel, Material edge, Material guard, Material wrapA, Material wrapB)
        {
            var k = J("Kodachi", parent, pos);
            k.localRotation = rot;
            Part(PrimitiveType.Cube, k, new Vector3(0f, 0f, -0.01f), new Vector3(0.036f, 0.046f, 0.22f), wrapA);
            for (int i = 0; i < 4; i++)
                Part(PrimitiveType.Cube, k, new Vector3(0f, 0f, -0.09f + i * 0.052f), new Vector3(0.04f, 0.04f, 0.018f), wrapB, new Vector3(0f, 0f, 45f));
            Ball(k, new Vector3(0f, 0f, -0.13f), new Vector3(0.05f, 0.056f, 0.04f), guard);
            Part(PrimitiveType.Cube, k, new Vector3(0f, 0f, 0.11f), new Vector3(0.1f, 0.11f, 0.016f), guard);
            Part(PrimitiveType.Cube, k, new Vector3(0f, 0f, 0.128f), new Vector3(0.032f, 0.058f, 0.024f), guard);
            // Three blade sections with a gentle curve.
            float seg = len / 3f;
            for (int i = 0; i < 3; i++)
            {
                var b = J("Blade", k, new Vector3(0f, i * i * 0.004f, 0.14f + seg * (i + 0.5f)));
                b.localRotation = Quaternion.Euler(-2f - i * 2.5f, 0f, 0f);
                Part(PrimitiveType.Cube, b, Vector3.zero, new Vector3(0.018f, 0.058f, seg * 1.02f), steel);
                Part(PrimitiveType.Cube, b, new Vector3(0f, -0.026f, 0f), new Vector3(0.012f, 0.012f, seg * 1.02f), edge);
            }
            var tip = MeshFactory.MeshObject(MeshFactory.FacetCone(4), k, new Vector3(0f, 0.018f, 0.14f + len), new Vector3(0.018f, 0.1f, 0.056f), steel);
            tip.transform.localRotation = Quaternion.Euler(84f, 0f, 0f);
            Add(tip);
        }

        // ------------------------------------------------------------------ Kaito — fast melee (wind, Rare)

        void BuildKaito(CharacterDefinition def)
        {
            pShadow = new Color(0.52f, 0.62f, 0.64f);
            pRim = new Color(0.75f, 1f, 0.8f);
            Color green = new Color(0.13f, 0.58f, 0.34f), greenDk = new Color(0.07f, 0.3f, 0.2f), white = new Color(0.95f, 0.96f, 0.93f);
            Color gold = new Color(0.96f, 0.76f, 0.3f), hairC = new Color(0.1f, 0.3f, 0.19f), tipC = new Color(0.5f, 0.9f, 0.45f);
            var mGreen = PM(green); var mGreenDk = PM(greenDk); var mWhite = PM(white); var mGold = PM(gold, 0.008f);
            var mPants = PM(new Color(0.15f, 0.2f, 0.19f)); var mLeather = PM(new Color(0.34f, 0.22f, 0.14f));
            var mSkin = PM(def.skinTone); var mWrap = PM(new Color(0.88f, 0.87f, 0.8f), 0.01f); var mSole = PM(new Color(0.08f, 0.08f, 0.08f), 0.01f);
            var mBoot = PM(new Color(0.1f, 0.14f, 0.13f)); var mHair = PM(hairC); var mTip = PM(tipC, 0.01f);

            var r = NewRig(0.6f, 0.085f, 0.27f, 0.27f, 0.08f, new Vector3(0.185f, 0.3f, -0.01f), 0.2f, 0.19f, new Vector3(0f, 0.36f, 0f));
            r.stance = 0.12f; r.crouch = 0.07f; r.lean = 10f; r.tempo = 12.5f; r.stride = 0.19f; r.lift = 0.14f;
            r.idleBounce = 0.025f; r.idleBounceFreq = 2.4f; r.twistGain = 0.3f; r.swingDip = 0.06f; r.swingLunge = 12f;
            r.grip = PremiumRig.Grip.Twin;
            r.restHandR = new Vector3(0.25f, 0.02f, 0.12f);
            r.restHandL = new Vector3(-0.25f, 0.02f, 0.1f);
            var T = r.torso;
            Vector3 ts = new Vector3(1.05f, 1f, 0.82f);

            // Torso: white wrap top under a cropped green jacket, open at the front in a V with gold edges.
            Shell(T, "kt_chest", new[] { new Vector2(0f, -0.04f), new Vector2(0.118f, -0.03f), new Vector2(0.125f, 0.05f), new Vector2(0.142f, 0.15f),
                new Vector2(0.152f, 0.23f), new Vector2(0.132f, 0.3f), new Vector2(0.075f, 0.345f), new Vector2(0f, 0.355f) }, Vector3.zero, ts, mWhite);
            Shell(T, "kt_jacket", new[] { new Vector2(0.15f, 0.1f), new Vector2(0.158f, 0.16f), new Vector2(0.166f, 0.235f), new Vector2(0.146f, 0.305f),
                new Vector2(0.085f, 0.35f), new Vector2(0f, 0.36f) }, Vector3.zero, new Vector3(1.06f, 1f, 0.85f), mGreen);
            for (int s = -1; s <= 1; s += 2)
            {
                var panel = Part(PrimitiveType.Cube, T, new Vector3(s * 0.03f, 0.25f, 0.118f), new Vector3(0.07f, 0.16f, 0.012f), mWhite, new Vector3(-16f, 0f, s * 24f));
                Part(PrimitiveType.Cube, panel.transform, new Vector3(s * 0.5f, 0f, 0.2f), new Vector3(0.2f, 1f, 1.2f), mGold);
            }
            // Jacket hem trim, high collar with a gold rim, and a long white scarf.
            Band(T, new Vector3(0f, 0.105f, 0f), 0.15f, 0.02f, 0.008f, mGreenDk, new Vector3(1.06f, 1f, 0.85f));
            Shell(T, "kt_collar", new[] { new Vector2(0.075f, 0.3f), new Vector2(0.088f, 0.34f), new Vector2(0.1f, 0.4f) }, Vector3.zero, Vector3.one, mGreen);
            Band(T, new Vector3(0f, 0.4f, 0f), 0.1f, 0.014f, 0.006f, mGold);
            Band(T, new Vector3(0f, 0.325f, 0f), 0.108f, 0.05f, 0.022f, mWhite);
            var tails = J("ScarfTails", T, new Vector3(0.04f, 0.33f, -0.11f));
            tails.localRotation = Quaternion.Euler(22f, 0f, -6f);
            for (int k = 0; k < 2; k++)
            {
                Transform prev = J("Tail" + k, tails, new Vector3(-k * 0.07f, 0f, 0f));
                prev.localRotation = Quaternion.Euler(8f + k * 6f, 0f, k == 0 ? -6f : 8f);
                for (int sgm = 0; sgm < 3; sgm++)
                {
                    var tail = Part(PrimitiveType.Cube, prev, new Vector3(0f, -0.13f, 0f), new Vector3(0.085f - sgm * 0.012f, 0.27f, 0.016f), mWhite);
                    if (sgm == 2) Part(PrimitiveType.Cube, tail.transform, new Vector3(0f, -0.4f, 0f), new Vector3(1.04f, 0.12f, 1.2f), mGold);
                    var sway = prev.gameObject.AddComponent<Sway>();
                    sway.Amount = 5f + sgm * 4f;
                    sway.Speed = 1.6f + k * 0.2f;
                    var next = J("Seg", prev, new Vector3(0f, -0.26f, 0f));
                    next.localRotation = Quaternion.Euler(12f, 0f, 0f);
                    prev = next;
                }
            }
            // Belt with a gold buckle, pouches and a knotted green sash.
            Band(T, new Vector3(0f, 0.03f, 0f), 0.128f, 0.05f, 0.014f, mLeather, ts);
            Part(PrimitiveType.Cube, T, new Vector3(0f, 0.03f, 0.118f), new Vector3(0.06f, 0.05f, 0.02f), mGold);
            for (int s = -1; s <= 1; s += 2)
            {
                Ball(T, new Vector3(s * 0.12f, 0f, -0.07f), new Vector3(0.075f, 0.085f, 0.06f), mLeather);
                Ball(T, new Vector3(s * 0.132f, 0.02f, -0.065f), new Vector3(0.022f, 0.022f, 0.022f), mGold);
            }
            Ball(T, new Vector3(-0.12f, 0.035f, 0.07f), new Vector3(0.07f, 0.06f, 0.05f), mGreen);
            var sash = Part(PrimitiveType.Cube, T, new Vector3(-0.13f, -0.06f, 0.08f), new Vector3(0.045f, 0.16f, 0.012f), mGreen, new Vector3(-6f, 0f, 10f));
            sash.AddComponent<Sway>().Amount = 6f;

            // Hips and legs: short hakama flaring over the thighs, dark leggings with white shin wraps, split-toe boots.
            Shell(r.pelvis, "kt_hips", new[] { new Vector2(0.16f, -0.13f), new Vector2(0.155f, -0.06f), new Vector2(0.135f, 0f), new Vector2(0.12f, 0.03f) },
                Vector3.zero, new Vector3(1.05f, 1f, 0.86f), mPants);
            for (int i = 0; i < 2; i++)
            {
                Taper(r.thigh[i], Vector3.zero, 0.24f, 0.078f, 0.1f, mPants);
                Band(r.thigh[i], new Vector3(0f, -0.235f, 0f), 0.096f, 0.018f, 0.008f, mGreenDk);
                Ball(r.shin[i], Vector3.zero, Vector3.one * 0.1f, mPants);
                Taper(r.shin[i], Vector3.zero, 0.25f, 0.052f, 0.042f, mPants);
                for (int k = 0; k < 4; k++)
                    Band(r.shin[i], new Vector3(0f, -0.07f - k * 0.045f, 0f), 0.05f - k * 0.003f, 0.026f, 0.009f, mWrap, null, new Vector3(0f, 0f, k % 2 == 0 ? 12f : -12f));
                var f = r.foot[i];
                Ball(f, new Vector3(0f, -0.035f, 0.045f), new Vector3(0.11f, 0.085f, 0.2f), mBoot);
                Ball(f, new Vector3(0f, -0.045f, 0.11f), new Vector3(0.1f, 0.065f, 0.1f), mBoot);
                Part(PrimitiveType.Cube, f, new Vector3(0f, -0.042f, 0.158f), new Vector3(0.006f, 0.05f, 0.04f), mSole);
                Part(PrimitiveType.Cube, f, new Vector3(0f, -0.068f, 0.05f), new Vector3(0.106f, 0.022f, 0.23f), mSole);
                Band(f, new Vector3(0f, 0.005f, 0f), 0.046f, 0.035f, 0.01f, mWrap);
            }

            // Arms: jacket sleeves with rolled white cuffs, wrapped forearms and fingerless gloves.
            for (int i = 0; i < 2; i++)
            {
                int side = i == 0 ? 1 : -1;
                Ball(r.upper[i], new Vector3(0f, -0.02f, 0f), Vector3.one * 0.125f, mGreen);
                Taper(r.upper[i], Vector3.zero, 0.19f, 0.058f, 0.05f, mGreen);
                Band(r.upper[i], new Vector3(0f, -0.185f, 0f), 0.052f, 0.036f, 0.012f, mWhite);
                Band(r.upper[i], new Vector3(0f, -0.2f, 0f), 0.056f, 0.008f, 0.005f, mGold);
                Ball(r.lower[i], Vector3.zero, Vector3.one * 0.09f, mWrap);
                Taper(r.lower[i], Vector3.zero, 0.17f, 0.045f, 0.038f, mWrap);
                for (int k = 0; k < 3; k++) Band(r.lower[i], new Vector3(0f, -0.05f - k * 0.045f, 0f), 0.044f - k * 0.002f, 0.01f, 0.006f, mLeather);
                PremiumHand(r.hand[i], mLeather, mSkin, side, true, 1f, mGold);
            }

            // Head.
            Vector3 hc = new Vector3(0f, 0.33f, 0.02f), hr = new Vector3(0.37f, 0.36f, 0.35f);
            Ball(head, new Vector3(0f, 0.04f, 0f), new Vector3(0.1f, 0.16f, 0.1f), mSkin);
            Ball(head, hc, hr * 2f, mSkin);
            for (int s = -1; s <= 1; s += 2) Ball(head, hc + new Vector3(s * 0.36f, -0.03f, -0.01f), new Vector3(0.07f, 0.12f, 0.08f), mSkin);
            PremiumEyes(hc, hr, 0.125f, -0.035f, 0.115f, 0.135f, new Color(0.3f, 0.78f, 0.35f), new Color(0.95f, 0.9f, 0.4f), 0.06f, 6f, false, 14f, 0.022f, new Color(0.06f, 0.16f, 0.1f), mSkin);
            Blush(hc, hr, 0.3f);
            var mMouth = PM(new Color(0.3f, 0.1f, 0.1f), 0f);
            var mTeeth = PM(new Color(0.99f, 0.98f, 0.96f), 0f);
            var nose = OnFace(hc, hr, 0f, -0.09f);
            Ball(nose, Vector3.zero, new Vector3(0.022f, 0.016f, 0.012f), PM(Color.Lerp(def.skinTone, new Color(0.8f, 0.5f, 0.45f), 0.4f), 0f));
            // A cocky grin with one fang.
            var mo = OnFace(hc, hr, 0.012f, -0.17f);
            Ball(mo, new Vector3(0f, 0f, -0.004f), new Vector3(0.085f, 0.045f, 0.02f), mMouth);
            Ball(mo, new Vector3(0f, 0.011f, 0.001f), new Vector3(0.07f, 0.016f, 0.016f), mTeeth);
            Ball(mo, new Vector3(0f, -0.012f, 0f), new Vector3(0.04f, 0.018f, 0.016f), PM(new Color(0.9f, 0.45f, 0.45f), 0f));
            ConePart(mo, new Vector3(0.022f, 0.006f, 0.004f), new Vector3(0.012f, 0.02f, 0.01f), mTeeth, new Vector3(180f, 0f, 0f));
            // A plaster on the cheek.
            var pl = OnFace(hc, hr, -0.2f, -0.1f);
            Part(PrimitiveType.Cube, pl, new Vector3(0f, 0f, 0.004f), new Vector3(0.07f, 0.022f, 0.008f), mWrap, new Vector3(0f, 0f, 35f));
            Part(PrimitiveType.Cube, pl, new Vector3(0f, 0f, 0.006f), new Vector3(0.07f, 0.022f, 0.008f), mWrap, new Vector3(0f, 0f, -35f));

            // Hair: swept-back spikes with bright tips, spiky bangs under a white headband, a long high ponytail.
            Ball(head, hc + new Vector3(0f, 0.06f, -0.045f), new Vector3(0.78f, 0.7f, 0.76f), mHair);
            for (int i = 0; i < 7; i++)
            {
                float az = -72f + i * 24f;
                HairSpike(hc + Quaternion.Euler(0f, az, 0f) * new Vector3(0f, 0.26f, -0.08f), new Vector3(-58f - Mathf.Abs(az) * 0.25f, az, 0f), 0.2f, 0.42f, mHair, mTip);
            }
            for (int i = 0; i < 6; i++)
            {
                float az = -60f + i * 24f;
                HairSpike(hc + Quaternion.Euler(0f, az, 0f) * new Vector3(0f, 0.12f, -0.28f), new Vector3(-85f, az, 0f), 0.18f, 0.34f, mHair, mTip);
            }
            for (int i = 0; i < 3; i++)
                HairSpike(hc + new Vector3(-0.08f + i * 0.08f, 0.33f, 0.04f), new Vector3(-40f, (i - 1) * 25f, 0f), 0.16f, 0.3f, mHair, mTip);
            for (int i = 0; i < 4; i++)
                ConePart(head, hc + new Vector3(-0.15f + i * 0.1f, 0.16f, 0.31f), new Vector3(0.1f, 0.14f, 0.07f), mHair, new Vector3(150f, 0f, -12f + i * 8f));
            for (int s = -1; s <= 1; s += 2)
                ConePart(head, hc + new Vector3(s * 0.33f, 0.08f, 0.14f), new Vector3(0.1f, 0.3f, 0.08f), mHair, new Vector3(172f, 0f, -10f * s));
            Band(head, hc + new Vector3(0f, 0.12f, -0.02f), 0.388f, 0.06f, 0.012f, mWhite, new Vector3(1f, 1f, 0.98f), new Vector3(-8f, 0f, 0f));
            Part(PrimitiveType.Cube, head, hc + new Vector3(0f, 0.173f, 0.39f), new Vector3(0.09f, 0.05f, 0.014f), mGold, new Vector3(-8f, 0f, 0f));
            Ball(head, hc + new Vector3(0f, 0.173f, 0.398f), new Vector3(0.03f, 0.03f, 0.014f), PMe(green, 0f, green * 0.3f));
            var bandTails = J("BandTails", head, hc + new Vector3(0.03f, 0.07f, -0.38f));
            bandTails.localRotation = Quaternion.Euler(30f, 0f, 0f);
            for (int k = 0; k < 2; k++)
            {
                var bt = J("BandTail", bandTails, new Vector3(-k * 0.06f, 0f, 0f));
                bt.localRotation = Quaternion.Euler(0f, 0f, k == 0 ? -12f : 14f);
                Part(PrimitiveType.Cube, bt, new Vector3(0f, -0.13f, 0f), new Vector3(0.05f, 0.26f, 0.012f), mWhite);
                var sw = bt.gameObject.AddComponent<Sway>();
                sw.Amount = 10f;
                sw.Speed = 1.8f + k * 0.3f;
            }
            var pony = J("Ponytail", head, hc + new Vector3(0f, 0.17f, -0.36f));
            pony.localRotation = Quaternion.Euler(40f, 0f, 0f);
            Band(pony, Vector3.zero, 0.05f, 0.045f, 0.014f, mGold);
            Ball(pony, new Vector3(0f, -0.12f, 0f), new Vector3(0.16f, 0.28f, 0.16f), mHair);
            var p2 = J("Pony2", pony, new Vector3(0f, -0.24f, 0f));
            p2.localRotation = Quaternion.Euler(15f, 0f, 0f);
            p2.gameObject.AddComponent<Sway>().Amount = 7f;
            Ball(p2, new Vector3(0f, -0.14f, 0f), new Vector3(0.14f, 0.3f, 0.14f), mHair);
            var p3 = J("Pony3", p2, new Vector3(0f, -0.27f, 0f));
            p3.localRotation = Quaternion.Euler(12f, 0f, 0f);
            p3.gameObject.AddComponent<Sway>().Amount = 10f;
            Ball(p3, new Vector3(0f, -0.1f, 0f), new Vector3(0.11f, 0.24f, 0.11f), mHair);
            ConePart(p3, new Vector3(0f, -0.18f, 0f), new Vector3(0.1f, 0.16f, 0.1f), mTip, new Vector3(180f, 0f, 0f));
            HeadY = (r.hipY + 0.02f + r.neck.y + hc.y);

            // Twin kodachi: normal grip in the right hand, reverse grip along the left forearm.
            var steel = PM(new Color(0.8f, 0.83f, 0.86f), 0.01f);
            var edge = PMe(new Color(0.85f, 1f, 0.9f), 0f, new Color(0.15f, 0.4f, 0.25f));
            SwordPivot.localRotation = swordRest;
            Kodachi(SwordPivot, Vector3.zero, Quaternion.identity, 0.6f, steel, edge, mGold, mGreenDk, mWhite);
            Kodachi(r.hand[1], new Vector3(-0.05f, -0.055f, 0f), Quaternion.Euler(-90f, 0f, 0f), 0.34f, steel, edge, mGold, mGreenDk, mWhite);
            PremiumTrail(0.66f, def.bladeColor);
        }

        void HairSpike(Vector3 pos, Vector3 euler, float w, float len, Material hair, Material tip)
        {
            ConePart(head, pos, new Vector3(w, len, w), hair, euler);
            Vector3 dir = Quaternion.Euler(euler) * Vector3.up;
            ConePart(head, pos + dir * len * 0.52f, new Vector3(w * 0.54f, len * 0.48f, w * 0.54f), tip, euler);
        }

        // ------------------------------------------------------------------ Oboro — heavy melee (flame, Epic)

        void BuildOboro(CharacterDefinition def)
        {
            pShadow = new Color(0.62f, 0.42f, 0.44f);
            pRim = new Color(1f, 0.6f, 0.3f);
            Color crimson = new Color(0.66f, 0.08f, 0.09f), crimsonDk = new Color(0.36f, 0.04f, 0.06f), black = new Color(0.1f, 0.08f, 0.09f);
            Color orange = new Color(1f, 0.5f, 0.12f), iron = new Color(0.26f, 0.26f, 0.3f), gold = new Color(0.92f, 0.7f, 0.28f);
            var mCrimson = PM(crimson); var mCrimsonDk = PM(crimsonDk); var mBlack = PM(black); var mOrange = PM(orange, 0.01f);
            var mIron = PM(iron); var mGold = PM(gold, 0.008f); var mSkin = PM(def.skinTone); var mHair = PM(new Color(0.12f, 0.08f, 0.08f));
            var mStubble = PM(Color.Lerp(def.skinTone, new Color(0.18f, 0.16f, 0.2f), 0.55f), 0.01f);
            var mGlove = PM(new Color(0.22f, 0.13f, 0.1f)); var mSole = PM(new Color(0.07f, 0.06f, 0.06f), 0.01f);
            var mEmber = PMe(orange, 0f, new Color(1f, 0.45f, 0.1f));

            var r = NewRig(0.62f, 0.12f, 0.28f, 0.27f, 0.09f, new Vector3(0.27f, 0.31f, -0.01f), 0.22f, 0.21f, new Vector3(0f, 0.38f, 0f));
            Model.localScale = Vector3.one * 1.1f;
            r.stance = 0.18f; r.crouch = 0.04f; r.lean = -3f; r.tempo = 8.5f; r.stride = 0.15f; r.lift = 0.1f; r.toeOut = 18f;
            r.weightShift = 0.025f; r.twistGain = 0.45f; r.swingDip = 0.1f; r.swingLunge = 16f;
            r.grip = PremiumRig.Grip.TwoHand;
            r.restHandR = new Vector3(0.27f, 0.26f, 0.22f);
            r.restHandL = new Vector3(-0.3f, 0.06f, 0.03f);
            swordRest = Quaternion.Euler(-108f, 32f, 0f);
            var T = r.torso;
            Vector3 ts = new Vector3(1.2f, 1f, 0.9f);

            // Torso: black kimono under a lacquered crimson chest plate with lacing lines and gold studs.
            Shell(T, "ob_chest", new[] { new Vector2(0f, -0.04f), new Vector2(0.16f, -0.03f), new Vector2(0.17f, 0.06f), new Vector2(0.2f, 0.17f),
                new Vector2(0.215f, 0.26f), new Vector2(0.19f, 0.32f), new Vector2(0.1f, 0.37f), new Vector2(0f, 0.38f) }, Vector3.zero, ts, mBlack);
            var plateProfile = new[] { new Vector2(0.18f, 0.07f), new Vector2(0.205f, 0.14f), new Vector2(0.222f, 0.23f), new Vector2(0.2f, 0.3f),
                new Vector2(0.13f, 0.345f), new Vector2(0f, 0.35f) };
            Vector3 ps = new Vector3(1.22f, 1f, 0.94f);
            Shell(T, "ob_plate", plateProfile, Vector3.zero, ps, mCrimson);
            float[] lines = { 0.12f, 0.18f, 0.24f };
            float[] lr = { 0.199f, 0.212f, 0.221f };
            for (int k = 0; k < 3; k++)
            {
                Band(T, new Vector3(0f, lines[k], 0f), lr[k], 0.012f, 0.005f, mBlack, ps);
                for (int j = -2; j <= 2; j++)
                {
                    float a = j * 0.32f;
                    Ball(T, new Vector3(Mathf.Sin(a) * lr[k] * ps.x, lines[k], Mathf.Cos(a) * (lr[k] + 0.006f) * ps.z), Vector3.one * 0.024f, mGold);
                }
            }
            Band(T, new Vector3(0f, 0.075f, 0f), 0.181f, 0.016f, 0.006f, mGold, ps);
            // Thick neck, black collar.
            Ball(T, new Vector3(0f, 0.36f, 0f), new Vector3(0.18f, 0.14f, 0.17f), mSkin);
            Shell(T, "ob_collar", new[] { new Vector2(0.11f, 0.3f), new Vector2(0.12f, 0.35f), new Vector2(0.11f, 0.38f) }, Vector3.zero, new Vector3(1.1f, 1f, 1f), mBlack);
            // Wide black obi with the sun buckle: a gold disc with rays and a glowing ember core.
            Band(T, new Vector3(0f, 0.02f, 0f), 0.17f, 0.09f, 0.016f, mBlack, new Vector3(1.2f, 1f, 0.92f));
            Band(T, new Vector3(0f, 0.02f, 0f), 0.186f, 0.012f, 0.006f, mOrange, new Vector3(1.2f, 1f, 0.92f));
            var sun = J("SunBuckle", T, new Vector3(0f, 0.025f, 0.178f));
            var disc = MeshFactory.MeshObject(MeshFactory.FacetCylinder(16), sun, Vector3.zero, new Vector3(0.11f, 0.014f, 0.11f), mGold);
            disc.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            Add(disc);
            Ball(sun, new Vector3(0f, 0f, 0.012f), new Vector3(0.05f, 0.05f, 0.02f), mEmber);
            for (int k = 0; k < 8; k++)
            {
                float a = k * 45f * Mathf.Deg2Rad;
                Vector3 dir = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f);
                var ray = MeshFactory.MeshObject(MeshFactory.FacetCone(4), sun, dir * 0.055f, new Vector3(0.026f, 0.036f, 0.012f), mGold);
                ray.transform.localRotation = Quaternion.FromToRotation(Vector3.up, dir);
                Add(ray);
            }
            // A rope across the chest holding the one-shoulder cape.
            Part(PrimitiveType.Cylinder, T, new Vector3(-0.01f, 0.235f, 0.2f), new Vector3(0.024f, 0.2f, 0.024f), mOrange, new Vector3(-12f, 0f, 52f));
            Ball(T, new Vector3(-0.16f, 0.31f, 0.13f), Vector3.one * 0.05f, mGold);
            var cape = J("Cape", T, new Vector3(-0.14f, 0.33f, -0.15f));
            cape.localRotation = Quaternion.Euler(10f, 0f, 6f);
            float[] lens = { 0.62f, 0.72f, 0.56f, 0.68f, 0.5f };
            for (int k = 0; k < 5; k++)
            {
                var strip = J("Strip", cape, new Vector3(-0.1f + k * 0.055f, 0f, -k * 0.004f));
                strip.localRotation = Quaternion.Euler(4f + k * 1.5f, 0f, (k - 2) * 3f);
                Part(PrimitiveType.Cube, strip, new Vector3(0f, -lens[k] * 0.5f, 0f), new Vector3(0.07f, lens[k], 0.02f), mBlack);
                Part(PrimitiveType.Cube, strip, new Vector3(0f, -lens[k] * 0.5f, 0.012f), new Vector3(0.064f, lens[k] * 0.95f, 0.008f), mCrimsonDk);
                var jag = MeshFactory.MeshObject(MeshFactory.FacetCone(3), strip, new Vector3(0f, -lens[k], 0f), new Vector3(0.07f, 0.08f, 0.02f), mBlack);
                jag.transform.localRotation = Quaternion.Euler(180f, 0f, 0f);
                Add(jag);
                var sw = strip.gameObject.AddComponent<Sway>();
                sw.Amount = 4f + k;
                sw.Speed = 0.9f + k * 0.1f;
            }

            // Wide hakama with an orange hem and pleats; iron shin guards and heavy boots.
            Shell(r.pelvis, "ob_hakama", new[] { new Vector2(0.3f, -0.5f), new Vector2(0.302f, -0.47f), new Vector2(0.27f, -0.3f), new Vector2(0.22f, -0.12f),
                new Vector2(0.18f, 0f), new Vector2(0.17f, 0.03f) }, Vector3.zero, Vector3.one, mBlack);
            Band(r.pelvis, new Vector3(0f, -0.48f, 0f), 0.298f, 0.035f, 0.01f, mOrange);
            float[] pleats = { -50f, -25f, 0f, 25f, 50f, 155f, 180f, 205f };
            foreach (float az in pleats)
            {
                var pl = Part(PrimitiveType.Cube, r.pelvis, Quaternion.Euler(0f, az, 0f) * new Vector3(0f, -0.25f, 0.248f), new Vector3(0.008f, 0.5f, 0.008f), mCrimsonDk);
                pl.transform.localRotation = Quaternion.Euler(-13.5f, az, 0f);
            }
            for (int i = 0; i < 2; i++)
            {
                Taper(r.thigh[i], Vector3.zero, 0.26f, 0.1f, 0.085f, mBlack);
                Ball(r.shin[i], Vector3.zero, new Vector3(0.13f, 0.12f, 0.13f), mCrimson);
                Taper(r.shin[i], Vector3.zero, 0.24f, 0.07f, 0.06f, mBlack);
                var guardGo = Taper(r.shin[i], new Vector3(0f, -0.02f, 0.012f), 0.19f, 0.072f, 0.064f, mIron, new Vector3(1f, 1f, 1.05f));
                guardGo.name = "ShinGuard";
                Band(r.shin[i], new Vector3(0f, -0.05f, 0f), 0.076f, 0.014f, 0.006f, mGold);
                Band(r.shin[i], new Vector3(0f, -0.19f, 0f), 0.068f, 0.014f, 0.006f, mGold);
                var f = r.foot[i];
                Ball(f, new Vector3(0f, -0.035f, 0.05f), new Vector3(0.15f, 0.1f, 0.25f), mBlack);
                Ball(f, new Vector3(0f, -0.045f, 0.13f), new Vector3(0.13f, 0.08f, 0.1f), mIron);
                Part(PrimitiveType.Cube, f, new Vector3(0f, -0.078f, 0.055f), new Vector3(0.15f, 0.026f, 0.28f), mSole);
                Band(f, new Vector3(0f, 0.01f, 0f), 0.07f, 0.05f, 0.014f, mBlack);
            }

            // Arms: layered round pauldrons, black sleeves, bare forearms with lacquered bracers, big gloves.
            for (int i = 0; i < 2; i++)
            {
                int side = i == 0 ? 1 : -1;
                Taper(r.upper[i], Vector3.zero, 0.21f, 0.08f, 0.07f, mBlack);
                for (int k = 0; k < 3; k++)
                {
                    Ball(r.upper[i], new Vector3(side * 0.03f, 0.04f - k * 0.055f, 0f), new Vector3(0.28f - k * 0.035f, 0.12f, 0.26f - k * 0.03f), k == 1 ? mBlack : mCrimson, new Vector3(0f, 0f, -side * 18f));
                }
                Ball(r.upper[i], new Vector3(side * 0.06f, 0.09f, 0f), Vector3.one * 0.035f, mGold);
                Ball(r.lower[i], Vector3.zero, Vector3.one * 0.12f, mSkin);
                Taper(r.lower[i], Vector3.zero, 0.2f, 0.064f, 0.055f, mSkin);
                Taper(r.lower[i], new Vector3(0f, -0.07f, 0f), 0.11f, 0.068f, 0.062f, mCrimsonDk);
                Band(r.lower[i], new Vector3(0f, -0.07f, 0f), 0.069f, 0.014f, 0.006f, mGold);
                Band(r.lower[i], new Vector3(0f, -0.18f, 0f), 0.063f, 0.014f, 0.006f, mGold);
                PremiumHand(r.hand[i], mGlove, mGlove, side, true, 1.25f, mIron);
            }

            // Head: strong jaw, shaved sides, a flat top with a topknot, short beard, a scar and fierce amber eyes.
            Vector3 hc = new Vector3(0f, 0.32f, 0.02f), hr = new Vector3(0.34f, 0.34f, 0.33f);
            Ball(head, hc, hr * 2f, mSkin);
            Ball(head, hc + new Vector3(0f, -0.11f, 0.03f), new Vector3(0.62f, 0.46f, 0.58f), mSkin);
            for (int s = -1; s <= 1; s += 2) Ball(head, hc + new Vector3(s * 0.335f, -0.03f, -0.01f), new Vector3(0.08f, 0.13f, 0.09f), mSkin);
            Ball(head, hc + new Vector3(0f, 0.09f, -0.06f), new Vector3(0.71f, 0.6f, 0.64f), mStubble);
            Ball(head, hc + new Vector3(0f, 0.25f, -0.01f), new Vector3(0.6f, 0.24f, 0.6f), mHair);
            Ball(head, hc + new Vector3(0f, 0.24f, 0.18f), new Vector3(0.5f, 0.16f, 0.26f), mHair);
            Part(PrimitiveType.Capsule, head, hc + new Vector3(0f, 0.43f, 0.02f), new Vector3(0.09f, 0.12f, 0.09f), mHair, new Vector3(80f, 0f, 0f));
            Part(PrimitiveType.Cylinder, head, hc + new Vector3(0f, 0.43f, -0.07f), new Vector3(0.1f, 0.018f, 0.1f), mOrange, new Vector3(80f, 0f, 0f));
            Ball(head, hc + new Vector3(0f, -0.2f, 0.13f), new Vector3(0.46f, 0.26f, 0.3f), mHair);
            for (int s = -1; s <= 1; s += 2) Ball(head, hc + new Vector3(s * 0.3f, -0.06f, 0.07f), new Vector3(0.1f, 0.26f, 0.14f), mHair);
            PremiumEyes(hc, hr, 0.12f, -0.02f, 0.1f, 0.1f, new Color(1f, 0.58f, 0.14f), new Color(1f, 0.85f, 0.35f), 0.28f, 8f, false, 24f, 0.034f, new Color(0.08f, 0.05f, 0.05f), mSkin);
            var mMouth = PM(new Color(0.3f, 0.1f, 0.1f), 0f);
            for (int s = -1; s <= 1; s += 2)
            {
                var mu = OnFace(hc, hr, s * 0.055f, -0.125f);
                Part(PrimitiveType.Capsule, mu, new Vector3(0f, 0f, 0.006f), new Vector3(0.028f, 0.05f, 0.02f), mHair, new Vector3(0f, 0f, s * 68f));
            }
            var mo = OnFace(hc, hr, 0f, -0.17f);
            Part(PrimitiveType.Capsule, mo, new Vector3(0f, 0f, 0.002f), new Vector3(0.016f, 0.05f, 0.012f), mMouth, new Vector3(0f, 0f, 90f));
            for (int s = -1; s <= 1; s += 2)
                Part(PrimitiveType.Capsule, mo, new Vector3(s * 0.05f, -0.008f, 0.001f), new Vector3(0.012f, 0.016f, 0.01f), mMouth, new Vector3(0f, 0f, s * 50f));
            var nose = OnFace(hc, hr, 0f, -0.075f);
            Ball(nose, Vector3.zero, new Vector3(0.04f, 0.03f, 0.03f), mSkin);
            var scar = OnFace(hc, hr, 0.13f, 0.06f);
            Part(PrimitiveType.Capsule, scar, new Vector3(0f, 0f, 0.006f), new Vector3(0.014f, 0.085f, 0.01f), PM(new Color(0.9f, 0.55f, 0.55f), 0f), new Vector3(0f, 0f, 18f));
            Band(head, hc + new Vector3(-0.35f, -0.1f, 0f), 0.028f, 0.01f, 0.006f, mGold, null, new Vector3(0f, 0f, 90f));
            HeadY = (r.hipY + 0.02f + r.neck.y + hc.y) * 1.1f;

            // The kanabo: an iron club with a red-corded grip, gold bands, rows of studs and glowing ember cracks.
            SwordPivot.localRotation = swordRest;
            var sp = SwordPivot;
            var grip = Part(PrimitiveType.Cylinder, sp, new Vector3(0f, 0f, 0.02f), new Vector3(0.07f, 0.2f, 0.07f), mCrimsonDk, new Vector3(90f, 0f, 0f));
            grip.name = "Grip";
            for (int k = 0; k < 5; k++)
                Part(PrimitiveType.Cylinder, sp, new Vector3(0f, 0f, -0.14f + k * 0.075f), new Vector3(0.076f, 0.01f, 0.076f), mBlack, new Vector3(90f, 0f, 0f));
            Ball(sp, new Vector3(0f, 0f, -0.2f), new Vector3(0.1f, 0.1f, 0.08f), mGold);
            Band(sp, new Vector3(0f, 0f, -0.25f), 0.03f, 0.012f, 0.008f, mGold, null, new Vector3(0f, 90f, 90f));
            Part(PrimitiveType.Cylinder, sp, new Vector3(0f, 0f, 0.24f), new Vector3(0.13f, 0.025f, 0.13f), mGold, new Vector3(90f, 0f, 0f));
            var club = MeshFactory.MeshObject(MeshFactory.Lathe("ob_kanabo", new[] { new Vector2(0f, 0f), new Vector2(0.075f, 0.002f), new Vector2(0.085f, 0.1f),
                new Vector2(0.12f, 0.6f), new Vector2(0.135f, 0.95f), new Vector2(0.12f, 1.02f), new Vector2(0.06f, 1.06f), new Vector2(0f, 1.07f) }, 8),
                sp, new Vector3(0f, 0f, 0.25f), Vector3.one, mIron);
            club.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            Add(club);
            var studM = PM(new Color(0.6f, 0.6f, 0.64f), 0.008f);
            for (int row = 0; row < 5; row++)
            {
                float h = 0.2f + row * 0.18f;
                float rr = Mathf.Lerp(0.085f, 0.135f, Mathf.Clamp01((h - 0.1f) / 0.85f));
                for (int k = 0; k < 8; k++)
                {
                    float a = (k * 45f + (row % 2) * 22.5f) * Mathf.Deg2Rad;
                    Vector3 dir = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f);
                    var stud = MeshFactory.MeshObject(MeshFactory.FacetCone(4), sp, dir * rr * 0.97f + Vector3.forward * (0.25f + h), new Vector3(0.05f, 0.07f, 0.05f), studM);
                    stud.transform.localRotation = Quaternion.FromToRotation(Vector3.up, dir);
                    Add(stud);
                }
            }
            for (int k = 0; k < 4; k++)
            {
                float a = (22.5f + k * 90f) * Mathf.Deg2Rad;
                Vector3 dir = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f);
                for (int j = 0; j < 3; j++)
                {
                    float h = 0.4f + j * 0.18f;
                    float rr = Mathf.Lerp(0.085f, 0.135f, Mathf.Clamp01((h - 0.1f) / 0.85f));
                    var crack = Part(PrimitiveType.Cube, sp, dir * (rr * 0.93f + 0.003f) + Vector3.forward * (0.25f + h), new Vector3(0.01f, 0.01f, 0.12f), mEmber);
                    crack.transform.localRotation = Quaternion.LookRotation(Vector3.forward, dir) * Quaternion.Euler(0f, (j % 2 == 0 ? 18f : -18f), 0f);
                }
            }
            Band(sp, new Vector3(0f, 0f, 0.3f), 0.088f, 0.03f, 0.01f, mGold, null, new Vector3(90f, 0f, 0f));
            Band(sp, new Vector3(0f, 0f, 1.2f), 0.13f, 0.03f, 0.01f, mGold, null, new Vector3(90f, 0f, 0f));
            Ball(sp, new Vector3(0f, 0f, 1.32f), new Vector3(0.1f, 0.1f, 0.06f), mGold);
            PremiumTrail(1.25f, def.bladeColor);
        }

        // ------------------------------------------------------------------ Shion — ranged / elemental (thunder, Legendary)

        void BuildShion(CharacterDefinition def)
        {
            pShadow = new Color(0.5f, 0.48f, 0.74f);
            pRim = new Color(0.85f, 0.65f, 1f);
            Color navy = new Color(0.1f, 0.12f, 0.32f), navyDk = new Color(0.06f, 0.07f, 0.18f), yellow = new Color(1f, 0.84f, 0.22f);
            Color purple = new Color(0.6f, 0.32f, 1f), gold = new Color(0.96f, 0.8f, 0.4f), paper = new Color(0.97f, 0.94f, 0.82f);
            Color hairC = new Color(0.85f, 0.8f, 0.97f), hairDk = new Color(0.62f, 0.56f, 0.86f);
            var mNavy = PM(navy); var mNavyDk = PM(navyDk); var mYellow = PM(yellow); var mPurple = PM(purple); var mGold = PM(gold, 0.008f);
            var mWhite = PM(new Color(0.97f, 0.97f, 1f)); var mPaper = PM(paper, 0.006f); var mSkin = PM(def.skinTone);
            var mHair = PM(hairC); var mHairDk = PM(hairDk); var mWood = PM(new Color(0.42f, 0.28f, 0.18f), 0.01f);
            var mSpark = PMe(new Color(1f, 0.92f, 0.5f), 0f, new Color(1f, 0.85f, 0.4f));
            var mBolt = PMe(purple, 0f, purple * 0.8f);

            var r = NewRig(0.64f, 0.075f, 0.28f, 0.27f, 0.08f, new Vector3(0.165f, 0.29f, -0.01f), 0.2f, 0.19f, new Vector3(0f, 0.35f, 0f));
            r.stance = 0.07f; r.hover = 0.16f; r.lean = -2f; r.tempo = 7f; r.stride = 0.06f; r.lift = 0.03f;
            r.twistGain = 0.15f; r.swingDip = 0f; r.swingLunge = 4f;
            r.grip = PremiumRig.Grip.Caster;
            r.restHandR = new Vector3(0.27f, 0.12f, 0.16f);
            r.restHandL = new Vector3(-0.16f, 0.3f, 0.3f);
            swordRest = Quaternion.Euler(-78f, 12f, 0f);
            var T = r.torso;
            Vector3 ts = new Vector3(1f, 1f, 0.82f);

            // Torso: navy robe, white crossed collar with yellow edges, a purple capelet with gold trim and a thunder crest.
            Shell(T, "sh_chest", new[] { new Vector2(0f, -0.04f), new Vector2(0.11f, -0.03f), new Vector2(0.112f, 0.06f), new Vector2(0.128f, 0.15f),
                new Vector2(0.138f, 0.23f), new Vector2(0.12f, 0.29f), new Vector2(0.07f, 0.335f), new Vector2(0f, 0.345f) }, Vector3.zero, ts, mNavy);
            for (int s = -1; s <= 1; s += 2)
            {
                var panel = Part(PrimitiveType.Cube, T, new Vector3(s * 0.028f, 0.22f, 0.106f), new Vector3(0.06f, 0.17f, 0.012f), mWhite, new Vector3(-14f, 0f, s * 24f));
                Part(PrimitiveType.Cube, panel.transform, new Vector3(s * 0.5f, 0f, 0.2f), new Vector3(0.22f, 1f, 1.2f), mYellow);
            }
            Shell(T, "sh_capelet", new[] { new Vector2(0.25f, 0.16f), new Vector2(0.24f, 0.19f), new Vector2(0.195f, 0.26f), new Vector2(0.145f, 0.305f),
                new Vector2(0.09f, 0.34f), new Vector2(0f, 0.35f) }, Vector3.zero, new Vector3(1f, 1f, 0.9f), mPurple);
            Band(T, new Vector3(0f, 0.165f, 0f), 0.25f, 0.018f, 0.007f, mGold, new Vector3(1f, 1f, 0.9f));
            for (int k = 0; k < 10; k++)
            {
                float a = (k * 36f + 18f) * Mathf.Deg2Rad;
                Ball(T, new Vector3(Mathf.Sin(a) * 0.253f, 0.15f, Mathf.Cos(a) * 0.253f * 0.9f), new Vector3(0.022f, 0.03f, 0.022f), mGold);
            }
            Shell(T, "sh_collar", new[] { new Vector2(0.068f, 0.3f), new Vector2(0.075f, 0.35f), new Vector2(0.082f, 0.39f) }, Vector3.zero, Vector3.one, mNavyDk);
            Band(T, new Vector3(0f, 0.39f, 0f), 0.082f, 0.012f, 0.005f, mGold);
            var crest = J("Crest", T, new Vector3(0f, 0.25f, 0.215f));
            crest.localRotation = Quaternion.Euler(-40f, 0f, 0f);
            Part(PrimitiveType.Cube, crest, new Vector3(0.01f, 0.018f, 0f), new Vector3(0.018f, 0.045f, 0.01f), mGold, new Vector3(0f, 0f, -25f));
            Part(PrimitiveType.Cube, crest, new Vector3(-0.004f, -0.004f, 0f), new Vector3(0.03f, 0.012f, 0.01f), mGold, new Vector3(0f, 0f, 10f));
            Part(PrimitiveType.Cube, crest, new Vector3(-0.012f, -0.026f, 0f), new Vector3(0.018f, 0.045f, 0.01f), mGold, new Vector3(0f, 0f, -25f));
            // Yellow obi with a purple cord and a big bow at the back.
            Band(T, new Vector3(0f, 0.035f, 0f), 0.12f, 0.1f, 0.014f, mYellow, new Vector3(1f, 1f, 0.84f));
            Band(T, new Vector3(0f, 0.035f, 0f), 0.134f, 0.014f, 0.006f, mPurple, new Vector3(1f, 1f, 0.84f));
            Ball(T, new Vector3(0f, 0.035f, 0.113f), new Vector3(0.035f, 0.035f, 0.02f), mSpark);
            for (int s = -1; s <= 1; s += 2)
                Ball(T, new Vector3(s * 0.11f, 0.05f, -0.15f), new Vector3(0.17f, 0.12f, 0.07f), mYellow, new Vector3(0f, 0f, s * 22f));
            Ball(T, new Vector3(0f, 0.04f, -0.12f), new Vector3(0.07f, 0.075f, 0.06f), mYellow);
            for (int s = -1; s <= 1; s += 2)
            {
                var bowTail = J("BowTail", T, new Vector3(s * 0.03f, 0.02f, -0.13f));
                bowTail.localRotation = Quaternion.Euler(10f, 0f, s * 8f);
                Part(PrimitiveType.Cube, bowTail, new Vector3(0f, -0.16f, 0f), new Vector3(0.065f, 0.32f, 0.014f), mYellow);
                var sw = bowTail.gameObject.AddComponent<Sway>();
                sw.Amount = 6f;
                sw.Speed = 1.1f + s * 0.1f;
            }

            // Two-tier robe to the ankles: navy underneath, a shorter purple layer with gold hem, and a front panel.
            Shell(r.pelvis, "sh_robe", new[] { new Vector2(0.26f, -0.56f), new Vector2(0.262f, -0.53f), new Vector2(0.22f, -0.3f), new Vector2(0.16f, -0.1f),
                new Vector2(0.13f, 0f), new Vector2(0.12f, 0.04f) }, Vector3.zero, Vector3.one, mNavy);
            Band(r.pelvis, new Vector3(0f, -0.545f, 0f), 0.258f, 0.022f, 0.008f, mYellow);
            Shell(r.pelvis, "sh_robe2", new[] { new Vector2(0.24f, -0.42f), new Vector2(0.2f, -0.2f), new Vector2(0.145f, -0.02f), new Vector2(0.135f, 0.03f) },
                Vector3.zero, Vector3.one, mPurple);
            Band(r.pelvis, new Vector3(0f, -0.41f, 0f), 0.237f, 0.02f, 0.008f, mGold);
            var apron = Part(PrimitiveType.Cube, r.pelvis, new Vector3(0f, -0.24f, 0.2f), new Vector3(0.1f, 0.42f, 0.012f), mWhite, new Vector3(-13f, 0f, 0f));
            Part(PrimitiveType.Cube, apron.transform, new Vector3(0f, -0.2f, 0.6f), new Vector3(0.45f, 0.12f, 0.8f), mBolt, new Vector3(0f, 0f, 45f));
            for (int k = 0; k < 3; k++)
            {
                var tal = J("Talisman", r.pelvis, new Vector3(-0.13f + k * 0.13f, -0.02f, k == 1 ? -0.16f : 0.1f));
                tal.localRotation = Quaternion.Euler(0f, k == 1 ? 180f : (k - 1) * 30f, 0f);
                Part(PrimitiveType.Cube, tal, new Vector3(0f, -0.09f, 0f), new Vector3(0.05f, 0.17f, 0.006f), mPaper);
                Part(PrimitiveType.Cube, tal, new Vector3(0f, -0.08f, 0.004f), new Vector3(0.012f, 0.09f, 0.004f), mBolt);
                var sw = tal.gameObject.AddComponent<Sway>();
                sw.Amount = 8f;
                sw.Speed = 1.4f + k * 0.2f;
            }
            // Legs under the robe: white socks and tall geta sandals.
            for (int i = 0; i < 2; i++)
            {
                Taper(r.thigh[i], Vector3.zero, 0.26f, 0.07f, 0.055f, mNavyDk);
                Ball(r.shin[i], Vector3.zero, Vector3.one * 0.08f, mNavyDk);
                Taper(r.shin[i], Vector3.zero, 0.25f, 0.045f, 0.038f, mWhite);
                var f = r.foot[i];
                Ball(f, new Vector3(0f, -0.03f, 0.04f), new Vector3(0.085f, 0.07f, 0.17f), mWhite);
                Part(PrimitiveType.Cube, f, new Vector3(0f, -0.07f, 0.04f), new Vector3(0.1f, 0.022f, 0.22f), mWood);
                Part(PrimitiveType.Cube, f, new Vector3(0f, -0.1f, 0.1f), new Vector3(0.09f, 0.04f, 0.022f), mWood);
                Part(PrimitiveType.Cube, f, new Vector3(0f, -0.1f, -0.02f), new Vector3(0.09f, 0.04f, 0.022f), mWood);
                Part(PrimitiveType.Cube, f, new Vector3(0f, -0.025f, 0.08f), new Vector3(0.09f, 0.012f, 0.02f), PM(new Color(0.8f, 0.15f, 0.2f), 0.004f), new Vector3(0f, 0f, 0f));
            }

            // Arms: navy sleeves opening into wide bell sleeves with gold trim; small hands, the left one glowing.
            for (int i = 0; i < 2; i++)
            {
                int side = i == 0 ? 1 : -1;
                Taper(r.upper[i], Vector3.zero, 0.19f, 0.052f, 0.056f, mNavy);
                Shell(r.lower[i], "sh_sleeve", new[] { new Vector2(0.12f, -0.17f), new Vector2(0.126f, -0.15f), new Vector2(0.09f, -0.07f), new Vector2(0.06f, 0f),
                    new Vector2(0.05f, 0.025f) }, Vector3.zero, Vector3.one, mNavy);
                Band(r.lower[i], new Vector3(0f, -0.155f, 0f), 0.124f, 0.022f, 0.008f, mGold);
                Band(r.lower[i], new Vector3(0f, -0.13f, 0f), 0.114f, 0.012f, 0.006f, mYellow);
                Part(PrimitiveType.Cylinder, r.lower[i], new Vector3(0f, -0.172f, 0f), new Vector3(0.23f, 0.004f, 0.23f), mPurple);
                Taper(r.lower[i], Vector3.zero, 0.17f, 0.036f, 0.032f, mSkin);
                PremiumHand(r.hand[i], mSkin, mSkin, side, i == 0, 0.9f);
            }
            var glowGo = MeshFactory.MeshObject(MeshFactory.SmoothSphere(), r.hand[1], new Vector3(0f, -0.1f, 0.06f), Vector3.one * 0.12f, MaterialFactory.Additive(new Color(0.75f, 0.55f, 1f, 0.55f)), false);
            r.castGlow = glowGo.transform;
            Ball(r.hand[1], new Vector3(0f, -0.09f, 0.05f), Vector3.one * 0.035f, mSpark);

            // Head: calm half-lidded violet eyes with gold lower irises, long lashes, a gentle smile.
            Vector3 hc = new Vector3(0f, 0.32f, 0.02f), hr = new Vector3(0.36f, 0.36f, 0.34f);
            Ball(head, new Vector3(0f, 0.04f, 0f), new Vector3(0.085f, 0.15f, 0.085f), mSkin);
            Ball(head, hc, hr * 2f, mSkin);
            for (int s = -1; s <= 1; s += 2) Ball(head, hc + new Vector3(s * 0.35f, -0.03f, -0.01f), new Vector3(0.06f, 0.11f, 0.07f), mSkin);
            PremiumEyes(hc, hr, 0.125f, -0.04f, 0.115f, 0.13f, new Color(0.6f, 0.36f, 1f), new Color(1f, 0.85f, 0.4f), 0.22f, -4f, true, -8f, 0.016f, hairDk * 0.8f, mSkin);
            Blush(hc, hr, 0.35f);
            var mMouth = PM(new Color(0.55f, 0.2f, 0.25f), 0f);
            var mo = OnFace(hc, hr, 0f, -0.165f);
            Part(PrimitiveType.Capsule, mo, new Vector3(-0.016f, 0f, 0.002f), new Vector3(0.012f, 0.024f, 0.01f), mMouth, new Vector3(0f, 0f, 70f));
            Part(PrimitiveType.Capsule, mo, new Vector3(0.016f, 0f, 0.002f), new Vector3(0.012f, 0.024f, 0.01f), mMouth, new Vector3(0f, 0f, -70f));
            var mark = OnFace(hc, hr, -0.19f, -0.13f);
            Ball(mark, Vector3.zero, Vector3.one * 0.014f, PM(new Color(0.25f, 0.15f, 0.2f), 0f));
            var nose = OnFace(hc, hr, 0f, -0.095f);
            Ball(nose, Vector3.zero, new Vector3(0.016f, 0.012f, 0.01f), PM(Color.Lerp(def.skinTone, new Color(0.85f, 0.55f, 0.55f), 0.4f), 0f));

            // Hair: blunt bangs, straight side locks, back hair to the shoulder blades, a side braid and a crescent ornament.
            Ball(head, hc + new Vector3(0f, 0.05f, -0.04f), new Vector3(0.78f, 0.76f, 0.76f), mHair);
            Ball(head, hc + new Vector3(0f, 0.2f, 0.21f), new Vector3(0.62f, 0.2f, 0.22f), mHair);
            for (int k = 0; k < 5; k++)
                ConePart(head, hc + new Vector3(-0.2f + k * 0.1f, 0.13f, 0.29f), new Vector3(0.09f, 0.08f, 0.05f), mHair, new Vector3(165f, 0f, 0f));
            Ball(head, hc + new Vector3(0.07f, 0.33f, 0.12f), new Vector3(0.22f, 0.05f, 0.1f), PM(Color.Lerp(hairC, Color.white, 0.5f), 0f), new Vector3(15f, 30f, -18f));
            for (int s = -1; s <= 1; s += 2)
            {
                Ball(head, hc + new Vector3(s * 0.31f, -0.15f, 0.12f), new Vector3(0.12f, 0.48f, 0.14f), mHair);
                Part(PrimitiveType.Cube, head, hc + new Vector3(s * 0.31f, -0.39f, 0.12f), new Vector3(0.1f, 0.03f, 0.11f), mHairDk);
            }
            Ball(head, hc + new Vector3(0f, -0.26f, -0.2f), new Vector3(0.64f, 0.62f, 0.26f), mHair);
            Ball(head, hc + new Vector3(0f, -0.24f, -0.16f), new Vector3(0.58f, 0.58f, 0.2f), mHairDk);
            for (int k = 0; k < 5; k++)
            {
                float x = -0.2f + k * 0.1f;
                ConePart(head, hc + new Vector3(x, -0.5f, -0.21f), new Vector3(0.12f, 0.16f, 0.08f), k % 2 == 0 ? mHair : mHairDk, new Vector3(180f, 0f, x * 40f));
            }
            for (int k = 0; k < 4; k++)
                Ball(head, hc + new Vector3(-0.31f + (k % 2) * 0.015f, -0.22f - k * 0.09f, 0.16f), Vector3.one * (0.11f - k * 0.01f), mHair);
            Band(head, hc + new Vector3(-0.3f, -0.56f, 0.16f), 0.035f, 0.03f, 0.01f, mGold);
            ConePart(head, hc + new Vector3(-0.3f, -0.58f, 0.16f), new Vector3(0.08f, 0.12f, 0.08f), mHair, new Vector3(180f, 0f, 0f));
            var orn = J("Crescent", head, hc + new Vector3(0.3f, 0.25f, 0.08f));
            orn.localRotation = Quaternion.Euler(0f, 70f, -20f);
            for (int k = 0; k < 9; k++)
            {
                float a = Mathf.Lerp(-120f, 120f, k / 8f) * Mathf.Deg2Rad;
                float sz = Mathf.Lerp(0.045f, 0.018f, Mathf.Abs(k - 4f) / 4f);
                Ball(orn, new Vector3(0f, Mathf.Sin(a) * 0.08f, -Mathf.Cos(a) * 0.08f), Vector3.one * sz, mGold);
            }
            Ball(orn, Vector3.zero, Vector3.one * 0.03f, mSpark);
            var tassel = MeshFactory.MeshObject(MeshFactory.Cone(), orn, new Vector3(0f, -0.1f, -0.03f), new Vector3(0.04f, 0.12f, 0.04f), mPurple);
            tassel.transform.localRotation = Quaternion.Euler(180f, 0f, 0f);
            Add(tassel);
            // Legendary halo: a slowly turning gold ring with four gems behind the head.
            var halo = J("Halo", head, hc + new Vector3(0f, 0.1f, -0.42f));
            halo.localRotation = Quaternion.Euler(90f, 0f, 0f);
            var haloSpin = J("HaloSpin", halo, Vector3.zero);
            MeshFactory.MeshObject(MeshFactory.Ring(0.9f), haloSpin, Vector3.zero, Vector3.one * 0.34f, MaterialFactory.Additive(new Color(1f, 0.85f, 0.45f, 0.75f)), false);
            for (int k = 0; k < 4; k++)
            {
                float a = k * 90f * Mathf.Deg2Rad;
                Ball(haloSpin, new Vector3(Mathf.Cos(a) * 0.32f, 0f, Mathf.Sin(a) * 0.32f), Vector3.one * 0.04f, k % 2 == 0 ? mSpark : mBolt);
            }
            haloSpin.gameObject.AddComponent<Spinner>().DegreesPerSecond = new Vector3(0f, 25f, 0f);
            HeadY = r.hipY + r.hover + 0.02f + r.neck.y + hc.y;

            // Floating talismans and thunder sparks orbiting her.
            var orbit = J("Orbit", r.body, new Vector3(0f, 0.85f, 0f));
            var spin = orbit.gameObject.AddComponent<Spinner>();
            spin.DegreesPerSecond = new Vector3(0f, 45f, 0f);
            spin.BobHeight = 0.05f;
            for (int k = 0; k < 3; k++)
            {
                float a = k * 120f;
                var t = J("Charm", orbit, Quaternion.Euler(0f, a, 0f) * new Vector3(0f, 0.08f * (k - 1), 0.55f));
                t.localRotation = Quaternion.Euler(0f, a, 8f);
                Part(PrimitiveType.Cube, t, Vector3.zero, new Vector3(0.07f, 0.19f, 0.006f), mPaper);
                Part(PrimitiveType.Cube, t, new Vector3(0f, 0.01f, 0.004f), new Vector3(0.016f, 0.1f, 0.004f), mBolt);
                Part(PrimitiveType.Cube, t, new Vector3(0f, 0.075f, 0.004f), new Vector3(0.07f, 0.014f, 0.004f), mGold);
                Ball(orbit, Quaternion.Euler(0f, a + 60f, 0f) * new Vector3(0f, 0.15f, 0.5f), Vector3.one * 0.045f, mSpark);
            }

            // The staff: lacquered shaft with gold bands, a crescent cradle holding a thunder orb, a spiked butt cap.
            SwordPivot.localRotation = swordRest;
            var sp = SwordPivot;
            Part(PrimitiveType.Cylinder, sp, new Vector3(0f, 0f, 0.35f), new Vector3(0.042f, 0.9f, 0.042f), mNavyDk, new Vector3(90f, 0f, 0f));
            float[] bands = { -0.45f, -0.08f, 0.12f, 0.62f, 1.18f };
            foreach (float z in bands) Part(PrimitiveType.Cylinder, sp, new Vector3(0f, 0f, z), new Vector3(0.05f, 0.012f, 0.05f), mGold, new Vector3(90f, 0f, 0f));
            var butt = MeshFactory.MeshObject(MeshFactory.FacetCone(6), sp, new Vector3(0f, 0f, -0.55f), new Vector3(0.06f, 0.12f, 0.06f), mGold);
            butt.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            Add(butt);
            Vector3 oc = new Vector3(0f, 0f, 1.45f);
            for (int k = 0; k < 13; k++)
            {
                float a = Mathf.Lerp(-125f, 125f, k / 12f) * Mathf.Deg2Rad;
                float sz = Mathf.Lerp(0.07f, 0.024f, Mathf.Abs(k - 6f) / 6f);
                Ball(sp, oc + new Vector3(0f, Mathf.Sin(a) * 0.2f, -Mathf.Cos(a) * 0.2f), Vector3.one * sz, mGold);
            }
            Ball(sp, oc, Vector3.one * 0.16f, mSpark);
            MeshFactory.MeshObject(MeshFactory.SmoothSphere(), sp, oc, Vector3.one * 0.3f, MaterialFactory.Additive(new Color(0.7f, 0.5f, 1f, 0.35f)), false);
            var orbRing = J("OrbRing", sp, oc);
            MeshFactory.MeshObject(MeshFactory.Ring(0.86f), orbRing, Vector3.zero, Vector3.one * 0.14f, MaterialFactory.Additive(new Color(0.8f, 0.55f, 1f, 0.9f)), false);
            orbRing.gameObject.AddComponent<Spinner>().DegreesPerSecond = new Vector3(120f, 60f, 0f);
            for (int s = -1; s <= 1; s += 2)
            {
                var hang = J("Charm", sp, oc + new Vector3(0f, s * 0.17f, 0.1f));
                hang.localRotation = Quaternion.Euler(-90f, 0f, 0f);
                Part(PrimitiveType.Cube, hang, new Vector3(0f, -0.08f, 0f), new Vector3(0.04f, 0.14f, 0.005f), mPaper);
                Ball(hang, Vector3.zero, Vector3.one * 0.03f, mGold);
                hang.gameObject.AddComponent<Sway>().Amount = 10f;
            }
            PremiumTrail(1.45f, def.bladeColor);
        }
    }
}
