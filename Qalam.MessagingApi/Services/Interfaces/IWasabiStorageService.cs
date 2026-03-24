namespace Qalam.MessagingApi.Services.Interfaces;

public interface IWasabiStorageService
{
    Task<string> UploadFileAsync(string key, Stream stream, string contentType);
    Task DeleteFileAsync(string fileUrl);
}
