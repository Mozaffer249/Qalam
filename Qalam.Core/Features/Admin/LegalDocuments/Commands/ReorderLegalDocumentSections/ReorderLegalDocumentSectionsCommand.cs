using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;
using Qalam.Data.DTOs.Legal;

namespace Qalam.Core.Features.Admin.LegalDocuments.Commands.ReorderLegalDocumentSections;

public class ReorderLegalDocumentSectionsCommand : IRequest<Response<string>>, IAuthenticatedRequest
{
    public int VersionId { get; set; }
    public ReorderLegalDocumentSectionsDto Data { get; set; } = null!;

    [BindNever]
    public int UserId { get; set; }
}
