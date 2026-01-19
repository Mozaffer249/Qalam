using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Teacher;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Authentication.Commands.VerifyOtpAndCreateAccount;

public class VerifyOtpAndCreateAccountCommandHandler : ResponseHandler,
    IRequestHandler<VerifyOtpAndCreateAccountCommand, Response<PhoneVerificationDto>>
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

    public async Task<Response<PhoneVerificationDto>> Handle(
        VerifyOtpAndCreateAccountCommand request,
        CancellationToken cancellationToken)
    {
        // Verify OTP
        var isValid = await _otpService.VerifyPhoneOtpAsync(
            request.PhoneNumber,
            request.OtpCode);

        if (!isValid)
        {
            return BadRequest<PhoneVerificationDto>("Invalid or expired OTP code");
        }

        // Get the OTP record to mark it as used
        var otp = await _otpRepository.GetValidOtpAsync(request.PhoneNumber, request.OtpCode);
        if (otp == null)
        {
            return BadRequest<PhoneVerificationDto>("OTP verification failed");
        }

        // Create full phone number
        var fullPhoneNumber = $"{otp.CountryCode}{request.PhoneNumber}";

        // Create basic account (User + Teacher role)
        var result = await _teacherRegistrationService.CreateBasicAccountAsync(fullPhoneNumber);

        // Mark OTP as used
        await _otpRepository.MarkAsUsedAsync(otp.Id, result.UserId);
        await _otpRepository.SaveChangesAsync();

        return Success<PhoneVerificationDto>(entity: result);
    }
}
