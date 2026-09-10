using System;
using System.Collections.Generic;
using Resonance.Battle;
using UnityEngine;
using UnityEngine.UI;

namespace Resonance.App
{
    public sealed class InspectHooks
    {
        public Action onClose;
        public Action onSkills;
        public Action<int> onEquipSlot;
        public Action<int> onEquipPlus;
        public Action onIgnition;
        public Action onLevel;
        public Action onUncap;
        public Action onAffection;
        public Action onSkin;
        public Action onJoinParty;
        public Action onPrev;
        public Action onNext;
    }

    /// <summary>
    /// 详情铬。G1 later_screens = <see cref="G1Screen"/>。020 语法：右缘白线、左栏压腿、底四槽+技能。无六页签。
    /// 主呈现是厚涂立绘（外层挂 DrawStage），不画圆形半身。
    /// 用户立绘缺失时遮罩 DrawStage 宿主，并写入 <see cref="LastMask"/>（T30 面积，不据此报整屏美术）。
    /// 星列金本命 / 红突破 / 空槽金边；装备槽金线井；详细层 75% 压暗；图库模式配金线画框。
    /// 布局未测自 primary GT。技能行只投影目录字段，不写技能文案。
    /// </summary>
    public static class InspectBoard
    {
        public const string G1Screen = "CharacterDetail";
        public const string LayoutStatus = "NEEDS_REFERENCE";

        /// <summary>与 CharacterPresenter.DrawStage 宿主对齐：全宽、底到 0.88。</summary>
        public const float ArtHostXMin = 0.00f;
        public const float ArtHostYMin = 0.00f;
        public const float ArtHostXMax = 1.00f;
        public const float ArtHostYMax = 0.88f;
        public const float ArtHostAreaNorm = 0.880f;

        public static readonly string[] G1Slots =
        {
            "detail.rail.gallery", "detail.rail.skin", "detail.rail.stats",
            "detail.name", "detail.flavor", "detail.element", "detail.role",
            "detail.lv", "detail.aff", "detail.power",
            "detail.hp", "detail.atk", "detail.def", "detail.agi", "detail.crt",
            "detail.skill_cta", "detail.slots",
            "stats.part.child", "stats.part.aff", "stats.part.gear",
            "equip.empty.body"
        };

        public static InspectArtMaskReport LastMask { get; private set; }

        const float Col = 520f;
        const float Left = 0.055f;
        const float NameY = 0.512f;
        const float FlavorY = 0.452f;
        const float StarY = 0.402f;
        const float StarPx = 34f;
        const float LevelY = 0.362f;
        const float AffY = 0.326f;
        const float PowerY = 0.286f;
        const float StatY0 = 0.248f;
        const float StatStep = 0.032f;
        const float WellX0 = 0.080f;
        const float WellStep = 0.130f;
        const float WellSize = 108f;
        const float WellY = 0.046f;
        const float WellLabelY = 0.096f;
        const float SkillX = 0.880f;
        const float SkillW = 260f;
        const float SkillH = 64f;
        const float RailX = 0.940f;
        const float CloseX = 0.970f;
        static readonly Color SkillDark = new Color(0.102f, 0.102f, 0.102f, 1f);
        static readonly Color PowerYellow = new Color(1f, 0.7686f, 0f, 1f);

        public static void Draw(Transform parent, List<GameObject> built, CharacterDef def, UnitProgress prog, InspectHooks hooks)
        {
            if (parent == null) return;
            if (hooks == null) hooks = new InspectHooks();
            var root = OverlayDraw.Group(parent, built, "InspectBoard");
            if (def == null)
            {
                CommitMask(ProbeMask(null));
                DrawInspectX(root, built, hooks.onClose);
                return;
            }
            var chrome = root.gameObject.AddComponent<InspectChrome>();

            var xGo = DrawInspectX(root, built, () =>
            {
                if (chrome != null && chrome.TryBack()) return;
                if (hooks.onClose != null) hooks.onClose();
            });

            var rail = OverlayDraw.Group(root, built, "Rail");
            DrawRail(rail, built, chrome, hooks);

            var identity = OverlayDraw.Group(root, built, "Identity");
            DrawIdentity(identity, built, def, prog, hooks);

            var stats = OverlayDraw.Group(root, built, "Stats");
            DrawStatStack(stats, built, def, prog, StatY0);

            var detail = OverlayDraw.Group(root, built, "Detail");
            DrawDetail(detail, built, def, prog, StatY0, hooks);
            detail.gameObject.SetActive(false);

            var wells = OverlayDraw.Group(root, built, "Wells");
            DrawWells(wells, built, prog, hooks.onEquipSlot, hooks.onEquipPlus);

            var skill = OverlayDraw.Group(root, built, "Skill");
            DrawSkillPill(skill, built, hooks.onSkills);

            var tick = OverlayDraw.Group(root, built, "Tick");
            DrawTick(tick, built, hooks);

            var frame = OverlayDraw.Group(root, built, "Frame");
            DrawGalleryFrame(frame, built);
            frame.gameObject.SetActive(false);

            var mask = DrawArtMaskIfNeeded(root, built, def);
            chrome.Bind(rail.gameObject, identity.gameObject, stats.gameObject, detail.gameObject,
                wells.gameObject, skill.gameObject, tick.gameObject, frame.gameObject, xGo,
                mask != null ? mask.gameObject : null);
            if (mask != null) mask.SetAsFirstSibling();
            else detail.SetAsFirstSibling();
            if (xGo != null) xGo.transform.SetAsLastSibling();
        }

