# 契灵回响 · 文案表总目 `20_taxonomy`

> 这是字符串包的**主索引**。不是「我们已经有什么」，而是一款 Destiny-Child 式 **5v5 节奏战斗抽卡** 从垂直切片打到 live-ops 必须出货的全部文本表。
>
> 产品内部名 Resonance / 契灵回响。发行包 **禁止** 原作 IP 名、原作技能散文、原作关卡名、原作 UI 口号。纪念版 / GameKee / SQL / `locale.pck` 只许当**结构与量级**参考。
>
> 计数单位是 **string key**（一条可本地化词条）。量级用 `~10 / ~100 / ~1k / ~10k`。P0 列写切片出货量，P1/P2 写该表扩到内容包 / 运营期的量级。

## 0. 怎么读

| 列 | 含义 |
|---|---|
| `id` | 稳定表号。将来 CSV / JSON 文件名用它。 |
| `~n` | 量级（一位有效数字）。括号里是切片更紧的估数。 |
| `P` | **P0 ship** 切片能打完一章并养成；**P1 content** 日常循环 + 图鉴厚度 + 一章完整剧情；**P2 live-ops** 活动模板与轮换皮。 |
| `src` | **WRITE** 必须干净写作；**HARVEST** 只收结构/分类/句式骨架，名词全换；**MIX** 骨架可收、句子必须新写；**HAVE** 切片里已有干净种子（`catalog.json` 或客户端硬编码）。 |

**Harvest 红线**

- 可收：属性循环、职业五格、技能槽 AUTO/TAP/SLIDE/DRIVE/LEADER、Drive>Slide>Tap 覆盖、Buff 93 类目、关卡套数、道具 category、locale 条数（韩 20470 / 中 81058）。
- 不可进发行包：`汇总表.csv` 天子名与技能原文、`人偶.csv` / `魂之歌牌.csv` 卡面名、wiki 关卡名、`locale.pck` 任意原文、纪念版 UI 口号（`IT'S SHOWTIME`、Child、Soul Carta、Ragna Break、S CLASS 等）。
- 动词可学、专名必须换。技能描述用模板槽（`{dmg}` `{dur}` `{n}` `{role}` `{el}`），不要粘贴 OCR。

**切片已有种子（不要当全集）**

- `client/Assets/Content/catalog.json`：C001–C025 名、技能短名、12 关名、6 敌+1 Boss、8 个 effect id。
- `GameRoot` 屏：Boot Home Characters Team Stage Battle Result Archive Library Deep Settings Summon Daily Shop Mail Achieve Title Friend Rest Food Pvp Tutorial Costume。
- 硬编码铬：底栏六键、战斗开始、胜利/失败、入门三页。**没有** skill desc、人物小传、关卡剧情、道具说明、概率公示。

**原作量级对照（只用来估表，不拷贝）**

| 原作块 | 量 |
|---|---|
| 纪念版天子 | 561（5★282） |
| 韩服 SQL 角色 | 637 |
| 技能文本（TAP 等） | 5★ 约 200 条有文；全量技能 id ~2170 |
| Buff 名录 | 93 |
| 魂卡 | 156（SQL category=8 ~316 = 普/闪） |
| 人偶 | 220（SQL 253） |
| 皮肤 | 1866 |
| 道具 | 2952 + 词缀 4406 |
| 主线/困难 area | 304 套 / stage 928 |
| 星云 | 2048 关 |
| 中文 locale | ~81k 词条 |

本产品不复刻 561 人。切片 25 契灵；P1 扩到 ~40–80；P2 才按活动加人。文案量按**可玩产品**估，不是按停服全集。

---

## 1. 本地化管道

| id | 内容 | ~n P0 | P | src | 备注 |
|---|---|---|---|---|---|
| `loc.meta` | 语言名、复数、RTL、字体回退 | ~10 | P0 | WRITE | `zh-Hans` 先发；`en` 可空表占位。 |
| `loc.fallback` | 缺 key 时显示的骨架句 | ~10 | P0 | WRITE | 禁止把 key 打到 HUD。 |
| `loc.plural` | `{n} 个` / `{n}s` | ~10 | P1 | WRITE | 中文几乎不需要，英文本要。 |

---

## 2. 通用铬 UI

所有按钮、空态、确认框。切片现在散在 `GameRoot` / `UiChrome` / 各 Board。

