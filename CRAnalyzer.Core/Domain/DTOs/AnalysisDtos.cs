using CRAnalyzer.Core.Domain.Enums;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Http;
namespace CRAnalyzer.Core.Domain.DTOs;

public class AnalysisRequestDto
{
    public string Title { get; set; } = string.Empty;
    public IFormFile? CRDocument { get; set; }
    public string? ProjectFolderPath { get; set; }
    public IFormFile? ProjectZip { get; set; }
}

public class AffectedFileDto
{
    public string FilePath { get; set; } = string.Empty;
    public ChangeType ChangeType { get; set; }
    public string Reason { get; set; } = string.Empty;
    public List<string> SuggestedSteps { get; set; } = new();
}

public class AnalysisResultDto
{
    public string Summary { get; set; } = string.Empty;
    public List<AffectedFileDto> AffectedFiles { get; set; } = new();
    public string GeneratedPrompt { get; set; } = string.Empty;
    public string RawResponse { get; set; } = string.Empty;
}
