using System.Text.Json;

namespace Qalam.Data.DTOs.Auth;

public static class AuthSettingsDefaults
{
    public static AuthSettingsDto Create() => new()
    {
        Teacher = new PersonaAuthSettingsDto
        {
            LoginMethod = "Otp",
            OtpDelivery = "Email",
            RegisterRequiresEmail = true,
            RegisterRequiresPhone = true
        },
        Student = new PersonaAuthSettingsDto
        {
            LoginMethod = "Otp",
            OtpDelivery = "Email",
            RegisterRequiresEmail = true,
            RegisterRequiresPhone = true
        },
        Otp = new OtpGlobalSettingsDto
        {
            Length = 4,
            ExpirySeconds = 300,
            AllowTestCodeInDevelopment = true
        }
    };

    public static string ToJson(AuthSettingsDto settings) =>
        JsonSerializer.Serialize(settings, JsonOptions);

    public static AuthSettingsDto FromJson(string json) =>
        JsonSerializer.Deserialize<AuthSettingsDto>(json, JsonOptions) ?? Create();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };
}