| id | 内容 | ~n P0 → P1 → P2 | P | src | 备注 |
|---|---|---|---|---|---|
| `ui.common` | 确定 取消 关闭 返回 下一步 跳过 重试 收下 知道了 | ~100 | P0 | HAVE+WRITE | 客户端已有一小撮。扩到统一表。 |
| `ui.nav` | 底栏六键：首页 / 契灵 / 出战 / 档案 / 图鉴 / 深层 | ~10 | P0 | HAVE | 纪念版六页签语法可学，图标与用词自造。 |
| `ui.boot` | 闪屏、点按开始、版本号旁白 | ~10 | P0 | HAVE+WRITE | `BootSplash`。 |
| `ui.home` | 体力/金/晶、快捷入口（召唤 日常 商店 邮件…） | ~100 | P0 | HAVE+WRITE | 切片 GhostBtn 已列入口；文案要收进表。运营期 Home 再加活动条。 |
| `ui.filter` | 筛选：属性 职业 星 持有 可进化 可突破 | ~50 | P0 | WRITE | 编队/图鉴/背包共用。 |
| `ui.sort` | 战力 星 等级 入手 属性 | ~10 | P0 | WRITE | |
| `ui.empty` | 空背包、无邮件、无好友、未解锁 | ~50 | P0 | WRITE | 每屏一条空态。 |
| `ui.confirm` | 高危确认：解散、覆盖编队、十连、卖出 | ~50 | P0 | WRITE | 含 `{item}` `{cost}` 插值。 |
| `ui.settings` | BGM SFX 语音 语言 画质 账号 重置存档 | ~100 | P0 | HAVE+WRITE | `SettingsModal`。 |
| `ui.loading` | 读条短句 | ~10 P0 / ~100 P1 | P0 | WRITE | 与 `help.tip` 可交叉引用。 |
| `ui.toast` | 弱提示：已保存、体力不足、编队不满 | ~100 | P0 | WRITE | |
| `ui.date` | 剩余 `{hh:mm:ss}`、每日重置 | ~10 | P1 | WRITE | 无后端也要给日常本。 |
| `ui.accessibility` | 色弱、减动效 | ~10 | P2 | WRITE | 可后置。 |

---

## 3. 枚举与战斗词典

短标签。HUD、筛选器、技能模板都引用这里，禁止各表各写一份「火」。

| id | 内容 | ~n | P | src | 备注 |
|---|---|---|---|---|---|
| `enum.element` | 火 水 木 光 暗（+空，若以后要） | ~10 | P0 | MIX | 五行名通用，**图标与罗马字**自造。克制说明在 `help.codex`。 |
| `enum.role` | 攻击 防御 干扰 治疗 辅助；狗粮/经验体 P1 | ~10 | P0 | MIX | `Enums.Role`。素材型是 P1。 |
| `enum.rarity` | 1★–6★ 显示名 | ~10 | P0 | WRITE | 不要 S CLASS。 |
| `enum.stat` | HP ATK DEF AGL CRT 及「战力」 | ~10 | P0 | MIX | 五维沿用玩家熟悉缩写可以；中文全称要自定。 |
| `enum.skill_slot` | 连击 / 点按 / 上滑 / 驱动 / 队长 | ~10 | P0 | WRITE | 产品用词已是点按/上滑/驱动，不要 TAP 当玩家可见字（调试层可留英文）。 |
| `enum.drive_timing` | 偏 / 可 / 佳 / 核（对应 Bad–Perfect） | ~10 | P0 | WRITE | 切片 HUD 仍可能残留 GOOD。出货必须中文产品词。 |
| `enum.auto_mode` | 手动 / 半自动 / 全自动 | ~10 | P0 | MIX | |
| `enum.speed` | ×1 ×2 ×3 | ~10 | P0 | WRITE | |
| `enum.target` | 自身、血最低盟友、全体盟友、随机敌… | ~10 | P0 | MIX | `TargetRule`。技能描述生成器用。 |
| `enum.difficulty` | 普通 / 困难 / 深域 | ~10 | P0 | WRITE | |
| `enum.affection` | E D C B A S 阶名 + 解锁说明 | ~10 | P0 | MIX | 结构收自原作好感阶，名称可保留字母（通用）。 |
| `enum.uncap` | +0–+6 | ~10 | P0 | WRITE | |
| `enum.ignition` | 燃起 1–12 档名 | ~10 | P1 | WRITE | 切片有 6/12 点 UI，玩家可见说明 P1 补齐。 |
| `enum.battle_result` | 胜利 失败 超时 撤退 | ~10 | P0 | HAVE | |
| `enum.currency` | 见 `item.currency`，这里只放短标签 | ~10 | P0 | WRITE | |

---

## 4. 战斗 HUD 与结算

节奏战斗的「这一分钟」全部可见字。P0 必须完整，否则切片看起来像调试器。

| id | 内容 | ~n P0 → 后期 | P | src | 备注 |
|---|---|---|---|---|---|
| `battle.hud` | 暂停 倍速 自动 驱动条 Fever 条 倒计时 波次 | ~50 | P0 | HAVE+WRITE | `BattleHud`。 |
| `battle.phase` | 波次 `{n}`、阶段 `{n}`、时间到 | ~10 | P0 | WRITE | 不要用英文 PHASE 当玩家字。 |
| `battle.banner` | 上滑全屏词、驱动全屏词、Fever 窗、连击 | ~10 | P0 | WRITE | **禁止** `IT'S SHOWTIME`。自造 4 条标语即可。 |
| `battle.float` | 暴击 / 弱点 / 抵抗 / Miss / Heal / 免疫 | ~50 | P0 | MIX | 通用战斗词，避开原作独特 Buff 花名。 |
| `battle.qte` | 判定四档 + 「点核」提示 | ~10 | P0 | WRITE | |
| `battle.warning` | Boss 预警、全屏技能警告 | ~10 P0 / ~50 P2 | P0 | WRITE | Ragna/WB 预警走 P2。 |
| `battle.pause` | 继续 / 撤退 / 设置 | ~10 | P0 | HAVE | `PauseBoard`。 |
| `battle.result` | 完成/失败、伤害合计、三星条件、下一关/再战 | ~50 | P0 | HAVE+WRITE | `ResultBoard`。 |
| `battle.loot` | `{item} ×{n}`、首次通关、掉落原因 | ~50 | P0 | WRITE | `LootPopup`。 |
| `battle.friend` | 助战队长技一句 | ~10 | P1 | WRITE | 无后端也要本地假助战文案。 |
| `battle.ko` | 退场、全灭 | ~10 | P0 | WRITE | |

