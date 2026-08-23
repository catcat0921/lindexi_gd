# XiaoXiIme 纯净 VM 安装排障工作模式

## 目的

本文档用于跨多轮、跨新对话持续排查 XiaoXiIme 在纯净 Windows VM 中的安装问题。开发环境负责构建包含诊断能力的自包含 payload；人类操作者只负责将 payload 复制到 VM、运行指定命令，并原样回传控制台输出和报告。后续分析与代码迭代必须以 VM 返回的证据为准。

## 环境约束

VM 被视为纯净最终用户环境，不假设存在以下组件：

- Visual Studio 或 Visual Studio Build Tools；
- .NET SDK 或系统级 .NET Runtime；
- dumpbin、Dependencies、Process Monitor 等开发或诊断工具；
- NuGet、Git 或源代码；
- 额外 PowerShell 模块。

`payload-build` 发布的 CLI 必须自包含。安装自检必须由 CLI 和 Windows 自带 API 完成，不能要求操作者临时安装工具。

## 固定协作流程

1. 在开发机的 `App\XiaoXiIme` 目录构建新的 payload：

   ```powershell
   dotnet run --project .\src\XiaoXiIme.Cli\XiaoXiIme.Cli.csproj -- payload-build --output .\artifacts\integration-payload
   ```

2. 将整个 `integration-payload` 目录复制到具有快照、可随时还原的 Windows VM。
3. 在 VM 中打开管理员 PowerShell，进入 payload 根目录并运行：

   ```powershell
   $env:XIAOXIIME_ENVIRONMENT = "VirtualMachine"
   .\app\cli\XiaoXiIme.Cli.exe integration-run . --confirm I-UNDERSTAND-THIS-MODIFIES-WINDOWS --report .\results\integration.json
   ```

4. 操作者原样回传以下两部分，不自行筛选或改写：
   - 从命令开始到结束的全部控制台 JSON 行；
   - `results\integration.json` 的完整内容。
5. 开发侧根据结构化证据判断下一步，只添加能区分剩余假设的最小诊断或根因修复。
6. 生成新 payload，重复以上流程。每轮都应注明 payload 的生成时间或 Git 提交，以避免分析旧版本输出。

## 当前诊断阶段

`integration-run` 在调用 `ImmInstallIMEW` 前输出 `diagnostics-pre-install`，其中包含：

- OS 描述、OS 架构、CLI 进程架构和关键目录；
- 源 IME 的绝对路径、文件长度、SHA-256、属性、版本和 Mark of the Web；
- PE Machine、Magic、Subsystem、DLL 标志和导入表；
- 导入模块是否为 API-set，以及普通系统 DLL 能否从系统目录解析；
- 使用 `DONT_RESOLVE_DLL_REFERENCES` 的安全映像映射结果；
- `System32\XiaoXiIme.ime`、System32 临时写入能力和匹配键盘布局注册项的安装前状态；
- 基于以上数据生成的 `Findings`。

安装前还会运行 `native-ime-load-probe`。该阶段在独立的 CLI 子进程中使用正常 Windows 加载器加载项目自身构建的 IME，并用 `GetProcAddress` 验证全部 11 个传统 IME 导出。独立进程可以安全记录 DLL 初始化失败、加载器错误、异常退出和超时，而不破坏主 `integration-run`。

如果 `ImmInstallIMEW` 返回失败，还会输出 `diagnostics-post-install-failure`，以便比较调用前后的系统目录和注册表状态。`install-x64` 阶段会记录传给 API 的绝对路径和原始 Win32 错误码。

## 当前已知问题

首次 VM 输出为：

```text
ImmInstallIME failed with Win32 error 2: 系统找不到指定的文件。
```

已知该错误发生前 payload manifest、文件长度、SHA-256、IME VERSIONINFO 和要求的导出函数均已通过校验。因此不能仅凭错误 2 断言源 `.ime` 文件不存在。当前待区分的主要假设是：

1. PE 架构或映像格式与 x64 CLI/Windows 不匹配；
2. IME 导入的原生系统模块在 VM 中无法解析；
3. Windows 能读取文件但无法将其映射为原生映像；
4. `ImmInstallIMEW` 内部复制到系统目录或创建布局时失败；
5. VM 的 Windows SKU、组件或安全策略不支持当前传统 IMM32 IME 安装路径；
6. 系统目录或键盘布局注册表存在调用前后不一致的残留状态。

### 2026-07-26 第一轮增强诊断结果

VM 返回的数据进一步确认：

- 源文件存在、可读且 SHA-256 稳定，没有 Mark of the Web；
- PE 为 `Amd64`、`PE32Plus`、`WindowsGui` DLL；
- CLI 进程和操作系统均为 x64；
- 10 个导入模块均可分类或从 System32 解析；
- Windows 能使用 `DONT_RESOLVE_DLL_REFERENCES` 映射该映像；
- 调用前后 System32 均没有 `XiaoXiIme.ime`，也没有匹配布局注册项。

该轮唯一 finding 是 `GetBinaryType` 返回 error 193。此 API 面向可执行程序，不能用它对 DLL/IME 的失败结果判定镜像无效。后续版本先停止将该结果视为 finding；在完整安装闭环验证通过后，已从诊断模型中移除此项，避免继续输出“%1 不是有效的 Win32 应用程序”这一干扰信息。IME 映像有效性改由 PE 解析、安全映像映射和独立进程真实加载共同验证。

下一轮判断方式：

- 若 `native-ime-load-probe` 失败，优先分析其 stdout 中的 `LoadErrorCode`，问题位于真实加载、NativeAOT 初始化或导出解析；
- 若原生加载成功但 System32 写入探测失败，问题位于权限、安全策略或系统目录访问；
- 若原生加载和 System32 写入均成功，而 `ImmInstallIMEW` 仍返回 2，则证据将集中指向 API 的复制/注册调用语义，而非当前 IME 二进制本身。

### 2026-07-26 第二轮增强诊断结果

VM 返回结果确认：

- `native-ime-load-probe` 使用正常 `LoadLibraryExW` 成功加载 IME；
- 11 个传统 IME 必需导出全部能由 `GetProcAddress` 解析；
- System32 随机临时文件创建和删除成功；
- 常规 `ImmInstallIMEW` 仍返回错误 2；
- 调用后 System32 没有目标文件，键盘布局注册表也没有变化。

这些证据排除了 NativeAOT 初始化失败、真实依赖缺失、导出缺失和 System32 权限问题。剩余主要变量是源文件所在目录与传统 IME 文件名兼容性。

