using Qalam.Data.DTOs.Admin;

namespace Qalam.Service.Abstracts;

public interface IFreeSessionLedgerReadService
{
    Task<List<AdminStudentFreeTrialConsumptionDto>> ListStudentConsumptionsAsync(
        int studentId,
        CancellationToken cancellationToken = default);

    Task<List<AdminTeacherInterviewUnlockDto>> ListTeacherInterviewUnlocksAsync(
        int teacherId,
        CancellationToken cancellationToken = default);
}
