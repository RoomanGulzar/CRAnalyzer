using CRAnalyzer.Core.Domain.DTOs;
using CRAnalyzer.Core.Domain.Enums;
using CRAnalyzer.Infrastructure.Services;
using Xunit;

namespace CRAnalyzer.Tests.Services;

public class PromptGeneratorServiceTests
{
    private readonly PromptGeneratorService _service = new();

    [Fact]
    public void GenerateCodingPrompt_ReturnsNonEmptyString()
    {
        var result = new AnalysisResultDto
        {
            Summary = "Add JWT authentication",
            AffectedFiles = new List<AffectedFileDto>
            {
                new()
                {
                    FilePath = "Controllers/AuthController.cs",
                    ChangeType = ChangeType.Create,
                    Reason = "New endpoint needed",
                    SuggestedSteps = new List<string> { "Create controller", "Add JWT middleware" }
                }
            }
        };

        var prompt = _service.GenerateCodingPrompt(result, "CR: Add JWT", "Project: MyApp");

        Assert.NotEmpty(prompt);
        Assert.Contains("JWT", prompt);
        Assert.Contains("AuthController", prompt);
        Assert.Contains("Create controller", prompt);
    }

    [Fact]
    public void GenerateCodingPrompt_HandlesEmptyAffectedFiles()
    {
        var result = new AnalysisResultDto { Summary = "Minor change", AffectedFiles = new List<AffectedFileDto>() };
        var prompt = _service.GenerateCodingPrompt(result, "CR content", "Project snapshot");
        Assert.NotEmpty(prompt);
    }

    [Fact]
    public void GenerateCodingPrompt_TruncatesLongCRContent()
    {
        var longCr = new string('A', 5000);
        var result = new AnalysisResultDto { Summary = "Test", AffectedFiles = new List<AffectedFileDto>() };
        var prompt = _service.GenerateCodingPrompt(result, longCr, "snapshot");
        Assert.Contains("[truncated]", prompt);
    }
}
