using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>Keeps sky objects (the moon) at a fixed offset from the camera so they never get closer.</summary>
    public class SkyAnchor : MonoBehaviour
    {
        public Vector3 Offset;

        void LateUpdate()
        {
            var cam = Camera.main;
            if (cam != null) transform.position = new Vector3(cam.transform.position.x, 0f, cam.transform.position.z) + Offset;
        }
    }
}
