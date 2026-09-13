# GT 片源交接（给 Cursor — 不要再搜，按 URL 直下）

## 为什么你「找不到」
不是 YouTube/B站没货。常见翻车原因：
1. 用中文/过宽关键词搜（Destiny Child PVE / 天命之子 关服）→ 被故事切片、Ragna、饭制淹没
2. YouTube 本机 TLS EOF → 搜/列表失败，误判为「没有」
3. 把 contrast/rejected 目录里的片当成「全库没有 primary」
4. 过滤过严（只要「shutdown ordinary 5p」字面标题）→ 真实片标题从不这么写

**立刻停止关键词搜索。** 用下面直链下载。

## 直链（按优先级）

### P0 — 窗内主候选
- https://www.youtube.com/watch?v=aSbBuFD12HY
- 落盘：`aSbBuFD12HY_GL_EN_Gameplay_Part1_202308.mp4`
- 说明：Destiny Child Gameplay English Version Part 1（Aug 2023 EN 窗）

### P1 — Global ND boss 短片
- https://www.youtube.com/watch?v=hgqXY5M9gFk
- 落盘：`hgqXY5M9gFk_GL_ND_Robin_Boss.mp4`
- 说明：Global Final Boss ND stage 8 ROBIN

### P2 — Eternal Vow N/H（较大，480p 即可）
- https://www.youtube.com/watch?v=IRDqNAhKKr4
- 落盘：`IRDqNAhKKr4_GL_EternalVow_NormalHard_480p.mp4`

## 落盘位置（硬）
`F:\天命之子\DC_RECON_KIT\docs\reference\gl-shutdown-pve\` **根目录**
不要放进 contrast / rejected / supplementary。

## 本机命令
```powershell
$dest = "F:\天命之子\DC_RECON_KIT\docs\reference\gl-shutdown-pve"
New-Item -ItemType Directory -Force -Path $dest | Out-Null
yt-dlp -f "bv*[height<=480]+ba/b[height<=480]/b" --no-playlist -o "$dest\aSbBuFD12HY_GL_EN_Gameplay_Part1_202308.%(ext)s" "https://www.youtube.com/watch?v=aSbBuFD12HY"
yt-dlp -f "bv*[height<=480]+ba/b[height<=480]/b" --no-playlist -o "$dest\hgqXY5M9gFk_GL_ND_Robin_Boss.%(ext)s" "https://www.youtube.com/watch?v=hgqXY5M9gFk"
```
YouTube TLS 失败时：换代理/`--force-ipv4`，或改用已装好的下载器；**不要**改回去关键词搜索。

## 完成后
1. ROOT 出现至少 1 个上述 mp4
2. 写 `FETCH_LOG.txt`
3. 更新 STATUS：primary GT 不再是 0
4. 继续 G2/M1，禁止开 G3
