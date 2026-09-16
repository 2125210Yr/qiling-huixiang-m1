# a82801b 固定提交复审：G2 路 A

审查日期：2026-09-16  
仓库：2125210Yr/qiling-huixiang-m1  
分支：m1-gt6-review  
审查提交：a82801b80fd81fb4f8ff30cf45b72b30fcd63521  
对照基线：1b2ca8e4ff56d9f0986e158fc97b50498f735cb3

## 判定

**NEEDS_FIX。保留已修复的入口、门禁、实际关卡身份和自然操作脚本；本轮只收口验证配置语义、可重放时间线、准确开局记录和对应运行证据。**

不是重新做 G0/G1，不开 G3，不重建战斗框架，不重新实现一套记录器。精确 GL 数值、像素/逐帧还原等原有延期仍保留，不把工程验证通过当成原作还原通过。

## 审查边界

实际通过 GitHub 连接读取固定提交的关键源文件、原始 TRX 尾部、控制台日志、自然操作结果、分场命令及失败读回报告。当前运行环境没有 dotnet/Mono/C# 或 Unity 命令，因此没有独立编译、执行 C#、运行 Unity 或回放。没有修改远程仓库。

提交的变更列表包含大量运行产物，其接口返回不能替代全仓审计。本报告是指定调用链及前轮问题的定向复审，不声称每个文件、每条执行路径均已审计。截图中说明未推送的录像/构建二进制，未在本轮审看或运行。

下文“原件记录”指仓库保存的运行结果；“源码确认”指实际调用链；“反例”指需本地执行的验证，不伪称已运行。

## 一、上一轮 A1–A4 复核

| 旧问题 | 本次核查 | 当前边界 |
|---|---|---|
| A1：摘要与 TRX 冲突 | 新 a1-1b2ca8e.trx 的 ResultSummary=Completed；245 total、244 executed、244 passed、0 failed；同跑控制台 EXIT_CODE=0，另有一项 Unity 合同 Skip | 纠正了旧报告配套问题。该跑绑定 1b2ca8e，而非后续全部 a82801b 源码 |
| A2：合法替代没有接到 Unity | VerificationCatalog.Apply 在克隆技能表上真正调用 ApplyNamedSubstitute；basic 原件记录 Player Slide/Tap 成功 | 主接线问题已修；但克隆效果遗漏目标字段，见 C1 |
| A3：按 ID 而非实例识别关卡 | BattleSim 暴露 ActiveStage；ResolveSelectedStage 优先实例 StageDef，才回退全局查找 | 原源码问题已修，不再要求重做身份框架；本轮未独立执行对应反例 |
| A4：Unity 自然流程未执行 | basic、最新 fever、auto 已有实际结果文件；不是 NOT_RUN | 文件读回仍 Match=False，且构建/录像未独立验收，不能放行 Path A |

### 原始运行结果

- `artifacts/tests/a1-1b2ca8e.trx`：RunId `a457d5a0-4d49-4492-b868-a50fb82349d8`，blob `4a1739ee6144c0857553a802b6898c586aeb1dfc`。对应日志是 `artifacts/a1-dotnet.log`。
- `20260914T152459-np_basic_v1/natural-play.result.txt`：首行 PASS；session `np-20260914T152600`；第一场 Victory，Player Slide/Tap，NEXT 后第二场再 HOME。
- `20260914T183658-np_fever_v1/natural-play.result.txt`：首行 PASS；session `np-20260914T183818`；三次 Player Perfect、Player FeverTap 槽位 0 和 1，第一场 Victory，NEXT/HOME。
- `20260914T153700-np_auto_v1/natural-play.result.txt`：首行 PASS；session `np-20260914T153840`；HUD Full→Manual，存在 Source=Auto 的成功技能命令，随后 HOME。此项验证模式切换和自动施法，不代表自动模式所有行为已穷尽验证。

这些结果标注 `pointer=UnityEngine.EventSystem`、`os_touch=NOT_CLAIMED`、`provenance=DESIGN_PLACEHOLDER`。不能改称真机触摸验证或原作数值验证。最新结果中 Home 后 `ever=False` 属于第二场重开，不可据此否定第一场 Fever。

## 二、C1 / P1：验证效果克隆会改变技能目标阵营

### 源码位置

- `client/Assets/Scripts/Resonance.Battle/Content/VerificationCatalog.cs`：`CloneEffects()`、`Apply()`。
- `client/Assets/Scripts/Resonance.Battle/Content/Catalog.cs`：`CreateBuiltinTables()`、`PartySkills()`、`Install()`。
- `client/Assets/Scripts/Resonance.Battle/Core/Definitions.cs`：`EffectDef`。
- `client/Assets/Scripts/Resonance.Battle/Core/Enums.cs`：`TargetSide.FromRule=0`。
- `client/Assets/Scripts/Resonance.Battle/Core/TargetSemantics.cs`：`Rule()`、`Side()`。
- `client/Assets/Scripts/Resonance.Battle/Core/BattleSim.cs`：`ApplyLinkedEffect()`、`PickEffectTargets()`。

