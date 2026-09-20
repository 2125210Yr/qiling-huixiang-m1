using System;

namespace Resonance.Battle
{
    /// <summary>Public-field DTO for the persisted, independently replayable battle opening.</summary>
    public sealed class ExpeditionBattleInput
    {
        public int SchemaVersion = 1;
        public string ModeId = "original-expedition";
        public string ContentVersion;
        public string ContentHash;
        public string RulesetId;
        public string RunId;
        public string EncounterId;
        public int BattleOrdinal;
        public string AttemptId;
        public int Seed;
        public string PresetId;
        public StageDef Stage;
        public CharacterDef[] Characters;
        public SkillDef[] Skills;
        public EffectDef[] Effects;
        public string[] PartyIds;
        public int[] OpeningHp;
        public UnitProgress[] Growth;
        public BattleMods Mods;
        public BattleClockPolicy Clocks;
        public FormulaProfile Profile;
        public string[] RelicIds;
        public ExpeditionRelicParameters RelicParameters;
        public bool IsBoss;
        public BossEncounterDef Boss;
        public EliteEncounterDef Elite;
        public bool EnableDrive = false;
        public bool EnableFever = false;

        public ExpeditionBattleInput DeepClone()
        {
            var copy = (ExpeditionBattleInput)MemberwiseClone();
            copy.Stage = CloneStage(Stage);
            copy.Characters = CloneArray(Characters, GrowthCharacter);
            copy.Skills = CloneArray(Skills, Catalog.CloneSkill);
            copy.Effects = CloneArray(Effects, CloneEffect);
            copy.PartyIds = PartyIds == null ? null : (string[])PartyIds.Clone();
            copy.OpeningHp = OpeningHp == null ? null : (int[])OpeningHp.Clone();
            copy.Growth = OpeningGrowth.Copy(Growth);
            copy.Mods = OpeningGrowth.CopyMods(Mods);
            copy.Clocks = CloneClocks(Clocks);
            copy.RelicIds = RelicIds == null ? null : (string[])RelicIds.Clone();
            copy.RelicParameters = RelicParameters == null ? null : RelicParameters.DeepClone();
            copy.Boss = Boss == null ? null : Boss.DeepClone();
            copy.Elite = Elite == null ? null : Elite.DeepClone();
            return copy;
        }

        static CharacterDef GrowthCharacter(CharacterDef src) => src == null ? null : Resonance.Battle.Growth.Clone(src);

        static T[] CloneArray<T>(T[] src, Func<T, T> clone)
        {
            if (src == null) return null;
            var result = new T[src.Length];
            for (int i = 0; i < src.Length; i++) result[i] = clone(src[i]);
            return result;
        }

        public static StageDef CloneStage(StageDef src)
        {
            if (src == null) return null;
            return new StageDef { Id = src.Id, Name = src.Name, TimeLimitSec = src.TimeLimitSec,
                Wave0 = src.Wave0 == null ? null : (string[])src.Wave0.Clone(),
                Wave1 = src.Wave1 == null ? null : (string[])src.Wave1.Clone(),
                EnemyHpMul = src.EnemyHpMul, EnemyAtkMul = src.EnemyAtkMul, EnemyDefMul = src.EnemyDefMul,
                Difficulty = src.Difficulty, NeedStageId = src.NeedStageId };
        }

        public static EffectDef CloneEffect(EffectDef src)
        {
            if (src == null) return null;
            return new EffectDef { Id = src.Id, Opcode = src.Opcode, Kind = src.Kind, HasTarget = src.HasTarget,
                Target = src.Target, Side = src.Side, Magnitude = src.Magnitude, DurationSec = src.DurationSec,
                MaxStack = src.MaxStack, SourceTier = src.SourceTier, Group = src.Group,
                Trigger = src.Trigger, PeriodSec = src.PeriodSec };
        }

