using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Resonance.App
{
    /// <summary>
    /// 打印风铬。六页签只出现在首页/编队。确认胶囊在面板下方。
    /// 底栏印半调网点与粗金线；选中页签衬双金线框与墨盘印章。
    /// </summary>
    public static class UiChrome
    {
        static readonly string[] DefaultTabs = { "首页", "契灵", "关卡", "图录", "书库", "深途" };

        public static void TabBar(Transform parent, List<GameObject> built, int selectedIndex, string[] labels, Action<int> onTab)
        {
            selectedIndex = Mathf.Clamp(selectedIndex, 0, 5);
            var lab = new string[6];
            for (int i = 0; i < 6; i++)
                lab[i] = labels != null && i < labels.Length && !string.IsNullOrEmpty(labels[i])
                    ? labels[i]
                    : DefaultTabs[i];

            var dock = Shell(parent, "底栏");
            var dockRt = dock.GetComponent<RectTransform>();
            if (dockRt != null)
            {
                dockRt.anchorMin = new Vector2(0f, 0f);
                dockRt.anchorMax = new Vector2(1f, 0.108f);
                dockRt.offsetMin = dockRt.offsetMax = Vector2.zero;
            }
            var dockImg = Paint(dock, UiSprites.Pixel(), new Color(0.010f, 0.009f, 0.013f, 0.22f), false);
            if (dockImg != null) dockImg.raycastTarget = false;
            // 半调网点 wash：印刷纸感，低透明度平铺。
            var wash = Child(dock.transform, "wash", UiSprites.Halftone(),
                new Color(VisualTokens.GoldMetal.r, VisualTokens.GoldMetal.g, VisualTokens.GoldMetal.b, 0.12f),
                new Vector2(0.5f, 0.5f), Vector2.zero);
            if (wash != null)
            {
                Stretch(wash.rectTransform);
                wash.type = Image.Type.Tiled;
                wash.preserveAspect = false;
                wash.raycastTarget = false;
            }
            // 顶部金规：细亮线压粗金线，再压一条虚线。
            var wireHi = Child(dock.transform, "wireHi", UiSprites.Pixel(),
                new Color(VisualTokens.GoldSelect.r, VisualTokens.GoldSelect.g, VisualTokens.GoldSelect.b, 0.50f),
                new Vector2(0.5f, 1f), new Vector2(0f, 1f));
            if (wireHi != null)
            {
                var hrt = wireHi.rectTransform;
                hrt.anchorMin = new Vector2(0f, 1f);
                hrt.anchorMax = new Vector2(1f, 1f);
                hrt.pivot = new Vector2(0.5f, 1f);
                hrt.offsetMin = new Vector2(0f, -1f);
                hrt.offsetMax = new Vector2(0f, 0f);
                wireHi.raycastTarget = false;
            }
            var wire = Child(dock.transform, "wire", UiSprites.Pixel(),
                new Color(VisualTokens.GoldWire.r, VisualTokens.GoldWire.g, VisualTokens.GoldWire.b, 0.90f),
                new Vector2(0.5f, 1f), new Vector2(0f, 4f));
            if (wire != null)
            {
                var wrt = wire.rectTransform;
                wrt.anchorMin = new Vector2(0f, 1f);
                wrt.anchorMax = new Vector2(1f, 1f);
                wrt.pivot = new Vector2(0.5f, 1f);
                wrt.offsetMin = new Vector2(0f, -5f);
                wrt.offsetMax = new Vector2(0f, -1f);
                wire.raycastTarget = false;
            }
            var dash = Child(dock.transform, "dash", UiSprites.Dashed(),
                new Color(VisualTokens.GoldWire.r, VisualTokens.GoldWire.g, VisualTokens.GoldWire.b, 0.45f),
                new Vector2(0.5f, 1f), new Vector2(0f, 6f));
            if (dash != null)
            {
                var drt = dash.rectTransform;
                drt.anchorMin = new Vector2(0f, 1f);
                drt.anchorMax = new Vector2(1f, 1f);
                drt.pivot = new Vector2(0.5f, 1f);
                drt.offsetMin = new Vector2(0f, -13f);
                drt.offsetMax = new Vector2(0f, -7f);
                dash.type = Image.Type.Tiled;
                dash.preserveAspect = false;
                dash.raycastTarget = false;
            }
            Reveal(dock);
            Track(built, dock);

            for (int i = 0; i < 6; i++)
            {
                var idx = i;
                var on = idx == selectedIndex;
                var x = (idx + 0.5f) / 6f;
                var go = Hit(parent, lab[idx], new Vector2(x, 0.054f), new Vector2(140, 96),
                    onTab == null ? (Action)null : () => onTab(idx));
                if (go == null) continue;
                var tabImg = go.GetComponent<Image>();
                if (tabImg != null) tabImg.color = Color.clear;
                KeepRaycast(tabImg);
                Track(built, go);
                if (on)
                {
                    // 双金线印框：外浅内实，印版卡纸感而非 Material 卡片。
                    var plateOut = Child(go.transform, "plateOut", UiSprites.WireFrame(),
                        new Color(VisualTokens.GoldMetal.r, VisualTokens.GoldMetal.g, VisualTokens.GoldMetal.b, 0.30f),
                        new Vector2(0.5f, 0.5f), new Vector2(132, 94));
                    if (plateOut != null)
                    {
                        plateOut.rectTransform.anchoredPosition = new Vector2(0f, 2f);
                        plateOut.raycastTarget = false;
                    }
                    var plate = Child(go.transform, "plate", UiSprites.WireFrame(),
                        new Color(VisualTokens.GoldWire.r, VisualTokens.GoldWire.g, VisualTokens.GoldWire.b, 0.95f),
                        new Vector2(0.5f, 0.5f), new Vector2(124, 86));
                    if (plate != null)
                    {
                        plate.rectTransform.anchoredPosition = new Vector2(0f, 2f);
                        plate.raycastTarget = false;
                    }
                    // 墨盘 + 金刷：印章盖在墨上。
                    var disc = Child(go.transform, "disc", UiSprites.Circle(),
                        new Color(0.012f, 0.011f, 0.014f, 0.85f),
                        new Vector2(0.5f, 0.5f), new Vector2(60, 60));
                    if (disc != null)
                    {
                        disc.rectTransform.anchoredPosition = new Vector2(0f, 14f);
                        disc.raycastTarget = false;
                    }
                    var sweep = Child(go.transform, "sweep", UiSprites.Slash(),
                        new Color(VisualTokens.GoldMetal.r, VisualTokens.GoldMetal.g, VisualTokens.GoldMetal.b, 0.55f),
                        new Vector2(0.5f, 0.5f), new Vector2(92, 26));
                    if (sweep != null)
                    {
                        sweep.rectTransform.anchoredPosition = new Vector2(0f, 14f);
                        sweep.raycastTarget = false;
                    }
                    var dots = Child(go.transform, "mark", UiSprites.NavMark(), VisualTokens.GoldSelect,
                        new Vector2(0.5f, 0.5f), new Vector2(40, 12));
                    if (dots != null)
                    {
                        dots.rectTransform.anchoredPosition = new Vector2(0f, -38f);
                        dots.raycastTarget = false;
                    }
                }
                var mark = on ? 50f : 38f;
                var ico = Child(go.transform, "ico", UiSprites.Stamp(idx),
                    on ? VisualTokens.YellowNavOn : VisualTokens.GoldMetal,
                    new Vector2(0.5f, 0.5f), new Vector2(mark, mark));
                if (ico != null)
                {
                    ico.rectTransform.anchoredPosition = new Vector2(0f, 14f);
                    ico.raycastTarget = false;
                }
                var tx = LabelOn(go.transform, lab[idx], 15,
                    on ? Color.white : VisualTokens.GoldMetal);
                if (tx != null)
                {
                    tx.rectTransform.anchorMin = new Vector2(0f, 0f);
                    tx.rectTransform.anchorMax = new Vector2(1f, 0.42f);
                    tx.rectTransform.offsetMin = tx.rectTransform.offsetMax = Vector2.zero;
                    if (on) tx.fontStyle = FontStyle.Bold;
                    var ol = tx.gameObject.AddComponent<Outline>();
                    ol.effectColor = Color.black;
                    ol.effectDistance = on ? new Vector2(2.0f, -2.0f) : new Vector2(1.4f, -1.4f);
                }
            }
        }

        public static GameObject HexButton(Transform parent, List<GameObject> built, Vector2 anchor, Action onClick)
        {
            return HexButton(parent, built, anchor, (Sprite)null, onClick);
        }

        public static GameObject HexButton(Transform parent, List<GameObject> built, Vector2 anchor, int stamp, Action onClick)
        {
            return HexButton(parent, built, anchor, UiSprites.Stamp(stamp), onClick);
        }

        public static GameObject HexButton(Transform parent, List<GameObject> built, Vector2 anchor, Sprite icon, Action onClick)
        {
            const float px = 60f;
            var go = Hit(parent, "hex", anchor, new Vector2(px, px), onClick);
            var img = go.GetComponent<Image>();
            if (img != null)
            {
                UiSprites.Apply(img, UiSprites.Hex());
                img.color = new Color(VisualTokens.RailFill.r, VisualTokens.RailFill.g, VisualTokens.RailFill.b, 0.92f);
                img.alphaHitTestMinimumThreshold = 0.2f;
            }
            var rimOut = Child(go.transform, "rimOut", UiSprites.HexRing(),
                new Color(VisualTokens.GoldMetal.r, VisualTokens.GoldMetal.g, VisualTokens.GoldMetal.b, 0.30f),
                new Vector2(0.5f, 0.5f), new Vector2(px + 8f, px + 8f));
            if (rimOut != null) rimOut.raycastTarget = false;
            var rim = Child(go.transform, "rim", UiSprites.HexRing(),
                new Color(VisualTokens.GoldWire.r, VisualTokens.GoldWire.g, VisualTokens.GoldWire.b, 0.95f),
                new Vector2(0.5f, 0.5f), new Vector2(px, px));
            if (rim != null) rim.raycastTarget = false;
            if (icon != null)
            {
                var mark = Child(go.transform, "ico", icon, VisualTokens.RailIcon,
                    new Vector2(0.5f, 0.5f), new Vector2(26, 26));
                if (mark != null) mark.raycastTarget = false;
            }
            Track(built, go);
            return go;
        }

        public static GameObject Confirm(Transform parent, List<GameObject> built, string label, Vector2 anchor, Action onClick)
        {
            var cap = string.IsNullOrEmpty(label) ? "确认" : label;
            var go = Hit(parent, cap, anchor, new Vector2(320, 84), onClick);
            var img = go.GetComponent<Image>();
            if (img != null)
            {
                UiSprites.Apply(img, UiSprites.ConfirmPill());
                img.color = Color.white;
            }
            var sh = go.AddComponent<Shadow>();
            sh.effectColor = new Color(0f, 0f, 0f, 0.50f);
            sh.effectDistance = new Vector2(0f, -5f);
            if (img != null)
            {
                // 金线掐边：沿胶囊形描一圈细金。
                var key = img.gameObject.AddComponent<Outline>();
                key.effectColor = new Color(VisualTokens.GoldWire.r, VisualTokens.GoldWire.g, VisualTokens.GoldWire.b, 0.85f);
                key.effectDistance = new Vector2(1.6f, -1.6f);
            }
            var tx = LabelOn(go.transform, cap, 28, Color.white);
            if (tx != null)
            {
                tx.fontStyle = FontStyle.Bold;
                var ol = tx.gameObject.AddComponent<Outline>();
                ol.effectColor = Color.black;
                ol.effectDistance = new Vector2(2.8f, -2.8f);
                var ol2 = tx.gameObject.AddComponent<Outline>();
                ol2.effectColor = Color.black;
                ol2.effectDistance = new Vector2(1.2f, -1.2f);
            }
            Track(built, go);
            return go;
        }

        public static GameObject Cancel(Transform parent, List<GameObject> built, string label, Vector2 anchor, Action onClick)
        {
            var cap = string.IsNullOrEmpty(label) ? "取消" : label;
            var go = Hit(parent, cap, anchor, new Vector2(320, 84), onClick);
            var img = go.GetComponent<Image>();
            if (img != null)
            {
                UiSprites.Apply(img, UiSprites.CancelPill());
                img.color = Color.white;
            }
            var sh = go.AddComponent<Shadow>();
            sh.effectColor = new Color(0f, 0f, 0f, 0.50f);
            sh.effectDistance = new Vector2(0f, -5f);
            if (img != null)
            {
                var key = img.gameObject.AddComponent<Outline>();
                key.effectColor = new Color(VisualTokens.GoldWire.r, VisualTokens.GoldWire.g, VisualTokens.GoldWire.b, 0.85f);
                key.effectDistance = new Vector2(1.6f, -1.6f);
            }
            var tx = LabelOn(go.transform, cap, 30, VisualTokens.TextPrimary);
            if (tx != null)
            {
                tx.fontStyle = FontStyle.Bold;
                var ol = tx.gameObject.AddComponent<Outline>();
                ol.effectColor = Color.black;
                ol.effectDistance = new Vector2(2.6f, -2.6f);
                var ol2 = tx.gameObject.AddComponent<Outline>();
                ol2.effectColor = Color.black;
                ol2.effectDistance = new Vector2(1.2f, -1.2f);
            }
            Track(built, go);
            return go;
        }

        public static GameObject CloseX(Transform parent, List<GameObject> built, Vector2 anchor, Action onClick)
        {
            var go = Hit(parent, "关闭", anchor, new Vector2(80, 80), onClick);
            var xImg = go.GetComponent<Image>();
            if (xImg != null) xImg.color = Color.clear;
            KeepRaycast(xImg);
            // 墨牌 + 金线框：关闭钮也是印出来的。
            var disc = Child(go.transform, "disc", UiSprites.Round(),
                new Color(0.012f, 0.012f, 0.015f, 0.88f),
                new Vector2(0.5f, 0.5f), new Vector2(64, 64));
            if (disc != null) disc.raycastTarget = false;
            var rim = Child(go.transform, "rim", UiSprites.WireFrame(),
                new Color(VisualTokens.GoldWire.r, VisualTokens.GoldWire.g, VisualTokens.GoldWire.b, 0.95f),
                new Vector2(0.5f, 0.5f), new Vector2(64, 64));
            if (rim != null) rim.raycastTarget = false;
            var tx = LabelOn(go.transform, "×", 44, VisualTokens.TextPrimary);
            if (tx != null)
            {
                var ol = tx.gameObject.AddComponent<Outline>();
                ol.effectColor = Color.black;
                ol.effectDistance = new Vector2(2.4f, -2.4f);
            }
            Track(built, go);
            return go;
        }

        public static GameObject ModalDim(Transform parent, List<GameObject> built)
        {
            var go = Shell(parent, "dim");
            Stretch(go.GetComponent<RectTransform>());
            var img = Paint(go, UiSprites.Pixel(), VisualTokens.OverlayDim, true);
            if (img != null) img.raycastTarget = true;
            Reveal(go);
            Track(built, go);
            return go;
        }

        public static GameObject Panel(Transform parent, List<GameObject> built, Vector2 size)
        {
            return Panel(parent, built, new Vector2(0.5f, 0.54f), size);
        }

        public static GameObject Panel(Transform parent, List<GameObject> built, Vector2 anchor, Vector2 size)
        {
            var go = Shell(parent, "panel");
            var rt = go.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = rt.anchorMax = anchor;
                rt.sizeDelta = size;
                rt.anchoredPosition = Vector2.zero;
            }
            var img = Paint(go, UiSprites.Round(),
                new Color(VisualTokens.PanelFill.r, VisualTokens.PanelFill.g, VisualTokens.PanelFill.b, 0.94f), true);
            if (img != null) img.raycastTarget = true;
            var wire = Child(go.transform, "wire", UiSprites.WireFrame(),
                new Color(VisualTokens.GoldWire.r, VisualTokens.GoldWire.g, VisualTokens.GoldWire.b, 0.90f),
                new Vector2(0.5f, 0.5f), size - new Vector2(10f, 10f));
            if (wire != null) wire.raycastTarget = false;
            Reveal(go);
            Track(built, go);
            return go;
        }

        public static Text NameLabel(Transform parent, List<GameObject> built, string text, Vector2 anchor, int size = 50)
        {
            size = Mathf.Clamp(size, 42, 52);
            var tx = MakeText(parent, "name", text ?? "", size, VisualTokens.TextPrimary, anchor, new Vector2(720, 96), TextAnchor.MiddleLeft);
            if (tx == null) return null;
            tx.rectTransform.pivot = new Vector2(0f, 0.5f);
            tx.fontStyle = FontStyle.Bold;
            ThickOutline(tx.gameObject);
            // 名牌左侧金押条：印刷名签的钉脚。
            var tick = Child(tx.transform, "tick", UiSprites.Pixel(),
                new Color(VisualTokens.GoldMetal.r, VisualTokens.GoldMetal.g, VisualTokens.GoldMetal.b, 0.90f),
                new Vector2(0f, 0.5f), new Vector2(6, 54));
            if (tick != null)
            {
                tick.rectTransform.pivot = new Vector2(1f, 0.5f);
                tick.rectTransform.anchoredPosition = new Vector2(-14f, 0f);
                tick.raycastTarget = false;
            }
            Track(built, tx.gameObject);
            return tx;
        }

        public static Text PowerLabel(Transform parent, List<GameObject> built, int power, Vector2 anchor, int size = 44)
        {
            return PowerPlate(parent, built, power, anchor, size);
        }

        public static Text PowerLabel(Transform parent, List<GameObject> built, string text, Vector2 anchor, int size = 44)
        {
            int n;
            if (string.IsNullOrEmpty(text)) n = 0;
            else if (!int.TryParse(StripPower(text), out n))
                n = 0;
            return PowerPlate(parent, built, n, anchor, size);
        }

        public static Text PowerPlate(Transform parent, List<GameObject> built, int power, Vector2 anchor, int size = 44)
        {
            size = Mathf.Clamp(size, 36, 48);
            var tag = MakeText(parent, "powerTag", "战斗力", 18, VisualTokens.GoldMetal,
                anchor, new Vector2(160, 36), TextAnchor.MiddleLeft);
            if (tag != null)
            {
                tag.rectTransform.pivot = new Vector2(0f, 0.5f);
                var tagOl = tag.gameObject.AddComponent<Outline>();
                tagOl.effectColor = Color.black;
                tagOl.effectDistance = new Vector2(2f, -2f);
                // 标签金线框：铅字外的印框。
                var tagRim = Child(tag.transform, "rim", UiSprites.WireFrame(),
                    new Color(VisualTokens.GoldWire.r, VisualTokens.GoldWire.g, VisualTokens.GoldWire.b, 0.85f),
                    new Vector2(0f, 0.5f), new Vector2(78, 38));
                if (tagRim != null)
                {
                    tagRim.rectTransform.pivot = new Vector2(0f, 0.5f);
                    tagRim.rectTransform.anchoredPosition = new Vector2(-8f, 0f);
                    tagRim.raycastTarget = false;
                }
                Track(built, tag.gameObject);
            }

            var tx = MakeText(parent, "power", Comma(power), size, VisualTokens.YellowValue,
                anchor, new Vector2(480, 72), TextAnchor.MiddleLeft);
            if (tx == null) return tag;
            tx.rectTransform.pivot = new Vector2(0f, 0.5f);
            tx.rectTransform.anchoredPosition = new Vector2(96f, 1f);
            tx.rectTransform.localScale = new Vector3(0.78f, 1.08f, 1f);
            tx.fontStyle = FontStyle.Bold;
            var glow = tx.gameObject.AddComponent<Shadow>();
            glow.effectColor = new Color(VisualTokens.YellowValue.r, VisualTokens.YellowValue.g, VisualTokens.YellowValue.b, 0.42f);
            glow.effectDistance = new Vector2(0f, -2.5f);
            var ol = tx.gameObject.AddComponent<Outline>();
            ol.effectColor = Color.black;
            ol.effectDistance = new Vector2(3.2f, -3.2f);
            var ol2 = tx.gameObject.AddComponent<Outline>();
            ol2.effectColor = Color.black;
            ol2.effectDistance = new Vector2(1.6f, -1.6f);
            // 数字下的虚线金规：铅字排版的脚线。
            var rule = Child(parent, "powerRule", UiSprites.Dashed(),
                new Color(VisualTokens.GoldWire.r, VisualTokens.GoldWire.g, VisualTokens.GoldWire.b, 0.60f),
                anchor, new Vector2(300, 5));
            if (rule != null)
            {
                rule.rectTransform.pivot = new Vector2(0f, 0.5f);
                rule.rectTransform.anchoredPosition = new Vector2(96f, -30f);
                rule.type = Image.Type.Tiled;
                rule.preserveAspect = false;
                rule.raycastTarget = false;
                Track(built, rule.gameObject);
            }
            Track(built, tx.gameObject);
            return tx;
        }

        public static void StarRow(Transform parent, List<GameObject> built, int count, Vector2 anchor, float size = 22f)
        {
            count = Mathf.Clamp(count, 0, 6);
            var go = new GameObject("stars", typeof(RectTransform));
            if (parent != null)
                go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = rt.anchorMax = anchor;
                rt.pivot = new Vector2(0f, 0.5f);
                rt.sizeDelta = new Vector2(count * (size + 4f), size);
                rt.anchoredPosition = Vector2.zero;
            }
            Track(built, go);
            for (int i = 0; i < count; i++)
            {
                var back = Child(go.transform, "sol", UiSprites.Star(), new Color(0f, 0f, 0f, 0.88f),
                    new Vector2(0f, 0.5f), new Vector2(size + 4.5f, size + 4.5f));
                if (back != null)
                {
                    back.rectTransform.pivot = new Vector2(0f, 0.5f);
                    back.rectTransform.anchoredPosition = new Vector2(i * (size + 4f) - 2.2f, 0f);
                    back.raycastTarget = false;
                }
                var rim = Child(go.transform, "rim", UiSprites.Star(),
                    new Color(VisualTokens.GoldWire.r, VisualTokens.GoldWire.g, VisualTokens.GoldWire.b, 0.90f),
                    new Vector2(0f, 0.5f), new Vector2(size + 2.6f, size + 2.6f));
                if (rim != null)
                {
                    rim.rectTransform.pivot = new Vector2(0f, 0.5f);
                    rim.rectTransform.anchoredPosition = new Vector2(i * (size + 4f) - 1.3f, 0f);
                    rim.raycastTarget = false;
                }
                var pip = Child(go.transform, "star", UiSprites.Star(), VisualTokens.StarEvolved,
                    new Vector2(0f, 0.5f), new Vector2(size, size));
                if (pip == null) continue;
                pip.rectTransform.pivot = new Vector2(0f, 0.5f);
                pip.rectTransform.anchoredPosition = new Vector2(i * (size + 4f), 0f);
                pip.raycastTarget = false;
            }
            // 星排脚下一条虚线金规。
            if (count > 0)
            {
                var rule = Child(go.transform, "rule", UiSprites.Dashed(),
                    new Color(VisualTokens.GoldWire.r, VisualTokens.GoldWire.g, VisualTokens.GoldWire.b, 0.50f),
                    new Vector2(0f, 0f), new Vector2(count * (size + 4f), 4f));
                if (rule != null)
                {
                    rule.rectTransform.pivot = new Vector2(0f, 1f);
                    rule.rectTransform.anchoredPosition = new Vector2(0f, -3f);
                    rule.type = Image.Type.Tiled;
                    rule.preserveAspect = false;
                    rule.raycastTarget = false;
                }
            }
        }

        public static GameObject MosaicFill(Transform parent, List<GameObject> built)
        {
            var go = Shell(parent, "mosaic");
            Stretch(go.GetComponent<RectTransform>());
            var img = Paint(go, UiSprites.FloorMosaic(), Color.white, false);
            if (img != null) img.raycastTarget = false;
            Reveal(go);
            Track(built, go);
            return go;
        }

        public static GameObject Toggle(Transform parent, List<GameObject> built, Vector2 anchor, bool on, Action onClick)
        {
            var go = Hit(parent, "tog", anchor, new Vector2(176, 48), onClick);
            var img = go.GetComponent<Image>();
            if (img != null)
            {
                UiSprites.Apply(img, UiSprites.Pill());
                img.color = new Color(0.16f, 0.16f, 0.16f, 1f);
            }
            var mask = go.AddComponent<Mask>();
            mask.showMaskGraphic = true;
            var onBg = Child(go.transform, "on", UiSprites.Pixel(),
                on ? VisualTokens.YellowValue : new Color(0.26f, 0.26f, 0.26f, 1f),
                new Vector2(0.25f, 0.5f), new Vector2(88, 48));
            if (onBg != null) StretchHalf(onBg.rectTransform, true);
            var offBg = Child(go.transform, "off", UiSprites.Pixel(),
                on ? new Color(0.26f, 0.26f, 0.26f, 1f) : new Color(0.32f, 0.32f, 0.32f, 1f),
                new Vector2(0.75f, 0.5f), new Vector2(88, 48));
            if (offBg != null) StretchHalf(offBg.rectTransform, false);
            var onTx = MakeText(go.transform, "onT", "开", 18,
                on ? VisualTokens.TextOnYellow : VisualTokens.TextMuted,
                new Vector2(0.25f, 0.5f), new Vector2(88, 44), TextAnchor.MiddleCenter);
            var offTx = MakeText(go.transform, "offT", "关", 18,
                on ? VisualTokens.TextMuted : VisualTokens.TextPrimary,
                new Vector2(0.75f, 0.5f), new Vector2(88, 44), TextAnchor.MiddleCenter);
            if (onTx != null) onTx.fontStyle = FontStyle.Bold;
            if (offTx != null) offTx.fontStyle = FontStyle.Bold;
            Track(built, go);
            return go;
        }

        static GameObject Hit(Transform parent, string name, Vector2 anchor, Vector2 size, Action onClick)
        {
            var go = Shell(parent, string.IsNullOrEmpty(name) ? "btn" : name);
            var rt = go.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = rt.anchorMax = anchor;
                rt.sizeDelta = size;
                rt.anchoredPosition = Vector2.zero;
            }
            var img = Paint(go, UiSprites.Pixel(), VisualTokens.PanelFill, true);
            KeepRaycast(img);
            var btn = go.GetComponent<Button>();
            if (btn == null) btn = go.AddComponent<Button>();
            if (btn != null)
            {
                if (img != null) btn.targetGraphic = img;
                if (onClick != null)
                    btn.onClick.AddListener(() => onClick());
            }
            if (go.GetComponent<ButtonPress>() == null)
                go.AddComponent<ButtonPress>();
            Reveal(go);
            return go;
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

        static Image Paint(GameObject go, Sprite sprite, Color color, bool raycast)
        {
            if (go == null) return null;
            var img = go.GetComponent<Image>();
            if (img == null) img = go.AddComponent<Image>();
            UiSprites.Apply(img, sprite);
            if (img == null) return null;
            img.color = color;
            img.raycastTarget = raycast;
            return img;
        }

        static void Reveal(GameObject go)
        {
            if (go != null) go.SetActive(true);
        }

        static void KeepRaycast(Image img)
        {
            if (img == null) return;
            img.raycastTarget = true;
            var cr = img.canvasRenderer;
            if (cr != null) cr.cullTransparentMesh = false;
        }

        static Image Child(Transform parent, string name, Sprite sprite, Color color, Vector2 anchor, Vector2 size)
        {
            var go = Shell(parent, name);
            var rt = go.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = rt.anchorMax = anchor;
                rt.sizeDelta = size;
                rt.anchoredPosition = Vector2.zero;
            }
            var img = Paint(go, sprite != null ? sprite : UiSprites.Pixel(), color, false);
            if (img != null) img.raycastTarget = false;
            Reveal(go);
            return img;
        }

        static Text LabelOn(Transform parent, string text, int size, Color color)
        {
            var go = new GameObject("t", typeof(RectTransform), typeof(Text));
            if (parent != null)
                go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = new Vector2(8, 6);
                rt.offsetMax = new Vector2(-8, -6);
            }
            var tx = go.GetComponent<Text>();
            BindText(tx, text, size, color, TextAnchor.MiddleCenter);
            return tx;
        }

        static Text MakeText(Transform parent, string name, string text, int size, Color color, Vector2 anchor, Vector2 dim, TextAnchor align)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            if (parent != null)
                go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = rt.anchorMax = anchor;
                rt.sizeDelta = dim;
                rt.anchoredPosition = Vector2.zero;
            }
            var tx = go.GetComponent<Text>();
            BindText(tx, text, size, color, align);
            return tx;
        }

        static void BindText(Text tx, string text, int size, Color color, TextAnchor align)
        {
            if (tx == null) return;
            var font = CharacterPresenter.UiFont();
            if (font != null) tx.font = font;
            tx.alignment = align;
            tx.color = color;
            tx.fontSize = size;
            tx.text = text ?? "";
            tx.horizontalOverflow = HorizontalWrapMode.Overflow;
            tx.verticalOverflow = VerticalWrapMode.Overflow;
            tx.raycastTarget = false;
        }

        static void ThickOutline(GameObject go)
        {
            if (go == null) return;
            var ol = go.AddComponent<Outline>();
            ol.effectColor = Color.black;
            ol.effectDistance = new Vector2(5f, -5f);
            var ol2 = go.AddComponent<Outline>();
            ol2.effectColor = Color.black;
            ol2.effectDistance = new Vector2(2.4f, -2.4f);
            var ol3 = go.AddComponent<Outline>();
            ol3.effectColor = Color.black;
            ol3.effectDistance = new Vector2(1.2f, -1.2f);
            var sh = go.AddComponent<Shadow>();
            sh.effectColor = new Color(0f, 0f, 0f, 0.92f);
            sh.effectDistance = new Vector2(2f, -3f);
        }

        static string Comma(int n)
        {
            return n.ToString("#,##0", System.Globalization.CultureInfo.InvariantCulture);
        }

        static string StripPower(string text)
        {
            var s = text.Replace(",", "").Replace("战斗力", "").Replace("战力", "");
            s = s.Replace("POWER", "").Replace("Power", "").Replace("CP", "");
            return s.Trim();
        }

        static void Stretch(RectTransform rt)
        {
            if (rt == null) return;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.zero;
        }

        static void StretchHalf(RectTransform rt, bool left)
        {
            if (rt == null) return;
            rt.anchorMin = new Vector2(left ? 0f : 0.5f, 0f);
            rt.anchorMax = new Vector2(left ? 0.5f : 1f, 1f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.zero;
        }

        static void Track(List<GameObject> built, GameObject go)
        {
            if (built != null && go != null) built.Add(go);
        }
    }
}
