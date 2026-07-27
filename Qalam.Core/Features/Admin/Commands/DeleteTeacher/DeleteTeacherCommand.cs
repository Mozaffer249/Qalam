using System.ComponentModel.DataAnnotations;
using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;

namespace Qalam.Core.Features.Admin.Commands.DeleteTeacher;

public class DeleteTeacherCommand : IRequest<Response<string>>, IAuthenticatedRequest
{
    [BindNever]
    public int UserId { get; set; }

    public int TeacherId { get; set; }

    [MaxLength(500)]
    public string? Reason { get; set; }
}
