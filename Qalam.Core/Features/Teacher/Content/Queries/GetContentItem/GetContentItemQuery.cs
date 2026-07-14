using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;
using Qalam.Data.DTOs.Teacher;

namespace Qalam.Core.Features.Teacher.Content.Queries.GetContentItem;

public class GetContentItemQuery : IRequest<Response<TeacherContentItemDto>>, IAuthenticatedRequest
{
    public int Id { get; set; }

    [BindNever]
    public int UserId { get; set; }
}