        public static InspectArtMaskReport ProbeMask(CharacterDef def)
        {
            var id = def != null ? def.Id : "";
            var art = HasUserStandee(id);
            var mask = def != null && !art;
            return new InspectArtMaskReport
            {
                ScreenId = G1Screen,
                CharacterId = id ?? "",
                ArtPresent = art,
                Masked = mask,
                XMin = ArtHostXMin,
                YMin = ArtHostYMin,
                XMax = ArtHostXMax,
                YMax = ArtHostYMax,
                AreaNorm = mask ? ArtHostAreaNorm : 0f,
                HostAreaNorm = ArtHostAreaNorm,
                LayoutStatus = LayoutStatus
            };
        }

        public static string FormatMask(InspectArtMaskReport r)
        {
            return string.Format(
                "[M1-G2-INSPECT] screen={0} id={1} art={2} mask={3} rect={4:0.00},{5:0.00}-{6:0.00},{7:0.00} area={8:0.000} host={9:0.000} layout={10}",
                r.ScreenId, string.IsNullOrEmpty(r.CharacterId) ? "-" : r.CharacterId,
                r.ArtPresent ? 1 : 0, r.Masked ? 1 : 0,
                r.XMin, r.YMin, r.XMax, r.YMax, r.AreaNorm, r.HostAreaNorm, r.LayoutStatus);
        }

        public static void DrawSkillSheet(Transform parent, List<GameObject> built, CharacterDef def, UnitProgress prog, Action onClose)
        {
            if (parent == null) return;
            var root = OverlayDraw.Group(parent, built, "SkillSheet");
            UiChrome.ModalDim(root, built);
            var panelGo = UiChrome.Panel(root, built, new Vector2(0.5f, 0.56f), new Vector2(940, 980));
            if (panelGo == null) return;
            var panel = panelGo.transform;
            GoldWire(panel, built, new Vector2(912, 952));
            OverlayDraw.Label(panel, built, "技能清单", 24, VisualTokens.GoldTitle,
                new Vector2(0.5f, 0.945f), new Vector2(360, 48), false, false, 2f, true);
            UiChrome.CloseX(panel, built, new Vector2(0.93f, 0.945f), onClose);
            OverlayDraw.DashLine(panel, built, new Vector2(0.5f, 0.90f), 820f);

            var ids = KitIds(def);
            var y = 0.80f;
            for (int i = 0; i < ids.Length; i++)
            {
                DrawSheetRow(panel, built, y, Catalog.TrySkill(ids[i]), (SkillType)i);
                y -= 0.14f;
            }
            var confirm = UiChrome.Confirm(root, built, "确认", new Vector2(0.5f, 0.24f), onClose);
            if (confirm != null) confirm.transform.SetAsLastSibling();
        }

        public static void DrawEmptyNotice(Transform parent, List<GameObject> built, Action onClose)
        {
            if (parent == null) return;
            var root = OverlayDraw.Group(parent, built, "EmptyNotice");
            UiChrome.ModalDim(root, built);
            var panelGo = UiChrome.Panel(root, built, new Vector2(0.5f, 0.52f), new Vector2(840, 240));
            if (panelGo == null) return;
            var panel = panelGo.transform;
            GoldWire(panel, built, new Vector2(812, 212));
            OverlayDraw.Label(panel, built, "注意", 22, VisualTokens.GoldTitle,
                new Vector2(0.10f, 0.80f), new Vector2(280, 40), true, false, 0f, true);
            UiChrome.CloseX(panel, built, new Vector2(0.92f, 0.82f), onClose);
            OverlayDraw.DashLine(panel, built, new Vector2(0.5f, 0.62f), 760f);
            OverlayDraw.Label(panel, built, "(i)  未装备任何物品。", 22, VisualTokens.TextPrimary,
                new Vector2(0.5f, 0.38f), new Vector2(720, 48), false, false);
            var confirm = UiChrome.Confirm(root, built, "确认", new Vector2(0.5f, 0.38f), onClose);
            if (confirm != null) confirm.transform.SetAsLastSibling();
        }

        static GameObject DrawInspectX(Transform parent, List<GameObject> built, Action onClick)
        {
            var go = OverlayDraw.Hit(parent, built, new Vector2(CloseX, 0.962f), new Vector2(64, 64), onClick);
            if (go == null) return null;
            go.name = "关闭";
            var halo = OverlayDraw.Pic(go.transform, built, "halo", new Vector2(0.5f, 0.5f), new Vector2(88, 88),
                new Color(0f, 0f, 0f, 0.42f), UiSprites.Soft());
            if (halo != null) halo.raycastTarget = false;
            OverlayDraw.Bar(go.transform, built, new Vector2(0.5f, 0.5f), new Vector2(32, 1.8f), VisualTokens.GoldTitle, 45f);
            OverlayDraw.Bar(go.transform, built, new Vector2(0.5f, 0.5f), new Vector2(32, 1.8f), VisualTokens.GoldTitle, -45f);
            return go;
        }

        static void DrawRail(Transform parent, List<GameObject> built, InspectChrome chrome, InspectHooks hooks)
        {
            var line = OverlayDraw.Bar(parent, built, new Vector2(RailX, 0.735f), new Vector2(2f, 560f),
                new Color(VisualTokens.GoldWire.r, VisualTokens.GoldWire.g, VisualTokens.GoldWire.b, 0.55f));
            if (line != null) line.raycastTarget = false;
            RailTool(parent, built, new Vector2(RailX, 0.855f), "图库\n模式", UiSprites.EyeSlash(), () =>
            {
                if (chrome != null) chrome.ToggleGallery();
            });
            RailTool(parent, built, new Vector2(RailX, 0.735f), "变更\n造型", UiSprites.InspectHanger(), hooks.onSkin);
            RailTool(parent, built, new Vector2(RailX, 0.615f), "详细\n能力", UiSprites.InspectBars(), () =>
            {
                if (chrome != null) chrome.ToggleDetail();
            });
        }

