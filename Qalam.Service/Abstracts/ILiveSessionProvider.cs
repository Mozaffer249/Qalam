using Qalam.Data.DTOs.Live;

namespace Qalam.Service.Abstracts;

/// <summary>
/// Vendor-specific live room credential minting.
/// Implement a new class and switch <c>LiveSession:Provider</c> to swap RTC vendors.
/// </summary>
public interface ILiveSessionProvider
{
    string ProviderName { get; }

    Task<LiveSessionAccessDto> CreateAccessAsync(
        LiveSessionAccessRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Ends the RTC room so connected clients disconnect.
    /// Soft-fails (logs) when the vendor is unreachable — callers should still complete DB state.
    /// </summary>
    Task EndRoomAsync(string roomName, CancellationToken cancellationToken = default);
}
