using Qalam.Data.DTOs.Admin;

namespace Qalam.Infrastructure.Abstracts;

public interface IStudentSessionReadRepository
{
    Task<List<StudentSessionListItemDto>> ListForStudentUserAsync(
        int userId,
        CancellationToken cancellationToken = default);
}
