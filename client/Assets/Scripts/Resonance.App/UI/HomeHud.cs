using System;
using System.Collections.Generic;
using Resonance.Battle;
using UnityEngine;
using UnityEngine.UI;

namespace Resonance.App
{
    /// <summary>
    /// 首页身份铬：腿侧名签、引文、星阶、战力与右上设定六边形。
    /// 印刷暗金：墨晕托底、书脊金规、箔撇点缀、厚黑描边。
    /// 金属铬件与黄数值分色。不画立绘、地板、六页签。
    /// </summary>
    public static class HomeHud
    {
        const float Left = 0.048f;
        const float NameY = 0.402f;
        const float RailX = 0.938f;
        const float RailTop = 0.948f;

        public static void Draw(Transform parent, List<GameObject> built, CharacterDef def, UnitProgress prog,
            Action onSettings, Action onTeam = null, Action onStage = null)
        {
            if (parent == null) return;
            var root = OverlayDraw.Group(parent, built, "首页");
            if (root == null) return;
            if (def != null)
                Nameplate(root, built, def, prog);
            HexSettings(root, built, onSettings);
            SliceEntry(root, built, onTeam, onStage);
        }

        static void SliceEntry(Transform parent, List<GameObject> built, Action onTeam, Action onStage)
        {
            if (onTeam != null)
                Link(parent, built, "编队", new Vector2(0.118f, 0.228f), onTeam);
            if (onStage != null)
                Link(parent, built, "关卡", new Vector2(0.268f, 0.228f), onStage);
        }

        static void Link(Transform parent, List<GameObject> built, string label, Vector2 anchor, Action click)
        {
            var go = OverlayDraw.Hit(parent, built, anchor, new Vector2(148, 52), click);
            if (go == null) return;
            go.name = label;
            var img = go.GetComponent<Image>();
            if (img != null)
            {
                UiSprites.Apply(img, UiSprites.Round());
                img.color = new Color(0.04f, 0.03f, 0.03f, 0.72f);
            }
            var tx = OverlayDraw.Label(parent, built, label, 20, VisualTokens.GoldMetal,
                anchor, new Vector2(140, 40), false, true, 2f);
            if (tx != null) tx.raycastTarget = false;
        }

        static void Nameplate(Transform parent, List<GameObject> built, CharacterDef def, UnitProgress prog)
        {
            // 墨晕：软边暗底贴左缘，只托字、不成卡。
            OverlayDraw.Pic(parent, built, "墨晕", new Vector2(0.12f, 0.348f), new Vector2(480, 400),
                new Color(0f, 0f, 0f, 0.38f), UiSprites.Soft());
            // 书脊金规：极左缘一粗一细双划线，把名签钉在屏边上。
            OverlayDraw.Bar(parent, built, new Vector2(0.014f, 0.348f), new Vector2(5, 380), VisualTokens.GoldMetal);
            OverlayDraw.Bar(parent, built, new Vector2(0.028f, 0.348f), new Vector2(2, 380),
                WithA(VisualTokens.GoldWire, 0.85f));
            // 名上箔撇：金箔软撇，像烫印起笔。
            OverlayDraw.Pic(parent, built, "箔撇", new Vector2(Left + 0.080f, NameY + 0.046f),
                new Vector2(170, 30), WithA(VisualTokens.GoldMetal, 0.92f), UiSprites.Slash());

            UiChrome.NameLabel(parent, built, def.Name, new Vector2(Left, NameY), 52);
            var flavor = CharacterPresenter.Flavor(def.Id);
            var flavorTx = OverlayDraw.Label(parent, built, flavor, 16, VisualTokens.TextSecondary,
                new Vector2(Left, NameY - 0.042f), new Vector2(560, 44), true, true, 2f);
            if (flavorTx != null)
            {
                flavorTx.horizontalOverflow = HorizontalWrapMode.Wrap;
                flavorTx.verticalOverflow = VerticalWrapMode.Truncate;
                flavorTx.lineSpacing = 0.92f;
            }
            // 引文下虚规：印刷对位线，隔开引文与星阶。
            OverlayDraw.DashLine(parent, built, new Vector2(Left + 0.093f, NameY - 0.059f), 200f);
            var starY = NameY - 0.078f;
            StarRow(parent, built, def, prog, new Vector2(Left, starY), 22f);
            var grown = Growth.Apply(def, prog);
            UiChrome.PowerPlate(parent, built, Growth.CombatPower(grown), new Vector2(Left, starY - 0.042f), 48);
        }

