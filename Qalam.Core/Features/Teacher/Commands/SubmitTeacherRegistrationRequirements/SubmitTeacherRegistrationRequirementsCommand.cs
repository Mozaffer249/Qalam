using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;
using Qalam.Data.DTOs.Teacher;
using Qalam.Data.Entity.Common.Enums;

namespace Qalam.Core.Features.Teacher.Commands.SubmitTeacherRegistrationRequirements;

public class SubmitTeacherRegistrationRequirementsCommand : IRequest<Response<string>>, IAuthenticatedRequest
{
    [BindNever]
    public int UserId { get; set; }

    public bool IsInSaudiArabia { get; set; }
    public string? Bio { get; set; }

    public IdentityType IdentityType { get; set; }
    public string DocumentNumber { get; set; } = null!;
    public string? IssuingCountryCode { get; set; }
    public IFormFile? IdentityDocumentFile { get; set; }
    public List<CertificateUploadDto> Certificates { get; set; } = new();

    /// <summary>Populated by controller from form files named file_{code}.</summary>
    [BindNever]
    public Dictionary<string, List<IFormFile>> CustomFilesByCode { get; set; } = new();
}
