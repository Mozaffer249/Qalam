using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Legal;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Helpers;

namespace Qalam.Core.Features.Admin.LegalDocuments.Queries.ListLegalDocuments;

public class ListLegalDocumentsQueryHandler : ResponseHandler,
    IRequestHandler<ListLegalDocumentsQuery, Response<List<LegalDocumentListItemDto>>>
{
    private readonly ILegalDocumentRepository _repository;

    public ListLegalDocumentsQueryHandler(
        ILegalDocumentRepository repository,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _repository = repository;
    }

    public async Task<Response<List<LegalDocumentListItemDto>>> Handle(
        ListLegalDocumentsQuery request,
        CancellationToken cancellationToken)
    {
        var docs = await _repository.ListAllWithPublishedAsync(cancellationToken);
        var items = docs.Select(LegalDocumentMapper.ToListItem).ToList();
        return Success(entity: items);
    }
}
