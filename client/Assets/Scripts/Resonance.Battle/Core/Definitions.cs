namespace Resonance.Battle
{
    public sealed class CharacterDef
    {
        public string Id;
        public string Name;
        public Element Element;
        public Role Role;
        public int Hp;
        public int Atk;
        public int Def;
        public int Agl;
        public int Crt;
        public float ChargeTimeSec;
        public string AutoSkillId;
        public string TapSkillId;
        public string SlideSkillId;
        public string DriveSkillId;
        public string LeaderSkillId;
        public bool IsEnemy;
        public bool IsBoss;
        /// <summary>Field plate level number (GT left of enemy name). Not Save/Growth level.</summary>
        public int BattleLevel = 1;
        public int NativeStar = 5;
        public int MaxStar = 6;
        public int UncapMax = 6;
        public int IgnitionMax = 12;
    }

    public sealed class SkillDef
    {
        public string Id;
        public string Name;
        public SkillType Type;
        public TargetRule Target;
        public int TargetCount;
        public int HitCount;
        public float AtkCoef;
        public int FlatPower;
        public int DriveGain;
        public string EffectId;
        public float HealCoef;
        public int FlatHeal;
        public float HealMaxHpFrac;
        public float SkillFlat;
        public float PercentAtk;
        public bool IsIgnitedVariant;
        public string BaseSkillId;
        public string[] RequireTags;
        public string Opcode;
        /// <summary>
        /// SHOWTIME <c>RANK n</c> from external skill content. 0 = unknown.
        /// Not unit level: same fight shows RANK 1 (Robin r50) and RANK 7 (r62 / PVP5 t090) at LV 10/10.
        /// </summary>
        public int SlideRank;
        /// <summary>SHOWTIME skill LV current. 0 = unknown. Cap frames use 10.</summary>
        public int SlideSkillLv;
        /// <summary>SHOWTIME skill LV max. 0 = unknown. Frames use 10.</summary>
        public int SlideSkillLvMax;
    }

    public sealed class EffectDef
    {
        public const string TriggerOnAction = "on_action";
        public const string TriggerOnHitTaken = "on_hit_taken";
        public const string TriggerPeriodic = "periodic";

        public string Id;
        public string Opcode;
        public EffectKind Kind;
        /// <summary>
        /// When true, <see cref="Target"/> is this effect's picker.
        /// When false, Cast inherits <see cref="SkillDef.Target"/>.
        /// TargetRule.Self == 0, so absence cannot be encoded as default Target.
        /// </summary>
        public bool HasTarget;
        public TargetRule Target;
        /// <summary>FromRule = SideOf(resolved rule). Ally/Foe/Self force the pool (mixed builtins).</summary>
        public TargetSide Side;
        public float Magnitude;
        /// <summary>&lt;= 0 → lives until consumed (shield) or dispelled; never expires by time.</summary>
        public float DurationSec;
        public int MaxStack;
        public int SourceTier;
        public string Group;
        /// <summary>
        /// DoT trigger policy: <c>on_action</c> / <c>on_hit_taken</c> / <c>periodic</c>, joined by <c>|</c>.
        /// Empty → Poison defaults to <c>on_action|on_hit_taken</c>, Bleed to <c>on_hit_taken</c> (design, not GL).
        /// </summary>
        public string Trigger;
        /// <summary>Seconds between <c>periodic</c> ticks; required when Trigger contains <c>periodic</c>.</summary>
        public float PeriodSec;
    }

    public sealed class StageDef
    {
        public string Id;
        public string Name;
        public float TimeLimitSec;
        public string[] Wave0;
        public string[] Wave1;
        public float EnemyHpMul = 1f;
        public float EnemyAtkMul = 1f;
        public float EnemyDefMul = 1f;
        public int Difficulty;
        public string NeedStageId = "";
    }
}
