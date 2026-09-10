using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Resonance.App
{
    /// <summary>
    /// Memorial presenter stage: void, perspective checker (#101012/#2A2934), warm backlight,
    /// heavy bottom vignette, orange embers behind the body. Front embers belong to HomeIdleFx.
    /// PolarFloor is restyled off the ice disc. Not wood, not a gold horizon, never mosaic diamond.
    /// </summary>
    public static class LobbyStage
    {
        public static void Draw(Transform parent, List<GameObject> built, bool ice)
        {
            if (parent == null) return;
            CharacterPresenter.PolarFloor(parent, built);
            BindChecker(parent);

            // Warm backlight behind the torso.
            var bloom = OverlayDraw.Pic(parent, built, "暖光", new Vector2(0.50f, 0.50f), new Vector2(960, 1280),
                new Color(0.66f, 0.32f, 0.07f, 0.55f), UiSprites.Soft(), false);
            if (bloom != null) bloom.raycastTarget = false;
            var core = OverlayDraw.Pic(parent, built, "暖核", new Vector2(0.49f, 0.48f), new Vector2(380, 500),
                new Color(1f, 0.56f, 0.12f, 0.32f), UiSprites.Soft(), false);
            if (core != null) core.raycastTarget = false;

            BottomVignette(parent, built);

            // Warm under-glow at the feet, glowing through the vignette dark.
            var pool = OverlayDraw.Pic(parent, built, "足光", new Vector2(0.50f, 0.135f), new Vector2(980, 280),
                WithA(VisualTokens.Ember, 0.30f), UiSprites.Soft(), false);
            if (pool != null) pool.raycastTarget = false;
            var poolCore = OverlayDraw.Pic(parent, built, "足心", new Vector2(0.50f, 0.105f), new Vector2(460, 140),
                new Color(1f, 0.62f, 0.18f, 0.36f), UiSprites.Soft(), false);
            if (poolCore != null) poolCore.raycastTarget = false;

            // Back embers: far dim drift first, then the mid field. Front layer is HomeIdleFx.
            BackEmbers(parent, built, 12);
            CharacterPresenter.Embers(parent, built, 30);
        }

        static void BindChecker(Transform parent)
        {
            var polar = parent.Find("polar");
            if (polar != null)
            {
                var img = polar.GetComponent<Image>();
                if (img != null)
                {
                    img.material = null;
                    UiSprites.Apply(img, UiSprites.FloorChecker());
                    img.color = Color.white;
                    img.raycastTarget = false;
                    img.type = Image.Type.Simple;
                    img.preserveAspect = false;
                }
                // PerspectiveFloor stays attached: it owns the warm checker breath now, not ice.
                if (polar.GetComponent<PerspectiveFloor>() == null)
                    polar.gameObject.AddComponent<PerspectiveFloor>();
            }
            Mute(parent.Find("icePool"));
            Mute(parent.Find("iceShadow"));
        }

        static void BottomVignette(Transform parent, List<GameObject> built)
        {
            // Heavier than the battle vignette: stacked soft bands, near-solid at the bottom edge.
            var wide = OverlayDraw.Pic(parent, built, "底帷", new Vector2(0.50f, 0.0f), new Vector2(1500, 620),
                new Color(0f, 0f, 0f, 0.90f), UiSprites.Soft(), false);
            if (wide != null) wide.raycastTarget = false;
            var deep = OverlayDraw.Pic(parent, built, "底沉", new Vector2(0.50f, 0.0f), new Vector2(1500, 300),
                new Color(0f, 0f, 0f, 0.88f), UiSprites.Soft(), false);
            if (deep != null) deep.raycastTarget = false;
        }

        static void BackEmbers(Transform parent, List<GameObject> built, int count)
        {
            // Far depth: big soft halos, dim, slow. Reuses EmberDrift; UGUI Images only, no new system.
            var rng = new System.Random(47);
            for (int i = 0; i < count; i++)
            {
                var go = new GameObject("远烬", typeof(RectTransform), typeof(EmberDrift));
                go.transform.SetParent(parent, false);
                var rt = go.GetComponent<RectTransform>();
                var x = 0.10f + (float)rng.NextDouble() * 0.80f;
                var y = 0.10f + (float)rng.NextDouble() * 0.55f;
                rt.anchorMin = rt.anchorMax = new Vector2(x, y);
                rt.sizeDelta = Vector2.zero;
                rt.anchoredPosition = Vector2.zero;
                var hot = Color.Lerp(VisualTokens.Ember, VisualTokens.EmberHot, (float)rng.NextDouble() * 0.45f);
                var haloSz = 34f + rng.Next(26);
                var halo = OverlayDraw.Pic(go.transform, null, "halo", new Vector2(0.5f, 0.5f),
                    new Vector2(haloSz, haloSz), new Color(hot.r, hot.g, hot.b, 0.20f), UiSprites.Soft(), false);
                if (halo != null) halo.raycastTarget = false;
                var coreSz = 6f + rng.Next(6);
                var core = OverlayDraw.Pic(go.transform, null, "core", new Vector2(0.5f, 0.5f),
                    new Vector2(coreSz, coreSz), new Color(hot.r, hot.g, hot.b, 0.55f), UiSprites.Circle(), false);
                if (core != null) core.raycastTarget = false;
                go.GetComponent<EmberDrift>().Seed(rng);
                if (built != null) built.Add(go);
            }
        }

        static Color WithA(Color c, float a)
        {
            c.a = a;
            return c;
        }

        static void Mute(Transform t)
        {
            if (t == null) return;
            t.gameObject.SetActive(false);
        }
    }
}
