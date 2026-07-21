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
    private readonly ILoginOtpRepository _loginOtpRepository;
    private readonly UserManager<User> _userManager;
    private readonly ITeacherRepository _teacherRepository;
    private readonly IAuthenticationService _authService;
    private readonly IAuthSettingsProvider _authSettingsProvider;
    private readonly IAuthLoginOtpHelper _authLoginOtpHelper;
    private readonly ITeacherRegistrationCompletionService _completionService;

    public VerifyOtpAndCreateAccountCommandHandler(
        IOtpService otpService,
        ITeacherRegistrationService teacherRegistrationService,
        ILoginOtpRepository loginOtpRepository,
        UserManager<User> userManager,
        ITeacherRepository teacherRepository,
        IAuthenticationService authService,
        IAuthSettingsProvider authSettingsProvider,
        IAuthLoginOtpHelper authLoginOtpHelper,
        ITeacherRegistrationCompletionService completionService,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _otpService = otpService;
        _teacherRegistrationService = teacherRegistrationService;
        _loginOtpRepository = loginOtpRepository;
        _userManager = userManager;
        _teacherRepository = teacherRepository;
        _authService = authService;
        _authSettingsProvider = authSettingsProvider;
        _authLoginOtpHelper = authLoginOtpHelper;
        _completionService = completionService;
    }

    public async Task<Response<object>> Handle(
        VerifyOtpAndCreateAccountCommand request,
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
            return BadRequest<object>("Invalid or expired OTP code");

        LoginOtp? loginOtp;
        if (request.OtpCode == IOtpService.TestOtpCode && allowTest)
        {
            // Test bypass skips code match — still load the OTP row to recover PendingEmail.
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
            var teacher = await _teacherRepository.GetByUserIdAsync(existingUser.Id);
            if (teacher?.Status == TeacherStatus.Blocked)
                return BadRequest<object>("Your account has been blocked. Please contact support.");

            await _teacherRegistrationService.EnsureTeacherRoleForUserAsync(existingUser.Id);

            if (teacher != null && teacher.Status != TeacherStatus.Active)
                await _completionService.RefreshTeacherStatusAfterReviewAsync(teacher.Id, cancellationToken);

            if (request.AcceptedTerms && existingUser.TermsAcceptedAt == null)
            {
                existingUser.TermsAcceptedAt = DateTime.UtcNow;
                await _userManager.UpdateAsync(existingUser);
            }

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
                NextStep = nextStep,
                HasAcceptedTerms = existingUser.TermsAcceptedAt != null
            });
        }

        var pendingEmail = _authLoginOtpHelper.ResolveRegistrationEmail(loginOtp);
        var result = await _teacherRegistrationService.CreateBasicAccountAsync(
            fullPhoneNumber,
            pendingEmail,
            termsAcceptedAt: request.AcceptedTerms ? DateTime.UtcNow : null);
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
            NextStep = registerNextStep,
            HasAcceptedTerms = request.AcceptedTerms
        });
    }
}
