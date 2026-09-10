using Resonance.Battle;
using Xunit;

namespace Resonance.Tests
{
    public sealed class IgnitionTests
    {
        [Fact]
        public void ZeroStonesAreZero()
        {
            var b = Ignition.Of(0, 0, 0);
            Assert.Equal(0f, b.Atk);
            Assert.Equal(0f, b.Crt);
            Assert.Equal(0f, b.Agl);
            Assert.Equal(0, b.ExtraAtk(0, 1000));
            Assert.Equal(0f, b.ExtraDmgAdd(true, true));
        }

        [Fact]
        public void OneAtkStoneIsMainCoreRed()
        {
            var b = Ignition.Of(1, 0, 0);
            Assert.Equal(Ignition.AtkPerRed * Ignition.MainRed, b.Atk, 5);
            Assert.Equal(0f, b.Crt);
            Assert.Equal(0f, b.Agl);
            Assert.Equal(600, b.ExtraAtk(0, 1000));
            Assert.Equal(2200, b.ExtraAtk(1000, 1000));
        }

        [Fact]
        public void CrtAndAglEnterExtraDmg()
        {
            var b = Ignition.Of(0, 1, 1);
            Assert.Equal(0f, b.Atk);
            Assert.Equal(Ignition.CrtPerRed * Ignition.MainRed, b.Crt, 5);
            Assert.Equal(Ignition.AglPerRed * Ignition.MainRed, b.Agl, 5);
            Assert.Equal(0f, b.ExtraDmgAdd(false, false));
            Assert.Equal(b.Crt, b.ExtraDmgAdd(true, false), 5);
            Assert.Equal(b.Agl, b.ExtraDmgAdd(false, true), 5);
            Assert.Equal(b.Crt + b.Agl, b.ExtraDmgAdd(true, true), 5);
        }

        [Fact]
        public void ExtraStonesAddLinearRed()
        {
            var one = Ignition.Of(1, 0, 0);
            var two = Ignition.Of(2, 0, 0);
            Assert.Equal(Ignition.AtkPerRed * (Ignition.MainRed + Ignition.ExtraRed), two.Atk, 5);
            Assert.True(two.Atk > one.Atk);
        }

        [Fact]
        public void ClampsNegativesAndCap()
        {
            var none = Ignition.Of(-3, -1, -8);
            Assert.Equal(0f, none.Atk);
            Assert.Equal(0f, none.Crt);
            Assert.Equal(0f, none.Agl);

            var cap = Ignition.Of(Ignition.StoneCap, 0, 0);
            var over = Ignition.Of(Ignition.StoneCap + 9, 0, 0);
            Assert.Equal(cap.Atk, over.Atk);
        }

        [Fact]
        public void ChannelsAreIndependent()
        {
            var b = Ignition.Of(2, 1, 3);
            Assert.Equal(Ignition.Of(2, 0, 0).Atk, b.Atk);
            Assert.Equal(Ignition.Of(0, 1, 0).Crt, b.Crt);
            Assert.Equal(Ignition.Of(0, 0, 3).Agl, b.Agl);
        }
    }
}
