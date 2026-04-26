using Qalam.Data.Entity.Payment;
using Qalam.Infrastructure.Abstracts;
using Qalam.Infrastructure.context;
using Qalam.Infrastructure.InfrastructureBases;

namespace Qalam.Infrastructure.Repositories;

public class PaymentRepository : GenericRepositoryAsync<Payment>, IPaymentRepository
{
    public PaymentRepository(ApplicationDBContext context) : base(context)
    {
    }
}
