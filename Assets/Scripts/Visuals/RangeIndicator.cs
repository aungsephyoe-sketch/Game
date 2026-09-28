using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// Shows the active slayer's charged-attack reach on the ground in their element colour: a faint outline with a
    /// fill that grows as the charge builds and pulses when it's full. Unlike <see cref="Telegraph"/> (enemy danger,
    /// red, read by the AI to dodge) this is purely a player aid, so it never registers as a threat.
    /// </summary>
    public class RangeIndicator : MonoBehaviour
    {
        BattleController battle;
        Transform root, area, fill;
        MeshFilter areaMf, fillMf;
        Material areaMat, fillMat;
        float shown;
        int shape = -1; // 0 sector in front, 1 circle on self, 2 circle at aim point

        public static void Attach(BattleController b)
        {
            var ri = b.gameObject.AddComponent<RangeIndicator>();
            ri.battle = b;
        }

        void Build()
        {
            root = new GameObject("RangeIndicator").transform;
            root.SetParent(transform, false);
            areaMat = MaterialFactory.Transparent(new Color(1f, 1f, 1f, 0.2f));
            fillMat = MaterialFactory.Transparent(new Color(1f, 1f, 1f, 0.35f));
            area = Part("Area", areaMat, out areaMf);
            fill = Part("Fill", fillMat, out fillMf);
            fill.localPosition = Vector3.up * 0.01f;
        }

        Transform Part(string name, Material m, out MeshFilter mf)
        {
            var go = new GameObject(name);
            go.transform.SetParent(root, false);
            mf = go.AddComponent<MeshFilter>();
            var mr = go.AddComponent<MeshRenderer>();
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            mr.sharedMaterial = m;
            return go.transform;
        }

        void LateUpdate()
        {
            var pc = battle != null && battle.Team != null ? battle.Team.Active : null;
            float charge = pc != null && pc.IsAlive ? pc.ChargeAmount : 0f;
            shown = Mathf.MoveTowards(shown, charge > 0f ? 1f : 0f, Time.deltaTime * (charge > 0f ? 8f : 5f));
            if (shown <= 0f)
            {
                if (root != null && root.gameObject.activeSelf) root.gameObject.SetActive(false);
                return;
            }
            if (root == null) Build();
            if (!root.gameObject.activeSelf) root.gameObject.SetActive(true);
            if (pc == null) return;

            var style = pc.Def.style;
            int want = style == CombatStyle.Ranged ? 2 : style == CombatStyle.Brawler || style == CombatStyle.Healer ? 1 : 0;
            if (want != shape)
            {
                shape = want;
                var mesh = shape == 0 ? MeshFactory.Sector(160f, 0f) : MeshFactory.Disc();
                areaMf.sharedMesh = mesh;
                fillMf.sharedMesh = mesh;
            }
            float radius = shape == 0 ? 3.8f : shape == 1 ? (style == CombatStyle.Healer ? 4f : 3.6f) : 3.4f;
            Vector3 fwd = pc.transform.forward;
            fwd.y = 0f;
            if (fwd.sqrMagnitude < 0.001f) fwd = Vector3.forward;
            Vector3 at = pc.Position;
            if (shape == 2)
            {
                var lk = PlayerCharacter.LockTarget;
                at = lk != null && lk.IsAlive ? lk.Position : pc.Position + fwd.normalized * 6f;
            }
            root.SetPositionAndRotation(new Vector3(at.x, at.y + 0.05f, at.z), Quaternion.LookRotation(fwd));
            Color c = ElementChart.ColorOf(pc.Def.element);
            float full = charge >= 1f ? 1f : 0f;
            float pulse = full * (0.5f + 0.5f * Mathf.Sin(Time.time * 18f));
            areaMat.color = new Color(c.r, c.g, c.b, (0.16f + 0.1f * full) * shown);
            fillMat.color = new Color(Mathf.Lerp(c.r, 1f, pulse * 0.4f), Mathf.Lerp(c.g, 1f, pulse * 0.4f), Mathf.Lerp(c.b, 1f, pulse * 0.4f), (0.28f + 0.2f * pulse) * shown);
            float k = Mathf.Max(0.05f, charge);
            area.localScale = new Vector3(radius, 1f, radius);
            fill.localScale = new Vector3(radius * k, 1f, radius * k);
        }
    }
}
