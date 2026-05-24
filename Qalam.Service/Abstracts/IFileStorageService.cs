using Microsoft.AspNetCore.Http;

namespace Qalam.Service.Abstracts;

public interface IFileStorageService
{
    Task<string> SaveTeacherDocumentAsync(
        IFormFile file,
        int teacherId,
        string documentType);

    Task<bool> ValidateFileAsync(
        IFormFile file,
        string[] allowedExtensions,
        long maxSizeBytes);

    Task<bool> DeleteFileAsync(string filePath);

    Task QueueTeacherDocUploadAsync(
        IFormFile file,
        int teacherId,
        string documentType,
        int documentId);

    Task QueueProfilePicUploadAsync(
        IFormFile file,
        int userId);

    /// <summary>
    /// Queue an Open Session Request attachment for upload. MessagingApi consumer uploads
    /// the file to OSS at <paramref name="storageKey"/>. The API handler is responsible for
    /// having pre-saved the StorageKey + PublicUrl on the attachment row so no cross-DB
    /// write is required from the consumer.
    /// </summary>
    Task QueueOpenSessionRequestAttachmentUploadAsync(
        IFormFile file,
        int openSessionRequestId,
        int attachmentId,
        string storageKey);
}
