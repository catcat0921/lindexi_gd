using System;
using System.Threading.Tasks;
using AgentLib;
using AgentLib.Coding;
using AgentLib.Core;
using AgentLib.Core.AgentApiManagers.LanguageModelProviders;
using AgentLib.Logging;
using CodingChatRoom.AvaloniaShell.ViewModels;

namespace CodingChatRoom.AvaloniaShell.Services;

internal sealed class CodingWorkTaskRuntime : IAsyncDisposable
{
    private readonly CodingAgent _codingAgent;
    private bool _isDisposed;

    public CodingWorkTaskRuntime(
        Guid workTaskId,
        string displayName,
        AgentApiEndpointManager endpointManager,
        FileCopilotChatLogger chatLogger,
        CopilotChatManager chatManager,
        CodingAgent codingAgent,
        CodingChatApplication application,
        CodingWorkspaceController workspaceController,
        ChatViewModel chatViewModel)
    {
        WorkTaskId = workTaskId;
        DisplayName = displayName;
        EndpointManager = endpointManager;
        ChatLogger = chatLogger;
        ChatManager = chatManager;
        _codingAgent = codingAgent;
        Application = application;
        WorkspaceController = workspaceController;
        ChatViewModel = chatViewModel;
    }

    public Guid WorkTaskId { get; }

    public string DisplayName { get; set; }

    public AgentApiEndpointManager EndpointManager { get; }

    public FileCopilotChatLogger ChatLogger { get; }

    public CopilotChatManager ChatManager { get; }

    public CodingChatApplication Application { get; }

    public CodingWorkspaceController WorkspaceController { get; }

    public ChatViewModel ChatViewModel { get; }

    public bool IsWorking => Application.IsRunActive || Application.IsCompressionActive || WorkspaceController.IsChangingWorkspace;

    public string StatusText => IsWorking ? "工作中" : "空闲";

    public async ValueTask DisposeAsync()
    {
        if (_isDisposed)
        {
            return;
        }

        Application.StopActiveRun();
        ChatViewModel.Dispose();
        await _codingAgent.DisposeAsync().ConfigureAwait(false);
        _isDisposed = true;
    }
}
