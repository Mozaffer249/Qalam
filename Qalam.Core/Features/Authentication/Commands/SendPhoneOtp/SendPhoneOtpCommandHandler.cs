using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Teacher;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Identity;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;
using Qalam.Service.Models;

namespace Qalam.Core.Features.Authentication.Commands.SendPhoneOtp;

public class SendPhoneOtpCommandHandler : ResponseHandler,
    IRequestHandler<SendPhoneOtpCommand, Response<SendOtpResponseDto>>
{
    private readonly IOtpService _otpService;
    private readonly UserManager<User> _userManager;
    private readonly ITeacherRepository _teacherRepository;
    private readonly IAuthSettingsProvider _authSettingsProvider;

    public SendPhoneOtpCommandHandler(
        IOtpService otpService,
        UserManager<User> userManager,
        ITeacherRepository teacherRepository,
        IAuthSettingsProvider authSettingsProvider,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _otpService = otpService;
        _userManager = userManager;
        _teacherRepository = teacherRepository;
        _authSettingsProvider = authSettingsProvider;
    }

    public async Task<Response<SendOtpResponseDto>> Handle(
        SendPhoneOtpCommand request,
        CancellationToken cancellationToken)
    {
        var settings = await _authSettingsProvider.GetSettingsAsync(cancellationToken);
        var persona = settings.Teacher;

        if (persona.RegisterRequiresPhone && string.IsNullOrWhiteSpace(request.PhoneNumber))
            return BadRequest<SendOtpResponseDto>("Phone number is required.");

        var fullPhoneNumber = $"{request.CountryCode}{request.PhoneNumber}";
        var existingUser = await _userManager.Users
            .FirstOrDefaultAsync(u => u.PhoneNumber == fullPhoneNumber, cancellationToken);
        var isNewUser = existingUser == null;

        if (!isNewUser)
        {
            var teacher = await _teacherRepository.GetByUserIdAsync(existingUser!.Id);
            if (teacher?.Status == TeacherStatus.Blocked)
                return BadRequest<SendOtpResponseDto>("Your account has been blocked. Please contact support.");
        }

        if (isNewUser && persona.RegisterRequiresEmail && string.IsNullOrWhiteSpace(request.Email))
            return BadRequest<SendOtpResponseDto>("Email is required for registration.");

        if (!isNewUser && string.Equals(persona.OtpDelivery, "Email", StringComparison.OrdinalIgnoreCase)
            && string.IsNullOrWhiteSpace(existingUser!.Email))
            return BadRequest<SendOtpResponseDto>("No email on file. Please contact support to add an email.");

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
                return BadRequest<SendOtpResponseDto>("Email is already registered.");
            }
            if (emailOwner != null && (isNewUser || emailOwner.Id != existingUser!.Id))
                return BadRequest<SendOtpResponseDto>("Email is already registered.");
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
                Persona = LoginOtpPersona.Teacher,
                PersonaSettings = persona,
                OtpSettings = settings.Otp
            }, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest<SendOtpResponseDto>(ex.Message);
        }

        if (sendResult == null)
        {
            return TooManyRequests<SendOtpResponseDto>(
                "A valid OTP has already been sent. Please check your messages or wait before requesting a new one.");
        }

        var maskedPhone = request.PhoneNumber.Length >= 4
            ? $"*******{request.PhoneNumber[^4..]}"
            : "****";

        return Success(entity: new SendOtpResponseDto
        {
            IsNewUser = isNewUser,
            PhoneNumber = maskedPhone,
            OtpSentTo = sendResult.OtpSentTo,
            MaskedDestination = sendResult.MaskedDestination,
            Message = isNewUser
                ? "OTP sent successfully. Complete registration after verification."
                : "OTP sent successfully. Welcome back!"
        });
    }
}
