using Microsoft.Extensions.Logging;
using Qalam.Data.DTOs.Admin;
using Qalam.Data.DTOs.Teacher;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Teacher;
using Qalam.Data.Results;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Service.Implementations;

public class TeacherSubjectAdminService : ITeacherSubjectAdminService
{
    private readonly ITeacherSubjectRepository _teacherSubjectRepository;
    private readonly ITeacherRepository _teacherRepository;
    private readonly ILogger<TeacherSubjectAdminService> _logger;

    public TeacherSubjectAdminService(
        ITeacherSubjectRepository teacherSubjectRepository,
        ITeacherRepository teacherRepository,
        ILogger<TeacherSubjectAdminService> logger)
    {
        _teacherSubjectRepository = teacherSubjectRepository;
        _teacherRepository = teacherRepository;
        _logger = logger;
    }

    public async Task<List<AdminTeacherSubjectDto>> GetTeacherSubjectsForAdminAsync(
        int teacherId,
        CancellationToken cancellationToken = default)
    {
        if (await _teacherRepository.GetByIdAsync(teacherId) == null)
            return new List<AdminTeacherSubjectDto>();

        var subjects = await _teacherSubjectRepository.GetAllByTeacherIdForAdminAsync(teacherId, cancellationToken);
        return subjects.Select(MapToAdminDto).ToList();
    }

    public async Task<AdminTeacherSubjectDto?> GetTeacherSubjectForAdminAsync(
        int teacherId,
        int teacherSubjectId,
        CancellationToken cancellationToken = default)
    {
        var subject = await _teacherSubjectRepository.GetByIdForTeacherAsync(teacherId, teacherSubjectId, cancellationToken);
        return subject == null ? null : MapToAdminDto(subject);
    }

    public async Task<PaginatedResult<AdminTeacherSubjectDto>> GetTeacherSubjectsListAsync(
        int pageNumber,
        int pageSize,
        int? teacherId = null,
        int? subjectId = null,
        bool? isActive = null,
        DocumentVerificationStatus? verificationStatus = null,
        CancellationToken cancellationToken = default)
    {
        var page = await _teacherSubjectRepository.GetPagedForAdminAsync(
            pageNumber, pageSize, teacherId, subjectId, isActive, verificationStatus, cancellationToken);

        return new PaginatedResult<AdminTeacherSubjectDto>(
            page.Items.Select(MapToAdminDto).ToList(),
            page.TotalCount,
            page.PageNumber,
            page.PageSize);
    }

    public async Task<TeacherSubjectSummaryDto> GetSubjectSummaryAsync(
        int teacherId,
        CancellationToken cancellationToken = default)
    {
        var subjects = await _teacherSubjectRepository.GetAllByTeacherIdForAdminAsync(teacherId, cancellationToken);
        return BuildSummary(subjects);
    }

    public async Task<bool> InactivateSubjectAsync(
        int teacherId,
        int teacherSubjectId,
        int adminId,
        CancellationToken cancellationToken = default)
    {
        var subject = await GetTrackedSubjectAsync(teacherId, teacherSubjectId, cancellationToken);
        if (subject == null)
            return false;

        subject.IsActive = false;
        subject.UpdatedAt = DateTime.UtcNow;
        subject.UpdatedBy = adminId;

        await _teacherSubjectRepository.UpdateAsync(subject);
        await _teacherSubjectRepository.SaveChangesAsync();

        _logger.LogInformation(
            "Teacher subject {TeacherSubjectId} inactivated by admin {AdminId}",
            teacherSubjectId, adminId);

        return true;
    }

    public async Task<bool> ActivateSubjectAsync(
        int teacherId,
        int teacherSubjectId,
        int adminId,
        CancellationToken cancellationToken = default)
    {
        var subject = await GetTrackedSubjectAsync(teacherId, teacherSubjectId, cancellationToken);
        if (subject == null)
            return false;

        if (subject.VerificationStatus == DocumentVerificationStatus.Rejected)
            throw new InvalidOperationException("Rejected subjects must be restored before activation.");

        subject.IsActive = true;
        subject.UpdatedAt = DateTime.UtcNow;
        subject.UpdatedBy = adminId;

        await _teacherSubjectRepository.UpdateAsync(subject);
        await _teacherSubjectRepository.SaveChangesAsync();

        _logger.LogInformation(
            "Teacher subject {TeacherSubjectId} activated by admin {AdminId}",
            teacherSubjectId, adminId);

        return true;
    }

