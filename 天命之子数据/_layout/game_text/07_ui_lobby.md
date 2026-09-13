# 07 · HOME / Lobby / Menu UI 文案槽

纪念版竖屏（繁中）打印字是源。运营期 Home 只作「纪念版剥掉了什么」对照。契灵回响客户端是自造简中，**不回写原作商标、抽卡口号、S CLASS**。

| 层 | 语言 | 证据 |
|---|---|---|
| **Memorial** | 繁体中文 + 英文匾 | `docs/reference/mobile-archive/screenshots/` · `NAVIGATION_MAP.md` · `_drafts/OBSERVED_UI_PATTERNS.md` |
| **LiveOps** | 日服截图 + 攻略叠字 | `视觉图鉴.html` · `jp_new_02.png`（白框是 wiki 标注，不是客户端字） |
| **Resonance** | 简体中文 | `client/Assets/Scripts/Resonance.App/UI/` · `GameRoot.cs` |

UNKNOWN = 截图像素或代码里没印出来。不要用运营记忆填纪念版空槽。

---

## 0. 三套底栏（不要混）

| 槽 | Memorial 六页签 | LiveOps 六页签（攻略标注） | Resonance `UiChrome.DefaultTabs` |
|---|---|---|---|
| 1 | **首頁**（拱门，亮金=当前） | 主界面 | **首页** |
| 2 | **Child**（英文，火焰） | 天子 / 天子列表 | **契灵** |
| 3 | **魔界溫泉** | （运营不在底栏；入口在侧栏） | **关卡** |
| 4 | **圖鑑** | 背包 | **图录** |
| 5 | **黑卡蒂的圖書館** | 成就 | **书库** |
| 6 | **夏娃的冒險** | 抽卡 · 好友（第六格是好友） | **深途** |

规则：六页签**只出现在首页和编队/Child 列表**。详情、模态、温泉、图鉴、图书馆、夏娃、设定都拿掉。

Resonance 页签映射：`0 Home` · `1 Characters/Team` · `2 Stage` · `3 Archive` · `4 Library` · `5 Deep`。

---

## 1. 公共铬（所有屏共用的槽）

| 槽 ID | Memorial | Resonance | 备注 |
|---|---|---|---|
| `chrome.close_x` | **×** | **×** | 永远右上。子页也再画一枚。 |
| `chrome.confirm` | **確認** | **确认** | 黄胶囊，浮在面板**下面**。 |
| `chrome.cancel` | **取消** | **取消** | 红橙胶囊。 |
| `chrome.notice.title` | **NOTICE** | **NOTICE** | 英文金标题，不译。 |
| `chrome.notice.info` | ① + 一句 | `(i)  …` | |
| `chrome.toggle.on` | **ON** | **ON** | |
| `chrome.toggle.off` | **OFF** | **OFF** | |
| `chrome.power` | **戰鬥力** | **战斗力** | 大黄数字带千分位。 |
| `chrome.star` | ★（已拥有红星） | ★ | 图鉴筛选用金星。 |

---

## 2. 首页 Home

证据：`001_home.png` · `183_home_after_settings.png` · `096_lobby_after_tabs.png`

纪念版首页**几乎空**：无体力、钻石、金币、红石、banner、抽卡。只有全身 Presenter + 右缘四枚六边形 + 底六页签。

### 2.1 身份（压在腿上，约 60–65% 屏高）

纪念版 Home **不印**名字/战斗力（`001_home.png` 无人名）。详情/编队才印。Resonance `HomeHud` 在 Home 就画身份。

| 槽 ID | Memorial | Resonance | 位置 |
|---|---|---|---|
| `home.name` | （Home 空；详情有角色名） | `{角色名}` | 左中 |
| `home.flavor` | （详情才有台词） | `CharacterPresenter.Flavor` | 名下 |
| `home.stars` | — | ★×N | |
| `home.power` | — | **战斗力** `{n}` | |

### 2.2 右缘工具条（图标无字）

