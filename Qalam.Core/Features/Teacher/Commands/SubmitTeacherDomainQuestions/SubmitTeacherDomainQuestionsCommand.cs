using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;
using Qalam.Data.DTOs.Teacher;

namespace Qalam.Core.Features.Teacher.Commands.SubmitTeacherDomainQuestions;

public class SubmitTeacherDomainQuestionsCommand : IRequest<Response<TeacherDomainQuestionSubmitResponseDto>>, IAuthenticatedRequest
{
    [BindNever]
    public int UserId { get; set; }

    public int DomainId { get; set; }

    public List<TeacherDomainQuestionAnswerItem> Answers { get; set; } = new();

    /// <summary>Populated from legacy multipart prefixes when <see cref="Answers"/> is empty.</summary>
    [BindNever]
    public Dictionary<string, List<IFormFile>> CustomFilesByCode { get; set; } = new();

    [BindNever]
    public Dictionary<string, string?> TextValuesByCode { get; set; } = new();

    [BindNever]
    public Dictionary<string, bool?> BoolValuesByCode { get; set; } = new();

    [BindNever]
    public Dictionary<string, List<string>> SelectionsByCode { get; set; } = new();
}

public class TeacherDomainQuestionAnswerItem
{
    public string Code { get; set; } = null!;
    public string? TextValue { get; set; }
    public bool? BoolValue { get; set; }
    public List<string> SelectedValues { get; set; } = new();
    public List<IFormFile> Files { get; set; } = new();
}
