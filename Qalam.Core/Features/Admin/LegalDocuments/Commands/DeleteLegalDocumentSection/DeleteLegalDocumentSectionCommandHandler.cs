using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Helpers;
using Qalam.Core.Resources.Shared;
using Qalam.Data.Entity.Legal;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Admin.LegalDocuments.Commands.DeleteLegalDocumentSection;

public class DeleteLegalDocumentSectionCommandHandler : ResponseHandler,
    IRequestHandler<DeleteLegalDocumentSectionCommand, Response<string>>
{
    private readonly ILegalDocumentRepository _repository;
    private readonly IAuditService _audit;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public DeleteLegalDocumentSectionCommandHandler(
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
        DeleteLegalDocumentSectionCommand request,
        CancellationToken cancellationToken)
    {
        var section = await _repository.GetSectionByIdTrackedAsync(request.SectionId, cancellationToken);
        if (section == null)
            return NotFound<string>();

        var version = section.LegalDocumentVersion;
        if (version.Status is LegalDocumentStatus.Published or LegalDocumentStatus.Archived)
            return BadRequest<string>("Cannot delete sections of a published or archived version.");

        var all = await _repository.GetSectionsByVersionTrackedAsync(section.LegalDocumentVersionId, cancellationToken);
        var toDelete = CollectDescendants(section.Id, all);
        toDelete.Add(section);

        // Delete children first (Restrict FK)
        var ordered = toDelete
            .OrderByDescending(s => Depth(s.Id, all))
            .ToList();

        await _repository.RemoveSectionsAsync(ordered, cancellationToken);

        version.UpdatedAt = DateTime.UtcNow;
        version.UpdatedBy = request.UserId;
        await _repository.SaveChangesAsync();

        await _audit.LogAsync(
            "LegalDocument.Deleted",
            request.UserId,
            ClientIpHelper.GetClientIpAddress(_httpContextAccessor.HttpContext),
            success: true,
            details: $"Deleted section '{section.AnchorKey}' and {toDelete.Count - 1} descendants",
            entityType: nameof(LegalDocumentSection),
            entityId: section.Id.ToString());

        return Success<string>("Section deleted");
    }

    private static List<LegalDocumentSection> CollectDescendants(int rootId, List<LegalDocumentSection> all)
    {
        var result = new List<LegalDocumentSection>();
        var queue = new Queue<int>();
        queue.Enqueue(rootId);
        while (queue.Count > 0)
        {
            var id = queue.Dequeue();
            foreach (var child in all.Where(s => s.ParentSectionId == id))
            {
                result.Add(child);
                queue.Enqueue(child.Id);
            }
        }
        return result;
    }

    private static int Depth(int id, List<LegalDocumentSection> all)
    {
        var depth = 0;
        var current = all.FirstOrDefault(s => s.Id == id);
        while (current?.ParentSectionId != null)
        {
            depth++;
            current = all.FirstOrDefault(s => s.Id == current.ParentSectionId.Value);
        }
        return depth;
    }
}
