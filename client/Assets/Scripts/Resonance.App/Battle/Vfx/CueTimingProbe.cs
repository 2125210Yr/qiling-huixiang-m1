using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Resonance.App
{
    /// <summary>
    /// Wall-clock overlay holds for TIMING_DIFF our-slice column.
    /// Not T28. Not GT. Measures our VFX first-on to destroy/hide.
    /// </summary>
    public static class CueTimingProbe
    {
        struct Open
        {
            public string Cue;
            public float On;
        }

        static readonly Dictionary<int, Open> Live = new Dictionary<int, Open>(8);
        static readonly List<string> Lines = new List<string>(16);

        public static void Reset()
        {
            Live.Clear();
            Lines.Clear();
        }

        public static void On(string cue, int id)
        {
            if (string.IsNullOrEmpty(cue) || id == 0) return;
            Live[id] = new Open { Cue = cue, On = Time.unscaledTime };
            CueStripRecorder.TryBegin(cue, id);
        }

        public static void Off(string cue, int id)
        {
            Open o;
            if (!Live.TryGetValue(id, out o)) return;
            Live.Remove(id);
            var hold = Time.unscaledTime - o.On;
            var name = string.IsNullOrEmpty(cue) ? o.Cue : cue;
            Lines.Add(name + " hold=" + hold.ToString("0.000")
                + " on=" + o.On.ToString("0.000")
                + " off=" + Time.unscaledTime.ToString("0.000"));
            CueStripRecorder.TryEnd(name, id);
        }

        public static void AppendTo(StringBuilder sb)
        {
            if (sb == null) return;
            if (Lines.Count == 0 && Live.Count == 0)
            {
                sb.AppendLine("cue-timing=none");
                return;
            }
            for (int i = 0; i < Lines.Count; i++)
                sb.AppendLine("cue-timing:" + Lines[i]);
            foreach (var kv in Live)
            {
                var hold = Time.unscaledTime - kv.Value.On;
                sb.AppendLine("cue-timing-open:" + kv.Value.Cue
                    + " hold=" + hold.ToString("0.000")
                    + " on=" + kv.Value.On.ToString("0.000"));
            }
        }
    }
}
