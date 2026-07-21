namespace Qalam.Data.DTOs.Teacher;

public class TeacherDocumentsUploadDto
{
    public string? NationalityCode { get; set; }
    public IdentityDocumentUploadDto IdentityDocument { get; set; } = null!;
    public List<CertificateUploadDto> Certificates { get; set; } = new();
}
