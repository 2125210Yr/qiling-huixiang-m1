using System;
using System.Collections.Generic;
using System.IO;
using Resonance.Battle;
using Xunit;

namespace Resonance.Tests
{
    public sealed class BattleSimTests
    {
        [Fact]
        public void FireBeatsWood()
        {
            Assert.Equal(DamageMath.ElemAdvantage, DamageMath.ElementMultiplier(Element.Fire, Element.Wood));
            Assert.Equal(DamageMath.ElemDisadvantage, DamageMath.ElementMultiplier(Element.Wood, Element.Fire));
        }

        [Fact]
        public void ElementMatchupIsFireWoodWaterAndLightDark()
        {
            Assert.Equal(1.4f, DamageMath.ElemAdvantage);
            Assert.Equal(1.0f, DamageMath.ElemNeutral);
            Assert.Equal(0.7f, DamageMath.ElemDisadvantage);

            var expected = new float[5, 5]
            {
                { 1.0f, 0.7f, 1.4f, 1.0f, 1.0f },
                { 1.4f, 1.0f, 0.7f, 1.0f, 1.0f },
                { 0.7f, 1.4f, 1.0f, 1.0f, 1.0f },
                { 1.0f, 1.0f, 1.0f, 1.0f, 1.4f },
                { 1.0f, 1.0f, 1.0f, 1.4f, 1.0f }
            };
            for (int a = 0; a < 5; a++)
            {
                for (int b = 0; b < 5; b++)
                {
                    var atk = (Element)a;
                    var def = (Element)b;
                    var mul = DamageMath.ElementMultiplier(atk, def);
                    Assert.Equal(expected[a, b], mul);
                    Assert.Equal(expected[a, b] + 1f, DamageMath.ElemCritMultiplier(atk, def, true));
                    Assert.Equal(expected[a, b], DamageMath.ElemCritMultiplier(atk, def, false));
                    Assert.Equal(expected[a, b] > 1f, DamageMath.Beats(atk, def));
                }
            }

            Assert.True(DamageMath.Beats(Element.Fire, Element.Wood));
            Assert.True(DamageMath.Beats(Element.Wood, Element.Water));
            Assert.True(DamageMath.Beats(Element.Water, Element.Fire));
            Assert.True(DamageMath.Beats(Element.Light, Element.Dark));
            Assert.True(DamageMath.Beats(Element.Dark, Element.Light));
            Assert.False(DamageMath.Beats(Element.Fire, Element.Water));
            Assert.False(DamageMath.Beats(Element.Fire, Element.Light));
            Assert.False(DamageMath.Beats(Element.Wood, Element.Fire));
        }

        [Fact]
        public void ElemCritTableIsAdvantageNeutralDisadvantage()
        {
            Assert.Equal(2.4f, DamageMath.ElemCritMultiplier(Element.Fire, Element.Wood, true));
            Assert.Equal(2.0f, DamageMath.ElemCritMultiplier(Element.Fire, Element.Fire, true));
            Assert.Equal(1.7f, DamageMath.ElemCritMultiplier(Element.Wood, Element.Fire, true));
            Assert.Equal(2.4f, DamageMath.ElemCritMultiplier(Element.Light, Element.Dark, true));
            Assert.Equal(2.4f, DamageMath.ElemCritMultiplier(Element.Dark, Element.Light, true));
            Assert.Equal(1.4f, DamageMath.ElemCritMultiplier(Element.Water, Element.Fire, false));
            Assert.Equal(1.0f, DamageMath.ElemCritMultiplier(Element.Light, Element.Fire, false));
            Assert.Equal(0.7f, DamageMath.ElemCritMultiplier(Element.Fire, Element.Water, false));
        }

        [Fact]
        public void ComputeSkillUsesElementMatchup()
        {
            const int atk = 1000, flat = 707, def = 2500;
            var skillDmg = 1707;
            var adv = DamageMath.ComputeSkill(SkillType.Tap, atk, 1f, flat, def, Element.Fire, Element.Wood, false, 1f, 1f);
            var neu = DamageMath.ComputeSkill(SkillType.Tap, atk, 1f, flat, def, Element.Fire, Element.Fire, false, 1f, 1f);
            var dis = DamageMath.ComputeSkill(SkillType.Tap, atk, 1f, flat, def, Element.Fire, Element.Water, false, 1f, 1f);
            Assert.Equal(DamageMath.ComputeTs(0, skillDmg, def, 1.4f, 1f, 1f, 0, 0, false, 0), adv);
            Assert.Equal(DamageMath.ComputeTs(0, skillDmg, def, 1.0f, 1f, 1f, 0, 0, false, 0), neu);
            Assert.Equal(DamageMath.ComputeTs(0, skillDmg, def, 0.7f, 1f, 1f, 0, 0, false, 0), dis);
            Assert.True(adv > neu && neu > dis);

            var light = DamageMath.ComputeSkill(SkillType.Tap, atk, 1f, flat, def, Element.Light, Element.Dark, false, 1f, 1f);
            var dark = DamageMath.ComputeSkill(SkillType.Tap, atk, 1f, flat, def, Element.Dark, Element.Light, false, 1f, 1f);
            Assert.Equal(adv, light);
            Assert.Equal(adv, dark);
        }

        [Fact]
        public void WikiTsFormula()
        {
            var d = DamageMath.ComputeTs(1000, 2707, 2500, 1.4f, 1f, 1f, 0, 0, false, 0);
            Assert.Equal(2182, d);
        }

        [Fact]
        public void WikiTsFeverIsSixtyPercent()
        {
            var full = DamageMath.ComputeTs(1000, 2707, 2500, 1.4f, 1f, 1f, 0, 0, false, 0);
            var fever = DamageMath.ComputeTs(1000, 2707, 2500, 1.4f, 1f, 0.6f, 0, 0, false, 0);
            Assert.Equal((int)System.Math.Round(full * 0.6, System.MidpointRounding.AwayFromZero), fever);
        }

        [Fact]
        public void WikiTsAdvantageCrit()
        {
            var d = DamageMath.ComputeTs(1000, 2707, 2500, 2.4f, 1f, 1f, 0, 0, true, 0);
            Assert.Equal(3740, d);
        }

        [Fact]
        public void RebuiltDriveSiblingFormula()
        {
            var d = DamageMath.ComputeDs(0, 2707, 2500, 1.4f, 1f, 0, 0, false, 0);
            Assert.Equal(2166, d);
        }

        [Fact]
        public void DriveUsesSiblingNotTap()
        {
            var tap = DamageMath.ComputeSkill(SkillType.Tap, 1180, 2.90f, 500, 2500, Element.Fire, Element.Wood, false, 1f, 1f);
            var drive = DamageMath.ComputeSkill(SkillType.Drive, 1180, 2.90f, 500, 2500, Element.Fire, Element.Wood, false, 1f, 1f);
            Assert.NotEqual(tap, drive);
            Assert.True(drive > 1);
        }

        [Fact]
        public void RosterCoversFiveByFive()
        {
            Catalog.BuildBuiltin();
            var cells = new bool[5, 5];
            var n = 0;
            foreach (var id in Catalog.PlayableIds)
            {
                var c = Catalog.MustChar(id);
                cells[(int)c.Element, (int)c.Role] = true;
                n++;
            }
            Assert.Equal(25, n);
            for (int e = 0; e < 5; e++)
                for (int r = 0; r < 5; r++)
                    Assert.True(cells[e, r], "missing " + (Element)e + " " + (Role)r);
        }

        [Fact]
        public void MatrixOrderMatchesRebuildTable()
        {
            Catalog.BuildBuiltin();
            Assert.Equal("C001", Catalog.PlayableAt(Element.Fire, Role.Attacker).Id);
            Assert.Equal("C011", Catalog.PlayableAt(Element.Fire, Role.Supporter).Id);
            Assert.Equal("C015", Catalog.PlayableAt(Element.Water, Role.Attacker).Id);
            Assert.Equal("C003", Catalog.PlayableAt(Element.Water, Role.Healer).Id);
            Assert.Equal("C006", Catalog.PlayableAt(Element.Wood, Role.Attacker).Id);
            Assert.Equal("C007", Catalog.PlayableAt(Element.Light, Role.Defender).Id);
            Assert.Equal("C010", Catalog.PlayableAt(Element.Dark, Role.Debuffer).Id);
            Assert.Equal("C025", Catalog.PlayableAt(Element.Dark, Role.Supporter).Id);
            var ids = Catalog.MatrixIds;
            Assert.Equal(new[]
            {
                "C001", "C002", "C013", "C014", "C011",
                "C015", "C016", "C004", "C003", "C012",
                "C006", "C017", "C018", "C019", "C005",
                "C020", "C007", "C021", "C022", "C008",
                "C023", "C024", "C010", "C009", "C025"
            }, ids);
            Assert.Equal(Catalog.DefaultParty, new[] { "C001", "C007", "C010", "C003", "C005" });
        }