下一版在常规安装失败后输出 `imm-install-variant-probe`，测试以下可回滚矩阵：

1. `payload-short-name`：保持 payload 目录，仅把私有副本命名为 `XIAOXI.IME`；
2. `system32-original-name`：复制到 System32，保持 `XiaoXiIme.ime`；
3. `system32-short-name`：复制到 System32，并使用 `XIAOXI.IME`。

结果解释：

- 只有两个短名变体成功：文件名兼容性是主要约束；
- 只有两个 System32 变体成功：文件必须先进入系统目录；
- 只有 `system32-short-name` 成功：目录和短文件名都是前置条件；
- 三个都成功而原路径失败：原始 payload 路径或路径长度存在约束；
- 三个都失败：停止继续猜路径，转向签名、Windows SKU/语言组件或 ImmInstallIME 的其他传统验证规则。

每个变体均不覆盖已有文件；成功注册后立即 `ImmUninstallIME`，并只删除本轮创建的私有副本。任何卸载或清理失败都会进入报告。

### 2026-07-26 第三轮变体诊断结果与根因

VM 返回的变体矩阵为：

- `payload-short-name`：失败，Win32 error 2；
- `system32-original-name`：成功注册；
- `system32-short-name`：成功注册。

因此文件名长短不是约束，决定性条件是：**调用 `ImmInstallIMEW` 前，传统 IME 文件必须已经位于 System32。** 原正式流程把 payload 中的绝对路径直接传给 API，并错误假设 API 会负责复制文件，这是本次安装失败的根因。

该轮也暴露了探测清理缺陷：`imm32.dll` 不导出代码中错误声明的 `ImmUninstallIME`，两个成功探测布局未能立即卸载，而探测副本已被删除，可能在 VM 留下 `XiaoXi IME Probe [...]` 的悬空布局项。后续版本已做以下修复：

- 正式安装先复制到 System32，校验长度和 SHA-256，再调用 `ImmInstallIMEW`；
- 不覆盖内容不同的既有 System32 同名文件；
- 注册失败时只回滚本次创建的副本；
- 不再调用不存在的 `ImmUninstallIME`；
- 通过返回的布局 ID、`Layout Text` 和 `Ime File` 精确验证并删除探测布局；
- `uninstall-old` 会识别并清理历史 `XiaoXi IME Probe [...]` 布局、preload 引用，以及不再被布局引用的 `XiaoXiIme.ime`/`XIAOXI.IME` 文件。

下一版首次在同一 VM 运行时，`uninstall-old` 应先报告移除了第三轮残留的布局 ID，随后正式 `install-x64` 应从 `C:\Windows\System32\XiaoXiIme.ime` 注册成功。若 System32 文件因进程占用无法删除，日志会明确保留该残留，不应人工静默忽略。

### 2026-07-26 第四轮正式安装验证结果

VM 返回结果确认根因修复有效：

- `uninstall-old` 成功移除第三轮残留布局 `E0200804`、`E0210804`；
- `diagnostics-pre-install` 和 `native-ime-load-probe` 均通过；
- 正式 `install-x64` 从 `C:\Windows\System32\XiaoXiIme.ime` 注册成功，返回布局 `E0200804`；
- x86/x64 的 TSF ABI 和隔离 COM 激活均通过；
- `cleanup` 成功移除正式布局和不再被引用的 System32 IME 文件。

安装、TSF 验证和清理均已成功。该轮最终未生成 `report`，原因不是 IME 安装失败，而是 payload 在纯净 VM 中硬编码执行 `dotnet vstest`；VM 按约束未安装 `dotnet`，因此 `IntegrationTestRunner` 抛出 Win32 error 2。

后续版本将集成冒烟场景发布为 `win-x64` 自包含 `XiaoXiIme.IntegrationTestHost.exe`，由 CLI 直接执行，不再依赖 VM 的 SDK、Runtime 或测试平台。外部 stage 的进程启动失败也必须转为结构化失败结果并写入报告，不能再以未处理异常结束。

### 2026-07-26 第五轮完整闭环验证结果

VM 返回结果确认纯净最终用户环境中的完整流程已经通过：

- `uninstall-old` 未发现上一轮残留；
- 安装前诊断无 finding，原生加载与 11 个传统 IME 导出验证全部通过；
- `install-x64` 从 `C:\Windows\System32\XiaoXiIme.ime` 成功注册布局 `E0200804`；
- x86/x64 的 TSF ABI 和隔离 COM 激活全部通过；
- 自包含 `XiaoXiIme.IntegrationTestHost.exe` 成功执行，输出 `PASS candidate-window-state` 和 `PASS ime-host-ipc`，不再依赖 VM 中的 `dotnet`；
- `cleanup` 成功移除布局和不再被引用的 System32 IME 文件；
- 生成了退出码为 0 的结构化集成报告。

至此，最初的 `ImmInstallIMEW` Win32 error 2、探测布局残留和纯净 VM 缺少测试平台三个问题均已完成根因修复与实机验证。当前输出还显示 `report` 控制台事件早于 `cleanup`；虽然报告文件随后会被重写并包含清理结果，但事件顺序容易造成误解，且清理失败不会改变原成功退出码。后续实现改为先执行并记录 `cleanup`，再写入和输出最终 `report`；当业务阶段全部成功但清理失败时，整体返回非零退出码。

### 2026-07-26 第六轮真实按键注入结果

VM 返回结果确认安装、x86/x64 TSF 验证以及前两个自包含集成场景继续通过，但 `real-ime-keystroke-commit` 在调用 `SendInput` 时失败：

```text
SendInput injected 0 of 4 keyboard events. Win32 error 87: 参数错误。
```

错误发生在四个键盘事件进入系统输入队列之前，因此本轮没有证据指向 `ImeProcessKey`、`ImeToAsciiEx`、HIMC 结果字符串或 `WM_IME_COMPOSITION` 链路。检查集成宿主发现其 `INPUT` P/Invoke 结构依赖运行时自动推导联合体对齐；`SendInput` 会严格校验 `cbSize == sizeof(INPUT)`，错误 87 与 x64 `INPUT` 大小或联合体偏移不匹配一致。

后续版本已将自包含 `win-x64` 测试宿主的 `INPUT` 显式声明为 Windows x64 ABI：总大小 40 字节，输入联合体位于偏移 8；调用前还会验证实际大小和偏移。若 `SendInput` 仍失败，stderr 会额外记录进程架构、`INPUT` 大小、联合体偏移、前台窗口和焦点窗口句柄，以便区分 ABI、前台焦点和 UIPI 完整性级别限制。

