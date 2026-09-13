using System;
using System.Collections.Generic;

namespace Resonance.Battle
{
    public enum BattleOutcome
    {
        InProgress = 0,
        Victory = 1,
        Defeat = 2,
        Failed = 3
    }

    public sealed class StatusInst
    {
        public EffectDef Def;
        public float Remaining;
        public int Stacks = 1;
    }

    public sealed class UnitState
    {
        public CharacterDef Def;
        public int Slot;
        public bool Ally;
        public int MaxHp;
        public int Hp;
        public int Shield;
        public float Charge;
        public float SlideCd;
        public float AutoTimer;
        public int ReserveCursor;
        public readonly List<StatusInst> Status = new List<StatusInst>();
        public bool Alive => Hp > 0;

        public int Atk => Scaled(Def.Atk, EffectKind.AtkBuff);
        public int Defense => DefenseAgainst(Def.Element);

        public int DefenseAgainst(Element atkEl)
        {
            var v = Def.Def;
            // Primary Robin mid-fight: DEF ↑. Buff then DefDebuff (not GL_FINAL).
            var buff = Magnitude(EffectKind.DefBuff);
            var deb = Magnitude(EffectKind.DefDebuff);
            return (int)Math.Round(v * (1f + buff) * (1f - deb));
        }

        public float ChargeSpeedMul => 1f + Magnitude(EffectKind.ChargeHaste);

        int Scaled(int baseV, EffectKind kind) => (int)Math.Round(baseV * (1f + Magnitude(kind)));

        public float Magnitude(EffectKind kind)
        {
            float m = 0f;
            for (int i = 0; i < Status.Count; i++)
            {
                if (Status[i].Def.Kind == kind) m += Status[i].Def.Magnitude * Status[i].Stacks;
            }
            return m;
        }

        public bool Has(EffectKind kind)
        {
            for (int i = 0; i < Status.Count; i++)
                if (Status[i].Def.Kind == kind) return true;
            return false;
        }

        public bool ActionLocked => Has(EffectKind.Stun) || Has(EffectKind.Freeze);

        public int ExtraAtk;
        public float IgnCrtAdd;
        public float IgnAglAdd;
        public bool DeathLogged;
    }

    public sealed class FloatText
    {
        public int UnitSlot;
        public bool Ally;
        public int CasterSlot = -1;
        public bool CasterAlly;
        public string Text;
        public bool Crit;
        public bool Heal;
        public bool Fever;
        public SkillType Kind;
    }

    public sealed class CastFx
    {
        public int CasterSlot;
        public bool CasterAlly;
        public SkillType Type;
        public string Name;
        public bool Fever;
        /// <summary>SHOWTIME RANK from the skill being cast. 0 = unknown.</summary>
        public int SlideRank;
        /// <summary>SHOWTIME skill LV current. 0 = unknown.</summary>
        public int SlideSkillLv;
        /// <summary>SHOWTIME skill LV max. 0 = unknown.</summary>
        public int SlideSkillLvMax;
    }

    public sealed class BattleMods
    {
        public float FoodAtkMul = 1f;
        public float CartaMul = 1f;
    }

    /// <summary>
    /// Per-clock durations and speed coupling. GL seconds stay UNKNOWN;
    /// defaults are design placeholders, not measured truth.
    /// </summary>
    public sealed class BattleClockPolicy
    {
        public float SlideCdSec;
        public float FeverWindowSec;
        public int FeverHitBudget;
        public float DriveQteTimeoutSec;
        public float HoldTimeoutSec;
        public bool StageCountdownScalesWithSpeed = true;
        public bool ChargeScalesWithSpeed = true;
        public bool SlideCdScalesWithSpeed = true;
        public bool StatusDurationScalesWithSpeed = true;
        public bool AutoIntervalScalesWithSpeed = true;
        public bool FeverWindowScalesWithSpeed = true;
        public bool DriveQteScalesWithSpeed = true;
        public bool HoldWatchdogScalesWithSpeed = true;
        public FormulaStatus NumericStatus = FormulaStatus.NotMeasured;
        public string NumericCode = DamageMath.DesignPlaceholderCode;

        public BattleClockPolicy()
        {
            SlideCdSec = BattleSim.UnknownSlideCdSec;
            FeverWindowSec = BattleSim.UnknownFeverWindowSec;
            FeverHitBudget = BattleSim.UnknownFeverHitBudget;
            DriveQteTimeoutSec = BattleSim.DriveQteTimeoutSec;
            HoldTimeoutSec = BattleSim.HoldTimeoutSec;
        }

        public static BattleClockPolicy DesignPlaceholder()
        {
            return new BattleClockPolicy();
        }

        public float BattleDt(int speed)
        {
            if (speed < 1) return BattleSim.TickDt;
            return BattleSim.TickDt * speed;
        }

        public float Scale(float battleDt, bool scalesWithSpeed)
        {
            if (scalesWithSpeed && battleDt > 0f) return battleDt;
            return BattleSim.TickDt;
        }
    }

    public sealed class BattleSim
    {
        public const int TickHz = 30;
        public const float TickDt = 1f / TickHz;
        /// <summary>
        /// Primary P0 tutorial portrait shows <c>DRIVE TIME</c> countdown at 7 (t365).
        /// Prior 1.2s was a placeholder — still not GL_FINAL_VERIFIED.
        /// </summary>
        public const float DriveQteTimeoutSec = 7f;
        public const float HoldTimeoutSec = 2f;
        public const float UnknownSlideCdSec = 8f;
        /// <summary>
        /// Primary P0 tip (~t435): "For 14 seconds tap…". Prior 7s was KR/compat placeholder — not GL_FINAL.
        /// </summary>
        public const float UnknownFeverWindowSec = 14f;
        public const int UnknownFeverHitBudget = 70;

