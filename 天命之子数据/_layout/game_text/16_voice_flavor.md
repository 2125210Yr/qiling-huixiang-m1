# 16 · VOICE / FLAVOR / PROFILE

对照：纪念版图鉴 561、三站 GameKee、HTML 图鉴、发行客户端、私服 SQL / 语音包。  
数字：`汇总表.csv` + `_layout/game_text/_stats.json`（`cv` 439/561）。  
本文只盘点**字符串槽**，不拆语音、不把原文写进 `client/`。

---

## 结论

| 槽 | 游戏里有没有 | 我们表里 | 覆盖 |
|---|---|---|---|
| **CV / 声优** | 有。图鉴 基础能力：名旁 `CV:…`（3★ 起常见；1★ 无；2★ 多数无） | `汇总表.csv` 列 `cv`；图鉴详情 `.cdet-cv` | **439 / 561**（78%） |
| **名下口头禅 / 召唤句** | 有。名字下一两行短句 | OCR 解析过 `quote`，**写出 CSV 时丢掉** | **0 / 561** 进表 |
| **简介（lore 段）** | 有。底栏 tab「簡介」长文 | 截图在，**从未切栏 OCR** | **0 / 561** |
| **开始对话（主页台词）** | 有。右栏笑脸「開始對話」（3★+） | 未点开、无文本 | **0** |
| **战斗喊话** | 有。语音包 | 只有 `.ogg` 文件名，无字幕 | **0 条文本** |
| **造型 flavor** | 有。衣柜一行简介 | 姬瓦 2 条手记 + 造型 inspect 样本，无表 | **≈2 条手记** |
| **生日 / 身高 / 喜欢 / 讨厌** | 纪念版图鉴**没有**这些格子 | 三站词条表也没有 | **N/A（游戏侧就空）** |
| **发行包 flavor** | 自造一句 | `CharacterPresenter.Flavor` C001–C025 | **25 句**，与 561 无关 |

所以：**进表的几乎只有 CV 名。** 口头禅、简介、对话、战斗字幕、造型句都还空。生日身高好恶不是漏采——纪念版 UI 就没印。

---

## 1. 游戏实际印了什么

证据：`docs/reference/mobile-archive/screenshots/` 图鉴 基础能力（`300_libstats_*` / `320_4s_*` / `330_3s_*` / `340_2s_*` / `350_1s_*`）+ 持有详情 `020_character_detail_overview.png`。

### 1.1 图鉴「基础能力」底栏（561 张都有）

从上到下：

1. **名字**  
2. **CV: 声优**（有则印在名右；1★ 样张无；2★ 样张无）  
3. **短句**（口头禅 / 契约句；2★ 样张带「」）  
4. Tab **簡介** + 多行设定  

左栏是面板，不是档案。右栏：图库 / 造型列表 / **開始對話**（3★+ 样张有；1★/2★ 样张没有笑脸钮）。

样张：

| 星 | 文件 | CV | 短句 | 简介 | 开始对话 |
|---|---|---|---|---|---|
| 5★ | `300_libstats_000.png` 黑暗塞勒涅 | 本泉莉奈 | 「我以為已忘記的過去，吞噬了我的當下。」 | 长段晴惠设定 | 有 |
| 4★ | `320_4s_stats_000.png` 暗夜祭泰風 | 小田久史 | 「不給糖就搗蛋！…」 | 长段 | 有 |
| 3★ | `330_3s_stats_000.png` 弓箭手天使洛特 | 植田理沙 | 「一個有著嬰兒天使外觀的邪惡Child。」 | 长段 | 有 |
| 2★ | `340_2s_stats_000.png` 黑色的啪布 | 无 | 「如果你想加深友誼…」 | 短设定（元素之灵套话） | 无 |
| 1★ | `350_1s_stats_000.png` 星影靈牛 | 无 | 「一隻看起來十分可愛的靈牛…」 | 短设定 | 无 |

1★/2★ 简介常是同一句「聚集在某個靈魂點的人類的邪念…」。不是独立档案卡。

### 1.2 持有详情（不是图鉴）

`020` 燃燒的姬瓦：名下 **flavor 两行**（「女王花園的入侵者…」），**没有 CV、没有简介 tab**。CV / 简介只在图鉴 基础能力。

### 1.3 造型一行

`024_character_costume.png`：每件造型名 + 一句。手记两条（姬瓦）。`450`–`454` 是图鉴造型 inspect 样本（gallery + flavor +「X 專用」），**315 件未进表**。

### 1.4 没有的格子

全套截图、`DATA_MODEL_FROM_REFERENCE.md`、`OBSERVED_UI_PATTERNS.md` 都**没有**生日、身高、体重、三围、血型、喜欢、讨厌、画师栏。  
天命之子档案 = **CV + 短句 + 简介段 + 对话/战斗语音**，不是学园偶像资料卡。不要按「典型 gacha」去空表里填这几列——游戏没印。

---

## 2. 我们表里实际有什么

### 2.1 `汇总表.csv` / 图鉴 HTML

列：`id…leader_text,cv,source,avatar_source`。  
**没有** `quote` / `profile` / `birthday` / `height` / `like` / `dislike` / `voice_line`。

| 星 | n | cv 非空 | 备注 |
|---|---:|---:|---|
| 5★ | 282 | **274** | 几乎齐 |
| 4★ | 77 | **68** | |
| 3★ | 99 | **92** | 含 `植田理沙`（洛特） |
| 2★ | 53 | **5** | 样张无 CV，这 5 条像 OCR 误读 |
| 1★ | 50 | **0** | 与样张一致 |
| **合计** | **561** | **439** | 看板 `#cover` 同一数 |