上→下，`001_home.png`：

| 槽 ID | 图标 | 打开后标题 | 证据 |
|---|---|---|---|
| `home.rail.settings` | 齿轮 | **設定** | `050_settings.png` |
| `home.rail.deco` | 箱子/机柜 | Home 编辑 **選擇Child** | `053_home_second_icon.png` |
| `home.rail.unknown` | 圆+斜杠 | UNKNOWN（未打开） | |
| `home.rail.hide` | 眼睛斜杠 | 剥铬看画（Home 上未截到结果） | |

Resonance：Home 只画一枚六边形齿轮，`go.name = "设定"`。另有幽灵钮（见 2.4），不是纪念版右缘。

### 2.3 底栏

见 §0。当前页签 **首頁** 亮金。

### 2.4 Resonance 首页幽灵入口（纪念版没有）

`GameRoot.DrawHome` 左上幽灵钮。LiveOps 才有这些系统；纪念版剥光。

| 槽 | 文案 | 打开屏 |
|---|---|---|
| | **召唤** | Summon |
| | **日常** | Daily |
| | **商店** | Shop |
| | **邮件** | Mail |
| | **成就** | Achieve |
| | **称号** | Title |
| | **助战** | Friend |
| | **休息** | Rest（不写「温泉」） |
| | **用餐** | Food |
| | **切磋** | Pvp |
| | **入门** | Tutorial |

顶栏货币胶囊：`金屑` `{n}` · `残核` `{n}`（`CurrencyPlate`，不写真金 IAP）。

### 2.5 LiveOps 首页（纪念版禁止画）

`jp_new_02.png` + `视觉图鉴.html`。白框是攻略叠字。

| 槽 | 攻略标注 / 可见字 |
|---|---|
| 顶货币 | 体力 · 钻石 · 金币 · 玛瑙 · 红石 |
| 玩家 | `LEVEL {n}` `{名}` · `EXP` |
| 右缘 | 设置 · 邮箱 · 图鉴 · ホーム設定 · 鑑賞モード |
| 左缘 | Event / 更新情报 · Shop / 商店 · 每日任务 · 特定活动 |
| 身份 | `{名}` · **詳細** · チームの総戦闘力 |
| 侧栏入口（叠字） | 主线 / PVP / 星云 · **还魂** / 制作 / 温泉 |
| 底栏 | 主界面 · 天子列表 · 背包 · 成就 · 抽卡 · 好友 |

**还魂**：纪念版截图集**没有**此屏。运营期养成入口（wiki 59271 等）。Resonance **BAN**（`04-mvp-gap-and-bans.md`）。文案槽保留空位，不填客户端。

---

## 3. 设定

证据：`050_settings.png` · `051_settings_scrolled.png`

| 槽 ID | Memorial | Resonance | 类型 |
|---|---|---|---|
| `settings.title` | **設定** | **设定** | 标题（齿轮图标旁） |
| `settings.player` | `Lv {n} {name}` | — | 例 `Lv 100 LaoTie101` |
| `settings.sub` | — | **只改本机  ·  不联网** | |
| `settings.sec.audio` | **音效** | — | 分区 |
| `settings.bgm` | **BGM** | — | 滑条 0–100 |
| `settings.sfx` | **效果音** | — | |
| `settings.voice` | **角色聲音** | — | |
| `settings.sec.video` | **畫面** | — | |
| `settings.live2d` | **Live2D動作** | — | ON/OFF |
| `settings.motion` | **動作強度** | — | 0–100 |
| `settings.motion.hint` | 設定Live2D動畫的強度。 | — | |
| `settings.lq` | **低畫質模式** | — | ON/OFF |
| `settings.lq.hint` | 降低畫面品質以改善顯示速度。 | — | |
| `settings.sec.data` | **資料設定** | **资料** | Resonance 分区名更短 |
| `settings.dl_voice` | **下載聲音檔** | — | |
| `settings.dl_voice.hint` | 下載所有聲音檔。 | — | |
| `settings.dl_voice.cta` | **下載** | — | 黄钮；流程未开 |
| `settings.res` | **SD/HD 變更解析度** | — | 未点 |
| `settings.sec.other` | **其他資訊** | **其他** | |
| `settings.opener` | **開場動畫** | — | 未开 |
| `settings.license` | **圖示授權** | — | 未开 |
| `settings.lang` | **選取語言** | — | 未开 |
| `settings.confirm` | **確認** | **确认** | |
| `settings.battle` | — | **战斗** | Resonance 自造分区 |
| `settings.auto` | — | **自动** · 关 / 半自动 / 全自动 | |
| `settings.speed` | — | **倍速** · 1× / 2× | |
| `settings.wipe` | — | **清除本地存档** | |
| `settings.ver` | — | **契灵回响  MVP v0.1.0** | |
| `settings.wipe.notice` | — | `(i)  会清掉本机存档。` | NOTICE |
| `settings.wipe.yes` | — | **确认** | |
| `settings.wipe.no` | — | **取消** | |

