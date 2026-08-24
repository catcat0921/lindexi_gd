using XiaoXiIme.Foundation;

namespace XiaoXiIme.Dictionary;

/// <summary>
/// Stores user selection frequencies and applies them as a ranking layer over a system dictionary.
/// </summary>
public sealed class UserDictionary : IImeDictionary, IShapeDictionary, ISymbolDictionary
{
    private readonly IImeDictionary _systemDictionary;
    private readonly Dictionary<UserDictionaryKey, int> _frequencies;
    private readonly object _syncRoot = new();

    public UserDictionary(IImeDictionary systemDictionary, IEnumerable<UserDictionaryEntry>? entries = null)
    {
        _systemDictionary = systemDictionary ?? throw new ArgumentNullException(nameof(systemDictionary));
        _frequencies = new Dictionary<UserDictionaryKey, int>();

        if (entries is null)
        {
            return;
        }

        foreach (var entry in entries)
        {
            if (string.IsNullOrWhiteSpace(entry.Text) || string.IsNullOrWhiteSpace(entry.Reading) || entry.SelectionCount <= 0)
            {
                continue;
            }

            var key = new UserDictionaryKey(entry.Text, DictionaryPackageFormat.NormalizeLookupKey(entry.Reading));
            _frequencies[key] = Math.Max(_frequencies.GetValueOrDefault(key), entry.SelectionCount);
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<ImeCandidate> Query(string reading, int maxCount = 9)
    {
        return Query(new ImeDictionaryQuery(reading, maxCount));
    }

    /// <inheritdoc />
    public IReadOnlyList<ImeCandidate> Query(ImeDictionaryQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (string.IsNullOrWhiteSpace(query.Input) || query.MaxCount <= 0)
        {
            return [];
        }

        var input = DictionaryPackageFormat.NormalizeLookupKey(query.Input);
        var systemCandidates = _systemDictionary.Query(query);
        lock (_syncRoot)
        {
            var userCandidates = _frequencies
                .Where(entry => string.Equals(entry.Key.Reading, input, StringComparison.Ordinal))
                .OrderByDescending(entry => entry.Value)
                .ThenBy(entry => entry.Key.Text, StringComparer.Ordinal)
                .Select(entry => new ImeCandidate(entry.Key.Text, entry.Key.Reading, entry.Value));

            return userCandidates
                .Concat(systemCandidates)
                .GroupBy(candidate => candidate.Text, StringComparer.Ordinal)
                .Select(group => group.First())
                .Take(query.MaxCount)
                .ToArray();
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<ShapeDictionaryEntry> QueryShape(string shapeCode, int maxCount = 9)
    {
        return _systemDictionary is IShapeDictionary shapeDictionary
            ? shapeDictionary.QueryShape(shapeCode, maxCount)
            : [];
    }

    /// <inheritdoc />
    public IReadOnlyList<ImeCandidate> FilterByShape(
        IReadOnlyList<ImeCandidate> candidates,
        string shapeCode,
        int maxCount = 9)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        return _systemDictionary is IShapeDictionary shapeDictionary
            ? shapeDictionary.FilterByShape(candidates, shapeCode, maxCount)
            : [];
    }

    /// <inheritdoc />
    public IReadOnlyList<ImeCandidate> QuerySymbols(string input, int maxCount = 9)
    {
        return _systemDictionary is ISymbolDictionary symbolDictionary
            ? symbolDictionary.QuerySymbols(input, maxCount)
            : [];
    }

    /// <summary>
    /// Records a user selection and returns its updated selection count.
    /// </summary>
    public int Learn(ImeDictionaryLearning learning)
    {
        ArgumentNullException.ThrowIfNull(learning);
        ArgumentNullException.ThrowIfNull(learning.Candidate);
        if (string.IsNullOrWhiteSpace(learning.Candidate.Text))
        {
            throw new ArgumentException(DictionaryResources.EmptyText, nameof(learning));
        }

        if (string.IsNullOrWhiteSpace(learning.OriginalInput))
        {
            throw new ArgumentException(DictionaryResources.EmptyReading, nameof(learning));
        }

        var key = new UserDictionaryKey(
            learning.Candidate.Text,
            DictionaryPackageFormat.NormalizeLookupKey(learning.OriginalInput));
        lock (_syncRoot)
        {
            var current = _frequencies.GetValueOrDefault(key);
            var updated = current == int.MaxValue ? int.MaxValue : current + 1;
            _frequencies[key] = updated;
            return updated;
        }
    }

    /// <summary>
    /// Returns a stable snapshot suitable for persistence.
    /// </summary>
    public IReadOnlyList<UserDictionaryEntry> GetEntries()
    {
        lock (_syncRoot)
        {
            return _frequencies
                .OrderBy(entry => entry.Key.Reading, StringComparer.Ordinal)
                .ThenBy(entry => entry.Key.Text, StringComparer.Ordinal)
                .Select(entry => new UserDictionaryEntry(entry.Key.Text, entry.Key.Reading, entry.Value))
                .ToArray();
        }
    }

    private readonly record struct UserDictionaryKey(string Text, string Reading);
}

/// <summary>
/// Represents one persisted user dictionary entry.
/// </summary>
public sealed record UserDictionaryEntry(string Text, string Reading, int SelectionCount);