        static void StarRow(Transform parent, List<GameObject> built, CharacterDef def, UnitProgress prog, Vector2 anchor, float size)
        {
            var total = OverlayDraw.StarCount(def, prog);
            var step = size + 5f;
            for (int i = 0; i < total; i++)
            {
                // 黑绒星底：星形厚黑衬边。
                var back = OverlayDraw.Pic(parent, built, "星底", anchor, new Vector2(size + 5f, size + 5f),
                    new Color(0f, 0f, 0f, 0.9f), UiSprites.Star());
                if (back != null)
                {
                    back.rectTransform.pivot = new Vector2(0f, 0.5f);
                    back.rectTransform.anchoredPosition = new Vector2(i * step - 2.5f, 0f);
                }
                // 已拥有单位：红焰星。金星只给图录筛选。
                var pip = OverlayDraw.Pic(parent, built, "星", anchor, new Vector2(size, size),
                    VisualTokens.StarEvolved, UiSprites.Star());
                if (pip != null)
                {
                    pip.rectTransform.pivot = new Vector2(0f, 0.5f);
                    pip.rectTransform.anchoredPosition = new Vector2(i * step, 0f);
                }
            }
        }

        static void HexSettings(Transform parent, List<GameObject> built, Action onSettings)
        {
            var at = new Vector2(RailX, RailTop);
            // 墨晕 + 外圈金环：双环烫金的设定六边形。
            OverlayDraw.Pic(parent, built, "墨晕", at, new Vector2(128, 128),
                new Color(0f, 0f, 0f, 0.5f), UiSprites.Soft());
            OverlayDraw.Pic(parent, built, "金环", at, new Vector2(74, 74),
                WithA(VisualTokens.GoldWire, 0.55f), UiSprites.HexRing());
            var go = UiChrome.HexButton(parent, built, at, UiSprites.Gear(), onSettings);
            if (go == null) return;
            go.name = "设定";
            Gild(go, "rim", VisualTokens.GoldMetal, 0.92f);
            Gild(go, "ico", VisualTokens.GoldTitle, 1f);
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
    }

    /// <summary>
    /// 开发层：标明 GL/UNKNOWN。像素条 + 亮绿字，不当正式美术，不宣称 T26。
    /// </summary>
    public static class DevOverlay
    {
        public const string FormulaTag = "GL/UNKNOWN";

        public static void Draw(Transform parent, List<GameObject> built, string screen, string extra)
        {
            if (parent == null) return;
            var root = OverlayDraw.Group(parent, built, "DEV");
            if (root == null) return;
            var ink = new Color(0.04f, 0.07f, 0.03f, 0.78f);
            var lime = new Color(0.55f, 1f, 0.38f, 1f);
            var at = new Vector2(0.20f, 0.992f);
            OverlayDraw.Pic(root, built, "devFill", at, new Vector2(400, 40), ink, UiSprites.Pixel());
            var line = "DEV  " + FormulaTag + "  " + (string.IsNullOrEmpty(screen) ? "?" : screen);
            if (!string.IsNullOrEmpty(extra)) line += "  " + extra;
            line += "  not T26";
            var tx = OverlayDraw.Label(root, built, line, 12, lime,
                at, new Vector2(388, 36), false, false);
            if (tx != null)
            {
                tx.horizontalOverflow = HorizontalWrapMode.Wrap;
                tx.verticalOverflow = VerticalWrapMode.Truncate;
            }
        }
    }

    internal static class OverlayDraw
    {
        public static Transform Group(Transform parent, List<GameObject> built, string name)
        {
            if (parent == null) return null;
            var go = Shell(parent, name);
            var rt = go.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = rt.offsetMax = Vector2.zero;
            }
            Reveal(go);
            Track(built, go);
            return go.transform;
        }

        public static void Track(List<GameObject> built, GameObject go)
        {
            if (built != null && go != null) built.Add(go);
        }

        public static Image Wash(Transform parent, List<GameObject> built, Color color, bool raycast)
        {
            if (parent == null) return null;
            var go = Shell(parent, "暗");
            var rt = go.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = rt.offsetMax = Vector2.zero;
            }
            var img = Paint(go, UiSprites.Pixel(), color, raycast);
            if (raycast) KeepRaycast(img);
            Reveal(go);
            Track(built, go);
            return img;
        }

        public static UnityEngine.UI.Image Pic(Transform parent, List<GameObject> built, string name, Vector2 anchor, Vector2 size,
            Color color, Sprite sprite, bool raycast = false)
        {
            if (parent == null) return null;
            var go = Shell(parent, name);
            Place(go, anchor, size);
            var img = Paint(go, sprite, color, raycast);
            if (raycast) KeepRaycast(img);
            Reveal(go);
            Track(built, go);
            return img;
        }

