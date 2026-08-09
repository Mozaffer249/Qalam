using Qalam.Data.DTOs.Legal;

namespace Qalam.Service.Abstracts;

public interface ILegalConsentService
{
    Task<List<PendingConsentDocumentDto>> GetPendingAsync(int userId, CancellationToken cancellationToken = default);

    Task AcceptAsync(
        int userId,
        IReadOnlyList<string>? documentCodes,
        string? source,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Records consent for all RequiresConsent published documents and stamps User.TermsAcceptedAt.
    /// Idempotent per (user, version).
    /// </summary>
    Task AcceptRequiredAsync(
        int userId,
        string? source,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default);
}
