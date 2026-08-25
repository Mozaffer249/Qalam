using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;
using Qalam.Service.Helpers;
using Microsoft.EntityFrameworkCore;

namespace Qalam.Core.Features.Student.Enrollments.Commands.CancelEnrollment;

public class CancelEnrollmentCommandHandler : ResponseHandler,
    IRequestHandler<CancelEnrollmentCommand, Response<string>>
{
    private readonly IEnrollmentRepository _enrollmentRepository;
    private readonly IEnrollmentCancellationService _cancellationService;

    public CancelEnrollmentCommandHandler(
        IEnrollmentRepository enrollmentRepository,
        IEnrollmentCancellationService cancellationService,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _enrollmentRepository = enrollmentRepository;
        _cancellationService = cancellationService;
    }

    public async Task<Response<string>> Handle(
        CancelEnrollmentCommand request,
        CancellationToken cancellationToken)
    {
        var enrollment = await _enrollmentRepository.GetTableNoTracking()
            .Include(e => e.EnrollmentRequest)
            .Include(e => e.CourseSchedules)
                .ThenInclude(cs => cs.Attendances)
            .FirstOrDefaultAsync(e => e.Id == request.EnrollmentId, cancellationToken);

        if (enrollment == null)
            return NotFound<string>("Enrollment not found.");

        var ownerUserId = enrollment.OwnerUserId
                          ?? enrollment.EnrollmentRequest?.RequestedByUserId;
        if (!ownerUserId.HasValue || ownerUserId.Value != request.UserId)
            return BadRequest<string>("Only the enrollment owner can cancel this enrollment.");

        if (!EnrollmentLifecycleRules.CanStudentCancel(enrollment, isOwner: true))
        {
            if (EnrollmentLifecycleRules.HasSessionStarted(enrollment))
                return BadRequest<string>("Cannot cancel after the first session has started.");
            return BadRequest<string>(
                "Only pending-payment or active (before first session) enrollments can be cancelled.");
        }

        try
        {
            await _cancellationService.CancelAsync(
                request.EnrollmentId,
                request.UserId,
                reason: "Student cancelled enrollment",
                cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest<string>(ex.Message);
        }

        return Success<string>(entity: "Enrollment cancelled.");
    }
}
