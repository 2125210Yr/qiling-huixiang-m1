# 15 Wiki 语料盘点（可喂字表）

范围仅下列已收获文件。正文来自 GameKee 三站（`dc` 国际服 / `destinychild` 韩服 / `dcj` 日服），harvest 日约 2026-08-27。  
**用途**：给字表提供术语、句式、技能模板；**不是**可整包拷进 `client/Assets/Content` 的洁净室文案（见 `docs/reference/gamekee/INDEX.md`）。  
**不拷贝** 大 JSON/HTML 原文。

语言默认：**简体中文**（社区 wiki）。点火官方说明偏**繁体**。韩/日站正文仍是中文，夹日文声优名、韩服外号。

---

## 喂字表优先级

| 档 | 建议 | 理由 |
|---|---|---|
| `tables/*.csv`（角色/技能/魂卡/人偶/buff） | **主料** | 已拆栏、可检索、技能句式完整 |
| `parsed/wiki_ts_ss_ft.txt` + `formulas.md` | **主料（战斗术语）** | TS/SS/FT/DS、弱点、红攻等固定词 |
| `parsed/wiki_ignition_*.txt` | **主料（养成术语）** | 点火、核心、邪恶之树、红/白词条 |
| `parsed/children.json` 等 raw JSON | 次料 | 与 CSV 同源，更脏（编辑器残渣） |
| `pages_cross/**/*.json` | 次料（外号/跨服名） | 全页 HTML API，需剥标签 |
| `_combined/all_pages.csv` + `三站攻略目录.html` | **目录** | 只有标题/路径，没有正文 |
| `tables/avatars/`、`_combined/battle_*` | 不喂字表 | 图/视频 |

---

## 1. `docs/reference/gamekee/parsed/`

国际服 `dc` 拆出来的结构化稿 + 几篇攻略纯文本。

| 文件 | 种类 | 语言 | 完整度 | 字表价值 |
|---|---|---|---|---|
| `children.json` | 5★+4★ 天子 345 条：`name` / `element` / `role` / 技能长串 / `stats` | 简中（技能原文，夹英文槽位 TS/SS/DS/U） | **技能文较全**（TS 345/345）；`stats.initial`/`uncap6` 多数空；`role` 约半空；技能字段互相截断重复（`tap` 里仍带着 SS/DS） | 用 `tables/characters.csv` 更干净；本文件当校对 |
| `buffs.json` | 增益/减益名+效果 | 简中 | **半成品**：52 条（`kind` 52），且 `name`/`effect` 常串到下一条。干净表是 `tables/buffs.csv` 的 **93** 行 | 勿直接喂；用 CSV |
| `equipment.json` | 武器/防具/饰品三页 `raw_head`（+15 数值） | 简中表头 + 数字 | **残**：无装备名；`plus15_samples` 空；B 装空白；约 6–7k 字符/页全是表格残渣 | 几乎不可用 |
| `puppets.json` | 人偶 220：`name`/`rarity`/`text` 整页粘贴 | 简中 | 结构化 `tap/slide/drive/leader` **多为 null**；技能埋在 `text`，夹「是否为二级目录：删除」 | 用 `tables/puppets.csv` |
| `soul_cartas.json` | 魂卡 156：`name`/`special`/`text` | 简中 | `plain`/`flash` 结构化多为 null；特效在 `special`/`text` 里，满破栏常空 | 用 `tables/soul_cartas.csv` |
| `formulas.md` | 伤害公式笔记（源 `dc/52136`） | 英文整理 + 中文术语 | **完整短稿**（约 70 行）：TS/FT、SS、属性暴击表、点火红字、百分比刀、覆盖规则 | 战斗 UI 术语、公式旁注 |
| `wiki_ts_ss_ft.txt` | 玩家实测长文（同 52136） | 简中 | **正文完整**（197 行）：名词解释 + EX1–4；缺图；作者声明 SS 敏捷项未测完 | 高：技能描述用词（最终伤害、加成伤害、无视防御…） |
| `wiki_dmg_method.txt` | 应是计算方法转贴 | 简中 | **空壳**：仅 2 行「转自 moot / 各服计算方法应该是一样的」 | 无 |
| `wiki_food.txt` | （预定料理/食物？） | — | **空文件** | 无 |
| `wiki_ignition_intro.txt` | 官方点火系统说明（赫菲斯托斯锻造屋，2020-12-03） | **繁体为主**（透过/帐号/机率）夹简体编辑残渣 | **说明文完整**（约 102 行）；图、机率表未抽；多处「是否为二级目录：删除」 | 高：系统说明句式（需洗编辑器垃圾、统一简繁） |
| `wiki_ignition_teach.txt` | 萌新点火教学（彼列实例） | 简中 | **教程文完整**（约 117 行）；评级图、截图未抽 | 高：养成流程用词 |
| `wiki_ignition_pick.txt` | 点火石挑选/评级 | 简中 | **大纲残**：更新日志 + 章节标题；评级表在图里，正文写「下方内容停止更新」 | 中：核心种类名（增幅攻击力/防御力/敏捷度/暴击） |
| `wiki_ignition_affix.txt` | 红攻/红防/红暴/红敏/白字取舍 | 简中 | **评论文完整**（约 54 行）；韩服三图未收录 | 高：红/白词条口语 |

