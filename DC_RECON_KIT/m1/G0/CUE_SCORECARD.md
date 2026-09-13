# 为什么还原分一直是 0，以及怎么处理

合同：`04_ACCEPTANCE_AND_TESTS.md`。**本表不是 90 分验收，也不是 `GL_FINAL_VERIFIED`。**

## 一直报 0 的原因（不是切片没做）

验收把「能跑」和「像原作」拆开。`FormulaProfiles.fidelity_pass_without_gt = false`。合同还原分要同时满足：

| 门槛 | 要什么 | 现在 |
|---|---|---|
| T27 叠图 | 同画幅叠加，中位误差 ≤ 屏宽 1% | 片是 MediaRecorder **~288×640**，`reference_viewport` = `NOT_MEASURED` |
| T28 时序 | 输入/命中/Drive/Fever 误差 ≤ 2 参考帧 | GT 叠层 + 冒烟探针（SHOWTIME 1.473s）。**不是** 30fps 同机位条，输入→命中未测。**仍 BLOCKED** |
| T14–T20 数值 | 完整输入 + 观测输出的误差报告 | GL 公式仍 `UNKNOWN`；教程 6 EXP / 73 GOLD **不准当式** |
| 总分 ≥ 90 且每域 ≥ 85 | 战斗规则 35 / UI 20 / VFX 15 / 数值 20 / 流程 10 | 数值域和 UI 几何域都还没测 |

所以：**身份对齐再多，合同还原分也必须是 0。** 之前每轮 STATUS 都写 0，是守这条门，不是说 HUD 一个字都没对上。

功能核是另一列：155 夹具绿、PlayMode 切片冒烟 PASS。那一列**不是 0**，也**不能**拿来填还原分。

## 处理办法：四层分开记，不再用一个 0 盖全部

| 层 | 问的是什么 | 本闸能不能涨 | 涨了算不算 M1 |
|---|---|---|---|
| **身份** | 这是不是同一类 cue（SHOWTIME ≠ Fever；PHASE ≠ 击破） | **能**。P0/P1 已够做身份表 | 否。只是 G2 表现闸 |
| **布局** | 中心/边界像素误差 | **不能**，除非换高分辨率原片或重抓 ≥720p | 否。T27 |
| **时序** | 和参考差几帧 | GT 叠层时长已量（见 `TIMING_DIFF.md`）。**不能**关 T28，除非再录我方同机位条并比输入/命中 | 否。T28 |
| **数值** | 伤害/CD/EXP/星级式 | **不能**，除非有可复现输入输出，禁止 KR/JP 顶替 | 否。T14–T20 |

合同还原分 = 后三层都过才许离开 0。身份层单独计数，避免「做了等于没做」。

## 身份层（P0 `aSbBuFD12HY` + P1 Robin；窄屏 OCR 噪声已目视）

`ID` = 身份对上（文案或结构）。`LAY`/`T`/`NUM` 全是 0。