---

## 5. 契灵（可玩角色）

切片 25 = 5 属性 × 5 职业。P1 加低星狗粮与重复卡。P2 活动新契灵。

| id | 内容 | ~n P0 | ~n P1 | ~n P2 | P | src | 备注 |
|---|---|---|---|---|---|---|---|
| `char.name` | 显示名 | 25 | ~80 | ~200 | P0 | HAVE | `catalog.json` 已有干净名。扩编必须新写，禁 561。 |
| `char.title` | 称号/外号（列表副行） | 25 | ~80 | ~200 | P0 | WRITE | 现表没有。 |
| `char.tagline` | 首页一句 | 25 | ~80 | ~200 | P0 | WRITE | Home 身份块。 |
| `char.profile` | 图鉴短传 80–160 字 | 25 | ~80 | ~200 | P1 | WRITE | 切片可缺；图鉴页 P1 必填。 |
| `char.profile_long` | 长传 / 世界观条目 | — | ~80 | ~200 | P1 | WRITE | |
| `char.cv` | CV 署名 | 0 | ~80 | ~200 | P1 | WRITE | 无配音就写「未定」。禁原作 CV 表。 |
| `char.unlock` | 获取途径一句 | 25 | ~80 | ~200 | P0 | WRITE | 主线/召唤/活动。 |
| `char.secret` | 好感解锁隐藏句 | — | ~400 | ~1k | P1 | WRITE | 每契灵 4–8 条随 E→S。 |

---

## 6. 技能

每契灵 5 槽：连击 / 点按 / 上滑 / 驱动 / 队长。燃起再出一套 `ign` 描述。

| id | 内容 | ~n P0 | ~n P1 | P | src | 备注 |
|---|---|---|---|---|---|---|
| `skill.name` | 技能短名 | ~150（25×5 + 敌） | ~1k | P0 | HAVE | catalog 已有短名；敌技能多重复，P0 给敌人独立名。 |
| `skill.desc` | 玩家可见效果句（模板展开） | ~150 | ~1k | P0 | WRITE | **切片最大缺口**。现只有数值无描述。 |
| `skill.desc_ign` | 燃起后差分句 | ~50 核心 | ~1k | P1 | WRITE | 未燃起可隐藏。 |
| `skill.desc_short` | 预约盘 / 编队一行 | ~150 | ~1k | P0 | WRITE | 详情弹层用。 |
| `skill.template` | 句式骨架 `{actor} 对 {target} 造成 {dmg}…` | ~50 | ~100 | P0 | MIX | 可从 wiki **句式**收类（减益/回复/暴击/弱点），禁粘贴。 |
| `skill.leader_cond` | 队长技触发条件（属性/职业/模式） | ~50 | ~200 | P1 | WRITE | 切片队长技是常时。 |
| `skill.reserve` | 预约槽 N/S/E 说明 | ~10 | ~10 | P0 | WRITE | Inspect 已有控件。 |

---

## 7. 效果（Buff / Debuff）

战斗可读性靠这一张。原作名录 93；切片 `catalog.effects` 仅 8 个 id。

| id | 内容 | ~n P0 | ~n P1 | P | src | 备注 |
|---|---|---|---|---|---|---|
| `fx.name` | 状态短名 | ~30 | ~100 | P0 | MIX | 通用「攻击↑」可写；花名（雪球炸弹、乱舞之刃）**必须新写或砍掉**。 |
| `fx.desc` | 一行效果 | ~30 | ~100 | P0 | MIX | `buffs.csv` 只作分类：输出/生存/控制/DOT。 |
| `fx.hud` | 图标旁 2 字 | ~30 | ~100 | P0 | WRITE | 战场小标。 |
| `fx.group` | 覆盖组名（给调试/图鉴） | ~20 | ~40 | P0 | MIX | Drive>Slide>Tap。 |
| `fx.codex` | 图鉴长解释 + 叠层规则 | — | ~100 | P1 | WRITE | |

---

## 8. 敌人与 Boss

