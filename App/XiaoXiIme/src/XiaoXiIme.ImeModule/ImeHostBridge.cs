using XiaoXiIme.Dictionary;
using XiaoXiIme.Foundation;
using XiaoXiIme.ImeCore;
using XiaoXiIme.ImeIpc;

namespace XiaoXiIme.ImeModule;

public sealed class ImeHostBridge : IDisposable
{
    private readonly IImeHostBridgeClient _client;
    private readonly ImeContext _fallbackContext;
    private string? _lastError;

    public ImeHostBridge(XiaoXiImeIpcOptions? options = null)
        : this(new IpcImeHostBridgeClient(options), CreateFallbackContext())
    {
    }

    internal ImeHostBridge(IImeHostBridgeClient client, ImeContext? fallbackContext = null)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _fallbackContext = fallbackContext ?? CreateFallbackContext();
    }

    public string? LastError => _lastError;

    public bool IsUsingFallback => _lastError is not null;

    public ImeProcessResult ProcessKey(ImeKey key)
    {
        try
        {
            var result = _client.ProcessKeyAsync(key).ConfigureAwait(false).GetAwaiter().GetResult();
            _lastError = null;
            return result;
        }
        catch (ImeHostUnavailableException exception)
        {
            _lastError = exception.Message;
            return _fallbackContext.ProcessKey(key);
        }
    }

    public ImeSessionSnapshot GetSnapshot()
    {
        try
        {
            var snapshot = _client.GetSnapshotAsync().ConfigureAwait(false).GetAwaiter().GetResult();
            _lastError = null;
            return snapshot;
        }
        catch (ImeHostUnavailableException exception)
        {
            _lastError = exception.Message;
            return _fallbackContext.Snapshot;
        }
    }

    public ImeUiState GetUiState()
    {
        try
        {
            var uiState = _client.GetUiStateAsync().ConfigureAwait(false).GetAwaiter().GetResult();
            _lastError = null;
            return uiState;
        }
        catch (ImeHostUnavailableException exception)
        {
            _lastError = exception.Message;
            return ImeUiState.FromSnapshot(_fallbackContext.Snapshot);
        }
    }

    public void Dispose()
    {
        _client.Dispose();
    }

    internal interface IImeHostBridgeClient : IDisposable
    {
        Task<ImeProcessResult> ProcessKeyAsync(ImeKey key);

        Task<ImeSessionSnapshot> GetSnapshotAsync();

        Task<ImeUiState> GetUiStateAsync();
    }

    internal sealed class ImeHostUnavailableException : Exception
    {
        public ImeHostUnavailableException(string message, Exception? innerException = null)
            : base(message, innerException)
        {
        }
    }

    private static ImeContext CreateFallbackContext()
    {
        return new ImeContext(InMemoryImeDictionary.CreateMinimalFallback());
    }

    private sealed class IpcImeHostBridgeClient : IImeHostBridgeClient
    {
        private readonly XiaoXiImeIpcClient _client;

        public IpcImeHostBridgeClient(XiaoXiImeIpcOptions? options)
        {
            _client = new XiaoXiImeIpcClient(options);
        }

        public Task<ImeProcessResult> ProcessKeyAsync(ImeKey key)
        {
            return InvokeAsync(() => _client.ProcessKeyAsync(key));
        }

        public Task<ImeSessionSnapshot> GetSnapshotAsync()
        {
            return InvokeAsync(_client.GetSnapshotAsync);
        }

        public Task<ImeUiState> GetUiStateAsync()
        {
            return InvokeAsync(_client.GetUiStateAsync);
        }

        public void Dispose()
        {
            _client.Dispose();
        }

        private static async Task<T> InvokeAsync<T>(Func<Task<T>> operation)
        {
            try
            {
                return await operation().ConfigureAwait(false);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                throw new ImeHostUnavailableException(exception.Message, exception);
            }
        }
    }
}
