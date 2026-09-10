using UnityEngine;
using UnityEngine.UI;

namespace Resonance.App
{
    /// <summary>
    /// Ice motes along C001's rapier. Lives on the standee so zoom/pan carry it.
    /// Does not deform the still.
    /// </summary>
    public sealed class IceAura : MonoBehaviour
    {
        struct Mote
        {
            public RectTransform Rt;
            public Image Img;
            public Vector2 Origin;
            public Vector2 Drift;
            public float Phase;
            public float Speed;
            public float BaseA;
            public float Spin;
        }

        // Original 冰刃 540x960 still: cyan rapier, lower-left. Not mint hair.
        static readonly Vector2 Hilt = new Vector2(0.3584f, 0.4774f);
        static readonly Vector2 Tip = new Vector2(0.1902f, 0.1696f);

        Mote[] _motes;
        RectTransform _host;

        public static IceAura Attach(RectTransform standee)
        {
            if (standee == null) return null;
            var fx = standee.GetComponent<IceAura>();
            if (fx == null) fx = standee.gameObject.AddComponent<IceAura>();
            fx.Build(standee);
            return fx;
        }

        void Build(RectTransform host)
        {
            _host = host;
            AddBladeGlow(host);
            const int n = 42;
            _motes = new Mote[n];
            var rng = new System.Random(19);
            for (int i = 0; i < n; i++)
            {
                var go = new GameObject(i < 26 ? "frost" : "shard", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(host, false);
                var rt = go.GetComponent<RectTransform>();
                var t = (float)rng.NextDouble();
                var along = Vector2.Lerp(Hilt, Tip, t);
                along += new Vector2(
                    ((float)rng.NextDouble() - 0.5f) * 0.05f,
                    ((float)rng.NextDouble() - 0.5f) * 0.04f);
                rt.anchorMin = rt.anchorMax = along;
                rt.anchoredPosition = Vector2.zero;
                var kind = i % 4;
                Sprite spr;
                Vector2 sz;
                float spin0;
                if (kind == 0)
                {
                    spr = UiSprites.IceShard();
                    sz = new Vector2(10 + rng.Next(8), 22 + rng.Next(18));
                    spin0 = Mathf.Atan2(Tip.y - Hilt.y, Tip.x - Hilt.x) * Mathf.Rad2Deg - 90f + rng.Next(-12, 12);
                }
                else if (kind == 1)
                {
                    spr = UiSprites.IceCrystal();
                    sz = new Vector2(14 + rng.Next(12), 14 + rng.Next(12));
                    spin0 = rng.Next(360);
                }
                else if (kind == 2)
                {
                    spr = UiSprites.IceFlake();
                    sz = new Vector2(16 + rng.Next(14), 16 + rng.Next(14));
                    spin0 = rng.Next(360);
                }
                else
                {
                    spr = UiSprites.IceSpark();
                    sz = new Vector2(12 + rng.Next(10), 12 + rng.Next(10));
                    spin0 = rng.Next(360);
                }
                rt.sizeDelta = sz;
                rt.localEulerAngles = new Vector3(0, 0, spin0);
                var img = go.GetComponent<Image>();
                UiSprites.Apply(img, spr);
                var ice = Color.Lerp(VisualTokens.IceCore, VisualTokens.IceShard, (float)rng.NextDouble());
                if (rng.NextDouble() < 0.28) ice = Color.Lerp(ice, VisualTokens.Aurora, 0.5f);
                ice.a = 0.45f + (float)rng.NextDouble() * 0.45f;
                img.color = ice;
                img.raycastTarget = false;
                _motes[i] = new Mote
                {
                    Rt = rt,
                    Img = img,
                    Origin = along,
                    Drift = new Vector2(
                        ((float)rng.NextDouble() - 0.5f) * 36f,
                        22f + (float)rng.NextDouble() * 48f),
                    Phase = (float)rng.NextDouble() * 6.28f,
                    Speed = 0.28f + (float)rng.NextDouble() * 0.40f,
                    BaseA = ice.a,
                    Spin = kind == 0 ? (rng.NextDouble() < 0.5 ? -10f : 10f) : (rng.NextDouble() < 0.5 ? -22f : 22f)
                };
            }
        }

        static void AddBladeGlow(RectTransform host)
        {
            var go = new GameObject("bladeGlow", typeof(RectTransform), typeof(Image), typeof(PulseGlow));
            go.transform.SetParent(host, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = (Hilt + Tip) * 0.5f;
            rt.sizeDelta = new Vector2(22f, 260f);
            var ang = Mathf.Atan2(Tip.y - Hilt.y, Tip.x - Hilt.x) * Mathf.Rad2Deg - 90f;
            rt.localEulerAngles = new Vector3(0, 0, ang);
            var img = go.GetComponent<Image>();
            UiSprites.Apply(img, UiSprites.Soft());
            img.color = new Color(0.72f, 0.92f, 1f, 0.38f);
            img.raycastTarget = false;
            go.GetComponent<PulseGlow>().Seed(0.22f, 1.8f, 0.1f);
        }

        void LateUpdate()
        {
            if (_motes == null || _host == null) return;
            var time = Time.unscaledTime;
            var size = _host.rect.size;
            for (int i = 0; i < _motes.Length; i++)
            {
                var m = _motes[i];
                if (m.Rt == null) continue;
                var u = 0.5f + 0.5f * Mathf.Sin(time * m.Speed + m.Phase);
                m.Rt.anchoredPosition = new Vector2(m.Drift.x * (u - 0.5f) * 0.35f, m.Drift.y * u)
                    * new Vector2(size.x / 720f, size.y / 1280f);
                if (m.Spin != 0f)
                    m.Rt.localEulerAngles = new Vector3(0, 0, m.Rt.localEulerAngles.z + m.Spin * Time.unscaledDeltaTime);
                if (m.Img != null)
                {
                    var c = m.Img.color;
                    c.a = m.BaseA * (0.45f + 0.55f * (0.5f + 0.5f * Mathf.Sin(time * (m.Speed * 1.6f) + m.Phase)));
                    m.Img.color = c;
                }
            }
        }
    }
}
