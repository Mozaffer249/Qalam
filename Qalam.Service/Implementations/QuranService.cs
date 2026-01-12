using Microsoft.EntityFrameworkCore;
using Qalam.Data.Entity.Quran;
using Qalam.Data.Results;
using Qalam.Infrastructure.Abstracts;
using Qalam.Infrastructure.context;
using Qalam.Service.Abstracts;

namespace Qalam.Service.Implementations;

public class QuranService : IQuranService
{
    private readonly IQuranLevelRepository _quranLevelRepository;
    private readonly IQuranContentTypeRepository _contentTypeRepository;
    private readonly ApplicationDBContext _context;

    public QuranService(
        IQuranLevelRepository quranLevelRepository,
        IQuranContentTypeRepository contentTypeRepository,
        ApplicationDBContext context)
    {
        _quranLevelRepository = quranLevelRepository;
        _contentTypeRepository = contentTypeRepository;
        _context = context;
    }

    #region Quran Level Operations

    public IQueryable<QuranLevel> GetQuranLevelsQueryable()
    {
        return _quranLevelRepository.GetQuranLevelsQueryable();
    }

    public IQueryable<QuranLevel> GetActiveQuranLevelsQueryable()
    {
        return _quranLevelRepository.GetActiveQuranLevelsQueryable();
    }

    public async Task<QuranLevel> GetQuranLevelByIdAsync(int id)
    {
        return await _quranLevelRepository.GetByIdAsync(id);
    }

    public async Task<QuranLevel> GetQuranLevelWithSubjectsAsync(int id)
    {
        return await _quranLevelRepository.GetQuranLevelWithSubjectsAsync(id);
    }

    public async Task<QuranLevel> GetQuranLevelByCodeAsync(string code)
    {
        return await _quranLevelRepository.GetQuranLevelByCodeAsync(code);
    }

    public async Task<QuranLevel> CreateQuranLevelAsync(QuranLevel level)
    {
        level.CreatedAt = DateTime.UtcNow;
        level.IsActive = true;
        return await _quranLevelRepository.AddAsync(level);
    }

    public async Task<QuranLevel> UpdateQuranLevelAsync(QuranLevel level)
    {
        var existing = await _quranLevelRepository.GetByIdAsync(level.Id);
        if (existing == null)
            throw new InvalidOperationException("Quran level not found");

        existing.NameAr = level.NameAr;
        existing.NameEn = level.NameEn;
        existing.DescriptionAr = level.DescriptionAr;
        existing.DescriptionEn = level.DescriptionEn;
        existing.OrderIndex = level.OrderIndex;
        existing.IsActive = level.IsActive;
        existing.UpdatedAt = DateTime.UtcNow;

        await _quranLevelRepository.UpdateAsync(existing);
        return existing;
    }

    public async Task<bool> DeleteQuranLevelAsync(int id)
    {
        var level = await _quranLevelRepository.GetByIdAsync(id);
        if (level == null)
            return false;

        await _quranLevelRepository.DeleteAsync(level);
        return true;
    }

    public async Task<bool> ToggleQuranLevelStatusAsync(int id)
    {
        var level = await _quranLevelRepository.GetByIdAsync(id);
        if (level == null)
            return false;

        level.IsActive = !level.IsActive;
        level.UpdatedAt = DateTime.UtcNow;
        await _quranLevelRepository.UpdateAsync(level);
        return true;
    }

    #endregion

    #region Quran Content Type Operations

    public IQueryable<QuranContentType> GetContentTypesQueryable()
    {
        return _contentTypeRepository.GetContentTypesQueryable();
    }

    public IQueryable<QuranContentType> GetActiveContentTypesQueryable()
    {
        return _contentTypeRepository.GetActiveContentTypesQueryable();
    }

    public async Task<QuranContentType> GetContentTypeByIdAsync(int id)
    {
        return await _contentTypeRepository.GetByIdAsync(id);
    }

    public async Task<QuranContentType> GetContentTypeByCodeAsync(string code)
    {
        return await _contentTypeRepository.GetContentTypeByCodeAsync(code);
    }

