using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using NUnit.Framework;
using Resonance.Battle;

namespace Resonance.EditorTests
{
    /// <summary>
    /// Unity-generated controlled boss tapes for independent cross-runtime verification.
    /// Frozen factory inputs plus legal Submit/Tick only; no ordinary UI or reward-acquisition claim.
    /// </summary>
    public sealed class OriginalReplayPortabilityTests
    {
        [TestCase("A")]
        [TestCase("B")]
        [TestCase("C")]
        public void CompleteFamilyBossVictory_RoundTripsAndExportsUnityTapeWhenRequested(string family)
        {
            var input = Input(family);
            var openingHash = ExpeditionContent.Fingerprint(input);
            var sim = RunBattleFactory.Create(input);
            var harmonyEvents = 0;
            var forteConsumptions = 0;
            PlayLegalStrategy(sim, family, ref harmonyEvents, ref forteConsumptions);

            Assert.That(sim.Outcome, Is.EqualTo(BattleOutcome.Victory));
            Assert.That(sim.ForceNoCrit, Is.False);
            Assert.That(sim.CommandLog.All(c => c.Accepted), Is.True);
            Assert.That(sim.Log.Any(c => c.Crit), Is.True, "Keep the authored seeded critical-hit behavior.");
            AssertFamilyTriggered(family, sim);
            if (family == "C")
            {
                Assert.That(harmonyEvents, Is.GreaterThan(0));
                Assert.That(forteConsumptions, Is.GreaterThan(0));
            }
            Assert.That(ExpeditionContent.Fingerprint(input), Is.EqualTo(openingHash));
            Assert.That(ExpeditionContent.Fingerprint(sim.OpeningExpeditionInput), Is.EqualTo(openingHash));

            var json = OriginalBattleRecord.Capture(sim).ToJson();
            var parsed = OriginalBattleRecord.FromJson(json);
            Assert.That(parsed.SchemaVersion, Is.EqualTo(2));
            Assert.That(parsed.Format, Is.EqualTo("original-expedition-replay-v2"));
            Assert.That(parsed.Input.ContentVersion, Is.EqualTo("original-expedition-content-v0.1.1"));
            Assert.That(parsed.Input.ContentHash, Is.EqualTo(ExpeditionContent.ContentHash));
            Assert.That(parsed.InputHash, Is.EqualTo(openingHash));
            Assert.That(parsed.Input.RelicIds, Is.EqualTo(input.RelicIds));
            Assert.That(parsed.Input.OpeningHp, Is.EqualTo(input.OpeningHp));

            var report = OriginalBattleReplayer.Verify(parsed);
            Assert.That(report.Match, Is.True, string.Join(" | ", report.Differences));
            Assert.That(report.Replayed, Is.Not.Null);
            Assert.That(report.Replayed.Outcome, Is.EqualTo(BattleOutcome.Victory));
            Assert.That(report.Replayed.ForceNoCrit, Is.False);
            AssertFamilyTriggered(family, report.Replayed);
            Assert.That(report.Replayed.TickIndex, Is.EqualTo(parsed.EndTick));
            Assert.That(report.Replayed.Events.ComputeHash(), Is.EqualTo(parsed.EventHash));
            Assert.That(report.Replayed.Log.Select(c => c.Crit), Is.EqualTo(sim.Log.Select(c => c.Crit)));
            var replayed = OriginalBattleRecord.Capture(report.Replayed);
            Assert.That(replayed.ResolutionHash, Is.EqualTo(parsed.ResolutionHash));
            Assert.That(replayed.FinalState.Hash, Is.EqualTo(parsed.FinalState.Hash));
            Assert.That(replayed.FinalState.RelicTriggerSerials, Is.EqualTo(parsed.FinalState.RelicTriggerSerials));

            TestContext.WriteLine(
                "scope=Unity-controlled-logic-fixture-not-ordinary-play-or-UI-video; unity={0}; family={1}; " +
                "outcome={2}; endTick={3}; seed={4}; relics={5}; contentVersion={6}; contentHash={7}; " +
                "inputHash={8}; eventHash={9}; resolutionHash={10}; harmonyEvents={11}; forteConsumptions={12}",
                UnityEngine.Application.unityVersion, family, sim.Outcome, parsed.EndTick, input.Seed,
                string.Join(",", input.RelicIds), parsed.Input.ContentVersion, parsed.Input.ContentHash,
                parsed.InputHash, parsed.EventHash, parsed.ResolutionHash, harmonyEvents, forteConsumptions);
            ExportIfRequested(family, json, sim);
        }

