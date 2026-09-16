using System;

namespace Resonance.Battle
{
    /// <summary>
    /// Read-only Drive QTE coin pulse. Same numbers as <c>VfxGoodButton</c>.
    /// Do not widen the window to hide a late tap. HUD still maps hit→Perfect / miss→Good.
    /// </summary>
    public static class VfxGoodWindow
    {
        public const float Cycle = 1.2f;
        public const float WindowAt = 0.60f;
        public const float WindowHalf = 0.08f;
        public const float Period = Cycle * 2f;

        public static float PingPong(float age, float length)
        {
            if (length <= 0f) return 0f;
            var span = length * 2f;
            var t = Repeat(age, span);
            return length - Math.Abs(t - length);
        }

        public static bool IsInWindow(float age)
        {
            // Inclusive WindowAt±Half. Epsilon is float hygiene, not a wider pulse.
            return Math.Abs(PingPong(age, Cycle) - WindowAt) <= WindowHalf + 1e-4f;
        }

        /// <summary>
        /// First half of the live window so pointer latency stays inside <see cref="WindowHalf"/>.
        /// </summary>
        public static bool IsEarlyInWindow(float age)
        {
            return SecondsLeftInWindow(age) + 1e-4f >= WindowHalf;
        }

        public static float SecondsLeftInWindow(float age)
        {
            if (!IsInWindow(age)) return 0f;
            var phase = Repeat(age, Period);
            var pp = PingPong(age, Cycle);
            if (phase <= Cycle)
                return (WindowAt + WindowHalf) - pp;
            return pp - (WindowAt - WindowHalf);
        }

        /// <summary>Seconds until <see cref="IsEarlyInWindow"/>. Zero if already ready to tap.</summary>
        public static float SecondsUntilWindow(float age)
        {
            if (IsEarlyInWindow(age)) return 0f;
            var phase = Repeat(age, Period);
            var forwardStart = WindowAt - WindowHalf;
            var backwardStart = Period - (WindowAt + WindowHalf);
            if (phase < forwardStart)
                return forwardStart - phase;
            if (phase < backwardStart)
                return backwardStart - phase;
            return Period + forwardStart - phase;
        }

        static float Repeat(float t, float length)
        {
            if (length <= 0f) return 0f;
            var r = t - (float)Math.Floor(t / length) * length;
            if (r < 0f) r += length;
            return r;
        }
    }
}