| id | 内容 | ~n P0 | ~n P1 | ~n P2 | P | src | 备注 |
|---|---|---|---|---|---|---|---|
| `enemy.name` | 杂兵名 | 6 | ~40 | ~100 | P0 | HAVE | E001–E005。 |
| `enemy.title` | 变体后缀（精英/狂化） | ~10 | ~50 | ~100 | P1 | WRITE | |
| `boss.name` | 章 Boss / 活动 Boss | 1 | ~20 | ~80 | P0 | HAVE | `城门守核`。 |
| `boss.intro` | 进场一句 / 字幕 | 1 | ~20 | ~80 | P0 | WRITE | `VfxBossIntro`。 |
| `boss.phase_line` | 转阶段台词 | 2 | ~40 | ~200 | P1 | WRITE | |
| `boss.skill_callout` | 大招点名 | ~5 | ~50 | ~200 | P1 | WRITE | |

---

## 9. 关卡 / 世界地图

SQL 证明 area 304 + stage 928 + 星云 2048。切片一章 12 普通（困难未进 catalog 文案）。

| id | 内容 | ~n P0 | ~n P1 | ~n P2 | P | src | 备注 |
|---|---|---|---|---|---|---|---|
| `world.name` | 世界/篇章总名 | 1 | ~8 | ~20 | P0 | WRITE | 废都。 |
| `chapter.name` | 章名 | 1 | ~8 | ~20 | P0 | WRITE | |
| `chapter.synopsis` | 章导语 40–80 字 | 1 | ~8 | ~20 | P0 | WRITE | |
| `stage.name` | 关名 | 12 | ~200 | ~1k | P0 | HAVE | `废都入口`…`城门守核`。困难 12 关 P0 也要名。 |
| `stage.blurb` | 预览一句 | 12 | ~200 | ~1k | P0 | WRITE | 现无。 |
| `stage.condition` | 胜利/三星条件 | ~10 模板 | 同左 | 同左 | P0 | WRITE | 「90 秒内击破」「无人倒地」。 |
| `stage.recommend` | 推荐属性/等级 | 12 | ~200 | — | P1 | WRITE | 可用生成。 |
| `area.daily` | 日常本名 | 3 | ~10 | ~20 | P1 | WRITE | 经验 / 进化 / 金。 |
| `nebula.node` | 星云层名 | — | — | ~2k | P2 | WRITE | 2048 关用生成器：`星云 {el} {layer}-{n}`，禁 SQL 韩文。 |

---

## 10. 道具、货币、掉落

SQL item 2952。切片只要核心经济能转。

| id | 内容 | ~n P0 | ~n P1 | ~n P2 | P | src | 备注 |
|---|---|---|---|---|---|---|---|
| `item.currency` | 金 / 晶 / 体力 / 黑石(好感) / 进化石 / 券 | ~10 | ~20 | ~40 | P0 | WRITE | 不要 Crystal/Onyx 原名。 |
| `item.material` | 强化/进化/突破材料 | ~20 | ~80 | ~150 | P0 | WRITE | |
| `item.consumable` | 体力药、经验食 | ~10 | ~30 | ~50 | P1 | WRITE | `FoodBoard`。 |
| `item.ticket` | 召唤券、活动券 | ~5 | ~20 | ~80 | P1 | WRITE | |
| `item.event` | 活动代币 | — | ~5 | ~100 | P2 | WRITE | 每期新币。 |
| `item.desc` | 所有道具说明 | =上列合计 | ~200 | ~1k | P0 | WRITE | 每件 1 句 + 获取途径。 |
| `item.use_fail` | 无法使用原因 | ~20 | ~50 | ~50 | P0 | WRITE | |
| `loot.reason` | 首次、三星、掉落、分解 | ~10 | ~20 | ~20 | P0 | WRITE | |

---

## 11. 装备

原作三槽 + 词缀 4406。切片 Inspect 四井（武器/防具/饰品 + 第四）。不要发明六格符文。

| id | 内容 | ~n P0 | ~n P1 | ~n P2 | P | src | 备注 |
|---|---|---|---|---|---|---|---|
| `gear.slot` | 武器 防具 饰品（+第四槽名） | ~10 | ~10 | ~10 | P0 | WRITE | |
| `gear.name` | 装备名 | ~15 | ~120 | ~400 | P0 | WRITE | P0 每槽 5 件即可。 |
| `gear.set` | 套装名与 2/3 件效果句 | — | ~40 | ~100 | P1 | WRITE | |
| `gear.option` | 词缀短名 | ~20 | ~200 | ~1k | P1 | MIX | SQL 4406 是数值行；玩家可见词缀类型 ~百。禁原名。 |
| `gear.enhance` | +1–+15 确认/失败 | ~20 | ~20 | ~20 | P0 | WRITE | |
| `gear.craft` | 工坊 UI | — | ~50 | ~50 | P1 | WRITE | |

---

## 12. 养成流程文案

