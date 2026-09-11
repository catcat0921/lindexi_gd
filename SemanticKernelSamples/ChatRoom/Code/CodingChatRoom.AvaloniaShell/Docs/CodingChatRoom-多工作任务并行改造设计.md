# CodingChatRoom 多工作任务并行改造设计

## 1. 状态与范围

本文是基于当前工作区源码的设计草案，不表示功能已经实现。需求与待确认交互见[需求文档](CodingChatRoom-多工作任务并行改造需求.md)。新增类型、文件及存储字段均为建议命名，实施时优先复用已有能力，不为内部组合机械增加接口。

推荐方向：**一个桌面进程、多个任务级运行时、一个全局历史目录**。不要通过取消当前任务再切换会话模拟并发，也不把“多个工作任务”解释为必须启动多个应用进程。

## 2. 当前项目探索

### 2.1 技术基础

- `CodingChatRoom.AvaloniaShell.csproj`：桌面 WinExe，`net10.0`，Nullable 开启，Avalonia `12.1.0`，使用编译绑定；主项目未显式指定 LangVersion。
- 主项目引用 `AgentLib/AgentLib.Coding`，核心聊天与持久化类型来自 AgentLib。
- 测试项目为 `CodingChatRoom.AvaloniaShell.Tests`，使用 MSTest `4.0.2`、Avalonia.Headless `12.1.0`，已有 Moq。
- `ChatRoom/Directory.Build.props` 使用 artifacts 输出；`Directory.Build.targets` 有构建后消息目标。工作区内未检索到 `global.json`。本次不调整 SDK、TFM 或依赖版本。

### 2.2 已核实的代码结构

下列路径除特别标注外，相对 Shell 项目目录。

| 文件/符号 | 当前行为 | 对改造的影响 |
| --- | --- | --- |
| `Views/MainView.axaml` | 顶部新建/历史/设置按钮，右侧聊天与独立历史页面，无常驻任务边栏 | 恢复边栏但不恢复旧历史侧栏 |
| `ViewModels/MainViewModel.cs` | 只持有一个只读 ChatViewModel；历史打开时 LoadAsync，未重置搜索 | 改为任务集合与活跃任务投影，入口显式设置过滤 |
| `App.axaml.cs` | 初始化一个 CodingChatRuntime，由它创建一套聊天与历史 ViewModel | 组合根改成宿主与任务作用域 |
| `Services/CodingChatStartup.cs` | 创建一个 EndpointManager、ChatManager、CodingAgent、WorkspaceController、Application | 必须拆出可重复创建的任务运行时流程 |
| `Services/CodingChatRuntime.cs` | 保存上述单份对象，DisposeAsync 只委托 CodingAgent | 需要多任务生命周期及完整停止/保存/释放流程 |
| `Services/CodingChatApplication.cs` | 单个 SelectedSession、运行 CTS、运行/压缩状态；繁忙时不能更换会话 | 保留单任务约束，不保留全局繁忙约束 |
| `Services/ICodingChatRunner.cs` | CodingAgentChatRunner 只有一个 `_activeRun`，运行中消息注入到它 | 每个任务独立 runner，不能全局共用 |
| `ViewModels/ChatViewModel.cs` | 保存输入、附件、模型和运行选项，订阅 ChatManager/Application | 每任务一份，切换时不得销毁后台状态 |
| `ChatViewModel.SelectedModel` | 写入 `_chatManager.AgentApiEndpointManager.PrimaryModel` | 共用可变 EndpointManager 会导致模型串扰 |
| `Services/CodingWorkspaceController.cs` | 一个下一次运行路径，变更门闩及路径校验 | 每任务一份，复用其规范化规则 |
| `Services/ICodingChatSessionStore.cs` | 文件适配器绑定 ChatManager，恢复 AgentSession 时创建发送上下文 | 原适配器不能直接当无状态全局单例 |
| `ViewModels/SessionListViewModel.cs` | 绑定一个 Application；按标题或 WorkspacePath 包含匹配搜索；首次加载后缓存 | 分离全局历史索引与目标任务操作，增加增量更新 |
| `Infrastructure/CodingChatRoomPaths.cs` | 用户 LocalApplicationData 下 CodingChatRoom，包含 Sessions、Logs、ShellSettings.json | 增加任务元数据文件，不搬迁旧会话 |
| `AgentLib/AgentLib.Coding/CodingAgent.cs`（工作区路径） | 一个 `_workspaceCache`、缓存 gate、附加工具源、独占资源释放 | 每任务独立 CodingAgent，避免切路径替换另一个任务资源 |

