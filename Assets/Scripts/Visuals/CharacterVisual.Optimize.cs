using System.Collections.Generic;
using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// Mobile budget for the detailed characters: after a premium body is built, every rigid part that rides
    /// on the same joint (upper arm, forearm, hand, thigh, shin, foot, hips, chest, head, weapon) is merged into
    /// one mesh per material. A fighter made of a few hundred pieces renders in a few dozen draw calls, and
    /// anything that moves on its own (swaying scarves, spinning halos, glows, particles, trails) is left alone.
    /// </summary>
    public partial class CharacterVisual
    {
        /// <summary>Set while a hero is being built: the merge waits until the stylize pass has added its pieces.</summary>
        bool deferOptimize;

        void OptimizeParts()
        {
            if (rig == null || deferOptimize) return;
            var joints = new HashSet<Transform>();
            joints.Add(Model);
            joints.Add(rig.body);
            joints.Add(rig.pelvis);
            joints.Add(rig.torso);
            if (head != null) joints.Add(head);
            if (SwordPivot != null) joints.Add(SwordPivot);
            for (int i = 0; i < 2; i++)
            {
                joints.Add(rig.upper[i]); joints.Add(rig.lower[i]); joints.Add(rig.hand[i]);
                joints.Add(rig.thigh[i]); joints.Add(rig.shin[i]); joints.Add(rig.foot[i]);
            }
            joints.Remove(null);
            foreach (var j in joints) Merge(j, joints);

            // The hit-flash list now points at the merged meshes.
            renderers.Clear();
            foreach (var r in Model.GetComponentsInChildren<Renderer>(true))
                if (r is MeshRenderer || r is SkinnedMeshRenderer) renderers.Add(r);
        }

        /// <summary>True when a transform only holds a mesh (nothing that animates or needs to stay separate).</summary>
        static bool Plain(Transform t)
        {
            // Face parts (brows, mouth, marks) stay separate: expressions move them.
            if (t.name == "Face") return false;
            foreach (var c in t.GetComponents<Component>())
                if (!(c is Transform) && !(c is MeshFilter) && !(c is MeshRenderer) && !(c is Collider)) return false;
            return true;
        }

        static void Collect(Transform t, HashSet<Transform> joints, List<MeshFilter> into)
        {
            for (int i = 0; i < t.childCount; i++)
            {
                var c = t.GetChild(i);
                if (joints.Contains(c) || !Plain(c)) continue;
                var mf = c.GetComponent<MeshFilter>();
                var mr = c.GetComponent<MeshRenderer>();
                if (mf != null && mr != null && mf.sharedMesh != null && mr.sharedMaterial != null) into.Add(mf);
                Collect(c, joints, into);
            }
        }

        static void Merge(Transform joint, HashSet<Transform> joints)
        {
            var parts = new List<MeshFilter>();
            Collect(joint, joints, parts);
            if (parts.Count < 2) return;
            var groups = new Dictionary<Material, List<MeshFilter>>();
            foreach (var mf in parts)
            {
                var mat = mf.GetComponent<MeshRenderer>().sharedMaterial;
                List<MeshFilter> list;
                if (!groups.TryGetValue(mat, out list)) { list = new List<MeshFilter>(); groups[mat] = list; }
                list.Add(mf);
            }
            Matrix4x4 toJoint = joint.worldToLocalMatrix;
            var dead = new List<GameObject>();
            foreach (var kv in groups)
            {
                if (kv.Value.Count < 2) continue;
                var ci = new CombineInstance[kv.Value.Count];
                int verts = 0;
                bool shadows = false;
                for (int i = 0; i < kv.Value.Count; i++)
                {
                    var mf = kv.Value[i];
                    ci[i] = new CombineInstance { mesh = mf.sharedMesh, subMeshIndex = 0, transform = toJoint * mf.transform.localToWorldMatrix };
                    verts += mf.sharedMesh.vertexCount;
                    if (mf.GetComponent<MeshRenderer>().shadowCastingMode != UnityEngine.Rendering.ShadowCastingMode.Off) shadows = true;
                }
                var mesh = new Mesh { name = "Merged" };
                if (verts > 65000) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
                mesh.CombineMeshes(ci, true, true);
                mesh.RecalculateBounds();
                var go = new GameObject("Merged_" + kv.Key.name);
                go.transform.SetParent(joint, false);
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var mr = go.AddComponent<MeshRenderer>();
                mr.sharedMaterial = kv.Key;
                mr.shadowCastingMode = shadows ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off;
                foreach (var mf in kv.Value)
                {
                    // Drop the mesh; keep the transform only if something else still hangs off it.
                    var g = mf.gameObject;
                    Object.DestroyImmediate(g.GetComponent<MeshRenderer>());
                    Object.DestroyImmediate(mf);
                    dead.Add(g);
                }
            }
            foreach (var g in dead)
                if (g != null && g.transform.childCount == 0 && Plain(g.transform)) Object.DestroyImmediate(g);
        }
    }
}
