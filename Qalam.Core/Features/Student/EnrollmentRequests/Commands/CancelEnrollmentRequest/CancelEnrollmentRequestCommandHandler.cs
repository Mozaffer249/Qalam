using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Student.EnrollmentRequests.Commands.CancelEnrollmentRequest;

public class CancelEnrollmentRequestCommandHandler : ResponseHandler,
    IRequestHandler<CancelEnrollmentRequestCommand, Response<string>>
{
    private readonly ICourseEnrollmentRequestRepository _requestRepository;
    private readonly IEnrollmentRepository _enrollmentRepository;

    public CancelEnrollmentRequestCommandHandler(
        ICourseEnrollmentRequestRepository requestRepository,
        IEnrollmentRepository enrollmentRepository,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _requestRepository = requestRepository;
        _enrollmentRepository = enrollmentRepository;
    }

    public async Task<Response<string>> Handle(
        CancelEnrollmentRequestCommand request,
        CancellationToken cancellationToken)
    {
        var enrollmentRequest = await _requestRepository.GetTableAsTracking()
            .Include(r => r.GroupMembers)
            .FirstOrDefaultAsync(r => r.Id == request.EnrollmentRequestId, cancellationToken);

        if (enrollmentRequest == null)
            return NotFound<string>("Enrollment request not found.");

        if (enrollmentRequest.RequestedByUserId != request.UserId)
            return BadRequest<string>("Only the request owner can cancel this request.");

        if (enrollmentRequest.Status is not (RequestStatus.Pending or RequestStatus.Approved))
            return BadRequest<string>("This enrollment request cannot be cancelled.");

        var enrollment = await _enrollmentRepository.GetTableAsTracking()
            .Include(e => e.Participants)
            .FirstOrDefaultAsync(e => e.EnrollmentRequestId == enrollmentRequest.Id, cancellationToken);

        if (enrollment != null)
        {
            if (enrollment.EnrollmentStatus != EnrollmentStatus.PendingPayment)
                return BadRequest<string>(
                    "Cannot cancel a request after the enrollment has been paid or completed.");

            enrollment.EnrollmentStatus = EnrollmentStatus.Cancelled;
            foreach (var participant in enrollment.Participants)
            {
                if (participant.PaymentStatus == PaymentStatus.Pending)
                    participant.PaymentStatus = PaymentStatus.Cancelled;
            }
        }

        foreach (var member in enrollmentRequest.GroupMembers)
        {
            if (member.MemberType == GroupMemberType.Invited
                && member.ConfirmationStatus == GroupMemberConfirmationStatus.Pending)
            {
                member.ConfirmationStatus = GroupMemberConfirmationStatus.Cancelled;
                member.ConfirmedAt = DateTime.UtcNow;
                member.ConfirmedByUserId = request.UserId;
            }
        }

        enrollmentRequest.Status = RequestStatus.Cancelled;
        await _requestRepository.SaveChangesAsync();
        return Success<string>(entity: "Enrollment request cancelled.");
    }
}
