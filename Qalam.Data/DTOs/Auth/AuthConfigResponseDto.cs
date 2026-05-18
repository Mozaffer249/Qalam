namespace Qalam.Data.DTOs.Auth;

/// <summary>
/// Public auth configuration for frontend UI (GET /Authentication/Config).
/// </summary>
public class AuthConfigResponseDto
{
    public AuthPersonaConfigDto Teacher { get; set; } = new();
    public AuthPersonaConfigDto Student { get; set; } = new();
    public AuthOtpConfigDto Otp { get; set; } = new();
}

public class AuthPersonaConfigDto
{
    /// <summary>Usually <c>Otp</c> — password login not used for teacher/student.</summary>
    public string LoginMethod { get; set; } = "Otp";
    /// <summary><c>Email</c> or <c>Sms</c> — where the login OTP is sent.</summary>
    public string OtpDelivery { get; set; } = "Email";
    /// <summary>Show phone + country code on step 1.</summary>
    public bool ShowPhoneField { get; set; }
    /// <summary>Show email field on step 1.</summary>
    public bool ShowEmailField { get; set; }
    /// <summary>Validate phone before Send OTP.</summary>
    public bool PhoneRequired { get; set; }
    /// <summary>Validate email before Send OTP.</summary>
    public bool EmailRequired { get; set; }
    /// <summary>English subtitle on OTP verify screen (combine with masked destination from Send OTP).</summary>
    public string OtpHintEn { get; set; } = default!;
    /// <summary>Arabic subtitle on OTP verify screen.</summary>
    public string OtpHintAr { get; set; } = default!;
}

public class AuthOtpConfigDto
{
    /// <summary>OTP digit count (input maxlength).</summary>
    public int Length { get; set; }
    /// <summary>OTP validity in seconds (optional UI countdown).</summary>
    public int ExpirySeconds { get; set; }
}
