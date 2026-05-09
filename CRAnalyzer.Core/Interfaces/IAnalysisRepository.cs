using CRAnalyzer.Core.Domain.Models;

namespace CRAnalyzer.Core.Interfaces;

public interface IAnalysisRepository
{
    Task<IEnumerable<Analysis>> GetAllAsync();
    Task<Analysis?> GetByIdAsync(int id);
    Task<Analysis> CreateAsync(Analysis analysis);
    Task<Analysis> UpdateAsync(Analysis analysis);
    Task DeleteAsync(int id);
    Task<int> GetTotalCountAsync();
    Task<int> GetCompletedCountAsync();
    Task<int> GetFailedCountAsync();
}
