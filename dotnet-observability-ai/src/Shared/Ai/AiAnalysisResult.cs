namespace Shared.Ai;

public sealed record AiAnalysisResult(
    string Title,
    string RootCause,
    string Impact,
    string Fix,
    string Severity,
    string PrDiff);

public sealed record GitHubIssueDraft(
    string Title,
    string Description);
