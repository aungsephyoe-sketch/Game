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
        Light keyLight, rimLight;
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
            var rg = new GameObject("RimLight");
            rg.transform.SetParent(transform, false);
            // From behind the subject toward the camera, a little from above and the side.
            rg.transform.rotation = Quaternion.LookRotation(new Vector3(-0.5f, -0.35f, 1f));
            rimLight = rg.AddComponent<Light>();
            rimLight.type = LightType.Directional;
            rimLight.intensity = 1.3f;
            rimLight.cullingMask = 1 << Layer;
            rimLight.enabled = false;
        }

        /// <summary>Head-and-shoulders (or full body) portrait of a character. Null until rendered (next frame).</summary>
        public static Texture Hero(CharacterDefinition def, bool fullBody = false)
        {
            if (def == null) return null;
            return Get("h:" + def.id + (fullBody ? ":f" : ""));
        }

        /// <summary>
        /// Card poster: head and upper body framed from the head itself (never cropped), facing the viewer with
        /// the slayer's own expression and pose, lit to match their poster environment (<see cref="PosterEnv"/>).
        /// </summary>
        public static Texture HeroPoster(CharacterDefinition def)
        {
            if (def == null) return null;
            return Get("h:" + def.id + ":p");
        }

        /// <summary>Full body in a dynamic battle pose from a low, heroic angle (summon and promo banners).</summary>
        public static Texture HeroAction(CharacterDefinition def)
        {
            if (def == null) return null;
            return Get("h:" + def.id + ":a");
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

        // A posed subject parked (inactive, so nothing moves) while its smooth body is meshed on a worker thread.
        Transform waitHolder;
        CharacterVisual waitVisual;
        string waitKey;
        CharacterDefinition waitDef;
        bool waitPoster, waitFull, waitAction;
        float waitHeight, waitHeadY;
        int waitFrames;

        void LateUpdate()
        {
            if (waitHolder != null)
            {
                waitFrames++;
                if (waitVisual != null && !waitVisual.MeltReady && waitFrames < 120) return;
                var holder = waitHolder;
                waitHolder = null;
                holder.gameObject.SetActive(true);
                try
                {
                    if (waitVisual != null) waitVisual.CompleteMelt();
                    Shoot(waitKey, holder, waitVisual, waitDef, waitPoster, waitFull, waitAction, waitHeight, waitHeadY);
                }
                catch (System.Exception ex) { Debug.LogWarning("[PortraitStudio] " + waitKey + ": " + ex.Message); if (holder != null) Destroy(holder.gameObject); }
                return;
            }
            if (queue.Count == 0) return;
            string key = queue.Dequeue();
            queued.Remove(key);
            try { Render(key); }
            catch (System.Exception ex) { Debug.LogWarning("[PortraitStudio] " + key + ": " + ex.Message); }
        }

        void Render(string key)
        {
            var parts = key.Split(':');
            bool action = parts.Length > 2 && parts[2] == "a";
            bool poster = parts.Length > 2 && parts[2] == "p";
            CharacterDefinition heroDef = null;
            bool full = parts.Length > 2 && (parts[2] == "f" || action);
            var holder = new GameObject("Subject").transform;
            holder.SetParent(stage, false);
            holder.localRotation = Quaternion.Euler(0f, 18f, 0f); // face the camera (which looks back along -Z), turned a little
            CharacterVisual v = null;
            float height = 2f;
            float headY = 1.6f;
            if (parts[0] == "h")
            {
                var def = GameDatabase.GetCharacter(parts[1]);
                heroDef = def;
                if (def != null) v = CharacterVisual.BuildHero(def, holder);
                if (v != null)
                {
                    headY = v.HeadY; height = headY + 0.52f; v.FidgetsEnabled = false;
                    if (poster)
                    {
                        holder.localRotation = Quaternion.Euler(0f, 14f, 0f);
                        v.ApplyBannerPose();
                        v.ApplyPosterLook(def);
                        v.FaceViewer();
                    }
                    else if (action)
                    {
                        // Turned three-quarters, in their battle pose.
                        holder.localRotation = Quaternion.Euler(0f, 34f, 0f);
                        v.ApplyBannerPose();
                        v.ApplyPosterLook(def); // its own expression and pose on the card art
                        v.FaceViewer(); // banners: the slayer looks out at you
                    }
                    else v.ApplyTeamIdle();
                }
            }
            else
            {
                var def = GameDatabase.GetEnemy(parts[1]);
                if (def != null) v = CharacterVisual.BuildDemon(def, holder);
                if (def != null) { height = 2.3f * def.scale; headY = height * 0.78f; }
            }
            if (v == null) { Destroy(holder.gameObject); return; }
            v.enabled = false; // freeze the pose for the snapshot
            if (!v.MeltReady)
            {
                // Wait (frozen) for the sculpted body, then shoot.
                waitHolder = holder; waitVisual = v; waitKey = key; waitDef = heroDef;
                waitPoster = poster; waitFull = full; waitAction = action; waitHeight = height; waitHeadY = headY; waitFrames = 0;
                holder.gameObject.SetActive(false);
                return;
            }
            v.CompleteMelt();
            Shoot(key, holder, v, heroDef, poster, full, action, height, headY);
        }

        void Shoot(string key, Transform holder, CharacterVisual v, CharacterDefinition heroDef, bool poster, bool full, bool action, float height, float headY)
        {
            foreach (var t in holder.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = Layer;
            foreach (var ps in holder.GetComponentsInChildren<ParticleSystem>(true)) ps.gameObject.SetActive(false);

            if (poster && v.HeadTransform != null)
            {
                // Frame from the real head, so it is never cut off.
                var hb = new Bounds(v.HeadTransform.position, Vector3.one * 0.2f);
                foreach (var hr in v.HeadTransform.GetComponentsInChildren<Renderer>())
                    if (hr.enabled && hr.bounds.size.magnitude < 3f) hb.Encapsulate(hr.bounds);
                // Close on the face: a little room above the hair, down to the shoulders.
                float top = hb.max.y + hb.size.y * 0.07f, bottom = hb.min.y - hb.size.y * 0.35f;
                float pspan = top - bottom;
                float pdist = pspan * 0.5f / Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
                Vector3 pf = new Vector3(hb.center.x, (top + bottom) * 0.5f, hb.center.z);
                cam.transform.position = pf + new Vector3(0f, 0.08f, pdist);
                cam.transform.LookAt(pf);
            }
            else
            {
                // Frame: full body, or the head and chest.
                float focusY = full ? height * 0.5f : headY - 0.25f;
                float span = full ? height * 1.1f : 1.42f;
                float dist = span * 0.5f / Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
                Vector3 focus = StagePos + Vector3.up * focusY;
                cam.transform.position = focus + new Vector3(0f, action ? -height * 0.28f : full ? 0.2f : 0.1f, dist);
                cam.transform.LookAt(focus + (action ? Vector3.up * height * 0.06f : Vector3.zero));
            }

            // High-resolution, 8× anti-aliased portraits with mipmaps so they stay crisp at every size on screen.
            var rt = new RenderTexture(poster ? 640 : full ? 800 : 512, poster ? 668 : full ? 1200 : 512, 24, RenderTextureFormat.ARGB32)
            {
                name = "Portrait_" + key, antiAliasing = 8, useMipMap = true, autoGenerateMips = true, filterMode = FilterMode.Trilinear, anisoLevel = 4
            };
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
            var keyRot = keyLight.transform.rotation;
            if (poster && heroDef != null)
            {
                // The poster's own light: key colour and angle from its environment, a coloured rim from behind.
                var env = PosterEnv.For(heroDef);
                keyLight.color = env.key;
                keyLight.intensity = env.keyIntensity;
                keyLight.transform.rotation = Quaternion.Euler(env.keyEuler);
                RenderSettings.ambientLight = env.ambient;
                rimLight.color = env.rim;
                rimLight.enabled = true;
            }
            cam.Render();
            keyLight.enabled = false;
            rimLight.enabled = false;
            keyLight.color = Color.white;
            keyLight.intensity = 1.1f;
            keyLight.transform.rotation = keyRot;
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