| 键 | ID | 证据 | 我方 | 仍缺 |
|---|---|---|---|---|
| slide | **ID** | P0 t360 `IT'S SHOWTIME!!` + `SLIDE SKILL` + RANK；r50 RANK 1 / r62+PVP5 RANK 7 皆可 LV 10/10 | `VfxShowtime`；RANK 走技能槽，空为 `—` | 全屏立绘切镜；RANK/LV 数字要外部稿 |
| tap | **tip** | P0 t352 满条后 tap **或** slide | 双时钟仍按 G1 冻结；Tap 短拳不再用 Slash 斩 | 孤立 tap 手势仍缺 GT |
| charge | **tip** | 同一 Skill Gauge | HUD `SKILL GAUGE` | 秒数 |
| drive select | **ID** | P0 t365/t380：点亮头像 + `DRIVE SKILL READY`；tip「tap the icon」。无弹板 | 状态旗 + 肖像 `DRIVE SKILL READY`；已停 `DRIVE SELECT` 板 | 教程指尖不进切片；坐标仍工程 |
| qte | **ID** | Robin t55 `PERFECT!` / `DAMAGE 150%` / `N% TO FEVER`。**13% 是 0→40 中间帧**，不是第二套式 | `VfxJudge` + 金币 `PRESS BUTTON` + `N% TO FEVER`；`DAMAGE 150%` / `HERE IS A POINT!!` / Hard `Skill Addition` 不进普通切片每场 QTE | 普通 PVE QTE 仍未见；150% / +40 不得当式 |
| fever gauge | **ID** | `FEVER n%`；Robin 中段 40% | 底栏 `FEVER n%` | — |
| fever window | **ID** | P0 t440/t442：彩虹条 + `FEVER TIME` + `12.50`/`10.83`；底栏仍 `FEVER 0%`。t452 肖像 `WEAKPOINT`（不是 DRIVE SKILL READY）。t435 窗长 tip 14s；t445 仍是 `FEVER TIME!!` 提示板 | 彩虹 overlay + leftover；底栏 `FEVER n%`；Fever 窗肖像改 `WEAKPOINT` 优先 | 坐标/T27；WEAKPOINT 是否只盖正在普攻的那张脸未测 |
| hit | **ID** | `15,300` / `72,730` 千分位 | `VfxDamagePopup` 逗号 | 暴击/弱点帧距 |
| death | **neg** | P0 t64–t90 **无击破字** | 已停印敌死亡章；`06f_kill` 残影+数字、无章 | 我方倒下未见 |
| wave | **ID** | t65 关卡名 + `PHASE 2` 心形；溅射期 HUD 仍 `PHASE 1/3`；场上无人 | `WavePreview` 藏站立单位 + HUD 停上一阶段 | 敌 HP 0→100 切换帧未测；切片仍 2 波 |
| result | **ID** | t391 `勝利`+`victory`+tap → t396 `CLEAR!!`+★；t470 才 LEVEL UP | 先闪后 CLEAR 板；LEVEL UP 延迟揭 | 星级/EXP/GOLD 式；花环立绘 |
| speed/auto | **ID** | `>> Xn SPEED` / `FULL AUTO` / `PAUSE` | `>> Xn SPEED` / `> FULL AUTO` / `\|\| PAUSE` | 半自动未见 GT |
| clock chrome | **ID** | `mm:ss BATTLE TIME` / `N% ENEMY HP TOTAL` | `BATTLE TIME` / `ENEMY HP TOTAL` | 画幅 |

身份：**10 ID + 2 tip + 1 neg** / 13 键。这不是 10/13 还原通过。

## 要把合同还原分从 0 拉起来，只能做这三件（缺一仍是 0）

1. **高分辨率 GT**  
   原机或 yt-dlp 原片，竖屏 ≥720p，放进 `DC_RECON_KIT/docs/reference/gl-shutdown-pve/` 根目录。现在的 288×640 remux **做不了 T27**。本机 YouTube TLS 不通。你这台 Chrome 今天下午已经能播 P2（1280×720）；把文件丢进上述根目录即可，不要让我从登录会话里自己拉。P0 原片 ≥720p 才是 T27 解锁件。

2. **同机位时序条**  
   GT 侧 30fps 叠层时长已写入 `TIMING_DIFF.md`（草稿）。还要录我们的切片（有图形 Editor，禁止 `-nographics`），并补输入/命中/数字列。缺我方条，T28 仍 BLOCKED。

3. **数值样本**  
   已知面板的 Tap/Slide/Drive 一段，或承认「M1 不验数值域」。合同写明数值权重 20；不验就要你书面把该域移出 M1，不能事后遮罩。

## 两条路：用户已选当前走路 A，路 B 整包延期

签字 2026-09-12：「这些我们暂时待定……合同你写清楚后面补」。补件：`DC_RECON_KIT/m1/G1/DEFERRED_SUPPLEMENT.md`。

**路 A — 当前工程（进行中）**  
验收「身份表无自相矛盾 + 切片可录」。只用已入库 P0/P1。合同还原分、T27、T28、公式继续 0。M1 **仍不算过**。身份层可以涨，不拿 0 分假装没进展。

**路 B — 合同 90 分 M1（`DEFERRED_NOT_REMOVED`）**  
上面三件（高清 GT / 同机位条 / 数值样本）**后面补**，不从分母删除。补完之前声称 90% 或 `GL_FINAL_VERIFIED` 都是假的。

暂停：公开关键词搜、YouTube/Archive 拉取、模拟器补录、用户投放升格。解禁见补件 §4。不升级 Unity，不另起通用卡牌项目。
