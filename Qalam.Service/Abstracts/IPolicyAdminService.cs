using Qalam.Data.DTOs.Policy;

namespace Qalam.Service.Abstracts;

public class PolicyAdminResult<T>
{
    public bool Succeeded { get; init; }
    public string? ErrorCode { get; init; }
    public List<string> Errors { get; init; } = new();
    public T? Data { get; init; }

    public static PolicyAdminResult<T> Ok(T data) => new() { Succeeded = true, Data = data };
    public static PolicyAdminResult<T> Fail(string code, params string[] errors) =>
        new() { Succeeded = false, ErrorCode = code, Errors = errors.ToList() };
}

public interface IPolicyAdminService
{
    Task<PolicyVersionDto> GetCurrentAsync(CancellationToken cancellationToken = default);
    Task<List<PolicyVersionDto>> ListVersionsAsync(CancellationToken cancellationToken = default);
    Task<PolicyVersionDto?> GetVersionAsync(int id, CancellationToken cancellationToken = default);
    Task<PolicyVersionDto?> GetDraftAsync(CancellationToken cancellationToken = default);
    Task<PolicyAdminResult<PolicyVersionDto>> SaveDraftAsync(SavePolicyDraftDto dto, int? userId, CancellationToken cancellationToken = default);
    Task<PolicyAdminResult<PolicyVersionDto>> PublishDraftAsync(PublishPolicyDraftDto dto, int? userId, CancellationToken cancellationToken = default);
    Task<bool> DiscardDraftAsync(CancellationToken cancellationToken = default);
    Task<PolicyCompareDto?> CompareAsync(int fromId, int toId, CancellationToken cancellationToken = default);
    List<string> ValidateRules(CancellationPolicyRules rules);
}
