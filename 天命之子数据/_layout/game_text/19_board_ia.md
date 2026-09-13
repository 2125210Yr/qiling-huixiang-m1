# 19 · 游戏开发文本 — 看板 / 手册接入

本文只说明怎么把模块「游戏开发文本」挂进现有顶栏和总看板。**不要把全文塞进 `总看板.html`。** 看板只放 KPI + 缺口，详情进独立页。本文不改任何 HTML。

对照先例：`星云一趟.html`（提案整页）+ `总看板.html#modes` 里一行跳转。文本模块比星云多一层覆盖数字，所以看板要有独立锚点，不只是模式表里的一行。

---

## 结论（先看这个）

| 问题 | 决定 |
|---|---|
| 全文放看板还是独立页？ | **独立页** `游戏开发文本.html`（与 `星云一趟.html` 同级） |
| 看板做什么？ | 新增 `id="text"` 一节：KPI 条 + 缺口卡 + 链到独立页 |
| 顶栏短名 | **文本**（两字，对齐 看板 / 图鉴 / 星云 / 重构） |
| 手册编号 | `06 · COPY`，宽卡（`mod gold wide`），放在星云卡后面 |
| 共用 CSS | 看板本地样式已够：`.kpi` `.card` `.cov` `.meter` `.chips` `table`。`_layout/shell.css` 只负责 `.mods` / `.page-nav` |

不要：在看板 `#cover` 里加长表、把技能原文贴进看板、给 `汇总表.html` 另做一套顶栏语法。

---

## 1. 现有结构（读过的事实）

### 1.1 顶栏模块开关 `.mods`

共享样式在 `_layout/shell.css`（药丸按钮、`.on` / `aria-current="page"`、`.page-nav` 金链）。注释仍写「设计手册 / UI / 数值 / 动效 / 图鉴」，实际已经是 8 项。

标准顺序（手册 / 看板 / UI / 数值 / 动效 / 图鉴 / 星云 / 重构）：

```
手册 → 看板 → UI → 数值 → 动效 → 图鉴 → 星云 → 重构
```

当前页：`class="on"` + `aria-current="page"`。

**例外：** `汇总表.html` 的 `.mods` 缺 看板 / 星云 / 重构，也没有 `总看板.html` 链。加「文本」时一并补齐，不要只插一项。

### 1.2 页内锚点 `.page-nav`

| 页 | 容器 | 现有锚 |
|---|---|---|
| `总看板.html` | `header.top` → `.page-nav` | `#kpi` 盘点 · `#kids` 天子 · `#elem` 克制 · `#combat` 战斗 · `#buffs` Buff · `#gear` 魂卡人偶 · `#cover` 覆盖 |
| `星云一趟.html` | 同上 | `#idea` `#demo` `#verbs` `#modes` `#not` |
| `战斗与数值.html` | `header > .inner` | `#cover` `#offline` `#formula` `#ignition` `#children` `#wiki5` `#cartas` `#puppets` `#buffs` |
| `战斗动效.html` | 同上 | `#cover` `#hud` `#modes` `#vfx` `#seq` `#ragna` `#raid` `#wb` |
| `视觉图鉴.html` | 第二行 `nav.tabs#nav`（不是 `.page-nav`） | `#overview` … `#dont` |
| `设计手册.html` | 无页内锚；右侧是 caption | — |
| `重构数值.html` | 无 `.page-nav` | — |
| `汇总表.html` | `nav.rnav` 跳稀有度 | `#r5`–`#r1` |

看板正文还有 `#formula`、`#modes`，**没进** `.page-nav`。新模块不要去占这两个 id。

### 1.3 看板 section id（`h2.sec`）

`kpi` → `kids` → `elem` → `combat` → `buffs` → `gear` → `formula` → `modes` → `cover`

`[id]{scroll-margin-top:84px}` 已在看板本地 CSS。新 id 自动吃这条。

星云在看板里**没有**自己的 `h2`，只出现在 `#modes` 表第三行和文末 caption。文本模块不走这条瘦路径：覆盖率是体检数字，需要 KPI。

### 1.4 手册卡片