    public async Task<QuranContentType> CreateContentTypeAsync(QuranContentType contentType)
    {
        contentType.CreatedAt = DateTime.UtcNow;
        contentType.IsActive = true;
        return await _contentTypeRepository.AddAsync(contentType);
    }

    public async Task<QuranContentType> UpdateContentTypeAsync(QuranContentType contentType)
    {
        var existing = await _contentTypeRepository.GetByIdAsync(contentType.Id);
        if (existing == null)
            throw new InvalidOperationException("Content type not found");

        existing.NameAr = contentType.NameAr;
        existing.NameEn = contentType.NameEn;
        existing.Code = contentType.Code;
        existing.IsActive = contentType.IsActive;
        existing.UpdatedAt = DateTime.UtcNow;

        await _contentTypeRepository.UpdateAsync(existing);
        return existing;
    }

    public async Task<bool> DeleteContentTypeAsync(int id)
    {
        var contentType = await _contentTypeRepository.GetByIdAsync(id);
        if (contentType == null)
            return false;

        await _contentTypeRepository.DeleteAsync(contentType);
        return true;
    }

    public async Task<bool> ToggleContentTypeStatusAsync(int id)
    {
        var contentType = await _contentTypeRepository.GetByIdAsync(id);
        if (contentType == null)
            return false;

        contentType.IsActive = !contentType.IsActive;
        contentType.UpdatedAt = DateTime.UtcNow;
        await _contentTypeRepository.UpdateAsync(contentType);
        return true;
    }

    #endregion

    #region Quran Part Operations

    public IQueryable<QuranPart> GetQuranPartsQueryable()
    {
        return _context.QuranParts
            .AsNoTracking()
            .OrderBy(p => p.PartNumber)
            .AsQueryable();
    }

    public async Task<QuranPart> GetQuranPartByNumberAsync(int partNumber)
    {
        return await _context.QuranParts
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.PartNumber == partNumber);
    }

    #endregion

    #region Quran Surah Operations

    public IQueryable<QuranSurah> GetQuranSurahsQueryable()
    {
        return _context.QuranSurahs
            .AsNoTracking()
            .OrderBy(s => s.SurahNumber)
            .AsQueryable();
    }

    public async Task<QuranSurah> GetQuranSurahByNumberAsync(int surahNumber)
    {
        return await _context.QuranSurahs
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.SurahNumber == surahNumber);
    }

    public IQueryable<QuranSurah> GetSurahsByPartQueryable(int partNumber)
    {
        return _context.QuranSurahs
            .AsNoTracking()
            .Where(s => s.PartNumber == partNumber)
            .OrderBy(s => s.SurahNumber)
            .AsQueryable();
    }

    #endregion

    #region Pagination

    public async Task<PaginatedResult<QuranLevel>> GetPaginatedQuranLevelsAsync(
        int pageNumber, int pageSize, string? search = null)
    {
        var query = _quranLevelRepository.GetQuranLevelsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(ql =>
                ql.NameAr.Contains(search) ||
                ql.NameEn.Contains(search));
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(ql => ql.OrderIndex)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PaginatedResult<QuranLevel>(items, totalCount, pageNumber, pageSize);
    }

    public async Task<PaginatedResult<QuranContentType>> GetPaginatedContentTypesAsync(
        int pageNumber, int pageSize, string? search = null)
    {
        var query = _contentTypeRepository.GetContentTypesQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(ct =>
                ct.NameAr.Contains(search) ||
                ct.NameEn.Contains(search) ||
                ct.Code.Contains(search));
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(ct => ct.NameEn)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PaginatedResult<QuranContentType>(items, totalCount, pageNumber, pageSize);
    }

    public async Task<PaginatedResult<QuranSurah>> GetPaginatedSurahsAsync(
        int pageNumber, int pageSize, int? partNumber = null, string? search = null)
    {
        var query = _context.QuranSurahs.AsNoTracking().AsQueryable();

        if (partNumber.HasValue)
            query = query.Where(s => s.PartNumber == partNumber.Value);

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(s =>
                s.NameAr.Contains(search) ||
                s.NameEn.Contains(search));
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(s => s.SurahNumber)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PaginatedResult<QuranSurah>(items, totalCount, pageNumber, pageSize);
    }

    #endregion
}
