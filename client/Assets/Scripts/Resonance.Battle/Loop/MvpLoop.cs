using System;
using System.Text;

namespace Resonance.Battle
{
    public static class MvpLoop
    {
        public static bool TryClearChapter(SaveBlob blob, bool hard, int seed, out string report)
        {
            Catalog.BuildBuiltin();
            if (blob == null) blob = new SaveBlob();
            SaveStore.EnsureStarterKit(blob);
            var table = Catalog.Chapter(hard);
            if (table == null || table.Length != 12)
            {
                report = "FAIL chapter length";
                return false;
            }
            var sb = new StringBuilder();
            for (int i = 0; i < table.Length; i++)
            {
                var sim = new BattleSim(blob.PartyIds, blob.LeaderSlot, seed + i, table[i], blob.ProgressForParty())
                {
                    Deterministic = true,
                    AutoTap = true,
                    Speed = 1,
                    Profile = FormulaProfile.JP_LEGACY_EMPIRICAL
                };
                var cap = BattleSim.TickHz * 120;
                var ticks = PlayOut(sim, cap);
                sb.Append(table[i].Id).Append('=').Append(sim.Outcome)
                    .Append(" ticks=").Append(ticks)
                    .Append(" drive=").Append(sim.Drive.ToString("0"))
                    .Append('\n');
                if (sim.Outcome != BattleOutcome.Victory)
                {
                    report = sb.ToString();
                    return false;
                }
                SaveStore.ApplyVictory(blob, i, hard);
            }
            report = sb.ToString();
            return true;
        }

        public static int PlayOut(BattleSim sim, int capTicks)
        {
            if (sim == null || capTicks < 1) return 0;
            var ticks = 0;
            while (ticks < capTicks)
            {
                if (sim.Outcome == BattleOutcome.InProgress)
                    sim.Tick();
                else if (sim.FeverActive)
                    sim.TickFeverOnly();
                else
                    break;
                ticks++;
            }
            return ticks;
        }

        public static bool HasEventKind(BattleSim sim, string kind)
        {
            return CountEventKind(sim, kind) > 0;
        }

        public static int CountEventKind(BattleSim sim, string kind)
        {
            if (sim == null || sim.Events == null || string.IsNullOrEmpty(kind)) return 0;
            var n = 0;
            var list = sim.Events.Events;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] != null && string.Equals(list[i].Kind, kind, StringComparison.Ordinal))
                    n++;
            }
            return n;
        }

        public static bool TryPlayUntilKindSince(BattleSim sim, string kind, int startCount, int capTicks, out int ticks)
        {
            ticks = 0;
            if (sim == null || capTicks < 1 || string.IsNullOrEmpty(kind)) return false;
            while (ticks < capTicks)
            {
                if (HasKindSince(sim, kind, startCount)) return true;
                if (sim.Outcome == BattleOutcome.InProgress)
                    sim.Tick();
                else if (sim.FeverActive)
                    sim.TickFeverOnly();
                else
                    break;
                ticks++;
            }
            return HasKindSince(sim, kind, startCount);
        }

        static bool HasKindSince(BattleSim sim, string kind, int startCount)
        {
            if (sim == null || sim.Events == null || string.IsNullOrEmpty(kind)) return false;
            var list = sim.Events.Events;
            for (int i = startCount; i < list.Count; i++)
            {
                if (list[i] != null && string.Equals(list[i].Kind, kind, StringComparison.Ordinal))
                    return true;
            }
            return false;
        }
    }
}
