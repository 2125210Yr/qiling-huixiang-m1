using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Json;
using Resonance.Battle;
using Xunit;

namespace Resonance.Tests
{
    public sealed class OriginalExpeditionContentTests
    {
        [Theory]
        [InlineData("N1")]
        [InlineData("N2-backstage")]
        [InlineData("N2-audience")]
        [InlineData("N4")]
        [InlineData("N5")]
        [InlineData("N7")]
        public void EveryEncounterUsesSelfContainedPlayableOriginalContent(string node)
        {
            var input = RunBattleFactory.CreateInput(node, "single", 9101, new[] { "A01" }, null);
            Assert.Equal(5, input.PartyIds.Length);
            Assert.Equal(5, input.OpeningHp.Length);
            Assert.Equal(5, input.Growth.Length);
            Assert.All(input.Characters, c => Assert.StartsWith("OE_", c.Id));
            Assert.All(input.Skills, s => Assert.StartsWith("OE_", s.Id));
            Assert.All(input.Effects, e => Assert.StartsWith("OE_", e.Id));
            var effects = input.Effects.ToDictionary(e => e.Id);
            foreach (var skill in input.Skills) Assert.True(EffectCapability.CheckSkill(skill, effects).Ok, skill.Id);
            foreach (var id in input.PartyIds.Concat(input.Stage.Wave0))
                Assert.Contains(input.Characters, c => c.Id == id);
            Assert.Empty(input.Stage.Wave1);
            Assert.False(input.EnableDrive);
            Assert.False(input.EnableFever);
            Assert.Equal(FormulaProfile.JP_LEGACY_EMPIRICAL, input.Profile);
            Assert.Equal(ExpeditionContent.ContentHash, input.ContentHash);
        }

        [Fact]
        public void DeepCloneSeversAllMutableArraysAndNestedDefinitions()
        {
            var input = RunBattleFactory.CreateInput("N7", "single", 2, new[] { "A01" }, null);
            input.Skills[0].RequireTags = new[] { "frozen" };
            var copy = input.DeepClone();
            input.Stage.Wave0[0] = "mutated";
            input.Characters[0].Hp = 1;
            input.Skills[0].RequireTags[0] = "mutated";
            input.Skills[0].FlatPower = 99999;
            input.Effects[0].Magnitude = 999;
            input.PartyIds[0] = "mutated";
            input.OpeningHp[0] = 0;
            input.Growth[0].Level = 60;
            input.Mods.FoodAtkMul = 10;
            input.Clocks.SlideCdSec = 999;
            input.RelicIds[0] = "B01";
            input.RelicParameters.ScatterRatio = 10;
            input.Boss.MaskSlots[0] = 77;
            input.Boss.CastDurationSec = 999;
            Assert.Equal("OE_BOSS", copy.Stage.Wave0[0]);
            Assert.True(copy.Characters[0].Hp > 1);
            Assert.Equal("frozen", copy.Skills[0].RequireTags[0]);
            Assert.NotEqual(99999, copy.Skills[0].FlatPower);
            Assert.NotEqual(999, copy.Effects[0].Magnitude);
            Assert.StartsWith("OE_", copy.PartyIds[0]);
            Assert.True(copy.OpeningHp[0] > 0);
            Assert.Equal(1, copy.Growth[0].Level);
            Assert.Equal(1, copy.Mods.FoodAtkMul);
            Assert.NotEqual(999, copy.Clocks.SlideCdSec);
            Assert.Equal("A01", copy.RelicIds[0]);
            Assert.Equal(0.4f, copy.RelicParameters.ScatterRatio);
            Assert.Equal(1, copy.Boss.MaskSlots[0]);
            Assert.Equal(4f, copy.Boss.CastDurationSec);
        }

