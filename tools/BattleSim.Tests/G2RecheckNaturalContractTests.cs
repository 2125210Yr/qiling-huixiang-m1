using Xunit;

namespace Resonance.Tests
{
    /// <summary>
    /// N01–N03 are Unity EventSystem / natural-play. Core must not fake them as PASS.
    ///
    /// Required battle_id + Submit kinds (Unity, not this assembly):
    ///   N01 — battle_id from the ordinary stage entry (not a core-only factory).
    ///         Submit Tap and Slide on a living slot; slide must not also fire Tap/Drive.
    ///         Assert accepted CommandRecord Kind/Slot and matching hit/cast events.
    ///   N02 — DESIGN_PLACEHOLDER pre-start scene that can reach Fever by real QTE
    ///         accumulation (no mid-fight Drive/Perfect injection). Two different
    ///         living slots each accept FeverTap; caster events match the slots.
    ///   N03 — real SetAuto Full then Manual. Auto Tap/Slide/Drive/Fever must go
    ///         through Submit; no double-cast; manual ownership returns after SetAuto.
    /// </summary>
    public sealed class G2RecheckNaturalContractTests
    {
        [Fact(Skip = "N01 N02 N03: Unity EventSystem natural-play required (battle_id + Submit Tap/Slide/FeverTap/SetAuto). Core must not fake PASS.")]
        public void N01_N02_N03_NaturalPlayContract_DocumentedNotFaked()
        {
        }
    }
}
