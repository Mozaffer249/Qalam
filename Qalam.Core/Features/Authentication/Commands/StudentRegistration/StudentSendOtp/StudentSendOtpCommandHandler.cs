using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.AppMetaData;
using Qalam.Data.DTOs.Student;
using Qalam.Data.Entity.Identity;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Authentication.Commands.StudentRegistration;

public class StudentSendOtpCommandHandler : ResponseHandler,
    IRequestHandler<StudentSendOtpCommand, Response<StudentSendOtpResponseDto>>
{
    private readonly IOtpService _otpService;
    private readonly UserManager<User> _userManager;

    public StudentSendOtpCommandHandler(
        IOtpService otpService,
        UserManager<User> userManager,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _otpService = otpService;
        _userManager = userManager;
    }

    public async Task<Response<StudentSendOtpResponseDto>> Handle(
        StudentSendOtpCommand request,
        CancellationToken cancellationToken)
    {
        var fullPhoneNumber = $"{request.CountryCode}{request.PhoneNumber}";
        var existingUser = await _userManager.Users
            .FirstOrDefaultAsync(u => u.PhoneNumber == fullPhoneNumber, cancellationToken);
        bool isNewUser = existingUser == null;

        var isExistingStudentOrGuardian = false;
        if (existingUser != null)
        {
            var roles = await _userManager.GetRolesAsync(existingUser);
            isExistingStudentOrGuardian = roles.Contains(Roles.Student) || roles.Contains(Roles.Guardian);
        }

        var otpCode = await _otpService.GeneratePhoneOtpAsync(
            request.CountryCode,
            request.PhoneNumber);
        await _otpService.SendOtpSmsAsync(fullPhoneNumber, otpCode);

        var maskedPhone = request.PhoneNumber.Length >= 4
            ? $"*******{request.PhoneNumber[^4..]}"
            : "****";

        var message = isExistingStudentOrGuardian
            ? "OTP sent successfully. Sign in after verification."
            : "OTP sent successfully. Complete registration after verification.";

        return Success(entity: new StudentSendOtpResponseDto
        {
            IsNewUser = isNewUser,
            PhoneNumber = maskedPhone,
            Message = message
        });
    }
}
