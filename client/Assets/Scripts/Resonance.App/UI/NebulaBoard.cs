using System.Collections.Generic;
using Resonance.Battle;
using UnityEngine;
using UnityEngine.UI;

namespace Resonance.App
{
    /// <summary>
    /// 星云一趟示意：五人圆台，按层换背景。一至五层桩。不是可玩肉鸽。
    /// 狂热字跟 <see cref="BattleCueCopy"/> 对齐，但这是大厅示意，不是战斗坐标。
    /// </summary>
    public static class NebulaBoard
    {
        public const int FloorCount = 5;

        static readonly float[] Charge = { 0.62f, 0.78f, 0.45f, 0.90f, 0.55f };

        public static void Draw(Transform parent, int floor, System.Action onBattle)
        {
            if (parent == null) return;
            floor = Mathf.Clamp(floor, 1, FloorCount);
            var built = new List<GameObject>();
            var root = OverlayDraw.Group(parent, built, "NebulaBoard");
            var spec = Spec(floor);

            CharacterPresenter.StageArena(root, built, spec.Arena);
            Wash(root, built, spec);
            Header(root, built, floor, spec);
            Path(root, built, floor);
            Party(root, built);
            Foe(root, built, spec);
            Rings(root, built);
            DriveMark(root, built, spec);
            Fever(root, built, spec);
            OverlayDraw.Label(root, built, "示意 · 词条出星云作废", 13, VisualTokens.TextMuted,
                new Vector2(0.5f, 0.028f), new Vector2(720, 28), false, false);
            UiChrome.Confirm(root, built, "战斗", new Vector2(0.5f, 0.072f), onBattle);
        }

        struct FloorSpec
        {
            public string Name;
            public string Tag;
            public string Hint;
            public int Arena;
            public float Fever;
            public Color WashA;
            public Color WashB;
            public Kind Kind;
        }

        enum Kind { Fight, Shop, Boss }

        static FloorSpec Spec(int floor)
        {
            switch (floor)
            {
                case 1:
                    return new FloorSpec
                    {
                        Name = "哨站",
                        Tag = "点按 · 圆台没变",
                        Hint = "五人、圆台、点按还是那五个圆。",
                        Arena = 0,
                        Fever = 0.18f,
                        WashA = new Color(0.81f, 0.58f, 0.01f, 0.16f),
                        WashB = new Color(0.05f, 0.05f, 0.05f, 0.18f),
                        Kind = Kind.Fight
                    };
                case 2:
                    return new FloorSpec
                    {
                        Name = "星屑带",
                        Tag = BattleCueCopy.SlideSkill,
                        Hint = "手还是那套。镜头词不变。",
                        Arena = 3,
                        Fever = 0.48f,
                        WashA = new Color(0.11f, 0.55f, 0.88f, 0.22f),
                        WashB = new Color(0.81f, 0.58f, 0.01f, 0.12f),
                        Kind = Kind.Fight
                    };
                case 3:
                    return new FloorSpec
                    {
                        Name = "近核",
                        Tag = "驱动 · 底中橙圆",
                        Hint = "驱动仍是底中那颗橙圆。改的是判定，不是键位。",
                        Arena = 8,
                        Fever = 0.72f,
                        WashA = new Color(0.91f, 0.45f, 0.00f, 0.20f),
                        WashB = new Color(1.00f, 0.77f, 0.00f, 0.10f),
                        Kind = Kind.Fight
                    };
                case 4:
                    return new FloorSpec
                    {
                        Name = "行商",
                        Tag = "星屑换词条",
                        Hint = "买的也是本趟词条。出星云一样作废。",
                        Arena = 5,
                        Fever = 0.72f,
                        WashA = new Color(0.11f, 0.55f, 0.88f, 0.18f),
                        WashB = new Color(0.05f, 0.12f, 0.22f, 0.24f),
                        Kind = Kind.Shop
                    };
                default:
                    return new FloorSpec
                    {
                        Name = "星核",
                        Tag = BattleCueCopy.FeverTime,
                        Hint = "五人打一个大的。底栏不会变成十二个头。",
                        Arena = 11,
                        Fever = 1f,
                        WashA = new Color(1.00f, 0.27f, 0.09f, 0.22f),
                        WashB = new Color(0.78f, 0.16f, 0.45f, 0.16f),
                        Kind = Kind.Boss
                    };
            }
        }

        static void Wash(Transform parent, List<GameObject> built, FloorSpec spec)
        {
            OverlayDraw.Pic(parent, built, "washA", new Vector2(0.50f, 0.82f), new Vector2(1400, 720),
                spec.WashA, UiSprites.Soft());
            OverlayDraw.Pic(parent, built, "washB", new Vector2(0.70f, 0.28f), new Vector2(980, 640),
                spec.WashB, UiSprites.Soft());
        }

