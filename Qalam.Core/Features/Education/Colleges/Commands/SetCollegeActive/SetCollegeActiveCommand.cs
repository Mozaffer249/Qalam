using MediatR;
using Qalam.Core.Bases;

namespace Qalam.Core.Features.Education.Colleges.Commands.SetCollegeActive;

public class SetCollegeActiveCommand : IRequest<Response<bool>>
{
    public int Id { get; set; }
    public bool IsActive { get; set; } = true;
}
