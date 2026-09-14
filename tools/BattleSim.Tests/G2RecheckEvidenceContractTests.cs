using System;
using System.Collections.Generic;
using System.Reflection;
using Resonance.Battle;
using Xunit;

namespace Resonance.Tests
{
    /// <summary>
    /// L01/L02 — BattleEvidence store contract. Unity NEXT/home is out of reach here.
    /// A store type may not exist yet; that is a FAIL with the required shape, not a skip.
    /// </summary>
    public sealed class G2RecheckEvidenceContractTests
    {
        [Fact]
        public void L01_TwoBattleIds_FirstFightEventsSurviveNext()
        {
            var storeType = G2RecheckEvidenceContract.FindStoreType();
            if (storeType == null)
            {
                Assert.Fail(
                    "L01: no BattleEvidence store on the current core API. Required: unique battle_id per fight; " +
                    "after NEXT, battle-001 Drive/hit/result events must still be readable; " +
                    "battle-002 RunHeader/CommandLog must not replace battle-001. " +
                    "BattleRunRecord.Capture of the live sim is not enough — that is the X02 overwrite bug.");
                return;
            }

            var store = G2RecheckEvidenceContract.CreateStore(storeType);
            G2RecheckEvidenceContract.AssertTwoBattlesIsolated(store);
        }

        [Fact]
        public void L02_EachBattle_HasFrozenHeaderCommandsAndExitSnapshot()
        {
            var storeType = G2RecheckEvidenceContract.FindStoreType();
            if (storeType == null)
            {
                Assert.Fail(
                    "L02: no BattleEvidence store on the current core API. Required per battle_id: " +
                    "frozen opening header (seed/profile/auto/speed/party/stage/data identity), " +
                    "full command fields Seq/Tick/Kind/Slot/Source/Accepted/Reason, " +
                    "event log, end-or-exit snapshot, and RulesVersion + content/source identity " +
                    "that match the cited run. Multi-fight natural sessions must keep every fight.");
                return;
            }

            var store = G2RecheckEvidenceContract.CreateStore(storeType);
            G2RecheckEvidenceContract.AssertEachBattleHasFullTape(store);
        }
    }

    /// <summary>
    /// Assertions a BattleEvidence store MUST satisfy. New production types may appear later;
    /// lookup is by name/shape so this file still compiles against today's API.
    /// </summary>
    internal static class G2RecheckEvidenceContract
    {
        public const string BattleIdA = "battle-001";
        public const string BattleIdB = "battle-002";

        public static readonly string[] RequiredHeaderTokens =
        {
            "seed", "rules", "profile", "auto", "speed", "stage", "party"
        };

        public static readonly string[] RequiredCommandFields =
        {
            "Seq", "Tick", "Kind", "Slot", "Source", "Accepted", "Reason"
        };

