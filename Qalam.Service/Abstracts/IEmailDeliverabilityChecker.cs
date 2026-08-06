namespace Qalam.Service.Abstracts;

public interface IEmailDeliverabilityChecker
{
    /// <summary>True when format is valid (MailKit parse).</summary>
    bool IsValidFormat(string? email);

    /// <summary>
    /// Format + MX (or A-record fallback) check. Cached per domain.
    /// </summary>
    Task<EmailDeliverabilityResult> CheckAsync(string? email, CancellationToken cancellationToken = default);
}

public sealed class EmailDeliverabilityResult
{
    public bool IsDeliverable { get; init; }
    public string? NormalizedEmail { get; init; }
    public string? ErrorMessage { get; init; }

    public static EmailDeliverabilityResult Ok(string normalized) => new()
    {
        IsDeliverable = true,
        NormalizedEmail = normalized
    };

    public static EmailDeliverabilityResult Fail(string message) => new()
    {
        IsDeliverable = false,
        ErrorMessage = message
    };
}
