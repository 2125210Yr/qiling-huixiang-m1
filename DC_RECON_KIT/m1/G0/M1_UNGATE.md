# M1 验收还缺什么

任务 `M1-G2-RESMOKE` 更新 PlayMode（2026-09-10）。前序 `M1-G2-PLAY-RECORD` / `M1-G2-SMOKE-GATE`。**M1 未验收。G2 未过。不是 G3 入口。**

用户 2026-09-12：硬缺件 **后面补、不从分母删**。补件 `../G1/DEFERRED_SUPPLEMENT.md`。当前工程走路 A（身份 + 可跑切片）。GT 搜暂停。还原分仍 0。

独立 QA 已确认功能核内部一致（默认 115 绿；执行器改盾/控制/毒；Fever 不借 Tap 系数）。这些**不能**升格为验收。

## 硬缺件（不入库则无法验收）

1. **primary GT 录像：** handoff P0+P1 已落 `DC_RECON_KIT/docs/reference/gl-shutdown-pve/` 根目录（`aSbBuFD12HY` ~584s、`hgqXY5M9gFk` ~74s，MediaRecorder 竖屏）。P2 Eternal Vow 仍 bot-gate。仓库根 `docs/reference/gl-shutdown-pve/` 仍 0 mp4（不要混路径）。`contrast/` / Ragna / Raid / WB 不得顶替。YouTube 族 TLS 仍 BLOCKED，不要再对本机 yt-dlp YouTube。
2. **逐帧对照：** 我方竖屏条 `strip_20260912g/`（1080×1920 源 / 360×640 JPG）+ GT 并排 `overlay/`。输入→数字我方 **0.000s**，GT 列 UNKNOWN。**仍不是 T27/T28**：几何对不上；无音效；教程 3 人 vs 切片 5 人。还原分保持 **0**。
3. **PlayMode / EditMode：** PlayMode 切片冒烟 `M1-G2-CLOCK-SMOKE`（`editor-playmode-20260911e`）已读 **PASS**。新增 `06g_speed`（×2）/ `06h_auto`（全自动）；仍有 `06a`–`06e` / `07` / `08` / `10` / `09`。Fever 无选择板。`06d` 仍与「击破」同帧。这是**我方切片**，不是逐帧 GT。EditMode 仍 `NOT_RUN`（工程 0 个 UTF）。无 GT 不得伪称视觉还原通过。T26–T32 仍 `NOT_RUN`/`BLOCKED`。**不是 M1 验收。**

## 仍 UNKNOWN（有 GT 之前不能闭）

- GL Tap / Slide / Auto / Fever / Drive 实际伤害公式
- SlideCd 秒数、毒触发帧距（U014）
- 参考画幅（U010）→ 禁 T27
- Ignition / 增幅组合（U006）

## 功能核已在、仍不是验收

- 已知 opcode 施加路径（盾 / Stun / Freeze / 嘲讽 / 毒入状态）
- Fever 不再读 Tap `AtkCoef`/`FlatPower`
- `ClockPolicy.json.slide_cd_present` / `EffectSchema.json.poison.implemented_in_engine` 与代码对齐
- 合成夹具 `catalog.json` 已补 `op`（与 builtin 一致）。`Serialize` 仍不写 `op`
- 默认 `dotnet test tools/BattleSim.Tests`：本轮 0 失败 / 115（夹具自洽）

## 明确不在本闸、也不当作验收条件去扩

- G3、20 人/前后排、新模式
- 升级 Unity
- 发明 `GL_FINAL_VERIFIED`
- 用 115 绿或无 GT 的 HUD 坐标报还原分
