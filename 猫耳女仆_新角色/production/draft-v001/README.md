# 猫耳女仆可动草稿 01

第三版立绘保持为外观基准。本次完成 16 层素材、可编辑 PSD、16 个新网格、12 个参数和 8 秒温柔待机，并导出 Cubism cmo3/moc3。它是第一份可动验证稿，不是最终精修模型。

## 打开

- 预览：`http://127.0.0.1:18273/production/draft-v001/player.html`。可播放/暂停、恢复默认、眨眼、拖动单个参数，并对照原图和透明边缘。本地服务关闭后，须重新从项目目录启动静态 HTTP 服务。
- Cubism 编辑：`export-v001/model/model.cmo3`。
- 运行文件：`export-v001/model/model.model3.json`，同目录附 moc3、纹理、显示信息和 `motions/CatMaid_idle.motion3.json`。
- 分层素材：`CatMaid_layers_v001.psd` 和 `layers/`。PSD 图层保留原始画布位置。
- 模型包：`CatMaid_draft_v001.zip`，含 Cubism 文件、纹理、待机和制作说明；PSD 为单独交付文件。

## 实际做了什么

11 个主图层的彩色像素直接来自已确认 `concept-v003.png`。内置 image_gen 生成前景选区、部件分区、隐藏桌面/内裙补绘和闭眼端点；原始输出与完整提示词保存在 `assets/`。生成工具没有交付真正透明的桌子 PNG，因此按木桌选区排除了其棋盘底，没有将棋盘写入最终贴图。阴影作为独立的透明渲染效果。

新模型网格和 UV 为本图重新建立。参考 `c020_01_full` 的腿/脚踝双轴组织，为每侧写入 3×3 关键形状；原模型的呼吸、腿和脚踝待机曲线经居中、限幅和变速后用于新控制。头发、尾巴、悬浮、眨眼等曲线为本稿新编。详见 `motion-derivation.json`。

`rig-spec-v4.json` 是本角色真实导出输入。`player.js` 预览同一份绑定数据，并以导出的 moc3 经官方 Core 采样的顶点位置作独立对照；网页本身不直接解析 moc3。

## 验证与目前边界

导出器完成 cmo3 回读、Core 中 1,889 个姿态的关键形状/父级/透明度/索引方向与复位检查。另采样 334 个姿态，包括完整待机 241 帧及 32 个随机联合姿态，图像证据在 `verification/`。具体指标以该目录 JSON 记录为准。

当前适合检查总体节奏、抬脚幅度与拆分关系。还需要精修细发丝及抠图边缘、部分遮挡补绘和眨眼混合过渡；尚无完整眼球跟随、口型或大幅转头。当前只为小幅待机制作，不保证扩大动作后无穿插。未用 Cubism 5.3 窗口逐项打开保存验证，也未穷举全部参数组合。

## 复现路径

`scripts/package_layers.cjs` → `scripts/build_rig.py` → 现有 `native-authoring/run.py head-export NEW_DIR --spec rig-spec-v4.json` → `scripts/verify_native.py` → `scripts/verify_player.cjs` → `scripts/package_delivery.py`。

使用本机现有 Node/Sharp/ag-psd、Python/NumPy、JDK 与 Cubism Core，未使用 computer use。导出器要求新输出目录；重做版本时使用新目录，不覆盖已有导出证据。脚本中的默认导出位置为本稿 `export-v001/model`。
