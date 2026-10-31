using Qalam.Data.DTOs.LRS;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Course;
using Qalam.Data.Entity.Identity;

namespace Qalam.Service.Abstracts;

public interface IXApiStatementBuilder
{
    Task LoadSettingsAsync(CancellationToken ct = default);
    XApiStatement BuildRegistered(User student, Course course, Enrollment enrollment, User? teacher, string? identityNumber = null);
    XApiStatement BuildSessionLaunched(User student, CourseSchedule schedule, Course course, string? identityNumber = null);
    XApiStatement BuildSessionAttended(User student, CourseSchedule schedule, Course course, string? identityNumber = null);
    XApiStatement BuildSessionCompleted(User student, CourseSchedule schedule, Course course, User? teacher, SessionAttendanceStatus? attendance, string? identityNumber = null);
    XApiStatement BuildProgressUpdated(User student, Course course, Enrollment enrollment, int completedSessions, int totalSessions, string? identityNumber = null);
    XApiStatement BuildCourseCompleted(User student, Course course, Enrollment enrollment, User? teacher = null, string? identityNumber = null);
}
