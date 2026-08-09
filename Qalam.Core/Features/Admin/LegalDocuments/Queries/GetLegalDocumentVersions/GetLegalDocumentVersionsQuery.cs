using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.DTOs.Legal;

namespace Qalam.Core.Features.Admin.LegalDocuments.Queries.GetLegalDocumentVersions;

public class GetLegalDocumentVersionsQuery : IRequest<Response<List<LegalDocumentVersionSummaryDto>>>
{
    public int DocumentId { get; set; }
}