        static ExpeditionBattleInput Input(string family)
        {
            // Preserve the existing validated family strategy: B includes auxiliary C01 within the route budget.
            var relics = family == "A" ? new[] { "A01", "A02", "A03", "A04" }
                : family == "B" ? new[] { "B01", "B02", "B03", "B04", "C01" }
                : new[] { "C01", "C02", "C03", "C04" };
            var input = RunBattleFactory.CreateInput("N7", "single", 260921, relics, null);
            input.RunId = "unity-family-replay-fixture-" + family;
            input.EncounterId = input.RunId + "/N7/5";
            input.BattleOrdinal = 5;
            input.AttemptId = "unity-family-" + family + "-attempt-1";
            return input;
        }

        static void AssertFamilyTriggered(string family, BattleSim sim)
        {
            if (family == "A")
            {
                Assert.That(sim.ExpeditionTotals.AllyShieldAbsorbed, Is.GreaterThan(0));
                Assert.That(sim.ExpeditionRelics.GetTriggerSerial("A01"), Is.GreaterThan(0));
                Assert.That(sim.ExpeditionResolutions.Any(r => r.Origin == ResolutionOrigin.Derived
                    && r.SourceRelicId == "A04" && !r.TargetAlly && r.EffectiveHpDamage > 0), Is.True);
            }
            else if (family == "B")
            {
                Assert.That(sim.ExpeditionResolutions.Any(r => r.Origin == ResolutionOrigin.Derived
                    && r.SourceRelicId == "B01" && !r.TargetAlly && r.EffectiveHpDamage > 0), Is.True);
                Assert.That(sim.ExpeditionRelics.GetTriggerSerial("B02"), Is.GreaterThan(0));
                Assert.That(sim.ExpeditionRelics.GetTriggerSerial("B03"), Is.GreaterThan(0));
            }
            else
            {
                Assert.That(sim.ExpeditionRelics.GetTriggerSerial("C03"), Is.GreaterThan(0));
                Assert.That(sim.ExpeditionRelics.GetTriggerSerial("C04"), Is.GreaterThan(0));
            }
        }

        static void PlayLegalStrategy(BattleSim sim, string family, ref int harmonyEvents, ref int forteConsumptions)
        {
            var priority = new[] { 1, 2, 4, 0, 3 };
            for (var tick = 0; tick < 300 * BattleSim.TickHz && sim.Outcome == BattleOutcome.InProgress; tick++)
            {
                var target = family == "A" ? sim.Enemies[0]
                    : sim.Enemies.Where(u => u.Alive && u.Slot != 0).OrderBy(u => u.Hp).ThenBy(u => u.Slot).FirstOrDefault()
                        ?? sim.Enemies[0];
                if (target.Alive && sim.FocusEnemySlot != target.Slot)
                    Assert.That(sim.Submit(BattleCommand.FocusEnemy(target.Slot, CommandSource.Player)).Accepted, Is.True);
                var intent = sim.OriginalIntentSnapshot;
                foreach (var slot in priority)
                {
                    if (!sim.CanAct(slot)) continue;
                    if (slot == 1 && (intent == null || !intent.IsCasting || intent.RemainingCastSec > 0.2f)) continue;
                    if (slot == 2 && !sim.Allies.Any(u => u.Alive && u.Hp <= u.MaxHp * 0.8f)) continue;
                    var beforeHarmony = sim.ExpeditionRelics.GetTriggerSerial("C03");
                    var beforeForte = sim.ExpeditionRelics.GetTriggerSerial("C04");
                    var beforeResults = sim.ExpeditionResolutions.Count;
                    Assert.That(sim.Submit(BattleCommand.Tap(slot, CommandSource.Player)).Accepted, Is.True, sim.FailedReason);
                    if (sim.ExpeditionRelics.GetTriggerSerial("C03") > beforeHarmony) harmonyEvents++;
                    if (sim.ExpeditionRelics.GetTriggerSerial("C04") > beforeForte)
                    {
                        forteConsumptions++;
                        Assert.That(sim.ExpeditionResolutions.Skip(beforeResults).Any(r => r.Origin == ResolutionOrigin.Native
                            && r.SourceAlly && r.SourceSlot == slot && !r.TargetAlly && r.EffectiveDamage > 0), Is.True);
                    }
                    if (sim.Outcome != BattleOutcome.InProgress) break;
                }
                if (sim.Outcome == BattleOutcome.InProgress) sim.Tick();
            }
        }

        static void ExportIfRequested(string family, string json, BattleSim sim)
        {
            var directory = Environment.GetEnvironmentVariable("ORIGINAL_REPLAY_UNITY_DIRECTORY");
            if (string.IsNullOrWhiteSpace(directory)) return;
            Assert.That(Path.IsPathRooted(directory), Is.True, "Unity tape directory must be absolute.");
            var fullDirectory = Path.GetFullPath(directory);
            Assert.That(Path.GetPathRoot(directory).Replace('\\', '/'),
                Is.EqualTo(Path.GetPathRoot(fullDirectory).Replace('\\', '/')).IgnoreCase,
                "Drive-relative and current-drive-rooted paths are not fully absolute.");
            Directory.CreateDirectory(fullDirectory);
            var path = Path.Combine(fullDirectory, "fixture-" + family.ToLowerInvariant() + "-boss.original-replay.json");
            var statePath = Path.Combine(fullDirectory, "fixture-" + family.ToLowerInvariant() + "-state-bits.json");
            var stateJson = OriginalReplayStateBits.Capture(sim);
            WriteNew(path, json);
            WriteNew(statePath, stateJson);
        }