该轮 `cleanup` 已移除布局 `E0200804`，但删除 `C:\Windows\System32\XiaoXiIme.ime` 时收到拒绝访问。测试宿主在真实按键场景中已加载该 IME，文件可能在测试进程退出与系统卸载之间仍被映射；下一轮需同时观察 ABI 修复后真实按键提交是否通过，以及清理阶段能否删除 System32 文件。若按键场景通过但文件仍无法删除，应单独修复测试宿主退出、布局卸载与清理重试之间的生命周期，而不能静默忽略残留。

开发机验证结果：`XiaoXiIme.IntegrationTestHost` 和完整解决方案生成成功，`XiaoXiIme.ImeModule.Tests` 的 59 个测试全部通过。当前 Visual Studio Test Explorer 使用项目筛选时未发现 `XiaoXiIme.IntegrationTests`，这是开发机测试发现问题，不改变 VM 必须执行自包含宿主的验收要求。

### 2026-07-26 第七轮旧 System32 映像阻塞结果

VM 在新 payload 启动时仍存在上一轮留下的 `C:\Windows\System32\XiaoXiIme.ime`。该文件没有布局引用，但无法立即删除；其长度与新源文件相同，SHA-256 不同，因此正式安装按安全约束拒绝覆盖。该轮没有进入 TSF 或集成测试，不能用于判断上一版 `SendInput` ABI 修复是否有效。

本轮输出还确认旧清理逻辑存在两个控制流问题：

- `uninstall-old` 将 System32 文件删除失败仅写入消息，仍返回成功，导致流程继续到必然失败的安装；
- 正式安装因“既有文件内容不同”而失败后仍运行 `imm-install-variant-probe`，短文件名变体成功并不能解决正式文件的版本冲突，属于无关诊断。

后续版本已增加确定性的重启恢复路径：当目标 IME 已无任何布局引用但因仍被系统映射而无法立即删除时，CLI 使用 `MoveFileExW(..., MOVEFILE_DELAY_UNTIL_REBOOT)` 安排下次 Windows 重启时删除。`uninstall-old` 会返回失败，并在结构化 `Data` 中输出 `RebootRequired: true` 和 `PendingDeletePaths`；本轮随即写报告并停止，不再继续安装。文件冲突也已与真正的 `ImmInstallIMEW` API 失败分类，只有后者才会运行变体探测。

下一轮需要分两次操作：

1. 使用包含此修复的新 payload 运行一次 `integration-run`。预期 `uninstall-old` 报告已安排删除并要求重启，进程非零退出；确认 `PendingDeletePaths` 包含 `C:\Windows\System32\XiaoXiIme.ime`。
2. 完整重启 Windows VM，不只是关闭 PowerShell或注销。重启后确认旧文件已消失，再使用同一 payload 重新运行 `integration-run`。第二次运行才用于验证 `PASS real-ime-keystroke-commit` 和最终清理。

如果第一次运行连延迟删除也无法安排，必须回传其中的原始 Win32 错误码；不要手工取得文件所有权、修改 ACL 或强制覆盖。开发机验证为 `XiaoXiIme.Cli.Tests` 22 个测试全部通过，完整解决方案生成成功。

### 2026-07-26 沙盒约束修正：被加载文件优先移动

实际实验环境在 Windows 重启后会丢失整个沙盒内容，因此上一版“安排重启删除并停止，重启后再运行”的恢复路径不适用于当前验证。后续清理策略调整为：

1. 确认目标 IME 文件已无任何键盘布局引用；
2. 先尝试立即删除；
3. 删除因映像仍被加载而失败时，优先使用 `MoveFileExW` 在 System32 同卷重命名为严格格式的隔离文件：`XiaoXiIme.retired-<UTC>-<GUID>.ime`；
4. 移动成功后，正式 `XiaoXiIme.ime` 路径已经释放，`uninstall-old` 保持成功并继续当前 `integration-run`；隔离路径进入 `Data.RetiredFilePaths`，不能描述成已删除；
5. 后续运行只扫描并清理严格匹配上述格式的隔离文件，不处理其他文件；
6. 只有移动也失败时才尝试安排重启删除并返回 `RebootRequired: true`。在当前沙盒中这只是最后诊断，不能作为正常恢复步骤。

下一版 payload 的预期行为是：`uninstall-old` 报告旧正式文件已移动到 `RetiredFilePaths`，随后安装新 `XiaoXiIme.ime` 并继续执行 TSF、IPC 和真实按键场景。若移动失败，必须回传移动操作的 Win32 错误码；不得要求操作者重启后继续同一沙盒实验。

开发机验证结果更新为：`XiaoXiIme.Cli.Tests` 29 个测试全部通过，完整解决方案生成成功。

### 2026-07-26 第八轮真实 IME 未激活结果

VM 返回结果确认上一版 `INPUT` x64 ABI 修复有效：`SendInput` 已成功注入全部四个键盘事件，不再返回 Win32 error 87。安装、x86/x64 TSF 验证、候选窗口状态和 IPC 场景也继续通过，但真实 EDIT 控件的最终文本为 `xxxx`，而不是“小希”。

`xxxx` 说明按下与抬起事件都进入了目标窗口并被普通键盘翻译为字符，但该轮宿主只调用了 `LoadKeyboardLayout`/`ActivateKeyboardLayout`，没有验证窗口线程实际 HKL，也没有获取、打开和复核 EDIT 控件的 IMM 输入上下文。因此该结果不能证明按键已经进入 XiaoXiIme 的 `ImeProcessKey`/`ImeToAsciiEx`，当前证据优先指向测试宿主没有确定性地启用目标 IME，而不是核心 `xx -> 小希` 或 HIMC 结果字符串逻辑失败。

后续版本在发送按键前增加以下严格前置条件：

- 设置前台窗口与 EDIT 焦点后激活 XiaoXiIme HKL，并用 `GetKeyboardLayout(0)` 验证当前窗口线程的实际 HKL 与预期一致；
- 使用 `ImmGetContext` 获取 EDIT 的 HIMC；
- 若输入上下文尚未打开，调用 `ImmSetOpenStatus(TRUE)`，随后再次用 `ImmGetOpenStatus` 验证；
- 最终文本仍不匹配时，在 stderr 中输出预期 HKL、实际 HKL、HIMC 和 IME 打开状态，区分布局回退、无输入上下文、IME 关闭和已经进入 IME 但提交失败。

