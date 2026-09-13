# 06 · 战斗 HUD / 场上字

完整一局战斗（进战前 → 场上 → 切镜 → 结算）需要的**可见字**。公式不算 HUD。发行包不要贴原作 Logo / `IT'S SHOWTIME!!` 字标；本表仍列出原作铬，方便对照我们自己的替换词。

## 来源

| 层 | 路径 | 能拿到什么 |
|---|---|---|
| 录像帧 | `docs/reference/gamekee/_combined/battle_vids/crops/` | 国际服 Ragna 手动、韩服 Ragna 全自动、世界王。HUD 铬几乎全是**英文** |
| wiki 静帧 | `docs/reference/gamekee/_combined/battle_refs/crop_{prebattle,inbattle,hud}.png` | 国际服主线：关卡名 + `PHASE`；黄签是攻略组中文，不是游戏字 |
| 动效页 | `天命之子数据/战斗动效.html` | 顶条 / 底栏 / 切镜词已写全，是本表主源 |
| UI 页 | `天命之子数据/视觉图鉴.html` | HUD 空间、自动三档、进战前「戰鬥開始」 |
| 公式 | `docs/reference/gamekee/parsed/formulas.md`、`wiki_ts_ss_ft.txt` | **没有**场上字，只有 TS/SS/FT 数学名 |
| 客户端 | `client/Assets/Scripts/Resonance.App/Battle/` | 自造中文铬；Vfx 注释明确禁止英文原词 |
| 纪念版截图 | `docs/reference/mobile-archive/` | **没有战斗 HUD**（`OBSERVED_UI_PATTERNS.md`：无五圆头、无 Drive、无倍速） |

日服 GameNext 把战场编成 8 号（视觉图鉴已引）：①敌信息 ②倍速 ③剩余时间 ④自动 ⑤技能环 ⑥Slide 冷却 ⑦Drive 钮 ⑧我方 Drive / Fever / 总 HP。和录像对得上。韩/日服**切镜大字仍是英文**（SHOWTIME / FEVER TIME / PERFECT / WeakPoint / Critical），本地化主要在技能名、大厅钮、结算列表。

状态：`docs` = 研究页已记；`client` = 切片已有可见字；`need` = 完整游戏还缺，或切片和原作铬对不齐。

---

## 1. 技能五槽（操作面标签）

Fever **没有**独立 skill 槽（`战斗与数值.html`、SQL `skill_1..5`）。

| 键 | 原作 HUD / 图鉴圈标 | 中文 wiki | 日 | 韩 wiki / 大厅 | 我们 docs | 我们 client | 状态 |
|---|---|---|---|---|---|---|---|
| AUTO | `DEFAULT` 圈标；场上无独立钮，自动打 | 普攻 / 自动攻击 | 通常攻撃 | 기본 공격 | 有 | `VisualTokens.SkillTag` **连击** | docs+client |
| TAP | `NORMAL` / `TAP`；满环浮字 `TAP FULL` | 重击 (TS) / 点按 | TAPスキル | 강타 / TAP | 有 | **点按**；就绪 `点按满` / Vfx `点按已满` | 两套就绪词，需并 |
| SLIDE | `SLIDE SKILL` 金圆标；`SLIDE SKILL READY` | 滑动 (SS) | スライドスキル | 슬라이드 | 有 | **上滑**；就绪 `上滑就绪`；切镜徽章 `滑步技能` | 徽章「滑步」≠ 操作「上滑」 |
| DRIVE | `DRIVE SKILL` 徽章 + 翅标 `DRIVE SKILL READY` | 大招 (DS) / DRIVE量表 | ドライブスキル | 드라이브 | 有 | **驱动**；就绪 `驱动就绪` | docs+client |
| LEADER | 编队槽 `LEADER` 白虚线标；场上无按钮 | 队长技 | リーダースキル | 리더 | 图鉴有 | `队长`（`VfxLeaderBurst`）；进战前编队标 **未做** | 进战前 LEADER 标 need |
| FEVER | 底条 `FEVER n%` → 窗口 `FEVER TIME` | FT / 狂热时间 | フィーバー（大字仍英文） | 피버（大字仍英文） | 有 | 条 `狂热 n%` / 窗 `狂热时间` | docs+client |

