# O5 打包准备审计

审计日期：2026-09-21。本页保留最初对现有 Unity 工程、资源引用、程序集、构建入口与字体元数据的只读调查；调查时未复制工程、未运行 Unity。源工程为 `F:/Resonance/client`，仓库中的 `F:/天命之子/client` 指向该工程。

后续已实现白名单复制脚本与 `OriginalExpeditionBuild` 审计入口，完成一次 499 文件的预备副本并核对哈希；许可文本也已保存。预备副本早于构建入口最后一次修改，不能作为最终交付版本。当前进度见 [O5 代码检查点](O5-CODE-CHECKPOINT.md)；真实 Unity BuildReport 和最终 Player 仍未生成。

**结论：不能直接用原工程的现有 WindowsBuild 生成本版分发包。应建立新的物理目录，按下面的正向白名单复制构建输入。原创模式现有界面只需要程序生成图形和一个中文字库；旧角色图、兔子模型、Inochi 图标和原生插件都不属于这份资源清单。最终“不含旧素材”的结论必须由隔离副本清单及真实 BuildReport 共同证明。**

## 已核实的隐式打包入口

`Assets/Editor/Build/WindowsBuild.cs:14` 的 `Build()` 只指定 `Assets/Scenes/Boot.unity`、Win64 和 LZ4，随后调用 `BuildPipeline.BuildPlayer`；它没有任何资源允许清单或构建产物审计。`BuildAndExit()` 可作已有批处理入口，输出为 `Builds/Win64/Resonance.exe`，构建结果为 `Temp/win.build.result.txt`。它还修改副本的 PlayerSettings，因此不应在共享工程上执行隔离打包。

只指定 Boot 场景并不能排除 Resources。扫描 **所有 Assets 子目录**，共发现 3 个 Resources 根、103 个非 meta 文件：

| 路径 | 文件数 | 风险与处置 |
| --- | ---: | --- |
| `Assets/Resources` | 95 | 其中 `Art/Characters/C*` 有 91 个旧图、分层、描述和着色器文件；另有 3 个表现着色器及 1 个字体。仅允许下述字体。 |
| `Assets/EmeraldInochi/Resources` | 3 | 含 `Inochi/EmeraldBunny.bytes`（5,260,557 字节）及两个着色器；整个 Resources 子目录不进入副本。 |
| `Assets/Inochi2D/Resources` | 5 | 含 `i2d-logo.png` 和四个着色器；整个 Resources 子目录不进入副本。 |

103 个文件按扩展名为 83 PNG、8 JSON、10 shader、1 OTF、1 bytes。这是源工程现状，**不是待分发包的内容清单**。未发现 Assets 内 StreamingAssets 目录。`Assets/Inochi2D/Native/windows-x86_64` 另有三个 DLL；例如 `inochi2d.dll.meta` 将 Win64 设置为启用，因此也不能随目录整体复制。

## 当前模式的真实依赖

- `GameRoot.cs:60` 在读取旧存档前确定模式；普通无参数启动进入原创模式，`OriginalModeSelection.UseOriginal()` 只有显式 `--legacy` 才选择旧模式。
- `GameRoot.Expedition.cs` 用冻结的 OE_* 定义建立五人和敌方界面；`ExpeditionHud.cs:501` 从 `CharacterPresenter.UiFont()` 取字体，`:517` 使用 `UiSprites.Round()`。`UiSprites.cs` 在代码中生成纹理和 Sprite，不读取原画。
- `CharacterPresenter.cs:742` 首选 `Resources.Load<Font>("Fonts/NotoSansSC")`。不应依赖系统字体回退来证明离线便携性。
- `CharacterArt.cs` 仍保留 `Resources/Art/Characters/<id>/...` 的动态读取，但当前原创 HUD 没有调用该旧角色展示路径。未发现本模式的 AssetBundle、Addressables、远程图像或源码目录读取路径；存档文件访问不属于素材依赖。
- 编译依赖不能忽略：`Resonance.App.asmdef` 引用 `Resonance.Battle` 和 `EmeraldInochi`；`EmeraldInochi.asmdef` 引用 `Inochi2D.Runtime` 与 `Unity.ugui`。`EmeraldLobby.cs` 和 `Debug/EmeraldLobbyCheck.cs` 直接使用 `InochiStandee` 类型。故仅省略整个 Inochi 代码目录会导致编译失败。
- 最小零生产代码修改候选是保留这两个依赖的**纯托管代码**，同时不复制它们的模型、Resources 和 native DLL。所查 Inochi 代码没有自动启动的 RuntimeInitialize 方法；模型与原生调用由 `InochiStandee.Attach/Initialize` 触发。默认原创流程不进入旧大厅，`EmeraldLobbyCheck` 要求显式调试参数。此候选仍须真实构建和普通流程验证；不能据静态检查声称所有运行时路径均已通过。
- 本交付包不承诺 `--legacy`、翡翠兔检查或旧素材演示参数可用。若后续要在发行程序集完全移除旧入口，应作为明确的小改动验证，不能在打包脚本里静默替换成另一份玩法代码。

