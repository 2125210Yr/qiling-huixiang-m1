using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Resonance.App
{
    /// <summary>
    /// Our-slice input → number delay. GT column still UNKNOWN.
    /// Not T28: no same-setup overlay, no original-game chain.
    /// </summary>
    public static class HitChainProbe
    {
        static float _inAt;
        static string _kind;
        static readonly List<string> Lines = new List<string>(8);

        public static void Reset()
        {
            Lines.Clear();
            _inAt = 0f;
            _kind = null;
        }

        public static void Input(string kind)
        {
            _kind = string.IsNullOrEmpty(kind) ? "input" : kind;
            _inAt = Time.unscaledTime;
        }

        public static void Cancel()
        {
            _inAt = 0f;
            _kind = null;
        }

        public static void NumberShown()
        {
            if (_inAt <= 0f) return;
            var dt = Time.unscaledTime - _inAt;
            Lines.Add((_kind ?? "input") + " in→num=" + dt.ToString("0.000"));
            _inAt = 0f;
            _kind = null;
        }

        public static void AppendTo(StringBuilder sb)
        {
            if (sb == null) return;
            if (Lines.Count == 0)
            {
                sb.AppendLine("hit-chain=none");
                return;
            }
            for (int i = 0; i < Lines.Count; i++)
                sb.AppendLine("hit-chain:" + Lines[i]);
        }
    }
}
