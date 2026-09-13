# 不做 Live2D · 先把游戏做能玩

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 竖屏一章可玩：首页静帧全身 → 25 人编队 → 关卡预览 → 点按/上滑/驱动/Fever → 结算写本地存档。不做 Cubism、不拧网格、不改 1.1 包。

**Architecture:** 继续用现有 `Resonance.Battle`（公式、25 人表、成长）和 `GameRoot` 路由。立绘只走静帧 `FitInParent`。桌面窗口锁 9:16（已有 `PhoneDisplay`）。本轮把壳收成「像一款竖屏游戏」，不重写战斗内核。

**Tech Stack:** Unity 6.3 UGUI、`Resonance.App` / `Resonance.Battle`、xUnit `tools/BattleSim.Tests`、Win64 `Resonance.exe`。

---

## 现在代码里已经有什么

| 块 | 状态 | 文件 |
|---|---|---|
| TAP/Slide/Drive 公式 + 测试 | 有，约 48 个 Fact | `client/Assets/Scripts/Resonance.Battle/Core/DamageMath.cs`、`tools/BattleSim.Tests/BattleSimTests.cs` |
| 25 人 5×5 表 | 有 | `Catalog.BuildBuiltin`、`catalog.json` |
| 成长：等级/突破/燃起 6 档/好感/四装备/预约 T·S | 有 | `Progression/Growth.cs`、`GameRoot.DrawInspect` |
| 普通 12 + 困难 12 + 波次预览 | 有 | `Catalog.MakeChapter`、`DrawStage` |
| 战斗 HUD：血条、驱动条、Fever、QTE、手动/半自动/全自动 | 有 | `Battle/BattleHud.cs`、`BattleSim` |
| 竖屏窗口 9:16 | 有 | `UI/PhoneDisplay.cs` |
| 首页立绘 | **已改静帧** `AddFittedStandee`，不再挂 `Live2DIdle` | `Characters/CharacterPresenter.cs` `DrawStage` |
| Live2D / 分层纸娃娃 | 代码还在，本轮冻结不用 | `Live2DIdle.cs`、`LayeredPresenter.cs` |
| 立绘资源 | 仅 C001 有 presenter；其余像素替身 | `Resources/Art/Characters/C001/` |

缺口不是「没有游戏」，是壳还像工具界面：首页居中「出战」、身份块压在人身上、24 人没有立绘、Inspect 控件挤、战斗场上仍是像素人。

---

## 本轮不做

- Cubism / `.moc3` / 网格呼吸 / 拆层
- 解包 1.1 apk、pck、jar、连 SQL
- 星云 2048、抽卡、商店、PvP、温泉
- 25 张完整原画（只留坑和 C001 静帧；其余用统一占位）

---

## 文件职责

| 文件 | 本轮改什么 |
|---|---|
| `client/Assets/Scripts/Resonance.App/Core/GameRoot.cs` | 首页去掉居中出战；战斗入口只在关卡；契灵页加出战 |
| `client/Assets/Scripts/Resonance.App/Characters/CharacterPresenter.cs` | `DrawStage` 只静帧全身；禁止再走 `AddLive` |
| `client/Assets/Scripts/Resonance.App/UI/PhoneDisplay.cs` | 保持 9:16 窗口，不要全屏拉扁 |
| `client/Assets/Scripts/Resonance.App/Battle/BattleHud.cs` | 五人点按/上滑区域可点、驱动 QTE 可见 |
| `client/Assets/Editor/WindowsBuild.cs` | 窗口模式 9:16，不要 FullScreenWindow |
| `tools/BattleSim.Tests/BattleSimTests.cs` | 不改公式；可加「章节可清」若还没有 |
| `client/Builds/Win64/` | 编完打开 exe 验收 |

---

### Task 1: 冻结 Live2D，首页只静帧全身

**Files:**
- Modify: `client/Assets/Scripts/Resonance.App/Characters/CharacterPresenter.cs`
- Modify: `client/Assets/Scripts/Resonance.App/Core/GameRoot.cs` `DrawHome`

- [ ] **Step 1:** 确认 `DrawStage` 只调用 `AddFittedStandee`，不要 `AddLive` / `LayeredPresenter`。

现有代码已接近，保持这样的框：

```csharp
rt.anchorMin = new Vector2(0.06f, 0.12f);
rt.anchorMax = new Vector2(0.94f, 0.96f);
// FitInParent + 2:3 立绘，头必须在框内
```

- [ ] **Step 2:** `DrawHome` 去掉居中「出战」和左下「选关」。首页只留：棋盘、队长静帧、右上设定、底栏六键。身份块移到不挡脸的位置（左下、底栏之上），或只留名字一行。

