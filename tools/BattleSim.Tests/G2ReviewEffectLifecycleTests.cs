using System;
using Resonance.Battle;
using Xunit;
using static Resonance.Tests.G2ReviewFixtures;

namespace Resonance.Tests
{
    /// <summary>
    /// G2 review rows E02–E04 plus the Silence/ChargeSpeed capability rows (REGRESSION_MATRIX.md E01 subset).
    /// Contract: API_CONTRACT.md §5. Formula fixtures use ForceNoCrit explicitly (§4) so damage is predictable.
    /// </summary>
    public sealed class G2ReviewEffectLifecycleTests
    {
        const string ShieldId = "g2_shield_1s";
        const string PermShieldId = "g2_shield_perm";

        [Fact]
        public void E02_ShieldExpiry_UsesDeclaredLifetime()
        {
            var sim = NewJp(401, AutoMode.Manual, forceNoCrit: true);
            SuppressAllyAutoAttacks(sim);
            LockFoes(sim);
            var target = sim.Enemies[0];
            Assert.True(sim.TryFocusEnemy(0));
            var shield = Fx(ShieldId, EffectKind.Shield, EffectOpcodes.ShieldApply, 0.5f, 1.0f, 1, 1, "g2_shield");

            sim.ApplyStatus(target, shield);
            var expected = (int)Math.Round(target.MaxHp * 0.5f);
            Assert.Equal(expected, target.Shield);
            var inst = FindStatus(target, ShieldId);
            Assert.NotNull(inst);
            Assert.Equal(expected, inst.ShieldLeft);
            Assert.Equal(target.Shield, inst.ShieldLeft);
            Assert.Equal(1.0f, inst.Remaining);
            Assert.False(inst.Permanent);

            // Half-life: still fully present, lifetime ticking.
            for (int i = 0; i < BattleSim.TickHz / 2; i++) sim.Tick();
            Assert.True(target.Alive);
            Assert.Equal(expected, target.Shield);
            inst = FindStatus(target, ShieldId);
            Assert.NotNull(inst);
            Assert.InRange(inst.Remaining, 0.4f, 0.6f);

            // Past the declared 1.0s lifetime: numeric side effect and status both gone.
            for (int i = 0; i < BattleSim.TickHz / 2 + 2; i++) sim.Tick();
            Assert.Null(FindStatus(target, ShieldId));
            Assert.False(target.Has(EffectKind.Shield));
            Assert.Equal(0, target.Shield);

            // A hit after expiry must land at full value (the icon is not the only thing that vanished).
            LockFoes(sim);
            var caster = sim.Allies[0];
            var tap = Catalog.MustSkill(caster.Def.TapSkillId);
            var hpBefore = target.Hp;
            var expectedHit = ExpectedTapHit(caster, target, tap); // computed before the hit (uses current HP cap)
            var ev0 = sim.Events.Events.Count;
            ChargeAll(sim);
            Assert.True(sim.TryTap(0));
            var dealt = hpBefore - target.Hp;
            Assert.True(dealt > 0);
            Assert.Equal(expectedHit, dealt);
            Assert.Equal(1, CountEvents(sim, e => e.Kind == "hit" && e.Channel == SkillType.Tap && !e.TargetAlly && e.TargetSlot == target.Slot, ev0));
            Assert.Equal(0, target.Shield);
        }

