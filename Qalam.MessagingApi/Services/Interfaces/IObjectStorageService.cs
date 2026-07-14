namespace Qalam.MessagingApi.Services.Interfaces;

public interface IObjectStorageService
{
    /// <summary>Upload to the identities bucket (default).</summary>
    Task<string> UploadFileAsync(string key, Stream stream, string contentType);

    /// <summary>Upload to a named logical bucket (<see cref="Configuration.OssBucketKeys"/>).</summary>
    Task<string> UploadFileAsync(string key, Stream stream, string contentType, string bucketKey);

    Task DeleteFileAsync(string fileUrl);
}
