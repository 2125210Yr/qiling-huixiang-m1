using System.Collections.Generic;
using Resonance.Battle;
using UnityEngine;
using UnityEngine.UI;

namespace Resonance.App
{
    public static class CharacterPresenter
    {
        public static string ElementWord(Element e)
        {
            switch (e)
            {
                case Element.Fire: return "火";
                case Element.Water: return "水";
                case Element.Wood: return "木";
                case Element.Light: return "光";
                default: return "暗";
            }
        }

        public static string Caption(CharacterDef def)
        {
            if (def == null) return "";
            return ElementWord(def.Element) + "·" + def.Name;
        }

        public static string RoleLine(CharacterDef def)
        {
            if (def == null) return "";
            return ElementWord(def.Element) + " · " + RoleLabel(def.Role);
        }

        public static Color ElementColor(Element e) => VisualTokens.Element(e);

        public static string Flavor(string id)
        {
            switch (id)
            {
                case "C001": return "废都里仍未熄的刃光";
                case "C002": return "把炉门扛在肩上的人";
                case "C003": return "潮声里数过每一息伤";
                case "C004": return "把名字写进深水的咒";
                case "C005": return "棘径上的一句路引";
                case "C006": return "箭羽沾着林间的锈";
                case "C007": return "白昼不肯落下的盾";
                case "C008": return "把晨星唱成路灯";
                case "C009": return "夜色里仍亮着的药灯";
                case "C010": return "把影子缝回原处";
                case "C011": return "跟在焰刃后面的火星";
                case "C012": return "镜面里倒映的冷河";
                case "C013": return "把咒印烫进炉渣";
                case "C014": return "灰里仍温的药";
                case "C015": return "潮口裂开的那一刀";
                case "C016": return "把河拦在门外";
                case "C017": return "根须就是城墙";
                case "C018": return "棘上的一口毒";
                case "C019": return "青苔盖住旧伤";
                case "C020": return "白昼不肯收的锋";
                case "C021": return "光也会变成绳";
                case "C022": return "露水比药还先到";
                case "C023": return "夜里只亮刃口";
                case "C024": return "影子站成一堵墙";
                case "C025": return "把路写进耳语";
                default: return "契核仍在回响";
            }
        }

        public static string RoleLabel(Role r)
        {
            switch (r)
            {
                case Role.Attacker: return "攻击";
                case Role.Defender: return "防御";
                case Role.Debuffer: return "干扰";
                case Role.Healer: return "治疗";
                default: return "辅助";
            }
        }

        public static Color SkinTint(Element e, string skin)
        {
            var c = ElementColor(e);
            if (skin == "echo")
                return Color.Lerp(c, new Color(0.55f, 0.72f, 0.85f), 0.35f);
            if (skin == "night")
                return Color.Lerp(c, new Color(0.12f, 0.08f, 0.22f), 0.55f) * 0.75f;
            return c;
        }

        public static string TapLine(string id, int zone)
        {
            if (id == "C001")
            {
                if (zone == 1) return "看清楚再动手。";
                if (zone == 2) return "……认真一点。";
                if (zone == 3) return "手往哪放。";
                if (zone == 5) return "这把刃还热着。";
                return "站稳了。";
            }
            if (zone == 1) return "嗯？";
            if (zone == 2) return "……";
            if (zone == 3) return "别闹。";
            return "干什么。";
        }

        public static GameObject DrawStage(Transform parent, CharacterDef def, string skinId = "")
        {
            return DrawStageBox(parent, def, skinId, 0.06f, 0.12f, 0.94f, 0.96f, 720f);
        }

        public static GameObject DrawStage(Transform parent, CharacterDef def, string skinId, float yMin, float yMax)
        {
            return DrawStageBox(parent, def, skinId, 0.15f, yMin, 0.85f, yMax, 560f);
        }

