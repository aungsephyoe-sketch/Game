using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// Faces that act: a character's mouth moves while they talk, and their brows, eyes and mouth shift into an
    /// expression (happy, angry, sad, surprised, determined). Talking also nods the head a little. Works on every
    /// character built on the jointed rig (it finds the eyes, brows and mouth on the head by their placement).
    /// </summary>
    [DefaultExecutionOrder(200)]
    public class CharacterExpression : MonoBehaviour
    {
        public enum Mood { Neutral, Happy, Angry, Sad, Surprised, Determined }

        public bool Talking;
        public Mood Current = Mood.Neutral;

        Transform head, mouth;
        Transform[] eyes = new Transform[0], brows = new Transform[0];
        Vector3 mouthBase;
        Vector3[] eyeBase, browBase;
        Quaternion[] browRotBase;
        float mood01;
        Mood shown = Mood.Neutral;
        Quaternion lastNod = Quaternion.identity;
        float talkPhase;

        public static CharacterExpression For(CharacterVisual v)
        {
            if (v == null) return null;
            var e = v.GetComponent<CharacterExpression>();
            if (e == null) e = v.gameObject.AddComponent<CharacterExpression>();
            return e;
        }

        void Start()
        {
            var cv = GetComponent<CharacterVisual>();
            head = cv != null ? cv.HeadTransform : null;
            if (head == null) return;
            var faces = new System.Collections.Generic.List<Transform>();
            foreach (Transform c in head) if (c.name == "Face") faces.Add(c);
            // Mouth: the lowest centred face part. Brows: the highest pair off-centre.
            float lowY = float.MaxValue;
            foreach (var f in faces)
                if (Mathf.Abs(f.localPosition.x) < 0.03f && f.localPosition.y < lowY && f.GetComponentInChildren<EyeLid>() == null) { lowY = f.localPosition.y; mouth = f; }
            var lids = head.GetComponentsInChildren<EyeLid>(true);
            eyes = new Transform[lids.Length];
            for (int i = 0; i < lids.Length; i++) eyes[i] = lids[i].transform;
            float eyeY = eyes.Length > 0 ? eyes[0].localPosition.y : 0f;
            var bl = new System.Collections.Generic.List<Transform>();
            foreach (var f in faces)
                if (Mathf.Abs(f.localPosition.x) > 0.04f && f.localPosition.y > eyeY + 0.04f && f.GetComponent<EyeLid>() == null) bl.Add(f);
            bl.Sort((a, b) => b.localPosition.y.CompareTo(a.localPosition.y));
            if (bl.Count > 2) bl.RemoveRange(2, bl.Count - 2);
            brows = bl.ToArray();
            if (mouth != null) mouthBase = mouth.localScale;
            eyeBase = new Vector3[eyes.Length];
            for (int i = 0; i < eyes.Length; i++) eyeBase[i] = eyes[i].localScale;
            browBase = new Vector3[brows.Length];
            browRotBase = new Quaternion[brows.Length];
            for (int i = 0; i < brows.Length; i++) { browBase[i] = brows[i].localPosition; browRotBase[i] = brows[i].localRotation; }
        }

        void LateUpdate()
        {
            if (head == null) return;
            float dt = Time.unscaledDeltaTime;
            if (shown != Current) { mood01 = Mathf.MoveTowards(mood01, 0f, dt * 6f); if (mood01 <= 0f) shown = Current; }
            else mood01 = Mathf.MoveTowards(mood01, Current == Mood.Neutral ? 0f : 1f, dt * 5f);
            float m = mood01;

            // Mouth: flaps while talking; shape per mood.
            if (mouth != null)
            {
                talkPhase += dt * 16f;
                float open = Talking ? 0.6f + 0.6f * Mathf.Abs(Mathf.Sin(talkPhase) * Mathf.Sin(talkPhase * 0.37f + 1f)) : 0f;
                Vector3 shape = Vector3.one;
                if (shown == Mood.Happy) shape = new Vector3(1.35f, 1.2f, 1f);
                else if (shown == Mood.Surprised) shape = new Vector3(0.75f, 1.9f, 1f);
                else if (shown == Mood.Sad) shape = new Vector3(0.8f, 0.7f, 1f);
                else if (shown == Mood.Angry || shown == Mood.Determined) shape = new Vector3(1.2f, 0.8f, 1f);
                shape = Vector3.Lerp(Vector3.one, shape, m);
                mouth.localScale = Vector3.Scale(mouthBase, new Vector3(shape.x, shape.y * (1f + open), shape.z));
            }
            // Eyes: wide when surprised, squint when happy or determined (blinks still apply on top).
            float eyeY = 1f, eyeX = 1f;
            if (shown == Mood.Surprised) { eyeY = 1.2f; eyeX = 1.1f; }
            else if (shown == Mood.Happy) eyeY = 0.6f;
            else if (shown == Mood.Determined || shown == Mood.Angry) eyeY = 0.8f;
            for (int i = 0; i < eyes.Length; i++)
            {
                if (eyes[i] == null) continue;
                Vector3 target = Vector3.Scale(eyeBase[i], new Vector3(Mathf.Lerp(1f, eyeX, m), Mathf.Lerp(1f, eyeY, m), 1f));
                // Only nudge the width here; the Blinker owns the height, so blend toward the mood without fighting it.
                eyes[i].localScale = new Vector3(target.x, Mathf.Min(eyes[i].localScale.y, target.y) * 0.3f + target.y * 0.7f, target.z);
            }
            // Brows: angry slants in and down, sad lifts the inner ends, surprised raises both.
            for (int i = 0; i < brows.Length; i++)
            {
                if (brows[i] == null) continue;
                float side = Mathf.Sign(brows[i].localPosition.x);
                float tilt = 0f, lift = 0f;
                if (shown == Mood.Angry) { tilt = -22f; lift = -0.012f; }
                else if (shown == Mood.Determined) { tilt = -12f; lift = -0.006f; }
                else if (shown == Mood.Sad) { tilt = 18f; lift = 0.006f; }
                else if (shown == Mood.Surprised) lift = 0.025f;
                else if (shown == Mood.Happy) lift = 0.01f;
                brows[i].localPosition = browBase[i] + Vector3.up * lift * m;
                brows[i].localRotation = browRotBase[i] * Quaternion.Euler(0f, 0f, side * tilt * m);
            }
            // A small nod while talking (applied on top of whatever else turns the head).
            Quaternion nod = Talking ? Quaternion.Euler(Mathf.Sin(talkPhase * 0.45f) * 5f, Mathf.Sin(talkPhase * 0.2f) * 4f, 0f) : Quaternion.identity;
            head.localRotation = head.localRotation * Quaternion.Inverse(lastNod) * nod;
            lastNod = nod;
        }

        /// <summary>Reads a line of dialogue for its mood.</summary>
        public static Mood FromText(string text)
        {
            if (string.IsNullOrEmpty(text)) return Mood.Neutral;
            string t = text.ToLowerInvariant();
            bool bang = text.Contains("!");
            if (t.Contains("?!") || t.StartsWith("what") && bang || t.Contains("impossible") || t.Contains("no way") || t.Contains("how is")) return Mood.Surprised;
            if (t.Contains("sorry") || t.Contains("gone") || t.Contains("lost") || t.Contains("forgive") || t.Contains("alone") || t.Contains("miss ") || t.StartsWith("...")) return Mood.Sad;
            if (t.Contains("thank") || t.Contains("haha") || t.Contains("welcome") || t.Contains("great") || t.Contains("well done") || t.Contains("proud") || t.Contains("home") || t.Contains("festival")) return Mood.Happy;
            if (bang && (t.Contains("demon") || t.Contains("never") || t.Contains("destroy") || t.Contains("you'll pay") || t.Contains("fool") || t.Contains("die") || t.Contains("kneel"))) return Mood.Angry;
            if (bang || t.Contains("will protect") || t.Contains("i'll") || t.Contains("we must") || t.Contains("ready")) return Mood.Determined;
            return Mood.Neutral;
        }
    }
}
