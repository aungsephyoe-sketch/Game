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
        public MenuStage Stage { get; private set; }

        public MissionDefinition SelectedMission;
        public string SelectedCharacterId;
        public BattleResult LastResult;
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

            GameDatabase.EnsureBuilt();
            Data = SaveSystem.Load();

            SetupCamera();
            SetupLight();

            Audio = gameObject.AddComponent<AudioManager>();
            Controls = gameObject.AddComponent<MobileControls>();
            UI = gameObject.AddComponent<UIManager>();

            var stageGo = new GameObject("[MenuStage]");
            DontDestroyOnLoad(stageGo);
            Stage = stageGo.AddComponent<MenuStage>();

            GoTo(GameScreen.MainMenu);
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
        }

        public void GoTo(GameScreen screen)
        {
            CurrentScreen = screen;
            if (screen != GameScreen.Battle && Stage != null) Stage.Show(Data);
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
            GoTo(GameScreen.MainMenu);
        }

        // ------------------------------------------------------------------ Battle flow

        public void StartMission(MissionDefinition mission)
        {
            if (Battle != null) Destroy(Battle.gameObject);
            SelectedMission = mission;
            if (Stage != null) Stage.Hide();
            var go = new GameObject("[Battle " + mission.id + "]");
            Battle = go.AddComponent<BattleController>();
            Battle.Setup(mission, Data);
            Audio.PlayMusic(true);
            CurrentScreen = GameScreen.Battle;
        }

        public void EndBattle(BattleResult result)
        {
            RewardSystem.Grant(Data, result);
            Save();
            LastResult = result;
            if (Battle != null) Destroy(Battle.gameObject);
            Battle = null;
            TimeController.ResetAll();
            Audio.PlayMusic(false);
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
