using System;
using System.ComponentModel;
using CodingChatRoom.AvaloniaShell.Services;

namespace CodingChatRoom.AvaloniaShell.ViewModels;

public sealed class WorkTaskItemViewModel : ViewModelBase, IDisposable
{
    private readonly CodingWorkTaskRuntime? _runtime;
    private string _displayName;
    private string _editedDisplayName;
    private bool _isEditing;
    private bool _isActive;

    public WorkTaskItemViewModel()
    {
        WorkTaskId = Guid.NewGuid();
        _displayName = "工作任务";
        _editedDisplayName = _displayName;
        ChatViewModel = new ChatViewModel();
        ChatViewModel.WorkTaskDisplayName = _displayName;
    }

    internal WorkTaskItemViewModel(CodingWorkTaskRuntime runtime)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        _runtime = runtime;
        WorkTaskId = runtime.WorkTaskId;
        _displayName = runtime.DisplayName;
        _editedDisplayName = _displayName;
        ChatViewModel = runtime.ChatViewModel;
        ChatViewModel.WorkTaskDisplayName = _displayName;
        runtime.Application.StateChanged += OnRuntimeStateChanged;
        runtime.WorkspaceController.PropertyChanged += OnWorkspacePropertyChanged;
    }

    public Guid WorkTaskId { get; }

    public string DisplayName
    {
        get => _displayName;
        internal set
        {
            if (SetField(ref _displayName, value))
            {
                ChatViewModel.WorkTaskDisplayName = value;
                OnPropertyChanged(nameof(HistoryTargetText));
            }
        }
    }

    public string EditedDisplayName
    {
        get => _editedDisplayName;
        set => SetField(ref _editedDisplayName, value);
    }

    public bool IsEditing
    {
        get => _isEditing;
        internal set => SetField(ref _isEditing, value);
    }

    public bool IsActive
    {
        get => _isActive;
        internal set
        {
            if (SetField(ref _isActive, value))
            {
                OnPropertyChanged(nameof(IsBackgroundWorking));
                OnPropertyChanged(nameof(DisplayStatusText));
            }
        }
    }

    public bool IsWorking => _runtime?.IsWorking == true;

    public bool IsBackgroundWorking => IsWorking && !IsActive;

    public string StatusText => _runtime?.StatusText ?? "空闲";

    public string DisplayStatusText => IsBackgroundWorking ? "后台工作中" : StatusText;

    public string WorkspacePath => _runtime?.WorkspaceController.NextRunWorkspacePath ?? "未设置工作路径";

    public string HistoryTargetText => $"打开到：{DisplayName}";

    public ChatViewModel ChatViewModel { get; }

    internal CodingWorkTaskRuntime? Runtime => _runtime;

    internal void RefreshState()
    {
        OnPropertyChanged(nameof(IsWorking));
        OnPropertyChanged(nameof(StatusText));
        OnPropertyChanged(nameof(DisplayStatusText));
        OnPropertyChanged(nameof(IsBackgroundWorking));
        OnPropertyChanged(nameof(WorkspacePath));
    }

    public void Dispose()
    {
        if (_runtime is null)
        {
            return;
        }

        _runtime.Application.StateChanged -= OnRuntimeStateChanged;
        _runtime.WorkspaceController.PropertyChanged -= OnWorkspacePropertyChanged;
    }

    private void OnRuntimeStateChanged(object? sender, EventArgs e) => RefreshState();

    private void OnWorkspacePropertyChanged(object? sender, PropertyChangedEventArgs e) => RefreshState();
}
