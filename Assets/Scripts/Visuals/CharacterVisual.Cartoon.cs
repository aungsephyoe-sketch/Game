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
        }

        void CartoonProportions(float strength)
        {
            if (!GameConfig.CartoonStyle || rig == null || strength <= 0f) return;
            var r = rig;
            float legK = Mathf.Lerp(1f, 0.84f, strength);
            float torsoY = Mathf.Lerp(1f, 0.9f, strength), torsoXZ = Mathf.Lerp(1f, 1.12f, strength);
            float limb = Mathf.Lerp(1f, 1.32f, strength);
            float headK = Mathf.Lerp(1f, 1.3f, strength);
            float handK = Mathf.Lerp(1f, 1.38f, strength), footK = Mathf.Lerp(1f, 1.3f, strength);
            float eyeK = Mathf.Lerp(1f, 1.2f, strength), weaponK = Mathf.Lerp(1f, 1.3f, strength);

            // Legs: shorter and thicker (the IK uses the same numbers, so feet still land on the ground).
            r.hipY *= legK; r.legA *= legK; r.legB *= legK;
            r.hipW *= torsoXZ; r.stance *= torsoXZ;
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
            if (r.pelvis != null) r.pelvis.localScale = Vector3.Scale(r.pelvis.localScale, new Vector3(torsoXZ, 1f, torsoXZ));
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
        }
    }
}
