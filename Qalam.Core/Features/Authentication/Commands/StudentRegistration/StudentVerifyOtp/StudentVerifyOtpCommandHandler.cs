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
    private readonly ILoginOtpRepository _loginOtpRepository;
    private readonly UserManager<User> _userManager;
    private readonly IAuthenticationService _authService;
    private readonly IAuthSettingsProvider _authSettingsProvider;
    private readonly IAuthLoginOtpHelper _authLoginOtpHelper;

    public StudentVerifyOtpCommandHandler(
        IOtpService otpService,
        ILoginOtpRepository loginOtpRepository,
        UserManager<User> userManager,
        IAuthenticationService authService,
        IAuthSettingsProvider authSettingsProvider,
        IAuthLoginOtpHelper authLoginOtpHelper,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _otpService = otpService;
        _loginOtpRepository = loginOtpRepository;
        _userManager = userManager;
        _authService = authService;
        _authSettingsProvider = authSettingsProvider;
        _authLoginOtpHelper = authLoginOtpHelper;
    }

    public async Task<Response<StudentRegistrationResponseDto>> Handle(
        StudentVerifyOtpCommand request,
        CancellationToken cancellationToken)
    {
        var settings = await _authSettingsProvider.GetSettingsAsync(cancellationToken);
        // Read ASPNETCORE_ENVIRONMENT directly so this assembly doesn't have to depend on
        // Microsoft.Extensions.Hosting. Avoids cross-assembly IHostEnvironment type-identity
        // problems that broke DI activation under transitive package-version drift.
        var envName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
        var allowTest = settings.Otp.AllowTestCodeInDevelopment
                        && (envName == "Development" || envName == "Staging");

        var isValid = await _otpService.VerifyLoginOtpAsync(request.PhoneNumber, request.OtpCode, allowTest, cancellationToken);
        if (!isValid)
            return BadRequest<StudentRegistrationResponseDto>("Invalid or expired OTP code");

        LoginOtp? loginOtp;
        if (request.OtpCode == IOtpService.TestOtpCode && allowTest)
        {
            loginOtp = await _loginOtpRepository.GetLatestValidOtpForPhoneAsync(
                request.PhoneNumber, cancellationToken);
        }
        else
        {
            loginOtp = await _otpService.GetValidLoginOtpAsync(
                request.PhoneNumber, request.OtpCode, cancellationToken);
        }

        var countryCode = loginOtp?.CountryCode ?? "+966";
        var fullPhoneNumber = $"{countryCode}{request.PhoneNumber}";

        var existingUser = await _userManager.Users
            .FirstOrDefaultAsync(u => u.PhoneNumber == fullPhoneNumber, cancellationToken);

        if (existingUser != null)
        {
            await MarkLoginOtpUsedIfNeeded(loginOtp, existingUser.Id, cancellationToken);
            var jwtResult = await _authService.GetJWTToken(existingUser);
            var roles = await _userManager.GetRolesAsync(existingUser);
            var hasStudentOrGuardianRole = roles.Contains(Roles.Student) || roles.Contains(Roles.Guardian);

            if (hasStudentOrGuardianRole)
            {
                return Success(entity: new StudentRegistrationResponseDto
                {
                    Token = jwtResult.AccessToken,
                    CurrentStep = 1,
                    Roles = new List<string>(roles),
                    NextStepName = "Dashboard",
                    IsNextStepRequired = false,
                    OptionalSteps = new List<string>(),
                    NextStepDescription = "Welcome back!",
                    IsRegistrationComplete = true,
                    Message = "Signed in successfully."
                });
            }

            return Success(entity: new StudentRegistrationResponseDto
            {
                Token = jwtResult.AccessToken,
                CurrentStep = 1,
                Roles = new List<string>(roles),
                NextStepName = "ChooseAccountType",
                IsNextStepRequired = true,
                OptionalSteps = new List<string>(),
                NextStepDescription = "Choose account type to add student/parent capabilities.",
                IsRegistrationComplete = false,
                Message = "Verified. Choose account type to complete profile."
            });
        }

        var accountEmail = _authLoginOtpHelper.ResolveAccountEmail(
            _authLoginOtpHelper.ResolveRegistrationEmail(loginOtp),
            fullPhoneNumber);

        var user = new User
        {
            UserName = fullPhoneNumber,
            PhoneNumber = fullPhoneNumber,
            PhoneNumberConfirmed = true,
            Email = accountEmail,
            NormalizedEmail = accountEmail.ToUpperInvariant(),
            IsActive = true,
            EmailConfirmed = false
        };
        var tempPassword = "Temp_" + Guid.NewGuid().ToString("N") + "1aA!";
        var createResult = await _userManager.CreateAsync(user, tempPassword);
        if (!createResult.Succeeded)
        {
            return BadRequest<StudentRegistrationResponseDto>(
                string.Join("; ", createResult.Errors.Select(e => e.Description)));
        }

        await MarkLoginOtpUsedIfNeeded(loginOtp, user.Id, cancellationToken);

        var jwt = await _authService.GetJWTToken(user);
        return Success(entity: new StudentRegistrationResponseDto
        {
            Token = jwt.AccessToken,
            CurrentStep = 1,
            Roles = new List<string>(),
            NextStepName = "ChooseAccountType",
            IsNextStepRequired = true,
            OptionalSteps = new List<string>(),
            NextStepDescription = "Choose your account type and complete profile.",
            IsRegistrationComplete = false,
            Message = string.IsNullOrEmpty(user.Email)
                ? "Verified. Choose account type and complete profile."
                : "Verified. Choose account type (email already on file)."
        });
    }

    private async Task MarkLoginOtpUsedIfNeeded(LoginOtp? loginOtp, int userId, CancellationToken cancellationToken)
    {
        if (loginOtp == null) return;
        await _otpService.MarkLoginOtpUsedAsync(loginOtp.Id, userId, cancellationToken);
        await _loginOtpRepository.SaveChangesAsync(cancellationToken);
    }
}