下一轮若在注入前因 HKL 或 HIMC 前置条件失败，应直接分析新增状态，不再把普通字符上屏归因于结果字符串实现。只有实际 HKL 等于 XiaoXiIme、HIMC 非零且 `ImeOpen=true`，最终文本仍不是“小希”时，才继续向 `ImeProcessKey`/`ImeToAsciiEx` 调用可见性和 IMM32 消息生成方向增加诊断。

本轮还观察到 retired 映像仍可能因加载生命周期无法立即删除，并被再次移动到新的 retired 路径。该问题不影响正式路径复用，但在真实按键链路通过后仍需继续验证测试宿主退出与清理之间是否能最终释放所有 retired 文件，不能把 `RetiredFilePaths` 描述成已删除。

开发机验证结果：核心 `ProcessKey_SecondXAutomaticallyCommitsXiaoXi` 测试通过，`XiaoXiIme.ImeModule.Tests` 59 个测试全部通过，`XiaoXiIme.IntegrationTestHost` 和完整解决方案生成成功。

### 2026-07-26 第九轮 `TRANSMSG` ABI 根因

VM 返回结果确认真实按键场景的全部激活前置条件均成立：预期 HKL 与实际 HKL 都是 `E0200804`，EDIT 的 HIMC 非零，且 `ImeOpen=True`。最终文本从上一轮的 `xxxx` 变为 `xxx`，说明至少部分按键已经进入 IME 路径，但 IMM32 返回消息没有被 Windows 按预期解释。

检查发现项目将 Windows 原生 `TRANSMSG` 错误声明为包含 `HWND` 的结构。真实 `TRANSMSG` 仅包含 `message`、`wParam`、`lParam`；多出的首字段会导致 Windows 从 `TRANSMSGLIST` 读取时把后续所有字段错位。在 x64 下错误结构大小为 32 字节，而正确大小为 24 字节。原测试只使用同一错误托管声明写入和读取，因此无法发现与 Windows ABI 的偏差；测试缓冲区还按 `sizeof(uint)` 计算首消息偏移，没有考虑 x64 下 `TRANSMSGLIST.TransMsg` 位于偏移 8。

后续版本已做以下修复：

- 从 `TransMsg` 删除不存在的 `Hwnd` 字段；
- 消息构造器不再接受或写入窗口句柄；
- 测试按 `sizeof(TRANSMSGLIST) + sizeof(TRANSMSG)` 为两条消息分配缓冲区；
- 新增原生 ABI 断言：x64 下 `TRANSMSG` 大小为 24，字段偏移依次为 0、8、16，`TRANSMSGLIST` 首消息偏移为 8；x86 下对应为 12 和 0、4、8，首消息偏移为 4。

下一轮预期 Windows 能正确解释 `WM_IME_STARTCOMPOSITION`、带 `GCS_RESULTSTR` 的 `WM_IME_COMPOSITION` 和 `WM_IME_ENDCOMPOSITION`。若最终仍不是“小希”，新增诊断应转向实际 `ImeProcessKey`/`ImeToAsciiEx` 调用次数和每次返回消息，不再继续修改 HKL 或 HIMC 激活逻辑。

开发机清理旧增量输出后，`XiaoXiIme.ImeModule.Tests` 60 个测试全部通过。

### 2026-07-26 第十轮 payload 版本可辨识与按键调用轨迹

最新回传再次得到 `xxxx`，同时严格前置状态为 `ExpectedHkl=ActiveHkl=E0200804`、HIMC 非零且 `ImeOpen=True`。该输出与第八轮完全一致，不能单独证明第九轮 `TRANSMSG` ABI 修复后的二进制仍然失败：当前工作区已经包含正确的三字段 `TRANSMSG` 和 x86/x64 ABI 断言，而回传材料没有包含 payload 的生成日志、manifest 生成时间或可识别该修复版本的导出。

默认 `payload-build` 会重新执行 Release build 和每个 RID 的 NativeAOT publish；只有显式使用 `--no-build` 才会复用 `artifacts\integration-publish` 中的既有产物。后续构建和复制 payload 时不得使用 `--no-build`，并应保留控制台中的 payload 创建时间与 manifest SHA-256，以排除新测试宿主搭配旧 IME 的情况。

为使下一轮证据不再依赖行为推断，IME 新增两个只读诊断导出：

- `XiaoXiImeResetKeystrokeDiagnostics`：发送按键前清零调用轨迹；
- `XiaoXiImeGetKeystrokeDiagnostics`：返回固定 40 字节、版本为 1 的快照，包含 `ImeProcessKey`/`ImeToAsciiEx` 调用次数、最后虚拟键、Handled、HIMC 写入是否成功、写入的 `TRANSMSG` 数量和最终返回值。

自包含集成宿主会从 System32 加载已安装的同一 IME 并解析这两个导出。若导出不存在，场景会明确报告 payload 版本不匹配，要求不带 `--no-build` 重新构建并复制整个 payload。若最终文本仍不是“小希”，stderr 中的 `ImeTraceVersion=1` 按以下方式判断：

- `ImeProcessKeyCalls=0`：尽管 HKL/HIMC 状态成立，IMM32 没有调用项目导出，继续调查线程布局切换或系统 IME 选择；
- `ImeProcessKeyCalls>=2` 但 `ImeToAsciiExCalls=0`：`ImeProcessKey` 返回语义或 Windows 后续转换调度异常；
- `LastProcessHandled=False`：虚拟键或 key data 被错误翻译，先检查实际 VK 和修饰键；
- `ImeToAsciiExCalls>=2` 且 `LastToAsciiHandled=True`，但 `CompositionWriteSucceeded=False`：HIMC 内部锁定、扩容或 `ImmGenerateMessage` 失败；
- `MessageCount=2`、`ReturnValue=2` 且仍为普通字符：Windows 已收到成功返回但未正确消费 `TRANSMSGLIST`，继续核对实际 payload 中的原生布局与消息内容；
- 文本为“小希”且两个调用次数均至少为 2：第九轮 ABI 修复完成实机闭环。

本轮清理仍显示已加载的 retired 映像无法立即删除，只能再次移动到新的 retired 路径。新增测试宿主会显式 `FreeLibrary` 其诊断加载引用，但 Windows/IMM32 自身可能继续持有映像；下一轮同时观察 `cleanup` 是否停止产生新的 `RetiredFilePaths`。该残留问题与真实按键提交分开判定，不得用移动成功代替实际删除成功。