---

## 4. Home 编辑 / Enjoy Home + Deco

证据：`053`–`057`

### 4.1 编辑态右缘（有中文标签）

| 槽 ID | Memorial |
|---|---|
| `homeedit.title` | **選擇Child** |
| `homeedit.slots` | **1**–**5**（展示槽） |
| `homeedit.adv` | **進階設定** |
| `homeedit.pick_child` | **選擇Child** |
| `homeedit.pick_spa` | **選擇溫泉** |
| `homeedit.reset` | **重置** |
| `homeedit.save` | **儲存** |
| `homeedit.view` | **變更檢視** |
| `homeedit.gallery` | **觀賞模式** |
| `homeedit.bgm` | ♪ `{曲名}` 例 `Pathos (Original Ver.)` |
| `homeedit.preset` | **預設1**–**預設4** |
| `homeedit.selected` | **SELECTED** |

### 4.2 选人 picker

| 槽 ID | Memorial |
|---|---|
| `homepick.lockup` | **Enjoy Home** + **Deco**（粉霓虹英文） |
| `homepick.hint1` | 選擇要顯示在主畫面上的Child。 |
| `homepick.hint2` | 最多可選擇5名Child。 |
| `homepick.sort` | **等級▼** |
| `homepick.search` | 放大镜（无字） |
| `homepick.cancel` | **取消** |
| `homepick.ok` | **確認** |

### 4.3 存档 NOTICE

| 槽 ID | Memorial |
|---|---|
| `homesave.body` | 儲存此預設？ |
| `homesave.cancel` | **取消** |
| `homesave.nosave` | **不要儲存** |
| `homesave.save` | **儲存** |

三钮全黄，层级靠位置：左取消 · 中不要 · 右储存。

---

## 5. 退出游戏 NOTICE

证据：`150`–`157`（文件名常标图鉴星级，像素是退出框）。`158_stay_in_game.png` 实际是回首页，不是框。

| 槽 ID | Memorial |
|---|---|
| `quit.body` | 要離開遊戲嗎？ |
| `quit.no` | **否**（左） |
| `quit.yes` | **是**（右） |

---

## 6. Child / 编队

证据：`010_character_team.png` · `047_team_list.png` · `100_child_list.png` · `115_ready_to_swipe.png`

