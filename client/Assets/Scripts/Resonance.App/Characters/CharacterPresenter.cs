using System.Collections.Generic;
using Resonance.Battle;
using UnityEngine;
using UnityEngine.UI;

namespace Resonance.App
{
    public static class CharacterPresenter
    {
        public static ICharacterPresentation BindExisting(Transform root, BattleFighter[] allies, BattleFighter[] enemies)
        {
            return new CharacterPresentationAdapter(root, allies, enemies);
        }

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
                case "C001": return "极夜里未化的刃光";
                case "C002": return "把炉门扛在肩上的人";
                case "C003": return "潮声里数过每一息伤";
                case "C004": return "把名字写进深水的咒";
                case "C005": return "棘径上的一句路引";
                case "C006": return "箭羽沾着林间的锈";
                case "C007": return "白昼不肯落下的盾";
                case "C008": return "把晨星唱成路灯";
                case "C009": return "夜色里仍亮着的药灯";
                case "C010": return "把影子缝回原处";
                case "C011": return "跟在冰刃后面的残火";
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

        public static string RoleMark(Role r)
        {
            switch (r)
            {
                case Role.Attacker: return "攻";
                case Role.Defender: return "防";
                case Role.Debuffer: return "扰";
                case Role.Healer: return "疗";
                default: return "辅";
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
                if (zone == 5) return "这把刃还冻着。";
                return "站稳了。";
            }
            if (zone == 1) return "嗯？";
            if (zone == 2) return "……";
            if (zone == 3) return "别闹。";
            return "干什么。";
        }

        public static GameObject DrawStage(Transform parent, CharacterDef def, string skinId = "")
        {
            return DrawStageBox(parent, def, skinId, 0.00f, 0.00f, 1.00f, 0.88f, 720f);
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
            // Home/inspect: packed puppet, then C001 Cubism plates, then LiveRig.
            if (def != null && PuppetRig.TryAttach(go.transform, def.Id))
            {
                var live = go.GetComponentInChildren<RawImage>(true);
                if (live != null)
                    EnableStandeeZoom(go, live, idleMotion: false);
                // IceAura sits on the standee UV and cannot track a sword that idles inside the RT.
                return go;
            }
            if (def != null && def.Id == "C001")
            {
                if (CubismPlateRig.TryAttach(go.transform, def.Id))
                {
                    var live = go.GetComponentInChildren<RawImage>(true);
                    if (live != null)
                    {
                        EnableStandeeZoom(go, live, idleMotion: false);
                        IceAura.Attach((RectTransform)live.transform);
                    }
                    return go;
                }
                if (LiveRig.TryAttach(go.transform, def.Id))
                {
                    var live = go.GetComponentInChildren<RawImage>(true);
                    if (live != null)
                    {
                        EnableStandeeZoom(go, live, idleMotion: false);
                        IceAura.Attach((RectTransform)live.transform);
                    }
                    return go;
                }
                if (CutoutRig.TryAttach(go.transform, def.Id))
                {
                    var still = go.GetComponentInChildren<RawImage>(true);
                    if (still != null)
                    {
                        EnableStandeeZoom(go, still, idleMotion: false);
                        IceAura.Attach((RectTransform)still.transform);
                    }
                    return go;
                }
                if (CharacterArt.TryPresenter(def.Id, out idle, out blink) && idle != null && idle.width >= 400)
                {
                    var still = AddFittedStandee(go.transform, idle);
                    EnableStandeeZoom(go, still, idleMotion: false);
                    IceAura.Attach((RectTransform)still.transform);
                    return go;
                }
                return go;
            }
            if (def != null && Sprite2DStandee.TryAttach(go.transform, def.Id))
                return go;
            Texture2D body, hairFront, hairBack, sword, layerBlink;
            if (def != null && def.Id != "C001"
                && CharacterArt.TryLayers(def.Id, out body, out layerBlink, out hairFront, out hairBack, out sword)
                && body != null)
            {
                AddLayeredStandee(go.transform, def.Id, body, layerBlink, hairFront, hairBack, sword);
                return go;
            }
            if (def != null && CharacterArt.TryPresenter(def.Id, out idle, out blink) && idle != null)
                AddFittedStandee(go.transform, idle);
            else
                Draw(go.transform, def, new Vector2(0.50f, 0.42f), fallbackH, skinId, false);
            return go;
        }

        // SNES pixel body for roster chips, tiles, and battle. Painted lobby stills are DrawStage only.
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

            var painted = def != null ? CharacterArt.Body(def.Id) : null;
            if (painted == null && def != null) painted = CharacterArt.Face(def.Id);
            if (painted != null)
            {
                var body = Part(go.transform, "paint", painted, Color.white,
                    new Vector2(0.50f, 0.48f), new Vector2(height * 0.78f, height));
                body.preserveAspect = true;
            }
            else
            {
                var elCol = ElementColor(el);
                var sil = Color.Lerp(elCol, new Color(0.06f, 0.06f, 0.06f, 1f), 0.55f);
                Part(go.transform, "sil", UiSprites.Soft(), sil,
                    new Vector2(0.50f, 0.48f), new Vector2(height * 0.42f, height * 0.78f));
            }

