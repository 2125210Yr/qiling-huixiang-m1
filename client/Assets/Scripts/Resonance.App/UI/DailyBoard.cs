using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Resonance.App
{
    /// <summary>
    /// 日常 overlay：金/虚空铬、自造今日条、领取胶囊。不画马赛克、不画六页签。
    /// </summary>
    public static class DailyBoard
    {
        const float RuleW = 780f;
        const float CardW = 820f;
        const float CardH = 112f;

        static readonly Task[] Tasks =
        {
            new Task("通关一回", "裂口封上一回，核才肯歇。", "金屑 ×80", 1, 1),
            new Task("驱动一次", "把核里那一跳全压出去。", "金屑 ×40", 1, 1),
            new Task("上滑一次", "刃口抬势，不是甩手。", "金屑 ×40", 1, 1),
            new Task("点按三十", "连击不停，核才记得你。", "金屑 ×30", 12, 30),
            new Task("听核一回", "城门底下那枚还在跳。", "残核 ×1", 0, 1)
        };

        public static void Draw(Transform parent, int claimedMask, Action onClose, Action<int> onClaim)
        {
            if (parent == null) return;
            var built = new List<GameObject>(72);
            var root = OverlayDraw.Group(parent, built, "DailyBoard");
            UiChrome.ModalDim(root, built);

            var panelGo = UiChrome.Panel(root, built, new Vector2(0.5f, 0.54f), new Vector2(920, 900));
            var panel = panelGo.transform;
            GoldWire(panel, built, new Vector2(892, 872));

            Header(panel, built, onClose);
            OverlayDraw.DashLine(panel, built, new Vector2(0.5f, 0.855f), RuleW);

            for (int i = 0; i < Tasks.Length; i++)
                DrawRow(panel, built, Tasks[i], 0.755f - i * 0.145f, i, (claimedMask & (1 << i)) != 0, onClaim);
        }

        static void Header(Transform panel, List<GameObject> built, Action onClose)
        {
            var halo = OverlayDraw.Pic(panel, built, "halo", new Vector2(0.5f, 0.93f), new Vector2(280, 72),
                new Color(VisualTokens.GoldTitle.r, VisualTokens.GoldTitle.g, VisualTokens.GoldTitle.b, 0.16f),
                UiSprites.Soft());
            halo.raycastTarget = false;
            OverlayDraw.Pic(panel, built, "stamp", new Vector2(0.418f, 0.935f), new Vector2(26, 26),
                VisualTokens.GoldTitle, UiSprites.Spark()).raycastTarget = false;
            OverlayDraw.Label(panel, built, "日常", 24, VisualTokens.GoldTitle,
                new Vector2(0.448f, 0.935f), new Vector2(160, 44), true, false, 2f, true);
            OverlayDraw.Label(panel, built, "日程", 15, VisualTokens.GoldMetal,
                new Vector2(0.62f, 0.935f), new Vector2(120, 32), true, false);
            OverlayDraw.Label(panel, built, "今日核还在跳  ·  过零点作废", 15, VisualTokens.TextMuted,
                new Vector2(0.5f, 0.888f), new Vector2(640, 28), false, false);
            UiChrome.CloseX(panel, built, new Vector2(0.93f, 0.94f), onClose);
        }

        static void DrawRow(Transform panel, List<GameObject> built, Task task, float y, int index, bool claimed, Action<int> onClaim)
        {
            var ready = !claimed && task.Have >= task.Need && task.Need > 0;
            var go = new GameObject("d" + (index + 1).ToString("00"), typeof(RectTransform), typeof(Image));
            go.transform.SetParent(panel, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, y);
            rt.sizeDelta = new Vector2(CardW, CardH);
            rt.anchoredPosition = Vector2.zero;
            var img = go.GetComponent<Image>();
            UiSprites.Apply(img, UiSprites.Round());
            img.color = VisualTokens.PanelFillAlt;
            img.raycastTarget = false;
            var ol = go.AddComponent<Outline>();
            ol.effectColor = ready ? VisualTokens.GoldMetal : VisualTokens.SlotRim;
            ol.effectDistance = new Vector2(2f, -2f);
            OverlayDraw.Track(built, go);

            GoldWire(go.transform, built, new Vector2(CardW - 28f, CardH - 22f));
            var spine = OverlayDraw.Bar(go.transform, built, new Vector2(0.022f, 0.5f),
                new Vector2(8f, CardH - 36f), ready ? VisualTokens.GoldSelect : VisualTokens.SlotRim);
            spine.raycastTarget = false;
            OverlayDraw.Pic(go.transform, built, "hex", new Vector2(0.08f, 0.68f), new Vector2(22, 22),
                ready ? VisualTokens.GoldMetal : VisualTokens.RailIcon, UiSprites.Hex()).raycastTarget = false;

            OverlayDraw.Label(go.transform, built, (index + 1).ToString("00"), 14, VisualTokens.GoldMetal,
                new Vector2(0.12f, 0.70f), new Vector2(48, 28), true, false, 2f, true);
            OverlayDraw.Label(go.transform, built, task.Title, 20,
                ready ? VisualTokens.GoldTitle : VisualTokens.TextMuted,
                new Vector2(0.18f, 0.70f), new Vector2(360, 36), true, false, 2f, true);
            OverlayDraw.Label(go.transform, built, task.Hint, 15, VisualTokens.TextSecondary,
                new Vector2(0.12f, 0.32f), new Vector2(420, 36), true, false);
            OverlayDraw.Label(go.transform, built, task.Reward, 16,
                ready ? VisualTokens.YellowValue : VisualTokens.TextMuted,
                new Vector2(0.58f, 0.70f), new Vector2(160, 32), false, false, 2f, true);
            OverlayDraw.Label(go.transform, built, task.Have + "/" + task.Need, 14,
                ready ? VisualTokens.YellowValue : VisualTokens.TextMuted,
                new Vector2(0.58f, 0.32f), new Vector2(120, 28), false, false);

            var idx = index;
            if (ready)
            {
                OverlayDraw.Pill(go.transform, built, "领取", new Vector2(0.86f, 0.5f),
                    new Vector2(156, 52), () =>
                    {
                        if (onClaim != null) onClaim(idx);
                    });
            }
            else
            {
                var chip = OverlayDraw.Pic(go.transform, built, "wait", new Vector2(0.86f, 0.5f),
                    new Vector2(156, 48), VisualTokens.RailFill, UiSprites.Pill());
                chip.raycastTarget = false;
                OverlayDraw.Label(go.transform, built, claimed ? "已领" : "未完", 16,
                    claimed ? VisualTokens.GoldMetal : VisualTokens.TextMuted,
                    new Vector2(0.86f, 0.5f), new Vector2(148, 40), false, false, 2f, true);
            }
        }

        static void GoldWire(Transform parent, List<GameObject> built, Vector2 size)
        {
            var img = OverlayDraw.Pic(parent, built, "wire", new Vector2(0.5f, 0.5f), size,
                VisualTokens.PanelFill, UiSprites.Round());
            img.raycastTarget = false;
            var ol = img.gameObject.AddComponent<Outline>();
            ol.effectColor = VisualTokens.GoldMetal;
            ol.effectDistance = new Vector2(1f, -1f);
        }

        struct Task
        {
            public readonly string Title;
            public readonly string Hint;
            public readonly string Reward;
            public readonly int Have;
            public readonly int Need;

            public Task(string title, string hint, string reward, int have, int need)
            {
                Title = title;
                Hint = hint;
                Reward = reward;
                Have = have;
                Need = need;
            }
        }
    }
}
