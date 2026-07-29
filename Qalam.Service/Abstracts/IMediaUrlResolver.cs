namespace Qalam.Service.Abstracts;

/// <summary>
/// Turns stored media paths (relative local uploads or absolute OSS URLs) into client-usable public URLs.
/// </summary>
public interface IMediaUrlResolver
{
    /// <summary>
    /// Absolute http(s) URL for browsers/apps, or null when <paramref name="storedPath"/> is empty.
    /// Already-absolute OSS/API URLs are returned unchanged. Legacy relative paths
    /// (e.g. <c>uploads/…</c>) are prefixed with the API public base.
    /// </summary>
    string? ToPublicUrl(string? storedPath);
}
