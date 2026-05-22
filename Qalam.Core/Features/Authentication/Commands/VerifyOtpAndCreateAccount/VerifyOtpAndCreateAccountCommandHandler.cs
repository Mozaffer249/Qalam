using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
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
    private readonly ILoginOtpRepository _loginOtpRepository;
    private readonly UserManager<User> _userManager;
    private readonly ITeacherRepository _teacherRepository;
    private readonly IAuthenticationService _authService;
    private readonly IAuthSettingsProvider _authSettingsProvider;
    private readonly IHostEnvironment _hostEnvironment;

    public VerifyOtpAndCreateAccountCommandHandler(
        IOtpService otpService,
        ITeacherRegistrationService teacherRegistrationService,
        ILoginOtpRepository loginOtpRepository,
        UserManager<User> userManager,
        ITeacherRepository teacherRepository,
        IAuthenticationService authService,
        IAuthSettingsProvider authSettingsProvider,
        IHostEnvironment hostEnvironment,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _otpService = otpService;
        _teacherRegistrationService = teacherRegistrationService;
        _loginOtpRepository = loginOtpRepository;
        _userManager = userManager;
        _teacherRepository = teacherRepository;
        _authService = authService;
        _authSettingsProvider = authSettingsProvider;
        _hostEnvironment = hostEnvironment;
    }

    public async Task<Response<object>> Handle(
        VerifyOtpAndCreateAccountCommand request,
        CancellationToken cancellationToken)
    {
        var settings = await _authSettingsProvider.GetSettingsAsync(cancellationToken);
        // Compare EnvironmentName as a string. Qalam.Core pins Microsoft.Extensions.Hosting.Abstractions
        // to 8.0.0 so IHostEnvironment can be DI-resolved, but the string comparison avoids depending
        // on the IsDevelopment()/IsStaging() extensions which can be sensitive to transitive versioning.
        var envName = _hostEnvironment.EnvironmentName;
        var allowTest = settings.Otp.AllowTestCodeInDevelopment
                        && (envName == "Development" || envName == "Staging");

        var isValid = await _otpService.VerifyLoginOtpAsync(request.PhoneNumber, request.OtpCode, allowTest, cancellationToken);
        if (!isValid)
            return BadRequest<object>("Invalid or expired OTP code");

        LoginOtp? loginOtp = null;
        if (request.OtpCode != IOtpService.TestOtpCode || !allowTest)
            loginOtp = await _otpService.GetValidLoginOtpAsync(request.PhoneNumber, request.OtpCode, cancellationToken);

        var countryCode = loginOtp?.CountryCode ?? "+966";
        var fullPhoneNumber = $"{countryCode}{request.PhoneNumber}";

        var existingUser = await _userManager.Users
            .FirstOrDefaultAsync(u => u.PhoneNumber == fullPhoneNumber, cancellationToken);

        if (existingUser != null)
        {
            var teacher = await _teacherRepository.GetByUserIdAsync(existingUser.Id);
            if (teacher?.Status == TeacherStatus.Blocked)
                return BadRequest<object>("Your account has been blocked. Please contact support.");

            var jwtResult = await _authService.GetJWTToken(existingUser);
            var nextStep = await _teacherRegistrationService.GetNextRegistrationStepAsync(existingUser.Id);

            if (loginOtp != null)
            {
                await _otpService.MarkLoginOtpUsedAsync(loginOtp.Id, existingUser.Id, cancellationToken);
                await _loginOtpRepository.SaveChangesAsync(cancellationToken);
            }

            return Success<object>(entity: new
            {
                Token = jwtResult.AccessToken,
                IsNewUser = false,
                NextStep = nextStep
            });
        }

        var pendingEmail = loginOtp?.PendingEmail;
        var result = await _teacherRegistrationService.CreateBasicAccountAsync(fullPhoneNumber, pendingEmail);
        var registerNextStep = await _teacherRegistrationService.GetNextRegistrationStepAsync(result.UserId);

        if (loginOtp != null)
        {
            await _otpService.MarkLoginOtpUsedAsync(loginOtp.Id, result.UserId, cancellationToken);
            await _loginOtpRepository.SaveChangesAsync(cancellationToken);
        }

        return Success<object>(entity: new
        {
            Token = result.Token,
            IsNewUser = true,
            NextStep = registerNextStep
        });
    }
}