        static void RailTool(Transform parent, List<GameObject> built, Vector2 anchor, string label, Sprite icon, Action click)
        {
            var go = OverlayDraw.Hit(parent, built, anchor, new Vector2(120, 156), click);
            go.name = label.Replace("\n", "");
            var disc = OverlayDraw.Pic(go.transform, built, "disc", new Vector2(0.5f, 0.70f), new Vector2(64, 64),
                new Color(0f, 0f, 0f, 0.42f), UiSprites.Circle());
            if (disc != null) disc.raycastTarget = false;
            var ico = OverlayDraw.Pic(go.transform, built, "ico", new Vector2(0.5f, 0.70f), new Vector2(48, 48),
                Color.white, icon);
            if (ico != null) ico.raycastTarget = false;
            var tx = OverlayDraw.Label(go.transform, built, label, 13, VisualTokens.TextPrimary,
                new Vector2(0.5f, 0.22f), new Vector2(108, 56), false, true, 2f);
            if (tx != null)
            {
                tx.horizontalOverflow = HorizontalWrapMode.Wrap;
                tx.verticalOverflow = VerticalWrapMode.Overflow;
                tx.alignment = TextAnchor.UpperCenter;
                tx.lineSpacing = 0.88f;
            }
        }

        static void DrawIdentity(Transform parent, List<GameObject> built, CharacterDef def, UnitProgress prog, InspectHooks hooks)
        {
            var name = def != null ? def.Name : "";
            var nameTx = UiChrome.NameLabel(parent, built, name, new Vector2(Left, NameY), 56);
            if (nameTx != null)
            {
                nameTx.fontSize = 58;
                nameTx.rectTransform.sizeDelta = new Vector2(780, 112);
            }

            var flavorTx = OverlayDraw.Label(parent, built, FlavorTwoLine(def), 18, VisualTokens.TextSecondary,
                new Vector2(Left, FlavorY), new Vector2(640, 78), true, true, 2f);
            if (flavorTx != null)
            {
                flavorTx.horizontalOverflow = HorizontalWrapMode.Wrap;
                flavorTx.verticalOverflow = VerticalWrapMode.Overflow;
                flavorTx.alignment = TextAnchor.UpperLeft;
                flavorTx.lineSpacing = 0.90f;
            }

            OverlayDraw.DashLine(parent, built, new Vector2(Left + 0.148f, 0.421f), 320f);
            var slots = StarRowLux(parent, built, def, prog, new Vector2(Left, StarY), StarPx);
            DrawStarMeta(parent, built, def, StarY, slots);
            DrawLevelRow(parent, built, prog, LevelY, hooks);
            OverlayDraw.StatRow(parent, built, new Vector2(Left, AffY), Col, "好感度",
                AffectionLine(prog), VisualTokens.TextPrimary, hooks.onAffection);
            DrawPower(parent, built, def, prog, PowerY);
        }

        static string FlavorTwoLine(CharacterDef def)
        {
            var s = def != null ? CharacterPresenter.Flavor(def.Id) : "";
            if (string.IsNullOrEmpty(s)) return "";
            if (s.IndexOf('\n') >= 0) return s;
            var n = s.Length;
            if (n < 5) return s;
            var cut = (n + 1) / 2;
            return s.Substring(0, cut) + "\n" + s.Substring(cut);
        }

        static int StarRowLux(Transform parent, List<GameObject> built, CharacterDef def, UnitProgress prog, Vector2 anchor, float size)
        {
            if (def == null) return 0;
            var native = def.NativeStar;
            var filled = OverlayDraw.StarCount(def, prog);
            var max = def.MaxStar;
            if (max < filled) max = filled;
            if (max < native) max = native;
            if (max < 1) max = 1;
            for (int i = 0; i < max; i++)
            {
                var x = anchor.x + 0.019f + i * 0.038f;
                var on = i < filled;
                var back = OverlayDraw.Pic(parent, built, "底", new Vector2(x, anchor.y),
                    new Vector2(size + 5f, size + 5f), new Color(0f, 0f, 0f, 0.88f), UiSprites.Star());
                if (back != null) back.raycastTarget = false;
                var pip = OverlayDraw.Pic(parent, built, "星", new Vector2(x, anchor.y), new Vector2(size, size),
                    on ? VisualTokens.StarEvolved : VisualTokens.SlotWell,
                    UiSprites.Star());
                if (pip == null) continue;
                pip.raycastTarget = false;
                if (on) continue;
                var ol = pip.gameObject.AddComponent<Outline>();
                ol.effectColor = VisualTokens.SlotRim;
                ol.effectDistance = new Vector2(1f, -1f);
            }
            return max;
        }

        static void DrawStarMeta(Transform parent, List<GameObject> built, CharacterDef def, float y, int slots)
        {
            var x = Left + slots * 0.038f + 0.016f;
            var el = def != null ? def.Element : Element.Dark;
            var disc = OverlayDraw.Pic(parent, built, "属", new Vector2(x, y), new Vector2(28, 28),
                VisualTokens.Element(el), UiSprites.Circle());
            if (disc != null) disc.raycastTarget = false;
            OverlayDraw.Label(parent, built, CharacterPresenter.ElementWord(el), 14, VisualTokens.TextPrimary,
                new Vector2(x + 0.030f, y), new Vector2(48, 28), true, true, 2f, true);
            var role = def != null ? CharacterPresenter.RoleLabel(def.Role) : "";
            OverlayDraw.Label(parent, built, role, 14, VisualTokens.TextSecondary,
                new Vector2(x + 0.082f, y), new Vector2(80, 28), true, true, 2f);
        }

