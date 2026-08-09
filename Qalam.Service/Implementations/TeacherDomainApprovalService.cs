using Microsoft.Extensions.Logging;
using Qalam.Data.Entity.Teacher;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Service.Implementations;

public class TeacherDomainApprovalService : ITeacherDomainApprovalService
{
    private readonly ITeacherDomainApprovalRepository _approvalRepository;
    private readonly ITeacherDomainSubjectCascadeService _cascadeService;
    private readonly ITeacherRepository _teacherRepository;
    private readonly IEducationDomainRepository _domainRepository;
    private readonly ILogger<TeacherDomainApprovalService> _logger;

    public TeacherDomainApprovalService(
        ITeacherDomainApprovalRepository approvalRepository,
        ITeacherDomainSubjectCascadeService cascadeService,
        ITeacherRepository teacherRepository,
        IEducationDomainRepository domainRepository,
        ILogger<TeacherDomainApprovalService> logger)
    {
        _approvalRepository = approvalRepository;
        _cascadeService = cascadeService;
        _teacherRepository = teacherRepository;
        _domainRepository = domainRepository;
        _logger = logger;
    }

    public async Task<(bool Success, string? ErrorMessage)> ApproveDomainAsync(
        int teacherId,
        int domainId,
        int adminId,
        CancellationToken cancellationToken = default)
    {
        var teacher = await _teacherRepository.GetByIdAsync(teacherId);
        if (teacher == null)
            return (false, "Teacher not found");

        var domain = await _domainRepository.GetByIdAsync(domainId);
        if (domain == null)
            return (false, "Education domain not found");

        if (!await _cascadeService.IsDomainFullyApprovedForTeacherAsync(teacherId, domainId, cancellationToken))
            return (false, "All domain answers must be approved before the domain can be approved.");

        var existing = await _approvalRepository.GetByTeacherAndDomainAsync(teacherId, domainId, cancellationToken);
        var now = DateTime.UtcNow;

        if (existing != null)
        {
            if (existing.RevokedAt == null)
                return (false, "This domain is already approved for the teacher.");

            existing.ApprovedByAdminId = adminId;
            existing.ApprovedAt = now;
            existing.RevokedAt = null;
            existing.RevokedByAdminId = null;
            existing.RevokeReason = null;
            existing.UpdatedAt = now;
            existing.UpdatedBy = adminId;
            await _approvalRepository.UpdateAsync(existing);
        }
        else
        {
            await _approvalRepository.AddAsync(new TeacherDomainApproval
            {
                TeacherId = teacherId,
                DomainId = domainId,
                ApprovedByAdminId = adminId,
                ApprovedAt = now,
                CreatedBy = adminId
            });
        }

        await _approvalRepository.SaveChangesAsync();

        await _cascadeService.ApproveSubjectsInDomainAsync(teacherId, domainId, cancellationToken);

        _logger.LogInformation(
            "Domain {DomainId} approved for teacher {TeacherId} by admin {AdminId}",
            domainId,
            teacherId,
            adminId);

        return (true, null);
    }

    public async Task<(bool Success, string? ErrorMessage)> RevokeAsync(
        int teacherId,
        int domainId,
        int adminId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(reason))
            return (false, "Revocation reason is required.");

        var existing = await _approvalRepository.GetByTeacherAndDomainAsync(teacherId, domainId, cancellationToken);
        if (existing == null || existing.RevokedAt != null)
            return (false, "No active domain approval found to revoke.");

        var now = DateTime.UtcNow;
        existing.RevokedAt = now;
        existing.RevokedByAdminId = adminId;
        existing.RevokeReason = reason.Trim();
        existing.UpdatedAt = now;
        existing.UpdatedBy = adminId;

        await _approvalRepository.UpdateAsync(existing);
        await _approvalRepository.SaveChangesAsync();

        await _cascadeService.RejectSubjectsInDomainAsync(
            teacherId,
            domainId,
            adminId,
            reason.Trim(),
            cancellationToken);

        _logger.LogInformation(
            "Domain {DomainId} approval revoked for teacher {TeacherId} by admin {AdminId}",
            domainId,
            teacherId,
            adminId);

        return (true, null);
    }

    public Task<bool> IsDomainApprovedAsync(
        int teacherId,
        int domainId,
        CancellationToken cancellationToken = default) =>
        _approvalRepository.IsDomainApprovedAsync(teacherId, domainId, cancellationToken);

    public Task<bool> HasAnyApprovedDomainAsync(
        int teacherId,
        CancellationToken cancellationToken = default) =>
        _approvalRepository.HasActiveApprovalAsync(teacherId, cancellationToken);

    public async Task<DateTime?> GetApprovedAtAsync(
        int teacherId,
        int domainId,
        CancellationToken cancellationToken = default)
    {
        var approval = await _approvalRepository.GetByTeacherAndDomainAsync(teacherId, domainId, cancellationToken);
        if (approval == null || approval.RevokedAt != null)
            return null;
        return approval.ApprovedAt;
    }
}
