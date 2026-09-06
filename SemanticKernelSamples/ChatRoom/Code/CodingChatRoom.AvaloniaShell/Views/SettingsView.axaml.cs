using System;
using System.Diagnostics;

using Avalonia.Controls;
using Avalonia.Interactivity;

using CodingChatRoom.AvaloniaShell.ViewModels;

namespace CodingChatRoom.AvaloniaShell.Views;

/// <summary>
/// 显示模型服务与 Windows 沙箱设置。
/// </summary>
public partial class SettingsView : UserControl
{
    /// <summary>
    /// 初始化设置视图。
    /// </summary>
    public SettingsView()
    {
        InitializeComponent();
    }

    private void OnWindowsSandboxServerAddressLostFocus(object? sender, RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel viewModel
            && viewModel.TestWindowsSandboxConnectionCommand.CanExecute(null))
        {
            viewModel.TestWindowsSandboxConnectionCommand.Execute(null);
        }
    }

    private async void OnInstallOrUpdateLanguageServerClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            var startInfo = new ProcessStartInfo("dotnet")
            {
                UseShellExecute = false,
                CreateNoWindow = false,
            };
            startInfo.ArgumentList.Add("tool");
            startInfo.ArgumentList.Add("update");
            startInfo.ArgumentList.Add("-g");
            startInfo.ArgumentList.Add("roslyn-language-server");

            using Process? process = Process.Start(startInfo);
            if (process is not null)
            {
                await process.WaitForExitAsync();
            }
        }
        catch
        {
            // 忽略
        }
    }
}
