using Qalam.Data.Entity.Identity;

namespace Qalam.Infrastructure.Abstracts;

public interface ILoginOtpRepository
{
    Task<LoginOtp?> GetValidOtpAsync(string phoneNumber, string otpCode, CancellationToken cancellationToken = default);
    Task<bool> HasValidOtpAsync(string phoneNumber, CancellationToken cancellationToken = default);
    /// <summary>True if an OTP was issued for <paramref name="phoneNumber"/> within the last <paramref name="cooldownSeconds"/> seconds. Used to throttle resends independently of OTP validity.</summary>
    Task<bool> HasRecentOtpAsync(string phoneNumber, int cooldownSeconds, CancellationToken cancellationToken = default);
    Task RemoveExpiredOtpsAsync(string phoneNumber, CancellationToken cancellationToken = default);
    Task MarkAsUsedAsync(int otpId, int? userId, CancellationToken cancellationToken = default);
    Task<LoginOtp> AddAsync(LoginOtp otp, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
