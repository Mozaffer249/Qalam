namespace Qalam.Data.DTOs.Teacher;

/// <summary>
/// Response DTO for SendOtp (LoginOrRegister) endpoint
/// </summary>
public class SendOtpResponseDto
{
    /// <summary>
    /// Indicates if this is a new user (registration) or existing user (login)
    /// </summary>
    public bool IsNewUser { get; set; }
    
    /// <summary>
    /// User-friendly message about the OTP status
    /// </summary>
    public string Message { get; set; } = default!;
    
    /// <summary>
    /// The phone number that OTP was sent to (masked for security)
    /// </summary>
    public string PhoneNumber { get; set; } = default!;

    /// <summary>
    /// Delivery channel: email or sms
    /// </summary>
    public string OtpSentTo { get; set; } = default!;

    /// <summary>
    /// Masked email or phone where the OTP was delivered
    /// </summary>
    public string MaskedDestination { get; set; } = default!;

    /// <summary>
    /// True when the existing user has already accepted Terms &amp; Privacy.
    /// Always false for new users until they accept during registration.
    /// </summary>
    public bool HasAcceptedTerms { get; set; }
}
