using System.Xml;
using Qalam.Data.DTOs.LRS;

namespace Qalam.Service.Implementations.LRS.Building;

/// <summary>
/// Last-line guard that asserts NELC's invariants on a built statement before it is queued, so a
/// regression is caught here (and logged) instead of by FutureX verification. Pure and testable.
/// </summary>
public static class NelcStatementValidator
{
    public static (bool Ok, string? Error) Validate(XApiStatement statement)
    {
        var actor = statement.Actor;
        if (actor == null || string.IsNullOrWhiteSpace(actor.Name))
            return (false, "actor.name is required");

        var ifiCount = (actor.Mbox != null ? 1 : 0) + (actor.Account != null ? 1 : 0);
        if (ifiCount != 1)
            return (false, "actor must have exactly one inverse-functional identifier (mbox or account)");

        // CRS002: a statement whose object is the course must not carry contextActivities.
        var isCourseObject = statement.Object?.Definition?.Type == XApiActivityTypes.Course;
        if (isCourseObject && statement.Context?.ContextActivities != null)
            return (false, "course-object statement must not have contextActivities");

        // CRS004: durations must be valid ISO-8601.
        if (!string.IsNullOrEmpty(statement.Result?.Duration) && !IsValidIso8601Duration(statement.Result!.Duration!))
            return (false, $"invalid ISO-8601 duration: {statement.Result.Duration}");

        var verb = statement.Verb?.Id ?? string.Empty;

        if (verb == XApiVerbs.Registered)
        {
            var ext = statement.Context?.Extensions;
            if (ext == null
                || !ext.ContainsKey(NelcExtensions.Duration)
                || !ext.ContainsKey(NelcExtensions.LmsUrl)
                || !ext.ContainsKey(NelcExtensions.ProgramUrl)
                || !ext.ContainsKey(NelcExtensions.Platform))
                return (false, "registered statement missing required NELC extensions");
        }

        // ASM004: rated must carry a text response.
        if (verb == XApiVerbs.Rated && string.IsNullOrWhiteSpace(statement.Result?.Response))
            return (false, "rated statement missing text response");

        return (true, null);
    }

    private static bool IsValidIso8601Duration(string value)
    {
        try
        {
            XmlConvert.ToTimeSpan(value);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
