# G0 / G1 / G2 STATUS

| Token | Value |
|---|---|
| TARGET | `TARGET_FROZEN_GT_SEARCHING` |
| CONTRACT | `CONTRACT_FROZEN` |
| MILESTONE | `M1 IN_PROGRESS`（**未验收**） |
| G0 | `PASSED_WITH_GT_OPEN` |
| G1 | `PASSED_CONTRACT_FROZEN` |
| G2 | `IN_PROGRESS_CORE_SLICE`（**未过**） |
| PlayMode | `PASSED_SLICE_SMOKE`（`M1-G2-CAPTURE-STATES` 已补拍 `07_drive` / `08_fever` / `09_result`；结果正文 PASS，存档已隔离。**不是** M1 验收） |
| EditMode | `NOT_RUN` |
| primary GT | `CANDIDATE_NEEDS_FETCH` / `BLOCKED`（`docs/reference/gl-shutdown-pve/` **0 mp4**） |
| GL formulas | `UNKNOWN` |
| `GL_FINAL_VERIFIED` | **forbidden / not created** |
| fidelity / 还原分 | **0 / not claimed** |
| 功能核 | 内部一致性切片可跑；**不是**原作回归 |

## 功能核 vs 还原分（本闸）

独立 QA（`QA_EXECUTOR.md`，`M1-G2-QA-EXECUTOR`）已确认：默认夹具绿；执行器会改盾/控制/毒；Fever 不分走 Tap 系数；无 GT。`M1-G2-RESMOKE` PlayMode 切片冒烟 **PASS**（日志 Drive/Fever）。**不重做**战斗公式，**不采信** 夹具绿或切片 PASS = M1 过。

| 面 | 状态 | 说明 |
|---|---|---|
| 功能核 | 夹具自洽 | 已知 opcode 施加盾/Stun/Freeze/嘲讽/毒；Fever 用 `FeverChannelAtkCoef`/`FeverChannelFlat`；`GL_UNKNOWN` 不结算伤；`M1-G2-RESMOKE` 默认 `dotnet test` 0 失败 / 126 |
| 还原分 | **0** | 无 primary GT。T 还原通过数 0。不得报 numeric fidelity / T27 / 90% |
| 功能 IMPLEMENTED（内部一致性，非还原） | T01 / T03 / T10 / T15 / T18 | 同 seed 哈希、独立 SlideCd、毒=行动/受击、通道不混、Slide 非均匀 RNG |
| 还原 IMPLEMENTED | **无** | 全部 T 还原列 BLOCKED |

## 本轮（2026-09-10 M1-G2-CAPTURE-STATES）

Editor PlayMode Vertical Slice Smoke（有图形）。只开一个 Editor（最终 pid **44048**，`BatchMode: 0`）。`vs-smoke.request` 20:06:39。日志 `[VS-SMOKE]` **PASS**，含 `07_drive` / `08_fever` / `09_result`。截图 mtime 20:06:46–20:06:51。`save-isolated=True`（`client/Temp/vs-smoke-save/save.json`）。用户 LocalLow `save.json` 仍 17:37:27，未改档。Editor 已退出。`client/Temp` 结果原件随退出被清；正文从日志回填到 `captures/` 与归档。

**我方切片，不是 GT。** 未宣称 M1 验收。未发明 `GL_FINAL_VERIFIED`。未开 G3。G2 仍未过。还原分仍 0。

## 此前（2026-09-10 M1-G2-RESMOKE）

重跑 Editor PlayMode Vertical Slice Smoke（有图形，非 `-nographics`）。先礼貌退出旧 Editor pid 27792。只开一个 Editor（pid **13000**，`BatchMode: 0`）。启动成功 ≠ 通过。已读新结果：`client/Temp/vs-smoke.result.txt` **PASS**（mtime `2026-09-10 17:20:24`）。日志：`drive qte slot=0` → `drive perfect fever=60` → 第二次 QTE → `fever=0 active=True` → `result 胜利`。

截到：Home / 契灵 / 编队 / 详情 / 战斗「开战」（Drive≈0%）。**没截到** Drive QTE / Fever / 结算画面（冒烟只在开战闪屏拍一帧）。窗口补拍 0 张。副本在 `BASELINE_LOGS/` 与 `our_slice/`（**我方切片，不是 GT**）。