        public readonly UnitState[] Allies;
        public readonly List<UnitState> Enemies = new List<UnitState>(8);
        public int FocusEnemySlot { get; private set; } = -1;
        public int WaveIndex;
        public float Drive;
        public float FeverGauge;
        public bool FeverActive;
        public bool FeverEver;
        public float FeverLeft;
        public int FeverHitsLeft;
        public float TimeLeft;
        public int Speed = 1;
        public bool Paused;
        bool _holdSim;
        bool _holdSticky;
        public bool HoldSim
        {
            get => _holdSim;
            set
            {
                _holdSim = value;
                if (!value)
                {
                    _holdSticky = false;
                    _holdElapsed = 0f;
                }
            }
        }
        public bool HoldSticky => _holdSticky;
        public AutoMode Auto;
        public bool AutoTap
        {
            get => Auto != AutoMode.Manual;
            set => Auto = value ? AutoMode.Full : AutoMode.Manual;
        }
        public bool Deterministic = true;
        public BattleOutcome Outcome = BattleOutcome.InProgress;
        public int PendingDriveSlot = -1;
        public readonly List<FloatText> Log = new List<FloatText>(64);
        public readonly List<CastFx> Casts = new List<CastFx>(64);
        public readonly BattleEventLog Events = new BattleEventLog();
        public readonly FightStats Stats;
        public string LastEvent = "";
        public string FailedReason = "";
        public int LeaderSlot;
        public int TickIndex;
        public FormulaProfile Profile = FormulaProfile.GL_UNKNOWN;
        public BattleClockPolicy Clocks = BattleClockPolicy.DesignPlaceholder();
        public float SlideCdDurationSec
        {
            get => Clocks != null ? Clocks.SlideCdSec : UnknownSlideCdSec;
            set { if (Clocks != null) Clocks.SlideCdSec = value; }
        }
        readonly Random _rng;
        readonly StageDef _stage;
        readonly UnitProgress[] _growth;
        float _feverAcc;
        float _qteElapsed;
        float _holdElapsed;
        SkillType _activeKind;
        bool _feverHit;
        bool _poisonResolving;
        Dictionary<string, SkillDef> _skillOverlay;
        SkillDef _execSkill;
        UnitState _execCaster;
        bool _execCasterAlly;
        float _execMul = 1f;
        UnitState _execTarget;
        EffectDef _execFx;
        bool _execFever;

        public bool Settled => Outcome != BattleOutcome.InProgress && !FeverActive;

        public void OverlaySkill(string id, SkillDef skill)
        {
            if (string.IsNullOrEmpty(id) || skill == null) return;
            if (_skillOverlay == null) _skillOverlay = new Dictionary<string, SkillDef>();
            _skillOverlay[id] = skill;
        }

        SkillDef ResolveSkill(string id)
        {
            if (_skillOverlay != null && !string.IsNullOrEmpty(id)
                && _skillOverlay.TryGetValue(id, out var over) && over != null)
                return over;
            return Catalog.TrySkill(id);
        }

        public BattleSim(string[] partyIds, int leaderSlot, int seed)
            : this(partyIds, leaderSlot, seed, null, null) { }

        public BattleSim(string[] partyIds, int leaderSlot, int seed, StageDef stage, UnitProgress[] growth, BattleMods mods = null)
        {
            _rng = new Random(seed);
            _stage = stage ?? Catalog.VerticalSliceStage;
            _growth = growth;
            TimeLeft = _stage.TimeLimitSec;
            LeaderSlot = leaderSlot;
            if (mods == null) mods = new BattleMods();
            var partyN = partyIds != null ? partyIds.Length : 0;
            Stats = new FightStats(partyN);
            Allies = new UnitState[partyN];
            for (int i = 0; i < partyN; i++)
            {
                var id = partyIds[i];
                var src = Catalog.TryChar(id);
                if (src == null) continue;
                var p = growth != null && i < growth.Length ? growth[i] : null;
                var grown = Growth.Apply(src, p);
                if (mods.FoodAtkMul > 1f)
                    grown.Atk = Math.Max(1, (int)Math.Round(grown.Atk * mods.FoodAtkMul));
                if (mods.CartaMul > 1f && p != null && !string.IsNullOrEmpty(p.Gear3))
                {
                    var carta = GearCatalog.Try(p.Gear3);
                    if (carta != null)
                        grown.Atk += (int)Math.Round(carta.Atk * (mods.CartaMul - 1f));
                }
                var unit = Spawn(grown, i, true);
                if (p != null)
                {
                    var bonus = Ignition.Of(p.IgnAtk, p.IgnCrt, p.IgnAgl);
                    unit.ExtraAtk = bonus.ExtraAtk(0, grown.Atk);
                    unit.IgnCrtAdd = bonus.Crt;
                    unit.IgnAglAdd = bonus.Agl;
                }
                Allies[i] = unit;
            }
            LoadWave(0);
            ApplyLeader();
        }

        public void Tick()
        {
            if (Outcome != BattleOutcome.InProgress || Paused) return;
            TickIndex++;
            var dt = Clocks != null ? Clocks.BattleDt(Speed) : TickDt * (Speed < 1 ? 1 : Speed);
            if (dt <= 0f) dt = TickDt;
            if (!ReleaseBlocks(dt)) return;

            var stageDt = ScaleClock(dt, Clocks != null && Clocks.StageCountdownScalesWithSpeed);
            TimeLeft -= stageDt;
            Stats.Tick(stageDt);
            if (TimeLeft <= 0f)
            {
                TimeLeft = 0f;
                if (Outcome == BattleOutcome.InProgress)
                {
                    Outcome = BattleOutcome.Defeat;
                    LastEvent = "时间耗尽";
                    NoteResult("timeout");
                }
                return;
            }

            TickStatus(ScaleClock(dt, Clocks != null && Clocks.StatusDurationScalesWithSpeed));
            TickSlideClocks(ScaleClock(dt, Clocks != null && Clocks.SlideCdScalesWithSpeed));
            if (FeverActive)
                TickFever(ScaleClock(dt, Clocks != null && Clocks.FeverWindowScalesWithSpeed));

            var chargeDt = ScaleClock(dt, Clocks != null && Clocks.ChargeScalesWithSpeed);
            var autoDt = ScaleClock(dt, Clocks != null && Clocks.AutoIntervalScalesWithSpeed);
            for (int i = 0; i < Allies.Length; i++)
                TickUnit(Allies[i], true, chargeDt, autoDt);
            for (int i = 0; i < Enemies.Count; i++)
                TickUnit(Enemies[i], false, chargeDt, autoDt);

            AutoFireDrive();
            AutoFireSkills();

            if (!AnyAllyAlive()) return;
            CheckWave();
        }

