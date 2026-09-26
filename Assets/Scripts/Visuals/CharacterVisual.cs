using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// A character's look and animation behind one API. Two modes:
    ///  • Rigged model: if Resources/Characters/&lt;id&gt; (or Resources/Enemies/&lt;id&gt;) exists, it is spawned,
    ///    converted to the game's cel shader for a consistent look, and driven through an Animator.
    ///  • Placeholder: a cel-shaded figure built from primitives with procedural animation.
    /// Gameplay code only calls the methods below, so art can be swapped without touching combat.
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
        Coroutine poseRoutine;
        bool dead;
        bool posing;
        bool sprinting;
        bool guarding;
        AnimatorDriver driver;

        public bool HasRig { get { return driver != null; } }
        public AnimatorDriver Driver { get { return driver; } }

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
            if (v.TryLoadModel("Characters/" + def.id, def.bladeColor, 1f)) return v;

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
            if (v.TryLoadModel("Enemies/" + def.id, def.accentColor, 0.6f)) return v;

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


        // ------------------------------------------------------------------ Rigged models

        /// <summary>Spawns a rigged prefab from Resources if one exists for this character.</summary>
        bool TryLoadModel(string resourcePath, Color trailColor, float trailWidth)
        {
            var prefab = Resources.Load<GameObject>(resourcePath);
            if (prefab == null) return false;
            var inst = Instantiate(prefab, Model, false);
            inst.name = prefab.name;
            foreach (var c in inst.GetComponentsInChildren<Collider>()) Destroy(c);

            foreach (var r in inst.GetComponentsInChildren<Renderer>())
            {
                ConvertToToon(r);
                renderers.Add(r);
            }

            var animator = inst.GetComponentInChildren<Animator>();
            if (animator != null && animator.runtimeAnimatorController != null) driver = new AnimatorDriver(animator);

            // Weapon trail: a child named "WeaponTip" wins, else the right hand bone, else the model root.
            Transform tip = FindDeep(inst.transform, "WeaponTip");
            if (tip == null && driver != null) tip = driver.Bone(HumanBodyBones.RightHand);
            if (tip == null) tip = inst.transform;
            SwordPivot = tip;
            var trailGo = new GameObject("Trail");
            trailGo.transform.SetParent(tip, false);
            Trail = trailGo.AddComponent<TrailRenderer>();
            SetupTrail(Trail, trailColor, trailWidth);
            return true;
        }

        static Transform FindDeep(Transform t, string name)
        {
            if (t.name == name) return t;
            for (int i = 0; i < t.childCount; i++)
            {
                var r = FindDeep(t.GetChild(i), name);
                if (r != null) return r;
            }
            return null;
        }

        /// <summary>Re-materials imported models with the game's toon shader so every character shares one art style.</summary>
        static void ConvertToToon(Renderer r)
        {
            var mats = r.sharedMaterials;
            var result = new Material[mats.Length];
            for (int i = 0; i < mats.Length; i++)
            {
                var src = mats[i];
                Texture tex = null;
                Color col = Color.white;
                if (src != null)
                {
                    foreach (var prop in new[] { "_MainTex", "_BaseMap", "baseColorTexture", "_BaseColorMap" })
                        if (tex == null && src.HasProperty(prop)) tex = src.GetTexture(prop);
                    foreach (var prop in new[] { "_Color", "_BaseColor", "baseColorFactor" })
                        if (src.HasProperty(prop)) { col = src.GetColor(prop); break; }
                }
                var m = MaterialFactory.Toon(col, 0.004f);
                if (tex != null && m.HasProperty("_MainTex")) m.SetTexture("_MainTex", tex);
                result[i] = m;
            }
            r.sharedMaterials = result;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        }

        static void SetupTrail(TrailRenderer trail, Color color, float width)
        {
            trail.time = 0.14f;
            trail.minVertexDistance = 0.05f;
            trail.widthMultiplier = 0.45f * width;
            trail.widthCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);
            trail.material = MaterialFactory.Additive(Color.white);
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.Lerp(color, Color.white, 0.5f), 0f), new GradientColorKey(color, 1f) },
                new[] { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(0f, 1f) });
            trail.colorGradient = g;
            trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            trail.emitting = false;
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
            SetupTrail(Trail, bladeColor, 1f);
        }

        void Add(GameObject go)
        {
            var r = go.GetComponent<Renderer>();
            if (r != null) renderers.Add(r);
        }

        // ------------------------------------------------------------------ Animation API

        public void SetMoving(float amount01, bool sprint = false)
        {
            moving = Mathf.Clamp01(amount01);
            sprinting = sprint && moving > 0.5f;
        }

        /// <summary>Combo step 0-4 (4 = finisher). Rigged: Attack1..Attack5 triggers.</summary>
        public void Attack(int step, float duration)
        {
            if (driver != null) { driver.Trigger("Attack" + (step + 1)); TrailBurst(duration + 0.1f); return; }
            Swing(SwingFrom[step % 5], SwingTo[step % 5], duration, SwingPitch[step % 5]);
            Punch(1.08f);
            if (step >= 4) Spin(0.18f);
        }

        public void HeavyAttack(float duration)
        {
            if (driver != null) { driver.Trigger("Heavy"); TrailBurst(duration + 0.15f); return; }
            Swing(-160f, 160f, duration, 25f);
            Punch(1.15f);
        }

        public void DashAttack(float duration)
        {
            if (driver != null) { driver.Trigger("DashAttack"); TrailBurst(duration + 0.1f); return; }
            Swing(-130f, 110f, duration, 10f);
            Punch(1.1f);
        }

        public void Skill(int index, float duration)
        {
            if (driver != null) { driver.Trigger("Skill" + (index + 1)); TrailBurst(duration + 0.2f); }
        }

        public void Ultimate()
        {
            if (driver != null) { driver.Trigger("Ultimate"); TrailBurst(1.5f); }
        }

        public void Dodge(Vector3 worldDir, float duration)
        {
            if (driver != null) { driver.Trigger("Dodge"); return; }
            if (dead) return;
            StartPose(DodgeRoutine(worldDir, duration));
        }

        public void Guard(bool on)
        {
            guarding = on;
            if (driver != null) { driver.SetBool("Guard", on); return; }
            if (SwordPivot == null || dead) return;
            if (on)
            {
                if (swingRoutine != null) StopCoroutine(swingRoutine);
                swingRoutine = null;
                SwordPivot.localRotation = Quaternion.Euler(-10f, -75f, 20f);
            }
            else SwordPivot.localRotation = swordRest;
        }

        public void Hit(Vector3 fromWorldDir)
        {
            if (driver != null) { driver.Trigger("Hit"); return; }
            if (dead || posing) return;
            StartPose(HitRoutine(fromWorldDir));
        }

        public void Knockdown()
        {
            if (driver != null) { driver.Trigger("Knockdown"); return; }
            if (dead) return;
            StartPose(KnockdownRoutine());
        }

        public void GetUp(float duration)
        {
            if (driver != null) { driver.Trigger("GetUp"); return; }
            if (dead) return;
            StartPose(GetUpRoutine(duration));
        }

        public void Victory()
        {
            if (driver != null) { driver.Trigger("Victory"); return; }
            if (dead) return;
            StartPose(VictoryRoutine());
        }

        public void Defeat()
        {
            if (driver != null) { driver.Trigger("Defeat"); return; }
            StartPose(DefeatRoutine());
        }

        void TrailBurst(float seconds)
        {
            if (Trail == null) return;
            if (trailRoutine != null) StopCoroutine(trailRoutine);
            trailRoutine = StartCoroutine(TrailRoutine(seconds));
        }

        Coroutine trailRoutine;

        IEnumerator TrailRoutine(float seconds)
        {
            Trail.Clear();
            Trail.emitting = true;
            yield return new WaitForSeconds(seconds);
            Trail.emitting = false;
        }

        /// <summary>Squash-and-stretch pop that sells the weight of a hit.</summary>
        public void Punch(float amount)
        {
            if (driver == null && !dead) StartCoroutine(PunchRoutine(amount));
        }

        IEnumerator PunchRoutine(float amount)
        {
            float t = 0f;
            while (t < 0.14f)
            {
                t += Time.deltaTime;
                float k = Mathf.Sin(Mathf.Clamp01(t / 0.14f) * Mathf.PI);
                float a = 1f + (amount - 1f) * k;
                Model.localScale = new Vector3(a, 2f - a, a) * baseScale;
                yield return null;
            }
            Model.localScale = Vector3.one * baseScale;
        }

        float baseScale = 1f;

        void StartPose(IEnumerator routine)
        {
            if (poseRoutine != null) StopCoroutine(poseRoutine);
            poseRoutine = StartCoroutine(PoseWrapper(routine));
        }

        IEnumerator PoseWrapper(IEnumerator routine)
        {
            posing = true;
            yield return routine;
            posing = false;
            poseRoutine = null;
        }

        IEnumerator DodgeRoutine(Vector3 worldDir, float duration)
        {
            Vector3 local = transform.InverseTransformDirection(worldDir);
            local.y = 0f;
            if (local.sqrMagnitude < 0.01f) local = Vector3.back;
            Vector3 axis = Vector3.Cross(Vector3.up, local.normalized);
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / duration);
                Model.localRotation = Quaternion.AngleAxis(360f * Mathf.SmoothStep(0f, 1f, k), axis);
                Model.localPosition = new Vector3(0f, Mathf.Sin(k * Mathf.PI) * 0.35f, 0f);
                yield return null;
            }
            Model.localRotation = Quaternion.identity;
            Model.localPosition = Vector3.zero;
        }

        IEnumerator HitRoutine(Vector3 fromWorldDir)
        {
            Vector3 away = transform.InverseTransformDirection(-fromWorldDir);
            away.y = 0f;
            if (away.sqrMagnitude < 0.01f) away = Vector3.back;
            Vector3 axis = Vector3.Cross(Vector3.up, away.normalized);
            float t = 0f;
            while (t < 0.22f)
            {
                t += Time.deltaTime;
                float k = Mathf.Sin(Mathf.Clamp01(t / 0.22f) * Mathf.PI);
                Model.localRotation = Quaternion.AngleAxis(-22f * k, axis);
                yield return null;
            }
            Model.localRotation = Quaternion.identity;
        }

        IEnumerator KnockdownRoutine()
        {
            float t = 0f;
            var start = Model.localRotation;
            var end = Quaternion.Euler(-85f, 0f, 0f);
            while (t < 0.25f)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / 0.25f);
                Model.localRotation = Quaternion.Slerp(start, end, k * k);
                Model.localPosition = new Vector3(0f, 0.25f * (1f - k), -0.4f * k);
                yield return null;
            }
            // Small bounce on landing.
            t = 0f;
            while (t < 0.15f)
            {
                t += Time.deltaTime;
                Model.localPosition = new Vector3(0f, Mathf.Sin(t / 0.15f * Mathf.PI) * 0.12f, -0.4f);
                yield return null;
            }
            // Hold until GetUp is called.
            while (true) yield return null;
        }

        IEnumerator GetUpRoutine(float duration)
        {
            float t = 0f;
            var start = Model.localRotation;
            var startPos = Model.localPosition;
            while (t < duration)
            {
                t += Time.deltaTime;
                float k = Mathf.SmoothStep(0f, 1f, t / duration);
                Model.localRotation = Quaternion.Slerp(start, Quaternion.identity, k);
                Model.localPosition = Vector3.Lerp(startPos, Vector3.zero, k);
                yield return null;
            }
            Model.localRotation = Quaternion.identity;
            Model.localPosition = Vector3.zero;
        }

        IEnumerator VictoryRoutine()
        {
            if (SwordPivot != null) SwordPivot.localRotation = Quaternion.Euler(-80f, 10f, 0f);
            float t = 0f;
            while (t < 0.5f)
            {
                t += Time.deltaTime;
                Model.localPosition = new Vector3(0f, Mathf.Sin(Mathf.Clamp01(t / 0.5f) * Mathf.PI) * 0.6f, 0f);
                yield return null;
            }
            Model.localPosition = Vector3.zero;
            while (true)
            {
                Model.localRotation = Quaternion.Euler(0f, Mathf.Sin(Time.time * 2f) * 6f, 0f);
                yield return null;
            }
        }

        IEnumerator DefeatRoutine()
        {
            float t = 0f;
            while (t < 0.6f)
            {
                t += Time.deltaTime;
                float k = Mathf.SmoothStep(0f, 1f, t / 0.6f);
                Model.localRotation = Quaternion.Euler(28f * k, 0f, 0f);
                Model.localPosition = new Vector3(0f, -0.45f * k, 0f);
                yield return null;
            }
            while (true) yield return null;
        }

        static readonly float[] SwingFrom = { -110f, 100f, -40f, 120f, -150f };
        static readonly float[] SwingTo = { 100f, -110f, 60f, -120f, 150f };
        static readonly float[] SwingPitch = { 20f, 10f, 70f, 5f, 15f };

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
            if (driver != null) { TrailBurst(duration + 0.1f); return; }
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
            if (driver != null) { TrailBurst(duration); return; }
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
            if (driver != null)
            {
                dead = true;
                driver.Trigger(driver.Has("Death") ? "Death" : "Knockdown");
                if (Trail != null) Trail.emitting = false;
                StartCoroutine(RigDeathFade());
                return;
            }
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

        IEnumerator RigDeathFade()
        {
            yield return new WaitForSeconds(0.7f);
            Vector3 s0 = Model.localScale;
            float t = 0f;
            while (t < 0.4f)
            {
                t += Time.deltaTime;
                Model.localScale = s0 * Mathf.Lerp(1f, 0.02f, t / 0.4f);
                yield return null;
            }
        }

        public void ResetPose()
        {
            dead = false;
            posing = false;
            guarding = false;
            StopAllCoroutines();
            swingRoutine = null;
            poseRoutine = null;
            trailRoutine = null;
            Model.localPosition = Vector3.zero;
            Model.localScale = Vector3.one * baseScale;
            if (driver != null) { driver.SetBool("Guard", false); return; }
            Model.localRotation = Quaternion.identity;
            if (SwordPivot != null) SwordPivot.localRotation = swordRest;
        }

        void Start()
        {
            baseScale = Model != null ? Model.localScale.x : 1f;
        }

        void Update()
        {
            if (driver != null)
            {
                driver.SetSpeed(dead ? 0f : (sprinting ? 1f : moving * 0.7f), Time.deltaTime);
                UpdateFlash();
                return;
            }
            if (dead) return;
            if (posing)
            {
                UpdateFlash();
                return;
            }
            bob += Time.deltaTime * (moving > 0.1f ? (sprinting ? 16f : 12f) : 3f);
            float y = moving > 0.1f ? Mathf.Abs(Mathf.Sin(bob)) * 0.1f : Mathf.Sin(bob) * 0.025f;
            Model.localPosition = new Vector3(0f, y, 0f);
            float lean = moving * (sprinting ? 22f : 12f) - (guarding ? 8f : 0f);
            var e = Model.localEulerAngles;
            Model.localRotation = Quaternion.Euler(lean, e.y, 0f);
            UpdateFlash();
        }

        void UpdateFlash()
        {
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
