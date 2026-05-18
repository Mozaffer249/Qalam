namespace Qalam.Data.DTOs.Auth;

/// <summary>
/// Stored as JSON in common.SystemSettings (Key = Auth.Settings).
/// </summary>
public class AuthSettingsDto
{
    public PersonaAuthSettingsDto Teacher { get; set; } = new();
    public PersonaAuthSettingsDto Student { get; set; } = new();
    public OtpGlobalSettingsDto Otp { get; set; } = new();
}

public class PersonaAuthSettingsDto
{
    /// <summary>Usually <c>Otp</c>.</summary>
    public string LoginMethod { get; set; } = "Otp";
    /// <summary><c>Email</c> or <c>Sms</c> (Sms needs server <c>SmsSettings:Enabled</c>).</summary>
    public string OtpDelivery { get; set; } = "Email";
    /// <summary>Maps to public <c>showEmailField</c> + <c>emailRequired</c>.</summary>
    public bool RegisterRequiresEmail { get; set; } = true;
    /// <summary>Maps to public <c>showPhoneField</c> + <c>phoneRequired</c>.</summary>
    public bool RegisterRequiresPhone { get; set; } = true;
}

public class OtpGlobalSettingsDto
{
    /// <summary>OTP digit count.</summary>
    public int Length { get; set; } = 4;
    /// <summary>OTP expiry in seconds.</summary>
    public int ExpirySeconds { get; set; } = 300;
    /// <summary>When true, dev environments may accept a fixed test code.</summary>
    public bool AllowTestCodeInDevelopment { get; set; } = true;
}