        [Fact]
        public void DamageFormulaMatchesRebuild()
        {
            var d = DamageMath.Compute(1180, 0.90f, 120, 420, Element.Fire, Element.Wood, false, 1f, 1f);
            Assert.True(d > 800 && d < 2500, "dmg=" + d);
        }

        [Fact]
        public void BuffBoardHasNinetyThree()
        {
            Assert.Equal(93, BuffCatalog.All.Length);
            var buffs = 0;
            var debuffs = 0;
            var sim = 0;
            for (int i = 0; i < BuffCatalog.All.Length; i++)
            {
                if (BuffCatalog.All[i].IsDebuff) debuffs++;
                else buffs++;
                if (BuffCatalog.All[i].Simulated) sim++;
            }
            Assert.Equal(58, buffs);
            Assert.Equal(35, debuffs);
            Assert.Equal(7, sim);
            Assert.Equal("atk_up", BuffCatalog.Find("攻击力↑").Id);
            Assert.Equal("def_down", BuffCatalog.Find("防御力↓").Id);
            Assert.Equal("stun", BuffCatalog.Find("晕眩").Id);
            Assert.True(BuffCatalog.Find("攻击力↑").Simulated);
            Assert.True(BuffCatalog.Find("防御力↑").Simulated);
            Assert.True(BuffCatalog.Find("防御力↓").Simulated);
            Assert.True(BuffCatalog.Find("晕眩").Simulated);
            Assert.True(BuffCatalog.Find("屏障").Simulated);
            Assert.True(BuffCatalog.Find("挑衅").Simulated);
            Assert.True(BuffCatalog.Find("Skill充能加速").Simulated);
            Assert.False(BuffCatalog.Find("石化").Simulated);
        }

        [Fact]
        public void LeaderAtkUpRaisesAttack()
        {
            var sim = NewSim(3);
            var u = sim.Allies[0];
            Assert.True(u.Has(EffectKind.AtkBuff));
            Assert.True(u.Atk > u.Def.Atk);
            Assert.Equal((int)System.Math.Round(u.Def.Atk * 1.18f), u.Atk);
        }

        [Fact]
        public void LeaderDefDownLowersDefense()
        {
            var sim = new BattleSim(Catalog.DefaultParty, 2, 3) { Deterministic = true, Speed = 1, Profile = FormulaProfile.JP_LEGACY_EMPIRICAL };
            var e = sim.Enemies[0];
            Assert.True(e.Has(EffectKind.DefDebuff));
            Assert.True(e.Defense < e.Def.Def);
            Assert.Equal((int)System.Math.Round(e.Def.Def * 0.80f), e.Defense);
        }

        [Fact]
        public void DefBuffRaisesDefense()
        {
            // Primary Robin mid-fight shows DEF ↑. Engineering DefBuff — not GL_FINAL.
            var sim = new BattleSim(Catalog.DefaultParty, 0, 3) { Deterministic = true, Speed = 1, Profile = FormulaProfile.JP_LEGACY_EMPIRICAL };
            var u = sim.Allies[0];
            var before = u.Defense;
            var fx = new EffectDef
            {
                Id = "def_up_fx",
                Opcode = EffectOpcodes.StatusApply,
                Kind = EffectKind.DefBuff,
                Magnitude = 0.18f,
                DurationSec = 12f,
                MaxStack = 1,
                SourceTier = 1,
                Group = "def"
            };
            sim.ApplyStatus(u, fx);
            Assert.True(u.Has(EffectKind.DefBuff));
            Assert.True(u.Defense > before);
            Assert.Equal((int)System.Math.Round(u.Def.Def * 1.18f), u.Defense);
            Assert.Equal(EffectKind.DefBuff, (EffectKind)30);
            Assert.True(BuffCatalog.FindId("def_up").Simulated);
        }

        [Fact]
        public void FightStatsCountsDealtAndDps()
        {
            var sim = new BattleSim(Catalog.DefaultParty, 0, 3, Catalog.Stages[0], null)
            {
                Deterministic = true,
                Profile = FormulaProfile.JP_LEGACY_EMPIRICAL,
                Speed = 1,
                Auto = AutoMode.Full
            };
            for (int i = 0; i < BattleSim.TickHz * 6 && sim.Outcome == BattleOutcome.InProgress; i++)
                sim.Tick();
            Assert.True(sim.Stats.Elapsed > 0.5f, "elapsed=" + sim.Stats.Elapsed);
            Assert.True(sim.Stats.TotalDealt > 0, "dealt=" + sim.Stats.TotalDealt);
            Assert.True(sim.Stats.Dps > 0, "dps=" + sim.Stats.Dps);
            var party = 0;
            for (int i = 0; i < sim.Stats.PartyCap; i++)
                party += sim.Stats.AllyDealt[i];
            Assert.True(party > 0, "partyDealt=" + party);
            Assert.True(party <= sim.Stats.TotalDealt);
            Assert.True(sim.Stats.Hits > 0);
        }

        [Fact]
        public void StunSkipsActionAndResetsCharge()
        {
            var sim = NewSim(3);
            var u = sim.Allies[0];
            var stun = Catalog.TryEffect("stun");
            Assert.NotNull(stun);
            u.Charge = 100f;
            sim.Drive = 100f;
            sim.ApplyStatus(u, stun);
            Assert.True(u.Has(EffectKind.Stun));
            Assert.True(u.ActionLocked);
            Assert.Equal(0f, u.Charge);
            Assert.False(sim.CanAct(0));
            Assert.False(sim.TryTap(0));
            Assert.False(sim.TryBeginDrive(0));
        }

        [Fact]
        public void StunExpiresThenUnitCanAct()
        {
            var sim = NewSim(3);
            var u = sim.Allies[0];
            sim.ApplyStatus(u, Catalog.TryEffect("stun"));
            for (int i = 0; i < sim.Enemies.Count; i++)
                sim.ApplyStatus(sim.Enemies[i], Catalog.TryEffect("stun"));
            var ticks = (int)(3f / BattleSim.TickDt) + 2;
            for (int i = 0; i < ticks && u.Has(EffectKind.Stun); i++)
                sim.Tick();
            Assert.False(u.Has(EffectKind.Stun));
            Assert.False(u.ActionLocked);
            u.Charge = 100f;
            Assert.True(sim.CanAct(0));
            Assert.True(sim.TryTap(0));
        }

        [Fact]
        public void SlideStunLocksAnEnemy()
        {
            var sim = NewSim(3);
            ChargeAll(sim);
            Assert.True(sim.TrySlide(2));
            var locked = 0;
            for (int i = 0; i < sim.Enemies.Count; i++)
                if (sim.Enemies[i].Has(EffectKind.Stun)) locked++;
            Assert.True(locked >= 1, "stun=" + locked);
            for (int i = 0; i < sim.Enemies.Count; i++)
            {
                if (!sim.Enemies[i].Has(EffectKind.Stun)) continue;
                Assert.Equal(0f, sim.Enemies[i].Charge);
                Assert.True(sim.Enemies[i].ActionLocked);
            }
        }

        [Fact]
        public void SlideDefDownCoversTapDefDown()
        {
            var sim = NewSim(3);
            var enemy = sim.Enemies[0];
            var tap = new EffectDef { Id = "tap_def", Opcode = EffectOpcodes.StatusApply, Kind = EffectKind.DefDebuff, Magnitude = 0.10f, DurationSec = 10f, MaxStack = 1, SourceTier = 1, Group = "def" };
            var slide = new EffectDef { Id = "slide_def", Opcode = EffectOpcodes.StatusApply, Kind = EffectKind.DefDebuff, Magnitude = 0.20f, DurationSec = 10f, MaxStack = 1, SourceTier = 2, Group = "def" };
            sim.ApplyStatus(enemy, tap);
            sim.ApplyStatus(enemy, slide);
            Assert.Single(enemy.Status);
            Assert.Equal("slide_def", enemy.Status[0].Def.Id);
            Assert.Equal(0.20f, enemy.Status[0].Def.Magnitude);
        }

        [Fact]
        public void CatalogJsonFillsEmptySliceFromBuiltin()
        {
            Catalog.BuildBuiltin();
            CatalogJson.Load("{\"stage\":{\"id\":\"VS-1\",\"name\":\"废都入口\",\"time\":90,\"w0\":[\"E001\"],\"w1\":[\"EBOSS\"]},\"chars\":[{\"id\":\"C001\",\"name\":\"\",\"el\":\"\",\"role\":\"\"}],\"skills\":[],\"effects\":[]}");
            Assert.Equal("冰刃", Catalog.MustChar("C001").Name);
            Assert.Equal(Element.Fire, Catalog.MustChar("C001").Element);
            Assert.Equal(Role.Attacker, Catalog.MustChar("C001").Role);
            Assert.Equal(25, Catalog.PlayableIds.Length);
            Assert.Equal("C011", Catalog.PlayableAt(Element.Fire, Role.Supporter).Id);
            Assert.Equal("C025", Catalog.PlayableAt(Element.Dark, Role.Supporter).Id);
            Assert.True(Catalog.Skills.ContainsKey("C001_tap"));
        }

