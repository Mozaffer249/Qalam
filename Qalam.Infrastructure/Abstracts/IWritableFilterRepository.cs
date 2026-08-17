using Qalam.Data.DTOs;
using Qalam.Data.Entity.Education;
using Qalam.Infrastructure.InfrastructureBases;

namespace Qalam.Infrastructure.Abstracts;

public interface IWritableFilterRepository : IGenericRepositoryAsync<WritableFilterValue>
{
    Task<List<WritableFilterSlot>> GetActiveSlotsByDomainIdAsync(int domainId, CancellationToken ct = default);

    Task<List<FilterOptionDto>> GetValuesAsOptionsAsync(
        int slotId,
        string? subjectCode = null,
        CancellationToken ct = default);

    Task<WritableFilterSlot?> GetSlotByDomainAndCodeAsync(int domainId, string slotCode, CancellationToken ct = default);

    Task<WritableFilterValue?> FindByNormalizedAsync(int slotId, string normalizedText, CancellationToken ct = default);

    Task<List<WritableFilterValue>> GetByIdsAsync(IReadOnlyCollection<int> ids, CancellationToken ct = default);
}
