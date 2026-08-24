using XiaoXiIme.Foundation;

namespace XiaoXiIme.ImeModule.Tests;

public class ImeHostBridgeTests
{
    [Fact(Timeout = 2_000)]
    public Task ProcessKey_WhenHostIsUnavailableThenUsesMinimalFallbackAndKeepsDiagnostic()
    {
        using var bridge = new ImeHostBridge(new UnavailableBridgeClient());

        var first = bridge.ProcessKey(ImeKey.FromCharacter('n'));
        var second = bridge.ProcessKey(ImeKey.FromCharacter('i'));
        var uiState = bridge.GetUiState();
        var commit = bridge.ProcessKey(new ImeKey(ImeKeyKind.Space));

        Assert.True(first.Handled);
        Assert.True(second.Handled);
        Assert.True(second.Snapshot.IsComposing);
        Assert.Equal("ni", second.Snapshot.Composition.Reading);
        Assert.NotNull(bridge.LastError);
        Assert.True(bridge.IsUsingFallback);
        Assert.True(uiState.CandidateWindowVisible);
        Assert.Equal("你", commit.CommitText);
        Assert.False(commit.Snapshot.IsComposing);

        return Task.CompletedTask;
    }

    [Fact]
    public void ProcessKey_WhenClientHasProgrammingErrorThenDoesNotHideFailure()
    {
        using var bridge = new ImeHostBridge(new InvalidBridgeClient());

        Assert.Throws<InvalidOperationException>(() => bridge.ProcessKey(ImeKey.FromCharacter('n')));
    }

    private sealed class UnavailableBridgeClient : ImeHostBridge.IImeHostBridgeClient
    {
        public Task<ImeProcessResult> ProcessKeyAsync(ImeKey key)
        {
            throw new ImeHostBridge.ImeHostUnavailableException("Host unavailable for test.");
        }

        public Task<ImeSessionSnapshot> GetSnapshotAsync()
        {
            throw new ImeHostBridge.ImeHostUnavailableException("Host unavailable for test.");
        }

        public Task<ImeUiState> GetUiStateAsync()
        {
            throw new ImeHostBridge.ImeHostUnavailableException("Host unavailable for test.");
        }

        public void Dispose()
        {
        }
    }

    private sealed class InvalidBridgeClient : ImeHostBridge.IImeHostBridgeClient
    {
        public Task<ImeProcessResult> ProcessKeyAsync(ImeKey key)
        {
            throw new InvalidOperationException("Programming error for test.");
        }

        public Task<ImeSessionSnapshot> GetSnapshotAsync()
        {
            throw new InvalidOperationException("Programming error for test.");
        }

        public Task<ImeUiState> GetUiStateAsync()
        {
            throw new InvalidOperationException("Programming error for test.");
        }

        public void Dispose()
        {
        }
    }
}