| id | 内容 | ~n P0 | ~n P1 | P | src | 备注 |
|---|---|---|---|---|---|---|
| `growth.level` | 升级、经验溢出、满级 | ~20 | ~20 | P0 | WRITE | |
| `growth.evolve` | 进化材料不足、星+1、等级重置警告 | ~30 | ~30 | P0 | MIX | 结构：满级→同星素材+进化石。 |
| `growth.uncap` | 突破确认、本体重复卡 | ~20 | ~20 | P0 | WRITE | |
| `growth.awaken` | 好感消耗、解锁台词/剧情/外观 | ~30 | ~50 | P0 | MIX | E→S。 |
| `growth.ignition` | 燃起石、红字 ATK/CRT/AGL、技能改写提示 | ~30 | ~80 | P1 | WRITE | `IgnitionBoard`。 |
| `growth.synthesis` | 分解/吞卡、同属 +10% 经验 | ~20 | ~40 | P1 | MIX | |
| `growth.skill_up` | 技能升级（若独立于突破） | ~10 | ~20 | P1 | WRITE | |

---

## 13. 魂卡（Soul Carta 类）

原作 156。切片不做；P1 内容包才像 Destiny Child。产品名自造（建议「契纹 / 残响卡」）。

| id | 内容 | ~n P1 | ~n P2 | P | src | 备注 |
|---|---|---|---|---|---|---|
| `carta.name` | 卡名 | ~40 | ~160 | P1 | WRITE | 禁 `魂之歌牌.csv` 名。 |
| `carta.flavor` | 卡背一句 | ~40 | ~160 | P1 | WRITE | |
| `carta.skill` | 装备效果 | ~40 | ~160 | P1 | MIX | 门：属性/职业/模式。句式可收。 |
| `carta.gate` | 门标签：通用、火、PVP、巨型 Boss… | ~20 | ~30 | P1 | WRITE | 模式门用产品自己的模式名。 |
| `carta.flash` | 普/闪 差异说明 | ~10 | ~10 | P1 | WRITE | |

---

## 14. 人偶 / 协战件

原作 220。P2 或 P1 末。产品名自造。

| id | 内容 | ~n P1 | ~n P2 | P | src | 备注 |
|---|---|---|---|---|---|---|
| `puppet.name` | 人偶名 | ~20 | ~220 | P2 | WRITE | 禁 `人偶.csv`。 |
| `puppet.skill` | TAP/SLIDE/DRIVE/队长 效果 | ~40 | ~400 | P2 | MIX | |
| `puppet.diorama` | 场景盒名 | — | ~70 | P2 | WRITE | SQL `puppet_diorama` 68。 |

---

## 15. 外观 / 皮肤 / 温泉装

SQL skin 1866（含默认待机）。切片 25 人默认一皮，无商店名。

| id | 内容 | ~n P0 | ~n P1 | ~n P2 | P | src | 备注 |
|---|---|---|---|---|---|---|---|
| `skin.name` | 皮肤名 | 25（默认可隐藏） | ~80 | ~400 | P1 | WRITE | `CostumeBoard`。 |
| `skin.unlock` | 获取：好感 S / 活动 / 商店 | ~10 | ~50 | ~200 | P1 | WRITE | |
| `skin.spa` | 温泉装名 | — | — | ~200 | P2 | WRITE | 切片不做温泉。 |
| `home.sticker` | 首页贴纸/摆件 | — | ~20 | ~100 | P2 | WRITE | SQL home sticker。 |

---

## 16. 剧情与对话

收藏型 RPG 的厚度。切片可以只有关前一句；P1 必须有可重看的一章。

| id | 内容 | ~n P0 | ~n P1 | ~n P2 | P | src | 备注 |
|---|---|---|---|---|---|---|---|
| `story.npc` | NPC 名 | ~5 | ~20 | ~50 | P0 | WRITE | 雇主/对手/向导。 |
| `story.location` | 场景名（与关卡可共用） | ~10 | ~40 | ~100 | P0 | WRITE | |
| `story.beat` | 关前/关后短剧 4–12 行 | ~150 | ~2k | ~10k | P0 | WRITE | P0：每关 8 行 ×12 ≈ 100。 |
| `story.chapter` | 章间过场 | ~50 | ~500 | ~2k | P1 | WRITE | |
| `story.choice` | 选项（若有，保持极少） | 0 | ~20 | ~100 | P1 | WRITE | 原作不是分支大作。 |
| `story.affection` | 个人剧（好感 B/A/S） | — | ~1k | ~4k | P1 | WRITE | 每契灵 3 场 × ~15 行。 |
| `story.event` | 活动剧情 | — | ~400（1 个模板活动） | ~10k | P2 | WRITE | Narrative Dungeon 型。 |
| `story.library` | 图文馆重看标题 | ~10 | ~80 | ~200 | P1 | WRITE | Hecate 图书馆对应物。 |
| `ui.dialogue` | 对话框铬：跳过 自动 日志 | ~10 | ~10 | ~10 | P0 | WRITE | |

---

## 17. 语音字幕

有无 Live2D 都要字幕表。切片可静音。

| id | 内容 | ~n P0 | ~n P1 | ~n P2 | P | src | 备注 |
|---|---|---|---|---|---|---|---|
| `vo.home` | 点击首页 2–4 句 | — | ~100 | ~800 | P1 | WRITE | |
| `vo.battle` | 点按/上滑/驱动/退场/胜利 | — | ~200 | ~2k | P1 | WRITE | 每契灵 ~8。 |
| `vo.affection` | 好感阶解锁 | — | ~200 | ~1k | P1 | WRITE | |
| `vo.summon` | 抽卡亮相一句 | — | ~80 | ~200 | P1 | WRITE | |
| `vo.story` | 剧情口播（可与 `story.*` 同 key） | — | 复用 | 复用 | P1 | WRITE | |

