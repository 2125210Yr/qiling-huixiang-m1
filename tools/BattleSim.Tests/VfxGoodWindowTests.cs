using Resonance.Battle;
using Xunit;

namespace Resonance.Tests
{
    /// <summary>
    /// B1-F2: coin Perfect window is read-only math. Do not widen so a late tap always hits.
    /// Reproduces the np.fever.v1 miss: find delay + blind 0.60s wait lands after WindowAt+Half.
    /// </summary>
    public sealed class VfxGoodWindowTests
    {
        const float BlindWait = 0.60f;

        [Fact]
        public void PublishedPulse_IsNotWidened()
        {
            Assert.Equal(1.2f, VfxGoodWindow.Cycle);
            Assert.Equal(0.60f, VfxGoodWindow.WindowAt);
            Assert.Equal(0.08f, VfxGoodWindow.WindowHalf);
        }

        [Fact]
        public void InWindow_OnlyInsideHalfAroundWindowAt()
        {
            Assert.False(VfxGoodWindow.IsInWindow(0.51f));
            Assert.True(VfxGoodWindow.IsInWindow(0.52f));
            Assert.True(VfxGoodWindow.IsInWindow(0.60f));
            Assert.True(VfxGoodWindow.IsInWindow(0.68f));
            Assert.False(VfxGoodWindow.IsInWindow(0.69f));
            Assert.False(VfxGoodWindow.IsInWindow(0.70f));
        }

        [Fact]
        public void BlindWaitAfterFind_LandsPastFirstWindow()
        {
            // Archive path: Show() already aged the coin before TapQte found it.
            // waitWindow used unscaled 0.60 from find, not from Begin().
            const float findAge = 0.12f;
            var tapAge = findAge + BlindWait;
            Assert.True(tapAge > VfxGoodWindow.WindowAt + VfxGoodWindow.WindowHalf);
            Assert.False(VfxGoodWindow.IsInWindow(tapAge));
            Assert.True(VfxGoodWindow.SecondsUntilWindow(findAge) > 0f);
            Assert.True(VfxGoodWindow.SecondsUntilWindow(findAge) < BlindWait);
        }

        [Fact]
        public void EarlyInWindow_LeavesHalfForPointerLatency()
        {
            Assert.True(VfxGoodWindow.IsEarlyInWindow(0.52f));
            Assert.True(VfxGoodWindow.IsEarlyInWindow(0.60f));
            Assert.False(VfxGoodWindow.IsEarlyInWindow(0.66f));
            Assert.False(VfxGoodWindow.IsEarlyInWindow(0.12f));
            Assert.Equal(0f, VfxGoodWindow.SecondsUntilWindow(0.52f));
            Assert.True(VfxGoodWindow.SecondsUntilWindow(0.66f) > 0.5f);
        }

        [Fact]
        public void SecondsUntilWindow_FromBeginIsFirstRisingEdge()
        {
            var until = VfxGoodWindow.SecondsUntilWindow(0f);
            Assert.InRange(until, 0.519f, 0.521f);
            Assert.True(VfxGoodWindow.IsEarlyInWindow(until));
        }

        [Fact]
        public void ReturnTripWindow_IsAlsoHittable()
        {
            // PingPong period = 2*Cycle. Backward window starts at 1.72.
            Assert.True(VfxGoodWindow.IsInWindow(1.72f));
            Assert.True(VfxGoodWindow.IsEarlyInWindow(1.72f));
            Assert.Equal(0f, VfxGoodWindow.SecondsUntilWindow(1.72f));
            Assert.False(VfxGoodWindow.IsInWindow(1.70f));
        }

        [Fact]
        public void ThreeGoodsStayBelowFeverOpen_ThreePerfectsReach()
        {
            // Documents why 3×Good never opened Fever. Do not raise QteFever(Good).
            Assert.Equal(15f, BattleSim.QteFever(DriveTiming.Good));
            Assert.Equal(40f, BattleSim.QteFever(DriveTiming.Perfect));
            Assert.True(3f * BattleSim.QteFever(DriveTiming.Good) < 100f);
            Assert.True(3f * BattleSim.QteFever(DriveTiming.Perfect) >= 100f);
        }
    }
}
