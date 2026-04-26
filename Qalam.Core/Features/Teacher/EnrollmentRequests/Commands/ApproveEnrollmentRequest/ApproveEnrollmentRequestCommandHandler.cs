using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Course;
using Qalam.Data.Helpers;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Teacher.EnrollmentRequests.Commands.ApproveEnrollmentRequest;

public class ApproveEnrollmentRequestCommandHandler : ResponseHandler,
    IRequestHandler<ApproveEnrollmentRequestCommand, Response<string>>
{
    private readonly ITeacherRepository _teacherRepository;
    private readonly ICourseEnrollmentRequestRepository _requestRepository;
    private readonly ICourseEnrollmentRepository _enrollmentRepository;
    private readonly ICourseGroupEnrollmentRepository _groupEnrollmentRepository;
    private readonly EnrollmentSettings _settings;

    public ApproveEnrollmentRequestCommandHandler(
        ITeacherRepository teacherRepository,
        ICourseEnrollmentRequestRepository requestRepository,
        ICourseEnrollmentRepository enrollmentRepository,
        ICourseGroupEnrollmentRepository groupEnrollmentRepository,
        IOptions<EnrollmentSettings> settings,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _teacherRepository = teacherRepository;
        _requestRepository = requestRepository;
        _enrollmentRepository = enrollmentRepository;
        _groupEnrollmentRepository = groupEnrollmentRepository;
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

        if (enrollmentRequest.Status != RequestStatus.Pending)
            return BadRequest<string>("Only pending requests can be approved.");

        var confirmedMembers = enrollmentRequest.GroupMembers
            .Where(gm => gm.ConfirmationStatus == GroupMemberConfirmationStatus.Confirmed)
            .ToList();

        if (confirmedMembers.Count == 0)
            return BadRequest<string>("No confirmed group members found.");

        var paymentDeadline = DateTime.UtcNow.AddHours(_settings.PaymentDeadlineHours);
        var isGroupCourse = string.Equals(enrollmentRequest.Course.SessionType?.Code, "group", StringComparison.OrdinalIgnoreCase);

        var transaction = await _requestRepository.BeginTransactionAsync();
        try
        {
            if (isGroupCourse)
            {
                var leaderMember = confirmedMembers
                    .FirstOrDefault(gm => gm.MemberType == GroupMemberType.Own)
                    ?? confirmedMembers.First();

                var groupEnrollment = new CourseGroupEnrollment
                {
                    CourseId = enrollmentRequest.CourseId,
                    EnrollmentRequestId = enrollmentRequest.Id,
                    LeaderStudentId = leaderMember.StudentId,
                    Status = EnrollmentStatus.PendingPayment,
                    PaymentDeadline = paymentDeadline
                };

                await _groupEnrollmentRepository.AddAsync(groupEnrollment);
                await _groupEnrollmentRepository.SaveChangesAsync();

                var members = confirmedMembers.Select(gm => new CourseGroupEnrollmentMember
                {
                    CourseGroupEnrollmentId = groupEnrollment.Id,
                    StudentId = gm.StudentId,
                    PaymentStatus = PaymentStatus.Pending
                }).ToList();

                await _groupEnrollmentRepository.GetTableAsTracking()
                    .Where(g => g.Id == groupEnrollment.Id)
                    .Include(g => g.Members)
                    .LoadAsync(cancellationToken);

                foreach (var member in members)
                    groupEnrollment.Members.Add(member);

                await _groupEnrollmentRepository.SaveChangesAsync();
            }
            else
            {
                var studentMember = confirmedMembers.First();

                var enrollment = new CourseEnrollment
                {
                    CourseId = enrollmentRequest.CourseId,
                    StudentId = studentMember.StudentId,
                    EnrollmentRequestId = enrollmentRequest.Id,
                    ApprovedByTeacherId = teacher.Id,
                    ApprovedAt = DateTime.UtcNow,
                    PaymentDeadline = paymentDeadline,
                    EnrollmentStatus = EnrollmentStatus.PendingPayment
                };

                await _enrollmentRepository.AddAsync(enrollment);
                await _enrollmentRepository.SaveChangesAsync();
            }

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
