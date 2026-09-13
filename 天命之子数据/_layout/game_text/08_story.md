# 08 剧情 / 任务 / 对话文本盘点

范围：主线章节、关卡名、对白、角色个人剧、活动剧、好感/觉醒剧、纪念馆（Hecate 图书馆）卡面、任务日志、结算/结尾卡。  
对照：`docs/reference/gamekee/`（三站 harvest）、`docs/deep-research-report.md`、`docs/PROJECT_STATUS.md`、`docs/reference/offline-pack-1.1/`、`docs/reference/mobile-archive/`、`client/Assets/Content/catalog.json`、`tools/wiki-harvest/README.md`。  
产品发行名 **契灵回响**。原作台词、关卡名、NPC 名 **禁止进发行包**；本文件只记**结构、量级、缺口**。

计数口径与 `_layout/game_text/20_taxonomy.md` §16–20 对齐。单位是 **string key**。

---

## 0. 结论（先读这个）

| 层 | 本地有没有可用正文 | 说明 |
|---|---|---|
| 主线对白脚本 | **没有** | SQL 无剧情表；`locale.pck` 未解包且 **BAN**；纪念版图书馆只拍到**书架落地页**。 |
| 主线章节/关卡**标题** | **极少、且不可发行** | 纪念版 `090_library.png` 能看见两张主线卡标题；GameKee 有掉落表编号、无完整关卡名表。 |
| 主线**剧情复述** | **半成品、二手** | 国际服 wiki 三篇剧情帖已 harvest；逐关正文帖（`590024.html` 等）**不在树里、本地没有**。 |
| 角色个人剧 / 好感台词 | **没有成表** | 图鉴 CSV 无 flavor/vo 列；wiki 天子页无故事栏。 |
| 活动剧情正文 | **没有** | 三站「活动」几乎全是 Raid/WB/卡池作业。 |
| 任务名（日常/限时/主线日志） | **机制说明有、清单无** | 限时任务、每日任务是攻略文，不是 quest 字表。 |
| 结尾卡 / 章末 still | **没有** | 无 ending card 文案、无 CG 字幕表。 |
| 切片已有 | **12 个自造关卡名** | `catalog.json` `VS-1` + `CH1-2`…`CH1-12`。**零句对白。** |

一句话：完整游戏要的剧情字表，**几乎全是 WRITE**。本地能当参考的只有「章有多少、图书馆分几柜、好感几档解锁什么」；不能当台词库。

三档标记：

- **LOCAL** — 仓库里已有正文/截图/结构数（仍 BAN 原文进包）。
- **WIKI-ONLY** — GameKee 线上或腾讯文档有，harvest 树没收、或只收了导航页。
- **MISSING** — 原作客户端/`locale.pck`/纪念版内页才有，仓库与 wiki 都没有可用正文。

---

## 1. 完整游戏需要哪些字表

对齐 `20_taxonomy.md`。切片（P0）打完一章；内容包（P1）可重看 + 好感短剧；运营期（P2）活动模板。

