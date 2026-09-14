# G2 路 A 工程收敛 — 核心 API 契约（集成者冻结，供并行代理编译对齐）

基线：审查 SHA `ed7a9ba`；当前 HEAD 同一提交，AUDIT R01–R09 全部仍存在。  
共享文件 `BattleSim.cs` / `BattleHud.cs` / `GameRoot.cs` **只由集成者落盘**。其他代理写独立文件、测试或补丁文本。  
所有新常量均为 `DesignPlaceholder`（工程预览值），不是 GL 实测。

## 1. 命令入口（`Resonance.Battle/Core/BattleSim.Commands.cs`，partial）

```csharp
public enum CommandSource { Player, Auto, Replay, Fixture }
public enum BattleCommandKind { Tap, Slide, DriveBegin, DriveResolve, FeverTap, FocusEnemy, Pause, Resume, SetSpeed, SetAuto }
public enum CommandReject { None, NotInProgress, Paused, QtePending, NoQtePending, SlotInvalid, UnitDead,
    ActionLocked, Silenced, NotCharged, SlideOnCooldown, DriveNotReady, FeverNotActive, FeverThrottled,
    FeverBudgetExhausted, AutoOwnsInput, InvalidValue }
public struct BattleCommand { BattleCommandKind Kind; int Slot; DriveTiming Timing; int Value; CommandSource Source; }
public struct CommandResult { bool Accepted; CommandReject Reason; int Seq; int Tick; }
public sealed class CommandRecord { int Seq; int Tick; BattleCommandKind Kind; int Slot; DriveTiming Timing; int Value; CommandSource Source; bool Accepted; CommandReject Reason; }

// BattleSim
public readonly List<CommandRecord> CommandLog;
public CommandResult Submit(BattleCommand cmd);
public string RunHeader();          // seed / profile / data version / auto mode / speed / rules version
public int Seed { get; }
public const string RulesVersion = "g2-patha-2026-09-13";
```

规则：玩家手势、自动策略、回放全部经 `Submit`。`Auto == Full` 时 `Player` 来源的 Tap/Slide/DriveBegin/FeverTap 被拒 `AutoOwnsInput`（暂停/速度/自动切换不受限）。

## 2. Fever（主文件，集成者）

```csharp
public enum FeverEndReason { None, TimeUp, BudgetExhausted, BattleEnded }
public FeverEndReason LastFeverEnd;
public bool TryFeverTap(int slot);        // 手动：被点击且存活、未被控制的己方出手；返回 false 时 Submit 给出原因
public int FeverHitsLeft;                  // 已有
public float FeverLeft;                    // 已有
// BattleClockPolicy 新增
public float FeverMinHitIntervalSec = 0.2f;   // 节流（设计值 = 14s / 70）
public float FeverAutoTapsPerSec = 5f;        // 自动策略（设计值）
public int FeverBudget => FeverHitBudget;
```

手动模式不再有计时器自动出手；`Auto != Manual` 时由 `Tick` 以 `CommandSource.Auto` 生成 `FeverTap`，出手角色在存活己方中轮转（设计值）。

## 3. QTE / 时钟（主文件，集成者）

```csharp
public enum DriveResolveResult { Accepted, NoPending, NotInProgress, Paused }
public float QteElapsed { get; }
public float QteLimitSec { get; }          // Clocks.DriveQteTimeoutSec
public float QteRemaining { get; }
public DriveTiming LastDriveTiming;         // 最近一次已结算 QTE
public int LastDriveResolveTick;            // -1 = 无
public DriveResolveResult ResolveDriveChecked(DriveTiming timing);
public bool ResolveDrive(DriveTiming timing); // 终局/暂停/无待决 → false，无副作用
```

QTE 等待期间关卡倒计时按 `StageCountdownScalesWithSpeed` 缩放，不再借用 `DriveQteScalesWithSpeed`。HUD 不再持有自己的 `_qteT`。

## 4. 随机

```csharp
public bool ForceNoCrit;            // 仅显式公式夹具
public bool Deterministic { get; set; }   // 兼容旧测试：读写 ForceNoCrit；新代码不要用
```
种子始终决定随机流；正常预览入口不再设 `Deterministic = true`。

## 5. 效果能力与生命周期

- `UnitState.ChargeSpeedMul` 计入 `ChargeHaste` 与 `ChargeSpeed`。
- `UnitState.SkillLocked => Has(Silence)`：Tap/Slide/Drive/FeverTap 拒 `Silenced`；普攻不受影响（设计值）。
- `StatusInst.ShieldLeft`：护盾数额跟随状态实例；到期/驱散释放剩余；`UnitState.Shield` = 各实例合计（只读）。
- `EffectDef.DurationSec <= 0` 表示“直到消耗/驱散”，不再次 tick 即消失。
- `MaxStack > 1` 且同 `Id`：叠层 `min(Stacks+1, MaxStack)` 并刷新时长；不同 Id 同 Group 走原 SourceTier 替换。
- `status.dispel` 实现：按 `Group`（空则全部非永久）移除并释放护盾。
- `EffectDef.Trigger`（新字段，字符串）：`on_action` / `on_hit_taken` / `periodic`，可用 `|` 组合；空时 Poison → `on_action|on_hit_taken`，Bleed → `on_hit_taken`（设计值）。`periodic` 需 `PeriodSec > 0`。
- `EffectOpcodes` 由 Effect-Capability 代理扩展为 opcode + kind + 参数级能力表；`Catalog` 载入外部内容时未支持即报错。

## 6. 数值证据分离（`FormulaProfile.cs` / `DamageMath.cs`，Numeric-Provenance 代理）

```csharp
public enum FormulaEvidence { Unknown, DesignPlaceholder, HistoricalCandidate, Measured }
// FormulaResult
public bool Computed { get; }              // 有可用数值
public FormulaEvidence Evidence { get; }
public bool Measured => Evidence == FormulaEvidence.Measured;   // 目前任何分支都不应为 true
public int RequireInt();                   // 依 Computed
```
`BattleSim.TryResolveCombat` 改用 `Computed`；事件日志记录 `Evidence`。GL_UNKNOWN 严格路径继续不产出数值。

## 7. 测试落点

- .NET：`tools/BattleSim.Tests/G2Review*.cs`（新文件，不改旧断言以求绿）。
- Unity 自然流程：`client/Assets/Scripts/Resonance.App/Debug/NaturalPlayRuntime.cs` + `client/Assets/Editor/Smoke/NaturalPlaySmoke.cs`，只通过 EventSystem 指针事件操作；request 文件 `client/Temp/natural-play.request`，结果 `client/captures/natural-play.result.txt`，截图前缀 `np_`。
- 产物统一 `DC_RECON_KIT/m1/G2_REVIEW_20260913/artifacts/`。
