using System;
using System.Collections.Generic;

namespace Resonance.Battle
{
    public enum BattleOutcome
    {
        InProgress = 0,
        Victory = 1,
        Defeat = 2
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
        public float AutoTimer;
        public readonly List<StatusInst> Status = new List<StatusInst>();
        public bool Alive => Hp > 0;

        public int Atk => Scaled(Def.Atk, EffectKind.AtkBuff);
        public int Defense
        {
            get
            {
                var v = Def.Def;
                var deb = Magnitude(EffectKind.DefDebuff);
                return (int)Math.Round(v * (1f - deb));
            }
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
    }

    public sealed class FloatText
    {
        public int UnitSlot;
        public bool Ally;
        public string Text;
        public bool Crit;
        public bool Heal;
    }

    public sealed class BattleSim
    {
        public const int TickHz = 30;
        public const float TickDt = 1f / TickHz;

        public readonly UnitState[] Allies = new UnitState[5];
        public readonly List<UnitState> Enemies = new List<UnitState>(8);
        public int WaveIndex;
        public float Drive;
        public float FeverGauge;
        public bool FeverActive;
        public float FeverLeft;
        public int FeverHitsLeft;
        public float TimeLeft;
        public int Speed = 1;
        public bool Paused;
        public bool HoldSim;
        public bool AutoTap;
        public bool Deterministic = true;
        public BattleOutcome Outcome = BattleOutcome.InProgress;
        public int PendingDriveSlot = -1;
        public readonly List<FloatText> Log = new List<FloatText>(64);
        public string LastEvent = "";
        public int LeaderSlot;

        readonly Random _rng;
        readonly StageDef _stage;
        int _feverAcc;

        public BattleSim(string[] partyIds, int leaderSlot, int seed)
        {
            _rng = new Random(seed);
            _stage = Catalog.VerticalSliceStage;
            TimeLeft = _stage.TimeLimitSec;
            LeaderSlot = leaderSlot;
            for (int i = 0; i < 5; i++)
                Allies[i] = Spawn(Catalog.MustChar(partyIds[i]), i, true);
            LoadWave(0);
            ApplyLeader();
        }

        public void Tick()
        {
            if (Outcome != BattleOutcome.InProgress || Paused || HoldSim || PendingDriveSlot >= 0) return;
            var dt = TickDt * Speed;
            TimeLeft -= dt;
            if (TimeLeft <= 0f)
            {
                TimeLeft = 0f;
                Outcome = BattleOutcome.Defeat;
                LastEvent = "时间耗尽";
                return;
            }

            TickStatus(dt);
            if (FeverActive) TickFever(dt);

            for (int i = 0; i < Allies.Length; i++)
                TickUnit(Allies[i], true, dt);
            for (int i = 0; i < Enemies.Count; i++)
                TickUnit(Enemies[i], false, dt);

            if (AutoTap)
            {
                for (int i = 0; i < Allies.Length; i++)
                {
                    if (Allies[i].Alive && Allies[i].Charge >= 100f)
                        TryTap(i);
                }
            }

            if (!AnyAllyAlive()) return;
            CheckWave();
        }

        public bool TryTap(int slot) => UsePlayerSkill(slot, SkillType.Tap);
        public bool TrySlide(int slot) => UsePlayerSkill(slot, SkillType.Slide);

        public bool TryBeginDrive(int slot)
        {
            if (!CanAct(slot)) return false;
            if (Drive < 100f) return false;
            PendingDriveSlot = slot;
            LastEvent = Allies[slot].Def.Name + " 准备 Drive";
            return true;
        }

        public bool ResolveDrive(DriveTiming timing)
        {
            if (PendingDriveSlot < 0) return false;
            var slot = PendingDriveSlot;
            PendingDriveSlot = -1;
            Drive = 0f;
            var mul = TimingDamage(timing);
            AddFever(TimingFever(timing));
            var unit = Allies[slot];
            var skill = Catalog.MustSkill(unit.Def.DriveSkillId);
            unit.Charge = 0f;
            Cast(unit, true, skill, mul);
            LastEvent = unit.Def.Name + " Drive " + timing;
            return true;
        }

        public bool CanAct(int slot)
        {
            return Outcome == BattleOutcome.InProgress
                && !Paused
                && PendingDriveSlot < 0
                && slot >= 0 && slot < Allies.Length
                && Allies[slot].Alive
                && Allies[slot].Charge >= 100f;
        }

        void TickUnit(UnitState u, bool ally, float dt)
        {
            if (!u.Alive) return;
            var chargePerSec = (100f / u.Def.ChargeTimeSec) * u.ChargeSpeedMul;
            u.Charge += chargePerSec * dt;
            if (u.Charge > 100f) u.Charge = 100f;

            u.AutoTimer += dt;
            var autoCd = Math.Max(1.1f, 2.4f - u.Def.Agl / 2000f);
            if (u.AutoTimer >= autoCd)
            {
                u.AutoTimer = 0f;
                Cast(u, ally, Catalog.MustSkill(u.Def.AutoSkillId), 1f);
            }

            if (!ally && u.Charge >= 100f)
            {
                var skill = Catalog.MustSkill(_rng.NextDouble() < 0.65 ? u.Def.TapSkillId : u.Def.SlideSkillId);
                u.Charge = 0f;
                Cast(u, false, skill, 1f);
            }
        }

