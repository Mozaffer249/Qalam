using MediatR;
using Qalam.Core.Bases;

namespace Qalam.Core.Features.Education.Departments.Commands.SetDepartmentActive;

public class SetDepartmentActiveCommand : IRequest<Response<bool>>
{
    public int Id { get; set; }
    public bool IsActive { get; set; } = true;
}
