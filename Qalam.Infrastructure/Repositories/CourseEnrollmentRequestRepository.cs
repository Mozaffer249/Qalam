using Microsoft.EntityFrameworkCore;
using Qalam.Data.DTOs.Course;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Course;
using Qalam.Infrastructure.Abstracts;
using Qalam.Infrastructure.context;
using Qalam.Infrastructure.InfrastructureBases;

namespace Qalam.Infrastructure.Repositories;

public class CourseEnrollmentRequestRepository : GenericRepositoryAsync<CourseEnrollmentRequest>, ICourseEnrollmentRequestRepository
{
    private readonly ApplicationDBContext _context;

    public CourseEnrollmentRequestRepository(ApplicationDBContext context) : base(context)
    {
        _context = context;
    }

    public IQueryable<CourseEnrollmentRequest> GetByUserIdQueryable(int userId)
    {
        return _context.CourseEnrollmentRequests
            .AsNoTracking()
            .Where(r => r.RequestedByUserId == userId)
            .Include(r => r.Course)
                .ThenInclude(c => c.TeachingMode)
            .Include(r => r.Course)
                .ThenInclude(c => c.SessionType)
            .OrderByDescending(r => r.CreatedAt);
    }

    public IQueryable<CourseEnrollmentRequest> GetByCourseIdQueryable(int courseId)
    {
        return _context.CourseEnrollmentRequests
            .AsNoTracking()
            .Where(r => r.CourseId == courseId)
            .Include(r => r.Course)
                .ThenInclude(c => c.TeachingMode)
            .Include(r => r.Course)
                .ThenInclude(c => c.SessionType)
            .Include(r => r.RequestedByUser)
            .Include(r => r.GroupMembers)
                .ThenInclude(gm => gm.Student)
                    .ThenInclude(s => s.User)
            .Include(r => r.SelectedAvailabilities)
            .Include(r => r.ProposedSessions)
            .OrderByDescending(r => r.CreatedAt);
    }

    public IQueryable<CourseRequestGroupMember> GetPendingInvitationsForStudentsQueryable(List<int> studentIds)
    {
        return _context.CourseRequestGroupMembers
            .AsNoTracking()
            .Where(gm => studentIds.Contains(gm.StudentId)
                      && gm.MemberType == GroupMemberType.Invited
                      && gm.ConfirmationStatus == GroupMemberConfirmationStatus.Pending
                      && (gm.CourseEnrollmentRequest.Status == RequestStatus.Pending
                          || gm.CourseEnrollmentRequest.Status == RequestStatus.Approved))
            .Include(gm => gm.Student)
                .ThenInclude(s => s.User)
            .Include(gm => gm.CourseEnrollmentRequest)
                .ThenInclude(r => r.Course)
                    .ThenInclude(c => c.Teacher)
                        .ThenInclude(t => t.User)
            .Include(gm => gm.CourseEnrollmentRequest)
                .ThenInclude(r => r.RequestedByUser)
            .OrderByDescending(gm => gm.CreatedAt);
    }

