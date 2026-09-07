# Puppet arm closure v0.4.0

Click the chest hit area or call PuppetRig.CloseArms(). Upper arms draw inward over 1.1 seconds, hold until 1.7 seconds, then release over 1.3 seconds. Local chest/fabric deformation follows 0.12 seconds later; all clasp parameters return to zero by 3.25 seconds. Repeated clicks during the action do not restart it.

This is modest upper-arm closure using the existing illustration, not a newly drawn hands-in-front pose. The sword hand and pocket-side hand retain their illustrated placement. No exposed body area is added and the clothing texture is unchanged. There is no global bust enlargement or simulated cloth tearing. This is controlled 2D deformation, not a calibrated 3D soft-body simulation or mathematical guarantee of constant volume.

Repaired incorrect SmoothStep input normalization in BustGate, broadened sleeve and chest influence transitions, limited local inward displacement, and reduced independent chest oscillation during the held pose. The existing chest/arm/idle tuning is consolidated in this version; unrelated presenter, battle, and asset edits are excluded.

Validation: 7 clasp timing checks, 15 existing motion checks, 10 weight/spring checks, and 4 IK checks. Unity RunClasp renders 165 combined idle/clasp/leg poses against an identical no-clasp baseline: minimum signed triangle-area ratio 0.1500157; local cloth area ratio relative to the baseline 0.6332787 to 1.379207; collar/lower-body differential 0; maximum clasp-induced vertex displacement 9.216011 source pixels; exact parameter return. These area ratios describe local triangles, not whole-body volume. First iteration failed the cloth constraint; displacement was reduced and transitions widened instead of relaxing the test.

Visual review of rest, hold and release shows intact shirt coverage and no obvious tear/seam artifacts. Larger arm movement requires separated and repaired artwork; this version intentionally keeps the deformation small.

Preview: F:/天命之子/codex专区/puppet-rig-fix/puppet-clasp-v0.4.0.mp4
Unity report: F:/天命之子/codex专区/puppet-rig-fix/clasp-preview/result.txt