### 2026-08-02 `ImeInquire` UI 类名缓冲区根因

VM 回传确认安装、原生加载和全部必需导出、x86/x64 TSF ABI 与隔离 COM 激活均通过。集成宿主在 `candidate-window-state` 和 `ime-host-ipc` 通过后，以 `-1073740791`（`0xC0000409`）退出，且尚未输出真实按键场景的初始 IME 状态。这说明失败发生在真实 IME 激活初始化期间，属于原生 FailFast/栈缓冲区越界，而不是普通托管异常或安装失败。

根因是 `ImeInquire` 将 UI 窗口类名按 80 个 UTF-16 字符的容量写入，但 Windows 传统 IME 的 `IME_CLASSNAME_SIZE` 缓冲区只有 16 个字符；原类名 `XiaoXiImeUiWindow` 本身也无法连同空终止符装入该缓冲区。Windows 调用导出时因此触发栈保护并终止测试宿主。

后续版本已把 UI 类名缩短为 `XiaoXiImeUI`，集中声明 16 字符缓冲区长度，让窗口类注册和 `ImeInquire` 共用同一名称，并将回归测试改为使用真实原生缓冲区大小。下一轮必须重新构建 payload，不能使用 `--no-build`，并复制整个新目录到已还原 VM 后重试。

### 2026-08-02 布局提前激活导致 HIMC 未选择 IME

新 payload 不再发生 `0xC0000409`，证明 UI 类名缓冲区修复有效。安装、原生加载、全部导出、x86/x64 TSF、候选窗口状态和 IPC 仍全部通过。真实按键场景满足 `ExpectedHkl=ActiveHkl=E0200804`、HIMC 非零、`ImeOpen=True` 和 `ImmIsIme=True`，但输入 `xx` 后 EDIT 直接收到普通文本 `xx`。诊断快照为 `ImeInquireCalls=1`，而 `ImeSelectCalls=0`、`ImeSetActiveContextCalls=0`、`ImeProcessKeyCalls=0`、`ImeToAsciiExCalls=0`。

该轨迹说明 IME 映像已被查询且 UI 类注册成功，但当前 EDIT 的输入上下文没有经历 IME 选择和激活，按键因此仍走普通键盘路径。测试宿主此前使用 `LoadKeyboardLayout(..., KLF_ACTIVATE)`，在创建并聚焦测试窗口之前就激活了目标 HKL；窗口聚焦后再次调用 `ActivateKeyboardLayout` 时 HKL 没有发生变化，Windows 无需对新 HIMC 执行真实输入语言切换。

后续版本改为使用 `LoadKeyboardLayout(..., 0)` 仅加载目标布局，待窗口成为前台且 EDIT 获得焦点后，再由 `ActivateAndOpenIme` 唯一一次激活 HKL 并打开 HIMC。开发机已通过自包含测试宿主 Release win-x64 构建、`XiaoXiIme.ImeModule.Tests` 和完整解决方案 Release 构建。下一轮必须重新完整生成 payload；预期激活后 `ImeSelectCalls` 或 `ImeSetActiveContextCalls` 不再为 0，并且输入 `xx` 时出现 `ImeProcessKey`/`ImeToAsciiEx` 调用。

### 2026-08-02 延后激活仍未形成实际布局切换

新一轮 VM 输出与上一轮相同：`ExpectedHkl=ActiveHkl=E0200804`、HIMC 非零、`ImeOpen=True`、`ImmIsIme=True`，但 `ImeSelectCalls`、`ImeSetActiveContextCalls`、`ImeProcessKeyCalls` 和 `ImeToAsciiExCalls` 仍全部为 0，EDIT 最终收到普通文本 `xx`。这证明仅去掉 `LoadKeyboardLayout` 的 `KLF_ACTIVATE` 不足以保证切换发生；测试线程可能在加载前已经继承或处于 XiaoXiIme HKL，随后激活相同 HKL 仍然不会触发 Windows 的 IME 选择链。

后续测试宿主会预加载标准美式键盘布局 `00000409`。在测试窗口成为前台且 EDIT 获得焦点后，先切换到该非 IME 布局并验证线程 HKL，再切换回 XiaoXiIme，强制形成确定的输入语言变化。下一轮应重点确认 `ImeSelectCalls` 或 `ImeSetActiveContextCalls` 是否变为非零；若仍为 0，则不能继续归因于重复激活，需要转向 `ImmGetProperty(..., IGP_PROPERTY)` 返回 0、Windows 对该布局的传统 IME 识别以及注册契约排查。

### 2026-08-02 强制布局切换后 IMM32 仍未进入 IME

最新 VM 输出确认测试宿主已先切换到 `00000409`，再切换到 `E0200804`；最终 HKL、HIMC、打开状态、`ImmIsIME` 和 IME 文件名都正确，但 EDIT 仍直接收到 `xx`。调用轨迹中 `ImeInquireCalls`、`ImeSelectCalls`、`ImeSetActiveContextCalls`、`ImeProcessKeyCalls` 和 `ImeToAsciiExCalls` 全部为 0，`ImmGetProperty(..., IGP_PROPERTY)` 仍返回 0。该结果排除了“目标 HKL 没有实际变化”的假设。

测试宿主此前在切换布局前使用 `LoadLibraryEx` 手工加载 System32 中的 IME，以解析诊断导出并清零计数。下一版移除激活前的手工预加载，让 Windows/IMM32 独立完成首次加载、查询和选择；布局激活完成后才加载同一模块读取诊断快照，并且不再清零首次加载轨迹。若下一轮仍为全 0，则证据将集中到 Windows 19041 对该注册布局的传统 IME 加载契约、`ImeInquire` 加载失败路径或注册缓存，而不是测试宿主的 HKL 切换时序。

同时，`integration-run` 默认改为成功阶段仅输出摘要，并隐藏非交互 TSF 子进程的重复成功文本；失败阶段仍输出完整 stdout、stderr 和诊断对象，完整结果始终写入 JSON 报告。需要旧的完整成功日志时添加 `--verbose`。

### 2026-08-02 移除手工预加载后仍为零调用，切换正式 8.3 文件名

