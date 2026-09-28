using System.Collections.Generic;
using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// Area view: each region opens as its own bright low-poly diorama (see <see cref="AreaDiorama"/>) with the
    /// region's missions as numbered stops along a stepping-stone trail and the leader standing on the current
    /// one, the per-chapter maps of the reference sheets. The UI draws the locks and number plates over the
    /// points in <see cref="AreaPoints"/>.
    /// </summary>
    public partial class MapStage
    {
        static readonly Vector3 AreaOrigin = new Vector3(1000f, 0f, 0f);

        public bool AreaMode { get; private set; }
        public string AreaRegion { get; private set; }
        public readonly List<Vector3> AreaPoints = new List<Vector3>();
        public AreaDiorama.Look AreaLook { get; private set; }
        int areaCurrent;
        float areaBlend;
        AreaDiorama.Result area;
        Transform currentRing;

        public void EnterArea(string regionId, int stops, int current)
        {
            if (!nodes.ContainsKey(regionId) || travel != null) return;
            if (area == null || AreaRegion != regionId || AreaPoints.Count != stops)
            {
                DestroyArea();
                pan = Vector3.zero;
                area = AreaDiorama.Build(regionId, stops, AreaOrigin, transform);
                AreaPoints.Clear();
                AreaPoints.AddRange(area.nodes);
                AreaLook = area.look;
                BuildAreaExtras();
                areaBlend = 0f;
                if (token != null && AreaPoints.Count > 0) token.position = AreaPoints[Mathf.Clamp(current, 0, AreaPoints.Count - 1)] + Vector3.up * 0.37f;
                if (AreaMode) ApplyAreaLighting();
            }
            if (!AreaMode)
            {
                pan = Vector3.zero;
                AreaMode = true;
                if (world != null) world.SetActive(false);
                ApplyAreaLighting();
            }
            AreaRegion = regionId;
            areaCurrent = Mathf.Clamp(current, 0, Mathf.Max(0, stops - 1));
            if (token != null) token.localScale = Vector3.one * 1.35f;
        }

        public void ExitArea()
        {
            if (!AreaMode && area == null) return;
            AreaMode = false;
            pan = Vector3.zero;
            DestroyArea();
            if (world != null) world.SetActive(true);
            if (token != null)
            {
                token.localScale = Vector3.one * 1.8f;
                if (!string.IsNullOrEmpty(CurrentRegion) && nodes.ContainsKey(CurrentRegion)) token.position = nodes[CurrentRegion].pos;
            }
            if (tokenVisual != null) tokenVisual.SetMoving(0f);
            ApplyLighting();
        }

        void DestroyArea()
        {
            if (area != null && area.root != null) Destroy(area.root);
            area = null;
            currentRing = null;
            AreaPoints.Clear();
        }

        /// <summary>The glowing ring under the current stop, and the region's weather.</summary>
        void BuildAreaExtras()
        {
            var ringGo = new GameObject("CurrentRing");
            ringGo.transform.SetParent(area.root.transform, false);
            currentRing = ringGo.transform;
            Color rc = area.look == AreaDiorama.Look.Crimson ? new Color(1f, 0.35f, 0.25f) : area.look == AreaDiorama.Look.Snow ? new Color(0.5f, 0.85f, 1f) : new Color(1f, 0.9f, 0.4f);
            var ring = MeshFactory.MeshObject(MeshFactory.Ring(0.72f), currentRing, Vector3.up * 0.4f, new Vector3(1.05f, 1f, 1.05f), MaterialFactory.Additive(new Color(rc.r, rc.g, rc.b, 0.95f)), false);
            ring.AddComponent<Pulse>().Speed = 3f;
            MeshFactory.MeshObject(MeshFactory.Disc(), currentRing, Vector3.up * 0.39f, new Vector3(0.95f, 1f, 0.95f), MaterialFactory.Additive(new Color(rc.r, rc.g, rc.b, 0.35f)), false);
            var lg = new GameObject("RingLight");
            lg.transform.SetParent(currentRing, false);
            lg.transform.localPosition = Vector3.up * 1.2f;
            var l = lg.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = rc;
            l.range = 5f;
            l.intensity = 1.4f;

            string kind = null;
            switch (area.look)
            {
                case AreaDiorama.Look.Snow: kind = "snow"; break;
                case AreaDiorama.Look.Crimson: kind = "embers"; break;
                case AreaDiorama.Look.Meadow: case AreaDiorama.Look.Village: case AreaDiorama.Look.Shrine: kind = "leaves"; break;
                case AreaDiorama.Look.Ruins: kind = "ash"; break;
                case AreaDiorama.Look.DarkForest: kind = "motes"; break;
            }
            if (kind != null) EnvFx.Weather(area.root.transform, Vector3.zero, kind, 30f);
        }

        void ApplyAreaLighting()
        {
            if (area == null) return;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = area.fog;
            RenderSettings.fogStartDistance = 40f;
            RenderSettings.fogEndDistance = 90f;
            RenderSettings.ambientLight = area.ambient * 0.8f;
            if (Camera.main != null) Camera.main.backgroundColor = area.sky;
            if (RenderSettings.sun != null)
            {
                RenderSettings.sun.color = area.sun;
                RenderSettings.sun.intensity = area.sunIntensity * 0.8f;
                RenderSettings.sun.transform.rotation = Quaternion.Euler(52f, -35f, 0f);
            }
        }

        int focusIndex = -1;

        /// <summary>Mission page view: the camera drops behind the leader standing on this stop, looking up the trail.</summary>
        public void FocusStop(int index) { focusIndex = index; if (index >= 0 && index < AreaPoints.Count) areaCurrent = index; }
        public void ClearFocus() { focusIndex = -1; }

        void UpdateArea()
        {
            if (area == null || AreaPoints.Count == 0) return;
            areaBlend = Mathf.MoveTowards(areaBlend, 1f, Time.unscaledDeltaTime * 0.8f);
            // Framed like the reference: high three-quarter view, the map shifted left of the mission panel.
            Vector3 c = AreaOrigin;
            Vector3 shift = new Vector3(6.2f, 0f, 0.5f);
            float k = Mathf.SmoothStep(0f, 1f, areaBlend);
            float dist = Mathf.Lerp(1.25f, 1f, k);
            Vector3 lookDir = Vector3.forward;
            if (focusIndex >= 0 && focusIndex < AreaPoints.Count)
            {
                // Behind the leader, looking along the trail toward the next stop.
                Vector3 here = AreaPoints[focusIndex];
                Vector3 next = focusIndex + 1 < AreaPoints.Count ? AreaPoints[focusIndex + 1] : here + (here - AreaPoints[Mathf.Max(0, focusIndex - 1)]);
                lookDir = next - here;
                lookDir.y = 0f;
                if (lookDir.sqrMagnitude < 0.01f) lookDir = Vector3.forward;
                lookDir.Normalize();
                CameraController.Instance.SetFixed(here - lookDir * 6.8f + Vector3.up * 3.8f, here + lookDir * 6f + Vector3.up * 0.4f);
            }
            else CameraController.Instance.SetFixed(c + shift + pan + new Vector3(0f, 19.5f, -15.5f) * dist, c + shift + pan);
            CameraFocus = c;

            // The leader walks to the current stop.
            Vector3 target = AreaPoints[areaCurrent] + Vector3.up * 0.37f;
            if (currentRing != null) currentRing.position = AreaPoints[areaCurrent];
            Vector3 d = target - token.position;
            d.y = 0f;
            if (d.magnitude > 0.05f)
            {
                float step = Time.unscaledDeltaTime * 4.5f;
                token.position = d.magnitude <= step ? target : new Vector3(token.position.x + d.normalized.x * step, target.y, token.position.z + d.normalized.z * step);
                token.rotation = Quaternion.Slerp(token.rotation, Quaternion.LookRotation(d.normalized), Time.unscaledDeltaTime * 10f);
                if (tokenVisual != null) tokenVisual.SetMoving(1f);
            }
            else
            {
                token.position = target;
                if (tokenVisual != null) tokenVisual.SetMoving(0f);
                Quaternion face = focusIndex >= 0 ? Quaternion.LookRotation(lookDir) : Quaternion.Euler(0f, 180f, 0f);
                token.rotation = Quaternion.Slerp(token.rotation, face, Time.unscaledDeltaTime * 4f);
            }
        }
    }
}
