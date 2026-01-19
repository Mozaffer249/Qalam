namespace Qalam.Service.Abstracts;

public interface IOtpService
{
    Task<string> GeneratePhoneOtpAsync(string countryCode, string phoneNumber);
    Task<bool> VerifyPhoneOtpAsync(string phoneNumber, string otpCode);
    Task SendOtpSmsAsync(string fullPhoneNumber, string otpCode);
}