        static void DrawLevelRow(Transform parent, List<GameObject> built, UnitProgress prog, float y, InspectHooks hooks)
        {
            OverlayDraw.Label(parent, built, "等级", 16, VisualTokens.TextStat,
                new Vector2(Left, y), new Vector2(80, 32), true, true, 2f);
            var lv = prog != null ? prog.Level : 1;
            var uncap = prog != null ? prog.Uncap : 0;
            if (hooks != null && hooks.onLevel != null)
                OverlayDraw.Hit(parent, built, new Vector2(Left + Col / 2160f, y), new Vector2(Col, 32), hooks.onLevel);
            var plusTx = OverlayDraw.Label(parent, built, "+" + uncap, 18, VisualTokens.GoldSelect,
                new Vector2(Left, y), new Vector2(Col - 132f, 32), true, true, 2f, true);
            if (plusTx != null) plusTx.alignment = TextAnchor.MiddleRight;
            var lvTx = OverlayDraw.Label(parent, built, lv + "/" + Growth.MaxLevel, 18, VisualTokens.TextPrimary,
                new Vector2(Left, y), new Vector2(Col, 32), true, true, 2f);
            if (lvTx != null) lvTx.alignment = TextAnchor.MiddleRight;
            if (hooks != null && hooks.onUncap != null)
                OverlayDraw.Hit(parent, built, new Vector2(Left + 0.38f, y), new Vector2(96, 32), hooks.onUncap);
        }

        static void DrawPower(Transform parent, List<GameObject> built, CharacterDef def, UnitProgress prog, float y)
        {
            if (def == null) return;
            var grown = Growth.Apply(def, prog);
            var tx = UiChrome.PowerPlate(parent, built, Growth.CombatPower(grown), new Vector2(Left, y), 48);
            if (tx == null) return;
            tx.fontSize = 52;
            tx.color = PowerYellow;
            tx.rectTransform.sizeDelta = new Vector2(520, 72);
            tx.rectTransform.localScale = new Vector3(0.92f, 1.16f, 1f);
            tx.rectTransform.anchoredPosition = new Vector2(108f, 2f);
        }

        static void DrawStatStack(Transform parent, List<GameObject> built, CharacterDef def, UnitProgress prog, float y)
        {
            var grown = Growth.Apply(def, prog);
            if (grown == null) return;
            TallStat(parent, built, y, "生命", OverlayDraw.Comma(grown.Hp));
            y -= StatStep;
            TallStat(parent, built, y, "攻击", OverlayDraw.Comma(grown.Atk));
            y -= StatStep;
            TallStat(parent, built, y, "防御", OverlayDraw.Comma(grown.Def));
            y -= StatStep;
            TallStat(parent, built, y, "敏捷", OverlayDraw.Comma(grown.Agl));
            y -= StatStep;
            TallStat(parent, built, y, "暴击", OverlayDraw.Comma(grown.Crt));
        }

        static void TallStat(Transform parent, List<GameObject> built, float y, string label, string value)
        {
            OverlayDraw.Label(parent, built, label, 20, VisualTokens.TextStat,
                new Vector2(Left, y), new Vector2(180, 52), true, true, 2f);
            var val = OverlayDraw.Label(parent, built, value, 24, VisualTokens.TextPrimary,
                new Vector2(Left, y), new Vector2(Col, 52), true, true, 2.5f, true);
            if (val != null) val.alignment = TextAnchor.MiddleRight;
        }

        static void DrawDetail(Transform parent, List<GameObject> built, CharacterDef def, UnitProgress prog, float y, InspectHooks hooks)
        {
            var dim = OverlayDraw.Wash(parent, built, VisualTokens.OverlayDim, false);
            if (dim != null) dim.transform.SetAsFirstSibling();
            var ign = prog != null ? prog.Ignition : 0;
            var ignMax = def != null ? def.IgnitionMax : 12;
            if (hooks != null && hooks.onIgnition != null)
                OverlayDraw.Hit(parent, built, new Vector2(Left + Col / 2160f, y), new Vector2(Col, 32), hooks.onIgnition);
            OverlayDraw.StatRow(parent, built, new Vector2(Left, y), Col, "燃起",
                ign + "/" + ignMax, VisualTokens.TextPrimary, null);
            OverlayDraw.DashLine(parent, built, new Vector2(Left + 0.148f, y - 0.016f), 320f);
            y -= 0.028f;
            var grown = Growth.Apply(def, prog);
            if (grown == null) return;
            var br = Growth.BreakDown(def, prog);
            y = DrawBreak(parent, built, y, "生命", grown.Hp, br.BodyHp, br.AffHp, br.GearHp);
            y = DrawBreak(parent, built, y, "攻击", grown.Atk, br.BodyAtk, br.AffAtk, br.GearAtk);
            y = DrawBreak(parent, built, y, "防御", grown.Def, br.BodyDef, br.AffDef, br.GearDef);
            y = DrawBreak(parent, built, y, "敏捷", grown.Agl, br.BodyAgl, br.AffAgl, br.GearAgl);
            y = DrawBreak(parent, built, y, "暴击", grown.Crt, br.BodyCrt, br.AffCrt, br.GearCrt);
            DrawDetailGear(parent, built, prog, hooks != null ? hooks.onEquipPlus : null, Mathf.Min(y, 0.108f));
        }

