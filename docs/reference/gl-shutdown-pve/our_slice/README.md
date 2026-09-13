# 我方切片（不是原作 GT）

本目录只放 **契灵回响 / Resonance 我方 Vertical Slice** 的运行截图与结果。

**不是** 国际服停服前后期普通 5 人 PVE 真值。  
**不得** 当作 `docs/reference/gl-shutdown-pve/` 的 primary GT。  
`gl-shutdown-pve/` 根目录仍应只有原作 mp4（当前 0）。

来源任务：`M1-G2-CAPTURE-STATES`（2026-09-10）。覆盖 `M1-G2-RESMOKE` 的无 Drive/Fever/结算图副本。

路径：Editor PlayMode Vertical Slice Smoke（Unity 6000.3.23f1，非 batch / 非 nographics）。  
结果正文：`vs-smoke.result.txt` **PASS**（PlayMode 写入约 20:06；`client/Temp` 随 Editor 退出被 Unity 清掉，正文从 `editor-playmode-20260910-capture-b.log` 的 `[VS-SMOKE]` 段回填）。  
截图 mtime：`07_drive.png` 20:06:46、`08_fever.png` 20:06:47、`09_result.png` 20:06:51。

存档隔离：**生效**。`save=F:\天命之子\client\Temp\vs-smoke-save\save.json`，`save-isolated=True`。用户 `LocalLow\Resonance\契灵回响\save.json` 仍为 2026-09-10 17:37:27 / 2249 bytes（`.bak` 仍 17:20:24）。未改用户档。

截图（已目视；仍是我方切片，不是 GT）：

| 文件 | 内容 |
|---|---|
| `01_home.png` | 大厅 |
| `02_roster.png` | 契灵一览 |
| `04_team.png` | 5 人编队 |
| `03_inspect.png` | 角色详情 |
| `06_battle.png` | 战斗开战 |
| `07_drive.png` | Drive QTE（场上「好」） |
| `08_fever.png` | Fever 激活（「完美」判定叠在场上） |
| `09_result.png` | 结算「完成」 |

**不是** M1 验收。无 `GL_FINAL_VERIFIED`。G2 未过。还原分仍 0。

这些 png 是契灵回响我方切片，**不能**挪到本目录的上一级当 GT。