        [Fact]
        public void CatalogJsonRoundtrip()
        {
            Catalog.BuildBuiltin();
            var json = CatalogJson.Serialize();
            Assert.Contains("\"id\":\"C001\"", json);
            Assert.Contains("\"stages\":[", json);
            Catalog.BuildBuiltin();
            CatalogJson.Load(json);
            Assert.Equal(2200, Catalog.MustChar("C001").Hp);
            Assert.Equal("冰刃", Catalog.MustChar("C001").Name);
            Assert.True(Catalog.Skills.ContainsKey("C001_tap"));
            Assert.Equal(12, Catalog.Stages.Length);
            Assert.Equal("VS-1", Catalog.Stages[0].Id);
            Assert.Equal(Element.Fire, Catalog.MustChar("C001").Element);
            Assert.Equal(Role.Attacker, Catalog.MustChar("C001").Role);
        }

        [Fact]
        public void CatalogJsonSerializeRoundtripKeepsOpcode()
        {
            var catalogPath = CatalogJson.FindCatalogPath();
            DateTime? stamp = null;
            if (!string.IsNullOrEmpty(catalogPath) && File.Exists(catalogPath))
                stamp = File.GetLastWriteTimeUtc(catalogPath);

            Catalog.BuildBuiltin();
            Catalog.MustSkill("C001_tap").Opcode = EffectOpcodes.DmgFeverParts;
            Catalog.TryEffect("stun").Opcode = EffectOpcodes.PoisonApply;
            var json = CatalogJson.Serialize();
            Assert.Contains("\"op\":\"" + EffectOpcodes.DmgFeverParts + "\"", json);
            Assert.Contains("\"op\":\"" + EffectOpcodes.PoisonApply + "\"", json);

            var tmp = Path.Combine(Path.GetTempPath(), "resonance-catalog-op-" + Guid.NewGuid().ToString("N") + ".json");
            try
            {
                File.WriteAllText(tmp, json);
                Catalog.BuildBuiltin();
                Assert.Equal(EffectOpcodes.DmgTap, Catalog.MustSkill("C001_tap").Opcode);
                CatalogJson.Load(File.ReadAllText(tmp));
                Assert.Equal(EffectOpcodes.DmgFeverParts, Catalog.MustSkill("C001_tap").Opcode);
                Assert.Equal(EffectOpcodes.PoisonApply, Catalog.TryEffect("stun").Opcode);
            }
            finally
            {
                if (File.Exists(tmp)) File.Delete(tmp);
            }

            if (stamp.HasValue)
                Assert.Equal(stamp.Value, File.GetLastWriteTimeUtc(catalogPath));
        }

        [Fact]
        public void SameSeedSameLog()
        {
            var a = RunAuto(42, 200);
            var b = RunAuto(42, 200);
            Assert.Equal(a, b);
        }

        [Fact]
        public void TapAddsSixDrive()
        {
            var sim = NewSim(7);
            ChargeAll(sim);
            Assert.True(sim.TryTap(0));
            Assert.Equal(6f, sim.Drive);
        }

        [Fact]
        public void SlideAddsFourteenDrive()
        {
            var sim = NewSim(8);
            ChargeAll(sim);
            Assert.True(sim.TrySlide(0));
            Assert.Equal(14f, sim.Drive);
        }

        [Fact]
        public void PerfectDrivesReachFever()
        {
            var sim = NewSim(9);
            for (int n = 0; n < 4 && !sim.FeverActive; n++)
            {
                ChargeAll(sim);
                sim.Drive = 100f;
                Assert.True(sim.TryBeginDrive(0));
                Assert.True(sim.ResolveDrive(DriveTiming.Perfect));
            }
            Assert.True(sim.FeverActive);
            Assert.True(sim.FeverEver);
            Assert.Equal(70, sim.FeverHitsLeft);
        }

        [Fact]
        public void FourGreatDrivesTriggerFever()
        {
            // Primary tip: Great +30 → four Greats = 120.
            var sim = NewSim(13);
            for (int n = 0; n < 4; n++)
            {
                ChargeAll(sim);
                sim.Drive = 100f;
                Assert.True(sim.TryBeginDrive(0));
                Assert.True(sim.ResolveDrive(DriveTiming.Great));
            }
            Assert.True(sim.FeverActive);
            Assert.True(sim.FeverEver);
        }

        [Fact]
        public void AutoTapReachesFever()
        {
            // Full auto Drive QTE resolves Great (+30 tip) inside AutoFireDrive.
            var sim = NewSim(21);
            sim.Auto = AutoMode.Full;
            sim.TimeLeft = 999f;
            for (int i = 0; i < sim.Enemies.Count; i++)
            {
                if (sim.Enemies[i] == null) continue;
                sim.Enemies[i].MaxHp = 500000;
                sim.Enemies[i].Hp = 500000;
            }
            float gauge = 0f;
            for (int n = 0; n < 4; n++)
            {
                // C2: Full-auto Drive / ally Slide start a core tick-budget hold. Drain it
                // (ticks advance, clocks do not) before asserting the next Drive round.
                while (sim.PolicyHoldTicksLeft > 0 && sim.Outcome == BattleOutcome.InProgress)
                    sim.Tick();
                ChargeAll(sim);
                sim.Drive = 100f;
                sim.Outcome = BattleOutcome.InProgress;
                Assert.True(sim.CanAct(0), "ally0 cannot act before auto drive");
                var before = sim.FeverGauge;
                sim.Tick();
                Assert.True(sim.FeverGauge > before || sim.FeverEver,
                    "tick n=" + n + " feverG=" + sim.FeverGauge + " event=" + sim.LastEvent + " pending=" + sim.PendingDriveSlot);
                if (sim.FeverEver) break;
                gauge = sim.FeverGauge;
            }
            Assert.True(sim.FeverEver, "feverG stuck at " + gauge);
        }

        [Fact]
        public void FeverKeepsTickingAfterVictory()
        {
            var sim = NewSim(14);
            for (int n = 0; n < 3; n++)
            {
                ChargeAll(sim);
                sim.Drive = 100f;
                Assert.True(sim.TryBeginDrive(0));
                Assert.True(sim.ResolveDrive(DriveTiming.Perfect));
            }
            Assert.True(sim.FeverActive);
            sim.Outcome = BattleOutcome.Victory;
            var left = sim.FeverLeft;
            sim.Tick();
            Assert.Equal(left, sim.FeverLeft);
            sim.TickFeverOnly();
            Assert.True(sim.FeverLeft < left);
            Assert.True(sim.FeverActive);
        }

        [Fact]
        public void AutoBattleDealsDamageOrFinishes()
        {
            var sim = NewSim(11);
            sim.AutoTap = true;
            var start = EnemyHp(sim);
            for (int i = 0; i < BattleSim.TickHz * 40; i++) sim.Tick();
            var end = EnemyHp(sim);
            Assert.True(end < start || sim.Outcome != BattleOutcome.InProgress);
        }

        [Fact]
        public void SaveRoundtrip()
        {
            var blob = new SaveBlob { LeaderSlot = 2, Vs1Cleared = true, Speed = 2, AutoTap = true };
            blob.PartyIds = new[] { "C006", "C002", "C008", "C009", "C012" };
            blob.ClearedCount = 4;
            var u = blob.GetUnit("C006");
            u.Level = 12;
            u.Uncap = 2;
            u.Ignition = 3;
            u.SkinId = "echo";
            u.Gear0 = "EQ_WPN";
            var parsed = SaveStore.Parse(SaveStore.Serialize(blob));
            Assert.Equal(2, parsed.LeaderSlot);
            Assert.True(parsed.Vs1Cleared);
            Assert.Equal(4, parsed.ClearedCount);
            Assert.Equal(2, parsed.Speed);
            Assert.True(parsed.AutoTap);
            Assert.Equal("C006", parsed.PartyIds[0]);
            Assert.Equal("C012", parsed.PartyIds[4]);
            var pu = parsed.GetUnit("C006");
            Assert.Equal(12, pu.Level);
            Assert.Equal(2, pu.Uncap);
            Assert.Equal(3, pu.Ignition);
            Assert.Equal("echo", pu.SkinId);
            Assert.Equal("EQ_WPN", pu.Gear0);
        }

        [Fact]
        public void TwentyFivePlayableAndTwelveStages()
        {
            Catalog.BuildBuiltin();
            Assert.Equal(25, Catalog.PlayableIds.Length);
            Assert.Equal(12, Catalog.Stages.Length);
            Assert.Equal("VS-1", Catalog.Stages[0].Id);
            Assert.Equal("CH1-12", Catalog.Stages[11].Id);
        }