当前组合关系：App → CodingChatRuntime → 单份 ChatManager / CodingAgent / Application / WorkspaceController → 单份 ChatViewModel；历史页面也依赖这一个 Application。

### 2.3 易遗漏的现有行为

1. `CodingChatApplication.SendMessageAsync` 在运行中不是启动另一条请求，而是调用 `InjectMessageAsync`。多任务设计应保留这一任务内语义。
2. 发送时将 `NextRunWorkspacePath` 写入会话，`CreateNewSessionAsync` 不重建 WorkspaceController；保留工作路径已有基础，但新会话摘要应在首次发送前也获得正确路径。
3. `OpenSessionAsync` 恢复/选择会话，但当前代码没有在该方法内同步 WorkspaceController 的路径。因此跨路径恢复不能只复用旧入口而不补齐规则。
4. 运行代码可能在最终保存前将 `_isRunActive` 清零；循环迭代还包括压缩与错误后的延迟。直接绑定 IsRunActive 会过早显示空闲，也不足以控制会话替换和删除。
5. `StopActiveRun` 只取消当前运行 CTS；不能据此承诺覆盖循环等待、加载和所有压缩路径。需要任务级完整生命周期取消与等待。
6. `CodingImageAnalysisToolSource` 在启动时绑定 ChatManager；WindowsSandboxToolSource 与设置服务存在实例关联。复制主 Agent 但共享这些有上下文的对象仍会串扰。
7. 历史过滤是标题或路径的忽略大小写包含匹配，不是规范化后的精确目录相等。自动填入路径可复用，但须明确其搜索语义。

### 2.4 探索边界

本轮为静态源码探索，未运行应用或并发试验。尚未证明模型客户端、文件日志存储、全部附加工具源和远端沙盒支持多实例同时使用；不把它们视为已验证线程安全。实施前应继续针对这些边界检查并添加测试。

## 3. 目标架构与所有权

### 3.1 应用级宿主

建议由应用级宿主管理：

- 全局配置的读取与版本发布、应用路径、主线程 dispatcher。
- 工作任务集合、活跃任务 ID、任务运行时的创建和销毁。
- 全局历史摘要目录、会话挂载注册表、会话文件读写协调。
- 工作任务元数据存储以及退出协调。

现有 `CodingChatRuntime` 可逐步演进为该宿主，或拆出职责清晰的宿主类型；不再让 MainViewModel 从宿主取得“唯一 ChatManager”。

### 3.2 任务级作用域

建议新增 `CodingWorkTaskRuntime`，拥有以下独立实例：

- CopilotChatManager 与当前挂载的 CopilotChatSession。
- CodingAgent 与它的工作区工具缓存、LSP 生命周期。
- CodingAgentChatRunner、CodingChatApplication、CodingWorkspaceController。
- ChatViewModel，含消息集合、输入草稿、待发送图片、模型及思考强度选项。
- 任务级生命周期 CTS、操作状态、可等待的执行完成任务。
- 绑定本任务 ChatManager 的图片分析工具源和其他有会话上下文的工具源。

建议先给每任务创建独立 EndpointManager，从同一配置快照初始化，隔离现有 `PrimaryModel` 写入行为；不要只隔离 ViewModel 字段而共用其可变底层管理器。此方案仍需验证 LoadConfiguration 产生的客户端和模型实例是否共享可变状态。长期若经测量需要共享客户端，可显式按请求选模型，但不作为首期前置重构。

任务可惰性创建昂贵工作区资源，但首期不在切换可见任务时释放运行时。删除任务或退出时才沿统一生命周期释放；空闲资源回收留待后续测量。

### 3.3 会话级共享协调

建议新增轻量 `SessionAttachmentRegistry`，维护 SessionId → WorkTaskId 的单一可写挂载关系。它只约束挂载，不给历史会话增加永久 TaskId 所有权。

- 两个任务并发打开同一历史时，注册表原子保留目标会话，后到请求定位既有任务或返回明确冲突。
- 需要覆盖加载过程的临时保留，不能加载完才登记。
- 协调同一会话的保存、重命名、删除，避免覆盖写及删除后迟到保存复活。
- 释放任务运行时后移除挂载关系，历史记录不变。