        static float DrawBreak(Transform parent, List<GameObject> built, float y, string label, int total, int body, int aff, int gear)
        {
            OverlayDraw.StatRow(parent, built, new Vector2(Left, y), Col, label, OverlayDraw.Comma(total), VisualTokens.TextPrimary, null);
            y -= 0.016f;
            OverlayDraw.Label(parent, built, "契体  " + OverlayDraw.Comma(body), 13, VisualTokens.TextMuted,
                new Vector2(Left + 0.01f, y), new Vector2(160, 24), true, false);
            OverlayDraw.Label(parent, built, "好感  " + OverlayDraw.Comma(aff), 13, VisualTokens.TextMuted,
                new Vector2(Left + 0.18f, y), new Vector2(160, 24), true, false);
            OverlayDraw.Label(parent, built, "装备  " + OverlayDraw.Comma(gear), 13, VisualTokens.TextMuted,
                new Vector2(Left + 0.34f, y), new Vector2(160, 24), true, false);
            return y - 0.018f;
        }

        static void DrawDetailGear(Transform parent, List<GameObject> built, UnitProgress prog, Action<int> onPlus, float y)
        {
            for (int i = 0; i < 4; i++)
            {
                var id = SlotId(prog, i);
                if (string.IsNullOrEmpty(id)) continue;
                var slot = i;
                var plus = WellPlus(prog, slot);
                var line = GearCatalog.SlotNames[i] + "  " + GearCatalog.Label(id);
                if (plus > 0) line += "  +" + plus;
                OverlayDraw.Label(parent, built, line, 14, VisualTokens.TextSecondary,
                    new Vector2(Left, y), new Vector2(360, 26), true, true, 2f);
                if (onPlus != null)
                {
                    var pill = OverlayDraw.Pic(parent, built, "强化", new Vector2(Left + 0.44f, y), new Vector2(88, 28),
                        VisualTokens.RailFill, UiSprites.Pill());
                    if (pill != null)
                    {
                        pill.raycastTarget = false;
                        var ol = pill.gameObject.AddComponent<Outline>();
                        ol.effectColor = VisualTokens.GoldMetal;
                        ol.effectDistance = new Vector2(1f, -1f);
                    }
                    OverlayDraw.Hit(parent, built, new Vector2(Left + 0.44f, y), new Vector2(88, 28),
                        () => onPlus(slot));
                    OverlayDraw.Label(parent, built, "强化", 13, VisualTokens.GoldMetal,
                        new Vector2(Left + 0.44f, y), new Vector2(80, 26), false, true, 2f);
                }
                y -= 0.022f;
            }
        }

        static void DrawWells(Transform parent, List<GameObject> built, UnitProgress prog, Action<int> onSlot, Action<int> onPlus)
        {
            var ids = new[]
            {
                prog != null ? prog.Gear0 : "",
                prog != null ? prog.Gear1 : "",
                prog != null ? prog.Gear2 : "",
                prog != null ? prog.Gear3 : ""
            };
            for (int i = 0; i < 4; i++)
            {
                var slot = i;
                var x = WellX0 + i * WellStep;
                OverlayDraw.Label(parent, built, GearCatalog.SlotNames[i], 13, VisualTokens.TextMuted,
                    new Vector2(x, WellLabelY), new Vector2(108, 26), false, true, 2f);
                var filled = !string.IsNullOrEmpty(ids[i]);
                var go = OverlayDraw.Hit(parent, built, new Vector2(x, WellY), new Vector2(WellSize, WellSize),
                    () => { if (onSlot != null) onSlot(slot); });
                go.name = GearCatalog.SlotNames[i];
                var img = go.GetComponent<Image>();
                if (img != null)
                {
                    UiSprites.Apply(img, UiSprites.Round());
                    img.color = new Color(0.10f, 0.10f, 0.10f, 1f);
                    var rim = go.AddComponent<Outline>();
                    rim.effectColor = filled ? VisualTokens.GoldMetal : VisualTokens.SlotRim;
                    rim.effectDistance = new Vector2(1.5f, -1.5f);
                }
                var fill = OverlayDraw.Pic(go.transform, built, "fill", new Vector2(0.5f, 0.5f), new Vector2(94, 94),
                    filled ? new Color(0.18f, 0.15f, 0.10f, 1f) : VisualTokens.SlotWell, UiSprites.Round());
                if (fill != null) fill.raycastTarget = false;
                var wire = OverlayDraw.Pic(go.transform, built, "wire", new Vector2(0.5f, 0.5f), new Vector2(100, 100),
                    new Color(VisualTokens.GoldWire.r, VisualTokens.GoldWire.g, VisualTokens.GoldWire.b, filled ? 0.95f : 0.60f),
                    UiSprites.WireFrame());
                if (wire != null) wire.raycastTarget = false;
                var ico = OverlayDraw.Pic(go.transform, built, "ico", new Vector2(0.5f, 0.5f), new Vector2(52, 52),
                    filled ? VisualTokens.GoldSelect : VisualTokens.RailIcon, WellSprite(i));
                if (ico != null) ico.raycastTarget = false;
                if (!filled) continue;
                var plus = WellPlus(prog, slot);
                if (plus <= 0) continue;
                var badge = OverlayDraw.Pic(go.transform, built, "badge", new Vector2(0.78f, 0.82f), new Vector2(48, 26),
                    new Color(0.02f, 0.02f, 0.02f, 0.88f), UiSprites.Round());
                if (badge != null)
                {
                    badge.raycastTarget = false;
                    var bol = badge.gameObject.AddComponent<Outline>();
                    bol.effectColor = VisualTokens.GoldMetal;
                    bol.effectDistance = new Vector2(1f, -1f);
                }
                OverlayDraw.Label(go.transform, built, "+" + plus, 12, VisualTokens.GoldSelect,
                    new Vector2(0.78f, 0.82f), new Vector2(44, 22), false, true, 2f, true);
                if (onPlus == null) continue;
                var plusGo = OverlayDraw.Hit(go.transform, built, new Vector2(0.78f, 0.82f), new Vector2(40, 28),
                    () => onPlus(slot));
                if (plusGo != null) plusGo.name = "plus";
            }
        }

