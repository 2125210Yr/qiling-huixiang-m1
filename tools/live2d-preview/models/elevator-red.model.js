/* elevator-red - the red-dress elevator reference (raised leg, hand on hip).
 *
 * Layers are PLACEHOLDER silhouettes generated into
 *   sample-layers/elevator-red/
 * by _tools/make_placeholder_layers.py. Layer names, z-order and pivots follow
 * art/live2d-lab/elevator-red/psd_cut_plan.json, so when the painted PSD cuts
 * land you can overwrite the PNGs in place and this config still applies.
 *
 * Pivots here are v-from-TOP, matching that cut plan.
 */
DCPuppet.register({
  id: 'elevator-red',
  name: '电梯红裙 · elevator-red (占位层)',
  dir: 'sample-layers/elevator-red/',
  canvas: [1098, 574],
  backdrop: '#0a0508',
  placeholder: true,
  note: '占位剪影，几何/层序/轴点按 psd_cut_plan.json。真图切好后同名覆盖 sample-layers/elevator-red/ 即可。',

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

  /* The rest pose IS the reference pose: right leg held high, foot pointed.
     Toggle "静止姿势" off in the UI to see the raised-leg chain relax. */
  restPose: {
    legRaiseThigh: -5.0,
    legRaiseCalf: 3.5,
    foot: -7.0,
    legStand: 0.6,
    arm: 1.5
  },

  lids: {
    eyes: [[0.399, 0.198], [0.428, 0.196]],
    rx: 0.011, ry: 0.014,
    color: '#e6c3ac',
    lash: 'rgba(58,26,34,0.9)'
  },

  layers: [
    { file: 'bg_elevator.png', role: 'bg', z: 0, name: 'bg_elevator (电梯底板)' },
    { file: 'back_hair.png', role: 'hairBack', z: 10, bend: 1.0 },
    { file: 'torso.png', role: 'torso', z: 20 },
    { file: 'chest.png', role: 'chest', z: 22 },
    { file: 'dress_body.png', role: 'cloth', z: 24, bend: 0.9 },
    { file: 'leg_stand.png', role: 'legStand', z: 26 },
    { file: 'dress_slit_panel.png', role: 'clothPanel', z: 28, bend: 1.4 },
    { file: 'leg_raise_thigh.png', role: 'legRaiseThigh', z: 30 },
    { file: 'leg_raise_calf.png', role: 'legRaiseCalf', z: 32 },
    { file: 'foot_raise.png', role: 'foot', z: 34, name: 'foot_raise (含 anklet 占位)' },
    { file: 'arm_hip.png', role: 'arm', z: 40 },
    { file: 'neck.png', role: 'neck', z: 44 },
    { file: 'face.png', role: 'head', z: 48 },
    { file: 'eyes.png', role: 'eyes', z: 50 },
    { file: 'front_hair.png', role: 'hairFront', z: 56, bend: 0.8 },
    { file: 'horns.png', role: 'horns', z: 60 }
  ]
});
