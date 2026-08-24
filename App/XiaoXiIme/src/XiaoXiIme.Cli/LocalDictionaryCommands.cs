using XiaoXiIme.Dictionary;

namespace XiaoXiIme.Cli;

internal static class LocalDictionaryCommands
{
    internal static int Update(DictionaryUpdateOptions options, TextWriter output, TextWriter error)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(error);

        if (string.IsNullOrWhiteSpace(options.SourceDirectory) || string.IsNullOrWhiteSpace(options.PackageDirectory))
        {
            error.WriteLine("dictionary-update requires <source-directory> <package-directory>.");
            return 2;
        }

        try
        {
            var sourceDirectory = Path.GetFullPath(options.SourceDirectory);
            if (!Directory.Exists(sourceDirectory))
            {
                error.WriteLine($"Dictionary source directory was not found: {sourceDirectory}");
                return 3;
            }

            var phoneticSources = FindSources(sourceDirectory, "*.phonetic.tsv");
            if (phoneticSources.Count == 0)
            {
                error.WriteLine("No *.phonetic.tsv source files were found.");
                return 3;
            }

            var manifest = LocalDictionaryPackageManager.Update(
                new LocalDictionaryPackageUpdate
                {
                    PhoneticSourcePaths = phoneticSources,
                    ShapeSourcePaths = FindSources(sourceDirectory, "*.shape.tsv"),
                    SymbolSourcePaths = FindSources(sourceDirectory, "*.symbols.tsv"),
                    Parameters = new DictionaryPackageParameters { InputScheme = options.Scheme },
                },
                options.PackageDirectory);

            output.WriteLine($"Dictionary package updated: {Path.GetFullPath(options.PackageDirectory)}");
            output.WriteLine($"Input scheme: {manifest.Parameters.InputScheme}");
            output.WriteLine($"Candidates: {manifest.Counts.Candidates}");
            return 0;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or InvalidOperationException or DictionarySourceException or DictionaryPackageException)
        {
            error.WriteLine(exception.Message);
            return 4;
        }
    }

    internal static int Rollback(DictionaryRollbackOptions options, TextWriter output, TextWriter error)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(error);

        if (string.IsNullOrWhiteSpace(options.PackageDirectory))
        {
            error.WriteLine("dictionary-rollback requires <package-directory>.");
            return 2;
        }

        try
        {
            LocalDictionaryPackageManager.Rollback(options.PackageDirectory);
            output.WriteLine($"Dictionary package rolled back: {Path.GetFullPath(options.PackageDirectory)}");
            return 0;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or InvalidOperationException or DictionaryPackageException)
        {
            error.WriteLine(exception.Message);
            return 4;
        }
    }

    internal static int ConvertSeWzc(DictionaryConvertSeWzcOptions options, TextWriter output, TextWriter error)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(error);

        if (string.IsNullOrWhiteSpace(options.SourceDirectory) || string.IsNullOrWhiteSpace(options.TargetDirectory))
        {
            error.WriteLine("dictionary-convert-sewzc requires <source-directory> <target-directory>.");
            return 2;
        }

        try
        {
            SeWzcDictionarySourceConverter.Convert(options.SourceDirectory, options.TargetDirectory);
            output.WriteLine($"SeWZC dictionary snapshot converted: {Path.GetFullPath(options.TargetDirectory)}");
            return 0;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or InvalidOperationException or DictionarySourceException)
        {
            error.WriteLine(exception.Message);
            return 4;
        }
    }

    private static IReadOnlyList<string> FindSources(string sourceDirectory, string pattern)
    {
        return Directory.EnumerateFiles(sourceDirectory, pattern, SearchOption.AllDirectories)
            .Order(StringComparer.Ordinal)
            .ToArray();
    }
}
