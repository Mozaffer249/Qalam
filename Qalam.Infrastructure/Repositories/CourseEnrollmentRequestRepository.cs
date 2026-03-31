using Microsoft.EntityFrameworkCore;
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
                      && gm.CourseEnrollmentRequest.Status == RequestStatus.Pending)
            .Include(gm => gm.Student)
                .ThenInclude(s => s.User)
            .Include(gm => gm.CourseEnrollmentRequest)
                .ThenInclude(r => r.Course)
            .Include(gm => gm.CourseEnrollmentRequest)
                .ThenInclude(r => r.RequestedByUser)
            .OrderByDescending(gm => gm.CreatedAt);
    }
}