        static void Header(Transform parent, List<GameObject> built, int floor, FloorSpec spec)
        {
            OverlayDraw.Label(parent, built, "星云一趟", 28, VisualTokens.GoldTitle,
                new Vector2(0.5f, 0.968f), new Vector2(640, 44), false, true, 2f, true);
            OverlayDraw.Label(parent, built, spec.Name + "  ·  " + floor + " / " + FloorCount, 18, VisualTokens.YellowValue,
                new Vector2(0.5f, 0.938f), new Vector2(640, 32), false, true, 2f);
            OverlayDraw.Label(parent, built, spec.Tag, 14, VisualTokens.GoldMetal,
                new Vector2(0.5f, 0.838f), new Vector2(720, 28), false, true, 2f, true);
            var hint = OverlayDraw.Label(parent, built, spec.Hint, 15, VisualTokens.TextSecondary,
                new Vector2(0.5f, 0.812f), new Vector2(880, 36), false, false);
            hint.horizontalOverflow = HorizontalWrapMode.Wrap;
        }

        static void Path(Transform parent, List<GameObject> built, int floor)
        {
            for (int i = 0; i < FloorCount; i++)
            {
                var n = i + 1;
                var x = 0.18f + i * 0.16f;
                var spec = Spec(n);
                var now = n == floor;
                var done = n < floor;
                var size = spec.Kind == Kind.Boss ? 36f : 28f;
                var sprite = spec.Kind == Kind.Shop ? UiSprites.Round() : UiSprites.Circle();
                if (now)
                    OverlayDraw.Pic(parent, built, "now" + n, new Vector2(x, 0.892f),
                        new Vector2(size + 10f, size + 10f), VisualTokens.YellowValue, sprite);
                OverlayDraw.Pic(parent, built, "node" + n, new Vector2(x, 0.892f), new Vector2(size, size),
                    MarkColor(spec.Kind, done), sprite);
                if (i < FloorCount - 1)
                    OverlayDraw.Bar(parent, built, new Vector2(x + 0.08f, 0.892f), new Vector2(40f, 2f), VisualTokens.Line);
                OverlayDraw.Label(parent, built, spec.Name, 12,
                    now ? VisualTokens.TextPrimary : VisualTokens.TextMuted,
                    new Vector2(x, 0.862f), new Vector2(120, 24), false, true, 2f);
            }
        }

        static Color MarkColor(Kind kind, bool done)
        {
            if (kind == Kind.Boss) return VisualTokens.StarEvolved;
            if (kind == Kind.Shop) return VisualTokens.ElemWater;
            if (done) return new Color(0.35f, 0.28f, 0.10f, 1f);
            return new Color(0.22f, 0.18f, 0.10f, 1f);
        }

        static void Party(Transform parent, List<GameObject> built)
        {
            for (int i = 0; i < FloorCount; i++)
            {
                var def = PartyAt(i);
                var pos = CharacterPresenter.AllyFieldAnchor(i);
                if (def != null)
                {
                    var go = CharacterPresenter.Draw(parent, def, pos, 260f, "", false);
                    OverlayDraw.Track(built, go);
                    OverlayDraw.Label(parent, built, CharacterPresenter.RoleMark(def.Role), 14,
                        VisualTokens.Element(def.Element),
                        pos + new Vector2(0f, -0.078f), new Vector2(88, 24), false, true, 2f);
                    continue;
                }
                OverlayDraw.Pic(parent, built, "empty", pos, new Vector2(96, 28),
                    new Color(0.12f, 0.11f, 0.10f, 0.92f), UiSprites.Circle());
            }
        }

        static void Foe(Transform parent, List<GameObject> built, FloorSpec spec)
        {
            if (spec.Kind == Kind.Shop) return;
            var pos = CharacterPresenter.EnemyFieldAnchor(0, 1);
            var size = spec.Kind == Kind.Boss ? 92f : 72f;
            var rim = spec.Kind == Kind.Boss ? VisualTokens.StarEvolved : VisualTokens.GoldMetal;
            OverlayDraw.Pic(parent, built, "foeRim", pos, new Vector2(size + 10f, size + 10f), rim, UiSprites.Circle());
            OverlayDraw.Pic(parent, built, "foe", pos, new Vector2(size, size),
                new Color(0.08f, 0.06f, 0.06f, 0.96f), UiSprites.Circle());
            OverlayDraw.Label(parent, built, spec.Name, 14, VisualTokens.TextPrimary,
                pos, new Vector2(size, 32), false, true, 2f, true);
            OverlayDraw.Label(parent, built, spec.Kind == Kind.Boss ? "核" : "障", 11, VisualTokens.TextMuted,
                pos + new Vector2(0f, -0.046f), new Vector2(120, 22), false, false);
        }

