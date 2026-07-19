using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Helpers;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Student.EnrollmentRequests.Commands.CancelGroupEnrollmentInvite;

public class CancelGroupEnrollmentInviteCommandHandler : ResponseHandler,
    IRequestHandler<CancelGroupEnrollmentInviteCommand, Response<string>>
{
    private readonly ICourseEnrollmentRequestRepository _requestRepository;
    private readonly IEnrollmentRepository _enrollmentRepository;
    private readonly IEnrollmentApprovalService _approvalService;
    private readonly EnrollmentSettings _settings;

    public CancelGroupEnrollmentInviteCommandHandler(
        ICourseEnrollmentRequestRepository requestRepository,
        IEnrollmentRepository enrollmentRepository,
        IEnrollmentApprovalService approvalService,
        IOptions<EnrollmentSettings> settings,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _requestRepository = requestRepository;
        _enrollmentRepository = enrollmentRepository;
        _approvalService = approvalService;
        _settings = settings.Value;
    }

    public async Task<Response<string>> Handle(
        CancelGroupEnrollmentInviteCommand request,
        CancellationToken cancellationToken)
    {
        var enrollmentRequest = await _requestRepository.GetTableAsTracking()
            .Include(r => r.GroupMembers)
            .Include(r => r.Course).ThenInclude(c => c.SessionType)
            .FirstOrDefaultAsync(r => r.Id == request.EnrollmentRequestId, cancellationToken);

        if (enrollmentRequest == null)
            return NotFound<string>("Enrollment request not found.");

        if (enrollmentRequest.RequestedByUserId != request.UserId)
            return BadRequest<string>("Only the request owner can cancel invitations.");

        if (enrollmentRequest.Status is RequestStatus.Rejected or RequestStatus.Cancelled)
            return BadRequest<string>("Cannot cancel invites on a closed request.");

        var alreadyHasEnrollment = await _enrollmentRepository.GetTableNoTracking()
            .AnyAsync(e => e.EnrollmentRequestId == enrollmentRequest.Id, cancellationToken);
        if (alreadyHasEnrollment)
            return BadRequest<string>("Cannot cancel invites after enrollment has been created.");

        var groupMember = enrollmentRequest.GroupMembers.FirstOrDefault(m => m.StudentId == request.StudentId);
        if (groupMember == null)
            return NotFound<string>("Group member invitation not found.");

        if (groupMember.MemberType != GroupMemberType.Invited)
            return BadRequest<string>("Only invited members can be cancelled.");

        if (groupMember.ConfirmationStatus != GroupMemberConfirmationStatus.Pending)
            return BadRequest<string>("This invitation has already been handled.");

        groupMember.ConfirmationStatus = GroupMemberConfirmationStatus.Cancelled;
        groupMember.ConfirmedAt = DateTime.UtcNow;
        groupMember.ConfirmedByUserId = request.UserId;

        await _requestRepository.SaveChangesAsync();

        // Fixed: when last pending invite cleared, finalize enrollment for Confirmed members.
        if (!enrollmentRequest.Course.IsFlexible
            && enrollmentRequest.Status == RequestStatus.Approved)
        {
            var stillPendingInvitees = enrollmentRequest.GroupMembers.Any(
                gm => gm.MemberType == GroupMemberType.Invited
                   && gm.ConfirmationStatus == GroupMemberConfirmationStatus.Pending);

            if (!stillPendingInvitees)
            {
                var hasAnyConfirmedMember = enrollmentRequest.GroupMembers.Any(
                    gm => gm.ConfirmationStatus == GroupMemberConfirmationStatus.Confirmed);

                if (hasAnyConfirmedMember)
                {
                    var paymentDeadline = DateTime.UtcNow.AddHours(_settings.PaymentDeadlineHours);
                    await _approvalService.CreatePendingPaymentArtifactsAsync(
                        enrollmentRequest,
                        enrollmentRequest.Course,
                        enrollmentRequest.Course.TeacherId,
                        paymentDeadline,
                        cancellationToken);
                }
            }
        }

        return Success<string>(entity: "Invitation cancelled.");
    }
}
