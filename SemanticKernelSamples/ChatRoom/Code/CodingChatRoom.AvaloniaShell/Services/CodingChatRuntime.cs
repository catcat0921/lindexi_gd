using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AgentLib;
using AgentLib.Core.AgentApiManagers.LanguageModelProviders;
using CodingChatRoom.AvaloniaShell.Infrastructure;

namespace CodingChatRoom.AvaloniaShell.Services;

internal sealed class CodingChatRuntime : IAsyncDisposable
{
    private readonly AgentApiManagerConfiguration _configuration;
    private readonly IMainThreadDispatcher _mainThreadDispatcher;
    private readonly CodingWorkTaskStore _workTaskStore;
    private readonly List<CodingWorkTaskRuntime> _workTasks = [];

    public CodingChatRuntime(
        CodingChatRoomPaths paths,
        AgentApiManagerConfiguration configuration,
        IMainThreadDispatcher mainThreadDispatcher,
        CodingChatSettingsService settingsService)
    {
        Paths = paths;
        _configuration = configuration;
        _mainThreadDispatcher = mainThreadDispatcher;
        SettingsService = settingsService;
        _workTaskStore = new CodingWorkTaskStore(paths.WorkTasksFile);
    }

    public CodingChatRoomPaths Paths { get; }

    public CodingChatSettingsService SettingsService { get; }

    public IReadOnlyList<CodingWorkTaskRuntime> WorkTasks => _workTasks;

    public Guid? ActiveWorkTaskId { get; private set; }

    public async Task InitializeWorkTasksAsync(CancellationToken cancellationToken = default)
    {
        CodingWorkTaskDocument? document = await _workTaskStore.LoadAsync(cancellationToken).ConfigureAwait(false);
        IReadOnlyList<CodingWorkTaskRecord> records = document?.Tasks is { Count: > 0 }
            ? document.Tasks
            : [new CodingWorkTaskRecord { WorkTaskId = Guid.NewGuid(), DisplayName = "工作任务 1" }];

        foreach (CodingWorkTaskRecord record in records)
        {
            CodingWorkTaskRuntime runtime = await CodingChatStartup.CreateWorkTaskRuntimeAsync(
                Paths,
                _configuration,
                _mainThreadDispatcher,
                SettingsService,
                record.WorkTaskId == Guid.Empty ? Guid.NewGuid() : record.WorkTaskId,
                string.IsNullOrWhiteSpace(record.DisplayName) ? $"工作任务 {_workTasks.Count + 1}" : record.DisplayName,
                record.WorkspacePath,
                record.CurrentSessionId,
                cancellationToken).ConfigureAwait(false);
            _workTasks.Add(runtime);
        }

        ActiveWorkTaskId = document?.ActiveWorkTaskId is Guid activeId
                           && _workTasks.Any(task => task.WorkTaskId == activeId)
            ? activeId
            : _workTasks[0].WorkTaskId;
        await SaveWorkTasksAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<CodingWorkTaskRuntime> CreateWorkTaskAsync(CancellationToken cancellationToken = default)
    {
        var runtime = await CodingChatStartup.CreateWorkTaskRuntimeAsync(
            Paths,
            _configuration,
            _mainThreadDispatcher,
            SettingsService,
            Guid.NewGuid(),
            $"工作任务 {_workTasks.Count + 1}",
            workspacePath: null,
            currentSessionId: null,
            cancellationToken).ConfigureAwait(false);
        _workTasks.Add(runtime);
        ActiveWorkTaskId = runtime.WorkTaskId;
        await SaveWorkTasksAsync(cancellationToken).ConfigureAwait(false);
        return runtime;
    }

    public async Task DeleteWorkTaskAsync(Guid workTaskId, CancellationToken cancellationToken = default)
    {
        CodingWorkTaskRuntime runtime = _workTasks.Single(task => task.WorkTaskId == workTaskId);
        _workTasks.Remove(runtime);
        await runtime.DisposeAsync().ConfigureAwait(false);
        if (ActiveWorkTaskId == workTaskId)
        {
            ActiveWorkTaskId = _workTasks.FirstOrDefault()?.WorkTaskId;
        }

        await SaveWorkTasksAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task RenameWorkTaskAsync(Guid workTaskId, string displayName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        CodingWorkTaskRuntime runtime = _workTasks.Single(task => task.WorkTaskId == workTaskId);
        runtime.DisplayName = displayName.Trim();
        await SaveWorkTasksAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task SetActiveWorkTaskAsync(Guid workTaskId, CancellationToken cancellationToken = default)
    {
        if (_workTasks.All(task => task.WorkTaskId != workTaskId))
        {
            throw new InvalidOperationException("指定的工作任务不存在。");
        }

        ActiveWorkTaskId = workTaskId;
        await SaveWorkTasksAsync(cancellationToken).ConfigureAwait(false);
    }

    public Task SaveWorkTasksAsync(CancellationToken cancellationToken = default)
    {
        var document = new CodingWorkTaskDocument
        {
            ActiveWorkTaskId = ActiveWorkTaskId,
            Tasks = _workTasks.Select(task => new CodingWorkTaskRecord
            {
                WorkTaskId = task.WorkTaskId,
                DisplayName = task.DisplayName,
                WorkspacePath = task.WorkspaceController.NextRunWorkspacePath,
                CurrentSessionId = task.ChatManager.SelectedSession.SessionId.ToString("D"),
            }).ToArray(),
        };
        return _workTaskStore.SaveAsync(document, cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await SaveWorkTasksAsync().ConfigureAwait(false);
        foreach (CodingWorkTaskRuntime runtime in _workTasks)
        {
            await runtime.DisposeAsync().ConfigureAwait(false);
        }

        _workTasks.Clear();
    }
}
