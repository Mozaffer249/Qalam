using Qalam.Data.Entity.Course;
using Qalam.Data.Entity.Pricing;

namespace Qalam.Service.Tests;

public class EnrollmentCoursePriceTests
{
    [Fact]
    public void ComputeTotalPriceFromHourly_TwoHoursAt85_Returns170()
    {
        var total = Qalam.Data.Helpers.CourseDurationHelper.ComputeTotalPriceFromHourly(85m, 120);
        Assert.Equal(170m, total);
    }

    [Fact]
    public void ResolveFixedTotalMinutes_SumsSessionDurations()
    {
        var course = new Course
        {
            Sessions =
            [
                new CourseSession { DurationMinutes = 60 },
                new CourseSession { DurationMinutes = 60 },
            ],
        };

        var minutes = Qalam.Data.Helpers.CourseDurationHelper.ResolveFixedTotalMinutes(course);
        Assert.Equal(120, minutes);
    }
}
