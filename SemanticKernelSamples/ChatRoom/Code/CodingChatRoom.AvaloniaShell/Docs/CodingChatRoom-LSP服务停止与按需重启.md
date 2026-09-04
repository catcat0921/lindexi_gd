# CodingChatRoom LSP 服务停止与按需重启

## 目标

输入区工具栏提供“结束 LSP 服务”按钮，用于释放当前工作区缓存中的 Roslyn Language Server 进程。停止操作不改变工作区路径、工具集合或会话状态。

## 生命周期语义

- 工作区路径只在下一次新对话运行开始时应用。
- `CodingWorkspaceCache` 跨对话复用工作区工具状态。
- `RoslynAgentTools` 是缓存中的一个属性，负责 Roslyn LSP 客户端的启动、停止和释放。
- 点击“结束 LSP 服务”时，直接取出并释放当前 LSP 客户端。
- 不取消 Agent，不等待当前运行结束，不禁止运行期间点击。
- 没有工作区缓存或没有活动 LSP 时，停止操作是正常空操作。
- `code_search`、`find_symbol`、`find_all_references` 在调用前检查 LSP；没有客户端时按需重新启动。
- `get_projects_in_solution` 和 `get_files_in_project` 使用项目目录，不会启动 LSP。
- 某次 LSP 启动失败不会永久标记缓存不可用，下一次符号工具调用仍会重新尝试。

## 调用链

```text
ChatView.StopLanguageServerButton
  -> ChatViewModel.StopLanguageServerCommand
  -> CodingChatApplication.StopLanguageServerAsync
  -> ICodingChatRunner.StopLanguageServerAsync
  -> CodingAgent.StopLanguageServerAsync
  -> CodingWorkspaceCache.StopLanguageServerAsync
  -> RoslynAgentTools.StopLanguageServerAsync
  -> RoslynLspClient.DisposeAsync
```

没有引入专用控制接口、工厂、租约或工作区事务。

## 用户反馈

- 成功停止：`LSP 服务已结束，将在下次调用符号工具时重新启动`
- 无活动服务：`当前没有正在运行的 LSP 服务`
- 停止失败：`结束 LSP 服务失败：<错误信息>`

反馈同时显示在输入区状态文本和当前会话的系统消息中。
