using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>3D backdrop behind the menus: the current team posed on a moonlit platform.</summary>
    public class MenuStage : MonoBehaviour
    {
        GameObject root;
        string signature = "";

        public void Show(PlayerData data)
        {
            string sig = string.Join(",", data.team.ToArray());
            if (root == null || sig != signature)
            {
                if (root != null) Destroy(root);
                Build(data);
                signature = sig;
            }
            root.SetActive(true);
            RenderSettings.fog = false;
            if (Camera.main != null) Camera.main.backgroundColor = new Color(0.05f, 0.04f, 0.1f);
            if (CameraController.Instance != null)
                CameraController.Instance.SetFixed(new Vector3(0f, 2.6f, -7.5f) + Offset, new Vector3(0f, 1.3f, 0f) + Offset);
        }

        /// <summary>Kept far from the battle arena so the two never overlap.</summary>
        static readonly Vector3 Offset = new Vector3(0f, 0f, 500f);

        public void Hide()
        {
            if (root != null) root.SetActive(false);
        }

        void Build(PlayerData data)
        {
            root = new GameObject("Stage");
            root.transform.SetParent(transform, false);
            root.transform.position = Offset;

            var disc = new GameObject("Platform");
            disc.transform.SetParent(root.transform, false);
            disc.transform.localScale = new Vector3(6f, 1f, 6f);
            disc.AddComponent<MeshFilter>().sharedMesh = MeshFactory.Disc();
            disc.AddComponent<MeshRenderer>().sharedMaterial = MaterialFactory.Toon(new Color(0.2f, 0.16f, 0.26f), 0f);

            var ring = new GameObject("Ring");
            ring.transform.SetParent(root.transform, false);
            ring.transform.localPosition = Vector3.up * 0.01f;
            ring.transform.localScale = new Vector3(6f, 1f, 6f);
            ring.AddComponent<MeshFilter>().sharedMesh = MeshFactory.Ring(0.94f);
            ring.AddComponent<MeshRenderer>().sharedMaterial = MaterialFactory.Toon(new Color(0.55f, 0.35f, 0.75f), 0f);

            var moon = MeshFactory.Primitive(PrimitiveType.Sphere, root.transform, new Vector3(5f, 7f, 20f), Vector3.one * 7f,
                MaterialFactory.Toon(new Color(1f, 0.95f, 0.8f), 0f, new Color(1f, 0.95f, 0.8f)));
            moon.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            float[] xs = { 0f, -2.2f, 2.2f };
            float[] zs = { 0f, 1f, 1f };
            for (int i = 0; i < data.team.Count && i < 3; i++)
            {
                var def = GameDatabase.GetCharacter(data.team[i]);
                if (def == null) continue;
                var holder = new GameObject(def.displayName);
                holder.transform.SetParent(root.transform, false);
                holder.transform.localPosition = new Vector3(xs[i], 0f, zs[i]);
                holder.transform.localRotation = Quaternion.Euler(0f, 180f + xs[i] * -8f, 0f);
                CharacterVisual.BuildHero(def, holder.transform);
            }
        }
    }
}