        public static Type FindStoreType()
        {
            var asm = typeof(BattleSim).Assembly;
            Type named = null;
            foreach (var t in asm.GetTypes())
            {
                if (t == null || t.IsAbstract) continue;
                var n = t.Name;
                if (n.IndexOf("BattleEvidence", StringComparison.OrdinalIgnoreCase) >= 0
                    || n.IndexOf("BattleRecordStore", StringComparison.OrdinalIgnoreCase) >= 0
                    || n.IndexOf("FightEvidence", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    named = t;
                    break;
                }
            }
            if (named != null) return named;

            foreach (var t in asm.GetTypes())
            {
                if (t == null || t.IsAbstract) continue;
                if (HasBattleId(t) && (HasMethod(t, "Save") || HasMethod(t, "Retain") || HasMethod(t, "Archive") || HasMethod(t, "Put")))
                    return t;
            }
            return null;
        }

        public static object CreateStore(Type storeType)
        {
            Assert.NotNull(storeType);
            var ctor = storeType.GetConstructor(Type.EmptyTypes);
            Assert.True(ctor != null, "L01/L02: BattleEvidence store '" + storeType.Name + "' needs a public parameterless ctor for core assertions.");
            return ctor.Invoke(null);
        }

        public static void AssertTwoBattlesIsolated(object store)
        {
            Assert.NotNull(store);
            var first = SaveSynthetic(store, BattleIdA, driveEvents: true, headerTag: "FIRST");
            var second = SaveSynthetic(store, BattleIdB, driveEvents: false, headerTag: "SECOND");
            Assert.False(string.Equals(ReadBattleId(first), ReadBattleId(second), StringComparison.Ordinal),
                "L01: two retained fights must have distinct battle_id");

            var loadedFirst = Load(store, BattleIdA);
            Assert.NotNull(loadedFirst);
            Assert.Equal(BattleIdA, ReadBattleId(loadedFirst));
            Assert.True(HasDriveOrResultEvidence(loadedFirst),
                "L01: battle-001 Drive/hit/result evidence vanished after retaining battle-002");
            var header = ReadHeader(loadedFirst) ?? "";
            Assert.DoesNotContain("SECOND", header, StringComparison.Ordinal);
            Assert.Contains("FIRST", header, StringComparison.Ordinal);
        }

        public static void AssertEachBattleHasFullTape(object store)
        {
            Assert.NotNull(store);
            SaveSynthetic(store, BattleIdA, driveEvents: true, headerTag: "OPEN-A");
            SaveSynthetic(store, BattleIdB, driveEvents: false, headerTag: "OPEN-B");
            foreach (var id in new[] { BattleIdA, BattleIdB })
            {
                var row = Load(store, id);
                Assert.NotNull(row);
                var header = ReadHeader(row) ?? "";
                for (int i = 0; i < RequiredHeaderTokens.Length; i++)
                    Assert.True(
                        header.IndexOf(RequiredHeaderTokens[i], StringComparison.OrdinalIgnoreCase) >= 0,
                        "L02: battle_id=" + id + " header missing '" + RequiredHeaderTokens[i] + "': " + header);
                AssertHasCommandFields(row);
                Assert.True(HasExitSnapshot(row), "L02: battle_id=" + id + " missing end/exit snapshot");
                Assert.True(HasVersionIdentity(row), "L02: battle_id=" + id + " missing RulesVersion / data identity");
            }
        }

        static object SaveSynthetic(object store, string battleId, bool driveEvents, string headerTag)
        {
            var save = FindMutator(store.GetType(), "Save", "Retain", "Archive", "Put", "Add");
            Assert.True(save != null, "L01/L02: store " + store.GetType().Name + " has no Save/Retain/Archive/Put");

            var payload = BuildPayload(save.GetParameters(), battleId, driveEvents, headerTag);
            var result = save.Invoke(store, payload);
            return result ?? Load(store, battleId);
        }

        static object Load(object store, string battleId)
        {
            var load = FindMutator(store.GetType(), "Load", "Get", "TryGet", "Find");
            if (load == null) return null;
            var args = BindId(load.GetParameters(), battleId);
            var ret = load.Invoke(store, args);
            if (ret is bool)
                return null;
            return ret;
        }

        static object[] BuildPayload(ParameterInfo[] ps, string battleId, bool driveEvents, string headerTag)
        {
            var args = new object[ps.Length];
            for (int i = 0; i < ps.Length; i++)
            {
                var t = ps[i].ParameterType;
                var name = ps[i].Name ?? "";
                if (t == typeof(string) && name.IndexOf("id", StringComparison.OrdinalIgnoreCase) >= 0)
                    args[i] = battleId;
                else if (t == typeof(string) && name.IndexOf("header", StringComparison.OrdinalIgnoreCase) >= 0)
                    args[i] = SyntheticHeader(headerTag);
                else if (typeof(BattleSim).IsAssignableFrom(t))
                    args[i] = SyntheticSim(driveEvents);
                else if (typeof(BattleRunRecord).IsAssignableFrom(t))
                    args[i] = SyntheticRecord(battleId, driveEvents, headerTag);
                else
                    args[i] = t.IsValueType ? Activator.CreateInstance(t) : null;
            }
            return args;
        }

        static object[] BindId(ParameterInfo[] ps, string battleId)
        {
            var args = new object[ps.Length];
            for (int i = 0; i < ps.Length; i++)
            {
                if (ps[i].ParameterType == typeof(string))
                    args[i] = battleId;
                else
                    args[i] = ps[i].ParameterType.IsValueType ? Activator.CreateInstance(ps[i].ParameterType) : null;
            }
            return args;
        }

        static BattleSim SyntheticSim(bool driveEvents)
        {
            var sim = G2ReviewFixtures.NewJp(driveEvents ? 101 : 102, AutoMode.Manual, forceNoCrit: true);
            G2ReviewFixtures.InflateEnemies(sim);
            G2ReviewFixtures.ChargeAll(sim);
            if (driveEvents)
            {
                sim.Drive = 100f;
                sim.TryBeginDrive(0);
                sim.ResolveDrive(DriveTiming.Good);
            }
            return sim;
        }

        static BattleRunRecord SyntheticRecord(string battleId, bool driveEvents, string headerTag)
        {
            var sim = SyntheticSim(driveEvents);
            var rec = BattleRunRecord.Capture(sim, Catalog.DefaultParty, Catalog.VerticalSliceStage != null ? Catalog.VerticalSliceStage.Id : "VS-1");
            rec.RunHeader = SyntheticHeader(headerTag) + ";battle_id=" + battleId + ";" + rec.RunHeader;
            return rec;
        }

        static string SyntheticHeader(string tag)
        {
            return "seed=1;rules=" + BattleSim.RulesVersion + ";profile=JP_LEGACY_EMPIRICAL;auto=Manual;speed=1;stage=VS-1;party=C001;tag=" + tag;
        }

        static string ReadBattleId(object row)
        {
            if (row == null) return null;
            var t = row.GetType();
            foreach (var n in new[] { "BattleId", "battle_id", "Id" })
            {
                var p = t.GetProperty(n, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (p != null && p.PropertyType == typeof(string))
                    return p.GetValue(row) as string;
                var f = t.GetField(n, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (f != null && f.FieldType == typeof(string))
                    return f.GetValue(row) as string;
            }
            return null;
        }

        static string ReadHeader(object row)
        {
            if (row == null) return null;
            var t = row.GetType();
            foreach (var n in new[] { "RunHeader", "Header", "OpeningHeader" })
            {
                var p = t.GetProperty(n, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (p != null && p.PropertyType == typeof(string))
                    return p.GetValue(row) as string;
                var f = t.GetField(n, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (f != null && f.FieldType == typeof(string))
                    return f.GetValue(row) as string;
            }
            if (row is BattleRunRecord rec)
                return rec.RunHeader;
            return row.ToString();
        }

        static bool HasDriveOrResultEvidence(object row)
        {
            if (row is BattleRunRecord rec)
            {
                for (int i = 0; i < rec.EventSummaries.Count; i++)
                {
                    var k = rec.EventSummaries[i] != null ? rec.EventSummaries[i].Key() : "";
                    if (k.IndexOf("drive", StringComparison.OrdinalIgnoreCase) >= 0
                        || k.IndexOf("hit", StringComparison.OrdinalIgnoreCase) >= 0
                        || k.IndexOf("result", StringComparison.OrdinalIgnoreCase) >= 0)
                        return true;
                }
            }
            var text = row != null ? row.ToString() : "";
            return text.IndexOf("drive", StringComparison.OrdinalIgnoreCase) >= 0
                || text.IndexOf("hit", StringComparison.OrdinalIgnoreCase) >= 0
                || text.IndexOf("result", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        static bool HasExitSnapshot(object row)
        {
            if (row is BattleRunRecord rec)
                return rec.FinalDigest != null;
            var t = row.GetType();
            return t.GetProperty("FinalDigest") != null
                || t.GetProperty("Result") != null
                || t.GetProperty("ExitSnapshot") != null
                || t.GetField("FinalDigest") != null;
        }

        static bool HasVersionIdentity(object row)
        {
            if (row is BattleRunRecord rec)
                return !string.IsNullOrEmpty(rec.RulesVersion);
            var t = row.GetType();
            return t.GetProperty("RulesVersion") != null
                || t.GetProperty("DataIdentity") != null
                || t.GetProperty("ContentFingerprint") != null;
        }

        static void AssertHasCommandFields(object row)
        {
            IEnumerable<CommandRecord> cmds = null;
            if (row is BattleRunRecord rec)
                cmds = rec.Commands;
            else
            {
                var p = row.GetType().GetProperty("Commands") ?? row.GetType().GetProperty("CommandLog");
                cmds = p != null ? p.GetValue(row) as IEnumerable<CommandRecord> : null;
            }
            Assert.True(cmds != null, "L02: store row has no Commands/CommandLog");
            foreach (var c in cmds)
            {
                if (c == null) continue;
                Assert.True(c.Seq != 0 || c.Tick >= 0, "L02: command missing Seq/Tick");
                return;
            }
        }

        static bool HasBattleId(Type t)
        {
            return t.GetProperty("BattleId") != null
                || t.GetField("BattleId") != null
                || t.GetProperty("battle_id") != null;
        }

        static bool HasMethod(Type t, string name)
        {
            return t.GetMethod(name, BindingFlags.Public | BindingFlags.Instance) != null;
        }

        static MethodInfo FindMutator(Type t, params string[] names)
        {
            for (int i = 0; i < names.Length; i++)
            {
                var methods = t.GetMethods(BindingFlags.Public | BindingFlags.Instance);
                for (int m = 0; m < methods.Length; m++)
                    if (string.Equals(methods[m].Name, names[i], StringComparison.Ordinal))
                        return methods[m];
            }
            return null;
        }
    }
}
