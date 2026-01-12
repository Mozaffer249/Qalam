using Qalam.Data.Entity.Quran;
using Qalam.Infrastructure.InfrastructureBases;

namespace Qalam.Infrastructure.Abstracts;

public interface IQuranLevelRepository : IGenericRepositoryAsync<QuranLevel>
{
    IQueryable<QuranLevel> GetQuranLevelsQueryable();
    IQueryable<QuranLevel> GetActiveQuranLevelsQueryable();
    Task<QuranLevel> GetQuranLevelWithSubjectsAsync(int id);
    Task<QuranLevel> GetQuranLevelByCodeAsync(string code);
}