        public static Image Bar(Transform parent, List<GameObject> built, Vector2 anchor, Vector2 size, Color color, float rot = 0f)
        {
            var img = Pic(parent, built, "条", anchor, size, color, UiSprites.Pixel());
            if (img == null) return null;
            img.rectTransform.localEulerAngles = new Vector3(0f, 0f, rot);
            img.preserveAspect = false;
            img.type = Image.Type.Simple;
            return img;
        }

        public static Text Label(Transform parent, List<GameObject> built, string text, int size, Color color,
            Vector2 anchor, Vector2 dim, bool left, bool stroke, float strokeW = 3f, bool bold = false)
        {
            if (parent == null) return null;
            var go = Shell(parent, "字");
            Place(go, anchor, dim);
            var rt = go.GetComponent<RectTransform>();
            if (left && rt != null) rt.pivot = new Vector2(0f, 0.5f);
            var tx = go.GetComponent<Text>();
            if (tx == null) tx = go.AddComponent<Text>();
            if (tx != null)
            {
                var font = CharacterPresenter.UiFont();
                if (font != null) tx.font = font;
                tx.fontSize = size;
                tx.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
                tx.color = color;
                tx.text = text ?? "";
                tx.alignment = left ? TextAnchor.MiddleLeft : TextAnchor.MiddleCenter;
                tx.horizontalOverflow = HorizontalWrapMode.Overflow;
                tx.verticalOverflow = VerticalWrapMode.Overflow;
                tx.raycastTarget = false;
                if (stroke) Stroke(tx, strokeW);
            }
            Reveal(go);
            Track(built, go);
            return tx;
        }

        public static void Stroke(Graphic g, float dist)
        {
            if (g == null) return;
            var a = g.gameObject.AddComponent<Outline>();
            a.effectColor = Color.black;
            a.effectDistance = new Vector2(dist, -dist);
            if (dist >= 3.5f)
            {
                var b = g.gameObject.AddComponent<Outline>();
                b.effectColor = Color.black;
                b.effectDistance = new Vector2(dist * 0.45f, -dist * 0.45f);
                var sh = g.gameObject.AddComponent<Shadow>();
                sh.effectColor = new Color(0f, 0f, 0f, 0.9f);
                sh.effectDistance = new Vector2(2f, -3f);
            }
        }

        public static GameObject Hit(Transform parent, List<GameObject> built, Vector2 anchor, Vector2 size, Action click)
        {
            if (parent == null) return null;
            var go = Shell(parent, "点");
            Place(go, anchor, size);
            var img = Paint(go, UiSprites.Pixel(), Color.clear, true);
            KeepRaycast(img);
            var btn = go.GetComponent<Button>();
            if (btn == null) btn = go.AddComponent<Button>();
            if (btn != null)
            {
                if (img != null) btn.targetGraphic = img;
                if (click != null) btn.onClick.AddListener(() => click());
            }
            if (go.GetComponent<ButtonPress>() == null)
                go.AddComponent<ButtonPress>();
            Reveal(go);
            Track(built, go);
            return go;
        }

        public static GameObject Pill(Transform parent, List<GameObject> built, string label, Vector2 anchor, Vector2 size, Action click)
        {
            var go = Hit(parent, built, anchor, size, click);
            if (go == null) return null;
            go.name = label;
            var img = go.GetComponent<Image>();
            if (img != null)
            {
                UiSprites.Apply(img, UiSprites.ConfirmPill());
                img.color = Color.white;
            }
            var tx = Label(go.transform, built, label, 24, Color.white,
                new Vector2(0.5f, 0.5f), size, false, true, 2.4f, true);
            if (tx != null) tx.raycastTarget = false;
            return go;
        }

        public static Image DashLine(Transform parent, List<GameObject> built, Vector2 anchor, float width)
        {
            var img = Pic(parent, built, "虚线", anchor, new Vector2(width, 8f),
                VisualTokens.GoldMetal, UiSprites.Dashed());
            if (img == null) return null;
            img.type = Image.Type.Tiled;
            img.preserveAspect = false;
            return img;
        }

        public static int StarCount(CharacterDef def, UnitProgress prog)
        {
            if (def == null) return 0;
            var n = def.NativeStar + (prog != null && prog.Uncap > 0 ? 1 : 0);
            if (n > def.MaxStar) n = def.MaxStar;
            if (n < 0) n = 0;
            return n;
        }

        public static string Stars(CharacterDef def, UnitProgress prog)
        {
            var n = StarCount(def, prog);
            var s = "";
            for (int i = 0; i < n; i++) s += "★";
            return s;
        }

        public static string Comma(int n)
        {
            return n.ToString("#,##0", System.Globalization.CultureInfo.InvariantCulture);
        }

