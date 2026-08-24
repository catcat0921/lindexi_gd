using XiaoXiIme.Cli;
using XiaoXiIme.Dictionary;

namespace XiaoXiIme.Cli.Tests;

public class LocalDictionaryCommandsTests
{
    [Fact]
    public void Update_WhenSourcesAreValidThenCreatesPackage()
    {
        var root = CreateRoot();
        var sourceDirectory = Path.Combine(root, "sources");
        Directory.CreateDirectory(sourceDirectory);
        File.WriteAllText(Path.Combine(sourceDirectory, "custom.phonetic.tsv"), "你好\tni hao\t100");
        var packageDirectory = Path.Combine(root, "package");
        using var output = new StringWriter();
        using var error = new StringWriter();

        var exitCode = LocalDictionaryCommands.Update(
            new DictionaryUpdateOptions
            {
                SourceDirectory = sourceDirectory,
                PackageDirectory = packageDirectory,
                Scheme = "xiaoheDoublePinyin",
            },
            output,
            error);

        Assert.Equal(0, exitCode);
        Assert.Equal("你好", Assert.Single(DictionaryPackageLoader.Load(packageDirectory).Query(new ImeDictionaryQuery("nihc"))).Text);
    }

    [Fact]
    public void Rollback_WhenPreviousPackageExistsThenRestoresIt()
    {
        var root = CreateRoot();
        var sourceDirectory = Path.Combine(root, "sources");
        Directory.CreateDirectory(sourceDirectory);
        var sourcePath = Path.Combine(sourceDirectory, "custom.phonetic.tsv");
        File.WriteAllText(sourcePath, "你\tni\t100");
        var packageDirectory = Path.Combine(root, "package");
        using var output = new StringWriter();
        using var error = new StringWriter();
        LocalDictionaryCommands.Update(
            new DictionaryUpdateOptions { SourceDirectory = sourceDirectory, PackageDirectory = packageDirectory },
            output,
            error);
        File.WriteAllText(sourcePath, "好\thao\t200");
        LocalDictionaryCommands.Update(
            new DictionaryUpdateOptions { SourceDirectory = sourceDirectory, PackageDirectory = packageDirectory },
            output,
            error);

        var exitCode = LocalDictionaryCommands.Rollback(
            new DictionaryRollbackOptions { PackageDirectory = packageDirectory },
            output,
            error);

        Assert.Equal(0, exitCode);
        Assert.Equal("你", Assert.Single(DictionaryPackageLoader.Load(packageDirectory).Query(new ImeDictionaryQuery("ni"))).Text);
    }

    [Fact]
    public void Update_WhenPhoneticSourceIsMissingThenReturnsFailure()
    {
        var root = CreateRoot();
        using var output = new StringWriter();
        using var error = new StringWriter();

        var exitCode = LocalDictionaryCommands.Update(
            new DictionaryUpdateOptions { SourceDirectory = root, PackageDirectory = Path.Combine(root, "package") },
            output,
            error);

        Assert.Equal(3, exitCode);
    }

    [Fact]
    public void ConvertSeWzc_WhenSnapshotIsValidThenWritesNativeSources()
    {
        var root = CreateRoot();
        var sourceDirectory = CreateSeWzcSnapshot(root);
        var targetDirectory = Path.Combine(root, "native");
        using var output = new StringWriter();
        using var error = new StringWriter();

        var exitCode = LocalDictionaryCommands.ConvertSeWzc(
            new DictionaryConvertSeWzcOptions { SourceDirectory = sourceDirectory, TargetDirectory = targetDirectory },
            output,
            error);

        Assert.Equal(0, exitCode);
        Assert.Contains("你好\tni hao\t100", File.ReadAllText(Path.Combine(targetDirectory, SeWzcDictionarySourceConverter.PhoneticOutputRelativePath)));
    }

    [Fact]
    public void ConvertSeWzc_WhenSymbolInputHasTooManyCandidatesThenTruncatesDeterministically()
    {
        var root = CreateRoot();
        var sourceDirectory = CreateSeWzcSnapshot(root);
        var symbolsPath = Path.Combine(sourceDirectory, "symbols", "default.symbols.tsv");
        File.WriteAllText(symbolsPath, "/all\t" + string.Join('\t', Enumerable.Range(0, SymbolDictionarySourceParser.MaxCandidatesPerInput + 12).Select(index => $"候选{index}")));
        var targetDirectory = Path.Combine(root, "native");
        using var output = new StringWriter();
        using var error = new StringWriter();

        var exitCode = LocalDictionaryCommands.ConvertSeWzc(
            new DictionaryConvertSeWzcOptions { SourceDirectory = sourceDirectory, TargetDirectory = targetDirectory },
            output,
            error);

        Assert.Equal(0, exitCode);
        using var reader = File.OpenText(Path.Combine(targetDirectory, SeWzcDictionarySourceConverter.SymbolOutputRelativePath));
        Assert.Equal(SymbolDictionarySourceParser.MaxCandidatesPerInput, Assert.Single(SymbolDictionarySourceParser.Parse(reader, symbolsPath)).Candidates.Count);
    }

    [Fact]
    public void ConvertSeWzc_WhenRequiredSourceIsMissingThenPreservesExistingTargets()
    {
        var root = CreateRoot();
        var sourceDirectory = CreateSeWzcSnapshot(root);
        File.Delete(Path.Combine(sourceDirectory, "shape", "moqi_chaifen.txt"));
        var targetDirectory = Path.Combine(root, "native");
        var existingPath = Path.Combine(targetDirectory, SeWzcDictionarySourceConverter.PhoneticOutputRelativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(existingPath)!);
        File.WriteAllText(existingPath, "保留");
        using var output = new StringWriter();
        using var error = new StringWriter();

        var exitCode = LocalDictionaryCommands.ConvertSeWzc(
            new DictionaryConvertSeWzcOptions { SourceDirectory = sourceDirectory, TargetDirectory = targetDirectory },
            output,
            error);

        Assert.Equal(4, exitCode);
        Assert.Equal("保留", File.ReadAllText(existingPath));
    }

    private static string CreateSeWzcSnapshot(string root)
    {
        var sourceDirectory = Path.Combine(root, "snapshot");
        var phoneticDirectory = Path.Combine(sourceDirectory, "phonetic");
        var shapeDirectory = Path.Combine(sourceDirectory, "shape");
        var symbolDirectory = Path.Combine(sourceDirectory, "symbols");
        Directory.CreateDirectory(phoneticDirectory);
        Directory.CreateDirectory(shapeDirectory);
        Directory.CreateDirectory(symbolDirectory);
        File.WriteAllText(Path.Combine(phoneticDirectory, "rime-frost-8105.dict.yaml"), "---\n...\n你\tni\t10");
        File.WriteAllText(Path.Combine(phoneticDirectory, "rime-frost-base.dict.yaml"), "你好\tni hao\t100");
        File.WriteAllText(Path.Combine(phoneticDirectory, "rime-frost-corrections.dict.yaml"), "角色\tjiao se\t0\tjué sè");
        File.WriteAllText(Path.Combine(phoneticDirectory, "project-terms.dict.yaml"), "小希输入法\txiao xi shu ru fa\t1000");
        File.WriteAllText(Path.Combine(shapeDirectory, "moqi_chaifen.txt"), "你\trb\t亻尔");
        File.WriteAllText(Path.Combine(symbolDirectory, "default.symbols.tsv"), "/xh\t★\t☆");
        return sourceDirectory;
    }

    private static string CreateRoot()
    {
        var path = Path.Combine(Path.GetTempPath(), "XiaoXiIme.Cli.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
