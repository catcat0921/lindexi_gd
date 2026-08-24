using XiaoXiIme.Dictionary;
using XiaoXiIme.Foundation;
using XiaoXiIme.ImeCore;
using XiaoXiIme.ImeIpc;

namespace XiaoXiIme.ImeHost;

public sealed class ImeHostService : IDisposable
{
    private const string DefaultDictionaryPackageDirectoryName = "XiaoXiIme.DictionaryPackage";
    private const string DefaultUserDictionaryFileName = "user-dictionary.json";
    private readonly Func<ImeContext> _createImeContext;
    private readonly string? _dictionaryLoadError;
    private readonly string? _dictionaryPackagePath;
    private readonly Dictionary<ImeSessionId, ImeContext> _imeContexts = [];
    private readonly XiaoXiImeIpcServer _ipcServer;
    private readonly bool _isUsingFallbackDictionary;
    private readonly string? _isolatedUserDictionaryPath;
    private readonly string? _userDictionaryLoadError;
    private readonly string? _userDictionaryPath;
    private readonly UserDictionarySaveCoordinator? _userDictionarySaveCoordinator;
    private readonly object _syncRoot = new();
    private bool _started;
    private string? _lastError;
    private string? _userDictionarySaveError;

    public ImeHostService(
        XiaoXiImeIpcOptions? options = null,
        string? dictionaryPackagePath = null,
        string? userDictionaryPath = null)
        : this(LoadRuntimeDictionary(dictionaryPackagePath, userDictionaryPath), options)
    {
    }

    public ImeHostService(ImeContext imeContext, XiaoXiImeIpcOptions? options = null)
        : this(CreateSingleContextFactory(imeContext), options)
    {
    }

    internal ImeHostService(Func<ImeContext> createImeContext, XiaoXiImeIpcOptions? options = null)
        : this(createImeContext, options, dictionaryPackagePath: null, dictionaryLoadError: null, isUsingFallbackDictionary: false)
    {
    }

    private ImeHostService(RuntimeDictionary runtimeDictionary, XiaoXiImeIpcOptions? options)
    {
        var userDictionary = new UserDictionary(runtimeDictionary.Dictionary, runtimeDictionary.UserDictionaryLoad.Entries);
        _dictionaryPackagePath = runtimeDictionary.PackagePath;
        _dictionaryLoadError = runtimeDictionary.LoadError;
        _isUsingFallbackDictionary = runtimeDictionary.IsFallback;
        _userDictionaryPath = runtimeDictionary.UserDictionaryPath;
        _userDictionaryLoadError = runtimeDictionary.UserDictionaryLoad.Error;
        _isolatedUserDictionaryPath = runtimeDictionary.UserDictionaryLoad.IsolatedCorruptFilePath;
        _userDictionarySaveCoordinator = new UserDictionarySaveCoordinator(
            runtimeDictionary.UserDictionaryPath,
            error =>
            {
                lock (_syncRoot)
                {
                    _userDictionarySaveError = error;
                }
            });
        _createImeContext = () => new ImeContext(
            userDictionary,
            learning =>
            {
                userDictionary.Learn(learning);
                _userDictionarySaveCoordinator.RequestSave(userDictionary.GetEntries());
            });
        _ipcServer = new XiaoXiImeIpcServer(ProcessKeyAsync, GetSnapshotAsync, GetUiStateAsync, GetHostStatusAsync, options);
    }

    private ImeHostService(
        Func<ImeContext> createImeContext,
        XiaoXiImeIpcOptions? options,
        string? dictionaryPackagePath,
        string? dictionaryLoadError,
        bool isUsingFallbackDictionary)
    {
        _createImeContext = createImeContext ?? throw new ArgumentNullException(nameof(createImeContext));
        _dictionaryPackagePath = dictionaryPackagePath;
        _dictionaryLoadError = dictionaryLoadError;
        _isUsingFallbackDictionary = isUsingFallbackDictionary;
        _ipcServer = new XiaoXiImeIpcServer(ProcessKeyAsync, GetSnapshotAsync, GetUiStateAsync, GetHostStatusAsync, options);
    }

