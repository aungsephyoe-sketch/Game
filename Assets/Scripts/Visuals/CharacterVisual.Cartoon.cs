using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// The cartoon look for every rigged character: a bigger head with bigger eyes, a shorter and chunkier
    /// body, thicker arms and legs, oversized hands, boots and weapon, and a livelier idle. Applied once after a
    /// character is built, on top of their own proportions, so everyone stays recognisable — just pushed toward
    /// a stylised mobile-action look (large head, compact body, strong silhouette).
    /// strength 1 = slayers and villagers; demons get a milder version so they stay menacing.
    /// </summary>
    public partial class CharacterVisual
    {
        /// <summary>
        /// Soft, squishy look: bodies use gentle shading steps and no hard shine; the weapon keeps crisp edges and
        /// its glint. Adds a springy squash-and-stretch bounce.
        /// </summary>
        void SoftenLook()
        {
            if (SwordPivot != null)
            {
                var pb = new MaterialPropertyBlock();
                foreach (var r in SwordPivot.GetComponentsInChildren<Renderer>(true))
                {
                    r.GetPropertyBlock(pb);
                    pb.SetFloat("_Crisp", 1f);
                    r.SetPropertyBlock(pb);
                }
            }
            if (Model != null && GetComponent<SquishBounce>() == null) gameObject.AddComponent<SquishBounce>().Init(this);
            LivelyFace();
            HeroFace();
        }

        /// <summary>Livelier cartoon faces: a big sparkle in every eye (it blinks with the eye) and rosy cheeks.</summary>
        void LivelyFace()
        {
            if (head == null) return;
            var sparkle = MaterialFactory.Toon(Color.white, 0f, new Color(0.95f, 0.95f, 0.95f));
            var blush = MaterialFactory.Transparent(new Color(1f, 0.42f, 0.5f, 0.42f));
            foreach (var lid in head.GetComponentsInChildren<EyeLid>(true))
            {
                var e = lid.transform;
                if (e.childCount == 0) continue;
                // The first part of each eye is its white: its size is the eye's size.
                Vector3 sc = e.GetChild(0).localScale;
                float w = Mathf.Max(0.02f, sc.x), h = Mathf.Max(0.02f, sc.y);
                if (e.GetComponentsInChildren<MeshRenderer>(true).Length < 6)
                {
                    // Simple eyes (villagers): add sparkles. The detailed hero eyes carry their own highlights.
                    Ball(e, new Vector3(-w * 0.2f, h * 0.24f, 0.014f), new Vector3(w * 0.36f, h * 0.32f, 0.018f), sparkle);
                    Ball(e, new Vector3(w * 0.2f, h * 0.02f, 0.014f), new Vector3(w * 0.12f, h * 0.11f, 0.016f), sparkle);
                }
                var cheek = MeshFactory.MeshObject(MeshFactory.SmoothSphere(), e, new Vector3(0f, -h * 1.05f, -0.008f), new Vector3(w * 0.95f, h * 0.42f, 0.01f), blush, false);
                cheek.name = "Cheek";
            }
        }

        /// <summary>
        /// Bold hero faces in the chunky mobile-hero style: thick, confident brows tilted down toward the nose and
        /// a wider mouth, so every slayer reads as determined and full of attitude even at a distance. Works on the
        /// face parts each design already has (brows = the highest off-centre pair above the eyes).
        /// </summary>
        void HeroFace()
        {
            if (head == null) return;
            var lids = head.GetComponentsInChildren<EyeLid>(true);
            if (lids.Length == 0) return;
            float eyeY = lids[0].transform.localPosition.y;
            var brows = new System.Collections.Generic.List<Transform>();
            Transform mouth = null;
            float lowY = float.MaxValue;
            foreach (Transform f in head)
            {
                if (f.name != "Face" || f.GetComponentInChildren<EyeLid>() != null) continue;
                Vector3 p = f.localPosition;
                if (Mathf.Abs(p.x) > 0.04f && p.y > eyeY + 0.04f) brows.Add(f);
                if (Mathf.Abs(p.x) < 0.03f && p.y < lowY) { lowY = p.y; mouth = f; }
            }
            brows.Sort((a, b) => b.localPosition.y.CompareTo(a.localPosition.y));
            for (int i = 0; i < brows.Count && i < 2; i++)
            {
                var b = brows[i];
                b.localScale = Vector3.Scale(b.localScale, new Vector3(1.15f, 1.3f, 1.15f));
                b.localPosition += new Vector3(0f, 0f, 0.004f);
            }
            if (mouth != null) mouth.localScale = Vector3.Scale(mouth.localScale, new Vector3(1.2f, 1.1f, 1f));
        }

        void CartoonProportions(float strength)
        {
            if (!GameConfig.CartoonStyle || rig == null || strength <= 0f) return;
            var r = rig;
            // Target proportions from the slayer's shape language (monsters use the neutral set at lower strength).
            float tLeg = 0.78f, tTorsoY = 0.88f, tTorsoXZ = 1.17f, tLimb = 1.42f, tHead = 1.42f, tHand = 1.52f, tFoot = 1.42f, tWeapon = 1.4f, tPelvis = 1.17f;
            if (shapeSet && strength >= 1f)
                switch (Shape)
                {
                    // Square: wide and heavy — broad shoulders, thick limbs, huge hands and boots, short legs.
                    case ShapeLanguage.Square: tLeg = 0.74f; tTorsoY = 0.84f; tTorsoXZ = 1.3f; tLimb = 1.58f; tHead = 1.34f; tHand = 1.72f; tFoot = 1.55f; tWeapon = 1.5f; tPelvis = 1.2f; break;
                    // Triangle: a V — wide shoulders over a narrow waist, longer legs, big pointed weapon.
                    case ShapeLanguage.Triangle: tLeg = 0.84f; tTorsoY = 0.84f; tTorsoXZ = 1.22f; tLimb = 1.34f; tHead = 1.42f; tHand = 1.5f; tFoot = 1.4f; tWeapon = 1.52f; tPelvis = 0.95f; break;
                    // Circle: round and cuddly — the biggest head, a short round body, soft chunky limbs.
                    case ShapeLanguage.Circle: tLeg = 0.72f; tTorsoY = 0.8f; tTorsoXZ = 1.24f; tLimb = 1.42f; tHead = 1.52f; tHand = 1.5f; tFoot = 1.45f; tWeapon = 1.36f; tPelvis = 1.22f; break;
                    // Diamond: tall and elegant — slim limbs, a narrow waist, long weapon.
                    case ShapeLanguage.Diamond: tLeg = 0.86f; tTorsoY = 0.86f; tTorsoXZ = 1.1f; tLimb = 1.26f; tHead = 1.45f; tHand = 1.42f; tFoot = 1.32f; tWeapon = 1.48f; tPelvis = 0.98f; break;
                }
            float legK = Mathf.Lerp(1f, tLeg, strength);
            float torsoY = Mathf.Lerp(1f, tTorsoY, strength), torsoXZ = Mathf.Lerp(1f, tTorsoXZ, strength);
            float limb = Mathf.Lerp(1f, tLimb, strength);
            float headK = Mathf.Lerp(1f, tHead, strength);
            float handK = Mathf.Lerp(1f, tHand, strength), footK = Mathf.Lerp(1f, tFoot, strength);
            float eyeK = Mathf.Lerp(1f, 1.24f, strength), weaponK = Mathf.Lerp(1f, tWeapon, strength);
            float pelvisK = Mathf.Lerp(1f, tPelvis, strength);

            // Legs: shorter and thicker (the IK uses the same numbers, so feet still land on the ground).
            r.hipY *= legK; r.legA *= legK; r.legB *= legK;
            r.hipW *= pelvisK; r.stance *= Mathf.Max(pelvisK, 1f);
            for (int i = 0; i < 2; i++)
            {
                if (r.thigh[i] != null) r.thigh[i].localScale = Vector3.Scale(r.thigh[i].localScale, new Vector3(limb, legK, limb));
                if (r.shin[i] != null) r.shin[i].localScale = Vector3.Scale(r.shin[i].localScale, new Vector3(limb, legK, limb));
                if (r.foot[i] != null) r.foot[i].localScale *= footK;
                if (r.upper[i] != null) r.upper[i].localScale = Vector3.Scale(r.upper[i].localScale, new Vector3(limb, 1f, limb));
                if (r.lower[i] != null) r.lower[i].localScale = Vector3.Scale(r.lower[i].localScale, new Vector3(limb, 1f, limb));
                if (r.hand[i] != null) r.hand[i].localScale *= handK;
            }
            // Torso: a little shorter and broader; shoulders, neck and resting hands follow it.
            if (r.torso != null) r.torso.localScale = Vector3.Scale(r.torso.localScale, new Vector3(torsoXZ, torsoY, torsoXZ));
            if (r.pelvis != null) r.pelvis.localScale = Vector3.Scale(r.pelvis.localScale, new Vector3(pelvisK, 1f, pelvisK));
            r.shoulder = Vector3.Scale(r.shoulder, new Vector3(torsoXZ, torsoY, torsoXZ));
            r.neck = new Vector3(r.neck.x, r.neck.y * torsoY, r.neck.z);
            r.restHandR = Vector3.Scale(r.restHandR, new Vector3(torsoXZ, torsoY, 1f));
            r.restHandL = Vector3.Scale(r.restHandL, new Vector3(torsoXZ, torsoY, 1f));
            // Head and eyes: the big readable face — a softer mochi shape (a little wider and flatter), not a ball.
            if (head != null)
            {
                head.localScale = Vector3.Scale(head.localScale, new Vector3(headK * 1.06f, headK * 0.9f, headK * 0.98f));
                foreach (var lid in head.GetComponentsInChildren<EyeLid>(true)) lid.transform.localScale *= eyeK;
            }
            // Oversized weapon.
            if (SwordPivot != null) SwordPivot.localScale *= weaponK;
            // Livelier idle: a visible breath-bounce and weight shift.
            r.idleBounce = Mathf.Max(r.idleBounce, 0.012f * strength);
            r.weightShift = Mathf.Max(r.weightShift, 0.008f * strength) * 1.2f;
            r.swingDip *= 1f + 0.5f * strength;
            r.swingLunge *= 1f + 0.4f * strength;
            // Bigger, more readable poses: the stance, lean and showcase pose are pushed further.
            r.poseBody *= 1f + 0.5f * strength;
            r.poseHead *= 1f + 0.4f * strength;
            r.lean *= 1f + 0.3f * strength;
            r.crouch *= 1f + 0.3f * strength;
        }
    }
}
