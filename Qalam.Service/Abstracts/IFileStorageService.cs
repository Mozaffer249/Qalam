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
}
