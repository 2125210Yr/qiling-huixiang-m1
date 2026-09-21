# Original Expedition 交付候选打包

`package-player.ps1` 将真实 `PASS` 构建审计对应的完整 Player 打包为交付候选。它不运行 Unity、Player 或联网，也不代表 UI 与试玩录像验收通过。

当前实现已用明确标记的 **FIXTURE 假 Player** 验证；这些结果不是实际 Unity 构建或实际游戏验收证据。

## 用法

在 Windows PowerShell 7 中运行，无需安装依赖：

```powershell
pwsh -NoProfile -File F:/天命之子/tools/OriginalExpedition.Delivery/package-player.ps1 `
  -BuildAuditPath F:/PackageProject/BuildAudit/build-audit.json `
  -Destination F:/Deliveries/OriginalExpedition-candidate-001 `
  -ExpectedCommit '<独立确认的完整 sourceCommit>' `
  -ExpectedContentHash '<独立确认的内容哈希>'
```

示例路径与占位值需替换。`ExpectedCommit`、`ExpectedContentHash` 必须来自预期构建版本，并与审计逐字符匹配；脚本不会推断版本或自动选择最近一次构建。审计是现有构建入口产生的证据，没有数字签名，因此还需保证它来自可信的实际构建过程。

Player 根目录由审计的 `exe` 推导；它必须指向已审计的 `OriginalExpedition.exe`。指南默认取本仓库的 `docs/original-expedition/PLAY-GUIDE.md`，可通过 `-GuidePath` 指定已有文件。输入和目标使用绝对本地盘符路径，不接受 UNC、设备路径或符号链接/junction。

`Destination` 及其旁边的 `Destination.zip` 必须均不存在，且目标须在源 Player 之外。失败时保留现有文件和部分产物；脚本没有删除、清理或覆盖逻辑。重试请使用另一个全新目录。

技术验收完成后可追加 `-AcceptanceSummaryPath F:/天命之子/docs/original-expedition/ACCEPTANCE-PROGRESS.json`。报告必须包含唯一的 O-001～O-038，全部为 PASS、实际结论非空、剩余要求为空，且每项证据指向现存物理文件；计数及 Player 的 commit/content 身份必须匹配。该检查验证报告结构和引用，不替代证据内容审查。

## 产物与核验

```text
OriginalExpedition-candidate-001/
  Player/                   完整 Player 文件集及原有 notices
  PLAY-GUIDE.md
  ACCEPTANCE-SUMMARY.json    仅传入验收报告时包含，保留输入原字节
  build-audit.json           原审计原字节副本
  VERSION.json               commit、content hash、候选状态与未完成验收项
  FILE-HASHES.json           文件相对路径、字节数、SHA-256
OriginalExpedition-candidate-001.zip
```

写入前要求审计 `result` 严格为 `PASS`，输入 manifest 哈希正确，实际 Player 的文件集、字节数与 SHA-256 和 `outputs` 完全相符。路径拒绝越界、绝对路径、大小写重复、ADS、Windows 保留名和尾部点/空格。源目录及所有已存在祖先均禁止 reparse point。

Player 必须含 `package-input-manifest.json` 和四份第三方声明：NotoSansSC 的 `COPYRIGHT.txt`、`OFL.txt`、`SOURCE.json`，以及 Inochi2D 的 `LICENSE.md`。所有其他已审计文件也完整保留，包括 Unity 自带 notices；不按扩展名筛选输出。复制只传输审计涵盖的普通文件字节，不携带未审计的 NTFS alternate streams。

复制后再次核验源 Player 和副本；ZIP 创建后逐项读取解压流，复核文件集合、字节数与 SHA-256，然后再次核验目录。`FILE-HASHES.json` 明确排除自身以避免循环哈希；它本身包含在 ZIP 验证及最终 ZIP SHA-256 中。

成功输出 JSON，包含 `Status: BUILD_CANDIDATE_PACKAGED`、`Zip`、`ZipSha256` 和文件数。未传报告时保留原有 UI 与录像待验收状态；传入完整报告时，输出和 VERSION 单列技术通过及 `PENDING_USER_REVIEW`，绑定报告 SHA-256。`FinalAcceptancePassed` 始终为 `false`，用户体验判断仍待用户给出。脚本不判断构建时的依赖审计是否充分，也不运行游戏来验证体验。

## 回归测试

```powershell
pwsh -NoProfile -File F:/天命之子/tools/OriginalExpedition.Delivery/test-package-player.ps1 -Phase GREEN
```

测试覆盖成功复制与 ZIP、独立复核 ZIP 流与哈希、FAIL 审计、commit/content 不符、同长度文件改写、缺失/额外文件、manifest 哈希、必需 notice、路径越界与 Windows 路径别名、大小写重复、错误 exe、已有目录/ZIP、源内目标和 junction。

2026-09-21 新增报告校验回归后为 38/38 PASS（原 23 项及新增 15 项）。新增用例检查报告原字节入包、Player 不变、用户验收仍待定，并拒绝缺项、重复 ID、非 PASS、缺证据、遗留要求、错误计数/版本及链接路径。这 38 项是打包工具夹具测试，不是 MVP 的 38 项实机验收。

`tests/evidence/*.json` 保存实际 RED/GREEN 结果。`-Phase` 只是证据标签，不改变测试内容。所有假 EXE、假 DLL 和 ZIP 均明确标记为 FIXTURE，写入带 GUID 的系统临时目录并保留；不执行它们，不将这些假 Player 产物加入 Git。
