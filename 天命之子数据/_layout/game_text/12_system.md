# 12 系统文案 — SYSTEM / ERROR / TUTORIAL / SETTINGS

Wiki / 图鉴只覆盖角色、技能、掉落。完整客户端还需要一套**系统 UI 字表**。本表只写系统层（启动、下载、权限、错误、确认框、设定、教程壳、账号、合规），不写真金商品名、不写角色技能、不写原作 Destiny Child 风味。

产品名沿用客户端：`契灵回响`。战斗动词沿用客户端自造词：`点按` / `上滑` / `驱动` / `连击`。货币沿用：`金屑` / `残核`。

占位：`{0}` `{1}` 按出现顺序替换；`{ver}` 版本；`{n}` `{m}` 计数；`{size}` 体积；`{path}` 路径；`{uid}` 账号标识。

键名：`域.面.槽`，全小写点分。运行时建议一张 `id → zh` 表，后续可加 `en`。

---

## 客户端现状（缺口）

来源：`F:\天命之子\client\Assets\Scripts\Resonance.App\`（硬编码中文，无字表）。`Assets\Content\` 只有 `catalog.json` 战斗数据，无系统文案。

| 已有（硬编码） | 缺（完整游戏仍要） |
|---|---|
| 启动标题「契灵回响」2 秒闪屏 | 点按开始、用户协议、隐私、年龄提示、强更、维护公告 |
| 设定：自动 / 倍速 / 清档 / 版本号 | 语言、BGM/SE/语音、画质、帧率、振动、推送、跳过演出、账号、缓存 |
| 入门三页：点按 / 上滑 / 驱动 | 跳过教程确认、回放、首页/编队/召唤首次气泡 |
| 暂停 / 确认 / 取消 / 关闭 / 回首页 | 再按一次退出、加载中、请稍候 |
| 清档 NOTICE | 存档损坏、写入失败、空间不足 |
| 切磋「不联网」 | 网络超时、断线、会话过期、服务器繁忙、资源下载 |
| 无账号层 | 游客、绑定、换号、登出、游客进度警告 |
| 无权限层 | 存储、通知、相册、去系统设置 |
| 商店「不写真金 IAP」 | 支付失败、恢复购买、余额不足（系统框） |

下列 `zh` 为洁净室自拟，可直接进字表。已在客户端出现的短词尽量对齐（确认、取消、设定、入门、暂停、知道了），避免两套说法。

---

## 1. 公共按钮与对话框壳

| id | zh | notes |
|---|---|---|
| sys.btn.confirm | 确认 | `UiChrome.Confirm` 默认 |
| sys.btn.cancel | 取消 | `UiChrome.Cancel` 默认 |
| sys.btn.close | 关闭 | 面板底胶囊、商店/成就/技能表 |
| sys.btn.ok | 好 | 单按钮提示 |
| sys.btn.back | 返回 | 返回上一屏 |
| sys.btn.next | 下一页 | 教程非末页；对齐 `TutorialBoard` |
| sys.btn.prev | 上一页 | 教程回翻 |
| sys.btn.skip | 跳过 | 教程 / 演出 / 开场 |
| sys.btn.retry | 重试 | 错误框主按钮 |
| sys.btn.later | 稍后 | 非强制更新、权限可延后 |
| sys.btn.continue | 继续 | 暂停恢复；对齐 `PauseBoard` |
| sys.btn.got_it | 知道了 | 教程末页、首次提示；对齐 `TutorialBoard` |
| sys.btn.home | 回首页 | 暂停 / 结算；对齐 `PauseBoard` `ResultBoard` |
| sys.btn.start | 开始 | 标题点按开始 |
| sys.btn.exit | 退出 | 确认退出游戏 |
| sys.btn.settings | 设定 | 齿轮；对齐 `SettingsModal` 标题 |
| sys.btn.open_os | 去系统设置 | 权限被拒后跳 OS |
| sys.btn.copy | 复制 | 复制 UID |
| sys.dlg.title | 提示 | 通用对话框标题 |
| sys.dlg.notice | 注意 | 对齐设定清档卡 `NOTICE` 语义，中文化 |
| sys.dlg.warning | 警告 | 不可逆操作 |
| sys.loading | 加载中… | 转场、进战斗、读档 |
| sys.wait | 请稍候 | 写入存档、校验资源 |
| sys.percent | {0}% | 进度数字 |
| sys.count_of | {0} / {1} | 通用分数 |
| sys.empty | 空 | 槽位空；对齐装备「空」 |
| sys.on | 开 | 开关开 |
| sys.off | 关 | 开关关；对齐自动「关」 |
| sys.none | 无 | 无掉落等 |
| sys.dont_show | 不再提示 | 首次提示勾选 |
| sys.copied | 已复制 | 复制 UID 成功 |
| sys.coming | 尚未开放 | 未实装入口 |
| sys.locked | 锁定 | 关卡节点；对齐 `DeepBoard` |

---

## 2. 启动、协议、版本、维护

| id | zh | notes |
|---|---|---|
| boot.title | 契灵回响 | `BootSplash` 产品名 |
| boot.tap | 点按屏幕开始 | 标题屏 |
| boot.copyright | © 契灵回响 | 无原作版权句 |
| boot.version | 版本 {ver} | 设定页现写死 MVP v0.1.0 |
| boot.ver_line | 契灵回响  {ver} | 设定「其他」行 |
| boot.age | 本游戏适合 12 岁及以上玩家。请合理安排游戏时间。 | 商店/适龄；非原作评级文 |
| boot.agree_lead | 进入游戏即表示你已阅读并同意 | 标题勾选前缀 |
| boot.tos | 用户协议 | 可点链接 |
| boot.privacy | 隐私政策 | 可点链接 |
| boot.and | 和 | 「协议和隐私」连接词 |
| boot.need_agree | 请先勾选同意用户协议与隐私政策。 | 未勾选点开始 |
| boot.force_update.title | 需要更新 | 强更标题 |
| boot.force_update.body | 当前版本过旧，请更新后再进入。 | 强更正文 |
| boot.force_update.go | 前往更新 | 跳商店 |
| boot.optional_update.title | 发现新版本 | 可选更新 |
| boot.optional_update.body | 新版本 {ver} 已发布。建议更新以获得修复与优化。 | 可稍后 |
| boot.maintenance.title | 维护中 | 服务器维护 |
| boot.maintenance.body | 服务器正在维护。预计结束时间：{0}。 | `{0}` 时间或「请稍后再试」 |
| boot.maintenance.until | 请稍后再试。 | 无结束时间时 |
| boot.incompatible | 客户端与资源版本不一致，请重新启动并完成更新。 | 热更校验失败 |

---

## 3. 资源下载与存储

客户端现为内置包、无热更。完整发行仍要这些键。

| id | zh | notes |
|---|---|---|
| dl.check | 正在检查资源… | 启动校验 |
| dl.ready | 资源已就绪 | 无需下载 |
| dl.start | 正在准备下载… | 清单拉取 |
| dl.progress | 下载中  {n} / {m} | 文件计数 |
| dl.size | 本次下载约 {size} | 体积预告 |
| dl.unpack | 正在解压资源… | 写本地 |
| dl.verify | 正在校验文件… | hash |
| dl.pause | 已暂停下载 | 切后台/点暂停 |
| dl.resume | 继续下载 | |
| dl.wifi_hint | 建议在 WLAN 环境下下载。 | 非阻断 |
| dl.cell.title | 使用移动网络下载？ | 流量确认 |
| dl.cell.body | 本次约 {size}。使用移动网络可能产生流量费用。 | |
| dl.cell.ok | 继续下载 | |
| dl.fail | 资源下载失败。请检查网络后重试。 | 通用失败 |
| dl.timeout | 下载超时。请稍后重试。 | |
| dl.space.title | 存储空间不足 | |
| dl.space.body | 需要约 {size} 可用空间才能继续。请清理后再试。 | |
| dl.corrupt | 资源文件损坏。将重新下载缺失部分。 | 校验失败后自动补 |
| dl.retry_all | 重新下载 | 整包 |

---

## 4. 系统权限

| id | zh | notes |
|---|---|---|
| perm.title | 需要权限 | |
| perm.storage.title | 存储权限 | Android 旧版写扩展存储 |
| perm.storage.body | 用于保存游戏数据与下载资源。 | |
| perm.notify.title | 通知权限 | |
| perm.notify.body | 用于活动与维护提醒。可随时在系统设置中关闭。 | |
| perm.photo.title | 相册权限 | 头像/分享，可选 |
| perm.photo.body | 用于保存截图到相册。 | |
| perm.denied.title | 权限未开启 | |
| perm.denied.body | 未获得所需权限，相关功能无法使用。可在系统设置中开启。 | |
| perm.optional | 也可以稍后在设定中开启。 | 非必需权限 |

---

## 5. 网络与会话错误

客户端设定写「只改本机 · 不联网」。联网或热更一旦接上，下列为最低集。

| id | zh | notes |
|---|---|---|
| err.net.title | 网络异常 | |
| err.net.body | 无法连接服务器。请检查网络后重试。 | |
| err.net.timeout | 连接超时。请稍后重试。 | |
| err.net.offline | 当前没有网络连接。 | 飞行模式等 |
| err.net.unstable | 网络不稳定，请稍后重试。 | |
| err.server.busy | 服务器繁忙，请稍后再试。 | 503 |
| err.server.internal | 服务器暂时无法处理请求。 | 5xx 兜底 |
| err.session.expired | 登录已过期，请重新进入。 | token |
| err.session.kicked | 账号已在其他设备登录。 | 互踢 |
| err.auth.fail | 登录失败，请重试。 | |
| err.auth.banned | 该账号暂时无法进入游戏。 | 不写具体原因以免泄露 |
| err.version | 版本不匹配，请更新客户端。 | |
| err.unknown | 发生未知错误（{0}）。 | `{0}` 错误码 |
| err.code | 错误码 {0} | 附在正文下 |
| err.retry_title | 是否重试？ | |
| err.return_title | 返回标题画面 | 无法恢复时 |
| err.save.write | 存档写入失败。请确认存储空间后重试。 | `SaveStore.Write` catch |
| err.save.read | 无法读取存档。 | |
| err.save.corrupt.title | 存档损坏 | |
| err.save.corrupt.body | 本地存档无法读取。可尝试从备份恢复，或清除后重新开始。 | 清档是毁灭性的，必须双确认 |
| err.save.missing | 找不到存档文件。 | `{path}` 可作副行 |
| err.catalog | 无法加载游戏数据。请重新启动。 | `catalog.json` 失败走 builtin 时也可提示 |

---

## 6. 账号：游客、绑定、换号

客户端无账号。完整包（含以后联网）仍要游客警告，否则商店审核与客诉会炸。

| id | zh | notes |
|---|---|---|
| acc.guest | 游客 | 登录方式 |
| acc.guest.enter | 游客进入 | 标题按钮 |
| acc.guest.warn.title | 游客进度提醒 | 首次进游戏 |
| acc.guest.warn.body | 游客进度保存在本机。卸载、更换设备或清除数据后可能丢失。建议尽快绑定账号。 | 系统句，不写世界观 |
| acc.guest.warn.bind | 去绑定 | |
| acc.guest.warn.later | 先进入 | |
| acc.bind | 绑定账号 | 设定入口 |
| acc.bind.lead | 绑定后可在其他设备找回进度。 | |
| acc.bind.ok | 绑定成功。 | |
| acc.bind.fail | 绑定失败，请重试。 | |
| acc.bind.taken | 该账号已绑定其他进度。 | |
| acc.bind.already | 当前进度已绑定。 | |
| acc.platform | 平台账号 | 抽象一层，不写具体渠道名 |
| acc.switch | 切换账号 | |
| acc.switch.confirm | 切换账号将返回标题画面。未绑定的游客进度可能无法找回。继续？ | |
| acc.logout | 登出 | |
| acc.logout.confirm | 确定登出？ | |
| acc.uid | 编号  {uid} | 设定页展示 |
| acc.uid.copy | 复制编号 | |
| acc.delete | 删除账号 | 合规入口；真删除走客服/冷静期 |
| acc.delete.warn | 删除账号将清除云端进度且不可恢复。此操作需额外确认。 | 不在客户端一键删 |

---

## 7. 设定

对齐 `SettingsModal`：分区 战斗 / 资料 / 其他；现有行 自动、倍速、清除本地存档。下列补齐完整设定。

### 7.1 壳与现有行

| id | zh | notes |
|---|---|---|
| set.title | 设定 | |
| set.sub.local | 只改本机  ·  不联网 | 当前单机副标题 |
| set.section.battle | 战斗 | |
| set.section.data | 资料 | |
| set.section.audio | 声音 | 新增 |
| set.section.graph | 画面 | 新增 |
| set.section.lang | 语言 | 新增 |
| set.section.account | 账号 | 新增；单机可隐藏 |
| set.section.other | 其他 | |
| set.auto | 自动 | |
| set.auto.off | 关 | `AutoMode.Manual` |
| set.auto.semi | 半自动 | |
| set.auto.full | 全自动 | |
| set.speed | 倍速 | |
| set.speed.1 | 1× | |
| set.speed.2 | 2× | |
| set.wipe | 清除本地存档 | 灰条按钮 |
| set.wipe.title | 注意 | |
| set.wipe.body | 会清掉本机存档。此操作无法撤销。 | 现文案「会清掉本机存档。」加不可撤销 |
| set.wipe.path | {path} | 副行显示路径 |

### 7.2 声音 / 画面 / 语言 / 其他

| id | zh | notes |
|---|---|---|
| set.bgm | 音乐 | |
| set.se | 音效 | |
| set.voice | 语音 | 无语音时隐藏 |
| set.mute | 静音 | |
| set.vibrate | 振动 | |
| set.quality | 画质 | |
| set.quality.low | 流畅 | |
| set.quality.mid | 标准 | |
| set.quality.high | 高清 | |
| set.fps | 帧率 | |
| set.fps.30 | 30 | |
| set.fps.60 | 60 | |
| set.lang | 语言 | |
| set.lang.zh | 简体中文 | |
| set.lang.en | English | 预留 |
| set.lang.apply | 更改语言将立即生效。 | |
| set.skip_skill | 跳过技能演出 | 重复战斗 |
| set.skip_story | 跳过剧情 | 无剧情时隐藏 |
| set.notify | 推送通知 | |
| set.cache | 清除缓存 | 资源缓存，不是存档 |
| set.cache.ok | 缓存已清除。 | |
| set.restore | 恢复购买 | IAP 预留；现商店不写真金可隐藏 |
| set.credits | 制作人员 | |
| set.support | 帮助与反馈 | |
| set.privacy | 隐私政策 | 设定内再入口 |
| set.tos | 用户协议 | |

---

## 8. 教程与首次提示

对齐 `TutorialBoard` 三页（点按 / 上滑 / 驱动），并补跳过、回放、大厅气泡。教程**战斗规则**可用客户端已有自造句；**壳按钮与跳过确认**必须进字表。

### 8.1 教程壳

| id | zh | notes |
|---|---|---|
| tut.title | 入门 | `TutorialBoard` 标题 |
| tut.sub | 三页入核  ·  点按 / 上滑 / 驱动 | 副标题 |
| tut.page_of | {0} / 03 | 现格式 `01 / 03` |
| tut.skip | 跳过入门 | 右上；客户端尚未做 |
| tut.skip.title | 跳过入门？ | |
| tut.skip.body | 跳过后仍可在首页「入门」里再看。战斗中点按、上滑、驱动不会改变。 | |
| tut.replay | 再看一遍 | 末页或设定 |
| tut.home_entry | 入门 | 首页 GhostBtn |

### 8.2 三页正文（已有，收入字表以免两套）

| id | zh | notes |
|---|---|---|
| tut.p1.tag | 点按 | 黄胶囊 |
| tut.p1.title | 点按是刃 | |
| tut.p1.body | 指尖落核，刃口才亮。连击只负责不停，点按才是真正的一刀。 | `TutorialBoard` Pages[0] |
| tut.p2.tag | 上滑 | |
| tut.p2.title | 上滑是势 | |
| tut.p2.body | 刃口抬势，不是甩手。上滑把核里那一跳抬高，势够才肯出招。 | |
| tut.p3.tag | 驱动 | |
| tut.p3.title | 驱动把核压出去 | |
| tut.p3.body | 核跳满了就别再攒。驱动把核里那一跳全压出去，废都才会让路。 | |

### 8.3 首次操作提示（气泡 / 遮罩，可关）

| id | zh | notes |
|---|---|---|
| tip.home.settings | 右上齿轮打开设定。自动与倍速只改本机。 | 首页 |
| tip.home.nav | 底栏：首页、契灵、关卡、图录、书库、深途。 | 对齐 `UiChrome.DefaultTabs` |
| tip.home.team | 先编队，再出战。 | 首页双胶囊 |
| tip.team.slot | 点空位或已上阵契灵，从下方矩阵换人。 | 编队 |
| tip.team.leader | 队长技能只由佩戴队长的契灵提供。 | |
| tip.stage.node | 点亮的节点可进入。已过的关可再战。 | 章节图 |
| tip.battle.tap | 点头像释放点按。 | 战斗 |
| tip.battle.slide | 上滑头像释放上滑。 | |
| tip.battle.drive | 核满后点 GOOD，或点头像放出驱动。 | 对齐 HUD `GOOD` +「驱动」 |
| tip.battle.auto | 右上可切手动 / 半自动 / 全自动。 | |
| tip.battle.pause | 左上暂停。暂停后可回首页，本场不保留。 | |
| tip.summon | 一次一响。抽取消耗残核。 | 不写原作抽卡法 |
| tip.shop | 本店用核尘兑包，不走真实货币。 | 对齐 `ShopBoard` |
| tip.save | 进度会在切后台或退出时写入本机。 | 首次清档前 |

---

## 9. 战斗 HUD 系统词（非技能名）

技能名走 catalog。下列是 HUD / 暂停 / 结算的系统层，wiki 也不会列全。

| id | zh | notes |
|---|---|---|
| bat.pause | 暂停 | `PauseBoard` `BattleHud` |
| bat.resume | 继续 | 暂停中按钮字 |
| bat.auto.manual | 手动 | HUD 右上 |
| bat.auto.semi | 半自动 | |
| bat.auto.full | 全自动 | |
| bat.speed.1 | ×1 | |
| bat.speed.2 | ×2 | |
| bat.drive | 驱动 | 条、pip、GOOD 下字 |
| bat.slide | 上滑 | 头像角标 |
| bat.tap_full | 点按满 | 就绪条 |
| bat.drive_ready | 驱动就绪 | |
| bat.good | GOOD | 驱动按钮；可保持拉丁 |
| bat.judge.perfect | 完美 | QTE |
| bat.judge.great | 优秀 | |
| bat.judge.good | 好 | |
| bat.judge.miss | 偏了 | |
| bat.fever | 狂热时间 | 非原作 FEVER 商标句 |
| bat.fever_pct | 狂热 {0}% | |
| bat.show.ready | 就绪？ | 驱动 showtime |
| bat.show.open | 开演 | 上滑 |
| bat.warn | 警告 | |
| bat.warn.enemy_drive | 敌方驱动 | |
| bat.crit | 暴击 | 飘字 |
| bat.weak | 弱点 | |
| bat.wave | 第{0}/2波 | |
| bat.result | 战斗结果 | |
| bat.result.win | 完成 | `ResultBoard` 胜 |
| bat.result.lose | 失败 | |
| bat.result.alt_win | 胜利 | `GameRoot` `_resultTitle` 另一套，建议统一用完成/失败 |
| bat.damage_total | 伤害总计 | |
| bat.loot | 掉落 | |
| bat.loot.none | 无掉落 | |
| bat.next | 下一关 | |
| bat.retry | 再战 | |
| bat.start | 战斗开始 | `WavePreview` |
| bat.enemies | 本关敌人 | |
| bat.leave.title | 现在离开？ | 暂停回首页 |
| bat.leave.body | 现在回首页将视为本场失败，不结算奖励。 | 系统规则，须写清 |

---

## 10. 通用资源与购买错误（系统框）

不是商店商品文案。缺材料、支付失败属于系统。

| id | zh | notes |
|---|---|---|
| eco.not_enough | 数量不足 | 通用 |
| eco.gold | 金屑不足 | |
| eco.stone | 残核不足 | |
| eco.go_shop | 去商店 | 可选 |
| iap.off | 本版本不提供真实货币购买。 | 对齐商店注释 |
| iap.fail | 购买未能完成。 | 预留 |
| iap.pending | 购买处理中，请稍候。 | |
| iap.restore.none | 没有可恢复的购买记录。 | |
| iap.unavailable | 当前无法使用购买功能。 | 商店下架/地区 |

---

## 11. 退出、返回键、崩溃

| id | zh | notes |
|---|---|---|
| os.back_again | 再按一次退出 | Android 返回 |
| os.exit.title | 退出游戏？ | |
| os.exit.body | 进度已尝试保存。确定退出？ | |
| os.crash | 游戏遇到问题，即将返回标题画面。 | 兜底 |
| os.bg_save | 已保存 | 切后台，可静默不显示 |

---

## 12. 合规与适龄（系统层最低）

不写实名接口细节；只留玩家看得到的句子。按发行地区显隐。

| id | zh | notes |
|---|---|---|
| legal.playtime | 合理游戏，注意休息。 | 时长提醒 |
| legal.minor | 未成年账号受游戏时长限制。 | 若做实名 |
| legal.health | 健康游戏忠告：抵制不良游戏，拒绝盗版游戏。注意自我保护，谨防受骗。适度游戏益脑，沉迷游戏伤身。合理安排时间，享受健康生活。 | 国内商店常见健康忠告，非原作 |

---

## 键统计与接入

| 分组 | 约略条数 |
|---|---|
| 公共按钮 / 对话框 | 28 |
| 启动协议版本维护 | 20 |
| 下载存储 | 18 |
| 权限 | 9 |
| 网络存档错误 | 22 |
| 账号 | 18 |
| 设定 | 40 |
| 教程 + 首次提示 | 28 |
| 战斗 HUD 系统词 | 32 |
| 资源 / IAP 系统框 | 9 |
| 退出崩溃 | 5 |
| 合规 | 3 |
| **合计** | **约 230** |

接入建议：

1. 先把 `UiChrome` 默认「确认 / 取消」、`SettingsModal`、`TutorialBoard`、`PauseBoard`、`BootSplash` 换成查表，消灭双份文案。
2. 错误框统一四件套：`title` + `body` + `err.code` + `sys.btn.retry` / `sys.btn.home`。
3. `{path}` `{uid}` `{ver}` 不要进翻译；只翻译外壳。
4. 单机包可隐藏 `set.section.account`、下载、IAP、推送；**键仍保留**，避免以后联网再拆表。
5. 禁止把 wiki 角色句、原作技能句、原作活动句塞进本表。

文件位置：`F:\天命之子\天命之子数据\_layout\game_text\12_system.md`。
)
