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

namespace Qalam.Core.Features.Admin.LegalDocuments.Commands.CreateLegalDocumentVersion;

public class CreateLegalDocumentVersionCommandHandler : ResponseHandler,
    IRequestHandler<CreateLegalDocumentVersionCommand, Response<LegalDocumentVersionDetailDto>>
{
    private readonly ILegalDocumentRepository _repository;
    private readonly IAuditService _audit;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CreateLegalDocumentVersionCommandHandler(
        ILegalDocumentRepository repository,
        IAuditService audit,
        IHttpContextAccessor httpContextAccessor,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _repository = repository;
        _audit = audit;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<Response<LegalDocumentVersionDetailDto>> Handle(
        CreateLegalDocumentVersionCommand request,
        CancellationToken cancellationToken)
    {
        var doc = await _repository.GetByIdTrackedAsync(request.DocumentId, cancellationToken);
        if (doc == null)
            return NotFound<LegalDocumentVersionDetailDto>();

        var existingDraft = await _repository.GetDraftVersionAsync(request.DocumentId, cancellationToken);
        if (existingDraft != null)
            return BadRequest<LegalDocumentVersionDetailDto>(
                "A draft or ready-for-review version already exists. Edit or publish it first.");

        var versions = await _repository.ListVersionsAsync(request.DocumentId, cancellationToken);
        var latest = versions.FirstOrDefault();
        int major = latest?.MajorVersion ?? 1;
        int minor = latest?.MinorVersion ?? 0;

        if (request.Data.IsMajor)
        {
            major += 1;
            minor = 0;
        }
        else
        {
            minor += 1;
            if (latest == null)
            {
                major = 1;
                minor = 0;
            }
        }

        LegalDocumentVersion? source = null;
        if (request.Data.SourceVersionId.HasValue)
        {
            source = await _repository.GetVersionWithSectionsAsync(request.Data.SourceVersionId.Value, cancellationToken);
            if (source == null || source.LegalDocumentId != request.DocumentId)
                return BadRequest<LegalDocumentVersionDetailDto>("Source version not found for this document.");
        }
        else if (doc.CurrentPublishedVersionId.HasValue)
        {
            source = await _repository.GetVersionWithSectionsAsync(doc.CurrentPublishedVersionId.Value, cancellationToken);
        }

        var version = new LegalDocumentVersion
        {
            LegalDocumentId = doc.Id,
            MajorVersion = major,
            MinorVersion = minor,
            Status = LegalDocumentStatus.Draft,
            ChangeNotes = request.Data.ChangeNotes?.Trim(),
            CreatedBy = request.UserId,
            CreatedAt = DateTime.UtcNow
        };

        if (source != null)
            LegalDocumentMapper.CloneSections(source, version);

        await _repository.AddVersionAsync(version, cancellationToken);

        var action = request.Data.SourceVersionId.HasValue ? "LegalDocument.Restored" : "LegalDocument.VersionCreated";
        await _audit.LogAsync(
            action,
            request.UserId,
            ClientIpHelper.GetClientIpAddress(_httpContextAccessor.HttpContext),
            success: true,
            userAgent: ClientIpHelper.GetUserAgent(_httpContextAccessor.HttpContext),
            details: $"Created version {version.VersionLabel} for '{doc.Code}'" +
                     (request.Data.SourceVersionId.HasValue ? $" from version {source?.VersionLabel}" : ""),
            entityType: nameof(LegalDocumentVersion),
            entityId: version.Id.ToString());

        var detail = await _repository.GetVersionWithSectionsAsync(version.Id, cancellationToken);
        return Success("Version created", entity: LegalDocumentMapper.ToVersionDetail(detail!));
    }
}
