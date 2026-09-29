using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// Character-card posters: each slayer gets an expression and a pose of their own for their card art — a
    /// smile, a smirk, a confident grin, an angry shout, a big happy laugh or a determined frown, and a hero stance,
    /// a charge, a cool lean or a playful tilt — chosen from their personality with a personal twist, so the
    /// roster doesn't all look the same. Applied to the freshly built model right before its snapshot.
    /// </summary>
    public partial class CharacterVisual
    {
        public enum PosterMood { Smile, Smirk, Confident, Angry, Happy, Determined }

        public static PosterMood PosterMoodFor(CharacterDefinition def)
        {
            bool alt = (IdSeed(def.id) & 1) == 1;
            switch (def.motion)
            {
                case MotionStyle.Aggressive: return alt ? PosterMood.Angry : PosterMood.Confident;
                case MotionStyle.Confident: return alt ? PosterMood.Smirk : PosterMood.Confident;
                case MotionStyle.Sly: return alt ? PosterMood.Smirk : PosterMood.Confident;
                case MotionStyle.Light: return alt ? PosterMood.Happy : PosterMood.Smile;
                case MotionStyle.Nervous: return alt ? PosterMood.Smile : PosterMood.Happy;
                case MotionStyle.Graceful: return alt ? PosterMood.Smirk : PosterMood.Smile;
                case MotionStyle.Stoic: return alt ? PosterMood.Angry : PosterMood.Determined;
                default: return alt ? PosterMood.Confident : PosterMood.Smile;
            }
        }

        /// <summary>Poster pose and face (call after <see cref="ApplyBannerPose"/>).</summary>
        public void ApplyPosterLook(CharacterDefinition def)
        {
            if (def == null) return;
            int pose = (IdSeed(def.id) >> 3) & 3;
            PosterPose(def, pose);
            PosterFace(PosterMoodFor(def));
        }

        void PosterPose(CharacterDefinition def, int pose)
        {
            bool blade = def.weapon != WeaponKind.Staff && def.weapon != WeaponKind.Bow && def.weapon != WeaponKind.Fans && def.weapon != WeaponKind.Fists;
            Vector3 body, headE, wpn;
            float drop;
            switch (pose)
            {
                case 0: body = new Vector3(-7f, 0f, 0f); headE = new Vector3(-9f, 0f, 0f); wpn = new Vector3(-130f, 25f, 0f); drop = 0f; break;     // hero stance, weapon up
                case 1: body = new Vector3(13f, -10f, 0f); headE = new Vector3(4f, 10f, 0f); wpn = new Vector3(-12f, 60f, 0f); drop = 0.07f; break;  // charging in, weapon low
                case 2: body = new Vector3(0f, 16f, -6f); headE = new Vector3(0f, -12f, 9f); wpn = new Vector3(-150f, 35f, 0f); drop = 0f; break;    // cool lean, weapon on shoulder
                default: body = new Vector3(4f, -8f, 8f); headE = new Vector3(0f, 12f, -11f); wpn = new Vector3(-70f, -30f, 0f); drop = 0.02f; break; // playful tilt
            }
            Model.localRotation = Model.localRotation * Quaternion.Euler(body);
            Model.localPosition += Vector3.down * drop;
            if (head != null) head.localRotation = head.localRotation * Quaternion.Euler(headE);
            if (blade && SwordPivot != null) SwordPivot.localRotation = Quaternion.Euler(wpn);
            if (rig != null) rig.Solve(0f);
        }

        void PosterFace(PosterMood mood)
        {
            if (head == null) return;
            var lids = head.GetComponentsInChildren<EyeLid>(true);
            float eyeY = lids.Length > 0 ? lids[0].transform.localPosition.y : 0f;
            Transform mouth = null;
            float lowY = float.MaxValue;
            var brows = new System.Collections.Generic.List<Transform>();
            foreach (Transform f in head)
            {
                if (f.name != "Face" || f.GetComponentInChildren<EyeLid>() != null) continue;
                Vector3 p = f.localPosition;
                if (Mathf.Abs(p.x) < 0.03f && p.y < lowY) { lowY = p.y; mouth = f; }
                if (Mathf.Abs(p.x) > 0.04f && p.y > eyeY + 0.04f) brows.Add(f);
            }
            brows.Sort((a, b) => b.localPosition.y.CompareTo(a.localPosition.y));

            // Brows: tilt (inner ends down = angry) and lift; the smirk raises just one.
            float tilt = 0f, lift = 0f;
            switch (mood)
            {
                case PosterMood.Angry: tilt = -24f; lift = -0.012f; break;
                case PosterMood.Determined: tilt = -14f; lift = -0.006f; break;
                case PosterMood.Confident: tilt = -8f; break;
                case PosterMood.Happy: lift = 0.014f; tilt = 6f; break;
                case PosterMood.Smile: lift = 0.006f; break;
            }
            for (int i = 0; i < brows.Count && i < 2; i++)
            {
                var b = brows[i];
                float side = Mathf.Sign(b.localPosition.x);
                float l = lift + (mood == PosterMood.Smirk && side > 0f ? 0.018f : 0f);
                float t = tilt + (mood == PosterMood.Smirk ? (side > 0f ? 10f : -12f) : 0f);
                b.localPosition += Vector3.up * l;
                b.localRotation = b.localRotation * Quaternion.Euler(0f, 0f, side * t);
            }
            // Eyes: happy squeezes them into arcs, angry and determined narrow them, the smirk half-closes one.
            foreach (var lid in lids)
            {
                var e = lid.transform;
                float side = Mathf.Sign(e.localPosition.x);
                float k = 1f;
                if (mood == PosterMood.Happy) k = 0.28f;
                else if (mood == PosterMood.Angry || mood == PosterMood.Determined) k = 0.78f;
                else if (mood == PosterMood.Confident) k = 0.86f;
                else if (mood == PosterMood.Smirk && side < 0f) k = 0.62f;
                e.localScale = new Vector3(e.localScale.x, e.localScale.y * k, e.localScale.z);
            }
            // Mouth: swap the everyday mouth for the poster one.
            if (mouth == null) return;
            foreach (var r in mouth.GetComponentsInChildren<Renderer>(true)) r.enabled = false;
            var mo = J("PosterMouth", head, mouth.localPosition);
            mo.localRotation = mouth.localRotation;
            mo.localScale = mouth.localScale;
            var ink = PM(new Color(0.3f, 0.08f, 0.1f), 0f);
            var teeth = PM(new Color(0.99f, 0.98f, 0.96f), 0f);
            var tongue = PM(new Color(0.95f, 0.45f, 0.48f), 0f);
            switch (mood)
            {
                case PosterMood.Smile:
                    Part(PrimitiveType.Capsule, mo, new Vector3(0f, -0.006f, 0.002f), new Vector3(0.012f, 0.016f, 0.01f), ink, new Vector3(0f, 0f, 90f));
                    for (int s = -1; s <= 1; s += 2)
                        Part(PrimitiveType.Capsule, mo, new Vector3(s * 0.026f, 0.002f, 0.002f), new Vector3(0.012f, 0.018f, 0.01f), ink, new Vector3(0f, 0f, 90f + s * 35f));
                    break;
                case PosterMood.Smirk:
                    Part(PrimitiveType.Capsule, mo, new Vector3(0.008f, 0f, 0.002f), new Vector3(0.012f, 0.034f, 0.01f), ink, new Vector3(0f, 0f, 112f));
                    Part(PrimitiveType.Capsule, mo, new Vector3(0.042f, 0.012f, 0.002f), new Vector3(0.009f, 0.012f, 0.009f), ink, new Vector3(0f, 0f, 150f));
                    break;
                case PosterMood.Confident:
                    Ball(mo, new Vector3(0f, -0.002f, -0.004f), new Vector3(0.1f, 0.042f, 0.02f), ink, new Vector3(0f, 0f, 5f));
                    Ball(mo, new Vector3(0f, 0.006f, 0.001f), new Vector3(0.088f, 0.02f, 0.016f), teeth, new Vector3(0f, 0f, 5f));
                    break;
                case PosterMood.Angry:
                    Ball(mo, new Vector3(0f, -0.008f, -0.004f), new Vector3(0.082f, 0.066f, 0.02f), ink);
                    Ball(mo, new Vector3(0f, 0.016f, 0.001f), new Vector3(0.064f, 0.016f, 0.016f), teeth);
                    Ball(mo, new Vector3(0f, -0.034f, 0.001f), new Vector3(0.05f, 0.012f, 0.016f), teeth);
                    break;
                case PosterMood.Happy:
                    Ball(mo, new Vector3(0f, -0.01f, -0.004f), new Vector3(0.09f, 0.064f, 0.02f), ink);
                    Ball(mo, new Vector3(0f, 0.014f, 0.001f), new Vector3(0.074f, 0.016f, 0.016f), teeth);
                    Ball(mo, new Vector3(0f, -0.026f, 0.002f), new Vector3(0.05f, 0.026f, 0.016f), tongue);
                    break;
                default: // determined: a firm line, corners pulled down
                    Part(PrimitiveType.Capsule, mo, new Vector3(0f, 0f, 0.002f), new Vector3(0.012f, 0.024f, 0.01f), ink, new Vector3(0f, 0f, 90f));
                    for (int s = -1; s <= 1; s += 2)
                        Part(PrimitiveType.Capsule, mo, new Vector3(s * 0.03f, -0.006f, 0.002f), new Vector3(0.011f, 0.012f, 0.01f), ink, new Vector3(0f, 0f, 90f - s * 35f));
                    break;
            }
        }
    }
}