        [Fact]
        public void GrowthAndGearRaiseStats()
        {
            Catalog.BuildBuiltin();
            var src = Catalog.MustChar("C001");
            var grown = Growth.Apply(src, new UnitProgress { Id = "C001", Level = 20, Uncap = 2, Ignition = 4, Gear0 = "EQ_WPN" });
            Assert.True(grown.Hp > src.Hp);
            Assert.True(grown.Atk > src.Atk + 70);
            Assert.Equal(src.Hp, Catalog.MustChar("C001").Hp);
        }

        [Fact]
        public void LaterStageEnemiesAreTougher()
        {
            Catalog.BuildBuiltin();
            var a = new BattleSim(Catalog.DefaultParty, 0, 3, Catalog.Stages[0], null) { Deterministic = true, Profile = FormulaProfile.JP_LEGACY_EMPIRICAL };
            var b = new BattleSim(Catalog.DefaultParty, 0, 3, Catalog.Stages[11], null) { Deterministic = true, Profile = FormulaProfile.JP_LEGACY_EMPIRICAL };
            Assert.True(b.Enemies[0].MaxHp > a.Enemies[0].MaxHp);
            Assert.True(b.Enemies[0].Def.Atk > a.Enemies[0].Def.Atk);
        }

        [Fact]
        public void WikiSsFormula()
        {
            var d = DamageMath.ComputeSs(0, 2707, 2500, 1.4f, 1f, 0, 0, false, 0);
            Assert.Equal(1684, d);
        }

        [Fact]
        public void SlideUsesSsNotTs()
        {
            var tap = DamageMath.ComputeSkill(SkillType.Tap, 1000, 1f, 707, 2500, Element.Fire, Element.Wood, false, 1f, 1f);
            var slide = DamageMath.ComputeSkill(SkillType.Slide, 1000, 1f, 707, 2500, Element.Fire, Element.Wood, false, 1f, 1f);
            Assert.Equal(1233, tap);
            Assert.Equal(1062, slide);
        }

        [Fact]
        public void ExtraDmgMulIsOnePlusClamped()
        {
            Assert.Equal(1f, DamageMath.ExtraDmgMul(0f));
            Assert.Equal(1.3f, DamageMath.ExtraDmgMul(0.3f));
            Assert.Equal(DamageMath.ExtraDmgMulFloor, DamageMath.ExtraDmgMul(-1f));
            var core = DamageMath.ComputeTs(1000, 2707, 2500, 1.4f, 1f, 1f, 0, 0, false, 0);
            var boosted = DamageMath.ComputeTs(1000, 2707, 2500, 1.4f, 1.3f, 1f, 0, 0, false, 0);
            Assert.Equal(2182, core);
            Assert.Equal(2836, boosted);
        }

        [Fact]
        public void WikiTsTruePierceAddsInsideBrackets()
        {
            Assert.Equal(650, DamageMath.TruePierce(1000, 2500));
            var d = DamageMath.ComputeTs(1000, 2707, 2500, 1.4f, 1f, 1f, 650, 0, false, 0);
            Assert.Equal(2832, d);
        }

        [Fact]
        public void WikiTsPercentFormula()
        {
            var d = DamageMath.ComputeTsPercent(0, 1000, 1f, 2500, 1.4f, 1f, 1f, 0, 0, false, 0);
            Assert.Equal(226, d);
            var viaSkill = DamageMath.ComputeSkill(SkillType.Tap, 1000, 0f, 0, 2500, Element.Fire, Element.Wood, false, 1f, 1f, 1f, 1f);
            Assert.Equal(226, viaSkill);
        }

        [Fact]
        public void WikiSsPercentFormula()
        {
            var d = DamageMath.ComputeSsPercent(0, 1000, 1f, 2500, 1.4f, 1f, 0, 0, false, 0);
            Assert.Equal(187, d);
            var viaSkill = DamageMath.ComputeSkill(SkillType.Slide, 1000, 0f, 0, 2500, Element.Fire, Element.Wood, false, 1f, 1f, 1f, 1f);
            Assert.Equal(187, viaSkill);
        }

        [Fact]
        public void SkillFlatAddsWithWikiWeight()
        {
            Assert.Equal(2707 + 1000, DamageMath.SkillDmg(0, 0f, 2707, 800f));
            var d = DamageMath.ComputeTs(0, 3707, 2500, 1.4f, 1f, 1f, 0, 0, false, 0);
            Assert.Equal(2679, d);
            var viaSkill = DamageMath.ComputeSkill(SkillType.Tap, 0, 0f, 2707, 2500, Element.Fire, Element.Wood, false, 1f, 1f, 1f, 0f, 800f);
            Assert.Equal(2679, viaSkill);
        }

        [Fact]
        public void FeverHitUsesFeverMulNotCoefScale()
        {
            var wiki = DamageMath.ComputeSkill(SkillType.Tap, 1000, 1f, 707, 2500, Element.Fire, Element.Wood, false, 1f, 1f, DamageMath.FeverMul);
            var oldSlice = DamageMath.ComputeSkill(SkillType.Tap, 1000, 0.6f, 707, 2500, Element.Fire, Element.Wood, false, 1f, 1f, 1f);
            Assert.NotEqual(oldSlice, wiki);
            Assert.Equal(DamageMath.ComputeTs(0, 1707, 2500, 1.4f, 1f, 0.6f, 0, 0, false, 0), wiki);
        }

        [Fact]
        public void SlideIgnoresFeverMul()
        {
            var a = DamageMath.ComputeSkill(SkillType.Slide, 1000, 1f, 707, 2500, Element.Fire, Element.Wood, false, 1f, 1f, 1f);
            var b = DamageMath.ComputeSkill(SkillType.Slide, 1000, 1f, 707, 2500, Element.Fire, Element.Wood, false, 1f, 1f, 0.6f);
            Assert.Equal(a, b);
            Assert.Equal(1062, a);
        }

        [Fact]
        public void BonusHitDoublesOnCrit()
        {
            var tap = DamageMath.ComputeTs(1000, 2707, 2500, 2.4f, 1f, 1f, 0, 100, true, 50);
            Assert.Equal(3740 + 200 + 50, tap);
            var slide = DamageMath.ComputeSs(0, 2707, 2500, 2.4f, 1f, 0, 100, true, 50);
            Assert.Equal(2887 + 200 + 50, slide);
        }

        [Fact]
        public void BattleSimSlideUsesElementMatchup()
        {
            Catalog.BuildBuiltin();
            var party = new[] { "C006", "C002", "C007", "C003", "C011" };
            var sim = new BattleSim(party, 0, 1, Catalog.Stages[0], null) { Deterministic = true, Speed = 1, Profile = FormulaProfile.JP_LEGACY_EMPIRICAL };
            ChargeAll(sim);
            Assert.True(sim.TrySlide(0));
            var caster = sim.Allies[0];
            var skill = Catalog.MustSkill(caster.Def.SlideSkillId);
            Assert.Equal(Element.Wood, caster.Def.Element);
            Assert.Equal(TargetRule.AllEnemies, skill.Target);
            Assert.Equal(3, sim.Enemies.Count);
            Assert.Equal(Element.Fire, sim.Enemies[0].Def.Element);
            Assert.Equal(Element.Wood, sim.Enemies[1].Def.Element);
            Assert.Equal(Element.Water, sim.Enemies[2].Def.Element);
            Assert.Equal(0.7f, DamageMath.ElementMultiplier(Element.Wood, Element.Fire));
            Assert.Equal(1.0f, DamageMath.ElementMultiplier(Element.Wood, Element.Wood));
            Assert.Equal(1.4f, DamageMath.ElementMultiplier(Element.Wood, Element.Water));
            for (int i = 0; i < sim.Enemies.Count; i++)
                Assert.Equal(ExpectedHit(caster, sim.Enemies[i], skill), sim.Enemies[i].MaxHp - sim.Enemies[i].Hp);
            var dFire = sim.Enemies[0].MaxHp - sim.Enemies[0].Hp;
            var dWood = sim.Enemies[1].MaxHp - sim.Enemies[1].Hp;
            var dWater = sim.Enemies[2].MaxHp - sim.Enemies[2].Hp;
            Assert.True(dWater > dWood, "wood→water 1.4");
            Assert.True(dFire < dWood, "wood→fire 0.7");
        }

