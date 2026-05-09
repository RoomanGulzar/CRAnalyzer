using CRAnalyzer.Core.Domain.Enums;

namespace CRAnalyzer.Core.Domain.Models;

public class Analysis
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string CRDocumentPath { get; set; } = string.Empty;
    public string CRDocumentName { get; set; } = string.Empty;
    public string ProjectPath { get; set; } = string.Empty;
    public AnalysisStatus Status { get; set; } = AnalysisStatus.Pending;
    public string? Summary { get; set; }
    public string? RawAIResponse { get; set; }
    public string? GeneratedPrompt { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }

    public ICollection<AffectedFile> AffectedFiles { get; set; } = new List<AffectedFile>();
}
