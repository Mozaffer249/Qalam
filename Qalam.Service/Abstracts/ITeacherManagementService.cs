using Qalam.Data.DTOs.Admin;
using Qalam.Data.Results;

namespace Qalam.Service.Abstracts;

public interface ITeacherManagementService
{
    // Admin operations
    Task<PaginatedResult<PendingTeacherDto>> GetPendingTeachersAsync(int pageNumber, int pageSize);
    Task<TeacherDetailsDto?> GetTeacherDetailsAsync(int teacherId);
    Task<bool> ApproveDocumentAsync(int teacherId, int documentId, int adminId);
    Task<bool> RejectDocumentAsync(int teacherId, int documentId, int adminId, string reason);
    Task<(bool Success, bool IsBlocked, string Message)> ToggleBlockTeacherAsync(int teacherId, int adminId, string? reason);

    /// <summary>
    /// Hard-deletes the teacher, linked Identity user, and teacher-owned related data.
    /// </summary>
    Task<(bool Success, string Message)> DeleteTeacherAccountAsync(
        int teacherId,
        int adminId,
        string? reason,
        CancellationToken cancellationToken = default);
    
    // Teacher operations
    Task<bool> ReuploadDocumentAsync(int teacherId, int documentId, string newFilePath);
    Task<List<TeacherDocumentReviewDto>> GetTeacherDocumentsStatusAsync(int teacherId);
}