    public async Task<bool> RejectSubjectAsync(
        int teacherId,
        int teacherSubjectId,
        int adminId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var subject = await GetTrackedSubjectAsync(teacherId, teacherSubjectId, cancellationToken);
        if (subject == null)
            return false;

        subject.VerificationStatus = DocumentVerificationStatus.Rejected;
        subject.RejectionReason = reason.Trim();
        subject.ReviewedByAdminId = adminId;
        subject.ReviewedAt = DateTime.UtcNow;
        subject.IsActive = false;
        subject.UpdatedAt = DateTime.UtcNow;
        subject.UpdatedBy = adminId;

        await _teacherSubjectRepository.UpdateAsync(subject);
        await _teacherSubjectRepository.SaveChangesAsync();

        _logger.LogInformation(
            "Teacher subject {TeacherSubjectId} rejected by admin {AdminId}",
            teacherSubjectId, adminId);

        return true;
    }

    public async Task<bool> RestoreSubjectAsync(
        int teacherId,
        int teacherSubjectId,
        int adminId,
        CancellationToken cancellationToken = default)
    {
        var subject = await GetTrackedSubjectAsync(teacherId, teacherSubjectId, cancellationToken);
        if (subject == null)
            return false;

        subject.VerificationStatus = DocumentVerificationStatus.Approved;
        subject.RejectionReason = null;
        subject.ReviewedByAdminId = adminId;
        subject.ReviewedAt = DateTime.UtcNow;
        subject.IsActive = true;
        subject.UpdatedAt = DateTime.UtcNow;
        subject.UpdatedBy = adminId;

        await _teacherSubjectRepository.UpdateAsync(subject);
        await _teacherSubjectRepository.SaveChangesAsync();

        _logger.LogInformation(
            "Teacher subject {TeacherSubjectId} restored by admin {AdminId}",
            teacherSubjectId, adminId);

        return true;
    }

    private async Task<TeacherSubject?> GetTrackedSubjectAsync(
        int teacherId,
        int teacherSubjectId,
        CancellationToken cancellationToken)
    {
        var subject = await _teacherSubjectRepository.GetByIdAsync(teacherSubjectId);
        if (subject == null || subject.TeacherId != teacherId)
            return null;
        return subject;
    }

    private AdminTeacherSubjectDto MapToAdminDto(TeacherSubject subject)
    {
        var teacherName = subject.Teacher?.User != null
            ? $"{subject.Teacher.User.FirstName} {subject.Teacher.User.LastName}".Trim()
            : "Unknown";

        return new AdminTeacherSubjectDto
        {
            Id = subject.Id,
            TeacherId = subject.TeacherId,
            TeacherFullName = string.IsNullOrWhiteSpace(teacherName) ? "Unknown" : teacherName,
            SubjectId = subject.SubjectId,
            SubjectNameAr = subject.Subject?.NameAr ?? "",
            SubjectNameEn = subject.Subject?.NameEn ?? "",
            DomainCode = subject.Subject?.Domain?.Code,
            CanTeachFullSubject = subject.CanTeachFullSubject,
            IsActive = subject.IsActive,
            VerificationStatus = subject.VerificationStatus,
            RejectionReason = subject.RejectionReason,
            ReviewedAt = subject.ReviewedAt,
            CreatedAt = subject.CreatedAt,
            Units = subject.TeacherSubjectUnits.Select(MapUnitToDto).ToList()
        };
    }

    private static TeacherSubjectUnitResponseDto MapUnitToDto(TeacherSubjectUnit unit)
    {
        return new TeacherSubjectUnitResponseDto
        {
            Id = unit.Id,
            UnitId = unit.UnitId,
            UnitNameAr = unit.Unit?.NameAr ?? "",
            UnitNameEn = unit.Unit?.NameEn ?? "",
            UnitTypeCode = unit.Unit?.UnitTypeCode,
            QuranContentTypeId = unit.QuranContentTypeId,
            QuranContentTypeNameAr = unit.QuranContentType?.NameAr,
            QuranContentTypeNameEn = unit.QuranContentType?.NameEn,
            QuranLevelId = unit.QuranLevelId,
            QuranLevelNameAr = unit.QuranLevel?.NameAr,
            QuranLevelNameEn = unit.QuranLevel?.NameEn
        };
    }

    private static TeacherSubjectSummaryDto BuildSummary(IReadOnlyCollection<TeacherSubject> subjects)
    {
        return new TeacherSubjectSummaryDto
        {
            TotalSubjects = subjects.Count,
            ActiveSubjects = subjects.Count(s => s.IsActive && s.VerificationStatus == DocumentVerificationStatus.Approved),
            InactiveSubjects = subjects.Count(s => !s.IsActive && s.VerificationStatus != DocumentVerificationStatus.Rejected),
            RejectedSubjects = subjects.Count(s => s.VerificationStatus == DocumentVerificationStatus.Rejected)
        };
    }
}
