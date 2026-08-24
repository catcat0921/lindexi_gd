namespace XiaoXiIme.Dictionary.Tests;

public class LocalDictionaryPackageManagerTests
{
    [Fact]
    public void Update_WhenSourceIsValidThenInstallsQueryablePackage()
    {
        var root = CreateRoot();
        var sourcePath = WritePhoneticSource(root, "first.phonetic.tsv", "你\tni\t100");
        var packagePath = Path.Combine(root, "package");

        var manifest = LocalDictionaryPackageManager.Update(
            new LocalDictionaryPackageUpdate { PhoneticSourcePaths = [sourcePath] },
            packagePath);

        Assert.Equal("你", Assert.Single(DictionaryPackageLoader.Load(packagePath).Query(new ImeDictionaryQuery("ni"))).Text);
    }

    [Fact]
    public void Update_WhenSourcesAreUnderCommonRootThenManifestUsesStableRelativePaths()
    {
        var root = CreateRoot();
        var phoneticDirectory = Path.Combine(root, "phonetic");
        Directory.CreateDirectory(phoneticDirectory);
        var phoneticPath = WritePhoneticSource(phoneticDirectory, "first.phonetic.tsv", "你\tni\t100");
        var shapeDirectory = Path.Combine(root, "shape");
        Directory.CreateDirectory(shapeDirectory);
        var shapePath = Path.Combine(shapeDirectory, "first.shape.tsv");
        File.WriteAllText(shapePath, "你\trb\t亻尔");

        var manifest = LocalDictionaryPackageManager.Update(
            new LocalDictionaryPackageUpdate { PhoneticSourcePaths = [phoneticPath], ShapeSourcePaths = [shapePath] },
            Path.Combine(root, "package"));

        Assert.Equal(["phonetic/first.phonetic.tsv", "shape/first.shape.tsv"], manifest.Sources.Select(source => source.Path));
    }

    [Fact]
    public void Update_WhenPackageAlreadyExistsThenRetainsPreviousVersion()
    {
        var root = CreateRoot();
        var sourcePath = WritePhoneticSource(root, "dictionary.phonetic.tsv", "你\tni\t100");
        var packagePath = Path.Combine(root, "package");
        LocalDictionaryPackageManager.Update(
            new LocalDictionaryPackageUpdate { PhoneticSourcePaths = [sourcePath] },
            packagePath);
        File.WriteAllText(sourcePath, "好\thao\t200");

        LocalDictionaryPackageManager.Update(
            new LocalDictionaryPackageUpdate { PhoneticSourcePaths = [sourcePath] },
            packagePath);

        Assert.Equal("你", Assert.Single(DictionaryPackageLoader.Load($"{packagePath}.previous").Query(new ImeDictionaryQuery("ni"))).Text);
    }

    [Fact]
    public void Rollback_WhenPreviousVersionExistsThenExchangesVersions()
    {
        var root = CreateRoot();
        var sourcePath = WritePhoneticSource(root, "dictionary.phonetic.tsv", "你\tni\t100");
        var packagePath = Path.Combine(root, "package");
        LocalDictionaryPackageManager.Update(
            new LocalDictionaryPackageUpdate { PhoneticSourcePaths = [sourcePath] },
            packagePath);
        File.WriteAllText(sourcePath, "好\thao\t200");
        LocalDictionaryPackageManager.Update(
            new LocalDictionaryPackageUpdate { PhoneticSourcePaths = [sourcePath] },
            packagePath);

        LocalDictionaryPackageManager.Rollback(packagePath);

        Assert.Equal("你", Assert.Single(DictionaryPackageLoader.Load(packagePath).Query(new ImeDictionaryQuery("ni"))).Text);
    }

    [Fact]
    public void Update_WhenSourceIsInvalidThenKeepsActivePackage()
    {
        var root = CreateRoot();
        var sourcePath = WritePhoneticSource(root, "dictionary.phonetic.tsv", "你\tni\t100");
        var packagePath = Path.Combine(root, "package");
        LocalDictionaryPackageManager.Update(
            new LocalDictionaryPackageUpdate { PhoneticSourcePaths = [sourcePath] },
            packagePath);
        File.WriteAllText(sourcePath, "invalid");

        Assert.Throws<PhoneticDictionarySourceException>(() => LocalDictionaryPackageManager.Update(
            new LocalDictionaryPackageUpdate { PhoneticSourcePaths = [sourcePath] },
            packagePath));
        Assert.Equal("你", Assert.Single(DictionaryPackageLoader.Load(packagePath).Query(new ImeDictionaryQuery("ni"))).Text);
    }

    [Fact]
    public void Rollback_WhenPreviousVersionIsInvalidThenKeepsActivePackage()
    {
        var root = CreateRoot();
        var sourcePath = WritePhoneticSource(root, "dictionary.phonetic.tsv", "你\tni\t100");
        var packagePath = Path.Combine(root, "package");
        LocalDictionaryPackageManager.Update(
            new LocalDictionaryPackageUpdate { PhoneticSourcePaths = [sourcePath] },
            packagePath);
        File.WriteAllText(sourcePath, "好\thao\t200");
        LocalDictionaryPackageManager.Update(
            new LocalDictionaryPackageUpdate { PhoneticSourcePaths = [sourcePath] },
            packagePath);
        File.Delete(Path.Combine($"{packagePath}.previous", "candidates.bin"));

        Assert.Throws<DictionaryPackageException>(() => LocalDictionaryPackageManager.Rollback(packagePath));
        Assert.Equal("好", Assert.Single(DictionaryPackageLoader.Load(packagePath).Query(new ImeDictionaryQuery("hao"))).Text);
    }

    private static string CreateRoot()
    {
        var path = Path.Combine(Path.GetTempPath(), "XiaoXiIme.Dictionary.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static string WritePhoneticSource(string root, string fileName, string content)
    {
        var path = Path.Combine(root, fileName);
        File.WriteAllText(path, content);
        return path;
    }
}