- 四张 `a.mod.gold` 在 `.grid.g2`：UI / 数值 / 动效 / 图鉴（`01`–`04`）
- 一张 `a.mod.gold.wide`：星云一趟（`05 · PROPOSAL`）
- 下面「哪一页写什么」表 + 「建议阅读顺序」ol

首页文案仍写「四件事，四个模块」，实际已是 5 张入口卡。加第六张时改这句。

---

## 2. 精确插入点

### 2.1 顶栏 `.mods` — 所有设计页

在 **重构** 链**之后**追加一项（提案/重构之后的内容盘点，不插进 UI–图鉴四核）：

```html
<a href="游戏开发文本.html">文本</a>
```

当前页则：

```html
<a href="游戏开发文本.html" class="on" aria-current="page">文本</a>
```

要改的文件与锚点：

| 文件 | 插入位置（最后一个 `<a>` 之后、`</nav>` 之前） |
|---|---|
| `设计手册.html` | L90 `重构数值.html` 之后 |
| `总看板.html` | L130 同上 |
| `视觉图鉴.html` | L343 单行 `.mods` 末尾 |
| `战斗与数值.html` | L49 单行 `.mods` 末尾 |
| `战斗动效.html` | L67 单行 `.mods` 末尾 |
| `星云一趟.html` | L265 之后 |
| `重构数值.html` | L56 之后 |
| `汇总表.html` | L311：先补 `看板` `星云` `重构`，再加 `文本`。建议收成与手册相同的 9 项，不要继续用 5 项残栏。 |
| `游戏开发文本.html`（新） | 自带完整 `.mods`，本页 `class="on"` |

`_layout/shell.css` L1 注释改成包含「文本」。样式本身不用动。

`README.md` 顶栏表加一行：`游戏开发文本.html` — 文案库存 / 缺口。`index.html` 仍跳手册，不必改。

### 2.2 看板 `.page-nav`

`总看板.html` L132–140。在 `#cover` **之后**加：

```html
<a href="#text">文本</a>
```

完整目标：

```html
<nav class="page-nav">
  <a href="#kpi">盘点</a>
  <a href="#kids">天子</a>
  <a href="#elem">克制</a>
  <a href="#combat">战斗</a>
  <a href="#buffs">Buff</a>
  <a href="#gear">魂卡人偶</a>
  <a href="#cover">覆盖</a>
  <a href="#text">文本</a>
</nav>
```

不要新增 `#formula` / `#modes` 进顶栏（本次范围外）。

### 2.3 看板新 section `id="text"`

插在 `#cover` 卡片块结束之后、文末 caption **之前**。

- 结束点：L330–334 那张「这里没有、游戏里有」`.card` 的 `</div>`
- 文末 caption：L336

不要插进 `#kpi` 的 `.grid.g6`（已满 6 格；改 g7 会挤手机）。不要复用 `id="cover"`。

建议骨架（只用已有 class）：

```html
<h2 class="sec" id="text">游戏开发文本</h2>
<p class="lede">纪念版技能原文、Buff 名录、魂卡特效、人偶技能、发行包 25 人台词，摊成覆盖率。全文和词表在独立页。</p>
<div class="chips">
  <span class="chip">纪念版技能槽</span>
  <span class="chip">Buff 93</span>
  <span class="chip">魂卡特效</span>
  <span class="chip">发行包不抄原文</span>
</div>
<div class="grid g5"><!-- 或 g3：kpi 块 --></div>
<div class="grid g2">
  <article class="card">…cov 条…</article>
  <article class="card">…缺口 + 链到独立页…</article>
</div>
<p class="caption">点 <a href="游戏开发文本.html">游戏开发文本</a> 看槽位、乱码名、catalog 禁写规则。</p>
```

KPI 数字从 `_layout/game_text/_stats.json` 取（`_stats.py` 已写）。看板只放 4～6 个大数，例如：

- 天子任一技能文本 / 561（`kids_any_skill`）
- 五槽齐（auto+tap+slide+drive+leader）（`kids_full_kit`）
- 点火三槽齐（`kids_ign_kit`）
- Buff 名录 93
- 魂卡特效（`cartas_fill.special` / 156）
- 人偶任一技能（`pups_any_skill` / 220）

缺口卡用 `.cov` + `.meter i.low` / `.mid`，对 TAP / 点火 U / 队长 / 人偶技能。已有对照：`#cover` 里 TAP 258/561、点火 U 167/561；以 JSON 为准，不要两处数字打架。