        public void TickFeverOnly()
        {
            if (!FeverActive || Paused) return;
            var dt = Clocks != null ? Clocks.BattleDt(Speed) : TickDt * (Speed < 1 ? 1 : Speed);
            if (dt <= 0f) dt = TickDt;
            TickFever(ScaleClock(dt, Clocks != null && Clocks.FeverWindowScalesWithSpeed));
        }

        public void StayHeld()
        {
            _holdSim = true;
            _holdSticky = true;
            _holdElapsed = 0f;
        }

        float ScaleClock(float battleDt, bool scalesWithSpeed)
        {
            if (Clocks != null) return Clocks.Scale(battleDt, scalesWithSpeed);
            return battleDt > 0f ? battleDt : TickDt;
        }

        bool ReleaseBlocks(float dt)
        {
            if (_holdSim)
            {
                if (_holdSticky)
                    return false;
                var holdDt = ScaleClock(dt, Clocks != null && Clocks.HoldWatchdogScalesWithSpeed);
                _holdElapsed += holdDt;
                var holdLimit = Clocks != null ? Clocks.HoldTimeoutSec : HoldTimeoutSec;
                if (_holdElapsed < holdLimit)
                    return false;
                HoldSim = false;
            }
            else
                _holdElapsed = 0f;

            if (PendingDriveSlot >= 0)
            {
                if (Auto == AutoMode.Full)
                    ResolveDrive(DriveTiming.Great);
                else
                {
                    var qteDt = ScaleClock(dt, Clocks != null && Clocks.DriveQteScalesWithSpeed);
                    _qteElapsed += qteDt;
                    TimeLeft -= qteDt;
                    Stats.Tick(qteDt);
                    if (TimeLeft <= 0f)
                    {
                        TimeLeft = 0f;
                        if (Outcome == BattleOutcome.InProgress)
                        {
                            Outcome = BattleOutcome.Defeat;
                            LastEvent = "时间耗尽";
                            NoteResult("timeout");
                        }
                        return false;
                    }
                    var qteLimit = Clocks != null ? Clocks.DriveQteTimeoutSec : DriveQteTimeoutSec;
                    if (_qteElapsed < qteLimit)
                        return false;
                    ResolveDrive(DriveTiming.Good);
                }
            }

            return Outcome == BattleOutcome.InProgress;
        }

        void AutoFireDrive()
        {
            if (Auto != AutoMode.Full || Drive < 100f) return;
            for (int i = 0; i < Allies.Length; i++)
            {
                if (!TryBeginDrive(i)) continue;
                if (PendingDriveSlot >= 0)
                    ResolveDrive(DriveTiming.Great);
                break;
            }
        }

        void AutoFireSkills()
        {
            if (Auto == AutoMode.Manual) return;
            for (int i = 0; i < Allies.Length; i++)
            {
                if (Allies[i] == null || !Allies[i].Alive || Allies[i].Charge < 100f) continue;
                if (NextAutoType(i) == SkillType.Slide && Allies[i].SlideCd <= 0f) TrySlide(i);
                else TryTap(i);
            }
        }

        public bool TryTap(int slot) => UsePlayerSkill(slot, SkillType.Tap);
        public bool TrySlide(int slot) => UsePlayerSkill(slot, SkillType.Slide);

        public bool TryFocusEnemy(int slot)
        {
            if (Outcome != BattleOutcome.InProgress) return false;
            if (slot < 0 || slot >= Enemies.Count) return false;
            var u = Enemies[slot];
            if (u == null || !u.Alive) return false;
            FocusEnemySlot = slot;
            return true;
        }

        void ClearDeadFocus()
        {
            if (FocusEnemySlot < 0) return;
            if (FocusEnemySlot >= Enemies.Count
                || Enemies[FocusEnemySlot] == null
                || !Enemies[FocusEnemySlot].Alive)
                FocusEnemySlot = -1;
        }

        public bool CanSlide(int slot)
        {
            if (!CanAct(slot)) return false;
            var u = Allies[slot];
            return u != null && u.SlideCd <= 0f;
        }

        public bool TryPortraitTap(int slot)
        {
            if (Drive >= 100f && TryBeginDrive(slot)) return true;
            return TryTap(slot);
        }

        public static float QteMul(DriveTiming t) => TimingDamage(t);
        /// <summary>Fever gauge % from Drive QTE. Primary P0 tip (~t445): Perfect 40 / Great 30 / Good 15.</summary>
        public static float QteFever(DriveTiming t) => TimingFever(t);

        static float ExtraDmg(UnitState caster, UnitState target, SkillType type)
        {
            if (caster == null || target == null) return 0f;
            return DamageMath.ExtraDmg(
                type,
                DamageMath.Beats(caster.Def.Element, target.Def.Element),
                caster.Magnitude(EffectKind.TsAmp),
                caster.Magnitude(EffectKind.SsAmp),
                caster.Magnitude(EffectKind.DsAmp),
                target.Magnitude(EffectKind.SkillDefDown),
                target.Magnitude(EffectKind.WeakDefDown));
        }

        static float IgnitionExtraMul(UnitState caster, UnitState target, bool crit)
        {
            if (caster == null || (caster.IgnCrtAdd == 0f && caster.IgnAglAdd == 0f)) return 1f;
            var add = new IgnitionBonus { Crt = caster.IgnCrtAdd, Agl = caster.IgnAglAdd }
                .ExtraDmgAdd(crit, target != null && DamageMath.Beats(caster.Def.Element, target.Def.Element));
            return 1f + add;
        }

