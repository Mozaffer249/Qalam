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

namespace Qalam.Core.Features.Student.EnrollmentRequests.Commands.RespondToGroupEnrollmentInvite;

public class RespondToGroupEnrollmentInviteCommandHandler : ResponseHandler,
    IRequestHandler<RespondToGroupEnrollmentInviteCommand, Response<string>>
{
    private readonly ICourseEnrollmentRequestRepository _requestRepository;
    private readonly IStudentRepository _studentRepository;
    private readonly IGuardianRepository _guardianRepository;
    private readonly IEnrollmentRepository _enrollmentRepository;
    private readonly IEnrollmentApprovalService _approvalService;
    private readonly EnrollmentSettings _settings;

    public RespondToGroupEnrollmentInviteCommandHandler(
        ICourseEnrollmentRequestRepository requestRepository,
        IStudentRepository studentRepository,
        IGuardianRepository guardianRepository,
        IEnrollmentRepository enrollmentRepository,
        IEnrollmentApprovalService approvalService,
        IOptions<EnrollmentSettings> settings,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _requestRepository = requestRepository;
        _studentRepository = studentRepository;
        _guardianRepository = guardianRepository;
        _enrollmentRepository = enrollmentRepository;
        _approvalService = approvalService;
        _settings = settings.Value;
    }

    public async Task<Response<string>> Handle(
        RespondToGroupEnrollmentInviteCommand request,
        CancellationToken cancellationToken)
    {
        var enrollmentRequest = await _requestRepository.GetTableAsTracking()
            .Include(r => r.GroupMembers)
            .Include(r => r.Course).ThenInclude(c => c.SessionType)
            .FirstOrDefaultAsync(r => r.Id == request.EnrollmentRequestId, cancellationToken);
        if (enrollmentRequest == null)
            return NotFound<string>("Enrollment request not found.");

        // Pending = flexible course waiting for teacher approval; invitees can still respond.
        // Approved + fixed = auto-approved at submit; invitees still confirm to finalize the group.
        // Approved + flexible = teacher already approved; group enrollment exists; further responses are noise.
        var canRespond = enrollmentRequest.Status == RequestStatus.Pending
                      || (enrollmentRequest.Status == RequestStatus.Approved
                          && !enrollmentRequest.Course.IsFlexible);
        if (!canRespond)
            return BadRequest<string>("Group confirmation is not allowed at this stage.");

        var groupMember = enrollmentRequest.GroupMembers.FirstOrDefault(m => m.StudentId == request.Data.StudentId);
        if (groupMember == null)
            return NotFound<string>("Group member invitation not found.");

        if (groupMember.MemberType != GroupMemberType.Invited)
            return BadRequest<string>("Only invited members can respond to invitations.");

        if (groupMember.ConfirmationStatus != GroupMemberConfirmationStatus.Pending)
            return BadRequest<string>("This invitation has already been handled.");

        var targetStudent = await _studentRepository.GetTableNoTracking()
            .Include(s => s.Guardian)
            .FirstOrDefaultAsync(s => s.Id == request.Data.StudentId && s.IsActive, cancellationToken);
        if (targetStudent == null)
            return NotFound<string>("Student not found.");

        // Minor students: ONLY guardian can respond
        // Non-minor students: ONLY the student themselves can respond
        if (targetStudent.IsMinor)
        {
            var guardian = await _guardianRepository.GetByUserIdAsync(request.UserId);
            var isGuardian = targetStudent.GuardianId.HasValue
                          && guardian != null
                          && targetStudent.GuardianId.Value == guardian.Id;

            if (!isGuardian)
                return BadRequest<string>("Only the guardian can respond to invitations for minor students.");
        }
        else
        {
            if (targetStudent.UserId != request.UserId)
                return BadRequest<string>("Only the student themselves can respond to this invitation.");
        }

        groupMember.ConfirmationStatus = request.Data.Decision;
        groupMember.ConfirmedAt = DateTime.UtcNow;
        groupMember.ConfirmedByUserId = request.UserId;

        await _requestRepository.SaveChangesAsync();

        // Fixed-course finalization: once every invitee has responded, the auto-approved request
        // crystallizes into a CourseGroupEnrollment in PendingPayment.
        if (!enrollmentRequest.Course.IsFlexible
            && enrollmentRequest.Status == RequestStatus.Approved)
        {
            var stillPendingInvitees = enrollmentRequest.GroupMembers.Any(
                gm => gm.MemberType == GroupMemberType.Invited
                   && gm.ConfirmationStatus == GroupMemberConfirmationStatus.Pending);

            if (!stillPendingInvitees)
            {
                var alreadyHasEnrollment = await _enrollmentRepository.GetTableNoTracking()
                    .AnyAsync(e => e.EnrollmentRequestId == enrollmentRequest.Id, cancellationToken);

                var hasAnyConfirmedMember = enrollmentRequest.GroupMembers.Any(
                    gm => gm.ConfirmationStatus == GroupMemberConfirmationStatus.Confirmed);

                if (!alreadyHasEnrollment && hasAnyConfirmedMember)
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

        return Success<string>(entity: "Group invitation response saved.");
    }
}