`BattleHud` 底栏另写 `HP n%`、`驱动 n%`、满条 `驱动满 · 点头像`。原作底中是**一条绿队血 + 一条粉 Fever**，Drive 不单独占底条百分比（Drive 在头像翅标 + 顶条敌 Drive 点）。切片多画了一条 Drive 条——语法能玩，铬和原作不一致。

---

## 2. 顶条（所有模式同一套）

录像可读（`ragna_hud.jpg`、`crop_inbattle.png`）：

| 位置 | 原作可见字 | 中 | 日（功能名，铬常仍英） | 韩 | docs | client | 状态 |
|---|---|---|---|---|---|---|---|
| 左 | `>> ×2 SPEED` / `×3 SPEED` | 倍速 | 倍速 | 배속 | 有 | `×1` / `×2`（无 SPEED 词、无 ×3） | ×3 need |
| 中上 | 拱形血 `HP cur/max` | HP | HP | HP | 有 | 只画填充 + `n%`，无 `HP` 前缀、无 cur/max | 数字格式 need |
| 中 | `n% HP OF FOE` | 敌方 HP% | 敵HP | 적 HP | 动效写了「89%」，未录这句原文 | 无 | **need** |
| 中 | 敌 Drive 三点 + 百分比 | 敌方驱动 | 敵ドライブ | 적 드라이브 | 有（三点） | 三点 + 左标 `驱动` | 百分比数字 need |
| 中 | 计时 `MM:SS` | 剩余时间 | 残り時間 | 남은 시간 | 有 | `00:00` | 有 |
| 中 | 帧上还看到 `BATTLE TIME` / `DO PAUSE`（OCR；黄签不是游戏字） | 暂停 | 一時停止 | 일시정지 | 写 PAUSE | 钮 `暂停` / 暂停中 `继续` | 英文 `PAUSE` 原文未进 loc 表 |
| 右 | `MANUAL` / 半自动 / `FULL AUTO` | 手动 / 半自动 / 全自动 | マニュアル / オート / フルオート | 수동 / 반자동 / 풀 오토 | 有 | `手动` / `半自动` / `全自动` | 有 |
| 主线中上 | 关卡名 + `PHASE n/m`（例 `第7關 第二次死亡` `PHASE 1/3`） | 阶段 | PHASE（日服也写 PHASE） | PHASE | 有 | `VfxPhaseBar` 仍输出英文 `PHASE n/m`；`BattleHud` 另写 `第n/2波` | 双写；波数写死 2 |
| Boss | 无 PHASE，一条百万血 | — | — | — | 有 | `VfxRagnaBar` 用 `阶段` 不当 PHASE | 模式切换未接线 |

暂停面板（切片）：`暂停` + `继续` + `回首页`（`PauseBoard.cs`）。原作暂停页全文未截到。

**Skip：** 场上帧**没有** SKIP / 跳过。进战前有连续战斗（韩 `연속전투`、繁中 `自動戰鬥`），那是大厅，不是场上钮。对话 SKIP 也不在战斗 HUD。完整游戏若要「跳过已通关」，还没源。标 **need / 未证实**。

---

## 3. 底栏五圆（操作面）

| 原作 | 中文对照 | docs | client | 状态 |
|---|---|---|---|---|
| 圆头像 + 充能环 | — | 有 | 有 | 有 |
| `TAP FULL` 黄浮字 | 点按已满 | 有 | `点按已满`（Vfx）/ `点按满`（Hud 顶标） | 并词 |
| 绿环 / `SLIDE SKILL READY` | 上滑就绪 | 有 | `上滑就绪` + 角标 `上滑` | 有 |
| 翅标 `DRIVE SKILL READY` | 驱动就绪 | 有 | `驱动就绪` + 角标 `驱动` | 有 |
| `COOL TIME n` 压在冷却头像上 | 冷却 | 有 | **无** | **need** |
| `60 MAX` + 短名 | 等级 / 满级 | 有 | 短名 + HP 数字，无 `MAX` | 等级标 need |
| 头像顶 Buff 英文：`Regen` `Vampirism` `ATK Stack` `Debuff Blast` `CRITICAL` `WEAK` | 再生 / 吸血 / 攻击叠加 / 减益爆破 / 暴击 / 弱点 | 动效表有 | `VfxBuffFloat` 已中文化：再生、吸血、攻击叠加、减益爆破 | CRITICAL/WEAK 头像小标 need |
| 绿条全队 HP%（帧上叠两行如 `13%` / `63%`） | 血量 | 有 | `HP n%` 一行 | 双行% 未复 |
| `FEVER n%` 绿条下粉字 | 狂热 | 有 | `狂热 n%` | 有 |
| 窗口改彩虹条 + `FEVER TIME` | 狂热时间 | 有 | `狂热时间` | 有 |
| WB：约 20 个小圆两排 | — | 有 | `VfxWorldBossBar` 有 `世界王`/`伤害`，底栏仍 5 人 | 20 头 **need** |

