using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using Resonance.Battle;
using Xunit;

namespace Resonance.Tests
{
    public sealed class OriginalEncounterInputTests
    {
        const string BeforeO3ContentHash = "03a8cc088289181ba9ba9c2f2cdee6de52b1bd6e191aabdd86b5d8d7638c3437";

        [Fact]
        public void N4FreezesTheDeclaredEliteTimingAndSkillIdentity()
        {
            var input = Input("N4");
            Assert.NotNull(input.Elite);
            Assert.Equal("prompter-v1", input.Elite.Version);
            Assert.Equal(0, input.Elite.CasterSlot);
            Assert.Equal("OE_PROMPTER", input.Stage.Wave0[input.Elite.CasterSlot]);
            Assert.Equal("OE_PROMPTER_auto", input.Elite.AutoSkillId);
            Assert.Equal("OE_PROMPTER_tap", input.Elite.AreaSkillId);
            Assert.Equal(6f, input.Elite.FirstIntentSec);
            Assert.Equal(3f, input.Elite.CastDurationSec);
            Assert.Equal(12f, input.Elite.IntervalSec);
            Assert.Equal(3f, input.Elite.AutoIntervalSec);
            Assert.Null(input.Boss);
            Assert.False(input.IsBoss);
        }

        [Fact]
        public void DeepCloneDetachesTheFrozenEliteDefinition()
        {
            var input = EliteInput();
            var copy = input.DeepClone();
            Assert.NotSame(input.Elite, copy.Elite);
            copy.Elite.CasterSlot = 2;
            copy.Elite.CastDurationSec = 33f;
            copy.Elite.Version = "changed";
            Assert.Equal(0, input.Elite.CasterSlot);
            Assert.Equal(3f, input.Elite.CastDurationSec);
            Assert.Equal("prompter-v1", input.Elite.Version);
        }

        [Fact]
        public void SerializationPreservesEliteAsBattleInput()
        {
            var input = EliteInput();
            input.Elite.FirstIntentSec = 5.5f;
            var serializer = new DataContractJsonSerializer(typeof(ExpeditionBattleInput));
            using (var stream = new MemoryStream())
            {
                serializer.WriteObject(stream, input);
                stream.Position = 0;
                var copy = (ExpeditionBattleInput)serializer.ReadObject(stream);
                Assert.NotSame(input.Elite, copy.Elite);
                Assert.Equal(ExpeditionContent.Fingerprint(input), ExpeditionContent.Fingerprint(copy));
                Assert.Equal(5.5f, copy.Elite.FirstIntentSec);
                Assert.Equal(input.Elite.AreaSkillId, copy.Elite.AreaSkillId);
                RunBattleFactory.Validate(copy);
            }
        }

        [Fact]
        public void EliteChangesContentCompatibilityAndItsOwnInputFingerprint()
        {
            Assert.NotEqual(BeforeO3ContentHash, ExpeditionContent.ContentHash);
            var input = EliteInput();
            var original = ExpeditionContent.Fingerprint(input);
            input.Elite.IntervalSec += 0.5f;
            Assert.NotEqual(original, ExpeditionContent.Fingerprint(input));
            input.Elite.IntervalSec -= 0.5f;
            Assert.Equal(original, ExpeditionContent.Fingerprint(input));
            input.Elite.Version = "different-version";
            Assert.NotEqual(original, ExpeditionContent.Fingerprint(input));
            input = Input("N4");
            input.ContentHash = BeforeO3ContentHash;
            Assert.Throws<ArgumentException>(() => RunBattleFactory.Validate(input));
        }

        [Fact]
        public void N4RejectsMissingOrMismatchedEliteIdentity()
        {
            Action<ExpeditionBattleInput>[] corruptions = {
                i => i.Elite = null,
                i => i.Elite.CasterSlot = 1,
                i => i.Stage.Wave0[0] = "OE_USHER",
                i => i.Elite.Version = "prompter-v2",
                i => i.Elite.AutoSkillId = "OE_CHORUS_auto",
                i => i.Elite.AreaSkillId = "OE_BOSS_echo",
                i => i.Characters.Single(c => c.Id == "OE_PROMPTER").AutoSkillId = "OE_CHORUS_auto"
            };
            foreach (var corrupt in corruptions)
            {
                var input = EliteInput(); corrupt(input);
                Assert.Throws<ArgumentException>(() => RunBattleFactory.Validate(input));
            }
        }

