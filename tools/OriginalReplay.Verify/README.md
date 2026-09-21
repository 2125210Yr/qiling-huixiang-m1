# 原创远征磁盘回放验证

用现有 `OriginalBattleReplayer.Verify` 检查一份已保存回放，不启动 Unity。项目直接编译当前仓库的 Battle 源码；验证时使用与回放对应的源码版本。

在仓库根目录用本机已有 .NET 6 SDK 构建一次：

```powershell
dotnet build tools/OriginalReplay.Verify/OriginalReplay.Verify.csproj -c Release
```

随后传入一个文件的明确路径。路径有空格或中文时保留引号：

```powershell
dotnet tools/OriginalReplay.Verify/bin/Release/net6.0/OriginalReplay.Verify.dll "C:/path/to/battle.original-replay.json"
$LASTEXITCODE
```

命令只读指定文件，标准输出是一份 JSON，包含原始文件 SHA-256、规则和内容版本、run/encounter/attempt 身份、输入哈希、结束 tick、重放结果及差异。支持 UTF-8 BOM。文件哈希覆盖读入的原始字节，不把序列化后的文本当作原文件。

| 退出码 | 状态 | 含义 |
| --- | --- | --- |
| 0 | `MATCH` | 冻结输入、命令、结算、事件和最终状态与重放一致。 |
| 1 | `REJECTED` / `MISMATCH` | 版本或输入被拒绝，或实际重放结果不一致；查看 `differences`。 |
| 2 | `ERROR` | 参数、文件读取或 JSON 解析失败；未得到可用的匹配结论。 |

本工具验证确定性与记录完整性，不证明录制来自普通 UI，也不替代对应的视频和 Player 构建验收。仓库 `docs/original-expedition/evidence/o5/family-tapes/` 的三个文件是明确的控制夹具；正式交付应另外验证实际 Player 保存的回放。

当前接收 `original-expedition-replay-v2` 与内容 `v0.1.1`：浮点指纹使用 IEEE 位模式，避开 Unity Mono 与 .NET 的小数格式差异。v1 记录会在模拟前明确拒绝，原件不改写。历史 v1 的 Unity 内自验证结果仅适用于当时的 Player/Mono；旧 .NET 验证器不能保证与它一致。旧版进行中的远征应在对应旧包继续，或通过普通界面结束后再在新版开始；永久发现与解锁仍保留。
