using Microsoft.EntityFrameworkCore;
using MimeKit;
using Qalam.MessagingApi.Data;
using Qalam.MessagingApi.Models.Entities;
using Qalam.MessagingApi.Models.Enums;
using Qalam.MessagingApi.Services.Interfaces;

namespace Qalam.MessagingApi.Services;

public class EmailSuppressionService : IEmailSuppressionService
{
    private readonly MessagingDbContext _db;
    private readonly ILogger<EmailSuppressionService> _logger;

    public EmailSuppressionService(MessagingDbContext db, ILogger<EmailSuppressionService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public static string? NormalizeEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return null;
        if (!MailboxAddress.TryParse(email.Trim(), out var mailbox))
            return email.Trim().ToLowerInvariant();
        return mailbox.Address.Trim().ToLowerInvariant();
    }

    public async Task<bool> IsSuppressedAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeEmail(email);
        if (normalized == null)
            return false;

        return await _db.EmailSuppressions
            .AsNoTracking()
            .AnyAsync(e => e.Email == normalized, cancellationToken);
    }

    public async Task SuppressAsync(
        string email,
        EmailSuppressionReason reason,
        EmailSuppressionSource source,
        string? diagnostic = null,
        CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeEmail(email);
        if (normalized == null)
            return;

        var existing = await _db.EmailSuppressions
            .FirstOrDefaultAsync(e => e.Email == normalized, cancellationToken);

        var now = DateTime.UtcNow;
        if (existing == null)
        {
            _db.EmailSuppressions.Add(new EmailSuppression
            {
                Email = normalized,
                Reason = reason,
                Source = source,
                Diagnostic = Truncate(diagnostic, 2000),
                BounceCount = 1,
                CreatedAt = now,
                LastBounceAt = now
            });
            _logger.LogInformation(
                "Email suppressed: {Email} reason={Reason} source={Source}",
                normalized, reason, source);
        }
        else
        {
            existing.BounceCount += 1;
            existing.LastBounceAt = now;
            existing.Reason = reason;
            existing.Source = source;
            if (!string.IsNullOrWhiteSpace(diagnostic))
                existing.Diagnostic = Truncate(diagnostic, 2000);
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    private static string? Truncate(string? value, int max)
        => string.IsNullOrEmpty(value) || value.Length <= max ? value : value[..max];
}