        public bool TryBeginDrive(int slot)
        {
            if (Outcome != BattleOutcome.InProgress || Paused || PendingDriveSlot >= 0) return false;
            if (Drive < 100f) return false;
            if (slot < 0 || slot >= Allies.Length || Allies[slot] == null || !Allies[slot].Alive || Allies[slot].ActionLocked) return false;
            PendingDriveSlot = slot;
            _qteElapsed = 0f;
            LastEvent = Allies[slot].Def.Name + " 准备 Drive";
            if (Auto == AutoMode.Full)
                return ResolveDrive(DriveTiming.Great);
            return true;
        }

        public bool ResolveDrive(DriveTiming timing)
        {
            if (PendingDriveSlot < 0) return false;
            var slot = PendingDriveSlot;
            PendingDriveSlot = -1;
            _qteElapsed = 0f;
            Drive = 0f;
            var mul = TimingDamage(timing);
            AddFever(TimingFever(timing));
            var unit = Allies[slot];
            var skill = unit != null && unit.Def != null ? ResolveSkill(unit.Def.DriveSkillId) : null;
            if (unit == null || skill == null) return false;
            unit.Charge = 0f;
            Cast(unit, true, skill, mul);
            LastEvent = "DRIVE  " + unit.Def.Name + "  " + skill.Name + "  " + timing;
            return true;
        }

        public bool CanAct(int slot)
        {
            return Outcome == BattleOutcome.InProgress
                && !Paused
                && PendingDriveSlot < 0
                && slot >= 0 && slot < Allies.Length
                && Allies[slot] != null
                && Allies[slot].Alive
                && !Allies[slot].ActionLocked
                && Allies[slot].Charge >= 100f;
        }

        void TickSlideClocks(float dt)
        {
            for (int i = 0; i < Allies.Length; i++)
                TickSlideCd(Allies[i], dt);
            for (int i = 0; i < Enemies.Count; i++)
                TickSlideCd(Enemies[i], dt);
        }

        static void TickSlideCd(UnitState u, float dt)
        {
            if (u == null || !u.Alive || u.SlideCd <= 0f) return;
            u.SlideCd -= dt;
            if (u.SlideCd < 0f) u.SlideCd = 0f;
        }

        void TickUnit(UnitState u, bool ally, float chargeDt, float autoDt)
        {
            if (u == null || u.Def == null || !u.Alive) return;
            if (u.ActionLocked) return;
            var chargePerSec = (100f / Math.Max(0.01f, u.Def.ChargeTimeSec)) * u.ChargeSpeedMul;
            u.Charge += chargePerSec * chargeDt;
            if (u.Charge > 100f) u.Charge = 100f;

            u.AutoTimer += autoDt;
            var autoCd = Math.Max(1.1f, 2.4f - u.Def.Agl / 2000f);
            if (u.AutoTimer >= autoCd)
            {
                u.AutoTimer = 0f;
                var autoSkill = ResolveSkill(u.Def.AutoSkillId);
                if (autoSkill != null)
                {
                    Cast(u, ally, autoSkill, 1f);
                    if (ally)
                        Drive = Math.Min(100f, Drive + Math.Max(14f, autoSkill.DriveGain));
                }
            }

            if (!ally && u.Charge >= 100f)
            {
                var wantSlide = _rng.NextDouble() >= 0.65 && u.SlideCd <= 0f;
                var skill = ResolveSkill(wantSlide ? u.Def.SlideSkillId : u.Def.TapSkillId);
                u.Charge = 0f;
                if (wantSlide) u.SlideCd = SlideCdDurationSec;
                if (skill != null)
                    Cast(u, false, skill, 1f);
            }
        }

        void TickFever(float dt)
        {
            FeverLeft -= dt;
            var step = TickDt > 0f ? dt / TickDt : 1f;
            if (step < 0f) step = 0f;
            _feverAcc += step;
            var ticksPerHit = Math.Max(1, TickHz / 10);
            while (_feverAcc >= ticksPerHit && FeverHitsLeft > 0)
            {
                _feverAcc -= ticksPerHit;
                FeverHitsLeft--;
                var caster = FirstAlive(Allies);
                var target = PickEnemy(TargetRule.LowestHpEnemies);
                if (caster != null && caster.Def != null && target != null)
                {
                    _feverHit = true;
                    _activeKind = SkillType.Fever;
                    _execFever = true;
                    _execCaster = caster;
                    _execTarget = target;
                    try
                    {
                        ExecuteOpcode(EffectOpcodes.DmgFeverParts);
                    }
                    finally
                    {
                        _execFever = false;
                        _execCaster = null;
                        _execTarget = null;
                        _feverHit = false;
                    }
                }
            }
            if (FeverLeft <= 0f || FeverHitsLeft <= 0)
            {
                FeverActive = false;
                FeverLeft = 0f;
                FeverHitsLeft = 0;
                LastEvent = "Fever 结束";
            }
        }

        void TickStatus(float dt)
        {
            TickList(Allies, dt);
            for (int i = 0; i < Enemies.Count; i++) TickOne(Enemies[i], dt);
        }

        void TickList(UnitState[] list, float dt)
        {
            for (int i = 0; i < list.Length; i++) TickOne(list[i], dt);
        }

        void TickOne(UnitState u, float dt)
        {
            if (u == null || !u.Alive) return;
            for (int i = u.Status.Count - 1; i >= 0; i--)
            {
                var st = u.Status[i];
                st.Remaining -= dt;
                if (st.Remaining <= 0f) u.Status.RemoveAt(i);
            }
        }

        bool UsePlayerSkill(int slot, SkillType type)
        {
            if (type == SkillType.Slide)
            {
                if (!CanSlide(slot)) return false;
            }
            else if (!CanAct(slot)) return false;
            var u = Allies[slot];
            if (u == null || u.Def == null) return false;
            var id = type == SkillType.Slide ? u.Def.SlideSkillId : u.Def.TapSkillId;
            var skill = ResolveSkill(id);
            if (skill == null) return false;
            u.Charge = 0f;
            if (type == SkillType.Slide) u.SlideCd = SlideCdDurationSec;
            Drive = Math.Min(100f, Drive + skill.DriveGain);
            Cast(u, true, skill, 1f);
            LastEvent = VisualTag(skill.Type) + "  " + u.Def.Name + "  " + skill.Name;
            return true;
        }

