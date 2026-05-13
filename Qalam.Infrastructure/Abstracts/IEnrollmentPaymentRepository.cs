using Qalam.Data.Entity.Payment;
using Qalam.Infrastructure.InfrastructureBases;

namespace Qalam.Infrastructure.Abstracts;

public interface IEnrollmentPaymentRepository : IGenericRepositoryAsync<EnrollmentPayment>
{
}
