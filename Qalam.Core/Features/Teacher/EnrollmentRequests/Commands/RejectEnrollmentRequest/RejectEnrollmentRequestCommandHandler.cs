using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Teacher.EnrollmentRequests.Commands.RejectEnrollmentRequest;

public class RejectEnrollmentRequestCommandHandler : ResponseHandler,
    IRequestHandler<RejectEnrollmentRequestCommand, Response<string>>
{
    private readonly ITeacherRepository _teacherRepository;
    private readonly ICourseEnrollmentRequestRepository _requestRepository;

    public RejectEnrollmentRequestCommandHandler(
        ITeacherRepository teacherRepository,
        ICourseEnrollmentRequestRepository requestRepository,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _teacherRepository = teacherRepository;
        _requestRepository = requestRepository;
    }

    public async Task<Response<string>> Handle(
        RejectEnrollmentRequestCommand request,
        CancellationToken cancellationToken)
    {
        var teacher = await _teacherRepository.GetByUserIdAsync(request.UserId);
        if (teacher == null)
            return NotFound<string>("Teacher profile not found.");

        var enrollmentRequest = await _requestRepository.GetTableAsTracking()
            .Include(r => r.Course)
            .FirstOrDefaultAsync(r => r.Id == request.RequestId, cancellationToken);

        if (enrollmentRequest == null)
            return NotFound<string>("Enrollment request not found.");

        if (enrollmentRequest.Course.TeacherId != teacher.Id)
            return BadRequest<string>("This request does not belong to your course.");

        if (enrollmentRequest.Status != RequestStatus.Pending)
            return BadRequest<string>("Only pending requests can be rejected.");

        enrollmentRequest.Status = RequestStatus.Rejected;
        enrollmentRequest.RejectionReason = request.Data?.RejectionReason;

        await _requestRepository.SaveChangesAsync();

        return Success<string>(entity: "Enrollment request rejected.");
    }
}
