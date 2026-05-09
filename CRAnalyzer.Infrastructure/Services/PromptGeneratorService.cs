using CRAnalyzer.Core.Domain.DTOs;
using CRAnalyzer.Core.Interfaces;
using System.Text;

namespace CRAnalyzer.Infrastructure.Services;

public class PromptGeneratorService : IPromptGeneratorService
{
    public string GenerateCodingPrompt(AnalysisResultDto result, string crContent, string projectSnapshot)
    {
        var sb = new StringBuilder();

        sb.AppendLine("# AI Implementation Prompt");
        sb.AppendLine();
        sb.AppendLine("## Context");
        sb.AppendLine("You are an expert software engineer. Implement the following change request in the codebase described below.");
        sb.AppendLine();

        sb.AppendLine("## Change Request Summary");
        sb.AppendLine(result.Summary);
        sb.AppendLine();

        sb.AppendLine("## Original Change Request");
        sb.AppendLine("```");
        sb.AppendLine(crContent.Length > 3000 ? crContent[..3000] + "\n...[truncated]" : crContent);
        sb.AppendLine("```");
        sb.AppendLine();

        sb.AppendLine("## Files to Change");
        sb.AppendLine();

        foreach (var file in result.AffectedFiles)
        {
            sb.AppendLine($"### {file.ChangeType}: `{file.FilePath}`");
            sb.AppendLine($"**Reason:** {file.Reason}");
            sb.AppendLine();
            sb.AppendLine("**Steps:**");
            foreach (var step in file.SuggestedSteps)
            {
                sb.AppendLine($"- {step}");
            }
            sb.AppendLine();
        }

        sb.AppendLine("## Instructions for Implementation");
        sb.AppendLine("""
            1. Implement ALL changes listed above in their respective files.
            2. Follow the existing code style and patterns in the project.
            3. Ensure all changes are backward compatible unless explicitly stated.
            4. Write clean, well-commented code.
            5. Handle edge cases and errors appropriately.
            6. Update any relevant tests if test files exist.
            7. Do NOT modify files not listed above unless absolutely necessary.
            8. Return the complete modified file contents for each changed file.
            """);

        sb.AppendLine();
        sb.AppendLine("## Project Snapshot (Reference)");
        sb.AppendLine("Use the project structure and code below as context:");
        sb.AppendLine();

        var snapshotPreview = projectSnapshot.Length > 8000
            ? projectSnapshot[..8000] + "\n...[snapshot truncated for brevity]"
            : projectSnapshot;
        sb.AppendLine(snapshotPreview);

        return sb.ToString();
    }
}
