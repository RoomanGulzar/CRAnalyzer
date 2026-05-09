using CRAnalyzer.Core.Interfaces;
using CRAnalyzer.Web.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace CRAnalyzer.Web.Controllers;

public class HomeController : Controller
{
    private readonly IAnalysisRepository _repo;
    private readonly ILogger<HomeController> _logger;

    public HomeController(IAnalysisRepository repo, ILogger<HomeController> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var total = await _repo.GetTotalCountAsync();
        var completed = await _repo.GetCompletedCountAsync();
        var failed = await _repo.GetFailedCountAsync();
        var recent = (await _repo.GetAllAsync()).Take(5);

        ViewBag.Total = total;
        ViewBag.Completed = completed;
        ViewBag.Failed = failed;
        ViewBag.Pending = total - completed - failed;
        ViewBag.RecentAnalyses = recent;

        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
