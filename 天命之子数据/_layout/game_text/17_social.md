# 17 社交文案 — SOCIAL / GUILD / MAIL / ACHIEVEMENT / FRIEND

完整 live-service 抽卡仍要一套**社交与进度字表**。Wiki / 图鉴只覆盖角色与掉落；离线包 SQL 几乎没有这些主数据。本文件先记原作运营期证据和档案有无，再列完整游戏仍要的字表。即使行数为 0，表名也要留下。

产品名沿用客户端：`契灵回响`。本文件后半的 `zh` 是洁净室自拟，禁止原作角色名、原作 Logo、原作任务句。原作专有名只出现在「档案 / 证据」段。

键名：`域.面.槽`，全小写点分。占位：`{0}` `{1}` `{n}` `{name}` `{uid}` `{exp}` `{date}`。

锁（与 `docs/reference/offline-pack-1.1/04-mvp-gap-and-bans.md` 对齐）：发行包 **无后端、无联网好友、无公会、无邮件当商店投递、无排行榜**。字表仍要写，客户端用本机 stub / 「尚未开放」，不要把空表当成「系统不存在」。

---

## 结论（先看这个）

原作 Destiny Child **不是公会 MMO**。全球/日/韩运营期的社交核是：

**好友借用（分享天子）→ 友情点扭蛋 → Raid 协力 → 异步排名**

底栏第六格是 **好友**，不是公会。公会战/Clan 在三站攻略树、离线包 54 表、纪念版截图里都 **找不到主功能**。中文攻略偶尔把 5 人 Raid 大厅叫「公会 Raid」，那是误称。

运营期 Home 另有：邮箱、成就页签、图鉴（右栏）、丽莎每日、Devil Pass（通行证）、Mission Pass（限时任务）。纪念版把 LiveOps 剥光，只留图鑑浏览器。

| 块 | 原作运营期 | 本档案 | 完整 gacha 仍要？ |
|---|---|---|---|
| 好友 / 社区 | 有。底栏「好友」、Community 列表、发友情点、邮件收点、分享天子、好友扭蛋 | **半有**：SQL 只有列，0 行；wiki/截图有壳 | 要（本机可 stub） |
| 邮件 | 有。右栏信封、奖励投递、商店→邮件、探索完成进邮箱 | **半有**：`user_mail` 空表；无邮件正文主数据 | 要（投递总线） |
| 成就 | 有。底栏月桂「成就」+ 红点 | **无表**。只有 UI 壳截图 | 要 |
| 每日任务 | 有。丽莎作业，完成 8 项额外水晶 | **无表**。wiki 流程文有 | 要 |
| 图鉴奖励 | 有。收集分里程碑 | **半有**：`collection_score` / `_reward` 列，0 行；图鉴名册有，奖励阶梯无 | 要 |
| 通行证 | 有。Devil Pass（付费轨）+ Mission Pass（限时任务轨） | **无表**。Home 截图 + wiki 21773 | 要（完整 live-ops） |
| 公会 | **不是核心功能** | **无** | 完整 gacha 通常要；本产品 **BAN 联网公会**，字表可留 stub |

档案状态图例：

- **有行**：主数据或 wiki 正文可引用
- **空壳**：`CREATE TABLE` / 列存在，INSERT = 0
- **仅截图**：UI 标签能读，无字表
- **无**：表、列、wiki 专页、纪念版屏都没有
- **禁拆**：`locale.pck` / jar 里可能有原文，**不要反编译**

---

## 0. 搜过的路径

| 关键词 | 命中 | 未命中 |
|---|---|---|
| 公会 | 研究报明确写「MVP 不要凭空加公会」；`04-mvp-gap-and-bans` BAN guild；动效页把 Raid 大厅误写成「5 人公会 Raid」 | wiki 树无「公会」专页；SQL 无 `guild` / `clan` |
| 好友 | Home 底栏；Community；`friend_point` `friend_max` `share_char`；韩服「每日任务」82285「发送友情点数」；歌牌名「好友三重奏」（装备，不是系统） | 无 `user_friend` 表 |
| 邮件 | `user_mail`；changelog 血石商店→邮件；探索满级进邮件 | 0 封样信；无 `mail_idx` 主表 |
| 成就 | 运营期底栏第 4 格「成就」 | 无 achievement 表；纪念版无此页签 |
| 图鉴奖励 | `do_login_user.collection_score` / `collection_score_reward`；纪念版/wiki 图鑑是名册 | 无分数阶梯、无「领取图鉴奖励」文案 |
| 通行证 | 日服 Home：`DEVIL PASS 30`、`MISSION PASS`；wiki 21773「限时任务 = MISSION PASS」；点火介绍把 Devil Pass 当核心素材来源；wiki 标注 DP=通行证 1–2 周 | SQL 无 pass 表 |

