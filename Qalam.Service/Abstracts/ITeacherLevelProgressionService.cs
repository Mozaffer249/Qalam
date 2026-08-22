namespace Qalam.Service.Abstracts;

public interface ITeacherLevelProgressionService
{
    Task EvaluateTeacherAsync(int teacherId, int domainId, CancellationToken cancellationToken = default);
}