---

## 4. 打击叠字（不是一条 HUD）

Fever 同一帧可四层（`ragna_fever.jpg`、`kr_crit.jpg`、`kr_weak.jpg`）。

| 层 | 原作 | 中 | 日/韩场上 | docs | client | 状态 |
|---|---|---|---|---|---|---|
| 单 hit | 白/浅黄数字 + 属性小字 | 数字 | 同（数字不翻译） | 有 | `VfxDamagePopup` + 火/水/木/光/暗 | 有 |
| WeakPoint | 橙立体 `WeakPoint` + 大数字 | 弱点 | **英文不译** | 有 | `弱点`（`VfxWeakPoint`） | 有（词已换） |
| Critical | 红 `Critical` + 大红数字 | 暴击 | **英文不译** | 有 | `暴击` | 有（词已换） |
| Combo | `17 COMBO` + `383,040 DAMAGE` | 连击 / 伤害 | **英文不译** | 有 | `VfxComboBanner`：`n 连击` + `伤害`；**`PixelCombatFx` 仍写 `n COMBO`** | 双实现，英文残留 |
| 判定 | 紫 `PERFECT !`；绿 `GREAT !`；另有 Good / Bad | 完美 / 优秀 / 好 / 偏了 | **英文不译** | 有 Perfect/Great；Good/Bad 较少出镜 | Hud 戳：完美/优秀/好/偏了；`VfxJudge`：`完美 !` / `优秀 !` / `好`（无 Bad 卡） | Bad 卡 need |
| Perfect 附行 | `DAMAGE 150%` + `n% TO FEVER` | 伤害 150% / 距狂热 n% | 英文 | 有 | Judge：`伤害`+`150%`；狂热条用 `狂热`+`n%`，**不是** `TO FEVER` 句式 | 句式差 |
| MISS | 场上帧**未见**。Drive 最差档是 **BAD**（+8% Fever，×0.90），不是 miss | — | — | 无 | 无 MISS 字 | **不要发明 MISS** |

Drive 四档（视觉图鉴 / 深研报告，早期韩服 Fever 增量）：

| 档 | 伤害 | Fever | 原作字 | 切片字 |
|---|---:|---:|---|---|
| BAD | ×0.90 | +8 | `BAD`（少出镜） | `偏了` |
| GOOD | ×1.00 | +15 | `GOOD` | `好` |
| GREAT | ×1.20 | +30 | `GREAT !` | `优秀` / `优秀 !` |
| PERFECT | ×1.50 | +40 | `PERFECT !` | `完美` / `完美 !` |

---

## 5. 全屏切镜（按出现顺序）

原作大字在韩/日/国际服都是英文。技能名跟语言走。

