using Microsoft.AspNetCore.Http;

namespace Qalam.Data.DTOs.Teacher;

public class CertificateUploadDto
{
    public IFormFile File { get; set; } = null!;
    public string? Title { get; set; }
    public string? Issuer { get; set; }
    public DateOnly? IssueDate { get; set; }
}
