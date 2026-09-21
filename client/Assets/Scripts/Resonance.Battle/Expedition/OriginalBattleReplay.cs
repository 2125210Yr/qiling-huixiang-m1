using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;

namespace Resonance.Battle
{
    public enum OriginalReplayInputKind { Command, QueueTap, ClearActor, ClearQueue }

    public sealed class OriginalReplayInput
    {
        public int Tick;
        public OriginalReplayInputKind Kind;
        public BattleCommand Command;
        public int ActorSlot;
    }

    public sealed class OriginalQueueSnapshot
    {
        public int ActorSlot, RequiredEnemySlot, RequiredEnemyGeneration;
        public CommandResult Result;
    }

    /// <summary>Comparison-only output. No part of this state constructs a replay opening.</summary>
    public sealed class OriginalBattleSnapshot
    {
        public string CoreState;
        public EncounterSnapshot Encounter;
        public int BarrierEnergy, BarrierThreshold, HarmonyActorMask;
        public bool ForteStored, Paused, ForceNoCrit, HoldSim;
        public long RootActionId, LastCompletedRootActionId, TriggerSerial;
        public int LastRequestCount, FocusEnemySlot, PolicyHoldTicksLeft;
        public string LastTriggeredRelicId, FailedReason, PolicyHoldKind;
        public int[] UnitGenerations;
        public string[] UnitStateHashes;
        public OriginalQueueSnapshot[] Queue, QueueResults;
        public long[] RelicTriggerSerials;
        public float TimeLeft;

        public static OriginalBattleSnapshot Capture(BattleSim sim)
        {
            if (sim == null || !sim.IsOriginalExpedition) throw new ArgumentException("Original battle required.", nameof(sim));
            var relic = sim.ExpeditionRelics;
            var state = new OriginalBattleSnapshot
            {
                CoreState = BattleStateDigest.Of(sim).ToCanonicalString(), Encounter = sim.OriginalIntentSnapshot,
                BarrierEnergy = relic.BarrierEnergy, BarrierThreshold = relic.BarrierThreshold,
                HarmonyActorMask = relic.HarmonyActorMask, ForteStored = relic.ForteStored,
                RootActionId = sim.OriginalRootActionId, LastCompletedRootActionId = relic.LastCompletedRootActionId,
                TriggerSerial = relic.TriggerSerial, LastTriggeredRelicId = relic.LastTriggeredRelicId,
                LastRequestCount = relic.LastRequestCount, Paused = sim.Paused, FocusEnemySlot = sim.FocusEnemySlot,
                ForceNoCrit = sim.ForceNoCrit, TimeLeft = sim.TimeLeft, FailedReason = sim.FailedReason,
                HoldSim = sim.HoldSim, PolicyHoldTicksLeft = sim.PolicyHoldTicksLeft, PolicyHoldKind = sim.PolicyHoldKind,
                UnitGenerations = new int[sim.Allies.Length + sim.Enemies.Count],
                UnitStateHashes = new string[sim.Allies.Length + sim.Enemies.Count]
            };
            for (int i = 0; i < state.UnitGenerations.Length; i++)
            {
                var unit = i < sim.Allies.Length ? sim.Allies[i] : sim.Enemies[i - sim.Allies.Length];
                state.UnitGenerations[i] = unit?.InstanceGeneration ?? 0;
                // UnitState and StatusInst are public-field DTOs, including exact float clocks,
                // shield instance balances, source identity, current definition and reserve cursor.
                state.UnitStateHashes[i] = ExpeditionContent.Fingerprint(unit);
            }
            var queue = sim.ExpeditionQueue; var results = sim.ExpeditionQueueResults;
            state.Queue = new OriginalQueueSnapshot[queue.Count];
            for (int i = 0; i < queue.Count; i++) state.Queue[i] = new OriginalQueueSnapshot
            { ActorSlot = queue[i].ActorSlot, RequiredEnemySlot = queue[i].RequiredEnemySlot, RequiredEnemyGeneration = queue[i].RequiredEnemyGeneration };
            state.QueueResults = new OriginalQueueSnapshot[results.Count];
            for (int i = 0; i < results.Count; i++) state.QueueResults[i] = new OriginalQueueSnapshot
            { ActorSlot = results[i].ActorSlot, RequiredEnemySlot = results[i].RequiredEnemySlot, RequiredEnemyGeneration = results[i].RequiredEnemyGeneration, Result = results[i].Result };
            state.RelicTriggerSerials = new long[12];
            for (int family = 0; family < 3; family++)
                for (int rank = 1; rank <= 4; rank++)
                    state.RelicTriggerSerials[family * 4 + rank - 1] = relic.GetTriggerSerial(((char)('A' + family)).ToString() + "0" + rank);
            return state;
        }
        public string ToCanonicalString() => OriginalReplayJson.Write(this);
        public string Hash => ExpeditionContent.Fingerprint(this);
    }