        static void Rings(Transform parent, List<GameObject> built)
        {
            OverlayDraw.Pic(parent, built, "dock", new Vector2(0.5f, 0.04f), new Vector2(1280, 250),
                new Color(0.05f, 0.055f, 0.07f, 0.88f), UiSprites.Circle());
            OverlayDraw.Pic(parent, built, "lip", new Vector2(0.5f, 0.225f), new Vector2(1080, 6),
                new Color(0.81f, 0.58f, 0.05f, 0.55f), UiSprites.Round());
            for (int i = 0; i < FloorCount; i++)
            {
                var def = PartyAt(i);
                var x = 0.12f + i * 0.19f;
                var y = 0.158f;
                var el = def != null ? VisualTokens.Element(def.Element) : VisualTokens.GoldMetal;
                var ring = OverlayDraw.Pic(parent, built, "ring" + i, new Vector2(x, y), new Vector2(72, 72),
                    VisualTokens.YellowValue, UiSprites.Circle());
                ring.type = Image.Type.Filled;
                ring.fillMethod = Image.FillMethod.Radial360;
                ring.fillOrigin = (int)Image.Origin360.Top;
                ring.fillClockwise = true;
                ring.fillAmount = Charge[i];
                OverlayDraw.Pic(parent, built, "rim" + i, new Vector2(x, y), new Vector2(62, 62),
                    Color.Lerp(VisualTokens.GoldMetal, el, 0.40f), UiSprites.Circle());
                OverlayDraw.Pic(parent, built, "well" + i, new Vector2(x, y), new Vector2(54, 54),
                    new Color(0.06f, 0.05f, 0.05f, 0.96f), UiSprites.Circle());
                OverlayDraw.Label(parent, built,
                    def != null ? CharacterPresenter.RoleMark(def.Role) : "—",
                    13, VisualTokens.YellowValue, new Vector2(x, y), new Vector2(56, 28), false, true, 2f, true);
            }
        }

        static void DriveMark(Transform parent, List<GameObject> built, FloorSpec spec)
        {
            var glow = spec.Kind == Kind.Fight && spec.Arena == 8
                ? VisualTokens.Ember
                : new Color(VisualTokens.Ember.r, VisualTokens.Ember.g, VisualTokens.Ember.b, 0.72f);
            OverlayDraw.Pic(parent, built, "driveRim", new Vector2(0.5f, 0.248f), new Vector2(96, 96),
                VisualTokens.GoldMetal, UiSprites.Circle());
            OverlayDraw.Pic(parent, built, "drive", new Vector2(0.5f, 0.248f), new Vector2(84, 84),
                glow, UiSprites.Circle());
            OverlayDraw.Label(parent, built, "驱动", 16, VisualTokens.TextOnYellow,
                new Vector2(0.5f, 0.248f), new Vector2(80, 32), false, true, 2f, true);
        }

        static void Fever(Transform parent, List<GameObject> built, FloorSpec spec)
        {
            OverlayDraw.Pic(parent, built, "fBg", new Vector2(0.5f, 0.118f), new Vector2(640, 12),
                new Color(0.10f, 0.08f, 0.03f, 0.92f), UiSprites.Round());
            var fill = OverlayDraw.Pic(parent, built, "fFill", new Vector2(0.5f, 0.118f), new Vector2(628, 8),
                spec.Fever >= 1f ? VisualTokens.StarEvolved : VisualTokens.FeverGold, UiSprites.Round());
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillAmount = spec.Fever;
            OverlayDraw.Label(parent, built,
                BattleCueCopy.FeverLine(spec.Fever >= 1f, Mathf.RoundToInt(spec.Fever * 100f)),
                11, VisualTokens.TextOnYellow, new Vector2(0.5f, 0.118f), new Vector2(320, 16), false, true, 2f, true);
        }

        static CharacterDef PartyAt(int slot)
        {
            string id = null;
            var save = GameRoot.Live != null ? GameRoot.Live.SaveData : null;
            if (save != null && save.PartyIds != null && slot >= 0 && slot < save.PartyIds.Length)
                id = save.PartyIds[slot];
            if (string.IsNullOrEmpty(id) && Catalog.DefaultParty != null && slot >= 0 && slot < Catalog.DefaultParty.Length)
                id = Catalog.DefaultParty[slot];
            return Catalog.TryChar(id);
        }
    }
}