不同会话文件操作可并发；短时保护元数据或同一 SessionId 不等于全局串行 Agent 执行。跨进程同时打开数据目录的保护不由进程内注册表自动解决，需要另行确认应用单实例约束或增加文件级协调。

## 4. 数据模型建议

### 4.1 工作任务持久化记录

建议 `WorkTasks.json` 放在 `CodingChatRoomPaths.RootDirectory`，独立于 Sessions 和 ShellSettings；由单一存储组件串行、原子更新。

| 字段 | 用途 |
| --- | --- |
| SchemaVersion | 元数据版本和未来迁移 |
| ActiveWorkTaskId | 上次活跃任务，可空 |
| Tasks | 按保存顺序排列的任务记录 |
| WorkTaskId | 稳定 Guid，名称/路径变化不改变 |
| DisplayName | 用户任务名，与会话标题独立 |
| WorkspacePath | 已应用的规范化路径，可空 |
| ModelReference | 指向模型配置中的稳定键，不存密钥或对象 |
| ReasoningEffort | 思考强度，允许未指定 |
| EnableAutomaticCompression / EnableDotNetRun | 任务级运行选项 |
| IsLoopIterationEnabled | 用户偏好；恢复时不得触发自动运行 |
| CurrentSessionId | 当前会话引用，可空，不复制会话正文 |
| WasInterrupted | 可选的中断提示标记，不作为恢复执行指令 |

模型稳定引用格式应基于配置实际可用标识实施，不能只用显示名称推断唯一性。任务配置与单次执行参数建议用不可变记录；可观察 UI 状态仍由 ViewModel 提供。

### 4.2 内存运行信息

任务保存 OperationState、LastOutcome、RunId、生命周期 CTS 和 CompletionTask。每次运行形成不可变快照，包含 WorkTaskId、SessionId、工作路径、模型引用、运行选项和配置版本。

只在真正准备好且取得必要操作保留后启动执行；迟到事件通过 TaskId/SessionId/RunId 校验，不以当前活跃任务归属输出。

## 5. 界面与导航

- `MainViewModel` 增加任务集合和 ActiveWorkTask，当前 ChatViewModel 从活跃任务投影并发送属性变更通知。
- 建议新增 `WorkTaskItemViewModel` 和 `WorkTaskListView`，任务项命令携带稳定 ID。
- `MainView.axaml` 调整为两列：左列常驻工作任务导航，右列聊天/历史/设置。
- 聊天区复用现有 ChatView；切换 DataContext 不销毁后台 ChatViewModel。切换时检查 ChatView 后台事件、滚动与附件处理是否假定固定 DataContext。
- 保留每任务输入、待发送附件及消息；新建会话时按需求规则清空会话草稿。滚动位置可作为任务 UI 状态保存，不需要首期持久化到磁盘。
- 历史页面显示恢复目标名称，区分“本任务当前会话”与“已在其他任务打开”。
- 新增文字进入 `Styles/Strings.axaml`，延续现有资源化方式；状态需具备文字、焦点与键盘可访问性。

建议使用一个历史导航上下文，包含入口类型、TargetWorkTaskId、初始 SearchText 和导航版本号。任务入口填入已应用路径，全局入口填空字符串；之后搜索框仍自由编辑。异步加载完成时校验导航版本，不能让旧入口响应覆盖新入口搜索。

## 6. 历史数据与恢复流程

### 6.1 分离目录浏览与运行上下文

`SessionListViewModel` 不再直接以某一个 Application.Sessions 作为全局真相源：

- 全局历史目录读取持久化摘要，并合并未落盘的新会话摘要。
- 创建、保存、重命名、删除会话发布增量变更，UI dispatcher 更新 ObservableCollection。
- 保留按需加载，但已加载历史也接收其他后台任务的更新；提供显式刷新。
- 文件适配器中依赖 ChatManager 的 AgentSession 恢复仍由目标任务执行，不把活跃 ChatManager 注入全局目录服务。
- `CodingChatHistoryLoader.cs` 可作为继续评估的复用候选；本轮未核实其完整契约，实施时先检查，避免新增重复加载器。

### 6.2 打开历史的事务性步骤

以下基于需求文档的推荐交互：

