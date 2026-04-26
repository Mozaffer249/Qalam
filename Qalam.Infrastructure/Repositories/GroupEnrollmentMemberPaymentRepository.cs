using Qalam.Data.Entity.Course;
using Qalam.Infrastructure.Abstracts;
using Qalam.Infrastructure.context;
using Qalam.Infrastructure.InfrastructureBases;

namespace Qalam.Infrastructure.Repositories;

public class GroupEnrollmentMemberPaymentRepository : GenericRepositoryAsync<GroupEnrollmentMemberPayment>, IGroupEnrollmentMemberPaymentRepository
{
    public GroupEnrollmentMemberPaymentRepository(ApplicationDBContext context) : base(context)
    {
    }
}
