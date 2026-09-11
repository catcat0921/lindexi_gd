using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AgentLib;
using AgentLib.Coding;
using AgentLib.Coding.Images;
using AgentLib.Coding.Sandboxes;
using AgentLib.Core;
using AgentLib.Core.AgentApiManagers.LanguageModelProviders;
using AgentLib.Logging;
using CodingChatRoom.AvaloniaShell.Infrastructure;
using CodingChatRoom.AvaloniaShell.ViewModels;

namespace CodingChatRoom.AvaloniaShell.Services;

internal static class CodingChatStartup
{
    public static async Task<CodingChatRuntime> InitializeAsync(
        CodingChatRoomPaths paths,
        IMainThreadDispatcher mainThreadDispatcher)
    {
        ArgumentNullException.ThrowIfNull(paths);
        ArgumentNullException.ThrowIfNull(mainThreadDispatcher);

        paths.EnsureDirectories();
        paths.ConfigurationFile.Refresh();
        if (!paths.ConfigurationFile.Exists)
        {
            throw new FileNotFoundException(
                $"未找到 CodingChatRoom 模型配置文件：{paths.ConfigurationFile.FullName}",
                paths.ConfigurationFile.FullName);
        }

        AgentApiManagerConfiguration configuration = await AgentApiManagerConfiguration
            .FromJsonFileAsync(paths.ConfigurationFile)
            .ConfigureAwait(false);
        var settingsSandboxToolSource = new WindowsSandboxToolSource(false, string.Empty, string.Empty);
        var settingsService = new CodingChatSettingsService(paths, settingsSandboxToolSource);
        var runtime = new CodingChatRuntime(paths, configuration, mainThreadDispatcher, settingsService);
        await runtime.InitializeWorkTasksAsync().ConfigureAwait(false);
        return runtime;
    }

    internal static async Task<CodingWorkTaskRuntime> CreateWorkTaskRuntimeAsync(
        CodingChatRoomPaths paths,
        AgentApiManagerConfiguration configuration,
        IMainThreadDispatcher mainThreadDispatcher,
        CodingChatSettingsService settingsService,
        Guid workTaskId,
        string displayName,
        string? workspacePath,
        string? currentSessionId,
        CancellationToken cancellationToken)
    {
        var endpointManager = new AgentApiEndpointManager();
        endpointManager.LoadConfiguration(configuration);
        ILanguageModel primaryModel = endpointManager.PrimaryModel;
        var chatLogger = new FileCopilotChatLogger(paths.LogDirectory);
        var chatManager = new CopilotChatManager(chatLogger)
        {
            AgentApiEndpointManager = endpointManager,
            MainThreadDispatcher = mainThreadDispatcher,
        };
        var windowsSandboxToolSource = new WindowsSandboxToolSource(false, string.Empty, string.Empty);
        settingsService.RegisterWindowsSandboxToolSource(windowsSandboxToolSource);
        CodingChatShellSettings shellSettings = await settingsService
            .LoadShellSettingsAsync(cancellationToken)
            .ConfigureAwait(false);
        windowsSandboxToolSource.UpdateConfiguration(
            shellSettings.IsWindowsSandboxEnabled,
            shellSettings.WindowsSandboxToolPath,
            shellSettings.WindowsSandboxServerAddress);
        var codingAgent = new CodingAgent(new CodingAgentOptions
        {
            AdditionalToolSources = new List<ICodingWorkspaceToolSource>
            {
                new CodingImageAnalysisToolSource(chatManager),
                windowsSandboxToolSource,
            },
            CopilotInstructionsPath = GetCopilotInstructionsPath(shellSettings),
        });
        var workspaceController = new CodingWorkspaceController(mainThreadDispatcher);
        if (!string.IsNullOrWhiteSpace(workspacePath) && Directory.Exists(workspacePath))
        {
            await workspaceController.ChangeWorkspaceAsync(workspacePath, cancellationToken).ConfigureAwait(false);
        }

        var sessionStore = new FileCodingChatSessionStore(
            paths.SessionDirectory,
            paths.LogDirectory,
            chatManager,
            mainThreadDispatcher);
        var chatRunner = new CodingAgentChatRunner(chatManager, codingAgent);
        var application = new CodingChatApplication(
            chatManager,
            sessionStore,
            chatRunner,
            workspaceController,
            codingAgent);
        if (Guid.TryParse(currentSessionId, out Guid sessionId))
        {
            try
            {
                await application.OpenSessionAsync(sessionId, cancellationToken).ConfigureAwait(false);
            }
            catch (FileNotFoundException)
            {
            }
        }

        string modelDisplayName = GetModelDisplayName(primaryModel);
        var chatViewModel = new ChatViewModel(
            chatManager,
            application,
            workspaceController,
            $"当前模型：{modelDisplayName}");
        return new CodingWorkTaskRuntime(
            workTaskId,
            displayName,
            endpointManager,
            chatLogger,
            chatManager,
            codingAgent,
            application,
            workspaceController,
            chatViewModel);
    }

    private static string GetModelDisplayName(ILanguageModel primaryModel)
    {
        string provider = primaryModel.ModelDefinition.Provider;
        string modelName = primaryModel.ModelDefinition.ModelName;
        return string.IsNullOrWhiteSpace(provider) ? modelName : $"{provider}/{modelName}";
    }

    private static string? GetCopilotInstructionsPath(CodingChatShellSettings shellSettings)
    {
        if (!shellSettings.IsCopilotInstructionsEnabled)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(shellSettings.CopilotInstructionsPath))
        {
            string userCopilotInstructionsPath = Path.Join(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "copilot-instructions.md");
            return File.Exists(userCopilotInstructionsPath) ? userCopilotInstructionsPath : null;
        }

        return Path.GetFullPath(shellSettings.CopilotInstructionsPath);
    }
}
