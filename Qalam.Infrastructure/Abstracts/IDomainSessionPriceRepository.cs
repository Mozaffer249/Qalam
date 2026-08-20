using Qalam.Data.Entity.Pricing;
using Qalam.Infrastructure.InfrastructureBases;
using Qalam.Data.Entity.Teacher;

namespace Qalam.Infrastructure.Abstracts;

public interface IDomainSessionPriceRepository : IGenericRepositoryAsync<DomainSessionPrice>
{
    Task<DomainSessionPrice?> GetEffectiveRateAsync(
        int domainId,
        string sessionTypeCode,
        string marketCode,
        DateTime asOf,
        CancellationToken cancellationToken = default);

    Task<DomainSessionPrice?> GetCurrentRateAsync(
        int domainId,
        string sessionTypeCode,
        string marketCode,
        CancellationToken cancellationToken = default);

    Task<List<DomainSessionPrice>> ListHistoryAsync(
        int domainId,
        string sessionTypeCode,
        string marketCode,
        CancellationToken cancellationToken = default);

    Task<List<DomainSessionPrice>> ListCurrentRatesAsync(
        string marketCode,
        CancellationToken cancellationToken = default);

    Task CloseCurrentRateAsync(
        int domainId,
        string sessionTypeCode,
        string marketCode,
        DateTime effectiveTo,
        CancellationToken cancellationToken = default);
}