        public void ExecuteOpcode(string opcode)
        {
            if (string.IsNullOrEmpty(opcode) || !EffectOpcodes.IsKnown(opcode) || !EffectOpcodes.IsImplemented(opcode))
            {
                FailUnknownOpcode(opcode);
                throw new UnknownOpcodeException(opcode);
            }
            NoteEvent("opcode", opcode, _execCaster, _execTarget, 0, _activeKind);
            if (_execFever)
                SettleFeverHit();
            else if (_execSkill != null)
                SettleSkill(opcode);
            else if (_execFx != null)
                SettleEffect(opcode);
        }

        void FailUnknownOpcode(string opcode)
        {
            var msg = "UNKNOWN_OPCODE " + (string.IsNullOrEmpty(opcode) ? "<empty>" : opcode);
            Outcome = BattleOutcome.Failed;
            FailedReason = msg;
            LastEvent = msg;
            NoteEvent("fail", opcode, null, null, 0, _activeKind);
        }

        void NoteEvent(string kind, string opcode, UnitState caster, UnitState target, int amount, SkillType channel)
        {
            var status = Profile == FormulaProfile.GL_UNKNOWN ? FormulaStatus.NotMeasured : FormulaStatus.Ok;
            Events.Add(TickIndex, Outcome == BattleOutcome.InProgress ? "resolve" : "end", kind, opcode, caster, target, amount, channel, Profile, status);
        }

        void NoteResult(string opcode)
        {
            NoteEvent("result", opcode, null, null, (int)Outcome, SkillType.Auto);
        }

        void NoteDeathOnce(UnitState t)
        {
            if (t == null || t.Alive || t.DeathLogged) return;
            t.DeathLogged = true;
            NoteEvent("death", "", null, t, 0, _activeKind);
        }

        void SweepDeaths()
        {
            for (int i = 0; i < Allies.Length; i++)
                NoteDeathOnce(Allies[i]);
            for (int i = 0; i < Enemies.Count; i++)
                NoteDeathOnce(Enemies[i]);
        }

        static string VisualTag(SkillType t)
        {
            if (t == SkillType.Slide) return "SLIDE";
            if (t == SkillType.Drive) return "DRIVE";
            if (t == SkillType.Auto) return "AUTO";
            return "TAP";
        }

        SkillType NextAutoType(int slot)
        {
            var p = _growth != null && slot >= 0 && slot < _growth.Length ? _growth[slot] : null;
            var r = Growth.NormalizedReserve(p == null ? "" : p.Reserve);
            if (Growth.ReserveAllEmpty(r)) return SkillType.Tap;
            var u = slot >= 0 && slot < Allies.Length ? Allies[slot] : null;
            if (u == null) return SkillType.Tap;
            for (int k = 0; k < 5; k++)
            {
                var i = (u.ReserveCursor + k) % 5;
                if (r[i] == 'S')
                {
                    u.ReserveCursor = (i + 1) % 5;
                    return SkillType.Slide;
                }
                if (r[i] == 'T')
                {
                    u.ReserveCursor = (i + 1) % 5;
                    return SkillType.Tap;
                }
            }
            return SkillType.Tap;
        }

        void Cast(UnitState caster, bool casterAlly, SkillDef skill, float dmgMul)
        {
            if (skill == null)
            {
                FailUnknownOpcode("");
                throw new UnknownOpcodeException("");
            }
            _activeKind = skill.Type;
            _execSkill = skill;
            _execCaster = caster;
            _execCasterAlly = casterAlly;
            _execMul = dmgMul;
            try
            {
                ExecuteOpcode(skill.Opcode);
            }
            finally
            {
                _execSkill = null;
                _execCaster = null;
                _execCasterAlly = false;
                _execMul = 1f;
            }
            TriggerPoisonOnAction(caster);
            NoteEvent("cast", DamageMath.ChannelOpcode(skill.Type), caster, null, 0, skill.Type);
            Casts.Add(new CastFx
            {
                CasterSlot = caster.Slot,
                CasterAlly = casterAlly,
                Type = skill.Type,
                Name = skill.Name,
                SlideRank = skill.SlideRank,
                SlideSkillLv = skill.SlideSkillLv,
                SlideSkillLvMax = skill.SlideSkillLvMax
            });

            var fx = Catalog.TryEffect(skill.EffectId);
            if (fx != null)
            {
                IEnumerable<UnitState> fxTargets = fx.Kind == EffectKind.AtkBuff
                    || fx.Kind == EffectKind.DefBuff
                    || fx.Kind == EffectKind.Shield
                    || fx.Kind == EffectKind.ChargeHaste
                    || fx.Kind == EffectKind.Taunt
                    ? PickAllies(fx.Kind == EffectKind.Taunt ? TargetRule.Self : skill.Target, skill.TargetCount, casterAlly, caster)
                    : PickFoes(casterAlly, skill.Target, skill.TargetCount, caster);
                if (fx.Kind == EffectKind.Taunt)
                    fxTargets = new[] { caster };
                foreach (var t in fxTargets)
                    ApplyEffect(t, fx);
            }
        }

        void SettleSkill(string opcode)
        {
            var skill = _execSkill;
            var caster = _execCaster;
            if (skill == null || caster == null) return;
            if (skill.HealCoef > 0f || skill.FlatHeal > 0 || skill.HealMaxHpFrac > 0f)
            {
                foreach (var t in PickAllies(skill.Target, skill.TargetCount, _execCasterAlly, caster))
                    Heal(caster, t, skill);
                return;
            }
            if (!EffectOpcodes.IsDamageChannel(opcode)) return;
            if (skill.AtkCoef <= 0f && skill.FlatPower <= 0 && skill.PercentAtk <= 0f) return;
            var hits = Math.Max(1, skill.HitCount);
            var feverMul = skill.Type == SkillType.Slide || skill.Type == SkillType.Drive
                ? 1f
                : (FeverActive ? DamageMath.FeverMul : 1f);
            for (int h = 0; h < hits; h++)
            {
                foreach (var t in PickFoes(_execCasterAlly, skill.Target, skill.TargetCount, caster))
                {
                    var crit = !Deterministic && _rng.NextDouble() < DamageMath.CritChance(caster.Def.Crt);
                    if (Deterministic) crit = false;
                    var extraMul = DamageMath.ExtraDmgMul(ExtraDmg(caster, t, skill.Type)) * _execMul;
                    extraMul *= IgnitionExtraMul(caster, t, crit);
                    if (TryResolveCombat(
                        skill.Type, caster, t, skill.AtkCoef, skill.FlatPower,
                        crit, extraMul, feverMul, skill.PercentAtk, skill.SkillFlat, out var dmg))
                        ApplyDamage(caster, t, dmg, crit);
                }
            }
        }

