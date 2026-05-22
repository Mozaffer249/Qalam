namespace Qalam.MessagingApi.Services.Interfaces;

public interface IObjectStorageService
{
    Task<string> UploadFileAsync(string key, Stream stream, string contentType);
    Task DeleteFileAsync(string fileUrl);
}
