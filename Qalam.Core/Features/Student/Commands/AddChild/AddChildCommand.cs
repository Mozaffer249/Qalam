using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;
using Qalam.Data.DTOs.Student;

namespace Qalam.Core.Features.Student.Commands.AddChild;

/// <summary>
/// Parent adds a child (Student record linked to Guardian)
/// </summary>
public class AddChildCommand : IRequest<Response<int>>, IAuthenticatedRequest
{
    [BindNever]
    public int UserId { get; set; }
    public AddChildDto Child { get; set; } = null!;
}