        [Fact]
        public void OpeningHpPreservesDownedMembersAndRejectsInvalidState()
        {
            var hp = ExpeditionContent.GetPartyMaxHp("single");
            hp[2] = 0;
            var input = RunBattleFactory.CreateInput("N4", "single", 3, new[] { "B01" }, hp);
            hp[0] = 0;
            Assert.True(input.OpeningHp[0] > 0);
            Assert.Equal(0, input.OpeningHp[2]);
            Assert.Throws<ArgumentException>(() => RunBattleFactory.CreateInput("N1", "single", 3, null, new int[2]));
            var invalid = ExpeditionContent.GetPartyMaxHp("single"); invalid[0]++;
            Assert.Throws<ArgumentException>(() => RunBattleFactory.CreateInput("N1", "single", 3, null, invalid));
            invalid[0] = -1;
            Assert.Throws<ArgumentException>(() => RunBattleFactory.CreateInput("N1", "single", 3, null, invalid));
        }

        [Fact]
        public void EveryRelicHasAReachablePrerequisiteChainAndRejectsDuplicates()
        {
            Assert.Equal(12, ExpeditionContent.Relics.Length);
            Assert.Equal(12, ExpeditionContent.Relics.Select(r => r.Id).Distinct().Count());
            foreach (string family in new[] { "A", "B", "C" })
            {
                var owned = new HashSet<string>(StringComparer.Ordinal);
                var relics = ExpeditionContent.Relics.Where(r => r.Family == family).OrderBy(r => r.Id).ToArray();
                Assert.Equal(4, relics.Length);
                Assert.True(relics[0].IsCore);
                Assert.True(relics[0].IsEligible(owned));
                Assert.False(relics[3].IsEligible(owned));
                foreach (var relic in relics)
                {
                    Assert.True(relic.IsEligible(owned), relic.Id);
                    owned.Add(relic.Id);
                    Assert.False(relic.IsEligible(owned), relic.Id + " duplicate");
                }
                Assert.True(relics[3].IsAmplifier);
            }
            Assert.False(ExpeditionContent.FindRelic("C04").IsEligible(new[] { "C01", "C02" }));
            Assert.True(ExpeditionContent.FindRelic("C04").IsEligible(new[] { "C01", "C03" }));
        }

        [Fact]
        public void PresetIsEquivalentBudgetAndDoesNotModifyOtherBattles()
        {
            var single = RunBattleFactory.CreateInput("N1", "single", 4, null, null);
            var sweep = RunBattleFactory.CreateInput("N1", "sweep", 4, null, null);
            Assert.Equal(single.Characters.Select(c => c.Hp), sweep.Characters.Select(c => c.Hp));
            Assert.Equal(single.Characters.Select(c => c.Atk), sweep.Characters.Select(c => c.Atk));
            var a = single.Skills.Single(s => s.Id == "OE_POINT_tap");
            var b = sweep.Skills.Single(s => s.Id == "OE_POINT_tap");
            Assert.Equal(a.AtkCoef * a.TargetCount, b.AtkCoef * b.TargetCount);
            Assert.Equal(a.FlatPower * a.TargetCount, b.FlatPower * b.TargetCount);
            Assert.Equal(1, a.TargetCount);
            Assert.Equal(2, b.TargetCount);
            single.Characters[0].Hp = 1;
            single.Stage.Wave0[0] = "bad";
            Assert.True(sweep.Characters[0].Hp > 1);
            Assert.NotEqual("bad", sweep.Stage.Wave0[0]);
            Assert.True(ExpeditionContent.GetPartyMaxHp("single")[0] > 1);
        }

        [Fact]
        public void BossAndPresetAreSerializedAsRealInputs()
        {
            var input = RunBattleFactory.CreateInput("N7", "sweep", 411, new[] { "C01", "C03", "C04" }, null);
            input.RunId = "content-serialization-fixture";
            input.EncounterId = "content-serialization-fixture:N7:5";
            input.AttemptId = "attempt-1";
            input.BattleOrdinal = 5;
            var serializer = new DataContractJsonSerializer(typeof(ExpeditionBattleInput));
            using (var stream = new MemoryStream())
            {
                serializer.WriteObject(stream, input);
                stream.Position = 0;
                var restored = (ExpeditionBattleInput)serializer.ReadObject(stream);
                Assert.Equal(input.ContentHash, restored.ContentHash);
                Assert.Equal(input.EncounterId, restored.EncounterId);
                Assert.Equal("sweep", restored.PresetId);
                Assert.Equal(input.Boss.MaskSlots, restored.Boss.MaskSlots);
                Assert.Equal(1.2f, restored.RelicParameters.ForteDamageBonus);
                Assert.Equal(input.OpeningHp, restored.OpeningHp);
            }
        }