---

## 18. 召唤（本地模拟，无支付）

硬禁：真钱、模糊概率。原作 2016 概率风波是反面教材。`PROJECT_STATUS` 切片禁 gacha，但 **P1 内容包必须有** 才能叫抽卡 RPG。

| id | 内容 | ~n P0 | ~n P1 | ~n P2 | P | src | 备注 |
|---|---|---|---|---|---|---|---|
| `summon.ui` | 单抽/十连、跳过、结果 | stub ~10 | ~50 | ~50 | P1 | HAVE+WRITE | `SummonBoard` 已有「抽取」。 |
| `summon.banner` | 卡池名、KV 副标 | — | ~10 | ~80 | P1 | WRITE | |
| `summon.rules` | 保底、Pickup、日抽 | — | ~30 | ~100 | P1 | WRITE | |
| `summon.odds` | 星级概率表（必须可读） | — | ~20 | ~100 | P1 | WRITE | **P1 法律级必做**。 |
| `summon.history` | 记录空态 | — | ~10 | ~10 | P1 | WRITE | |
| `summon.legal` | 免费模拟声明、无充值 | — | ~10 | ~10 | P1 | WRITE | |

---

## 19. 商店

无后端、无 IAP。本地晶/金币/模式币。

| id | 内容 | ~n P0 | ~n P1 | ~n P2 | P | src | 备注 |
|---|---|---|---|---|---|---|---|
| `shop.tab` | 推荐 / 兑换 / 日常 / 活动 | stub ~5 | ~10 | ~20 | P1 | HAVE | `ShopBoard`。 |
| `shop.sku` | 商品名 | — | ~40 | ~200 | P1 | WRITE | |
| `shop.limit` | 每日限购、售罄 | — | ~20 | ~20 | P1 | WRITE | |
| `shop.mode_coin` | 模式商店：地下城币、斗技币、Raid 币 | — | ~20 | ~80 | P2 | WRITE | 名称自造。 |

---

## 20. 邮件、任务、成就、称号

| id | 内容 | ~n P0 | ~n P1 | ~n P2 | P | src | 备注 |
|---|---|---|---|---|---|---|---|
| `mail.ui` | 领取全部、过期 | stub ~10 | ~20 | ~20 | P1 | HAVE | `MailBoard`。 |
| `mail.template` | 补偿、活动开始、维护（本地假邮件） | ~5 | ~30 | ~200 | P1 | WRITE | |
| `mission.daily` | 日常任务名+条件 | — | ~20 | ~40 | P1 | WRITE | |
| `mission.weekly` | 周常 | — | ~15 | ~30 | P1 | WRITE | |
| `mission.story` | 主线任务 | ~12 | ~80 | ~80 | P0 | WRITE | 「通关 1-1」。 |
| `mission.event` | 活动任务 / Pass | — | ~20 | ~200 | P2 | WRITE | |
| `achieve.name` | 成就名 | stub ~10 | ~80 | ~200 | P1 | HAVE+WRITE | `AchievementBoard`。 |
| `achieve.desc` | 条件 | stub | ~80 | ~200 | P1 | WRITE | |
| `title.name` | 玩家称号 | stub ~5 | ~40 | ~100 | P1 | HAVE | `TitleBoard`。 |
| `title.desc` | 获取 | stub | ~40 | ~100 | P1 | WRITE | |

---

## 21. 社交 / 异步切磋

切片 `Friend` `Pvp` 是门板。无后端。P2 才写完整段位文案。

| id | 内容 | ~n P0 | ~n P1 | ~n P2 | P | src | 备注 |
|---|---|---|---|---|---|---|---|
| `friend.ui` | 助战、借用说明 | stub ~10 | ~30 | ~50 | P1 | HAVE | 本地假好友。 |
| `friend.lead` | 助战队长技展示（引用 `skill.*`） | — | 复用 | 复用 | P1 | — | |
| `pvp.ui` | 开门、匹配、防守编队 | stub ~10 | — | ~80 | P2 | HAVE | 切片「切磋」只是开关。 |
| `pvp.rank` | 段位名 | — | — | ~20 | P2 | WRITE | Devil Rumble 对应物。 |
| `pvp.season` | 赛季名、结算邮件 | — | — | ~50 | P2 | WRITE | |
| `rank.board` | 排行榜列名 | — | — | ~20 | P2 | WRITE | Raid / WB / PVP。 |
| `guild.*` | — | 0 | 0 | 0 | 不做 | — | 原作核心不是公会。 |

---

## 22. 模式皮（PVE 循环 → live-ops）

每个模式：入口名、规则短页、奖励预览、空态。系统少、配置多。

