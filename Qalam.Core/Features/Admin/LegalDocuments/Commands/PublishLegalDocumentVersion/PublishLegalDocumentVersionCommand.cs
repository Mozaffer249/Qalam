using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;
using Qalam.Data.DTOs.Legal;

namespace Qalam.Core.Features.Admin.LegalDocuments.Commands.PublishLegalDocumentVersion;

public class PublishLegalDocumentVersionCommand : IRequest<Response<LegalDocumentVersionSummaryDto>>, IAuthenticatedRequest
{
    public int VersionId { get; set; }
    public PublishLegalDocumentVersionDto Data { get; set; } = new();

    [BindNever]
    public int UserId { get; set; }
}
