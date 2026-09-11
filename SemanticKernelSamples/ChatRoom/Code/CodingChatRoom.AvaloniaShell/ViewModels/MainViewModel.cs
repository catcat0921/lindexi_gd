using System;
using System.Collections.ObjectModel;
using Avalonia.Controls;
using System.Threading.Tasks;
using System.Windows.Input;
using CodingChatRoom.AvaloniaShell.Services;

namespace CodingChatRoom.AvaloniaShell.ViewModels;

/// <summary>组合独立工作任务与常驻导航。</summary>
public sealed class MainViewModel : ViewModelBase
{
    private readonly SettingsViewModel? _settingsViewModel;
    private Func<Task<CodingChatRuntime>>? _createRuntimeAsync;
    private WorkTaskItemViewModel _activeWorkTask;
    private bool _isSettingsOpen;
    private bool _isHistoryOpen;

    /// <summary>创建未连接模型的界面。</summary>
    public MainViewModel() : this(new SessionListViewModel(), new ChatViewModel()) { }

    /// <summary>使用现有聊天上下文初始化首个任务。</summary>
    public MainViewModel(SessionListViewModel sessions, ChatViewModel chat)
    {
        ArgumentNullException.ThrowIfNull(sessions);
        ArgumentNullException.ThrowIfNull(chat);
        _activeWorkTask = new WorkTaskItemViewModel(GetTaskName(1), chat, sessions) { IsActive = true };
        WorkTasks.Add(_activeWorkTask);
        Subscribe(_activeWorkTask);
        OpenSettingsCommand = new SimpleAsyncCommand(OpenSettingsAsync, () => _settingsViewModel is not null);
        OpenHistoryCommand = new SimpleAsyncCommand(() => OpenHistoryAsync(false));
        OpenTaskHistoryCommand = new SimpleAsyncCommand(() => OpenHistoryAsync(true));
        CloseHistoryCommand = new SimpleCommand(() => { IsHistoryOpen = false; IsSettingsOpen = false; });
        CreateWorkTaskCommand = new SimpleAsyncCommand(CreateWorkTaskAsync, () => _createRuntimeAsync is not null);
        ActivateWorkTaskCommand = new SimpleCommand<WorkTaskItemViewModel>(task => { if (task is not null) Activate(task); });
        RenameWorkTaskCommand = new SimpleCommand<WorkTaskItemViewModel>(task => { if (task is not null) task.IsEditing = !task.IsEditing; });
        DeleteWorkTaskCommand = new SimpleAsyncCommand<WorkTaskItemViewModel>(DeleteTaskAsync, task => task is not null && !task.IsWorking && WorkTasks.Count > 1);
    }

    internal MainViewModel(SessionListViewModel sessions, ChatViewModel chat, CodingChatSettingsService settings) : this(sessions, chat)
    {
        _settingsViewModel = new SettingsViewModel(settings, () => IsSettingsOpen = false);
    }

    internal void ConfigureTaskFactory(Func<Task<CodingChatRuntime>> factory) => _createRuntimeAsync = factory;

    /// <summary>获取工作任务列表。</summary>
    public ObservableCollection<WorkTaskItemViewModel> WorkTasks { get; } = [];
    /// <summary>获取当前任务。</summary>
    public WorkTaskItemViewModel ActiveWorkTask => _activeWorkTask;
    /// <summary>获取当前任务聊天。</summary>
    public ChatViewModel ChatViewModel => _activeWorkTask.Chat;
    /// <summary>获取当前历史页面。</summary>
    public SessionListViewModel SessionListViewModel => _activeWorkTask.Sessions;
    /// <summary>获取全局设置。</summary>
    public SettingsViewModel? SettingsViewModel => _settingsViewModel;
    /// <summary>获取聊天页可见性。</summary>
    public bool IsChatOpen => !IsHistoryOpen && !IsSettingsOpen;
    /// <summary>获取历史页可见性。</summary>
    public bool IsHistoryOpen { get => _isHistoryOpen; private set { if (SetField(ref _isHistoryOpen, value)) OnPropertyChanged(nameof(IsChatOpen)); } }
    /// <summary>获取设置页可见性。</summary>
    public bool IsSettingsOpen { get => _isSettingsOpen; private set { if (SetField(ref _isSettingsOpen, value)) OnPropertyChanged(nameof(IsChatOpen)); } }
    /// <summary>新建独立任务。</summary>
    public ICommand CreateWorkTaskCommand { get; }
    /// <summary>切换任务而不停止后台执行。</summary>
    public ICommand ActivateWorkTaskCommand { get; }
    /// <summary>编辑任务名。</summary>
    public ICommand RenameWorkTaskCommand { get; }
    /// <summary>删除空闲任务，不删除历史文件。</summary>
    public ICommand DeleteWorkTaskCommand { get; }
    /// <summary>打开全部历史。</summary>
    public ICommand OpenHistoryCommand { get; }
    /// <summary>按任务路径打开历史。</summary>
    public ICommand OpenTaskHistoryCommand { get; }
    /// <summary>返回聊天。</summary>
    public ICommand CloseHistoryCommand { get; }
    /// <summary>打开设置。</summary>
    public ICommand OpenSettingsCommand { get; }

    private static string GetTaskName(int number) => $"{Avalonia.Application.Current?.FindResource("WorkTaskText") ?? "工作任务"} {number}";

    private void Subscribe(WorkTaskItemViewModel task)
    {
        task.Sessions.SessionOpened += (_, _) => Activate(task);
        task.PropertyChanged += (_, _) => (DeleteWorkTaskCommand as SimpleAsyncCommand<WorkTaskItemViewModel>)?.RaiseCanExecuteChanged();
    }

    private void Activate(WorkTaskItemViewModel task)
    {
        _activeWorkTask.IsActive = false;
        _activeWorkTask = task;
        task.IsActive = true;
        IsHistoryOpen = false;
        IsSettingsOpen = false;
        OnPropertyChanged(nameof(ActiveWorkTask));
        OnPropertyChanged(nameof(ChatViewModel));
        OnPropertyChanged(nameof(SessionListViewModel));
    }

    private async Task CreateWorkTaskAsync()
    {
        if (_createRuntimeAsync is null) return;
        CodingChatRuntime runtime = await _createRuntimeAsync();
        var task = new WorkTaskItemViewModel(GetTaskName(WorkTasks.Count + 1),
            new ChatViewModel(runtime.ChatManager, runtime.Application, runtime.WorkspaceController, runtime.ModelDisplayName),
            new SessionListViewModel(runtime.Application), runtime);
        WorkTasks.Add(task);
        Subscribe(task);
        Activate(task);
    }

    private async Task DeleteTaskAsync(WorkTaskItemViewModel? task)
    {
        if (task is null || task.IsWorking || WorkTasks.Count <= 1) return;
        if (task.Runtime is not null) await task.Runtime.DisposeAsync();
        task.Detach();
        task.Chat.Dispose();
        WorkTasks.Remove(task);
        if (ReferenceEquals(task, _activeWorkTask)) Activate(WorkTasks[0]);
    }

    private async Task OpenHistoryAsync(bool filterByPath)
    {
        var sessions = SessionListViewModel;
        sessions.SearchText = filterByPath ? ChatViewModel.NextRunWorkspacePath ?? string.Empty : string.Empty;
        IsSettingsOpen = false;
        IsHistoryOpen = true;
        await sessions.LoadAsync();
    }

    private async Task OpenSettingsAsync()
    {
        if (_settingsViewModel is null) return;
        IsHistoryOpen = false;
        IsSettingsOpen = true;
        await _settingsViewModel.LoadAsync();
    }
}