| id | 内容 | ~n P0 | ~n P1 | ~n P2 | P | src | 备注 |
|---|---|---|---|---|---|---|---|
| `mode.story` | 主线地图铬 | ~20 | ~40 | ~40 | P0 | HAVE+WRITE | `ChapterMap` `WavePreview`。 |
| `mode.daily` | 星期轮换说明 | stub | ~40 | ~40 | P1 | WRITE | EXP/进化/金。 |
| `mode.underground` | 连续战、HP 继承、当日阵亡 | — | ~40 | ~40 | P1 | MIX | 规则收结构，文案新写。`DeepBoard` 可挂这里。 |
| `mode.explore` | 派遣 1/3/6/12h | — | ~30 | ~30 | P1 | MIX | |
| `mode.narrative` | 活动地图、Boost 契灵、代币 | — | ~80（模板） | ~80/期 | P1 | WRITE | **live-ops 母模板**。 |
| `mode.ragna` | 级 Boss、共享血、协力 | — | — | ~80 | P2 | MIX | 勿用 Ragna 原名。 |
| `mode.worldboss` | 20 人队、镜像币 | — | — | ~80 | P2 | MIX | `VfxWorldBossBar`。 |
| `mode.nebula` | 星云规则 | — | — | ~40 | P2 | WRITE | `NebulaBoard`。 |
| `mode.rebirth` | 高难外观本 | — | — | ~40 | P2 | WRITE | |
| `mode.reincarnation` | 重复卡获取屋 | — | — | ~30 | P2 | WRITE | Uncap 漏斗。 |
| `mode.spa` | 温泉互动短句 | — | — | ~200 | P2 | WRITE | 收藏反馈，非战斗。`RestBoard` 是切片替代。 |
| `mode.rest` | 休息回复 | ~10 | ~10 | ~10 | P0 | HAVE | 切片体力阀。 |
| `mode.food` | 用餐 | ~10 | ~20 | ~20 | P0 | HAVE | |

---

## 23. 帮助、图鉴、入门

| id | 内容 | ~n P0 | ~n P1 | P | src | 备注 |
|---|---|---|---|---|---|---|
| `help.tutorial` | 强制引导 3–8 步 | ~20 | ~80 | P0 | HAVE+WRITE | `TutorialBoard` 三页：点按/上滑/驱动。补 Fever、编队、克制。 |
| `help.coach` | 战场一次性教练气泡 | ~20 | ~40 | P0 | WRITE | 「上滑头像」。 |
| `help.codex` | 战斗百科：克制、Fever、覆盖规则 | ~30 | ~80 | P0 | WRITE | 切片能打开即可。 |
| `help.faq` | 养成 FAQ | — | ~40 | P1 | WRITE | |
| `help.tip` | 加载小知识 | ~10 | ~80 | P0 | WRITE | |
| `help.input` | 键位：点按、上滑、空格开战 | ~10 | ~10 | P0 | WRITE | 桌面窗需要。 |

---

## 24. 系统、法律、错误

单机也要。

| id | 内容 | ~n P0 | ~n P1 | ~n P2 | P | src | 备注 |
|---|---|---|---|---|---|---|---|
| `sys.error` | 存档损坏、磁盘、缺图 | ~30 | ~50 | ~80 | P0 | WRITE | |
| `sys.save` | 新建/覆盖/导入导出 | ~20 | ~20 | ~20 | P0 | WRITE | |
| `legal.credits` | 引擎、字体、原创声明 | ~20 | ~40 | ~40 | P0 | WRITE | 点名 Unity，**不点**原作 IP。 |
| `legal.ip` | 「非官方、无原作资产」 | ~10 | ~10 | ~10 | P0 | WRITE | 设置页常驻。 |
| `legal.privacy` | 单机隐私（无账号） | ~10 | ~10 | ~10 | P0 | WRITE | |
| `legal.age` | 内容分级自述 | ~10 | ~10 | ~10 | P0 | WRITE | |
| `sys.patch` | 更新说明 | — | ~20/次 | ~20/次 | P2 | WRITE | |
| `sys.push` | 体力满、活动（若有推送） | — | — | ~20 | P2 | WRITE | 默认可不做。 |
| `sys.debug` | 调试层英文（可不本地化） | ~20 | ~20 | ~20 | — | HAVE | 不进发行字符串包。 |

---

## 25. 格式与插值

| id | 内容 | ~n | P | src | 备注 |
|---|---|---|---|---|---|
| `fmt.number` | `1,234`、`12.3万`、`1.2M` | ~10 | P0 | WRITE | 伤害弹字。 |
| `fmt.duration` | `{m}:{s}`、`{h}小时` | ~10 | P0 | WRITE | |
| `fmt.stat_delta` | `+12%`、`HP +340` | ~10 | P0 | WRITE | |
| `fmt.skill` | 技能模板连接词 | ~20 | P0 | WRITE | 「几率」「持续」「优先」。 |

---

## 26. 运营期专用（P2 每次活动克隆）

不要每次活动新程序。复制 `mode.narrative` + 下列增量。