            var discPx = Mathf.Clamp(height * 0.10f, 22f, 52f);
            if (height >= 160f)
            {
                var disc = Part(go.transform, "disc", UiSprites.Circle(), VisualTokens.RoleDisc, new Vector2(0.14f, 0.70f), new Vector2(discPx, discPx));
                var rim = Part(disc.transform, "rim", UiSprites.Circle(), ElementColor(el), new Vector2(0.5f, 0.5f), new Vector2(discPx + 6f, discPx + 6f));
                rim.transform.SetAsFirstSibling();
                var role = def != null ? def.Role : Role.Supporter;
                Part(disc.transform, "glyph", RoleGlyph(role), Color.white, new Vector2(0.5f, 0.5f), new Vector2(discPx * 0.52f, discPx * 0.52f));
            }
            if (showPlate && height >= 180f)
                NamePlate(go.transform, def, height);
            return go;
        }

        static void EnableStandeeZoom(GameObject host, RawImage img, bool idleMotion = true)
        {
            if (host == null || img == null) return;
            var hit = host.GetComponent<Image>();
            if (hit == null) hit = host.AddComponent<Image>();
            hit.color = new Color(0f, 0f, 0f, 0f);
            hit.raycastTarget = true;
            img.raycastTarget = false;
            var zoom = host.GetComponent<StandeeZoom>();
            if (zoom == null) zoom = host.AddComponent<StandeeZoom>();
            zoom.Bind((RectTransform)img.transform, idleMotion);
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

        static void AddLayeredStandee(Transform parent, string id, Texture body, Texture blink,
            Texture hairFront, Texture hairBack, Texture sword)
        {
            var go = new GameObject("layered", typeof(RectTransform), typeof(AspectRatioFitter), typeof(LayeredPresenter));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var fit = go.GetComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fit.aspectRatio = body != null && body.height > 0 ? body.width / (float)body.height : 2f / 3f;
            go.GetComponent<LayeredPresenter>().Bind(body, blink, hairFront, hairBack, sword, id);
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

        public static int EvolvedStars(CharacterDef def, int uncap)
        {
            if (def == null) return 0;
            var n = def.NativeStar + (uncap > 0 ? 1 : 0);
            if (n > def.MaxStar) n = def.MaxStar;
            return n < 0 ? 0 : n;
        }

        public static GameObject DrawChip(Transform parent, CharacterDef def, Vector2 anchor, Vector2 size, bool selected, bool leader, int level, int uncap = 0)
        {
            if (size.x < 8f) size.x = 8f;
            if (size.y < 8f) size.y = 8f;
            var box = selected ? size + new Vector2(8f, 42f) : size;
            var go = new GameObject("chip", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.32f);
            rt.sizeDelta = box;
            rt.anchoredPosition = Vector2.zero;
            var img = go.GetComponent<Image>();
            UiSprites.Apply(img, UiSprites.Round());
            img.color = selected ? VisualTokens.GoldSelect : new Color(0.06f, 0.06f, 0.06f, 1f);

            var pad = selected ? 6f : 2f;
            FillPortrait(go.transform, def, box - new Vector2(pad * 2f, pad * 2f), true);

            if (leader)
            {
                Part(go.transform, "dash", DashFrame(), new Color(1f, 1f, 1f, 0.94f),
                    new Vector2(0.5f, 0.5f), box - new Vector2(3f, 3f));
                var plate = Part(go.transform, "lead", UiSprites.Round(), new Color(0.04f, 0.04f, 0.04f, 0.94f),
                    new Vector2(0.5f, 0.97f), new Vector2(Mathf.Min(96f, box.x * 0.82f), 22f));
                Label(plate.transform, "队长", 12, Color.white, new Vector2(0.5f, 0.5f), new Vector2(90f, 20f), false);
            }

            if (selected)
            {
                var honey = Part(go.transform, "honey", Honeycomb(),
                    new Color(VisualTokens.GoldSelect.r, VisualTokens.GoldSelect.g, VisualTokens.GoldSelect.b, 0.78f),
                    new Vector2(0.07f, 0.52f), new Vector2(16f, box.y * 0.90f));
                honey.type = Image.Type.Tiled;
                Part(go.transform, "arrow", Chevron(), VisualTokens.GoldSelect,
                    new Vector2(0.5f, -0.018f), new Vector2(34f, 16f));
            }

            if (box.y >= 180f)
                StarStack(go.transform, EvolvedStars(def, uncap), box);

            var lvPx = Mathf.Clamp(Mathf.RoundToInt(box.y * 0.078f), 12, 24);
            var lv = Label(go.transform, "等级 " + level, lvPx, Color.white,
                new Vector2(0.30f, 0.072f), new Vector2(64f, 28f), true);
            lv.alignment = TextAnchor.MiddleLeft;
            lv.rectTransform.pivot = new Vector2(0f, 0.5f);
            if (uncap > 0)
            {
                var plus = Label(go.transform, "+" + uncap, Mathf.Max(11, lvPx - 6), VisualTokens.YellowValue,
                    new Vector2(0.30f, 0.152f), new Vector2(48f, 22f), true);
                plus.alignment = TextAnchor.MiddleLeft;
                plus.rectTransform.pivot = new Vector2(0f, 0.5f);
            }
            if (box.y >= 100f)
                RolePair(go.transform, def, new Vector2(0.82f, 0.105f), Mathf.Clamp(box.x * 0.18f, 16f, 28f));
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
            var el = def != null ? def.Element : Element.Dark;
            var rimColor = def != null ? ElementColor(el) : new Color(0.28f, 0.28f, 0.28f, 1f);
            img.color = rimColor;

            var well = Part(go.transform, "well", UiSprites.Round(),
                marked ? new Color(0.16f, 0.12f, 0.05f, 1f) : new Color(0.09f, 0.09f, 0.09f, 1f),
                new Vector2(0.5f, 0.5f), new Vector2(Mathf.Max(8f, size.x - 8f), Mathf.Max(8f, size.y - 8f)));
            well.raycastTarget = false;

            if (def == null)
            {
                var mark = Watermark((Mathf.Abs(anchor.GetHashCode()) + Mathf.RoundToInt(size.x)) % 3);
                Part(go.transform, "wm", mark, new Color(0.22f, 0.22f, 0.22f, 0.55f),
                    new Vector2(0.5f, 0.54f), new Vector2(size.x * 0.42f, size.y * 0.42f));
            }
            else
            {
                FillPortrait(go.transform, def,
                    new Vector2(Mathf.Max(8f, size.x - 10f), Mathf.Max(8f, size.y - 10f)), false);
                var barH = compact ? 22f : 28f;
                Part(go.transform, "nbar", UiSprites.Round(), new Color(0f, 0f, 0f, 0.72f),
                    new Vector2(0.5f, 0.08f), new Vector2(size.x - 10f, barH));
                Label(go.transform, def.Name, compact ? 14 : 18, Color.white,
                    new Vector2(0.5f, 0.08f), new Vector2(size.x - 14f, barH), true);
            }
            return go;
        }

        public static void CheckerFloor(Transform parent, List<GameObject> built)
        {
            CheckerFloor(parent, built, Color.white);
        }

        public static void CheckerFloor(Transform parent, List<GameObject> built, Color tint)
        {
            var root = new GameObject("checker", typeof(RectTransform), typeof(Image));
            root.transform.SetParent(parent, false);
            Stretch(root.GetComponent<RectTransform>());
            var img = root.GetComponent<Image>();
            img.sprite = UiSprites.FloorChecker();
            img.color = tint;
            img.raycastTarget = false;
            if (built != null) built.Add(root);
        }

        public static void MosaicFloor(Transform parent, List<GameObject> built)
        {
            MosaicFloor(parent, built, Color.white);
        }

        public static void MosaicFloor(Transform parent, List<GameObject> built, Color tint)
        {
            var root = new GameObject("mosaic", typeof(RectTransform), typeof(Image));
            root.transform.SetParent(parent, false);
            Stretch(root.GetComponent<RectTransform>());
            var img = root.GetComponent<Image>();
            img.sprite = MosaicSprite();
            img.type = Image.Type.Simple;
            img.color = tint;
            img.raycastTarget = false;
            img.preserveAspect = false;
            if (built != null) built.Add(root);
        }

        /// <summary>
        /// World wash only. Chrome stays gold/void; furniture is P2.
        /// </summary>
        public static Color StageTint(int stageIndex, bool hard = false)
        {
            Color c;
            switch (Mathf.Clamp(stageIndex, 0, 11))
            {
                case 0: c = new Color(0.46f, 0.26f, 0.10f); break;
                case 1: c = new Color(0.40f, 0.24f, 0.16f); break;
                case 2: c = new Color(0.30f, 0.26f, 0.22f); break;
                case 3: c = new Color(0.10f, 0.36f, 0.38f); break;
                case 4: c = new Color(0.16f, 0.34f, 0.14f); break;
                case 5: c = new Color(0.40f, 0.24f, 0.08f); break;
                case 6: c = new Color(0.42f, 0.36f, 0.16f); break;
                case 7: c = new Color(0.24f, 0.12f, 0.36f); break;
                case 8: c = new Color(0.48f, 0.16f, 0.06f); break;
                case 9: c = new Color(0.34f, 0.20f, 0.10f); break;
                case 10: c = new Color(0.36f, 0.10f, 0.12f); break;
                default: c = new Color(0.44f, 0.08f, 0.10f); break;
            }
            if (hard) c = Color.Lerp(c, new Color(0.10f, 0.04f, 0.12f), 0.32f);
            return c;
        }

        public static void StageBackdrop(Transform parent, List<GameObject> built, int stageIndex, bool hard = false, bool arena = false)
        {
            var tint = StageTint(stageIndex, hard);
            var floorTint = Color.Lerp(new Color(0.78f, 0.76f, 0.74f), tint, 0.52f);
            // 战斗用圆形擂台 + 马赛克暗场；透视棋盘格只属于主页。
            MosaicFloor(parent, built, floorTint);

            var wash = tint;
            wash.a = arena ? 0.30f : 0.22f;
            var glow = Part(parent, "stageWash", UiSprites.Soft(), wash,
                new Vector2(0.5f, arena ? 0.68f : 0.58f),
                new Vector2(1400, arena ? 980 : 1400));
            if (built != null) built.Add(glow.gameObject);

            if (arena)
            {
                var ground = Color.Lerp(tint, new Color(0.04f, 0.03f, 0.03f), 0.42f);
                ground.a = 0.52f;
                var discR = Part(parent, "discR", UiSprites.Circle(), ground, new Vector2(0.50f, 0.74f), new Vector2(980, 260));
                if (built != null) built.Add(discR.gameObject);
                var discF = Part(parent, "discF", UiSprites.Circle(), Color.Lerp(ground, Color.black, 0.18f),
                    new Vector2(0.50f, 0.48f), new Vector2(1080, 300));
                if (built != null) built.Add(discF.gameObject);
            }
            Vignette(parent);
        }

        static void FillPortrait(Transform parent, CharacterDef def, Vector2 size, bool bust)
        {
            var cut = new GameObject("cut", typeof(RectTransform), typeof(Image), typeof(Mask));
            cut.transform.SetParent(parent, false);
            var rt = cut.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = Vector2.zero;
            var well = cut.GetComponent<Image>();
            UiSprites.Apply(well, UiSprites.Round());
            well.color = new Color(0.07f, 0.07f, 0.07f, 1f);
            well.raycastTarget = false;
            cut.GetComponent<Mask>().showMaskGraphic = true;

            var spr = def != null ? CharacterArt.Face(def.Id) : null;
            if (spr != null)
            {
                var paint = Part(cut.transform, "paint", spr, Color.white,
                    new Vector2(0.5f, bust ? 0.40f : 0.46f),
                    new Vector2(size.x * 1.16f, size.y * (bust ? 1.38f : 1.14f)));
                paint.preserveAspect = true;
                Part(cut.transform, "shade", UiSprites.Soft(), new Color(0f, 0f, 0f, bust ? 0.28f : 0.18f),
                    new Vector2(0.5f, 0.04f), new Vector2(size.x * 1.1f, size.y * 0.22f));
                if (bust)
                {
                    // 头顶网点溶解：竖条肖像顶部化进马赛克，不是硬切矩形。
                    var tone = Part(cut.transform, "tone", Halftone(), new Color(0.05f, 0.05f, 0.05f, 1f),
                        new Vector2(0.5f, 0.88f), new Vector2(size.x * 1.06f, size.y * 0.30f));
                    tone.raycastTarget = false;
                }
                return;
            }

            var el = def != null ? ElementColor(def.Element) : VisualTokens.SlotRim;
            var wash = Color.Lerp(el, new Color(0.05f, 0.05f, 0.05f, 1f), 0.62f);
            wash.a = 1f;
            Part(cut.transform, "sil", UiSprites.Soft(), wash,
                new Vector2(0.5f, 0.48f), new Vector2(size.x * 0.72f, size.y * 0.88f));
        }

        static void StarStack(Transform t, int n, Vector2 box)
        {
            n = Mathf.Clamp(n, 0, 6);
            var pip = Mathf.Clamp(box.x * 0.15f, 10f, 18f);
            var gap = pip + 1.5f;
            for (int i = 0; i < n; i++)
            {
                var y = 0.068f + i * (gap / Mathf.Max(1f, box.y));
                Part(t, "star", StarPip(), VisualTokens.StarEvolved, new Vector2(0.14f, y), new Vector2(pip, pip));
            }
        }

        static void RolePair(Transform t, CharacterDef def, Vector2 anchor, float disc)
        {
            var el = def != null ? def.Element : Element.Dark;
            var role = def != null ? def.Role : Role.Supporter;
            Part(t, "el", UiSprites.Circle(), ElementColor(el),
                new Vector2(anchor.x - 0.09f, anchor.y), new Vector2(disc, disc));
            var dark = Part(t, "role", UiSprites.Circle(), VisualTokens.RoleDisc,
                new Vector2(anchor.x + 0.05f, anchor.y), new Vector2(disc, disc));
            Part(dark.transform, "g", RoleGlyph(role), Color.white,
                new Vector2(0.5f, 0.5f), new Vector2(disc * 0.55f, disc * 0.55f));
        }

        public static void PolarFloor(Transform parent, List<GameObject> built)
        {
            var root = new GameObject("polar", typeof(RectTransform), typeof(Image), typeof(PerspectiveFloor));
            root.transform.SetParent(parent, false);
            Stretch(root.GetComponent<RectTransform>());
            var img = root.GetComponent<Image>();
            img.sprite = UiSprites.Pixel();
            img.type = Image.Type.Simple;
            img.color = Color.white;
            img.raycastTarget = false;
            if (built != null) built.Add(root);
            var pool = Part(parent, "icePool", UiSprites.Soft(),
                new Color(0.18f, 0.38f, 0.48f, 0.16f), new Vector2(0.51f, 0.138f), new Vector2(520, 110));
            pool.raycastTarget = false;
            if (built != null) built.Add(pool.gameObject);
            var shadow = Part(parent, "iceShadow", UiSprites.Circle(),
                new Color(0.01f, 0.02f, 0.04f, 0.32f), new Vector2(0.51f, 0.132f), new Vector2(300, 52));
            shadow.raycastTarget = false;
            if (built != null) built.Add(shadow.gameObject);
        }

        public static void IceMotes(Transform parent, List<GameObject> built, int count = 14)
        {
            var rng = new System.Random(23);
            for (int i = 0; i < count; i++)
            {
                var go = new GameObject("ice", typeof(RectTransform), typeof(Image), typeof(EmberDrift));
                go.transform.SetParent(parent, false);
                var rt = go.GetComponent<RectTransform>();
                var x = 0.10f + (float)rng.NextDouble() * 0.80f;
                var y = 0.18f + (float)rng.NextDouble() * 0.70f;
                rt.anchorMin = rt.anchorMax = new Vector2(x, y);
                var kind = rng.Next(3);
                Sprite spr;
                if (kind == 0) { spr = UiSprites.IceFlake(); rt.sizeDelta = new Vector2(22 + rng.Next(18), 22 + rng.Next(18)); }
                else if (kind == 1) { spr = UiSprites.IceCrystal(); rt.sizeDelta = new Vector2(20 + rng.Next(16), 20 + rng.Next(16)); }
                else { spr = UiSprites.IceSpark(); rt.sizeDelta = new Vector2(16 + rng.Next(14), 16 + rng.Next(14)); }
                rt.localEulerAngles = new Vector3(0, 0, rng.Next(360));
                var img = go.GetComponent<Image>();
                UiSprites.Apply(img, spr);
                var ice = Color.Lerp(VisualTokens.IceCore, VisualTokens.IceShard, (float)rng.NextDouble());
                ice.a = 0.20f + (float)rng.NextDouble() * 0.28f;
                img.color = ice;
                img.raycastTarget = false;
                go.GetComponent<EmberDrift>().Seed(rng);
                if (built != null) built.Add(go);
            }
        }

        public static void Embers(Transform parent, List<GameObject> built, int count = 10)
        {
            var rng = new System.Random(11);
            for (int i = 0; i < count; i++)
            {
                var go = new GameObject("ember", typeof(RectTransform), typeof(EmberDrift));
                go.transform.SetParent(parent, false);
                var rt = go.GetComponent<RectTransform>();
                var x = 0.14f + (float)rng.NextDouble() * 0.72f;
                var y = 0.18f + (float)rng.NextDouble() * 0.62f;
                rt.anchorMin = rt.anchorMax = new Vector2(x, y);
                rt.sizeDelta = Vector2.zero;
                rt.anchoredPosition = Vector2.zero;
                var hot = Color.Lerp(VisualTokens.Ember, VisualTokens.EmberHot, (float)rng.NextDouble());
                var haloSz = 22f + rng.Next(20);
                var coreSz = 7f + rng.Next(8);
                var halo = Part(go.transform, "halo", UiSprites.Soft(),
                    new Color(hot.r, hot.g, hot.b, 0.38f), new Vector2(0.5f, 0.5f), new Vector2(haloSz, haloSz));
                halo.raycastTarget = false;
                var core = Part(go.transform, "core",
                    i % 3 == 0 ? UiSprites.Spark() : UiSprites.Circle(),
                    new Color(hot.r, hot.g, hot.b, 0.82f), new Vector2(0.5f, 0.5f), new Vector2(coreSz, coreSz));
                core.raycastTarget = false;
                go.GetComponent<EmberDrift>().Seed(rng);
                if (built != null) built.Add(go);
            }
        }

        public static void BattleLanes(Transform parent, List<GameObject> built)
        {
            var rear = Part(parent, "laneR", UiSprites.Soft(), new Color(0.18f, 0.16f, 0.14f, 0.55f),
                new Vector2(0.5f, 0.72f), new Vector2(980, 160));
            if (built != null) built.Add(rear.gameObject);
            var front = Part(parent, "laneF", UiSprites.Soft(), new Color(0.22f, 0.18f, 0.14f, 0.62f),
                new Vector2(0.5f, 0.50f), new Vector2(1040, 180));
            if (built != null) built.Add(front.gameObject);
        }

        public static void StageArena(Transform parent, List<GameObject> built, int stageIndex)
        {
            StageArena(parent, built, stageIndex, false);
        }

        public static void StageArena(Transform parent, List<GameObject> built, int stageIndex, bool hard)
        {
            StageBackdrop(parent, built, stageIndex, hard, true);
        }

        public static Vector2 AllyFieldAnchor(int i)
        {
            // NEEDS_REFERENCE: not measured from primary GT. Not T27.
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
        static Sprite _mosaic;
        static Sprite _halftone;
        static Sprite _dash;
        static Sprite _starPip;
        static Sprite _honey;
        static Sprite _chevron;
        static Sprite[] _roleGlyphs;
        static Sprite[] _waterMarks;

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

        static Sprite RoleGlyph(Role r)
        {
            if (_roleGlyphs == null) _roleGlyphs = new Sprite[5];
            var i = (int)r;
            if (i < 0 || i > 4) i = 4;
            if (_roleGlyphs[i] != null) return _roleGlyphs[i];
            _roleGlyphs[i] = BakeRole(i);
            return _roleGlyphs[i];
        }

        static Sprite BakeRole(int kind)
        {
            const int s = 48;
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            var px = new Color32[s * s];
            for (int i = 0; i < px.Length; i++) px[i] = new Color32(255, 255, 255, 0);
            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    float a = 0f;
                    if (kind == 0) a = SwordA(x, y, s);
                    else if (kind == 1) a = ShieldA(x, y, s);
                    else if (kind == 2) a = SkullA(x, y, s);
                    else if (kind == 3) a = CrossA(x, y, s);
                    else a = WingA(x, y, s);
                    if (a > 0.02f)
                        px[y * s + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(Mathf.Clamp01(a) * 255f));
                }
            }
            tex.SetPixels32(px);
            tex.Apply(false, false);
            return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100f);
        }

