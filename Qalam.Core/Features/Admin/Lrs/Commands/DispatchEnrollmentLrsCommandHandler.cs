using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Admin.Lrs.Commands;

public class DispatchEnrollmentLrsCommandHandler : ResponseHandler,
    IRequestHandler<DispatchEnrollmentLrsCommand, Response<string>>
{
    private readonly ILrsDispatchService _lrsDispatch;

    public DispatchEnrollmentLrsCommandHandler(
        ILrsDispatchService lrsDispatch,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _lrsDispatch = lrsDispatch;
    }

    public async Task<Response<string>> Handle(
        DispatchEnrollmentLrsCommand request, CancellationToken cancellationToken)
    {
        var count = await _lrsDispatch.ReplayFullJourneyAsync(request.EnrollmentId, cancellationToken);

        if (count == 0)
            return NotFound<string>("Enrollment not found or has no course/participants.");

        return Success(entity: $"{count} LRS statements queued for enrollment {request.EnrollmentId}.");
    }
}
