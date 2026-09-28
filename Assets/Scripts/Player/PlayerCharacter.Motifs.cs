using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// Strong attacks (the skill buttons) get a finishing touch that belongs to one slayer only, so no two
    /// characters' moves look alike even when the underlying form (dash, spin, wave...) is similar, plus a
    /// rarity flourish: Epic adds a shockwave, Legendary a pillar of light and a camera punch, Mythic a ripple
    /// through every rarity colour.
    /// </summary>
    public partial class PlayerCharacter
    {
        void SkillMotifStart(int index)
        {
            int stars = Owned != null ? Owned.stars : 2;
            Color c = ElementColor;
            if (stars >= 4) VFX.Shockwave(Position, 2.5f * RarityFx, Color.Lerp(c, Color.white, 0.3f), 0.35f);
            if (stars >= 5)
            {
                VFX.Pillar(Position, c, 6f * RarityFx, 0.35f);
                if (CameraController.Instance != null) CameraController.Instance.Punch(0.35f, 0.15f);
            }
            if (stars >= 6)
                for (int tier = 2; tier <= 6; tier++) VFX.Shockwave(Position, 1.2f + tier * 0.6f, RarityInfo.Color(tier), 0.3f + tier * 0.04f);
        }

        void SkillMotifEnd(int index)
        {
            float fx = RarityFx;
            Vector3 p = Position, f = transform.forward;
            Vector3 ahead = p + f * 2.5f;
            var near = Foes(ahead, 6f);
            Vector3 tgt = near.Count > 0 && near[0] != null ? near[0].Position : ahead;
            switch (Def.baseId)
            {
                case "ren":
                    if (Def.id == "ren_sundance")
                    {
                        // Dawn: a small sun flares behind the blade.
                        VFX.BurstDisc(p + f, 1.8f * fx, new Color(1f, 0.85f, 0.4f), 0.3f);
                        for (int i = 0; i < 6; i++) VFX.Flash(MeshFactory.Line(), p + Vector3.up, Quaternion.Euler(0f, i * 60f, 0f), new Vector3(0.3f, 1f, 1f), new Vector3(0.05f, 1f, 3f * fx), new Color(1f, 0.85f, 0.4f), 0.35f);
                    }
                    else
                    {
                        // Tide: two rippling rings and spray.
                        VFX.Shockwave(p, 2f * fx, new Color(0.55f, 0.85f, 1f), 0.4f);
                        VFX.Shockwave(p, 3.2f * fx, new Color(0.3f, 0.6f, 1f), 0.55f);
                        ElementFx.Stream(p + Vector3.up, f, Element.Water, 10);
                    }
                    break;
                case "sora":
                    // A three-step afterimage zig-zag of sparks.
                    for (int i = 0; i < 3; i++)
                    {
                        Vector3 a = p + f * (i * 1.2f) + transform.right * (i % 2 == 0 ? 0.8f : -0.8f);
                        Vector3 b = p + f * ((i + 1) * 1.2f) + transform.right * (i % 2 == 0 ? -0.8f : 0.8f);
                        BoltFx.Strike(a + Vector3.up, b + Vector3.up, new Color(1f, 0.95f, 0.5f), 0.15f, 0.25f, 0.5f);
                    }
                    break;
                case "kiba":
                    // Beast claws: an X of rakes.
                    VFX.Slash(tgt, f, 2.2f * fx, 70f, 45f, new Color(0.85f, 0.75f, 1f), 0.3f);
                    VFX.Slash(tgt, f, 2.2f * fx, 70f, -45f, new Color(0.85f, 0.75f, 1f), 0.3f);
                    break;
                case "hana":
                    // A ring of petals.
                    for (int i = 0; i < 8; i++) VFX.Breath(p + Quaternion.Euler(0f, i * 45f, 0f) * Vector3.forward * 1.8f * fx, new Color(1f, 0.7f, 0.85f), 5);
                    break;
                case "tetsu":
                    // Small stone spikes around him.
                    for (int i = 0; i < 6; i++) RisingSpike.Burst(p + Quaternion.Euler(0f, i * 60f, 0f) * Vector3.forward * 2f * fx, new Color(0.55f, 0.48f, 0.4f), 0.8f, 0.35f);
                    break;
                case "homura":
                    if (Def.id == "homura_lastflame")
                    {
                        // Last Flame: an ember ring bursts outward from the greatsword's landing.
                        ElementFx.Finisher(tgt, f, Element.Flame, 2.5f * fx);
                        VFX.Shockwave(tgt, 3f * fx, new Color(1f, 0.6f, 0.15f), 0.45f);
                        VFX.Breath(tgt, new Color(1f, 0.3f, 0.05f), 30);
                        break;
                    }
                    // Flame columns in a cross.
                    for (int i = 0; i < 4; i++) VFX.Pillar(tgt + Quaternion.Euler(0f, i * 90f + 45f, 0f) * Vector3.forward * 1.5f * fx, new Color(1f, 0.45f, 0.1f), 4f * fx, 0.45f);
                    break;
                case "mina":
                    // A violet afterimage crosses the target.
                    VFX.Flash(MeshFactory.Line(), tgt - transform.right * 2f + Vector3.up, Quaternion.LookRotation(transform.right), new Vector3(0.8f, 1f, 4f), new Vector3(0.05f, 1f, 4f), new Color(0.75f, 0.4f, 1f), 0.3f);
                    VFX.Smoke(tgt, new Color(0.25f, 0.08f, 0.35f, 0.6f), 8);
                    break;
                case "rokuro":
                    // A few arrows rain on the target.
                    for (int i = 0; i < 4; i++)
                    {
                        Vector2 r = Random.insideUnitCircle * 1.5f;
                        FallingMeteor.Drop(tgt + new Vector3(r.x, 0f, r.y), new Color(0.75f, 0.9f, 0.5f), 0.7f, 0.3f, true, at => VFX.Dust(at, 3));
                    }
                    break;
                case "genji":
                    // Wind blades fan out ahead.
                    for (int i = -1; i <= 1; i++) VFX.Flash(MeshFactory.Line(), p + Vector3.up * 1.1f, Quaternion.LookRotation(Quaternion.Euler(0f, i * 25f, 0f) * f), new Vector3(0.5f, 1f, 1f), new Vector3(0.05f, 1f, 5f * fx), new Color(0.75f, 0.9f, 1f), 0.3f);
                    break;
                case "yui":
                    // Fans throw a spray of bubbles.
                    ElementFx.Stream(p + Vector3.up, f, Element.Water, 14);
                    VFX.Shockwave(ahead, 1.8f * fx, new Color(0.5f, 0.9f, 1f), 0.4f);
                    break;
                case "raiga":
                    // The sky answers: a bolt on the nearest demon.
                    BoltFx.Strike(tgt + Vector3.up * 14f, tgt, new Color(1f, 0.95f, 0.45f), 0.35f * fx, 0.3f, 0.4f);
                    VFX.ImpactLight(tgt + Vector3.up * 2f, new Color(1f, 0.95f, 0.6f), 8f, 0.15f);
                    break;
                case "kuroe":
                    // Twin crimson crescents.
                    ElementFx.Slash(tgt, f, 2f * fx, 200f, 60f, Element.Dark, 1.2f);
                    ElementFx.Slash(tgt, f, 2f * fx, 200f, -60f, Element.Dark, 1.2f);
                    break;
                case "seren":
                    // Three little stars fall.
                    for (int i = 0; i < 3; i++)
                    {
                        Vector3 at = tgt + Quaternion.Euler(0f, i * 120f, 0f) * Vector3.forward * 1.2f;
                        BoltFx.Strike(at + Vector3.up * 9f, at, new Color(1f, 0.95f, 0.7f), 0.2f, 0.3f, 0.05f);
                        VFX.HitSpark(at, new Color(1f, 0.95f, 0.7f), 8);
                    }
                    break;
                case "garou":
                    // A whirl of dust and green blades.
                    VFX.Slash(p, f, 3f * fx, 340f, 10f, new Color(0.45f, 0.95f, 0.75f), 0.3f);
                    VFX.Dust(p, 14);
                    break;
            }
        }
    }
}
