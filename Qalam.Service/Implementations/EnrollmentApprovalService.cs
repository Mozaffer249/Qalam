using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Course;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Service.Implementations;

public class EnrollmentApprovalService : IEnrollmentApprovalService
{
    private readonly IEnrollmentRepository _enrollmentRepository;

    public EnrollmentApprovalService(IEnrollmentRepository enrollmentRepository)
    {
        _enrollmentRepository = enrollmentRepository;
    }

    public async Task<Enrollment> CreatePendingPaymentArtifactsAsync(
        CourseEnrollmentRequest request,
        Course course,
        int approvingTeacherId,
        DateTime paymentDeadline,
        CancellationToken cancellationToken)
    {
        var confirmedMembers = request.GroupMembers
            .Where(gm => gm.ConfirmationStatus == GroupMemberConfirmationStatus.Confirmed)
            .ToList();

        if (confirmedMembers.Count == 0)
            throw new InvalidOperationException(
                $"No confirmed group members; cannot create enrollment artifacts for request {request.Id}.");

        var isGroupCourse = string.Equals(course.SessionType?.Code, "group", StringComparison.OrdinalIgnoreCase);

        var kind = isGroupCourse ? EnrollmentKind.Group : EnrollmentKind.Individual;
        int? leaderStudentId = null;

        if (isGroupCourse)
        {
            var leader = confirmedMembers.FirstOrDefault(gm => gm.MemberType == GroupMemberType.Own)
                         ?? confirmedMembers.First();
            leaderStudentId = leader.StudentId;
        }

        var enrollment = new Enrollment
        {
            CourseId = request.CourseId,
            EnrollmentRequestId = request.Id,
            Kind = kind,
            LeaderStudentId = leaderStudentId,
            ApprovedByTeacherId = approvingTeacherId,
            ApprovedAt = DateTime.UtcNow,
            PaymentDeadline = paymentDeadline,
            EnrollmentStatus = EnrollmentStatus.PendingPayment,
            Participants = confirmedMembers.Select(gm => new EnrollmentParticipant
            {
                StudentId = gm.StudentId,
                PaymentStatus = PaymentStatus.Pending
            }).ToList()
        };

        await _enrollmentRepository.AddAsync(enrollment);
        await _enrollmentRepository.SaveChangesAsync();

        return enrollment;
    }
}
