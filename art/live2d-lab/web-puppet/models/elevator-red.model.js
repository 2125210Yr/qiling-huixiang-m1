/* elevator-red - the red-dress elevator still (raised leg, hand on hip).
 *
 * Prefers the painted 1280x720 still:
 *   layers/body_original.png   rembg figure (same canvas as original-still.png)
 *   layers/<name>.png          same-canvas part cuts
 * Falls back to layers/body.png (old 1092x584 GIF 2x plate) if the original
 * plate is missing. Numbered 2048 Cubism pads are not used here.
 *
 * Serve from art/live2d-lab so ../elevator-red/layers/ resolves.
 * Pivots are v-from-TOP.
 */
DCPuppet.register({
  id: 'elevator-red',
  name: '电梯红裙 · 新静帧分层',
  dir: '../elevator-red/layers/',
  canvas: [1280, 720],
  backdrop: '#0a0508',
  placeholder: false,
  note: 'body_original.png 是 1280×720 rembg 全身底板；其余层是同画布静帧切层。抬腿已画在像素里，restPose 全 0。',

  pivotOrigin: 'top-left',
  pivots: {
    root: [0.452, 0.960],
    head: [0.416, 0.258],
    chest: [0.412, 0.330],
    torso: [0.428, 0.560],
    eyes: [0.413, 0.197],
    cloth: [0.424, 0.330],
    clothPanel: [0.398, 0.520],
    legStand: [0.455, 0.580],
    legRaiseThigh: [0.418, 0.545],
    legRaiseCalf: [0.352, 0.366],
    foot: [0.295, 0.660],
    arm: [0.462, 0.325],
    hand: [0.476, 0.532],
    hairBack: [0.428, 0.200],
    hairFront: [0.412, 0.150],
    hairSide: [0.420, 0.200]
  },

  /* Raised right leg is already in the still pixels. Do not add a rest offset. */
  restPose: {
    legRaiseThigh: 0,
    legRaiseCalf: 0,
    foot: 0,
    legStand: 0,
    arm: 0
  },

  lids: {
    eyes: [[0.399, 0.198], [0.428, 0.196]],
    rx: 0.011, ry: 0.014,
    color: '#e6c3ac',
    lash: 'rgba(58,26,34,0.9)'
  },

  layers: [
    { file: 'body_original.png', fallback: 'body.png', role: 'base', z: 8, name: 'body_original (rembg 底板)' },
    { file: 'back_hair.png', role: 'hairBack', z: 10, bend: 1.0 },
    { file: 'torso.png', role: 'torso', z: 20 },
    { file: 'chest.png', role: 'chest', z: 22 },
    { file: 'dress_body.png', role: 'cloth', z: 24, bend: 0.9 },
    { file: 'leg_stand.png', role: 'legStand', z: 26 },
    { file: 'dress_slit_panel.png', role: 'clothPanel', z: 28, bend: 1.4 },
    { file: 'leg_raise_thigh.png', role: 'legRaiseThigh', z: 30 },
    { file: 'leg_raise_calf.png', role: 'legRaiseCalf', z: 32 },
    { file: 'foot_raise.png', role: 'foot', z: 34 },
    { file: 'anklet.png', role: 'foot', z: 35, name: 'anklet' },
    { file: 'arm_hip.png', role: 'arm', z: 40 },
    { file: 'neck.png', role: 'neck', z: 44 },
    { file: 'face.png', role: 'head', z: 48 },
    { file: 'eyes.png', role: 'eyes', z: 50 },
    { file: 'front_hair.png', role: 'hairFront', z: 56, bend: 0.8 },
    { file: 'horns.png', role: 'horns', z: 60 }
  ]
});