未宣称 M1 验收。未发明 `GL_FINAL_VERIFIED`。未开 G3。G2 仍未过。还原分仍 0。

## 对照（2026-09-10 M1-G2-SUPP-CUES）

从本地 `mJrT2conPCI.mp4` 抽 Ragna 补充帧到 `docs/reference/gl-shutdown-pve/supplementary/ragna_gl/`。cue 草稿见该目录 `CUES.md`。`GL_PVE_GROUND_TRUTH.json` 分层：`supplementary` ≠ `primary`。**primary 仍 `NEEDS_FETCH`。** 未改 Unity。未跑 yt-dlp。未宣称还原通过。Raid/WB 只记模式差。纪念版大厅不能当战斗坐标。`battle_refs` HUD 轮廓 `REPORTED` + 混区。

## 此前（2026-09-10 M1-G2-FIX-DRIVE-SMOKE）

当时 PlayMode 仍 FAILED（未重开 Editor）。代码侧已修：开战 Hold 不再被 Full Auto/2s 看门狗放行，五人切片序列能打出 Drive/QTE/Fever。已被 `M1-G2-RESMOKE` 重跑。未宣称 M1 验收。

## 此前（2026-09-10 M1-G2-PLAY-RECORD）

把 PlayMode 从「怕写 Library 所以 NOT_RUN」推进到**真实跑了一次**。未升级 Unity。未 reset 脏树。未发明 `GL_FINAL_VERIFIED`。未开 G3。未重跑 `dotnet test`。未宣称 M1 验收。115/116 绿仍不是还原。

**路径：** Editor PlayMode Vertical Slice Smoke（有图形）。Win64 包 `2026-08-28` 相对今日 `catalog.json` / `BattleSim.cs` 过旧，未当成本轮冒烟。启动前无第二个 Editor；玩家包 pid 38412 仍在，未抢。

命令：

```
D:\Unity\Hub\Editor\6000.3.23f1\Editor\Unity.exe -projectPath F:\天命之子\client -logFile F:\天命之子\DC_RECON_KIT\m1\G0\BASELINE_LOGS\editor-playmode-20260910.log
```

无 `-batchmode` / `-nographics`。pid 27792。`BatchMode: 0`。许可 OK。会写 `client/Library`（预期）。启动冲掉预写 `Temp/vs-smoke.request`；加载后再写，日志 `[VS-SMOKE] entering play mode`。

**已读结果（启动成功 ≠ 通过）：** `client/Temp/vs-smoke.result.txt` → `FAIL drive never fired`，`phase=Result`，`tap 0` / `slide 1` / `result 胜利`。无 Fever。无结算截图。

截图已目视，非黑帧（大厅翡翠兔 / 契灵 / 编队 / 详情 / 战斗开战且 Drive≈0%）。实战仍 `JP_LEGACY_EMPIRICAL`。

| 项 | 路径 |
|---|---|
| 原始结果 | `client/Temp/vs-smoke.result.txt` |
| 原始截图 | `client/captures/` |
| Editor 日志 | `DC_RECON_KIT/m1/G0/BASELINE_LOGS/editor-playmode-20260910.log` |
| 归档 + 说明 | `DC_RECON_KIT/m1/G0/BASELINE_LOGS/` 、 `docs/reference/gl-shutdown-pve/our_slice/`（**我方切片，不是 GT**） |

Editor 仍占用 `client/`（pid 27792 + 其子 AssetImportWorker）。不要再开第二个 Editor。

## 此前（2026-09-10 M1-G2-CATALOG-OP）

`CatalogJson.Serialize` 技能/效果写出 `op`（与 Load 对称）；roundtrip 测走 temp、不写回 `catalog.json`。默认 `dotnet test` 0 失败 / 116。未宣称验收。

## 此前（2026-09-10 M1-G2-SMOKE-GATE）

闸门收尾，未扩 G3 / 20 人 / 新模式。未 reset 脏树。未升级 Unity。未宣称 M1 验收。