        static Sprite WellSprite(int slot)
        {
            if (slot == 0) return UiSprites.InspectSword();
            if (slot == 1) return UiSprites.InspectShield();
            if (slot == 2) return UiSprites.InspectRing();
            return UiSprites.InspectCards();
        }

        static void DrawSkillPill(Transform parent, List<GameObject> built, Action onSkills)
        {
            var go = OverlayDraw.Hit(parent, built, new Vector2(SkillX, WellY), new Vector2(SkillW, SkillH), onSkills);
            if (go == null) return;
            go.name = "技能";
            var img = go.GetComponent<Image>();
            if (img != null)
            {
                UiSprites.Apply(img, UiSprites.Pill());
                img.color = SkillDark;
                var ol = go.AddComponent<Outline>();
                ol.effectColor = VisualTokens.GoldMetal;
                ol.effectDistance = new Vector2(1.2f, -1.2f);
            }
            var mask = go.AddComponent<RectMask2D>();
            if (mask != null) mask.enabled = true;
            var stripe = OverlayDraw.Pic(go.transform, built, "stripe", new Vector2(0.5f, 0.5f), new Vector2(SkillW, SkillH),
                new Color(1f, 1f, 1f, 0.06f), UiSprites.InspectDiag());
            if (stripe != null)
            {
                stripe.raycastTarget = false;
                stripe.type = Image.Type.Tiled;
                stripe.preserveAspect = false;
            }
            var slash = OverlayDraw.Pic(go.transform, built, "slash", new Vector2(0.16f, 0.5f), new Vector2(44, 28),
                VisualTokens.GoldSelect, UiSprites.Slash());
            if (slash != null) slash.raycastTarget = false;
            var spark = OverlayDraw.Pic(go.transform, built, "spark", new Vector2(0.16f, 0.5f), new Vector2(18, 18),
                PowerYellow, UiSprites.Spark());
            if (spark != null) spark.raycastTarget = false;
            OverlayDraw.Label(go.transform, built, "技能", 24, VisualTokens.TextPrimary,
                new Vector2(0.62f, 0.5f), new Vector2(148, 44), false, true, 2f, true);
        }

        static void DrawTick(Transform parent, List<GameObject> built, InspectHooks hooks)
        {
            OverlayDraw.Bar(parent, built, new Vector2(0.018f, 0.62f), new Vector2(3f, 78f), Color.white);
            if (hooks != null && hooks.onPrev != null)
                OverlayDraw.Hit(parent, built, new Vector2(0.028f, 0.58f), new Vector2(48, 220), hooks.onPrev);
            if (hooks != null && hooks.onNext != null)
                OverlayDraw.Hit(parent, built, new Vector2(0.80f, 0.55f), new Vector2(48, 720), hooks.onNext);
        }

        static void DrawGalleryFrame(Transform parent, List<GameObject> built)
        {
            var wire = OverlayDraw.Pic(parent, built, "框", new Vector2(0.5f, 0.5f), new Vector2(1032, 1856),
                new Color(VisualTokens.GoldWire.r, VisualTokens.GoldWire.g, VisualTokens.GoldWire.b, 0.95f),
                UiSprites.WireFrame());
            if (wire != null) wire.raycastTarget = false;
            CornerSpark(parent, built, new Vector2(0.022f, 0.983f));
            CornerSpark(parent, built, new Vector2(0.978f, 0.983f));
            CornerSpark(parent, built, new Vector2(0.022f, 0.017f));
            CornerSpark(parent, built, new Vector2(0.978f, 0.017f));
            OverlayDraw.Label(parent, built, "图 库", 14, VisualTokens.GoldMetal,
                new Vector2(0.5f, 0.028f), new Vector2(160, 28), false, true, 2f);
        }

        static void CornerSpark(Transform parent, List<GameObject> built, Vector2 anchor)
        {
            var sp = OverlayDraw.Pic(parent, built, "芒", anchor, new Vector2(18, 18),
                VisualTokens.GoldMetal, UiSprites.Spark());
            if (sp != null) sp.raycastTarget = false;
        }

        static void GoldWire(Transform parent, List<GameObject> built, Vector2 size)
        {
            var img = OverlayDraw.Pic(parent, built, "wire", new Vector2(0.5f, 0.5f), size,
                VisualTokens.PanelFill, UiSprites.Round());
            if (img == null) return;
            img.raycastTarget = false;
            var ol = img.gameObject.AddComponent<Outline>();
            ol.effectColor = VisualTokens.GoldMetal;
            ol.effectDistance = new Vector2(1f, -1f);
        }

