using Qalam.Data.Entity.Course;
using Qalam.Infrastructure.InfrastructureBases;

namespace Qalam.Infrastructure.Abstracts;

public interface IEnrollmentParticipantRepository : IGenericRepositoryAsync<EnrollmentParticipant>
{
    /// <summary>
    /// Tracking load with Enrollment + Course + Student graph, scoped for the pay-participant flow.
    /// </summary>
    Task<EnrollmentParticipant?> GetByIdForPaymentAsync(int id, CancellationToken ct);
}