    public async Task<List<StudentInvitationListItemDto>> GetPendingInvitationListItemsAsync(
        IReadOnlyCollection<int> studentIds,
        CancellationToken cancellationToken = default)
    {
        if (studentIds.Count == 0)
            return new List<StudentInvitationListItemDto>();

        return await _context.CourseRequestGroupMembers
            .AsNoTracking()
            .Where(gm => studentIds.Contains(gm.StudentId)
                      && gm.MemberType == GroupMemberType.Invited
                      && gm.ConfirmationStatus == GroupMemberConfirmationStatus.Pending
                      && (gm.CourseEnrollmentRequest.Status == RequestStatus.Pending
                          || gm.CourseEnrollmentRequest.Status == RequestStatus.Approved))
            .OrderByDescending(gm => gm.CreatedAt)
            .Select(gm => new StudentInvitationListItemDto
            {
                Source = "EnrollmentRequest",
                InvitationId = gm.Id,
                EnrollmentRequestId = gm.CourseEnrollmentRequestId,
                OpenSessionRequestId = null,
                CourseId = gm.CourseEnrollmentRequest.CourseId,
                CourseTitle = gm.CourseEnrollmentRequest.Course != null
                    ? gm.CourseEnrollmentRequest.Course.Title
                    : "",
                CourseImageUrl = gm.CourseEnrollmentRequest.Course != null
                    ? gm.CourseEnrollmentRequest.Course.ImageUrl
                    : null,
                TeacherDisplayName = gm.CourseEnrollmentRequest.Course != null
                    && gm.CourseEnrollmentRequest.Course.Teacher != null
                    && gm.CourseEnrollmentRequest.Course.Teacher.User != null
                    ? (gm.CourseEnrollmentRequest.Course.Teacher.User.FirstName + " "
                       + gm.CourseEnrollmentRequest.Course.Teacher.User.LastName).Trim()
                    : null,
                TitleEn = null,
                TitleAr = null,
                InvitedStudentId = gm.StudentId,
                InvitedStudentName = gm.Student != null && gm.Student.User != null
                    ? (gm.Student.User.FirstName + " " + gm.Student.User.LastName).Trim()
                    : null,
                RequestedByUserName = gm.CourseEnrollmentRequest.RequestedByUser != null
                    ? (gm.CourseEnrollmentRequest.RequestedByUser.FirstName + " "
                       + gm.CourseEnrollmentRequest.RequestedByUser.LastName).Trim()
                    : null,
                CreatedAt = gm.CreatedAt,
                ConfirmationStatus = gm.ConfirmationStatus,
                IsOwner = false
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<List<StudentInvitationListItemDto>> GetSentInvitationListItemsAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var members = await _context.CourseRequestGroupMembers
            .AsNoTracking()
            .Where(gm => gm.CourseEnrollmentRequest.RequestedByUserId == userId
                      && gm.MemberType == GroupMemberType.Invited)
            .Select(gm => new
            {
                gm.Id,
                gm.StudentId,
                gm.ConfirmationStatus,
                gm.CreatedAt,
                gm.CourseEnrollmentRequestId,
                RequestCreatedAt = gm.CourseEnrollmentRequest.CreatedAt,
                RequestStatus = gm.CourseEnrollmentRequest.Status,
                CourseId = gm.CourseEnrollmentRequest.CourseId,
                CourseTitle = gm.CourseEnrollmentRequest.Course != null
                    ? gm.CourseEnrollmentRequest.Course.Title
                    : "",
                CourseImageUrl = gm.CourseEnrollmentRequest.Course != null
                    ? gm.CourseEnrollmentRequest.Course.ImageUrl
                    : null,
                TeacherDisplayName = gm.CourseEnrollmentRequest.Course != null
                    && gm.CourseEnrollmentRequest.Course.Teacher != null
                    && gm.CourseEnrollmentRequest.Course.Teacher.User != null
                    ? (gm.CourseEnrollmentRequest.Course.Teacher.User.FirstName + " "
                       + gm.CourseEnrollmentRequest.Course.Teacher.User.LastName).Trim()
                    : null,
                InvitedStudentName = gm.Student != null && gm.Student.User != null
                    ? (gm.Student.User.FirstName + " " + gm.Student.User.LastName).Trim()
                    : null,
                RequestedByUserName = gm.CourseEnrollmentRequest.RequestedByUser != null
                    ? (gm.CourseEnrollmentRequest.RequestedByUser.FirstName + " "
                       + gm.CourseEnrollmentRequest.RequestedByUser.LastName).Trim()
                    : null
            })
            .ToListAsync(cancellationToken);

        return members
            .GroupBy(m => m.CourseEnrollmentRequestId)
            .Select(g =>
            {
                var invite = g
                    .OrderBy(m => m.ConfirmationStatus == GroupMemberConfirmationStatus.Pending ? 0 : 1)
                    .ThenBy(m => m.CreatedAt)
                    .First();
                var request = g.First();
                return new StudentInvitationListItemDto
                {
                    Source = "EnrollmentRequest",
                    InvitationId = invite.Id,
                    EnrollmentRequestId = request.CourseEnrollmentRequestId,
                    OpenSessionRequestId = null,
                    CourseId = request.CourseId,
                    CourseTitle = request.CourseTitle,
                    CourseImageUrl = request.CourseImageUrl,
                    TeacherDisplayName = request.TeacherDisplayName,
                    TitleEn = null,
                    TitleAr = null,
                    InvitedStudentId = invite.StudentId,
                    InvitedStudentName = invite.InvitedStudentName,
                    RequestedByUserName = request.RequestedByUserName,
                    CreatedAt = request.RequestCreatedAt,
                    ConfirmationStatus = invite.ConfirmationStatus,
                    IsOwner = true,
                    ParentStatus = request.RequestStatus.ToString()
                };
            })
            .ToList();
    }
}
