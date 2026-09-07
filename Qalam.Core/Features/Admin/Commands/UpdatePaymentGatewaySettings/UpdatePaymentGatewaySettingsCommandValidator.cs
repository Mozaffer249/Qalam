using FluentValidation;
using Qalam.Data.DTOs.Payment;
using Qalam.Data.DTOs.Platform;
using Qalam.Service.Abstracts;
using Qalam.Service.Implementations;

namespace Qalam.Core.Features.Admin.Commands.UpdatePaymentGatewaySettings;

public class UpdatePaymentGatewaySettingsCommandValidator : AbstractValidator<UpdatePaymentGatewaySettingsCommand>
{
    public UpdatePaymentGatewaySettingsCommandValidator(IPaymentGatewayResolver resolver)
    {
        RuleFor(x => x.Settings.ActiveProvider)
            .NotEmpty()
            .Must(name =>
            {
                try
                {
                    var gateway = resolver.Resolve(name);
                    if (gateway.ProviderName.Equals(MockPaymentGateway.Name, StringComparison.OrdinalIgnoreCase))
                        return true;
                    return gateway.IsConfigured;
                }
                catch
                {
                    return false;
                }
            })
            .WithMessage("ActiveProvider must be a registered and configured payment gateway.");

        RuleFor(x => x.Settings.MoyasarClientMode)
            .Must(mode =>
            {
                var n = PaymentGatewaySettingsDefaults.NormalizeMoyasarClientMode(mode);
                return n is nameof(PaymentClientMode.HostedRedirect) or nameof(PaymentClientMode.NativeSdk);
            })
            .WithMessage("MoyasarClientMode must be HostedRedirect or NativeSdk.");
    }
}
