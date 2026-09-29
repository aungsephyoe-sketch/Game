using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// Monster-folk slayers. Built on the same humanoid rig as everyone else, then given their kind's features and
    /// their kind's way of fighting:
    ///   Goblin   — long pointed ears, a long nose, little fangs; a pair of short daggers.
    ///   Skeleton — a skull face (dark sockets with glowing pupils, nose hole, a grin of teeth); a bone blade.
    ///   Demon    — curved horns, pointed ears, fangs, a spade-tipped tail; a trident.
    ///   Cyclops  — one big blinking eye under a single brow, a horn nub; a spiked tree-trunk club.
    ///   Werewolf — muzzle and nose, tall wolf ears, cheek tufts, a bushy tail; claws instead of weapons.
    ///   Mummy    — wrapped head to toe, one eye peeking out and glowing; living bandages that lash out.
    /// Their strikes get their own effects too (claw rakes, bandage lashes, dagger flurries...).
    /// </summary>
    public partial class CharacterVisual
    {
        public Species SpeciesKind { get; private set; }

        void ApplySpecies(CharacterDefinition def)
        {
            SpeciesKind = def.species;
            if (def.species == Species.Human || head == null || rig == null) return;
            var skin = PM(def.skinTone);
            var dark = PM(new Color(0.08f, 0.05f, 0.08f), 0f);
            var ivory = PM(new Color(0.97f, 0.94f, 0.86f), 0.008f);
            Color glowC = Color.Lerp(def.bladeColor, Color.white, 0.2f);
            var glow = PMe(glowC, 0f, glowC * 0.9f);
            float R = (faceR.x + faceR.y) * 0.5f;
            switch (def.species)
            {
                case Species.Goblin:
                    for (int s = -1; s <= 1; s += 2)
                    {
                        var ear = Spike(head, OnHead(s * 88f, 12f, 0.92f), new Vector3(s, 0.4f, -0.25f), R * 0.42f, R * 0.95f, skin, 0.35f, 2f);
                        Spike(ear, new Vector3(0f, R * 0.08f, R * 0.03f), Vector3.up, R * 0.24f, R * 0.7f, PM(new Color(0.95f, 0.6f, 0.6f), 0f), 0.3f);
                    }
                    Spike(head, faceC + new Vector3(0f, -faceR.y * 0.12f, faceR.z * 0.9f), new Vector3(0f, -0.2f, 1f), R * 0.2f, R * 0.42f, skin);
                    Fangs(ivory, 0.045f);
                    // Little daggers: the twin blades, shortened.
                    ScaleWeaponParts(0.72f);
                    break;
                case Species.Skeleton:
                    SkullFace(dark, glow, ivory);
                    BoneBlade(def);
                    break;
                case Species.Demon:
                {
                    var horn = PM(new Color(0.16f, 0.1f, 0.12f), 0.01f);
                    for (int s = -1; s <= 1; s += 2)
                    {
                        var h1 = Spike(head, OnHead(s * 32f, 62f, 0.9f), new Vector3(s * 0.45f, 1f, -0.25f), R * 0.34f, R * 0.55f, horn);
                        Spike(h1, new Vector3(0f, R * 0.45f, 0f), new Vector3(s * 0.2f, 0.6f, -0.8f), R * 0.2f, R * 0.4f, horn);
                        Spike(head, OnHead(s * 88f, 10f, 0.92f), new Vector3(s, 0.35f, -0.2f), R * 0.26f, R * 0.45f, skin, 0.35f);
                    }
                    Fangs(ivory, 0.04f);
                    Tail(def, skin, true, 0.05f);
                    Trident(def);
                    break;
                }
                case Species.Cyclops:
                {
                    HideFace(false);
                    var eye = OnFace(faceC, faceR, 0f, 0.02f, 0.004f);
                    eye.gameObject.AddComponent<EyeLid>();
                    Ball(eye, Vector3.zero, new Vector3(0.3f, 0.26f, 0.05f), PM(new Color(0.99f, 0.99f, 1f), 0f));
                    Ball(eye, new Vector3(0f, -0.01f, 0.006f), new Vector3(0.17f, 0.17f, 0.04f), PMe(glowC, 0f, glowC * 0.2f));
                    Ball(eye, new Vector3(0f, -0.01f, 0.012f), new Vector3(0.08f, 0.09f, 0.035f), dark);
                    Ball(eye, new Vector3(-0.04f, 0.035f, 0.02f), new Vector3(0.05f, 0.045f, 0.02f), PM(Color.white, 0f));
                    Part(PrimitiveType.Capsule, eye, new Vector3(0f, 0.13f, 0.004f), new Vector3(0.035f, 0.13f, 0.02f), PM(Color.Lerp(def.hairColor, Color.black, 0.4f), 0f), new Vector3(0f, 0f, 90f));
                    Spike(head, OnHead(0f, 58f, 0.95f), new Vector3(0f, 1f, 0.3f), R * 0.2f, R * 0.25f, PM(new Color(0.9f, 0.85f, 0.7f), 0.01f));
                    Club(def);
                    break;
                }
                case Species.Werewolf:
                {
                    var fur = skin;
                    var furLight = PM(Color.Lerp(def.skinTone, Color.white, 0.35f));
                    HideMouth();
                    var muzzle = faceC + new Vector3(0f, -faceR.y * 0.28f, faceR.z * 0.72f);
                    Ball(head, muzzle, new Vector3(R * 0.62f, R * 0.46f, R * 0.8f), furLight);
                    Ball(head, muzzle + new Vector3(0f, R * 0.08f, R * 0.4f), new Vector3(R * 0.22f, R * 0.14f, R * 0.14f), dark);
                    Part(PrimitiveType.Capsule, head, muzzle + new Vector3(0f, -R * 0.14f, R * 0.33f), new Vector3(0.02f, R * 0.18f, 0.02f), dark, new Vector3(0f, 0f, 90f));
                    for (int s = -1; s <= 1; s += 2)
                    {
                        Spike(head, muzzle + new Vector3(s * R * 0.12f, -R * 0.18f, R * 0.3f), Vector3.down, R * 0.07f, R * 0.12f, ivory);
                        var ear = Spike(head, OnHead(s * 38f, 64f, 0.9f), new Vector3(s * 0.35f, 1f, -0.1f), R * 0.46f, R * 0.7f, fur, 0.45f, 3f);
                        Spike(ear, new Vector3(0f, R * 0.06f, R * 0.04f), Vector3.up, R * 0.26f, R * 0.46f, PM(new Color(0.9f, 0.6f, 0.62f), 0f), 0.3f);
                        Spike(head, OnHead(s * 84f, -22f, 0.95f), new Vector3(s, -0.4f, -0.1f), R * 0.3f, R * 0.36f, fur, 0.4f);
                    }
                    Tail(def, fur, false, 0.1f);
                    Claws(ivory);
                    break;
                }
                case Species.Mummy:
                {
                    var wrap = PM(new Color(0.9f, 0.85f, 0.7f), 0.01f);
                    var wrapD = PM(new Color(0.78f, 0.72f, 0.58f), 0.01f);
                    RemoveHair();
                    HideMouth();
                    float[] ys = { 0.8f, 0.55f, 0.3f, -0.42f, -0.68f };
                    for (int i = 0; i < ys.Length; i++)
                    {
                        float y = ys[i] * faceR.y;
                        float k = Mathf.Sqrt(Mathf.Max(0.05f, 1f - (y * y) / (faceR.y * faceR.y)));
                        Band(head, faceC + new Vector3(0f, y, 0f), faceR.x * k + 0.02f, 0.07f, 0.014f, i % 2 == 0 ? wrap : wrapD,
                            new Vector3(1f, 1f, faceR.z / faceR.x), new Vector3(i % 2 == 0 ? 6f : -8f, 0f, i % 2 == 0 ? 5f : -4f));
                    }
                    // One eye covered by a diagonal wrap, the other glowing.
                    Band(head, faceC + new Vector3(0f, -0.02f * faceR.y, 0f), faceR.x + 0.03f, 0.06f, 0.014f, wrap, new Vector3(1f, 1f, faceR.z / faceR.x), new Vector3(0f, 0f, 22f));
                    var lids = head.GetComponentsInChildren<EyeLid>(true);
                    foreach (var l in lids)
                        if (l.transform.localPosition.x > 0f) Ball(l.transform, new Vector3(0f, 0f, 0.012f), Vector3.one * 0.05f, glow);
                    for (int i = 0; i < 2; i++)
                    {
                        Band(rig.upper[i], new Vector3(0f, -0.08f, 0f), 0.075f, 0.05f, 0.012f, wrap);
                        Band(rig.lower[i], new Vector3(0f, -0.06f, 0f), 0.06f, 0.05f, 0.012f, wrapD);
                        Band(rig.lower[i], new Vector3(0f, -0.14f, 0f), 0.055f, 0.05f, 0.012f, wrap);
                        Band(rig.shin[i], new Vector3(0f, -0.1f, 0f), 0.065f, 0.06f, 0.012f, wrap);
                        DStrips(rig.lower[i], new Vector3(0f, -0.12f, -0.03f), new Vector3(10f, 0f, 0f), 1, 0.04f, 0.28f, 0.05f, 2, wrapD, null);
                    }
                    Bandages(wrap, wrapD);
                    break;
                }
                default:
                    MoreSpecies(def, skin, dark, ivory, glow, glowC, R);
                    break;
            }
            Menace(def, R);
        }

        /// <summary>Skull face: dark sockets with glowing pupils, a nose hole and a grin of teeth.</summary>
        void SkullFace(Material dark, Material glow, Material ivory)
        {
            HideFace(true);
            for (int s = -1; s <= 1; s += 2)
            {
                var so = OnFace(faceC, faceR, s * 0.14f, -0.01f, 0.004f);
                Ball(so, Vector3.zero, new Vector3(0.15f, 0.16f, 0.03f), dark);
                Ball(so, new Vector3(0f, 0f, 0.012f), Vector3.one * 0.05f, glow);
            }
            var nose = OnFace(faceC, faceR, 0f, -0.12f, 0.004f);
            for (int s = -1; s <= 1; s += 2) Ball(nose, new Vector3(s * 0.012f, 0f, 0f), new Vector3(0.022f, 0.04f, 0.02f), dark, new Vector3(0f, 0f, s * 20f));
            var grin = OnFace(faceC, faceR, 0f, -0.23f, 0.004f);
            Ball(grin, Vector3.zero, new Vector3(0.22f, 0.07f, 0.025f), dark);
            for (int k = 0; k < 7; k++) Spike(grin, new Vector3(-0.078f + k * 0.026f, 0.02f, 0.008f), Vector3.down, 0.022f, 0.035f, ivory);
        }

        // ------------------------------------------------------------------ Face helpers

        /// <summary>Hides the human face parts: eyes and brows (and nose and mouth for a skull).</summary>
        void HideFace(bool all)
        {
            foreach (var l in head.GetComponentsInChildren<EyeLid>(true))
                foreach (var r in l.GetComponentsInChildren<Renderer>(true)) r.enabled = false;
            foreach (Transform f in head)
            {
                if (f.name != "Face") continue;
                bool brow = f.localPosition.y > faceC.y + 0.02f && Mathf.Abs(f.localPosition.x) > 0.04f;
                if (all || brow)
                    foreach (var r in f.GetComponentsInChildren<Renderer>(true)) r.enabled = false;
            }
        }

        void HideMouth()
        {
            Transform mouth = null;
            float low = float.MaxValue;
            foreach (Transform f in head)
                if (f.name == "Face" && Mathf.Abs(f.localPosition.x) < 0.03f && f.localPosition.y < low && f.GetComponentInChildren<EyeLid>() == null) { low = f.localPosition.y; mouth = f; }
            if (mouth != null) foreach (var r in mouth.GetComponentsInChildren<Renderer>(true)) r.enabled = false;
        }

        void RemoveHair()
        {
            for (int i = head.childCount - 1; i >= 0; i--)
            {
                var c = head.GetChild(i);
                if (c.name.StartsWith("Hair") || c.name == "Twintail" || c.name == "Braid") Object.DestroyImmediate(c.gameObject);
            }
        }

        void Fangs(Material ivory, float spread)
        {
            var mo = OnFace(faceC, faceR, 0f, -0.2f, 0.006f);
            for (int s = -1; s <= 1; s += 2) Spike(mo, new Vector3(s * spread, 0.004f, 0f), Vector3.down, 0.02f, 0.035f, ivory);
        }

        /// <summary>A swaying tail from the small of the back (spade-tipped for demons, bushy for wolves).</summary>
        void Tail(CharacterDefinition def, Material m, bool spade, float thick)
        {
            var root = J("Tail", rig.pelvis, new Vector3(0f, -0.02f, -0.16f));
            root.localRotation = Quaternion.Euler(-35f, 0f, 0f);
            Transform prev = root;
            int n = spade ? 6 : 4;
            for (int k = 0; k < n; k++)
            {
                float w = spade ? thick : thick * (1f + k * 0.35f - (k == n - 1 ? 0.5f : 0f));
                Ball(prev, new Vector3(0f, -0.06f, 0f), new Vector3(w * 1.5f, spade ? 0.12f : 0.16f, w * 1.5f), m);
                var sw = prev.gameObject.AddComponent<Sway>();
                sw.Amount = 6f + k * 3f; sw.Speed = 1.3f;
                var next = J("T", prev, new Vector3(0f, spade ? -0.1f : -0.11f, 0f));
                next.localRotation = Quaternion.Euler(spade ? 16f : 12f, 0f, 0f);
                prev = next;
            }
            if (spade) Spike(prev, Vector3.zero, Vector3.down, 0.1f, 0.12f, PM(new Color(0.16f, 0.1f, 0.12f), 0.01f), 0.35f);
        }

        // ------------------------------------------------------------------ Species weapons

        /// <summary>Removes the built weapon meshes (keeps the swing trail) so a species weapon can replace them.</summary>
        void ClearWeapon()
        {
            if (SwordPivot == null) return;
            foreach (var r in SwordPivot.GetComponentsInChildren<MeshRenderer>(true))
                if (r != null && r.GetComponent<TrailRenderer>() == null) Object.DestroyImmediate(r.gameObject); // (a parent may already be gone)
        }

        void ScaleWeaponParts(float k)
        {
            if (SwordPivot == null) return;
            for (int i = 0; i < SwordPivot.childCount; i++) SwordPivot.GetChild(i).localScale *= k;
            var left = rig.hand[1] != null ? FindPart(rig.hand[1], "Kodachi") : null;
            if (left != null) left.localScale *= k;
        }

        void BoneBlade(CharacterDefinition def)
        {
            ClearWeapon();
            var sp = SwordPivot;
            if (sp == null) return;
            var bone = PM(new Color(0.95f, 0.92f, 0.82f), 0.012f);
            var boneD = PM(new Color(0.8f, 0.75f, 0.64f), 0.012f);
            Part(PrimitiveType.Capsule, sp, new Vector3(0f, 0f, -0.04f), new Vector3(0.05f, 0.18f, 0.05f), boneD, new Vector3(90f, 0f, 0f));
            for (int s = -1; s <= 1; s += 2) Ball(sp, new Vector3(0f, s * 0.03f, -0.22f), Vector3.one * 0.06f, bone);
            // The blade: a long flat bone, knobbed at the tip, with a jagged back.
            Part(PrimitiveType.Capsule, sp, new Vector3(0f, 0f, 0.62f), new Vector3(0.04f, 0.46f, 0.12f), bone, new Vector3(90f, 0f, 0f));
            for (int s = -1; s <= 1; s += 2) Ball(sp, new Vector3(0f, s * 0.035f, 1.07f), new Vector3(0.06f, 0.09f, 0.09f), bone);
            for (int k = 0; k < 4; k++) Spike(sp, new Vector3(0f, 0.05f, 0.3f + k * 0.17f), new Vector3(0f, 1f, -0.4f), 0.05f, 0.07f, boneD, 0.4f);
            Ball(sp, new Vector3(0f, 0f, 0.1f), new Vector3(0.1f, 0.12f, 0.06f), boneD);
        }

        void Trident(CharacterDefinition def)
        {
            ClearWeapon();
            var sp = SwordPivot;
            if (sp == null) return;
            var iron = PM(new Color(0.22f, 0.18f, 0.22f), 0.012f);
            var edge = PMe(new Color(1f, 0.55f, 0.2f), 0f, new Color(0.8f, 0.3f, 0.05f));
            Part(PrimitiveType.Cylinder, sp, new Vector3(0f, 0f, 0.25f), new Vector3(0.045f, 1.05f, 0.045f), iron, new Vector3(90f, 0f, 0f));
            Part(PrimitiveType.Cube, sp, new Vector3(0f, 0f, 1.28f), new Vector3(0.05f, 0.42f, 0.06f), iron);
            Spike(sp, new Vector3(0f, 0f, 1.3f), Vector3.forward, 0.08f, 0.42f, iron, 0.5f);
            for (int s = -1; s <= 1; s += 2)
            {
                Part(PrimitiveType.Cube, sp, new Vector3(0f, s * 0.19f, 1.36f), new Vector3(0.04f, 0.04f, 0.16f), iron);
                Spike(sp, new Vector3(0f, s * 0.19f, 1.43f), new Vector3(0f, s * 0.15f, 1f), 0.07f, 0.28f, iron, 0.5f);
            }
            Part(PrimitiveType.Cube, sp, new Vector3(0f, 0f, 1.5f), new Vector3(0.012f, 0.02f, 0.3f), edge);
            Ball(sp, new Vector3(0f, 0f, 1.28f), Vector3.one * 0.08f, edge);
            Spike(sp, new Vector3(0f, 0f, -0.8f), Vector3.back, 0.06f, 0.14f, iron);
        }

        void Club(CharacterDefinition def)
        {
            ClearWeapon();
            var sp = SwordPivot;
            if (sp == null) return;
            var wood = PM(new Color(0.5f, 0.34f, 0.2f), 0.014f);
            var woodD = PM(new Color(0.38f, 0.25f, 0.15f), 0.014f);
            var iron = PM(new Color(0.4f, 0.4f, 0.44f), 0.012f);
            Part(PrimitiveType.Capsule, sp, new Vector3(0f, 0f, 0f), new Vector3(0.07f, 0.26f, 0.07f), woodD, new Vector3(90f, 0f, 0f));
            // The head thickens like a tree trunk toward the end.
            for (int k = 0; k < 6; k++)
            {
                float z = 0.35f + k * 0.2f, rad = Mathf.Lerp(0.1f, 0.26f, k / 5f);
                Ball(sp, new Vector3(0f, 0f, z), new Vector3(rad * 2f, rad * 2f, 0.3f), k % 2 == 0 ? wood : woodD);
            }
            Band(sp, new Vector3(0f, 0f, 0.6f), 0.15f, 0.05f, 0.02f, iron, null, new Vector3(90f, 0f, 0f));
            Band(sp, new Vector3(0f, 0f, 1.2f), 0.25f, 0.06f, 0.02f, iron, null, new Vector3(90f, 0f, 0f));
            for (int k = 0; k < 8; k++)
            {
                float a = k * Mathf.PI * 2f / 8f;
                Vector3 o = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f);
                Spike(sp, o * 0.2f + Vector3.forward * (0.95f + (k % 2) * 0.2f), o, 0.06f, 0.1f, iron);
            }
        }

        void Claws(Material ivory)
        {
            for (int i = 0; i < 2; i++)
            {
                var h = rig.hand[i];
                if (h == null) continue;
                var g = FindPart(h, "Gauntlet");
                if (g != null) Object.DestroyImmediate(g.gameObject);
                for (int k = 0; k < 3; k++)
                    Spike(h, new Vector3(-0.035f + k * 0.035f, -0.1f, 0.045f), new Vector3((k - 1) * 0.15f, -1f, 0.55f), 0.028f, 0.13f, ivory);
            }
        }

        /// <summary>Living bandages: a long wrap trails from each hand and whips with every strike.</summary>
        void Bandages(Material wrap, Material wrapD)
        {
            ClearWeapon();
            var left = rig.hand[1] != null ? FindPart(rig.hand[1], "LeftFan") : null;
            if (left != null) Object.DestroyImmediate(left.gameObject);
            for (int side = 0; side < 2; side++)
            {
                Transform parent = side == 0 ? SwordPivot : rig.hand[1];
                if (parent == null) continue;
                Transform prev = J("Bandage", parent, side == 0 ? Vector3.zero : new Vector3(0f, -0.08f, 0f));
                prev.localRotation = side == 0 ? Quaternion.identity : Quaternion.Euler(-70f, 0f, 0f);
                for (int k = 0; k < 7; k++)
                {
                    Part(PrimitiveType.Cube, prev, new Vector3(0f, 0f, 0.07f), new Vector3(0.012f, 0.07f - k * 0.004f, 0.15f), k % 2 == 0 ? wrap : wrapD, new Vector3(0f, 0f, k * 12f));
                    var sw = prev.gameObject.AddComponent<Sway>();
                    sw.Amount = 8f + k * 3f; sw.Speed = 1.6f + k * 0.1f;
                    var next = J("B", prev, new Vector3(0f, 0f, 0.14f));
                    next.localRotation = Quaternion.Euler(k % 2 == 0 ? 10f : -8f, 12f, 0f);
                    prev = next;
                }
            }
        }

        // ------------------------------------------------------------------ Species strikes

        /// <summary>The slayer's species flavour at a point (used by their special): claw rakes, bandages, bats...</summary>
        public void SpeciesBurstAt(Vector3 at)
        {
            if (SpeciesKind == Species.Human) return;
            Vector3 fwd = transform.forward;
            switch (SpeciesKind)
            {
                case Species.Werewolf:
                case Species.Yeti:
                case Species.Zombie:
                case Species.Gargoyle:
                {
                    var rot = Quaternion.LookRotation(fwd) * Quaternion.Euler(0f, 0f, 35f);
                    for (int k = -1; k <= 1; k++)
                        VFX.Flash(MeshFactory.SmoothCapsule(), at + rot * new Vector3(k * 0.2f, 0f, 0f), rot, new Vector3(0.04f, 0.1f, 0.04f), new Vector3(0.07f, 1f, 0.07f), new Color(1f, 0.95f, 0.9f, 0.9f), 0.22f);
                    break;
                }
                case Species.Mummy:
                    VFX.Flash(MeshFactory.RoundedCube(), at, Quaternion.Euler(Random.Range(0f, 360f), Random.Range(0f, 360f), 0f), new Vector3(0.12f, 0.3f, 0.02f), new Vector3(0.1f, 2.2f, 0.02f), new Color(0.95f, 0.9f, 0.72f, 0.9f), 0.3f);
                    break;
                case Species.Vampire:
                    for (int k = 0; k < 5; k++) VFX.Flash(MeshFactory.SmoothSphere(), at + Random.insideUnitSphere * 0.8f, Quaternion.identity, new Vector3(0.08f, 0.04f, 0.08f), new Vector3(0.3f, 0.06f, 0.1f), new Color(0.35f, 0.05f, 0.15f, 0.9f), 0.3f);
                    break;
                case Species.Skeleton:
                case Species.Reaper:
                    VFX.HitSpark(at, new Color(0.95f, 0.92f, 0.82f), 12);
                    VFX.Breath(at, new Color(0.55f, 0.35f, 1f), 8);
                    break;
                default:
                    MoreStrike(4, true, at, fwd, transform.right);
                    break;
            }
        }

        /// <summary>Extra strike effect in the slayer's own kind of attack (called with every attack).</summary>
        void SpeciesStrike(int step, bool heavy)
        {
            if (SpeciesKind == Species.Human) return;
            Vector3 fwd = transform.forward, right = transform.right;
            Vector3 at = transform.position + Vector3.up * 1f + fwd * 1.1f;
            switch (SpeciesKind)
            {
                case Species.Werewolf:
                {
                    // Three claw rakes, slanting the way the paw swipes.
                    float tilt = (step % 2 == 0 ? 35f : -35f);
                    var rot = Quaternion.LookRotation(fwd) * Quaternion.Euler(0f, 0f, tilt);
                    for (int k = -1; k <= 1; k++)
                        VFX.Flash(MeshFactory.SmoothCapsule(), at + rot * new Vector3(k * 0.16f, 0f, 0f), rot, new Vector3(0.03f, 0.1f, 0.03f), new Vector3(0.05f, heavy ? 0.9f : 0.65f, 0.05f), new Color(1f, 0.95f, 0.9f, 0.9f), 0.18f);
                    break;
                }
                case Species.Mummy:
                {
                    // A bandage lashes out straight ahead and snaps back.
                    var rot = Quaternion.LookRotation(fwd) * Quaternion.Euler(90f, 0f, 0f);
                    VFX.Flash(MeshFactory.RoundedCube(), transform.position + Vector3.up * 1.1f + fwd * 1.6f, rot, new Vector3(0.12f, 0.4f, 0.02f), new Vector3(0.1f, 3f, 0.02f), new Color(0.95f, 0.9f, 0.72f, 0.9f), 0.22f);
                    break;
                }
                case Species.Goblin:
                    VFX.HitSpark(at + right * (step % 2 == 0 ? 0.2f : -0.2f), new Color(0.8f, 1f, 0.5f), 6);
                    break;
                case Species.Cyclops:
                    if (heavy || step >= 3) { VFX.Dust(transform.position + fwd * 1.4f, 8); VFX.Shockwave(transform.position + fwd * 1.4f, 2.2f, new Color(0.85f, 0.75f, 0.55f), 0.3f); }
                    break;
                case Species.Skeleton:
                    VFX.HitSpark(at, new Color(0.95f, 0.92f, 0.82f), 5);
                    break;
                case Species.Demon:
                    VFX.Breath(at, new Color(1f, 0.45f, 0.12f), 6);
                    break;
                default:
                    MoreStrike(step, heavy, at, fwd, right);
                    break;
            }
        }
    }
}
