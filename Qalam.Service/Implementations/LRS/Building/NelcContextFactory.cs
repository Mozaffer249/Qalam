using Qalam.Data.DTOs.LRS;
using Qalam.Data.Entity.Course;
using Qalam.Data.Entity.Identity;
using Qalam.Data.Helpers;

namespace Qalam.Service.Implementations.LRS.Building;

/// <summary>
/// Builds the xAPI <c>context</c>. Statements whose object IS the course must not carry
/// <c>contextActivities</c> (validator rule CRS002) — <see cref="Course"/> enforces that by
/// construction. Only sub-activity statements (e.g. the certificate for <c>earned</c>) carry a parent.
/// </summary>
public static class NelcContextFactory
{
    private static string PlatformValue(LrsSettings s) =>
        !string.IsNullOrEmpty(s.PlatformKey) ? s.PlatformKey : s.PlatformIri;

    /// <summary>Context for a course-object statement: platform + language, optional instructor/extensions, never contextActivities.</summary>
    public static XApiContext Course(
        LrsSettings settings,
        User? instructor = null,
        Dictionary<string, object>? extensions = null)
    {
        return new XApiContext
        {
            Platform = PlatformValue(settings),
            Language = "ar-SA",
            Instructor = instructor != null ? XApiActorFactory.Instructor(instructor) : null,
            Extensions = extensions
        };
    }

    /// <summary>Context for a sub-activity statement (certificate): adds <c>contextActivities.parent = [course]</c>.</summary>
    public static XApiContext Certificate(
        LrsSettings settings,
        Course course,
        Dictionary<string, object>? extensions = null)
    {
        return new XApiContext
        {
            Platform = PlatformValue(settings),
            Language = "ar-SA",
            Extensions = extensions,
            ContextActivities = new XApiContextActivities
            {
                Parent = new List<XApiObject> { NelcActivityFactory.Course(settings, course) }
            }
        };
    }
}