        [Fact]
        public void E02_PermanentShield_LivesUntilConsumed()
        {
            var sim = NewJp(402, AutoMode.Manual, forceNoCrit: true);
            SuppressAllyAutoAttacks(sim);
            LockFoes(sim);
            var target = sim.Enemies[1];
            Assert.True(sim.TryFocusEnemy(1));
            var shield = Fx(PermShieldId, EffectKind.Shield, EffectOpcodes.ShieldApply, 0.3f, 0f, 1, 1, "g2_shield");
            sim.ApplyStatus(target, shield);
            var expected = (int)Math.Round(target.MaxHp * 0.3f);
            Assert.Equal(expected, target.Shield);
            var inst = FindStatus(target, PermShieldId);
            Assert.NotNull(inst);
            Assert.True(inst.Permanent);
            Assert.Equal(expected, inst.ShieldLeft);

            // Five seconds of ticking must not expire a DurationSec <= 0 effect.
            for (int s = 0; s < 5; s++)
            {
                LockFoes(sim);
                for (int i = 0; i < BattleSim.TickHz; i++) sim.Tick();
            }
            Assert.True(target.Alive);
            inst = FindStatus(target, PermShieldId);
            Assert.NotNull(inst);
            Assert.Equal(expected, target.Shield);
            Assert.Equal(expected, inst.ShieldLeft);

            // Consume it with taps: shield absorbs first, then goes away when spent.
            var hpBefore = target.Hp;
            var taps = 0;
            while (target.Shield > 0 && taps < 8)
            {
                ChargeAll(sim);
                Assert.True(sim.TryTap(0));
                taps++;
                inst = FindStatus(target, PermShieldId);
                if (target.Shield > 0)
                {
                    Assert.NotNull(inst);
                    Assert.Equal(target.Shield, inst.ShieldLeft);
                    Assert.Equal(hpBefore, target.Hp); // fully absorbed while shield remains
                }
            }
            Assert.Equal(0, target.Shield);
            Assert.Null(FindStatus(target, PermShieldId));
            Assert.False(target.Has(EffectKind.Shield));
            Assert.True(target.Hp <= hpBefore);
        }

        [Fact]
        public void E02_ShieldPerInstance_TwoGroupsSumAndExpireSeparately()
        {
            var sim = NewJp(403, AutoMode.Manual, forceNoCrit: true);
            SuppressAllyAutoAttacks(sim);
            LockFoes(sim);
            var target = sim.Enemies[0];
            var shortFx = Fx("g2_sh_short", EffectKind.Shield, EffectOpcodes.ShieldApply, 0.1f, 0.5f, 1, 1, "g2_sh_a");
            var longFx = Fx("g2_sh_long", EffectKind.Shield, EffectOpcodes.ShieldApply, 0.2f, 3f, 1, 1, "g2_sh_b");
            sim.ApplyStatus(target, shortFx);
            sim.ApplyStatus(target, longFx);
            var a = (int)Math.Round(target.MaxHp * 0.1f);
            var b = (int)Math.Round(target.MaxHp * 0.2f);
            Assert.Equal(a + b, target.Shield);
            Assert.Equal(a, FindStatus(target, "g2_sh_short").ShieldLeft);
            Assert.Equal(b, FindStatus(target, "g2_sh_long").ShieldLeft);
            for (int i = 0; i < BattleSim.TickHz / 2 + 2; i++) sim.Tick();
            Assert.Null(FindStatus(target, "g2_sh_short"));
            Assert.NotNull(FindStatus(target, "g2_sh_long"));
            Assert.Equal(b, target.Shield);
        }