## 隔离副本最小允许清单

使用新的独立物理目录，逐项正向复制。保留列出资产对应的 `.meta` 及必要目录 `.meta`，以维持 GUID；不要把任何源目录直接映射为 junction/symlink。下表是本次审计建议，不是已经执行的操作。

| 允许输入 | 复制范围与目的 |
| --- | --- |
| `Assets/Scripts/Resonance.Battle` | 仅 `.cs`、`.asmdef` 和对应 `.meta`；包含原创内容、规则、存档、调度与重放，以及其现有核心依赖。 |
| `Assets/Scripts/Resonance.App` | 仅 `.cs`、`.asmdef` 和对应 `.meta`；保留原实现，避免引入另一份 UI/流程代码。 |
| `Assets/EmeraldInochi` | 仅根目录的 `FootStretchTimeline.cs`、`InochiStandee.cs`、`NativeSurface.cs`、`EmeraldInochi.asmdef` 及其 `.meta`。不递归复制 Resources。 |
| `Assets/Inochi2D/Runtime` | 仅 `.cs`、`.asmdef` 和对应 `.meta`，满足现有托管类型依赖。随包提供其 BSD 2-Clause notice；不复制 Native、Resources、Editor。 |
| `Assets/Scenes/Boot.unity` | 场景及其 `.meta`；唯一构建场景。 |
| `Assets/Settings` | 仅 `DefaultVolumeProfile.asset`、`Mobile_RPAsset.asset`、`Mobile_Renderer.asset`、`PC_RPAsset.asset`、`PC_Renderer.asset`、`SampleSceneProfile.asset`、`UniversalRenderPipelineGlobalSettings.asset` 及对应 `.meta`。这是保留现有 ProjectSettings 的本地序列化引用闭包；不另复制未引用的 `Postprocessing Profile.asset`。 |
| `Assets/Resources/Fonts/NotoSansSC.otf` | 唯一项目 Resources 数据资产及 `.meta`；保留字体原始字节，附下述版权与许可文本。 |
| `Assets/Editor/Build/WindowsBuild.cs` | 构建入口及 `.meta`，加 `Assets/Editor/Resonance.Editor.asmdef` 及 `.meta`。只为构建，不复制其他预览/旧素材 Editor 工具。 |
| `Packages/manifest.json`、`Packages/packages-lock.json` | 锁定现有 Unity 包依赖。已检查无 `file:` 本地包或自定义 Git 包引用；现有 registry URL 为 Unity 官方包源。不要复制旧 PackageCache。 |
| `ProjectSettings/*.asset`、`ProjectSettings/ProjectVersion.txt` | 复制当前项目配置。图形和质量配置引用上述七个 Settings 资产；本地 preloadedAssets 为空，未发现自定义启动图和图标引用。 |
| 包外说明和 notices | 启动说明、限制、版本与内容 hash、字体 notice/OFL、Inochi 托管代码 BSD notice、适用的 Unity 包第三方 notices、允许清单及 SHA-256 清单。无需放入 Resources。 |

`Assets/Content/original-expedition/README.md` 是内容来源说明，玩法数据实际编译于 `ExpeditionContent.cs`；可以随说明文档交付，不是运行所需的资源。

**默认不复制：** 其余 Assets 内容，尤其所有旧 `Resources/Art`、其他 Resources 根、`EmeraldBunny`、Inochi 原生插件、`Assets/_Recovery`、测试和旧烟测工具；也不复制源工程的 Library、Temp、UserSettings、Builds、captures、自动生成 csproj/sln、源仓库根目录、原作参考图/动画目录或旧发行包。这是一份新副本的输入选择，不要求删除、移动或改名任何源文件。重复操作使用新的输出目录，禁止递归清空旧目录。

