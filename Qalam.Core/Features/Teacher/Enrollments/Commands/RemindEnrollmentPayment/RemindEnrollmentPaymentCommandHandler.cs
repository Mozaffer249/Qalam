using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Teacher.Enrollments.Commands.RemindEnrollmentPayment;

public class RemindEnrollmentPaymentCommandHandler : ResponseHandler,
    IRequestHandler<RemindEnrollmentPaymentCommand, Response<string>>
{
    private readonly ITeacherEnrollmentService _enrollmentService;

    public RemindEnrollmentPaymentCommandHandler(
        ITeacherEnrollmentService enrollmentService,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _enrollmentService = enrollmentService;
    }

    public async Task<Response<string>> Handle(
        RemindEnrollmentPaymentCommand request,
        CancellationToken cancellationToken)
    {
        var (ok, message, forbidden) = await _enrollmentService.RemindPaymentAsync(
            request.UserId, request.Id, cancellationToken);

        if (forbidden)
            return Forbidden<string>(message);

        if (!ok)
        {
            if (message.Contains("not found", StringComparison.OrdinalIgnoreCase))
                return NotFound<string>(message);
            return BadRequest<string>(message);
        }

        return Success(entity: message);
    }
}
