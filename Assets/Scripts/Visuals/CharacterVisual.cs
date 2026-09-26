using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// Placeholder cel-shaded "figure" assembled from primitives, with procedural animation
    /// (idle bob, run lean, sword swings, hit flash, death). Replace BuildHero/BuildDemon with
    /// real rigged models later – gameplay code only talks to this component's methods.
    /// </summary>
    public class CharacterVisual : MonoBehaviour
    {
        public Transform Model;
        public Transform SwordPivot;
        public TrailRenderer Trail;

        readonly List<Renderer> renderers = new List<Renderer>();
        MaterialPropertyBlock block;
        float flash;
        Color flashColor = Color.white;
        float bob;
        float moving;
        float chargeGlow;
        Color chargeColor;
        Quaternion swordRest = Quaternion.Euler(25f, 40f, 0f);
        Coroutine swingRoutine;
        bool dead;

        static readonly Color Skin = new Color(1f, 0.86f, 0.74f);

        // ------------------------------------------------------------------ Builders

        public static CharacterVisual BuildHero(CharacterDefinition def, Transform parent)
        {
            var root = new GameObject("Visual");
            root.transform.SetParent(parent, false);
            var v = root.AddComponent<CharacterVisual>();
            var model = new GameObject("Model").transform;
            model.SetParent(root.transform, false);
            v.Model = model;

            var hakama = MaterialFactory.Toon(def.bodyColor * 0.8f);
            var uniform = MaterialFactory.Toon(def.bodyColor);
            var haori = MaterialFactory.Toon(def.haoriColor);
            var skin = MaterialFactory.Toon(Skin, 0.02f);
            var hair = MaterialFactory.Toon(def.hairColor);
            var dark = MaterialFactory.Toon(new Color(0.05f, 0.05f, 0.07f), 0f);

            v.Add(MeshFactory.Primitive(PrimitiveType.Capsule, model, new Vector3(0f, 0.55f, 0f), new Vector3(0.62f, 0.55f, 0.62f), hakama));
            v.Add(MeshFactory.Primitive(PrimitiveType.Capsule, model, new Vector3(0f, 1.15f, 0.02f), new Vector3(0.56f, 0.42f, 0.48f), uniform));
            v.Add(MeshFactory.Primitive(PrimitiveType.Capsule, model, new Vector3(0f, 1.0f, -0.06f), new Vector3(0.68f, 0.5f, 0.6f), haori));
            v.Add(MeshFactory.Primitive(PrimitiveType.Sphere, model, new Vector3(0f, 1.72f, 0.02f), Vector3.one * 0.42f, skin));
            v.Add(MeshFactory.Primitive(PrimitiveType.Sphere, model, new Vector3(0f, 1.8f, -0.06f), new Vector3(0.47f, 0.42f, 0.44f), hair));
            v.Add(MeshFactory.Primitive(PrimitiveType.Sphere, model, new Vector3(0.08f, 1.74f, 0.2f), Vector3.one * 0.06f, dark));
            v.Add(MeshFactory.Primitive(PrimitiveType.Sphere, model, new Vector3(-0.08f, 1.74f, 0.2f), Vector3.one * 0.06f, dark));
            // Sash in the element colour so teams read at a glance.
            v.Add(MeshFactory.Primitive(PrimitiveType.Cylinder, model, new Vector3(0f, 0.92f, 0f), new Vector3(0.66f, 0.05f, 0.6f),
                MaterialFactory.Toon(ElementChart.ColorOf(def.element), 0f)));

            v.BuildSword(def.bladeColor, 1.25f, new Vector3(0.38f, 1.1f, 0.12f));
            return v;
        }

        public static CharacterVisual BuildDemon(EnemyDefinition def, Transform parent)
        {
            var root = new GameObject("Visual");
            root.transform.SetParent(parent, false);
            var v = root.AddComponent<CharacterVisual>();
            var model = new GameObject("Model").transform;
            model.SetParent(root.transform, false);
            model.localScale = Vector3.one * def.scale;
            v.Model = model;

            var body = MaterialFactory.Toon(def.bodyColor);
            var accent = MaterialFactory.Toon(def.accentColor, 0.02f, def.accentColor * 0.8f);
            var skin = MaterialFactory.Toon(Color.Lerp(def.bodyColor, new Color(0.85f, 0.75f, 0.7f), 0.5f));
            var eye = MaterialFactory.Toon(new Color(1f, 0.9f, 0.3f), 0f, new Color(1f, 0.8f, 0.2f));

            bool slim = def.archetype == EnemyArchetype.Fast;
            v.Add(MeshFactory.Primitive(PrimitiveType.Capsule, model, new Vector3(0f, 0.85f, 0f),
                slim ? new Vector3(0.55f, 0.85f, 0.5f) : new Vector3(0.8f, 0.85f, 0.7f), body));
            v.Add(MeshFactory.Primitive(PrimitiveType.Sphere, model, new Vector3(0f, 1.85f, 0.05f), Vector3.one * 0.5f, skin));
            v.Add(MeshFactory.Primitive(PrimitiveType.Sphere, model, new Vector3(0.1f, 1.9f, 0.27f), new Vector3(0.1f, 0.06f, 0.05f), eye));
            v.Add(MeshFactory.Primitive(PrimitiveType.Sphere, model, new Vector3(-0.1f, 1.9f, 0.27f), new Vector3(0.1f, 0.06f, 0.05f), eye));
            var hornL = MeshFactory.Primitive(PrimitiveType.Cylinder, model, new Vector3(0.14f, 2.12f, 0f), new Vector3(0.06f, 0.18f, 0.06f), accent);
            hornL.transform.localRotation = Quaternion.Euler(0f, 0f, -25f);
            var hornR = MeshFactory.Primitive(PrimitiveType.Cylinder, model, new Vector3(-0.14f, 2.12f, 0f), new Vector3(0.06f, 0.18f, 0.06f), accent);
            hornR.transform.localRotation = Quaternion.Euler(0f, 0f, 25f);
            v.Add(hornL);
            v.Add(hornR);

            switch (def.archetype)
            {
                case EnemyArchetype.Tank:
                    v.Add(MeshFactory.Primitive(PrimitiveType.Sphere, model, new Vector3(0.5f, 1.35f, 0f), Vector3.one * 0.5f, accent));
                    v.Add(MeshFactory.Primitive(PrimitiveType.Sphere, model, new Vector3(-0.5f, 1.35f, 0f), Vector3.one * 0.5f, accent));
                    break;
                case EnemyArchetype.Ranged:
                    v.Add(MeshFactory.Primitive(PrimitiveType.Sphere, model, new Vector3(0.45f, 1.1f, 0.35f), Vector3.one * 0.3f,
                        MaterialFactory.Toon(def.accentColor, 0f, def.accentColor)));
                    break;
                case EnemyArchetype.Boss:
                    if (def.bossStyle == "thousandarm")
                    {
                        for (int i = 0; i < 10; i++)
                        {
                            float a = i * 36f;
                            var arm = MeshFactory.Primitive(PrimitiveType.Capsule, model, Vector3.zero, new Vector3(0.18f, 0.7f, 0.18f), skin);
                            arm.transform.localRotation = Quaternion.Euler(0f, a, 70f);
                            arm.transform.localPosition = Quaternion.Euler(0f, a, 0f) * new Vector3(0.75f, 1.1f + (i % 2) * 0.35f, 0f);
                            v.Add(arm);
                        }
                    }
                    else
                    {
                        for (int i = 0; i < 4; i++)
                        {
                            v.Add(MeshFactory.Primitive(PrimitiveType.Cube, model, new Vector3(0f, 0.55f + i * 0.28f, 0.33f),
                                new Vector3(0.55f, 0.04f, 0.04f), accent));
                        }
                    }
                    break;
            }

            // Claws / blade use the same pivot so the swing animation works for demons too.
            var clawColor = def.archetype == EnemyArchetype.Elite ? def.accentColor : new Color(0.9f, 0.85f, 0.8f);
            v.BuildSword(clawColor, def.archetype == EnemyArchetype.Elite ? 1.1f : 0.55f, new Vector3(0.45f, 1.1f, 0.2f));
            v.Trail.widthMultiplier = 0.25f;
            return v;
        }

        void BuildSword(Color bladeColor, float length, Vector3 pivotPos)
        {
            SwordPivot = new GameObject("SwordPivot").transform;
            SwordPivot.SetParent(Model, false);
            SwordPivot.localPosition = pivotPos;
            SwordPivot.localRotation = swordRest;

            var bladeMat = MaterialFactory.Toon(bladeColor, 0.01f, bladeColor * 0.6f);
            Add(MeshFactory.Primitive(PrimitiveType.Cube, SwordPivot, new Vector3(0f, 0f, length * 0.55f), new Vector3(0.05f, 0.08f, length), bladeMat));
            Add(MeshFactory.Primitive(PrimitiveType.Cube, SwordPivot, new Vector3(0f, 0f, 0.05f), new Vector3(0.18f, 0.05f, 0.05f),
                MaterialFactory.Toon(new Color(0.15f, 0.12f, 0.1f), 0f)));

            var tip = new GameObject("Trail");
            tip.transform.SetParent(SwordPivot, false);
            tip.transform.localPosition = new Vector3(0f, 0f, length * 0.9f);
            Trail = tip.AddComponent<TrailRenderer>();
            Trail.time = 0.14f;
            Trail.minVertexDistance = 0.05f;
            Trail.widthMultiplier = 0.45f;
            Trail.widthCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);
            Trail.material = MaterialFactory.Additive(Color.white);
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.Lerp(bladeColor, Color.white, 0.5f), 0f), new GradientColorKey(bladeColor, 1f) },
                new[] { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(0f, 1f) });
            Trail.colorGradient = g;
            Trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            Trail.emitting = false;
        }

        void Add(GameObject go)
        {
            var r = go.GetComponent<Renderer>();
            if (r != null) renderers.Add(r);
        }

        // ------------------------------------------------------------------ Animation API

        public void SetMoving(float amount01) { moving = Mathf.Clamp01(amount01); }

        public void Flash(Color color, float amount = 1f)
        {
            flashColor = color;
            flash = Mathf.Max(flash, amount);
        }

        public void SetCharge(float amount01, Color color)
        {
            chargeGlow = amount01;
            chargeColor = color;
        }

        /// <summary>Sweeps the sword from one yaw angle to another (degrees, relative to facing).</summary>
        public void Swing(float fromYaw, float toYaw, float duration, float pitch = 20f, float roll = 0f)
        {
            if (dead || SwordPivot == null) return;
            if (swingRoutine != null) StopCoroutine(swingRoutine);
            swingRoutine = StartCoroutine(SwingRoutine(fromYaw, toYaw, duration, pitch, roll));
        }

        IEnumerator SwingRoutine(float fromYaw, float toYaw, float duration, float pitch, float roll)
        {
            if (Trail != null) { Trail.Clear(); Trail.emitting = true; }
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / duration);
                k = 1f - Mathf.Pow(1f - k, 3f);
                SwordPivot.localRotation = Quaternion.Euler(pitch, Mathf.Lerp(fromYaw, toYaw, k), roll);
                yield return null;
            }
            if (Trail != null) Trail.emitting = false;
            yield return new WaitForSeconds(0.12f);
            float r = 0f;
            var start = SwordPivot.localRotation;
            while (r < 1f)
            {
                r += Time.deltaTime * 6f;
                SwordPivot.localRotation = Quaternion.Slerp(start, swordRest, r);
                yield return null;
            }
            swingRoutine = null;
        }

        /// <summary>Full-body spin used by whirl-type skills.</summary>
        public void Spin(float duration, int turns = 1)
        {
            if (!dead) StartCoroutine(SpinRoutine(duration, turns));
        }

        IEnumerator SpinRoutine(float duration, int turns)
        {
            if (Trail != null) { Trail.Clear(); Trail.emitting = true; }
            SwordPivot.localRotation = Quaternion.Euler(10f, 90f, 0f);
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                Model.localRotation = Quaternion.Euler(0f, 360f * turns * Mathf.Clamp01(t / duration), 0f);
                yield return null;
            }
            Model.localRotation = Quaternion.identity;
            if (Trail != null) Trail.emitting = false;
            SwordPivot.localRotation = swordRest;
        }

        public void PlayDeath()
        {
            dead = true;
            StopAllCoroutines();
            if (Trail != null) Trail.emitting = false;
            StartCoroutine(DeathRoutine());
        }

        IEnumerator DeathRoutine()
        {
            float t = 0f;
            var start = Model.localRotation;
            var end = Quaternion.Euler(-80f, 0f, 0f);
            Vector3 s0 = Model.localScale;
            while (t < 1f)
            {
                t += Time.deltaTime * 1.6f;
                Model.localRotation = Quaternion.Slerp(start, end, Mathf.Clamp01(t * 1.5f));
                if (t > 0.5f) Model.localScale = s0 * Mathf.Lerp(1f, 0.05f, (t - 0.5f) * 2f);
                yield return null;
            }
        }

        public void ResetPose()
        {
            dead = false;
            StopAllCoroutines();
            swingRoutine = null;
            Model.localRotation = Quaternion.identity;
            if (SwordPivot != null) SwordPivot.localRotation = swordRest;
        }

        void Update()
        {
            if (dead) return;
            bob += Time.deltaTime * (moving > 0.1f ? 12f : 3f);
            float y = moving > 0.1f ? Mathf.Abs(Mathf.Sin(bob)) * 0.1f : Mathf.Sin(bob) * 0.025f;
            Model.localPosition = new Vector3(0f, y, 0f);
            float lean = moving * 12f;
            var e = Model.localEulerAngles;
            Model.localRotation = Quaternion.Euler(lean, e.y, 0f);

            flash = Mathf.MoveTowards(flash, 0f, Time.unscaledDeltaTime * 6f);
            float glow = Mathf.Max(flash, chargeGlow * (0.45f + 0.25f * Mathf.Sin(Time.time * 20f)));
            Color c = flash >= chargeGlow * 0.5f ? flashColor : chargeColor;
            if (block == null) block = new MaterialPropertyBlock();
            for (int i = 0; i < renderers.Count; i++)
            {
                var r = renderers[i];
                if (r == null) continue;
                r.GetPropertyBlock(block);
                block.SetFloat("_Flash", glow);
                block.SetColor("_FlashColor", c);
                r.SetPropertyBlock(block);
            }
        }
    }
}
