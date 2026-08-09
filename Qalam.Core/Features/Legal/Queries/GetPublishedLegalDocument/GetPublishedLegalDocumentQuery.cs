using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.DTOs.Legal;

namespace Qalam.Core.Features.Legal.Queries.GetPublishedLegalDocument;

public class GetPublishedLegalDocumentQuery : IRequest<Response<PublicLegalDocumentDto>>
{
    public string Code { get; set; } = null!;
}
