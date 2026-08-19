using Qalam.Data.Entity.Pricing;
using Qalam.Infrastructure.InfrastructureBases;
using Qalam.Data.Entity.Teacher;

namespace Qalam.Infrastructure.Abstracts;

public interface IDomainSessionPriceRepository : IGenericRepositoryAsync<DomainSessionPrice>
{
    Task<DomainSessionPrice?> GetEffectiveRateAsync(
        int domainId,
        string sessionTypeCode,
        DateTime asOf,
        CancellationToken cancellationToken = default);

    Task<DomainSessionPrice?> GetCurrentRateAsync(
        int domainId,
        string sessionTypeCode,
        CancellationToken cancellationToken = default);

    Task<List<DomainSessionPrice>> ListHistoryAsync(
        int domainId,
        string sessionTypeCode,
        CancellationToken cancellationToken = default);

    Task<List<DomainSessionPrice>> ListCurrentRatesAsync(CancellationToken cancellationToken = default);

    Task CloseCurrentRateAsync(
        int domainId,
        string sessionTypeCode,
        DateTime effectiveTo,
        CancellationToken cancellationToken = default);
}
