# Known YT title-string mirrors (non-YouTube)

Hunt: 2026-09-11. 4 title-string sweeps. Hosts: Bilibili / archive.org / reddit / niconico.  
No yt-dlp YouTube. YouTube / Invidious / Piped 不算镜像。未打开可播非 YT 片。

## Result

| id | channel | title string | era | bili | archive | reddit | niconico | mirror |
|---|---|---|---|---|---|---|---|---|
| `-SUrcOZav_c` | A Yuu | Destiny Child let you experience being a "Whale" in gacha game now. End of Service 21st Sept 23 | late EOS | none | none | none | none | **NONE** |
| `VydVl7BtbKk` | TrickDuck | Destiny Child - El predecesor de NIKKE - Debes Probarlo! Gameplay mas Lore (Android / IOS) | late 2022 | none | none | none | none | **NONE** |
| `QIng41LnZN4` | AD | WHERE TO LEVEL UNITS! \| BEST STAGES FOR LEVELING, FARMING + MORE! - DESTINY CHILD | grind | none | none | none | none | **NONE** |
| `IRDqNAhKKr4` | Ryanto F2P | Destiny Child (Global) - Eternal Vow Normal & Hard Full Run (Bathory Narrative Dungeon) | **cross_era_gl only** | none | none | none | none | **NONE** |

**4/4 NONE.** 没有可入库的非 YT 镜像。`docs/reference/gl-shutdown-pve/` 仍 0 mp4。

## Queries (4)

1. `"End of Service 21st Sept 23" OR "let you experience being a Whale" Destiny Child` + site:bilibili.com / archive.org / reddit.com / nicovideo.jp / b23.tv
2. `"El predecesor de NIKKE" Destiny Child TrickDuck` + 同上
3. `"WHERE TO LEVEL UNITS" "DESTINY CHILD" farming` + 同上
4. `"Eternal Vow" "Bathory" "Destiny Child" Ryanto` + 同上

Bilibili / archive.org / niconico：四次检索均无标题串命中、无 BV/sm/IA item。

## False positives (not mirrors)

- A Yuu：r/DestinyChildGlobal memorial/EOS 帖、r/gachagaming farewell。无 `-SUrcOZav_c`、无 A Yuu 上传。
- TrickDuck：r/NikkeMobile DC 提及。无西语标题、无 `VydVl7BtbKk`。
- AD：r/DestinyChildGlobal 文字练级帖（e3xe88 等）。不是 AD 片。
- Ryanto：2019-09 ND Eternal Vow 更新帖。不是 Ryanto VOD。`IRDqNAhKKr4` 仅标 `cross_era_gl`，不得当停服窗 GT。
