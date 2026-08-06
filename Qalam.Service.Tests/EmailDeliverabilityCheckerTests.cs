using Microsoft.Extensions.Logging.Abstractions;
using Qalam.Service.Implementations;
using Xunit;

namespace Qalam.Service.Tests;

public class EmailDeliverabilityCheckerTests
{
    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("not an email", false)]
    [InlineData("@nodomain", false)]
    [InlineData("valid@example.com", true)]
    public void IsValidFormat_checks_syntax(string? email, bool expected)
    {
        var checker = new EmailDeliverabilityChecker(NullLogger<EmailDeliverabilityChecker>.Instance);
        Assert.Equal(expected, checker.IsValidFormat(email));
    }

    [Fact]
    public async Task CheckAsync_rejects_synthetic_local_domain()
    {
        var checker = new EmailDeliverabilityChecker(NullLogger<EmailDeliverabilityChecker>.Instance);
        var result = await checker.CheckAsync("phone_123@phone.qalam.local");
        Assert.False(result.IsDeliverable);
    }

    [Fact]
    public async Task CheckAsync_rejects_invalid_format()
    {
        var checker = new EmailDeliverabilityChecker(NullLogger<EmailDeliverabilityChecker>.Instance);
        var result = await checker.CheckAsync("amail-without-at");
        Assert.False(result.IsDeliverable);
        Assert.Contains("format", result.ErrorMessage!, StringComparison.OrdinalIgnoreCase);
    }
}
