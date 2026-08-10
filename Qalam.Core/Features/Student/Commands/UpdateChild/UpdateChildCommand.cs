using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;
using Qalam.Data.DTOs.Student;

namespace Qalam.Core.Features.Student.Commands.UpdateChild;

/// <summary>
/// Guardian updates a child's profile (name, DOB, gender, relation, academic ids).
/// </summary>
public class UpdateChildCommand : IRequest<Response<ChildStudentDto>>, IAuthenticatedRequest
{
    [BindNever]
    public int UserId { get; set; }

    public int StudentId { get; set; }
    public UpdateChildDto Child { get; set; } = null!;
}
