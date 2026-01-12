using Qalam.Data.Entity.Quran;
using Qalam.Data.Results;

namespace Qalam.Service.Abstracts;

public interface IQuranService
{
    // Quran Level Operations
    IQueryable<QuranLevel> GetQuranLevelsQueryable();
    IQueryable<QuranLevel> GetActiveQuranLevelsQueryable();
    Task<QuranLevel> GetQuranLevelByIdAsync(int id);
    Task<QuranLevel> GetQuranLevelWithSubjectsAsync(int id);
    Task<QuranLevel> GetQuranLevelByCodeAsync(string code);
    Task<QuranLevel> CreateQuranLevelAsync(QuranLevel level);
    Task<QuranLevel> UpdateQuranLevelAsync(QuranLevel level);
    Task<bool> DeleteQuranLevelAsync(int id);
    Task<bool> ToggleQuranLevelStatusAsync(int id);

    // Quran Content Type Operations
    IQueryable<QuranContentType> GetContentTypesQueryable();
    IQueryable<QuranContentType> GetActiveContentTypesQueryable();
    Task<QuranContentType> GetContentTypeByIdAsync(int id);
    Task<QuranContentType> GetContentTypeByCodeAsync(string code);
    Task<QuranContentType> CreateContentTypeAsync(QuranContentType contentType);
    Task<QuranContentType> UpdateContentTypeAsync(QuranContentType contentType);
    Task<bool> DeleteContentTypeAsync(int id);
    Task<bool> ToggleContentTypeStatusAsync(int id);

    // Quran Part Operations
    IQueryable<QuranPart> GetQuranPartsQueryable();
    Task<QuranPart> GetQuranPartByNumberAsync(int partNumber);

    // Quran Surah Operations
    IQueryable<QuranSurah> GetQuranSurahsQueryable();
    Task<QuranSurah> GetQuranSurahByNumberAsync(int surahNumber);
    IQueryable<QuranSurah> GetSurahsByPartQueryable(int partNumber);

    // Pagination
    Task<PaginatedResult<QuranLevel>> GetPaginatedQuranLevelsAsync(
        int pageNumber, int pageSize, string? search = null);
    Task<PaginatedResult<QuranContentType>> GetPaginatedContentTypesAsync(
        int pageNumber, int pageSize, string? search = null);
    Task<PaginatedResult<QuranSurah>> GetPaginatedSurahsAsync(
        int pageNumber, int pageSize, int? partNumber = null, string? search = null);
}
