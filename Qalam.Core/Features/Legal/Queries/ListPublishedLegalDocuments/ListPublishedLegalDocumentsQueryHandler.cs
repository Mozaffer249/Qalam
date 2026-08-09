using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Legal;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Helpers;

namespace Qalam.Core.Features.Legal.Queries.ListPublishedLegalDocuments;

public class ListPublishedLegalDocumentsQueryHandler : ResponseHandler,
    IRequestHandler<ListPublishedLegalDocumentsQuery, Response<List<PublicLegalDocumentSummaryDto>>>
{
    private readonly ILegalDocumentRepository _repository;

    public ListPublishedLegalDocumentsQueryHandler(
        ILegalDocumentRepository repository,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _repository = repository;
    }

    public async Task<Response<List<PublicLegalDocumentSummaryDto>>> Handle(
        ListPublishedLegalDocumentsQuery request,
        CancellationToken cancellationToken)
    {
        var docs = await _repository.ListPublishedDocumentsAsync(cancellationToken);
        return Success(entity: docs.Select(LegalDocumentMapper.ToPublicSummary).ToList());
    }
}
