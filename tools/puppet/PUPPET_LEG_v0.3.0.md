# Puppet leg v0.3.0

C001 right leg (viewer-left boot): pelvis/thigh/knee/ankle chain, constant-length two-link IK, 3.7-second lift/hold/return. Click the leg area or call PuppetRig.LiftRightLeg(). Repeated clicks do not restart an active lift.

Target ankle displacement is -10.24 px horizontally and +24.576 px vertically on the 1024x1536 source. This is deliberately a small illustration motion; large knee raises require separate thigh/calf assets and repaired occluded clothing. Sword rendering is not implemented in this milestone.

Dense 129x193 grid; boot mesh topology split along the transparent gap with original UVs retained. Lifted boot has rigid ankle weights; support boot remains rooted. No artwork generation or pixel erasure.

Validation: 4 IK checks, 15 motion checks and 10 existing weight/spring checks pass. A separate leg-only source snapshot compiles against installed Unity assemblies (0 errors, 7 pre-existing unused-field warnings), and its 15 motion checks pass. Current Unity workspace renders 135 samples at 30 Hz: minimum signed triangle-area ratio 0.1524372, maximum bone-length error 7.867813e-6 world units, support ankle/sample-vertex error 1.192093e-7, exact return after 3.7 s. Visual inspection found and corrected a detached toe fragment missed by numerical checks. These are isolated leg checks; support error is not an exhaustive pixel test or proof of all combined idle states.

Preview: F:/天命之子/codex专区/puppet-rig-fix/puppet-leg-v0.3.0.mp4 (actual Unity output, 1024x1536, 15 FPS). Unity report: leg-final-preview/result.txt in the same directory. Workspace has concurrent face/chest/idle tuning; this commit isolates leg changes and leaves those other changes unstaged. The rendered preview uses the current workspace, while the separate compile verifies the isolated commit sources.
