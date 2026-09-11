using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using CodingChatRoom.AvaloniaShell.Services;

namespace CodingChatRoom.AvaloniaShell.ViewModels;

public sealed class MainViewModel : ViewModelBase
{
    private readonly CodingChatRuntime? _runtime;
    private readonly SettingsViewModel? _settingsViewModel;
    private readonly ChatViewModel _placeholderChatViewModel = new();
    private WorkTaskItemViewModel? _activeWorkTask;
    private Guid? _historyTargetWorkTaskId;
    private bool _isSettingsOpen;
    private bool _isHistoryOpen;

    public MainViewModel()
        : this(new SessionListViewModel(), new ChatViewModel())
    {
    }

    public MainViewModel(SessionListViewModel sessionListViewModel, ChatViewModel chatViewModel)
    {
        ArgumentNullException.ThrowIfNull(sessionListViewModel);
        ArgumentNullException.ThrowIfNull(chatViewModel);
        SessionListViewModel = sessionListViewModel;
        WorkTasks.CollectionChanged += OnWorkTasksCollectionChanged;
        WorkTasks.Add(new WorkTaskItemViewModel());
        ActiveWorkTask = WorkTasks[0];
        OpenSettingsCommand = new SimpleAsyncCommand(static () => Task.CompletedTask, static () => false);
        InitializeCommands();
    }

    internal MainViewModel(CodingChatRuntime runtime)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        _runtime = runtime;
        WorkTasks.CollectionChanged += OnWorkTasksCollectionChanged;
        foreach (CodingWorkTaskRuntime workTaskRuntime in runtime.WorkTasks)
        {
            WorkTasks.Add(new WorkTaskItemViewModel(workTaskRuntime));
        }

