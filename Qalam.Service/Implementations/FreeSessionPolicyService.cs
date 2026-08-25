using Qalam.Data.Entity.Student;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Service.Implementations;

public interface IFreeSessionPolicyService
{
    /// <summary>
    /// Package shape eligible for the lifetime free trial: individual and exactly one session.
    /// Does not check whether the student has already used their trial.
    /// </summary>
    bool IsEligiblePackage(bool isGroup, int sessionCount);

    Task<bool> IsStudentEligibleForFreeTrialAsync(int studentId, CancellationToken cancellationToken = default);

    Task MarkStudentFreeTrialUsedAsync(int studentId, CancellationToken cancellationToken = default);

    /// <summary>Unlock lowest active level for the domain after first completed session (idempotent).</summary>
    Task TryCompleteTeacherInterviewAsync(
        int teacherId,
        int domainId,
        CancellationToken cancellationToken = default);
}

public class FreeSessionPolicyService : IFreeSessionPolicyService
{
    private readonly IStudentRepository _studentRepository;
    private readonly ITeacherRepository _teacherRepository;
    private readonly ITeacherLevelRepository _teacherLevelRepository;
    private readonly ITeacherDomainPricingRepository _domainPricingRepository;

    public FreeSessionPolicyService(
        IStudentRepository studentRepository,
        ITeacherRepository teacherRepository,
        ITeacherLevelRepository teacherLevelRepository,
        ITeacherDomainPricingRepository domainPricingRepository)
    {
        _studentRepository = studentRepository;
        _teacherRepository = teacherRepository;
        _teacherLevelRepository = teacherLevelRepository;
        _domainPricingRepository = domainPricingRepository;
    }

    public bool IsEligiblePackage(bool isGroup, int sessionCount) =>
        !isGroup && sessionCount == 1;

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

    public async Task TryCompleteTeacherInterviewAsync(
        int teacherId,
        int domainId,
        CancellationToken cancellationToken = default)
    {
        if (domainId <= 0)
            return;

        var teacher = await _teacherRepository.GetByIdAsync(teacherId);
        if (teacher == null)
            return;

        var pricing = await _domainPricingRepository.GetOrCreateAsync(teacherId, domainId, cancellationToken);
        if (pricing.HasCompletedInterviewSession && pricing.TeacherLevelId.HasValue)
        {
            // Keep legacy teacher flags in sync when any domain is unlocked.
            if (!teacher.HasCompletedInterviewSession)
            {
                teacher.HasCompletedInterviewSession = true;
                teacher.TeacherLevelId ??= pricing.TeacherLevelId;
                teacher.UpdatedAt = DateTime.UtcNow;
                await _teacherRepository.UpdateAsync(teacher);
                await _teacherRepository.SaveChangesAsync();
            }
            return;
        }

        var minLevel = await _teacherLevelRepository.GetStarterLevelAsync(cancellationToken);
        if (minLevel == null)
            throw new InvalidOperationException("No active teacher level configured.");

        pricing.HasCompletedInterviewSession = true;
        pricing.TeacherLevelId ??= minLevel.Id;
        pricing.UpdatedAt = DateTime.UtcNow;
        await _domainPricingRepository.UpdateAsync(pricing);

        teacher.HasCompletedInterviewSession = true;
        teacher.TeacherLevelId ??= pricing.TeacherLevelId;
        teacher.UpdatedAt = DateTime.UtcNow;
        await _teacherRepository.UpdateAsync(teacher);
        await _teacherRepository.SaveChangesAsync();
    }
}