主要证据文件：

- 截图：`docs/reference/gamekee/_combined/battle_refs/jp_new_02.png`（运营期 Home 全栏）、`crop_livehome.png`（中文标注 DP/MP）、`sys_01.png`（系统介绍拼图：Community / 邮箱 / 成就 / 图鉴）
- 纪念版：`docs/reference/mobile-archive/screenshots/001_home.png` 起，底栏换成 首页/Child/温泉/图鑑/图书馆/Eve，**无**好友/成就/抽卡/邮箱
- wiki：国服 `77671` 游戏系统介绍、`21773` 限时任务、`71966` 全资源；韩服 `82285` 每日任务
- SQL：`_sandbox/offline-pack-1.1/schema-only.sql`（54 表）
- 研究：`docs/deep-research-report.md` §社交；`01-schema-map.md` §1.2 / §4

---

## 1. 运营期 Home 壳（字表入口）

`jp_new_02.png`（日服运营期）底栏六格，从左到右：

| 格 | 日/中标注 | 原作功能 | 纪念版 |
|---|---|---|---|
| 1 | 主界面 | Home | 首頁 |
| 2 | 天子列表 | 编队 / Child | Child |
| 3 | 背包 | 物品 | **无**（装备进详情） |
| 4 | 成就 | 成就 / 任务总簿 | **无** |
| 5 | 抽卡 | 召唤 | **无** |
| 6 | 好友 | Community | **无** |

右栏（上→下）：设置、**邮箱**（红点）、**图鉴**、ホーム設定、鑑賞モード。

左栏：Event、Shop、**每日任务**（丽莎头像 + 红点）、特定活动。

顶栏货币：体力 / 钻石 / 金币 / 玛瑙 / 红石。旁：每日登录、Devil Pass、Mission Pass。

`crop_livehome.png` 中文框：

- **DP**：通行证活动，约 1–2 周，白嫖轨 + 付费解锁
- **MP**：活动任务
- **设定**：个人 ID（加好友用）
- 商店每日免费礼包

这些是字表必须挂得上的 **入口键**，不是玩法本身。

| id | zh | 档案 |
|---|---|---|
| nav.home | 首页 | 仅截图 |
| nav.roster | 契灵 | 仅截图（原作 Child） |
| nav.bag | 背包 | 仅截图 |
| nav.achieve | 成就 | 仅截图 |
| nav.summon | 召唤 | 仅截图 |
| nav.friends | 好友 | 仅截图 |
| nav.mail | 邮箱 | 仅截图 |
| nav.catalog | 图鉴 | 纪念版有图鑑页，无奖励 |
| nav.daily | 每日 | 仅截图 + wiki 82285 |
| nav.pass | 通行证 | 仅截图 |
| nav.mission_pass | 限时任务 | wiki 21773 |
| nav.shop | 商店 | 仅截图；SQL 无 SKU |
| nav.event | 活动 | 仅截图 |

---

## 2. 好友 / 社区

### 2.1 原作语法

- 屏名 **Community**（`sys_01.png` 第 5 块）。
- 列表：头像、昵称、等级、分享中的天子、最后上线。
- 每日第一件事：给好友发 **友情点**；对方在 **邮件** 里收到（wiki 82285）。
- 友情点进召唤里的友情扭蛋（水晶 / 体力 / 玛瑙）。
- 加友：个人 ID；按最后上线筛「邀请」。
- 队伍空位可上好友分享天子，带额外队长技（研究报）。Ragna 可协力打同一 Boss。
- SQL 列：`do_login.friend_point`；`do_login_user.friend_max`、`share_char`。**没有**好友关系表。
- Resonance：**BAN** 联网好友、`share_char` 助战。本机可做「NPC 助战」或整页「尚未开放」。

### 2.2 完整 gacha 仍要的字表

即使 0 行，也要这些键。状态：**无主数据**；列空壳；截图/wiki 有壳。

#### 列表与空态