| 表 id | 内容 | P0 | P1 | P2 | 现况 |
|---|---|---:|---:|---:|---|
| `story.chapter` | 章标题、幕间标题 | ~2 | ~15 | ~40 | 切片无章标题；纪念版可见「第1-1章 / 第6-6章」句式 |
| `story.stage` | 关卡显示名 | 12 | ~80 | ~200 | **HAVE** 12 个自造名 |
| `story.location` | 场景名（可与关卡共用） | ~10 | ~40 | ~100 | 与 stage 混用；无独立表 |
| `story.npc` | NPC 名 | ~5 | ~20 | ~50 | **MISSING**；wiki 设定帖有原作人名（BAN） |
| `story.beat` | 关前/关后 4–12 行 | ~100 | ~2k | ~10k | **MISSING**（切片零行） |
| `story.chapter_cut` | 章间过场 | ~50 | ~500 | ~2k | **MISSING** |
| `story.choice` | 选项（极少） | 0 | ~20 | ~100 | 原作不是大分支；**MISSING** |
| `story.affection` | 好感 B/A/S 个人剧 | — | ~1k | ~4k | **MISSING** |
| `story.awaken` | 觉醒柜短剧（图书馆「覺醒」） | — | ~200 | ~800 | **MISSING**；柜名 LOCAL 截图 |
| `story.event` | Narrative 活动剧 | — | ~400（1 个模板） | ~10k | **MISSING** 正文；wiki 只有作战攻略 |
| `story.library` | 重看卡标题 + 柜名 | ~10 | ~80 | ~200 | 柜名 + 少量卡标题 LOCAL 截图 |
| `story.ending` | 章末/活动末/好感 S 静帧字幕 | — | ~20 | ~80 | **MISSING** |
| `ui.dialogue` | 跳过 / 自动 / 日志 / 语音开关 | ~10 | ~10 | ~10 | `12_system.md` 已拟系统键；剧情壳未接 |
| `vo.home` `vo.battle` `vo.affection` `vo.summon` `vo.story` | 字幕 | — | 数百～千 | 数千 | 图鉴有 **CV 名 439/561**，无台词 |
| `mission.story` | 「通关 1-1」主线日志 | ~12 | ~80 | ~80 | **MISSING** |
| `mission.daily` / `weekly` | 日常/周常名+条件 | — | ~35 | ~70 | wiki 有机制，无完整清单 |
| `mission.event` | 活动 / Mission Pass | — | ~20 | ~200 | wiki 有限时任务**攻略**，无字表 |
| `quest.log` | 任务描述、进度句、领奖 | ~20 | ~80 | ~200 | **MISSING** |
| `npc.home` | 图书馆/向导气泡（非战斗） | ~5 | ~40 | ~80 | 纪念版 1 句 LOCAL；其余 MISSING |

原作量级只用来估表，不拷贝：

| 原作块 | 量（结构，不是可发行文本） |
|---|---|
| 主线 area | SQL `game_area_dungeon` **304**（30 `area_idx` × 普通 152 + 每章困难 152） |
| 主线 stage | `game_area_dungeon_stage` **928**（前 8 区 4 战/副本，9–12 区 4 战，13–30 区 2 战） |
| 星云 | 256 副本 × 8 层 = **2048**（后期系统，不进首批） |
| 中文 locale | `20_taxonomy` 记 ~**81k** 词条（未在本仓库展开） |
| 纪念版天子 | 561；好感 E→S 解锁台词 / Profile / 互动 / Side Story / 外观 |
| 图书馆柜 | 主要 / 特别 / 觉醒 / 其他（纪念版落地页） |
| 活动 | 七年 Narrative + Raid/WB 文案；wiki 不收录对白 |

---

## 2. LOCAL — 仓库里实际有什么

### 2.1 结构（可抄语法，不可抄专名）

`docs/reference/offline-pack-1.1/03-content-loops.md`：

- 普通：一条 **152 节点** 的线性脊。困难：**每章一条链**，开门条件是该章普通通关。
- 每副本末战 `is_boss_stage`；多数 `phase_count=3`。
- 区 1–8：每区 8 普通 + 8 困难，每副本 **4** 战。区 9–12：4+4，每副本 4 战。区 13–30：4+4，每副本 **2** 战。
- `game_area_dungeon.name` 是 **varchar(32) 桩**，dump 说明明确写：**不能从这里恢复剧情**。
- **无** script / dialogue / quest 表。故事在 `locale.pck` / 客户端；**禁止解包进工程**。

`deep-research-report.md`：

- 早期定位 Narrative CCG：世界地图 + 全语音剧情 + 每角色台词/个人剧。
- Affection：E 初始 → D 新台词 → C Profile → B 新互动 → A Side Story → S Costume。
- 内容层：Episode 世界地图（必须）→ Narrative Dungeon（MVP 活动模板）→ Hecate's Library 重看（P1）。
- 切片规格：**1 Chapter、12 战斗关**；剧情 NPC 3、剧情背景 5。
- 停服后 Memorial 只能看立绘等，**不能当可玩剧情源**。

`PROJECT_STATUS.md`：垂直切片已通关卡战斗；**无剧情演出**。硬禁原作 IP。

