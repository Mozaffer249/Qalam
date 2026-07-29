using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Authentication;
using Qalam.Core.Resources.Shared;
using Qalam.Data.AppMetaData;
using Qalam.Data.Entity.Identity;
using Qalam.Infrastructure.context;
using Qalam.Service.Abstracts;
using Qalam.Service.Models;

namespace Qalam.Core.Features.Authentication.Commands.ResetPassword
{
    public class ResetPasswordCommandHandler : ResponseHandler, IRequestHandler<ResetPasswordCommand, Response<string>>
    {
        /// <summary>Fixed reset code accepted only in Development / Staging.</summary>
        public const string TestResetCode = "123456";

        private readonly UserManager<User> _userManager;
        private readonly ApplicationDBContext _context;
        private readonly IStringLocalizer<AuthenticationResources> _authLocalizer;
        private readonly IStringLocalizer<SharedResources> _sharedLocalizer;
        private readonly IPasswordSecurityService _passwordSecurityService;
        private readonly IAuditService _auditService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ISecurityNotificationService _notificationService;
        private readonly IOptions<SecuritySettings> _securitySettings;
        private readonly ILogger<ResetPasswordCommandHandler> _logger;

        public ResetPasswordCommandHandler(
            IStringLocalizer<SharedResources> sharedLocalizer,
            IStringLocalizer<AuthenticationResources> authLocalizer,
            UserManager<User> userManager,
            ApplicationDBContext context,
            IPasswordSecurityService passwordSecurityService,
            IAuditService auditService,
            IHttpContextAccessor httpContextAccessor,
            ISecurityNotificationService notificationService,
            IOptions<SecuritySettings> securitySettings,
            ILogger<ResetPasswordCommandHandler> logger) : base(sharedLocalizer)
        {
            _userManager = userManager;
            _context = context;
            _authLocalizer = authLocalizer;
            _sharedLocalizer = sharedLocalizer;
            _passwordSecurityService = passwordSecurityService;
            _auditService = auditService;
            _httpContextAccessor = httpContextAccessor;
            _notificationService = notificationService;
            _securitySettings = securitySettings;
            _logger = logger;
        }

        public async Task<Response<string>> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                return NotFound<string>(_authLocalizer[AuthenticationResourcesKeys.EmailIsNotExist]);
            }

            if (!user.IsActive)
            {
                return Unauthorized<string>(_authLocalizer[AuthenticationResourcesKeys.UserIsNotActive]);
            }

            if (request.RequireAdminRole)
            {
                var roles = await _userManager.GetRolesAsync(user);
                var isAdmin = roles.Contains(Roles.Admin) || roles.Contains(Roles.SuperAdmin);
                if (!isAdmin)
                {
                    return NotFound<string>(_authLocalizer[AuthenticationResourcesKeys.EmailIsNotExist]);
                }
            }

            var usedTestCode = IsNonProductionTestResetCode(request.ResetCode);
            PasswordResetOtp? otp = null;

            if (!usedTestCode)
            {
                otp = await _context.PasswordResetOtps
                    .Where(o => o.UserId == user.Id
                             && o.OtpCode == request.ResetCode
                             && !o.IsUsed)
                    .FirstOrDefaultAsync(cancellationToken);

                if (otp == null)
                {
                    return BadRequest<string>("Invalid reset code.");
                }

                if (otp.ExpiresAt < DateTime.UtcNow)
                {
                    return BadRequest<string>("Reset code has expired. Please request a new one.");
                }
            }
            else
            {
                _logger.LogWarning(
                    "Non-production test reset code {Code} accepted for user {UserId} ({Email}).",
                    TestResetCode, user.Id, user.Email);
            }

            var isReused = await _passwordSecurityService.IsPasswordInHistoryAsync(user.Id, request.NewPassword, historyCount: 5);
            if (isReused)
            {
                return BadRequest<string>("Cannot reuse one of your last 5 passwords. Please choose a different password.");
            }

            if (otp != null)
            {
                otp.IsUsed = true;
                otp.UsedAt = DateTime.UtcNow;
                _context.PasswordResetOtps.Update(otp);
            }

            var removePasswordResult = await _userManager.RemovePasswordAsync(user);
            if (!removePasswordResult.Succeeded)
            {
                return BadRequest<string>("Failed to reset password.");
            }

            var addPasswordResult = await _userManager.AddPasswordAsync(user, request.NewPassword);
            if (!addPasswordResult.Succeeded)
            {
                var errors = string.Join(", ", addPasswordResult.Errors.Select(e => e.Description));
                return BadRequest<string>(errors);
            }

            if (!string.IsNullOrEmpty(user.PasswordHash))
            {
                await _passwordSecurityService.AddToPasswordHistoryAsync(user.Id, user.PasswordHash);
            }

            user.PasswordChangedAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);
            await _userManager.ResetAccessFailedCountAsync(user);
            await _userManager.SetLockoutEndDateAsync(user, null);

            var ipAddress = _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString() ?? "Unknown";
            await _auditService.LogSecurityEventAsync(
                userId: user.Id,
                eventType: SecurityEventType.PasswordChanged,
                ipAddress: ipAddress,
                details: usedTestCode
                    ? "Admin password reset via non-production test code"
                    : request.RequireAdminRole
                        ? "Admin password reset via reset code"
                        : "Password reset via reset code"
            );

            if (_securitySettings.Value.EmailNotifications.Enabled &&
                _securitySettings.Value.EmailNotifications.NotifyOnPasswordChange)
            {
                await _notificationService.NotifyPasswordChangedAsync(user, ipAddress);
            }

            await _context.SaveChangesAsync(cancellationToken);

            return Success<string>("Password reset successfully. You can now login with your new password.");
        }

        private static bool IsNonProductionTestResetCode(string? resetCode)
        {
            if (!string.Equals(resetCode, TestResetCode, StringComparison.Ordinal))
                return false;

            var envName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
            return string.Equals(envName, "Development", StringComparison.OrdinalIgnoreCase)
                || string.Equals(envName, "Staging", StringComparison.OrdinalIgnoreCase);
        }
    }
}
