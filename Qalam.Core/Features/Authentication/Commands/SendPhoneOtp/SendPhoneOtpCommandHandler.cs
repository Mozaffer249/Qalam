using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.Entity.Identity;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Authentication.Commands.SendPhoneOtp;

public class SendPhoneOtpCommandHandler : ResponseHandler,
    IRequestHandler<SendPhoneOtpCommand, Response<string>>
{
    private readonly IOtpService _otpService;
    private readonly UserManager<User> _userManager;

    public SendPhoneOtpCommandHandler(
        IOtpService otpService,
        UserManager<User> userManager,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _otpService = otpService;
        _userManager = userManager;
    }

    public async Task<Response<string>> Handle(
        SendPhoneOtpCommand request,
        CancellationToken cancellationToken)
    {
        // Create full phone number
        var fullPhoneNumber = $"{request.CountryCode}{request.PhoneNumber}";

        // Check if phone already registered
        var existingUser = _userManager.Users
            .FirstOrDefault(u => u.PhoneNumber == fullPhoneNumber);
        
        if (existingUser != null)
        {
            return BadRequest<string>("Phone number already registered");
        }

        // Generate and send OTP
        var otpCode = await _otpService.GeneratePhoneOtpAsync(
            request.CountryCode,
            request.PhoneNumber);

        await _otpService.SendOtpSmsAsync(fullPhoneNumber, otpCode);

        return Success<string>("OTP sent successfully. Please check your phone.");
    }
}
