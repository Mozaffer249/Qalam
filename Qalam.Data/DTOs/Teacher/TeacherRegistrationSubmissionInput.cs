using Microsoft.AspNetCore.Http;
using Qalam.Data.Entity.Common.Enums;

namespace Qalam.Data.DTOs.Teacher;

/// <summary>
/// Service-layer input for <c>ITeacherRegistrationSubmitService.SubmitAsync</c>.
/// Mirrors the multipart fields the FE sends — the handler maps its
/// <c>SubmitTeacherRegistrationRequirementsCommand</c> onto this DTO before delegating,
/// so the service depends only on Qalam.Data (no Qalam.Core reference).
/// </summary>
public class TeacherRegistrationSubmissionInput
{
    public string? NationalityCode { get; set; }
    public string? Bio { get; set; }
    public IdentityType IdentityType { get; set; }
    public string DocumentNumber { get; set; } = null!;
    public string? IssuingCountryCode { get; set; }
    public IFormFile? IdentityDocumentFile { get; set; }
    public List<CertificateUploadDto> Certificates { get; set; } = new();
    public Dictionary<string, List<IFormFile>> CustomFilesByCode { get; set; } = new();
    public Dictionary<string, string?> TextValuesByCode { get; set; } = new();
    public Dictionary<string, bool?> BoolValuesByCode { get; set; } = new();
    public Dictionary<string, List<string>> SelectionsByCode { get; set; } = new();
}
