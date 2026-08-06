using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;
using Qalam.Data.DTOs.Teacher;

namespace Qalam.Core.Features.Teacher.Commands.UpdateTeacherSubject;

/// <summary>
/// Command to update units / CanTeachFullSubject / Quran coverage for an owned teacher subject.
/// </summary>
public class UpdateTeacherSubjectCommand : IRequest<Response<TeacherSubjectResponseDto>>, IAuthenticatedRequest
{
    /// <summary>
    /// Automatically populated by UserIdentityBehavior from JWT token.
    /// </summary>
    [BindNever]
    public int UserId { get; set; }

    /// <summary>
    /// TeacherSubject ID to update.
    /// </summary>
    public int Id { get; set; }

    public bool CanTeachFullSubject { get; set; }

    public List<TeacherSubjectUnitItemDto> Units { get; set; } = new();

    public List<int> QuranContentTypeIds { get; set; } = new();

    public List<int> QuranLevelIds { get; set; } = new();
}
