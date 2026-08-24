using XiaoXiIme.Dictionary;
using XiaoXiIme.Foundation;

namespace XiaoXiIme.ImeCore;

public sealed class ImeContext
{
    private readonly IImeDictionary _dictionary;
    private readonly IShapeDictionary? _shapeDictionary;
    private readonly ISymbolDictionary? _symbolDictionary;
    private readonly Action<ImeDictionaryLearning>? _learn;
    private readonly List<ImeCandidate> _candidates = [];
    private string _reading = string.Empty;
    private int _caretIndex;
    private int _selection;
    private int _pageStart;
    private int _pageSize;
    private const int CandidatePageSize = 9;
    private const int MaxCandidateCount = 100;
    private const string AutoCommitReading = "xx";

    public ImeContext(IImeDictionary dictionary, Action<ImeDictionaryLearning>? learn = null)
    {
        _dictionary = dictionary ?? throw new ArgumentNullException(nameof(dictionary));
        _shapeDictionary = dictionary as IShapeDictionary;
        _symbolDictionary = dictionary as ISymbolDictionary;
        _learn = learn;
    }

    public ImeSessionSnapshot Snapshot => CreateSnapshot();

    public ImeProcessResult ProcessKey(ImeKey key)
    {
        return key.Kind switch
        {
            ImeKeyKind.Character => ProcessCharacter(key.Character),
            ImeKeyKind.Backspace => ProcessBackspace(),
            ImeKeyKind.Space => CommitCandidate(_selection),
            ImeKeyKind.Enter => CommitReading(),
            ImeKeyKind.Escape => ClearComposition(handled: IsComposing),
            ImeKeyKind.PreviousCandidate => MoveSelection(-1),
            ImeKeyKind.NextCandidate => MoveSelection(1),
            ImeKeyKind.PreviousCandidatePage => MoveSelection(-CandidatePageSize),
            ImeKeyKind.NextCandidatePage => MoveSelection(CandidatePageSize),
            ImeKeyKind.FirstCandidate => MoveSelectionTo(0),
            ImeKeyKind.LastCandidate => MoveSelectionTo(_candidates.Count - 1),
            ImeKeyKind.MoveCompositionCaretLeft => MoveCompositionCaret(-1),
            ImeKeyKind.MoveCompositionCaretRight => MoveCompositionCaret(1),
            ImeKeyKind.CandidateSelection => IsSymbolComposition
                ? ProcessSymbolDigit(key.CandidateIndex)
                : CommitCandidateInCurrentPage(key.CandidateIndex),
            _ => new ImeProcessResult(Snapshot, null, false)
        };
    }

    private bool IsComposing => _reading.Length > 0;

    private bool IsSymbolComposition => _reading.Length > 0 && _reading[0] == '/';

    private ImeProcessResult ProcessCharacter(char character)
    {
        if (!CanInsertCharacter(character))
        {
            return new ImeProcessResult(Snapshot, null, false);
        }

        var normalizedCharacter = IsSymbolComposition || character == '/'
            ? char.ToLowerInvariant(character)
            : character;
        _reading = _reading.Insert(_caretIndex, normalizedCharacter.ToString());
        _caretIndex++;
        _selection = 0;
        RefreshCandidates();

        if (string.Equals(_reading, AutoCommitReading, StringComparison.Ordinal)
            && _candidates.Count > 0)
        {
            return CommitCandidate(0);
        }

        return new ImeProcessResult(Snapshot, null, true);
    }

    private ImeProcessResult ProcessSymbolDigit(int candidateIndex)
    {
        if ((uint)candidateIndex >= 10)
        {
            return new ImeProcessResult(Snapshot, null, true);
        }

        var digit = candidateIndex == 9 ? '0' : (char)('1' + candidateIndex);
        _reading = _reading.Insert(_caretIndex, digit.ToString());
        _caretIndex++;
        _selection = 0;
        RefreshCandidates();
        return new ImeProcessResult(Snapshot, null, true);
    }

    private ImeProcessResult ProcessBackspace()
    {
        if (!IsComposing)
        {
            return new ImeProcessResult(Snapshot, null, false);
        }

        if (_caretIndex == 0)
        {
            return new ImeProcessResult(Snapshot, null, true);
        }

        _reading = _reading.Remove(_caretIndex - 1, 1);
        _caretIndex--;
        RefreshCandidates();

        return new ImeProcessResult(Snapshot, null, true);
    }

    private ImeProcessResult MoveCompositionCaret(int delta)
    {
        if (!IsComposing)
        {
            return new ImeProcessResult(Snapshot, null, false);
        }

        _caretIndex = Math.Clamp(_caretIndex + delta, 0, _reading.Length);

        return new ImeProcessResult(Snapshot, null, true);
    }

    private ImeProcessResult MoveSelection(int delta)
    {
        if (!IsComposing || _candidates.Count == 0)
        {
            return new ImeProcessResult(Snapshot, null, false);
        }

        _selection = Math.Clamp(_selection + delta, 0, _candidates.Count - 1);
        NormalizeCandidateWindow();

        return new ImeProcessResult(Snapshot, null, true);
    }

    private ImeProcessResult MoveSelectionTo(int candidateIndex)
    {
        if (!IsComposing || _candidates.Count == 0)
        {
            return new ImeProcessResult(Snapshot, null, false);
        }

        _selection = Math.Clamp(candidateIndex, 0, _candidates.Count - 1);
        NormalizeCandidateWindow();

        return new ImeProcessResult(Snapshot, null, true);
    }

