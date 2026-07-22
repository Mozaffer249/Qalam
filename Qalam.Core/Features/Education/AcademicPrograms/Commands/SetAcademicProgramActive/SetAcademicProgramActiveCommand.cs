using MediatR;
using Qalam.Core.Bases;

namespace Qalam.Core.Features.Education.AcademicPrograms.Commands.SetAcademicProgramActive;

public class SetAcademicProgramActiveCommand : IRequest<Response<bool>>
{
    public int Id { get; set; }
    public bool IsActive { get; set; } = true;
}
