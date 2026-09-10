using Microsoft.Extensions.Configuration;
using Qalam.Service.Implementations;

namespace Qalam.Service.Tests;

public class StoragePublicUrlProviderTests
{
    [Fact]
    public void GetLearningPublicBaseUrl_UsesWasabi_WhenProviderIsWasabi()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["STORAGE_PROVIDER"] = "wasabi",
                ["WasabiSettings:LearningPublicBaseUrl"] =
                    "https://qalam-content-stg.s3.ap-southeast-1.wasabisys.com",
                ["OssSettings:LearningPublicBaseUrl"] =
                    "https://qalam-content-stg.oss-me-central-1.aliyuncs.com",
            })
            .Build();

        var sut = new StoragePublicUrlProvider(config);

        Assert.Equal(
            "https://qalam-content-stg.s3.ap-southeast-1.wasabisys.com",
            sut.GetLearningPublicBaseUrl());
    }

    [Fact]
    public void GetLearningPublicBaseUrl_UsesAlibaba_WhenProviderIsAlibaba()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["StorageSettings:Provider"] = "alibaba",
                ["WasabiSettings:LearningPublicBaseUrl"] =
                    "https://qalam-content-stg.s3.ap-southeast-1.wasabisys.com",
                ["OssSettings:LearningPublicBaseUrl"] =
                    "https://qalam-content-stg.oss-me-central-1.aliyuncs.com",
            })
            .Build();

        var sut = new StoragePublicUrlProvider(config);

        Assert.Equal(
            "https://qalam-content-stg.oss-me-central-1.aliyuncs.com",
            sut.GetLearningPublicBaseUrl());
    }

    [Fact]
    public void GetLearningPublicBaseUrl_FallsBackToLegacyOssChain_WhenProviderUnset()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["OssSettings:LearningPublicBaseUrl"] = "https://cdn.example.com",
            })
            .Build();

        var sut = new StoragePublicUrlProvider(config);

        Assert.Equal("https://cdn.example.com", sut.GetLearningPublicBaseUrl());
    }

    [Fact]
    public void GetIdentitiesPublicBaseUrl_UsesWasabi_WhenProviderIsWasabi()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["STORAGE_PROVIDER"] = "wasabi",
                ["WASABI_PUBLIC_BASE_URL"] =
                    "https://auth-and-identities-certificates-staging.s3.ap-southeast-1.wasabisys.com",
            })
            .Build();

        var sut = new StoragePublicUrlProvider(config);

        Assert.Equal(
            "https://auth-and-identities-certificates-staging.s3.ap-southeast-1.wasabisys.com",
            sut.GetIdentitiesPublicBaseUrl());
    }
}
