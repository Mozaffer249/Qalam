using Microsoft.Extensions.Logging;
using Qalam.Data.DTOs.Admin;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Teacher;
using Qalam.Data.Results;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Service.Implementations;

public class TeacherManagementService : ITeacherManagementService
{
    private readonly ITeacherRepository _teacherRepository;
    private readonly ITeacherDocumentRepository _documentRepository;
    private readonly ITeacherRegistrationCompletionService _completionService;
    private readonly ITeacherLifecycleEmailService _lifecycleEmailService;
    private readonly ILogger<TeacherManagementService> _logger;

    public TeacherManagementService(
        ITeacherRepository teacherRepository,
        ITeacherDocumentRepository documentRepository,
        ITeacherRegistrationCompletionService completionService,
        ITeacherLifecycleEmailService lifecycleEmailService,
        ILogger<TeacherManagementService> logger)
    {
        _teacherRepository = teacherRepository;
        _documentRepository = documentRepository;
        _completionService = completionService;
        _lifecycleEmailService = lifecycleEmailService;
        _logger = logger;
    }

    public async Task<PaginatedResult<PendingTeacherDto>> GetPendingTeachersAsync(int pageNumber, int pageSize)
    {
        var query = _teacherRepository.GetPendingTeachersQueryable();
        
        var totalCount = await _teacherRepository.CountAsync(query);
        
        var teachers = await _teacherRepository.GetPendingTeachersDtoAsync(pageNumber, pageSize);
        
        return new PaginatedResult<PendingTeacherDto>(teachers, totalCount, pageNumber, pageSize);
    }

    public async Task<TeacherDetailsDto?> GetTeacherDetailsAsync(int teacherId)
    {
        return await _teacherRepository.GetTeacherDetailsAsync(teacherId);
    }

    public async Task<bool> ApproveDocumentAsync(int teacherId, int documentId, int adminId)
    {
        var document = await _documentRepository.GetByIdAsync(documentId);
        if (document == null || document.TeacherId != teacherId)
        {
            _logger.LogWarning("Document {DocumentId} not found for teacher {TeacherId}", documentId, teacherId);
            return false;
        }

        document.VerificationStatus = DocumentVerificationStatus.Approved;
        document.ReviewedByAdminId = adminId;
        document.ReviewedAt = DateTime.UtcNow;
        document.RejectionReason = null;

        await _documentRepository.UpdateAsync(document);
        await _documentRepository.SaveChangesAsync();

        await _completionService.SyncSubmissionStatusFromDocumentAsync(
            documentId, DocumentVerificationStatus.Approved, adminId, null);

        _logger.LogInformation("Document {DocumentId} approved by admin {AdminId}", documentId, adminId);

        await _completionService.RefreshTeacherStatusAfterReviewAsync(teacherId);

        return true;
    }

    public async Task<bool> RejectDocumentAsync(int teacherId, int documentId, int adminId, string reason)
    {
        var document = await _documentRepository.GetByIdAsync(documentId);
        if (document == null || document.TeacherId != teacherId)
        {
            _logger.LogWarning("Document {DocumentId} not found for teacher {TeacherId}", documentId, teacherId);
            return false;
        }

        document.VerificationStatus = DocumentVerificationStatus.Rejected;
        document.ReviewedByAdminId = adminId;
        document.ReviewedAt = DateTime.UtcNow;
        document.RejectionReason = reason;

        await _documentRepository.UpdateAsync(document);
        await _documentRepository.SaveChangesAsync();

        await _completionService.SyncSubmissionStatusFromDocumentAsync(
            documentId, DocumentVerificationStatus.Rejected, adminId, reason);

        await _completionService.RefreshTeacherStatusAfterReviewAsync(teacherId);

        _logger.LogInformation("Document {DocumentId} rejected by admin {AdminId}: {Reason}", documentId, adminId, reason);

        await _lifecycleEmailService.SendDocumentRejectedAsync(
            teacherId,
            BuildDocumentLabel(document),
            reason);

        return true;
    }

    public async Task<(bool Success, bool IsBlocked, string Message)> ToggleBlockTeacherAsync(
        int teacherId,
        int adminId,
        string? reason)
    {
        var teacher = await _teacherRepository.GetByIdAsync(teacherId);
        if (teacher == null)
        {
            _logger.LogWarning("Teacher {TeacherId} not found", teacherId);
            return (false, false, "Teacher not found");
        }

        if (teacher.Status == TeacherStatus.Blocked)
        {
            var restoredStatus = teacher.StatusBeforeBlock ?? TeacherStatus.PendingVerification;
            teacher.Status = restoredStatus;
            teacher.IsActive = restoredStatus == TeacherStatus.Active;
            teacher.StatusBeforeBlock = null;

            await _teacherRepository.UpdateAsync(teacher);
            await _teacherRepository.SaveChangesAsync();

            _logger.LogInformation(
                "Teacher {TeacherId} unblocked by admin {AdminId}, restored status {Status}",
                teacherId,
                adminId,
                restoredStatus);

            await _lifecycleEmailService.SendAccountUnblockedAsync(teacherId);

            return (true, false, "Teacher account unblocked successfully");
        }

        teacher.StatusBeforeBlock = teacher.Status;
        teacher.Status = TeacherStatus.Blocked;
        teacher.IsActive = false;

        await _teacherRepository.UpdateAsync(teacher);
        await _teacherRepository.SaveChangesAsync();

        _logger.LogInformation(
            "Teacher {TeacherId} blocked by admin {AdminId}: {Reason}",
            teacherId,
            adminId,
            reason);

        await _lifecycleEmailService.SendAccountBlockedAsync(teacherId, reason);

        return (true, true, "Teacher account blocked successfully");
    }

    public async Task<bool> ReuploadDocumentAsync(int teacherId, int documentId, string newFilePath)
    {
        var document = await _documentRepository.GetByIdAsync(documentId);
        if (document == null || document.TeacherId != teacherId)
        {
            _logger.LogWarning("Document {DocumentId} not found for teacher {TeacherId}", documentId, teacherId);
            return false;
        }

        if (document.VerificationStatus != DocumentVerificationStatus.Rejected)
        {
            _logger.LogWarning("Document {DocumentId} is not rejected, cannot reupload", documentId);
            return false;
        }

        document.FilePath = newFilePath;
        document.VerificationStatus = DocumentVerificationStatus.Pending;
        document.RejectionReason = null;
        document.ReviewedByAdminId = null;
        document.ReviewedAt = null;

        await _documentRepository.UpdateAsync(document);
        await _documentRepository.SaveChangesAsync();

        _logger.LogInformation("Document {DocumentId} reuploaded for teacher {TeacherId}", documentId, teacherId);

        // Update teacher status back to PendingVerification
        await _teacherRepository.UpdateStatusAsync(teacherId, TeacherStatus.PendingVerification);
        await _teacherRepository.SaveChangesAsync();

        return true;
    }

    public async Task<List<TeacherDocumentReviewDto>> GetTeacherDocumentsStatusAsync(int teacherId)
    {
        return await _documentRepository.GetDocumentsStatusAsync(teacherId);
    }

    private static string BuildDocumentLabel(TeacherDocument document)
    {
        if (!string.IsNullOrWhiteSpace(document.CertificateTitle))
            return document.CertificateTitle;

        return document.DocumentType switch
        {
            TeacherDocumentType.IdentityDocument => "Identity document",
            TeacherDocumentType.Certificate => "Certificate",
            _ => "Document"
        };
    }
}
