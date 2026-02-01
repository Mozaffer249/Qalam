using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;
using Qalam.Data.DTOs.Teacher;

namespace Qalam.Core.Features.Teacher.Commands.SaveTeacherSubjects;

/// <summary>
/// Command to save teacher subjects with units (batch operation)
/// </summary>
public class SaveTeacherSubjectsCommand : IRequest<Response<TeacherSubjectsResponseDto>>, IAuthenticatedRequest
{
    /// <summary>
    /// Automatically populated by UserIdentityBehavior from JWT token.
    /// Should NOT be sent by client - will be ignored if provided.
    /// </summary>
    [BindNever]
    public int UserId { get; set; }
    
    /// <summary>
    /// List of subjects with their units
    /// </summary>
    public List<TeacherSubjectItemDto> Subjects { get; set; } = new();
}
