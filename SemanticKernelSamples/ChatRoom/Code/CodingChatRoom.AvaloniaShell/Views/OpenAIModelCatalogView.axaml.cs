using System;
using System.Linq;

using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;

using CodingChatRoom.AvaloniaShell.Services;
using CodingChatRoom.AvaloniaShell.ViewModels;

namespace CodingChatRoom.AvaloniaShell.Views;

public partial class OpenAIModelCatalogView : UserControl
{
    private readonly IOpenAIModelCatalogClient _client = new OpenAIModelCatalogClient();

    public OpenAIModelCatalogView()
    {
        InitializeComponent();
    }

    private async void OnLoadClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not ProviderSettingsViewModel provider)
        {
            return;
        }

        LoadButton.IsEnabled = false;
        ShowStatus("正在获取模型列表…", isError: false);
        try
        {
            OpenAIModelCatalogResult result = await _client.GetModelIdsAsync(provider.EndPoint, provider.ApiKey);
            if (!result.IsSuccessful)
            {
                ModelsList.ItemsSource = null;
                ShowStatus(result.Message, isError: true);
                return;
            }

            ModelsList.ItemsSource = result.ModelIds;
            ShowStatus(result.Message, isError: false);
        }
        finally
        {
            LoadButton.IsEnabled = true;
        }
    }

    private void OnAddClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not ProviderSettingsViewModel provider
            || sender is not Button { DataContext: string modelId } button
            || IsConfigured(provider, modelId))
        {
            return;
        }

        ModelSettingsViewModel? model = provider.Models.FirstOrDefault(model =>
            string.IsNullOrWhiteSpace(model.Provider)
            && string.IsNullOrWhiteSpace(model.ModelName)
            && string.IsNullOrWhiteSpace(model.ModelId));
        if (model is null)
        {
            model = new ModelSettingsViewModel();
            provider.Models.Add(model);
        }

        model.Provider = "openai";
        model.ModelName = modelId;
        model.ModelId = modelId;
        if (button.Parent?.Parent is Border modelItem)
        {
            modelItem.IsVisible = false;
        }

        ShowStatus($"已添加模型 {modelId}。", isError: false);
    }

    private void OnModelItemLoaded(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ProviderSettingsViewModel provider
            && sender is Border { DataContext: string modelId } modelItem)
        {
            modelItem.IsVisible = !IsConfigured(provider, modelId);
        }
    }

    private static bool IsConfigured(ProviderSettingsViewModel provider, string modelId)
        => provider.Models.Any(model => model.ModelId == modelId || model.ModelName == modelId);

    private void ShowStatus(string message, bool isError)
    {
        StatusText.Text = message;
        StatusText.Foreground = isError ? Brushes.Crimson : Brushes.Gray;
        StatusText.IsVisible = true;
    }
}
