using Microsoft.AspNetCore.Http;

namespace CRAnalyzer.Core.Interfaces;

public interface IDocumentParserService
{
    Task<string> ParseAsync(IFormFile file);
    bool IsSupported(string extension);
}
