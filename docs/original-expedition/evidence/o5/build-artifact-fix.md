# 构建时性能测试资源排除修复

2026-09-21 重启后原有 Unity EditMode 75/75 通过。首次独立构建遇到本机默认 GI 缓存目录创建失败；保留原日志和部分输出，改用副本内 `-giCustomCacheLocation` 后 BuildPlayer 已产出 EXE，但严格审计拒绝了性能测试包自动生成的 Resources JSON。失败审计见 `build-8075dc35-rejected-audit.json`。

本机已锁定的性能包 3.5.0 通过 URP → Core → Collections 传递引入；它的 order-0 `TestRunBuilder.OnPreprocessBuild` 对普通构建也无条件生成两份 JSON。删直接测试依赖不能解决，包版本与 Packages 文件均保留。

修复仅在 `OriginalExpeditionBuild` 的审计构建调用期间启用后置预处理：对两个精确 JSON 及其存在的 meta，先检查全部源和目标，再逐文件移至副本 `BuildAudit/ExcludedPerformanceTestAssets`。不删除、不覆盖、不修改字节；物理路径和独立 manifest 必须有效。审计记录这些排除文件的 SHA 与大小。其他新增 Assets／packedAssets 仍由原白名单拒绝，普通构建不执行这段排除。

验证：

- `build-artifact-guard-red.xml`：新增 7 项先全部因明确占位实现失败，RED 原件保留。
- `unity-build-artifact-guard-green.xml`：真实 Unity 当前全套 82/82 通过、0 跳过（原 75 项＋7 项磁盘回归）。新增测试验证精确字节保留、不动其他资源、缺文件不产生副作用、四种目标占用均在任何移动前拒绝。
- 独立子代理复核作用域、回调顺序、物理路径、目标预检与原审计约束，未发现重要缺陷。

这是构建修复与回归证据。真实新副本构建、普通 Player 与录像仍须后续实际结果证明；本页不把磁盘夹具冒充真实打包通过。
