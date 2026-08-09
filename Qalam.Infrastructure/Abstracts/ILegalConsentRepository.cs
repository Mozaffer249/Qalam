using Qalam.Data.Entity.Legal;
using Qalam.Infrastructure.InfrastructureBases;

namespace Qalam.Infrastructure.Abstracts;

public interface ILegalConsentRepository : IGenericRepositoryAsync<UserLegalConsent>
{
    Task<List<UserLegalConsent>> GetByUserAsync(int userId, CancellationToken cancellationToken = default);

    Task<bool> HasAcceptedVersionAsync(int userId, int versionId, CancellationToken cancellationToken = default);

    Task<List<LegalDocument>> GetPendingConsentDocumentsAsync(int userId, CancellationToken cancellationToken = default);

    Task AddConsentAsync(UserLegalConsent consent, CancellationToken cancellationToken = default);
}