        static float SwordA(int x, int y, int s)
        {
            var blade = DistSeg(x, y, 14f, 10f, 38f, 38f);
            var guard = DistSeg(x, y, 22f, 16f, 34f, 12f);
            var hilt = DistSeg(x, y, 12f, 8f, 18f, 13f);
            var a = Mathf.Clamp01(2.4f - blade) + Mathf.Clamp01(1.8f - guard) + Mathf.Clamp01(1.7f - hilt);
            var pom = Vector2.Distance(new Vector2(x, y), new Vector2(12.5f, 8.2f));
            a += Mathf.Clamp01(2.2f - pom);
            return Mathf.Clamp01(a);
        }

        static float ShieldA(int x, int y, int s)
        {
            var nx = (x - 23.5f) / 14.5f;
            var ny = (y - 23f) / 16.5f;
            var top = ny - 0.55f;
            var half = ny > 0.15f ? 1f - (ny - 0.15f) * 0.35f : 1f - (0.15f - ny) * 1.15f;
            if (half < 0.12f) half = 0.12f;
            var inside = Mathf.Abs(nx) < half && ny > -1.05f && ny < 0.92f;
            if (!inside) return 0f;
            var edge = Mathf.Min(half - Mathf.Abs(nx), 0.92f - ny, ny + 1.05f);
            var boss = Vector2.Distance(new Vector2(nx, ny), new Vector2(0f, 0.12f));
            var a = edge < 0.16f ? 1f : (boss < 0.18f ? 0.85f : 0.55f);
            return a;
        }

