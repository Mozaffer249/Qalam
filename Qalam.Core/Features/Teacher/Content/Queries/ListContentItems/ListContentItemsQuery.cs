using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;
using Qalam.Data.DTOs.Teacher;
using Qalam.Data.Entity.Common.Enums;

namespace Qalam.Core.Features.Teacher.Content.Queries.ListContentItems;

public class ListContentItemsQuery : IRequest<Response<List<TeacherContentItemDto>>>, IAuthenticatedRequest
{
    public int? FolderId { get; set; }
    public TeacherContentItemKind? Kind { get; set; }
    public string? Search { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 50;

    [BindNever]
    public int UserId { get; set; }
}
