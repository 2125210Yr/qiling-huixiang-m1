using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Resonance.App
{
    /// <summary>Presentation-only home standee. Never changes Catalog or player progression.</summary>
    internal static class EmeraldLobby
    {
        const string PreferenceKey = "Resonance.Lobby.EmeraldBunny.Enabled.v1";

        public static bool TryAttach(Transform parent, List<GameObject> built)
        {
            if (PlayerPrefs.GetInt(PreferenceKey, 1) == 0) return false;
            var standee = EmeraldInochi.InochiStandee.Attach(parent);
            if (standee == null) return false;
            OverlayDraw.Track(built, standee);
            return true;
        }

        /// <summary>
        /// Native Inochi must not be DestroyImmediate'd mid-UI rebuild — that stalls
        /// Home→Characters long enough for smoke to die on the roster phase.
        /// </summary>
        public static void ReleaseFrom(Transform root)
        {
            if (root == null) return;
            var stands = root.GetComponentsInChildren<EmeraldInochi.InochiStandee>(true);
            if (stands == null || stands.Length == 0) return;
            for (int i = 0; i < stands.Length; i++)
            {
                var standee = stands[i];
                if (standee == null) continue;
                var holder = standee.transform.parent != null && standee.transform.parent != root
                    ? standee.transform.parent.gameObject
                    : standee.gameObject;
                holder.transform.SetParent(null, false);
                UnityEngine.Object.Destroy(holder);
            }
        }

        public static void DrawIdentity(Transform parent, List<GameObject> built)
        {
            OverlayDraw.Pic(parent, built, "看板名签底", new Vector2(0.12f, 0.375f),
                new Vector2(480, 230), new Color(0f, 0f, 0f, 0.38f), UiSprites.Soft());
            OverlayDraw.Bar(parent, built, new Vector2(0.014f, 0.375f),
                new Vector2(5, 190), VisualTokens.GoldMetal);
            UiChrome.NameLabel(parent, built, "翡翠兔", new Vector2(0.048f, 0.402f), 52);
            OverlayDraw.Label(parent, built, "大厅看板", 18, VisualTokens.TextSecondary,
                new Vector2(0.048f, 0.360f), new Vector2(320, 40), true, true, 2f);
        }

        public static void DrawToggle(Transform parent, List<GameObject> built, bool bunnyVisible, Action redraw)
        {
            var anchor = new Vector2(0.884f, 0.586f);
            var size = new Vector2(190, 62);
            var button = OverlayDraw.Hit(parent, built, anchor, size, () =>
            {
                // A failed model load leaves the leader visible; selecting again retries the bunny.
                PlayerPrefs.SetInt(PreferenceKey, bunnyVisible ? 0 : 1);
                PlayerPrefs.Save();
                redraw?.Invoke();
            });
            if (button == null) return;
            button.name = "看板切换";
            var image = button.GetComponent<Image>();
            UiSprites.Apply(image, UiSprites.Round());
            image.color = VisualTokens.PanelFillAlt;
            OverlayDraw.Label(button.transform, built, bunnyVisible ? "看板 · 翡翠兔" : "看板 · 队长",
                22, VisualTokens.GoldTitle, new Vector2(0.5f, 0.5f), size, false, true, 2f);
        }
    }
}
