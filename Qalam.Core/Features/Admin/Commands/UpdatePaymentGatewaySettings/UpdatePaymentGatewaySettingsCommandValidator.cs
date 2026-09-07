using FluentValidation;
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
    }
}
