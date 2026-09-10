using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Resonance.App
{
    /// <summary>
    /// 首页右缘栏：设定六边形之下三枚暗金小六边形，无文字，以金线虚丝装订成列。
    /// 其余入口收在第二枚「枢庭」拉出的暗金面板里。
    /// </summary>
    public static class LobbyRail
    {
        const float RailX = 0.938f;
        const float RailTop = 0.948f;
        const float RailStep = 0.09f;

        struct Item
        {
            public string Label;
            public string Screen;
        }

        static readonly Item[] Menu =
        {
            new Item { Label = "日常", Screen = "Daily" },
            new Item { Label = "商店", Screen = "Shop" },
            new Item { Label = "成就", Screen = "Achieve" },
            new Item { Label = "称号", Screen = "Title" },
            new Item { Label = "助战", Screen = "Friend" },
            new Item { Label = "休息", Screen = "Rest" },
            new Item { Label = "用餐", Screen = "Food" },
            new Item { Label = "切磋", Screen = "Pvp" },
            new Item { Label = "入门", Screen = "Tutorial" },
        };

        static Sprite _cycle;
        static Sprite _mail;

        public static void DrawHome(Transform parent, List<GameObject> built, Action onSummon, Action onMore, Action onMail)
        {
            if (parent == null) return;
            var root = OverlayDraw.Group(parent, built, "右缘");
            if (root == null) return;
            // 金线虚丝：贯穿设定与三枚六边形间隙的装订线，先画、压在六边形底下。
            for (int i = 0; i < 3; i++)
                Thread(root, built, new Vector2(RailX, RailTop - RailStep * (i + 0.5f)));
            DarkHex(root, built, new Vector2(RailX, RailTop - RailStep), SparkMark(), "召唤", onSummon);
            DarkHex(root, built, new Vector2(RailX, RailTop - RailStep * 2f), CycleMark(), "枢庭", onMore);
            DarkHex(root, built, new Vector2(RailX, RailTop - RailStep * 3f), MailMark(), "邮件", onMail);
        }

        public static void DrawMenu(Transform parent, List<GameObject> built, Action<string> onOpen, Action onClose)
        {
            if (parent == null) return;
            var root = OverlayDraw.Group(parent, built, "枢庭");
            if (root == null) return;
            UiChrome.ModalDim(root, built);
            var panelGo = UiChrome.Panel(root, built, new Vector2(0.5f, 0.52f), new Vector2(840, 980));
            if (panelGo == null) return;
            var panel = panelGo.transform;

            // 题字左右各一撇金箔。
            OverlayDraw.Label(panel, built, "枢庭", 28, VisualTokens.GoldTitle,
                new Vector2(0.5f, 0.94f), new Vector2(240, 44), false, true, 2.4f, true);
            OverlayDraw.Pic(panel, built, "箔撇", new Vector2(0.365f, 0.94f), new Vector2(96, 18),
                WithA(VisualTokens.GoldMetal, 0.9f), UiSprites.Slash());
            OverlayDraw.Pic(panel, built, "箔撇", new Vector2(0.635f, 0.94f), new Vector2(96, 18),
                WithA(VisualTokens.GoldMetal, 0.9f), UiSprites.Slash());
            OverlayDraw.Label(panel, built, "本机入口", 14, VisualTokens.TextMuted,
                new Vector2(0.5f, 0.90f), new Vector2(320, 28), false, false);
            UiChrome.CloseX(panel, built, new Vector2(0.93f, 0.945f), onClose);
            OverlayDraw.DashLine(panel, built, new Vector2(0.5f, 0.86f), 720f);

            for (int i = 0; i < Menu.Length; i++)
            {
                var col = i % 3;
                var row = i / 3;
                var x = 0.22f + col * 0.28f;
                var y = 0.74f - row * 0.20f;
                Cell(panel, built, Menu[i], new Vector2(x, y), onOpen);
            }

            // 页脚印刷带：虚规、网点与小印，像版权页落款。
            OverlayDraw.DashLine(panel, built, new Vector2(0.5f, 0.188f), 720f);
            var band = OverlayDraw.Pic(panel, built, "网点", new Vector2(0.5f, 0.108f), new Vector2(740, 80),
                WithA(VisualTokens.GoldMetal, 0.10f), UiSprites.Halftone());
            if (band != null)
            {
                band.type = Image.Type.Tiled;
                band.raycastTarget = false;
            }
            OverlayDraw.Pic(panel, built, "印环", new Vector2(0.5f, 0.108f), new Vector2(40, 40),
                WithA(VisualTokens.GoldMetal, 0.85f), UiSprites.HexRing());
            OverlayDraw.Pic(panel, built, "印芯", new Vector2(0.5f, 0.108f), new Vector2(20, 20),
                VisualTokens.GoldTitle, UiSprites.Stamp(4));

            UiChrome.Confirm(root, built, "确认", new Vector2(0.5f, 0.14f), onClose);
        }

        static void Thread(Transform parent, List<GameObject> built, Vector2 anchor)
        {
            var img = OverlayDraw.Pic(parent, built, "虚丝", anchor, new Vector2(96, 5),
                WithA(VisualTokens.GoldWire, 0.8f), UiSprites.Dashed());
            if (img == null) return;
            img.type = Image.Type.Tiled;
            img.preserveAspect = false;
            img.rectTransform.localEulerAngles = new Vector3(0f, 0f, 90f);
        }

        static void DarkHex(Transform parent, List<GameObject> built, Vector2 anchor, Sprite icon, string name, Action click)
        {
            // 墨晕托住六边形，再烫金边金芯。
            OverlayDraw.Pic(parent, built, "墨晕", anchor, new Vector2(116, 116),
                new Color(0f, 0f, 0f, 0.45f), UiSprites.Soft());
            var go = UiChrome.HexButton(parent, built, anchor, icon, click);
            if (go == null) return;
            go.name = name;
            Gild(go, "rim", VisualTokens.GoldMetal, 0.9f);
            Gild(go, "ico", VisualTokens.GoldTitle, 1f);
        }

        static void Cell(Transform parent, List<GameObject> built, Item item, Vector2 anchor, Action<string> onOpen)
        {
            var go = OverlayDraw.Hit(parent, built, anchor, new Vector2(220, 160),
                () => { if (onOpen != null) onOpen(item.Screen); });
            if (go == null) return;
            go.name = item.Label;
            var img = go.GetComponent<Image>();
            if (img != null)
            {
                UiSprites.Apply(img, UiSprites.Round());
                img.color = VisualTokens.PanelFillAlt;
            }
            // 金线内框，收进圆角之内。
            OverlayDraw.Pic(go.transform, built, "线框", new Vector2(0.5f, 0.5f), new Vector2(196, 136),
                WithA(VisualTokens.GoldWire, 0.55f), UiSprites.WireFrame());
            // 上星记、下虚规，中间留字。
            OverlayDraw.Pic(go.transform, built, "星记", new Vector2(0.5f, 0.74f), new Vector2(13, 13),
                VisualTokens.GoldMetal, UiSprites.Star());
            var tx = OverlayDraw.Label(go.transform, built, item.Label, 22, VisualTokens.TextPrimary,
                new Vector2(0.5f, 0.5f), new Vector2(200, 40), false, true, 2f, true);
            if (tx != null) tx.raycastTarget = false;
            var rule = OverlayDraw.Pic(go.transform, built, "虚规", new Vector2(0.5f, 0.28f), new Vector2(56, 6),
                WithA(VisualTokens.GoldMetal, 0.8f), UiSprites.Dashed());
            if (rule != null)
            {
                rule.type = Image.Type.Tiled;
                rule.preserveAspect = false;
            }
        }

        static void Gild(GameObject go, string child, Color color, float a)
        {
            if (go == null) return;
            var t = go.transform.Find(child);
            if (t == null) return;
            var img = t.GetComponent<Image>();
            if (img == null) return;
            img.color = WithA(color, a);
        }

        static Color WithA(Color c, float a)
        {
            return new Color(c.r, c.g, c.b, a);
        }

        static Sprite SparkMark()
        {
            return UiSprites.Spark();
        }

        static Sprite CycleMark()
        {
            if (_cycle != null) return _cycle;
            const int s = 64;
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            var px = new Color32[s * s];
            var c = (s - 1) * 0.5f;
            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    var dx = x - c;
                    var dy = y - c;
                    var r = Mathf.Sqrt(dx * dx + dy * dy);
                    var ang = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
                    if (ang < 0f) ang += 360f;
                    var ring = r >= s * 0.24f && r <= s * 0.36f;
                    var gapA = ang > 28f && ang < 82f;
                    var gapB = ang > 208f && ang < 262f;
                    var a = 0f;
                    if (ring && !gapA && !gapB)
                    {
                        var inner = Mathf.Clamp01((r - s * 0.24f) * 0.55f);
                        var outer = Mathf.Clamp01((s * 0.36f - r) * 0.55f);
                        a = Mathf.Min(inner, outer);
                    }
                    a = Mathf.Max(a, ArrowHead(dx, dy, 55f, s * 0.33f));
                    a = Mathf.Max(a, ArrowHead(dx, dy, 235f, s * 0.33f));
                    px[y * s + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(Mathf.Clamp01(a) * 255f));
                }
            }
            tex.SetPixels32(px);
            tex.Apply(false, false);
            _cycle = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100f);
            return _cycle;
        }

        static float ArrowHead(float dx, float dy, float deg, float radius)
        {
            var rad = deg * Mathf.Deg2Rad;
            var fx = Mathf.Cos(rad);
            var fy = Mathf.Sin(rad);
            var px = dx - fx * radius;
            var py = dy - fy * radius;
            var along = px * fx + py * fy;
            var side = -px * fy + py * fx;
            if (along < -2f || along > 9f) return 0f;
            var w = 6.5f * (1f - along / 9f);
            return Mathf.Clamp01(w + 0.6f - Mathf.Abs(side));
        }

        static Sprite MailMark()
        {
            if (_mail != null) return _mail;
            const int s = 64;
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            var px = new Color32[s * s];
            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    var a = 0f;
                    // 信封身
                    if (x >= 14 && x <= 50 && y >= 20 && y <= 44)
                    {
                        var edge = x <= 16 || x >= 48 || y <= 22 || y >= 42;
                        if (edge) a = 1f;
                    }
                    // 封口折线
                    var d1 = DistToSeg(x, y, 15, 42, 32, 30);
                    var d2 = DistToSeg(x, y, 49, 42, 32, 30);
                    if (d1 < 1.35f || d2 < 1.35f) a = 1f;
                    px[y * s + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
                }
            }
            tex.SetPixels32(px);
            tex.Apply(false, false);
            _mail = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100f);
            return _mail;
        }

        static float DistToSeg(int x, int y, float x0, float y0, float x1, float y1)
        {
            var vx = x1 - x0;
            var vy = y1 - y0;
            var len2 = vx * vx + vy * vy;
            if (len2 < 0.001f) return Vector2.Distance(new Vector2(x, y), new Vector2(x0, y0));
            var t = Mathf.Clamp01(((x - x0) * vx + (y - y0) * vy) / len2);
            var px = x0 + t * vx;
            var py = y0 + t * vy;
            return Vector2.Distance(new Vector2(x, y), new Vector2(px, py));
        }
    }
}
