using Qalam.Data.DTOs.LRS;
using Qalam.Data.Entity.Course;
using Qalam.Data.Entity.Identity;

namespace Qalam.Service.Abstracts;

public interface IXApiStatementBuilder
{
    Task LoadSettingsAsync(CancellationToken ct = default);
    XApiStatement BuildRegistered(User student, Course course, Enrollment enrollment, User? teacher, string? identityNumber = null);
    XApiStatement BuildInitialized(User student, Course course, User? teacher, string? identityNumber = null);
    XApiStatement BuildProgressUpdated(User student, Course course, Enrollment enrollment, int completedSessions, int totalSessions, string? identityNumber = null);
    XApiStatement BuildCourseCompleted(User student, Course course, Enrollment enrollment, string? identityNumber = null);
    XApiStatement BuildRated(User student, Course course, int rating, string? feedback, string? identityNumber = null);
    XApiStatement BuildCourseEarned(User student, Course course, Enrollment enrollment, string? identityNumber = null);
}
