using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Qalam.Data.Entity.Messaging;
using Qalam.Service.Abstracts;

namespace Qalam.Service.Implementations;

public class FileStorageService : IFileStorageService
{
    private readonly ILogger<FileStorageService> _logger;
    private readonly IRabbitMQService _rabbitMQService;
    private readonly string _uploadPath;

    public FileStorageService(ILogger<FileStorageService> logger, IRabbitMQService rabbitMQService)
    {
        _logger = logger;
        _rabbitMQService = rabbitMQService;
        _uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "uploads", "teachers");

        // Ensure directory exists
        if (!Directory.Exists(_uploadPath))
        {
            Directory.CreateDirectory(_uploadPath);
        }
    }

    public async Task<string> SaveTeacherDocumentAsync(
        IFormFile file,
        int teacherId,
        string documentType)
    {
        // Create teacher-specific directory
        var teacherPath = Path.Combine(_uploadPath, teacherId.ToString(), documentType);
        if (!Directory.Exists(teacherPath))
        {
            Directory.CreateDirectory(teacherPath);
        }

        // Generate unique filename
        var extension = Path.GetExtension(file.FileName);
        var fileName = $"{Guid.NewGuid()}{extension}";
        var filePath = Path.Combine(teacherPath, fileName);

        // Save file
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        _logger.LogInformation(
            "File saved for teacher {TeacherId}, type {Type}: {Path}",
            teacherId,
            documentType,
            filePath);

        // Return relative path for database storage
        return Path.Combine("uploads", "teachers", teacherId.ToString(), documentType, fileName);
    }

    public async Task<string> SaveCourseImageAsync(IFormFile file, int teacherId)
    {
        var coursePath = Path.Combine(Directory.GetCurrentDirectory(), "uploads", "courses", teacherId.ToString());
        if (!Directory.Exists(coursePath))
        {
            Directory.CreateDirectory(coursePath);
        }

        var extension = Path.GetExtension(file.FileName);
        var fileName = $"{Guid.NewGuid()}{extension}";
        var filePath = Path.Combine(coursePath, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var relative = string.Join('/', "uploads", "courses", teacherId.ToString(), fileName);
        _logger.LogInformation(
            "Course image saved for teacher {TeacherId}: {Path}",
            teacherId,
            relative);

        return relative;
    }

    public Task<bool> ValidateFileAsync(
        IFormFile file,
        string[] allowedExtensions,
        long maxSizeBytes)
    {
        if (file == null || file.Length == 0)
        {
            _logger.LogWarning("File validation failed: File is null or empty");
            return Task.FromResult(false);
        }

        // Check file size
        if (file.Length > maxSizeBytes)
        {
            _logger.LogWarning(
                "File validation failed: File size {Size} exceeds limit {Limit}",
                file.Length,
                maxSizeBytes);
            return Task.FromResult(false);
        }

        // Check file extension
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(extension))
        {
            _logger.LogWarning(
                "File validation failed: Extension {Extension} not allowed",
                extension);
            return Task.FromResult(false);
        }

        return Task.FromResult(true);
    }

    public Task<bool> DeleteFileAsync(string filePath)
    {
        try
        {
            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), filePath);
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
                _logger.LogInformation("File deleted: {Path}", filePath);
                return Task.FromResult(true);
            }

            _logger.LogWarning("File not found for deletion: {Path}", filePath);
            return Task.FromResult(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file: {Path}", filePath);
            return Task.FromResult(false);
        }
    }

    public async Task QueueTeacherDocUploadAsync(
        IFormFile file,
        int teacherId,
        string documentType,
        int documentId)
    {
        using var memoryStream = new MemoryStream();
        await file.CopyToAsync(memoryStream);
        var base64Data = Convert.ToBase64String(memoryStream.ToArray());

        var message = new TeacherDocUploadMessage
        {
            TeacherId = teacherId,
            DocumentType = documentType,
            FileName = file.FileName,
            ContentType = file.ContentType,
            FileData = base64Data,
            DocumentId = documentId
        };

        await _rabbitMQService.QueueTeacherDocUploadAsync(message);

        _logger.LogInformation(
            "Teacher doc upload queued: TeacherId={TeacherId}, DocId={DocumentId}, Type={Type}",
            teacherId, documentId, documentType);
    }

    public async Task QueueProfilePicUploadAsync(
        IFormFile file,
        int userId)
    {
        using var memoryStream = new MemoryStream();
        await file.CopyToAsync(memoryStream);
        var base64Data = Convert.ToBase64String(memoryStream.ToArray());

        var message = new ProfilePicUploadMessage
        {
            UserId = userId,
            FileName = file.FileName,
            ContentType = file.ContentType,
            FileData = base64Data
        };

        await _rabbitMQService.QueueProfilePicUploadAsync(message);

        _logger.LogInformation("Profile pic upload queued: UserId={UserId}", userId);
    }

    public async Task QueueOpenSessionRequestAttachmentUploadAsync(
        IFormFile file,
        int openSessionRequestId,
        int attachmentId,
        string storageKey)
    {
        using var memoryStream = new MemoryStream();
        await file.CopyToAsync(memoryStream);
        var base64Data = Convert.ToBase64String(memoryStream.ToArray());

        var message = new OpenSessionRequestAttachmentUploadMessage
        {
            OpenSessionRequestId = openSessionRequestId,
            AttachmentId = attachmentId,
            FileName = file.FileName,
            ContentType = file.ContentType,
            StorageKey = storageKey,
            FileData = base64Data
        };

        await _rabbitMQService.QueueOpenSessionRequestAttachmentUploadAsync(message);

        _logger.LogInformation(
            "Open session request attachment queued: RequestId={RequestId}, AttachmentId={AttachmentId}, Key={Key}",
            openSessionRequestId, attachmentId, storageKey);
    }

    public async Task QueueTeacherContentFileUploadAsync(
        IFormFile file,
        int teacherId,
        int contentItemId,
        string storageKey)
    {
        using var memoryStream = new MemoryStream();
        await file.CopyToAsync(memoryStream);
        var base64Data = Convert.ToBase64String(memoryStream.ToArray());

        var message = new TeacherContentFileUploadMessage
        {
            TeacherId = teacherId,
            ContentItemId = contentItemId,
            FileName = file.FileName,
            ContentType = file.ContentType ?? "application/octet-stream",
            StorageKey = storageKey,
            FileData = base64Data,
        };

        await _rabbitMQService.QueueTeacherContentFileUploadAsync(message);

        _logger.LogInformation(
            "Teacher content file upload queued: TeacherId={TeacherId}, ItemId={ItemId}, Key={Key}",
            teacherId, contentItemId, storageKey);
    }

    public async Task<string> SaveTeacherContentFileAsync(IFormFile file, int teacherId, int itemId)
    {
        var contentPath = Path.Combine(Directory.GetCurrentDirectory(), "uploads", "teacher-content", teacherId.ToString());
        if (!Directory.Exists(contentPath))
            Directory.CreateDirectory(contentPath);

        var extension = Path.GetExtension(file.FileName);
        var fileName = $"{itemId}{extension}";
        var filePath = Path.Combine(contentPath, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var relative = Path.Combine("uploads", "teacher-content", teacherId.ToString(), fileName);
        _logger.LogInformation("Teacher content file saved: TeacherId={TeacherId}, ItemId={ItemId}, Path={Path}",
            teacherId, itemId, relative);
        return relative;
    }
}
