using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.DTOs.Legal;

namespace Qalam.Core.Features.Admin.LegalDocuments.Queries.GetLegalDocumentVersionDetail;

public class GetLegalDocumentVersionDetailQuery : IRequest<Response<LegalDocumentVersionDetailDto>>
{
    public int VersionId { get; set; }
}
