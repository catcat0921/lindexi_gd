using XiaoXiIme.Foundation;

namespace XiaoXiIme.Dictionary.Tests;

public class UserDictionaryTests
{
    [Fact]
    public void Learn_WhenCandidateSelectedThenPromotesItAboveSystemCandidates()
    {
        var dictionary = CreateDictionary();

        dictionary.Learn(new ImeDictionaryLearning(new ImeCandidate("呢", "ni", 60), " NI "));
        var result = dictionary.Query("ni");

        Assert.Equal("呢", result[0].Text);
    }

    [Fact]
    public void Learn_WhenSameNormalizedReadingSelectedThenIncrementsOneEntry()
    {
        var dictionary = CreateDictionary();

        dictionary.Learn(new ImeDictionaryLearning(new ImeCandidate("呢", "ni"), "NI"));
        dictionary.Learn(new ImeDictionaryLearning(new ImeCandidate("呢", "ni"), " ni "));
        var entries = dictionary.GetEntries();

        Assert.Equal(2, entries[0].SelectionCount);
    }

    [Fact]
    public void Learn_WhenSameTextHasDifferentReadingThenKeepsSeparateEntries()
    {
        var dictionary = CreateDictionary();

        dictionary.Learn(new ImeDictionaryLearning(new ImeCandidate("行", "xing"), "xing"));
        dictionary.Learn(new ImeDictionaryLearning(new ImeCandidate("行", "hang"), "hang"));
        var entries = dictionary.GetEntries();

        Assert.Equal(2, entries.Count);
    }

    [Fact]
    public void Learn_WhenPhraseCandidateSelectedThenContinuousInputPromotesIt()
    {
        var dictionary = new UserDictionary(new InMemoryImeDictionary(
        [
            new ImeCandidate("你号", "ni hao", 200),
            new ImeCandidate("你好", "ni hao", 100),
        ]));

        dictionary.Learn(new ImeDictionaryLearning(new ImeCandidate("你好", "ni hao", 100), "nihao"));
        var result = dictionary.Query("nihao");

        Assert.Equal("你好", result[0].Text);
    }

    [Fact]
    public void Query_WhenUserAndSystemContainSameCandidateThenDoesNotDuplicateIt()
    {
        var dictionary = CreateDictionary();
        dictionary.Learn(new ImeDictionaryLearning(new ImeCandidate("你", "ni"), "ni"));

        var result = dictionary.Query("ni");

        Assert.Single(result, candidate => candidate.Text == "你");
    }

    [Fact]
    public void Query_WhenDoublePinyinUserEntryMatchesSystemCandidateThenDoesNotDuplicateIt()
    {
        var packagePath = Path.Combine(Path.GetTempPath(), "XiaoXiIme.Dictionary.Tests", Guid.NewGuid().ToString("N"));
        DictionaryPackageCompiler.Compile(
            [new PhoneticDictionaryEntry("你好", "ni hao", 100)],
            packagePath,
            new DictionaryPackageParameters { InputScheme = "xiaoheDoublePinyin" });
        var dictionary = new UserDictionary(DictionaryPackageLoader.Load(packagePath));
        dictionary.Learn(new ImeDictionaryLearning(new ImeCandidate("你好", "ni hao"), "nihc"));

        var result = dictionary.Query("nihc");

        Assert.Single(result, candidate => candidate.Text == "你好");
    }

    [Fact]
    public void QueryShape_WhenSystemDictionarySupportsShapeThenDelegates()
    {
        var dictionary = new UserDictionary(new MultiResourceDictionary());

        var result = dictionary.QueryShape("a");

        Assert.Collection(result, entry => Assert.Equal("你", entry.Text));
    }

    [Fact]
    public void QuerySymbols_WhenSystemDictionarySupportsSymbolsThenDelegates()
    {
        var dictionary = new UserDictionary(new MultiResourceDictionary());

        var result = dictionary.QuerySymbols("/xh");

        Assert.Collection(result, candidate => Assert.Equal("★", candidate.Text));
    }

    [Fact]
    public void Save_WhenLoadedThenPreservesEntries()
    {
        var path = CreatePath();
        var entries = new[] { new UserDictionaryEntry("呢", "ni", 3) };

        UserDictionaryStore.Save(path, entries);
        var result = UserDictionaryStore.Load(path);

        Assert.Equal(entries, result.Entries);
    }

    [Fact]
    public void Load_WhenFileIsCorruptThenIsolatesItAndReturnsEmptyEntries()
    {
        var path = CreatePath();
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, "not-json");

        var result = UserDictionaryStore.Load(path);

        Assert.Empty(result.Entries);
        Assert.NotNull(result.Error);
        Assert.NotNull(result.IsolatedCorruptFilePath);
        Assert.False(File.Exists(path));
        Assert.True(File.Exists(result.IsolatedCorruptFilePath));
    }

    private sealed class MultiResourceDictionary : IImeDictionary, IShapeDictionary, ISymbolDictionary
    {
        public IReadOnlyList<ImeCandidate> Query(ImeDictionaryQuery query) => [];

        public IReadOnlyList<ShapeDictionaryEntry> QueryShape(string shapeCode, int maxCount = 9) =>
            [new ShapeDictionaryEntry("你", "ab", "亻尔")];

        public IReadOnlyList<ImeCandidate> FilterByShape(
            IReadOnlyList<ImeCandidate> candidates,
            string shapeCode,
            int maxCount = 9) => candidates.Take(maxCount).ToArray();

        public IReadOnlyList<ImeCandidate> QuerySymbols(string input, int maxCount = 9) =>
            [new ImeCandidate("★", input)];
    }

    private static UserDictionary CreateDictionary()
    {
        return new UserDictionary(new InMemoryImeDictionary(
        [
            new ImeCandidate("你", "ni", 100),
            new ImeCandidate("呢", "ni", 60),
        ]));
    }

    private static string CreatePath()
    {
        return Path.Combine(Path.GetTempPath(), $"XiaoXiIme_UserDictionary_{Guid.NewGuid():N}", "user-dictionary.json");
    }
}