| id | zh | notes |
|---|---|---|
| friend.title | 好友 | 底栏 / 页标题 |
| friend.tab.list | 列表 | |
| friend.tab.request | 申请 | |
| friend.tab.search | 查找 | |
| friend.tab.gift | 赠礼 | 通用 gacha；原作主路径是友情点不是道具礼 |
| friend.count | {n} / {m} | 当前 / 上限 |
| friend.empty | 还没有好友。 | |
| friend.empty.hint | 用 ID 查找，或查看推荐。 | |
| friend.last_login | 最近上线 {0} | |
| friend.last_login.now | 在线 | |
| friend.offline_days | {n} 天前 | |
| friend.share_unit | 助战：{name} | 原作分享天子 |
| friend.power | 战力 {0} | |
| friend.level | Lv.{0} | |

#### 申请 / 查找 / 上限

| id | zh | notes |
|---|---|---|
| friend.add | 添加 | |
| friend.add.ok | 已发送申请。 | |
| friend.add.dup | 已经是好友。 | |
| friend.add.self | 不能添加自己。 | |
| friend.add.full.self | 好友已满。 | `friend_max` |
| friend.add.full.other | 对方好友已满。 | |
| friend.add.blocked | 无法添加该玩家。 | |
| friend.add.not_found | 找不到该 ID。 | |
| friend.search.placeholder | 输入玩家 ID | 设定里的个人 ID |
| friend.search.recommend | 推荐 | |
| friend.accept | 接受 | |
| friend.reject | 拒绝 | |
| friend.accept.all | 全部接受 | |
| friend.request.empty | 没有待处理的申请。 | |
| friend.request.inbox | 收到的申请 | |
| friend.request.outbox | 已发送 | |
| friend.delete | 删除好友 | |
| friend.delete.confirm | 删除 {name}？删除后需重新申请。 | |
| friend.block | 屏蔽 | |
| friend.unblock | 解除屏蔽 | |
| friend.visit | 查看主页 | 原作可看大厅布置 |

#### 友情点 / 助战

| id | zh | notes |
|---|---|---|
| friend.point.name | 友情点 | 钱包 `friend_point` |
| friend.point.send | 赠送友情点 | |
| friend.point.send.all | 一键赠送 | |
| friend.point.sent | 已赠送 | 当日已送 |
| friend.point.recv_mail | 友情点已发到邮箱。 | wiki：收点走邮件 |
| friend.point.cap | 今日赠送已达上限。 | |
| friend.point.not_enough | 友情点不足。 | |
| friend.gacha.title | 友情召唤 | 友情点扭蛋 |
| friend.assist.pick | 选择助战 | |
| friend.assist.none | 没有可用的助战。 | |
| friend.assist.used | 今日已用过该助战。 | |
| friend.assist.lead | 助战队长技生效中 | |
| friend.share.set | 设置助战契灵 | `share_char` |
| friend.share.clear | 取消助战 | |

#### 邀请码（通用 gacha；原作有「邀请好友」）

| id | zh | notes |
|---|---|---|
| friend.invite.title | 邀请 | |
| friend.invite.code | 我的邀请码 {0} | |
| friend.invite.input | 输入邀请码 | |
| friend.invite.ok | 邀请已绑定。 | |
| friend.invite.used | 已经用过邀请码。 | |
| friend.invite.invalid | 邀请码无效。 | |

---

## 3. 邮件

### 3.1 原作语法

`user_mail` 空壳列：`mail_idx` `title` `text` `reason` `reward_idx` `reward_type` `reward_count` `reward_add_info` `is_receive_reward` `showTS` `expireTS` `receiveTS`。

这是 **投递总线**，不是玩法循环。1.1 私服 changelog：血石商店无限购买 → **领取在邮件**（BAN，不要当产品）。探索 4 星挂满 30 级 → 邮件 30 水晶/人（wiki 82285）。友情点也进邮箱。

无 `mail` 主表、无样信正文。`locale.pck` 可能有系统信模板，禁拆。

Resonance：奖励直接进背包；邮件页可做本机「系统通知」stub，禁止商店→邮件。

### 3.2 字表

