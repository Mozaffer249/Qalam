using Microsoft.Extensions.Logging;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Service.Abstracts;

/// <summary>Best-effort FCM device token registration during login / verify OTP.</summary>
public interface IAuthDeviceTokenRegistrar
{
    Task TryRegisterAsync(
        int userId,
        string? deviceToken,
        string? platform,
        string? appVersion,
        CancellationToken cancellationToken = default);
}
