using Qalam.Data.Entity.Course;

namespace Qalam.Data.Helpers;

public static class CourseDurationHelper
{
    /// <summary>
    /// Non-flexible courses store duration per <see cref="CourseSession"/>; <see cref="Course.SessionDurationMinutes"/> may be null.
    /// Prefer summing sessions when present so pricing matches catalog totals.
    /// </summary>
    public static int ResolveFixedTotalMinutes(Course course)
    {
        if (course.IsFlexible)
            return 0;

        var sessionCountNav = course.Sessions?.Count ?? 0;
        var sumSessions = sessionCountNav > 0 ? course.Sessions!.Sum(s => s.DurationMinutes) : 0;

        var countForUniform = course.SessionsCount ?? sessionCountNav;
        var uniformProduct = countForUniform * (course.SessionDurationMinutes ?? 0);

        if (sumSessions > 0)
            return sumSessions;

        return uniformProduct;
    }

    public static decimal ComputeTotalPriceFromHourly(decimal pricePerHour, int totalMinutes)
    {
        if (totalMinutes <= 0 || pricePerHour <= 0)
            return pricePerHour;

        return Math.Round(totalMinutes / 60m * pricePerHour, 2, MidpointRounding.AwayFromZero);
    }
}
