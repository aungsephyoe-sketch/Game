using System.Collections.Generic;
using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// The animated home screen: Kiriha at night — moonlight, warm paper lanterns that flicker, cherry blossoms
    /// drifting, light fog — with the team leader standing in the foreground in their natural idle (breathing,
    /// weight shift, glances, blinking, hair and cloth moving), a soft contact shadow and a rim light that lifts
    /// them off the background. Villagers stroll in the distance. The same stage doubles as the 3D character
    /// viewer (drag to rotate, preview skills) and the team line-up.
    /// </summary>
    public class HomeStage : MonoBehaviour
    {
        GameObject world;
        Transform heroHolder;

        /// <summary>World position just above the home leader's head (for the greeting bubble).</summary>
        public Vector3 LeaderHead { get { return heroHolder != null ? heroHolder.position + Vector3.up * 2.4f : Vector3.up * 2.4f; } }
        public bool LeaderVisible { get { return heroHolder != null && heroHolder.gameObject.activeInHierarchy; } }
        public Vector3 LeaderFeet { get { return heroHolder != null ? heroHolder.position : Vector3.zero; } }

        float leaderSpin, leaderSpinAt = -10f;

        /// <summary>Turns the featured slayer (drag on the home screen); after a few seconds they turn back to you.</summary>
        public void RotateLeader(float degrees)
        {
            leaderSpin += degrees;
            leaderSpinAt = Time.time;
        }
        CharacterVisual hero;
        CharacterDefinition heroDef;
        string shownId = "";
        bool viewer;
        float viewerYaw = 180f;
        float nextLook;
        float yawTarget = 180f;

        static readonly Vector3 HeroPos = new Vector3(1.4f, 0f, 0f);

        public void ShowHome(PlayerData data)
        {
            viewer = false;
            ClearLineup();
            EnsureWorld();
            SetHero(data.team.Count > 0 ? data.team[0] : GameDatabase.Protagonist);
            gameObject.SetActive(true);
            PrototypeWorld.ApplyVillageLighting();
        }

        public void ShowViewer(string characterId)
        {
            viewer = true;
            ClearLineup();
            EnsureWorld();
            if (shownId != characterId) viewerYaw = 180f;
            SetHero(characterId);
            gameObject.SetActive(true);
            PrototypeWorld.ApplyVillageLighting();
        }

        public void Hide() { gameObject.SetActive(false); }

        public void RotateViewer(float degrees) { viewerYaw += degrees; }

        /// <summary>Turns the viewer model to face the camera (front) or away from it (back).</summary>
        public void FaceViewer(bool back) { viewerYaw = back ? 0f : 180f; }

        /// <summary>Plays one of the slayer's animations on the viewer model (idle, walk, run, attack, heavy,
        /// special, hit, victory, defeat).</summary>
        public void PreviewAnim(string anim)
        {
            if (hero == null || heroDef == null) return;
            hero.PlayPreview(anim);
            if (anim == "special")
            {
                Color c = ElementChart.ColorOf(heroDef.element);
                VFX.Breath(heroHolder.position, c, 60);
                if (GameManager.Instance != null) GameManager.Instance.Audio.Play("buildup", 0.5f);
            }
            else if ((anim == "attack" || anim == "heavy") && GameManager.Instance != null)
                GameManager.Instance.Audio.PlayVaried(anim == "heavy" ? "slashHeavy" : "slash", 0.5f, 0.1f);

        }

        /// <summary>Plays the selected skill's motion and elemental effects on the viewer model.</summary>
        public void PreviewSkill(int index)
        {
            if (hero == null || heroDef == null) return;
            Color c = ElementChart.ColorOf(heroDef.element);
            var ab = index < 3 ? heroDef.skills[index] : heroDef.ultimate;
            Vector3 p = heroHolder.position;
            switch (ab.shape)
            {
                case AbilityShape.Spin: hero.Spin(0.35f, 2); VFX.Shockwave(p, 3f, c, 0.5f); break;
                case AbilityShape.Wave: hero.HeavyAttack(0.2f); VFX.Slash(p, heroHolder.forward, 3.5f, 160f, 0f, c, 0.4f); break;
                case AbilityShape.Dash: hero.DashAttack(0.2f); VFX.Flash(MeshFactory.Line(), p + Vector3.up, heroHolder.rotation, new Vector3(1.2f, 1f, 4f), new Vector3(0.05f, 1f, 4f), c, 0.4f); break;
                case AbilityShape.Heal: hero.Victory(); VFX.BurstDisc(p, 3f, new Color(0.5f, 1f, 0.6f), 0.8f); break;
                default: hero.Spin(0.3f); VFX.BurstDisc(p, 3.5f, c, 0.6f); break;
            }
            VFX.Breath(p, c, index == 3 ? 80 : 35);
            if (index == 3) { VFX.Pillar(p, c, 7f, 0.8f); VFX.ImpactLight(p + Vector3.up, c, 8f, 0.6f); }
            if (GameManager.Instance != null) GameManager.Instance.Audio.Play(index == 3 ? "ultimate" : "skill", 0.6f);

        }

        /// <summary>Celebration burst for level-ups and ascensions.</summary>
        public void Celebrate(Color color)
        {
            if (heroHolder == null) return;
            VFX.Pillar(heroHolder.position, color, 6f, 0.9f);
            VFX.Breath(heroHolder.position, color, 60);
            VFX.Shockwave(heroHolder.position, 3f, color, 0.6f);
            if (hero != null) hero.Victory();

        }

        void EnsureWorld()
        {
            if (world != null) return;
            world = new GameObject("HomeWorld");
            world.transform.SetParent(transform, false);
            // The real Kiriha Village (the same world as the open-world hub): the street up to the plaza and its
            // great cherry tree, townhouses with glowing windows, lantern strings, the moon and stars.
            try { PrototypeWorld.BuildVillage(Journey.Village(), world.transform, false); }
            catch (System.Exception ex)
            {
                Debug.LogError("[Home] village build failed, using the simple backdrop: " + ex);
                foreach (Transform c in world.transform) Destroy(c.gameObject);
                ArenaBuilder.Build(NightVillage(), world.transform, false, 3);
                LanternStrings(world.transform);
            }
            string[] lines =
            {
                "Ren! Training again? You'll wear out the post!", "The harvest looks good this year.",
                "Have you seen the sky lately? Such strange colours...", "Master Tessai says you're almost ready.",
                "Grandma's making rice cakes tonight!", "They say the capital has an arena. Imagine!"
            };
            // Villagers stroll in the lane behind the leader, short of the tree.
            for (int i = 0; i < Mathf.RoundToInt(5 * GameSettings.SceneryDensity) + 1; i++)
                walkers.Add(NpcWalker.Spawn(world.transform, i % 2 == 0 ? "npc_villager" : "npc_villager2", new Vector3(0f, 0f, 8.5f), 2.8f, 5.5f, lines));
            heroHolder = new GameObject("HeroHolder").transform;
            heroHolder.SetParent(transform, false);
            heroHolder.position = HeroPos;
            heroHolder.rotation = Quaternion.Euler(0f, HomeYaw, 0f);
            // A soft contact shadow under the feet and a cool rim light behind, so the hero sits in the scene.
            var blob = MeshFactory.MeshObject(MeshFactory.PlanarDisc(), transform, HeroPos + Vector3.up * 0.015f, new Vector3(0.75f, 1f, 0.6f), MaterialFactory.Transparent(new Color(0f, 0f, 0.03f, 0.42f)), false);
            blob.name = "HeroShadow";
            heroShadow = blob.transform;
            var rim = new GameObject("HeroRim").AddComponent<Light>();
            rim.transform.SetParent(transform, false);
            rim.transform.position = HeroPos + new Vector3(0.9f, 2.3f, 1.6f);
            rim.type = LightType.Point;
            rim.color = new Color(0.6f, 0.75f, 1f);
            rim.range = 4.5f;
            rim.intensity = 1.6f;
            rim.shadows = LightShadows.None;
            heroRim = rim;
        }

        Transform heroShadow;
        Light heroRim;

        /// <summary>The leader stands turned a little toward the player (three-quarter view).</summary>
        const float HomeYaw = 196f;

        /// <summary>Kiriha by night: moonlight, warm lanterns, cherry blossoms, a little fog.</summary>
        static ArenaTheme NightVillage()
        {
            return new ArenaTheme
            {
                kind = EnvironmentKind.Village, night = true, burning = false, petals = true,
                ground = new Color(0.22f, 0.24f, 0.3f), groundAccent = new Color(0.36f, 0.32f, 0.38f),
                sky = new Color(0.1f, 0.12f, 0.26f), fog = new Color(0.2f, 0.18f, 0.34f), fogStart = 16f, fogEnd = 70f,
                lantern = new Color(1f, 0.66f, 0.32f), foliage = new Color(1f, 0.68f, 0.82f),
                sun = new Color(0.66f, 0.74f, 1f), sunIntensity = 0.95f
            };
        }

        /// <summary>Strings of paper lanterns across the lane behind the hero, each gently swinging and flickering.</summary>
        void LanternStrings(Transform root)
        {
            var paper = MaterialFactory.Toon(new Color(1f, 0.55f, 0.3f), 0.01f, new Color(0.9f, 0.4f, 0.15f));
            var paperB = MaterialFactory.Toon(new Color(1f, 0.82f, 0.45f), 0.01f, new Color(0.85f, 0.6f, 0.25f));
            var cord = MaterialFactory.Toon(new Color(0.15f, 0.1f, 0.08f), 0f);
            int lightsLeft = Mathf.Min(4, GameSettings.MaxDynamicLights);
            for (int row = 0; row < 2; row++)
            {
                float z = 6f + row * 5f, y = 3.4f + row * 0.4f;
                var line = MeshFactory.MeshObject(MeshFactory.SmoothCylinder(), root, new Vector3(0.5f, y + 0.25f, z), new Vector3(0.02f, 6.5f, 0.02f), cord, false);
                line.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                for (int i = 0; i < 7; i++)
                {
                    float x = -5.5f + i * 2f + row;
                    var l = new GameObject("PaperLantern").transform;
                    l.SetParent(root, false);
                    l.position = new Vector3(x, y + 0.2f, z);
                    var sw = l.gameObject.AddComponent<Sway>();
                    sw.Amount = 4f;
                    sw.Speed = 0.6f + i * 0.07f;
                    var body = MeshFactory.MeshObject(MeshFactory.SmoothSphere(), l, new Vector3(0f, -0.35f, 0f), new Vector3(0.42f, 0.52f, 0.42f), (i + row) % 2 == 0 ? paper : paperB, false);
                    body.name = "Glow";
                    MeshFactory.MeshObject(MeshFactory.SmoothCylinder(), l, new Vector3(0f, -0.08f, 0f), new Vector3(0.22f, 0.03f, 0.22f), cord, false);
                    MeshFactory.MeshObject(MeshFactory.SmoothCylinder(), l, new Vector3(0f, -0.62f, 0f), new Vector3(0.22f, 0.03f, 0.22f), cord, false);
                    if (i % 3 == 1 && lightsLeft-- > 0)
                    {
                        var lg = new GameObject("Light");
                        lg.transform.SetParent(l, false);
                        lg.transform.localPosition = new Vector3(0f, -0.35f, 0f);
                        var light = lg.AddComponent<Light>();
                        light.type = LightType.Point;
                        light.color = new Color(1f, 0.62f, 0.3f);
                        light.range = 6f;
                        light.intensity = 1.1f;
                        light.shadows = LightShadows.None;
                        lg.AddComponent<LanternFlicker>();
                    }
                }
            }
        }

        void SetHero(string id)
        {
            if (id == shownId && hero != null) return;
            if (hero != null) Destroy(hero.gameObject);
            heroDef = GameDatabase.GetCharacter(id);
            if (heroDef == null) return;
            hero = CharacterVisual.BuildHero(heroDef, heroHolder);
            shownId = id;

            VFX.Breath(heroHolder.position, ElementChart.ColorOf(heroDef.element), 25);
        }

        // ------------------------------------------------------------------ Team line-up

        GameObject lineupRoot;
        string lineupKey = "";
        readonly List<Transform> lineupSlots = new List<Transform>();
        readonly List<CharacterVisual> lineupVisuals = new List<CharacterVisual>();
        public bool LineupActive { get { return lineupRoot != null; } }

        /// <summary>Where slot i stands (feet), for the UI to place names and stats under each slayer.</summary>
        public Vector3 LineupSlot(int i)
        {
            if (lineupCount <= 3) return new Vector3(-1.85f + i * 2.15f, PodiumHeight, 0.4f);
            // Roster line-up: a shallow arc, the ends a little closer to the camera, like a promotional group shot.
            float x = LineupCamera.x + (i - (lineupCount - 1) * 0.5f) * 1.8f;
            float dx = x - LineupCamera.x;
            return new Vector3(x, PodiumHeight, 0.6f - dx * dx * 0.045f);
        }

        int lineupCount = 3;
        bool silhouette;
        readonly Dictionary<Renderer, Material[]> silhouetteSaved = new Dictionary<Renderer, Material[]>();
        Material silhouetteMat;

        /// <summary>Roster check: every slayer drawn as a flat black shape (can you still tell them apart?).</summary>
        public bool Silhouette
        {
            get { return silhouette; }
            set
            {
                if (silhouette == value) return;
                silhouette = value;
                if (silhouetteMat == null)
                {
                    silhouetteMat = MaterialFactory.Toon(new Color(0.02f, 0.02f, 0.03f), 0f, new Color(0.02f, 0.02f, 0.03f));
                    if (silhouetteMat.HasProperty("_RimColor")) silhouetteMat.SetColor("_RimColor", Color.black);
                    if (silhouetteMat.HasProperty("_ShadowColor")) silhouetteMat.SetColor("_ShadowColor", Color.black);
                }
                if (value)
                {
                    foreach (var v in lineupVisuals)
                    {
                        if (v == null) continue;
                        foreach (var rend in v.GetComponentsInChildren<Renderer>(true))
                        {
                            silhouetteSaved[rend] = rend.sharedMaterials;
                            var m0 = rend.sharedMaterial;
                            bool glow = !(rend is MeshRenderer) || (m0 != null && m0.shader != null && (m0.shader.name.Contains("Additive") || m0.shader.name.Contains("Transparent")));
                            if (glow) { rend.enabled = false; continue; }
                            var mats = new Material[rend.sharedMaterials.Length];
                            for (int k = 0; k < mats.Length; k++) mats[k] = silhouetteMat;
                            rend.sharedMaterials = mats;
                        }
                    }
                }
                else RestoreSilhouette();
            }
        }

        void RestoreSilhouette()
        {
            foreach (var kv in silhouetteSaved)
            {
                if (kv.Key == null) continue;
                kv.Key.sharedMaterials = kv.Value;
                kv.Key.enabled = true;
            }
            silhouetteSaved.Clear();
            silhouette = false;
        }

        const float PodiumHeight = 0.28f;

        /// <summary>Line-up camera position (see Update): each slayer is turned to face it.</summary>
        static readonly Vector3 LineupCamera = new Vector3(0.3f, 2.2f, -6.6f);
        static readonly Vector3 RosterCamera = new Vector3(0.3f, 2.7f, -11f);
        Vector3 CurrentLineupCamera { get { return lineupCount > 3 ? RosterCamera : LineupCamera; } }

        /// <summary>Facing the camera, turned a little (three-quarter view) toward the middle of the group.</summary>
        float LineupYaw(int i)
        {
            Vector3 p = LineupSlot(i);
            Vector3 cam = CurrentLineupCamera;
            float face = Mathf.Atan2(cam.x - p.x, cam.z - p.z) * Mathf.Rad2Deg;
            float centre = cam.x;
            float turn = Mathf.Abs(p.x - centre) < 0.5f ? -6f : (p.x < centre ? -14f : 14f);
            return face + turn;
        }

        /// <summary>The team stands side by side in the village, facing the camera (TEAM screen).</summary>
        public void ShowLineup(List<string> ids)
        {
            string key = string.Join(",", ids.ToArray());
            if (lineupRoot != null && key == lineupKey) return;
            ClearLineup();
            EnsureWorld();
            lineupKey = key;
            lineupCount = Mathf.Max(3, ids.Count);
            lineupRoot = new GameObject("Lineup");
            lineupRoot.transform.SetParent(transform, false);
            if (heroHolder != null) heroHolder.gameObject.SetActive(false);
            for (int i = 0; i < lineupCount; i++)
            {
                var slot = new GameObject("Slot" + i).transform;
                slot.SetParent(lineupRoot.transform, false);
                slot.position = LineupSlot(i);
                // A stone podium with a ring in the slayer's element colour.
                Color ec = new Color(0.7f, 0.7f, 0.75f);
                if (i < ids.Count) { var dd = GameDatabase.GetCharacter(ids[i]); if (dd != null) ec = ElementChart.ColorOf(dd.element); }
                var stone = MaterialFactory.Toon(new Color(0.42f, 0.4f, 0.42f), 0.02f);
                var stoneTop = MaterialFactory.Toon(new Color(0.58f, 0.56f, 0.56f), 0.01f);
                var pod = MeshFactory.MeshObject(MeshFactory.FacetCylinder(12), lineupRoot.transform, Vector3.zero, new Vector3(1.5f, PodiumHeight - 0.04f, 1.5f), stone);
                pod.transform.position = new Vector3(slot.position.x, 0f, slot.position.z);
                var top = MeshFactory.MeshObject(MeshFactory.FacetCylinder(12), lineupRoot.transform, Vector3.zero, new Vector3(1.38f, 0.04f, 1.38f), stoneTop);
                top.transform.position = new Vector3(slot.position.x, PodiumHeight - 0.04f, slot.position.z);
                var ringGo = MeshFactory.MeshObject(MeshFactory.Ring(0.86f), lineupRoot.transform, Vector3.zero, new Vector3(0.66f, 1f, 0.66f), MaterialFactory.Additive(new Color(ec.r, ec.g, ec.b, 0.8f)), false);
                ringGo.transform.position = slot.position + Vector3.up * 0.01f;
                ringGo.AddComponent<Pulse>().Speed = 2f + i * 0.3f;
                var lg = new GameObject("PodiumLight");
                lg.transform.SetParent(lineupRoot.transform, false);
                lg.transform.position = slot.position + new Vector3(0f, 2.4f, -0.8f);
                var l = lg.AddComponent<Light>();
                l.type = LightType.Point;
                l.color = Color.Lerp(ec, Color.white, 0.5f);
                l.range = 3.2f;
                l.intensity = 0.8f;
                // Everyone faces the camera with the same slight three-quarter turn in toward the group's centre.
                slot.rotation = Quaternion.Euler(0f, LineupYaw(i), 0f);
                lineupSlots.Add(slot);
                CharacterVisual v = null;
                if (i < ids.Count)
                {
                    var def = GameDatabase.GetCharacter(ids[i]);
                    if (def != null)
                    {
                        v = CharacterVisual.BuildHero(def, slot);
                        v.HoldTeamIdle();
                        VFX.Breath(slot.position, ElementChart.ColorOf(def.element), 12);
                        // Aura: a soft glowing column in the element colour, red and brighter at max level.
                        var owned = GameManager.Instance != null ? GameManager.Instance.Data.GetCharacter(def.id) : null;
                        bool maxed = owned != null && ExperienceSystem.IsMaxed(owned);
                        bool god = owned != null && ExperienceSystem.IsGod(owned);
                        Color ac = god ? new Color(0.8f, 0.4f, 1f) : maxed ? new Color(1f, 0.12f, 0.08f) : ElementChart.ColorOf(def.element);
                        float aa = maxed ? 0.16f : 0.08f;
                        for (int layer = 0; layer < 2; layer++)
                        {
                            float rad = 1.1f + layer * 0.35f;
                            var col = MeshFactory.MeshObject(MeshFactory.FacetCylinder(16), lineupRoot.transform, Vector3.zero, new Vector3(rad, 2.1f - layer * 0.4f, rad), MaterialFactory.Additive(new Color(ac.r, ac.g, ac.b, aa - layer * 0.03f)), false);
                            col.transform.position = slot.position;
                            var pu = col.AddComponent<Pulse>();
                            pu.Speed = (maxed ? 4f : 2f) + layer;
                            pu.Amount = 0.06f;
                        }
                        var aring = MeshFactory.MeshObject(MeshFactory.Ring(0.9f), lineupRoot.transform, Vector3.zero, new Vector3(0.75f, 1f, 0.75f), MaterialFactory.Additive(new Color(ac.r, ac.g, ac.b, maxed ? 0.9f : 0.5f)), false);
                        aring.transform.position = slot.position + Vector3.up * 0.03f;
                        aring.AddComponent<Pulse>().Speed = maxed ? 6f : 3f;
                    }
                }
                lineupVisuals.Add(v);
            }
        }

        readonly List<float> slotSpin = new List<float>();

        /// <summary>Turns the slayer in line-up slot i (drag to rotate on the TEAM screen).</summary>
        public void RotateLineupSlot(int i, float degrees)
        {
            while (slotSpin.Count <= i) slotSpin.Add(0f);
            slotSpin[i] += degrees;
        }

        public void ClearLineup()
        {
            slotSpin.Clear();
            RestoreSilhouette();
            if (lineupRoot != null) Destroy(lineupRoot);
            lineupRoot = null;
            lineupKey = "";
            lineupSlots.Clear();
            lineupVisuals.Clear();
            if (heroHolder != null) heroHolder.gameObject.SetActive(true);
        }

        /// <summary>A little flourish from the slayer in slot i (when picked in the UI).</summary>
        public void LineupCheer(int i)
        {
            if (i < 0 || i >= lineupVisuals.Count || lineupVisuals[i] == null) return;
            lineupVisuals[i].Victory();
            VFX.Breath(lineupSlots[i].position, Color.white, 15);
        }

        readonly List<NpcWalker> walkers = new List<NpcWalker>();

        /// <summary>Villagers step out of shot while the team line-up or the character viewer is on screen.</summary>
        void KeepWalkersOutOfShot()
        {
            bool framing = lineupRoot != null || viewer;
            foreach (var w in walkers)
            {
                if (w == null) continue;
                bool inFront = w.transform.position.z < 2.5f && Mathf.Abs(w.transform.position.x) < 9f;
                bool show = !(framing && inFront);
                if (w.gameObject.activeSelf != show) w.gameObject.SetActive(show);
            }
        }

        void Update()
        {
            if (heroHolder == null) return;
            KeepWalkersOutOfShot();
            float t = Time.unscaledTime;
            if (lineupRoot != null)
            {
                // After a cheer, each slayer settles back into their own pose.
                for (int i = 0; i < lineupVisuals.Count; i++)
                    if (lineupVisuals[i] != null && !lineupVisuals[i].Posing) lineupVisuals[i].HoldTeamIdle();
                // Dragged slayers turn smoothly to where the player spun them.
                for (int i = 0; i < lineupSlots.Count; i++)
                {
                    float spin = i < slotSpin.Count ? slotSpin[i] : 0f;
                    var want = Quaternion.Euler(0f, LineupYaw(i) + spin, 0f);
                    lineupSlots[i].rotation = Quaternion.Slerp(lineupSlots[i].rotation, want, 1f - Mathf.Exp(-Time.unscaledDeltaTime * 12f));
                }
                if (CameraController.Instance != null)
                    CameraController.Instance.SetFixed(CurrentLineupCamera + new Vector3(Mathf.Sin(t * 0.15f) * 0.15f, 0f, 0f), new Vector3(0.3f, lineupCount > 3 ? 1.3f : 1.2f, 0.4f));
                return;
            }
            if (viewer)
            {
                heroHolder.rotation = Quaternion.Slerp(heroHolder.rotation, Quaternion.Euler(0f, viewerYaw, 0f), Time.unscaledDeltaTime * 10f);
                if (CameraController.Instance != null)
                    CameraController.Instance.SetFixed(HeroPos + new Vector3(1.25f + Mathf.Sin(t * 0.3f) * 0.08f, 1.45f, -4.2f), HeroPos + new Vector3(1.25f, 1.1f, 0f));
            }
            else
            {
                // The featured slayer simply stands in their natural idle (breathing, weight shift, glances, blinks);
                // hair and cloth move on their own. They turn a little toward the player now and then.
                if (Time.time > nextLook)
                {
                    nextLook = Time.time + Random.Range(5f, 9f);
                    yawTarget = HomeYaw + Random.Range(-6f, 6f);
                }
                if (Time.time - leaderSpinAt > 6f) leaderSpin = Mathf.Lerp(leaderSpin, Mathf.Round(leaderSpin / 360f) * 360f, 1f - Mathf.Exp(-Time.deltaTime * 1.2f));
                bool spinning = Time.time - leaderSpinAt < 0.3f;
                heroHolder.rotation = Quaternion.Slerp(heroHolder.rotation, Quaternion.Euler(0f, yawTarget + leaderSpin, 0f), 1f - Mathf.Exp(-Time.deltaTime * (spinning ? 14f : 1.5f)));
                if (hero != null && !hero.Posing) hero.HoldTeamIdle();
                if (CameraController.Instance != null)
                {
                    Vector3 drift = new Vector3(Mathf.Sin(t * 0.11f) * 0.12f, Mathf.Sin(t * 0.17f) * 0.05f, 0f);
                    CameraController.Instance.SetFixed(new Vector3(0.35f, 1.6f, -5.3f) + drift, new Vector3(0.35f, 1.12f, 0f));
                }
            }
            if (heroShadow != null) heroShadow.gameObject.SetActive(heroHolder.gameObject.activeInHierarchy);
            if (heroRim != null) heroRim.enabled = heroHolder.gameObject.activeInHierarchy;
        }

        void ResetHeroPose()
        {
            if (hero != null) hero.ResetPose();
        }
    }
}
