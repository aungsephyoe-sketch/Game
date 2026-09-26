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
            EnsureWorld();
            SetHero(data.team.Count > 0 ? data.team[0] : GameDatabase.Protagonist);
            gameObject.SetActive(true);
            ArenaBuilder.ApplyLighting(theme);
        }

        public void ShowViewer(string characterId)
        {
            viewer = true;
            EnsureWorld();
            if (shownId != characterId) viewerYaw = 180f;
            SetHero(characterId);
            gameObject.SetActive(true);
            ArenaBuilder.ApplyLighting(theme);
        }

        public void Hide() { gameObject.SetActive(false); }

        public void RotateViewer(float degrees) { viewerYaw += degrees; }

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

        void Update()
        {
            if (heroHolder == null) return;
            float t = Time.unscaledTime;
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
