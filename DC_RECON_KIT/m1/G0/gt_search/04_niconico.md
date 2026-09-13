# GT search — nicovideo.jp

Date: 2026-09-11  
Scope: 普通 5 人 PVE（ストーリー / ナラティブ / 日常）。JP = `contrast_only`，不得当 GL primary。  
Exclude: Raid / WB / Ragna / 抽卡 / PV / OST。  
Fetch: **未跑 YouTube / yt-dlp。** 未逐帧。未入库 mp4。

## Queries (this pass)

| # | query | result |
|---|---|---|
| 1 | `site:nicovideo.jp デスティニーチャイルド バトル ストーリー` | WebSearch: **No results found** |
| 2 | `site:nicovideo.jp デスティニーチャイルド ナラティブ ストーリー 日常` | **Aborted**（本回合已停搜） |
| 3 | — | not run |
| 4 | — | not run |

未打 nicovideo 搜索页 / snapshot API。未打开任何 watch 页。

## URL table

| url | title | date | dur | region | mode | verdict | reason |
|---|---|---|---|---|---|---|---|
| — | — | — | — | — | — | **NONE** | 本回合 0 条 nicovideo URL |

## Verdict

- KEEP / NEEDS_WATCH / contrast_only 候选：**0**
- REJECT（Raid/WB/Ragna）：**0**（无命中可标）
- GL primary：**无**
- JP 即使后补命中也只标 `contrast_only`

主 GT 仍 `NEEDS_FETCH`。`docs/reference/gl-shutdown-pve/` 仍 0 mp4。
