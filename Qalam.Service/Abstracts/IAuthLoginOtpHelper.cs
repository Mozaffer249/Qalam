using Qalam.Data.DTOs.Auth;
using Qalam.Data.Entity.Identity;

namespace Qalam.Service.Abstracts;

public interface IAuthLoginOtpHelper
{
    string? ResolveDeliveryEmail(LoginOtpEmailContext context);
    /// <summary>Email to persist on the user after OTP verify (PendingEmail or delivery destination).</summary>
    string? ResolveRegistrationEmail(LoginOtp? loginOtp);
    /// <summary>Non-empty email for Identity user creation (registration email or phone-derived fallback).</summary>
    string ResolveAccountEmail(string? registrationEmail, string fullPhoneNumber);
    string MaskEmail(string email);
    string MaskPhone(string phoneNumber);
}

public class LoginOtpEmailContext
{
    public PersonaAuthSettingsDto Settings { get; set; } = new();
    public bool IsNewUser { get; set; }
    public string? RequestEmail { get; set; }
    public string? ExistingUserEmail { get; set; }
}
