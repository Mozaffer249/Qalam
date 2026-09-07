using Qalam.Data.Entity.Payment;
using Qalam.Infrastructure.InfrastructureBases;

namespace Qalam.Infrastructure.Abstracts;

public interface IPaymentRepository : IGenericRepositoryAsync<Payment>
{
    Task<Payment?> GetByIdWithItemsAsync(int paymentId, CancellationToken cancellationToken = default);

    Task<Payment?> GetByProviderTransactionIdAsync(
        string providerTransactionId,
        CancellationToken cancellationToken = default);
}