    public sealed class OriginalBattleRecord
    {
        public int SchemaVersion = 2;
        public string Format = "original-expedition-replay-v2";
        public ExpeditionBattleInput Input;
        public string InputHash;
        /// <summary>Independent clock input, never recovered from expected final state.</summary>
        public int EndTick;
        public List<OriginalReplayInput> Inputs = new List<OriginalReplayInput>();
        public List<CommandRecord> Commands = new List<CommandRecord>();
        public OriginalBattleSnapshot FinalState;
        public List<BattleEvent> Events = new List<BattleEvent>();
        public List<ResolutionResult> Resolutions = new List<ResolutionResult>();
        public string EventHash, ResolutionHash;

        public static OriginalBattleRecord Capture(BattleSim sim)
        {
            if (sim == null || !sim.IsOriginalExpedition) throw new ArgumentException("Original battle required.", nameof(sim));
            var record = new OriginalBattleRecord
            {
                Input = sim.OpeningExpeditionInput, EndTick = sim.TickIndex,
                Inputs = sim.CopyOriginalReplayInputs(), Commands = OriginalReplayJson.Copy(sim.CommandLog),
                FinalState = OriginalBattleSnapshot.Capture(sim), Events = OriginalReplayJson.Copy(sim.Events.Events),
                Resolutions = new List<ResolutionResult>(sim.ExpeditionResolutions), EventHash = sim.Events.ComputeHash()
            };
            record.InputHash = ExpeditionContent.Fingerprint(record.Input);
            // ResolutionResult uses explicit DataMember properties, not public fields.
            record.ResolutionHash = BattleEventLog.HashUtf8(OriginalReplayJson.Write(record.Resolutions));
            return record;
        }
        public string ToJson() => OriginalReplayJson.Write(this);
        public static OriginalBattleRecord FromJson(string json) => OriginalReplayJson.Read<OriginalBattleRecord>(json);
    }

    public sealed class OriginalReplayReport
    {
        public bool Match => Differences.Count == 0;
        public BattleSim Replayed;
        public readonly List<string> Differences = new List<string>();
    }

