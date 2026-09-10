namespace Resonance.Battle
{
    // PvP is an offline flag only; no live PvP.
    public static class PvpRules
    {
        public const float CartaMul = 1.05f;

        public static float CartaMulIfPvp(bool pvpDoor) => pvpDoor ? CartaMul : 1f;

        public static float CartaMulIfPvp(bool pvpDoor, GearDef carta)
        {
            if (!pvpDoor || carta == null) return 1f;
            return CartaMul;
        }
    }
}
