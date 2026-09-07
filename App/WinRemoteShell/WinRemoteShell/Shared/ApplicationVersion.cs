using System.Reflection;

namespace WinRemoteShell.Shared;

internal static class ApplicationVersion
{
    internal static string Current { get; } =
        typeof(ApplicationVersion).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion
        ?? typeof(ApplicationVersion).Assembly.GetName().Version?.ToString()
        ?? "0.0.0";

    internal static int Compare(string left, string right) =>
        Parse(left).CompareTo(Parse(right));

    private static Version Parse(string value)
    {
        var metadataIndex = value.IndexOfAny(['-', '+']);
        var versionValue = metadataIndex < 0 ? value : value[..metadataIndex];
        if (!Version.TryParse(versionValue, out var version))
        {
            throw new InvalidDataException($"Application version '{value}' is invalid.");
        }

        return version;
    }
}