1. **catalog `op`：** `client/Assets/Content/catalog.json` 是合成夹具，原先技能/效果无 `op`。`ReadSkill`/`ReadEffect` 空 opcode 会失败关闭。已给**已有**合成技能与效果补 opcode，与 `Catalog.cs` builtin 一致（`DamageMath.ChannelOpcode(type)` / `EffectOpcodes.ForKind(kind)`）。未改角色名、技能名、系数。`CatalogJson.Serialize` **仍不写** `op`（本轮未改序列化）。
2. **PlayMode：** 当时 `NOT_RUN`。已被 `M1-G2-PLAY-RECORD` 推进为 **FAILED**（见上）。
3. 验收缺口清单：`DC_RECON_KIT/m1/G0/M1_UNGATE.md`。**不写 G3 入口。**

## PlayMode / Vertical Slice Smoke（`M1-G2-PLAY-RECORD` 原始记录）

| 项 | 事实 |
|---|---|
| Unity.exe | `D:\Unity\Hub\Editor\6000.3.23f1\Editor\Unity.exe` |
| 活动 Editor | pid **27792** 占用 `client/`（`Library/EditorInstance.json`）；子进程为 AssetImportWorker，不是第二份工程 |
| 玩家包 | `Resonance.exe` pid 38412 = `client/Builds/Win64`（2026-08-28，过旧）；本轮未用 |
| `client/Library` | 已写（预期） |
| 命令 | 见上；无 `-batchmode` |
| 触发 | 工程加载后写 `client/Temp/vs-smoke.request` → `vs-smoke.running` |
| 本轮结果 | `client/Temp/vs-smoke.result.txt` **已读**：`FAIL drive never fired` |
| 本轮截图 | `client/captures/`（已目视，非黑帧）；副本在 `BASELINE_LOGS/` 与 `our_slice/` |
| 判定 | **`FAILED`**。启动成功 ≠ 通过。无 Fever / 结算图。不得伪视觉通过 |

旧文件 `client/Builds/Win64/Temp/vs-smoke.result.txt` 是既往玩家包产物，**不是**本轮 Editor PlayMode。

## 此前（2026-09-10 M1-G2-EXECUTOR）

已知 opcode 按目录 kind 结算（伤害通道 / 盾 / Stun·Freeze / 毒·流血 / 嘲讽）；Fever 走 fever 通道、不借 Tap `AtkCoef`/`FlatPower`；`ClockPolicy.json` / `EffectSchema.json` 与代码对齐。测量值仍 UNKNOWN。M1 未验收。

## 此前（2026-09-10 M1-G0-GT-LOCAL）

只读本地搜录像。**未改 Unity。未再对 YouTube 跑 yt-dlp。未逐帧。**

- `GL_PVE_GROUND_TRUTH.json` 增加 `local_candidates`：38 条本地命中，**0 条可用主 GT**
- 缺件：`LOCAL_PVE_MISSING.md`（把国际服停服前后期、普通 5 人 PVE mp4 放进 `docs/reference/gl-shutdown-pve/`）
- `docs/reference/gl-shutdown-pve/` 仍 **0 mp4**（仅 `README.md` + `FETCH_LOG.txt`）

远程拉取 **BLOCKED**（本机 YouTube 族 TLS EOF，已证实）。等待本地入库。

## 此前（2026-09-10 战斗核 / GT 重试）

战斗核 REPAIR（未 REPLACE，未 checkout 旧扁平文件）：

- `client/Assets/Scripts/Resonance.Battle/Core/DamageMath.cs` — `NotMeasuredCode`；Auto ≠ Tap 通道；`Resolve` 对 `GL_UNKNOWN` 返回未测；KR Tap 二次衰减仅对照
- `client/Assets/Scripts/Resonance.Battle/Core/EffectOpcodes.cs` — 已知 opcode 表；未知抛 `UnknownOpcodeException`
- `client/Assets/Scripts/Resonance.Battle/Core/BattleEventLog.cs` — 可哈希事件日志；`BattleHudState` 多域（非单枚举）
- `client/Assets/Scripts/Resonance.Battle/Core/BattleSim.cs` — SlideCd 独立时钟；毒=on_action/on_hit_taken（禁止每秒 DoT）；可变 `Allies` 容量；默认 `Profile=GL_UNKNOWN`；未知 opcode → `Failed`
- `client/Assets/Scripts/Resonance.Battle/Core/Definitions.cs` — `SkillDef.Opcode` / `EffectDef.Opcode`
- `tools/BattleSim.Tests/BattleSimTests.cs` — 去掉 `catalog.json` 写盘
- `tools/BattleSim.Tests/M1CoreSliceTests.cs` — SlideCd / Auto / opcode / 毒 / 编队 / 哈希 / 禁终式