| id | zh | notes |
|---|---|---|
| mail.title | 邮箱 | |
| mail.tab.system | 系统 | |
| mail.tab.social | 社交 | 好友赠礼 / 友情点 |
| mail.tab.event | 活动 | |
| mail.empty | 没有邮件。 | |
| mail.unread | {n} 封未读 | 右栏红点 |
| mail.claim | 领取 | |
| mail.claim.all | 全部领取 | |
| mail.claimed | 已领取 | |
| mail.delete | 删除 | |
| mail.delete.read | 删除已读 | |
| mail.expire | {date} 过期 | |
| mail.expired | 已过期 | |
| mail.expired.body | 过期邮件无法领取。 | |
| mail.full | 邮箱已满。请先领取或删除。 | |
| mail.full.reward | 背包已满，奖励仍留在邮件。 | |
| mail.sender.system | 系统 | |
| mail.sender.friend | {name} | |
| mail.no_attach | 无附件 | |
| mail.attach | 附件 ×{n} | |

#### `reason` 枚举（主数据即使空也要键）

| id | zh | 原作对照 |
|---|---|---|
| mail.reason.system | 系统补偿 | 维护 / GM |
| mail.reason.event | 活动奖励 | |
| mail.reason.daily | 每日奖励 | |
| mail.reason.achieve | 成就奖励 | |
| mail.reason.catalog | 图鉴奖励 | `collection_score_reward` |
| mail.reason.pass | 通行证奖励 | Devil Pass 溢出 |
| mail.reason.friend_point | 友情点 | wiki 82285 |
| mail.reason.gift | 好友赠礼 | |
| mail.reason.explore | 探索完成 | 探索满级水晶 |
| mail.reason.shop | 商店发货 | **BAN** 血石店→邮件 |
| mail.reason.levelup | 成长礼包 | `free/paid_levelup_package_reward` |
| mail.reason.coupon | 兑换码 | 韩服 wiki 584472 |

系统信标题/正文模板（无档案行，完整游戏仍要）：

| id | zh |
|---|---|
| mail.tpl.welcome.title | 欢迎 |
| mail.tpl.welcome.body | 这是你的第一封信。附件请记得领取。 |
| mail.tpl.maint.title | 维护补偿 |
| mail.tpl.maint.body | 维护已结束。附件为本次补偿。 |
| mail.tpl.expire_warn.title | 即将过期 |
| mail.tpl.expire_warn.body | 有附件将于 {date} 过期。 |

---

## 4. 成就 / 每日 / 限时任务

### 4.1 原作语法

三层不要混：

| 层 | 入口 | 内容 | 档案 |
|---|---|---|---|
| **成就** | 底栏月桂 | 长期图鉴式成就总簿（截图有红点 2） | 仅截图，无表 |
| **丽莎每日** | 左栏丽莎头 | 每日作业：夜世界材料/金币/进化、探索、PVP 3 场、升技能、拆玛瑙、强化歌牌、10 次剧情、地铁、花水晶。完成 **8 项** 额外约 400 水晶，全做约 1000 | wiki 82285 流程；无 mission 主表 |
| **Mission Pass** | 蓝图标 / 限时任务 | 活动期 Child 主题任务：强化指定角色、收集、耗体、打副本/WB/Raid；有免费链和氪金链；结束维护后移除，未领作废 | wiki 21773 |

原作成就页内部标签未进档案（没有点进成就的截图）。完整 gacha 仍按通用分册写。

### 4.2 成就字表

| id | zh | notes |
|---|---|---|
| ach.title | 成就 | |
| ach.tab.all | 全部 | |
| ach.tab.story | 旅程 | |
| ach.tab.collect | 收集 | 与图鉴奖励可交叉 |
| ach.tab.battle | 战斗 | |
| ach.tab.social | 社交 | |
| ach.tab.growth | 培养 | |
| ach.progress | {n} / {m} | |
| ach.claim | 领取 | |
| ach.claim.all | 一键领取 | |
| ach.claimed | 已完成 | |
| ach.locked | 未解锁 | |
| ach.empty | 这一类还没有成就。 | |
| ach.reward | 奖励 | |
| ach.point | 成就点 {0} | 通用；原作是否用点数 **无表** |
| ach.goto | 前往 | 跳对应系统 |

成就条目模板（主数据空时 UI 仍要）：

| id | zh |
|---|---|
| ach.row.title | {0} |
| ach.row.desc | {0} |
| ach.row.hidden | ??? |

建议本机最小集（洁净室，不抄原作任务）：首次编队、首次战斗、首次强化、收集 N 名契灵、通关第 1 章。条目正文进独立 `achievement.json`，不要写死在代码。

