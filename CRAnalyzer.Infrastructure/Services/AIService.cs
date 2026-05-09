using CRAnalyzer.Core.Domain.DTOs;
using CRAnalyzer.Core.Domain.Enums;
using CRAnalyzer.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenAI.Chat;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CRAnalyzer.Infrastructure.Services;

public class AIService : IAIService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<AIService> _logger;
    private readonly IPromptGeneratorService _promptGenerator;

    public AIService(IConfiguration configuration, ILogger<AIService> logger, IPromptGeneratorService promptGenerator)
    {
        _configuration = configuration;
        _logger = logger;
        _promptGenerator = promptGenerator;
    }

    public async Task<AnalysisResultDto> AnalyzeAsync(string crContent, string projectSnapshot, CancellationToken cancellationToken = default)
    {
        var apiKey = _configuration["OpenAI:ApiKey"] ?? throw new InvalidOperationException("OpenAI API key not configured.");
        var model = _configuration["OpenAI:Model"] ?? "gpt-4o";
        var maxTokens = int.TryParse(_configuration["OpenAI:MaxTokens"], out var mt) ? mt : 4096;

        var systemPrompt = BuildSystemPrompt();
        var userPrompt = BuildUserPrompt(crContent, projectSnapshot);

        _logger.LogInformation("Calling OpenAI model {Model} for analysis", model);

        var client = new ChatClient(model, apiKey);

        var messages = new List<ChatMessage>
        {
            new SystemChatMessage(systemPrompt),
            new UserChatMessage(userPrompt)
        };

        var options = new ChatCompletionOptions
        {
            MaxOutputTokenCount = maxTokens,
            ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat()
        };

        var response = await client.CompleteChatAsync(messages, options, cancellationToken);
        var rawContent = response.Value.Content[0].Text;

        _logger.LogDebug("Raw AI response: {Response}", rawContent);

        var result = ParseAIResponse(rawContent, crContent, projectSnapshot);
        result.RawResponse = rawContent;

        return result;
    }

    private static string BuildSystemPrompt() => """
        You are an expert software architect and code analysis AI. Your job is to analyze a Change Request (CR) document against a given project structure and identify all files/components that need to be changed.

        You MUST respond with ONLY valid JSON in the following exact structure:
        {
          "summary": "A concise summary of the change request and its overall impact",
          "affectedFiles": [
            {
              "filePath": "relative/path/to/file.ext",
              "changeType": "Create|Modify|Delete|Refactor",
              "reason": "Why this file is affected",
              "suggestedSteps": ["Step 1", "Step 2", "Step 3"]
            }
          ],
          "generatedPrompt": "A detailed, optimized implementation prompt that can be given to another AI coding agent to implement all the required changes"
        }

        Rules:
        - changeType must be exactly one of: Create, Modify, Delete, Refactor
        - suggestedSteps must be a non-empty array of strings
        - generatedPrompt must be comprehensive and actionable
        - Do NOT include markdown, code fences, or any text outside the JSON
        """;

    private static string BuildUserPrompt(string crContent, string projectSnapshot) => $"""
        ## CHANGE REQUEST DOCUMENT
        {crContent}

        ## PROJECT STRUCTURE AND FILES
        {projectSnapshot}

        Analyze the change request against the project and return the JSON analysis.
        """;

    private AnalysisResultDto ParseAIResponse(string rawContent, string crContent, string projectSnapshot)
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new JsonStringEnumConverter() }
            };

            var parsed = JsonSerializer.Deserialize<AIResponseJson>(rawContent, options);
            if (parsed == null) throw new JsonException("Null response from AI");

            return new AnalysisResultDto
            {
                Summary = parsed.Summary ?? string.Empty,
                AffectedFiles = parsed.AffectedFiles?.Select(f => new AffectedFileDto
                {
                    FilePath = f.FilePath ?? string.Empty,
                    ChangeType = Enum.TryParse<ChangeType>(f.ChangeType, true, out var ct) ? ct : ChangeType.Modify,
                    Reason = f.Reason ?? string.Empty,
                    SuggestedSteps = f.SuggestedSteps ?? new List<string>()
                }).ToList() ?? new List<AffectedFileDto>(),
                GeneratedPrompt = parsed.GeneratedPrompt ?? _promptGenerator.GenerateCodingPrompt(new AnalysisResultDto(), crContent, projectSnapshot)
            };
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse AI JSON response");
            return new AnalysisResultDto
            {
                Summary = "AI response parsing failed. Raw response captured.",
                AffectedFiles = new List<AffectedFileDto>(),
                GeneratedPrompt = rawContent
            };
        }
    }

    private sealed class AIResponseJson
    {
        [JsonPropertyName("summary")]
        public string? Summary { get; set; }

        [JsonPropertyName("affectedFiles")]
        public List<AffectedFileJsonItem>? AffectedFiles { get; set; }

        [JsonPropertyName("generatedPrompt")]
        public string? GeneratedPrompt { get; set; }
    }

    private sealed class AffectedFileJsonItem
    {
        [JsonPropertyName("filePath")]
        public string? FilePath { get; set; }

        [JsonPropertyName("changeType")]
        public string? ChangeType { get; set; }

        [JsonPropertyName("reason")]
        public string? Reason { get; set; }

        [JsonPropertyName("suggestedSteps")]
        public List<string>? SuggestedSteps { get; set; }
    }
}
