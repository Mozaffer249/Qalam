using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;
using Qalam.Data.DTOs.Legal;

namespace Qalam.Core.Features.Admin.LegalDocuments.Commands.CreateLegalDocumentVersion;

public class CreateLegalDocumentVersionCommand : IRequest<Response<LegalDocumentVersionDetailDto>>, IAuthenticatedRequest
{
    public int DocumentId { get; set; }
    public CreateLegalDocumentVersionDto Data { get; set; } = null!;

    [BindNever]
    public int UserId { get; set; }
}
