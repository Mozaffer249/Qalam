using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MimeKit;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Messaging;
using Qalam.Infrastructure.context;
using Qalam.Service.Abstracts;

namespace Qalam.Service.Implementations;

public class EmailSuppressionService : IEmailSuppressionService
{
    private readonly ApplicationDBContext _db;
    private readonly ILogger<EmailSuppressionService> _logger;

    public EmailSuppressionService(ApplicationDBContext db, ILogger<EmailSuppressionService> logger)
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
            _logger.LogInformation(
                "Email suppression updated: {Email} bounceCount={Count}",
                normalized, existing.BounceCount);
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> SeedAsync(
        IEnumerable<string> emails,
        EmailSuppressionReason reason,
        EmailSuppressionSource source,
        string? diagnostic = null,
        CancellationToken cancellationToken = default)
    {
        var added = 0;
        foreach (var raw in emails.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var normalized = NormalizeEmail(raw);
            if (normalized == null)
                continue;

            var exists = await _db.EmailSuppressions
                .AnyAsync(e => e.Email == normalized, cancellationToken);
            if (exists)
                continue;

            await SuppressAsync(normalized, reason, source, diagnostic, cancellationToken);
            added++;
        }

        return added;
    }

    private static string? Truncate(string? value, int max)
    {
        if (string.IsNullOrEmpty(value))
            return value;
        return value.Length <= max ? value : value[..max];
    }
}
