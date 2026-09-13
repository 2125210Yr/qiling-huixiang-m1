# Bilibili round-4/5 classify (2026-09-11 late)

**Primary GL ordinary 5p PVE in shutdown window: still 0.**  
Root `docs/reference/gl-shutdown-pve/*.mp4` = **0**.

## Round-4 (NEEDS_WATCH closeout)

| file | verdict | why |
|---|---|---|
| `rejected_not_primary/post_eos_story_1_1_2023_BV1ju411M7kM.mp4` | REJECT | post-EOS VN only |
| `rejected_not_primary/kr_story_guide_2023_BV17s4y1v7Ce.mp4` | REJECT | cloud-phone bot / RAID menu / lobby |
| `rejected_not_primary/gl_equip_vlog26_2022_BV1K94y1S7sD.mp4` | REJECT | equip bookmark VLOG |
| `rejected_not_primary/gl_story_compile_2021_BV1Jf4y1t7qV_first8m.mp4` | REJECT | first 8m only downloaded; JP VN / splash — no 5p HUD in sample |

Story anthology P3–P6 closed in `15_bili_classify.md`.

## Round-5 (Playwright search `国际服 战斗` + FEVER title)

| file | verdict | why |
|---|---|---|
| `contrast/gl_auto_ds_fever_2019_BV1xt41187U4.mp4` | **contrast_only** | Real vertical 5p PVE: AUTO SKILL / X2 / FEVER / Drive. Title GL; **2019** + **Chinese UI**. Not shutdown-window English primary |
| `contrast/gl_tryout_noladder_BV1p4411x77V.mp4` | contrast_only | Story 1-1 battle + DRIVE GAUGE / FEVER / READY TO RUMBLE. Early tryout + Chinese UI |
| `contrast/jp_water_enhance_BV1ht411C79Z.mp4` | contrast_only | JP 强化地下城 5p + FEVER/AUTO；title implies GL wish, footage JP |
| `contrast/jp_change_story_2018_BV1rW411n7Hb.mp4` | contrast_only | JP 嫦娥剧情关：5p battle HUD (AUTO SKILL / FEVER) — 2018 JP |
| `rejected_not_primary/kr_closed_beta_promo_2017_BV1bx41157bP.mp4` | REJECT | Closed-beta promo / QTE PERFECT overlay；not ordinary GL PVE |
| `rejected_not_primary/kr_dark_raid_BV1xp4y1k7y1.mp4` | REJECT | KR dark raid / FULL AUTO / 千万级 HP |

## Engineering (not acceptance)

- `MultiHitRetargetsAfterFocusedEnemyDiesMidSkill` + `ClearDeadFocus` on kill
- Drive select title 52px
- `dotnet test` **152** green

Logs: `16_bili_fetch_round4.json`. Frames: `frames_round4/`. Search API 412; Playwright search + direct BV yt-dlp OK. Hard stop: `../BLOCKED.md`.
