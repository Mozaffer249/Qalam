namespace Qalam.Service.Abstracts;

/// <summary>
/// Resolves the public base URL for object storage uploads so API-side precompute
/// matches MessagingApi's active <c>STORAGE_PROVIDER</c>.
/// </summary>
public interface IStoragePublicUrlProvider
{
    string GetLearningPublicBaseUrl();
    string GetIdentitiesPublicBaseUrl();
}
