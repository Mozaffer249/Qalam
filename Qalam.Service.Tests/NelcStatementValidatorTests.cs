using System.Collections.Generic;
using Qalam.Data.DTOs.LRS;
using Qalam.Service.Implementations.LRS.Building;
using Xunit;

namespace Qalam.Service.Tests;

public class NelcStatementValidatorTests
{
    private static XApiStatement ValidCourseStatement(string verbId = XApiVerbs.Completed) => new()
    {
        Actor = new XApiActor { Name = "1105806192", Mbox = "mailto:x@y.com" },
        Verb = XApiVerbs.Create(verbId, "verb"),
        Object = new XApiObject
        {
            Id = "https://qalam.net.sa/course/6008",
            Definition = new XApiActivityDefinition { Type = XApiActivityTypes.Course }
        },
        Context = new XApiContext { Platform = "https://qalam.net.sa", Language = "ar-SA" }
    };

    [Fact]
    public void Valid_statement_passes()
    {
        Assert.True(NelcStatementValidator.Validate(ValidCourseStatement()).Ok);
    }

    [Fact]
    public void Actor_with_two_ifi_fails()
    {
        var s = ValidCourseStatement();
        s.Actor.Account = new XApiAccount { HomePage = "https://qalam.net.sa", Name = "1105806192" };
        Assert.False(NelcStatementValidator.Validate(s).Ok);
    }

    [Fact]
    public void Course_object_with_contextActivities_fails() // CRS002
    {
        var s = ValidCourseStatement();
        s.Context!.ContextActivities = new XApiContextActivities { Parent = new List<XApiObject> { s.Object } };
        Assert.False(NelcStatementValidator.Validate(s).Ok);
    }

    [Fact]
    public void Invalid_duration_fails() // CRS004
    {
        var s = ValidCourseStatement();
        s.Result = new XApiResult { Duration = "PT60M-bad" };
        Assert.False(NelcStatementValidator.Validate(s).Ok);
    }

    [Fact]
    public void Rated_without_response_fails() // ASM004
    {
        var s = ValidCourseStatement(XApiVerbs.Rated);
        s.Result = new XApiResult { Response = null };
        Assert.False(NelcStatementValidator.Validate(s).Ok);
    }

    [Fact]
    public void Registered_without_extensions_fails()
    {
        var s = ValidCourseStatement(XApiVerbs.Registered);
        Assert.False(NelcStatementValidator.Validate(s).Ok);
    }
}
