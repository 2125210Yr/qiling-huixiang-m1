using Resonance.Battle;
using Xunit;
using static Resonance.Tests.G2RecheckFixtures;
using static Resonance.Tests.G2ReviewFixtures;

namespace Resonance.Tests
{
    /// <summary>
    /// N04 — TickUnit auto-attack applies Math.Max(14f, autoSkill.DriveGain).
    /// Declared DriveGain below 14 must win; the hidden floor must not.
    /// DESIGN_PLACEHOLDER value, not original-game Drive.
    /// </summary>
    public sealed class G2RecheckDriveGainTests
    {
        [Fact]
        public void N04_AutoAttack_HonorsDeclaredDriveGainBelow14()
        {
            const int declared = 5;
            Assert.True(declared < 14, "N04 fixture DriveGain must stay below the hidden 14 floor");

            var sim = NewJp(1404, AutoMode.Manual, forceNoCrit: true);
            InflateEnemies(sim);
            SuppressAllyAutoAttacks(sim);
            SuppressEnemyAutoAttacks(sim);
            LockFoes(sim);

            var slot = SlotOf(sim, "C001");
            Assert.True(slot >= 0, "C001 missing from DefaultParty");
            var unit = sim.Allies[slot];
            var autoId = unit.Def.AutoSkillId;
            var src = Catalog.MustSkill(autoId);
            var overlay = Catalog.CloneSkill(src);
            overlay.DriveGain = declared;
            sim.OverlaySkill(autoId, overlay);

            sim.Drive = 0f;
            unit.AutoTimer = 100f;
            var autos0 = CountCasts(sim, SkillType.Auto, true);

            sim.Tick();

            Assert.Equal(autos0 + 1, CountCasts(sim, SkillType.Auto, true));
            Assert.Equal(
                (float)declared,
                sim.Drive,
                3);
            Assert.True(
                sim.Drive < 14f - 1e-3f,
                "N04: hidden Math.Max(14, DriveGain) overrode declared " + declared + " → Drive=" + sim.Drive);
        }
    }
}
