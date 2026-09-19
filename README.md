# 契灵回响 / Project Resonance

总目录的分类入口、保留资料和可回收内容见 [00_整理说明.md](00_整理说明.md)。原项目路径保留兼容，以下开发说明仍适用。

内部名 Resonance。Unity 工程在 `client/`（junction 到 `F:\Resonance\client`）。

## 根目录怎么分

| 文件夹 | 放什么 |
|---|---|
| `client/` | **游戏工程**：场景、脚本、Resources、打包输出 `client/Builds/` |
| `dist/` | **给人玩的包**：`windows/Resonance.exe`、`android/契灵回响.apk` |
| `launch/` | 启动脚本：电脑 exe、装手机 |
| `docs/` | 设计笔记、调研、状态 |
| `天命之子数据/` | 给人看的手册：视觉 / 数值 / 动效 HTML（不进发行包） |
| `tools/` | 测试与离线工具，不进游戏包 |
| `art/` | 源立绘（若有）。进游戏的图在 `client/Assets/Resources/Art/` |
| `单机dc1.1整合包/` | 原作资料，**禁止打进发行包** |
| `_sandbox/` | 临时分析，已 gitignore |
| `reference/` | 空占位，资料在 `docs/reference/` |

## 代码（`client/Assets/Scripts`）

### Resonance.Battle — 规则与数值，无 Unity UI

- `Core/` 战斗模拟、伤害公式、枚举、定义
- `Content/` 25 人格与关卡表
- `Progression/` 等级 / 突破 / 装备
- `Save/` 本地存档
- `Loop/` 自动清章测试环

### Resonance.App — 画面与操作

- `Core/` 进游戏、换页（`GameRoot`）
- `Battle/` 战场 HUD、角色表现、手感
- `Characters/` 立绘、剪影、纸片层（层文件已不用在首页）
- `UI/` 色板、精灵、窗口适配
- `Debug/` 截图 / 冒烟，发行时可忽略

### Editor

- `Build/` Windows / Android 打包
- `Smoke/` 编辑器无头冒烟

## 立绘

首页只读 `Resources/Art/Characters/C001/presenter.png`。  
切开的头发/刀在 `C001/Layers/`，不参与首页显示。

## 常用入口

- 电脑玩：`launch/在电脑上看.cmd`
- 测战斗：`dotnet test tools/BattleSim.Tests/BattleSim.Tests.csproj`
