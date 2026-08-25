using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.DTOs.Admin;

namespace Qalam.Core.Features.Admin.Students.Queries.GetAdminStudentsList;

/// <summary>Paginated admin browse of students. Filters are optional and AND-combined.</summary>
public class GetAdminStudentsListQuery : IRequest<Response<List<AdminStudentListItemDto>>>
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? Search { get; set; }
    public bool? IsMinor { get; set; }
    public bool? IsActive { get; set; }
}
