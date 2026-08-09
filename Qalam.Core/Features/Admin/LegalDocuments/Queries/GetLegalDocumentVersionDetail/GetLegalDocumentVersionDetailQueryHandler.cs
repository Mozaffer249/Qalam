using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Legal;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Helpers;

namespace Qalam.Core.Features.Admin.LegalDocuments.Queries.GetLegalDocumentVersionDetail;

public class GetLegalDocumentVersionDetailQueryHandler : ResponseHandler,
    IRequestHandler<GetLegalDocumentVersionDetailQuery, Response<LegalDocumentVersionDetailDto>>
{
    private readonly ILegalDocumentRepository _repository;

    public GetLegalDocumentVersionDetailQueryHandler(
        ILegalDocumentRepository repository,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _repository = repository;
    }

    public async Task<Response<LegalDocumentVersionDetailDto>> Handle(
        GetLegalDocumentVersionDetailQuery request,
        CancellationToken cancellationToken)
    {
        var version = await _repository.GetVersionWithSectionsAsync(request.VersionId, cancellationToken);
        if (version == null)
            return NotFound<LegalDocumentVersionDetailDto>();

        return Success(entity: LegalDocumentMapper.ToVersionDetail(version));
    }
}
