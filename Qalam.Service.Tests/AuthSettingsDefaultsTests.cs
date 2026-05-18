using Qalam.Data.DTOs.Auth;
using Xunit;

namespace Qalam.Service.Tests;

public class AuthSettingsDefaultsTests
{
    [Fact]
    public void RoundTrip_Json_PreservesEmailDelivery()
    {
        var original = AuthSettingsDefaults.Create();
        var json = AuthSettingsDefaults.ToJson(original);
        var restored = AuthSettingsDefaults.FromJson(json);

        Assert.Equal("Email", restored.Teacher.OtpDelivery);
        Assert.Equal("Email", restored.Student.OtpDelivery);
        Assert.True(restored.Teacher.RegisterRequiresEmail);
        Assert.Equal(4, restored.Otp.Length);
    }
}
