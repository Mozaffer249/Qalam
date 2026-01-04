using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Authentication;
using Qalam.Data.Entity.Identity;
using Qalam.Service.Abstracts;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace Qalam.Core.Features.Authentication.Commands.ChangePassword
{
    public class ChangePasswordCommandHandler : ResponseHandler, IRequestHandler<ChangePasswordCommand, Response<string>>
    {
        private readonly UserManager<User> _userManager;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IStringLocalizer<AuthenticationResources> _authLocalizer;
        private readonly ISecurityNotificationService _notificationService;
        private readonly IPasswordSecurityService _passwordSecurityService;
        private readonly IAuditService _auditService;

        public ChangePasswordCommandHandler(
            UserManager<User> userManager,
            IHttpContextAccessor httpContextAccessor,
            IStringLocalizer<AuthenticationResources> authLocalizer,
            ISecurityNotificationService notificationService,
            IPasswordSecurityService passwordSecurityService,
            IAuditService auditService) : base(authLocalizer)
        {
            _userManager = userManager;
            _httpContextAccessor = httpContextAccessor;
            _authLocalizer = authLocalizer;
            _notificationService = notificationService;
            _passwordSecurityService = passwordSecurityService;
            _auditService = auditService;
        }

        public async Task<Response<string>> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
        {
            // Get current user from HttpContext
            var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? _httpContextAccessor.HttpContext?.User.FindFirst("uid")?.Value;

            if (string.IsNullOrEmpty(userIdClaim))
            {
                return Unauthorized<string>(_authLocalizer[AuthenticationResourcesKeys.UserNotFound]);
            }

            var user = await _userManager.FindByIdAsync(userIdClaim);
            if (user == null)
            {
                return NotFound<string>(_authLocalizer[AuthenticationResourcesKeys.UserNotFound]);
            }

            // Verify current password
            var isValidPassword = await _userManager.CheckPasswordAsync(user, request.CurrentPassword);
            if (!isValidPassword)
            {
                return BadRequest<string>(_authLocalizer[AuthenticationResourcesKeys.PasswordNotCorrect]);
            }

            // Check if password is in recent history
            var isReused = await _passwordSecurityService.IsPasswordInHistoryAsync(user.Id, request.NewPassword, historyCount: 5);
            if (isReused)
            {
                return BadRequest<string>("Cannot reuse one of your last 5 passwords. Please choose a different password.");
            }

            // Check password strength (optional, already validated by Identity)
            var strength = await _passwordSecurityService.CheckPasswordStrengthAsync(request.NewPassword);
            if (strength.Score < 2)
            {
                return BadRequest<string>($"Password is too weak. {string.Join(", ", strength.Feedback)}");
            }

            // Change password
            var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return BadRequest<string>(errors);
            }

            // Add current password to history
            var currentPasswordHash = user.PasswordHash;
            if (!string.IsNullOrEmpty(currentPasswordHash))
            {
                await _passwordSecurityService.AddToPasswordHistoryAsync(user.Id, currentPasswordHash);
            }

            // Update PasswordChangedAt timestamp
            user.PasswordChangedAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            // Log security event
            var ipAddress = _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString() ?? "Unknown";
            await _auditService.LogSecurityEventAsync(
                userId: user.Id,
                eventType: SecurityEventType.PasswordChanged,
                ipAddress: ipAddress,
                details: "Password changed via ChangePassword endpoint"
            );

            // Send security notification
            await _notificationService.NotifyPasswordChangedAsync(user, ipAddress);

            return Success<string>(_authLocalizer[AuthenticationResourcesKeys.PasswordChangedSuccessfully]);
        }
    }
}

