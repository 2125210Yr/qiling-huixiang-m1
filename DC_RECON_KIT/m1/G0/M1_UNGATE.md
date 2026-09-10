# M1 验收还缺什么

任务 `M1-G2-RESMOKE` 更新 PlayMode（2026-09-10）。前序 `M1-G2-PLAY-RECORD` / `M1-G2-SMOKE-GATE`。**M1 未验收。G2 未过。不是 G3 入口。**

独立 QA 已确认功能核内部一致（默认 115 绿；执行器改盾/控制/毒；Fever 不借 Tap 系数）。这些**不能**升格为验收。

## 硬缺件（不入库则无法验收）

1. **primary GT 录像：** 把国际服停服前后期、普通 5 人 PVE 的 `.mp4` 放进 `docs/reference/gl-shutdown-pve/`。现在 **0 mp4**（仅 README + FETCH_LOG）。见 `LOCAL_PVE_MISSING.md`。不要等本机 yt-dlp（YouTube 族 TLS EOF，远程拉取 BLOCKED）。
2. **逐帧对照：** 入库后再做 Drive / QTE / Fever / 受击 / 死亡 / 换波 / 结算。现在未逐帧。无 GT 则还原分保持 0。
3. **PlayMode / EditMode：** PlayMode 切片冒烟 `M1-G2-RESMOKE` 已读 **PASS**（Drive/Fever 在结果日志里）。有 Home/编队/Inspect/Battle「开战」截图，**仍无** Drive/QTE/Fever/结算图。EditMode 仍 `NOT_RUN`。无 GT 不得伪称视觉还原通过。T26–T32 仍 `NOT_RUN`/`BLOCKED`。我方切片在 `docs/reference/gl-shutdown-pve/our_slice/`，**不是** primary GT。**不是 M1 验收。**

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
