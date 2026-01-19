using Qalam.Data.Entity.Identity;

namespace Qalam.Infrastructure.Abstracts;

public interface IPhoneOtpRepository
{
    Task<PhoneConfirmationOtp?> GetValidOtpAsync(string phoneNumber, string otpCode);
    Task AddAsync(PhoneConfirmationOtp otp);
    Task RemoveExpiredOtpsAsync(string phoneNumber);
    Task MarkAsUsedAsync(int otpId, int userId);
    Task SaveChangesAsync();
}