    public void Start()
    {
        try
        {
            _ipcServer.Start();
            lock (_syncRoot)
            {
                _started = true;
                _lastError = null;
            }
        }
        catch (Exception exception)
        {
            lock (_syncRoot)
            {
                _started = false;
                _lastError = exception.Message;
            }

            throw;
        }
    }

    public Task<ImeProcessKeyResponse> ProcessKeyAsync(ImeProcessKeyRequest request)
    {
        lock (_syncRoot)
        {
            var sessionId = request.EffectiveSessionId;
            if (!_imeContexts.TryGetValue(sessionId, out var imeContext))
            {
                imeContext = _createImeContext();
                _imeContexts.Add(sessionId, imeContext);
            }

            var result = imeContext.ProcessKey(request.Key);
            return Task.FromResult(new ImeProcessKeyResponse(
                result,
                sessionId,
                request.Generation,
                request.SequenceNumber));
        }
    }

    public async Task<ImeProcessResult> ProcessKeyAsync(ImeKey key)
    {
        var response = await ProcessKeyAsync(new ImeProcessKeyRequest(key)).ConfigureAwait(false);
        return response.Result;
    }

    public Task<ImeSessionSnapshot> GetSnapshotAsync()
    {
        lock (_syncRoot)
        {
            return Task.FromResult(GetOrCreateContext(ImeSessionId.Default).Snapshot);
        }
    }

    public Task<ImeUiState> GetUiStateAsync()
    {
        lock (_syncRoot)
        {
            return Task.FromResult(ImeUiState.FromSnapshot(GetOrCreateContext(ImeSessionId.Default).Snapshot));
        }
    }

    public Task<ImeHostStatus> GetHostStatusAsync()
    {
        lock (_syncRoot)
        {
            return Task.FromResult(new ImeHostStatus(
                _started,
                _lastError ?? _dictionaryLoadError,
                _dictionaryPackagePath,
                _isUsingFallbackDictionary,
                _userDictionaryPath,
                _userDictionarySaveError ?? _userDictionaryLoadError,
                _isolatedUserDictionaryPath));
        }
    }

    public void Dispose()
    {
        lock (_syncRoot)
        {
            _started = false;
        }

        _userDictionarySaveCoordinator?.Dispose();
        _ipcServer.Dispose();
    }

    private ImeContext GetOrCreateContext(ImeSessionId sessionId)
    {
        if (_imeContexts.TryGetValue(sessionId, out var imeContext))
        {
            return imeContext;
        }

        imeContext = _createImeContext();
        _imeContexts.Add(sessionId, imeContext);
        return imeContext;
    }

    private static RuntimeDictionary LoadRuntimeDictionary(string? dictionaryPackagePath, string? userDictionaryPath)
    {
        var packagePath = Path.GetFullPath(dictionaryPackagePath ?? Path.Combine(
            AppContext.BaseDirectory,
            DefaultDictionaryPackageDirectoryName));
        var resolvedUserDictionaryPath = Path.GetFullPath(userDictionaryPath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "XiaoXiIme",
            DefaultUserDictionaryFileName));
        var userDictionaryLoad = UserDictionaryStore.Load(resolvedUserDictionaryPath);

        try
        {
            return new RuntimeDictionary(
                DictionaryPackageLoader.Load(packagePath),
                packagePath,
                null,
                IsFallback: false,
                resolvedUserDictionaryPath,
                userDictionaryLoad);
        }
        catch (DictionaryPackageException exception)
        {
            return new RuntimeDictionary(
                InMemoryImeDictionary.CreateMinimalFallback(),
                packagePath,
                exception.Message,
                IsFallback: true,
                resolvedUserDictionaryPath,
                userDictionaryLoad);
        }
    }

    private static Func<ImeContext> CreateSingleContextFactory(ImeContext imeContext)
    {
        ArgumentNullException.ThrowIfNull(imeContext);
        var used = false;
        return () =>
        {
            if (used)
            {
                return new ImeContext(InMemoryImeDictionary.CreateMinimalFallback());
            }

            used = true;
            return imeContext;
        };
    }

    private sealed record RuntimeDictionary(
        IImeDictionary Dictionary,
        string PackagePath,
        string? LoadError,
        bool IsFallback,
        string UserDictionaryPath,
        UserDictionaryLoadResult UserDictionaryLoad);
}
