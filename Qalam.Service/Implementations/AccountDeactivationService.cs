using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Qalam.Data.DTOs.Account;
using Qalam.Data.Entity.Identity;
using Qalam.Infrastructure.Abstracts;
using Qalam.Infrastructure.context;
using Qalam.Service.Abstracts;

namespace Qalam.Service.Implementations;

public class AccountDeactivationService : IAccountDeactivationService
{
    private readonly UserManager<User> _userManager;
    private readonly ApplicationDBContext _db;
    private readonly IAccountDeactivationGuardService _guard;
    private readonly IAuthenticationService _authenticationService;
    private readonly IUserDeviceTokenRepository _deviceTokens;
    private readonly IAuditService _auditService;
    private readonly ILogger<AccountDeactivationService> _logger;

    public AccountDeactivationService(
        UserManager<User> userManager,
        ApplicationDBContext db,
        IAccountDeactivationGuardService guard,
        IAuthenticationService authenticationService,
        IUserDeviceTokenRepository deviceTokens,
        IAuditService auditService,
        ILogger<AccountDeactivationService> logger)
    {
        _userManager = userManager;
        _db = db;
        _guard = guard;
        _authenticationService = authenticationService;
        _deviceTokens = deviceTokens;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<(bool Success, string Message, IReadOnlyList<string>? BlockingReasons)> DeactivateAsync(
        int userId,
        string password,
        string? accessToken,
        string? refreshToken,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
            return (false, "User not found", null);

        var roles = await _userManager.GetRolesAsync(user);
        var isStudentOrGuardian = roles.Contains(Data.AppMetaData.Roles.Student)
            || roles.Contains(Data.AppMetaData.Roles.Guardian);
        if (!isStudentOrGuardian)
            return (false, "Only student or guardian accounts can be deactivated from this endpoint", null);

        if (!await _userManager.CheckPasswordAsync(user, password))
            return (false, "Password is incorrect", null);

        var blocking = await _guard.GetBlockingReasonsAsync(userId, cancellationToken);
        if (blocking.Count > 0)
        {
            return (
                false,
                "Cannot deactivate account while you have active enrollments, open session requests, or pending invitations",
                blocking);
        }

        user.IsActive = false;
        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
            return (false, "Failed to deactivate account", null);

        var student = await _db.Students.FirstOrDefaultAsync(s => s.UserId == userId, cancellationToken);
        if (student != null)
        {
            student.IsActive = false;
            student.UpdatedAt = DateTime.UtcNow;
        }

        var guardian = await _db.Guardians.FirstOrDefaultAsync(g => g.UserId == userId, cancellationToken);
        if (guardian != null)
        {
            guardian.IsActive = false;
            guardian.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(cancellationToken);

        await _deviceTokens.DeactivateAllForUserAsync(userId, cancellationToken);

        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            try
            {
                await _authenticationService.RevokeTokenAsync(accessToken, refreshToken, userId, allDevices: true);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Token revoke failed during account deactivation for user {UserId}", userId);
            }
        }

        try
        {
            await _auditService.LogSecurityEventAsync(
                userId,
                SecurityEventType.AccountDeactivated,
                ipAddress ?? "unknown",
                "Account soft-deactivated by user");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Security event log failed for user {UserId}", userId);
        }

        return (true, "Account deactivated successfully", null);
    }
}