### 4.3 每日 / 周常

| id | zh | notes |
|---|---|---|
| daily.title | 每日 | 原作「丽莎的作业」 |
| daily.reset | 每日 {0} 重置 | 韩服 wiki：北京时间凌晨 3 点 |
| daily.progress | 完成 {n} / {m} | 原作 8 项门槛 |
| daily.bonus | 额外奖励 | 完成 N 项箱子 |
| daily.claim | 领取 | |
| daily.done | 今日已完成 | |
| daily.goto | 前往 | |
| weekly.title | 每周 | 通用 gacha；原作是否有独立周常 **无表** |
| weekly.reset | 每周 {0} 重置 | |

### 4.4 限时任务 / Mission Pass

| id | zh | notes |
|---|---|---|
| mpass.title | 限时任务 | wiki 官方译 MISSION PASS |
| mpass.theme | {0} | 当期 Child 主题名 — 不要填原作名 |
| mpass.until | 剩余 {0} | |
| mpass.locked | 完成前置后解锁 | 21773：部分要先解锁后段 |
| mpass.unlock | 解锁 | |
| mpass.free | 免费 | 强化链分免费/付费 |
| mpass.paid | 进阶 | 不要写「氪金」进按钮 |
| mpass.ended | 本期已结束。未领取的奖励无法补领。 | 21773 |
| mpass.removed | 将在维护后从清单移除。 | |

---

## 5. 图鉴奖励

### 5.1 原作语法

图鉴本身在纪念版/wiki **很满**：天子 561、魂之歌牌、人偶、造型。那是 **名册**，不是奖励阶梯。

奖励线索只有登录列：

- `collection_score`
- `collection_score_reward`

无阶梯表、无「领取」文案、无每格分数。运营期右栏「图鉴」与纪念版「圖鑑」同入口，纪念版看不到分数奖励。

完整 gacha 仍要：首次获得奖励、收集分、里程碑箱、图鉴完成度。

### 5.2 字表

| id | zh | notes |
|---|---|---|
| cat.title | 图鉴 | |
| cat.tab.unit | 契灵 | 原作 Child |
| cat.tab.carta | 魂卡 | 原作魂之歌牌 |
| cat.tab.puppet | 人偶 | |
| cat.tab.skin | 造型 | |
| cat.score | 收集分 {0} | `collection_score` |
| cat.complete | {n} / {m} | |
| cat.percent | {0}% | |
| cat.reward.title | 收集奖励 | |
| cat.reward.next | 再 {n} 分可领 | |
| cat.reward.claim | 领取 | `collection_score_reward` 游标 |
| cat.reward.claimed | 已领取 | |
| cat.reward.locked | 分数不足 | |
| cat.first_get | 首次收录 | 获得新卡弹窗 |
| cat.not_owned | 未获得 | 剪影 |
| cat.owned | 已收录 | |
| cat.new | NEW | |

名册正文（名字、技能、故事）不在本文件，见 `04_puppets.md` / 汇总表。本文件只管 **奖励与空态**。

---

## 6. 通行证（Devil Pass / 通用 Battle Pass）

### 6.1 原作语法

日服 Home 右上 **DEVIL PASS 30**（当前等级）+ **MISSION PASS**。中文攻略把 DP 叫通行证：约 1–2 周一期，免费轨 + 付费解锁。点火核心材料来源包括 Devil Pass（`wiki_ignition_intro.txt`）。

`do_login_user` 另有 `free_levelup_package_reward` / `paid_levelup_package_reward`（等级成长礼包，双轨，**不是** 通行证但同形）。

SQL **无** pass 季、轨、等级、奖励行。

Resonance：完整 live-ops 才做；MVP 不要做付费轨。字表可留，入口显示「尚未开放」。

### 6.2 字表

| id | zh | notes |
|---|---|---|
| pass.title | 通行证 | 原作 Devil Pass |
| pass.season | 第 {n} 期 | |
| pass.until | {date} 结束 | |
| pass.lv | Lv.{0} | Home 上的 30 |
| pass.exp | {n} / {m} | 本级经验 |
| pass.track.free | 免费 | |
| pass.track.paid | 进阶 | 付费解锁轨 |
| pass.unlock | 解锁进阶奖励 | |
| pass.unlocked | 进阶已解锁 | |
| pass.claim | 领取 | |
| pass.claim.all | 领取全部 | |
| pass.locked | 达到该级后可领 | |
| pass.buy_level | 购买等级 | 通用 gacha |
| pass.buy_level.confirm | 将通行证升到 {0} 级？ | |
| pass.max | 已满级 | |
| pass.ended | 本期已结束 | |
| pass.preview | 下期预告 | |
| pass.xp_source | 完成每日与限时任务可获得通行证经验。 | |

