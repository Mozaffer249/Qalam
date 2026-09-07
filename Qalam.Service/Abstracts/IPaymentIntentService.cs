using Qalam.Data.DTOs.Payment;

namespace Qalam.Service.Abstracts;

public interface IPaymentIntentService
{
    /// <summary>
    /// Creates a Pending Payment for the participant against the active gateway
    /// and returns the client checkout payload.
    /// </summary>
    Task<PaymentIntentServiceResult> CreateAsync(
        int participantId,
        int userId,
        CancellationToken cancellationToken = default);
}

public sealed class PaymentIntentServiceResult
{
    public bool Succeeded { get; init; }
    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }
    public PaymentIntentDto? Intent { get; init; }

    public static PaymentIntentServiceResult Ok(PaymentIntentDto intent) => new()
    {
        Succeeded = true,
        Intent = intent
    };

    public static PaymentIntentServiceResult Fail(string code, string message) => new()
    {
        Succeeded = false,
        ErrorCode = code,
        ErrorMessage = message
    };
}