        [Fact]
        public void BattleSimLightDarkMatchup()
        {
            Catalog.BuildBuiltin();
            var stage = new StageDef
            {
                Id = "EL-1",
                Name = "光暗",
                TimeLimitSec = 90f,
                Wave0 = new[] { "E004", "E001" },
                Wave1 = new[] { "EBOSS" }
            };
            var party = new[] { "C023", "C002", "C007", "C003", "C011" };
            var sim = G2ReviewFixtures.BindStripDotFlame(
                new BattleSim(party, 0, 1, stage, null) { Deterministic = true, Speed = 1, Profile = FormulaProfile.JP_LEGACY_EMPIRICAL });
            ChargeAll(sim);
            Assert.True(sim.TrySlide(0));
            var caster = sim.Allies[0];
            var skill = Catalog.MustSkill(caster.Def.SlideSkillId);
            Assert.Equal(Element.Dark, caster.Def.Element);
            Assert.Equal(Element.Light, sim.Enemies[0].Def.Element);
            Assert.Equal(Element.Fire, sim.Enemies[1].Def.Element);
            for (int i = 0; i < sim.Enemies.Count; i++)
                Assert.Equal(ExpectedHit(caster, sim.Enemies[i], skill), sim.Enemies[i].MaxHp - sim.Enemies[i].Hp);
            Assert.Equal(1.4f, DamageMath.ElementMultiplier(Element.Dark, Element.Light));
            Assert.Equal(1.0f, DamageMath.ElementMultiplier(Element.Dark, Element.Fire));
        }

        [Fact]
        public void WeakDefDownOnlyOnAdvantage()
        {
            var sim = NewSim(3);
            var wood = sim.Enemies[1];
            Assert.Equal(Element.Wood, wood.Def.Element);
            var fx = new EffectDef
            {
                Id = "weak_def",
                Opcode = EffectOpcodes.StatusApply,
                Kind = EffectKind.WeakDefDown,
                Magnitude = 0.20f,
                DurationSec = 10f,
                MaxStack = 1,
                SourceTier = 1,
                Group = "weakdef"
            };
            sim.ApplyStatus(wood, fx);
            Assert.Equal(wood.Def.Def, wood.Defense);
            Assert.Equal(wood.Def.Def, wood.DefenseAgainst(Element.Fire));
            Assert.Equal(0.20f, DamageMath.ExtraDmg(SkillType.Tap, true, 0f, 0f, 0f, 0f, 0.20f));
            Assert.Equal(0f, DamageMath.ExtraDmg(SkillType.Tap, false, 0f, 0f, 0f, 0f, 0.20f));
            Assert.Equal(1.20f, DamageMath.ExtraDmgMul(0.20f));
            var baseDmg = DamageMath.ComputeSkill(SkillType.Tap, 1000, 1f, 707, wood.Defense, Element.Fire, Element.Wood, false, 1f, 1f);
            var weakDmg = DamageMath.ComputeSkill(SkillType.Tap, 1000, 1f, 707, wood.Defense, Element.Fire, Element.Wood, false, 1.20f, 1f);
            Assert.True(weakDmg > baseDmg);
        }

        [Fact]
        public void IgnitionStopsAreSixNodes()
        {
            Assert.Equal(1, Growth.CycleIgnition(0, 12));
            Assert.Equal(2, Growth.CycleIgnition(1, 12));
            Assert.Equal(5, Growth.CycleIgnition(2, 12));
            Assert.Equal(8, Growth.CycleIgnition(5, 12));
            Assert.Equal(11, Growth.CycleIgnition(8, 12));
            Assert.Equal(12, Growth.CycleIgnition(11, 12));
            Assert.Equal(0, Growth.CycleIgnition(12, 12));
            Assert.Equal(0, Growth.IgnitionPips(0));
            Assert.Equal(6, Growth.IgnitionPips(12));
        }

        [Fact]
        public void CombatPowerUsesLockedWeights()
        {
            Catalog.BuildBuiltin();
            var d = Catalog.MustChar("C001");
            var expected = (int)Math.Round(d.Hp * 0.25 + d.Atk * 1.8 + d.Def * 1.3 + d.Agl * 0.8 + d.Crt * 0.7);
            Assert.Equal(expected, Growth.CombatPower(d));
        }

        [Fact]
        public void SaveRoundtripAffectionReserveHard()
        {
            var blob = new SaveBlob { UseHard = true, ClearedCount = 12, ClearedHard = 3 };
            var u = blob.GetUnit("C001");
            u.Affection = 80;
            u.Reserve = "TSTEE";
            var parsed = SaveStore.Parse(SaveStore.Serialize(blob));
            Assert.True(parsed.UseHard);
            Assert.Equal(12, parsed.ClearedCount);
            Assert.Equal(3, parsed.ClearedHard);
            var pu = parsed.GetUnit("C001");
            Assert.Equal(80, pu.Affection);
            Assert.Equal("TSTEE", pu.Reserve);
        }

        [Fact]
        public void HardUnlocksAfterChapterClear()
        {
            Catalog.BuildBuiltin();
            var blob = new SaveBlob { ClearedCount = 11, UseHard = true, ClearedHard = 0 };
            Assert.True(blob.IsStageLocked(0));
            blob.ClearedCount = 12;
            Assert.False(blob.IsStageLocked(0));
            Assert.True(blob.IsStageLocked(1));
            blob.ClearedHard = 1;
            Assert.False(blob.IsStageLocked(1));
            blob.UseHard = false;
            blob.ClearedCount = 0;
            Assert.False(blob.IsStageLocked(0));
            Assert.True(blob.IsStageLocked(1));
        }

        [Fact]
        public void HardStageEnemiesAreTougher()
        {
            Catalog.BuildBuiltin();
            var n = new BattleSim(Catalog.DefaultParty, 0, 3, Catalog.Stages[0], null) { Deterministic = true, Profile = FormulaProfile.JP_LEGACY_EMPIRICAL };
            var h = new BattleSim(Catalog.DefaultParty, 0, 3, Catalog.HardStages[0], null) { Deterministic = true, Profile = FormulaProfile.JP_LEGACY_EMPIRICAL };
            Assert.True(h.Enemies[0].MaxHp > n.Enemies[0].MaxHp);
            Assert.EndsWith("H", Catalog.HardStages[0].Id);
        }

        [Fact]
        public void AutoTapFollowsReservation()
        {
            Catalog.BuildBuiltin();
            var blob = new SaveBlob();
            blob.GetUnit(Catalog.DefaultParty[0]).Reserve = "STEEE";
            var sim = G2ReviewFixtures.BindStripDotFlame(
                new BattleSim(Catalog.DefaultParty, 0, 5, Catalog.Stages[0], blob.ProgressForParty())
                {
                    Deterministic = true,
                    Profile = FormulaProfile.JP_LEGACY_EMPIRICAL,
                    AutoTap = true
                });
            for (int i = 0; i < 5; i++)
                sim.Allies[i].Charge = i == 0 ? 100f : 0f;
            sim.Tick();
            Assert.Equal(14f, sim.Drive);
        }

        [Fact]
        public void TapAndSlideRecordDistinctCasts()
        {
            var sim = NewSim(11);
            ChargeAll(sim);
            Assert.True(sim.TryTap(0));
            ChargeAll(sim);
            Assert.True(sim.TrySlide(0));
            var tap = 0;
            var slide = 0;
            for (int i = 0; i < sim.Casts.Count; i++)
            {
                var fx = sim.Casts[i];
                if (!fx.CasterAlly || fx.Fever) continue;
                if (fx.Type == SkillType.Tap) tap++;
                if (fx.Type == SkillType.Slide) slide++;
            }
            Assert.True(tap >= 1, "tap=" + tap);
            Assert.True(slide >= 1, "slide=" + slide);
        }

        [Fact]
        public void TwelveKitsAreNotFiveTemplates()
        {
            Catalog.BuildBuiltin();
            Assert.Equal("直斩", Catalog.MustSkill("C001_tap").Name);
            Assert.NotEqual(Catalog.MustSkill("C001_tap").Name, Catalog.MustSkill("C006_tap").Name);
            Assert.NotEqual(Catalog.MustSkill("C001_slide").TargetCount, Catalog.MustSkill("C006_slide").TargetCount);
            Assert.NotEqual(Catalog.MustSkill("C003_drive").Name, Catalog.MustSkill("C009_drive").Name);
            Assert.NotEqual(Catalog.MustSkill("C002_slide").EffectId, Catalog.MustSkill("C007_slide").EffectId);
            Assert.Equal(TargetRule.AllEnemies, Catalog.MustSkill("C010_drive").Target);
        }

        [Fact]
        public void ChapterWavesDiffer()
        {
            Catalog.BuildBuiltin();
            Assert.Equal(12, Catalog.Stages.Length);
            Assert.Equal(3, Catalog.Stages[0].Wave0.Length);
            Assert.Equal(3, Catalog.Stages[2].Wave0.Length);
            Assert.NotEqual(string.Join(",", Catalog.Stages[0].Wave0), string.Join(",", Catalog.Stages[4].Wave0));
            Assert.Equal("EBOSS", Catalog.Stages[11].Wave1[0]);
        }

