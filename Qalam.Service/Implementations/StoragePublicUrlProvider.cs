using Microsoft.Extensions.Configuration;
using Qalam.Service.Abstracts;

namespace Qalam.Service.Implementations;

public class StoragePublicUrlProvider : IStoragePublicUrlProvider
{
    private readonly IConfiguration _configuration;

    public StoragePublicUrlProvider(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GetLearningPublicBaseUrl()
    {
        if (IsWasabi())
        {
            var wasabi = FirstNonEmpty(
                _configuration["WasabiSettings:LearningPublicBaseUrl"],
                _configuration["WASABI_LEARNING_PUBLIC_BASE_URL"],
                _configuration["WasabiSettings:PublicBaseUrl"],
                _configuration["WASABI_PUBLIC_BASE_URL"]);
            if (!string.IsNullOrWhiteSpace(wasabi))
                return wasabi.TrimEnd('/');
        }

        // Alibaba path + legacy fallback used by tests that only set OssSettings:*
        return FirstNonEmpty(
            _configuration["OssSettings:LearningPublicBaseUrl"],
            _configuration["OSS_LEARNING_PUBLIC_BASE_URL"],
            _configuration["OssSettings:PublicBaseUrl"],
            _configuration["OSS_PUBLIC_BASE_URL"])?.TrimEnd('/') ?? string.Empty;
    }

    public string GetIdentitiesPublicBaseUrl()
    {
        if (IsWasabi())
        {
            var wasabi = FirstNonEmpty(
                _configuration["WasabiSettings:PublicBaseUrl"],
                _configuration["WASABI_PUBLIC_BASE_URL"]);
            if (!string.IsNullOrWhiteSpace(wasabi))
                return wasabi.TrimEnd('/');
        }

        return FirstNonEmpty(
            _configuration["OssSettings:PublicBaseUrl"],
            _configuration["OSS_PUBLIC_BASE_URL"])?.TrimEnd('/') ?? string.Empty;
    }

    private bool IsWasabi()
    {
        var provider = FirstNonEmpty(
            _configuration["StorageSettings:Provider"],
            _configuration["STORAGE_PROVIDER"]);
        return string.Equals(provider, "wasabi", StringComparison.OrdinalIgnoreCase);
    }

    private static string? FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
                return value;
        }

        return null;
    }
}
