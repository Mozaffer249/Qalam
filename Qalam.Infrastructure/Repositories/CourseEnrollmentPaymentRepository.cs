using Qalam.Data.Entity.Payment;
using Qalam.Infrastructure.Abstracts;
using Qalam.Infrastructure.context;
using Qalam.Infrastructure.InfrastructureBases;

namespace Qalam.Infrastructure.Repositories;

public class CourseEnrollmentPaymentRepository : GenericRepositoryAsync<CourseEnrollmentPayment>, ICourseEnrollmentPaymentRepository
{
    public CourseEnrollmentPaymentRepository(ApplicationDBContext context) : base(context)
    {
    }
}