        void SettleFeverHit()
        {
            var caster = _execCaster;
            var target = _execTarget;
            if (caster == null || target == null) return;
            var extraMul = DamageMath.ExtraDmgMul(ExtraDmg(caster, target, SkillType.Fever));
            extraMul *= IgnitionExtraMul(caster, target, false);
            if (TryResolveCombat(
                SkillType.Fever, caster, target,
                DamageMath.FeverChannelAtkCoef, DamageMath.FeverChannelFlat,
                false, extraMul, DamageMath.FeverMul, 0f, 0f, out var dmg))
                ApplyDamage(caster, target, dmg, false);
        }

        bool TryResolveCombat(
            SkillType type,
            UnitState caster,
            UnitState t,
            float coef,
            int flat,
            bool crit,
            float extraMul,
            float feverMul,
            float percentAtk,
            float skillFlat,
            out int dmg)
        {
            dmg = 0;
            var extraAtk = caster != null ? caster.ExtraAtk : 0;
            var atk = caster != null ? caster.Atk : 0;
            var atkEl = caster != null && caster.Def != null ? caster.Def.Element : Element.Fire;
            var defEl = t != null && t.Def != null ? t.Def.Element : Element.Fire;
            var defense = t != null ? t.DefenseAgainst(atkEl) : 0;
            var result = DamageMath.Resolve(
                Profile, type, atk, coef, flat, defense, atkEl, defEl,
                crit, extraMul, 1f, feverMul, percentAtk, skillFlat, extraAtk: extraAtk);
            if (!result.Measured)
            {
                NoteEvent("unresolved", DamageMath.ChannelOpcode(type), caster, t, 0, type);
                return false;
            }
            dmg = result.RequireInt();
            return true;
        }

        void Heal(UnitState caster, UnitState t, SkillDef skill)
        {
            if (!t.Alive) return;
            if (Profile != FormulaProfile.JP_LEGACY_EMPIRICAL && Profile != FormulaProfile.KR_LEGACY_REPORTED)
            {
                NoteEvent("unresolved", DamageMath.ChannelOpcode(_activeKind), caster, t, 0, _activeKind);
                return;
            }
            var amt = (int)Math.Round(caster.Atk * skill.HealCoef + skill.FlatHeal + t.MaxHp * skill.HealMaxHpFrac);
            if (amt < 1) amt = 1;
            t.Hp = Math.Min(t.MaxHp, t.Hp + amt);
            Stats.NoteHeal(caster, t, amt);
            Log.Add(new FloatText
            {
                UnitSlot = t.Slot,
                Ally = t.Ally,
                CasterSlot = caster != null ? caster.Slot : -1,
                CasterAlly = caster != null && caster.Ally,
                Text = "+" + amt,
                Heal = true,
                Kind = _activeKind
            });
        }

        void ApplyDamage(UnitState caster, UnitState t, int dmg, bool crit)
        {
            if (t == null || !t.Alive) return;
            if (t.Shield > 0)
            {
                var absorb = Math.Min(t.Shield, dmg);
                t.Shield -= absorb;
                dmg -= absorb;
            }
            if (dmg <= 0) return;
            t.Hp -= dmg;
            if (t.Hp < 0) t.Hp = 0;
            NoteDeathOnce(t);
            if (!t.Alive) ClearDeadFocus();
            if (caster != null) StretchStun(t);
            Stats.NoteDamage(caster, t, dmg);
            NoteEvent("hit", DamageMath.ChannelOpcode(_activeKind), caster, t, dmg, _activeKind);
            if (!_poisonResolving)
                TriggerPoisonOnHitTaken(t);
            Log.Add(new FloatText
            {
                UnitSlot = t.Slot,
                Ally = t.Ally,
                CasterSlot = caster != null ? caster.Slot : -1,
                CasterAlly = caster != null && caster.Ally,
                Text = dmg.ToString(),
                Crit = crit,
                Fever = _feverHit,
                Kind = _activeKind
            });
        }

        void TriggerPoisonOnAction(UnitState actor)
        {
            ApplyPoisonTriggers(actor, "on_action");
        }

        void TriggerPoisonOnHitTaken(UnitState victim)
        {
            ApplyPoisonTriggers(victim, "on_hit_taken");
        }

        void ApplyPoisonTriggers(UnitState u, string trigger)
        {
            if (u == null || !u.Alive || _poisonResolving) return;
            _poisonResolving = true;
            try
            {
                for (int i = 0; i < u.Status.Count; i++)
                {
                    var st = u.Status[i];
                    if (st == null || st.Def == null || !EffectOpcodes.IsDotTriggerKind(st.Def.Kind)) continue;
                    if (Profile != FormulaProfile.JP_LEGACY_EMPIRICAL
                        && Profile != FormulaProfile.KR_LEGACY_REPORTED)
                    {
                        NoteEvent("unresolved", EffectOpcodes.PoisonApply, u, u, 0, _activeKind);
                        continue;
                    }
                    var tick = Math.Max(1, (int)Math.Round(u.MaxHp * st.Def.Magnitude * st.Stacks));
                    ApplyDamage(null, u, tick, false);
                    NoteEvent(trigger, EffectOpcodes.PoisonApply, u, u, tick, _activeKind);
                }
            }
            finally
            {
                _poisonResolving = false;
            }
        }