        static GameObject DrawStageBox(Transform parent, CharacterDef def, string skinId,
            float xMin, float yMin, float xMax, float yMax, float fallbackH)
        {
            var go = new GameObject("StagePresenter", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(xMin, yMin);
            rt.anchorMax = new Vector2(xMax, yMax);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            // FitInParent + 2:3 立绘，头必须在框内

            Texture2D idle;
            Texture2D blink;
            if (def != null && CharacterArt.TryPresenter(def.Id, out idle, out blink) && idle != null)
                AddFittedStandee(go.transform, idle);
            else
                Draw(go.transform, def, new Vector2(0.50f, 0.42f), fallbackH, skinId, false);
            return go;
        }

        public static GameObject Draw(Transform parent, CharacterDef def, Vector2 anchor, float height, string skinId = "", bool showPlate = true)
        {
            var go = new GameObject("Presenter", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            var wide = height * 0.58f;
            rt.sizeDelta = new Vector2(wide, height);
            rt.anchoredPosition = Vector2.zero;

            var el = def != null ? def.Element : Element.Dark;
            var cloth = SkinTint(el, skinId);
            Part(go.transform, "glow", UiSprites.Soft(), new Color(cloth.r, cloth.g, cloth.b, 0.22f),
                new Vector2(0.50f, 0.22f), new Vector2(height * 0.52f, height * 0.20f));

            var bodyH = height;
            var bodyW = height * (height >= 260f ? 0.72f : 0.78f);
            var still = ResolveStill(def, height);
            if (still != null)
                AddStill(go.transform, still, new Vector2(0.50f, height >= 260f ? 0.48f : 0.58f), new Vector2(bodyW, bodyH));
            else
            {
                var body = Part(go.transform, "pixel", PixelStandIn.Get(def, skinId, 0), Color.white,
                    new Vector2(0.50f, 0.48f), new Vector2(height * 0.78f, height));
                body.preserveAspect = true;
                go.AddComponent<PresenterIdle>().Bind(def, skinId, height);
            }

            var discPx = Mathf.Clamp(height * 0.10f, 22f, 52f);
            if (height >= 160f && still == null)
            {
                var disc = Part(go.transform, "disc", UiSprites.Circle(), VisualTokens.RoleDisc, new Vector2(0.14f, 0.70f), new Vector2(discPx, discPx));
                var rim = Part(disc.transform, "rim", UiSprites.Circle(), ElementColor(el), new Vector2(0.5f, 0.5f), new Vector2(discPx + 6f, discPx + 6f));
                rim.transform.SetAsFirstSibling();
                var role = def != null ? def.Role : Role.Supporter;
                Part(disc.transform, "glyph", UiSprites.Stamp(RoleStamp(role)), Color.white, new Vector2(0.5f, 0.5f), new Vector2(discPx * 0.52f, discPx * 0.52f));
            }
            if (showPlate && height >= 180f)
                NamePlate(go.transform, def, height);
            return go;
        }

        static Texture2D ResolveStill(CharacterDef def, float height)
        {
            if (def == null || string.IsNullOrEmpty(def.Id)) return null;
            Texture2D idle;
            Texture2D blink;
            if (height >= 260f && CharacterArt.TryPresenter(def.Id, out idle, out blink) && idle != null)
                return idle;
            if (CharacterArt.TryPortrait(def.Id, out idle, out blink) && idle != null)
                return idle;
            return null;
        }

        static RawImage AddFittedStandee(Transform parent, Texture tex)
        {
            var go = new GameObject("standee", typeof(RectTransform), typeof(RawImage), typeof(AspectRatioFitter));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var img = go.GetComponent<RawImage>();
            img.texture = tex;
            img.color = Color.white;
            img.raycastTarget = false;
            var fit = go.GetComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fit.aspectRatio = tex != null && tex.height > 0 ? tex.width / (float)tex.height : 2f / 3f;
            return img;
        }

        static Live2DIdle AddLive(Transform parent, Texture idle, Texture blink, Vector2 anchor, Vector2 size, string id, bool tappable, Vector2 pivot, Vector2 pos)
        {
            var go = new GameObject("live2d", typeof(RectTransform), typeof(Live2DIdle));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
            var fx = go.GetComponent<Live2DIdle>();
            fx.color = Color.white;
            var phase = id != null ? (id.GetHashCode() & 255) * 0.11f : 0f;
            fx.Bind(idle, blink, phase, id, tappable);
            return fx;
        }

        static RawImage AddStill(Transform parent, Texture tex, Vector2 anchor, Vector2 size)
        {
            if (tex == null) return null;
            var go = new GameObject("still", typeof(RectTransform), typeof(RawImage));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.sizeDelta = size;
            rt.anchoredPosition = Vector2.zero;
            var img = go.GetComponent<RawImage>();
            img.texture = tex;
            img.color = Color.white;
            img.raycastTarget = false;
            return img;
        }

        static void NamePlate(Transform t, CharacterDef def, float h)
        {
            var w = Mathf.Clamp(h * 0.78f, 160f, 420f);
            var namePx = Mathf.Clamp(Mathf.RoundToInt(h * 0.048f), 22, 44);
            var el = def != null ? def.Element : Element.Dark;
            Part(t, "plate", UiSprites.Round(), new Color(0f, 0f, 0f, 0.82f),
                new Vector2(0.50f, 0.00f), new Vector2(w, namePx + 36f));
            Label(t, def != null ? def.Name : "", namePx, Color.white, new Vector2(0.50f, 0.018f),
                new Vector2(w - 8f, namePx + 8f), true);
            Label(t, RoleLine(def), Mathf.Max(16, namePx - 10), ElementColor(el),
                new Vector2(0.50f, -0.028f), new Vector2(w - 8f, 28f), true);
        }

        static int RoleStamp(Role r)
        {
            switch (r)
            {
                case Role.Attacker: return 2;
                case Role.Defender: return 4;
                case Role.Debuffer: return 5;
                case Role.Healer: return 0;
                default: return 3;
            }
        }

        public static GameObject DrawChip(Transform parent, CharacterDef def, Vector2 anchor, Vector2 size, bool selected, bool leader, int level)
        {
            var go = new GameObject("chip", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.sizeDelta = selected ? size + new Vector2(8, 28) : size;
            rt.anchoredPosition = Vector2.zero;
            var img = go.GetComponent<Image>();
            UiSprites.Apply(img, UiSprites.Round());
            img.color = selected ? VisualTokens.GoldSelect : VisualTokens.PanelFill;

            var el = def != null ? def.Element : Element.Dark;
            var rimCol = ElementColor(el);
            var inner = Part(go.transform, "body", UiSprites.Round(), new Color(0.08f, 0.07f, 0.07f, 1f),
                new Vector2(0.5f, 0.58f), new Vector2(size.x - 18, size.y - 52));
            inner.raycastTarget = false;

            var rim = Part(go.transform, "rim", UiSprites.Round(), rimCol,
                new Vector2(0.5f, 0.52f), new Vector2(size.x - 2, size.y - 8));
            rim.transform.SetAsFirstSibling();
            rim.color = rimCol;

            Draw(go.transform, def, new Vector2(0.5f, 0.60f), size.y * 0.82f, "", false);
            Label(go.transform, def != null ? def.Name : "", 20, Color.white, new Vector2(0.5f, 0.11f), new Vector2(size.x - 8, 34), true);
            Label(go.transform, RoleLine(def), 14, rimCol, new Vector2(0.5f, -0.01f), new Vector2(size.x - 8, 24), true);
            Label(go.transform, "LV " + level, 14, VisualTokens.YellowValue, new Vector2(0.28f, 0.22f), new Vector2(78, 24), true);
            if (leader)
                Label(go.transform, "队长", 12, VisualTokens.TextPrimary, new Vector2(0.5f, 1.08f), new Vector2(90, 22), true)
                    .color = VisualTokens.TextPrimary;

            return go;
        }

        public static GameObject DrawTile(Transform parent, CharacterDef def, Vector2 anchor, Vector2 size)
        {
            return DrawTile(parent, def, anchor, size, false, false);
        }

        public static GameObject DrawTile(Transform parent, CharacterDef def, Vector2 anchor, Vector2 size, bool compact, bool marked)
        {
            var go = new GameObject("tile", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.sizeDelta = size;
            rt.anchoredPosition = Vector2.zero;
            var img = go.GetComponent<Image>();
            UiSprites.Apply(img, UiSprites.Round());
            img.color = marked ? new Color(0.18f, 0.14f, 0.06f, 1f) : new Color(0.10f, 0.09f, 0.09f, 1f);
            var el = def != null ? def.Element : Element.Dark;
            var rimColor = ElementColor(el);
            var rim = Part(go.transform, "rim", UiSprites.Round(), rimColor,
                new Vector2(0.5f, 0.52f), new Vector2(size.x + 6f, size.y + 6f));
            rim.transform.SetAsFirstSibling();
            var bodyH = compact ? Mathf.Min(size.y * 0.70f, 118f) : Mathf.Min(size.y * 0.82f, 160f);
            Draw(go.transform, def, new Vector2(0.5f, compact ? 0.58f : 0.60f), bodyH, "", false);
            var namePx = compact ? 15 : 22;
            var rolePx = compact ? 12 : 15;
            Label(go.transform, def != null ? def.Name : "", namePx, Color.white,
                new Vector2(0.5f, compact ? 0.16f : 0.15f),
                new Vector2(size.x - 6, compact ? 26 : 34), true);
            Label(go.transform, RoleLine(def), rolePx, rimColor,
                new Vector2(0.5f, 0.03f),
                new Vector2(size.x - 6, compact ? 22 : 24), true);
            return go;
        }

        public static void CheckerFloor(Transform parent, List<GameObject> built)
        {
            var root = new GameObject("checker", typeof(RectTransform), typeof(Image));
            root.transform.SetParent(parent, false);
            Stretch(root.GetComponent<RectTransform>());
            var img = root.GetComponent<Image>();
            img.sprite = UiSprites.FloorChecker();
            img.color = Color.white;
            img.raycastTarget = false;
            built.Add(root);
        }

        public static void MosaicFloor(Transform parent, List<GameObject> built)
        {
            var root = new GameObject("mosaic", typeof(RectTransform), typeof(Image));
            root.transform.SetParent(parent, false);
            Stretch(root.GetComponent<RectTransform>());
            root.GetComponent<Image>().color = VisualTokens.BgMosaic;
            built.Add(root);
            for (int y = 0; y < 9; y++)
            {
                for (int x = 0; x < 6; x++)
                {
                    var img = Part(root.transform, "d", UiSprites.Round(),
                        ((x + y) & 1) == 0 ? new Color(0.10f, 0.10f, 0.10f, 0.7f) : new Color(0.16f, 0.16f, 0.16f, 0.45f),
                        new Vector2(0.08f + x * 0.17f, 0.12f + y * 0.10f), new Vector2(140, 140));
                    img.rectTransform.localEulerAngles = new Vector3(0, 0, 45);
                }
            }
        }

        public static void Embers(Transform parent, List<GameObject> built, int count = 10)
        {
            var rng = new System.Random(11);
            for (int i = 0; i < count; i++)
            {
                var go = new GameObject("ember", typeof(RectTransform), typeof(Image), typeof(EmberDrift));
                go.transform.SetParent(parent, false);
                var rt = go.GetComponent<RectTransform>();
                var x = 0.18f + (float)rng.NextDouble() * 0.64f;
                var y = 0.22f + (float)rng.NextDouble() * 0.55f;
                rt.anchorMin = rt.anchorMax = new Vector2(x, y);
                rt.sizeDelta = new Vector2(6 + rng.Next(10), 6 + rng.Next(10));
            var img = go.GetComponent<Image>();
            UiSprites.Apply(img, UiSprites.Circle());
            img.color = Color.Lerp(VisualTokens.Ember, VisualTokens.YellowValue, (float)rng.NextDouble()) * new Color(1, 1, 1, 0.7f);
            img.raycastTarget = false;
                go.GetComponent<EmberDrift>().Seed(rng);
                built.Add(go);
            }
        }

        public static void BattleLanes(Transform parent, List<GameObject> built)
        {
            var rear = Part(parent, "laneR", UiSprites.Soft(), new Color(0.18f, 0.16f, 0.14f, 0.55f),
                new Vector2(0.5f, 0.72f), new Vector2(980, 160));
            built.Add(rear.gameObject);
            var front = Part(parent, "laneF", UiSprites.Soft(), new Color(0.22f, 0.18f, 0.14f, 0.62f),
                new Vector2(0.5f, 0.50f), new Vector2(1040, 180));
            built.Add(front.gameObject);
        }

        public static void StageArena(Transform parent, List<GameObject> built, int stageIndex)
        {
            Color sky, ground, accent;
            if (stageIndex >= 11)
            {
                sky = new Color(0.10f, 0.05f, 0.06f, 1f);
                ground = new Color(0.38f, 0.16f, 0.12f, 0.95f);
                accent = new Color(0.55f, 0.16f, 0.12f, 0.35f);
            }
            else if (stageIndex >= 8)
            {
                sky = new Color(0.06f, 0.05f, 0.12f, 1f);
                ground = new Color(0.22f, 0.16f, 0.30f, 0.94f);
                accent = new Color(0.42f, 0.22f, 0.48f, 0.30f);
            }
            else if (stageIndex >= 4)
            {
                sky = new Color(0.04f, 0.08f, 0.05f, 1f);
                ground = new Color(0.14f, 0.26f, 0.12f, 0.94f);
                accent = new Color(0.18f, 0.38f, 0.12f, 0.32f);
            }
            else
            {
                sky = new Color(0.08f, 0.06f, 0.05f, 1f);
                ground = new Color(0.32f, 0.22f, 0.12f, 0.94f);
                accent = new Color(0.45f, 0.22f, 0.08f, 0.32f);
            }

            var wash = Part(parent, "sky", UiSprites.Soft(), sky, new Vector2(0.5f, 0.72f), new Vector2(1400, 900));
            built.Add(wash.gameObject);
            var discR = Part(parent, "discR", UiSprites.Circle(), ground, new Vector2(0.50f, 0.74f), new Vector2(980, 280));
            built.Add(discR.gameObject);
            var discF = Part(parent, "discF", UiSprites.Circle(), Color.Lerp(ground, Color.black, 0.15f),
                new Vector2(0.50f, 0.48f), new Vector2(1080, 320));
            built.Add(discF.gameObject);
            var colL = Part(parent, "colL", UiSprites.Soft(), accent, new Vector2(0.08f, 0.62f), new Vector2(160, 900));
            built.Add(colL.gameObject);
            var colR = Part(parent, "colR", UiSprites.Soft(), accent, new Vector2(0.92f, 0.62f), new Vector2(160, 900));
            built.Add(colR.gameObject);
            var moon = Part(parent, "moon", UiSprites.Soft(),
                Color.Lerp(accent, Color.white, 0.35f), new Vector2(0.82f, 0.88f), new Vector2(220, 220));
            built.Add(moon.gameObject);
            moon.gameObject.AddComponent<PulseGlow>().Seed(0.22f, 1.4f, 0.4f);
            AddTorch(parent, built, new Vector2(0.10f, 0.58f), accent);
            AddTorch(parent, built, new Vector2(0.90f, 0.58f), accent);
            Vignette(parent);
        }

        static void AddTorch(Transform parent, List<GameObject> built, Vector2 anchor, Color accent)
        {
            var stem = Part(parent, "torch", UiSprites.Soft(), new Color(0.08f, 0.05f, 0.04f, 0.9f),
                anchor, new Vector2(18, 140));
            built.Add(stem.gameObject);
            var flame = Part(parent, "flame", UiSprites.Soft(),
                Color.Lerp(VisualTokens.Ember, accent, 0.25f),
                anchor + new Vector2(0f, 0.08f), new Vector2(56, 90));
            built.Add(flame.gameObject);
            flame.gameObject.AddComponent<TorchFlicker>();
        }

        public static Vector2 AllyFieldAnchor(int i)
        {
            return new Vector2(0.12f + i * 0.19f, 0.40f + (i % 2 == 0 ? 0f : 0.025f));
        }

        public static Vector2 EnemyFieldAnchor(int i, int n)
        {
            if (n <= 1) return new Vector2(0.50f, 0.72f);
            var t = i / (float)(n - 1);
            return new Vector2(Mathf.Lerp(0.16f, 0.84f, t), 0.72f + (i % 2 == 0 ? 0.04f : 0f));
        }

        public static Text Label(Transform parent, string text, int size, Color color, Vector2 anchor, Vector2 dim, bool outline)
        {
            var go = new GameObject("t", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.sizeDelta = dim;
            rt.anchoredPosition = Vector2.zero;
            var tx = go.GetComponent<Text>();
            tx.font = UiFont();
            tx.alignment = TextAnchor.MiddleCenter;
            tx.color = color;
            tx.fontSize = size;
            tx.text = text ?? "";
            tx.horizontalOverflow = HorizontalWrapMode.Overflow;
            tx.verticalOverflow = VerticalWrapMode.Overflow;
            tx.raycastTarget = false;
            if (outline)
            {
                var ol = go.AddComponent<Outline>();
                ol.effectColor = Color.black;
                ol.effectDistance = new Vector2(2f, -2f);
            }
            return tx;
        }

        static Font _ui;

        public static Font UiFont()
        {
            if (_ui != null) return _ui;
            _ui = Resources.Load<Font>("Fonts/NotoSansSC");
            if (_ui != null) return _ui;
            _ui = Font.CreateDynamicFontFromOSFont(new[]
            {
                "Noto Sans SC",
                "Microsoft YaHei",
                "微软雅黑",
                "Noto Sans CJK SC",
                "NotoSansCJK-Regular",
                "DroidSansFallback",
                "Source Han Sans SC",
                "SimHei"
            }, 24);
            if (_ui == null)
                _ui = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                      ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            return _ui;
        }

        static Image Body(Transform parent, string name, Sprite sprite, Color color, Vector2 anchor, Vector2 size, float rot = 0f)
        {
            var pad = Mathf.Max(5f, Mathf.Min(size.x, size.y) * 0.10f);
            Part(parent, name + "_ol", sprite, new Color(0.05f, 0.03f, 0.03f, 1f), anchor, size + new Vector2(pad, pad), rot);
            return Part(parent, name, sprite, color, anchor, size, rot);
        }

        static Image Part(Transform parent, string name, Sprite sprite, Color color, Vector2 anchor, Vector2 size, float rot = 0f)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.sizeDelta = size;
            rt.anchoredPosition = Vector2.zero;
            rt.localEulerAngles = new Vector3(0, 0, rot);
            var img = go.GetComponent<Image>();
            if (sprite != null)
                UiSprites.Apply(img, sprite);
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        static void Vignette(Transform parent)
        {
            Part(parent, "vB", UiSprites.Soft(), new Color(0, 0, 0, 0.85f), new Vector2(0.5f, 0.0f), new Vector2(1400, 420));
            Part(parent, "vT", UiSprites.Soft(), new Color(0, 0, 0, 0.65f), new Vector2(0.5f, 1.0f), new Vector2(1400, 360));
            Part(parent, "vL", UiSprites.Soft(), new Color(0, 0, 0, 0.45f), new Vector2(0.0f, 0.5f), new Vector2(280, 1920));
            Part(parent, "vR", UiSprites.Soft(), new Color(0, 0, 0, 0.45f), new Vector2(1.0f, 0.5f), new Vector2(280, 1920));
        }

        static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }
    }

    public sealed class EmberDrift : MonoBehaviour
    {
        Vector2 _from;
        Vector2 _to;
        float _t;
        float _spd;

        public void Seed(System.Random rng)
        {
            var rt = (RectTransform)transform;
            _from = rt.anchoredPosition;
            _to = _from + new Vector2(rng.Next(-18, 18), rng.Next(24, 80));
            _spd = 0.12f + (float)rng.NextDouble() * 0.18f;
            _t = (float)rng.NextDouble();
        }

        void Update()
        {
            _t += Time.deltaTime * _spd;
            var u = Mathf.PingPong(_t, 1f);
            ((RectTransform)transform).anchoredPosition = Vector2.Lerp(_from, _to, u);
        }
    }

    public sealed class PresenterIdle : MonoBehaviour
    {
        Vector2 _base;
        bool _ready;
        Image _pixel;
        Sprite _a;
        Sprite _b;
        float _phase;
        float _amp = 10f;
        float _pop;

        public void Bind(CharacterDef def, string skin, float height)
        {
            _amp = height >= 400f ? 14f : height >= 220f ? 8f : height >= 160f ? 3.5f : 0f;
            _phase = def != null && def.Id != null ? (def.Id.GetHashCode() & 255) * 0.11f : 0f;
            var t = transform.Find("pixel");
            if (t != null) _pixel = t.GetComponent<Image>();
            _a = PixelStandIn.Get(def, skin, 0);
            _b = PixelStandIn.Get(def, skin, 1);
            if (_pixel != null) _pixel.sprite = _a;
        }

        void LateUpdate()
        {
            var rt = (RectTransform)transform;
            if (!_ready)
            {
                _base = rt.anchoredPosition;
                _ready = true;
            }
            var t = Time.unscaledTime + _phase;
            if (_amp > 0f)
            {
                _pop = Mathf.MoveTowards(_pop, 1f, Time.unscaledDeltaTime * 2.8f);
                var enter = Mathf.SmoothStep(0.78f, 1f, _pop);
                rt.anchoredPosition = _base + new Vector2(Mathf.Sin(t * 1.15f) * (_amp * 0.45f), Mathf.Sin(t * 1.7f) * _amp);
                var breathe = 1f + Mathf.Sin(t * 2.3f) * 0.03f;
                rt.localScale = Vector3.one * enter * breathe;
            }
            if (_pixel != null && _a != null && _b != null)
                _pixel.sprite = Mathf.Repeat(t, 0.64f) < 0.32f ? _a : _b;
        }
    }
}