        static void DrawSheetRow(Transform panel, List<GameObject> built, float y, SkillDef s, SkillType fallback)
        {
            var type = s != null ? s.Type : fallback;
            var tagCol = VisualTokens.Skill(type);
            var chip = OverlayDraw.Pic(panel, built, "tag", new Vector2(0.10f, y), new Vector2(56, 56),
                tagCol, UiSprites.Circle());
            if (chip != null) chip.raycastTarget = false;
            var onChip = type == SkillType.Auto ? VisualTokens.TextPrimary : VisualTokens.TextOnYellow;
            OverlayDraw.Label(panel, built, VisualTokens.SkillTag(type), 13, onChip,
                new Vector2(0.10f, y), new Vector2(56, 56), false, false, 2f, true);
            OverlayDraw.Label(panel, built, SkillName(s), 20, VisualTokens.TextPrimary,
                new Vector2(0.18f, y + 0.022f), new Vector2(700, 36), true, false, 2f, true);
            var body = OverlayDraw.Label(panel, built, SkillLine(s), 15, VisualTokens.TextSecondary,
                new Vector2(0.18f, y - 0.028f), new Vector2(720, 48), true, false);
            if (body != null)
            {
                body.horizontalOverflow = HorizontalWrapMode.Wrap;
                body.verticalOverflow = VerticalWrapMode.Overflow;
            }
            OverlayDraw.DashLine(panel, built, new Vector2(0.5f, y - 0.062f), 780f);
        }

        static int WellPlus(UnitProgress prog, int slot)
        {
            if (prog == null) return 0;
            int n;
            switch (slot)
            {
                case 1: n = prog.Plus1; break;
                case 2: n = prog.Plus2; break;
                case 3: n = prog.Plus3; break;
                default: n = prog.Plus0; break;
            }
            return Equipment.ClampPlus(n);
        }

        static string SlotId(UnitProgress prog, int slot)
        {
            if (prog == null) return "";
            switch (slot)
            {
                case 1: return prog.Gear1 ?? "";
                case 2: return prog.Gear2 ?? "";
                case 3: return prog.Gear3 ?? "";
                default: return prog.Gear0 ?? "";
            }
        }

        static string[] KitIds(CharacterDef def)
        {
            if (def == null) return new[] { "", "", "", "", "" };
            return new[] { def.AutoSkillId, def.TapSkillId, def.SlideSkillId, def.DriveSkillId, def.LeaderSkillId };
        }

        static string SkillName(SkillDef s)
        {
            return s != null && !string.IsNullOrEmpty(s.Name) ? s.Name : "—";
        }

        // 目录字段投影（目标 / 段数 / 治疗或伤害 / 效果种）。不写技能文案。
        static string SkillLine(SkillDef s)
        {
            if (s == null) return "—";
            var bits = new List<string>(6);
            bits.Add(TargetWord(s.Target));
            if (s.TargetCount > 1) bits.Add("×" + s.TargetCount);
            if (s.HitCount > 1) bits.Add(s.HitCount + "击");
            if (s.HealCoef > 0f || s.FlatHeal > 0 || s.HealMaxHpFrac > 0f)
                bits.Add("治疗");
            else if (s.AtkCoef > 0f || s.FlatPower > 0)
                bits.Add("伤害");
            var fx = Catalog.TryEffect(s.EffectId);
            if (fx != null)
            {
                var w = EffectWord(fx.Kind);
                if (!string.IsNullOrEmpty(w)) bits.Add(w);
            }
            return bits.Count == 0 ? "—" : string.Join(" · ", bits.ToArray());
        }

        static string TargetWord(TargetRule t)
        {
            switch (t)
            {
                case TargetRule.Self: return "自身";
                case TargetRule.LowestHpAlly: return "残血盟友";
                case TargetRule.AllAllies: return "全体盟友";
                case TargetRule.RandomEnemies: return "随机敌人";
                case TargetRule.LowestHpEnemies: return "残血敌人";
                case TargetRule.HighestAtkEnemies: return "高攻敌人";
                case TargetRule.AllEnemies: return "全体敌人";
                default: return "—";
            }
        }

        static string EffectWord(EffectKind k)
        {
            switch (k)
            {
                case EffectKind.Damage: return "伤害";
                case EffectKind.Heal: return "回复";
                case EffectKind.Dot: return "持续伤害";
                case EffectKind.Shield: return "屏障";
                case EffectKind.AtkBuff: return "攻击↑";
                case EffectKind.DefDebuff: return "防御↓";
                case EffectKind.ChargeHaste: return "充能加速";
                case EffectKind.Taunt: return "挑衅";
                case EffectKind.TsAmp: return "点按强化";
                case EffectKind.SsAmp: return "上滑强化";
                case EffectKind.DsAmp: return "驱动强化";
                case EffectKind.SkillDefDown: return "技能防御↓";
                case EffectKind.WeakDefDown: return "弱点防御↓";
                case EffectKind.Reflect: return "反射";
                case EffectKind.Immortal: return "不死";
                case EffectKind.Silence: return "沉默";
                case EffectKind.Stun: return "晕眩";
                case EffectKind.Freeze: return "冻结";
                case EffectKind.Bleed: return "流血";
                case EffectKind.Poison: return "中毒";
                case EffectKind.Burn: return "灼烧";
                case EffectKind.AntiHeal: return "禁疗";
                case EffectKind.ChargeAmount: return "充能↑";
                case EffectKind.ChargeSpeed: return "充能速度↑";
                case EffectKind.CooldownDelta: return "冷却";
                case EffectKind.Barrier: return "屏障";
                case EffectKind.DebuffBarrier: return "减益屏障";
                case EffectKind.Enrage: return "激怒";
                case EffectKind.Overload: return "超载";
                case EffectKind.DualWield: return "双刃";
                default: return "";
            }
        }

        static string AffectionLine(UnitProgress prog)
        {
            var n = prog != null ? prog.Affection : 0;
            if (n < 0) n = 0;
            if (n > 100) n = 100;
            return n + "/100";
        }

