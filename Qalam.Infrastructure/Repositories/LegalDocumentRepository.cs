using Microsoft.EntityFrameworkCore;
using Qalam.Data.Entity.Legal;
using Qalam.Infrastructure.Abstracts;
using Qalam.Infrastructure.context;
using Qalam.Infrastructure.InfrastructureBases;

namespace Qalam.Infrastructure.Repositories;

public class LegalDocumentRepository : GenericRepositoryAsync<LegalDocument>, ILegalDocumentRepository
{
    private readonly ApplicationDBContext _db;
    private readonly DbSet<LegalDocument> _documents;
    private readonly DbSet<LegalDocumentVersion> _versions;
    private readonly DbSet<LegalDocumentSection> _sections;

    public LegalDocumentRepository(ApplicationDBContext context) : base(context)
    {
        _db = context;
        _documents = context.Set<LegalDocument>();
        _versions = context.Set<LegalDocumentVersion>();
        _sections = context.Set<LegalDocumentSection>();
    }

    public Task<List<LegalDocument>> ListAllWithPublishedAsync(CancellationToken cancellationToken = default) =>
        _documents.AsNoTracking()
            .Include(d => d.CurrentPublishedVersion)
            .OrderBy(d => d.DisplayOrder)
            .ThenBy(d => d.Id)
            .ToListAsync(cancellationToken);

    public Task<LegalDocument?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _documents.AsNoTracking()
            .Include(d => d.CurrentPublishedVersion)
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

    public Task<LegalDocument?> GetByCodeAsync(string code, CancellationToken cancellationToken = default) =>
        _documents.AsNoTracking()
            .Include(d => d.CurrentPublishedVersion)
            .FirstOrDefaultAsync(d => d.Code == code, cancellationToken);

    public Task<LegalDocument?> GetByCodeTrackedAsync(string code, CancellationToken cancellationToken = default) =>
        _documents.FirstOrDefaultAsync(d => d.Code == code, cancellationToken);

    public Task<LegalDocument?> GetByIdTrackedAsync(int id, CancellationToken cancellationToken = default) =>
        _documents.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

    public Task<bool> CodeExistsAsync(string code, int? excludeId = null, CancellationToken cancellationToken = default)
    {
        var query = _documents.AsNoTracking().Where(d => d.Code == code);
        if (excludeId.HasValue)
            query = query.Where(d => d.Id != excludeId.Value);
        return query.AnyAsync(cancellationToken);
    }

    public Task<LegalDocumentVersion?> GetVersionByIdAsync(int versionId, CancellationToken cancellationToken = default) =>
        _versions.AsNoTracking()
            .Include(v => v.LegalDocument)
            .FirstOrDefaultAsync(v => v.Id == versionId, cancellationToken);

    public Task<LegalDocumentVersion?> GetVersionByIdTrackedAsync(int versionId, CancellationToken cancellationToken = default) =>
        _versions
            .Include(v => v.LegalDocument)
            .FirstOrDefaultAsync(v => v.Id == versionId, cancellationToken);

    public Task<LegalDocumentVersion?> GetVersionWithSectionsAsync(int versionId, CancellationToken cancellationToken = default) =>
        _versions.AsNoTracking()
            .Include(v => v.LegalDocument)
            .Include(v => v.Sections)
            .FirstOrDefaultAsync(v => v.Id == versionId, cancellationToken);

    public Task<LegalDocumentVersion?> GetVersionWithSectionsTrackedAsync(int versionId, CancellationToken cancellationToken = default) =>
        _versions
            .Include(v => v.LegalDocument)
            .Include(v => v.Sections)
            .FirstOrDefaultAsync(v => v.Id == versionId, cancellationToken);

    public Task<List<LegalDocumentVersion>> ListVersionsAsync(int documentId, CancellationToken cancellationToken = default) =>
        _versions.AsNoTracking()
            .Where(v => v.LegalDocumentId == documentId)
            .OrderByDescending(v => v.MajorVersion)
            .ThenByDescending(v => v.MinorVersion)
            .ToListAsync(cancellationToken);

    public Task<LegalDocumentVersion?> GetDraftVersionAsync(int documentId, CancellationToken cancellationToken = default) =>
        _versions
            .Include(v => v.Sections)
            .FirstOrDefaultAsync(v =>
                v.LegalDocumentId == documentId &&
                (v.Status == LegalDocumentStatus.Draft || v.Status == LegalDocumentStatus.ReadyForReview),
                cancellationToken);

    public Task<LegalDocumentVersion?> GetPublishedVersionByCodeAsync(string code, CancellationToken cancellationToken = default) =>
        _versions.AsNoTracking()
            .Include(v => v.LegalDocument)
            .Include(v => v.Sections)
            .Where(v => v.LegalDocument.Code == code && v.Status == LegalDocumentStatus.Published)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<List<LegalDocument>> ListPublishedDocumentsAsync(CancellationToken cancellationToken = default) =>
        _documents.AsNoTracking()
            .Include(d => d.CurrentPublishedVersion)
            .Where(d => d.IsActive && d.CurrentPublishedVersionId != null)
            .OrderBy(d => d.DisplayOrder)
            .ThenBy(d => d.Id)
            .ToListAsync(cancellationToken);

    public Task<LegalDocumentSection?> GetSectionByIdTrackedAsync(int sectionId, CancellationToken cancellationToken = default) =>
        _sections
            .Include(s => s.LegalDocumentVersion)
            .FirstOrDefaultAsync(s => s.Id == sectionId, cancellationToken);

    public Task<List<LegalDocumentSection>> GetSectionsByVersionTrackedAsync(int versionId, CancellationToken cancellationToken = default) =>
        _sections
            .Where(s => s.LegalDocumentVersionId == versionId)
            .ToListAsync(cancellationToken);

    public async Task AddVersionAsync(LegalDocumentVersion version, CancellationToken cancellationToken = default)
    {
        await _versions.AddAsync(version, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task AddSectionAsync(LegalDocumentSection section, CancellationToken cancellationToken = default)
    {
        await _sections.AddAsync(section, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveSectionAsync(LegalDocumentSection section, CancellationToken cancellationToken = default)
    {
        _sections.Remove(section);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveSectionsAsync(IEnumerable<LegalDocumentSection> sections, CancellationToken cancellationToken = default)
    {
        _sections.RemoveRange(sections);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
