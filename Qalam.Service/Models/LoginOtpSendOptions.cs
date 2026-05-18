using Qalam.Data.DTOs.Auth;
using Qalam.Data.Entity.Identity;

namespace Qalam.Service.Models;

public class LoginOtpSendOptions
{
    public string CountryCode { get; set; } = "+966";
    public string PhoneNumber { get; set; } = default!;
    public string? RequestEmail { get; set; }
    public string? ExistingUserEmail { get; set; }
    public bool IsNewUser { get; set; }
    public LoginOtpPersona Persona { get; set; } = LoginOtpPersona.Teacher;
    public PersonaAuthSettingsDto PersonaSettings { get; set; } = new();
    public OtpGlobalSettingsDto OtpSettings { get; set; } = new();
}

public class LoginOtpSendResult
{
    public string OtpSentTo { get; set; } = default!;
    public string MaskedDestination { get; set; } = default!;
    public LoginOtpChannel Channel { get; set; }
}
