using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.DTOs;

namespace Qalam.Core.Features.Education.Departments.Queries.GetDepartmentById;

public class GetDepartmentByIdQuery : IRequest<Response<DepartmentDto>>
{
    public int Id { get; set; }
}
