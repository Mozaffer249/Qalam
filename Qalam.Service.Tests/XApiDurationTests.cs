using Qalam.Service.Implementations.LRS.Building;
using Xunit;

namespace Qalam.Service.Tests;

public class XApiDurationTests
{
    [Theory]
    [InlineData(0, "PT00H00M00S")]
    [InlineData(60, "PT01H00M00S")]
    [InlineData(90, "PT01H30M00S")]
    [InlineData(1800, "PT30H00M00S")] // 30 hours — exceeds 24h, still valid (NELC course duration)
    public void FromMinutes_formats_full_iso8601(double minutes, string expected)
    {
        Assert.Equal(expected, XApiDuration.FromMinutes(minutes));
    }

    [Fact]
    public void ToIso8601_includes_seconds()
    {
        // 622s = 10m22s → the NELC "watched" example shape
        Assert.Equal("PT00H10M22S", XApiDuration.ToIso8601(System.TimeSpan.FromSeconds(622)));
    }

    [Fact]
    public void Negative_clamps_to_zero()
    {
        Assert.Equal("PT00H00M00S", XApiDuration.FromMinutes(-5));
    }
}