| 槽 ID | Memorial | Resonance |
|---|---|---|
| `team.leader_tag` | **LEADER** | 队长槽描边 |
| `team.leader_select` | **LEADER SELECT** | **队长** |
| `team.pager_l` | `{n} TEAM` 例 **7 TEAM** | — |
| `team.pager_r` | `{n} TEAM` 例 **9 TEAM** | — |
| `team.in_party` | **已加入隊伍** | **已加入队伍** |
| `team.join` | **加入隊伍**（`115`） | — |
| `team.power` | **戰鬥力** `{n}` | **战斗力  {n}`** |
| `team.sort` | **等級▼** | — |
| `team.search` | 放大镜 | — |
| `team.detail` | **詳細** | **詳細**（编队条仍用繁体两字） |
| `team.empty` | — | **空位** |
| `team.empty.hint` | — | 点下方格子加入编队 |
| `team.stats.hp` | **HP** | **HP** |
| `team.stats.atk` | **攻擊力** | **攻击** |
| `team.stats.def` | **防禦力** | **防御** |
| `team.lv` | **Lv {n}/{max}** | |
| `team.element` | **火屬性** 等 | `ElementWord` 火/水/木/光/暗 |
| `team.role` | **干擾型** 等 | 攻/防/扰/疗/辅 |
| `team.class` | **S CLASS** | 不进发行包 |
| `team.list_cta` | — | **编队** / **出战**（名册页双黄钮） |
| `team.radar` | 橙雷达钮 | UNKNOWN 目的地 |
| `team.figures` | 三人图标 | UNKNOWN |

底栏：Child 页签亮。有 **×**。

---

## 7. 角色详情

证据：`020_character_detail_overview.png` · `021`–`029` · `101_char_swipe_*`

无六页签。右缘有中文标签。

### 7.1 右缘

| 槽 ID | Memorial | Resonance |
|---|---|---|
| `detail.rail.gallery` | **圖庫模式** | **图库** |
| `detail.rail.skin` | **變更造型** | **造型** |
| `detail.rail.stats` | **詳細能力** | **详细** |

### 7.2 左叠字

| 槽 ID | Memorial | Resonance |
|---|---|---|
| `detail.name` | `{全名}` | `{名}` |
| `detail.flavor` | 台词 1–2 行 | Flavor |
| `detail.element` | **{色}屬性** | 火/水/木/光/暗 |
| `detail.role` | **{職}型** | 攻防扰疗辅 |
| `detail.lv` | **LV 等級** `{+uncap} {lv}/{max}` | **LV** `{lv}/{max}` · `+{uncap}` |
| `detail.ign` | 六边形 `{n}/{max}` 例 12/12 | **燃起  {n}/{max}** |
| `detail.aff` | **好感度** `{n}/{max}` | **好感度** |
| `detail.power` | **戰鬥力** | **战斗力** |
| `detail.hp` | **HP** | **生命** |
| `detail.atk` | **攻擊力** | **攻击** |
| `detail.def` | **防禦力** | **防御** |
| `detail.agi` | **敏捷度** | **敏捷** |
| `detail.crt` | **暴擊** | **暴击** |
| `detail.skill_cta` | **技能** | **技能** |
| `detail.slots` | **武器** · **防具** · **裝飾品** · **魂之歌牌** | **武器** · **防具** · **饰品** · **残章** |
| `detail.slot.empty` | （空井，无字） | **空** |
| `detail.puppet` | 人偶芯片（有的角色） | **契偶** |

### 7.3 详细能力分解

`021_character_detailed_stats.png`

| 槽 ID | Memorial | Resonance |
|---|---|---|
| `stats.part.child` | **Child** | **契体** |
| `stats.part.aff` | **好感度** | **好感** |
| `stats.part.gear` | **裝備** | **装备** |
| `stats.part.inlay` | **裝備鑲嵌** | — |

### 7.4 空装备 NOTICE

`042` `044` `045`（魂卡空槽 NOTICE 未入画）

| 槽 ID | Memorial |
|---|---|
| `equip.empty.body` | 未裝備任何物品。 |
| `equip.empty.ok` | **確認** |

已装备走物品卡（`112_saaya_weapon.png` 等），本表不列道具名。

---

## 8. 造型 / 衣柜

证据：`024_character_costume.png` · `025`

| 槽 ID | Memorial | Resonance |
|---|---|---|
| `skin.title` | **{名}的衣櫃** | **造型** |
| `skin.en` | — | **SKIN** |
| `skin.prompt` | 請從清單中選擇造型。 | **三面回响  ·  本貌常在** |
| `skin.sec.normal` | **一般造型** **NORMAL SKIN & CLOTHES** | — |
| `skin.row.name` | **{造型名}的造型** | **本貌** / **残响** / **夜巡** |
| `skin.wear` | **穿著**（灰） | **已启** |
| `skin.wearing` | **目前穿著中**（黄） | **着装** |
| `skin.locked` | — | **虚空** |
| `skin.ok` | **確認** | × 关闭 |
| `skin.hint.base` | （造型说明） | 核里最初那一面。本貌常在。 |
| `skin.hint.echo` | | 回声叠在刃上。核还记得那一跳。 |
| `skin.hint.night` | | 夜里才肯露面。虚空里那一层。 |
| `skin.footer` | | 点已启着装  ·  虚空未开 |

---

## 9. 技能

证据：`030`–`038` · `111` · `130_*`

| 槽 ID | Memorial | Resonance |
|---|---|---|
| `skill.lockup` | **SKILL INFORMATION** | — |
| `skill.title` | **技能清單** | **技能** |
| `skill.sub` | — | `LV  {n}   +{u}   连击 · 点按 · 上滑 · 驱动 · 队长` |
| `skill.type.default` | **DEFAULT** | **连击** |
| `skill.type.normal` | **NORMAL** | **点按** |
| `skill.type.slide` | **SLIDE** | **上滑** |
| `skill.type.drive` | **DRIVE** | **驱动** |
| `skill.type.leader` | **LEADER** | **队长** |
| `skill.reserve` | **技能預約** | — |
| `skill.reserve.hint` | 你能設定Skill順序。 | |
| `skill.reserve.set` | **設定** | |
| `skill.reserve.reset` | **重置** | |
| `skill.reserve.slot` | **E** / **T** / **S** | |
| `skill.edit.copy` | 按下欄位(E)來設定Tap Skill(T)或Slide Skill(S)。如果所有欄位皆未指派，Skill會自動啟動。如有尚未指派的欄位，已指派的欄位會集中在左側。 | |
| `skill.ok` | **確認** | **关闭** |
| `skill.edit.cancel` | **取消** | |
| `skill.edit.ok` | **確認** | |

---

## 10. 魔界温泉

证据：`070`–`078` · `073` · `075`–`076`

无六页签。

| 槽 ID | Memorial | Resonance |
|---|---|---|
| `spa.title.prefix` | **大家的** | — |
| `spa.title` | **露天溫泉**（木匾） | **休息**（禁止写温泉） |
| `spa.count` | 泡湯中的Child：`{n}/{max}` | |
| `spa.rail.set` | **設定** | |
| `spa.rail.book` | **溫泉圖鑑** | |
| `spa.rail.view` | **變更檢視** | |
| `spa.float.name` | `{短名}` + × | |
| `spa.action` | **泡湯**（世界内芯片，部分帧） | **歇一会** |
| `spa.rest.sub` | — | **契合入核  ·  核歇一回** |
| `spa.rest.body` | — | 核歇一回，契合才肯涨。 |
| `spa.rest.foot` | — | 歇一会入核  ·  不联网 |
| `spa.rest.stat` | — | `属性 ×{0.00}` |

### 10.1 DECORATION

`073_hotspring_encyclopedia.png`

| 槽 ID | Memorial |
|---|---|
| `deco.lockup` | **DECORATION** |
| `deco.tab.deco` | **溫泉裝飾品** |
| `deco.tab.sticker` | **貼紙** |
| `deco.hint` | 選擇溫泉裝飾品或貼紙來使用。 |
| `deco.using` | 物品正在使用中 `{n}/{max}` |
| `deco.info` | **詳細資訊** |
| `deco.close` | **關閉** |

### 10.2 温泉 LIBRARY

`075_hotspring_book.png`

| 槽 ID | Memorial |
|---|---|
| `spalib.lockup` | **溫泉 LIBRARY** |
| `spalib.tab.skin` | **溫泉造型** |
| `spalib.tab.deco` | **溫泉裝飾品** |
| `spalib.tab.sticker` | **貼紙** |
| `spalib.hint` | 檢視溫泉造型並獲得靈氣。 |
| `spalib.count` | **獲得的溫泉造型** `{n}/{max}` |

---

## 11. 图鉴

证据：`080_archive.png` · `084` · `085` · `086`/`365` · `160`–`167` · `170`–`180`

无六页签。**×** 回首页。

| 槽 ID | Memorial | Resonance |
|---|---|---|
| `lib.lockup` | **CHARACTER LIBRARY** | **LIBRARY** |
| `lib.title` | — | **图录** |
| `lib.tab.child` | **Child** | （无顶栏四页；25 契灵矩阵） |
| `lib.tab.carta` | **魂之歌牌** | |
| `lib.tab.puppet` | **人偶** | |
| `lib.tab.skin` | **造型** | |
| `lib.owned` | `{n}/{max}` 例 561/561 | **已录  {n} / 25** |
| `lib.filter.star` | **★5+** **★4** **★3** **★2** **★1** | 金星筛选语法 |
| `lib.sort` | **變更順序** | |
| `lib.sub` | — | 二十五契灵  ·  五色五职 |
| `lib.empty` | 水印空砖（心/颅/火） | **—** |

魂卡筛 ★5/★4/★3。人偶：属性圆 + 分区 **傳奇** 等。

### 11.1 图鉴鉴赏（未拥有详情）

`170_libview_base_stats.png`

| 槽 ID | Memorial |
|---|---|
| `libview.base` | **基礎能力** |
| `libview.skills` | **技能預覽** |
| `libview.drive` | **檢視 Drive效果** |
| `libview.gallery` | **圖庫模式** |
| `libview.intro` | **簡介** |
| `libview.ok` | **確認**（技能预览底） |

---

## 12. 黑卡蒂图书馆

证据：`090_library.png`（落地页；内页未开）

| 槽 ID | Memorial | Resonance |
|---|---|---|
| `hecate.lockup` | **HECATE'S LIBRARY** | — |
| `hecate.title` | — | **书库** |
| `hecate.sub` | — | **契灵回响  ·  残章** |
| `hecate.bubble` | 图书馆提供的愛的策略……啊……！進……進門時好歹發出點聲音吧，好不好？！ | 不写原作剧情 |
| `hecate.recent` | **最近閱讀** | |
| `hecate.new` | **新到貨** | |
| `hecate.tab.home` | **圖書館** | |
| `hecate.tab.main` | **主要** | |
| `hecate.tab.sp` | **特別** | |
| `hecate.tab.awake` | **覺醒** | |
| `hecate.tab.other` | **其他** | |

Resonance 书库条目标题（自造）：契核 · 契灵 · 废都裂口 · 五色 · 点按 · 上滑 · 驱动 · 狂热时间 · 城门守核。

---

## 13. 夏娃的冒险

证据：`095_eve_adventure.png` · `097_after_eve_back.png`  
`GAME START` 未点进。

| 槽 ID | Memorial |
|---|---|
| `eve.lockup` | **ADVENTURE OF EVE** |
| `eve.brand` | **SHIFT UP** |
| `eve.start` | **GAME START** |
| `eve.nosave` | 尚無可用的遊戲記錄。 |
| `eve.back` | **返回**（棕，不是黄） |

---

## 14. Resonance 大厅覆盖层（纪念版无对应屏）

全部：金/虚空铬、不画六页签、不画马赛克。关 **×** 或底胶囊。

### 14.1 召唤

| 槽 | 文案 |
|---|---|
| 标题 | **召唤** |
| 副标 | 契核应声  ·  回响入册 |
| 核 | **契核** |
| 句 | 核跳一下，契灵应一声。 |
| 脚 | 一次一响 |
| CTA | **抽取** |

### 14.2 日常（任务）

| 槽 | 文案 |
|---|---|
| 标题 | **日常** / **DAILY** |
| 副标 | 今日核还在跳  ·  过零点作废 |
| 行标题 | 通关一回 · 驱动一次 · 上滑一次 · 点按三十 · 听核一回 |
| CTA | **领取** / **未完** |

### 14.3 商店

| 槽 | 文案 |
|---|---|
| 标题 | **商店** |
| 副标 | 核尘兑包  ·  只走本机 |
| 包名 | **金币** · **魂石** · **经验** · **好感** |
| 脚 | 点包兑换  ·  核尘标价 |
| CTA | **关闭** |

### 14.4 邮件

| 槽 | 文案 |
|---|---|
| 标题 | **邮件** |
| 副标 | 三封未拆  ·  补偿 / 活动 / 系统 |
| 类 | **补偿** · **活动** · **系统** |
| 信题 | 核尘回补 · 裂口加倍 · 本机核还在跳 |
| CTA | **阅读** |
| 脚 | 点阅读入核  ·  不联网 |

### 14.5 成就

| 槽 | 文案 |
|---|---|
| 标题 | **成就** / **SEAL** |
| 进度 | **已应  {n} / {max}** |
| 印 | 首胜 · 狂热一次 · 全队满编 · 深域 |
| 态 | **已应** / **虚空** |
| 脚 | 金印已应  ·  虚空未启 |
| CTA | **关闭** |

### 14.6 称号

| 槽 | 文案 |
|---|---|
| 标题 | **称号** / **TITLE** |
| 副标 | 四枚核印  ·  佩戴一枚 |
| 名 | 裂口初开 · 狂热一瞬 · 五人同核 · 听核 |
| 芯片 | **佩戴** / **已录** / **未闻** |
| 脚 | 核印不联网  ·  只记本机 |

### 14.7 助战

| 槽 | 文案 |
|---|---|
| 标题 | **助战** / **SUPPORT** |
| 副标 | 三名过客  ·  核还亮着 |
| 脚 | 借刃一回  ·  不入编队 |
| 态 | **可借** / **虚空** / **虚位** |

### 14.8 用餐

| 槽 | 文案 |
|---|---|
| 标题 | **用餐** / **MEAL** |
| 副标 | 三餐入核  ·  战攻一时 |
| 餐 | 余烬羹 · 契核饼 · 潮汐粥 |
| CTA | **用餐** / **虚空** |

### 14.9 切磋

| 槽 | 文案 |
|---|---|
| 标题 | **切磋** |
| 副标 | 本机开关  ·  不联网 |
| 脚 | 无对战  ·  不联网  ·  不拷贝网络 |
| CTA | **开门** / **关门** |

### 14.10 入门

| 槽 | 文案 |
|---|---|
| 标题 | **入门** |
| 进度 | `01 / 03` |
| 副标 | 三页入核  ·  点按 / 上滑 / 驱动 |
| 页题 | 点按是刃 · 上滑是势 · 驱动把核压出去 |
| CTA | **下一页** / **知道了** |

### 14.11 燃起

| 槽 | 文案 |
|---|---|
| 标题 | **燃起** / **IGNITE** |
| 副标 | 攻 / 暴 / 敏  ·  各十二核 |
| 脚 | 首核四百红  ·  余核一百  ·  非树 |
| CTA | **关闭** |

### 14.12 关卡 / 深途 / 星云（底栏 3、6）

| 屏 | 标题槽 | 其它 |
|---|---|---|
| 第1章 | **第1章** · **废都裂口** | 点节点进入  ·  已过可再战 |
| 难度 | **普通** / **困难** | 当前项加 **·** |
| 波次 | **本关敌人** | CTA **战斗开始** |
| 深途 | **深途** | 短径通向更硬的关 |
| 深途节点 | 裂口余烬 · 锈轨深层 · 守核夹层 · 炉心 | 困难 I–IV · 锁定 / 进入 / 已过 |
| 星云 | **星云一趟** | CTA **战斗** · 核 / 障 · **驱动** |

### 14.13 暂停 / 结算 / 掉落 / 启动

| 屏 | 槽 | 文案 |
|---|---|---|
| Boot | 标题 | **契灵回响** |
| 暂停 | 标题 | **暂停** |
| 暂停 | CTA | **继续** · **回首页** |
| 结算 | 眉 | **战斗结果** |
| 结算 | 标题 | **完成** / **失败** |
| 结算 | 伤 | **伤害总计** |
| 结算 | 掉落眉 | **掉落** |
| 结算 | CTA | **下一关** / **再战** · **回首页** |
| 掉落 | 标题 | **获得** |
| 掉落 | 眉 | **掉落** |
| 掉落 | 脚 | 金屑入袋  ·  虚空不留 |
| 掉落 | CTA | **收下** |
| 掉落格 | | **材料** |

---

## 15. 截图文件名 → 需要文案的屏

忽略 `_*.png` 调试叠层。文件名与像素冲突时以像素为准（`141`/`142`/`150`–`157` 常是退出 NOTICE）。

| 前缀 | 屏 | 文案重点 |
|---|---|---|
| `001` `183` `096` | Home | 六页签、右缘无字 |
| `050` `051` | 设定 | 全部分区标签 |
| `053`–`057` | Home+Deco | 选择Child / NOTICE 三钮 |
| `010` `047` `100` `115` | Child 编队 | LEADER、已加入隊伍、詳細、等級▼ |
| `020`–`029` `101_*` `110` | 详情 | 右缘三工具、四槽名、技能 |
| `021` `022` | 详细能力 | Child / 好感度 / 裝備 / 鑲嵌 |
| `024` `025` `450`–`454` | 衣柜 / 造型卡 | {名}的衣櫃、穿著 |
| `030`–`038` `111` `130` `131` | 技能 | SKILL INFORMATION、預約 |
| `042`–`046` `112` `500`–`536` | 装备 | 空 NOTICE、物品卡 |
| `070`–`078` | 温泉 | 露天溫泉、泡湯、圖鑑 |
| `073` | DECORATION | 裝飾品 / 貼紙 / 關閉 |
| `075` `076` | 温泉 LIBRARY | 三页签 |
| `080` `082`–`087` `160`–`167` `365`–`368` | 图鉴格 | 四顶栏、星筛 |
| `084` `400`–`419` | 魂之歌牌 | 页签 + 详情 |
| `085` `432`–`436` | 人偶 | 傳奇等分区 |
| `168`–`199` `300`–`350` | 鉴赏 | 基礎能力 / 技能預覽 |
| `090` | 黑卡蒂 | 五底栏 + 英文匾 |
| `095` `097` | 夏娃 | GAME START / 返回 |
| `157`（像素） | 退出 | 要離開遊戲嗎？ |

未截：战斗 HUD、抽卡、商店、体力条、还魂、背包、公会、登录/标题（纪念版会话从局内开始）。

---

## 16. 复刻时用哪一套字

| 要做的产品 | 底栏 | 设定标题 | 装备第四槽 | 温泉 |
|---|---|---|---|---|
| 对齐纪念版截图 | 首頁 / Child / 魔界溫泉 / 圖鑑 / 黑卡蒂的圖書館 / 夏娃的冒險 | 設定 | 魂之歌牌 | 露天溫泉 |
| 契灵回响发行 | 首页 / 契灵 / 关卡 / 图录 / 书库 / 深途 | 设定 | 残章 | 休息（禁止「温泉」） |
| 运营期还原 | 主界面 / 天子 / 背包 / 成就 / 抽卡 / 好友 | 设置 | 魂之歌牌 | 侧栏入口；另有还魂 |

简繁不要混在同一屏：纪念版全繁；Resonance 全简（编队 **詳細** 是现存例外）。英文匾（NOTICE、LIBRARY、SKILL INFORMATION、GAME START、LEADER）保持英文。
