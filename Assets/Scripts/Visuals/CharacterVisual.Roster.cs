using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// The premium standard for the whole roster. Every slayer (and every villager or soldier in the story) is built
    /// on the same jointed rig as the three hand-designed test fighters, driven by their data:
    ///   frame     — standard, heavy (tanks, brawlers, heavy weapons) or slim (casters, healers, dancers)
    ///   outfit    — long haori over hakama, a light striker's cropped jacket, armour, or a layered caster robe
    ///   palette   — outfit colour (primary), robe colour (secondary) and accent, plus element rim light
    ///   face      — eyes, brows and mouth from the movement personality; iris in the element glow
    ///   hair      — their own hairstyle, rebuilt on the new head
    ///   weapon    — a detailed version of their weapon kind with the matching grip
    ///   extras    — scarf, cape, pelt, armour, bell, and more gold and ornaments the rarer they are
    /// </summary>
    public partial class CharacterVisual
    {
        enum Frame { Standard, Heavy, Slim }
        enum Outfit { Haori, Light, Armored, Robe }

        void BuildRoster(CharacterDefinition def)
        {
            Motion = def.motion;
            Weapon = def.weapon;
            int rarity = def.npc ? 1 : Mathf.Clamp(def.rarity, 2, 6);
            Color elem = def.npc ? def.bladeColor : ElementChart.ColorOf(def.element);
            pRim = Color.Lerp(elem, Color.white, 0.35f);
            pShadow = Color.Lerp(new Color(0.55f, 0.5f, 0.72f), Color.Lerp(elem, new Color(0.4f, 0.4f, 0.5f), 0.6f), 0.35f);

            // ---- Frame and outfit
            bool heavyWeapon = def.weapon == WeaponKind.Greatsword || def.weapon == WeaponKind.SwordShield;
            Frame frame = Frame.Standard;
            if (def.style == CombatStyle.Heavy || def.style == CombatStyle.Brawler || def.role == Role.Tank || def.bodyWidth >= 1.15f) frame = Frame.Heavy;
            else if (def.bodyWidth <= 0.93f || def.style == CombatStyle.Healer || (def.style == CombatStyle.Ranged && def.motion == MotionStyle.Graceful)) frame = Frame.Slim;
            Outfit outfit = Outfit.Haori;
            if (def.style == CombatStyle.Swift) outfit = Outfit.Light;
            if (def.style == CombatStyle.Healer || def.weapon == WeaponKind.Staff || def.weapon == WeaponKind.Fans) outfit = Outfit.Robe;
            if ((def.armor && frame == Frame.Heavy) || (def.style == CombatStyle.Heavy && heavyWeapon)) outfit = Outfit.Armored;

            // ---- Palette
            // Bold colour blocking: vivid colours, and hair / outfit / under-layer kept clearly apart.
            Color primary = SeparateFrom(Vivid(def.haoriColor), def.hairColor, 0.35f);
            Color secondary = SeparateFrom(Vivid(def.bodyColor), primary, 0.3f);
            Color accent = Vivid(def.accentColor);
            if (Diff(accent, primary) < 0.08f) accent = Color.Lerp(def.bladeColor, new Color(0.95f, 0.78f, 0.35f), 0.5f);
            Color gold = new Color(0.95f, 0.78f, 0.34f);
            Color trimC = rarity >= 4 ? gold : accent;
            Color pantsC = Color.Lerp(secondary, new Color(0.08f, 0.08f, 0.1f), 0.35f);
            Color light = Color.Lerp(new Color(0.94f, 0.94f, 0.92f), primary, 0.08f);
            var mP = PM(primary); var mPd = PM(Color.Lerp(primary, Color.black, 0.35f)); var mS = PM(secondary);
            var mA = PM(accent, 0.01f); var mTrim = PM(trimC, 0.008f); var mPants = PM(pantsC); var mLight = PM(light, 0.01f);
            var mSkin = PM(def.skinTone); var mBoot = PM(Color.Lerp(pantsC, new Color(0.1f, 0.08f, 0.07f), 0.5f));
            var mSole = PM(new Color(0.07f, 0.06f, 0.06f), 0.008f); var mLeather = PM(new Color(0.34f, 0.22f, 0.14f));
            var mMetal = PM(Color.Lerp(new Color(0.62f, 0.64f, 0.7f), accent, 0.12f), 0.012f);
            var mGlowE = PMe(elem, 0f, elem * 0.9f);

            // ---- Skeleton
            PremiumRig r;
            Vector3 hc, hr;
            float torsoW, torsoD;
            switch (frame)
            {
                case Frame.Heavy:
                    r = NewRig(0.62f, 0.12f, 0.28f, 0.27f, 0.09f, new Vector3(0.31f, 0.3f, -0.01f), 0.22f, 0.21f, new Vector3(0f, 0.38f, 0f));
                    hc = new Vector3(0f, 0.32f, 0.02f); hr = new Vector3(0.34f, 0.34f, 0.33f);
                    torsoW = 1.2f; torsoD = 0.9f;
                    r.stance = 0.17f; r.toeOut = 16f;
                    break;
                case Frame.Slim:
                    r = NewRig(0.62f, 0.075f, 0.28f, 0.27f, 0.08f, new Vector3(0.2f, 0.28f, -0.01f), 0.2f, 0.19f, new Vector3(0f, 0.35f, 0f));
                    hc = new Vector3(0f, 0.32f, 0.02f); hr = new Vector3(0.36f, 0.36f, 0.34f);
                    torsoW = 1f; torsoD = 0.82f;
                    r.stance = 0.08f;
                    break;
                default:
                    r = NewRig(0.6f, 0.085f, 0.27f, 0.27f, 0.08f, new Vector3(0.215f, 0.29f, -0.01f), 0.2f, 0.19f, new Vector3(0f, 0.36f, 0f));
                    hc = new Vector3(0f, 0.33f, 0.02f); hr = new Vector3(0.37f, 0.36f, 0.35f);
                    torsoW = 1.05f; torsoD = 0.82f;
                    r.stance = 0.1f;
                    break;
            }
            // Proportions of their own (Design Bible): leg length from how they fight, head size from their age and
            // manner, shoulder width from their build, plus a small personal variation so no two match.
            var pv = new System.Random(IdSeed(def.id));
            float jitter = 1f + ((float)pv.NextDouble() - 0.5f) * 0.06f;
            float legK = def.style == CombatStyle.Swift ? 1.1f : def.style == CombatStyle.Ranged ? 1.08f : def.style == CombatStyle.Heavy ? 0.92f
                : def.style == CombatStyle.Brawler ? 0.95f : def.style == CombatStyle.Healer ? 0.93f : 1f;
            legK *= jitter;
            float headK = (def.motion == MotionStyle.Nervous || def.motion == MotionStyle.Light ? 1.07f : def.motion == MotionStyle.Stoic || def.motion == MotionStyle.Confident ? 0.95f : 1f)
                * (frame == Frame.Heavy ? 0.9f : frame == Frame.Slim ? 1.03f : 1f) * (1f + ((float)pv.NextDouble() - 0.5f) * 0.05f);
            float shoulderK = Mathf.Clamp(Mathf.Lerp(0.94f, 1.1f, (def.bodyWidth - 0.85f) / 0.5f), 0.92f, 1.1f);
            r.hipY *= legK; r.legA *= legK; r.legB *= legK;
            r.shoulder = new Vector3(r.shoulder.x * shoulderK, r.shoulder.y, r.shoulder.z);
            torsoW *= shoulderK;
            for (int i = 0; i < 2; i++) { r.thigh[i].localScale = new Vector3(1f, legK, 1f); r.shin[i].localScale = new Vector3(1f, legK, 1f); }
            head.localScale = Vector3.one * headK;
            float frameScale = (frame == Frame.Heavy ? 1.06f : 1f) * Mathf.Clamp(def.bodyHeight, 0.85f, 1.2f);
            Model.localScale = Vector3.one * frameScale;
            float sx = r.shoulder.x;
            r.restHandR = new Vector3(sx + 0.1f, frame == Frame.Heavy ? 0.06f : 0.03f, 0.08f);
            r.restHandL = new Vector3(-sx - 0.1f, frame == Frame.Heavy ? 0.06f : 0.03f, 0.06f);
            PersonalityRig(r, def);
            var T = r.torso;
            Vector3 ts = new Vector3(torsoW, 1f, torsoD);
            string fk = frame.ToString();

            // ---- Torso
            float chestR = frame == Frame.Heavy ? 0.2f : frame == Frame.Slim ? 0.13f : 0.145f;
            Shell(T, "ro_chest_" + fk, new[] { new Vector2(0f, -0.04f), new Vector2(chestR * 0.8f, -0.03f), new Vector2(chestR * 0.84f, 0.05f), new Vector2(chestR * 0.96f, 0.15f),
                new Vector2(chestR, 0.23f), new Vector2(chestR * 0.87f, 0.3f), new Vector2(chestR * 0.5f, 0.345f), new Vector2(0f, 0.355f) }, Vector3.zero, ts, outfit == Outfit.Light ? mLight : mS);
            // Crossed collar: the inner robe shows in a V with trimmed edges.
            Material vMat = outfit == Outfit.Light ? mLight : Diff(secondary, light) < 0.2f ? mP : mLight;
            switch (outfit)
            {
                case Outfit.Light:
                    // Cropped jacket, high collar.
                    Shell(T, "ro_jacket_" + fk, new[] { new Vector2(chestR + 0.004f, 0.1f), new Vector2(chestR * 1.05f + 0.004f, 0.16f), new Vector2(chestR * 1.1f + 0.004f, 0.235f),
                        new Vector2(chestR * 0.97f, 0.305f), new Vector2(chestR * 0.55f, 0.35f), new Vector2(0f, 0.36f) }, Vector3.zero, ts, mP);
                    Band(T, new Vector3(0f, 0.105f, 0f), chestR + 0.002f, 0.02f, 0.008f, mTrim, ts);
                    Shell(T, "ro_hcollar", new[] { new Vector2(0.075f, 0.3f), new Vector2(0.088f, 0.34f), new Vector2(0.1f, 0.4f) }, Vector3.zero, Vector3.one, mP);
                    Band(T, new Vector3(0f, 0.4f, 0f), 0.1f, 0.014f, 0.006f, mTrim);
                    break;
                case Outfit.Armored:
                    ChestPlate(T, chestR, ts, mP, mS, mTrim, rarity);
                    break;
                case Outfit.Robe:
                    Shell(T, "ro_mantle_" + fk, new[] { new Vector2(chestR + 0.075f, 0.2f), new Vector2(chestR + 0.07f, 0.23f), new Vector2(chestR + 0.04f, 0.28f), new Vector2(chestR * 0.85f, 0.32f),
                        new Vector2(0.07f, 0.345f), new Vector2(0f, 0.35f) }, Vector3.zero, new Vector3(1f, 1f, 0.9f), mP);
                    Band(T, new Vector3(0f, 0.205f, 0f), chestR + 0.075f, 0.018f, 0.007f, mTrim, new Vector3(1f, 1f, 0.9f));
                    break;
                default:
                    // Haori over the robe: shoulders and back, open at the front.
                    Shell(T, "ro_haori_" + fk, new[] { new Vector2(chestR + 0.012f, -0.02f), new Vector2(chestR * 1.03f + 0.012f, 0.1f), new Vector2(chestR * 1.07f + 0.012f, 0.22f),
                        new Vector2(chestR * 0.95f + 0.01f, 0.3f), new Vector2(chestR * 0.55f, 0.348f), new Vector2(0f, 0.358f) }, Vector3.zero, ts, mP);
                    break;
            }
            // Outer layer radius at chest height (so the collar and sash sit on top of it, not inside).
            float outerR = outfit == Outfit.Light ? chestR * 1.1f + 0.004f : outfit == Outfit.Haori ? chestR * 1.07f + 0.012f : chestR;
            if (outfit == Outfit.Haori || outfit == Outfit.Light)
                for (int s = -1; s <= 1; s += 2)
                {
                    var panel = Part(PrimitiveType.Cube, T, new Vector3(s * 0.03f, 0.23f, outerR * torsoD * 0.97f), new Vector3(0.068f, 0.17f, 0.012f), vMat, new Vector3(-16f, 0f, s * 24f));
                    Part(PrimitiveType.Cube, panel.transform, new Vector3(s * 0.5f, 0f, 0.2f), new Vector3(0.2f, 1f, 1.2f), mTrim);
                }
            Ball(T, new Vector3(0f, 0.34f, 0f), new Vector3(0.12f, 0.1f, 0.12f), mSkin);

            // Sash / belt.
            float sashR = outfit == Outfit.Haori ? chestR * 1.03f + 0.016f : chestR * 0.86f;
            Band(T, new Vector3(0f, 0.03f, 0f), sashR, 0.06f, 0.014f, outfit == Outfit.Light ? mLeather : mA, ts);
            if (rarity >= 4) Band(T, new Vector3(0f, 0.03f, 0f), sashR + 0.012f, 0.01f, 0.005f, mTrim, ts);
            Ball(T, new Vector3(-sashR * 0.8f * torsoW, 0.035f, sashR * torsoD * 0.6f), new Vector3(0.07f, 0.06f, 0.05f), outfit == Outfit.Light ? mLeather : mA);
            var knot = Part(PrimitiveType.Cube, T, new Vector3(-sashR * 0.85f * torsoW, -0.06f, sashR * torsoD * 0.6f), new Vector3(0.045f, 0.15f, 0.012f), outfit == Outfit.Light ? mLeather : mA, new Vector3(-6f, 0f, 10f));
            knot.AddComponent<Sway>().Amount = 6f;
            if (def.bell) Ball(T, new Vector3(sashR * 0.5f, -0.02f, sashR * torsoD + 0.02f), Vector3.one * 0.06f, PM(gold, 0.006f));

            // ---- Hips and legs
            float hipR = frame == Frame.Heavy ? 0.18f : frame == Frame.Slim ? 0.13f : 0.15f;
            switch (outfit)
            {
                case Outfit.Robe:
                    Shell(r.pelvis, "ro_robe_" + fk, new[] { new Vector2(hipR + 0.12f, -0.56f), new Vector2(hipR + 0.122f, -0.53f), new Vector2(hipR + 0.08f, -0.3f), new Vector2(hipR + 0.03f, -0.1f),
                        new Vector2(hipR, 0f), new Vector2(hipR - 0.01f, 0.04f) }, Vector3.zero, Vector3.one, mS);
                    Band(r.pelvis, new Vector3(0f, -0.545f, 0f), hipR + 0.118f, 0.022f, 0.008f, mTrim);
                    Shell(r.pelvis, "ro_robe2_" + fk, new[] { new Vector2(hipR + 0.1f, -0.42f), new Vector2(hipR + 0.06f, -0.2f), new Vector2(hipR + 0.015f, -0.02f), new Vector2(hipR + 0.005f, 0.03f) },
                        Vector3.zero, Vector3.one, mP);
                    Band(r.pelvis, new Vector3(0f, -0.41f, 0f), hipR + 0.097f, 0.02f, 0.008f, mTrim);
                    break;
                case Outfit.Armored:
                    Shell(r.pelvis, "ro_hakamaW_" + fk, new[] { new Vector2(hipR + 0.12f, -0.5f), new Vector2(hipR + 0.122f, -0.47f), new Vector2(hipR + 0.09f, -0.3f), new Vector2(hipR + 0.04f, -0.12f),
                        new Vector2(hipR, 0f), new Vector2(hipR - 0.01f, 0.03f) }, Vector3.zero, Vector3.one, mPants);
                    Band(r.pelvis, new Vector3(0f, -0.48f, 0f), hipR + 0.118f, 0.035f, 0.01f, mA);
                    // Armoured tassets over the hips.
                    for (int k = -2; k <= 2; k++)
                    {
                        var tas = Part(PrimitiveType.Cube, r.pelvis, Quaternion.Euler(0f, k * 32f, 0f) * new Vector3(0f, -0.1f, hipR + 0.02f), new Vector3(0.11f, 0.16f, 0.02f), k % 2 == 0 ? mP : mPd);
                        tas.transform.localRotation = Quaternion.Euler(-12f, k * 32f, 0f);
                    }
                    break;
                case Outfit.Light:
                    Shell(r.pelvis, "ro_hips_" + fk, new[] { new Vector2(hipR + 0.01f, -0.13f), new Vector2(hipR + 0.005f, -0.06f), new Vector2(hipR - 0.015f, 0f), new Vector2(hipR - 0.03f, 0.03f) },
                        Vector3.zero, new Vector3(1.05f, 1f, 0.86f), mPants);
                    break;
                default:
                    // Haori tails hang to the knees over the hakama.
                    Shell(r.pelvis, "ro_hakama_" + fk, new[] { new Vector2(hipR + 0.012f, -0.14f), new Vector2(hipR + 0.006f, -0.06f), new Vector2(hipR - 0.015f, 0f), new Vector2(hipR - 0.03f, 0.03f) },
                        Vector3.zero, new Vector3(1.05f, 1f, 0.9f), mPants);
                    Shell(T, "ro_tails_" + fk, new[] { new Vector2(chestR * 1.3f + 0.03f, -0.36f), new Vector2(chestR * 1.3f + 0.035f, -0.33f), new Vector2(chestR * 1.15f + 0.02f, -0.18f),
                        new Vector2(chestR + 0.015f, -0.03f), new Vector2(chestR * 0.95f, 0f) }, Vector3.zero, new Vector3(torsoW, 1f, torsoD * 1.05f), mP);
                    Band(T, new Vector3(0f, -0.345f, 0f), chestR * 1.3f + 0.033f, 0.026f, 0.008f, mTrim, new Vector3(torsoW, 1f, torsoD * 1.05f));
                    // The open front of the haori shows the dark hakama.
                    Part(PrimitiveType.Cube, T, new Vector3(0f, -0.2f, (chestR * 1.2f + 0.03f) * torsoD * 1.05f), new Vector3(0.1f, 0.34f, 0.012f), mPants, new Vector3(-14f, 0f, 0f));
                    if (rarity >= 6)
                        for (int k = 0; k < 8; k++)
                        {
                            float a = (k * 45f + 22.5f) * Mathf.Deg2Rad;
                            float rr = chestR * 1.3f + 0.04f;
                            Ball(T, new Vector3(Mathf.Sin(a) * rr * torsoW, -0.3f, Mathf.Cos(a) * rr * torsoD * 1.05f), new Vector3(0.05f, 0.05f, 0.02f), mGlowE, new Vector3(0f, k * 45f + 22.5f, 45f));
                        }
                    break;
            }
            for (int i = 0; i < 2; i++)
            {
                float thighR = frame == Frame.Heavy ? 0.1f : 0.075f;
                if (outfit == Outfit.Light) Taper(r.thigh[i], Vector3.zero, 0.24f, 0.078f, 0.098f, mPants);
                else Taper(r.thigh[i], Vector3.zero, 0.26f, thighR + 0.01f, thighR, mPants);
                Ball(r.shin[i], Vector3.zero, Vector3.one * (frame == Frame.Heavy ? 0.13f : 0.1f), outfit == Outfit.Armored ? mP : mPants);
                Taper(r.shin[i], Vector3.zero, 0.25f, frame == Frame.Heavy ? 0.07f : 0.052f, frame == Frame.Heavy ? 0.06f : 0.042f, mPants);
                if (outfit == Outfit.Light)
                    Band(r.shin[i], new Vector3(0f, -0.14f, 0f), 0.05f, 0.14f, 0.01f, mLight); // one bold wrap, not many thin ones
                else if (outfit == Outfit.Armored || def.armor)
                {
                    Taper(r.shin[i], new Vector3(0f, -0.02f, 0.012f), 0.19f, frame == Frame.Heavy ? 0.072f : 0.056f, frame == Frame.Heavy ? 0.064f : 0.048f, mMetal, new Vector3(1f, 1f, 1.05f));
                    Band(r.shin[i], new Vector3(0f, -0.05f, 0f), frame == Frame.Heavy ? 0.076f : 0.06f, 0.014f, 0.006f, mTrim);
                }
                else
                {
                    // Leg wraps above the boot.
                    Band(r.shin[i], new Vector3(0f, -0.2f, 0f), frame == Frame.Heavy ? 0.068f : 0.05f, 0.05f, 0.01f, mLight);
                }
                var f = r.foot[i];
                float fs = frame == Frame.Heavy ? 1.25f : 1f;
                if (outfit == Outfit.Robe)
                {
                    Ball(f, new Vector3(0f, -0.03f, 0.04f), new Vector3(0.085f, 0.07f, 0.17f), mLight);
                    Part(PrimitiveType.Cube, f, new Vector3(0f, -0.07f, 0.04f), new Vector3(0.1f, 0.022f, 0.22f), PM(new Color(0.42f, 0.28f, 0.18f), 0.01f));
                    Part(PrimitiveType.Cube, f, new Vector3(0f, -0.025f, 0.08f), new Vector3(0.09f, 0.012f, 0.02f), mA);
                }
                else
                {
                    Ball(f, new Vector3(0f, -0.035f, 0.045f), new Vector3(0.11f * fs, 0.085f, 0.2f * fs), mBoot);
                    Ball(f, new Vector3(0f, -0.045f, 0.11f * fs), new Vector3(0.1f * fs, 0.065f, 0.1f * fs), outfit == Outfit.Armored ? mMetal : mBoot);
                    Part(PrimitiveType.Cube, f, new Vector3(0f, -r.ankle + 0.012f, 0.05f), new Vector3(0.106f * fs, 0.022f, 0.23f * fs), mSole);
                    Band(f, new Vector3(0f, 0.005f, 0f), 0.046f * fs, 0.035f, 0.01f, mBoot);
                }
            }

            // ---- Arms
            bool gloves = frame == Frame.Heavy || def.weapon == WeaponKind.Fists || def.weapon == WeaponKind.Cleavers;
            for (int i = 0; i < 2; i++)
            {
                int side = i == 0 ? 1 : -1;
                float ua = frame == Frame.Heavy ? 0.078f : 0.058f;
                switch (outfit)
                {
                    case Outfit.Light:
                        Taper(r.upper[i], Vector3.zero, 0.19f, 0.054f, 0.048f, mSkin);
                        Ball(r.upper[i], new Vector3(0f, -0.01f, 0f), Vector3.one * 0.14f, mP);
                        Taper(r.upper[i], Vector3.zero, 0.09f, 0.068f, 0.066f, mP);
                        Band(r.upper[i], new Vector3(0f, -0.09f, 0f), 0.066f, 0.02f, 0.008f, mTrim);
                        Ball(r.lower[i], Vector3.zero, Vector3.one * 0.09f, mLight);
                        Taper(r.lower[i], Vector3.zero, 0.17f, 0.045f, 0.038f, mLight);
                        Band(r.lower[i], new Vector3(0f, -0.1f, 0f), 0.045f, 0.07f, 0.008f, mLeather);
                        break;
                    case Outfit.Robe:
                        Ball(r.upper[i], new Vector3(0f, -0.01f, 0f), Vector3.one * 0.12f, mP);
                        Taper(r.upper[i], Vector3.zero, 0.19f, 0.056f, 0.058f, mP);
                        Shell(r.lower[i], "ro_bell", new[] { new Vector2(0.12f, -0.17f), new Vector2(0.126f, -0.15f), new Vector2(0.09f, -0.07f), new Vector2(0.06f, 0f),
                            new Vector2(0.05f, 0.025f) }, Vector3.zero, Vector3.one, mLight);
                        Band(r.lower[i], new Vector3(0f, -0.155f, 0f), 0.124f, 0.022f, 0.008f, mTrim);
                        Part(PrimitiveType.Cylinder, r.lower[i], new Vector3(0f, -0.172f, 0f), new Vector3(0.23f, 0.004f, 0.23f), mA);
                        Taper(r.lower[i], Vector3.zero, 0.17f, 0.036f, 0.032f, mSkin);
                        break;
                    default:
                        // Wide haori sleeve on the upper arm, trimmed cuff, bare or wrapped forearm.
                        Ball(r.upper[i], new Vector3(0f, -0.01f, 0f), Vector3.one * (ua * 2.2f), mP);
                        Taper(r.upper[i], Vector3.zero, 0.2f, ua + 0.01f, ua + 0.022f, mP);
                        Band(r.upper[i], new Vector3(0f, -0.19f, 0f), ua + 0.02f, 0.026f, 0.009f, mTrim);
                        Ball(r.lower[i], Vector3.zero, Vector3.one * (ua * 1.6f), mS);
                        Taper(r.lower[i], Vector3.zero, 0.18f, ua * 0.82f, ua * 0.7f, frame == Frame.Heavy ? mSkin : mS);
                        if (frame == Frame.Heavy)
                        {
                            Taper(r.lower[i], new Vector3(0f, -0.07f, 0f), 0.11f, ua * 0.88f, ua * 0.8f, mPd);
                            Band(r.lower[i], new Vector3(0f, -0.07f, 0f), ua * 0.88f, 0.014f, 0.006f, mTrim);
                        }
                        else Band(r.lower[i], new Vector3(0f, -0.15f, 0f), ua * 0.74f, 0.04f, 0.008f, mLight);
                        break;
                }
                if (outfit == Outfit.Armored || def.armor)
                    for (int k = 0; k < 3; k++)
                        Ball(r.upper[i], new Vector3(side * 0.03f, 0.04f - k * 0.055f, 0f), new Vector3(0.28f - k * 0.035f, 0.12f, 0.26f - k * 0.03f) * (frame == Frame.Heavy ? 1f : 0.8f),
                            k == 1 ? mPd : mP, new Vector3(0f, 0f, -side * 18f));
                bool open = def.weapon == WeaponKind.Staff && i == 1 || def.weapon == WeaponKind.Bow && i == 0;
                PremiumHand(r.hand[i], gloves ? mLeather : mSkin, gloves ? mLeather : mSkin, side, !open, frame == Frame.Heavy ? 1.2f : 1f, gloves || rarity >= 4 ? mTrim : null);
            }

            // ---- Head, face, hair
            Ball(head, new Vector3(0f, 0.04f, 0f), new Vector3(0.1f, 0.16f, 0.1f), mSkin);
            Ball(head, hc, hr * 2f, mSkin); SetFace(hc, hr);
            for (int s = -1; s <= 1; s += 2) Ball(head, hc + new Vector3(s * (hr.x - 0.01f), -0.03f, -0.01f), new Vector3(0.07f, 0.12f, 0.08f), mSkin);
            RosterFaceV2(def, hc, hr, mSkin, elem);
            var realHead = head;
            // The sculpted hair (quality standard: Ren, Mina, Sora); everyone else keeps their current hair for now.
            bool sculpted = SculptedHair(def, hc, hr, mP);
            if (sculpted) HairExtras(def, hc, hr);
            if (!sculpted)
            {
                var hairRoot = J("HairRoot", head, hc);
                hairRoot.localScale = Vector3.one * (hr.x / 0.4f);
                head = hairRoot;
                BuildHair(def.hair, PM(def.hairColor, 0.014f), PM(Color.Lerp(accent, gold, rarity >= 5 ? 0.5f : 0f), 0.01f));
                HairDetail(def);
                head = realHead;
            }
            HeadY = (r.hipY + r.hover + 0.02f + r.neck.y + hc.y * headK) * frameScale;

            // ---- Accessories
            if (def.scarf)
            {
                Band(T, new Vector3(0f, 0.325f, 0f), 0.108f, 0.05f, 0.022f, mA);
                var tails = J("ScarfTails", T, new Vector3(0.04f, 0.33f, -0.11f));
                tails.localRotation = Quaternion.Euler(22f, 0f, -6f);
                for (int k = 0; k < 2; k++)
                {
                    Transform prev = J("Tail" + k, tails, new Vector3(-k * 0.07f, 0f, 0f));
                    prev.localRotation = Quaternion.Euler(8f + k * 6f, 0f, k == 0 ? -6f : 8f);
                    for (int sgm = 0; sgm < 3; sgm++)
                    {
                        Part(PrimitiveType.Cube, prev, new Vector3(0f, -0.11f, 0f), new Vector3(0.08f - sgm * 0.012f, 0.23f, 0.016f), mA);
                        var sw = prev.gameObject.AddComponent<Sway>();
                        sw.Amount = 5f + sgm * 4f;
                        sw.Speed = 1.6f + k * 0.2f;
                        var next = J("Seg", prev, new Vector3(0f, -0.22f, 0f));
                        next.localRotation = Quaternion.Euler(12f, 0f, 0f);
                        prev = next;
                    }
                }
            }
            if (def.cape)
            {
                var cape = J("Cape", T, new Vector3(0f, 0.32f, -chestR * torsoD - 0.04f));
                cape.localRotation = Quaternion.Euler(8f, 0f, 0f);
                for (int k = 0; k < 5; k++)
                {
                    var strip = J("Strip", cape, new Vector3(-0.16f + k * 0.08f, 0f, -Mathf.Abs(k - 2) * 0.012f));
                    strip.localRotation = Quaternion.Euler(3f, 0f, (k - 2) * 2.5f);
                    float len = 0.78f - Mathf.Abs(k - 2) * 0.05f;
                    Part(PrimitiveType.Cube, strip, new Vector3(0f, -len * 0.5f, 0f), new Vector3(0.085f, len, 0.02f), mPd);
                    Part(PrimitiveType.Cube, strip, new Vector3(0f, -len + 0.02f, 0f), new Vector3(0.088f, 0.04f, 0.024f), mTrim);
                    var sw = strip.gameObject.AddComponent<Sway>();
                    sw.Amount = 4f;
                    sw.Speed = 0.9f + k * 0.08f;
                }
            }
            if (def.pelt)
            {
                var fur = PM(new Color(0.62f, 0.58f, 0.52f), 0.012f);
                var furD = PM(new Color(0.45f, 0.4f, 0.35f), 0.012f);
                for (int k = 0; k < 6; k++)
                {
                    float a = -60f + k * 28f;
                    Vector3 p = Quaternion.Euler(0f, a, 0f) * new Vector3(0f, 0.3f, chestR * 0.9f);
                    Ball(T, new Vector3(-sx * 0.6f, 0f, 0f) + p, new Vector3(0.14f, 0.1f, 0.13f), k % 2 == 0 ? fur : furD);
                    ConePart(T, new Vector3(-sx * 0.6f, 0f, 0f) + p + Vector3.down * 0.05f, new Vector3(0.06f, 0.1f, 0.06f), fur, new Vector3(180f, a, 0f));
                }
            }
            if (def.armor && outfit != Outfit.Armored) ChestPlate(T, chestR, ts, mMetal, mS, mTrim, rarity);
            if (rarity >= 5)
            {
                // Legendary+: a glowing element crest on the chest and gold cords from the shoulders.
                float cz = (outfit == Outfit.Armored ? (chestR + 0.018f) * 1.06f * 1.02f : outerR) * torsoD;
                Ball(T, new Vector3(0f, 0.15f, cz + 0.02f), new Vector3(0.06f, 0.06f, 0.025f), mGlowE);
                Ball(T, new Vector3(0f, 0.15f, cz + 0.01f), new Vector3(0.085f, 0.085f, 0.02f), mTrim);
                Part(PrimitiveType.Cylinder, T, new Vector3(sx * 0.45f, 0.24f, cz * 0.95f), new Vector3(0.018f, 0.1f, 0.018f), mTrim, new Vector3(0f, 0f, 40f));
                ConePart(T, new Vector3(sx * 0.2f, 0.14f, cz), new Vector3(0.04f, 0.08f, 0.04f), mTrim, new Vector3(180f, 0f, 0f));
            }
            if (rarity >= 6)
            {
                // Mythic: a slowly turning halo and element motes that orbit the slayer.
                var halo = J("Halo", realHead, hc + new Vector3(0f, 0.1f, -hr.z - 0.08f));
                halo.localRotation = Quaternion.Euler(90f, 0f, 0f);
                var spin = J("HaloSpin", halo, Vector3.zero);
                MeshFactory.MeshObject(MeshFactory.Ring(0.9f), spin, Vector3.zero, Vector3.one * 0.34f, MaterialFactory.Additive(new Color(elem.r, elem.g, elem.b, 0.7f)), false);
                for (int k = 0; k < 4; k++)
                {
                    float a = k * 90f * Mathf.Deg2Rad;
                    Ball(spin, new Vector3(Mathf.Cos(a) * 0.32f, 0f, Mathf.Sin(a) * 0.32f), Vector3.one * 0.04f, k % 2 == 0 ? mGlowE : mTrim);
                }
                spin.gameObject.AddComponent<Spinner>().DegreesPerSecond = new Vector3(0f, 25f, 0f);
                var orbit = J("Orbit", r.body, new Vector3(0f, 0.8f, 0f));
                var os = orbit.gameObject.AddComponent<Spinner>();
                os.DegreesPerSecond = new Vector3(0f, 50f, 0f);
                os.BobHeight = 0.05f;
                for (int k = 0; k < 3; k++)
                {
                    Vector3 p = Quaternion.Euler(0f, k * 120f, 0f) * new Vector3(0f, 0.1f * (k - 1), 0.55f);
                    Ball(orbit, p, Vector3.one * 0.05f, mGlowE);
                    MeshFactory.MeshObject(MeshFactory.SmoothSphere(), orbit, p, Vector3.one * 0.13f, MaterialFactory.Additive(new Color(elem.r, elem.g, elem.b, 0.35f)), false);
                }
            }

            // ---- Weapon
            // One hero piece by role, for a silhouette of their own (Design Bible §5).
            if (def.role == Role.Tank && outfit != Outfit.Armored)
            {
                var pad = J("Pauldron", r.upper[1], new Vector3(-0.03f, 0.05f, 0f));
                for (int k = 0; k < 3; k++)
                    Ball(pad, new Vector3(-0.02f * k, -k * 0.065f, 0f), new Vector3(0.32f - k * 0.045f, 0.13f, 0.28f - k * 0.035f), k == 1 ? mTrim : mMetal, new Vector3(0f, 0f, 22f));
            }
            if (def.style == CombatStyle.Ranged && !def.cape)
            {
                var half = J("HalfCape", T, new Vector3(-0.1f, 0.33f, -0.04f));
                half.localRotation = Quaternion.Euler(0f, 0f, 18f);
                Ball(half, new Vector3(-0.04f, -0.02f, 0f), new Vector3(0.22f, 0.09f, 0.26f), mA);
                DStrips(half, new Vector3(-0.05f, -0.02f, -0.09f), new Vector3(10f, 0f, 0f), 3, 0.065f, 0.38f, 0.07f, 2, mA, mTrim);
            }
            if (def.style == CombatStyle.Healer)
            {
                var bow = J("ObiBow", T, new Vector3(0f, 0.05f, -chestR - 0.03f));
                for (int sd = -1; sd <= 1; sd += 2)
                    Ball(bow, new Vector3(sd * 0.1f, 0.02f, -0.02f), new Vector3(0.16f, 0.12f, 0.05f), mA, new Vector3(0f, 0f, sd * 15f));
                Ball(bow, new Vector3(0f, 0.01f, -0.03f), new Vector3(0.06f, 0.07f, 0.05f), mTrim);
                DStrips(bow, new Vector3(0f, -0.02f, -0.03f), new Vector3(8f, 0f, 0f), 2, 0.07f, 0.24f, 0.07f, 2, mA, null);
            }

            RosterWeapon(def, r, mTrim, mA, mS, mLeather, mMetal, rarity);
            RosterShowPose(def, r);
            OptimizeParts();
            r.Solve(0f);
        }

        static float Diff(Color a, Color b) { return Mathf.Abs(a.r - b.r) + Mathf.Abs(a.g - b.g) + Mathf.Abs(a.b - b.b); }

        /// <summary>A natural team-screen stance of their own, chosen from weapon and manner (never all identical).</summary>
        void RosterShowPose(CharacterDefinition def, PremiumRig r)
        {
            switch (def.weapon)
            {
                case WeaponKind.Staff: r.showPose = PremiumRig.ShowPose.StaffBothHands; r.showButt = 0.56f; return;
                case WeaponKind.Spear: r.showButt = 0.86f; r.showPose = def.motion == MotionStyle.Confident ? PremiumRig.ShowPose.OrbHand : PremiumRig.ShowPose.Natural; return;
                case WeaponKind.Bow: r.showPose = PremiumRig.ShowPose.HandOnHipR; return;
                case WeaponKind.Greatsword: r.showPose = PremiumRig.ShowPose.HandOnHipL; return;
            }
            switch (def.motion)
            {
                case MotionStyle.Confident: r.showPose = def.weapon == WeaponKind.Katana || def.weapon == WeaponKind.Moon ? PremiumRig.ShowPose.WeaponShoulder : PremiumRig.ShowPose.HandOnHipL; break;
                case MotionStyle.Sly: r.showPose = PremiumRig.ShowPose.HandOnHipL; break;
                case MotionStyle.Light: r.showPose = def.weapon == WeaponKind.Katana ? PremiumRig.ShowPose.WeaponShoulder : PremiumRig.ShowPose.Natural; break;
                case MotionStyle.Aggressive: r.showPose = def.weapon == WeaponKind.Fists ? PremiumRig.ShowPose.HandOnHipL : PremiumRig.ShowPose.WeaponShoulder; break;
                default: r.showPose = PremiumRig.ShowPose.Natural; break;
            }
        }

        /// <summary>
        /// A face of their own (Design Bible): eye shape, brows and mouth from their manner, then a personal
        /// variation of eye width and spacing, nose and at most one mark, all stable per character.
        /// </summary>
        void RosterFaceV2(CharacterDefinition def, Vector3 hc, Vector3 hr, Material skin, Color elem)
        {
            var pv = new System.Random(IdSeed(def.id) ^ 0x5bd1);
            var f = new FaceSpec();
            f.iris = Color.Lerp(def.bladeColor, elem, 0.4f);
            f.brows = Color.Lerp(def.hairColor, Color.black, 0.4f);
            f.lashes = def.hair == HairStyle.Long || def.hair == HairStyle.Twintails || def.hair == HairStyle.Bob || def.hair == HairStyle.Braid || def.hair == HairStyle.Bun;
            switch (def.motion)
            {
                case MotionStyle.Nervous: f.eyeH = 0.15f; f.lid = 0f; f.tilt = -6f; f.brow = -20f; f.mouth = "o"; f.mark = "blush"; break;
                case MotionStyle.Aggressive: f.eyeH = 0.11f; f.lid = 0.25f; f.tilt = 10f; f.brow = 26f; f.browThick = 0.028f; f.mouth = "grin"; break;
                case MotionStyle.Graceful: f.lid = 0.22f; f.tilt = -4f; f.brow = -8f; f.browThick = 0.016f; f.lashes = true; f.mouth = "smile"; f.mark = "blush"; break;
                case MotionStyle.Stoic: f.eyeH = 0.11f; f.lid = 0.3f; f.tilt = 0f; f.brow = 6f; f.browThick = 0.026f; f.mouth = "flat"; break;
                case MotionStyle.Confident: f.lid = 0.12f; f.tilt = 6f; f.brow = 12f; f.browThick = 0.024f; f.mouth = "smirk"; break;
                case MotionStyle.Sly: f.eyeH = 0.12f; f.lid = 0.28f; f.tilt = 8f; f.brow = 16f; f.mouth = "smirk"; break;
                case MotionStyle.Light: f.eyeH = 0.14f; f.lid = 0f; f.tilt = -2f; f.brow = -10f; f.mouth = "grin"; f.mark = "blush"; break;
                default: f.mouth = "smile"; break;
            }
            f.eyeW *= 0.93f + (float)pv.NextDouble() * 0.14f;
            f.eyeX += ((float)pv.NextDouble() - 0.5f) * 0.02f;
            f.eyeY += ((float)pv.NextDouble() - 0.5f) * 0.02f;
            f.nose = pv.NextDouble() < 0.5 ? "dot" : "line";
            if (string.IsNullOrEmpty(f.mark))
            {
                double m = pv.NextDouble();
                if (def.style == CombatStyle.Brawler || def.style == CombatStyle.Heavy) f.mark = m < 0.5 ? "scar" : m < 0.7 ? "bandage" : "";
                else f.mark = m < 0.18 ? "beauty" : m < 0.3 ? "bandage" : m < 0.4 ? "scar" : "";
            }
            var b = new Proportions { hc = hc, hr = hr };
            DFace(b, f, skin, def.skinTone);
        }

        /// <summary>Hair pieces that go beyond the sculpted base: a bun, twin tails, a long braid.</summary>
        void HairExtras(CharacterDefinition def, Vector3 hc, Vector3 hr)
        {
            var hm = PM(def.hairColor, 0.01f);
            switch (def.hair)
            {
                case HairStyle.Bun:
                {
                    Vector3 p = hc + new Vector3(0f, hr.y * 0.72f, -hr.z * 0.62f);
                    Ball(head, p, new Vector3(0.24f, 0.22f, 0.22f), hm);
                    Band(head, p + new Vector3(0f, -0.06f, 0.05f), 0.08f, 0.03f, 0.01f, PM(def.accentColor, 0.008f), null, new Vector3(-40f, 0f, 0f));
                    break;
                }
                case HairStyle.Twintails:
                    for (int s = -1; s <= 1; s += 2)
                    {
                        var t = J("Twintail", head, hc + new Vector3(s * hr.x * 0.82f, hr.y * 0.35f, -hr.z * 0.3f));
                        t.localRotation = Quaternion.Euler(0f, 0f, s * 18f);
                        Band(t, Vector3.zero, 0.05f, 0.04f, 0.012f, PM(def.accentColor, 0.008f));
                        Transform prev = t;
                        for (int k = 0; k < 5; k++)
                        {
                            Ball(prev, new Vector3(0f, -0.07f, 0f), new Vector3(0.16f - k * 0.02f, 0.16f, 0.14f - k * 0.015f), hm);
                            var sw = prev.gameObject.AddComponent<Sway>();
                            sw.Amount = 3f + k;
                            sw.Speed = 1f;
                            prev = J("T", prev, new Vector3(0f, -0.11f, 0f));
                        }
                    }
                    break;
                case HairStyle.Braid:
                {
                    var braid = J("Braid", head, hc + new Vector3(0f, -0.1f, -hr.z - 0.03f));
                    Transform prev = braid;
                    for (int k = 0; k < 6; k++)
                    {
                        Ball(prev, new Vector3(k % 2 == 0 ? 0.012f : -0.012f, -0.05f, 0f), new Vector3(0.1f - k * 0.006f, 0.1f, 0.08f - k * 0.004f), hm, new Vector3(0f, 0f, k % 2 == 0 ? 20f : -20f));
                        var sw = prev.gameObject.AddComponent<Sway>();
                        sw.Amount = 2f + k * 0.8f;
                        sw.Speed = 0.9f;
                        prev = J("B", prev, new Vector3(0f, -0.085f, -0.005f));
                    }
                    Band(prev, new Vector3(0f, 0.02f, 0f), 0.03f, 0.03f, 0.01f, PM(def.accentColor, 0.008f));
                    break;
                }
            }
        }

        void ChestPlate(Transform T, float chestR, Vector3 ts, Material plate, Material under, Material trim, int rarity)
        {
            string key = "ro_plate_" + chestR.ToString("F3");
            float pr = chestR + 0.018f;
            Shell(T, key, new[] { new Vector2(pr * 0.9f, 0.07f), new Vector2(pr * 1.0f, 0.14f), new Vector2(pr * 1.06f, 0.23f), new Vector2(pr * 0.95f, 0.3f),
                new Vector2(pr * 0.6f, 0.345f), new Vector2(0f, 0.35f) }, Vector3.zero, ts * 1.02f, plate);
            float[] ys = { 0.12f, 0.18f, 0.24f };
            float[] rs = { pr * 0.97f, pr * 1.03f, pr * 1.055f };
            for (int k = 0; k < 3; k++)
            {
                Band(T, new Vector3(0f, ys[k], 0f), rs[k], 0.012f, 0.005f, under, ts * 1.02f);
                if (rarity >= 4 && k == 1)
                    Ball(T, new Vector3(0f, ys[k], (rs[k] + 0.006f) * ts.z * 1.02f), new Vector3(0.07f, 0.05f, 0.02f), trim); // one bold stud
            }
        }

        /// <summary>Walk, stance and showcase pose from the movement personality.</summary>
        void PersonalityRig(PremiumRig r, CharacterDefinition def)
        {
            r.hasPose = true;
            switch (def.motion)
            {
                case MotionStyle.Nervous:
                    r.crouch = 0.04f; r.lean = 8f; r.tempo = 14f; r.stride = 0.14f; r.idleBounce = 0.005f; r.idleBounceFreq = 2.5f; r.weightShift = 0.01f;
                    r.poseBody = new Vector3(4f, 0f, -2f); r.poseDrop = 0.02f; r.poseWeapon = new Vector3(20f, 25f, 0f); r.poseHead = new Vector3(0f, 8f, 3f);
                    break;
                case MotionStyle.Aggressive:
                    r.crouch = 0.06f; r.lean = 12f; r.tempo = 10f; r.stride = 0.2f; r.twistGain = 0.4f; r.swingDip = 0.08f; r.swingLunge = 14f;
                    r.poseBody = new Vector3(6f, 0f, 0f); r.poseDrop = 0.03f; r.poseWeapon = new Vector3(-15f, 40f, 0f); r.poseHead = new Vector3(5f, 0f, 0f);
                    break;
                case MotionStyle.Graceful:
                    r.crouch = 0f; r.lean = -2f; r.tempo = 8f; r.stride = 0.12f; r.weightShift = 0.015f; r.swingDip = 0f; r.swingLunge = 4f; r.twistGain = 0.15f;
                    r.poseBody = new Vector3(-2f, 0f, 2f); r.poseDrop = 0f; r.poseWeapon = new Vector3(-80f, 10f, 0f); r.poseHead = new Vector3(0f, 0f, -5f);
                    if (def.weapon == WeaponKind.Staff && def.rarity >= 5) { r.hover = 0.14f; r.stride = 0.06f; r.lift = 0.03f; }
                    break;
                case MotionStyle.Stoic:
                    r.crouch = 0.01f; r.lean = 0f; r.tempo = 8.5f; r.stride = 0.15f;
                    r.poseBody = Vector3.zero; r.poseDrop = 0f; r.poseWeapon = new Vector3(70f, 20f, 0f); r.poseHead = Vector3.zero;
                    break;
                case MotionStyle.Confident:
                    r.crouch = 0.02f; r.lean = -4f; r.tempo = 10f; r.stride = 0.18f; r.weightShift = 0.02f; r.twistGain = 0.35f;
                    r.poseBody = new Vector3(-2f, 0f, 0f); r.poseDrop = 0f; r.poseWeapon = new Vector3(-105f, 30f, 0f); r.poseHead = new Vector3(-5f, 0f, 0f);
                    break;
                case MotionStyle.Sly:
                    r.crouch = 0.04f; r.lean = 8f; r.tempo = 11f; r.stride = 0.16f; r.weightShift = 0.02f;
                    r.poseBody = new Vector3(0f, 8f, 2f); r.poseDrop = 0f; r.poseWeapon = new Vector3(50f, 95f, 0f); r.poseHead = new Vector3(0f, -8f, 3f);
                    break;
                case MotionStyle.Light:
                    r.crouch = 0.035f; r.lean = 8f; r.tempo = 12.5f; r.stride = 0.19f; r.lift = 0.14f; r.idleBounce = 0.008f; r.idleBounceFreq = 2f;
                    r.poseBody = new Vector3(4f, 0f, 0f); r.poseDrop = 0.02f; r.poseWeapon = new Vector3(40f, 95f, 0f); r.poseHead = new Vector3(2f, -6f, 0f);
                    break;
                default:
                    r.crouch = 0.02f; r.lean = 4f; r.tempo = 11f; r.stride = 0.17f;
                    r.poseBody = new Vector3(2f, 0f, 0f); r.poseDrop = 0.01f; r.poseWeapon = new Vector3(38f, 50f, 0f); r.poseHead = Vector3.zero;
                    break;
            }
            if (def.style == CombatStyle.Heavy || def.role == Role.Tank) { r.swingDip = 0.1f; r.swingLunge = 16f; r.twistGain = 0.45f; r.tempo *= 0.85f; }
        }

        /// <summary>Eyes, brows and mouth from the personality; iris in the element glow.</summary>
        void RosterFace(CharacterDefinition def, Vector3 hc, Vector3 hr, Material skin, Color elem)
        {
            Color iris = Color.Lerp(def.bladeColor, elem, 0.4f);
            Color irisLow = Color.Lerp(iris, Color.white, 0.45f);
            Color browC = Color.Lerp(def.hairColor, Color.black, 0.4f);
            bool lashes = def.hair == HairStyle.Long || def.hair == HairStyle.Twintails || def.hair == HairStyle.Bob || def.hair == HairStyle.Braid || def.hair == HairStyle.Bun;
            float w = 0.112f, h = 0.13f, lid = 0.06f, tilt = 2f, brow = 4f, thick = 0.02f;
            switch (def.motion)
            {
                case MotionStyle.Nervous: h = 0.15f; lid = 0f; tilt = -6f; brow = -20f; break;
                case MotionStyle.Aggressive: h = 0.11f; lid = 0.25f; tilt = 10f; brow = 26f; thick = 0.028f; break;
                case MotionStyle.Graceful: lid = 0.22f; tilt = -4f; brow = -8f; thick = 0.016f; lashes = true; break;
                case MotionStyle.Stoic: h = 0.11f; lid = 0.3f; tilt = 0f; brow = 6f; thick = 0.026f; break;
                case MotionStyle.Confident: lid = 0.12f; tilt = 6f; brow = 12f; thick = 0.024f; break;
                case MotionStyle.Sly: h = 0.12f; lid = 0.28f; tilt = 8f; brow = 16f; break;
                case MotionStyle.Light: h = 0.14f; lid = 0f; tilt = -2f; brow = -10f; break;
            }
            PremiumEyes(hc, hr, 0.125f, -0.035f, w, h, iris, irisLow, lid, tilt, lashes, brow, thick, browC, skin);
            var nose = OnFace(hc, hr, 0f, -0.09f);
            Ball(nose, Vector3.zero, new Vector3(0.022f, 0.016f, 0.012f), PM(Color.Lerp(def.skinTone, new Color(0.8f, 0.5f, 0.45f), 0.4f), 0f));
            if (def.motion != MotionStyle.Stoic && def.motion != MotionStyle.Aggressive) Blush(hc, hr, def.motion == MotionStyle.Nervous ? 0.45f : 0.3f);
            var mouthM = PM(new Color(0.3f, 0.1f, 0.1f), 0f);
            var teeth = PM(new Color(0.99f, 0.98f, 0.96f), 0f);
            var tongue = PM(new Color(0.9f, 0.45f, 0.45f), 0f);
            var mo = OnFace(hc, hr, 0f, -0.17f);
            switch (def.motion)
            {
                case MotionStyle.Confident:
                    Ball(mo, new Vector3(0f, 0f, -0.004f), new Vector3(0.09f, 0.04f, 0.02f), mouthM);
                    Ball(mo, new Vector3(0f, 0.009f, 0.001f), new Vector3(0.076f, 0.016f, 0.016f), teeth);
                    break;
                case MotionStyle.Aggressive:
                    Ball(mo, new Vector3(0f, 0f, -0.004f), new Vector3(0.09f, 0.036f, 0.02f), mouthM);
                    Part(PrimitiveType.Cube, mo, new Vector3(0f, 0f, 0.004f), new Vector3(0.07f, 0.018f, 0.008f), teeth);
                    break;
                case MotionStyle.Nervous:
                    Ball(mo, Vector3.zero, new Vector3(0.036f, 0.045f, 0.02f), mouthM);
                    var sweat = OnFace(hc, hr, 0.26f, 0.12f);
                    Ball(sweat, new Vector3(0f, 0f, 0.01f), new Vector3(0.04f, 0.065f, 0.03f), PMe(new Color(0.7f, 0.88f, 1f), 0f, new Color(0.2f, 0.3f, 0.4f)));
                    break;
                case MotionStyle.Stoic:
                    Part(PrimitiveType.Capsule, mo, new Vector3(0f, 0f, 0.002f), new Vector3(0.014f, 0.04f, 0.012f), mouthM, new Vector3(0f, 0f, 90f));
                    break;
                case MotionStyle.Sly:
                    Part(PrimitiveType.Capsule, mo, new Vector3(-0.01f, 0f, 0.002f), new Vector3(0.014f, 0.035f, 0.012f), mouthM, new Vector3(0f, 0f, 90f));
                    Part(PrimitiveType.Capsule, mo, new Vector3(0.04f, 0.01f, 0.002f), new Vector3(0.013f, 0.022f, 0.012f), mouthM, new Vector3(0f, 0f, 40f));
                    break;
                case MotionStyle.Light:
                    Ball(mo, new Vector3(0f, -0.006f, -0.004f), new Vector3(0.075f, 0.06f, 0.02f), mouthM);
                    Ball(mo, new Vector3(0f, -0.022f, 0f), new Vector3(0.045f, 0.022f, 0.016f), tongue);
                    break;
                default:
                    Part(PrimitiveType.Capsule, mo, new Vector3(-0.02f, 0f, 0.002f), new Vector3(0.014f, 0.03f, 0.012f), mouthM, new Vector3(0f, 0f, 65f));
                    Part(PrimitiveType.Capsule, mo, new Vector3(0.02f, 0f, 0.002f), new Vector3(0.014f, 0.03f, 0.012f), mouthM, new Vector3(0f, 0f, -65f));
                    break;
            }
        }

        /// <summary>A long single-edged sword along +Z with a wrapped grip, a guard and a slightly curved blade.</summary>
        void LongBlade(Transform parent, Vector3 pos, Quaternion rot, float len, float width, Material steel, Material edge, Material guard, Material wrapA, Material wrapB, bool round)
        {
            var k = J("Blade", parent, pos);
            k.localRotation = rot;
            Part(PrimitiveType.Cube, k, new Vector3(0f, 0f, -0.03f), new Vector3(0.036f, 0.046f, 0.28f), wrapA);
            for (int i = 0; i < 5; i++)
                Part(PrimitiveType.Cube, k, new Vector3(0f, 0f, -0.14f + i * 0.055f), new Vector3(0.04f, 0.04f, 0.018f), wrapB, new Vector3(0f, 0f, 45f));
            Ball(k, new Vector3(0f, 0f, -0.18f), new Vector3(0.05f, 0.056f, 0.04f), guard);
            if (round)
            {
                var ts = MeshFactory.MeshObject(MeshFactory.FacetCylinder(12), k, new Vector3(0f, 0f, 0.12f), new Vector3(0.14f * width, 0.018f, 0.14f * width), guard);
                ts.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                Add(ts);
            }
            else Part(PrimitiveType.Cube, k, new Vector3(0f, 0f, 0.12f), new Vector3(0.1f * width, 0.12f * width, 0.016f), guard);
            Part(PrimitiveType.Cube, k, new Vector3(0f, 0f, 0.14f), new Vector3(0.032f, 0.058f * width, 0.026f), guard);
            float seg = len / 4f;
            for (int i = 0; i < 4; i++)
            {
                var b = J("Seg", k, new Vector3(0f, i * i * 0.004f, 0.155f + seg * (i + 0.5f)));
                b.localRotation = Quaternion.Euler(-1.5f - i * 1.8f, 0f, 0f);
                Part(PrimitiveType.Cube, b, Vector3.zero, new Vector3(0.018f, 0.06f * width, seg * 1.02f), steel);
                Part(PrimitiveType.Cube, b, new Vector3(0f, -0.028f * width, 0f), new Vector3(0.012f, 0.012f, seg * 1.02f), edge);
            }
            var tip = MeshFactory.MeshObject(MeshFactory.FacetCone(4), k, new Vector3(0f, 0.036f, 0.155f + len), new Vector3(0.018f, 0.1f * width, 0.06f * width), steel);
            tip.transform.localRotation = Quaternion.Euler(83f, 0f, 0f);
            Add(tip);
        }

        void RosterWeapon(CharacterDefinition def, PremiumRig r, Material trim, Material accent, Material dark, Material leather, Material metal, int rarity)
        {
            Color bc = def.bladeColor;
            var steel = PM(new Color(0.8f, 0.83f, 0.86f), 0.01f);
            var edge = PMe(Color.Lerp(new Color(0.9f, 0.92f, 0.95f), bc, 0.45f), 0f, bc * 0.3f);
            var wood = PM(new Color(0.42f, 0.28f, 0.18f), 0.01f);
            var sp = SwordPivot;
            switch (def.weapon)
            {
                case WeaponKind.TwinBlades:
                    r.grip = PremiumRig.Grip.Twin;
                    sp.localRotation = swordRest;
                    Kodachi(sp, Vector3.zero, Quaternion.identity, 0.6f, steel, edge, trim, dark, accent);
                    Kodachi(r.hand[1], new Vector3(-0.05f, -0.055f, 0f), Quaternion.Euler(-90f, 0f, 0f), 0.34f, steel, edge, trim, dark, accent);
                    PremiumTrail(0.66f, bc);
                    break;
                case WeaponKind.Greatsword:
                {
                    r.grip = PremiumRig.Grip.TwoHand;
                    r.handleOffset = -0.15f;
                    swordRest = Quaternion.Euler(-108f, 32f, 0f);
                    sp.localRotation = swordRest;
                    r.poseWeapon = swordRest.eulerAngles;
                    Part(PrimitiveType.Cube, sp, new Vector3(0f, 0f, -0.04f), new Vector3(0.05f, 0.05f, 0.4f), leather);
                    for (int i = 0; i < 5; i++) Part(PrimitiveType.Cube, sp, new Vector3(0f, 0f, -0.2f + i * 0.08f), new Vector3(0.056f, 0.056f, 0.02f), accent);
                    Ball(sp, new Vector3(0f, 0f, -0.26f), Vector3.one * 0.07f, trim);
                    Part(PrimitiveType.Cube, sp, new Vector3(0f, 0f, 0.18f), new Vector3(0.06f, 0.34f, 0.05f), trim);
                    Part(PrimitiveType.Cube, sp, new Vector3(0f, 0f, 0.22f), new Vector3(0.05f, 0.16f, 0.04f), dark);
                    Part(PrimitiveType.Cube, sp, new Vector3(0f, 0f, 0.86f), new Vector3(0.04f, 0.24f, 1.26f), steel);
                    Part(PrimitiveType.Cube, sp, new Vector3(0f, -0.115f, 0.86f), new Vector3(0.028f, 0.02f, 1.26f), edge);
                    Part(PrimitiveType.Cube, sp, new Vector3(0f, 0.115f, 0.86f), new Vector3(0.028f, 0.02f, 1.26f), edge);
                    Part(PrimitiveType.Cube, sp, new Vector3(0f, 0f, 0.8f), new Vector3(0.046f, 0.03f, 1f), dark);
                    var tip = MeshFactory.MeshObject(MeshFactory.FacetCone(4), sp, new Vector3(0f, 0f, 1.49f), new Vector3(0.04f, 0.16f, 0.24f), steel);
                    tip.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                    Add(tip);
                    PremiumTrail(1.4f, bc);
                    break;
                }
                case WeaponKind.SwordShield:
                {
                    r.grip = PremiumRig.Grip.OneHand;
                    sp.localRotation = swordRest;
                    LongBlade(sp, Vector3.zero, Quaternion.identity, 0.8f, 1.1f, steel, edge, trim, dark, accent, false);
                    var sh = J("Shield", r.lower[1], new Vector3(-0.07f, -0.12f, 0.02f));
                    sh.localRotation = Quaternion.Euler(0f, 0f, 90f) * Quaternion.Euler(0f, 0f, 0f);
                    var rim = PM(Color.Lerp(new Color(0.6f, 0.5f, 0.28f), metal.color, 0.3f), 0.01f);
                    var face = MeshFactory.MeshObject(MeshFactory.FacetCylinder(18), sh, Vector3.zero, new Vector3(0.62f, 0.05f, 0.62f), rim);
                    Add(face);
                    Add(MeshFactory.MeshObject(MeshFactory.FacetCylinder(18), sh, new Vector3(0f, 0.018f, 0f), new Vector3(0.54f, 0.05f, 0.54f), accent));
                    Ball(sh, new Vector3(0f, 0.05f, 0f), new Vector3(0.16f, 0.08f, 0.16f), metal);
                    for (int k = 0; k < 8; k++)
                    {
                        float a = k * 45f * Mathf.Deg2Rad;
                        Ball(sh, new Vector3(Mathf.Cos(a) * 0.27f, 0.035f, Mathf.Sin(a) * 0.27f), Vector3.one * 0.03f, trim);
                    }
                    PremiumTrail(0.85f, bc);
                    break;
                }
                case WeaponKind.Spear:
                {
                    r.grip = PremiumRig.Grip.TwoHand;
                    r.handleOffset = -0.34f;
                    swordRest = Quaternion.Euler(-65f, 25f, 0f);
                    sp.localRotation = swordRest;
                    r.poseWeapon = swordRest.eulerAngles;
                    Part(PrimitiveType.Cylinder, sp, new Vector3(0f, 0f, 0.25f), new Vector3(0.045f, 1.05f, 0.045f), wood, new Vector3(90f, 0f, 0f));
                    for (int i = 0; i < 4; i++) Part(PrimitiveType.Cylinder, sp, new Vector3(0f, 0f, -0.6f + i * 0.5f), new Vector3(0.052f, 0.012f, 0.052f), trim, new Vector3(90f, 0f, 0f));
                    Part(PrimitiveType.Cylinder, sp, new Vector3(0f, 0f, 1.3f), new Vector3(0.07f, 0.03f, 0.07f), trim, new Vector3(90f, 0f, 0f));
                    var head2 = MeshFactory.MeshObject(MeshFactory.FacetCone(4), sp, new Vector3(0f, 0f, 1.33f), new Vector3(0.03f, 0.42f, 0.13f), steel);
                    head2.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                    Add(head2);
                    Part(PrimitiveType.Cube, sp, new Vector3(0f, 0f, 1.5f), new Vector3(0.012f, 0.02f, 0.3f), edge);
                    var tas = J("Tassel", sp, new Vector3(0f, -0.03f, 1.26f));
                    ConePart(tas, new Vector3(0f, -0.08f, 0f), new Vector3(0.08f, 0.16f, 0.08f), accent, new Vector3(180f, 0f, 0f));
                    tas.gameObject.AddComponent<Sway>().Amount = 12f;
                    var butt = MeshFactory.MeshObject(MeshFactory.FacetCone(6), sp, new Vector3(0f, 0f, -0.8f), new Vector3(0.05f, 0.1f, 0.05f), trim);
                    butt.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
                    Add(butt);
                    PremiumTrail(1.6f, bc);
                    break;
                }
                case WeaponKind.Staff:
                {
                    r.grip = PremiumRig.Grip.Caster;
                    r.restHandL = new Vector3(-r.shoulder.x - 0.02f, 0.3f, 0.3f);
                    swordRest = Quaternion.Euler(-78f, 12f, 0f);
                    sp.localRotation = swordRest;
                    r.poseWeapon = swordRest.eulerAngles;
                    Part(PrimitiveType.Cylinder, sp, new Vector3(0f, 0f, 0.35f), new Vector3(0.042f, 0.9f, 0.042f), wood, new Vector3(90f, 0f, 0f));
                    float[] bands = { -0.45f, 0.12f, 0.62f, 1.18f };
                    foreach (float z in bands) Part(PrimitiveType.Cylinder, sp, new Vector3(0f, 0f, z), new Vector3(0.05f, 0.012f, 0.05f), trim, new Vector3(90f, 0f, 0f));
                    Vector3 oc = new Vector3(0f, 0f, 1.42f);
                    var ring = J("StaffRing", sp, oc);
                    ring.localRotation = Quaternion.Euler(0f, 90f, 0f);
                    for (int k = 0; k < 14; k++)
                    {
                        float a = k * Mathf.PI * 2f / 14f;
                        Ball(ring, new Vector3(Mathf.Cos(a) * 0.17f, 0f, Mathf.Sin(a) * 0.17f), Vector3.one * 0.05f, trim);
                    }
                    Ball(sp, oc, Vector3.one * 0.14f, PMe(Color.Lerp(bc, Color.white, 0.2f), 0f, bc * 0.8f));
                    MeshFactory.MeshObject(MeshFactory.SmoothSphere(), sp, oc, Vector3.one * 0.26f, MaterialFactory.Additive(new Color(bc.r, bc.g, bc.b, 0.3f)), false);
                    var glowGo = MeshFactory.MeshObject(MeshFactory.SmoothSphere(), r.hand[1], new Vector3(0f, -0.1f, 0.06f), Vector3.one * 0.1f, MaterialFactory.Additive(new Color(bc.r, bc.g, bc.b, 0.5f)), false);
                    r.castGlow = glowGo.transform;
                    PremiumTrail(1.42f, bc);
                    break;
                }
                case WeaponKind.Bow:
                {
                    r.grip = PremiumRig.Grip.Caster;
                    r.restHandL = new Vector3(-r.shoulder.x - 0.1f, 0.12f, 0.2f);
                    sp.localRotation = swordRest;
                    var limb = PM(new Color(0.42f, 0.27f, 0.15f), 0.012f);
                    var bow = J("Bow", r.hand[1], new Vector3(0f, -0.06f, 0.02f));
                    bow.localRotation = Quaternion.Euler(-90f, 0f, 0f);
                    for (int i = 0; i < 13; i++)
                    {
                        float a = Mathf.Lerp(-70f, 70f, i / 12f) * Mathf.Deg2Rad;
                        float tt = Mathf.Abs(i - 6f) / 6f;
                        Ball(bow, new Vector3(0f, Mathf.Sin(a) * 0.6f, Mathf.Cos(a) * 0.2f - 0.2f), new Vector3(0.045f, 0.11f, 0.05f) * (1.2f - tt * 0.4f), i == 6 ? leather : limb);
                    }
                    Part(PrimitiveType.Cube, bow, new Vector3(0f, 0f, -0.14f), new Vector3(0.006f, 1.12f, 0.006f), PM(new Color(0.95f, 0.93f, 0.85f), 0f));
                    Ball(bow, new Vector3(0f, 0.56f, -0.08f), Vector3.one * 0.05f, trim);
                    Ball(bow, new Vector3(0f, -0.56f, -0.08f), Vector3.one * 0.05f, trim);
                    // Quiver on the back with fletched arrows.
                    var q = J("Quiver", r.torso, new Vector3(0.1f, 0.2f, -0.2f));
                    q.localRotation = Quaternion.Euler(-12f, 0f, -22f);
                    Part(PrimitiveType.Cylinder, q, Vector3.zero, new Vector3(0.12f, 0.22f, 0.12f), leather);
                    Band(q, new Vector3(0f, 0.18f, 0f), 0.062f, 0.03f, 0.008f, trim);
                    for (int i = 0; i < 4; i++)
                    {
                        Part(PrimitiveType.Cylinder, q, new Vector3((i - 1.5f) * 0.025f, 0.3f, (i % 2) * 0.02f), new Vector3(0.012f, 0.1f, 0.012f), wood);
                        Part(PrimitiveType.Cube, q, new Vector3((i - 1.5f) * 0.025f, 0.4f, (i % 2) * 0.02f), new Vector3(0.004f, 0.06f, 0.03f), accent);
                    }
                    PremiumTrail(0.2f, bc);
                    break;
                }
                case WeaponKind.Fans:
                {
                    r.grip = PremiumRig.Grip.Twin;
                    r.twinLeft = Quaternion.Euler(-60f, 0f, 0f);
                    r.twinLeftPos = new Vector3(0f, -0.06f, 0f);
                    sp.localRotation = swordRest;
                    var paper = PM(Color.Lerp(bc, new Color(0.96f, 0.94f, 0.9f), 0.55f), 0.008f);
                    for (int side = 0; side < 2; side++)
                    {
                        Transform parent = side == 0 ? sp : J("LeftFan", r.hand[1], new Vector3(0f, -0.06f, 0f));
                        if (side == 1) parent.localRotation = Quaternion.Euler(-60f, 0f, 0f);
                        for (int i = 0; i < 9; i++)
                        {
                            float a = Mathf.Lerp(-60f, 60f, i / 8f);
                            var rib = J("Rib", parent, Vector3.zero);
                            rib.localRotation = Quaternion.Euler(a, 0f, 0f);
                            Part(PrimitiveType.Cube, rib, new Vector3(0f, 0f, 0.22f), new Vector3(0.008f, 0.07f, 0.32f), i % 2 == 0 ? paper : accent);
                            Part(PrimitiveType.Cube, rib, new Vector3(0.006f, 0f, 0.1f), new Vector3(0.006f, 0.012f, 0.2f), dark);
                        }
                        Ball(parent, Vector3.zero, Vector3.one * 0.04f, trim);
                    }
                    PremiumTrail(0.35f, bc);
                    break;
                }
                case WeaponKind.Cleavers:
                {
                    r.grip = PremiumRig.Grip.Twin;
                    r.twinLeft = Quaternion.Euler(80f, 0f, 0f);
                    r.twinLeftPos = new Vector3(0f, -0.055f, 0f);
                    sp.localRotation = swordRest;
                    for (int side = 0; side < 2; side++)
                    {
                        Transform parent = side == 0 ? sp : J("LeftCleaver", r.hand[1], new Vector3(0f, -0.055f, 0f));
                        if (side == 1) parent.localRotation = Quaternion.Euler(80f, 0f, 0f);
                        Part(PrimitiveType.Cube, parent, new Vector3(0f, 0f, 0.02f), new Vector3(0.04f, 0.04f, 0.22f), wood);
                        Part(PrimitiveType.Cube, parent, new Vector3(0f, -0.08f, 0.38f), new Vector3(0.028f, 0.24f, 0.46f), steel);
                        Part(PrimitiveType.Cube, parent, new Vector3(0f, -0.2f, 0.38f), new Vector3(0.018f, 0.02f, 0.46f), edge);
                        Ball(parent, new Vector3(0f, 0.01f, 0.52f), Vector3.one * 0.05f, trim);
                    }
                    PremiumTrail(0.55f, bc);
                    break;
                }
                case WeaponKind.Cane:
                    r.grip = PremiumRig.Grip.OneHand;
                    swordRest = Quaternion.Euler(75f, 20f, 0f);
                    sp.localRotation = swordRest;
                    r.poseWeapon = swordRest.eulerAngles;
                    Part(PrimitiveType.Cylinder, sp, new Vector3(0f, 0f, 0.4f), new Vector3(0.035f, 0.5f, 0.035f), wood, new Vector3(90f, 0f, 0f));
                    Ball(sp, new Vector3(0f, 0.02f, -0.08f), new Vector3(0.07f, 0.06f, 0.1f), trim);
                    PremiumTrail(0.8f, bc);
                    break;
                case WeaponKind.Moon:
                {
                    r.grip = PremiumRig.Grip.OneHand;
                    sp.localRotation = swordRest;
                    Part(PrimitiveType.Cube, sp, new Vector3(0f, 0f, 0.02f), new Vector3(0.04f, 0.04f, 0.3f), dark);
                    Ball(sp, new Vector3(0f, 0f, -0.14f), Vector3.one * 0.05f, trim);
                    // The crescent: a curved row of blade segments.
                    for (int k = 0; k < 11; k++)
                    {
                        float a = Mathf.Lerp(-80f, 80f, k / 10f) * Mathf.Deg2Rad;
                        Vector3 p = new Vector3(0f, Mathf.Sin(a) * 0.36f, 0.5f + Mathf.Cos(a) * 0.36f - 0.36f + 0.2f);
                        var seg = J("Moon", sp, p);
                        seg.localRotation = Quaternion.Euler(-a * Mathf.Rad2Deg, 0f, 0f);
                        float wdt = Mathf.Lerp(0.1f, 0.03f, Mathf.Abs(k - 5f) / 5f);
                        Part(PrimitiveType.Cube, seg, Vector3.zero, new Vector3(0.018f, 0.075f, wdt), steel);
                        Part(PrimitiveType.Cube, seg, new Vector3(0f, 0f, wdt * 0.5f), new Vector3(0.012f, 0.075f, 0.014f), edge);
                    }
                    PremiumTrail(0.62f, bc);
                    break;
                }
                case WeaponKind.Fists:
                    r.grip = PremiumRig.Grip.Twin;
                    sp.localRotation = swordRest;
                    for (int i = 0; i < 2; i++)
                    {
                        var g = J("Gauntlet", r.hand[i], Vector3.zero);
                        Taper(g, new Vector3(0f, 0.08f, 0f), 0.1f, 0.052f, 0.048f, metal);
                        Part(PrimitiveType.Cube, g, new Vector3(0f, -0.09f, 0.035f), new Vector3(0.1f, 0.05f, 0.04f), metal);
                        for (int k = 0; k < 4; k++) Ball(g, new Vector3(-0.03f + k * 0.02f, -0.1f, 0.056f), Vector3.one * 0.02f, trim);
                    }
                    PremiumTrail(0.05f, bc);
                    break;
                default:
                    // Katana: long curved blade, round guard, two-handed on the swing.
                    r.grip = PremiumRig.Grip.TwoHand;
                    r.handleOffset = -0.13f;
                    sp.localRotation = swordRest;
                    LongBlade(sp, Vector3.zero, Quaternion.identity, 0.95f, 1f, steel, edge, trim, dark, accent, true);
                    // A scabbard at the hip.
                    var sc = J("Scabbard", r.torso, new Vector3(-0.14f, 0.0f, 0.04f));
                    sc.localRotation = Quaternion.Euler(118f, -20f, 0f);
                    Part(PrimitiveType.Cube, sc, new Vector3(0f, 0f, 0.35f), new Vector3(0.04f, 0.07f, 0.9f), dark);
                    Part(PrimitiveType.Cube, sc, new Vector3(0f, 0f, 0.02f), new Vector3(0.05f, 0.08f, 0.06f), trim);
                    Part(PrimitiveType.Cube, sc, new Vector3(0f, 0f, 0.78f), new Vector3(0.05f, 0.08f, 0.05f), trim);
                    PremiumTrail(1.02f, bc);
                    break;
            }
        }
    }
}
