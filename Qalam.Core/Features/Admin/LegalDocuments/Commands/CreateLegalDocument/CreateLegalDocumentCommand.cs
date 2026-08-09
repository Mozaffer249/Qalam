using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;
using Qalam.Data.DTOs.Legal;

namespace Qalam.Core.Features.Admin.LegalDocuments.Commands.CreateLegalDocument;

public class CreateLegalDocumentCommand : IRequest<Response<LegalDocumentListItemDto>>, IAuthenticatedRequest
{
    public CreateLegalDocumentDto Data { get; set; } = null!;

    [BindNever]
    public int UserId { get; set; }
}
