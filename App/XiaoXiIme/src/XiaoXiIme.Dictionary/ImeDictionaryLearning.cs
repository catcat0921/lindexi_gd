using XiaoXiIme.Foundation;

namespace XiaoXiIme.Dictionary;

/// <summary>
/// Describes a selected candidate together with the input that produced it.
/// </summary>
public sealed record ImeDictionaryLearning(ImeCandidate Candidate, string OriginalInput);
