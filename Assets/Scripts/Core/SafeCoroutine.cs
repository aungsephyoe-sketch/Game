using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// Runs a coroutine (and every coroutine it yields) so that an exception in one step is logged and that step is
    /// skipped, instead of silently killing the whole sequence (which would freeze a mission or leave a cutscene black).
    /// </summary>
    public static class SafeCoroutine
    {
        public static IEnumerator Run(IEnumerator root, string label)
        {
            var stack = new Stack<IEnumerator>();
            stack.Push(root);
            while (stack.Count > 0)
            {
                var top = stack.Peek();
                bool more;
                try { more = top.MoveNext(); }
                catch (System.Exception ex)
                {
                    Debug.LogError("[" + label + "] step failed and was skipped: " + ex);
                    more = false;
                }
                if (!more) { stack.Pop(); continue; }
                var cur = top.Current;
                var nested = cur as IEnumerator;
                if (nested != null) { stack.Push(nested); continue; }
                yield return cur;
            }
        }
    }
}
