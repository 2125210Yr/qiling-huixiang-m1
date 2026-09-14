using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true, MaxParallelThreads = 1)]

namespace Resonance.Tests
{
    /// <summary>
    /// A53-T07 — catalog / BattleReplayer statics are process-wide.
    /// Collection locks alone cannot protect G2ReviewCapability / G2ReviewReplay
    /// in other collections. This assembly is the serial green path:
    /// <c>dotnet test tools/BattleSim.Tests/BattleSim.Tests.csproj</c> with no filter.
    /// </summary>
    public sealed class G2A53IsolationTests
    {
        [Fact]
        public void T07_AssemblyDisablesTestParallelization()
        {
            var attrs = typeof(G2A53IsolationTests).Assembly.GetCustomAttributes(
                typeof(CollectionBehaviorAttribute), false);
            Assert.True(attrs.Length > 0, "A53-T07: missing [assembly: CollectionBehavior]");
            var found = false;
            for (int i = 0; i < attrs.Length; i++)
            {
                var a = attrs[i] as CollectionBehaviorAttribute;
                if (a == null) continue;
                Assert.True(
                    a.DisableTestParallelization,
                    "A53-T07: CollectionBehavior.DisableTestParallelization must be true");
                found = true;
            }
            Assert.True(found, "A53-T07: CollectionBehaviorAttribute not applied");
        }
    }
}
