using Qalam.Data.Entity.Payment;
using Qalam.Infrastructure.Abstracts;
using Qalam.Infrastructure.context;
using Qalam.Infrastructure.InfrastructureBases;

namespace Qalam.Infrastructure.Repositories;

public class EnrollmentPaymentRepository : GenericRepositoryAsync<EnrollmentPayment>, IEnrollmentPaymentRepository
{
    public EnrollmentPaymentRepository(ApplicationDBContext context) : base(context)
    {
    }
}