| id | 内容 | 每期 ~n | P | src | 备注 |
|---|---|---|---|---|---|
| `ops.event_kv` | 活动标题、倒计时副标 | ~10 | P2 | WRITE | |
| `ops.boost` | Boost 契灵名单说明 | ~10 | P2 | WRITE | |
| `ops.pass` | 任务通行证档位名 | ~20 | P2 | WRITE | |
| `ops.login` | 七日登录格文案 | ~10 | P2 | WRITE | |
| `ops.shop` | 活动店 SKU | ~20 | P2 | WRITE | |
| `ops.mail` | 开始/结束/补偿 | ~5 | P2 | WRITE | |
| `ops.collab` | 联动专用（默认不做） | 0 | 不做 | — | 无授权不写联动名。 |

---

## 27. 量级汇总

按 **唯一 key** 估（中文一语言）。英文本来是 ×1，不是新创意。

| 层 | 表数 | 词条量级 | 玩家能看见什么 |
|---|---:|---|---|
| **P0 ship** | ~55 张有行 | **~2k–3.5k** | 25 契灵可编队；技能说明；12+12 关名；战斗 HUD；入门；设置/法律；核心道具与养成确认。 |
| **P1 content** | +~25 张 | **~12k–18k** | 一章完整剧情+好感短剧；日常/地下/派遣；本地召唤+公示概率；商店邮件成就；魂卡第一批；图鉴长传。 |
| **P2 live-ops** | 上列克隆 | **累计 ~40k–70k** | 活动剧、Raid/WB/星云生成名、皮肤/人偶、语音字幕。仍低于原作中文 locale 81k（他们有 561 人+七年活动）。 |

**P0 内部分布（约）**

| 域 | keys |
|---|---:|
| UI 铬 + 枚举 + 格式 + 错误法律 | 500 |
| 战斗 HUD/结算/横幅 | 150 |
| 契灵名/称号/tagline/获取 | 120 |
| 技能名+描述+短句（含敌） | 500 |
| 效果名录 | 80 |
| 关卡/章/条件/Boss 句 | 80 |
| 道具货币装备养成 | 250 |
| 入门/百科/任务 | 120 |
| 剧情短打 | 150 |
| 余量/空态/确认 | 200 |
| **合计** | **~2.2k** |

切片 **HAVE** 大约：25 人名 + ~150 技能短名 + 12 关名 + 7 敌名 + ~80 硬编码按钮。**缺口主要是 `skill.desc`、`fx.*` 玩家句、`stage.blurb`、`item.desc`、`help.codex`、`legal.*`。**

---

## 28. 和客户端屏的对照

| `ScreenId` | 主要吃哪些表 |
|---|---|
| Boot | `ui.boot` `legal.ip` |
| Home | `ui.home` `ui.nav` `char.name` `char.tagline` `item.currency` |
| Characters / Inspect | `char.*` `skill.*` `fx.*` `enum.*` `growth.*` `gear.*` `skill.reserve` |
| Team | `ui.filter` `enum.*` `char.name` `skill.leader_*` |
| Stage | `mode.story` `stage.*` `enemy.name` `battle.*`（预览） |
| Battle | `battle.*` `fx.hud` `enum.drive_timing` |
| Result | `battle.result` `battle.loot` `item.*` |
| Archive / Library | `char.profile` `story.library` `help.codex` |
| Deep | `mode.underground` 或深层地图 |
| Settings | `ui.settings` `legal.*` `sys.save` |
| Summon | `summon.*` |
| Daily | `mode.daily` `area.daily` |
| Shop | `shop.*` |
| Mail | `mail.*` |
| Achieve / Title | `achieve.*` `title.*` |
| Friend | `friend.*` |
| Rest / Food | `mode.rest` `mode.food` `item.consumable` |
| Pvp | `pvp.*` |
| Tutorial | `help.tutorial` |
| Costume | `skin.*` |

---

## 29. 建议的文件落盘（给后续 worker）

每个 `id` 一张 UTF-8 CSV 或 JSON：

```
key,zh-Hans,en,notes,priority
```

技能与道具用宽表（程序列 + 文本列分离）：

```
id,slot,name_key,desc_key
```

`desc_key` 进字符串包；系数留 `catalog.json`。**禁止**把伤害数字只写在散文里。

写作顺序（P0）：

1. `enum.*` + `ui.common` + `battle.*`（屏幕能读）
2. `skill.template` + `fx.name/desc`（战斗能懂）
3. `skill.desc` × 25 契灵（切片最大内容洞）
4. `stage.blurb` + `story.beat` 一章
5. `item.*` + `growth.*` 确认框
6. `help.tutorial` 扩到 Fever/克制
7. `legal.*`

P1 再动 `summon.odds`、`char.profile`、`story.affection`、`carta.*`。

---

## 30. 明确不做进包的表

| 原作东西 | 处理 |
|---|---|
| 561 天子名与技能 OCR | 研究用，不进 `game_text` 发行 |
| locale.pck 中/韩原文 | 只计数 |
| 真钱 IAP 文案 | 不做 |
| 公会 | 不做 |
| 联动角色名 | 不做 |
| 调试 `sys.debug` | 可不进 loc 包 |

本文件是合并用主索引。增表时先改这里的 `id`，再写细表。
