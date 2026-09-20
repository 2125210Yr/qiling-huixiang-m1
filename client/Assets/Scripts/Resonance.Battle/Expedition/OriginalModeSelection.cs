using System;

namespace Resonance.Battle
{
    public static class OriginalModeSelection
    {
        // Resolve before any player persistence API. Old regression hosts opt in explicitly.
        public static bool UseOriginal(string[] arguments)
        {
            if (arguments != null)
                foreach (var argument in arguments)
                    if (string.Equals(argument, "--legacy", StringComparison.Ordinal)) return false;
            return true;
        }
    }
}
