using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// The sculpted hairstyles. Each one is a small recipe of hairline, sections and locks on top of
    /// <see cref="HairGeometry"/>: the direction the hair grows from (whorl or part), the section it belongs to
    /// (bangs, sides, top, back, long fall), how heavy it is and how the tips behave.
    /// Groups: "Cap" and "Top" are fixed to the head; "Bangs", "SideL", "SideR", "Back" and "Fall" each swing.
    /// </summary>
    public static class HairStyles
    {
        /// <summary>True for the characters that use the new sculpted hair (the quality standard).</summary>
        public static bool Has(string id)
        {
            switch (id)
            {
                case "ren_initiate": case "mina_ember": case "sora_initiate":
                case "tobi_kazami": case "bunta_okuyama": case "sayo_mikage": case "nene_hanabusa": case "nagi_kurokiri": case "seiran_mizuchi":
                    return true;
            }
            return false;
        }

        public static void Build(string id, HairGeometry g)
        {
            switch (id)
            {
                case "mina_ember": LongFlowing(g); break;
                case "sora_initiate": ControlledSpiky(g); break;
                case "tobi_kazami": SweptBack(g); break;
                case "bunta_okuyama": Cropped(g); break;
                case "sayo_mikage": HighPonytail(g); break;
                case "nene_hanabusa": RoundBob(g); break;
                case "nagi_kurokiri": Hooded(g); break;
                case "seiran_mizuchi": SleekLong(g); break;
                default: ShortMessy(g); break;
            }
        }

        /// <summary>Flow direction on the head: away from a crown whorl, with a downward pull.</summary>
        static Vector3 AwayFrom(HairGeometry g, Vector3 from, float az, float polar, float down)
        {
            Vector3 d = g.Surf(az, polar, 0f) - from;
            d.y -= down;
            return d.sqrMagnitude > 1e-6f ? d.normalized : Vector3.down;
        }

        /// <summary>
        /// Ren — short, soft and a little messy: hair spirals out from a crown whorl, an uneven fringe falls to the
        /// brows, swept slightly to one side, pieces in front of the ears and short flicks at the nape.
        /// </summary>
        static void ShortMessy(HairGeometry g)
        {
            g.Cap("Top", HairGeometry.Shade, 0.035f, 64f, 100f, 142f, 6f);
            Vector3 whorl = g.Surf(165f, 22f, 0f);
            // Top layer: overlapping clumps radiating from the whorl, tips lifting a little for volume.
            for (int i = 0; i < 16; i++)
            {
                float az = 70f + i * 14.5f + g.R(-5f, 5f);
                float pol = g.R(14f, 34f);
                Vector3 flow = AwayFrom(g, whorl, az, pol + 25f, 0.08f);
                g.Lock("Top", i % 3 == 0 ? HairGeometry.Shade : HairGeometry.Base, az, pol, g.R(0.08f, 0.11f), flow,
                    g.R(0.26f, 0.34f), g.R(0.15f, 0.19f), 0.05f, 0.35f, 0.25f, g.R(-30f, -12f), 10, -9f, -0.3f, 0.22f);
            }
            // Back: two overlapping tiers down to the nape, uneven lengths, tips flicking out.
            for (int i = 0; i < 9; i++)
            {
                float az = 112f + i * 17f + g.R(-5f, 5f);
                g.Lock("Back", i % 2 == 0 ? HairGeometry.Base : HairGeometry.Shade, az, g.R(50f, 66f), g.R(0.07f, 0.09f), HairGeometry.V(0f, -1f, -0.2f),
                    g.R(0.3f, 0.4f), g.R(0.16f, 0.19f), 0.045f, 0.8f, 0.12f, g.R(-40f, -18f), 10, -9f, -0.95f, 0.25f);
            }
            for (int i = 0; i < 8; i++)
            {
                float az = 120f + i * 17f + g.R(-5f, 5f);
                g.Lock("Back", i % 2 == 1 ? HairGeometry.Base : HairGeometry.Shade, az, g.R(84f, 100f), g.R(0.045f, 0.06f), HairGeometry.V(0f, -1f, -0.1f),
                    g.R(0.16f, 0.24f), g.R(0.14f, 0.17f), 0.04f, 0.8f, 0.08f, g.R(-30f, -10f), 8, -9f, -0.98f, 0.3f);
            }
            // Sides: a piece in front of each ear, down to the cheek.
            for (int s = -1; s <= 1; s += 2)
            {
                g.Lock(s < 0 ? "SideL" : "SideR", HairGeometry.Base, s * 68f, 52f, 0.06f, HairGeometry.V(s * 0.25f, -1f, 0.25f), 0.3f, 0.1f, 0.035f, 1f, 0.02f, 18f, 10, -9f, -0.9f, 0.3f);
                g.Lock(s < 0 ? "SideL" : "SideR", HairGeometry.Shade, s * 95f, 50f, 0.055f, HairGeometry.V(0f, -1f, -0.2f), 0.26f, 0.12f, 0.04f, 1f, 0.05f, -10f, 9, -9f, -0.9f, 0.3f);
            }
            // Fringe: uneven lengths, swept a little to the character's right, stopping above the brows.
            float[] bangAz = { -38f, -20f, -4f, 12f, 28f, 44f };
            for (int i = 0; i < bangAz.Length; i++)
            {
                float az = bangAz[i];
                g.Lock("Bangs", i == 2 ? HairGeometry.Shade : HairGeometry.Base, az, g.R(30f, 38f), 0.075f, HairGeometry.V(0.35f + az * 0.004f, -0.45f, 1f),
                    g.R(0.27f, 0.34f), g.R(0.12f, 0.15f), 0.04f, 0.55f, 0.02f, g.R(14f, 26f), 10, 0.1f + g.R(0f, 0.03f), -1f, 0.2f);
            }
            g.Highlight("Top", -20f, 26f, 0.1f, HairGeometry.V(-0.3f, -0.4f, 1f), 0.16f, 0.04f);
            g.Highlight("Top", 25f, 30f, 0.1f, HairGeometry.V(0.4f, -0.4f, 1f), 0.13f, 0.035f);
            g.Highlight("Top", 70f, 34f, 0.1f, HairGeometry.V(1f, -0.4f, 0.3f), 0.12f, 0.035f);
            g.pivots["Bangs"] = g.hc + new Vector3(0f, g.hr.y * 0.75f, g.hr.z * 0.45f);
            g.pivots["Back"] = g.hc + new Vector3(0f, g.hr.y * 0.35f, -g.hr.z * 0.85f);
            g.pivots["SideL"] = g.hc + new Vector3(-g.hr.x * 0.8f, g.hr.y * 0.45f, 0.05f);
            g.pivots["SideR"] = g.hc + new Vector3(g.hr.x * 0.8f, g.hr.y * 0.45f, 0.05f);
        }

        /// <summary>
        /// Mina — long and flowing: a soft side part, the top sweeping away from it and falling past the shoulders
        /// with weight, face-framing pieces in front of the shoulders, long side-swept bangs clear of the eyes.
        /// </summary>
        static void LongFlowing(HairGeometry g)
        {
            const float part = 22f;
            g.Cap("Top", HairGeometry.Shade, 0.03f, 62f, 100f, 145f, 3f, part);
            // Long hair keeps its width and ends softly.
            g.taperStart = 0.55f;
            g.tipWidth = 0.18f;
            // Upper body, so the fall drapes over the shoulders and down the back instead of through them.
            g.AddBody(new Vector3(0f, -0.3f, -0.01f), new Vector3(0.3f, 0.33f, 0.2f), 0.025f);
            g.AddBody(new Vector3(0f, -0.02f, 0f), new Vector3(0.12f, 0.14f, 0.12f), 0.01f);
            // The fall: from the part and crown, over the head and down the back.
            for (int i = 0; i < 18; i++)
            {
                float az = 100f + i * 9.5f + g.R(-3f, 3f);
                float pol = g.R(12f, 40f);
                Vector3 flow = HairGeometry.V(Mathf.Sin(az * Mathf.Deg2Rad) * 0.6f, -0.6f, Mathf.Cos(az * Mathf.Deg2Rad) * 0.6f - 0.35f);
                g.Lock("Fall", i % 3 == 1 ? HairGeometry.Shade : HairGeometry.Base, az, pol, g.R(0.05f, 0.08f), flow,
                    g.R(0.85f, 1.02f), g.R(0.19f, 0.24f), 0.055f, 1.1f, 0.03f, g.R(8f, 22f), 16, -9f, -0.05f, 0f);
            }
            // Sides: sweep away from the part, down past the ears; the front ones frame the face and rest in front of the shoulders.
            for (int s = -1; s <= 1; s += 2)
            {
                string grp = s < 0 ? "SideL" : "SideR";
                for (int i = 0; i < 5; i++)
                {
                    float az = s * (48f + i * 13f) + g.R(-3f, 3f);
                    float pol = s > 0 ? g.R(22f, 38f) : g.R(30f, 46f);
                    bool front = i < 2;
                    Vector3 flow = HairGeometry.V(s * 0.7f, -0.8f, front ? 0.35f : -0.15f);
                    g.Lock(grp, i == 2 ? HairGeometry.Shade : HairGeometry.Base, az, pol, g.R(0.055f, 0.08f), flow,
                        front ? g.R(0.62f, 0.72f) : g.R(0.72f, 0.85f), front ? g.R(0.12f, 0.15f) : g.R(0.17f, 0.21f), 0.05f, 1.15f, 0.02f, g.R(10f, 22f), 14, -9f, -0.1f, 0f);
                }
            }
            // From the part: the front of the crown sweeps away to both sides.
            for (int i = 0; i < 7; i++)
            {
                int s = i % 2 == 0 ? 1 : -1;
                float pol = 6f + (i / 2) * 9f;
                g.Lock(s < 0 ? "SideL" : "SideR", i == 3 ? HairGeometry.Shade : HairGeometry.Base, part + s * 4f, pol, g.R(0.07f, 0.09f), HairGeometry.V(s * 1f, -0.25f, -0.35f),
                    g.R(0.75f, 0.85f), g.R(0.18f, 0.22f), 0.05f, 1.1f, 0.02f, g.R(10f, 20f), 15, 0.12f, -0.1f, 0f);
            }
            // Bangs: long and side-swept from the part toward the character's left, ending above the eyes.
            g.taperStart = 0.3f;
            for (int i = 0; i < 5; i++)
            {
                float az = part - 6f - i * 12f;
                g.Lock("Bangs", i == 1 ? HairGeometry.Shade : HairGeometry.Base, az, g.R(24f, 32f), 0.07f, HairGeometry.V(-0.95f, -0.35f, 0.55f),
                    g.R(0.34f, 0.42f), g.R(0.13f, 0.16f), 0.04f, 0.5f, 0.02f, g.R(18f, 28f), 11, 0.1f + g.R(0f, 0.025f), -1f, 0.15f);
            }
            g.taperStart = 0.55f;
            // One longer piece at the side of the face, outside the eye line.
            g.Lock("SideL", HairGeometry.Base, -44f, 40f, 0.08f, HairGeometry.V(-0.4f, -1f, 0.35f), 0.62f, 0.11f, 0.04f, 1.2f, 0.03f, 16f, 14, -9f, -0.1f, 0f);
            g.taperStart = 0.12f;
            g.tipWidth = 0f;
            g.Highlight("Top", part + 30f, 24f, 0.1f, HairGeometry.V(0.9f, -0.5f, 0.2f), 0.2f, 0.045f);
            g.Highlight("Top", part - 40f, 30f, 0.1f, HairGeometry.V(-0.9f, -0.4f, 0.4f), 0.18f, 0.04f);
            g.Highlight("Top", 160f, 30f, 0.1f, HairGeometry.V(0f, -0.6f, -1f), 0.2f, 0.045f);
            g.Highlight("Top", -150f, 34f, 0.1f, HairGeometry.V(-0.3f, -0.6f, -1f), 0.16f, 0.04f);
            g.pivots["Bangs"] = g.hc + new Vector3(0f, g.hr.y * 0.8f, g.hr.z * 0.4f);
            g.pivots["Fall"] = g.hc + new Vector3(0f, g.hr.y * 0.55f, -g.hr.z * 0.7f);
            g.pivots["SideL"] = g.hc + new Vector3(-g.hr.x * 0.85f, g.hr.y * 0.5f, 0f);
            g.pivots["SideR"] = g.hc + new Vector3(g.hr.x * 0.85f, g.hr.y * 0.5f, 0f);
        }

        /// <summary>
        /// Sora — controlled spikes: broad clumps rooted in the scalp, grouped in three swept clusters that curve up
        /// and back, a pointed fringe that parts over the forehead, and short spikes at the nape.
        /// </summary>
        static void ControlledSpiky(HairGeometry g)
        {
            g.Cap("Top", HairGeometry.Shade, 0.04f, 66f, 98f, 138f, 7f);
            // Three clusters: left, crown, right; each clump shares its cluster's sweep with a little variation.
            float[] clusterAz = { -120f, 180f, 120f };
            for (int c = 0; c < 3; c++)
            {
                for (int i = 0; i < 5; i++)
                {
                    float az = clusterAz[c] + (i - 2f) * 17f + g.R(-4f, 4f);
                    float pol = c == 1 ? g.R(18f, 58f) : g.R(28f, 62f);
                    Vector3 outward = HairGeometry.Dir(az, pol);
                    Vector3 flow = (new Vector3(outward.x, 0f, outward.z).normalized * 0.8f + new Vector3(0f, 0.25f, -0.55f)).normalized;
                    g.Lock("Top", i % 2 == 0 ? HairGeometry.Base : HairGeometry.Shade, az, pol, g.R(0.05f, 0.075f), flow,
                        g.R(0.24f, 0.34f), g.R(0.17f, 0.21f), 0.06f, 0.12f, 0.75f, g.R(-38f, -22f), 9, -9f, -0.3f, 0.7f);
                }
            }
            // Top: broad clumps lying back from the hairline, so the silhouette is connected, not a crown of cones.
            for (int s = -1; s <= 1; s += 2)
                g.Lock("Top", HairGeometry.Base, s * 24f, 24f, 0.07f, HairGeometry.V(s * 0.4f, 0f, -1f), 0.3f, 0.2f, 0.06f, 0.2f, 0.5f, -25f, 9, -9f, -0.3f, 0.45f);
            // Fringe: pointed pieces that part over the forehead and curve toward the temples.
            float[] bangAz = { -34f, -14f, 6f, 26f };
            for (int i = 0; i < bangAz.Length; i++)
            {
                float az = bangAz[i];
                float sweep = az < 0f ? -0.55f : 0.55f;
                g.Lock("Bangs", i == 1 ? HairGeometry.Shade : HairGeometry.Base, az, g.R(32f, 38f), 0.075f, HairGeometry.V(sweep, -0.5f, 1f),
                    g.R(0.26f, 0.32f), g.R(0.13f, 0.16f), 0.045f, 0.45f, 0.08f, g.R(10f, 20f), 9, 0.11f + g.R(0f, 0.02f), -1f, 0.3f);
            }
            // Nape and sides: short, pointed, angled down and back.
            for (int i = 0; i < 7; i++)
            {
                float az = 125f + i * 18f + g.R(-5f, 5f);
                g.Lock("Back", i % 2 == 0 ? HairGeometry.Base : HairGeometry.Shade, az, g.R(66f, 96f), g.R(0.05f, 0.07f), HairGeometry.V(0f, -1f, -0.4f),
                    g.R(0.18f, 0.28f), g.R(0.14f, 0.17f), 0.045f, 0.5f, 0.3f, g.R(-30f, -15f), 8, -9f, -0.95f, 0.4f);
            }
            for (int s = -1; s <= 1; s += 2)
                g.Lock(s < 0 ? "SideL" : "SideR", HairGeometry.Base, s * 72f, 55f, 0.06f, HairGeometry.V(s * 0.3f, -1f, 0.2f), 0.22f, 0.1f, 0.04f, 0.9f, 0.1f, 14f, 8, -9f, -0.9f, 0.35f);
            g.Highlight("Top", -30f, 28f, 0.1f, HairGeometry.V(-0.5f, 0.2f, -1f), 0.14f, 0.04f);
            g.Highlight("Top", 40f, 32f, 0.1f, HairGeometry.V(0.5f, 0.2f, -1f), 0.12f, 0.035f);
            g.pivots["Bangs"] = g.hc + new Vector3(0f, g.hr.y * 0.75f, g.hr.z * 0.45f);
            g.pivots["Back"] = g.hc + new Vector3(0f, g.hr.y * 0.2f, -g.hr.z * 0.85f);
            g.pivots["SideL"] = g.hc + new Vector3(-g.hr.x * 0.8f, g.hr.y * 0.45f, 0.05f);
            g.pivots["SideR"] = g.hc + new Vector3(g.hr.x * 0.8f, g.hr.y * 0.45f, 0.05f);
        }

        static void StdPivots(HairGeometry g)
        {
            if (!g.pivots.ContainsKey("Bangs")) g.pivots["Bangs"] = g.hc + new Vector3(0f, g.hr.y * 0.75f, g.hr.z * 0.45f);
            if (!g.pivots.ContainsKey("Back")) g.pivots["Back"] = g.hc + new Vector3(0f, g.hr.y * 0.3f, -g.hr.z * 0.85f);
            if (!g.pivots.ContainsKey("SideL")) g.pivots["SideL"] = g.hc + new Vector3(-g.hr.x * 0.8f, g.hr.y * 0.45f, 0.05f);
            if (!g.pivots.ContainsKey("SideR")) g.pivots["SideR"] = g.hc + new Vector3(g.hr.x * 0.8f, g.hr.y * 0.45f, 0.05f);
        }

        /// <summary>Tobi — short and swept back by the wind: locks run from the hairline over the top and flick up at the back; two loose strands at the temples.</summary>
        static void SweptBack(HairGeometry g)
        {
            g.Cap("Top", HairGeometry.Shade, 0.035f, 60f, 100f, 138f, 5f);
            for (int i = 0; i < 13; i++)
            {
                float az = -84f + i * 14f + g.R(-4f, 4f);
                float pol = g.R(48f, 60f) - Mathf.Abs(az) * 0.05f;
                Vector3 flow = HairGeometry.V(Mathf.Sin(az * Mathf.Deg2Rad) * 0.25f, 0.35f, -1f);
                g.Lock("Back", i % 3 == 1 ? HairGeometry.Shade : HairGeometry.Base, az, pol, g.R(0.07f, 0.1f), flow,
                    g.R(0.42f, 0.52f), g.R(0.16f, 0.2f), 0.05f, 0.3f, 0.2f, g.R(-35f, -18f), 11, -9f, -0.35f, 0.25f);
            }
            for (int i = 0; i < 7; i++)
            {
                float az = 125f + i * 18f + g.R(-4f, 4f);
                g.Lock("Back", HairGeometry.Base, az, g.R(70f, 88f), 0.05f, HairGeometry.V(0f, -1f, -0.3f), g.R(0.16f, 0.22f), 0.15f, 0.045f, 0.5f, 0.3f, -35f, 8, -9f, -0.95f, 0.35f);
            }
            for (int s = -1; s <= 1; s += 2)
                g.Lock(s < 0 ? "SideL" : "SideR", HairGeometry.Base, s * 40f, 56f, 0.07f, HairGeometry.V(s * 0.3f, -1f, 0.4f), 0.24f, 0.08f, 0.03f, 0.9f, 0.05f, 20f, 9, 0.08f, -1f, 0.3f);
            g.Highlight("Back", -10f, 45f, 0.11f, HairGeometry.V(0f, 0.4f, -1f), 0.2f, 0.04f);
            g.Highlight("Back", 30f, 50f, 0.11f, HairGeometry.V(0f, 0.4f, -1f), 0.16f, 0.035f);
            g.pivots["Back"] = g.hc + new Vector3(0f, g.hr.y * 0.8f, 0f);
            StdPivots(g);
        }

        /// <summary>Bunta — close-cropped sides, the top combed back to a top-knot (the knot itself is built with the head).</summary>
        static void Cropped(HairGeometry g)
        {
            g.Cap("Top", HairGeometry.Shade, 0.02f, 58f, 96f, 132f, 8f);
            for (int i = 0; i < 9; i++)
            {
                float az = -48f + i * 12f;
                g.Lock("Top", i % 2 == 0 ? HairGeometry.Base : HairGeometry.Shade, az, 52f - Mathf.Abs(az) * 0.1f, 0.045f, HairGeometry.V(az * -0.004f, 0.3f, -1f),
                    0.36f, 0.14f, 0.035f, 0.1f, 0f, 0f, 10, -9f, -1f, 0f);
            }
            g.Highlight("Top", 0f, 40f, 0.07f, HairGeometry.V(0f, 0.3f, -1f), 0.2f, 0.04f);
            StdPivots(g);
        }

        /// <summary>Sayo — a high ponytail: the top gathers back to a tie at the crown and the tail falls down the back; light parted bangs and long face-framing strands.</summary>
        static void HighPonytail(HairGeometry g)
        {
            g.Cap("Top", HairGeometry.Shade, 0.03f, 62f, 100f, 140f, 3f, 0f);
            Vector3 tie = g.Surf(180f, 52f, 0.05f);
            // Gathered: everything flows toward the tie.
            for (int i = 0; i < 16; i++)
            {
                float az = -150f + i * 20f + g.R(-4f, 4f);
                float pol = Mathf.Abs(Mathf.DeltaAngle(az, 0f)) < 60f ? g.R(52f, 60f) : g.R(70f, 95f);
                Vector3 flow = (tie - g.Surf(az, pol, 0f)).normalized;
                g.Lock("Top", i % 3 == 0 ? HairGeometry.Shade : HairGeometry.Base, az, pol, 0.05f, flow, 0.4f, 0.17f, 0.04f, 0f, 0f, 0f, 10, -9f, -1f, 0f);
            }
            // The tail: up out of the tie, then falling with weight.
            g.taperStart = 0.5f;
            g.tipWidth = 0.1f;
            for (int i = 0; i < 9; i++)
            {
                float az = 180f + (i - 4f) * 2.5f;
                g.Lock("Fall", i % 3 == 1 ? HairGeometry.Shade : HairGeometry.Base, az, 52f + (i % 3) * 2f, 0.1f + (i % 2) * 0.03f, HairGeometry.V((i - 4f) * 0.08f, 0.35f, -1f),
                    g.R(0.95f, 1.1f), g.R(0.15f, 0.18f), 0.055f, 1.2f, 0.4f, g.R(10f, 25f), 16, -9f, 0.95f, 0.85f);
            }
            g.taperStart = 0.12f;
            g.tipWidth = 0f;
            float[] bangAz = { -30f, -12f, 14f, 32f };
            for (int i = 0; i < bangAz.Length; i++)
            {
                float az = bangAz[i];
                g.Lock("Bangs", HairGeometry.Base, az, 40f, 0.07f, HairGeometry.V(az < 0f ? -0.5f : 0.5f, -0.4f, 1f), g.R(0.22f, 0.28f), 0.12f, 0.035f, 0.5f, 0.02f, 18f, 9, 0.11f, -1f, 0.2f);
            }
            for (int s = -1; s <= 1; s += 2)
                g.Lock(s < 0 ? "SideL" : "SideR", HairGeometry.Base, s * 58f, 50f, 0.07f, HairGeometry.V(s * 0.2f, -1f, 0.3f), 0.55f, 0.08f, 0.03f, 1.2f, 0.02f, 15f, 12, -9f, -0.1f, 0f);
            g.Highlight("Top", 20f, 40f, 0.09f, (tie - g.Surf(20f, 40f, 0f)).normalized, 0.22f, 0.04f);
            g.Highlight("Top", -40f, 44f, 0.09f, (tie - g.Surf(-40f, 44f, 0f)).normalized, 0.2f, 0.035f);
            g.pivots["Fall"] = tie;
            StdPivots(g);
        }

        /// <summary>Nene — a round bob that curls under at the jaw, with a blunt straight fringe.</summary>
        static void RoundBob(HairGeometry g)
        {
            g.Cap("Top", HairGeometry.Shade, 0.03f, 60f, 102f, 142f, 0f);
            g.taperStart = 0.6f;
            g.tipWidth = 0.35f;
            for (int i = 0; i < 18; i++)
            {
                float az = 55f + i * 14f + g.R(-3f, 3f);
                Vector3 d = HairGeometry.Dir(az, 90f);
                g.Lock(Mathf.Abs(Mathf.DeltaAngle(az, 180f)) < 60f ? "Back" : az < 180f ? "SideR" : "SideL", i % 3 == 1 ? HairGeometry.Shade : HairGeometry.Base,
                    az, g.R(10f, 22f), g.R(0.09f, 0.11f), new Vector3(d.x, -0.6f, d.z), g.R(0.62f, 0.68f), 0.22f, 0.06f, 1f, 0.05f, 45f, 13, -9f, -0.1f, 0f);
            }
            // Blunt fringe: straight across, ending just above the brows.
            for (int i = 0; i < 7; i++)
            {
                float az = -45f + i * 15f;
                g.tipWidth = 0.75f;
                g.Lock("Bangs", i % 2 == 0 ? HairGeometry.Base : HairGeometry.Shade, az, 18f, 0.1f, HairGeometry.V(az * 0.004f, -0.2f, 1f), 0.55f, 0.2f, 0.05f, 0.5f, 0f, 10f, 12, 0.095f, -1f, 0.05f);
            }
            g.taperStart = 0.12f;
            g.tipWidth = 0f;
            g.Highlight("Top", 30f, 26f, 0.13f, HairGeometry.V(0.6f, -0.4f, 0.5f), 0.18f, 0.045f);
            g.Highlight("Top", -35f, 28f, 0.13f, HairGeometry.V(-0.6f, -0.4f, 0.5f), 0.15f, 0.04f);
            StdPivots(g);
        }

        /// <summary>Nagi — a deep hood with a hanging point (cloth), a few dark bangs under its rim and one long lock at the side of the face.</summary>
        static void Hooded(HairGeometry g)
        {
            g.Cap("Top", HairGeometry.Shade, 0.03f, 62f, 100f, 140f, 0f);
            for (int i = 0; i < 4; i++)
            {
                float az = -30f + i * 20f;
                g.Lock("Bangs", HairGeometry.Base, az, 50f, 0.06f, HairGeometry.V(0.4f, -0.4f, 1f), 0.22f, 0.12f, 0.035f, 0.6f, 0f, 20f, 8, 0.1f, -1f, 0.25f);
            }
            g.Lock("SideL", HairGeometry.Base, -55f, 55f, 0.07f, HairGeometry.V(-0.2f, -1f, 0.3f), 0.5f, 0.1f, 0.035f, 1.2f, 0.02f, 12f, 11, -9f, -0.1f, 0f);
            // The hood: a thick cloth shell framing the face.
            g.Cap("Hood", HairGeometry.Cloth, 0.2f, 70f, 110f, 165f, 0f);
            g.taperStart = 0.35f;
            g.Lock("Back", HairGeometry.Cloth, 180f, 30f, 0.2f, HairGeometry.V(0f, 0.2f, -1f), 0.55f, 0.34f, 0.07f, 0.45f, 0.1f, -10f, 10, -9f, -0.2f, 0.6f);
            g.taperStart = 0.12f;
            g.pivots["Back"] = g.hc + new Vector3(0f, g.hr.y * 0.6f, -g.hr.z * 0.9f);
            StdPivots(g);
        }

        /// <summary>Seiran — long hair combed sleekly back from a centre part (the long braid is built with the head), with two long strands framing the face.</summary>
        static void SleekLong(HairGeometry g)
        {
            g.Cap("Top", HairGeometry.Shade, 0.03f, 58f, 100f, 145f, 2f, 0f);
            for (int s = -1; s <= 1; s += 2)
                for (int i = 0; i < 8; i++)
                {
                    float az = s * (6f + i * 20f);
                    g.Lock("Top", i % 3 == 1 ? HairGeometry.Shade : HairGeometry.Base, az, 8f + i * 2f, 0.08f + (i % 2) * 0.02f, HairGeometry.V(s * 0.5f, -0.4f, -1f), 0.6f, 0.2f, 0.06f, 0.3f, 0f, 0f, 12, -9f, -1f, 0f);
                }
            g.taperStart = 0.5f;
            for (int s = -1; s <= 1; s += 2)
                g.Lock(s < 0 ? "SideL" : "SideR", HairGeometry.Base, s * 34f, 40f, 0.1f, HairGeometry.V(s * 1f, -0.3f, 0.2f), 0.9f, 0.11f, 0.04f, 1.2f, 0.02f, 12f, 16, -9f, -0.1f, 0f);
            g.taperStart = 0.12f;
            g.Highlight("Top", 20f, 25f, 0.08f, HairGeometry.V(0.5f, -0.3f, -1f), 0.22f, 0.04f);
            g.Highlight("Top", -20f, 25f, 0.08f, HairGeometry.V(-0.5f, -0.3f, -1f), 0.22f, 0.04f);
            StdPivots(g);
        }
    }
}