| 顺序 | 原作 | 中文替换（切片） | 日/韩 | docs | client | 状态 |
|---|---|---|---|---|---|---|
| Boss 开场 | `THE MASTER OF DESIRE BOSS` + 爪影 + 底条 `WARNING · 名字` | `欲望之主` / `首领` | 标题仍英；名字本地化（`창기사 루인`） | 有 | `VfxBossIntro` | 有 |
| Slide | `IT'S SHOWTIME!!` + `SLIDE SKILL` + 技能名 + `RANK 5 LV 5/10` | `开战`（Showtime）/ Hud 戳 `开演` + 徽章 `滑步技能`/`上滑` | 大字英；韩技能名（만월）+ 底部署名 | 有 | `VfxShowtime` + `PixelCombatFx` | **开战 vs 开演 双词**；RANK/LV 行 need |
| Drive 起手 | `READY TO RUMBLE?` | Hud `就绪？`；Crush `准备` | 英文 | 有 | 有 | **就绪 vs 准备** |
| Drive 切镜 | `DRIVE CRUSH` + `DRIVE SKILL` + 技能名 | `碾压` + `驱动` | 英文 + 本地技能名 | 有 | `VfxDriveCrush` | 有 |
| QTE 钮 | 底中橙圆 `GOOD BUTTON` | `好`（`VfxGoodButton`）；Hud 旧钮仍写 **`GOOD`** + 副标 `驱动` | 英文 | 有 | 双钮 | **清掉 Hud 里残留 GOOD** |
| 敌 Drive | `WARNING !!` + `ENEMY DRIVE SKILL` | `警告` + `敌方全力`（Warning）/ Hud `敌方驱动` | 英文 | 有 | 双词 | 并词 |
| Fever 窗 | `FEVER TIME` | `狂热时间` | 英文 | 有 | 有 | 有 |
| 击倒 | 角色倒地，无统一 KO 大字 | 无字（`VfxKo` 闪崩） | — | 未强调 | 无 KO 字 | 可保持无字 |

韩 SHOWTIME 底部会滚一行技能说明（国际服通常不滚）。完整游戏若做韩文包才需要。

---

## 6. 进战前（还不算场上，但一局完整游戏要）

`crop_prebattle.png`（繁中国际服 wiki）+ `kr_lobby.jpg`：

| 原作 | 繁中帧 | 韩帧 | docs | client | 状态 |
|---|---|---|---|---|---|
| 队槽 | `5TH TEAM` / `LEADER` / `戰鬥力` | `10TH PARTY` / `LEADER` / `전투력` | 有 | 编队有战斗力，缺 TEAM n 铬 | 部分 |
| 编辑 | `編輯隊伍` | `파티 편집` | 有 | 有编队页 | 有 |
| 开始 | 大金剑 `戰鬥開始` | `전투 시작` | 有 | 关卡钮非这把剑 | **剑钮皮 need** |
| 连续 | `自動戰鬥`（黄签：脚本） | `연속전투` | 有 | 无 | **need** |
| 出现 | `出現Child` | `출전 적 차일드` | 有 | 无 | need |
| 奖励 | `可能獲得獎勵` | `획득 가능 보상` | 有 | 结算才有掉落 | 进战前预览 need |
| 预约 | `技能預約` | 技能 예약 / `SKILL RESERVE` | 有 | 无 N/S/E 五槽 | **need** |
| 体力 | `-25` 心 | 心 | 有 | 无 | need |
| 助战 | 好友列表 + 增益句 | `도전 횟수` 等 | 有 | 无 | need（可后做） |

日：バトル開始 / 連続戦闘 / 編成。未截到原图，按功能对齐。

---

## 7. 胜利 / 失败 / 结算

| 模式 | 原作 | 中 | 韩帧 | docs | client | 状态 |
|---|---|---|---|---|---|---|
| 主线胜/负 | 未截到统一 `VICTORY`/`DEFEAT` 大字 | — | — | 弱 | `GameRoot` `胜利`/`失败`；`ResultBoard`/`VfxStageClear` **`完成`/`失败`** | **胜利 vs 完成 双词** |
| Ragna 击杀 | 金环 `RAGNA:BREAK COMPLETE` + 左右 `擊` `滅` | 擊滅 | 同（漢字）+ `누적 피해량` `내 도전 횟수` `순위 보기` `전투 통계` `RANK 1/5` `Touch the screen` | 有 | 无这张皮 | **need** |
| Ragna 伤害榜 | `BOSS LEVEL` `Damage Dealt/Taken` `HP Received` `Total Damage` `RANK` `Close` `Home` `REMAINING TIME` | 关闭 / 主页 | — | 有 | `ResultBoard`：战斗结果、伤害总计、掉落、材料、回首页、下一关/再战 | 榜格式 need |
| 世界王结束 | `100TH HIT` + Boss 名 + 白字总伤（Boss 仍站着） | — | — | 有 | 无 | **need** |
| 通用结果钮 | `Close` / `Home` / `Touch the screen` | 关闭 / 主页 / 触摸屏幕 | 터치 | 有 | 回首页 / 下一关 / 再战 | 有（词不同） |

