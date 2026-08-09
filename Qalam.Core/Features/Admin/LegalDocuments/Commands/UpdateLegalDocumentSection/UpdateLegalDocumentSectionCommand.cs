using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;
using Qalam.Data.DTOs.Legal;

namespace Qalam.Core.Features.Admin.LegalDocuments.Commands.UpdateLegalDocumentSection;

public class UpdateLegalDocumentSectionCommand : IRequest<Response<LegalDocumentSectionDto>>, IAuthenticatedRequest
{
    public int SectionId { get; set; }
    public UpdateLegalDocumentSectionDto Data { get; set; } = null!;

    [BindNever]
    public int UserId { get; set; }
}