1. 捕获入口目标 ID 和选中 SessionId，检查目标仍存在。
2. 查询挂载注册表；已挂载则导航至该任务，不再加载第二份。
3. 取得目标任务的会话变更保留和目标 SessionId 的加载保留；繁忙时返回明确选择，不替换运行会话。
4. 检查待离开的会话及草稿，完成必要保存；存在未发送内容时提示丢弃或取消，不能静默丢失。
5. 读取历史数据并检查路径差异，按确认结果准备新路径和恢复环境。
6. 在临时对象中恢复消息及 AgentSession；使用目标任务模型配置，不提前改变可见会话或任务路径。
7. 全部成功后在 UI 线程提交会话、路径、注册表关系和历史标记，释放原挂载。
8. 任何失败或取消均释放保留并保持原会话/路径/模型不变，记录错误。

当前文件适配器需要改造才能满足第 6 步的显式目标恢复；不能假设现有 CreateManualSendMessageContextAsync 与当前会话无关。

### 6.3 新建与删除

- 新建会话：取得任务空闲操作保留，处理旧草稿/保存，创建新的 SessionId，立即设置任务路径，更新挂载和全局摘要；保留任务配置。
- 删除任务：阻止新操作，必要时确认取消整个执行链，等待收尾保存，释放事件订阅、ViewModel、Agent/LSP 等资源，再提交任务元数据删除；不调用 DeleteSessionAsync。
- 删除历史会话：未挂载会话可以独立删除；已挂载且运行中的会话禁止删除。空闲挂载会话需确认，先为关联任务准备新会话再完成删除；删除失败回滚，不移除任务。
- 重命名历史会话：按 SessionId 协调保存并更新所有视图；不能从磁盘加载过期副本覆盖正在增长的消息。首期可禁用工作中会话改名。

## 7. 并发与状态机

### 7.1 两级约束

- 应用级：不同任务可真正同时执行，不持有跨整个模型调用的全局锁。
- 任务级：至多一条顶层执行链，涵盖循环迭代和压缩；运行中发送走注入。发送、会话更换、删除、配置修改通过同一操作状态约束，不能只依赖按钮禁用。
- 启动期间注入请求可能早于 runner 设置 `_activeRun`，需要明确准备完成信号或暂时禁用注入，避免竞态。
- 锁只保护状态转换，避免长时间持锁执行 UI 回调或网络调用；原子状态变更与异步工作分离。

### 7.2 推荐状态

| 内部状态 | 左侧主状态 | 说明 |
| --- | --- | --- |
| Idle | 空闲 | 可新建/更换会话 |
| LoadingSession / ChangingWorkspace | 工作中 | 辅助文案说明正在准备，不允许并发替换 |
| Starting / Running | 工作中 | 包含工具调用和图片子智能体 |
| Compressing | 工作中 | 覆盖手动与自动压缩 |
| LoopWaiting | 工作中 | 循环仍有效，不得提前空闲 |
| Stopping | 工作中 | 显示正在停止，等待真实完成 |
| Saving | 工作中 | 最终持久化尚未完成 |
| Disposed | 不再展示 | 确认资源释放后移除 |

正常流程：Idle → Starting → Running →（Compressing / LoopWaiting → Running）→ Saving → Idle。取消走 Stopping → Saving → Idle；失败也必须进入清理并记录 LastOutcome。空闲与成功是不同维度，可显示失败或上次中断提示。

任务列表从这一统一生命周期状态派生，不直接等同于 ChatViewModel.IsRunning 或 Application.IsRunActive。仅在最终保存和必要收尾完成后解除会话替换限制。

### 7.3 取消、循环与退出

- 每任务持有整条执行链的 CTS，传播到模型、工具、循环延迟、可取消的加载/压缩入口。
- 停止循环必须阻止后续轮次，不能只取消当轮请求后继续下一轮。
- 对当前没有 CancellationToken 参数的依赖先核实契约；必要时沿依赖链补齐。不能通过 UI 提前标空闲掩盖后台仍在工作。
- 宿主持有并观察每个后台执行 Task；界面切换不取消，但绝不是丢弃任务或异常的 fire-and-forget。
- 收尾保存使用独立、有界的取消策略，不沿用已取消运行 token，也不无限等待。
- 退出先停止接收新操作、确认运行任务，再取消并等待全部执行，保存任务元数据，最后释放各运行时。超时/保存失败展示明确结果，不静默丢数据。

