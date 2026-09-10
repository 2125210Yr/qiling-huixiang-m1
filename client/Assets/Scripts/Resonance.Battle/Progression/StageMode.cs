namespace Resonance.Battle
{
    public enum StageModeKind
    {
        Story = 0,
        Hard = 1
    }

    public static class StageMode
    {
        public static float EnemyHpMul(StageModeKind k) =>
            k == StageModeKind.Hard ? 1.6f : 1f;

        public static float EnemyAtkMul(StageModeKind k) =>
            k == StageModeKind.Hard ? 1.35f : 1f;

        public static float EnemyDefMul(StageModeKind k) =>
            k == StageModeKind.Hard ? 1.25f : 1f;
    }
}