成长礼包（SQL 列在，0 行）：

| id | zh |
|---|---|
| lvpack.title | 成长礼包 |
| lvpack.free | 免费 |
| lvpack.paid | 进阶 |
| lvpack.claim | 领取等级 {0} 奖励 |

---

## 7. 公会（完整 gacha 有；原作几乎没有）

研究报：全球服核心不是 Clan。离线包 54 表无 guild。wiki 无公会专页。BAN 联网公会。

完整 live-service 抽卡仍通常要下列字表。本产品全部标 **无档案**，客户端用 `sys.coming`。

| id | zh | 档案 |
|---|---|---|
| guild.title | 公会 | 无 |
| guild.create | 创建公会 | 无 |
| guild.join | 加入 | 无 |
| guild.leave | 退出 | 无 |
| guild.leave.confirm | 退出公会？今日可能无法再加入其他公会。 | 无 |
| guild.kick | 请离 | 无 |
| guild.disband | 解散 | 无 |
| guild.name | 公会名 | 无 |
| guild.notice | 公告 | 无 |
| guild.board | 留言板 | 无 |
| guild.id | 公会 ID {0} | 无 |
| guild.lv | 公会等级 {0} | 无 |
| guild.exp | {n} / {m} | 无 |
| guild.members | {n} / {m} 人 | 无 |
| guild.role.leader | 会长 | 无 |
| guild.role.officer | 干部 | 无 |
| guild.role.member | 成员 | 无 |
| guild.apply | 申请加入 | 无 |
| guild.apply.ok | 已发送申请。 | 无 |
| guild.full | 公会人数已满。 | 无 |
| guild.not_found | 找不到该公会。 | 无 |
| guild.need_guild | 需要加入公会。 | 无 |
| guild.donate | 捐献 | 无 |
| guild.shop | 公会商店 | 无 |
| guild.raid | 公会讨伐 | 无；勿把原作 Ragna 写成公会战 |
| guild.checkin | 公会签到 | 无 |
| guild.empty | 你还没有公会。 | 无 |

原作近义、不要叫公会：

- Ragna:Break = 5 人 Raid 协力 + 共享 Boss HP
- Devil Rumble / Endless Duel = 异步 PvP
- World Boss = 最多 20 人桌 + 排名

这些字表在战斗/模式文件，不在本社交文件。

---

## 8. 资料片 / 个人主页 / 聊天（社交周边）

运营期设定含个人 ID。Community 能看他人。完整 gacha 还常有世界聊天；原作是否有世界频道 **无专页**，不臆造原文。

| id | zh | 档案 |
|---|---|---|
| profile.title | 主页 | 仅设定「个人 ID」 |
| profile.nickname | 昵称 | SQL `nickname` 空壳 |
| profile.uid | ID {uid} | `shortNfguid` 空壳 |
| profile.copy_id | 复制 ID | |
| profile.rename | 修改昵称 | |
| profile.rename.bad | 无法使用该昵称。 | |
| profile.rename.cd | 改名冷却中。 | |
| profile.title_badge | 称号 | 通用；原作有无 **无表** |
| profile.intro | 签名 | 通用 |
| profile.like | 点赞 | 通用 |
| chat.title | 聊天 | 无专页 |
| chat.tab.world | 世界 | 无 |
| chat.tab.guild | 公会 | 无 |
| chat.tab.private | 私聊 | 无 |
| chat.placeholder | 输入内容 | |
| chat.too_fast | 发送过快。 | |
| chat.banned | 无法发送。 | |
| chat.report | 举报 | |
| present.title | 礼物 | 温泉有 present 物品列，属养成不是好友礼 |
| coupon.title | 兑换码 | wiki 韩服 584472 有专页，无码表 |
| coupon.ok | 奖励已发送到邮箱。 | 通用投递 |
| coupon.bad | 兑换码无效。 | |
| coupon.used | 已经使用过。 | |
| coupon.expired | 兑换码已过期。 | |