        static float SkullA(int x, int y, int s)
        {
            var head = Oval(x, y, 24f, 28f, 13f, 12.5f);
            var jaw = Oval(x, y, 24f, 16f, 9f, 7f);
            var a = Mathf.Max(head, jaw * 0.95f);
            var e1 = Oval(x, y, 19.2f, 28.5f, 3.2f, 3.8f);
            var e2 = Oval(x, y, 28.8f, 28.5f, 3.2f, 3.8f);
            var nose = DistSeg(x, y, 24f, 22f, 24f, 18.5f);
            if (e1 > 0.4f || e2 > 0.4f) a = 0f;
            if (nose < 1.1f) a = 0f;
            if (y < 18 && y > 12 && ((x + y) % 5 < 2) && Mathf.Abs(x - 24) < 7)
                a = Mathf.Max(a, 0.9f);
            return Mathf.Clamp01(a);
        }

        static float CrossA(int x, int y, int s)
        {
            var v = x >= 19 && x <= 29 && y >= 8 && y <= 40;
            var h = y >= 22 && y <= 30 && x >= 10 && x <= 38;
            if (!v && !h) return 0f;
            return 1f;
        }

        static float WingA(int x, int y, int s)
        {
            var a = 0f;
            a = Mathf.Max(a, Mathf.Clamp01(2.0f - DistSeg(x, y, 10f, 16f, 22f, 36f)));
            a = Mathf.Max(a, Mathf.Clamp01(1.8f - DistSeg(x, y, 14f, 14f, 30f, 32f)));
            a = Mathf.Max(a, Mathf.Clamp01(1.7f - DistSeg(x, y, 18f, 12f, 38f, 26f)));
            a = Mathf.Max(a, Mathf.Clamp01(1.6f - DistSeg(x, y, 12f, 22f, 36f, 20f)));
            var cover = Oval(x, y, 16f, 24f, 5f, 10f);
            a = Mathf.Max(a, cover);
            return Mathf.Clamp01(a);
        }

