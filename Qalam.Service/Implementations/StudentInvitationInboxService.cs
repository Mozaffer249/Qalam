using Microsoft.Extensions.Options;
using Qalam.Data.DTOs.Course;
using Qalam.Data.Helpers;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Service.Implementations;

public class StudentInvitationInboxService : IStudentInvitationInboxService
{
    private readonly IStudentRepository _studentRepository;
    private readonly IGuardianRepository _guardianRepository;
    private readonly ICourseEnrollmentRequestRepository _enrollmentRequestRepository;
    private readonly IOpenSessionRequestRepository _openSessionRequestRepository;
    private readonly IMediaUrlResolver _mediaUrlResolver;
    private readonly EnrollmentSettings _enrollmentSettings;

    public StudentInvitationInboxService(
        IStudentRepository studentRepository,
        IGuardianRepository guardianRepository,
        ICourseEnrollmentRequestRepository enrollmentRequestRepository,
        IOpenSessionRequestRepository openSessionRequestRepository,
        IMediaUrlResolver mediaUrlResolver,
        IOptions<EnrollmentSettings> enrollmentSettings)
    {
        _studentRepository = studentRepository;
        _guardianRepository = guardianRepository;
        _enrollmentRequestRepository = enrollmentRequestRepository;
        _openSessionRequestRepository = openSessionRequestRepository;
        _mediaUrlResolver = mediaUrlResolver;
        _enrollmentSettings = enrollmentSettings.Value;
    }

    public async Task<StudentInvitationListResultDto> GetMyInvitationsAsync(
        int userId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var visibleStudentIds = await ResolveVisibleStudentIdsAsync(userId, cancellationToken);
        if (visibleStudentIds.Count == 0)
        {
            return new StudentInvitationListResultDto
            {
                Items = new List<StudentInvitationListItemDto>(),
                TotalCount = 0
            };
        }

        var deadlineHours = Math.Max(1, _enrollmentSettings.InviteResponseDeadlineHours);

        var s1Items = await _enrollmentRequestRepository.GetPendingInvitationListItemsAsync(
            visibleStudentIds, cancellationToken);
        foreach (var item in s1Items)
        {
            item.CourseImageUrl = _mediaUrlResolver.ToPublicUrl(item.CourseImageUrl);
            item.RespondByUtc = item.CreatedAt.AddHours(deadlineHours);
        }

        var s2Items = await _openSessionRequestRepository.GetPendingInvitationListItemsAsync(
            visibleStudentIds, cancellationToken);
        foreach (var item in s2Items)
            item.RespondByUtc = item.CreatedAt.AddHours(deadlineHours);

        var merged = s1Items
            .Concat(s2Items)
            .OrderByDescending(x => x.CreatedAt)
            .ToList();

        var totalCount = merged.Count;
        var page = Math.Max(1, pageNumber);
        var size = Math.Clamp(pageSize, 1, 100);
        var items = merged
            .Skip((page - 1) * size)
            .Take(size)
            .ToList();

        return new StudentInvitationListResultDto
        {
            Items = items,
            TotalCount = totalCount
        };
    }

    /// <summary>
    /// Adult self (no GuardianId) and/or this user's guardian children — never a minor's own id.
    /// </summary>
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