登录奖励（Home「每日登录」图标，无表）：

| id | zh |
|---|---|
| login_bonus.title | 每日登录 |
| login_bonus.day | 第 {n} 天 |
| login_bonus.claim | 领取 |
| login_bonus.claimed | 今日已领 |

---

## 9. 档案对照总表（字表 × 有无）

| 字表（建议文件） | 用途 | SQL | wiki | 运营截图 | 纪念版 | 原文可进产品？ |
|---|---|---|---|---|---|---|
| `ui_nav_social` | 底栏/右栏入口 | 无 | 间接 | **有** jp_new_02 | 无对应六格 | 否，自拟 |
| `friend_ui` | 列表/申请/查找 | 列空壳 | 82285 流程 | Community 块 | 无 | 否 |
| `friend_point` | 赠点/友情召唤 | `friend_point` 空 | 82285 | 无独立商店图 | 无 | 否 |
| `friend_assist` | 助战/分享 | `share_char` 空 | 研究报 | 无 | 无 | 否；BAN 联网 |
| `mail_ui` | 邮箱壳 | `user_mail` 空 | changelog | 信封图标 | 无 | 否 |
| `mail_tpl` | 系统信模板 | 无 | 无 | 无 | 无 | 自拟 |
| `achievement` | 成就条目+UI | 无 | 无 | 底栏月桂 | 无 | 自拟条目 |
| `daily_mission` | 每日 | 无 | **82285** | 丽莎头 | 无 | 不抄原作任务句 |
| `mission_pass` | 限时任务 | 无 | **21773** | MP 图标 | 无 | 不抄当期 Child 名 |
| `catalog_reward` | 图鉴分/箱 | 两列空壳 | 图鉴名册有、奖励无 | 图鉴按钮 | 名册有、奖励无 | 自拟阶梯 |
| `battle_pass` | 通行证双轨 | 无 | 点火文提及 | **Devil Pass 30** | 无 | 自拟 |
| `levelup_pack` | 成长礼包双轨 | 两列空壳 | 无 | 无 | 无 | 自拟 |
| `guild_*` | 公会全家桶 | **无** | **无** | **无** | 无 | stub + BAN |
| `profile` | 昵称/ID | `nickname` 空 | 设定说明 | 有 | 设定有 | 自拟 |
| `coupon` | 兑换码 | 无 | 584472 | 无 | 无 | 自拟；无码库 |
| `login_bonus` | 七日登录 | 无 | 无 | 每日登录图标 | 无 | 自拟 |
| `locale.pck` 社交句 | 韩/日/英原文 | 未解包 | — | — | — | **禁拆、禁进包** |

名册类（角色/歌牌/人偶/造型）**有**，见汇总表与 `04_puppets.md`。它们不是本文件的社交字表。

---

## 10. 客户端缺口（契灵回响）

来源对照：`12_system.md`（已有确认/取消/尚未开放）、`18_legal.md`（访客无账号服）。

| 已有 | 缺 |
|---|---|
| `sys.coming` 尚未开放 | 好友/邮件/成就/通行证整页 |
| 设定清档 | 个人 ID、复制 ID、改名 |
| 本机存档 | 邮件总线、成就进度、每日重置时钟 |
| 图鉴名册可挂汇总表 | 收集分与里程碑箱 |
| BAN 联网 | 仍要空态文案，避免入口点进去崩溃 |

建议落地顺序（不改本文件范围外的系统）：

1. 导航键 + `sys.coming` 接上入口（邮箱/好友/成就）
2. 本机成就 JSON（少量洁净室条目）+ 领取
3. 本机每日（不抄丽莎列表）
4. 图鉴收集分（只对 25 名自造契灵）
5. 邮件只收系统补偿/成就溢出，**禁止**商店发货
6. 通行证 / 公会 / 联网好友：保持尚未开放

---

## 11. 不要做的事

- 不要把 Raid 大厅写成公会，不要为「看起来像手游」补 Clan。
- 不要从 `locale.pck` / jar 抽原作邮件、每日、通行证原文。
- 不要把歌牌「好友三重奏」当好友系统。
- 不要把纪念版六页签当成运营期社交壳。
- 不要实现血石商店→`user_mail`。
- 条目进度文案用 `{n} / {m}`，不要把原作「10 次剧情」「PVP 3 场」写进产品。
