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

namespace Qalam.Core.Features.Admin.LegalDocuments.Commands.UpdateLegalDocumentVersion;

public class UpdateLegalDocumentVersionCommandHandler : ResponseHandler,
    IRequestHandler<UpdateLegalDocumentVersionCommand, Response<LegalDocumentVersionSummaryDto>>
{
    private readonly ILegalDocumentRepository _repository;
    private readonly IAuditService _audit;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UpdateLegalDocumentVersionCommandHandler(
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
        UpdateLegalDocumentVersionCommand request,
        CancellationToken cancellationToken)
    {
        var version = await _repository.GetVersionByIdTrackedAsync(request.VersionId, cancellationToken);
        if (version == null)
            return NotFound<LegalDocumentVersionSummaryDto>();

        if (version.Status is LegalDocumentStatus.Published or LegalDocumentStatus.Archived)
            return BadRequest<LegalDocumentVersionSummaryDto>("Published or archived versions cannot be edited.");

        var dto = request.Data;
        if (dto.ChangeNotes != null)
            version.ChangeNotes = dto.ChangeNotes.Trim();
        if (dto.EffectiveDate.HasValue)
            version.EffectiveDate = dto.EffectiveDate;

        if (!string.IsNullOrWhiteSpace(dto.Status))
        {
            var status = dto.Status.Trim();
            if (status is not (LegalDocumentStatus.Draft or LegalDocumentStatus.ReadyForReview))
                return BadRequest<LegalDocumentVersionSummaryDto>("Status may only be Draft or ReadyForReview.");
            version.Status = status;
        }

        version.UpdatedAt = DateTime.UtcNow;
        version.UpdatedBy = request.UserId;
        await _repository.SaveChangesAsync();

        await _audit.LogAsync(
            "LegalDocument.Edited",
            request.UserId,
            ClientIpHelper.GetClientIpAddress(_httpContextAccessor.HttpContext),
            success: true,
            details: $"Updated version {version.VersionLabel}",
            entityType: nameof(LegalDocumentVersion),
            entityId: version.Id.ToString());

        return Success("Version updated", entity: LegalDocumentMapper.ToVersionSummary(version));
    }
}
