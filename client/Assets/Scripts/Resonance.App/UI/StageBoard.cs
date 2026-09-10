using System;
using System.Collections.Generic;
using Resonance.Battle;
using UnityEngine;
using UnityEngine.UI;

namespace Resonance.App
{
    /// <summary>
    /// 关卡选关：普通/困难、关卡格、波次预览、回编队/回首页。
    /// 只负责入口与返回，不结算战斗。不是 T26 还原，不是正式美术定稿。
    /// </summary>
    public static class StageBoard
    {
        public static void Draw(
            Transform parent,
            List<GameObject> built,
            StageDef[] table,
            int selected,
            bool useHard,
            bool hardLocked,
            int cleared,
            Func<int, bool> lockedAt,
            Action<int> onPick,
            Action onNormal,
            Action onHard,
            Action onFight,
            Action onBackHome,
            Action onBackTeam)
        {
            if (parent == null) return;
            var root = OverlayDraw.Group(parent, built, "StageBoard");
            if (root == null) return;
            if (table == null) table = new StageDef[0];
            if (table.Length == 0)
            {
                OverlayDraw.Label(root, built, "无关卡", 24, VisualTokens.TextMuted,
                    new Vector2(0.5f, 0.5f), new Vector2(400, 48), false, false);
                if (onBackHome != null)
                    UiChrome.CloseX(root, built, new Vector2(0.93f, 0.95f), onBackHome);
                if (onBackTeam != null)
                    BackTeam(root, built, onBackTeam);
                return;
            }
            selected = Mathf.Clamp(selected, 0, table.Length - 1);

            OverlayDraw.Label(root, built, "第1章  废都裂口", 30, VisualTokens.GoldTitle,
                new Vector2(0.5f, 0.96f), new Vector2(640, 48), false, true, 2f, true);

            ModeHit(root, built, useHard ? "普通" : "普通·", new Vector2(0.28f, 0.915f),
                useHard ? VisualTokens.GoldMetal : VisualTokens.GoldSelect, onNormal);
            var hardCol = hardLocked
                ? VisualTokens.TextMuted
                : (useHard ? VisualTokens.GoldSelect : VisualTokens.GoldMetal);
            ModeHit(root, built, useHard ? "困难·" : "困难", new Vector2(0.72f, 0.915f),
                hardCol, hardLocked ? null : onHard);

            OverlayDraw.Label(root, built, (useHard ? "困难 " : "普通 ") + cleared + " / 12",
                20, VisualTokens.YellowValue, new Vector2(0.5f, 0.875f), new Vector2(360, 36), false, false);

            for (int i = 0; i < table.Length; i++)
            {
                var idx = i;
                var locked = lockedAt != null && lockedAt(idx);
                var on = i == selected;
                var label = (locked ? "锁\n" : (i + 1).ToString("00") + "\n") + ShortName(table[i]);
                var col = i % 3;
                var row = i / 3;
                var at = new Vector2(0.18f + col * 0.32f, 0.76f - row * 0.125f);
                var go = OverlayDraw.Hit(root, built, at, new Vector2(300, 150), () =>
                {
                    if (locked) return;
                    if (onPick != null) onPick(idx);
                });
                if (go == null) continue;
                go.name = "stage" + idx;
                var img = go.GetComponent<Image>();
                if (img != null)
                {
                    UiSprites.Apply(img, UiSprites.Round());
                    img.color = on ? new Color(0.18f, 0.14f, 0.04f) : VisualTokens.PanelFill;
                }
                var tx = OverlayDraw.Label(go.transform, built, label, 20,
                    locked ? VisualTokens.TextMuted : VisualTokens.TextPrimary,
                    new Vector2(0.5f, 0.5f), new Vector2(280, 140), false, false);
                if (tx != null)
                {
                    tx.raycastTarget = false;
                    tx.horizontalOverflow = HorizontalWrapMode.Wrap;
                    tx.verticalOverflow = VerticalWrapMode.Overflow;
                }
                if (on)
                {
                    var ol = go.AddComponent<Outline>();
                    ol.effectColor = VisualTokens.GoldSelect;
                    ol.effectDistance = new Vector2(2, -2);
                }
                if (locked)
                {
                    var btn = go.GetComponent<Button>();
                    if (btn != null) btn.interactable = false;
                }
            }

            var cur = table[selected];
            WavePreview.Draw(root, cur != null ? cur.Wave0 : null, onFight);

            BackTeam(root, built, onBackTeam);
            if (onBackHome != null)
                UiChrome.CloseX(root, built, new Vector2(0.93f, 0.95f), onBackHome);
        }

        static void BackTeam(Transform parent, List<GameObject> built, Action onBackTeam)
        {
            if (onBackTeam == null) return;
            var go = OverlayDraw.Hit(parent, built, new Vector2(0.10f, 0.95f), new Vector2(140, 48), onBackTeam);
            if (go != null) go.name = "回编队";
            var tx = OverlayDraw.Label(parent, built, "回编队", 18, VisualTokens.GoldMetal,
                new Vector2(0.10f, 0.95f), new Vector2(140, 40), false, true, 2f);
            if (tx != null) tx.raycastTarget = false;
        }

        static void ModeHit(Transform parent, List<GameObject> built, string label, Vector2 anchor, Color color, Action click)
        {
            var go = OverlayDraw.Hit(parent, built, anchor, new Vector2(72, 32), click);
            if (go != null)
            {
                go.name = label;
                if (click == null)
                {
                    var btn = go.GetComponent<Button>();
                    if (btn != null) btn.interactable = false;
                }
            }
            var tx = OverlayDraw.Label(parent, built, label, 20, color,
                anchor, new Vector2(80, 32), false, true, 2f);
            if (tx != null) tx.raycastTarget = false;
        }

        static string ShortName(StageDef st)
        {
            var raw = st != null ? (st.Name ?? "") : "";
            if (raw.EndsWith("困难"))
            {
                var cut = raw.LastIndexOf("困难", StringComparison.Ordinal);
                if (cut > 0) raw = raw.Substring(0, cut).TrimEnd();
            }
            var sp = raw.LastIndexOf(' ');
            return sp >= 0 && sp + 1 < raw.Length ? raw.Substring(sp + 1) : raw;
        }
    }
}
