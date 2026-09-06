using Qalam.Data.DTOs.Account;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Service.Implementations;

public class AccountDeactivationGuardService : IAccountDeactivationGuardService
{
    private readonly IStudentRepository _studentRepository;
    private readonly IGuardianRepository _guardianRepository;
    private readonly IEnrollmentRepository _enrollmentRepository;
    private readonly IOpenSessionRequestRepository _openSessionRequestRepository;
    private readonly ICourseEnrollmentRequestRepository _enrollmentRequestRepository;

    public AccountDeactivationGuardService(
        IStudentRepository studentRepository,
        IGuardianRepository guardianRepository,
        IEnrollmentRepository enrollmentRepository,
        IOpenSessionRequestRepository openSessionRequestRepository,
        ICourseEnrollmentRequestRepository enrollmentRequestRepository)
    {
        _studentRepository = studentRepository;
        _guardianRepository = guardianRepository;
        _enrollmentRepository = enrollmentRepository;
        _openSessionRequestRepository = openSessionRequestRepository;
        _enrollmentRequestRepository = enrollmentRequestRepository;
    }

    public async Task<IReadOnlyList<string>> GetBlockingReasonsAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var studentIds = await ResolveVisibleStudentIdsAsync(userId, cancellationToken);
        var reasons = new List<string>();

        if (await _enrollmentRepository.AnyActiveOrPendingPaymentAsync(studentIds, cancellationToken))
            reasons.Add(AccountDeactivationBlockingReasons.ActiveEnrollment);

        if (await _openSessionRequestRepository.AnyBlockingForUserAsync(userId, studentIds, cancellationToken))
            reasons.Add(AccountDeactivationBlockingReasons.OpenSessionRequest);

        var hasS1Invites = await _enrollmentRequestRepository.AnyBlockingInvitationsForUserAsync(
            userId, studentIds, cancellationToken);
        var hasS2Invites = await _openSessionRequestRepository.AnyBlockingInvitationsForUserAsync(
            userId, studentIds, cancellationToken);
        if (hasS1Invites || hasS2Invites)
            reasons.Add(AccountDeactivationBlockingReasons.PendingInvitation);

        return reasons;
    }

    private async Task<List<int>> ResolveVisibleStudentIdsAsync(
        int userId,
        CancellationToken cancellationToken)
    {
        var visible = new List<int>();

        var ownStudent = await _studentRepository.GetByUserIdAsync(userId);
        if (ownStudent != null && !ownStudent.GuardianId.HasValue)
            visible.Add(ownStudent.Id);

        var guardian = await _guardianRepository.GetByUserIdAsync(userId);
        if (guardian != null)
        {
            var children = await _studentRepository.GetChildrenByGuardianIdAsync(guardian.Id);
            visible.AddRange(children.Select(c => c.Id));
        }

        return visible.Distinct().ToList();
    }
}
