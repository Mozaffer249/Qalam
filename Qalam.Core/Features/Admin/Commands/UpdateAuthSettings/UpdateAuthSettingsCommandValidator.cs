using FluentValidation;

namespace Qalam.Core.Features.Admin.Commands.UpdateAuthSettings;

public class UpdateAuthSettingsCommandValidator : AbstractValidator<UpdateAuthSettingsCommand>
{
    private static readonly string[] AllowedLoginMethods = { "Otp" };
    private static readonly string[] AllowedOtpDelivery = { "Email", "Sms" };

    public UpdateAuthSettingsCommandValidator()
    {
        RuleFor(x => x.Settings).NotNull();
        RuleFor(x => x.Settings.Teacher).NotNull();
        RuleFor(x => x.Settings.Student).NotNull();
        RuleFor(x => x.Settings.Otp).NotNull();

        RuleFor(x => x.Settings.Teacher.LoginMethod)
            .Must(m => AllowedLoginMethods.Contains(m, StringComparer.OrdinalIgnoreCase));
        RuleFor(x => x.Settings.Student.LoginMethod)
            .Must(m => AllowedLoginMethods.Contains(m, StringComparer.OrdinalIgnoreCase));

        RuleFor(x => x.Settings.Teacher.OtpDelivery)
            .Must(d => AllowedOtpDelivery.Contains(d, StringComparer.OrdinalIgnoreCase));
        RuleFor(x => x.Settings.Student.OtpDelivery)
            .Must(d => AllowedOtpDelivery.Contains(d, StringComparer.OrdinalIgnoreCase));

        RuleFor(x => x.Settings.Otp.Length).InclusiveBetween(4, 8);
        RuleFor(x => x.Settings.Otp.ExpirySeconds).InclusiveBetween(60, 900);
    }
}
