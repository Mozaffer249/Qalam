using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Authentication;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Teacher;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Identity;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Authentication.Commands.VerifyOtpAndCreateAccount;

public class VerifyOtpAndCreateAccountCommandHandler : ResponseHandler,
    IRequestHandler<VerifyOtpAndCreateAccountCommand, Response<PhoneVerificationResponseDto>>
{
    private readonly IOtpService _otpService;
    private readonly ITeacherRegistrationService _teacherRegistrationService;
    private readonly IPhoneOtpRepository _otpRepository;
    private readonly UserManager<User> _userManager;
    private readonly IAuthenticationService _authService;
    private readonly ITeacherRepository _teacherRepository;
    private readonly IStringLocalizer<AuthenticationResources> _authLocalizer;

    public VerifyOtpAndCreateAccountCommandHandler(
        IOtpService otpService,
        ITeacherRegistrationService teacherRegistrationService,
        IPhoneOtpRepository otpRepository,
        UserManager<User> userManager,
        IAuthenticationService authService,
        ITeacherRepository teacherRepository,
        IStringLocalizer<SharedResources> localizer,
        IStringLocalizer<AuthenticationResources> authLocalizer) : base(localizer)
    {
        _otpService = otpService;
        _teacherRegistrationService = teacherRegistrationService;
        _otpRepository = otpRepository;
        _userManager = userManager;
        _authService = authService;
        _teacherRepository = teacherRepository;
        _authLocalizer = authLocalizer;
    }

    public async Task<Response<PhoneVerificationResponseDto>> Handle(
        VerifyOtpAndCreateAccountCommand request,
        CancellationToken cancellationToken)
    {
        // Verify OTP
        var isValid = await _otpService.VerifyPhoneOtpAsync(
            request.PhoneNumber,
            request.OtpCode);

        if (!isValid)
        {
            return BadRequest<PhoneVerificationResponseDto>("Invalid or expired OTP code");
        }

        string fullPhoneNumber;

        // For test OTP, use default country code
        if (request.OtpCode == "1234")
        {
            fullPhoneNumber = $"+966{request.PhoneNumber}";
        }
        else
        {
            // Get the OTP record
            var otp = await _otpRepository.GetValidOtpAsync(request.PhoneNumber, request.OtpCode);
            if (otp == null)
            {
                return BadRequest<PhoneVerificationResponseDto>("OTP verification failed");
            }
            fullPhoneNumber = $"{otp.CountryCode}{request.PhoneNumber}";
        }

        // Check if user already exists
        var existingUser = await _userManager.FindByNameAsync(fullPhoneNumber);

        if (existingUser != null)
        {
            // USER EXISTS - THIS IS LOGIN
            
            // Check if teacher is blocked BEFORE issuing token
            var teacher = await _teacherRepository.GetByUserIdAsync(existingUser.Id);
            if (teacher != null && teacher.Status == TeacherStatus.Blocked)
            {
                return Unauthorized<PhoneVerificationResponseDto>(
                    _authLocalizer[AuthenticationResourcesKeys.AccountBlocked]);
            }
            
            // User exists - generate new token directly
            var jwtResult = await _authService.GetJWTToken(existingUser);

            // Get their registration status
            var registrationStep = await _teacherRegistrationService
                .GetNextRegistrationStepAsync(existingUser.Id);

            return Success(entity: new PhoneVerificationResponseDto
            {
                Token = jwtResult.AccessToken,
                NextStep = registrationStep
            });
        }

        // Create new account
        var result = await _teacherRegistrationService.CreateBasicAccountAsync(fullPhoneNumber);

        // Mark OTP as used (skip for test OTP)
        if (request.OtpCode != "1234")
        {
            var otp = await _otpRepository.GetValidOtpAsync(request.PhoneNumber, request.OtpCode);
            if (otp != null)
            {
                await _otpRepository.MarkAsUsedAsync(otp.Id, result.UserId);
                await _otpRepository.SaveChangesAsync();
            }
        }

        // Get next step (should be Step 3 for new user)
        var nextStep = await _teacherRegistrationService
            .GetNextRegistrationStepAsync(result.UserId);

        return Success(entity: new PhoneVerificationResponseDto
        {
            Token = result.Token,
            NextStep = nextStep
        });
    }
}
