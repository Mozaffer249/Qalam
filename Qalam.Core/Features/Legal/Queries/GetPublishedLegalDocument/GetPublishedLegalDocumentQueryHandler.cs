using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Legal;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Helpers;

namespace Qalam.Core.Features.Legal.Queries.GetPublishedLegalDocument;

public class GetPublishedLegalDocumentQueryHandler : ResponseHandler,
    IRequestHandler<GetPublishedLegalDocumentQuery, Response<PublicLegalDocumentDto>>
{
    private readonly ILegalDocumentRepository _repository;

    public GetPublishedLegalDocumentQueryHandler(
        ILegalDocumentRepository repository,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _repository = repository;
    }

    public async Task<Response<PublicLegalDocumentDto>> Handle(
        GetPublishedLegalDocumentQuery request,
        CancellationToken cancellationToken)
    {
        var code = request.Code?.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(code))
            return BadRequest<PublicLegalDocumentDto>("Document code is required.");

        var version = await _repository.GetPublishedVersionByCodeAsync(code, cancellationToken);
        if (version == null)
            return NotFound<PublicLegalDocumentDto>();

        return Success(entity: LegalDocumentMapper.ToPublicDocument(version));
    }
}