        [Fact]
        public void NewSaveSeedsStarterKit()
        {
            var path = Path.Combine(Path.GetTempPath(), "resonance-seed-" + Guid.NewGuid().ToString("N") + ".json");
            try
            {
                var b = SaveStore.LoadOrNew(path);
                var lead = b.GetUnit(b.PartyIds[0]);
                Assert.Equal("EQ_WPN", lead.Gear0);
                Assert.Equal(GearCatalog.DefaultCartaId, lead.Gear3);
                Assert.Equal("TSTEE", Growth.NormalizedReserve(lead.Reserve));
            }
            finally
            {
                WipeSave(path);
            }
        }

        [Fact]
        public void ExistingSaveGetsStarterWeaponIfEmpty()
        {
            var path = Path.Combine(Path.GetTempPath(), "resonance-mig-" + Guid.NewGuid().ToString("N") + ".json");
            var old = new SaveBlob { ClearedCount = 1 };
            old.GetUnit(old.PartyIds[0]).Affection = 8;
            SaveStore.Write(old, path);
            var loaded = SaveStore.LoadOrNew(path);
            var lead = loaded.GetUnit(loaded.PartyIds[0]);
            Assert.Equal("EQ_WPN", lead.Gear0);
            Assert.Equal(8, lead.Affection);
            Assert.Equal("TSTEE", Growth.NormalizedReserve(lead.Reserve));
            WipeSave(path);
        }

        [Fact]
        public void ClearRewardFillsEmptyGearSlot()
        {
            Catalog.BuildBuiltin();
            var b = new SaveBlob();
            var msg = SaveStore.ApplyClearReward(b, 0);
            Assert.Contains("残响刃", msg);
            Assert.Equal("EQ_WPN", b.GetUnit(b.PartyIds[0]).Gear0);
            Assert.Equal(2, b.GetUnit(b.PartyIds[0]).Level);
        }

        [Fact]
        public void SemiAutoDoesNotFireDrive()
        {
            Catalog.BuildBuiltin();
            var blob = new SaveBlob();
            SaveStore.EnsureStarterKit(blob);
            var sim = new BattleSim(blob.PartyIds, 0, 21, Catalog.Stages[11], blob.ProgressForParty())
            {
                Deterministic = true,
                Profile = FormulaProfile.JP_LEGACY_EMPIRICAL,
                Auto = AutoMode.Semi
            };
            ChargeAll(sim);
            sim.Drive = 100f;
            sim.Tick();
            var driveCasts = 0;
            for (int i = 0; i < sim.Casts.Count; i++)
                if (sim.Casts[i].CasterAlly && sim.Casts[i].Type == SkillType.Drive) driveCasts++;
            Assert.Equal(0, driveCasts);
            Assert.Equal(-1, sim.PendingDriveSlot);
            Assert.Equal(100f, sim.Drive);
        }

        [Fact]
        public void TryBeginDriveSelectsThatSlot()
        {
            var sim = NewSim(3);
            ChargeAll(sim);
            sim.Drive = 100f;
            Assert.True(sim.TryBeginDrive(2));
            Assert.Equal(2, sim.PendingDriveSlot);
            Assert.False(sim.TryBeginDrive(0));
        }

        [Fact]
        public void PerfectDriveIsOneFifty()
        {
            Assert.Equal(1.50f, BattleSim.QteMul(DriveTiming.Perfect));
            Assert.Equal(1.20f, BattleSim.QteMul(DriveTiming.Great));
            Assert.Equal(1.00f, BattleSim.QteMul(DriveTiming.Good));
            Assert.Equal(0.90f, BattleSim.QteMul(DriveTiming.Bad));
        }

        [Fact]
        public void QteFeverMatchesPrimaryP0Tip()
        {
            // Primary handoff P0 tip ~t445: PERFECT +40 / GREAT +30 / GOOD +15.
            Assert.Equal(40f, BattleSim.QteFever(DriveTiming.Perfect));
            Assert.Equal(30f, BattleSim.QteFever(DriveTiming.Great));
            Assert.Equal(15f, BattleSim.QteFever(DriveTiming.Good));
            Assert.Equal(8f, BattleSim.QteFever(DriveTiming.Bad));
            Assert.Equal(14f, BattleSim.UnknownFeverWindowSec);
            Assert.Equal(7f, BattleSim.DriveQteTimeoutSec);
        }

        [Fact]
        public void PortraitTapStartsDriveWhenGaugeFull()
        {
            var sim = NewSim(4);
            ChargeAll(sim);
            sim.Drive = 100f;
            Assert.True(sim.TryPortraitTap(1));
            Assert.Equal(1, sim.PendingDriveSlot);
        }

        [Fact]
        public void PortraitTapIsTapWhenDriveEmpty()
        {
            var sim = NewSim(5);
            ChargeAll(sim);
            sim.Drive = 0f;
            Assert.True(sim.TryPortraitTap(0));
            Assert.Equal(-1, sim.PendingDriveSlot);
            Assert.Equal(0f, sim.Allies[0].Charge);
        }

        [Fact]
        public void OldAutoTrueSaveBecomesFull()
        {
            var parsed = SaveStore.Parse("{\"party\":[\"C001\",\"C007\",\"C010\",\"C003\",\"C005\"],\"auto\":true}");
            Assert.Equal(AutoMode.Full, parsed.Auto);
            Assert.True(parsed.AutoTap);
            var semi = new SaveBlob { Auto = AutoMode.Semi };
            var round = SaveStore.Parse(SaveStore.Serialize(semi));
            Assert.Equal(AutoMode.Semi, round.Auto);
        }

        [Fact]
        public void AutoTapFiresDriveWhenReady()
        {
            Catalog.BuildBuiltin();
            var blob = new SaveBlob();
            SaveStore.EnsureStarterKit(blob);
            var sim = new BattleSim(blob.PartyIds, 0, 21, Catalog.Stages[11], blob.ProgressForParty())
            {
                Deterministic = true,
                Profile = FormulaProfile.JP_LEGACY_EMPIRICAL,
                AutoTap = true
            };
            ChargeAll(sim);
            sim.Drive = 100f;
            sim.Tick();
            var driveCasts = 0;
            for (int i = 0; i < sim.Casts.Count; i++)
                if (sim.Casts[i].CasterAlly && sim.Casts[i].Type == SkillType.Drive) driveCasts++;
            Assert.True(driveCasts >= 1, "auto never fired drive");
        }

        [Fact]
        public void NakedTeamClearsOpeningNotFinale()
        {
            Catalog.BuildBuiltin();
            var cap = BattleSim.TickHz * 100;
            var open = new BattleSim(Catalog.DefaultParty, 0, 7, Catalog.Stages[0], null)
            {
                Deterministic = true,
                Profile = FormulaProfile.JP_LEGACY_EMPIRICAL,
                AutoTap = true
            };
            for (int i = 0; i < cap && open.Outcome == BattleOutcome.InProgress; i++)
                open.Tick();
            Assert.Equal(BattleOutcome.Victory, open.Outcome);

            var finale = new BattleSim(Catalog.DefaultParty, 0, 7, Catalog.Stages[11], null)
            {
                Deterministic = true,
                Profile = FormulaProfile.JP_LEGACY_EMPIRICAL,
                AutoTap = true
            };
            for (int i = 0; i < cap && finale.Outcome == BattleOutcome.InProgress; i++)
                finale.Tick();
            Assert.NotEqual(BattleOutcome.Victory, finale.Outcome);
        }

        [Fact]
        public void MvpChapterClearsOnAuto()
        {
            var blob = new SaveBlob();
            Assert.True(MvpLoop.TryClearChapter(blob, false, 42, out var report), report);
            Assert.Equal(12, blob.ClearedCount);
            Assert.True(blob.GetUnit(blob.PartyIds[0]).Level >= 12);
            Assert.True(blob.GetUnit(blob.PartyIds[0]).Affection >= 40);
        }

        [Fact]
        public void AutoPlayCompletesStageWithAllVerbs()
        {
            Catalog.BuildBuiltin();
            var blob = new SaveBlob();
            SaveStore.EnsureStarterKit(blob);
            var sim = new BattleSim(blob.PartyIds, 0, 42, Catalog.Stages[0], blob.ProgressForParty())
            {
                Deterministic = true,
                Profile = FormulaProfile.JP_LEGACY_EMPIRICAL,
                AutoTap = true,
                Speed = 1
            };
            sim.HoldSim = true;
            ChargeAll(sim);
            sim.Drive = 100f;
            sim.FeverGauge = 50f;
            Assert.True(sim.TryBeginDrive(0));
            var cap = BattleSim.TickHz * 90;
            var ticks = MvpLoop.PlayOut(sim, cap);
            Assert.True(ticks < cap, "auto hung ticks=" + ticks);
            Assert.Equal(BattleOutcome.Victory, sim.Outcome);
            Assert.True(sim.FeverEver);
            int tap = 0, slide = 0, drive = 0, fever = 0;
            for (int i = 0; i < sim.Casts.Count; i++)
            {
                var fx = sim.Casts[i];
                if (!fx.CasterAlly) continue;
                if (fx.Fever) { fever++; continue; }
                if (fx.Type == SkillType.Tap) tap++;
                else if (fx.Type == SkillType.Slide) slide++;
                else if (fx.Type == SkillType.Drive) drive++;
            }
            Assert.True(tap >= 1, "tap=" + tap);
            Assert.True(slide >= 1, "slide=" + slide);
            Assert.True(drive >= 1, "drive=" + drive);
            Assert.True(fever >= 1, "fever=" + fever);
        }

