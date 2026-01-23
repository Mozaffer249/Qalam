using Microsoft.Extensions.Logging;
using Qalam.Data.DTOs.Admin;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Results;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Service.Implementations;

public class TeacherManagementService : ITeacherManagementService
{
    private readonly ITeacherRepository _teacherRepository;
    private readonly ITeacherDocumentRepository _documentRepository;
    private readonly ILogger<TeacherManagementService> _logger;

    public TeacherManagementService(
        ITeacherRepository teacherRepository,
        ITeacherDocumentRepository documentRepository,
        ILogger<TeacherManagementService> logger)
    {
        _teacherRepository = teacherRepository;
        _documentRepository = documentRepository;
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

        _logger.LogInformation("Document {DocumentId} approved by admin {AdminId}", documentId, adminId);

        // Check if all documents are now approved
        await UpdateTeacherStatusAfterReviewAsync(teacherId);

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

        _logger.LogInformation("Document {DocumentId} rejected by admin {AdminId}: {Reason}", documentId, adminId, reason);

        // Update teacher status to DocumentsRejected
        await _teacherRepository.UpdateStatusAsync(teacherId, TeacherStatus.DocumentsRejected);
        await _teacherRepository.SaveChangesAsync();

        return true;
    }

    public async Task<bool> BlockTeacherAsync(int teacherId, int adminId, string? reason)
    {
        var teacher = await _teacherRepository.GetByIdAsync(teacherId);
        if (teacher == null)
        {
            _logger.LogWarning("Teacher {TeacherId} not found", teacherId);
            return false;
        }

        teacher.Status = TeacherStatus.Blocked;
        teacher.IsActive = false;

        await _teacherRepository.UpdateAsync(teacher);
        await _teacherRepository.SaveChangesAsync();

        _logger.LogInformation("Teacher {TeacherId} blocked by admin {AdminId}: {Reason}", teacherId, adminId, reason);

        return true;
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

    private async Task UpdateTeacherStatusAfterReviewAsync(int teacherId)
    {
        var documents = await _documentRepository.GetByTeacherIdAsync(teacherId);
        
        if (!documents.Any())
            return;

        var hasRejected = documents.Any(d => d.VerificationStatus == DocumentVerificationStatus.Rejected);
        var hasPending = documents.Any(d => d.VerificationStatus == DocumentVerificationStatus.Pending);
        var allApproved = documents.All(d => d.VerificationStatus == DocumentVerificationStatus.Approved);

        TeacherStatus newStatus;
        if (hasRejected)
        {
            newStatus = TeacherStatus.DocumentsRejected;
        }
        else if (allApproved)
        {
            newStatus = TeacherStatus.Active;
            _logger.LogInformation("All documents approved for teacher {TeacherId}, activating account", teacherId);
        }
        else if (hasPending)
        {
            newStatus = TeacherStatus.PendingVerification;
        }
        else
        {
            return; // No change needed
        }

        await _teacherRepository.UpdateStatusAsync(teacherId, newStatus);
        await _teacherRepository.SaveChangesAsync();
    }
}
