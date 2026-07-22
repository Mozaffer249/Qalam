using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.DTOs;

namespace Qalam.Core.Features.Education.Departments.Queries.GetDepartmentsList;

public class GetDepartmentsListQuery : IRequest<Response<List<DepartmentDto>>>
{
    public int? CollegeId { get; set; }
    public bool ActiveOnly { get; set; }
}