        [Fact]
        public void FullAutoHonorsHoldSim()
        {
            var sim = NewSim(3);
            sim.AutoTap = true;
            sim.HoldSim = true;
            var t = sim.TimeLeft;
            sim.Tick();
            Assert.Equal(t, sim.TimeLeft);
            Assert.True(sim.HoldSim);
        }

        [Fact]
        public void ManualDriveQteTimesOut()
        {
            var sim = NewSim(4);
            ChargeAll(sim);
            sim.Drive = 100f;
            Assert.True(sim.TryBeginDrive(0));
            Assert.Equal(0, sim.PendingDriveSlot);
            var cap = (int)(BattleSim.DriveQteTimeoutSec * BattleSim.TickHz) + 2;
            for (int i = 0; i < cap; i++) sim.Tick();
            Assert.Equal(-1, sim.PendingDriveSlot);
            var driveCasts = 0;
            for (int i = 0; i < sim.Casts.Count; i++)
                if (sim.Casts[i].CasterAlly && sim.Casts[i].Type == SkillType.Drive) driveCasts++;
            Assert.True(driveCasts >= 1);
        }

        [Fact]
        public void HoldSimWatchdogReleasesManual()
        {
            var sim = NewSim(5);
            sim.HoldSim = true;
            var t = sim.TimeLeft;
            var cap = (int)(BattleSim.HoldTimeoutSec * BattleSim.TickHz) + 2;
            for (int i = 0; i < cap; i++) sim.Tick();
            Assert.False(sim.HoldSim);
            Assert.True(sim.TimeLeft < t);
        }

        [Fact]
        public void ResetWipesProgressAndReseedsKit()
        {
            var path = Path.Combine(Path.GetTempPath(), "resonance-reset-" + Guid.NewGuid().ToString("N") + ".json");
            var old = new SaveBlob { ClearedCount = 9 };
            old.GetUnit(old.PartyIds[0]).Level = 12;
            SaveStore.Write(old, path);
            var fresh = SaveStore.Reset(path);
            Assert.Equal(0, fresh.ClearedCount);
            Assert.Equal(1, fresh.GetUnit(fresh.PartyIds[0]).Level);
            Assert.Equal("EQ_WPN", fresh.GetUnit(fresh.PartyIds[0]).Gear0);
            WipeSave(path);
        }

        [Fact]
        public void PuppetCatalogIsCleanRoom220()
        {
            Assert.Equal(220, PuppetCatalog.All.Length);
            Assert.Equal(220, PuppetCatalog.Count);
            var rare = new int[5];
            var el = new int[5];
            var untyped = 0;
            var tap = 0;
            var slide = 0;
            var drive = 0;
            var leader = 0;
            var ids = new HashSet<string>();
            var names = new HashSet<string>();
            var banned = new[] { "摩根", "夏娃", "朱庇特", "嫦娥", "梅杜莎", "克丽欧", "玛门", "报丧", "自请妃", "玛亚特" };
            for (int i = 0; i < PuppetCatalog.All.Length; i++)
            {
                var p = PuppetCatalog.All[i];
                Assert.True(ids.Add(p.Id));
                Assert.True(names.Add(p.Name));
                Assert.Equal("P" + (i + 1).ToString("D3"), p.Id);
                Assert.StartsWith("契偶·", p.Name);
                for (int b = 0; b < banned.Length; b++)
                    Assert.DoesNotContain(banned[b], p.Name);
                rare[(int)p.Rarity]++;
                if (p.Untyped) untyped++;
                else el[(int)p.Element]++;
                if (p.Tap) tap++;
                if (p.Slide) slide++;
                if (p.Drive) drive++;
                if (p.Leader) leader++;
            }
            Assert.Equal(35, rare[(int)PuppetRarity.Common]);
            Assert.Equal(52, rare[(int)PuppetRarity.Uncommon]);
            Assert.Equal(66, rare[(int)PuppetRarity.Rare]);
            Assert.Equal(55, rare[(int)PuppetRarity.Epic]);
            Assert.Equal(12, rare[(int)PuppetRarity.Legendary]);
            Assert.Equal(43, el[(int)Element.Fire]);
            Assert.Equal(45, el[(int)Element.Water]);
            Assert.Equal(44, el[(int)Element.Wood]);
            Assert.Equal(42, el[(int)Element.Light]);
            Assert.Equal(44, el[(int)Element.Dark]);
            Assert.Equal(2, untyped);
            Assert.Equal(83, tap);
            Assert.Equal(104, slide);
            Assert.Equal(56, drive);
            Assert.Equal(49, leader);
        }

        [Fact]
        public void NewSaveOwnsThreePuppets()
        {
            var path = Path.Combine(Path.GetTempPath(), "resonance-puppets-" + Guid.NewGuid().ToString("N") + ".json");
            var b = SaveStore.LoadOrNew(path);
            Assert.Equal(3, b.Puppets.Count);
            Assert.Equal("P001", b.Puppets[0]);
            Assert.Equal("P002", b.Puppets[1]);
            Assert.Equal("P003", b.Puppets[2]);
            Assert.NotNull(PuppetCatalog.Try(b.Puppets[0]));
            WipeSave(path);
        }

        [Fact]
        public void PuppetOwnCapsAtThreeAndRoundtrips()
        {
            var blob = new SaveBlob();
            Assert.True(PuppetCatalog.TryOwn(blob.Puppets, "P010"));
            Assert.True(PuppetCatalog.TryOwn(blob.Puppets, "P011"));
            Assert.True(PuppetCatalog.TryOwn(blob.Puppets, "P012"));
            Assert.False(PuppetCatalog.TryOwn(blob.Puppets, "P013"));
            Assert.False(PuppetCatalog.TryOwn(blob.Puppets, "593126"));
            Assert.False(PuppetCatalog.TryOwn(blob.Puppets, "女仆摩根"));
            var parsed = SaveStore.Parse(SaveStore.Serialize(blob));
            Assert.Equal(new[] { "P010", "P011", "P012" }, parsed.Puppets.ToArray());
        }

        [Fact]
        public void ExistingSaveGetsStarterPuppetsIfEmpty()
        {
            var path = Path.Combine(Path.GetTempPath(), "resonance-pupmig-" + Guid.NewGuid().ToString("N") + ".json");
            var old = new SaveBlob { ClearedCount = 1 };
            old.GetUnit(old.PartyIds[0]).Affection = 8;
            SaveStore.Write(old, path);
            var loaded = SaveStore.LoadOrNew(path);
            Assert.Equal(3, loaded.Puppets.Count);
            Assert.Equal(8, loaded.GetUnit(loaded.PartyIds[0]).Affection);
            WipeSave(path);
        }

        [Fact]
        public void ApplyVictoryUnlocksNextStage()
        {
            Catalog.BuildBuiltin();
            var b = new SaveBlob();
            SaveStore.ApplyVictory(b, 0, false);
            Assert.Equal(1, b.ClearedCount);
            Assert.False(b.IsStageLocked(1));
            Assert.True(b.IsStageLocked(2));
        }

        [Fact]
        public void CartaCatalogCoversOneFiveSix()
        {
            Assert.Equal(156, GearCatalog.Cartas.Length);
            Assert.Equal(156, Catalog.Cartas.Length);
            var s5 = 0;
            var s4 = 0;
            var s3 = 0;
            var names = new HashSet<string>();
            for (int i = 0; i < GearCatalog.Cartas.Length; i++)
            {
                var g = GearCatalog.Cartas[i];
                Assert.Equal("SC" + (i + 1).ToString("000"), g.Id);
                Assert.Equal(GearCatalog.CartaSlot, g.Slot);
                Assert.False(string.IsNullOrEmpty(g.Name));
                Assert.True(names.Add(g.Name), g.Name);
                if (g.Star == 5) s5++;
                else if (g.Star == 4) s4++;
                else if (g.Star == 3) s3++;
            }
            Assert.Equal(134, s5);
            Assert.Equal(13, s4);
            Assert.Equal(9, s3);
            Assert.Equal("SC006", GearCatalog.DefaultCartaId);
            var fire = GearCatalog.Try(GearCatalog.DefaultCartaId);
            Assert.Equal("火", fire.ElementGate);
            Assert.True(fire.Atk > 0);
            Assert.True(fire.Hp > 0);
            Assert.DoesNotContain("炎热夏日", fire.Name);
            Assert.DoesNotContain("莉莎", fire.Name);
            Assert.Equal("焰生锋五围", fire.Name);
        }