```csharp
void DrawHome()
{
    CharacterPresenter.CheckerFloor(Root(), _built);
    CharacterPresenter.Embers(Root(), _built, 8);
    var lead = _save.PartyIds[Mathf.Clamp(_save.LeaderSlot, 0, 4)];
    var def = Catalog.MustChar(lead);
    _built.Add(CharacterPresenter.DrawStage(Root(), def, _save.GetUnit(lead).SkinId));
    LeftLabel(def.Name, 36, VisualTokens.TextPrimary, new Vector2(0.06f, 0.14f), true);
    GhostBtn("设定", new Vector2(0.90f, 0.94f), () => Show(ScreenId.Settings));
    Nav();
}
```

- [ ] **Step 3:** `Live2DIdle.AddLive` 和 `LayeredPresenter` 本轮不删除文件，但没有任何屏再调用。

- [ ] **Step 4:** 编译 Win64，打开 exe：首页能看见焰刃**整张脸和脚**，窗口是竖屏，不是 16:9 拉满。

验收：截图 `01_home.png` 头完整。

---

### Task 2: 信息架构对齐「壳」——出战只在关卡

**Files:**
- Modify: `GameRoot.cs` `DrawCharacters`、`DrawStage`、`Update` 空格开战

- [ ] **Step 1:** 契灵页底部「编队」旁加「出战」→ `Show(ScreenId.Stage)`。  
- [ ] **Step 2:** 空格/回车开战只在 `Stage`（不要从 Home 直接跳进战斗）。  
- [ ] **Step 3:** 关卡页已有普通/困难、12 格、敌人预览、「战斗开始」——保留。困难未解锁时按钮灰色。

验收：Home 不能一键进战斗；关卡页能选关并开战。

---

### Task 3: 战斗能打完、操作看得懂

**Files:**
- Modify: `Battle/BattleHud.cs`（只改可见性，不改公式）
- Test: `tools/BattleSim.Tests/BattleSimTests.cs` 已有 `MvpLoop.TryClearChapter` 的调用则跳过

- [ ] **Step 1:** 跑测试：

```
dotnet test "F:\天命之子\tools\BattleSim.Tests\BattleSim.Tests.csproj" --nologo
```

期望：全绿。失败先修战斗核，再动 UI。

- [ ] **Step 2:** 确认 `BattleHud` 底栏五人可点（点按）和上滑（已有 PortraitGesture 则接上）。驱动满条出现 GOOD 钮。Fever 有全屏闪。暂停/倍速/自动可用。

- [ ] **Step 3:** 实机：全自动打完第 1 关，结算「胜利」且 `clearedN` 增加。

验收：不看代码也能分清点按、上滑、驱动、Fever。

---

### Task 4: 25 人能选、没图也不崩

**Files:**
- Modify: `CharacterPresenter.Draw` / `DrawTile` / `DrawChip`
- 不强制生 24 张立绘

- [ ] **Step 1:** 没 `presenter.png` 时用现有 `PixelStandIn`，格子上必须有名字 + 元素色 + 职业。  
- [ ] **Step 2:** 图录/契灵 `DrawElementRoleGrid` 25 格都能点进详情。  
- [ ] **Step 3:** 编队五槽能换人、设队长，默认队 `C001,C007,C010,C003,C005`。

验收：点开 C025 不报错；换队长后首页静帧跟着换（没图就像素人）。

---

### Task 5: 详情页能养成，不挡操作

**Files:**
- Modify: `GameRoot.DrawInspect` / `DrawWells`

- [ ] **Step 1:** 详情仍含：四装备井、燃起 6 点、预约 5 格、LV/突破/好感。  
- [ ] **Step 2:** 控件不要盖住立绘脸：装备和燃起放在下半屏（y &lt; 0.40），立绘框收在上半。  
- [ ] **Step 3:** 技能弹层已有点按/上滑/驱动/队长预览伤害，保留。

验收：改 LV 和装备后战力数字变；返回编队不丢存档。

---

### Task 6: 出包验收

**Files:**
- Modify: `client/Assets/Editor/WindowsBuild.cs` — `fullScreenMode = Windowed`，不要再设 FullScreenWindow
- `CaptureRuntime` 若仍在：Home / 契灵 / 关卡 / 战斗 各拍一张

- [ ] **Step 1:** Unity `-executeMethod Resonance.EditorTools.WindowsBuild.BuildAndExit`  
- [ ] **Step 2:** 打开 `client/Builds/Win64/Resonance.exe`  
- [ ] **Step 3:** 手过一遍：首页（有头）→ 契灵 → 编队 → 关卡 → 打一关 → 结算

验收清单：

1. 窗口竖屏，不铺满 16:9 显示器  
2. 首页焰刃头、脚都在画面里  
3. 没有 Live2D 拧图  
4. 能从关卡进战斗并胜利写存档  
5. 25 格都能点  

---

## 之后再开（不在本 plan）

- 25 人静帧立绘（生图，一张一张，不拆 Cubism）  
- 战斗场上用立绘代替像素  
- Cubism 切件  

---

## 自检

- 规格：不做 Live2D、先把游戏做完一章 → Task 1–6 覆盖。  
- 无 TBD。  
- 公式文件本轮不改，避免把已过的 2182/2166 测碎。
