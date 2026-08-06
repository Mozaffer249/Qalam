using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;
using Qalam.Data.DTOs.Teacher;

namespace Qalam.Core.Features.Teacher.Commands.UpdateTeacherSubjectBySubject;

/// <summary>
/// Update units / CanTeachFullSubject / Quran coverage keyed by catalog SubjectId.
/// </summary>
public class UpdateTeacherSubjectBySubjectCommand : IRequest<Response<TeacherSubjectResponseDto>>, IAuthenticatedRequest
{
    [BindNever]
    public int UserId { get; set; }

    public int SubjectId { get; set; }

    public bool CanTeachFullSubject { get; set; }

    public List<TeacherSubjectUnitItemDto> Units { get; set; } = new();

    public List<int> QuranContentTypeIds { get; set; } = new();

    public List<int> QuranLevelIds { get; set; } = new();
}
