using Microsoft.EntityFrameworkCore;
using Qalam.Data.Entity.Identity;
using Qalam.Infrastructure.Abstracts;
using Qalam.Infrastructure.context;

namespace Qalam.Infrastructure.Repositories;

public class LoginOtpRepository : ILoginOtpRepository
{
    private readonly ApplicationDBContext _context;

    public LoginOtpRepository(ApplicationDBContext context)
    {
        _context = context;
    }

    public async Task<LoginOtp?> GetValidOtpAsync(string phoneNumber, string otpCode, CancellationToken cancellationToken = default)
    {
        return await _context.LoginOtps
            .Where(o => o.PhoneNumber == phoneNumber
                        && o.OtpCode == otpCode
                        && !o.IsUsed
                        && o.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> HasValidOtpAsync(string phoneNumber, CancellationToken cancellationToken = default)
    {
        return await _context.LoginOtps
            .AnyAsync(o => o.PhoneNumber == phoneNumber
                           && !o.IsUsed
                           && o.ExpiresAt > DateTime.UtcNow, cancellationToken);
    }

    public async Task<bool> HasRecentOtpAsync(string phoneNumber, int cooldownSeconds, CancellationToken cancellationToken = default)
    {
        if (cooldownSeconds <= 0) return false;
        var cutoff = DateTime.UtcNow.AddSeconds(-cooldownSeconds);
        return await _context.LoginOtps
            .AnyAsync(o => o.PhoneNumber == phoneNumber
                           && !o.IsUsed
                           && o.CreatedAt > cutoff, cancellationToken);
    }

    public async Task RemoveExpiredOtpsAsync(string phoneNumber, CancellationToken cancellationToken = default)
    {
        var expired = await _context.LoginOtps
            .Where(o => o.PhoneNumber == phoneNumber && (o.IsUsed || o.ExpiresAt < DateTime.UtcNow))
            .ToListAsync(cancellationToken);
        _context.LoginOtps.RemoveRange(expired);
    }

    public async Task MarkAsUsedAsync(int otpId, int? userId, CancellationToken cancellationToken = default)
    {
        var otp = await _context.LoginOtps.FindAsync(new object[] { otpId }, cancellationToken);
        if (otp == null) return;
        otp.IsUsed = true;
        otp.UsedAt = DateTime.UtcNow;
        otp.UserId = userId;
        _context.LoginOtps.Update(otp);
    }

    public async Task<LoginOtp> AddAsync(LoginOtp otp, CancellationToken cancellationToken = default)
    {
        await _context.LoginOtps.AddAsync(otp, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return otp;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);
}
