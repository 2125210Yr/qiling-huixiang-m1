using System;

namespace Resonance.Battle
{
    public enum ExpeditionStatus { Ready, Battle, Reward, BossRetry, Failed }

    // This single document is the transaction boundary. It never embeds a legacy SaveBlob.
    public sealed class OriginalProfile
    {
        public int SchemaVersion = 1;
        public long Revision;
        public string ContentVersion = ExpeditionContent.Version;
        public string IntegritySha256;
        public string[] UnlockedPresets = { "single" };
        public string[] DiscoveredRelics = Array.Empty<string>();
        public string[] ClearedChapters = Array.Empty<string>();
        public ExpeditionState ActiveRun;
        public ExpeditionRunSummary LastRunSummary;
    }

    public sealed class ExpeditionState
    {
        public string RunId;
        public int RunSeed;
        public string RulesetId;
        public string FrozenContentHash;
        public string[] UnlockSnapshot;
        public string PresetId;
        public string InitialCoreId;
        public string CurrentNode;
        public ExpeditionStatus Status;
        public string[] VisitedNodeIds = Array.Empty<string>();
        public string[] SelectedChoices = Array.Empty<string>();
        public int[] PartyHp;
        public string[] OwnedRelicIds = Array.Empty<string>();
        public RewardOffer PendingOffer;
        public int BattleOrdinal;
        public ExpeditionBattleInput CurrentBattleCheckpoint;
        public ExpeditionBattleInput BossCheckpoint;
        public string[] SettledBattleIds = Array.Empty<string>();
        public bool ResultApplied;
        public string LastError;
    }

    public sealed class RewardOffer
    {
        public string Id;
        public string RewardId;
        public long Revision;
        public string[] CandidateIds;
        public string NextNode;
        public bool IsRecoveryOnly;
    }

    public sealed class ExpeditionRunSummary
    {
        public string RunId;
        public string LastEncounterId;
        public bool Victory;
        public bool ResultApplied;
        public bool UnlockedNewPreset;
        public string[] RelicIds;
        public string[] VisitedNodeIds;
        public int BattlesCompleted;
        public string EndReason;
        public string[] Facts = Array.Empty<string>();
    }
}