    public static class OriginalBattleReplayer
    {
        public static OriginalReplayReport Verify(OriginalBattleRecord record)
        {
            var report = new OriginalReplayReport();
            try
            {
                Validate(record);
                var sim = RunBattleFactory.Create(record.Input);
                report.Replayed = sim;
                foreach (var input in record.Inputs)
                {
                    if (!AdvanceTo(sim, input.Tick, report)) return report;
                    switch (input.Kind)
                    {
                        case OriginalReplayInputKind.Command: sim.Submit(input.Command); break;
                        case OriginalReplayInputKind.QueueTap: sim.QueueExpeditionTap(input.ActorSlot); break;
                        case OriginalReplayInputKind.ClearActor: sim.ClearExpeditionQueuedCommand(input.ActorSlot); break;
                        case OriginalReplayInputKind.ClearQueue: sim.ClearExpeditionQueue(); break;
                        default: throw new ArgumentException("Unknown original replay input kind.");
                    }
                }
                if (!AdvanceTo(sim, record.EndTick, report)) return report;
                var actual = OriginalBattleRecord.Capture(sim);
                Compare(report, "FinalState", record.FinalState?.Hash, actual.FinalState.Hash);
                Compare(report, "Commands", ExpeditionContent.Fingerprint(record.Commands), ExpeditionContent.Fingerprint(actual.Commands));
                Compare(report, "Events", ExpeditionContent.Fingerprint(record.Events), ExpeditionContent.Fingerprint(actual.Events));
                Compare(report, "EventHash", record.EventHash, actual.EventHash);
                Compare(report, "Resolutions", OriginalReplayJson.Write(record.Resolutions), OriginalReplayJson.Write(actual.Resolutions));
                Compare(report, "ResolutionHash", record.ResolutionHash, actual.ResolutionHash);
            }
            catch (Exception error)
            {
                report.Differences.Add("Original replay rejected: " + error.GetType().Name + ": " + error.Message);
            }
            return report;
        }

        static void Validate(OriginalBattleRecord record)
        {
            if (record == null) throw new ArgumentNullException(nameof(record));
            if (record.SchemaVersion != 2 || record.Format != "original-expedition-replay-v2")
                throw new ArgumentException("Unsupported original replay format or schema version.");
            RunBattleFactory.Validate(record.Input);
            if (string.IsNullOrEmpty(record.InputHash) || record.InputHash != ExpeditionContent.Fingerprint(record.Input))
                throw new ArgumentException("Frozen input hash mismatch.");
            // Explicit engineering bounds, independent of expected output. Authored encounters last <= 1h.
            if (record.EndTick < 0 || record.EndTick > 1000000 || record.Inputs == null || record.Inputs.Count > 100000)
                throw new ArgumentException("Invalid independent replay boundary or input budget.");
            int previous = 0;
            foreach (var input in record.Inputs)
            {
                if (input == null || input.Tick < previous || input.Tick > record.EndTick ||
                    !Enum.IsDefined(typeof(OriginalReplayInputKind), input.Kind))
                    throw new ArgumentException("Replay input is null, out of order, or outside the independent boundary.");
                if (input.Kind == OriginalReplayInputKind.Command &&
                    (!Enum.IsDefined(typeof(CommandSource), input.Command.Source) || input.Command.Source == CommandSource.Auto))
                    throw new ArgumentException("Automatic commands cannot be external replay intentions.");
                previous = input.Tick;
            }
        }

        static bool AdvanceTo(BattleSim sim, int tick, OriginalReplayReport report)
        {
            while (sim.TickIndex < tick)
            {
                int before = sim.TickIndex;
                sim.Tick();
                if (sim.TickIndex <= before)
                {
                    report.Differences.Add("Independent replay boundary unreachable at tick " + before + "; requested " + tick + ".");
                    return false;
                }
            }
            return true;
        }

        static void Compare(OriginalReplayReport report, string field, string expected, string actual)
        {
            if (!string.Equals(expected, actual, StringComparison.Ordinal)) report.Differences.Add(field + " mismatch.");
        }
    }

    internal static class OriginalReplayJson
    {
        public static string Write<T>(T value)
        {
            using (var stream = new MemoryStream())
            {
                new DataContractJsonSerializer(typeof(T)).WriteObject(stream, value);
                return Encoding.UTF8.GetString(stream.ToArray());
            }
        }
        public static T Read<T>(string json)
        {
            if (json == null) throw new ArgumentNullException(nameof(json));
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(json)))
                return (T)new DataContractJsonSerializer(typeof(T)).ReadObject(stream);
        }
        public static T Copy<T>(T value) => Read<T>(Write(value));
    }
}
