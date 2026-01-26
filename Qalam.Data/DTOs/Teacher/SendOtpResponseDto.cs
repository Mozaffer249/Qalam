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
}
