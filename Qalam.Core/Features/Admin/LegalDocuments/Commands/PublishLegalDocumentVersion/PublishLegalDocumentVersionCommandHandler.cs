using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Helpers;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Legal;
using Qalam.Data.Entity.Legal;
using Qalam.Infrastructure.Abstracts;
using Qalam.Infrastructure.context;
using Qalam.Service.Abstracts;
using Qalam.Service.Helpers;

namespace Qalam.Core.Features.Admin.LegalDocuments.Commands.PublishLegalDocumentVersion;

public class PublishLegalDocumentVersionCommandHandler : ResponseHandler,
    IRequestHandler<PublishLegalDocumentVersionCommand, Response<LegalDocumentVersionSummaryDto>>
{
    private readonly ILegalDocumentRepository _repository;
    private readonly ApplicationDBContext _db;
    private readonly IAuditService _audit;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public PublishLegalDocumentVersionCommandHandler(
        ILegalDocumentRepository repository,
        ApplicationDBContext db,
        IAuditService audit,
        IHttpContextAccessor httpContextAccessor,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _repository = repository;
        _db = db;
        _audit = audit;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<Response<LegalDocumentVersionSummaryDto>> Handle(
        PublishLegalDocumentVersionCommand request,
        CancellationToken cancellationToken)
    {
        var version = await _repository.GetVersionByIdTrackedAsync(request.VersionId, cancellationToken);
        if (version == null)
            return NotFound<LegalDocumentVersionSummaryDto>();

        if (version.Status is LegalDocumentStatus.Published)
            return BadRequest<LegalDocumentVersionSummaryDto>("Version is already published.");

        if (version.Status is LegalDocumentStatus.Archived)
            return BadRequest<LegalDocumentVersionSummaryDto>("Archived versions cannot be published directly. Restore as a new draft first.");

        var doc = await _repository.GetByIdTrackedAsync(version.LegalDocumentId, cancellationToken);
        if (doc == null)
            return NotFound<LegalDocumentVersionSummaryDto>();

        await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var now = DateTime.UtcNow;

            // Archive current published version
            var currentPublished = await _db.LegalDocumentVersions
                .Where(v => v.LegalDocumentId == doc.Id && v.Status == LegalDocumentStatus.Published && v.Id != version.Id)
                .ToListAsync(cancellationToken);

            foreach (var old in currentPublished)
            {
                old.Status = LegalDocumentStatus.Archived;
                old.ArchivedAt = now;
                old.UpdatedAt = now;
                old.UpdatedBy = request.UserId;
            }

            if (!string.IsNullOrWhiteSpace(request.Data.ChangeNotes))
                version.ChangeNotes = request.Data.ChangeNotes.Trim();

            version.Status = LegalDocumentStatus.Published;
            version.PublishedAt = now;
            version.PublishedByUserId = request.UserId;
            version.EffectiveDate = request.Data.EffectiveDate ?? now;
            version.UpdatedAt = now;
            version.UpdatedBy = request.UserId;

            doc.CurrentPublishedVersionId = version.Id;
            doc.UpdatedAt = now;
            doc.UpdatedBy = request.UserId;

            await _db.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
        }

        await _audit.LogAsync(
            "LegalDocument.Published",
            request.UserId,
            ClientIpHelper.GetClientIpAddress(_httpContextAccessor.HttpContext),
            success: true,
            userAgent: ClientIpHelper.GetUserAgent(_httpContextAccessor.HttpContext),
            details: $"Published version {version.VersionLabel} of '{doc.Code}'",
            entityType: nameof(LegalDocumentVersion),
            entityId: version.Id.ToString());

        return Success("Version published", entity: LegalDocumentMapper.ToVersionSummary(version));
    }
}
