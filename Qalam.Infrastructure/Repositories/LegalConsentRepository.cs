using Microsoft.EntityFrameworkCore;
using Qalam.Data.Entity.Legal;
using Qalam.Infrastructure.Abstracts;
using Qalam.Infrastructure.context;
using Qalam.Infrastructure.InfrastructureBases;

namespace Qalam.Infrastructure.Repositories;

public class LegalConsentRepository : GenericRepositoryAsync<UserLegalConsent>, ILegalConsentRepository
{
    private readonly ApplicationDBContext _db;
    private readonly DbSet<UserLegalConsent> _consents;
    private readonly DbSet<LegalDocument> _documents;

    public LegalConsentRepository(ApplicationDBContext context) : base(context)
    {
        _db = context;
        _consents = context.Set<UserLegalConsent>();
        _documents = context.Set<LegalDocument>();
    }

    public Task<List<UserLegalConsent>> GetByUserAsync(int userId, CancellationToken cancellationToken = default) =>
        _consents.AsNoTracking()
            .Include(c => c.LegalDocument)
            .Include(c => c.LegalDocumentVersion)
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.AcceptedAt)
            .ToListAsync(cancellationToken);

    public Task<bool> HasAcceptedVersionAsync(int userId, int versionId, CancellationToken cancellationToken = default) =>
        _consents.AsNoTracking()
            .AnyAsync(c => c.UserId == userId && c.LegalDocumentVersionId == versionId, cancellationToken);

    public async Task<List<LegalDocument>> GetPendingConsentDocumentsAsync(int userId, CancellationToken cancellationToken = default)
    {
        var required = await _documents.AsNoTracking()
            .Include(d => d.CurrentPublishedVersion)
            .Where(d => d.IsActive && d.RequiresConsent && d.CurrentPublishedVersionId != null)
            .ToListAsync(cancellationToken);

        if (required.Count == 0)
            return required;

        var acceptedVersionIds = await _consents.AsNoTracking()
            .Where(c => c.UserId == userId)
            .Select(c => c.LegalDocumentVersionId)
            .ToListAsync(cancellationToken);

        var acceptedSet = acceptedVersionIds.ToHashSet();
        return required
            .Where(d => d.CurrentPublishedVersionId.HasValue && !acceptedSet.Contains(d.CurrentPublishedVersionId.Value))
            .OrderBy(d => d.DisplayOrder)
            .ToList();
    }

    public async Task AddConsentAsync(UserLegalConsent consent, CancellationToken cancellationToken = default)
    {
        await _consents.AddAsync(consent, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