`VerificationCatalog.CloneEffects()` 复制 Id/Opcode/Kind/Magnitude/Duration/Stack/SourceTier/Group/Trigger/Period，却没有复制 `HasTarget`、`Target`、`Side`。新实例的 Side 回到 FromRule，HasTarget 回到 false。Install 直接发布候选，不补回这些字段。

明确例子：builtin `burst_atk` 声明 Side=Ally；`C001_drive` 的伤害选择器是 HighestAtkEnemies，链接 burst_atk。进入验证副本后，burst_atk 的 Side 变成 FromRule。TargetSemantics 继承伤害技能的敌方规则，ApplyLinkedEffect 因而选择敌方池。

**由当前源码可推导：这条验证路径可能在造成伤害后给存活敌方加攻，而不是给己方加攻。**具体哪些敌人存活、加到几个目标由实际状态决定，本审查没有在 Unity 现场运行。相同遗漏还会破坏 taunt 的 Self 等显式选边。

这不是 GL 数值未知，也不是允许的“只移除 Dot 链接”的命名替代。验证副本意外改变了已声明的语义；roster=ok 只检查能力，不会发现同一种已支持效果被发给错误阵营。

### 最小修复与验收

统一复用完整效果复制方法，或显式保留全部 EffectDef 字段；只有具名替代允许改变其明确声明的字段。先保存 builtin 原件，再执行 VerificationCatalog.Apply，断言 Side/HasTarget/Target 与原件一致且原件未被修改。随后通过完整技能入口验证 C001 Drive 的己方增益、伤害技能附带盾、Self 嘲讽。

不要靠调整敌方攻击/血量掩盖这个问题，不要删除效果或关闭门禁来通过测试。

## 三、C2 / P1：真实界面的停顿不在回放输入中

### 已提交的失败原件

`DC_RECON_KIT/m1/REVIEW_1b2ca8e/artifacts/natural-play/fight1-readback.txt`：

| 项目 | 记录 |
|---|---|
| Match / Ok | False / False |
| NamedOpeningDiff / VersionDiff | null / 0 |
| CommandDiff / Unconsumed | 0 / 0 |
| 首分歧 | 第 12 个事件：原件 tick 69 的 dmg.auto，回放为 tick 40 |
| 最终 TickIndex | 191 对 163 |
| Drive | 58 对 68 |
| TimeLeft | 111.933 对 109.933 |
| 事件数 | 100 对 124 |

对应第一场命令：tick19 Pause/Resume，tick24 SetSpeed=2，tick33 Slide，tick81 Tap。没有演出停顿起止命令。

### 源码确认的机制

`BattleHud.DrainCasts()` 收到己方 Slide 后调用 `BeginShowtime(..., hold:true)`。后者直接写 `battle.HoldSim=true`；`TickCutHold()` 用 `Time.unscaledDeltaTime` 倒计时后直接写回 false。

`BattleSim.Tick()` 在检查 Hold 之前已递增 TickIndex；Hold 分支会阻止关卡时间、充能和普攻推进。`BattleReplayer.ReplayCore()` 只重投已记录的外部命令并调用 Tick，没有 BattleHud 及其 Hold 起止。

因此真实界面中“算作 tick，但战斗时钟没有走”的区间，在纯模拟回放中没有对应记录或确定性的核心派生行为。这个缺口与 Slide 后首个普攻提前的原件相吻合，是应优先验证和修复的根因链路。**本报告不声称已经独立证明所有 17 个状态差异都只来自这一原因。**

### 最小修复

保留需要的技能停顿；将影响模拟的停顿交给核心，以明确政策、固定 tick 和事件维护，HUD 只展示。若必须保留外部结束控制，该控制也必须显式记录并重放，不能继续直接写共享字段而不记输入。

不要为了通过旧文件硬塞“延迟 29 tick”，不要删除 TickIndex/EventDiff/TimeLeft 的比较，也不要把回放重新挂到 Unity 动画帧率上。

旧文件缺少停顿记录时，应保留其失败及缺失信息。修复后重新录制同一操作路径生成新带；不手改旧文件、不通过忽略差异把旧失败变绿。

### C2b / P2：点敌方锁定也绕过命令入口

`BattleHud.SpawnEnemies()` 的 BindFocus 回调调用 `GameRoot.TryFocusEnemy()`；GameRoot 直接调用 `_battle.TryFocusEnemy(slot)`，没有 Submit。现有 `BattleCommandKind.FocusEnemy` 已有执行分支。

这意味着真实点击锁定可以改变后续选敌，却没有进入 CommandLog。该缺口不是上述 basic 样本的已证实原因，因为其命令里没有锁敌操作；它是同类输入完整性问题。

