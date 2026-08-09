using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;
using Qalam.Data.DTOs.Legal;

namespace Qalam.Core.Features.Admin.LegalDocuments.Commands.UnpublishLegalDocumentVersion;

public class UnpublishLegalDocumentVersionCommand : IRequest<Response<LegalDocumentVersionSummaryDto>>, IAuthenticatedRequest
{
    public int VersionId { get; set; }

    [BindNever]
    public int UserId { get; set; }
}
