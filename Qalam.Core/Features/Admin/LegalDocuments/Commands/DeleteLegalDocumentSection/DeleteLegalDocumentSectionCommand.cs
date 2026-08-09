using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;

namespace Qalam.Core.Features.Admin.LegalDocuments.Commands.DeleteLegalDocumentSection;

public class DeleteLegalDocumentSectionCommand : IRequest<Response<string>>, IAuthenticatedRequest
{
    public int SectionId { get; set; }

    [BindNever]
    public int UserId { get; set; }
}
