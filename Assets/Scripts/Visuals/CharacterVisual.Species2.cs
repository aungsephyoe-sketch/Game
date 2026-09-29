using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// More monster-folk, and the "menace" pass every monster gets (cartoon-scary, never gory):
    ///   • glowing eyes, angry brows, a hunched, glaring stance and a slow ring of dark smoke around them.
    /// Kinds (features / weapon):
    ///   Vampire — pale, pointed ears, fangs, bat wings / a slim blade.      Zombie — stitches, a drooping eye, grasping claws.
    ///   Ghost — no legs (a floating wisp), a moaning mouth, will-o'-wisps / lantern staff.
    ///   Lizardfolk — snout full of teeth, head crest, back spines, tail / spear.
    ///   Minotaur — bull muzzle, nose ring, great horns / double axe.        Orc — tusks, heavy brow / cleavers.
    ///   Troll — huge nose, droopy ears, moss / tree club.                   Harpy — feathered arm-wings, talons / feather fans.
    ///   Kitsune — fox ears, five tails, orbiting foxfire / fans.            Gargoyle — stone skin, horns, bat wings / stone claws.
    ///   Kappa — beak, water dish on the head, shell / webbed fists.         Tengu — long red nose, black wings, cap / feather fans.
    ///   Yeti — shaggy fur ring, big fangs / claws.                          Reaper — skull under a hood, floats / scythe.
    ///   Golem — rock plates, glowing rune eyes / boulder fists.             Pumpkin Knight — carved glowing pumpkin head / greatsword.
    /// </summary>
    public partial class CharacterVisual
    {
        void MoreSpecies(CharacterDefinition def, Material skin, Material dark, Material ivory, Material glow, Color glowC, float R)
        {
            var T = rig.torso;
            var skinD = PM(Color.Lerp(def.skinTone, Color.black, 0.25f));
            switch (def.species)
            {
                case Species.Vampire:
                    Fangs(ivory, 0.035f);
                    PointedEars(skin, R, 0.38f);
                    Wings(true, PM(new Color(0.16f, 0.08f, 0.16f), 0.01f), PM(new Color(0.42f, 0.08f, 0.16f), 0.008f), 0.95f);
                    break;
                case Species.Zombie:
                {
                    var stitch = PM(new Color(0.15f, 0.1f, 0.12f), 0f);
                    var st = OnFace(faceC, faceR, -0.09f, 0.22f, 0.004f);
                    Part(PrimitiveType.Capsule, st, Vector3.zero, new Vector3(0.012f, 0.1f, 0.01f), stitch, new Vector3(0f, 0f, 70f));
                    for (int k = -2; k <= 2; k++) Part(PrimitiveType.Capsule, st, new Vector3(k * 0.03f, k * 0.011f, 0.003f), new Vector3(0.008f, 0.022f, 0.008f), stitch, new Vector3(0f, 0f, -20f));
                    Ball(head, OnHead(-40f, -25f, 0.97f), new Vector3(R * 0.35f, R * 0.25f, R * 0.12f), PM(Color.Lerp(def.skinTone, new Color(0.4f, 0.5f, 0.3f), 0.5f)));
                    foreach (var l in head.GetComponentsInChildren<EyeLid>(true))
                        if (l.transform.localPosition.x < 0f) l.transform.localScale = Vector3.Scale(l.transform.localScale, new Vector3(1f, 0.55f, 1f));
                    Claws(PM(new Color(0.7f, 0.68f, 0.55f), 0.008f));
                    // Arms held out, reaching.
                    rig.restHandR += new Vector3(-0.05f, 0.22f, 0.3f);
                    rig.restHandL += new Vector3(0.05f, 0.2f, 0.3f);
                    break;
                }
                case Species.Ghost:
                {
                    var wisp = MaterialFactory.Transparent(new Color(0.78f, 0.9f, 1f, 0.55f));
                    Floating(wisp);
                    HideMouth();
                    Ball(OnFace(faceC, faceR, 0f, -0.2f, 0.004f), Vector3.zero, new Vector3(0.07f, 0.1f, 0.02f), dark);
                    Wisps(glow, 3, 0.55f);
                    break;
                }
                case Species.Lizardfolk:
                {
                    HideMouth();
                    var sn = faceC + new Vector3(0f, -faceR.y * 0.3f, faceR.z * 0.62f);
                    Ball(head, sn, new Vector3(R * 0.62f, R * 0.4f, R * 1.0f), skin);
                    for (int s = -1; s <= 1; s += 2)
                    {
                        Ball(head, sn + new Vector3(s * R * 0.1f, R * 0.12f, R * 0.46f), Vector3.one * R * 0.06f, dark);
                        for (int k = 0; k < 3; k++) Spike(head, sn + new Vector3(s * R * 0.24f, -R * 0.12f, R * (0.1f + k * 0.14f)), Vector3.down, R * 0.05f, R * 0.09f, ivory);
                    }
                    var crest = PM(Color.Lerp(def.bladeColor, Color.black, 0.2f), 0.008f);
                    for (int k = 0; k < 4; k++) Spike(head, OnHead(180f, 85f - k * 22f, 0.95f), new Vector3(0f, 1f, -0.5f), R * 0.2f, R * (0.45f - k * 0.06f), crest, 0.25f);
                    for (int k = 0; k < 4; k++) Spike(T, new Vector3(0f, 0.28f - k * 0.08f, -0.13f), new Vector3(0f, 0.4f, -1f), 0.07f, 0.12f, crest, 0.3f);
                    TaperTail(skin, 0.07f, 6);
                    break;
                }
                case Species.Minotaur:
                {
                    HideMouth();
                    var mz = faceC + new Vector3(0f, -faceR.y * 0.35f, faceR.z * 0.62f);
                    Ball(head, mz, new Vector3(R * 0.95f, R * 0.6f, R * 0.7f), PM(Color.Lerp(def.skinTone, Color.white, 0.25f)));
                    for (int s = -1; s <= 1; s += 2) Ball(head, mz + new Vector3(s * R * 0.2f, 0f, R * 0.34f), Vector3.one * R * 0.1f, dark);
                    Band(head, mz + new Vector3(0f, -R * 0.1f, R * 0.36f), R * 0.13f, 0.02f, 0.012f, PM(new Color(1f, 0.8f, 0.3f), 0.008f), null, new Vector3(0f, 0f, 90f));
                    var horn = PM(new Color(0.92f, 0.88f, 0.76f), 0.01f);
                    for (int s = -1; s <= 1; s += 2)
                    {
                        var h1 = Spike(head, OnHead(s * 72f, 42f, 0.9f), new Vector3(s, 0.2f, 0.25f), R * 0.34f, R * 0.7f, horn);
                        Spike(h1, new Vector3(0f, R * 0.6f, 0f), new Vector3(s * 0.2f, 0.4f, 0.8f), R * 0.2f, R * 0.5f, horn);
                        Spike(head, OnHead(s * 92f, 15f, 0.92f), new Vector3(s, 0f, -0.3f), R * 0.3f, R * 0.35f, skin, 0.3f);
                    }
                    DoubleAxe();
                    break;
                }
                case Species.Orc:
                    for (int s = -1; s <= 1; s += 2) Spike(OnFace(faceC, faceR, s * 0.08f, -0.25f, 0.006f), Vector3.zero, new Vector3(s * 0.2f, 1f, 0.2f), 0.035f, 0.08f, ivory);
                    Ball(OnFace(faceC, faceR, 0f, 0.14f, 0.004f), Vector3.zero, new Vector3(0.4f, 0.07f, 0.06f), skinD);
                    PointedEars(skin, R, 0.3f);
                    break;
                case Species.Troll:
                    Ball(OnFace(faceC, faceR, 0f, -0.06f, 0.01f), Vector3.zero, new Vector3(0.16f, 0.14f, 0.16f), skinD);
                    for (int s = -1; s <= 1; s += 2)
                    {
                        Spike(head, OnHead(s * 90f, 5f, 0.92f), new Vector3(s, -0.4f, -0.2f), R * 0.34f, R * 0.7f, skin, 0.3f, 3f);
                        Spike(OnFace(faceC, faceR, s * 0.07f, -0.24f, 0.006f), Vector3.zero, Vector3.up, 0.03f, 0.05f, ivory);
                    }
                    var moss = PM(new Color(0.3f, 0.5f, 0.22f), 0.008f);
                    for (int k = 0; k < 4; k++) Ball(head, OnHead(k * 90f + 30f, 60f, 0.95f), Vector3.one * R * 0.35f, moss);
                    for (int i = 0; i < 2; i++) Ball(rig.upper[i], new Vector3(0f, 0.03f, 0f), Vector3.one * 0.14f, moss);
                    Club(def);
                    break;
                case Species.Harpy:
                {
                    var f1 = PM(Color.Lerp(def.hairColor, Color.white, 0.1f), 0.008f);
                    var f2 = PM(Color.Lerp(def.bladeColor, def.hairColor, 0.5f), 0.008f);
                    for (int i = 0; i < 2; i++)
                    {
                        int side = i == 0 ? 1 : -1;
                        for (int k = 0; k < 4; k++)
                        {
                            Spike(rig.upper[i], new Vector3(side * 0.03f, -0.03f - k * 0.045f, -0.03f), new Vector3(side * 0.5f, -1f, -0.5f), 0.08f, 0.26f + k * 0.03f, k % 2 == 0 ? f1 : f2, 0.25f, 3f);
                            Spike(rig.lower[i], new Vector3(side * 0.03f, -0.03f - k * 0.045f, -0.03f), new Vector3(side * 0.3f, -1f, -0.6f), 0.07f, 0.3f + k * 0.03f, k % 2 == 0 ? f2 : f1, 0.25f, 3f);
                        }
                        for (int k = -1; k <= 1; k++) Spike(rig.foot[i], new Vector3(k * 0.035f, -0.05f, 0.14f), new Vector3(k * 0.3f, -0.3f, 1f), 0.03f, 0.08f, dark);
                    }
                    for (int k = 0; k < 3; k++) Spike(head, OnHead(180f + (k - 1) * 25f, 50f, 0.95f), new Vector3((k - 1) * 0.3f, 0.8f, -1f), R * 0.2f, R * 0.7f, f2, 0.3f, 3f);
                    break;
                }
                case Species.Kitsune:
                {
                    var fur = PM(def.hairColor, 0.01f);
                    var tip = PM(new Color(0.98f, 0.96f, 0.92f), 0.008f);
                    for (int s = -1; s <= 1; s += 2)
                    {
                        var ear = Spike(head, OnHead(s * 36f, 66f, 0.9f), new Vector3(s * 0.3f, 1f, -0.1f), R * 0.46f, R * 0.72f, fur, 0.45f, 3f);
                        Spike(ear, new Vector3(0f, R * 0.5f, 0f), Vector3.up, R * 0.2f, R * 0.22f, tip, 0.45f);
                    }
                    for (int t = 0; t < 5; t++)
                    {
                        var root = J("FoxTail", rig.pelvis, new Vector3(0f, 0f, -0.15f));
                        root.localRotation = Quaternion.Euler(-40f - Mathf.Abs(t - 2) * 6f, (t - 2) * 28f, 0f);
                        Transform prev = root;
                        for (int k = 0; k < 3; k++)
                        {
                            Ball(prev, new Vector3(0f, -0.08f, 0f), new Vector3(0.12f + k * 0.04f, 0.2f, 0.12f + k * 0.04f), fur);
                            var sw = prev.gameObject.AddComponent<Sway>();
                            sw.Amount = 7f + k * 3f; sw.Speed = 1.1f + t * 0.1f;
                            var next = J("T", prev, new Vector3(0f, -0.15f, 0f));
                            next.localRotation = Quaternion.Euler(18f, 0f, 0f);
                            prev = next;
                        }
                        Ball(prev, Vector3.zero, new Vector3(0.18f, 0.2f, 0.18f), tip);
                    }
                    Wisps(glow, 3, 0.7f);
                    break;
                }
                case Species.Gargoyle:
                {
                    var stone = PM(Color.Lerp(def.skinTone, Color.black, 0.35f), 0.012f);
                    for (int s = -1; s <= 1; s += 2)
                    {
                        var h1 = Spike(head, OnHead(s * 40f, 60f, 0.9f), new Vector3(s * 0.4f, 0.8f, -0.6f), R * 0.26f, R * 0.5f, stone);
                        Spike(h1, new Vector3(0f, R * 0.4f, 0f), new Vector3(0f, -0.2f, -1f), R * 0.16f, R * 0.3f, stone);
                    }
                    Ball(OnFace(faceC, faceR, 0f, 0.14f, 0.004f), Vector3.zero, new Vector3(0.38f, 0.07f, 0.06f), stone);
                    Wings(true, stone, PM(Color.Lerp(def.skinTone, Color.black, 0.15f), 0.01f), 0.85f);
                    Claws(stone);
                    break;
                }
                case Species.Kappa:
                {
                    HideMouth();
                    Spike(head, faceC + new Vector3(0f, -faceR.y * 0.25f, faceR.z * 0.82f), new Vector3(0f, -0.35f, 1f), R * 0.42f, R * 0.36f, PM(new Color(0.95f, 0.78f, 0.25f), 0.01f), 0.55f);
                    var dish = OnHead(0f, 88f, 0.98f);
                    Part(PrimitiveType.Cylinder, head, dish, new Vector3(R * 1.15f, 0.02f, R * 1.15f), PM(new Color(0.92f, 0.9f, 0.82f), 0.01f));
                    Part(PrimitiveType.Cylinder, head, dish + Vector3.up * 0.015f, new Vector3(R * 0.9f, 0.012f, R * 0.9f), PMe(new Color(0.45f, 0.75f, 1f), 0f, new Color(0.1f, 0.25f, 0.4f)));
                    var shell = PM(new Color(0.34f, 0.44f, 0.24f), 0.014f);
                    Ball(T, new Vector3(0f, 0.15f, -0.17f), new Vector3(0.44f, 0.52f, 0.24f), shell);
                    for (int k = -1; k <= 1; k++) Ball(T, new Vector3(k * 0.1f, 0.15f, -0.28f), new Vector3(0.1f, 0.3f, 0.05f), PM(new Color(0.46f, 0.56f, 0.3f), 0.01f));
                    break;
                }
                case Species.Tengu:
                {
                    Spike(head, faceC + new Vector3(0f, -faceR.y * 0.08f, faceR.z * 0.88f), new Vector3(0f, 0.1f, 1f), R * 0.2f, R * 0.9f, skin);
                    Wings(false, PM(new Color(0.08f, 0.08f, 0.1f), 0.01f), PM(new Color(0.18f, 0.18f, 0.22f), 0.01f), 1f);
                    Part(PrimitiveType.Cube, head, OnHead(0f, 55f, 0.98f), new Vector3(0.09f, 0.08f, 0.09f), dark, new Vector3(-30f, 0f, 45f));
                    break;
                }
                case Species.Yeti:
                {
                    var fur = PM(new Color(0.94f, 0.96f, 1f), 0.012f);
                    HideMouth();
                    var mo = OnFace(faceC, faceR, 0f, -0.2f, 0.004f);
                    Ball(mo, Vector3.zero, new Vector3(0.14f, 0.07f, 0.02f), dark);
                    for (int s = -1; s <= 1; s += 2) Spike(mo, new Vector3(s * 0.045f, 0.03f, 0.008f), Vector3.down, 0.03f, 0.06f, ivory);
                    for (int k = 0; k < 10; k++)
                    {
                        float a = k * 36f;
                        Vector3 p = OnHead(Mathf.Sin(a * Mathf.Deg2Rad) * 70f, Mathf.Cos(a * Mathf.Deg2Rad) * 55f, 0.95f);
                        Spike(head, p, HeadNormal(p) + Vector3.back * 0.3f, R * 0.35f, R * 0.4f, fur, 0.5f);
                    }
                    for (int i = 0; i < 2; i++) Ball(rig.upper[i], new Vector3(0f, -0.02f, 0f), Vector3.one * 0.2f, fur);
                    Claws(ivory);
                    break;
                }
                case Species.Reaper:
                {
                    SkullFace(dark, glow, ivory);
                    Floating(PM(new Color(0.08f, 0.06f, 0.1f), 0.01f));
                    Scythe();
                    Wisps(glow, 2, 0.6f);
                    break;
                }
                case Species.Golem:
                {
                    HideFace(true);
                    RemoveHair();
                    var rock = PM(Color.Lerp(def.skinTone, Color.black, 0.2f), 0.014f);
                    for (int s = -1; s <= 1; s += 2) Part(PrimitiveType.Capsule, OnFace(faceC, faceR, s * 0.13f, 0f, 0.006f), Vector3.zero, new Vector3(0.03f, 0.06f, 0.02f), glow, new Vector3(0f, 0f, 90f + s * 12f));
                    Part(PrimitiveType.Cube, head, OnHead(0f, 70f, 0.9f), new Vector3(R * 1.3f, R * 0.5f, R * 1.2f), rock, new Vector3(8f, 20f, 5f));
                    for (int i = 0; i < 2; i++)
                    {
                        Part(PrimitiveType.Cube, rig.upper[i], new Vector3(0f, 0.02f, 0f), new Vector3(0.2f, 0.14f, 0.2f), rock, new Vector3(10f, 30f, 12f));
                        var g = FindPart(rig.hand[i], "Gauntlet");
                        if (g != null) Object.DestroyImmediate(g.gameObject);
                        Ball(rig.hand[i], new Vector3(0f, -0.08f, 0f), Vector3.one * 0.24f, rock);
                    }
                    Part(PrimitiveType.Cube, T, new Vector3(0f, 0.18f, 0.16f), new Vector3(0.07f, 0.07f, 0.02f), glow, new Vector3(0f, 0f, 45f));
                    break;
                }
                case Species.PumpkinKnight:
                {
                    HideFace(true);
                    RemoveHair();
                    var orange = PM(new Color(1f, 0.5f, 0.1f), 0.014f);
                    var orangeD = PM(new Color(0.9f, 0.4f, 0.08f), 0.014f);
                    Ball(head, faceC, faceR * 2.3f, orange);
                    for (int k = 0; k < 6; k++)
                        Ball(head, faceC, new Vector3(faceR.x * 0.9f, faceR.y * 2.28f, faceR.z * 2.36f), orangeD, new Vector3(0f, k * 30f, 0f));
                    Part(PrimitiveType.Cylinder, head, faceC + Vector3.up * faceR.y * 1.15f, new Vector3(0.07f, 0.07f, 0.07f), PM(new Color(0.3f, 0.45f, 0.18f), 0.01f), new Vector3(0f, 0f, 15f));
                    var fire = PMe(new Color(1f, 0.85f, 0.3f), 0f, new Color(1f, 0.6f, 0.1f));
                    for (int s = -1; s <= 1; s += 2) Spike(OnFace(faceC, faceR, s * 0.15f, 0.06f, 0.06f), Vector3.zero, Vector3.up, 0.13f, 0.12f, fire, 0.2f);
                    var mouth = OnFace(faceC, faceR, 0f, -0.22f, 0.06f);
                    for (int k = 0; k < 5; k++) Spike(mouth, new Vector3(-0.1f + k * 0.05f, 0f, 0f), k % 2 == 0 ? Vector3.up : Vector3.down, 0.06f, 0.06f, fire, 0.2f);
                    break;
                }
            }
        }

        // ------------------------------------------------------------------ Menace (every monster)

        /// <summary>Cartoon-scary finish: glowing eyes, angry brows, a hunched glare and a ring of dark smoke.</summary>
        void Menace(CharacterDefinition def, float R)
        {
            if (SpeciesKind == Species.Human) return;
            Color ec = def.species == Species.Ghost || def.species == Species.Kitsune || def.species == Species.Yeti ? new Color(0.5f, 0.85f, 1f)
                : def.species == Species.Zombie || def.species == Species.Goblin || def.species == Species.Orc ? new Color(0.85f, 1f, 0.3f)
                : new Color(1f, 0.22f, 0.15f);
            var eg = PMe(ec, 0f, ec);
            foreach (var l in head.GetComponentsInChildren<EyeLid>(true))
            {
                bool visible = false;
                foreach (var r in l.GetComponentsInChildren<Renderer>(true)) if (r.enabled) { visible = true; break; }
                if (!visible) continue;
                Ball(l.transform, new Vector3(0f, -0.005f, 0.015f), new Vector3(0.05f, 0.06f, 0.02f), eg);
            }
            foreach (Transform f in head)
            {
                if (f.name != "Face" || Mathf.Abs(f.localPosition.x) < 0.04f || f.localPosition.y < faceC.y + 0.03f) continue;
                if (f.GetComponentInChildren<EyeLid>() != null) continue;
                float side = Mathf.Sign(f.localPosition.x);
                f.localRotation = f.localRotation * Quaternion.Euler(0f, 0f, -side * 16f);
            }
            rig.lean += 6f;
            rig.crouch += 0.015f;
            rig.poseHead += new Vector3(7f, 0f, 0f);
            rig.poseBody += new Vector3(4f, 0f, 0f);
            // A slow ring of dark smoke around the legs.
            var smoke = MaterialFactory.Transparent(new Color(0.14f, 0.05f, 0.18f, 0.32f));
            var ring = J("MenaceSmoke", rig.body, new Vector3(0f, 0.25f, 0f));
            var sp = ring.gameObject.AddComponent<Spinner>();
            sp.DegreesPerSecond = new Vector3(0f, 30f, 0f);
            sp.BobHeight = 0.05f;
            for (int k = 0; k < 6; k++)
            {
                float a = k * Mathf.PI * 2f / 6f;
                MeshFactory.MeshObject(MeshFactory.SmoothSphere(), ring, new Vector3(Mathf.Cos(a) * 0.5f, (k % 2) * 0.12f, Mathf.Sin(a) * 0.5f), Vector3.one * (0.2f + (k % 3) * 0.06f), smoke, false);
            }
        }

        // ------------------------------------------------------------------ Parts

        void PointedEars(Material skin, float R, float len)
        {
            for (int s = -1; s <= 1; s += 2)
                Spike(head, OnHead(s * 88f, 10f, 0.92f), new Vector3(s, 0.35f, -0.2f), R * 0.26f, R * len, skin, 0.35f);
        }

        /// <summary>Wings on the back: bat (finger bones and a membrane) or feathered.</summary>
        void Wings(bool bat, Material bone, Material skinM, float size)
        {
            for (int s = -1; s <= 1; s += 2)
            {
                var root = J("Wing", rig.torso, new Vector3(s * 0.08f, 0.25f, -0.13f));
                root.localRotation = Quaternion.Euler(-15f, s * 35f, s * -30f);
                var sw = root.gameObject.AddComponent<Sway>();
                sw.Amount = 6f; sw.Speed = 1.8f;
                if (bat)
                {
                    for (int k = 0; k < 3; k++)
                        Spike(root, Vector3.zero, new Vector3(s * (0.6f + k * 0.4f), 1f - k * 0.5f, -0.2f), 0.04f * size, (0.55f - k * 0.08f) * size, bone);
                    Ball(root, new Vector3(s * 0.22f * size, 0.12f * size, -0.05f), new Vector3(0.5f * size, 0.42f * size, 0.02f), skinM, new Vector3(0f, 0f, s * -20f));
                }
                else
                    for (int k = 0; k < 6; k++)
                        Spike(root, new Vector3(s * k * 0.03f, -k * 0.02f, 0f), new Vector3(s * (0.3f + k * 0.25f), 1f - k * 0.2f, -0.2f), 0.09f * size, (0.5f - k * 0.03f) * size, k % 2 == 0 ? bone : skinM, 0.25f);
            }
        }

        /// <summary>No legs: the lower body becomes a floating wisp (ghosts, reapers).</summary>
        void Floating(Material wisp)
        {
            for (int i = 0; i < 2; i++)
                foreach (var t in new[] { rig.thigh[i], rig.shin[i], rig.foot[i] })
                    if (t != null) foreach (var r in t.GetComponentsInChildren<Renderer>(true)) r.enabled = false;
            var w = J("Wisp", rig.pelvis, new Vector3(0f, -0.05f, 0f));
            Ball(w, Vector3.zero, new Vector3(0.44f, 0.3f, 0.38f), wisp);
            Spike(w, new Vector3(0f, -0.05f, 0f), new Vector3(0f, -1f, -0.3f), 0.42f, 0.6f, wisp, 1f, 5f);
            rig.hover = Mathf.Max(rig.hover, 0.25f);
            rig.stride = 0.04f;
            rig.lift = 0.02f;
        }

        void Wisps(Material glow, int n, float radius)
        {
            var orbit = J("Wisps", rig.body, new Vector3(0f, 1f, 0f));
            var sp = orbit.gameObject.AddComponent<Spinner>();
            sp.DegreesPerSecond = new Vector3(0f, 70f, 0f);
            sp.BobHeight = 0.08f;
            for (int k = 0; k < n; k++)
            {
                float a = k * Mathf.PI * 2f / n;
                Vector3 p = new Vector3(Mathf.Cos(a) * radius, 0.1f * k, Mathf.Sin(a) * radius);
                Ball(orbit, p, Vector3.one * 0.07f, glow);
                var c = glow.HasProperty("_Emission") ? glow.GetColor("_Emission") : Color.white;
                MeshFactory.MeshObject(MeshFactory.SmoothSphere(), orbit, p, Vector3.one * 0.18f, MaterialFactory.Additive(new Color(c.r, c.g, c.b, 0.35f)), false);
            }
        }

        void TaperTail(Material m, float thick, int n)
        {
            var root = J("Tail", rig.pelvis, new Vector3(0f, -0.02f, -0.16f));
            root.localRotation = Quaternion.Euler(-40f, 0f, 0f);
            Transform prev = root;
            for (int k = 0; k < n; k++)
            {
                float w = thick * (1f - k / (float)n);
                Ball(prev, new Vector3(0f, -0.06f, 0f), new Vector3(w * 2f, 0.14f, w * 2f), m);
                var sw = prev.gameObject.AddComponent<Sway>();
                sw.Amount = 5f + k * 2f; sw.Speed = 1.2f;
                var next = J("T", prev, new Vector3(0f, -0.1f, 0f));
                next.localRotation = Quaternion.Euler(12f, 0f, 0f);
                prev = next;
            }
        }

        // ------------------------------------------------------------------ Weapons

        void DoubleAxe()
        {
            ClearWeapon();
            var sp = SwordPivot;
            if (sp == null) return;
            var haft = PM(new Color(0.35f, 0.24f, 0.15f), 0.012f);
            var iron = PM(new Color(0.55f, 0.56f, 0.6f), 0.012f);
            var edge = PMe(new Color(0.9f, 0.92f, 0.95f), 0f, new Color(0.2f, 0.2f, 0.25f));
            Part(PrimitiveType.Cylinder, sp, new Vector3(0f, 0f, 0.4f), new Vector3(0.06f, 0.7f, 0.06f), haft, new Vector3(90f, 0f, 0f));
            for (int s = -1; s <= 1; s += 2)
            {
                Ball(sp, new Vector3(0f, s * 0.2f, 1.02f), new Vector3(0.05f, 0.36f, 0.42f), iron);
                Ball(sp, new Vector3(0f, s * 0.36f, 1.02f), new Vector3(0.03f, 0.08f, 0.46f), edge);
            }
            Part(PrimitiveType.Cube, sp, new Vector3(0f, 0f, 1.02f), new Vector3(0.09f, 0.16f, 0.14f), iron);
            Spike(sp, new Vector3(0f, 0f, 1.1f), Vector3.forward, 0.08f, 0.2f, iron);
        }

        void Scythe()
        {
            ClearWeapon();
            var sp = SwordPivot;
            if (sp == null) return;
            var haft = PM(new Color(0.12f, 0.1f, 0.12f), 0.012f);
            var blade = PM(new Color(0.75f, 0.78f, 0.84f), 0.012f);
            var edge = PMe(new Color(0.6f, 0.4f, 1f), 0f, new Color(0.4f, 0.2f, 0.8f));
            Part(PrimitiveType.Cylinder, sp, new Vector3(0f, 0f, 0.3f), new Vector3(0.045f, 1.1f, 0.045f), haft, new Vector3(90f, 0f, 0f));
            // The curved blade: a row of segments sweeping down and back from the top of the staff.
            for (int k = 0; k < 9; k++)
            {
                float a = k / 8f;
                Vector3 p = new Vector3(0f, -0.08f - Mathf.Sin(a * Mathf.PI * 0.6f) * 0.55f, 1.38f - a * a * 0.45f);
                var seg = J("Scythe", sp, p);
                seg.localRotation = Quaternion.Euler(-40f - a * 70f, 0f, 0f);
                Part(PrimitiveType.Cube, seg, Vector3.zero, new Vector3(0.02f, 0.1f, Mathf.Lerp(0.2f, 0.06f, a)), blade);
                Part(PrimitiveType.Cube, seg, new Vector3(0f, -0.05f, 0f), new Vector3(0.012f, 0.016f, Mathf.Lerp(0.2f, 0.06f, a)), edge);
            }
            Ball(sp, new Vector3(0f, 0f, 1.4f), Vector3.one * 0.09f, edge);
        }

        // ------------------------------------------------------------------ Strikes

        void MoreStrike(int step, bool heavy, Vector3 at, Vector3 fwd, Vector3 right)
        {
            switch (SpeciesKind)
            {
                case Species.Vampire:
                    for (int k = 0; k < 4; k++) VFX.Flash(MeshFactory.SmoothSphere(), at + right * Random.Range(-0.6f, 0.6f) + Vector3.up * Random.Range(-0.2f, 0.4f), Quaternion.identity, new Vector3(0.08f, 0.04f, 0.08f), new Vector3(0.28f, 0.06f, 0.1f), new Color(0.35f, 0.05f, 0.15f, 0.9f), 0.25f);
                    break;
                case Species.Zombie:
                    VFX.HitSpark(at, new Color(0.6f, 0.85f, 0.3f), 8);
                    break;
                case Species.Ghost:
                case Species.Reaper:
                    VFX.Breath(at, SpeciesKind == Species.Ghost ? new Color(0.6f, 0.85f, 1f) : new Color(0.55f, 0.35f, 1f), 8);
                    break;
                case Species.Lizardfolk:
                case Species.Kappa:
                    VFX.HitSpark(at, new Color(0.5f, 0.8f, 1f), 10);
                    break;
                case Species.Minotaur:
                case Species.Troll:
                case Species.Golem:
                    if (heavy || step >= 2) { VFX.Dust(transform.position + fwd * 1.3f, 10); VFX.Shockwave(transform.position + fwd * 1.3f, 2.4f, new Color(0.8f, 0.7f, 0.55f), 0.3f); }
                    break;
                case Species.Orc:
                    VFX.HitSpark(at, new Color(1f, 0.7f, 0.3f), 10);
                    break;
                case Species.Harpy:
                case Species.Tengu:
                    for (int k = 0; k < 3; k++) VFX.Flash(MeshFactory.SmoothCapsule(), at + right * (k - 1) * 0.3f, Quaternion.LookRotation(fwd) * Quaternion.Euler(0f, 0f, 60f + k * 20f), new Vector3(0.02f, 0.1f, 0.02f), new Vector3(0.04f, 0.4f, 0.04f), new Color(0.95f, 0.95f, 1f, 0.8f), 0.2f);
                    break;
                case Species.Kitsune:
                    VFX.Breath(at, new Color(0.4f, 0.75f, 1f), 10);
                    break;
                case Species.Gargoyle:
                    VFX.HitSpark(at, new Color(0.6f, 0.6f, 0.62f), 8);
                    break;
                case Species.Yeti:
                    VFX.HitSpark(at, new Color(0.8f, 0.95f, 1f), 12);
                    break;
                case Species.PumpkinKnight:
                    VFX.Breath(at, new Color(1f, 0.6f, 0.15f), 8);
                    break;
            }
        }
    }
}
