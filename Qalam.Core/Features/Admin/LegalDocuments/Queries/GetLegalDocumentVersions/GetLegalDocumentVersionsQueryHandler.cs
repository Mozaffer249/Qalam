using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Legal;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Helpers;

namespace Qalam.Core.Features.Admin.LegalDocuments.Queries.GetLegalDocumentVersions;

public class GetLegalDocumentVersionsQueryHandler : ResponseHandler,
    IRequestHandler<GetLegalDocumentVersionsQuery, Response<List<LegalDocumentVersionSummaryDto>>>
{
    private readonly ILegalDocumentRepository _repository;

    public GetLegalDocumentVersionsQueryHandler(
        ILegalDocumentRepository repository,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _repository = repository;
    }

    public async Task<Response<List<LegalDocumentVersionSummaryDto>>> Handle(
        GetLegalDocumentVersionsQuery request,
        CancellationToken cancellationToken)
    {
        var doc = await _repository.GetByIdAsync(request.DocumentId, cancellationToken);
        if (doc == null)
            return NotFound<List<LegalDocumentVersionSummaryDto>>();

        var versions = await _repository.ListVersionsAsync(request.DocumentId, cancellationToken);
        return Success(entity: versions.Select(LegalDocumentMapper.ToVersionSummary).ToList());
    }
}
