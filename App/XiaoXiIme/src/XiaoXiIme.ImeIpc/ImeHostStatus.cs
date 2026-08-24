namespace XiaoXiIme.ImeIpc;

public sealed record ImeHostStatus(
    bool IsRunning,
    string? LastError = null,
    string? DictionaryPackagePath = null,
    bool IsUsingFallbackDictionary = false,
    string? UserDictionaryPath = null,
    string? UserDictionaryError = null,
    string? IsolatedUserDictionaryPath = null)
{
    public static ImeHostStatus Stopped { get; } = new(false);

    public static ImeHostStatus Running { get; } = new(true);
}