### 2.2 GameKee harvest（三站 2975 篇，`THREE_WIKIS.md`）

harvest 跟 **wiki 树**走（`harvest.py` / `harvest_site.py`），不是全站帖子。和剧情沾边、**本地 JSON 在树里**的：

| 站 | content_id | 树名 | 实际内容 | 可用性 |
|---|---|---|---|---|
| dc | `154795` | 剧情设定 | 黒角砂糖设定堆叠；正文大量外链 **腾讯文档**；角色折叠条（丽莎等）是短设定不是对白 | LOCAL 导航 + 少量设定；**正文在 QQ 文档 = WIKI-ONLY** |
| dc | `155536` | 剧情梗概 | 主线导航 beta。第一章 1-1…1-8 链到 `590024`–`590033`；幕间 `590034`；2-1 `590035`；2-2 `594526`；**2-3 coming soon** | LOCAL 只有目录；**逐关帖未抓** |
| dc | `166355` | 剧情故事 | 树节点存在、文件极大；未能抽出可读章节目录（不像逐行脚本） | 当「有这篇」即可，**不要当完整脚本** |
| dc | `80696` | 全关卡资源经验值 | 剧情关掉落/经验图。编号例 `2-6-3-1` = Chapter 后数字 / 章节 / 副本 / 关 | **掉落表不是关卡名表** |
| dc | `21773` | 限时任务 | Mission Pass 机制 + 截图例子 | 机制 LOCAL；任务句 **MISSING** |
| dc | `152979` | 夏娃的冒險 | 玩法搬运，不是剧情脚本 | 机制 LOCAL |
| dc | `95787` | 天子语音翻译优化 | 作者停更去写剧情贴；部分角色语音校对 | **抽样台词**，非 561 全表 |
| dc | `21582`–`21593` | 官方漫画 1–6 话 | 图页 | 漫画 ≠ 游戏对白；BAN 原作剧情 |
| dc | `97078` | LOKI_S STORY Ⅰ | 同人/汉化漫画 | 非游戏脚本 |
| dc | `77671` | 游戏系统介绍 | UI 截图导览 | 无任务清单 |
| kr | `82285` | 每日任务 | 入门攻略节点 | 机制向 |
| kr | `165592` | 点火树文字 | 「同人美图/剧情」柜，点火 flavor | **未当剧情脚本解析** |
| jp | `活动攻略` 75 篇 | 千里眼、夏活丽莎补课、RB/WB | 作战日历 | **无对白** |

图鉴拆表 `docs/reference/gamekee/tables/`：`characters.csv` 345 行 5★+4★，字段是面板+技能。**无故事、无台词、无 CV**（CV 在纪念版 OCR → `汇总表.csv`）。`parse_child.py` 只抽技能。

`汇总表.csv` 561 行：`name` / `cv` / 技能句。**无** `flavor` `voice` `story` `profile` 列。  
`02_identity.md`：wiki 天子模板 = 名称/星/属性/定位/技能。**无简介、无召唤台词。**

### 2.3 纪念版截图（`mobile-archive`）

捕获声明 **COMPLETE** 的是图鉴数值/技能，不是剧情内页。

| 文件 | 看见的文本 | 没看见的 |
|---|---|---|
| `090_library.png` | 柜：圖書館 / 主要 / 特別 / 覺醒 / 其他。向导气泡一句。卡：角色名、活动短名、**第1-1章 惡魔的遊樂場**、**第6-6章 不入虎穴，焉得虎子** | 点进卡后的对白、日志、CG |
| `095_eve_adventure.png` | `ADVENTURE OF EVE`、`GAME START`、`尚無可用的遊戲記錄。` | GAME START 之后（明确没点） |
| `CAPTURE_GAPS.md` | — | **Hecate inner pages** 仍是缺口 |
| 图鉴/详情 | 左下「名字 / 台词 / 星」的**版式**（`画风整理.md`） | 台词字符串未 OCR 进表 |
| 魂卡 | 卡底「短台词」版式 | 未进 `魂之歌牌.csv` |

图书馆落地页证明：纪念版仍挂着主线卡（至少看到 **1-1 与 6-6**）和「特別/覺醒」柜。**不等于**本地有脚本。