        [Fact]
        public void E03_StackCapRefreshAndReplacement()
        {
            var sim = NewJp(411, AutoMode.Manual, forceNoCrit: true);
            var unit = sim.Enemies[0]; // no leader AtkBuff on enemies → clean Atk baseline
            Assert.False(unit.Has(EffectKind.AtkBuff));
            var baseAtk = unit.Def.Atk;
            var fx = Fx("g2_stack_atk", EffectKind.AtkBuff, EffectOpcodes.StatusApply, 0.10f, 5f, 3, 1, "g2_atk");

            for (int i = 0; i < 5; i++) sim.ApplyStatus(unit, fx);
            Assert.Equal(1, CountStatus(unit, "g2_stack_atk"));
            var inst = FindStatus(unit, "g2_stack_atk");
            Assert.Equal(3, inst.Stacks);
            Assert.Equal(5f, inst.Remaining);
            Assert.Equal((int)Math.Round(baseAtk * (1f + 0.10f * 3)), unit.Atk);

            // Refresh: re-applying at cap resets the declared duration without exceeding MaxStack.
            LockFoes(sim);
            for (int i = 0; i < BattleSim.TickHz; i++) sim.Tick();
            inst = FindStatus(unit, "g2_stack_atk");
            Assert.NotNull(inst);
            Assert.InRange(inst.Remaining, 3.9f, 4.1f);
            sim.ApplyStatus(unit, fx);
            inst = FindStatus(unit, "g2_stack_atk");
            Assert.Equal(3, inst.Stacks);
            Assert.Equal(5f, inst.Remaining);

            // Replacement: different Id, same Group, higher SourceTier wins and resets stacks.
            var hi = Fx("g2_stack_atk_hi", EffectKind.AtkBuff, EffectOpcodes.StatusApply, 0.25f, 5f, 1, 2, "g2_atk");
            sim.ApplyStatus(unit, hi);
            Assert.Equal(0, CountStatus(unit, "g2_stack_atk"));
            Assert.Equal(1, CountStatus(unit, "g2_stack_atk_hi"));
            Assert.Equal(1, CountGroup(unit, "g2_atk"));
            Assert.Equal(1, FindStatus(unit, "g2_stack_atk_hi").Stacks);
            Assert.Equal((int)Math.Round(baseAtk * 1.25f), unit.Atk);

            // Lower SourceTier in the same Group is ignored.
            var lo = Fx("g2_stack_atk_lo", EffectKind.AtkBuff, EffectOpcodes.StatusApply, 0.05f, 9f, 1, 0, "g2_atk");
            sim.ApplyStatus(unit, lo);
            Assert.Equal(0, CountStatus(unit, "g2_stack_atk_lo"));
            Assert.Equal(1, CountGroup(unit, "g2_atk"));
            Assert.Equal("g2_stack_atk_hi", FindStatus(unit, "g2_stack_atk_hi").Def.Id);
            Assert.Equal((int)Math.Round(baseAtk * 1.25f), unit.Atk);

            // MaxStack 1 (or 0) never stacks — repeated application is a refresh only.
            var single = Fx("g2_single", EffectKind.DefBuff, EffectOpcodes.StatusApply, 0.10f, 4f, 1, 1, "g2_def");
            sim.ApplyStatus(unit, single);
            sim.ApplyStatus(unit, single);
            Assert.Equal(1, FindStatus(unit, "g2_single").Stacks);
            Assert.Equal((int)Math.Round(unit.Def.Def * 1.10f), unit.Defense);
        }

        [Fact]
        public void E04_DotTriggers_OnActionOnly()
        {
            var sim = NewJp(421, AutoMode.Manual);
            SuppressAllyAutoAttacks(sim);
            SuppressEnemyAutoAttacks(sim);
            var actor = sim.Allies[0];
            var fx = DotFx("g2_dot_action", EffectKind.Poison, "on_action", 0.05f, 0f);
            sim.ApplyStatus(actor, fx);
            Assert.True(actor.Has(EffectKind.Poison));
            sim.ApplyStatus(actor, Catalog.TryEffect("taunt")); // enemies must target this unit

            // Acting triggers exactly one on_action tick.
            var hp0 = actor.Hp;
            var ev0 = sim.Events.Events.Count;
            ChargeAll(sim);
            Assert.True(sim.TryTap(0));
            var onAction = CountEvents(sim, e => e.Kind == "on_action" && e.Opcode == EffectOpcodes.PoisonApply && e.TargetAlly && e.TargetSlot == 0, ev0);
            Assert.Equal(1, onAction);
            Assert.Equal(0, CountEvents(sim, e => e.Kind == "on_hit_taken" && e.TargetAlly && e.TargetSlot == 0, ev0));
            Assert.Equal(0, CountEvents(sim, e => e.Kind == "periodic" && e.TargetAlly && e.TargetSlot == 0, ev0));
            var actionEv = sim.Events.Events.Find(e => e.Seq >= ev0 && e.Kind == "on_action" && e.TargetAlly && e.TargetSlot == 0);
            Assert.NotNull(actionEv);
            Assert.True(actionEv.Amount > 0);
            Assert.Equal(hp0 - actionEv.Amount, actor.Hp);

            // Taking a hit must NOT trigger an on_action-only effect.
            var ev1 = sim.Events.Events.Count;
            var hp1 = actor.Hp;
            var hits = ForceEnemyHitOn(sim, actor);
            Assert.True(hits > 0, "fixture failed to land an enemy hit on the taunting unit");
            Assert.Equal(0, CountEvents(sim, e => e.Kind == "on_hit_taken" && e.TargetAlly && e.TargetSlot == 0, ev1));
            Assert.Equal(0, CountEvents(sim, e => e.Kind == "on_action" && e.TargetAlly && e.TargetSlot == 0, ev1));
            Assert.Equal(hp1 - SumHitAmounts(sim, 0, ev1), actor.Hp);
        }

