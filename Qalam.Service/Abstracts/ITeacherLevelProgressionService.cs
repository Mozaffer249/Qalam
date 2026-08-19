namespace Qalam.Service.Abstracts;

public interface ITeacherLevelProgressionService
{
    Task EvaluateTeacherAsync(int teacherId, CancellationToken cancellationToken = default);
}
