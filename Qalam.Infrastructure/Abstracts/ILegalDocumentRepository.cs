using Qalam.Data.Entity.Legal;
using Qalam.Infrastructure.InfrastructureBases;

namespace Qalam.Infrastructure.Abstracts;

public interface ILegalDocumentRepository : IGenericRepositoryAsync<LegalDocument>
{
    Task<List<LegalDocument>> ListAllWithPublishedAsync(CancellationToken cancellationToken = default);

    Task<LegalDocument?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<LegalDocument?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);

    Task<LegalDocument?> GetByCodeTrackedAsync(string code, CancellationToken cancellationToken = default);

    Task<LegalDocument?> GetByIdTrackedAsync(int id, CancellationToken cancellationToken = default);

    Task<bool> CodeExistsAsync(string code, int? excludeId = null, CancellationToken cancellationToken = default);

    Task<LegalDocumentVersion?> GetVersionByIdAsync(int versionId, CancellationToken cancellationToken = default);

    Task<LegalDocumentVersion?> GetVersionByIdTrackedAsync(int versionId, CancellationToken cancellationToken = default);

    /// <summary>Loads a version with its full section tree (flat list; caller builds hierarchy).</summary>
    Task<LegalDocumentVersion?> GetVersionWithSectionsAsync(int versionId, CancellationToken cancellationToken = default);

    Task<LegalDocumentVersion?> GetVersionWithSectionsTrackedAsync(int versionId, CancellationToken cancellationToken = default);

    Task<List<LegalDocumentVersion>> ListVersionsAsync(int documentId, CancellationToken cancellationToken = default);

    Task<LegalDocumentVersion?> GetDraftVersionAsync(int documentId, CancellationToken cancellationToken = default);

    Task<LegalDocumentVersion?> GetPublishedVersionByCodeAsync(string code, CancellationToken cancellationToken = default);

    Task<List<LegalDocument>> ListPublishedDocumentsAsync(CancellationToken cancellationToken = default);

    Task<LegalDocumentSection?> GetSectionByIdTrackedAsync(int sectionId, CancellationToken cancellationToken = default);

    Task<List<LegalDocumentSection>> GetSectionsByVersionTrackedAsync(int versionId, CancellationToken cancellationToken = default);

    Task AddVersionAsync(LegalDocumentVersion version, CancellationToken cancellationToken = default);

    Task AddSectionAsync(LegalDocumentSection section, CancellationToken cancellationToken = default);

    Task RemoveSectionAsync(LegalDocumentSection section, CancellationToken cancellationToken = default);

    Task RemoveSectionsAsync(IEnumerable<LegalDocumentSection> sections, CancellationToken cancellationToken = default);
}
