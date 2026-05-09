using CRAnalyzer.Core.Domain.Enums;

namespace CRAnalyzer.Core.Domain.Models;

public class AffectedFile
{
    public int Id { get; set; }
    public int AnalysisId { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public ChangeType ChangeType { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string SuggestedSteps { get; set; } = string.Empty;

    public Analysis Analysis { get; set; } = null!;
}
