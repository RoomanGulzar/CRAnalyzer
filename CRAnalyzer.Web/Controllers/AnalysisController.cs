using CRAnalyzer.Core.Domain.DTOs;
using CRAnalyzer.Core.Domain.Enums;
using CRAnalyzer.Core.Domain.Models;
using CRAnalyzer.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CRAnalyzer.Web.Controllers;

public class AnalysisController : Controller
{
    private readonly IAnalysisRepository _repo;
    private readonly IAIService _aiService;
    private readonly IDocumentParserService _docParser;
    private readonly IRepositoryScannerService _repoScanner;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AnalysisController> _logger;
    private readonly IWebHostEnvironment _env;

    public AnalysisController(
        IAnalysisRepository repo,
        IAIService aiService,
        IDocumentParserService docParser,
        IRepositoryScannerService repoScanner,
        IConfiguration configuration,
        ILogger<AnalysisController> logger,
        IWebHostEnvironment env)
    {
        _repo = repo;
        _aiService = aiService;
        _docParser = docParser;
        _repoScanner = repoScanner;
        _configuration = configuration;
        _logger = logger;
        _env = env;
    }

    // GET /Analysis/New
    public IActionResult New()
    {
        return View();
    }

    // POST /Analysis/Submit
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(52_428_800)] // 50 MB
    public async Task<IActionResult> Submit(AnalysisRequestDto request)
    {
        if (request.CRDocument == null || request.CRDocument.Length == 0)
        {
            ModelState.AddModelError("CRDocument", "Please upload a Change Request document.");
            return View("New", request);
        }

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            ModelState.AddModelError("Title", "Please provide a title for this analysis.");
            return View("New", request);
        }

        // Validate CR document type
        var ext = Path.GetExtension(request.CRDocument.FileName);
        if (!_docParser.IsSupported(ext))
        {
            ModelState.AddModelError("CRDocument", $"Unsupported file type: {ext}. Supported: TXT, MD, PDF, DOCX");
            return View("New", request);
        }

        // Create initial analysis record
        var analysis = new Analysis
        {
            Title = request.Title,
            Status = AnalysisStatus.Running,
            CreatedAt = DateTime.UtcNow
        };

        try
        {
            // Save CR document
            var uploadPath = Path.Combine(_env.WebRootPath, _configuration["FileUpload:UploadPath"] ?? "uploads");
            var uniqueFileName = $"{Guid.NewGuid()}{ext}";
            var filePath = Path.Combine(uploadPath, uniqueFileName);

            await using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await request.CRDocument.CopyToAsync(stream);
            }

            analysis.CRDocumentPath = Path.Combine(_configuration["FileUpload:UploadPath"] ?? "uploads", uniqueFileName);
            analysis.CRDocumentName = request.CRDocument.FileName;

            analysis = await _repo.CreateAsync(analysis);

            // Parse CR document
            _logger.LogInformation("Parsing CR document for analysis {Id}", analysis.Id);
            var crContent = await _docParser.ParseAsync(request.CRDocument);

            // Get project snapshot
            string projectSnapshot;
            if (request.ProjectZip != null && request.ProjectZip.Length > 0)
            {
                _logger.LogInformation("Scanning ZIP project for analysis {Id}", analysis.Id);
                analysis.ProjectPath = $"[ZIP] {request.ProjectZip.FileName}";
                await using var zipStream = request.ProjectZip.OpenReadStream();
                projectSnapshot = await _repoScanner.ScanZipAsync(zipStream);
            }
            else if (!string.IsNullOrWhiteSpace(request.ProjectFolderPath) && Directory.Exists(request.ProjectFolderPath))
            {
                _logger.LogInformation("Scanning folder {Path} for analysis {Id}", request.ProjectFolderPath, analysis.Id);
                analysis.ProjectPath = request.ProjectFolderPath;
                projectSnapshot = await _repoScanner.ScanFolderAsync(request.ProjectFolderPath);
            }
            else
            {
                projectSnapshot = "No project structure provided. Analyze CR in isolation.";
                analysis.ProjectPath = "(None provided)";
            }

            // Call AI
            _logger.LogInformation("Calling AI for analysis {Id}", analysis.Id);
            var result = await _aiService.GAnalyzeAsync(crContent, projectSnapshot);

            // Persist results
            analysis.Status = AnalysisStatus.Completed;
            analysis.Summary = result.Summary;
            analysis.RawAIResponse = result.RawResponse;
            analysis.GeneratedPrompt = result.GeneratedPrompt;
            analysis.CompletedAt = DateTime.UtcNow;

            analysis.AffectedFiles = result.AffectedFiles.Select(f => new AffectedFile
            {
                FilePath = f.FilePath,
                ChangeType = f.ChangeType,
                Reason = f.Reason,
                SuggestedSteps = string.Join("\n", f.SuggestedSteps),
                AnalysisId = analysis.Id
            }).ToList();

            await _repo.UpdateAsync(analysis);

            _logger.LogInformation("Analysis {Id} completed successfully", analysis.Id);
            return RedirectToAction(nameof(Result), new { id = analysis.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during analysis {Id}", analysis.Id);
            analysis.Status = AnalysisStatus.Failed;
            analysis.ErrorMessage = ex.Message;
            analysis.CompletedAt = DateTime.UtcNow;
            await _repo.UpdateAsync(analysis);

            TempData["ErrorMessage"] = $"Analysis failed: {ex.Message}";
            return RedirectToAction(nameof(Result), new { id = analysis.Id });
        }
    }

    // GET /Analysis/Result/{id}
    public async Task<IActionResult> Result(int id)
    {
        var analysis = await _repo.GetByIdAsync(id);
        if (analysis == null) return NotFound();
        return View(analysis);
    }

    // GET /Analysis/History
    public async Task<IActionResult> History()
    {
        var analyses = await _repo.GetAllAsync();
        return View(analyses);
    }

    // GET /Analysis/Delete/{id}
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        await _repo.DeleteAsync(id);
        TempData["SuccessMessage"] = "Analysis deleted successfully.";
        return RedirectToAction(nameof(History));
    }
}