新一轮 VM 已使用移除激活前手工 `LoadLibraryEx` 的测试宿主。结果仍为 `ImmProperty=0`，且 `ImeInquire`、`ImeSelect`、`ImeSetActiveContext`、`ImeProcessKey`、`ImeToAsciiEx` 全部零调用，证明测试宿主预加载不是根因。注册项报告的 `Ime File` 为 `XIAOXIIME.IME`，其主文件名超过传统 IME 兼容使用的 8 个字符；这种状态可以让 `ImmInstallIME` 返回 HKL、`ImmIsIME` 返回真和 `ImmGetIMEFileName` 返回文件名，但 IMM32 运行时仍不加载 IME，因此与当前轨迹一致。

正式安装现在保留 payload 源文件 `XiaoXiIme.ime`，但复制到 System32 时统一命名为 8.3 兼容的 `XIAOXI.IME`，并将该短文件名传给 `ImmInstallIME`。集成宿主、清理逻辑和 VERSIONINFO 的 `OriginalFilename` 已同步；卸载仍兼容旧版 `XiaoXiIme.ime` 布局和残留。下一轮应首先确认 `ImmImeFile='XIAOXI.IME'`、`ImmProperty` 变为非零且 `ImeInquireCalls` 出现；随后再检查选择、激活和按键入口。

### 2026-08-02 短文件名已注册但 VM 会话仍命中旧 HKL 缓存

新一轮安装日志已明确显示 System32 目标为 `XIAOXI.IME`，注册表查找也只能在 `Ime File=XIAOXI.IME` 时找到布局；但同一测试进程中的 `ImmGetIMEFileName(E0200804)` 仍返回旧值 `XIAOXIIME.IME`。这不是新安装器继续注册长文件名，而是 Windows 当前会话仍保留此前相同布局 ID `E0200804` 对应的 IMM32/键盘布局缓存。该缓存继续指向已经删除的旧文件，因此 `ImmProperty=0`、生命周期导出零调用和普通文本 `xx` 都是预期后果，本轮不能用于判断短文件名方案是否有效。

同时发现卸载器过去把 8 位注册表布局 ID 直接转换为 64 位 `nint`。对于 `E0200804`，Windows x64 返回的真实 HKL 是符号扩展后的 `0xFFFFFFFFE0200804`，旧代码却向 `UnloadKeyboardLayout` 传入 `0x00000000E0200804`，无法正确卸载已缓存布局。卸载器现已按 Win32 `LONG` 规则符号扩展布局 ID，并增加回归测试。集成宿主也会在注册表期望 `XIAOXI.IME`、但 `ImmGetIMEFileName` 返回其他名称时立即报告 stale cache，而不是继续等待输入并误报转换失败。

下一轮必须先重启 VM，最好直接还原到安装任何 XiaoXiIme payload 之前的快照，再复制新 payload。验收的第一条件是输入前日志出现 `ImmImeFile='XIAOXI.IME'`；若仍为 `XIAOXIIME.IME`，说明运行环境没有真正清除旧会话缓存，不应继续分析按键导出。

### 2026-08-02 干净沙盒验证短文件名与首次查询，问题收敛到 `ImeInquire` 之后

操作者已明确说明本轮在重启并清空内容后的新沙盒中执行，不存在上一轮会话或文件残留。运行结果建立了新的可信基线：

- 正式安装路径为 `C:\Windows\System32\XIAOXI.IME`；
- `ImmGetIMEFileName(E0200804)` 返回 `XIAOXI.IME`，证明注册表、运行时 HKL 缓存和实际 System32 文件名已经一致；
- `ExpectedHkl` 与 `ActiveHkl` 都是 `0xFFFFFFFFE0200804`；
- EDIT 控件具有非零 HIMC，且 `ImeOpen=True`、`ImmIsIme=True`；
- `ImeInquireCalls=1`，证明 Windows/IMM32 已经真实加载该短文件名映像并调用项目的 `ImeInquire`；
- UI 类注册已尝试且成功；
- `ImmProperty=0`；
- `ImeSelectCalls=0`、`ImeSetActiveContextCalls=0`、`ImeProcessKeyCalls=0`、`ImeToAsciiExCalls=0`；
- 实体键盘输入 `xx` 后，EDIT 收到普通文本 `xx`，测试超时失败；
- `cleanup` 成功移除布局和 `C:\Windows\System32\XIAOXI.IME`，本轮没有文件残留阻塞证据。

这组证据推翻了上一节“本轮可能仍受旧 HKL 缓存污染”的条件性判断，也确认短文件名修复已经完成它应解决的部分：IMM32 能定位并首次查询 IME。当前失败发生在 `ImeInquire` 返回之后、输入上下文选择和按键分派之前。下一步必须检查 `ImeInquire` 返回的 `IMEINFO` 内容、返回值和 Windows 传统 IME 契约，解释为什么调用发生后 `ImmGetProperty(..., IGP_PROPERTY)` 仍为 0，并且当前 HIMC 没有进入 `ImeSelect`/`ImeSetActiveContext`。

后续调查边界如下：

1. 记录 `ImeInquire` 的实际返回值和完整 `IMEINFO` 字段，而不只记录调用次数与 `dwSystemInfoFlags`；至少包括 `dwPrivateDataSize`、`fdwProperty`、`fdwConversionCaps`、`fdwSentenceCaps`、`fdwUICaps`、`fdwSCSCaps` 和 `fdwSelectCaps`。
2. 将这些字段与 Windows SDK 中传统 IMM IME 的结构布局、允许标志和必需能力逐项核对，特别检查 `fdwProperty` 为什么没有通过 `ImmGetProperty(..., IGP_PROPERTY)` 体现。
3. 检查 `ImeInquire` 的原生签名、调用约定、布尔返回 ABI、`IMEINFO` 大小和字段偏移；现有独立加载与导出解析只能证明符号存在，不能证明 Windows 调用后的返回数据符合 ABI。
4. 若 `ImeInquire` 返回失败或字段不合法，优先修复该契约；在 `ImmProperty` 非零或选择回调出现之前，不分析 `ImeProcessKey`、`ImeToAsciiEx`、HIMC 结果字符串或 `TRANSMSG`。
5. 若 `ImeInquire` 返回成功且字段、ABI 均正确，但 `ImmProperty` 仍为 0，再增加 Windows 侧错误可见性和最小对照探测，调查 Windows 10 19041 是否拒绝当前能力组合。

以下方向已经有 VM 证据排除，除非后续出现与本轮矛盾的新证据，不得重复调查或再次作为首要修复：

