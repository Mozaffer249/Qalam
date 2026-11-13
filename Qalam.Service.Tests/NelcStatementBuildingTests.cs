using Qalam.Data.DTOs.LRS;
using Qalam.Data.Entity.Course;
using Qalam.Data.Entity.Identity;
using Qalam.Data.Helpers;
using Qalam.Service.Implementations.LRS.Building;
using Xunit;

namespace Qalam.Service.Tests;

public class NelcStatementBuildingTests
{
    private static LrsSettings Settings() => new()
    {
        PlatformIri = "https://qalam.net.sa",
        PlatformKey = "https://qalam.net.sa"
    };

    private static Course Course() => new() { Id = 6008, Title = "cee" };

    [Fact]
    public void CourseContext_has_no_contextActivities() // CRS002
    {
        var ctx = NelcContextFactory.Course(Settings());
        Assert.Null(ctx.ContextActivities);
    }

    [Fact]
    public void CertificateContext_parents_the_course() // earned is a sub-activity, parent allowed
    {
        var ctx = NelcContextFactory.Certificate(Settings(), Course());

        Assert.NotNull(ctx.ContextActivities);
        Assert.NotNull(ctx.ContextActivities!.Parent);
        Assert.Equal("https://qalam.net.sa/course/6008", ctx.ContextActivities.Parent![0].Id);
    }

    [Fact]
    public void CourseObject_uses_course_type_and_singular_id() // CRS003
    {
        var obj = NelcActivityFactory.Course(Settings(), Course());

        Assert.Equal("https://qalam.net.sa/course/6008", obj.Id);
        Assert.Equal(XApiActivityTypes.Course, obj.Definition!.Type);
    }

    [Fact]
    public void CertificateObject_uses_certificate_type_and_enrollment_scoped_id()
    {
        var obj = NelcActivityFactory.Certificate(Settings(), Course(), enrollmentId: 7069);

        Assert.Equal("https://qalam.net.sa/course/6008/certificate/7069", obj.Id);
        Assert.Equal(XApiActivityTypes.Certificate, obj.Definition!.Type);
    }

    [Fact]
    public void Actor_with_identity_and_email_is_single_ifi_mbox() // avoids CRS "> 3 properties"
    {
        var user = new User { Id = 1, Email = "learner@example.com", FirstName = "A", LastName = "B" };

        var actor = XApiActorFactory.Agent(Settings(), user, "1105806192");

        Assert.Equal("1105806192", actor.Name);
        Assert.Equal("mailto:learner@example.com", actor.Mbox);
        Assert.Null(actor.Account); // never both mbox and account
    }

    [Fact]
    public void Actor_with_identity_but_no_email_falls_back_to_account()
    {
        var user = new User { Id = 1, Email = null, FirstName = "A", LastName = "B" };

        var actor = XApiActorFactory.Agent(Settings(), user, "1105806192");

        Assert.Equal("1105806192", actor.Name);
        Assert.Null(actor.Mbox);
        Assert.NotNull(actor.Account);
        Assert.Equal("1105806192", actor.Account!.Name);
    }
}