        SessionListViewModel = new SessionListViewModel(GetHistoryApplication);
        _settingsViewModel = new SettingsViewModel(runtime.SettingsService, CloseSettings);
        OpenSettingsCommand = new SimpleAsyncCommand(OpenSettingsAsync, () => !IsSettingsOpen);
        InitializeCommands();
        WorkTaskItemViewModel? active = WorkTasks.FirstOrDefault(task => task.WorkTaskId == runtime.ActiveWorkTaskId)
                                        ?? WorkTasks.FirstOrDefault();
        if (active is not null)
        {
            SetActiveWorkTask(active);
        }
    }

    public ObservableCollection<WorkTaskItemViewModel> WorkTasks { get; } = [];

    public string WorkTaskCountText => $"{WorkTasks.Count}";

    public WorkTaskItemViewModel? ActiveWorkTask
    {
        get => _activeWorkTask;
        private set
        {
            if (ReferenceEquals(_activeWorkTask, value)) return;
            if (_activeWorkTask is not null) _activeWorkTask.IsActive = false;
            _activeWorkTask = value;
            if (_activeWorkTask is not null) _activeWorkTask.IsActive = true;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ChatViewModel));
            OnPropertyChanged(nameof(IsChatOpen));
        }
    }

    public SessionListViewModel SessionListViewModel { get; }

    public ChatViewModel ChatViewModel => ActiveWorkTask?.ChatViewModel ?? _placeholderChatViewModel;

    public SettingsViewModel? SettingsViewModel => _settingsViewModel;

    public bool IsHistoryOpen
    {
        get => _isHistoryOpen;
        private set
        {
            if (SetField(ref _isHistoryOpen, value)) OnPropertyChanged(nameof(IsChatOpen));
        }
    }

    public bool IsSettingsOpen
    {
        get => _isSettingsOpen;
        private set
        {
            if (SetField(ref _isSettingsOpen, value))
            {
                OnPropertyChanged(nameof(IsChatOpen));
                (OpenSettingsCommand as SimpleAsyncCommand)?.RaiseCanExecuteChanged();
            }
        }
    }

    public bool IsChatOpen => !IsSettingsOpen && !IsHistoryOpen && ActiveWorkTask is not null;

    public ICommand CreateWorkTaskCommand { get; private set; } = null!;
    public ICommand ActivateWorkTaskCommand { get; private set; } = null!;
    public ICommand CreateSessionForTaskCommand { get; private set; } = null!;
    public ICommand OpenTaskHistoryCommand { get; private set; } = null!;
    public ICommand EditWorkTaskNameCommand { get; private set; } = null!;
    public ICommand SaveWorkTaskNameCommand { get; private set; } = null!;
    public ICommand CancelWorkTaskNameCommand { get; private set; } = null!;
    public ICommand DeleteWorkTaskCommand { get; private set; } = null!;
    public ICommand OpenHistoryCommand { get; private set; } = null!;
    public ICommand CloseHistoryCommand { get; private set; } = null!;
    public ICommand OpenSettingsCommand { get; }

    private void InitializeCommands()
    {
        CreateWorkTaskCommand = new SimpleAsyncCommand(CreateWorkTaskAsync, () => _runtime is not null);
        ActivateWorkTaskCommand = new SimpleAsyncCommand<WorkTaskItemViewModel>(ActivateWorkTaskAsync, task => task is not null);
        CreateSessionForTaskCommand = new SimpleAsyncCommand<WorkTaskItemViewModel>(CreateSessionForTaskAsync, CanChangeTaskSession);
        OpenTaskHistoryCommand = new SimpleAsyncCommand<WorkTaskItemViewModel>(OpenTaskHistoryAsync, task => task?.Runtime is not null);
        EditWorkTaskNameCommand = new SimpleCommand<WorkTaskItemViewModel>(task =>
        {
            if (task is null) return;
            task.EditedDisplayName = task.DisplayName;
            task.IsEditing = true;
        });
        SaveWorkTaskNameCommand = new SimpleAsyncCommand<WorkTaskItemViewModel>(SaveWorkTaskNameAsync, task =>
            task?.Runtime is not null && !string.IsNullOrWhiteSpace(task.EditedDisplayName));
        CancelWorkTaskNameCommand = new SimpleCommand<WorkTaskItemViewModel>(task =>
        {
            if (task is not null) task.IsEditing = false;
        });
        DeleteWorkTaskCommand = new SimpleAsyncCommand<WorkTaskItemViewModel>(DeleteWorkTaskAsync, task =>
            task?.Runtime is not null && !task.IsWorking);
        OpenHistoryCommand = new SimpleAsyncCommand(OpenGlobalHistoryAsync, allowConcurrentExecutions: true);
        CloseHistoryCommand = new SimpleCommand(() => IsHistoryOpen = false);
        SessionListViewModel.SessionOpened += (_, taskId) =>
        {
            WorkTaskItemViewModel? target = WorkTasks.FirstOrDefault(task => task.WorkTaskId == taskId);
            if (target is not null) SetActiveWorkTask(target);
            IsHistoryOpen = false;
        };
    }

    private CodingChatApplication? GetHistoryApplication()
        => WorkTasks.FirstOrDefault(task => task.WorkTaskId == _historyTargetWorkTaskId)?.Runtime?.Application
           ?? ActiveWorkTask?.Runtime?.Application;

    private async Task CreateWorkTaskAsync()
    {
        if (_runtime is null) return;
        CodingWorkTaskRuntime runtime = await _runtime.CreateWorkTaskAsync().ConfigureAwait(true);
        var item = new WorkTaskItemViewModel(runtime);
        WorkTasks.Add(item);
        SetActiveWorkTask(item);
    }

    private async Task ActivateWorkTaskAsync(WorkTaskItemViewModel? task)
    {
        if (task is null) return;
        SetActiveWorkTask(task);
        if (_runtime is not null) await _runtime.SetActiveWorkTaskAsync(task.WorkTaskId).ConfigureAwait(true);
    }

    private void SetActiveWorkTask(WorkTaskItemViewModel task)
    {
        ActiveWorkTask = task;
        IsHistoryOpen = false;
        IsSettingsOpen = false;
    }

    private static bool CanChangeTaskSession(WorkTaskItemViewModel? task)
        => task?.Runtime?.Application.CanChangeSession == true;

    private async Task CreateSessionForTaskAsync(WorkTaskItemViewModel? task)
    {
        if (task?.Runtime is null) return;
        await task.Runtime.Application.CreateNewSessionAsync().ConfigureAwait(true);
        task.ChatViewModel.ClearDraft();
        if (_runtime is not null) await _runtime.SaveWorkTasksAsync().ConfigureAwait(true);
        SetActiveWorkTask(task);
    }

    private async Task OpenTaskHistoryAsync(WorkTaskItemViewModel? task)
    {
        if (task?.Runtime is null) return;
        _historyTargetWorkTaskId = task.WorkTaskId;
        SessionListViewModel.Navigate(
            task.WorkTaskId,
            task.DisplayName,
            task.Runtime.WorkspaceController.NextRunWorkspacePath ?? string.Empty);
        IsSettingsOpen = false;
        IsHistoryOpen = true;
        await SessionListViewModel.LoadAsync().ConfigureAwait(true);
    }

    private async Task OpenGlobalHistoryAsync()
    {
        _historyTargetWorkTaskId = ActiveWorkTask?.WorkTaskId;
        SessionListViewModel.Navigate(ActiveWorkTask?.WorkTaskId, ActiveWorkTask?.DisplayName, string.Empty);
        IsSettingsOpen = false;
        IsHistoryOpen = true;
        await SessionListViewModel.LoadAsync().ConfigureAwait(true);
    }

    private async Task SaveWorkTaskNameAsync(WorkTaskItemViewModel? task)
    {
        if (_runtime is null || task is null) return;
        await _runtime.RenameWorkTaskAsync(task.WorkTaskId, task.EditedDisplayName).ConfigureAwait(true);
        task.DisplayName = task.Runtime!.DisplayName;
        task.IsEditing = false;
    }

    private async Task DeleteWorkTaskAsync(WorkTaskItemViewModel? task)
    {
        if (_runtime is null || task is null || task.IsWorking) return;
        int index = WorkTasks.IndexOf(task);
        await _runtime.DeleteWorkTaskAsync(task.WorkTaskId).ConfigureAwait(true);
        task.Dispose();
        WorkTasks.Remove(task);
        if (ReferenceEquals(ActiveWorkTask, task))
        {
            ActiveWorkTask = WorkTasks.Count == 0 ? null : WorkTasks[Math.Min(index, WorkTasks.Count - 1)];
        }
    }

    private async Task OpenSettingsAsync()
    {
        if (_settingsViewModel is null) return;
        IsHistoryOpen = false;
        IsSettingsOpen = true;
        await _settingsViewModel.LoadAsync().ConfigureAwait(true);
    }

    private void CloseSettings() => IsSettingsOpen = false;

    private void OnWorkTasksCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        => OnPropertyChanged(nameof(WorkTaskCountText));
}
