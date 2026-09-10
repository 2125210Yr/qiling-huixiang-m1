using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Resonance.App
{
    /// <summary>
    /// Tiny ice motes that cross in front of the still. Person does not move.
    /// </summary>
    public static class ForegroundIce
    {
        public static void Emit(Transform world, int layer, List<Material> mats)
        {
            if (world == null) return;
            IceParticles.AddSystem(world, layer, mats, "fgDust", UiSprites.Soft(),
                32f, 150, 3.6f, 6.8f, 0.015f, 0.030f, 0.18f, -0.46f, 0.016f);
            IceParticles.AddSystem(world, layer, mats, "fgGlint", UiSprites.IceSpark(),
                18f, 80, 0.32f, 0.85f, 0.018f, 0.038f, 0.82f, -0.20f, 0.00f,
                0.22f, 0f, 10f, 18f, true);
            IceParticles.AddSystem(world, layer, mats, "fgFlake", UiSprites.IceFlake(),
                3.2f, 16, 3.8f, 6.4f, 0.032f, 0.050f, 0.34f, -0.58f, 0.022f);
            IceParticles.AddSystem(world, layer, mats, "fgCross", UiSprites.IceSpark(),
                5.5f, 22, 2.4f, 4.2f, 0.020f, 0.040f, 0.48f, -0.08f, 0.00f,
                0.92f, 0.6f, 11f, 14f, true);
        }

        public static void Spawn(Transform parent, List<GameObject> built)
        {
            if (parent == null) return;
            var rng = new System.Random(41);
            for (int i = 0; i < 8; i++)
            {
                var spark = i % 3 != 0;
                var px = spark ? 6 + rng.Next(4) : 7 + rng.Next(3);
                var spr = spark ? UiSprites.IceSpark() : UiSprites.Soft();
                var y0 = 0.24f + (float)rng.NextDouble() * 0.50f;
                SpawnOne(parent, built, spr, new Vector2(px, px),
                    Color.Lerp(VisualTokens.IceCore, VisualTokens.IceShard, (float)rng.NextDouble())
                        * new Color(1, 1, 1, 0.16f + (float)rng.NextDouble() * 0.12f),
                    new Vector2(i % 2 == 0 ? -0.08f : 1.08f, y0),
                    new Vector2(i % 2 == 0 ? 1.08f : -0.08f, y0 + ((float)rng.NextDouble() - 0.5f) * 0.10f),
                    0.042f + (float)rng.NextDouble() * 0.022f,
                    rng.NextDouble() < 0.5 ? -36f : 36f,
                    (float)rng.NextDouble());
            }
        }

        static void SpawnOne(Transform parent, List<GameObject> built, Sprite spr, Vector2 size,
            Color color, Vector2 from, Vector2 to, float spd, float spin, float phase)
        {
            var go = new GameObject("fgIce", typeof(RectTransform), typeof(Image), typeof(ForegroundDrift));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = from;
            rt.sizeDelta = size;
            rt.anchoredPosition = Vector2.zero;
            rt.localEulerAngles = new Vector3(0, 0, phase * 360f);
            var img = go.GetComponent<Image>();
            UiSprites.Apply(img, spr);
            img.preserveAspect = true;
            img.color = color;
            img.raycastTarget = false;
            go.GetComponent<ForegroundDrift>().Seed(from, to, spd, spin, color.a, phase);
            if (built != null) built.Add(go);
        }
    }

    public sealed class ForegroundDrift : MonoBehaviour
    {
        Vector2 _from;
        Vector2 _to;
        float _t;
        float _spd;
        float _spin;
        float _baseA;
        float _wobble;
        Image _img;
        RectTransform _rt;

        public void Seed(Vector2 from, Vector2 to, float spd, float spin, float baseA, float phase)
        {
            _from = from;
            _to = to;
            _spd = spd;
            _spin = spin;
            _baseA = baseA;
            _t = phase;
            _wobble = 0.012f + Mathf.Abs(spin) * 0.00025f;
            _img = GetComponent<Image>();
            _rt = (RectTransform)transform;
        }

        void LateUpdate()
        {
            if (_rt == null) return;
            _t += Time.unscaledDeltaTime * _spd;
            var u = Mathf.Repeat(_t, 1f);
            var p = Vector2.Lerp(_from, _to, u);
            p.y += Mathf.Sin(u * 6.2832f) * _wobble;
            _rt.anchorMin = _rt.anchorMax = p;
            _rt.localEulerAngles = new Vector3(0, 0, _rt.localEulerAngles.z + _spin * Time.unscaledDeltaTime);
            if (_img != null)
            {
                var fade = Mathf.SmoothStep(0f, 1f, Mathf.Min(u * 8f, (1f - u) * 8f));
                var c = _img.color;
                c.a = _baseA * fade;
                _img.color = c;
            }
        }
    }
}
