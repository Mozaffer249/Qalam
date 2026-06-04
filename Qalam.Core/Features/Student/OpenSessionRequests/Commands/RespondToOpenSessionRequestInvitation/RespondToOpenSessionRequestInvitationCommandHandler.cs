using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Features.Student.OpenSessionRequests.Services;
using Qalam.Core.Resources.Shared;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Infrastructure.context;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Student.OpenSessionRequests.Commands.RespondToOpenSessionRequestInvitation;

public class RespondToOpenSessionRequestInvitationCommandHandler
    : ResponseHandler, IRequestHandler<RespondToOpenSessionRequestInvitationCommand, Response<string>>
{
    private readonly ApplicationDBContext _db;
    private readonly IOpenSessionRequestAccessGuard _accessGuard;
    private readonly IOpenSessionRequestTargetingService _targetingService;

    public RespondToOpenSessionRequestInvitationCommandHandler(
        IStringLocalizer<SharedResources> sharedLocalizer,
        ApplicationDBContext db,
        IOpenSessionRequestAccessGuard accessGuard,
        IOpenSessionRequestTargetingService targetingService) : base(sharedLocalizer)
    {
        _db = db;
        _accessGuard = accessGuard;
        _targetingService = targetingService;
    }

    public async Task<Response<string>> Handle(
        RespondToOpenSessionRequestInvitationCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Authorize: the responder must be the invited student (if adult) or their guardian
        if (!await _accessGuard.CanRespondToInvitationAsync(request.UserId, request.Data.StudentId, cancellationToken))
            return Unauthorized<string>("غير مصرح لك بالرد على هذه الدعوة");

        // 2. Load the request + the specific invitation row
        var parentRequest = await _db.OpenSessionRequests
            .Include(r => r.Invitations)
            .FirstOrDefaultAsync(r => r.Id == request.OpenSessionRequestId, cancellationToken);

        if (parentRequest is null)
            return NotFound<string>("الطلب غير موجود");

        if (parentRequest.Status == OpenSessionRequestStatus.Cancelled
            || parentRequest.Status == OpenSessionRequestStatus.Expired)
            return BadRequest<string>("لا يمكن الرد على دعوة لطلب ملغى أو منتهي");

        var invitation = parentRequest.Invitations
            .FirstOrDefault(i => i.InvitedStudentId == request.Data.StudentId);
        if (invitation is null)
            return NotFound<string>("الدعوة غير موجودة لهذا الطالب");

        if (invitation.Status != OpenSessionRequestInvitationStatus.Pending)
            return BadRequest<string>($"تم الرد على هذه الدعوة مسبقاً ({invitation.Status})");

        // 3. Update the invitation
        invitation.Status = request.Data.Decision;
        invitation.RespondedAt = DateTime.UtcNow;

        // 4. If all invitations have responded and the request was waiting on them,
        //    transition status: PendingInvitations -> Active (or Cancelled if all rejected).
        if (parentRequest.Status == OpenSessionRequestStatus.PendingInvitations)
        {
            var stillPending = parentRequest.Invitations
                .Any(i => i.Status == OpenSessionRequestInvitationStatus.Pending);

            if (!stillPending)
            {
                var anyAccepted = parentRequest.Invitations
                    .Any(i => i.Status == OpenSessionRequestInvitationStatus.Accepted);

                parentRequest.Status = anyAccepted
                    ? OpenSessionRequestStatus.Active
                    : OpenSessionRequestStatus.Cancelled;

                if (parentRequest.Status == OpenSessionRequestStatus.Cancelled)
                {
                    parentRequest.CancelledAt = DateTime.UtcNow;
                    parentRequest.CancellationReason = "جميع المدعوين رفضوا الانضمام";
                }
            }
        }

        await _db.SaveChangesAsync(cancellationToken);

        // P3: if this response flipped the request to Active, dispatch now. Targeted requests
        // notify only the chosen teacher; un-targeted requests fall back to broadcast matching.
        // Both branches are idempotent — safe to call.
        if (parentRequest.Status == OpenSessionRequestStatus.Active)
        {
            if (parentRequest.TargetedTeacherId.HasValue)
            {
                await _targetingService.NotifyTargetedTeacherAsync(
                    parentRequest.Id, parentRequest.TargetedTeacherId.Value, cancellationToken);
            }
            else
            {
                await _targetingService.RunMatchingAndNotifyAsync(parentRequest.Id, cancellationToken);
            }
        }

        return Success(entity: invitation.Status == OpenSessionRequestInvitationStatus.Accepted
            ? "تم قبول الدعوة"
            : "تم رفض الدعوة");
    }
}