来源：纪念版底栏 OCR（`extract_memorial.parse_name`），不是 wiki。国服词条表没有声优列。

图鉴展示：`汇总表.html` / `视觉图鉴.html` 详情只渲染 `CV {cv}`。无 cv 不画。caption：「空值和乱码来自纪念版截图 OCR。」

### 2.2 OCR 管道：quote 采了又扔

`tools/wiki-harvest/extract_memorial.py`：

- `NAME_BOX = (20, 1580, 920, 1980)` → 名字 + CV + 短句  
- `QUOTE_BOX` 定义了，**没人调用**  
- `parse_name` 写出 `cv` + `quote`（短句拼到 200 字）  
- **简介在 NAME_BOX 下面，整段没切**

`merge_561.py` 内存里有 `"quote": r.get("quote")`，`write_csv` 的 `fieldnames` **不含 quote**（`extrasaction="ignore"`）。  
jsonl 也不在 `_extract/`（只剩 `cross_check.md`）。短句等于采过、没落盘。

### 2.3 Wiki 表

| 站 | 天子页结构 | 声优 | 短句 / 简介 | 生日身高好恶 |
|---|---|---|---|---|
| **国服 dc** `pages/` 1227 | 战斗模版：名称 / 稀有度 / 属性 / 定位 / 技能 | **无列**。`characters.csv` 345 行无 cv | 无 | 无 |
| **韩服 destinychild** | 同上战斗向 | 无 | 无 | 无 |
| **日服 dcj** 五星 307 页 | 「基本情报」表：属性 / 定位 / 稀有度 / **声优**；页首常有一句（如密涅瓦「想和我一战的话…」） | **在正文 HTML，未拆表** | 页首一句 ≈ 口头禅；无简介段、无生日身高 | 无 |

`parse_child.py` / `build_tables.py` 只抓元素、职业、面板、技能。日服声优要另写解析才能补 5★ 对照，**补不了 3★ 以下**（dcj 无低星图鉴）。

人偶 / 魂卡 CSV：技能与数值，**无 flavor 列**。纪念版人偶卡有 flavor（INDEX：name + flavor + 技能），未进 `人偶.csv`。

### 2.4 客户端（发行包）

`client/Assets/Content/schema.md`：**禁止**原作名字、flavor、立绘。  
`catalog.json`：C001–C025 + 敌，无 cv / quote / profile。

`CharacterPresenter.Flavor(id)`：25 句自造（「极夜里未化的刃光」…），主页 / 检视当副标题。与纪念版短句无关，**不要**回填 561。

### 2.5 私服 / 语音包（文本仍空）

SQL `characters`：`idx name role stats skill_* attribute…`。**无** cv / 简介 / 生日。  
`user_character.is_open_voice_true3`、`is_voice`：开关，不是台词。

`locale.pck`：文档写明剧情/对白在此，**禁止解包进产品**。当前没有抽出的 loc 表。

磁盘上的音频（`_sandbox/offline-pack-1.1/phone/dataout/.../files/`）：

| 路径 | 数量 | 含义 |
|---|---:|---|
| `asset/sound/voice/*.ogg` | **58790** | 角色/场景语音，文件名数字（`1010010.ogg`…） |
| `asset/info_charactervoices.dat` | 1 二进制 | 角色语音目录，不可当文本读 |
| `asset/info_scenariovoices.dat` | 1 | 剧情语音目录 |
| `bgm/` + `sound/` | 203+535 ogg 等 | BGM / SFX |
| `lastpatchdata295_opt_char_voice.txt` | 极大 | 补丁清单，不是台词 |

有声无字。战斗喊话 / 主页对话的**字幕**不在 SQL、不在 CSV。

---

## 3. 槽位对照（给文本模块用）

| 建议 key | 游戏 UI | 现状 | 下一刀（若要填） |
|---|---|---|---|
| `cv` | `CV:` | **439/561** 已进表、已进详情 | 抽查 2★ 的 5 条；日服 5★ 声优对一下 OCR |
| `quote` | 名下 1–2 行 | OCR 过、CSV 丢了 | 重跑 `NAME_BOX` 或补 `QUOTE_BOX`；`write_csv` 加列 |
| `profile` | 簡介 tab | 561 张截图都有像素 | 新切底栏简介盒再 OCR；1★/2★ 大量套话 |
| `home_lines[]` | 開始對話 | 未采集 | 要另录对话 UI；3★+ 才有钮 |
| `battle_lines[]` | 战斗语音 | 58790 ogg，无字 | 不要从 pck/dat 拆原文进发行包 |
| `skin_flavor` | 衣柜 / 造型 inspect | 2 条手记 + 5 张样本 | 315 造型未扫 |
| `birthday` `height` `like` `dislike` | — | 游戏无栏 | **不要造列** |

发行包继续只用 `name_key` + 自造 `Flavor`。561 的 CV/短句/简介只留在 `天命之子数据/`。

---

## 4. 不要做的

- 不要把纪念版简介/口头禅写进 `catalog.json`。  
- 不要解 `locale.pck` / `info_charactervoices.dat` 来填产品文案。  
- 不要按学园偶像模板加生日身高好恶——截图和三站表都没有。  
- 不要把 `Flavor("C001")` 当成原作短句。
)
