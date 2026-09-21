using System;
using System.Globalization;
using NUnit.Framework;
using Resonance.Battle;

namespace Resonance.EditorTests
{
    public sealed class OriginalFingerprintPortabilityTests
    {
        // Shared verbatim with the other runtime's OriginalFingerprintPortabilityTests.
        // Fixed SHA-256 goldens over ASCII "<prefix><IEEE bits>;", computed independently.
        // Floats include 1/3, decimal-rounding midpoints 1.001953125 / 1048576.125,
        // minimum subnormal, both maximum finite signs, signed zeros, and adjacent 1 values.
        // Doubles include 1/3, nextAfter(1), 1, minimum subnormal, maxima, and signed zeros.
        static readonly string[][] GoldenVectors =
        {
            new[] { "f3eaaaaab", "6822f3ca32faee3fae5f36e0640139df909988fff63e280a427c7705f1a2d55c" },
            new[] { "f3f804000", "d86ba6f5c95a8b3bce574915d2465db24462ec66f3f7971031a8de6e30c409b0" },
            new[] { "f49800001", "f77d8d427fd99e61e31db568f012bc47fa200ed4593a291958664cc30df297f9" },
            new[] { "f00000001", "775ca129030c81903ec8fccbeea77aa733b66491e89575b7be047cf6c2969482" },
            new[] { "f7f7fffff", "13c30927a515ce7a96dae72017f8bdaecbaf86be9069e25bb0c4b29ccc55da86" },
            new[] { "fff7fffff", "0dbf6ab9d39052c78311295ca5054a5a30645abdc69bd4bc42e4269eff8c396e" },
            new[] { "f00000000", "a1af6e0727c25c71e45dafa2ca9df0ba89239b384829bdf3cd9178da084674e2" },
            new[] { "f80000000", "b6e437f73f94b897376317715fcd3083188f87bd2c0f353797f9fe250235b769" },
            new[] { "f3f800000", "77030802d1725b3ccbbe14df8c04413be1478e4ed107281d42d9398ca3551171" },
            new[] { "f3f800001", "1e669aeee96da0c09e7ddbafb6cf46dd0cdeb17f6e2f14cde6f0988daa143bb6" },
            new[] { "d3fd5555555555555", "6c1bb951f23281ef79d94fe60cae1f80ec3ceba0bca1865ace67807d6d48abbc" },
            new[] { "d3ff0000000000001", "9ff29733e3e099921c2511a1f53991c40ad27ae75778e8103890f5f6e8d3c2c2" },
            new[] { "d3ff0000000000000", "c212e9a0720fc99506627a86fb6b4c9a4afa85f20fa3fd1b059c787e38c9df53" },
            new[] { "d0000000000000001", "c0558490f3aaec73b4884404fd60ef9d6ff0234f59148cf7b5830cb246230b2f" },
            new[] { "d7fefffffffffffff", "01f56a8c6f683bc974269e8b9a5e3ed58a1c93758c7eb84ac75d1bb49322ba6e" },
            new[] { "dffefffffffffffff", "5dabd4be857db25072366d434e2f6a597d2dd3c652ed4ca34889e44ffe0c2086" },
            new[] { "d0000000000000000", "6455e75a524d3fbbc2df7f4cfb82c6de51970d4a956a5b74d1519dac92c1b059" },
            new[] { "d8000000000000000", "0cd7c2a0306c930717ab1fd859b2608c3e14ff61b70fcda695849692c983dd92" },
        };

        [Test]
        public void NumericFingerprintMatchesFixedIeeeGoldenVectors()
        {
            AssertGoldenVectors();
        }

        [Test]
        public void OriginalGuideNaturallyRechargesToTheSameIeeeBitsAfterThreeTicks()
        {
            var input = RunBattleFactory.CreateInput("N7", "single", 260921, new[] { "C01" }, null);
            var sim = RunBattleFactory.Create(input);
            for (var tick = 0; tick < 1200 && !sim.CanAct(4) && sim.Outcome == BattleOutcome.InProgress; tick++)
                sim.Tick();
            Assert.That(sim.Outcome, Is.EqualTo(BattleOutcome.InProgress));
            Assert.That(sim.CanAct(4), Is.True, "Guide must become ready through natural charge accumulation.");
            Assert.That(sim.Submit(BattleCommand.Tap(4, CommandSource.Player)).Accepted, Is.True, sim.FailedReason);
            Assert.That(BitConverter.ToUInt32(BitConverter.GetBytes(sim.Allies[4].Charge), 0), Is.EqualTo(0u));

            // Legal guide tap consumes its charge. C01 relays to another ally, never the caster.
            // Three ordinary ticks exposed a Mono/.NET float intermediate-rounding divergence.
            for (var tick = 0; tick < 3; tick++) sim.Tick();

            Assert.That(sim.Outcome, Is.EqualTo(BattleOutcome.InProgress));
            var actualBits = BitConverter.ToUInt32(BitConverter.GetBytes(sim.Allies[4].Charge), 0);
            Assert.That(actualBits.ToString("x8", CultureInfo.InvariantCulture), Is.EqualTo("3fa00001"));
        }

        [Test]
        public void AdjacentValuesAndSignedZeroRemainDistinct()
        {
            AssertDistinct("f3f800000", "f3f800001");
            AssertDistinct("f49800000", "f49800001");
            AssertDistinct("d3ff0000000000000", "d3ff0000000000001");
            AssertDistinct("f00000000", "f80000000");
            AssertDistinct("d0000000000000000", "d8000000000000000");
            AssertDistinct("f3f800000", "d3ff0000000000000");
        }

        [Test]
        public void NumericFingerprintIsUnchangedByCurrentCulture()
        {
            var previousCulture = CultureInfo.CurrentCulture;
            var previousUiCulture = CultureInfo.CurrentUICulture;
            try
            {
                foreach (var culture in new[] { "fr-FR", "tr-TR" })
                {
                    CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
                    CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(culture);
                    AssertGoldenVectors();
                }
            }
            finally
            {
                CultureInfo.CurrentCulture = previousCulture;
                CultureInfo.CurrentUICulture = previousUiCulture;
            }
        }

        static void AssertGoldenVectors()
        {
            foreach (var vector in GoldenVectors)
            {
                var actual = ExpeditionContent.Fingerprint(Value(vector[0]));
                Assert.That(actual, Is.EqualTo(vector[1]), vector[0] + "; culture=" + CultureInfo.CurrentCulture.Name);
            }
        }

        static void AssertDistinct(string left, string right)
        {
            Assert.That(ExpeditionContent.Fingerprint(Value(left)), Is.Not.EqualTo(ExpeditionContent.Fingerprint(Value(right))), left + " must differ from " + right);
        }

        static object Value(string encodedBits)
        {
            // Bit reconstruction deliberately avoids compiler/formatter treatment of negative zero.
            if (encodedBits[0] == 'f')
                return BitConverter.ToSingle(BitConverter.GetBytes(Convert.ToUInt32(encodedBits.Substring(1), 16)), 0);
            return BitConverter.ToDouble(BitConverter.GetBytes(Convert.ToUInt64(encodedBits.Substring(1), 16)), 0);
        }
    }
}
