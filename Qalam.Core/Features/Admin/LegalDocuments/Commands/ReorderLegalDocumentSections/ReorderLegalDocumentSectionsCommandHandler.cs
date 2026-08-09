using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Helpers;
using Qalam.Core.Resources.Shared;
using Qalam.Data.Entity.Legal;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Admin.LegalDocuments.Commands.ReorderLegalDocumentSections;

public class ReorderLegalDocumentSectionsCommandHandler : ResponseHandler,
    IRequestHandler<ReorderLegalDocumentSectionsCommand, Response<string>>
{
    private readonly ILegalDocumentRepository _repository;
    private readonly IAuditService _audit;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ReorderLegalDocumentSectionsCommandHandler(
        ILegalDocumentRepository repository,
        IAuditService audit,
        IHttpContextAccessor httpContextAccessor,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _repository = repository;
        _audit = audit;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<Response<string>> Handle(
        ReorderLegalDocumentSectionsCommand request,
        CancellationToken cancellationToken)
    {
        var version = await _repository.GetVersionByIdTrackedAsync(request.VersionId, cancellationToken);
        if (version == null)
            return NotFound<string>();

        if (version.Status is LegalDocumentStatus.Published or LegalDocumentStatus.Archived)
            return BadRequest<string>("Cannot reorder sections of a published or archived version.");

        var sections = await _repository.GetSectionsByVersionTrackedAsync(request.VersionId, cancellationToken);
        var byId = sections.ToDictionary(s => s.Id);

        foreach (var item in request.Data.Items)
        {
            if (!byId.TryGetValue(item.Id, out var section))
                return BadRequest<string>($"Section {item.Id} not found in this version.");

            if (item.ParentSectionId.HasValue)
            {
                if (!byId.ContainsKey(item.ParentSectionId.Value))
                    return BadRequest<string>($"Parent section {item.ParentSectionId} not found.");
                if (WouldCreateCycle(item.Id, item.ParentSectionId.Value, byId, request.Data.Items))
                    return BadRequest<string>("Reorder would create a cycle.");
            }

            section.ParentSectionId = item.ParentSectionId;
            section.DisplayOrder = item.DisplayOrder;
            section.UpdatedAt = DateTime.UtcNow;
            section.UpdatedBy = request.UserId;
        }

        version.UpdatedAt = DateTime.UtcNow;
        version.UpdatedBy = request.UserId;
        await _repository.SaveChangesAsync();

        await _audit.LogAsync(
            "LegalDocument.Reordered",
            request.UserId,
            ClientIpHelper.GetClientIpAddress(_httpContextAccessor.HttpContext),
            success: true,
            details: $"Reordered {request.Data.Items.Count} sections in version {version.Id}",
            entityType: nameof(LegalDocumentVersion),
            entityId: version.Id.ToString());

        return Success<string>("Sections reordered");
    }

    private static bool WouldCreateCycle(
        int sectionId,
        int newParentId,
        Dictionary<int, LegalDocumentSection> byId,
        List<Data.DTOs.Legal.ReorderLegalDocumentSectionItemDto> pending)
    {
        var parentMap = byId.ToDictionary(kv => kv.Key, kv => kv.Value.ParentSectionId);
        foreach (var p in pending)
            parentMap[p.Id] = p.ParentSectionId;

        parentMap[sectionId] = newParentId;

        var visited = new HashSet<int>();
        var current = (int?)newParentId;
        while (current.HasValue)
        {
            if (current.Value == sectionId)
                return true;
            if (!visited.Add(current.Value))
                return true;
            parentMap.TryGetValue(current.Value, out current);
        }
        return false;
    }
}
