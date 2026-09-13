using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace Resonance.App
{
    /// <summary>
    /// Smoke-only 30fps overlay strip. Writes our-slice frames for TIMING_DIFF.
    /// Not T28: no GT overlay, no input→hit chain, Game view fps may drop.
    /// First clip of each cue name only.
    /// </summary>
    public sealed class CueStripRecorder : MonoBehaviour
    {
        public const float TargetFps = 30f;
        const float FrameSec = 1f / TargetFps;
        const int MaxFrames = 90;

        static CueStripRecorder _host;
        static readonly HashSet<string> Seen = new HashSet<string>();
        static readonly List<string> Summary = new List<string>(8);

        string _cue;
        int _id;
        string _dir;
        int _frame;
        int _drops;
        float _onAt;
        float _nextAt;
        bool _grabbing;
        readonly List<string> _index = new List<string>(96);

        public static bool Grabbing
        {
            get { return _host != null && _host._grabbing; }
        }

        public static void ResetForSmoke()
        {
            Seen.Clear();
            Summary.Clear();
            EnsureHost();
            if (_host != null) _host.Flush();
            var root = RootDir();
            try
            {
                if (Directory.Exists(root)) Directory.Delete(root, true);
            }
            catch { }
            Directory.CreateDirectory(root);
        }

        public static void TryBegin(string cue, int id)
        {
            if (_host == null || string.IsNullOrEmpty(cue) || id == 0) return;
            if (Seen.Contains(cue)) return;
            Seen.Add(cue);
            _host.Begin(cue, id);
        }

        public static void TryEnd(string cue, int id)
        {
            if (_host == null) return;
            if (_host._id == id && _host._dir != null)
                _host.Flush();
        }

        public static void FlushAll()
        {
            if (_host != null) _host.Flush();
        }

        public static void AppendTo(StringBuilder sb)
        {
            if (sb == null) return;
            if (Summary.Count == 0)
            {
                sb.AppendLine("cue-strip=none");
                return;
            }
            for (int i = 0; i < Summary.Count; i++)
                sb.AppendLine("cue-strip:" + Summary[i]);
        }

        static void EnsureHost()
        {
            if (_host != null) return;
            var go = new GameObject("CueStripRecorder");
            DontDestroyOnLoad(go);
            _host = go.AddComponent<CueStripRecorder>();
        }

        static string RootDir()
        {
            return Path.Combine(CaptureShots.CapturesDir(), "strip");
        }

        void Begin(string cue, int id)
        {
            Flush();
            _cue = cue;
            _id = id;
            _frame = 0;
            _drops = 0;
            _onAt = Time.unscaledTime;
            _nextAt = _onAt;
            _dir = Path.Combine(RootDir(), cue);
            _index.Clear();
            try { Directory.CreateDirectory(_dir); }
            catch { _dir = null; }
        }

        void Update()
        {
            if (_dir == null || _grabbing) return;
            if (_frame >= MaxFrames)
            {
                Flush();
                return;
            }
            if (Time.unscaledTime < _nextAt) return;
            if (CaptureShots.Busy)
            {
                _drops++;
                _nextAt += FrameSec;
                return;
            }
            StartCoroutine(Grab());
        }

        IEnumerator Grab()
        {
            _grabbing = true;
            yield return new WaitForEndOfFrame();
            if (_dir == null)
            {
                _grabbing = false;
                yield break;
            }
            var t = Time.unscaledTime - _onAt;
            Texture2D tex = null;
            Texture2D small = null;
            try
            {
                tex = ScreenCapture.CaptureScreenshotAsTexture();
                if (tex != null)
                {
                    small = Downscale(tex, 360);
                    var bytes = (small != null ? small : tex).EncodeToJPG(55);
                    if (bytes != null && bytes.Length > 0)
                    {
                        _frame++;
                        var name = "f_" + _frame.ToString("000") + ".jpg";
                        File.WriteAllBytes(Path.Combine(_dir, name), bytes);
                        _index.Add(name + " t=" + t.ToString("0.000"));
                    }
                    else
                        _drops++;
                }
                else
                    _drops++;
            }
            catch
            {
                _drops++;
            }
            finally
            {
                if (small != null && small != tex) Destroy(small);
                if (tex != null) Destroy(tex);
            }
            _nextAt += FrameSec;
            _grabbing = false;
        }

        void Flush()
        {
            if (string.IsNullOrEmpty(_dir))
            {
                _cue = null;
                _id = 0;
                return;
            }
            var hold = Time.unscaledTime - _onAt;
            var line = (_cue ?? "?")
                + " frames=" + _frame
                + " drops=" + _drops
                + " hold=" + hold.ToString("0.000")
                + " dir=" + _dir;
            Summary.Add(line);
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("cue=" + _cue);
                sb.AppendLine("target_fps=" + TargetFps.ToString("0"));
                sb.AppendLine("frames=" + _frame);
                sb.AppendLine("drops=" + _drops);
                sb.AppendLine("hold=" + hold.ToString("0.000"));
                sb.AppendLine("measured_fps=" + (hold > 0.01f ? (_frame / hold).ToString("0.00") : "0"));
                sb.AppendLine("uncertainty_frames=>=T28");
                sb.AppendLine("not_t28=true");
                for (int i = 0; i < _index.Count; i++)
                    sb.AppendLine(_index[i]);
                File.WriteAllText(Path.Combine(_dir, "INDEX.txt"), sb.ToString());
            }
            catch { }
            _dir = null;
            _cue = null;
            _id = 0;
            _frame = 0;
            _index.Clear();
        }

        static Texture2D Downscale(Texture2D src, int maxW)
        {
            if (src == null || maxW < 8 || src.width <= maxW) return src;
            var nh = Mathf.Max(1, src.height * maxW / src.width);
            var rt = RenderTexture.GetTemporary(maxW, nh, 0, RenderTextureFormat.ARGB32);
            var prev = RenderTexture.active;
            Graphics.Blit(src, rt);
            RenderTexture.active = rt;
            var dst = new Texture2D(maxW, nh, TextureFormat.RGB24, false);
            dst.ReadPixels(new Rect(0, 0, maxW, nh), 0, 0);
            dst.Apply(false, false);
            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);
            return dst;
        }
    }
}
