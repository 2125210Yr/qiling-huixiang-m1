using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Resonance.App
{
    /// <summary>
    /// 大厅身前火星：橙芯 + 暖晕，慢升、闪烁，近亮远暗两层视差。身后火星由 LobbyStage 画。
    /// </summary>
    public sealed class HomeIdleFx : MonoBehaviour
    {
        const int GlowN = 16;
        const int SparkN = 14;

        struct Mote
        {
            public RectTransform Rt;
            public Image Halo;
            public Image Core;
            public float X;
            public float Y;
            public float Rise;
            public float Sway;
            public float Phase;
            public float Twinkle;
            public float BaseA;
            public float Spin;
            public float Size;
        }

        Mote[] _motes;

        public static void Attach(Transform parent, List<GameObject> built)
        {
            if (parent == null) return;
            Wipe(parent);
            var root = OverlayDraw.Group(parent, built, "闲尘");
            if (root == null) return;
            var cg = root.gameObject.GetComponent<CanvasGroup>();
            if (cg == null) cg = root.gameObject.AddComponent<CanvasGroup>();
            cg.interactable = false;
            cg.blocksRaycasts = false;
            var fx = root.gameObject.GetComponent<HomeIdleFx>();
            if (fx == null) fx = root.gameObject.AddComponent<HomeIdleFx>();
            fx.Build();
        }

        static void Wipe(Transform parent)
        {
            if (parent == null) return;
            var found = parent.GetComponentsInChildren<HomeIdleFx>(true);
            for (int i = found.Length - 1; i >= 0; i--)
            {
                var fx = found[i];
                if (fx == null) continue;
                Object.DestroyImmediate(fx.gameObject);
            }
        }

        void Build()
        {
            var n = GlowN + SparkN;
            _motes = new Mote[n];
            var rng = new System.Random(29);
            for (int i = 0; i < n; i++)
            {
                var spark = i >= GlowN;
                var x = 0.10f + (float)rng.NextDouble() * 0.80f;
                var y = 0.12f + (float)rng.NextDouble() * 0.74f;
                // 深度视差：近处大、亮、升得快；远处小、暗、慢。
                var depth = Mathf.Pow((float)rng.NextDouble(), 0.8f);
                var sizeMul = 0.70f + 0.75f * depth;
                var alphaMul = 0.55f + 0.45f * depth;
                var haloSz = (spark ? 18f + rng.Next(12) : 30f + rng.Next(26)) * sizeMul;
                var coreSz = (spark ? 8f + rng.Next(7) : 9f + rng.Next(9)) * (0.70f + 0.60f * depth);
                var hot = Color.Lerp(VisualTokens.Ember, VisualTokens.EmberHot, (float)rng.NextDouble() * 0.55f);
                var haloCol = hot;
                haloCol.a = (spark ? 0.50f : 0.46f) * alphaMul;
                var coreCol = Color.Lerp(hot, Color.white, spark ? 0.38f : 0.18f);
                coreCol.a = (spark ? 0.96f : 0.88f) * alphaMul;

                var go = new GameObject(spark ? "星屑" : "火星", typeof(RectTransform));
                go.transform.SetParent(transform, false);
                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin = rt.anchorMax = new Vector2(x, y);
                rt.sizeDelta = Vector2.zero;
                rt.anchoredPosition = Vector2.zero;

                var halo = Child(go.transform, "halo", UiSprites.Soft(), haloCol, haloSz);
                var core = Child(go.transform, "core",
                    spark ? UiSprites.Spark() : UiSprites.Circle(), coreCol, coreSz);

                _motes[i] = new Mote
                {
                    Rt = rt,
                    Halo = halo,
                    Core = core,
                    X = x,
                    Y = y,
                    Rise = (0.018f + (float)rng.NextDouble() * 0.028f) * (0.70f + 0.70f * depth),
                    Sway = (0.012f + (float)rng.NextDouble() * 0.018f) * (0.80f + 0.60f * depth),
                    Phase = (float)rng.NextDouble() * 6.2832f,
                    Twinkle = spark ? 1.4f + (float)rng.NextDouble() * 1.8f : 0.55f + (float)rng.NextDouble() * 0.70f,
                    BaseA = coreCol.a,
                    Spin = spark ? (rng.NextDouble() < 0.5 ? -22f : 22f) : (rng.NextDouble() < 0.5 ? -8f : 8f),
                    Size = coreSz
                };
            }
        }

        void LateUpdate()
        {
            if (_motes == null) return;
            var dt = Time.unscaledDeltaTime;
            var time = Time.unscaledTime;
            for (int i = 0; i < _motes.Length; i++)
            {
                var m = _motes[i];
                if (m.Rt == null) continue;
                m.Y += m.Rise * dt;
                if (m.Y > 0.94f) m.Y = 0.10f;
                var sway = Mathf.Sin(time * 0.55f + m.Phase) * m.Sway;
                m.Rt.anchorMin = m.Rt.anchorMax = new Vector2(m.X + sway, m.Y);
                if (m.Core != null)
                    m.Core.rectTransform.localEulerAngles = new Vector3(0f, 0f,
                        m.Core.rectTransform.localEulerAngles.z + m.Spin * dt);
                var wave = 0.5f + 0.5f * Mathf.Sin(time * m.Twinkle + m.Phase);
                var spark = Mathf.Pow(wave, 5f);
                var a = m.BaseA * (0.42f + 0.58f * wave);
                if (m.Halo != null)
                {
                    var c = m.Halo.color;
                    c.a = a * 0.55f;
                    m.Halo.color = c;
                    m.Halo.rectTransform.localScale = Vector3.one * (0.86f + 0.28f * wave);
                }
                if (m.Core != null)
                {
                    var c = m.Core.color;
                    c.a = a;
                    m.Core.color = c;
                    m.Core.rectTransform.localScale = Vector3.one * (0.78f + 0.55f * spark);
                }
                _motes[i] = m;
            }
        }

        static Image Child(Transform parent, string name, Sprite sprite, Color color, float size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(size, size);
            rt.anchoredPosition = Vector2.zero;
            var img = go.GetComponent<Image>();
            UiSprites.Apply(img, sprite);
            img.color = color;
            img.raycastTarget = false;
            return img;
        }
    }
}