切片没有「跳过结算」。

---

## 8. 模式专用条

| 模式 | 原作字 | docs | client | 状态 |
|---|---|---|---|---|
| 主线 | `PHASE n/m` + 关卡名 | 有 | `PHASE` 芯片 + `第n/2波` | 并轨 |
| Ragna | 百万 `HP cur/max`；大厅 `BOSS LEVEL` `CALL` | 有 | `VfxRagnaBar` 阶段+HP | 大厅字 need |
| 世界王 | `WORLD BOSS TRIAL` `TRIAL READY` `Boss Info` `Challenge Count` `nTH HIT` | 有 | `VfxWorldBossBar`：`世界王` `伤害` | 大厅+20 头 need |
| PvP / 乱斗 | 预约屏 `SKILL RESERVE`；未进本任务场上逐帧 | 入口图有 | 无 | 后做 |
| 星云 / 困难 | 无战场录像；铬=主线 | 有说明 | 同主线 HUD | 不要另做 HUD |

---

## 9. 状态 / Buff 飘字（场上会看见）

切片 `VfxStatusIcons` / `VfxBuffFloat` 已有：沉默、毒、眩晕、睡眠、嘲讽、流血、无敌、净化、格挡、反击、灼烧、石化、失明、诅咒、禁疗、再生、吸血、攻击叠加、减益爆破、恢复。

原作头像顶常见英文：`Regen` `Vampirism` `ATK Stack` `Debuff Blast` `CRITICAL` `WEAK`。wiki Buff 页是中文名表，不是 HUD 截图。完整 loc 还缺：每条 Buff 的 **EN 原词 ↔ 中/日/韩** 对照（93 条名字在表里，场上显示规则未截全）。

---

## 10. 已在 docs vs 仍缺

### 研究页已经写清（不要再猜）

- 顶条：SPEED、MANUAL/FULL AUTO、PAUSE、拱血、PHASE、计时
- 底栏：五圆、TAP FULL、SLIDE/DRIVE READY、COOL TIME、FEVER / FEVER TIME、队 HP%
- 切镜：SHOWTIME、READY TO RUMBLE、DRIVE CRUSH、GOOD BUTTON、PERFECT 150%、TO FEVER、WARNING、THE MASTER OF DESIRE
- 叠字：WeakPoint、Critical、COMBO、DAMAGE
- 结算：RAGNA:BREAK COMPLETE、擊/滅、100TH HIT、Close
- 进战前：戰鬥開始、LEADER、技能預約、連続전투
- 自动三档含义（手动 / 半自动不放 Drive / 全自动连 Drive）
- Drive 四档倍率与 Fever 增量
- **没有 MISS**；Fever 不是第五技能槽

### 切片已经有替换词（中文铬）

`点按 / 上滑 / 驱动 / 连击 / 队长`，`狂热 / 狂热时间`，`完美 / 优秀 / 好 / 偏了`，`开战 / 开演 / 就绪？ / 准备 / 碾压`，`警告`，`暂停 / 继续 / 手动 / 半自动 / 全自动`，`完成 / 失败 / 胜利`，`欲望之主 / 首领`，`连击 / 伤害 / 弱点 / 暴击`，`好` 圆钮。

### 完整游戏还缺（HUD 字）

1. **统一词表**：开战≠开演、就绪≠准备、点按满≠点按已满、胜利≠完成、Hud `GOOD` 残留、`PixelCombatFx` 的 `COMBO` 残留、`VfxPhaseBar` 的英文 `PHASE`。
2. **原作顶条原文未进 loc：** `HP OF FOE`、`BATTLE TIME`、`PAUSE`、`SPEED`、`MANUAL`、`FULL AUTO`（半自动英文原词未拍死，wiki 只写了中文三档）。
3. **`COOL TIME n`、`60 MAX`、头像 `CRITICAL`/`WEAK` 小标。**
4. **SHOWTIME 的 `RANK n LV n/n` 行。**
5. **Judge 的 `n% TO FEVER` 句式、BAD 卡。**
6. **进战前：** 戰鬥開始剑、連続战斗、出現 Child、奖励预览、技能預約 N/S/E、LEADER 虚线标、体力心。
7. **Skip：** 场上未见到；不要先做跳过战斗。
8. **Ragna COMPLETE 擊滅皮、伤害榜三页签、WB `nTH HIT`、20 头像。**
9. **×3 SPEED。**
10. **日/韩 loc 包：** 切镜大字可继续英文；大厅钮和技能名要本地化。现有资料韩大厅较全，日 HUD 只有 GameNext 编号、没有逐词表。
11. **暂停页 / 失败页原作全文**（撤退？重试？）未截到。
12. **公式页不提供 HUD 字**——不要从 `formulas.md` 编按钮文案。