### 2.4 客户端切片（洁净室，可发行）

`client/Assets/Content/catalog.json`：

| id | name |
|---|---|
| VS-1 | 废都入口 |
| CH1-2 … CH1-12 | 第1章-2 锈轨巷 … 第1章-12 城门守核 |

25 契灵短名 + 技能短名。`schema.md`：**No original Destiny Child names, flavor, or art.**  
无 `story` 节点、无对话 JSON、无 quest 表。战斗结算只有胜负铬（见 `12_system.md`）。

---

## 3. WIKI-ONLY — 线上有、本地树没收或外链

harvest 只抓目录树。下面这些 **GameKee 活站 / 外链还在**，`pages/590024.json` 等 **不存在**：

| 源 | 内容 | 缺口 |
|---|---|---|
| https://dc.gamekee.com/590024.html … 590033 | 梗概帖标的 **1-1～1-8** 逐关复述 | 未进 `wiki_tree` → 未 harvest |
| 590034 / 590035 / 594526 | 幕间、2-1、2-2 | 同上；2-3 原帖已写 coming soon |
| 154795 内腾讯文档 | 剧情目录、韩服前瞻粗翻、男主 Child 查询、活动千里眼（剧情版）、角色词条 | 需登录的 QQ 文档；**不在 pages/** |
| 154795 更新记录点名 | 主线 6-7、6-0 Wish、情人节支线、何仙姑觉醒、WB Eclipse 等 | 译文在外链，不在 JSON |
| 语音贴 95787 | 「想看某角色留言再更」 | 覆盖角色数未核对全集 |
| 日服 `活动攻略` | 75 篇作战 | 无剧情字 |
| 韩服「同人美图/剧情」 | 美图 + 点火树文字 | 不是主线脚本 |

即便补抓 590024 系列：那是**玩家复述**，不是客户端脚本，且停在第二章前期。韩服后续（设定帖写到约第 6 章 + 腰斩）主要在腾讯文档。

**不要**为了填表去解 `locale.pck`。

---

## 4. MISSING — 两边都没有可用正文

| 块 | 为什么没有 | 完整游戏是否要 |
|---|---|---|
| 逐行对白（speaker / line / voice id / 表情） | 不在 SQL、不在 wiki 树、纪念版内页没拍 | **要**。P0 每关短打即可 |
| 928 关官方关卡名 | dump 有 id 无标题；wiki 80696 是掉落图 | P0 用自造 12 名；扩章再写 |
| NPC 全表 | 设定帖是考据，不是字表 | 要（雇主/向导/对手） |
| 好感 D–S 台词、Profile、Side Story | 图鉴无列；wiki 天子页无 | P1 每契灵 3 场 |
| 战斗/家园/召唤语音字幕 | 只有 CV 名 | P1 |
| Narrative 活动脚本 | wiki 只有 Boost/掉率/作业 | P2 一个模板活动 |
| Mission Pass / 每日任务**句子** | 只有「这是什么」攻略 | P1 日常；P2 Pass |
| 章末 ending card、活动结束卡、好感 S 静帧 | 无截图、无 wiki | P1 至少章末 1 张 |
| 对话 UI 日志回放正文 | 图书馆内页未进 | P1 与 `story.library` 绑定 |
| 夏娃迷宫事件句 | 标题页有空存档句；内部未开 | 可不进首批 |
| 星云 2048 层名 | SQL 有怪表无文案 | 不建议首批 |

---

## 5. 原作内容地图（只记形状）

主线玩家口头「第 N 章」≠ SQL 30 个 `area_idx`。图书馆卡写成 **「第1-1章」「第6-6章」**（章-节）。80696 四段编号是 Chapter / 节 / 副本 / 战。

| 玩家可见 | 证据 | 本地脚本 |
|---|---|---|
| 第 1 章（早期流水账：收契灵教学） | 155536 写完 1-1…1-8 + 幕间 | 无 |
| 第 2 章起多线 | 154795「chapter3 起多线」；梗概停在 2-2 | 无 |
| 至少到第 6 章 | 图书馆 6-6 卡；设定帖「主线 6-7」 | 无 |
| 韩服比国际服超前、腰斩 | 154795 | 无 |
| 角色柜（琪隆、塞勒涅…） | `090_library` 「最近閱讀 / 新到貨」 | 无正文 |
| 特别柜（仲夏夜記憶、夏季物語、什麼是愛？） | 同上 = 活动/节日短篇 | 无 |
| 觉醒柜 | 底栏「覺醒」 | 无 |
| 向导 NPC 气泡 | 图书馆落地 1 句 | 无表 |

活动侧：Narrative Dungeon = 新剧情 + Normal/Hard + token + Boost。wiki 把「活动」做成 **Raid/WB/翻牌作业**。丽莎作业 = 养成任务攻略，不是剧情。

---

## 6. 切片 vs 完整游戏（契灵回响）

| | 切片现在 | 完整游戏还要写 |
|---|---|---|
| 章 | 隐含第 1 章，无章标题 | `story.chapter` + 章间过场 |
| 关 | 12 自造地名 | 关前/关后 `story.beat`；主线 `mission.story` |
| 对白 | 0 | P0 ≈ 12×8 行；P1 可重看一章 |
| NPC | 0 | ≥3 半身（研究报告资产表） |
| 图书馆 | 客户端有 Library 屏，无卡数据 | 柜名 + 卡标题 + 回放 |
| 好感剧 | 0 | P1；E–S 解锁句 |
| 活动剧 | 0 | P2 一个 Narrative 模板 |
| 任务 | 无日志 | 日常/周常/主线/领奖 |
| 结尾卡 | 结算胜负 | 章末静帧 1–2 句 |
| 语音字幕 | 0 | 与 `vo.*` 同 key |

`20_taxonomy` 量级：P0 剧情短打 ~150 key；P1 一章完整 + 好感 ~2k–4k line；P2 活动把 `story.event` 拉到 ~10k。不要按原作 928 关 × 全语音去排期。

---

## 7. 建议字表文件（尚未创建）

运行时一张 `id → zh`，不要进原作句。

```
loc/story_chapter.csv     id, title, synopsis
loc/story_stage.csv       id, chapter_id, title, location
loc/story_beat.csv        id, stage_id, slot(pre|post|mid), speaker, line, vo
loc/story_npc.csv         id, name, role
loc/story_affection.csv   id, char_id, rank(B|A|S), speaker, line
loc/story_library.csv     id, shelf(main|special|awaken|other), card_title, script_id
loc/story_ending.csv      id, kind(chapter|event|affection), title, caption
loc/mission_story.csv     id, cond, name, desc, reward_hint
loc/mission_daily.csv     id, name, cond, desc
loc/ui_dialogue.csv       id, zh          # 跳过/自动/日志
```

P0 最小集：`story_stage`（已有 12 名）+ `story_beat`（每关 ≥1 句）+ `mission_story`（12 条通关）+ `ui_dialogue`（可从 `12_system.md` 抽）。

---

## 8. 诚实缺口清单

1. **没有**可编译的原作剧情脚本，也不该有。  
2. GameKee 国际服剧情是**同人复述 + 外链文档**，harvest 只收下三篇树节点；逐关帖和 QQ 文档都还在站外。  
3. 纪念版图书馆能证明「主线卡还挂着、至少看到第 6 章」，但内页对白 **没拍**。  
4. SQL 304/928 是战斗图，不是字表；`name` 桩不能当关卡名用。  
5. 图鉴 561 行解决的是身份和技能，**零行个人剧**。  
6. 三站「活动」页不能当 Narrative 剧本。  
7. 客户端 12 关名是洁净室种子，**后面没有剧情 JSON**。  
8. 若有人用 `locale.pck` 条数（韩 ~20k / 中 ~81k）当 backlog：那是全 UI+剧情+技能的原作包，**禁止当任务列表**。

补洞顺序（产品，不是还原）：先写 P0 12 关短打与任务日志 → P1 图书馆回放 + 好感三场 → P2 一个活动剧模板。原作章节名、对白、ending 只许当**句式参考**，写进包的必须是新中文。
