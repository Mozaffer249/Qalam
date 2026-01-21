using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;
using Qalam.Data.DTOs.Teacher;
using Qalam.Data.Entity.Common.Enums;

namespace Qalam.Core.Features.Teacher.Commands.UploadTeacherDocuments;

public class UploadTeacherDocumentsCommand : IRequest<Response<string>>, IAuthenticatedRequest
{
    /// <summary>
    /// Automatically populated by UserIdentityBehavior from JWT token.
    /// Should NOT be sent by client - will be ignored if provided.
    /// </summary>
    [BindNever]
    public int UserId { get; set; }

    public bool IsInSaudiArabia { get; set; }

    // Identity document properties (flattened)
    public IdentityType IdentityType { get; set; }
    public string DocumentNumber { get; set; } = null!;
    public string? IssuingCountryCode { get; set; }
    public IFormFile IdentityDocumentFile { get; set; } = null!;

    // Certificates (keep as DTO list)
    public List<CertificateUploadDto> Certificates { get; set; } = new();
}
