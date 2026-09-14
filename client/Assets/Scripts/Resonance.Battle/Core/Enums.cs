namespace Resonance.Battle
{
    public enum Element
    {
        Fire = 0,
        Water = 1,
        Wood = 2,
        Light = 3,
        Dark = 4
    }

    public enum Role
    {
        Attacker = 0,
        Defender = 1,
        Debuffer = 2,
        Healer = 3,
        Supporter = 4
    }

    public enum SkillType
    {
        Auto = 0,
        Tap = 1,
        Slide = 2,
        Drive = 3,
        Leader = 4,
        Fever = 5
    }

    public enum DriveTiming
    {
        Bad = 0,
        Good = 1,
        Great = 2,
        Perfect = 3
    }

    public enum AutoMode
    {
        Manual = 0,
        Semi = 1,
        Full = 2
    }

    public enum TargetRule
    {
        Self = 0,
        LowestHpAlly = 1,
        AllAllies = 2,
        RandomEnemies = 3,
        LowestHpEnemies = 4,
        HighestAtkEnemies = 5,
        AllEnemies = 6,
        LowestHpRatioAlly = 7,
        LowestHpRatioEnemies = 8
    }

    /// <summary>
    /// Pool for an effect. FromRule = derive from the resolved TargetRule.
    /// Self=0 on TargetRule is a real picker, so this enum is the unset/override channel.
    /// </summary>
    public enum TargetSide
    {
        FromRule = 0,
        Ally = 1,
        Foe = 2,
        Self = 3
    }

    public enum EffectKind
    {
        Damage = 0,
        Heal = 1,
        Dot = 2,
        Shield = 3,
        AtkBuff = 4,
        DefDebuff = 5,
        ChargeHaste = 6,
        Taunt = 7,
        TsAmp = 8,
        SsAmp = 9,
        DsAmp = 10,
        SkillDefDown = 11,
        WeakDefDown = 12,
        Reflect = 13,
        Immortal = 14,
        Silence = 15,
        Stun = 16,
        Freeze = 17,
        Bleed = 18,
        Poison = 19,
        Burn = 20,
        AntiHeal = 21,
        ChargeAmount = 22,
        ChargeSpeed = 23,
        CooldownDelta = 24,
        Barrier = 25,
        DebuffBarrier = 26,
        Enrage = 27,
        Overload = 28,
        DualWield = 29,
        /// <summary>DEF up. Primary Robin mid-fight shows DEF ↑. Appended — do not renumber prior kinds.</summary>
        DefBuff = 30
    }
}
