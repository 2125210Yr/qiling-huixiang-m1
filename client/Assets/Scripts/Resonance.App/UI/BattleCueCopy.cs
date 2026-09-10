using UnityEngine;

namespace Resonance.App
{
    /// <summary>
    /// Observable battle cue copy for HUD and UI demos (M1-G2-UI-CUES).
    ///
    /// Supplementary only: <c>docs/reference/gl-shutdown-pve/supplementary/ragna_gl/CUES.md</c>
    /// (Ragna GL, not ordinary 5-person PVE ground truth). Layout stays
    /// <see cref="LayoutStatus"/>. Do not treat the three-column landscape
    /// crop (<c>ui/</c> <c>crop=406:720:437:0</c>) or memorial lobby stills
    /// as vertical battle pixels. Not T27. Not M1 acceptance.
    ///
    /// Must stay distinguishable on screen:
    /// SLIDE SHOWTIME = 开演 (IT'S SHOWTIME!! — not Fever);
    /// FEVER TIME banner = 狂热时间;
    /// QTE GREAT = 优秀; QTE PERFECT = 完美.
    /// 开战 is battle-start only — never reuse it as the Fever banner or as 开演.
    /// </summary>
    public static class BattleCueCopy
    {
        public const string LayoutStatus = "NEEDS_REFERENCE";
        public const string SourceNote = "supplementary/ragna_gl — not primary GT";

        public const string FeverTime = "狂热时间";
        public const string FeverGauge = "狂热";
        public const string SlideShowtime = "开演";
        public const string SlideSkill = "上滑";
        public const string QteGreat = "优秀";
        public const string QtePerfect = "完美";
        public const string QteGood = "好";
        public const string QteBad = "失误";
        public const string QtePerfectSub = "伤害 150%";
        public const string BattleStart = "开战";
        public const string DriveSelect = "驱动选择";
        public const string DriveReady = "就绪？";
        public const string DriveCast = "驱动";
        public const string EnemyWarn = "警告";
        public const string EnemyWarnSub = "敌方驱动";

        public enum Kind
        {
            None = 0,
            BattleStart,
            SlideShowtime,
            FeverTime,
            QtePerfect,
            QteGreat,
            QteGood,
            QteBad,
            DriveSelect,
            DriveReady,
            DriveCast,
            EnemyWarn
        }

        public static bool ShowtimeDistinctFromFever
        {
            get
            {
                return SlideShowtime != FeverTime
                    && SlideShowtime != BattleStart
                    && FeverTime != BattleStart;
            }
        }

        public static string Title(Kind kind)
        {
            switch (kind)
            {
                case Kind.BattleStart: return BattleStart;
                case Kind.SlideShowtime: return SlideShowtime;
                case Kind.FeverTime: return FeverTime;
                case Kind.QtePerfect: return QtePerfect;
                case Kind.QteGreat: return QteGreat;
                case Kind.QteGood: return QteGood;
                case Kind.QteBad: return QteBad;
                case Kind.DriveSelect: return DriveSelect;
                case Kind.DriveReady: return DriveReady;
                case Kind.DriveCast: return DriveCast;
                case Kind.EnemyWarn: return EnemyWarn;
                default: return "";
            }
        }

        public static string FeverLine(bool window, int pct)
        {
            if (window) return FeverTime;
            return FeverGauge + "  " + pct + "%";
        }

        public static string SlideBanner(string skillName)
        {
            if (string.IsNullOrEmpty(skillName)) return SlideShowtime;
            return SlideShowtime + "  " + skillName;
        }

        /// <summary>
        /// Title handed to <c>VfxWordStamp</c>. Channel-owned words no-op there
        /// so Slide/Fever/QTE do not collapse into one generic slash.
        /// HUD loc for Slide stays <see cref="SlideShowtime"/> (开演); do not
        /// send 开演 through the generic stamp (it is not in that no-op list).
        /// </summary>
        public static string VfxChannelKey(Kind kind)
        {
            if (kind == Kind.SlideShowtime) return "SHOWTIME";
            return Title(kind);
        }

        public static Color StampTint(Kind kind)
        {
            switch (kind)
            {
                case Kind.SlideShowtime: return VisualTokens.StarEvolved;
                case Kind.FeverTime: return VisualTokens.FeverGold;
                case Kind.QtePerfect: return new Color(0.78f, 0.55f, 1f, 1f);
                case Kind.QteGreat: return VisualTokens.GoldTitle;
                case Kind.QteGood: return VisualTokens.YellowValue;
                case Kind.QteBad: return VisualTokens.TextMuted;
                case Kind.DriveSelect:
                case Kind.DriveReady:
                case Kind.DriveCast: return VisualTokens.DriveOrange;
                case Kind.EnemyWarn: return VisualTokens.StarEvolved;
                case Kind.BattleStart: return VisualTokens.YellowConfirm;
                default: return VisualTokens.TapWhite;
            }
        }
    }
}
