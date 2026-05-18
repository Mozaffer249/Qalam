using Qalam.Data.Entity.Identity;
using Qalam.Service.Models;

namespace Qalam.Service.Abstracts;

public interface IOtpService
{
    const string TestOtpCode = "1234";

    Task<string?> GeneratePhoneOtpAsync(string countryCode, string phoneNumber);
    Task<bool> VerifyPhoneOtpAsync(string phoneNumber, string otpCode);
    Task SendOtpSmsAsync(string fullPhoneNumber, string otpCode);

    /// <summary>Generate login OTP, persist to LoginOtps, and send via configured channel.</summary>
    Task<LoginOtpSendResult?> GenerateAndSendLoginOtpAsync(LoginOtpSendOptions options, CancellationToken cancellationToken = default);

    Task<bool> VerifyLoginOtpAsync(string phoneNumber, string otpCode, bool allowTestCode, CancellationToken cancellationToken = default);

    Task<LoginOtp?> GetValidLoginOtpAsync(string phoneNumber, string otpCode, CancellationToken cancellationToken = default);

    Task MarkLoginOtpUsedAsync(int otpId, int? userId, CancellationToken cancellationToken = default);
}
