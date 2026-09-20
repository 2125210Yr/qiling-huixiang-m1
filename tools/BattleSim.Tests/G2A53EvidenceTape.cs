using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Resonance.App;
using Resonance.Battle;
using Xunit;
using static Resonance.Tests.G2RecheckFixtures;
using static Resonance.Tests.G2ReviewFixtures;

namespace Resonance.Tests
{
    /// <summary>
    /// Strongly-typed NaturalPlayBattleEvidence helpers for L01/L02 and A53-T05/T06.
    /// No second store type. No reflection.
    /// </summary>
    internal static class G2A53EvidenceTape
    {
        public static readonly string[] RequiredHeaderTokens =
        {
            "seed", "rules", "profile", "auto", "speed", "stage", "party"
        };

        public static readonly string[] RequiredCommandFields =
        {
            "seq=", "tick=", "kind=", "slot=", "source=", "accepted=", "reason="
        };

        public sealed class Session
        {
            public string Root;
            public string SessionId;
            public NaturalPlayBattleEvidence Fight1;
            public NaturalPlayBattleEvidence Fight2;
            public BattleSim Sim1;
            public BattleSim Sim2;
        }

        public static Session RecordTwoFights()
        {
            var root = Path.Combine(Path.GetTempPath(), "a53-ev-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            var sessionId = NaturalPlayBattleEvidence.NewSessionId();

            var sim1 = PlayFight(5305, drive: true);
            var ev1 = NaturalPlayBattleEvidence.Begin(sim1, sessionId, 1, VerificationScenario.Basic);
            Assert.Same(sim1, ev1.Sim);
            Assert.False(string.IsNullOrEmpty(ev1.BattleId));
            Assert.False(string.IsNullOrEmpty(ev1.FrozenHeader));
            ev1.Persist(root, sim1);
            Assert.True(ev1.Persisted);
            Assert.True(Directory.Exists(ev1.Dir));

            var sim2 = PlayFight(5306, drive: false);
            var ev2 = NaturalPlayBattleEvidence.Begin(sim2, sessionId, 2, VerificationScenario.Fever);
            Assert.NotEqual(ev1.BattleId, ev2.BattleId);
            ev2.Persist(root, sim2);
            Assert.True(ev2.Persisted);

            NaturalPlayBattleEvidence.WriteSessionIndex(root, new List<NaturalPlayBattleEvidence> { ev1, ev2 });

            return new Session
            {
                Root = root,
                SessionId = sessionId,
                Fight1 = ev1,
                Fight2 = ev2,
                Sim1 = sim1,
                Sim2 = sim2
            };
        }

        public static void Dispose(Session session)
        {
            if (session == null || string.IsNullOrEmpty(session.Root)) return;
            // Keep the real recorder output for review runs that must not delete evidence.
            // Assertions still execute; only the post-test cleanup is disabled.
            if (Environment.GetEnvironmentVariable("RESONANCE_KEEP_TEST_ARTIFACTS") == "1") return;
            try
            {
                if (Directory.Exists(session.Root))
                    Directory.Delete(session.Root, true);
            }
            catch (IOException)
            {
            }
        }

        public static void AssertTwoBattlesIsolated(Session session)
        {
            Assert.NotNull(session);
            Assert.NotEqual(session.Fight1.BattleId, session.Fight2.BattleId);

            var first = Reload(session.Fight1);
            Assert.Equal(session.Fight1.BattleId, first.BattleId);
            Assert.Contains(session.Fight1.FrozenHeader.Trim(), first.Header.Trim(), StringComparison.Ordinal);
            Assert.DoesNotContain("seed=" + session.Sim2.Seed, first.Header, StringComparison.Ordinal);
            Assert.Contains("seed=" + session.Sim1.Seed, first.Header, StringComparison.Ordinal);
            Assert.True(
                HasDriveHitOrResult(first),
                "L01/T05: battle-001 Drive/hit/result evidence vanished after persisting battle-002");
            Assert.Contains("DriveBegin", first.Commands, StringComparison.Ordinal);
            Assert.DoesNotContain(session.Fight2.BattleId, first.Result, StringComparison.Ordinal);
            Assert.Contains(session.Fight1.BattleId, first.Result, StringComparison.Ordinal);
            Assert.Equal(session.Fight1.Digest.Trim(), first.Digest.Trim());
        }

        public static void AssertEachBattleHasFullTape(Session session)
        {
            Assert.NotNull(session);
            foreach (var ev in new[] { session.Fight1, session.Fight2 })
            {
                var row = Reload(ev);
                Assert.Equal(ev.BattleId, row.BattleId);
                for (int i = 0; i < RequiredHeaderTokens.Length; i++)
                    Assert.True(
                        row.Header.IndexOf(RequiredHeaderTokens[i], StringComparison.OrdinalIgnoreCase) >= 0,
                        "L02: battle_id=" + ev.BattleId + " header missing '" + RequiredHeaderTokens[i] + "': " + row.Header);
                for (int i = 0; i < RequiredCommandFields.Length; i++)
                    Assert.True(
                        row.Commands.IndexOf(RequiredCommandFields[i], StringComparison.OrdinalIgnoreCase) >= 0,
                        "L02: battle_id=" + ev.BattleId + " commands missing '" + RequiredCommandFields[i] + "'");
                Assert.False(string.IsNullOrEmpty(row.Events), "L02: battle_id=" + ev.BattleId + " events.txt empty");
                Assert.True(
                    row.Result.IndexOf("outcome=", StringComparison.OrdinalIgnoreCase) >= 0,
                    "L02: battle_id=" + ev.BattleId + " missing end/exit snapshot");
                Assert.True(
                    row.Header.IndexOf("data=", StringComparison.OrdinalIgnoreCase) >= 0
                    || row.Header.IndexOf("rules=", StringComparison.OrdinalIgnoreCase) >= 0,
                    "L02: battle_id=" + ev.BattleId + " missing RulesVersion / data identity");
                Assert.False(string.IsNullOrEmpty(row.Digest), "L02: battle_id=" + ev.BattleId + " missing digest");
            }
        }

        public static Reloaded Reload(NaturalPlayBattleEvidence ev)
        {
            Assert.NotNull(ev);
            Assert.True(Directory.Exists(ev.Dir), "evidence dir missing: " + ev.Dir);
            return new Reloaded
            {
                BattleId = ev.BattleId,
                Header = Read(ev.Dir, "header.txt"),
                Commands = Read(ev.Dir, "commands.txt"),
                Events = Read(ev.Dir, "events.txt"),
                Result = Read(ev.Dir, "result.txt"),
                Digest = Read(ev.Dir, "digest.txt"),
                Scenario = Read(ev.Dir, "scenario.txt")
            };
        }

        public static void AssertPersistRefusesUnboundLiveSim(Session session)
        {
            Assert.NotNull(session);
            var bound = NaturalPlayBattleEvidence.Begin(
                session.Sim1, session.SessionId, 9, VerificationScenario.Basic);
            Assert.Same(session.Sim1, bound.Sim);
            Assert.NotSame(session.Sim1, session.Sim2);

            Exception caught = null;
            string token = null;
            try
            {
                token = bound.Persist(session.Root, session.Sim2);
            }
            catch (Exception ex)
            {
                caught = ex;
            }

            var wroteWrong = false;
            if (!string.IsNullOrEmpty(bound.Dir) && Directory.Exists(bound.Dir))
            {
                var header = File.Exists(Path.Combine(bound.Dir, "header.txt"))
                    ? File.ReadAllText(Path.Combine(bound.Dir, "header.txt"), Encoding.UTF8)
                    : "";
                var result = File.Exists(Path.Combine(bound.Dir, "result.txt"))
                    ? File.ReadAllText(Path.Combine(bound.Dir, "result.txt"), Encoding.UTF8)
                    : "";
                if (header.IndexOf("seed=" + session.Sim2.Seed, StringComparison.Ordinal) >= 0
                    || result.IndexOf("seed=" + session.Sim2.Seed, StringComparison.Ordinal) >= 0)
                    wroteWrong = true;
            }

            Assert.False(
                wroteWrong,
                "A53-T06: Persist must not write the unbound live BattleSim into this battle_id");
            Assert.True(
                caught != null || !bound.Persisted || IsRefuseToken(token),
                "A53-T06: Persist with a different live BattleSim than the bound Sim must refuse. token="
                + token + " persisted=" + bound.Persisted);
        }

        public static void AssertSessionIndexKeepsRun7Fail(Session session)
        {
            var indexPath = Path.Combine(session.Root, "natural-play", "BATTLE_INDEX.txt");
            Assert.True(File.Exists(indexPath), "WriteSessionIndex did not write BATTLE_INDEX.txt");
            var index = File.ReadAllText(indexPath, Encoding.UTF8);
            Assert.Contains(session.Fight1.BattleId, index, StringComparison.Ordinal);
            Assert.Contains(session.Fight2.BattleId, index, StringComparison.Ordinal);
            Assert.Contains("historical_verdict=FAIL", index, StringComparison.Ordinal);
            Assert.Contains("run7", index, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("historical_verdict=PASS", index, StringComparison.Ordinal);
        }

        public sealed class Reloaded
        {
            public string BattleId;
            public string Header;
            public string Commands;
            public string Events;
            public string Result;
            public string Digest;
            public string Scenario;
        }

        static BattleSim PlayFight(int seed, bool drive)
        {
            var sim = NewJp(seed, AutoMode.Manual, forceNoCrit: true);
            HoldTheLine(sim);
            ChargeAll(sim);
            var slot = SlotOf(sim, "C001");
            Assert.True(slot >= 0);
            Submit(sim, BattleCommandKind.Tap, slot);
            if (drive)
            {
                sim.Drive = 100f;
                var begin = Submit(sim, BattleCommandKind.DriveBegin, slot);
                if (begin.Accepted)
                    Submit(sim, BattleCommandKind.DriveResolve, slot, DriveTiming.Good);
            }
            else
            {
                var support = SlotOf(sim, "C005");
                if (support >= 0)
                {
                    sim.Allies[support].Charge = 100f;
                    Submit(sim, BattleCommandKind.Slide, support);
                }
            }
            return sim;
        }

        static bool HasDriveHitOrResult(Reloaded row)
        {
            var blob = (row.Events ?? "") + "\n" + (row.Commands ?? "") + "\n" + (row.Result ?? "");
            return blob.IndexOf("drive", StringComparison.OrdinalIgnoreCase) >= 0
                || blob.IndexOf("hit", StringComparison.OrdinalIgnoreCase) >= 0
                || blob.IndexOf("result", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        static bool IsRefuseToken(string token)
        {
            if (string.IsNullOrEmpty(token)) return true;
            if (token.IndexOf("no-sim", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (token.IndexOf("refus", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (token.IndexOf("mismatch", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (token.IndexOf("bound", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            return false;
        }

        static string Read(string dir, string name)
        {
            var path = Path.Combine(dir, name);
            Assert.True(File.Exists(path), "missing " + path);
            return File.ReadAllText(path, Encoding.UTF8);
        }
    }
}