---

## 11. 建议发行用词（中文包，不抄原作英文标）

只作 loc 草案，尚未写进客户端统一表。

| 功能 | 建议中文 | 不要用 |
|---|---|---|
| TAP | 点按 | TAP FULL 原文 |
| SLIDE | 上滑 | IT'S SHOWTIME!! |
| DRIVE | 驱动 | DRIVE CRUSH / GOOD BUTTON |
| AUTO 普攻 | 连击 | AUTO 当按钮名 |
| LEADER | 队长 | LEADER 白标可保留作编队功能色，字用中文 |
| Fever 条/窗 | 狂热 / 狂热时间 | FEVER TIME 原文 |
| QTE 四档 | 偏了 / 好 / 优秀 / 完美 | PERFECT 原文；不要 MISS |
| QTE 钮 | 好 | GOOD |
| Slide 切镜 | 开演 | SHOWTIME |
| Drive 起手 | 就绪？ | READY TO RUMBLE? |
| 敌大招 | 警告 | WARNING !! |
| 弱点/暴击 | 弱点 / 暴击 | WeakPoint / Critical |
| 连段 | n 连击 + 伤害 | n COMBO / DAMAGE |
| 暂停/自动/倍速 | 暂停 · 手动/半自动/全自动 · ×n | SPEED / MANUAL 原文 |
| 主线波 | 阶段 n/m | 与 Hud「第n波」只留一个 |
| 胜负 | 完成 / 失败（或统一用 胜利） | VICTORY 原文；先消掉双词 |

日文包：功能名可用 タップ / スライド / ドライブ / フィーバー / リーダー；场上大字若要「像原作」可留英文，那是产品选择，不是资料缺口。韩文包同理：大厅用 전투시작 / 연속전투 / 파티 편집，切镜可留英文。

---

## 12. 对照图（帧文件名）

| 要核对的字 | 文件 |
|---|---|
| 顶条 HP / SPEED / MANUAL / FEVER | `battle_vids/crops/ragna_hud.jpg` |
| TAP FULL / DRIVE SKILL READY / Regen | `ragna_hud_slide.jpg` |
| 主线 PHASE / FULL AUTO | `battle_refs/crop_inbattle.png` |
| 底栏五圆 + FEVER 0% | `battle_refs/crop_hud.png` |
| SHOWTIME / SLIDE SKILL | `ragna_showtime.jpg`、`kr_showtime.jpg` |
| READY TO RUMBLE? | `ragna_ready.jpg` |
| GOOD BUTTON / DRIVE CRUSH | `drive_qte.jpg` |
| PERFECT / DAMAGE 150% / TO FEVER | `ragna_qte.jpg`、`ragna_perfect.jpg` |
| GREAT ! | `ragna_great.jpg` |
| WARNING !! | `ragna_warn.jpg` |
| FEVER TIME / COMBO / WeakPoint | `ragna_fever.jpg` |
| Critical | `kr_crit.jpg` |
| WeakPoint 单层 | `kr_weak.jpg` |
| COMPLETE 擊滅 | `kr_complete.jpg` |
| 伤害榜 Close | `ragna_result.jpg` |
| 100TH HIT | `wb_result.jpg` |
| Boss 海报 | `wb_intro.jpg`、`kr_boss.jpg` |
| 进战前 戰鬥開始 | `battle_refs/crop_prebattle.png` |
| 韩 전투시작 | `kr_lobby.jpg` |

纪念版 `mobile-archive/screenshots` 无战斗 HUD，不要从那里补这张表。