UI / 表现（未动共享 scene/prefab/meta）：

- `client/Assets/Scripts/Resonance.App/Battle/BattleHud.cs` — 竖屏 HUD 多域状态；Tap/Slide 手势已分离；Slide 就绪跟 SlideCd；布局标 `NEEDS_REFERENCE`（禁止伪 T27）
- `client/Assets/Scripts/Resonance.App/Battle/ICharacterPresentation.cs`
- `client/Assets/Scripts/Resonance.App/Battle/CharacterPresentationAdapter.cs`
- `CombatFeel.cs` / `VfxRouter.cs` / `BattleFighter.cs` / `CharacterPresenter.cs` — cue 跟事件走；伤害仍在 `BattleSim` 结算，不绑动画结束；保留现有骨骼

GT：

- `docs/reference/gl-shutdown-pve/` 已建，**仍 0 mp4**（`M1-G0-GT-RETRY` 换通道仍失败）
- `DC_RECON_KIT/m1/G0/GL_PVE_GROUND_TRUTH.*` — 重试命令/错误已记；oembed 只确认标题；2019 英文 5 人仍 `cross_era_gl`；未逐帧

未改：Unity 版本、URP、`TARGET.json`、共享 scene/prefab/meta、角色立绘/具体技能文案。

## 测试原始输出（本轮，改 catalog 后必跑）

```
dotnet test tools/BattleSim.Tests/BattleSim.Tests.csproj --nologo --verbosity minimal

BattleSim.Tests -> F:\天命之子\tools\BattleSim.Tests\bin\Debug\net6.0\BattleSim.Tests.dll
F:\天命之子\tools\BattleSim.Tests\bin\Debug\net6.0\BattleSim.Tests.dll (.NETCoreApp,Version=v6.0)的测试运行
正在启动测试执行，请稍候...
总共 1 个测试文件与指定模式相匹配。

已通过! - 失败:     0，通过:   115，已跳过:     0，总计:   115，持续时间: 150 ms - BattleSim.Tests.dll (net6.0)
```

退出码：`0`。**115 绿是夹具自洽，不是原作回归，不能升格为 M1 验收。**

PlayMode：当时 `NOT_RUN`。已被 `M1-G2-PLAY-RECORD` 推进为 **FAILED**。

## BLOCKED

- **`docs/reference/gl-shutdown-pve/` 0 mp4。** 国际服停服前后期、普通 5 人 PVE 实机仍缺。说明见 `LOCAL_PVE_MISSING.md`。远程 YouTube 族 TLS EOF，**不再**本机 yt-dlp。
- primary GT 仍 `CANDIDATE_NEEDS_FETCH` / `BLOCKED`。无 GT 不得报 UI 坐标 pass，不得报 numeric fidelity pass。
- GL 公式通道：`UNKNOWN`。对照仅 JP/KR legacy。无 `GL_FINAL_VERIFIED`。
- PlayMode：切片冒烟 **`PASSED_SLICE_SMOKE`**（`M1-G2-RESMOKE` 已读 PASS）。**无** Drive/QTE/Fever/结算截图。EditMode 仍 `NOT_RUN`。证据见 `BASELINE_LOGS/` 与 `our_slice/`（我方切片，不是 GT）。无 GT 不得报还原通过。
- 毒触发帧距、SlideCd 秒数：GL=`UNKNOWN`（U014）。
- Catalog `Load`/`BuildBuiltin` 仍全局换表、无并行锁。
- 默认编队 5；无 20 人/前后排。

## 不得 90%

M1 未验收。G2 未过。无 GT 不得报还原分。PlayMode 本轮切片冒烟 **PASS**（有图形；结果文件已读）。不得把大厅立绘、「开战」图或「胜利」日志写成原作回归。无 Drive/QTE/Fever/结算截图。

验收还缺什么见 `M1_UNGATE.md`。**M1 未验收，不写下一阶段 G3 入口。**