        [Fact]
        public void ContentFingerprintIncludesNumbersAndNestedConfiguration()
        {
            var input = RunBattleFactory.CreateInput("N7", "single", 1, null, null);
            string original = ExpeditionContent.Fingerprint(input);
            input.Boss.CastDurationSec += 0.25f;
            Assert.NotEqual(original, ExpeditionContent.Fingerprint(input));
            input.Boss.CastDurationSec -= 0.25f;
            Assert.Equal(original, ExpeditionContent.Fingerprint(input));
            input.RelicParameters.RelayCharge++;
            Assert.NotEqual(original, ExpeditionContent.Fingerprint(input));
        }

        [Fact]
        public void ValidateRejectsNonFiniteOrUnboundedCombatNumbers()
        {
            Action<ExpeditionBattleInput>[] invalid = {
                i => i.Skills[0].AtkCoef = float.NaN,
                i => i.Skills[0].HitCount = int.MaxValue,
                i => i.Skills[0].HealMaxHpFrac = -1,
                i => i.Effects[0].Magnitude = float.PositiveInfinity,
                i => i.Stage.EnemyHpMul = float.NaN,
                i => i.Stage.TimeLimitSec = float.PositiveInfinity,
                i => i.Clocks.SlideCdSec = float.NaN,
                i => i.Boss.CastDurationSec = 0,
                i => i.Boss.MaskSlots[0] = i.Boss.BossSlot,
                i => i.RelicParameters.ScatterRatio = float.PositiveInfinity,
                i => i.RelicParameters.HarmonyDistinctActors = 0
            };
            foreach (var corrupt in invalid)
            {
                var input = RunBattleFactory.CreateInput("N7", "single", 1, null, null);
                corrupt(input);
                Assert.Throws<ArgumentException>(() => RunBattleFactory.Validate(input));
            }
        }

        [Fact]
        public void ValidateRejectsMissingReferencesAndUnsupportedSkills()
        {
            Action<ExpeditionBattleInput>[] invalid = {
                i => i.Stage.Wave0[1] = "MISSING_ENEMY",
                i => i.Characters[0].TapSkillId = "MISSING_SKILL",
                i => i.Skills[0].Opcode = EffectOpcodes.Revive,
                i => i.Skills[0].EffectId = "MISSING_EFFECT",
                i => i.Effects[0].Kind = EffectKind.Reflect,
                i => i.Boss.AreaSkillId = "MISSING_SKILL"
            };
            foreach (var corrupt in invalid)
            {
                var input = RunBattleFactory.CreateInput("N7", "single", 1, null, null);
                corrupt(input);
                Assert.Throws<ArgumentException>(() => RunBattleFactory.Validate(input));
            }
        }

        [Fact]
        public void ValidateRejectsVersionMismatchAndUnreachableRelics()
        {
            var input = RunBattleFactory.CreateInput("N1", "single", 1, null, null);
            input.ContentHash = new string('0', 64);
            Assert.Throws<ArgumentException>(() => RunBattleFactory.Validate(input));
            Assert.Throws<ArgumentException>(() => RunBattleFactory.CreateInput("N1", "single", 1, new[] { "A04" }, null));
            Assert.Throws<ArgumentException>(() => RunBattleFactory.CreateInput("N1", "single", 1, new[] { "A01", "A01" }, null));
            Assert.Throws<ArgumentException>(() => RunBattleFactory.CreateInput("N1", "missing", 1, null, null));
        }
    }
}
