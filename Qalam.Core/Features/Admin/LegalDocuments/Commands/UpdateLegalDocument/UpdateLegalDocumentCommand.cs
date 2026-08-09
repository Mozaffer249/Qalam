using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;
using Qalam.Data.DTOs.Legal;

namespace Qalam.Core.Features.Admin.LegalDocuments.Commands.UpdateLegalDocument;

public class UpdateLegalDocumentCommand : IRequest<Response<LegalDocumentListItemDto>>, IAuthenticatedRequest
{
    public int Id { get; set; }
    public UpdateLegalDocumentDto Data { get; set; } = null!;

    [BindNever]
    public int UserId { get; set; }
}
