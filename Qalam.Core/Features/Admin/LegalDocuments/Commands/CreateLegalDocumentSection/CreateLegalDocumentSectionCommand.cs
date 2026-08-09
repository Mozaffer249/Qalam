using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;
using Qalam.Data.DTOs.Legal;

namespace Qalam.Core.Features.Admin.LegalDocuments.Commands.CreateLegalDocumentSection;

public class CreateLegalDocumentSectionCommand : IRequest<Response<LegalDocumentSectionDto>>, IAuthenticatedRequest
{
    public int VersionId { get; set; }
    public CreateLegalDocumentSectionDto Data { get; set; } = null!;

    [BindNever]
    public int UserId { get; set; }
}
