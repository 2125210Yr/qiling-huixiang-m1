# UiStateMap

版本：`1.0-G1-2026-09-09`。状态：`CONTRACT_FROZEN`。画幅：手机竖屏。像素几何：**NOT_MEASURED**（等 primary GT）。

本波文档任务：`M1-G2-UIMAP-DOC`（2026-09-10）。只补**已实现页面路径**。路径 ≠ 还原完成。**T27 未宣称。** 未改 `client/`。

营销图不可当坐标。三份旧录像可作模式差异/气氛，不可当普通 5 人 PVE 热区真值。

## M1 战斗必测状态
`BattleIdle` `ChargingReady` `Tap` `Slide` `DriveSelect` `Qte` `Fever` `Controlled` `DeathOrWave` `Result`。另需基础入口/返回，从真实客户端流程进切片。

十态分母保留。坐标一律 **`NEEDS_REFERENCE`**。禁止用大厅立绘、纪念版截图、或「胜利」日志写成状态还原 / T27。

延期补件（用户 2026-09-12）：T27 / 画幅测量 **后面补**，坐标不从分母删除、不得标已测。详见 `DEFERRED_SUPPLEMENT.md`。

| 状态 | 实现路径 | 坐标 |
|---|---|---|
| BattleIdle | `client/Assets/Scripts/Resonance.App/Battle/BattleHud.cs` | `NEEDS_REFERENCE` |
| ChargingReady | 同上 | `NEEDS_REFERENCE` |
| Tap | 同上 | `NEEDS_REFERENCE` |
| Slide | 同上 | `NEEDS_REFERENCE` |
| DriveSelect | 同上 | `NEEDS_REFERENCE` |
| Qte | 同上 | `NEEDS_REFERENCE` |
| Fever | 同上 | `NEEDS_REFERENCE` |
| Controlled | 同上 | `NEEDS_REFERENCE` |
| DeathOrWave | 同上 | `NEEDS_REFERENCE` |
| Result | `client/Assets/Scripts/Resonance.App/UI/ResultBoard.cs` | `NEEDS_REFERENCE` |

战斗暂停 overlay（非十态分母）：`client/Assets/Scripts/Resonance.App/UI/PauseBoard.cs`。同样 `NEEDS_REFERENCE`。

入口 / 返回（不从分母删）：Home → 编队 / 关卡 → 开战；`StageBoard` 回编队 / 回首页；`ResultBoard` / `PauseBoard` 回首页。路由在 `GameRoot.cs`（只记，本任务未改）。

## 后续页（列名≠完成）
Home、角色列表/筛选、角色详情、养成入口、编队/Leader、关卡选择、队伍确认、掉落、装备/Soul Carta。WB 20 人页单列，不在 M1 验收分母里删掉，只是本切片不测。

| 契约名 | 有无实现 | 路径 | 备注 |
|---|---|---|---|
| Home | 有 | `HomeHud.cs` `LobbyRail.cs` `EmeraldLobby.cs` `LobbyStage.cs` | **大厅，不是战斗页。** 纪念版大厅图不能当战斗坐标。 |
| CharacterList | 有 | `TeamBoard.cs`（`DrawRoster`）`ArchiveBoard.cs`（图录筛选） | 两路列表。筛选在图录。 |
| CharacterDetail | 有 | `InspectBoard.cs`（`G1Screen=CharacterDetail`） | 立绘缺失可遮罩；布局 `NEEDS_REFERENCE`。 |
| Growth | 有 | `InspectBoard.cs`（升级/突破）`IgnitionBoard.cs` | 养成入口，不是独立关卡页。 |
| FormationLeader | 有 | `TeamBoard.cs`（`DrawTeam`） | `DrawMemorial` 只是编队排版函数名，**不是**纪念版大厅，**不是**战斗页。 |
| StageSelect | 有 | `StageBoard.cs` `WavePreview.cs` | 普通/困难 + 波次预览。`ChapterMap.cs` 有文件、未挂。 |
| PartyConfirm | 无独立页 | — | 开战 CTA 在 `WavePreview` / `StageBoard`，不是单独确认页。 |
| Drops | 部分 | `ResultBoard.cs`（掉落一行） | `LootPopup.cs` 有文件、未挂。 |
| EquipmentSoulCarta | 部分 | `InspectBoard.cs`（底四装备井） | 无独立 Soul Carta 页。`LibraryBoard` 是书库残章，不是装备。 |
| WorldBoss20 | 无页 | — | 不从分母删除。`DeepBoard` / `NebulaBoard` 不是 20 人 WB。`VfxWorldBossBar` 是 VFX，不是页。 |

上表路径均在 `client/Assets/Scripts/Resonance.App/UI/`（Home 四件与 Inspect/Ignition 同目录）。有路径 ≠ 对照通过。

## 其余已挂 Board（不在 G1 后续页名单，只标路径）

| 页 | 路径 |
|---|---|
| Archive | `ArchiveBoard.cs` |
| Library | `LibraryBoard.cs` |
| Deep | `DeepBoard.cs` |
| Nebula overlay | `NebulaBoard.cs`（深途通关后叠层，不是 WB） |
| Summon | `SummonBoard.cs` |
| Daily | `DailyBoard.cs` |
| Shop | `ShopBoard.cs` |
| Mail | `MailBoard.cs` |
| Achieve | `AchievementBoard.cs` |
| Title | `TitleBoard.cs` |
| Friend | `FriendBoard.cs` |
| Rest | `RestBoard.cs` |
| Food | `FoodBoard.cs` |
| Pvp | `PvpBoard.cs` |
| Tutorial | `TutorialBoard.cs` |
| Costume overlay | `CostumeBoard.cs` |
| Settings | `SettingsModal.cs`（非 Board） |
| Boot | `BootSplash.cs`（非 Board） |

未挂：`ChapterMap.cs`、`LootPopup.cs`。有文件 ≠ 已接线。

## 纪念版大厅 ≠ 战斗页
纪念版客户端无战斗 HUD。下列**不得**填十态坐标、不得当 T27 参考：

- 本工程大厅：`HomeHud` / `LobbyRail` / `EmeraldLobby` / `LobbyStage`
- 本地静帧：`docs/reference/mobile-archive/`、`docs/reference/ui-lock/`

`TeamBoard.DrawMemorial` 不是纪念版大厅。

## 测量规则
每页：reference_id、region/date、内容区、归一化位置、锚点、层级、遮罩、字号比、描边、切片边框、热区、弹窗、返回。未知留空。禁止圆角 SaaS 改稿。立绘缺失可遮罩，必须报遮罩面积。

## 本波
无原生界面逐帧测量包。G1 冻结的是**状态清单与测量空位**，不是坐标复原完成。G2 只把已实现页面对上 Board / HUD 路径。战斗坐标仍 `NEEDS_REFERENCE`。**不宣称 T27。**