        public static float NameFlavorStars(Transform parent, List<GameObject> built, CharacterDef def, UnitProgress prog,
            Vector2 anchor, bool withRole)
        {
            var name = def != null ? def.Name : "";
            var flavor = def != null ? CharacterPresenter.Flavor(def.Id) : "";
            UiChrome.NameLabel(parent, built, name, anchor, 48);
            var fy = anchor.y - 0.050f;
            var flavorTx = Label(parent, built, flavor, 17, VisualTokens.TextSecondary,
                new Vector2(anchor.x, fy), new Vector2(620, 48), true, true, 2f);
            if (flavorTx != null)
            {
                flavorTx.horizontalOverflow = HorizontalWrapMode.Wrap;
                flavorTx.verticalOverflow = VerticalWrapMode.Overflow;
            }
            var sy = fy - 0.034f;
            var n = StarCount(def, prog);
            UiChrome.StarRow(parent, built, n, new Vector2(anchor.x, sy), 22f);
            if (withRole && def != null)
            {
                var rx = anchor.x + n * 0.026f + 0.018f;
                ElementRole(parent, built, def, new Vector2(rx, sy), 30f);
            }
            return sy;
        }

        public static float ElementRole(Transform parent, List<GameObject> built, CharacterDef def, Vector2 left, float disc)
        {
            var el = def != null ? def.Element : Element.Dark;
            var role = def != null ? def.Role : Role.Supporter;
            var elPos = new Vector2(left.x + 0.016f, left.y);
            var rolePos = new Vector2(left.x + 0.050f, left.y);
            Pic(parent, built, "属", elPos, new Vector2(disc, disc), VisualTokens.Element(el), UiSprites.Circle());
            Label(parent, built, CharacterPresenter.ElementWord(el), 14, VisualTokens.TextPrimary,
                elPos, new Vector2(disc + 4f, disc + 4f), false, true, 2f, true);
            Pic(parent, built, "职圈", rolePos, new Vector2(disc + 4f, disc + 4f), VisualTokens.Element(el), UiSprites.Circle());
            Pic(parent, built, "职", rolePos, new Vector2(disc, disc), VisualTokens.RoleDisc, UiSprites.Circle());
            Label(parent, built, CharacterPresenter.RoleMark(role), 14, VisualTokens.TextPrimary,
                rolePos, new Vector2(disc + 4f, disc + 4f), false, true, 2f, true);
            return 0.080f;
        }

        public static void PowerRow(Transform parent, List<GameObject> built, CharacterDef def, UnitProgress prog, Vector2 anchor)
        {
            if (def == null) return;
            var grown = Growth.Apply(def, prog);
            UiChrome.PowerPlate(parent, built, Growth.CombatPower(grown), anchor, 42);
        }

        public static void StatRow(Transform parent, List<GameObject> built, Vector2 anchor, float width, string label, string value,
            Color valueColor, Action click)
        {
            if (click != null)
                Hit(parent, built, new Vector2(anchor.x + width / 2160f, anchor.y), new Vector2(width, 36), click);
            Label(parent, built, label, 18, VisualTokens.TextStat, anchor, new Vector2(200, 36), true, true, 2f);
            var val = Label(parent, built, value, 20, valueColor,
                new Vector2(anchor.x, anchor.y), new Vector2(width, 36), true, true, 2f);
            if (val != null) val.alignment = TextAnchor.MiddleRight;
        }

        static GameObject Shell(Transform parent, string name)
        {
            var go = new GameObject(string.IsNullOrEmpty(name) ? "ui" : name);
            go.SetActive(false);
            if (parent != null)
                go.transform.SetParent(parent, false);
            if (go.GetComponent<RectTransform>() == null)
                go.AddComponent<RectTransform>();
            return go;
        }

        static void Place(GameObject go, Vector2 anchor, Vector2 size)
        {
            if (go == null) return;
            var rt = go.GetComponent<RectTransform>();
            if (rt == null) return;
            rt.anchorMin = rt.anchorMax = anchor;
            rt.sizeDelta = size;
            rt.anchoredPosition = Vector2.zero;
        }

        static Image Paint(GameObject go, Sprite sprite, Color color, bool raycast)
        {
            if (go == null) return null;
            var img = go.GetComponent<Image>();
            if (img == null) img = go.AddComponent<Image>();
            UiSprites.Apply(img, sprite != null ? sprite : UiSprites.Pixel());
            if (img == null) return null;
            img.color = color;
            img.raycastTarget = raycast;
            return img;
        }

        static void KeepRaycast(Image img)
        {
            if (img == null) return;
            img.raycastTarget = true;
            var cr = img.canvasRenderer;
            if (cr != null) cr.cullTransparentMesh = false;
        }

        static void Reveal(GameObject go)
        {
            if (go != null) go.SetActive(true);
        }
    }
}
