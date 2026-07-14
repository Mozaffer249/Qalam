using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;
using Qalam.Data.DTOs.Teacher;

namespace Qalam.Core.Features.Teacher.Content.Queries.ListContentFolders;

public class ListContentFoldersQuery : IRequest<Response<List<TeacherContentFolderDto>>>, IAuthenticatedRequest
{
    public int? ParentId { get; set; }

    [BindNever]
    public int UserId { get; set; }
}
