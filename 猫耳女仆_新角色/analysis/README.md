# 模板分析交付

入口：`review.html`。可双击在浏览器打开，也可使用当前本地只读服务 `http://127.0.0.1:18273/analysis/review.html`。服务终止后，本地 HTML 及图片仍可直接打开。

`制作方案.md` 给出结论与限制，`reuse-map.json` 是待实现的控制映射，**不是可导入的绑定 spec**。`candidates/*-idle.gif` 为源模型动画；新角色目前仍以已确认 `../concept-v003.png` 为静态基准。

## 复现

在 Windows PowerShell 中使用本机已配置的 Python、JDK 17、Cubism 5.3 Core 和 OpenGL 库。路径集中在 `scan_rigs.py` 与 `render_rigs.py` 顶部；Java 导出器只读模型。中间数据、编译文件和单张图录缩略图已由本项目 `.gitignore` 排除。

```powershell
$rigPython = 'C:\Users\Administrator\.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe'
& $rigPython '.\scan_rigs.py'
& $rigPython '.\render_rigs.py'
& $rigPython '.\analyze_candidates.py'
& $rigPython '.\build_review.py'
& $rigPython '.\verify_delivery.py'
```

从本目录运行以上命令。重新采样会更新分析目录的输出，不写源模型。先读 `verification.json` 和 `output/playwright/browser-validation.json` 了解已完成验证的范围；输入和代码未改变时无需重复运行。

页面后台检查使用已安装的 Playwright 与 Chrome：启动只监听 127.0.0.1:18273、根目录为项目目录的静态 HTTP 服务后，执行 `node .\verify_review.cjs`。脚本检查五个标签切换、动画画面变化、资源加载和窄屏布局，不操纵桌面。

当前验证：144 个模型的 Core 起点采样；五个候选的 220 个参数、2,001 个姿态；6,114 条图录所用运动曲线的首尾与有限采样；288 个源文件哈希未改变。此结果不构成新角色模型验收，也不是 144 套模型的全参数联合检查。
