using System.Collections.Generic;
using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// Renders the in-game chibi characters and monsters into portrait images for menus (team cards, roster,
    /// summons, mission pages), so every picture in the UI is the same character the player controls.
    /// Renders happen off-screen on a hidden layer, one per frame, and are cached.
    /// </summary>
    public class PortraitStudio : MonoBehaviour
    {
        public const int Layer = 31;
        static PortraitStudio instance;
        static readonly Dictionary<string, RenderTexture> cache = new Dictionary<string, RenderTexture>();
        static readonly Queue<string> queue = new Queue<string>();
        static readonly HashSet<string> queued = new HashSet<string>();

        Camera cam;
        Transform stage;
        Light keyLight;
        static readonly Vector3 StagePos = new Vector3(0f, -400f, 0f);

        static PortraitStudio Instance
        {
            get
            {
                if (instance == null)
                {
                    var go = new GameObject("[PortraitStudio]");
                    DontDestroyOnLoad(go);
                    instance = go.AddComponent<PortraitStudio>();
                    instance.Setup();
                }
                return instance;
            }
        }

        void Setup()
        {
            stage = new GameObject("Stage").transform;
            stage.SetParent(transform, false);
            stage.position = StagePos;
            var camGo = new GameObject("PortraitCamera");
            camGo.transform.SetParent(transform, false);
            cam = camGo.AddComponent<Camera>();
            cam.enabled = false;
            cam.cullingMask = 1 << Layer;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0f, 0f, 0f, 0f);
            cam.fieldOfView = 26f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 50f;
            cam.allowHDR = false;
            var lg = new GameObject("KeyLight");
            lg.transform.SetParent(transform, false);
            lg.transform.rotation = Quaternion.Euler(35f, 150f, 0f);
            keyLight = lg.AddComponent<Light>();
            keyLight.type = LightType.Directional;
            keyLight.intensity = 1.1f;
            keyLight.cullingMask = 1 << Layer;
            keyLight.enabled = false;
        }

        /// <summary>Head-and-shoulders (or full body) portrait of a character. Null until rendered (next frame).</summary>
        public static Texture Hero(CharacterDefinition def, bool fullBody = false)
        {
            if (def == null) return null;
            return Get("h:" + def.id + (fullBody ? ":f" : ""));
        }

        public static Texture Enemy(EnemyDefinition def)
        {
            if (def == null) return null;
            return Get("e:" + def.id);
        }

        static Texture Get(string key)
        {
            RenderTexture rt;
            if (cache.TryGetValue(key, out rt) && rt != null && rt.IsCreated()) return rt;
            if (!queued.Contains(key)) { queued.Add(key); queue.Enqueue(key); }
            if (Instance == null) return null;
            return null;
        }

        void LateUpdate()
        {
            if (queue.Count == 0) return;
            string key = queue.Dequeue();
            queued.Remove(key);
            try { Render(key); }
            catch (System.Exception ex) { Debug.LogWarning("[PortraitStudio] " + key + ": " + ex.Message); }
        }

        void Render(string key)
        {
            var parts = key.Split(':');
            bool full = parts.Length > 2 && parts[2] == "f";
            var holder = new GameObject("Subject").transform;
            holder.SetParent(stage, false);
            holder.localRotation = Quaternion.Euler(0f, 160f, 0f);
            CharacterVisual v = null;
            float height = 2f;
            float headY = 1.6f;
            if (parts[0] == "h")
            {
                var def = GameDatabase.GetCharacter(parts[1]);
                if (def != null) v = CharacterVisual.BuildHero(def, holder);
                if (v != null) { headY = v.HeadY; height = headY + 0.45f; v.FidgetsEnabled = false; }
            }
            else
            {
                var def = GameDatabase.GetEnemy(parts[1]);
                if (def != null) v = CharacterVisual.BuildDemon(def, holder);
                if (def != null) { height = 2.3f * def.scale; headY = height * 0.78f; }
            }
            if (v == null) { Destroy(holder.gameObject); return; }
            v.enabled = false; // freeze the pose for the snapshot
            foreach (var t in holder.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = Layer;
            foreach (var ps in holder.GetComponentsInChildren<ParticleSystem>(true)) ps.gameObject.SetActive(false);

            // Frame: full body, or the head and chest.
            float focusY = full ? height * 0.5f : headY - 0.25f;
            float span = full ? height * 1.1f : 1.3f;
            float dist = span * 0.5f / Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
            Vector3 focus = StagePos + Vector3.up * focusY;
            cam.transform.position = focus + new Vector3(0f, full ? 0.2f : 0.1f, dist);
            cam.transform.LookAt(focus);

            var rt = new RenderTexture(full ? 384 : 256, full ? 576 : 256, 16, RenderTextureFormat.ARGB32) { name = "Portrait_" + key, antiAliasing = 4 };
            rt.Create();
            cam.targetTexture = rt;

            // Clean studio lighting, independent of the scene's fog and mood.
            bool fog = RenderSettings.fog;
            var amb = RenderSettings.ambientLight;
            var sun = RenderSettings.sun;
            bool sunOn = sun != null && sun.enabled;
            RenderSettings.fog = false;
            RenderSettings.ambientLight = new Color(0.62f, 0.6f, 0.68f);
            if (sun != null) sun.enabled = false;
            keyLight.enabled = true;
            cam.Render();
            keyLight.enabled = false;
            if (sun != null) sun.enabled = sunOn;
            RenderSettings.fog = fog;
            RenderSettings.ambientLight = amb;
            cam.targetTexture = null;

            RenderTexture old;
            if (cache.TryGetValue(key, out old) && old != null) old.Release();
            cache[key] = rt;
            Destroy(holder.gameObject);
        }
    }
}