        public void ApplyStatus(UnitState t, EffectDef fx) => ApplyEffect(t, fx);

        void ApplyEffect(UnitState t, EffectDef fx)
        {
            if (t == null || !t.Alive || fx == null) return;
            _execTarget = t;
            _execFx = fx;
            try
            {
                ExecuteOpcode(fx.Opcode);
            }
            finally
            {
                _execTarget = null;
                _execFx = null;
            }
        }

        void SettleEffect(string opcode)
        {
            var t = _execTarget;
            var fx = _execFx;
            if (t == null || !t.Alive || fx == null) return;
            if (string.Equals(opcode, EffectOpcodes.PoisonApply, StringComparison.Ordinal)
                && !EffectOpcodes.IsDotTriggerKind(fx.Kind))
            {
                fx.Kind = EffectKind.Poison;
            }
            if (string.Equals(opcode, EffectOpcodes.ChargeAdd, StringComparison.Ordinal))
            {
                t.Charge += fx.Magnitude;
                if (t.Charge > 100f) t.Charge = 100f;
                if (t.Charge < 0f) t.Charge = 0f;
                LogFx(t, fx);
                return;
            }
            if (string.Equals(opcode, EffectOpcodes.SlideCd, StringComparison.Ordinal))
            {
                t.SlideCd = fx.DurationSec > 0f ? fx.DurationSec : fx.Magnitude;
                if (t.SlideCd < 0f) t.SlideCd = 0f;
                LogFx(t, fx);
                return;
            }
            for (int i = 0; i < t.Status.Count; i++)
            {
                var cur = t.Status[i];
                if (cur.Def.Group == fx.Group)
                {
                    if (fx.SourceTier >= cur.Def.SourceTier)
                    {
                        cur.Def = fx;
                        cur.Remaining = fx.DurationSec;
                        ApplyControlAndShield(t, fx, opcode);
                        LogFx(t, fx);
                    }
                    return;
                }
            }
            t.Status.Add(new StatusInst { Def = fx, Remaining = fx.DurationSec });
            ApplyControlAndShield(t, fx, opcode);
            LogFx(t, fx);
        }

        void ApplyControlAndShield(UnitState t, EffectDef fx, string opcode)
        {
            if (t == null || fx == null) return;
            var shield = string.Equals(opcode, EffectOpcodes.ShieldApply, StringComparison.Ordinal)
                || fx.Kind == EffectKind.Shield
                || fx.Kind == EffectKind.Barrier;
            if (shield)
                t.Shield = Math.Max(t.Shield, (int)Math.Round(t.MaxHp * fx.Magnitude));
            var lockCharge = string.Equals(opcode, EffectOpcodes.ControlApply, StringComparison.Ordinal)
                && (fx.Kind == EffectKind.Stun || fx.Kind == EffectKind.Freeze);
            if (lockCharge || fx.Kind == EffectKind.Stun || fx.Kind == EffectKind.Freeze)
                t.Charge = 0f;
        }

        void LogFx(UnitState t, EffectDef fx)
        {
            var id = fx.Id;
            if (string.IsNullOrEmpty(id)) id = fx.Kind.ToString();
            Log.Add(new FloatText
            {
                UnitSlot = t.Slot,
                Ally = t.Ally,
                Text = "FX " + id,
                Kind = _activeKind
            });
        }

        static void StretchStun(UnitState t)
        {
            for (int i = 0; i < t.Status.Count; i++)
            {
                if (t.Status[i].Def.Kind != EffectKind.Stun) continue;
                t.Status[i].Remaining += 1f;
                if (t.Status[i].Remaining > 5f) t.Status[i].Remaining = 5f;
            }
        }

        IEnumerable<UnitState> PickFoes(bool casterAlly, TargetRule rule, int count, UnitState caster)
        {
            if (casterAlly) return PickEnemies(rule, count, caster);
            return PickFrom(Allies, rule, count, preferTaunt: true, caster);
        }

        IEnumerable<UnitState> PickAllies(TargetRule rule, int count, bool casterAlly, UnitState caster)
        {
            return casterAlly ? PickFrom(Allies, rule, count, false, caster) : PickEnemies(rule, count, caster);
        }

        List<UnitState> PickEnemies(TargetRule rule, int count, UnitState caster = null)
        {
            ClearDeadFocus();
            var buf = new List<UnitState>(8);
            if (rule != TargetRule.AllEnemies && count <= 1 && FocusEnemySlot >= 0
                && FocusEnemySlot < Enemies.Count)
            {
                var focus = Enemies[FocusEnemySlot];
                if (focus != null && focus.Alive)
                {
                    buf.Add(focus);
                    return Select(buf, rule, count, false, caster);
                }
            }
            for (int i = 0; i < Enemies.Count; i++)
                if (Enemies[i] != null && Enemies[i].Alive) buf.Add(Enemies[i]);
            return Select(buf, rule, count, false, caster);
        }

        List<UnitState> PickFrom(UnitState[] src, TargetRule rule, int count, bool preferTaunt, UnitState caster)
        {
            var buf = new List<UnitState>(src != null ? src.Length : 0);
            if (preferTaunt)
            {
                for (int i = 0; i < src.Length; i++)
                    if (src[i] != null && src[i].Alive && src[i].Has(EffectKind.Taunt)) buf.Add(src[i]);
            }
            if (buf.Count == 0)
            {
                for (int i = 0; i < src.Length; i++)
                    if (src[i] != null && src[i].Alive) buf.Add(src[i]);
            }
            return Select(buf, rule, count, true, caster);
        }

