using System.Collections.Generic;
using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// Area view: the camera drops close over one region and its missions become numbered stops along a
    /// winding trail, with the leader standing on the current stop (the per-chapter maps of the reference).
    /// The UI draws the numbers and the dotted trail over the points in <see cref="AreaPoints"/>.
    /// </summary>
    public partial class MapStage
    {
        public bool AreaMode { get; private set; }
        public string AreaRegion { get; private set; }
        public readonly List<Vector3> AreaPoints = new List<Vector3>();
        int areaCurrent;
        float areaBlend;
        GameObject areaDecor;

        public void EnterArea(string regionId, int stops, int current)
        {
            if (!nodes.ContainsKey(regionId) || travel != null) return;
            if (!AreaMode || AreaRegion != regionId || AreaPoints.Count != stops)
            {
                AreaPoints.Clear();
                Vector3 c = nodes[regionId].pos;
                // An S-shaped trail across the region, left to right, gently weaving.
                for (int i = 0; i < stops; i++)
                {
                    float k = stops > 1 ? (float)i / (stops - 1) : 0.5f;
                    float x = Mathf.Lerp(-6.5f, 6.5f, k);
                    float z = Mathf.Sin(k * Mathf.PI * 2.2f) * 3.2f - 1f;
                    AreaPoints.Add(c + new Vector3(x, 0.05f, z));
                }
                BuildAreaPads();
                areaBlend = 0f;
            }
            AreaMode = true;
            AreaRegion = regionId;
            areaCurrent = Mathf.Clamp(current, 0, Mathf.Max(0, stops - 1));
        }

        public void ExitArea()
        {
            if (!AreaMode) return;
            AreaMode = false;
            if (areaDecor != null) Destroy(areaDecor);
            areaDecor = null;
            if (token != null && nodes.ContainsKey(CurrentRegion)) token.position = nodes[CurrentRegion].pos;
            if (tokenVisual != null) tokenVisual.SetMoving(0f);
        }

        /// <summary>Small stone pads under each stop so the trail reads in 3D too.</summary>
        void BuildAreaPads()
        {
            if (areaDecor != null) Destroy(areaDecor);
            areaDecor = new GameObject("AreaPads");
            areaDecor.transform.SetParent(transform, false);
            var pad = MaterialFactory.Toon(new Color(0.92f, 0.86f, 0.7f), 0.02f);
            var dot = MaterialFactory.Toon(new Color(0.98f, 0.95f, 0.85f), 0f);
            for (int i = 0; i < AreaPoints.Count; i++)
            {
                MeshFactory.Primitive(PrimitiveType.Cylinder, areaDecor.transform, AreaPoints[i], new Vector3(1.2f, 0.06f, 1.2f), pad);
                if (i == AreaPoints.Count - 1) continue;
                // Stepping stones between stops.
                for (int k = 1; k < 5; k++)
                {
                    Vector3 p = Vector3.Lerp(AreaPoints[i], AreaPoints[i + 1], k / 5f);
                    MeshFactory.Primitive(PrimitiveType.Cylinder, areaDecor.transform, p, new Vector3(0.28f, 0.03f, 0.28f), dot);
                }
            }
        }

        void UpdateArea()
        {
            if (AreaPoints.Count == 0) return;
            areaBlend = Mathf.MoveTowards(areaBlend, 1f, Time.unscaledDeltaTime * 1.5f);
            Vector3 c = nodes[AreaRegion].pos;
            CameraFocus = c;
            Vector3 from = token.position + new Vector3(0f, 26f, -21f);
            // Offset to the right so the trail sits clear of the mission panel on the right of the screen.
            Vector3 shift = new Vector3(4.5f, 0f, 0f);
            Vector3 to = c + shift + new Vector3(0f, 15f, -13f);
            float k = Mathf.SmoothStep(0f, 1f, areaBlend);
            CameraController.Instance.SetFixed(Vector3.Lerp(from, to, k), Vector3.Lerp(token.position, c + shift + Vector3.forward * 1.5f, k));

            // The leader walks to the current stop.
            Vector3 target = AreaPoints[areaCurrent];
            Vector3 d = target - token.position;
            d.y = 0f;
            if (d.magnitude > 0.05f)
            {
                float step = Time.unscaledDeltaTime * 5f;
                token.position = d.magnitude <= step ? new Vector3(target.x, token.position.y, target.z) : token.position + d.normalized * step;
                token.rotation = Quaternion.Slerp(token.rotation, Quaternion.LookRotation(d.normalized), Time.unscaledDeltaTime * 10f);
                if (tokenVisual != null) tokenVisual.SetMoving(1f);
            }
            else
            {
                if (tokenVisual != null) tokenVisual.SetMoving(0f);
                token.rotation = Quaternion.Slerp(token.rotation, Quaternion.Euler(0f, 180f, 0f), Time.unscaledDeltaTime * 4f);
            }
        }
    }
}
