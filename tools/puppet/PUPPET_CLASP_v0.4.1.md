# v0.4.1 — Withdraw rejected clasp prototype

The v0.4.0 visual result was rejected for anatomically incorrect narrowing and distortion. Its passing numerical tests did not establish anatomical or artistic correctness. The previous preview is a rejected prototype, not an approved animation.

PuppetRig.CloseArms() now returns false. Renderer-side inward clasp and arm-displacement channels are disabled even if the motion controller is triggered externally. Idle, blink, hair and leg code are retained. No replacement clasp artwork or animation is claimed in this version.

The prior RunClasp acceptance entry point is historical and is expected to reject the disabled trigger. Use RunClaspDisabled for the withdrawal regression check. Future neutral arm posing needs correctly drawn shoulder/elbow key poses before animation.
