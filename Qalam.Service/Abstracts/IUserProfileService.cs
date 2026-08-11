using Microsoft.AspNetCore.Http;
using Qalam.Data.DTOs.Auth;

namespace Qalam.Service.Abstracts;

public interface IUserProfileService
{
    /// <summary>
    /// Queues a profile picture upload for the authenticated user (OSS via MessagingApi).
    /// </summary>
    Task<UserProfilePictureUpdateResult> UpdateProfilePictureAsync(
        int userId,
        IFormFile file,
        CancellationToken cancellationToken = default);

    Task<RelatedAccountsDto> GetRelatedAccountsAsync(
        int userId,
        CancellationToken cancellationToken = default);
}
