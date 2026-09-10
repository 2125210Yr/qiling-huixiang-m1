using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Resonance.App
{
    /// <summary>
    /// 邮件 overlay：三封自造信（补偿 / 活动 / 系统）。金/虚空铬。不画马赛克、不画六页签。
    /// </summary>
    public static class MailBoard
    {
        const float RuleW = 780f;
        const float CardW = 820f;
        const float CardH = 148f;

        static readonly Mail[] Mails =
        {
            new Mail("补偿", "核尘回补", "城门核跳空了半晌。补金屑 ×200、残核 ×2。"),
            new Mail("活动", "裂口加倍", "三日里封口，金屑翻倍。过点作废。"),
            new Mail("系统", "本机核还在跳", "存档不联网。关游戏前记得核还亮着。")
        };

        public static void Draw(Transform parent, int readMask, Action onClose, Action<int> onRead)
        {
            if (parent == null) return;
            var built = new List<GameObject>(48);
            var root = OverlayDraw.Group(parent, built, "MailBoard");
            UiChrome.ModalDim(root, built);

            var panelGo = UiChrome.Panel(root, built, new Vector2(0.5f, 0.54f), new Vector2(920, 820));
            var panel = panelGo.transform;
            GoldWire(panel, built, new Vector2(892, 792));

            Header(panel, built, onClose);
            OverlayDraw.DashLine(panel, built, new Vector2(0.5f, 0.855f), RuleW);

            for (int i = 0; i < Mails.Length; i++)
                DrawRow(panel, built, Mails[i], 0.70f - i * 0.22f, i, (readMask & (1 << i)) != 0, onRead);

            OverlayDraw.Label(panel, built, "点阅读入核  ·  不联网", 14, VisualTokens.TextMuted,
                new Vector2(0.5f, 0.085f), new Vector2(640, 28), false, false);
        }

        static void Header(Transform panel, List<GameObject> built, Action onClose)
        {
            var halo = OverlayDraw.Pic(panel, built, "halo", new Vector2(0.5f, 0.93f), new Vector2(280, 72),
                new Color(VisualTokens.GoldTitle.r, VisualTokens.GoldTitle.g, VisualTokens.GoldTitle.b, 0.16f),
                UiSprites.Soft());
            halo.raycastTarget = false;
            OverlayDraw.Pic(panel, built, "stamp", new Vector2(0.418f, 0.935f), new Vector2(26, 26),
                VisualTokens.GoldTitle, UiSprites.Spark()).raycastTarget = false;
            OverlayDraw.Label(panel, built, "邮件", 24, VisualTokens.GoldTitle,
                new Vector2(0.448f, 0.935f), new Vector2(160, 44), true, false, 2f, true);
            OverlayDraw.Label(panel, built, "三封未拆  ·  补偿 / 活动 / 系统", 15, VisualTokens.TextMuted,
                new Vector2(0.5f, 0.888f), new Vector2(640, 28), false, false);
            UiChrome.CloseX(panel, built, new Vector2(0.93f, 0.94f), onClose);
        }

        static void DrawRow(Transform panel, List<GameObject> built, Mail mail, float y, int index, bool read, Action<int> onRead)
        {
            var go = new GameObject(mail.Kind, typeof(RectTransform), typeof(Image));
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
            ol.effectColor = VisualTokens.GoldMetal;
            ol.effectDistance = new Vector2(2f, -2f);
            OverlayDraw.Track(built, go);

            GoldWire(go.transform, built, new Vector2(CardW - 28f, CardH - 22f));
            var spine = OverlayDraw.Bar(go.transform, built, new Vector2(0.022f, 0.5f),
                new Vector2(8f, CardH - 36f), VisualTokens.GoldSelect);
            spine.raycastTarget = false;
            OverlayDraw.Pic(go.transform, built, "hex", new Vector2(0.08f, 0.72f), new Vector2(22, 22),
                VisualTokens.GoldMetal, UiSprites.Hex()).raycastTarget = false;
            OverlayDraw.Pic(go.transform, built, "pip", new Vector2(0.08f, 0.28f), new Vector2(10, 10),
                VisualTokens.YellowValue, UiSprites.Circle()).raycastTarget = false;

            OverlayDraw.Label(go.transform, built, mail.Kind, 14, VisualTokens.GoldMetal,
                new Vector2(0.12f, 0.72f), new Vector2(80, 28), true, false, 2f, true);
            OverlayDraw.Label(go.transform, built, mail.Title, 20, VisualTokens.GoldTitle,
                new Vector2(0.24f, 0.72f), new Vector2(360, 36), true, false, 2f, true);
            var body = OverlayDraw.Label(go.transform, built, mail.Body, 15, VisualTokens.TextSecondary,
                new Vector2(0.12f, 0.34f), new Vector2(500, 64), true, false);
            body.horizontalOverflow = HorizontalWrapMode.Wrap;
            body.verticalOverflow = VerticalWrapMode.Truncate;
            body.alignment = TextAnchor.UpperLeft;
            body.lineSpacing = 0.94f;

            var idx = index;
            var pill = OverlayDraw.Pill(go.transform, built, read ? "已读" : "阅读", new Vector2(0.86f, 0.5f),
                new Vector2(156, 52), () =>
                {
                    if (!read && onRead != null) onRead(idx);
                });
            KeepHit(pill != null ? pill.GetComponent<Image>() : null);
        }

        static void KeepHit(Image img)
        {
            if (img == null) return;
            img.raycastTarget = true;
            img.canvasRenderer.cullTransparentMesh = false;
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

        struct Mail
        {
            public readonly string Kind;
            public readonly string Title;
            public readonly string Body;

            public Mail(string kind, string title, string body)
            {
                Kind = kind;
                Title = title;
                Body = body;
            }
        }
    }
}
