using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Helpers;

namespace Qalam.Service.Abstracts;

public interface IStudentIdentityService
{
    Task<StudentIdentityRule?> GetRuleForNationalityAsync(int nationalityId, CancellationToken ct = default);
    Task<List<IdentityType>> GetAllowedIdentityTypesAsync(int nationalityId, CancellationToken ct = default);
    Task<StudentIdentitySettings> GetSettingsAsync(CancellationToken ct = default);
}