## 8. 配置与工具隔离

### 8.1 设置边界

全局保存模型目录、连接凭据、沙盒地址等应用配置；任务保存模型引用、路径与运行选项。修改任务模型不得改变其他任务或应用默认模型。

建议全局配置保存后发布带版本的快照，新任务采用最新配置；运行中请求保留开始时快照，空闲任务在下一次操作前更新。模型删除时任务标记不可用并要求选择，不静默回退。

沙盒设置现有方案强调实时更新，实施前需与 `CodingChatRoom-沙盒工具配置实时更新.md` 约定对齐：至少更新未来调用可用配置，不把已启动操作中途切到另一端点；任务级工具实例都应接到更新，不能只更新启动时的一个实例。

### 8.2 资源与并发风险

| 风险 | 首期处理建议 |
| --- | --- |
| 多 LSP 实例占用内存 | 每任务独占、按需启动；停止语言服务只影响所属任务；删除释放 |
| 同目录写文件/构建输出冲突 | 明示风险，推荐独立目录/worktree；不宣称实例隔离等于磁盘隔离 |
| 日志与会话覆盖写 | 核实 FileCopilotChatLogger/FileCopilotChatSessionStore 的实例和文件锁语义；按 SessionId 协调 |
| 图片分析使用错误模型或会话 | 每任务创建绑定自身 ChatManager 的工具源 |
| 沙盒结果或“最后一次构建日志”串扰 | 检查任务目录、输出文件名和最近日志状态的作用域；必须按任务/运行定位 |
| 全局可变状态 | 审计 AgentLib.Coding 工具及依赖中的 static 可变字段、当前目录修改和缓存 |
| API 限流与成本增加 | 错误归属单任务；先验证双任务并发，再决定是否增加并发额度与排队状态 |
| 后台输出大量 UI 更新 | 通过现有 dispatcher 更新；测量后批量派发，不跨线程改集合 |

上表后四类底层资源并未在本轮完成全面审计，属于实施前门槛而非已证实缺陷。确有不可并发外部资源时只协调该资源，不让全部 Agent 回退为全局串行。

## 9. 持久化、迁移与故障恢复

1. 增加 WorkTasks 元数据路径和版本化读写；原 Sessions/Logs 格式与目录保留。
2. 不存在任务文件时按建议创建一个默认任务，不为所有历史会话自动创建任务。
3. 写任务文件采用临时文件加原子替换及单写入者；失败可见，不覆盖旧有效记录。
4. 会话先保存成功，再提交 CurrentSessionId 引用。空会话如需跨重启保留，应先保存空会话数据。
5. 两种文件不构成数据库事务；崩溃恢复时允许历史中出现未被任务引用的会话，禁止因引用缺失删除历史。
6. 任务文件损坏时保留原文件并提示恢复/重置，不静默清空；未知更高版本不擅自覆写。
7. 缺失会话保留任务配置并提示；路径不存在可查看历史但禁止执行；模型缺失要求重新选择。
8. 重复会话引用只恢复一个可写挂载，其他任务明确提示并进入未挂载状态，不恢复多个可写副本。
9. 重启全部恢复为空闲，保留中断提示，不自动恢复执行、循环、工具调用或未完成取消。

## 10. 建议改动范围

| 区域 | 改动 |
| --- | --- |
| App / CodingChatStartup / CodingChatRuntime | 分离应用宿主和任务级创建，接入退出协调 |
| CodingChatApplication / ICodingChatRunner | 保留任务内执行逻辑，完善生命周期、取消、启动注入竞态和最终保存时序 |
| CodingWorkspaceController / CodingChatRunOptions | 复用路径处理与选项，加入任务快照语义 |
| ICodingChatSessionStore / 历史加载相关服务 | 拆分全局摘要浏览和目标任务上下文恢复，增加同会话操作协调 |
| MainViewModel / SessionListViewModel | 任务集合、导航上下文、历史目标选择与全局增量更新 |
| ChatViewModel / ChatView 后台逻辑 | 每任务实例、事件解绑、模型隔离与可切换 DataContext 验证 |
| MainView / 新任务列表视图 / Strings | 两列布局、任务动作与状态、文案资源 |
| CodingChatSettingsService | 全局配置向多个任务的版本化传播，保留既有设置行为 |
| CodingChatRoomPaths / 新任务存储 | 独立工作任务元数据，不迁移或删除历史 |
| AgentLib / AgentLib.Coding | 仅针对实际确认的共享状态或取消契约缺口做必要改动，不先重写核心库 |
| Shell 测试项目 | 新增任务隔离、导航、恢复、取消、存储测试，并更新旧单任务结构断言 |

