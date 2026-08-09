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

namespace Qalam.Core.Features.Admin.LegalDocuments.Commands.UpdateLegalDocumentSection;

public class UpdateLegalDocumentSectionCommandHandler : ResponseHandler,
    IRequestHandler<UpdateLegalDocumentSectionCommand, Response<LegalDocumentSectionDto>>
{
    private readonly ILegalDocumentRepository _repository;
    private readonly ILegalContentSanitizer _sanitizer;
    private readonly IAuditService _audit;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UpdateLegalDocumentSectionCommandHandler(
        ILegalDocumentRepository repository,
        ILegalContentSanitizer sanitizer,
        IAuditService audit,
        IHttpContextAccessor httpContextAccessor,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _repository = repository;
        _sanitizer = sanitizer;
        _audit = audit;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<Response<LegalDocumentSectionDto>> Handle(
        UpdateLegalDocumentSectionCommand request,
        CancellationToken cancellationToken)
    {
        var section = await _repository.GetSectionByIdTrackedAsync(request.SectionId, cancellationToken);
        if (section == null)
            return NotFound<LegalDocumentSectionDto>();

        var version = section.LegalDocumentVersion;
        if (version.Status is LegalDocumentStatus.Published or LegalDocumentStatus.Archived)
            return BadRequest<LegalDocumentSectionDto>("Cannot edit sections of a published or archived version.");

        var dto = request.Data;
        if (!string.IsNullOrWhiteSpace(dto.AnchorKey))
        {
            var newKey = dto.AnchorKey.Trim().ToLowerInvariant();
            if (newKey != section.AnchorKey)
            {
                var siblings = await _repository.GetSectionsByVersionTrackedAsync(section.LegalDocumentVersionId, cancellationToken);
                if (siblings.Any(s => s.Id != section.Id && s.AnchorKey == newKey))
                    return BadRequest<LegalDocumentSectionDto>("Anchor key already exists in this version.");
                section.AnchorKey = newKey;
            }
        }

        section.TitleAr = dto.TitleAr.Trim();
        section.TitleEn = dto.TitleEn.Trim();
        section.ContentAr = _sanitizer.Sanitize(dto.ContentAr);
        section.ContentEn = _sanitizer.Sanitize(dto.ContentEn);
        section.IsEnabled = dto.IsEnabled;
        section.UpdatedAt = DateTime.UtcNow;
        section.UpdatedBy = request.UserId;

        version.UpdatedAt = DateTime.UtcNow;
        version.UpdatedBy = request.UserId;
        await _repository.SaveChangesAsync();

        await _audit.LogAsync(
            "LegalDocument.Edited",
            request.UserId,
            ClientIpHelper.GetClientIpAddress(_httpContextAccessor.HttpContext),
            success: true,
            details: $"Updated section '{section.AnchorKey}'",
            entityType: nameof(LegalDocumentSection),
            entityId: section.Id.ToString());

        return Success("Section updated", entity: new LegalDocumentSectionDto
        {
            Id = section.Id,
            LegalDocumentVersionId = section.LegalDocumentVersionId,
            ParentSectionId = section.ParentSectionId,
            AnchorKey = section.AnchorKey,
            TitleAr = section.TitleAr,
            TitleEn = section.TitleEn,
            ContentAr = section.ContentAr,
            ContentEn = section.ContentEn,
            DisplayOrder = section.DisplayOrder,
            IsEnabled = section.IsEnabled
        });
    }
}