## 字体来源与分发条件

本地字体为 Noto Sans SC Regular，8,482,020 字节，SHA-256：

`a2b93e6c2db05d6bbbf6f27d413ec73269735b7b679019c8a5aa9670ff0ffbf2`

直接解析其 SFNT `name` 表得到版本 `2.002`、PostScript 名 `NotoSansSC-Regular`、2014–2020 Adobe 版权、Google Noto 项目地址，以及 SIL Open Font License 1.1 标识。字体 importer 设置 `includeFontData: 1`。没有找到与该字体一同保存的完整本地 OFL 文件，因此仅检查文件名还不足以完成分发说明。

已核对 [Noto CJK 官方仓库的 Sans 许可文本](https://raw.githubusercontent.com/notofonts/noto-cjk/main/Sans/LICENSE)。该许可允许字体随软件分发，但要求携带版权及许可文本。建议在成品 `ThirdPartyNotices/NotoSansSC` 中保留本地字体版权声明和完整 OFL 1.1，避免把封装在 Unity 数据文件中的简短字体元数据当成唯一用户可读通知。此调查没有下载或替换字体；其原始获取过程未在仓库中找到记录，也没有做上游同版本二进制逐字节比对。当前 fingerprint 与字体内置许可元数据已明确记录。

若保留上述 Inochi 托管编译依赖，还应将 `Assets/Inochi2D/LICENSE.md` 的完整 BSD 2-Clause notice 随包交付。不要把字体的许可推及其他旧图片或模型。

## 可复核的隔离与产物验证

本次以 Boot 和全部 ProjectSettings 的序列化 GUID 为根，遍历本地 Assets 与现有 PackageCache 的 YAML 引用。闭包落到 9 个 Assets 文件：Boot、GameRoot 脚本及上述 7 个 Settings；没有落到旧角色图、Emerald 或 Inochi 数据资产。另解析到 190 个包依赖文件，尚有 20 个 GUID 在本地 meta 索引中未解出。**这只是静态来源检查，不替代 Unity 的依赖解析，也不把未解析项默认为安全或构建错误。**

Unity 恢复后，O5 应留下以下实际证据：

1. 复制前输出逐文件允许清单与 SHA-256；副本路径解析必须仍在新物理根下。扫描副本内**所有** Resources 路径，只能有本字体。没有 StreamingAssets、外链目录或旧 DLL/模型；不能只检查顶层 Resources。
2. 在副本中调用 `AssetDatabase.GetDependencies` 校验 Boot/Settings 依赖，核对静态扫描未解析的引用。检查导入后是否有脚本或包生成额外 Assets/Resources。
3. 使用与既有 WindowsBuild 相同的场景和参数构建，保留完整 BuildReport。可在独立审计入口增加 `DetailedBuildReport` 并导出 `report.packedAssets[].contents[].sourceAssetPath/sourceAssetGUID`，对每个 `Assets/...` 条目做允许清单匹配。发现清单外资源必须使交付检查失败，不能只搜索图片文件名或检查压缩包表面目录。
4. 同时检查 `BuildReport.GetFiles()`/输出文件清单，确认没有 Inochi DLL、`.bytes` 模型或额外 StreamingAssets。这补足非序列化输出不一定出现在 packedAssets 的边界。保留 Unity 自带引擎/包文件及其 notices。
5. 把完整 Player 输出复制到与源工程无关的新路径，无调试参数运行 N0→N7→结算→返回→新一趟；核验字体、输入与持久化。最终 ZIP 只来自本次新的 Player 输出，不从旧 Builds 或整个源码目录打包。

上述 BuildReport、PackedAssets.contents、PackedAssetInfo.sourceAssetPath、DetailedBuildReport 和 AssetDatabase.GetDependencies API 均已在本机 Unity 6000.3.23f1 的 `Editor/Data/Managed/UnityEditor.xml` 核实存在。已有 WindowsBuild 尚未导出该审计信息；本次未修改它。

截至本审计，许可握手阻止 Unity 启动，详见 [只读许可诊断](evidence/o3/unity-license-diagnosis.md)。所以当前完成的是打包输入和风险调查，**不是新版 EXE 构建通过、普通流程通过或最终资源排除通过**。
