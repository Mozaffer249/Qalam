using MediatR;
using Qalam.Core.Bases;

namespace Qalam.Core.Features.Admin.Lrs.Commands;

public class DispatchEnrollmentLrsCommand : IRequest<Response<string>>
{
    public int EnrollmentId { get; set; }
}