---

## 2. `docs/reference/gamekee/tables/`

README：从国际服已抓 **1227** 篇里拆出的 **5★+4★ 原文表**。给人看的 561 图鉴在 `天命之子数据/汇总表.html`，这里另有一份拷贝。

| 文件 | 种类 | 语言 | 完整度 | 字表价值 |
|---|---|---|---|---|
| `README.md` | 拆表说明与覆盖率 | 简中 | 完整元数据 | 口径，不进字表 |
| `characters.csv` / `.json` | 天子 **345** 行：面板 + `auto/tap/slide/drive/leader` + 点火 U | 简中技能原文 | 属性 345/345；职业 176/345；初始 HP 192/345；满破 HP 73/345；TS 文 345/345；点火 U 210/345。无 3★ 及以下独立页 | **主技能句库** |
| `skills.csv` / `.json` | **1725** 行（约 345×5 槽：auto/tap/slide/drive/leader） | 简中 | 每槽 `text` + `text_ignited` + MIN/MAX/U 伤害数字。4★ 点火 U 大多空 | 按槽位拆技能字符串 |
| `soul_cartas.csv` / `.json` | 魂卡 **156** | 简中 | 分类/限定/特效 153/156；至少一项基础数值 122/156；满破大量空。5★ 134 / 4★ 13 / 3★ 9 | 魂卡特效句（「装备时…」「在 PVP 中…」） |
| `puppets.csv` | 人偶 **220** | 简中 | 名/稀有度 100%；属性 218/220；技能碎片：tap 83 / slide 104 / drive 56 / leader 49。`lv70` 全是字面 `LV70`，无成长文 | 短被动句 |
| `buffs.csv` | Buff **82** + Debuff **11** = **93** | 简中短名+一句效果 | 相对 `parsed/buffs.json` 干净、条目更多 | **状态名主表** |
| `汇总表.csv` | 纪念版+wiki 合并图鉴 **561** 天子 | 简中 | 与 `天命之子数据/汇总表.csv` 同口径：5★282 / 4★77 / 3★99 / 2★53 / 1★50。技能文几乎只在 5★/4★（全套约 258，点火套约 166）。3★ 及以下基本无技能文 | 已进图鉴；低星只有名/属性 |
| `汇总表.html` | 561 卡面图鉴页（设计稿壳） | 简中 UI | 展示用，技能嵌在卡数据里 | 不适合当语料源（用 CSV） |
| `avatars/` | 721 头像（天子 345 + 魂卡 156 + 人偶 220；png/jpg） | — | 图，无字 | 不喂 |

缺口（README 已写）：3★/2★/1★ 无独立 wiki 页；装备图鉴几乎有图无字，未进表。

---

## 3. `docs/reference/gamekee/_combined/`

三站目录 + 战斗参考图/视频。**可喂字表的只有 CSV。**

| 路径 | 种类 | 语言 | 完整度 | 字表价值 |
|---|---|---|---|---|
| `all_pages.csv` | 三站词条目录 **2975** 行（header 外） | 标题简中 | **目录完整**（ok=1）：`dc` **1227**（L2–1228）+ `destinychild` **1006**（L1229–2234）+ `dcj` **742**（L2235–2976）。列：`wiki,wiki_label,section,name,content_id,path,ok` | 栏目名、攻略标题、图鉴路径；**无正文** |
| `battle_refs/` | HUD/PVP/Raid/WB/点火/系统截图 + 空的 `manifest.txt` | 图上有韩/日/英 UI | 视觉参考，未 OCR | 不喂（除非另做 OCR） |
| `battle_vids/` | 3 条 mp4（`89jpoNqAwa8` / `mJrT2conPCI` / `Vdf4V693IcU`）+ frames/keys/crops 大量 jpg | 影像 | 战斗动效参考 | 不喂 |