        static Transform DrawArtMaskIfNeeded(Transform parent, List<GameObject> built, CharacterDef def)
        {
            var report = ProbeMask(def);
            CommitMask(report);
            if (!report.Masked) return null;
            var host = OverlayDraw.Group(parent, built, "ArtMask");
            if (host == null) return null;
            var rt = host.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = new Vector2(ArtHostXMin, ArtHostYMin);
                rt.anchorMax = new Vector2(ArtHostXMax, ArtHostYMax);
                rt.offsetMin = rt.offsetMax = Vector2.zero;
            }
            var dim = OverlayDraw.Wash(host, built, new Color(0f, 0f, 0f, 0.42f), false);
            if (dim != null)
            {
                dim.gameObject.name = "ArtMaskDim";
                dim.raycastTarget = false;
            }
            var hatch = OverlayDraw.Wash(host, built, new Color(1f, 1f, 1f, 0.08f), false);
            if (hatch != null)
            {
                hatch.gameObject.name = "ArtMaskHatch";
                UiSprites.Apply(hatch, UiSprites.InspectDiag());
                hatch.type = Image.Type.Tiled;
                hatch.preserveAspect = false;
                hatch.raycastTarget = false;
            }
            var pct = (report.AreaNorm * 100f).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);
            var stamp = OverlayDraw.Label(host, built, "立绘遮罩  " + pct + "%", 16, VisualTokens.TextMuted,
                new Vector2(0.50f, 0.58f), new Vector2(320, 36), false, true, 2f);
            if (stamp != null) stamp.gameObject.name = "ArtMaskStamp";
            return host;
        }

        static bool HasUserStandee(string id)
        {
            if (string.IsNullOrEmpty(id)) return false;
            if (CharacterArt.TryPuppet(id, out _)) return true;
            CubismPlates plates;
            if (CharacterArt.TryCubismPlates(id, out plates) && plates.body != null) return true;
            LiveParts live;
            if (CharacterArt.TryLiveLayers(id, out live) && (live.torso != null || live.head != null)) return true;
            Texture2D body;
            if (CharacterArt.TrySpineLayers(id, out body, out _, out _, out _, out _, out _)
                && body != null)
                return true;
            if (CharacterArt.TryPreviewLayers(id, out body, out _, out _, out _, out _)
                && body != null)
                return true;
            if (CharacterArt.TryV3Layers(id, out body, out _, out _, out _, out _)
                && body != null)
                return true;
            if (id != "C001" && CharacterArt.TryLayers(id, out body, out _, out _, out _, out _)
                && body != null)
                return true;
            Texture2D idle;
            if (CharacterArt.TryPresenter(id, out idle, out _) && idle != null && idle.width >= 400)
                return true;
            if (CharacterArt.Body(id) != null) return true;
            if (CharacterArt.Face(id) != null) return true;
            return false;
        }

        static void CommitMask(InspectArtMaskReport report)
        {
            LastMask = report;
            Debug.Log(FormatMask(report));
        }
    }

    public struct InspectArtMaskReport
    {
        public string ScreenId;
        public string CharacterId;
        public bool ArtPresent;
        public bool Masked;
        public float XMin;
        public float YMin;
        public float XMax;
        public float YMax;
        public float AreaNorm;
        public float HostAreaNorm;
        public string LayoutStatus;

        public override string ToString()
        {
            return InspectBoard.FormatMask(this);
        }
    }

    sealed class InspectChrome : MonoBehaviour
    {
        GameObject _rail;
        GameObject _identity;
        GameObject _stats;
        GameObject _detail;
        GameObject _wells;
        GameObject _skill;
        GameObject _tick;
        GameObject _frame;
        GameObject _close;
        GameObject _artMask;
        int _mode;

        public void Bind(GameObject rail, GameObject identity, GameObject stats, GameObject detail,
            GameObject wells, GameObject skill, GameObject tick, GameObject frame, GameObject close,
            GameObject artMask)
        {
            _rail = rail;
            _identity = identity;
            _stats = stats;
            _detail = detail;
            _wells = wells;
            _skill = skill;
            _tick = tick;
            _frame = frame;
            _close = close;
            _artMask = artMask;
        }

        public void ToggleGallery()
        {
            _mode = _mode == 1 ? 0 : 1;
            Apply();
        }

        public void ToggleDetail()
        {
            _mode = _mode == 2 ? 0 : 2;
            Apply();
        }

        public bool TryBack()
        {
            if (_mode == 0) return false;
            _mode = 0;
            Apply();
            return true;
        }

        void Apply()
        {
            var gallery = _mode == 1;
            var detail = _mode == 2;
            if (_rail != null) _rail.SetActive(!gallery && !detail);
            if (_identity != null) _identity.SetActive(!gallery);
            if (_stats != null) _stats.SetActive(!gallery && !detail);
            if (_artMask != null) _artMask.transform.SetAsFirstSibling();
            if (_detail != null)
            {
                _detail.SetActive(detail);
                if (detail)
                    _detail.transform.SetSiblingIndex(_artMask != null ? 1 : 0);
            }
            if (_wells != null) _wells.SetActive(!gallery && !detail);
            if (_skill != null) _skill.SetActive(!gallery && !detail);
            if (_tick != null) _tick.SetActive(!gallery);
            if (_frame != null)
            {
                _frame.SetActive(gallery);
                if (gallery) _frame.transform.SetAsLastSibling();
            }
            if (_close != null) _close.transform.SetAsLastSibling();
        }
    }
}
