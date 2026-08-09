using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Helpers;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Legal;
using Qalam.Data.Entity.Legal;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;
using Qalam.Service.Helpers;

namespace Qalam.Core.Features.Admin.LegalDocuments.Commands.UnpublishLegalDocumentVersion;

public class UnpublishLegalDocumentVersionCommandHandler : ResponseHandler,
    IRequestHandler<UnpublishLegalDocumentVersionCommand, Response<LegalDocumentVersionSummaryDto>>
{
    private readonly ILegalDocumentRepository _repository;
    private readonly IAuditService _audit;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UnpublishLegalDocumentVersionCommandHandler(
        ILegalDocumentRepository repository,
        IAuditService audit,
        IHttpContextAccessor httpContextAccessor,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _repository = repository;
        _audit = audit;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<Response<LegalDocumentVersionSummaryDto>> Handle(
        UnpublishLegalDocumentVersionCommand request,
        CancellationToken cancellationToken)
    {
        var version = await _repository.GetVersionByIdTrackedAsync(request.VersionId, cancellationToken);
        if (version == null)
            return NotFound<LegalDocumentVersionSummaryDto>();

        if (version.Status != LegalDocumentStatus.Published)
            return BadRequest<LegalDocumentVersionSummaryDto>("Only the published version can be unpublished.");

        var doc = await _repository.GetByIdTrackedAsync(version.LegalDocumentId, cancellationToken);
        if (doc == null)
            return NotFound<LegalDocumentVersionSummaryDto>();

        var now = DateTime.UtcNow;
        version.Status = LegalDocumentStatus.Archived;
        version.ArchivedAt = now;
        version.UpdatedAt = now;
        version.UpdatedBy = request.UserId;

        if (doc.CurrentPublishedVersionId == version.Id)
            doc.CurrentPublishedVersionId = null;

        doc.UpdatedAt = now;
        doc.UpdatedBy = request.UserId;
        await _repository.SaveChangesAsync();

        await _audit.LogAsync(
            "LegalDocument.Unpublished",
            request.UserId,
            ClientIpHelper.GetClientIpAddress(_httpContextAccessor.HttpContext),
            success: true,
            details: $"Unpublished version {version.VersionLabel} of '{doc.Code}'",
            entityType: nameof(LegalDocumentVersion),
            entityId: version.Id.ToString());

        return Success("Version unpublished", entity: LegalDocumentMapper.ToVersionSummary(version));
    }
}
