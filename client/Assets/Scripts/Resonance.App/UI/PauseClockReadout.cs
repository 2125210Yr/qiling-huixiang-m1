using System.Text;
using Resonance.Battle;

namespace Resonance.App
{
    /// <summary>
    /// Read-only pause / speed view of <see cref="BattleSim"/> + <see cref="BattleClockPolicy"/>.
    /// Host persist tiers are ×1 / ×2. Reference speed table is U011 UNKNOWN. Not T04.
    /// </summary>
    public readonly struct PauseClockView
    {
        public readonly bool HasBattle;
        public readonly bool Paused;
        public readonly int Speed;
        public readonly float BattleDt;
        public readonly string SpeedWord;
        public readonly string StatusLine;
        public readonly string PolicyLine;

        public PauseClockView(bool hasBattle, bool paused, int speed, float battleDt,
            string speedWord, string statusLine, string policyLine)
        {
            HasBattle = hasBattle;
            Paused = paused;
            Speed = speed;
            BattleDt = battleDt;
            SpeedWord = speedWord ?? "";
            StatusLine = statusLine ?? "";
            PolicyLine = policyLine ?? "";
        }
    }

    public static class PauseClockReadout
    {
        public const string LayoutStatus = "NEEDS_REFERENCE";
        public const int SpeedLo = 1;
        public const int SpeedHi = 2;

        public const string Title = "暂停";
        public const string SpeedSection = "倍速";
        public const string HomeWarn = "回首页将离开本场战斗";
        public const string UiIndependent = "UI 不随暂停冻";
        public const string Unmeasured = "参考档未测";

        public static bool IsHostTier(int speed)
        {
            return speed == SpeedLo || speed == SpeedHi;
        }

        public static string FormatSpeed(int speed)
        {
            if (speed < 1) speed = 1;
            return "×" + speed;
        }

        public static PauseClockView Read(BattleSim battle)
        {
            if (battle == null)
            {
                return new PauseClockView(false, false, SpeedLo, BattleSim.TickDt,
                    FormatSpeed(SpeedLo), "无战斗时钟", "只读对接失败 · " + Unmeasured);
            }

            var speed = battle.Speed < 1 ? 1 : battle.Speed;
            var clocks = battle.Clocks;
            var dt = clocks != null ? clocks.BattleDt(speed) : BattleSim.TickDt * speed;
            if (dt <= 0f) dt = BattleSim.TickDt;

            var word = FormatSpeed(speed);
            var status = battle.Paused
                ? "战斗时钟已停  ·  现档 " + word
                : "战斗时钟在走  ·  现档 " + word;
            return new PauseClockView(true, battle.Paused, speed, dt, word, status, PolicyLine(clocks, speed));
        }

        public static string PolicyLine(BattleClockPolicy clocks, int speed)
        {
            var sb = new StringBuilder(96);
            sb.Append("战斗步 ").Append(FormatSpeed(speed < 1 ? 1 : speed));
            sb.Append("  ·  ").Append(Coupling(clocks));
            sb.Append("  ·  ").Append(UiIndependent);
            sb.Append("  ·  ").Append(Unmeasured);
            return sb.ToString();
        }

        public static string Coupling(BattleClockPolicy clocks)
        {
            if (clocks == null) return "时钟政策缺省";
            var sb = new StringBuilder(48);
            Append(sb, clocks.StageCountdownScalesWithSpeed, "倒计时");
            Append(sb, clocks.ChargeScalesWithSpeed, "充能");
            Append(sb, clocks.SlideCdScalesWithSpeed, "滑步冷却");
            Append(sb, clocks.StatusDurationScalesWithSpeed, "状态");
            Append(sb, clocks.AutoIntervalScalesWithSpeed, "自动");
            Append(sb, clocks.FeverWindowScalesWithSpeed, "狂热窗");
            Append(sb, clocks.DriveQteScalesWithSpeed, "驱动QTE");
            if (sb.Length == 0) return "各时钟不随倍速";
            return "随倍速：" + sb;
        }

        static void Append(StringBuilder sb, bool on, string word)
        {
            if (!on || sb == null || string.IsNullOrEmpty(word)) return;
            if (sb.Length > 0) sb.Append('·');
            sb.Append(word);
        }
    }
}
