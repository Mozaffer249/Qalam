using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.DTOs;

namespace Qalam.Core.Features.Education.Departments.Commands.UpdateDepartment;

public class UpdateDepartmentCommand : IRequest<Response<DepartmentDto>>
{
    public int Id { get; set; }
    public int CollegeId { get; set; }
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string? Code { get; set; }
    public bool IsActive { get; set; } = true;
}
