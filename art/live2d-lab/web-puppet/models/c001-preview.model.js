/* C001 焰刃 - the preview-layers set. No cut-out head, but it ships a real
 * closed-eye plate, so this is the slot where blink looks correct rather than
 * approximated.
 *
 * Source: art/characters/C001-焰刃/preview-layers/
 * pivots.json there stores v from the BOTTOM.
 */
DCPuppet.register({
  id: 'c001-preview',
  name: 'C001 焰刃 · preview-layers (真眨眼)',
  dir: '../../characters/C001-焰刃/preview-layers/',
  canvas: [1080, 1920],
  backdrop: '#08070c',
  pivotOrigin: 'bottom-left',
  note: '眨眼是真实的闭眼图层交叉淡入（preview_layer_body_blink.png），不是画上去的眼皮。',

  pivots: {
    // from preview-layers/pivots.json
    hairBack: [0.271, 0.717],
    hairTip: [0.336, 0.686],
    hairFront: [0.753, 0.660],
    // added for the web rig
    root: [0.480, 0.040],
    torso: [0.500, 0.440],
    chest: [0.500, 0.700],
    head: [0.505, 0.800],
    eyes: [0.505, 0.845],
    hairSide: [0.520, 0.780],
    hand: [0.395, 0.560],
    arm: [0.450, 0.680],
    cloth: [0.500, 0.520],
    legRaiseThigh: [0.500, 0.400],
    legStand: [0.520, 0.400]
  },

  layers: [
    { file: 'bg_plate.png', role: 'bg' },
    { file: 'preview_layer_hair_back.png', role: 'hairBack', bend: 1.0 },
    { file: 'preview_layer_body.png', role: 'base' },
    { file: 'preview_layer_body_blink.png', role: 'blink', follow: 'base' },
    { file: 'preview_layer_hair_tip.png', role: 'hairTip', z: 24, bend: 1.3 },
    { file: 'preview_layer_sword.png', role: 'weapon' },
    { file: 'preview_layer_hair_front.png', role: 'hairFront', bend: 0.9 }
  ]
});
