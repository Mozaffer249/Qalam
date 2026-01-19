using Microsoft.AspNetCore.Http;
using Qalam.Data.Entity.Common.Enums;

namespace Qalam.Data.DTOs.Teacher;

public class IdentityDocumentUploadDto
{
    public IdentityType IdentityType { get; set; }
    public string DocumentNumber { get; set; } = null!;
    public string? IssuingCountryCode { get; set; }  // Required for Passport
    public IFormFile File { get; set; } = null!;
}
