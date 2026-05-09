using CRAnalyzer.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.IO.Compression;
using System.Text;

namespace CRAnalyzer.Infrastructure.Services;

public class RepositoryScannerService : IRepositoryScannerService
{
    private readonly ILogger<RepositoryScannerService> _logger;
    private readonly IConfiguration _configuration;

    private static readonly HashSet<string> ExcludedDirNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ".git", ".vs", ".idea", "node_modules", "bin", "obj", "__pycache__",
        ".next", "dist", "build", "coverage", ".nyc_output"
    };

    private static readonly HashSet<string> TextExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".cs", ".js", ".ts", ".jsx", ".tsx", ".py", ".java", ".go", ".rs", ".rb",
        ".php", ".swift", ".kt", ".cpp", ".c", ".h", ".hpp", ".fs", ".fsx",
        ".html", ".cshtml", ".razor", ".vue", ".svelte", ".xml", ".json",
        ".yaml", ".yml", ".toml", ".ini", ".cfg", ".env", ".sh", ".bat", ".ps1",
        ".md", ".txt", ".sql", ".csproj", ".sln", ".props", ".targets", ".editorconfig"
    };

    public RepositoryScannerService(ILogger<RepositoryScannerService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<string> ScanFolderAsync(string folderPath)
    {
        if (!Directory.Exists(folderPath))
            throw new DirectoryNotFoundException($"Directory not found: {folderPath}");

        _logger.LogInformation("Scanning folder: {FolderPath}", folderPath);
        var sb = new StringBuilder();
        var maxFileSizeKb = int.TryParse(_configuration["RepositoryScanner:MaxFileSizeKb"], out var maxKb) ? maxKb : 500;

        sb.AppendLine($"# Project Structure: {Path.GetFileName(folderPath)}");
        sb.AppendLine();

        await ScanDirectoryAsync(new DirectoryInfo(folderPath), sb, folderPath, maxFileSizeKb, 0);

        return sb.ToString();
    }

    public async Task<string> ScanZipAsync(Stream zipStream)
    {
        _logger.LogInformation("Scanning ZIP archive");
        var sb = new StringBuilder();
        var maxFileSizeKb = int.TryParse(_configuration["RepositoryScanner:MaxFileSizeKb"], out var maxKb) ? maxKb : 500;

        sb.AppendLine("# Project Structure (from ZIP)");
        sb.AppendLine();

        using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read);

        foreach (var entry in archive.Entries.OrderBy(e => e.FullName))
        {
            if (entry.Name == string.Empty) continue; // directory entry

            // Skip excluded directories
            var parts = entry.FullName.Split('/');
            if (parts.Any(p => ExcludedDirNames.Contains(p))) continue;

            var ext = Path.GetExtension(entry.Name);
            if (!TextExtensions.Contains(ext)) continue;
            if (entry.Length > maxFileSizeKb * 1024) continue;

            sb.AppendLine($"## File: {entry.FullName}");
            sb.AppendLine("```");

            try
            {
                using var stream = entry.Open();
                using var reader = new StreamReader(stream);
                var content = await reader.ReadToEndAsync();
                sb.AppendLine(content);
            }
            catch (Exception ex)
            {
                sb.AppendLine($"[Error reading file: {ex.Message}]");
            }

            sb.AppendLine("```");
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private async Task ScanDirectoryAsync(DirectoryInfo dir, StringBuilder sb, string rootPath, int maxFileSizeKb, int depth)
    {
        if (depth > 10) return;
        if (ExcludedDirNames.Contains(dir.Name)) return;

        var files = dir.GetFiles().Where(f => TextExtensions.Contains(f.Extension)).OrderBy(f => f.Name);

        foreach (var file in files)
        {
            if (file.Length > maxFileSizeKb * 1024)
            {
                sb.AppendLine($"## File: {Path.GetRelativePath(rootPath, file.FullName)}");
                sb.AppendLine($"[Skipped — file too large ({file.Length / 1024} KB)]");
                sb.AppendLine();
                continue;
            }

            sb.AppendLine($"## File: {Path.GetRelativePath(rootPath, file.FullName)}");
            sb.AppendLine("```");

            try
            {
                var content = await File.ReadAllTextAsync(file.FullName);
                sb.AppendLine(content);
            }
            catch (Exception ex)
            {
                sb.AppendLine($"[Error reading file: {ex.Message}]");
            }

            sb.AppendLine("```");
            sb.AppendLine();
        }

        foreach (var subDir in dir.GetDirectories().OrderBy(d => d.Name))
        {
            await ScanDirectoryAsync(subDir, sb, rootPath, maxFileSizeKb, depth + 1);
        }
    }
}
