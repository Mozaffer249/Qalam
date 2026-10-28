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

    public XApiStatement BuildRegistered(User student, Course course, Enrollment enrollment, User? teacher)
    {
        return new XApiStatement
        {
            Actor = BuildActor(student),
            Verb = XApiVerbs.Create(XApiVerbs.Registered, "registered", "مسجّل"),
            Object = BuildCourseObject(course),
            Context = BuildContext(teacher, course),
            Timestamp = DateTime.UtcNow.ToString("o")
        };
    }

    public XApiStatement BuildSessionLaunched(User student, CourseSchedule schedule, Course course)
    {
        return new XApiStatement
        {
            Actor = BuildActor(student),
            Verb = XApiVerbs.Create(XApiVerbs.Launched, "launched", "بدأ"),
            Object = BuildSessionObject(schedule, course),
            Context = BuildContextWithParentCourse(course),
            Timestamp = DateTime.UtcNow.ToString("o")
        };
    }

    public XApiStatement BuildSessionAttended(User student, CourseSchedule schedule, Course course)
    {
        return new XApiStatement
        {
            Actor = BuildActor(student),
            Verb = XApiVerbs.Create(XApiVerbs.Attended, "attended", "حضر"),
            Object = BuildSessionObject(schedule, course),
            Context = BuildContextWithParentCourse(course),
            Timestamp = DateTime.UtcNow.ToString("o")
        };
    }

    public XApiStatement BuildSessionCompleted(User student, CourseSchedule schedule, Course course, User? teacher, SessionAttendanceStatus? attendance)
    {
        var stmt = new XApiStatement
        {
            Actor = BuildActor(student),
            Verb = XApiVerbs.Create(XApiVerbs.Completed, "completed", "أكمل"),
            Object = BuildSessionObject(schedule, course),
            Context = BuildContext(teacher, course),
            Timestamp = DateTime.UtcNow.ToString("o"),
            Result = new XApiResult
            {
                Completion = attendance == SessionAttendanceStatus.Present
            }
        };

        if (schedule.StartedAt.HasValue && schedule.EndedAt.HasValue)
        {
            var duration = schedule.EndedAt.Value - schedule.StartedAt.Value;
            stmt.Result.Duration = $"PT{(int)duration.TotalMinutes}M";
        }

        return stmt;
    }

    public XApiStatement BuildProgressUpdated(User student, Course course, Enrollment enrollment, int completedSessions, int totalSessions)
    {
        var scaled = totalSessions > 0 ? (decimal)completedSessions / totalSessions : 0m;

        return new XApiStatement
        {
            Actor = BuildActor(student),
            Verb = XApiVerbs.Create(XApiVerbs.Progressed, "progressed", "تقدّم"),
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
            Timestamp = DateTime.UtcNow.ToString("o")
        };
    }

    public XApiStatement BuildCourseCompleted(User student, Course course, Enrollment enrollment)
    {
        var stmt = new XApiStatement
        {
            Actor = BuildActor(student),
            Verb = XApiVerbs.Create(XApiVerbs.Completed, "completed", "أكمل"),
            Object = BuildCourseObject(course),
            Result = new XApiResult { Completion = true },
            Timestamp = DateTime.UtcNow.ToString("o")
        };

        if (enrollment.CreatedAt != default && enrollment.CompletedAt.HasValue)
        {
            var duration = enrollment.CompletedAt.Value - enrollment.CreatedAt;
            stmt.Result.Duration = $"P{(int)duration.TotalDays}D";
        }

        return stmt;
    }

    private XApiActor BuildActor(User student)
    {
        return new XApiActor
        {
            Name = $"{student.FirstName} {student.LastName}".Trim(),
            Account = new XApiAccount
            {
                HomePage = _settings.PlatformIri,
                Name = $"student-{student.Id}"
            }
        };
    }

    private XApiObject BuildCourseObject(Course course)
    {
        return new XApiObject
        {
            Id = $"{_settings.PlatformIri}/courses/{course.Id}",
            Definition = new XApiActivityDefinition
            {
                Type = XApiActivityTypes.Course,
                Name = new Dictionary<string, string> { ["ar"] = course.Title }
            }
        };
    }

    private XApiObject BuildSessionObject(CourseSchedule schedule, Course course)
    {
        return new XApiObject
        {
            Id = $"{_settings.PlatformIri}/sessions/{schedule.Id}",
            Definition = new XApiActivityDefinition
            {
                Type = XApiActivityTypes.Session,
                Name = new Dictionary<string, string>
                {
                    ["ar"] = $"{course.Title} - جلسة {schedule.Date:yyyy-MM-dd}"
                }
            }
        };
    }

    private XApiContext BuildContext(User? teacher, Course? course)
    {
        var ctx = new XApiContext
        {
            Platform = _settings.PlatformIri,
            Language = "ar"
        };

        if (teacher != null)
        {
            ctx.Instructor = new XApiActor
            {
                Name = $"{teacher.FirstName} {teacher.LastName}".Trim(),
                Account = new XApiAccount
                {
                    HomePage = _settings.PlatformIri,
                    Name = $"teacher-{teacher.Id}"
                }
            };
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
            Platform = _settings.PlatformIri,
            Language = "ar",
            ContextActivities = new XApiContextActivities
            {
                Parent = new List<XApiObject> { BuildCourseObject(course) }
            }
        };
    }
}
