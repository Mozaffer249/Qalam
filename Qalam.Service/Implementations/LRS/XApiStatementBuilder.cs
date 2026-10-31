using Microsoft.Extensions.Options;
using Qalam.Data.DTOs.LRS;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Course;
using Qalam.Data.Entity.Identity;
using Qalam.Data.Helpers;
using Qalam.Service.Abstracts;

namespace Qalam.Service.Implementations.LRS;

public class XApiStatementBuilder : IXApiStatementBuilder
{
    private readonly LrsSettings _settings;

    public XApiStatementBuilder(IOptions<LrsSettings> settings)
    {
        _settings = settings.Value;
    }

    public XApiStatement BuildRegistered(User student, Course course, Enrollment enrollment, User? teacher, string? identityNumber = null)
    {
        var ctx = BuildContext(teacher, course);
        ctx.Extensions ??= new Dictionary<string, object>();
        ctx.Extensions[NelcExtensions.LearnerFullName] = $"{student.FirstName} {student.LastName}".Trim();
        ctx.Extensions[NelcExtensions.LmsUrl] = _settings.PlatformIri;
        ctx.Extensions[NelcExtensions.ProgramUrl] = $"{_settings.PlatformIri}/courses/{course.Id}";
        ctx.Extensions[NelcExtensions.Platform] = new Dictionary<string, string>
        {
            ["ar-SA"] = _settings.PlatformNameAr,
            ["en-US"] = _settings.PlatformNameEn
        };

        if (!string.IsNullOrEmpty(student.PhoneNumber))
            ctx.Extensions[NelcExtensions.LearnerMobileNo] = student.PhoneNumber;
        if (!string.IsNullOrEmpty(student.Nationality))
            ctx.Extensions[NelcExtensions.LearnerNationality] = student.Nationality;

        return new XApiStatement
        {
            Actor = BuildActor(student, identityNumber),
            Verb = XApiVerbs.Create(XApiVerbs.Registered, "registered"),
            Object = BuildCourseObject(course),
            Context = ctx,
            Timestamp = DateTime.UtcNow.ToString("o")
        };
    }

    public XApiStatement BuildSessionLaunched(User student, CourseSchedule schedule, Course course, string? identityNumber = null)
    {
        return new XApiStatement
        {
            Actor = BuildActor(student, identityNumber),
            Verb = XApiVerbs.Create(XApiVerbs.Initialized, "initialized"),
            Object = BuildSessionObject(schedule, course),
            Context = BuildContextWithParentCourse(course),
            Timestamp = DateTime.UtcNow.ToString("o")
        };
    }

    public XApiStatement BuildSessionAttended(User student, CourseSchedule schedule, Course course, string? identityNumber = null)
    {
        var result = new XApiResult { Completion = true };

        if (schedule.StartedAt.HasValue && schedule.EndedAt.HasValue)
        {
            var duration = schedule.EndedAt.Value - schedule.StartedAt.Value;
            result.Duration = $"PT{(int)duration.TotalMinutes}M";
        }
        else if (schedule.DurationMinutes > 0)
        {
            result.Duration = $"PT{schedule.DurationMinutes}M";
        }

        return new XApiStatement
        {
            Actor = BuildActor(student, identityNumber),
            Verb = XApiVerbs.Create(XApiVerbs.Attended, "attended"),
            Object = BuildSessionObject(schedule, course),
            Result = result,
            Context = BuildContextWithParentCourse(course),
            Timestamp = DateTime.UtcNow.ToString("o")
        };
    }

    public XApiStatement BuildSessionCompleted(User student, CourseSchedule schedule, Course course, User? teacher, SessionAttendanceStatus? attendance, string? identityNumber = null)
    {
        var stmt = new XApiStatement
        {
            Actor = BuildActor(student, identityNumber),
            Verb = XApiVerbs.Create(XApiVerbs.Completed, "completed"),
            Object = BuildSessionObject(schedule, course),
            Context = BuildContext(teacher, course),
            Timestamp = DateTime.UtcNow.ToString("o"),
            Result = new XApiResult
            {
                Completion = attendance == SessionAttendanceStatus.Present,
                Success = attendance == SessionAttendanceStatus.Present
            }
        };

        if (schedule.StartedAt.HasValue && schedule.EndedAt.HasValue)
        {
            var duration = schedule.EndedAt.Value - schedule.StartedAt.Value;
            stmt.Result.Duration = $"PT{(int)duration.TotalMinutes}M";
        }

        return stmt;
    }

