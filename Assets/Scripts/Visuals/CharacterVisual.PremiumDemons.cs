using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// Demons on the premium standard: the humanoid forms are built on the same jointed rig as the slayers
    /// (<see cref="PremiumRig"/>), so they walk with real strides, swing weapons with their arms and stagger with
    /// their whole body. Proportions are taller and meaner than the chibi heroes (smaller heads, long limbs,
    /// claws), with a two-tone palette, accent-coloured rim light, glowing eyes with slit pupils, and a detailed
    /// weapon for each type. PG: spooky and powerful, never gory.
    /// </summary>
    public partial class CharacterVisual
    {
        enum DemonFrame { Normal, Hunched, Lanky, Bulky, Tiny, Tall }

        Material dSkin, dSkinDk, dAccent, dGlow, dDark, dBone, dMetal, dCloth;
        Vector3 dHc, dHrad;

        /// <summary>The main head ball; eyes, masks and visors are placed on its real surface.</summary>
        void DemonHead(Vector3 scale, Material m)
        {
            Ball(head, dHc, scale, m);
            dHrad = scale * 0.5f;
        }

        bool BuildPremiumDemon(EnemyDefinition def)
        {
            DemonFrame frame;
            switch (def.form)
            {
                case "ghoul": frame = DemonFrame.Hunched; break;
                case "stalker": case "shadow": case "hunter": frame = DemonFrame.Lanky; break;
                case "oni": case "sentinel": case "brute": case "guardian": frame = DemonFrame.Bulky; break;
                case "imp": frame = DemonFrame.Tiny; break;
                case "lord": frame = DemonFrame.Tall; break;
                case "bone": case "knight": case "general": frame = DemonFrame.Normal; break;
                default: return false;
            }
            Color ac = def.accentColor;
            pRim = Color.Lerp(ac, Color.white, 0.2f);
            pShadow = new Color(0.42f, 0.36f, 0.52f);
            Color body = def.bodyColor;
            dSkin = PM(body, 0.02f);
            dSkinDk = PM(Color.Lerp(body, Color.black, 0.4f), 0.02f);
            dAccent = PM(ac, 0.015f);
            dGlow = PMe(ac, 0f, ac * 1.1f);
            dDark = PM(new Color(0.07f, 0.06f, 0.08f), 0.015f);
            dBone = PM(new Color(0.88f, 0.84f, 0.74f), 0.015f);
            dMetal = PM(Color.Lerp(new Color(0.22f, 0.22f, 0.26f), ac, 0.08f), 0.015f);
            dCloth = PM(Color.Lerp(body, new Color(0.15f, 0.1f, 0.18f), 0.6f), 0.015f);

            PremiumRig r;
            float chest, waist, depth;
            switch (frame)
            {
                case DemonFrame.Hunched:
                    r = NewRig(0.84f, 0.13f, 0.4f, 0.4f, 0.09f, new Vector3(0.3f, 0.46f, 0.02f), 0.38f, 0.38f, new Vector3(0f, 0.5f, 0.14f));
                    dHc = new Vector3(0f, 0.16f, 0.08f); chest = 0.24f; waist = 0.15f; depth = 0.8f;
                    r.lean = 26f; r.crouch = 0.1f; r.tempo = 10f; r.stride = 0.26f; r.lift = 0.14f; r.weightShift = 0.02f;
                    break;
                case DemonFrame.Lanky:
                    r = NewRig(1.04f, 0.1f, 0.5f, 0.48f, 0.08f, new Vector3(0.25f, 0.5f, 0f), 0.38f, 0.36f, new Vector3(0f, 0.56f, 0.04f));
                    dHc = new Vector3(0f, 0.2f, 0.03f); chest = 0.18f; waist = 0.11f; depth = 0.75f;
                    r.lean = 12f; r.crouch = 0.06f; r.tempo = 12f; r.stride = 0.3f; r.lift = 0.18f;
                    break;
                case DemonFrame.Bulky:
                    r = NewRig(0.82f, 0.2f, 0.4f, 0.38f, 0.1f, new Vector3(0.46f, 0.55f, 0f), 0.38f, 0.36f, new Vector3(0f, 0.66f, 0.06f));
                    dHc = new Vector3(0f, 0.2f, 0.06f); chest = 0.36f; waist = 0.27f; depth = 0.78f;
                    r.lean = 8f; r.crouch = 0.06f; r.tempo = 7.5f; r.stride = 0.24f; r.lift = 0.1f; r.weightShift = 0.03f;
                    r.swingDip = 0.12f; r.swingLunge = 18f; r.twistGain = 0.5f; r.toeOut = 18f;
                    break;
                case DemonFrame.Tiny:
                    r = NewRig(0.44f, 0.1f, 0.2f, 0.2f, 0.06f, new Vector3(0.24f, 0.26f, 0f), 0.2f, 0.2f, new Vector3(0f, 0.32f, 0.02f));
                    dHc = new Vector3(0f, 0.28f, 0.03f); chest = 0.2f; waist = 0.2f; depth = 0.9f;
                    r.lean = 10f; r.crouch = 0.04f; r.tempo = 15f; r.stride = 0.14f; r.lift = 0.12f; r.idleBounce = 0.03f; r.idleBounceFreq = 3f;
                    break;
                case DemonFrame.Tall:
                    r = NewRig(1.02f, 0.12f, 0.48f, 0.47f, 0.09f, new Vector3(0.3f, 0.56f, 0f), 0.36f, 0.34f, new Vector3(0f, 0.63f, 0.02f));
                    dHc = new Vector3(0f, 0.2f, 0.03f); chest = 0.22f; waist = 0.14f; depth = 0.75f;
                    r.lean = -3f; r.crouch = 0.01f; r.tempo = 8f; r.stride = 0.24f; r.lift = 0.1f; r.weightShift = 0.015f;
                    break;
                default:
                    r = NewRig(0.92f, 0.13f, 0.44f, 0.42f, 0.09f, new Vector3(0.32f, 0.52f, 0f), 0.34f, 0.32f, new Vector3(0f, 0.6f, 0.03f));
                    dHc = new Vector3(0f, 0.21f, 0.04f); chest = 0.25f; waist = 0.16f; depth = 0.75f;
                    r.lean = 6f; r.crouch = 0.04f; r.tempo = 9f; r.stride = 0.26f; r.lift = 0.12f; r.weightShift = 0.015f;
                    break;
            }
            r.restHandR = new Vector3(r.shoulder.x + 0.1f, 0.02f, 0.1f);
            r.restHandL = new Vector3(-r.shoulder.x - 0.1f, 0.02f, 0.08f);
            r.grip = PremiumRig.Grip.OneHand;
            r.twistGain = Mathf.Max(r.twistGain, 0.35f);
            r.swingLunge = Mathf.Max(r.swingLunge, 10f);
            r.hasPose = true;
            r.poseBody = new Vector3(r.lean * 0.5f + 6f, 0f, 0f); r.poseDrop = 0.04f; r.poseWeapon = new Vector3(-50f, 40f, 0f); r.poseHead = new Vector3(6f, 0f, 0f);
            Motion = MotionStyle.Aggressive;
            // No idle flourishes on demons: a random swing would read as an attack telegraph.
            FidgetsEnabled = false;

            // Torso: a V-shaped chest in the skin colour, a darker belly, glowing cracks.
            float tw = chest / 0.24f;
            string key = "dm_torso_" + frame;
            Shell(r.torso, key, new[] { new Vector2(0f, -0.06f), new Vector2(waist, -0.05f), new Vector2(waist * 1.05f, 0.1f), new Vector2(chest * 0.88f, 0.28f),
                new Vector2(chest, 0.42f), new Vector2(chest * 0.82f, 0.52f), new Vector2(chest * 0.4f, 0.6f), new Vector2(0f, 0.62f) }, Vector3.zero, new Vector3(1f, 1f, depth), dSkin);
            Ball(r.torso, new Vector3(0f, 0.14f, waist * depth * 0.45f), new Vector3(waist * 1.6f, 0.2f, waist * 0.9f), dSkinDk);
            if (def.form != "bone" && def.form != "knight" && def.form != "sentinel" && def.form != "guardian")
                for (int k = 0; k < 3; k++)
                {
                    float x = (k - 1) * chest * 0.45f;
                    Part(PrimitiveType.Cube, r.torso, new Vector3(x, 0.32f + (k % 2) * 0.05f, chest * depth * 0.92f), new Vector3(0.012f, 0.16f, 0.01f), dGlow, new Vector3(-20f, 0f, (k - 1) * 25f + 8f));
                }
            Ball(r.torso, new Vector3(0f, r.neck.y - 0.03f, r.neck.z * 0.5f), new Vector3(0.16f, 0.16f, 0.15f) * tw, dSkin);

            // Legs: thick thighs, shins with a darker wrap, clawed feet.
            for (int i = 0; i < 2; i++)
            {
                float th = frame == DemonFrame.Bulky ? 0.13f : frame == DemonFrame.Tiny ? 0.07f : 0.09f;
                Ball(r.thigh[i], Vector3.zero, Vector3.one * th * 2.1f, dSkin);
                Taper(r.thigh[i], Vector3.zero, r.legA, th, th * 0.78f, dSkin);
                Ball(r.shin[i], Vector3.zero, Vector3.one * th * 1.7f, dSkinDk);
                Taper(r.shin[i], Vector3.zero, r.legB, th * 0.8f, th * 0.55f, dSkin);
                DemonFoot(r.foot[i], th * 2.2f);
            }
            // Arms: muscled upper arm, forearm, clawed hands.
            for (int i = 0; i < 2; i++)
            {
                float ar = frame == DemonFrame.Bulky ? 0.11f : frame == DemonFrame.Tiny ? 0.055f : 0.075f;
                Ball(r.upper[i], new Vector3(0f, -0.02f, 0f), Vector3.one * ar * 2.4f, dSkin);
                Taper(r.upper[i], Vector3.zero, r.armA, ar * 1.05f, ar * 0.85f, dSkin);
                Ball(r.lower[i], Vector3.zero, Vector3.one * ar * 1.8f, dSkin);
                Taper(r.lower[i], Vector3.zero, r.armB, ar * 0.95f, ar * 0.7f, dSkinDk);
                DemonHand(r.hand[i], i == 0 ? 1 : -1, ar / 0.055f);
            }

            // Head.
            Ball(head, new Vector3(0f, 0.05f, 0f), new Vector3(0.14f, 0.16f, 0.14f) * tw, dSkin);

            switch (def.form)
            {
                case "ghoul": DemonGhoul(def, r); break;
                case "stalker": DemonStalker(def, r); break;
                case "shadow": DemonShadow(def, r); break;
                case "hunter": DemonHunter(def, r); break;
                case "bone": DemonBone(def, r); break;
                case "knight": DemonKnight(def, r); break;
                case "general": DemonGeneral(def, r); break;
                case "oni": DemonOni(def, r); break;
                case "sentinel": DemonSentinel(def, r); break;
                case "brute": DemonBrute(def, r); break;
                case "guardian": DemonGuardian(def, r); break;
                case "imp": DemonImp(def, r); break;
                case "lord": DemonLord(def, r); break;
            }
            HeadY = r.hipY + 0.02f + r.neck.y + dHc.y;
            OptimizeParts();
            r.Solve(0f);
            return true;
        }

        // ------------------------------------------------------------------ Shared demon parts

        void DemonHand(Transform h, int side, float size)
        {
            var g = J("Claw", h, Vector3.zero);
            g.localScale = Vector3.one * size;
            Ball(g, new Vector3(0f, -0.05f, 0.005f), new Vector3(0.09f, 0.1f, 0.08f), dSkinDk);
            for (int k = 0; k < 4; k++)
            {
                float x = -0.03f + k * 0.02f;
                Ball(g, new Vector3(x, -0.094f, 0.024f), new Vector3(0.024f, 0.032f, 0.034f), dSkin);
                ConePart(g, new Vector3(x, -0.1f, 0.045f), new Vector3(0.018f, 0.05f, 0.018f), dBone, new Vector3(150f, 0f, 0f));
            }
            Ball(g, new Vector3(-side * 0.032f, -0.07f, 0.036f), new Vector3(0.03f, 0.045f, 0.03f), dSkin);
        }

        void DemonFoot(Transform f, float size)
        {
            Ball(f, new Vector3(0f, -0.03f, 0.05f), new Vector3(0.7f, 0.5f, 1.2f) * size * 0.8f, dSkinDk);
            for (int k = 0; k < 3; k++)
                ConePart(f, new Vector3((k - 1) * size * 0.22f, -0.05f, 0.05f + size * 0.5f), new Vector3(0.03f, 0.08f, 0.03f) * (size / 0.2f), dBone, new Vector3(95f, (k - 1) * 12f, 0f));
        }

        /// <summary>Glowing slit-pupil eyes in dark sockets, under a heavy brow ridge.</summary>
        void DemonEyes(float spread, float y, float size, Color c, bool single = false)
        {
            var socket = PM(new Color(0.04f, 0.03f, 0.05f), 0f);
            var iris = PMe(c, 0f, c * 1.2f);
            var core = PMe(Color.Lerp(c, Color.white, 0.6f), 0f, Color.Lerp(c, Color.white, 0.5f));
            var slit = PM(new Color(0.05f, 0.02f, 0.03f), 0f);
            var halo = MaterialFactory.Additive(new Color(c.r, c.g, c.b, 0.4f));
            Vector3 rad = dHrad;
            int n = single ? 1 : 2;
            for (int k = 0; k < n; k++)
            {
                float s = single ? 0f : (k == 0 ? 1f : -1f);
                var e = OnFace(dHc, rad, s * spread, y);
                e.localRotation *= Quaternion.Euler(0f, 0f, s * 16f);
                Ball(e, new Vector3(0f, 0f, -0.006f), new Vector3(size * 1.5f, size * 0.95f, 0.03f), socket);
                Ball(e, new Vector3(0f, 0f, 0f), new Vector3(size * 1.15f, size * 0.62f, 0.03f), iris);
                Ball(e, new Vector3(0f, 0.002f, 0.004f), new Vector3(size * 0.6f, size * 0.3f, 0.026f), core);
                Ball(e, new Vector3(0f, 0f, 0.008f), new Vector3(size * 0.16f, size * 0.55f, 0.02f), slit);
                var hl = MeshFactory.MeshObject(MeshFactory.SmoothSphere(), e, Vector3.zero, new Vector3(size * 3f, size * 2f, 0.02f), halo, false);
                hl.AddComponent<Pulse>().Speed = 6f;
                if (!single)
                    Part(PrimitiveType.Capsule, e, new Vector3(-s * size * 0.1f, size * 0.62f, 0.004f), new Vector3(size * 0.35f, size * 0.95f, 0.03f), dSkinDk, new Vector3(0f, 0f, 90f - s * 18f));
            }
        }

        void Horns(float spread, float y, float len, float thick, float back, Material m, int pairs = 1)
        {
            for (int p = 0; p < pairs; p++)
                for (int s = -1; s <= 1; s += 2)
                {
                    var root = J("Horn", head, dHc + new Vector3(s * (spread + p * 0.06f), y - p * 0.05f, -0.02f - p * 0.04f));
                    root.localRotation = Quaternion.Euler(-back - p * 15f, 0f, -s * (18f + p * 20f));
                    // Three segments curving back, each thinner.
                    Transform cur = root;
                    for (int k = 0; k < 3; k++)
                    {
                        float l = len * (0.42f - k * 0.08f) * (1f - p * 0.3f);
                        float w = thick * (1f - k * 0.28f) * (1f - p * 0.25f);
                        if (k < 2) Taper(cur, new Vector3(0f, l, 0f), l, w * 0.8f, w, m);
                        else ConePart(cur, Vector3.zero, new Vector3(w * 2f, l * 1.4f, w * 2f), m, Vector3.zero);
                        var next = J("HornSeg", cur, new Vector3(0f, l, 0f));
                        next.localRotation = Quaternion.Euler(-18f, 0f, 0f);
                        cur = next;
                    }
                }
        }

        void Pauldrons(PremiumRig r, Material a, Material b, float size, bool spikes)
        {
            for (int i = 0; i < 2; i++)
            {
                int side = i == 0 ? 1 : -1;
                for (int k = 0; k < 3; k++)
                    Ball(r.upper[i], new Vector3(side * 0.04f * size, (0.05f - k * 0.07f) * size, 0f), new Vector3(0.36f - k * 0.05f, 0.14f, 0.34f - k * 0.04f) * size, k == 1 ? b : a, new Vector3(0f, 0f, -side * 20f));
                if (spikes)
                    for (int k = 0; k < 2; k++)
                        ConePart(r.upper[i], new Vector3(side * (0.1f + k * 0.04f) * size, 0.09f * size, (k - 0.5f) * 0.1f * size), new Vector3(0.05f, 0.18f, 0.05f) * size, dBone, new Vector3(0f, 0f, -side * (30f + k * 15f)));
            }
        }

        void Loincloth(PremiumRig r, Material m, float w, float len)
        {
            Band(r.pelvis, new Vector3(0f, 0.02f, 0f), w, 0.06f, 0.015f, dDark);
            for (int s = -1; s <= 1; s += 2)
            {
                var flap = J("Flap", r.pelvis, new Vector3(0f, 0f, s * w * 0.8f));
                flap.localRotation = Quaternion.Euler(-s * 8f, 0f, 0f);
                Part(PrimitiveType.Cube, flap, new Vector3(0f, -len * 0.5f, 0f), new Vector3(w * 1.1f, len, 0.02f), m);
                var jag = MeshFactory.MeshObject(MeshFactory.FacetCone(3), flap, new Vector3(0f, -len, 0f), new Vector3(w * 1.1f, 0.08f, 0.02f), m);
                jag.transform.localRotation = Quaternion.Euler(180f, 0f, 0f);
                Add(jag);
                flap.gameObject.AddComponent<Sway>().Amount = 6f;
            }
        }

        void Tatters(Transform parent, Vector3 pos, float width, float len, Material m, int strips, float tilt)
        {
            var root = J("Tatters", parent, pos);
            root.localRotation = Quaternion.Euler(tilt, 0f, 0f);
            float sw = width / strips;
            for (int k = 0; k < strips; k++)
            {
                var st = J("Strip", root, new Vector3(-width * 0.5f + sw * (k + 0.5f), 0f, 0f));
                st.localRotation = Quaternion.Euler(2f + k % 3, 0f, (k - strips * 0.5f) * 2f);
                float l = len * (0.8f + 0.2f * Mathf.Sin(k * 2.3f));
                Part(PrimitiveType.Cube, st, new Vector3(0f, -l * 0.5f, 0f), new Vector3(sw * 1.05f, l, 0.02f), m);
                var jag = MeshFactory.MeshObject(MeshFactory.FacetCone(3), st, new Vector3(0f, -l, 0f), new Vector3(sw * 1.05f, 0.1f, 0.02f), m);
                jag.transform.localRotation = Quaternion.Euler(180f, 0f, 0f);
                Add(jag);
                var s = st.gameObject.AddComponent<Sway>();
                s.Amount = 4f + k % 3;
                s.Speed = 0.8f + k * 0.07f;
            }
        }

        /// <summary>A broad blade along +Z from the hand (cleavers, greatswords, machetes).</summary>
        void BroadBlade(Transform sp, float len, float width, Material blade, Material edge, Material grip, Material guard, float gripLen = 0.28f)
        {
            Part(PrimitiveType.Cube, sp, new Vector3(0f, 0f, 0f), new Vector3(0.05f, 0.05f, gripLen), grip);
            Ball(sp, new Vector3(0f, 0f, -gripLen * 0.55f), Vector3.one * 0.07f, guard);
            Part(PrimitiveType.Cube, sp, new Vector3(0f, 0f, gripLen * 0.5f + 0.02f), new Vector3(0.07f, width * 1.3f, 0.05f), guard);
            Part(PrimitiveType.Cube, sp, new Vector3(0f, width * 0.1f, gripLen * 0.5f + 0.04f + len * 0.5f), new Vector3(0.045f, width, len), blade);
            Part(PrimitiveType.Cube, sp, new Vector3(0f, -width * 0.42f, gripLen * 0.5f + 0.04f + len * 0.5f), new Vector3(0.03f, width * 0.12f, len), edge);
            var tip = MeshFactory.MeshObject(MeshFactory.FacetCone(4), sp, new Vector3(0f, width * 0.1f, gripLen * 0.5f + 0.04f + len), new Vector3(0.045f, width * 0.6f, width), blade);
            tip.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            Add(tip);
            for (int k = 0; k < 3; k++)
                Ball(sp, new Vector3(0f, width * 0.35f, gripLen * 0.5f + 0.15f + k * len * 0.28f), new Vector3(0.05f, 0.03f, 0.03f), guard);
        }

        void DemonTrail(float z, Color c)
        {
            PremiumTrail(z, c);
        }

        // ------------------------------------------------------------------ Forms

        void DemonGhoul(EnemyDefinition d, PremiumRig r)
        {
            DemonHead(new Vector3(0.44f, 0.4f, 0.46f), dSkin);
            Ball(head, dHc + new Vector3(0f, -0.1f, 0.1f), new Vector3(0.3f, 0.18f, 0.3f), dSkinDk);
            for (int s = -1; s <= 1; s += 2) ConePart(head, dHc + new Vector3(s * 0.2f, 0.03f, -0.02f), new Vector3(0.06f, 0.2f, 0.04f), dSkin, new Vector3(0f, 0f, -s * 75f));
            DemonEyes(0.08f, 0.02f, 0.07f, new Color(1f, 0.85f, 0.2f));
            Horns(0.1f, 0.15f, 0.25f, 0.035f, 30f, dBone);
            for (int i = 0; i < 4; i++) ConePart(r.torso, new Vector3(0f, 0.55f - i * 0.12f, -0.16f - i * 0.01f), new Vector3(0.08f, 0.18f, 0.08f), dAccent, new Vector3(-70f, 0f, 0f));
            Loincloth(r, dCloth, 0.16f, 0.32f);
            Tatters(r.torso, new Vector3(0.05f, 0.5f, 0.1f), 0.2f, 0.3f, dCloth, 3, -8f);
            // A crude rusty machete.
            SwordPivot.localRotation = swordRest;
            var rust = PM(new Color(0.45f, 0.3f, 0.22f), 0.01f);
            BroadBlade(SwordPivot, 0.6f, 0.13f, rust, PM(new Color(0.7f, 0.66f, 0.6f), 0.008f), dDark, dBone, 0.22f);
            DemonTrail(0.7f, new Color(0.9f, 0.8f, 0.5f));
        }

        void DemonStalker(EnemyDefinition d, PremiumRig r)
        {
            // A long blade-crested head, armour strips on the shins, bone blades along the forearms.
            DemonHead(new Vector3(0.34f, 0.36f, 0.42f), dSkin);
            for (int k = 0; k < 4; k++) ConePart(head, dHc + new Vector3(0f, 0.14f - k * 0.02f, -0.02f - k * 0.08f), new Vector3(0.03f, 0.3f - k * 0.04f, 0.14f), dAccent, new Vector3(-60f - k * 8f, 0f, 0f));
            DemonEyes(0.07f, 0.0f, 0.06f, d.accentColor);
            for (int i = 0; i < 2; i++)
            {
                Taper(r.shin[i], new Vector3(0f, -0.04f, 0.02f), r.legB * 0.8f, 0.07f, 0.05f, dAccent, new Vector3(1f, 1f, 1.1f));
                var bl = J("ArmBlade", r.lower[i], new Vector3((i == 0 ? 1 : -1) * 0.05f, -0.02f, 0f));
                bl.localRotation = Quaternion.Euler(-90f, 0f, 0f);
                ConePart(bl, new Vector3(0f, 0f, -0.05f), new Vector3(0.03f, 0.5f, 0.1f), dBone, new Vector3(-100f, 0f, 0f));
            }
            Tatters(r.torso, new Vector3(0f, 0.55f, -0.14f), 0.3f, 0.6f, dCloth, 4, 6f);
            SwordPivot.localRotation = swordRest;
            ConePart(SwordPivot, new Vector3(0f, 0f, 0.05f), new Vector3(0.04f, 0.6f, 0.12f), dBone, new Vector3(90f, 0f, 0f));
            Ball(SwordPivot, Vector3.zero, Vector3.one * 0.06f, dGlow);
            DemonTrail(0.6f, new Color(1f, 0.9f, 0.5f));
        }

        void DemonShadow(EnemyDefinition d, PremiumRig r)
        {
            // Smoke-dark body, a cracked porcelain mask, long needle claws, a shredded shroud.
            var smoke = PM(new Color(0.06f, 0.05f, 0.09f), 0.02f);
            foreach (var rend in r.body.GetComponentsInChildren<Renderer>()) rend.sharedMaterial = smoke;
            dSkin = smoke; dSkinDk = smoke;
            DemonHead(new Vector3(0.38f, 0.42f, 0.4f), smoke);
            var mask = PM(new Color(0.94f, 0.92f, 0.9f), 0.015f);
            var mk = OnFace(dHc, dHrad, 0f, 0f, -0.02f);
            Ball(mk, new Vector3(0f, 0f, 0.01f), new Vector3(0.3f, 0.36f, 0.08f), mask);
            Part(PrimitiveType.Cube, mk, new Vector3(0.05f, 0.06f, 0.05f), new Vector3(0.012f, 0.14f, 0.01f), dDark, new Vector3(0f, 0f, 20f));
            Part(PrimitiveType.Cube, mk, new Vector3(-0.06f, -0.09f, 0.05f), new Vector3(0.1f, 0.012f, 0.01f), PM(new Color(0.75f, 0.2f, 0.35f), 0f), new Vector3(0f, 0f, -8f));
            DemonEyes(0.07f, 0.02f, 0.06f, new Color(0.75f, 0.35f, 1f));
            for (int i = 0; i < 2; i++)
                for (int k = 0; k < 3; k++)
                    ConePart(r.hand[i], new Vector3(-0.025f + k * 0.025f, -0.12f, 0.03f), new Vector3(0.025f, 0.4f, 0.025f), dBone, new Vector3(170f, 0f, 0f));
            Tatters(r.torso, new Vector3(0f, 0.58f, -0.12f), 0.44f, 1f, smoke, 6, 5f);
            Tatters(r.pelvis, new Vector3(0f, 0f, 0.14f), 0.3f, 0.5f, smoke, 4, -6f);
            var wisps = EnvFx.Smoke(r.body, new Vector3(0f, 1f, 0f), 0.3f);
            if (wisps != null) { var m = wisps.main; m.startColor = new Color(0.25f, 0.1f, 0.4f, 0.4f); }
            SwordPivot.localRotation = swordRest;
            DemonTrail(0.4f, new Color(0.75f, 0.4f, 1f));
        }

        void DemonHunter(EnemyDefinition d, PremiumRig r)
        {
            // Hooded assassin with a mask and a crescent glaive.
            DemonHead(new Vector3(0.36f, 0.4f, 0.4f), dSkin);
            DemonEyes(0.07f, 0.0f, 0.06f, d.accentColor);
            var cloak = PM(new Color(0.14f, 0.06f, 0.18f), 0.02f);
            Ball(head, dHc + new Vector3(0f, 0.04f, -0.04f), new Vector3(0.46f, 0.46f, 0.46f), cloak);
            ConePart(head, dHc + new Vector3(0f, 0.15f, -0.1f), new Vector3(0.3f, 0.35f, 0.3f), cloak, new Vector3(-40f, 0f, 0f));
            var mk = OnFace(dHc, dHrad, 0f, -0.07f);
            Ball(mk, new Vector3(0f, 0f, 0.01f), new Vector3(0.28f, 0.14f, 0.06f), dMetal);
            Shell(r.torso, "dm_hcloak", new[] { new Vector2(0.28f, -0.5f), new Vector2(0.27f, -0.45f), new Vector2(0.24f, 0f), new Vector2(0.22f, 0.4f), new Vector2(0.14f, 0.58f), new Vector2(0f, 0.6f) },
                new Vector3(0f, 0f, -0.04f), new Vector3(1f, 1f, 0.8f), cloak);
            Tatters(r.torso, new Vector3(0f, -0.45f, -0.14f), 0.5f, 0.35f, cloak, 5, 4f);
            Band(r.torso, new Vector3(0f, 0.02f, 0f), 0.17f, 0.05f, 0.015f, dAccent, new Vector3(1f, 1f, 0.8f));
            r.grip = PremiumRig.Grip.TwoHand;
            r.handleOffset = -0.3f;
            SwordPivot.localRotation = swordRest;
            Part(PrimitiveType.Cylinder, SwordPivot, new Vector3(0f, 0f, 0.1f), new Vector3(0.04f, 0.6f, 0.04f), dDark, new Vector3(90f, 0f, 0f));
            for (int k = 0; k < 11; k++)
            {
                float a = Mathf.Lerp(-10f, 150f, k / 10f) * Mathf.Deg2Rad;
                var seg = J("Crescent", SwordPivot, new Vector3(0f, Mathf.Sin(a) * 0.32f, 0.7f + Mathf.Cos(a) * 0.32f - 0.1f));
                seg.localRotation = Quaternion.Euler(-a * Mathf.Rad2Deg, 0f, 0f);
                float w = Mathf.Lerp(0.09f, 0.025f, k / 10f);
                Part(PrimitiveType.Cube, seg, Vector3.zero, new Vector3(0.02f, 0.075f, w), PM(new Color(0.8f, 0.82f, 0.86f), 0.008f));
                Part(PrimitiveType.Cube, seg, new Vector3(0f, 0f, w * 0.5f), new Vector3(0.014f, 0.075f, 0.014f), dGlow);
            }
            DemonTrail(0.9f, d.accentColor);
        }

        void DemonBone(EnemyDefinition d, PremiumRig r)
        {
            // Skeleton samurai: rib cage, skull under a rusted kabuto, rusted plates, green spirit fire, a huge cleaver.
            foreach (var rend in r.body.GetComponentsInChildren<Renderer>()) rend.sharedMaterial = dBone;
            dSkin = dBone; dSkinDk = PM(new Color(0.7f, 0.66f, 0.56f), 0.015f);
            var rust = PM(new Color(0.38f, 0.22f, 0.15f), 0.018f);
            var spirit = new Color(0.4f, 1f, 0.5f);
            var dark = PM(new Color(0.08f, 0.1f, 0.08f), 0.01f);
            for (int k = 0; k < 4; k++) Band(r.torso, new Vector3(0f, 0.25f + k * 0.08f, 0f), 0.16f + k * 0.012f, 0.025f, 0.012f, dBone, new Vector3(1.1f, 1f, 0.8f));
            Ball(r.torso, new Vector3(0f, 0.3f, 0f), new Vector3(0.24f, 0.4f, 0.2f), dark);
            DemonHead(new Vector3(0.4f, 0.42f, 0.42f), dBone);
            Ball(head, dHc + new Vector3(0f, -0.13f, 0.08f), new Vector3(0.26f, 0.14f, 0.24f), dBone);
            DemonEyes(0.075f, 0.01f, 0.07f, spirit);
            // Kabuto with swept horns.
            Ball(head, dHc + new Vector3(0f, 0.12f, -0.03f), new Vector3(0.46f, 0.3f, 0.46f), rust);
            Part(PrimitiveType.Cylinder, head, dHc + new Vector3(0f, 0.09f, 0f), new Vector3(0.58f, 0.015f, 0.58f), rust);
            Horns(0.08f, 0.16f, 0.35f, 0.03f, 10f, PM(new Color(0.85f, 0.65f, 0.3f), 0.01f));
            Pauldrons(r, rust, dark, 1f, false);
            for (int k = -1; k <= 1; k++)
            {
                var tas = Part(PrimitiveType.Cube, r.pelvis, Quaternion.Euler(0f, k * 40f, 0f) * new Vector3(0f, -0.14f, 0.16f), new Vector3(0.16f, 0.26f, 0.025f), rust);
                tas.transform.localRotation = Quaternion.Euler(-12f, k * 40f, 0f);
            }
            var fire = EnvFx.Fire(r.torso, new Vector3(0f, 0.32f, 0f), 0.25f, false);
            if (fire != null) { var m = fire.main; m.startColor = new Color(0.4f, 1f, 0.5f, 0.6f); }
            r.grip = PremiumRig.Grip.TwoHand;
            r.handleOffset = -0.15f;
            swordRest = Quaternion.Euler(-108f, 32f, 0f);
            SwordPivot.localRotation = swordRest;
            BroadBlade(SwordPivot, 1.2f, 0.34f, PM(new Color(0.42f, 0.38f, 0.35f), 0.01f), PM(new Color(0.75f, 0.9f, 0.75f), 0f), dark, rust, 0.4f);
            DemonTrail(1.4f, spirit);
        }

        void DemonKnight(EnemyDefinition d, PremiumRig r)
        {
            // Black spiked plate armour, a horned helm with a burning visor, a demon-face shield and a barbed halberd.
            var plate = PM(new Color(0.09f, 0.09f, 0.11f), 0.018f);
            var trim = PM(new Color(0.55f, 0.2f, 0.7f), 0.01f);
            foreach (var rend in r.body.GetComponentsInChildren<Renderer>()) rend.sharedMaterial = plate;
            Shell(r.torso, "dm_kplate", new[] { new Vector2(0.2f, 0.05f), new Vector2(0.24f, 0.2f), new Vector2(0.27f, 0.38f), new Vector2(0.24f, 0.5f), new Vector2(0.14f, 0.58f), new Vector2(0f, 0.6f) },
                Vector3.zero, new Vector3(1.05f, 1f, 0.8f), plate);
            for (int k = 0; k < 3; k++) Band(r.torso, new Vector3(0f, 0.15f + k * 0.12f, 0f), 0.225f + k * 0.018f, 0.014f, 0.006f, trim, new Vector3(1.05f, 1f, 0.8f));
            Part(PrimitiveType.Cube, r.torso, new Vector3(0f, 0.36f, 0.22f), new Vector3(0.012f, 0.26f, 0.01f), dGlow);
            var tab = Part(PrimitiveType.Cube, r.pelvis, new Vector3(0f, -0.2f, 0.16f), new Vector3(0.22f, 0.44f, 0.02f), PM(new Color(0.3f, 0.1f, 0.4f), 0.015f), new Vector3(-8f, 0f, 0f));
            tab.AddComponent<Sway>().Amount = 3f;
            Pauldrons(r, plate, trim, 1.2f, true);
            for (int i = 0; i < 2; i++)
            {
                Taper(r.shin[i], new Vector3(0f, -0.02f, 0.015f), r.legB * 0.85f, 0.1f, 0.075f, plate);
                Ball(r.shin[i], Vector3.zero, Vector3.one * 0.2f, trim);
                Taper(r.lower[i], new Vector3(0f, -0.04f, 0f), r.armB * 0.8f, 0.085f, 0.075f, plate);
            }
            // Helm.
            DemonHead(new Vector3(0.46f, 0.48f, 0.48f), plate);
            var visor = OnFace(dHc, dHrad, 0f, 0f);
            Part(PrimitiveType.Cube, visor, new Vector3(0f, 0f, 0.01f), new Vector3(0.28f, 0.035f, 0.02f), dGlow);
            Part(PrimitiveType.Cube, visor, new Vector3(0f, -0.08f, 0.012f), new Vector3(0.02f, 0.12f, 0.02f), trim);
            Horns(0.12f, 0.12f, 0.55f, 0.04f, 45f, plate);
            // Kite shield on the left forearm.
            var sh = J("Shield", r.lower[1], new Vector3(-0.1f, -0.16f, 0.04f));
            sh.localRotation = Quaternion.Euler(0f, -90f, 0f);
            Shell(sh, "dm_kite", new[] { new Vector2(0f, -0.5f), new Vector2(0.18f, -0.3f), new Vector2(0.28f, 0f), new Vector2(0.27f, 0.25f), new Vector2(0.2f, 0.35f), new Vector2(0f, 0.37f) },
                Vector3.zero, new Vector3(1f, 1f, 0.12f), plate, 16);
            Ball(sh, new Vector3(0f, 0.02f, 0.04f), new Vector3(0.22f, 0.22f, 0.06f), trim);
            Ball(sh, new Vector3(0f, 0.04f, 0.06f), new Vector3(0.1f, 0.05f, 0.03f), dGlow);
            // Halberd.
            r.grip = PremiumRig.Grip.OneHand;
            SwordPivot.localRotation = swordRest;
            Part(PrimitiveType.Cylinder, SwordPivot, new Vector3(0f, 0f, 0.55f), new Vector3(0.045f, 1.05f, 0.045f), dDark, new Vector3(90f, 0f, 0f));
            Part(PrimitiveType.Cube, SwordPivot, new Vector3(0.14f, 0f, 1.45f), new Vector3(0.28f, 0.03f, 0.3f), PM(new Color(0.55f, 0.55f, 0.6f), 0.01f));
            Part(PrimitiveType.Cube, SwordPivot, new Vector3(0.28f, 0f, 1.45f), new Vector3(0.02f, 0.035f, 0.32f), dGlow);
            var spike = MeshFactory.MeshObject(MeshFactory.FacetCone(4), SwordPivot, new Vector3(0f, 0f, 1.6f), new Vector3(0.05f, 0.3f, 0.05f), PM(new Color(0.55f, 0.55f, 0.6f), 0.01f));
            spike.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            Add(spike);
            DemonTrail(1.6f, new Color(0.7f, 0.3f, 1f));
        }

        void DemonGeneral(EnemyDefinition d, PremiumRig r)
        {
            // Ash-grey warlord: horns, fur collar, war cloak, back banners, lacquered armour, twin blades.
            var ash = PM(new Color(0.58f, 0.58f, 0.62f), 0.02f);
            foreach (var rend in r.body.GetComponentsInChildren<Renderer>()) rend.sharedMaterial = ash;
            dSkin = ash;
            var armor = PM(Color.Lerp(d.bodyColor, new Color(0.12f, 0.1f, 0.1f), 0.5f), 0.018f);
            var lacquer = PM(d.accentColor * 0.8f, 0.015f);
            var fur = PM(new Color(0.1f, 0.09f, 0.09f), 0.02f);
            Shell(r.torso, "dm_gplate", new[] { new Vector2(0.2f, 0.05f), new Vector2(0.23f, 0.2f), new Vector2(0.26f, 0.38f), new Vector2(0.23f, 0.5f), new Vector2(0.14f, 0.57f), new Vector2(0f, 0.59f) },
                Vector3.zero, new Vector3(1.05f, 1f, 0.8f), armor);
            Part(PrimitiveType.Cube, r.torso, new Vector3(0f, 0.34f, 0.215f), new Vector3(0.3f, 0.2f, 0.02f), lacquer, new Vector3(-10f, 0f, 0f));
            for (int k = 0; k < 8; k++)
            {
                float a = k * 45f;
                Ball(r.torso, Quaternion.Euler(0f, a, 0f) * new Vector3(0f, 0.56f, 0.2f), new Vector3(0.16f, 0.12f, 0.12f), fur);
            }
            Pauldrons(r, armor, lacquer, 1.05f, false);
            Shell(r.pelvis, "dm_gskirt", new[] { new Vector2(0.3f, -0.45f), new Vector2(0.3f, -0.42f), new Vector2(0.24f, -0.2f), new Vector2(0.18f, 0f), new Vector2(0.16f, 0.04f) },
                Vector3.zero, Vector3.one, armor);
            Band(r.pelvis, new Vector3(0f, -0.43f, 0f), 0.298f, 0.03f, 0.01f, lacquer);
            Tatters(r.torso, new Vector3(0f, 0.55f, -0.2f), 0.55f, 1.2f, fur, 6, 6f);
            for (int s = -1; s <= 1; s += 2)
            {
                var pole = J("Banner", r.torso, new Vector3(s * 0.12f, 0.5f, -0.22f));
                pole.localRotation = Quaternion.Euler(-8f, 0f, s * 8f);
                Part(PrimitiveType.Cylinder, pole, new Vector3(0f, 0.45f, 0f), new Vector3(0.025f, 0.45f, 0.025f), dDark);
                var flag = J("Flag", pole, new Vector3(s * 0.13f, 0.72f, 0f));
                Part(PrimitiveType.Cube, flag, new Vector3(0f, -0.1f, 0f), new Vector3(0.22f, 0.4f, 0.015f), lacquer);
                Ball(flag, new Vector3(0f, -0.05f, 0.01f), new Vector3(0.08f, 0.08f, 0.01f), dGlow);
                flag.gameObject.AddComponent<Sway>().Amount = 8f;
            }
            DemonHead(new Vector3(0.4f, 0.44f, 0.42f), ash);
            Ball(head, dHc + new Vector3(0f, 0.1f, -0.04f), new Vector3(0.42f, 0.3f, 0.42f), fur);
            Ball(head, dHc + new Vector3(0f, 0f, -0.2f), new Vector3(0.14f, 0.2f, 0.14f), fur);
            DemonEyes(0.07f, 0.0f, 0.06f, d.accentColor);
            Horns(0.1f, 0.14f, 0.35f, 0.03f, 25f, dBone);
            r.grip = PremiumRig.Grip.Twin;
            SwordPivot.localRotation = swordRest;
            var steel = PM(Color.Lerp(new Color(0.8f, 0.8f, 0.85f), d.accentColor, 0.15f), 0.01f);
            LongBlade(SwordPivot, Vector3.zero, Quaternion.identity, 1f, 1.1f, steel, dGlow, lacquer, dDark, lacquer, false);
            LongBlade(r.hand[1], new Vector3(-0.05f, -0.055f, 0f), Quaternion.Euler(-90f, 0f, 0f), 0.6f, 1f, steel, dGlow, lacquer, dDark, lacquer, false);
            DemonTrail(1.1f, d.accentColor);
        }

        void DemonOni(EnemyDefinition d, PremiumRig r)
        {
            // Frost oni: ice horns and crystal shoulders, a tiger-stripe loincloth, a spiked ice club.
            var ice = PMe(new Color(0.8f, 0.92f, 1f), 0.01f, new Color(0.3f, 0.5f, 0.7f));
            DemonHead(new Vector3(0.5f, 0.46f, 0.48f), dSkin);
            Ball(head, dHc + new Vector3(0f, -0.14f, 0.1f), new Vector3(0.36f, 0.2f, 0.3f), dSkin);
            for (int s = -1; s <= 1; s += 2) ConePart(head, dHc + new Vector3(s * 0.08f, -0.16f, 0.22f), new Vector3(0.035f, 0.08f, 0.03f), dBone, Vector3.zero);
            Ball(head, dHc + new Vector3(0f, 0.16f, -0.04f), new Vector3(0.52f, 0.2f, 0.5f), PM(new Color(0.85f, 0.9f, 0.95f), 0.015f));
            DemonEyes(0.09f, 0.02f, 0.075f, new Color(0.6f, 0.95f, 1f));
            Horns(0.1f, 0.18f, 0.5f, 0.05f, 5f, ice);
            for (int i = 0; i < 2; i++)
            {
                int side = i == 0 ? 1 : -1;
                for (int k = 0; k < 4; k++)
                    ConePart(r.upper[i], new Vector3(side * 0.06f, 0.06f, (k - 1.5f) * 0.06f), new Vector3(0.08f, 0.25f - k % 2 * 0.07f, 0.08f), ice, new Vector3((k - 1.5f) * 15f, 0f, -side * (20f + k * 8f)));
                for (int k = 0; k < 3; k++) Band(r.lower[i], new Vector3(0f, -0.06f - k * 0.08f, 0f), 0.09f - k * 0.008f, 0.03f, 0.01f, dDark);
            }
            var stripe = PM(new Color(0.95f, 0.8f, 0.3f), 0.015f);
            Shell(r.pelvis, "dm_oloin", new[] { new Vector2(0.32f, -0.25f), new Vector2(0.3f, -0.1f), new Vector2(0.28f, 0f), new Vector2(0.27f, 0.05f) }, Vector3.zero, new Vector3(1f, 1f, 0.8f), stripe);
            for (int k = 0; k < 8; k++)
            {
                var st = Part(PrimitiveType.Cube, r.pelvis, Quaternion.Euler(0f, k * 45f, 0f) * new Vector3(0f, -0.12f, 0.3f * 0.8f), new Vector3(0.03f, 0.22f, 0.012f), dDark);
                st.transform.localRotation = Quaternion.Euler(-12f, k * 45f, 12f);
            }
            Band(r.torso, new Vector3(0f, 0.04f, 0f), 0.28f, 0.08f, 0.02f, PM(new Color(0.8f, 0.2f, 0.2f), 0.012f), new Vector3(1f, 1f, 0.8f));
            // Ice club.
            r.grip = PremiumRig.Grip.TwoHand;
            swordRest = Quaternion.Euler(-108f, 32f, 0f);
            SwordPivot.localRotation = swordRest;
            Part(PrimitiveType.Cylinder, SwordPivot, new Vector3(0f, 0f, 0.05f), new Vector3(0.08f, 0.25f, 0.08f), dDark, new Vector3(90f, 0f, 0f));
            var club = MeshFactory.MeshObject(MeshFactory.Lathe("dm_iceclub", new[] { new Vector2(0f, 0f), new Vector2(0.09f, 0.01f), new Vector2(0.12f, 0.4f),
                new Vector2(0.18f, 0.9f), new Vector2(0.16f, 1.02f), new Vector2(0f, 1.06f) }, 8), SwordPivot, new Vector3(0f, 0f, 0.3f), Vector3.one, ice);
            club.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            Add(club);
            for (int k = 0; k < 10; k++)
            {
                float a = k * 72f * Mathf.Deg2Rad + (k / 5) * 0.6f;
                Vector3 dir = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f);
                var sp = MeshFactory.MeshObject(MeshFactory.FacetCone(4), SwordPivot, dir * 0.15f + Vector3.forward * (0.8f + (k / 5) * 0.3f), new Vector3(0.07f, 0.16f, 0.07f), ice);
                sp.transform.localRotation = Quaternion.FromToRotation(Vector3.up, dir);
                Add(sp);
            }
            DemonTrail(1.3f, new Color(0.75f, 0.9f, 1f));
        }

        void DemonSentinel(EnemyDefinition d, PremiumRig r)
        {
            // A towering suit of eclipse armour around a glowing core; no face, just a visor slit.
            var plate = PM(new Color(0.13f, 0.11f, 0.15f), 0.018f);
            var trim = PM(Color.Lerp(d.accentColor, new Color(0.6f, 0.5f, 0.3f), 0.5f), 0.01f);
            foreach (var rend in r.body.GetComponentsInChildren<Renderer>()) rend.sharedMaterial = plate;
            Shell(r.torso, "dm_splate", new[] { new Vector2(0.3f, 0.05f), new Vector2(0.36f, 0.2f), new Vector2(0.4f, 0.4f), new Vector2(0.36f, 0.52f), new Vector2(0.2f, 0.6f), new Vector2(0f, 0.63f) },
                Vector3.zero, new Vector3(1f, 1f, 0.72f), plate);
            Ball(r.torso, new Vector3(0f, 0.35f, 0.27f), Vector3.one * 0.16f, dGlow);
            MeshFactory.MeshObject(MeshFactory.SmoothSphere(), r.torso, new Vector3(0f, 0.35f, 0.27f), Vector3.one * 0.32f, MaterialFactory.Additive(new Color(d.accentColor.r, d.accentColor.g, d.accentColor.b, 0.35f)), false);
            for (int k = 0; k < 6; k++)
            {
                float a = k * 60f * Mathf.Deg2Rad;
                Part(PrimitiveType.Cube, r.torso, new Vector3(Mathf.Cos(a) * 0.13f, 0.35f + Mathf.Sin(a) * 0.13f, 0.27f), new Vector3(0.03f, 0.08f, 0.02f), trim, new Vector3(0f, 0f, k * 60f + 90f));
            }
            Pauldrons(r, plate, trim, 1.5f, true);
            for (int i = 0; i < 2; i++)
            {
                Taper(r.shin[i], new Vector3(0f, -0.02f, 0.02f), r.legB * 0.9f, 0.15f, 0.12f, plate);
                Taper(r.thigh[i], Vector3.zero, r.legA, 0.15f, 0.13f, plate);
                Taper(r.lower[i], Vector3.zero, r.armB, 0.13f, 0.12f, plate);
                Band(r.lower[i], new Vector3(0f, -r.armB * 0.8f, 0f), 0.125f, 0.03f, 0.01f, trim);
            }
            DemonHead(new Vector3(0.5f, 0.52f, 0.52f), plate);
            ConePart(head, dHc + new Vector3(0f, 0.22f, -0.02f), new Vector3(0.14f, 0.3f, 0.3f), trim, new Vector3(-15f, 0f, 0f));
            var visor = OnFace(dHc, dHrad, 0f, 0f);
            Part(PrimitiveType.Cube, visor, new Vector3(0f, 0f, 0.012f), new Vector3(0.32f, 0.03f, 0.02f), dGlow);
            r.grip = PremiumRig.Grip.TwoHand;
            r.handleOffset = -0.16f;
            swordRest = Quaternion.Euler(-100f, 28f, 0f);
            SwordPivot.localRotation = swordRest;
            BroadBlade(SwordPivot, 1.5f, 0.3f, PM(new Color(0.3f, 0.28f, 0.34f), 0.012f), dGlow, dDark, trim, 0.42f);
            DemonTrail(1.7f, d.accentColor);
        }

        void DemonBrute(EnemyDefinition d, PremiumRig r)
        {
            // Gorvath: a hulking horned butcher with a leather apron, chains and a great cleaver.
            DemonHead(new Vector3(0.48f, 0.44f, 0.48f), dSkin);
            Ball(head, dHc + new Vector3(0f, -0.14f, 0.1f), new Vector3(0.4f, 0.2f, 0.34f), dSkin);
            for (int s = -1; s <= 1; s += 2) ConePart(head, dHc + new Vector3(s * 0.1f, -0.15f, 0.25f), new Vector3(0.04f, 0.09f, 0.035f), dBone, Vector3.zero);
            DemonEyes(0.09f, 0.02f, 0.07f, d.accentColor);
            Horns(0.14f, 0.12f, 0.6f, 0.06f, 20f, dBone, 2);
            var leather = PM(new Color(0.4f, 0.28f, 0.22f), 0.015f);
            var apron = Part(PrimitiveType.Cube, r.torso, new Vector3(0f, 0.12f, 0.28f), new Vector3(0.5f, 0.6f, 0.025f), leather, new Vector3(-6f, 0f, 0f));
            apron.AddComponent<Sway>().Amount = 2f;
            var chain = PM(new Color(0.5f, 0.5f, 0.55f), 0.01f);
            for (int k = 0; k < 9; k++)
            {
                float t = k / 8f;
                Band(r.torso, new Vector3(Mathf.Lerp(-0.34f, 0.3f, t), Mathf.Lerp(0.55f, 0.1f, t), 0.3f - Mathf.Sin(t * Mathf.PI) * 0.02f), 0.035f, 0.02f, 0.01f, chain, null, new Vector3(0f, 0f, 90f + (k % 2) * 90f));
            }
            Loincloth(r, leather, 0.28f, 0.3f);
            for (int i = 0; i < 2; i++) for (int k = 0; k < 2; k++) Band(r.lower[i], new Vector3(0f, -0.08f - k * 0.12f, 0f), 0.12f, 0.035f, 0.012f, leather);
            r.grip = PremiumRig.Grip.OneHand;
            SwordPivot.localRotation = swordRest;
            BroadBlade(SwordPivot, 1f, 0.46f, PM(new Color(0.55f, 0.52f, 0.5f), 0.012f), PM(new Color(0.85f, 0.85f, 0.88f), 0f), leather, chain, 0.32f);
            DemonTrail(1.2f, new Color(0.9f, 0.6f, 0.3f));
        }

        void DemonGuardian(EnemyDefinition d, PremiumRig r)
        {
            // Ancient moss-covered stone colossus: a serene gold mask, glowing runes, a halo of floating stones, a ring-blade.
            var stone = PM(new Color(0.75f, 0.74f, 0.68f), 0.02f);
            var moss = PM(new Color(0.3f, 0.47f, 0.25f), 0.012f);
            var gold = PM(new Color(0.88f, 0.72f, 0.3f), 0.01f);
            var rune = PMe(new Color(0.35f, 0.95f, 1f), 0f, new Color(0.3f, 0.9f, 1f));
            foreach (var rend in r.body.GetComponentsInChildren<Renderer>()) rend.sharedMaterial = stone;
            dSkin = stone;
            Part(PrimitiveType.Cube, r.torso, new Vector3(0f, 0.3f, 0.27f), new Vector3(0.03f, 0.4f, 0.02f), rune);
            Part(PrimitiveType.Cube, r.torso, new Vector3(0f, 0.2f, 0.27f), new Vector3(0.4f, 0.03f, 0.02f), rune);
            for (int k = 0; k < 5; k++) Ball(r.torso, new Vector3(-0.2f + k * 0.1f, 0.52f, (k % 2) * 0.1f - 0.05f), new Vector3(0.2f, 0.1f, 0.18f), moss);
            Pauldrons(r, stone, moss, 1.4f, false);
            // Two extra arms folded in prayer below the main pair.
            for (int s = -1; s <= 1; s += 2)
            {
                var la = J("LowerArm", r.torso, new Vector3(s * 0.3f, 0.2f, 0.05f));
                la.localRotation = Quaternion.Euler(-50f, 0f, s * 35f);
                Taper(la, Vector3.zero, 0.32f, 0.08f, 0.07f, stone);
                Ball(la, new Vector3(0f, -0.35f, 0f), new Vector3(0.12f, 0.14f, 0.1f), stone);
                la.gameObject.AddComponent<Sway>().Amount = 3f;
            }
            DemonHead(new Vector3(0.5f, 0.52f, 0.5f), stone);
            var mask = OnFace(dHc, dHrad, 0f, 0f, 0.005f);
            Ball(mask, new Vector3(0f, 0f, 0.01f), new Vector3(0.36f, 0.42f, 0.08f), gold);
            Part(PrimitiveType.Capsule, mask, new Vector3(0f, -0.1f, 0.05f), new Vector3(0.012f, 0.05f, 0.01f), PM(new Color(0.35f, 0.28f, 0.12f), 0f), new Vector3(0f, 0f, 90f));
            for (int s = -1; s <= 1; s += 2)
                Part(PrimitiveType.Capsule, mask, new Vector3(s * 0.07f, 0.03f, 0.05f), new Vector3(0.014f, 0.045f, 0.01f), rune, new Vector3(0f, 0f, 90f - s * 10f));
            var halo = J("Halo", head, dHc + new Vector3(0f, 0.08f, -0.38f));
            halo.localRotation = Quaternion.Euler(90f, 0f, 0f);
            for (int i = 0; i < 10; i++)
            {
                float a = i * 36f * Mathf.Deg2Rad;
                var st = MeshFactory.MeshObject(MeshFactory.Rock(i), halo, new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * 0.6f, Vector3.one * 0.14f, i % 3 == 0 ? rune : stone);
                Add(st);
            }
            halo.gameObject.AddComponent<Spinner>().DegreesPerSecond = new Vector3(0f, 25f, 0f);
            r.grip = PremiumRig.Grip.OneHand;
            SwordPivot.localRotation = swordRest;
            Part(PrimitiveType.Cylinder, SwordPivot, new Vector3(0f, 0f, 0.3f), new Vector3(0.06f, 0.35f, 0.06f), gold, new Vector3(90f, 0f, 0f));
            var ring = J("RingBlade", SwordPivot, new Vector3(0f, 0f, 0.95f));
            ring.localRotation = Quaternion.Euler(0f, 90f, 0f);
            for (int k = 0; k < 18; k++)
            {
                float a = k * 20f * Mathf.Deg2Rad;
                Ball(ring, new Vector3(Mathf.Cos(a) * 0.32f, 0f, Mathf.Sin(a) * 0.32f), new Vector3(0.1f, 0.05f, 0.12f), k % 3 == 0 ? rune : stone);
            }
            DemonTrail(1.2f, new Color(0.35f, 0.95f, 1f));
        }

        void DemonImp(EnemyDefinition d, PremiumRig r)
        {
            // A small lava imp: big round head, little horns, bat ears, a burning tail tip, a pitchfork.
            var lava = PMe(d.accentColor, 0f, d.accentColor);
            DemonHead(new Vector3(0.6f, 0.56f, 0.56f), dSkin);
            for (int s = -1; s <= 1; s += 2) ConePart(head, dHc + new Vector3(s * 0.28f, 0.05f, -0.02f), new Vector3(0.08f, 0.22f, 0.04f), dSkinDk, new Vector3(0f, 0f, -s * 70f));
            DemonEyes(0.11f, 0.02f, 0.09f, new Color(1f, 0.9f, 0.3f));
            Horns(0.12f, 0.2f, 0.25f, 0.035f, 15f, lava);
            var grin = OnFace(dHc, dHrad, 0f, -0.14f);
            Ball(grin, Vector3.zero, new Vector3(0.14f, 0.04f, 0.02f), PM(new Color(0.15f, 0.03f, 0.03f), 0f));
            var tail = J("Tail", r.pelvis, new Vector3(0f, -0.02f, -0.12f));
            tail.localRotation = Quaternion.Euler(-120f, 0f, 0f);
            Taper(tail, Vector3.zero, 0.35f, 0.035f, 0.02f, dSkin);
            ConePart(tail, new Vector3(0f, -0.38f, 0f), new Vector3(0.08f, 0.12f, 0.03f), lava, new Vector3(180f, 0f, 0f));
            tail.gameObject.AddComponent<Sway>().Amount = 18f;
            Part(PrimitiveType.Cube, r.torso, new Vector3(0f, 0.12f, 0.17f), new Vector3(0.16f, 0.015f, 0.01f), lava, new Vector3(0f, 0f, 10f));
            EnvFx.Fire(head, dHc + new Vector3(0f, 0.3f, 0f), 0.15f, false);
            r.grip = PremiumRig.Grip.OneHand;
            SwordPivot.localRotation = swordRest;
            Part(PrimitiveType.Cylinder, SwordPivot, new Vector3(0f, 0f, 0.15f), new Vector3(0.025f, 0.35f, 0.025f), dDark, new Vector3(90f, 0f, 0f));
            for (int k = -1; k <= 1; k++)
            {
                var prong = MeshFactory.MeshObject(MeshFactory.FacetCone(4), SwordPivot, new Vector3(k * 0.05f, 0f, 0.55f), new Vector3(0.025f, 0.14f, 0.025f), lava);
                prong.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                Add(prong);
            }
            Part(PrimitiveType.Cube, SwordPivot, new Vector3(0f, 0f, 0.49f), new Vector3(0.13f, 0.02f, 0.02f), dDark);
            DemonTrail(0.6f, new Color(1f, 0.6f, 0.2f));
        }

        void DemonLord(EnemyDefinition d, PremiumRig r)
        {
            // Veyrath: a tall regal demon king. Silver hair, a horn crown, black armour, crimson cape, a void in the chest.
            var armor = PM(new Color(0.07f, 0.06f, 0.08f), 0.018f);
            var crimson = PM(new Color(0.58f, 0.05f, 0.13f), 0.018f);
            var gold = PM(new Color(0.92f, 0.74f, 0.32f), 0.01f);
            var skin = PM(new Color(0.8f, 0.8f, 0.84f), 0.018f);
            var silver = PM(new Color(0.93f, 0.93f, 0.96f), 0.015f);
            var voidM = PMe(new Color(0.85f, 0.12f, 0.5f), 0f, new Color(0.8f, 0.1f, 0.45f));
            foreach (var rend in r.body.GetComponentsInChildren<Renderer>()) rend.sharedMaterial = armor;
            Shell(r.torso, "dm_lplate", new[] { new Vector2(0.17f, 0.05f), new Vector2(0.2f, 0.2f), new Vector2(0.24f, 0.4f), new Vector2(0.21f, 0.52f), new Vector2(0.12f, 0.6f), new Vector2(0f, 0.62f) },
                Vector3.zero, new Vector3(1.05f, 1f, 0.78f), armor);
            Ball(r.torso, new Vector3(0.06f, 0.4f, 0.18f), new Vector3(0.08f, 0.08f, 0.03f), gold);
            Ball(r.torso, new Vector3(-0.07f, 0.42f, 0.18f), new Vector3(0.1f, 0.12f, 0.04f), voidM);
            Band(r.torso, new Vector3(0f, 0.08f, 0f), 0.19f, 0.03f, 0.01f, gold, new Vector3(1.05f, 1f, 0.78f));
            Pauldrons(r, armor, crimson, 1f, true);
            Shell(r.pelvis, "dm_lrobe", new[] { new Vector2(0.28f, -0.8f), new Vector2(0.28f, -0.76f), new Vector2(0.22f, -0.4f), new Vector2(0.17f, -0.05f), new Vector2(0.15f, 0.03f) },
                Vector3.zero, Vector3.one, armor);
            Band(r.pelvis, new Vector3(0f, -0.77f, 0f), 0.278f, 0.03f, 0.01f, gold);
            var cape = J("Cape", r.torso, new Vector3(0f, 0.56f, -0.18f));
            Tatters(cape, Vector3.zero, 0.6f, 1.45f, crimson, 7, 6f);
            DemonHead(new Vector3(0.38f, 0.42f, 0.4f), skin);
            Ball(head, dHc + new Vector3(0f, 0.07f, -0.03f), new Vector3(0.42f, 0.34f, 0.42f), silver);
            var hair = J("Hair", head, dHc + new Vector3(0f, -0.05f, -0.16f));
            Part(PrimitiveType.Cube, hair, new Vector3(0f, -0.35f, 0f), new Vector3(0.34f, 0.7f, 0.06f), silver, new Vector3(6f, 0f, 0f));
            hair.gameObject.AddComponent<Sway>().Amount = 2f;
            DemonEyes(0.07f, 0.0f, 0.055f, new Color(1f, 0.15f, 0.2f));
            // Crown of horns.
            for (int k = 0; k < 5; k++)
            {
                float a = (k - 2) * 28f;
                var cr = J("Crown", head, dHc + Quaternion.Euler(0f, a, 0f) * new Vector3(0f, 0.14f, 0.12f));
                cr.localRotation = Quaternion.Euler(-15f, a, 0f);
                ConePart(cr, Vector3.zero, new Vector3(0.05f, 0.22f - Mathf.Abs(k - 2) * 0.04f, 0.05f), armor, Vector3.zero);
                if (k == 2) Ball(cr, new Vector3(0f, 0.02f, 0.03f), Vector3.one * 0.04f, voidM);
            }
            Horns(0.12f, 0.1f, 0.4f, 0.035f, 30f, armor);
            r.grip = PremiumRig.Grip.TwoHand;
            r.handleOffset = -0.14f;
            SwordPivot.localRotation = swordRest;
            BroadBlade(SwordPivot, 1.4f, 0.2f, armor, PMe(new Color(1f, 0.15f, 0.25f), 0f, new Color(0.9f, 0.1f, 0.2f)), crimson, gold, 0.36f);
            DemonTrail(1.6f, new Color(1f, 0.1f, 0.2f));
        }
    }
}