    private ImeProcessResult CommitCandidate(int candidateIndex)
    {
        if (!IsComposing)
        {
            return new ImeProcessResult(Snapshot, null, false);
        }

        if ((uint)candidateIndex >= (uint)_candidates.Count)
        {
            return new ImeProcessResult(Snapshot, null, true);
        }

        var selectedCandidate = _candidates[candidateIndex];
        var originalInput = _reading;
        var shouldLearn = IsPhoneticComposition(originalInput);
        Clear();
        if (shouldLearn)
        {
            _learn?.Invoke(new ImeDictionaryLearning(selectedCandidate, originalInput));
        }

        return new ImeProcessResult(Snapshot, selectedCandidate.Text, true);
    }

    private ImeProcessResult CommitCandidateInCurrentPage(int pageCandidateIndex)
    {
        if (!IsComposing)
        {
            return new ImeProcessResult(Snapshot, null, false);
        }

        if ((uint)pageCandidateIndex >= (uint)_pageSize)
        {
            return new ImeProcessResult(Snapshot, null, true);
        }

        return CommitCandidate(_pageStart + pageCandidateIndex);
    }

    private ImeProcessResult CommitReading()
    {
        if (!IsComposing)
        {
            return new ImeProcessResult(Snapshot, null, false);
        }

        var commitText = _reading;
        Clear();

        return new ImeProcessResult(Snapshot, commitText, true);
    }

    private ImeProcessResult ClearComposition(bool handled)
    {
        Clear();

        return new ImeProcessResult(Snapshot, null, handled);
    }

    private void RefreshCandidates()
    {
        _candidates.Clear();

        if (IsComposing)
        {
            if (IsSymbolComposition)
            {
                _candidates.AddRange(_symbolDictionary?.QuerySymbols(_reading, MaxCandidateCount) ?? []);
            }
            else
            {
                var shapeStart = FindShapeStart(_reading);
                if (shapeStart == 0)
                {
                    var shapeCode = _reading.ToLowerInvariant();
                    _candidates.AddRange((_shapeDictionary?.QueryShape(shapeCode, MaxCandidateCount) ?? [])
                        .Select(entry => new ImeCandidate(entry.Text, entry.ShapeCode)));
                }
                else if (shapeStart > 0)
                {
                    var phoneticCandidates = _dictionary.Query(new ImeDictionaryQuery(
                        _reading[..shapeStart],
                        MaxCandidateCount,
                        ImeDictionaryMatchMode.ExactAndPrefix));
                    _candidates.AddRange(_shapeDictionary?.FilterByShape(
                        phoneticCandidates,
                        _reading[shapeStart..],
                        MaxCandidateCount) ?? []);
                }
                else
                {
                    _candidates.AddRange(_dictionary.Query(new ImeDictionaryQuery(
                        _reading,
                        MaxCandidateCount,
                        ImeDictionaryMatchMode.ExactAndPrefix)));
                }
            }
        }

        NormalizeCandidateWindow();
    }

    private void Clear()
    {
        _reading = string.Empty;
        _caretIndex = 0;
        _candidates.Clear();
        _selection = 0;
        _pageStart = 0;
        _pageSize = 0;
    }

    private void NormalizeCandidateWindow()
    {
        _caretIndex = Math.Clamp(_caretIndex, 0, _reading.Length);

        if (_candidates.Count == 0)
        {
            _selection = 0;
            _pageStart = 0;
            _pageSize = 0;
            return;
        }

        _selection = Math.Clamp(_selection, 0, _candidates.Count - 1);
        _pageStart = (_selection / CandidatePageSize) * CandidatePageSize;
        _pageSize = Math.Min(CandidatePageSize, _candidates.Count - _pageStart);
    }

    private ImeSessionSnapshot CreateSnapshot()
    {
        if (!IsComposing)
        {
            return ImeSessionSnapshot.Empty;
        }

        return new ImeSessionSnapshot(
            new CompositionText(_reading, _reading, _caretIndex),
            _candidates.ToArray(),
            CreateCandidateWindowState(),
            true,
            CreateGuideline());
    }

    private ImeGuideline CreateGuideline()
    {
        if (!IsComposing)
        {
            return ImeGuideline.Empty;
        }

        if (_candidates.Count == 0)
        {
            return new ImeGuideline(ImeGuidelineLevel.NoCandidate, $"无候选：{_reading}");
        }

        return new ImeGuideline(ImeGuidelineLevel.Reading, _reading);
    }

    private ImeCandidateWindowState CreateCandidateWindowState()
    {
        if (_candidates.Count == 0)
        {
            return ImeCandidateWindowState.Empty;
        }

        return new ImeCandidateWindowState(
            Selection: _selection,
            PageStart: _pageStart,
            PageSize: _pageSize);
    }

    private bool CanInsertCharacter(char character)
    {
        if (character == '/')
        {
            return !IsComposing;
        }

        return IsAsciiLetter(character);
    }

    private static bool IsPhoneticComposition(string input)
    {
        return input.Length > 0 && input[0] != '/' && FindShapeStart(input) < 0;
    }

    private static int FindShapeStart(string input)
    {
        for (var index = 0; index < input.Length; index++)
        {
            if (input[index] is >= 'A' and <= 'Z')
            {
                return index;
            }
        }

        return -1;
    }

    private static bool IsAsciiLetter(char character)
    {
        return character is >= 'a' and <= 'z' or >= 'A' and <= 'Z';
    }
}

