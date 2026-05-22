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
using Qalam.Service.Models;

namespace Qalam.Core.Features.Authentication.Commands.StudentRegistration;

public class StudentSendOtpCommandHandler : ResponseHandler,
    IRequestHandler<StudentSendOtpCommand, Response<StudentSendOtpResponseDto>>
{
    private readonly IOtpService _otpService;
    private readonly UserManager<User> _userManager;
    private readonly IAuthSettingsProvider _authSettingsProvider;

    public StudentSendOtpCommandHandler(
        IOtpService otpService,
        UserManager<User> userManager,
        IAuthSettingsProvider authSettingsProvider,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _otpService = otpService;
        _userManager = userManager;
        _authSettingsProvider = authSettingsProvider;
    }

    public async Task<Response<StudentSendOtpResponseDto>> Handle(
        StudentSendOtpCommand request,
        CancellationToken cancellationToken)
    {
        var settings = await _authSettingsProvider.GetSettingsAsync(cancellationToken);
        var persona = settings.Student;

        if (persona.RegisterRequiresPhone && string.IsNullOrWhiteSpace(request.PhoneNumber))
            return BadRequest<StudentSendOtpResponseDto>("Phone number is required.");

        var fullPhoneNumber = $"{request.CountryCode}{request.PhoneNumber}";
        var existingUser = await _userManager.Users
            .FirstOrDefaultAsync(u => u.PhoneNumber == fullPhoneNumber, cancellationToken);
        var isNewUser = existingUser == null;

        var isExistingStudentOrGuardian = false;
        if (existingUser != null)
        {
            var roles = await _userManager.GetRolesAsync(existingUser);
            isExistingStudentOrGuardian = roles.Contains(Roles.Student) || roles.Contains(Roles.Guardian);
        }

        if (isNewUser && persona.RegisterRequiresEmail && string.IsNullOrWhiteSpace(request.Email))
            return BadRequest<StudentSendOtpResponseDto>("Email is required for registration.");

        if (!isNewUser && string.Equals(persona.OtpDelivery, "Email", StringComparison.OrdinalIgnoreCase)
            && string.IsNullOrWhiteSpace(existingUser!.Email))
            return BadRequest<StudentSendOtpResponseDto>("No email on file. Please contact support.");

        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            User? emailOwner;
            try
            {
                emailOwner = await _userManager.FindByEmailAsync(request.Email.Trim());
            }
            catch (InvalidOperationException)
            {
                // FindByEmailAsync throws if the DB already contains duplicate emails (legacy data
                // from before RequireUniqueEmail was enabled). Treat as a hard collision.
                return BadRequest<StudentSendOtpResponseDto>("Email is already registered.");
            }
            if (emailOwner != null && (isNewUser || emailOwner.Id != existingUser!.Id))
                return BadRequest<StudentSendOtpResponseDto>("Email is already registered.");
        }

        LoginOtpSendResult? sendResult;
        try
        {
            sendResult = await _otpService.GenerateAndSendLoginOtpAsync(new LoginOtpSendOptions
            {
                CountryCode = request.CountryCode,
                PhoneNumber = request.PhoneNumber,
                RequestEmail = request.Email,
                ExistingUserEmail = existingUser?.Email,
                IsNewUser = isNewUser,
                Persona = LoginOtpPersona.Student,
                PersonaSettings = persona,
                OtpSettings = settings.Otp
            }, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest<StudentSendOtpResponseDto>(ex.Message);
        }

        if (sendResult == null)
        {
            return TooManyRequests<StudentSendOtpResponseDto>(
                "A valid OTP code has already been sent. Please check your messages or wait before requesting a new one.");
        }

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
            OtpSentTo = sendResult.OtpSentTo,
            MaskedDestination = sendResult.MaskedDestination,
            Message = message
        });
    }
}
