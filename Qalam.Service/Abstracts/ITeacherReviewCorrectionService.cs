using Qalam.Data.DTOs.Teacher;

namespace Qalam.Service.Abstracts;

public interface ITeacherReviewCorrectionService
{
    Task<List<TeacherReviewCorrectionDto>> GetPendingCorrectionsAsync(
        int teacherId,
        CancellationToken cancellationToken = default);
}
