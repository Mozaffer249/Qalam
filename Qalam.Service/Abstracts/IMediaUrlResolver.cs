namespace Qalam.Service.Abstracts;

/// <summary>
/// Turns stored media paths (relative local uploads or absolute OSS URLs) into client-usable public URLs.
/// </summary>
public interface IMediaUrlResolver
{
    /// <summary>
    /// Absolute http(s) URL for browsers/apps, or null when <paramref name="storedPath"/> is empty.
    /// Relative paths like <c>uploads/courses/…</c> are prefixed with the API public base.
    /// </summary>
    string? ToPublicUrl(string? storedPath);
}
