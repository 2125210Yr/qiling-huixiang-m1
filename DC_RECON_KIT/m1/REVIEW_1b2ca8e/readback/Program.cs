using System;
using System.IO;
using Resonance.App;
using Resonance.Battle;

namespace B1A4
{
    /// <summary>Load fight-1 from disk and BattleReplayer.Verify on a NEW sim.</summary>
    static class Program
    {
        static int Main(string[] args)
        {
            if (args == null || args.Length < 1)
            {
                Console.Error.WriteLine("usage: Readback <battle-dir>");
                return 2;
            }
            var dir = Path.GetFullPath(args[0]);
            Console.WriteLine("dir=" + dir);
            if (!Directory.Exists(dir))
            {
                Console.Error.WriteLine("BLOCKED: battle dir missing");
                return 2;
            }

            var ev = NaturalPlayBattleEvidence.Load(dir);
            BattleRunRecord tape;
            if (!NaturalPlayBattleEvidence.TryReadRecord(dir, out tape) || tape == null)
            {
                Console.Error.WriteLine("BLOCKED: replay.jsonl parse failed");
                return 2;
            }

            var plan = VerificationRunPlan.Resolve(
                string.IsNullOrEmpty(ev.ScenarioName) ? "np.basic.v1" : ev.ScenarioName,
                null);
            var catalog = VerificationCatalog.Apply(plan);
            Console.WriteLine("catalog=" + catalog);
            NaturalPlayBattleEvidence.BindOpeningPolicy(ev);

            var recovered = NaturalPlayBattleEvidence.RecoverOpeningGrowth(tape);
            var rebuilt = NaturalPlayBattleEvidence.NewSim(tape, recovered);
            var named = NaturalPlayBattleEvidence.NamedOpeningDiff(tape, rebuilt);
            Console.WriteLine("recovered=" + (OpeningGrowth.Format(recovered) ?? ""));
            Console.WriteLine("NamedOpeningDiff=" + (named ?? "null"));

            var report = BattleReplayer.Verify(tape, NaturalPlayBattleEvidence.ReplayFactory(tape, recovered));
            Console.WriteLine("battle_id=" + ev.BattleId);
            Console.WriteLine("scenario=" + ev.ScenarioName);
            Console.WriteLine("Match=" + report.Match);
            Console.WriteLine("Ok=" + report.Ok);
            Console.WriteLine("CommandDiff=" + report.CommandDiff.Count);
            Console.WriteLine("Unconsumed=" + report.Unconsumed.Count);
            Console.WriteLine("DigestDiff=" + report.DigestDiff.Count);
            Console.WriteLine("EventDiff=" + report.EventDiff.Count);
            Console.WriteLine("VersionDiff=" + report.VersionDiff.Count);
            Console.WriteLine("FirstEventDivergence=" + report.FirstEventDivergence);
            Console.WriteLine("ExpectedEventCount=" + report.ExpectedEventCount);
            Console.WriteLine("ActualEventCount=" + report.ActualEventCount);
            var all = report.Diff;
            for (int i = 0; i < all.Count && i < 20; i++)
                Console.WriteLine("diff " + all[i]);
            try { VerificationCatalog.RestoreBuiltin(); } catch { }
            return report.Match ? 0 : 1;
        }
    }
}