        [Fact]
        public void EncounterDefinitionsCannotAppearOnUnrelatedNodes()
        {
            foreach (var node in new[] { "N1", "N2-backstage", "N2-audience", "N5", "N7" })
            {
                var input = Input(node);
                Assert.Null(input.Elite);
                input.Elite = new EliteEncounterDef();
                Assert.Throws<ArgumentException>(() => RunBattleFactory.Validate(input));
            }
            var ordinary = Input("N1"); ordinary.Boss = new BossEncounterDef();
            Assert.Throws<ArgumentException>(() => RunBattleFactory.Validate(ordinary));
        }

        [Fact]
        public void EliteClocksMustRemainFinitePositiveAndBounded()
        {
            Action<EliteEncounterDef>[] corruptions = {
                e => e.FirstIntentSec = float.NaN,
                e => e.CastDurationSec = float.PositiveInfinity,
                e => e.IntervalSec = 0f,
                e => e.AutoIntervalSec = -1f,
                e => e.CastDurationSec = 121f
            };
            foreach (var corrupt in corruptions)
            {
                var input = EliteInput(); corrupt(input.Elite);
                Assert.Throws<ArgumentException>(() => RunBattleFactory.Validate(input));
            }
        }

        [Fact]
        public void EncounterAutoAndAreaCannotSilentlyChangeTheirTargetContract()
        {
            foreach (var node in new[] { "N4", "N7" })
            {
                Action<SkillDef, SkillDef>[] corruptions = {
                    (auto, area) => auto.Target = TargetRule.AllEnemies,
                    (auto, area) => auto.TargetCount = 2,
                    (auto, area) => { auto.Type = SkillType.Tap; auto.Opcode = EffectOpcodes.DmgTap; },
                    (auto, area) => area.Target = TargetRule.RandomEnemies,
                    (auto, area) => area.TargetCount = 1,
                    (auto, area) => area.HealMaxHpFrac = 0.1f
                };
                foreach (var corrupt in corruptions)
                {
                    var input = node == "N4" ? EliteInput() : Input(node);
                    var autoId = node == "N4" ? input.Elite.AutoSkillId : input.Boss.AutoSkillId;
                    var areaId = node == "N4" ? input.Elite.AreaSkillId : input.Boss.AreaSkillId;
                    corrupt(input.Skills.Single(s => s.Id == autoId), input.Skills.Single(s => s.Id == areaId));
                    Assert.Throws<ArgumentException>(() => RunBattleFactory.Validate(input));
                }
            }
        }

        [Fact]
        public void N7RequiresTheFixedBossAndTwoMasksAndSupportedVersion()
        {
            Action<ExpeditionBattleInput>[] corruptions = {
                i => i.Stage.Wave0 = new[] { "OE_USHER", "OE_MASK", "OE_MASK" },
                i => i.Stage.Wave0 = new[] { "OE_BOSS", "OE_CHORUS", "OE_MASK" },
                i => i.Stage.Wave0 = new[] { "OE_BOSS", "OE_MASK" },
                i => i.Stage.Wave0 = new[] { "OE_BOSS", "OE_MASK", "OE_MASK", "OE_CHORUS" },
                i => i.Boss.Version = "white-conductor-v2",
                i => i.Boss.MaskCharacterId = "OE_CHORUS",
                i => i.Boss.AutoSkillId = "OE_CHORUS_auto",
                i => i.Boss.AreaSkillId = "OE_PROMPTER_tap",
                i => i.Characters.Single(c => c.Id == "OE_BOSS").IsBoss = false
            };
            foreach (var corrupt in corruptions)
            {
                var input = Input("N7"); corrupt(input);
                Assert.Throws<ArgumentException>(() => RunBattleFactory.Validate(input));
            }
        }

        static ExpeditionBattleInput Input(string node) => RunBattleFactory.CreateInput(node, "single", 73921, null, null);
        static ExpeditionBattleInput EliteInput()
        {
            var input = Input("N4");
            input.Elite = new EliteEncounterDef(); // Explicit frozen fixture also exercises validation before factory wiring.
            return input;
        }
    }
}
