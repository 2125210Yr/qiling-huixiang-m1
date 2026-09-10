namespace Resonance.Battle
{
    /// <summary>
    /// Shared 5-person slice Drive fill + Perfect QTE resolve.
    /// Charge is spent on each Tap/Slide/Drive; callers refill via ReadyCharges.
    /// </summary>
    public static class SliceDriveSequence
    {
        public static void ReadyCharges(BattleSim sim)
        {
            if (sim == null || sim.Allies == null) return;
            for (int i = 0; i < sim.Allies.Length; i++)
            {
                if (sim.Allies[i] != null && sim.Allies[i].Alive)
                    sim.Allies[i].Charge = 100f;
            }
        }

        public static bool HasDriveCast(BattleSim sim)
        {
            if (sim == null || sim.Casts == null) return false;
            for (int i = 0; i < sim.Casts.Count; i++)
            {
                var fx = sim.Casts[i];
                if (fx != null && fx.CasterAlly && fx.Type == SkillType.Drive)
                    return true;
            }
            return false;
        }

        public static bool TryFillDrive(BattleSim sim, int guard = 32)
        {
            if (sim == null) return false;
            if (sim.Drive >= 100f) return true;
            while (sim.Drive < 100f && guard-- > 0 && sim.Outcome == BattleOutcome.InProgress)
            {
                ReadyCharges(sim);
                var any = false;
                for (int i = 0; i < sim.Allies.Length; i++)
                {
                    if (sim.Drive >= 100f) break;
                    if (sim.TrySlide(i) || sim.TryTap(i)) any = true;
                }
                if (!any) return false;
            }
            return sim.Drive >= 100f;
        }

        public static bool TryFirePerfect(BattleSim sim)
        {
            if (sim == null || sim.Outcome != BattleOutcome.InProgress) return false;
            if (sim.Drive < 100f && !TryFillDrive(sim)) return false;
            ReadyCharges(sim);
            var casts = sim.Casts != null ? sim.Casts.Count : 0;
            if (sim.PendingDriveSlot >= 0)
                return sim.ResolveDrive(DriveTiming.Perfect);
            for (int i = 0; i < sim.Allies.Length; i++)
            {
                if (sim.Allies[i] == null || !sim.Allies[i].Alive) continue;
                if (!sim.TryBeginDrive(i)) continue;
                if (sim.PendingDriveSlot >= 0)
                    return sim.ResolveDrive(DriveTiming.Perfect);
                return HasDriveCastSince(sim, casts);
            }
            return HasDriveCastSince(sim, casts);
        }

        public static bool PlayUntilFever(BattleSim sim, int maxDrives = 8)
        {
            if (sim == null) return false;
            var drove = false;
            for (int n = 0; n < maxDrives && !sim.FeverActive && sim.Outcome == BattleOutcome.InProgress; n++)
            {
                if (!TryFillDrive(sim)) break;
                if (!TryFirePerfect(sim)) break;
                drove = true;
            }
            return drove;
        }

        static bool HasDriveCastSince(BattleSim sim, int start)
        {
            if (sim == null || sim.Casts == null) return false;
            for (int i = start; i < sim.Casts.Count; i++)
            {
                var fx = sim.Casts[i];
                if (fx != null && fx.CasterAlly && fx.Type == SkillType.Drive)
                    return true;
            }
            return false;
        }
    }
}
