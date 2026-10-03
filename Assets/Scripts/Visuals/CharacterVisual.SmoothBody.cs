using System.Collections.Generic;
using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>Marks a body part that is about to be melted into a smooth body (kept out of the per-joint merge).</summary>
    public class MeltPending : MonoBehaviour { }

    /// <summary>Marks a melted, single-surface body mesh.</summary>
    public class MeltedBody : MonoBehaviour { }

    /// <summary>
    /// Sculpted bodies. After a slayer is assembled from parts, the parts that make up the body are melted into a
    /// few continuous surfaces (see <see cref="SdfMesher"/>): the torso and hips, each arm, each leg, and the head
    /// with its jaw, ears and snouts. Seams turn into soft fillets and the many little outlines between parts
    /// disappear, so the character reads as one smooth, hand-modelled figure. The limbs are skinned to the rig's
    /// joints (weights come from which parts were nearest), so elbows, knees and the waist bend smoothly.
    ///
    /// Colours stay as crisp as before: every vertex remembers how far it is from each of the original colours'
    /// parts and the ToonSkin shader paints each pixel with the nearest one. Trims, belts, armour plates, props,
    /// glowing parts, hands, faces and hair stay separate pieces on top.
    ///
    /// The meshing runs on a worker thread; until it is done the original parts are shown. Results are cached
    /// per slayer, so the second time someone appears it is instant.
    /// </summary>
    public partial class CharacterVisual
    {
        const int MeltSlots = 8;

        sealed class MeltChain
        {
            public string name;
            public Transform frame;
            public bool rigid;
            public Transform[] bones;
            public Matrix4x4[] bindposes;
            public readonly List<SdfMesher.Prim> prims = new List<SdfMesher.Prim>();
            public readonly List<GameObject> parts = new List<GameObject>();
            public readonly Color[] slotColor = new Color[MeltSlots];
            public readonly float[] slotKind = new float[MeltSlots];
            public Material source;
            public float cell, k;
            public string key;
            public SdfMesher.Result res;
            public float[] slotDist, boneW;
            public int[] boneIdx;
        }

        sealed class MeltCached
        {
            public Mesh mesh;
            public Material mat;
            public int refs, stamp;
        }

        struct MeltCandidate
        {
            public MeshFilter mf;
            public int bone;
            public SdfMesher.Shape shape;
            public Vector2[] profile;
            public string meshKey;
            public long slotKey;
            public Color color;
            public float kind, weight;
        }

        static readonly Dictionary<string, MeltCached> meltCache = new Dictionary<string, MeltCached>();
        static int meltClock;
        /// <summary>Cached melted surfaces kept when no one is using them (about eight slayers).</summary>
        const int MeltCacheMax = 48;
        static Shader skinShader;
        static bool skinShaderTried;

        readonly List<MeltCached> meltHeld = new List<MeltCached>();
        List<MeltChain> meltChains;
        System.Threading.Tasks.Task meltTask;

        static Shader SkinShader()
        {
            if (!skinShaderTried)
            {
                skinShaderTried = true;
                var s = Resources.Load<Shader>("Shaders/ToonSkin");
                skinShader = s != null && s.isSupported ? s : null;
                if (skinShader == null) Debug.LogWarning("[CharacterVisual] ToonSkin shader unavailable; bodies stay as separate parts.");
            }
            return skinShader;
        }

        /// <summary>True while the smooth body is still being built (the parts are shown meanwhile).</summary>
        public bool MeltBusy { get { return meltTask != null; } }

        /// <summary>True when <see cref="CompleteMelt"/> won't have to wait.</summary>
        public bool MeltReady { get { return meltTask == null || meltTask.IsCompleted; } }

        // ------------------------------------------------------------------ Collect

        /// <summary>Picks the body parts to melt and starts the meshing (called by Stylize, before the merge).</summary>
        void PrepareMelt(CharacterDefinition def)
        {
            if (!GameConfig.SmoothBodies || rig == null || Model == null || SkinShader() == null) return;
            try
            {
                var stops = new HashSet<Transform> { Model, rig.body, rig.pelvis, rig.torso };
                if (head != null) stops.Add(head);
                if (SwordPivot != null) stops.Add(SwordPivot);
                for (int i = 0; i < 2; i++)
                {
                    stops.Add(rig.upper[i]); stops.Add(rig.lower[i]); stops.Add(rig.hand[i]);
                    stops.Add(rig.thigh[i]); stops.Add(rig.shin[i]); stops.Add(rig.foot[i]);
                }
                stops.Remove(null);
                string id = def != null ? def.id : name;
                var chains = new List<MeltChain>();
                // Registered first, so a failure part-way can release the parts already marked.
                meltChains = chains;
                AddChain(chains, id, "core", Model, false, new[] { rig.torso, rig.pelvis }, stops, 0.014f, 0.03f);
                for (int i = 0; i < 2; i++)
                {
                    AddChain(chains, id, "arm" + i, Model, false, new[] { rig.upper[i], rig.lower[i] }, stops, 0.011f, 0.024f);
                    AddChain(chains, id, "leg" + i, Model, false, new[] { rig.thigh[i], rig.shin[i], rig.foot[i] }, stops, 0.012f, 0.024f);
                }
                if (head != null) AddChain(chains, id, "head", head, true, new[] { head }, stops, 0.016f, 0.015f);
                if (chains.Count == 0) { meltChains = null; return; }

                var todo = new List<MeltChain>();
                foreach (var c in chains) if (!meltCache.ContainsKey(c.key)) todo.Add(c);
                if (todo.Count == 0) { ApplyMelt(); return; }
#if UNITY_WEBGL
                foreach (var c in todo) RunMelt(c);
                ApplyMelt();
#else
                meltTask = System.Threading.Tasks.Task.Run(() => { foreach (var c in todo) RunMelt(c); });
#endif
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("[CharacterVisual] smooth body skipped: " + ex.Message);
                CancelMelt();
            }
        }

        static void RunMelt(MeltChain c)
        {
            c.res = new SdfMesher().Build(c.prims, c.cell, c.k);
            if (c.res != null)
                SdfMesher.Attributes(c.res, c.prims, MeltSlots, c.rigid ? 1 : c.bones.Length, 0.015f, out c.slotDist, out c.boneIdx, out c.boneW);
        }

        static bool SkipName(string n)
        {
            if (n.StartsWith("Hair") || n.StartsWith("Melted")) return true;
            switch (n)
            {
                case "Shield": case "Bow": case "Quiver": case "Scabbard": case "Kodachi": case "LeftFan": case "LeftCleaver":
                case "Gauntlet": case "Pauldron": case "Wing": case "Tail": case "FoxTail": case "Cape": case "HalfCape":
                case "ScarfTails": case "ObiBow": case "Bandage": case "Bandages": case "Wisp": case "Wisps": case "MenaceSmoke":
                case "FloatGem": case "Orbit": case "Halo": case "Ribbon": case "Strips": case "Tassel": case "Twintail":
                case "Braid": case "HandShape": case "PosterMouth":
                    return true;
            }
            return false;
        }

        void CollectMelt(Transform t, int bone, HashSet<Transform> stops, Transform frame, List<MeltCandidate> into)
        {
            for (int i = 0; i < t.childCount; i++)
            {
                var c = t.GetChild(i);
                if (stops.Contains(c) || !Plain(c) || SkipName(c.name) || !c.gameObject.activeSelf) continue;
                var mf = c.GetComponent<MeshFilter>();
                var mr = c.GetComponent<MeshRenderer>();
                MeltCandidate cand;
                if (mf != null && mr != null && Meltable(mf, mr, frame, out cand)) { cand.bone = bone; into.Add(cand); }
                CollectMelt(c, bone, stops, frame, into);
            }
        }

        static bool Meltable(MeshFilter mf, MeshRenderer mr, Transform frame, out MeltCandidate cand)
        {
            cand = new MeltCandidate { mf = mf };
            var mesh = mf.sharedMesh;
            var mat = mr.sharedMaterial;
            if (mesh == null || mat == null || !mr.enabled || mr.sharedMaterials.Length != 1) return false;
            if (mat.shader == null || mat.shader.name != "Hashira/Toon") return false;
            if (mat.HasProperty("_Emission"))
            {
                Color em = mat.GetColor("_Emission");
                if (em.r + em.g + em.b > 0.05f) return false;
            }
            Vector2[] prof;
            string key;
            if (MeshFactory.IsSphere(mesh)) { cand.shape = SdfMesher.Shape.Sphere; cand.meshKey = "s"; }
            else if (MeshFactory.IsRoundedCube(mesh)) { cand.shape = SdfMesher.Shape.RoundBox; cand.meshKey = "b"; }
            else if (MeshFactory.IsCone(mesh)) { cand.shape = SdfMesher.Shape.Lathe; cand.profile = new[] { new Vector2(0.5f, 0f), new Vector2(0f, 1f) }; cand.meshKey = "c"; }
            else if (MeshFactory.LatheInfo(mesh, out prof, out key))
            {
                // Bands are trims: they stay crisp pieces on top of the smooth body.
                if (key != null && key.StartsWith("pb")) return false;
                cand.shape = SdfMesher.Shape.Lathe; cand.profile = prof; cand.meshKey = key ?? "l";
            }
            else return false;
            // Thin plates and tiny bits would melt into lumps: they stay as they are.
            Matrix4x4 toFrame = frame.worldToLocalMatrix * mf.transform.localToWorldMatrix;
            Vector3 size = mesh.bounds.size;
            float ex = size.x * ((Vector3)toFrame.GetColumn(0)).magnitude;
            float ey = size.y * ((Vector3)toFrame.GetColumn(1)).magnitude;
            float ez = size.z * ((Vector3)toFrame.GetColumn(2)).magnitude;
            float lo = Mathf.Min(ex, Mathf.Min(ey, ez)), hi = Mathf.Max(ex, Mathf.Max(ey, ez));
            if (lo < 0.024f || hi < 0.03f) return false;
            cand.color = mat.color;
            cand.kind = mat.HasProperty("_TexAmt") && mat.GetFloat("_TexAmt") > 0.5f && mat.HasProperty("_TexKind") ? mat.GetFloat("_TexKind") + 1f : 0f;
            cand.slotKey = ((long)Mathf.RoundToInt(cand.color.r * 63f) << 18) | ((long)Mathf.RoundToInt(cand.color.g * 63f) << 12)
                | ((long)Mathf.RoundToInt(cand.color.b * 63f) << 6) | (long)Mathf.RoundToInt(cand.kind);
            cand.weight = ex * ey * ez;
            return true;
        }

        void AddChain(List<MeltChain> into, string id, string chainName, Transform frame, bool rigid, Transform[] joints, HashSet<Transform> stops, float cell, float k)
        {
            var bones = new List<Transform>();
            foreach (var j in joints) if (j != null) bones.Add(j);
            if (bones.Count == 0) return;
            var cands = new List<MeltCandidate>();
            for (int b = 0; b < bones.Count; b++) CollectMelt(bones[b], b, stops, frame, cands);
            if (cands.Count < 2) return;

            // Up to eight colours: the ones covering the most volume (anything else stays a separate part).
            var weightBySlot = new Dictionary<long, float>();
            foreach (var c in cands) { float w; weightBySlot.TryGetValue(c.slotKey, out w); weightBySlot[c.slotKey] = w + c.weight; }
            var keys = new List<long>(weightBySlot.Keys);
            keys.Sort((a, b) => weightBySlot[b].CompareTo(weightBySlot[a]));
            if (keys.Count > MeltSlots) keys.RemoveRange(MeltSlots, keys.Count - MeltSlots);

            var chain = new MeltChain { name = chainName, frame = frame, rigid = rigid, cell = cell, k = k };
            var hash = new System.Text.StringBuilder(chainName);
            float bestW = -1f;
            Matrix4x4 frameToWorld = frame.localToWorldMatrix;
            foreach (var c in cands)
            {
                int slot = keys.IndexOf(c.slotKey);
                if (slot < 0) continue;
                chain.slotColor[slot] = c.color;
                chain.slotKind[slot] = c.kind;
                var mr = c.mf.GetComponent<MeshRenderer>();
                if (c.weight > bestW) { bestW = c.weight; chain.source = mr.sharedMaterial; }
                Matrix4x4 toLocal = c.mf.transform.worldToLocalMatrix * frameToWorld;
                Matrix4x4 toFrame = frame.worldToLocalMatrix * c.mf.transform.localToWorldMatrix;
                var p = new SdfMesher.Prim
                {
                    shape = c.shape, slot = slot, bone = rigid ? 0 : c.bone,
                    m00 = toLocal.m00, m01 = toLocal.m01, m02 = toLocal.m02, m03 = toLocal.m03,
                    m10 = toLocal.m10, m11 = toLocal.m11, m12 = toLocal.m12, m13 = toLocal.m13,
                    m20 = toLocal.m20, m21 = toLocal.m21, m22 = toLocal.m22, m23 = toLocal.m23,
                };
                if (c.shape == SdfMesher.Shape.Lathe)
                {
                    var r = new float[c.profile.Length];
                    var y = new float[c.profile.Length];
                    for (int i = 0; i < r.Length; i++) { r[i] = c.profile[i].x; y[i] = c.profile[i].y; }
                    p.SetProfile(r, y);
                }
                Bounds lb = c.mf.sharedMesh.bounds;
                p.minX = p.minY = p.minZ = float.MaxValue;
                p.maxX = p.maxY = p.maxZ = float.MinValue;
                for (int q = 0; q < 8; q++)
                {
                    Vector3 corner = new Vector3((q & 1) != 0 ? lb.max.x : lb.min.x, (q & 2) != 0 ? lb.max.y : lb.min.y, (q & 4) != 0 ? lb.max.z : lb.min.z);
                    Vector3 w = toFrame.MultiplyPoint3x4(corner);
                    p.minX = Mathf.Min(p.minX, w.x); p.maxX = Mathf.Max(p.maxX, w.x);
                    p.minY = Mathf.Min(p.minY, w.y); p.maxY = Mathf.Max(p.maxY, w.y);
                    p.minZ = Mathf.Min(p.minZ, w.z); p.maxZ = Mathf.Max(p.maxZ, w.z);
                }
                chain.prims.Add(p);
                chain.parts.Add(c.mf.gameObject);
                hash.Append('|').Append(c.meshKey).Append(slot).Append(p.bone);
                AppendQ(hash, toLocal.m00, toLocal.m01, toLocal.m02, toLocal.m03, toLocal.m10, toLocal.m11, toLocal.m12, toLocal.m13, toLocal.m20, toLocal.m21, toLocal.m22, toLocal.m23);
            }
            if (chain.prims.Count < 2) return;
            for (int s = 0; s < MeltSlots; s++) hash.Append('#').Append(ColorUtility.ToHtmlStringRGB(chain.slotColor[s])).Append(chain.slotKind[s]);
            chain.key = id + ":" + Fnv(hash.ToString());

            if (!rigid)
            {
                chain.bones = bones.ToArray();
                chain.bindposes = new Matrix4x4[bones.Count];
                for (int b = 0; b < bones.Count; b++) chain.bindposes[b] = bones[b].worldToLocalMatrix * frameToWorld;
            }
            // Keep the parts out of the per-joint merge until the smooth body replaces them.
            foreach (var go in chain.parts) if (go.GetComponent<MeltPending>() == null) go.AddComponent<MeltPending>();
            into.Add(chain);
        }

        static void AppendQ(System.Text.StringBuilder sb, params float[] v)
        {
            foreach (var f in v) sb.Append(',').Append(Mathf.RoundToInt(f * 2000f));
        }

        static string Fnv(string s)
        {
            ulong h = 14695981039346656037UL;
            foreach (char ch in s) { h ^= ch; h *= 1099511628211UL; }
            return h.ToString("x16");
        }

        // ------------------------------------------------------------------ Apply

        void PollMelt()
        {
            if (meltTask != null && meltTask.IsCompleted) ApplyMelt();
        }

        /// <summary>Finishes the smooth body now (waits for the worker if needed). Call before reading or recolouring
        /// a slayer's renderers right after building them (silhouettes, snapshots).</summary>
        public void CompleteMelt()
        {
            if (meltTask == null) return;
            try { meltTask.Wait(); }
            catch (System.Exception) { }
            ApplyMelt();
        }

        void ApplyMelt()
        {
            var task = meltTask;
            meltTask = null;
            var chains = meltChains;
            meltChains = null;
            if (chains == null) return;
            if (task != null && task.IsFaulted)
            {
                Debug.LogWarning("[CharacterVisual] smooth body failed: " + (task.Exception != null ? task.Exception.GetBaseException().Message : "?"));
                foreach (var c in chains) Unpend(c);
                return;
            }
            foreach (var c in chains)
            {
                try
                {
                    MeltCached cached;
                    if (!meltCache.TryGetValue(c.key, out cached) || cached.mesh == null)
                    {
                        if (c.res == null || c.res.count < 3) { Unpend(c); continue; }
                        cached = new MeltCached { mesh = MeltMesh(c), mat = MeltMaterial(c) };
                        meltCache[c.key] = cached;
                    }
                    cached.refs++;
                    cached.stamp = ++meltClock;
                    meltHeld.Add(cached);

                    var go = new GameObject("Melted_" + c.name);
                    go.layer = c.frame.gameObject.layer;
                    go.transform.SetParent(c.frame, false);
                    go.AddComponent<MeltedBody>();
                    Renderer rend;
                    if (c.rigid)
                    {
                        go.AddComponent<MeshFilter>().sharedMesh = cached.mesh;
                        rend = go.AddComponent<MeshRenderer>();
                    }
                    else
                    {
                        var smr = go.AddComponent<SkinnedMeshRenderer>();
                        smr.sharedMesh = cached.mesh;
                        smr.bones = c.bones;
                        smr.updateWhenOffscreen = true;
                        rend = smr;
                    }
                    rend.sharedMaterial = cached.mat;
                    rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                    renderers.Add(rend);
                    RemoveMeltedParts(c);
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning("[CharacterVisual] smooth body part " + c.name + " skipped: " + ex.Message);
                    Unpend(c);
                }
            }
            TrimMeltCache();
        }

        static Mesh MeltMesh(MeltChain c)
        {
            var r = c.res;
            int n = r.count;
            var verts = new Vector3[n];
            var norms = new Vector3[n];
            var pal0 = new List<Vector4>(n);
            var pal1 = new List<Vector4>(n);
            var rest = new List<Vector3>(n);
            for (int v = 0; v < n; v++)
            {
                verts[v] = new Vector3(r.pos[v * 3], r.pos[v * 3 + 1], r.pos[v * 3 + 2]);
                norms[v] = new Vector3(r.nrm[v * 3], r.nrm[v * 3 + 1], r.nrm[v * 3 + 2]);
                int s = v * MeltSlots;
                pal0.Add(new Vector4(c.slotDist[s], c.slotDist[s + 1], c.slotDist[s + 2], c.slotDist[s + 3]));
                pal1.Add(new Vector4(c.slotDist[s + 4], c.slotDist[s + 5], c.slotDist[s + 6], c.slotDist[s + 7]));
                rest.Add(verts[v]);
            }
            var mesh = new Mesh { name = "Melted_" + c.name };
            if (n > 65000) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.vertices = verts;
            mesh.normals = norms;
            mesh.SetUVs(1, pal0);
            mesh.SetUVs(2, pal1);
            mesh.SetUVs(3, rest);
            mesh.triangles = r.tris;
            if (!c.rigid)
            {
                var bw = new BoneWeight[n];
                for (int v = 0; v < n; v++)
                {
                    int q = v * 4;
                    bw[v] = new BoneWeight
                    {
                        boneIndex0 = c.boneIdx[q], weight0 = c.boneW[q],
                        boneIndex1 = c.boneIdx[q + 1], weight1 = c.boneW[q + 1],
                        boneIndex2 = c.boneIdx[q + 2], weight2 = c.boneW[q + 2],
                        boneIndex3 = c.boneIdx[q + 3], weight3 = c.boneW[q + 3],
                    };
                }
                mesh.boneWeights = bw;
                mesh.bindposes = c.bindposes;
            }
            mesh.RecalculateBounds();
            return mesh;
        }

        static Material MeltMaterial(MeltChain c)
        {
            var m = new Material(SkinShader()) { name = "ToonSkin_" + c.name };
            for (int s = 0; s < MeltSlots; s++) m.SetColor("_P" + s, c.slotColor[s]);
            m.SetVector("_K0", new Vector4(c.slotKind[0], c.slotKind[1], c.slotKind[2], c.slotKind[3]));
            m.SetVector("_K1", new Vector4(c.slotKind[4], c.slotKind[5], c.slotKind[6], c.slotKind[7]));
            var src = c.source;
            if (src != null)
            {
                foreach (var prop in new[] { "_ShadowColor", "_RimColor", "_OutlineColor" })
                    if (src.HasProperty(prop)) m.SetColor(prop, src.GetColor(prop));
                foreach (var prop in new[] { "_RimPower", "_OutlineWidth" })
                    if (src.HasProperty(prop)) m.SetFloat(prop, src.GetFloat(prop));
            }
            return m;
        }

        /// <summary>The melted parts are gone: drop their meshes (and the objects, unless something hangs off them).</summary>
        static void RemoveMeltedParts(MeltChain c)
        {
            foreach (var go in c.parts)
            {
                if (go == null) continue;
                var mr = go.GetComponent<MeshRenderer>();
                var mf = go.GetComponent<MeshFilter>();
                var mp = go.GetComponent<MeltPending>();
                // Hidden at once (a snapshot may be taken this frame); removed at the end of the frame.
                if (mr != null) { mr.enabled = false; Object.Destroy(mr); }
                if (mf != null) Object.Destroy(mf);
                if (mp != null) Object.Destroy(mp);
                if (go.transform.childCount == 0) Object.Destroy(go);
            }
        }

        static void Unpend(MeltChain c)
        {
            foreach (var go in c.parts)
            {
                if (go == null) continue;
                var mp = go.GetComponent<MeltPending>();
                if (mp != null) Object.Destroy(mp);
            }
        }

        void CancelMelt()
        {
            if (meltChains != null) foreach (var c in meltChains) Unpend(c);
            meltChains = null;
            meltTask = null;
        }

        static void TrimMeltCache()
        {
            while (meltCache.Count > MeltCacheMax)
            {
                string oldest = null;
                int stamp = int.MaxValue;
                foreach (var kv in meltCache)
                    if (kv.Value.refs <= 0 && kv.Value.stamp < stamp) { stamp = kv.Value.stamp; oldest = kv.Key; }
                if (oldest == null) return;
                var e = meltCache[oldest];
                meltCache.Remove(oldest);
                if (e.mesh != null) Object.Destroy(e.mesh);
                if (e.mat != null) Object.Destroy(e.mat);
            }
        }

        void OnDestroy()
        {
            foreach (var e in meltHeld) e.refs--;
            meltHeld.Clear();
            meltChains = null;
            meltTask = null;
            TrimMeltCache();
        }
    }
}