        [Fact]
        public void C001CartaRaisesBattleAtkAndSaves()
        {
            Catalog.BuildBuiltin();
            var path = Path.Combine(Path.GetTempPath(), "resonance-carta-" + Guid.NewGuid().ToString("N") + ".json");
            var b = new SaveBlob();
            SaveStore.EnsureStarterKit(b);
            var u = b.GetUnit("C001");
            Assert.Equal(GearCatalog.DefaultCartaId, u.Gear3);
            var carta = GearCatalog.Try(u.Gear3);
            var src = Catalog.MustChar("C001");
            var only = Growth.Apply(src, new UnitProgress { Id = "C001", Gear3 = u.Gear3 });
            Assert.Equal(src.Atk + carta.Atk, only.Atk);
            Assert.Equal(src.Hp + carta.Hp, only.Hp);
            var grown = Growth.Apply(src, u);
            Assert.True(grown.Atk > src.Atk + carta.Atk);
            SaveStore.Write(b, path);
            var loaded = SaveStore.LoadOrNew(path);
            Assert.Equal(GearCatalog.DefaultCartaId, loaded.GetUnit("C001").Gear3);
            WipeSave(path);
        }

        [Fact]
        public void DefaultPathIsLocalLowTitleSave()
        {
            var appData = Path.GetDirectoryName(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));
            var expected = Path.GetFullPath(Path.Combine(appData, "LocalLow", "Resonance", "契灵回响", "save.json"));
            Assert.Equal(expected, Path.GetFullPath(SaveStore.DefaultPath));
        }

        [Fact]
        public void DiskKeepsPartyStageGear()
        {
            var path = Path.Combine(Path.GetTempPath(), "resonance-disk-" + Guid.NewGuid().ToString("N") + ".json");
            try
            {
                var b = new SaveBlob { ClearedCount = 7, ClearedHard = 2, UseHard = true, LeaderSlot = 3 };
                b.PartyIds = new[] { "C006", "C002", "C008", "C009", "C012" };
                var u = b.GetUnit("C006");
                u.Level = 9;
                u.Gear0 = "EQ_WPN";
                u.Gear1 = "EQ_ARM";
                u.Gear2 = "EQ_ACC";
                u.Gear3 = "EQ_CRD";
                SaveStore.Write(b, path);
                Assert.True(File.Exists(path));
                Assert.False(File.Exists(path + ".tmp"));
                var loaded = SaveStore.LoadOrNew(path);
                Assert.Equal(7, loaded.ClearedCount);
                Assert.Equal(2, loaded.ClearedHard);
                Assert.True(loaded.UseHard);
                Assert.Equal(3, loaded.LeaderSlot);
                Assert.Equal("C006", loaded.PartyIds[0]);
                Assert.Equal("C012", loaded.PartyIds[4]);
                var p = loaded.GetUnit("C006");
                Assert.Equal(9, p.Level);
                Assert.Equal("EQ_WPN", p.Gear0);
                Assert.Equal("EQ_ARM", p.Gear1);
                Assert.Equal("EQ_ACC", p.Gear2);
                Assert.Equal("EQ_CRD", p.Gear3);
            }
            finally
            {
                WipeSave(path);
            }
        }

        [Fact]
        public void GrowthPlusZeroDoesNotDoubleWeapon()
        {
            Catalog.BuildBuiltin();
            var src = Catalog.MustChar("C001");
            var wpn = GearCatalog.Try("EQ_WPN");
            Assert.Equal(220, wpn.Atk);
            var withPlus = Growth.Apply(src, new UnitProgress { Id = "C001", Gear0 = "EQ_WPN", Plus0 = 0 });
            var withoutPlus = Growth.Apply(src, new UnitProgress { Id = "C001", Gear0 = "EQ_WPN" });
            Assert.Equal(withoutPlus.Atk, withPlus.Atk);
            var gearAtk = withPlus.Atk - src.Atk;
            Assert.Equal(wpn.Atk, gearAtk);
            Assert.NotEqual(wpn.Atk * 2, gearAtk);
        }

        [Fact]
        public void GrowthPlusOneWeaponAdds18()
        {
            Catalog.BuildBuiltin();
            var src = Catalog.MustChar("C001");
            var zero = Growth.Apply(src, new UnitProgress { Id = "C001", Gear0 = "EQ_WPN", Plus0 = 0 });
            var one = Growth.Apply(src, new UnitProgress { Id = "C001", Gear0 = "EQ_WPN", Plus0 = 1 });
            Assert.Equal(zero.Atk + 18, one.Atk);
        }

        [Fact]
        public void SaveRoundtripPlus()
        {
            var path = Path.Combine(Path.GetTempPath(), "resonance-plus-" + Guid.NewGuid().ToString("N") + ".json");
            try
            {
                var b = new SaveBlob();
                var u = b.GetUnit("C001");
                u.Plus0 = 7;
                u.Plus3 = 3;
                SaveStore.Write(b, path);
                var loaded = SaveStore.LoadOrNew(path);
                var p = loaded.GetUnit("C001");
                Assert.Equal(7, p.Plus0);
                Assert.Equal(3, p.Plus3);
            }
            finally
            {
                WipeSave(path);
            }
        }

        [Fact]
        public void BattleSimDefaultExtraAtkIsZero()
        {
            var sim = NewSim(3);
            Assert.Equal(0, sim.Allies[0].ExtraAtk);
        }

        [Fact]
        public void FoodAtkMulOfNegativeIsOne()
        {
            Assert.Equal(1f, Food.AtkMulOf(-1));
            Assert.Equal(1.03f, Food.AtkMulOf(0));
        }

        [Fact]
        public void BattleSimFoodAtkMulRaisesAllyAtk()
        {
            Catalog.BuildBuiltin();
            var plain = new BattleSim(Catalog.DefaultParty, 0, 3, null, null, new BattleMods())
            {
                Deterministic = true,
                Profile = FormulaProfile.JP_LEGACY_EMPIRICAL,
                Speed = 1
            };
            var fed = new BattleSim(Catalog.DefaultParty, 0, 3, null, null, new BattleMods { FoodAtkMul = 1.03f })
            {
                Deterministic = true,
                Profile = FormulaProfile.JP_LEGACY_EMPIRICAL,
                Speed = 1
            };
            Assert.True(fed.Allies[0].Atk > plain.Allies[0].Atk);
        }

        [Fact]
        public void CorruptPrimaryFallsBackToBak()
        {
            var path = Path.Combine(Path.GetTempPath(), "resonance-bak-" + Guid.NewGuid().ToString("N") + ".json");
            try
            {
                var b = new SaveBlob { ClearedCount = 6, LeaderSlot = 1 };
                b.PartyIds = new[] { "C006", "C002", "C008", "C009", "C012" };
                b.GetUnit("C006").Gear0 = "EQ_WPN";
                SaveStore.Write(b, path);
                b.ClearedCount = 8;
                SaveStore.Write(b, path);
                File.WriteAllText(path, "{");
                var loaded = SaveStore.LoadOrNew(path);
                Assert.Equal(6, loaded.ClearedCount);
                Assert.Equal(1, loaded.LeaderSlot);
                Assert.Equal("C006", loaded.PartyIds[0]);
                Assert.Equal("EQ_WPN", loaded.GetUnit("C006").Gear0);
            }
            finally
            {
                WipeSave(path);
            }
        }

        static void WipeSave(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); } catch { }
            try { if (File.Exists(path + ".bak")) File.Delete(path + ".bak"); } catch { }
            try { if (File.Exists(path + ".tmp")) File.Delete(path + ".tmp"); } catch { }
        }

        static BattleSim NewSim(int seed)
        {
            return G2ReviewFixtures.BindStripDotFlame(new BattleSim(Catalog.DefaultParty, 0, seed)
            {
                Deterministic = true,
                Speed = 1,
                Profile = FormulaProfile.JP_LEGACY_EMPIRICAL
            });
        }

        static void ChargeAll(BattleSim sim)
        {
            for (int i = 0; i < sim.Allies.Length; i++)
                sim.Allies[i].Charge = 100f;
        }

        static int EnemyHp(BattleSim sim)
        {
            var n = 0;
            for (int i = 0; i < sim.Enemies.Count; i++) n += sim.Enemies[i].Hp;
            return n;
        }

        static int ExpectedHit(UnitState caster, UnitState target, SkillDef skill)
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
                false, extraMul, 1f, 1f,
                skill.PercentAtk, skill.SkillFlat);
            var hits = Math.Max(1, skill.HitCount);
            var total = dmg * hits;
            return Math.Min(total, target.MaxHp);
        }

        static string RunAuto(int seed, int ticks)
        {
            var sim = NewSim(seed);
            sim.AutoTap = true;
            for (int i = 0; i < ticks; i++) sim.Tick();
            return sim.Outcome + "/" + EnemyHp(sim) + "/" + sim.Drive + "/" + sim.TimeLeft;
        }
    }
}
