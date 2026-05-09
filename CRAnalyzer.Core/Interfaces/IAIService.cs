using CRAnalyzer.Core.Domain.DTOs;

namespace CRAnalyzer.Core.Interfaces;

public interface IAIService
{
    Task<AnalysisResultDto> AnalyzeAsync(string crContent, string projectSnapshot, CancellationToken cancellationToken = default);
}
