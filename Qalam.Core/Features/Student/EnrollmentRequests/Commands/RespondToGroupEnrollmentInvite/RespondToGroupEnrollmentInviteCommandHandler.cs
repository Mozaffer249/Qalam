using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Student.EnrollmentRequests.Commands.RespondToGroupEnrollmentInvite;

public class RespondToGroupEnrollmentInviteCommandHandler : ResponseHandler,
    IRequestHandler<RespondToGroupEnrollmentInviteCommand, Response<string>>
{
    private readonly ICourseEnrollmentRequestRepository _requestRepository;
    private readonly IStudentRepository _studentRepository;
    private readonly IGuardianRepository _guardianRepository;

    public RespondToGroupEnrollmentInviteCommandHandler(
        ICourseEnrollmentRequestRepository requestRepository,
        IStudentRepository studentRepository,
        IGuardianRepository guardianRepository,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _requestRepository = requestRepository;
        _studentRepository = studentRepository;
        _guardianRepository = guardianRepository;
    }

    public async Task<Response<string>> Handle(
        RespondToGroupEnrollmentInviteCommand request,
        CancellationToken cancellationToken)
    {
        var enrollmentRequest = await _requestRepository.GetTableAsTracking()
            .Include(r => r.GroupMembers)
            .FirstOrDefaultAsync(r => r.Id == request.EnrollmentRequestId, cancellationToken);
        if (enrollmentRequest == null)
            return NotFound<string>("Enrollment request not found.");

        if (enrollmentRequest.Status != RequestStatus.Pending)
            return BadRequest<string>("Group confirmation is only allowed while request is pending.");

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
        return Success<string>(entity: "Group invitation response saved.");
    }
}
