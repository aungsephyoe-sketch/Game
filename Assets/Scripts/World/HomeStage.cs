using System.Collections.Generic;
using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// The animated home screen: Kiriha at golden hour with the team leader in the foreground. The hero breathes,
    /// looks around and occasionally practises a form; villagers stroll and chat, birds circle, leaves fall and a
    /// campfire crackles. The same stage doubles as the 3D character viewer (drag to rotate, preview skills).
    /// </summary>
    public class HomeStage : MonoBehaviour
    {
        GameObject world;
        Transform heroHolder;
        CharacterVisual hero;
        CharacterDefinition heroDef;
        string shownId = "";
        bool viewer;
        float viewerYaw = 180f;
        float nextFlourish;
        float nextLook;
        float yawTarget = 180f;
        ArenaTheme theme;

        static readonly Vector3 HeroPos = new Vector3(1.4f, 0f, 0f);

        public void ShowHome(PlayerData data)
        {
            viewer = false;
            ClearLineup();
            EnsureWorld();
            SetHero(data.team.Count > 0 ? data.team[0] : GameDatabase.Protagonist);
            gameObject.SetActive(true);
            ArenaBuilder.ApplyLighting(theme);
        }

        public void ShowViewer(string characterId)
        {
            viewer = true;
            ClearLineup();
            EnsureWorld();
            if (shownId != characterId) viewerYaw = 180f;
            SetHero(characterId);
            gameObject.SetActive(true);
            ArenaBuilder.ApplyLighting(theme);
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
            nextFlourish = Time.time + 8f;
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
            nextFlourish = Time.time + 6f;
        }

        /// <summary>Celebration burst for level-ups and ascensions.</summary>
        public void Celebrate(Color color)
        {
            if (heroHolder == null) return;
            VFX.Pillar(heroHolder.position, color, 6f, 0.9f);
            VFX.Breath(heroHolder.position, color, 60);
            VFX.Shockwave(heroHolder.position, 3f, color, 0.6f);
            if (hero != null) hero.Victory();
            nextFlourish = Time.time + 4f;
        }

        void EnsureWorld()
        {
            if (world != null) return;
            theme = CutsceneDatabase.PeacefulVillage();
            world = new GameObject("HomeWorld");
            world.transform.SetParent(transform, false);
            ArenaBuilder.Build(theme, world.transform, false, 3);
            // Campfire, training posts and villagers.
            EnvFx.Fire(world.transform, new Vector3(-2.5f, 0f, 3.5f), 1f);
            var wood = MaterialFactory.Toon(new Color(0.35f, 0.24f, 0.15f));
            for (int i = 0; i < 5; i++)
            {
                var log = MeshFactory.Primitive(PrimitiveType.Cylinder, world.transform, new Vector3(-2.5f, 0.1f, 3.5f), new Vector3(0.18f, 0.5f, 0.18f), wood);
                log.transform.localRotation = Quaternion.Euler(80f, i * 72f, 0f);
            }
            var post = MeshFactory.Primitive(PrimitiveType.Cylinder, world.transform, new Vector3(4f, 0.9f, 3f), new Vector3(0.3f, 0.9f, 0.3f), wood);
            MeshFactory.Primitive(PrimitiveType.Cube, post.transform, new Vector3(0f, 0.3f, 0f), new Vector3(4f, 0.12f, 0.6f), wood);
            string[] lines =
            {
                "Ren! Training again? You'll wear out the post!", "The harvest looks good this year.",
                "Have you seen the sky lately? Such strange colours...", "Master Tessai says you're almost ready.",
                "Grandma's making rice cakes tonight!", "They say the capital has an arena. Imagine!"
            };
            for (int i = 0; i < Mathf.RoundToInt(5 * GameSettings.SceneryDensity) + 1; i++)
                NpcWalker.Spawn(world.transform, i % 2 == 0 ? "npc_villager" : "npc_villager2", Vector3.zero, 7f, 13f, lines);
            heroHolder = new GameObject("HeroHolder").transform;
            heroHolder.SetParent(transform, false);
            heroHolder.position = HeroPos;
            heroHolder.rotation = Quaternion.Euler(0f, 180f, 0f);
        }

        void SetHero(string id)
        {
            if (id == shownId && hero != null) return;
            if (hero != null) Destroy(hero.gameObject);
            heroDef = GameDatabase.GetCharacter(id);
            if (heroDef == null) return;
            hero = CharacterVisual.BuildHero(heroDef, heroHolder);
            shownId = id;
            nextFlourish = Time.time + 3f;
            VFX.Breath(heroHolder.position, ElementChart.ColorOf(heroDef.element), 25);
        }

        // ------------------------------------------------------------------ Team line-up

        GameObject lineupRoot;
        string lineupKey = "";
        readonly List<Transform> lineupSlots = new List<Transform>();
        readonly List<CharacterVisual> lineupVisuals = new List<CharacterVisual>();
        public bool LineupActive { get { return lineupRoot != null; } }

        /// <summary>Where slot i stands (feet), for the UI to place names and stats under each slayer.</summary>
        public Vector3 LineupSlot(int i) { return new Vector3(-1.85f + i * 2.15f, PodiumHeight, 0.4f); }

        const float PodiumHeight = 0.28f;

        /// <summary>The team stands side by side in the village, facing the camera (TEAM screen).</summary>
        public void ShowLineup(List<string> ids)
        {
            string key = string.Join(",", ids.ToArray());
            if (lineupRoot != null && key == lineupKey) return;
            ClearLineup();
            EnsureWorld();
            lineupKey = key;
            lineupRoot = new GameObject("Lineup");
            lineupRoot.transform.SetParent(transform, false);
            if (heroHolder != null) heroHolder.gameObject.SetActive(false);
            for (int i = 0; i < 3; i++)
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
                slot.rotation = Quaternion.Euler(0f, 180f + (i - 1.5f) * -6f, 0f);
                lineupSlots.Add(slot);
                CharacterVisual v = null;
                if (i < ids.Count)
                {
                    var def = GameDatabase.GetCharacter(ids[i]);
                    if (def != null)
                    {
                        v = CharacterVisual.BuildHero(def, slot);
                        v.HoldSignaturePose();
                        VFX.Breath(slot.position, ElementChart.ColorOf(def.element), 12);
                        // Aura: a soft glowing column in the element colour, red and brighter at max level.
                        var owned = GameManager.Instance != null ? GameManager.Instance.Data.GetCharacter(def.id) : null;
                        bool maxed = owned != null && ExperienceSystem.IsMaxed(owned);
                        Color ac = maxed ? new Color(1f, 0.12f, 0.08f) : ElementChart.ColorOf(def.element);
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

        public void ClearLineup()
        {
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

        void Update()
        {
            if (heroHolder == null) return;
            float t = Time.unscaledTime;
            if (lineupRoot != null)
            {
                // After a cheer, each slayer settles back into their own pose.
                for (int i = 0; i < lineupVisuals.Count; i++)
                    if (lineupVisuals[i] != null && !lineupVisuals[i].Posing) lineupVisuals[i].HoldSignaturePose();
                if (CameraController.Instance != null)
                    CameraController.Instance.SetFixed(new Vector3(0.3f + Mathf.Sin(t * 0.15f) * 0.15f, 2.2f, -6.6f), new Vector3(0.3f, 1.2f, 0.4f));
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
                // Idle life: glance around, and every so often practise a form.
                if (Time.time > nextLook)
                {
                    nextLook = Time.time + Random.Range(2.5f, 5f);
                    yawTarget = 180f + Random.Range(-35f, 35f);
                }
                heroHolder.rotation = Quaternion.Slerp(heroHolder.rotation, Quaternion.Euler(0f, yawTarget, 0f), Time.deltaTime * 2f);
                if (hero != null && heroDef != null && Time.time > nextFlourish)
                {
                    nextFlourish = Time.time + Random.Range(7f, 13f);
                    Color c = ElementChart.ColorOf(heroDef.element);
                    int pick = Random.Range(0, 4);
                    if (pick == 0) { hero.Attack(Random.Range(0, 4), 0.14f); VFX.Slash(heroHolder.position, heroHolder.forward, 2.2f, 150f, 10f, c, 0.3f); }
                    else if (pick == 1) { hero.Spin(0.35f); VFX.Shockwave(heroHolder.position, 2.4f, c, 0.4f); }
                    else if (pick == 2) { hero.HeavyAttack(0.2f); VFX.Breath(heroHolder.position, c, 25); }
                    else { hero.Victory(); Invoke("ResetHeroPose", 2.2f); }
                    if (GameManager.Instance != null) GameManager.Instance.Audio.Play("slash", 0.25f);
                }
                if (CameraController.Instance != null)
                {
                    Vector3 drift = new Vector3(Mathf.Sin(t * 0.13f) * 0.4f, Mathf.Sin(t * 0.21f) * 0.12f, 0f);
                    CameraController.Instance.SetFixed(new Vector3(-0.2f, 2.1f, -6.2f) + drift, new Vector3(0.2f, 1.35f, 0f));
                }
            }
        }

        void ResetHeroPose()
        {
            if (hero != null) hero.ResetPose();
        }
    }
}
