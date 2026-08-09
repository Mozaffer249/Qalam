using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;
using Qalam.Data.DTOs.Legal;

namespace Qalam.Core.Features.Admin.LegalDocuments.Commands.UpdateLegalDocumentVersion;

public class UpdateLegalDocumentVersionCommand : IRequest<Response<LegalDocumentVersionSummaryDto>>, IAuthenticatedRequest
{
    public int VersionId { get; set; }
    public UpdateLegalDocumentVersionDto Data { get; set; } = null!;

    [BindNever]
    public int UserId { get; set; }
}
