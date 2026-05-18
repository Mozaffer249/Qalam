using Qalam.Data.DTOs.Auth;
using Qalam.Service.Implementations;
using Xunit;

namespace Qalam.Service.Tests;

public class AuthLoginOtpHelperTests
{
    private readonly AuthLoginOtpHelper _helper = new();

    [Fact]
    public void ResolveDeliveryEmail_NewUser_RequiresEmail_ReturnsNormalized()
    {
        var email = _helper.ResolveDeliveryEmail(new()
        {
            Settings = new PersonaAuthSettingsDto { OtpDelivery = "Email", RegisterRequiresEmail = true },
            IsNewUser = true,
            RequestEmail = "User@Example.com"
        });

        Assert.Equal("user@example.com", email);
    }

    [Fact]
    public void ResolveDeliveryEmail_ExistingUser_UsesProfileEmail()
    {
        var email = _helper.ResolveDeliveryEmail(new()
        {
            Settings = new PersonaAuthSettingsDto { OtpDelivery = "Email" },
            IsNewUser = false,
            ExistingUserEmail = "teacher@qalam.test"
        });

        Assert.Equal("teacher@qalam.test", email);
    }

    [Fact]
    public void MaskEmail_HidesLocalPart()
    {
        var masked = _helper.MaskEmail("ahmed@example.com");
        Assert.Contains("@example.com", masked);
        Assert.Contains("***", masked);
    }
}
