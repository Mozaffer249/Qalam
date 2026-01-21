using Qalam.Data.Entity.Identity;
using Qalam.Infrastructure.InfrastructureBases;

namespace Qalam.Infrastructure.Abstracts;

public interface IPhoneOtpRepository : IGenericRepositoryAsync<PhoneConfirmationOtp>
{
    Task<PhoneConfirmationOtp?> GetValidOtpAsync(string phoneNumber, string otpCode);
    Task<bool> HasValidOtpAsync(string phoneNumber);
    Task RemoveExpiredOtpsAsync(string phoneNumber);
    Task MarkAsUsedAsync(int otpId, int userId);
}
