using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.DTOs.Legal;

namespace Qalam.Core.Features.Admin.LegalDocuments.Queries.ListLegalDocuments;

public class ListLegalDocumentsQuery : IRequest<Response<List<LegalDocumentListItemDto>>>
{
}
