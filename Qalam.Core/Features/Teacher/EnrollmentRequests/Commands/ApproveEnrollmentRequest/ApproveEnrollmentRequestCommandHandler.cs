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

namespace Qalam.Core.Features.Teacher.EnrollmentRequests.Commands.ApproveEnrollmentRequest;

public class ApproveEnrollmentRequestCommandHandler : ResponseHandler,
    IRequestHandler<ApproveEnrollmentRequestCommand, Response<string>>
{
    private readonly ITeacherRepository _teacherRepository;
    private readonly ICourseEnrollmentRequestRepository _requestRepository;
    private readonly IEnrollmentApprovalService _approvalService;
    private readonly EnrollmentSettings _settings;

    public ApproveEnrollmentRequestCommandHandler(
        ITeacherRepository teacherRepository,
        ICourseEnrollmentRequestRepository requestRepository,
        IEnrollmentApprovalService approvalService,
        IOptions<EnrollmentSettings> settings,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _teacherRepository = teacherRepository;
        _requestRepository = requestRepository;
        _approvalService = approvalService;
        _settings = settings.Value;
    }

    public async Task<Response<string>> Handle(
        ApproveEnrollmentRequestCommand request,
        CancellationToken cancellationToken)
    {
        var teacher = await _teacherRepository.GetByUserIdAsync(request.UserId);
        if (teacher == null)
            return NotFound<string>("Teacher profile not found.");

        var enrollmentRequest = await _requestRepository.GetTableAsTracking()
            .Include(r => r.Course).ThenInclude(c => c.SessionType)
            .Include(r => r.GroupMembers)
            .FirstOrDefaultAsync(r => r.Id == request.RequestId, cancellationToken);

        if (enrollmentRequest == null)
            return NotFound<string>("Enrollment request not found.");

        if (enrollmentRequest.Course.TeacherId != teacher.Id)
            return BadRequest<string>("This request does not belong to your course.");

        // Fixed courses auto-approve on submit; manual teacher approval is meaningless.
        if (!enrollmentRequest.Course.IsFlexible)
            return BadRequest<string>(
                "This course is fixed — approval is automatic on submit. No teacher action is required.");

        if (enrollmentRequest.Status != RequestStatus.Pending)
            return BadRequest<string>("Only pending requests can be approved.");

        var paymentDeadline = DateTime.UtcNow.AddHours(_settings.PaymentDeadlineHours);

        var transaction = await _requestRepository.BeginTransactionAsync();
        try
        {
            await _approvalService.CreatePendingPaymentArtifactsAsync(
                enrollmentRequest,
                enrollmentRequest.Course,
                teacher.Id,
                paymentDeadline,
                cancellationToken);

            enrollmentRequest.Status = RequestStatus.Approved;
            await _requestRepository.SaveChangesAsync();

            await _requestRepository.CommitAsync();

            return Success<string>(entity: $"Enrollment request approved. Payment deadline: {paymentDeadline:u}");
        }
        catch
        {
            await _requestRepository.RollBackAsync();
            throw;
        }
    }
}
