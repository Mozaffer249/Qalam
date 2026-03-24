using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using Qalam.MessagingApi.Configuration;
using Qalam.MessagingApi.Services.Interfaces;

namespace Qalam.MessagingApi.Services;

public class WasabiStorageService : IWasabiStorageService, IDisposable
{
    private readonly WasabiSettings _settings;
    private readonly ILogger<WasabiStorageService> _logger;
    private readonly AmazonS3Client _s3Client;

    public WasabiStorageService(IOptions<WasabiSettings> settings, ILogger<WasabiStorageService> logger)
    {
        _settings = settings.Value;
        _logger = logger;

        var config = new AmazonS3Config
        {
            ServiceURL = _settings.ServiceUrl,
            ForcePathStyle = true
        };

        _s3Client = new AmazonS3Client(_settings.AccessKey, _settings.SecretKey, config);
    }

    public async Task<string> UploadFileAsync(string key, Stream stream, string contentType)
    {
        try
        {
            var putRequest = new PutObjectRequest
            {
                BucketName = _settings.BucketName,
                Key = key,
                InputStream = stream,
                ContentType = contentType
            };

            await _s3Client.PutObjectAsync(putRequest);

            var fileUrl = $"{_settings.ServiceUrl}/{_settings.BucketName}/{key}";
            _logger.LogInformation("File uploaded to Wasabi: {Key} → {Url}", key, fileUrl);
            return fileUrl;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload file to Wasabi: {Key}", key);
            throw;
        }
    }

    public async Task DeleteFileAsync(string fileUrl)
    {
        try
        {
            var key = ExtractKeyFromUrl(fileUrl);
            if (string.IsNullOrEmpty(key)) return;

            var deleteRequest = new DeleteObjectRequest
            {
                BucketName = _settings.BucketName,
                Key = key
            };

            await _s3Client.DeleteObjectAsync(deleteRequest);
            _logger.LogInformation("File deleted from Wasabi: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete file from Wasabi: {Url}", fileUrl);
            throw;
        }
    }

    private string? ExtractKeyFromUrl(string fileUrl)
    {
        var prefix = $"{_settings.ServiceUrl}/{_settings.BucketName}/";
        if (fileUrl.StartsWith(prefix))
            return fileUrl[prefix.Length..];

        // Handle legacy relative paths
        if (!fileUrl.StartsWith("http"))
            return fileUrl.TrimStart('/');

        return null;
    }

    public void Dispose()
    {
        _s3Client.Dispose();
    }
}
