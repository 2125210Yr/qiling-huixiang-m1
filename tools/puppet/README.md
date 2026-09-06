# Resonance Puppet

规格：`docs/superpowers/specs/2026-09-06-puppet-from-masks-design.md`

遮罩（PS/Krita）放在 `art/characters/<角色>/puppet-src/masks/`。

文件名：`keep.png`、`chest.png`、`hair_back`、`hair_front`、`hair_side`、`head`、`hand_r`、`hand_l`、`foot_r`、`foot_l`、`sword`。

在透明图层上涂，或白画在黑底上；尺寸 1024×1536。

```
python tools/puppet/from_masks.py --id C001 --src art/characters/C001-焰刃/puppet-src
python tools/puppet/from_masks.py --id C001 --src art/characters/C001-焰刃/puppet-src --pack
```

先看 `puppet-src/layers/preview/`，再 `--pack`。
