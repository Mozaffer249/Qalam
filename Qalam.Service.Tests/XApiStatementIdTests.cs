using System;
using Qalam.Service.Implementations.LRS.Building;
using Xunit;

namespace Qalam.Service.Tests;

public class XApiStatementIdTests
{
    [Fact]
    public void Same_parts_produce_same_id() // idempotent re-dispatch (SEQ003)
    {
        var a = XApiStatementId.Deterministic("https://qalam.net.sa", "1105806192", "registered", "https://qalam.net.sa/course/6008");
        var b = XApiStatementId.Deterministic("https://qalam.net.sa", "1105806192", "registered", "https://qalam.net.sa/course/6008");
        Assert.Equal(a, b);
    }

    [Fact]
    public void Different_verb_produces_different_id()
    {
        var registered = XApiStatementId.Deterministic("p", "1105806192", "registered", "course/6008");
        var earned = XApiStatementId.Deterministic("p", "1105806192", "earned", "course/6008");
        Assert.NotEqual(registered, earned);
    }

    [Fact]
    public void Different_learner_produces_different_id()
    {
        var one = XApiStatementId.Deterministic("p", "1105806192", "registered", "course/6008");
        var two = XApiStatementId.Deterministic("p", "1066181346", "registered", "course/6008");
        Assert.NotEqual(one, two);
    }

    [Fact]
    public void Produces_a_valid_guid()
    {
        Assert.True(Guid.TryParse(XApiStatementId.Deterministic("a", "b"), out _));
    }
}
