using XiaoXiIme.Dictionary;
using XiaoXiIme.Foundation;
using XiaoXiIme.ImeHost;
using XiaoXiIme.ImeIpc;

namespace XiaoXiIme.IntegrationTests;

public class ImeHostIpcIntegrationTests
{
    [Fact(Timeout = 5_000)]
    public async Task IpcClient_ProcessesKeysThroughImeHostService()
    {
        var options = new XiaoXiImeIpcOptions($"XiaoXiIme_Test_{Guid.NewGuid():N}");
        using var host = new ImeHostService(options);
        using var client = new XiaoXiImeIpcClient(options);

        host.Start();

        await client.ConnectAsync();
        await client.ProcessKeyAsync(ImeKey.FromCharacter('n'));
        var composingResult = await client.ProcessKeyAsync(ImeKey.FromCharacter('i'));

        Assert.True(composingResult.Handled);
        Assert.True(composingResult.Snapshot.IsComposing);
        Assert.Equal("ni", composingResult.Snapshot.Composition.Reading);
        Assert.Equal("你", composingResult.Snapshot.Candidates[0].Text);

        var commitResult = await client.ProcessKeyAsync(new ImeKey(ImeKeyKind.Space));

        Assert.True(commitResult.Handled);
        Assert.Equal("你", commitResult.CommitText);
        Assert.False(commitResult.Snapshot.IsComposing);

        var snapshot = await client.GetSnapshotAsync();

        Assert.False(snapshot.IsComposing);
        Assert.Empty(snapshot.Candidates);
    }

    [Fact]
    public async Task ImeHostService_WhenPackageIsValidThenLoadsProductionDictionary()
    {
        var packagePath = CreatePackage([new PhoneticDictionaryEntry("测试词", "ce", 500)]);
        using var host = new ImeHostService(dictionaryPackagePath: packagePath);

        var first = await host.ProcessKeyAsync(ImeKey.FromCharacter('c'));
        var result = await host.ProcessKeyAsync(ImeKey.FromCharacter('e'));
        var status = await host.GetHostStatusAsync();

        Assert.True(first.Handled);
        Assert.Equal("测试词", result.Snapshot.Candidates[0].Text);
        Assert.False(status.IsUsingFallbackDictionary);
        Assert.Null(status.LastError);
        Assert.Equal(Path.GetFullPath(packagePath), status.DictionaryPackagePath);
    }

    [Fact]
    public async Task ImeHostService_WhenPackageIsMissingThenReportsMinimalFallback()
    {
        var packagePath = Path.Combine(Path.GetTempPath(), $"XiaoXiIme_Missing_{Guid.NewGuid():N}");
        using var host = new ImeHostService(dictionaryPackagePath: packagePath);

        await host.ProcessKeyAsync(ImeKey.FromCharacter('n'));
        var result = await host.ProcessKeyAsync(ImeKey.FromCharacter('i'));
        var status = await host.GetHostStatusAsync();

        Assert.Equal("你", result.Snapshot.Candidates[0].Text);
        Assert.True(status.IsUsingFallbackDictionary);
        Assert.NotNull(status.LastError);
        Assert.Equal(Path.GetFullPath(packagePath), status.DictionaryPackagePath);
    }

    [Fact]
    public async Task ImeHostService_WhenCandidateSelectedThenPersistsLearningForNewHost()
    {
        var packagePath = CreatePackage(
        [
            new PhoneticDictionaryEntry("你", "ni", 100),
            new PhoneticDictionaryEntry("呢", "ni", 60),
        ]);
        var userDictionaryPath = Path.Combine(Path.GetTempPath(), $"XiaoXiIme_User_{Guid.NewGuid():N}", "user-dictionary.json");

        using (var host = new ImeHostService(dictionaryPackagePath: packagePath, userDictionaryPath: userDictionaryPath))
        {
            await host.ProcessKeyAsync(ImeKey.FromCharacter('n'));
            await host.ProcessKeyAsync(ImeKey.FromCharacter('i'));
            await host.ProcessKeyAsync(ImeKey.NextCandidate());
            await host.ProcessKeyAsync(new ImeKey(ImeKeyKind.Space));
        }

        using var reloadedHost = new ImeHostService(dictionaryPackagePath: packagePath, userDictionaryPath: userDictionaryPath);
        await reloadedHost.ProcessKeyAsync(ImeKey.FromCharacter('n'));
        var result = await reloadedHost.ProcessKeyAsync(ImeKey.FromCharacter('i'));

        Assert.Equal("呢", result.Snapshot.Candidates[0].Text);
    }

    [Fact]
    public async Task ImeHostService_WhenUserDictionaryIsCorruptThenReportsIsolationAndContinues()
    {
        var packagePath = CreatePackage([new PhoneticDictionaryEntry("你", "ni", 100)]);
        var userDictionaryPath = Path.Combine(Path.GetTempPath(), $"XiaoXiIme_User_{Guid.NewGuid():N}", "user-dictionary.json");
        Directory.CreateDirectory(Path.GetDirectoryName(userDictionaryPath)!);
        File.WriteAllText(userDictionaryPath, "invalid-json");

        using var host = new ImeHostService(dictionaryPackagePath: packagePath, userDictionaryPath: userDictionaryPath);
        await host.ProcessKeyAsync(ImeKey.FromCharacter('n'));
        var result = await host.ProcessKeyAsync(ImeKey.FromCharacter('i'));
        var status = await host.GetHostStatusAsync();

        Assert.Equal("你", result.Snapshot.Candidates[0].Text);
        Assert.NotNull(status.UserDictionaryError);
        Assert.NotNull(status.IsolatedUserDictionaryPath);
        Assert.Equal(Path.GetFullPath(userDictionaryPath), status.UserDictionaryPath);
    }

    [Fact(Timeout = 5_000)]
    public async Task IpcClient_GetsUiStateAndHostStatusThroughImeHostService()
    {
        var options = new XiaoXiImeIpcOptions($"XiaoXiIme_Test_{Guid.NewGuid():N}");
        using var host = new ImeHostService(options);
        using var client = new XiaoXiImeIpcClient(options);

        host.Start();

        var status = await client.GetHostStatusAsync();
        await client.ProcessKeyAsync(ImeKey.FromCharacter('n'));
        var result = await client.ProcessKeyAsync(ImeKey.FromCharacter('i'));
        var uiState = await client.GetUiStateAsync();

        Assert.True(status.IsRunning);
        Assert.True(result.Snapshot.IsComposing);
        Assert.True(uiState.CandidateWindowVisible);
        Assert.Equal("ni", uiState.Composition.Reading);
        Assert.Equal("你", uiState.Candidates[0].Text);
        Assert.Equal(uiState.Candidates.Count, uiState.CandidateWindow.PageSize);
        Assert.Equal(ImeGuidelineLevel.Reading, uiState.Guideline.Level);
    }

    private static string CreatePackage(IReadOnlyList<PhoneticDictionaryEntry> entries)
    {
        var packagePath = Path.Combine(Path.GetTempPath(), $"XiaoXiIme_HostPackage_{Guid.NewGuid():N}");
        DictionaryPackageCompiler.Compile(entries, packagePath);
        return packagePath;
    }
}