`all_pages.csv` 栏目抽样：国际服含新手入门、太空漫步、地狱地铁、高级讨论、图鉴、同人博物馆、日韩专区；韩服入门/玩法/抽卡/进阶/图鉴；日服新手/进阶/常驻/活动/人偶/五星/花牌。

---

## 4. `docs/reference/gamekee/pages_cross/`

跨服对名用的 **子集** 正文（GameKee API 整包 JSON：`code/msg/data`，`data.content` 为 HTML 表）。不是 2975 篇全量（全量在 `docs/reference/gamekee/pages/`、`destinychild/pages/`、`dcj/pages/`，本任务未盘）。

索引：`docs/reference/gamekee/cross_index.csv`（712 行数据）。

| 目录 | n | 种类 | 语言 | 完整度 |
|---|---:|---|---|---|
| `dcj/` | **306** | 日服天子页（五星为主，含四星/泡面） | 中文社区名 + 日文声优；标题常是外号（光摩根、奥拉夫、双马尾）而 `title`/`summary` 才是正式称呼 | API `code=0`；技能在 HTML 表（普攻/重击 NS/滑动/大招/队长 + U）；评价区常敷衍（如「1234567」） |
| `destinychild/` | **406** | 韩服图鉴页（五星 + 四星 +「有价值的三星」） | 中文；大量仓管外号（水老头=塔纳托斯、电话妖、毛妹） | 同上，HTML 技能表 + 推荐用途（WB/仓管等） |

字表用法：剥 HTML 后可收 **跨服别名、日文声优、技能表另一译法**。不要把外号当正式角色名。体积大，禁止整文件拷进字表仓库。

---

## 5. `天命之子数据/三站攻略目录.html`

可筛选目录页（`lang=zh-CN`），数据与 `all_pages.csv` **同一套 2975 条**。

- 国际服 `data-w="dc"`：约 L26–1252 → **1227**
- 韩服 `destinychild`：约 L1253–2258 → **1006**
- 日服 `dcj`：约 L2259–3000 → **742**
- 链到 `https://www.gamekee.com/{alias}/{id}.html`，**页面内无词条正文**
- 脚本只做标题/站点过滤

字表价值：栏目中文（新手入门、地狱地铁、花牌…）与攻略题名；正文仍要回 wiki dump。

---

## 6. 对字表的具体可抽字段

已清洗、可直接扫：

- 技能模板：`对目标造成X（Y）伤害` / `N秒内` / `优先` / `最低HP` / `点火 U`
- 槽位名：普攻、重击/TS、滑动/SS、大招/DS、队长技；韩日页还用 NS
- 职业/属性：攻击型、辅助、治疗、干扰、防御；火木水光暗
- 状态：`tables/buffs.csv` 93 条短定义
- 点火：增幅攻击力/防御力/敏捷度/暴击；红攻红防红暴红敏；白攻白血；核心/狗粮/邪恶之树
- 玩法标签：PVP、WB、Raid、RB、太空漫步、地狱地铁、5 人/20 人巨型 BOSS

不要当正式文案源：同人博物馆、外号、攻略评价、QQ、编辑器指令、「是否为二级目录」。

---

## 7. 缺口（本盘范围内）

| 缺 | 说明 |
|---|---|
| 国际服 1227 篇**正文**未在本清单展开 | 正文 JSON 在 `docs/reference/gamekee/pages/`（1227），此处只有拆表 + 7 篇 txt |
| 韩/日全量 1006+742 正文 | 在各自 `pages/`，`pages_cross` 只是对名子集 |
| `wiki_food.txt` / `wiki_dmg_method.txt` | 空或两行 |
| 装备名 | `equipment.json` 无名称 |
| 3★ 以下技能 | wiki 无独立页；`汇总表` 低星无套装技能文 |
| 图内字 | 点火评级表、韩服石头图、战斗 HUD 均未 OCR |
| 简繁混排 | 点火介绍繁体，其余简体 |

对照全量目录见 `docs/reference/gamekee/THREE_WIKIS.md`（2975 篇，失败 0）。