        [Fact]
        public void E04_DotTriggers_OnHitTakenOnly()
        {
            var sim = NewJp(422, AutoMode.Manual);
            SuppressAllyAutoAttacks(sim);
            SuppressEnemyAutoAttacks(sim);
            var actor = sim.Allies[0];
            var fx = DotFx("g2_dot_hit", EffectKind.Bleed, "on_hit_taken", 0.05f, 0f);
            sim.ApplyStatus(actor, fx);
            Assert.True(actor.Has(EffectKind.Bleed));
            sim.ApplyStatus(actor, Catalog.TryEffect("taunt"));

            // Acting must not trigger an on_hit_taken-only effect.
            var hp0 = actor.Hp;
            var ev0 = sim.Events.Events.Count;
            ChargeAll(sim);
            Assert.True(sim.TryTap(0));
            Assert.Equal(0, CountEvents(sim, e => e.Kind == "on_action" && e.TargetAlly && e.TargetSlot == 0, ev0));
            Assert.Equal(0, CountEvents(sim, e => e.Kind == "on_hit_taken" && e.TargetAlly && e.TargetSlot == 0, ev0));
            Assert.Equal(hp0, actor.Hp);

            // Taking a hit triggers exactly one bleed tick per hit taken.
            var ev1 = sim.Events.Events.Count;
            var hp1 = actor.Hp;
            var hits = ForceEnemyHitOn(sim, actor);
            Assert.True(hits > 0, "fixture failed to land an enemy hit on the taunting unit");
            var bleeds = CountEvents(sim, e => e.Kind == "on_hit_taken" && e.Opcode == EffectOpcodes.PoisonApply && e.TargetAlly && e.TargetSlot == 0, ev1);
            Assert.Equal(hits, bleeds);
            Assert.Equal(0, CountEvents(sim, e => e.Kind == "on_action" && e.TargetAlly && e.TargetSlot == 0, ev1));
            Assert.Equal(hp1 - SumHitAmounts(sim, 0, ev1), actor.Hp);
            var bleedTotal = 0;
            foreach (var e in sim.Events.Events)
                if (e.Seq >= ev1 && e.Kind == "on_hit_taken" && e.TargetAlly && e.TargetSlot == 0) bleedTotal += e.Amount;
            Assert.True(bleedTotal > 0);
            Assert.True(SumHitAmounts(sim, 0, ev1) > bleedTotal, "hp drop must include both the enemy hit and the bleed tick");
        }

