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

namespace Qalam.Core.Features.Admin.LegalDocuments.Commands.CreateLegalDocumentSection;

public class CreateLegalDocumentSectionCommandHandler : ResponseHandler,
    IRequestHandler<CreateLegalDocumentSectionCommand, Response<LegalDocumentSectionDto>>
{
    private readonly ILegalDocumentRepository _repository;
    private readonly ILegalContentSanitizer _sanitizer;
    private readonly IAuditService _audit;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CreateLegalDocumentSectionCommandHandler(
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
        CreateLegalDocumentSectionCommand request,
        CancellationToken cancellationToken)
    {
        var version = await _repository.GetVersionByIdTrackedAsync(request.VersionId, cancellationToken);
        if (version == null)
            return NotFound<LegalDocumentSectionDto>();

        if (version.Status is LegalDocumentStatus.Published or LegalDocumentStatus.Archived)
            return BadRequest<LegalDocumentSectionDto>("Cannot edit sections of a published or archived version.");

        var dto = request.Data;
        var sections = await _repository.GetSectionsByVersionTrackedAsync(request.VersionId, cancellationToken);

        if (sections.Any(s => s.AnchorKey == dto.AnchorKey.Trim().ToLowerInvariant()))
            return BadRequest<LegalDocumentSectionDto>("Anchor key already exists in this version.");

        if (dto.ParentSectionId.HasValue && sections.All(s => s.Id != dto.ParentSectionId.Value))
            return BadRequest<LegalDocumentSectionDto>("Parent section not found in this version.");

        var maxOrder = sections
            .Where(s => s.ParentSectionId == dto.ParentSectionId)
            .Select(s => (int?)s.DisplayOrder)
            .Max() ?? -1;

        var section = new LegalDocumentSection
        {
            LegalDocumentVersionId = request.VersionId,
            ParentSectionId = dto.ParentSectionId,
            AnchorKey = dto.AnchorKey.Trim().ToLowerInvariant(),
            TitleAr = dto.TitleAr.Trim(),
            TitleEn = dto.TitleEn.Trim(),
            ContentAr = _sanitizer.Sanitize(dto.ContentAr),
            ContentEn = _sanitizer.Sanitize(dto.ContentEn),
            DisplayOrder = dto.DisplayOrder ?? (maxOrder + 1),
            IsEnabled = dto.IsEnabled,
            CreatedBy = request.UserId,
            CreatedAt = DateTime.UtcNow
        };

        await _repository.AddSectionAsync(section, cancellationToken);

        version.UpdatedAt = DateTime.UtcNow;
        version.UpdatedBy = request.UserId;
        await _repository.SaveChangesAsync();

        await _audit.LogAsync(
            "LegalDocument.Edited",
            request.UserId,
            ClientIpHelper.GetClientIpAddress(_httpContextAccessor.HttpContext),
            success: true,
            details: $"Created section '{section.AnchorKey}'",
            entityType: nameof(LegalDocumentSection),
            entityId: section.Id.ToString());

        return Success("Section created", entity: Map(section));
    }

    private static LegalDocumentSectionDto Map(LegalDocumentSection s) => new()
    {
        Id = s.Id,
        LegalDocumentVersionId = s.LegalDocumentVersionId,
        ParentSectionId = s.ParentSectionId,
        AnchorKey = s.AnchorKey,
        TitleAr = s.TitleAr,
        TitleEn = s.TitleEn,
        ContentAr = s.ContentAr,
        ContentEn = s.ContentEn,
        DisplayOrder = s.DisplayOrder,
        IsEnabled = s.IsEnabled
    };
}
