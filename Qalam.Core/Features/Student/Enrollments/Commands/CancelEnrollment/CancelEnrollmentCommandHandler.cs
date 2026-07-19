using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Student.Enrollments.Commands.CancelEnrollment;

public class CancelEnrollmentCommandHandler : ResponseHandler,
    IRequestHandler<CancelEnrollmentCommand, Response<string>>
{
    private readonly IEnrollmentRepository _enrollmentRepository;
    private readonly ICourseEnrollmentRequestRepository _requestRepository;

    public CancelEnrollmentCommandHandler(
        IEnrollmentRepository enrollmentRepository,
        ICourseEnrollmentRequestRepository requestRepository,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _enrollmentRepository = enrollmentRepository;
        _requestRepository = requestRepository;
    }

    public async Task<Response<string>> Handle(
        CancelEnrollmentCommand request,
        CancellationToken cancellationToken)
    {
        var enrollment = await _enrollmentRepository.GetTableAsTracking()
            .Include(e => e.Participants)
            .Include(e => e.EnrollmentRequest)
            .FirstOrDefaultAsync(e => e.Id == request.EnrollmentId, cancellationToken);

        if (enrollment == null)
            return NotFound<string>("Enrollment not found.");

        var ownerUserId = enrollment.OwnerUserId
                          ?? enrollment.EnrollmentRequest?.RequestedByUserId;
        if (!ownerUserId.HasValue || ownerUserId.Value != request.UserId)
            return BadRequest<string>("Only the enrollment owner can cancel this enrollment.");

        if (enrollment.EnrollmentStatus != EnrollmentStatus.PendingPayment)
            return BadRequest<string>("Only pending-payment enrollments can be cancelled.");

        enrollment.EnrollmentStatus = EnrollmentStatus.Cancelled;
        foreach (var participant in enrollment.Participants)
        {
            if (participant.PaymentStatus == PaymentStatus.Pending)
                participant.PaymentStatus = PaymentStatus.Cancelled;
        }

        if (enrollment.EnrollmentRequestId.HasValue)
        {
            var enrollmentRequest = enrollment.EnrollmentRequest
                ?? await _requestRepository.GetTableAsTracking()
                    .FirstOrDefaultAsync(
                        r => r.Id == enrollment.EnrollmentRequestId.Value,
                        cancellationToken);

            if (enrollmentRequest != null
                && enrollmentRequest.Status is RequestStatus.Pending or RequestStatus.Approved)
            {
                enrollmentRequest.Status = RequestStatus.Cancelled;
            }
        }

        await _enrollmentRepository.SaveChangesAsync();
        return Success<string>(entity: "Enrollment cancelled.");
    }
}
