using Microsoft.Extensions.Options;
using Qalam.MessagingApi.Configuration;
using Qalam.MessagingApi.Services.Interfaces;

namespace Qalam.MessagingApi.Services;

/// <summary>
/// Routes uploads to the configured active provider; routes deletes by URL host so
/// objects left on the previous provider remain deletable after a cutover.
/// </summary>
public class StorageProviderRouter : IObjectStorageService
{
    private readonly StorageSettings _storageSettings;
    private readonly OssStorageService _alibaba;
    private readonly WasabiStorageService _wasabi;
    private readonly ILogger<StorageProviderRouter> _logger;

    public StorageProviderRouter(
        IOptions<StorageSettings> storageSettings,
        OssStorageService alibaba,
        WasabiStorageService wasabi,
        ILogger<StorageProviderRouter> logger)
    {
        _storageSettings = storageSettings.Value;
        _alibaba = alibaba;
        _wasabi = wasabi;
        _logger = logger;

        _logger.LogInformation(
            "Object storage active provider={Provider}",
            _storageSettings.ActiveProvider);
    }

    private IObjectStorageService ActiveUpload =>
        _storageSettings.ActiveProvider == StorageProvider.Wasabi ? _wasabi : _alibaba;

    public Task<string> UploadFileAsync(string key, Stream stream, string contentType)
        => ActiveUpload.UploadFileAsync(key, stream, contentType);

    public Task<string> UploadFileAsync(string key, Stream stream, string contentType, string bucketKey)
        => ActiveUpload.UploadFileAsync(key, stream, contentType, bucketKey);

    public Task DeleteFileAsync(string fileUrl)
    {
        var target = ResolveDeleteTarget(fileUrl);
        _logger.LogDebug("Delete routed to {Provider} for {Url}", target, fileUrl);
        return target == StorageProvider.Wasabi
            ? _wasabi.DeleteFileAsync(fileUrl)
            : _alibaba.DeleteFileAsync(fileUrl);
    }

    /// <summary>Exposed for unit tests and host-based delete routing.</summary>
    public static StorageProvider ResolveDeleteTarget(string fileUrl, StorageProvider activeProvider)
    {
        if (Uri.TryCreate(fileUrl, UriKind.Absolute, out var uri))
        {
            if (uri.Host.Contains("wasabisys.com", StringComparison.OrdinalIgnoreCase))
                return StorageProvider.Wasabi;
            if (uri.Host.Contains("aliyuncs.com", StringComparison.OrdinalIgnoreCase))
                return StorageProvider.Alibaba;
        }

        return activeProvider;
    }

    private StorageProvider ResolveDeleteTarget(string fileUrl)
        => ResolveDeleteTarget(fileUrl, _storageSettings.ActiveProvider);
}