        static void WriteNew(string path, string json)
        {
            var bytes = new UTF8Encoding(false, true).GetBytes(json);
            using (var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                stream.Write(bytes, 0, bytes.Length);
                stream.Flush(true);
            }
            TestContext.WriteLine("scope=Unity-controlled-logic-fixture-not-ordinary-play-or-UI-video; artifact={0}; bytes={1}; sha256={2}",
                path, bytes.Length, BattleEventLog.HashUtf8(json));
        }
    }

    /// <summary>
    /// Test-only diagnostic shared by copying this class into a .NET probe. It reads public fields,
    /// never properties or battle commands. JSON rows have path/type/value; IEEE bits never use float formatting.
    /// </summary>
    public static class OriginalReplayStateBits
    {
        public static string Capture(BattleSim sim)
        {
            if (sim == null) throw new ArgumentNullException(nameof(sim));
            var json = new StringBuilder("[\n");
            var first = true;
            Visit(json, ref first, "Allies", sim.Allies, sim.Allies.GetType(), 0);
            Visit(json, ref first, "Enemies", sim.Enemies, sim.Enemies.GetType(), 0);
            return json.Append("\n]\n").ToString();
        }

        static void Visit(StringBuilder json, ref bool first, string path, object value, Type declaredType, int depth)
        {
            if (depth > 64) throw new InvalidOperationException("Diagnostic field nesting exceeded 64 at " + path);
            var type = value == null ? declaredType : value.GetType();
            var name = StableTypeName(type);
            if (value == null) { Row(json, ref first, path, name, null); return; }
            if (type == typeof(float))
            {
                Row(json, ref first, path, name,
                    BitConverter.ToUInt32(BitConverter.GetBytes((float)value), 0).ToString("x8", CultureInfo.InvariantCulture));
                return;
            }
            if (type == typeof(double))
            {
                Row(json, ref first, path, name,
                    BitConverter.ToUInt64(BitConverter.GetBytes((double)value), 0).ToString("x16", CultureInfo.InvariantCulture));
                return;
            }
            if (type.IsEnum)
            {
                var numeric = Convert.ChangeType(value, Enum.GetUnderlyingType(type), CultureInfo.InvariantCulture);
                Row(json, ref first, path, name, Convert.ToString(numeric, CultureInfo.InvariantCulture));
                return;
            }
            if (type == typeof(bool)) { Row(json, ref first, path, name, (bool)value ? "true" : "false"); return; }
            if (type.IsPrimitive || type == typeof(string) || type == typeof(decimal))
            {
                Row(json, ref first, path, name, Convert.ToString(value, CultureInfo.InvariantCulture));
                return;
            }
            var list = value as IList;
            if (list != null)
            {
                Row(json, ref first, path, name, "count:" + list.Count.ToString(CultureInfo.InvariantCulture));
                var elementType = type.IsArray ? type.GetElementType()
                    : type.IsGenericType ? type.GetGenericArguments()[0] : typeof(object);
                for (var i = 0; i < list.Count; i++)
                    Visit(json, ref first, path + "[" + i.ToString(CultureInfo.InvariantCulture) + "]", list[i], elementType, depth + 1);
                return;
            }
            Row(json, ref first, path, name, "object");
            foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public).OrderBy(f => f.Name, StringComparer.Ordinal))
                Visit(json, ref first, path + "." + field.Name, field.GetValue(value), field.FieldType, depth + 1);
        }

        static string StableTypeName(Type type)
        {
            if (type.IsArray) return StableTypeName(type.GetElementType()) + "[]";
            if (!type.IsGenericType) return type.FullName ?? type.Name;
            var name = type.GetGenericTypeDefinition().FullName;
            return name.Substring(0, name.IndexOf('`')) + "<" + string.Join(",", type.GetGenericArguments().Select(StableTypeName)) + ">";
        }

        static void Row(StringBuilder json, ref bool first, string path, string type, string value)
        {
            if (!first) json.Append(",\n");
            first = false;
            json.Append("  {\"path\":"); Quote(json, path);
            json.Append(",\"type\":"); Quote(json, type);
            json.Append(",\"value\":"); Quote(json, value);
            json.Append('}');
        }

        static void Quote(StringBuilder json, string value)
        {
            if (value == null) { json.Append("null"); return; }
            json.Append('"');
            foreach (var c in value)
            {
                if (c == '"') json.Append("\\\"");
                else if (c == '\\') json.Append("\\\\");
                else if (c < 0x20) json.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                else json.Append(c);
            }
            json.Append('"');
        }
    }
}
