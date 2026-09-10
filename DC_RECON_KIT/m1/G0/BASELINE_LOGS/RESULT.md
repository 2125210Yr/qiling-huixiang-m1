# M1-G2-CAPTURE-STATES — 本轮结果

日期：2026-09-10。**我方切片，不是原作 GT。**  
切片冒烟结果正文 = **PASS**。启动成功 ≠ 验收。无 GT。不报还原分。不发明 `GL_FINAL_VERIFIED`。未开 G3。M1 **未验收**。G2 **未过**。

## 用了哪条路径

**Editor PlayMode Vertical Slice Smoke**（有图形，非 `-batchmode` / 非 `-nographics`）。

只开一个 Editor。首次 pid 53064 卡在 `ExitedPlayMode` 编译失败（无窗口）；杀掉后重开 pid **44048**（`BatchMode: 0`，Tundra success，Loading completed 16.176s）。

触发：工程加载后写 `client/Temp/vs-smoke.request`（**2026-09-10 20:06:39**）→ `vs-smoke.running` → `[VS-SMOKE] entering play mode`。跑完 `EditorApplication.Exit`。

## 必须先读的结果

`client/Temp/vs-smoke.result.txt` 在 Editor 退出时被 Unity 清 `Temp` 删掉。同文在：

- `editor-playmode-20260910-capture-b.log` `[VS-SMOKE]`（约 20:06，含 `07_drive` / `08_fever` / `09_result`）
- `client/captures/vs-smoke.result.txt`（回填）
- 本目录与 `docs/reference/gl-shutdown-pve/our_slice/vs-smoke.result.txt`

第一行 `PASS`。`save-isolated=True`。`captures=` 含三张新图。

截图原件 mtime：

| 文件 | mtime | 看见（我方切片） |
|---|---|---|
| `07_drive.png` | 2026-09-10 **20:06:46** | Drive QTE「好」 |
| `08_fever.png` | 2026-09-10 **20:06:47** | Fever 激活 / 「完美」 |
| `09_result.png` | 2026-09-10 **20:06:51** | 结算「完成」 |

## 存档隔离

**生效。** 冒烟写 `client/Temp/vs-smoke-save/save.json`（隔离沙箱，随 Temp 清掉）。  
用户 `LocalLow\Resonance\契灵回响\save.json`：**未改**（仍 17:37:27 / 2249 bytes；`.bak` 仍 17:20:24）。未读改用户档内容。

## 仍缺

- primary GT：`docs/reference/gl-shutdown-pve/` 仍 **0 mp4**
- EditMode
- 逐帧对照、还原分、T26 / T27 / T28
- 不得把本轮切片 PASS 或补拍图当成原作回归 / M1 验收