- payload 源路径、System32 写权限和 `ImmInstallIMEW` 复制语义；
- PE 架构、NativeAOT 初始化、系统依赖和必需导出缺失；
- x86/x64 TSF ABI 或隔离 COM 激活；
- `SendInput` 的 x64 `INPUT` ABI；
- `TRANSMSG` 的字段布局；
- `ImeInquire` UI 类名 16 字符缓冲区越界；
- 测试宿主过早激活布局、没有发生真实 HKL 切换或激活前手工预加载；
- 长 IME 文件名和旧 `XIAOXIIME.IME` 会话缓存；
- 当前沙盒未重启或未清空。

### 2026-08-02 `ImeInquire` 完整返回契约诊断

现有 `ImeTraceVersion=3` 只能证明 Windows 进入了 `ImeInquire`，不能区分 UI 类注册后导出最终返回 0、`IMEINFO` 写入异常或 Windows 拒绝某个能力组合。为避免再次依靠推测，后续 payload 将诊断快照升级为版本 4，并新增以下字段：

- `ImeInquireReturnValue`；
- `PrivateDataSize`；
- `Property`；
- `ConversionCaps`；
- `SentenceCaps`；
- `UiCaps`；
- `SetCompositionStringCaps`；
- `SelectCaps`。

真实 `ImeInquire` 导出会在返回 Windows 前记录最终返回值和实际写入调用方缓冲区的七个 `IMEINFO` 字段。集成测试宿主会在 `IME STATE before input` 和失败信息中直接打印这些值。该修改不改变当前能力组合，只增加证据，避免在不知道 Windows 实际收到什么时盲目调整标志。

下一轮按以下顺序判断：

1. `ImeTraceVersion` 必须为 4，否则 payload 混用了旧 IME 或旧测试宿主。
2. 若 `ImeInquireReturnValue=0`，检查 UI 类注册或导出内部失败，不再分析 `ImmProperty`。
3. 若返回 1，但七个字段全为 0，检查指针、结构写入和诊断读取 ABI。
4. 若返回 1 且 `Property` 为预期非零值，但 `ImmProperty` 仍为 0，证据将直接指向 Windows 拒绝或没有缓存当前 `IMEINFO` 契约；随后才对能力标志做最小对照实验。
5. 若 `ImmProperty` 变为非零或 `ImeSelect`/`ImeSetActiveContext` 出现，再继续检查按键分派链。

开发机验证：`XiaoXiIme.ImeModule.Tests` 全部通过，完整解决方案 Release 构建成功。

### 2026-08-02 版本 4 零快照暴露诊断模块实例生命周期问题

新 payload 已确认 `ImeTraceVersion=4`，但输入前快照为 `ImeInquireCalls=0`、`ImeInquireReturnValue=0` 且七个 `IMEINFO` 字段全为 0。该结果不能按上一节第 2 条直接解释为真实 `ImeInquire` 返回失败，因为 UI 类注册状态为成功，而 UI 类注册也会在 IME 模块初始化时发生；更关键的是，测试宿主原来的顺序是先完成 `00000409 → XiaoXiIme` 切换，随后才调用 `LoadLibraryEx(XIAOXI.IME)` 获取诊断导出。

若 IMM32 在查询布局时临时加载 IME、调用 `ImeInquire` 后又释放映像，那么测试宿主随后手工加载的是同一路径的新模块实例。NativeAOT 静态诊断字段会随新实例重新初始化为 0，因此版本号和 UI 类注册状态有效，但无法看到前一实例的 `ImeInquire` 轨迹。上一轮版本 3 曾观察到 `ImeInquireCalls=1`，也不能保证每轮模块引用生命周期完全一致。

后续测试宿主调整为：

1. 创建窗口并让 EDIT 获得焦点；
2. 在切换到 XiaoXiIme 之前，从 System32 加载并持续持有 `XIAOXI.IME`；
3. 解析诊断导出并清零快照；
4. 再执行确定的 `00000409 → XiaoXiIme` 切换并打开 HIMC；
5. 在不释放该模块引用的情况下读取输入前和失败后的快照。

这样 IMM32 对同一路径的加载会复用当前进程中已持有的模块实例，诊断读取与 Windows 调用落在同一组 NativeAOT 静态状态上。本次重新引入激活前加载不是回退到已经排除的“预加载是否导致失败”假设，而是仅用于固定诊断实例；此前 VM 已证明预加载存在与否都不会改变普通文本 `xx` 和零选择/按键调用的行为。

下一轮判定规则更新为：

- 若同一持有实例仍为 `ImeInquireCalls=0`，才可确认本次布局切换没有调用 `ImeInquire`，应调查 IMM32 的查询缓存与触发条件；
- 若 `ImeInquireCalls>=1` 且 `ImeInquireReturnValue=0`，再检查导出内部失败；
- 若返回 1 且完整 `IMEINFO` 非零，但 `ImmProperty=0`，再调查 Windows 对能力组合的接受和缓存；
- 在 `ImeSelect`、`ImeSetActiveContext` 或 `ImeProcessKey` 出现前，仍不回到按键转换和 `TRANSMSG` 调查。

### 2026-08-02 同一模块实例仍为零：诊断清零晚于目标布局首次加载

新一轮已使用持续持有 `XIAOXI.IME` 模块实例的测试宿主，快照仍为版本 4 且全部 IME 调用为 0。该结果排除了“读取了后加载的新模块实例”的歧义，但检查实际执行顺序发现：宿主在创建窗口之前已经调用 `LoadKeyboardLayout(E0200804, 0)`，直到窗口获得焦点后才加载诊断模块并执行 `Reset`。传统 IMM32 很可能在首次 `LoadKeyboardLayout` 时完成唯一一次 `ImeInquire` 和属性缓存；后续 `ActivateKeyboardLayout` 不必再次查询。因而宿主把需要观测的首次查询发生在前，又在之后将同一实例的轨迹清零，最终零快照是测试时序造成的假阴性。

后续测试顺序已修正为：

1. 只预加载标准美式回退布局；
2. 创建窗口并让 EDIT 获得焦点；
3. 从 System32 加载并持续持有 `XIAOXI.IME`，解析诊断导出后清零；
4. 首次调用 `LoadKeyboardLayout(E0200804, 0)`；
5. 再执行 `00000409 → E0200804` 激活并打开 HIMC；
6. 从同一模块实例读取快照。