## 11. 分阶段实施建议

1. **确认交互与基线**：确认需求文档第 10 节，构建现有项目并运行现有相关测试；记录既有问题，避免与本改造混淆。
2. **补齐底层并发审计**：检查模型客户端、日志、沙盒、构建日志和 LSP 资源；使用两个独立 runtime 的受控测试验证可同时进入执行。
3. **引入任务作用域**：拆分组合根与任务运行时，落实模型、路径、runner、取消及工具源隔离；先保持单任务 UI 兼容。
4. **建立全局历史协调**：实现摘要目录、挂载注册表、事务式恢复和单会话写保护；完成重复打开及失败回滚测试。
5. **接入任务界面**：新增边栏、活跃任务切换、任务动作、历史入口过滤和统一状态；保留后台执行。
6. **完善持久化与生命周期**：任务文件迁移、删除、停止循环、退出、多资源释放与错误可见性。
7. **并发回归与体验验证**：双任务流式输出、切换界面、历史恢复、取消和资源压力测试，通过验收后再讨论上限与优化。

每阶段保持可构建和原行为回归，不通过同时大改所有共享核心类型才能首次验证功能。

## 12. 测试与验证设计

复用 MSTest 与 Avalonia.Headless。优先通过实际任务宿主/导航入口测试，不为测试新加 public 或 InternalsVisibleTo；现有公开 API 发生变化时补充契约测试。外部模型和工具执行可用受控替身，不 mock 被验证的协调逻辑。

### 12.1 必要测试组

- 运行隔离：A/B 使用不同路径与模型；两个 runner 都进入启动屏障后再释放完成，证明真实重叠，不靠 Sleep。
- 输出归属：运行期间切换活跃任务，验证文本、工具结果、附件和异常仍属于原 SessionId。
- 取消隔离：停止 A 不取消 B；循环等待和压缩期间停止不能启动后续轮次。
- 状态边界：最终保存未完成时不显示空闲，不允许会话替换；失败保留结果提示。
- 任务管理：改名不改会话标题；删除任务不调用会话删除；删除运行任务等待完整收尾。
- 会话新建：保留路径和任务配置，生成新 SessionId，原会话仍存在。
- 历史导航：任务入口写路径，全局入口清空；异步响应乱序不能恢复旧搜索；目标 ID 不随活跃任务漂移。
- 恢复竞争：两个任务同时打开相同会话只能有一个可写挂载；加载/保存失败完全回滚；目标删除可安全取消。
- 历史同步：后台保存/重命名/删除刷新全局目录，已缓存页面不漏更新；迟到保存不能复活已删除会话。
- 恢复与迁移：无文件、坏文件、未知版本、缺失模型/路径/会话、重复引用；重启绝不自动运行。
- 资源释放：切换任务不释放 Agent；删除只释放本任务资源；退出等待所有已观察执行任务。
- UI 结构：边栏在聊天/历史/设置页可见；任务按钮命令目标正确；ChatView 切换绑定、滚动、附件操作不串任务。

### 12.2 现有回归入口

重点回归 `CodingChatApplicationTests`、`CodingChatSendingTests`、`ChatViewModelTests`、`CodingWorkspaceControllerTests`、`HistoryInteractionTests`、`ShellStructureTests`、`CodingChatStartupTests`、`SettingsViewModelTests`、`ChatOutputVisualTests`、`ChatAutoScrollStateTests`。底层有改动时增加 AgentLib.Coding 的对应测试。

真实 API/LSP/沙盒并发验证与纯单元测试分开，避免将网络、服务器和模型额度依赖引入普通测试。资源测试记录双任务及更多任务的内存、后台 UI 更新量、取消延迟与释放结果，再决定并发上限。

## 13. 本轮交付与验证说明

本轮仅新增需求和设计两份 Markdown 文档，不修改业务源码、项目配置或旧设计文档。当前实现结论来自文件读取和符号追踪；新增方案与尚待审计部分已分别标识。未执行构建、测试或实际并发运行，因此不声称当前项目构建通过，也不声称上述设计已获得运行验证。
