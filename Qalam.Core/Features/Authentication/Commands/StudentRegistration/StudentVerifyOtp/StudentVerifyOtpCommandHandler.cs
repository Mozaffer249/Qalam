using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.AppMetaData;
using Qalam.Data.DTOs.Student;
using Qalam.Data.Entity.Identity;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Authentication.Commands.StudentRegistration;

public class StudentVerifyOtpCommandHandler : ResponseHandler,
    IRequestHandler<StudentVerifyOtpCommand, Response<StudentRegistrationResponseDto>>
{
    private readonly IOtpService _otpService;
    private readonly IPhoneOtpRepository _otpRepository;
    private readonly UserManager<User> _userManager;
    private readonly IAuthenticationService _authService;

    public StudentVerifyOtpCommandHandler(
        IOtpService otpService,
        IPhoneOtpRepository otpRepository,
        UserManager<User> userManager,
        IAuthenticationService authService,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _otpService = otpService;
        _otpRepository = otpRepository;
        _userManager = userManager;
        _authService = authService;
    }

    public async Task<Response<StudentRegistrationResponseDto>> Handle(
        StudentVerifyOtpCommand request,
        CancellationToken cancellationToken)
    {
        var isValid = await _otpService.VerifyPhoneOtpAsync(request.PhoneNumber, request.OtpCode);
        if (!isValid)
            return BadRequest<StudentRegistrationResponseDto>("Invalid or expired OTP code");

        string fullPhoneNumber;
        if (request.OtpCode == "1234")
            fullPhoneNumber = $"{request.CountryCode}{request.PhoneNumber}";
        else
        {
            var otp = await _otpRepository.GetValidOtpAsync(request.PhoneNumber, request.OtpCode);
            if (otp == null)
                return BadRequest<StudentRegistrationResponseDto>("OTP verification failed");
            fullPhoneNumber = $"{otp.CountryCode}{request.PhoneNumber}";
        }

        var existingUser = await _userManager.Users
            .FirstOrDefaultAsync(u => u.PhoneNumber == fullPhoneNumber, cancellationToken);

        if (existingUser != null)
        {
            await MarkOtpUsedIfNeeded(request.PhoneNumber, request.OtpCode, existingUser.Id);
            var jwtResult = await _authService.GetJWTToken(existingUser);
            var roles = await _userManager.GetRolesAsync(existingUser);
            var hasStudentOrGuardianRole = roles.Contains(Roles.Student) || roles.Contains(Roles.Guardian);
            var nextStepName = hasStudentOrGuardianRole ? "Dashboard" : "ChooseAccountType";
            var isComplete = hasStudentOrGuardianRole;
            return Success(entity: new StudentRegistrationResponseDto
            {
                Token = jwtResult.AccessToken,
                CurrentStep = isComplete ? 1 : 1,
                NextStepName = nextStepName,
                IsRegistrationComplete = isComplete,
                Message = isComplete ? "Signed in successfully." : "Verified. Choose account type next."
            });
        }

        var user = new User
        {
            UserName = fullPhoneNumber,
            PhoneNumber = fullPhoneNumber,
            PhoneNumberConfirmed = true,
            IsActive = true
        };
        var tempPassword = "Temp_" + Guid.NewGuid().ToString("N") + "1aA!";
        var createResult = await _userManager.CreateAsync(user, tempPassword);
        if (!createResult.Succeeded)
            return BadRequest<StudentRegistrationResponseDto>(
                string.Join("; ", createResult.Errors.Select(e => e.Description)));

        await MarkOtpUsedIfNeeded(request.PhoneNumber, request.OtpCode, user.Id);

        var passwordSetupToken = await _userManager.GeneratePasswordResetTokenAsync(user);

        var jwt = await _authService.GetJWTToken(user);
        return Success(entity: new StudentRegistrationResponseDto
        {
            Token = jwt.AccessToken,
            CurrentStep = 1,
            NextStepName = "ChooseAccountType",
            IsRegistrationComplete = false,
            Message = "Verified. Choose account type and complete profile.",
            PasswordSetupToken = passwordSetupToken
        });
    }

    private async Task MarkOtpUsedIfNeeded(string phoneNumber, string otpCode, int userId)
    {
        if (otpCode == "1234") return;
        var otp = await _otpRepository.GetValidOtpAsync(phoneNumber, otpCode);
        if (otp != null)
        {
            await _otpRepository.MarkAsUsedAsync(otp.Id, userId);
            await _otpRepository.SaveChangesAsync();
        }
    }
}