    public XApiStatement BuildProgressUpdated(User student, Course course, Enrollment enrollment, int completedSessions, int totalSessions, string? identityNumber = null)
    {
        var scaled = totalSessions > 0 ? (decimal)completedSessions / totalSessions : 0m;

        return new XApiStatement
        {
            Actor = BuildActor(student, identityNumber),
            Verb = XApiVerbs.Create(XApiVerbs.Progressed, "progressed"),
            Object = BuildCourseObject(course),
            Result = new XApiResult
            {
                Score = new XApiScore
                {
                    Scaled = Math.Round(scaled, 4),
                    Raw = completedSessions,
                    Min = 0,
                    Max = totalSessions
                },
                Completion = completedSessions >= totalSessions
            },
            Context = BuildContextWithParentCourse(course),
            Timestamp = DateTime.UtcNow.ToString("o")
        };
    }

    public XApiStatement BuildCourseCompleted(User student, Course course, Enrollment enrollment, string? identityNumber = null)
    {
        var stmt = new XApiStatement
        {
            Actor = BuildActor(student, identityNumber),
            Verb = XApiVerbs.Create(XApiVerbs.Completed, "completed"),
            Object = BuildCourseObject(course),
            Result = new XApiResult
            {
                Completion = true,
                Success = true
            },
            Timestamp = DateTime.UtcNow.ToString("o")
        };

        if (enrollment.CreatedAt != default && enrollment.CompletedAt.HasValue)
        {
            var duration = enrollment.CompletedAt.Value - enrollment.CreatedAt;
            stmt.Result.Duration = $"P{(int)duration.TotalDays}D";
        }

        return stmt;
    }

    private XApiActor BuildActor(User user, string? identityNumber = null)
    {
        if (!string.IsNullOrEmpty(identityNumber))
        {
            return new XApiActor
            {
                Name = $"{user.FirstName} {user.LastName}".Trim(),
                Account = new XApiAccount
                {
                    HomePage = _settings.PlatformIri,
                    Name = identityNumber
                }
            };
        }

        return new XApiActor
        {
            Name = user.Email ?? $"{user.FirstName} {user.LastName}".Trim(),
            Mbox = !string.IsNullOrEmpty(user.Email) ? $"mailto:{user.Email}" : null,
            Account = string.IsNullOrEmpty(user.Email)
                ? new XApiAccount
                {
                    HomePage = _settings.PlatformIri,
                    Name = $"user-{user.Id}"
                }
                : null
        };
    }

    private XApiObject BuildCourseObject(Course course)
    {
        return new XApiObject
        {
            Id = $"{_settings.PlatformIri}/course/{course.Id}",
            Definition = new XApiActivityDefinition
            {
                Type = XApiActivityTypes.Course,
                Name = new Dictionary<string, string> { ["ar-SA"] = course.Title }
            }
        };
    }

    private XApiObject BuildSessionObject(CourseSchedule schedule, Course course)
    {
        return new XApiObject
        {
            Id = $"{_settings.PlatformIri}/course/{course.Id}/session/{schedule.Id}",
            Definition = new XApiActivityDefinition
            {
                Type = XApiActivityTypes.VirtualClassroom,
                Name = new Dictionary<string, string>
                {
                    ["ar-SA"] = $"{course.Title} - جلسة {schedule.Date:yyyy-MM-dd}"
                }
            }
        };
    }

    private XApiContext BuildContext(User? teacher, Course? course)
    {
        var ctx = new XApiContext
        {
            Platform = _settings.PlatformKey,
            Language = "ar-SA"
        };

        if (teacher != null)
        {
            ctx.Instructor = BuildActor(teacher);
        }

        if (course != null)
        {
            ctx.ContextActivities = new XApiContextActivities
            {
                Grouping = new List<XApiObject> { BuildCourseObject(course) }
            };
        }

        return ctx;
    }

    private XApiContext BuildContextWithParentCourse(Course course)
    {
        return new XApiContext
        {
            Platform = _settings.PlatformKey,
            Language = "ar-SA",
            ContextActivities = new XApiContextActivities
            {
                Parent = new List<XApiObject> { BuildCourseObject(course) }
            }
        };
    }
}
