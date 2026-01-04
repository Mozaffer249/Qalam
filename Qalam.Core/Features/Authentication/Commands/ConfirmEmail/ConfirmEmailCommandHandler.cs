using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Authentication;
using Qalam.Core.Resources.Shared;
using Qalam.Data.Entity.Identity;
using Qalam.Infrastructure.context;

namespace Qalam.Core.Features.Authentication.Commands.ConfirmEmail
{
    public class ConfirmEmailCommandHandler : ResponseHandler, IRequestHandler<ConfirmEmailCommand, Response<string>>
    {
        private readonly UserManager<User> _userManager;
        private readonly ApplicationDBContext _context;
        private readonly IStringLocalizer<AuthenticationResources> _authLocalizer;
        private readonly IStringLocalizer<SharedResources> _sharedLocalizer;

        public ConfirmEmailCommandHandler(
            IStringLocalizer<SharedResources> sharedLocalizer,
            IStringLocalizer<AuthenticationResources> authLocalizer,
            UserManager<User> userManager,
            ApplicationDBContext context) : base(sharedLocalizer)
        {
            _userManager = userManager;
            _context = context;
            _authLocalizer = authLocalizer;
            _sharedLocalizer = sharedLocalizer;
        }

        public async Task<Response<string>> Handle(ConfirmEmailCommand request, CancellationToken cancellationToken)
        {
            // Find user
            var user = await _userManager.FindByIdAsync(request.UserId.ToString());
            if (user == null)
            {
                return NotFound<string>(_authLocalizer[AuthenticationResourcesKeys.UserNotFound]);
            }

            // Check if already confirmed
            if (user.EmailConfirmed)
            {
                return BadRequest<string>("Email is already confirmed.");
            }

            // Find OTP in database
            var otp = await _context.EmailConfirmationOtps
                .Where(o => o.UserId == request.UserId
                         && o.OtpCode == request.Code
                         && !o.IsUsed)
                .FirstOrDefaultAsync(cancellationToken);

            if (otp == null)
            {
                return BadRequest<string>("Invalid OTP code.");
            }

            // Check if expired
            if (otp.ExpiresAt < DateTime.UtcNow)
            {
                return BadRequest<string>("OTP code has expired. Please request a new one.");
            }

            // Mark OTP as used
            otp.IsUsed = true;
            otp.UsedAt = DateTime.UtcNow;
            _context.EmailConfirmationOtps.Update(otp);

            // Confirm email and activate user
            user.EmailConfirmed = true;
            user.IsActive = true;
            await _userManager.UpdateAsync(user);

            await _context.SaveChangesAsync(cancellationToken);

            return Success<string>("Email confirmed successfully. You can now login.");
        }
    }
}

