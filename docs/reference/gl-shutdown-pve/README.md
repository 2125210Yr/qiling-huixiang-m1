# GL shutdown PVE reference

把 **国际服停服前后期、普通 5 人 PVE** 的 mp4 放进这个文件夹。

Target: GL / pre-shutdown late including Ignition / mobile portrait / ordinary 5-person PVE.

**根目录仍 0 主 GT mp4。** `contrast/` / `rejected_not_primary/` / `supplementary/` 有对照或否决片，**不能**当 primary。本机 YouTube 族 TLS EOF，远程拉取 BLOCKED。B 站通道可用但仍无停服窗普通 5 人 PVE。硬停见 `DC_RECON_KIT/m1/G0/BLOCKED.md`；入库说明见 `LOCAL_PVE_MISSING.md`。

## 不要当普通基准

`docs/reference/gamekee/_combined/battle_vids/` 里已有的三份：

- `mJrT2conPCI` — GL Ragna only
- `Vdf4V693IcU` — KR Raid only
- `89jpoNqAwa8` — WB only

纪念版截图无战斗。2019 英文 5 人只标 `cross_era_gl`。

## 补充帧（不是 primary）

`M1-G2-SUPP-CUES` 从本地 Ragna 片抽出对照帧：`supplementary/ragna_gl/`（`full/` + `ui/` + `CUES.md`）。  
**仍 0 条普通 5 人 PVE mp4。** primary 仍 NEEDS_FETCH。

## Fetch log（历史，不再重试）

`FETCH_LOG.txt`：`M1-G0-GT-RETRY` 对 YouTube 族 TLS EOF。`M1-G0-GT-LOCAL` / `M1-G2-SUPP-CUES` 未再跑 yt-dlp。
