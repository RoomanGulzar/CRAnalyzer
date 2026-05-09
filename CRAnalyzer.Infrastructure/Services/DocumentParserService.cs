using CRAnalyzer.Core.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using UglyToad.PdfPig;
using DocumentFormat.OpenXml.Packaging;

namespace CRAnalyzer.Infrastructure.Services;

public class DocumentParserService : IDocumentParserService
{
    private readonly ILogger<DocumentParserService> _logger;
    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".txt", ".md", ".markdown", ".pdf", ".docx"
    };

    public DocumentParserService(ILogger<DocumentParserService> logger)
    {
        _logger = logger;
    }

    public bool IsSupported(string extension) => SupportedExtensions.Contains(extension);

    public async Task<string> ParseAsync(IFormFile file)
    {
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        _logger.LogInformation("Parsing document {FileName} with extension {Extension}", file.FileName, ext);

        return ext switch
        {
            ".txt" or ".md" or ".markdown" => await ParseTextAsync(file),
            ".pdf" => await ParsePdfAsync(file),
            ".docx" => await ParseDocxAsync(file),
            _ => throw new NotSupportedException($"File extension '{ext}' is not supported.")
        };
    }

    private static async Task<string> ParseTextAsync(IFormFile file)
    {
        using var reader = new StreamReader(file.OpenReadStream());
        return await reader.ReadToEndAsync();
    }

    private async Task<string> ParsePdfAsync(IFormFile file)
    {
        using var ms = new MemoryStream();
        await file.CopyToAsync(ms);
        ms.Position = 0;

        using var pdf = PdfDocument.Open(ms);
        var sb = new System.Text.StringBuilder();

        foreach (var page in pdf.GetPages())
        {
            sb.AppendLine(page.Text);
        }

        return sb.ToString();
    }

    private async Task<string> ParseDocxAsync(IFormFile file)
    {
        using var ms = new MemoryStream();
        await file.CopyToAsync(ms);
        ms.Position = 0;

        using var wordDoc = WordprocessingDocument.Open(ms, false);
        var body = wordDoc.MainDocumentPart?.Document?.Body;

        if (body == null) return string.Empty;

        var sb = new System.Text.StringBuilder();
        foreach (var para in body.Elements<DocumentFormat.OpenXml.Wordprocessing.Paragraph>())
        {
            sb.AppendLine(para.InnerText);
        }

        return sb.ToString();
    }
}