        static float Oval(int x, int y, float cx, float cy, float rx, float ry)
        {
            var nx = (x - cx) / rx;
            var ny = (y - cy) / ry;
            var d = nx * nx + ny * ny;
            return Mathf.Clamp01(1.12f - d);
        }

        static float DistSeg(float px, float py, float ax, float ay, float bx, float by)
        {
            var vx = bx - ax;
            var vy = by - ay;
            var c2 = vx * vx + vy * vy;
            var t = c2 < 0.0001f ? 0f : Mathf.Clamp01(((px - ax) * vx + (py - ay) * vy) / c2);
            var dx = px - (ax + t * vx);
            var dy = py - (ay + t * vy);
            return Mathf.Sqrt(dx * dx + dy * dy);
        }

        static Sprite Halftone()
        {
            if (_halftone != null) return _halftone;
            const int w = 64;
            const int h = 96;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            var px = new Color32[w * h];
            const float cell = 6f;
            for (int y = 0; y < h; y++)
            {
                var v = y / (float)(h - 1);
                var ox = ((int)(y / cell) & 1) * cell * 0.5f;
                for (int x = 0; x < w; x++)
                {
                    var lx = Mathf.Repeat(x - ox + cell * 0.5f, cell) - cell * 0.5f;
                    var ly = Mathf.Repeat(y + cell * 0.5f, cell) - cell * 0.5f;
                    var rad = 0.35f + v * 2.35f;
                    var d = Mathf.Sqrt(lx * lx + ly * ly);
                    var a = Mathf.Clamp01(rad - d) * (0.18f + v * 0.95f);
                    a = Mathf.Max(a, v * 0.12f);
                    px[y * w + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
                }
            }
            tex.SetPixels32(px);
            tex.Apply(false, false);
            _halftone = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
            return _halftone;
        }

        static Sprite DashFrame()
        {
            if (_dash != null) return _dash;
            const int s = 64;
            const int b = 8;
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            var px = new Color32[s * s];
            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    var edge = x < b || y < b || x >= s - b || y >= s - b;
                    if (!edge) continue;
                    var along = x < b || x >= s - b ? y : x;
                    if ((along % 7) < 4)
                        px[y * s + x] = new Color32(255, 255, 255, 255);
                }
            }
            tex.SetPixels32(px);
            tex.Apply(false, false);
            _dash = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect, new Vector4(b, b, b, b));
            return _dash;
        }

        static Sprite StarPip()
        {
            if (_starPip != null) return _starPip;
            const int s = 32;
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            var px = new Color32[s * s];
            var cx = (s - 1) * 0.5f;
            var cy = (s - 1) * 0.5f;
            var r = s * 0.46f;
            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    var dx = x - cx;
                    var dy = y - cy;
                    var dist = Mathf.Sqrt(dx * dx + dy * dy);
                    if (dist > r + 0.6f) continue;
                    var ang = Mathf.Atan2(dx, dy);
                    if (ang < 0f) ang += Mathf.PI * 2f;
                    var slice = (Mathf.PI * 2f) / 5f;
                    var t = (ang % slice) / slice;
                    var inner = 0.38f;
                    var edge = t < 0.5f
                        ? Mathf.Lerp(1f, inner, t * 2f)
                        : Mathf.Lerp(inner, 1f, (t - 0.5f) * 2f);
                    var a = Mathf.Clamp01(r * edge + 0.45f - dist);
                    px[y * s + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
                }
            }
            tex.SetPixels32(px);
            tex.Apply(false, false);
            _starPip = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100f);
            return _starPip;
        }

        static Sprite Honeycomb()
        {
            if (_honey != null) return _honey;
            const int w = 24;
            const int h = 96;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Repeat;
            var px = new Color32[w * h];
            const float R = 6.2f;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    var row = Mathf.FloorToInt(y / (R * 1.5f));
                    var ox = (row & 1) * R * 0.86f;
                    var cx = Mathf.Repeat(x - ox, R * 1.73f);
                    var cy = Mathf.Repeat(y, R * 1.5f);
                    var dx = Mathf.Abs(cx - R * 0.86f);
                    var dy = Mathf.Abs(cy - R * 0.75f);
                    var hex = Mathf.Max(dx, dy * 0.58f + dx * 0.5f);
                    var a = Mathf.Clamp01(1.15f - Mathf.Abs(hex - R * 0.72f) * 2.4f);
                    px[y * w + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 180f));
                }
            }
            tex.SetPixels32(px);
            tex.Apply(false, false);
            _honey = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
            return _honey;
        }

        static Sprite Chevron()
        {
            if (_chevron != null) return _chevron;
            const int w = 36;
            const int h = 18;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    var t = y / (float)(h - 1);
                    var half = 2f + t * (w * 0.42f);
                    var d = Mathf.Abs(x - (w - 1) * 0.5f);
                    var a = d < half && d > half - 2.6f ? 1f : 0f;
                    px[y * w + x] = new Color32(255, 255, 255, (byte)(a * 255));
                }
            }
            tex.SetPixels32(px);
            tex.Apply(false, false);
            _chevron = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
            return _chevron;
        }

        static Sprite Watermark(int kind)
        {
            if (_waterMarks == null) _waterMarks = new Sprite[3];
            kind = ((kind % 3) + 3) % 3;
            if (_waterMarks[kind] != null) return _waterMarks[kind];
            const int s = 48;
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            var px = new Color32[s * s];
            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    var nx = (x - 23.5f) / 18f;
                    var ny = (y - 23.5f) / 18f;
                    float a;
                    if (kind == 0) a = HeartA(nx, ny);
                    else if (kind == 1) a = SkullA(x, y, s);
                    else a = FlameA(nx, ny);
                    px[y * s + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(Mathf.Clamp01(a) * 255f));
                }
            }
            tex.SetPixels32(px);
            tex.Apply(false, false);
            _waterMarks[kind] = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100f);
            return _waterMarks[kind];
        }

        static float HeartA(float nx, float ny)
        {
            var c1 = Vector2.Distance(new Vector2(nx, ny), new Vector2(-0.32f, 0.22f));
            var c2 = Vector2.Distance(new Vector2(nx, ny), new Vector2(0.32f, 0.22f));
            var tri = ny < 0.22f && Mathf.Abs(nx) < 0.62f + ny * 0.55f && ny > -0.85f;
            var a = 0f;
            if (c1 < 0.40f || c2 < 0.40f || tri) a = 1f;
            return a;
        }

        static float FlameA(float nx, float ny)
        {
            var body = 1f - Mathf.Abs(nx) * 1.7f - (ny + 0.15f) * (ny + 0.15f) * 0.85f;
            var tip = 1f - Mathf.Abs(nx) * 3.2f - (ny - 0.45f) * (ny - 0.45f) * 2.4f;
            return Mathf.Clamp01(Mathf.Max(body, tip));
        }

        static Sprite MosaicSprite()
        {
            if (_mosaic != null) return _mosaic;
            const int w = 270;
            const int h = 480;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            var px = new Color32[w * h];
            const float cell = 34f;
            var dark = new Color(0.070f, 0.070f, 0.070f, 1f);
            var lit = new Color(0.118f, 0.118f, 0.118f, 1f);
            var bg = VisualTokens.BgMosaic;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    var u = (x + y) / cell;
                    var v = (x - y) / cell;
                    var iu = Mathf.FloorToInt(u);
                    var iv = Mathf.FloorToInt(v);
                    var fu = u - iu;
                    var fv = v - iv;
                    var aCell = ((iu + iv) & 1) == 0;
                    var col = aCell ? dark : lit;
                    col = Color.Lerp(bg, col, 0.92f);
                    var edge = Mathf.Min(fu, fv, 1f - fu, 1f - fv);
                    var bevel = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(edge * 14f));
                    col = Color.Lerp(col * 0.72f, col, bevel);
                    var hi = Mathf.Clamp01(0.10f - Mathf.Abs(fu - 0.18f) - Mathf.Abs(fv - 0.18f));
                    col.r = Mathf.Min(1f, col.r + hi * 0.07f);
                    col.g = Mathf.Min(1f, col.g + hi * 0.07f);
                    col.b = Mathf.Min(1f, col.b + hi * 0.07f);

                    var id = (iu * 13 + iv * 7) & 31;
                    if (edge > 0.18f && id < 6)
                    {
                        var lx = (fu - 0.5f) * 2f;
                        var ly = (fv - 0.5f) * 2f;
                        float mk;
                        if (id == 0 || id == 3) mk = HeartA(lx, ly);
                        else if (id == 1 || id == 4) mk = StarMark(lx, ly);
                        else mk = HundredMark(lx, ly);
                        if (mk > 0.3f)
                        {
                            var ink = new Color(0.16f, 0.16f, 0.16f, 1f);
                            col = Color.Lerp(col, ink, mk * 0.45f);
                        }
                    }
                    px[y * w + x] = (Color32)col;
                }
            }
            tex.SetPixels32(px);
            tex.Apply(false, false);
            _mosaic = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
            return _mosaic;
        }

        static float StarMark(float nx, float ny)
        {
            var dist = Mathf.Sqrt(nx * nx + ny * ny);
            if (dist > 0.72f) return 0f;
            var ang = Mathf.Atan2(nx, ny);
            if (ang < 0f) ang += Mathf.PI * 2f;
            var slice = (Mathf.PI * 2f) / 5f;
            var t = (ang % slice) / slice;
            var edge = t < 0.5f ? Mathf.Lerp(1f, 0.38f, t * 2f) : Mathf.Lerp(0.38f, 1f, (t - 0.5f) * 2f);
            return dist < 0.62f * edge ? 1f : 0f;
        }

        static float HundredMark(float nx, float ny)
        {
            var d0 = DigitBar(nx + 0.46f, ny, true);
            var d1 = DigitBar(nx, ny, false);
            var d2 = DigitBar(nx - 0.46f, ny, false);
            return Mathf.Max(d0, Mathf.Max(d1, d2));
        }

        static float DigitBar(float nx, float ny, bool one)
        {
            if (one)
                return Mathf.Abs(nx) < 0.07f && Mathf.Abs(ny) < 0.42f ? 1f : 0f;
            var box = Mathf.Abs(nx) < 0.18f && Mathf.Abs(ny) < 0.42f;
            var hole = Mathf.Abs(nx) < 0.08f && Mathf.Abs(ny) < 0.22f;
            return box && !hole ? 1f : 0f;
        }
    }

    public sealed class EmberDrift : MonoBehaviour
    {
        Vector2 _anchor;
        float _phase;
        float _rise;
        float _sway;
        float _twinkle;
        float _spin;
        Image[] _imgs;
        float[] _baseA;

        public void Seed(System.Random rng)
        {
            var rt = (RectTransform)transform;
            _anchor = rt.anchorMin;
            _phase = (float)rng.NextDouble() * 6.2832f;
            _rise = 0.012f + (float)rng.NextDouble() * 0.022f;
            _sway = 0.008f + (float)rng.NextDouble() * 0.016f;
            _twinkle = 0.5f + (float)rng.NextDouble() * 1.1f;
            _spin = ((float)rng.NextDouble() - 0.5f) * 28f;
            _imgs = GetComponentsInChildren<Image>(true);
            _baseA = new float[_imgs != null ? _imgs.Length : 0];
            for (int i = 0; i < _baseA.Length; i++)
                _baseA[i] = _imgs[i] != null ? _imgs[i].color.a : 0f;
        }

        void Update()
        {
            var rt = (RectTransform)transform;
            if (rt == null) return;
            var dt = Time.unscaledDeltaTime;
            var t = Time.unscaledTime;
            _anchor.y += _rise * dt;
            if (_anchor.y > 0.90f) _anchor.y = 0.16f;
            var x = _anchor.x + Mathf.Sin(t * 0.48f + _phase) * _sway;
            rt.anchorMin = rt.anchorMax = new Vector2(x, _anchor.y);
            rt.localEulerAngles = new Vector3(0f, 0f, rt.localEulerAngles.z + _spin * dt);
            if (_imgs == null || _baseA == null) return;
            var wave = 0.5f + 0.5f * Mathf.Sin(t * _twinkle + _phase);
            for (int i = 0; i < _imgs.Length; i++)
            {
                var img = _imgs[i];
                if (img == null) continue;
                var c = img.color;
                c.a = _baseA[i] * (0.40f + 0.60f * wave);
                img.color = c;
            }
            rt.localScale = Vector3.one * (0.82f + 0.28f * wave);
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
