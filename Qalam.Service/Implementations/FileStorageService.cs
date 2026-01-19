using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Qalam.Service.Abstracts;

namespace Qalam.Service.Implementations;

public class FileStorageService : IFileStorageService
{
    private readonly ILogger<FileStorageService> _logger;
    private readonly string _uploadPath;

    public FileStorageService(ILogger<FileStorageService> logger)
    {
        _logger = logger;
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
}
