namespace Qalam.Data.DTOs;

public class SubjectDomainInfo
{
    public int SubjectId { get; set; }
    public int DomainId { get; set; }
    public string DomainCode { get; set; } = null!;
    public string DomainNameEn { get; set; } = null!;
    public string DomainNameAr { get; set; } = null!;
}
