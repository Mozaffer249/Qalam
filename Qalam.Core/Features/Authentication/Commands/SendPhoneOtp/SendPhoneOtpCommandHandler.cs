using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Teacher;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Identity;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Authentication.Commands.SendPhoneOtp;

public class SendPhoneOtpCommandHandler : ResponseHandler,
    IRequestHandler<SendPhoneOtpCommand, Response<SendOtpResponseDto>>
{
    private readonly IOtpService _otpService;
    private readonly UserManager<User> _userManager;
    private readonly ITeacherRepository _teacherRepository;

    public SendPhoneOtpCommandHandler(
        IOtpService otpService,
        UserManager<User> userManager,
        ITeacherRepository teacherRepository,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _otpService = otpService;
        _userManager = userManager;
        _teacherRepository = teacherRepository;
    }

    public async Task<Response<SendOtpResponseDto>> Handle(
        SendPhoneOtpCommand request,
        CancellationToken cancellationToken)
    {
        // Create full phone number
        var fullPhoneNumber = $"{request.CountryCode}{request.PhoneNumber}";

        // Check if phone already registered
        var existingUser = _userManager.Users
            .FirstOrDefault(u => u.PhoneNumber == fullPhoneNumber);
        
        bool isNewUser = existingUser == null;

        // If existing user, check if blocked
        if (!isNewUser)
        {
            var teacher = await _teacherRepository.GetByUserIdAsync(existingUser!.Id);
            if (teacher?.Status == TeacherStatus.Blocked)
            {
                return BadRequest<SendOtpResponseDto>("Your account has been blocked. Please contact support.");
            }
        }

        // Generate and send OTP (for both new and existing users)
        var otpCode = await _otpService.GeneratePhoneOtpAsync(
            request.CountryCode,
            request.PhoneNumber);

        await _otpService.SendOtpSmsAsync(fullPhoneNumber, otpCode);

        // Mask phone number for response (show last 4 digits)
        var maskedPhone = $"*******{request.PhoneNumber[^4..]}";

        return Success<SendOtpResponseDto>(entity: new SendOtpResponseDto
        {
            IsNewUser = isNewUser,
            PhoneNumber = maskedPhone,
            Message = isNewUser 
                ? "OTP sent successfully. Welcome! You will create a new account."
                : "OTP sent successfully. Welcome back!"
        });
    }
}
