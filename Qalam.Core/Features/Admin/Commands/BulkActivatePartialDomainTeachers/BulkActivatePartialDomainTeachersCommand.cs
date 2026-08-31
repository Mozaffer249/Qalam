using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;
using Qalam.Data.DTOs.Admin;

namespace Qalam.Core.Features.Admin.Commands.BulkActivatePartialDomainTeachers;

public class BulkActivatePartialDomainTeachersCommand : IRequest<Response<BulkActivatePartialDomainTeachersResultDto>>, IAuthenticatedRequest
{
    [BindNever]
    public int UserId { get; set; }

    public List<int> TeacherIds { get; set; } = new();
}