下一轮若 `ImeInquireCalls>=1`，应按返回值和完整 `IMEINFO` 继续判断 Windows 是否接受契约。只有在上述顺序下仍为 0，才调查 `LoadKeyboardLayout` 是否在进程外或其他不可观测实例中查询，以及 Windows 的布局属性缓存触发条件。不得再把本轮零快照解释为 `ImeInquire` 返回失败，也不得重复调查模块实例生命周期。

## 后续回归验证必须回传的信息

安装问题已闭环，后续仅在修改安装、TSF、IPC、payload 或集成运行流程后执行回归。回归输出至少应包含以下 stage，并保持 `cleanup` 早于 `report`：

- `uninstall-old`；
- `diagnostics-pre-install`；
- `native-ime-load-probe`；
- `install-x64`；
- `tsf-abi-x64`、`tsf-com-activation-x64`；
- `tsf-abi-x86`、`tsf-com-activation-x86`；
- `integration-tests`，其 stdout 应包含 `PASS candidate-window-state`、`PASS ime-host-ipc` 和 `PASS real-ime-keystroke-commit`；
- `cleanup`；
- 若安装失败，`diagnostics-post-install-failure`；
- 若安装失败，`imm-install-variant-probe`；
- `report`。

成功回归应满足进程退出码为 0、`cleanup` 成功，并且最终报告中的 `results` 包含 `cleanup`。如果 `--keep-installed` 被显式启用，则可以不包含 `cleanup`，但必须由操作者负责后续卸载。

真实按键上屏回归必须在已登录的交互式 Windows 桌面会话中执行。运行命令前应关闭可能抢占前台焦点的程序，不得通过计划任务的非交互会话、断开桌面的服务会话或远程后台执行器启动。测试宿主会创建并聚焦标题包含“请用键盘输入 xx”的窗口，操作者必须在输入框中通过键盘输入 `xx`，不得粘贴；若无法取得前台窗口或焦点，或者 60 秒内未得到“小希”，`integration-tests` 必须失败并在 stderr 中给出原因。

如果控制台粘贴受长度限制，应优先回传完整的 `results\integration.json`，不得只回传错误消息摘要。

## 诊断设计原则

- 不执行来源不明 DLL 的初始化代码；加载探测只使用安全标志。
- API-set 名称不按磁盘缺失文件处理，避免产生错误结论。
- 不通过跳过校验、吞掉错误或强行写注册表来让流程表面通过。
- 每个新增诊断都应回答一个明确问题，并进入结构化 JSON 报告。
- 所有可能修改系统的操作继续要求管理员权限和一次性 VM 确认令牌。
- 在根因确认前，不把 VM 特例硬编码为产品安装逻辑。

## 新对话接续提示

在新的对话中，先阅读本文档和用户回传的最新 `integration.json`，再检查当前 `ImeInstallationDiagnostics.cs`、`WindowsImeInstaller.cs` 与 `IntegrationTestRunner.cs`。不要从最初的 Win32 错误 2 重新猜测，也不要要求 VM 安装开发工具；应从最新一轮结构化字段继续缩小问题范围。

## `xx` 输入“小希”的真实输入闭环实施计划

安装排障已经闭环，下一阶段转为验证真实 Windows 编辑控件中的按键、组合和结果字符串上屏链路。最小目标是：激活已安装的 XiaoXiIme 后，在 Win32 `EDIT` 控件中连续输入两次 `x`，第二个 `x` 到达后由 IME 直接提交“小希”，最终控件文本必须严格等于“小希”。本阶段暂不要求显示可交互候选窗口，也不要求再按空格确认。

实施步骤：

1. 在默认内存词典中增加 `xx -> 小希` 的确定性词条。
2. 在 IME 核心中保留第一个 `x` 的组合状态，并在第二个 `x` 后自动提交唯一候选“小希”、清空组合状态。
3. 增加核心回归测试，验证第一个 `x` 只建立组合，第二个 `x` 返回完整的双字符结果字符串。
4. 增加 IME 消息层回归测试，验证提交结果继续通过 HIMC 的 `GCS_RESULTSTR` 和 `WM_IME_COMPOSITION` 传递，而不是绕过输入法协议直接写宿主文本。
5. 扩展自包含 `XiaoXiIme.IntegrationTestHost.exe`：创建真实 Win32 `EDIT` 控件，查找并激活 XiaoXiIme 布局，将窗口置于前台，提示用户通过键盘输入 `xx`，泵送窗口消息，并在有限超时内读取控件文本。
6. 集成场景成功时输出 `PASS real-ime-keystroke-commit`；无法取得交互式前台窗口、无法激活布局、用户未在时限内完成输入、超时或最终文本不匹配时必须输出明确失败原因并返回非零退出码。
7. 在开发机运行核心、IME 模块和集成宿主测试，并构建完整解决方案与 payload，随后复制到纯净 VM 做实机回归。

实现约束：

- 必须经过现有 `ImeProcessKey` → `ImeToAsciiEx` → composition/result string → `WM_IME_*` 链路；测试代码不得直接把“小希”设置到编辑框。
- VM 仍不得依赖 .NET SDK、系统级 Runtime、Visual Studio 或额外诊断工具。
- 真实按键场景要求交互式桌面会话，并且必须由操作者在获得焦点的测试输入框中通过键盘输入 `xx`；不得使用 `SendInput` 或粘贴代替真实按键。
- 测试必须使用有限超时和消息泵，不得无限等待。
- 若真实宿主测试暴露消息顺序、HIMC 缓冲区或多字符结果字符串问题，应修复协议实现，不能降低断言或伪造通过。

验收标准：

- 核心层：第一个 `x` 返回正在组合且 reading 为 `x`；第二个 `x` 返回 `CommitText == "小希"` 且组合结束。
- 消息层：提交结果包含 `GCS_RESULTSTR`，HIMC 中保存的结果字符串为完整的“小希”。
- VM 端：`integration-tests` 的 stdout 在原有两个 PASS 之外包含 `PASS real-ime-keystroke-commit`。
- 完整集成流程仍保持 `cleanup` 早于 `report`，最终进程退出码为 0，报告包含成功的 `cleanup` 与 `integration-tests`。

开发机完成实现后重新构建 payload：

```powershell
dotnet run --project .\src\XiaoXiIme.Cli\XiaoXiIme.Cli.csproj -- payload-build --output .\artifacts\integration-payload
```

复制到 VM 后仍执行文档开头的 `integration-run` 命令。新的成功输出必须同时证明安装、x86/x64 TSF 验证、IPC 测试、真实 `xx` 按键提交“小希”和最终清理均通过。
