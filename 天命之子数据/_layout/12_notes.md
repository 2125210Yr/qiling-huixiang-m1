# 12 天子详情 — 接入说明

## 行为

- 默认仍是图鉴 `#gal`。点 `.card` 打开底部 dock（`position:fixed; bottom:0`），不是盖满网格的全屏 modal。
- 同一张卡再点、关按钮、Escape → 关。换另一张卡 → 替换 `#cdet-inner`，始终只有一个 `#cdet`。
- 打开后聚焦 `#cdet-close`；`role="dialog"` `aria-modal="true"`；Tab 限制在面板内。
- 手机：面板 `width:100%`，`max-height:42vh`，技能 `.cdet-sk` 单独滚动。`body.cdet-open` 加底 padding，最后一排卡不被挡住。
- 152px 头像用 `.cdet-face`，**不要**用 `.face`：当前 `汇总表.html` 有 `.face { display:none !important }`。

## 不破坏现有脚本

`12_detail.js` **包装** `setView` / `filt`，不改函数体：

- `setView('tbl')` / `#tbl` → 关面板，避免盖住表。
- `filt()` 把当前卡筛没了 → 关面板。
- `applyHash`、`#gal`、`#r5`–`#r1`、`.rnav` 不碰。详情不用 hash，避免和 `#gal` `#tbl` `#r5` 抢。

## JSON 体积

只放上表字段。禁止复制 `data-hay`、wiki 名、ignited 列、source。技能正文由父脚本从 CSV 原样写入，本目录文件不编技能。

## 色板

与图鉴 `:root` 对齐：`#0b0d12` / `#cf9403` / `#f4f0e8`。属性色与 `.el-fire` 等现有 badge 一致。

## 粘贴清单

1. `12_detail.css` → `<style>`
2. 面板 + `#child-json` → `</main>` 前
3. 父脚本把 CSV 填进 `#child-json`（见 markup）
4. `12_detail.js` → 现有 gallery script **之后**

不要改 `merge_561.py` / `汇总表.html`（本任务）。`gallery_html.py` 的 `write_html` 以后若内联这两份文件：卡片已输出 `data-id`，JSON 用 `r.get("id") or r.get("idx")`。
