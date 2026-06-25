using Microsoft.AspNetCore.Http;

namespace Qalam.Data.DTOs.Teacher;

public class TeacherDomainQuestionSubmissionInput
{
    public int DomainId { get; set; }
    public Dictionary<string, List<IFormFile>> CustomFilesByCode { get; set; } = new();
    public Dictionary<string, string?> TextValuesByCode { get; set; } = new();
    public Dictionary<string, bool?> BoolValuesByCode { get; set; } = new();
    public Dictionary<string, List<string>> SelectionsByCode { get; set; } = new();
}
