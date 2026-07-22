using MediatR;
using Qalam.Core.Bases;

namespace Qalam.Core.Features.Education.Universities.Commands.SetUniversityActive;

public class SetUniversityActiveCommand : IRequest<Response<bool>>
{
    public int Id { get; set; }
    public bool IsActive { get; set; } = true;
}
