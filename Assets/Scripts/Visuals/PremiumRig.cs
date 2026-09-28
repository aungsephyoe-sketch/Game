using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// Limb rig for the premium character builder. <see cref="CharacterVisual"/> keeps driving the whole body
    /// (bob, lean, squash, swings, poses); this runs afterwards each frame and moves the jointed parts to match:
    ///  • legs walk with a real stride, feet stay planted on the ground and knees bend (two-bone IK),
    ///  • the weapon hand follows the blade through every swing, and the other hand grips, mirrors or casts
    ///    depending on the fighter,
    ///  • hips, chest and head each move a little on their own (weight shift, twist into swings, breathing),
    ///  • personality numbers (crouch, lean, tempo, bounce, hover) make each fighter stand and move differently.
    /// </summary>
    public class PremiumRig : MonoBehaviour
    {
        public enum Grip { OneHand, TwoHand, Twin, Caster }

        public CharacterVisual cv;
        public Transform body, pelvis, torso, head;
        public readonly Transform[] upper = new Transform[2], lower = new Transform[2], hand = new Transform[2];
        public readonly Transform[] thigh = new Transform[2], shin = new Transform[2], foot = new Transform[2];

        // Proportions (body space; index 0 = right, 1 = left).
        public Vector3 shoulder = new Vector3(0.19f, 0.3f, 0f);
        public Vector3 neck = new Vector3(0f, 0.36f, 0f);
        public float armA = 0.2f, armB = 0.2f, legA = 0.27f, legB = 0.27f, ankle = 0.08f;
        public float hipY = 0.6f, hipW = 0.085f, stance = 0.11f, toeOut = 8f;

        // Personality.
        public float crouch, lean, stride = 0.16f, lift = 0.12f, tempo = 11f;
        public float idleBounce, idleBounceFreq = 2f, weightShift, hover, twistGain = 0.25f, swingDip = 0.04f, swingLunge = 6f;
        public Grip grip = Grip.OneHand;
        /// <summary>Where the hands rest when the weapon isn't swinging (torso space).</summary>
        public Vector3 restHandR = new Vector3(0.24f, 0.02f, 0.14f), restHandL = new Vector3(-0.24f, 0.02f, 0.12f);
        public Transform castGlow;
        /// <summary>Where the second hand holds the weapon for two-handed grips (metres along the blade from the first hand).</summary>
        public float handleOffset = -0.13f;

        // Showcase pose (team line-up, portraits), designed for the jointed body.
        public bool hasPose;
        public Vector3 poseBody, poseWeapon, poseHead;
        public float poseDrop;

        float phase, move, sprint, time, tuck;

        void LateUpdate() { Solve(Time.deltaTime); }

        public void Solve(float dt)
        {
            if (cv == null || body == null || pelvis == null || torso == null) return;
            time += dt;
            bool dead = cv.IsDead;
            float k = dt <= 0f ? 1f : 1f - Mathf.Exp(-dt * 10f);
            move = Mathf.Lerp(move, dead ? 0f : cv.MoveAmount, k);
            sprint = Mathf.Lerp(sprint, cv.IsSprinting ? 1f : 0f, k);
            float m = Mathf.Clamp01(move * 1.4f);
            float idle = 1f - m;
            phase += dt * tempo * (1f + 0.4f * sprint) * m;
            float sw = cv.SwingWeight;
            bool guard = cv.IsGuarding;
            // Showcase poses (line-up, portraits, victory) keep the elbows bent and the hands near the body,
            // so a held weapon reads as a relaxed stance rather than an arm stretched out mid-swing.
            bool posing = cv.Posing && !dead;
            if (posing) sw = Mathf.Min(sw, 0.65f);

            // Airborne (dodges, leaps, victory hops): tuck the legs.
            bool airborne = cv.Model.localPosition.y > 0.18f || cv.transform.localPosition.y > 0.3f;
            tuck = Mathf.Lerp(tuck, airborne && !dead ? 1f : 0f, k);

            // Float (casters), hips and weight.
            body.localPosition = hover > 0f ? new Vector3(0f, hover + Mathf.Sin(time * 1.7f) * 0.05f, 0f) : Vector3.zero;
            float dip = crouch * idle + (guard ? 0.05f : 0f) + swingDip * sw
                        + Mathf.Abs(Mathf.Sin(time * idleBounceFreq * Mathf.PI)) * idleBounce * idle
                        + Mathf.Abs(Mathf.Sin(phase)) * 0.035f * m;
            float shift = Mathf.Sin(time * 0.9f) * weightShift * idle;
            pelvis.localPosition = new Vector3(shift, hipY - dip, 0f);
            pelvis.localRotation = Quaternion.Euler(0f, -Mathf.Sin(phase) * 10f * m, Mathf.Sin(phase) * 3f * m + shift * 60f);

            // Chest: twist into the swing, lean into the run, breathe.
            Vector3 d = cv.SwordPivot != null ? cv.SwordPivot.localRotation * Vector3.forward : Vector3.forward;
            float yaw = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
            float twist = Mathf.Clamp(yaw * twistGain * sw, -35f, 35f) + Mathf.Sin(phase) * 12f * m;
            float pitch = lean * idle + (6f + 8f * sprint) * m + swingLunge * sw + Mathf.Sin(time * 2f) * 1.2f + (guard ? 6f : 0f);
            if (dead) pitch += 12f;
            torso.localPosition = pelvis.localPosition + Vector3.up * 0.02f;
            torso.localRotation = Quaternion.Euler(pitch, twist, -shift * 40f);
            if (head != null) head.localPosition = torso.localPosition + torso.localRotation * neck;

            // ---- Arms
            Vector3 sR = torso.localPosition + torso.localRotation * shoulder;
            Vector3 sL = torso.localPosition + torso.localRotation * new Vector3(-shoulder.x, shoulder.y, shoulder.z);
            float reach = (armA + armB) * (posing ? 0.62f : 0.92f);
            Vector3 sd = d;
            if (sd.z < 0f) sd += Vector3.forward * (-sd.z) * 0.9f; // keep the hand in front when the blade points back
            sd = sd.sqrMagnitude > 0.0001f ? sd.normalized : Vector3.forward;
            Vector3 walk = Vector3.forward * Mathf.Sin(phase) * 0.12f * m;
            Vector3 back = new Vector3(0f, 0.06f, -0.3f) * sprint * m;
            Vector3 restR = torso.localPosition + torso.localRotation * restHandR;
            Vector3 restL = torso.localPosition + torso.localRotation * restHandL;
            if (grip == Grip.Twin) { restR += back; restL += back; }
            Vector3 tR = Vector3.Lerp(restR - walk, sR + sd * reach, sw);
            // Raised hands go out to the side and a little forward so they never end up inside the big head.
            float upR = Mathf.Clamp01(sd.y) * sw;
            tR.x = Mathf.Max(tR.x, shoulder.x + 0.22f * upR);
            tR.z += 0.12f * upR;
            Vector3 hR = TwoBone(upper[0], lower[0], sR, tR, armA, armB, new Vector3(0.7f, -0.2f, -1f), Vector3.forward);
            if (hand[0] != null) { hand[0].localPosition = hR; hand[0].localRotation = lower[0].localRotation; }
            if (cv.SwordPivot != null) cv.SwordPivot.localPosition = hR + lower[0].localRotation * new Vector3(0f, -0.055f, 0.01f);

            Vector3 tL;
            switch (grip)
            {
                case Grip.Twin:
                {
                    Vector3 dm = new Vector3(-sd.x, sd.y, sd.z);
                    tL = Vector3.Lerp(restL + walk, sL + dm * reach, sw);
                    tL.x = Mathf.Min(tL.x, -shoulder.x - 0.22f * upR);
                    tL.z += 0.12f * upR;
                    break;
                }
                case Grip.TwoHand:
                {
                    Vector3 onHandle = (cv.SwordPivot != null ? cv.SwordPivot.localPosition : hR) + d * handleOffset;
                    tL = Vector3.Lerp(restL + walk, onHandle, Mathf.Max(posing ? 0f : sw, guard ? 1f : 0f));
                    break;
                }
                case Grip.Caster:
                {
                    Vector3 circle = new Vector3(Mathf.Sin(time * 1.3f), Mathf.Cos(time * 1.1f), 0f) * 0.025f;
                    Vector3 thrust = sL + new Vector3(0.05f, 0.08f, 1f).normalized * reach;
                    tL = Vector3.Lerp(restL + circle + walk * 0.5f, thrust, Mathf.Max(sw, guard ? 0.7f : 0f));
                    break;
                }
                default:
                    tL = restL + walk;
                    if (guard) tL = sL + new Vector3(0.1f, 0.05f, 0.35f);
                    break;
            }
            if (tuck > 0.01f && grip != Grip.TwoHand) tL = Vector3.Lerp(tL, sL + new Vector3(-0.12f, 0.2f, 0.1f), tuck * 0.7f);
            Vector3 hL = TwoBone(upper[1], lower[1], sL, tL, armA, armB, new Vector3(-0.7f, -0.2f, -1f), Vector3.forward);
            if (hand[1] != null) { hand[1].localPosition = hL; hand[1].localRotation = lower[1].localRotation; }
            if (castGlow != null) castGlow.localScale = Vector3.one * (0.1f + 0.05f * Mathf.Sin(time * 5f) + 0.12f * sw);

            // ---- Legs
            bool plant = !dead && hover <= 0f && tuck < 0.5f && Vector3.Angle(cv.Model.up, cv.transform.up) < 30f;
            Vector3 fwdB = body.InverseTransformDirection(cv.transform.forward);
            Vector3 upB = body.InverseTransformDirection(cv.transform.up);
            float scale = cv.Model.lossyScale.y / Mathf.Max(0.0001f, cv.transform.lossyScale.y);
            for (int i = 0; i < 2; i++)
            {
                if (thigh[i] == null) continue;
                float s = i == 0 ? 1f : -1f;
                Vector3 hip = pelvis.localPosition + pelvis.localRotation * new Vector3(s * hipW, -0.02f, 0f);
                float ph = phase + (i == 0 ? 0f : Mathf.PI);
                float strideNow = stride * (1f + 0.5f * sprint);
                float stepZ = Mathf.Sin(ph) * strideNow * m;
                float up = Mathf.Max(0f, Mathf.Cos(ph)) * lift * m * (1f + 0.5f * sprint);
                Vector3 fb = new Vector3(s * stance, ankle + up, stepZ + 0.01f);
                if (plant)
                {
                    // Pin the foot to the ground under the character, whatever the body is doing above it.
                    Vector3 rp = cv.transform.InverseTransformPoint(body.TransformPoint(fb));
                    rp.y = (ankle + up) * scale;
                    fb = body.InverseTransformPoint(cv.transform.TransformPoint(rp));
                }
                else if (hover > 0f)
                {
                    // Floating: legs hang loose with pointed toes.
                    fb = hip + new Vector3(s * 0.015f, -(legA + legB) * 0.93f, (i == 0 ? 0.06f : -0.02f) + stepZ * 0.3f);
                }
                if (tuck > 0.01f) fb = Vector3.Lerp(fb, hip + new Vector3(s * 0.04f, -(legA + legB) * 0.55f, 0.14f), tuck);
                Vector3 hF = TwoBone(thigh[i], shin[i], hip, fb, legA, legB, new Vector3(0.05f * s, 0f, 1f), Vector3.forward);
                if (foot[i] == null) continue;
                foot[i].localPosition = hF;
                if (plant)
                {
                    float toe = -Mathf.Max(0f, Mathf.Cos(ph)) * 16f * m;
                    foot[i].localRotation = Quaternion.LookRotation(fwdB, upB) * Quaternion.Euler(toe, s * toeOut, 0f);
                }
                else foot[i].localRotation = shin[i].localRotation * Quaternion.Euler(hover > 0f ? 45f : 10f + tuck * 30f, 0f, 0f);
            }
        }

        /// <summary>
        /// Two-bone IK in the parent's space: places the upper segment at <paramref name="S"/>, bends the middle joint
        /// toward <paramref name="pole"/> and returns where the end (hand / ankle) lands. Segments hang along −Y.
        /// </summary>
        static Vector3 TwoBone(Transform a, Transform b, Vector3 S, Vector3 T, float la, float lb, Vector3 pole, Vector3 hint)
        {
            Vector3 st = T - S;
            float dist = st.magnitude;
            Vector3 dir = dist > 0.0001f ? st / dist : Vector3.down;
            dist = Mathf.Clamp(dist, Mathf.Abs(la - lb) + 0.02f, la + lb - 0.002f);
            float cosA = Mathf.Clamp((la * la + dist * dist - lb * lb) / (2f * la * dist), -1f, 1f);
            float sinA = Mathf.Sqrt(Mathf.Max(0f, 1f - cosA * cosA));
            Vector3 perp = pole - dir * Vector3.Dot(pole, dir);
            if (perp.sqrMagnitude < 0.000001f) perp = Vector3.Cross(dir, Vector3.right);
            perp.Normalize();
            Vector3 E = S + (dir * cosA + perp * sinA) * la;
            Vector3 H = S + dir * dist;
            if (a != null) { a.localPosition = S; a.localRotation = Orient(E - S, hint); }
            if (b != null) { b.localPosition = E; b.localRotation = Orient(H - E, hint); }
            return H;
        }

        /// <summary>Rotation whose −Y runs along <paramref name="seg"/> and whose +Z faces <paramref name="hint"/> as far as it can.</summary>
        static Quaternion Orient(Vector3 seg, Vector3 hint)
        {
            Vector3 up = -seg.normalized;
            Vector3 f = Vector3.ProjectOnPlane(hint, up);
            if (f.sqrMagnitude < 0.0001f) f = Vector3.ProjectOnPlane(Vector3.up, up);
            if (f.sqrMagnitude < 0.0001f) f = Vector3.ProjectOnPlane(Vector3.right, up);
            return Quaternion.LookRotation(f.normalized, up);
        }
    }
}
