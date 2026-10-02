using Qalam.Data.Entity.Payment;

namespace Qalam.Service.Abstracts;

/// <summary>Turns an applied policy case into inbox/email/push notifications for the affected parties.</summary>
public interface IPolicyNotificationService
{
    Task NotifyCaseAsync(PolicyCase policyCase, CancellationToken cancellationToken = default);
}
