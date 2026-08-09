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

namespace Qalam.Core.Features.Admin.LegalDocuments.Commands.CreateLegalDocument;

public class CreateLegalDocumentCommandHandler : ResponseHandler,
    IRequestHandler<CreateLegalDocumentCommand, Response<LegalDocumentListItemDto>>
{
    private readonly ILegalDocumentRepository _repository;
    private readonly IAuditService _audit;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CreateLegalDocumentCommandHandler(
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
        CreateLegalDocumentCommand request,
        CancellationToken cancellationToken)
    {
        var dto = request.Data;
        var code = dto.Code.Trim().ToLowerInvariant();

        if (await _repository.CodeExistsAsync(code, cancellationToken: cancellationToken))
            return BadRequest<LegalDocumentListItemDto>("Document code already exists.");

        var doc = new LegalDocument
        {
            Code = code,
            TitleAr = dto.TitleAr.Trim(),
            TitleEn = dto.TitleEn.Trim(),
            DisplayOrder = dto.DisplayOrder,
            RequiresConsent = dto.RequiresConsent,
            IsActive = true,
            CreatedBy = request.UserId,
            CreatedAt = DateTime.UtcNow
        };

        var version = new LegalDocumentVersion
        {
            MajorVersion = 1,
            MinorVersion = 0,
            Status = LegalDocumentStatus.Draft,
            ChangeNotes = "Initial draft",
            CreatedBy = request.UserId,
            CreatedAt = DateTime.UtcNow
        };
        doc.Versions.Add(version);

        await _repository.AddAsync(doc);

        await _audit.LogAsync(
            "LegalDocument.Created",
            request.UserId,
            ClientIpHelper.GetClientIpAddress(_httpContextAccessor.HttpContext),
            success: true,
            userAgent: ClientIpHelper.GetUserAgent(_httpContextAccessor.HttpContext),
            details: $"Created legal document '{code}'",
            entityType: nameof(LegalDocument),
            entityId: doc.Id.ToString());

        var reloaded = await _repository.GetByIdAsync(doc.Id, cancellationToken);
        return Success("Legal document created", entity: LegalDocumentMapper.ToListItem(reloaded!));
    }
}
