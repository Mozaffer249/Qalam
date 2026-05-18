using Qalam.Data.DTOs.Auth;

namespace Qalam.Service.Abstracts;

public interface IAuthSettingsProvider
{
    Task<AuthSettingsDto> GetSettingsAsync(CancellationToken cancellationToken = default);
    Task<AuthSettingsDto> SaveSettingsAsync(AuthSettingsDto settings, CancellationToken cancellationToken = default);
    AuthConfigResponseDto ToPublicConfig(AuthSettingsDto settings);
}
