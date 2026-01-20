using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Authentication.Commands.VerifyOtpAndCreateAccount;

public class VerifyOtpAndCreateAccountCommandHandler  : ResponseHandler,
    IRequestHandler<VerifyOtpAndCreateAccountCommand, Response<string>>
{
    private readonly IOtpService _otpService;
    private readonly ITeacherRegistrationService _teacherRegistrationService;
    private readonly IPhoneOtpRepository _otpRepository;

    public VerifyOtpAndCreateAccountCommandHandler(
        IOtpService otpService,
        ITeacherRegistrationService teacherRegistrationService,
        IPhoneOtpRepository otpRepository,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _otpService = otpService;
        _teacherRegistrationService = teacherRegistrationService;
        _otpRepository = otpRepository;
    }

    public async Task<Response<string>> Handle(
        VerifyOtpAndCreateAccountCommand request,
        CancellationToken cancellationToken)
    {
        // Verify OTP
        var isValid = await _otpService.VerifyPhoneOtpAsync(
            request.PhoneNumber,
            request.OtpCode);

        if (!isValid)
        {
            return BadRequest<string>("Invalid or expired OTP code");
        }

        string fullPhoneNumber;
        
        // For test OTP, use default country code (no DB record exists)
        if (request.OtpCode == "1234")
        {
            fullPhoneNumber = $"+966{request.PhoneNumber}";  // Default to Saudi Arabia
            
            // Create basic account (User + Teacher role)
            var testResult = await _teacherRegistrationService.CreateBasicAccountAsync(fullPhoneNumber);
            return Success<string>(entity: testResult.Token);
        }
        
        // Get the OTP record to mark it as used
        var otp = await _otpRepository.GetValidOtpAsync(request.PhoneNumber, request.OtpCode);
        if (otp == null)
        {
            return BadRequest<string>("OTP verification failed");
        }

        // Create full phone number from OTP record
        fullPhoneNumber = $"{otp.CountryCode}{request.PhoneNumber}";

        // Create basic account (User + Teacher role)
        var result = await _teacherRegistrationService.CreateBasicAccountAsync(fullPhoneNumber);

        // Mark OTP as used
        await _otpRepository.MarkAsUsedAsync(otp.Id, result.UserId);
        await _otpRepository.SaveChangesAsync();

        return Success<string>(entity: result.Token);
    }
}
