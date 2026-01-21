using Microsoft.EntityFrameworkCore;
using Qalam.Data.Entity.Identity;
using Qalam.Infrastructure.Abstracts;
using Qalam.Infrastructure.context;
using Qalam.Infrastructure.InfrastructureBases;

namespace Qalam.Infrastructure.Repositories;

public class PhoneOtpRepository : GenericRepositoryAsync<PhoneConfirmationOtp>, IPhoneOtpRepository
{
    public PhoneOtpRepository(ApplicationDBContext context) : base(context)
    {
    }

    public async Task<PhoneConfirmationOtp?> GetValidOtpAsync(string phoneNumber, string otpCode)
    {
        return await _dbContext.PhoneConfirmationOtps
            .Where(o => o.PhoneNumber == phoneNumber
                     && o.OtpCode == otpCode
                     && !o.IsUsed
                     && o.ExpiresAt > DateTime.UtcNow)
            .FirstOrDefaultAsync();
    }

    public async Task<bool> HasValidOtpAsync(string phoneNumber)
    {
        return await _dbContext.PhoneConfirmationOtps
            .AnyAsync(o => o.PhoneNumber == phoneNumber
                        && !o.IsUsed
                        && o.ExpiresAt > DateTime.UtcNow);
    }

    public async Task RemoveExpiredOtpsAsync(string phoneNumber)
    {
        var expiredOtps = await _dbContext.PhoneConfirmationOtps
            .Where(o => o.PhoneNumber == phoneNumber 
                     && (o.IsUsed || o.ExpiresAt < DateTime.UtcNow))
            .ToListAsync();
        
        _dbContext.PhoneConfirmationOtps.RemoveRange(expiredOtps);
    }

    public async Task MarkAsUsedAsync(int otpId, int userId)
    {
        var otp = await _dbContext.PhoneConfirmationOtps.FindAsync(otpId);
        if (otp != null)
        {
            otp.IsUsed = true;
            otp.UsedAt = DateTime.UtcNow;
            otp.UserId = userId;
            _dbContext.PhoneConfirmationOtps.Update(otp);
        }
    }
}
