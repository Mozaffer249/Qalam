using Qalam.Data.DTOs.Auth;

namespace Qalam.Service.Abstracts;

public interface IAuthLoginOtpHelper
{
    string? ResolveDeliveryEmail(LoginOtpEmailContext context);
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
