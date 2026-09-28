using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// Persistent root of the game. Owns the save data, the screen state machine and the long-lived
    /// services (camera, audio, UI, controls). Created automatically by GameBootstrap in any scene.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public PlayerData Data { get; private set; }
        public GameScreen CurrentScreen { get; private set; }
        public UIManager UI { get; private set; }
        public AudioManager Audio { get; private set; }
        public MobileControls Controls { get; private set; }
        public CameraController Cam { get; private set; }
        public BattleController Battle { get; private set; }
        public HomeStage Home { get; private set; }
        public MapStage Map { get; private set; }
        public SummonStage SummonHall { get; private set; }
        /// <summary>Region the leader is walking to (shown on the map while travelling).</summary>
        public string TravelDestination { get; private set; }
        /// <summary>Screen fade used for transitions (0 = clear, 1 = black). Drawn by the UI.</summary>
        public float TransitionAlpha { get; private set; }
        /// <summary>Time the current screen was entered (drives UI entrance animations).</summary>
        public float ScreenEnteredAt { get; private set; }

        [System.NonSerialized] public MissionDefinition SelectedMission;
        public string SelectedCharacterId;
        [System.NonSerialized] public BattleResult LastResult;
        public string ComingSoonFeature = "";

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Screen.orientation = ScreenOrientation.AutoRotation;
            Screen.autorotateToPortrait = false;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToLandscapeLeft = true;
            Screen.autorotateToLandscapeRight = true;
            QualitySettings.shadowDistance = 45f;

            GameSettings.Load();
            GameDatabase.EnsureBuilt();
            Data = SaveSystem.Load();

            SetupCamera();
            SetupLight();

            Audio = gameObject.AddComponent<AudioManager>();
            Controls = gameObject.AddComponent<MobileControls>();
            UI = gameObject.AddComponent<UIManager>();

            QuestSystem.EnsureReset(Data);
            TutorialSystem.Migrate(Data);
            Home = MakeStage<HomeStage>("[HomeStage]");
            Map = MakeStage<MapStage>("[MapStage]");
            SummonHall = MakeStage<SummonStage>("[SummonStage]");

            try
            {
                if (!Data.introSeen)
                {
                    // First launch: the opening cinematic flows straight into the first battle.
                    PlayCutscene("opening", () =>
                    {
                        Data.introSeen = true;
                        Save();
                        StartMission(GameDatabase.GetMission("1-1"));
                    });
                }
                else GoTo(GameScreen.MainMenu);
            }
            catch (System.Exception ex)
            {
                Debug.LogError("[GameManager] startup scene failed, opening the home screen instead: " + ex);
                Data.introSeen = true;
                GoTo(GameScreen.MainMenu);
            }
        }

        T MakeStage<T>(string name) where T : MonoBehaviour
        {
            var go = new GameObject(name);
            DontDestroyOnLoad(go);
            var st = go.AddComponent<T>();
            go.SetActive(false);
            return st;
        }

        void SetupCamera()
        {
            var cam = Camera.main;
            if (cam == null)
            {
                var go = new GameObject("Main Camera");
                go.tag = "MainCamera";
                cam = go.AddComponent<Camera>();
                go.AddComponent<AudioListener>();
            }
            else if (cam.GetComponent<AudioListener>() == null) cam.gameObject.AddComponent<AudioListener>();
            DontDestroyOnLoad(cam.gameObject);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.04f, 0.04f, 0.1f);
            cam.fieldOfView = 45f;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 200f;
            Cam = cam.GetComponent<CameraController>();
            if (Cam == null) Cam = cam.gameObject.AddComponent<CameraController>();
            if (cam.GetComponent<PostFX>() == null) cam.gameObject.AddComponent<PostFX>();
            cam.allowHDR = false;
            cam.cullingMask &= ~(1 << PortraitStudio.Layer); // the portrait studio renders on its own layer
            cam.allowMSAA = true;
        }

        void SetupLight()
        {
            var sun = RenderSettings.sun;
            if (sun == null)
            {
                var go = new GameObject("Moonlight");
                sun = go.AddComponent<Light>();
                sun.type = LightType.Directional;
                RenderSettings.sun = sun;
            }
            DontDestroyOnLoad(sun.gameObject);
            sun.transform.rotation = Quaternion.Euler(55f, -35f, 0f);
            sun.color = new Color(0.85f, 0.85f, 1f);
            sun.intensity = 1.1f;
            sun.shadows = LightShadows.Soft;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.3f, 0.28f, 0.4f);
        }

        void Update()
        {
            TimeController.Tick();
            TutorialSystem.Tick();
            TransitionAlpha = Mathf.MoveTowards(TransitionAlpha, 0f, Time.unscaledDeltaTime * 2.2f);
        }

        /// <summary>Switches screens and makes sure the matching 3D stage (home, map, summon hall) is the one on show.</summary>
        public void GoTo(GameScreen screen)
        {
            var prev = CurrentScreen;
            CurrentScreen = screen;
            ScreenEnteredAt = Time.unscaledTime;
            if (StageFor(prev) != StageFor(screen)) TransitionAlpha = Mathf.Max(TransitionAlpha, 0.85f);
            // A soft whoosh on every menu change (battles and cutscenes bring their own sound).
            if (prev != screen && screen != GameScreen.Battle && screen != GameScreen.Cutscene && Audio != null) Audio.PlayPitched("whoosh", 0.16f, 1.25f);
            ShowStageFor(screen);
            var music = MusicFor(screen);
            if (music.HasValue) Audio.SetMusicState(music.Value);
        }

        /// <summary>0 none, 1 home, 2 viewer, 3 map, 4 summon.</summary>
        static int StageFor(GameScreen s)
        {
            switch (s)
            {
                case GameScreen.Battle:
                case GameScreen.Cutscene:
                case GameScreen.Results:
                case GameScreen.Credits: return 0;
                case GameScreen.CharacterDetail: return 2;
                case GameScreen.WorldMap:
                case GameScreen.Story:
                case GameScreen.MissionDetail: return 3;
                case GameScreen.Summon: return 4;
                default: return 1;
            }
        }

        static MusicState? MusicFor(GameScreen s)
        {
            switch (StageFor(s))
            {
                case 1:
                case 2: return MusicState.Menu;
                case 3: return MusicState.Map;
                case 4: return MusicState.Summon;
            }
            if (s == GameScreen.Credits) return MusicState.Story;
            return null;
        }

        void ShowStageFor(GameScreen screen)
        {
            int st = StageFor(screen);
            // Results keep the battle arena behind them; everything else hides what it doesn't use.
            if (st != 1 && st != 2) Home.Hide();
            if (st != 3) { Map.Hide(); TravelDestination = null; }
            if (st != 4) SummonHall.Hide();
            if (st == 1) Home.ShowHome(Data);
            else if (st == 2) Home.ShowViewer(string.IsNullOrEmpty(SelectedCharacterId) ? Data.team[0] : SelectedCharacterId);
            else if (st == 3) Map.Show(Data);
            else if (st == 4) SummonHall.Show();
            else if (screen == GameScreen.Results && Battle == null) Map.Show(Data); // after a story scene, results sit over the world map
            if (st != 0 && Battle != null) { Destroy(Battle.gameObject); Battle = null; }
            // Background sound for the calm screens; battles set their own region ambience.
            if (st == 1 || st == 2) Audio.SetAmbience("home");
            else if (st == 3) Audio.SetAmbience("wind");
            else if (st == 4) Audio.SetAmbience(null);
        }

        /// <summary>Refreshes the visible stage after team or roster changes.</summary>
        public void RefreshStage() { ShowStageFor(CurrentScreen); }

        // ------------------------------------------------------------------ Story flow

        /// <summary>Plays an in-engine cutscene over everything else, marks it seen, then continues.</summary>
        public void PlayCutscene(string id, System.Action onDone)
        {
            if (CutsceneDatabase.Get(id) == null) { if (onDone != null) onDone(); return; }
            TimeController.ResetAll();
            if (Battle != null) { Destroy(Battle.gameObject); Battle = null; }
            CurrentScreen = GameScreen.Cutscene;
            ScreenEnteredAt = Time.unscaledTime;
            ShowStageFor(GameScreen.Cutscene);
            Audio.SetMusicState(MusicState.Story);
            CutscenePlayer.Play(id, () =>
            {
                Data.MarkSeen(id);
                Save();
                if (onDone != null) onDone();
            });
        }

        /// <summary>
        /// The full road into a mission: walk the leader across the world map if the mission is somewhere else,
        /// play its story scene the first time, then fight.
        /// </summary>
        /// <summary>An encounter on the road waiting for the player's choice (shown over the world map).</summary>
        [System.NonSerialized] public Encounter CurrentEncounter;
        /// <summary>The mission the team was travelling to when an encounter battle interrupted the journey.</summary>
        [System.NonSerialized] public MissionDefinition PendingMission;
        bool encounterRolled;

        bool OnTravelMidway(string from, string to)
        {
            if (encounterRolled) return false;
            encounterRolled = true;
            CurrentEncounter = EncounterSystem.Roll(Data, from, to);
            if (CurrentEncounter != null) Audio.Play(CurrentEncounter.battle ? "roar" : "perfect", 0.5f);
            return CurrentEncounter != null;
        }

        /// <summary>The player's answer to a road encounter.</summary>
        public string ResolveEncounter(bool accept)
        {
            var e = CurrentEncounter;
            if (e == null) return null;
            if (e.battle && accept)
            {
                CurrentEncounter = null;
                PendingMission = SelectedMission;
                StartMission(EncounterSystem.BuildBattle(e));
                return null;
            }
            string msg = e.battle ? "You slip past unseen." : EncounterSystem.Resolve(Data, e, accept);
            Save();
            CurrentEncounter = null;
            return msg;
        }

        /// <summary>After an encounter battle: pick the road back up toward the original destination.</summary>
        public void ContinueJourney()
        {
            var m = PendingMission;
            PendingMission = null;
            if (m != null) BeginMission(m, true);
            else GoTo(GameScreen.WorldMap);
        }

        public void BeginMission(MissionDefinition m) { BeginMission(m, false); }

        public void BeginMission(MissionDefinition m, bool resuming)
        {
            if (m == null) return;
            if (!resuming) encounterRolled = false;
            Map.OnMidway = OnTravelMidway;
            Map.Paused = () => CurrentEncounter != null;
            SelectedMission = m;
            if (!string.IsNullOrEmpty(m.regionId) && m.regionId != Data.currentRegion && System.Array.IndexOf(MapStage.RouteOrder, m.regionId) >= 0
                && m.type != MissionType.Training && m.type != MissionType.Event)
            {
                if (CurrentScreen != GameScreen.WorldMap) GoTo(GameScreen.WorldMap);
                TravelDestination = m.regionId;
                Map.TravelTo(m.regionId, Data, () =>
                {
                    TravelDestination = null;
                    Save();
                    EnterMission(m);
                });
                return;
            }
            EnterMission(m);
        }

        /// <summary>Summon-banner trial: play this Mythic at max power for 60 seconds against waves of demons.</summary>
        public void BeginTrial(string characterId)
        {
            var def = GameDatabase.GetCharacter(characterId);
            if (def == null) return;
            // Test play happens in the forest.
            var region = GameDatabase.GetRegion("forest");
            var trial = new MissionDefinition
            {
                id = "TRIAL", name = "Trial: " + def.displayName, type = MissionType.Training, training = true, trialCharacterId = characterId,
                regionId = "forest", enemyLevel = 30, timeLimit = 60f, storyText = "Try " + def.displayName + " at full power.",
                theme = region != null ? region.theme : new ArenaTheme()
            };
            StartMission(trial);
        }

        /// <summary>Environment quality test: play one of the three prototype worlds with the current team.</summary>
        public void BeginPrototype(string env)
        {
            int total = 0, n = 0;
            foreach (var id in Data.team)
            {
                var c = Data.GetCharacter(id);
                if (c != null) { total += c.level; n++; }
            }
            var m = GameDatabase.PrototypeMission(env, n > 0 ? total / n : 10);
            if (m != null) StartMission(m);
        }

        /// <summary>Walks the leader to a region without starting anything (map exploration).</summary>
        public void TravelTo(string regionId)
        {
            if (Map.Traveling || regionId == Data.currentRegion) return;
            encounterRolled = false;
            SelectedMission = null;
            Map.OnMidway = OnTravelMidway;
            Map.Paused = () => CurrentEncounter != null;
            TravelDestination = regionId;
            Map.TravelTo(regionId, Data, () => { TravelDestination = null; Save(); });
        }

        void EnterMission(MissionDefinition m)
        {
            TransitionAlpha = 1f;
            if (!string.IsNullOrEmpty(m.cutsceneBefore) && !Data.HasSeen(m.cutsceneBefore))
                PlayCutscene(m.cutsceneBefore, () => StartMission(m));
            else StartMission(m);
        }

        /// <summary>The next story mission the player hasn't cleared (null when the story is finished).</summary>
        public MissionDefinition NextStoryMission()
        {
            foreach (var ch in GameDatabase.Chapters)
                foreach (var m in ch.missions)
                    if ((m.type == MissionType.Story || m.type == MissionType.Boss) && !Data.IsMissionCleared(m.id) && Data.IsMissionUnlocked(m))
                        return m;
            return null;
        }

        GameScreen settingsReturn = GameScreen.MainMenu;
        public GameScreen SettingsReturn { get { return settingsReturn; } }

        public void OpenSettings()
        {
            settingsReturn = CurrentScreen;
            GoTo(GameScreen.Settings);
        }

        public void ShowComingSoon(string feature)
        {
            ComingSoonFeature = feature;
            GoTo(GameScreen.ComingSoon);
        }

        public void Save()
        {
            SaveSystem.Save(Data);
        }

        public void ResetSave()
        {
            Data = SaveSystem.ResetProgress();
            QuestSystem.EnsureReset(Data);
            PlayCutscene("opening", () =>
            {
                Data.introSeen = true;
                Save();
                StartMission(GameDatabase.GetMission("1-1"));
            });
        }

        // ------------------------------------------------------------------ Battle flow

        public void StartMission(MissionDefinition mission)
        {
            if (Battle != null) Destroy(Battle.gameObject);
            SelectedMission = mission;
            TimeController.ResetAll();
            CurrentScreen = GameScreen.Battle;
            ScreenEnteredAt = Time.unscaledTime;
            ShowStageFor(GameScreen.Battle);
            TransitionAlpha = 1f;
            var go = new GameObject("[Battle " + mission.id + "]");
            Battle = go.AddComponent<BattleController>();
            Battle.Setup(mission, Data);
            Audio.PlayMusic(true);
        }

        public void EndBattle(BattleResult result)
        {
            var m = result.mission;
            if (m.training)
            {
                LastResult = null;
                TimeController.ResetAll();
                if (m.openWorld) { Save(); GoTo(GameScreen.MainMenu); return; }
                if (!string.IsNullOrEmpty(m.trialCharacterId)) { GoTo(GameScreen.Summon); return; }
                GoTo(GameScreen.Characters);
                return;
            }
            RewardSystem.Grant(Data, result);
            QuestSystem.Report("kill", result.kills);
            if (m.coopTier >= 0)
            {
                if (result.victory) Data.coopClears++;
                // People you just played with may want to stay in touch.
                for (int i = 0; i < m.coopAllyNames.Count && i < m.coopAllyIds.Count; i++)
                    SocialSystem.MaybeRequest(Data, m.coopAllyNames[i], m.coopAllyIds[i], m.enemyLevel + Random.Range(-3, 6));
            }
            if (result.victory)
            {
                VillageChatter.OnMissionCleared(m);
                QuestSystem.Report("clear", 1);
                if (!string.IsNullOrEmpty(m.bossId))
                {
                    QuestSystem.Report("boss", 1 + m.preBosses.Count);
                    Data.bossesDefeated += 1 + m.preBosses.Count;
                }
                if (m.type != MissionType.Encounter && m.type != MissionType.Event && !string.IsNullOrEmpty(m.regionId) && System.Array.IndexOf(MapStage.RouteOrder, m.regionId) >= 0) Data.currentRegion = m.regionId;
            }
            Save();
            LastResult = result;
            TimeController.ResetAll();

            // First clears continue the story before the results.
            if (result.victory && result.firstClear && !string.IsNullOrEmpty(m.cutsceneAfter) && !Data.HasSeen(m.cutsceneAfter))
            {
                string after = m.cutsceneAfter;
                PlayCutscene(after, () =>
                {
                    if (after == "ending") GoTo(GameScreen.Credits);
                    else GoTo(GameScreen.Results);
                });
                return;
            }
            GoTo(GameScreen.Results);
        }

        public void TogglePause()
        {
            if (CurrentScreen != GameScreen.Battle || Battle == null || Battle.Finished) return;
            TimeController.Paused = !TimeController.Paused;
            Audio.Play("click", 0.6f);
        }

        public void RetreatFromBattle()
        {
            TimeController.Paused = false;
            if (Battle != null && Battle.Mission != null) Battle.Mission.Retreat();
        }

        void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                Save();
                if (CurrentScreen == GameScreen.Battle && Battle != null && !Battle.Finished) TimeController.Paused = true;
            }
        }

        void OnApplicationQuit()
        {
            Save();
        }
    }
}
