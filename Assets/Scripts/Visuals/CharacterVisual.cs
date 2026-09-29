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
    public partial class CharacterVisual : MonoBehaviour
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
        Quaternion swordRest = Quaternion.Euler(48f, 62f, 0f); // held low at the side, tip toward the ground
        Coroutine swingRoutine;
        Coroutine poseRoutine;
        bool dead;
        bool posing;
        bool sprinting;
        bool guarding;
        AnimatorDriver driver;

        public bool HasRig { get { return driver != null; } }
        // Read by ProceduralRig to animate imported skeletons.
        public float MoveAmount { get { return moving; } }
        public bool IsSprinting { get { return sprinting; } }
        public bool IsGuarding { get { return guarding; } }
        public bool IsDead { get { return dead; } }
        /// <summary>0 at rest → 1 when the blade is swung far from its resting angle.</summary>
        public float SwingWeight { get { return SwordPivot == null ? 0f : Mathf.Clamp01(Quaternion.Angle(SwordPivot.localRotation, swordRest) / 50f); } }
        public AnimatorDriver Driver { get { return driver; } }


        // ------------------------------------------------------------------ Builders

        public static CharacterVisual BuildHero(CharacterDefinition def, Transform parent)
        {
            var root = new GameObject("Visual");
            root.transform.SetParent(parent, false);
            var v = root.AddComponent<CharacterVisual>();
            var model = new GameObject("Model").transform;
            model.SetParent(root.transform, false);
            v.Model = model;
            if (GameConfig.UseImportedModels && v.TryLoadModel("Characters/" + def.id, def.bladeColor, 1f, 1.75f * def.scale)) return v;

            v.deferOptimize = true;
            if (IsPremium(def.id)) v.BuildPremium(def);
            else if (IsDesign(def.id)) v.BuildDesign(def);
            else if (GameConfig.PremiumRoster) v.BuildRoster(def);
            else v.BuildChibi(def);
            v.Stylize(def);
            v.CartoonProportions(1f);
            v.SoftenLook();
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
            if (GameConfig.UseImportedModels && v.TryLoadModel("Enemies/" + def.id, def.accentColor, 0.6f, 2.1f)) return v;

            if (v.BuildMonster(def, model)) { v.CartoonProportions(0.55f); return v; }

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
        bool TryLoadModel(string resourcePath, Color trailColor, float trailWidth, float targetHeight)
        {
            GameObject prefab = null;
            try { prefab = Resources.Load<GameObject>(resourcePath); }
            catch (System.Exception ex) { Debug.LogWarning("[CharacterVisual] could not load " + resourcePath + ": " + ex.Message); }
            if (prefab == null) return false;
            try { return LoadModel(prefab, trailColor, trailWidth, targetHeight); }
            catch (System.Exception ex)
            {
                // Fall back to the procedural body rather than breaking the scene.
                Debug.LogError("[CharacterVisual] model " + resourcePath + " failed, using the built-in body instead: " + ex);
                for (int i = Model.childCount - 1; i >= 0; i--) Destroy(Model.GetChild(i).gameObject);
                renderers.Clear();
                driver = null;
                SwordPivot = null;
                Trail = null;
                return false;
            }
        }

        bool LoadModel(GameObject prefab, Color trailColor, float trailWidth, float targetHeight)
        {
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
            else
            {
                // Raw AI-generated models (e.g. a Higgsfield/Meshy GLB): normalise size and pivot, and if the mesh
                // is rigged, bring the skeleton to life procedurally.
                NormalizeModel(inst.transform, targetHeight);
                // Sanity check: a model that imported at a wildly wrong size would swallow the camera.
                var rs = inst.GetComponentsInChildren<Renderer>();
                if (rs.Length == 0) throw new System.Exception("model has no renderers");
                Bounds nb = rs[0].bounds;
                for (int i = 1; i < rs.Length; i++) nb.Encapsulate(rs[i].bounds);
                float worldH = nb.size.y / Mathf.Max(0.0001f, Model.lossyScale.y);
                if (float.IsNaN(worldH) || worldH < targetHeight * 0.3f || worldH > targetHeight * 3f)
                    throw new System.Exception("model size out of range after normalising (" + worldH + " m)");
                var rig = inst.AddComponent<ProceduralRig>();
                if (!rig.Bind(this)) Destroy(rig);
            }

            // Weapon trail: a child named "WeaponTip" wins, else the right hand bone, else a drawn blade.
            Transform tip = FindDeep(inst.transform, "WeaponTip");
            if (tip == null && driver != null) tip = driver.Bone(HumanBodyBones.RightHand);
            if (tip == null && driver == null)
            {
                // No animator: give the model a drawn blade in the right hand so swings read clearly.
                BuildSword(trailColor, 1.1f * targetHeight / 1.75f, new Vector3(0.38f, 1.05f, 0.15f) * (targetHeight / 1.75f));
                return true;
            }
            if (tip == null) tip = inst.transform;
            SwordPivot = tip;
            var trailGo = new GameObject("Trail");
            trailGo.transform.SetParent(tip, false);
            Trail = trailGo.AddComponent<TrailRenderer>();
            SetupTrail(Trail, trailColor, trailWidth);
            return true;
        }

        /// <summary>Scales an imported model to a target height, puts its feet on the ground and centres it.</summary>
        static void NormalizeModel(Transform inst, float targetHeight)
        {
            var rs = inst.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) return;
            Bounds b = rs[0].bounds;
            for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
            var parent = inst.parent;
            float parentScale = parent != null ? parent.lossyScale.y : 1f;
            float h = b.size.y / Mathf.Max(0.0001f, parentScale);
            if (h < 0.001f) return;
            float k = targetHeight / h;
            inst.localScale *= k;
            // Recompute after scaling, then shift so the feet sit at y = 0 and the body is centred.
            b = rs[0].bounds;
            for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
            Vector3 localMin = parent != null ? parent.InverseTransformPoint(new Vector3(b.center.x, b.min.y, b.center.z)) : new Vector3(b.center.x, b.min.y, b.center.z);
            inst.localPosition -= localMin;
            foreach (var smr in inst.GetComponentsInChildren<SkinnedMeshRenderer>()) smr.updateWhenOffscreen = true;
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

            if (length > 0.2f) SteelBlade(SwordPivot, length, bladeColor, 1f);

            var tipT = new GameObject("Trail");
            tipT.transform.SetParent(SwordPivot, false);
            tipT.transform.localPosition = new Vector3(0f, 0f, Mathf.Max(0.3f, length * 0.9f));
            Trail = tipT.AddComponent<TrailRenderer>();
            SetupTrail(Trail, bladeColor, 1f);
        }

        /// <summary>
        /// A real blade: polished steel with only a faint tint of the wielder's element on the edge, a tapered tip,
        /// a round guard, a wrapped grip and a pommel. Built along +Z from the parent's origin (the hand).
        /// </summary>
        void SteelBlade(Transform parent, float length, Color tint, float width)
        {
            var steel = MaterialFactory.Toon(new Color(0.76f, 0.78f, 0.82f), 0.012f, new Color(0.05f, 0.05f, 0.06f));
            var edge = MaterialFactory.Toon(Color.Lerp(new Color(0.9f, 0.92f, 0.95f), tint, 0.35f), 0.008f, tint * 0.15f);
            var guard = MaterialFactory.Toon(new Color(0.52f, 0.43f, 0.24f), 0.012f);
            var wrap = MaterialFactory.Toon(new Color(0.12f, 0.1f, 0.12f), 0.012f);
            var cord = MaterialFactory.Toon(Color.Lerp(tint, new Color(0.5f, 0.5f, 0.5f), 0.45f), 0.008f);
            float bl = length * 0.9f;
            Add(MeshFactory.Primitive(PrimitiveType.Cube, parent, new Vector3(0f, 0.004f, 0.12f + bl * 0.5f), new Vector3(0.028f, 0.075f * width, bl), steel));
            Add(MeshFactory.Primitive(PrimitiveType.Cube, parent, new Vector3(0f, -0.038f * width, 0.12f + bl * 0.5f), new Vector3(0.018f, 0.02f, bl), edge));
            var tip = MeshFactory.MeshObject(MeshFactory.FacetCone(4), parent, new Vector3(0f, -0.004f, 0.12f + bl), new Vector3(0.03f, 0.16f * width, 0.085f * width), steel);
            tip.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            Add(tip);
            var tsuba = MeshFactory.MeshObject(MeshFactory.FacetCylinder(10), parent, new Vector3(0f, 0f, 0.1f), new Vector3(0.17f, 0.025f, 0.17f) * Mathf.Max(1f, width * 0.8f), guard);
            tsuba.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            Add(tsuba);
            Add(MeshFactory.Primitive(PrimitiveType.Cube, parent, new Vector3(0f, 0f, -0.04f), new Vector3(0.045f, 0.055f, 0.26f), wrap));
            for (int i = 0; i < 3; i++)
                Add(MeshFactory.Primitive(PrimitiveType.Cube, parent, new Vector3(0f, 0f, 0.05f - i * 0.08f), new Vector3(0.05f, 0.06f, 0.015f), cord));
            Add(MeshFactory.Primitive(PrimitiveType.Sphere, parent, new Vector3(0f, 0f, -0.18f), Vector3.one * 0.05f, guard));
        }

        /// <summary>A wooden pole along +Z (spears, staves, canes).</summary>
        void Shaft(Transform parent, float length, Color wood)
        {
            var m = MaterialFactory.Toon(wood, 0.012f);
            var go = MeshFactory.Primitive(PrimitiveType.Cylinder, parent, new Vector3(0f, 0f, length * 0.5f - 0.3f), new Vector3(0.05f, length * 0.5f, 0.05f), m);
            go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            Add(go);
        }

        void Add(GameObject go)
        {
            var r = go.GetComponent<Renderer>();
            if (r != null) renderers.Add(r);
        }

        // ------------------------------------------------------------------ Animation API

        /// <summary>How much it's walking or running right now (0..1).</summary>
        public float MovingAmount { get { return moving; } }

        /// <summary>Jiggle the soft body: negative squashes, positive stretches.</summary>
        public void Squish(float amount)
        {
            var sb = GetComponent<SquishBounce>();
            if (sb != null) sb.Poke(amount);
        }

        public void SetMoving(float amount01, bool sprint = false)
        {
            moving = Mathf.Clamp01(amount01);
            sprinting = sprint && moving > 0.5f;
        }

        /// <summary>Combo step 0-4 (4 = finisher). Rigged: Attack1..Attack5 triggers.</summary>
        public void Attack(int step, float duration)
        {
            Squish(0.1f);
            if (driver != null) { driver.Trigger("Attack" + (step + 1)); TrailBurst(duration + 0.1f); return; }
            Swing(SwingFrom[step % 5], SwingTo[step % 5], duration, SwingPitch[step % 5]);
            if (!GameConfig.CartoonStyle) Punch(1.08f);
            if (step >= 4) Spin(0.18f);
        }

        public void HeavyAttack(float duration)
        {
            Squish(-0.16f);
            if (driver != null) { driver.Trigger("Heavy"); TrailBurst(duration + 0.15f); return; }
            Swing(-160f, 160f, duration, 25f);
            Punch(1.15f);
        }

        public void DashAttack(float duration)
        {
            Squish(0.14f);
            if (driver != null) { driver.Trigger("DashAttack"); TrailBurst(duration + 0.1f); return; }
            Swing(-130f, 110f, duration, 10f);
            Punch(1.1f);
        }

        public void Skill(int index, float duration)
        {
            if (driver != null) { driver.Trigger("Skill" + (index + 1)); TrailBurst(duration + 0.2f); return; }
            if (dead || !GameConfig.CartoonStyle) return;
            Squish(0.14f);
            StartCoroutine(SkillFlourish(index));
        }

        public void Ultimate()
        {
            if (driver != null) { driver.Trigger("Ultimate"); TrailBurst(1.5f); return; }
            if (dead || !GameConfig.CartoonStyle) return;
            StartPose(UltimateRoutine());
        }

        public void Dodge(Vector3 worldDir, float duration)
        {
            Squish(-0.1f);
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
            Squish(-0.18f);
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
            punchUntil = Time.time + 0.15f;
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
        float punchUntil;

        void StartPose(IEnumerator routine)
        {
            if (poseRoutine != null) StopCoroutine(poseRoutine);
            showcase = false;
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

        static float EaseOutBack(float x)
        {
            const float c1 = 1.70158f, c3 = c1 + 1f;
            x = Mathf.Clamp01(x) - 1f;
            return 1f + c3 * x * x * x + c1 * x * x;
        }

        IEnumerator HitRoutine(Vector3 fromWorldDir)
        {
            // Impact → knockback → springy recovery: the body snaps away from the blow, squashes, slides back a
            // little, then rebounds past upright before settling.
            Vector3 away = transform.InverseTransformDirection(-fromWorldDir);
            away.y = 0f;
            if (away.sqrMagnitude < 0.01f) away = Vector3.back;
            away.Normalize();
            Vector3 axis = Vector3.Cross(Vector3.up, away);
            const float T = 0.36f;
            float t = 0f;
            while (t < T)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / T);
                float snap = k < 0.18f ? k / 0.18f : 1f - EaseOutBack((k - 0.18f) / 0.82f);
                Model.localRotation = Quaternion.AngleAxis(-30f * snap, axis);
                Model.localPosition = away * 0.2f * Mathf.Max(0f, snap);
                Model.localScale = baseScale * Vector3.LerpUnclamped(Vector3.one, new Vector3(1.12f, 0.86f, 1.12f), snap);
                yield return null;
            }
            Model.localRotation = Quaternion.identity;
            Model.localPosition = Vector3.zero;
            Model.localScale = Vector3.one * baseScale;
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

        /// <summary>Squash-and-stretch jump: crouch (anticipation), launch (stretch), land (squash).</summary>
        IEnumerator Jump(float height, float air, float spinTurns, bool heavy)
        {
            float t = 0f;
            while (t < 0.13f)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / 0.13f);
                Model.localScale = baseScale * Vector3.Lerp(Vector3.one, new Vector3(1.16f, 0.8f, 1.16f), k);
                Model.localPosition = new Vector3(0f, -0.06f * k, 0f);
                yield return null;
            }
            t = 0f;
            while (t < air)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / air);
                Model.localPosition = new Vector3(0f, Mathf.Sin(k * Mathf.PI) * height, 0f);
                float st = Mathf.Sin(Mathf.Clamp01(k * 1.6f) * Mathf.PI);
                Model.localScale = baseScale * Vector3.Lerp(Vector3.one, new Vector3(0.86f, 1.2f, 0.86f), st);
                Model.localRotation = Quaternion.Euler(0f, 360f * spinTurns * Mathf.SmoothStep(0f, 1f, k), 0f);
                yield return null;
            }
            Model.localRotation = Quaternion.identity;
            if (heavy) { VFX.Dust(transform.position, 8); Squish(-0.2f); }
            t = 0f;
            while (t < 0.14f)
            {
                t += Time.deltaTime;
                float k = Mathf.Sin(Mathf.Clamp01(t / 0.14f) * Mathf.PI);
                Model.localPosition = Vector3.zero;
                Model.localScale = baseScale * Vector3.Lerp(Vector3.one, heavy ? new Vector3(1.22f, 0.76f, 1.22f) : new Vector3(1.14f, 0.84f, 1.14f), k);
                yield return null;
            }
            Model.localScale = Vector3.one * baseScale;
        }

        IEnumerator VictoryRoutine()
        {
            ShapeLanguage sh = shapeSet ? Shape : ShapeLanguage.Triangle;
            // Every shape celebrates differently: a spinning leap, a heavy stomp, happy hops, an elegant twirl.
            switch (sh)
            {
                case ShapeLanguage.Square:
                    if (SwordPivot != null) SwordPivot.localRotation = Quaternion.Euler(-95f, 20f, 0f);
                    yield return Jump(0.45f, 0.36f, 0f, true);
                    break;
                case ShapeLanguage.Circle:
                    if (SwordPivot != null) SwordPivot.localRotation = Quaternion.Euler(-70f, -30f, 0f);
                    yield return Jump(0.4f, 0.28f, 0f, false);
                    yield return Jump(0.55f, 0.32f, 0f, false);
                    break;
                case ShapeLanguage.Diamond:
                    if (SwordPivot != null) SwordPivot.localRotation = Quaternion.Euler(-110f, 0f, 0f);
                    yield return Jump(0.7f, 0.6f, 1f, false);
                    break;
                default:
                    if (SwordPivot != null) SwordPivot.localRotation = Quaternion.Euler(-80f, 10f, 0f);
                    yield return Jump(0.75f, 0.42f, 1f, false);
                    if (SwordPivot != null) SwordPivot.localRotation = Quaternion.Euler(-15f, 35f, 0f);
                    break;
            }
            float time = 0f;
            while (true)
            {
                time += Time.deltaTime;
                switch (sh)
                {
                    case ShapeLanguage.Square:
                    {
                        // Flexing bounce.
                        float f = Mathf.Abs(Mathf.Sin(time * 3.2f));
                        Model.localScale = baseScale * new Vector3(1f + 0.06f * f, 1f - 0.05f * f, 1f + 0.06f * f);
                        Model.localRotation = Quaternion.Euler(-4f, Mathf.Sin(time * 1.6f) * 8f, 0f);
                        break;
                    }
                    case ShapeLanguage.Circle:
                        Model.localPosition = new Vector3(0f, Mathf.Abs(Mathf.Sin(time * 5f)) * 0.14f, 0f);
                        Model.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(time * 5f) * 7f);
                        if (SwordPivot != null) SwordPivot.localRotation = Quaternion.Euler(-70f, -30f + Mathf.Sin(time * 6f) * 30f, 0f);
                        break;
                    case ShapeLanguage.Diamond:
                        Model.localPosition = new Vector3(0f, 0.08f + Mathf.Sin(time * 2f) * 0.05f, 0f);
                        Model.localRotation = Quaternion.Euler(-3f, Mathf.Sin(time * 0.9f) * 14f, Mathf.Sin(time * 1.8f) * 3f);
                        break;
                    default:
                        Model.localRotation = Quaternion.Euler(-6f, Mathf.Sin(time * 2f) * 6f, 0f);
                        break;
                }
                yield return null;
            }
        }

        IEnumerator DefeatRoutine()
        {
            // Stagger back, sway, drop to the knees and slump, still breathing hard.
            float t = 0f;
            while (t < 0.4f)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / 0.4f);
                Model.localPosition = new Vector3(0f, 0f, -0.28f * Mathf.SmoothStep(0f, 1f, k));
                Model.localRotation = Quaternion.Euler(-10f * Mathf.Sin(k * Mathf.PI), 0f, Mathf.Sin(k * Mathf.PI * 3f) * 9f);
                yield return null;
            }
            if (SwordPivot != null) SwordPivot.localRotation = Quaternion.Euler(70f, 25f, 0f);
            t = 0f;
            while (t < 0.35f)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / 0.35f);
                float e = k * k;
                Model.localRotation = Quaternion.Euler(30f * e, 0f, 0f);
                Model.localPosition = new Vector3(0f, -0.42f * e, -0.28f);
                yield return null;
            }
            Squish(-0.18f);
            VFX.Dust(transform.position, 4);
            float time = 0f;
            while (true)
            {
                time += Time.deltaTime;
                float br = Mathf.Sin(time * 3.2f);
                Model.localScale = baseScale * new Vector3(1f + 0.015f * br, 1f + 0.03f * br, 1f + 0.015f * br);
                Model.localRotation = Quaternion.Euler(30f + br * 2f, 0f, 0f);
                yield return null;
            }
        }

        /// <summary>Signature ultimate pose: crouch and gather, leap with the weapon raised high, hold, drop.</summary>
        IEnumerator UltimateRoutine()
        {
            var start = SwordPivot != null ? SwordPivot.localRotation : Quaternion.identity;
            float t = 0f;
            while (t < 0.16f)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / 0.16f);
                Model.localScale = baseScale * Vector3.Lerp(Vector3.one, new Vector3(1.18f, 0.78f, 1.18f), k);
                Model.localPosition = new Vector3(0f, -0.08f * k, 0f);
                Model.localRotation = Quaternion.Euler(14f * k, 0f, 0f);
                if (SwordPivot != null) SwordPivot.localRotation = Quaternion.Slerp(start, Quaternion.Euler(25f, -120f, 0f), k);
                yield return null;
            }
            t = 0f;
            while (t < 0.22f)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / 0.22f);
                float e = 1f - (1f - k) * (1f - k);
                Model.localScale = baseScale * Vector3.Lerp(new Vector3(1.18f, 0.78f, 1.18f), new Vector3(0.86f, 1.2f, 0.86f), e);
                Model.localPosition = new Vector3(0f, 0.4f * e, 0f);
                Model.localRotation = Quaternion.Euler(Mathf.Lerp(14f, -12f, e), 0f, 0f);
                if (SwordPivot != null) SwordPivot.localRotation = Quaternion.Slerp(Quaternion.Euler(25f, -120f, 0f), Quaternion.Euler(-160f, 0f, 0f), e);
                yield return null;
            }
            t = 0f;
            while (t < 0.24f)
            {
                t += Time.deltaTime;
                // Power hold: hover and tremble with energy.
                Model.localPosition = new Vector3(Mathf.Sin(t * 90f) * 0.012f, 0.4f + Mathf.Sin(t * 12f) * 0.02f, 0f);
                Model.localScale = baseScale * Vector3.Lerp(new Vector3(0.86f, 1.2f, 0.86f), Vector3.one, t / 0.24f);
                yield return null;
            }
            t = 0f;
            while (t < 0.12f)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / 0.12f);
                Model.localPosition = new Vector3(0f, 0.4f * (1f - k * k), 0f);
                Model.localRotation = Quaternion.Euler(Mathf.Lerp(-12f, 8f, k), 0f, 0f);
                yield return null;
            }
            Squish(-0.22f);
            Model.localPosition = Vector3.zero;
            Model.localRotation = Quaternion.identity;
            Model.localScale = Vector3.one * baseScale;
            if (SwordPivot != null) SwordPivot.localRotation = swordRest;
        }

        /// <summary>A quick skill flourish in the slayer's shape (a spin, a stomp, a hop or a twirl).</summary>
        IEnumerator SkillFlourish(int index)
        {
            ShapeLanguage sh = shapeSet ? Shape : ShapeLanguage.Triangle;
            float T = 0.28f, t = 0f;
            punchUntil = Time.time + T + 0.05f;
            while (t < T && !dead && !posing)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / T);
                float a = Mathf.Sin(k * Mathf.PI);
                switch (sh)
                {
                    case ShapeLanguage.Square: Model.localScale = baseScale * Vector3.Lerp(Vector3.one, new Vector3(1.16f, 0.82f, 1.16f), a); break;
                    case ShapeLanguage.Circle: Model.localPosition = new Vector3(0f, a * 0.25f, 0f); break;
                    case ShapeLanguage.Diamond: Model.localRotation = Quaternion.Euler(0f, (index % 2 == 0 ? 1f : -1f) * 360f * Mathf.SmoothStep(0f, 1f, k), 0f); break;
                    default: Model.localRotation = Quaternion.Euler(18f * a, 0f, (index % 2 == 0 ? -1f : 1f) * 16f * a); break;
                }
                yield return null;
            }
            if (!posing && !dead)
            {
                Model.localRotation = Quaternion.identity;
                Model.localScale = Vector3.one * baseScale;
            }
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
            // Anticipation → strike → follow-through → settle, inside the same total time so hit timing is unchanged.
            float dir = Mathf.Sign(toYaw - fromYaw);
            if (dir == 0f) dir = 1f;
            float windUp = duration * 0.3f;
            float pull = GameConfig.CartoonStyle ? 34f : 10f, over = GameConfig.CartoonStyle ? 26f : 8f;
            // Our own squash and stretch owns the body scale for this swing.
            if (GameConfig.CartoonStyle) punchUntil = Time.time + duration + 0.12f;
            var from = SwordPivot.localRotation;
            var cocked = Quaternion.Euler(pitch - 8f, fromYaw - dir * pull, roll);
            float t = 0f;
            while (t < windUp)
            {
                t += Time.deltaTime;
                // Reach the cocked pose by 70% of the wind-up and hold it: a readable anticipation beat.
                float w = Mathf.Clamp01(t / (windUp * 0.7f));
                SwordPivot.localRotation = Quaternion.Slerp(from, cocked, w * w * (3f - 2f * w));
                // Anticipation: the body squashes down as it winds up...
                if (GameConfig.CartoonStyle && !dead) Model.localScale = baseScale * Vector3.Lerp(Vector3.one, new Vector3(1.1f, 0.86f, 1.1f), w);
                yield return null;
            }
            if (Trail != null) { Trail.Clear(); Trail.emitting = true; }
            float strike = Mathf.Max(0.01f, duration - windUp);
            float s = 0f;
            while (s < strike)
            {
                s += Time.deltaTime;
                float k = Mathf.Clamp01(s / strike);
                // Ease in-out: gathers speed, whips through the middle, decelerates past the target.
                k = k < 0.5f ? 4f * k * k * k : 1f - Mathf.Pow(-2f * k + 2f, 3f) * 0.5f;
                float yaw = Mathf.LerpUnclamped(fromYaw - dir * pull, toYaw + dir * over, k);
                SwordPivot.localRotation = Quaternion.Euler(Mathf.Lerp(pitch - 8f, pitch, k), yaw, roll);
                // ...then stretches through the strike and settles back (squash and stretch).
                if (GameConfig.CartoonStyle && !dead)
                {
                    float st = Mathf.Sin(Mathf.Clamp01(s / strike) * Mathf.PI);
                    Model.localScale = baseScale * Vector3.Lerp(new Vector3(1.1f, 0.86f, 1.1f), new Vector3(0.9f, 1.16f, 0.9f), Mathf.Clamp01(s / (strike * 0.4f)));
                    if (s > strike * 0.4f) Model.localScale = baseScale * Vector3.Lerp(Vector3.one, new Vector3(0.9f, 1.16f, 0.9f), st);
                }
                yield return null;
            }
            if (GameConfig.CartoonStyle && !dead) { Model.localScale = Vector3.one * baseScale; Squish(-0.1f); } // impact pop
            if (Trail != null) Trail.emitting = false;
            yield return new WaitForSeconds(0.1f);
            // Recovery: back to the ready pose with a little springy overshoot.
            float r = 0f;
            var start = SwordPivot.localRotation;
            while (r < 1f)
            {
                r = Mathf.Min(1f, r + Time.deltaTime * 4.5f);
                SwordPivot.localRotation = Quaternion.SlerpUnclamped(start, swordRest, EaseOutBack(r));
                yield return null;
            }
            SwordPivot.localRotation = swordRest;
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
            bool wasDead = dead;
            dead = false;
            posing = false;
            showcase = false;
            guarding = false;
            StopAllCoroutines();
            swingRoutine = null;
            poseRoutine = null;
            trailRoutine = null;
            Model.localPosition = Vector3.zero;
            Model.localScale = Vector3.one * baseScale;
            if (driver != null)
            {
                driver.SetBool("Guard", false);
                // Revived (PvP respawn): leave the death/knockdown state.
                if (wasDead)
                {
                    int idle = Animator.StringToHash("Idle");
                    if (driver.Has("GetUp")) driver.Trigger("GetUp");
                    else if (driver.Animator.HasState(0, idle)) driver.Animator.Play(idle, 0, 0f);
                }
                return;
            }
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
            // Movement personality: every motion style walks, runs and breathes differently.
            var mp = MotionParams.For(Motion);
            float dt = Time.deltaTime;
            // Everything eases: movement blends smoothly between idle, walk and run instead of snapping.
            smoothMove = Mathf.SmoothDamp(smoothMove, moving, ref smoothMoveVel, 0.12f);
            smoothSprint = Mathf.MoveTowards(smoothSprint, sprinting ? 1f : 0f, dt * 4f);
            float freq = Mathf.Lerp(mp.idleFreq, mp.walkFreq * (1f + 0.35f * smoothSprint), Mathf.Clamp01(smoothMove * 1.5f));
            bob += dt * freq;
            float walkY = Mathf.Abs(Mathf.Sin(bob)) * mp.walkAmp * (1f + 0.2f * smoothSprint);
            float idleY = Mathf.Sin(bob) * mp.idleAmp;
            float y = Mathf.Lerp(idleY, walkY, Mathf.Clamp01(smoothMove * 1.5f));
            if (Hover > 0f) y = Hover + Mathf.Sin(Time.time * 2.2f) * 0.18f;
            Model.localPosition = Vector3.Lerp(Model.localPosition, new Vector3(0f, y, 0f), 1f - Mathf.Exp(-dt * 25f));

            // Bank into turns, with a little overshoot when the turn stops.
            float yaw = transform.eulerAngles.y;
            float turn = Mathf.DeltaAngle(lastYaw, yaw) / Mathf.Max(dt, 0.0001f);
            lastYaw = yaw;
            bank = Mathf.Lerp(bank, Mathf.Clamp(-turn * 0.03f, -14f, 14f) * Mathf.Clamp01(smoothMove * 2f), 1f - Mathf.Exp(-dt * 8f));

            float lean = smoothMove * mp.lean * (1f + 0.7f * smoothSprint) - (guarding ? 8f : 0f) + (1f - Mathf.Clamp01(smoothMove * 3f)) * mp.idleLean;
            float roll = Mathf.Lerp(Mathf.Sin(Time.time * mp.idleFreq * 0.5f) * mp.roll * 0.5f, Mathf.Sin(bob) * mp.roll, Mathf.Clamp01(smoothMove * 1.5f)) + bank;
            float jitter = mp.jitter > 0f ? (Mathf.PerlinNoise(Time.time * 6f, 0f) - 0.5f) * mp.jitter : 0f;
            var e = Model.localEulerAngles;
            Model.localRotation = Quaternion.Slerp(Model.localRotation, Quaternion.Euler(lean, e.y + jitter, roll), 1f - Mathf.Exp(-dt * 14f));

            // Squash on each footfall and a soft breathing stretch at rest.
            float squash = smoothMove > 0.1f ? (1f - Mathf.Abs(Mathf.Sin(bob))) * 0.05f * smoothMove : 0f;
            float breathe = Mathf.Sin(Time.time * 2f) * 0.012f * (1f - Mathf.Clamp01(smoothMove * 2f));
            if (Time.time > punchUntil)
                Model.localScale = Vector3.Lerp(Model.localScale, new Vector3(1f + squash * 0.6f, 1f - squash + breathe, 1f + squash * 0.6f) * baseScale, 1f - Mathf.Exp(-dt * 20f));
            if (moving > 0.1f && mp.stomp && Mathf.Abs(Mathf.Sin(bob)) < 0.08f && Time.time - lastStomp > 0.2f)
            {
                lastStomp = Time.time;
                VFX.Dust(transform.position, 2);
            }
            UpdateFidget(mp);
            UpdateFlash();
        }

        float lastStomp;
        float nextFidget = -1f;
        float smoothMove, smoothMoveVel, smoothSprint, lastYaw, bank;

        /// <summary>Idle personality: glances, nods, hops and weapon flourishes while standing still.</summary>
        void UpdateFidget(MotionParams mp)
        {
            if (head == null) return;
            if (nextFidget < 0f) nextFidget = Time.time + Random.Range(3f, 7f);
            float look = Mathf.Sin(Time.time * (Motion == MotionStyle.Nervous ? 2.2f : 0.6f)) * mp.headLook;
            head.localRotation = Quaternion.Slerp(head.localRotation, Quaternion.Euler(0f, moving > 0.1f ? 0f : look, Motion == MotionStyle.Sly ? 8f : 0f), Time.deltaTime * 6f);
            if (moving > 0.1f || Time.time < nextFidget || !FidgetsEnabled) return;
            nextFidget = Time.time + Random.Range(5f, 10f);
            switch (Motion)
            {
                case MotionStyle.Light: StartPose(HopRoutine(0.35f)); break;
                case MotionStyle.Nervous: StartPose(HopRoutine(0.15f)); break;
                case MotionStyle.Aggressive: HeavyAttack(0.2f); VFX.Dust(transform.position + transform.forward, 5); break;
                case MotionStyle.Graceful: Spin(0.6f); break;
                case MotionStyle.Confident: Swing(-120f, 200f, 0.35f, 60f); break;
                case MotionStyle.Stoic: StartPose(NodRoutine()); break;
                case MotionStyle.Sly: Swing(40f, -40f, 0.5f, 10f); break;
                default: Swing(-60f, 60f, 0.3f, 30f); break;
            }
        }

        /// <summary>Idle flourishes (off in battle, where the slayer's pose must stay readable).</summary>
        public bool FidgetsEnabled = true;

        System.Collections.IEnumerator HopRoutine(float height)
        {
            float t = 0f;
            while (t < 0.3f)
            {
                t += Time.deltaTime;
                Model.localPosition = new Vector3(0f, Mathf.Sin(t / 0.3f * Mathf.PI) * height, 0f);
                yield return null;
            }
            Model.localPosition = Vector3.zero;
        }

        System.Collections.IEnumerator NodRoutine()
        {
            float t = 0f;
            while (t < 0.6f)
            {
                t += Time.deltaTime;
                if (head != null) head.localRotation = Quaternion.Euler(Mathf.Sin(t / 0.6f * Mathf.PI) * 18f, 0f, 0f);
                yield return null;
            }
        }

        /// <summary>Plays a named animation for the character viewer: idle, walk, run, attack, heavy, special, hit, victory, defeat.</summary>
        public void PlayPreview(string anim)
        {
            StopAllCoroutines();
            posing = false;
            ResetPose();
            switch (anim)
            {
                case "walk": StartCoroutine(PreviewMove(0.6f, false)); break;
                case "run": StartCoroutine(PreviewMove(1f, true)); break;
                case "attack": StartCoroutine(PreviewCombo()); break;
                case "heavy": HeavyAttack(0.22f); break;
                case "special": StartCoroutine(PreviewSpecial()); break;
                case "hit": Hit(transform.forward); break;
                case "victory": Victory(); break;
                case "defeat": Defeat(); break;
                default: nextFidget = Time.time; break;
            }
        }

        System.Collections.IEnumerator PreviewMove(float amount, bool sprint)
        {
            float t = 0f;
            while (t < 2.2f) { t += Time.deltaTime; SetMoving(amount, sprint); yield return null; }
            SetMoving(0f);
        }

        System.Collections.IEnumerator PreviewCombo()
        {
            for (int i = 0; i < 5; i++)
            {
                Attack(i, 0.12f);
                yield return new WaitForSeconds(i == 4 ? 0.45f : 0.26f);
            }
        }

        System.Collections.IEnumerator PreviewSpecial()
        {
            SetCharge(1f, Color.white);
            yield return new WaitForSeconds(0.9f);
            SetCharge(0f, Color.white);
            Spin(0.35f, 2);
            yield return new WaitForSeconds(0.4f);
            HeavyAttack(0.2f);
            yield return new WaitForSeconds(0.5f);
            Victory();
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