        [Fact]
        public void E04_DotTriggers_PeriodicOnly()
        {
            var sim = NewJp(423, AutoMode.Manual);
            SuppressAllyAutoAttacks(sim);
            LockFoes(sim);
            var actor = sim.Allies[0];
            var fx = DotFx("g2_dot_periodic", EffectKind.Poison, "periodic", 0.02f, 0.5f);
            sim.ApplyStatus(actor, fx);
            Assert.True(actor.Has(EffectKind.Poison));

            // Acting does nothing for a periodic-only effect.
            var ev0 = sim.Events.Events.Count;
            var hp0 = actor.Hp;
            ChargeAll(sim);
            Assert.True(sim.TryTap(0));
            Assert.Equal(0, CountEvents(sim, e => e.Kind == "on_action" && e.TargetAlly && e.TargetSlot == 0, ev0));
            Assert.Equal(hp0, actor.Hp);

            // 1.067s of idle ticking at PeriodSec 0.5 → exactly two periodic ticks (0.5s, 1.0s).
            var ev1 = sim.Events.Events.Count;
            var hp1 = actor.Hp;
            for (int i = 0; i < 32; i++) sim.Tick();
            var periodic = CountEvents(sim, e => e.Kind == "periodic" && e.Opcode == EffectOpcodes.PoisonApply && e.TargetAlly && e.TargetSlot == 0, ev1);
            Assert.Equal(2, periodic);
            var periodicTotal = 0;
            foreach (var e in sim.Events.Events)
                if (e.Seq >= ev1 && e.Kind == "periodic" && e.TargetAlly && e.TargetSlot == 0) periodicTotal += e.Amount;
            Assert.True(periodicTotal > 0);
            Assert.Equal(hp1 - periodicTotal, actor.Hp);
            Assert.Equal(0, CountEvents(sim, e => (e.Kind == "on_action" || e.Kind == "on_hit_taken") && e.TargetAlly && e.TargetSlot == 0, ev1));

            // Paused: the periodic clock stops with the battle clocks.
            sim.Paused = true;
            var ev2 = sim.Events.Events.Count;
            for (int i = 0; i < 32; i++) sim.Tick();
            Assert.Equal(0, CountEvents(sim, e => e.Kind == "periodic", ev2));
        }

        [Fact]
        public void E04_DotTriggers_DefaultsByKind()
        {
            // Empty Trigger keeps the design defaults: Poison → on_action|on_hit_taken, Bleed → on_hit_taken.
            var sim = NewJp(424, AutoMode.Manual);
            SuppressAllyAutoAttacks(sim);
            LockFoes(sim);
            var poisoned = sim.Allies[0];
            var bled = sim.Allies[1];
            sim.ApplyStatus(poisoned, DotFx("g2_poison_default", EffectKind.Poison, "", 0.05f, 0f));
            sim.ApplyStatus(bled, DotFx("g2_bleed_default", EffectKind.Bleed, "", 0.05f, 0f));
            var ev0 = sim.Events.Events.Count;
            var hpBled = bled.Hp;
            ChargeAll(sim);
            Assert.True(sim.TryTap(0));
            Assert.True(sim.TryTap(1));
            Assert.Equal(1, CountEvents(sim, e => e.Kind == "on_action" && e.TargetAlly && e.TargetSlot == 0, ev0));
            Assert.Equal(0, CountEvents(sim, e => e.Kind == "on_action" && e.TargetAlly && e.TargetSlot == 1, ev0));
            Assert.Equal(hpBled, bled.Hp);
        }

        [Fact]
        public void E04_PeriodicWithoutPeriodNeverTicks()
        {
            // Contract: `periodic` requires PeriodSec > 0. Either the apply is rejected, or the effect never damages.
            var sim = NewJp(425, AutoMode.Manual);
            SuppressAllyAutoAttacks(sim);
            LockFoes(sim);
            var actor = sim.Allies[0];
            var bad = DotFx("g2_dot_bad_periodic", EffectKind.Poison, "periodic", 0.02f, 0f);
            var hp0 = actor.Hp;
            var ev0 = sim.Events.Events.Count;
            try
            {
                sim.ApplyStatus(actor, bad);
            }
            catch (Exception)
            {
                Assert.Null(FindStatus(actor, "g2_dot_bad_periodic"));
                return;
            }
            for (int i = 0; i < BattleSim.TickHz * 2; i++) sim.Tick();
            Assert.Equal(0, CountEvents(sim, e => e.Kind == "periodic" && e.TargetAlly && e.TargetSlot == 0, ev0));
            Assert.Equal(hp0, actor.Hp);
        }