        public static BattleClockPolicy CloneClocks(BattleClockPolicy src)
        {
            if (src == null) return null;
            return new BattleClockPolicy { SlideCdSec = src.SlideCdSec, FeverWindowSec = src.FeverWindowSec,
                FeverHitBudget = src.FeverHitBudget, DriveQteTimeoutSec = src.DriveQteTimeoutSec,
                HoldTimeoutSec = src.HoldTimeoutSec, SlideShowtimeHoldSec = src.SlideShowtimeHoldSec,
                DriveCastHoldSec = src.DriveCastHoldSec, WaveAdvanceHoldSec = src.WaveAdvanceHoldSec,
                StageCountdownScalesWithSpeed = src.StageCountdownScalesWithSpeed,
                ChargeScalesWithSpeed = src.ChargeScalesWithSpeed, SlideCdScalesWithSpeed = src.SlideCdScalesWithSpeed,
                StatusDurationScalesWithSpeed = src.StatusDurationScalesWithSpeed,
                AutoIntervalScalesWithSpeed = src.AutoIntervalScalesWithSpeed,
                FeverWindowScalesWithSpeed = src.FeverWindowScalesWithSpeed,
                DriveQteScalesWithSpeed = src.DriveQteScalesWithSpeed,
                HoldWatchdogScalesWithSpeed = src.HoldWatchdogScalesWithSpeed,
                FeverMinHitIntervalSec = src.FeverMinHitIntervalSec, FeverAutoTapsPerSec = src.FeverAutoTapsPerSec,
                NumericStatus = src.NumericStatus, NumericCode = src.NumericCode };
        }
    }

    public sealed class BossEncounterDef
    {
        public string Version = "white-conductor-v1";
        public int BossSlot = 0;
        public int[] MaskSlots = { 1, 2 };
        public string MaskCharacterId = "OE_MASK";
        public string AutoSkillId = "OE_BOSS_auto";
        public string AreaSkillId = "OE_BOSS_echo";
        public float FirstIntentSec = 9f;
        public float CastDurationSec = 4f;
        public float PhaseOneIntervalSec = 10f;
        public float PhaseTwoIntervalSec = 8f;
        public float AutoIntervalSec = 3f;
        public float PhaseTwoHpFraction = 0.5f;
        public float DamagePerLivingMask = 0.5f;

        public BossEncounterDef DeepClone()
        {
            var result = (BossEncounterDef)MemberwiseClone();
            result.MaskSlots = MaskSlots == null ? null : (int[])MaskSlots.Clone();
            return result;
        }
    }

    /// <summary>N4's authored warning and attack clocks, frozen with the battle opening.</summary>
    public sealed class EliteEncounterDef
    {
        public string Version = "prompter-v1";
        public int CasterSlot = 0;
        public string AutoSkillId = "OE_PROMPTER_auto";
        public string AreaSkillId = "OE_PROMPTER_tap";
        public float FirstIntentSec = 6f;
        public float CastDurationSec = 3f;
        public float IntervalSec = 12f;
        public float AutoIntervalSec = 3f;

        public EliteEncounterDef DeepClone() => (EliteEncounterDef)MemberwiseClone();
    }

    /// <summary>DESIGN_CANDIDATE_V1; resolved parameters travel with checkpoint and tape.</summary>
    public sealed class ExpeditionRelicParameters
    {
        public float BarrierThresholdFraction = 0.20f;
        public int BarrierStoredThresholds = 3;
        public float ShieldBonus = 0.25f;
        public float ShockRatio = 1f;
        public float ShockSplashRatio = 0.5f;
        public int ShockSplashTargets = 2;
        public int OverloadThresholds = 3;
        public float OverloadRatio = 2f;
        public float ScatterRatio = 0.4f;
        public float ImprovedScatterRatio = 0.7f;
        public int ScatterTargets = 1;
        public int ImprovedScatterTargets = 2;
        public float KillEchoRatio = 1f;
        public float RelayCharge = 12f;
        public float ImprovedRelayCharge = 20f;
        public int HarmonyDistinctActors = 3;
        public float HarmonyCharge = 10f;
        public float ForteDamageBonus = 1.2f;
        public ExpeditionRelicParameters DeepClone() => (ExpeditionRelicParameters)MemberwiseClone();
    }
}
