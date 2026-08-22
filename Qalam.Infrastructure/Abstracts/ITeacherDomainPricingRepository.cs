using Qalam.Data.Entity.Teacher;
using Qalam.Infrastructure.InfrastructureBases;

namespace Qalam.Infrastructure.Abstracts;

public interface ITeacherDomainPricingRepository : IGenericRepositoryAsync<TeacherDomainPricing>
{
    Task<TeacherDomainPricing?> GetByTeacherAndDomainAsync(
        int teacherId,
        int domainId,
        CancellationToken cancellationToken = default);

    Task<List<TeacherDomainPricing>> ListByTeacherAsync(
        int teacherId,
        CancellationToken cancellationToken = default);

    Task<TeacherDomainPricing> GetOrCreateAsync(
        int teacherId,
        int domainId,
        CancellationToken cancellationToken = default);
}
