using System.Collections.Concurrent;
using DnsClient;
using Microsoft.Extensions.Logging;
using MimeKit;
using Qalam.Service.Abstracts;

namespace Qalam.Service.Implementations;

public class EmailDeliverabilityChecker : IEmailDeliverabilityChecker
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(30);
    private readonly ConcurrentDictionary<string, CacheEntry> _domainCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly ILookupClient _lookupClient;
    private readonly ILogger<EmailDeliverabilityChecker> _logger;

    public EmailDeliverabilityChecker(ILogger<EmailDeliverabilityChecker> logger)
        : this(new LookupClient(), logger)
    {
    }

    internal EmailDeliverabilityChecker(ILookupClient lookupClient, ILogger<EmailDeliverabilityChecker> logger)
    {
        _lookupClient = lookupClient;
        _logger = logger;
    }

    public bool IsValidFormat(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;
        return MailboxAddress.TryParse(email.Trim(), out _);
    }

    public async Task<EmailDeliverabilityResult> CheckAsync(
        string? email,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
            return EmailDeliverabilityResult.Fail("Email is required.");

        var trimmed = email.Trim();
        if (!MailboxAddress.TryParse(trimmed, out var mailbox))
            return EmailDeliverabilityResult.Fail("Email format is invalid.");

        var normalized = mailbox.Address.Trim().ToLowerInvariant();
        var at = normalized.LastIndexOf('@');
        if (at <= 0 || at == normalized.Length - 1)
            return EmailDeliverabilityResult.Fail("Email format is invalid.");

        var domain = normalized[(at + 1)..];

        // Synthetic local addresses used for phone-only accounts — not deliverable.
        if (domain.Equals("phone.qalam.local", StringComparison.OrdinalIgnoreCase)
            || domain.EndsWith(".qalam.local", StringComparison.OrdinalIgnoreCase))
        {
            return EmailDeliverabilityResult.Fail("Email domain is not deliverable.");
        }

        var hasMx = await DomainAcceptsMailAsync(domain, cancellationToken);
        if (!hasMx)
            return EmailDeliverabilityResult.Fail("Email domain does not accept mail.");

        return EmailDeliverabilityResult.Ok(normalized);
    }

    private async Task<bool> DomainAcceptsMailAsync(string domain, CancellationToken cancellationToken)
    {
        if (_domainCache.TryGetValue(domain, out var cached) && cached.ExpiresAt > DateTime.UtcNow)
            return cached.AcceptsMail;

        bool accepts;
        try
        {
            var mx = await _lookupClient.QueryAsync(domain, QueryType.MX);
            accepts = mx.Answers.MxRecords().Any(r => !string.IsNullOrWhiteSpace(r.Exchange?.Value));

            if (!accepts)
            {
                // RFC 5321: fall back to A/AAAA if no MX
                var a = await _lookupClient.QueryAsync(domain, QueryType.A);
                var aaaa = await _lookupClient.QueryAsync(domain, QueryType.AAAA);
                accepts = a.Answers.ARecords().Any() || aaaa.Answers.AaaaRecords().Any();
            }
        }
        catch (Exception ex)
        {
            // Fail open on DNS infrastructure errors so signup isn't blocked by transient DNS outages.
            _logger.LogWarning(ex, "DNS lookup failed for domain {Domain}; treating as deliverable", domain);
            accepts = true;
        }

        _domainCache[domain] = new CacheEntry(accepts, DateTime.UtcNow.Add(CacheTtl));
        return accepts;
    }

    private sealed record CacheEntry(bool AcceptsMail, DateTime ExpiresAt);
}