将这条 UI 路径接入现有 FocusEnemy 命令，Source=Player，记录接受/拒绝。无需新建命令框架。加一次“真实点敌人→Tap→保存→读回”的目标一致性验证。

## 四、C3 / P1：新的开局记录仍反推养成，丢失自动技能预约

### 实际实现

`BattleRunRecord.Capture()` 设置 OpeningProgress 时调用 `OpeningGrowth.FromSim()`。后者不是读取构造战斗时的原始 UnitProgress，而是根据角色最终定义中的 HP/ATK/DEF/AGL/CRT/ExtraAtk 调用 Search，搜索等级、装备组合等。

`OpeningGrowth.Format()` / `Parse()` 没有序列化 `UnitProgress.Reserve`。虽然 CopyOne() 复制了 Reserve，但不能补救 Format/Parse 中不存在的字段，也不能让 Search 从属性反推出预约。

`BattleSim.NextAutoType()` 明确读取 `_growth[slot].Reserve`，决定 Tap/Slide 次序。两名角色可以有完全相同的属性，但预约分别为 SSSSS 和 TTTTT。仅从属性无法区分；新记录可能丢失预约并恢复默认动作。因此“opening stats 一致”不是“opening inputs 完整”。

另外 FromSim 搜索失败会回退到 Level=1；不能把这种恢复无条件标为准确开局。

### 最小修复

在战斗首次执行前，深拷贝实际传入的成长/装备/预约及本场使用的修正参数，保存到已有初始头/记录中。Format/Parse 必须完整往返 Reserve。对比应检查原始输入和实际生效数据，而不是用逆向搜索替代记录。

旧带缺输入时，逆推只能是显式 legacy-recovered/partial 辅助，不进入新带的正常 Capture 路径，也不默默退成 Level=1 后宣称准确。当前逆推代码无需立刻彻底删除，但不要再为这轮扩大装备搜索空间。

此项不宣称是当前手动 basic 首分歧的原因；它是当前新增保存实现中会影响自动战斗的确定缺口。

## 五、C4：新的验收证据必须覆盖修复后的源码

本批 A1 原件确实与摘要一致，旧“234/10 对 244/0”问题不再重复追责。但是 SOURCE_IDENTITY 明确记录该次测试是基线 1b2ca8e，之后 BattleReplay 已被另一个任务改动。

实际文件 blob 对照：

| 文件 | A1 记录的被测 blob | a82801b 中实际 blob |
|---|---|---|
| BattleSim.cs | 72a8a3408b11afee33134e400c4fe645754abc83 | 89dbea518fba4009147a92be35848fc0bf2e5e17 |
| VerificationCatalog.cs | 97a6fe5e52cb9a7f7c07162f15463d1f81e653e4 | bc19a8c984c086a6292e0c4d582629fe703fc0d2 |
| BattleReplay.cs | 6677c70678964575506d9c9c3f8ea5595d3c3e91 | f739d7097400a008e420f460c0f0a567456e93ac |

所以 244/0 可以证明该份基线报告，不自动覆盖新改动。读取到的本批 tests 目录只有这份 A1 TRX，不能把它当修复后全量回归结果。仓库有运行日志和构建哈希记录；未取得对应二进制与录像字节，不把哈希文本当独立构建验收。

C1–C3 修复后冻结源码，跑无 filter 全量回归及三组自然操作；将实际产生的分场文件读入新模拟实例，用标准回放器验证。保存源码/数据版本、TRX、退出码、日志、构建标识及可审看的录像。源码可以先提交 C，再提交只含证据的 D，清单指向被测 C，不必追求报告提交本身的递归哈希绑定。

录影可以保留原始大文件在本地，另交不剪掉关键动作的压缩版本或可访问分发件。不要把压缩/上传问题扩展成新基础设施项目。

## 六、本轮执行决策

下一批只有 C1–C4。使用环境允许的最大并行 subagent 数量，按文件所有权拆分；共享 BattleSim/BattleReplay/HUD 由唯一集成者落盘，复用活动 Unity 会话，不重复派发旧任务。

先用小反例修 C1、C2、C3；再在固定源码上一次性执行 C4。不得一边改被测树一边引用刚开始跑的旧测试结果。并行开发与最终测试实例的隔离是两件事。

通过条件：合法验证配置不改变声明目标；影响模拟的行为都能确定性重建；保存输入含真实预约/成长；三组自然操作保留原来必达目标；新分场回放 Match=True 且各差异项为零；原始回归与被测源码可对应。达到后结束本批交复审，不自行进入 G3，也不无限扩写工单。

## 证据索引

固定文件路径、blob 与本轮证据范围见 EVIDENCE.json。以上判断来源于固定提交，不把前轮结论自动继承为本轮结果。
