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

    public static XApiObject Course(LrsSettings settings, Course course)
    {
        var definition = new XApiActivityDefinition
        {
            Type = XApiActivityTypes.Course,
            Name = new Dictionary<string, string> { ["ar-SA"] = course.Title, ["en-US"] = course.Title }
        };

        if (!string.IsNullOrWhiteSpace(course.Description))
            definition.Description = new Dictionary<string, string> { ["ar-SA"] = course.Description! };

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
