using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.DTOs.Legal;

namespace Qalam.Core.Features.Legal.Queries.ListPublishedLegalDocuments;

public class ListPublishedLegalDocumentsQuery : IRequest<Response<List<PublicLegalDocumentSummaryDto>>>
{
}