文末 L336 caption 追加一句链，仿星云：

```
查文案去 <a href="游戏开发文本.html">游戏开发文本</a>。
```

`#modes` 表**不必**为文本加行（那张表是战场皮，不是文案库）。

`#kpi` 六格保持「人 / 卡 / 偶 / Buff / 人数 / 四弦」。最多在「Buff 名录」那格 `<small>` 里加「技能文本见 #text」，不要第七张 KPI。

### 2.4 设计手册

三处，都在 `设计手册.html`：

**A. 入口卡** — L201 `</a>`（星云宽卡）之后、L203 `h2.sec`「哪一页写什么」之前。复制星云宽卡，改：

```html
<a class="mod gold wide" href="游戏开发文本.html">
  …
  <span class="en">06 · COPY</span>
  <h2>游戏开发文本</h2>
  …
  <span class="go">打开文本模块</span>
</a>
```

截图槽：不要角色胸像。三格可用纯字底（仿图鉴卡「561 · 名 · 属性 · 技能」）或技能/Buff 表的抽象截屏。禁止把原作立绘当封面。

**B. 「哪一页写什么」表** — L204–216 `<table>` 在星云行（L214）和私服行（L215）之间插入：

| 你要找 | 去哪 | 不要去 |
|---|---|---|
| 技能原文覆盖、Buff 名、魂卡特效句子、发行包能写哪句 | `游戏开发文本.html` | 不要把 561 原文粘进 `catalog.json`；看板只有覆盖率 |

**C. 阅读顺序** — L222–228 `<ol>` 末项之后加第 7 步：先看板覆盖，再打开文本页对槽位。L97–98 的 h1 / lede 把「四件事，四个模块」改成承认看板 + 四核 + 星云 + 文本（不要继续数 4）。

手册 `.chips`（L99–105）可加一条链：`<a class="chip" href="游戏开发文本.html">游戏开发文本 · 文案缺口</a>`（已有总看板 chip 先例）。

### 2.5 新页 `游戏开发文本.html` 自己的头

抄 `星云一趟.html` L251–275 / `总看板.html` L116–141：

- `<link rel="stylesheet" href="_layout/shell.css">`
- `header.top` > `.brand`（`DESTINY CHILD` + `游戏开发文本 · 库存`）
- `.mods` 九项，本页 on
- `.page-nav` 只链本页 section，例如：`#stock` `#kids` `#buffs` `#cartas` `#pups` `#pack` `#dont`
- 本地 `<style>` 复制看板色板与 `.kpi` `.card` `.cov` `.chips` `table`（见 §3）。不要引入视觉图鉴的 Cinzel / checker。

独立页可以长（槽位表、乱码样本、catalog 禁写）。看板禁止复制这些表。

---

## 3. 可复用 CSS（不要新造一套皮）

### `_layout/shell.css`（全局已链的页）

- `.mods` / `.mods a` / `.on` / `[aria-current="page"]` / `.ghost`
- `.page-nav` / `header .mods{flex:1}`

看板、手册、星云、重构已 `<link>` 这份。数值 / 动效 / 图鉴 / UI **内联了同样规则**（UI 在 L327–331）。新页用 link，不要第三份复制，除非该页像数值一样完全不链 shell。

### `总看板.html` 本地（L8–112）— 看板新 section 与新页都应抄这些

| class | 用途 |
|---|---|
| `.kpi` `b` `span` `small` | 大数砖。`b` 28px 金，`span` 12px 字距标签 |
| `.card` `h3` | 缺口说明、对照 |
| `.grid` `.g2` `.g3` `.g5` `.g6` | 砖排列；900px 塌成 2 列，560px 1 列 |
| `.chips` `.chip` | 页头约束（纪念版 / 不进发行包） |
| `.cov` `.cov-h` `.meter` `i` `.mid` `.low` | 覆盖条：绿满 / 金中 / 红低 |
| `table` `th` `td` `.sys` | 槽位对照表 |
| `.ok` `.no` | 有 / 无（`--ok` `#7dba6a` · `--miss` `#e07070`） |
| `.bar` `.track` `i` | 若按稀有度画技能填充 |
| `.caption` `.kicker` `h2.sec` `.lede` | 层级 |
| `.verb` | 可选：AUTO/TAP/SLIDE/DRIVE/LEADER 各一句定义 |
| `.filters` `.blist` `.bitem` | 仅当独立页要筛 Buff；看板不要再嵌一套 Buff 搜索（`#buffs` 已有） |

