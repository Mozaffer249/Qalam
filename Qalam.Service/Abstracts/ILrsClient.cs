namespace Qalam.Service.Abstracts;

public interface ILrsClient
{
    Task<LrsSendResult> SendStatementAsync(string statementJson, CancellationToken ct = default);
}

public record LrsSendResult(bool Success, int? HttpStatusCode, string? ErrorMessage);