        [Fact]
        public void Silence_BlocksSkillInputButNotAutoAttack()
        {
            var sim = NewJp(431, AutoMode.Manual);
            InflateEnemies(sim);
            var unit = sim.Allies[0];
            var silence = Fx("g2_silence", EffectKind.Silence, EffectOpcodes.ControlApply, 1f, 5f, 1, 2, "g2_silence");
            ChargeAll(sim);
            sim.Drive = 100f;
            sim.ApplyStatus(unit, silence);
            Assert.True(unit.Has(EffectKind.Silence));
            Assert.True(unit.SkillLocked);
            Assert.False(unit.ActionLocked);
            var chargeAfterSilence = unit.Charge;
            Assert.False(sim.CanAct(0));
            Assert.False(sim.CanSlide(0));
            Assert.False(sim.TryTap(0));
            Assert.False(sim.TrySlide(0));
            Assert.False(sim.TryBeginDrive(0));
            Assert.Equal(-1, sim.PendingDriveSlot);

            var tap = sim.Submit(Cmd(BattleCommandKind.Tap, 0));
            Assert.False(tap.Accepted);
            Assert.Equal(CommandReject.Silenced, tap.Reason);
            var slide = sim.Submit(Cmd(BattleCommandKind.Slide, 0));
            Assert.False(slide.Accepted);
            Assert.Equal(CommandReject.Silenced, slide.Reason);
            var drive = sim.Submit(Cmd(BattleCommandKind.DriveBegin, 0));
            Assert.False(drive.Accepted);
            Assert.Equal(CommandReject.Silenced, drive.Reason);
            Assert.Equal(100f, sim.Drive);
            Assert.Equal(chargeAfterSilence, unit.Charge); // rejected input has no side effects

            // Another, un-silenced slot still works through the same entry.
            var other = sim.Submit(Cmd(BattleCommandKind.Tap, 1));
            Assert.True(other.Accepted, "slot 1 tap rejected: " + other.Reason);

            // Normal attack is unaffected (design placeholder): force the auto timer and tick once.
            var casts0 = sim.Casts.Count;
            unit.AutoTimer = 10f;
            sim.Tick();
            var autoCast = false;
            for (int i = casts0; i < sim.Casts.Count; i++)
                if (sim.Casts[i].CasterAlly && sim.Casts[i].CasterSlot == 0 && sim.Casts[i].Type == SkillType.Auto) autoCast = true;
            Assert.True(autoCast, "silenced unit did not auto-attack");
            Assert.Contains(sim.Events.Events, e => e.Kind == "cast" && e.Channel == SkillType.Auto && e.CasterAlly && e.CasterSlot == 0);

            // Silence also blocks FeverTap with the same reason.
            ArmFeverSkippingSlot0(sim);
            var fever = sim.Submit(Cmd(BattleCommandKind.FeverTap, 0));
            Assert.False(fever.Accepted);
            Assert.Equal(CommandReject.Silenced, fever.Reason);
            Assert.False(sim.TryFeverTap(0));
        }

        [Fact]
        public void ChargeSpeed_FeedsChargeRate()
        {
            var plain = NewJp(441, AutoMode.Manual);
            var hasted = NewJp(441, AutoMode.Manual);
            var both = NewJp(441, AutoMode.Manual);
            LockFoes(plain);
            LockFoes(hasted);
            LockFoes(both);
            var speed = Fx("g2_charge_speed", EffectKind.ChargeSpeed, EffectOpcodes.ChargeRate, 0.25f, 10f, 1, 1, "g2_cs");
            var haste = Fx("g2_charge_haste", EffectKind.ChargeHaste, EffectOpcodes.ChargeRate, 0.25f, 10f, 1, 1, "g2_ch");

            Assert.Equal(1f, plain.Allies[0].ChargeSpeedMul, 4);
            hasted.ApplyStatus(hasted.Allies[0], speed);
            Assert.True(hasted.Allies[0].Has(EffectKind.ChargeSpeed));
            Assert.Equal(1.25f, hasted.Allies[0].ChargeSpeedMul, 4);
            both.ApplyStatus(both.Allies[0], speed);
            both.ApplyStatus(both.Allies[0], haste);
            Assert.Equal(1.5f, both.Allies[0].ChargeSpeedMul, 4);

            plain.Allies[0].Charge = 0f;
            hasted.Allies[0].Charge = 0f;
            both.Allies[0].Charge = 0f;
            const int n = 30;
            for (int i = 0; i < n; i++)
            {
                plain.Tick();
                hasted.Tick();
                both.Tick();
            }
            var p = plain.Allies[0].Charge;
            var h = hasted.Allies[0].Charge;
            var b = both.Allies[0].Charge;
            Assert.True(p > 0f);
            Assert.Equal(1.25f, h / p, 2);
            Assert.Equal(1.5f, b / p, 2);
        }

