using CRAnalyzer.Core.Domain.DTOs;

namespace CRAnalyzer.Core.Interfaces;

public interface IPromptGeneratorService
{
    string GenerateCodingPrompt(AnalysisResultDto result, string crContent, string projectSnapshot);
}
