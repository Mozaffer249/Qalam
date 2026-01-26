using Qalam.Data.DTOs;
using Qalam.Data.Entity.Education;
using Qalam.Data.Results;

namespace Qalam.Service.Abstracts;

public interface ICurriculumService
{
    // Query Operations
    IQueryable<Curriculum> GetCurriculumsQueryable();
    IQueryable<Curriculum> GetActiveCurriculumsQueryable();
    Task<Curriculum> GetCurriculumByIdAsync(int id);
    Task<Curriculum> GetCurriculumWithLevelsAsync(int id);
    Task<Curriculum> GetCurriculumByCodeAsync(string code);

    // DTO Query Operations
    IQueryable<CurriculumDto> GetCurriculumsDtoQueryable();
    Task<CurriculumDto?> GetCurriculumDtoByIdAsync(int id);

    // Pagination
    Task<PaginatedResult<CurriculumDto>> GetPaginatedCurriculumsAsync(
        int pageNumber, int pageSize, string? search = null, int? domainId = null);

    // Command Operations
    Task<Curriculum> CreateCurriculumAsync(Curriculum curriculum);
    Task<Curriculum> UpdateCurriculumAsync(Curriculum curriculum);
    Task<bool> DeleteCurriculumAsync(int id);
    Task<bool> ToggleCurriculumStatusAsync(int id);

    // Validation
    Task<bool> IsCurriculumCodeUniqueAsync(string code, int? excludeId = null);
}
