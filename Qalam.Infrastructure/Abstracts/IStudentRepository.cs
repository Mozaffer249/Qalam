using Qalam.Data.DTOs.Admin;
using Qalam.Data.Entity.Student;
using Qalam.Data.Results;
using Qalam.Infrastructure.InfrastructureBases;

namespace Qalam.Infrastructure.Abstracts;

public interface IStudentRepository : IGenericRepositoryAsync<Student>
{
    Task<Student?> GetByUserIdAsync(int userId);
    Task<List<Student>> GetChildrenByGuardianIdAsync(int guardianId);

    Task<PaginatedResult<AdminStudentListItemDto>> SearchForAdminAsync(
        AdminStudentListFilters filters,
        CancellationToken cancellationToken = default);

    Task<AdminStudentDetailDto?> GetAdminDetailAsync(
        int studentId,
        CancellationToken cancellationToken = default);
}

/// <summary>Filter + paging inputs for <see cref="IStudentRepository.SearchForAdminAsync"/>.</summary>
public record AdminStudentListFilters(
    string? Search,
    bool? IsMinor,
    bool? IsActive,
    int PageNumber,
    int PageSize);
