using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Identity;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Authentication.Commands.VerifyOtpAndCreateAccount;

public class VerifyOtpAndCreateAccountCommandHandler : ResponseHandler,
    IRequestHandler<VerifyOtpAndCreateAccountCommand, Response<object>>
{
    private readonly IOtpService _otpService;
    private readonly ITeacherRegistrationService _teacherRegistrationService;
    private readonly IPhoneOtpRepository _otpRepository;
    private readonly UserManager<User> _userManager;
    private readonly ITeacherRepository _teacherRepository;
    private readonly IAuthenticationService _authService;

    public VerifyOtpAndCreateAccountCommandHandler(
        IOtpService otpService,
        ITeacherRegistrationService teacherRegistrationService,
        IPhoneOtpRepository otpRepository,
        UserManager<User> userManager,
        ITeacherRepository teacherRepository,
        IAuthenticationService authService,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _otpService = otpService;
        _teacherRegistrationService = teacherRegistrationService;
        _otpRepository = otpRepository;
        _userManager = userManager;
        _teacherRepository = teacherRepository;
        _authService = authService;
    }

    public async Task<Response<object>> Handle(
        VerifyOtpAndCreateAccountCommand request,
        CancellationToken cancellationToken)
    {
        // Verify OTP
        var isValid = await _otpService.VerifyPhoneOtpAsync(
            request.PhoneNumber,
            request.OtpCode);

        if (!isValid)
        {
            return BadRequest<object>("Invalid or expired OTP code");
        }

        string fullPhoneNumber;
        
        // For test OTP, use default country code (no DB record exists)
        if (request.OtpCode == "1234")
        {
            fullPhoneNumber = $"+966{request.PhoneNumber}";
        }
        else
        {
            // Get the OTP record to get country code
            var otp = await _otpRepository.GetValidOtpAsync(request.PhoneNumber, request.OtpCode);
            if (otp == null)
            {
                return BadRequest<object>("OTP verification failed");
            }
            fullPhoneNumber = $"{otp.CountryCode}{request.PhoneNumber}";
            
            // Mark OTP as used (will be done after account creation/login)
        }

        // Check if user already exists
        var existingUser = await _userManager.Users
            .FirstOrDefaultAsync(u => u.PhoneNumber == fullPhoneNumber, cancellationToken);

        if (existingUser != null)
        {
            // LOGIN flow - existing user
            var teacher = await _teacherRepository.GetByUserIdAsync(existingUser.Id);
            
            if (teacher?.Status == TeacherStatus.Blocked)
            {
                return BadRequest<object>("Your account has been blocked. Please contact support.");
            }

            // Generate JWT token for existing user
            var jwtResult = await _authService.GetJWTToken(existingUser);
            
            // Get next registration step
            var nextStep = await _teacherRegistrationService.GetNextRegistrationStepAsync(existingUser.Id);

            // Mark OTP as used (if not test OTP)
            if (request.OtpCode != "1234")
            {
                var otp = await _otpRepository.GetValidOtpAsync(request.PhoneNumber, request.OtpCode);
                if (otp != null)
                {
                    await _otpRepository.MarkAsUsedAsync(otp.Id, existingUser.Id);
                    await _otpRepository.SaveChangesAsync();
                }
            }

            return Success<object>(entity: new
            {
                Token = jwtResult.AccessToken,
                IsNewUser = false,
                NextStep = nextStep
            });
        }
        else
        {
            // REGISTER flow - new user
            var result = await _teacherRegistrationService.CreateBasicAccountAsync(fullPhoneNumber);
            
            // Get next registration step
            var nextStep = await _teacherRegistrationService.GetNextRegistrationStepAsync(result.UserId);

            // Mark OTP as used (if not test OTP)
            if (request.OtpCode != "1234")
            {
                var otp = await _otpRepository.GetValidOtpAsync(request.PhoneNumber, request.OtpCode);
                if (otp != null)
                {
                    await _otpRepository.MarkAsUsedAsync(otp.Id, result.UserId);
                    await _otpRepository.SaveChangesAsync();
                }
            }

            return Success<object>(entity: new
            {
                Token = result.Token,
                IsNewUser = true,
                NextStep = nextStep
            });
        }
    }
}
