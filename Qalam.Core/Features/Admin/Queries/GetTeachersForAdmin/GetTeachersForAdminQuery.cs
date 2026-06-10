using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.DTOs.Admin;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Admin.Queries.GetTeachersForAdmin;

/// <summary>
/// Paginated admin browse of all teachers. Filters are optional and AND-combined.
/// </summary>
public class GetTeachersForAdminQuery : IRequest<Response<List<AdminTeacherListItemDto>>>
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? Status { get; set; }
    public TeacherLocation? Location { get; set; }
    public int? SubjectId { get; set; }
    public string? Search { get; set; }
    public AdminTeacherListSort SortBy { get; set; } = AdminTeacherListSort.Newest;
}
