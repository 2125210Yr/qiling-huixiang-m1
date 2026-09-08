# Knee fabric correction v0.4.2

Fixes excessive stretching near the bent-knee trouser highlight during C001 right-leg lift. The moving-leg/root fade crossed visible knee fabric and the thigh/calf blend was concentrated in a narrow horizontal band. Move the right-side fade into the transparent margin only around the knee and broaden the hinge blend. Preserve the existing lift target (24.576 source pixels vertically), 3.7-second action timing, bone lengths and boot topology.

Verification: RunKnee tests 135 Unity poses and samples only source-opaque fabric in UV x 0.48–0.64, y 0.32–0.445. The initial diagnostic incorrectly included transparent vertices; the final regression uses original texture alpha > 0.95. Before correction, maximum sampled edge stretch was 1.303855 and the regression failed. After correction it is 1.085949 (threshold 1.12). Minimum signed triangle-area ratio improved from 0.1524372 to 0.3317416. Maximum bone length error 7.867813e-6 world units, support sample error 1.192093e-7; exact return. Existing 10 weight/spring and 4 IK checks pass. These are regional mesh measurements, not a claim of cloth simulation or complete removal of visible distortion.

Visual review compares identical frame 25 at the held pose against the old result and frame 0. Inner-knee cloth pulling is reduced; the original painted creases are retained. Review the zoomed video for aesthetic acceptance. No regenerated artwork, new alpha cutouts or reduction of lift amplitude.

Full preview: F:/天命之子/codex专区/puppet-rig-fix/puppet-knee-v0.4.2.mp4
Close-up: F:/天命之子/codex专区/puppet-rig-fix/puppet-knee-detail-v0.4.2.mp4
Before report: F:/天命之子/codex专区/puppet-rig-fix/knee-before/result.txt
After report: F:/天命之子/codex专区/puppet-rig-fix/knee-preview/result.txt

Only knee corrections are included from PuppetRig.cs. Existing unrelated working-tree upper-body changes are preserved and excluded from this commit. RunKnee is an isolated leg regression; it does not certify every combination of unrelated working-tree animation changes.

The isolated commit source compiles against the installed Unity assemblies with zero errors and seven existing unused-field warnings; its 10 weight/spring checks also pass.
