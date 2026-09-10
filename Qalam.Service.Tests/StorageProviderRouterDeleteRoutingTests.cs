using Qalam.MessagingApi.Configuration;
using Qalam.MessagingApi.Services;

namespace Qalam.Service.Tests;

public class StorageProviderRouterDeleteRoutingTests
{
    [Theory]
    [InlineData(
        "https://auth-and-identities-certificates-staging.s3.ap-southeast-1.wasabisys.com/teachers/1/a.pdf",
        StorageProvider.Alibaba,
        StorageProvider.Wasabi)]
    [InlineData(
        "https://qalam-content-stg.oss-me-central-1.aliyuncs.com/courses/1/b.jpg",
        StorageProvider.Wasabi,
        StorageProvider.Alibaba)]
    [InlineData(
        "uploads/teachers/1/doc.pdf",
        StorageProvider.Wasabi,
        StorageProvider.Wasabi)]
    [InlineData(
        "uploads/teachers/1/doc.pdf",
        StorageProvider.Alibaba,
        StorageProvider.Alibaba)]
    public void ResolveDeleteTarget_RoutesByHost_OrFallsBackToActive(
        string url,
        StorageProvider active,
        StorageProvider expected)
    {
        Assert.Equal(expected, StorageProviderRouter.ResolveDeleteTarget(url, active));
    }
}
