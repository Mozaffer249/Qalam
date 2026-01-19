using Microsoft.EntityFrameworkCore;
using Qalam.Data.Entity.Identity;
using Qalam.Infrastructure.Abstracts;
using Qalam.Infrastructure.context;

namespace Qalam.Infrastructure.Repositories;

public class PhoneOtpRepository : IPhoneOtpRepository
{
    private readonly ApplicationDBContext _context;

    public PhoneOtpRepository(ApplicationDBContext context)
    {
        _context = context;
    }

    public async Task<PhoneConfirmationOtp?> GetValidOtpAsync(string phoneNumber, string otpCode)
    {
        return await _context.PhoneConfirmationOtps
            .Where(o => o.PhoneNumber == phoneNumber
                     && o.OtpCode == otpCode
                     && !o.IsUsed
                     && o.ExpiresAt > DateTime.UtcNow)
            .FirstOrDefaultAsync();
    }

    public async Task AddAsync(PhoneConfirmationOtp otp)
    {
        await _context.PhoneConfirmationOtps.AddAsync(otp);
    }

    public async Task RemoveExpiredOtpsAsync(string phoneNumber)
    {
        var expiredOtps = await _context.PhoneConfirmationOtps
            .Where(o => o.PhoneNumber == phoneNumber 
                     && (o.IsUsed || o.ExpiresAt < DateTime.UtcNow))
            .ToListAsync();
        
        _context.PhoneConfirmationOtps.RemoveRange(expiredOtps);
    }

    public async Task MarkAsUsedAsync(int otpId, int userId)
    {
        var otp = await _context.PhoneConfirmationOtps.FindAsync(otpId);
        if (otp != null)
        {
            otp.IsUsed = true;
            otp.UsedAt = DateTime.UtcNow;
            otp.UserId = userId;
            _context.PhoneConfirmationOtps.Update(otp);
        }
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