        // ---- helpers ------------------------------------------------------------------

        static EffectDef DotFx(string id, EffectKind kind, string trigger, float magnitude, float periodSec)
        {
            var fx = Fx(id, kind, EffectOpcodes.PoisonApply, magnitude, 30f, 1, 1, id);
            fx.Trigger = trigger;
            fx.PeriodSec = periodSec;
            return fx;
        }

        /// <summary>Makes Enemies[0] cast at the taunting unit right now; returns the number of enemy hits it took.</summary>
        static int ForceEnemyHitOn(BattleSim sim, UnitState victim)
        {
            Assert.True(victim.Has(EffectKind.Taunt));
            var ev = sim.Events.Events.Count;
            for (int attempt = 0; attempt < 6; attempt++)
            {
                var foe = sim.Enemies[0];
                Assert.True(foe.Alive);
                foe.Charge = 100f;
                foe.SlideCd = 0f;
                sim.Tick();
                // Enemy hits carry a real caster slot; DoT ticks are logged with CasterSlot == -1.
                var hits = CountEvents(sim, e => e.Kind == "hit" && !e.CasterAlly && e.CasterSlot >= 0 && e.TargetAlly && e.TargetSlot == victim.Slot, ev);
                if (hits > 0) return hits;
            }
            return 0;
        }

        static int SumHitAmounts(BattleSim sim, int allySlot, int since)
        {
            var sum = 0;
            var ev = sim.Events.Events;
            for (int i = since; i < ev.Count; i++)
                if (ev[i].Kind == "hit" && ev[i].TargetAlly && ev[i].TargetSlot == allySlot) sum += ev[i].Amount;
            return sum;
        }

        static int CountStatus(UnitState u, string id)
        {
            var n = 0;
            for (int i = 0; i < u.Status.Count; i++)
                if (u.Status[i] != null && u.Status[i].Def != null && u.Status[i].Def.Id == id) n++;
            return n;
        }

        static int CountGroup(UnitState u, string group)
        {
            var n = 0;
            for (int i = 0; i < u.Status.Count; i++)
                if (u.Status[i] != null && u.Status[i].Def != null && u.Status[i].Def.Group == group) n++;
            return n;
        }

        static void ArmFeverSkippingSlot0(BattleSim sim)
        {
            for (int n = 0; n < 8 && !sim.FeverActive; n++)
            {
                ChargeAll(sim);
                sim.Drive = 100f;
                Assert.True(sim.TryBeginDrive(1));
                Assert.True(sim.ResolveDrive(DriveTiming.Perfect));
            }
            Assert.True(sim.FeverActive);
        }

        static int ExpectedTapHit(UnitState caster, UnitState target, SkillDef skill)
        {
            var extraMul = DamageMath.ExtraDmgMul(DamageMath.ExtraDmg(
                skill.Type,
                DamageMath.Beats(caster.Def.Element, target.Def.Element),
                caster.Magnitude(EffectKind.TsAmp),
                caster.Magnitude(EffectKind.SsAmp),
                caster.Magnitude(EffectKind.DsAmp),
                target.Magnitude(EffectKind.SkillDefDown),
                target.Magnitude(EffectKind.WeakDefDown)));
            var dmg = DamageMath.ComputeSkill(
                skill.Type, caster.Atk, skill.AtkCoef, skill.FlatPower,
                target.DefenseAgainst(caster.Def.Element),
                caster.Def.Element, target.Def.Element,
                false, extraMul, 1f, 1f, skill.PercentAtk, skill.SkillFlat, caster.ExtraAtk);
            return Math.Min(dmg * Math.Max(1, skill.HitCount), target.Hp);
        }
    }
}
