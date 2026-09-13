/* C001 焰刃 - richest existing layer set: head / hand / feet are cut out, so
 * head mouse-follow and the raised-leg chain both have real geometry.
 *
 * Source: art/characters/C001-焰刃/puppet-src/layers/
 * body.png is a FULL composite plate; the other files are overlays that sit on
 * top of it. That is why amplitudes here stay small - push them hard and you
 * see the baked-in original ghosting behind the moving overlay.
 *
 * Pivots are the repo's landmarks.json convention: v measured from the BOTTOM.
 */
DCPuppet.register({
  id: 'c001-puppet',
  name: 'C001 焰刃 · 分层木偶 (puppet-src)',
  dir: '../../characters/C001-焰刃/puppet-src/layers/',
  canvas: [1024, 1536],
  backdrop: '#12121a',
  pivotOrigin: 'bottom-left',
  note: 'body.png 是完整合成底板，其余是叠加层：动作幅度要小，否则会看到底板残影。',

  pivots: {
    // straight out of puppet-src/layers/landmarks.json
    head: [0.508, 0.779],
    chest: [0.503, 0.703],
    hand: [0.425, 0.547],
    foot: [0.459, 0.202],
    // added for the web rig
    root: [0.490, 0.060],
    torso: [0.490, 0.450],
    eyes: [0.505, 0.820],
    hairBack: [0.430, 0.880],
    hairSide: [0.500, 0.800],
    hairFront: [0.500, 0.850],
    arm: [0.460, 0.660],
    legRaiseThigh: [0.500, 0.400],
    legRaiseCalf: [0.470, 0.280],
    legStand: [0.525, 0.400],
    cloth: [0.490, 0.520]
  },

  // Eyes are baked into body.png, so blink is the procedural lid path.
  lids: {
    eyes: [[0.476, 0.180], [0.539, 0.180]],
    rx: 0.018, ry: 0.009,
    color: '#e9cbb6',
    lash: 'rgba(74,44,44,0.9)'
  },

  restPose: { legRaiseThigh: -1.2, legRaiseCalf: 0.8, foot: -1.6 },

  layers: [
    { file: 'body.png', role: 'base', name: 'body (底板/composite)' },
    // Hair sits in front of the plate so its sway is actually visible; it
    // barely overlaps the torso, so the z cheat does not read as wrong.
    { file: 'hair_back.png', role: 'hairBack', z: 21, bend: 1.0 },
    { file: 'hair_side.png', role: 'hairSide', z: 23, bend: 0.8 },
    { file: 'foot_l.png', role: 'legStand' },
    { file: 'foot_r.png', role: 'foot' },
    { file: 'hand_r.png', role: 'hand' },
    { file: 'sword.png', role: 'weapon' },
    { file: 'head.png', role: 'head' },
    { file: 'hair_front.png', role: 'hairFront', bend: 0.9 }
  ]
});