色板变量与看板 `:root` 对齐：`--void #050505` `--gold #CF9403` `--value #FFC400` `--line #2a2418` `--max 1180px`。

### 手册本地

入口卡只用 `.mod` `.mod.wide` `.mod.gold` `.shot` `.body` `.en` `.go`。不要在手册卡上用 `.kpi`。

### 不要用

- 视觉图鉴 `.tabs` / `.phone` / Cinzel（文本页不是画廊）
- 星云 `.phone` 假机（文本不是走一趟）
- 图鉴 `#gal` / `.child-detail`（查人仍去 `汇总表.html`）
- 新全局 class 名。缺口用 `.cov`，不要 `.progress` / `.stat`。

---

## 4. 整页 vs 看板一节

| | 只加看板 section | 独立页 + 看板 KPI（推荐） |
|---|---|---|
| 顶栏能到达 | 只能从看板页内锚 | 任何页 `.mods` 一跳 |
| 手册「模块」叙事 | 破例（星云已经是整页） | 与 05 星云一致 |
| 体积 | 看板已含 Buff JSON + 覆盖表，再塞槽位会过长 | 看板保持「一张体检单」 |
| 和 `#cover` 关系 | 易与 TAP 258/561 重复 | `#cover` 管面板/头像；`#text` 管句子 |

**不要**只做独立页却不改看板：覆盖率会从「现有的一切」里消失。  
**不要**只做看板长文：和 `#buffs` 名录、`#cover` 仪表盘抢职责。

职责切分（对齐手册「哪一页写什么」）：

| 要找 | 去哪 | 不要去 |
|---|---|---|
| 技能原文覆盖率、乱码名、catalog 禁写 | `游戏开发文本.html` | 看板只有 KPI |
| 人数 / 克制 / Buff 名录 / 面板缺口 | `总看板.html` | 文本页不重画 561 星级条 |
| 某句技能原文 | `汇总表.html` | 文本页可链过去，不复制 561 全文 |
| 公式 | `战斗与数值.html` | 文本页不解释 1.4/0.7 |
| 发行包 25 人名字 | `重构数值.html` + 文本页 `#pack` | 561 原名不进包 |

---

## 5. 推荐 IA（实施清单）

```
设计手册.html          目录卡 06 · COPY
总看板.html#text       KPI + 缺口 + 出口
游戏开发文本.html      正文（库存 / 槽位 / 禁写）
汇总表.html            查某一句
重构数值.html          发行包用哪 25 个名字
```

独立页建议锚（写入该页 `.page-nav`，不要写进看板 page-nav）：

1. `#stock` — 库存 KPI（与看板同数，可更细）
2. `#kids` — 天子八槽填充（含按星）
3. `#buffs` — 93 名录是「名+一句」，无百分比
4. `#cartas` — 魂卡特效句
5. `#pups` — 人偶技能句
6. `#pack` — `catalog.json` 25 人 / 技能名（clean-room）
7. `#dont` — 不把纪念版原文、韩文 SQL 名、wiki 串行名写进发行包

看板 `#text` 只保留与 1、7 对应的缩略：四～六块 `.kpi`、两张 `.card`（纪念版填充 / 发行包禁写）、一条出口。

数字源：`_layout/game_text/_stats.py` → `_stats.json`。改表后先跑脚本再填 HTML，避免和 `#cover` 的 258/167 手滑不一致。

### 粘贴顺序（以后改 HTML 时）

1. 九个文件的 `.mods` 加 `文本`（汇总表同时补缺项）
2. `总看板.html` `.page-nav` + `#text` 块 + 文末 caption
3. 新建 `游戏开发文本.html`（shell + 看板 class）
4. `设计手册.html` 宽卡 + 表 + 阅读顺序 + 标题句
5. `README.md` 表
6. `shell.css` 文件头注释

本次任务到此文档为止，不改 HTML。
