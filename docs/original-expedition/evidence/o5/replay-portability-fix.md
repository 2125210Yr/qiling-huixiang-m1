# Unity / .NET 回放一致性修复

2026-09-21。真实 `e04e49e9` 无参数 Player 的 N1 回放在原 Unity/Mono 自验证 MATCH，独立 .NET 6 工具却报 `FinalState mismatch`，胜负、670 tick、命令、事件与结算一致。原始 tape、Player.log、失败报告均保留，未调整任何预期输出。

发现并修复两层原因：

1. `ToString("R")` 在两个运行时产生不同小数字符串，七个单位中五个哈希不一致。改用 G7/G9 也不够：实跑 `1.001953125f` 得到 Unity `1.00195313` / .NET `1.00195312`。共享指纹现使用 float/double 的 IEEE 整数位模式和固定宽度十六进制，保留正负零差别，不使用数值容差。
2. 首轮位模式修复后，A 首领跨运行时 MATCH，但 B/C 仍有三处自然充能位差；递归字段诊断确认其余浮点字段一致。原创模式现从除法开始使用 double 中间计算，每 tick 写回 float 一次；旧模式两句原逻辑保留。引灯通过自然充能、合法技能清零后再过三 tick，精确结果为 `3fa00001`。该回归修改前在 .NET 为 `3fa00000`，实际失败证据保留。

内容版本为 `original-expedition-content-v0.1.1`，内容 SHA-256 为 `69aaa51d7fa5077a5e1d30b4c5f2fd042b62f932f86d0dec59a037762880fdbd`；回放为 Schema 2 / `original-expedition-replay-v2`。v1 在模拟前明确拒绝，历史原件及旧候选包保留，不改写旧记录来求匹配。原个人 save.json 与独立原创 profile 的存档 Schema、完整性算法未改。实际已结束 v0.1.0 原创档的隔离副本可加载并正常新开 v0.1.1，永久记录保留、源字节未变，见 `profile-v011-copy-compatibility.json`。旧活动远征仍由既有版本检查拒绝，应在对应旧包继续或经普通界面结束后新开。

最终实际验证：

- `replay-portability-core-final-20260921.trx`：564 通过、1 既有普通操作占位跳过；已知旧 Catalog 污染测试单独 1/1（`fingerprint-catalog-isolated-20260921.trx`），合计 565 通过、1 跳过。旧 Catalog 路径未被最后的原创充能分支改动影响，复用其有效结果。
- `replay-portability-unity-final-20260921.xml`：Unity 6000.3.23f1，91/91、0 跳过。两运行时共同校验 18 个固定数值 SHA 向量、文化无关性、相邻值与正负零；并验证实际合法技能后的充能位值。
- `family-tapes-v2-unity-final/`：Unity 实际模拟生成的 A/B/C 首领胜局，分别 1141/1675/1899 tick；由当前独立 .NET CLI 读取原始文件，三份全部严格 MATCH。
- `family-tapes-v2-net6-final/`：.NET 对应三份首领录制亦全部 MATCH。完整文件哈希、内容身份与退出码见 `replay-v2-portability-results-final.json`。这是六份磁盘核验，其中三份跨运行时。
- v1 原始 Player tape 由新验证器明确 REJECTED，文件 SHA 不变。旧 .NET 验证器归档到 `dist/original-expedition-v01/replay-verifier-v1-net6`；不得声称它能验证旧 Unity v1 回放。

这些首领记录是固定工厂输入、合法 Submit/Tick 的受控逻辑夹具，未修改战中生命、充能或胜负，**不是普通界面完整录像**。本修复仍须生成对应新包，并在普通实玩恢复后另验真实 Player v2 回放。前面的 `fingerprint-*`、`replay-v2-*-cli.json` 与 `family-tapes-v2-unity/` 是诊断阶段原件，不能冒充最终通过证据。
