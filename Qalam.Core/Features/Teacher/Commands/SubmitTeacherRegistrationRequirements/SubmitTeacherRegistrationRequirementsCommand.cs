using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;
using Qalam.Data.DTOs.Teacher;
using Qalam.Data.Entity.Common.Enums;

namespace Qalam.Core.Features.Teacher.Commands.SubmitTeacherRegistrationRequirements;

public class SubmitTeacherRegistrationRequirementsCommand : IRequest<Response<TeacherRegistrationSubmitResponseDto>>, IAuthenticatedRequest
{
    [BindNever]
    public int UserId { get; set; }

    // Nullable so the handler can tell "field absent" when nationality is required.
    public string? NationalityCode { get; set; }
    public string? Bio { get; set; }

    public IdentityType IdentityType { get; set; }
    public string DocumentNumber { get; set; } = null!;
    /// <summary>
    /// Optional on the wire — when omitted for foreign IDs, the handler sets this to <see cref="NationalityCode"/>.
    /// </summary>
    public string? IssuingCountryCode { get; set; }
    public IFormFile? IdentityDocumentFile { get; set; }
    public List<CertificateUploadDto> Certificates { get; set; } = new();

    /// <summary>Populated by controller from form files named file_{code}.</summary>
    [BindNever]
    public Dictionary<string, List<IFormFile>> CustomFilesByCode { get; set; } = new();

    /// <summary>Populated by controller from form fields named text_{code}.</summary>
    [BindNever]
    public Dictionary<string, string?> TextValuesByCode { get; set; } = new();

    /// <summary>Populated by controller from form fields named bool_{code}.</summary>
    [BindNever]
    public Dictionary<string, bool?> BoolValuesByCode { get; set; } = new();

    /// <summary>Populated by controller from form fields named select_{code} (repeatable for multi-select).</summary>
    [BindNever]
    public Dictionary<string, List<string>> SelectionsByCode { get; set; } = new();
}