        void TickFever(float dt)
        {
            FeverLeft -= dt;
            _feverAcc++;
            var ticksPerHit = Math.Max(1, TickHz / 10);
            if (_feverAcc >= ticksPerHit && FeverHitsLeft > 0)
            {
                _feverAcc = 0;
                FeverHitsLeft--;
                var caster = FirstAlive(Allies);
                var target = PickEnemy(TargetRule.LowestHpEnemies);
                if (caster != null && target != null)
                {
                    var tap = Catalog.MustSkill(caster.Def.TapSkillId);
                    var dmg = DamageMath.Compute(
                        caster.Atk, tap.AtkCoef * DamageMath.FeverHitTapFraction, tap.FlatPower,
                        target.Defense, caster.Def.Element, target.Def.Element,
                        false, 1f, 1f);
                    ApplyDamage(caster, target, dmg, false);
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
            if (!u.Alive) return;
            for (int i = u.Status.Count - 1; i >= 0; i--)
            {
                var st = u.Status[i];
                if (st.Def.Kind == EffectKind.Dot)
                {
                    var tick = Math.Max(1, (int)Math.Round(u.MaxHp * st.Def.Magnitude * dt / Math.Max(1f, st.Def.DurationSec)));
                    ApplyDamage(null, u, tick, false);
                }
                st.Remaining -= dt;
                if (st.Remaining <= 0f) u.Status.RemoveAt(i);
            }
        }

        bool UsePlayerSkill(int slot, SkillType type)
        {
            if (!CanAct(slot)) return false;
            var u = Allies[slot];
            var id = type == SkillType.Slide ? u.Def.SlideSkillId : u.Def.TapSkillId;
            var skill = Catalog.MustSkill(id);
            u.Charge = 0f;
            Drive = Math.Min(100f, Drive + skill.DriveGain);
            Cast(u, true, skill, 1f);
            LastEvent = u.Def.Name + " " + skill.Name;
            return true;
        }

        void Cast(UnitState caster, bool casterAlly, SkillDef skill, float dmgMul)
        {
            if (skill.HealCoef > 0f || skill.FlatHeal > 0 || skill.HealMaxHpFrac > 0f)
            {
                foreach (var t in PickAllies(skill.Target, skill.TargetCount, casterAlly))
                    Heal(caster, t, skill);
            }
            else if (skill.AtkCoef > 0f || skill.FlatPower > 0)
            {
                var hits = Math.Max(1, skill.HitCount);
                for (int h = 0; h < hits; h++)
                {
                    foreach (var t in PickFoes(casterAlly, skill.Target, skill.TargetCount))
                    {
                        var crit = !Deterministic && _rng.NextDouble() < DamageMath.CritChance(caster.Def.Crt);
                        if (Deterministic) crit = false;
                        var dmg = DamageMath.Compute(
                            caster.Atk, skill.AtkCoef, skill.FlatPower,
                            t.Defense, caster.Def.Element, t.Def.Element,
                            crit, dmgMul, Deterministic ? 1f : Variance());
                        ApplyDamage(caster, t, dmg, crit);
                    }
                }
            }

            var fx = Catalog.TryEffect(skill.EffectId);
            if (fx != null)
            {
                IEnumerable<UnitState> fxTargets = fx.Kind == EffectKind.AtkBuff
                    || fx.Kind == EffectKind.Shield
                    || fx.Kind == EffectKind.ChargeHaste
                    || fx.Kind == EffectKind.Taunt
                    ? PickAllies(fx.Kind == EffectKind.Taunt ? TargetRule.Self : skill.Target, skill.TargetCount, casterAlly)
                    : PickFoes(casterAlly, skill.Target, skill.TargetCount);
                if (fx.Kind == EffectKind.Taunt)
                    fxTargets = new[] { caster };
                foreach (var t in fxTargets)
                    ApplyEffect(t, fx);
            }
        }

        void Heal(UnitState caster, UnitState t, SkillDef skill)
        {
            if (!t.Alive) return;
            var amt = (int)Math.Round(caster.Atk * skill.HealCoef + skill.FlatHeal + t.MaxHp * skill.HealMaxHpFrac);
            if (amt < 1) amt = 1;
            t.Hp = Math.Min(t.MaxHp, t.Hp + amt);
            Log.Add(new FloatText { UnitSlot = t.Slot, Ally = t.Ally, Text = "+" + amt, Heal = true });
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
            Log.Add(new FloatText { UnitSlot = t.Slot, Ally = t.Ally, Text = dmg.ToString(), Crit = crit });
        }

        public void ApplyStatus(UnitState t, EffectDef fx) => ApplyEffect(t, fx);

        void ApplyEffect(UnitState t, EffectDef fx)
        {
            if (t == null || !t.Alive) return;
            for (int i = 0; i < t.Status.Count; i++)
            {
                var cur = t.Status[i];
                if (cur.Def.Group == fx.Group)
                {
                    if (fx.SourceTier >= cur.Def.SourceTier)
                    {
                        cur.Def = fx;
                        cur.Remaining = fx.DurationSec;
                    }
                    return;
                }
            }
            t.Status.Add(new StatusInst { Def = fx, Remaining = fx.DurationSec });
            if (fx.Kind == EffectKind.Shield)
                t.Shield = Math.Max(t.Shield, (int)Math.Round(t.MaxHp * fx.Magnitude));
        }

        IEnumerable<UnitState> PickFoes(bool casterAlly, TargetRule rule, int count)
        {
            if (casterAlly) return PickEnemies(rule, count);
            return PickFrom(Allies, rule, count, preferTaunt: true);
        }

        IEnumerable<UnitState> PickAllies(TargetRule rule, int count, bool casterAlly)
        {
            return casterAlly ? PickFrom(Allies, rule, count, false) : PickEnemies(rule, count);
        }

        List<UnitState> PickEnemies(TargetRule rule, int count)
        {
            var buf = new List<UnitState>(8);
            for (int i = 0; i < Enemies.Count; i++)
                if (Enemies[i].Alive) buf.Add(Enemies[i]);
            return Select(buf, rule, count, false);
        }

        List<UnitState> PickFrom(UnitState[] src, TargetRule rule, int count, bool preferTaunt)
        {
            var buf = new List<UnitState>(5);
            if (preferTaunt)
            {
                for (int i = 0; i < src.Length; i++)
                    if (src[i].Alive && src[i].Has(EffectKind.Taunt)) buf.Add(src[i]);
            }
            if (buf.Count == 0)
            {
                for (int i = 0; i < src.Length; i++)
                    if (src[i].Alive) buf.Add(src[i]);
            }
            return Select(buf, rule, count, true);
        }

        List<UnitState> Select(List<UnitState> alive, TargetRule rule, int count, bool allySide)
        {
            var result = new List<UnitState>(count);
            if (alive.Count == 0) return result;
            if (rule == TargetRule.AllAllies || rule == TargetRule.AllEnemies)
                return alive;
            if (rule == TargetRule.Self)
            {
                result.Add(alive[0]);
                return result;
            }
            count = Math.Min(count, alive.Count);
            if (rule == TargetRule.LowestHpAlly || rule == TargetRule.LowestHpEnemies)
            {
                alive.Sort((a, b) => a.Hp.CompareTo(b.Hp));
                for (int i = 0; i < count; i++) result.Add(alive[i]);
                return result;
            }
            if (rule == TargetRule.HighestAtkEnemies)
            {
                alive.Sort((a, b) => b.Atk.CompareTo(a.Atk));
                for (int i = 0; i < count; i++) result.Add(alive[i]);
                return result;
            }
            for (int i = 0; i < count; i++)
                result.Add(alive[_rng.Next(alive.Count)]);
            return result;
        }

        UnitState PickEnemy(TargetRule rule)
        {
            var list = PickEnemies(rule, 1);
            return list.Count > 0 ? list[0] : null;
        }

        static UnitState FirstAlive(UnitState[] list)
        {
            for (int i = 0; i < list.Length; i++)
                if (list[i].Alive) return list[i];
            return null;
        }

        float Variance() => 0.95f + (float)_rng.NextDouble() * 0.10f;

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
            switch (t)
            {
                case DriveTiming.Bad: return 8f;
                case DriveTiming.Great: return 30f;
                case DriveTiming.Perfect: return 40f;
                default: return 15f;
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
                FeverLeft = 7f;
                FeverHitsLeft = 70;
                _feverAcc = 0;
                LastEvent = "FEVER";
            }
        }

        void ApplyLeader()
        {
            var leader = Allies[LeaderSlot];
            var skill = Catalog.MustSkill(leader.Def.LeaderSkillId);
            Cast(leader, true, skill, 1f);
        }

        void LoadWave(int index)
        {
            WaveIndex = index;
            Enemies.Clear();
            var ids = index == 0 ? _stage.Wave0 : _stage.Wave1;
            for (int i = 0; i < ids.Length; i++)
                Enemies.Add(Spawn(Catalog.MustChar(ids[i]), i, false));
            LastEvent = index == 0 ? "第一波" : "首领出现";
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
                if (Enemies[i].Alive) { any = true; break; }
            if (any) return;
            if (WaveIndex == 0)
            {
                LoadWave(1);
                return;
            }
            Outcome = BattleOutcome.Victory;
            LastEvent = "胜利";
        }

        public bool AnyAllyAlive()
        {
            for (int i = 0; i < Allies.Length; i++)
                if (Allies[i].Alive) return true;
            Outcome = BattleOutcome.Defeat;
            LastEvent = "全灭";
            return false;
        }
    }
}
