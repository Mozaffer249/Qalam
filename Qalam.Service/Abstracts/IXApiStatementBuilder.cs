using Qalam.Data.DTOs.LRS;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Course;
using Qalam.Data.Entity.Identity;

namespace Qalam.Service.Abstracts;

public interface IXApiStatementBuilder
{
    XApiStatement BuildRegistered(User student, Course course, Enrollment enrollment, User? teacher);
    XApiStatement BuildSessionLaunched(User student, CourseSchedule schedule, Course course);
    XApiStatement BuildSessionAttended(User student, CourseSchedule schedule, Course course);
    XApiStatement BuildSessionCompleted(User student, CourseSchedule schedule, Course course, User? teacher, SessionAttendanceStatus? attendance);
    XApiStatement BuildProgressUpdated(User student, Course course, Enrollment enrollment, int completedSessions, int totalSessions);
    XApiStatement BuildCourseCompleted(User student, Course course, Enrollment enrollment);
}
