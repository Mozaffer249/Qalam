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

namespace Qalam.Core.Features.Admin.LegalDocuments.Commands.UpdateLegalDocument;

public class UpdateLegalDocumentCommandHandler : ResponseHandler,
    IRequestHandler<UpdateLegalDocumentCommand, Response<LegalDocumentListItemDto>>
{
    private readonly ILegalDocumentRepository _repository;
    private readonly IAuditService _audit;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UpdateLegalDocumentCommandHandler(
        ILegalDocumentRepository repository,
        IAuditService audit,
        IHttpContextAccessor httpContextAccessor,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _repository = repository;
        _audit = audit;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<Response<LegalDocumentListItemDto>> Handle(
        UpdateLegalDocumentCommand request,
        CancellationToken cancellationToken)
    {
        var doc = await _repository.GetByIdTrackedAsync(request.Id, cancellationToken);
        if (doc == null)
            return NotFound<LegalDocumentListItemDto>();

        var dto = request.Data;
        doc.TitleAr = dto.TitleAr.Trim();
        doc.TitleEn = dto.TitleEn.Trim();
        doc.DisplayOrder = dto.DisplayOrder;
        doc.IsActive = dto.IsActive;
        doc.RequiresConsent = dto.RequiresConsent;
        doc.UpdatedAt = DateTime.UtcNow;
        doc.UpdatedBy = request.UserId;

        await _repository.UpdateAsync(doc);

        await _audit.LogAsync(
            "LegalDocument.Edited",
            request.UserId,
            ClientIpHelper.GetClientIpAddress(_httpContextAccessor.HttpContext),
            success: true,
            userAgent: ClientIpHelper.GetUserAgent(_httpContextAccessor.HttpContext),
            details: $"Updated legal document '{doc.Code}'",
            entityType: nameof(LegalDocument),
            entityId: doc.Id.ToString());

        var reloaded = await _repository.GetByIdAsync(doc.Id, cancellationToken);
        return Success("Legal document updated", entity: LegalDocumentMapper.ToListItem(reloaded!));
    }
}
