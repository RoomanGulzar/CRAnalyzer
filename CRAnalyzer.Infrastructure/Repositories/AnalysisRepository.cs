using CRAnalyzer.Core.Domain.Enums;
using CRAnalyzer.Core.Domain.Models;
using CRAnalyzer.Core.Interfaces;
using CRAnalyzer.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRAnalyzer.Infrastructure.Repositories;

public class AnalysisRepository : IAnalysisRepository
{
    private readonly AppDbContext _context;

    public AnalysisRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Analysis>> GetAllAsync()
    {
        return await _context.Analyses
            .Include(a => a.AffectedFiles)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();
    }

    public async Task<Analysis?> GetByIdAsync(int id)
    {
        return await _context.Analyses
            .Include(a => a.AffectedFiles)
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<Analysis> CreateAsync(Analysis analysis)
    {
        _context.Analyses.Add(analysis);
        await _context.SaveChangesAsync();
        return analysis;
    }

    public async Task<Analysis> UpdateAsync(Analysis analysis)
    {
        _context.Analyses.Update(analysis);
        await _context.SaveChangesAsync();
        return analysis;
    }

    public async Task DeleteAsync(int id)
    {
        var analysis = await _context.Analyses.FindAsync(id);
        if (analysis != null)
        {
            _context.Analyses.Remove(analysis);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<int> GetTotalCountAsync()
        => await _context.Analyses.CountAsync();

    public async Task<int> GetCompletedCountAsync()
        => await _context.Analyses.CountAsync(a => a.Status == AnalysisStatus.Completed);

    public async Task<int> GetFailedCountAsync()
        => await _context.Analyses.CountAsync(a => a.Status == AnalysisStatus.Failed);
}
