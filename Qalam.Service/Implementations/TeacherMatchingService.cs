using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Service.Implementations;

public class TeacherMatchingService : ITeacherMatchingService
{
    private readonly IOpenSessionRequestRepository _requestRepo;
    private readonly IOpenSessionRequestTargetRepository _targetRepo;
    private readonly ITeacherSubjectRepository _teacherSubjectRepo;

    public TeacherMatchingService(
        IOpenSessionRequestRepository requestRepo,
        IOpenSessionRequestTargetRepository targetRepo,
        ITeacherSubjectRepository teacherSubjectRepo)
    {
        _requestRepo = requestRepo;
        _targetRepo = targetRepo;
        _teacherSubjectRepo = teacherSubjectRepo;
    }

    public async Task<List<int>> FindMatchingTeacherIdsAsync(int requestId, CancellationToken cancellationToken = default)
    {
        var subjectId = await _requestRepo.GetSubjectIdAsync(requestId, cancellationToken);
        if (subjectId == null)
            return new List<int>();

        var candidates = await _teacherSubjectRepo.GetActiveTeacherIdsBySubjectAsync(subjectId.Value, cancellationToken);
        if (candidates.Count == 0)
            return new List<int>();

        var alreadyTargeted = await _targetRepo.GetTargetedTeacherIdsAsync(requestId, cancellationToken);
        var alreadySet = alreadyTargeted.ToHashSet();

        return candidates.Where(id => !alreadySet.Contains(id)).ToList();
    }
}
