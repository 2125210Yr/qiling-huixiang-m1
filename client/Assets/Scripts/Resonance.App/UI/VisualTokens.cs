using UnityEngine;

namespace Resonance.App
{
    public static class VisualTokens
    {
        public static readonly Color BgVoid = Hex("050505");
        public static readonly Color BgMosaic = Hex("121212");
        public static readonly Color FloorDark = Hex("101012");
        public static readonly Color FloorLight = Hex("2A2934");
        public static readonly Color PanelFill = Hex("121010");
        public static readonly Color PanelFillAlt = Hex("161111");
        public static readonly Color SlotWell = Hex("252526");
        public static readonly Color SlotRim = Hex("5C5C5C");
        public static readonly Color Rim = Hex("4A4A4A");
        public static readonly Color Line = Hex("333333");
        public static readonly Color GoldMetal = Hex("CF9403");
        public static readonly Color GoldTitle = Hex("D1A40B");
        public static readonly Color GoldSelect = Hex("F8BA00");
        public static readonly Color YellowValue = Hex("FFC400");
        public static readonly Color YellowNavOn = Hex("FFCC00");
        public static readonly Color YellowConfirm = Hex("FAC700");
        public static readonly Color YellowConfirmTop = Hex("FDDA00");
        public static readonly Color YellowConfirmBottom = Hex("FFB200");
        public static readonly Color OrangeLeader = Hex("E97400");
        public static readonly Color OrangeSClass = Hex("FD7100");
        public static readonly Color OrangeCancel = Hex("B05228");
        public static readonly Color OrangeCancelBottom = Hex("7A3018");
        public static readonly Color TextPrimary = Hex("FBFBFB");
        public static readonly Color TextStat = Hex("E8E8E8");
        public static readonly Color TextSecondary = Hex("A0A0A0");
        public static readonly Color TextMuted = Hex("808080");
        public static readonly Color TextOnYellow = Hex("1A1A1A");
        public static readonly Color StarEvolved = Hex("FF4618");
        public static readonly Color StarCatalog = Hex("FFC400");
        public static readonly Color ElemFire = Hex("D02018");
        public static readonly Color ElemWater = Hex("1C8DE1");
        public static readonly Color ElemWood = Hex("2F900A");
        public static readonly Color ElemLight = Hex("D0B33D");
        public static readonly Color ElemDark = Hex("9A4DB8");
        public static readonly Color RoleDisc = Hex("141414");
        public static readonly Color Ember = Hex("FF8C00");
        public static readonly Color EmberHot = Hex("FFD966");
        public static readonly Color IceCore = Hex("D7F6FF");
        public static readonly Color IceShard = Hex("7EC8E8");
        public static readonly Color IceDeep = Hex("143044");
        public static readonly Color Aurora = Hex("5EE0C8");
        public static readonly Color UnderGlow = Hex("A35C2B");
        public static readonly Color RailIcon = Hex("ABA9A6");
        public static readonly Color RailFill = Hex("1C1C1C");
        public static readonly Color GoldWire = Hex("8E6D3E");
        public static readonly Color OverlayDim = new Color(0f, 0f, 0f, 0.75f);
        public static readonly Color SlideGreen = Hex("7DFF6A");
        public static readonly Color DriveOrange = Hex("FF8C00");
        public static readonly Color FeverGold = Hex("FFD24A"); // Fever banner tint — not SHOWTIME
        public static readonly Color TapWhite = Hex("FFF4D6");
        public static readonly Color AutoGrey = Hex("B8B8B8");

        public static Color Skill(Resonance.Battle.SkillType t)
        {
            switch (t)
            {
                case Resonance.Battle.SkillType.Slide: return SlideGreen;
                case Resonance.Battle.SkillType.Drive: return DriveOrange;
                case Resonance.Battle.SkillType.Auto: return AutoGrey;
                case Resonance.Battle.SkillType.Leader: return GoldTitle;
                default: return TapWhite;
            }
        }

        // Verb chips only. Slide SHOWTIME copy is BattleCueCopy.SlideShowtime (开演), not this.
        public static string SkillTag(Resonance.Battle.SkillType t)
        {
            switch (t)
            {
                case Resonance.Battle.SkillType.Slide: return BattleCueCopy.SlideSkill;
                case Resonance.Battle.SkillType.Drive: return BattleCueCopy.DriveCast;
                case Resonance.Battle.SkillType.Auto: return "连击";
                case Resonance.Battle.SkillType.Leader: return "队长";
                default: return "点按";
            }
        }

        public static Color Element(Resonance.Battle.Element e)
        {
            switch (e)
            {
                case Resonance.Battle.Element.Fire: return ElemFire;
                case Resonance.Battle.Element.Water: return ElemWater;
                case Resonance.Battle.Element.Wood: return ElemWood;
                case Resonance.Battle.Element.Light: return ElemLight;
                default: return ElemDark;
            }
        }

        public static Color Hex(string h)
        {
            Color c;
            if (string.IsNullOrEmpty(h))
                return new Color(0f, 0f, 0f, 1f);
            var s = h[0] == '#' ? h : "#" + h;
            if (!ColorUtility.TryParseHtmlString(s, out c))
                return new Color(0f, 0f, 0f, 1f);
            if (c.a <= 0f) c.a = 1f;
            return c;
        }
    }
}
