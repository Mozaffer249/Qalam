using Qalam.Data.DTOs.LRS;
using Qalam.Data.Entity.Course;
using Qalam.Data.Helpers;

namespace Qalam.Service.Implementations.LRS.Building;

/// <summary>
/// Builds xAPI activity objects with the correct ids, activity types and definitions.
/// Centralizes the <c>{iri}/course/{id}</c> id convention so every verb lands on the right object type (CRS003).
/// </summary>
public static class NelcActivityFactory
{
    public static string CourseId(LrsSettings settings, Course course) =>
        $"{settings.PlatformIri}/course/{course.Id}";

    // includeDescription is true ONLY for the registered statement — NELC rejects a course
    // description on any other course-level statement (CTX001 / CRS008).
    public static XApiObject Course(LrsSettings settings, Course course, bool includeDescription = false)
    {
        var definition = new XApiActivityDefinition
        {
            Type = XApiActivityTypes.Course,
            Name = new Dictionary<string, string> { ["ar-SA"] = course.Title, ["en-US"] = course.Title }
        };

        if (includeDescription && !string.IsNullOrWhiteSpace(course.Description))
            definition.Description = new Dictionary<string, string>
            {
                ["ar-SA"] = course.Description!,
                ["en-US"] = course.Description!
            };

        return new XApiObject { Id = CourseId(settings, course), Definition = definition };
    }

    public static XApiObject Certificate(LrsSettings settings, Course course, int enrollmentId)
    {
        return new XApiObject
        {
            Id = $"{settings.PlatformIri}/course/{course.Id}/certificate/{enrollmentId}",
            Definition = new XApiActivityDefinition
            {
                Type = XApiActivityTypes.Certificate,
                Name = new Dictionary<string, string>
                {
                    ["ar-SA"] = $"شهادة {course.Title}",
                    ["en-US"] = $"{course.Title} Certificate"
                }
            }
        };
    }
}
