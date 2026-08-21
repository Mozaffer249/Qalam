using Qalam.Data.Entity.Student;
using Qalam.Data.Entity.Teacher;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Service.Implementations;

public interface IFreeSessionPolicyService
{
    Task<bool> IsStudentEligibleForFreeTrialAsync(int studentId, CancellationToken cancellationToken = default);

    Task MarkStudentFreeTrialUsedAsync(int studentId, CancellationToken cancellationToken = default);

    /// <summary>Unlock lowest active level after first completed session (idempotent).</summary>
    Task TryCompleteTeacherInterviewAsync(int teacherId, CancellationToken cancellationToken = default);
}

public class FreeSessionPolicyService : IFreeSessionPolicyService
{
    private readonly IStudentRepository _studentRepository;
    private readonly ITeacherRepository _teacherRepository;
    private readonly ITeacherLevelRepository _teacherLevelRepository;

    public FreeSessionPolicyService(
        IStudentRepository studentRepository,
        ITeacherRepository teacherRepository,
        ITeacherLevelRepository teacherLevelRepository)
    {
        _studentRepository = studentRepository;
        _teacherRepository = teacherRepository;
        _teacherLevelRepository = teacherLevelRepository;
    }

    public async Task<bool> IsStudentEligibleForFreeTrialAsync(
        int studentId,
        CancellationToken cancellationToken = default)
    {
        var student = await _studentRepository.GetByIdAsync(studentId);
        return student is { HasUsedFreeTrialSession: false };
    }

    public async Task MarkStudentFreeTrialUsedAsync(int studentId, CancellationToken cancellationToken = default)
    {
        var student = await _studentRepository.GetByIdAsync(studentId);
        if (student == null || student.HasUsedFreeTrialSession)
            return;

        student.HasUsedFreeTrialSession = true;
        student.UpdatedAt = DateTime.UtcNow;
        await _studentRepository.UpdateAsync(student);
        await _studentRepository.SaveChangesAsync();
    }

    public async Task TryCompleteTeacherInterviewAsync(int teacherId, CancellationToken cancellationToken = default)
    {
        var teacher = await _teacherRepository.GetByIdAsync(teacherId);
        if (teacher == null || teacher.HasCompletedInterviewSession)
            return;

        var minLevel = await _teacherLevelRepository.GetStarterLevelAsync(cancellationToken);
        if (minLevel == null)
            throw new InvalidOperationException("No active teacher level configured.");

        teacher.HasCompletedInterviewSession = true;
        teacher.TeacherLevelId ??= minLevel.Id;
        teacher.UpdatedAt = DateTime.UtcNow;
        await _teacherRepository.UpdateAsync(teacher);
        await _teacherRepository.SaveChangesAsync();
    }
}
