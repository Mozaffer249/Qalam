namespace Qalam.Service.Abstracts;

public sealed class UserProfilePictureUpdateResult
{
    public bool Succeeded { get; private init; }
    public bool NotFound { get; private init; }
    public string? Error { get; private init; }
    public string? ProfilePictureUrl { get; private init; }

    public static UserProfilePictureUpdateResult Ok(string? profilePictureUrl) => new()
    {
        Succeeded = true,
        ProfilePictureUrl = profilePictureUrl,
    };

    public static UserProfilePictureUpdateResult Fail(string error) => new()
    {
        Succeeded = false,
        Error = error,
    };

    public static UserProfilePictureUpdateResult FailNotFound(string error) => new()
    {
        Succeeded = false,
        NotFound = true,
        Error = error,
    };
}
