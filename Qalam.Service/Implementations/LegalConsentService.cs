using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Qalam.Data.DTOs.Legal;
using Qalam.Data.Entity.Identity;
using Qalam.Data.Entity.Legal;
using Qalam.Infrastructure.Abstracts;
using Qalam.Infrastructure.context;
using Qalam.Service.Abstracts;
using Qalam.Service.Helpers;

namespace Qalam.Service.Implementations;

public class LegalConsentService : ILegalConsentService
{
    private readonly ILegalConsentRepository _consents;
    private readonly ILegalDocumentRepository _documents;
    private readonly UserManager<User> _userManager;
    private readonly ApplicationDBContext _db;

    public LegalConsentService(
        ILegalConsentRepository consents,
        ILegalDocumentRepository documents,
        UserManager<User> userManager,
        ApplicationDBContext db)
    {
        _consents = consents;
        _documents = documents;
        _userManager = userManager;
        _db = db;
    }

    public async Task<List<PendingConsentDocumentDto>> GetPendingAsync(int userId, CancellationToken cancellationToken = default)
    {
        var pending = await _consents.GetPendingConsentDocumentsAsync(userId, cancellationToken);
        return pending.Select(LegalDocumentMapper.ToPendingConsent).ToList();
    }

    public Task AcceptRequiredAsync(
        int userId,
        string? source,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default) =>
        AcceptAsync(userId, null, source, ipAddress, userAgent, cancellationToken);

    public async Task AcceptAsync(
        int userId,
        IReadOnlyList<string>? documentCodes,
        string? source,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        var required = await _db.LegalDocuments
            .Include(d => d.CurrentPublishedVersion)
            .Where(d => d.IsActive && d.RequiresConsent && d.CurrentPublishedVersionId != null)
            .ToListAsync(cancellationToken);

        if (documentCodes is { Count: > 0 })
        {
            var codes = documentCodes.Select(c => c.Trim().ToLowerInvariant()).ToHashSet();
            required = required.Where(d => codes.Contains(d.Code.ToLowerInvariant())).ToList();
        }

        if (required.Count == 0)
            return;

        var versionIds = required.Select(d => d.CurrentPublishedVersionId!.Value).ToList();
        var alreadyAccepted = await _db.UserLegalConsents
            .Where(c => c.UserId == userId && versionIds.Contains(c.LegalDocumentVersionId))
            .Select(c => c.LegalDocumentVersionId)
            .ToListAsync(cancellationToken);
        var acceptedSet = alreadyAccepted.ToHashSet();

        var now = DateTime.UtcNow;
        foreach (var doc in required)
        {
            var versionId = doc.CurrentPublishedVersionId!.Value;
            if (acceptedSet.Contains(versionId))
                continue;

            await _db.UserLegalConsents.AddAsync(new UserLegalConsent
            {
                UserId = userId,
                LegalDocumentId = doc.Id,
                LegalDocumentVersionId = versionId,
                AcceptedAt = now,
                IpAddress = Truncate(ipAddress, 50),
                UserAgent = Truncate(userAgent, 500),
                Source = Truncate(source, 50),
                CreatedAt = now
            }, cancellationToken);
        }

        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user != null && user.TermsAcceptedAt == null)
        {
            user.TermsAcceptedAt = now;
            await _userManager.UpdateAsync(user);
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    private static string? Truncate(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        value = value.Trim();
        return value.Length <= max ? value : value[..max];
    }
}