        List<UnitState> Select(List<UnitState> alive, TargetRule rule, int count, bool allySide, UnitState caster)
        {
            var result = new List<UnitState>(count);
            if (rule == TargetRule.Self)
            {
                if (caster != null && caster.Alive)
                    result.Add(caster);
                return result;
            }
            if (alive.Count == 0) return result;
            if (rule == TargetRule.AllAllies || rule == TargetRule.AllEnemies)
                return alive;
            count = Math.Min(count, alive.Count);
            if (rule == TargetRule.LowestHpAlly || rule == TargetRule.LowestHpEnemies)
            {
                alive.Sort(CompareAbsHpThenSlot);
                for (int i = 0; i < count; i++) result.Add(alive[i]);
                return result;
            }
            if (rule == TargetRule.LowestHpRatioAlly || rule == TargetRule.LowestHpRatioEnemies)
            {
                alive.Sort(CompareHpRatioThenSlot);
                for (int i = 0; i < count; i++) result.Add(alive[i]);
                return result;
            }
            if (rule == TargetRule.HighestAtkEnemies)
            {
                alive.Sort(CompareHighestAtkThenSlot);
                for (int i = 0; i < count; i++) result.Add(alive[i]);
                return result;
            }
            for (int i = 0; i < count; i++)
                result.Add(alive[_rng.Next(alive.Count)]);
            return result;
        }

        static int CompareAbsHpThenSlot(UnitState a, UnitState b)
        {
            var hp = a.Hp.CompareTo(b.Hp);
            return hp != 0 ? hp : a.Slot.CompareTo(b.Slot);
        }

        // hp_ratio = Hp / MaxHp（契约 target_keys）；交叉相乘只比大小，不是 GL 式。
        static int CompareHpRatioThenSlot(UnitState a, UnitState b)
        {
            var aMax = a.MaxHp;
            var bMax = b.MaxHp;
            if (aMax <= 0 && bMax <= 0) return a.Slot.CompareTo(b.Slot);
            if (aMax <= 0) return 1;
            if (bMax <= 0) return -1;
            var lhs = (long)a.Hp * bMax;
            var rhs = (long)b.Hp * aMax;
            var cmp = lhs.CompareTo(rhs);
            return cmp != 0 ? cmp : a.Slot.CompareTo(b.Slot);
        }

        static int CompareHighestAtkThenSlot(UnitState a, UnitState b)
        {
            var atk = b.Atk.CompareTo(a.Atk);
            return atk != 0 ? atk : a.Slot.CompareTo(b.Slot);
        }

        UnitState PickEnemy(TargetRule rule)
        {
            var list = PickEnemies(rule, 1);
            return list.Count > 0 ? list[0] : null;
        }

        static UnitState FirstAlive(UnitState[] list)
        {
            if (list == null) return null;
            for (int i = 0; i < list.Length; i++)
                if (list[i] != null && list[i].Alive) return list[i];
            return null;
        }

        static float TimingDamage(DriveTiming t)
        {
            switch (t)
            {
                case DriveTiming.Bad: return 0.90f;
                case DriveTiming.Great: return 1.20f;
                case DriveTiming.Perfect: return 1.50f;
                default: return 1.00f;
            }
        }

        static float TimingFever(DriveTiming t)
        {
            // Primary GT P0 tip frame (~t445): PERFECT +40 / GREAT +30 / GOOD +15.
            // Bad not on that tip; keep research placeholder +8 (still not GL_FINAL_VERIFIED).
            switch (t)
            {
                case DriveTiming.Bad: return 8f;
                case DriveTiming.Great: return 30f;
                case DriveTiming.Perfect: return 40f;
                default: return 15f; // Good
            }
        }

        void AddFever(float amount)
        {
            if (FeverActive) return;
            FeverGauge = Math.Min(100f, FeverGauge + amount);
            if (FeverGauge >= 100f)
            {
                FeverGauge = 0f;
                FeverActive = true;
                FeverEver = true;
                FeverLeft = Clocks != null ? Clocks.FeverWindowSec : UnknownFeverWindowSec;
                FeverHitsLeft = Clocks != null ? Clocks.FeverHitBudget : UnknownFeverHitBudget;
                _feverAcc = 0;
                LastEvent = "FEVER";
                Casts.Add(new CastFx { CasterSlot = 0, CasterAlly = true, Type = SkillType.Fever, Name = "FEVER", Fever = true });
            }
        }

        void ApplyLeader()
        {
            if (LeaderSlot < 0 || LeaderSlot >= Allies.Length) return;
            var leader = Allies[LeaderSlot];
            if (leader == null || leader.Def == null) return;
            var skill = ResolveSkill(leader.Def.LeaderSkillId);
            if (skill == null) return;
            Cast(leader, true, skill, 1f);
        }

        void LoadWave(int index)
        {
            WaveIndex = index;
            FocusEnemySlot = -1;
            Enemies.Clear();
            var ids = index == 0 ? _stage.Wave0 : _stage.Wave1;
            if (ids != null)
            {
                for (int i = 0; i < ids.Length; i++)
                {
                    var src = Catalog.TryChar(ids[i]);
                    if (src == null) continue;
                    Enemies.Add(Spawn(Growth.ScaleEnemy(src, _stage), i, false));
                }
            }
            LastEvent = index == 0 ? "第一波" : "首领出现";
            NoteEvent("wave", index == 0 ? "enter" : "advance", null, null, index, SkillType.Auto);
        }

        static UnitState Spawn(CharacterDef def, int slot, bool ally)
        {
            return new UnitState
            {
                Def = def,
                Slot = slot,
                Ally = ally,
                MaxHp = def.Hp,
                Hp = def.Hp,
                Charge = ally ? 35f : 10f
            };
        }

        void CheckWave()
        {
            var any = false;
            for (int i = 0; i < Enemies.Count; i++)
                if (Enemies[i] != null && Enemies[i].Alive) { any = true; break; }
            if (any) return;
            SweepDeaths();
            if (WaveIndex == 0)
            {
                LoadWave(1);
                return;
            }
            if (Outcome == BattleOutcome.InProgress)
            {
                Outcome = BattleOutcome.Victory;
                LastEvent = "胜利";
                NoteResult("clear");
            }
        }

        public bool AnyAllyAlive()
        {
            for (int i = 0; i < Allies.Length; i++)
                if (Allies[i] != null && Allies[i].Alive) return true;
            SweepDeaths();
            if (Outcome == BattleOutcome.InProgress)
            {
                Outcome = BattleOutcome.Defeat;
                LastEvent = "全灭";
                NoteResult("wipe");
            }
            return false;
        }
    }
}
